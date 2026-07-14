namespace CRM.Core.Models.Dtos;

public class BizBrandRowDto
{
    public long Id { get; set; }
    public string? BrandEName { get; set; }
    public string? BrandCName { get; set; }
    public string? StandardBrand { get; set; }
    public string? Alias { get; set; }
    public string? CountryCode { get; set; }
    public string? Country { get; set; }
    public string? Remark { get; set; }
    public string? CreateByUserId { get; set; }
    public string? CreateUserName { get; set; }
    public DateTime? CreateTime { get; set; }
    public short? AuditStatus { get; set; }
    public string? AuditByUserId { get; set; }
    public string? AuditUserName { get; set; }
    public DateTime? AuditTime { get; set; }
}

public class BizBrandQuery
{
    public string? BrandCName { get; set; }
    public string? BrandEName { get; set; }
    public string? StandardBrand { get; set; }
    public string? Alias { get; set; }
    public string? Country { get; set; }
    public string? Remark { get; set; }
    public short? AuditStatus { get; set; }
    public DateTime? CreateTimeFrom { get; set; }
    public DateTime? CreateTimeTo { get; set; }
    /// <summary>true 时文本筛选项按输入完全匹配（不含 % 模糊）。</summary>
    public bool ExactMatch { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class BizBrandPagedDto
{
    public List<BizBrandRowDto> Items { get; set; } = new();
    public int Total { get; set; }
}

/// <summary>品牌下拉选项（RFQ 等场景）。</summary>
public class BizBrandOptionDto
{
    public long Id { get; set; }
    public string? StandardBrand { get; set; }
    public short? AuditStatus { get; set; }
    public string? BrandEName { get; set; }
    public string? BrandCName { get; set; }
    /// <summary>别名（逗号/分号等分隔），供前端精确 token 匹配。</summary>
    public string? Alias { get; set; }
}

public class BizBrandOptionsQuery
{
    public string? Keyword { get; set; }
    public int PageSize { get; set; } = 50;
}

public class UpsertBizBrandRequest
{
    public string? BrandEName { get; set; }
    public string? BrandCName { get; set; }
    public string? StandardBrand { get; set; }
    public string? Alias { get; set; }
    public string? CountryCode { get; set; }
    public string? Country { get; set; }
    public string? Remark { get; set; }
}

public class RememberBizBrandLearnedMappingRequest
{
    public string? SourceText { get; set; }
    public long BrandId { get; set; }
}

public class ResolveBizBrandLearnedMappingsRequest
{
    public List<string> SourceTexts { get; set; } = new();
}

public class BizBrandLearnedMappingResolvedDto
{
    public string SourceText { get; set; } = string.Empty;
    public string SourceKey { get; set; } = string.Empty;
    public long BrandId { get; set; }
    public string? StandardBrand { get; set; }
}
