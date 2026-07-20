using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.AiAssistant;

public sealed class UserFeedbackAdminService : IUserFeedbackAdminService
{
    private readonly ApplicationDbContext _db;

    public UserFeedbackAdminService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UserFeedbackPagedResult> GetAdminListAsync(
        UserFeedbackAdminQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);

        var q = _db.UserFeedbacks.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Category))
            q = q.Where(x => x.Category == query.Category.Trim());
        if (query.NeedsHandling.HasValue)
            q = q.Where(x => x.NeedsHandling == query.NeedsHandling.Value);
        if (query.IsHandled.HasValue)
            q = q.Where(x => x.IsHandled == query.IsHandled.Value);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.Trim();
            q = q.Where(x =>
                (x.Title != null && x.Title.Contains(k))
                || (x.Summary != null && x.Summary.Contains(k))
                || (x.BizRef != null && x.BizRef.Contains(k)));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(x => x.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = items.Select(i => i.SubmitUserId).Distinct().ToList();
        var names = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.RealName })
            .ToListAsync(cancellationToken);
        var nameMap = names.ToDictionary(
            x => x.Id,
            x => string.IsNullOrWhiteSpace(x.RealName) ? x.UserName : x.RealName,
            StringComparer.OrdinalIgnoreCase);

        return new UserFeedbackPagedResult
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(x => new UserFeedbackListItemDto
            {
                Id = x.Id,
                Category = x.Category,
                Title = x.Title,
                Summary = x.Summary,
                BizRef = x.BizRef,
                SubmitUserId = x.SubmitUserId,
                SubmitUserName = nameMap.TryGetValue(x.SubmitUserId, out var n) ? n : null,
                NeedsHandling = x.NeedsHandling,
                IsHandled = x.IsHandled,
                CompletedDate = x.CompletedDate,
                CreateTime = x.CreateTime,
                PageUrl = x.PageUrl,
                RouteName = x.RouteName
            }).ToList()
        };
    }

    public async Task<UserFeedbackDetailDto?> GetAdminDetailAsync(
        string id,
        bool includeMessages,
        CancellationToken cancellationToken = default)
    {
        var x = await _db.UserFeedbacks.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (x == null) return null;

        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == x.SubmitUserId)
            .Select(u => new { u.UserName, u.RealName })
            .FirstOrDefaultAsync(cancellationToken);

        var dto = new UserFeedbackDetailDto
        {
            Id = x.Id,
            SessionId = x.SessionId,
            Category = x.Category,
            Title = x.Title,
            Summary = x.Summary,
            BizRef = x.BizRef,
            ReproSteps = x.ReproSteps,
            SubmitUserId = x.SubmitUserId,
            SubmitUserName = user == null
                ? null
                : (string.IsNullOrWhiteSpace(user.RealName) ? user.UserName : user.RealName),
            NeedsHandling = x.NeedsHandling,
            IsHandled = x.IsHandled,
            CompletedDate = x.CompletedDate,
            HandleRemark = x.HandleRemark,
            CreateTime = x.CreateTime,
            PageUrl = x.PageUrl,
            RouteName = x.RouteName,
            RouteParamsJson = x.RouteParamsJson,
            RouteQueryJson = x.RouteQueryJson
        };

        var docIds = await _db.UploadDocuments.AsNoTracking()
            .Where(d => d.BizType == FeedbackDocumentBizType.Feedback && d.BizId == x.Id && !d.IsDeleted)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);
        dto.AttachmentDocumentIds = docIds;

        if (includeMessages)
        {
            dto.Messages = await _db.AiAssistantMessages.AsNoTracking()
                .Where(m => m.SessionId == x.SessionId)
                .OrderBy(m => m.CreateTime)
                .Select(m => new AiAssistantMessageDto
                {
                    Id = m.Id,
                    Role = m.Role,
                    Content = m.Content,
                    AttachmentDocumentId = m.AttachmentDocumentId,
                    CreateTime = m.CreateTime
                })
                .ToListAsync(cancellationToken);
        }

        return dto;
    }

    public async Task<UserFeedbackDetailDto> PatchAdminAsync(
        string id,
        PatchUserFeedbackRequest request,
        string actorUserId,
        CancellationToken cancellationToken = default)
    {
        var x = await _db.UserFeedbacks.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("反馈不存在");

        if (request.NeedsHandling.HasValue)
            x.NeedsHandling = request.NeedsHandling.Value;
        if (request.IsHandled.HasValue)
            x.IsHandled = request.IsHandled.Value;
        if (request.CompletedDate.HasValue)
            x.CompletedDate = DateTime.SpecifyKind(request.CompletedDate.Value.Date, DateTimeKind.Unspecified);

        if (request.HandleRemark != null)
            x.HandleRemark = string.IsNullOrWhiteSpace(request.HandleRemark) ? null : request.HandleRemark.Trim();
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var c = request.Category.Trim().ToLowerInvariant();
            if (c is FeedbackCategories.Bug or FeedbackCategories.Suggestion or FeedbackCategories.Other)
                x.Category = c;
        }

        if (x.IsHandled && !x.CompletedDate.HasValue)
            x.CompletedDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);

        x.ModifyTime = DateTime.UtcNow;
        x.ModifyByUserId = string.IsNullOrWhiteSpace(actorUserId) ? null : actorUserId.Trim();
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            throw new InvalidOperationException($"保存失败: {detail}", ex);
        }

        return (await GetAdminDetailAsync(id, false, cancellationToken))!;
    }
}
