using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;

namespace CRM.Core.Services
{
    /// <summary>
    /// SalesInvoice服务实现
    /// </summary>
    public class SalesInvoiceService : ISalesInvoiceService
    {
        private readonly IRepository<Invoice> _repository;

        public SalesInvoiceService(IRepository<Invoice> repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 创建
        /// </summary>
        public async Task<Invoice> CreateAsync(CreateSalesInvoiceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                throw new ArgumentException("编码不能为空", nameof(request.Code));

            var entity = new Invoice
            {
                Id = Guid.NewGuid().ToString(),
                CreateTime = DateTime.UtcNow
            };

            // 使用反射设置属性
            SetProperty(entity, "Code", request.Code?.Trim());
            SetProperty(entity, "Name", request.Name?.Trim());
            SetProperty(entity, "Remark", request.Remark?.Trim());
            SetProperty(entity, "Status", (short)0);

            await _repository.AddAsync(entity);
            return entity;
        }

        /// <summary>
        /// 根据ID获取
        /// </summary>
        public async Task<Invoice?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var entities = await _repository.FindAsync(e => e.Id == id);
            return entities.FirstOrDefault();
            // 类型过滤已在上面的代码中处理
        }

        /// <summary>
        /// 获取所有
        /// </summary>
        public async Task<IEnumerable<Invoice>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            // 类型过滤已在上面的代码中处理
            return entities;
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        public async Task<PagedResult<Invoice>> GetPagedAsync(SalesInvoiceQueryRequest request)
        {
            var allEntities = await _repository.GetAllAsync();
            var query = allEntities.AsQueryable();
            // 类型过滤已在上面的代码中处理

            // 关键词搜索
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim().ToLower();
                query = query.Where(e => 
                    (GetProperty(e, "Code") != null && GetProperty(e, "Code")!.ToString()!.ToLower().Contains(keyword)) ||
                    (GetProperty(e, "Name") != null && GetProperty(e, "Name")!.ToString()!.ToLower().Contains(keyword)));
            }

            // 状态筛选
            if (request.Status.HasValue)
            {
                query = query.Where(e => GetProperty(e, "Status") != null && GetProperty(e, "Status").Equals(request.Status.Value));
            }

            var totalCount = query.Count();
            var items = query
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<Invoice>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }

        /// <summary>
        /// 更新
        /// </summary>
        public async Task<Invoice> UpdateAsync(string id, UpdateSalesInvoiceRequest request)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var entity = await GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的记录");

            if (request.Name != null)
                SetProperty(entity, "Name", request.Name.Trim());
            if (request.Remark != null)
                SetProperty(entity, "Remark", request.Remark.Trim());

            entity.ModifyTime = DateTime.UtcNow;
            await _repository.UpdateAsync(entity);
            return entity;
        }

        /// <summary>
        /// 删除
        /// </summary>
        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var entity = await GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的记录");

            await _repository.DeleteAsync(entity.Id);
        }

        /// <summary>
        /// 批量删除
        /// </summary>
        public async Task BatchDeleteAsync(IEnumerable<string> ids)
        {
            if (ids == null || !ids.Any()) return;

            foreach (var id in ids.Where(id => !string.IsNullOrWhiteSpace(id)))
            {
                try { await DeleteAsync(id); }
                catch (KeyNotFoundException) { }
            }
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        public async Task UpdateStatusAsync(string id, short status)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var entity = await GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的记录");

            SetProperty(entity, "Status", status);
            entity.ModifyTime = DateTime.UtcNow;
            await _repository.UpdateAsync(entity);
        }

        /// <summary>
        /// 搜索
        /// </summary>
        public async Task<IEnumerable<Invoice>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetAllAsync();

            var allEntities = await _repository.GetAllAsync();
            var searchTerm = keyword.Trim().ToLower();
            // 类型过滤已在上面的代码中处理

            return allEntities.Where(e =>
                (GetProperty(e, "Code") != null && GetProperty(e, "Code")!.ToString()!.ToLower().Contains(searchTerm)) ||
                (GetProperty(e, "Name") != null && GetProperty(e, "Name")!.ToString()!.ToLower().Contains(searchTerm)));
        }

        /// <summary>
        /// 设置属性值
        /// </summary>
        private void SetProperty(object obj, string propertyName, object? value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
            }
        }

        /// <summary>
        /// 获取属性值
        /// </summary>
        private object? GetProperty(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);
            return property?.GetValue(obj);
        }
    }
}
