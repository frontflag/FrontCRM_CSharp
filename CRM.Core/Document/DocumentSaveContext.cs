namespace CRM.Core.Document
{
    /// <summary>
    /// 文件存储入参
    /// </summary>
    public class DocumentSaveContext
    {
        public required Stream FileStream { get; set; }
        public required string OriginalFileName { get; set; }
        public required string BizType { get; set; }
        public required string BizId { get; set; }
        public string? ContentType { get; set; }
        public string? UploadUserId { get; set; }
    }

    /// <summary>
    /// 存储结果
    /// </summary>
    public class StoredFileResult
    {
        public string StoredFileName { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public bool IsImage { get; set; }
        public string? ThumbnailRelativePath { get; set; }
        public string? FileExtension { get; set; }
        public string? MimeType { get; set; }
    }
}
