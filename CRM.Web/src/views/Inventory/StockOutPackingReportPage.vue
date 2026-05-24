<template>
  <div class="po-report-page">
    <div class="toolbar no-print">
      <el-button @click="router.back()">{{ t('stockOutPackingReport.back') }}</el-button>
      <div class="toolbar__sp" />
      <span v-if="ready" class="toolbar__tag">{{ variantTitle }}</span>
      <div class="toolbar__opt">
        <el-radio-group v-model="reportLang" size="small" class="toolbar__lang">
          <el-radio-button label="zh">{{ t('stockOutPackingReport.langZh') }}</el-radio-button>
          <el-radio-button label="en">{{ t('stockOutPackingReport.langEn') }}</el-radio-button>
        </el-radio-group>
      </div>
      <div class="toolbar__opt" :title="t('stockOutPackingReport.sealHint')">
        <span class="toolbar__opt-lbl">{{ t('stockOutPackingReport.showSeal') }}</span>
        <el-switch v-model="showSealOnReport" />
      </div>
      <el-button type="primary" :disabled="!ready" @click="doPrint">{{ t('stockOutPackingReport.print') }}</el-button>
    </div>

    <div v-loading="loading" class="preview-wrap">
      <div v-if="errorMsg" class="err">{{ errorMsg }}</div>
      <div v-else-if="ready" class="print-root">
        <StockOutPackingReportDocument v-bind="docBind" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { packingApi, packingDeliveryMethodLabel, packingDeliveryMethodLabelEn, packingDetailItemsToReportLines } from '@/api/packing'
import {
  pickReportRemarkLines,
  type CompanyBasicRow,
  type CompanyLogoRow,
  type CompanySealRow,
  type CompanyWarehouseRow,
  type CompanyReportInfo
} from '@/api/companyProfile'
import apiClient from '@/api/client'
import { formatDisplayDate } from '@/utils/displayDateTime'
import StockOutPackingReportDocument, {
  type StockOutPackingLineVm
} from '@/components/stockOut/StockOutPackingReportDocument.vue'
import { renderPdfBlobFirstPageToPngDataUrl } from '@/utils/pdfSealToPng'
import { getApiErrorMessage } from '@/utils/apiError'
import type { PackingReportAddressPanel, StockOutDetailDto, PackingReportLine } from '@/api/stockOut'
import {
  getPackingReportLabels,
  type InvoiceReportLang
} from '@/components/stockOut/packingReportLabels'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { normalizePackingAddrLines } from '@/utils/packingReportAddressLines'

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const PO_REPORT_PRINT_BODY_CLASS = 'po-order-report-print'
const DEFAULT_REPORT_LOGO = '/purchase-order-template/logo.svg'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const loading = ref(true)
const errorMsg = ref('')
const stockOut = ref<StockOutDetailDto | null>(null)
const packingAddresses = ref<PackingReportAddressPanel | null>(null)
const packingCode = ref<string | null>(null)
const withShipmentInspection = ref(false)
const basicDefault = ref<CompanyBasicRow | null>(null)
const warehouseRow = ref<CompanyWarehouseRow | null>(null)
const warehouseInfoAddress = ref('')
const packingDeliveryMethod = ref<number | null>(null)
const reportInfo = ref<CompanyReportInfo | null>(null)
const packingLines = ref<PackingReportLine[]>([])
const sealUrl = ref<string | null>(null)
const companyLogoObjectUrl = ref<string | null>(null)
const showSealOnReport = ref(true)
const reportLang = ref<InvoiceReportLang>('en')

const packingLabels = computed(() => getPackingReportLabels(reportLang.value))

let loadSeq = 0

const packingId = computed(() => String(route.params.packingId || ''))
const packingInspection = computed(() => String(route.params.packingInspection || ''))

const ready = computed(() => !!stockOut.value && !errorMsg.value && !loading.value)

const variantTitle = computed(() =>
  withShipmentInspection.value
    ? t('stockOutPackingReport.variantWithInspection')
    : t('stockOutPackingReport.variantWithoutInspection')
)

/** 页脚备注：中文读 Remark.CN，英文读 Remark.EN */
const packingRemarks = computed(() =>
  pickReportRemarkLines(
    reportInfo.value?.packingList,
    reportLang.value === 'zh' ? 'zh-CN' : 'en-US'
  )
)

function pickDefault<T extends { isDefault?: boolean; enabled?: boolean }>(rows: T[] | undefined | null): T | undefined {
  if (!rows?.length) return undefined
  const d = rows.find((r) => r.isDefault && r.enabled !== false)
  return d ?? rows[0]
}

function pickReportLogoRow(rows: CompanyLogoRow[] | undefined | null): CompanyLogoRow | undefined {
  if (!rows?.length) return undefined
  const hasDoc = (r: CompanyLogoRow) => {
    const id = r.documentId
    return typeof id === 'string' && id.trim().length > 0
  }
  const defWithDoc = rows.find((r) => r.isDefault && hasDoc(r))
  if (defWithDoc) return defWithDoc
  return rows.find((r) => hasDoc(r))
}

function pickReportSealRow(rows: CompanySealRow[] | undefined | null): CompanySealRow | undefined {
  if (!rows?.length) return undefined
  const hasDoc = (r: CompanySealRow) => {
    const id = r.documentId
    return typeof id === 'string' && id.trim().length > 0
  }
  const defWithDoc = rows.find((r) => r.isDefault && r.enabled !== false && hasDoc(r))
  if (defWithDoc) return defWithDoc
  const anyWithDoc = rows.find((r) => hasDoc(r))
  if (anyWithDoc) return anyWithDoc
  return rows.find((r) => r.isDefault) ?? rows[0]
}

function pickWarehouseForStockOut(
  rows: CompanyWarehouseRow[] | undefined | null,
  warehouseId: string | undefined | null
): CompanyWarehouseRow | undefined {
  if (!rows?.length) return undefined
  const id = (warehouseId || '').trim()
  if (id) {
    const hit = rows.find((r) => String(r.id) === id)
    if (hit) return hit
  }
  return pickDefault(rows)
}

function formatReportQty(n: number): string {
  return (n ?? 0).toLocaleString('zh-CN', { maximumFractionDigits: 4 })
}

function resolveShipMethodDisplay(so: StockOutDetailDto, deliveryMethod: number | null): string {
  const labelFn = reportLang.value === 'zh' ? packingDeliveryMethodLabel : packingDeliveryMethodLabelEn
  const fromPacking = labelFn(deliveryMethod)
  if (fromPacking !== '—') return fromPacking
  return (so.shipmentMethod || '').trim() || '—'
}

function cellText(v: string | null | undefined, dash = '—'): string {
  const s = (v ?? '').trim()
  return s || dash
}

/** 无值时留空（不显示 —） */
function blankIfEmpty(v: string | null | undefined): string {
  return (v ?? '').trim()
}

function mapPackingReportLines(rows: PackingReportLine[]): StockOutPackingLineVm[] {
  return rows.map((row, idx) => ({
    index: idx + 1,
    pn: cellText(row.pn),
    customerPn: blankIfEmpty(row.customerPn),
    brand: cellText(row.brand),
    customerBrand: blankIfEmpty(row.customerBrand),
    qty: formatReportQty(row.qty),
    carton: blankIfEmpty(row.carton),
    remark: blankIfEmpty(row.remark)
  }))
}

function buildFallbackReportLines(so: StockOutDetailDto): StockOutPackingLineVm[] {
  const qty = Number(so.totalQuantity) || 0
  return [
    {
      index: 1,
      pn: cellText(so.sourceCode || so.sellOrderItemCode),
      customerPn: '',
      brand: cellText(undefined),
      customerBrand: '',
      qty: formatReportQty(qty),
      carton: '',
      remark: blankIfEmpty(so.remark)
    }
  ]
}

function reportLinesForDoc(so: StockOutDetailDto): StockOutPackingLineVm[] {
  if (packingLines.value.length > 0) return mapPackingReportLines(packingLines.value)
  return buildFallbackReportLines(so)
}

function reportTotalQty(lines: StockOutPackingLineVm[]): string {
  if (packingLines.value.length > 0) {
    const sum = packingLines.value.reduce((acc, row) => acc + (Number(row.qty) || 0), 0)
    return formatReportQty(sum)
  }
  return formatReportQty(lines.reduce((acc, row) => acc + (Number(String(row.qty).replace(/,/g, '')) || 0), 0))
}

const docBind = computed(() => {
  const so = stockOut.value
  const basic = basicDefault.value
  const wqc = withShipmentInspection.value
  const L = packingLabels.value

  if (!so) {
    return {
      labels: L,
      headerCompanyName: '',
      headerWarehouseAddress: '',
      docTitle: L.docTitle,
      docSubtitle: '',
      docNo: '',
      docDate: '',
      shipmentMethodDisplay: '—',
      billToLines: normalizePackingAddrLines(undefined, undefined, L),
      shipToLines: normalizePackingAddrLines(undefined, undefined, L),
      lines: [],
      totalQty: '0',
      notes: packingRemarks.value,
      withShipmentInspection: wqc,
      sealUrl: null as string | null,
      logoUrl: companyLogoObjectUrl.value ?? DEFAULT_REPORT_LOGO,
      showSeal: showSealOnReport.value,
      signDate: ''
    }
  }

  const addr = packingAddresses.value
  const customerLine = maskSaleSensitiveFields.value ? '—' : (so.customerName || '').trim() || '—'
  const billToLines = normalizePackingAddrLines(addr?.billToLines, customerLine, L)
  const shipToLines = normalizePackingAddrLines(addr?.shipToLines, customerLine, L)

  const shipperName = (basic?.companyName || '').trim() || '—'
  const shipMethodDisplay = resolveShipMethodDisplay(so, packingDeliveryMethod.value)
  const lines = reportLinesForDoc(so)

  return {
    labels: L,
    headerCompanyName: shipperName,
    headerWarehouseAddress: warehouseInfoAddress.value.trim(),
    docTitle: L.docTitle,
    docSubtitle: '',
    docNo: (packingCode.value || '').trim() || '—',
    docDate: formatDisplayDate(so.stockOutDate) || '—',
    shipmentMethodDisplay: shipMethodDisplay,
    billToLines,
    shipToLines,
    lines,
    totalQty: reportTotalQty(lines),
    notes: packingRemarks.value,
    withShipmentInspection: wqc,
    sealUrl: sealUrl.value,
    logoUrl: companyLogoObjectUrl.value ?? DEFAULT_REPORT_LOGO,
    showSeal: showSealOnReport.value,
    signDate: formatDisplayDate(so.stockOutDate) || '—'
  }
})

function revokeSealUrlIfBlob() {
  const u = sealUrl.value
  if (u && u.startsWith('blob:')) URL.revokeObjectURL(u)
}

async function loadSealBlobUrl(seal: CompanySealRow | undefined) {
  revokeSealUrlIfBlob()
  sealUrl.value = null
  if (!seal?.documentId?.trim()) return
  try {
    const blob = await apiClient.getBlob(`/api/v1/documents/${seal.documentId.trim()}/download`)
    if (!blob.size) return
    const mime = (blob.type || '').toLowerCase()
    if (mime.startsWith('image/')) {
      sealUrl.value = URL.createObjectURL(blob)
      return
    }
    if (mime === 'application/pdf' || mime === 'application/x-pdf') {
      sealUrl.value = await renderPdfBlobFirstPageToPngDataUrl(blob)
      return
    }
    const fn = String(seal.fileName || '')
    if (/\.pdf$/i.test(fn)) {
      sealUrl.value = await renderPdfBlobFirstPageToPngDataUrl(blob)
      return
    }
    sealUrl.value = URL.createObjectURL(blob)
  } catch {
    sealUrl.value = null
  }
}

async function loadCompanyLogoBlobUrl(logo: CompanyLogoRow | undefined) {
  if (logo?.documentId) {
    try {
      const blob = await apiClient.getBlob(`/api/v1/documents/${logo.documentId}/download`)
      if (blob.size > 0) {
        companyLogoObjectUrl.value = URL.createObjectURL(blob)
        return
      }
    } catch {
      // ignore
    }
  }
  companyLogoObjectUrl.value = null
}

async function loadReportAssets(seal: CompanySealRow | undefined, logo: CompanyLogoRow | undefined) {
  await Promise.all([loadSealBlobUrl(seal), loadCompanyLogoBlobUrl(logo)])
}

async function load() {
  const seq = ++loadSeq
  loading.value = true
  errorMsg.value = ''
  revokeSealUrlIfBlob()
  sealUrl.value = null
  if (companyLogoObjectUrl.value) {
    URL.revokeObjectURL(companyLogoObjectUrl.value)
    companyLogoObjectUrl.value = null
  }

  const id = packingId.value
  const kind = packingInspection.value
  if (!id) {
    errorMsg.value = t('stockOutPackingReport.missingPackingId')
    loading.value = false
    return
  }
  if (kind !== 'with-inspection' && kind !== 'without-inspection') {
    errorMsg.value = t('stockOutPackingReport.badRoute')
    loading.value = false
    return
  }

  const wantInspection = kind === 'with-inspection'
  withShipmentInspection.value = wantInspection

  let seal: CompanySealRow | undefined
  let logo: CompanyLogoRow | undefined

  try {
    const bundle = await packingApi.getPackingReportBundle(id, wantInspection)
    if (seq !== loadSeq) return
    if (!bundle?.stockOut) {
      errorMsg.value = t('stockOutPackingReport.notFound')
      stockOut.value = null
      packingAddresses.value = null
      packingCode.value = null
      warehouseInfoAddress.value = ''
      packingDeliveryMethod.value = null
      packingLines.value = []
      return
    }
    withShipmentInspection.value = bundle.withShipmentInspection
    stockOut.value = bundle.stockOut
    packingCode.value = bundle.packingCode ?? null
    warehouseInfoAddress.value = (bundle.warehouseAddress || '').trim()
    packingDeliveryMethod.value =
      bundle.deliveryMethod != null && !Number.isNaN(Number(bundle.deliveryMethod))
        ? Number(bundle.deliveryMethod)
        : null
    packingAddresses.value = bundle.packingAddresses ?? null
    let lines = bundle.packingLines ?? []
    if (lines.length === 0) {
      try {
        const detail = await packingApi.getById(id)
        lines = packingDetailItemsToReportLines(detail.items)
      } catch {
        /* bundle 未带明细时降级拉装箱单详情 */
      }
    }
    packingLines.value = lines
    const cp = bundle.companyProfile
    reportInfo.value = cp.reportInfo ?? null
    const logos = cp.logos ?? []
    const seals = cp.seals ?? []
    basicDefault.value = pickDefault(cp.basicInfos) ?? null
    warehouseRow.value = pickWarehouseForStockOut(cp.warehouses, bundle.stockOut.warehouseId) ?? null
    seal = pickReportSealRow(seals)
    logo = pickReportLogoRow(logos)
  } catch (e) {
    if (seq !== loadSeq) return
    errorMsg.value = getApiErrorMessage(e, t('stockOutPackingReport.loadFailed'))
    stockOut.value = null
    packingAddresses.value = null
    packingCode.value = null
    warehouseInfoAddress.value = ''
    packingDeliveryMethod.value = null
    reportInfo.value = null
    packingLines.value = []
  } finally {
    if (seq === loadSeq) loading.value = false
  }

  if (seq === loadSeq && stockOut.value) {
    void loadReportAssets(seal, logo)
  }
}

function doPrint() {
  window.print()
}

onMounted(() => {
  document.body.classList.add(PO_REPORT_PRINT_BODY_CLASS)
  load()
})
watch([packingId, packingInspection], () => load())

onBeforeUnmount(() => {
  document.body.classList.remove(PO_REPORT_PRINT_BODY_CLASS)
})

onUnmounted(() => {
  revokeSealUrlIfBlob()
  if (companyLogoObjectUrl.value) URL.revokeObjectURL(companyLogoObjectUrl.value)
})
</script>

<style scoped lang="scss">
.po-report-page {
  min-height: 100%;
  background: #0a1628;
  padding: 16px;
}

.toolbar {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  margin-bottom: 16px;
}

.toolbar__sp {
  flex: 1;
  min-width: 8px;
}

.toolbar__tag {
  font-size: 12px;
  color: #8eb4d4;
  border: 1px solid rgba(142, 180, 212, 0.45);
  border-radius: 6px;
  padding: 4px 10px;
  margin-right: 4px;
}

.toolbar__opt {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-right: 8px;
  flex-shrink: 0;
}

.toolbar__lang {
  :deep(.el-radio-button__inner) {
    padding: 5px 12px;
    font-size: 13px;
  }
}

.toolbar__opt-lbl {
  font-size: 13px;
  color: #8eb4d4;
  white-space: nowrap;
}

.preview-wrap {
  min-height: 400px;
}

.print-root {
  background: #525659;
  padding: 24px 16px 48px;
  border-radius: 8px;
  overflow: auto;
}

.err {
  color: #f56c6c;
  padding: 24px;
}

@media print {
  .po-report-page {
    background: #fff !important;
    padding: 0 !important;
  }

  .no-print {
    display: none !important;
  }

  .preview-wrap {
    min-height: 0 !important;
  }

  .print-root {
    background: #fff !important;
    padding: 0 !important;
    overflow: visible !important;
    border-radius: 0 !important;
  }
}
</style>
