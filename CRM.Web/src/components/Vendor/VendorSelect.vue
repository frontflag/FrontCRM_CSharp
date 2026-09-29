<template>
  <div class="vendor-select-root">
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
            :class="{ 'is-active': previewVendorId === opt.id }"
            :title="t('vendorSelect.preview.expandTip')"
            :aria-label="t('vendorSelect.preview.expandTip')"
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
        class="vendor-select-preview"
        :style="previewStyle"
        @mousedown.stop.prevent
      >
        <header class="vendor-select-preview__head">
          <span class="vendor-select-preview__title">{{ t('vendorSelect.preview.title') }}</span>
          <button
            type="button"
            class="vendor-select-preview__close"
            :aria-label="t('vendorSelect.preview.close')"
            @click="closePreview"
          >
            ×
          </button>
        </header>
        <div v-loading="previewLoading" class="vendor-select-preview__body">
          <p v-if="previewError" class="vendor-select-preview__error">{{ previewError }}</p>
          <template v-else-if="previewVendor">
            <div
              v-for="row in previewMainRows"
              :key="row.key"
              class="vendor-select-preview__row"
            >
              <span class="vendor-select-preview__label">{{ row.label }}</span>
              <span class="vendor-select-preview__value">{{ row.value }}</span>
            </div>
            <div class="vendor-select-preview__divider" />
            <div
              v-for="row in previewMetaRows"
              :key="row.key"
              class="vendor-select-preview__row"
            >
              <span class="vendor-select-preview__label">{{ row.label }}</span>
              <span class="vendor-select-preview__value">{{ row.value }}</span>
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
import { vendorApi } from '@/api/vendor'
import type { Vendor } from '@/types/vendor'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useVendorDictStore } from '@/stores/vendorDict'
import { formatDisplayDate } from '@/utils/displayDateTime'

export type VendorSelectOption = {
  id: string
  name: string
  code: string
  level?: number
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
    selectedLevel?: number | null
    emptyHint?: string
    loadingText?: string
  }>(),
  {
    modelValue: '',
    placeholder: '请选择供应商',
    clearable: true,
    disabled: false,
    selectedLabel: '',
    selectedCode: '',
    selectedLevel: undefined,
    emptyHint: '请输入内容之后选择',
    loadingText: '搜索中...'
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'change', option: VendorSelectOption | null): void
}>()

const { t } = useI18n()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const vendorDict = useVendorDictStore()

const instanceId = `vs${Math.random().toString(36).slice(2, 9)}`
const popperClass = `vendor-select-popper vendor-select-popper--${instanceId}`

const options = ref<VendorSelectOption[]>([])
const loading = ref(false)
let searchTimer: ReturnType<typeof setTimeout> | null = null

const previewOpen = ref(false)
const previewVendorId = ref('')
const previewVendor = ref<Vendor | null>(null)
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

function dictOrDash(label: string) {
  return !label || label === '--' ? '—' : label
}

function vendorDisplayName(v: Vendor) {
  return String(v.officialName || v.nickName || v.name || v.code || '').trim() || '供应商'
}

function mapVendor(v: Vendor): VendorSelectOption {
  return {
    id: String(v.id ?? '').trim(),
    name: vendorDisplayName(v),
    code: String(v.code ?? '').trim(),
    level: typeof v.level === 'number' ? v.level : undefined
  }
}

/** 记住最近一次选中项的编码，避免父页未传 selectedCode 时回填成 — */
const lastSelectedMeta = ref<{ id: string; code: string; name: string; level?: number } | null>(
  null
)

function seedFromSelected(): VendorSelectOption | null {
  const id = String(props.modelValue ?? '').trim()
  if (!id) return null
  const cached = lastSelectedMeta.value?.id === id ? lastSelectedMeta.value : null
  const name = String(props.selectedLabel ?? '').trim() || cached?.name || id
  const code = String(props.selectedCode ?? '').trim() || cached?.code || ''
  const level =
    props.selectedLevel != null && Number.isFinite(Number(props.selectedLevel))
      ? Number(props.selectedLevel)
      : cached?.level
  return { id, name, code, level }
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
  if (props.selectedLevel != null) hit.level = Number(props.selectedLevel)
  else if (hit.level == null && seed.level != null) hit.level = seed.level
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
      const res = await vendorApi.searchVendors({
        pageNumber: 1,
        pageSize: 30,
        keyword: q
      })
      const list = (res.items || []).map(mapVendor).filter((o) => o.id)
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
  lastSelectedMeta.value = {
    id: next.id,
    code: next.code || '',
    name: next.name,
    level: next.level
  }
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
  closePreview()
}

function closePreview() {
  previewOpen.value = false
  previewVendorId.value = ''
  previewVendor.value = null
  previewError.value = ''
  previewLoading.value = false
  previewStyle.value = {}
}

function findDropdownEl(): HTMLElement | null {
  return document.querySelector(`.vendor-select-popper--${instanceId}`) as HTMLElement | null
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

async function loadPreview(vendorId: string) {
  const seq = ++previewLoadSeq
  previewLoading.value = true
  previewError.value = ''
  previewVendor.value = null
  try {
    await vendorDict.ensureLoaded()
    const v = await vendorApi.getVendorById(vendorId)
    if (seq !== previewLoadSeq) return
    previewVendor.value = v
    // 详情带回编码时，回填到当前选项行（修复父页未传 selectedCode 时显示 —）
    const code = String(v.code ?? '').trim()
    if (code) {
      const hit = options.value.find((o) => o.id === vendorId)
      if (hit && !hit.code) hit.code = code
      if (lastSelectedMeta.value?.id === vendorId) {
        lastSelectedMeta.value = { ...lastSelectedMeta.value, code }
      }
    }
  } catch {
    if (seq !== previewLoadSeq) return
    previewError.value = t('vendorSelect.preview.loadFailed')
  } finally {
    if (seq === previewLoadSeq) previewLoading.value = false
  }
}

async function togglePreview(opt: VendorSelectOption) {
  if (!opt?.id) return
  if (previewOpen.value && previewVendorId.value === opt.id) {
    closePreview()
    return
  }
  previewVendorId.value = opt.id
  previewOpen.value = true
  await nextTick()
  updatePreviewPosition()
  await loadPreview(opt.id)
  await nextTick()
  updatePreviewPosition()
}

const previewMainRows = computed((): PreviewRow[] => {
  const v = previewVendor.value
  if (!v) return []
  const mask = maskPurchaseSensitiveFields.value
  const rows: PreviewRow[] = []
  if (!mask) {
    rows.push({
      key: 'code',
      label: t('vendorSelect.preview.vendorCode'),
      value: dash(v.code)
    })
    rows.push({
      key: 'nameZh',
      label: t('vendorSelect.preview.nameZh'),
      value: dash(v.officialName || v.name)
    })
    rows.push({
      key: 'nameEn',
      label: t('vendorSelect.preview.nameEn'),
      value: dash(v.englishOfficialName)
    })
    rows.push({
      key: 'short',
      label: t('vendorSelect.preview.shortName'),
      value: dash(v.nickName)
    })
    rows.push({
      key: 'credit',
      label: t('vendorSelect.preview.creditCode'),
      value: dash(v.creditCode)
    })
    rows.push({
      key: 'duns',
      label: t('vendorSelect.preview.duns'),
      value: dash(v.duns)
    })
  }
  rows.push({
    key: 'level',
    label: t('vendorSelect.preview.level'),
    value: dictOrDash(vendorDict.levelLabel(v.level))
  })
  rows.push({
    key: 'identity',
    label: t('vendorSelect.preview.identity'),
    value: dictOrDash(vendorDict.identityLabel(v.credit))
  })
  rows.push({
    key: 'industry',
    label: t('vendorSelect.preview.industry'),
    value: dictOrDash(vendorDict.industryLabel(v.industry))
  })
  return rows
})

const previewMetaRows = computed((): PreviewRow[] => {
  const v = previewVendor.value
  if (!v) return []
  const mask = maskPurchaseSensitiveFields.value
  const created = formatDisplayDate(v.createTime || '') || '—'
  const rows: PreviewRow[] = [
    {
      key: 'created',
      label: t('vendorSelect.preview.createDate'),
      value: created
    }
  ]
  if (!mask) {
    rows.push({
      key: 'purchaser',
      label: t('vendorSelect.preview.purchaser'),
      value: dash(v.purchaseUserName || v.purchaserName)
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
  () => [props.modelValue, props.selectedLabel, props.selectedCode, props.selectedLevel] as const,
  () => {
    ensureSelectedVisible()
  },
  { immediate: true }
)
</script>

<style scoped lang="scss">
.vendor-select-root {
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
.vendor-select-preview {
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

.vendor-select-preview__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 10px 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.vendor-select-preview__title {
  font-weight: 600;
  font-size: 13px;
}

.vendor-select-preview__close {
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

.vendor-select-preview__close:hover {
  background: var(--el-fill-color-light);
  color: var(--el-text-color-primary);
}

.vendor-select-preview__body {
  min-height: 72px;
  padding: 10px 12px 12px;
}

.vendor-select-preview__error {
  margin: 0;
  font-size: 12px;
  color: var(--el-color-danger);
}

.vendor-select-preview__row {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 4px 0;
  line-height: 1.45;
}

.vendor-select-preview__label {
  flex: 0 0 108px;
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.vendor-select-preview__value {
  flex: 1;
  min-width: 0;
  word-break: break-word;
  font-size: 12px;
}

.vendor-select-preview__divider {
  height: 1px;
  margin: 8px 0;
  background: var(--el-border-color-lighter);
}

.vendor-select-popper .entity-select-option__expand {
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

.vendor-select-popper .el-select-dropdown__item:hover .entity-select-option__expand,
.vendor-select-popper .entity-select-option__expand.is-active {
  opacity: 1;
  pointer-events: auto;
}

.vendor-select-popper .entity-select-option__expand:hover,
.vendor-select-popper .entity-select-option__expand.is-active {
  background: rgba(64, 158, 255, 0.12);
}

.vendor-select-popper .entity-select-option {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.vendor-select-popper .entity-select-option__name {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.vendor-select-popper .entity-select-option__code {
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
