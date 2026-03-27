using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;

        public LogisticsService(
            IRepository<StockInNotify> notifyRepo,
            IRepository<StockInNotifyItem> notifyItemRepo,
            IRepository<QCInfo> qcRepo,
            IRepository<QCItem> qcItemRepo,
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            ISerialNumberService serialNumberService,
            IUnitOfWork unitOfWork)
        {
            _notifyRepo = notifyRepo;
            _notifyItemRepo = notifyItemRepo;
            _qcRepo = qcRepo;
            _qcItemRepo = qcItemRepo;
            _poRepo = poRepo;
            _poItemRepo = poItemRepo;
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

        public async Task<IReadOnlyList<QCInfo>> GetQcsAsync()
        {
            var list = (await _qcRepo.GetAllAsync()).OrderByDescending(x => x.CreateTime).ToList();
            var items = (await _qcItemRepo.GetAllAsync()).ToList();
            var map = items.GroupBy(x => x.QcInfoId).ToDictionary(g => g.Key, g => (ICollection<QCItem>)g.ToList());
            foreach (var row in list)
            {
                row.Items = map.TryGetValue(row.Id, out var v) ? v : new List<QCItem>();
            }
            return list;
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
