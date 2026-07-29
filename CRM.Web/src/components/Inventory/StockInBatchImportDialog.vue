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
        当前入库明细编号：<strong>{{ stockInItemCode || '—' }}</strong>。请先下载模板，按列填写后选择 Excel 导入；第 1 行为表头，从第 2 行起为数据。系统将自动生成批次全局编号（PC-xxxxxxxx）。单次最多约 5 万行（含 SN）。正式环境请删除或覆盖模板中的示例行后再导入。
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
          <span>本次将导入批次</span>
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
import { stockInBatchApi, type StockInBatchImportRow } from '@/api/stockInBatch'

const props = defineProps<{
  modelValue: boolean
  stockInId: string
  stockInItemId: string
  stockInItemCode?: string | null
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
const parsedRows = ref<StockInBatchImportRow[]>([])
const submitting = ref(false)

const previewReady = computed(
  () => parsedRows.value.length > 0 && parseErrors.value.length === 0
)

const canSubmit = computed(() => previewReady.value && !submitting.value)

const rowCount = computed(() => parsedRows.value.length)

const stockInItemCode = computed(() => (props.stockInItemCode ?? '').trim() || '')

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

function isRowEmpty(row: StockInBatchImportRow): boolean {
  const s = (v: string | undefined | null) => !(v && String(v).trim())
  return (
    s(row.batchDimension) &&
    s(row.batchUnit) &&
    s(row.unitNo) &&
    row.batchQty <= 0 &&
    s(row.dc) &&
    s(row.packageOrigin) &&
    s(row.waferOrigin) &&
    s(row.lot) &&
    s(row.serialNumber) &&
    s(row.firmwareVersion) &&
    s(row.partCode) &&
    s(row.remark)
  )
}

function downloadTemplate() {
  const headers = [
    '批次维度',
    '批次记录单位',
    '单位编号',
    '批次数量',
    '批次DC',
    '封装产地',
    '晶圆产地',
    'LOT',
    'SN',
    '固件版本号',
    'PARTCODE',
    '备注'
  ]
  const ws = XLSX.utils.aoa_to_sheet([
    headers,
    [
      'LOT',
      'PCS',
      '001',
      '100',
      '2540',
      '马来西亚',
      '台湾',
      'LOT20260101',
      '',
      'v1.0.0',
      'PC-ABC',
      '可删除本行后填写真实数据'
    ]
  ])
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, '批次')
  XLSX.writeFile(wb, '入库批次录入模板.xlsx')
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
      const out: StockInBatchImportRow[] = []

      for (let i = 0; i < rows.length; i++) {
        const row = rows[i]
        const excelRow = i + 2

        const batchDimension = getCell(row, '批次维度')
        const batchUnit = getCell(row, '批次记录单位')
        const unitNo = getCell(row, '单位编号')
        const batchQtyRaw = getCell(row, '批次数量')
        const dc = getCell(row, '批次DC', 'DC')
        const packageOrigin = getCell(row, '封装产地')
        const waferOrigin = getCell(row, '晶圆产地')
        const lot = getCell(row, 'LOT')
        const serialNumber = getCell(row, 'SN', 'SN号')
        const firmwareVersion = getCell(row, '固件版本号')
        const partCode = getCell(row, 'PARTCODE')
        const remark = getCell(row, '备注')

        const qtyParsed = parsePositiveInt(batchQtyRaw, excelRow, '批次数量')
        if (!qtyParsed.ok) {
          errors.push(qtyParsed.msg)
          continue
        }

        const rec: StockInBatchImportRow = {
          batchDimension: batchDimension || null,
          batchUnit: batchUnit || null,
          unitNo: unitNo || null,
          batchQty: qtyParsed.v,
          dc: dc || null,
          packageOrigin: packageOrigin || null,
          waferOrigin: waferOrigin || null,
          lot: lot || null,
          serialNumber: serialNumber || null,
          firmwareVersion: firmwareVersion || null,
          partCode: partCode || null,
          remark: remark || null
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
          '未解析到有效数据行：请确认第 1 行为表头、从第 2 行起填写，且批次数量大于 0。'
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
  const sid = (props.stockInId ?? '').trim()
  const iid = (props.stockInItemId ?? '').trim()
  if (!sid || !iid) {
    ElNotification.error({ title: '无法导入', message: '缺少入库单或明细标识' })
    return
  }
  try {
    await ElMessageBox.confirm(
      `本次将导入 ${parsedRows.value.length} 条批次记录，并关联到当前入库明细。确认提交吗？`,
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
    const result = await stockInBatchApi.importRows({
      stockInId: sid,
      stockInItemId: iid,
      rows: parsedRows.value
    })
    const count = result.importedCount ?? parsedRows.value.length
    const nos = result.globalBatchNos ?? []
    let msg = `成功写入 ${count} 条`
    if (nos.length > 0 && nos.length <= 8) {
      msg += `。编号：${nos.join('、')}`
    } else if (nos.length > 8) {
      msg += `。编号：${nos.slice(0, 5).join('、')} 等 ${nos.length} 条`
    }
    ElNotification.success({
      title: '导入完成',
      message: msg
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
    margin-bottom: 6px;
    &:last-child {
      margin-bottom: 0;
    }
  }

  strong {
    font-size: 18px;
    color: $cyan-primary;
  }
}
</style>
