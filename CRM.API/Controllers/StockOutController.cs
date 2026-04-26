using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.API.Services;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/stock-out")]
    public class StockOutController : ControllerBase
    {
        private readonly IStockOutService _service;
        private readonly IRepository<StockOut> _stockOutRepo;
        private readonly IRepository<StockOutItem> _stockOutItemRepo;
        private readonly IRepository<StockOutItemExtend> _stockOutItemExtendRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _db;
        private readonly IRbacService _rbacService;
        private readonly ILogger<StockOutController> _logger;

        public StockOutController(
            IStockOutService service,
            IRepository<StockOut> stockOutRepo,
            IRepository<StockOutItem> stockOutItemRepo,
            IRepository<StockOutItemExtend> stockOutItemExtendRepo,
            IUnitOfWork unitOfWork,
            ApplicationDbContext db,
            IRbacService rbacService,
            ILogger<StockOutController> logger)
        {
            _service = service;
            _stockOutRepo = stockOutRepo;
            _stockOutItemRepo = stockOutItemRepo;
            _stockOutItemExtendRepo = stockOutItemExtendRepo;
            _unitOfWork = unitOfWork;
            _db = db;
            _rbacService = rbacService;
            _logger = logger;
        }

        public class ForceDeleteStockOutRequest
        {
            public string ConfirmBillCode { get; set; } = string.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<StockOutListItemDto>>>> GetAll()
        {
            try
            {
                var list = await _service.GetStockOutListAsync();
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                {
                    var masked = list.ToList();
                    SaleSensitiveFieldMask521.ApplyStockOutListItems(masked, true);
                    list = masked;
                }

                return Ok(ApiResponse<IEnumerable<StockOutListItemDto>>.Ok(list, "获取出库单列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库单列表失败");
                return StatusCode(500, ApiResponse<IEnumerable<StockOutListItemDto>>.Fail($"获取出库单列表失败: {ex.Message}", 500));
            }
        }

        /// <summary>出库明细（stockoutitem）列表，query 与 <see cref="StockOutItemListQuery"/> 一致。</summary>
        [HttpGet("items")]
        public async Task<ActionResult<ApiResponse<IEnumerable<StockOutItemListRowDto>>>> GetItems([FromQuery] StockOutItemListQuery? query)
        {
            try
            {
                var list = await _service.GetStockOutItemListAsync(query);
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                {
                    var masked = list.ToList();
                    SaleSensitiveFieldMask521.ApplyStockOutItemListRows(masked, true);
                    list = masked;
                }

                return Ok(ApiResponse<IEnumerable<StockOutItemListRowDto>>.Ok(list, "获取出库明细列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库明细列表失败");
                return StatusCode(500, ApiResponse<IEnumerable<StockOutItemListRowDto>>.Fail($"获取出库明细列表失败: {ex.Message}", 500));
            }
        }

        /// <summary>出库 Invoice 报表：出库详情 + 公司参数（打印页单请求）。</summary>
        [HttpGet("{id}/invoice-report-bundle")]
        public async Task<ActionResult<ApiResponse<StockOutInvoiceReportBundleDto>>> GetInvoiceReportBundle(string id, CancellationToken cancellationToken)
        {
            try
            {
                var dto = await _service.GetDetailViewAsync(id);
                if (dto == null)
                    return NotFound(ApiResponse<StockOutInvoiceReportBundleDto>.Fail("出库单不存在", 404));
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                    SaleSensitiveFieldMask521.ApplyStockOutDetailView(dto, true);
                var companyProfile = await CompanyProfileBundleLoader.LoadAsync(_db, _logger, cancellationToken);
                CompanyProfileBundleLoader.StripSmtpEmail(companyProfile);
                var bundle = new StockOutInvoiceReportBundleDto { StockOut = dto, CompanyProfile = companyProfile };
                return Ok(ApiResponse<StockOutInvoiceReportBundleDto>.Ok(bundle, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库 Invoice 报表数据失败");
                return StatusCode(500, ApiResponse<StockOutInvoiceReportBundleDto>.Fail($"加载失败: {ex.Message}", 500));
            }
        }

        /// <summary>出库 Packing 报表：出库详情 + 公司参数；<paramref name="withInspection"/> 区分含/不含出货检验版式。</summary>
        [HttpGet("{id}/packing-report-bundle")]
        public async Task<ActionResult<ApiResponse<StockOutPackingReportBundleDto>>> GetPackingReportBundle(
            string id,
            [FromQuery] bool withInspection = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var dto = await _service.GetDetailViewAsync(id);
                if (dto == null)
                    return NotFound(ApiResponse<StockOutPackingReportBundleDto>.Fail("出库单不存在", 404));
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                    SaleSensitiveFieldMask521.ApplyStockOutDetailView(dto, true);
                var companyProfile = await CompanyProfileBundleLoader.LoadAsync(_db, _logger, cancellationToken);
                CompanyProfileBundleLoader.StripSmtpEmail(companyProfile);
                var bundle = new StockOutPackingReportBundleDto
                {
                    StockOut = dto,
                    CompanyProfile = companyProfile,
                    WithShipmentInspection = withInspection
                };
                return Ok(ApiResponse<StockOutPackingReportBundleDto>.Ok(bundle, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库 Packing 报表数据失败");
                return StatusCode(500, ApiResponse<StockOutPackingReportBundleDto>.Fail($"加载失败: {ex.Message}", 500));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<StockOutDetailViewDto>>> GetById(string id)
        {
            try
            {
                var dto = await _service.GetDetailViewAsync(id);
                if (dto == null)
                    return NotFound(ApiResponse<StockOutDetailViewDto>.Fail("出库单不存在", 404));
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                    SaleSensitiveFieldMask521.ApplyStockOutDetailView(dto, true);
                return Ok(ApiResponse<StockOutDetailViewDto>.Ok(dto, "获取出库单成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库单失败");
                return StatusCode(500, ApiResponse<StockOutDetailViewDto>.Fail($"获取出库单失败: {ex.Message}", 500));
            }
        }

        /// <summary>更新出库日期、出货方式、快递单号</summary>
        [HttpPatch("{id}/header")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateHeader(string id, [FromBody] UpdateStockOutHeaderRequest? body)
        {
            try
            {
                if (body == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateHeaderAsync(id, body, actorId);
                return Ok(ApiResponse<object>.Ok(null, "保存成功"));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新出库单头信息失败");
                return StatusCode(500, ApiResponse<object>.Fail($"保存失败: {ex.Message}", 500));
            }
        }

        /// <summary>销售明细申请出库前的数量上下文（服务端计算，前端只读展示）</summary>
        [HttpGet("request/apply-context")]
        public async Task<ActionResult<ApiResponse<StockOutApplyContextDto>>> GetRequestApplyContext(
            [FromQuery] string salesOrderId,
            [FromQuery] string salesOrderItemId)
        {
            try
            {
                var dto = await _service.GetApplyContextAsync(salesOrderId ?? string.Empty, salesOrderItemId ?? string.Empty);
                return Ok(ApiResponse<StockOutApplyContextDto>.Ok(dto, "ok"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<StockOutApplyContextDto>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<StockOutApplyContextDto>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库申请上下文失败");
                return StatusCode(500, ApiResponse<StockOutApplyContextDto>.Fail($"获取出库申请上下文失败: {ex.Message}", 500));
            }
        }

        [HttpPost("request")]
        public async Task<ActionResult<ApiResponse<StockOutRequest>>> CreateRequest([FromBody] StockOutRequestCreateApiRequest? body)
        {
            try
            {
                if (body == null)
                    return BadRequest(ApiResponse<StockOutRequest>.Fail("请求体不能为空", 400));
                var request = new CreateStockOutRequestRequest
                {
                    RequestCode = body.RequestCode ?? string.Empty,
                    SalesOrderId = body.SalesOrderId ?? string.Empty,
                    SalesOrderItemId = body.SalesOrderItemId ?? string.Empty,
                    MaterialCode = body.MaterialCode ?? string.Empty,
                    MaterialName = body.MaterialName ?? string.Empty,
                    Quantity = body.Quantity,
                    CustomerId = body.CustomerId ?? string.Empty,
                    RequestUserId = body.RequestUserId ?? string.Empty,
                    RequestDate = body.RequestDate,
                    Remark = body.Remark,
                    ShipmentMethod = body.ShipmentMethod,
                    RegionType = body.RegionType,
                };
                var entity = await _service.CreateStockOutRequestAsync(request);
                return Ok(ApiResponse<StockOutRequest>.Ok(entity, "创建出库申请成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<StockOutRequest>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<StockOutRequest>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建出库申请失败");
                return StatusCode(500, ApiResponse<StockOutRequest>.Fail($"创建出库申请失败: {ex.Message}", 500));
            }
        }

        [HttpGet("request")]
        public async Task<ActionResult<ApiResponse<IEnumerable<StockOutRequestListItemDto>>>> GetRequests()
        {
            try
            {
                var list = await _service.GetStockOutRequestListAsync();
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                {
                    var masked = list.ToList();
                    SaleSensitiveFieldMask521.ApplyStockOutRequestListItems(masked, true);
                    list = masked;
                }

                return Ok(ApiResponse<IEnumerable<StockOutRequestListItemDto>>.Ok(list, "获取出库通知列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库通知列表失败");
                return StatusCode(500, ApiResponse<IEnumerable<StockOutRequestListItemDto>>.Fail($"获取出库通知列表失败: {ex.Message}", 500));
            }
        }

        [HttpPost("execute")]
        public async Task<ActionResult<ApiResponse<StockOut>>> Execute([FromBody] ExecuteStockOutRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<StockOut>.Fail("请求体不能为空", 400));
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation(
                    "[SellLineStockOutSync] API ExecuteStockOut request StockOutRequestId={RequestId} StockOutCode={Code} ItemCount={Count}",
                    request.StockOutRequestId,
                    request.StockOutCode,
                    request.Items?.Count ?? 0);
                var entity = await _service.ExecuteStockOutAsync(request, actorId);
                _logger.LogInformation(
                    "[SellLineStockOutSync] API ExecuteStockOut ok StockOutId={StockOutId} StockOutCode={Code} SellOrderItemId={SellOrderItemId}",
                    entity.Id,
                    entity.StockOutCode,
                    entity.SellOrderItemId ?? "(null)");
                return Ok(ApiResponse<StockOut>.Ok(entity, "执行出库成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<StockOut>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<StockOut>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行出库失败");
                return StatusCode(500, ApiResponse<StockOut>.Fail($"执行出库失败: {ex.Message}", 500));
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(string id, [FromQuery] short status)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation(
                    "[SellLineStockOutSync] API PatchStockOutStatus StockOutId={StockOutId} Status={Status} Actor={Actor}",
                    id,
                    status,
                    actorId ?? "(null)");
                await _service.UpdateStatusAsync(id, status, actorId);
                _logger.LogInformation("[SellLineStockOutSync] API PatchStockOutStatus done StockOutId={StockOutId}", id);
                return Ok(ApiResponse<object>.Ok(null, "更新状态成功"));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新出库单状态失败");
                return StatusCode(500, ApiResponse<object>.Fail($"更新状态失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteStockOut(string id)
        {
            try
            {
                var entity = await _stockOutRepo.GetByIdAsync(id);
                if (entity == null)
                    return NotFound(ApiResponse<object>.Fail("出库单不存在", 404));
                if (entity.Status == 2 || entity.Status == 4)
                    return BadRequest(ApiResponse<object>.Fail("已执行出库的单据不能普通删除", 400));

                var items = (await _stockOutItemRepo.FindAsync(x => x.StockOutId == entity.Id)).ToList();
                var itemIds = items.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var exts = (await _stockOutItemExtendRepo.GetAllAsync())
                    .Where(x => itemIds.Contains(x.Id))
                    .ToList();
                foreach (var ext in exts)
                    await _stockOutItemExtendRepo.DeleteAsync(ext.Id);
                foreach (var item in items)
                    await _stockOutItemRepo.DeleteAsync(item.Id);
                await _stockOutRepo.DeleteAsync(entity.Id);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "删除出库单成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除出库单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除出库单失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{id}/force-delete")]
        public async Task<ActionResult<ApiResponse<object>>> ForceDeleteStockOut(string id, [FromBody] ForceDeleteStockOutRequest? body)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(403, ApiResponse<object>.Fail("未登录或身份无效", 403));

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
                if (summary?.IsSysAdmin != true)
                    return StatusCode(403, ApiResponse<object>.Fail("仅系统管理员可执行强制删除", 403));

                if (body == null || string.IsNullOrWhiteSpace(body.ConfirmBillCode))
                    return BadRequest(ApiResponse<object>.Fail("请填写 confirmBillCode", 400));

                var entity = await _stockOutRepo.GetByIdAsync(id);
                if (entity == null)
                    return NotFound(ApiResponse<object>.Fail("出库单不存在", 404));
                if (!string.Equals(body.ConfirmBillCode.Trim(), entity.StockOutCode.Trim(), StringComparison.Ordinal))
                    return BadRequest(ApiResponse<object>.Fail("确认单号不匹配，已拒绝删除", 400));

                var items = (await _stockOutItemRepo.FindAsync(x => x.StockOutId == entity.Id)).ToList();
                var itemIds = items.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var exts = (await _stockOutItemExtendRepo.GetAllAsync())
                    .Where(x => itemIds.Contains(x.Id))
                    .ToList();
                foreach (var ext in exts)
                    await _stockOutItemExtendRepo.DeleteAsync(ext.Id);
                foreach (var item in items)
                    await _stockOutItemRepo.DeleteAsync(item.Id);
                await _stockOutRepo.DeleteAsync(entity.Id);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "强制删除出库单成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制删除出库单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"强制删除出库单失败: {ex.Message}", 500));
            }
        }
    }
}
