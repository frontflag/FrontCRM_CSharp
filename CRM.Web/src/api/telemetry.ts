import apiClient from './client'

export interface TelemetryPageRankRow {
  pageKey: string
  description?: string | null
  viewCount: number
  visibleMsSum: number
  activeMsSum: number
}

export interface TelemetryActionRankRow {
  pageKey: string
  actionId: string
  description?: string | null
  clickCount: number
}

export interface TelemetryApiRankRow {
  method: string
  pathTemplate: string
  description?: string | null
  callCount: number
  failCount: number
  avgDurationMs: number
  maxDurationMs: number
}

export const telemetryApi = {
  topPages(params: { startDate?: string; endDate?: string; take?: number }) {
    return apiClient.get<TelemetryPageRankRow[]>('/api/v1/telemetry/analytics/top-pages', { params })
  },
  topActions(params: { startDate?: string; endDate?: string; take?: number }) {
    return apiClient.get<TelemetryActionRankRow[]>('/api/v1/telemetry/analytics/top-actions', {
      params
    })
  },
  topApis(params: { startDate?: string; endDate?: string; take?: number }) {
    return apiClient.get<TelemetryApiRankRow[]>('/api/v1/telemetry/analytics/top-apis', { params })
  }
}
