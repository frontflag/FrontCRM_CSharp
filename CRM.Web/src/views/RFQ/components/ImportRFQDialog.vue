<template>
  <el-dialog
    ref="mainDialogRef"
    v-model="visible"
    :title="stepTitle"
    width="960px"
    :close-on-click-modal="false"
    :before-close="handleClose"
    class="import-rfq-dialog"
    destroy-on-close
    @opened="onMainDialogOpened"
    @closed="onMainDialogClosed"
  >
    <!-- ── STEP 1：上传 Excel ── -->
    <div v-if="step === 1" class="step-upload import-rfq-step-pane">
      <div class="upload-tips">
        <el-alert type="info" :closable="false" show-icon>
          <template #title>
            <span>
              {{ t('rfqExcelImport.uploadHintPhase2') }}
              <el-link type="primary" :underline="false" @click="downloadTemplate" style="margin-left:8px;">
                <el-icon><Download /></el-icon> {{ t('rfqExcelImport.downloadTemplate') }}
              </el-link>
              <el-link type="primary" :underline="false" @click="fieldDescVisible = true" style="margin-left:12px;">
                {{ t('rfqExcelImport.fieldDescButton') }}
              </el-link>
            </span>
          </template>
        </el-alert>
      </div>

      <el-upload
        class="excel-upload-area"
        drag
        :auto-upload="false"
        :show-file-list="false"
        accept=".xlsx,.xls"
        :on-change="handleFileChange"
      >
        <el-icon class="el-icon--upload"><UploadFilled /></el-icon>
        <div class="el-upload__text">
          {{ t('rfqExcelImport.dragOrClick') }}
        </div>
        <template #tip>
          <div class="el-upload__tip">{{ t('rfqExcelImport.fileTip') }}</div>
        </template>
      </el-upload>

      <div v-if="uploadedFileName" class="uploaded-file-info">
        <el-icon color="#67c23a"><CircleCheckFilled /></el-icon>
        <span>{{ t('rfqExcelImport.selectedFile', { name: uploadedFileName }) }}</span>
        <el-button link type="primary" @click="clearFile">{{ t('rfqExcelImport.reselect') }}</el-button>
      </div>

      <div v-if="rawRows.length" class="import-options">
        <div class="import-options__row">
          <div v-if="sheetNames.length > 1" class="import-options__item">
            <span class="import-options__label">{{ t('rfqExcelImport.sheetLabel') }}</span>
            <el-select
              v-model="selectedSheetIndex"
              size="small"
              class="import-options__sheet"
              @change="onSheetIndexChange"
            >
              <el-option
                v-for="(name, idx) in sheetNames"
                :key="`${idx}-${name}`"
                :label="formatSheetOptionLabel(name, idx)"
                :value="idx"
              />
            </el-select>
          </div>
          <div class="import-options__item">
            <span class="import-options__label">{{ t('rfqExcelImport.headerRowLabel') }}</span>
            <el-input-number
              v-model="headerRowNumber"
              :min="1"
              :max="headerRowMax"
              size="small"
              controls-position="right"
              @change="onHeaderRowNumberChange"
            />
          </div>
        </div>
        <div class="import-options__hints">
          <span v-if="sheetNames.length > 1">{{ t('rfqExcelImport.sheetHint') }}</span>
          <span>{{ t('rfqExcelImport.headerRowHint') }}</span>
        </div>
      </div>

      <div v-if="headerPreviewRows.length" class="header-preview-table">
        <div class="mapping-title">{{ t('rfqExcelImport.headerPreview') }}</div>
        <el-table :data="headerPreviewRows" size="small" border>
          <el-table-column prop="rowLabel" :label="t('rfqExcelImport.previewRow')" width="72" align="center" />
          <el-table-column
            v-for="col in headerPreviewCols"
            :key="col.index"
            :label="col.letter"
            min-width="100"
            show-overflow-tooltip
          >
            <template #default="{ row }">
              {{ row.cells[col.index] || '—' }}
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>

    <el-dialog
      ref="fieldDescDialogRef"
      v-model="fieldDescVisible"
      :title="t('rfqExcelImport.fieldDescTitle')"
      width="720px"
      append-to-body
      destroy-on-close
      class="import-rfq-field-desc-dialog"
      @opened="onFieldDescDialogOpened"
      @closed="onFieldDescDialogClosed"
    >
      <div class="field-desc-hint">{{ t('rfqExcelImport.supportedFields') }}</div>
      <el-table :data="supportedFields" size="small" border max-height="420">
        <el-table-column prop="label" :label="t('rfqExcelImport.colField')" width="160" />
        <el-table-column prop="required" :label="t('rfqExcelImport.colRequired')" width="70" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.required" type="danger" size="small">{{ t('rfqExcelImport.required') }}</el-tag>
            <span v-else style="color:#909399">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="example" :label="t('rfqExcelImport.colExample')" width="140" />
        <el-table-column prop="note" :label="t('rfqExcelImport.colNote')" />
      </el-table>
      <template #footer>
        <el-button type="primary" @click="fieldDescVisible = false">{{ t('common.confirm') }}</el-button>
      </template>
    </el-dialog>

    <!-- ── STEP 2：列映射与解析统计 ── -->
    <div v-if="step === 2" class="step-mapping import-rfq-step-pane">
      <div class="mapping-toolbar">
        <el-button size="small" :disabled="aiMappingLoading" @click="applyRuleMappings">
          {{ t('rfqExcelImport.reapplyRuleMapping') }}
        </el-button>
        <el-button size="small" :loading="aiMappingLoading" @click="invokeAiColumnMapping(false)">
          {{ t('rfqExcelImport.aiMapColumns') }}
        </el-button>
        <el-button size="small" :loading="aiBrandLoading" :disabled="!pendingBrandCount" @click="invokeAiBrandMatching">
          {{ t('rfqExcelImport.aiMapBrands') }}
        </el-button>
        <span class="mapping-toolbar__meta">
          <span v-if="sheetNames.length > 1 && currentSheetName">{{ t('rfqExcelImport.sheetSelected', { name: currentSheetName }) }}</span>
          <span>{{ t('rfqExcelImport.headerRowSelected', { row: headerRowIndex + 1 }) }}</span>
        </span>
      </div>

      <div class="parse-stats">
        <el-tag type="success">{{ t('rfqExcelImport.statsSuccess', { count: validItems.length }) }}</el-tag>
        <el-tag v-if="brandLearnedRuleMatchedCount" type="success" style="margin-left:8px;">
          {{ t('rfqExcelImport.statsBrandLearnedRuleMatched', { count: brandLearnedRuleMatchedCount }) }}
        </el-tag>
        <el-tag v-if="brandAiMatchedCount" type="warning" style="margin-left:8px;">
          {{ t('rfqExcelImport.statsBrandAiMatched', { count: brandAiMatchedCount }) }}
        </el-tag>
        <el-tag v-if="pendingBrandCount" type="warning" style="margin-left:8px;">
          {{ t('rfqExcelImport.statsBrandPending', { count: pendingBrandCount }) }}
        </el-tag>
        <el-tag v-if="errorItems.length" type="danger" style="margin-left:8px;">
          {{ t('rfqExcelImport.statsError', { count: errorItems.length }) }}
        </el-tag>
        <el-tag v-if="skippedRows > 0" type="info" style="margin-left:8px;">
          {{ t('rfqExcelImport.statsSkipped', { count: skippedRows }) }}
        </el-tag>
      </div>

      <el-alert
        v-if="!hasRequiredColumns"
        type="error"
        :closable="false"
        style="margin-bottom:10px;"
        :title="t('rfqExcelImport.missingRequiredColumns', { fields: missingRequiredFields.join('、') })"
      />

      <el-alert
        v-else-if="errorItems.length"
        type="warning"
        :closable="false"
        style="margin-bottom:10px;"
        :title="t('rfqExcelImport.errorRowsHint')"
      />

      <div class="mapping-result-table">
        <div class="mapping-title">{{ t('rfqExcelImport.detectedMappingEditable') }}</div>
        <el-table
          v-loading="brandMatchingLoading || aiMappingLoading || aiBrandLoading"
          :data="columnMappings"
          size="small"
          border
        >
          <el-table-column prop="colLetter" :label="t('rfqExcelImport.colExcel')" width="72" align="center" />
          <el-table-column prop="headerText" :label="t('rfqExcelImport.colHeader')" min-width="140" show-overflow-tooltip />
          <el-table-column :label="t('rfqExcelImport.colMappedField')" min-width="200">
            <template #default="{ row }">
              <el-select
                :model-value="row.fieldKey"
                size="small"
                clearable
                :placeholder="t('rfqExcelImport.unmapped')"
                class="mapping-field-select"
                @update:model-value="(v: RfqExcelItemFieldKey | null | undefined) => onMappingFieldChange(row, v ?? null)"
              >
                <el-option
                  v-for="opt in fieldSelectOptions"
                  :key="String(opt.value)"
                  :label="opt.label"
                  :value="opt.value"
                />
              </el-select>
            </template>
          </el-table-column>
          <el-table-column :label="t('rfqExcelImport.colMappingSource')" width="88" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.matched" size="small" :type="mappingSourceTagType(row.mappingSource)">
                {{ mappingSourceLabel(row.mappingSource, t) }}
              </el-tag>
              <span v-else style="color:#909399">-</span>
            </template>
          </el-table-column>
          <el-table-column prop="required" :label="t('rfqExcelImport.colRequired')" width="70" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.required" type="danger" size="small">{{ t('rfqExcelImport.required') }}</el-tag>
              <span v-else style="color:#909399">-</span>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="handleClose">{{ t('common.cancel') }}</el-button>
        <el-button v-if="step === 2" @click="step = 1">{{ t('rfqExcelImport.prevStep') }}</el-button>
        <el-button
          v-if="step === 1"
          type="primary"
          :loading="parsingFile"
          :disabled="!uploadedFileName"
          @click="goToMapping"
        >
          {{ t('rfqExcelImport.nextMapping') }}
        </el-button>
        <el-button
          v-if="step === 2"
          type="primary"
          :loading="submitting"
          :disabled="validItems.length === 0 || brandMatchingLoading || aiMappingLoading || !hasRequiredColumns"
          @click="handleConfirmParse"
        >
          {{ t('rfqExcelImport.confirmParse', { count: validItems.length }) }}
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, nextTick, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { UploadFilled, Download, CircleCheckFilled } from '@element-plus/icons-vue'
import * as XLSX from 'xlsx'
import {
  brandMatchStatusLabel,
  buildBrandMatchCache,
  fetchBrandOptionsForKeyword,
  normalizeBrandSourceKey,
  pickBizBrandMatch,
  resolveBrandMatchKeyword,
  type BrandMappingSource,
  type BrandMatchStatus
} from '@/utils/bizBrandMatch'
import {
  emptyParsedRfq,
  type ParsedRfqFields,
  type ParsedRfqItemFields
} from '@/utils/entityParseSchema'
import {
  RFQ_EXCEL_FIELD_METAS,
  RFQ_EXCEL_MAX_DATA_ROWS,
  RFQ_EXCEL_MAX_HEADER_ROW_OPTIONS,
  buildAiColumnMapInput,
  buildRuleColumnMappings,
  filterColumnMappingsWithData,
  countNonEmptyDataRows,
  fieldMetaLabel,
  mappingSourceLabel,
  mergeRuleAndAiMappings,
  parseAiColumnMapResponse,
  parseRfqExcelRows,
  resolveHeaderFieldKey,
  type RfqExcelColumnMappingRow,
  type RfqExcelAiColumnMapResult,
  type RfqExcelItemFieldKey,
  type RfqExcelMappingSource,
  type RfqExcelParseRowResult
} from '@/utils/rfqExcelColumnMap'
import {
  aiApi,
  AI_SCENARIO_ENTITY_PARSE_RFQ_EXCEL_BRAND_MAP,
  AI_SCENARIO_ENTITY_PARSE_RFQ_EXCEL_COLUMN_MAP
} from '@/api/ai'
import { getApiErrorMessage } from '@/utils/apiError'
import { DEFAULT_SETTLEMENT_CURRENCY_STRING } from '@/constants/currency'
import { useResizableDialog } from '@/composables/useResizableDialog'
import { loadRfqExcelWorkbook, type RfqExcelWorkbookCache } from '@/utils/rfqExcelWorkbook'
import { buildAiBrandMapInput, parseAiBrandMapResponse } from '@/utils/rfqExcelBrandMap'

type ElDialogExpose = {
  dialogContentRef?: {
    $el: HTMLElement
  }
}

const mainDialogRef = ref<ElDialogExpose | null>(null)
const fieldDescDialogRef = ref<ElDialogExpose | null>(null)

const { enableResizableDialogWithRetry: enableMainResizable, disableResizableDialog: disableMainResizable, fitDialogToContentWithRetry: fitMainDialogToContent } =
  useResizableDialog({
    resolveDialogEl: () => mainDialogRef.value?.dialogContentRef?.$el ?? null,
    minWidth: 720,
    minHeight: 420
  })

const { enableResizableDialogWithRetry: enableFieldDescResizable, disableResizableDialog: disableFieldDescResizable } =
  useResizableDialog({
    resolveDialogEl: () => fieldDescDialogRef.value?.dialogContentRef?.$el ?? null,
    minWidth: 520,
    minHeight: 320
  })

async function onMainDialogOpened() {
  await nextTick()
  enableMainResizable()
  await scheduleFitMainDialog()
}

function onMainDialogClosed() {
  disableMainResizable()
}

async function onFieldDescDialogOpened() {
  await nextTick()
  enableFieldDescResizable()
}

function onFieldDescDialogClosed() {
  disableFieldDescResizable()
}

const emit = defineEmits<{
  parsed: [data: ParsedRfqFields]
}>()

const { t } = useI18n()

type ParsedItem = ParsedRfqItemFields & {
  _error?: string
  _brandMatchStatus?: BrandMatchStatus
  _brandMatchLabel?: string
  _importBrandText?: string
  _brandMappingSource?: BrandMappingSource
}

const visible = defineModel<boolean>({ default: false })
const fieldDescVisible = ref(false)
const step = ref(1)

const stepTitle = computed(() =>
  step.value === 1 ? t('rfqExcelImport.step1Title') : t('rfqExcelImport.step2Title')
)

const supportedFields = RFQ_EXCEL_FIELD_METAS.map((m) => ({
  label: m.label,
  required: m.required,
  example: m.example,
  note: m.note
}))

const fieldSelectOptions = computed(() => [
  { value: null as RfqExcelItemFieldKey | null, label: t('rfqExcelImport.unmapped') },
  ...RFQ_EXCEL_FIELD_METAS.map((m) => ({ value: m.key as RfqExcelItemFieldKey, label: m.label }))
])

const uploadedFileName = ref('')
const rawFile = ref<File | null>(null)
const sheetNames = ref<string[]>([])
const selectedSheetIndex = ref(0)
let workbookCache: RfqExcelWorkbookCache | null = null
const rawRows = ref<unknown[][]>([])
const headerRowIndex = ref(0)
const headerRowNumber = ref(1)
const headerRowMax = computed(() =>
  Math.min(RFQ_EXCEL_MAX_HEADER_ROW_OPTIONS, Math.max(1, rawRows.value.length))
)

const currentSheetName = computed(() => sheetNames.value[selectedSheetIndex.value] ?? '')

function formatSheetOptionLabel(name: string, index: number) {
  return `${index + 1}. ${name}`
}

function resetSheetState() {
  workbookCache = null
  sheetNames.value = []
  selectedSheetIndex.value = 0
}

function applyRowsFromSelectedSheet() {
  if (!workbookCache) {
    rawRows.value = []
    return
  }
  rawRows.value = workbookCache.readSheetRows(selectedSheetIndex.value)
  if (headerRowIndex.value >= rawRows.value.length) {
    headerRowIndex.value = 0
    headerRowNumber.value = 1
  }
}

const headerPreviewRows = computed(() => {
  const limit = Math.min(rawRows.value.length, RFQ_EXCEL_MAX_HEADER_ROW_OPTIONS)
  return Array.from({ length: limit }, (_, i) => {
    const row = rawRows.value[i] ?? []
    const cells = row.map((c) => String(c ?? '').trim())
    return {
      rowIndex: i,
      rowLabel: String(i + 1),
      isHeader: i === headerRowIndex.value,
      cells
    }
  })
})

const headerPreviewCols = computed(() => {
  const header = rawRows.value[headerRowIndex.value] ?? []
  const colCount = Math.min(
    8,
    Math.max(header.length, ...headerPreviewRows.value.map((r) => r.cells.length), 1)
  )
  return Array.from({ length: colCount }, (_, index) => ({
    index,
    letter: String.fromCharCode(65 + (index % 26))
  }))
})

const parsedRows = ref<ParsedItem[]>([])
const columnMappings = ref<RfqExcelColumnMappingRow[]>([])
const skippedRows = ref(0)
const hasRequiredColumns = ref(true)
const missingRequiredFields = ref<string[]>([])
const brandMatchingLoading = ref(false)
const aiMappingLoading = ref(false)
const aiBrandLoading = ref(false)
const parsingFile = ref(false)
const submitting = ref(false)

async function scheduleFitMainDialog() {
  if (!visible.value) return
  await nextTick()
  requestAnimationFrame(() => {
    requestAnimationFrame(() => fitMainDialogToContent())
  })
}

watch(
  () =>
    [
      rawRows.value.length,
      step.value,
      headerRowIndex.value,
      selectedSheetIndex.value,
      columnMappings.value.length,
      brandMatchingLoading.value,
      aiMappingLoading.value,
      aiBrandLoading.value
    ] as const,
  () => {
    void scheduleFitMainDialog()
  }
)

const validItems = computed(() => parsedRows.value.filter((r) => !r._error))
const errorItems = computed(() => parsedRows.value.filter((r) => !!r._error))
const brandLearnedRuleMatchedCount = computed(
  () =>
    validItems.value.filter(
      (r) => r._brandMatchStatus === 'matched' && r._brandMappingSource !== 'ai'
    ).length
)
const brandAiMatchedCount = computed(
  () => validItems.value.filter((r) => r._brandMatchStatus === 'matched' && r._brandMappingSource === 'ai').length
)
const pendingBrandCount = computed(
  () =>
    validItems.value.filter((r) => r._brandMatchStatus === 'pending' || r._brandMatchStatus === 'empty')
      .length
)

function mappingSourceTagType(source?: RfqExcelMappingSource) {
  if (source === 'ai') return 'warning'
  if (source === 'manual') return 'info'
  return 'success'
}

async function loadRowsFromFile() {
  if (!rawFile.value) {
    resetSheetState()
    rawRows.value = []
    return
  }
  parsingFile.value = true
  try {
    workbookCache = await loadRfqExcelWorkbook(rawFile.value)
    sheetNames.value = workbookCache.sheetNames
    if (selectedSheetIndex.value >= sheetNames.value.length) {
      selectedSheetIndex.value = 0
    }
    applyRowsFromSelectedSheet()
  } catch {
    resetSheetState()
    rawRows.value = []
    ElMessage.error(t('rfqExcelImport.parseFailed'))
  } finally {
    parsingFile.value = false
    if (rawRows.value.length > 0) {
      await scheduleFitMainDialog()
    }
  }
}

async function handleFileChange(file: { raw: File; name: string }) {
  rawFile.value = file.raw
  uploadedFileName.value = file.name
  headerRowIndex.value = 0
  headerRowNumber.value = 1
  selectedSheetIndex.value = 0
  await loadRowsFromFile()
}

function clearFile() {
  rawFile.value = null
  uploadedFileName.value = ''
  resetSheetState()
  rawRows.value = []
  headerRowIndex.value = 0
  headerRowNumber.value = 1
}

function onSheetIndexChange() {
  headerRowIndex.value = 0
  headerRowNumber.value = 1
  applyRowsFromSelectedSheet()
  void scheduleFitMainDialog()
}

function onHeaderRowNumberChange(val: number | undefined) {
  const n = val ?? 1
  headerRowIndex.value = Math.max(0, Math.min(n - 1, headerRowMax.value - 1))
  headerRowNumber.value = headerRowIndex.value + 1
}

async function resolveBrandMatches(items: ParsedItem[]) {
  brandMatchingLoading.value = true
  try {
    const keywords = items
      .filter((it) => !it._error)
      .map((it) => resolveBrandMatchKeyword(it.brand, it.customerBrand))
      .filter(Boolean)
    const cache = await buildBrandMatchCache(keywords)

    for (const it of items) {
      if (it._error) continue
      const kw = resolveBrandMatchKeyword(it.brand, it.customerBrand)
      if (!kw) {
        it._brandMatchStatus = 'empty'
        it._brandMatchLabel = t('rfqExcelImport.brandMissing')
        it._importBrandText = undefined
        continue
      }
      const result = cache.get(normalizeBrandSourceKey(kw)) ?? { status: 'pending' as const, matchKeyword: kw }
      it._brandMatchStatus = result.status
      it._brandMatchLabel = brandMatchStatusLabel(result)
      it._brandMappingSource = result.mappingSource
      it._importBrandText = kw
      if (result.status === 'matched' && result.brandId) {
        it.brandId = result.brandId
        it.brand = result.standardBrand ?? kw
      } else {
        it.brand = kw
        it.brandId = undefined
      }
    }
  } finally {
    brandMatchingLoading.value = false
  }
}

async function invokeAiBrandMatching() {
  const pendingItems = validItems.value.filter((r) => r._brandMatchStatus === 'pending' && r._importBrandText)
  if (!pendingItems.length) {
    ElMessage.info(t('rfqExcelImport.aiBrandMapNoPending'))
    return
  }

  const uniqueTexts = [...new Set(pendingItems.map((r) => (r._importBrandText || '').trim()).filter(Boolean))]
  aiBrandLoading.value = true
  try {
    const { sourceTextsJson } = buildAiBrandMapInput(uniqueTexts)
    const result = await aiApi.invoke({
      scenarioCode: AI_SCENARIO_ENTITY_PARSE_RFQ_EXCEL_BRAND_MAP,
      input: { source_texts: sourceTextsJson },
      bizType: 'RFQ'
    })

    const aiResult = parseAiBrandMapResponse(result.data, result.content ?? '')
    if (!aiResult?.mappings.length) {
      ElMessage.info(t('rfqExcelImport.aiBrandMapNoResult'))
      return
    }

    const aiCache = new Map<string, { brandId: number; standardBrand: string }>()
    for (const mapping of aiResult.mappings) {
      const standardBrand = (mapping.standardBrand || '').trim()
      if (!standardBrand) continue
      const opts = await fetchBrandOptionsForKeyword(standardBrand)
      const match = pickBizBrandMatch(standardBrand, opts)
      if (!match?.id) continue
      aiCache.set(normalizeBrandSourceKey(mapping.sourceText), {
        brandId: match.id,
        standardBrand: (match.standardBrand || standardBrand).trim()
      })
    }

    let matchedNow = 0
    for (const it of parsedRows.value) {
      if (it._error || it._brandMatchStatus !== 'pending') continue
      const kw = (it._importBrandText || resolveBrandMatchKeyword(it.brand, it.customerBrand)).trim()
      if (!kw) continue
      const hit = aiCache.get(normalizeBrandSourceKey(kw))
      if (!hit) continue
      it.brandId = hit.brandId
      it.brand = hit.standardBrand
      it._brandMatchStatus = 'matched'
      it._brandMappingSource = 'ai'
      it._brandMatchLabel = brandMatchStatusLabel({
        status: 'matched',
        brandId: hit.brandId,
        standardBrand: hit.standardBrand,
        matchKeyword: kw,
        mappingSource: 'ai'
      })
      matchedNow++
    }

    if (matchedNow > 0) {
      ElMessage.success(t('rfqExcelImport.aiBrandMapSuccess', { count: matchedNow }))
    } else {
      ElMessage.info(t('rfqExcelImport.aiBrandMapNoResult'))
    }
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('rfqExcelImport.aiBrandMapFailed')))
  } finally {
    aiBrandLoading.value = false
  }
}

function mapParseRows(rows: RfqExcelParseRowResult[]): ParsedItem[] {
  return rows.map((r) => ({
    ...r.item,
    _error: r.error
  }))
}

function currentHeaders(): unknown[] {
  return rawRows.value[headerRowIndex.value] ?? []
}

function buildRuleMappingsForCurrentSheet() {
  return buildRuleColumnMappings(currentHeaders(), {
    rows: rawRows.value,
    headerRowIndex: headerRowIndex.value
  })
}

async function applyParseFromMappings(mappings: RfqExcelColumnMappingRow[], options?: { skipBrand?: boolean }) {
  const visibleMappings = filterColumnMappingsWithData(
    mappings,
    rawRows.value,
    headerRowIndex.value
  )
  const result = parseRfqExcelRows(rawRows.value, {
    headerRowIndex: headerRowIndex.value,
    columnMappings: visibleMappings
  })
  columnMappings.value = visibleMappings
  skippedRows.value = result.skippedEmptyRows
  hasRequiredColumns.value = result.hasRequiredColumns
  missingRequiredFields.value = result.missingRequiredFields
  parsedRows.value = mapParseRows(result.rows)
  if (!options?.skipBrand) {
    await resolveBrandMatches(parsedRows.value)
  }
}

function applyRuleMappings() {
  const mappings = buildRuleMappingsForCurrentSheet()
  void applyParseFromMappings(mappings)
}

async function invokeAiColumnMapping(silent: boolean) {
  const headers = currentHeaders()
  if (!headers.some((h) => String(h ?? '').trim())) {
    if (!silent) ElMessage.warning(t('rfqExcelImport.noHeaderText'))
    return false
  }

  const beforeMappings = columnMappings.value.map((m) => ({ colIndex: m.colIndex, fieldKey: m.fieldKey }))
  const hadRequiredBefore = hasRequiredColumns.value
  const validBefore = validItems.value.length

  aiMappingLoading.value = true
  try {
    const { headersJson, targetFieldsJson } = buildAiColumnMapInput(headers)
    const result = await aiApi.invoke({
      scenarioCode: AI_SCENARIO_ENTITY_PARSE_RFQ_EXCEL_COLUMN_MAP,
      input: {
        headers: headersJson,
        target_fields: targetFieldsJson
      },
      bizType: 'RFQ'
    })

    const aiResult = parseAiColumnMapResponse(result.data, result.content ?? '', headers, headerRowIndex.value)
    if (!aiResult) {
      if (hasRequiredColumns.value && validItems.value.length > 0) {
        if (!silent) ElMessage.success(t('rfqExcelImport.aiMapReady', { rows: validItems.value.length }))
        return true
      }
      if (hasRequiredColumns.value) {
        if (!silent) ElMessage.info(t('rfqExcelImport.aiMapParseSkipped'))
        return true
      }
      if (!silent) ElMessage.error(t('rfqExcelImport.aiMapFailed'))
      return false
    }

    if (aiResult.headerRowIndex !== headerRowIndex.value && aiResult.headerRowIndex < rawRows.value.length) {
      headerRowIndex.value = aiResult.headerRowIndex
      headerRowNumber.value = headerRowIndex.value + 1
    }

    const baseMappings =
      columnMappings.value.length > 0 ? columnMappings.value : buildRuleMappingsForCurrentSheet()
    const merged = finalizeMappingsAfterAi(baseMappings, aiResult)
    await applyParseFromMappings(merged)
    if (!silent) notifyAiMappingOutcome(beforeMappings, hadRequiredBefore, validBefore, merged)
    return true
  } catch (e) {
    if (!silent) ElMessage.error(getApiErrorMessage(e, t('rfqExcelImport.aiMapFailed')))
    return false
  } finally {
    aiMappingLoading.value = false
  }
}

/** AI 合并后，对仍为「未识别」的列再尝试规则同义词（不覆盖 AI 已填列） */
function finalizeMappingsAfterAi(
  baseMappings: RfqExcelColumnMappingRow[],
  aiResult: RfqExcelAiColumnMapResult
) {
  const merged = mergeRuleAndAiMappings(baseMappings, aiResult)
  const usedFields = new Set(
    merged.map((m) => m.fieldKey).filter((k): k is RfqExcelItemFieldKey => !!k)
  )
  return merged.map((m) => {
    if (m.fieldKey) return m
    const fk = resolveHeaderFieldKey(m.headerText)
    if (!fk || usedFields.has(fk)) return m
    usedFields.add(fk)
    const meta = RFQ_EXCEL_FIELD_METAS.find((f) => f.key === fk)
    return {
      ...m,
      fieldKey: fk,
      fieldLabel: fieldMetaLabel(fk),
      required: meta?.required ?? false,
      matched: true,
      mappingSource: 'rule' as const
    }
  })
}

function notifyAiMappingOutcome(
  beforeMappings: Array<{ colIndex: number; fieldKey: RfqExcelItemFieldKey | null | undefined }>,
  hadRequiredBefore: boolean,
  validBefore: number,
  merged: RfqExcelColumnMappingRow[]
) {
  const newlyMappedCount = merged.filter((m) => {
    const prev = beforeMappings.find((b) => b.colIndex === m.colIndex)
    return !!m.fieldKey && !prev?.fieldKey
  }).length
  const aiFilledCount = merged.filter((m) => m.mappingSource === 'ai' && m.fieldKey).length
  const validNow = validItems.value.length
  const improved =
    newlyMappedCount > 0 ||
    (!hadRequiredBefore && hasRequiredColumns.value) ||
    validNow > validBefore

  if (improved) {
    if (aiFilledCount > 0) {
      ElMessage.success(t('rfqExcelImport.aiMapSuccessFilled', { count: aiFilledCount }))
    } else {
      ElMessage.success(t('rfqExcelImport.aiMapReady', { rows: validNow }))
    }
    return
  }

  if (hasRequiredColumns.value && validNow > 0) {
    ElMessage.success(t('rfqExcelImport.aiMapReady', { rows: validNow }))
    return
  }

  if (!hasRequiredColumns.value) {
    ElMessage.warning(t('rfqExcelImport.aiMapStillIncomplete'))
  }
}

function onMappingFieldChange(row: RfqExcelColumnMappingRow, newKey: RfqExcelItemFieldKey | null) {
  const updated = columnMappings.value.map((m) => {
    if (m.colIndex === row.colIndex) {
      const meta = newKey ? RFQ_EXCEL_FIELD_METAS.find((f) => f.key === newKey) : undefined
      return {
        ...m,
        fieldKey: newKey,
        fieldLabel: fieldMetaLabel(newKey),
        required: meta?.required ?? false,
        matched: !!newKey,
        mappingSource: 'manual' as const,
        confidence: null
      }
    }
    if (newKey && m.fieldKey === newKey) {
      return {
        ...m,
        fieldKey: null,
        fieldLabel: '—',
        required: false,
        matched: false,
        mappingSource: 'manual' as const,
        confidence: null
      }
    }
    return m
  })
  columnMappings.value = updated
  void applyParseFromMappings(updated)
}

async function goToMapping() {
  if (!rawFile.value) return
  try {
    if (!rawRows.value.length) await loadRowsFromFile()
    if (rawRows.value.length < 2) {
      ElMessage.warning(t('rfqExcelImport.noDataRows'))
      return
    }

    const dataRowCount = countNonEmptyDataRows(rawRows.value, headerRowIndex.value)
    if (dataRowCount === 0) {
      ElMessage.warning(t('rfqExcelImport.noDataRows'))
      return
    }
    if (dataRowCount > RFQ_EXCEL_MAX_DATA_ROWS) {
      ElMessage.error(t('rfqExcelImport.maxRowsExceeded', { max: RFQ_EXCEL_MAX_DATA_ROWS }))
      return
    }

    const ruleMappings = buildRuleMappingsForCurrentSheet()
    const ruleCheck = parseRfqExcelRows(rawRows.value, {
      headerRowIndex: headerRowIndex.value,
      columnMappings: ruleMappings
    })

    step.value = 2
    columnMappings.value = ruleMappings
    skippedRows.value = ruleCheck.skippedEmptyRows
    hasRequiredColumns.value = ruleCheck.hasRequiredColumns
    missingRequiredFields.value = ruleCheck.missingRequiredFields
    parsedRows.value = mapParseRows(ruleCheck.rows)

    if (!ruleCheck.hasRequiredColumns) {
      const aiOk = await invokeAiColumnMapping(true)
      if (!aiOk) {
        await applyParseFromMappings(ruleMappings, { skipBrand: false })
      }
    } else {
      await resolveBrandMatches(parsedRows.value)
    }
  } catch {
    ElMessage.error(t('rfqExcelImport.parseFailed'))
  }
}

function buildParsedRfqFields(): ParsedRfqFields {
  const base = emptyParsedRfq()
  base.items = validItems.value.map((it) => ({
    customerMpn: it.customerMpn,
    customerBrand: it.customerBrand,
    mpn: it.mpn,
    brand: it.brand,
    brandId: it.brandId,
    targetPrice: it.targetPrice,
    priceCurrency: it.priceCurrency,
    quantity: it.quantity,
    productionDate: it.productionDate,
    expiryDate: it.expiryDate,
    minPackageQty: it.minPackageQty,
    minOrderQty: it.minOrderQty,
    alternativeMaterials: it.alternativeMaterials,
    remark: it.remark,
    _importBrandText: it.brandId && it.brandId > 0 ? undefined : it._importBrandText
  }))
  return base
}

async function handleConfirmParse() {
  if (!hasRequiredColumns.value) {
    ElMessage.error(t('rfqExcelImport.missingRequiredColumns', { fields: missingRequiredFields.value.join('、') }))
    return
  }
  if (validItems.value.length === 0) {
    ElMessage.warning(t('rfqExcelImport.noValidRows'))
    return
  }
  submitting.value = true
  try {
    emit('parsed', buildParsedRfqFields())
    handleClose()
  } finally {
    submitting.value = false
  }
}

function handleClose() {
  visible.value = false
  fieldDescVisible.value = false
  step.value = 1
  uploadedFileName.value = ''
  rawFile.value = null
  resetSheetState()
  rawRows.value = []
  headerRowIndex.value = 0
  headerRowNumber.value = 1
  parsedRows.value = []
  columnMappings.value = []
  skippedRows.value = 0
  hasRequiredColumns.value = true
  missingRequiredFields.value = []
  brandMatchingLoading.value = false
  aiMappingLoading.value = false
  parsingFile.value = false
}

function downloadTemplate() {
  const headers = [
    '客户物料型号',
    '物料型号(MPN)*',
    '客户品牌',
    '供应品牌',
    '数量*',
    '目标价',
    `货币(${DEFAULT_SETTLEMENT_CURRENCY_STRING}/USD/EUR/HKD)`,
    '最小包装量',
    '最小起订量(MOQ)',
    '可替代料(逗号分隔)',
    '备注'
  ]
  const exampleRow = [
    'ABC-001',
    'STM32F103C8T6',
    'ST',
    'STMicroelectronics',
    '1000',
    '2.5',
    DEFAULT_SETTLEMENT_CURRENCY_STRING,
    '100',
    '500',
    'STM32F103CBT6',
    '需2年内产品'
  ]
  const ws = XLSX.utils.aoa_to_sheet([headers, exampleRow])
  ws['!cols'] = headers.map(() => ({ wch: 20 }))
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'RFQ明细')
  XLSX.writeFile(wb, 'RFQ导入模板.xlsx')
}
</script>

<style lang="scss">
.import-rfq-dialog,
.import-rfq-field-desc-dialog {
  &.el-dialog {
    max-width: calc(100vw - 48px);
  }

  .el-dialog__body {
    padding: 16px 20px;
    box-sizing: border-box;
    overflow-y: auto;
  }
}

.crm-dialog-resizable {
  position: relative !important;
  display: flex !important;
  flex-direction: column !important;
  overflow: hidden !important;
  max-height: none !important;
  margin-top: 8vh !important;

  .el-dialog__header {
    flex: 0 0 auto;
  }

  .el-dialog__body {
    flex: 1 1 auto;
    min-height: 0;
    max-height: none !important;
    overflow-y: auto;
  }

  .el-dialog__footer {
    flex: 0 0 auto;
  }
}

.crm-dialog-resize-handle {
  position: absolute;
  z-index: 10;
  background: transparent;

  &:hover {
    background: rgba(64, 158, 255, 0.15);
  }

  &--e {
    top: 0;
    right: 0;
    width: 12px;
    height: 100%;
    cursor: ew-resize;
  }

  &--s {
    left: 0;
    bottom: 0;
    width: 100%;
    height: 12px;
    cursor: ns-resize;
  }

  &--se {
    right: 0;
    bottom: 0;
    width: 20px;
    height: 20px;
    cursor: nwse-resize;
  }
}
</style>

<style lang="scss" scoped>
.import-rfq-step-pane {
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
}

.step-upload {
  .upload-tips {
    margin-bottom: 16px;
  }

  .excel-upload-area {
    width: 100%;
    :deep(.el-upload) {
      width: 100%;
    }
    :deep(.el-upload-dragger) {
      width: 100%;
      padding: 28px 12px;
      background: rgba(0, 212, 255, 0.03);
      border-color: rgba(0, 212, 255, 0.2);
      &:hover {
        border-color: rgba(0, 212, 255, 0.5);
      }
    }
  }

  .uploaded-file-info {
    display: flex;
    align-items: center;
    gap: 8px;
    margin: 10px 0;
    min-height: 48px;
    padding: 0 16px;
    background: rgba(103, 194, 58, 0.08);
    border: 1px solid rgba(103, 194, 58, 0.2);
    border-radius: 4px;
    font-size: 13px;
    color: #4d4d4d;
  }

  .import-options {
    margin: 12px 0 8px;
    font-size: 13px;

    &__row {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: 16px 24px;
    }

    &__item {
      display: flex;
      align-items: center;
      gap: 10px;
    }

    &__label {
      color: #4d4d4d;
      white-space: nowrap;
    }

    &__sheet {
      min-width: 200px;
    }

    &__hints {
      display: flex;
      flex-wrap: wrap;
      gap: 8px 16px;
      margin-top: 6px;
      font-size: 12px;
      color: #909399;
    }
  }

  .header-preview-table {
    margin-top: 12px;
    .mapping-title {
      font-size: 13px;
      color: #4d4d4d;
      margin-bottom: 8px;
    }
  }
}

.step-mapping {
  .mapping-toolbar {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 10px;

    &__meta {
      margin-left: auto;
      font-size: 12px;
      color: #909399;
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 2px;
    }
  }

  .parse-stats {
    margin-bottom: 10px;
  }

  .mapping-result-table {
    .mapping-title {
      font-size: 13px;
      color: #4d4d4d;
      margin-bottom: 8px;
    }
  }

  .mapping-field-select {
    width: 100%;
  }
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}

.field-desc-hint {
  font-size: 13px;
  color: #4d4d4d;
  margin-bottom: 10px;
}
</style>
