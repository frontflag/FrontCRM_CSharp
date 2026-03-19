using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;

namespace CRM.Core.Services
{
    /// <summary>采购订单服务实现</summary>
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IRepository<PurchaseOrder> _poRepo;
        private readonly IRepository<PurchaseOrderItem> _poItemRepo;
        private readonly IRepository<SellOrder> _soRepo;
        private readonly IRepository<SellOrderItem> _soItemRepo;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUnitOfWork? _unitOfWork;

        public PurchaseOrderService(
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IDataPermissionService dataPermissionService,
            IUnitOfWork? unitOfWork = null)
        {
            _poRepo = poRepo;
            _poItemRepo = poItemRepo;
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _dataPermissionService = dataPermissionService;
            _unitOfWork = unitOfWork;
        }

        public async Task<PurchaseOrder> CreateAsync(CreatePurchaseOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PurchaseOrderCode))
                throw new ArgumentException("采购单号不能为空", nameof(request.PurchaseOrderCode));
            if (string.IsNullOrWhiteSpace(request.VendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(request.VendorId));
            if (!request.Items.Any())
                throw new ArgumentException("至少需要一条明细行", nameof(request.Items));

            var all = await _poRepo.GetAllAsync();
            if (all.Any(o => o.PurchaseOrderCode == request.PurchaseOrderCode))
                throw new InvalidOperationException($"采购单号 {request.PurchaseOrderCode} 已存在");

            var order = new PurchaseOrder
            {
                Id = Guid.NewGuid().ToString(),
                PurchaseOrderCode = request.PurchaseOrderCode.Trim(),
                VendorId = request.VendorId,
                VendorName = request.VendorName,
                VendorCode = request.VendorCode,
                VendorContactId = request.VendorContactId,
                PurchaseUserId = request.PurchaseUserId,
                PurchaseUserName = request.PurchaseUserName,
                Type = request.Type,
                Currency = request.Currency,
                DeliveryDate = request.DeliveryDate,
                DeliveryAddress = request.DeliveryAddress,
                Comment = request.Comment,
                InnerComment = request.InnerComment,
                Status = 0,
                ItemRows = request.Items.Count,
                CreateTime = DateTime.UtcNow
            };
            await _poRepo.AddAsync(order);

            decimal total = 0m;
            foreach (var item in request.Items)
            {
                var poItem = new PurchaseOrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    PurchaseOrderId = order.Id,
                    SellOrderItemId = item.SellOrderItemId,
                    VendorId = item.VendorId.Length > 0 ? item.VendorId : request.VendorId,
                    ProductId = item.ProductId,
                    PN = item.PN,
                    Brand = item.Brand,
                    Qty = item.Qty,
                    Cost = item.Cost,
                    Currency = item.Currency,
                    DeliveryDate = item.DeliveryDate,
                    Comment = item.Comment,
                    CreateTime = DateTime.UtcNow
                };
                await _poItemRepo.AddAsync(poItem);
                total += item.Qty * item.Cost;
            }
            order.Total = total;
            order.ConvertTotal = total;
            await _poRepo.UpdateAsync(order);

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return order;
        }

        public async Task<PurchaseOrder?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return await _poRepo.GetByIdAsync(id);
        }

        public async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
        {
            return await _poRepo.GetAllAsync();
        }

        public async Task<PagedResult<PurchaseOrder>> GetPagedAsync(PurchaseOrderQueryRequest request)
        {
            var all = await _poRepo.GetAllAsync();
            var filteredByPermission = all.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
            {
                filteredByPermission = await _dataPermissionService.FilterPurchaseOrdersAsync(request.CurrentUserId, filteredByPermission);
            }

            var query = filteredByPermission.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();
                query = query.Where(o =>
                    (!string.IsNullOrWhiteSpace(o.PurchaseOrderCode) && o.PurchaseOrderCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(o.VendorName) && o.VendorName.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
            }

            if (request.Status.HasValue)
                query = query.Where(o => o.Status == request.Status.Value);

            if (request.StartDate.HasValue)
                query = query.Where(o => o.CreateTime >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(o => o.CreateTime <= request.EndDate.Value.AddDays(1));

            var totalCount = query.Count();
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
            var items = query.OrderByDescending(o => o.CreateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<PurchaseOrder>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = page,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<PurchaseOrder>> GetBySellOrderCodeAsync(string sellOrderCode)
        {
            // 通过 SellOrderItem 关联查找
            var soAll = await _soRepo.GetAllAsync();
            var so = soAll.FirstOrDefault(o => o.SellOrderCode == sellOrderCode);
            if (so == null) return Enumerable.Empty<PurchaseOrder>();

            var soItems = await _soItemRepo.GetAllAsync();
            var sellItemIds = soItems.Where(i => i.SellOrderId == so.Id).Select(i => i.Id).ToHashSet();

            var poItems = await _poItemRepo.GetAllAsync();
            var poIds = poItems.Where(i => sellItemIds.Contains(i.SellOrderItemId))
                               .Select(i => i.PurchaseOrderId).Distinct().ToHashSet();

            var allPo = await _poRepo.GetAllAsync();
            return allPo.Where(p => poIds.Contains(p.Id));
        }

        public async Task<IEnumerable<PurchaseOrderItem>> GetItemsBySellOrderItemIdsAsync(List<string> sellOrderItemIds)
        {
            var all = await _poItemRepo.GetAllAsync();
            return all.Where(i => sellOrderItemIds.Contains(i.SellOrderItemId));
        }

        public async Task<IEnumerable<PurchaseOrder>> AutoGenerateFromSellOrderAsync(string sellOrderId)
        {
            var so = await _soRepo.GetByIdAsync(sellOrderId)
                ?? throw new InvalidOperationException($"销售订单 {sellOrderId} 不存在");

            var soItems = await _soItemRepo.GetAllAsync();
            var items = soItems.Where(i => i.SellOrderId == sellOrderId && i.Status == 0).ToList();
            if (!items.Any()) return Enumerable.Empty<PurchaseOrder>();

            // 按供应商分组（此处简化：每个明细生成一张采购单）
            var result = new List<PurchaseOrder>();
            var seq = 1;
            foreach (var item in items)
            {
                var poCode = $"PO-AUTO-{so.SellOrderCode}-{seq++:D2}";
                var req = new CreatePurchaseOrderRequest
                {
                    PurchaseOrderCode = poCode,
                    VendorId = "PENDING",
                    Type = so.Type,
                    Currency = so.Currency,
                    DeliveryDate = item.DeliveryDate ?? so.DeliveryDate,
                    Comment = $"由销售订单 {so.SellOrderCode} 自动生成",
                    Items = new List<CreatePurchaseOrderItemRequest>
                    {
                        new()
                        {
                            SellOrderItemId = item.Id,
                            VendorId = "PENDING",
                            ProductId = item.ProductId,
                            PN = item.PN,
                            Brand = item.Brand,
                            Qty = item.Qty - item.PurchasedQty,
                            Cost = 0,
                            Currency = item.Currency,
                            DeliveryDate = item.DeliveryDate ?? so.DeliveryDate
                        }
                    }
                };
                var po = await CreateAsync(req);
                result.Add(po);
            }
            return result;
        }

        public async Task<PurchaseOrder> UpdateAsync(string id, UpdatePurchaseOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var order = await _poRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"采购订单 {id} 不存在");

            if (request.VendorName != null) order.VendorName = request.VendorName;
            if (request.PurchaseUserName != null) order.PurchaseUserName = request.PurchaseUserName;
            if (request.Type.HasValue) order.Type = request.Type.Value;
            if (request.Currency.HasValue) order.Currency = request.Currency.Value;
            if (request.DeliveryDate.HasValue) order.DeliveryDate = request.DeliveryDate;
            if (request.DeliveryAddress != null) order.DeliveryAddress = request.DeliveryAddress;
            if (request.Comment != null) order.Comment = request.Comment;
            if (request.InnerComment != null) order.InnerComment = request.InnerComment;

            if (request.Items != null && request.Items.Count > 0)
            {
                var existing = await _poItemRepo.GetAllAsync();
                foreach (var d in existing.Where(i => i.PurchaseOrderId == id))
                    await _poItemRepo.DeleteAsync(d.Id);

                decimal total = 0m;
                foreach (var item in request.Items)
                {
                    var poItem = new PurchaseOrderItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        PurchaseOrderId = id,
                        SellOrderItemId = item.SellOrderItemId,
                        VendorId = item.VendorId.Length > 0 ? item.VendorId : order.VendorId,
                        ProductId = item.ProductId,
                        PN = item.PN,
                        Brand = item.Brand,
                        Qty = item.Qty,
                        Cost = item.Cost,
                        Currency = item.Currency,
                        DeliveryDate = item.DeliveryDate,
                        Comment = item.Comment,
                        CreateTime = DateTime.UtcNow
                    };
                    await _poItemRepo.AddAsync(poItem);
                    total += item.Qty * item.Cost;
                }
                order.Total = total;
                order.ConvertTotal = total;
                order.ItemRows = request.Items.Count;
            }

            order.ModifyTime = DateTime.UtcNow;
            await _poRepo.UpdateAsync(order);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return order;
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            _ = await _poRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"采购订单 {id} 不存在");
            var items = await _poItemRepo.GetAllAsync();
            foreach (var item in items.Where(i => i.PurchaseOrderId == id))
                await _poItemRepo.DeleteAsync(item.Id);
            await _poRepo.DeleteAsync(id);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            var order = await _poRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"采购订单 {id} 不存在");
            order.Status = status;
            order.ModifyTime = DateTime.UtcNow;
            await _poRepo.UpdateAsync(order);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }
    }
}
