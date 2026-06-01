import type { FinancePaymentBankDto } from '@/api/financePaymentBank'
import type { VendorBankInfo } from '@/types/vendor'

/** 供应商默认请款银行：仅使用 vendorbankinfo.FinancePaymentBankId。 */
export function resolveVendorDefaultFinancePaymentBankId(banks: VendorBankInfo[]): string {
  if (!banks.length) return ''
  const preferred = banks.find((b) => b.isDefault) ?? banks[0]
  return preferred.financePaymentBankId?.trim() ?? ''
}

/** 列表/详情展示开户银行名称（由枚举 Id 解析，不读手填 bankName）。 */
export function vendorBankLabel(
  bank: Pick<VendorBankInfo, 'financePaymentBankId' | 'bankName'> | null | undefined,
  paymentBankOptions: FinancePaymentBankDto[]
): string {
  if (!bank) return '--'
  const id = bank.financePaymentBankId?.trim()
  if (id) {
    const hit = paymentBankOptions.find((b) => b.id === id)
    if (hit?.bankName) return hit.bankName
  }
  return bank.bankName?.trim() || '--'
}
