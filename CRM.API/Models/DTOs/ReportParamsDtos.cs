namespace CRM.API.Models.DTOs;

public class ReportParamsStyleVersionDto
{
    public string StyleVersion { get; set; } = "V1";
}

public class SetReportParamsStyleVersionRequest
{
    public string? StyleVersion { get; set; }
}
