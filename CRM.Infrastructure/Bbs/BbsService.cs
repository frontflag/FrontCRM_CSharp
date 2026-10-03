using CRM.Core.Constants;
using CRM.Core.Document;
using CRM.Core.Interfaces;
using CRM.Core.Models.Bbs;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Bbs;

public class BbsService : IBbsService
{
    private readonly ApplicationDbContext _db;
    private readonly IDocumentService _documents;

    public BbsService(ApplicationDbContext db, IDocumentService documents)
    {
        _db = db;
        _documents = documents;
    }

    public async Task<IReadOnlySet<int>> GetModeratedTypesAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new HashSet<int>();

        var types = await _db.BbsBoardModerators.AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .Select(x => x.SubjectType)
            .ToListAsync(ct);
        return types.ToHashSet();
    }

    public async Task<IReadOnlyList<BbsBoardModeratorDto>> ListBoardModeratorsAsync(CancellationToken ct = default)
    {
        var rows = await _db.BbsBoardModerators.AsNoTracking().ToListAsync(ct);
        var byType = rows.ToDictionary(x => x.SubjectType, x => x);
        var userIds = rows
            .Select(x => x.UserId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var users = await LoadUserAccountsAsync(userIds, ct);
        var counts = await _db.BbsSubjects.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .GroupBy(x => x.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var countMap = counts.ToDictionary(x => x.Type, x => x.Count);

        var types = BbsSubjectTypes.All
            .Concat(rows.Where(r => BbsSubjectTypes.IsCustom(r.SubjectType)).Select(r => r.SubjectType))
            .Distinct()
            .ToList();

        return types.Select(t =>
        {
            var dto = ToBoardDto(t, byType.GetValueOrDefault(t), users);
            dto.SubjectCount = countMap.GetValueOrDefault(t);
            return dto;
        }).ToList();
    }

    public async Task<BbsBoardModeratorDto> SetBoardModeratorAsync(
        int type,
        string? userId,
        string? displayName,
        int? sortOrder,
        string operatorUserId,
        CancellationToken ct = default)
    {
        if (!BbsSubjectTypes.IsAllowed(type))
            throw new ArgumentException("主题类型无效");
        if (!BbsSubjectTypes.SupportsBoardModerator(type))
            throw new ArgumentException("该板块不支持设置版主与名称");
        if (BbsSubjectTypes.IsCustom(type))
        {
            var custom = await _db.BbsBoardModerators.AsNoTracking()
                .FirstOrDefaultAsync(x => x.SubjectType == type, ct);
            if (custom == null)
                throw new ArgumentException("自定义板块不存在");
        }

        var existing = await _db.BbsBoardModerators
            .FirstOrDefaultAsync(x => x.SubjectType == type, ct);
        if (existing?.IsDeleted == true)
            throw new InvalidOperationException("该板块已删除，无法设置");

        var name = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        if (name != null && name.Length > 50)
            throw new ArgumentException("板块名称最长 50 字");
        if (BbsSubjectTypes.IsCustom(type) && string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(existing?.DisplayName))
            throw new ArgumentException("自定义板块须填写名称");

        var order = sortOrder is int so && so > 0
            ? so
            : existing is { SortOrder: > 0 }
                ? existing.SortOrder
                : BbsSubjectTypes.DefaultSortOrder(type);
        if (order < 1 || order > 999)
            throw new ArgumentException("显示顺序须在 1～999");

        string? uid = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
        string? userName = null;
        string? realName = null;
        if (uid != null)
        {
            var user = await _db.Users.AsNoTracking()
                .Where(u => u.Id == uid)
                .Select(u => new { u.Id, u.UserName, u.RealName, u.IsActive, u.Status })
                .FirstOrDefaultAsync(ct)
                ?? throw new ArgumentException("用户不存在");
            if (!user.IsActive || user.Status != 1)
                throw new ArgumentException("用户已停用，不能设为版主");
            uid = user.Id;
            userName = user.UserName;
            realName = user.RealName;
        }

        // 仅当显式传入 sortOrder 时才做同号后移，避免改版主时打乱顺序
        if (sortOrder is int explicitOrder && explicitOrder > 0)
            await BumpCollidingSortOrdersAsync(type, explicitOrder, operatorUserId, ct);

        if (existing == null)
        {
            existing = new BbsBoardModerator
            {
                SubjectType = type,
                UserId = uid,
                DisplayName = name,
                SortOrder = order,
                IsDeleted = false,
                UpdateTime = DateTime.UtcNow,
                UpdateBy = operatorUserId
            };
            _db.BbsBoardModerators.Add(existing);
        }
        else
        {
            existing.UserId = uid;
            if (name != null || !BbsSubjectTypes.IsCustom(type))
                existing.DisplayName = name;
            existing.SortOrder = order;
            existing.IsDeleted = false;
            existing.UpdateTime = DateTime.UtcNow;
            existing.UpdateBy = operatorUserId;
        }

        await _db.SaveChangesAsync(ct);

        return new BbsBoardModeratorDto
        {
            Type = type,
            UserId = uid,
            UserName = userName,
            RealName = realName,
            DisplayName = existing.DisplayName,
            DefaultName = BbsSubjectTypes.ToLabel(type),
            IsDeleted = false,
            SortOrder = order,
            SubjectCount = await CountSubjectsByTypeAsync(type, ct)
        };
    }

    public async Task<BbsBoardModeratorDto> CreateBoardAsync(
        string displayName,
        string operatorUserId,
        CancellationToken ct = default)
    {
        var name = (displayName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("请填写板块名称");
        if (name.Length > 50)
            throw new ArgumentException("板块名称最长 50 字");

        var maxType = await _db.BbsBoardModerators.AsNoTracking()
            .Where(x => x.SubjectType >= BbsSubjectTypes.CustomMin)
            .Select(x => (int?)x.SubjectType)
            .MaxAsync(ct) ?? (BbsSubjectTypes.CustomMin - 1);
        var type = Math.Max(maxType + 1, BbsSubjectTypes.CustomMin);

        var maxOrder = await _db.BbsBoardModerators.AsNoTracking()
            .Where(x => !x.IsDeleted && x.SortOrder > 0)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(ct) ?? 40;
        var order = Math.Min(999, maxOrder + 10);

        var row = new BbsBoardModerator
        {
            SubjectType = type,
            UserId = null,
            DisplayName = name,
            SortOrder = order,
            IsDeleted = false,
            UpdateTime = DateTime.UtcNow,
            UpdateBy = operatorUserId
        };
        _db.BbsBoardModerators.Add(row);
        await _db.SaveChangesAsync(ct);

        return new BbsBoardModeratorDto
        {
            Type = type,
            DisplayName = name,
            DefaultName = BbsSubjectTypes.ToLabel(type),
            IsDeleted = false,
            SortOrder = order,
            SubjectCount = 0
        };
    }

    public async Task ReorderBoardsAsync(
        IReadOnlyList<int> orderedTypes,
        string operatorUserId,
        CancellationToken ct = default)
    {
        if (orderedTypes == null || orderedTypes.Count == 0)
            throw new ArgumentException("排序列表不能为空");

        var seen = new HashSet<int>();
        foreach (var type in orderedTypes)
        {
            if (!seen.Add(type))
                throw new ArgumentException($"板块类型重复：{type}");
            if (!BbsSubjectTypes.IsMovableBoardType(type))
                throw new ArgumentException($"板块不可调整顺序：{BbsSubjectTypes.ToLabel(type)}");
        }

        var rows = await _db.BbsBoardModerators.ToListAsync(ct);
        var byType = rows.ToDictionary(x => x.SubjectType);

        foreach (var type in orderedTypes)
        {
            if (byType.TryGetValue(type, out var row) && row.IsDeleted)
                throw new InvalidOperationException($"板块已删除：{BbsSubjectTypes.ToLabel(type)}");
            if (BbsSubjectTypes.IsCustom(type) && !byType.ContainsKey(type))
                throw new ArgumentException($"自定义板块不存在：{type}");
        }

        var sort = 10;
        foreach (var type in orderedTypes)
        {
            if (!byType.TryGetValue(type, out var row))
            {
                row = new BbsBoardModerator
                {
                    SubjectType = type,
                    UserId = null,
                    DisplayName = null,
                    IsDeleted = false
                };
                _db.BbsBoardModerators.Add(row);
                byType[type] = row;
            }

            row.SortOrder = sort;
            row.IsDeleted = false;
            row.UpdateTime = DateTime.UtcNow;
            row.UpdateBy = operatorUserId;
            sort = Math.Min(999, sort + 10);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteBoardAsync(int type, string operatorUserId, CancellationToken ct = default)
    {
        if (!BbsSubjectTypes.IsAllowed(type))
            throw new ArgumentException("主题类型无效");
        if (!BbsSubjectTypes.SupportsBoardModerator(type))
            throw new ArgumentException("该板块不可删除");

        var count = await CountSubjectsByTypeAsync(type, ct);
        if (count > 0)
            throw new InvalidOperationException("板块内仍有帖子，无法删除");

        var existing = await _db.BbsBoardModerators
            .FirstOrDefaultAsync(x => x.SubjectType == type, ct);
        if (existing == null)
        {
            _db.BbsBoardModerators.Add(new BbsBoardModerator
            {
                SubjectType = type,
                UserId = null,
                DisplayName = null,
                IsDeleted = true,
                UpdateTime = DateTime.UtcNow,
                UpdateBy = operatorUserId
            });
        }
        else
        {
            if (existing.IsDeleted) return;
            existing.IsDeleted = true;
            existing.UserId = null;
            existing.UpdateTime = DateTime.UtcNow;
            existing.UpdateBy = operatorUserId;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> CountSubjectsByTypeAsync(int type, CancellationToken ct) =>
        await _db.BbsSubjects.AsNoTracking().CountAsync(x => !x.IsDeleted && x.Type == type, ct);

    private static BbsBoardModeratorDto ToBoardDto(
        int type,
        BbsBoardModerator? row,
        IReadOnlyDictionary<string, (string? UserName, string? RealName)> users)
    {
        var defaultName = BbsSubjectTypes.ToLabel(type);
        var customName = string.IsNullOrWhiteSpace(row?.DisplayName) ? null : row!.DisplayName!.Trim();
        var dto = new BbsBoardModeratorDto
        {
            Type = type,
            DefaultName = defaultName,
            DisplayName = customName,
            IsDeleted = row?.IsDeleted == true,
            SortOrder = row != null && row.SortOrder > 0
                ? row.SortOrder
                : BbsSubjectTypes.DefaultSortOrder(type)
        };
        // 自定义板块默认展示名用 DisplayName
        if (BbsSubjectTypes.IsCustom(type) && !string.IsNullOrWhiteSpace(customName))
            dto.DefaultName = customName;
        if (row == null || string.IsNullOrWhiteSpace(row.UserId) || row.IsDeleted) return dto;
        dto.UserId = row.UserId;
        if (users.TryGetValue(row.UserId, out var u))
        {
            dto.UserName = u.UserName;
            dto.RealName = u.RealName;
        }
        return dto;
    }

    private async Task<IReadOnlyDictionary<int, string>> LoadBoardDisplayNamesAsync(CancellationToken ct)
    {
        var rows = await _db.BbsBoardModerators.AsNoTracking()
            .Where(x => !x.IsDeleted && x.DisplayName != null && x.DisplayName != "")
            .Select(x => new { x.SubjectType, x.DisplayName })
            .ToListAsync(ct);
        return rows.ToDictionary(
            x => x.SubjectType,
            x => x.DisplayName!.Trim(),
            EqualityComparer<int>.Default);
    }

    private static string ResolveTypeLabel(int type, IReadOnlyDictionary<int, string>? customNames)
    {
        if (customNames != null && customNames.TryGetValue(type, out var n) && !string.IsNullOrWhiteSpace(n))
            return n;
        return BbsSubjectTypes.ToLabel(type);
    }

    public async Task<IReadOnlyList<BbsSubjectListItemDto>> GetTopSubjectsAsync(
        BbsActorContext actor,
        CancellationToken ct = default)
    {
        var list = await _db.BbsSubjects.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsTop)
            .OrderByDescending(x => x.CreateTime)
            .Take(50)
            .ToListAsync(ct);
        var names = await ResolveUserNamesAsync(list.Select(x => x.CreateBy), ct);
        var mine = await LoadMyReactionsAsync(BbsReactionTargetTypes.Subject, list.Select(x => x.Id), actor.UserId, ct);
        var typeNames = await LoadBoardDisplayNamesAsync(ct);
        return list.Select(x => ToListItem(x, actor, names, mine, typeNames)).ToList();
    }

    public async Task<BbsBoardStatsDto> GetBoardStatsAsync(CancellationToken ct = default)
    {
        var rows = await _db.BbsSubjects.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => new { x.Type, x.IsTop, x.ViewCount })
            .ToListAsync(ct);

        var all = new BbsBoardStatItemDto
        {
            Key = "all",
            SubjectCount = rows.Count,
            ViewCount = rows.Sum(x => x.ViewCount)
        };
        var topRows = rows.Where(x => x.IsTop).ToList();
        var top = new BbsBoardStatItemDto
        {
            Key = "top",
            SubjectCount = topRows.Count,
            ViewCount = topRows.Sum(x => x.ViewCount)
        };
        var byType = BbsSubjectTypes.All
            .Concat(rows.Select(x => x.Type))
            .Concat(
                (await _db.BbsBoardModerators.AsNoTracking()
                    .Where(x => !x.IsDeleted && x.SubjectType >= BbsSubjectTypes.CustomMin)
                    .Select(x => x.SubjectType)
                    .ToListAsync(ct)))
            .Distinct()
            .OrderBy(t => t)
            .Select(t =>
            {
                var part = rows.Where(x => x.Type == t).ToList();
                return new BbsBoardStatItemDto
                {
                    Key = t.ToString(),
                    Type = t,
                    SubjectCount = part.Count,
                    ViewCount = part.Sum(x => x.ViewCount)
                };
            })
            .ToList();

        return new BbsBoardStatsDto
        {
            All = all,
            Top = top,
            ByType = byType
        };
    }

    public async Task<BbsSubjectPagedDto> QuerySubjectsAsync(
        BbsSubjectQuery query,
        BbsActorContext actor,
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
        var mine = await LoadMyReactionsAsync(BbsReactionTargetTypes.Subject, list.Select(x => x.Id), actor.UserId, ct);
        var typeNames = await LoadBoardDisplayNamesAsync(ct);
        return new BbsSubjectPagedDto
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = list.Select(x => ToListItem(x, actor, names, mine, typeNames)).ToList()
        };
    }

    public async Task<BbsSubjectDetailDto?> GetSubjectDetailAsync(
        string subjectId,
        BbsActorContext actor,
        CancellationToken ct = default)
    {
        var entity = await _db.BbsSubjects
            .FirstOrDefaultAsync(x => x.Id == subjectId && !x.IsDeleted, ct);
        if (entity == null) return null;

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE public.bbs_subject SET view_count = view_count + 1 WHERE id = {subjectId} AND is_deleted = false",
            ct);
        entity.ViewCount += 1;

        return await BuildDetailDtoAsync(entity, actor, ct);
    }

    public async Task<BbsSubjectDetailDto> CreateSubjectAsync(
        BbsSubjectCreateRequest request,
        BbsActorContext actor,
        CancellationToken ct = default)
    {
        ValidateContent(request.Title, request.Content, request.Type);
        await EnsureCanPostTypeAsync(request.Type, actor, ct);

        var kind = request.Kind;
        if (!BbsSubjectKinds.IsValid(kind))
            throw new ArgumentException("主题形态无效");

        var entity = new BbsSubject
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            Type = request.Type,
            Status = BbsSubjectStatuses.Open,
            Anonymous = request.Anonymous,
            Kind = kind,
            CreateTime = DateTime.UtcNow,
            CreateBy = actor.UserId
        };

        if (kind == BbsSubjectKinds.Poll)
        {
            var (mode, maxChoices, deadline, options) = NormalizePollCreate(request);
            entity.VoteMode = mode;
            entity.VoteMaxChoices = maxChoices;
            entity.VoteDeadline = deadline;
            entity.VoteCount = 0;
            for (var i = 0; i < options.Count; i++)
            {
                _db.BbsPollOptions.Add(new BbsPollOption
                {
                    Id = Guid.NewGuid().ToString(),
                    SubjectId = entity.Id,
                    SortOrder = i + 1,
                    Text = options[i],
                    IsDeleted = false
                });
            }
        }

        _db.BbsSubjects.Add(entity);
        await _db.SaveChangesAsync(ct);

        var detail = await GetSubjectDetailWithoutBumpAsync(entity.Id, actor, ct);
        return detail!;
    }

    public async Task<BbsSubjectDetailDto> UpdateSubjectAsync(
        string subjectId,
        BbsSubjectUpdateRequest request,
        BbsActorContext actor,
        CancellationToken ct = default)
    {
        ValidateContent(request.Title, request.Content, request.Type);
        await EnsureCanPostTypeAsync(request.Type, actor, ct);
        var entity = await RequireSubjectAsync(subjectId, ct);
        EnsureNotSystemPost(entity);
        EnsureOwnerOrGlobalModerator(entity, actor);

        entity.Title = request.Title.Trim();
        entity.Content = request.Content.Trim();
        entity.Type = request.Type;
        entity.Anonymous = request.Anonymous;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyBy = actor.UserId;

        if (entity.Kind == BbsSubjectKinds.Poll)
        {
            var hasVotes = entity.VoteCount > 0
                || await _db.BbsPollVotes.AsNoTracking().AnyAsync(x => x.SubjectId == entity.Id, ct);
            if (!hasVotes)
            {
                if (request.PollOptions is { Count: > 0 })
                {
                    var stub = new BbsSubjectCreateRequest
                    {
                        Kind = BbsSubjectKinds.Poll,
                        VoteMode = request.VoteMode ?? entity.VoteMode,
                        VoteMaxChoices = request.VoteMaxChoices,
                        VoteDeadline = request.VoteDeadline ?? entity.VoteDeadline,
                        PollOptions = request.PollOptions
                    };
                    var (mode, maxChoices, deadline, options) = NormalizePollCreate(stub);
                    entity.VoteMode = mode;
                    entity.VoteMaxChoices = maxChoices;
                    entity.VoteDeadline = deadline;
                    var oldOpts = await _db.BbsPollOptions
                        .Where(x => x.SubjectId == entity.Id && !x.IsDeleted)
                        .ToListAsync(ct);
                    foreach (var o in oldOpts) o.IsDeleted = true;
                    for (var i = 0; i < options.Count; i++)
                    {
                        _db.BbsPollOptions.Add(new BbsPollOption
                        {
                            Id = Guid.NewGuid().ToString(),
                            SubjectId = entity.Id,
                            SortOrder = i + 1,
                            Text = options[i],
                            IsDeleted = false
                        });
                    }
                }
                else
                {
                    if (request.VoteMode is int vm && BbsVoteModes.IsValid(vm))
                        entity.VoteMode = vm;
                    if (request.VoteMaxChoices.HasValue)
                    {
                        var optCount = await _db.BbsPollOptions.CountAsync(
                            x => x.SubjectId == entity.Id && !x.IsDeleted, ct);
                        entity.VoteMaxChoices = NormalizeMaxChoices(
                            entity.VoteMode, request.VoteMaxChoices, optCount);
                    }
                    if (request.VoteDeadline.HasValue)
                        entity.VoteDeadline = ToUtcDeadline(request.VoteDeadline);
                }
            }
        }

        await _db.SaveChangesAsync(ct);

        return (await GetSubjectDetailWithoutBumpAsync(subjectId, actor, ct))!;
    }

    public async Task<BbsSubjectDetailDto> VotePollAsync(
        string subjectId,
        IReadOnlyList<string> optionIds,
        BbsActorContext actor,
        CancellationToken ct = default)
    {
        var entity = await RequireSubjectAsync(subjectId, ct);
        if (entity.Kind != BbsSubjectKinds.Poll)
            throw new ArgumentException("该主题不是投票帖");
        if (entity.Status != BbsSubjectStatuses.Open)
            throw new InvalidOperationException("主题已关闭，无法投票");
        if (IsVoteDeadlinePassed(entity.VoteDeadline))
            throw new InvalidOperationException("投票已截止");

        var exists = await _db.BbsPollVotes.AsNoTracking()
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == actor.UserId, ct);
        if (exists)
            throw new InvalidOperationException("您已投过票，不可修改");

        var options = await _db.BbsPollOptions.AsNoTracking()
            .Where(x => x.SubjectId == subjectId && !x.IsDeleted)
            .ToListAsync(ct);
        if (options.Count < BbsPollLimits.MinOptions)
            throw new InvalidOperationException("投票选项无效");

        var ids = (optionIds ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count == 0)
            throw new ArgumentException("请选择投票选项");

        var optionMap = options.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids)
        {
            if (!optionMap.ContainsKey(id))
                throw new ArgumentException("选项无效或不属于本投票");
        }

        if (entity.VoteMode == BbsVoteModes.Single && ids.Count != 1)
            throw new ArgumentException("单选投票只能选择 1 项");
        if (entity.VoteMode == BbsVoteModes.Multi)
        {
            var max = entity.VoteMaxChoices is int m && m > 0 ? m : options.Count;
            if (ids.Count > max)
                throw new ArgumentException($"最多可选 {max} 项");
        }

        var vote = new BbsPollVote
        {
            Id = Guid.NewGuid().ToString(),
            SubjectId = subjectId,
            UserId = actor.UserId,
            CreateTime = DateTime.UtcNow
        };
        _db.BbsPollVotes.Add(vote);
        foreach (var id in ids)
        {
            _db.BbsPollVoteItems.Add(new BbsPollVoteItem
            {
                Id = Guid.NewGuid().ToString(),
                VoteId = vote.Id,
                OptionId = optionMap[id].Id
            });
        }
        entity.VoteCount += 1;
        entity.ModifyTime = DateTime.UtcNow;
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("您已投过票，不可修改");
        }

        return (await GetSubjectDetailWithoutBumpAsync(subjectId, actor, ct))!;
    }

    public async Task CloseSubjectAsync(string subjectId, BbsActorContext actor, CancellationToken ct = default)
    {
        var entity = await RequireSubjectAsync(subjectId, ct);
        EnsureNotSystemPost(entity);
        EnsureOwnerOrBoardModerator(entity, actor);
        entity.Status = BbsSubjectStatuses.Close;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyBy = actor.UserId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task OpenSubjectAsync(string subjectId, BbsActorContext actor, CancellationToken ct = default)
    {
        var entity = await RequireSubjectAsync(subjectId, ct);
        EnsureNotSystemPost(entity);
        EnsureOwnerOrBoardModerator(entity, actor);
        entity.Status = BbsSubjectStatuses.Open;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyBy = actor.UserId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetTopAsync(string subjectId, BbsActorContext actor, CancellationToken ct = default)
    {
        var entity = await RequireSubjectAsync(subjectId, ct);
        EnsureNotSystemPost(entity);
        if (!actor.CanModerateType(entity.Type))
            throw new UnauthorizedAccessException("仅版主可置顶");
        entity.IsTop = true;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyBy = actor.UserId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CancelTopAsync(string subjectId, BbsActorContext actor, CancellationToken ct = default)
    {
        var entity = await RequireSubjectAsync(subjectId, ct);
        EnsureNotSystemPost(entity);
        if (!actor.CanModerateType(entity.Type))
            throw new UnauthorizedAccessException("仅版主可取消置顶");
        entity.IsTop = false;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyBy = actor.UserId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteSubjectAsync(string subjectId, BbsActorContext actor, CancellationToken ct = default)
    {
        var entity = await RequireSubjectAsync(subjectId, ct);
        EnsureNotSystemPost(entity);
        EnsureOwnerOrBoardModerator(entity, actor);
        entity.IsDeleted = true;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyBy = actor.UserId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<BbsReplyPagedDto> GetRepliesAsync(
        string subjectId,
        int page,
        int pageSize,
        BbsActorContext actor,
        CancellationToken ct = default)
    {
        var subject = await RequireSubjectAsync(subjectId, ct);
        var canMod = actor.CanModerateType(subject.Type);
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
            Items = list.Select(x => ToReplyDto(x, actor.UserId, canMod, names)).ToList()
        };
    }

    public async Task<BbsReplyDto> AddReplyAsync(
        string subjectId,
        BbsReplyCreateRequest request,
        BbsActorContext actor,
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
            CreateBy = actor.UserId
        };
        _db.BbsReplies.Add(reply);

        subject.ReplyCount += 1;
        subject.LastReplyTime = DateTime.UtcNow;
        if (subject.ReplyCount > BbsLimits.HotReplyThreshold)
            subject.IsHot = true;
        subject.ModifyTime = DateTime.UtcNow;
        subject.ModifyBy = actor.UserId;

        await _db.SaveChangesAsync(ct);

        var names = await ResolveUserNamesAsync(new[] { actor.UserId }, ct);
        return ToReplyDto(reply, actor.UserId, actor.CanModerateType(subject.Type), names);
    }

    public async Task DeleteReplyAsync(string replyId, BbsActorContext actor, CancellationToken ct = default)
    {
        var reply = await _db.BbsReplies.FirstOrDefaultAsync(x => x.Id == replyId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("没有找到回复");

        var subject = await _db.BbsSubjects.FirstOrDefaultAsync(x => x.Id == reply.SubjectId && !x.IsDeleted, ct);
        var canMod = subject != null && actor.CanModerateType(subject.Type);
        if (!canMod && !string.Equals(reply.CreateBy, actor.UserId, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("无权删除该回复");

        reply.IsDeleted = true;
        reply.ModifyTime = DateTime.UtcNow;
        reply.ModifyBy = actor.UserId;

        if (subject != null && subject.ReplyCount > 0)
            subject.ReplyCount -= 1;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<BbsMediaItemDto>> ListSubjectMediaAsync(
        string subjectId,
        CancellationToken ct = default)
    {
        _ = await RequireSubjectAsync(subjectId, ct);
        var docs = await _documents.GetByBizAsync(BbsDocumentBizTypes.Subject, subjectId);
        return docs.Select(ToMediaItem).ToList();
    }

    public async Task<IReadOnlyList<BbsMediaItemDto>> UploadSubjectMediaAsync(
        string subjectId,
        IReadOnlyList<BbsMediaUploadFile> files,
        BbsActorContext actor,
        CancellationToken ct = default)
    {
        if (files == null || files.Count == 0)
            throw new ArgumentException("请选择至少一个文件");

        var subject = await RequireSubjectAsync(subjectId, ct);
        EnsureNotSystemPost(subject);
        EnsureOwnerOrBoardModerator(subject, actor);

        var existing = await _documents.GetByBizAsync(BbsDocumentBizTypes.Subject, subjectId);
        var imageCount = existing.Count(d => BbsMediaLimits.IsImage(d.FileExtension));
        var videoCount = existing.Count(d => BbsMediaLimits.IsVideo(d.FileExtension));

        var uploadFiles = new List<DocumentUploadFile>();
        foreach (var f in files)
        {
            var ext = Path.GetExtension(f.FileName ?? "").ToLowerInvariant();
            if (BbsMediaLimits.IsImage(ext))
            {
                if (f.Length > BbsMediaLimits.MaxImageBytes)
                    throw new ArgumentException($"图片「{f.FileName}」超过 {BbsMediaLimits.MaxImageBytes / (1024 * 1024)}MB");
                imageCount++;
                if (imageCount > BbsMediaLimits.MaxImagesPerSubject)
                    throw new ArgumentException($"每个主题最多 {BbsMediaLimits.MaxImagesPerSubject} 张图片");
            }
            else if (BbsMediaLimits.IsVideo(ext))
            {
                if (f.Length > BbsMediaLimits.MaxVideoBytes)
                    throw new ArgumentException($"视频「{f.FileName}」超过 {BbsMediaLimits.MaxVideoBytes / (1024 * 1024)}MB");
                videoCount++;
                if (videoCount > BbsMediaLimits.MaxVideosPerSubject)
                    throw new ArgumentException($"每个主题最多 {BbsMediaLimits.MaxVideosPerSubject} 个视频");
            }
            else
            {
                throw new ArgumentException($"不支持的文件格式: {ext}（图片 jpg/png/webp/gif，视频 mp4/webm）");
            }

            uploadFiles.Add(new DocumentUploadFile
            {
                Stream = f.Stream,
                FileName = f.FileName,
                ContentType = f.ContentType
            });
        }

        var saved = await _documents.UploadAsync(new DocumentUploadRequest
        {
            BizType = BbsDocumentBizTypes.Subject,
            BizId = subjectId,
            UploadUserId = actor.UserId,
            Remark = "bbs-media",
            Files = uploadFiles
        });
        return saved.Select(ToMediaItem).ToList();
    }

    public async Task DeleteSubjectMediaAsync(
        string documentId,
        BbsActorContext actor,
        CancellationToken ct = default)
    {
        var doc = await _documents.GetByIdAsync(documentId)
            ?? throw new KeyNotFoundException("媒体不存在");
        if (doc.IsDeleted || !BbsDocumentBizTypes.IsSubject(doc.BizType))
            throw new KeyNotFoundException("媒体不存在");

        var subject = await RequireSubjectAsync(doc.BizId, ct);
        EnsureOwnerOrBoardModerator(subject, actor);
        await _documents.SoftDeleteAsync(documentId, actor.UserId);
    }

    public async Task<BbsReactionResultDto> SetSubjectReactionAsync(
        string subjectId,
        int value,
        string userId,
        CancellationToken ct = default)
    {
        var subject = await RequireSubjectAsync(subjectId, ct);
        return await ApplyReactionAsync(
            BbsReactionTargetTypes.Subject,
            subjectId,
            value,
            userId,
            () => (subject.LikeCount, subject.DislikeCount),
            (like, dislike) =>
            {
                subject.LikeCount = like;
                subject.DislikeCount = dislike;
            },
            ct);
    }

    private async Task<BbsReactionResultDto> ApplyReactionAsync(
        int targetType,
        string targetId,
        int value,
        string userId,
        Func<(int Like, int Dislike)> readCounts,
        Action<int, int> writeCounts,
        CancellationToken ct)
    {
        if (!BbsReactionValues.IsValidRequest(value))
            throw new ArgumentException("赞踩取值无效");

        var existing = await _db.BbsReactions
            .FirstOrDefaultAsync(
                x => x.TargetType == targetType && x.TargetId == targetId && x.UserId == userId,
                ct);

        var (like, dislike) = readCounts();
        var prev = existing?.Value ?? BbsReactionValues.None;
        var next = value;

        if (prev == next)
            next = BbsReactionValues.None;

        if (prev == BbsReactionValues.Like) like = Math.Max(0, like - 1);
        if (prev == BbsReactionValues.Dislike) dislike = Math.Max(0, dislike - 1);
        if (next == BbsReactionValues.Like) like += 1;
        if (next == BbsReactionValues.Dislike) dislike += 1;

        if (next == BbsReactionValues.None)
        {
            if (existing != null) _db.BbsReactions.Remove(existing);
        }
        else if (existing == null)
        {
            _db.BbsReactions.Add(new BbsReaction
            {
                Id = Guid.NewGuid().ToString(),
                TargetType = targetType,
                TargetId = targetId,
                UserId = userId,
                Value = next,
                CreateTime = DateTime.UtcNow
            });
        }
        else
        {
            existing.Value = next;
            existing.ModifyTime = DateTime.UtcNow;
        }

        writeCounts(like, dislike);
        await _db.SaveChangesAsync(ct);
        return new BbsReactionResultDto
        {
            LikeCount = like,
            DislikeCount = dislike,
            MyReaction = next
        };
    }

    private async Task<Dictionary<string, int>> LoadMyReactionsAsync(
        int targetType,
        IEnumerable<string> targetIds,
        string userId,
        CancellationToken ct)
    {
        var ids = targetIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (ids.Count == 0 || string.IsNullOrWhiteSpace(userId))
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var rows = await _db.BbsReactions.AsNoTracking()
            .Where(x => x.TargetType == targetType && x.UserId == userId && ids.Contains(x.TargetId))
            .Select(x => new { x.TargetId, x.Value })
            .ToListAsync(ct);
        return rows.ToDictionary(x => x.TargetId, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static BbsMediaItemDto ToMediaItem(CRM.Core.Models.Document.UploadDocument d)
    {
        var ext = d.FileExtension ?? Path.GetExtension(d.OriginalFileName ?? "");
        var kind = BbsMediaLimits.IsVideo(ext) ? "video" : "image";
        return new BbsMediaItemDto
        {
            Id = d.Id,
            OriginalFileName = d.OriginalFileName,
            MimeType = d.MimeType,
            FileExtension = d.FileExtension,
            FileSize = d.FileSize,
            Kind = kind,
            PreviewPath = $"/api/v1/documents/{d.Id}/preview"
        };
    }

    private async Task<BbsSubjectDetailDto?> GetSubjectDetailWithoutBumpAsync(
        string subjectId,
        BbsActorContext actor,
        CancellationToken ct)
    {
        var entity = await _db.BbsSubjects.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == subjectId && !x.IsDeleted, ct);
        if (entity == null) return null;
        return await BuildDetailDtoAsync(entity, actor, ct);
    }

    private async Task<BbsSubjectDetailDto> BuildDetailDtoAsync(
        BbsSubject entity,
        BbsActorContext actor,
        CancellationToken ct)
    {
        var names = await ResolveUserNamesAsync(new[] { entity.CreateBy }, ct);
        var mine = await LoadMyReactionsAsync(BbsReactionTargetTypes.Subject, new[] { entity.Id }, actor.UserId, ct);
        var typeNames = await LoadBoardDisplayNamesAsync(ct);
        var item = ToListItem(entity, actor, names, mine, typeNames);
        var detail = new BbsSubjectDetailDto
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
            LikeCount = item.LikeCount,
            DislikeCount = item.DislikeCount,
            MyReaction = item.MyReaction,
            LastReplyTime = item.LastReplyTime,
            CreateTime = item.CreateTime,
            CreateBy = item.CreateBy,
            AuthorDisplay = item.AuthorDisplay,
            CanDelete = item.CanDelete,
            CanEdit = item.CanEdit,
            CanSetTop = item.CanSetTop,
            CanModerate = item.CanModerate,
            IsSystem = item.IsSystem,
            Kind = item.Kind,
            VoteCount = item.VoteCount,
            VoteDeadline = item.VoteDeadline,
            Content = entity.Content
        };
        if (entity.Kind == BbsSubjectKinds.Poll)
            detail.Poll = await BuildPollDtoAsync(entity, actor, ct);
        return detail;
    }

    private async Task<BbsSubject> RequireSubjectAsync(string subjectId, CancellationToken ct)
    {
        return await _db.BbsSubjects.FirstOrDefaultAsync(x => x.Id == subjectId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("没有找到主题");
    }

    private static void EnsureOwnerOrBoardModerator(BbsSubject entity, BbsActorContext actor)
    {
        if (actor.CanModerateType(entity.Type)) return;
        if (string.Equals(entity.CreateBy, actor.UserId, StringComparison.OrdinalIgnoreCase)) return;
        throw new UnauthorizedAccessException("无权操作该主题");
    }

    private static void EnsureOwnerOrGlobalModerator(BbsSubject entity, BbsActorContext actor)
    {
        if (actor.IsGlobalModerator) return;
        if (string.Equals(entity.CreateBy, actor.UserId, StringComparison.OrdinalIgnoreCase)) return;
        throw new UnauthorizedAccessException("无权编辑该主题");
    }

    private async Task BumpCollidingSortOrdersAsync(
        int type,
        int order,
        string operatorUserId,
        CancellationToken ct)
    {
        var slot = order;
        while (true)
        {
            var occupant = await _db.BbsBoardModerators
                .FirstOrDefaultAsync(
                    x => x.SubjectType != type && !x.IsDeleted && x.SortOrder == slot,
                    ct);
            if (occupant == null) break;
            if (occupant.SortOrder >= 999) break;
            slot = occupant.SortOrder + 1;
            occupant.SortOrder = slot;
            occupant.UpdateTime = DateTime.UtcNow;
            occupant.UpdateBy = operatorUserId;
        }
    }

    private async Task EnsureCanPostTypeAsync(int type, BbsActorContext actor, CancellationToken ct)
    {
        if (!BbsSubjectTypes.IsAllowed(type))
            throw new ArgumentException("主题类型无效");
        if (BbsSubjectTypes.IsAdminOnlyPostType(type) && !actor.IsSysAdmin)
            throw new UnauthorizedAccessException("仅系统管理员可在该板块发帖");

        if (BbsSubjectTypes.IsCustom(type))
        {
            var row = await _db.BbsBoardModerators.AsNoTracking()
                .FirstOrDefaultAsync(x => x.SubjectType == type, ct);
            if (row == null || row.IsDeleted)
                throw new ArgumentException("该板块不存在或已删除，无法发帖");
            return;
        }

        var deleted = await _db.BbsBoardModerators.AsNoTracking()
            .AnyAsync(x => x.SubjectType == type && x.IsDeleted, ct);
        if (deleted)
            throw new ArgumentException("该板块已删除，无法发帖");
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
        var accounts = await LoadUserAccountsAsync(
            userIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!),
            ct);
        return accounts.ToDictionary(
            kv => kv.Key,
            kv => string.IsNullOrWhiteSpace(kv.Value.RealName)
                ? (kv.Value.UserName ?? kv.Key)
                : kv.Value.RealName!,
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<string, (string? UserName, string? RealName)>> LoadUserAccountsAsync(
        IEnumerable<string> userIds,
        CancellationToken ct)
    {
        var ids = userIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count == 0)
            return new Dictionary<string, (string?, string?)>(StringComparer.OrdinalIgnoreCase);

        var users = await _db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.RealName })
            .ToListAsync(ct);

        return users.ToDictionary(
            u => u.Id,
            u => (u.UserName, u.RealName),
            StringComparer.OrdinalIgnoreCase);
    }

    private static BbsSubjectListItemDto ToListItem(
        BbsSubject x,
        BbsActorContext actor,
        IReadOnlyDictionary<string, string> names,
        IReadOnlyDictionary<string, int> myReactions,
        IReadOnlyDictionary<int, string>? typeNames = null)
    {
        var rawName = !string.IsNullOrWhiteSpace(x.CreateBy) && names.TryGetValue(x.CreateBy, out var n)
            ? n
            : (x.CreateBy ?? "");
        var isOwner = string.Equals(x.CreateBy, actor.UserId, StringComparison.OrdinalIgnoreCase);
        var canMod = actor.CanModerateType(x.Type);
        var locked = x.IsSystem;
        myReactions.TryGetValue(x.Id, out var myReaction);
        return new BbsSubjectListItemDto
        {
            Id = x.Id,
            Title = x.Title,
            Type = x.Type,
            TypeLabel = ResolveTypeLabel(x.Type, typeNames),
            Status = x.Status,
            StatusLabel = BbsSubjectStatuses.ToLabel(x.Status),
            IsTop = x.IsTop,
            IsHot = x.IsHot,
            Anonymous = x.Anonymous,
            ViewCount = x.ViewCount,
            ReplyCount = x.ReplyCount,
            LikeCount = x.LikeCount,
            DislikeCount = x.DislikeCount,
            MyReaction = myReaction,
            LastReplyTime = x.LastReplyTime,
            CreateTime = x.CreateTime,
            CreateBy = x.CreateBy,
            AuthorDisplay = locked
                ? BbsSystemPosts.AuthorName
                : FormatAuthor(rawName, x.Anonymous, canMod),
            CanDelete = !locked && (canMod || isOwner),
            CanEdit = !locked && (actor.IsGlobalModerator || isOwner),
            CanSetTop = !locked && canMod,
            CanModerate = canMod,
            IsSystem = locked,
            Kind = x.Kind,
            VoteCount = x.VoteCount,
            VoteDeadline = x.VoteDeadline
        };
    }

    private async Task<BbsPollDto> BuildPollDtoAsync(
        BbsSubject entity,
        BbsActorContext actor,
        CancellationToken ct)
    {
        var options = await _db.BbsPollOptions.AsNoTracking()
            .Where(x => x.SubjectId == entity.Id && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);

        var myVote = await _db.BbsPollVotes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SubjectId == entity.Id && x.UserId == actor.UserId, ct);
        var hasVoted = myVote != null;
        var myOptionIds = Array.Empty<string>();
        if (myVote != null)
        {
            myOptionIds = await _db.BbsPollVoteItems.AsNoTracking()
                .Where(x => x.VoteId == myVote.Id)
                .Select(x => x.OptionId)
                .ToArrayAsync(ct);
        }

        var isOwner = string.Equals(entity.CreateBy, actor.UserId, StringComparison.OrdinalIgnoreCase);
        var canMod = actor.CanModerateType(entity.Type);
        var deadlinePassed = IsVoteDeadlinePassed(entity.VoteDeadline);
        var closed = entity.Status != BbsSubjectStatuses.Open || deadlinePassed;
        var canSeeStats = hasVoted || isOwner || canMod || deadlinePassed;
        var canVote = !hasVoted && !closed;

        Dictionary<string, int>? counts = null;
        if (canSeeStats)
        {
            var optionIds = options.Select(o => o.Id).ToList();
            var rows = await _db.BbsPollVoteItems.AsNoTracking()
                .Where(x => optionIds.Contains(x.OptionId))
                .GroupBy(x => x.OptionId)
                .Select(g => new { OptionId = g.Key, Count = g.Count() })
                .ToListAsync(ct);
            counts = rows.ToDictionary(x => x.OptionId, x => x.Count, StringComparer.OrdinalIgnoreCase);
        }

        var voterCount = entity.VoteCount;
        var selected = new HashSet<string>(myOptionIds, StringComparer.OrdinalIgnoreCase);
        var optionDtos = options.Select(o =>
        {
            var dto = new BbsPollOptionDto
            {
                Id = o.Id,
                Text = o.Text,
                SortOrder = o.SortOrder,
                Selected = selected.Contains(o.Id)
            };
            if (canSeeStats && counts != null)
            {
                var c = counts.GetValueOrDefault(o.Id);
                dto.VoteCount = c;
                dto.Percent = voterCount <= 0 ? 0 : Math.Round(100.0 * c / voterCount, 1);
            }
            return dto;
        }).ToList();

        return new BbsPollDto
        {
            VoteMode = entity.VoteMode,
            VoteMaxChoices = entity.VoteMaxChoices,
            VoteDeadline = entity.VoteDeadline,
            VoterCount = voterCount,
            HasVoted = hasVoted,
            CanVote = canVote,
            CanSeeStats = canSeeStats,
            IsClosedForVote = closed,
            MyOptionIds = myOptionIds,
            Options = optionDtos
        };
    }

    private static (int Mode, int? MaxChoices, DateTime? Deadline, List<string> Options) NormalizePollCreate(
        BbsSubjectCreateRequest request)
    {
        var mode = request.VoteMode ?? BbsVoteModes.Single;
        if (!BbsVoteModes.IsValid(mode))
            throw new ArgumentException("投票模式无效");

        var raw = request.PollOptions ?? [];
        var options = raw
            .Select(x => (x ?? string.Empty).Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (options.Count < BbsPollLimits.MinOptions)
            throw new ArgumentException($"投票至少需要 {BbsPollLimits.MinOptions} 个选项");
        if (options.Count > BbsPollLimits.MaxOptions)
            throw new ArgumentException($"投票最多 {BbsPollLimits.MaxOptions} 个选项");
        foreach (var t in options)
        {
            if (t.Length > BbsPollLimits.OptionTextMaxLength)
                throw new ArgumentException($"选项最长 {BbsPollLimits.OptionTextMaxLength} 字");
        }

        var maxChoices = NormalizeMaxChoices(mode, request.VoteMaxChoices, options.Count);
        var deadline = ToUtcDeadline(request.VoteDeadline);
        if (deadline != null && deadline <= DateTime.UtcNow)
            throw new ArgumentException("截止时间须晚于当前时间");

        return (mode, maxChoices, deadline, options);
    }

    private static int? NormalizeMaxChoices(int mode, int? maxChoices, int optionCount)
    {
        if (mode != BbsVoteModes.Multi) return null;
        if (maxChoices is null or <= 0) return null;
        if (maxChoices < 2 || maxChoices > optionCount)
            throw new ArgumentException($"多选上限须在 2～{optionCount} 之间");
        return maxChoices;
    }

    private static DateTime? ToUtcDeadline(DateTime? deadline)
    {
        if (deadline == null) return null;
        var d = deadline.Value;
        return d.Kind switch
        {
            DateTimeKind.Utc => d,
            DateTimeKind.Local => d.ToUniversalTime(),
            _ => DateTime.SpecifyKind(d, DateTimeKind.Utc)
        };
    }

    private static bool IsVoteDeadlinePassed(DateTime? deadlineUtc)
    {
        if (deadlineUtc == null) return false;
        return DateTime.UtcNow >= deadlineUtc.Value;
    }

    private static BbsReplyDto ToReplyDto(
        BbsReply x,
        string userId,
        bool canModerate,
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
            AuthorDisplay = FormatAuthor(rawName, x.Anonymous, canModerate),
            CanDelete = canModerate || isOwner
        };
    }

    private static void EnsureNotSystemPost(BbsSubject entity)
    {
        if (entity.IsSystem)
            throw new InvalidOperationException("系统发布的帖子不能在页面上修改");
    }

    private static string FormatAuthor(string realName, bool anonymous, bool isModerator)
    {
        if (!anonymous) return string.IsNullOrWhiteSpace(realName) ? "—" : realName;
        if (isModerator) return string.IsNullOrWhiteSpace(realName) ? "【匿名】" : $"{realName}【匿名】";
        return "*****";
    }
}
