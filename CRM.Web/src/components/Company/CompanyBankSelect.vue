<template>
  <el-select
    :model-value="modelValue"
    :placeholder="placeholder"
    filterable
    :clearable="clearable"
    :disabled="disabled"
    style="width: 100%"
    @update:model-value="onModelUpdate"
    @visible-change="onVisibleChange"
  >
    <el-option
      v-for="row in visibleOptions"
      :key="row.id"
      :label="optionLabel(row)"
      :value="row.id"
    />
  </el-select>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import type { CompanyBankRow } from '@/api/companyProfile'
import { useCompanyBankOptions } from '@/composables/useCompanyBankOptions'
import { formatCompanyBankOptionLabel } from '@/utils/companyBank'

const props = withDefaults(
  defineProps<{
    modelValue?: string
    placeholder?: string
    clearable?: boolean
    disabled?: boolean
    masked?: boolean
    options?: CompanyBankRow[]
  }>(),
  {
    modelValue: '',
    placeholder: '',
    clearable: false,
    disabled: false,
    masked: false
  }
)

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const { companyBankRows, loadCompanyBankOptions } = useCompanyBankOptions()
const filterQuery = ref('')

const sourceOptions = computed(() =>
  props.options?.length ? props.options : companyBankRows.value
)

const visibleOptions = computed(() => {
  const q = filterQuery.value.trim().toLowerCase()
  if (!q) return sourceOptions.value
  return sourceOptions.value.filter((row) =>
    formatCompanyBankOptionLabel(row).toLowerCase().includes(q)
  )
})

function optionLabel(row: CompanyBankRow): string {
  return formatCompanyBankOptionLabel(row, props.masked)
}

function onModelUpdate(v: string | undefined) {
  emit('update:modelValue', v ? String(v) : '')
}

function onVisibleChange(open: boolean) {
  if (!open) filterQuery.value = ''
}

onMounted(() => {
  if (!props.options?.length) void loadCompanyBankOptions()
})

defineExpose({ loadCompanyBankOptions, companyBankRows })
</script>
