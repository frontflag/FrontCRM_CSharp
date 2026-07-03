<template>
  <div class="ai-config-page">
    <div class="page-header">
      <h1 class="page-title">{{ t('aiConfig.title') }}</h1>
    </div>

    <div v-if="loadError" class="load-error-banner">
      <span>{{ loadError }}</span>
      <el-button size="small" type="primary" @click="loadAll">{{ t('aiConfig.refresh') }}</el-button>
    </div>

    <div v-if="usage" class="usage-cards">
      <div class="usage-card">
        <div class="usage-label">{{ t('aiConfig.usageToday') }}</div>
        <div class="usage-value">{{ usage.todayInvocationCount }} / {{ usage.dailyQuotaLimit }}</div>
      </div>
      <div class="usage-card">
        <div class="usage-label">{{ t('aiConfig.usageTokens') }}</div>
        <div class="usage-value">{{ usage.todayTokenTotal }}</div>
      </div>
      <div class="usage-card">
        <div class="usage-label">{{ t('aiConfig.usageCacheHits') }}</div>
        <div class="usage-value">{{ usage.todayCacheHitCount }}</div>
      </div>
    </div>

    <el-tabs v-model="activeTab" v-loading="loading">
      <el-tab-pane :label="t('aiConfig.tabProviders')" name="providers">
        <el-table :data="providers" stripe size="small" class="ai-providers-table">
          <el-table-column prop="code" label="Code" min-width="1" show-overflow-tooltip />
          <el-table-column prop="name" :label="t('aiConfig.colName')" min-width="1" show-overflow-tooltip />
          <el-table-column prop="baseUrl" label="Base URL" min-width="1" show-overflow-tooltip />
          <el-table-column prop="apiKeyEnv" label="API Key Env" min-width="1" show-overflow-tooltip />
          <el-table-column prop="defaultModel" label="Model" min-width="1" show-overflow-tooltip />
          <el-table-column prop="timeoutSeconds" label="Timeout" min-width="1" align="right" class-name="col-nowrap" label-class-name="col-nowrap" />
          <el-table-column prop="isEnabled" :label="t('aiConfig.colEnabled')" width="80">
            <template #default="{ row }">
              <el-tag :type="row.isEnabled ? 'success' : 'info'" size="small">
                {{ row.isEnabled ? t('aiConfig.yes') : t('aiConfig.no') }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column :label="t('aiConfig.colActions')" width="90" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" size="small" @click="openProviderEdit(row)">{{ t('aiConfig.edit') }}</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-tab-pane>

      <el-tab-pane :label="t('aiConfig.tabScenarios')" name="scenarios">
        <el-table :data="scenarios" stripe size="small">
          <el-table-column prop="code" label="Code" min-width="160" />
          <el-table-column prop="name" :label="t('aiConfig.colName')" min-width="140" />
          <el-table-column prop="providerCode" label="Provider" min-width="120" />
          <el-table-column prop="model" label="Model" min-width="260" />
          <el-table-column prop="permissionCode" label="Permission" min-width="180" show-overflow-tooltip />
          <el-table-column prop="isEnabled" :label="t('aiConfig.colEnabled')" width="80">
            <template #default="{ row }">
              <el-tag :type="row.isEnabled ? 'success' : 'info'" size="small">
                {{ row.isEnabled ? t('aiConfig.yes') : t('aiConfig.no') }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column :label="t('aiConfig.colActions')" width="90" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" size="small" @click="openScenarioEdit(row)">{{ t('aiConfig.edit') }}</el-button>
            </template>
          </el-table-column>
        </el-table>
        <div v-if="!loading && scenarios.length === 0" class="empty-hint">
          {{ loadError ? t('aiConfig.loadFailed') : t('aiConfig.emptyScenarios') }}
        </div>
      </el-tab-pane>

      <el-tab-pane :label="t('aiConfig.tabTemplates')" name="templates">
        <el-table :data="templates" stripe size="small" class="ai-templates-table">
          <el-table-column prop="code" label="Code" min-width="1" show-overflow-tooltip />
          <el-table-column prop="version" label="Ver" min-width="1" align="center" class-name="col-nowrap" label-class-name="col-nowrap" />
          <el-table-column prop="outputFormat" label="Format" min-width="1" align="center" class-name="col-nowrap" label-class-name="col-nowrap" />
          <el-table-column prop="isActive" :label="t('aiConfig.colActive')" width="80">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'" size="small">
                {{ row.isActive ? t('aiConfig.yes') : t('aiConfig.no') }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column :label="t('aiConfig.colActions')" width="90" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" size="small" @click="openTemplateEdit(row)">{{ t('aiConfig.edit') }}</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-tab-pane>

      <el-tab-pane :label="t('aiConfig.tabEntityParseLogs')" name="entityParseLogs">
        <div class="log-toolbar">
          <el-input v-model="entityParseScenarioFilter" placeholder="scenarioCode" clearable style="width: 180px" />
          <el-select v-model="entityParseOutcomeFilter" clearable placeholder="outcome" style="width: 120px">
            <el-option label="parsed" value="parsed" />
            <el-option label="confirmed" value="confirmed" />
            <el-option label="saved" value="saved" />
            <el-option label="failed" value="failed" />
          </el-select>
          <el-input v-model="entityParseEntityFilter" placeholder="entityType" clearable style="width: 160px" />
          <el-button @click="loadEntityParseLogs">{{ t('aiConfig.refresh') }}</el-button>
          <el-button @click="exportEntityParseLogs">{{ t('aiConfig.exportCsv') }}</el-button>
          <el-button type="danger" plain @click="purgeEntityParseLogs">{{ t('aiConfig.purgeOld') }}</el-button>
        </div>
        <el-table :data="entityParseLogs" stripe size="small" class="ai-logs-table" @row-click="openEntityParseDetail">
          <el-table-column prop="createdAt" :label="t('aiConfig.colTime')" width="170">
            <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column prop="scenarioCode" label="Scenario" min-width="150" show-overflow-tooltip />
          <el-table-column prop="entityType" label="Entity" width="130" />
          <el-table-column prop="outcome" label="Outcome" width="96" />
          <el-table-column prop="rawTextLength" label="RawLen" width="80" align="right" />
          <el-table-column prop="savedBizId" label="SavedId" min-width="120" show-overflow-tooltip />
          <el-table-column prop="latencyMs" label="ms" width="72" align="right" />
          <el-table-column prop="userId" label="User" width="100" show-overflow-tooltip />
        </el-table>
      </el-tab-pane>

      <el-tab-pane :label="t('aiConfig.tabLogs')" name="logs">
        <div class="log-toolbar">
          <el-input v-model="logScenarioFilter" placeholder="scenarioCode" clearable style="width: 220px" />
          <el-button @click="loadLogs">{{ t('aiConfig.refresh') }}</el-button>
        </div>
        <el-table :data="logs" stripe size="small" class="ai-logs-table">
          <el-table-column prop="createdAt" :label="t('aiConfig.colTime')" width="170">
            <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column
            prop="executorUserName"
            :label="t('aiConfig.colExecutor')"
            min-width="120"
            show-overflow-tooltip
          >
            <template #default="{ row }">{{ row.executorUserName || '—' }}</template>
          </el-table-column>
          <el-table-column prop="scenarioCode" label="Scenario" min-width="160" show-overflow-tooltip />
          <el-table-column prop="status" label="Status" width="96" class-name="col-nowrap" label-class-name="col-nowrap" />
          <el-table-column
            prop="fromCache"
            label="Cache"
            width="88"
            align="center"
            class-name="col-nowrap"
            label-class-name="col-nowrap"
          >
            <template #default="{ row }">{{ row.fromCache ? 'Y' : 'N' }}</template>
          </el-table-column>
          <el-table-column
            prop="latencyMs"
            label="ms"
            width="100"
            align="right"
            class-name="col-nowrap"
            label-class-name="col-nowrap"
          />
          <el-table-column
            prop="totalTokens"
            label="Tokens"
            width="100"
            align="right"
            class-name="col-nowrap"
            label-class-name="col-nowrap"
          />
          <el-table-column prop="errorMessage" label="Error" min-width="200" show-overflow-tooltip />
        </el-table>
      </el-tab-pane>
    </el-tabs>

    <el-dialog v-model="providerDialogVisible" :title="t('aiConfig.editProvider')" width="560px">
      <el-form v-if="editingProvider" label-width="120px">
        <el-form-item :label="t('aiConfig.colName')"><el-input v-model="editingProvider.name" /></el-form-item>
        <el-form-item label="Base URL"><el-input v-model="editingProvider.baseUrl" /></el-form-item>
        <el-form-item label="API Key Env"><el-input v-model="editingProvider.apiKeyEnv" placeholder="AI_MOONSHOT_API_KEY" /></el-form-item>
        <el-form-item label="Default Model"><el-input v-model="editingProvider.defaultModel" /></el-form-item>
        <el-form-item label="Timeout (s)"><el-input-number v-model="editingProvider.timeoutSeconds" :min="5" :max="600" /></el-form-item>
        <el-form-item :label="t('aiConfig.colEnabled')"><el-switch v-model="editingProvider.isEnabled" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="providerDialogVisible = false">{{ t('aiConfig.cancel') }}</el-button>
        <el-button type="primary" :loading="saving" @click="saveProvider">{{ t('aiConfig.save') }}</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="scenarioDialogVisible" :title="t('aiConfig.editScenario')" width="640px">
      <el-form v-if="editingScenario" label-width="140px">
        <el-form-item :label="t('aiConfig.colName')"><el-input v-model="editingScenario.name" /></el-form-item>
        <el-form-item label="Description"><el-input v-model="editingScenario.description" type="textarea" :rows="2" /></el-form-item>
        <el-form-item label="Provider">
          <el-select
            v-model="editingScenario.providerCode"
            class="field-full"
            filterable
            :teleported="false"
            :placeholder="t('aiConfig.selectProvider')"
            @change="onScenarioProviderChange"
          >
            <el-option
              v-for="p in scenarioProviderOptions"
              :key="p.code"
              :label="providerOptionLabel(p)"
              :value="p.code"
              :disabled="!p.isEnabled"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="Model">
          <el-select
            v-model="editingScenario.model"
            class="field-full"
            filterable
            :teleported="false"
            :placeholder="t('aiConfig.selectModel')"
          >
            <el-option v-for="m in scenarioModelOptions" :key="m" :label="m" :value="m" />
          </el-select>
        </el-form-item>
        <el-form-item label="Cache TTL (s)"><el-input-number v-model="editingScenario.cacheTtlSeconds" :min="0" /></el-form-item>
        <el-form-item label="Max Tokens">
          <el-input-number v-model="editingScenario.maxTokens" :min="256" :max="32768" :step="256" />
        </el-form-item>
        <el-form-item label="Temperature"><el-input-number v-model="editingScenario.temperature" :min="0" :max="2" :step="0.1" /></el-form-item>
        <el-form-item label="Permission"><el-input v-model="editingScenario.permissionCode" /></el-form-item>
        <el-form-item label="Rate/min"><el-input-number v-model="editingScenario.rateLimitPerUserPerMin" :min="1" /></el-form-item>
        <el-form-item :label="t('aiConfig.enableWebSearch')">
          <el-switch v-model="editingScenario.enableWebSearch" />
          <div class="field-hint">{{ t('aiConfig.enableWebSearchHint') }}</div>
        </el-form-item>
        <el-form-item :label="t('aiConfig.colEnabled')"><el-switch v-model="editingScenario.isEnabled" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="scenarioDialogVisible = false">{{ t('aiConfig.cancel') }}</el-button>
        <el-button type="primary" :loading="saving" @click="saveScenario">{{ t('aiConfig.save') }}</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="templateDialogVisible" :title="t('aiConfig.editTemplate')" width="720px">
      <el-form v-if="editingTemplate" label-width="120px">
        <el-form-item label="System Prompt"><el-input v-model="editingTemplate.systemPrompt" type="textarea" :rows="4" /></el-form-item>
        <el-form-item label="User Template"><el-input v-model="editingTemplate.userPromptTemplate" type="textarea" :rows="3" /></el-form-item>
        <el-form-item label="JSON Schema"><el-input v-model="editingTemplate.jsonSchemaHint" type="textarea" :rows="2" /></el-form-item>
        <el-form-item :label="t('aiConfig.colActive')"><el-switch v-model="editingTemplate.isActive" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="templateDialogVisible = false">{{ t('aiConfig.cancel') }}</el-button>
        <el-button type="primary" :loading="saving" @click="saveTemplate">{{ t('aiConfig.save') }}</el-button>
      </template>
    </el-dialog>

    <el-drawer v-model="entityParseDetailVisible" :title="t('aiConfig.entityParseDetailTitle')" size="520px">
      <div v-if="entityParseDetail" class="entity-parse-detail">
        <p><strong>ID:</strong> {{ entityParseDetail.id }}</p>
        <p><strong>Outcome:</strong> {{ entityParseDetail.outcome }}</p>
        <p><strong>Saved:</strong> {{ entityParseDetail.savedBizId || '—' }}</p>
        <h4>{{ t('aiConfig.rawText') }}</h4>
        <pre class="detail-pre">{{ entityParseDetail.rawText || '—' }}</pre>
        <h4>{{ t('aiConfig.parseResult') }}</h4>
        <pre class="detail-pre">{{ formatJson(entityParseDetail.parseResultJson) }}</pre>
        <h4>{{ t('aiConfig.confirmedFields') }}</h4>
        <pre class="detail-pre">{{ formatJson(entityParseDetail.confirmedFieldsJson) }}</pre>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  aiApi,
  type AiProviderAdmin,
  type AiPromptTemplateAdmin,
  type AiScenarioAdmin,
  type AiInvocationLogItem,
  type AiUsageSummary,
  type AiEntityParseLogItem,
  type AiEntityParseLogDetail
} from '@/api/ai'
import { buildModelOptions } from '@/constants/aiProviderModels'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()

const loading = ref(false)
const saving = ref(false)
const loadError = ref<string | null>(null)
const activeTab = ref('providers')
const usage = ref<AiUsageSummary | null>(null)
const providers = ref<AiProviderAdmin[]>([])
const scenarios = ref<AiScenarioAdmin[]>([])
const templates = ref<AiPromptTemplateAdmin[]>([])
const logs = ref<AiInvocationLogItem[]>([])
const logScenarioFilter = ref('')
const entityParseLogs = ref<AiEntityParseLogItem[]>([])
const entityParseScenarioFilter = ref('entity.parse.')
const entityParseOutcomeFilter = ref('')
const entityParseEntityFilter = ref('')
const entityParseDetailVisible = ref(false)
const entityParseDetail = ref<AiEntityParseLogDetail | null>(null)

const providerDialogVisible = ref(false)
const scenarioDialogVisible = ref(false)
const templateDialogVisible = ref(false)
const editingProvider = ref<AiProviderAdmin | null>(null)
const editingScenario = ref<AiScenarioAdmin | null>(null)
const editingTemplate = ref<AiPromptTemplateAdmin | null>(null)

const scenarioProviderOptions = computed(() => {
  const enabled = providers.value.filter((p) => p.isEnabled)
  const cur = editingScenario.value?.providerCode
  if (cur && !enabled.some((p) => p.code === cur)) {
    const extra = providers.value.find((p) => p.code === cur)
    if (extra) return [...enabled, extra]
  }
  return enabled
})

const scenarioModelOptions = computed(() => {
  if (!editingScenario.value) return [] as string[]
  const code = editingScenario.value.providerCode
  const provider = providers.value.find((p) => p.code === code)
  return buildModelOptions(code, provider?.defaultModel, editingScenario.value.model)
})

function providerOptionLabel(p: AiProviderAdmin) {
  return p.name?.trim() ? `${p.code} — ${p.name}` : p.code
}

function onScenarioProviderChange(providerCode: string) {
  if (!editingScenario.value) return
  const provider = providers.value.find((p) => p.code === providerCode)
  const options = buildModelOptions(providerCode, provider?.defaultModel, editingScenario.value.model)
  if (!options.includes(editingScenario.value.model)) {
    editingScenario.value.model = provider?.defaultModel?.trim() || options[0] || ''
  }
}

function formatTime(iso: string) {
  if (!iso) return '—'
  try {
    return new Date(iso).toLocaleString()
  } catch {
    return iso
  }
}

async function loadAll() {
  loading.value = true
  loadError.value = null
  const errors: string[] = []

  try {
    usage.value = await aiApi.getUsage().catch((e: unknown) => {
      errors.push(getApiErrorMessage(e, t('aiConfig.usageLoadFailed')))
      return null
    })
    providers.value = await aiApi.listProviders().catch((e: unknown) => {
      errors.push(getApiErrorMessage(e, t('aiConfig.providersLoadFailed')))
      return [] as AiProviderAdmin[]
    })
    scenarios.value = await aiApi.listScenariosAdmin().catch((e: unknown) => {
      errors.push(getApiErrorMessage(e, t('aiConfig.scenariosLoadFailed')))
      return [] as AiScenarioAdmin[]
    })
    templates.value = await aiApi.listTemplates().catch((e: unknown) => {
      errors.push(getApiErrorMessage(e, t('aiConfig.templatesLoadFailed')))
      return [] as AiPromptTemplateAdmin[]
    })

    if (errors.length > 0) {
      loadError.value = errors[0]
      ElMessage.error(errors[0])
    }
  } finally {
    loading.value = false
  }
}

async function loadLogs() {
  try {
    logs.value = await aiApi.listLogs(80, logScenarioFilter.value.trim() || undefined)
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, t('aiConfig.loadFailed')))
  }
}

function formatJson(value: unknown) {
  if (value == null) return '—'
  try {
    return JSON.stringify(value, null, 2)
  } catch {
    return String(value)
  }
}

async function loadEntityParseLogs() {
  try {
    entityParseLogs.value = await aiApi.listEntityParseLogs({
      take: 100,
      scenarioCode: entityParseScenarioFilter.value.trim() || undefined,
      outcome: entityParseOutcomeFilter.value.trim() || undefined,
      entityType: entityParseEntityFilter.value.trim() || undefined
    })
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, t('aiConfig.loadFailed')))
  }
}

async function openEntityParseDetail(row: AiEntityParseLogItem) {
  try {
    entityParseDetail.value = await aiApi.getEntityParseLogDetail(row.id)
    entityParseDetailVisible.value = true
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, t('aiConfig.loadFailed')))
  }
}

async function exportEntityParseLogs() {
  try {
    const blob = await aiApi.exportEntityParseLogs({
      take: 1000,
      scenarioCode: entityParseScenarioFilter.value.trim() || undefined,
      outcome: entityParseOutcomeFilter.value.trim() || undefined,
      entityType: entityParseEntityFilter.value.trim() || undefined
    })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `ai_entity_parse_logs_${Date.now()}.csv`
    a.click()
    URL.revokeObjectURL(url)
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, t('aiConfig.exportFailed')))
  }
}

async function purgeEntityParseLogs() {
  try {
    await ElMessageBox.confirm(t('aiConfig.purgeConfirm'), t('aiConfig.purgeTitle'), {
      type: 'warning',
      confirmButtonText: t('aiConfig.purgeConfirmBtn'),
      cancelButtonText: t('aiConfig.cancel')
    })
    const result = await aiApi.purgeEntityParseLogs(180)
    ElMessage.success(t('aiConfig.purgeDone', { count: result.deleted }))
    await loadEntityParseLogs()
  } catch (e: unknown) {
    if (e === 'cancel' || e === 'close') return
    ElMessage.error(getApiErrorMessage(e, t('aiConfig.purgeFailed')))
  }
}

function openProviderEdit(row: AiProviderAdmin) {
  editingProvider.value = { ...row }
  providerDialogVisible.value = true
}

function openScenarioEdit(row: AiScenarioAdmin) {
  editingScenario.value = { ...row }
  scenarioDialogVisible.value = true
}

function openTemplateEdit(row: AiPromptTemplateAdmin) {
  editingTemplate.value = { ...row }
  templateDialogVisible.value = true
}

async function saveProvider() {
  if (!editingProvider.value) return
  saving.value = true
  try {
    await aiApi.updateProvider(editingProvider.value.id, editingProvider.value)
    ElMessage.success(t('aiConfig.saved'))
    providerDialogVisible.value = false
    await loadAll()
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, t('aiConfig.saveFailed')))
  } finally {
    saving.value = false
  }
}

async function saveScenario() {
  if (!editingScenario.value) return
  saving.value = true
  try {
    await aiApi.updateScenario(editingScenario.value.id, editingScenario.value)
    ElMessage.success(t('aiConfig.saved'))
    scenarioDialogVisible.value = false
    await loadAll()
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, t('aiConfig.saveFailed')))
  } finally {
    saving.value = false
  }
}

async function saveTemplate() {
  if (!editingTemplate.value) return
  saving.value = true
  try {
    await aiApi.updateTemplate(editingTemplate.value.id, editingTemplate.value)
    ElMessage.success(t('aiConfig.saved'))
    templateDialogVisible.value = false
    await loadAll()
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, t('aiConfig.saveFailed')))
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  await loadAll()
  await loadLogs()
  await loadEntityParseLogs()
})
</script>

<style lang="scss" scoped>
.ai-config-page {
  padding: 20px 24px;
}

.page-header {
  margin-bottom: 16px;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 700;
}

.usage-cards {
  display: flex;
  gap: 16px;
  margin-bottom: 20px;
  flex-wrap: wrap;
}

.usage-card {
  min-width: 160px;
  padding: 12px 16px;
  border: 1px solid #ebeef5;
  border-radius: 8px;
  background: #fff;
}

.usage-label {
  font-size: 12px;
  color: #909399;
}

.usage-value {
  margin-top: 4px;
  font-size: 20px;
  font-weight: 600;
}

.log-toolbar {
  display: flex;
  gap: 8px;
  margin-bottom: 12px;
}

.load-error-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 16px;
  padding: 10px 14px;
  border-radius: 8px;
  border: 1px solid var(--el-color-danger-light-5);
  background: var(--el-color-danger-light-9);
  color: var(--el-color-danger);
  font-size: 13px;
}

.empty-hint {
  margin-top: 12px;
  font-size: 13px;
  color: #909399;
}

.field-full {
  width: 100%;
}

.field-hint {
  margin-top: 6px;
  font-size: 12px;
  line-height: 1.45;
  color: #909399;
}

.ai-providers-table :deep(.el-table__header),
.ai-providers-table :deep(.el-table__body),
.ai-templates-table :deep(.el-table__header),
.ai-templates-table :deep(.el-table__body) {
  table-layout: fixed;
  width: 100%;
}

.ai-providers-table :deep(th.col-nowrap .cell),
.ai-providers-table :deep(td.col-nowrap .cell),
.ai-templates-table :deep(th.col-nowrap .cell),
.ai-templates-table :deep(td.col-nowrap .cell) {
  white-space: nowrap;
}

.ai-logs-table :deep(th.col-nowrap .cell),
.ai-logs-table :deep(td.col-nowrap .cell) {
  white-space: nowrap;
}

.entity-parse-detail {
  font-size: 13px;
  line-height: 1.5;
}

.detail-pre {
  max-height: 200px;
  overflow: auto;
  padding: 8px 10px;
  background: #f5f7fa;
  border-radius: 6px;
  font-size: 12px;
  white-space: pre-wrap;
  word-break: break-word;
}
</style>
