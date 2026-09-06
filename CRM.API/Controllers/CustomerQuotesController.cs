using CRM.API.Authorization;
using CRM.API.Services;
using CRM.API.Services.Interfaces;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/customer-quotes")]
public class CustomerQuotesController : ControllerBase
{
    private readonly ICustomerQuoteService _service;
    private readonly IEmailSender _emailSender;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CustomerQuotesController> _logger;

    public CustomerQuotesController(
        ICustomerQuoteService service,
        IEmailSender emailSender,
        ApplicationDbContext db,
        ILogger<CustomerQuotesController> logger)
    {
        _service = service;
        _emailSender = emailSender;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    [RequirePermission("customer-quote.read")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] short? status = null,
        [FromQuery] string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var (items, total) = await _service.GetQuotesPagedAsync(userId, page, pageSize, status, keyword, cancellationToken);
        return Ok(new { success = true, data = new { items, total, page, pageSize }, errorCode = 0 });
    }

    [HttpGet("{id}")]
    [RequirePermission("customer-quote.read")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var row = await _service.GetQuoteByIdAsync(userId, id, cancellationToken);
        if (row == null)
            return NotFound(new { success = false, message = "客户报价单不存在或无权查看" });
        return Ok(new { success = true, data = row, errorCode = 0 });
    }

    [HttpGet("{id}/report-data")]
    [RequirePermission("customer-quote.read")]
    public async Task<IActionResult> GetReportData(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        try
        {
            var (quote, billTo) = await _service.GetReportDataAsync(userId, id, cancellationToken);
            var companyProfile = await CompanyProfileBundleLoader.LoadAsync(_db, _logger, cancellationToken);
            CompanyProfileBundleLoader.StripSmtpEmail(companyProfile);
            return Ok(new
            {
                success = true,
                data = new { quote, billTo, companyProfile },
                errorCode = 0
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id}/send-email")]
    [RequirePermission("customer-quote.send")]
    public async Task<IActionResult> SendEmail(
        string id,
        [FromBody] SendCustomerQuoteEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { success = false, message = "未登录", code = "NoDefaultMailbox" });

        try
        {
            if (request == null || !CustomerQuoteSendRules.LooksLikeEmail(request.To))
                return BadRequest(new { success = false, message = CustomerQuoteSendRules.InvalidToMessage });
            if (string.IsNullOrWhiteSpace(request.PdfBase64))
                return BadRequest(new { success = false, message = "PDF 内容不能为空" });

            var quote = await _service.GetQuoteByIdAsync(userId, id, cancellationToken)
                ?? throw new KeyNotFoundException("客户报价单不存在或无权查看");
            if (!CustomerQuoteSendRules.CanSendEmail(quote.Status))
                return BadRequest(new { success = false, message = CustomerQuoteSendRules.VoidCannotSendMessage });

            var raw = request.PdfBase64.Trim();
            var comma = raw.IndexOf(',');
            if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
                raw = raw[(comma + 1)..];

            byte[] pdfBytes;
            try
            {
                pdfBytes = Convert.FromBase64String(raw);
            }
            catch (FormatException)
            {
                return BadRequest(new { success = false, message = "PDF 编码无效" });
            }

            const int maxBytes = 25 * 1024 * 1024;
            if (pdfBytes.Length > maxBytes)
                return BadRequest(new { success = false, message = "附件过大" });

            var displayCode = string.IsNullOrWhiteSpace(quote.DisplayCode)
                ? quote.CustomerQuoteCode
                : quote.DisplayCode;
            var fileName = string.IsNullOrWhiteSpace(request.FileName) ? $"{displayCode}.pdf" : request.FileName.Trim();
            if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                fileName += ".pdf";

            var subject = string.IsNullOrWhiteSpace(request.Subject)
                ? $"客户报价单 {displayCode}"
                : request.Subject.Trim();

            await _emailSender.SendWithAttachmentAsync(
                userId,
                request.To.Trim(),
                subject,
                request.Body,
                pdfBytes,
                fileName,
                "application/pdf",
                cancellationToken);

            var updated = await _service.MarkSentByEmailAsync(userId, id, cancellationToken);
            try
            {
                await _service.AppendActionLogAsync(
                    userId,
                    id,
                    CustomerQuoteActionLogRules.SendEmail,
                    request.To,
                    cancellationToken);
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "客户报价单邮件已发送但写日志失败: {Id}", id);
            }
            return Ok(new { success = true, message = "邮件已发送", data = updated, errorCode = 0 });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { success = false, message = ex.Message });
        }
        catch (EmailSendException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, code = ex.Code });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送客户报价单邮件失败: {Id}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [RequirePermission("customer-quote.write")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCustomerQuoteRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { success = false, message = "未登录" });

        try
        {
            var row = await _service.UpdateQuoteAsync(userId, id, request, cancellationToken);
            return Ok(new { success = true, data = row, errorCode = 0 });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id}/apply-profit-factor")]
    [RequirePermission("customer-quote.write")]
    public async Task<IActionResult> ApplyProfitFactor(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { success = false, message = "未登录" });

        try
        {
            var row = await _service.ApplyProfitFactorAsync(userId, id, cancellationToken);
            return Ok(new { success = true, data = row, errorCode = 0 });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id}/mark-sent")]
    [RequirePermission("customer-quote.write")]
    public async Task<IActionResult> MarkSent(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { success = false, message = "未登录" });

        try
        {
            var row = await _service.MarkSentAsync(userId, id, cancellationToken);
            return Ok(new { success = true, data = row, errorCode = 0 });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [RequirePermission("customer-quote.write")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { success = false, message = "未登录" });

        try
        {
            await _service.DeleteQuoteAsync(userId, id, cancellationToken);
            return Ok(new { success = true, errorCode = 0 });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{id}/action-logs")]
    [RequirePermission("customer-quote.read")]
    public async Task<IActionResult> GetActionLogs(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        try
        {
            var rows = await _service.GetActionLogsAsync(userId, id, cancellationToken);
            return Ok(new { success = true, data = rows, errorCode = 0 });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id}/action-logs")]
    [RequirePermission("customer-quote.read")]
    public async Task<IActionResult> AppendActionLog(
        string id,
        [FromBody] AppendCustomerQuoteActionLogRequest? request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { success = false, message = "未登录" });

        try
        {
            var actionType = CustomerQuoteActionLogRules.ParseClientAction(request?.Action);
            await _service.AppendActionLogAsync(userId, id, actionType, request?.Remark, cancellationToken);
            return Ok(new { success = true, errorCode = 0 });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

public class AppendCustomerQuoteActionLogRequest
{
    public string Action { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

public class SendCustomerQuoteEmailRequest
{
    public string To { get; set; } = string.Empty;
    public string PdfBase64 { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
}
