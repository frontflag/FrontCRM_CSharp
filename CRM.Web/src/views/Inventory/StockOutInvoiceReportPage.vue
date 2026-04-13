<template>
  <div class="po-report-page">
    <div class="toolbar no-print">
      <el-button @click="router.back()">{{ t('stockOutInvoiceReport.back') }}</el-button>
      <div class="toolbar__sp" />
      <div class="toolbar__opt" :title="t('stockOutInvoiceReport.sealHint')">
        <span class="toolbar__opt-lbl">{{ t('stockOutInvoiceReport.showSeal') }}</span>
        <el-switch v-model="showSealOnReport" />
      </div>
      <el-button type="primary" :disabled="!ready" @click="doPrint">{{ t('stockOutInvoiceReport.print') }}</el-button>
    </div>

    <div v-loading="loading" class="preview-wrap">
      <div v-if="errorMsg" class="err">{{ errorMsg }}</div>
      <div v-else-if="ready" class="print-root">
        <StockOutInvoiceReportDocument v-bind="docBind" />
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
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDate } from '@/utils/displayDateTime'
import StockOutInvoiceReportDocument from '@/components/stockOut/StockOutInvoiceReportDocument.vue'
import { renderPdfBlobFirstPageToPngDataUrl } from '@/utils/pdfSealToPng'
import { getApiErrorMessage } from '@/utils/apiError'
import type { StockOutDetailDto } from '@/api/stockOut'

const PO_REPORT_PRINT_BODY_CLASS = 'po-order-report-print'
const DEFAULT_REPORT_LOGO = '/purchase-order-template/logo.svg'

const INVOICE_TERMS_ZH = [
  '本发票根据系统出库单及关联销售信息开立，仅供商检、海关及收付款参考。',
  '货物品名、数量金额以双方确认订单及实物为准；如有争议以合同约定为准。'
]

const route = useRoute()
const router = useRouter()
const { t, locale } = useI18n()
const authStore = useAuthStore()

const loading = ref(true)
const errorMsg = ref('')
const stockOut = ref<StockOutDetailDto | null>(null)
const basicDefault = ref<CompanyBasicRow | null>(null)
const warehouseRow = ref<CompanyWarehouseRow | null>(null)
const bankDefault = ref<CompanyBankRow | null>(null)
const sealUrl = ref<string | null>(null)
const companyLogoObjectUrl = ref<string | null>(null)
const showSealOnReport = ref(true)

const canViewAmount = computed(
  () =>
    authStore.hasPermission('sales.amount.read') ||
    authStore.hasPermission('purchase.amount.read')
)

const stockOutId = computed(() => String(route.params.id || ''))
const ready = computed(() => !!stockOut.value && !errorMsg.value && !loading.value)

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

function formatMoney(n: number): string {
  return (n ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatChineseDate(input: Date | string | undefined | null): string {
  const s = formatDisplayDate(input)
  if (!s || s === '--') return '—'
  const parts = s.split('-').map((x) => parseInt(x, 10))
  if (parts.length < 3 || Number.isNaN(parts[0])) return s
  return `${parts[0]}年${parts[1]}月${parts[2]}日`
}

function formatBankLines(b: CompanyBankRow | null): string[] {
  if (!b) return ['—']
  const lines: string[] = []
  if (b.bankName?.trim()) lines.push(`${t('stockOutInvoiceReport.bank.bankName')}：${b.bankName.trim()}`)
  if (b.accountName?.trim()) lines.push(`${t('stockOutInvoiceReport.bank.accountName')}：${b.accountName.trim()}`)
  if (b.bankCode?.trim()) lines.push(`${t('stockOutInvoiceReport.bank.accountNo')}：${b.bankCode.trim()}`)
  if (b.swift?.trim()) lines.push(`SWIFT：${b.swift.trim()}`)
  if (b.iban?.trim()) lines.push(`IBAN：${b.iban.trim()}`)
  if (b.bankAddress?.trim()) lines.push(`${t('stockOutInvoiceReport.bank.bankAddress')}：${b.bankAddress.trim()}`)
  if (b.currency?.trim()) lines.push(`${t('stockOutInvoiceReport.bank.currency')}：${b.currency.trim()}`)
  return lines.length ? lines : ['—']
}

const invoiceTerms = computed(() => {
  if (locale.value === 'en-US') {
    return [
      'This invoice is issued from the stock-out record and related sales data for customs, inspection, and payment reference only.',
      'Product description, quantities, and amounts are subject to confirmed orders and physical goods; disputes are governed by the underlying contract.'
    ]
  }
  return INVOICE_TERMS_ZH
})

const docBind = computed(() => {
  const so = stockOut.value
  const basic = basicDefault.value
  const wh = warehouseRow.value

  if (!so) {
    return {
      headerCompanyName: '',
      invoiceTitle: t('stockOutInvoiceReport.docTitle'),
      invoiceSubtitle: t('stockOutInvoiceReport.docSubtitle'),
      invoiceNo: '',
      invoiceDate: '',
      exporterLines: [] as string[],
      consigneeLines: [] as string[],
      shipmentLines: [] as string[],
      currencyLabel: 'RMB',
      lines: [],
      totalQty: '0',
      totalAmount: '0.00',
      grandTotal: '0.00',
      bankLines: ['—'],
      remarkLines: [] as string[],
      terms: invoiceTerms.value,
      sealUrl: null as string | null,
      logoUrl: companyLogoObjectUrl.value ?? DEFAULT_REPORT_LOGO,
      showAmounts: canViewAmount.value,
      showSeal: showSealOnReport.value,
      signDate: ''
    }
  }

  const qty = Number(so.totalQuantity) || 0
  const amt = Number(so.totalAmount) || 0
  const unit = qty > 0 ? amt / qty : 0

  const exporterAddr = (wh?.address || basic?.address || '').trim() || '—'
  const exporterPhone = (wh?.contactPhone || basic?.phone || '').trim() || '—'
  const exporterName = (basic?.companyName || '').trim() || '—'

  const consigneeName = (so.customerName || '').trim() || '—'

  const shipmentLines = [
    `${t('stockOutInvoiceReport.labels.shipMethod')}：${(so.shipmentMethod || '').trim() || '—'}`,
    `${t('stockOutInvoiceReport.labels.tracking')}：${(so.courierTrackingNo || '').trim() || '—'}`,
    `${t('stockOutInvoiceReport.labels.stockOutDate')}：${formatDisplayDate(so.stockOutDate) || '—'}`,
    `${t('stockOutInvoiceReport.labels.warehouse')}：${(wh?.warehouseName || so.warehouseCode || '').trim() || '—'}`,
    `${t('stockOutInvoiceReport.labels.source')}：${(so.sourceCode || '').trim() || '—'}`
  ]

  const descParts = [so.remark, so.sourceCode].filter((x) => x && String(x).trim())
  const description = descParts.length ? descParts.map((x) => String(x).trim()).join(' / ') : t('stockOutInvoiceReport.defaultLineDesc')

  const refCode = (so.sellOrderItemCode || so.sourceCode || '—').toString()

  return {
    headerCompanyName: exporterName,
    invoiceTitle: t('stockOutInvoiceReport.docTitle'),
    invoiceSubtitle: t('stockOutInvoiceReport.docSubtitle'),
    invoiceNo: so.stockOutCode || '—',
    invoiceDate: formatDisplayDate(so.stockOutDate) || '—',
    exporterLines: [
      `${t('stockOutInvoiceReport.labels.company')}：${exporterName}`,
      `${t('stockOutInvoiceReport.labels.address')}：${exporterAddr}`,
      `${t('stockOutInvoiceReport.labels.phone')}：${exporterPhone}`
    ],
    consigneeLines: [
      `${t('stockOutInvoiceReport.labels.company')}：${consigneeName}`,
      `${t('stockOutInvoiceReport.labels.address')}：—`
    ],
    shipmentLines,
    currencyLabel: 'RMB',
    lines: [
      {
        index: 1,
        ref: refCode,
        description,
        qty: formatReportQty(qty),
        unitPrice: formatMoney(unit),
        amount: formatMoney(amt)
      }
    ],
    totalQty: formatReportQty(qty),
    totalAmount: formatMoney(amt),
    grandTotal: formatMoney(amt),
    bankLines: formatBankLines(bankDefault.value),
    remarkLines: [
      `${t('stockOutInvoiceReport.labels.sellLine')}：${(so.sellOrderItemCode || '').trim() || '—'}`,
      `${t('stockOutInvoiceReport.labels.salesRep')}：${(so.salesUserName || '').trim() || '—'}`,
      `${t('stockOutInvoiceReport.labels.remark')}：${(so.remark || '').trim() || '—'}`
    ],
    terms: invoiceTerms.value,
    sealUrl: sealUrl.value,
    logoUrl: companyLogoObjectUrl.value ?? DEFAULT_REPORT_LOGO,
    showAmounts: canViewAmount.value,
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
  try {
    const id = stockOutId.value
    if (!id) {
      errorMsg.value = t('stockOutInvoiceReport.missingId')
      return
    }
    const bundle = await stockOutApi.getInvoiceReportBundle(id)
    if (!bundle?.stockOut) {
      errorMsg.value = t('stockOutInvoiceReport.notFound')
      stockOut.value = null
      return
    }
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
    errorMsg.value = getApiErrorMessage(e, t('stockOutInvoiceReport.loadFailed'))
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
watch(stockOutId, () => load())

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
