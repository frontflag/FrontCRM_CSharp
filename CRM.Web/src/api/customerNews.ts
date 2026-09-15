import apiClient from '@/api/client'

export interface CustomerNewsListItem {
  id: string
  briefingDate: string
  periodStart: string
  periodEnd: string
  generatedAt: string
}

export interface CustomerNewsList {
  canFetch: boolean
  isRunning?: boolean
  items: CustomerNewsListItem[]
}

export interface CustomerNewsDetail {
  id: string
  briefingDate: string
  periodStart: string
  periodEnd: string
  generatedAt: string
  markdown: string
}

export interface CustomerNewsLatestRun {
  id?: string | null
  status?: string | null
  message?: string | null
  generatedAt?: string | null
  isRunning?: boolean
  IsRunning?: boolean
}

export interface CustomerNewsRunResult {
  ran: boolean
  success: boolean
  pending?: boolean
  message: string
  acceptedAt: string
  latest: CustomerNewsDetail | null
}

function base(customerId: string) {
  return `/api/v1/customers/${encodeURIComponent(customerId)}/news`
}

export function isRunningFlag(row: { isRunning?: boolean; IsRunning?: boolean } | null | undefined) {
  return !!(row?.isRunning ?? row?.IsRunning)
}

export const customerNewsApi = {
  list(customerId: string) {
    return apiClient.get<CustomerNewsList>(base(customerId))
  },
  getById(customerId: string, briefingId: string) {
    return apiClient.get<CustomerNewsDetail>(`${base(customerId)}/${encodeURIComponent(briefingId)}`)
  },
  latestRun(customerId: string) {
    return apiClient.get<CustomerNewsLatestRun>(`${base(customerId)}/latest-run`)
  },
  run(customerId: string) {
    return apiClient.post<CustomerNewsRunResult>(`${base(customerId)}/run`, null, {
      timeout: 30000
    })
  }
}
