import apiClient from '@/api/client'

export interface IndustryNewsItem {
  category: string
  title: string
  occurredOn: string
  summary: string
  importance: number
  isBackground: boolean
  unconfirmed: boolean
}

export interface IndustryNewsLatest {
  hasBriefing: boolean
  isStale: boolean
  briefingDate: string | null
  periodStart: string | null
  periodEnd: string | null
  generatedAt: string | null
  items: IndustryNewsItem[]
  markdown: string
}

export interface IndustryNewsRunResult {
  ran: boolean
  success: boolean
  message: string
  latest: IndustryNewsLatest | null
}

export const industryNewsApi = {
  getLatest() {
    return apiClient.get<IndustryNewsLatest>('/api/v1/industry-news/latest')
  },
  runToday(force = true) {
    return apiClient.post<IndustryNewsRunResult>('/api/v1/industry-news/run', null, {
      params: { force },
      timeout: 180000
    })
  }
}
