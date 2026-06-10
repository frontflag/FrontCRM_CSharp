import type { FinancePaymentBankDto } from '@/api/financePaymentBank'
import type { VendorBankInfo } from '@/types/vendor'

/** 仅返回已启用的供应商银行账户。 */
export function filterEnabledVendorBanks(banks: VendorBankInfo[]): VendorBankInfo[] {
  return banks.filter((b) => b.isEnabled !== false)
}

/** 供应商默认请款银行账户 ID（vendorbankinfo.id）。 */
export function resolveVendorDefaultBankId(banks: VendorBankInfo[]): string {
  const enabled = filterEnabledVendorBanks(banks)
  if (!enabled.length) return ''
  const preferred = enabled.find((b) => b.isDefault) ?? enabled[0]
  return preferred.id?.trim() ?? ''
}

/** @deprecated 请使用 resolveVendorDefaultBankId；仅兼容旧逻辑。 */
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

/** 请款下拉：{开户银行名} · {账户名称} · {完整账号} */
export function formatVendorBankOptionLabel(
  bank: VendorBankInfo,
  paymentBankOptions: FinancePaymentBankDto[],
  masked = false
): string {
  if (masked) return '—'
  const bankName = vendorBankLabel(bank, paymentBankOptions)
  const accountName = bank.accountName?.trim() || '—'
  const accountNo = bank.bankAccount?.trim() || '—'
  return `${bankName} · ${accountName} · ${accountNo}`
}
