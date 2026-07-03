using System.Text.Json;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Services
{
    public class StockInBatchService : IStockInBatchService
    {
        private readonly IRepository<StockInBatch> _repository;
        private readonly IRepository<StockIn> _stockInRepository;
        private readonly IRepository<StockInItem> _stockInItemRepository;
        private readonly IBatchGlobalNumberService _batchGlobalNumberService;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly IUnitOfWork _unitOfWork;

        public StockInBatchService(
            IRepository<StockInBatch> repository,
            IRepository<StockIn> stockInRepository,
            IRepository<StockInItem> stockInItemRepository,
            IBatchGlobalNumberService batchGlobalNumberService,
            ILogOperationAppendService logOperationAppend,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _stockInRepository = stockInRepository;
            _stockInItemRepository = stockInItemRepository;
            _batchGlobalNumberService = batchGlobalNumberService;
            _logOperationAppend = logOperationAppend;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<StockInBatch>> ListAsync(StockInBatchListQuery? query, CancellationToken cancellationToken = default)
        {
            query ??= new StockInBatchListQuery();
            var all = (await _repository.GetAllAsync()).ToList();
            var globalNeedle = query.GlobalBatchNo?.Trim();
            var lotNeedle = query.Lot?.Trim();
            var snNeedle = query.SerialNumber?.Trim();

            IEnumerable<StockInBatch> q = all.Where(x => !x.IsDeleted);
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
            var entity = await _repository.GetByIdAsync(id.Trim());
            return entity is { IsDeleted: false } ? entity : null;
        }

        public async Task<StockInBatch> UpdateAsync(
            string id,
            StockInBatchUpdateRequest request,
            StockInBatchOperationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id 不能为空", nameof(id));
            var entity = await _repository.GetByIdAsync(id.Trim());
            if (entity == null || entity.IsDeleted)
                throw new InvalidOperationException("批次记录不存在");

            if (request.BatchQty < 0)
                throw new InvalidOperationException("批次数量不能为负数");

            var stockInCtx = await ResolveStockInContextByItemIdAsync(entity.StockInItemId, cancellationToken);

            var before = Snapshot(entity);
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

            var changes = CollectFieldChanges(before, Snapshot(entity));
            if (changes.Count > 0)
            {
                await WriteFieldChangeLogsAsync(
                    entity,
                    stockInCtx,
                    changes,
                    context,
                    cancellationToken);
                var changeSummary = string.Join("；", changes.Select(c => $"{c.Label}:{c.OldValue}→{c.NewValue}"));
                await AppendBatchOperationLogAsync(
                    stockInCtx,
                    StockInBatchOperationActionTypes.Update,
                    $"编辑批次 {entity.GlobalBatchNo}，变更 {changes.Count} 项：{changeSummary}",
                    null,
                    BuildExtraInfo(stockInCtx.ItemCode, 1, entity.GlobalBatchNo, 0, null),
                    context,
                    cancellationToken);
            }

            return entity;
        }

        public async Task<StockInBatchImportResultDto> ImportAsync(
            StockInBatchImportRequest request,
            StockInBatchOperationContext? context = null,
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

            var line = await _stockInItemRepository.GetByIdAsync(stockInItemId);
            if (line == null)
                throw new InvalidOperationException("入库明细不存在");
            if (!string.Equals((line.StockInId ?? string.Empty).Trim(), stockInId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("入库明细不属于当前入库单");

            var stockInCtx = new StockInOperationContext(header, line, ItemCode(line));

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

            var batchSummary = SummarizeBatchNos(globalBatchNos);
            await AppendBatchOperationLogAsync(
                stockInCtx,
                StockInBatchOperationActionTypes.Import,
                $"入库明细 {stockInCtx.ItemCode} 导入批次 {entities.Count} 条：{batchSummary}",
                null,
                BuildExtraInfo(stockInCtx.ItemCode, entities.Count, batchSummary, 0, null),
                context,
                cancellationToken);

            return new StockInBatchImportResultDto
            {
                ImportedCount = entities.Count,
                GlobalBatchNos = globalBatchNos
            };
        }

        public async Task SoftDeleteAsync(
            string id,
            string? reason,
            StockInBatchOperationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id 不能为空", nameof(id));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("请填写删除原因", nameof(reason));

            var entity = await _repository.GetByIdAsync(id.Trim());
            if (entity == null || entity.IsDeleted)
                throw new InvalidOperationException("批次记录不存在");

            var stockInCtx = await ResolveStockInContextByItemIdAsync(entity.StockInItemId, cancellationToken);

            entity.IsDeleted = true;
            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            await AppendBatchOperationLogAsync(
                stockInCtx,
                StockInBatchOperationActionTypes.Delete,
                $"删除批次 {entity.GlobalBatchNo}",
                reason.Trim(),
                BuildExtraInfo(stockInCtx.ItemCode, 1, entity.GlobalBatchNo, 0, null),
                context,
                cancellationToken);
        }

        public async Task<StockInBatchBulkDeleteResultDto> BulkDeleteByItemAsync(
            string stockInItemId,
            string reason,
            StockInBatchOperationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            var itemId = (stockInItemId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(itemId))
                throw new ArgumentException("入库明细 ID 不能为空", nameof(stockInItemId));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("请填写删除原因", nameof(reason));

            var stockInCtx = await ResolveStockInContextByItemIdAsync(itemId, cancellationToken);

            var batches = (await _repository.FindAsync(b => b.StockInItemId == itemId))
                .Where(b => !b.IsDeleted)
                .OrderBy(b => b.GlobalBatchNo, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var result = new StockInBatchBulkDeleteResultDto();
            foreach (var batch in batches)
            {
                batch.IsDeleted = true;
                await _repository.UpdateAsync(batch);
                result.DeletedCount++;
                result.DeletedGlobalBatchNos.Add(batch.GlobalBatchNo);
            }

            if (result.DeletedCount == 0)
                throw new InvalidOperationException("该入库明细下没有可删除的批次记录");

            await _unitOfWork.SaveChangesAsync();

            var deletedSummary = SummarizeBatchNos(result.DeletedGlobalBatchNos);
            var desc = $"入库明细 {stockInCtx.ItemCode} 批量删除 {result.DeletedCount} 条";

            await AppendBatchOperationLogAsync(
                stockInCtx,
                StockInBatchOperationActionTypes.BulkDelete,
                desc,
                reason.Trim(),
                BuildExtraInfo(
                    stockInCtx.ItemCode,
                    result.DeletedCount,
                    deletedSummary,
                    0,
                    null),
                context,
                cancellationToken);

            return result;
        }

        public async Task LogExportAsync(
            string stockInId,
            int exportedCount,
            StockInBatchOperationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            var sid = (stockInId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(sid))
                throw new ArgumentException("入库单 ID 不能为空", nameof(stockInId));

            var header = await _stockInRepository.GetByIdAsync(sid);
            if (header == null)
                throw new InvalidOperationException("入库单不存在");

            var count = Math.Max(0, exportedCount);
            await _logOperationAppend.AppendAsync(
                BusinessLogTypes.StockIn,
                header.Id,
                header.StockInCode,
                StockInBatchOperationActionTypes.Export,
                context?.OperatorUserId,
                context?.OperatorUserName,
                $"导出入库批次 {count} 条",
                null,
                BuildExtraInfo(null, count, null, 0, null),
                cancellationToken);
        }

        private async Task<StockInOperationContext> ResolveStockInContextByItemIdAsync(
            string stockInItemId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await _stockInItemRepository.GetByIdAsync(stockInItemId.Trim());
            if (line == null)
                throw new InvalidOperationException("入库明细不存在");
            var header = await _stockInRepository.GetByIdAsync(line.StockInId);
            if (header == null)
                throw new InvalidOperationException("入库单不存在");
            return new StockInOperationContext(header, line, ItemCode(line));
        }

        private static string ItemCode(StockInItem line) =>
            string.IsNullOrWhiteSpace(line.StockInItemCode) ? line.Id : line.StockInItemCode.Trim();

        private async Task AppendBatchOperationLogAsync(
            StockInOperationContext ctx,
            string actionType,
            string operationDesc,
            string? reason,
            string? extraInfo,
            StockInBatchOperationContext? operatorCtx,
            CancellationToken cancellationToken)
        {
            await _logOperationAppend.AppendAsync(
                BusinessLogTypes.StockIn,
                ctx.Header.Id,
                ctx.Header.StockInCode,
                actionType,
                operatorCtx?.OperatorUserId,
                operatorCtx?.OperatorUserName,
                operationDesc,
                reason,
                extraInfo,
                cancellationToken);
        }

        private async Task WriteFieldChangeLogsAsync(
            StockInBatch entity,
            StockInOperationContext stockInCtx,
            IReadOnlyList<FieldChange> changes,
            StockInBatchOperationContext? operatorCtx,
            CancellationToken cancellationToken)
        {
            foreach (var c in changes)
            {
                await AddChangeLogAsync(
                    entity.Id,
                    entity.GlobalBatchNo,
                    c.FieldName,
                    c.Label,
                    c.OldValue,
                    c.NewValue,
                    operatorCtx?.OperatorUserId,
                    operatorCtx?.OperatorUserName,
                    cancellationToken);
            }
        }

        private async Task AddChangeLogAsync(
            string batchId,
            string? batchNo,
            string fieldName,
            string? fieldLabel,
            string? oldValue,
            string? newValue,
            string? userId,
            string? userName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            static string SqlQ(string? s) => (s ?? "").Replace("'", "''", StringComparison.Ordinal);
            var recordCodeSql = string.IsNullOrWhiteSpace(batchNo) ? "NULL" : $"'{SqlQ(batchNo)}'";
            var sql = $@"
INSERT INTO log_change_fldval (""Id"", ""BizType"", ""RecordId"", ""RecordCode"", ""FieldName"", ""FieldLabel"", ""OldValue"", ""NewValue"", ""ChangedAt"", ""ChangedByUserId"", ""ChangedByUserName"", ""ExtraInfo"", ""SysRemark"")
VALUES (gen_random_uuid()::text, '{BusinessLogTypes.StockInBatch}', '{SqlQ(batchId)}', {recordCodeSql}, '{SqlQ(fieldName)}', {(fieldLabel == null ? "NULL" : $"'{SqlQ(fieldLabel)}'")}, {(oldValue == null ? "NULL" : $"'{SqlQ(oldValue)}'")}, {(newValue == null ? "NULL" : $"'{SqlQ(newValue)}'")}, NOW(), {(userId == null ? "NULL" : $"'{SqlQ(userId)}'")}, '{SqlQ(userName)}', NULL, NULL)";
            await _unitOfWork.ExecuteAsync(sql);
        }

        private static string? BuildExtraInfo(
            string? stockInItemCode,
            int affectedCount,
            string? batchNosSummary,
            int skippedCount,
            string? skippedBatchNosSummary)
        {
            var payload = new Dictionary<string, object?>
            {
                ["stockInItemCode"] = stockInItemCode,
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

        private sealed record StockInOperationContext(StockIn Header, StockInItem Line, string ItemCode);

        private sealed record FieldChange(string FieldName, string Label, string? OldValue, string? NewValue);

        private sealed record BatchSnapshot(
            string? BatchDimension,
            string? BatchUnit,
            string? UnitNo,
            int BatchQty,
            string? Dc,
            string? PackageOrigin,
            string? WaferOrigin,
            string? Lot,
            string? SerialNumber,
            string? FirmwareVersion,
            string? PartCode,
            string? Remark);

        private static BatchSnapshot Snapshot(StockInBatch e) => new(
            e.BatchDimension,
            e.BatchUnit,
            e.UnitNo,
            e.BatchQty,
            e.Dc,
            e.PackageOrigin,
            e.WaferOrigin,
            e.Lot,
            e.SerialNumber,
            e.FirmwareVersion,
            e.PartCode,
            e.Remark);

        private static List<FieldChange> CollectFieldChanges(BatchSnapshot before, BatchSnapshot after)
        {
            var list = new List<FieldChange>();
            AddChange(list, nameof(StockInBatch.BatchDimension), "批次维度", before.BatchDimension, after.BatchDimension);
            AddChange(list, nameof(StockInBatch.BatchUnit), "批次单位", before.BatchUnit, after.BatchUnit);
            AddChange(list, nameof(StockInBatch.UnitNo), "单位编号", before.UnitNo, after.UnitNo);
            if (before.BatchQty != after.BatchQty)
                list.Add(new FieldChange(nameof(StockInBatch.BatchQty), "批次数量", before.BatchQty.ToString(), after.BatchQty.ToString()));
            AddChange(list, nameof(StockInBatch.Dc), "DC", before.Dc, after.Dc);
            AddChange(list, nameof(StockInBatch.PackageOrigin), "封装产地", before.PackageOrigin, after.PackageOrigin);
            AddChange(list, nameof(StockInBatch.WaferOrigin), "晶圆产地", before.WaferOrigin, after.WaferOrigin);
            AddChange(list, nameof(StockInBatch.Lot), "LOT", before.Lot, after.Lot);
            AddChange(list, nameof(StockInBatch.SerialNumber), "SN", before.SerialNumber, after.SerialNumber);
            AddChange(list, nameof(StockInBatch.FirmwareVersion), "固件版本", before.FirmwareVersion, after.FirmwareVersion);
            AddChange(list, nameof(StockInBatch.PartCode), "Part Code", before.PartCode, after.PartCode);
            AddChange(list, nameof(StockInBatch.Remark), "备注", before.Remark, after.Remark);
            return list;
        }

        private static void AddChange(
            List<FieldChange> list,
            string fieldName,
            string label,
            string? oldVal,
            string? newVal)
        {
            var o = NullIfWhiteSpace(oldVal) ?? string.Empty;
            var n = NullIfWhiteSpace(newVal) ?? string.Empty;
            if (!string.Equals(o, n, StringComparison.Ordinal))
                list.Add(new FieldChange(fieldName, label, o, n));
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
