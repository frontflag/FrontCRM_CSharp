namespace CRM.Core.Constants;

/// <summary>质检列表左栏 preset（URL <c>preset</c> / API <see cref="Interfaces.QcQueryRequest.Preset"/>）。</summary>
public static class QcListQuickFilterCodes
{
    public const string QcToday = "qc_today";
    public const string QcTodayYesterday = "qc_today_yesterday";
    public const string QcWithin3Days = "qc_within_3_days";
    public const string QcWithin7Days = "qc_within_7_days";
    public const string QcWithin30Days = "qc_within_30_days";

    public const string StatusPassed = "status_passed";
    public const string StatusPartial = "status_partial";
    public const string StatusRejected = "status_rejected";
    public const string HasQcImages = "has_qc_images";
    public const string NoQcImages = "no_qc_images";

    public static bool IsKnown(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var c = code.Trim();
        return c is QcToday or QcTodayYesterday or QcWithin3Days or QcWithin7Days or QcWithin30Days
            or StatusPassed or StatusPartial or StatusRejected or HasQcImages or NoQcImages;
    }
}
