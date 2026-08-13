import apiClient from './client'
import type { FinanceCustomerAdvanceBalance } from './financeCustomerAdvance'
import { toExportQueryString } from '@/utils/exportFileName'

export interface FinanceReceivable {
  id: string
  receivableCode?: string
  stockOutId: string
  stockOutCode: string
  sellOrderId: string
  sellOrderCode?: string
  sellOrderItemId: string
  customerId: string
  customerName?: string
  customerEnglishName?: string
  customerCode?: string
  salesUserId?: string
  salesUserName?: string
  pn?: string
  brand?: string
  outboundQty: number
  unitPrice: number
  currency: number
  amount: number
  verifiedDone: number
  verifiedToBe: number
  verificationStatus: number
  stockOutDate?: string
  createTime?: string
}

export interface FinanceReceivableWriteOffDetailItem {
  id: string
  amount: number
  writeOffSource: number
  createTime?: string
  financeReceiptId?: string
  financeReceiptItemId?: string
  financeReceiptCode?: string
  operatorUserId?: string
  operatorUserName?: string
  remark?: string
}

export interface FinanceReceiptItemWriteOffCandidate {
  item: {
    id: string
    financeReceiptId: string
    receiptAmount: number
    receiptConvertAmount?: number
    verifiedAmount: number
    advancePoolAmount?: number
    verificationStatus: number
    receiptPurpose?: number
    advanceSellOrderId?: string
    sellOrderId?: string
    pn?: string
    brand?: string
    remark?: string
  }
  financeReceiptCode: string
  receiptStatus: number
  remainingAmount: number
  receiptPurpose?: number
  advanceSellOrderId?: string
  receiptDate?: string
  receiptAmount: number
  receiptCurrency: number
  receiptMode: number
  remark?: string
}

export interface FinanceReceivableWriteOffCandidateRow {
  id: string
  receivableCode?: string
  stockOutId: string
  stockOutCode: string
  sellOrderId: string
  sellOrderCode?: string
  sellOrderItemId: string
  customerId: string
  customerName?: string
  customerEnglishName?: string
  salesUserId?: string
  salesUserName?: string
  pn?: string
  brand?: string
  outboundQty: number
  unitPrice: number
  currency: number
  amount: number
  verifiedDone: number
  verifiedToBe: number
  verificationStatus: number
  stockOutDate?: string
  freightForwarderOrderNo?: string
  stockInCode?: string
}

export interface FinanceWriteOffCustomerCurrencyTotal {
  currency: number
  amount: number
}

export interface FinanceWriteOffCustomerSummary {
  customerId: string
  customerName?: string
  customerEnglishName?: string
  customerCode?: string
  salesUserId?: string
  salesUserName?: string
  pendingWriteOffTotal: number
  currency?: number | null
  isMultiCurrency: boolean
  currencyTotals: FinanceWriteOffCustomerCurrencyTotal[]
  pendingReceiptItemCount: number
  earliestReceiptDate?: string | null
  latestReceiptDate?: string | null
  /** 该客户该币别是否存在未清应收（与右栏口径一致） */
  hasOpenReceivable?: boolean
}

export interface FinanceReceivableWriteOffCandidates {
  receiptItems: FinanceReceiptItemWriteOffCandidate[]
  receivables: FinanceReceivableWriteOffCandidateRow[]
  advanceBalances: FinanceCustomerAdvanceBalance[]
}

export interface FinanceReceivableWriteOffAllocation {
  financeReceiptItemId: string
  financeReceivableId: string
  amount: number
}

export interface FinanceAdvancePoolAllocation {
  financeReceivableId: string
  amount: number
  advanceSellOrderId?: string
}

export interface FinanceReceivableWriteOffSoMismatch {
  financeReceivableId: string
  advanceSellOrderId?: string
  receivableSellOrderId?: string
  message?: string
}

export interface FinanceReceivableWriteOffResult {
  applied: boolean
  requiresSoMismatchConfirm: boolean
  soMismatches: FinanceReceivableWriteOffSoMismatch[]
}

export interface CreditReceiptItemRemainderToPoolResult {
  creditedAmount: number
  remainingAfter: number
}

export interface FinanceReceivableWriteOffLedgerItem {
  id: string
  amount: number
  writeOffSource: number
  createTime?: string
  financeReceiptId?: string
  financeReceiptItemId?: string
  financeReceiptCode?: string
  financeReceivableId: string
  receivableCode?: string
  stockOutId?: string
  stockOutCode?: string
  sellOrderId?: string
  sellOrderCode?: string
  customerId: string
  customerName?: string
  customerEnglishName?: string
  pn?: string
  brand?: string
  currency: number
  operatorUserId?: string
  operatorUserName?: string
  remark?: string
}

export interface FinanceReceivableWriteOffLedgerPage {
  items: FinanceReceivableWriteOffLedgerItem[]
  total: number
  page: number
  pageSize: number
}

export interface FinanceReceivableWriteOffRequest {
  allocations: FinanceReceivableWriteOffAllocation[]
  advancePoolAllocations?: FinanceAdvancePoolAllocation[]
  confirmSoMismatch?: boolean
}

const RECEIVABLE_BASE = '/api/v1/finance/receivables'
const WRITE_OFF_BASE = '/api/v1/finance/receivable-write-offs'

export const financeReceivableApi = {
  getPaged: (params: Record<string, unknown>) =>
    apiClient.get<{ items: FinanceReceivable[]; total: number; page: number; pageSize: number }>(
      RECEIVABLE_BASE,
      { params }
    ),
  exportList: (params?: Record<string, unknown>) => {
    const q = toExportQueryString(params)
    return apiClient.getBlob(q ? `${RECEIVABLE_BASE}/export?${q}` : `${RECEIVABLE_BASE}/export`)
  },
  getById: (id: string) => apiClient.get<FinanceReceivable>(`${RECEIVABLE_BASE}/${encodeURIComponent(id)}`),
  getWriteOffs: (id: string) =>
    apiClient.get<FinanceReceivableWriteOffDetailItem[]>(`${RECEIVABLE_BASE}/${encodeURIComponent(id)}/write-offs`),
  getWriteOffCustomerSummaries: (keyword?: string) =>
    apiClient.get<FinanceWriteOffCustomerSummary[]>(`${WRITE_OFF_BASE}/customer-summaries`, {
      params: keyword?.trim() ? { keyword: keyword.trim() } : undefined
    }),
  getWriteOffLedger: (params: { keyword?: string; page?: number; pageSize?: number }) =>
    apiClient.get<FinanceReceivableWriteOffLedgerPage>(`${WRITE_OFF_BASE}/ledger`, { params }),
  getWriteOffCandidates: (customerId: string) =>
    apiClient.get<FinanceReceivableWriteOffCandidates>(`${WRITE_OFF_BASE}/candidates`, {
      params: { customerId }
    }),
  applyWriteOff: (payload: FinanceReceivableWriteOffRequest) =>
    apiClient.post<FinanceReceivableWriteOffResult>(WRITE_OFF_BASE, payload),
  creditReceiptItemRemainderToAdvancePool: (receiptItemId: string, amount?: number) =>
    apiClient.post<CreditReceiptItemRemainderToPoolResult>(
      `${WRITE_OFF_BASE}/receipt-items/${encodeURIComponent(receiptItemId)}/credit-to-advance-pool`,
      amount != null && amount > 0 ? { amount } : {}
    )
}
