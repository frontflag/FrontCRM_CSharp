<template>
  <div class="demand-protection-settings" v-loading="loading">
    <div class="section-head">
      <div class="section-head__left">
        <div class="section-title">
          <span class="title-bar"></span>{{ t('purchaseParams.demandProtectionTitle') }}
        </div>
        <p class="section-hint">{{ t('purchaseParams.demandProtectionHint') }}</p>
      </div>
      <div class="section-head__actions">
        <el-button type="primary" :loading="saving" @click="save">{{ t('purchaseParams.saveBtn') }}</el-button>
        <el-button :loading="loading" @click="load">{{ t('purchaseParams.refreshBtn') }}</el-button>
      </div>
    </div>

    <div class="group-card">
      <div class="field-row">
        <span class="field-label">{{ t('purchaseParams.demandProtectionLabel') }}</span>
        <el-input-number
          v-model="minutes"
          class="minutes-input"
          :min="0"
          :max="43200"
          :step="5"
          controls-position="right"
        />
        <span class="field-unit">{{ t('purchaseParams.demandProtectionUnit') }}</span>
      </div>
      <p class="field-note">{{ t('purchaseParams.demandProtectionNote') }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { purchaseParamsApi } from '@/api/purchaseParams'

const { t } = useI18n()
const loading = ref(false)
const saving = ref(false)
const minutes = ref(30)

async function load() {
  loading.value = true
  try {
    minutes.value = await purchaseParamsApi.getDemandProtectionMinutes()
  } catch {
    ElMessage.error(t('purchaseParams.demandProtectionLoadFailed'))
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  try {
    minutes.value = await purchaseParamsApi.setDemandProtectionMinutes(minutes.value)
    ElMessage.success(t('purchaseParams.saveSuccess'))
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('purchaseParams.saveFailed')
    ElMessage.error(msg)
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.demand-protection-settings {
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
  gap: 12px;
}

.field-label {
  font-size: 13px;
  color: $text-secondary;
  min-width: 120px;
}

.minutes-input {
  width: 160px;
}

.field-unit {
  font-size: 13px;
  color: $text-muted;
}

.field-note {
  margin: 12px 0 0;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}
</style>
