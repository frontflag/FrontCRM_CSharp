using CRM.Core.Constants;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Services;

/// <summary>
/// 到货通知状态（10/20/30/100）重算，与操作面板入库单关联口径一致。
/// </summary>
public static class ArrivalNoticeStatusCalculator
{
    public const short StatusNotArrived = 10;
    public const short StatusPendingQc = 20;
    public const short StatusQcDone = 30;
    public const short StatusStockedIn = 100;

    public static short ComputeTargetStatus(
        StockInNotify notice,
        IReadOnlyList<QCInfo>? qcRows,
        IReadOnlyList<StockIn> candidateStockIns)
    {
        var hasQc = qcRows is { Count: > 0 };
        var hasPostedStockIn = candidateStockIns.Any(si =>
            IsPostedStockInLinkedToNotice(si, notice, qcRows));

        return hasPostedStockIn ? StatusStockedIn :
            hasQc ? StatusQcDone :
            notice.ReceiveQty > 0 ? StatusPendingQc :
            StatusNotArrived;
    }

    /// <summary>
    /// 已过账入库单是否关联本到货通知（SourceId / SourceCode / QCID / qc.StockInId；含历史 SourceId 存单号）。
    /// </summary>
    public static bool IsPostedStockInLinkedToNotice(
        StockIn si,
        StockInNotify notice,
        IReadOnlyList<QCInfo>? qcRows)
    {
        if (si.Status != StockInHeaderStatusCode.Posted) return false;
        if (!StockInTypeCode.MatchesNoticeStockInType(si.StockInType, notice.StockInType)) return false;

        var noticeKey = notice.Id.Trim();
        var noticeCode = notice.NoticeCode?.Trim();

        if (!string.IsNullOrWhiteSpace(si.SourceId) &&
            string.Equals(si.SourceId.Trim(), noticeKey, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrWhiteSpace(noticeCode))
        {
            if (!string.IsNullOrWhiteSpace(si.SourceCode) &&
                string.Equals(si.SourceCode.Trim(), noticeCode, StringComparison.OrdinalIgnoreCase))
                return true;

            // 历史数据：SourceId 误存到货通知单号
            if (!string.IsNullOrWhiteSpace(si.SourceId) &&
                string.Equals(si.SourceId.Trim(), noticeCode, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (qcRows == null || qcRows.Count == 0) return false;

        return qcRows.Any(q =>
            (!string.IsNullOrWhiteSpace(si.QcId) &&
             string.Equals(si.QcId.Trim(), q.Id.Trim(), StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(q.StockInId) &&
                string.Equals(q.StockInId.Trim(), si.Id.Trim(), StringComparison.OrdinalIgnoreCase)));
    }
}
