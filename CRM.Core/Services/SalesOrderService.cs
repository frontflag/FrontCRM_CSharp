using CRM.Core.Interfaces;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Purchase;

namespace CRM.Core.Services
{
    /// <summary>销售订单服务实现</summary>
    public class SalesOrderService : ISalesOrderService
    {
        private readonly IRepository<SellOrder> _soRepo;
        private readonly IRepository<SellOrderItem> _soItemRepo;
        private readonly IRepository<PurchaseOrder> _poRepo;
        private readonly IRepository<PurchaseOrderItem> _poItemRepo;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUnitOfWork? _unitOfWork;

        public SalesOrderService(
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IDataPermissionService dataPermissionService,
            IUnitOfWork? unitOfWork = null)
        {
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _poRepo = poRepo;
            _poItemRepo = poItemRepo;
            _dataPermissionService = dataPermissionService;
            _unitOfWork = unitOfWork;
        }

        public async Task<SellOrder> CreateAsync(CreateSalesOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SellOrderCode))
                throw new ArgumentException("销售单号不能为空", nameof(request.SellOrderCode));
            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new ArgumentException("客户ID不能为空", nameof(request.CustomerId));

            var all = await _soRepo.GetAllAsync();
            if (all.Any(o => o.SellOrderCode == request.SellOrderCode))
                throw new InvalidOperationException($"销售单号 {request.SellOrderCode} 已存在");

            var order = new SellOrder
            {
                Id = Guid.NewGuid().ToString(),
                SellOrderCode = request.SellOrderCode.Trim(),
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                SalesUserId = request.SalesUserId,
                SalesUserName = request.SalesUserName,
                Type = request.Type,
                Currency = request.Currency,
                DeliveryDate = request.DeliveryDate,
                DeliveryAddress = request.DeliveryAddress,
                Comment = request.Comment,
                Status = 0,
                ItemRows = request.Items.Count,
                CreateTime = DateTime.UtcNow
            };
            await _soRepo.AddAsync(order);

            decimal total = 0m;
            foreach (var item in request.Items)
            {
                var soItem = new SellOrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    SellOrderId = order.Id,
                    QuoteId = item.QuoteId,
                    ProductId = item.ProductId,
                    PN = item.PN,
                    Brand = item.Brand,
                    CustomerPnNo = item.CustomerPnNo,
                    Qty = item.Qty,
                    Price = item.Price,
                    Currency = item.Currency,
                    DateCode = item.DateCode,
                    DeliveryDate = item.DeliveryDate,
                    Comment = item.Comment,
                    CreateTime = DateTime.UtcNow
                };
                await _soItemRepo.AddAsync(soItem);
                total += item.Qty * item.Price;
            }
            order.Total = total;
            order.ConvertTotal = total;
            await _soRepo.UpdateAsync(order);

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return order;
        }

        public async Task<SellOrder?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return await _soRepo.GetByIdAsync(id);
        }

        public async Task<IEnumerable<SellOrder>> GetAllAsync()
        {
            return await _soRepo.GetAllAsync();
        }

        public async Task<PagedResult<SellOrder>> GetPagedAsync(SalesOrderQueryRequest request)
        {
            var all = await _soRepo.GetAllAsync();
            var filteredByPermission = all.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
            {
                filteredByPermission = await _dataPermissionService.FilterSalesOrdersAsync(request.CurrentUserId, filteredByPermission);
            }

            var query = filteredByPermission.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();
                query = query.Where(o =>
                    (!string.IsNullOrWhiteSpace(o.SellOrderCode) && o.SellOrderCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(o.CustomerName) && o.CustomerName.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
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

            return new PagedResult<SellOrder>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = page,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<SellOrder>> GetByCustomerIdAsync(string customerId)
        {
            var all = await _soRepo.GetAllAsync();
            return all.Where(o => o.CustomerId == customerId);
        }

        public async Task<SellOrder> UpdateAsync(string id, UpdateSalesOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var order = await _soRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"销售订单 {id} 不存在");

            if (request.CustomerName != null) order.CustomerName = request.CustomerName;
            if (request.SalesUserName != null) order.SalesUserName = request.SalesUserName;
            if (request.Type.HasValue) order.Type = request.Type.Value;
            if (request.Currency.HasValue) order.Currency = request.Currency.Value;
            if (request.DeliveryDate.HasValue) order.DeliveryDate = request.DeliveryDate;
            if (request.DeliveryAddress != null) order.DeliveryAddress = request.DeliveryAddress;
            if (request.Comment != null) order.Comment = request.Comment;

            if (request.Items != null && request.Items.Count > 0)
            {
                var existingItems = await _soItemRepo.GetAllAsync();
                var toDelete = existingItems.Where(i => i.SellOrderId == id).ToList();
                foreach (var d in toDelete) await _soItemRepo.DeleteAsync(d.Id);

                decimal total = 0m;
                foreach (var item in request.Items)
                {
                    var soItem = new SellOrderItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        SellOrderId = id,
                        QuoteId = item.QuoteId,
                        ProductId = item.ProductId,
                        PN = item.PN,
                        Brand = item.Brand,
                        CustomerPnNo = item.CustomerPnNo,
                        Qty = item.Qty,
                        Price = item.Price,
                        Currency = item.Currency,
                        DateCode = item.DateCode,
                        DeliveryDate = item.DeliveryDate,
                        Comment = item.Comment,
                        CreateTime = DateTime.UtcNow
                    };
                    await _soItemRepo.AddAsync(soItem);
                    total += item.Qty * item.Price;
                }
                order.Total = total;
                order.ConvertTotal = total;
                order.ItemRows = request.Items.Count;
            }

            order.ModifyTime = DateTime.UtcNow;
            await _soRepo.UpdateAsync(order);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return order;
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var order = await _soRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"销售订单 {id} 不存在");
            var items = await _soItemRepo.GetAllAsync();
            foreach (var item in items.Where(i => i.SellOrderId == id))
                await _soItemRepo.DeleteAsync(item.Id);
            await _soRepo.DeleteAsync(id);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            var order = await _soRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"销售订单 {id} 不存在");
            order.Status = status;
            order.ModifyTime = DateTime.UtcNow;
            await _soRepo.UpdateAsync(order);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task RequestStockOutAsync(string id, string requestedBy)
        {
            await UpdateStatusAsync(id, 4); // 已发货
        }

        public async Task<IEnumerable<object>> GetRelatedPurchaseOrdersAsync(string sellOrderId)
        {
            var soItems = await _soItemRepo.GetAllAsync();
            var sellItemIds = soItems.Where(i => i.SellOrderId == sellOrderId)
                                     .Select(i => i.Id).ToHashSet();
            var poItems = await _poItemRepo.GetAllAsync();
            var relatedPoIds = poItems.Where(i => sellItemIds.Contains(i.SellOrderItemId))
                                       .Select(i => i.PurchaseOrderId).Distinct().ToList();
            var allPo = await _poRepo.GetAllAsync();
            return allPo.Where(p => relatedPoIds.Contains(p.Id)).Cast<object>();
        }
    }
}
