using CRM.Core.Interfaces;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Purchase;
using CRM.Core.Utilities;

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
        private readonly ISerialNumberService _serialNumberService;

        public SalesOrderService(
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IDataPermissionService dataPermissionService,
            ISerialNumberService serialNumberService,
            IUnitOfWork? unitOfWork = null)
        {
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _poRepo = poRepo;
            _poItemRepo = poItemRepo;
            _dataPermissionService = dataPermissionService;
            _serialNumberService = serialNumberService;
            _unitOfWork = unitOfWork;
        }

        public async Task<SellOrder> CreateAsync(CreateSalesOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new ArgumentException("客户ID不能为空", nameof(request.CustomerId));

            var sellOrderCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.SalesOrder);

            var order = new SellOrder
            {
                Id = Guid.NewGuid().ToString(),
                SellOrderCode = sellOrderCode,
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                SalesUserId = request.SalesUserId,
                SalesUserName = request.SalesUserName,
                Type = request.Type,
                Currency = request.Currency,
                DeliveryDate = PostgreSqlDateTime.ToUtc(request.DeliveryDate),
                DeliveryAddress = request.DeliveryAddress,
                Comment = request.Comment,
                Status = SellOrderMainStatus.New,
                ItemRows = request.Items.Count,
                CreateTime = DateTime.UtcNow
            };
            await _soRepo.AddAsync(order);
            // 先落库主表，避免 sellorderitem 先插入导致外键约束失败
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

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
                    DeliveryDate = PostgreSqlDateTime.ToUtc(item.DeliveryDate),
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
            var order = await _soRepo.GetByIdAsync(id);
            if (order == null) return null;
            var items = await _soItemRepo.FindAsync(i => i.SellOrderId == id);
            order.Items = items.ToList();
            return order;
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
                query = query.Where(o => o.Status == (SellOrderMainStatus)request.Status.Value);

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
            if (request.DeliveryDate.HasValue) order.DeliveryDate = PostgreSqlDateTime.ToUtc(request.DeliveryDate.Value);
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
                        DeliveryDate = PostgreSqlDateTime.ToUtc(item.DeliveryDate),
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

        public async Task UpdateStatusAsync(string id, SellOrderMainStatus status, string? auditRemark = null)
        {
            if (!Enum.IsDefined(typeof(SellOrderMainStatus), status))
                throw new ArgumentException($"无效的销售订单主状态: {(short)status}", nameof(status));

            var order = await _soRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"销售订单 {id} 不存在");

            if (status == SellOrderMainStatus.PendingAudit && order.Status != SellOrderMainStatus.New)
                throw new InvalidOperationException("仅「新建」状态可提交审核");

            if (status == SellOrderMainStatus.Approved && order.Status != SellOrderMainStatus.PendingAudit)
                throw new InvalidOperationException("仅「待审核」状态可审核通过");

            if (status == SellOrderMainStatus.AuditFailed && order.Status != SellOrderMainStatus.PendingAudit)
                throw new InvalidOperationException("仅「待审核」状态可审核拒绝");

            order.Status = status;
            if (status == SellOrderMainStatus.AuditFailed && !string.IsNullOrWhiteSpace(auditRemark))
                order.AuditRemark = auditRemark.Trim();
            else if (status == SellOrderMainStatus.Approved)
                order.AuditRemark = null;

            order.ModifyTime = DateTime.UtcNow;
            await _soRepo.UpdateAsync(order);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task RequestStockOutAsync(string id, string requestedBy)
        {
            // 申请出库后进入「进行中」（完成=Completed 由业务手动或后续流程标记）
            await UpdateStatusAsync(id, SellOrderMainStatus.InProgress);
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

        public async Task<PagedResult<SellOrderItemLineDto>> GetSellOrderItemLinesPagedAsync(SellOrderItemLineQueryRequest request)
        {
            var allOrders = await _soRepo.GetAllAsync();
            var filteredOrders = allOrders.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
                filteredOrders = await _dataPermissionService.FilterSalesOrdersAsync(request.CurrentUserId, filteredOrders);
            var orderDict = filteredOrders.ToDictionary(o => o.Id);

            var allItems = await _soItemRepo.GetAllAsync();
            IEnumerable<(SellOrderItem Item, SellOrder Order)> joined = allItems
                .Where(i => orderDict.ContainsKey(i.SellOrderId))
                .Select(i => (i, orderDict[i.SellOrderId]));

            if (request.OrderCreateStart.HasValue)
            {
                var s = request.OrderCreateStart.Value;
                joined = joined.Where(x => x.Order.CreateTime >= s);
            }

            if (request.OrderCreateEnd.HasValue)
            {
                var e = request.OrderCreateEnd.Value.Date.AddDays(1);
                joined = joined.Where(x => x.Order.CreateTime < e);
            }

            if (!string.IsNullOrWhiteSpace(request.CustomerName))
            {
                var k = request.CustomerName.Trim();
                joined = joined.Where(x =>
                    !string.IsNullOrWhiteSpace(x.Order.CustomerName) &&
                    x.Order.CustomerName.Contains(k, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.SalesUserName))
            {
                var k = request.SalesUserName.Trim();
                joined = joined.Where(x =>
                    !string.IsNullOrWhiteSpace(x.Order.SalesUserName) &&
                    x.Order.SalesUserName.Contains(k, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.Pn))
            {
                var k = request.Pn.Trim();
                joined = joined.Where(x =>
                    !string.IsNullOrWhiteSpace(x.Item.PN) &&
                    x.Item.PN.Contains(k, StringComparison.OrdinalIgnoreCase));
            }

            var list = joined
                .OrderByDescending(x => x.Order.CreateTime)
                .ThenBy(x => x.Item.Id)
                .Select(x =>
                {
                    var lineTotal = Math.Round(x.Item.Qty * x.Item.Price, 2, MidpointRounding.AwayFromZero);
                    decimal? usdUnit = x.Item.Currency == 2 ? x.Item.Price : null;
                    decimal? usdLine = x.Item.Currency == 2 ? lineTotal : null;
                    return new SellOrderItemLineDto
                    {
                        SellOrderItemId = x.Item.Id,
                        SellOrderId = x.Order.Id,
                        SellOrderCode = x.Order.SellOrderCode,
                        OrderStatus = (short)x.Order.Status,
                        OrderCreateTime = x.Order.CreateTime,
                        CustomerId = x.Order.CustomerId,
                        CustomerName = x.Order.CustomerName,
                        SalesUserName = x.Order.SalesUserName,
                        PN = x.Item.PN,
                        Brand = x.Item.Brand,
                        Qty = x.Item.Qty,
                        Price = x.Item.Price,
                        LineTotal = lineTotal,
                        Currency = x.Item.Currency,
                        UsdUnitPrice = usdUnit,
                        UsdLineTotal = usdLine,
                        ItemStatus = x.Item.Status
                    };
                })
                .ToList();

            var total = list.Count;
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
            var pageItems = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResult<SellOrderItemLineDto>
            {
                Items = pageItems,
                TotalCount = total,
                PageIndex = page,
                PageSize = pageSize
            };
        }
    }
}
