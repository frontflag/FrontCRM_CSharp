namespace CRM.Core.Models.Dtos
{
    public class SysDictItemAdminRowDto
    {
        public string Id { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string NameZh { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class SysDictItemAdminPagedDto
    {
        public IReadOnlyList<SysDictItemAdminRowDto> Items { get; set; } = Array.Empty<SysDictItemAdminRowDto>();
        public int Total { get; set; }
    }

    public class SysDictItemAdminQuery
    {
        /// <summary>业务分段：customer / vendor，空表示不按分段过滤（兼容旧调用）</summary>
        public string? BizSegment { get; set; }

        /// <summary>具体 Category（控件名称），空表示该分段下全部</summary>
        public string? Category { get; set; }

        /// <summary>关键字：匹配 ItemCode / 中文名 / 英文名</summary>
        public string? Keyword { get; set; }

        /// <summary>null=全部，true=仅有效，false=仅禁用</summary>
        public bool? IsActive { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class CreateSysDictItemDto
    {
        public string Category { get; set; } = string.Empty;

        /// <summary>已废弃：创建时由服务端按同类最大数字编码 +1 自动生成，请求体可省略。</summary>
        public string? ItemCode { get; set; }

        public string NameZh { get; set; } = string.Empty;
        public string? NameEn { get; set; }

        /// <summary>不传则自动排到同类末尾</summary>
        public int? SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateSysDictItemDto
    {
        public string NameZh { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>同一 Category 下按 Id 顺序重排 SortOrder（从 1 递增）</summary>
    public class ReorderSysDictItemsDto
    {
        public string Category { get; set; } = string.Empty;
        public List<string> OrderedIds { get; set; } = new();
    }
}
