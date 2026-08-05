import apiClient from './client'

export type BizType =
  | 'VENDOR'
  | 'SALES_ORDER'
  | 'PURCHASE_ORDER'
  | 'CUSTOMER'
  | 'FINANCE_RECEIPT'
  | 'FINANCE_PAYMENT'

export interface PendingApprovalItem {
  bizType: BizType
  bizTypeName: string
  businessId: string
  documentCode: string
  title?: string | null
  counterpartyName?: string | null
  amount?: number | null
  currency?: number | null
  submitter?: string | null
  /** 最近一次通过/驳回审批人 */
  approver?: string | null
  /** 最近一次通过/驳回时间 */
  approvedAt?: string | null
  status: number
  createdAt: string
  /** 后端：是否可执行通过/驳回；缺省 true 兼容旧接口 */
  canDecide?: boolean
}

export interface PendingApprovalsQueryParams {
  bizType?: string
  state?: 'pending' | 'approved' | 'rejected'
  submittedFrom?: string
  submittedTo?: string
  documentCode?: string
  submitter?: string
  approver?: string
  /** submittedAt | approvedAt */
  sortBy?: 'submittedAt' | 'approvedAt'
  /** asc | desc（优先，避免布尔 query 绑定问题） */
  sortDir?: 'asc' | 'desc'
  sortAsc?: boolean
  page: number
  pageSize: number
}

export interface PageResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface ApprovalSummary {
  pendingCount: number
  approvedCount: number
  rejectedCount: number
}

export interface ApprovalHistoryItem {
  id: string
  bizType: string
  businessId: string
  documentCode?: string
  itemDescription?: string | null
  actionType: 'submit' | 'approve' | 'reject' | string
  fromStatus?: number | null
  toStatus?: number | null
  submitRemark?: string | null
  auditRemark?: string | null
  submitterUserId?: string | null
  submitterUserName?: string | null
  approverUserId?: string | null
  approverUserName?: string | null
  actionTime: string
}

export const approvalsApi = {
  getApprovalItems: (params: PendingApprovalsQueryParams) =>
    apiClient.get<PageResult<PendingApprovalItem>>('/api/v1/approvals/items', { params }),

  getApprovalSummary: (params: { bizType?: string }) =>
    apiClient.get<ApprovalSummary>('/api/v1/approvals/summary', { params }),

  getApprovalHistory: (params: { bizType: string; businessId: string }) =>
    apiClient.get<ApprovalHistoryItem[]>('/api/v1/approvals/history', { params }),

  getPendingApprovals: (params: Omit<PendingApprovalsQueryParams, 'state'>) =>
    apiClient.get<PageResult<PendingApprovalItem>>('/api/v1/approvals/pending', { params }),

  decidePendingApproval: (payload: {
    bizType: BizType
    businessId: string
    decision: 'approve' | 'reject'
    remark?: string
  }) => apiClient.post('/api/v1/approvals/pending/decide', payload)
}

