<template>
  <div class="assignee-count-settings" v-loading="loading">
    <div class="section-head">
      <div class="section-head__left">
        <div class="section-title">
          <span class="title-bar"></span>{{ t('purchaseParams.assigneeCountTitle') }}
        </div>
        <p class="section-hint">{{ t('purchaseParams.assigneeCountHint') }}</p>
      </div>
      <div class="section-head__actions">
        <el-button type="primary" :loading="saving" @click="save">{{ t('purchaseParams.saveBtn') }}</el-button>
        <el-button :loading="loading" @click="load">{{ t('purchaseParams.refreshBtn') }}</el-button>
      </div>
    </div>

    <div class="group-card">
      <div class="field-row">
        <span class="field-label">{{ t('purchaseParams.assigneeCountLabel') }}</span>
        <el-select v-model="count" class="count-select" :teleported="false">
          <el-option :label="t('purchaseParams.assigneeCountOption1')" :value="1" />
          <el-option :label="t('purchaseParams.assigneeCountOption2')" :value="2" />
        </el-select>
      </div>
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
const count = ref(2)
const savedCount = ref(2)

async function load() {
  loading.value = true
  try {
    const n = await purchaseParamsApi.getAssigneeCount()
    count.value = n === 1 ? 1 : 2
    savedCount.value = count.value
  } catch {
    ElMessage.error(t('purchaseParams.assigneeCountLoadFailed'))
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  try {
    const n = await purchaseParamsApi.setAssigneeCount(count.value)
    count.value = n === 1 ? 1 : 2
    savedCount.value = count.value
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

.assignee-count-settings {
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
  min-width: 80px;
}

.count-select {
  width: 200px;
}
</style>
