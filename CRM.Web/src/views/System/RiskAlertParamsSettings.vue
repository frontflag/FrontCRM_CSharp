<template>
  <div class="risk-alert-settings" v-loading="loading">
    <div class="section-head">
      <div class="section-head__left">
        <div class="section-title">
          <span class="title-bar"></span>{{ t('riskAlertParams.title') }}
        </div>
        <p class="section-hint">{{ t('riskAlertParams.hint') }}</p>
      </div>
      <div class="section-head__actions">
        <el-button
          v-if="canWrite"
          type="primary"
          :loading="saving"
          @click="save"
        >{{ t('riskAlertParams.saveBtn') }}</el-button>
        <el-button :loading="loading" @click="load">{{ t('riskAlertParams.refreshBtn') }}</el-button>
      </div>
    </div>

    <div class="group-card">
      <div class="field-row">
        <span class="field-label">{{ t('riskAlertParams.inventoryAmount') }}</span>
        <el-input-number
          v-model="form.inventoryAmountUsdMax"
          :min="0"
          :max="999999999.99"
          :precision="2"
          :step="1000"
          :disabled="!canWrite"
          controls-position="right"
        />
        <span class="field-unit">USD</span>
      </div>
      <div class="field-row">
        <span class="field-label">{{ t('riskAlertParams.stockAge') }}</span>
        <el-input-number
          v-model="form.stockAgeDaysMax"
          :min="0"
          :max="9999"
          :step="1"
          :disabled="!canWrite"
          controls-position="right"
        />
        <span class="field-unit">{{ t('riskAlertParams.daysUnit') }}</span>
      </div>
      <div class="field-row">
        <span class="field-label">{{ t('riskAlertParams.receivableAmount') }}</span>
        <el-input-number
          v-model="form.receivableAmountUsdMax"
          :min="0"
          :max="999999999.99"
          :precision="2"
          :step="1000"
          :disabled="!canWrite"
          controls-position="right"
        />
        <span class="field-unit">USD</span>
      </div>
      <div class="field-row">
        <span class="field-label">{{ t('riskAlertParams.customerReceivable') }}</span>
        <el-input-number
          v-model="form.customerReceivableUsdMax"
          :min="0"
          :max="999999999.99"
          :precision="2"
          :step="1000"
          :disabled="!canWrite"
          controls-position="right"
        />
        <span class="field-unit">USD</span>
      </div>
      <div class="field-row">
        <span class="field-label">{{ t('riskAlertParams.soReceivableAge') }}</span>
        <el-input-number
          v-model="form.soReceivableAgeDaysMax"
          :min="0"
          :max="9999"
          :step="1"
          :disabled="!canWrite"
          controls-position="right"
        />
        <span class="field-unit">{{ t('riskAlertParams.daysUnit') }}</span>
      </div>
      <p class="field-note">{{ t('riskAlertParams.zeroNote') }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores'
import { riskAlertApi, type RiskAlertSettings } from '@/api/riskAlert'

const { t } = useI18n()
const authStore = useAuthStore()
const canWrite =
  authStore.canAccessSystemPermission('system.params.risk-alert.write') && authStore.canForceDelete()

const loading = ref(false)
const saving = ref(false)
const form = reactive<RiskAlertSettings>({
  inventoryAmountUsdMax: 0,
  stockAgeDaysMax: 90,
  receivableAmountUsdMax: 0,
  customerReceivableUsdMax: 0,
  soReceivableAgeDaysMax: 90
})

function apply(dto: RiskAlertSettings) {
  form.inventoryAmountUsdMax = Number(dto.inventoryAmountUsdMax ?? 0)
  form.stockAgeDaysMax = Number(dto.stockAgeDaysMax ?? 0)
  form.receivableAmountUsdMax = Number(dto.receivableAmountUsdMax ?? 0)
  form.customerReceivableUsdMax = Number(dto.customerReceivableUsdMax ?? 0)
  form.soReceivableAgeDaysMax = Number(dto.soReceivableAgeDaysMax ?? 0)
}

async function load() {
  loading.value = true
  try {
    apply(await riskAlertApi.getSettings())
  } catch {
    ElMessage.error(t('riskAlertParams.loadFailed'))
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  try {
    apply(await riskAlertApi.putSettings({ ...form }))
    ElMessage.success(t('riskAlertParams.saveSuccess'))
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('riskAlertParams.saveFailed')
    ElMessage.error(msg)
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.risk-alert-settings {
  min-height: 200px;
}

.section-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 16px;
  &__left {
    flex: 1;
    min-width: 0;
  }
  &__actions {
    display: flex;
    gap: 8px;
    flex-shrink: 0;
  }
}

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 6px;
}

.title-bar {
  width: 3px;
  height: 16px;
  background: linear-gradient(180deg, #00c8ff, #0066cc);
  border-radius: 2px;
}

.section-hint {
  margin: 0;
  font-size: 13px;
  color: $text-muted;
  line-height: 1.5;
}

.group-card {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 20px;
}

.field-row {
  display: flex;
  align-items: center;
  gap: 16px;
  & + & {
    margin-top: 14px;
  }
}

.field-label {
  font-size: 13px;
  color: $text-secondary;
  min-width: 180px;
}

.field-unit {
  font-size: 12px;
  color: $text-muted;
}

.field-note {
  margin: 16px 0 0;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}
</style>
