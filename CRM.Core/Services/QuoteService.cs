using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.System;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CRM.Core.Services
{
    /// <summary>
    /// 报价服务实现
    /// </summary>
    public partial class QuoteService : IQuoteService
    {
        private readonly IRepository<Quote> _quoteRepository;
        private readonly IRepository<QuoteItem> _quoteItemRepository;
        private readonly IRepository<RFQItem> _rfqItemRepository;
        private readonly IRepository<RFQ> _rfqRepository;
        private readonly IRepository<CustomerInfo> _customerRepository;
        private readonly IRepository<VendorInfo> _vendorRepository;
        private readonly IVendorService _vendorService;
        private readonly IVendorTradeCountQuery _vendorTradeCountQuery;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IUserService _userService;
        private readonly IQuoteListQuery _quoteListQuery;
        private readonly IRbacService _rbacService;
        private readonly ILogger<QuoteService> _logger;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly IPurchaseQuoterPoolService _purchaseQuoterPoolService;

        public QuoteService(
            IRepository<Quote> quoteRepository,
            IRepository<QuoteItem> quoteItemRepository,
            IRepository<RFQItem> rfqItemRepository,
            IRepository<RFQ> rfqRepository,
            IRepository<CustomerInfo> customerRepository,
            IRepository<VendorInfo> vendorRepository,
            IVendorService vendorService,
            IVendorTradeCountQuery vendorTradeCountQuery,
            IUnitOfWork unitOfWork,
            ISerialNumberService serialNumberService,
            IUserService userService,
            IQuoteListQuery quoteListQuery,
            IRbacService rbacService,
            ILogger<QuoteService> logger,
            ILogOperationAppendService logOperationAppend,
            IPurchaseQuoterPoolService purchaseQuoterPoolService)
        {
            _quoteRepository = quoteRepository;
            _quoteItemRepository = quoteItemRepository;
            _rfqItemRepository = rfqItemRepository;
            _rfqRepository = rfqRepository;
            _customerRepository = customerRepository;
            _vendorRepository = vendorRepository;
            _vendorService = vendorService;
            _vendorTradeCountQuery = vendorTradeCountQuery;
            _unitOfWork = unitOfWork;
            _serialNumberService = serialNumberService;
            _userService = userService;
            _quoteListQuery = quoteListQuery;
            _rbacService = rbacService;
            _logger = logger;
            _logOperationAppend = logOperationAppend;
            _purchaseQuoterPoolService = purchaseQuoterPoolService;
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

        private static string? FormatQuoteCustomerDisplayName(CustomerInfo c)
        {
            if (!string.IsNullOrWhiteSpace(c.OfficialName)) return c.OfficialName.Trim();
            if (!string.IsNullOrWhiteSpace(c.NickName)) return c.NickName.Trim();
            if (!string.IsNullOrWhiteSpace(c.CustomerCode)) return c.CustomerCode.Trim();
            return null;
        }

        /// <summary>为列表/详情 JSON 填充客户展示名（报价头 customer_id；缺省时由关联 RFQ 主表客户解析）。</summary>
        private async Task HydrateQuoteCustomerDisplayAsync(IReadOnlyCollection<Quote> quotes)
        {
            if (quotes.Count == 0) return;

            var rfqIdsForFallback = quotes
                .Where(q => string.IsNullOrWhiteSpace(q.CustomerId) && !string.IsNullOrWhiteSpace(q.RFQId))
                .Select(q => q.RFQId!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            Dictionary<string, string> customerIdByRfqId = new(StringComparer.OrdinalIgnoreCase);
            if (rfqIdsForFallback.Count > 0)
            {
                var rfqs = (await _rfqRepository.FindAsync(r => rfqIdsForFallback.Contains(r.Id))).ToList();
                foreach (var r in rfqs)
                {
                    if (!string.IsNullOrWhiteSpace(r.CustomerId))
                        customerIdByRfqId[r.Id.Trim()] = r.CustomerId.Trim();
                }
            }

            var customerIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var q in quotes)
            {
                if (!string.IsNullOrWhiteSpace(q.CustomerId))
                    customerIdSet.Add(q.CustomerId.Trim());
                else if (!string.IsNullOrWhiteSpace(q.RFQId) &&
                         customerIdByRfqId.TryGetValue(q.RFQId.Trim(), out var cidFromRfq))
                    customerIdSet.Add(cidFromRfq);
            }

            if (customerIdSet.Count == 0) return;
            var idList = customerIdSet.ToList();
            var customers = (await _customerRepository.FindAsync(c => idList.Contains(c.Id))).ToList();
            var customerById = customers.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);

            foreach (var q in quotes)
            {
                var cid = !string.IsNullOrWhiteSpace(q.CustomerId)
                    ? q.CustomerId.Trim()
                    : (!string.IsNullOrWhiteSpace(q.RFQId) &&
                       customerIdByRfqId.TryGetValue(q.RFQId.Trim(), out var x)
                        ? x
                        : null);
                if (cid == null) continue;
                if (!customerById.TryGetValue(cid, out var cust)) continue;
                q.CustomerName = FormatQuoteCustomerDisplayName(cust);
            }
        }

        /// <summary>从需求明细已分配采购员解析采购员 ID（优先 AssignedPurchaserUserId1）。</summary>
        private static string? ResolveAssignedPurchaserUserId(RFQItem? rfqItem)
        {
            if (rfqItem == null) return null;
            if (!string.IsNullOrWhiteSpace(rfqItem.AssignedPurchaserUserId1))
                return rfqItem.AssignedPurchaserUserId1.Trim();
            if (!string.IsNullOrWhiteSpace(rfqItem.AssignedPurchaserUserId2))
                return rfqItem.AssignedPurchaserUserId2.Trim();
            return null;
        }

        /// <summary>为列表/详情 JSON 填充采购员、业务员登录账号（业务列表与客户习惯一致）。</summary>
        private async Task HydrateQuoteUserDisplayAsync(IReadOnlyCollection<Quote> quotes)
        {
            if (quotes.Count == 0) return;
            var users = (await _userService.GetAllAsync())
                .ToDictionary(u => u.Id, StringComparer.OrdinalIgnoreCase);

            var needRfqFallbackItemIds = quotes
                .Where(q => string.IsNullOrWhiteSpace(q.PurchaseUserId) && !string.IsNullOrWhiteSpace(q.RFQItemId))
                .Select(q => q.RFQItemId!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var purchaserByRfqItemId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (needRfqFallbackItemIds.Count > 0)
            {
                var rfqItems = (await _rfqItemRepository.FindAsync(i => needRfqFallbackItemIds.Contains(i.Id))).ToList();
                foreach (var it in rfqItems)
                {
                    var pid = ResolveAssignedPurchaserUserId(it);
                    if (!string.IsNullOrWhiteSpace(pid))
                        purchaserByRfqItemId[it.Id.Trim()] = pid;
                }
            }

            var needRfqSalesIds = quotes
                .Where(q => !string.IsNullOrWhiteSpace(q.RFQId) && NeedsRfqSalesUserFallback(q))
                .Select(q => q.RFQId!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var salesByRfqId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (needRfqSalesIds.Count > 0)
            {
                var rfqs = (await _rfqRepository.FindAsync(r => needRfqSalesIds.Contains(r.Id))).ToList();
                foreach (var r in rfqs)
                {
                    if (!string.IsNullOrWhiteSpace(r.SalesUserId))
                        salesByRfqId[r.Id.Trim()] = r.SalesUserId.Trim();
                }
            }

            foreach (var q in quotes)
            {
                var purchaseUserId = !string.IsNullOrWhiteSpace(q.PurchaseUserId)
                    ? q.PurchaseUserId.Trim()
                    : (!string.IsNullOrWhiteSpace(q.RFQItemId) &&
                       purchaserByRfqItemId.TryGetValue(q.RFQItemId.Trim(), out var fallbackPid)
                        ? fallbackPid
                        : null);
                if (!string.IsNullOrWhiteSpace(purchaseUserId) &&
                    users.TryGetValue(purchaseUserId, out var pu))
                    q.PurchaseUserName = EntityLookupService.FormatUserLoginName(pu);

                var salesUserId = ResolveDisplaySalesUserId(q, purchaseUserId, salesByRfqId);
                if (!string.IsNullOrWhiteSpace(salesUserId) &&
                    users.TryGetValue(salesUserId, out var su))
                    q.SalesUserName = EntityLookupService.FormatUserLoginName(su);
                if (!string.IsNullOrWhiteSpace(q.CreateByUserId) &&
                    users.TryGetValue(q.CreateByUserId.Trim(), out var cu))
                    q.CreateUserName = EntityLookupService.FormatUserLoginName(cu);
            }
        }

        /// <summary>
        /// 报价头业务员为空，或被写成与采购员同一人时，改用需求主表业务员展示。
        /// </summary>
        private static bool NeedsRfqSalesUserFallback(Quote q)
        {
            if (string.IsNullOrWhiteSpace(q.SalesUserId))
                return true;
            return !string.IsNullOrWhiteSpace(q.PurchaseUserId) &&
                   string.Equals(q.SalesUserId.Trim(), q.PurchaseUserId.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static string? ResolveDisplaySalesUserId(
            Quote q,
            string? purchaseUserId,
            IReadOnlyDictionary<string, string> salesByRfqId)
        {
            var quoteSales = string.IsNullOrWhiteSpace(q.SalesUserId) ? null : q.SalesUserId.Trim();
            string? rfqSales = null;
            if (!string.IsNullOrWhiteSpace(q.RFQId) &&
                salesByRfqId.TryGetValue(q.RFQId.Trim(), out var fromRfq) &&
                !string.IsNullOrWhiteSpace(fromRfq))
                rfqSales = fromRfq.Trim();

            if (string.IsNullOrWhiteSpace(quoteSales))
                return rfqSales;

            if (purchaseUserId != null &&
                string.Equals(quoteSales, purchaseUserId, StringComparison.OrdinalIgnoreCase) &&
                rfqSales != null &&
                !string.Equals(rfqSales, purchaseUserId, StringComparison.OrdinalIgnoreCase))
                return rfqSales;

            return quoteSales;
        }

        public async Task<Quote> CreateAsync(CreateQuoteRequest request, string? actingUserId = null)
        {
            // 后端统一生成报价单号（忽略客户端传入）
            var quoteCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.Quotation);

            // 必须关联需求明细
            var rfqItemIdTrim = string.IsNullOrWhiteSpace(request.RFQItemId) ? null : request.RFQItemId.Trim();
            if (string.IsNullOrWhiteSpace(rfqItemIdTrim))
                throw new ArgumentException("必须关联需求明细");

            var linkedRfqItem = await _rfqItemRepository.GetByIdAsync(rfqItemIdTrim)
                ?? throw new ArgumentException("需求明细不存在");

            var purchaseUserId = string.IsNullOrWhiteSpace(request.PurchaseUserId) ? null : request.PurchaseUserId.Trim();
            if (string.IsNullOrWhiteSpace(purchaseUserId))
                purchaseUserId = ResolveAssignedPurchaserUserId(linkedRfqItem);
            await EnsureCanQuoteRfqItemAsync(linkedRfqItem, actingUserId);

            if (string.IsNullOrWhiteSpace(purchaseUserId))
                throw new ArgumentException("请选择采购员");

            var rfqIdFromItem = linkedRfqItem.RfqId?.Trim();
            if (string.IsNullOrWhiteSpace(rfqIdFromItem))
                throw new ArgumentException("需求明细未关联需求主单");

            if (!string.IsNullOrWhiteSpace(request.RFQId) &&
                !string.Equals(request.RFQId.Trim(), rfqIdFromItem, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("需求主单与明细不一致");

            var salesUserId = string.IsNullOrWhiteSpace(request.SalesUserId) ? null : request.SalesUserId.Trim();
            if (string.IsNullOrWhiteSpace(salesUserId))
            {
                var linkedRfq = await _rfqRepository.GetByIdAsync(rfqIdFromItem);
                salesUserId = string.IsNullOrWhiteSpace(linkedRfq?.SalesUserId) ? null : linkedRfq.SalesUserId.Trim();
            }

            var quote = new Quote
            {
                Id = Guid.NewGuid().ToString(),
                QuoteCode = quoteCode,
                RFQId = rfqIdFromItem,
                RFQItemId = rfqItemIdTrim,
                Mpn = request.Mpn,
                CustomerId = request.CustomerId,
                SalesUserId = salesUserId,
                PurchaseUserId = purchaseUserId,
                QuoteDate = request.QuoteDate == default ? DateTime.UtcNow : PostgreSqlDateTime.ToUtc(request.QuoteDate),
                Status = (short)QuoteMainStatus.New,
                Remark = request.Remark,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
            };

            await _quoteRepository.AddAsync(quote);

            var createdItems = new List<QuoteItem>();
            foreach (var itemReq in request.Items)
            {
                var item = MapToQuoteItem(quote.Id, itemReq);
                await _quoteItemRepository.AddAsync(item);
                createdItems.Add(item);
            }

            var activeOrdered = createdItems.OrderBy(i => i.CreateTime).ToList();
            foreach (var item in createdItems)
                await LogQuoteItemAddedAsync(quote, item, activeOrdered, actingUserId);

            // 创建报价后回写需求明细状态（待报价/查无报价→已报价）
            _logger.LogInformation(
                "创建报价后尝试回写需求明细状态。QuoteId={QuoteId} QuoteCode={QuoteCode} RFQItemId={RfqItemId}",
                quote.Id, quoteCode, rfqItemIdTrim);

            if (linkedRfqItem.Status != (short)RfqItemStatus.Pending
                && linkedRfqItem.Status != (short)RfqItemStatus.NoQuoteFound)
            {
                _logger.LogInformation(
                    "需求明细状态非待报价(0)或查无报价(5)，不覆盖。RFQItemId={RfqItemId} CurrentStatus={Status} QuoteId={QuoteId}",
                    rfqItemIdTrim, linkedRfqItem.Status, quote.Id);
            }
            else
            {
                var prevStatus = linkedRfqItem.Status;
                linkedRfqItem.Status = (short)RfqItemStatus.Quoted;
                linkedRfqItem.ModifyTime = DateTime.UtcNow;
                await _rfqItemRepository.UpdateAsync(linkedRfqItem);
                _logger.LogInformation(
                    "需求明细状态已更新：{PrevStatus}→已报价(1)。RFQItemId={RfqItemId} RfqId={RfqId} QuoteId={QuoteId}",
                    prevStatus, rfqItemIdTrim, linkedRfqItem.RfqId, quote.Id);
            }

            await ApplyVendorLevelsFromQuoteItemsAsync(request.Items, actingUserId);
            await _unitOfWork.SaveChangesAsync();
            await HydrateQuoteRfqCodeAsync(new[] { quote });
            await HydrateQuoteCustomerDisplayAsync(new[] { quote });
            await HydrateQuoteUserDisplayAsync(new[] { quote });
            quote.Items = createdItems;
            await HydrateQuoteItemVendorLevelAsync(new[] { quote });
            await HydrateQuoteItemVendorTradeCountAsync(new[] { quote });
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
            quote.Items = items.Where(i => !i.IsDeleted).ToList();
            await HydrateQuoteRfqCodeAsync(new[] { quote });
            await HydrateQuoteCustomerDisplayAsync(new[] { quote });
            await HydrateQuoteUserDisplayAsync(new[] { quote });
            await HydrateQuoteItemVendorLevelAsync(new[] { quote });
            await HydrateQuoteItemVendorTradeCountAsync(new[] { quote });
            return quote;
        }

        public async Task<IEnumerable<Quote>> GetAllAsync()
        {
            var quotes = (await _quoteRepository.GetAllAsync())
                .OrderByDescending(q => q.CreateTime)
                .ToList();
            await AttachItemsAndHydrateAsync(quotes);
            return quotes;
        }

        /// <inheritdoc />
        public async Task<PagedResult<Quote>> GetPagedAsync(QuoteQueryRequest request)
        {
            var page = await _quoteListQuery.GetPagedAsync(request);
            var list = page.Items.ToList();
            await AttachItemsAndHydrateAsync(list);
            return new PagedResult<Quote>
            {
                Items = list,
                TotalCount = page.TotalCount,
                PageIndex = page.PageIndex,
                PageSize = page.PageSize
            };
        }

        private async Task AttachItemsAndHydrateAsync(List<Quote> quotes)
        {
            if (quotes.Count == 0)
                return;

            var quoteIds = quotes.Select(q => q.Id).ToList();
            var itemRows = await _quoteItemRepository.FindAsync(i => quoteIds.Contains(i.QuoteId));
            var byQuoteId = itemRows
                .Where(i => !i.IsDeleted)
                .GroupBy(i => i.QuoteId)
                .ToDictionary(g => g.Key, g => (ICollection<QuoteItem>)g.ToList());

            foreach (var q in quotes)
                q.Items = byQuoteId.TryGetValue(q.Id, out var list) ? list : new List<QuoteItem>();

            await HydrateQuoteRfqCodeAsync(quotes);
            await HydrateQuoteCustomerDisplayAsync(quotes);
            await HydrateQuoteUserDisplayAsync(quotes);
            await HydrateQuoteItemVendorLevelAsync(quotes);
            await HydrateQuoteItemVendorTradeCountAsync(quotes);
        }

        /// <summary>为报价明细现读供应商等级、英文名；中文名为空时回填主数据全称。</summary>
        private async Task HydrateQuoteItemVendorLevelAsync(IReadOnlyCollection<Quote> quotes)
        {
            var items = quotes
                .Where(q => q.Items != null)
                .SelectMany(q => q.Items)
                .ToList();
            if (items.Count == 0) return;

            var ids = items
                .Select(i => i.VendorId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0) return;

            var vendors = (await _vendorRepository.FindAsync(v => ids.Contains(v.Id))).ToList();
            var vendorById = vendors
                .GroupBy(v => v.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var it in items)
            {
                if (string.IsNullOrWhiteSpace(it.VendorId)) continue;
                if (!vendorById.TryGetValue(it.VendorId.Trim(), out var vendor)) continue;
                it.VendorLevel = vendor.Level;
                it.VendorEnglishName = string.IsNullOrWhiteSpace(vendor.EnglishOfficialName)
                    ? null
                    : vendor.EnglishOfficialName.Trim();
                if (string.IsNullOrWhiteSpace(it.VendorName) && !string.IsNullOrWhiteSpace(vendor.OfficialName))
                    it.VendorName = vendor.OfficialName.Trim();
            }
        }

        /// <summary>为报价明细现读供应商交易次数（有效付款单 × distinct 采购明细）。</summary>
        private async Task HydrateQuoteItemVendorTradeCountAsync(IReadOnlyCollection<Quote> quotes)
        {
            var items = quotes
                .Where(q => q.Items != null)
                .SelectMany(q => q.Items)
                .ToList();
            if (items.Count == 0) return;

            var ids = items
                .Select(i => i.VendorId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0) return;

            var counts = await _vendorTradeCountQuery.GetTradeCountsAsync(ids);
            foreach (var it in items)
            {
                if (string.IsNullOrWhiteSpace(it.VendorId)) continue;
                it.VendorTradeCount = counts.TryGetValue(it.VendorId.Trim(), out var n) ? n : 0;
            }
        }

        public async Task<Quote> UpdateAsync(string id, UpdateQuoteRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var quote = await _quoteRepository.GetByIdAsync(id);
            if (quote == null)
                throw new InvalidOperationException($"报价单 {id} 不存在");

            EnsureQuoteEditable(quote);

            if (!string.IsNullOrWhiteSpace(quote.RFQItemId))
            {
                var linkedRfqItem = await _rfqItemRepository.GetByIdAsync(quote.RFQItemId.Trim());
                await EnsureCanQuoteRfqItemAsync(linkedRfqItem, actingUserId);
            }

            var headerBefore = CaptureQuoteHeaderSnapshot(quote);

            if (request.Mpn != null) quote.Mpn = request.Mpn;
            if (request.CustomerId != null) quote.CustomerId = request.CustomerId;
            if (request.SalesUserId != null) quote.SalesUserId = request.SalesUserId;
            if (request.PurchaseUserId != null)
            {
                if (string.IsNullOrWhiteSpace(request.PurchaseUserId))
                    throw new ArgumentException("请选择采购员");
                quote.PurchaseUserId = request.PurchaseUserId.Trim();
            }

            if (string.IsNullOrWhiteSpace(quote.PurchaseUserId))
                throw new ArgumentException("请选择采购员");

            if (request.QuoteDate.HasValue) quote.QuoteDate = PostgreSqlDateTime.ToUtc(request.QuoteDate.Value);
            if (request.Remark != null) quote.Remark = request.Remark;

            quote.ModifyTime = DateTime.UtcNow;
            quote.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

            await _quoteRepository.UpdateAsync(quote);

            List<QuoteItem>? deletedLines = null;
            if (request.Items != null && request.Items.Count > 0)
            {
                var sync = await SyncQuoteItemsOnUpdateAsync(quote, id, request.Items, actingUserId);
                deletedLines = sync.Deleted;
            }

            await LogQuoteHeaderFieldChangesAsync(quote, headerBefore, actingUserId);

            if (request.Items != null)
                await ApplyVendorLevelsFromQuoteItemsAsync(request.Items, actingUserId);

            await _unitOfWork.SaveChangesAsync();

            if (deletedLines is { Count: > 0 })
            {
                await AppendQuoteItemDeleteOperationLogsAsync(
                    quote,
                    deletedLines,
                    actingUserId,
                    OperationLogActionTypes.QuoteItemDelete,
                    $"编辑报价单 {quote.QuoteCode} 时删除明细行");
            }
            await HydrateQuoteRfqCodeAsync(new[] { quote });
            await HydrateQuoteCustomerDisplayAsync(new[] { quote });
            await HydrateQuoteUserDisplayAsync(new[] { quote });
            await HydrateQuoteItemVendorLevelAsync(new[] { quote });
            await HydrateQuoteItemVendorTradeCountAsync(new[] { quote });
            return quote;
        }

        /// <summary>按明细请求回写供应商等级；同一供应商取首次出现的等级。</summary>
        private async Task ApplyVendorLevelsFromQuoteItemsAsync(
            IEnumerable<CreateQuoteItemRequest> items,
            string? actingUserId)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                var vendorId = item.VendorId?.Trim();
                if (string.IsNullOrWhiteSpace(vendorId) || !item.VendorLevel.HasValue)
                    continue;
                if (!seen.Add(vendorId))
                    continue;
                await _vendorService.ApplyLevelIfChangedAsync(vendorId, item.VendorLevel, actingUserId);
            }
        }

        public async Task DeleteAsync(string id, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var quote = await _quoteRepository.GetByIdAsync(id);
            if (quote == null)
                throw new InvalidOperationException($"报价单 {id} 不存在");

            EnsureQuoteDeletable(quote);

            RFQItem? rfqItem = null;
            if (!string.IsNullOrWhiteSpace(quote.RFQItemId))
                rfqItem = await _rfqItemRepository.GetByIdAsync(quote.RFQItemId.Trim());

            // 先删明细行
            var items = await _quoteItemRepository.GetAllAsync();
            var quoteItems = items.Where(i => i.QuoteId == id).ToList();
            foreach (var item in quoteItems)
                await _quoteItemRepository.DeleteAsync(item.Id);

            await _quoteRepository.DeleteAsync(id);

            var (actorId, actorName) = await ResolveActorAsync(actingUserId);
            var mpn = string.IsNullOrWhiteSpace(rfqItem?.Mpn) ? quote.Mpn : rfqItem!.Mpn;
            var brand = rfqItem?.Brand;
            var lineNo = rfqItem?.LineNo ?? 0;
            var lineHint = rfqItem != null
                ? $"需求明细行号 {lineNo}，物料型号 {mpn}，品牌 {brand}。"
                : "";
            var extraInfo = JsonSerializer.Serialize(new
            {
                quoteItemCount = quoteItems.Count,
                rfqItemId = quote.RFQItemId,
                lineNo,
                mpn,
                brand
            });
            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = BusinessLogTypes.Quote,
                RecordId = quote.Id,
                RecordCode = quote.QuoteCode,
                EntityDisplayName = DeleteLogEntityNames.Quote,
                ActionTypeOverride = OperationLogActionTypes.QuoteHeaderDelete,
                ExtraDetail = $"明细行数={quoteItems.Count}",
                ExtraInfo = extraInfo,
                OperatorUserId = actorId,
                OperatorUserName = actorName,
                OperationDescOverride = $"删除报价单 {quote.QuoteCode}。{lineHint}明细行数={quoteItems.Count}"
            });

            // 删除报价后，如果该 RFQ 明细已无任何报价，则回退为「待报价」(0)
            if (!string.IsNullOrWhiteSpace(quote.RFQItemId))
            {
                var rfqItemId = quote.RFQItemId.Trim();
                var remainingQuotes = await _quoteRepository.FindAsync(q => q.RFQItemId == rfqItemId && q.Id != id);
                if (!remainingQuotes.Any())
                {
                    rfqItem ??= await _rfqItemRepository.GetByIdAsync(rfqItemId);
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

        public Task UpdateStatusAsync(string id, short status, string? actingUserId = null)
        {
            _ = id;
            _ = status;
            _ = actingUserId;
            throw new InvalidOperationException("报价状态由系统自动维护，不支持手动修改");
        }

        private static void EnsureQuoteEditable(Quote quote)
        {
            if (quote.Status == (short)QuoteMainStatus.Won)
                throw new InvalidOperationException("成单状态的报价不可编辑");
            if (quote.Status == (short)QuoteMainStatus.Closed)
                throw new InvalidOperationException("关闭状态的报价不可编辑");
        }

        private static void EnsureQuoteDeletable(Quote quote)
        {
            if (quote.Status == (short)QuoteMainStatus.Won)
                throw new InvalidOperationException("成单状态的报价不可删除");
        }

        private sealed record QuoteItemSyncResult(
            List<QuoteItem> Inserted,
            List<(QuoteItem Item, QuoteItemFieldSnapshot Before)> Updated,
            List<QuoteItem> Deleted);

        private async Task<QuoteItemSyncResult> SyncQuoteItemsOnUpdateAsync(
            Quote quote,
            string quoteId,
            List<CreateQuoteItemRequest> requestItems,
            string? actingUserId)
        {
            var existingActive = (await _quoteItemRepository.FindAsync(i => i.QuoteId == quoteId))
                .Where(i => !i.IsDeleted)
                .ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);

            var inserted = new List<QuoteItem>();
            var updated = new List<(QuoteItem Item, QuoteItemFieldSnapshot Before)>();
            var keptIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var newItemRequests = new List<CreateQuoteItemRequest>();

            foreach (var itemReq in requestItems)
            {
                var reqId = itemReq.Id?.Trim();
                if (!string.IsNullOrEmpty(reqId))
                {
                    if (!existingActive.TryGetValue(reqId, out var existing))
                        throw new InvalidOperationException($"报价明细 {reqId} 不存在或已删除");

                    keptIds.Add(reqId);
                    var before = CaptureQuoteItemFieldSnapshot(existing);
                    ApplyQuoteItemFromRequest(existing, itemReq);
                    existing.ModifyTime = DateTime.UtcNow;
                    await _quoteItemRepository.UpdateAsync(existing);
                    updated.Add((existing, before));
                }
                else
                {
                    newItemRequests.Add(itemReq);
                }
            }

            foreach (var itemReq in newItemRequests)
            {
                var item = new QuoteItem
                {
                    Id = Guid.NewGuid().ToString(),
                    QuoteId = quoteId,
                    CreateTime = DateTime.UtcNow
                };
                ApplyQuoteItemFromRequest(item, itemReq);
                await _quoteItemRepository.AddAsync(item);
                inserted.Add(item);
            }

            var deleted = new List<QuoteItem>();
            foreach (var existing in existingActive.Values)
            {
                if (keptIds.Contains(existing.Id))
                    continue;

                existing.IsDeleted = true;
                existing.ModifyTime = DateTime.UtcNow;
                await _quoteItemRepository.UpdateAsync(existing);
                deleted.Add(existing);
            }

            var activeOrdered = existingActive.Values
                .Where(i => !deleted.Any(d => string.Equals(d.Id, i.Id, StringComparison.OrdinalIgnoreCase)))
                .Concat(inserted)
                .OrderBy(i => i.CreateTime)
                .ToList();

            foreach (var (item, before) in updated)
                await LogQuoteItemFieldChangesAsync(quote, item, before, activeOrdered, actingUserId);

            foreach (var item in inserted)
                await LogQuoteItemAddedAsync(quote, item, activeOrdered, actingUserId);

            return new QuoteItemSyncResult(inserted, updated, deleted);
        }

        private static void ApplyQuoteItemFromRequest(QuoteItem target, CreateQuoteItemRequest req)
        {
            target.VendorId = req.VendorId;
            target.VendorName = req.VendorName;
            target.VendorCode = req.VendorCode;
            target.ContactId = req.ContactId;
            target.ContactName = req.ContactName;
            target.PriceType = req.PriceType;
            target.ExpiryDate = PostgreSqlDateTime.ToUtc(req.ExpiryDate);
            target.Mpn = req.Mpn;
            target.Brand = req.Brand;
            target.BrandOrigin = req.BrandOrigin;
            target.DateCode = req.DateCode;
            target.LeadTime = req.LeadTime;
            target.LabelType = req.LabelType;
            target.WaferOrigin = req.WaferOrigin;
            target.PackageOrigin = req.PackageOrigin;
            target.FreeShipping = req.FreeShipping;
            target.Currency = req.Currency;
            target.Quantity = req.Quantity;
            target.UnitPrice = req.UnitPrice;
            target.ConvertedPrice = req.ConvertedPrice;
            target.MinPackageQty = req.MinPackageQty;
            target.MinPackageUnit = req.MinPackageUnit;
            target.StockQty = req.StockQty;
            target.Moq = req.Moq;
            target.Remark = req.Remark;
            target.Status = req.Status;
        }

        private static QuoteItem MapToQuoteItem(string quoteId, CreateQuoteItemRequest req)
        {
            var item = new QuoteItem
            {
                Id = Guid.NewGuid().ToString(),
                QuoteId = quoteId,
                CreateTime = DateTime.UtcNow
            };
            ApplyQuoteItemFromRequest(item, req);
            return item;
        }

        private async Task AppendQuoteItemDeleteOperationLogsAsync(
            Quote quote,
            IReadOnlyList<QuoteItem> deletedItems,
            string? actingUserId,
            string actionType,
            string descriptionPrefix)
        {
            var (actorId, actorName) = await ResolveActorAsync(actingUserId);
            var tier = 0;
            foreach (var d in deletedItems.OrderBy(i => i.CreateTime))
            {
                tier++;
                var lineCode = $"{quote.QuoteCode}#{tier}";
                await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
                {
                    BizType = BusinessLogTypes.QuoteItem,
                    RecordId = d.Id,
                    RecordCode = lineCode,
                    EntityDisplayName = DeleteLogEntityNames.QuoteItem,
                    ActionTypeOverride = actionType,
                    OperatorUserId = actorId,
                    OperatorUserName = actorName,
                    OperationDescOverride = $"{descriptionPrefix} {lineCode}"
                });
            }
        }

        private async Task<(string? UserId, string UserName)> ResolveActorAsync(string? actingUserId)
        {
            var id = ActingUserIdNormalizer.Normalize(actingUserId);
            if (string.IsNullOrEmpty(id))
                return (null, "系统");
            var user = await _userService.GetByIdAsync(id);
            return (id, string.IsNullOrWhiteSpace(user?.UserName) ? id : user!.UserName!.Trim());
        }

        private async Task EnsureCanQuoteRfqItemAsync(RFQItem? rfqItem, string? actingUserId)
        {
            var actorId = ActingUserIdNormalizer.Normalize(actingUserId);
            if (string.IsNullOrEmpty(actorId))
                throw new UnauthorizedAccessException("未登录或无法识别当前用户，无法创建/编辑报价");

            if (rfqItem == null)
                throw new InvalidOperationException("关联的需求明细不存在，无法报价");

            var summary = await _rbacService.GetUserPermissionSummaryAsync(actorId);
            var protectionMinutes = await _purchaseQuoterPoolService.GetDemandProtectionMinutesAsync();
            if (!RfqItemQuoteAccessRules.CanQuote(summary, rfqItem, actorId, protectionMinutes))
                throw new UnauthorizedAccessException("无权为该需求明细创建或编辑报价");
        }
    }
}
