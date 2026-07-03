namespace CRM.Core.Constants;

/// <summary>入库单详情「入库批次」面板相关操作日志 ActionType（log_operation）。</summary>
public static class StockInBatchOperationActionTypes
{
    public const string Prefix = "StockInBatch";

    public const string Import = "StockInBatchImport";
    public const string Delete = "StockInBatchDelete";
    public const string BulkDelete = "StockInBatchBulkDelete";
    public const string Update = "StockInBatchUpdate";
    public const string Export = "StockInBatchExport";
}
