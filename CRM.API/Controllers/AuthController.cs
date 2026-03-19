using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CRM.API.Models.DTOs;
using CRM.API.Services.Interfaces;
using CRM.Core.Interfaces;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRbacService _rbacService;
        private readonly ILogger<AuthController> _logger;
        private readonly IUserService _userService;

        public AuthController(IAuthService authService, IRbacService rbacService, ILogger<AuthController> logger, IUserService userService)
        {
            _authService = authService;
            _rbacService = rbacService;
            _logger = logger;
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                return StatusCode(500, ApiResponse<AuthResponse>.Fail("注册失败", 500));
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed: {Message}", ex.Message);
                return StatusCode(500, ApiResponse<AuthResponse>.Fail($"登录失败: {ex.Message}", 500));
            }
        }

        [Authorize]
        [HttpGet("me")]
        public ActionResult<ApiResponse<object>> GetCurrentUser()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            return Ok(ApiResponse<object>.Ok(new
            {
                Email = userEmail,
                UserName = userName,
                Id = userId
            }, "获取用户信息成功"));
        }

        [Authorize]
        [HttpGet("permission-summary")]
        public async Task<ActionResult<ApiResponse<object>>> GetPermissionSummary()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(ApiResponse<object>.Fail("未获取到用户信息", 401));

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
                return Ok(ApiResponse<object>.Ok(summary, "获取权限摘要成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPermissionSummary failed");
                return StatusCode(500, ApiResponse<object>.Fail("获取权限摘要失败", 500));
            }
        }

        /// <summary>获取业务员列表（用于下拉选择）</summary>
        [Authorize]
        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<object>>> GetUsers()
        {
            try
            {
                var users = await _userService.GetAllAsync();
                var list = users
                    .Where(u => u.Status == 1)
                    .Select(u => new
                    {
                        id = u.Id,
                        label = string.IsNullOrWhiteSpace(u.RealName) ? u.UserName : u.RealName,
                        userName = u.UserName,
                        realName = u.RealName
                    });
                return Ok(ApiResponse<object>.Ok(list, "获取用户列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUsers failed");
                return StatusCode(500, ApiResponse<object>.Fail("获取用户列表失败", 500));
            }
        }
    }
}
