import type { CompanyBankRow } from '@/api/companyProfile'

/** 已启用且用途为付款的公司银行账户。 */
export function filterEnabledCompanyPaymentBanks(rows: CompanyBankRow[]): CompanyBankRow[] {
  return rows.filter((r) => r.enabled !== false && (r.purposeType || 'payment').trim().toLowerCase() === 'payment')
}

/** 付款单下拉：{银行名称} · {账户名} · {账号} */
export function formatCompanyBankOptionLabel(row: CompanyBankRow, masked = false): string {
  if (masked) return '—'
  const bankName = row.bankName?.trim() || '—'
  const accountName = row.accountName?.trim() || '—'
  const accountNo = row.accountNumber?.trim() || '—'
  return `${bankName} · ${accountName} · ${accountNo}`
}
