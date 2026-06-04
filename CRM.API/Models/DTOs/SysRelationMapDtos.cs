namespace CRM.API.Models.DTOs;

public class SaveSysRelationMapRequest
{
    public short Type { get; set; }
    public string ObjSrc { get; set; } = string.Empty;
    public List<string>? AddDestIds { get; set; }
    public List<string>? RemoveDestIds { get; set; }
}
