<template>
  <el-select
    :model-value="modelValue"
    filterable
    allow-create
    default-first-option
    :placeholder="placeholder"
    :disabled="disabled"
    :clearable="clearable"
    style="width: 100%"
    :class="selectClass"
    @update:model-value="onUpdate"
  >
    <el-option v-for="o in options" :key="o.code" :label="o.label" :value="o.code" />
  </el-select>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useMaterialProductionDateDict } from '@/composables/useMaterialProductionDateDict'

withDefaults(
  defineProps<{
    modelValue: string
    placeholder?: string
    disabled?: boolean
    clearable?: boolean
    /** 附加 class，如 q-input / q-select */
    selectClass?: string
  }>(),
  {
    placeholder: '请选择生产日期',
    disabled: false,
    clearable: true,
    selectClass: ''
  }
)

const emit = defineEmits<{ 'update:modelValue': [string] }>()

const { options, ensureLoaded } = useMaterialProductionDateDict()

onMounted(() => {
  void ensureLoaded()
})

function onUpdate(v: string | null | undefined) {
  emit('update:modelValue', v != null && v !== '' ? String(v) : '')
}
</script>
