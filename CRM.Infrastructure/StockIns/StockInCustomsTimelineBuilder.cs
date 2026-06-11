using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;

namespace CRM.Infrastructure.StockIns;

/// <summary>报关入库详情：按业务顺序组装上下游时间线。</summary>
internal static class StockInCustomsTimelineBuilder
{
    public static List<StockInCustomsTimelineStepDto> Build(
        StockIn stockIn,
        CustomsDeclarationItem cdi,
        CustomsDeclaration dec,
        StockInNotify? arrivalNotify,
        StockOutRequest? salesSor,
        StockOutRequest? customsSor,
        CustomsPendlist? pendlist,
        Packing? packing,
        StockTransfer? transfer,
        QCInfo? qc)
    {
        var steps = new List<StockInCustomsTimelineStepDto>(9);

        AddStep(steps, "salesStockOutNotify", 1, salesSor?.Id, salesSor?.RequestCode, salesSor?.Status,
            salesSor?.CreateTime);

        AddStep(steps, "pendlist", 2,
            pendlist?.Id,
            null,
            pendlist?.Status,
            pendlist?.CreateTime);

        AddStep(steps, "customsStockOutNotify", 3, customsSor?.Id, customsSor?.RequestCode, customsSor?.Status,
            customsSor?.CreateTime);

        AddStep(steps, "packing", 4, packing?.Id, packing?.Code, packing?.Status,
            packing?.ModifyTime ?? packing?.CreateTime);

        AddStep(steps, "declaration", 5, dec.Id, dec.DeclarationCode, dec.InternalStatus,
            dec.DeclareDate != default ? dec.DeclareDate : dec.CreateTime);

        AddStep(steps, "stockTransfer", 6, transfer?.Id, transfer?.TransferCode, transfer?.Status,
            transfer?.ConfirmedTime ?? transfer?.CreateTime);

        AddStep(steps, "arrivalNotify", 7, arrivalNotify?.Id, arrivalNotify?.NoticeCode, arrivalNotify?.Status,
            arrivalNotify?.CreateTime);

        AddStep(steps, "qc", 8, qc?.Id, qc?.QcCode, qc?.Status, qc?.CreateTime);

        AddStep(steps, "stockIn", 9, stockIn.Id, stockIn.StockInCode, stockIn.Status,
            stockIn.StockInDate != default ? stockIn.StockInDate : stockIn.CreateTime,
            forceDone: true);

        return steps;
    }

    private static void AddStep(
        List<StockInCustomsTimelineStepDto> steps,
        string stepCode,
        int sortOrder,
        string? docId,
        string? docCode,
        short? status,
        DateTime? occurredAt,
        bool forceDone = false)
    {
        var hasDoc = !string.IsNullOrWhiteSpace(docId);
        steps.Add(new StockInCustomsTimelineStepDto
        {
            StepCode = stepCode,
            SortOrder = sortOrder,
            DocId = string.IsNullOrWhiteSpace(docId) ? null : docId.Trim(),
            DocCode = string.IsNullOrWhiteSpace(docCode) ? null : docCode.Trim(),
            Status = status,
            OccurredAt = occurredAt,
            State = forceDone || hasDoc ? "done" : "pending"
        });
    }
}
