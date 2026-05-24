<template>
  <el-select
    :model-value="modelValue || undefined"
    :placeholder="placeholder"
    :clearable="clearable"
    filterable
    style="width: 100%"
    :loading="loading"
    @update:model-value="onUpdate"
    @change="onChange"
  >
    <el-option
      v-for="u in options"
      :key="u.id"
      :label="u.userName"
      :value="u.id"
    />
  </el-select>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { authApi, type PurchaseOpsStaffUserOption } from '@/api/auth'

withDefaults(
  defineProps<{
    modelValue?: string
    placeholder?: string
    clearable?: boolean
  }>(),
  {
    modelValue: '',
    placeholder: '请选择采购助理',
    clearable: true
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'change', payload: { id: string; label: string }): void
}>()

const loading = ref(false)
const options = ref<PurchaseOpsStaffUserOption[]>([])

function onUpdate(val: string | undefined) {
  emit('update:modelValue', val ? String(val) : '')
}

function onChange(val: string | undefined) {
  const id = val ? String(val) : ''
  const row = options.value.find((x) => x.id === id)
  emit('change', { id, label: row?.userName ?? '' })
}

onMounted(async () => {
  loading.value = true
  try {
    options.value = await authApi.getPurchaseOpsStaffUsers()
  } catch {
    options.value = []
  } finally {
    loading.value = false
  }
})
</script>
