<template>
  <div class="bom-create-page">
    <!-- 面包屑 -->
    <div class="breadcrumb-bar">
      <el-button link @click="router.push({ name: 'BOMList' })">
        <el-icon><ArrowLeft /></el-icon>返回列表
      </el-button>
      <span class="sep">/</span>
      <span class="crumb-link" @click="router.push({ name: 'BOMList' })">BOM 快速报价</span>
      <span class="sep">/</span>
      <span class="crumb-current">新建 BOM</span>
    </div>

    <div class="create-layout">
      <!-- 左侧：BOM 主体信息 -->
      <div class="left-panel">
        <div class="panel-card">
          <div class="panel-title">
            <span class="dot"></span>BOM 基础信息
          </div>
          <el-form ref="formRef" :model="formData" :rules="formRules" label-position="top" class="bom-form">
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item label="客户" prop="customerId">
                  <el-select
                    v-model="formData.customerId"
                    filterable
                    remote
                    :remote-method="searchCustomers"
                    :loading="customerLoading"
                    placeholder="搜索客户名称..."
                    class="w-full"
                    @change="handleCustomerChange"
                  >
                    <el-option
                      v-for="c in customerOptions"
                      :key="c.id"
                      :label="c.name"
                      :value="c.id"
                    />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="需求日期">
                  <el-date-picker
                    v-model="formData.bomDate"
                    type="date"
                    value-format="YYYY-MM-DD"
                    placeholder="选择日期"
                    class="w-full"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="需求类型">
                  <el-select v-model="formData.bomType" placeholder="请选择" class="w-full">
                    <el-option label="现货" :value="1" />
                    <el-option label="期货" :value="2" />
                    <el-option label="样品" :value="3" />
                    <el-option label="批量" :value="4" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="来源">
                  <el-select v-model="formData.source" placeholder="请选择" class="w-full">
                    <el-option label="线下" :value="1" />
                    <el-option label="线上" :value="2" />
                    <el-option label="邮件" :value="3" />
                    <el-option label="电话" :value="4" />
                    <el-option label="导入" :value="5" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="行业">
                  <el-input v-model="formData.industry" placeholder="请输入行业" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="结算货币">
                  <el-select v-model="formData.currency" placeholder="CNY" class="w-full">
                    <el-option label="CNY（人民币）" value="CNY" />
                    <el-option label="USD（美元）" value="USD" />
                    <el-option label="EUR（欧元）" value="EUR" />
                    <el-option label="HKD（港币）" value="HKD" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="24">
                <el-form-item label="备注">
                  <el-input
                    v-model="formData.remark"
                    type="textarea"
                    :rows="3"
                    placeholder="请输入备注信息"
                  />
                </el-form-item>
              </el-col>
            </el-row>
          </el-form>
        </div>
      </div>

      <!-- 右侧：Excel 导入明细 -->
      <div class="right-panel">
        <div class="panel-card">
          <div class="panel-title">
            <span class="dot"></span>BOM 明细（Excel 导入）
            <div class="panel-title-actions">
              <el-button link size="small" @click="downloadTemplate">
                <el-icon><Download /></el-icon>下载模板
              </el-button>
            </div>
          </div>

          <!-- 上传区域 -->
          <div v-if="!parsedItems.length" class="upload-area">
            <div
              class="drop-zone"
              :class="{ 'drag-over': isDragging }"
              @dragover.prevent="isDragging = true"
              @dragleave="isDragging = false"
              @drop.prevent="handleDrop"
              @click="triggerFileInput"
            >
              <div class="drop-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/>
                  <polyline points="17 8 12 3 7 8"/>
                  <line x1="12" y1="3" x2="12" y2="15"/>
                </svg>
              </div>
              <div class="drop-text">拖拽 Excel 文件到此处，或 <em>点击上传</em></div>
              <div class="drop-tip">支持 .xlsx / .xls 格式</div>
            </div>
            <input ref="fileInputRef" type="file" accept=".xlsx,.xls" class="hidden-input" @change="handleFileChange" />

            <!-- 列说明 -->
            <div class="column-guide">
              <div class="guide-title">Excel 列对应关系</div>
              <table class="guide-table">
                <thead><tr><th>列</th><th>字段</th><th>必填</th><th>示例</th></tr></thead>
                <tbody>
                  <tr><td>A</td><td>客户物料型号</td><td>—</td><td>ABC-001</td></tr>
                  <tr><td>B</td><td>物料型号(MPN)</td><td><span class="req">必填</span></td><td>STM32F103C8T6</td></tr>
                  <tr><td>C</td><td>客户品牌</td><td>—</td><td>ST</td></tr>
                  <tr><td>D</td><td>品牌</td><td>—</td><td>STMicro</td></tr>
                  <tr><td>E</td><td>数量</td><td><span class="req">必填</span></td><td>1000</td></tr>
                  <tr><td>F</td><td>目标价</td><td>—</td><td>2.50</td></tr>
                  <tr><td>G</td><td>货币</td><td>—</td><td>CNY</td></tr>
                  <tr><td>H</td><td>最小包装量</td><td>—</td><td>100</td></tr>
                  <tr><td>I</td><td>最小起订量</td><td>—</td><td>500</td></tr>
                  <tr><td>J</td><td>可替代料</td><td>—</td><td>GD32F103</td></tr>
                  <tr><td>K</td><td>备注</td><td>—</td><td>需2年内</td></tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- 解析结果预览 -->
          <div v-else class="preview-area">
            <div class="preview-stats">
              <span class="stat-tag success">✓ 有效 {{ validItems.length }} 行</span>
              <span v-if="errorItems.length" class="stat-tag error">✗ 错误 {{ errorItems.length }} 行</span>
              <span class="stat-tag info">共 {{ parsedItems.length }} 行</span>
              <el-button link size="small" class="reupload-btn" @click="clearParsed">
                <el-icon><RefreshRight /></el-icon>重新上传
              </el-button>
            </div>
            <el-table :data="parsedItems" class="preview-table" max-height="360" size="small">
              <el-table-column label="#" type="index" width="40" />
              <el-table-column label="客户物料型号" prop="customerMpn" min-width="130" show-overflow-tooltip />
              <el-table-column label="MPN" prop="mpn" min-width="140" show-overflow-tooltip>
                <template #default="{ row }">
                  <span :class="{ 'text-error': row._hasError }">{{ row.mpn || '—' }}</span>
                </template>
              </el-table-column>
              <el-table-column label="品牌" prop="brand" width="90" show-overflow-tooltip />
              <el-table-column label="数量" prop="quantity" width="70" align="right" />
              <el-table-column label="目标价" prop="targetPrice" width="80" align="right">
                <template #default="{ row }">{{ row.targetPrice ?? '—' }}</template>
              </el-table-column>
              <el-table-column label="货币" prop="currency" width="60" />
              <el-table-column label="状态" width="90">
                <template #default="{ row }">
                  <el-tag size="small" :type="row._hasError ? 'danger' : 'success'">
                    {{ row._hasError ? row._errorMsg : '正常' }}
                  </el-tag>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </div>
      </div>
    </div>

    <!-- 底部操作 -->
    <div class="footer-bar">
      <el-button @click="router.push({ name: 'BOMList' })">取消</el-button>
      <el-button
        type="primary"
        :loading="submitting"
        :disabled="!canSubmit"
        @click="handleSubmit"
      >
        确认创建 BOM
        <span v-if="validItems.length">（{{ validItems.length }} 条明细）</span>
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ArrowLeft, Download, RefreshRight } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import * as XLSX from 'xlsx'
import { bomApi } from '@/api/bom'
import type { CreateBOMItemRequest } from '@/types/bom'

const router = useRouter()

// ── 表单数据 ──
const formRef = ref()
const formData = ref({
  customerId: '',
  bomDate: new Date().toISOString().slice(0, 10),
  bomType: 1,
  source: 5,
  industry: '',
  currency: 'CNY',
  remark: '',
})
const formRules = {
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }],
}

// ── 客户搜索 ──
const customerLoading = ref(false)
const customerOptions = ref<{ id: string; name: string }[]>([])
const searchCustomers = async (query: string) => {
  if (!query) return
  customerLoading.value = true
  try {
    const { customerApi } = await import('@/api/customer')
    const res = await customerApi.searchCustomers({ keyword: query, pageSize: 20 })
    customerOptions.value = (res.items || []).map((c: any) => ({ id: c.id, name: c.name }))
  } catch {
    customerOptions.value = []
  } finally {
    customerLoading.value = false
  }
}
const handleCustomerChange = () => {}

// ── Excel 解析 ──
const fileInputRef = ref<HTMLInputElement>()
const isDragging = ref(false)
const parsedItems = ref<CreateBOMItemRequest[]>([])
const submitting = ref(false)

const validItems = computed(() => parsedItems.value.filter(i => !i._hasError))
const errorItems = computed(() => parsedItems.value.filter(i => i._hasError))
const canSubmit = computed(() => formData.value.customerId && validItems.value.length > 0)

const triggerFileInput = () => fileInputRef.value?.click()

const handleDrop = (e: DragEvent) => {
  isDragging.value = false
  const file = e.dataTransfer?.files?.[0]
  if (file) parseExcel(file)
}
const handleFileChange = (e: Event) => {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (file) parseExcel(file)
}

const parseExcel = (file: File) => {
  const reader = new FileReader()
  reader.onload = (e) => {
    try {
      const data = new Uint8Array(e.target!.result as ArrayBuffer)
      const wb = XLSX.read(data, { type: 'array' })
      const ws = wb.Sheets[wb.SheetNames[0]]
      const rows: any[][] = XLSX.utils.sheet_to_json(ws, { header: 1, defval: '' })
      // 跳过第一行（表头）
      const dataRows = rows.slice(1).filter(r => r.some(cell => cell !== ''))
      parsedItems.value = dataRows.map((r, idx) => {
        const mpn = String(r[1] || '').trim()
        const qty = Number(r[4])
        const hasError = !mpn || isNaN(qty) || qty <= 0
        return {
          lineNo: idx + 1,
          customerMpn: String(r[0] || '').trim() || undefined,
          mpn: mpn || undefined,
          customerBrand: String(r[2] || '').trim() || undefined,
          brand: String(r[3] || '').trim() || undefined,
          quantity: isNaN(qty) ? 0 : qty,
          targetPrice: r[5] !== '' ? Number(r[5]) : undefined,
          currency: String(r[6] || '').trim() || undefined,
          minPackageQty: r[7] !== '' ? Number(r[7]) : undefined,
          moq: r[8] !== '' ? Number(r[8]) : undefined,
          alternatives: String(r[9] || '').trim() || undefined,
          remark: String(r[10] || '').trim() || undefined,
          _key: idx,
          _hasError: hasError,
          _errorMsg: !mpn ? '缺少MPN' : (isNaN(qty) || qty <= 0) ? '数量无效' : '',
        }
      })
      if (parsedItems.value.length === 0) {
        ElMessage.warning('Excel 中未找到有效数据行')
      }
    } catch {
      ElMessage.error('Excel 解析失败，请检查文件格式')
    }
  }
  reader.readAsArrayBuffer(file)
}

const clearParsed = () => {
  parsedItems.value = []
  if (fileInputRef.value) fileInputRef.value.value = ''
}

// ── 下载模板 ──
const downloadTemplate = () => {
  const headers = ['客户物料型号', '物料型号(MPN)*', '客户品牌', '品牌', '数量*', '目标价', '货币(CNY/USD/EUR/HKD)', '最小包装量', '最小起订量(MOQ)', '可替代料(逗号分隔)', '备注']
  const example = ['ABC-001', 'STM32F103C8T6', 'ST', 'STMicroelectronics', 1000, 2.5, 'CNY', 100, 500, 'STM32F103CBT6', '需2年内产品']
  const wb = XLSX.utils.book_new()
  const ws = XLSX.utils.aoa_to_sheet([headers, example])
  ws['!cols'] = headers.map(() => ({ wch: 20 }))
  XLSX.utils.book_append_sheet(wb, ws, 'BOM明细')
  XLSX.writeFile(wb, 'BOM导入模板.xlsx')
}

// ── 提交 ──
const handleSubmit = async () => {
  await formRef.value?.validate()
  if (!validItems.value.length) {
    ElMessage.warning('请上传包含有效数据的 Excel 文件')
    return
  }
  submitting.value = true
  try {
    const bom = await bomApi.createBOM({
      customerId: formData.value.customerId,
      bomDate: formData.value.bomDate,
      bomType: formData.value.bomType,
      source: formData.value.source,
      industry: formData.value.industry || undefined,
      currency: formData.value.currency || undefined,
      remark: formData.value.remark || undefined,
      items: validItems.value,
    })
    ElMessage.success(`BOM 创建成功：${bom.bomCode}`)
    router.push({ name: 'BOMDetail', params: { id: bom.id } })
  } catch {
    ElMessage.error('创建失败，请稍后重试')
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped lang="scss">
.bom-create-page {
  padding: 20px;
  min-height: 100%;
}

/* ── 面包屑 ── */
.breadcrumb-bar {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 18px;
  font-size: 13px;
  .sep { color: #334; }
  .crumb-link { color: #00d4ff; cursor: pointer; &:hover { text-decoration: underline; } }
  .crumb-current { color: #8aa0b8; }
}

/* ── 布局 ── */
.create-layout {
  display: grid;
  grid-template-columns: 380px 1fr;
  gap: 20px;
  margin-bottom: 20px;
}

/* ── 面板卡片 ── */
.panel-card {
  background: rgba(0, 20, 45, 0.8);
  border: 1px solid rgba(0, 212, 255, 0.15);
  border-radius: 10px;
  padding: 20px;
  height: 100%;
}
.panel-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 700;
  color: #e0f0ff;
  margin-bottom: 18px;
  .dot {
    width: 4px; height: 16px;
    background: linear-gradient(180deg, #00d4ff, #0066cc);
    border-radius: 2px;
    flex-shrink: 0;
  }
  .panel-title-actions { margin-left: auto; }
}

/* ── 表单 ── */
.bom-form {
  :deep(.el-form-item__label) { color: #8aa0b8; font-size: 12px; padding-bottom: 4px; }
  :deep(.el-input__wrapper), :deep(.el-select .el-input__wrapper), :deep(.el-textarea__inner) {
    background: rgba(255, 255, 255, 0.04);
    border-color: rgba(255, 255, 255, 0.12);
    color: #c8d8e8;
  }
}
.w-full { width: 100%; }

/* ── 上传区域 ── */
.drop-zone {
  border: 2px dashed rgba(0, 212, 255, 0.25);
  border-radius: 8px;
  padding: 32px 20px;
  text-align: center;
  cursor: pointer;
  transition: all 0.2s;
  background: rgba(0, 212, 255, 0.02);
  &:hover, &.drag-over {
    border-color: rgba(0, 212, 255, 0.5);
    background: rgba(0, 212, 255, 0.05);
  }
  .drop-icon {
    svg { width: 40px; height: 40px; color: rgba(0, 212, 255, 0.4); margin-bottom: 10px; }
  }
  .drop-text { font-size: 13px; color: #8aa0b8; em { color: #00d4ff; font-style: normal; } }
  .drop-tip { font-size: 11px; color: #445; margin-top: 4px; }
}
.hidden-input { display: none; }

/* ── 列说明 ── */
.column-guide {
  margin-top: 16px;
  .guide-title { font-size: 11px; font-weight: 700; color: rgba(0, 212, 255, 0.5); letter-spacing: 1px; text-transform: uppercase; margin-bottom: 6px; }
}
.guide-table {
  width: 100%; border-collapse: collapse; font-size: 11px;
  th { background: rgba(0, 212, 255, 0.06); color: rgba(0, 212, 255, 0.6); padding: 4px 8px; border: 1px solid rgba(0, 212, 255, 0.08); font-weight: 600; }
  td { padding: 4px 8px; border: 1px solid rgba(255, 255, 255, 0.04); color: #6a7f94; }
  .req { display: inline-block; padding: 1px 5px; border-radius: 3px; background: rgba(245, 108, 108, 0.15); color: #f56c6c; font-size: 10px; }
}

/* ── 预览 ── */
.preview-stats {
  display: flex; align-items: center; gap: 8px; margin-bottom: 10px; flex-wrap: wrap;
  .stat-tag {
    display: inline-flex; align-items: center; padding: 2px 10px; border-radius: 10px; font-size: 11px;
    &.success { background: rgba(39, 174, 96, 0.12); color: #27ae60; border: 1px solid rgba(39, 174, 96, 0.25); }
    &.error { background: rgba(245, 108, 108, 0.12); color: #f56c6c; border: 1px solid rgba(245, 108, 108, 0.25); }
    &.info { background: rgba(0, 212, 255, 0.08); color: #00d4ff; border: 1px solid rgba(0, 212, 255, 0.2); }
  }
  .reupload-btn { margin-left: auto; color: #00d4ff; }
}
.text-error { color: #f56c6c; }

:deep(.preview-table) {
  background: transparent;
  --el-table-border-color: rgba(0, 212, 255, 0.06);
  --el-table-header-bg-color: rgba(0, 212, 255, 0.05);
  --el-table-tr-bg-color: transparent;
  --el-table-row-hover-bg-color: rgba(0, 212, 255, 0.03);
  color: #c8d8e8;
  font-size: 12px;
  .el-table__header th { color: rgba(0, 212, 255, 0.6); }
}

/* ── 底部操作 ── */
.footer-bar {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 20px;
  background: rgba(0, 20, 45, 0.6);
  border: 1px solid rgba(0, 212, 255, 0.1);
  border-radius: 8px;
}
</style>
