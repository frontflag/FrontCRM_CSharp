<template>
  <div class="customer-select-root">
    <el-select
      :model-value="modelValue || undefined"
      :placeholder="placeholder"
      :clearable="clearable"
      :disabled="disabled"
      :filterable="true"
      :filter-method="onFilterInput"
      :loading="loading"
      :loading-text="loadingText"
      :popper-class="popperClass"
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
          <button
            type="button"
            class="entity-select-option__expand"
            :class="{ 'is-active': previewCustomerId === opt.id }"
            :title="t('customerSelect.preview.expandTip')"
            :aria-label="t('customerSelect.preview.expandTip')"
            @mousedown.stop.prevent
            @click.stop.prevent="togglePreview(opt)"
          >
            <el-icon :size="14"><DArrowRight /></el-icon>
          </button>
        </div>
      </el-option>
    </el-select>

    <Teleport to="body">
      <div
        v-if="previewOpen"
        ref="previewRef"
        class="customer-select-preview"
        :style="previewStyle"
        @mousedown.stop.prevent
      >
        <header class="customer-select-preview__head">
          <span class="customer-select-preview__title">{{ t('customerSelect.preview.title') }}</span>
          <button
            type="button"
            class="customer-select-preview__close"
            :aria-label="t('customerSelect.preview.close')"
            @click="closePreview"
          >
            ×
          </button>
        </header>
        <div v-loading="previewLoading" class="customer-select-preview__body">
          <p v-if="previewError" class="customer-select-preview__error">{{ previewError }}</p>
          <template v-else-if="previewCustomer">
            <div
              v-for="row in previewMainRows"
              :key="row.key"
              class="customer-select-preview__row"
            >
              <span class="customer-select-preview__label">{{ row.label }}</span>
              <span class="customer-select-preview__value">{{ row.value }}</span>
            </div>
            <div class="customer-select-preview__divider" />
            <div
              v-for="row in previewMetaRows"
              :key="row.key"
              class="customer-select-preview__row"
            >
              <span class="customer-select-preview__label">{{ row.label }}</span>
              <span class="customer-select-preview__value">{{ row.value }}</span>
            </div>
          </template>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { DArrowRight } from '@element-plus/icons-vue'
import { customerApi } from '@/api/customer'
import type { Customer } from '@/types/customer'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useCustomerDictStore } from '@/stores/customerDict'
import { formatDisplayDate } from '@/utils/displayDateTime'

export type CustomerSelectOption = {
  id: string
  name: string
  code: string
}

type PreviewRow = { key: string; label: string; value: string }

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

const { t } = useI18n()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const customerDict = useCustomerDictStore()

const instanceId = `cs${Math.random().toString(36).slice(2, 9)}`
const popperClass = `customer-select-popper customer-select-popper--${instanceId}`

const options = ref<CustomerSelectOption[]>([])
const loading = ref(false)
let searchTimer: ReturnType<typeof setTimeout> | null = null

const previewOpen = ref(false)
const previewCustomerId = ref('')
const previewCustomer = ref<Customer | null>(null)
const previewLoading = ref(false)
const previewError = ref('')
const previewRef = ref<HTMLElement | null>(null)
const previewStyle = ref<Record<string, string>>({})
let previewLoadSeq = 0

function displayCode(code?: string | null) {
  const c = String(code ?? '').trim()
  return c || '—'
}

function dash(v?: string | null) {
  const s = String(v ?? '').trim()
  return s || '—'
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

/** 记住最近一次选中项的编码，避免父页未传 selectedCode 时回填成 — */
const lastSelectedMeta = ref<{ id: string; code: string; name: string } | null>(null)

function seedFromSelected(): CustomerSelectOption | null {
  const id = String(props.modelValue ?? '').trim()
  if (!id) return null
  const cached = lastSelectedMeta.value?.id === id ? lastSelectedMeta.value : null
  const name = String(props.selectedLabel ?? '').trim() || cached?.name || id
  const code = String(props.selectedCode ?? '').trim() || cached?.code || ''
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
  // 仅在有有效回填值时覆盖，避免空 selectedCode 把搜索结果里的编码抹成 —
  if (props.selectedLabel?.trim()) hit.name = props.selectedLabel.trim()
  if (props.selectedCode?.trim()) hit.code = props.selectedCode.trim()
  else if (!hit.code && seed.code) hit.code = seed.code
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
    lastSelectedMeta.value = null
    emit('change', null)
    return
  }
  const opt = options.value.find((o) => o.id === id) || seedFromSelected()
  const next = opt && opt.id === id ? opt : { id, name: id, code: '' }
  lastSelectedMeta.value = { id: next.id, code: next.code || '', name: next.name }
  emit('change', next)
}

function onClear() {
  options.value = []
  lastSelectedMeta.value = null
  closePreview()
  emit('update:modelValue', '')
  emit('change', null)
}

function onVisibleChange(open: boolean) {
  if (open) {
    ensureSelectedVisible()
    return
  }
  // 下拉关闭时同步关闭预览
  closePreview()
}

function closePreview() {
  previewOpen.value = false
  previewCustomerId.value = ''
  previewCustomer.value = null
  previewError.value = ''
  previewLoading.value = false
  previewStyle.value = {}
}

function findDropdownEl(): HTMLElement | null {
  return document.querySelector(`.customer-select-popper--${instanceId}`) as HTMLElement | null
}

function updatePreviewPosition() {
  const dropdown = findDropdownEl()
  if (!dropdown) return
  const rect = dropdown.getBoundingClientRect()
  const panelW = 320
  const gap = 8
  let left = rect.right + gap
  if (left + panelW > window.innerWidth - 8) {
    left = Math.max(8, rect.left - panelW - gap)
  }
  const top = Math.min(Math.max(8, rect.top), Math.max(8, window.innerHeight - 360))
  previewStyle.value = {
    position: 'fixed',
    top: `${top}px`,
    left: `${left}px`,
    width: `${panelW}px`,
    zIndex: '4100'
  }
}

async function loadPreview(customerId: string) {
  const seq = ++previewLoadSeq
  previewLoading.value = true
  previewError.value = ''
  previewCustomer.value = null
  try {
    await customerDict.ensureLoaded()
    const c = await customerApi.getCustomerById(customerId)
    if (seq !== previewLoadSeq) return
    previewCustomer.value = c
    const code = String(c.customerCode ?? '').trim()
    if (code) {
      const hit = options.value.find((o) => o.id === customerId)
      if (hit && !hit.code) hit.code = code
      if (lastSelectedMeta.value?.id === customerId) {
        lastSelectedMeta.value = { ...lastSelectedMeta.value, code }
      }
    }
  } catch {
    if (seq !== previewLoadSeq) return
    previewError.value = t('customerSelect.preview.loadFailed')
  } finally {
    if (seq === previewLoadSeq) previewLoading.value = false
  }
}

async function togglePreview(opt: CustomerSelectOption) {
  if (!opt?.id) return
  if (previewOpen.value && previewCustomerId.value === opt.id) {
    closePreview()
    return
  }
  previewCustomerId.value = opt.id
  previewOpen.value = true
  await nextTick()
  updatePreviewPosition()
  await loadPreview(opt.id)
  await nextTick()
  updatePreviewPosition()
}

function textOrDash(v?: string | null) {
  return dash(v)
}

const previewMainRows = computed((): PreviewRow[] => {
  const c = previewCustomer.value
  if (!c) return []
  const mask = maskSaleSensitiveFields.value
  const rows: PreviewRow[] = []
  if (!mask) {
    rows.push({
      key: 'code',
      label: t('customerSelect.preview.customerCode'),
      value: textOrDash(c.customerCode)
    })
    rows.push({
      key: 'nameZh',
      label: t('customerSelect.preview.nameZh'),
      value: textOrDash(c.customerName)
    })
    rows.push({
      key: 'nameEn',
      label: t('customerSelect.preview.nameEn'),
      value: textOrDash(c.englishOfficialName)
    })
    rows.push({
      key: 'short',
      label: t('customerSelect.preview.shortName'),
      value: textOrDash(c.customerShortName)
    })
    rows.push({
      key: 'credit',
      label: t('customerSelect.preview.creditCode'),
      value: textOrDash(c.unifiedSocialCreditCode)
    })
    const duns = String(
      (c as Customer & { dUNS?: string; DUNS?: string }).duns ??
        (c as Customer & { dUNS?: string }).dUNS ??
        ''
    ).trim()
    rows.push({
      key: 'duns',
      label: t('customerSelect.preview.duns'),
      value: textOrDash(duns)
    })
  }
  const typeLabel = customerDict.typeLabel(c.customerType ?? 0)
  rows.push({
    key: 'type',
    label: t('customerSelect.preview.customerType'),
    value: !typeLabel || typeLabel === '--' ? '—' : typeLabel
  })
  const industryLabel = customerDict.industryLabel(c.industry)
  rows.push({
    key: 'industry',
    label: t('customerSelect.preview.industry'),
    value: !industryLabel || industryLabel === '--' ? '—' : industryLabel
  })
  return rows
})

const previewMetaRows = computed((): PreviewRow[] => {
  const c = previewCustomer.value
  if (!c) return []
  const mask = maskSaleSensitiveFields.value
  const created = formatDisplayDate(c.createTime || c.createdAt || '') || '—'
  const rows: PreviewRow[] = [
    {
      key: 'created',
      label: t('customerSelect.preview.createDate'),
      value: created
    }
  ]
  if (!mask) {
    rows.push({
      key: 'sales',
      label: t('customerSelect.preview.salesPerson'),
      value: textOrDash(c.salesPersonName)
    })
  }
  return rows
})

function onWinReposition() {
  if (previewOpen.value) updatePreviewPosition()
}

onMounted(() => {
  window.addEventListener('resize', onWinReposition)
  window.addEventListener('scroll', onWinReposition, true)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', onWinReposition)
  window.removeEventListener('scroll', onWinReposition, true)
  if (searchTimer) clearTimeout(searchTimer)
})

watch(
  () => [props.modelValue, props.selectedLabel, props.selectedCode] as const,
  () => {
    ensureSelectedVisible()
  },
  { immediate: true }
)
</script>

<style scoped lang="scss">
.customer-select-root {
  width: 100%;
}

.entity-select-empty {
  padding: 8px 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  text-align: center;
}

.entity-select-option {
  display: flex;
  align-items: center;
  gap: 8px;
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
  max-width: 36%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  text-align: right;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  font-variant-numeric: tabular-nums;
}

.entity-select-option__expand {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  margin: 0;
  padding: 0;
  border: none;
  border-radius: 4px;
  background: transparent;
  color: var(--el-color-primary);
  cursor: pointer;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.12s ease, background 0.12s ease;
}

.entity-select-option:hover .entity-select-option__expand,
.entity-select-option__expand.is-active {
  opacity: 1;
  pointer-events: auto;
}

.entity-select-option__expand:hover,
.entity-select-option__expand.is-active {
  background: rgba(64, 158, 255, 0.12);
}
</style>

<!-- 预览浮层 Teleport 到 body，需非 scoped 样式；用唯一根类隔离 -->
<style lang="scss">
.customer-select-preview {
  box-sizing: border-box;
  max-height: min(420px, calc(100vh - 24px));
  overflow: auto;
  padding: 0;
  border: 1px solid var(--el-border-color-light);
  border-radius: 10px;
  background: var(--el-bg-color-overlay, #fff);
  box-shadow: var(--el-box-shadow-light);
  font-size: 13px;
  color: var(--el-text-color-primary);
}

.customer-select-preview__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 10px 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.customer-select-preview__title {
  font-weight: 600;
  font-size: 13px;
}

.customer-select-preview__close {
  width: 24px;
  height: 24px;
  margin: 0;
  padding: 0;
  border: none;
  border-radius: 4px;
  background: transparent;
  color: var(--el-text-color-secondary);
  font-size: 18px;
  line-height: 1;
  cursor: pointer;
}

.customer-select-preview__close:hover {
  background: var(--el-fill-color-light);
  color: var(--el-text-color-primary);
}

.customer-select-preview__body {
  min-height: 72px;
  padding: 10px 12px 12px;
}

.customer-select-preview__error {
  margin: 0;
  font-size: 12px;
  color: var(--el-color-danger);
}

.customer-select-preview__row {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 4px 0;
  line-height: 1.45;
}

.customer-select-preview__label {
  flex: 0 0 108px;
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.customer-select-preview__value {
  flex: 1;
  min-width: 0;
  word-break: break-word;
  font-size: 12px;
}

.customer-select-preview__divider {
  height: 1px;
  margin: 8px 0;
  background: var(--el-border-color-lighter);
}

/* 下拉选项行悬停时露出展开按钮（选项在 popper 内，不受 scoped 影响） */
.customer-select-popper .entity-select-option__expand {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  margin: 0;
  padding: 0;
  border: none;
  border-radius: 4px;
  background: transparent;
  color: var(--el-color-primary);
  cursor: pointer;
  opacity: 0;
  pointer-events: none;
}

.customer-select-popper .el-select-dropdown__item:hover .entity-select-option__expand,
.customer-select-popper .entity-select-option__expand.is-active {
  opacity: 1;
  pointer-events: auto;
}

.customer-select-popper .entity-select-option__expand:hover,
.customer-select-popper .entity-select-option__expand.is-active {
  background: rgba(64, 158, 255, 0.12);
}

.customer-select-popper .entity-select-option {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.customer-select-popper .entity-select-option__name {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.customer-select-popper .entity-select-option__code {
  flex-shrink: 0;
  max-width: 36%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  text-align: right;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  font-variant-numeric: tabular-nums;
}
</style>
