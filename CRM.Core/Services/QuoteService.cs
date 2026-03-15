using CRM.Core.Interfaces;
using CRM.Core.Models.Quote;

namespace CRM.Core.Services
{
    /// <summary>
    /// 报价服务实现
    /// </summary>
    public class QuoteService : IQuoteService
    {
        private readonly IRepository<Quote> _quoteRepository;

        public QuoteService(IRepository<Quote> quoteRepository)
        {
            _quoteRepository = quoteRepository;
        }

        public async Task<Quote> CreateAsync(CreateQuoteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.QuoteCode))
                throw new ArgumentException("报价单号不能为空", nameof(request.QuoteCode));

            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new ArgumentException("客户ID不能为空", nameof(request.CustomerId));

            // 检查报价单号是否已存在
            var allQuotes = await _quoteRepository.GetAllAsync();
            if (allQuotes.Any(q => q.QuoteCode == request.QuoteCode))
                throw new InvalidOperationException($"报价单号 {request.QuoteCode} 已存在");

            var quote = new Quote
            {
                Id = Guid.NewGuid().ToString(),
                QuoteCode = request.QuoteCode.Trim(),
                QuoteType = 1, // 销售报价
                CustomerId = request.CustomerId,
                SalesUserId = request.SalesUserId,
                PurchaseUserId = request.PurchaseUserId,
                QuoteDate = request.QuoteDate,
                ValidityDate = request.ValidUntil,
                TotalAmount = request.TotalAmount,
                TaxAmount = request.TaxAmount,
                TotalAmountWithTax = request.GrandTotal,
                Status = 0, // 草稿
                CreateTime = DateTime.UtcNow
            };

            await _quoteRepository.AddAsync(quote);
            return quote;
        }

        public async Task<Quote?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _quoteRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Quote>> GetAllAsync()
        {
            return await _quoteRepository.GetAllAsync();
        }

        public async Task<Quote> UpdateAsync(string id, UpdateQuoteRequest request)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var quote = await _quoteRepository.GetByIdAsync(id);
            if (quote == null)
                throw new InvalidOperationException($"报价单 {id} 不存在");

            if (!string.IsNullOrWhiteSpace(request.Remark))
                quote.Remark = request.Remark;

            quote.ModifyTime = DateTime.UtcNow;

            await _quoteRepository.UpdateAsync(quote);
            return quote;
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var quote = await _quoteRepository.GetByIdAsync(id);
            if (quote == null)
                throw new InvalidOperationException($"报价单 {id} 不存在");

            await _quoteRepository.DeleteAsync(id);
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var quote = await _quoteRepository.GetByIdAsync(id);
            if (quote == null)
                throw new InvalidOperationException($"报价单 {id} 不存在");

            quote.Status = status;
            quote.ModifyTime = DateTime.UtcNow;

            await _quoteRepository.UpdateAsync(quote);
        }
    }
}
