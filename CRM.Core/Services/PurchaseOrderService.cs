using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Models.System;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;

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
        private readonly IRepository<PurchaseRequisition>? _prRepo;
        private readonly IRepository<StockInNotify>? _notifyRepo;
        private readonly IRepository<SellOrder> _soRepo;
        private readonly IRepository<SellOrderItem> _soItemRepo;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUnitOfWork? _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IFinanceExchangeRateService _financeExchangeRateService;
        private readonly IOrderJourneyLogService _orderJourneyLog;
        private readonly ISellOrderItemExtendSyncService _sellOrderItemExtendSync;
        private readonly IPurchaseRequisitionService? _purchaseRequisitionService;
        private readonly IPurchaseOrderItemExtendSyncService _poItemExtendSync;
        private readonly IPurchaseOrderExtendLineSeqService _poLineSeq;
        private readonly ILogger<PurchaseOrderService> _logger;

        public PurchaseOrderService(
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IRepository<PurchaseOrderItemExtend> poItemExtendRepo,
            IRepository<PurchaseRequisition>? prRepo,
            IRepository<StockInNotify>? notifyRepo,
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IDataPermissionService dataPermissionService,
            ISerialNumberService serialNumberService,
            IFinanceExchangeRateService financeExchangeRateService,
            IOrderJourneyLogService orderJourneyLog,
            ISellOrderItemExtendSyncService sellOrderItemExtendSync,
            IPurchaseRequisitionService? purchaseRequisitionService,
            IPurchaseOrderItemExtendSyncService poItemExtendSync,
            IPurchaseOrderExtendLineSeqService poLineSeq,
            ILogger<PurchaseOrderService> logger,
            IUnitOfWork? unitOfWork = null)
        {
            _poRepo = poRepo;
            _poItemRepo = poItemRepo;
            _poItemExtendRepo = poItemExtendRepo;
            _prRepo = prRepo;
            _notifyRepo = notifyRepo;
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _dataPermissionService = dataPermissionService;
            _serialNumberService = serialNumberService;
            _financeExchangeRateService = financeExchangeRateService;
            _orderJourneyLog = orderJourneyLog;
            _sellOrderItemExtendSync = sellOrderItemExtendSync;
            _purchaseRequisitionService = purchaseRequisitionService;
            _poItemExtendSync = poItemExtendSync;
            _poLineSeq = poLineSeq;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        // 兼容旧调用方（单测/临时构造）：不注入采购申请回写依赖时，状态回写能力自动降级为 no-op。
        public PurchaseOrderService(
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IRepository<PurchaseOrderItemExtend> poItemExtendRepo,
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IDataPermissionService dataPermissionService,
            ISerialNumberService serialNumberService,
            IFinanceExchangeRateService financeExchangeRateService,
            IOrderJourneyLogService orderJourneyLog,
            ISellOrderItemExtendSyncService sellOrderItemExtendSync,
            IPurchaseOrderItemExtendSyncService poItemExtendSync,
            IPurchaseOrderExtendLineSeqService poLineSeq,
            ILogger<PurchaseOrderService> logger,
            IUnitOfWork? unitOfWork = null)
            : this(
                poRepo,
                poItemRepo,
                poItemExtendRepo,
                null,
                null,
                soRepo,
                soItemRepo,
                dataPermissionService,
                serialNumberService,
                financeExchangeRateService,
                orderJourneyLog,
                sellOrderItemExtendSync,
                null,
                poItemExtendSync,
                poLineSeq,
                logger,
                unitOfWork)
        {
        }

        private async Task<Dictionary<string, short>> LoadArrivalNoticeStatusMapByPoLineIdsAsync(
            IReadOnlyCollection<string> purchaseOrderItemIds)
        {
            if (_notifyRepo == null || purchaseOrderItemIds.Count == 0)
                return new Dictionary<string, short>(StringComparer.OrdinalIgnoreCase);
            var rows = (await _notifyRepo.FindAsync(n =>
                    n.PurchaseOrderItemId != null && purchaseOrderItemIds.Contains(n.PurchaseOrderItemId)))
                .ToList();
            return rows.ToDictionary(x => x.Id, x => x.Status, StringComparer.OrdinalIgnoreCase);
        }

        private static int CountArrivalNoticeStatusChanges(
            IReadOnlyDictionary<string, short> before,
            IReadOnlyDictionary<string, short> after)
        {
            var changed = 0;
            foreach (var kv in after)
            {
                if (before.TryGetValue(kv.Key, out var prev) && prev == kv.Value) continue;
                changed++;
            }
            return changed;
        }

        private static string? NormalizeActingUserId(string? actingUserId) =>
            string.IsNullOrWhiteSpace(actingUserId) ? null : actingUserId.Trim();

        /// <summary>前端占位：无销售明细时传入的全零 GUID，不视为以销定采。</summary>
        private const string EmptySellOrderItemSentinel = "00000000-0000-0000-0000-000000000000";

        private static bool IsLinkedSellOrderPurchaseLine(string? sellOrderItemId) =>
            !string.IsNullOrWhiteSpace(sellOrderItemId) &&
            !string.Equals(sellOrderItemId.Trim(), EmptySellOrderItemSentinel, StringComparison.OrdinalIgnoreCase);

        private async Task<int> RecalculatePurchaseRequisitionBySellLinesAsync(IEnumerable<string> sellOrderItemIds)
        {
            if (_purchaseRequisitionService == null || _prRepo == null) return 0;
            var ids = sellOrderItemIds
                .Where(IsLinkedSellOrderPurchaseLine)
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0) return 0;

            var prs = (await _prRepo.FindAsync(x => ids.Contains(x.SellOrderItemId))).ToList();
            foreach (var pr in prs)
                await _purchaseRequisitionService.RecalculateAsync(pr.Id);
            return prs.Count;
        }

        /// <summary>写入库：无销售行或前端占位 GUID → NULL，避免违反 sellorderitem 外键。</summary>
        private static string? NormalizeStoredSellOrderItemId(string? sellOrderItemId) =>
            IsLinkedSellOrderPurchaseLine(sellOrderItemId) ? sellOrderItemId!.Trim() : null;

        /// <summary>
        /// 有销售明细关联 → 客单采购(1)；否则备货(2)；无销售关联且请求为样品 → 3。
        /// </summary>
        private static short ResolvePurchaseOrderHeaderType(short requestedType, IEnumerable<CreatePurchaseOrderItemRequest> items)
        {
            if (items.Any(i => IsLinkedSellOrderPurchaseLine(i.SellOrderItemId)))
                return 1;
            if (requestedType == 3)
                return 3;
            return 2;
        }

        public async Task<PurchaseOrder> CreateAsync(CreatePurchaseOrderRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.VendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(request.VendorId));
            if (!request.Items.Any())
                throw new ArgumentException("至少需要一条明细行", nameof(request.Items));

            var purchaseOrderCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.PurchaseOrder);

            var total = request.Items.Sum(item => item.Qty * item.Cost);
            var headerType = ResolvePurchaseOrderHeaderType(request.Type, request.Items);
            var distinctLineCurrencies = request.Items.Select(i => i.Currency).Distinct().ToList();
            var headerCurrency = distinctLineCurrencies.Count == 1 ? distinctLineCurrencies[0] : request.Currency;

            _logger.LogInformation(
                "PO CreateAsync 开始: RequestType={RequestType} HeaderType={HeaderType} ItemCount={ItemCount} VendorId={VendorId} PurchaseUserId={PurchaseUserId} ActingUserId={ActingUserId} GeneratedCode={Code}",
                request.Type, headerType, request.Items.Count, request.VendorId, request.PurchaseUserId ?? "(null)", actingUserId ?? "(null)", purchaseOrderCode);

            for (var i = 0; i < request.Items.Count; i++)
            {
                var it = request.Items[i];
                var rawSell = it.SellOrderItemId;
                var stored = NormalizeStoredSellOrderItemId(rawSell);
                _logger.LogInformation(
                    "PO CreateAsync 明细[{Index}]: SellOrderItemIdRaw={RawSell} StoredNull={StoredNull} PN={Pn} Qty={Qty} Cost={Cost} VendorId={LineVendorId}",
                    i, string.IsNullOrEmpty(rawSell) ? "(empty)" : rawSell, stored == null, it.PN ?? "(null)", it.Qty, it.Cost, string.IsNullOrEmpty(it.VendorId) ? "(header)" : it.VendorId);
            }

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
                Type = headerType,
                Currency = headerCurrency,
                DeliveryDate = PostgreSqlDateTime.ToUtc(request.DeliveryDate),
                DeliveryAddress = request.DeliveryAddress,
                Comment = request.Comment,
                InnerComment = request.InnerComment,
                Status = StatusNew,
                ItemRows = request.Items.Count,
                Total = total,
                ConvertTotal = total,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = NormalizeActingUserId(actingUserId)
            };
            await _poRepo.AddAsync(order);
            if (_unitOfWork != null)
            {
                _logger.LogInformation("PO CreateAsync SaveChanges(主表): OrderId={OrderId}", order.Id);
                await _unitOfWork.SaveChangesAsync();
            }

            var fx = await _financeExchangeRateService.GetCurrentAsync();
            var firstSeq = await _poLineSeq.ReserveNextSequenceBlockAsync(order.Id, request.Items.Count);
            var lineIndex = 0;
            var createdLines = new List<PurchaseOrderItem>();
            foreach (var item in request.Items)
            {
                var seq = firstSeq + lineIndex++;
                var poItem = new PurchaseOrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    PurchaseOrderId = order.Id,
                    PurchaseOrderItemCode = OrderLineItemCodes.Purchase(order.PurchaseOrderCode, seq),
                    SellOrderItemId = NormalizeStoredSellOrderItemId(item.SellOrderItemId),
                    VendorId = !string.IsNullOrWhiteSpace(item.VendorId) ? item.VendorId.Trim() : request.VendorId,
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
                poItem.ConvertPrice = ExchangeRateToUsdConverter.UnitLocalToUsd(
                    poItem.Cost, poItem.Currency, fx.UsdToCny, fx.UsdToHkd, fx.UsdToEur);
                await _poItemRepo.AddAsync(poItem);
                createdLines.Add(poItem);
                await AddPurchaseOrderItemExtendAsync(poItem);
            }

            if (_unitOfWork != null)
            {
                _logger.LogInformation("PO CreateAsync SaveChanges(明细+扩展): LineCount={Count} OrderId={OrderId}", createdLines.Count, order.Id);
                await _unitOfWork.SaveChangesAsync();
            }

            var linkedSellLineIds = createdLines
                         .Select(x => x.SellOrderItemId)
                         .Where(s => IsLinkedSellOrderPurchaseLine(s))
                         .Select(s => s!.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .ToList();
            foreach (var sid in linkedSellLineIds)
                await _sellOrderItemExtendSync.RecalculateAsync(sid);
            await RecalculatePurchaseRequisitionBySellLinesAsync(linkedSellLineIds);

            foreach (var line in createdLines)
                await _poItemExtendSync.RecalculateAsync(line.Id);

            var journeyTime = DateTime.UtcNow;
            await _orderJourneyLog.AppendAsync(new OrderJourneyLog
            {
                EntityKind = OrderJourneyEntityKinds.PurchaseOrder,
                EntityId = order.Id,
                DocumentCode = order.PurchaseOrderCode,
                EventCode = OrderJourneyEventCodes.PoCreated,
                EventTime = journeyTime,
                Amount = order.Total,
                Currency = order.Currency,
                ActorKind = OrderJourneyActorKinds.System,
                Source = nameof(PurchaseOrderService)
            });
            foreach (var line in createdLines)
            {
                var lineTotal = Math.Round(line.Qty * line.Cost, 2, MidpointRounding.AwayFromZero);
                await _orderJourneyLog.AppendAsync(new OrderJourneyLog
                {
                    EntityKind = OrderJourneyEntityKinds.PurchaseOrderItem,
                    EntityId = line.Id,
                    ParentEntityKind = OrderJourneyEntityKinds.PurchaseOrder,
                    ParentEntityId = order.Id,
                    DocumentCode = order.PurchaseOrderCode,
                    LineHint = JourneyLineHint(line.PN, line.Brand),
                    EventCode = OrderJourneyEventCodes.PoItemCreated,
                    EventTime = journeyTime,
                    Quantity = line.Qty,
                    Amount = lineTotal,
                    Currency = line.Currency,
                    RelatedEntityKind = OrderJourneyEntityKinds.SellOrderItem,
                    RelatedEntityId = line.SellOrderItemId,
                    ActorKind = OrderJourneyActorKinds.System,
                    Source = nameof(PurchaseOrderService)
                });
            }

            return order;
        }

        private static string? JourneyLineHint(string? pn, string? brand)
        {
            var s = $"{pn ?? ""} / {brand ?? ""}".Trim();
            if (s == "/") return null;
            return s.Length <= 200 ? s : s[..200];
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
                PaymentAmountRequested = 0m,
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
            var poIds = poItems.Where(i => i.SellOrderItemId != null && sellItemIds.Contains(i.SellOrderItemId))
                               .Select(i => i.PurchaseOrderId).Distinct().ToHashSet();

            var allPo = await _poRepo.GetAllAsync();
            return allPo.Where(p => poIds.Contains(p.Id));
        }

        public async Task<IEnumerable<PurchaseOrderItem>> GetItemsBySellOrderItemIdsAsync(List<string> sellOrderItemIds)
        {
            var all = await _poItemRepo.GetAllAsync();
            return all.Where(i => i.SellOrderItemId != null && sellOrderItemIds.Contains(i.SellOrderItemId));
        }

        public async Task<IEnumerable<PurchaseOrder>> AutoGenerateFromSellOrderAsync(string sellOrderId, string? actingUserId = null)
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
                    Type = 1,
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
                var po = await CreateAsync(req, actingUserId);
                result.Add(po);
            }
            return result;
        }

        public async Task<PurchaseOrder> UpdateAsync(string id, UpdatePurchaseOrderRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var order = await _poRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"采购订单 {id} 不存在");

            if (request.VendorName != null) order.VendorName = request.VendorName;
            if (request.PurchaseUserId != null) order.PurchaseUserId = request.PurchaseUserId;
            if (request.PurchaseUserName != null) order.PurchaseUserName = request.PurchaseUserName;
            if (request.Currency.HasValue) order.Currency = request.Currency.Value;
            if (request.DeliveryDate.HasValue) order.DeliveryDate = PostgreSqlDateTime.ToUtc(request.DeliveryDate.Value);
            if (request.DeliveryAddress != null) order.DeliveryAddress = request.DeliveryAddress;
            if (request.Comment != null) order.Comment = request.Comment;
            if (request.InnerComment != null) order.InnerComment = request.InnerComment;

            var replacedItemCount = 0;
            List<PurchaseOrderItem>? newLines = null;
            var recalcSellLineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (request.Items != null && request.Items.Count > 0)
            {
                var existing = await _poItemRepo.GetAllAsync();
                foreach (var d in existing.Where(i => i.PurchaseOrderId == id))
                {
                    if (!string.IsNullOrWhiteSpace(d.SellOrderItemId))
                        recalcSellLineIds.Add(d.SellOrderItemId.Trim());
                    await _poItemExtendRepo.DeleteAsync(d.Id);
                    await _poItemRepo.DeleteAsync(d.Id);
                }

                var fx = await _financeExchangeRateService.GetCurrentAsync();
                var firstSeq = await _poLineSeq.ReserveNextSequenceBlockAsync(id, request.Items.Count);
                var lineIndex = 0;
                decimal total = 0m;
                newLines = new List<PurchaseOrderItem>();
                foreach (var item in request.Items)
                {
                    var seq = firstSeq + lineIndex++;
                    var poItem = new PurchaseOrderItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        PurchaseOrderId = id,
                        PurchaseOrderItemCode = OrderLineItemCodes.Purchase(order.PurchaseOrderCode, seq),
                        SellOrderItemId = NormalizeStoredSellOrderItemId(item.SellOrderItemId),
                        VendorId = !string.IsNullOrWhiteSpace(item.VendorId) ? item.VendorId.Trim() : order.VendorId,
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
                    poItem.ConvertPrice = ExchangeRateToUsdConverter.UnitLocalToUsd(
                        poItem.Cost, poItem.Currency, fx.UsdToCny, fx.UsdToHkd, fx.UsdToEur);
                    await _poItemRepo.AddAsync(poItem);
                    newLines.Add(poItem);
                    await AddPurchaseOrderItemExtendAsync(poItem);
                    total += item.Qty * item.Cost;
                    if (!string.IsNullOrWhiteSpace(poItem.SellOrderItemId))
                        recalcSellLineIds.Add(poItem.SellOrderItemId.Trim());
                }
                var distinctNewLineCurrencies = newLines.Select(l => l.Currency).Distinct().ToList();
                if (distinctNewLineCurrencies.Count == 1)
                    order.Currency = distinctNewLineCurrencies[0];
                order.Total = total;
                order.ConvertTotal = total;
                order.ItemRows = request.Items.Count;
                replacedItemCount = request.Items.Count;
                order.Type = ResolvePurchaseOrderHeaderType(request.Type ?? order.Type, request.Items);
            }
            else if (request.Type.HasValue)
            {
                var existingLines = (await _poItemRepo.GetAllAsync()).Where(i => i.PurchaseOrderId == id).ToList();
                var hasSell = existingLines.Any(i => IsLinkedSellOrderPurchaseLine(i.SellOrderItemId));
                order.Type = hasSell
                    ? (short)1
                    : ResolvePurchaseOrderHeaderType(request.Type.Value, Array.Empty<CreatePurchaseOrderItemRequest>());
            }

            order.ModifyTime = DateTime.UtcNow;
            order.ModifyByUserId = NormalizeActingUserId(actingUserId);
            await _poRepo.UpdateAsync(order);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

            foreach (var sid in recalcSellLineIds)
                await _sellOrderItemExtendSync.RecalculateAsync(sid);
            await RecalculatePurchaseRequisitionBySellLinesAsync(recalcSellLineIds);

            if (newLines != null)
            {
                foreach (var line in newLines)
                    await _poItemExtendSync.RecalculateAsync(line.Id);
            }

            if (replacedItemCount > 0 && newLines != null)
            {
                var t = DateTime.UtcNow;
                await _orderJourneyLog.AppendAsync(new OrderJourneyLog
                {
                    EntityKind = OrderJourneyEntityKinds.PurchaseOrder,
                    EntityId = order.Id,
                    DocumentCode = order.PurchaseOrderCode,
                    EventCode = OrderJourneyEventCodes.PoUpdated,
                    EventTime = t,
                    Amount = order.Total,
                    Currency = order.Currency,
                    PayloadJson = $"{{\"itemRows\":{replacedItemCount}}}",
                    ActorKind = OrderJourneyActorKinds.System,
                    Source = nameof(PurchaseOrderService)
                });
                foreach (var line in newLines)
                {
                    var lineTotal = Math.Round(line.Qty * line.Cost, 2, MidpointRounding.AwayFromZero);
                    await _orderJourneyLog.AppendAsync(new OrderJourneyLog
                    {
                        EntityKind = OrderJourneyEntityKinds.PurchaseOrderItem,
                        EntityId = line.Id,
                        ParentEntityKind = OrderJourneyEntityKinds.PurchaseOrder,
                        ParentEntityId = order.Id,
                        DocumentCode = order.PurchaseOrderCode,
                        LineHint = JourneyLineHint(line.PN, line.Brand),
                        EventCode = OrderJourneyEventCodes.PoItemCreated,
                        EventTime = t,
                        Quantity = line.Qty,
                        Amount = lineTotal,
                        Currency = line.Currency,
                        RelatedEntityKind = OrderJourneyEntityKinds.SellOrderItem,
                        RelatedEntityId = line.SellOrderItemId,
                        ActorKind = OrderJourneyActorKinds.System,
                        Source = nameof(PurchaseOrderService)
                    });
                }
            }

            return order;
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var po = await _poRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"采购订单 {id} 不存在");
            await _orderJourneyLog.AppendAsync(new OrderJourneyLog
            {
                EntityKind = OrderJourneyEntityKinds.PurchaseOrder,
                EntityId = po.Id,
                DocumentCode = po.PurchaseOrderCode,
                EventCode = OrderJourneyEventCodes.PoDeleted,
                EventTime = DateTime.UtcNow,
                ActorKind = OrderJourneyActorKinds.System,
                Source = nameof(PurchaseOrderService)
            });
            var items = await _poItemRepo.GetAllAsync();
            var recalcAfterDelete = items
                .Where(i => i.PurchaseOrderId == id && IsLinkedSellOrderPurchaseLine(i.SellOrderItemId))
                .Select(i => i.SellOrderItemId!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var item in items.Where(i => i.PurchaseOrderId == id))
            {
                await _poItemExtendRepo.DeleteAsync(item.Id);
                await _poItemRepo.DeleteAsync(item.Id);
            }
            await _poRepo.DeleteAsync(id);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            foreach (var sid in recalcAfterDelete)
                await _sellOrderItemExtendSync.RecalculateAsync(sid);
            await RecalculatePurchaseRequisitionBySellLinesAsync(recalcAfterDelete);
        }

        public async Task<PurchaseOrderItemExtendRefreshResult> RefreshItemExtendsAsync(string purchaseOrderId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(purchaseOrderId))
                throw new ArgumentException("采购订单ID不能为空", nameof(purchaseOrderId));

            var orderId = purchaseOrderId.Trim();
            var order = await _poRepo.GetByIdAsync(orderId)
                ?? throw new InvalidOperationException($"采购订单 {orderId} 不存在");

            var items = (await _poItemRepo.FindAsync(x => x.PurchaseOrderId == orderId)).ToList();
            var result = new PurchaseOrderItemExtendRefreshResult
            {
                PurchaseOrderId = orderId,
                TotalItems = items.Count,
                RefreshedAt = DateTime.UtcNow
            };
            var poLineIds = items.Select(x => x.Id).ToList();
            var beforeArrivalStatus =
                await LoadArrivalNoticeStatusMapByPoLineIdsAsync(poLineIds);

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var before = await BuildRefreshSnapshotAsync(item.Id);
                await _poItemExtendSync.RecalculateAsync(item.Id, cancellationToken);
                var after = await BuildRefreshSnapshotAsync(item.Id);
                var fields = BuildFieldChanges(before, after);
                if (fields.Count == 0) continue;

                result.Changes.Add(new PurchaseOrderItemExtendChangeDto
                {
                    PurchaseOrderItemId = item.Id,
                    PurchaseOrderItemCode = item.PurchaseOrderItemCode,
                    Fields = fields
                });
                result.ChangedFieldsCount += fields.Count;
            }

            var refreshSellLineIds = items
                .Select(i => i.SellOrderItemId)
                .Where(s => IsLinkedSellOrderPurchaseLine(s))
                .Select(s => s!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            result.SyncedPurchaseRequisitionStatusCount =
                await RecalculatePurchaseRequisitionBySellLinesAsync(refreshSellLineIds);
            var afterArrivalStatus =
                await LoadArrivalNoticeStatusMapByPoLineIdsAsync(poLineIds);
            result.SyncedArrivalNoticeStatusCount =
                CountArrivalNoticeStatusChanges(beforeArrivalStatus, afterArrivalStatus);

            result.ChangedItems = result.Changes.Count;
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "PO明细扩展刷新完成: PurchaseOrderId={PurchaseOrderId} Code={Code} TotalItems={TotalItems} ChangedItems={ChangedItems} ChangedFields={ChangedFields}",
                orderId, order.PurchaseOrderCode, result.TotalItems, result.ChangedItems, result.ChangedFieldsCount);

            return result;
        }

        public async Task UpdateStatusAsync(string id, short status, string? actingUserId = null)
        {
            var order = await _poRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"采购订单 {id} 不存在");

            var fromStatus = order.Status;
            ValidateStatusTransition(order.Status, status);
            order.Status = status;
            order.ModifyTime = DateTime.UtcNow;
            order.ModifyByUserId = NormalizeActingUserId(actingUserId);
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

            string? remark = null;
            if (status == StatusConfirmed)
                remark = "采购订单已确认";

            await _orderJourneyLog.AppendAsync(new OrderJourneyLog
            {
                EntityKind = OrderJourneyEntityKinds.PurchaseOrder,
                EntityId = order.Id,
                DocumentCode = order.PurchaseOrderCode,
                EventCode = OrderJourneyEventCodes.PoStatusChanged,
                FromState = fromStatus.ToString(),
                ToState = status.ToString(),
                EventTime = DateTime.UtcNow,
                Remark = remark,
                ActorKind = OrderJourneyActorKinds.System,
                Source = nameof(PurchaseOrderService)
            });

            var statusSyncItems = await _poItemRepo.FindAsync(i => i.PurchaseOrderId == id);
            var statusSyncSellLineIds = statusSyncItems
                         .Select(i => i.SellOrderItemId)
                         .Where(s => IsLinkedSellOrderPurchaseLine(s))
                         .Select(s => s!.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .ToList();
            foreach (var sid in statusSyncSellLineIds)
                await _sellOrderItemExtendSync.RecalculateAsync(sid);
            await RecalculatePurchaseRequisitionBySellLinesAsync(statusSyncSellLineIds);

            foreach (var line in statusSyncItems)
                await _poItemExtendSync.RecalculateAsync(line.Id);
        }

        private static bool ShouldSyncOrderAndItemStatus(short status)
        {
            return status is StatusNew or StatusPendingAudit or StatusApproved or StatusPendingConfirm or StatusConfirmed;
        }

        private async Task<PoItemExtendRefreshSnapshot?> BuildRefreshSnapshotAsync(string purchaseOrderItemId)
        {
            var ext = await _poItemExtendRepo.GetByIdAsync(purchaseOrderItemId);
            if (ext == null) return null;
            return new PoItemExtendRefreshSnapshot
            {
                PurchaseProgressStatus = ext.PurchaseProgressStatus,
                StockInProgressStatus = ext.StockInProgressStatus,
                PaymentProgressStatus = ext.PaymentProgressStatus,
                InvoiceProgressStatus = ext.InvoiceProgressStatus,
                PurchaseProgressQty = ext.PurchaseProgressQty,
                QtyReceiveTotal = ext.QtyReceiveTotal,
                PaymentAmountRequested = ext.PaymentAmountRequested,
                PaymentAmountFinish = ext.PaymentAmountFinish,
                PaymentAmountNot = ext.PaymentAmountNot,
                PurchaseInvoiceDone = ext.PurchaseInvoiceDone,
                PurchaseInvoiceToBe = ext.PurchaseInvoiceToBe,
                QtyStockInNotifyNot = ext.QtyStockInNotifyNot,
                QtyStockInNotifyExpectSum = ext.QtyStockInNotifyExpectSum
            };
        }

        private static List<PurchaseOrderItemExtendFieldChangeDto> BuildFieldChanges(PoItemExtendRefreshSnapshot? before, PoItemExtendRefreshSnapshot? after)
        {
            var changes = new List<PurchaseOrderItemExtendFieldChangeDto>();
            AddShortField(changes, "purchaseProgressStatus", "采购状态", before?.PurchaseProgressStatus, after?.PurchaseProgressStatus);
            AddShortField(changes, "stockInProgressStatus", "入库状态", before?.StockInProgressStatus, after?.StockInProgressStatus);
            AddShortField(changes, "paymentProgressStatus", "付款状态", before?.PaymentProgressStatus, after?.PaymentProgressStatus);
            AddShortField(changes, "invoiceProgressStatus", "开票状态", before?.InvoiceProgressStatus, after?.InvoiceProgressStatus);

            AddDecimalField(changes, "purchaseProgressQty", "采购数量", before?.PurchaseProgressQty, after?.PurchaseProgressQty, 4);
            AddDecimalField(changes, "qtyReceiveTotal", "入库数量", before?.QtyReceiveTotal, after?.QtyReceiveTotal, 4);
            AddDecimalField(changes, "paymentAmountRequested", "请款金额", before?.PaymentAmountRequested, after?.PaymentAmountRequested, 2);
            AddDecimalField(changes, "paymentAmountFinish", "已付款金额", before?.PaymentAmountFinish, after?.PaymentAmountFinish, 2);
            AddDecimalField(changes, "paymentAmountNot", "待付款金额", before?.PaymentAmountNot, after?.PaymentAmountNot, 2);
            AddDecimalField(changes, "purchaseInvoiceDone", "已开票金额", before?.PurchaseInvoiceDone, after?.PurchaseInvoiceDone, 2);
            AddDecimalField(changes, "purchaseInvoiceToBe", "待开票金额", before?.PurchaseInvoiceToBe, after?.PurchaseInvoiceToBe, 2);
            AddDecimalField(changes, "qtyStockInNotifyNot", "待通知到货数量", before?.QtyStockInNotifyNot, after?.QtyStockInNotifyNot, 4);
            AddDecimalField(changes, "qtyStockInNotifyExpectSum", "通知到货累计预期数量", before?.QtyStockInNotifyExpectSum, after?.QtyStockInNotifyExpectSum, 4);
            return changes;
        }

        private static void AddShortField(List<PurchaseOrderItemExtendFieldChangeDto> changes, string field, string label, short? before, short? after)
        {
            var b = before ?? 0;
            var a = after ?? 0;
            if (b == a) return;
            changes.Add(new PurchaseOrderItemExtendFieldChangeDto
            {
                Field = field,
                Label = label,
                Before = b.ToString(),
                After = a.ToString()
            });
        }

        private static void AddDecimalField(List<PurchaseOrderItemExtendFieldChangeDto> changes, string field, string label, decimal? before, decimal? after, int digits)
        {
            var b = decimal.Round(before ?? 0m, digits, MidpointRounding.AwayFromZero);
            var a = decimal.Round(after ?? 0m, digits, MidpointRounding.AwayFromZero);
            if (b == a) return;
            changes.Add(new PurchaseOrderItemExtendFieldChangeDto
            {
                Field = field,
                Label = label,
                Before = b.ToString($"F{digits}"),
                After = a.ToString($"F{digits}")
            });
        }

        private sealed class PoItemExtendRefreshSnapshot
        {
            public short PurchaseProgressStatus { get; set; }
            public short StockInProgressStatus { get; set; }
            public short PaymentProgressStatus { get; set; }
            public short InvoiceProgressStatus { get; set; }
            public decimal PurchaseProgressQty { get; set; }
            public decimal QtyReceiveTotal { get; set; }
            public decimal PaymentAmountRequested { get; set; }
            public decimal PaymentAmountFinish { get; set; }
            public decimal PaymentAmountNot { get; set; }
            public decimal PurchaseInvoiceDone { get; set; }
            public decimal PurchaseInvoiceToBe { get; set; }
            public decimal QtyStockInNotifyNot { get; set; }
            public decimal QtyStockInNotifyExpectSum { get; set; }
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
