import apiClient from './client'
import type { FinanceCustomerAdvanceBalance } from './financeCustomerAdvance'

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
  salesUserId?: string
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
  }
  financeReceiptCode: string
  receiptStatus: number
  remainingAmount: number
  receiptPurpose?: number
  advanceSellOrderId?: string
}

export interface FinanceReceivableWriteOffCandidates {
  receiptItems: FinanceReceiptItemWriteOffCandidate[]
  receivables: FinanceReceivable[]
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
  getWriteOffCandidates: (customerId: string) =>
    apiClient.get<FinanceReceivableWriteOffCandidates>(`${WRITE_OFF_BASE}/candidates`, {
      params: { customerId }
    }),
  applyWriteOff: (payload: FinanceReceivableWriteOffRequest) =>
    apiClient.post<FinanceReceivableWriteOffResult>(WRITE_OFF_BASE, payload)
}
