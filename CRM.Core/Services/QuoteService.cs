using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.System;
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
        private readonly IRepository<CustomerInfo> _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IUserService _userService;
        private readonly IQuoteListQuery _quoteListQuery;
        private readonly IRbacService _rbacService;
        private readonly ILogger<QuoteService> _logger;
        private readonly ILogOperationAppendService _logOperationAppend;

        public QuoteService(
            IRepository<Quote> quoteRepository,
            IRepository<QuoteItem> quoteItemRepository,
            IRepository<RFQItem> rfqItemRepository,
            IRepository<RFQ> rfqRepository,
            IRepository<CustomerInfo> customerRepository,
            IUnitOfWork unitOfWork,
            ISerialNumberService serialNumberService,
            IUserService userService,
            IQuoteListQuery quoteListQuery,
            IRbacService rbacService,
            ILogger<QuoteService> logger,
            ILogOperationAppendService logOperationAppend)
        {
            _quoteRepository = quoteRepository;
            _quoteItemRepository = quoteItemRepository;
            _rfqItemRepository = rfqItemRepository;
            _rfqRepository = rfqRepository;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _serialNumberService = serialNumberService;
            _userService = userService;
            _quoteListQuery = quoteListQuery;
            _rbacService = rbacService;
            _logger = logger;
            _logOperationAppend = logOperationAppend;
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
                if (!string.IsNullOrWhiteSpace(q.SalesUserId) &&
                    users.TryGetValue(q.SalesUserId.Trim(), out var su))
                    q.SalesUserName = EntityLookupService.FormatUserLoginName(su);
            }
        }

        public async Task<Quote> CreateAsync(CreateQuoteRequest request, string? actingUserId = null)
        {
            // 后端统一生成报价单号（忽略客户端传入）
            var quoteCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.Quotation);

            // 仅使用 RFQItemId 绑定需求明细（同一需求下可有多条相同 MPN，禁止按 RFQId+Mpn 推断）
            var rfqItemIdTrim = string.IsNullOrWhiteSpace(request.RFQItemId) ? null : request.RFQItemId.Trim();

            RFQItem? linkedRfqItem = null;
            var purchaseUserId = string.IsNullOrWhiteSpace(request.PurchaseUserId) ? null : request.PurchaseUserId.Trim();
            if (!string.IsNullOrWhiteSpace(rfqItemIdTrim))
            {
                linkedRfqItem = await _rfqItemRepository.GetByIdAsync(rfqItemIdTrim);
                if (string.IsNullOrWhiteSpace(purchaseUserId))
                    purchaseUserId = ResolveAssignedPurchaserUserId(linkedRfqItem);
                await EnsureCanQuoteRfqItemAsync(linkedRfqItem, actingUserId);
            }

            if (string.IsNullOrWhiteSpace(purchaseUserId))
                throw new ArgumentException("请选择采购员");

            var quote = new Quote
            {
                Id = Guid.NewGuid().ToString(),
                QuoteCode = quoteCode,
                RFQId = request.RFQId,
                RFQItemId = rfqItemIdTrim,
                Mpn = request.Mpn,
                CustomerId = request.CustomerId,
                SalesUserId = request.SalesUserId,
                PurchaseUserId = purchaseUserId,
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

                var rfqItem = linkedRfqItem ?? await _rfqItemRepository.GetByIdAsync(rfqItemIdTrim);
                if (rfqItem == null)
                {
                    _logger.LogWarning(
                        "需求明细不存在，无法回写状态。RFQItemId={RfqItemId} QuoteId={QuoteId}",
                        rfqItemIdTrim, quote.Id);
                }
                else if (rfqItem.Status != (short)RfqItemStatus.Pending
                         && rfqItem.Status != (short)RfqItemStatus.NoQuoteFound)
                {
                    _logger.LogInformation(
                        "需求明细状态非待报价(0)或查无报价(5)，不覆盖。RFQItemId={RfqItemId} CurrentStatus={Status} QuoteId={QuoteId}",
                        rfqItemIdTrim, rfqItem.Status, quote.Id);
                }
                else
                {
                    var prevStatus = rfqItem.Status;
                    rfqItem.Status = (short)RfqItemStatus.Quoted;
                    rfqItem.ModifyTime = DateTime.UtcNow;
                    await _rfqItemRepository.UpdateAsync(rfqItem);
                    _logger.LogInformation(
                        "需求明细状态已更新：{PrevStatus}→已报价(1)。RFQItemId={RfqItemId} RfqId={RfqId} QuoteId={QuoteId}",
                        prevStatus, rfqItemIdTrim, rfqItem.RfqId, quote.Id);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await HydrateQuoteRfqCodeAsync(new[] { quote });
            await HydrateQuoteCustomerDisplayAsync(new[] { quote });
            await HydrateQuoteUserDisplayAsync(new[] { quote });
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
        }

        public async Task<Quote> UpdateAsync(string id, UpdateQuoteRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var quote = await _quoteRepository.GetByIdAsync(id);
            if (quote == null)
                throw new InvalidOperationException($"报价单 {id} 不存在");

            if (!string.IsNullOrWhiteSpace(quote.RFQItemId))
            {
                var linkedRfqItem = await _rfqItemRepository.GetByIdAsync(quote.RFQItemId.Trim());
                await EnsureCanQuoteRfqItemAsync(linkedRfqItem, actingUserId);
            }

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
            if (request.Status.HasValue) quote.Status = request.Status.Value;
            if (request.Remark != null) quote.Remark = request.Remark;

            quote.ModifyTime = DateTime.UtcNow;
            quote.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

            await _quoteRepository.UpdateAsync(quote);

            List<QuoteItem>? deletedLines = null;
            if (request.Items != null && request.Items.Count > 0)
            {
                var sync = await SyncQuoteItemsOnUpdateAsync(quote, id, request.Items);
                deletedLines = sync.Deleted;
            }

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
            var quoteItems = items.Where(i => i.QuoteId == id).ToList();
            foreach (var item in quoteItems)
                await _quoteItemRepository.DeleteAsync(item.Id);

            await _quoteRepository.DeleteAsync(id);

            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = BusinessLogTypes.Quote,
                RecordId = quote.Id,
                RecordCode = quote.QuoteCode,
                EntityDisplayName = DeleteLogEntityNames.Quote,
                ExtraDetail = $"明细行数={quoteItems.Count}"
            });

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

        private sealed record QuoteItemSyncResult(
            List<QuoteItem> Inserted,
            List<QuoteItem> Updated,
            List<QuoteItem> Deleted);

        private async Task<QuoteItemSyncResult> SyncQuoteItemsOnUpdateAsync(
            Quote quote,
            string quoteId,
            List<CreateQuoteItemRequest> requestItems)
        {
            var existingActive = (await _quoteItemRepository.FindAsync(i => i.QuoteId == quoteId))
                .Where(i => !i.IsDeleted)
                .ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);

            var inserted = new List<QuoteItem>();
            var updated = new List<QuoteItem>();
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
                    ApplyQuoteItemFromRequest(existing, itemReq);
                    existing.ModifyTime = DateTime.UtcNow;
                    await _quoteItemRepository.UpdateAsync(existing);
                    updated.Add(existing);
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
            if (!RfqItemQuoteAccessRules.CanQuote(summary, rfqItem, actorId))
                throw new UnauthorizedAccessException("无权为该需求明细创建或编辑报价");
        }
    }
}
