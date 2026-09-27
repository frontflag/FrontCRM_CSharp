import apiClient from './client'

export type AiAssistantSession = {
  sessionId: string
  status: string
  welcomeMessage: string
  inferredBizRef?: string | null
}

export type AiAssistantMessage = {
  id: string
  role: string
  content?: string | null
  attachmentDocumentId?: string | null
  createTime: string
}

export type AiAssistantChatTurn = {
  sessionId: string
  status: string
  assistantMessage: string
  conversationAction: string
  feedbackId?: string | null
  messages: AiAssistantMessage[]
}

export type CreateAiAssistantSessionPayload = {
  pageUrl?: string
  routeName?: string
  routeParamsJson?: string
  routeQueryJson?: string
  userAgent?: string
  preferredCategory?: string | null
}

export type SendAiAssistantMessagePayload = {
  text?: string
  attachmentDocumentId?: string
  imageBase64?: string
  imageMimeType?: string
  imageFileName?: string
  /** 本轮其他技能的可见对话。只进模型提示，不写入反馈正文。 */
  backgroundContext?: string
}

const AI_TIMEOUT_MS = 90_000

export type AiDataQueryState = {
  intent?: string | null
  metric?: string | null
  measure?: string | null
  basis?: string | null
  ageDays?: number | null
  dimension?: string | null
  topN?: number | null
  period?: string | null
  pendingSlot?: string | null
  missCount?: number
}

export type AiDataQueryOption = { id: string; label: string }

export type AiDataQueryPoint = { label: string; value?: number | null }

export type AiDataQueryChart = {
  type: 'number' | 'line' | 'bar' | string
  unit?: string
  points: AiDataQueryPoint[]
}

export type AiDataQueryRow = {
  label: string
  valueText?: string | null
  routeName?: string | null
  routeQueryKey?: string | null
  routeQueryValue?: string | null
}

export type AiDataQueryLink = {
  label: string
  routeName: string
  queryKey?: string | null
  queryValue?: string | null
}

export type AiDataQueryResponse = {
  kind: string
  summary: string
  basisNote?: string | null
  state: AiDataQueryState
  options: AiDataQueryOption[]
  chart?: AiDataQueryChart | null
  rows: AiDataQueryRow[]
  link?: AiDataQueryLink | null
}

export type AiDataQueryPayload = {
  question: string
  state?: AiDataQueryState | null
}

export const aiAssistantApi = {
  createSession(payload: CreateAiAssistantSessionPayload) {
    return apiClient.post<AiAssistantSession>('/api/v1/ai-assistant/sessions', payload, {
      timeout: AI_TIMEOUT_MS
    })
  },
  sendMessage(sessionId: string, payload: SendAiAssistantMessagePayload) {
    return apiClient.post<AiAssistantChatTurn>(
      `/api/v1/ai-assistant/sessions/${encodeURIComponent(sessionId)}/messages`,
      payload,
      { timeout: AI_TIMEOUT_MS }
    )
  },
  routeSkill(text: string) {
    return apiClient.post<{ skill: string }>('/api/v1/ai-assistant/route', { text }, {
      timeout: AI_TIMEOUT_MS
    })
  },
  queryData(payload: AiDataQueryPayload) {
    return apiClient.post<AiDataQueryResponse>('/api/v1/ai-assistant/data-query', payload, {
      timeout: AI_TIMEOUT_MS
    })
  }
}
