<template>
  <div class="default-assign-method-settings" v-loading="loading">
    <div class="section-head">
      <div class="section-head__left">
        <div class="section-title">
          <span class="title-bar"></span>{{ t('purchaseParams.defaultAssignMethodTitle') }}
        </div>
        <p class="section-hint">{{ t('purchaseParams.defaultAssignMethodHint') }}</p>
      </div>
      <div class="section-head__actions">
        <el-button type="primary" :loading="saving" @click="save">{{ t('purchaseParams.saveBtn') }}</el-button>
        <el-button :loading="loading" @click="load">{{ t('purchaseParams.refreshBtn') }}</el-button>
      </div>
    </div>

    <div class="group-card">
      <div class="field-row">
        <span class="field-label">{{ t('purchaseParams.defaultAssignMethodLabel') }}</span>
        <el-select
          v-model="assignMethod"
          class="method-select"
          :teleported="false"
          popper-class="purchase-default-assign-method-select-popper"
        >
          <el-option v-for="o in ASSIGN_METHOD_OPTIONS" :key="o.value" :label="o.label" :value="o.value">
            <span class="assign-method-option">
              <span class="assign-method-option-label">{{ o.label }}</span>
              <el-tooltip :content="o.tip" placement="top" :hide-after="0">
                <el-icon class="assign-method-option-tip" aria-label="说明" @click.stop>
                  <QuestionFilled />
                </el-icon>
              </el-tooltip>
            </span>
          </el-option>
        </el-select>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { QuestionFilled } from '@element-plus/icons-vue'
import { purchaseParamsApi } from '@/api/purchaseParams'
import { ASSIGN_METHOD_OPTIONS } from '@/constants/rfqFormEnums'

const { t } = useI18n()
const loading = ref(false)
const saving = ref(false)
const assignMethod = ref(5)

async function load() {
  loading.value = true
  try {
    assignMethod.value = await purchaseParamsApi.getDefaultAssignMethod()
  } catch {
    ElMessage.error(t('purchaseParams.defaultAssignMethodLoadFailed'))
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  try {
    assignMethod.value = await purchaseParamsApi.setDefaultAssignMethod(assignMethod.value)
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

.default-assign-method-settings {
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

.method-select {
  width: 280px;
}

.assign-method-option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  gap: 8px;
}

.assign-method-option-tip {
  color: $cyan-primary;
  font-size: 14px;
  flex-shrink: 0;
}
</style>

<style lang="scss">
.purchase-default-assign-method-select-popper {
  .el-select-dropdown__item {
    padding-right: 12px;
  }
}
</style>
