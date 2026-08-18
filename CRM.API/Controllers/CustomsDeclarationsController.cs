using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CRM.API.Authorization;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/customs-declarations")]
public class CustomsDeclarationsController : ControllerBase
{
    private readonly ICustomsDeclarationService _service;
    private readonly ICustomsV2FlowService _customsV2FlowService;
    private readonly ICustomsDeclarationBusinessRecordsQuery _businessRecordsQuery;
    private readonly IRbacService _rbacService;
    private readonly IDataPermissionService _dataPermissionService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CustomsDeclarationsController> _logger;

    public CustomsDeclarationsController(
        ICustomsDeclarationService service,
        ICustomsV2FlowService customsV2FlowService,
        ICustomsDeclarationBusinessRecordsQuery businessRecordsQuery,
        IRbacService rbacService,
        IDataPermissionService dataPermissionService,
        ApplicationDbContext db,
        ILogger<CustomsDeclarationsController> logger)
    {
        _service = service;
        _customsV2FlowService = customsV2FlowService;
        _businessRecordsQuery = businessRecordsQuery;
        _rbacService = rbacService;
        _dataPermissionService = dataPermissionService;
        _db = db;
        _logger = logger;
    }

    public class ForceDeleteCustomsDeclarationRequest
    {
        public string ConfirmBillCode { get; set; } = string.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CustomsDeclarationListItemDto>>>> GetList(
        [FromQuery] string? declarationCode,
        [FromQuery] string? stockOutRequestId,
        [FromQuery] short? internalStatus,
        [FromQuery] short? customsClearanceStatus,
        [FromQuery] short? declarationType,
        [FromQuery] DateTime? declareDateFrom,
        [FromQuery] DateTime? declareDateTo,
        [FromQuery] int take = 500)
    {
        try
        {
            if (!await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<List<CustomsDeclarationListItemDto>>.Fail("当前账号无权访问报关模块", 403));

            var n = Math.Clamp(take, 1, 1000);
            var codeQ = (declarationCode ?? string.Empty).Trim();
            var sorQ = (stockOutRequestId ?? string.Empty).Trim();

            var dq = _db.CustomsDeclarations.AsNoTracking();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
                if (!CustomsModuleAccessRules.BypassLogisticsDataScopeForCustomsList(summary))
                {
                    dq = await _dataPermissionService.ApplyLogisticsCreatorUserScopeAsync(
                        userId,
                        dq,
                        d => d.CreateByUserId,
                        CancellationToken.None);
                }
            }

            if (!string.IsNullOrEmpty(codeQ))
                dq = dq.Where(d => EF.Functions.ILike(d.DeclarationCode, $"%{codeQ}%"));
            if (!string.IsNullOrEmpty(sorQ))
            {
                var decIdsForSor = await _db.CustomsDeclarationItems.AsNoTracking()
                    .Where(i => i.StockOutRequestId == sorQ)
                    .Select(i => i.DeclarationId)
                    .Distinct()
                    .ToListAsync();
                dq = dq.Where(d => decIdsForSor.Contains(d.Id));
            }
            if (internalStatus.HasValue)
                dq = dq.Where(d => d.InternalStatus == internalStatus.Value);
            if (customsClearanceStatus.HasValue)
                dq = dq.Where(d => d.CustomsClearanceStatus == customsClearanceStatus.Value);
            if (declarationType.HasValue)
                dq = dq.Where(d => d.DeclarationType == declarationType.Value);
            if (declareDateFrom.HasValue)
            {
                var from = declareDateFrom.Value.Date;
                dq = dq.Where(d => d.DeclareDate >= from);
            }

            if (declareDateTo.HasValue)
            {
                var toExclusive = declareDateTo.Value.Date.AddDays(1);
                dq = dq.Where(d => d.DeclareDate < toExclusive);
            }

            var query =
                from d in dq
                join b in _db.CustomsBrokers.AsNoTracking().IgnoreQueryFilters() on d.CustomsBrokerId equals b.Id
                join u in _db.Users.AsNoTracking() on d.CreateByUserId equals u.Id into uj
                from u in uj.DefaultIfEmpty()
                orderby d.DeclareDate descending, d.CreateTime descending
                select new { d, b, u };

            var rows = await query.Take(n).ToListAsync();
            var decIds = rows.Select(x => x.d.Id).ToList();
            var firstSorByDec = await _db.CustomsDeclarationItems.AsNoTracking()
                .Where(i => decIds.Contains(i.DeclarationId))
                .GroupBy(i => i.DeclarationId)
                .Select(g => new { DeclarationId = g.Key, SorId = g.OrderBy(i => i.LineNo).Select(i => i.StockOutRequestId).FirstOrDefault() })
                .ToDictionaryAsync(x => x.DeclarationId, x => x.SorId);
            var sorIds = firstSorByDec.Values
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var sorCodeById = sorIds.Count == 0
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : await _db.StockOutRequests.AsNoTracking()
                    .Where(r => sorIds.Contains(r.Id))
                    .ToDictionaryAsync(
                        r => r.Id.Trim(),
                        r => r.RequestCode?.Trim() ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase);
            var list = rows.Select(x => new CustomsDeclarationListItemDto
            {
                Id = x.d.Id,
                DeclarationCode = x.d.DeclarationCode,
                PackingId = x.d.PackingId,
                StockOutRequestId = firstSorByDec.TryGetValue(x.d.Id, out var sor) ? sor : null,
                StockOutRequestCode = firstSorByDec.TryGetValue(x.d.Id, out var sorId)
                    && !string.IsNullOrWhiteSpace(sorId)
                    && sorCodeById.TryGetValue(sorId.Trim(), out var sorCode)
                    && !string.IsNullOrWhiteSpace(sorCode)
                        ? sorCode
                        : null,
                CustomsBrokerId = x.d.CustomsBrokerId,
                CustomsBrokerName = x.b.Cname,
                DeclarationType = x.d.DeclarationType,
                InternalStatus = x.d.InternalStatus,
                CustomsClearanceStatus = x.d.CustomsClearanceStatus,
                DeclareDate = x.d.DeclareDate,
                TotalTaxAmount = x.d.TotalTaxAmount,
                Remark = x.d.Remark,
                CreateTime = x.d.CreateTime,
                CreateByUserId = x.d.CreateByUserId,
                CreateUserDisplay = x.u != null && !string.IsNullOrWhiteSpace(x.u.UserName)
                    ? x.u.UserName.Trim()
                    : null
            }).ToList();

            return Ok(ApiResponse<List<CustomsDeclarationListItemDto>>.Ok(list, "OK"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取报关单列表失败");
            return StatusCode(500, ApiResponse<List<CustomsDeclarationListItemDto>>.Fail(ex.Message, 500));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CustomsDeclarationDetailViewDto>>> GetById(string id)
    {
        if (!await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User))
            return StatusCode(403, ApiResponse<CustomsDeclarationDetailViewDto>.Fail("当前账号无权访问报关模块", 403));

        var key = id.Trim();
        var row = await _db.CustomsDeclarations.AsNoTracking()
            .Include(x => x.Items.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == key);
        if (row == null)
            return NotFound(ApiResponse<CustomsDeclarationDetailViewDto>.Fail("报关单不存在", 404));

        var broker = await _db.CustomsBrokers.AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == row.CustomsBrokerId);
        Packing? packing = null;
        if (!string.IsNullOrWhiteSpace(row.PackingId))
            packing = await _db.Packings.AsNoTracking().FirstOrDefaultAsync(p => p.Id == row.PackingId!.Trim());

        var whIds = new[] { row.FromWarehouseId, row.ToWarehouseId }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var warehouses = whIds.Count == 0
            ? new List<CRM.Core.Models.Inventory.WarehouseInfo>()
            : await _db.Warehouses.AsNoTracking().Where(w => whIds.Contains(w.Id)).ToListAsync();
        var whById = warehouses.ToDictionary(w => w.Id.Trim(), w => w, StringComparer.OrdinalIgnoreCase);
        whById.TryGetValue(row.FromWarehouseId.Trim(), out var fromWh);
        whById.TryGetValue(row.ToWarehouseId.Trim(), out var toWh);

        var items = row.Items.Where(i => !i.IsDeleted).OrderBy(i => i.LineNo).ToList();
        var vendorIds = items.Where(i => !string.IsNullOrWhiteSpace(i.VendorId)).Select(i => i.VendorId!.Trim()).Distinct().ToList();
        var customerIds = items.Where(i => !string.IsNullOrWhiteSpace(i.CustomerId)).Select(i => i.CustomerId!.Trim()).Distinct().ToList();
        var vendors = vendorIds.Count == 0
            ? new List<CRM.Core.Models.Vendor.VendorInfo>()
            : await _db.Vendors.AsNoTracking().Where(v => vendorIds.Contains(v.Id)).ToListAsync();
        var customers = customerIds.Count == 0
            ? new List<CRM.Core.Models.Customer.CustomerInfo>()
            : await _db.Customers.AsNoTracking().Where(c => customerIds.Contains(c.Id)).ToListAsync();
        var venById = vendors.ToDictionary(v => v.Id.Trim(), v => v, StringComparer.OrdinalIgnoreCase);
        var custById = customers.ToDictionary(c => c.Id.Trim(), c => c, StringComparer.OrdinalIgnoreCase);

        var firstSor = items.FirstOrDefault()?.StockOutRequestId;
        string? firstSorCode = null;
        if (!string.IsNullOrWhiteSpace(firstSor))
        {
            var sorRow = await _db.StockOutRequests.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == firstSor.Trim());
            firstSorCode = sorRow?.RequestCode?.Trim();
        }

        string? createUserDisplay = null;
        if (!string.IsNullOrWhiteSpace(row.CreateByUserId))
        {
            var creator = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == row.CreateByUserId.Trim());
            createUserDisplay = creator?.UserName?.Trim();
        }

        var readiness = await _customsV2FlowService.GetArrivalNotifyReadinessAsync(key);
        var notifyByCdi = await _db.StockInNotifies.AsNoTracking()
            .Where(n => !n.IsDeleted && n.CustomsDeclarationItemId != null)
            .Where(n => items.Select(i => i.Id).Contains(n.CustomsDeclarationItemId!))
            .ToListAsync();
        var notifyCodeByCdi = notifyByCdi
            .GroupBy(n => n.CustomsDeclarationItemId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().NoticeCode?.Trim(), StringComparer.OrdinalIgnoreCase);

        var dto = new CustomsDeclarationDetailViewDto
        {
            Id = row.Id,
            DeclarationCode = row.DeclarationCode,
            PackingId = row.PackingId,
            PackingCode = packing?.Code,
            StockOutRequestId = string.IsNullOrWhiteSpace(firstSor) ? null : firstSor.Trim(),
            StockOutRequestCode = firstSorCode,
            CustomsBrokerId = row.CustomsBrokerId,
            CustomsBrokerName = broker?.Cname,
            CustomsBrokerCode = broker?.BrokerCode,
            DeclarationType = row.DeclarationType,
            InternalStatus = row.InternalStatus,
            CustomsClearanceStatus = row.CustomsClearanceStatus,
            DeclareDate = row.DeclareDate,
            ExchangeRate = row.ExchangeRate,
            BrokerAgencyRate = row.BrokerAgencyRate,
            TotalTaxAmount = row.TotalTaxAmount,
            FeesCalculatedAt = row.FeesCalculatedAt,
            FeesLocked = row.FeesLocked,
            FromWarehouseId = row.FromWarehouseId,
            ToWarehouseId = row.ToWarehouseId,
            FromWarehouseCode = fromWh?.WarehouseCode,
            ToWarehouseCode = toWh?.WarehouseCode,
            FromWarehouseName = fromWh?.WarehouseName,
            ToWarehouseName = toWh?.WarehouseName,
            Remark = row.Remark,
            CreateTime = row.CreateTime,
            CreateByUserId = row.CreateByUserId,
            CreateUserDisplay = createUserDisplay,
            CanCreateArrivalNotifies = readiness.CanCreate,
            PendingArrivalNotifyCount = readiness.PendingCount,
            ExistingArrivalNotifyCount = readiness.ExistingCount,
            ExistingArrivalNotifyCodes = readiness.ExistingNoticeCodes.ToList(),
            ArrivalNotifyBlockReason = readiness.BlockReason,
            Items = items.Select(i =>
            {
                string? vendorName = null;
                if (!string.IsNullOrWhiteSpace(i.VendorId) && venById.TryGetValue(i.VendorId.Trim(), out var ven))
                {
                    vendorName = !string.IsNullOrWhiteSpace(ven.OfficialName) ? ven.OfficialName.Trim()
                        : !string.IsNullOrWhiteSpace(ven.NickName) ? ven.NickName.Trim()
                        : ven.Code?.Trim();
                }

                string? customerName = null;
                if (!string.IsNullOrWhiteSpace(i.CustomerId) && custById.TryGetValue(i.CustomerId.Trim(), out var cust))
                {
                    customerName = !string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.OfficialName.Trim()
                        : !string.IsNullOrWhiteSpace(cust.NickName) ? cust.NickName.Trim()
                        : cust.CustomerCode?.Trim();
                }

                return new CustomsDeclarationDetailItemViewDto
                {
                    Id = i.Id,
                    LineNo = i.LineNo,
                    HsCode = i.HsCode,
                    PurchasePn = i.PurchasePn,
                    PurchaseBrand = i.PurchaseBrand,
                    DeclareQty = i.DeclareQty,
                    DeclareUnitPrice = i.DeclareUnitPrice,
                    OriginalPurchasePrice = i.OriginalPurchasePrice,
                    PurchaseCostParamId = i.PurchaseCostParamId,
                    PurchaseRatio = i.PurchaseRatio,
                    PurchaseCurrency = i.PurchaseCurrency,
                    DutyRate = i.DutyRate,
                    VatRate = i.VatRate,
                    CostUsd = i.CostUsd,
                    DutyAmount = i.DutyAmount,
                    VatAmount = i.VatAmount,
                    CustomsPaymentGoods = i.CustomsPaymentGoods,
                    CustomsAgencyFee = i.CustomsAgencyFee,
                    OtherFee = i.OtherFee,
                    InspectionFee = i.InspectionFee,
                    TotalValueTax = i.TotalValueTax,
                    TaxIncludedUnitPrice = i.TaxIncludedUnitPrice,
                    SellOrderItemCode = i.SellOrderItemCode,
                    CustomerId = i.CustomerId,
                    CustomerName = customerName,
                    VendorId = i.VendorId,
                    VendorName = vendorName,
                    StockOutRequestId = i.StockOutRequestId,
                    ArrivalNotifyCode = notifyCodeByCdi.TryGetValue(i.Id.Trim(), out var nc) ? nc : null
                };
            }).ToList()
        };

        var mask511 = await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User);
        var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User);
        if (mask511)
        {
            dto.ExchangeRate = 0m;
            dto.BrokerAgencyRate = 0m;
            dto.TotalTaxAmount = 0m;
            dto.FeesCalculatedAt = null;
            foreach (var it in dto.Items)
            {
                it.VendorId = null;
                it.VendorName = null;
                it.DeclareUnitPrice = 0m;
                it.OriginalPurchasePrice = 0m;
                it.PurchaseCostParamId = null;
                it.PurchaseRatio = 0m;
                it.PurchaseCurrency = null;
                it.DutyRate = 0m;
                it.VatRate = 0m;
                it.CostUsd = 0m;
                it.DutyAmount = 0m;
                it.VatAmount = 0m;
                it.CustomsPaymentGoods = 0m;
                it.CustomsAgencyFee = 0m;
                it.OtherFee = 0m;
                it.InspectionFee = 0m;
                it.TotalValueTax = 0m;
                it.TaxIncludedUnitPrice = 0m;
            }
        }

        if (mask521)
        {
            foreach (var it in dto.Items)
            {
                it.CustomerId = null;
                it.CustomerName = null;
                it.SellOrderItemCode = null;
            }
        }

        return Ok(ApiResponse<CustomsDeclarationDetailViewDto>.Ok(dto, "OK"));
    }

    [HttpGet("{id}/business-records")]
    public async Task<ActionResult<ApiResponse<CustomsDeclarationBusinessRecordsDto>>> GetBusinessRecords(string id)
    {
        if (!await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User))
            return StatusCode(403, ApiResponse<CustomsDeclarationBusinessRecordsDto>.Fail("当前账号无权访问报关模块", 403));

        var key = id.Trim();
        var exists = await _db.CustomsDeclarations.AsNoTracking()
            .AnyAsync(x => x.Id == key && !x.IsDeleted);
        if (!exists)
            return NotFound(ApiResponse<CustomsDeclarationBusinessRecordsDto>.Fail("报关单不存在", 404));

        var dto = await _businessRecordsQuery.LoadAsync(key);
        if (dto == null)
            return NotFound(ApiResponse<CustomsDeclarationBusinessRecordsDto>.Fail("报关单不存在", 404));

        return Ok(ApiResponse<CustomsDeclarationBusinessRecordsDto>.Ok(dto, "OK"));
    }

    [HttpGet("by-stock-out-request/{stockOutRequestId}")]
    public async Task<ActionResult<ApiResponse<CustomsDeclaration>>> GetByStockOutRequest(string stockOutRequestId)
    {
        if (!await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User))
            return StatusCode(403, ApiResponse<CustomsDeclaration>.Fail("当前账号无权访问报关模块", 403));

        var key = stockOutRequestId.Trim();
        var item = await _db.CustomsDeclarationItems.AsNoTracking()
            .Where(i => i.StockOutRequestId == key)
            .OrderBy(i => i.LineNo)
            .Select(i => i.DeclarationId)
            .FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(item))
            return NotFound(ApiResponse<CustomsDeclaration>.Fail("未找到对应报关单", 404));
        var row = await _db.CustomsDeclarations
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == item);
        if (row == null)
            return NotFound(ApiResponse<CustomsDeclaration>.Fail("未找到对应报关单", 404));
        return Ok(ApiResponse<CustomsDeclaration>.Ok(row, "OK"));
    }

    public class SetClearanceStatusRequest
    {
        public short CustomsClearanceStatus { get; set; }
    }

    public class PatchCustomsDeclarationHeaderRequest
    {
        public string? ToWarehouseId { get; set; }
        public string? Remark { get; set; }
        public decimal? ExchangeRate { get; set; }
        public string? CustomsBrokerId { get; set; }
    }

    [HttpPost("{id}/recalculate-fees")]
    public async Task<ActionResult<ApiResponse<RecalculateCustomsDeclarationFeesResultDto>>> RecalculateFees(string id)
    {
        try
        {
            if (!await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<RecalculateCustomsDeclarationFeesResultDto>.Fail("当前账号无权访问报关模块", 403));

            if (!await LogisticsDataAccessHttp.CanWriteAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<RecalculateCustomsDeclarationFeesResultDto>.Fail("当前账号物流数据为只读或禁止", 403));

            var uid = User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value;
            var result = await _customsV2FlowService.RecalculateDeclarationFeesAsync(id, uid);
            return Ok(ApiResponse<RecalculateCustomsDeclarationFeesResultDto>.Ok(result, "试算成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<RecalculateCustomsDeclarationFeesResultDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "报关费用试算失败 {Id}", id);
            return StatusCode(500, ApiResponse<RecalculateCustomsDeclarationFeesResultDto>.Fail(ex.Message, 500));
        }
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> PatchHeader(string id, [FromBody] PatchCustomsDeclarationHeaderRequest body)
    {
        try
        {
            if (!await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<object>.Fail("当前账号无权访问报关模块", 403));

            if (!await LogisticsDataAccessHttp.CanWriteAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<object>.Fail("当前账号物流数据为只读或禁止", 403));
            var uid = User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value;
            await _customsV2FlowService.UpdateDeclarationHeaderAsync(
                id, body?.ToWarehouseId, body?.Remark, uid, body?.ExchangeRate, body?.CustomsBrokerId);
            return Ok(ApiResponse<object>.Ok(null, "已更新报关单"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新报关单失败");
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
        }
    }

    [HttpPatch("{id}/customs-clearance-status")]
    public async Task<ActionResult<ApiResponse<object>>> SetClearanceStatus(string id, [FromBody] SetClearanceStatusRequest body)
    {
        try
        {
            if (!await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<object>.Fail("当前账号无权访问报关模块", 403));

            if (!await LogisticsDataAccessHttp.CanWriteAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<object>.Fail("当前账号物流数据为只读或禁止", 403));
            var uid = User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value;
            await _service.SetCustomsClearanceStatusAsync(id, body.CustomsClearanceStatus, uid);
            return Ok(ApiResponse<object>.Ok(null, "已更新海关状态"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新海关状态失败");
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
        }
    }

    [HttpPost("{id}/create-arrival-notifies")]
    public async Task<ActionResult<ApiResponse<CreateCustomsArrivalNotifiesResultDto>>> CreateArrivalNotifies(string id)
    {
        try
        {
            if (!await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<CreateCustomsArrivalNotifiesResultDto>.Fail("当前账号无权访问报关模块", 403));

            if (!await LogisticsDataAccessHttp.CanWriteAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<CreateCustomsArrivalNotifiesResultDto>.Fail("当前账号物流数据为只读或禁止", 403));

            var uid = User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value;
            var result = await _customsV2FlowService.CreateCustomsArrivalNotifiesAsync(id, uid);
            return Ok(ApiResponse<CreateCustomsArrivalNotifiesResultDto>.Ok(result, $"已生成 {result.CreatedCount} 条报关到货通知"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CreateCustomsArrivalNotifiesResultDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成报关到货通知失败");
            return StatusCode(500, ApiResponse<CreateCustomsArrivalNotifiesResultDto>.Fail(ex.Message, 500));
        }
    }

    [HttpPost("{id}/complete")]
    public Task<ActionResult<ApiResponse<object>>> Complete(string id)
    {
        _ = id;
        return Task.FromResult<ActionResult<ApiResponse<object>>>(
            StatusCode(410, ApiResponse<object>.Fail(
                "报关 V2 已废弃「报关完成+移库一步过账」，请使用报关出库/入库流程。",
                410)));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        try
        {
            if (!await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<object>.Fail("当前账号无权访问报关模块", 403));

            if (!await LogisticsDataAccessHttp.CanWriteAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<object>.Fail("当前账号物流数据为只读或禁止", 403));
            var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _service.DeleteDeclarationAsync(id, actorId);
            return Ok(ApiResponse<object>.Ok(null, "删除报关单成功"));
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("不存在", StringComparison.Ordinal))
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除报关单失败");
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
        }
    }

    [HttpPost("{id}/force-delete")]
    public async Task<ActionResult<ApiResponse<object>>> ForceDelete(string id, [FromBody] ForceDeleteCustomsDeclarationRequest? body)
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
            await _service.ForceDeleteDeclarationAsync(
                id,
                body.ConfirmBillCode.Trim(),
                userId.Trim(),
                string.IsNullOrWhiteSpace(userName) ? null : userName.Trim());

            return Ok(ApiResponse<object>.Ok(null, "强制删除报关单成功"));
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
            _logger.LogError(ex, "强制删除报关单失败");
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
        }
    }
}
