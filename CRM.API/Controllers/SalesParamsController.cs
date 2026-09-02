using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/sales-params")]
public class SalesParamsController : ControllerBase
{
    private readonly ISalesParamsService _service;
    private readonly ILogger<SalesParamsController> _logger;

    public SalesParamsController(ISalesParamsService service, ILogger<SalesParamsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("allow-refresh-completed-biz-nodes")]
    [RequirePermission("system.params.sales.refresh-customer.read")]
    public async Task<ActionResult<ApiResponse<SalesParamsAllowRefreshCompletedBizNodesDto>>> GetAllowRefreshCompletedBizNodes(
        CancellationToken ct)
    {
        try
        {
            var allow = await _service.GetAllowRefreshCompletedBizNodesAsync(ct);
            return Ok(ApiResponse<SalesParamsAllowRefreshCompletedBizNodesDto>.Ok(
                new SalesParamsAllowRefreshCompletedBizNodesDto { Allow = allow },
                "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取「允许刷新已完成业务节点」失败");
            return StatusCode(500, ApiResponse<SalesParamsAllowRefreshCompletedBizNodesDto>.Fail("读取失败", 500));
        }
    }

    [HttpPut("allow-refresh-completed-biz-nodes")]
    [RequirePermission("system.params.sales.refresh-customer.write")]
    public async Task<ActionResult<ApiResponse<SalesParamsAllowRefreshCompletedBizNodesDto>>> SetAllowRefreshCompletedBizNodes(
        [FromBody] SetSalesParamsAllowRefreshCompletedBizNodesRequest? body,
        CancellationToken ct)
    {
        if (body == null)
            return BadRequest(ApiResponse<SalesParamsAllowRefreshCompletedBizNodesDto>.Fail("请求体为空", 400));

        try
        {
            await _service.SetAllowRefreshCompletedBizNodesAsync(body.Allow, ct);
            return Ok(ApiResponse<SalesParamsAllowRefreshCompletedBizNodesDto>.Ok(
                new SalesParamsAllowRefreshCompletedBizNodesDto { Allow = body.Allow },
                "已保存"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存「允许刷新已完成业务节点」失败");
            return StatusCode(500, ApiResponse<SalesParamsAllowRefreshCompletedBizNodesDto>.Fail("保存失败", 500));
        }
    }

    [HttpGet("refresh-completed-facets")]
    [RequirePermission("system.params.sales.refresh-customer.read")]
    public async Task<ActionResult<ApiResponse<SalesParamsRefreshCompletedFacetsDto>>> GetRefreshCompletedFacets(
        CancellationToken ct)
    {
        try
        {
            var facets = await _service.GetRefreshCompletedFacetsAsync(ct);
            return Ok(ApiResponse<SalesParamsRefreshCompletedFacetsDto>.Ok(MapFacets(facets), "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取销售分面刷新参数失败");
            return StatusCode(500, ApiResponse<SalesParamsRefreshCompletedFacetsDto>.Fail("读取失败", 500));
        }
    }

    [HttpPut("refresh-completed-facets")]
    [RequirePermission("system.params.sales.refresh-customer.write")]
    public async Task<ActionResult<ApiResponse<SalesParamsRefreshCompletedFacetsDto>>> SetRefreshCompletedFacets(
        [FromBody] SetSalesParamsRefreshCompletedFacetsRequest? body,
        CancellationToken ct)
    {
        if (body == null)
            return BadRequest(ApiResponse<SalesParamsRefreshCompletedFacetsDto>.Fail("请求体为空", 400));

        try
        {
            var facets = new SalesRefreshCompletedFacets
            {
                Customer = body.Customer,
                Pn = body.Pn,
                Brand = body.Brand,
                Qty = body.Qty,
                Price = body.Price
            };
            await _service.SetRefreshCompletedFacetsAsync(facets, ct);
            var saved = await _service.GetRefreshCompletedFacetsAsync(ct);
            return Ok(ApiResponse<SalesParamsRefreshCompletedFacetsDto>.Ok(MapFacets(saved), "已保存"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存销售分面刷新参数失败");
            return StatusCode(500, ApiResponse<SalesParamsRefreshCompletedFacetsDto>.Fail("保存失败", 500));
        }
    }

    private static SalesParamsRefreshCompletedFacetsDto MapFacets(SalesRefreshCompletedFacets facets) =>
        new()
        {
            Customer = facets.Customer,
            Pn = facets.Pn,
            Brand = facets.Brand,
            Qty = facets.Qty,
            Price = facets.Price
        };
}
