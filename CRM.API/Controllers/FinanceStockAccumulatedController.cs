using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Rbac;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[RequireAnyPermission("finance-accumulated.read", "finance-receipt.read")]
[ApiController]
[Route("api/v1/finance/accumulated")]
public class FinanceStockAccumulatedController : ControllerBase
{
    private readonly IFinanceStockAccumulatedQuery _query;
    private readonly IRbacService _rbacService;

    public FinanceStockAccumulatedController(
        IFinanceStockAccumulatedQuery query,
        IRbacService rbacService)
    {
        _query = query;
        _rbacService = rbacService;
    }

    [HttpGet("search-options")]
    public async Task<IActionResult> GetSearchOptions(CancellationToken cancellationToken = default)
    {
        var denied = await DenyIfFinanceForbiddenAsync();
        if (denied != null)
            return denied;

        var data = await _query.GetSearchOptionsAsync(cancellationToken);
        return Ok(ApiResponse<FinanceStockAccumulatedSearchOptionsDto>.Ok(data));
    }

    [HttpGet("stock")]
    public async Task<IActionResult> GetStockSummary(
        [FromQuery] string? year,
        CancellationToken cancellationToken = default)
    {
        var denied = await DenyIfFinanceForbiddenAsync();
        if (denied != null)
            return denied;

        if (!FinanceAccumulatedMonthBoundary.TryParseYear(year, out var y))
            return BadRequest(ApiResponse<object>.Fail("请选择年份！", 400));

        try
        {
            var maskAmounts = await ResolveMaskAmountsAsync();
            var data = await _query.GetStockSummaryAsync(y, maskAmounts, cancellationToken);
            return Ok(ApiResponse<FinanceStockAccumulatedListDto>.Ok(data));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail($"加载库存滚存失败：{ex.Message}", 500));
        }
    }

    [HttpGet("stock-items")]
    public async Task<IActionResult> GetStockItems(
        [FromQuery] FinanceStockAccumulatedItemQueryRequest request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var denied = await DenyIfFinanceForbiddenAsync();
        if (denied != null)
            return denied;

        if (string.IsNullOrWhiteSpace(request.Month))
            return BadRequest(ApiResponse<object>.Fail("请选择月份！", 400));

        try
        {
            var maskAmounts = await ResolveMaskAmountsAsync();
            var result = await _query.GetStockItemPageAsync(request, page, pageSize, maskAmounts, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new
            {
                maskAmounts,
                items = result.Items,
                total = result.TotalCount,
                page = result.PageIndex,
                pageSize = result.PageSize
            }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    [HttpGet("vendors")]
    public async Task<IActionResult> GetVendors(
        [FromQuery] FinanceVendorAccumulatedQueryRequest request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var denied = await DenyIfFinanceForbiddenAsync();
        if (denied != null)
            return denied;

        if (string.IsNullOrWhiteSpace(request.Month))
            return BadRequest(ApiResponse<object>.Fail("请选择月份！", 400));

        try
        {
            var maskAmounts = await ResolveMaskAmountsAsync();
            var result = await _query.GetVendorPageAsync(request, page, pageSize, maskAmounts, cancellationToken);
            return Ok(ApiResponse<FinanceVendorAccumulatedListDto>.Ok(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail($"加载供应商滚存失败：{ex.Message}", 500));
        }
    }

    [HttpGet("vendor-items")]
    public async Task<IActionResult> GetVendorItems(
        [FromQuery] FinanceVendorAccumulatedItemQueryRequest request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var denied = await DenyIfFinanceForbiddenAsync();
        if (denied != null)
            return denied;

        if (string.IsNullOrWhiteSpace(request.Month))
            return BadRequest(ApiResponse<object>.Fail("请选择月份！", 400));

        if (request.VendorId == null)
            return BadRequest(ApiResponse<object>.Fail("请选择供应商！", 400));

        try
        {
            var maskAmounts = await ResolveMaskAmountsAsync();
            var result = await _query.GetVendorItemPageAsync(request, page, pageSize, maskAmounts, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new
            {
                maskAmounts,
                items = result.Items,
                total = result.TotalCount,
                page = result.PageIndex,
                pageSize = result.PageSize
            }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] FinanceCustomerAccumulatedQueryRequest request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var denied = await DenyIfFinanceForbiddenAsync();
        if (denied != null)
            return denied;

        if (string.IsNullOrWhiteSpace(request.Month))
            return BadRequest(ApiResponse<object>.Fail("请选择月份！", 400));

        try
        {
            var maskAmounts = await ResolveMaskAmountsAsync();
            var result = await _query.GetCustomerPageAsync(request, page, pageSize, maskAmounts, cancellationToken);
            return Ok(ApiResponse<FinanceCustomerAccumulatedListDto>.Ok(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail($"加载客户滚存失败：{ex.Message}", 500));
        }
    }

    [HttpGet("customer-items")]
    public async Task<IActionResult> GetCustomerItems(
        [FromQuery] FinanceCustomerAccumulatedItemQueryRequest request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var denied = await DenyIfFinanceForbiddenAsync();
        if (denied != null)
            return denied;

        if (string.IsNullOrWhiteSpace(request.Month))
            return BadRequest(ApiResponse<object>.Fail("请选择月份！", 400));

        if (request.CustomerId == null)
            return BadRequest(ApiResponse<object>.Fail("请选择客户！", 400));

        try
        {
            var maskAmounts = await ResolveMaskAmountsAsync();
            var result = await _query.GetCustomerItemPageAsync(request, page, pageSize, maskAmounts, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new
            {
                maskAmounts,
                items = result.Items,
                total = result.TotalCount,
                page = result.PageIndex,
                pageSize = result.PageSize
            }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    private async Task<IActionResult?> DenyIfFinanceForbiddenAsync()
    {
        if (!await FinanceDataAccessHttp.CanViewFinanceMenusAsync(_rbacService, User))
            return Forbidden("当前账号无财务数据范围，无法访问库存滚存");

        return null;
    }

    private async Task<bool> ResolveMaskAmountsAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return true;

        var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
        return ShouldMaskAmounts(summary);
    }

    private static bool ShouldMaskAmounts(UserPermissionSummaryDto summary)
    {
        if (summary.IsSysAdmin)
            return false;
        if (PurchaseSensitiveFieldMask511.ShouldMask(summary))
            return true;
        if (SaleSensitiveFieldMask521.ShouldMask(summary))
            return true;
        if (summary.PermissionCodes == null)
            return true;

        return !summary.PermissionCodes.Contains("purchase.amount.read", StringComparer.OrdinalIgnoreCase)
               && !summary.PermissionCodes.Contains("sales.amount.read", StringComparer.OrdinalIgnoreCase)
               && !summary.PermissionCodes.Contains("finance-payment.read", StringComparer.OrdinalIgnoreCase)
               && !summary.PermissionCodes.Contains("finance-receipt.read", StringComparer.OrdinalIgnoreCase);
    }

    private IActionResult Forbidden(string message) =>
        new ObjectResult(ApiResponse<object>.Fail(message, 403)) { StatusCode = 403 };
}
