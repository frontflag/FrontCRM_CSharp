import type { FinancePaymentBankDto } from '@/api/financePaymentBank'

/** 中文名 / 英文名 / 简称 模糊匹配（不区分大小写） */
export function matchesFinancePaymentBankKeyword(
  bank: FinancePaymentBankDto,
  query: string
): boolean {
  const q = query.trim().toLowerCase()
  if (!q) return true
  return (
    bank.bankName.toLowerCase().includes(q) ||
    (bank.eBankName ?? '').toLowerCase().includes(q) ||
    (bank.shortName ?? '').toLowerCase().includes(q)
  )
}

export function filterFinancePaymentBankOptions(
  banks: FinancePaymentBankDto[],
  query: string
): FinancePaymentBankDto[] {
  const q = query.trim()
  if (!q) return banks
  return banks.filter((b) => matchesFinancePaymentBankKeyword(b, q))
}
