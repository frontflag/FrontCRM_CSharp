import apiClient from './client'
import { formatDisplayDate2DigitYear } from '@/utils/displayDateTime'

export const COMMISSION_ROLE_SALES = 1
export const COMMISSION_ROLE_PURCHASE = 2

export interface CommissionOpenWindow {
  from: string
  to: string
  term: string
  label: string
}

export interface CommissionTermOption {
  term: string
  label: string
}

export interface CommissionSummaryRow {
  userId: string
  userName: string
  userLevel: number
  lineCount: number
  monthCount: number
  periodGpUsd: number
  commissionUsd: number
}

export interface CommissionMonthRow {
  userId: string
  userName: string
  userLevel: number
  calcMonth: string
  qualified: boolean
  monthGpUsd: number
  ratePoints: number
  commissionUsd: number
  lineCount: number
  qualifyGpUsd?: number | null
  term?: string | null
}

export interface CommissionPersonMonths {
  userId: string
  userName: string
  userLevel: number
  items: CommissionMonthRow[]
  totalCount: number
  page: number
  pageSize: number
  totalLineCount: number
  totalCommissionUsd: number
}

export interface CommissionLineRow {
  id: string
  roleType: number
  stockOutItemId: string
  userId: string
  userName: string
  userLevel: number
  versionId?: string | null
  ratePoints: number
  gpUsd: number
  periodGpUsd: number
  commissionUsd: number
  sellOrderId?: string | null
  sellOrderCode?: string | null
  sellOrderItemId?: string | null
  sellOrderItemCode?: string | null
  purchaseOrderId?: string | null
  purchaseOrderCode?: string | null
  stockOutId: string
  stockOutCode: string
  stockOutDate?: string | null
  receiptDate: string
  poolDate: string
  calcMonth: string
  entryKind: number
  term?: string | null
}

export interface CommissionPersonLineRow {
  stockOutItemId: string
  stockOutId: string
  stockOutCode: string
  stockOutDate?: string | null
  userId: string
  userName: string
  userLevel: number
  receiptWriteOffDone: boolean
  receiptProgressStatus: number
  receiptDate?: string | null
  daysSinceReceipt?: number | null
  inCommission: boolean
  gpUsd: number
  ratePoints: number
  commissionUsd: number
  calcMonth: string
  entryKind: number
  sellOrderId?: string | null
  sellOrderCode?: string | null
  purchaseOrderId?: string | null
  purchaseOrderCode?: string | null
  term?: string | null
}

export interface CommissionPersonDetail {
  userId: string
  userName: string
  userLevel: number
  items: CommissionPersonLineRow[]
  totalCount: number
  page: number
  pageSize: number
}

export interface CommissionPaged<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export interface CommissionCalcRunResult {
  calcDate: string
  salesCount: number
  purchaseCount: number
}

export interface CommissionLockRunResult {
  lockDate: string
  term: string
  windowFrom: string
  windowTo: string
  inserted: number
  skippedExisting: number
}

const longTimeout = { timeout: 180000 }

export const commissionApi = {
  estimatedWindow(roleType: number) {
    return apiClient.get<CommissionOpenWindow>('/api/v1/commission/estimated/window', {
      params: { roleType }
    })
  },
  estimatedSummary(params: Record<string, unknown>) {
    return apiClient.get<CommissionPaged<CommissionSummaryRow>>('/api/v1/commission/estimated/summary', {
      params
    })
  },
  estimatedLines(params: Record<string, unknown>) {
    return apiClient.get<CommissionPaged<CommissionLineRow>>('/api/v1/commission/estimated', { params })
  },
  estimatedMonths(params: Record<string, unknown>) {
    return apiClient.get<CommissionPersonMonths>('/api/v1/commission/estimated/months', { params })
  },
  estimatedPersonLines(params: Record<string, unknown>) {
    return apiClient.get<CommissionPersonDetail>('/api/v1/commission/estimated/person-lines', { params })
  },
  recalcEstimated() {
    return apiClient.post<CommissionCalcRunResult>('/api/v1/commission/estimated/recalc', {}, longTimeout)
  },
  officialTerms(roleType: number) {
    return apiClient.get<CommissionTermOption[]>('/api/v1/commission/official/terms', {
      params: { roleType }
    })
  },
  officialSummary(params: Record<string, unknown>) {
    return apiClient.get<CommissionPaged<CommissionSummaryRow>>('/api/v1/commission/official/summary', {
      params
    })
  },
  officialMonths(params: Record<string, unknown>) {
    return apiClient.get<CommissionPersonMonths>('/api/v1/commission/official/months', { params })
  },
  officialPersonLines(params: Record<string, unknown>) {
    return apiClient.get<CommissionPersonDetail>('/api/v1/commission/official/person-lines', { params })
  },
  officialLines(params: Record<string, unknown>) {
    return apiClient.get<CommissionPaged<CommissionLineRow>>('/api/v1/commission/official', { params })
  },
  lockOfficial(lockDate?: string) {
    return apiClient.post<CommissionLockRunResult>(
      '/api/v1/commission/official/lock',
      { lockDate: lockDate || null },
      longTimeout
    )
  }
}

export function formatCommissionMoney(value: number | null | undefined): string {
  if (value == null || Number.isNaN(Number(value))) return '—'
  return Number(value).toLocaleString('zh-CN', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  })
}

export function formatCommissionPoints(value: number | null | undefined): string {
  if (value == null || Number.isNaN(Number(value))) return '—'
  return `${Number(value).toLocaleString('zh-CN', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1
  })}%`
}

export function formatCommissionDate(value?: string | null): string {
  if (!value) return '—'
  return String(value).slice(0, 10)
}

/** 列表日期：两位年 YY-MM-DD，空值显示 — */
export function formatCommissionListDate(value?: string | null): string {
  if (!value) return '—'
  const formatted = formatDisplayDate2DigitYear(value)
  return !formatted || formatted === '--' ? '—' : formatted
}
