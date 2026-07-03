using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Models.System;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Vendor;
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
        private const short ItemStatusPaid = 40;
        private const short ItemStatusShipped = 50;
        private const short ItemStatusStockedIn = 60;

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
        private readonly IUserService? _userService;
        private readonly ILogOperationAppendService? _logOperationAppend;
        private readonly ILogger<PurchaseOrderService> _logger;
        private readonly IPurchaseOrderListQuery _purchaseOrderListQuery;
        private readonly IRepository<VendorInfo>? _vendorRepo;

        public PurchaseOrderService(
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IRepository<PurchaseOrderItemExtend> poItemExtendRepo,
            IRepository<PurchaseRequisition>? prRepo,
            IRepository<StockInNotify>? notifyRepo,
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IDataPermissionService dataPermissionService,
            IPurchaseOrderListQuery purchaseOrderListQuery,
            ISerialNumberService serialNumberService,
            IFinanceExchangeRateService financeExchangeRateService,
            IOrderJourneyLogService orderJourneyLog,
            ISellOrderItemExtendSyncService sellOrderItemExtendSync,
            IPurchaseRequisitionService? purchaseRequisitionService,
            IPurchaseOrderItemExtendSyncService poItemExtendSync,
            IPurchaseOrderExtendLineSeqService poLineSeq,
            ILogger<PurchaseOrderService> logger,
            IUserService? userService = null,
            ILogOperationAppendService? logOperationAppend = null,
            IUnitOfWork? unitOfWork = null,
            IRepository<VendorInfo>? vendorRepo = null)
        {
            _poRepo = poRepo;
            _poItemRepo = poItemRepo;
            _poItemExtendRepo = poItemExtendRepo;
            _prRepo = prRepo;
            _notifyRepo = notifyRepo;
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _dataPermissionService = dataPermissionService;
            _purchaseOrderListQuery = purchaseOrderListQuery;
            _serialNumberService = serialNumberService;
            _financeExchangeRateService = financeExchangeRateService;
            _orderJourneyLog = orderJourneyLog;
            _sellOrderItemExtendSync = sellOrderItemExtendSync;
            _purchaseRequisitionService = purchaseRequisitionService;
            _poItemExtendSync = poItemExtendSync;
            _poLineSeq = poLineSeq;
            _userService = userService;
            _logOperationAppend = logOperationAppend;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _vendorRepo = vendorRepo;
        }

        // 兼容旧调用方（单测/临时构造）：不注入采购申请回写依赖时，状态回写能力自动降级为 no-op。
        public PurchaseOrderService(
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IRepository<PurchaseOrderItemExtend> poItemExtendRepo,
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IDataPermissionService dataPermissionService,
            IPurchaseOrderListQuery purchaseOrderListQuery,
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
                purchaseOrderListQuery,
                serialNumberService,
                financeExchangeRateService,
                orderJourneyLog,
                sellOrderItemExtendSync,
                null,
                poItemExtendSync,
                poLineSeq,
                logger,
                unitOfWork: unitOfWork)
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

        private static string? NormalizeOptionalUserId(string? userId) =>
            string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();

        private static bool IsLinkedSellOrderPurchaseLine(string? sellOrderItemId) =>
            PurchaseOrderItemLinkRules.IsLinkedSellOrderLine(sellOrderItemId);

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

        private static short ResolvePurchaseOrderHeaderType(short requestedType, IEnumerable<CreatePurchaseOrderItemRequest> items) =>
            PurchaseOrderItemLinkRules.ResolveHeaderType(
                requestedType,
                items.Select(i => i.SellOrderItemId));

        public async Task<PurchaseOrder> CreateAsync(CreatePurchaseOrderRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.VendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(request.VendorId));
            if (!request.Items.Any())
                throw new ArgumentException("至少需要一条明细行", nameof(request.Items));

            var purchaseOrderCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.PurchaseOrder);

            var total = request.Items.Sum(item => item.Qty * item.Cost);
            var headerType = ResolvePurchaseOrderHeaderType(request.Type, request.Items);
            PurchaseOrderItemLinkRules.ValidateCustomerOrderItems(
                headerType,
                request.Items.Select(i => i.SellOrderItemId).ToList());
            var distinctLineCurrencies = request.Items.Select(i => i.Currency).Distinct().ToList();
            var headerCurrency = distinctLineCurrencies.Count == 1 ? distinctLineCurrencies[0] : request.Currency;
            var fx = await _financeExchangeRateService.GetCurrentAsync();
            var convertTotalUsd = request.Items.Sum(item =>
            {
                var convertPrice = ExchangeRateToUsdConverter.UnitLocalToUsd(
                    item.Cost, item.Currency, fx.UsdToCny, fx.UsdToHkd, fx.UsdToEur);
                return ExchangeRateToUsdConverter.LineAmountUsd(item.Qty, convertPrice);
            });

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
                Assistor = NormalizeOptionalUserId(request.Assistor),
                Type = headerType,
                Currency = headerCurrency,
                DeliveryDate = PostgreSqlDateTime.ToUtc(request.DeliveryDate),
                DeliveryAddress = request.DeliveryAddress,
                Comment = request.Comment,
                InnerComment = request.InnerComment,
                Status = StatusNew,
                ItemRows = request.Items.Count,
                Total = total,
                ConvertTotal = convertTotalUsd,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = NormalizeActingUserId(actingUserId)
            };
            await _poRepo.AddAsync(order);
            if (_unitOfWork != null)
            {
                _logger.LogInformation("PO CreateAsync SaveChanges(主表): OrderId={OrderId}", order.Id);
                await _unitOfWork.SaveChangesAsync();
            }

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
                    DateCode = NormalizeDateCode(item.DateCode),
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
            order.Items = items.Where(i => !i.IsDeleted).ToList();
            return order;
        }

        public async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
        {
            return await _poRepo.GetAllAsync();
        }

        public Task<PagedResult<PurchaseOrder>> GetPagedAsync(PurchaseOrderQueryRequest request) =>
            _purchaseOrderListQuery.GetPagedAsync(request, CancellationToken.None);

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

            var headerBefore = CapturePurchaseOrderHeaderSnapshot(order);
            if (request.VendorName != null) order.VendorName = request.VendorName;
            if (request.PurchaseUserId != null) order.PurchaseUserId = request.PurchaseUserId;
            if (request.PurchaseUserName != null) order.PurchaseUserName = request.PurchaseUserName;
            if (request.Assistor != null) order.Assistor = NormalizeOptionalUserId(request.Assistor);
            if (request.Currency.HasValue) order.Currency = request.Currency.Value;
            if (request.DeliveryDate.HasValue) order.DeliveryDate = PostgreSqlDateTime.ToUtc(request.DeliveryDate.Value);
            if (request.DeliveryAddress != null) order.DeliveryAddress = request.DeliveryAddress;
            if (request.Comment != null) order.Comment = request.Comment;
            if (request.InnerComment != null) order.InnerComment = request.InnerComment;

            var replacedItemCount = 0;
            List<PurchaseOrderItem>? insertedLines = null;
            List<PurchaseOrderItem>? updatedLines = null;
            List<PurchaseOrderItem>? deletedLines = null;
            var recalcSellLineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            PurchaseOrderItemSyncResult? syncResult = null;
            if (request.Items != null && request.Items.Count > 0)
            {
                syncResult = await SyncPurchaseOrderItemsOnUpdateAsync(order, id, request.Items, actingUserId, recalcSellLineIds);
                insertedLines = syncResult.Inserted;
                updatedLines = syncResult.Updated;
                deletedLines = syncResult.Deleted;
                var activeLines = request.Items.Count;
                var distinctCurrencies = (insertedLines ?? new List<PurchaseOrderItem>())
                    .Concat(updatedLines ?? new List<PurchaseOrderItem>())
                    .Select(l => l.Currency)
                    .Distinct()
                    .ToList();
                if (distinctCurrencies.Count == 1)
                    order.Currency = distinctCurrencies[0];
                order.Total = syncResult.Total;
                order.ConvertTotal = syncResult.ConvertTotal;
                order.ItemRows = activeLines;
                replacedItemCount = activeLines;
                order.Type = ResolvePurchaseOrderHeaderType(request.Type ?? order.Type, request.Items);
                PurchaseOrderItemLinkRules.ValidateCustomerOrderItems(
                    order.Type,
                    request.Items.Select(i => i.SellOrderItemId).ToList());
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

            await LogPurchaseOrderHeaderChangesAsync(order, headerBefore, actingUserId);

            if (syncResult != null)
            {
                foreach (var audit in syncResult.ItemUpdateAudits)
                    await LogPurchaseOrderItemFieldChangesAsync(audit.Item, audit.Before, actingUserId);
                foreach (var ins in syncResult.Inserted)
                    await LogPurchaseOrderItemAddedAsync(ins, actingUserId);
            }

            if (deletedLines is { Count: > 0 })
            {
                await LogPurchaseOrderItemsDeletedAsync(
                    order,
                    deletedLines,
                    actingUserId,
                    OperationLogActionTypes.PurchaseOrderItemDelete,
                    $"编辑采购订单 {order.PurchaseOrderCode} 时删除明细行");
            }

            foreach (var sid in recalcSellLineIds)
                await _sellOrderItemExtendSync.RecalculateAsync(sid);
            await RecalculatePurchaseRequisitionBySellLinesAsync(recalcSellLineIds);

            var touchedPoLines = new List<PurchaseOrderItem>();
            if (insertedLines != null) touchedPoLines.AddRange(insertedLines);
            if (updatedLines != null) touchedPoLines.AddRange(updatedLines);
            if (touchedPoLines.Count > 0)
            {
                foreach (var line in touchedPoLines)
                    await _poItemExtendSync.RecalculateAsync(line.Id);
            }

            if (replacedItemCount > 0 && insertedLines is { Count: > 0 })
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
                foreach (var line in insertedLines)
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

        public async Task DeleteAsync(string id, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var po = await _poRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"采购订单 {id} 不存在");

            var (actorId, actorName) = await ResolveActorAsync(actingUserId);
            var itemsToDelete = (await _poItemRepo.GetAllAsync())
                .Where(i => i.PurchaseOrderId == id)
                .ToList();
            var recalcAfterDelete = itemsToDelete
                .Where(i => IsLinkedSellOrderPurchaseLine(i.SellOrderItemId))
                .Select(i => i.SellOrderItemId!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var item in itemsToDelete)
            {
                item.IsDeleted = true;
                item.ModifyTime = DateTime.UtcNow;
                item.DeletedByUserId = actorId;
                item.DeletedByUserName = actorName;
                await _poItemRepo.UpdateAsync(item);
                await _poItemExtendRepo.DeleteAsync(item.Id);
            }

            await SoftDeletePurchaseOrderExtendAsync(id);

            po.IsDeleted = true;
            po.ModifyTime = DateTime.UtcNow;
            po.ModifyByUserId = NormalizeActingUserId(actingUserId);
            await _poRepo.UpdateAsync(po);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

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

            await LogPurchaseOrderWholeDeleteOperationLogsAsync(po, itemsToDelete, actingUserId);

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
                var beforeItemStatus = item.Status;
                var before = await BuildRefreshSnapshotAsync(item.Id);
                await _poItemExtendSync.RecalculateAsync(item.Id, cancellationToken);
                var after = await BuildRefreshSnapshotAsync(item.Id);
                var fields = BuildFieldChanges(before, after);

                var targetItemStatus = ComputeItemStatusAfterRefresh(item, after);
                if (targetItemStatus != beforeItemStatus)
                {
                    item.Status = targetItemStatus;
                    item.ModifyTime = DateTime.UtcNow;
                    await _poItemRepo.UpdateAsync(item);
                    fields.Add(new PurchaseOrderItemExtendFieldChangeDto
                    {
                        Field = "status",
                        Label = "明细状态",
                        Before = beforeItemStatus.ToString(),
                        After = targetItemStatus.ToString()
                    });
                    // 扩展里「采购进度状态」等仍按本行 status 推导；主状态下调后需再算一遍，避免 extend 与主状态长期不一致
                    await _poItemExtendSync.RecalculateAsync(item.Id, cancellationToken);
                    var afterStatusFix = await BuildRefreshSnapshotAsync(item.Id);
                    fields.AddRange(BuildFieldChanges(after, afterStatusFix));
                }
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

            var targetOrderStatus = ComputeOrderStatusAfterRefresh(order, items);
            if (targetOrderStatus != order.Status)
            {
                result.ChangedFieldsCount += 1;
                order.Status = targetOrderStatus;
                order.ModifyTime = DateTime.UtcNow;
                await _poRepo.UpdateAsync(order);
            }

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "PO明细扩展刷新完成: PurchaseOrderId={PurchaseOrderId} Code={Code} TotalItems={TotalItems} ChangedItems={ChangedItems} ChangedFields={ChangedFields}",
                orderId, order.PurchaseOrderCode, result.TotalItems, result.ChangedItems, result.ChangedFieldsCount);

            return result;
        }

        public async Task<PurchaseOrderVendorNameRefreshResult> RefreshVendorNameAsync(string purchaseOrderId, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(purchaseOrderId))
                throw new ArgumentException("采购订单ID不能为空", nameof(purchaseOrderId));
            if (_vendorRepo == null)
                throw new InvalidOperationException("供应商仓储未配置，无法刷新供应商名称");

            var orderId = purchaseOrderId.Trim();
            var order = await _poRepo.GetByIdAsync(orderId)
                ?? throw new InvalidOperationException($"采购订单 {orderId} 不存在");

            var vendorId = order.VendorId?.Trim();
            if (string.IsNullOrEmpty(vendorId)
                || vendorId.Equals("PENDING", StringComparison.OrdinalIgnoreCase)
                || vendorId.Equals(MANUAL_VENDOR_PLACEHOLDER_ID, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("采购订单供应商无效，无法刷新名称");

            var vendor = await _vendorRepo.GetByIdAsync(vendorId);
            if (vendor == null)
                throw new InvalidOperationException($"供应商 {vendorId} 不存在");

            var newName = FormatVendorDisplayName(vendor);
            if (string.IsNullOrWhiteSpace(newName))
                throw new InvalidOperationException("供应商主数据无可用名称");

            var oldName = order.VendorName;
            var headerBefore = CapturePurchaseOrderHeaderSnapshot(order);
            order.VendorName = newName;
            order.ModifyTime = DateTime.UtcNow;
            order.ModifyByUserId = NormalizeActingUserId(actingUserId);
            await _poRepo.UpdateAsync(order);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            await LogPurchaseOrderHeaderChangesAsync(order, headerBefore, actingUserId);

            var changed = !string.Equals(
                string.IsNullOrWhiteSpace(oldName) ? null : oldName.Trim(),
                newName,
                StringComparison.Ordinal);

            _logger.LogInformation(
                "PO供应商名称刷新: PurchaseOrderId={PurchaseOrderId} Code={Code} VendorId={VendorId} Changed={Changed} Old={OldName} New={NewName}",
                orderId, order.PurchaseOrderCode, vendorId, changed, oldName ?? "(null)", newName);

            return new PurchaseOrderVendorNameRefreshResult
            {
                PurchaseOrderId = orderId,
                VendorId = vendorId,
                OldVendorName = oldName,
                NewVendorName = newName,
                Changed = changed
            };
        }

        private static string? FormatVendorDisplayName(VendorInfo vendor)
        {
            if (!string.IsNullOrWhiteSpace(vendor.OfficialName)) return vendor.OfficialName.Trim();
            if (!string.IsNullOrWhiteSpace(vendor.NickName)) return vendor.NickName.Trim();
            return string.IsNullOrWhiteSpace(vendor.Code) ? null : vendor.Code.Trim();
        }

        private const string MANUAL_VENDOR_PLACEHOLDER_ID = "00000000-0000-0000-0000-000000000002";

        private static short ComputeItemStatusAfterRefresh(PurchaseOrderItem item, PoItemExtendRefreshSnapshot? after)
        {
            var current = item.Status;
            if (current is StatusCancelled or StatusAuditFailed)
                return current;
            var next = current;
            // 与扩展表/行上事实一致：先按里程碑上调（原逻辑）
            if ((after?.PaymentProgressStatus ?? 0) >= 2 && next < ItemStatusPaid)
                next = ItemStatusPaid;
            if (item.StockOutStatus >= 2 && next < ItemStatusShipped)
                next = ItemStatusShipped;
            if ((after?.StockInProgressStatus ?? 0) >= 2 && next < ItemStatusStockedIn)
                next = ItemStatusStockedIn;

            // 刷新时允许下调：主状态曾被人为/历史写高，但扩展已按单据重算为「未达该里程碑」时应对齐（否则出现「主状态=已入库」与「入库状态=待入库」并存）
            if ((after?.StockInProgressStatus ?? 0) < 2 && next >= ItemStatusStockedIn)
            {
                var cap = StatusConfirmed;
                if ((after?.PaymentProgressStatus ?? 0) >= 2)
                    cap = ItemStatusPaid;
                if (item.StockOutStatus >= 2)
                    cap = ItemStatusShipped;
                next = Math.Min(next, cap);
            }

            if (item.StockOutStatus < 2 && next >= ItemStatusShipped)
            {
                var cap = StatusConfirmed;
                if ((after?.PaymentProgressStatus ?? 0) >= 2)
                    cap = ItemStatusPaid;
                next = Math.Min(next, cap);
            }

            if ((after?.PaymentProgressStatus ?? 0) < 2 && next >= ItemStatusPaid)
                next = Math.Min(next, StatusConfirmed);

            return next;
        }

        private static short ComputeOrderStatusAfterRefresh(PurchaseOrder order, IReadOnlyList<PurchaseOrderItem> items)
        {
            if (order.Status is StatusCancelled or StatusAuditFailed)
                return order.Status;
            var activeItems = items.Where(i => i.Status != StatusCancelled).ToList();
            if (activeItems.Count == 0)
                return order.Status;
            var next = order.Status;
            if (next < StatusInProgress && activeItems.All(i => i.Status >= ItemStatusShipped))
                next = StatusInProgress;
            if (next < StatusCompleted && activeItems.All(i => i.Status >= StatusCompleted))
                next = StatusCompleted;
            return next;
        }

        public async Task UpdateStatusAsync(string id, short status, string? actingUserId = null)
        {
            var order = await _poRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"采购订单 {id} 不存在");

            var fromStatus = order.Status;
            var statusBefore = order.Status;
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

            if (statusBefore != status)
            {
                await AddPurchaseOrderFieldChangeLogAsync(
                    order,
                    "status",
                    "订单状态",
                    FormatPurchaseOrderStatus(statusBefore),
                    FormatPurchaseOrderStatus(status),
                    actingUserId);
            }

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

        public async Task<PurchaseOrder> UpdateFreightForwarderOrderNoAsync(
            string purchaseOrderId,
            string? freightForwarderOrderNo,
            string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(purchaseOrderId))
                throw new ArgumentException("采购订单 Id 不能为空", nameof(purchaseOrderId));

            var order = await _poRepo.GetByIdAsync(purchaseOrderId.Trim())
                ?? throw new InvalidOperationException("采购订单不存在");

            if (!PurchaseOrderFreightForwarderOrderNoRules.IsEditableStatus(order.Status))
                throw new InvalidOperationException("当前采购订单状态不允许录入或修改货代单号");

            var normalized = PurchaseOrderFreightForwarderOrderNoRules.Normalize(freightForwarderOrderNo);
            if (normalized != null && normalized.Length > PurchaseOrderFreightForwarderOrderNoRules.MaxLength)
                throw new ArgumentException($"货代单号长度不能超过 {PurchaseOrderFreightForwarderOrderNoRules.MaxLength} 个字符");

            var oldValue = PurchaseOrderFreightForwarderOrderNoRules.Normalize(order.FreightForwarderOrderNo);
            if (string.Equals(oldValue, normalized, StringComparison.OrdinalIgnoreCase))
                return order;

            if (!string.IsNullOrEmpty(normalized))
            {
                var dup = (await _poRepo.FindAsync(p =>
                        p.Id != order.Id &&
                        p.FreightForwarderOrderNo != null &&
                        p.FreightForwarderOrderNo.ToLower() == normalized.ToLower()))
                    .FirstOrDefault();
                if (dup != null)
                    throw new InvalidOperationException($"货代单号「{normalized}」已被采购订单 {dup.PurchaseOrderCode} 使用");
            }

            order.FreightForwarderOrderNo = normalized;
            order.ModifyByUserId = NormalizeActingUserId(actingUserId);
            order.ModifyTime = DateTime.UtcNow;
            await _poRepo.UpdateAsync(order);
            await _unitOfWork!.SaveChangesAsync();

            await CompareAndLogPoHeaderFieldAsync(
                order,
                oldValue,
                normalized,
                "freightForwarderOrderNo",
                "货代单号",
                actingUserId);

            return order;
        }

        public async Task<IReadOnlyList<PurchaseOrderFieldChangeLogDto>> GetFieldChangeLogsAsync(string purchaseOrderId)
        {
            if (_unitOfWork == null || string.IsNullOrWhiteSpace(purchaseOrderId))
                return Array.Empty<PurchaseOrderFieldChangeLogDto>();
            var safe = SqlQ(purchaseOrderId.Trim());
            var headerBiz = BusinessLogTypes.PurchaseOrder;
            var itemBiz = BusinessLogTypes.PurchaseOrderItem;
            var sql = $@"
SELECT c.""Id"",
       '{safe}' AS ""PurchaseOrderId"",
       po.purchase_order_code AS ""PurchaseOrderCode"",
       c.""FieldName"",
       c.""FieldLabel"",
       c.""OldValue"",
       c.""NewValue"",
       c.""ChangedByUserId"",
       c.""ChangedByUserName"",
       c.""ChangedAt"",
       CASE
         WHEN c.""BizType"" = '{headerBiz}' THEN '主表'
         ELSE COALESCE(NULLIF(TRIM(c.""RecordCode""), ''), '明细')
       END AS ""ObjectLabel""
FROM log_change_fldval c
LEFT JOIN purchaseorder po ON po.""PurchaseOrderId"" = '{safe}'
WHERE (
    c.""BizType"" = '{headerBiz}' AND c.""RecordId"" = '{safe}'
) OR (
    c.""BizType"" = '{itemBiz}' AND c.""RecordId"" IN (
        SELECT i.""PurchaseOrderItemId"" FROM purchaseorderitem i
        WHERE i.purchase_order_id = '{safe}'
    )
)
ORDER BY c.""ChangedAt"" DESC";
            var rows = await _unitOfWork.QueryAsync<PurchaseOrderFieldChangeLogDto>(sql);
            return rows.ToList();
        }

        public async Task<IReadOnlyList<PurchaseOrderDeletedItemLogDto>> GetDeletedOrderItemsAsync(string purchaseOrderId)
        {
            if (_unitOfWork == null || string.IsNullOrWhiteSpace(purchaseOrderId))
                return Array.Empty<PurchaseOrderDeletedItemLogDto>();
            var safe = SqlQ(purchaseOrderId.Trim());
            var itemBiz = BusinessLogTypes.PurchaseOrderItem;
            var sql = $@"
SELECT i.""PurchaseOrderItemId"",
       i.purchase_order_item_code AS ""PurchaseOrderItemCode"",
       i.pn AS ""PN"",
       i.brand AS ""Brand"",
       i.qty AS ""Qty"",
       i.cost AS ""Cost"",
       i.currency AS ""Currency"",
       i.comment AS ""Comment"",
       i.""CreateTime"",
       i.""ModifyTime"" AS ""DeletedAt"",
       COALESCE(
         NULLIF(TRIM(i.deleted_by_user_name), ''),
         NULLIF(TRIM(del_op.""OperatorUserName""), ''),
         NULLIF(TRIM(chg_near.""ChangedByUserName""), ''),
         NULLIF(TRIM(u.""UserName""), '')
       ) AS ""DeletedByUserName"",
       COALESCE(NULLIF(TRIM(i.deleted_by_user_id), ''), del_op.""OperatorUserId"") AS ""DeletedByUserId""
FROM purchaseorderitem i
INNER JOIN purchaseorder po ON po.""PurchaseOrderId"" = i.purchase_order_id
LEFT JOIN ""user"" u ON u.""UserId"" = po.modify_by_user_id
    AND po.""ModifyTime"" IS NOT NULL
    AND i.""ModifyTime"" IS NOT NULL
    AND ABS(EXTRACT(EPOCH FROM (i.""ModifyTime"" - po.""ModifyTime""))) <= 120
LEFT JOIN LATERAL (
    SELECT o.""OperatorUserId"", o.""OperatorUserName""
    FROM log_operation o
    WHERE o.""BizType"" = '{itemBiz}'
      AND o.""RecordId"" = i.""PurchaseOrderItemId""
      AND o.""ActionType"" IN ('{OperationLogActionTypes.PurchaseOrderItemDelete}', '{OperationLogActionTypes.PurchaseOrderItemDeleteWithOrder}')
    ORDER BY o.""OperationTime"" DESC
    LIMIT 1
) del_op ON true
LEFT JOIN LATERAL (
    SELECT c.""ChangedByUserName""
    FROM log_change_fldval c
    WHERE c.""BizType"" = '{BusinessLogTypes.PurchaseOrder}'
      AND c.""RecordId"" = po.""PurchaseOrderId""
      AND c.""ChangedByUserName"" IS NOT NULL
      AND TRIM(c.""ChangedByUserName"") <> ''
      AND c.""ChangedAt"" BETWEEN i.""ModifyTime"" - INTERVAL '30 minutes' AND i.""ModifyTime"" + INTERVAL '30 minutes'
    ORDER BY ABS(EXTRACT(EPOCH FROM (c.""ChangedAt"" - i.""ModifyTime"")))
    LIMIT 1
) chg_near ON true
WHERE i.purchase_order_id = '{safe}' AND i.is_deleted = true
ORDER BY i.""ModifyTime"" DESC NULLS LAST, i.""CreateTime"" DESC";
            var rows = await _unitOfWork.QueryAsync<PurchaseOrderDeletedItemLogDto>(sql);
            return rows.ToList();
        }

        private static string SqlQ(string? s) => (s ?? "").Replace("'", "''", StringComparison.Ordinal);

        private sealed record PurchaseOrderHeaderSnapshot(
            string? VendorName,
            string? PurchaseUserName,
            string? Assistor,
            short Currency,
            DateTime? DeliveryDate,
            string? DeliveryAddress,
            string? Comment,
            string? InnerComment,
            decimal Total,
            decimal ConvertTotal,
            short Type);

        private static PurchaseOrderHeaderSnapshot CapturePurchaseOrderHeaderSnapshot(PurchaseOrder order) =>
            new(
                order.VendorName,
                order.PurchaseUserName,
                order.Assistor,
                order.Currency,
                order.DeliveryDate,
                order.DeliveryAddress,
                order.Comment,
                order.InnerComment,
                order.Total,
                order.ConvertTotal,
                order.Type);

        private async Task LogPurchaseOrderHeaderChangesAsync(
            PurchaseOrder order,
            PurchaseOrderHeaderSnapshot before,
            string? actingUserId)
        {
            var after = CapturePurchaseOrderHeaderSnapshot(order);
            await CompareAndLogPoHeaderFieldAsync(order, before.VendorName, after.VendorName, "vendorName", "供应商", actingUserId);
            await CompareAndLogPoHeaderFieldAsync(order, before.PurchaseUserName, after.PurchaseUserName, "purchaseUserName", "采购员", actingUserId);
            await CompareAndLogPoAssistorFieldAsync(order, before.Assistor, after.Assistor, actingUserId);
            await CompareAndLogPoHeaderFieldAsync(order, FormatCurrency(before.Currency), FormatCurrency(after.Currency), "currency", "币别", actingUserId);
            await CompareAndLogPoHeaderFieldAsync(order, FormatDate(before.DeliveryDate), FormatDate(after.DeliveryDate), "deliveryDate", "交期", actingUserId);
            await CompareAndLogPoHeaderFieldAsync(order, before.DeliveryAddress, after.DeliveryAddress, "deliveryAddress", "送货地址", actingUserId);
            await CompareAndLogPoHeaderFieldAsync(order, before.Comment, after.Comment, "comment", "备注", actingUserId);
            await CompareAndLogPoHeaderFieldAsync(order, before.InnerComment, after.InnerComment, "innerComment", "内部备注", actingUserId);
            await CompareAndLogPoHeaderFieldAsync(order, FormatDecimal2(before.Total), FormatDecimal2(after.Total), "total", "订单总额", actingUserId);
            await CompareAndLogPoHeaderFieldAsync(order, FormatDecimal2(before.ConvertTotal), FormatDecimal2(after.ConvertTotal), "convertTotal", "折算总额(USD)", actingUserId);
            await CompareAndLogPoHeaderFieldAsync(order, FormatPurchaseOrderType(before.Type), FormatPurchaseOrderType(after.Type), "type", "订单类型", actingUserId);
        }

        private async Task CompareAndLogPoHeaderFieldAsync(
            PurchaseOrder order,
            string? oldVal,
            string? newVal,
            string fieldName,
            string fieldLabel,
            string? actingUserId)
        {
            var o = string.IsNullOrWhiteSpace(oldVal) ? null : oldVal.Trim();
            var n = string.IsNullOrWhiteSpace(newVal) ? null : newVal.Trim();
            if (string.Equals(o, n, StringComparison.Ordinal))
                return;
            await AddPurchaseOrderFieldChangeLogAsync(order, fieldName, fieldLabel, o, n, actingUserId);
        }

        private async Task CompareAndLogPoAssistorFieldAsync(
            PurchaseOrder order,
            string? oldId,
            string? newId,
            string? actingUserId)
        {
            var o = await ResolveUserDisplayNameAsync(oldId);
            var n = await ResolveUserDisplayNameAsync(newId);
            if (string.Equals(o, n, StringComparison.Ordinal))
                return;
            await AddPurchaseOrderFieldChangeLogAsync(order, "assistor", "采购助理", o, n, actingUserId);
        }

        private async Task<string?> ResolveUserDisplayNameAsync(string? userId)
        {
            var id = NormalizeActingUserId(userId);
            if (string.IsNullOrEmpty(id) || _userService == null)
                return string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
            var user = await _userService.GetByIdAsync(id);
            return string.IsNullOrWhiteSpace(user?.UserName) ? id : user!.UserName!.Trim();
        }

        private async Task AddPurchaseOrderFieldChangeLogAsync(
            PurchaseOrder order,
            string fieldName,
            string fieldLabel,
            string? oldValue,
            string? newValue,
            string? actingUserId)
        {
            if (_unitOfWork == null) return;
            var (userId, userName) = await ResolveActorAsync(actingUserId);
            var recordCodeSql = string.IsNullOrWhiteSpace(order.PurchaseOrderCode) ? "NULL" : $"'{SqlQ(order.PurchaseOrderCode)}'";
            var safeRecordId = SqlQ(order.Id);
            var safeField = SqlQ(fieldName);
            var safeLabel = SqlQ(fieldLabel);
            var oldSql = oldValue == null ? "NULL" : $"'{SqlQ(oldValue)}'";
            var newSql = newValue == null ? "NULL" : $"'{SqlQ(newValue)}'";
            var userIdSql = string.IsNullOrWhiteSpace(userId) ? "NULL" : $"'{SqlQ(userId)}'";
            var sql = $@"
INSERT INTO log_change_fldval (""Id"", ""BizType"", ""RecordId"", ""RecordCode"", ""FieldName"", ""FieldLabel"", ""OldValue"", ""NewValue"", ""ChangedAt"", ""ChangedByUserId"", ""ChangedByUserName"", ""ExtraInfo"", ""SysRemark"")
VALUES (gen_random_uuid()::text, '{BusinessLogTypes.PurchaseOrder}', '{safeRecordId}', {recordCodeSql}, '{safeField}', '{safeLabel}', {oldSql}, {newSql}, NOW(), {userIdSql}, '{SqlQ(userName)}', NULL, NULL)";
            await _unitOfWork.ExecuteAsync(sql);
        }

        private static PurchaseOrderItemFieldSnapshot CapturePurchaseOrderItemFieldSnapshot(PurchaseOrderItem item) =>
            new(
                item.SellOrderItemId,
                item.VendorId,
                item.ProductId,
                item.PN,
                item.Brand,
                item.Qty,
                item.Cost,
                item.Currency,
                item.DeliveryDate,
                item.DateCode,
                item.Comment,
                item.InnerComment);

        private async Task LogPurchaseOrderItemFieldChangesAsync(
            PurchaseOrderItem item,
            PurchaseOrderItemFieldSnapshot before,
            string? actingUserId)
        {
            var after = CapturePurchaseOrderItemFieldSnapshot(item);
            await CompareAndLogPoItemFieldAsync(item, before.Qty, after.Qty, "qty", "数量", FormatDecimal4, actingUserId);
            await CompareAndLogPoItemFieldAsync(item, before.Cost, after.Cost, "cost", "单价", FormatDecimal4, actingUserId);
            await CompareAndLogPoItemFieldAsync(item, FormatCurrency(before.Currency), FormatCurrency(after.Currency), "currency", "币别", actingUserId);
            await CompareAndLogPoItemFieldAsync(item, FormatDate(before.DeliveryDate), FormatDate(after.DeliveryDate), "deliveryDate", "交期", actingUserId);
            await CompareAndLogPoItemFieldAsync(item, before.DateCode, after.DateCode, "dateCode", "DC", actingUserId);
            await CompareAndLogPoItemFieldAsync(item, before.PN, after.PN, "pn", "物料型号", actingUserId);
            await CompareAndLogPoItemFieldAsync(item, before.Brand, after.Brand, "brand", "品牌", actingUserId);
            await CompareAndLogPoItemFieldAsync(item, before.Comment, after.Comment, "comment", "备注", actingUserId);
            await CompareAndLogPoItemFieldAsync(item, before.InnerComment, after.InnerComment, "innerComment", "内部备注", actingUserId);
            await CompareAndLogPoItemFieldAsync(item, before.SellOrderItemId, after.SellOrderItemId, "sellOrderItemId", "关联销售明细", actingUserId);
            await CompareAndLogPoItemFieldAsync(item, before.VendorId, after.VendorId, "vendorId", "行供应商", actingUserId);
            await CompareAndLogPoItemFieldAsync(item, before.ProductId, after.ProductId, "productId", "产品ID", actingUserId);
        }

        private async Task LogPurchaseOrderItemAddedAsync(PurchaseOrderItem item, string? actingUserId)
        {
            var summary =
                $"{item.PurchaseOrderItemCode} · {item.PN} · 数量 {FormatDecimal4(item.Qty)} · 单价 {FormatDecimal4(item.Cost)} {FormatCurrency(item.Currency)}";
            await AddPurchaseOrderItemFieldChangeLogAsync(item, "lineAdded", "新增明细", null, summary, actingUserId);
        }

        private async Task CompareAndLogPoItemFieldAsync(
            PurchaseOrderItem item,
            string? oldVal,
            string? newVal,
            string fieldName,
            string fieldLabel,
            string? actingUserId)
        {
            var o = string.IsNullOrWhiteSpace(oldVal) ? null : oldVal.Trim();
            var n = string.IsNullOrWhiteSpace(newVal) ? null : newVal.Trim();
            if (string.Equals(o, n, StringComparison.Ordinal))
                return;
            await AddPurchaseOrderItemFieldChangeLogAsync(item, fieldName, fieldLabel, o, n, actingUserId);
        }

        private async Task CompareAndLogPoItemFieldAsync(
            PurchaseOrderItem item,
            decimal oldVal,
            decimal newVal,
            string fieldName,
            string fieldLabel,
            Func<decimal, string> format,
            string? actingUserId)
        {
            if (oldVal == newVal)
                return;
            await AddPurchaseOrderItemFieldChangeLogAsync(item, fieldName, fieldLabel, format(oldVal), format(newVal), actingUserId);
        }

        private async Task AddPurchaseOrderItemFieldChangeLogAsync(
            PurchaseOrderItem item,
            string fieldName,
            string fieldLabel,
            string? oldValue,
            string? newValue,
            string? actingUserId)
        {
            if (_unitOfWork == null) return;
            var (userId, userName) = await ResolveActorAsync(actingUserId);
            var recordCode = string.IsNullOrWhiteSpace(item.PurchaseOrderItemCode) ? null : item.PurchaseOrderItemCode.Trim();
            var recordCodeSql = recordCode == null ? "NULL" : $"'{SqlQ(recordCode)}'";
            var sql = $@"
INSERT INTO log_change_fldval (""Id"", ""BizType"", ""RecordId"", ""RecordCode"", ""FieldName"", ""FieldLabel"", ""OldValue"", ""NewValue"", ""ChangedAt"", ""ChangedByUserId"", ""ChangedByUserName"", ""ExtraInfo"", ""SysRemark"")
VALUES (gen_random_uuid()::text, '{BusinessLogTypes.PurchaseOrderItem}', '{SqlQ(item.Id)}', {recordCodeSql}, '{SqlQ(fieldName)}', '{SqlQ(fieldLabel)}', {(oldValue == null ? "NULL" : $"'{SqlQ(oldValue)}'")}, {(newValue == null ? "NULL" : $"'{SqlQ(newValue)}'")}, NOW(), {(userId == null ? "NULL" : $"'{SqlQ(userId)}'")}, '{SqlQ(userName)}', NULL, NULL)";
            await _unitOfWork.ExecuteAsync(sql);
        }

        private sealed record PurchaseOrderItemFieldChangeAudit(PurchaseOrderItem Item, PurchaseOrderItemFieldSnapshot Before);

        private sealed record PurchaseOrderItemFieldSnapshot(
            string? SellOrderItemId,
            string? VendorId,
            string? ProductId,
            string? PN,
            string? Brand,
            decimal Qty,
            decimal Cost,
            short Currency,
            DateTime? DeliveryDate,
            string? DateCode,
            string? Comment,
            string? InnerComment);

        private sealed record PurchaseOrderItemSyncResult(
            List<PurchaseOrderItem> Inserted,
            List<PurchaseOrderItem> Updated,
            List<PurchaseOrderItem> Deleted,
            List<PurchaseOrderItemFieldChangeAudit> ItemUpdateAudits,
            decimal Total,
            decimal ConvertTotal);

        private async Task<PurchaseOrderItemSyncResult> SyncPurchaseOrderItemsOnUpdateAsync(
            PurchaseOrder order,
            string purchaseOrderId,
            List<CreatePurchaseOrderItemRequest> requestItems,
            string? actingUserId,
            HashSet<string> recalcSellLineIds)
        {
            var existingActive = (await _poItemRepo.FindAsync(i => i.PurchaseOrderId == purchaseOrderId))
                .Where(i => !i.IsDeleted)
                .ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);

            var fx = await _financeExchangeRateService.GetCurrentAsync();
            var inserted = new List<PurchaseOrderItem>();
            var updated = new List<PurchaseOrderItem>();
            var keptIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            decimal total = 0m;
            decimal convertTotalUsd = 0m;
            var newItemRequests = new List<CreatePurchaseOrderItemRequest>();
            var itemUpdateAudits = new List<PurchaseOrderItemFieldChangeAudit>();

            foreach (var itemReq in requestItems)
            {
                var reqId = itemReq.PurchaseOrderItemId?.Trim();
                if (!string.IsNullOrEmpty(reqId))
                {
                    if (!existingActive.TryGetValue(reqId, out var existing))
                        throw new InvalidOperationException($"采购订单明细 {reqId} 不存在或已删除");

                    keptIds.Add(reqId);
                    var prevSell = existing.SellOrderItemId;
                    var before = CapturePurchaseOrderItemFieldSnapshot(existing);
                    ApplyPurchaseOrderItemFromRequest(existing, itemReq, order, fx);
                    existing.ModifyTime = DateTime.UtcNow;
                    await _poItemRepo.UpdateAsync(existing);
                    updated.Add(existing);
                    itemUpdateAudits.Add(new PurchaseOrderItemFieldChangeAudit(existing, before));
                    total += existing.Qty * existing.Cost;
                    convertTotalUsd += ExchangeRateToUsdConverter.LineAmountUsd(existing.Qty, existing.ConvertPrice);
                    TrackSellLineForRecalc(recalcSellLineIds, existing.SellOrderItemId);
                    if (!string.Equals(prevSell, existing.SellOrderItemId, StringComparison.OrdinalIgnoreCase))
                        TrackSellLineForRecalc(recalcSellLineIds, prevSell);
                }
                else
                {
                    newItemRequests.Add(itemReq);
                }
            }

            if (newItemRequests.Count > 0)
            {
                var firstSeq = await _poLineSeq.ReserveNextSequenceBlockAsync(purchaseOrderId, newItemRequests.Count);
                var lineIndex = 0;
                foreach (var item in newItemRequests)
                {
                    var seq = firstSeq + lineIndex++;
                    var poItem = new PurchaseOrderItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        PurchaseOrderId = purchaseOrderId,
                        PurchaseOrderItemCode = OrderLineItemCodes.Purchase(order.PurchaseOrderCode, seq),
                        Status = ShouldSyncOrderAndItemStatus(order.Status) ? order.Status : StatusNew,
                        CreateTime = DateTime.UtcNow
                    };
                    ApplyPurchaseOrderItemFromRequest(poItem, item, order, fx);
                    await _poItemRepo.AddAsync(poItem);
                    inserted.Add(poItem);
                    await AddPurchaseOrderItemExtendAsync(poItem);
                    total += poItem.Qty * poItem.Cost;
                    convertTotalUsd += ExchangeRateToUsdConverter.LineAmountUsd(poItem.Qty, poItem.ConvertPrice);
                    TrackSellLineForRecalc(recalcSellLineIds, poItem.SellOrderItemId);
                }
            }

            var deleted = new List<PurchaseOrderItem>();
            var (deleteActorId, deleteActorName) = await ResolveActorAsync(actingUserId);
            foreach (var existing in existingActive.Values)
            {
                if (keptIds.Contains(existing.Id))
                    continue;

                TrackSellLineForRecalc(recalcSellLineIds, existing.SellOrderItemId);
                existing.IsDeleted = true;
                existing.ModifyTime = DateTime.UtcNow;
                existing.DeletedByUserId = deleteActorId;
                existing.DeletedByUserName = deleteActorName;
                await _poItemRepo.UpdateAsync(existing);
                await _poItemExtendRepo.DeleteAsync(existing.Id);
                deleted.Add(existing);
            }

            return new PurchaseOrderItemSyncResult(inserted, updated, deleted, itemUpdateAudits, total, convertTotalUsd);
        }

        private static void TrackSellLineForRecalc(HashSet<string> recalcSellLineIds, string? sellOrderItemId)
        {
            if (!string.IsNullOrWhiteSpace(sellOrderItemId))
                recalcSellLineIds.Add(sellOrderItemId.Trim());
        }

        private static void ApplyPurchaseOrderItemFromRequest(
            PurchaseOrderItem target,
            CreatePurchaseOrderItemRequest item,
            PurchaseOrder order,
            FinanceExchangeRateDto fx)
        {
            target.SellOrderItemId = NormalizeStoredSellOrderItemId(item.SellOrderItemId);
            target.VendorId = !string.IsNullOrWhiteSpace(item.VendorId) ? item.VendorId.Trim() : order.VendorId;
            target.ProductId = item.ProductId;
            target.PN = item.PN;
            target.Brand = item.Brand;
            target.Qty = item.Qty;
            target.Cost = item.Cost;
            target.Currency = item.Currency;
            target.DeliveryDate = PostgreSqlDateTime.ToUtc(item.DeliveryDate);
            target.DateCode = NormalizeDateCode(item.DateCode);
            target.Comment = item.Comment;
            target.InnerComment = item.InnerComment;
            target.ConvertPrice = ExchangeRateToUsdConverter.UnitLocalToUsd(
                target.Cost, target.Currency, fx.UsdToCny, fx.UsdToHkd, fx.UsdToEur);
        }

        private static string? NormalizeDateCode(string? value)
        {
            var s = (value ?? string.Empty).Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        private async Task LogPurchaseOrderItemsDeletedAsync(
            PurchaseOrder order,
            IReadOnlyList<PurchaseOrderItem> deletedLines,
            string? actingUserId,
            string actionType,
            string descriptionPrefix)
        {
            if (_logOperationAppend == null) return;
            var (actorId, actorName) = await ResolveActorAsync(actingUserId);
            foreach (var d in deletedLines)
            {
                await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
                {
                    BizType = BusinessLogTypes.PurchaseOrderItem,
                    RecordId = d.Id,
                    RecordCode = d.PurchaseOrderItemCode,
                    ActionTypeOverride = actionType,
                    OperatorUserId = actorId,
                    OperatorUserName = actorName,
                    OperationDescOverride = $"{descriptionPrefix} {d.PurchaseOrderItemCode}"
                });
            }
        }

        private async Task LogPurchaseOrderWholeDeleteOperationLogsAsync(
            PurchaseOrder order,
            IReadOnlyList<PurchaseOrderItem> deletedItems,
            string? actingUserId)
        {
            if (_logOperationAppend == null) return;
            var (actorId, actorName) = await ResolveActorAsync(actingUserId);
            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = BusinessLogTypes.PurchaseOrder,
                RecordId = order.Id,
                RecordCode = order.PurchaseOrderCode,
                ActionTypeOverride = OperationLogActionTypes.PurchaseOrderDelete,
                OperatorUserId = actorId,
                OperatorUserName = actorName,
                OperationDescOverride =
                    $"整单删除采购订单 {order.PurchaseOrderCode}，共 {deletedItems.Count} 条明细"
            });

            if (deletedItems.Count > 0)
            {
                await LogPurchaseOrderItemsDeletedAsync(
                    order,
                    deletedItems,
                    actingUserId,
                    OperationLogActionTypes.PurchaseOrderItemDeleteWithOrder,
                    $"整单删除采购订单 {order.PurchaseOrderCode} 时删除明细行");
            }
        }

        private async Task SoftDeletePurchaseOrderExtendAsync(string purchaseOrderId)
        {
            if (string.IsNullOrWhiteSpace(purchaseOrderId) || _unitOfWork == null)
                return;
            var safeId = purchaseOrderId.Trim().Replace("'", "''", StringComparison.Ordinal);
            await _unitOfWork.ExecuteAsync(
                $@"UPDATE purchaseorderextend SET is_deleted = true, ""ModifyTime"" = NOW() WHERE ""PurchaseOrderId"" = '{safeId}' AND is_deleted = false");
        }

        private async Task<(string? UserId, string UserName)> ResolveActorAsync(string? actingUserId)
        {
            var id = NormalizeActingUserId(actingUserId);
            if (string.IsNullOrEmpty(id))
                return (null, "系统");
            if (_userService == null)
                return (id, id);
            var user = await _userService.GetByIdAsync(id);
            return (id, string.IsNullOrWhiteSpace(user?.UserName) ? id : user!.UserName!.Trim());
        }

        private static string FormatPurchaseOrderStatus(short status) => status switch
        {
            1 => "新建",
            2 => "待审核",
            10 => "审核通过",
            20 => "待确认",
            30 => "已确认",
            50 => "进行中",
            100 => "采购完成",
            -1 => "审核失败",
            -2 => "取消",
            _ => status.ToString()
        };

        private static string FormatCurrency(short currency) => currency switch
        {
            1 => "RMB",
            2 => "USD",
            3 => "EUR",
            4 => "HKD",
            5 => "JPY",
            6 => "GBP",
            _ => currency.ToString()
        };

        private static string FormatPurchaseOrderType(short type) => type switch
        {
            PurchaseOrderItemLinkRules.PurchaseOrderTypeCustomer => "客单采购",
            PurchaseOrderItemLinkRules.PurchaseOrderTypeStocking => "备货采购",
            PurchaseOrderItemLinkRules.PurchaseOrderTypeSample => "样品采购",
            _ => type.ToString()
        };

        private static string FormatDecimal2(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        private static string FormatDecimal4(decimal value) =>
            Math.Round(value, 4, MidpointRounding.AwayFromZero).ToString("F4", System.Globalization.CultureInfo.InvariantCulture);

        private static string? FormatDate(DateTime? dt) =>
            dt.HasValue ? dt.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : null;

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
