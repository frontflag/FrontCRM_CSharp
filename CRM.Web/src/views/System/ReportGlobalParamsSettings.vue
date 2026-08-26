<template>
  <div class="report-global-settings" v-loading="loading">
    <div class="section-head">
      <div class="section-head__left">
        <div class="section-title">
          <span class="title-bar"></span>{{ t('reportParams.globalTitle') }}
        </div>
        <p class="section-hint">{{ t('reportParams.globalHint') }}</p>
      </div>
      <div class="section-head__actions">
        <el-button
          v-if="canWrite"
          type="primary"
          :loading="saving"
          @click="save"
        >{{ t('reportParams.saveBtn') }}</el-button>
        <el-button :loading="loading" @click="load">{{ t('reportParams.refreshBtn') }}</el-button>
      </div>
    </div>

    <div class="group-card">
      <div class="field-row">
        <span class="field-label">{{ t('reportParams.styleVersionLabel') }}</span>
        <el-select
          v-model="styleVersion"
          class="version-select"
          :disabled="!canWrite"
          :teleported="false"
        >
          <el-option label="V1" value="V1" />
          <el-option label="V2" value="V2" />
        </el-select>
      </div>
      <p class="field-note">{{ t('reportParams.styleVersionNote') }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores'
import { reportParamsApi, type ReportStyleVersion } from '@/api/reportParams'

const { t } = useI18n()
const authStore = useAuthStore()
const canWrite = authStore.canAccessSystemPermission('system.params.report.global.write')

const loading = ref(false)
const saving = ref(false)
const styleVersion = ref<ReportStyleVersion>('V1')

async function load() {
  loading.value = true
  try {
    styleVersion.value = await reportParamsApi.getStyleVersion()
  } catch {
    ElMessage.error(t('reportParams.loadFailed'))
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  try {
    styleVersion.value = await reportParamsApi.setStyleVersion(styleVersion.value)
    ElMessage.success(t('reportParams.saveSuccess'))
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('reportParams.saveFailed')
    ElMessage.error(msg)
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.report-global-settings {
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
}

.field-label {
  font-size: 13px;
  color: $text-secondary;
  min-width: 120px;
}

.version-select {
  width: 160px;
}

.field-note {
  margin: 12px 0 0;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}
</style>
