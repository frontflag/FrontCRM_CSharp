<template>
  <div class="po-report-page">
    <div class="toolbar no-print">
      <el-button @click="router.back()">{{ t('salesOrderReport.back') }}</el-button>
      <div class="toolbar__sp" />
      <div class="toolbar__opt" :title="t('salesOrderReport.sealHint')">
        <span class="toolbar__opt-lbl">{{ t('salesOrderReport.sealOnReport') }}</span>
        <el-switch v-model="showSealOnReport" />
      </div>
      <el-button type="primary" :disabled="!ready" @click="doPrint">{{ t('salesOrderReport.print') }}</el-button>
      <el-button type="primary" :disabled="!ready" :loading="exporting" @click="doExportPdf">
        {{ t('salesOrderReport.exportPdf') }}
      </el-button>
    </div>

    <div v-loading="loading" class="preview-wrap">
      <div v-if="errorMsg" class="err">{{ errorMsg }}</div>
      <div v-else-if="ready" id="so-report-print-root" ref="reportRoot" class="print-root">
        <SalesOrderReportDocument v-bind="docBind" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { salesOrderApi } from '@/api/salesOrder'
import type {
  CompanyBasicRow,
  CompanyLogoRow,
  CompanySealRow,
  CompanyWarehouseRow
} from '@/api/companyProfile'
import apiClient from '@/api/client'
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { SALES_ORDER_SERVICE_TERMS } from '@/constants/salesOrderReportTerms'
import { CURRENCY_CODE_TO_TEXT, settlementVatRateDecimal } from '@/constants/currency'
import SalesOrderReportDocument from '@/components/SalesOrder/SalesOrderReportDocument.vue'
import { renderElementToPdfBlob } from '@/utils/poReportPdf'
import { renderPdfBlobFirstPageToPngDataUrl } from '@/utils/pdfSealToPng'
import { getApiErrorMessage } from '@/utils/apiError'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const PO_REPORT_PRINT_BODY_CLASS = 'po-order-report-print'

const DEFAULT_SO_REPORT_LOGO = '/purchase-order-template/logo.svg'

const loading = ref(true)
const errorMsg = ref('')
const order = ref<Record<string, any> | null>(null)
const basicDefault = ref<CompanyBasicRow | null>(null)
const warehouseDefault = ref<CompanyWarehouseRow | null>(null)
const sealUrl = ref<string | null>(null)
const companyLogoObjectUrl = ref<string | null>(null)
const reportRoot = ref<HTMLElement | null>(null)

const exporting = ref(false)
const showSealOnReport = ref(true)

const canViewAmount = computed(() => authStore.hasPermission('sales.amount.read'))
const showReportAmounts = computed(() => canViewAmount.value && !maskSaleSensitiveFields.value)

const soId = computed(() => String(route.params.id || ''))

const ready = computed(() => !!order.value && !errorMsg.value && !loading.value)

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

function currencyCode(v: number | undefined | null): string {
  const n = Number(v)
  if (!Number.isFinite(n)) return CURRENCY_CODE_TO_TEXT[1]
  return CURRENCY_CODE_TO_TEXT[n] ?? CURRENCY_CODE_TO_TEXT[1]
}

function formatFooterTaxRateLabel(exclSum: number, taxSum: number): string {
  if (!taxSum || taxSum < 1e-9) return '0'
  if (!exclSum || exclSum < 1e-9) return '0'
  const eff = taxSum / exclSum
  const s = eff.toFixed(4).replace(/\.?0+$/, '')
  return s || '0'
}

function formatReportQty(n: number): string {
  return (n ?? 0).toLocaleString('zh-CN', { maximumFractionDigits: 4 })
}

function formatReportLineTotal(n: number): string {
  return (n ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatReportUnitPrice(n: number): string {
  return (n ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

function formatChineseDate(input: Date | string | undefined | null): string {
  const s = formatDisplayDate(input)
  if (!s || s === '--') return '—'
  const parts = s.split('-').map((x) => parseInt(x, 10))
  if (parts.length < 3 || Number.isNaN(parts[0])) return s
  return `${parts[0]}年${parts[1]}月${parts[2]}日`
}

const docBind = computed(() => {
  const o = order.value
  if (!o) {
    return {
      headerCompanyName: '',
      orderCode: '',
      orderDate: '',
      deliveryDate: '',
      deliveryMode: '送货/物流/快递',
      partySeller: { name: '', address: '', phone: '', consignee: '' },
      partyBuyer: { name: '', address: '' },
      currencyLabel: 'RMB',
      lines: [],
      totalQty: '0',
      totalIncl: '0.00',
      exclTax: '0.00',
      taxAmount: '0.00',
      grandIncl: '0.00',
      taxRateLabel: '0',
      extraLines: [] as string[],
      terms: SALES_ORDER_SERVICE_TERMS,
      sealUrl: null as string | null,
      logoUrl: companyLogoObjectUrl.value ?? DEFAULT_SO_REPORT_LOGO,
      showAmounts: showReportAmounts.value,
      showSeal: showSealOnReport.value,
      sellerSignDate: ''
    }
  }

  const seller = basicDefault.value
  const wh = warehouseDefault.value
  const items = (o.items as any[]) || []
  const headerCurNum = Number(o.currency)
  const cur = currencyCode(o.currency)

  let qtySum = 0
  let inclSum = 0
  let exclSum = 0
  let taxSum = 0
  const lines = items.map((row, i) => {
    const qty = Number(row.qty) || 0
    const unitPrice = Number(row.price) || 0
    const lineTotal = qty * unitPrice
    const lineCur = row.currency != null && row.currency !== '' ? Number(row.currency) : headerCurNum
    const rate = settlementVatRateDecimal(Number.isFinite(lineCur) ? lineCur : headerCurNum)
    const lineExcl = rate > 0 ? lineTotal / (1 + rate) : lineTotal
    const lineTax = lineTotal - lineExcl
    qtySum += qty
    inclSum += lineTotal
    exclSum += lineExcl
    taxSum += lineTax
    const remark = (row.comment && String(row.comment).trim()) || ''
    return {
      index: i + 1,
      brand: row.brand || '—',
      unit: 'PCS',
      currency: currencyCode(Number.isFinite(lineCur) ? lineCur : headerCurNum),
      qty: formatReportQty(qty),
      unitPrice: formatReportUnitPrice(unitPrice),
      taxRate: String(rate),
      lineTotal: formatReportLineTotal(lineTotal),
      productName: remark || row.pn || '—',
      spec: row.pn || '—'
    }
  })

  const excl = exclSum
  const tax = taxSum
  const taxRateLabel = formatFooterTaxRateLabel(exclSum, taxSum)

  const shipTo = [o.deliveryAddress, wh?.warehouseName, wh?.address].filter((x) => x && String(x).trim()).join('；') || '—'

  const salesLine = maskSaleSensitiveFields.value ? '—' : o.salesUserName ? String(o.salesUserName) : '—'

  const custName = maskSaleSensitiveFields.value ? '—' : o.customerName ? String(o.customerName) : '—'
  const buyerAddr = (o.deliveryAddress && String(o.deliveryAddress).trim()) || '—'

  const sellerPhone = [seller?.phone, seller?.fax].filter(Boolean).join(' / ') || '—'

  return {
    headerCompanyName: seller?.companyName || '—',
    orderCode: o.sellOrderCode || '—',
    orderDate: formatDisplayDate(o.createTime) || '—',
    deliveryDate: formatDisplayDate(o.deliveryDate) || '—',
    deliveryMode: '送货/物流/快递',
    partySeller: {
      name: seller?.companyName || '—',
      address: seller?.address || '—',
      phone: sellerPhone,
      consignee: salesLine
    },
    partyBuyer: {
      name: custName,
      address: buyerAddr
    },
    currencyLabel: cur,
    lines,
    totalQty: formatReportQty(qtySum),
    totalIncl: formatReportLineTotal(inclSum),
    exclTax: formatReportLineTotal(excl),
    taxAmount: formatReportLineTotal(tax),
    grandIncl: formatReportLineTotal(inclSum),
    taxRateLabel,
    extraLines: [
      `交货地址：${shipTo}`,
      `订单备注：${(o.comment && String(o.comment).trim()) || '—'}`
    ],
    terms: SALES_ORDER_SERVICE_TERMS,
    sealUrl: sealUrl.value,
    logoUrl: companyLogoObjectUrl.value ?? DEFAULT_SO_REPORT_LOGO,
    showAmounts: showReportAmounts.value,
    showSeal: showSealOnReport.value,
    sellerSignDate: formatChineseDate(o.createTime)
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
    const id = soId.value
    if (!id) {
      errorMsg.value = t('salesOrderReport.missingId')
      return
    }
    const data = (await salesOrderApi.getReportData(id)) as {
      order: Record<string, unknown>
      companyProfile: {
        basicInfos?: CompanyBasicRow[]
        warehouses?: CompanyWarehouseRow[]
        seals?: CompanySealRow[]
        logos?: CompanyLogoRow[]
      }
    }
    order.value = data.order as any
    const profile = data.companyProfile as typeof data.companyProfile & { Seals?: CompanySealRow[] }
    const logos =
      profile?.logos ??
      (profile as { Logos?: CompanyLogoRow[] } | undefined)?.Logos ??
      []
    const seals = profile?.seals ?? profile?.Seals ?? []
    basicDefault.value = pickDefault(profile.basicInfos) ?? null
    warehouseDefault.value = pickDefault(profile.warehouses) ?? null
    const seal = pickReportSealRow(seals)
    await loadSealBlobUrl(seal)
    const logo = pickReportLogoRow(logos)
    await loadCompanyLogoBlobUrl(logo)
  } catch (e) {
    errorMsg.value = getApiErrorMessage(e, t('salesOrderReport.loadFailed'))
    order.value = null
  } finally {
    loading.value = false
  }
}

function doPrint() {
  window.print()
}

function getPdfDocumentElement(): HTMLElement | null {
  const wrap = reportRoot.value
  if (!wrap) return null
  return wrap.querySelector('.po-doc') as HTMLElement | null
}

async function doExportPdf() {
  const el = getPdfDocumentElement()
  if (!el) {
    ElMessage.error(t('salesOrderReport.pdfNoDom'))
    return
  }
  exporting.value = true
  try {
    const blob = await renderElementToPdfBlob(el)
    const code = order.value?.sellOrderCode || 'sales-order'
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${code}.pdf`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success(t('salesOrderReport.exportOk'))
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('salesOrderReport.exportFailed')))
  } finally {
    exporting.value = false
  }
}

onMounted(() => {
  document.body.classList.add(PO_REPORT_PRINT_BODY_CLASS)
  load()
})
watch(soId, () => load())

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
