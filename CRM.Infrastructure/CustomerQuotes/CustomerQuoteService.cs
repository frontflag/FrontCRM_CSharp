using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Services;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.CustomerQuotes;

public class CustomerQuoteService : ICustomerQuoteService
{
    private readonly ApplicationDbContext _db;
    private readonly ISerialNumberService _serialNumberService;
    private readonly IRbacService _rbacService;
    private readonly IDataPermissionService _dataPermission;
    private readonly ILogOperationAppendService _logOperationAppend;

    public CustomerQuoteService(
        ApplicationDbContext db,
        ISerialNumberService serialNumberService,
        IRbacService rbacService,
        IDataPermissionService dataPermission,
        ILogOperationAppendService logOperationAppend)
    {
        _db = db;
        _serialNumberService = serialNumberService;
        _rbacService = rbacService;
        _dataPermission = dataPermission;
        _logOperationAppend = logOperationAppend;
    }

    public async Task<(IReadOnlyList<CustomerQuoteDraft> Items, int Total)> GetDraftsPagedAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        var q = _db.CustomerQuoteDrafts.AsNoTracking()
            .Where(d => !d.IsDeleted && d.Status == CustomerQuoteDraftStatus.Draft && d.CreateByUserId == uid);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(d => d.CreateTime)
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(Math.Clamp(pageSize, 1, 200))
            .ToListAsync(cancellationToken);

        await HydrateDraftDisplayAsync(items, cancellationToken);
        return (items, total);
    }

    public Task<CustomerQuoteDraft> AddDraftFromQuoteItemAsync(
        string userId,
        string quoteItemId,
        CancellationToken cancellationToken = default) =>
        AddDraftCoreAsync(userId, quoteItemId.Trim(), cancellationToken);

    public async Task<IReadOnlyList<CustomerQuoteDraft>> AddDraftsFromQuoteAsync(
        string userId,
        string quoteId,
        CancellationToken cancellationToken = default)
    {
        var qid = quoteId.Trim();
        var itemIds = await _db.QuoteItems.AsNoTracking()
            .Where(i => !i.IsDeleted && i.QuoteId == qid)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);
        if (itemIds.Count == 0)
            throw new InvalidOperationException("报价单无有效明细行");

        var results = new List<CustomerQuoteDraft>();
        foreach (var itemId in itemIds)
        {
            try
            {
                results.Add(await AddDraftCoreAsync(userId, itemId, cancellationToken));
            }
            catch (InvalidOperationException)
            {
                // skip occupied / duplicate
            }
        }

        if (results.Count == 0)
            throw new InvalidOperationException("所选报价明细均已加入草稿或已被占用");
        return results;
    }

    public async Task DeleteDraftAsync(string userId, string draftId, CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        var row = await _db.CustomerQuoteDrafts.FirstOrDefaultAsync(
            d => d.Id == draftId.Trim() && !d.IsDeleted, cancellationToken);
        if (row == null)
            throw new KeyNotFoundException("草稿不存在");
        if (row.CreateByUserId != uid)
            throw new UnauthorizedAccessException("无权删除该草稿");
        if (row.Status != CustomerQuoteDraftStatus.Draft)
            throw new InvalidOperationException("仅草稿状态可删除");

        row.IsDeleted = true;
        row.ModifyTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CustomerQuote> GenerateFromDraftsAsync(
        string userId,
        IReadOnlyList<string> draftIds,
        CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        var ids = draftIds.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().ToList();
        if (ids.Count == 0)
            throw new InvalidOperationException("请选择草稿");

        var drafts = await _db.CustomerQuoteDrafts
            .Where(d => ids.Contains(d.Id) && !d.IsDeleted && d.Status == CustomerQuoteDraftStatus.Draft && d.CreateByUserId == uid)
            .ToListAsync(cancellationToken);
        if (drafts.Count != ids.Count)
            throw new InvalidOperationException("部分草稿不存在或无权操作");

        CustomerQuoteGenerateRules.AssertDraftsSameCustomerAndSalesUser(
            drafts.Select(d => d.CustomerId),
            drafts.Select(d => d.SalesUserId));

        foreach (var d in drafts)
        {
            if (await IsQuoteItemOccupiedAsync(d.SourceQuoteItemId, excludeDraftId: d.Id, cancellationToken))
                throw new InvalidOperationException($"采购报价行 {d.SourceQuoteCode ?? d.SourceQuoteItemId} 已被占用");
        }

        var code = await _serialNumberService.GenerateNextAsync(ModuleCodes.CustomerQuote);
        var groupId = Guid.NewGuid().ToString();
        var customerId = CustomerQuoteGenerateRules.FirstNonEmpty(drafts.Select(d => d.CustomerId));
        var salesUserId = CustomerQuoteGenerateRules.FirstNonEmpty(drafts.Select(d => d.SalesUserId));
        var contact = await ResolvePrimaryContactAsync(customerId, cancellationToken);

        var header = new CustomerQuote
        {
            GroupId = groupId,
            CustomerQuoteCode = code,
            VersionNo = 1,
            Status = CustomerQuoteStatus.Unsent,
            CustomerId = customerId,
            CustomerContactId = contact?.Id,
            ContactName = contact?.ContactName,
            ContactEmail = contact?.Email,
            SalesUserId = salesUserId,
            ProfitFactor = 1.00m,
            CreateByUserId = uid,
            ModifyByUserId = uid
        };
        _db.CustomerQuotes.Add(header);

        var lineNo = 1;
        foreach (var d in drafts.OrderBy(x => x.CreateTime))
        {
            _db.CustomerQuoteItems.Add(new CustomerQuoteItem
            {
                CustomerQuoteId = header.Id,
                LineNo = lineNo++,
                SourceQuoteItemId = d.SourceQuoteItemId,
                SourceQuoteId = d.SourceQuoteId,
                RfqItemId = d.RfqItemId,
                Mpn = d.Mpn,
                Brand = d.Brand,
                Quantity = d.Quantity,
                PurchasePrice = d.PurchasePrice,
                PurchaseCurrency = d.PurchaseCurrency,
                SendPrice = d.PurchasePrice,
                SendCurrency = d.PurchaseCurrency,
                CustomerMpn = d.CustomerMpn,
                CustomerBrand = d.CustomerBrand,
                LeadTime = d.LeadTime,
                DateCode = d.DateCode,
                Remark = d.Remark,
                SourceQuoteCode = d.SourceQuoteCode,
                SourceQuoteDate = d.SourceQuoteDate,
                PurchaseUserId = d.PurchaseUserId
            });
            d.Status = CustomerQuoteDraftStatus.Converted;
            d.CustomerQuoteId = header.Id;
            d.ModifyTime = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (await GetQuoteByIdAsync(uid, header.Id, cancellationToken))!;
    }

    public async Task<(IReadOnlyList<CustomerQuote> Items, int Total)> GetQuotesPagedAsync(
        string? userId,
        int page,
        int pageSize,
        short? status,
        string? keyword,
        CancellationToken cancellationToken = default)
    {
        var q = _db.CustomerQuotes.AsNoTracking().Where(c => !c.IsDeleted);
        if (status.HasValue)
            q = q.Where(c => c.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            q = q.Where(c => EF.Functions.ILike(c.CustomerQuoteCode, $"%{kw}%"));
        }

        q = await _dataPermission.ApplyCustomerQuoteListDataScopeAsync(userId, q, cancellationToken);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(c => c.CreateTime)
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(Math.Clamp(pageSize, 1, 200))
            .ToListAsync(cancellationToken);

        await HydrateQuoteHeaderDisplayAsync(items, cancellationToken);
        await HydrateQuoteListSummariesAsync(items, cancellationToken);
        return (items, total);
    }

    public async Task<CustomerQuote?> GetQuoteByIdAsync(string? userId, string id, CancellationToken cancellationToken = default)
    {
        var row = await _db.CustomerQuotes.AsNoTracking()
            .Include(c => c.Items.Where(i => !i.IsDeleted).OrderBy(i => i.LineNo))
            .FirstOrDefaultAsync(c => c.Id == id.Trim() && !c.IsDeleted, cancellationToken);
        if (row == null)
            return null;

        if (!await CanAccessQuoteAsync(userId, row, cancellationToken))
            return null;

        await HydrateQuoteHeaderDisplayAsync(new[] { row }, cancellationToken);
        await HydrateQuoteItemUsersAsync(row.Items, cancellationToken);
        return row;
    }

    public async Task<CustomerQuote> UpdateQuoteAsync(
        string userId,
        string id,
        UpdateCustomerQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        var header = await _db.CustomerQuotes
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == id.Trim() && !c.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("客户报价单不存在");

        if (!await CanAccessQuoteAsync(uid, header, cancellationToken))
            throw new UnauthorizedAccessException("无权编辑该客户报价单");
        if (header.Status != CustomerQuoteStatus.Unsent)
            throw new InvalidOperationException("仅未发送版本可编辑");

        if (request.CustomerContactId != null)
            header.CustomerContactId = string.IsNullOrWhiteSpace(request.CustomerContactId) ? null : request.CustomerContactId.Trim();
        if (request.ContactName != null)
            header.ContactName = request.ContactName.Trim();
        if (request.ContactEmail != null)
            header.ContactEmail = request.ContactEmail.Trim();
        if (request.ProfitFactor.HasValue)
            header.ProfitFactor = Math.Round(request.ProfitFactor.Value, 2);
        if (request.Remark != null)
            header.Remark = TruncateRemark(request.Remark);

        if (request.Items != null)
        {
            foreach (var patch in request.Items)
            {
                var item = header.Items.FirstOrDefault(i => i.Id == patch.Id.Trim());
                if (item == null) continue;
                if (patch.SendPrice.HasValue)
                    item.SendPrice = patch.SendPrice.Value;
                if (patch.SendCurrency.HasValue)
                    item.SendCurrency = patch.SendCurrency.Value;
                if (patch.IsLocked.HasValue)
                    item.IsLocked = patch.IsLocked.Value;
                if (patch.LeadTime != null)
                    item.LeadTime = patch.LeadTime;
                if (patch.DateCode != null)
                    item.DateCode = patch.DateCode;
                if (patch.Remark != null)
                    item.Remark = patch.Remark;
            }
        }

        header.ModifyByUserId = uid;
        header.ModifyTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetQuoteByIdAsync(uid, header.Id, cancellationToken))!;
    }

    public async Task<CustomerQuote> ApplyProfitFactorAsync(
        string userId,
        string id,
        CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        var header = await _db.CustomerQuotes
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == id.Trim() && !c.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("客户报价单不存在");

        if (!await CanAccessQuoteAsync(uid, header, cancellationToken))
            throw new UnauthorizedAccessException("无权操作");
        if (header.Status != CustomerQuoteStatus.Unsent)
            throw new InvalidOperationException("仅未发送版本可设置发送报价");

        var factor = header.ProfitFactor;
        foreach (var item in header.Items.Where(i => !i.IsLocked))
        {
            item.SendPrice = Math.Round(item.PurchasePrice * factor, 6, MidpointRounding.AwayFromZero);
            item.SendCurrency = item.PurchaseCurrency;
        }

        header.ModifyByUserId = uid;
        header.ModifyTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetQuoteByIdAsync(uid, header.Id, cancellationToken))!;
    }

    public async Task<(CustomerQuote Quote, CustomerQuoteBillTo BillTo)> GetReportDataAsync(
        string? userId,
        string id,
        CancellationToken cancellationToken = default)
    {
        var quote = await GetQuoteByIdAsync(userId, id, cancellationToken)
            ?? throw new KeyNotFoundException("客户报价单不存在或无权查看");

        var billTo = new CustomerQuoteBillTo
        {
            Company = quote.CustomerName,
            Attn = quote.ContactName,
            Email = quote.ContactEmail
        };

        if (!string.IsNullOrWhiteSpace(quote.CustomerId))
        {
            var cust = await _db.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == quote.CustomerId, cancellationToken);
            if (cust != null)
            {
                billTo.Company = string.IsNullOrWhiteSpace(cust.OfficialName) ? quote.CustomerName : cust.OfficialName;
                billTo.CompanyEn = cust.EnglishOfficialName;
            }

            var addr = await _db.CustomerAddresses.AsNoTracking()
                .Where(a => !a.IsDeleted && a.CustomerId == quote.CustomerId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.AddressType)
                .ThenBy(a => a.CreateTime)
                .FirstOrDefaultAsync(cancellationToken);
            billTo.Address = FormatCustomerAddress(addr);
        }

        if (!string.IsNullOrWhiteSpace(quote.CustomerContactId))
        {
            var contact = await _db.CustomerContacts.AsNoTracking()
                .FirstOrDefaultAsync(
                    c => !c.IsDeleted && c.Id == quote.CustomerContactId,
                    cancellationToken);
            if (contact != null)
            {
                if (string.IsNullOrWhiteSpace(billTo.Attn))
                    billTo.Attn = contact.ContactName;
                billTo.Tel = FirstNonEmpty(contact.Mobile, contact.Tel);
                if (string.IsNullOrWhiteSpace(billTo.Email))
                    billTo.Email = contact.Email;
                if (string.IsNullOrWhiteSpace(billTo.Address))
                    billTo.Address = contact.Address;
            }
        }

        return (quote, billTo);
    }

    public async Task<CustomerQuote> MarkSentByEmailAsync(
        string userId,
        string id,
        CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        var header = await _db.CustomerQuotes
            .FirstOrDefaultAsync(c => c.Id == id.Trim() && !c.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("客户报价单不存在");

        if (!await CanAccessQuoteAsync(uid, header, cancellationToken))
            throw new UnauthorizedAccessException("无权发送该客户报价单");

        CustomerQuoteSendRules.ApplySentByEmail(header, DateTime.UtcNow);
        header.ModifyByUserId = uid;
        header.ModifyTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetQuoteByIdAsync(uid, header.Id, cancellationToken))!;
    }

    public async Task<CustomerQuote> MarkSentAsync(
        string userId,
        string id,
        CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        var header = await _db.CustomerQuotes
            .FirstOrDefaultAsync(c => c.Id == id.Trim() && !c.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("客户报价单不存在");

        if (!await CanAccessQuoteAsync(uid, header, cancellationToken))
            throw new UnauthorizedAccessException("无权操作该客户报价单");

        CustomerQuoteSendRules.ApplyMarkSent(header, DateTime.UtcNow);
        header.ModifyByUserId = uid;
        header.ModifyTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetQuoteByIdAsync(uid, header.Id, cancellationToken))!;
    }

    public async Task DeleteQuoteAsync(string userId, string id, CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        var header = await _db.CustomerQuotes
            .FirstOrDefaultAsync(c => c.Id == id.Trim() && !c.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("客户报价单不存在");

        if (!await CanAccessQuoteAsync(uid, header, cancellationToken))
            throw new UnauthorizedAccessException("无权删除该客户报价单");
        if (!CustomerQuoteSendRules.CanDelete(header.Status))
            throw new InvalidOperationException(CustomerQuoteSendRules.OnlyUnsentCanDeleteMessage);

        header.IsDeleted = true;
        header.ModifyByUserId = uid;
        header.ModifyTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AppendActionLogAsync(
        string userId,
        string id,
        string actionType,
        string? remark,
        CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        var header = await _db.CustomerQuotes
            .FirstOrDefaultAsync(c => c.Id == id.Trim() && !c.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("客户报价单不存在");

        if (!await CanAccessQuoteAsync(uid, header, cancellationToken))
            throw new UnauthorizedAccessException("无权操作该客户报价单");
        if (!CustomerQuoteActionLogRules.IsDisplayAction(actionType))
            throw new InvalidOperationException(CustomerQuoteActionLogRules.InvalidActionMessage);

        var operatorName = await ResolveOperatorLoginNameAsync(uid, cancellationToken);
        var note = string.IsNullOrWhiteSpace(remark) ? null : TruncateRemark(remark);
        await _logOperationAppend.AppendAsync(
            BusinessLogTypes.CustomerQuote,
            header.Id,
            header.DisplayCode,
            actionType,
            uid,
            operatorName,
            actionType,
            note,
            null,
            cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerQuoteActionLogRow>> GetActionLogsAsync(
        string? userId,
        string id,
        CancellationToken cancellationToken = default)
    {
        var header = await _db.CustomerQuotes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id.Trim() && !c.IsDeleted, cancellationToken);
        if (header == null)
            throw new KeyNotFoundException("客户报价单不存在");
        if (!await CanAccessQuoteAsync(userId, header, cancellationToken))
            throw new UnauthorizedAccessException("无权查看该客户报价单");

        var rows = await _db.OperationLogs.AsNoTracking()
            .Where(o => o.BizType == BusinessLogTypes.CustomerQuote
                        && o.RecordId == header.Id
                        && (o.ActionType == CustomerQuoteActionLogRules.Print
                            || o.ActionType == CustomerQuoteActionLogRules.Export
                            || o.ActionType == CustomerQuoteActionLogRules.SendEmail))
            .OrderByDescending(o => o.OperationTime)
            .Take(200)
            .Select(o => new CustomerQuoteActionLogRow
            {
                Id = o.Id,
                OperationTime = o.OperationTime,
                OperatorUserName = o.OperatorUserName,
                ActionType = o.ActionType,
                Remark = o.Reason
            })
            .ToListAsync(cancellationToken);
        return rows;
    }

    private async Task<string> ResolveOperatorLoginNameAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return EntityLookupService.FormatUserLoginName(user) ?? userId;
    }

    private static string? TruncateRemark(string? value)
    {
        var t = value?.Trim();
        if (string.IsNullOrEmpty(t))
            return null;
        return t.Length <= 2000 ? t : t[..2000];
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            var t = v?.Trim();
            if (!string.IsNullOrEmpty(t))
                return t;
        }

        return null;
    }

    private static string? FormatCustomerAddress(CustomerAddress? addr)
    {
        if (addr == null) return null;
        var parts = new[]
        {
            addr.CountryName,
            addr.Province,
            addr.City,
            addr.Area,
            addr.Address,
            string.IsNullOrWhiteSpace(addr.ZipCode) ? null : addr.ZipCode.Trim()
        };
        var text = string.Join(" ", parts.Select(p => p?.Trim()).Where(p => !string.IsNullOrEmpty(p)));
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private async Task<CustomerQuoteDraft> AddDraftCoreAsync(
        string userId,
        string quoteItemId,
        CancellationToken cancellationToken)
    {
        var uid = userId.Trim();
        if (await _db.CustomerQuoteDrafts.AnyAsync(
                d => !d.IsDeleted && d.Status == CustomerQuoteDraftStatus.Draft && d.SourceQuoteItemId == quoteItemId,
                cancellationToken))
            throw new InvalidOperationException("该采购报价行已在草稿中");

        if (await IsQuoteItemOccupiedAsync(quoteItemId, excludeDraftId: null, cancellationToken))
            throw new InvalidOperationException("该采购报价行已被客户报价单占用");

        var snap = await LoadQuoteItemSnapshotAsync(quoteItemId, cancellationToken)
            ?? throw new KeyNotFoundException("采购报价行不存在");

        if (snap.Customer != null && !await _dataPermission.CanAccessCustomerAsync(uid, snap.Customer))
            throw new UnauthorizedAccessException("无权访问该客户报价");

        var draft = new CustomerQuoteDraft
        {
            SourceQuoteItemId = snap.QuoteItemId,
            SourceQuoteId = snap.QuoteId,
            RfqItemId = snap.RfqItemId,
            CustomerId = snap.CustomerId,
            SalesUserId = snap.SalesUserId,
            PurchaseUserId = snap.PurchaseUserId,
            Mpn = snap.Mpn,
            Brand = snap.Brand,
            Quantity = snap.Quantity,
            PurchasePrice = snap.PurchasePrice,
            PurchaseCurrency = snap.PurchaseCurrency,
            CustomerMpn = snap.CustomerMpn,
            CustomerBrand = snap.CustomerBrand,
            SourceQuoteCode = snap.SourceQuoteCode,
            SourceQuoteDate = snap.SourceQuoteDate,
            LeadTime = snap.LeadTime,
            DateCode = snap.DateCode,
            Remark = snap.Remark,
            Status = CustomerQuoteDraftStatus.Draft,
            CreateByUserId = uid
        };
        _db.CustomerQuoteDrafts.Add(draft);
        await _db.SaveChangesAsync(cancellationToken);

        await HydrateDraftDisplayAsync(new[] { draft }, cancellationToken);
        return draft;
    }

    private async Task<bool> IsQuoteItemOccupiedAsync(
        string quoteItemId,
        string? excludeDraftId,
        CancellationToken cancellationToken)
    {
        var inFormal = await (
            from i in _db.CustomerQuoteItems.AsNoTracking()
            join c in _db.CustomerQuotes.AsNoTracking() on i.CustomerQuoteId equals c.Id
            where !i.IsDeleted && i.SourceQuoteItemId == quoteItemId
                  && !c.IsDeleted && c.Status != CustomerQuoteStatus.Void
            select i.Id).AnyAsync(cancellationToken);
        if (inFormal) return true;

        var draftQ = _db.CustomerQuoteDrafts.AsNoTracking()
            .Where(d => !d.IsDeleted && d.SourceQuoteItemId == quoteItemId && d.Status == CustomerQuoteDraftStatus.Draft);
        if (!string.IsNullOrWhiteSpace(excludeDraftId))
            draftQ = draftQ.Where(d => d.Id != excludeDraftId);
        return await draftQ.AnyAsync(cancellationToken);
    }

    private async Task<bool> CanAccessQuoteAsync(string? userId, CustomerQuote quote, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return true;
        var scoped = await _dataPermission.ApplyCustomerQuoteListDataScopeAsync(
            userId, _db.CustomerQuotes.AsNoTracking().Where(c => c.Id == quote.Id), cancellationToken);
        return await scoped.AnyAsync(cancellationToken);
    }

    private async Task<QuoteItemSnapshot?> LoadQuoteItemSnapshotAsync(string quoteItemId, CancellationToken cancellationToken)
    {
        var qi = await _db.QuoteItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == quoteItemId && !i.IsDeleted, cancellationToken);
        if (qi == null) return null;

        var quote = await _db.Quotes.AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == qi.QuoteId && !q.IsDeleted, cancellationToken);
        if (quote == null) return null;

        RFQItem? rfqItem = null;
        RFQ? rfq = null;
        if (!string.IsNullOrWhiteSpace(quote.RFQItemId))
        {
            rfqItem = await _db.RFQItems.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == quote.RFQItemId && !r.IsDeleted, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(quote.RFQId))
        {
            rfq = await _db.RFQs.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == quote.RFQId && !r.IsDeleted, cancellationToken);
        }

        var customerId = quote.CustomerId ?? rfq?.CustomerId;
        CustomerInfo? customer = null;
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            customer = await _db.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted, cancellationToken);
        }

        return new QuoteItemSnapshot
        {
            QuoteItemId = qi.Id,
            QuoteId = qi.QuoteId,
            RfqItemId = quote.RFQItemId,
            CustomerId = customerId,
            Customer = customer,
            SalesUserId = quote.SalesUserId ?? rfq?.SalesUserId,
            PurchaseUserId = quote.PurchaseUserId,
            Mpn = qi.Mpn ?? quote.Mpn,
            Brand = qi.Brand,
            Quantity = qi.Quantity,
            PurchasePrice = qi.UnitPrice,
            PurchaseCurrency = qi.Currency,
            CustomerMpn = rfqItem?.CustomerMpn,
            CustomerBrand = rfqItem?.CustomerBrand,
            SourceQuoteCode = quote.QuoteCode,
            SourceQuoteDate = quote.QuoteDate,
            LeadTime = qi.LeadTime,
            DateCode = qi.DateCode,
            Remark = qi.Remark
        };
    }

    private async Task<CustomerContactInfo?> ResolvePrimaryContactAsync(string? customerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerId)) return null;
        return await _db.CustomerContacts.AsNoTracking()
            .Where(c => !c.IsDeleted && c.CustomerId == customerId)
            .OrderByDescending(c => c.IsMain)
            .ThenBy(c => c.CreateTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task HydrateDraftDisplayAsync(IReadOnlyList<CustomerQuoteDraft> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0) return;
        var customerIds = items.Select(i => i.CustomerId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var customers = await _db.Customers.AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var userIds = items.SelectMany(i => new[] { i.SalesUserId, i.PurchaseUserId })
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        foreach (var d in items)
        {
            if (d.CustomerId != null && customers.TryGetValue(d.CustomerId, out var cust))
                d.CustomerName = cust.CustomerName;
            if (d.SalesUserId != null && users.TryGetValue(d.SalesUserId, out var su))
                d.SalesUserName = EntityLookupService.FormatUserLoginName(su);
            if (d.PurchaseUserId != null && users.TryGetValue(d.PurchaseUserId, out var pu))
                d.PurchaseUserName = pu.RealName ?? pu.UserName;
        }
    }

    private async Task HydrateQuoteHeaderDisplayAsync(IReadOnlyList<CustomerQuote> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0) return;
        var customerIds = items.Select(i => i.CustomerId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var customers = await _db.Customers.AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);
        var userIds = items
            .SelectMany(i => new[] { i.SalesUserId, i.CreateByUserId })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct()
            .ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        foreach (var q in items)
        {
            if (q.CustomerId != null && customers.TryGetValue(q.CustomerId, out var cust))
                q.CustomerName = cust.CustomerName;
            if (q.SalesUserId != null && users.TryGetValue(q.SalesUserId, out var su))
                q.SalesUserName = EntityLookupService.FormatUserLoginName(su);
            if (q.CreateByUserId != null && users.TryGetValue(q.CreateByUserId, out var cu))
                q.CreateByUserName = EntityLookupService.FormatUserLoginName(cu);
        }
    }

    private async Task HydrateQuoteListSummariesAsync(
        IReadOnlyList<CustomerQuote> items,
        CancellationToken cancellationToken)
    {
        var ids = items.Select(i => i.Id).ToList();
        if (ids.Count == 0) return;

        var counts = await _db.CustomerQuoteItems.AsNoTracking()
            .Where(i => ids.Contains(i.CustomerQuoteId) && !i.IsDeleted)
            .GroupBy(i => i.CustomerQuoteId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        foreach (var q in items)
            q.ItemCount = counts.GetValueOrDefault(q.Id);
    }

    private async Task HydrateQuoteItemUsersAsync(IEnumerable<CustomerQuoteItem> items, CancellationToken cancellationToken)
    {
        var userIds = items.Select(i => i.PurchaseUserId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (userIds.Count == 0) return;
        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);
        foreach (var item in items)
        {
            if (item.PurchaseUserId != null && users.TryGetValue(item.PurchaseUserId, out var pu))
                item.PurchaseUserName = pu.RealName ?? pu.UserName;
        }
    }

    private sealed class QuoteItemSnapshot
    {
        public string QuoteItemId { get; set; } = string.Empty;
        public string QuoteId { get; set; } = string.Empty;
        public string? RfqItemId { get; set; }
        public string? CustomerId { get; set; }
        public CustomerInfo? Customer { get; set; }
        public string? SalesUserId { get; set; }
        public string? PurchaseUserId { get; set; }
        public string? Mpn { get; set; }
        public string? Brand { get; set; }
        public decimal Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public short PurchaseCurrency { get; set; }
        public string? CustomerMpn { get; set; }
        public string? CustomerBrand { get; set; }
        public string? SourceQuoteCode { get; set; }
        public DateTime? SourceQuoteDate { get; set; }
        public string? LeadTime { get; set; }
        public string? DateCode { get; set; }
        public string? Remark { get; set; }
    }
}
