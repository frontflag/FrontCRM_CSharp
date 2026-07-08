import apiClient from './client'
import type { FinanceReceipt } from './finance'

export const FF_PAYABLE_STATUS = {
  Pending: 10,
  Partial: 20,
  Completed: 30
} as const

export interface FfPayableListItem {
  receiptId: string
  financeReceiptCode: string
  receiptStatus: number
  customerId: string
  customerName?: string | null
  freightForwarderCompanyId?: string | null
  freightForwarderCompanyName?: string | null
  receiptAmount: number
  paidAmount: number
  pendingAmount: number
  receiptCurrency: number
  payableStatus: number
  receiptDate?: string | null
  createTime: string
}

export interface FfPaymentLine {
  id: string
  financeReceiptId: string
  freightForwarderCompanyId: string
  freightForwarderCompanyName?: string | null
  paymentAmount: number
  paymentCurrency: number
  paymentMode: number
  companyBankId?: string | null
  ffCompanyBankId?: string | null
  ffCompanyBankName?: string | null
  bankSlipNo?: string | null
  paymentDate?: string | null
  paymentUserId?: string | null
  paymentUserName?: string | null
  remark?: string | null
  createTime?: string
}

export interface FfPayableDetail {
  receipt: FinanceReceipt
  freightForwarderCompanyName?: string | null
  paidAmount: number
  pendingAmount: number
  payableStatus: number
  payments: FfPaymentLine[]
}

export interface FfPayableListQuery {
  keyword?: string
  customerId?: string
  freightForwarderCompanyId?: string
  payableStatus?: number
  page?: number
  pageSize?: number
}

export interface CreateFfPaymentBody {
  freightForwarderCompanyId?: string
  paymentAmount: number
  paymentCurrency?: number
  paymentMode?: number
  companyBankId?: string
  ffCompanyBankId?: string
  bankSlipNo?: string
  paymentDate?: string
  remark?: string
}

export interface FfPayableListResult {
  items: FfPayableListItem[]
  total: number
  page: number
  pageSize: number
}

export const financeFreightForwarderPayableApi = {
  getList: (params: FfPayableListQuery = {}) =>
    apiClient.get<FfPayableListResult>('/api/v1/finance/freight-forwarder-payables', { params }),
  getDetail: (receiptId: string) =>
    apiClient.get<FfPayableDetail>(`/api/v1/finance/freight-forwarder-payables/${receiptId}`),
  createPayment: (receiptId: string, body: CreateFfPaymentBody) =>
    apiClient.post<FfPaymentLine>(`/api/v1/finance/freight-forwarder-payables/${receiptId}/payments`, body),
  updateFfCompany: (receiptId: string, freightForwarderCompanyId: string) =>
    apiClient.put<FinanceReceipt>(`/api/v1/finance/freight-forwarder-payables/${receiptId}/freight-forwarder-company`, {
      freightForwarderCompanyId
    })
}
