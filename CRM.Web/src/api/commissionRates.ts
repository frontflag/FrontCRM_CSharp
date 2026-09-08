import apiClient from './client'

export const COMMISSION_LADDER_COUNT = 10
export const COMMISSION_ROLE_SALES = 1
export const COMMISSION_ROLE_PURCHASE = 2

export interface CommissionRateRow {
  id: string
  roleType: number
  userLevel: number
  thresholds: Array<number | null>
  ratePoints: Array<number | null>
  remark?: string | null
}

export interface CommissionLadderWrite {
  thresholdAmount: number | null
  ratePoints: number | null
}

export const commissionRatesApi = {
  list(roleType: number) {
    return apiClient.get<CommissionRateRow[]>('/api/v1/commission-rates', { params: { roleType } })
  },
  update(id: string, payload: { remark?: string | null; ladders: CommissionLadderWrite[] }) {
    return apiClient.put<CommissionRateRow>(`/api/v1/commission-rates/${encodeURIComponent(id)}`, payload)
  }
}

export function formatLadderCell(
  threshold: number | null | undefined,
  ratePoints: number | null | undefined
): string {
  if (threshold == null || ratePoints == null) return '—'
  const amt = threshold.toLocaleString('zh-CN', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  })
  const pts = ratePoints.toLocaleString('zh-CN', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1
  })
  return `${amt} | ${pts}%`
}
