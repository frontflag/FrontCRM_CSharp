using CRM.API.Models.DTOs;
using CRM.Core.Models;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Controllers;

/// <summary>移库单列表（报关迁库等），供物流专页查询。</summary>
[ApiController]
[Route("api/v1/stock-transfers")]
public class StockTransfersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<StockTransfersController> _logger;

    public StockTransfersController(ApplicationDbContext db, ILogger<StockTransfersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<StockTransferListItemDto>>>> GetList(
        [FromQuery] short? status,
        [FromQuery] DateTime? confirmedFrom,
        [FromQuery] DateTime? confirmedTo,
        [FromQuery] string? declarationCode,
        [FromQuery] string? fromWarehouseId,
        [FromQuery] string? toWarehouseId,
        [FromQuery] bool? pendingConfirm,
        [FromQuery] int take = 200)
    {
        try
        {
            var n = Math.Clamp(take, 1, 500);
            var decQ = (declarationCode ?? string.Empty).Trim();
            var fromW = (fromWarehouseId ?? string.Empty).Trim();
            var toW = (toWarehouseId ?? string.Empty).Trim();

            var tq = _db.StockTransfers.AsNoTracking();
            if (status.HasValue)
                tq = tq.Where(x => x.Status == status.Value);
            if (confirmedFrom.HasValue)
                tq = tq.Where(x => x.ConfirmedTime != null && x.ConfirmedTime >= confirmedFrom.Value);
            if (confirmedTo.HasValue)
            {
                var end = confirmedTo.Value.Date.AddDays(1);
                tq = tq.Where(x => x.ConfirmedTime != null && x.ConfirmedTime < end);
            }

            if (!string.IsNullOrEmpty(fromW))
                tq = tq.Where(x => x.FromWarehouseId == fromW);
            if (!string.IsNullOrEmpty(toW))
                tq = tq.Where(x => x.ToWarehouseId == toW);
            if (pendingConfirm == true)
                tq = tq.Where(x => x.ConfirmedTime == null);

            var query =
                from t in tq
                join d in _db.CustomsDeclarations.AsNoTracking() on t.CustomsDeclarationId equals d.Id
                join u in _db.Users.AsNoTracking() on t.CreateByUserId equals u.Id into uj
                from u in uj.DefaultIfEmpty()
                where string.IsNullOrEmpty(decQ) || EF.Functions.ILike(d.DeclarationCode, $"%{decQ}%")
                orderby t.CreateTime descending
                select new { t, d, u };

            var rows = await query.Take(n).ToListAsync();
            var whIds = rows.SelectMany(x => new[] { x.t.FromWarehouseId, x.t.ToWarehouseId }).Distinct().ToList();
            var wh = await _db.Warehouses.AsNoTracking()
                .Where(w => whIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.WarehouseName);

            var list = rows.Select(x => new StockTransferListItemDto
            {
                Id = x.t.Id,
                TransferCode = x.t.TransferCode,
                BizScene = x.t.BizScene,
                CustomsDeclarationId = x.t.CustomsDeclarationId,
                DeclarationCode = x.d.DeclarationCode,
                Status = x.t.Status,
                ConfirmedTime = x.t.ConfirmedTime,
                ConfirmedByUserId = x.t.ConfirmedByUserId,
                FromWarehouseId = x.t.FromWarehouseId,
                ToWarehouseId = x.t.ToWarehouseId,
                FromWarehouseName = wh.GetValueOrDefault(x.t.FromWarehouseId),
                ToWarehouseName = wh.GetValueOrDefault(x.t.ToWarehouseId),
                CreateTime = x.t.CreateTime,
                CreateByUserId = x.t.CreateByUserId,
                CreateUserDisplay = x.u != null
                    ? (string.IsNullOrWhiteSpace(x.u.RealName) ? x.u.UserName : x.u.RealName)
                    : null,
                IsConfirmed = x.t.ConfirmedTime != null
            }).ToList();

            return Ok(ApiResponse<List<StockTransferListItemDto>>.Ok(list, "OK"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取移库单列表失败");
            return StatusCode(500, ApiResponse<List<StockTransferListItemDto>>.Fail(ex.Message, 500));
        }
    }

    /// <summary>确认移仓：写入确认时间与操作人（<c>ConfirmedTime</c> 原为空时可执行）。</summary>
    [HttpPatch("{id}/confirm")]
    public async Task<ActionResult<ApiResponse<object>>> Confirm(string id)
    {
        try
        {
            var tid = id.Trim();
            var row = await _db.StockTransfers.FirstOrDefaultAsync(x => x.Id == tid);
            if (row == null)
                return NotFound(ApiResponse<object>.Fail("移库单不存在", 404));
            if (row.ConfirmedTime != null)
                return BadRequest(ApiResponse<object>.Fail("移库单已确认", 400));

            var uid = User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value?.Trim();
            row.ConfirmedTime = DateTime.UtcNow;
            row.ConfirmedByUserId = string.IsNullOrEmpty(uid) ? null : uid;
            row.ModifyTime = DateTime.UtcNow;
            row.ModifyByUserId = string.IsNullOrEmpty(uid) ? null : uid;
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(null, "已确认移仓"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "确认移仓失败");
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
        }
    }
}
