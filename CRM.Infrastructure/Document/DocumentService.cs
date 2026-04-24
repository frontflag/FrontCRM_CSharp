using CRM.Core.Document;
using CRM.Core.Models.Document;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Document
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _storage;

        public DocumentService(ApplicationDbContext context, IFileStorageService storage)
        {
            _context = context;
            _storage = storage;
        }

        public async Task<IReadOnlyList<UploadDocument>> UploadAsync(DocumentUploadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.BizType) || string.IsNullOrWhiteSpace(request.BizId))
                throw new ArgumentException("BizType 与 BizId 不能为空");

            if (request.Files == null || request.Files.Count == 0)
                return Array.Empty<UploadDocument>();

            var maxFiles = 5;
            if (request.Files.Count > maxFiles)
                throw new ArgumentException($"单次最多上传 {maxFiles} 个文件");

            if (request.Remark != null && request.Remark.Length > 256)
                throw new ArgumentException("备注不能超过 256 个字符");

            var list = new List<UploadDocument>();
            foreach (var file in request.Files)
            {
                var ctx = new DocumentSaveContext
                {
                    FileStream = file.Stream,
                    OriginalFileName = file.FileName,
                    BizType = request.BizType,
                    BizId = request.BizId,
                    ContentType = file.ContentType,
                    UploadUserId = request.UploadUserId
                };
                var result = await _storage.SaveAsync(ctx);

                var doc = new UploadDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    BizType = request.BizType,
                    BizId = request.BizId,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    StoredFileName = result.StoredFileName,
                    RelativePath = result.RelativePath,
                    FileSize = result.FileSize,
                    FileExtension = result.FileExtension,
                    MimeType = result.MimeType,
                    ThumbnailRelativePath = result.ThumbnailRelativePath,
                    Remark = request.Remark,
                    UploadUserId = request.UploadUserId,
                    CreateTime = DateTime.UtcNow
                };
                list.Add(doc);
                await _context.UploadDocuments.AddAsync(doc);
            }

            await _context.SaveChangesAsync();
            return list;
        }

        public async Task<IReadOnlyList<UploadDocument>> GetByBizAsync(string bizType, string bizId)
        {
            return await _context.UploadDocuments
                .Where(d => d.BizType == bizType && d.BizId == bizId && !d.IsDeleted)
                .OrderByDescending(d => d.CreateTime)
                .ToListAsync();
        }

        public async Task<PagedResult<UploadDocument>> SearchAsync(DocumentSearchRequest request)
        {
            var q = _context.UploadDocuments.AsNoTracking().Where(d => !d.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.BizType))
                q = q.Where(d => d.BizType == request.BizType);
            if (!string.IsNullOrWhiteSpace(request.BizId))
                q = q.Where(d => d.BizId == request.BizId);
            if (!string.IsNullOrWhiteSpace(request.UploadUserId))
                q = q.Where(d => d.UploadUserId == request.UploadUserId);
            if (!string.IsNullOrWhiteSpace(request.RemarkKeyword))
                q = q.Where(d => d.Remark != null && d.Remark.Contains(request.RemarkKeyword));
            if (request.StartDate.HasValue)
                q = q.Where(d => d.CreateTime >= request.StartDate.Value);
            if (request.EndDate.HasValue)
            {
                var end = request.EndDate.Value.Date.AddDays(1);
                q = q.Where(d => d.CreateTime < end);
            }

            if (request.ExcludeBizTypes is { Count: > 0 })
            {
                var excluded = request.ExcludeBizTypes
                    .Select(x => (x ?? string.Empty).Trim())
                    .Where(x => x.Length > 0)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (excluded.Count > 0)
                    q = q.Where(d => d.BizType == null || !excluded.Contains(d.BizType));
            }

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(d => d.CreateTime)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<UploadDocument> { Items = items, TotalCount = total };
        }

        public async Task SoftDeleteAsync(string documentId, string userId)
        {
            var doc = await _context.UploadDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
            if (doc == null)
                throw new InvalidOperationException("文档不存在");
            doc.IsDeleted = true;
            doc.DeleteTime = DateTime.UtcNow;
            doc.DeleteUserId = userId;
            await _context.SaveChangesAsync();
        }

        public async Task<UploadDocument?> GetByIdAsync(string id)
        {
            return await _context.UploadDocuments.FirstOrDefaultAsync(d => d.Id == id);
        }
    }
}
