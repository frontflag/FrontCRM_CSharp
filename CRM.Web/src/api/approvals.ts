import apiClient from './client'

export type BizType = 'VENDOR' | 'QUOTE' | 'SALES_ORDER' | 'FINANCE_RECEIPT' | 'FINANCE_PAYMENT'

export interface PendingApprovalItem {
  bizType: BizType
  bizTypeName: string
  businessId: string
  documentCode: string
  title?: string | null
  counterpartyName?: string | null
  amount?: number | null
  currency?: number | null
  status: number
  createdAt: string
}

export interface PageResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export const approvalsApi = {
  getPendingApprovals: (params: { bizType?: string; page: number; pageSize: number }) =>
    apiClient.get<PageResult<PendingApprovalItem>>('/api/v1/approvals/pending', { params }),

  decidePendingApproval: (payload: {
    bizType: BizType
    businessId: string
    decision: 'approve' | 'reject'
    remark?: string
  }) => apiClient.post('/api/v1/approvals/pending/decide', payload)
}

