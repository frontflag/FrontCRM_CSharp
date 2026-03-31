using Microsoft.AspNetCore.Mvc;
using CRM.API.Models.DTOs;
using CRM.Core.Document;
using CRM.Core.Models.Document;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(IDocumentService documentService, IFileStorageService fileStorage, ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        /// <summary>上传文档（multipart/form-data: bizType, bizId, remark?, uploadUserId?, files）</summary>
        [HttpPost("upload")]
        [RequestSizeLimit(300 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 300 * 1024 * 1024)]
        public async Task<ActionResult<ApiResponse<object>>> Upload(
            [FromForm] string bizType,
            [FromForm] string bizId,
            [FromForm] string? remark,
            [FromForm] string? uploadUserId,
            [FromForm] IFormFileCollection? files)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bizType) || string.IsNullOrWhiteSpace(bizId))
                    return BadRequest(ApiResponse<object>.Fail("bizType 与 bizId 不能为空", 400));

                var list = new List<DocumentUploadFile>();
                if (files != null)
                {
                    foreach (var f in files)
                    {
                        if (f.Length == 0) continue;
                        var stream = new MemoryStream();
                        await f.CopyToAsync(stream);
                        stream.Position = 0;
                        list.Add(new DocumentUploadFile
                        {
                            Stream = stream,
                            FileName = f.FileName ?? "file",
                            ContentType = f.ContentType
                        });
                    }
                }

                if (list.Count == 0)
                    return BadRequest(ApiResponse<object>.Fail("请选择至少一个文件", 400));

                var request = new DocumentUploadRequest
                {
                    BizType = bizType,
                    BizId = bizId,
                    Remark = remark,
                    UploadUserId = uploadUserId,
                    Files = list
                };

                var result = await _documentService.UploadAsync(request);
                return Ok(ApiResponse<object>.Ok(result.Select(d => ToDto(d)).ToList(), "上传成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文档上传失败");
                return StatusCode(500, ApiResponse<object>.Fail($"上传失败: {ex.Message}", 500));
            }
        }

        /// <summary>按业务查询文档列表</summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetByBiz([FromQuery] string bizType, [FromQuery] string bizId)
        {
            try
            {
                var list = await _documentService.GetByBizAsync(bizType, bizId);
                return Ok(ApiResponse<object>.Ok(list.Select(ToDto).ToList(), "获取成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取文档列表失败");
                return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
            }
        }

        /// <summary>管理端分页查询</summary>
        [HttpGet("admin")]
        public async Task<ActionResult<ApiResponse<object>>> SearchAdmin(
            [FromQuery] string? bizType,
            [FromQuery] string? bizId,
            [FromQuery] string? uploadUserId,
            [FromQuery] string? remarkKeyword,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var request = new DocumentSearchRequest
                {
                    BizType = bizType,
                    BizId = bizId,
                    UploadUserId = uploadUserId,
                    RemarkKeyword = remarkKeyword,
                    StartDate = startDate,
                    EndDate = endDate,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                var result = await _documentService.SearchAsync(request);
                return Ok(ApiResponse<object>.Ok(new
                {
                    items = result.Items.Select(ToDto).ToList(),
                    totalCount = result.TotalCount
                }, "查询成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文档管理查询失败");
                return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
            }
        }

        /// <summary>下载文件</summary>
        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(string id)
        {
            try
            {
                var doc = await _documentService.GetByIdAsync(id);
                if (doc == null || doc.IsDeleted)
                {
                    _logger.LogWarning("文档下载：记录不存在或已删除。DocumentId={DocumentId}", id);
                    return NotFound();
                }

                try
                {
                    var stream = await _fileStorage.OpenReadAsync(doc.RelativePath);
                    return File(stream, doc.MimeType ?? "application/octet-stream", doc.OriginalFileName);
                }
                catch (FileNotFoundException ex)
                {
                    _logger.LogWarning(ex,
                        "文档下载：物理文件缺失（多实例请配置共享存储与相同 RootPath）。DocumentId={DocumentId}, RelativePath={RelativePath}",
                        doc.Id, doc.RelativePath);
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文档下载失败");
                return StatusCode(500);
            }
        }

        /// <summary>预览（图片/PDF 直接返回文件流，便于前端 iframe/img）</summary>
        [HttpGet("{id}/preview")]
        public async Task<IActionResult> Preview(string id)
        {
            try
            {
                var doc = await _documentService.GetByIdAsync(id);
                if (doc == null || doc.IsDeleted)
                {
                    _logger.LogWarning("文档预览：记录不存在或已删除。DocumentId={DocumentId}", id);
                    return NotFound();
                }

                try
                {
                    var stream = await _fileStorage.OpenReadAsync(doc.RelativePath);
                    var mime = doc.MimeType ?? "application/octet-stream";
                    return File(stream, mime);
                }
                catch (FileNotFoundException ex)
                {
                    _logger.LogWarning(ex,
                        "文档预览：物理文件缺失（多实例请配置共享存储与相同 RootPath）。DocumentId={DocumentId}, RelativePath={RelativePath}",
                        doc.Id, doc.RelativePath);
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文档预览失败");
                return StatusCode(500);
            }
        }

        /// <summary>软删除</summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(string id, [FromQuery] string? userId)
        {
            try
            {
                await _documentService.SoftDeleteAsync(id, userId ?? "");
                return Ok(ApiResponse<object>.Ok(null, "删除成功"));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文档删除失败");
                return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
            }
        }

        private static object ToDto(UploadDocument d)
        {
            return new
            {
                d.Id,
                d.BizType,
                d.BizId,
                d.OriginalFileName,
                d.StoredFileName,
                d.RelativePath,
                d.FileSize,
                d.FileExtension,
                d.MimeType,
                d.ThumbnailRelativePath,
                d.Remark,
                d.UploadUserId,
                d.CreateTime
            };
        }
    }
}
