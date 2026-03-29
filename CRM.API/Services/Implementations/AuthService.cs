using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CRM.API.Models.DTOs;
using CRM.API.Services.Interfaces;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Infrastructure.Data;

namespace CRM.API.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<User> _userRepository;
        private readonly IRbacService _rbacService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, IRepository<User> userRepository, IRbacService rbacService, ILogger<AuthService> logger)
        {
            _context = context;
            _userRepository = userRepository;
            _rbacService = rbacService;
            _logger = logger;
        }

        public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            // 检查账号是否重名
            var existingByUserName = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == request.UserName);
            if (existingByUserName != null)
            {
                return ApiResponse<AuthResponse>.Fail("账号已存在，请更换其他账号名");
            }

            // 检查邮箱是否已被注册（如果提供了邮箱）
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var existingByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingByEmail != null)
                {
                    return ApiResponse<AuthResponse>.Fail("邮箱已被注册");
                }
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new User
            {
                UserName = request.UserName,
                Email = request.Email,
                PasswordHash = passwordHash,
                Salt = BCrypt.Net.BCrypt.GenerateSalt(),
                PasswordPlain = request.Password, // 仅用于开发测试
                IsActive = true
            };

            await _userRepository.AddAsync(user);
            var saved = await _context.SaveChangesAsync();
            _logger.LogInformation($"User registered: {user.UserName}, Email: {user.Email}, UserId: {user.Id}, SaveChanges result: {saved}");

            var token = GenerateJwtToken(user.Email, user.UserName, user.Id);
            var summary = await _rbacService.GetUserPermissionSummaryAsync(user.Id);

            return ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                Token = token,
                UserName = user.UserName,
                Email = user.Email ?? string.Empty,
                UserId = user.Id,
                IsSysAdmin = summary.IsSysAdmin,
                RoleCodes = summary.RoleCodes,
                PermissionCodes = summary.PermissionCodes,
                DepartmentIds = summary.DepartmentIds
            }, "注册成功");
        }

        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            // 按账号（UserName）查询用户
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == request.UserName);

            if (user == null || !user.IsActive)
            {
                return ApiResponse<AuthResponse>.Fail("账号不存在或已被禁用");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return ApiResponse<AuthResponse>.Fail("密码错误");
            }

            var token = GenerateJwtToken(user.Email ?? user.UserName, user.UserName, user.Id);
            var summary = await _rbacService.GetUserPermissionSummaryAsync(user.Id);
            return ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                Token = token,
                UserName = user.UserName,
                Email = user.Email ?? "",
                UserId = user.Id,
                IsSysAdmin = summary.IsSysAdmin,
                RoleCodes = summary.RoleCodes,
                PermissionCodes = summary.PermissionCodes,
                DepartmentIds = summary.DepartmentIds
            }, "登录成功");;
        }

        public async Task<ApiResponse<AuthResponse>> ImpersonateAsync(string actorUserId, string targetUserId)
        {
            if (string.IsNullOrWhiteSpace(actorUserId))
                return ApiResponse<AuthResponse>.Fail("未识别当前操作者", 401);
            if (string.IsNullOrWhiteSpace(targetUserId))
                return ApiResponse<AuthResponse>.Fail("目标用户无效");

            var actorSummary = await _rbacService.GetUserPermissionSummaryAsync(actorUserId);
            if (!actorSummary.IsSysAdmin)
                return ApiResponse<AuthResponse>.Fail("仅系统管理员可使用模拟登录", 403);

            if (string.Equals(actorUserId, targetUserId, StringComparison.OrdinalIgnoreCase))
                return ApiResponse<AuthResponse>.Fail("不能模拟登录当前账号");

            var target = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);
            if (target == null || !target.IsActive || target.Status != 1)
                return ApiResponse<AuthResponse>.Fail("目标账号不存在、已禁用或未启用");

            _logger.LogWarning(
                "Impersonate login: actorUserId={ActorUserId} targetUserId={TargetUserId} targetUserName={TargetUserName}",
                actorUserId,
                target.Id,
                target.UserName);

            var token = GenerateJwtToken(target.Email ?? target.UserName, target.UserName, target.Id);
            var summary = await _rbacService.GetUserPermissionSummaryAsync(target.Id);
            return ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                Token = token,
                UserName = target.UserName,
                Email = target.Email ?? "",
                UserId = target.Id,
                IsSysAdmin = summary.IsSysAdmin,
                RoleCodes = summary.RoleCodes,
                PermissionCodes = summary.PermissionCodes,
                DepartmentIds = summary.DepartmentIds
            }, "模拟登录成功");
        }

        public string GenerateJwtToken(string email, string userName, string userId)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: JwtSettings.Issuer,
                audience: JwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(JwtSettings.ExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
