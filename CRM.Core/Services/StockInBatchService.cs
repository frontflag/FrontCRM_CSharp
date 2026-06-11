using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Services
{
    public class StockInBatchService : IStockInBatchService
    {
        private readonly IRepository<StockInBatch> _repository;
        private readonly IRepository<StockOutBatch> _stockOutBatchRepository;
        private readonly IRepository<StockIn> _stockInRepository;
        private readonly IRepository<StockInItem> _stockInItemRepository;
        private readonly IBatchGlobalNumberService _batchGlobalNumberService;
        private readonly IUnitOfWork _unitOfWork;

        public StockInBatchService(
            IRepository<StockInBatch> repository,
            IRepository<StockOutBatch> stockOutBatchRepository,
            IRepository<StockIn> stockInRepository,
            IRepository<StockInItem> stockInItemRepository,
            IBatchGlobalNumberService batchGlobalNumberService,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _stockOutBatchRepository = stockOutBatchRepository;
            _stockInRepository = stockInRepository;
            _stockInItemRepository = stockInItemRepository;
            _batchGlobalNumberService = batchGlobalNumberService;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<StockInBatch>> ListAsync(StockInBatchListQuery? query, CancellationToken cancellationToken = default)
        {
            query ??= new StockInBatchListQuery();
            var all = (await _repository.GetAllAsync()).ToList();
            var globalNeedle = query.GlobalBatchNo?.Trim();
            var lotNeedle = query.Lot?.Trim();
            var snNeedle = query.SerialNumber?.Trim();

            IEnumerable<StockInBatch> q = all;
            if (!string.IsNullOrEmpty(globalNeedle))
            {
                q = q.Where(x =>
                    !string.IsNullOrEmpty(x.GlobalBatchNo) &&
                    x.GlobalBatchNo.Contains(globalNeedle, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(lotNeedle))
            {
                q = q.Where(x =>
                    !string.IsNullOrEmpty(x.Lot) &&
                    x.Lot.Contains(lotNeedle, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(snNeedle))
            {
                q = q.Where(x =>
                    !string.IsNullOrEmpty(x.SerialNumber) &&
                    x.SerialNumber.Contains(snNeedle, StringComparison.OrdinalIgnoreCase));
            }

            return q
                .OrderByDescending(x => x.CreateTime)
                .ThenBy(x => x.GlobalBatchNo, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<StockInBatch?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            return await _repository.GetByIdAsync(id.Trim());
        }

        public async Task<StockInBatch> UpdateAsync(string id, StockInBatchUpdateRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id 不能为空", nameof(id));
            var entity = await _repository.GetByIdAsync(id.Trim());
            if (entity == null)
                throw new InvalidOperationException("批次记录不存在");

            if (request.BatchQty < 0)
                throw new InvalidOperationException("批次数量不能为负数");

            entity.BatchDimension = NullIfWhiteSpace(request.BatchDimension);
            entity.BatchUnit = NullIfWhiteSpace(request.BatchUnit);
            entity.UnitNo = NullIfWhiteSpace(request.UnitNo);
            entity.BatchQty = request.BatchQty;
            entity.Dc = NullIfWhiteSpace(request.Dc);
            entity.PackageOrigin = NullIfWhiteSpace(request.PackageOrigin);
            entity.WaferOrigin = NullIfWhiteSpace(request.WaferOrigin);
            entity.Lot = NullIfWhiteSpace(request.Lot);
            entity.SerialNumber = NullIfWhiteSpace(request.SerialNumber);
            entity.FirmwareVersion = NullIfWhiteSpace(request.FirmwareVersion);
            entity.PartCode = NullIfWhiteSpace(request.PartCode);
            entity.Remark = NullIfWhiteSpace(request.Remark);

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return entity;
        }

        public async Task<StockInBatchImportResultDto> ImportAsync(
            StockInBatchImportRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var stockInId = (request.StockInId ?? string.Empty).Trim();
            var stockInItemId = (request.StockInItemId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(stockInId))
                throw new InvalidOperationException("入库单 ID 不能为空");
            if (string.IsNullOrEmpty(stockInItemId))
                throw new InvalidOperationException("入库明细 ID 不能为空");

            var header = await _stockInRepository.GetByIdAsync(stockInId);
            if (header == null)
                throw new InvalidOperationException("入库单不存在");
            if (header.Status != StockInHeaderStatusCode.Posted)
                throw new InvalidOperationException("仅已过账的入库单可录入批次");

            var line = await _stockInItemRepository.GetByIdAsync(stockInItemId);
            if (line == null)
                throw new InvalidOperationException("入库明细不存在");
            if (!string.Equals((line.StockInId ?? string.Empty).Trim(), stockInId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("入库明细不属于当前入库单");

            var rows = request.Rows ?? new List<StockInBatchImportRowRequest>();
            var entities = new List<StockInBatch>();
            var globalBatchNos = new List<string>();

            foreach (var r in rows)
            {
                if (IsImportRowEmpty(r))
                    continue;
                if (r.BatchQty <= 0)
                    throw new InvalidOperationException("批次数量须为正整数");

                var globalNo = await _batchGlobalNumberService.GenerateNextAsync(cancellationToken);
                globalBatchNos.Add(globalNo);

                entities.Add(new StockInBatch
                {
                    Id = Guid.NewGuid().ToString(),
                    StockInItemId = stockInItemId,
                    GlobalBatchNo = globalNo,
                    BatchDimension = NullIfWhiteSpace(r.BatchDimension),
                    BatchUnit = NullIfWhiteSpace(r.BatchUnit),
                    UnitNo = NullIfWhiteSpace(r.UnitNo),
                    BatchQty = r.BatchQty,
                    Dc = NullIfWhiteSpace(r.Dc),
                    PackageOrigin = NullIfWhiteSpace(r.PackageOrigin),
                    WaferOrigin = NullIfWhiteSpace(r.WaferOrigin),
                    Lot = NullIfWhiteSpace(r.Lot),
                    SerialNumber = NullIfWhiteSpace(r.SerialNumber),
                    FirmwareVersion = NullIfWhiteSpace(r.FirmwareVersion),
                    PartCode = NullIfWhiteSpace(r.PartCode),
                    Remark = NullIfWhiteSpace(r.Remark)
                });
            }

            if (entities.Count == 0)
            {
                throw new InvalidOperationException(
                    "没有可导入的有效行：请确认第 1 行为表头、从第 2 行起填写数据，且批次数量大于 0。");
            }

            foreach (var e in entities)
                await _repository.AddAsync(e);
            await _unitOfWork.SaveChangesAsync();

            return new StockInBatchImportResultDto
            {
                ImportedCount = entities.Count,
                GlobalBatchNos = globalBatchNos
            };
        }

        public async Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id 不能为空", nameof(id));

            var entity = await _repository.GetByIdAsync(id.Trim());
            if (entity == null)
                throw new InvalidOperationException("批次记录不存在");

            var globalNo = (entity.GlobalBatchNo ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(globalNo))
            {
                var linkedOut = (await _stockOutBatchRepository.FindAsync(b => b.GlobalBatchNo == globalNo)).Any();
                if (linkedOut)
                {
                    throw new InvalidOperationException(
                        $"批次「{globalNo}」已有出库记录，无法删除");
                }
            }

            entity.IsDeleted = true;
            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
        }

        private static bool IsImportRowEmpty(StockInBatchImportRowRequest r) =>
            string.IsNullOrWhiteSpace(r.BatchDimension)
            && string.IsNullOrWhiteSpace(r.BatchUnit)
            && string.IsNullOrWhiteSpace(r.UnitNo)
            && r.BatchQty <= 0
            && string.IsNullOrWhiteSpace(r.Dc)
            && string.IsNullOrWhiteSpace(r.PackageOrigin)
            && string.IsNullOrWhiteSpace(r.WaferOrigin)
            && string.IsNullOrWhiteSpace(r.Lot)
            && string.IsNullOrWhiteSpace(r.SerialNumber)
            && string.IsNullOrWhiteSpace(r.FirmwareVersion)
            && string.IsNullOrWhiteSpace(r.PartCode)
            && string.IsNullOrWhiteSpace(r.Remark);

        private static string? NullIfWhiteSpace(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
