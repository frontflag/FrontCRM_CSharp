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
  pending?: boolean
  message: string
  latest: IndustryNewsLatest | null
}

export function industryNewsFingerprint(row: IndustryNewsLatest | null | undefined) {
  if (!row?.hasBriefing) return ''
  return `${row.briefingDate ?? ''}|${row.generatedAt ?? ''}`
}

export async function waitForIndustryNewsRefresh(
  previous: IndustryNewsLatest | null | undefined,
  timeoutMs = 240_000
): Promise<IndustryNewsLatest> {
  const prevKey = industryNewsFingerprint(previous)
  const deadline = Date.now() + timeoutMs
  while (Date.now() < deadline) {
    await new Promise((r) => setTimeout(r, 3000))
    const latest = await industryNewsApi.getLatest()
    const nextKey = industryNewsFingerprint(latest)
    if (nextKey && nextKey !== prevKey) return latest
  }
  throw new Error('行业新闻仍在生成，请稍后刷新页面查看。')
}

export const industryNewsApi = {
  getLatest() {
    return apiClient.get<IndustryNewsLatest>('/api/v1/industry-news/latest')
  },
  runToday(force = true) {
    return apiClient.post<IndustryNewsRunResult>('/api/v1/industry-news/run', null, {
      params: { force },
      timeout: 30000
    })
  }
}
