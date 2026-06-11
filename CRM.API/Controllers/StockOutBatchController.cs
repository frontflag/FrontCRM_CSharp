using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
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
                var result = await _service.ImportAsync(request, cancellationToken);
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

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> SoftDelete(string id, CancellationToken cancellationToken)
        {
            try
            {
                await _service.SoftDeleteAsync(id, cancellationToken);
                return Ok(ApiResponse<object>.Ok(null!, "已删除出库批次记录"));
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
