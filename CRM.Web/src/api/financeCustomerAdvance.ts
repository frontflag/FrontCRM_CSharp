import apiClient from './client'
import { toExportQueryString } from '@/utils/exportFileName'

export interface FinanceCustomerAdvance {
  id: string
  customerId: string
  customerName?: string
  customerEnglishName?: string
  currency: number
  balance: number
  totalIn: number
  totalApplied: number
  totalRefund: number
  salesUserId?: string
  createTime?: string
}

export interface FinanceCustomerAdvanceLedger {
  id: string
  financeCustomerAdvanceId: string
  customerId: string
  currency: number
  ledgerType: number
  amount: number
  balanceAfter: number
  financeReceiptId?: string
  financeReceiptItemId?: string
  financeReceivableId?: string
  advanceSellOrderId?: string
  remark?: string
  createTime?: string
}

export interface FinanceCustomerAdvanceBalance {
  customerId: string
  currency: number
  balance: number
  advanceSellOrderId?: string
}

const BASE = '/api/v1/finance/customer-advances'

export const financeCustomerAdvanceApi = {
  getPaged: (params: Record<string, unknown>) =>
    apiClient.get<{ items: FinanceCustomerAdvance[]; total: number; page: number; pageSize: number }>(
      BASE,
      { params }
    ),
  exportList: (params?: Record<string, unknown>) => {
    const q = toExportQueryString(params)
    return apiClient.getBlob(q ? `${BASE}/export?${q}` : `${BASE}/export`)
  },
  getLedger: (params: Record<string, unknown>) =>
    apiClient.get<{ items: FinanceCustomerAdvanceLedger[]; total: number; page: number; pageSize: number }>(
      `${BASE}/ledger`,
      { params }
    ),
  getBalance: (customerId: string, currency?: number) =>
    apiClient.get<{ balance: FinanceCustomerAdvanceBalance | null; balances: FinanceCustomerAdvanceBalance[] }>(
      `${BASE}/balance`,
      { params: { customerId, currency } }
    )
}
