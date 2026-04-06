using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    /// <summary>
    /// 数据字典项维护（sys_dict_item）
    /// 双路由：部分生产网关/WAF 对路径中的 "admin" 返回 404，故增加 mgmt 前缀（与 admin 等价）。
    /// </summary>
    [ApiController]
    [Route("api/v1/dictionaries/admin")]
    [Route("api/v1/dictionaries/mgmt")]
    [Authorize]
    public class DictionariesAdminController : ControllerBase
    {
        private readonly ISysDictItemAdminService _adminService;
        private readonly ILogger<DictionariesAdminController> _logger;

        public DictionariesAdminController(ISysDictItemAdminService adminService,
            ILogger<DictionariesAdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        [HttpGet("categories")]
        [RequirePermission("rbac.manage")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> GetCategories(CancellationToken ct)
        {
            try
            {
                var list = await _adminService.GetCategoriesAsync(ct);
                return Ok(ApiResponse<IReadOnlyList<string>>.Ok(list, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取字典分类失败");
                return StatusCode(500, ApiResponse<IReadOnlyList<string>>.Fail("读取失败"));
            }
        }

        [HttpGet("items")]
        [RequirePermission("rbac.manage")]
        public async Task<ActionResult<ApiResponse<SysDictItemAdminPagedDto>>> List(
            [FromQuery] string? bizSegment,
            [FromQuery] string? category,
            [FromQuery] string? keyword,
            [FromQuery] bool? isActive,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var q = new SysDictItemAdminQuery
                {
                    BizSegment = bizSegment,
                    Category = category,
                    Keyword = keyword,
                    IsActive = isActive,
                    Page = page,
                    PageSize = pageSize
                };
                var data = await _adminService.ListAsync(q, cancellationToken);
                return Ok(ApiResponse<SysDictItemAdminPagedDto>.Ok(data, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "分页查询字典项失败");
                return StatusCode(500, ApiResponse<SysDictItemAdminPagedDto>.Fail("查询失败"));
            }
        }

        /// <summary>预取下一选项编码；同时注册 admin/next-item-code 与 items/next-item-code，避免网关仅转发 items 列表路径时 404。</summary>
        [HttpGet("next-item-code")]
        [HttpGet("items/next-item-code")]
        [RequirePermission("rbac.manage")]
        public async Task<ActionResult<ApiResponse<string>>> GetNextItemCode([FromQuery] string category,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest(ApiResponse<string>.Fail("category 必填"));
            try
            {
                var code = await _adminService.GetNextItemCodeAsync(category.Trim(), ct);
                return Ok(ApiResponse<string>.Ok(code, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "预取下一选项编码失败 category={Category}", category);
                return StatusCode(500, ApiResponse<string>.Fail("读取失败"));
            }
        }

        [HttpGet("items/{id:guid}")]
        [RequirePermission("rbac.manage")]
        public async Task<ActionResult<ApiResponse<SysDictItemAdminRowDto>>> GetById(Guid id, CancellationToken ct)
        {
            var row = await _adminService.GetByIdAsync(id.ToString(), ct);
            if (row == null) return NotFound(ApiResponse<SysDictItemAdminRowDto>.Fail("记录不存在"));
            return Ok(ApiResponse<SysDictItemAdminRowDto>.Ok(row, "ok"));
        }

        [HttpPost("items")]
        [RequirePermission("rbac.manage")]
        public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateSysDictItemDto dto,
            CancellationToken ct)
        {
            var (ok, err) = await _adminService.CreateAsync(dto, ct);
            if (!ok) return BadRequest(ApiResponse<object>.Fail(err ?? "创建失败"));
            return Ok(ApiResponse<object>.Ok(new object(), "创建成功"));
        }

        [HttpPut("items/{id:guid}")]
        [RequirePermission("rbac.manage")]
        public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] UpdateSysDictItemDto dto,
            CancellationToken ct)
        {
            var (ok, err) = await _adminService.UpdateAsync(id.ToString(), dto, ct);
            if (!ok)
            {
                if (err == "记录不存在") return NotFound(ApiResponse<object>.Fail(err));
                return BadRequest(ApiResponse<object>.Fail(err ?? "更新失败"));
            }

            return Ok(ApiResponse<object>.Ok(new object(), "更新成功"));
        }

        /// <summary>
        /// 排序保存：items/{id} 已约束为 guid，故 items/reorder 不会与更新单条冲突。
        /// 保留 admin/reorder 以兼容旧客户端；前端优先使用 items/reorder。
        /// </summary>
        [HttpPut("reorder")]
        [HttpPut("items/reorder")]
        [RequirePermission("rbac.manage")]
        public async Task<ActionResult<ApiResponse<object>>> Reorder([FromBody] ReorderSysDictItemsDto dto,
            CancellationToken ct)
        {
            try
            {
                var (ok, err) = await _adminService.ReorderAsync(dto, ct);
                if (!ok) return BadRequest(ApiResponse<object>.Fail(err ?? "调整排序失败"));
                return Ok(ApiResponse<object>.Ok(new object(), "排序已更新"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "字典项重排序失败");
                return StatusCode(500, ApiResponse<object>.Fail("调整排序失败"));
            }
        }
    }
}
