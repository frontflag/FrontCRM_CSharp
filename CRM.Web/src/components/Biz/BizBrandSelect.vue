<template>
  <div class="biz-brand-select">
    <el-select
      ref="selectRef"
      :model-value="modelValue ?? undefined"
      :placeholder="placeholder"
      :clearable="clearable"
      :disabled="disabled"
      :size="size"
      filterable
      remote
      reserve-keyword
      :remote-method="onRemoteSearch"
      :loading="loading"
      style="width: 100%"
      class="biz-brand-select__control"
      @update:model-value="onModelUpdate"
      @change="onSelectChange"
      @visible-change="onVisibleChange"
      @clear="onClear"
    >
      <el-option
        v-for="opt in displayOptions"
        :key="opt.id"
        :label="optionLabel(opt)"
        :value="opt.id"
      >
        <div class="biz-brand-select__option">
          <span class="biz-brand-select__name">{{ opt.standardBrand || optionLabel(opt) }}</span>
          <span v-if="isPending(opt)" class="biz-brand-select__pending">{{ t('bizBrand.auditStatusPending') }}</span>
        </div>
      </el-option>
    </el-select>
    <button
      v-if="showCreateButton"
      type="button"
      class="biz-brand-select__create-btn"
      :disabled="disabled"
      @click="openCreateDialog"
    >
      {{ t('bizBrand.create') }}
    </button>
    <BizBrandCreateDialog
      v-if="!delegateCreateDialog"
      v-model="createDialogVisible"
      mode="add"
      @created="onBrandCreated"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { ElSelect } from 'element-plus'
import { bizBrandApi, bizBrandRowToOption, type BizBrandOption, type BizBrandRow } from '@/api/bizBrand'
import BizBrandCreateDialog from '@/components/Biz/BizBrandCreateDialog.vue'

const props = withDefaults(
  defineProps<{
    modelValue?: number | null
    placeholder?: string
    clearable?: boolean
    disabled?: boolean
    size?: 'default' | 'small' | 'large'
    showCreateButton?: boolean
    /** 列表表格等场景：由父级挂载唯一弹窗，避免单元格卸载导致闪烁 */
    delegateCreateDialog?: boolean
  }>(),
  {
    modelValue: undefined,
    placeholder: '请选择品牌',
    clearable: true,
    disabled: false,
    size: 'default',
    showCreateButton: true,
    delegateCreateDialog: false
  }
)

const emit = defineEmits<{
  'update:modelValue': [value: number | undefined]
  change: [payload: { id: number; standardBrand: string; auditStatus?: number | null }]
  'request-create': []
}>()

const { t } = useI18n()
const selectRef = ref<InstanceType<typeof ElSelect> | null>(null)
const loading = ref(false)
const options = ref<BizBrandOption[]>([])
const selectedOption = ref<BizBrandOption | null>(null)
const createDialogVisible = ref(false)
const lastKeyword = ref('')
let searchTimer: ReturnType<typeof setTimeout> | null = null
let searchSeq = 0

const displayOptions = computed(() => {
  const list = options.value.filter((o) => o.id > 0)
  const sel = selectedOption.value
  if (sel && sel.id > 0 && !list.some((o) => o.id === sel.id)) {
    list.unshift(sel)
  }
  return list
})

function isPending(opt: BizBrandOption) {
  return Number(opt.auditStatus) === 1
}

function optionLabel(opt: BizBrandOption): string {
  const std = (opt.standardBrand || '').trim()
  if (std) return std
  const en = (opt.brandEName || '').trim()
  const cn = (opt.brandCName || '').trim()
  return en || cn || (opt.id > 0 ? String(opt.id) : '')
}

function onModelUpdate(val: number | undefined) {
  if (val == null || val <= 0) {
    emit('update:modelValue', undefined)
    selectedOption.value = null
    return
  }
  emit('update:modelValue', val)
}

function onSelectChange(val: number | undefined) {
  if (val == null || val <= 0) {
    emit('change', { id: 0, standardBrand: '' })
    return
  }
  const row = displayOptions.value.find((o) => o.id === val)
  if (row) {
    selectedOption.value = row
    lastKeyword.value = optionLabel(row)
    emit('change', {
      id: row.id,
      standardBrand: (row.standardBrand || optionLabel(row)).trim(),
      auditStatus: row.auditStatus
    })
  }
}

function onClear() {
  lastKeyword.value = ''
  selectedOption.value = null
  options.value = []
  emit('update:modelValue', undefined)
  emit('change', { id: 0, standardBrand: '' })
}

function onVisibleChange(open: boolean) {
  if (!open) return
  void searchOptions(lastKeyword.value)
}

function onRemoteSearch(query: string) {
  lastKeyword.value = query
  const selectedLabel = selectedOption.value ? optionLabel(selectedOption.value) : ''
  // 已有选中项时继续输入，视为重新搜索而非编辑已选文案
  if (
    props.modelValue &&
    selectedLabel &&
    query.trim() &&
    query.trim() !== selectedLabel.trim()
  ) {
    selectedOption.value = null
    emit('update:modelValue', undefined)
  }
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(() => void searchOptions(query), 200)
}

async function searchOptions(keyword: string) {
  const seq = ++searchSeq
  loading.value = true
  try {
    const items = await bizBrandApi.fetchOptions({ keyword: keyword.trim(), pageSize: 50 })
    if (seq !== searchSeq) return
    options.value = items.filter((o) => o.id > 0)
  } catch {
    if (seq !== searchSeq) return
    options.value = []
  } finally {
    if (seq === searchSeq) loading.value = false
  }
}

async function ensureSelectedLoaded(id: number) {
  if (!id || id <= 0) {
    selectedOption.value = null
    return
  }
  const cached = options.value.find((o) => o.id === id) ?? selectedOption.value
  if (cached?.id === id) {
    selectedOption.value = cached
    lastKeyword.value = optionLabel(cached)
    return
  }
  try {
    const row = await bizBrandApi.getById(id)
    const opt = bizBrandRowToOption(row)
    selectedOption.value = opt
    lastKeyword.value = optionLabel(opt)
  } catch {
    selectedOption.value = { id, standardBrand: String(id) }
    lastKeyword.value = String(id)
  }
}

function openCreateDialog() {
  if (props.delegateCreateDialog) {
    emit('request-create')
    return
  }
  createDialogVisible.value = true
}

async function onBrandCreated(row: BizBrandRow) {
  if (!row.id || row.id <= 0) return
  const opt = bizBrandRowToOption(row)
  selectedOption.value = opt
  lastKeyword.value = optionLabel(opt)
  options.value = [opt]
  emit('update:modelValue', row.id)
  emit('change', {
    id: row.id,
    standardBrand: (row.standardBrand || optionLabel(opt)).trim(),
    auditStatus: row.auditStatus
  })
  await nextTick()
  const select = selectRef.value
  if (select && typeof (select as { blur?: () => void }).blur === 'function') {
    ;(select as { blur: () => void }).blur()
  }
}

watch(
  () => props.modelValue,
  (id) => {
    if (id && id > 0) void ensureSelectedLoaded(id)
    else if (!createDialogVisible.value) {
      selectedOption.value = null
      lastKeyword.value = ''
    }
  },
  { immediate: true }
)

onMounted(() => {
  void searchOptions('')
})
</script>

<style scoped>
.biz-brand-select {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.biz-brand-select__control {
  flex: 1;
  min-width: 0;
}

.biz-brand-select__create-btn {
  flex-shrink: 0;
  padding: 0;
  border: none;
  background: none;
  color: var(--el-color-primary);
  font-size: 13px;
  cursor: pointer;
  white-space: nowrap;
}

.biz-brand-select__create-btn:disabled {
  color: var(--el-text-color-disabled);
  cursor: not-allowed;
}

.biz-brand-select__option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.biz-brand-select__name {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}

.biz-brand-select__pending {
  flex-shrink: 0;
  padding: 0 6px;
  font-size: 12px;
  line-height: 20px;
  border-radius: 4px;
  background: #fef9c3;
  color: #a16207;
}
</style>
