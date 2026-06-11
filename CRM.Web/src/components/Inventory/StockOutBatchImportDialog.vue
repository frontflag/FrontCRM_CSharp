<template>
  <el-dialog
    v-model="visibleInner"
    title="录入批次"
    width="560px"
    destroy-on-close
    class="customer-import-dialog"
    @closed="onClosed"
  >
    <div class="import-body">
      <p class="hint">
        当前装箱单号：<strong>{{ packingCode || '—' }}</strong>。请先下载模板，填写「批次全局唯一编号」与「批次出库数量」后导入；第 1 行为表头，从第 2 行起为数据。编号须为已录入的入库批次（如 PC-00000001）。
      </p>
      <div class="actions-row">
        <button type="button" class="btn-template" @click="downloadTemplate">下载 Excel 模板</button>
        <label class="btn-upload">
          <input
            ref="fileInputRef"
            type="file"
            accept=".xlsx,.xls"
            class="file-input"
            @change="onFileChange"
          />
          选择 Excel 文件
        </label>
      </div>

      <div v-if="fileName" class="file-name">已选：{{ fileName }}</div>

      <div v-if="parseErrors.length" class="error-box">
        <div class="error-title">解析问题</div>
        <ul>
          <li v-for="(e, i) in parseErrors" :key="i">{{ e }}</li>
        </ul>
      </div>

      <div v-if="previewReady" class="preview-box">
        <div class="preview-row">
          <span>本次将导入出库批次</span>
          <strong>{{ rowCount }}</strong>
          <span>条</span>
        </div>
      </div>
    </div>

    <template #footer>
      <el-button @click="visibleInner = false">取消</el-button>
      <el-button
        type="primary"
        :disabled="!canSubmit"
        :loading="submitting"
        @click="confirmAndSubmit"
      >
        确认导入
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import * as XLSX from 'xlsx'
import { ElMessageBox, ElNotification } from 'element-plus'
import { stockOutBatchApi, type StockOutBatchImportRow } from '@/api/stockOutBatch'

const props = defineProps<{
  modelValue: boolean
  packingId: string
  packingCode?: string | null
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void
  (e: 'success'): void
}>()

const visibleInner = computed({
  get: () => props.modelValue,
  set: (v: boolean) => emit('update:modelValue', v)
})

const fileInputRef = ref<HTMLInputElement | null>(null)
const fileName = ref('')
const parseErrors = ref<string[]>([])
const parsedRows = ref<StockOutBatchImportRow[]>([])
const submitting = ref(false)

const previewReady = computed(
  () => parsedRows.value.length > 0 && parseErrors.value.length === 0
)

const canSubmit = computed(() => previewReady.value && !submitting.value)
const rowCount = computed(() => parsedRows.value.length)
const packingCode = computed(() => (props.packingCode ?? '').trim() || '')

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    resetState()
  }
)

function resetState() {
  fileName.value = ''
  parseErrors.value = []
  parsedRows.value = []
  if (fileInputRef.value) fileInputRef.value.value = ''
}

function onClosed() {
  resetState()
}

function normalizeHeaderKey(k: string): string {
  return k.replace(/（必填）/g, '').replace(/\([^)]*\)/g, '').trim()
}

function getCell(row: Record<string, unknown>, ...candidates: string[]): string {
  const keys = Object.keys(row)
  for (const cand of candidates) {
    const hit = keys.find((k) => normalizeHeaderKey(k) === cand)
    if (hit) {
      const v = row[hit]
      if (v == null) return ''
      return String(v).trim()
    }
  }
  return ''
}

function parsePositiveInt(raw: string, excelRow: number, colLabel: string): { ok: true; v: number } | { ok: false; msg: string } {
  const s = String(raw ?? '').replace(/\s/g, '')
  if (!s) {
    return { ok: false, msg: `第 ${excelRow} 行：「${colLabel}」须为正整数` }
  }
  if (!/^\d+$/.test(s)) {
    return {
      ok: false,
      msg: `第 ${excelRow} 行：「${colLabel}」须为正整数，当前为「${String(raw).trim()}」`
    }
  }
  const n = parseInt(s, 10)
  if (n <= 0) {
    return { ok: false, msg: `第 ${excelRow} 行：「${colLabel}」须大于 0` }
  }
  return { ok: true, v: n }
}

function isRowEmpty(row: StockOutBatchImportRow): boolean {
  return !(row.globalBatchNo && String(row.globalBatchNo).trim()) && row.outQty <= 0
}

function downloadTemplate() {
  const headers = ['批次全局唯一编号', '批次出库数量']
  const ws = XLSX.utils.aoa_to_sheet([
    headers,
    ['PC-00000001', '10']
  ])
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, '批次')
  XLSX.writeFile(wb, '出库批次录入模板.xlsx')
}

function onFileChange(ev: Event) {
  const input = ev.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  fileName.value = file.name
  parseErrors.value = []
  parsedRows.value = []

  const reader = new FileReader()
  reader.onload = (e) => {
    try {
      const data = new Uint8Array(e.target?.result as ArrayBuffer)
      const wb = XLSX.read(data, { type: 'array' })
      const sheetName =
        wb.SheetNames.find((n) => n.includes('批次')) || wb.SheetNames[0]
      if (!sheetName) {
        parseErrors.value = ['工作簿中未找到工作表']
        return
      }
      const sh = wb.Sheets[sheetName]
      const rows = XLSX.utils.sheet_to_json<Record<string, unknown>>(sh, { defval: '', raw: false })
      if (rows.length === 0) {
        parseErrors.value = ['表中没有数据行']
        return
      }

      const errors: string[] = []
      const out: StockOutBatchImportRow[] = []
      const seenGlobal = new Set<string>()

      for (let i = 0; i < rows.length; i++) {
        const row = rows[i]
        const excelRow = i + 2

        const globalBatchNo = getCell(row, '批次全局唯一编号')
        const outQtyRaw = getCell(row, '批次出库数量')

        const qtyParsed = parsePositiveInt(outQtyRaw, excelRow, '批次出库数量')
        if (!qtyParsed.ok) {
          errors.push(qtyParsed.msg)
          continue
        }

        if (!globalBatchNo) {
          errors.push(`第 ${excelRow} 行：「批次全局唯一编号」不能为空`)
          continue
        }

        const globalKey = globalBatchNo.toUpperCase()
        if (seenGlobal.has(globalKey)) {
          errors.push(`第 ${excelRow} 行：批次全局唯一编号「${globalBatchNo}」在 Excel 中重复`)
          continue
        }
        seenGlobal.add(globalKey)

        const rec: StockOutBatchImportRow = {
          globalBatchNo,
          outQty: qtyParsed.v
        }

        if (isRowEmpty(rec)) continue
        out.push(rec)
      }

      parseErrors.value = errors
      if (errors.length > 0) {
        parsedRows.value = []
        return
      }
      if (out.length === 0) {
        parseErrors.value = [
          '未解析到有效数据行：请确认第 1 行为表头、从第 2 行起填写，且出库数量大于 0。'
        ]
        parsedRows.value = []
        return
      }
      parsedRows.value = out
    } catch (err: unknown) {
      parseErrors.value = [err instanceof Error ? err.message : '解析 Excel 失败']
    }
  }
  reader.readAsArrayBuffer(file)
}

async function confirmAndSubmit() {
  if (!parsedRows.value.length) return
  const pid = (props.packingId ?? '').trim()
  if (!pid) {
    ElNotification.error({ title: '无法导入', message: '缺少装箱单标识' })
    return
  }
  try {
    await ElMessageBox.confirm(
      `本次将导入 ${parsedRows.value.length} 条出库批次记录。系统将校验入库余额，确认提交吗？`,
      '确认导入',
      {
        type: 'warning',
        confirmButtonText: '确认上传',
        cancelButtonText: '取消'
      }
    )
  } catch {
    return
  }

  submitting.value = true
  try {
    const result = await stockOutBatchApi.importRows({
      packingId: pid,
      rows: parsedRows.value
    })
    ElNotification.success({
      title: '导入完成',
      message: `成功写入 ${result.importedCount} 条出库批次`
    })
    visibleInner.value = false
    emit('success')
  } catch (e: unknown) {
    ElNotification.error({
      title: '导入失败',
      message: e instanceof Error ? e.message : '请求失败'
    })
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.hint {
  font-size: 13px;
  color: $text-muted;
  line-height: 1.5;
  margin: 0 0 16px;
}

.actions-row {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
  margin-bottom: 12px;
}

.btn-template,
.btn-upload {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid $border-panel;
  background: $layer-2;
  color: $text-primary;
}

.btn-upload {
  position: relative;
  overflow: hidden;
  border-color: rgba(0, 212, 255, 0.35);
  color: $cyan-primary;
}

.file-input {
  position: absolute;
  inset: 0;
  opacity: 0;
  cursor: pointer;
}

.file-name {
  font-size: 12px;
  color: $text-muted;
  margin-bottom: 12px;
}

.error-box {
  background: rgba(255, 80, 80, 0.08);
  border: 1px solid rgba(255, 80, 80, 0.25);
  border-radius: $border-radius-md;
  padding: 10px 12px;
  margin-bottom: 12px;
  font-size: 12px;
  color: $text-secondary;

  .error-title {
    font-weight: 600;
    margin-bottom: 6px;
    color: #f56c6c;
  }

  ul {
    margin: 0;
    padding-left: 18px;
  }
}

.preview-box {
  background: rgba(0, 212, 255, 0.06);
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: $border-radius-md;
  padding: 12px 14px;
  font-size: 13px;
  color: $text-primary;

  .preview-row {
    display: flex;
    align-items: baseline;
    gap: 6px;
  }

  strong {
    font-size: 18px;
    color: $cyan-primary;
  }
}
</style>
