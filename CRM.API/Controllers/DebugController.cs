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

        public DebugController(ApplicationDbContext context)
        {
            _context = context;
        }

        public class DebugItemDto
        {
            public string Name { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<DebugItemDto>>>> GetAll()
        {
            var items = await _context.DebugRecords
                .OrderBy(x => x.Name)
                .Select(x => new DebugItemDto
                {
                    Name = x.Name,
                    Value = x.Value
                })
                .ToListAsync();

            return Ok(ApiResponse<List<DebugItemDto>>.Ok(items));
        }
    }
}

