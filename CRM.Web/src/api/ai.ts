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
  }
}

export const AI_SCENARIO_MATERIAL_SPEC_LOOKUP = 'material.spec.lookup'
export const AI_SCENARIO_MATERIAL_INTEL_LOOKUP = 'material.intel.lookup'
export const AI_PERMISSION_MATERIAL_INTEL_LOOKUP = 'biz.ai.material_intel.lookup'
