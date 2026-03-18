namespace CRM.Core.Document
{
    /// <summary>
    /// 文件存储服务（落盘、读、删）
    /// </summary>
    public interface IFileStorageService
    {
        Task<StoredFileResult> SaveAsync(DocumentSaveContext ctx);
        Task<Stream> OpenReadAsync(string relativePath);
        Task<bool> ExistsAsync(string relativePath);
        Task DeleteAsync(string relativePath);
    }
}
