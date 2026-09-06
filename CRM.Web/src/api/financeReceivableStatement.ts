import apiClient from './client'

export interface FinanceReceivableStatementListItem {
  customerId: string
  customerCode?: string
  customerName?: string
  customerEnglishName?: string
  currency: number
  receivableCount: number
  amountTotal: number
  verifiedDone: number
  verifiedToBe: number
  latestStockOutDate?: string
  salesUserId?: string
  salesUserName?: string
}

export interface FinanceReceivableStatementCustomer {
  customerId: string
  customerCode?: string
  customerName?: string
  customerEnglishName?: string
  contactName?: string
  contactPhone?: string
  paymentDays?: number | null
  creditLimit?: number | null
  salesUserId?: string
  salesUserName?: string
  canViewFull: boolean
}

export interface FinanceReceivableStatementHeader {
  periodFrom: string
  periodTo: string
  generatedOn: string
  agingCutoff: string
  currency: number
  opening: number
  periodIncrease: number
  periodReceived: number
  ending: number
}

export interface FinanceReceivableStatementLine {
  lineType: 'opening' | 'increase' | 'receipt' | string
  date: string
  docNo?: string | null
  summary: string
  increaseAmount?: number | null
  receivedAmount?: number | null
  balance: number
  receivableId?: string | null
  stockOutId?: string | null
  receiptId?: string | null
  writeOffId?: string | null
}

export interface FinanceReceivableStatementDetail {
  customer: FinanceReceivableStatementCustomer
  statement: FinanceReceivableStatementHeader
  lines: FinanceReceivableStatementLine[]
}

const BASE = '/api/v1/finance/receivable-statements'

export const financeReceivableStatementApi = {
  getPaged: (params: Record<string, unknown>) =>
    apiClient.get<{
      items: FinanceReceivableStatementListItem[]
      total: number
      page: number
      pageSize: number
    }>(BASE, { params }),
  getDetail: (customerId: string, currency: number, params: { from: string; to: string; aging?: string }) =>
    apiClient.get<FinanceReceivableStatementDetail>(
      `${BASE}/${encodeURIComponent(customerId)}/${currency}`,
      { params }
    )
}
