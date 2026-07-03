using System.Text.Json;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Services;

public class StockOutBatchService : IStockOutBatchService
{
    private readonly IRepository<StockOutBatch> _repository;
    private readonly IRepository<StockInBatch> _stockInBatchRepository;
    private readonly IRepository<Packing> _packingRepository;
    private readonly ILogOperationAppendService _logOperationAppend;
    private readonly IUnitOfWork _unitOfWork;

    public StockOutBatchService(
        IRepository<StockOutBatch> repository,
        IRepository<StockInBatch> stockInBatchRepository,
        IRepository<Packing> packingRepository,
        ILogOperationAppendService logOperationAppend,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _stockInBatchRepository = stockInBatchRepository;
        _packingRepository = packingRepository;
        _logOperationAppend = logOperationAppend;
        _unitOfWork = unitOfWork;
    }

    public async Task<StockOutBatch?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        var entity = await _repository.GetByIdAsync(id.Trim());
        return entity is { IsDeleted: false } ? entity : null;
    }

    public async Task<StockOutBatchImportResultDto> ImportAsync(
        StockOutBatchImportRequest request,
        StockOutBatchOperationContext? context = null,
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
        var existingOnPacking = (await _repository.FindAsync(b => b.PackingId == packingId))
            .Where(b => !b.IsDeleted)
            .ToList();
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

        var inBatches = (await _stockInBatchRepository.FindAsync(b => globalNos.Contains(b.GlobalBatchNo)))
            .Where(b => !b.IsDeleted)
            .ToList();
        var inBatchByGlobal = inBatches
            .GroupBy(b => b.GlobalBatchNo.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var g in globalNos)
        {
            if (!inBatchByGlobal.ContainsKey(g))
                throw new InvalidOperationException($"批次全局唯一编号「{g}」在入库批次中不存在");
        }

        var allOutForGlobals = (await _repository.FindAsync(b => globalNos.Contains(b.GlobalBatchNo)))
            .Where(b => !b.IsDeleted)
            .ToList();
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

        var batchSummary = SummarizeBatchNos(entities.Select(e => e.GlobalBatchNo).ToList());
        await AppendBatchOperationLogAsync(
            packing,
            StockOutBatchOperationActionTypes.Import,
            $"导入出库批次 {entities.Count} 条：{batchSummary}",
            null,
            BuildExtraInfo(packing.Code, entities.Count, batchSummary, 0, null),
            context,
            cancellationToken);

        return new StockOutBatchImportResultDto { ImportedCount = entities.Count };
    }

    public async Task<StockOutBatch> UpdateAsync(
        string id,
        StockOutBatchUpdateRequest request,
        StockOutBatchOperationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id 不能为空", nameof(id));
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var entity = await _repository.GetByIdAsync(id.Trim());
        if (entity == null || entity.IsDeleted)
            throw new InvalidOperationException("出库批次记录不存在");

        var packing = await _packingRepository.GetByIdAsync(entity.PackingId);
        if (packing == null)
            throw new InvalidOperationException("装箱单不存在");

        await ValidateOutQtyBalanceAsync(entity, request.OutQty, cancellationToken);

        var oldQty = entity.OutQty;
        entity.OutQty = request.OutQty;
        await _repository.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        if (oldQty != entity.OutQty)
        {
            await WriteFieldChangeLogAsync(
                entity,
                nameof(StockOutBatch.OutQty),
                "出库数量",
                oldQty.ToString(),
                entity.OutQty.ToString(),
                context,
                cancellationToken);
            await AppendBatchOperationLogAsync(
                packing,
                StockOutBatchOperationActionTypes.Update,
                $"编辑出库批次 {entity.GlobalBatchNo}，出库数量：{oldQty}→{entity.OutQty}",
                null,
                BuildExtraInfo(packing.Code, 1, entity.GlobalBatchNo, 0, null),
                context,
                cancellationToken);
        }

        return entity;
    }

    public async Task SoftDeleteAsync(
        string id,
        string reason,
        StockOutBatchOperationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id 不能为空", nameof(id));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("请填写删除原因", nameof(reason));

        var entity = await _repository.GetByIdAsync(id.Trim());
        if (entity == null || entity.IsDeleted)
            throw new InvalidOperationException("出库批次记录不存在");

        var packing = await _packingRepository.GetByIdAsync(entity.PackingId);
        if (packing == null)
            throw new InvalidOperationException("装箱单不存在");

        entity.IsDeleted = true;
        await _repository.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        await AppendBatchOperationLogAsync(
            packing,
            StockOutBatchOperationActionTypes.Delete,
            $"删除出库批次 {entity.GlobalBatchNo}",
            reason.Trim(),
            BuildExtraInfo(packing.Code, 1, entity.GlobalBatchNo, 0, null),
            context,
            cancellationToken);
    }

    public async Task<StockOutBatchBulkDeleteResultDto> BulkDeleteByPackingAsync(
        string packingId,
        string reason,
        StockOutBatchOperationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var pid = (packingId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(pid))
            throw new ArgumentException("装箱单 ID 不能为空", nameof(packingId));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("请填写删除原因", nameof(reason));

        var packing = await _packingRepository.GetByIdAsync(pid);
        if (packing == null)
            throw new InvalidOperationException("装箱单不存在");

        var batches = (await _repository.FindAsync(b => b.PackingId == pid))
            .Where(b => !b.IsDeleted)
            .OrderBy(b => b.GlobalBatchNo, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (batches.Count == 0)
            throw new InvalidOperationException("该装箱单下没有可删除的出库批次记录");

        var result = new StockOutBatchBulkDeleteResultDto();
        foreach (var batch in batches)
        {
            batch.IsDeleted = true;
            await _repository.UpdateAsync(batch);
            result.DeletedCount++;
            result.DeletedGlobalBatchNos.Add(batch.GlobalBatchNo);
        }

        await _unitOfWork.SaveChangesAsync();

        var deletedSummary = SummarizeBatchNos(result.DeletedGlobalBatchNos);
        var desc = $"装箱单 {packing.Code} 批量删除出库批次 {result.DeletedCount} 条";

        await AppendBatchOperationLogAsync(
            packing,
            StockOutBatchOperationActionTypes.BulkDelete,
            desc,
            reason.Trim(),
            BuildExtraInfo(packing.Code, result.DeletedCount, deletedSummary, 0, null),
            context,
            cancellationToken);

        return result;
    }

    public async Task LogExportAsync(
        string packingId,
        int exportedCount,
        StockOutBatchOperationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var pid = (packingId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(pid))
            throw new ArgumentException("装箱单 ID 不能为空", nameof(packingId));

        var packing = await _packingRepository.GetByIdAsync(pid);
        if (packing == null)
            throw new InvalidOperationException("装箱单不存在");

        var count = Math.Max(0, exportedCount);
        await AppendBatchOperationLogAsync(
            packing,
            StockOutBatchOperationActionTypes.Export,
            $"导出出库批次 {count} 条",
            null,
            BuildExtraInfo(packing.Code, count, null, 0, null),
            context,
            cancellationToken);
    }

    private async Task ValidateOutQtyBalanceAsync(
        StockOutBatch entity,
        int newOutQty,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (newOutQty <= 0)
            throw new InvalidOperationException("出库数量须为正整数");

        var globalNo = (entity.GlobalBatchNo ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(globalNo))
            throw new InvalidOperationException("批次全局唯一编号无效");

        var inBatch = (await _stockInBatchRepository.FindAsync(b => b.GlobalBatchNo == globalNo))
            .FirstOrDefault(b => !b.IsDeleted);
        if (inBatch == null)
            throw new InvalidOperationException($"批次全局唯一编号「{globalNo}」在入库批次中不存在");

        var otherOut = (await _repository.FindAsync(b => b.GlobalBatchNo == globalNo))
            .Where(b => !b.IsDeleted && !string.Equals(b.Id, entity.Id, StringComparison.OrdinalIgnoreCase))
            .Sum(b => b.OutQty);
        var remaining = inBatch.BatchQty - otherOut;
        if (newOutQty > remaining)
        {
            throw new InvalidOperationException(
                $"批次「{globalNo}」剩余可出 {Math.Max(0, remaining)}，本次数量 {newOutQty} 超出余额（入库数量 {inBatch.BatchQty}，其他出库 {otherOut}）");
        }
    }

    private async Task AppendBatchOperationLogAsync(
        Packing packing,
        string actionType,
        string operationDesc,
        string? reason,
        string? extraInfo,
        StockOutBatchOperationContext? operatorCtx,
        CancellationToken cancellationToken)
    {
        await _logOperationAppend.AppendAsync(
            BusinessLogTypes.Packing,
            packing.Id,
            packing.Code,
            actionType,
            operatorCtx?.OperatorUserId,
            operatorCtx?.OperatorUserName,
            operationDesc,
            reason,
            extraInfo,
            cancellationToken);
    }

    private async Task WriteFieldChangeLogAsync(
        StockOutBatch entity,
        string fieldName,
        string fieldLabel,
        string? oldValue,
        string? newValue,
        StockOutBatchOperationContext? operatorCtx,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        static string SqlQ(string? s) => (s ?? "").Replace("'", "''", StringComparison.Ordinal);
        var recordCodeSql = string.IsNullOrWhiteSpace(entity.GlobalBatchNo) ? "NULL" : $"'{SqlQ(entity.GlobalBatchNo)}'";
        var sql = $@"
INSERT INTO log_change_fldval (""Id"", ""BizType"", ""RecordId"", ""RecordCode"", ""FieldName"", ""FieldLabel"", ""OldValue"", ""NewValue"", ""ChangedAt"", ""ChangedByUserId"", ""ChangedByUserName"", ""ExtraInfo"", ""SysRemark"")
VALUES (gen_random_uuid()::text, '{BusinessLogTypes.StockOutBatch}', '{SqlQ(entity.Id)}', {recordCodeSql}, '{SqlQ(fieldName)}', '{SqlQ(fieldLabel)}', {(oldValue == null ? "NULL" : $"'{SqlQ(oldValue)}'")}, {(newValue == null ? "NULL" : $"'{SqlQ(newValue)}'")}, NOW(), {(operatorCtx?.OperatorUserId == null ? "NULL" : $"'{SqlQ(operatorCtx.OperatorUserId)}'")}, '{SqlQ(operatorCtx?.OperatorUserName)}', NULL, NULL)";
        await _unitOfWork.ExecuteAsync(sql);
    }

    private static string? BuildExtraInfo(
        string? packingCode,
        int affectedCount,
        string? batchNosSummary,
        int skippedCount,
        string? skippedBatchNosSummary)
    {
        var payload = new Dictionary<string, object?>
        {
            ["packingCode"] = packingCode,
            ["affectedCount"] = affectedCount,
            ["batchNosSummary"] = batchNosSummary,
            ["skippedCount"] = skippedCount,
            ["skippedBatchNosSummary"] = skippedBatchNosSummary
        };
        return JsonSerializer.Serialize(payload);
    }

    private static string SummarizeBatchNos(IReadOnlyList<string> nos)
    {
        if (nos.Count == 0) return string.Empty;
        if (nos.Count <= 5) return string.Join("、", nos);
        return string.Join("、", nos.Take(5)) + $" 等 {nos.Count} 条";
    }

    private static bool IsImportRowEmpty(StockOutBatchImportRowRequest r) =>
        string.IsNullOrWhiteSpace(r.GlobalBatchNo) && r.OutQty <= 0;
}
