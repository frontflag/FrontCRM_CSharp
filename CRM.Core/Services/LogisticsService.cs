using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public class LogisticsService : ILogisticsService
    {
        private readonly IRepository<StockInNotify> _notifyRepo;
        private readonly IRepository<StockInNotifyItem> _notifyItemRepo;
        private readonly IRepository<QCInfo> _qcRepo;
        private readonly IRepository<QCItem> _qcItemRepo;
        private readonly IRepository<PurchaseOrder> _poRepo;
        private readonly IRepository<PurchaseOrderItem> _poItemRepo;
        private readonly IRepository<SellOrderItem> _sellOrderItemRepo;
        private readonly IRepository<SellOrder> _sellOrderRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;

        public LogisticsService(
            IRepository<StockInNotify> notifyRepo,
            IRepository<StockInNotifyItem> notifyItemRepo,
            IRepository<QCInfo> qcRepo,
            IRepository<QCItem> qcItemRepo,
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IRepository<SellOrderItem> sellOrderItemRepo,
            IRepository<SellOrder> sellOrderRepo,
            ISerialNumberService serialNumberService,
            IUnitOfWork unitOfWork)
        {
            _notifyRepo = notifyRepo;
            _notifyItemRepo = notifyItemRepo;
            _qcRepo = qcRepo;
            _qcItemRepo = qcItemRepo;
            _poRepo = poRepo;
            _poItemRepo = poItemRepo;
            _sellOrderItemRepo = sellOrderItemRepo;
            _sellOrderRepo = sellOrderRepo;
            _serialNumberService = serialNumberService;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<StockInNotify>> GetArrivalNoticesAsync()
        {
            var list = (await _notifyRepo.GetAllAsync()).OrderByDescending(x => x.CreateTime).ToList();
            var items = (await _notifyItemRepo.GetAllAsync()).ToList();
            var map = items.GroupBy(x => x.StockInNotifyId).ToDictionary(g => g.Key, g => (ICollection<StockInNotifyItem>)g.ToList());
            foreach (var row in list)
            {
                row.Items = map.TryGetValue(row.Id, out var v) ? v : new List<StockInNotifyItem>();
            }
            return list;
        }

        public async Task<StockInNotify> CreateArrivalNoticeAsync(CreateArrivalNoticeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PurchaseOrderId))
                throw new ArgumentException("采购订单ID不能为空", nameof(request.PurchaseOrderId));

            var existed = (await _notifyRepo.FindAsync(x => x.PurchaseOrderId == request.PurchaseOrderId)).FirstOrDefault();
            if (existed != null)
                return existed;

            var po = await _poRepo.GetByIdAsync(request.PurchaseOrderId) ?? throw new InvalidOperationException("采购订单不存在");
            var poItems = (await _poItemRepo.FindAsync(x => x.PurchaseOrderId == po.Id)).ToList();
            if (!poItems.Any())
                throw new InvalidOperationException("采购订单无明细，无法创建到货通知");

            var noticeCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.ArrivalNotice);

            var expectedArrival = request.ExpectedArrivalDate ?? po.DeliveryDate;

            var notice = new StockInNotify
            {
                Id = Guid.NewGuid().ToString(),
                NoticeCode = noticeCode,
                PurchaseOrderId = po.Id,
                PurchaseOrderCode = po.PurchaseOrderCode,
                VendorId = po.VendorId,
                VendorName = po.VendorName,
                PurchaseUserName = po.PurchaseUserName,
                Status = 10,
                ExpectedArrivalDate = PostgreSqlDateTime.ToUtc(expectedArrival),
                CreateTime = DateTime.UtcNow
            };
            await _notifyRepo.AddAsync(notice);

            foreach (var item in poItems)
            {
                await _notifyItemRepo.AddAsync(new StockInNotifyItem
                {
                    Id = Guid.NewGuid().ToString(),
                    StockInNotifyId = notice.Id,
                    PurchaseOrderItemId = item.Id,
                    Pn = item.PN,
                    Brand = item.Brand,
                    Qty = item.Qty,
                    ArrivedQty = item.Qty,
                    PassedQty = 0,
                    CreateTime = DateTime.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync();
            notice.Items = (await _notifyItemRepo.FindAsync(x => x.StockInNotifyId == notice.Id)).ToList();
            return notice;
        }

        public async Task<AutoGenerateArrivalNoticeResult> AutoGenerateArrivalNoticesAsync()
        {
            var result = new AutoGenerateArrivalNoticeResult();
            var candidatePos = (await _poRepo.GetAllAsync())
                .Where(x => x.Status >= 30 && x.Status != -1 && x.Status != -2)
                .ToList();
            result.PurchaseOrdersScanned = candidatePos.Count;

            var existingPoIds = (await _notifyRepo.GetAllAsync())
                .Select(x => x.PurchaseOrderId)
                .ToHashSet();

            foreach (var po in candidatePos)
            {
                if (existingPoIds.Contains(po.Id))
                {
                    result.ExistingCount++;
                    continue;
                }
                await CreateArrivalNoticeAsync(new CreateArrivalNoticeRequest { PurchaseOrderId = po.Id });
                existingPoIds.Add(po.Id);
                result.CreatedCount++;
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
            var noticeIds = list.Select(x => x.StockInNotifyId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToHashSet();
            var relatedNoticeItems = (await _notifyItemRepo.GetAllAsync())
                .Where(x => noticeIds.Contains(x.StockInNotifyId))
                .ToList();
            var noticeItemsMap = relatedNoticeItems.GroupBy(x => x.StockInNotifyId).ToDictionary(g => g.Key, g => g.ToList());
            var notifyItemById = relatedNoticeItems.ToDictionary(x => x.Id, x => x);

            var poItems = (await _poItemRepo.GetAllAsync()).ToList();
            var poItemMap = poItems.ToDictionary(x => x.Id, x => x);
            var poItemsByPurchaseOrderId = poItems
                .GroupBy(x => x.PurchaseOrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var sellOrderItems = (await _sellOrderItemRepo.GetAllAsync()).ToList();
            var sellOrders = (await _sellOrderRepo.GetAllAsync()).ToList();
            var sellOrderCodeById = sellOrders.ToDictionary(x => x.Id, x => x.SellOrderCode ?? string.Empty);
            var sellOrderCodeBySellOrderItemId = sellOrderItems.ToDictionary(
                x => x.Id,
                x => x.SellOrderId != null && sellOrderCodeById.TryGetValue(x.SellOrderId, out var code) ? code : string.Empty);

            // 始终回填列表展示字段（供应商、采购单号、销售单号、型号），避免无筛选时列表列为空
            foreach (var qc in list)
            {
                noticeMap.TryGetValue(qc.StockInNotifyId, out var notice);
                noticeItemsMap.TryGetValue(qc.StockInNotifyId, out var noticeItems);
                noticeItems ??= new List<StockInNotifyItem>();

                qc.VendorName = notice?.VendorName;
                qc.PurchaseOrderCode = notice?.PurchaseOrderCode;

                var modelSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var ni in noticeItems)
                {
                    if (!string.IsNullOrWhiteSpace(ni.Pn)) modelSet.Add(ni.Pn.Trim());
                    else if (!string.IsNullOrWhiteSpace(ni.PurchaseOrderItemId)
                             && poItemMap.TryGetValue(ni.PurchaseOrderItemId, out var poi0)
                             && !string.IsNullOrWhiteSpace(poi0.PN))
                        modelSet.Add(poi0.PN.Trim());
                }

                // 合并同一采购订单下全部明细的 PN，便于列表展示与按物料型号检索（不仅限于到货通知行）
                if (notice != null
                    && !string.IsNullOrWhiteSpace(notice.PurchaseOrderId)
                    && poItemsByPurchaseOrderId.TryGetValue(notice.PurchaseOrderId, out var poLines))
                {
                    foreach (var pl in poLines)
                    {
                        if (!string.IsNullOrWhiteSpace(pl.PN)) modelSet.Add(pl.PN.Trim());
                    }
                }

                qc.Model = modelSet.Count == 0 ? null : string.Join(", ", modelSet.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

                var soCodes = noticeItems
                    .Select(ni =>
                    {
                        if (string.IsNullOrWhiteSpace(ni.PurchaseOrderItemId)) return string.Empty;
                        if (!poItemMap.TryGetValue(ni.PurchaseOrderItemId, out var poi)) return string.Empty;
                        if (string.IsNullOrWhiteSpace(poi.SellOrderItemId)) return string.Empty;
                        return sellOrderCodeBySellOrderItemId.TryGetValue(poi.SellOrderItemId, out var so) ? so : string.Empty;
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                qc.SalesOrderCode = soCodes.Count == 0 ? null : string.Join(", ", soCodes);
            }

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

            bool MatchModel(QCInfo qc, StockInNotify? notice, List<StockInNotifyItem> noticeItems, string keyword)
            {
                // 列表已聚合的型号串（含整单 PN）
                if (!string.IsNullOrWhiteSpace(qc.Model) && qc.Model.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return true;

                bool MatchNi(StockInNotifyItem ni)
                {
                    if (!string.IsNullOrWhiteSpace(ni.Pn) && ni.Pn.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (!string.IsNullOrWhiteSpace(ni.PurchaseOrderItemId)
                        && poItemMap.TryGetValue(ni.PurchaseOrderItemId, out var poi)
                        && !string.IsNullOrWhiteSpace(poi.PN)
                        && poi.PN.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        return true;
                    return false;
                }

                if (noticeItems.Any(MatchNi))
                    return true;

                if (qc.Items is { Count: > 0 })
                {
                    foreach (var qci in qc.Items)
                    {
                        if (notifyItemById.TryGetValue(qci.StockInNotifyItemId, out var ni) && MatchNi(ni))
                            return true;
                    }
                }

                // 整单采购明细 PN（与到货通知行可能不一致）
                if (notice != null
                    && !string.IsNullOrWhiteSpace(notice.PurchaseOrderId)
                    && poItemsByPurchaseOrderId.TryGetValue(notice.PurchaseOrderId, out var polines))
                {
                    if (polines.Any(p => !string.IsNullOrWhiteSpace(p.PN) && p.PN.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }

                return false;
            }

            return list.Where(qc =>
            {
                noticeMap.TryGetValue(qc.StockInNotifyId, out var notice);
                noticeItemsMap.TryGetValue(qc.StockInNotifyId, out var noticeItems);
                noticeItems ??= new List<StockInNotifyItem>();

                if (!string.IsNullOrWhiteSpace(vendorKeyword)
                    && !(notice?.VendorName?.Contains(vendorKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;

                if (!string.IsNullOrWhiteSpace(poCodeKeyword)
                    && !(notice?.PurchaseOrderCode?.Contains(poCodeKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;

                if (!string.IsNullOrWhiteSpace(modelKeyword) && !MatchModel(qc, notice, noticeItems, modelKeyword))
                    return false;

                if (!string.IsNullOrWhiteSpace(soCodeKeyword))
                {
                    var soMatched = noticeItems.Any(ni =>
                        !string.IsNullOrWhiteSpace(ni.PurchaseOrderItemId)
                        && poItemMap.TryGetValue(ni.PurchaseOrderItemId, out var poi)
                        && !string.IsNullOrWhiteSpace(poi.SellOrderItemId)
                        && sellOrderCodeBySellOrderItemId.TryGetValue(poi.SellOrderItemId, out var soCode)
                        && !string.IsNullOrWhiteSpace(soCode)
                        && soCode.Contains(soCodeKeyword, StringComparison.OrdinalIgnoreCase));
                    if (!soMatched) return false;
                }

                return true;
            }).ToList();
        }

        public async Task<QCInfo> CreateQcAsync(CreateQcRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StockInNotifyId))
                throw new ArgumentException("到货通知ID不能为空", nameof(request.StockInNotifyId));

            var existed = (await _qcRepo.FindAsync(x => x.StockInNotifyId == request.StockInNotifyId)).FirstOrDefault();
            if (existed != null) return existed;

            var notice = await _notifyRepo.GetByIdAsync(request.StockInNotifyId) ?? throw new InvalidOperationException("到货通知不存在");
            var noticeItems = (await _notifyItemRepo.FindAsync(x => x.StockInNotifyId == notice.Id)).ToList();
            if (!noticeItems.Any()) throw new InvalidOperationException("到货通知无明细，无法创建质检");

            var passQty = noticeItems.Sum(x => x.ArrivedQty);
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
                CreateTime = DateTime.UtcNow
            };
            await _qcRepo.AddAsync(qc);

            foreach (var x in noticeItems)
            {
                await _qcItemRepo.AddAsync(new QCItem
                {
                    Id = Guid.NewGuid().ToString(),
                    QcInfoId = qc.Id,
                    StockInNotifyItemId = x.Id,
                    ArrivedQty = x.ArrivedQty,
                    PassedQty = x.ArrivedQty,
                    RejectQty = 0,
                    CreateTime = DateTime.UtcNow
                });
            }

            notice.Status = 30;
            notice.ModifyTime = DateTime.UtcNow;
            await _notifyRepo.UpdateAsync(notice);

            await _unitOfWork.SaveChangesAsync();
            qc.Items = (await _qcItemRepo.FindAsync(x => x.QcInfoId == qc.Id)).ToList();
            return qc;
        }

        public async Task<QCInfo> UpdateQcResultAsync(string id, UpdateQcResultRequest request)
        {
            var qc = await _qcRepo.GetByIdAsync(id) ?? throw new InvalidOperationException("质检单不存在");
            var items = (await _qcItemRepo.FindAsync(x => x.QcInfoId == qc.Id)).ToList();
            if (!items.Any()) throw new InvalidOperationException("质检明细为空");

            var arrivedTotal = items.Sum(x => x.ArrivedQty);
            qc.PassQty = request.PassQty;
            qc.RejectQty = request.RejectQty;
            qc.Status = request.Result switch
            {
                "pass" => 100,
                "partial" => 10,
                "reject" => -1,
                _ => throw new ArgumentException("质检结果不合法", nameof(request.Result))
            };
            qc.StockInStatus = qc.Status switch
            {
                100 => 100,
                10 => 10,
                -1 => -1,
                _ => 1
            };
            qc.ModifyTime = DateTime.UtcNow;
            await _qcRepo.UpdateAsync(qc);

            var ratio = arrivedTotal > 0 ? request.PassQty / arrivedTotal : 0;
            foreach (var item in items)
            {
                item.PassedQty = decimal.Round(item.ArrivedQty * ratio, 4, MidpointRounding.AwayFromZero);
                item.RejectQty = Math.Max(0, item.ArrivedQty - item.PassedQty);
                item.ModifyTime = DateTime.UtcNow;
                await _qcItemRepo.UpdateAsync(item);
            }

            var notice = await _notifyRepo.GetByIdAsync(qc.StockInNotifyId);
            if (notice != null)
            {
                notice.Status = 30;
                notice.ModifyTime = DateTime.UtcNow;
                await _notifyRepo.UpdateAsync(notice);

                var notifyItems = (await _notifyItemRepo.FindAsync(x => x.StockInNotifyId == notice.Id)).ToDictionary(x => x.Id, x => x);
                foreach (var qci in items)
                {
                    if (!notifyItems.TryGetValue(qci.StockInNotifyItemId, out var ni)) continue;
                    ni.PassedQty = qci.PassedQty;
                    ni.ModifyTime = DateTime.UtcNow;
                    await _notifyItemRepo.UpdateAsync(ni);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            qc.Items = items;
            return qc;
        }

        public async Task BindQcStockInAsync(string id, string stockInId)
        {
            if (string.IsNullOrWhiteSpace(stockInId))
                throw new ArgumentException("入库单ID不能为空", nameof(stockInId));

            var qc = await _qcRepo.GetByIdAsync(id) ?? throw new InvalidOperationException("质检单不存在");
            qc.StockInId = stockInId;
            qc.StockInStatus = qc.Status == 100 ? (short)100 : qc.Status == 10 ? (short)10 : qc.StockInStatus;
            qc.ModifyTime = DateTime.UtcNow;
            await _qcRepo.UpdateAsync(qc);

            var notice = await _notifyRepo.GetByIdAsync(qc.StockInNotifyId);
            if (notice != null)
            {
                notice.Status = 100;
                notice.ModifyTime = DateTime.UtcNow;
                await _notifyRepo.UpdateAsync(notice);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task HandleStockInCompletedAsync(string stockInId, string? purchaseOrderId)
        {
            if (string.IsNullOrWhiteSpace(stockInId)) return;

            var qcs = (await _qcRepo.FindAsync(x => x.StockInId == stockInId)).ToList();
            var relatedNotices = new List<StockInNotify>();
            var hasChanges = false;

            foreach (var qc in qcs)
            {
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

            var poIds = relatedNotices.Select(x => x.PurchaseOrderId).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet();
            if (!string.IsNullOrWhiteSpace(purchaseOrderId)) poIds.Add(purchaseOrderId);

            var allNotifyItems = (await _notifyItemRepo.GetAllAsync()).ToList();
            foreach (var poId in poIds)
            {
                var po = await _poRepo.GetByIdAsync(poId);
                if (po == null) continue;

                var poItems = (await _poItemRepo.FindAsync(x => x.PurchaseOrderId == poId)).ToList();
                var noticeIds = relatedNotices.Where(x => x.PurchaseOrderId == poId).Select(x => x.Id).ToHashSet();
                var notifyItems = allNotifyItems.Where(x => noticeIds.Contains(x.StockInNotifyId)).ToList();
                var passedMap = notifyItems
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
                await _unitOfWork.SaveChangesAsync();
            }
        }

    }
}
