using System.Security.Claims;

using CRM.API.Authorization;

using CRM.API.Models.DTOs;

using CRM.Core.Interfaces;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;



namespace CRM.API.Controllers;



[ApiController]

[Authorize]

[Route("api/v1/sys-relation-maps")]

public class SysRelationMapController : ControllerBase

{

    private readonly ISysRelationMapService _relationMapService;

    private readonly IRbacService _rbacService;



    public SysRelationMapController(ISysRelationMapService relationMapService, IRbacService rbacService)

    {

        _relationMapService = relationMapService;

        _rbacService = rbacService;

    }



    /// <summary>

    /// 查询某助理已配置的目标用户 Id 列表。

    /// 本人可查自己的映射；具备 rbac.manage 或系统管理员可查任意助理。

    /// </summary>

    [HttpGet("destinations")]

    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> GetDestinations(

        [FromQuery] short type,

        [FromQuery] string objSrc,

        CancellationToken cancellationToken)

    {

        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(currentUserId))

            return Unauthorized(ApiResponse<IReadOnlyList<string>>.Fail("未登录或登录态失效", 401));



        if (!await CanReadMappingsAsync(currentUserId, objSrc, cancellationToken))

            return StatusCode(403, ApiResponse<IReadOnlyList<string>>.Fail("无权查看该助理的映射配置", 403));



        try

        {

            var ids = await _relationMapService.GetMappedDestIdsAsync(type, objSrc, cancellationToken);

            return Ok(ApiResponse<IReadOnlyList<string>>.Ok(ids));

        }

        catch (ArgumentException ex)

        {

            return BadRequest(ApiResponse<IReadOnlyList<string>>.Fail(ex.Message));

        }

    }



    /// <summary>保存助理与目标用户的映射变更（用户配置页，需 rbac.manage）。</summary>

    [HttpPut("batch")]

    [RequirePermission("system.org.user-config.write")]

    public async Task<ActionResult<ApiResponse<object>>> SaveBatch(

        [FromBody] SaveSysRelationMapRequest request,

        CancellationToken cancellationToken)

    {

        if (request == null)

            return BadRequest(ApiResponse<object>.Fail("请求体不能为空"));



        try

        {

            await _relationMapService.SaveMappingsAsync(

                request.Type,

                request.ObjSrc,

                request.AddDestIds ?? [],

                request.RemoveDestIds ?? [],

                cancellationToken);

            return Ok(ApiResponse<object>.Ok(null, "保存成功"));

        }

        catch (ArgumentException ex)

        {

            return BadRequest(ApiResponse<object>.Fail(ex.Message));

        }

        catch (InvalidOperationException ex)

        {

            return BadRequest(ApiResponse<object>.Fail(ex.Message));

        }

    }



    private async Task<bool> CanReadMappingsAsync(

        string currentUserId,

        string objSrc,

        CancellationToken cancellationToken)

    {

        if (string.IsNullOrWhiteSpace(objSrc))

            return false;



        if (string.Equals(currentUserId.Trim(), objSrc.Trim(), StringComparison.OrdinalIgnoreCase))

            return true;



        var summary = await _rbacService.GetUserPermissionSummaryAsync(currentUserId);

        if (summary.IsSysAdmin)

            return true;



        return summary.PermissionCodes.Any(c =>

            string.Equals(c, "rbac.manage", StringComparison.OrdinalIgnoreCase));

    }

}


