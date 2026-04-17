using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services
{
    public class LogisticsService : ILogisticsService
    {
        private readonly IRepository<StockInNotify> _notifyRepo;
        private readonly IRepository<StockIn> _stockInRepo;
        private readonly IRepository<StockInItemExtend> _stockInItemExtendRepo;
        private readonly IRepository<QCInfo> _qcRepo;
        private readonly IRepository<QCItem> _qcItemRepo;
        private readonly IRepository<PurchaseOrder> _poRepo;
        private readonly IRepository<PurchaseOrderItem> _poItemRepo;
        private readonly IRepository<PurchaseOrderItemExtend> _poItemExtendRepo;
        private readonly IRepository<SellOrderItem> _sellOrderItemRepo;
        private readonly IRepository<SellOrder> _sellOrderRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IPurchaseOrderItemExtendSyncService _poItemExtendSync;
        private readonly ILogger<LogisticsService> _logger;

        public LogisticsService(
            IRepository<StockInNotify> notifyRepo,
            IRepository<StockIn> stockInRepo,
            IRepository<StockInItemExtend> stockInItemExtendRepo,
            IRepository<QCInfo> qcRepo,
            IRepository<QCItem> qcItemRepo,
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IRepository<PurchaseOrderItemExtend> poItemExtendRepo,
            IRepository<SellOrderItem> sellOrderItemRepo,
            IRepository<SellOrder> sellOrderRepo,
            ISerialNumberService serialNumberService,
            IPurchaseOrderItemExtendSyncService poItemExtendSync,
            IUnitOfWork unitOfWork,
            ILogger<LogisticsService> logger)
        {
            _notifyRepo = notifyRepo;
            _stockInRepo = stockInRepo;
            _stockInItemExtendRepo = stockInItemExtendRepo;
            _qcRepo = qcRepo;
            _qcItemRepo = qcItemRepo;
            _poRepo = poRepo;
            _poItemRepo = poItemRepo;
            _poItemExtendRepo = poItemExtendRepo;
            _sellOrderItemRepo = sellOrderItemRepo;
            _sellOrderRepo = sellOrderRepo;
            _serialNumberService = serialNumberService;
            _poItemExtendSync = poItemExtendSync;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IReadOnlyList<StockInNotify>> GetArrivalNoticesAsync()
        {
            var list = (await _notifyRepo.GetAllAsync()).OrderByDescending(x => x.CreateTime).ToList();
            var poIds = list.Select(x => x.PurchaseOrderId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            var poMap = new Dictionary<string, PurchaseOrder>(StringComparer.Ordinal);
            if (poIds.Count > 0)
            {
                var pos = await _poRepo.FindAsync(x => poIds.Contains(x.Id));
                foreach (var po in pos)
                    poMap[po.Id] = po;
            }

            foreach (var row in list)
            {
                if (poMap.TryGetValue(row.PurchaseOrderId, out var po))
                    row.VendorCode = po.VendorCode;
                AttachItemSnapshot(row);
            }

            return list;
        }

        private static void AttachItemSnapshot(StockInNotify n)
        {
            n.Items = new List<StockInNotifyItemSnapshot>
            {
                new()
                {
                    Id = n.Id,
                    StockInNotifyId = n.Id,
                    PurchaseOrderItemId = n.PurchaseOrderItemId,
                    Pn = n.Pn,
                    Brand = n.Brand,
                    Qty = n.ExpectQty,
                    ArrivedQty = n.ReceiveQty,
                    PassedQty = n.PassedQty
                }
            };
        }

        public async Task<StockInNotify> CreateArrivalNoticeAsync(CreateArrivalNoticeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PurchaseOrderItemId))
                throw new ArgumentException("采购明细ID不能为空", nameof(request.PurchaseOrderItemId));

            var poItem = await _poItemRepo.GetByIdAsync(request.PurchaseOrderItemId)
                         ?? throw new InvalidOperationException("采购订单明细不存在");

            var expectQty = request.ExpectQty;
            if (expectQty <= 0)
                throw new InvalidOperationException("预期到货数量必须大于0");

            var po = await _poRepo.GetByIdAsync(poItem.PurchaseOrderId) ?? throw new InvalidOperationException("采购订单不存在");
            if (po.Status < 30 || po.Status == -1 || po.Status == -2)
                throw new InvalidOperationException("仅供应商已确认且有效的采购订单可创建到货通知");

            await _poItemExtendSync.RecalculateAsync(poItem.Id);
            await _unitOfWork.SaveChangesAsync();

            var ext = await _poItemExtendRepo.GetByIdAsync(poItem.Id)
                      ?? throw new InvalidOperationException("采购明细扩展不存在");
            if (expectQty > ext.QtyStockInNotifyNot)
                throw new InvalidOperationException($"预期到货数量不能大于剩余可通知数量（当前剩余 {ext.QtyStockInNotifyNot}）");

            var noticeCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.ArrivalNotice);
            var expectedArrival = request.ExpectedArrivalDate ?? poItem.DeliveryDate ?? po.DeliveryDate;
            var expectTotal = Math.Round(expectQty * poItem.Cost, 2, MidpointRounding.AwayFromZero);
            var regionType = RegionTypeCode.Normalize(request.RegionType);

            var notice = new StockInNotify
            {
                Id = Guid.NewGuid().ToString(),
                NoticeCode = noticeCode,
                PurchaseOrderId = po.Id,
                PurchaseOrderCode = po.PurchaseOrderCode,
                PurchaseOrderItemId = poItem.Id,
                SellOrderItemId = poItem.SellOrderItemId,
                VendorId = po.VendorId,
                VendorName = po.VendorName,
                PurchaseUserName = po.PurchaseUserName,
                Status = 10,
                ExpectedArrivalDate = PostgreSqlDateTime.ToUtc(expectedArrival),
                RegionType = regionType,
                Pn = poItem.PN,
                Brand = poItem.Brand,
                ExpectQty = InventoryQuantity.RoundFromDecimal(expectQty),
                ReceiveQty = 0,
                PassedQty = 0,
                Cost = poItem.Cost,
                ExpectTotal = expectTotal,
                ReceiveTotal = 0,
                CreateTime = DateTime.UtcNow
            };
            await _notifyRepo.AddAsync(notice);
            await _unitOfWork.SaveChangesAsync();

            await _poItemExtendSync.RecalculateAsync(poItem.Id);
            notice.VendorCode = po.VendorCode;
            AttachItemSnapshot(notice);
            return notice;
        }

        public async Task<AutoGenerateArrivalNoticeResult> AutoGenerateArrivalNoticesAsync()
        {
            var result = new AutoGenerateArrivalNoticeResult();
            var candidatePos = (await _poRepo.GetAllAsync())
                .Where(x => x.Status >= 30 && x.Status != -1 && x.Status != -2)
                .ToList();
            result.PurchaseOrdersScanned = candidatePos.Count;

            foreach (var po in candidatePos)
            {
                var lines = (await _poItemRepo.FindAsync(x => x.PurchaseOrderId == po.Id)).ToList();
                foreach (var line in lines)
                {
                    await _poItemExtendSync.RecalculateAsync(line.Id);
                    var ext = await _poItemExtendRepo.GetByIdAsync(line.Id);
                    if (ext == null || ext.QtyStockInNotifyNot <= 0)
                    {
                        result.ExistingCount++;
                        continue;
                    }

                    await CreateArrivalNoticeAsync(new CreateArrivalNoticeRequest
                    {
                        PurchaseOrderItemId = line.Id,
                        ExpectQty = ext.QtyStockInNotifyNot,
                        PurchaseOrderId = po.Id,
                        ExpectedArrivalDate = line.DeliveryDate ?? po.DeliveryDate
                    });
                    result.CreatedCount++;
                }
            }

            return result;
        }

        public async Task UpdateArrivalNoticeStatusAsync(string id, short status)
        {
            var row = await _notifyRepo.GetByIdAsync(id) ?? throw new InvalidOperationException("到货通知不存在");
            row.Status = status;
            row.ModifyTime = DateTime.UtcNow;
            await _notifyRepo.UpdateAsync(row);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<QCInfo>> GetQcsAsync(QcQueryRequest? request = null)
        {
            var list = (await _qcRepo.GetAllAsync()).OrderByDescending(x => x.CreateTime).ToList();
            var qcItemsAll = (await _qcItemRepo.GetAllAsync()).ToList();
            var map = qcItemsAll.GroupBy(x => x.QcInfoId).ToDictionary(g => g.Key, g => (ICollection<QCItem>)g.ToList());
            foreach (var row in list)
            {
                row.Items = map.TryGetValue(row.Id, out var v) ? v : new List<QCItem>();
            }

            if (list.Count == 0)
                return list;

            var notices = (await _notifyRepo.GetAllAsync()).ToList();
            var noticeMap = notices.ToDictionary(x => x.Id, x => x);

            var poItems = (await _poItemRepo.GetAllAsync()).ToList();
            var poItemMap = poItems.ToDictionary(x => x.Id, x => x);
            // 采购明细 purchase_order_id 为空时 GroupBy(null).ToDictionary 会抛 ArgumentNullException，导致质检列表整页失败
            var poItemsByPurchaseOrderId = poItems
                .Where(x => !string.IsNullOrWhiteSpace(x.PurchaseOrderId))
                .GroupBy(x => x.PurchaseOrderId.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var sellOrderItems = (await _sellOrderItemRepo.GetAllAsync()).ToList();
            var sellOrders = (await _sellOrderRepo.GetAllAsync()).ToList();
            var sellOrderCodeById = sellOrders.ToDictionary(x => x.Id, x => x.SellOrderCode ?? string.Empty);
            var sellOrderCodeBySellOrderItemId = sellOrderItems.ToDictionary(
                x => x.Id,
                x => x.SellOrderId != null && sellOrderCodeById.TryGetValue(x.SellOrderId, out var code) ? code : string.Empty);

            foreach (var qc in list)
            {
                noticeMap.TryGetValue(qc.StockInNotifyId, out var notice);

                qc.VendorName = notice?.VendorName;
                qc.PurchaseOrderCode = notice?.PurchaseOrderCode;

                var modelSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (notice != null)
                {
                    if (!string.IsNullOrWhiteSpace(notice.Pn)) modelSet.Add(notice.Pn.Trim());
                    else if (!string.IsNullOrWhiteSpace(notice.PurchaseOrderItemId)
                             && poItemMap.TryGetValue(notice.PurchaseOrderItemId, out var poi0)
                             && !string.IsNullOrWhiteSpace(poi0.PN))
                        modelSet.Add(poi0.PN.Trim());
                }

                if (notice != null
                    && !string.IsNullOrWhiteSpace(notice.PurchaseOrderId)
                    && poItemsByPurchaseOrderId.TryGetValue(notice.PurchaseOrderId.Trim(), out var poLines))
                {
                    foreach (var pl in poLines)
                    {
                        if (!string.IsNullOrWhiteSpace(pl.PN)) modelSet.Add(pl.PN.Trim());
                    }
                }

                qc.Model = modelSet.Count == 0 ? null : string.Join(", ", modelSet.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

                var brandSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (notice != null)
                {
                    if (!string.IsNullOrWhiteSpace(notice.Brand)) brandSet.Add(notice.Brand.Trim());
                    else if (!string.IsNullOrWhiteSpace(notice.PurchaseOrderItemId)
                             && poItemMap.TryGetValue(notice.PurchaseOrderItemId, out var poiBrand)
                             && !string.IsNullOrWhiteSpace(poiBrand.Brand))
                        brandSet.Add(poiBrand.Brand.Trim());
                }

                if (notice != null
                    && !string.IsNullOrWhiteSpace(notice.PurchaseOrderId)
                    && poItemsByPurchaseOrderId.TryGetValue(notice.PurchaseOrderId.Trim(), out var poLinesBrand))
                {
                    foreach (var pl in poLinesBrand)
                    {
                        if (!string.IsNullOrWhiteSpace(pl.Brand)) brandSet.Add(pl.Brand.Trim());
                    }
                }

                qc.Brand = brandSet.Count == 0 ? null : string.Join(", ", brandSet.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

                if (notice != null && !string.IsNullOrWhiteSpace(notice.PurchaseOrderItemId)
                    && poItemMap.TryGetValue(notice.PurchaseOrderItemId, out var poiSo)
                    && !string.IsNullOrWhiteSpace(poiSo.SellOrderItemId)
                    && sellOrderCodeBySellOrderItemId.TryGetValue(poiSo.SellOrderItemId, out var so)
                    && !string.IsNullOrWhiteSpace(so))
                    qc.SalesOrderCode = so;
            }

            await ReconcileQcMissingStockInBindingAsync(list, noticeMap);
            await NormalizeUnboundQcStockInDisplayStatusAsync(list);

            var modelKeyword = request?.Model?.Trim();
            var vendorKeyword = request?.VendorName?.Trim();
            var poCodeKeyword = request?.PurchaseOrderCode?.Trim();
            var soCodeKeyword = request?.SalesOrderCode?.Trim();
            var hasFilters = !string.IsNullOrWhiteSpace(modelKeyword)
                             || !string.IsNullOrWhiteSpace(vendorKeyword)
                             || !string.IsNullOrWhiteSpace(poCodeKeyword)
                             || !string.IsNullOrWhiteSpace(soCodeKeyword);
            if (!hasFilters)
            {
                return list;
            }

            bool MatchModel(QCInfo qc, StockInNotify? notice, string keyword)
            {
                if (!string.IsNullOrWhiteSpace(qc.Model) && qc.Model.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (notice != null && !string.IsNullOrWhiteSpace(notice.Pn) && notice.Pn.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (notice != null && !string.IsNullOrWhiteSpace(notice.PurchaseOrderItemId)
                    && poItemMap.TryGetValue(notice.PurchaseOrderItemId, out var poi)
                    && !string.IsNullOrWhiteSpace(poi.PN)
                    && poi.PN.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (qc.Items is { Count: > 0 })
                {
                    foreach (var qci in qc.Items)
                    {
                        if (noticeMap.TryGetValue(qci.ArrivalStockInNotifyId, out var n) && MatchModel(qc, n, keyword))
                            return true;
                    }
                }

                if (notice != null
                    && !string.IsNullOrWhiteSpace(notice.PurchaseOrderId)
                    && poItemsByPurchaseOrderId.TryGetValue(notice.PurchaseOrderId.Trim(), out var polines))
                {
                    if (polines.Any(p => !string.IsNullOrWhiteSpace(p.PN) && p.PN.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }

                return false;
            }

            return list.Where(qc =>
            {
                noticeMap.TryGetValue(qc.StockInNotifyId, out var notice);

                if (!string.IsNullOrWhiteSpace(vendorKeyword)
                    && !(notice?.VendorName?.Contains(vendorKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;

                if (!string.IsNullOrWhiteSpace(poCodeKeyword)
                    && !(notice?.PurchaseOrderCode?.Contains(poCodeKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;

                if (!string.IsNullOrWhiteSpace(modelKeyword) && !MatchModel(qc, notice, modelKeyword))
                    return false;

                if (!string.IsNullOrWhiteSpace(soCodeKeyword))
                {
                    var soMatched = notice != null
                                    && !string.IsNullOrWhiteSpace(notice.PurchaseOrderItemId)
                                    && poItemMap.TryGetValue(notice.PurchaseOrderItemId, out var poi)
                                    && !string.IsNullOrWhiteSpace(poi.SellOrderItemId)
                                    && sellOrderCodeBySellOrderItemId.TryGetValue(poi.SellOrderItemId, out var soCode)
                                    && !string.IsNullOrWhiteSpace(soCode)
                                    && soCode.Contains(soCodeKeyword, StringComparison.OrdinalIgnoreCase);
                    if (!soMatched) return false;
                }

                return true;
            }).ToList();
        }

        public async Task<QCInfo> CreateQcAsync(CreateQcRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.StockInNotifyId))
                throw new ArgumentException("到货通知ID不能为空", nameof(request.StockInNotifyId));

            var existed = (await _qcRepo.FindAsync(x => x.StockInNotifyId == request.StockInNotifyId)).FirstOrDefault();
            if (existed != null)
                return existed;

            var notice = await _notifyRepo.GetByIdAsync(request.StockInNotifyId) ?? throw new InvalidOperationException("到货通知不存在");

            var passQty = notice.ExpectQty;
            var qcCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.QcRecord);
            var qc = new QCInfo
            {
                Id = Guid.NewGuid().ToString(),
                QcCode = qcCode,
                StockInNotifyId = notice.Id,
                StockInNotifyCode = notice.NoticeCode,
                Status = 10,
                StockInStatus = 1,
                PassQty = passQty,
                RejectQty = 0,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
            };
            await _qcRepo.AddAsync(qc);

            await _qcItemRepo.AddAsync(new QCItem
            {
                Id = Guid.NewGuid().ToString(),
                QcInfoId = qc.Id,
                ArrivalStockInNotifyId = notice.Id,
                ArrivedQty = notice.ExpectQty,
                PassedQty = notice.ExpectQty,
                RejectQty = 0,
                CreateTime = DateTime.UtcNow
            });

            notice.Status = 30;
            notice.ModifyTime = DateTime.UtcNow;
            await _notifyRepo.UpdateAsync(notice);

            await _unitOfWork.SaveChangesAsync();
            qc.Items = (await _qcItemRepo.FindAsync(x => x.QcInfoId == qc.Id)).ToList();
            return qc;
        }

        public async Task<QCInfo> UpdateQcResultAsync(string id, UpdateQcResultRequest request, string? actingUserId = null)
        {
            var qc = await _qcRepo.GetByIdAsync(id) ?? throw new InvalidOperationException("质检单不存在");
            var items = (await _qcItemRepo.FindAsync(x => x.QcInfoId == qc.Id)).ToList();
            if (!items.Any()) throw new InvalidOperationException("质检明细为空");

            var arrivedTotal = items.Sum(x => x.ArrivedQty);
            qc.PassQty = InventoryQuantity.RoundFromDecimal(request.PassQty);
            qc.RejectQty = InventoryQuantity.RoundFromDecimal(request.RejectQty);
            qc.Status = request.Result switch
            {
                "pass" => 100,
                "partial" => 10,
                "reject" => -1,
                _ => throw new ArgumentException("质检结果不合法", nameof(request.Result))
            };
            // StockInStatus：入库进度（见 QCInfo 注释），与质检 Status 的 10/100 不可混用；保存质检后尚未入库
            qc.StockInStatus = request.Result == "reject" ? (short)-1 : (short)1;
            if (request.HasStockInPlanDate == true)
                qc.StockInPlanDate = PostgreSqlDateTime.ToUtc(request.StockInPlanDate);
            qc.ModifyTime = DateTime.UtcNow;
            qc.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _qcRepo.UpdateAsync(qc);

            var ratio = arrivedTotal > 0 ? (decimal)qc.PassQty / arrivedTotal : 0m;
            foreach (var item in items)
            {
                item.PassedQty = InventoryQuantity.RoundFromDecimal(item.ArrivedQty * ratio);
                item.RejectQty = Math.Max(0, item.ArrivedQty - item.PassedQty);
                item.ModifyTime = DateTime.UtcNow;
                await _qcItemRepo.UpdateAsync(item);
            }

            var notice = await _notifyRepo.GetByIdAsync(qc.StockInNotifyId);
            if (notice != null)
            {
                notice.Status = 30;
                notice.PassedQty = items.Sum(x => x.PassedQty);
                notice.ReceiveQty = notice.PassedQty;
                notice.ReceiveTotal = Math.Round(notice.ReceiveQty * notice.Cost, 2, MidpointRounding.AwayFromZero);
                notice.ModifyTime = DateTime.UtcNow;
                await _notifyRepo.UpdateAsync(notice);

                if (!string.IsNullOrWhiteSpace(notice.PurchaseOrderItemId))
                    await _poItemExtendSync.RecalculateAsync(notice.PurchaseOrderItemId);
            }

            await _unitOfWork.SaveChangesAsync();
            qc.Items = items;
            return qc;
        }

        public async Task BindQcStockInAsync(string id, string stockInId, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(stockInId))
                throw new ArgumentException("入库单ID不能为空", nameof(stockInId));

            var qc = await _qcRepo.GetByIdAsync(id) ?? throw new InvalidOperationException("质检单不存在");
            qc.StockInId = stockInId;
            // 仅绑定入库单；入库进度在过账完成时由 HandleStockInCompletedAsync 回写
            qc.ModifyTime = DateTime.UtcNow;
            qc.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _qcRepo.UpdateAsync(qc);

            var notice = await _notifyRepo.GetByIdAsync(qc.StockInNotifyId);
            if (notice != null)
            {
                notice.Status = 100;
                notice.ModifyTime = DateTime.UtcNow;
                await _notifyRepo.UpdateAsync(notice);
            }

            var stockIn = await _stockInRepo.GetByIdAsync(stockInId);
            if (stockIn != null)
            {
                stockIn.SourceId = string.IsNullOrWhiteSpace(notice?.Id) ? null : notice!.Id.Trim();
                stockIn.SourceCode = !string.IsNullOrWhiteSpace(notice?.NoticeCode)
                    ? notice!.NoticeCode.Trim()
                    : (string.IsNullOrWhiteSpace(qc.StockInNotifyCode) ? null : qc.StockInNotifyCode.Trim());
                stockIn.QcId = string.IsNullOrWhiteSpace(qc.Id) ? null : qc.Id.Trim();
                stockIn.QcCode = string.IsNullOrWhiteSpace(qc.QcCode) ? null : qc.QcCode.Trim();
                stockIn.ModifyTime = DateTime.UtcNow;
                await _stockInRepo.UpdateAsync(stockIn);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task HandleStockInCompletedAsync(string stockInId, string? purchaseOrderId)
        {
            if (string.IsNullOrWhiteSpace(stockInId)) return;

            _logger.LogInformation(
                "[InboundStatus2] HandleStockInCompleted enter StockInId={StockInId} HookPurchaseOrderId={PoId}",
                stockInId,
                purchaseOrderId ?? "(null)");

            var stockIn = await _stockInRepo.GetByIdAsync(stockInId);
            var qcsMap = new Dictionary<string, QCInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var qc in await _qcRepo.FindAsync(x => x.StockInId == stockInId))
                qcsMap[qc.Id] = qc;
            if (stockIn != null && !string.IsNullOrWhiteSpace(stockIn.QcId))
            {
                var qcBySi = await _qcRepo.GetByIdAsync(stockIn.QcId.Trim());
                if (qcBySi != null)
                    qcsMap[qcBySi.Id] = qcBySi;
            }

            var qcs = qcsMap.Values.ToList();
            var relatedNotices = new List<StockInNotify>();
            var hasChanges = false;

            foreach (var qc in qcs)
            {
                if (string.IsNullOrWhiteSpace(qc.StockInId))
                {
                    qc.StockInId = stockInId;
                    qc.ModifyTime = DateTime.UtcNow;
                    await _qcRepo.UpdateAsync(qc);
                    hasChanges = true;
                }

                var targetQcStockInStatus = qc.Status == 100 ? (short)100 : qc.Status == 10 ? (short)10 : qc.StockInStatus;
                if (qc.StockInStatus != targetQcStockInStatus)
                {
                    qc.StockInStatus = targetQcStockInStatus;
                    qc.ModifyTime = DateTime.UtcNow;
                    await _qcRepo.UpdateAsync(qc);
                    hasChanges = true;
                }

                var notice = await _notifyRepo.GetByIdAsync(qc.StockInNotifyId);
                if (notice == null) continue;
                if (notice.Status != 100)
                {
                    notice.Status = 100;
                    notice.ModifyTime = DateTime.UtcNow;
                    await _notifyRepo.UpdateAsync(notice);
                    hasChanges = true;
                }
                relatedNotices.Add(notice);
            }

            var poIds = relatedNotices.Select(x => x.PurchaseOrderId).Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var resolvedHook = await ResolvePurchaseOrderIdForStockInCompletedAsync(purchaseOrderId);
            if (!string.IsNullOrWhiteSpace(resolvedHook))
                poIds.Add(resolvedHook);

            _logger.LogInformation(
                "[InboundStatus2] HandleStockInCompleted poIds={PoIds} QcCount={QcCount} NoticeCount={NoticeCount}",
                string.Join(',', poIds),
                qcs.Count,
                relatedNotices.Count);

            foreach (var poId in poIds)
            {
                var po = await _poRepo.GetByIdAsync(poId);
                if (po == null) continue;

                _logger.LogInformation(
                    "[InboundStatus2] HandleStockInCompleted before PoItemRepo.FindAsync PoId={PoId}",
                    poId);
                var poItems = (await _poItemRepo.FindAsync(x => x.PurchaseOrderId == poId)).ToList();
                _logger.LogInformation(
                    "[InboundStatus2] HandleStockInCompleted after PoItemRepo.FindAsync PoId={PoId} LineCount={Count}",
                    poId,
                    poItems.Count);
                var allNotifiesForPo = (await _notifyRepo.FindAsync(x => x.PurchaseOrderId == poId)).ToList();
                var passedMap = allNotifiesForPo
                    .GroupBy(x => x.PurchaseOrderItemId)
                    .ToDictionary(g => g.Key, g => g.Sum(v => v.PassedQty));

                foreach (var item in poItems)
                {
                    var passed = passedMap.TryGetValue(item.Id, out var qty) ? qty : 0m;
                    var targetStockInStatus = (short)(passed <= 0m ? 0 : passed >= item.Qty ? 2 : 1);
                    var targetItemStatus = targetStockInStatus == 2 && item.Status < 60 ? (short)60 : item.Status;

                    if (item.StockInStatus == targetStockInStatus && item.Status == targetItemStatus)
                        continue;

                    if (passed <= 0m)
                    {
                        item.StockInStatus = 0;
                    }
                    else if (passed >= item.Qty)
                    {
                        item.StockInStatus = 2;
                        if (item.Status < 60) item.Status = 60;
                    }
                    else
                    {
                        item.StockInStatus = 1;
                    }
                    item.Status = targetItemStatus;
                    item.ModifyTime = DateTime.UtcNow;
                    await _poItemRepo.UpdateAsync(item);
                    hasChanges = true;
                }

                var targetPoStockStatus = (short)(poItems.All(x => x.StockInStatus == 2) ? 2 : poItems.Any(x => x.StockInStatus > 0) ? 1 : 0);
                if (po.StockStatus != targetPoStockStatus)
                {
                    po.StockStatus = targetPoStockStatus;
                    po.ModifyTime = DateTime.UtcNow;
                    await _poRepo.UpdateAsync(po);
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                _logger.LogInformation("[InboundStatus2] HandleStockInCompleted before SaveChanges (po/notice/qc updates)");
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("[InboundStatus2] HandleStockInCompleted after SaveChanges");
            }

            foreach (var poId in poIds)
            {
                _logger.LogInformation(
                    "[InboundStatus2] HandleStockInCompleted extend sync before PoItemRepo.FindAsync PoId={PoId}",
                    poId);
                var linesToSync = (await _poItemRepo.FindAsync(x => x.PurchaseOrderId == poId)).ToList();
                _logger.LogInformation(
                    "[InboundStatus2] HandleStockInCompleted extend sync lines PoId={PoId} LineCount={Count}",
                    poId,
                    linesToSync.Count);
                foreach (var line in linesToSync)
                    await _poItemExtendSync.RecalculateAsync(line.Id);
            }

            _logger.LogInformation("[InboundStatus2] HandleStockInCompleted exit StockInId={StockInId}", stockInId);
        }

        /// <summary>
        /// 已入库完成但 qcinfo 未写入 StockInId 的历史数据（例如先过账后绑定）：按入库单 QcId 或 SourceId+采购明细 回填绑定与入库状态。
        /// </summary>
        private async Task ReconcileQcMissingStockInBindingAsync(
            List<QCInfo> list,
            IReadOnlyDictionary<string, StockInNotify> noticeMap)
        {
            const short posted = 2;
            var pending = list.Where(q => string.IsNullOrWhiteSpace(q.StockInId) && q.Status != -1).ToList();
            if (pending.Count == 0)
                return;

            var qcIdList = pending.Select(q => q.Id).Distinct().ToList();
            var notifyIdList = pending
                .Select(q => q.StockInNotifyId?.Trim() ?? string.Empty)
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var stockIns = (await _stockInRepo.FindAsync(si =>
                    si.Status == posted
                    && (
                        (si.QcId != null && qcIdList.Contains(si.QcId))
                        || (si.SourceId != null && notifyIdList.Contains(si.SourceId))
                    )))
                .OrderByDescending(si => si.StockInDate)
                .ToList();
            if (stockIns.Count == 0)
                return;

            var utc = DateTime.UtcNow;
            var dirty = false;
            var claimedStockInIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var qc in pending)
            {
                var hit = stockIns.FirstOrDefault(si =>
                    !string.IsNullOrWhiteSpace(si.QcId)
                    && string.Equals(si.QcId.Trim(), qc.Id.Trim(), StringComparison.OrdinalIgnoreCase));
                if (hit == null)
                {
                    noticeMap.TryGetValue(qc.StockInNotifyId, out var notice);
                    var poiId = notice?.PurchaseOrderItemId?.Trim();
                    HashSet<string>? stockInIdsForPoi = null;
                    if (!string.IsNullOrWhiteSpace(poiId))
                    {
                        var exts = (await _stockInItemExtendRepo.FindAsync(e => e.PurchaseOrderItemId == poiId)).ToList();
                        stockInIdsForPoi = exts
                            .Select(e => e.StockInId?.Trim() ?? string.Empty)
                            .Where(s => s.Length > 0)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    }

                    hit = stockIns
                        .Where(si =>
                            string.Equals(si.SourceId?.Trim(), qc.StockInNotifyId.Trim(), StringComparison.OrdinalIgnoreCase))
                        .Where(si =>
                            string.IsNullOrWhiteSpace(poiId)
                            || (stockInIdsForPoi != null && stockInIdsForPoi.Contains(si.Id.Trim())))
                        .OrderByDescending(si => si.StockInDate)
                        .FirstOrDefault();
                }

                if (hit == null || claimedStockInIds.Contains(hit.Id))
                    continue;
                claimedStockInIds.Add(hit.Id);

                qc.StockInId = hit.Id;
                qc.StockInStatus = qc.Status == 100 ? (short)100 : qc.Status == 10 ? (short)10 : (short)100;
                qc.ModifyTime = utc;
                await _qcRepo.UpdateAsync(qc);
                dirty = true;
            }

            if (dirty)
                await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 未绑定入库单时 StockInStatus 只应为「未入库」(1)；历史上曾误将质检结论 10/100 写入该字段，此处加载列表时自愈并落库。
        /// </summary>
        private async Task NormalizeUnboundQcStockInDisplayStatusAsync(List<QCInfo> list)
        {
            var utc = DateTime.UtcNow;
            var dirty = false;
            foreach (var qc in list)
            {
                if (!string.IsNullOrWhiteSpace(qc.StockInId) || qc.Status == -1)
                    continue;
                if (qc.StockInStatus == 1)
                    continue;
                qc.StockInStatus = 1;
                qc.ModifyTime = utc;
                await _qcRepo.UpdateAsync(qc);
                dirty = true;
            }

            if (dirty)
                await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 入库完成回写时，<paramref name="sourceIdOrCode"/> 可为采购订单主键、采购单号（由调用方从入库单关联的采购行解析）。
        /// </summary>
        private async Task<string?> ResolvePurchaseOrderIdForStockInCompletedAsync(string? sourceIdOrCode)
        {
            if (string.IsNullOrWhiteSpace(sourceIdOrCode)) return null;
            var t = sourceIdOrCode.Trim();

            var po = await _poRepo.GetByIdAsync(t);
            if (po != null) return po.Id;

            var byCode = (await _poRepo.FindAsync(p => p.PurchaseOrderCode == t)).FirstOrDefault();
            if (byCode != null) return byCode.Id;

            var qc = await _qcRepo.GetByIdAsync(t);
            if (qc == null) return null;
            var notice = await _notifyRepo.GetByIdAsync(qc.StockInNotifyId);
            return string.IsNullOrWhiteSpace(notice?.PurchaseOrderId) ? null : notice!.PurchaseOrderId.Trim();
        }

    }
}
