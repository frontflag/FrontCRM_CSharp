namespace CRM.Core.Services.RfqAssignment;

internal static class RfqAssignmentBrandKey
{
    public static string Resolve(long? brandId, string? brand) =>
        brandId is > 0 ? $"id:{brandId.Value}" : $"name:{(brand ?? string.Empty).Trim()}";
}
