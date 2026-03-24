using CRM.Core.Interfaces;
using CRM.Core.Models.Quote;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    /// <summary>
    /// 报价服务实现
    /// </summary>
    public class QuoteService : IQuoteService
    {
        private readonly IRepository<Quote> _quoteRepository;
        private readonly IRepository<QuoteItem> _quoteItemRepository;
        private readonly IUnitOfWork _unitOfWork;

        public QuoteService(
            IRepository<Quote> quoteRepository,
            IRepository<QuoteItem> quoteItemRepository,
            IUnitOfWork unitOfWork)
        {
            _quoteRepository = quoteRepository;
            _quoteItemRepository = quoteItemRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Quote> CreateAsync(CreateQuoteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.QuoteCode))
                throw new ArgumentException("报价单号不能为空", nameof(request.QuoteCode));

            // 检查报价单号是否已存在
            var allQuotes = await _quoteRepository.GetAllAsync();
            if (allQuotes.Any(q => q.QuoteCode == request.QuoteCode))
                throw new InvalidOperationException($"报价单号 {request.QuoteCode} 已存在");

            var quote = new Quote
            {
                Id = Guid.NewGuid().ToString(),
                QuoteCode = request.QuoteCode.Trim(),
                RFQId = request.RFQId,
                RFQItemId = request.RFQItemId,
                Mpn = request.Mpn,
                CustomerId = request.CustomerId,
                SalesUserId = request.SalesUserId,
                PurchaseUserId = request.PurchaseUserId,
                QuoteDate = request.QuoteDate == default ? DateTime.UtcNow : PostgreSqlDateTime.ToUtc(request.QuoteDate),
                Status = request.Status,
                Remark = request.Remark,
                CreateTime = DateTime.UtcNow
            };

            await _quoteRepository.AddAsync(quote);

            // 创建明细行
            foreach (var itemReq in request.Items)
            {
                var item = MapToQuoteItem(quote.Id, itemReq);
                await _quoteItemRepository.AddAsync(item);
            }

            await _unitOfWork.SaveChangesAsync();
            return quote;
        }

        public async Task<Quote?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var quote = await _quoteRepository.GetByIdAsync(id);
            if (quote == null)
                return null;

            var items = await _quoteItemRepository.FindAsync(i => i.QuoteId == id);
            quote.Items = items.ToList();
            return quote;
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

            if (request.Mpn != null) quote.Mpn = request.Mpn;
            if (request.CustomerId != null) quote.CustomerId = request.CustomerId;
            if (request.SalesUserId != null) quote.SalesUserId = request.SalesUserId;
            if (request.PurchaseUserId != null) quote.PurchaseUserId = request.PurchaseUserId;
            if (request.QuoteDate.HasValue) quote.QuoteDate = PostgreSqlDateTime.ToUtc(request.QuoteDate.Value);
            if (request.Status.HasValue) quote.Status = request.Status.Value;
            if (request.Remark != null) quote.Remark = request.Remark;

            quote.ModifyTime = DateTime.UtcNow;

            await _quoteRepository.UpdateAsync(quote);

            // 更新明细行（先删后增）
            if (request.Items != null)
            {
                var existingItems = await _quoteItemRepository.GetAllAsync();
                var toDelete = existingItems.Where(i => i.QuoteId == id).ToList();
                foreach (var item in toDelete)
                    await _quoteItemRepository.DeleteAsync(item.Id);

                foreach (var itemReq in request.Items)
                {
                    var item = MapToQuoteItem(id, itemReq);
                    await _quoteItemRepository.AddAsync(item);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return quote;
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var quote = await _quoteRepository.GetByIdAsync(id);
            if (quote == null)
                throw new InvalidOperationException($"报价单 {id} 不存在");

            // 先删明细行
            var items = await _quoteItemRepository.GetAllAsync();
            foreach (var item in items.Where(i => i.QuoteId == id))
                await _quoteItemRepository.DeleteAsync(item.Id);

            await _quoteRepository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
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
            await _unitOfWork.SaveChangesAsync();
        }

        private static QuoteItem MapToQuoteItem(string quoteId, CreateQuoteItemRequest req)
        {
            return new QuoteItem
            {
                Id = Guid.NewGuid().ToString(),
                QuoteId = quoteId,
                VendorId = req.VendorId,
                VendorName = req.VendorName,
                VendorCode = req.VendorCode,
                ContactId = req.ContactId,
                ContactName = req.ContactName,
                PriceType = req.PriceType,
                ExpiryDate = PostgreSqlDateTime.ToUtc(req.ExpiryDate),
                Mpn = req.Mpn,
                Brand = req.Brand,
                BrandOrigin = req.BrandOrigin,
                DateCode = req.DateCode,
                LeadTime = req.LeadTime,
                LabelType = req.LabelType,
                WaferOrigin = req.WaferOrigin,
                PackageOrigin = req.PackageOrigin,
                FreeShipping = req.FreeShipping,
                Currency = req.Currency,
                Quantity = req.Quantity,
                UnitPrice = req.UnitPrice,
                ConvertedPrice = req.ConvertedPrice,
                MinPackageQty = req.MinPackageQty,
                MinPackageUnit = req.MinPackageUnit,
                StockQty = req.StockQty,
                Moq = req.Moq,
                Remark = req.Remark,
                Status = req.Status,
                CreateTime = DateTime.UtcNow
            };
        }
    }
}
