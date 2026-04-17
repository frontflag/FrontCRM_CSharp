using CRM.API.Models.DTOs;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
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
    private readonly ILogger<CustomsDeclarationItemsController> _logger;

    public CustomsDeclarationItemsController(ApplicationDbContext db, ILogger<CustomsDeclarationItemsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CustomsDeclarationItemListItemDto>>>> GetList(
        [FromQuery] string? declarationCode,
        [FromQuery] string? purchasePn,
        [FromQuery] string? customer,
        [FromQuery] string? salesUserId,
        [FromQuery] string? sellOrderItemCode,
        [FromQuery] string? stockOutRequestId,
        [FromQuery] int take = 500)
    {
        try
        {
            var n = Math.Clamp(take, 1, 1000);
            var decQ = (declarationCode ?? string.Empty).Trim();
            var pnQ = (purchasePn ?? string.Empty).Trim();
            var custQ = (customer ?? string.Empty).Trim();
            var suQ = (salesUserId ?? string.Empty).Trim();
            var soLineQ = (sellOrderItemCode ?? string.Empty).Trim();
            var sorQ = (stockOutRequestId ?? string.Empty).Trim();

            var iq = _db.CustomsDeclarationItems.AsNoTracking();
            if (!string.IsNullOrEmpty(pnQ))
                iq = iq.Where(i => i.PurchasePn != null && EF.Functions.ILike(i.PurchasePn, $"%{pnQ}%"));
            if (!string.IsNullOrEmpty(soLineQ))
                iq = iq.Where(i => i.SellOrderItemCode != null && EF.Functions.ILike(i.SellOrderItemCode, $"%{soLineQ}%"));
            if (!string.IsNullOrEmpty(sorQ))
                iq = iq.Where(i => i.StockOutRequestId == sorQ);
            if (!string.IsNullOrEmpty(suQ))
                iq = iq.Where(i => i.SalesUserId == suQ);

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
            var list = rows.Select(x => new CustomsDeclarationItemListItemDto
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
                SalesUserName = x.u != null
                    ? (string.IsNullOrWhiteSpace(x.u.RealName) ? x.u.UserName : x.u.RealName)
                    : null,
                SellOrderItemCode = x.i.SellOrderItemCode,
                PurchasePn = x.i.PurchasePn,
                PurchaseBrand = x.i.PurchaseBrand,
                DeclareQty = x.i.DeclareQty,
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
