<template>
  <el-dialog
    v-model="visible"
    :title="stepTitle"
    width="1320px"
    :close-on-click-modal="false"
    :before-close="handleClose"
    class="import-rfq-dialog"
    destroy-on-close
  >
    <!-- ── STEP 1：上传 Excel ── -->
    <div v-if="step === 1" class="step-upload import-rfq-step-pane">
      <div class="upload-tips">
        <el-alert type="info" :closable="false" show-icon>
          <template #title>
            <span>
              请上传 Excel 文件（.xlsx / .xls），每行代表一条 RFQ 明细（RFQItem）。
              <el-link type="primary" :underline="false" @click="downloadTemplate" style="margin-left:8px;">
                <el-icon><Download /></el-icon> 下载模板
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
          拖拽 Excel 文件到此处，或 <em>点击上传</em>
        </div>
        <template #tip>
          <div class="el-upload__tip">支持 .xlsx / .xls 格式，文件大小不超过 10MB</div>
        </template>
      </el-upload>

      <div v-if="uploadedFileName" class="uploaded-file-info">
        <el-icon color="#67c23a"><CircleCheckFilled /></el-icon>
        <span>已选择：<strong>{{ uploadedFileName }}</strong></span>
        <el-button link type="primary" @click="clearFile">重新选择</el-button>
      </div>

      <div class="field-mapping-table">
        <div class="mapping-title">Excel 列对应关系</div>
        <el-table :data="columnMapping" size="small" border>
          <el-table-column prop="col" label="Excel 列" width="100" align="center" />
          <el-table-column prop="field" label="字段名称" width="160" />
          <el-table-column prop="required" label="必填" width="70" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.required" type="danger" size="small">必填</el-tag>
              <span v-else style="color:#909399">-</span>
            </template>
          </el-table-column>
          <el-table-column prop="example" label="示例值" />
          <el-table-column prop="note" label="说明" />
        </el-table>
      </div>
    </div>

    <!-- ── STEP 2：预览解析结果 ── -->
    <div v-if="step === 2" class="step-preview import-rfq-step-pane">
      <div class="parse-stats">
        <el-tag type="success">成功解析 {{ validItems.length }} 行</el-tag>
        <el-tag v-if="matchedBrandCount" type="success" style="margin-left:8px;">
          品牌已匹配 {{ matchedBrandCount }} 行
        </el-tag>
        <el-tag v-if="pendingBrandCount" type="warning" style="margin-left:8px;">
          品牌待选择 {{ pendingBrandCount }} 行
        </el-tag>
        <el-tag v-if="errorItems.length" type="danger" style="margin-left:8px;">
          {{ errorItems.length }} 行有错误
        </el-tag>
        <el-tag v-if="skippedRows > 0" type="info" style="margin-left:8px;">
          跳过 {{ skippedRows }} 行空行
        </el-tag>
      </div>

      <el-alert
        v-if="errorItems.length"
        type="warning"
        :closable="false"
        style="margin-bottom:10px;"
      >
        <template #title>
          以下行存在问题（已标红），将忽略错误行；有效行将进入「新建需求」页继续编辑。
        </template>
      </el-alert>

      <div ref="previewTableWrapRef" class="preview-table-wrap">
        <el-table
          v-loading="brandMatchingLoading"
          class="preview-table"
          :data="previewItems"
          size="small"
          border
          :max-height="previewTableMaxHeight"
          :row-class-name="getRowClass"
        >
        <el-table-column type="index" label="行" width="52" align="center" />
        <el-table-column
          prop="customerMaterialModel"
          label="客户物料型号"
          min-width="120"
          show-overflow-tooltip
        />
        <el-table-column prop="materialModel" label="物料型号(MPN)" min-width="150" show-overflow-tooltip />
        <el-table-column prop="customerBrand" label="客户品牌" width="96" show-overflow-tooltip />
        <el-table-column label="供应品牌" width="108" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row._supplyBrandDisplay || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="品牌匹配" min-width="168" show-overflow-tooltip>
          <template #default="{ row }">
            <el-tag v-if="row._error" type="danger" size="small">{{ row._error }}</el-tag>
            <el-tag v-else-if="row._brandMatchStatus === 'matched'" type="success" size="small">
              {{ row._brandMatchLabel }}
            </el-tag>
            <el-tag v-else-if="row._brandMatchStatus === 'pending'" type="warning" size="small">
              {{ row._brandMatchLabel }}
            </el-tag>
            <el-tag v-else-if="row._brandMatchStatus === 'empty'" type="info" size="small">
              缺少品牌
            </el-tag>
            <span v-else style="color:#909399">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="quantity" label="数量" width="96" align="right" />
        <el-table-column prop="targetPrice" label="目标价" width="100" align="right">
          <template #default="{ row }">
            {{ row.targetPrice != null ? row.targetPrice : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="currency" label="货币" width="88" align="center" />
        <el-table-column prop="remark" label="备注" min-width="100" show-overflow-tooltip />
        </el-table>
      </div>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="handleClose">取消</el-button>
        <el-button v-if="step === 2" @click="step = 1">上一步</el-button>
        <el-button
          v-if="step === 1"
          type="primary"
          :disabled="!uploadedFileName"
          @click="goToPreview"
        >
          下一步：预览数据
        </el-button>
        <el-button
          v-if="step === 2"
          type="primary"
          :loading="submitting"
          :disabled="validItems.length === 0 || brandMatchingLoading"
          @click="handleGoToCreate"
        >
          进入新建需求（{{ validItems.length }} 条明细）
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { UploadFilled, Download, CircleCheckFilled } from '@element-plus/icons-vue'
import * as XLSX from 'xlsx'
import type { CreateRFQItemRequest } from '@/types/rfq'
import { setAiPrefill } from '@/utils/aiPrefill'
import {
  brandMatchStatusLabel,
  buildBrandMatchCache,
  resolveBrandMatchKeyword,
  type BrandMatchStatus
} from '@/utils/bizBrandMatch'
import { DEFAULT_SETTLEMENT_CURRENCY_STRING } from '@/constants/currency'

const router = useRouter()

type PreviewItem = CreateRFQItemRequest & {
  _error?: string
  _supplyBrandDisplay?: string
  _brandMatchStatus?: BrandMatchStatus
  _brandMatchLabel?: string
  _importBrandText?: string
}

const visible = defineModel<boolean>({ default: false })
const step = ref(1)

const stepTitle = computed(() => {
  if (step.value === 1) return '导入 Excel 创建 RFQ — 第1步：上传文件'
  return '导入 Excel 创建 RFQ — 第2步：预览并确认'
})

const uploadedFileName = ref('')
const rawFile = ref<File | null>(null)

function handleFileChange(file: any) {
  rawFile.value = file.raw
  uploadedFileName.value = file.name
}

function clearFile() {
  rawFile.value = null
  uploadedFileName.value = ''
}

const previewItems = ref<PreviewItem[]>([])
const skippedRows = ref(0)
const brandMatchingLoading = ref(false)

const validItems = computed(() => previewItems.value.filter((r) => !r._error))
const errorItems = computed(() => previewItems.value.filter((r) => !!r._error))
const matchedBrandCount = computed(
  () => validItems.value.filter((r) => r._brandMatchStatus === 'matched').length
)
const pendingBrandCount = computed(
  () =>
    validItems.value.filter((r) => r._brandMatchStatus === 'pending' || r._brandMatchStatus === 'empty')
      .length
)


const previewTableWrapRef = ref<HTMLElement | null>(null)
const previewTableMaxHeight = ref(480)
let previewTableResizeObserver: ResizeObserver | null = null

function updatePreviewTableHeight() {
  const el = previewTableWrapRef.value
  if (!el) return
  previewTableMaxHeight.value = Math.max(240, el.clientHeight)
}

function bindPreviewTableResizeObserver() {
  previewTableResizeObserver?.disconnect()
  previewTableResizeObserver = null
  const el = previewTableWrapRef.value
  if (!el) return
  updatePreviewTableHeight()
  previewTableResizeObserver = new ResizeObserver(updatePreviewTableHeight)
  previewTableResizeObserver.observe(el)
}

watch(
  () => [step.value, visible.value] as const,
  async ([s, open]) => {
    if (s !== 2 || !open) return
    await nextTick()
    bindPreviewTableResizeObserver()
  }
)

onUnmounted(() => {
  previewTableResizeObserver?.disconnect()
})

const submitting = ref(false)

const columnMapping = [
  { col: 'A', field: '客户物料型号', required: false, example: 'ABC-123', note: '客户自己的物料编号' },
  { col: 'B', field: '物料型号(MPN)', required: true, example: 'STM32F103C8T6', note: '标准物料型号，必填' },
  { col: 'C', field: '客户品牌', required: false, example: 'ST', note: 'D 列为空时用于品牌匹配' },
  { col: 'D', field: '供应品牌', required: false, example: 'STMicroelectronics', note: '优先匹配；支持中英文名/别名' },
  { col: 'E', field: '数量', required: true, example: '1000', note: '需求数量，必填，正整数' },
  { col: 'F', field: '目标价', required: false, example: '2.5', note: '目标单价' },
  { col: 'G', field: '货币', required: false, example: 'USD', note: 'USD/RMB/HKD/EUR，默认 USD' },
  { col: 'H', field: '最小包装量', required: false, example: '100', note: '最小包装数量' },
  { col: 'I', field: '最小起订量', required: false, example: '500', note: 'MOQ' },
  { col: 'J', field: '可替代料', required: false, example: 'STM32F103CBT6', note: '多个用逗号分隔' },
  { col: 'K', field: '备注', required: false, example: '需2年内产品', note: '行备注' },
]

function mapCurrencyToPriceCurrency(c?: string | number): number {
  if (typeof c === 'number' && c >= 1 && c <= 4) return c
  const u = String(c || '').toUpperCase()
  if (u.includes('USD')) return 2
  if (u.includes('EUR')) return 3
  if (u.includes('HKD')) return 4
  return 1
}

function parseExcel(file: File): Promise<PreviewItem[]> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = (e) => {
      try {
        const data = new Uint8Array(e.target!.result as ArrayBuffer)
        const wb = XLSX.read(data, { type: 'array' })
        const ws = wb.Sheets[wb.SheetNames[0]]
        const rows: any[][] = XLSX.utils.sheet_to_json(ws, { header: 1, defval: '' })

        const dataRows = rows.slice(1)
        skippedRows.value = 0
        let lineNo = 1
        const items: PreviewItem[] = []

        for (const row of dataRows) {
          const isEmptyRow = row.every((cell: any) => cell === '' || cell == null)
          if (isEmptyRow) {
            skippedRows.value++
            continue
          }

          const customerMaterialModel = String(row[0] || '').trim()
          const materialModel = String(row[1] || '').trim()
          const customerBrand = String(row[2] || '').trim()
          const supplyBrand = String(row[3] || '').trim()
          const quantityRaw = row[4]
          const targetPriceRaw = row[5]
          const currency = String(row[6] || DEFAULT_SETTLEMENT_CURRENCY_STRING).trim().toUpperCase() || DEFAULT_SETTLEMENT_CURRENCY_STRING
          const minPackageQty = row[7] ? Number(row[7]) : undefined
          const moq = row[8] ? Number(row[8]) : undefined
          const alternatives = String(row[9] || '').trim()
          const remark = String(row[10] || '').trim()

          let error = ''
          if (!materialModel) error = '缺少MPN'
          const quantity = Number(quantityRaw)
          if (!error && (isNaN(quantity) || quantity <= 0)) error = '数量无效'

          const matchKeyword = resolveBrandMatchKeyword(supplyBrand, customerBrand)

          const item: PreviewItem = {
            lineNo: lineNo++,
            customerMaterialModel: customerMaterialModel || undefined,
            materialModel,
            customerMpn: customerMaterialModel || undefined,
            mpn: materialModel,
            customerBrand: customerBrand || undefined,
            brand: supplyBrand || undefined,
            quantity: isNaN(quantity) ? 0 : quantity,
            targetPrice: targetPriceRaw !== '' && targetPriceRaw != null ? Number(targetPriceRaw) : undefined,
            currency: currency || DEFAULT_SETTLEMENT_CURRENCY_STRING,
            minPackageQty,
            moq,
            minOrderQty: moq,
            alternatives: alternatives || undefined,
            alternativeMaterials: alternatives || undefined,
            remark: remark || undefined,
            _key: lineNo,
            _supplyBrandDisplay: supplyBrand || customerBrand || '',
            _importBrandText: matchKeyword || undefined
          }
          if (error) item._error = error
          items.push(item)
        }
        resolve(items)
      } catch (err) {
        reject(err)
      }
    }
    reader.onerror = reject
    reader.readAsArrayBuffer(file)
  })
}

async function resolvePreviewBrandMatches(items: PreviewItem[]) {
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
        it._brandMatchLabel = '缺少品牌'
        it._importBrandText = undefined
        continue
      }
      const result = cache.get(kw.toLowerCase()) ?? { status: 'pending' as const, matchKeyword: kw }
      it._brandMatchStatus = result.status
      it._brandMatchLabel = brandMatchStatusLabel(result)
      it._importBrandText = kw
      if (result.status === 'matched' && result.brandId) {
        it.brandId = result.brandId
        it.brand = result.standardBrand
      } else {
        it.brand = kw
        it.brandId = undefined
      }
    }
  } finally {
    brandMatchingLoading.value = false
  }
}

async function goToPreview() {
  if (!rawFile.value) return
  try {
    previewItems.value = await parseExcel(rawFile.value)
    if (previewItems.value.length === 0) {
      ElMessage.warning('Excel 中没有有效数据行，请检查文件内容')
      return
    }
    step.value = 2
    await resolvePreviewBrandMatches(previewItems.value)
    await nextTick()
    bindPreviewTableResizeObserver()
  } catch {
    ElMessage.error('Excel 解析失败，请检查文件格式')
  }
}

function getRowClass({ row }: { row: PreviewItem }) {
  return row._error ? 'row-error' : ''
}

function buildPrefillPayload() {
  return {
    _prefillSource: 'excel-import',
    items: validItems.value.map((it) => ({
      customerMpn: it.customerMpn || it.customerMaterialModel || '',
      customerBrand: it.customerBrand || '',
      mpn: it.mpn || it.materialModel || '',
      brand: it.brand || '',
      brandId: it.brandId,
      quantity: it.quantity,
      targetPrice: it.targetPrice,
      priceCurrency: mapCurrencyToPriceCurrency(it.currency),
      minPackageQty: it.minPackageQty,
      minOrderQty: it.minOrderQty ?? it.moq,
      alternativeMaterials: it.alternativeMaterials || it.alternatives || '',
      remark: it.remark || '',
      _importBrandText:
        it.brandId && it.brandId > 0 ? undefined : it._importBrandText || undefined
    }))
  }
}

async function handleGoToCreate() {
  if (validItems.value.length === 0) {
    ElMessage.warning('没有有效的明细行')
    return
  }
  submitting.value = true
  try {
    const token = setAiPrefill('RFQ', buildPrefillPayload())
    visible.value = false
    handleClose()
    await router.push({ name: 'RFQCreate', query: { aiPrefill: token } })
  } finally {
    submitting.value = false
  }
}

function handleClose() {
  visible.value = false
  step.value = 1
  uploadedFileName.value = ''
  rawFile.value = null
  previewItems.value = []
  skippedRows.value = 0
  brandMatchingLoading.value = false
}

function downloadTemplate() {
  const headers = [
    '客户物料型号', '物料型号(MPN)*', '客户品牌', '供应品牌',
    '数量*', '目标价', '货币(RMB/USD/EUR/HKD)',
    '最小包装量', '最小起订量(MOQ)', '可替代料(逗号分隔)', '备注'
  ]
  const exampleRow = [
    'ABC-001', 'STM32F103C8T6', 'ST', 'STMicroelectronics',
    '1000', '2.5', 'RMB',
    '100', '500', 'STM32F103CBT6', '需2年内产品'
  ]
  const ws = XLSX.utils.aoa_to_sheet([headers, exampleRow])
  ws['!cols'] = headers.map(() => ({ wch: 20 }))
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'RFQ明细')
  XLSX.writeFile(wb, 'RFQ导入模板.xlsx')
}
</script>

<style lang="scss">
$import-rfq-dialog-body-height: 900px;

.import-rfq-dialog {
  .el-dialog__body {
    padding: 16px 20px;
    height: $import-rfq-dialog-body-height;
    min-height: $import-rfq-dialog-body-height;
    max-height: calc(100vh - 120px);
    box-sizing: border-box;
    display: flex;
    flex-direction: column;
    overflow: hidden;
  }

  .el-dialog {
    max-width: calc(100vw - 48px);
  }

  .el-table .row-error td {
    background-color: rgba(245, 108, 108, 0.08) !important;
  }
}
</style>

<style lang="scss" scoped>
.import-rfq-step-pane {
  flex: 1;
  min-height: 0;
  height: 100%;
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
}

.step-upload {
  overflow: hidden;
  .upload-tips { margin-bottom: 16px; }

  .excel-upload-area {
    width: 100%;
    :deep(.el-upload) { width: 100%; }
    :deep(.el-upload-dragger) {
      width: 100%;
      padding: 28px 12px;
      background: rgba(0, 212, 255, 0.03);
      border-color: rgba(0, 212, 255, 0.2);
      &:hover { border-color: rgba(0, 212, 255, 0.5); }
    }
  }

  .uploaded-file-info {
    display: flex;
    align-items: center;
    gap: 8px;
    margin: 10px 0;
    min-height: 72px;
    padding: 0 16px;
    background: rgba(103, 194, 58, 0.08);
    border: 1px solid rgba(103, 194, 58, 0.2);
    border-radius: 4px;
    font-size: 13px;
    line-height: 1.5;
    color: #4d4d4d;

    strong {
      color: #303133;
      font-weight: 600;
    }
  }

  .field-mapping-table {
    margin-top: 16px;
    .mapping-title {
      font-size: 13px;
      font-weight: 400;
      color: #4d4d4d;
      margin-bottom: 8px;
    }
  }
}

.step-preview {
  display: flex;
  flex-direction: column;
  height: 100%;

  .parse-stats {
    flex-shrink: 0;
    margin-bottom: 10px;
  }

  .preview-table-wrap {
    flex: 1;
    min-height: 0;
    overflow: hidden;
  }

  .preview-table {
    width: 100%;
    height: 100%;
  }
}

.dialog-footer {
  display: flex; justify-content: flex-end; gap: 10px;
}
</style>
