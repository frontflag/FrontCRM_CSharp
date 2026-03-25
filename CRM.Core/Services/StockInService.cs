using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    /// <summary>
    /// 入库服务实现
    /// </summary>
    public class StockInService : IStockInService
    {
        private readonly IRepository<StockIn> _stockInRepository;
        private readonly IRepository<StockInItem> _stockInItemRepository;
        private readonly IRepository<PurchaseOrder> _purchaseOrderRepository;
        private readonly IRepository<QCInfo> _qcRepository;
        private readonly IRepository<VendorInfo> _vendorRepository;
        private readonly ILogisticsService _logisticsService;
        private readonly IInventoryCenterService _inventoryCenterService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;

        public StockInService(
            IRepository<StockIn> stockInRepository,
            IRepository<StockInItem> stockInItemRepository,
            IRepository<PurchaseOrder> purchaseOrderRepository,
            IRepository<QCInfo> qcRepository,
            IRepository<VendorInfo> vendorRepository,
            ILogisticsService logisticsService,
            IInventoryCenterService inventoryCenterService,
            ISerialNumberService serialNumberService,
            IUnitOfWork unitOfWork)
        {
            _stockInRepository = stockInRepository;
            _stockInItemRepository = stockInItemRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _qcRepository = qcRepository;
            _vendorRepository = vendorRepository;
            _logisticsService = logisticsService;
            _inventoryCenterService = inventoryCenterService;
            _serialNumberService = serialNumberService;
            _unitOfWork = unitOfWork;
        }

        public async Task<StockIn> CreateAsync(CreateStockInRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.WarehouseId))
                throw new ArgumentException("仓库ID不能为空", nameof(request.WarehouseId));

            var stockInCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.StockIn);

            var stockInId = Guid.NewGuid().ToString();
            var purchaseOrderId = request.PurchaseOrderId?.Trim();
            var stockIn = new StockIn
            {
                Id = stockInId,
                StockInCode = stockInCode,
                StockInType = 1, // 采购入库
                // SourceCode 最大 32，避免直接写入 36 位 GUID 导致保存失败
                SourceCode = !string.IsNullOrWhiteSpace(purchaseOrderId)
                    ? purchaseOrderId!.Length > 32 ? purchaseOrderId[..32] : purchaseOrderId
                    : null,
                // 保留完整来源单ID，供入库完成回写使用
                SourceId = purchaseOrderId,
                WarehouseId = request.WarehouseId,
                VendorId = request.VendorId,
                StockInDate = PostgreSqlDateTime.ToUtc(request.StockInDate),
                TotalQuantity = request.TotalQuantity,
                Remark = request.Remark,
                Status = 0, // 草稿
                CreateTime = DateTime.UtcNow
            };

            await _stockInRepository.AddAsync(stockIn);
            // 先落主单，避免后续插入明细时触发 FK(stockinitem.stockinid -> stockin.stockinid) 失败
            await _unitOfWork.SaveChangesAsync();

            if (request.Items != null && request.Items.Count > 0)
            {
                decimal totalAmount = 0;
                foreach (var item in request.Items)
                {
                    var price = item.UnitPrice ?? 0;
                    var amount = item.Quantity * price;
                    totalAmount += amount;
                    var line = new StockInItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        StockInId = stockInId,
                        MaterialId = item.MaterialCode?.Trim() ?? string.Empty,
                        Quantity = item.Quantity,
                        Price = price,
                        Amount = amount,
                        BatchNo = item.BatchNo?.Trim(),
                        LocationId = !string.IsNullOrWhiteSpace(item.WarehouseLocation) ? item.WarehouseLocation.Trim() : null,
                        CreateTime = DateTime.UtcNow
                    };
                    await _stockInItemRepository.AddAsync(line);
                }
                stockIn.TotalAmount = totalAmount;
                await _stockInRepository.UpdateAsync(stockIn);
            }

            await _unitOfWork.SaveChangesAsync();
            return stockIn;
        }

        public async Task<StockIn?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _stockInRepository.GetByIdAsync(id);
        }

        public async Task<IReadOnlyList<StockInListItemDto>> GetListAsync()
        {
            var raw = (await _stockInRepository.GetAllAsync())
                .OrderByDescending(x => x.StockInDate)
                .ThenByDescending(x => x.CreateTime)
                .ToList();

            if (raw.Count == 0)
                return Array.Empty<StockInListItemDto>();

            var sourceIds = raw.Select(x => x.SourceId).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            var stockInIds = raw.Select(x => x.Id).ToList();
            var vendorIds = raw.Select(x => x.VendorId).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

            var poDict = sourceIds.Count == 0
                ? new Dictionary<string, PurchaseOrder>()
                : (await _purchaseOrderRepository.FindAsync(p => sourceIds.Contains(p.Id))).ToDictionary(p => p.Id);

            var qcById = sourceIds.Count == 0
                ? new Dictionary<string, QCInfo>()
                : (await _qcRepository.FindAsync(q => sourceIds.Contains(q.Id))).ToDictionary(q => q.Id);

            var qcByStockInId = (await _qcRepository.FindAsync(q => q.StockInId != null && stockInIds.Contains(q.StockInId!)))
                .GroupBy(q => q.StockInId!, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            var venDict = vendorIds.Count == 0
                ? new Dictionary<string, VendorInfo>()
                : (await _vendorRepository.FindAsync(v => vendorIds.Contains(v.Id))).ToDictionary(v => v.Id);

            var result = new List<StockInListItemDto>(raw.Count);
            foreach (var s in raw)
            {
                string? sourceDisplay = null;
                if (!string.IsNullOrWhiteSpace(s.SourceId))
                {
                    if (poDict.TryGetValue(s.SourceId!, out var po))
                        sourceDisplay = po.PurchaseOrderCode;
                    else if (qcById.TryGetValue(s.SourceId!, out var qc))
                        sourceDisplay = qc.QcCode;
                }
                if (string.IsNullOrWhiteSpace(sourceDisplay) && qcByStockInId.TryGetValue(s.Id, out var qcLinked))
                    sourceDisplay = qcLinked.QcCode;
                if (string.IsNullOrWhiteSpace(sourceDisplay))
                    sourceDisplay = string.IsNullOrWhiteSpace(s.SourceCode) ? null : s.SourceCode;

                string? vendorName = null;
                if (!string.IsNullOrWhiteSpace(s.VendorId) && venDict.TryGetValue(s.VendorId!, out var v))
                {
                    vendorName = !string.IsNullOrWhiteSpace(v.OfficialName) ? v.OfficialName
                        : !string.IsNullOrWhiteSpace(v.NickName) ? v.NickName
                        : v.Code;
                }

                result.Add(new StockInListItemDto
                {
                    Id = s.Id,
                    StockInCode = s.StockInCode,
                    StockInType = s.StockInType,
                    SourceDisplayNo = sourceDisplay,
                    WarehouseId = s.WarehouseId,
                    VendorId = s.VendorId,
                    VendorName = vendorName,
                    StockInDate = s.StockInDate,
                    TotalQuantity = s.TotalQuantity,
                    TotalAmount = s.TotalAmount,
                    Status = s.Status,
                    Remark = s.Remark,
                    CreateTime = s.CreateTime
                });
            }

            return result;
        }

        public async Task<StockIn> UpdateAsync(string id, UpdateStockInRequest request)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var stockIn = await _stockInRepository.GetByIdAsync(id);
            if (stockIn == null)
                throw new InvalidOperationException($"入库单 {id} 不存在");

            if (!string.IsNullOrWhiteSpace(request.Remark))
                stockIn.Remark = request.Remark;

            stockIn.ModifyTime = DateTime.UtcNow;

            await _stockInRepository.UpdateAsync(stockIn);
            await _unitOfWork.SaveChangesAsync();
            return stockIn;
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var stockIn = await _stockInRepository.GetByIdAsync(id);
            if (stockIn == null)
                throw new InvalidOperationException($"入库单 {id} 不存在");

            await _stockInRepository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var stockIn = await _stockInRepository.GetByIdAsync(id);
            if (stockIn == null)
                throw new InvalidOperationException($"入库单 {id} 不存在");

            // 幂等保护：状态未变化时直接返回，避免重复点击导致重复推进回写
            if (stockIn.Status == status)
                return;

            stockIn.Status = status;
            stockIn.ModifyTime = DateTime.UtcNow;

            await _stockInRepository.UpdateAsync(stockIn);
            await _unitOfWork.SaveChangesAsync();

            if (status == 2)
            {
                await _inventoryCenterService.PostStockInAsync(stockIn.Id);
                await _logisticsService.HandleStockInCompletedAsync(
                    stockIn.Id,
                    !string.IsNullOrWhiteSpace(stockIn.SourceId) ? stockIn.SourceId : stockIn.SourceCode);
            }
        }
    }
}
