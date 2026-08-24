using CRM.Core.Models.Finance;

namespace CRM.Core.Utilities;

/// <summary>收款明细可核销余额（与收款详情页、核销候选口径一致）。</summary>
public static class FinanceReceiptItemWriteOffHelper
{
    /// <summary>
    /// 有效折算金额：优先 <see cref="FinanceReceiptItem.ReceiptConvertAmount"/>；
    /// 为 0 时回退 <see cref="FinanceReceiptItem.ReceiptAmount"/>（历史/导入数据）。
    /// 默认明细的折算金额与明细金额由新建/编辑单头金额时写成同一值。
    /// </summary>
    public static decimal EffectiveConvertAmount(FinanceReceiptItem item)
    {
        if (item.ReceiptConvertAmount > 0m)
            return item.ReceiptConvertAmount;
        return item.ReceiptAmount > 0m ? item.ReceiptAmount : 0m;
    }

    /// <summary>剩余可核销 = 有效折算金额 − 已核销 − 已转预收池。</summary>
    public static decimal GetRemaining(FinanceReceiptItem item) =>
        EffectiveConvertAmount(item) - item.VerifiedAmount - item.AdvancePoolAmount;
}
