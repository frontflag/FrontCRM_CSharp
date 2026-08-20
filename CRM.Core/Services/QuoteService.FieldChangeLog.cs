using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Quote;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public partial class QuoteService
    {
        private static string SqlQ(string? s) => (s ?? "").Replace("'", "''", StringComparison.Ordinal);

        public async Task<IReadOnlyList<QuoteFieldChangeLogDto>> GetFieldChangeLogsAsync(string quoteId)
        {
            if (string.IsNullOrWhiteSpace(quoteId))
                return Array.Empty<QuoteFieldChangeLogDto>();
            var safe = SqlQ(quoteId.Trim());
            var headerBiz = BusinessLogTypes.Quote;
            var itemBiz = BusinessLogTypes.QuoteItem;
            var sql = $@"
SELECT c.""Id"",
       '{safe}' AS ""QuoteId"",
       q.quote_code AS ""QuoteCode"",
       c.""FieldName"",
       c.""FieldLabel"",
       c.""OldValue"",
       c.""NewValue"",
       c.""ChangedByUserId"",
       c.""ChangedByUserName"",
       c.""ChangedAt"",
       CASE
         WHEN c.""BizType"" = '{headerBiz}' THEN '主表'
         ELSE COALESCE(NULLIF(TRIM(c.""RecordCode""), ''), '明细')
       END AS ""ObjectLabel""
FROM log_change_fldval c
LEFT JOIN quote q ON q.""QuoteId"" = '{safe}'
WHERE (
    c.""BizType"" = '{headerBiz}' AND c.""RecordId"" = '{safe}'
) OR (
    c.""BizType"" = '{itemBiz}' AND c.""RecordId"" IN (
        SELECT i.""QuoteItemId"" FROM quoteitem i WHERE i.quote_id = '{safe}'
    )
)
ORDER BY c.""ChangedAt"" DESC";
            var rows = await _unitOfWork.QueryAsync<QuoteFieldChangeLogDto>(sql);
            return rows.ToList();
        }

        public async Task<IReadOnlyList<QuoteDeletedOnRfqItemDto>> GetDeletedQuotesByRfqItemIdsAsync(
            IReadOnlyCollection<string> rfqItemIds)
        {
            var ids = (rfqItemIds ?? Array.Empty<string>())
                .Select(id => id?.Trim())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(2000)
                .ToList();
            if (ids.Count == 0)
                return Array.Empty<QuoteDeletedOnRfqItemDto>();

            var inList = string.Join(",", ids.Select(id => $"'{SqlQ(id)}'"));
            var action = OperationLogActionTypes.QuoteHeaderDelete;
            var sql = $@"
SELECT q.""QuoteId"" AS ""QuoteId"",
       q.quote_code AS ""QuoteCode"",
       q.rfq_item_id AS ""RfqItemId"",
       COALESCE(i.line_no, 0) AS ""LineNo"",
       COALESCE(NULLIF(TRIM(i.mpn), ''), NULLIF(TRIM(q.mpn), '')) AS ""Mpn"",
       i.brand AS ""Brand"",
       q.""CreateTime"" AS ""QuoteCreatedAt"",
       qi_agg.vendor_name AS ""VendorName"",
       qi_agg.unit_price_text AS ""UnitPriceText"",
       qi_agg.currency_text AS ""CurrencyText"",
       COALESCE(NULLIF(TRIM(pu.""UserName""), ''), NULLIF(TRIM(cu.""UserName""), ''), '') AS ""PurchaseUserName"",
       COALESCE(del_op.""OperationTime"", q.""ModifyTime"") AS ""DeletedAt"",
       del_op.""OperatorUserId"" AS ""DeletedByUserId"",
       COALESCE(NULLIF(TRIM(del_op.""OperatorUserName""), ''), '') AS ""DeletedByUserName""
FROM quote q
LEFT JOIN rfqitem i ON i.item_id = q.rfq_item_id
LEFT JOIN ""user"" pu ON pu.""UserId"" = q.purchase_user_id
LEFT JOIN ""user"" cu ON cu.""UserId"" = q.create_by_user_id
LEFT JOIN LATERAL (
    SELECT
      string_agg(DISTINCT NULLIF(TRIM(qi.vendor_name), ''), '、') AS vendor_name,
      string_agg(
        to_char(ROUND(qi.unit_price, 4), 'FM9999999990.0000'),
        chr(10) ORDER BY qi.""CreateTime"", qi.""QuoteItemId""
      ) AS unit_price_text,
      string_agg(
        CASE qi.currency
          WHEN 2 THEN 'USD'
          WHEN 3 THEN 'EUR'
          WHEN 4 THEN 'HKD'
          WHEN 5 THEN 'JPY'
          WHEN 6 THEN 'GBP'
          ELSE 'RMB'
        END,
        chr(10) ORDER BY qi.""CreateTime"", qi.""QuoteItemId""
      ) AS currency_text
    FROM quoteitem qi
    WHERE qi.quote_id = q.""QuoteId""
) qi_agg ON true
LEFT JOIN LATERAL (
    SELECT o.""OperatorUserId"", o.""OperatorUserName"", o.""OperationTime""
    FROM log_operation o
    WHERE o.""BizType"" = '{BusinessLogTypes.Quote}'
      AND o.""RecordId"" = q.""QuoteId""
      AND o.""ActionType"" = '{action}'
    ORDER BY o.""OperationTime"" DESC
    LIMIT 1
) del_op ON true
WHERE q.is_deleted = true
  AND q.rfq_item_id IN ({inList})
ORDER BY COALESCE(del_op.""OperationTime"", q.""ModifyTime"") DESC NULLS LAST, q.quote_code";
            var rows = await _unitOfWork.QueryAsync<QuoteDeletedOnRfqItemDto>(sql);
            return rows.ToList();
        }

        private async Task<(string? UserId, string UserName)> ResolveFieldChangeActorAsync(string? actingUserId) =>
            await OperationLogActorResolver.ResolveAsync(_userService, actingUserId);

        private async Task<string?> ResolveUserDisplayNameAsync(string? userId)
        {
            var id = ActingUserIdNormalizer.Normalize(userId);
            if (string.IsNullOrEmpty(id))
                return string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
            var user = await _userService.GetByIdAsync(id);
            return string.IsNullOrWhiteSpace(user?.UserName) ? id : user!.UserName!.Trim();
        }

        private async Task<string?> ResolveCustomerDisplayNameAsync(string? customerId)
        {
            var id = customerId?.Trim();
            if (string.IsNullOrEmpty(id))
                return null;
            var list = await _customerRepository.FindAsync(c => c.Id == id);
            var cust = list.FirstOrDefault();
            if (cust == null)
                return id;
            if (!string.IsNullOrWhiteSpace(cust.OfficialName)) return cust.OfficialName.Trim();
            if (!string.IsNullOrWhiteSpace(cust.NickName)) return cust.NickName.Trim();
            return cust.CustomerCode?.Trim() ?? id;
        }

        private async Task AppendQuoteFieldChangeLogAsync(
            Quote quote,
            string fieldName,
            string fieldLabel,
            string? oldValue,
            string? newValue,
            string? actingUserId)
        {
            var (userId, userName) = await ResolveFieldChangeActorAsync(actingUserId);
            await FieldChangeLogAppender.AppendIfChangedAsync(
                _unitOfWork,
                BusinessLogTypes.Quote,
                quote.Id,
                quote.QuoteCode,
                fieldName,
                fieldLabel,
                oldValue,
                newValue,
                userId,
                userName);
        }

        private async Task AppendQuoteItemFieldChangeLogAsync(
            Quote quote,
            QuoteItem item,
            string? lineCode,
            string fieldName,
            string fieldLabel,
            string? oldValue,
            string? newValue,
            string? actingUserId)
        {
            var (userId, userName) = await ResolveFieldChangeActorAsync(actingUserId);
            await FieldChangeLogAppender.AppendIfChangedAsync(
                _unitOfWork,
                BusinessLogTypes.QuoteItem,
                item.Id,
                lineCode ?? BuildQuoteItemLineCode(quote, item, new[] { item }),
                fieldName,
                fieldLabel,
                oldValue,
                newValue,
                userId,
                userName);
        }

        private sealed record QuoteHeaderSnapshot(
            string? Mpn,
            string? CustomerId,
            string? SalesUserId,
            string? PurchaseUserId,
            DateTime QuoteDate,
            string? Remark);

        private static QuoteHeaderSnapshot CaptureQuoteHeaderSnapshot(Quote quote) =>
            new(
                quote.Mpn,
                quote.CustomerId,
                quote.SalesUserId,
                quote.PurchaseUserId,
                quote.QuoteDate,
                quote.Remark);

        private async Task LogQuoteHeaderFieldChangesAsync(
            Quote quote,
            QuoteHeaderSnapshot before,
            string? actingUserId)
        {
            var after = CaptureQuoteHeaderSnapshot(quote);
            await CompareAndLogQuoteHeaderFieldAsync(quote, before.Mpn, after.Mpn, "mpn", "物料型号", actingUserId);

            var oldCustomer = await ResolveCustomerDisplayNameAsync(before.CustomerId);
            var newCustomer = await ResolveCustomerDisplayNameAsync(after.CustomerId);
            await CompareAndLogQuoteHeaderFieldAsync(quote, oldCustomer, newCustomer, "customerId", "客户", actingUserId);

            var oldSales = await ResolveUserDisplayNameAsync(before.SalesUserId);
            var newSales = await ResolveUserDisplayNameAsync(after.SalesUserId);
            await CompareAndLogQuoteHeaderFieldAsync(quote, oldSales, newSales, "salesUserId", "业务员", actingUserId);

            var oldPurchase = await ResolveUserDisplayNameAsync(before.PurchaseUserId);
            var newPurchase = await ResolveUserDisplayNameAsync(after.PurchaseUserId);
            await CompareAndLogQuoteHeaderFieldAsync(quote, oldPurchase, newPurchase, "purchaseUserId", "采购员", actingUserId);

            await CompareAndLogQuoteHeaderFieldAsync(
                quote,
                FormatQuoteDate(before.QuoteDate),
                FormatQuoteDate(after.QuoteDate),
                "quoteDate",
                "报价日期",
                actingUserId);
            await CompareAndLogQuoteHeaderFieldAsync(quote, before.Remark, after.Remark, "remark", "备注", actingUserId);
        }

        private async Task CompareAndLogQuoteHeaderFieldAsync(
            Quote quote,
            string? oldVal,
            string? newVal,
            string fieldName,
            string fieldLabel,
            string? actingUserId)
        {
            await AppendQuoteFieldChangeLogAsync(quote, fieldName, fieldLabel, oldVal, newVal, actingUserId);
        }

        private sealed record QuoteItemFieldSnapshot(
            string? VendorId,
            string? VendorName,
            string? VendorCode,
            string? ContactId,
            string? ContactName,
            string? PriceType,
            DateTime? ExpiryDate,
            string? Mpn,
            string? Brand,
            string? BrandOrigin,
            string? DateCode,
            string? LeadTime,
            short LabelType,
            short WaferOrigin,
            short PackageOrigin,
            bool FreeShipping,
            short Currency,
            decimal Quantity,
            decimal UnitPrice,
            decimal? ConvertedPrice,
            int MinPackageQty,
            string? MinPackageUnit,
            int StockQty,
            int Moq,
            string? Remark,
            short Status);

        private static QuoteItemFieldSnapshot CaptureQuoteItemFieldSnapshot(QuoteItem item) =>
            new(
                item.VendorId,
                item.VendorName,
                item.VendorCode,
                item.ContactId,
                item.ContactName,
                item.PriceType,
                item.ExpiryDate,
                item.Mpn,
                item.Brand,
                item.BrandOrigin,
                item.DateCode,
                item.LeadTime,
                item.LabelType,
                item.WaferOrigin,
                item.PackageOrigin,
                item.FreeShipping,
                item.Currency,
                item.Quantity,
                item.UnitPrice,
                item.ConvertedPrice,
                item.MinPackageQty,
                item.MinPackageUnit,
                item.StockQty,
                item.Moq,
                item.Remark,
                item.Status);

        private static string BuildQuoteItemLineCode(Quote quote, QuoteItem item, IReadOnlyList<QuoteItem> activeOrdered)
        {
            var tier = 0;
            foreach (var row in activeOrdered.OrderBy(i => i.CreateTime))
            {
                tier++;
                if (string.Equals(row.Id, item.Id, StringComparison.OrdinalIgnoreCase))
                    return $"{quote.QuoteCode}#{tier}";
            }
            return $"{quote.QuoteCode}#{item.Id[..Math.Min(8, item.Id.Length)]}";
        }

        private async Task LogQuoteItemFieldChangesAsync(
            Quote quote,
            QuoteItem item,
            QuoteItemFieldSnapshot before,
            IReadOnlyList<QuoteItem> activeOrdered,
            string? actingUserId)
        {
            var after = CaptureQuoteItemFieldSnapshot(item);
            var lineCode = BuildQuoteItemLineCode(quote, item, activeOrdered);

            await CompareAndLogQuoteItemFieldAsync(quote, item, lineCode, before.VendorName, after.VendorName, "vendorName", "供应商", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(quote, item, lineCode, before.VendorCode, after.VendorCode, "vendorCode", "供应商代码", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(quote, item, lineCode, before.ContactName, after.ContactName, "contactName", "联系人", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(quote, item, lineCode, before.PriceType, after.PriceType, "priceType", "价格类型", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(
                quote, item, lineCode,
                FormatQuoteDate(before.ExpiryDate),
                FormatQuoteDate(after.ExpiryDate),
                "expiryDate", "失效日期", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(quote, item, lineCode, before.Mpn, after.Mpn, "mpn", "物料型号", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(quote, item, lineCode, before.Brand, after.Brand, "brand", "品牌", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(quote, item, lineCode, before.BrandOrigin, after.BrandOrigin, "brandOrigin", "品牌属地", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(quote, item, lineCode, before.DateCode, after.DateCode, "dateCode", "DC", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(quote, item, lineCode, before.LeadTime, after.LeadTime, "leadTime", "交期", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(
                quote, item, lineCode,
                FormatLabelType(before.LabelType),
                FormatLabelType(after.LabelType),
                "labelType", "涂标", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(
                quote, item, lineCode,
                FormatOriginType(before.WaferOrigin),
                FormatOriginType(after.WaferOrigin),
                "waferOrigin", "晶圆产地", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(
                quote, item, lineCode,
                FormatOriginType(before.PackageOrigin),
                FormatOriginType(after.PackageOrigin),
                "packageOrigin", "封装产地", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(
                quote, item, lineCode,
                FormatBool(before.FreeShipping),
                FormatBool(after.FreeShipping),
                "freeShipping", "包邮", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(
                quote, item, lineCode,
                FormatCurrency(before.Currency),
                FormatCurrency(after.Currency),
                "currency", "币别", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(
                quote, item, lineCode,
                FormatDecimal4(before.Quantity),
                FormatDecimal4(after.Quantity),
                "quantity", "数量", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(
                quote, item, lineCode,
                FormatDecimal6(before.UnitPrice),
                FormatDecimal6(after.UnitPrice),
                "unitPrice", "单价", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(
                quote, item, lineCode,
                before.ConvertedPrice.HasValue ? FormatDecimal6(before.ConvertedPrice.Value) : null,
                after.ConvertedPrice.HasValue ? FormatDecimal6(after.ConvertedPrice.Value) : null,
                "convertedPrice", "折算价(USD)", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(
                quote, item, lineCode,
                before.MinPackageQty.ToString(),
                after.MinPackageQty.ToString(),
                "minPackageQty", "最小包装量", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(quote, item, lineCode, before.MinPackageUnit, after.MinPackageUnit, "minPackageUnit", "包装单位", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(
                quote, item, lineCode,
                before.StockQty.ToString(),
                after.StockQty.ToString(),
                "stockQty", "库存数量", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(
                quote, item, lineCode,
                before.Moq.ToString(),
                after.Moq.ToString(),
                "moq", "起订量", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(quote, item, lineCode, before.Remark, after.Remark, "remark", "备注", actingUserId);
            await CompareAndLogQuoteItemFieldAsync(
                quote, item, lineCode,
                FormatQuoteItemStatus(before.Status),
                FormatQuoteItemStatus(after.Status),
                "status", "明细状态", actingUserId);
        }

        private async Task CompareAndLogQuoteItemFieldAsync(
            Quote quote,
            QuoteItem item,
            string lineCode,
            string? oldVal,
            string? newVal,
            string fieldName,
            string fieldLabel,
            string? actingUserId)
        {
            await AppendQuoteItemFieldChangeLogAsync(quote, item, lineCode, fieldName, fieldLabel, oldVal, newVal, actingUserId);
        }

        private async Task LogQuoteItemAddedAsync(
            Quote quote,
            QuoteItem item,
            IReadOnlyList<QuoteItem> activeOrdered,
            string? actingUserId)
        {
            var lineCode = BuildQuoteItemLineCode(quote, item, activeOrdered);
            var vendor = item.VendorName ?? item.VendorCode ?? "—";
            var summary =
                $"{lineCode} · {vendor} · 数量 {FormatDecimal4(item.Quantity)} · 单价 {FormatDecimal6(item.UnitPrice)} {FormatCurrency(item.Currency)}";
            await AppendQuoteItemFieldChangeLogAsync(
                quote,
                item,
                lineCode,
                "lineAdded",
                "新增明细",
                null,
                summary,
                actingUserId);
        }

        private static string FormatQuoteDate(DateTime value) =>
            PostgreSqlDateTime.ToUtc(value).ToString("yyyy-MM-dd");

        private static string FormatQuoteDate(DateTime? value)
        {
            if (!value.HasValue)
                return string.Empty;
            return PostgreSqlDateTime.ToUtc(value.Value).ToString("yyyy-MM-dd");
        }

        private static string FormatDecimal4(decimal value) => value.ToString("0.####");
        private static string FormatDecimal6(decimal value) => value.ToString("0.######");
        private static string FormatBool(bool value) => value ? "是" : "否";

        private static string FormatCurrency(short currency) => currency switch
        {
            1 => "RMB",
            2 => "USD",
            3 => "EUR",
            4 => "HKD",
            5 => "JPY",
            6 => "GBP",
            _ => currency.ToString()
        };

        private static string FormatLabelType(short value) => value switch
        {
            0 => "不涂标",
            1 => "涂标",
            2 => "待确定",
            _ => value.ToString()
        };

        private static string FormatOriginType(short value) => value switch
        {
            0 => "美产",
            1 => "非美产",
            2 => "待确定",
            _ => value.ToString()
        };

        private static string FormatQuoteItemStatus(short value) => value switch
        {
            0 => "有效",
            1 => "已取消",
            _ => value.ToString()
        };
    }
}
