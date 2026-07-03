<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { stockOutBatchApi } from '@/api/stockOutBatch'
import { getApiErrorMessage } from '@/utils/apiError'

const props = defineProps<{
  modelValue: boolean
  batchId: string | null
}>()

const emit = defineEmits<{
  'update:modelValue': [boolean]
  saved: []
}>()

const { t } = useI18n()
const saving = ref(false)
const editForm = ref<{ id: string; globalBatchNo: string; outQty: number } | null>(null)

async function loadBatch(id: string) {
  const row = await stockOutBatchApi.getById(id)
  if (!row) {
    ElMessage.error(t('packingDetail.batchPanel.messages.batchNotFound'))
    emit('update:modelValue', false)
    return
  }
  editForm.value = {
    id: row.id,
    globalBatchNo: String(row.globalBatchNo ?? '').trim(),
    outQty: Number(row.outQty) || 0
  }
}

watch(
  () => [props.modelValue, props.batchId] as const,
  ([open, id]) => {
    if (open && id) void loadBatch(id)
    if (!open) editForm.value = null
  }
)

async function saveEdit() {
  if (!editForm.value?.id) return
  if (editForm.value.outQty <= 0) {
    ElMessage.warning(t('packingDetail.batchPanel.messages.outQtyInvalid'))
    return
  }
  saving.value = true
  try {
    await stockOutBatchApi.update(editForm.value.id, { outQty: editForm.value.outQty })
    ElMessage.success(t('packingDetail.batchPanel.messages.editSuccess'))
    emit('update:modelValue', false)
    emit('saved')
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('packingDetail.batchPanel.messages.editFailed')))
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <el-dialog
    :model-value="modelValue"
    :title="t('packingDetail.batchPanel.editDialog.title')"
    width="420px"
    destroy-on-close
    @update:model-value="emit('update:modelValue', $event)"
  >
    <template v-if="editForm">
      <el-form label-width="120px" @submit.prevent="saveEdit">
        <el-form-item :label="t('batchReconciliation.columns.globalBatchNo')">
          <span>{{ editForm.globalBatchNo || '—' }}</span>
        </el-form-item>
        <el-form-item :label="t('batchReconciliation.columns.outQty')">
          <el-input-number v-model="editForm.outQty" :min="1" :step="1" controls-position="right" />
        </el-form-item>
      </el-form>
    </template>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">{{ t('packingDetail.batchPanel.prompts.cancel') }}</el-button>
      <el-button type="primary" :loading="saving" @click="saveEdit">{{ t('packingDetail.batchPanel.editDialog.save') }}</el-button>
    </template>
  </el-dialog>
</template>
