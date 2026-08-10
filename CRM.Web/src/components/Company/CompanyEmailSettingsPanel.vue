<template>
  <div class="email-panel">
    <el-tabs v-model="innerTab" class="email-panel-tabs" @tab-change="onTabChange">
      <el-tab-pane :label="t('companyInfo.smtp.tabSettings')" name="settings">
        <div class="section-head">
          <div class="section-head__left">
            <p class="section-hint">{{ t('companyInfo.smtp.sectionHint') }}</p>
          </div>
          <el-button type="primary" class="save-all-btn" :loading="saving" @click="$emit('save')">
            {{ t('companyInfo.saveAll') }}
          </el-button>
        </div>

        <div class="group-card group-card--single">
          <h4 class="server-title">{{ t('companyInfo.smtp.serverSectionTitle') }}</h4>
          <el-form label-width="140px" class="settings-form" :model="model">
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item :label="t('companyInfo.smtp.platformSuffix')">
                  <div class="suffix-edit">
                    <span class="suffix-at" aria-hidden="true">@</span>
                    <el-input
                      class="suffix-domain-input"
                      :model-value="suffixDomain"
                      :placeholder="t('companyInfo.smtp.phPlatformSuffix')"
                      clearable
                      @update:model-value="setSuffixDomain"
                    />
                  </div>
                </el-form-item>
              </el-col>
              <el-col :span="24">
                <el-form-item :label="t('companyInfo.smtp.enableOutgoing')">
                  <el-switch v-model="model.enabled" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('companyInfo.smtp.smtpHost')">
                  <el-input v-model="model.smtpHost" :placeholder="t('companyInfo.smtp.phSmtpHost')" clearable />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('companyInfo.smtp.smtpPort')">
                  <el-input-number
                    v-model="model.smtpPort"
                    :min="1"
                    :max="65535"
                    controls-position="right"
                    style="width: 100%"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('companyInfo.smtp.popHost')">
                  <el-input
                    :model-value="model.popHost ?? ''"
                    :placeholder="t('companyInfo.smtp.phPopHost')"
                    clearable
                    @update:model-value="setPopHost"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('companyInfo.smtp.popPort')">
                  <el-input-number
                    :model-value="model.popPort ?? 995"
                    :min="1"
                    :max="65535"
                    controls-position="right"
                    style="width: 100%"
                    @update:model-value="setPopPort"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="24">
                <el-form-item :label="t('companyInfo.smtp.useSsl')">
                  <el-switch :model-value="sslEnabled" @update:model-value="setSsl" />
                  <span class="form-item-hint">{{ t('companyInfo.smtp.sslHint') }}</span>
                </el-form-item>
              </el-col>
            </el-row>
          </el-form>
        </div>
      </el-tab-pane>

      <el-tab-pane :label="t('companyInfo.smtp.tabVerified')" name="verified">
        <p class="section-hint verified-hint">{{ t('companyInfo.smtp.verifiedListHint') }}</p>
        <div class="group-card group-card--single verified-block">
          <el-table
            v-loading="verifiedLoading"
            :data="verifiedRows"
            stripe
            :empty-text="t('companyInfo.smtp.emptyVerified')"
          >
            <el-table-column :label="t('companyInfo.smtp.colAccount')" min-width="120">
              <template #default="{ row }">
                <span>{{ row.realName || row.userName }}</span>
                <span v-if="row.realName" class="muted"> ({{ row.userName }})</span>
              </template>
            </el-table-column>
            <el-table-column prop="address" :label="t('companyInfo.smtp.colAddress')" min-width="180" />
            <el-table-column :label="t('companyInfo.smtp.colKind')" width="100">
              <template #default="{ row }">
                {{
                  row.kind === 'personal'
                    ? t('companyInfo.smtp.kindPersonal')
                    : t('companyInfo.smtp.kindPlatform')
                }}
              </template>
            </el-table-column>
            <el-table-column prop="displayName" :label="t('companyInfo.smtp.colDisplayName')" min-width="120" />
            <el-table-column :label="t('companyInfo.smtp.colPassword')" min-width="160">
              <template #default="{ row }">
                <span class="pwd-reveal">
                  <span>{{ plainById[row.id] != null ? plainById[row.id] : '******' }}</span>
                  <el-button
                    v-if="row.passwordSet"
                    link
                    type="primary"
                    :loading="revealingId === row.id"
                    @click="toggleReveal(row.id)"
                  >
                    {{ plainById[row.id] != null ? t('companyInfo.smtp.hidePwd') : t('companyInfo.smtp.showPwd') }}
                  </el-button>
                </span>
              </template>
            </el-table-column>
            <el-table-column :label="t('companyInfo.smtp.colVerifiedAt')" width="170">
              <template #default="{ row }">{{ formatAt(row.verifiedAt) }}</template>
            </el-table-column>
          </el-table>
        </div>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import type { CompanySmtpEmailSettings } from '@/api/companyProfile'
import {
  fetchVerifiedUserMailboxes,
  revealVerifiedMailboxPassword,
  type VerifiedUserMailboxRow
} from '@/api/userMailboxes'
import { getApiErrorMessage } from '@/utils/apiError'

const model = defineModel<CompanySmtpEmailSettings>({ required: true })

const props = defineProps<{
  saving?: boolean
  /** 进入「公司邮箱」侧栏时为 true */
  active?: boolean
}>()

defineEmits<{ save: [] }>()

const { t } = useI18n()

const innerTab = ref<'settings' | 'verified'>('settings')

/** 展示/编辑域名部分；入库仍带前导 @ */
const suffixDomain = computed(() =>
  String(model.value.platformEmailSuffix ?? '')
    .trim()
    .replace(/^@+/, '')
)

function setSuffixDomain(v: string) {
  const domain = String(v ?? '')
    .trim()
    .replace(/^@+/, '')
  model.value.platformEmailSuffix = domain ? `@${domain}` : ''
}
function setPopHost(v: string) {
  model.value.popHost = v
}
function setPopPort(v: number | undefined) {
  model.value.popPort = typeof v === 'number' && v >= 1 ? v : 995
}

/** 单一 SSL：同时驱动 SMTP useSsl 与 POP popUseSsl */
const sslEnabled = computed(() => model.value.useSsl !== false && model.value.popUseSsl !== false)

function setSsl(v: string | number | boolean) {
  const on = !!v
  model.value.useSsl = on
  model.value.popUseSsl = on
}

const verifiedRows = ref<VerifiedUserMailboxRow[]>([])
const verifiedLoading = ref(false)
const plainById = reactive<Record<string, string>>({})
const revealingId = ref<string | null>(null)

async function loadVerified() {
  verifiedLoading.value = true
  try {
    verifiedRows.value = await fetchVerifiedUserMailboxes()
  } catch (e) {
    verifiedRows.value = []
    ElMessage.error(getApiErrorMessage(e, t('companyInfo.messages.loadFailed')))
  } finally {
    verifiedLoading.value = false
  }
}

function onTabChange(name: string | number) {
  if (name === 'verified') void loadVerified()
}

async function toggleReveal(id: string) {
  if (plainById[id] != null) {
    delete plainById[id]
    return
  }
  revealingId.value = id
  try {
    plainById[id] = (await revealVerifiedMailboxPassword(id)) || ''
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('companyInfo.messages.loadFailed')))
  } finally {
    revealingId.value = null
  }
}

function formatAt(v?: string | null) {
  if (!v) return '—'
  try {
    return new Date(v).toLocaleString()
  } catch {
    return String(v)
  }
}

watch(
  () => props.active,
  (on) => {
    if (!on) {
      innerTab.value = 'settings'
      return
    }
    if (innerTab.value === 'verified') void loadVerified()
  }
)

onMounted(() => {
  if (props.active && innerTab.value === 'verified') void loadVerified()
})

defineExpose({
  clearRevealed() {
    for (const k of Object.keys(plainById)) delete plainById[k]
  },
  reloadVerified: loadVerified
})
</script>

<style scoped lang="scss">
@use '@/assets/styles/variables' as vars;

.email-panel-tabs {
  :deep(.el-tabs__header) {
    margin-bottom: 16px;
  }

  :deep(.el-tabs__item) {
    font-size: 13px;
  }

  :deep(.el-tabs__active-bar) {
    background-color: vars.$cyan-primary;
  }

  :deep(.el-tabs__item.is-active) {
    color: vars.$cyan-primary;
    font-weight: 600;
  }
}

.section-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.section-head__left {
  flex: 1;
  min-width: 0;
}

.section-hint {
  margin: 0;
  font-size: 12px;
  color: vars.$text-muted;
  line-height: 1.5;
}

.verified-hint {
  margin-bottom: 12px;
}

.save-all-btn {
  flex-shrink: 0;
}

.group-card--single {
  max-width: 920px;
  background: rgba(0, 212, 255, 0.03);
  border: 1px solid vars.$border-panel;
  border-radius: 8px;
  padding: 14px 16px 8px;
  margin-bottom: 14px;
}

.server-title {
  margin: 0 0 12px;
  font-size: 14px;
  font-weight: 600;
  color: vars.$text-primary;
}

.suffix-edit {
  display: flex;
  align-items: stretch;
  width: 100%;
}

.suffix-at {
  display: inline-flex;
  align-items: center;
  flex-shrink: 0;
  height: 32px;
  padding: 0 10px;
  font-size: 14px;
  color: vars.$text-muted;
  background: rgba(0, 0, 0, 0.12);
  border: 1px solid vars.$border-panel;
  border-right: none;
  border-radius: 4px 0 0 4px;
  user-select: none;
}

.suffix-domain-input {
  flex: 1;
  min-width: 0;

  :deep(.el-input__wrapper) {
    border-top-left-radius: 0;
    border-bottom-left-radius: 0;
  }
}

.verified-block {
  max-width: none;
  padding-bottom: 14px;
}

.form-item-hint {
  margin-left: 12px;
  font-size: 12px;
  color: vars.$text-muted;
}

.muted {
  color: vars.$text-muted;
  font-size: 12px;
}

.pwd-reveal {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 13px;
}

.settings-form {
  :deep(.el-form-item__label) {
    color: vars.$text-muted;
    font-size: 13px;
  }
}
</style>
