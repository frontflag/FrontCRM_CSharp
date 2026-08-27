<template>
  <div class="po-report-page">
    <div class="toolbar no-print">
      <el-button @click="router.back()">{{ t('stockOutInvoiceReport.back') }}</el-button>
      <ReportLetterheadSelect
        v-model="selectedBasicId"
        :options="letterheadOptions"
        :disabled="!ready"
      />
      <div class="toolbar__sp" />
      <div class="toolbar__opt">
        <el-radio-group v-model="reportLang" size="small" class="toolbar__lang">
          <el-radio-button label="zh">{{ t('stockOutInvoiceReport.langZh') }}</el-radio-button>
          <el-radio-button label="en">{{ t('stockOutInvoiceReport.langEn') }}</el-radio-button>
        </el-radio-group>
      </div>
      <div class="toolbar__opt" :title="t('stockOutInvoiceReport.sealHint')">
        <span class="toolbar__opt-lbl">{{ t('stockOutInvoiceReport.showSeal') }}</span>
        <el-switch v-model="showSealOnReport" />
      </div>
      <el-button type="primary" :disabled="!ready" @click="doPrint">{{ t('stockOutInvoiceReport.print') }}</el-button>
    </div>

    <div v-loading="loading" class="preview-wrap">
      <div v-if="errorMsg" class="err">{{ errorMsg }}</div>
      <div v-else-if="ready" class="print-root">
        <component :is="invoiceReportSkin" v-bind="docBind" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { stockOutApi } from '@/api/stockOut'
import { packingApi, packingDetailItemsToReportLines } from '@/api/packing'
import {
  type CompanyBasicRow,
  type CompanyBankRow,
  type CompanyLogoRow,
  type CompanySealRow,
  type CompanyWarehouseRow,
  pickDefaultBankByRegion
} from '@/api/companyProfile'
import ReportLetterheadSelect from '@/components/Common/ReportLetterheadSelect.vue'
import {
  letterheadKindOf,
  pickEnabledDefault,
  pickReportLogoRow,
  pickReportSealRow,
  resolveLetterheadSelection,
  tradeCurrencyToLetterheadPrefer
} from '@/utils/reportLetterhead'
import apiClient from '@/api/client'
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { renderPdfBlobFirstPageToPngDataUrl } from '@/utils/pdfSealToPng'
import { getApiErrorMessage } from '@/utils/apiError'
import type { PackingReportAddressPanel, PackingReportLine, StockOutDetailDto } from '@/api/stockOut'
import type { StockOutInvoiceLineVm } from '@/components/stockOut/invoiceReport/types'
import { resolveInvoiceReportSkin } from '@/components/stockOut/invoiceReport/resolveInvoiceReportSkin'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import { reportParamsApi, type ReportStyleVersion } from '@/api/reportParams'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { normalizePackingAddrLines } from '@/utils/packingReportAddressLines'
import {
  getInvoiceReportLabels,
  type InvoiceReportLang,
  type InvoiceReportLabels
} from '@/components/stockOut/packingReportLabels'

const styleVersion = ref<ReportStyleVersion>('V1')
const invoiceReportSkin = computed(() => resolveInvoiceReportSkin(LOGIN_TENANT_ID, styleVersion.value))

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const PO_REPORT_PRINT_BODY_CLASS = 'po-order-report-print'
const DEFAULT_REPORT_LOGO = '/purchase-order-template/logo.svg'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()

const loading = ref(true)
const errorMsg = ref('')
const stockOut = ref<StockOutDetailDto | null>(null)
const packingAddresses = ref<PackingReportAddressPanel | null>(null)
const packingCode = ref<string | null>(null)
const packingLines = ref<PackingReportLine[]>([])
const warehouseInfoAddress = ref('')
const basicDefault = ref<CompanyBasicRow | null>(null)
const profileBasics = ref<CompanyBasicRow[]>([])
const profileSeals = ref<CompanySealRow[]>([])
const selectedBasicId = ref('')
const letterheadOptions = ref<{ value: string; label: string }[]>([])
const warehouseRow = ref<CompanyWarehouseRow | null>(null)
const bankDefault = ref<CompanyBankRow | null>(null)
const sealUrl = ref<string | null>(null)
const companyLogoObjectUrl = ref<string | null>(null)
const showSealOnReport = ref(true)
const reportLang = ref<InvoiceReportLang>('en')

const invoiceLabels = computed(() => getInvoiceReportLabels(reportLang.value))

const canViewAmount = computed(
  () =>
    authStore.hasPermission('sales.amount.read') ||
    authStore.hasPermission('purchase.amount.read')
)
const showInvoiceAmounts = computed(() => canViewAmount.value && !maskSaleSensitiveFields.value)

const stockOutId = computed(() => String(route.params.id || ''))
const packingId = computed(() => String(route.params.packingId || ''))
const ready = computed(() => !!stockOut.value && !errorMsg.value && !loading.value)

function letterheadLabels() {
  return {
    defaultSuffix: t('reportLetterhead.defaultSuffix'),
    fallbackRmb: t('reportLetterhead.fallbackRmb'),
    fallbackForeign: t('reportLetterhead.fallbackForeign'),
    fallbackDefault: t('reportLetterhead.fallbackDefault')
  }
}

function applyLetterheadSelection(preferCode: number | string | null | undefined) {
  const prefer = tradeCurrencyToLetterheadPrefer(preferCode)
  const resolved = resolveLetterheadSelection(profileBasics.value, prefer, letterheadLabels())
  letterheadOptions.value = resolved.options
  selectedBasicId.value = resolved.selectedId
  basicDefault.value =
    profileBasics.value.find((r) => r.id === resolved.selectedId) ?? resolved.auto ?? null
}

function sealForCurrentLetterhead(): CompanySealRow | undefined {
  return pickReportSealRow(profileSeals.value, letterheadKindOf(basicDefault.value))
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
  return pickEnabledDefault(rows)
}

function formatReportQty(n: number): string {
  return (n ?? 0).toLocaleString('zh-CN', { maximumFractionDigits: 4 })
}

function formatMoney(n: number): string {
  return (n ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function cellText(v: string | null | undefined, dash = '—'): string {
  const s = (v ?? '').trim()
  return s || dash
}

function blankIfEmpty(v: string | null | undefined): string {
  return (v ?? '').trim()
}

function mapInvoiceReportLines(
  rows: PackingReportLine[],
  totalAmount: number,
  showAmounts: boolean
): StockOutInvoiceLineVm[] {
  const totalQty = rows.reduce((acc, row) => acc + (Number(row.qty) || 0), 0)
  return rows.map((row, idx) => {
    const lineQty = Number(row.qty) || 0
    const lineAmt = totalQty > 0 && showAmounts ? (totalAmount * lineQty) / totalQty : 0
    const unit = lineQty > 0 && showAmounts ? lineAmt / lineQty : 0
    return {
      index: idx + 1,
      pn: cellText(row.pn),
      customerPn: blankIfEmpty(row.customerPn),
      brand: cellText(row.brand),
      customerBrand: blankIfEmpty(row.customerBrand),
      qty: formatReportQty(lineQty),
      unitPrice: showAmounts ? formatMoney(unit) : '—',
      amount: showAmounts ? formatMoney(lineAmt) : '—',
      remark: blankIfEmpty(row.remark)
    }
  })
}

function buildFallbackInvoiceLines(so: StockOutDetailDto, showAmounts: boolean): StockOutInvoiceLineVm[] {
  const qty = Number(so.totalQuantity) || 0
  const amt = Number(so.totalAmount) || 0
  const unit = qty > 0 ? amt / qty : 0
  return [
    {
      index: 1,
      pn: cellText(so.sourceCode || so.sellOrderItemCode),
      customerPn: '',
      brand: cellText(undefined),
      customerBrand: '',
      qty: formatReportQty(qty),
      unitPrice: showAmounts ? formatMoney(unit) : '—',
      amount: showAmounts ? formatMoney(amt) : '—',
      remark: blankIfEmpty(so.remark)
    }
  ]
}

function reportLinesForDoc(so: StockOutDetailDto): StockOutInvoiceLineVm[] {
  const showAmounts = showInvoiceAmounts.value
  if (packingLines.value.length > 0) {
    return mapInvoiceReportLines(packingLines.value, Number(so.totalAmount) || 0, showAmounts)
  }
  return buildFallbackInvoiceLines(so, showAmounts)
}

function reportTotalQty(lines: StockOutInvoiceLineVm[]): string {
  if (packingLines.value.length > 0) {
    const sum = packingLines.value.reduce((acc, row) => acc + (Number(row.qty) || 0), 0)
    return formatReportQty(sum)
  }
  return formatReportQty(lines.reduce((acc, row) => acc + (Number(String(row.qty).replace(/,/g, '')) || 0), 0))
}

function formatBankLines(b: CompanyBankRow | null, L: InvoiceReportLabels): string[] {
  if (!b) return ['—']
  const lines: string[] = []
  if (b.bankName?.trim()) lines.push(`${L.bankName}${b.bankName.trim()}`)
  if (b.accountName?.trim()) lines.push(`${L.accountName}${b.accountName.trim()}`)
  const acctNo = (b.accountNumber || b.bankCode || '').trim()
  if (acctNo) lines.push(`${L.accountNo}${acctNo}`)
  if (b.swift?.trim()) lines.push(`${L.swift}${b.swift.trim()}`)
  if (b.iban?.trim()) lines.push(`${L.iban}${b.iban.trim()}`)
  if (b.bankAddress?.trim()) lines.push(`${L.bankAddress}${b.bankAddress.trim()}`)
  if (b.currency?.trim()) lines.push(`${L.currency}${b.currency.trim()}`)
  return lines.length ? lines : ['—']
}

const docBind = computed(() => {
  const so = stockOut.value
  const basic = basicDefault.value
  const L = invoiceLabels.value

  if (!so) {
    return {
      labels: L,
      headerCompanyName: '',
      invoiceTitle: L.invoiceDocTitle,
      invoiceSubtitle: '',
      invoiceNo: '',
      invoiceDate: '',
      headerWarehouseAddress: '',
      billToLines: normalizePackingAddrLines(undefined),
      shipToLines: normalizePackingAddrLines(undefined),
      lines: [],
      totalQty: '0',
      totalAmount: '0.00',
      bankLines: ['—'],
      sealUrl: null as string | null,
      logoUrl: companyLogoObjectUrl.value ?? DEFAULT_REPORT_LOGO,
      showAmounts: showInvoiceAmounts.value,
      showSeal: showSealOnReport.value,
      signDate: '',
      reportLang: reportLang.value
    }
  }

  const amt = Number(so.totalAmount) || 0

  const exporterName = (basic?.companyName || '').trim() || '—'
  const customerLine = maskSaleSensitiveFields.value ? '—' : (so.customerName || '').trim() || '—'
  const addr = packingAddresses.value
  const billToLines = normalizePackingAddrLines(addr?.billToLines, customerLine, L)
  const shipToLines = normalizePackingAddrLines(addr?.shipToLines, customerLine, L)

  const lines = reportLinesForDoc(so)

  return {
    labels: L,
    headerCompanyName: exporterName,
    headerWarehouseAddress: warehouseInfoAddress.value.trim(),
    invoiceTitle: L.invoiceDocTitle,
    invoiceSubtitle: '',
    invoiceNo: (packingCode.value || so.stockOutCode || '').trim() || '—',
    invoiceDate: formatDisplayDate(so.stockOutDate) || '—',
    billToLines,
    shipToLines,
    lines,
    totalQty: reportTotalQty(lines),
    totalAmount: formatMoney(amt),
    bankLines: formatBankLines(bankDefault.value, L),
    sealUrl: sealUrl.value,
    logoUrl: companyLogoObjectUrl.value ?? DEFAULT_REPORT_LOGO,
    showAmounts: showInvoiceAmounts.value,
    showSeal: showSealOnReport.value,
    signDate: formatDisplayDate(so.stockOutDate) || '—',
    reportLang: reportLang.value
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
    const fromPacking = packingId.value.trim()
    const fromStockOut = stockOutId.value.trim()
    if (!fromPacking && !fromStockOut) {
      errorMsg.value = t('stockOutInvoiceReport.missingId')
      return
    }
    const [effectiveVersion, bundle] = await Promise.all([
      reportParamsApi.getEffectiveStyleVersion(),
      fromPacking
        ? packingApi.getInvoiceReportBundle(fromPacking)
        : stockOutApi.getInvoiceReportBundle(fromStockOut)
    ])
    styleVersion.value = effectiveVersion
    if (!bundle?.stockOut) {
      errorMsg.value = t('stockOutInvoiceReport.notFound')
      stockOut.value = null
      packingAddresses.value = null
      packingCode.value = null
      packingLines.value = []
      warehouseInfoAddress.value = ''
      return
    }
    stockOut.value = bundle.stockOut
    packingAddresses.value = bundle.packingAddresses ?? null
    packingCode.value = bundle.packingCode ?? null
    warehouseInfoAddress.value = (bundle.warehouseAddress || '').trim()
    let lines = bundle.packingLines ?? []
    if (fromPacking && lines.length === 0) {
      try {
        const detail = await packingApi.getById(fromPacking)
        lines = packingDetailItemsToReportLines(detail.items)
      } catch {
        /* bundle 未带明细时降级拉装箱单详情 */
      }
    }
    packingLines.value = lines
    const cp = bundle.companyProfile
    const logos = cp.logos ?? []
    profileBasics.value = cp.basicInfos ?? []
    profileSeals.value = cp.seals ?? []
    const firstLineCurrency = lines.find((r) => r.priceCurrency != null)?.priceCurrency ?? null
    applyLetterheadSelection(firstLineCurrency)
    bankDefault.value = pickDefaultBankByRegion(cp.bankInfos, bundle.warehouseRegionType) ?? null
    warehouseRow.value = pickWarehouseForStockOut(cp.warehouses, bundle.stockOut.warehouseId) ?? null
    await loadSealBlobUrl(sealForCurrentLetterhead())
    const logo = pickReportLogoRow(logos)
    await loadCompanyLogoBlobUrl(logo)
  } catch (e) {
    errorMsg.value = getApiErrorMessage(e, t('stockOutInvoiceReport.loadFailed'))
    stockOut.value = null
    packingAddresses.value = null
    packingCode.value = null
    packingLines.value = []
    warehouseInfoAddress.value = ''
  } finally {
    loading.value = false
  }
}

watch(selectedBasicId, (id) => {
  if (!profileBasics.value.length) return
  const next = profileBasics.value.find((r) => r.id === id) ?? null
  if (next?.id === basicDefault.value?.id) return
  basicDefault.value = next
  void loadSealBlobUrl(sealForCurrentLetterhead())
})

function doPrint() {
  window.print()
}

onMounted(() => {
  document.body.classList.add(PO_REPORT_PRINT_BODY_CLASS)
  load()
})
watch([stockOutId, packingId], () => load())

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
