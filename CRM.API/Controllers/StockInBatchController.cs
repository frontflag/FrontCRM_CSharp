using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/stock-in/batches")]
    public class StockInBatchController : ControllerBase
    {
        private readonly IStockInBatchService _service;
        private readonly IStockInBatchListQuery _batchListQuery;
        private readonly ILogger<StockInBatchController> _logger;

        public StockInBatchController(
            IStockInBatchService service,
            IStockInBatchListQuery batchListQuery,
            ILogger<StockInBatchController> logger)
        {
            _service = service;
            _batchListQuery = batchListQuery;
            _logger = logger;
        }

        public sealed class StockInBatchDeleteBody
        {
            public string Reason { get; set; } = string.Empty;
        }

        public sealed class StockInBatchBulkDeleteByItemBody
        {
            public string StockInItemId { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
        }

        public sealed class StockInBatchLogExportBody
        {
            public string StockInId { get; set; } = string.Empty;
            public int ExportedCount { get; set; }
        }

        private StockInBatchOperationContext OperationContext() => new()
        {
            OperatorUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            OperatorUserName = User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
        };

        /// <summary>Excel 解析后的批次行批量写入 <c>stock_in_batch</c>，每行自动生成全局编号。</summary>
        [HttpPost("import")]
        [RequestSizeLimit(64 * 1024 * 1024)]
        public async Task<ActionResult<ApiResponse<StockInBatchImportResultDto>>> Import(
            [FromBody] StockInBatchImportRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<StockInBatchImportResultDto>.Fail("请求体不能为空", 400));
                var result = await _service.ImportAsync(request, OperationContext(), cancellationToken);
                return Ok(ApiResponse<StockInBatchImportResultDto>.Ok(
                    result,
                    $"成功导入 {result.ImportedCount} 条批次记录"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<StockInBatchImportResultDto>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入入库批次失败");
                return StatusCode(500, ApiResponse<StockInBatchImportResultDto>.Fail($"导入入库批次失败: {ex.Message}", 500));
            }
        }

        [HttpPost("bulk-delete-by-item")]
        public async Task<ActionResult<ApiResponse<StockInBatchBulkDeleteResultDto>>> BulkDeleteByItem(
            [FromBody] StockInBatchBulkDeleteByItemBody body,
            CancellationToken cancellationToken)
        {
            try
            {
                if (body == null)
                    return BadRequest(ApiResponse<StockInBatchBulkDeleteResultDto>.Fail("请求体不能为空", 400));
                var result = await _service.BulkDeleteByItemAsync(
                    body.StockInItemId,
                    body.Reason,
                    OperationContext(),
                    cancellationToken);
                return Ok(ApiResponse<StockInBatchBulkDeleteResultDto>.Ok(result, "批量删除完成"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<StockInBatchBulkDeleteResultDto>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<StockInBatchBulkDeleteResultDto>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除入库批次失败");
                return StatusCode(500, ApiResponse<StockInBatchBulkDeleteResultDto>.Fail($"批量删除入库批次失败: {ex.Message}", 500));
            }
        }

        [HttpPost("log-export")]
        public async Task<ActionResult<ApiResponse<object>>> LogExport(
            [FromBody] StockInBatchLogExportBody body,
            CancellationToken cancellationToken)
        {
            try
            {
                if (body == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));
                await _service.LogExportAsync(body.StockInId, body.ExportedCount, OperationContext(), cancellationToken);
                return Ok(ApiResponse<object>.Ok(null!, "已记录导出日志"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录入库批次导出日志失败");
                return StatusCode(500, ApiResponse<object>.Fail($"记录导出日志失败: {ex.Message}", 500));
            }
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? globalBatchNo,
            [FromQuery] string? lot,
            [FromQuery] string? serialNumber,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _batchListQuery.GetPagedAsync(
                    globalBatchNo,
                    lot,
                    serialNumber,
                    page,
                    pageSize,
                    userId,
                    cancellationToken);
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items = result.Items,
                        total = result.TotalCount,
                        page = result.PageIndex,
                        pageSize = result.PageSize
                    },
                    message = "获取入库批次记录成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取入库批次记录失败");
                return StatusCode(500, new { success = false, message = $"获取入库批次记录失败: {ex.Message}" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<StockInBatch>>> GetById(string id, CancellationToken cancellationToken)
        {
            try
            {
                var entity = await _service.GetByIdAsync(id, cancellationToken);
                if (entity == null)
                    return NotFound(ApiResponse<StockInBatch>.Fail("批次记录不存在", 404));
                return Ok(ApiResponse<StockInBatch>.Ok(entity, "获取批次记录成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取批次记录失败");
                return StatusCode(500, ApiResponse<StockInBatch>.Fail($"获取批次记录失败: {ex.Message}", 500));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<StockInBatch>>> Update(
            string id,
            [FromBody] StockInBatchUpdateRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<StockInBatch>.Fail("请求体不能为空", 400));
                var entity = await _service.UpdateAsync(id, request, OperationContext(), cancellationToken);
                return Ok(ApiResponse<StockInBatch>.Ok(entity, "更新批次记录成功"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<StockInBatch>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新批次记录失败");
                return StatusCode(500, ApiResponse<StockInBatch>.Fail($"更新批次记录失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> SoftDelete(
            string id,
            [FromBody] StockInBatchDeleteBody body,
            CancellationToken cancellationToken)
        {
            try
            {
                await _service.SoftDeleteAsync(id, body?.Reason, OperationContext(), cancellationToken);
                return Ok(ApiResponse<object>.Ok(null!, "已删除批次记录"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除批次记录失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除批次记录失败: {ex.Message}", 500));
            }
        }
    }
}
