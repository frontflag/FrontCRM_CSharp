<template>
  <el-dialog
    v-model="visibleInner"
    :title="t('stockInBatchList.writeOff.dialogTitle')"
    width="560px"
    destroy-on-close
    class="customer-import-dialog"
    @closed="onClosed"
  >
    <template v-if="phase === 'edit'">
      <div class="import-body">
        <p class="hint">
          {{ t('stockInBatchList.writeOff.hint') }}
        </p>
        <div class="actions-row">
          <button type="button" class="btn-template" @click="downloadTemplate">
            {{ t('stockInBatchList.writeOff.downloadTemplate') }}
          </button>
          <label class="btn-upload">
            <input
              ref="fileInputRef"
              type="file"
              accept=".xlsx,.xls"
              class="file-input"
              @change="onFileChange"
            />
            {{ t('stockInBatchList.writeOff.selectFile') }}
          </label>
        </div>

        <div v-if="fileName" class="file-name">{{ t('stockInBatchList.writeOff.selectedFile', { name: fileName }) }}</div>

        <div v-if="parseErrors.length" class="error-box">
          <div class="error-title">{{ t('stockInBatchList.writeOff.parseErrorsTitle') }}</div>
          <ul>
            <li v-for="(e, i) in parseErrors" :key="i">{{ e }}</li>
          </ul>
        </div>

        <div v-if="previewReady" class="preview-box">
          <div class="preview-row">
            <span>{{ t('stockInBatchList.writeOff.previewRows') }}</span>
            <strong>{{ rowCount }}</strong>
            <span>{{ t('stockInBatchList.writeOff.previewRowsUnit') }}</span>
          </div>
        </div>
      </div>
    </template>

    <template v-else>
      <div class="import-body validation-block">
        <div class="error-title-main">{{ t('stockInBatchList.writeOff.invalidTitle') }}</div>
        <p class="hint hint--tight">{{ t('stockInBatchList.writeOff.invalidHint') }}</p>

        <div v-if="blockedInvalidLots.length" class="copy-section">
          <div class="copy-label">{{ t('stockInBatchList.writeOff.invalidLotsLabel') }}</div>
          <el-input type="textarea" :rows="4" readonly :model-value="lotsText" class="copy-textarea" />
        </div>
        <div v-if="blockedInvalidSns.length" class="copy-section">
          <div class="copy-label">{{ t('stockInBatchList.writeOff.invalidSnsLabel') }}</div>
          <el-input type="textarea" :rows="4" readonly :model-value="snsText" class="copy-textarea" />
        </div>

        <div class="validation-actions">
          <el-button @click="copyAllInvalid">{{ t('stockInBatchList.writeOff.copyAll') }}</el-button>
          <el-button type="primary" @click="closeAfterValidationError">{{ t('stockInBatchList.writeOff.ok') }}</el-button>
        </div>
      </div>
    </template>

    <template #footer>
      <template v-if="phase === 'edit'">
        <el-button @click="visibleInner = false">{{ t('stockInBatchList.writeOff.cancel') }}</el-button>
        <el-button
          type="primary"
          :disabled="!canSubmit"
          :loading="submitting"
          @click="confirmAndSubmit"
        >
          {{ t('stockInBatchList.writeOff.confirm') }}
        </el-button>
      </template>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import * as XLSX from 'xlsx'
import { useI18n } from 'vue-i18n'
import { ElMessageBox, ElNotification } from 'element-plus'
import {
  stockInBatchApi,
  type StockInBatchWriteOffRow,
  type StockInBatchWriteOffResultDto
} from '@/api/stockInBatch'

const { t } = useI18n()

const props = defineProps<{
  modelValue: boolean
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void
  (e: 'success'): void
}>()

const visibleInner = computed({
  get: () => props.modelValue,
  set: (v: boolean) => emit('update:modelValue', v)
})

type Phase = 'edit' | 'validation-error'
const phase = ref<Phase>('edit')
const fileInputRef = ref<HTMLInputElement | null>(null)
const fileName = ref('')
const parseErrors = ref<string[]>([])
const parsedRows = ref<StockInBatchWriteOffRow[]>([])
const submitting = ref(false)
const blockedInvalidLots = ref<string[]>([])
const blockedInvalidSns = ref<string[]>([])

const previewReady = computed(
  () => parsedRows.value.length > 0 && parseErrors.value.length === 0
)

const canSubmit = computed(() => previewReady.value && !submitting.value)

const rowCount = computed(() => parsedRows.value.length)

const lotsText = computed(() => blockedInvalidLots.value.join('\n'))
const snsText = computed(() => blockedInvalidSns.value.join('\n'))

const allInvalidCopyText = computed(() => {
  const parts: string[] = []
  if (blockedInvalidLots.value.length) {
    parts.push(`${t('stockInBatchList.writeOff.invalidLotsLabel')}\n${lotsText.value}`)
  }
  if (blockedInvalidSns.value.length) {
    parts.push(`${t('stockInBatchList.writeOff.invalidSnsLabel')}\n${snsText.value}`)
  }
  return parts.join('\n\n')
})

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    resetState()
  }
)

function resetState() {
  phase.value = 'edit'
  fileName.value = ''
  parseErrors.value = []
  parsedRows.value = []
  blockedInvalidLots.value = []
  blockedInvalidSns.value = []
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

function parseNonNegInt(
  raw: string,
  excelRow: number,
  colLabel: string
): { ok: true; v: number } | { ok: false; msg: string } {
  const s = String(raw ?? '').replace(/\s/g, '')
  if (!s) return { ok: true, v: 0 }
  if (!/^\d+$/.test(s)) {
    return {
      ok: false,
      msg: t('stockInBatchList.writeOff.parseIntError', { row: excelRow, col: colLabel, val: String(raw).trim() })
    }
  }
  return { ok: true, v: parseInt(s, 10) }
}

function isRowEmpty(r: StockInBatchWriteOffRow): boolean {
  const lot = (r.lot ?? '').trim()
  const sn = (r.serialNumber ?? '').trim()
  return !lot && !sn && r.lotWriteOffQty === 0 && r.snWriteOffQty === 0
}

function downloadTemplate() {
  const headers = ['LOT', 'LOT_核销数量', 'SN号', 'SN号_核销数量']
  const ws = XLSX.utils.aoa_to_sheet([headers, ['示例LOT001', '10', '示例SN001', '2']])
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, '核销')
  XLSX.writeFile(wb, '入库批次核销模板.xlsx')
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
      const sheetName = wb.SheetNames.find((n) => n.includes('核销')) || wb.SheetNames[0]
      if (!sheetName) {
        parseErrors.value = [t('stockInBatchList.writeOff.noSheet')]
        return
      }
      const sh = wb.Sheets[sheetName]
      const rows = XLSX.utils.sheet_to_json<Record<string, unknown>>(sh, { defval: '', raw: false })
      if (rows.length === 0) {
        parseErrors.value = [t('stockInBatchList.writeOff.emptySheet')]
        return
      }

      const errors: string[] = []
      const out: StockInBatchWriteOffRow[] = []

      for (let i = 0; i < rows.length; i++) {
        const row = rows[i]
        const excelRow = i + 2
        const lot = getCell(row, 'LOT')
        const lotQtyRaw = getCell(row, 'LOT_核销数量')
        const serialNumber = getCell(row, 'SN号')
        const snQtyRaw = getCell(row, 'SN号_核销数量')

        const lotP = parseNonNegInt(lotQtyRaw, excelRow, 'LOT_核销数量')
        if (!lotP.ok) {
          errors.push(lotP.msg)
          continue
        }
        const snP = parseNonNegInt(snQtyRaw, excelRow, 'SN号_核销数量')
        if (!snP.ok) {
          errors.push(snP.msg)
          continue
        }

        const rec: StockInBatchWriteOffRow = {
          lot: lot || null,
          lotWriteOffQty: lotP.v,
          serialNumber: serialNumber || null,
          snWriteOffQty: snP.v
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
        parseErrors.value = [t('stockInBatchList.writeOff.noDataRows')]
        parsedRows.value = []
        return
      }
      parsedRows.value = out
    } catch (err: unknown) {
      parseErrors.value = [err instanceof Error ? err.message : t('stockInBatchList.writeOff.parseFailed')]
    }
  }
  reader.readAsArrayBuffer(file)
}

async function confirmAndSubmit() {
  if (!parsedRows.value.length) return
  try {
    await ElMessageBox.confirm(t('stockInBatchList.writeOff.confirmBoxMessage', { count: parsedRows.value.length }), t('stockInBatchList.writeOff.confirmBoxTitle'), {
      type: 'warning',
      confirmButtonText: t('stockInBatchList.writeOff.confirmUpload'),
      cancelButtonText: t('stockInBatchList.writeOff.cancel')
    })
  } catch {
    return
  }

  submitting.value = true
  try {
    const res: StockInBatchWriteOffResultDto = await stockInBatchApi.writeOff({ rows: parsedRows.value })
    if (!res.validationPassed) {
      blockedInvalidLots.value = Array.isArray(res.invalidLots) ? res.invalidLots : []
      blockedInvalidSns.value = Array.isArray(res.invalidSerialNumbers) ? res.invalidSerialNumbers : []
      phase.value = 'validation-error'
      return
    }
    if (res.updatedRowCount === 0) {
      ElNotification.info({
        title: t('stockInBatchList.writeOff.successTitle'),
        message: t('stockInBatchList.writeOff.successNoRowsUpdated')
      })
    } else {
      ElNotification.success({
        title: t('stockInBatchList.writeOff.successTitle'),
        message: t('stockInBatchList.writeOff.successNotify', { n: res.updatedRowCount })
      })
    }
    visibleInner.value = false
    emit('success')
  } catch (e: unknown) {
    ElNotification.error({
      title: t('stockInBatchList.writeOff.failedTitle'),
      message: e instanceof Error ? e.message : t('stockInBatchList.writeOff.requestFailed')
    })
  } finally {
    submitting.value = false
  }
}

async function copyAllInvalid() {
  const text = allInvalidCopyText.value
  try {
    await navigator.clipboard.writeText(text)
    ElNotification.success({ title: t('stockInBatchList.writeOff.copiedTitle'), message: t('stockInBatchList.writeOff.copiedMessage') })
  } catch {
    ElNotification.warning({ title: t('stockInBatchList.writeOff.copyFailedTitle'), message: t('stockInBatchList.writeOff.copyFailedMessage') })
  }
}

function closeAfterValidationError() {
  visibleInner.value = false
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

.hint--tight {
  margin-bottom: 12px;
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

.validation-block {
  .error-title-main {
    font-size: 15px;
    font-weight: 600;
    color: #f56c6c;
    margin-bottom: 8px;
  }
}

.copy-section {
  margin-bottom: 12px;
}

.copy-label {
  font-size: 12px;
  color: $text-muted;
  margin-bottom: 6px;
}

.copy-textarea :deep(textarea) {
  font-family: ui-monospace, 'Cascadia Code', 'Segoe UI Mono', monospace;
  font-size: 12px;
}

.validation-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  justify-content: flex-end;
  margin-top: 8px;
}
</style>
