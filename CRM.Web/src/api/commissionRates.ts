import apiClient from './client'

export const COMMISSION_LADDER_COUNT = 10
export const COMMISSION_ROLE_SALES = 1
export const COMMISSION_ROLE_PURCHASE = 2
export const COMMISSION_VERSION_DRAFT = 1
export const COMMISSION_VERSION_ACTIVE = 2
export const COMMISSION_VERSION_DISABLED = 3
export const COMMISSION_DELAY_DAYS_MIN = 0
export const COMMISSION_DELAY_DAYS_MAX = 3650

export interface CommissionRateRow {
  id: string
  versionId: string
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

export interface CommissionRateVersion {
  id: string
  roleType: number
  versionNo: number
  remark?: string | null
  status: number
  isActive: boolean
}

export interface CommissionRateSettings {
  salesVersions: CommissionRateVersion[]
  purchaseVersions: CommissionRateVersion[]
  salesActiveVersionId?: string | null
  purchaseActiveVersionId?: string | null
  receiptWriteoffDelayDays: number
}

export const commissionRatesApi = {
  settings() {
    return apiClient.get<CommissionRateSettings>('/api/v1/commission-rates/settings')
  },
  listVersions(roleType: number) {
    return apiClient.get<CommissionRateVersion[]>('/api/v1/commission-rates/versions', {
      params: { roleType }
    })
  },
  createVersion(payload: { roleType: number; remark: string; copyFromVersionId?: string | null }) {
    return apiClient.post<CommissionRateVersion>('/api/v1/commission-rates/versions', payload)
  },
  updateVersion(id: string, remark: string) {
    return apiClient.put<CommissionRateVersion>(
      `/api/v1/commission-rates/versions/${encodeURIComponent(id)}`,
      { remark }
    )
  },
  setActive(payload: {
    salesVersionId: string
    purchaseVersionId: string
    receiptWriteoffDelayDays: number
  }) {
    return apiClient.put<CommissionRateSettings>('/api/v1/commission-rates/versions/active', payload)
  },
  list(roleType: number, versionId?: string | null) {
    return apiClient.get<CommissionRateRow[]>('/api/v1/commission-rates', {
      params: { roleType, versionId: versionId || undefined }
    })
  },
  update(id: string, payload: { remark?: string | null; ladders: CommissionLadderWrite[] }) {
    return apiClient.put<CommissionRateRow>(`/api/v1/commission-rates/${encodeURIComponent(id)}`, payload)
  }
}

export function formatCommissionVersionLabel(v: CommissionRateVersion, activeText: string): string {
  const remark = (v.remark || '').trim()
  const base = remark ? `V${v.versionNo}　${remark}` : `V${v.versionNo}`
  return v.isActive ? `${base}（${activeText}）` : base
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
