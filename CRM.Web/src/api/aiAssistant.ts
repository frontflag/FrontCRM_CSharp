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
}

const AI_TIMEOUT_MS = 90_000

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
  }
}
