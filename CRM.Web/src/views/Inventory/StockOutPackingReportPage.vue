<template>
  <div class="po-report-page">
    <div class="toolbar no-print">
      <el-button @click="router.back()">{{ t('stockOutPackingReport.back') }}</el-button>
      <div class="toolbar__sp" />
      <span v-if="ready" class="toolbar__tag">{{ variantTitle }}</span>
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
import { stockOutApi } from '@/api/stockOut'
import type {
  CompanyBasicRow,
  CompanyBankRow,
  CompanyLogoRow,
  CompanySealRow,
  CompanyWarehouseRow
} from '@/api/companyProfile'
import apiClient from '@/api/client'
import { formatDisplayDate } from '@/utils/displayDateTime'
import StockOutPackingReportDocument from '@/components/stockOut/StockOutPackingReportDocument.vue'
import { renderPdfBlobFirstPageToPngDataUrl } from '@/utils/pdfSealToPng'
import { getApiErrorMessage } from '@/utils/apiError'
import type { StockOutDetailDto } from '@/api/stockOut'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const PO_REPORT_PRINT_BODY_CLASS = 'po-order-report-print'
const DEFAULT_REPORT_LOGO = '/purchase-order-template/logo.svg'

const PACKING_NOTES_ZH = [
  '本装箱单依据系统出库记录开立，包装件数与唛头以实物为准。',
  '收货时请核对箱数与外包装完好情况；异常请当场提出。'
]

const route = useRoute()
const router = useRouter()
const { t, locale } = useI18n()

const loading = ref(true)
const errorMsg = ref('')
const stockOut = ref<StockOutDetailDto | null>(null)
const withShipmentInspection = ref(false)
const basicDefault = ref<CompanyBasicRow | null>(null)
const warehouseRow = ref<CompanyWarehouseRow | null>(null)
const bankDefault = ref<CompanyBankRow | null>(null)
const sealUrl = ref<string | null>(null)
const companyLogoObjectUrl = ref<string | null>(null)
const showSealOnReport = ref(true)

const stockOutId = computed(() => String(route.params.id || ''))
const packingInspection = computed(() => String(route.params.packingInspection || ''))

const ready = computed(() => !!stockOut.value && !errorMsg.value && !loading.value)

const variantTitle = computed(() =>
  withShipmentInspection.value
    ? t('stockOutPackingReport.variantWithInspection')
    : t('stockOutPackingReport.variantWithoutInspection')
)

/** 含出货检版式：五项检验项目（与 SEMICORE 模版一致） */
const qcInspectionItems = computed(() => [
  t('stockOutPackingReport.qcItems.i1'),
  t('stockOutPackingReport.qcItems.i2'),
  t('stockOutPackingReport.qcItems.i3'),
  t('stockOutPackingReport.qcItems.i4'),
  t('stockOutPackingReport.qcItems.i5')
])

const packingNotes = computed(() => {
  if (locale.value === 'en-US') {
    const base = [
      'This packing list is generated from the stock-out record; carton count and marks are subject to physical goods.',
      'Please verify carton count and outer packaging on receipt; report discrepancies immediately.'
    ]
    if (withShipmentInspection.value) {
      base.push('The outbound inspection section is for QC use; fill in after inspection.')
    }
    return base
  }
  const base = [...PACKING_NOTES_ZH]
  if (withShipmentInspection.value) {
    base.push('「出货检验」栏由 QC 填写；含检验版式用于需留痕的出货场景。')
  }
  return base
})

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

function formatChineseDate(input: Date | string | undefined | null): string {
  const s = formatDisplayDate(input)
  if (!s || s === '--') return '—'
  const parts = s.split('-').map((x) => parseInt(x, 10))
  if (parts.length < 3 || Number.isNaN(parts[0])) return s
  return `${parts[0]}年${parts[1]}月${parts[2]}日`
}

const docBind = computed(() => {
  const so = stockOut.value
  const basic = basicDefault.value
  const wh = warehouseRow.value
  const wqc = withShipmentInspection.value

  if (!so) {
    return {
      headerCompanyName: '',
      docTitle: t('stockOutPackingReport.docTitle'),
      docSubtitle: t('stockOutPackingReport.docSubtitle'),
      docNo: '',
      docDate: '',
      shipperLines: [] as string[],
      consigneeLines: [] as string[],
      shipmentLines: [] as string[],
      lines: [],
      totalQty: '0',
      remarkLines: [] as string[],
      notes: packingNotes.value,
      withShipmentInspection: wqc,
      qcInspectionItems: qcInspectionItems.value,
      qcInspectorLabel: t('stockOutPackingReport.qcInspector'),
      qcDateLabel: t('stockOutPackingReport.qcDate'),
      sealUrl: null as string | null,
      logoUrl: companyLogoObjectUrl.value ?? DEFAULT_REPORT_LOGO,
      showSeal: showSealOnReport.value,
      signDate: ''
    }
  }

  const qty = Number(so.totalQuantity) || 0
  const shipperAddr = (wh?.address || basic?.address || '').trim() || '—'
  const shipperPhone = (wh?.contactPhone || basic?.phone || '').trim() || '—'
  const shipperName = (basic?.companyName || '').trim() || '—'
  const consigneeName = maskSaleSensitiveFields.value ? '—' : (so.customerName || '').trim() || '—'

  const shipmentLines = [
    `${t('stockOutPackingReport.labels.shipMethod')}：${(so.shipmentMethod || '').trim() || '—'}`,
    `${t('stockOutPackingReport.labels.tracking')}：${(so.courierTrackingNo || '').trim() || '—'}`,
    `${t('stockOutPackingReport.labels.stockOutDate')}：${formatDisplayDate(so.stockOutDate) || '—'}`,
    `${t('stockOutPackingReport.labels.warehouse')}：${(wh?.warehouseName || so.warehouseCode || '').trim() || '—'}`,
    `${t('stockOutPackingReport.labels.source')}：${(so.sourceCode || '').trim() || '—'}`
  ]

  const descParts = [so.remark, so.sourceCode].filter((x) => x && String(x).trim())
  const description = descParts.length ? descParts.map((x) => String(x).trim()).join(' / ') : t('stockOutPackingReport.defaultLineDesc')
  const refCode = (so.sellOrderItemCode || so.sourceCode || '—').toString()

  return {
    headerCompanyName: shipperName,
    docTitle: t('stockOutPackingReport.docTitle'),
    docSubtitle: t('stockOutPackingReport.docSubtitle'),
    docNo: so.stockOutCode || '—',
    docDate: formatDisplayDate(so.stockOutDate) || '—',
    shipperLines: [
      `${t('stockOutPackingReport.labels.company')}：${shipperName}`,
      `${t('stockOutPackingReport.labels.address')}：${shipperAddr}`,
      `${t('stockOutPackingReport.labels.phone')}：${shipperPhone}`
    ],
    consigneeLines: [
      `${t('stockOutPackingReport.labels.company')}：${consigneeName}`,
      `${t('stockOutPackingReport.labels.address')}：—`
    ],
    shipmentLines,
    lines: [
      {
        index: 1,
        description,
        ref: refCode,
        qty: formatReportQty(qty),
        carton: '—',
        remark: (so.remark || '').trim() || '—'
      }
    ],
    totalQty: formatReportQty(qty),
    remarkLines: [
      `${t('stockOutPackingReport.labels.sellLine')}：${(so.sellOrderItemCode || '').trim() || '—'}`,
      `${t('stockOutPackingReport.labels.salesRep')}：${maskSaleSensitiveFields.value ? '—' : (so.salesUserName || '').trim() || '—'}`,
      `${t('stockOutPackingReport.labels.bankHint')}：${(bankDefault.value?.bankName || '').trim() || '—'}`
    ],
    notes: packingNotes.value,
    withShipmentInspection: wqc,
    qcInspectionItems: qcInspectionItems.value,
    qcInspectorLabel: t('stockOutPackingReport.qcInspector'),
    qcDateLabel: t('stockOutPackingReport.qcDate'),
    sealUrl: sealUrl.value,
    logoUrl: companyLogoObjectUrl.value ?? DEFAULT_REPORT_LOGO,
    showSeal: showSealOnReport.value,
    signDate: formatChineseDate(so.stockOutDate)
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

async function load() {
  loading.value = true
  errorMsg.value = ''
  revokeSealUrlIfBlob()
  sealUrl.value = null
  if (companyLogoObjectUrl.value) {
    URL.revokeObjectURL(companyLogoObjectUrl.value)
    companyLogoObjectUrl.value = null
  }

  const id = stockOutId.value
  const kind = packingInspection.value
  if (!id) {
    errorMsg.value = t('stockOutPackingReport.missingId')
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

  try {
    const bundle = await stockOutApi.getPackingReportBundle(id, wantInspection)
    if (!bundle?.stockOut) {
      errorMsg.value = t('stockOutPackingReport.notFound')
      stockOut.value = null
      return
    }
    withShipmentInspection.value = bundle.withShipmentInspection
    stockOut.value = bundle.stockOut
    const cp = bundle.companyProfile
    const logos = cp.logos ?? []
    const seals = cp.seals ?? []
    basicDefault.value = pickDefault(cp.basicInfos) ?? null
    bankDefault.value = pickDefault(cp.bankInfos) ?? null
    warehouseRow.value = pickWarehouseForStockOut(cp.warehouses, bundle.stockOut.warehouseId) ?? null
    const seal = pickReportSealRow(seals)
    await loadSealBlobUrl(seal)
    const logo = pickReportLogoRow(logos)
    await loadCompanyLogoBlobUrl(logo)
  } catch (e) {
    errorMsg.value = getApiErrorMessage(e, t('stockOutPackingReport.loadFailed'))
    stockOut.value = null
  } finally {
    loading.value = false
  }
}

function doPrint() {
  window.print()
}

onMounted(() => {
  document.body.classList.add(PO_REPORT_PRINT_BODY_CLASS)
  load()
})
watch([stockOutId, packingInspection], () => load())

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
