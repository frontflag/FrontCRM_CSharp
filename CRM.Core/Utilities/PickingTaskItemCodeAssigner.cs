namespace CRM.Core.Utilities;

/// <summary>拣货明细 <c>item_code</c> 分配（方案 B：以装箱明细编号为前缀，拆行加子序号）。</summary>
public static class PickingTaskItemCodeAssigner
{
    /// <summary>
    /// 按保存顺序为每条拣货行分配业务编号。
    /// 有 <paramref name="packingItemId"/> 且能解析到装箱明细编号时：单条同编号，多条为 <c>{packingItemCode}-{子序号}</c>；
    /// 否则回退为 <see cref="OrderLineItemCodes.PickingTaskItem"/>。
    /// </summary>
    public static string[] Assign(
        IReadOnlyList<string?> packingItemIds,
        IReadOnlyDictionary<string, string> packingItemCodeById,
        string? taskCode)
    {
        var result = new string[packingItemIds.Count];
        var fallbackSeq = 0;

        var groups = packingItemIds
            .Select((pid, idx) => (idx, Key: pid?.Trim() ?? string.Empty))
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var g in groups)
        {
            var packingItemId = g.Key;
            var indices = g.Select(x => x.idx).OrderBy(i => i).ToList();

            if (packingItemId.Length > 0
                && packingItemCodeById.TryGetValue(packingItemId, out var baseCode)
                && !string.IsNullOrWhiteSpace(baseCode))
            {
                baseCode = baseCode.Trim();
                if (indices.Count == 1)
                {
                    result[indices[0]] = baseCode;
                }
                else
                {
                    for (var sub = 0; sub < indices.Count; sub++)
                        result[indices[sub]] = $"{baseCode}-{sub + 1}";
                }
            }
            else
            {
                foreach (var idx in indices)
                {
                    fallbackSeq++;
                    result[idx] = OrderLineItemCodes.PickingTaskItem(taskCode, fallbackSeq);
                }
            }
        }

        return result;
    }
}
