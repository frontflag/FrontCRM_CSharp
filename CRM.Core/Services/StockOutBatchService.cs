using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Services;

public class StockOutBatchService : IStockOutBatchService
{
    private readonly IRepository<StockOutBatch> _repository;
    private readonly IRepository<StockInBatch> _stockInBatchRepository;
    private readonly IRepository<Packing> _packingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StockOutBatchService(
        IRepository<StockOutBatch> repository,
        IRepository<StockInBatch> stockInBatchRepository,
        IRepository<Packing> packingRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _stockInBatchRepository = stockInBatchRepository;
        _packingRepository = packingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StockOutBatchImportResultDto> ImportAsync(
        StockOutBatchImportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var packingId = (request.PackingId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(packingId))
            throw new InvalidOperationException("装箱单 ID 不能为空");

        var packing = await _packingRepository.GetByIdAsync(packingId);
        if (packing == null)
            throw new InvalidOperationException("装箱单不存在");
        if (packing.Status < PackingStatusCode.Confirmed)
            throw new InvalidOperationException("仅已确认及之后状态的装箱单可录入出库批次");

        var rows = (request.Rows ?? new List<StockOutBatchImportRowRequest>())
            .Where(r => !IsImportRowEmpty(r))
            .ToList();
        if (rows.Count == 0)
        {
            throw new InvalidOperationException(
                "没有可导入的有效行：请确认第 1 行为表头、从第 2 行起填写，且批次出库数量大于 0。");
        }

        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            var globalNo = (r.GlobalBatchNo ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(globalNo))
                throw new InvalidOperationException("批次全局唯一编号不能为空");
            if (r.OutQty <= 0)
                throw new InvalidOperationException($"批次 {globalNo} 的出库数量须为正整数");
            if (!seenInFile.Add(globalNo))
                throw new InvalidOperationException($"Excel 中批次全局唯一编号「{globalNo}」重复，请合并为一行");
        }

        var globalNos = seenInFile.ToList();
        var existingOnPacking = (await _repository.FindAsync(b => b.PackingId == packingId)).ToList();
        var duplicateOnPacking = existingOnPacking
            .Select(b => b.GlobalBatchNo?.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var g in globalNos)
        {
            if (duplicateOnPacking.Contains(g))
            {
                throw new InvalidOperationException(
                    $"装箱单已存在批次「{g}」的出库记录，请勿重复导入（可先删除原记录后再导入）");
            }
        }

        var inBatches = (await _stockInBatchRepository.FindAsync(b => globalNos.Contains(b.GlobalBatchNo))).ToList();
        var inBatchByGlobal = inBatches
            .GroupBy(b => b.GlobalBatchNo.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var g in globalNos)
        {
            if (!inBatchByGlobal.ContainsKey(g))
                throw new InvalidOperationException($"批次全局唯一编号「{g}」在入库批次中不存在");
        }

        var allOutForGlobals = (await _repository.FindAsync(b => globalNos.Contains(b.GlobalBatchNo))).ToList();
        var usedOutQty = allOutForGlobals
            .GroupBy(b => b.GlobalBatchNo.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.OutQty), StringComparer.OrdinalIgnoreCase);

        var pendingInImport = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            var globalNo = r.GlobalBatchNo!.Trim();
            var inBatch = inBatchByGlobal[globalNo];
            usedOutQty.TryGetValue(globalNo, out var alreadyOut);
            pendingInImport.TryGetValue(globalNo, out var pendingOut);
            var remaining = inBatch.BatchQty - alreadyOut - pendingOut;
            if (r.OutQty > remaining)
            {
                throw new InvalidOperationException(
                    $"批次「{globalNo}」剩余可出 {Math.Max(0, remaining)}，本次导入 {r.OutQty} 超出余额（入库数量 {inBatch.BatchQty}，已出库 {alreadyOut}）");
            }

            pendingInImport[globalNo] = pendingOut + r.OutQty;
        }

        var entities = rows.Select(r => new StockOutBatch
        {
            Id = Guid.NewGuid().ToString(),
            PackingId = packingId,
            GlobalBatchNo = r.GlobalBatchNo!.Trim(),
            OutQty = r.OutQty
        }).ToList();

        foreach (var e in entities)
            await _repository.AddAsync(e);
        await _unitOfWork.SaveChangesAsync();

        return new StockOutBatchImportResultDto { ImportedCount = entities.Count };
    }

    public async Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id 不能为空", nameof(id));

        var entity = await _repository.GetByIdAsync(id.Trim());
        if (entity == null)
            throw new InvalidOperationException("出库批次记录不存在");

        entity.IsDeleted = true;
        await _repository.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
    }

    private static bool IsImportRowEmpty(StockOutBatchImportRowRequest r) =>
        string.IsNullOrWhiteSpace(r.GlobalBatchNo) && r.OutQty <= 0;
}
