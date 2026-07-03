using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/stock-out/batches")]
    public class StockOutBatchController : ControllerBase
    {
        private readonly IStockOutBatchService _service;
        private readonly ILogger<StockOutBatchController> _logger;

        public StockOutBatchController(
            IStockOutBatchService service,
            ILogger<StockOutBatchController> logger)
        {
            _service = service;
            _logger = logger;
        }

        public sealed class StockOutBatchDeleteBody
        {
            public string Reason { get; set; } = string.Empty;
        }

        public sealed class StockOutBatchBulkDeleteByPackingBody
        {
            public string PackingId { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
        }

        public sealed class StockOutBatchLogExportBody
        {
            public string PackingId { get; set; } = string.Empty;
            public int ExportedCount { get; set; }
        }

        private StockOutBatchOperationContext OperationContext() => new()
        {
            OperatorUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            OperatorUserName = User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
        };

        /// <summary>Excel 解析后的出库批次行写入 <c>stock_out_batch</c>，并校验余额。</summary>
        [HttpPost("import")]
        public async Task<ActionResult<ApiResponse<StockOutBatchImportResultDto>>> Import(
            [FromBody] StockOutBatchImportRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<StockOutBatchImportResultDto>.Fail("请求体不能为空", 400));
                var result = await _service.ImportAsync(request, OperationContext(), cancellationToken);
                return Ok(ApiResponse<StockOutBatchImportResultDto>.Ok(
                    result,
                    $"成功导入 {result.ImportedCount} 条出库批次记录"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<StockOutBatchImportResultDto>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入出库批次失败");
                return StatusCode(500, ApiResponse<StockOutBatchImportResultDto>.Fail($"导入出库批次失败: {ex.Message}", 500));
            }
        }

        [HttpPost("bulk-delete-by-packing")]
        public async Task<ActionResult<ApiResponse<StockOutBatchBulkDeleteResultDto>>> BulkDeleteByPacking(
            [FromBody] StockOutBatchBulkDeleteByPackingBody body,
            CancellationToken cancellationToken)
        {
            try
            {
                if (body == null)
                    return BadRequest(ApiResponse<StockOutBatchBulkDeleteResultDto>.Fail("请求体不能为空", 400));
                var result = await _service.BulkDeleteByPackingAsync(
                    body.PackingId,
                    body.Reason,
                    OperationContext(),
                    cancellationToken);
                return Ok(ApiResponse<StockOutBatchBulkDeleteResultDto>.Ok(result, "批量删除完成"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<StockOutBatchBulkDeleteResultDto>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<StockOutBatchBulkDeleteResultDto>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除出库批次失败");
                return StatusCode(500, ApiResponse<StockOutBatchBulkDeleteResultDto>.Fail($"批量删除出库批次失败: {ex.Message}", 500));
            }
        }

        [HttpPost("log-export")]
        public async Task<ActionResult<ApiResponse<object>>> LogExport(
            [FromBody] StockOutBatchLogExportBody body,
            CancellationToken cancellationToken)
        {
            try
            {
                if (body == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));
                await _service.LogExportAsync(body.PackingId, body.ExportedCount, OperationContext(), cancellationToken);
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
                _logger.LogError(ex, "记录出库批次导出日志失败");
                return StatusCode(500, ApiResponse<object>.Fail($"记录导出日志失败: {ex.Message}", 500));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<StockOutBatch>>> GetById(string id, CancellationToken cancellationToken)
        {
            try
            {
                var entity = await _service.GetByIdAsync(id, cancellationToken);
                if (entity == null)
                    return NotFound(ApiResponse<StockOutBatch>.Fail("出库批次记录不存在", 404));
                return Ok(ApiResponse<StockOutBatch>.Ok(entity, "获取出库批次记录成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库批次记录失败");
                return StatusCode(500, ApiResponse<StockOutBatch>.Fail($"获取出库批次记录失败: {ex.Message}", 500));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<StockOutBatch>>> Update(
            string id,
            [FromBody] StockOutBatchUpdateRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<StockOutBatch>.Fail("请求体不能为空", 400));
                var entity = await _service.UpdateAsync(id, request, OperationContext(), cancellationToken);
                return Ok(ApiResponse<StockOutBatch>.Ok(entity, "更新出库批次记录成功"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<StockOutBatch>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新出库批次记录失败");
                return StatusCode(500, ApiResponse<StockOutBatch>.Fail($"更新出库批次记录失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> SoftDelete(
            string id,
            [FromBody] StockOutBatchDeleteBody body,
            CancellationToken cancellationToken)
        {
            try
            {
                await _service.SoftDeleteAsync(id, body?.Reason ?? string.Empty, OperationContext(), cancellationToken);
                return Ok(ApiResponse<object>.Ok(null!, "已删除出库批次记录"));
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
                _logger.LogError(ex, "删除出库批次记录失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除出库批次记录失败: {ex.Message}", 500));
            }
        }
    }
}
