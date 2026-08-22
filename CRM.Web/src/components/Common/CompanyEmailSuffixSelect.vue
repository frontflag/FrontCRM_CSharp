<template>
  <el-select
    :model-value="displayValue"
    filterable
    allow-create
    default-first-option
    clearable
    :placeholder="placeholder"
    class="q-select"
    style="width: 100%"
    @update:model-value="onUpdate"
  >
    <el-option v-for="s in options" :key="s" :label="s" :value="s" />
  </el-select>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import {
  contactEmailSuffixOptions,
  normalizeCompanyEmailSuffix
} from '@/utils/companyEmailSuffix'

const props = defineProps<{
  modelValue?: string | null
  placeholder?: string
  contactEmails?: Array<string | null | undefined>
}>()

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const options = computed(() => contactEmailSuffixOptions(props.contactEmails ?? []))

const displayValue = computed(() => {
  const v = props.modelValue
  return v == null ? '' : String(v)
})

function onUpdate(v: string | number | boolean | null | undefined) {
  if (v == null || v === '') {
    emit('update:modelValue', '')
    return
  }
  emit('update:modelValue', normalizeCompanyEmailSuffix(String(v)) ?? String(v).trim())
}
</script>

<style scoped lang="scss">
@use '@/assets/styles/variables' as *;

.q-select {
  :deep(.el-select__placeholder.is-transparent) {
    color: $text-placeholder !important;
  }

  :deep(.el-select__placeholder:not(.is-transparent)) {
    color: $text-primary !important;
  }

  :deep(.el-select__input) {
    color: $text-primary !important;
  }

  :deep(.el-select__input::placeholder) {
    color: $text-placeholder !important;
  }
}
</style>
