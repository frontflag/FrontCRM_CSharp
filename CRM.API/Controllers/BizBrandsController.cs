using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/biz-brands")]
[Authorize]
public class BizBrandsController : ControllerBase
{
    private readonly IBizBrandService _brandService;
    private readonly ILogger<BizBrandsController> _logger;

    public BizBrandsController(IBizBrandService brandService, ILogger<BizBrandsController> logger)
    {
        _brandService = brandService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<BizBrandPagedDto>>> List(
        [FromQuery] string? brandCName,
        [FromQuery] string? brandEName,
        [FromQuery] string? alias,
        [FromQuery] string? country,
        [FromQuery] string? remark,
        [FromQuery] short? auditStatus,
        [FromQuery] DateTime? createTimeFrom,
        [FromQuery] DateTime? createTimeTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var data = await _brandService.ListAsync(new BizBrandQuery
            {
                BrandCName = brandCName,
                BrandEName = brandEName,
                Alias = alias,
                Country = country,
                Remark = remark,
                AuditStatus = auditStatus,
                CreateTimeFrom = createTimeFrom,
                CreateTimeTo = createTimeTo,
                Page = page,
                PageSize = pageSize
            }, ct);
            return Ok(ApiResponse<BizBrandPagedDto>.Ok(data, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询品牌列表失败");
            return StatusCode(500, ApiResponse<BizBrandPagedDto>.Fail("查询失败", 500));
        }
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(
        [FromQuery] string? keyword,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var items = await _brandService.ListOptionsAsync(new BizBrandOptionsQuery
            {
                Keyword = keyword,
                PageSize = pageSize
            }, ct);
            return Ok(ApiResponse<List<BizBrandOptionDto>>.Ok(items, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询品牌下拉选项失败");
            return StatusCode(500, ApiResponse<List<BizBrandOptionDto>>.Fail("查询失败", 500));
        }
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<BizBrandRowDto>>> Get(long id, CancellationToken ct)
    {
        try
        {
            var row = await _brandService.GetByIdAsync(id, ct);
            if (row == null)
                return NotFound(ApiResponse<BizBrandRowDto>.Fail("品牌不存在", 404));
            return Ok(ApiResponse<BizBrandRowDto>.Ok(row, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取品牌失败 Id={Id}", id);
            return StatusCode(500, ApiResponse<BizBrandRowDto>.Fail("读取失败", 500));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BizBrandRowDto>>> Create(
        [FromBody] UpsertBizBrandRequest body,
        CancellationToken ct)
    {
        if (body == null)
            return BadRequest(ApiResponse<BizBrandRowDto>.Fail("请求体为空", 400));

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var row = await _brandService.CreateAsync(body, userId, ct);
            return Ok(ApiResponse<BizBrandRowDto>.Ok(row, "创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BizBrandRowDto>.Fail(ex.Message, 400));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<BizBrandRowDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建品牌失败");
            return StatusCode(500, ApiResponse<BizBrandRowDto>.Fail("创建失败", 500));
        }
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<BizBrandRowDto>>> Update(
        long id,
        [FromBody] UpsertBizBrandRequest body,
        CancellationToken ct)
    {
        if (body == null)
            return BadRequest(ApiResponse<BizBrandRowDto>.Fail("请求体为空", 400));

        try
        {
            var row = await _brandService.UpdateAsync(id, body, ct);
            return Ok(ApiResponse<BizBrandRowDto>.Ok(row, "保存成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<BizBrandRowDto>.Fail(ex.Message, 404));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BizBrandRowDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新品牌失败 Id={Id}", id);
            return StatusCode(500, ApiResponse<BizBrandRowDto>.Fail("保存失败", 500));
        }
    }

    [HttpPost("{id:long}/approve")]
    public async Task<ActionResult<ApiResponse<BizBrandRowDto>>> Approve(long id, CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var row = await _brandService.ApproveAsync(id, userId, ct);
            return Ok(ApiResponse<BizBrandRowDto>.Ok(row, "审核成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<BizBrandRowDto>.Fail(ex.Message, 404));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BizBrandRowDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "审核品牌失败 Id={Id}", id);
            return StatusCode(500, ApiResponse<BizBrandRowDto>.Fail("审核失败", 500));
        }
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken ct)
    {
        try
        {
            await _brandService.DeleteAsync(id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value, ct);
            return Ok(ApiResponse<object>.Ok(null, "删除成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除品牌失败 Id={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("删除失败", 500));
        }
    }
}
