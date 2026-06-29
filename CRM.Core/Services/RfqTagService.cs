using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Tag;

namespace CRM.Core.Services;

/// <summary>需求主表标签应用服务。</summary>
public sealed class RfqTagService : IRfqTagService
{
    private readonly IRepository<RFQ> _rfqRepo;
    private readonly IRepository<TagDefinition> _tagRepo;
    private readonly IRepository<TagRelation> _relationRepo;
    private readonly ITagApplyService _tagApplyService;
    private readonly IDataPermissionService _dataPermissionService;
    private readonly ILogOperationAppendService _logOperationAppend;
    private readonly ITagService _tagService;

    public RfqTagService(
        IRepository<RFQ> rfqRepo,
        IRepository<TagDefinition> tagRepo,
        IRepository<TagRelation> relationRepo,
        ITagApplyService tagApplyService,
        IDataPermissionService dataPermissionService,
        ILogOperationAppendService logOperationAppend,
        ITagService tagService)
    {
        _rfqRepo = rfqRepo;
        _tagRepo = tagRepo;
        _relationRepo = relationRepo;
        _tagApplyService = tagApplyService;
        _dataPermissionService = dataPermissionService;
        _logOperationAppend = logOperationAppend;
        _tagService = tagService;
    }

    public async Task<IReadOnlyList<EntityTagDto>> GetTagsForRfqAsync(string rfqId, string? viewerUserId)
    {
        if (string.IsNullOrWhiteSpace(viewerUserId))
            throw new UnauthorizedAccessException("未登录");
        var rfq = await _rfqRepo.GetByIdAsync(rfqId)
            ?? throw new KeyNotFoundException("需求不存在");
        if (!await _dataPermissionService.CanViewRfqTagsAsync(viewerUserId, rfq.CreateByUserId, rfq.SalesUserId))
            throw new UnauthorizedAccessException("无权查看该需求的标签");

        var tags = await _tagApplyService.GetTagsForEntityAsync(RfqTagConstants.EntityType, rfqId);
        return tags.Select(MapTag).ToList();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<EntityTagDto>>> GetTagsForRfqIdsAsync(
        IEnumerable<string> rfqIds,
        string? viewerUserId,
        IEnumerable<(string RfqId, string? CreateByUserId, string? SalesUserId)> rfqRows)
    {
        var result = new Dictionary<string, IReadOnlyList<EntityTagDto>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(viewerUserId)) return result;

        var visibleIds = new List<string>();
        foreach (var row in rfqRows)
        {
            if (await _dataPermissionService.CanViewRfqTagsAsync(viewerUserId, row.CreateByUserId, row.SalesUserId))
                visibleIds.Add(row.RfqId);
        }

        if (visibleIds.Count == 0) return result;

        var idSet = visibleIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var relations = (await _relationRepo.FindAsync(r =>
            r.EntityType == RfqTagConstants.EntityType && idSet.Contains(r.EntityId))).ToList();
        if (relations.Count == 0) return result;

        var tagIds = relations.Select(r => r.TagId).Distinct().ToList();
        var tags = (await _tagRepo.FindAsync(t => tagIds.Contains(t.Id) && !t.IsDeleted))
            .ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var group in relations.GroupBy(r => r.EntityId, StringComparer.OrdinalIgnoreCase))
        {
            var list = group
                .Select(r => tags.TryGetValue(r.TagId, out var t) ? t : null)
                .Where(t => t != null)
                .Select(t => MapTag(t!))
                .OrderByDescending(t => t.Type)
                .ThenBy(t => t.Name, StringComparer.Ordinal)
                .ToList();
            if (list.Count > 0)
                result[group.Key] = list;
        }

        return result;
    }

    public async Task ApplyTagsAsync(string rfqId, IReadOnlyList<string> tagIds, string actorUserId, string? actorUserName)
    {
        var rfq = await RequireEditableRfqAsync(rfqId, actorUserId);
        var ids = tagIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (ids.Count == 0) return;

        await ValidateTagApplyAsync(ids, actorUserId);

        await _tagApplyService.AddTagsToEntityAsync(new AddTagsToEntityRequest
        {
            EntityType = RfqTagConstants.EntityType,
            EntityIds = new List<string> { rfqId },
            TagIds = ids,
            AppliedByUserId = 0,
            Source = "Manual"
        });

        var names = await ResolveTagNamesAsync(ids);
        await _logOperationAppend.AppendAsync(
            RfqTagConstants.EntityType,
            rfq.Id,
            rfq.RfqCode,
            OperationLogActionTypes.RfqTagApply,
            actorUserId,
            actorUserName,
            $"添加标签：{string.Join("、", names)}");
    }

    public async Task RemoveTagsAsync(string rfqId, IReadOnlyList<string> tagIds, string actorUserId, string? actorUserName)
    {
        var rfq = await RequireEditableRfqAsync(rfqId, actorUserId);
        var ids = tagIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (ids.Count == 0) return;

        await _tagApplyService.RemoveTagsFromEntityAsync(new RemoveTagsFromEntityRequest
        {
            EntityType = RfqTagConstants.EntityType,
            EntityIds = new List<string> { rfqId },
            TagIds = ids,
            AppliedByUserId = 0,
            Source = "Manual"
        });

        var names = await ResolveTagNamesAsync(ids);
        await _logOperationAppend.AppendAsync(
            RfqTagConstants.EntityType,
            rfq.Id,
            rfq.RfqCode,
            OperationLogActionTypes.RfqTagRemove,
            actorUserId,
            actorUserName,
            $"移除标签：{string.Join("、", names)}");
    }

    public Task ValidateCustomTagCreateAsync(string name, string actorUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("标签名称不能为空", nameof(name));
        if (RfqTagConstants.IsReservedSystemTagName(name))
            throw new InvalidOperationException("不能与系统预设标签同名");
        return Task.CompletedTask;
    }

    public async Task<TagDefinition> CreateUserRfqTagAsync(string name, string? color, string actorUserId)
    {
        await ValidateCustomTagCreateAsync(name, actorUserId);
        return await _tagService.CreateTagAsync(new CreateTagRequest
        {
            Name = name.Trim(),
            Code = RfqTagConstants.BuildOwnerCode(actorUserId),
            Color = color,
            Type = 2,
            Scope = RfqTagConstants.EntityType,
            Visibility = 1
        }, 0);
    }

    private async Task<RFQ> RequireEditableRfqAsync(string rfqId, string actorUserId)
    {
        if (string.IsNullOrWhiteSpace(rfqId))
            throw new ArgumentException("需求 ID 不能为空", nameof(rfqId));
        var rfq = await _rfqRepo.GetByIdAsync(rfqId)
            ?? throw new KeyNotFoundException("需求不存在");
        if (!await _dataPermissionService.CanEditRfqTagsAsync(actorUserId, rfq.CreateByUserId, rfq.SalesUserId))
            throw new UnauthorizedAccessException("无权修改该需求的标签");
        return rfq;
    }

    private async Task ValidateTagApplyAsync(IReadOnlyList<string> tagIds, string actorUserId)
    {
        var tags = await _tagRepo.FindAsync(t => tagIds.Contains(t.Id) && !t.IsDeleted);
        var found = tags.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var id in tagIds)
        {
            if (!found.TryGetValue(id, out var tag))
                throw new KeyNotFoundException($"标签不存在：{id}");
            if (tag.Type == 2
                && tag.Scope != null
                && tag.Scope.Contains(RfqTagConstants.EntityType, StringComparison.OrdinalIgnoreCase)
                && !RfqTagConstants.IsOwnedByUser(tag, actorUserId))
                throw new UnauthorizedAccessException($"无权使用该自定义标签：{tag.Name}");
        }
    }

    private async Task<IReadOnlyList<string>> ResolveTagNamesAsync(IReadOnlyList<string> tagIds)
    {
        var tags = await _tagRepo.FindAsync(t => tagIds.Contains(t.Id));
        return tagIds
            .Select(id => tags.FirstOrDefault(t => t.Id == id)?.Name ?? id)
            .ToList();
    }

    private static EntityTagDto MapTag(TagDefinition tag) => new()
    {
        Id = tag.Id,
        Name = tag.Name,
        Color = tag.Color,
        Type = tag.Type
    };
}
