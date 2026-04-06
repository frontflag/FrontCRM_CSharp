using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;
using CRM.Core.Models.System;

namespace CRM.Core.Services
{
    public class DictionaryService : IDictionaryService
    {
        private readonly IRepository<SysDictItem> _repository;

        public DictionaryService(IRepository<SysDictItem> repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<DictionaryItemDto>> GetItemsAsync(string category, bool preferEnglish,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(category))
                return Array.Empty<DictionaryItemDto>();

            var rows = await _repository.FindAsync(x =>
                x.Category == category && x.IsActive);

            return rows
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.ItemCode)
                .Select(x => ToDto(x, preferEnglish))
                .ToList();
        }

        public async Task<IReadOnlyDictionary<string, IReadOnlyList<DictionaryItemDto>>> GetBatchAsync(
            IReadOnlyList<string> categories, bool preferEnglish, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (categories == null || categories.Count == 0)
                return new Dictionary<string, IReadOnlyList<DictionaryItemDto>>();

            var distinct = categories.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
            if (distinct.Count == 0)
                return new Dictionary<string, IReadOnlyList<DictionaryItemDto>>();

            var all = await _repository.FindAsync(x => distinct.Contains(x.Category) && x.IsActive);

            var map = new Dictionary<string, IReadOnlyList<DictionaryItemDto>>(StringComparer.Ordinal);
            foreach (var cat in distinct)
            {
                var list = all
                    .Where(x => x.Category == cat)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.ItemCode)
                    .Select(x => ToDto(x, preferEnglish))
                    .ToList();
                map[cat] = list;
            }

            return map;
        }

        public async Task<DictionaryItemDto?> LookupByCodeAsync(string category, string code, bool preferEnglish,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(code))
                return null;

            var rows = await _repository.FindAsync(ent =>
                ent.Category == category.Trim() && ent.ItemCode == code.Trim());

            var row = rows.FirstOrDefault();
            return row == null ? null : ToDto(row, preferEnglish);
        }

        private static DictionaryItemDto ToDto(SysDictItem x, bool preferEnglish)
        {
            var label = preferEnglish && !string.IsNullOrWhiteSpace(x.NameEn) ? x.NameEn! : x.NameZh;
            return new DictionaryItemDto { Code = x.ItemCode, Label = label };
        }
    }
}
