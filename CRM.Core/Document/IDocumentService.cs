using CRM.Core.Models.Document;

namespace CRM.Core.Document
{
    /// <summary>
    /// 文档业务服务（上传、查询、软删除）
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>批量上传</summary>
        Task<IReadOnlyList<UploadDocument>> UploadAsync(DocumentUploadRequest request);

        /// <summary>按业务维度查询（未删除）</summary>
        Task<IReadOnlyList<UploadDocument>> GetByBizAsync(string bizType, string bizId);

        /// <summary>管理端分页查询</summary>
        Task<PagedResult<UploadDocument>> SearchAsync(DocumentSearchRequest request);

        /// <summary>软删除</summary>
        Task SoftDeleteAsync(string documentId, string userId);

        /// <summary>按 ID 获取</summary>
        Task<UploadDocument?> GetByIdAsync(string id);
    }

    public class DocumentUploadRequest
    {
        public string BizType { get; set; } = string.Empty;
        public string BizId { get; set; } = string.Empty;
        public List<DocumentUploadFile> Files { get; set; } = new();
        public string? Remark { get; set; }
        public string? UploadUserId { get; set; }
    }

    public class DocumentUploadFile
    {
        public Stream Stream { get; set; } = null!;
        public string FileName { get; set; } = string.Empty;
        public string? ContentType { get; set; }
    }

    public class DocumentSearchRequest
    {
        public string? BizType { get; set; }
        public string? BizId { get; set; }
        public string? UploadUserId { get; set; }
        public string? RemarkKeyword { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
