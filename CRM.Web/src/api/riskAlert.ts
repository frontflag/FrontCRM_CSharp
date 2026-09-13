import apiClient from '@/api/client'

export interface RiskAlertSettings {
  inventoryAmountUsdMax: number
  stockAgeDaysMax: number
  receivableAmountUsdMax: number
  customerReceivableUsdMax: number
  soReceivableAgeDaysMax: number
}

export type RiskAlertItemCode =
  | 'inventory-amount'
  | 'stock-age'
  | 'receivable-amount'
  | 'customer-receivable'
  | 'so-receivable-age'

export interface RiskAlertItem {
  code: RiskAlertItemCode
  triggered: boolean
  enabled: boolean
  masked: boolean
  actualUsd: number | null
  thresholdUsd: number | null
  actualDays: number | null
  thresholdDays: number | null
  hitCount: number | null
  subjectId: string | null
  subjectCode: string | null
  subjectName: string | null
}

export interface RiskAlertDashboard {
  immediateCount: number
  anyEnabled: boolean
  items: RiskAlertItem[]
}

export const riskAlertApi = {
  getSettings() {
    return apiClient.get<RiskAlertSettings>('/api/v1/risk-alert-params')
  },
  putSettings(body: RiskAlertSettings) {
    return apiClient.put<RiskAlertSettings>('/api/v1/risk-alert-params', body)
  },
  getDashboard() {
    return apiClient.get<RiskAlertDashboard>('/api/v1/risk-alerts/dashboard')
  }
}
