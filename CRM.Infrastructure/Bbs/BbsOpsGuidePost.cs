using System.Text;
using CRM.Core.Constants;
using CRM.Core.Models.Bbs;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Bbs;

/// <summary>
/// 启动时读取 <c>Bbs/SystemPosts/*.md</c>，按主键幂等写入系统帖。
/// 运维 Bot 新增「操作说明」时只加 Markdown 和配图，不改 C#。
/// </summary>
public static class BbsOpsGuidePost
{
    private static readonly HashSet<string> AllowedBoards = new(StringComparer.OrdinalIgnoreCase)
    {
        "ops-guide"
    };

    /// <summary>已撤回的系统帖。启动时软删，避免生产环境留下文件已删除的帖。</summary>
    private static readonly string[] RetiredPostIds = ["bbs-sys-ops-manual"];

    /// <summary>返回 true 表示本次至少写入了一篇。forceAll 为 false 且正文未变时跳过。</summary>
    public static async Task<bool> EnsureAsync(
        ApplicationDbContext db,
        bool forceAll,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Bbs", "SystemPosts");
        if (!Directory.Exists(dir))
        {
            logger.LogWarning("未找到系统帖目录 {Dir}，跳过系统帖同步", dir);
            return false;
        }

        var wrote = false;
        var now = DateTime.UtcNow;
        foreach (var retiredId in RetiredPostIds)
        {
            var retired = await db.BbsSubjects
                .FirstOrDefaultAsync(x => x.Id == retiredId && !x.IsDeleted, cancellationToken);
            if (retired == null) continue;
            retired.IsDeleted = true;
            retired.IsTop = false;
            retired.ModifyTime = now;
            wrote = true;
        }

        foreach (var file in Directory.EnumerateFiles(dir, "*.md").OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            if (!TryRead(file, out var post, out var error))
            {
                logger.LogWarning("跳过系统帖 {File}：{Error}", Path.GetFileName(file), error);
                continue;
            }

            if (RetiredPostIds.Contains(post.Id))
                continue;

            var entity = await db.BbsSubjects
                .FirstOrDefaultAsync(x => x.Id == post.Id, cancellationToken);
            if (!forceAll && IsCurrent(entity, post))
                continue;

            if (entity == null)
            {
                entity = new BbsSubject
                {
                    Id = post.Id,
                    CreateTime = now,
                    ViewCount = 0,
                    ReplyCount = 0
                };
                db.BbsSubjects.Add(entity);
            }

            entity.Title = post.Title;
            entity.Content = post.Content;
            entity.Type = post.Type;
            entity.Status = BbsSubjectStatuses.Open;
            entity.IsTop = post.IsTop;
            entity.IsSystem = true;
            entity.IsDeleted = false;
            entity.Anonymous = false;
            entity.Kind = BbsSubjectKinds.Normal;
            entity.ModifyTime = now;
            wrote = true;
        }

        if (wrote)
            await db.SaveChangesAsync(cancellationToken);
        return wrote;
    }

    private static bool IsCurrent(BbsSubject? entity, SystemPostFile post)
    {
        if (entity == null) return false;
        return entity.Title == post.Title
            && entity.Content == post.Content
            && entity.Type == post.Type
            && entity.Status == BbsSubjectStatuses.Open
            && entity.IsTop == post.IsTop
            && entity.IsSystem
            && !entity.IsDeleted
            && !entity.Anonymous
            && entity.Kind == BbsSubjectKinds.Normal;
    }

    private static bool TryRead(string file, out SystemPostFile post, out string error)
    {
        post = null!;
        error = "";
        var text = File.ReadAllText(file, Encoding.UTF8).Replace("\r\n", "\n").Replace('\r', '\n');
        if (!text.StartsWith("---\n", StringComparison.Ordinal))
        {
            error = "缺少开头的 --- 头信息";
            return false;
        }

        var end = text.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (end < 0)
        {
            error = "头信息未闭合";
            return false;
        }

        var header = text[4..end];
        var body = text[(end + 5)..].Trim('\n');
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in header.Split('\n'))
        {
            var idx = line.IndexOf(':');
            if (idx <= 0) continue;
            map[line[..idx].Trim()] = line[(idx + 1)..].Trim();
        }

        if (!map.TryGetValue("id", out var id) || !IsSafeId(id))
        {
            error = "id 须为 bbs-sys- 加小写字母、数字或连字符";
            return false;
        }

        if (!map.TryGetValue("title", out var title) || string.IsNullOrWhiteSpace(title) || title.Length > 200)
        {
            error = "title 不能为空且不超过 200 字";
            return false;
        }

        var board = map.TryGetValue("board", out var boardRaw) ? boardRaw : "ops-guide";
        if (!AllowedBoards.Contains(board))
        {
            error = "board 只能是 ops-guide";
            return false;
        }

        var isTop = !map.TryGetValue("top", out var topRaw) || !topRaw.Equals("false", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(body))
        {
            error = "正文为空";
            return false;
        }

        post = new SystemPostFile(id, title, BbsSubjectTypes.OpsGuide, isTop, body);
        return true;
    }

    private static bool IsSafeId(string id)
    {
        if (id.Length is < 9 or > 36 || !id.StartsWith("bbs-sys-", StringComparison.Ordinal))
            return false;
        for (var i = 8; i < id.Length; i++)
        {
            var c = id[i];
            if (c is not (>= 'a' and <= 'z' or >= '0' and <= '9' or '-'))
                return false;
        }
        return true;
    }

    private sealed record SystemPostFile(string Id, string Title, int Type, bool IsTop, string Content);
}
