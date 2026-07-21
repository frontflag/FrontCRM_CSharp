<template>
  <div class="refresh-vendor-settings" v-loading="loading">
    <div class="section-head">
      <div class="section-head__left">
        <div class="section-title">
          <span class="title-bar"></span>{{ t('purchaseParams.refreshVendorTitle') }}
        </div>
        <p class="section-hint">{{ t('purchaseParams.refreshVendorHint') }}</p>
      </div>
      <div class="section-head__actions">
        <el-button type="primary" :loading="saving" @click="save">{{ t('purchaseParams.saveBtn') }}</el-button>
        <el-button :loading="loading" @click="load">{{ t('purchaseParams.refreshBtn') }}</el-button>
      </div>
    </div>

    <div class="group-card">
      <div class="field-row">
        <span class="field-label">{{ t('purchaseParams.allowRefreshCompletedLabel') }}</span>
        <div class="field-control">
          <el-switch
            v-model="allow"
            :active-text="t('purchaseParams.allow')"
            :inactive-text="t('purchaseParams.disallow')"
            inline-prompt
          />
        </div>
      </div>
      <p class="field-note">{{ t('purchaseParams.allowRefreshCompletedNote') }}</p>
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
const allow = ref(false)

async function load() {
  loading.value = true
  try {
    allow.value = await purchaseParamsApi.getAllowRefreshCompletedBizNodes()
  } catch {
    ElMessage.error(t('purchaseParams.refreshVendorLoadFailed'))
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  try {
    allow.value = await purchaseParamsApi.setAllowRefreshCompletedBizNodes(allow.value)
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

.refresh-vendor-settings {
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
  min-width: 180px;
}

.field-control {
  display: flex;
  align-items: center;
}

.field-note {
  margin: 12px 0 0;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}
</style>
