using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

/// <summary>订单旅程日志查询（表 log_orderjourney）。</summary>
[ApiController]
[Route("api/v1/order-journey")]
[Authorize]
public class OrderJourneyController : ControllerBase
{
    private readonly IOrderJourneyLogService _service;

    public OrderJourneyController(IOrderJourneyLogService service)
    {
        _service = service;
    }

    [HttpGet("sell-order/{sellOrderId}")]
    public async Task<IActionResult> BySellOrder(string sellOrderId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sellOrderId))
            return BadRequest(new { success = false, message = "销售订单 Id 不能为空" });
        var data = await _service.GetBySellOrderIdAsync(sellOrderId.Trim(), cancellationToken);
        return Ok(new { success = true, data });
    }

    [HttpGet("purchase-order/{purchaseOrderId}")]
    public async Task<IActionResult> ByPurchaseOrder(string purchaseOrderId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(purchaseOrderId))
            return BadRequest(new { success = false, message = "采购订单 Id 不能为空" });
        var data = await _service.GetByPurchaseOrderIdAsync(purchaseOrderId.Trim(), cancellationToken);
        return Ok(new { success = true, data });
    }
}
