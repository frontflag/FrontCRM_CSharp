<template>
  <div class="finance-exchange-settings">
    <div class="form-section" v-loading="loading">
      <div class="section-head">
        <div class="section-head__left">
          <div class="section-title">
            <span class="title-bar"></span>{{ t('financeParams.exchangeTitle') }}
          </div>
          <p class="section-hint">{{ t('financeParams.exchangeHint') }}</p>
        </div>
        <div class="section-head__actions">
          <el-button type="primary" class="save-all-btn" :loading="saving" @click="save">
            {{ t('financeParams.saveBtn') }}
          </el-button>
          <el-button :loading="loading" @click="load()">{{ t('financeParams.refreshBtn') }}</el-button>
        </div>
      </div>

      <div class="group-card group-card--rates">
        <div class="rate-grid">
          <div class="rate-block rate-block--cny">
            <div class="rate-label">{{ t('financeParams.usdToCny') }}</div>
            <div class="rate-divider" />
            <el-input-number
              v-model="form.usdToCny"
              :min="0.0001"
              :max="999999"
              :precision="4"
              :step="0.0001"
              controls-position="right"
              class="rate-input"
            />
          </div>
          <div class="rate-block rate-block--hkd">
            <div class="rate-label">{{ t('financeParams.usdToHkd') }}</div>
            <div class="rate-divider" />
            <el-input-number
              v-model="form.usdToHkd"
              :min="0.0001"
              :max="999999"
              :precision="4"
              :step="0.0001"
              controls-position="right"
              class="rate-input"
            />
          </div>
          <div class="rate-block rate-block--eur">
            <div class="rate-label">{{ t('financeParams.usdToEur') }}</div>
            <div class="rate-divider" />
            <el-input-number
              v-model="form.usdToEur"
              :min="0.0001"
              :max="999999"
              :precision="4"
              :step="0.0001"
              controls-position="right"
              class="rate-input"
            />
          </div>
        </div>
      </div>

      <p v-if="lastSavedText" class="meta-line">{{ lastSavedText }}</p>

      <div class="group-card group-card--log" v-loading="logLoading">
        <div class="group-card__head">
          <span class="group-card__title">{{ t('financeParams.changeLogTitle') }}</span>
        </div>
        <el-table :data="logRows" border stripe size="small" class="log-table">
          <el-table-column prop="changeTimeUtc" :label="t('financeParams.colChangeTime')" width="200">
            <template #default="{ row }">
              {{ formatTime(row.changeTimeUtc) }}
            </template>
          </el-table-column>
          <el-table-column
            prop="changeUserName"
            :label="t('financeParams.colChangeUser')"
            width="140"
            show-overflow-tooltip
          />
          <el-table-column
            prop="changeSummary"
            :label="t('financeParams.colChangeContent')"
            min-width="280"
            show-overflow-tooltip
          />
          <el-table-column prop="usdToCny" :label="t('financeParams.colCny')" width="110" align="right" />
          <el-table-column prop="usdToHkd" :label="t('financeParams.colHkd')" width="110" align="right" />
          <el-table-column prop="usdToEur" :label="t('financeParams.colEur')" width="110" align="right" />
        </el-table>
        <div class="pager">
          <el-pagination
            v-model:current-page="logPage"
            :page-size="logPageSize"
            :total="logTotal"
            layout="total, prev, pager, next"
            @current-change="loadLog"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { financeExchangeRateApi } from '@/api/financeExchangeRate'

const { t, locale } = useI18n()

const loading = ref(false)
const saving = ref(false)
const logLoading = ref(false)
const form = reactive({ usdToCny: 6.9194, usdToHkd: 7.8367, usdToEur: 0.8725 })
const modifyTimeUtc = ref<string | null>(null)
const modifyUserName = ref<string | null>(null)

const logRows = ref<
  Array<{
    changeTimeUtc: string
    changeUserName?: string | null
    changeSummary?: string | null
    usdToCny: number
    usdToHkd: number
    usdToEur: number
  }>
>([])
const logPage = ref(1)
const logPageSize = ref(20)
const logTotal = ref(0)

const lastSavedText = computed(() => {
  if (!modifyTimeUtc.value) return ''
  return t('financeParams.lastSaved', {
    time: formatTime(modifyTimeUtc.value),
    user: modifyUserName.value || '—'
  })
})

function formatTime(iso: string) {
  try {
    const d = new Date(iso)
    if (Number.isNaN(d.getTime())) return iso
    return new Intl.DateTimeFormat(locale.value === 'zh-CN' ? 'zh-CN' : 'en-US', {
      dateStyle: 'short',
      timeStyle: 'medium'
    }).format(d)
  } catch {
    return iso
  }
}

async function load(options?: { silent?: boolean }) {
  const silent = options?.silent === true
  loading.value = true
  try {
    const d = await financeExchangeRateApi.getCurrent()
    form.usdToCny = Number(d.usdToCny)
    form.usdToHkd = Number(d.usdToHkd)
    form.usdToEur = Number(d.usdToEur)
    modifyTimeUtc.value = d.modifyTimeUtc ?? null
    modifyUserName.value = d.modifyUserName ?? null
    return true
  } catch (e: unknown) {
    if (!silent) ElMessage.error((e as Error)?.message || t('financeParams.loadFailed'))
    return false
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  try {
    const d = await financeExchangeRateApi.save({
      usdToCny: form.usdToCny,
      usdToHkd: form.usdToHkd,
      usdToEur: form.usdToEur
    })
    modifyTimeUtc.value = d.modifyTimeUtc ?? null
    modifyUserName.value = d.modifyUserName ?? null
    ElMessage.success(t('financeParams.saveSuccess'))
    await loadLog()
  } catch (e: unknown) {
    ElMessage.error((e as Error)?.message || t('financeParams.saveFailed'))
  } finally {
    saving.value = false
  }
}

async function loadLog(options?: { silent?: boolean }) {
  const silent = options?.silent === true
  logLoading.value = true
  try {
    const p = await financeExchangeRateApi.getChangeLog(logPage.value, logPageSize.value)
    logRows.value = p.items || []
    logTotal.value = p.totalCount ?? 0
    return true
  } catch (e: unknown) {
    if (!silent) ElMessage.error((e as Error)?.message || t('financeParams.logLoadFailed'))
    return false
  } finally {
    logLoading.value = false
  }
}

onMounted(async () => {
  const [okCurrent, okLog] = await Promise.all([load({ silent: true }), loadLog({ silent: true })])
  if (!okCurrent && !okLog) {
    ElMessage.error(t('financeParams.loadAllFailed'))
  } else if (!okCurrent) {
    ElMessage.error(t('financeParams.loadFailed'))
  } else if (!okLog) {
    ElMessage.error(t('financeParams.logLoadFailed'))
  }
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.finance-exchange-settings {
  min-width: 0;
}

.form-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 20px 24px;
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

.section-head__actions {
  display: flex;
  flex-shrink: 0;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
  margin-top: 2px;
}

.save-all-btn {
  flex-shrink: 0;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
  margin: 0 0 6px;

  .title-bar {
    width: 3px;
    height: 16px;
    background: linear-gradient(180deg, #00c8ff, #0066cc);
    border-radius: 2px;
    flex-shrink: 0;
  }
}

.section-hint {
  font-size: 12px;
  color: $text-muted;
  margin: 0;
  line-height: 1.5;
}

.group-card {
  background: rgba(0, 212, 255, 0.03);
  border: 1px solid $border-panel;
  border-radius: 8px;
  padding: 14px 16px 8px;
  margin-bottom: 14px;

  &--log {
    margin-bottom: 0;
    padding-bottom: 14px;
  }
}

.group-card__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 8px;
  padding-bottom: 10px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.group-card__title {
  font-size: 13px;
  font-weight: 600;
  color: $text-secondary;
}

.rate-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 24px;
}

@media (max-width: 900px) {
  .rate-grid {
    grid-template-columns: 1fr;
  }
}

.rate-block {
  padding: 16px;
  border-radius: 8px;
  background: $layer-3;
  border: 1px solid $border-panel;
}

.rate-label {
  font-size: 14px;
  font-weight: 500;
  color: $text-primary;
}

.rate-divider {
  height: 3px;
  border-radius: 2px;
  margin: 12px 0 16px;
}

.rate-block--cny .rate-divider {
  background: linear-gradient(90deg, #79bbff, #a0cfff);
}

.rate-block--hkd .rate-divider {
  background: linear-gradient(90deg, #e6a23c, #f3d19e);
}

.rate-block--eur .rate-divider {
  background: linear-gradient(90deg, #f89898, #fab6b6);
}

.rate-input {
  width: 100%;
}

.rate-input :deep(.el-input__wrapper) {
  width: 100%;
}

.meta-line {
  margin: 0 0 16px;
  font-size: 12px;
  color: $text-muted;
}

.log-table {
  width: 100%;
}

.log-table :deep(.el-table) {
  --el-table-bg-color: #{$layer-3};
  --el-table-tr-bg-color: #{$layer-3};
  --el-table-header-bg-color: #{$layer-2};
}

.pager {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>
