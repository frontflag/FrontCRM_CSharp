<template>
  <el-select
    :model-value="modelValue || undefined"
    :placeholder="placeholder"
    :clearable="clearable"
    :disabled="disabled"
    :filterable="true"
    :filter-method="onFilterInput"
    :loading="loading"
    :loading-text="loadingText"
    style="width: 100%"
    @update:model-value="onModelUpdate"
    @change="onSelectChange"
    @visible-change="onVisibleChange"
    @clear="onClear"
  >
    <template #empty>
      <div class="entity-select-empty">
        <span>{{ emptyHint }}</span>
      </div>
    </template>
    <el-option
      v-for="opt in options"
      :key="opt.id"
      :label="opt.name"
      :value="opt.id"
    >
      <div class="entity-select-option">
        <span class="entity-select-option__name">{{ opt.name }}</span>
        <span class="entity-select-option__code">{{ displayCode(opt.code) }}</span>
      </div>
    </el-option>
  </el-select>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { customerApi } from '@/api/customer'
import type { Customer } from '@/types/customer'

export type CustomerSelectOption = {
  id: string
  name: string
  code: string
}

const props = withDefaults(
  defineProps<{
    modelValue?: string | null
    placeholder?: string
    clearable?: boolean
    disabled?: boolean
    /** 编辑回填：当前选中名称（无搜索结果时保留选项） */
    selectedLabel?: string | null
    selectedCode?: string | null
    emptyHint?: string
    loadingText?: string
  }>(),
  {
    modelValue: '',
    placeholder: '请选择客户',
    clearable: true,
    disabled: false,
    selectedLabel: '',
    selectedCode: '',
    emptyHint: '请输入内容之后选择',
    loadingText: '搜索中...'
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'change', option: CustomerSelectOption | null): void
}>()

const options = ref<CustomerSelectOption[]>([])
const loading = ref(false)
let searchTimer: ReturnType<typeof setTimeout> | null = null

function displayCode(code?: string | null) {
  const c = String(code ?? '').trim()
  return c || '—'
}

function customerDisplayName(c: Customer) {
  const ext = c as Customer & { officialName?: string; name?: string; nickName?: string }
  return (
    String(
      c.customerName ||
        ext.officialName ||
        ext.name ||
        c.customerShortName ||
        ext.nickName ||
        c.customerCode ||
        ''
    ).trim() || '客户'
  )
}

function mapCustomer(c: Customer): CustomerSelectOption {
  return {
    id: String(c.id ?? '').trim(),
    name: customerDisplayName(c),
    code: String(c.customerCode ?? '').trim()
  }
}

function seedFromSelected(): CustomerSelectOption | null {
  const id = String(props.modelValue ?? '').trim()
  if (!id) return null
  const name = String(props.selectedLabel ?? '').trim() || id
  const code = String(props.selectedCode ?? '').trim()
  return { id, name, code }
}

function ensureSelectedVisible() {
  const seed = seedFromSelected()
  if (!seed) {
    if (!loading.value) options.value = []
    return
  }
  const hit = options.value.find((o) => o.id === seed.id)
  if (!hit) {
    options.value = [seed]
    return
  }
  if (props.selectedLabel?.trim()) hit.name = props.selectedLabel.trim()
  if (props.selectedCode != null) hit.code = String(props.selectedCode).trim()
}

function onFilterInput(query: string) {
  if (searchTimer) clearTimeout(searchTimer)
  const q = String(query ?? '').trim()
  if (!q) {
    ensureSelectedVisible()
    return
  }
  searchTimer = setTimeout(async () => {
    loading.value = true
    try {
      const res = await customerApi.searchCustomers({
        pageNumber: 1,
        pageSize: 30,
        searchTerm: q
      })
      const list = (res.items || []).map(mapCustomer).filter((o) => o.id)
      const seed = seedFromSelected()
      if (seed && !list.some((o) => o.id === seed.id)) {
        options.value = [seed, ...list]
      } else {
        options.value = list
      }
    } catch {
      ensureSelectedVisible()
    } finally {
      loading.value = false
    }
  }, 300)
}

function onModelUpdate(val: string | number | null | undefined) {
  emit('update:modelValue', val == null ? '' : String(val))
}

function onSelectChange(val: string | number | null | undefined) {
  const id = val == null ? '' : String(val)
  if (!id) {
    emit('change', null)
    return
  }
  const opt = options.value.find((o) => o.id === id) || seedFromSelected()
  emit('change', opt && opt.id === id ? opt : { id, name: id, code: '' })
}

function onClear() {
  options.value = []
  emit('update:modelValue', '')
  emit('change', null)
}

function onVisibleChange(open: boolean) {
  if (open) ensureSelectedVisible()
}

watch(
  () => [props.modelValue, props.selectedLabel, props.selectedCode] as const,
  () => {
    ensureSelectedVisible()
  },
  { immediate: true }
)
</script>

<style scoped lang="scss">
.entity-select-empty {
  padding: 8px 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  text-align: center;
}

.entity-select-option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
}

.entity-select-option__name {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.entity-select-option__code {
  flex-shrink: 0;
  max-width: 42%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  text-align: right;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  font-variant-numeric: tabular-nums;
}
</style>
