import type { CompanyBankRow } from '@/api/companyProfile'

/** 兼容旧接口：未返回 availableForPayment 时按「用途=付款」推断。 */
export function normalizeCompanyBankRow(row: CompanyBankRow): CompanyBankRow {
  const r = { ...row }
  if (r.availableForPayment == null) {
    r.availableForPayment =
      r.enabled !== false && (r.purposeType || 'payment').trim().toLowerCase() === 'payment'
  }
  return r
}

/** 是否可用于付款窗口「付款银行」下拉。 */
export function isCompanyBankAvailableForPayment(row: CompanyBankRow): boolean {
  const r = normalizeCompanyBankRow(row)
  return r.enabled !== false && r.availableForPayment === true
}

/** 已启用且勾选「可用付款」的公司银行账户（付款窗口付款银行下拉）。 */
export function filterEnabledCompanyPaymentBanks(rows: CompanyBankRow[]): CompanyBankRow[] {
  return rows.filter((r) => isCompanyBankAvailableForPayment(r))
}

/** 付款单下拉：{银行名称} · {账户名} · {账号} */
export function formatCompanyBankOptionLabel(row: CompanyBankRow, masked = false): string {
  if (masked) return '—'
  const bankName = row.bankName?.trim() || '—'
  const accountName = row.accountName?.trim() || '—'
  const accountNo = row.accountNumber?.trim() || '—'
  return `${bankName} · ${accountName} · ${accountNo}`
}
