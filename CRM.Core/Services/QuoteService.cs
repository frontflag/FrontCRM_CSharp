using CRM.Core.Interfaces;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;

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
        private readonly IRepository<RFQ> _rfqRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IUserService _userService;
        private readonly ILogger<QuoteService> _logger;

        public QuoteService(
            IRepository<Quote> quoteRepository,
            IRepository<QuoteItem> quoteItemRepository,
            IRepository<RFQItem> rfqItemRepository,
            IRepository<RFQ> rfqRepository,
            IUnitOfWork unitOfWork,
            ISerialNumberService serialNumberService,
            IUserService userService,
            ILogger<QuoteService> logger)
        {
            _quoteRepository = quoteRepository;
            _quoteItemRepository = quoteItemRepository;
            _rfqItemRepository = rfqItemRepository;
            _rfqRepository = rfqRepository;
            _unitOfWork = unitOfWork;
            _serialNumberService = serialNumberService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>为列表/详情 JSON 填充需求主表编号（与 RFQId 对应）。</summary>
        private async Task HydrateQuoteRfqCodeAsync(IReadOnlyCollection<Quote> quotes)
        {
            if (quotes.Count == 0) return;
            var ids = quotes
                .Select(q => q.RFQId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0) return;

            var rfqs = (await _rfqRepository.FindAsync(r => ids.Contains(r.Id))).ToList();
            var byId = rfqs.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var q in quotes)
            {
                if (string.IsNullOrWhiteSpace(q.RFQId)) continue;
                if (byId.TryGetValue(q.RFQId.Trim(), out var rfq))
                    q.RfqCode = rfq.RfqCode;
            }
        }

        /// <summary>为列表/详情 JSON 填充采购员、业务员展示名（与需求明细列表一致）。</summary>
        private async Task HydrateQuoteUserDisplayAsync(IReadOnlyCollection<Quote> quotes)
        {
            if (quotes.Count == 0) return;
            var users = (await _userService.GetAllAsync())
                .ToDictionary(u => u.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var q in quotes)
            {
                if (!string.IsNullOrWhiteSpace(q.PurchaseUserId) &&
                    users.TryGetValue(q.PurchaseUserId.Trim(), out var pu))
                    q.PurchaseUserName = EntityLookupService.FormatUserDisplayName(pu);
                if (!string.IsNullOrWhiteSpace(q.SalesUserId) &&
                    users.TryGetValue(q.SalesUserId.Trim(), out var su))
                    q.SalesUserName = EntityLookupService.FormatUserDisplayName(su);
            }
        }

        public async Task<Quote> CreateAsync(CreateQuoteRequest request, string? actingUserId = null)
        {
            // 后端统一生成报价单号（忽略客户端传入）
            var quoteCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.Quotation);

            // 仅使用 RFQItemId 绑定需求明细（同一需求下可有多条相同 MPN，禁止按 RFQId+Mpn 推断）
            var rfqItemIdTrim = string.IsNullOrWhiteSpace(request.RFQItemId) ? null : request.RFQItemId.Trim();

            var quote = new Quote
            {
                Id = Guid.NewGuid().ToString(),
                QuoteCode = quoteCode,
                RFQId = request.RFQId,
                RFQItemId = rfqItemIdTrim,
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

            // 创建报价后回写需求明细状态（仅 RFQItemId + 待报价→已报价）
            if (string.IsNullOrWhiteSpace(rfqItemIdTrim))
            {
                _logger.LogWarning(
                    "创建报价后跳过需求明细状态回写：请求未带 RFQItemId。QuoteId={QuoteId} QuoteCode={QuoteCode} RFQId={RfqId}",
                    quote.Id, quoteCode, request.RFQId);
            }
            else
            {
                _logger.LogInformation(
                    "创建报价后尝试回写需求明细状态。QuoteId={QuoteId} QuoteCode={QuoteCode} RFQItemId={RfqItemId}",
                    quote.Id, quoteCode, rfqItemIdTrim);

                var rfqItem = await _rfqItemRepository.GetByIdAsync(rfqItemIdTrim);
                if (rfqItem == null)
                {
                    _logger.LogWarning(
                        "需求明细不存在，无法回写状态。RFQItemId={RfqItemId} QuoteId={QuoteId}",
                        rfqItemIdTrim, quote.Id);
                }
                else if (rfqItem.Status != 0)
                {
                    _logger.LogInformation(
                        "需求明细状态非待报价(0)，不覆盖。RFQItemId={RfqItemId} CurrentStatus={Status} QuoteId={QuoteId}",
                        rfqItemIdTrim, rfqItem.Status, quote.Id);
                }
                else
                {
                    rfqItem.Status = 1;
                    rfqItem.ModifyTime = DateTime.UtcNow;
                    await _rfqItemRepository.UpdateAsync(rfqItem);
                    _logger.LogInformation(
                        "需求明细状态已更新：待报价(0)→已报价(1)。RFQItemId={RfqItemId} RfqId={RfqId} QuoteId={QuoteId}",
                        rfqItemIdTrim, rfqItem.RfqId, quote.Id);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await HydrateQuoteRfqCodeAsync(new[] { quote });
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
            await HydrateQuoteRfqCodeAsync(new[] { quote });
            await HydrateQuoteUserDisplayAsync(new[] { quote });
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

            await HydrateQuoteRfqCodeAsync(quotes);
            await HydrateQuoteUserDisplayAsync(quotes);
            return quotes.OrderByDescending(q => q.CreateTime).ToList();
        }

        public async Task<Quote> UpdateAsync(string id, UpdateQuoteRequest request, string? actingUserId = null)
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
            quote.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

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
            await HydrateQuoteRfqCodeAsync(new[] { quote });
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
                        _logger.LogInformation(
                            "删除报价后需求明细状态回退：已报价(1)→待报价(0)。RFQItemId={RfqItemId} DeletedQuoteId={QuoteId}",
                            rfqItemId, id);
                    }
                    else if (rfqItem != null)
                    {
                        _logger.LogInformation(
                            "删除报价后未回退需求明细状态（当前非已报价(1)）。RFQItemId={RfqItemId} CurrentStatus={Status} DeletedQuoteId={QuoteId}",
                            rfqItemId, rfqItem.Status, id);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "删除报价后无法回退需求明细：明细不存在。RFQItemId={RfqItemId} DeletedQuoteId={QuoteId}",
                            rfqItemId, id);
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "删除报价后仍有其他报价关联该明细，需求明细状态不变。RFQItemId={RfqItemId} RemainingQuoteCount={Count} DeletedQuoteId={QuoteId}",
                        rfqItemId, remainingQuotes.Count(), id);
                }
            }
            else
            {
                _logger.LogInformation("删除报价：无 RFQItemId，跳过需求明细状态处理。DeletedQuoteId={QuoteId}", id);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(string id, short status, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var quote = await _quoteRepository.GetByIdAsync(id);
            if (quote == null)
                throw new InvalidOperationException($"报价单 {id} 不存在");

            quote.Status = status;
            quote.ModifyTime = DateTime.UtcNow;
            quote.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

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
