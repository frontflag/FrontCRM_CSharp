using CRM.Core.Interfaces;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
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
        private readonly IRepository<RFQItem> _rfqItemRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;

        public QuoteService(
            IRepository<Quote> quoteRepository,
            IRepository<QuoteItem> quoteItemRepository,
            IRepository<RFQItem> rfqItemRepository,
            IUnitOfWork unitOfWork,
            ISerialNumberService serialNumberService)
        {
            _quoteRepository = quoteRepository;
            _quoteItemRepository = quoteItemRepository;
            _rfqItemRepository = rfqItemRepository;
            _unitOfWork = unitOfWork;
            _serialNumberService = serialNumberService;
        }

        public async Task<Quote> CreateAsync(CreateQuoteRequest request)
        {
            // 后端统一生成报价单号（忽略客户端传入）
            var quoteCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.Quotation);

            var quote = new Quote
            {
                Id = Guid.NewGuid().ToString(),
                QuoteCode = quoteCode,
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

            // 创建报价后，回写对应 RFQ 明细为「已报价」(1)
            if (!string.IsNullOrWhiteSpace(request.RFQItemId))
            {
                var rfqItem = await _rfqItemRepository.GetByIdAsync(request.RFQItemId.Trim());
                if (rfqItem != null && rfqItem.Status == 0)
                {
                    rfqItem.Status = 1;
                    rfqItem.ModifyTime = DateTime.UtcNow;
                    await _rfqItemRepository.UpdateAsync(rfqItem);
                }
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
            var quotes = (await _quoteRepository.GetAllAsync()).ToList();
            if (quotes.Count == 0)
                return quotes;

            var quoteIds = quotes.Select(q => q.Id).ToList();
            var itemRows = await _quoteItemRepository.FindAsync(i => quoteIds.Contains(i.QuoteId));
            var byQuoteId = itemRows
                .GroupBy(i => i.QuoteId)
                .ToDictionary(g => g.Key, g => (ICollection<QuoteItem>)g.ToList());

            foreach (var q in quotes)
            {
                q.Items = byQuoteId.TryGetValue(q.Id, out var list) ? list : new List<QuoteItem>();
            }

            return quotes;
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

            // 删除报价后，如果该 RFQ 明细已无任何报价，则回退为「待报价」(0)
            if (!string.IsNullOrWhiteSpace(quote.RFQItemId))
            {
                var rfqItemId = quote.RFQItemId.Trim();
                var remainingQuotes = await _quoteRepository.FindAsync(q => q.RFQItemId == rfqItemId && q.Id != id);
                if (!remainingQuotes.Any())
                {
                    var rfqItem = await _rfqItemRepository.GetByIdAsync(rfqItemId);
                    if (rfqItem != null && rfqItem.Status == 1)
                    {
                        rfqItem.Status = 0;
                        rfqItem.ModifyTime = DateTime.UtcNow;
                        await _rfqItemRepository.UpdateAsync(rfqItem);
                    }
                }
            }

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
