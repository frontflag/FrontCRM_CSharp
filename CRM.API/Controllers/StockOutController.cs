using System.Security.Claims;
using CRM.API.Authorization;
using System.Text;
using System.Threading;
using CRM.API.Models.DTOs;
using CRM.API.Services;
using CRM.API.Utilities;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Services;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/stock-out")]
    public class StockOutController : ControllerBase
    {
        private readonly IStockOutService _service;
        private readonly IRepository<StockOut> _stockOutRepo;
        private readonly IRepository<StockOutRequest> _stockOutRequestRepo;
        private readonly IRepository<StockOutItem> _stockOutItemRepo;
        private readonly IRepository<StockOutItemExtend> _stockOutItemExtendRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _db;
        private readonly IRbacService _rbacService;
        private readonly IForceDeleteGuardService _forceDeleteGuard;
        private readonly ICustomsPendlistService _customsPendlistService;
        private readonly IExportOperationLogService _exportLog;
        private readonly IStockOutOpsCheckService _opsCheck;
        private readonly IFinanceReceivableService _financeReceivableService;
        private readonly ILogger<StockOutController> _logger;

        public StockOutController(
            IStockOutService service,
            IRepository<StockOut> stockOutRepo,
            IRepository<StockOutRequest> stockOutRequestRepo,
            IRepository<StockOutItem> stockOutItemRepo,
            IRepository<StockOutItemExtend> stockOutItemExtendRepo,
            IUnitOfWork unitOfWork,
            ApplicationDbContext db,
            IRbacService rbacService,
            IForceDeleteGuardService forceDeleteGuard,
            ICustomsPendlistService customsPendlistService,
            IExportOperationLogService exportLog,
            IStockOutOpsCheckService opsCheck,
            IFinanceReceivableService financeReceivableService,
            ILogger<StockOutController> logger)
        {
            _service = service;
            _stockOutRepo = stockOutRepo;
            _stockOutRequestRepo = stockOutRequestRepo;
            _stockOutItemRepo = stockOutItemRepo;
            _stockOutItemExtendRepo = stockOutItemExtendRepo;
            _unitOfWork = unitOfWork;
            _db = db;
            _rbacService = rbacService;
            _forceDeleteGuard = forceDeleteGuard;
            _customsPendlistService = customsPendlistService;
            _exportLog = exportLog;
            _opsCheck = opsCheck;
            _financeReceivableService = financeReceivableService;
            _logger = logger;
        }

        public class ForceDeleteStockOutRequest
        {
            public string ConfirmBillCode { get; set; } = string.Empty;
        }

        /// <summary>出库运维检查（系统/平台管理员、财务总监）：全量只读对账。</summary>
        [HttpPost("ops-check")]
        public async Task<ActionResult<ApiResponse<StockOutOpsCheckResultDto>>> RunOpsCheck(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(403, ApiResponse<StockOutOpsCheckResultDto>.Fail("未登录或身份无效", 403));

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
                if (!InventoryOpsCheckAccessRules.CanAccess(summary))
                    return StatusCode(403, ApiResponse<StockOutOpsCheckResultDto>.Fail("仅系统管理员、平台管理员或财务总监可做出库运维检查", 403));

                var result = await _opsCheck.RunAsync(cancellationToken);
                return Ok(ApiResponse<StockOutOpsCheckResultDto>.Ok(result, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "出库运维检查失败");
                return StatusCode(500, ApiResponse<StockOutOpsCheckResultDto>.Fail($"检查失败: {ex.Message}", 500));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] StockOutListQueryRequest? filter,
            [FromQuery] string? keyword,
            [FromQuery] string? sourceCode,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                filter ??= new StockOutListQueryRequest();
                filter.CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(keyword) && string.IsNullOrWhiteSpace(filter.Keyword))
                    filter.Keyword = keyword;
                if (!string.IsNullOrWhiteSpace(sourceCode) && string.IsNullOrWhiteSpace(filter.SourceCode))
                    filter.SourceCode = sourceCode;

                var result = await _service.GetStockOutListPagedAsync(filter, page, pageSize, cancellationToken);
                var items = result.Items.ToList();
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                    SaleSensitiveFieldMask521.ApplyStockOutListItems(items, true);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items,
                        total = result.TotalCount,
                        page = result.PageIndex,
                        pageSize = result.PageSize
                    },
                    message = "获取出库单列表成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库单列表失败");
                return StatusCode(500, new { success = false, message = $"获取出库单列表失败: {ex.Message}" });
            }
        }

        /// <summary>按当前筛选导出出库单列表 CSV，并写入操作审计。</summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportList(
            [FromQuery] StockOutListQueryRequest? filter,
            [FromQuery] string? keyword,
            [FromQuery] string? sourceCode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                filter ??= new StockOutListQueryRequest();
                filter.CurrentUserId = InventoryExportHttp.UserId(User);
                if (!string.IsNullOrWhiteSpace(keyword) && string.IsNullOrWhiteSpace(filter.Keyword))
                    filter.Keyword = keyword;
                if (!string.IsNullOrWhiteSpace(sourceCode) && string.IsNullOrWhiteSpace(filter.SourceCode))
                    filter.SourceCode = sourceCode;

                var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User);
                if (mask521)
                {
                    filter.CustomerName = null;
                    filter.SalesUserName = null;
                }

                var (items, truncated, _) = await InventoryExportHttp.CollectForExportAsync(
                    (page, pageSize, ct) => _service.GetStockOutListPagedAsync(filter, page, pageSize, ct),
                    cancellationToken: cancellationToken);

                if (mask521)
                    SaleSensitiveFieldMask521.ApplyStockOutListItems(items, true);

                var sb = new StringBuilder();
                sb.AppendLine(string.Join(',',
                    "状态", "出库类型", "出库单号", "来源单号", "装箱单号",
                    "客户中文名称", "客户英文名称", "业务员", "出库日期",
                    "数量", "销售单价", "销售单价币别", "金额", "金额币别",
                    "出货方式", "货代单号", "备注", "创建时间", "创建人"));

                foreach (var r in items)
                {
                    sb.AppendLine(string.Join(',',
                        InventoryExportHttp.CsvCell(InventoryExportHttp.StockOutStatusLabel(r.Status)),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.StockOutTypeLabel(r.StockOutType)),
                        InventoryExportHttp.CsvCell(r.StockOutCode),
                        InventoryExportHttp.CsvCell(r.SourceCode),
                        InventoryExportHttp.CsvCell(r.PackingCodes),
                        InventoryExportHttp.CsvCell(mask521 ? "***" : r.CustomerChineseName),
                        InventoryExportHttp.CsvCell(mask521 ? "***" : r.CustomerEnglishName),
                        InventoryExportHttp.CsvCell(mask521 ? "***" : r.SalesUserName),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDate(r.StockOutDate)),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDecimal(r.TotalQuantity)),
                        InventoryExportHttp.CsvCell(mask521 ? string.Empty : r.SalesUnitPriceSummary),
                        InventoryExportHttp.CsvCell(mask521 ? string.Empty : InventoryExportHttp.CurrencyLabel(r.SalesUnitPriceCurrencyCode)),
                        InventoryExportHttp.CsvCell(mask521 ? string.Empty : InventoryExportHttp.FormatDecimal(r.TotalAmount)),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.CurrencyLabel(r.CurrencyCode)),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.ShipmentMethodLabel(r.ShipmentMethod)),
                        InventoryExportHttp.CsvCell(r.FreightForwarderOrderNo),
                        InventoryExportHttp.CsvCell(r.Remark),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDateTime(r.CreateTime)),
                        InventoryExportHttp.CsvCell(r.CreateUserName)));
                }

                var filters = ExportOperationAudit.NormalizeFilters(new Dictionary<string, object?>
                {
                    ["keyword"] = filter.Keyword,
                    ["sourceCode"] = filter.SourceCode,
                    ["status"] = filter.Status,
                    ["stockOutCode"] = filter.StockOutCode,
                    ["packingCode"] = filter.PackingCode,
                    ["shipmentMethod"] = filter.ShipmentMethod,
                    ["customerName"] = filter.CustomerName,
                    ["salesUserName"] = filter.SalesUserName,
                    ["remark"] = filter.Remark,
                    ["stockOutType"] = filter.StockOutType,
                    ["stockOutDateFrom"] = filter.StockOutDateFrom,
                    ["stockOutDateTo"] = filter.StockOutDateTo,
                    ["freightForwarderOrderNo"] = filter.FreightForwarderOrderNo
                });

                var truncNote = truncated ? "（已截断）" : string.Empty;
                await _exportLog.AppendAsync(new ExportOperationLogRequest
                {
                    BizType = BusinessLogTypes.StockOut,
                    RecordId = ExportOperationAudit.ListRecordId,
                    RecordCode = ExportOperationAudit.StockOutListRecordCode,
                    ActionType = InventoryExportActionTypes.StockOutListExport,
                    ExportKind = ExportAuditKinds.StockOutList,
                    OperationDesc = $"导出出库单列表 {items.Count} 条{truncNote}",
                    ExportedCount = items.Count,
                    Truncated = truncated,
                    Filters = filters,
                    FiltersMasked = mask521,
                    OperatorUserId = InventoryExportHttp.UserId(User),
                    OperatorUserName = InventoryExportHttp.UserName(User)
                }, cancellationToken);

                return InventoryExportHttp.CsvFile(sb.ToString(), "出库单列表.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出出库单列表失败");
                return StatusCode(500, new { success = false, message = $"导出出库单列表失败: {ex.Message}" });
            }
        }

        /// <summary>出库明细（stockoutitem）列表，query 与 <see cref="StockOutItemListQuery"/> 一致。</summary>
        [HttpGet("items")]
        public async Task<IActionResult> GetItems(
            [FromQuery] StockOutItemListQuery? query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                query ??= new StockOutItemListQuery();
                query.CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _service.GetStockOutItemListPagedAsync(query, page, pageSize, cancellationToken);
                var items = result.Items.ToList();
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                    SaleSensitiveFieldMask521.ApplyStockOutItemListRows(items, true);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items,
                        total = result.TotalCount,
                        page = result.PageIndex,
                        pageSize = result.PageSize
                    },
                    message = "获取出库明细列表成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库明细列表失败");
                return StatusCode(500, new { success = false, message = $"获取出库明细列表失败: {ex.Message}" });
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
                short? warehouseRegionType = null;
                var warehouseId = dto.WarehouseId?.Trim();
                if (!string.IsNullOrEmpty(warehouseId))
                {
                    var regionType = await _db.Warehouses.AsNoTracking()
                        .Where(w => w.Id == warehouseId)
                        .Select(w => (short?)w.RegionType)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (regionType.HasValue)
                        warehouseRegionType = RegionTypeCode.Normalize(regionType.Value);
                }
                var bundle = new StockOutInvoiceReportBundleDto
                {
                    StockOut = dto,
                    CompanyProfile = companyProfile,
                    WarehouseRegionType = warehouseRegionType
                };
                return Ok(ApiResponse<StockOutInvoiceReportBundleDto>.Ok(bundle, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库 Invoice 报表数据失败");
                return StatusCode(500, ApiResponse<StockOutInvoiceReportBundleDto>.Fail($"加载失败: {ex.Message}", 500));
            }
        }

        /// <summary>出库 Packing 报表：出库详情 + 公司参数；<paramref name="withInspection"/> 区分含/不含出货检验版式；<paramref name="packingId"/> 为装箱单主键，用于直接读取 packing.code 与地址。</summary>
        [HttpGet("{id}/packing-report-bundle")]
        public async Task<ActionResult<ApiResponse<StockOutPackingReportBundleDto>>> GetPackingReportBundle(
            string id,
            [FromQuery] bool withInspection = false,
            [FromQuery] string? packingId = null,
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
                var (packingCode, packingAddresses, deliveryMethod) =
                    await TryLoadPackingReportExtrasAsync(packingId, cancellationToken);
                ApplyCustomerToPackingAddressPanel(packingAddresses, dto.CustomerName);
                var bundle = new StockOutPackingReportBundleDto
                {
                    StockOut = dto,
                    CompanyProfile = companyProfile,
                    WithShipmentInspection = withInspection,
                    PackingCode = packingCode,
                    PackingAddresses = packingAddresses,
                    DeliveryMethod = deliveryMethod
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

        /// <summary>出库详情应收摘要（与出库单同一读权限；非销售出库为空）。</summary>
        [HttpGet("{id}/receivables")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<StockOutDetailReceivableRowDto>>>> GetReceivables(
            string id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var rows = await _service.GetDetailReceivablesAsync(id, cancellationToken);
                return Ok(ApiResponse<IReadOnlyList<StockOutDetailReceivableRowDto>>.Ok(rows, "OK"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<IReadOnlyList<StockOutDetailReceivableRowDto>>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<IReadOnlyList<StockOutDetailReceivableRowDto>>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库单应收摘要失败");
                return StatusCode(500, ApiResponse<IReadOnlyList<StockOutDetailReceivableRowDto>>.Fail($"加载失败: {ex.Message}", 500));
            }
        }

        /// <summary>列表右侧操作面板聚合</summary>
        [HttpGet("{id}/ops-aggregates")]
        public async Task<ActionResult<ApiResponse<StockOutOpsAggregatesDto>>> GetOpsAggregates(
            string id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var dto = await _service.GetOpsAggregatesAsync(id, cancellationToken);
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                    SaleSensitiveFieldMask521.ApplyStockOutOpsAggregates(dto, true);
                return Ok(ApiResponse<StockOutOpsAggregatesDto>.Ok(dto, "OK"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<StockOutOpsAggregatesDto>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<StockOutOpsAggregatesDto>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库单操作面板失败");
                return StatusCode(500, ApiResponse<StockOutOpsAggregatesDto>.Fail($"加载失败: {ex.Message}", 500));
            }
        }

        /// <summary>更新出库日期、出货方式、快递公司、快递单号</summary>
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
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
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

        /// <summary>标记完成对话框上下文</summary>
        [HttpGet("{id}/mark-finish-context")]
        public async Task<ActionResult<ApiResponse<StockOutMarkFinishContextDto>>> GetMarkFinishContext(
            string id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var dto = await BuildMarkFinishContextAsync(id, cancellationToken);
                if (dto == null)
                    return NotFound(ApiResponse<StockOutMarkFinishContextDto>.Fail("出库单不存在", 404));
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                {
                    dto.CustomerName = null;
                    dto.ShipAddress = null;
                }
                return Ok(ApiResponse<StockOutMarkFinishContextDto>.Ok(dto, "OK"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取标记完成上下文失败");
                return StatusCode(500, ApiResponse<StockOutMarkFinishContextDto>.Fail($"加载失败: {ex.Message}", 500));
            }
        }

        /// <summary>标记完成：更新实际出库日期、快递单号、备注并置状态为已完成</summary>
        [HttpPost("{id}/mark-finished")]
        public async Task<ActionResult<ApiResponse<object>>> MarkFinished(string id, [FromBody] MarkStockOutFinishedRequest? body)
        {
            try
            {
                if (body == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.MarkFinishedAsync(id, body, actorId);
                return Ok(ApiResponse<object>.Ok(null, "已标记为完成"));
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
                _logger.LogError(ex, "标记出库单完成失败");
                return StatusCode(500, ApiResponse<object>.Fail($"标记完成失败: {ex.Message}", 500));
            }
        }

        /// <summary>销售明细申请出库前的数量上下文（服务端计算，前端只读展示）</summary>
        [HttpGet("request/apply-context")]
        public async Task<ActionResult<ApiResponse<StockOutApplyContextDto>>> GetRequestApplyContext(
            [FromQuery] string salesOrderId,
            [FromQuery] string salesOrderItemId,
            [FromQuery] decimal? requestedQty = null)
        {
            try
            {
                var dto = await _service.GetApplyContextAsync(
                    salesOrderId ?? string.Empty,
                    salesOrderItemId ?? string.Empty,
                    requestedQty);
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
                    ExpressCompany = body.ExpressCompany,
                    RegionType = body.RegionType,
                    UseOverseasWarehouseAndCustoms = body.UseOverseasWarehouseAndCustoms,
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
        public async Task<IActionResult> GetRequests(
            [FromQuery] StockOutRequestListQueryRequest filter,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                filter ??= new StockOutRequestListQueryRequest();
                filter.CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _service.GetStockOutRequestListPagedAsync(filter, page, pageSize, cancellationToken);
                var items = result.Items.ToList();
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                    SaleSensitiveFieldMask521.ApplyStockOutRequestListItems(items, true);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items,
                        total = result.TotalCount,
                        page = result.PageIndex,
                        pageSize = result.PageSize
                    },
                    message = "获取出库通知列表成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库通知列表失败");
                return StatusCode(500, new { success = false, message = $"获取出库通知列表失败: {ex.Message}" });
            }
        }

        [HttpDelete("request/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteRequest(string id)
        {
            try
            {
                var entity = await _stockOutRequestRepo.GetByIdAsync(id);
                if (entity == null)
                    return NotFound(ApiResponse<object>.Fail("出库通知不存在", 404));
                if (entity.Status == StockOutRequestStatusCode.StockedOut)
                    return BadRequest(ApiResponse<object>.Fail("已出库通知不能普通删除", 400));

                var guard = await _forceDeleteGuard.CanForceDeleteStockOutRequestAsync(entity.Id);
                if (!guard.CanDelete)
                    return BadRequest(ApiResponse<object>.Fail(guard.Message, 400));

                var actingUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? User.FindFirst("userId")?.Value;

                if (StockOutTypeCode.NormalizeForNotify(entity.StockOutType) == StockOutTypeCode.Customs)
                {
                    await _customsPendlistService.RevertPendlistOnCustomsOutNotifyDeleteAsync(
                        entity.Id,
                        actingUserId);
                }
                else
                {
                    await _customsPendlistService.EnsureSalesNotifyDeletableAsync(entity.Id);
                }

                await _stockOutRequestRepo.DeleteAsync(entity.Id);
                if (StockOutTypeCode.NormalizeForNotify(entity.StockOutType) != StockOutTypeCode.Customs)
                {
                    await _customsPendlistService.CancelBySalesStockOutNotifyAsync(entity.Id, actingUserId);
                }
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "删除出库通知成功"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除出库通知失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除出库通知失败: {ex.Message}", 500));
            }
        }

        [HttpPost("request/{id}/force-delete")]
        public async Task<ActionResult<ApiResponse<object>>> ForceDeleteRequest(string id, [FromBody] ForceDeleteStockOutRequest? body)
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
                await _service.ForceDeleteStockOutRequestAsync(
                    id,
                    body.ConfirmBillCode.Trim(),
                    userId.Trim(),
                    string.IsNullOrWhiteSpace(userName) ? null : userName.Trim());

                return Ok(ApiResponse<object>.Ok(null, "强制删除出库通知成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制删除出库通知失败");
                return StatusCode(500, ApiResponse<object>.Fail($"强制删除出库通知失败: {ex.Message}", 500));
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
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var entity = await _stockOutRepo.GetByIdAsync(id);
                if (entity == null)
                    return NotFound(ApiResponse<object>.Fail("出库单不存在", 404));
                if (entity.Status == 2 || entity.Status == 4)
                    return BadRequest(ApiResponse<object>.Fail("已执行出库的单据不能普通删除", 400));

                var guard = await _forceDeleteGuard.CanForceDeleteStockOutAsync(entity.Id);
                if (!guard.CanDelete)
                    return BadRequest(ApiResponse<object>.Fail(guard.Message, 400));
                await _financeReceivableService.TrySoftDeleteForStockOutAsync(entity.Id, userId);
                await SoftDeleteStockOutCascadeAsync(entity.Id);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "删除出库单成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除出库单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除出库单失败: {ex.Message}", 500));
            }
        }

        /// <summary>强制删除前只读预览（系统管理员 / 平台管理员）：应收与核销状态、后果。</summary>
        [HttpGet("{id}/force-delete-preview")]
        public async Task<ActionResult<ApiResponse<StockOutForceDeletePreviewDto>>> GetForceDeletePreview(
            string id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(403, ApiResponse<StockOutForceDeletePreviewDto>.Fail("未登录或身份无效", 403));

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
                if (!ManagementAccountPolicy.CanForceDelete(summary))
                    return StatusCode(403, ApiResponse<StockOutForceDeletePreviewDto>.Fail("仅系统管理员或平台管理员可强制删除出库单", 403));

                var dto = await _service.GetForceDeletePreviewAsync(id, cancellationToken);
                return Ok(ApiResponse<StockOutForceDeletePreviewDto>.Ok(dto, "OK"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<StockOutForceDeletePreviewDto>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<StockOutForceDeletePreviewDto>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库强制删除预览失败");
                return StatusCode(500, ApiResponse<StockOutForceDeletePreviewDto>.Fail($"加载失败: {ex.Message}", 500));
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
                if (!ManagementAccountPolicy.CanForceDelete(summary))
                    return StatusCode(403, ApiResponse<object>.Fail("仅系统管理员或平台管理员可执行强制删除", 403));

                if (body == null || string.IsNullOrWhiteSpace(body.ConfirmBillCode))
                    return BadRequest(ApiResponse<object>.Fail("请填写 confirmBillCode", 400));

                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                await _service.ForceDeleteStockOutAsync(
                    id,
                    body.ConfirmBillCode.Trim(),
                    userId.Trim(),
                    string.IsNullOrWhiteSpace(userName) ? null : userName.Trim());

                return Ok(ApiResponse<object>.Ok(null, "强制删除出库单成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制删除出库单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"强制删除出库单失败: {ex.Message}", 500));
            }
        }

        private static PackingReportAddressPanelDto EmptyPackingAddressPanel() =>
            new()
            {
                BillToLines = new List<string> { "—", "—", "—", "—" },
                ShipToLines = new List<string> { "—", "—", "—", "—" }
            };

        private static void ApplyCustomerToPackingAddressPanel(PackingReportAddressPanelDto panel, string? customerName)
        {
            var customer = string.IsNullOrWhiteSpace(customerName) ? "—" : customerName.Trim();
            SetCustomerFirstPackingAddressLine(panel.BillToLines, customer);
            SetCustomerFirstPackingAddressLine(panel.ShipToLines, customer);
        }

        private static void SetCustomerFirstPackingAddressLine(List<string> lines, string customer)
        {
            if (lines.Count >= 4)
                lines[0] = customer;
            else if (lines.Count == 3)
                lines.Insert(0, customer);
            else
            {
                lines.Clear();
                lines.Add(customer);
                while (lines.Count < 4) lines.Add("—");
                return;
            }
            while (lines.Count < 4) lines.Add("—");
            if (lines.Count > 4) lines.RemoveRange(4, lines.Count - 4);
        }

        private static string PackingAddressLine(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

        /// <summary>标记完成对话框：客户、送货地址、关联装箱单及当前头表字段。</summary>
        private async Task<StockOutMarkFinishContextDto?> BuildMarkFinishContextAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var sid = id?.Trim();
            if (string.IsNullOrEmpty(sid))
                return null;

            var detail = await _service.GetDetailViewAsync(sid);
            if (detail == null)
                return null;

            var stockOut = await _db.StockOuts.AsNoTracking()
                .Where(x => x.Id == sid && !x.IsDeleted)
                .Select(x => new { x.StockOutCode, x.StockOutDate, x.CourierTrackingNo, x.Remark })
                .FirstOrDefaultAsync(cancellationToken);

            var directPackingIds = await _db.StockOutItems.AsNoTracking()
                .Where(i => i.StockOutId == sid && !i.IsDeleted && i.PackingId != null && i.PackingId != "")
                .Select(i => i.PackingId!)
                .Distinct()
                .ToListAsync(cancellationToken);

            var pickingTaskItemIds = await _db.StockOutItems.AsNoTracking()
                .Where(i => i.StockOutId == sid && !i.IsDeleted && i.PickingTaskItemId != null && i.PickingTaskItemId != "")
                .Select(i => i.PickingTaskItemId!)
                .Distinct()
                .ToListAsync(cancellationToken);

            var viaPickingPackingIds = new List<string>();
            if (pickingTaskItemIds.Count > 0)
            {
                viaPickingPackingIds = await (
                    from pti in _db.PickingTaskItems.AsNoTracking()
                    join pt in _db.PickingTasks.AsNoTracking() on pti.PickingTaskId equals pt.Id
                    where pickingTaskItemIds.Contains(pti.Id)
                          && !pti.IsDeleted
                          && !pt.IsDeleted
                          && pt.PackingId != null
                          && pt.PackingId != ""
                    select pt.PackingId!
                ).Distinct().ToListAsync(cancellationToken);
            }

            var packingIds = directPackingIds
                .Concat(viaPickingPackingIds)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var packings = new List<StockOutMarkFinishPackingDto>();
            string? shipAddress = null;

            if (packingIds.Count > 0)
            {
                var packingRows = await _db.Packings.AsNoTracking()
                    .Where(p => packingIds.Contains(p.Id) && !p.IsDeleted)
                    .OrderBy(p => p.Code)
                    .Select(p => new { p.Id, p.Code })
                    .ToListAsync(cancellationToken);

                var shipRows = await _db.PackingExtendShips.AsNoTracking()
                    .Where(s => packingIds.Contains(s.PackingId))
                    .Select(s => new { s.PackingId, s.ShipAddress })
                    .ToListAsync(cancellationToken);
                var shipByPacking = shipRows.ToDictionary(
                    x => x.PackingId,
                    x => x.ShipAddress,
                    StringComparer.OrdinalIgnoreCase);

                foreach (var p in packingRows)
                {
                    packings.Add(new StockOutMarkFinishPackingDto
                    {
                        Id = p.Id,
                        Code = string.IsNullOrWhiteSpace(p.Code) ? null : p.Code.Trim()
                    });
                    if (shipAddress == null
                        && shipByPacking.TryGetValue(p.Id, out var addr)
                        && !string.IsNullOrWhiteSpace(addr))
                    {
                        shipAddress = addr.Trim();
                    }
                }
            }

            return new StockOutMarkFinishContextDto
            {
                StockOutId = sid,
                StockOutCode = stockOut?.StockOutCode ?? detail.StockOutCode,
                CustomerName = detail.CustomerName,
                ShipAddress = shipAddress,
                Packings = packings,
                StockOutDate = detail.Status == 4
                    ? stockOut?.StockOutDate ?? detail.StockOutDate
                    : null,
                CourierTrackingNo = string.IsNullOrWhiteSpace(stockOut?.CourierTrackingNo)
                    ? detail.CourierTrackingNo
                    : stockOut!.CourierTrackingNo.Trim(),
                Remark = stockOut?.Remark ?? detail.Remark
            };
        }

        /// <summary>按装箱单主键直接读取 code 与账单/送货地址（packing + packing_extend_ship）。</summary>
        private async Task<(string? PackingCode, PackingReportAddressPanelDto Addresses, short? DeliveryMethod)>
            TryLoadPackingReportExtrasAsync(string? packingId, CancellationToken cancellationToken)
        {
            var pid = packingId?.Trim();
            if (string.IsNullOrEmpty(pid))
                return (null, EmptyPackingAddressPanel(), null);

            try
            {
                var packing = await _db.Packings
                    .AsNoTracking()
                    .Where(p => p.Id == pid && !p.IsDeleted)
                    .Select(p => new { p.Code })
                    .FirstOrDefaultAsync(cancellationToken);
                if (packing == null)
                    return (null, EmptyPackingAddressPanel(), null);

                var packingCode = string.IsNullOrWhiteSpace(packing.Code) ? null : packing.Code.Trim();

                var ship = await _db.PackingExtendShips
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.PackingId == pid, cancellationToken);
                if (ship == null)
                    return (packingCode, EmptyPackingAddressPanel(), null);

                return (
                    packingCode,
                    new PackingReportAddressPanelDto
                    {
                        BillToLines = new List<string>
                        {
                            PackingAddressLine(ship.BillAddress),
                            PackingAddressLine(ship.BillAttn),
                            PackingAddressLine(ship.BillTel)
                        },
                        ShipToLines = new List<string>
                        {
                            PackingAddressLine(ship.ShipAddress),
                            PackingAddressLine(ship.ShipAttn),
                            PackingAddressLine(ship.ShipTel)
                        }
                    },
                    ship.DeliveryMethod);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "装箱单扩展信息加载失败 PackingId={PackingId}", pid);
                return (null, EmptyPackingAddressPanel(), null);
            }
        }

        private async Task SoftDeleteStockOutCascadeAsync(string stockOutId)
        {
            var items = (await _stockOutItemRepo.FindAsync(x => x.StockOutId == stockOutId)).ToList();
            var itemIds = items.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var exts = (await _stockOutItemExtendRepo.GetAllAsync())
                .Where(x => itemIds.Contains(x.Id))
                .ToList();
            foreach (var ext in exts)
                await _stockOutItemExtendRepo.DeleteAsync(ext.Id);
            foreach (var item in items)
                await _stockOutItemRepo.DeleteAsync(item.Id);
            await _stockOutRepo.DeleteAsync(stockOutId);
        }
    }
}
