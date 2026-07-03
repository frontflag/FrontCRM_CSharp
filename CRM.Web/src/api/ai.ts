import apiClient from './client'

export type AiTokenUsage = {
  promptTokens: number
  completionTokens: number
  totalTokens: number
}

export type AiInvokeResult = {
  invocationId: string
  fromCache: boolean
  content: string
  data?: Record<string, unknown> | null
  usage?: AiTokenUsage | null
  scenarioCode: string
  providerCode: string
  model: string
  entityParseLogId?: string | null
}

export type AiScenarioListItem = {
  code: string
  name: string
  description?: string | null
  permissionCode: string
}

export type AiProviderAdmin = {
  id: string
  code: string
  name: string
  baseUrl: string
  apiKeyEnv?: string | null
  defaultModel: string
  timeoutSeconds: number
  isEnabled: boolean
}

export type AiPromptTemplateAdmin = {
  id: string
  code: string
  version: number
  systemPrompt: string
  userPromptTemplate: string
  outputFormat: string
  jsonSchemaHint?: string | null
  isActive: boolean
}

export type AiScenarioAdmin = {
  id: string
  code: string
  name: string
  description?: string | null
  providerCode: string
  model: string
  promptTemplateId: string
  cacheTtlSeconds: number
  cacheKeyFieldsJson: string
  allowedInputFieldsJson: string
  maxTokens: number
  temperature: number
  permissionCode: string
  rateLimitPerUserPerMin: number
  isEnabled: boolean
  enableWebSearch: boolean
}

export type AiInvocationLogItem = {
  id: string
  scenarioCode: string
  providerCode: string
  model: string
  userId?: string | null
  executorUserName?: string | null
  status: string
  fromCache: boolean
  latencyMs: number
  totalTokens?: number | null
  errorMessage?: string | null
  createdAt: string
}

export type AiUsageSummary = {
  todayInvocationCount: number
  todayTokenTotal: number
  todayCacheHitCount: number
  dailyQuotaLimit: number
}

export type AiEntityParseLogItem = {
  id: string
  invocationId: string
  scenarioCode: string
  entityType: string
  userId?: string | null
  parentBizType?: string | null
  parentBizId?: string | null
  outcome: string
  savedBizId?: string | null
  rawTextLength: number
  fromCache: boolean
  latencyMs: number
  providerCode: string
  model: string
  createdAt: string
  confirmedAt?: string | null
  savedAt?: string | null
}

export type AiEntityParseLogDetail = AiEntityParseLogItem & {
  rawText: string
  parseResultJson?: Record<string, unknown> | null
  confirmedFieldsJson?: Record<string, unknown> | null
  parseResultRaw?: string | null
}

export type AiEntityParseLogQuery = {
  take?: number
  scenarioCode?: string
  entityType?: string
  outcome?: string
  userId?: string
}

const AI_ADMIN_BASE = '/api/v1/ai/mgmt'

export const aiApi = {
  async invoke(payload: {
    scenarioCode: string
    input: Record<string, string | null | undefined>
    bizType?: string
    bizId?: string
  }): Promise<AiInvokeResult> {
    return apiClient.post<AiInvokeResult>('/api/v1/ai/invoke', payload, { timeout: 180000 })
  },

  async invokeBusinessCard(payload: {
    scenarioCode: string
    front: File
    back?: File | null
    bizType?: string
    bizId?: string
  }): Promise<AiInvokeResult> {
    const form = new FormData()
    form.append('scenarioCode', payload.scenarioCode)
    form.append('file', payload.front)
    if (payload.back) form.append('fileBack', payload.back)
    if (payload.bizType) form.append('bizType', payload.bizType)
    if (payload.bizId) form.append('bizId', payload.bizId)
    return apiClient.post<AiInvokeResult>('/api/v1/ai/invoke-business-card', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 180000
    })
  },

  async confirmEntityParseLog(
    parseLogId: string,
    confirmedFields: Record<string, unknown>
  ): Promise<void> {
    await apiClient.post(`/api/v1/ai/entity-parse-logs/${encodeURIComponent(parseLogId)}/confirm`, {
      confirmedFields
    })
  },

  async markEntityParseSaved(parseLogId: string, savedBizId: string): Promise<void> {
    await apiClient.post(`/api/v1/ai/entity-parse-logs/${encodeURIComponent(parseLogId)}/saved`, {
      savedBizId
    })
  },

  async listScenarios(): Promise<AiScenarioListItem[]> {
    return apiClient.get<AiScenarioListItem[]>('/api/v1/ai/scenarios')
  },

  async listProviders(): Promise<AiProviderAdmin[]> {
    return apiClient.get<AiProviderAdmin[]>(`${AI_ADMIN_BASE}/providers`)
  },

  async updateProvider(id: string, dto: AiProviderAdmin): Promise<void> {
    await apiClient.put(`${AI_ADMIN_BASE}/providers/${encodeURIComponent(id)}`, dto)
  },

  async listTemplates(): Promise<AiPromptTemplateAdmin[]> {
    return apiClient.get<AiPromptTemplateAdmin[]>(`${AI_ADMIN_BASE}/templates`)
  },

  async updateTemplate(id: string, dto: AiPromptTemplateAdmin): Promise<void> {
    await apiClient.put(`${AI_ADMIN_BASE}/templates/${encodeURIComponent(id)}`, dto)
  },

  async listScenariosAdmin(): Promise<AiScenarioAdmin[]> {
    return apiClient.get<AiScenarioAdmin[]>(`${AI_ADMIN_BASE}/scenarios`)
  },

  async updateScenario(id: string, dto: AiScenarioAdmin): Promise<void> {
    await apiClient.put(`${AI_ADMIN_BASE}/scenarios/${encodeURIComponent(id)}`, dto)
  },

  async listLogs(take = 50, scenarioCode?: string): Promise<AiInvocationLogItem[]> {
    return apiClient.get<AiInvocationLogItem[]>(`${AI_ADMIN_BASE}/logs`, {
      params: { take, scenarioCode: scenarioCode || undefined }
    })
  },

  async getUsage(): Promise<AiUsageSummary> {
    return apiClient.get<AiUsageSummary>(`${AI_ADMIN_BASE}/usage`)
  },

  async listEntityParseLogs(query: AiEntityParseLogQuery = {}): Promise<AiEntityParseLogItem[]> {
    return apiClient.get<AiEntityParseLogItem[]>(`${AI_ADMIN_BASE}/entity-parse-logs`, {
      params: {
        take: query.take ?? 50,
        scenarioCode: query.scenarioCode || undefined,
        entityType: query.entityType || undefined,
        outcome: query.outcome || undefined,
        userId: query.userId || undefined
      }
    })
  },

  async getEntityParseLogDetail(id: string): Promise<AiEntityParseLogDetail> {
    return apiClient.get<AiEntityParseLogDetail>(`${AI_ADMIN_BASE}/entity-parse-logs/${encodeURIComponent(id)}`)
  },

  async exportEntityParseLogs(query: AiEntityParseLogQuery = {}): Promise<Blob> {
    return apiClient.getBlob(`${AI_ADMIN_BASE}/entity-parse-logs/export`, {
      params: {
        take: query.take ?? 500,
        scenarioCode: query.scenarioCode || undefined,
        entityType: query.entityType || undefined,
        outcome: query.outcome || undefined,
        userId: query.userId || undefined
      }
    })
  },

  async purgeEntityParseLogs(keepDays = 180): Promise<{ deleted: number; keepDays: number }> {
    return apiClient.post<{ deleted: number; keepDays: number }>(
      `${AI_ADMIN_BASE}/entity-parse-logs/purge`,
      null,
      { params: { keepDays } }
    )
  }
}

export const AI_SCENARIO_MATERIAL_SPEC_LOOKUP = 'material.spec.lookup'
export const AI_SCENARIO_MATERIAL_INTEL_LOOKUP = 'material.intel.lookup'
export const AI_PERMISSION_MATERIAL_INTEL_LOOKUP = 'biz.ai.material_intel.lookup'

export const AI_SCENARIO_ENTITY_PARSE_CUSTOMER = 'entity.parse.customer'
export const AI_SCENARIO_ENTITY_PARSE_RFQ = 'entity.parse.rfq'
export const AI_SCENARIO_ENTITY_PARSE_VENDOR = 'entity.parse.vendor'
export const AI_SCENARIO_ENTITY_PARSE_CUSTOMER_CONTACT = 'entity.parse.customer_contact'
export const AI_SCENARIO_ENTITY_PARSE_VENDOR_CONTACT = 'entity.parse.vendor_contact'
export const AI_SCENARIO_ENTITY_PARSE_CUSTOMER_ADDRESS = 'entity.parse.customer_address'
export const AI_SCENARIO_ENTITY_PARSE_VENDOR_ADDRESS = 'entity.parse.vendor_address'
export const AI_SCENARIO_ENTITY_PARSE_CUSTOMER_BUSINESS_CARD = 'entity.parse.customer_business_card'
export const AI_SCENARIO_ENTITY_PARSE_VENDOR_BUSINESS_CARD = 'entity.parse.vendor_business_card'
export const AI_PERMISSION_ENTITY_PARSE_CUSTOMER = 'biz.ai.entity.parse.customer'
export const AI_PERMISSION_ENTITY_PARSE_RFQ = 'biz.ai.entity.parse.rfq'
export const AI_PERMISSION_ENTITY_PARSE_VENDOR = 'biz.ai.entity.parse.vendor'
export const AI_PERMISSION_ENTITY_PARSE_CUSTOMER_CONTACT = 'biz.ai.entity.parse.customer_contact'
export const AI_PERMISSION_ENTITY_PARSE_VENDOR_CONTACT = 'biz.ai.entity.parse.vendor_contact'
export const AI_PERMISSION_ENTITY_PARSE_CUSTOMER_ADDRESS = 'biz.ai.entity.parse.customer_address'
export const AI_PERMISSION_ENTITY_PARSE_VENDOR_ADDRESS = 'biz.ai.entity.parse.vendor_address'
export const AI_PERMISSION_ENTITY_PARSE_CUSTOMER_BUSINESS_CARD = 'biz.ai.entity.parse.customer_business_card'
export const AI_PERMISSION_ENTITY_PARSE_VENDOR_BUSINESS_CARD = 'biz.ai.entity.parse.vendor_business_card'
