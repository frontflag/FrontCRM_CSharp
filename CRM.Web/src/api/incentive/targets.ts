import apiClient from '@/api/client'

export interface IncentiveTargetPeriod {
  periodKey: string
  from: string
  to: string
  label: string
  monthSpan: string
  hasTarget: boolean
  targetUsd: number | null
  actualUsd: number | null
  completionPct: number | null
}

export interface IncentiveTargetYear {
  periodKey: string
  hasTarget: boolean
  targetUsd: number | null
  actualUsd: number | null
  completionPct: number | null
}

export interface IncentiveTargetMine {
  roleType: number
  term: IncentiveTargetPeriod
  year: IncentiveTargetYear
}

export interface IncentiveTargetPutBody {
  termTargetUsd: number | null
  yearTargetUsd: number | null
}

export const incentiveTargetsApi = {
  getMine() {
    return apiClient.get<IncentiveTargetMine>('/api/v1/incentive-targets/me')
  },
  putMine(body: IncentiveTargetPutBody) {
    return apiClient.put<IncentiveTargetMine>('/api/v1/incentive-targets/me', body)
  }
}
