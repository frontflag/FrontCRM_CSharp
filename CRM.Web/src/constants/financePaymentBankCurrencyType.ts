import type { FinancePaymentBankDto } from '@/api/financePaymentBank'

/** financepaymentbank.CurrencyType，与后端 FinancePaymentBankCurrencyType 一致 */
export const FINANCE_PAYMENT_BANK_CURRENCY_CNY = 10
export const FINANCE_PAYMENT_BANK_CURRENCY_FOREIGN = 20

export const FINANCE_PAYMENT_BANK_CURRENCY_OPTIONS = [
  { value: FINANCE_PAYMENT_BANK_CURRENCY_CNY, labelKey: 'financeParams.currencyTypeCny' },
  { value: FINANCE_PAYMENT_BANK_CURRENCY_FOREIGN, labelKey: 'financeParams.currencyTypeForeign' }
] as const

export function financePaymentBankCurrencyTypeLabel(
  value: number,
  t: (key: string) => string
): string {
  const hit = FINANCE_PAYMENT_BANK_CURRENCY_OPTIONS.find((o) => o.value === value)
  return hit ? t(hit.labelKey) : String(value)
}

/** 选中后输入框展示：人民币银行→中文名；外币银行→英文名（无英文名时回退中文名） */
export function financePaymentBankSelectedDisplayLabel(bank: FinancePaymentBankDto): string {
  if (bank.currencyType === FINANCE_PAYMENT_BANK_CURRENCY_FOREIGN) {
    const en = bank.eBankName?.trim()
    return en || bank.bankName
  }
  return bank.bankName
}
