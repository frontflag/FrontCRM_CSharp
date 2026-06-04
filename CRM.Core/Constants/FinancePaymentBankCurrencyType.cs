namespace CRM.Core.Constants
{
    /// <summary>financepaymentbank.CurrencyType：付款银行币别类型。</summary>
    public static class FinancePaymentBankCurrencyType
    {
        /// <summary>人民币银行</summary>
        public const int Cny = 10;

        /// <summary>外币银行</summary>
        public const int Foreign = 20;

        public static bool IsValid(int value) => value is Cny or Foreign;
    }
}
