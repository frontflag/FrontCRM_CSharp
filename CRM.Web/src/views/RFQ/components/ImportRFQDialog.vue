<template>
  <el-dialog
    v-model="visible"
    :title="stepTitle"
    width="900px"
    :close-on-click-modal="false"
    :before-close="handleClose"
    class="import-rfq-dialog"
    destroy-on-close
  >
    <!-- ── STEP 1：上传 Excel ── -->
    <div v-if="step === 1" class="step-upload">
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

      <!-- 字段映射说明 -->
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
    <div v-if="step === 2" class="step-preview">
      <!-- RFQ 基础信息填写 -->
      <div class="rfq-base-form">
        <div class="section-label">RFQ 基础信息</div>
        <el-form :model="rfqBaseForm" label-width="100px" size="small" inline>
          <el-form-item label="客户" required>
            <el-select
              v-model="rfqBaseForm.customerId"
              filterable
              remote
              :remote-method="searchCustomers"
              :loading="customerSearchLoading"
              placeholder="请输入客户名称搜索"
              style="width:200px"
              clearable
            >
              <el-option
                v-for="c in customerOptions"
                :key="c.id"
                :label="c.customerName"
                :value="c.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="需求日期">
            <el-date-picker
              v-model="rfqBaseForm.rfqDate"
              type="date"
              value-format="YYYY-MM-DD"
              placeholder="选择日期"
              style="width:160px"
            />
          </el-form-item>
          <el-form-item label="来源">
            <el-select v-model="rfqBaseForm.source" style="width:120px" clearable>
              <el-option label="线下" :value="1" />
              <el-option label="线上" :value="2" />
              <el-option label="邮件" :value="3" />
              <el-option label="电话" :value="4" />
              <el-option label="导入" :value="5" />
            </el-select>
          </el-form-item>
          <el-form-item label="需求类型">
            <el-select v-model="rfqBaseForm.rfqType" style="width:120px" clearable>
              <el-option label="现货" :value="1" />
              <el-option label="期货" :value="2" />
              <el-option label="样品" :value="3" />
              <el-option label="批量" :value="4" />
            </el-select>
          </el-form-item>
          <el-form-item label="备注">
            <el-input v-model="rfqBaseForm.remark" placeholder="RFQ 备注" style="width:200px" />
          </el-form-item>
        </el-form>
      </div>

      <!-- 解析统计 -->
      <div class="parse-stats">
        <el-tag type="success">成功解析 {{ validItems.length }} 行</el-tag>
        <el-tag v-if="errorItems.length" type="danger" style="margin-left:8px;">
          {{ errorItems.length }} 行有错误
        </el-tag>
        <el-tag v-if="skippedRows > 0" type="warning" style="margin-left:8px;">
          跳过 {{ skippedRows }} 行空行
        </el-tag>
      </div>

      <!-- 错误提示 -->
      <el-alert
        v-if="errorItems.length"
        type="warning"
        :closable="false"
        style="margin-bottom:10px;"
      >
        <template #title>
          以下行存在问题（已标红），请检查后再提交，或忽略错误行继续创建。
        </template>
      </el-alert>

      <!-- 预览表格 -->
      <el-table
        :data="previewItems"
        size="small"
        border
        max-height="340"
        :row-class-name="getRowClass"
      >
        <el-table-column type="index" label="行" width="50" align="center" />
        <el-table-column prop="customerMaterialModel" label="客户物料型号" min-width="140" />
        <el-table-column prop="materialModel" label="物料型号(MPN)" min-width="140" />
        <el-table-column prop="customerBrand" label="客户品牌" width="110" />
        <el-table-column prop="brand" label="品牌" width="110" />
        <el-table-column prop="quantity" label="数量" width="80" align="right" />
        <el-table-column prop="targetPrice" label="目标价" width="90" align="right">
          <template #default="{ row }">
            {{ row.targetPrice != null ? row.targetPrice : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="currency" label="货币" width="70" align="center" />
        <el-table-column prop="remark" label="备注" min-width="100" show-overflow-tooltip />
        <el-table-column label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag v-if="row._error" type="danger" size="small">{{ row._error }}</el-tag>
            <el-tag v-else type="success" size="small">正常</el-tag>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <!-- ── STEP 3：创建结果 ── -->
    <div v-if="step === 3" class="step-result">
      <div v-if="createSuccess" class="result-success">
        <el-result icon="success" :title="`RFQ 创建成功`" :sub-title="`需求单号：${createdRFQCode}`">
          <template #extra>
            <el-button type="primary" @click="handleViewCreated">查看 RFQ 详情</el-button>
            <el-button @click="handleClose">关闭</el-button>
          </template>
        </el-result>
      </div>
      <div v-else class="result-error">
        <el-result icon="error" title="创建失败" :sub-title="createError">
          <template #extra>
            <el-button type="primary" @click="step = 2">返回修改</el-button>
            <el-button @click="handleClose">关闭</el-button>
          </template>
        </el-result>
      </div>
    </div>

    <!-- ── 底部按钮 ── -->
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
          :disabled="validItems.length === 0 || !rfqBaseForm.customerId"
          @click="handleSubmit"
        >
          确认创建 RFQ（{{ validItems.length }} 条明细）
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { UploadFilled, Download, CircleCheckFilled } from '@element-plus/icons-vue'
import * as XLSX from 'xlsx'
import { rfqApi } from '@/api/rfq'
import { customerApi } from '@/api/customer'
import type { CreateRFQRequest, CreateRFQItemRequest } from '@/types/rfq'

const router = useRouter()
const emit = defineEmits<{ (e: 'created', rfqId: string): void }>()

// ── 对话框状态 ──
const visible = defineModel<boolean>({ default: false })
const step = ref(1)

const stepTitle = computed(() => {
  if (step.value === 1) return '导入 Excel 创建 RFQ — 第1步：上传文件'
  if (step.value === 2) return '导入 Excel 创建 RFQ — 第2步：预览并确认'
  return '导入 Excel 创建 RFQ — 第3步：创建结果'
})

// ── 文件上传 ──
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

// ── 解析结果 ──
const previewItems = ref<(CreateRFQItemRequest & { _error?: string })[]>([])
const skippedRows = ref(0)

const validItems = computed(() => previewItems.value.filter(r => !r._error))
const errorItems = computed(() => previewItems.value.filter(r => !!r._error))

// ── RFQ 基础信息 ──
const rfqBaseForm = ref({
  customerId: '',
  rfqDate: new Date().toISOString().slice(0, 10),
  source: 5, // 导入
  rfqType: undefined as number | undefined,
  remark: '',
})

// ── 客户搜索 ──
const customerOptions = ref<any[]>([])
const customerSearchLoading = ref(false)
async function searchCustomers(query: string) {
  if (!query) return
  customerSearchLoading.value = true
  try {
    const res = await customerApi.searchCustomers({ keyword: query, pageSize: 20 })
    customerOptions.value = res.items || []
  } catch {
    customerOptions.value = []
  } finally {
    customerSearchLoading.value = false
  }
}

// ── 提交状态 ──
const submitting = ref(false)
const createSuccess = ref(false)
const createError = ref('')
const createdRFQCode = ref('')
const createdRFQId = ref('')

// ── Excel 列映射说明 ──
const columnMapping = [
  { col: 'A', field: '客户物料型号', required: false, example: 'ABC-123', note: '客户自己的物料编号' },
  { col: 'B', field: '物料型号(MPN)', required: true, example: 'STM32F103C8T6', note: '标准物料型号，必填' },
  { col: 'C', field: '客户品牌', required: false, example: 'ST', note: '客户指定品牌' },
  { col: 'D', field: '品牌', required: false, example: 'STMicroelectronics', note: '我方品牌' },
  { col: 'E', field: '数量', required: true, example: '1000', note: '需求数量，必填，正整数' },
  { col: 'F', field: '目标价', required: false, example: '2.5', note: '目标单价' },
  { col: 'G', field: '货币', required: false, example: 'CNY', note: 'CNY/USD/EUR/HKD，默认 CNY' },
  { col: 'H', field: '最小包装量', required: false, example: '100', note: '最小包装数量' },
  { col: 'I', field: '最小起订量', required: false, example: '500', note: 'MOQ' },
  { col: 'J', field: '可替代料', required: false, example: 'STM32F103CBT6', note: '多个用逗号分隔' },
  { col: 'K', field: '备注', required: false, example: '需2年内产品', note: '行备注' },
]

// ── 解析 Excel ──
function parseExcel(file: File): Promise<(CreateRFQItemRequest & { _error?: string })[]> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = (e) => {
      try {
        const data = new Uint8Array(e.target!.result as ArrayBuffer)
        const wb = XLSX.read(data, { type: 'array' })
        const ws = wb.Sheets[wb.SheetNames[0]]
        const rows: any[][] = XLSX.utils.sheet_to_json(ws, { header: 1, defval: '' })

        // 跳过第一行（表头）
        const dataRows = rows.slice(1)
        skippedRows.value = 0
        let lineNo = 1
        const items: (CreateRFQItemRequest & { _error?: string })[] = []

        for (const row of dataRows) {
          // 跳过完全空行
          const isEmptyRow = row.every((cell: any) => cell === '' || cell == null)
          if (isEmptyRow) { skippedRows.value++; continue }

          const customerMaterialModel = String(row[0] || '').trim()
          const materialModel = String(row[1] || '').trim()
          const customerBrand = String(row[2] || '').trim()
          const brand = String(row[3] || '').trim()
          const quantityRaw = row[4]
          const targetPriceRaw = row[5]
          const currency = String(row[6] || 'CNY').trim().toUpperCase() || 'CNY'
          const minPackageQty = row[7] ? Number(row[7]) : undefined
          const moq = row[8] ? Number(row[8]) : undefined
          const alternatives = String(row[9] || '').trim()
          const remark = String(row[10] || '').trim()

          // 验证必填字段
          let error = ''
          if (!materialModel) error = '缺少MPN'
          const quantity = Number(quantityRaw)
          if (!error && (isNaN(quantity) || quantity <= 0)) error = '数量无效'

          const item: CreateRFQItemRequest & { _error?: string } = {
            lineNo: lineNo++,
            customerMaterialModel: customerMaterialModel || undefined,
            materialModel,
            customerMpn: customerMaterialModel || undefined,
            mpn: materialModel,
            customerBrand: customerBrand || undefined,
            brand: brand || undefined,
            quantity: isNaN(quantity) ? 0 : quantity,
            targetPrice: targetPriceRaw !== '' && targetPriceRaw != null ? Number(targetPriceRaw) : undefined,
            currency: currency || 'CNY',
            minPackageQty,
            moq,
            minOrderQty: moq,
            alternatives: alternatives || undefined,
            alternativeMaterials: alternatives || undefined,
            remark: remark || undefined,
            _key: lineNo,
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

// ── 进入预览步骤 ──
async function goToPreview() {
  if (!rawFile.value) return
  try {
    previewItems.value = await parseExcel(rawFile.value)
    if (previewItems.value.length === 0) {
      ElMessage.warning('Excel 中没有有效数据行，请检查文件内容')
      return
    }
    step.value = 2
  } catch (err) {
    ElMessage.error('Excel 解析失败，请检查文件格式')
  }
}

// ── 行样式 ──
function getRowClass({ row }: { row: any }) {
  return row._error ? 'row-error' : ''
}

// ── 提交创建 ──
async function handleSubmit() {
  if (!rfqBaseForm.value.customerId) {
    ElMessage.warning('请选择客户')
    return
  }
  if (validItems.value.length === 0) {
    ElMessage.warning('没有有效的明细行')
    return
  }
  submitting.value = true
  try {
    const payload: CreateRFQRequest = {
      customerId: rfqBaseForm.value.customerId,
      rfqDate: rfqBaseForm.value.rfqDate,
      source: rfqBaseForm.value.source as any,
      rfqType: rfqBaseForm.value.rfqType as any,
      remark: rfqBaseForm.value.remark || undefined,
      items: validItems.value.map(item => {
        const { _error, _key, ...rest } = item as any
        return rest
      }),
    }
    const created = await rfqApi.createRFQ(payload)
    createdRFQCode.value = (created as any).rfqCode || (created as any).id || '已创建'
    createdRFQId.value = (created as any).id || ''
    createSuccess.value = true
    step.value = 3
    emit('created', createdRFQId.value)
  } catch (err: any) {
    createError.value = err?.message || '创建 RFQ 失败，请稍后重试'
    createSuccess.value = false
    step.value = 3
  } finally {
    submitting.value = false
  }
}

// ── 查看创建的 RFQ ──
function handleViewCreated() {
  if (createdRFQId.value) {
    router.push({ name: 'RFQDetail', params: { id: createdRFQId.value } })
  }
  visible.value = false
}

// ── 关闭 ──
function handleClose() {
  visible.value = false
  // 重置状态
  step.value = 1
  uploadedFileName.value = ''
  rawFile.value = null
  previewItems.value = []
  skippedRows.value = 0
  createSuccess.value = false
  createError.value = ''
  createdRFQCode.value = ''
  createdRFQId.value = ''
  rfqBaseForm.value = {
    customerId: '',
    rfqDate: new Date().toISOString().slice(0, 10),
    source: 5,
    rfqType: undefined,
    remark: '',
  }
}

// ── 下载模板 ──
function downloadTemplate() {
  // 构建模板数据
  const headers = [
    '客户物料型号', '物料型号(MPN)*', '客户品牌', '品牌',
    '数量*', '目标价', '货币(CNY/USD/EUR/HKD)',
    '最小包装量', '最小起订量(MOQ)', '可替代料(逗号分隔)', '备注'
  ]
  const exampleRow = [
    'ABC-001', 'STM32F103C8T6', 'ST', 'STMicroelectronics',
    '1000', '2.5', 'CNY',
    '100', '500', 'STM32F103CBT6', '需2年内产品'
  ]
  const ws = XLSX.utils.aoa_to_sheet([headers, exampleRow])

  // 设置列宽
  ws['!cols'] = headers.map(() => ({ wch: 20 }))

  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'RFQ明细')
  XLSX.writeFile(wb, 'RFQ导入模板.xlsx')
}
</script>

<style lang="scss">
.import-rfq-dialog {
  .el-dialog__body { padding: 16px 20px; }

  // 错误行高亮
  .el-table .row-error td {
    background-color: rgba(245, 108, 108, 0.08) !important;
  }
}
</style>

<style lang="scss" scoped>
.step-upload {
  .upload-tips { margin-bottom: 16px; }

  .excel-upload-area {
    width: 100%;
    :deep(.el-upload) { width: 100%; }
    :deep(.el-upload-dragger) {
      width: 100%;
      background: rgba(0, 212, 255, 0.03);
      border-color: rgba(0, 212, 255, 0.2);
      &:hover { border-color: rgba(0, 212, 255, 0.5); }
    }
  }

  .uploaded-file-info {
    display: flex; align-items: center; gap: 8px;
    margin: 10px 0;
    padding: 8px 12px;
    background: rgba(103, 194, 58, 0.08);
    border: 1px solid rgba(103, 194, 58, 0.2);
    border-radius: 4px;
    font-size: 13px;
  }

  .field-mapping-table {
    margin-top: 16px;
    .mapping-title {
      font-size: 13px; font-weight: 600;
      color: rgba(0, 212, 255, 0.7);
      margin-bottom: 8px;
    }
  }
}

.step-preview {
  .rfq-base-form {
    background: rgba(0, 212, 255, 0.03);
    border: 1px solid rgba(0, 212, 255, 0.1);
    border-radius: 6px;
    padding: 12px 16px;
    margin-bottom: 14px;

    .section-label {
      font-size: 12px; font-weight: 700;
      color: rgba(0, 212, 255, 0.6);
      letter-spacing: 1px;
      margin-bottom: 10px;
      text-transform: uppercase;
    }
  }

  .parse-stats { margin-bottom: 10px; }
}

.step-result {
  .result-success, .result-error { padding: 20px 0; }
}

.dialog-footer {
  display: flex; justify-content: flex-end; gap: 10px;
}
</style>
