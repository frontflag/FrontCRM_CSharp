using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public partial class RFQService
    {
        private static string SqlQ(string? s) => (s ?? "").Replace("'", "''", StringComparison.Ordinal);

        public async Task<IReadOnlyList<RfqFieldChangeLogDto>> GetFieldChangeLogsAsync(string rfqId)
        {
            if (string.IsNullOrWhiteSpace(rfqId))
                return Array.Empty<RfqFieldChangeLogDto>();
            var safe = SqlQ(rfqId.Trim());
            var headerBiz = BusinessLogTypes.Rfq;
            var itemBiz = BusinessLogTypes.RfqItem;
            var sql = $@"
SELECT c.""Id"",
       '{safe}' AS ""RfqId"",
       r.rfq_code AS ""RfqCode"",
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
LEFT JOIN rfq r ON r.rfq_id = '{safe}'
WHERE (
    c.""BizType"" = '{headerBiz}' AND c.""RecordId"" = '{safe}'
) OR (
    c.""BizType"" = '{itemBiz}' AND c.""RecordId"" IN (
        SELECT i.item_id FROM rfqitem i WHERE i.rfq_id = '{safe}'
    )
)
ORDER BY c.""ChangedAt"" DESC";
            var rows = await _unitOfWork.QueryAsync<RfqFieldChangeLogDto>(sql);
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
            var customer = await _entityLookup.GetCustomerByIdAsync(id);
            if (customer == null)
                return id;
            return customer.OfficialName ?? customer.NickName ?? customer.CustomerCode ?? id;
        }

        private async Task<string?> ResolveContactDisplayNameAsync(string? contactId)
        {
            var id = contactId?.Trim();
            if (string.IsNullOrEmpty(id))
                return null;
            var contact = await _entityLookup.GetCustomerContactByIdAsync(id);
            return contact?.Name ?? id;
        }

        private async Task AppendRfqFieldChangeLogAsync(
            RFQ rfq,
            string fieldName,
            string fieldLabel,
            string? oldValue,
            string? newValue,
            string? actingUserId)
        {
            var (userId, userName) = await ResolveFieldChangeActorAsync(actingUserId);
            await FieldChangeLogAppender.AppendIfChangedAsync(
                _unitOfWork,
                BusinessLogTypes.Rfq,
                rfq.Id,
                rfq.RfqCode,
                fieldName,
                fieldLabel,
                oldValue,
                newValue,
                userId,
                userName);
        }

        private async Task AppendRfqItemFieldChangeLogAsync(
            RFQ rfq,
            RFQItem item,
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
                BusinessLogTypes.RfqItem,
                item.Id,
                lineCode ?? BuildRfqItemLineCode(rfq, item),
                fieldName,
                fieldLabel,
                oldValue,
                newValue,
                userId,
                userName);
        }

        private async Task AppendRfqStatusFieldChangeLogAsync(
            RFQ rfq,
            short oldStatus,
            short newStatus,
            string? actingUserId)
        {
            var (userId, userName) = await ResolveFieldChangeActorAsync(actingUserId);
            await RfqFieldChangeLogWriter.AppendRfqStatusChangeAsync(
                _unitOfWork,
                rfq,
                oldStatus,
                newStatus,
                userId,
                userName);
        }

        private sealed record RfqHeaderSnapshot(
            string? CustomerId,
            string? ContactId,
            string? ContactEmail,
            string? SalesUserId,
            short RfqType,
            short QuoteMethod,
            short AssignMethod,
            string? Industry,
            string? Product,
            short TargetType,
            short Importance,
            bool IsLastInquiry,
            string? ProjectBackground,
            string? Competitor,
            string? Remark);

        private static RfqHeaderSnapshot CaptureRfqHeaderSnapshot(RFQ rfq) =>
            new(
                rfq.CustomerId,
                rfq.ContactId,
                rfq.ContactEmail,
                rfq.SalesUserId,
                rfq.RfqType,
                rfq.QuoteMethod,
                rfq.AssignMethod,
                rfq.Industry,
                rfq.Product,
                rfq.TargetType,
                rfq.Importance,
                rfq.IsLastInquiry,
                rfq.ProjectBackground,
                rfq.Competitor,
                rfq.Remark);

        private async Task LogRfqHeaderFieldChangesAsync(RFQ rfq, RfqHeaderSnapshot before, string? actingUserId)
        {
            var after = CaptureRfqHeaderSnapshot(rfq);

            var oldCustomer = await ResolveCustomerDisplayNameAsync(before.CustomerId);
            var newCustomer = await ResolveCustomerDisplayNameAsync(after.CustomerId);
            await CompareAndLogRfqHeaderFieldAsync(rfq, oldCustomer, newCustomer, "customerId", "客户", actingUserId);

            var oldContact = await ResolveContactDisplayNameAsync(before.ContactId);
            var newContact = await ResolveContactDisplayNameAsync(after.ContactId);
            await CompareAndLogRfqHeaderFieldAsync(rfq, oldContact, newContact, "contactId", "联系人", actingUserId);

            await CompareAndLogRfqHeaderFieldAsync(rfq, before.ContactEmail, after.ContactEmail, "contactEmail", "联系人邮箱", actingUserId);

            var oldSales = await ResolveUserDisplayNameAsync(before.SalesUserId);
            var newSales = await ResolveUserDisplayNameAsync(after.SalesUserId);
            await CompareAndLogRfqHeaderFieldAsync(rfq, oldSales, newSales, "salesUserId", "业务员", actingUserId);

            await CompareAndLogRfqHeaderFieldAsync(
                rfq,
                FormatRfqType(before.RfqType),
                FormatRfqType(after.RfqType),
                "rfqType",
                "需求类型",
                actingUserId);
            await CompareAndLogRfqHeaderFieldAsync(
                rfq,
                FormatQuoteMethod(before.QuoteMethod),
                FormatQuoteMethod(after.QuoteMethod),
                "quoteMethod",
                "报价方式",
                actingUserId);
            await CompareAndLogRfqHeaderFieldAsync(
                rfq,
                FormatAssignMethod(before.AssignMethod),
                FormatAssignMethod(after.AssignMethod),
                "assignMethod",
                "分配方式",
                actingUserId);
            await CompareAndLogRfqHeaderFieldAsync(rfq, before.Industry, after.Industry, "industry", "行业", actingUserId);
            await CompareAndLogRfqHeaderFieldAsync(rfq, before.Product, after.Product, "product", "产品", actingUserId);
            await CompareAndLogRfqHeaderFieldAsync(
                rfq,
                FormatTargetType(before.TargetType),
                FormatTargetType(after.TargetType),
                "targetType",
                "目标类型",
                actingUserId);
            await CompareAndLogRfqHeaderFieldAsync(
                rfq,
                before.Importance.ToString(),
                after.Importance.ToString(),
                "importance",
                "重要程度",
                actingUserId);
            await CompareAndLogRfqHeaderFieldAsync(
                rfq,
                FormatBool(before.IsLastInquiry),
                FormatBool(after.IsLastInquiry),
                "isLastInquiry",
                "最后一次询价",
                actingUserId);
            await CompareAndLogRfqHeaderFieldAsync(rfq, before.ProjectBackground, after.ProjectBackground, "projectBackground", "项目背景", actingUserId);
            await CompareAndLogRfqHeaderFieldAsync(rfq, before.Competitor, after.Competitor, "competitor", "竞争对手", actingUserId);
            await CompareAndLogRfqHeaderFieldAsync(rfq, before.Remark, after.Remark, "remark", "备注", actingUserId);
        }

        private async Task CompareAndLogRfqHeaderFieldAsync(
            RFQ rfq,
            string? oldVal,
            string? newVal,
            string fieldName,
            string fieldLabel,
            string? actingUserId)
        {
            await AppendRfqFieldChangeLogAsync(rfq, fieldName, fieldLabel, oldVal, newVal, actingUserId);
        }

        private sealed record RfqItemFieldSnapshot(
            int LineNo,
            string? CustomerMpn,
            string Mpn,
            string CustomerBrand,
            string Brand,
            decimal? TargetPrice,
            short PriceCurrency,
            decimal Quantity,
            string? ProductionDate,
            DateTime? ExpiryDate,
            decimal? MinPackageQty,
            decimal? Moq,
            string? Alternatives,
            string? Remark,
            short Status,
            string? AssignedPurchaserUserId1,
            string? AssignedPurchaserUserId2);

        private static RfqItemFieldSnapshot CaptureRfqItemFieldSnapshot(RFQItem item) =>
            new(
                item.LineNo,
                item.CustomerMpn,
                item.Mpn,
                item.CustomerBrand,
                item.Brand,
                item.TargetPrice,
                item.PriceCurrency,
                item.Quantity,
                item.ProductionDate,
                item.ExpiryDate,
                item.MinPackageQty,
                item.Moq,
                item.Alternatives,
                item.Remark,
                item.Status,
                item.AssignedPurchaserUserId1,
                item.AssignedPurchaserUserId2);

        private static string BuildRfqItemLineCode(RFQ rfq, RFQItem item) =>
            $"{rfq.RfqCode}-L{item.LineNo}";

        private async Task LogRfqItemFieldChangesAsync(
            RFQ rfq,
            RFQItem item,
            RfqItemFieldSnapshot before,
            string? actingUserId)
        {
            var after = CaptureRfqItemFieldSnapshot(item);
            var lineCode = BuildRfqItemLineCode(rfq, item);

            await CompareAndLogRfqItemFieldAsync(rfq, item, lineCode, before.LineNo.ToString(), after.LineNo.ToString(), "lineNo", "行号", actingUserId);
            await CompareAndLogRfqItemFieldAsync(rfq, item, lineCode, before.CustomerMpn, after.CustomerMpn, "customerMpn", "客户料号", actingUserId);
            await CompareAndLogRfqItemFieldAsync(rfq, item, lineCode, before.Mpn, after.Mpn, "mpn", "物料型号", actingUserId);
            await CompareAndLogRfqItemFieldAsync(rfq, item, lineCode, before.CustomerBrand, after.CustomerBrand, "customerBrand", "客户品牌", actingUserId);
            await CompareAndLogRfqItemFieldAsync(rfq, item, lineCode, before.Brand, after.Brand, "brand", "供应品牌", actingUserId);
            await CompareAndLogRfqItemFieldAsync(
                rfq, item, lineCode,
                before.TargetPrice.HasValue ? FormatDecimal6(before.TargetPrice.Value) : null,
                after.TargetPrice.HasValue ? FormatDecimal6(after.TargetPrice.Value) : null,
                "targetPrice", "目标价", actingUserId);
            await CompareAndLogRfqItemFieldAsync(
                rfq, item, lineCode,
                FormatCurrency(before.PriceCurrency),
                FormatCurrency(after.PriceCurrency),
                "priceCurrency", "目标价币别", actingUserId);
            await CompareAndLogRfqItemFieldAsync(
                rfq, item, lineCode,
                FormatDecimal4(before.Quantity),
                FormatDecimal4(after.Quantity),
                "quantity", "数量", actingUserId);
            await CompareAndLogRfqItemFieldAsync(rfq, item, lineCode, before.ProductionDate, after.ProductionDate, "productionDate", "DC", actingUserId);
            await CompareAndLogRfqItemFieldAsync(
                rfq, item, lineCode,
                FormatRfqDate(before.ExpiryDate),
                FormatRfqDate(after.ExpiryDate),
                "expiryDate", "失效日期", actingUserId);
            await CompareAndLogRfqItemFieldAsync(
                rfq, item, lineCode,
                before.MinPackageQty?.ToString("0.####"),
                after.MinPackageQty?.ToString("0.####"),
                "minPackageQty", "最小包装量", actingUserId);
            await CompareAndLogRfqItemFieldAsync(
                rfq, item, lineCode,
                before.Moq?.ToString("0.####"),
                after.Moq?.ToString("0.####"),
                "moq", "起订量", actingUserId);
            await CompareAndLogRfqItemFieldAsync(rfq, item, lineCode, before.Alternatives, after.Alternatives, "alternatives", "可替代料", actingUserId);
            await CompareAndLogRfqItemFieldAsync(rfq, item, lineCode, before.Remark, after.Remark, "remark", "备注", actingUserId);
            await CompareAndLogRfqItemFieldAsync(
                rfq, item, lineCode,
                RfqFieldChangeLogWriter.FormatRfqItemStatus(before.Status),
                RfqFieldChangeLogWriter.FormatRfqItemStatus(after.Status),
                "status", "明细状态", actingUserId);

            var oldP1 = await ResolveUserDisplayNameAsync(before.AssignedPurchaserUserId1);
            var newP1 = await ResolveUserDisplayNameAsync(after.AssignedPurchaserUserId1);
            await CompareAndLogRfqItemFieldAsync(rfq, item, lineCode, oldP1, newP1, "assignedPurchaserUserId1", "询价采购员1", actingUserId);

            var oldP2 = await ResolveUserDisplayNameAsync(before.AssignedPurchaserUserId2);
            var newP2 = await ResolveUserDisplayNameAsync(after.AssignedPurchaserUserId2);
            await CompareAndLogRfqItemFieldAsync(rfq, item, lineCode, oldP2, newP2, "assignedPurchaserUserId2", "询价采购员2", actingUserId);
        }

        private async Task CompareAndLogRfqItemFieldAsync(
            RFQ rfq,
            RFQItem item,
            string lineCode,
            string? oldVal,
            string? newVal,
            string fieldName,
            string fieldLabel,
            string? actingUserId)
        {
            await AppendRfqItemFieldChangeLogAsync(rfq, item, lineCode, fieldName, fieldLabel, oldVal, newVal, actingUserId);
        }

        private async Task LogRfqItemAddedAsync(RFQ rfq, RFQItem item, string? actingUserId)
        {
            var lineCode = BuildRfqItemLineCode(rfq, item);
            var summary = $"{lineCode} · {item.Mpn} · {item.Brand} · 数量 {FormatDecimal4(item.Quantity)}";
            await AppendRfqItemFieldChangeLogAsync(
                rfq,
                item,
                lineCode,
                "lineAdded",
                "新增明细",
                null,
                summary,
                actingUserId);
        }

        private static string FormatRfqDate(DateTime? value)
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
            _ => currency.ToString()
        };

        private static string FormatRfqType(short value) => value switch
        {
            1 => "现货",
            2 => "排单",
            3 => "代理",
            4 => "自营",
            5 => "信息服务",
            _ => value.ToString()
        };

        private static string FormatQuoteMethod(short value) => value switch
        {
            1 => "不接受任何消息",
            2 => "系统推送",
            3 => "邮件",
            4 => "短信",
            _ => value.ToString()
        };

        private static string FormatAssignMethod(short value) => value switch
        {
            1 => "系统分配同一采购",
            2 => "条目轮询",
            3 => "品牌轮询",
            4 => "指定采购",
            5 => "采报优先",
            _ => value.ToString()
        };

        private static string FormatTargetType(short value) => value switch
        {
            1 => "比价需求",
            2 => "独家需求",
            3 => "紧急需求",
            4 => "常规需求",
            _ => value.ToString()
        };
    }
}
