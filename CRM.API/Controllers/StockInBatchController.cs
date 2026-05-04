using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using System.Threading;

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

        /// <summary>按 LOT / SN 累加核销出库数量；校验失败不写库。</summary>
        [HttpPost("write-off")]
        public async Task<ActionResult<ApiResponse<StockInBatchWriteOffResultDto>>> WriteOff(
            [FromBody] StockInBatchWriteOffRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<StockInBatchWriteOffResultDto>.Fail("请求体不能为空", 400));
                var result = await _service.ApplyWriteOffAsync(request, cancellationToken);
                var msg = result.ValidationPassed
                    ? (result.UpdatedRowCount > 0 ? $"核销成功，已更新 {result.UpdatedRowCount} 条记录" : "无有效核销数据")
                    : "校验未通过，未更新任何记录";
                return Ok(ApiResponse<StockInBatchWriteOffResultDto>.Ok(result, msg));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<StockInBatchWriteOffResultDto>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "入库批次核销失败");
                return StatusCode(500, ApiResponse<StockInBatchWriteOffResultDto>.Fail($"入库批次核销失败: {ex.Message}", 500));
            }
        }

        /// <summary>Excel 解析后的批次行批量写入 <c>stock_in_batch</c>。</summary>
        [HttpPost("import")]
        public async Task<ActionResult<ApiResponse<int>>> Import(
            [FromBody] StockInBatchImportRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<int>.Fail("请求体不能为空", 400));
                var count = await _service.ImportAsync(request, cancellationToken);
                return Ok(ApiResponse<int>.Ok(count, $"成功导入 {count} 条批次记录"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<int>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入入库批次失败");
                return StatusCode(500, ApiResponse<int>.Fail($"导入入库批次失败: {ex.Message}", 500));
            }
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? stockInItemCode,
            [FromQuery] string? lot,
            [FromQuery] string? serialNumber,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _batchListQuery.GetPagedAsync(
                    stockInItemCode,
                    lot,
                    serialNumber,
                    page,
                    pageSize,
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
                var entity = await _service.UpdateAsync(id, request, cancellationToken);
                return Ok(ApiResponse<StockInBatch>.Ok(entity, "更新批次记录成功"));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<StockInBatch>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新批次记录失败");
                return StatusCode(500, ApiResponse<StockInBatch>.Fail($"更新批次记录失败: {ex.Message}", 500));
            }
        }
    }
}
