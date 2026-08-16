using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/customs-declaration-items")]
public class CustomsDeclarationItemsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICustomsV2FlowService _customsV2FlowService;
    private readonly IRbacService _rbacService;
    private readonly ILogger<CustomsDeclarationItemsController> _logger;

    public CustomsDeclarationItemsController(
        ApplicationDbContext db,
        ICustomsV2FlowService customsV2FlowService,
        IRbacService rbacService,
        ILogger<CustomsDeclarationItemsController> logger)
    {
        _db = db;
        _customsV2FlowService = customsV2FlowService;
        _rbacService = rbacService;
        _logger = logger;
    }

    public class PatchCustomsDeclarationItemRequest
    {
        public string? HsCode { get; set; }
        public int? DeclareQty { get; set; }
        public decimal? DeclareUnitPrice { get; set; }
        public decimal? DutyRate { get; set; }
        public decimal? VatRate { get; set; }
        public decimal? OtherFee { get; set; }
        public decimal? InspectionFee { get; set; }
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> PatchItem(string id, [FromBody] PatchCustomsDeclarationItemRequest body)
    {
        try
        {
            if (!await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<object>.Fail("当前账号无权访问报关模块", 403));

            if (!await LogisticsDataAccessHttp.CanWriteAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<object>.Fail("当前账号物流数据为只读或禁止", 403));
            var uid = User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value;
            await _customsV2FlowService.UpdateDeclarationItemAsync(id, new CustomsDeclarationItemPatch
            {
                HsCode = body?.HsCode,
                DeclareQty = body?.DeclareQty,
                DeclareUnitPrice = body?.DeclareUnitPrice,
                DutyRate = body?.DutyRate,
                VatRate = body?.VatRate,
                OtherFee = body?.OtherFee,
                InspectionFee = body?.InspectionFee
            }, uid);
            return Ok(ApiResponse<object>.Ok(null, "已更新报关明细"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新报关明细失败");
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CustomsDeclarationItemListItemDto>>>> GetList(
        [FromQuery] string? declarationCode,
        [FromQuery] string? purchasePn,
        [FromQuery] string? customer,
        [FromQuery] string? salesUserId,
        [FromQuery] string? sellOrderItemCode,
        [FromQuery] string? stockOutRequestId,
        [FromQuery] string? purchaseOrderItemCode,
        [FromQuery] int take = 500)
    {
        try
        {
            if (!await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<List<CustomsDeclarationItemListItemDto>>.Fail("当前账号无权访问报关模块", 403));

            var n = Math.Clamp(take, 1, 1000);
            var decQ = (declarationCode ?? string.Empty).Trim();
            var pnQ = (purchasePn ?? string.Empty).Trim();
            var custQ = (customer ?? string.Empty).Trim();
            var suQ = (salesUserId ?? string.Empty).Trim();
            var soLineQ = (sellOrderItemCode ?? string.Empty).Trim();
            var sorQ = (stockOutRequestId ?? string.Empty).Trim();
            var poItemQ = (purchaseOrderItemCode ?? string.Empty).Trim();
            var mask511 = await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User);

            var iq = _db.CustomsDeclarationItems.AsNoTracking();
            if (!string.IsNullOrEmpty(pnQ))
                iq = iq.Where(i => i.PurchasePn != null && EF.Functions.ILike(i.PurchasePn, $"%{pnQ}%"));
            if (!string.IsNullOrEmpty(soLineQ))
                iq = iq.Where(i => i.SellOrderItemCode != null && EF.Functions.ILike(i.SellOrderItemCode, $"%{soLineQ}%"));
            if (!string.IsNullOrEmpty(sorQ))
                iq = iq.Where(i => i.StockOutRequestId == sorQ);
            if (!string.IsNullOrEmpty(suQ))
                iq = iq.Where(i => i.SalesUserId == suQ);
            if (!mask511 && !string.IsNullOrEmpty(poItemQ))
            {
                var matchedLayerIds = _db.StockItems.AsNoTracking()
                    .Where(s => s.PurchaseOrderItemCode != null
                                && EF.Functions.ILike(s.PurchaseOrderItemCode, $"%{poItemQ}%"))
                    .Select(s => s.Id);
                iq = iq.Where(i => i.SourceStockItemId != null && matchedLayerIds.Contains(i.SourceStockItemId));
            }

            var query =
                from i in iq
                join d in _db.CustomsDeclarations.AsNoTracking() on i.DeclarationId equals d.Id
                join c in _db.Customers.AsNoTracking().IgnoreQueryFilters() on i.CustomerId equals c.Id into cj
                from c in cj.DefaultIfEmpty()
                join u in _db.Users.AsNoTracking() on i.SalesUserId equals u.Id into uj
                from u in uj.DefaultIfEmpty()
                where string.IsNullOrEmpty(decQ) || EF.Functions.ILike(d.DeclarationCode, $"%{decQ}%")
                where string.IsNullOrEmpty(custQ)
                      || (i.CustomerId != null && i.CustomerId == custQ)
                      || (c != null && c.OfficialName != null && EF.Functions.ILike(c.OfficialName, $"%{custQ}%"))
                orderby d.DeclareDate descending, i.LineNo, i.CreateTime descending
                select new { i, d, c, u };

            var rows = await query.Take(n).ToListAsync();
            var layerIds = rows
                .Select(x => x.i.SourceStockItemId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var layers = await _db.StockItems.AsNoTracking()
                .Where(s => layerIds.Contains(s.Id))
                .Select(s => new { s.Id, s.PurchaseOrderItemCode, s.PurchaseOrderItemId })
                .ToListAsync();
            var layerById = layers.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
            var poItemIds = layers
                .Select(s => s.PurchaseOrderItemId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var poByItemId = await _db.PurchaseOrderItems.AsNoTracking()
                .Where(p => poItemIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.PurchaseOrderId, StringComparer.OrdinalIgnoreCase);

            var list = rows.Select(x =>
            {
                string? poCode = null;
                string? poId = null;
                decimal? p0 = null;
                short? ccy = null;
                decimal? amount = null;
                var layerId = x.i.SourceStockItemId?.Trim();
                if (!mask511 && !string.IsNullOrWhiteSpace(layerId) && layerById.TryGetValue(layerId, out var layer))
                {
                    poCode = string.IsNullOrWhiteSpace(layer.PurchaseOrderItemCode)
                        ? null
                        : layer.PurchaseOrderItemCode.Trim();
                    var poItemId = layer.PurchaseOrderItemId?.Trim();
                    if (!string.IsNullOrWhiteSpace(poItemId) && poByItemId.TryGetValue(poItemId, out var hid))
                        poId = string.IsNullOrWhiteSpace(hid) ? null : hid.Trim();
                    p0 = x.i.OriginalPurchasePrice;
                    ccy = x.i.PurchaseCurrency;
                    amount = x.i.OriginalPurchasePrice * x.i.DeclareQty;
                }

                return new CustomsDeclarationItemListItemDto
                {
                    Id = x.i.Id,
                    DeclarationId = x.i.DeclarationId,
                    DeclarationCode = x.d.DeclarationCode,
                    DeclareDate = x.d.DeclareDate,
                    LineNo = x.i.LineNo,
                    StockOutRequestId = x.i.StockOutRequestId,
                    CustomerId = x.i.CustomerId,
                    CustomerName = x.c?.OfficialName,
                    SalesUserId = x.i.SalesUserId,
                    SalesUserName = x.u != null && !string.IsNullOrWhiteSpace(x.u.UserName)
                        ? x.u.UserName.Trim()
                        : null,
                    SellOrderItemCode = x.i.SellOrderItemCode,
                    PurchasePn = x.i.PurchasePn,
                    PurchaseBrand = x.i.PurchaseBrand,
                    DeclareQty = x.i.DeclareQty,
                    PurchaseOrderItemCode = poCode,
                    PurchaseOrderId = poId,
                    OriginalPurchasePrice = p0,
                    PurchaseCurrency = ccy,
                    OriginalPurchaseAmount = amount,
                    DeclareUnitPrice = x.i.DeclareUnitPrice,
                    DutyAmount = x.i.DutyAmount,
                    VatAmount = x.i.VatAmount,
                    CustomsPaymentGoods = x.i.CustomsPaymentGoods,
                    CustomsAgencyFee = x.i.CustomsAgencyFee,
                    OtherFee = x.i.OtherFee,
                    InspectionFee = x.i.InspectionFee,
                    TotalValueTax = x.i.TotalValueTax,
                    TaxIncludedUnitPrice = x.i.TaxIncludedUnitPrice,
                    CreateTime = x.i.CreateTime,
                    CreateByUserId = null,
                    CreateUserDisplay = x.i.CreateUserId.HasValue ? x.i.CreateUserId.Value.ToString() : null
                };
            }).ToList();

            return Ok(ApiResponse<List<CustomsDeclarationItemListItemDto>>.Ok(list, "OK"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取报关明细列表失败");
            return StatusCode(500, ApiResponse<List<CustomsDeclarationItemListItemDto>>.Fail(ex.Message, 500));
        }
    }
}
