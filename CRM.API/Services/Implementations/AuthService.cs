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
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, IRepository<User> userRepository, ILogger<AuthService> logger)
        {
            _context = context;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return ApiResponse<AuthResponse>.Fail("邮箱已被注册");
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

            return ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                Token = token,
                UserName = user.UserName,
                Email = user.Email
            }, "注册成功");
        }

        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !user.IsActive)
            {
                return ApiResponse<AuthResponse>.Fail("用户不存在或已被禁用");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return ApiResponse<AuthResponse>.Fail("密码错误");
            }

            var token = GenerateJwtToken(user.Email, user.UserName, user.Id);

            return ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                Token = token,
                UserName = user.UserName,
                Email = user.Email
            }, "登录成功");
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
