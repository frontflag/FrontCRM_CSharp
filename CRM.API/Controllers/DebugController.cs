using System.Text.RegularExpressions;
using CRM.API.Models.DTOs;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/debug")]
    public class DebugController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public DebugController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public class DebugItemDto
        {
            public string Name { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        /// <summary>
        /// Debug 页数据：数据库连接（密码前 5 位为星号）、debug 表记录。版本号由前端硬编码（PRD）。
        /// </summary>
        public class DebugPageDto
        {
            /// <summary>供界面展示的数据库连接串（密码前 5 位为星号，其余保留便于排错）</summary>
            public string DatabaseConnectionDisplay { get; set; } = string.Empty;
            public List<DebugItemDto> Items { get; set; } = new();
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<DebugPageDto>>> GetAll()
        {
            var items = await _context.DebugRecords
                .OrderBy(x => x.Name)
                .Select(x => new DebugItemDto
                {
                    Name = x.Name,
                    Value = x.Value
                })
                .ToListAsync();

            var rawCs = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(rawCs))
            {
                // 与运行时 DbContext 实际使用的一致（例如仅通过环境变量注入时）
                rawCs = _context.Database.GetConnectionString();
            }

            var page = new DebugPageDto
            {
                DatabaseConnectionDisplay = MaskPasswordFirstFiveInConnectionString(rawCs),
                Items = items
            };

            return Ok(ApiResponse<DebugPageDto>.Ok(page));
        }

        /// <summary>PRD：密码的前 5 位显示为星号；不足 5 位则全部打星。</summary>
        private static string MaskPasswordFirstFiveInConnectionString(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return "(无法获取数据库连接串，请检查配置)";

            var s = connectionString.Trim();
            return Regex.Replace(
                s,
                @"(Password|Pwd)\s*=\s*([^;]*)",
                m =>
                {
                    var key = m.Groups[1].Value;
                    var pwd = m.Groups[2].Value;
                    if (string.IsNullOrEmpty(pwd))
                        return $"{key}=";
                    string display;
                    if (pwd.Length <= 5)
                        display = new string('*', pwd.Length);
                    else
                        display = "*****" + pwd.Substring(5);
                    return $"{key}={display}";
                },
                RegexOptions.IgnoreCase);
        }
    }
}

