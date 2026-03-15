using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;

namespace CRM.Core.Services
{
    /// <summary>
    /// 询价单服务实现
    /// </summary>
    public class RFQService : IRFQService
    {
        private readonly IRepository<RFQ> _rfqRepository;

        public RFQService(IRepository<RFQ> rfqRepository)
        {
            _rfqRepository = rfqRepository;
        }

        public async Task<RFQ> CreateAsync(CreateRFQRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RFQCode))
                throw new ArgumentException("询价单号不能为空", nameof(request.RFQCode));

            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new ArgumentException("客户ID不能为空", nameof(request.CustomerId));

            // 检查询价单号是否已存在
            var allRfqs = await _rfqRepository.GetAllAsync();
            if (allRfqs.Any(r => r.RFQCode == request.RFQCode))
                throw new InvalidOperationException($"询价单号 {request.RFQCode} 已存在");

            var rfq = new RFQ
            {
                Id = Guid.NewGuid().ToString(),
                RFQCode = request.RFQCode.Trim(),
                RFQType = 1, // 客户询价
                CustomerId = request.CustomerId,
                SalesUserId = request.SalesUserId,
                RFQDate = request.RFQDate,
                Status = 0, // 草稿
                CreateTime = DateTime.UtcNow
            };

            await _rfqRepository.AddAsync(rfq);
            return rfq;
        }

        public async Task<RFQ?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _rfqRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<RFQ>> GetAllAsync()
        {
            return await _rfqRepository.GetAllAsync();
        }

        public async Task<IEnumerable<RFQ>> GetByCustomerIdAsync(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return new List<RFQ>();

            var allRfqs = await _rfqRepository.GetAllAsync();
            return allRfqs.Where(r => r.CustomerId == customerId);
        }

        public async Task<RFQ> UpdateAsync(string id, UpdateRFQRequest request)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var rfq = await _rfqRepository.GetByIdAsync(id);
            if (rfq == null)
                throw new InvalidOperationException($"询价单 {id} 不存在");

            if (!string.IsNullOrWhiteSpace(request.Remark))
                rfq.Remark = request.Remark;

            rfq.ModifyTime = DateTime.UtcNow;

            await _rfqRepository.UpdateAsync(rfq);
            return rfq;
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var rfq = await _rfqRepository.GetByIdAsync(id);
            if (rfq == null)
                throw new InvalidOperationException($"询价单 {id} 不存在");

            await _rfqRepository.DeleteAsync(id);
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var rfq = await _rfqRepository.GetByIdAsync(id);
            if (rfq == null)
                throw new InvalidOperationException($"询价单 {id} 不存在");

            rfq.Status = status;
            rfq.ModifyTime = DateTime.UtcNow;

            await _rfqRepository.UpdateAsync(rfq);
        }
    }
}
