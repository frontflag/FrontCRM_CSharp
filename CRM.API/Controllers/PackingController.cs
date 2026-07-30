using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.API.Services;
using CRM.API.Utilities;
using CRM.Core.Constants;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/packing")]
public class PackingController : ControllerBase
{
    private readonly IPackingService _packingService;
    private readonly IStockOutService _stockOutService;
    private readonly IOperationLogQueryService _operationLogQuery;
    private readonly ApplicationDbContext _db;
    private readonly IRbacService _rbacService;
    private readonly ILogger<PackingController> _logger;

    public PackingController(
        IPackingService packingService,
        IStockOutService stockOutService,
        IOperationLogQueryService operationLogQuery,
        ApplicationDbContext db,
        IRbacService rbacService,
        ILogger<PackingController> logger)
    {
        _packingService = packingService;
        _stockOutService = stockOutService;
        _operationLogQuery = operationLogQuery;
        _db = db;
        _rbacService = rbacService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> List(
        [FromQuery] string? packingCode,
        [FromQuery] short? status,
        [FromQuery] short? stockOutType,
        [FromQuery] short? materialType,
        [FromQuery] string? customerName,
        [FromQuery] string? salesUserName,
        [FromQuery] DateTime? createTimeFrom,
        [FromQuery] DateTime? createTimeTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new PackingListQueryRequest
            {
                PackingCode = packingCode,
                Status = status,
                StockOutType = stockOutType,
                MaterialType = materialType,
                CustomerName = customerName,
                SalesUserName = salesUserName,
                CreateTimeFrom = createTimeFrom,
                CreateTimeTo = createTimeTo,
                CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            };
            var result = await _packingService.GetPackingListPagedAsync(filter, page, pageSize, cancellationToken);
            if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
            {
                foreach (var row in result.Items)
                {
                    row.CustomerName = null;
                    row.SalesUserName = null;
                }
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                items = result.Items,
                total = result.TotalCount,
                page = result.PageIndex,
                pageSize = result.PageSize
            }, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "装箱单列表查询失败");
            return StatusCode(500, ApiResponse<object>.Fail($"加载失败: {ex.Message}", 500));
        }
    }

    [HttpGet("items")]
    public async Task<ActionResult<ApiResponse<object>>> ItemList(
        [FromQuery] string? keyword,
        [FromQuery] string? packingCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _packingService.GetPackingItemListPagedAsync(
                keyword,
                packingCode,
                page,
                pageSize,
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                cancellationToken);
            if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
            {
                foreach (var row in result.Items)
                    row.CustomerName = null;
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                items = result.Items,
                total = result.TotalCount,
                page = result.PageIndex,
                pageSize = result.PageSize
            }, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "装箱单明细列表查询失败");
            return StatusCode(500, ApiResponse<object>.Fail($"加载失败: {ex.Message}", 500));
        }
    }

    /// <summary>批量出库：校验装箱单后直接生成出库单（不经过执行出库页）。</summary>
    [HttpPost("batch-stock-out")]
    public async Task<ActionResult<ApiResponse<PackingBatchStockOutResultDto>>> BatchStockOut(
        [FromBody] BatchStockOutFromPackingsBody? body,
        CancellationToken cancellationToken = default)
    {
        if (body?.PackingIds == null || body.PackingIds.Count == 0)
            return BadRequest(ApiResponse<PackingBatchStockOutResultDto>.Fail("请至少选择一张装箱单", 400));

        if (body.ExpectedStockOutDate is not { } expectedDate || expectedDate == default)
            return BadRequest(ApiResponse<PackingBatchStockOutResultDto>.Fail("请填写预计出库日期", 400));

        try
        {
            var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _packingService.BatchExecuteStockOutFromPackingsAsync(
                body.PackingIds,
                expectedDate,
                actorId,
                cancellationToken);
            return Ok(ApiResponse<PackingBatchStockOutResultDto>.Ok(result, "批量出库成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PackingBatchStockOutResultDto>.Fail(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PackingBatchStockOutResultDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "装箱单批量出库失败");
            return StatusCode(500, ApiResponse<PackingBatchStockOutResultDto>.Fail($"出库失败: {ex.Message}", 500));
        }
    }

    /// <summary>解析所选装箱单对应的出库通知 Id。forPicking=true 时用于拣货；否则用于批量出库前解析。</summary>
    [HttpGet("stock-out-request-ids")]
    public async Task<ActionResult<ApiResponse<PackingStockOutRequestsResolveDto>>> ResolveStockOutRequestIds(
        [FromQuery] string? ids,
        [FromQuery] bool forPicking = false,
        CancellationToken cancellationToken = default)
    {
        var idList = ParseIdsQuery(ids);
        if (idList.Count == 0)
            return BadRequest(ApiResponse<PackingStockOutRequestsResolveDto>.Fail("请至少选择一张装箱单", 400));

        try
        {
            var dto = await _packingService.ResolveStockOutRequestIdsFromPackingsAsync(idList, forPicking, cancellationToken);
            return Ok(ApiResponse<PackingStockOutRequestsResolveDto>.Ok(dto, "ok"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PackingStockOutRequestsResolveDto>.Fail(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PackingStockOutRequestsResolveDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析装箱单出库通知失败");
            return StatusCode(500, ApiResponse<PackingStockOutRequestsResolveDto>.Fail($"加载失败: {ex.Message}", 500));
        }
    }

    /// <summary>装箱单 Packing 打印页：装箱单 + 关联出库单（若有）+ 公司参数。</summary>
    [HttpGet("{id}/packing-report-bundle")]
    public async Task<ActionResult<ApiResponse<StockOutPackingReportBundleDto>>> GetPackingReportBundle(
        string id,
        [FromQuery] bool withInspection = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bundle = await PackingReportBundleLoader.LoadByPackingIdAsync(
                id,
                withInspection,
                _packingService,
                _stockOutService,
                _db,
                _rbacService,
                User,
                _logger,
                cancellationToken);
            if (bundle == null)
                return NotFound(ApiResponse<StockOutPackingReportBundleDto>.Fail("装箱单不存在", 404));
            return Ok(ApiResponse<StockOutPackingReportBundleDto>.Ok(bundle, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取装箱单 Packing 报表数据失败 id={Id}", id);
            return StatusCode(500, ApiResponse<StockOutPackingReportBundleDto>.Fail($"加载失败: {ex.Message}", 500));
        }
    }

    /// <summary>装箱单 Invoice 打印页：装箱单 + 关联出库单（若有）+ 公司参数。</summary>
    [HttpGet("{id}/invoice-report-bundle")]
    public async Task<ActionResult<ApiResponse<StockOutInvoiceReportBundleDto>>> GetInvoiceReportBundle(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bundle = await PackingReportBundleLoader.LoadInvoiceByPackingIdAsync(
                id,
                _packingService,
                _stockOutService,
                _db,
                _rbacService,
                User,
                _logger,
                cancellationToken);
            if (bundle == null)
                return NotFound(ApiResponse<StockOutInvoiceReportBundleDto>.Fail("装箱单不存在", 404));
            return Ok(ApiResponse<StockOutInvoiceReportBundleDto>.Ok(bundle, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取装箱单 Invoice 报表数据失败 id={Id}", id);
            return StatusCode(500, ApiResponse<StockOutInvoiceReportBundleDto>.Fail($"加载失败: {ex.Message}", 500));
        }
    }

    /// <summary>解析装箱单关联的出库单 Id（用于 Invoice/Packing 打印页，取匹配销售行的最新出库单）。</summary>
    [HttpGet("{id:guid}/linked-stock-out-id")]
    public async Task<ActionResult<ApiResponse<object>>> GetLinkedStockOutId(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stockOutId = await _packingService.ResolveLinkedStockOutIdForPrintAsync(id, cancellationToken);
            if (string.IsNullOrWhiteSpace(stockOutId))
                return NotFound(ApiResponse<object>.Fail("未找到关联出库单", 404));
            return Ok(ApiResponse<object>.Ok(new { stockOutId }, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析装箱单关联出库单失败 id={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail($"加载失败: {ex.Message}", 500));
        }
    }

    /// <summary>按出库通知 Id 查询关联装箱单详情（含全部装箱明细）。</summary>
    [HttpGet("by-stock-out-request/{requestId:guid}")]
    public async Task<ActionResult<ApiResponse<PackingDetailDto>>> GetByStockOutRequestId(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = await _packingService.GetPackingByStockOutRequestIdAsync(requestId, cancellationToken);
            if (dto == null)
                return NotFound(ApiResponse<PackingDetailDto>.Fail("未找到与该出库通知关联的装箱单", 404));

            if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
            {
                dto.CustomerName = null;
                dto.SalesUserName = null;
                foreach (var ext in dto.ItemExtends)
                {
                    ext.CustomerName = null;
                    ext.SalesUserName = null;
                }
                foreach (var notify in dto.StockOutNotifies)
                {
                    notify.CustomerName = null;
                    notify.SalesUserName = null;
                }
            }

            return Ok(ApiResponse<PackingDetailDto>.Ok(dto, "ok"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PackingDetailDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按出库通知查询装箱单失败 requestId={RequestId}", requestId);
            return StatusCode(500, ApiResponse<PackingDetailDto>.Fail($"加载失败: {ex.Message}", 500));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PackingDetailDto>>> GetById(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = await _packingService.GetPackingByIdAsync(id, cancellationToken);
            if (dto == null)
                return NotFound(ApiResponse<PackingDetailDto>.Fail("装箱单不存在", 404));

            if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
            {
                dto.CustomerName = null;
                dto.SalesUserName = null;
                foreach (var ext in dto.ItemExtends)
                {
                    ext.CustomerName = null;
                    ext.SalesUserName = null;
                }
                foreach (var notify in dto.StockOutNotifies)
                {
                    notify.CustomerName = null;
                    notify.SalesUserName = null;
                }
            }

            return Ok(ApiResponse<PackingDetailDto>.Ok(dto, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "装箱单详情查询失败 id={Id}", id);
            return StatusCode(500, ApiResponse<PackingDetailDto>.Fail($"加载失败: {ex.Message}", 500));
        }
    }

    /// <summary>装箱单详情「出库批次」面板操作日志（导入/删除/编辑/导出）。</summary>
    [HttpGet("{id}/batch-operation-logs")]
    public async Task<ActionResult<ApiResponse<OperationLogPagedResult>>> GetBatchOperationLogs(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = await _packingService.GetPackingByIdAsync(id, cancellationToken);
            if (dto == null)
                return NotFound(ApiResponse<OperationLogPagedResult>.Fail("装箱单不存在", 404));

            var data = await _operationLogQuery.QueryAsync(new OperationLogQuery
            {
                BizType = BusinessLogTypes.Packing,
                RecordId = id.Trim(),
                ActionTypePrefix = StockOutBatchOperationActionTypes.Prefix,
                Page = page,
                PageSize = pageSize
            }, cancellationToken);
            return Ok(ApiResponse<OperationLogPagedResult>.Ok(data, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取出库批次操作日志失败 PackingId={PackingId}", id);
            return StatusCode(500, ApiResponse<OperationLogPagedResult>.Fail("获取操作日志失败", 500));
        }
    }

    [HttpGet("from-stock-out-requests/preview")]
    public async Task<ActionResult<ApiResponse<PackingDraftFromStockOutRequestsDto>>> PreviewFromStockOutRequests(
        [FromQuery] string? ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ParseIdsQuery(ids);
        if (idList.Count == 0)
            return BadRequest(ApiResponse<PackingDraftFromStockOutRequestsDto>.Fail("请至少选择一条出库通知", 400));

        try
        {
            var dto = await _packingService.GetDraftFromStockOutRequestsAsync(idList, cancellationToken);
            if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
            {
                dto.CustomerName = null;
                dto.SalesUserName = null;
            }

            return Ok(ApiResponse<PackingDraftFromStockOutRequestsDto>.Ok(dto, "ok"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PackingDraftFromStockOutRequestsDto>.Fail(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PackingDraftFromStockOutRequestsDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "装箱单新建预览失败");
            return StatusCode(500, ApiResponse<PackingDraftFromStockOutRequestsDto>.Fail($"加载失败: {ex.Message}", 500));
        }
    }

    /// <summary>确认装箱单（status 10 → 20）。</summary>
    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<object>>> Confirm(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _packingService.ConfirmPackingAsync(id, actorId, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { status = PackingStatusCode.Confirmed }, "确认装箱单成功"));
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
            _logger.LogError(ex, "确认装箱单失败 id={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail($"确认失败: {ex.Message}", 500));
        }
    }

    /// <summary>报关装箱单补生成报关单（强制删除报关单后修复孤儿关联）。</summary>
    [HttpPost("{id:guid}/regenerate-customs-declaration")]
    public async Task<ActionResult<ApiResponse<object>>> RegenerateCustomsDeclaration(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _packingService.RegenerateCustomsDeclarationAsync(id, actorId, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { ok = true }, "报关单已补生成"));
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
            _logger.LogError(ex, "补生成报关单失败 id={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail($"补生成失败: {ex.Message}", 500));
        }
    }

    /// <summary>备货完成（status 30 → 40）。</summary>
    [HttpPost("{id:guid}/ready")]
    public async Task<ActionResult<ApiResponse<object>>> MarkReady(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _packingService.MarkPackingReadyAsync(id, actorId, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { status = PackingStatusCode.Ready }, "备货完成"));
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
            _logger.LogError(ex, "装箱单备货失败 id={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail($"备货失败: {ex.Message}", 500));
        }
    }

    /// <summary>删除装箱单（仅 status=新建）；回滚关联出库通知为待装箱。</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _packingService.DeletePackingAsync(id, actorId, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "删除装箱单成功"));
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
            _logger.LogError(ex, "删除装箱单失败 id={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail($"删除失败: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// 按有效出库事实刷新装箱单主状态（SuperAdmin / SYS_MANAGER）。
    /// 无有效已完成出库时可下行 100→40；有则可上行至 100。
    /// </summary>
    [HttpPost("{id:guid}/refresh-status")]
    public async Task<ActionResult<ApiResponse<object>>> RefreshStatus(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return StatusCode(403, ApiResponse<object>.Fail("未登录或身份无效", 403));

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
            if (!ManagementAccountPolicy.CanForceDelete(summary))
                return StatusCode(403, ApiResponse<object>.Fail("仅系统管理员或平台管理员可刷新装箱状态", 403));

            var result = await _packingService.RefreshStatusAsync(id, userId.Trim(), cancellationToken);
            return Ok(ApiResponse<object>.Ok(result, result.Changed ? "装箱状态已更新" : "装箱状态无变化"));
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
            _logger.LogError(ex, "刷新装箱状态失败 id={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail($"刷新失败: {ex.Message}", 500));
        }
    }

    public class ForceDeletePackingRequest
    {
        public string? ConfirmBillCode { get; set; }
    }

    /// <summary>强制删除装箱单（SYS_ADMIN / SYS_MANAGER）；释放关联拣货任务后软删并回滚出库通知。</summary>
    [HttpPost("{id:guid}/force-delete")]
    public async Task<ActionResult<ApiResponse<object>>> ForceDelete(
        string id,
        [FromBody] ForceDeletePackingRequest? body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return StatusCode(403, ApiResponse<object>.Fail("未登录或身份无效", 403));

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
            if (!ManagementAccountPolicy.CanForceDelete(summary))
                return StatusCode(403, ApiResponse<object>.Fail("仅系统管理员或平台管理员可执行强制删除", 403));

            if (body == null || string.IsNullOrWhiteSpace(body.ConfirmBillCode))
                return BadRequest(ApiResponse<object>.Fail("请填写 confirmBillCode", 400));

            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            await _packingService.ForceDeletePackingAsync(
                id,
                body.ConfirmBillCode.Trim(),
                userId.Trim(),
                string.IsNullOrWhiteSpace(userName) ? null : userName.Trim(),
                cancellationToken);

            return Ok(ApiResponse<object>.Ok(null, "强制删除装箱单成功"));
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
            _logger.LogError(ex, "强制删除装箱单失败 id={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail($"强制删除失败: {ex.Message}", 500));
        }
    }

    [HttpPost("from-stock-out-requests")]
    public async Task<ActionResult<ApiResponse<PackingCreateResultDto>>> CreateFromStockOutRequests(
        [FromBody] CreatePackingFromStockOutRequestsBody? body,
        CancellationToken cancellationToken = default)
    {
        if (body?.StockOutRequestIds == null || body.StockOutRequestIds.Count == 0)
            return BadRequest(ApiResponse<PackingCreateResultDto>.Fail("请至少选择一条出库通知", 400));

        try
        {
            var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _packingService.CreateFromStockOutRequestsAsync(
                body.StockOutRequestIds,
                body.Extras,
                actorId,
                cancellationToken);
            return Ok(ApiResponse<PackingCreateResultDto>.Ok(result, "生成装箱单成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PackingCreateResultDto>.Fail(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PackingCreateResultDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从出库通知生成装箱单失败");
            var detail = ApiExceptionMessages.FormatWithDatabaseInner(ex);
            return StatusCode(500, ApiResponse<PackingCreateResultDto>.Fail($"生成失败: {detail}", 500));
        }
    }

    private static List<string> ParseIdsQuery(string? ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
            return new List<string>();
        return ids
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

public class CreatePackingFromStockOutRequestsBody
{
    public List<string> StockOutRequestIds { get; set; } = new();
    public PackingCreateExtras? Extras { get; set; }
}

public class BatchStockOutFromPackingsBody
{
    public List<string> PackingIds { get; set; } = new();
    /// <summary>预计出库日期（必填，YYYY-MM-DD 或 ISO 日期时间）。</summary>
    public DateTime? ExpectedStockOutDate { get; set; }
}
