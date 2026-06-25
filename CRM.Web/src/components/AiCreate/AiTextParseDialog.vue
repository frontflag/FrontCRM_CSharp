<template>
  <el-dialog
    v-model="visibleModel"
    :title="t('aiEntityCreate.textDialog.title')"
    width="640px"
    destroy-on-close
    :close-on-click-modal="false"
    @closed="onClosed"
  >
    <p class="ai-text-parse-dialog__hint">{{ t('aiEntityCreate.textDialog.hint') }}</p>
    <el-input
      v-model="rawText"
      type="textarea"
      :rows="12"
      :placeholder="placeholder"
      :disabled="loading"
      class="ai-text-parse-dialog__textarea"
    />
    <template #footer>
      <el-button @click="visibleModel = false" :disabled="loading">{{ t('common.cancel') }}</el-button>
      <el-button type="primary" :loading="loading" @click="emitGenerate">
        {{ t('aiEntityCreate.textDialog.generate') }}
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'

const props = defineProps<{
  visible: boolean
  loading: boolean
  placeholder?: string
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  generate: [rawText: string]
}>()

const { t } = useI18n()
const rawText = ref('')

const visibleModel = computed({
  get: () => props.visible,
  set: (v) => emit('update:visible', v)
})

watch(
  () => props.visible,
  (open) => {
    if (open) rawText.value = ''
  }
)

function onClosed() {
  rawText.value = ''
}

function emitGenerate() {
  const text = rawText.value.trim()
  if (!text) {
    ElMessage.warning(t('aiEntityCreate.textDialog.emptyText'))
    return
  }
  emit('generate', text)
}
</script>

<style scoped lang="scss">
.ai-text-parse-dialog__hint {
  margin: 0 0 12px;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.ai-text-parse-dialog__textarea :deep(textarea) {
  font-family: inherit;
  line-height: 1.5;
}
</style>
