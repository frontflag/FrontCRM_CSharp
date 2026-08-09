<template>
  <div v-if="options.length" class="toolbar__opt report-letterhead-select">
    <span class="toolbar__opt-lbl">{{ t('reportLetterhead.label') }}</span>
    <el-select
      :model-value="modelValue"
      size="small"
      class="report-letterhead-select__el"
      :disabled="disabled"
      @update:model-value="onChange"
    >
      <el-option v-for="opt in options" :key="opt.value" :label="opt.label" :value="opt.value" />
    </el-select>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'

defineProps<{
  modelValue: string
  options: { value: string; label: string }[]
  disabled?: boolean
}>()

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const { t } = useI18n()

function onChange(v: string | number | null | undefined) {
  emit('update:modelValue', v == null ? '' : String(v))
}
</script>

<style scoped lang="scss">
.report-letterhead-select {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.report-letterhead-select__el {
  width: 220px;
}

.toolbar__opt-lbl {
  font-size: 13px;
  color: #8eb4d4;
  white-space: nowrap;
}
</style>
