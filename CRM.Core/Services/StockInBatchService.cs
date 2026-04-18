using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Services
{
    public class StockInBatchService : IStockInBatchService
    {
        private readonly IRepository<StockInBatch> _repository;
        private readonly IRepository<StockIn> _stockInRepository;
        private readonly IRepository<StockInItem> _stockInItemRepository;
        private readonly IUnitOfWork _unitOfWork;

        public StockInBatchService(
            IRepository<StockInBatch> repository,
            IRepository<StockIn> stockInRepository,
            IRepository<StockInItem> stockInItemRepository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _stockInRepository = stockInRepository;
            _stockInItemRepository = stockInItemRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<StockInBatch>> ListAsync(StockInBatchListQuery? query, CancellationToken cancellationToken = default)
        {
            query ??= new StockInBatchListQuery();
            var all = (await _repository.GetAllAsync()).ToList();
            var codeNeedle = query.StockInItemCode?.Trim();
            var lotNeedle = query.Lot?.Trim();
            var snNeedle = query.SerialNumber?.Trim();

            IEnumerable<StockInBatch> q = all;
            if (!string.IsNullOrEmpty(codeNeedle))
            {
                q = q.Where(x =>
                    !string.IsNullOrEmpty(x.StockInItemCode) &&
                    x.StockInItemCode.Contains(codeNeedle, StringComparison.OrdinalIgnoreCase));
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
                .ThenBy(x => x.StockInItemCode, StringComparer.OrdinalIgnoreCase)
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

            entity.MaterialModel = NullIfWhiteSpace(request.MaterialModel);
            entity.Dc = NullIfWhiteSpace(request.Dc);
            entity.PackageOrigin = NullIfWhiteSpace(request.PackageOrigin);
            entity.WaferOrigin = NullIfWhiteSpace(request.WaferOrigin);
            entity.Lot = NullIfWhiteSpace(request.Lot);
            entity.LotQtyIn = request.LotQtyIn;
            entity.LotQtyOut = request.LotQtyOut;
            entity.Origin = NullIfWhiteSpace(request.Origin);
            entity.SerialNumber = NullIfWhiteSpace(request.SerialNumber);
            entity.SnQtyIn = request.SnQtyIn;
            entity.SnQtyOut = request.SnQtyOut;
            entity.FirmwareVersion = NullIfWhiteSpace(request.FirmwareVersion);
            entity.Remark = NullIfWhiteSpace(request.Remark);

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return entity;
        }

        public async Task<int> ImportAsync(StockInBatchImportRequest request, CancellationToken cancellationToken = default)
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

            var line = await _stockInItemRepository.GetByIdAsync(stockInItemId);
            if (line == null)
                throw new InvalidOperationException("入库明细不存在");
            if (!string.Equals((line.StockInId ?? string.Empty).Trim(), stockInId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("入库明细不属于当前入库单");

            var itemCode = NullIfWhiteSpace(request.StockInItemCode) ?? NullIfWhiteSpace(line.StockInItemCode);
            var rows = request.Rows ?? new List<StockInBatchImportRowRequest>();
            var entities = new List<StockInBatch>();

            foreach (var r in rows)
            {
                if (IsImportRowEmpty(r))
                    continue;
                if (r.LotQtyIn < 0 || r.SnQtyIn < 0)
                    throw new InvalidOperationException("LOT_入库数量、SN号_入库数量不能为负数");

                entities.Add(new StockInBatch
                {
                    Id = Guid.NewGuid().ToString(),
                    StockInId = stockInId,
                    StockInItemId = stockInItemId,
                    StockInItemCode = itemCode,
                    MaterialModel = NullIfWhiteSpace(r.MaterialModel),
                    Dc = NullIfWhiteSpace(r.Dc),
                    PackageOrigin = NullIfWhiteSpace(r.PackageOrigin),
                    WaferOrigin = NullIfWhiteSpace(r.WaferOrigin),
                    Lot = NullIfWhiteSpace(r.Lot),
                    LotQtyIn = r.LotQtyIn,
                    LotQtyOut = 0,
                    Origin = NullIfWhiteSpace(r.Origin),
                    SerialNumber = NullIfWhiteSpace(r.SerialNumber),
                    SnQtyIn = r.SnQtyIn,
                    SnQtyOut = 0,
                    FirmwareVersion = NullIfWhiteSpace(r.FirmwareVersion),
                    Remark = NullIfWhiteSpace(r.Remark)
                });
            }

            if (entities.Count == 0)
                throw new InvalidOperationException("没有可导入的有效行：请确认第 1 行为表头、从第 2 行起填写数据，且至少有一列有内容或数量非零。");

            foreach (var e in entities)
                await _repository.AddAsync(e);
            await _unitOfWork.SaveChangesAsync();
            return entities.Count;
        }

        public async Task<StockInBatchWriteOffResultDto> ApplyWriteOffAsync(
            StockInBatchWriteOffRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var rows = request.Rows ?? new List<StockInBatchWriteOffRowRequest>();
            var all = (await _repository.GetAllAsync()).ToList();

            var byLot = all
                .Where(b => !string.IsNullOrWhiteSpace(b.Lot))
                .GroupBy(b => b.Lot!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var bySn = all
                .Where(b => !string.IsNullOrWhiteSpace(b.SerialNumber))
                .GroupBy(b => b.SerialNumber!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var invalidLots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var invalidSns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in rows)
            {
                if (IsWriteOffRowEmpty(r))
                    continue;
                if (r.LotWriteOffQty < 0 || r.SnWriteOffQty < 0)
                    throw new InvalidOperationException("LOT_核销数量、SN号_核销数量不能为负数");

                if (r.LotWriteOffQty > 0)
                {
                    var lotKey = (r.Lot ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(lotKey))
                        invalidLots.Add("(LOT为空)");
                    else if (!byLot.TryGetValue(lotKey, out var list) || list.Count == 0)
                        invalidLots.Add(lotKey);
                    else if (list.Count > 1)
                        invalidLots.Add(lotKey);
                }

                if (r.SnWriteOffQty > 0)
                {
                    var snKey = (r.SerialNumber ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(snKey))
                        invalidSns.Add("(SN号为空)");
                    else if (!bySn.TryGetValue(snKey, out var list) || list.Count == 0)
                        invalidSns.Add(snKey);
                    else if (list.Count > 1)
                        invalidSns.Add(snKey);
                }
            }

            if (invalidLots.Count > 0 || invalidSns.Count > 0)
            {
                return new StockInBatchWriteOffResultDto
                {
                    ValidationPassed = false,
                    UpdatedRowCount = 0,
                    InvalidLots = invalidLots.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
                    InvalidSerialNumbers = invalidSns.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList()
                };
            }

            var deltas = new Dictionary<string, (int Lot, int Sn)>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in rows)
            {
                if (IsWriteOffRowEmpty(r))
                    continue;

                if (r.LotWriteOffQty > 0)
                {
                    var lotKey = r.Lot!.Trim();
                    var entity = byLot[lotKey][0];
                    if (!deltas.TryGetValue(entity.Id, out var d))
                        d = (0, 0);
                    deltas[entity.Id] = (d.Lot + r.LotWriteOffQty, d.Sn);
                }

                if (r.SnWriteOffQty > 0)
                {
                    var snKey = r.SerialNumber!.Trim();
                    var entity = bySn[snKey][0];
                    if (!deltas.TryGetValue(entity.Id, out var d))
                        d = (0, 0);
                    deltas[entity.Id] = (d.Lot, d.Sn + r.SnWriteOffQty);
                }
            }

            if (deltas.Count == 0)
            {
                return new StockInBatchWriteOffResultDto
                {
                    ValidationPassed = true,
                    UpdatedRowCount = 0,
                    InvalidLots = new List<string>(),
                    InvalidSerialNumbers = new List<string>()
                };
            }

            var byId = all.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var (id, add) in deltas)
            {
                if (!byId.TryGetValue(id, out var entity))
                    continue;
                entity.LotQtyOut += add.Lot;
                entity.SnQtyOut += add.Sn;
                await _repository.UpdateAsync(entity);
            }

            await _unitOfWork.SaveChangesAsync();

            return new StockInBatchWriteOffResultDto
            {
                ValidationPassed = true,
                UpdatedRowCount = deltas.Count,
                InvalidLots = new List<string>(),
                InvalidSerialNumbers = new List<string>()
            };
        }

        private static bool IsWriteOffRowEmpty(StockInBatchWriteOffRowRequest r) =>
            string.IsNullOrWhiteSpace(r.Lot)
            && string.IsNullOrWhiteSpace(r.SerialNumber)
            && r.LotWriteOffQty == 0
            && r.SnWriteOffQty == 0;

        private static bool IsImportRowEmpty(StockInBatchImportRowRequest r) =>
            string.IsNullOrWhiteSpace(r.MaterialModel)
            && string.IsNullOrWhiteSpace(r.Dc)
            && string.IsNullOrWhiteSpace(r.PackageOrigin)
            && string.IsNullOrWhiteSpace(r.WaferOrigin)
            && string.IsNullOrWhiteSpace(r.Lot)
            && string.IsNullOrWhiteSpace(r.Origin)
            && string.IsNullOrWhiteSpace(r.SerialNumber)
            && string.IsNullOrWhiteSpace(r.FirmwareVersion)
            && string.IsNullOrWhiteSpace(r.Remark)
            && r.LotQtyIn == 0
            && r.SnQtyIn == 0;

        private static string? NullIfWhiteSpace(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
