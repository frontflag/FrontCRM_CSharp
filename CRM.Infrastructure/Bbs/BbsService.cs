using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Bbs;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Bbs;

public class BbsService : IBbsService
{
    private readonly ApplicationDbContext _db;

    public BbsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BbsSubjectListItemDto>> GetTopSubjectsAsync(
        string userId,
        bool isModerator,
        CancellationToken ct = default)
    {
        var list = await _db.BbsSubjects.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsTop)
            .OrderByDescending(x => x.CreateTime)
            .Take(50)
            .ToListAsync(ct);
        var names = await ResolveUserNamesAsync(list.Select(x => x.CreateBy), ct);
        return list.Select(x => ToListItem(x, userId, isModerator, names)).ToList();
    }

    public async Task<BbsSubjectPagedDto> QuerySubjectsAsync(
        BbsSubjectQuery query,
        string userId,
        bool isModerator,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.BbsSubjects.AsNoTracking().Where(x => !x.IsDeleted && !x.IsTop);
        if (query.Type is int t)
            q = q.Where(x => x.Type == t);
        if (query.Status is int s)
            q = q.Where(x => x.Status == s);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(x => x.Title.Contains(kw) || x.Content.Contains(kw));
        }

        var total = await q.CountAsync(ct);
        var list = await q
            .OrderByDescending(x => x.LastReplyTime ?? x.CreateTime)
            .ThenByDescending(x => x.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var names = await ResolveUserNamesAsync(list.Select(x => x.CreateBy), ct);
        return new BbsSubjectPagedDto
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = list.Select(x => ToListItem(x, userId, isModerator, names)).ToList()
        };
    }

    public async Task<BbsSubjectDetailDto?> GetSubjectDetailAsync(
        string subjectId,
        string userId,
        bool isModerator,
        CancellationToken ct = default)
    {
        var entity = await _db.BbsSubjects
            .FirstOrDefaultAsync(x => x.Id == subjectId && !x.IsDeleted, ct);
        if (entity == null) return null;

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE public.bbs_subject SET view_count = view_count + 1 WHERE id = {subjectId} AND is_deleted = false",
            ct);
        entity.ViewCount += 1;

        var names = await ResolveUserNamesAsync(new[] { entity.CreateBy }, ct);
        var item = ToListItem(entity, userId, isModerator, names);
        return new BbsSubjectDetailDto
        {
            Id = item.Id,
            Title = item.Title,
            Type = item.Type,
            TypeLabel = item.TypeLabel,
            Status = item.Status,
            StatusLabel = item.StatusLabel,
            IsTop = item.IsTop,
            IsHot = item.IsHot,
            Anonymous = item.Anonymous,
            ViewCount = item.ViewCount,
            ReplyCount = item.ReplyCount,
            LastReplyTime = item.LastReplyTime,
            CreateTime = item.CreateTime,
            CreateBy = item.CreateBy,
            AuthorDisplay = item.AuthorDisplay,
            CanDelete = item.CanDelete,
            CanEdit = item.CanEdit,
            CanSetTop = item.CanSetTop,
            CanModerate = item.CanModerate,
            Content = entity.Content
        };
    }

    public async Task<BbsSubjectDetailDto> CreateSubjectAsync(
        BbsSubjectCreateRequest request,
        string userId,
        bool isModerator,
        CancellationToken ct = default)
    {
        ValidateContent(request.Title, request.Content, request.Type);

        var entity = new BbsSubject
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            Type = request.Type,
            Status = BbsSubjectStatuses.Open,
            Anonymous = request.Anonymous,
            CreateTime = DateTime.UtcNow,
            CreateBy = userId
        };
        _db.BbsSubjects.Add(entity);
        await _db.SaveChangesAsync(ct);

        var detail = await GetSubjectDetailWithoutBumpAsync(entity.Id, userId, isModerator, ct);
        return detail!;
    }

    public async Task<BbsSubjectDetailDto> UpdateSubjectAsync(
        string subjectId,
        BbsSubjectUpdateRequest request,
        string userId,
        bool isModerator,
        CancellationToken ct = default)
    {
        ValidateContent(request.Title, request.Content, request.Type);
        var entity = await RequireSubjectAsync(subjectId, ct);
        EnsureOwnerOrModerator(entity, userId, isModerator);

        entity.Title = request.Title.Trim();
        entity.Content = request.Content.Trim();
        entity.Type = request.Type;
        entity.Anonymous = request.Anonymous;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyBy = userId;
        await _db.SaveChangesAsync(ct);

        return (await GetSubjectDetailWithoutBumpAsync(subjectId, userId, isModerator, ct))!;
    }

    public async Task CloseSubjectAsync(string subjectId, string userId, bool isModerator, CancellationToken ct = default)
    {
        var entity = await RequireSubjectAsync(subjectId, ct);
        EnsureOwnerOrModerator(entity, userId, isModerator);
        entity.Status = BbsSubjectStatuses.Close;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyBy = userId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task OpenSubjectAsync(string subjectId, string userId, bool isModerator, CancellationToken ct = default)
    {
        var entity = await RequireSubjectAsync(subjectId, ct);
        EnsureOwnerOrModerator(entity, userId, isModerator);
        entity.Status = BbsSubjectStatuses.Open;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyBy = userId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetTopAsync(string subjectId, string userId, bool isModerator, CancellationToken ct = default)
    {
        if (!isModerator) throw new UnauthorizedAccessException("仅版主可置顶");
        var entity = await RequireSubjectAsync(subjectId, ct);
        entity.IsTop = true;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyBy = userId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CancelTopAsync(string subjectId, string userId, bool isModerator, CancellationToken ct = default)
    {
        if (!isModerator) throw new UnauthorizedAccessException("仅版主可取消置顶");
        var entity = await RequireSubjectAsync(subjectId, ct);
        entity.IsTop = false;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyBy = userId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteSubjectAsync(string subjectId, string userId, bool isModerator, CancellationToken ct = default)
    {
        var entity = await RequireSubjectAsync(subjectId, ct);
        EnsureOwnerOrModerator(entity, userId, isModerator);
        entity.IsDeleted = true;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyBy = userId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<BbsReplyPagedDto> GetRepliesAsync(
        string subjectId,
        int page,
        int pageSize,
        string userId,
        bool isModerator,
        CancellationToken ct = default)
    {
        _ = await RequireSubjectAsync(subjectId, ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.BbsReplies.AsNoTracking()
            .Where(x => !x.IsDeleted && x.SubjectId == subjectId);
        var total = await q.CountAsync(ct);
        var list = await q
            .OrderBy(x => x.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var names = await ResolveUserNamesAsync(list.Select(x => x.CreateBy), ct);
        return new BbsReplyPagedDto
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = list.Select(x => ToReplyDto(x, userId, isModerator, names)).ToList()
        };
    }

    public async Task<BbsReplyDto> AddReplyAsync(
        string subjectId,
        BbsReplyCreateRequest request,
        string userId,
        bool isModerator,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ArgumentException("回复内容不能为空");
        EnsureContentBytes(request.Content);

        var subject = await RequireSubjectAsync(subjectId, ct);
        if (subject.Status != BbsSubjectStatuses.Open)
            throw new InvalidOperationException("主题已经关闭,不能回复");

        var reply = new BbsReply
        {
            Id = Guid.NewGuid().ToString(),
            SubjectId = subjectId,
            Content = request.Content.Trim(),
            Anonymous = request.Anonymous,
            CreateTime = DateTime.UtcNow,
            CreateBy = userId
        };
        _db.BbsReplies.Add(reply);

        subject.ReplyCount += 1;
        subject.LastReplyTime = DateTime.UtcNow;
        if (subject.ReplyCount > BbsLimits.HotReplyThreshold)
            subject.IsHot = true;
        subject.ModifyTime = DateTime.UtcNow;
        subject.ModifyBy = userId;

        await _db.SaveChangesAsync(ct);

        var names = await ResolveUserNamesAsync(new[] { userId }, ct);
        return ToReplyDto(reply, userId, isModerator, names);
    }

    public async Task DeleteReplyAsync(string replyId, string userId, bool isModerator, CancellationToken ct = default)
    {
        var reply = await _db.BbsReplies.FirstOrDefaultAsync(x => x.Id == replyId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("没有找到回复");
        if (!isModerator && !string.Equals(reply.CreateBy, userId, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("无权删除该回复");

        reply.IsDeleted = true;
        reply.ModifyTime = DateTime.UtcNow;
        reply.ModifyBy = userId;

        var subject = await _db.BbsSubjects.FirstOrDefaultAsync(x => x.Id == reply.SubjectId && !x.IsDeleted, ct);
        if (subject != null && subject.ReplyCount > 0)
            subject.ReplyCount -= 1;

        await _db.SaveChangesAsync(ct);
    }

    private async Task<BbsSubjectDetailDto?> GetSubjectDetailWithoutBumpAsync(
        string subjectId,
        string userId,
        bool isModerator,
        CancellationToken ct)
    {
        var entity = await _db.BbsSubjects.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == subjectId && !x.IsDeleted, ct);
        if (entity == null) return null;
        var names = await ResolveUserNamesAsync(new[] { entity.CreateBy }, ct);
        var item = ToListItem(entity, userId, isModerator, names);
        return new BbsSubjectDetailDto
        {
            Id = item.Id,
            Title = item.Title,
            Type = item.Type,
            TypeLabel = item.TypeLabel,
            Status = item.Status,
            StatusLabel = item.StatusLabel,
            IsTop = item.IsTop,
            IsHot = item.IsHot,
            Anonymous = item.Anonymous,
            ViewCount = item.ViewCount,
            ReplyCount = item.ReplyCount,
            LastReplyTime = item.LastReplyTime,
            CreateTime = item.CreateTime,
            CreateBy = item.CreateBy,
            AuthorDisplay = item.AuthorDisplay,
            CanDelete = item.CanDelete,
            CanEdit = item.CanEdit,
            CanSetTop = item.CanSetTop,
            CanModerate = item.CanModerate,
            Content = entity.Content
        };
    }

    private async Task<BbsSubject> RequireSubjectAsync(string subjectId, CancellationToken ct)
    {
        return await _db.BbsSubjects.FirstOrDefaultAsync(x => x.Id == subjectId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("没有找到主题");
    }

    private static void EnsureOwnerOrModerator(BbsSubject entity, string userId, bool isModerator)
    {
        if (isModerator) return;
        if (string.Equals(entity.CreateBy, userId, StringComparison.OrdinalIgnoreCase)) return;
        throw new UnauthorizedAccessException("无权操作该主题");
    }

    private static void ValidateContent(string title, string content, int type)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("标题不能为空");
        if (title.Trim().Length > BbsLimits.TitleMaxLength)
            throw new ArgumentException($"标题最长 {BbsLimits.TitleMaxLength} 字符");
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("正文不能为空");
        EnsureContentBytes(content);
        if (!BbsSubjectTypes.IsAllowed(type))
            throw new ArgumentException("主题类型无效");
    }

    private static void EnsureContentBytes(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetByteCount(content);
        if (bytes > BbsLimits.ContentMaxBytes)
            throw new ArgumentException("正文过长");
    }

    private async Task<Dictionary<string, string>> ResolveUserNamesAsync(
        IEnumerable<string?> userIds,
        CancellationToken ct)
    {
        var ids = userIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count == 0) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var users = await _db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.RealName })
            .ToListAsync(ct);

        return users.ToDictionary(
            u => u.Id,
            u => string.IsNullOrWhiteSpace(u.RealName) ? (u.UserName ?? u.Id) : u.RealName!,
            StringComparer.OrdinalIgnoreCase);
    }

    private static BbsSubjectListItemDto ToListItem(
        BbsSubject x,
        string userId,
        bool isModerator,
        IReadOnlyDictionary<string, string> names)
    {
        var rawName = !string.IsNullOrWhiteSpace(x.CreateBy) && names.TryGetValue(x.CreateBy, out var n)
            ? n
            : (x.CreateBy ?? "");
        var isOwner = string.Equals(x.CreateBy, userId, StringComparison.OrdinalIgnoreCase);
        return new BbsSubjectListItemDto
        {
            Id = x.Id,
            Title = x.Title,
            Type = x.Type,
            TypeLabel = BbsSubjectTypes.ToLabel(x.Type),
            Status = x.Status,
            StatusLabel = BbsSubjectStatuses.ToLabel(x.Status),
            IsTop = x.IsTop,
            IsHot = x.IsHot,
            Anonymous = x.Anonymous,
            ViewCount = x.ViewCount,
            ReplyCount = x.ReplyCount,
            LastReplyTime = x.LastReplyTime,
            CreateTime = x.CreateTime,
            CreateBy = x.CreateBy,
            AuthorDisplay = FormatAuthor(rawName, x.Anonymous, isModerator),
            CanDelete = isModerator || isOwner,
            CanEdit = isModerator || isOwner,
            CanSetTop = isModerator,
            CanModerate = isModerator
        };
    }

    private static BbsReplyDto ToReplyDto(
        BbsReply x,
        string userId,
        bool isModerator,
        IReadOnlyDictionary<string, string> names)
    {
        var rawName = !string.IsNullOrWhiteSpace(x.CreateBy) && names.TryGetValue(x.CreateBy, out var n)
            ? n
            : (x.CreateBy ?? "");
        var isOwner = string.Equals(x.CreateBy, userId, StringComparison.OrdinalIgnoreCase);
        return new BbsReplyDto
        {
            Id = x.Id,
            SubjectId = x.SubjectId,
            Content = x.Content,
            Anonymous = x.Anonymous,
            CreateTime = x.CreateTime,
            CreateBy = x.CreateBy,
            AuthorDisplay = FormatAuthor(rawName, x.Anonymous, isModerator),
            CanDelete = isModerator || isOwner
        };
    }

    private static string FormatAuthor(string realName, bool anonymous, bool isModerator)
    {
        if (!anonymous) return string.IsNullOrWhiteSpace(realName) ? "—" : realName;
        if (isModerator) return string.IsNullOrWhiteSpace(realName) ? "【匿名】" : $"{realName}【匿名】";
        return "*****";
    }
}
