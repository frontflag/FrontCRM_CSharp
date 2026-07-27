namespace CRM.Core.Constants;

public static class TelemetryPermissionCodes
{
    public const string Analytics = "biz.telemetry.analytics";
}

public static class TelemetryEventTypes
{
    public const string Session = "session";
    public const string Page = "page";
    public const string Engagement = "engagement";
    public const string Action = "action";
    public const string Result = "result";
    public const string Error = "error";
    public const string Perf = "perf";
}

public static class TelemetryLimits
{
    public const int MaxEventsPerBatch = 100;
    public const int MaxPayloadChars = 8000;
    public const int EventRetentionDays = 90;
    public const int DailyRetentionDays = 400;
}

/// <summary>
/// API 埋点失败口径：仅系统异常（超时/断网 status=0、HTTP 5xx）。
/// 业务 4xx（如资源不存在 404、校验 400）不计入失败。
/// </summary>
public static class TelemetryApiFailure
{
    public static bool IsSystemFailure(int status) => status == 0 || status >= 500;
}
