using CRM.API.Models.DTOs;
using CRM.API.Authorization;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [RequirePermission("draft.read")]
    public class DraftsController : ControllerBase
    {
        private const string PurchaserBuyerRoleCode = "purchase_buyer";
        private const string SalesOperatorRoleCode = "sales_operator";
        /// <summary>与 sys_department / RbacService 一致：主部门 IdentityType=1 表示销售。</summary>
        private const short SalesDepartmentIdentityType = 1;

        private readonly IDraftService _draftService;
        private readonly IRbacService _rbacService;
        private readonly ILogger<DraftsController> _logger;

        public DraftsController(IDraftService draftService, IRbacService rbacService, ILogger<DraftsController> logger)
        {
            _draftService = draftService;
            _rbacService = rbacService;
            _logger = logger;
        }

        private long GetCurrentUserId()
        {
            // 占位实现：后续可从登录态中解析
            return 0;
        }

        /// <summary>
        /// 按角色/主部门身份限制草稿业务类型（列表、详情、保存、删除、转正式）：
        /// 采购员 purchase_buyer → 仅 VENDOR；主部门销售 IdentityType=1 或销售职员角色 sales_operator → 仅 CUSTOMER；系统管理员不限制。
        /// </summary>
        private async Task<IReadOnlyList<string>?> GetDraftEntityTypeWhitelistOrNullAsync()
        {
            var rbacId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(rbacId)) return null;
            var summary = await _rbacService.GetUserPermissionSummaryAsync(rbacId.Trim());
            if (summary.IsSysAdmin) return null;
            if (summary.RoleCodes.Any(c => string.Equals(c, PurchaserBuyerRoleCode, StringComparison.OrdinalIgnoreCase)))
                return new[] { "VENDOR" };
            var isSalesPrimaryDept = summary.IdentityType == SalesDepartmentIdentityType;
            var isSalesOperatorRole = summary.RoleCodes.Any(c => string.Equals(c, SalesOperatorRoleCode, StringComparison.OrdinalIgnoreCase));
            if (isSalesPrimaryDept || isSalesOperatorRole)
                return new[] { "CUSTOMER" };
            return null;
        }

        [HttpPost]
        [RequirePermission("draft.write")]
        public async Task<ActionResult<ApiResponse<DraftDto>>> SaveDraft([FromBody] SaveDraftRequest request)
        {
            try
            {
                var allowed = await GetDraftEntityTypeWhitelistOrNullAsync();
                var draft = await _draftService.SaveDraftAsync(GetCurrentUserId(), request, allowed);
                return Ok(ApiResponse<DraftDto>.Ok(draft, "保存草稿成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<DraftDto>.Fail(ex.Message, 400));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<DraftDto>.Fail(ex.Message, 403));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存草稿失败");
                return StatusCode(500, ApiResponse<DraftDto>.Fail(ex.Message, 500));
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<DraftDto>>>> GetDrafts(
            [FromQuery] string? entityType = null,
            [FromQuery] short? status = null,
            [FromQuery] string? keyword = null)
        {
            try
            {
                var allowed = await GetDraftEntityTypeWhitelistOrNullAsync();
                var items = await _draftService.GetDraftsAsync(GetCurrentUserId(), new GetDraftsRequest
                {
                    EntityType = entityType,
                    Status = status,
                    Keyword = keyword,
                    AllowedEntityTypes = allowed
                });
                return Ok(ApiResponse<IReadOnlyList<DraftDto>>.Ok(items, "获取草稿列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取草稿列表失败");
                return StatusCode(500, ApiResponse<IReadOnlyList<DraftDto>>.Fail(ex.Message, 500));
            }
        }

        [HttpGet("{draftId}")]
        public async Task<ActionResult<ApiResponse<DraftDto>>> GetDraftById(string draftId)
        {
            try
            {
                var allowed = await GetDraftEntityTypeWhitelistOrNullAsync();
                var draft = await _draftService.GetDraftByIdAsync(GetCurrentUserId(), draftId, allowed);
                if (draft == null)
                    return NotFound(ApiResponse<DraftDto>.Fail("草稿不存在", 404));

                return Ok(ApiResponse<DraftDto>.Ok(draft, "获取草稿详情成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取草稿详情失败: {DraftId}", draftId);
                return StatusCode(500, ApiResponse<DraftDto>.Fail(ex.Message, 500));
            }
        }

        [HttpDelete("{draftId}")]
        [RequirePermission("draft.write")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteDraft(string draftId)
        {
            try
            {
                var allowed = await GetDraftEntityTypeWhitelistOrNullAsync();
                await _draftService.DeleteDraftAsync(GetCurrentUserId(), draftId, allowed);
                return Ok(ApiResponse<object>.Ok(null, "删除草稿成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<object>.Fail(ex.Message, 403));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除草稿失败: {DraftId}", draftId);
                return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
            }
        }

        [HttpPost("{draftId}/convert")]
        [RequirePermission("draft.write")]
        public async Task<ActionResult<ApiResponse<DraftConvertResultDto>>> ConvertDraft(string draftId)
        {
            try
            {
                var rbacUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var allowed = await GetDraftEntityTypeWhitelistOrNullAsync();
                var result = await _draftService.ConvertDraftAsync(GetCurrentUserId(), draftId, rbacUserId, allowed);
                return Ok(ApiResponse<DraftConvertResultDto>.Ok(result, "草稿转正式成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<DraftConvertResultDto>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse<DraftConvertResultDto>.Fail(ex.Message, 409));
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ApiResponse<DraftConvertResultDto>.Fail(ex.Message, 400));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<DraftConvertResultDto>.Fail(ex.Message, 404));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<DraftConvertResultDto>.Fail(ex.Message, 403));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "草稿转正式失败: {DraftId}", draftId);
                return StatusCode(500, ApiResponse<DraftConvertResultDto>.Fail(ex.Message, 500));
            }
        }
    }
}
