using System.Globalization;
using System.Linq;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;
using CRM.Core.Models.System;

namespace CRM.Core.Services
{
    public class SysDictItemAdminService : ISysDictItemAdminService
    {
        private readonly IRepository<SysDictItem> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public SysDictItemAdminService(IRepository<SysDictItem> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rows = await _repository.FindAsync(_ => true);
            return rows
                .Select(r => r.Category)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToList();
        }

        public async Task<SysDictItemAdminPagedDto> ListAsync(SysDictItemAdminQuery query,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cat = string.IsNullOrWhiteSpace(query.Category) ? null : query.Category.Trim();
            var kw = string.IsNullOrWhiteSpace(query.Keyword) ? null : query.Keyword.Trim();
            var active = query.IsActive;
            var seg = (query.BizSegment ?? string.Empty).Trim().ToLowerInvariant();
            var segCustomer = seg == DictBizSegment.Customer;
            var segVendor = seg == DictBizSegment.Vendor;
            var segMaterial = seg == DictBizSegment.Material;
            var segLogistics = seg == DictBizSegment.Logistics;

            // 显式 OR，避免 Contains(静态数组) 在部分 EF/Npgsql 版本下无法翻译 SQL 导致生产 500
            var rows = await _repository.FindAsync(x =>
                (string.IsNullOrEmpty(seg) ||
                 (segCustomer &&
                  (x.Category == DictCategories.CustomerType ||
                   x.Category == DictCategories.CustomerLevel ||
                   x.Category == DictCategories.CustomerIndustry ||
                   x.Category == DictCategories.CustomerTaxRate ||
                   x.Category == DictCategories.CustomerInvoiceType)) ||
                 (segVendor &&
                  (x.Category == DictCategories.VendorIndustry ||
                   x.Category == DictCategories.VendorLevel ||
                   x.Category == DictCategories.VendorIdentity ||
                   x.Category == DictCategories.VendorPaymentMethod)) ||
                 (segMaterial && x.Category == DictCategories.MaterialProductionDate) ||
                 (segLogistics &&
                  (x.Category == DictCategories.LogisticsArrivalMethod ||
                   x.Category == DictCategories.LogisticsExpressMethod))) &&
                (cat == null || x.Category == cat) &&
                (active == null || x.IsActive == active.Value) &&
                (kw == null ||
                 x.ItemCode.Contains(kw) ||
                 x.NameZh.Contains(kw) ||
                 (x.NameEn != null && x.NameEn.Contains(kw))));

            var ordered = rows
                .OrderBy(x => x.Category, StringComparer.Ordinal)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.ItemCode, StringComparer.Ordinal)
                .ToList();

            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 500);
            var total = ordered.Count;
            var slice = ordered.Skip((page - 1) * size).Take(size).Select(ToRowDto).ToList();

            return new SysDictItemAdminPagedDto { Items = slice, Total = total };
        }

        public async Task<SysDictItemAdminRowDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(id)) return null;
            var e = await _repository.GetByIdAsync(id);
            return e == null ? null : ToRowDto(e);
        }

        public async Task<string> GetNextItemCodeAsync(string category, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cat = (category ?? string.Empty).Trim();
            if (cat.Length == 0) return "1";
            var rows = await _repository.FindAsync(x => x.Category == cat);
            return ComputeNextNumericItemCode(rows);
        }

        public async Task<(bool Ok, string? Error)> CreateAsync(CreateSysDictItemDto dto,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var category = (dto.Category ?? string.Empty).Trim();
            var nameZh = (dto.NameZh ?? string.Empty).Trim();

            if (category.Length == 0) return (false, "业务类型不能为空");
            if (nameZh.Length == 0) return (false, "中文名称不能为空");
            if (category.Length > 64) return (false, "业务类型长度不能超过 64");
            if (nameZh.Length > 200) return (false, "中文名称长度不能超过 200");
            var nameEn = string.IsNullOrWhiteSpace(dto.NameEn) ? null : dto.NameEn.Trim();
            if (nameEn != null && nameEn.Length > 200) return (false, "英文名称长度不能超过 200");

            string code;
            for (var attempt = 0; attempt < 16; attempt++)
            {
                var sameCat = (await _repository.FindAsync(x => x.Category == category)).ToList();
                code = ComputeNextNumericItemCode(sameCat);
                if (code.Length > 64) return (false, "选项编码长度将超过上限，请联系管理员");
                if (sameCat.Any(x => string.Equals(x.ItemCode, code, StringComparison.Ordinal)))
                    continue;
                var sort = dto.SortOrder ?? (sameCat.Count == 0 ? 0 : sameCat.Max(x => x.SortOrder) + 1);

                var entity = new SysDictItem
                {
                    Category = category,
                    ItemCode = code,
                    NameZh = nameZh,
                    NameEn = nameEn,
                    SortOrder = sort,
                    IsActive = dto.IsActive
                };

                await _repository.AddAsync(entity);
                await _unitOfWork.SaveChangesAsync();
                return (true, null);
            }

            return (false, "选项编码分配冲突，请稍后重试");
        }

        /// <summary>同类下将 ItemCode 按整数解析后的最大值 +1；无可解析项时返回 "1"。</summary>
        private static string ComputeNextNumericItemCode(IEnumerable<SysDictItem> items)
        {
            long max = 0;
            foreach (var x in items)
            {
                var s = x.ItemCode?.Trim() ?? string.Empty;
                if (s.Length == 0) continue;
                if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > max)
                    max = n;
            }

            if (max >= long.MaxValue - 1) return long.MaxValue.ToString(CultureInfo.InvariantCulture);
            return (max + 1).ToString(CultureInfo.InvariantCulture);
        }

        public async Task<(bool Ok, string? Error)> UpdateAsync(string id, UpdateSysDictItemDto dto,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(id)) return (false, "Id 无效");

            var nameZh = (dto.NameZh ?? string.Empty).Trim();
            if (nameZh.Length == 0) return (false, "中文名称不能为空");
            if (nameZh.Length > 200) return (false, "中文名称长度不能超过 200");
            var nameEn = string.IsNullOrWhiteSpace(dto.NameEn) ? null : dto.NameEn.Trim();
            if (nameEn != null && nameEn.Length > 200) return (false, "英文名称长度不能超过 200");

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return (false, "记录不存在");

            entity.NameZh = nameZh;
            entity.NameEn = nameEn;
            entity.SortOrder = dto.SortOrder;
            entity.IsActive = dto.IsActive;

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Ok, string? Error)> ReorderAsync(ReorderSysDictItemsDto dto,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var category = (dto.Category ?? string.Empty).Trim();
            if (category.Length == 0) return (false, "业务类型不能为空");
            if (dto.OrderedIds == null || dto.OrderedIds.Count == 0) return (false, "顺序列表不能为空");

            var items = (await _repository.FindAsync(x => x.Category == category)).ToList();
            if (items.Count == 0) return (false, "该分类下无数据");

            var dbIds = items.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
            if (dto.OrderedIds.Count != dbIds.Count) return (false, "记录数量与数据库不一致，请刷新后重试");
            foreach (var id in dto.OrderedIds)
            {
                if (string.IsNullOrWhiteSpace(id) || !dbIds.Contains(id))
                    return (false, "顺序列表与数据库不一致，请刷新后重试");
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in dto.OrderedIds)
            {
                if (!seen.Add(id)) return (false, "顺序列表中存在重复 Id");
            }

            var byId = items.ToDictionary(x => x.Id, StringComparer.Ordinal);
            for (var i = 0; i < dto.OrderedIds.Count; i++)
            {
                var ent = byId[dto.OrderedIds[i]];
                ent.SortOrder = i + 1;
                await _repository.UpdateAsync(ent);
            }

            await _unitOfWork.SaveChangesAsync();
            return (true, null);
        }

        private static SysDictItemAdminRowDto ToRowDto(SysDictItem e) => new()
        {
            Id = e.Id,
            Category = e.Category,
            ItemCode = e.ItemCode,
            NameZh = e.NameZh,
            NameEn = e.NameEn,
            SortOrder = e.SortOrder,
            IsActive = e.IsActive,
            CreateTime = e.CreateTime
        };
    }
}
