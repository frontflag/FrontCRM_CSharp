using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    /// <summary>采购订单服务实现</summary>
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private const short StatusNew = 1;
        private const short StatusPendingAudit = 2;
        private const short StatusApproved = 10;
        private const short StatusPendingConfirm = 20;
        private const short StatusConfirmed = 30;
        private const short StatusInProgress = 50;
        private const short StatusCompleted = 100;
        private const short StatusAuditFailed = -1;
        private const short StatusCancelled = -2;

        private readonly IRepository<PurchaseOrder> _poRepo;
        private readonly IRepository<PurchaseOrderItem> _poItemRepo;
        private readonly IRepository<PurchaseOrderItemExtend> _poItemExtendRepo;
        private readonly IRepository<SellOrder> _soRepo;
        private readonly IRepository<SellOrderItem> _soItemRepo;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUnitOfWork? _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;

        public PurchaseOrderService(
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IRepository<PurchaseOrderItemExtend> poItemExtendRepo,
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IDataPermissionService dataPermissionService,
            ISerialNumberService serialNumberService,
            IUnitOfWork? unitOfWork = null)
        {
            _poRepo = poRepo;
            _poItemRepo = poItemRepo;
            _poItemExtendRepo = poItemExtendRepo;
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _dataPermissionService = dataPermissionService;
            _serialNumberService = serialNumberService;
            _unitOfWork = unitOfWork;
        }

        public async Task<PurchaseOrder> CreateAsync(CreatePurchaseOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.VendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(request.VendorId));
            if (!request.Items.Any())
                throw new ArgumentException("至少需要一条明细行", nameof(request.Items));

            var purchaseOrderCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.PurchaseOrder);

            var total = request.Items.Sum(item => item.Qty * item.Cost);

            var order = new PurchaseOrder
            {
                Id = Guid.NewGuid().ToString(),
                PurchaseOrderCode = purchaseOrderCode,
                VendorId = request.VendorId,
                VendorName = request.VendorName,
                VendorCode = request.VendorCode,
                VendorContactId = request.VendorContactId,
                PurchaseUserId = request.PurchaseUserId,
                PurchaseUserName = request.PurchaseUserName,
                Type = request.Type,
                Currency = request.Currency,
                DeliveryDate = PostgreSqlDateTime.ToUtc(request.DeliveryDate),
                DeliveryAddress = request.DeliveryAddress,
                Comment = request.Comment,
                InnerComment = request.InnerComment,
                Status = StatusNew,
                ItemRows = request.Items.Count,
                Total = total,
                ConvertTotal = total,
                CreateTime = DateTime.UtcNow
            };
            await _poRepo.AddAsync(order);

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
                    // PostgreSQL timestamptz 不接受 DateTimeKind=Unspecified，统一转 UTC
                    DeliveryDate = PostgreSqlDateTime.ToUtc(item.DeliveryDate),
                    Comment = item.Comment,
                    InnerComment = item.InnerComment,
                    Status = StatusNew,
                    CreateTime = DateTime.UtcNow
                };
                await _poItemRepo.AddAsync(poItem);
                await AddPurchaseOrderItemExtendAsync(poItem);
            }

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return order;
        }

        private async Task AddPurchaseOrderItemExtendAsync(PurchaseOrderItem poItem)
        {
            var lineTotal = Math.Round(poItem.Qty * poItem.Cost, 2, MidpointRounding.AwayFromZero);
            await _poItemExtendRepo.AddAsync(new PurchaseOrderItemExtend
            {
                Id = poItem.Id,
                QtyStockInNotifyNot = poItem.Qty,
                PurchaseInvoiceAmount = lineTotal,
                PurchaseInvoiceToBe = lineTotal,
                PaymentAmount = lineTotal,
                PaymentAmountNot = lineTotal,
                CreateTime = DateTime.UtcNow
            });
        }

        public async Task<PurchaseOrder?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var order = await _poRepo.GetByIdAsync(id);
            if (order == null) return null;
            var items = await _poItemRepo.FindAsync(i => i.PurchaseOrderId == id);
            order.Items = items.ToList();
            return order;
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
            foreach (var item in items)
            {
                var req = new CreatePurchaseOrderRequest
                {
                    PurchaseOrderCode = string.Empty,
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
            if (request.PurchaseUserId != null) order.PurchaseUserId = request.PurchaseUserId;
            if (request.PurchaseUserName != null) order.PurchaseUserName = request.PurchaseUserName;
            if (request.Type.HasValue) order.Type = request.Type.Value;
            if (request.Currency.HasValue) order.Currency = request.Currency.Value;
            if (request.DeliveryDate.HasValue) order.DeliveryDate = PostgreSqlDateTime.ToUtc(request.DeliveryDate.Value);
            if (request.DeliveryAddress != null) order.DeliveryAddress = request.DeliveryAddress;
            if (request.Comment != null) order.Comment = request.Comment;
            if (request.InnerComment != null) order.InnerComment = request.InnerComment;

            if (request.Items != null && request.Items.Count > 0)
            {
                var existing = await _poItemRepo.GetAllAsync();
                foreach (var d in existing.Where(i => i.PurchaseOrderId == id))
                {
                    await _poItemExtendRepo.DeleteAsync(d.Id);
                    await _poItemRepo.DeleteAsync(d.Id);
                }

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
                    // PostgreSQL timestamptz 不接受 DateTimeKind=Unspecified，统一转 UTC
                    DeliveryDate = PostgreSqlDateTime.ToUtc(item.DeliveryDate),
                        Comment = item.Comment,
                        InnerComment = item.InnerComment,
                        Status = ShouldSyncOrderAndItemStatus(order.Status) ? order.Status : StatusNew,
                        CreateTime = DateTime.UtcNow
                    };
                    await _poItemRepo.AddAsync(poItem);
                    await AddPurchaseOrderItemExtendAsync(poItem);
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
            {
                await _poItemExtendRepo.DeleteAsync(item.Id);
                await _poItemRepo.DeleteAsync(item.Id);
            }
            await _poRepo.DeleteAsync(id);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            var order = await _poRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"采购订单 {id} 不存在");

            ValidateStatusTransition(order.Status, status);
            order.Status = status;
            order.ModifyTime = DateTime.UtcNow;
            await _poRepo.UpdateAsync(order);

            if (ShouldSyncOrderAndItemStatus(status))
            {
                var items = await _poItemRepo.FindAsync(i => i.PurchaseOrderId == id);
                foreach (var item in items)
                {
                    item.Status = status;
                    item.ModifyTime = DateTime.UtcNow;
                    await _poItemRepo.UpdateAsync(item);
                }
            }

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        private static bool ShouldSyncOrderAndItemStatus(short status)
        {
            return status is StatusNew or StatusPendingAudit or StatusApproved or StatusPendingConfirm or StatusConfirmed;
        }

        private static void ValidateStatusTransition(short current, short target)
        {
            if (current == target) return;
            var valid = current switch
            {
                StatusNew => target is StatusPendingAudit or StatusCancelled,
                StatusPendingAudit => target is StatusApproved or StatusAuditFailed or StatusCancelled,
                StatusAuditFailed => target is StatusNew or StatusCancelled,
                StatusApproved => target is StatusPendingConfirm or StatusCancelled,
                StatusPendingConfirm => target is StatusConfirmed or StatusCancelled,
                StatusConfirmed => target is StatusInProgress or StatusCancelled,
                StatusInProgress => target is StatusCompleted,
                _ => false
            };

            if (!valid)
            {
                throw new InvalidOperationException($"不允许的状态流转: {current} -> {target}");
            }
        }
    }
}
