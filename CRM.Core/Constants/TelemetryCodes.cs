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
