using CRM.Core.Models.Quote;

namespace CRM.Core.Utilities
{
    /// <summary>
    /// 销售订单明细通过 <c>QuoteId</c> 只与报价<strong>主表</strong>关联；取用于采购申请回填（供应商、报价成本等）的报价<strong>明细</strong>时，
    /// 不按 PN/品牌/币别/单价/DateCode 与销单行再匹配。
    /// 同一 <c>QuoteId</c> 下多行 <see cref="QuoteItem"/> 时，按 <see cref="Models.BaseEntity.CreateTime"/>、再按 <c>Id</c> 稳定取一条。
    /// </summary>
    public static class QuoteItemForPrResolver
    {
        public static QuoteItem? PickSingleLine(IReadOnlyList<QuoteItem> quoteItems)
        {
            if (quoteItems == null || quoteItems.Count == 0)
                return null;
            return quoteItems
                .OrderBy(qi => qi.CreateTime)
                .ThenBy(qi => qi.Id, StringComparer.Ordinal)
                .First();
        }
    }
}
