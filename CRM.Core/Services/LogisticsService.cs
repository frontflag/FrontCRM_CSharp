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
        private readonly IRepository<QCInfo> _qcRepo;
        private readonly IRepository<QCItem> _qcItemRepo;
        private readonly IRepository<PurchaseOrder> _poRepo;
        private readonly IRepository<PurchaseOrderItem> _poItemRepo;
        private readonly IRepository<PurchaseOrderItemExtend> _poItemExtendRepo;
        private readonly IRepository<SellOrderItem> _sellOrderItemRepo;
        private readonly IRepository<SellOrder> _sellOrderRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;

        public LogisticsService(
            IRepository<StockInNotify> notifyRepo,
            IRepository<QCInfo> qcRepo,
            IRepository<QCItem> qcItemRepo,
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IRepository<PurchaseOrderItemExtend> poItemExtendRepo,
            IRepository<SellOrderItem> sellOrderItemRepo,
            IRepository<SellOrder> sellOrderRepo,
            ISerialNumberService serialNumberService,
            IUnitOfWork unitOfWork)
        {
            _notifyRepo = notifyRepo;
            _qcRepo = qcRepo;
            _qcItemRepo = qcItemRepo;
            _poRepo = poRepo;
            _poItemRepo = poItemRepo;
            _poItemExtendRepo = poItemExtendRepo;
            _sellOrderItemRepo = sellOrderItemRepo;
            _sellOrderRepo = sellOrderRepo;
            _serialNumberService = serialNumberService;
            _unitOfWork = unitOfWork;
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

            await EnsurePurchaseOrderItemExtendAsync(poItem);
            await RecalcPurchaseOrderItemExtendAsync(poItem.Id, poItem.Qty);
            await _unitOfWork.SaveChangesAsync();

            var ext = await _poItemExtendRepo.GetByIdAsync(poItem.Id)
                      ?? throw new InvalidOperationException("采购明细扩展不存在");
            if (expectQty > ext.QtyStockInNotifyNot)
                throw new InvalidOperationException($"预期到货数量不能大于剩余可通知数量（当前剩余 {ext.QtyStockInNotifyNot}）");

            var noticeCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.ArrivalNotice);
            var expectedArrival = request.ExpectedArrivalDate ?? poItem.DeliveryDate ?? po.DeliveryDate;
            var expectTotal = Math.Round(expectQty * poItem.Cost, 2, MidpointRounding.AwayFromZero);

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
                Pn = poItem.PN,
                Brand = poItem.Brand,
                ExpectQty = expectQty,
                ReceiveQty = 0,
                PassedQty = 0,
                Cost = poItem.Cost,
                ExpectTotal = expectTotal,
                ReceiveTotal = 0,
                CreateTime = DateTime.UtcNow
            };
            await _notifyRepo.AddAsync(notice);
            await _unitOfWork.SaveChangesAsync();

            await RecalcPurchaseOrderItemExtendAsync(poItem.Id, poItem.Qty);
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
                    await RecalcPurchaseOrderItemExtendAsync(line.Id, line.Qty);
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
            var poItemsByPurchaseOrderId = poItems
                .GroupBy(x => x.PurchaseOrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

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
                    && poItemsByPurchaseOrderId.TryGetValue(notice.PurchaseOrderId, out var poLines))
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
                    && poItemsByPurchaseOrderId.TryGetValue(notice.PurchaseOrderId, out var poLinesBrand))
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

        public async Task<QCInfo> CreateQcAsync(CreateQcRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StockInNotifyId))
                throw new ArgumentException("到货通知ID不能为空", nameof(request.StockInNotifyId));

            var existed = (await _qcRepo.FindAsync(x => x.StockInNotifyId == request.StockInNotifyId)).FirstOrDefault();
            if (existed != null) return existed;

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
                CreateTime = DateTime.UtcNow
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
            // StockInStatus：入库进度（见 QCInfo 注释），与质检 Status 的 10/100 不可混用；保存质检后尚未入库
            qc.StockInStatus = request.Result == "reject" ? (short)-1 : (short)1;
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
                notice.PassedQty = items.Sum(x => x.PassedQty);
                notice.ReceiveQty = notice.PassedQty;
                notice.ReceiveTotal = Math.Round(notice.ReceiveQty * notice.Cost, 2, MidpointRounding.AwayFromZero);
                notice.ModifyTime = DateTime.UtcNow;
                await _notifyRepo.UpdateAsync(notice);

                if (!string.IsNullOrWhiteSpace(notice.PurchaseOrderItemId))
                    await RecalcPurchaseOrderItemExtendAsync(notice.PurchaseOrderItemId,
                        (await _poItemRepo.GetByIdAsync(notice.PurchaseOrderItemId))?.Qty ?? 0);
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
            // 仅绑定入库单；入库进度在过账完成时由 HandleStockInCompletedAsync 回写
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

            foreach (var poId in poIds)
            {
                var po = await _poRepo.GetByIdAsync(poId);
                if (po == null) continue;

                var poItems = (await _poItemRepo.FindAsync(x => x.PurchaseOrderId == poId)).ToList();
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

        private async Task<PurchaseOrderItemExtend> EnsurePurchaseOrderItemExtendAsync(PurchaseOrderItem poItem)
        {
            var ext = await _poItemExtendRepo.GetByIdAsync(poItem.Id);
            if (ext != null) return ext;

            var lineTotal = Math.Round(poItem.Qty * poItem.Cost, 2, MidpointRounding.AwayFromZero);
            ext = new PurchaseOrderItemExtend
            {
                Id = poItem.Id,
                QtyStockInNotifyNot = poItem.Qty,
                PurchaseInvoiceAmount = lineTotal,
                PurchaseInvoiceToBe = lineTotal,
                PaymentAmount = lineTotal,
                PaymentAmountNot = lineTotal,
                CreateTime = DateTime.UtcNow
            };
            await _poItemExtendRepo.AddAsync(ext);
            await _unitOfWork.SaveChangesAsync();
            return ext;
        }

        /// <summary>
        /// 文档：QtyStockInNotifyNot = 采购数量 - 累计实收 - 在途(Expect-Receive 未结批次之和)
        /// </summary>
        private async Task RecalcPurchaseOrderItemExtendAsync(string purchaseOrderItemId, decimal poLineQty)
        {
            var ext = await _poItemExtendRepo.GetByIdAsync(purchaseOrderItemId);
            if (ext == null) return;

            var lines = (await _notifyRepo.FindAsync(x => x.PurchaseOrderItemId == purchaseOrderItemId)).ToList();
            var sumReceive = lines.Sum(x => x.ReceiveQty);
            var inTransit = lines.Sum(x => Math.Max(0m, x.ExpectQty - x.ReceiveQty));
            ext.QtyReceiveTotal = sumReceive;
            ext.QtyStockInNotifyExpectSum = lines.Sum(x => x.ExpectQty);
            ext.QtyStockInNotifyNot = Math.Max(0m, poLineQty - sumReceive - inTransit);
            ext.ModifyTime = DateTime.UtcNow;
            await _poItemExtendRepo.UpdateAsync(ext);
        }
    }
}
