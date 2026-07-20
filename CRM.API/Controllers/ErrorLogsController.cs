using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/error-logs")]
[Authorize]
public class ErrorLogsController : ControllerBase
{
    private readonly IErrorLogService _errorLogService;

    public ErrorLogsController(IErrorLogService errorLogService)
    {
        _errorLogService = errorLogService;
    }

    public class ErrorLogListItemDto
    {
        public long Id { get; set; }
        public string ErrorId { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string? OperationType { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string? RequestPath { get; set; }
        public string? UserName { get; set; }
        public bool IsResolved { get; set; }
        /// <summary>open | resolved | ignored</summary>
        public string Status { get; set; } = SysErrorLogFilterStatus.Open;
        public string? ResolveRemark { get; set; }
    }

    public sealed class ErrorLogDetailDto : ErrorLogListItemDto
    {
        public string? ErrorDetail { get; set; }
        public string? DocumentNo { get; set; }
        public string? DataId { get; set; }
        public string? UserId { get; set; }
        public string? RequestBody { get; set; }
    }

    public sealed class ErrorLogPagedDto
    {
        public IReadOnlyList<ErrorLogListItemDto> Items { get; set; } = Array.Empty<ErrorLogListItemDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public sealed class ResolveErrorLogRequest
    {
        public string? Remark { get; set; }
    }

    [HttpGet]
    [RequirePermission(SysErrorLogPermissionCodes.Read)]
    public async Task<ActionResult<ApiResponse<ErrorLogPagedDto>>> List(
        [FromQuery] string? moduleName,
        [FromQuery] string? keyword,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _errorLogService.GetPagedAsync(
            page, pageSize, moduleName, keyword, startDate, endDate, status);

        return Ok(ApiResponse<ErrorLogPagedDto>.Ok(new ErrorLogPagedDto
        {
            Page = page < 1 ? 1 : page,
            PageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100),
            Total = total,
            Items = items.Select(x => new ErrorLogListItemDto
            {
                Id = x.Id,
                ErrorId = SysErrorLogIdFormat.Format(x.Id),
                OccurredAt = x.OccurredAt,
                ModuleName = x.ModuleName,
                OperationType = x.OperationType,
                ErrorMessage = x.ErrorMessage,
                RequestPath = x.RequestPath,
                UserName = x.UserName,
                IsResolved = x.IsResolved,
                Status = SysErrorLogFilterStatus.Resolve(x),
                ResolveRemark = x.ResolveRemark
            }).ToList()
        }));
    }

    [HttpGet("{id:long}")]
    [RequirePermission(SysErrorLogPermissionCodes.Read)]
    public async Task<ActionResult<ApiResponse<ErrorLogDetailDto>>> Detail(long id)
    {
        var x = await _errorLogService.GetByIdAsync(id);
        if (x == null)
            return NotFound(ApiResponse<ErrorLogDetailDto>.Fail("错误日志不存在", 404));

        return Ok(ApiResponse<ErrorLogDetailDto>.Ok(new ErrorLogDetailDto
        {
            Id = x.Id,
            ErrorId = SysErrorLogIdFormat.Format(x.Id),
            OccurredAt = x.OccurredAt,
            ModuleName = x.ModuleName,
            OperationType = x.OperationType,
            ErrorMessage = x.ErrorMessage,
            ErrorDetail = x.ErrorDetail,
            DocumentNo = x.DocumentNo,
            DataId = x.DataId,
            UserId = x.UserId,
            UserName = x.UserName,
            RequestPath = x.RequestPath,
            RequestBody = x.RequestBody,
            IsResolved = x.IsResolved,
            Status = SysErrorLogFilterStatus.Resolve(x),
            ResolveRemark = x.ResolveRemark
        }));
    }

    [HttpPost("{id:long}/resolve")]
    [RequirePermission(SysErrorLogPermissionCodes.Resolve)]
    public async Task<ActionResult<ApiResponse<object>>> Resolve(long id, [FromBody] ResolveErrorLogRequest? request)
    {
        try
        {
            await _errorLogService.ResolveAsync(id, request?.Remark ?? string.Empty);
            return Ok(ApiResponse<object>.Ok(null, "已标记处理"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
        }
    }
}
