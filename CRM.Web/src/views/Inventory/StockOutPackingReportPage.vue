<template>
  <div class="po-report-page">
    <div class="toolbar no-print">
      <el-button @click="router.back()">{{ t('stockOutPackingReport.back') }}</el-button>
      <ReportLetterheadSelect
        v-model="selectedBasicId"
        :options="letterheadOptions"
        :disabled="!ready"
      />
      <div class="toolbar__sp" />
      <div class="toolbar__opt">
        <el-radio-group v-model="inspectionVariant" size="small" class="toolbar__lang">
          <el-radio-button label="without-inspection">
            {{ t('stockOutPackingReport.variantWithoutInspection') }}
          </el-radio-button>
          <el-radio-button label="with-inspection">
            {{ t('stockOutPackingReport.variantWithInspection') }}
          </el-radio-button>
        </el-radio-group>
      </div>
      <div class="toolbar__opt">
        <el-radio-group v-model="pageOrientation" size="small" class="toolbar__lang">
          <el-radio-button label="landscape">{{ t('stockOutPackingReport.orientLandscape') }}</el-radio-button>
          <el-radio-button label="portrait">{{ t('stockOutPackingReport.orientPortrait') }}</el-radio-button>
        </el-radio-group>
      </div>
      <div v-if="!isPackingV2" class="toolbar__opt">
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
      <div v-else-if="ready" class="print-root" :class="{ 'print-root--landscape': pageOrientation === 'landscape' }">
        <component :is="reportView.component" v-bind="docBind" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { packingApi, packingDeliveryMethodLabel, packingDeliveryMethodLabelEn, packingDetailItemsToReportLines } from '@/api/packing'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import {
  pickReportRemarkLines,
  splitReportRemarkLines,
  type CompanyBasicRow,
  type CompanyLogoRow,
  type CompanySealRow,
  type CompanyWarehouseRow,
  type CompanyReportInfo
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
import { formatDisplayDate } from '@/utils/displayDateTime'
import type {
  PackingReportOrientation,
  PackingReportV2DocumentProps,
  PackingReportV2LandscapeLineVm,
  PackingReportV2LineVm,
  PackingReportV2Party,
  StockOutPackingLandscapeLineVm,
  StockOutPackingLineVm
} from '@/components/stockOut/packingReport/types'
import {
  formatPackingV2Carton,
  readPackingReportInspectionVariant,
  readPackingReportOrientation,
  writePackingReportInspectionVariant,
  writePackingReportOrientation,
  type PackingReportInspectionVariant
} from '@/components/stockOut/packingReport/types'
import {
  resolvePackingReportView,
  usesPackingReportV2
} from '@/components/stockOut/packingReport/resolvePackingReportSkin'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import { reportParamsApi, type ReportStyleVersion } from '@/api/reportParams'
import { renderPdfBlobFirstPageToPngDataUrl } from '@/utils/pdfSealToPng'
import { getApiErrorMessage } from '@/utils/apiError'
import type { PackingReportAddressPanel, StockOutDetailDto, PackingReportLine } from '@/api/stockOut'
import {
  getPackingReportLabels,
  PACKING_LIST_REPORT_LABELS_EN,
  PACKING_LIST_REPORT_LABELS_ZH,
  type InvoiceReportLang
} from '@/components/stockOut/packingReportLabels'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { normalizePackingAddrLines } from '@/utils/packingReportAddressLines'
import { resolvePackingReportConsigneeName } from '@/utils/packingReportCustomsConsignee'

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const PO_REPORT_PRINT_BODY_CLASS = 'po-order-report-print'
const PO_REPORT_PRINT_LANDSCAPE_CLASS = 'po-order-report-print-landscape'
const DEFAULT_REPORT_LOGO = '/purchase-order-template/logo.svg'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions, expressOptions } =
  useLogisticsFormDict()

const loading = ref(true)
const errorMsg = ref('')
const stockOut = ref<StockOutDetailDto | null>(null)
const packingAddresses = ref<PackingReportAddressPanel | null>(null)
const packingCode = ref<string | null>(null)
const withShipmentInspection = ref(false)
const basicDefault = ref<CompanyBasicRow | null>(null)
const profileBasics = ref<CompanyBasicRow[]>([])
const profileSeals = ref<CompanySealRow[]>([])
const selectedBasicId = ref('')
const letterheadOptions = ref<{ value: string; label: string }[]>([])
const warehouseRow = ref<CompanyWarehouseRow | null>(null)
const warehouseInfoAddress = ref('')
const packingDeliveryMethod = ref<number | null>(null)
const packingShipmentMethod = ref<string | null>(null)
const reportInfo = ref<CompanyReportInfo | null>(null)
const packingLines = ref<PackingReportLine[]>([])
const sealUrl = ref<string | null>(null)
const companyLogoObjectUrl = ref<string | null>(null)
const showSealOnReport = ref(true)
const reportLang = ref<InvoiceReportLang>('en')
const pageOrientation = ref<PackingReportOrientation>(readPackingReportOrientation())
const inspectionVariant = ref<PackingReportInspectionVariant>(readPackingReportInspectionVariant())
const styleVersion = ref<ReportStyleVersion>('V1')

const packingLabels = computed(() => getPackingReportLabels(reportLang.value))
const reportView = computed(() =>
  resolvePackingReportView(pageOrientation.value, LOGIN_TENANT_ID, styleVersion.value)
)
const isPackingV2 = computed(() =>
  usesPackingReportV2(LOGIN_TENANT_ID, styleVersion.value, pageOrientation.value)
)

let loadSeq = 0

const packingId = computed(() => String(route.params.packingId || ''))

const ready = computed(() => !!stockOut.value && !errorMsg.value && !loading.value)

/** 页脚备注：中文读 Remark.CN，英文读 Remark.EN */
const packingRemarks = computed(() =>
  pickReportRemarkLines(
    reportInfo.value?.packingList,
    reportLang.value === 'zh' ? 'zh-CN' : 'en-US'
  )
)

/** V2 中英对照：公司档案装箱备注中英都有则都印，都空则正文用默认双语句 */
function packingRemarksV2(): string[] {
  const remarks = reportInfo.value?.packingList
  const cn = splitReportRemarkLines(remarks?.remarkCn)
  const en = splitReportRemarkLines(remarks?.remarkEn)
  return [...cn, ...en]
}

function expressCompanyDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return ''
  const hit = expressOptions.value.find((o) => String(o.value) === c)
  return (hit?.label || c).trim()
}

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

function resolveShipMethodDisplay(
  so: StockOutDetailDto,
  shipmentMethod: string | null,
  deliveryMethod: number | null
): string {
  const code = (shipmentMethod || '').trim()
  if (code) {
    const hit = shipmentArrivalOptions.value.find((o) => String(o.value) === code)
    if (hit?.label) return hit.label
    return code
  }
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

function mapLandscapeLines(rows: PackingReportLine[]): StockOutPackingLandscapeLineVm[] {
  return rows.map((row, idx) => {
    const qtyNum = Number(row.qty) || 0
    const nwNum = row.nw != null && Number.isFinite(Number(row.nw)) ? Number(row.nw) : null
    const gwNum = row.gw != null && Number.isFinite(Number(row.gw)) ? Number(row.gw) : null
    const cartonRaw = (row.carton ?? '').trim()
    const cartonNum = cartonRaw && Number.isFinite(Number(cartonRaw)) ? Number(cartonRaw) : null
    return {
      index: idx + 1,
      customerPo: blankIfEmpty(row.customerPo),
      partNumber: blankIfEmpty(row.pn),
      customerPn: blankIfEmpty(row.customerPn),
      brand: blankIfEmpty(row.brand),
      qty: formatReportQty(qtyNum),
      dc: blankIfEmpty(row.dc),
      co: blankIfEmpty(row.co),
      cod: blankIfEmpty(row.cod),
      size: blankIfEmpty(row.size),
      nw: nwNum != null ? formatReportQty(nwNum) : '',
      gw: gwNum != null ? formatReportQty(gwNum) : '',
      carton: blankIfEmpty(row.carton),
      remark: blankIfEmpty(row.remark),
      qtyNum,
      nwNum,
      gwNum,
      cartonNum
    }
  })
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

function emptyV2Party(): PackingReportV2Party {
  return { name: '—', address: '—', contact: '—', phone: '—', email: '—' }
}

function mapV2PartyFromLines(
  lines: string[] | undefined,
  name: string,
  email?: string | null
): PackingReportV2Party {
  const src = (lines ?? []).map((x) => String(x ?? '').trim() || '—')
  return {
    name: cellText(name),
    address: cellText(src[1]),
    contact: cellText(src[2]),
    phone: cellText(src[3]),
    email: cellText(email)
  }
}

function mapV2LandscapeLines(rows: PackingReportLine[]): PackingReportV2LandscapeLineVm[] {
  return rows.map((row, idx) => {
    const nwNum = row.nw != null && Number.isFinite(Number(row.nw)) ? Number(row.nw) : null
    const gwNum = row.gw != null && Number.isFinite(Number(row.gw)) ? Number(row.gw) : null
    return {
      index: idx + 1,
      customerPo: cellText(row.customerPo),
      partNumber: cellText(row.pn),
      customerPn: cellText(row.customerPn),
      brand: cellText(row.brand),
      qty: formatReportQty(Number(row.qty) || 0),
      dc: cellText(row.dc),
      co: cellText(row.co),
      cod: cellText(row.cod),
      size: cellText(row.size),
      nw: nwNum != null ? formatReportQty(nwNum) : '—',
      gw: gwNum != null ? formatReportQty(gwNum) : '—',
      carton: formatPackingV2Carton(row.carton, idx + 1),
      remark: cellText(row.remark)
    }
  })
}

function mapV2Lines(rows: PackingReportLine[]): PackingReportV2LineVm[] {
  return rows.map((row, idx) => {
    const carton = formatPackingV2Carton(row.carton, idx + 1)
    const descParts = [blankIfEmpty(row.remark), blankIfEmpty(row.customerPn)].filter(Boolean)
    const nwNum = row.nw != null && Number.isFinite(Number(row.nw)) ? Number(row.nw) : null
    const gwNum = row.gw != null && Number.isFinite(Number(row.gw)) ? Number(row.gw) : null
    return {
      index: idx + 1,
      carton,
      mpn: cellText(row.pn),
      brand: cellText(row.brand),
      lotNo: cellText(row.dc),
      description: descParts.join(' / ') || '—',
      qty: formatReportQty(Number(row.qty) || 0),
      nw: nwNum != null ? `${formatReportQty(nwNum)} kg` : '—',
      gw: gwNum != null ? `${formatReportQty(gwNum)} kg` : '—',
      dimensions: cellText(row.size)
    }
  })
}

function v2QcItems(): string[] {
  const zh = PACKING_LIST_REPORT_LABELS_ZH.qcItems
  const en = PACKING_LIST_REPORT_LABELS_EN.qcItems
  return zh.map((z, i) => (en[i] ? `${z} / ${en[i]}` : z))
}

function buildV2Bind(so: StockOutDetailDto | null): PackingReportV2DocumentProps {
  const basic = basicDefault.value
  const emptyParty = emptyV2Party()
  if (!so) {
    return {
      headerCompanyName: '',
      packingNo: '',
      docDate: '',
      invoicePoNo: '—',
      incoterms: '—',
      transportMode: '—',
      shipper: emptyParty,
      consignee: emptyParty,
      lines: [],
      shipMarks: '—',
      departure: '—',
      destination: '—',
      carrierAwb: '—',
      remarks: packingRemarksV2(),
      totalCartons: '—',
      totalQty: '0',
      totalNw: '—',
      totalGw: '—',
      totalVolume: '—',
      withShipmentInspection: withShipmentInspection.value,
      qcItems: v2QcItems(),
      sealUrl: null,
      logoUrl: companyLogoObjectUrl.value ?? DEFAULT_REPORT_LOGO,
      showSeal: showSealOnReport.value,
      shipperSignDate: ''
    }
  }
  const customerLine = resolvePackingReportConsigneeName({
    stockOutType: so.stockOutType,
    customerName: so.customerName,
    shipToFirstLine: packingAddresses.value?.shipToLines?.[0],
    maskSaleSensitive: maskSaleSensitiveFields.value,
    customsBrokerConsignee: packingAddresses.value?.customsBrokerConsignee
  })
  const firstPo = packingLines.value.map((r) => (r.customerPo ?? '').trim()).find(Boolean)
  const invoicePo = firstPo || (so.sourceCode || '').trim() || (so.sellOrderItemCode || '').trim() || '—'
  const carrier = [expressCompanyDisplay(so.expressCompany), (so.courierTrackingNo || '').trim()]
    .filter(Boolean)
    .join(' ')
  const rows = packingLines.value
  const v2Lines = rows.length > 0 ? mapV2Lines(rows) : []
  const nwSum = rows.reduce((a, r) => a + (r.nw != null && Number.isFinite(Number(r.nw)) ? Number(r.nw) : 0), 0)
  const gwSum = rows.reduce((a, r) => a + (r.gw != null && Number.isFinite(Number(r.gw)) ? Number(r.gw) : 0), 0)
  const hasNw = rows.some((r) => r.nw != null && Number.isFinite(Number(r.nw)))
  const hasGw = rows.some((r) => r.gw != null && Number.isFinite(Number(r.gw)))
  const qtySum = rows.reduce((a, r) => a + (Number(r.qty) || 0), 0)
  return {
    headerCompanyName: (basic?.companyName || '').trim() || '—',
    packingNo: (packingCode.value || '').trim() || '—',
    docDate: formatDisplayDate(so.stockOutDate) || '—',
    invoicePoNo: invoicePo,
    incoterms: '—',
    transportMode: resolveShipMethodDisplay(so, packingShipmentMethod.value, packingDeliveryMethod.value),
    shipper: {
      name: (basic?.companyName || '').trim() || '—',
      address: (basic?.address || '').trim() || '—',
      contact: (basic?.legalPerson || '').trim() || '—',
      phone: (basic?.phone || '').trim() || '—',
      email: (basic?.email || '').trim() || '—'
    },
    consignee: mapV2PartyFromLines(
      packingAddresses.value?.shipToLines,
      customerLine,
      packingAddresses.value?.email
    ),
    lines: v2Lines,
    shipMarks: '—',
    departure: '—',
    destination: '—',
    carrierAwb: carrier || '—',
    remarks: packingRemarksV2(),
    totalCartons: v2Lines.length ? String(v2Lines.length) : '—',
    totalQty: rows.length ? formatReportQty(qtySum) : '—',
    totalNw: hasNw ? `${formatReportQty(nwSum)} kg` : '—',
    totalGw: hasGw ? `${formatReportQty(gwSum)} kg` : '—',
    totalVolume: '—',
    withShipmentInspection: withShipmentInspection.value,
    qcItems: v2QcItems(),
    sealUrl: sealUrl.value,
    logoUrl: companyLogoObjectUrl.value ?? DEFAULT_REPORT_LOGO,
    showSeal: showSealOnReport.value,
    shipperSignDate: formatDisplayDate(so.stockOutDate) || '—'
  }
}

function buildV2LandscapeBind(so: StockOutDetailDto | null): PackingReportV2DocumentProps {
  const base = buildV2Bind(so)
  const rows = packingLines.value
  return {
    ...base,
    orientation: 'landscape',
    landscapeLines: rows.length > 0 ? mapV2LandscapeLines(rows) : []
  }
}

const docBind = computed(() => {
  const so = stockOut.value
  if (isPackingV2.value) {
    return pageOrientation.value === 'landscape' ? buildV2LandscapeBind(so) : buildV2Bind(so)
  }

  const basic = basicDefault.value
  const wqc = withShipmentInspection.value
  const L = packingLabels.value
  const isLandscape = pageOrientation.value === 'landscape'
  const theme = reportView.value.landscapeTheme

  const baseEmpty = {
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
    notes: packingRemarks.value,
    withShipmentInspection: wqc,
    sealUrl: null as string | null,
    logoUrl: companyLogoObjectUrl.value ?? DEFAULT_REPORT_LOGO,
    showSeal: showSealOnReport.value,
    signDate: ''
  }

  if (!so) {
    return isLandscape
      ? { ...baseEmpty, theme, lines: [] as StockOutPackingLandscapeLineVm[] }
      : { ...baseEmpty, lines: [] as StockOutPackingLineVm[], totalQty: '0' }
  }

  const addr = packingAddresses.value
  const customerLine = resolvePackingReportConsigneeName({
    stockOutType: so.stockOutType,
    customerName: so.customerName,
    shipToFirstLine: packingAddresses.value?.shipToLines?.[0],
    maskSaleSensitive: maskSaleSensitiveFields.value,
    customsBrokerConsignee: packingAddresses.value?.customsBrokerConsignee
  })
  const billToLines = normalizePackingAddrLines(addr?.billToLines, customerLine, L)
  const shipToLines = normalizePackingAddrLines(addr?.shipToLines, customerLine, L)
  const shipperName = (basic?.companyName || '').trim() || '—'
  const shipMethodDisplay = resolveShipMethodDisplay(so, packingShipmentMethod.value, packingDeliveryMethod.value)
  const common = {
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
    notes: packingRemarks.value,
    withShipmentInspection: wqc,
    sealUrl: sealUrl.value,
    logoUrl: companyLogoObjectUrl.value ?? DEFAULT_REPORT_LOGO,
    showSeal: showSealOnReport.value,
    signDate: formatDisplayDate(so.stockOutDate) || '—'
  }

  if (isLandscape) {
    const lines =
      packingLines.value.length > 0
        ? mapLandscapeLines(packingLines.value)
        : ([
            {
              index: 1,
              customerPo: '',
              partNumber: blankIfEmpty(so.sourceCode || so.sellOrderItemCode),
              customerPn: '',
              brand: '',
              qty: formatReportQty(Number(so.totalQuantity) || 0),
              dc: '',
              co: '',
              cod: '',
              size: '',
              nw: '',
              gw: '',
              carton: '',
              remark: blankIfEmpty(so.remark),
              qtyNum: Number(so.totalQuantity) || 0,
              nwNum: null,
              gwNum: null,
              cartonNum: null
            }
          ] as StockOutPackingLandscapeLineVm[])
    return { ...common, theme, lines }
  }

  const lines = reportLinesForDoc(so)
  return { ...common, lines, totalQty: reportTotalQty(lines) }
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
  if (!id) {
    errorMsg.value = t('stockOutPackingReport.missingPackingId')
    loading.value = false
    return
  }

  const wantInspection = inspectionVariant.value === 'with-inspection'
  withShipmentInspection.value = wantInspection

  let seal: CompanySealRow | undefined
  let logo: CompanyLogoRow | undefined

  try {
    const [effectiveVersion, bundle] = await Promise.all([
      reportParamsApi.getEffectiveStyleVersion(),
      packingApi.getPackingReportBundle(id, wantInspection)
    ])
    if (seq !== loadSeq) return
    styleVersion.value = effectiveVersion
    if (!bundle?.stockOut) {
      errorMsg.value = t('stockOutPackingReport.notFound')
      stockOut.value = null
      packingAddresses.value = null
      packingCode.value = null
      warehouseInfoAddress.value = ''
      packingDeliveryMethod.value = null
      packingShipmentMethod.value = null
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
    packingShipmentMethod.value = (bundle.shipmentMethod || '').trim() || null
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
    profileBasics.value = cp.basicInfos ?? []
    profileSeals.value = cp.seals ?? []
    const firstLineCurrency = lines.find((r) => r.priceCurrency != null)?.priceCurrency ?? null
    applyLetterheadSelection(firstLineCurrency)
    warehouseRow.value = pickWarehouseForStockOut(cp.warehouses, bundle.stockOut.warehouseId) ?? null
    seal = sealForCurrentLetterhead()
    logo = pickReportLogoRow(logos)
  } catch (e) {
    if (seq !== loadSeq) return
    errorMsg.value = getApiErrorMessage(e, t('stockOutPackingReport.loadFailed'))
    stockOut.value = null
    packingAddresses.value = null
    packingCode.value = null
    warehouseInfoAddress.value = ''
    packingDeliveryMethod.value = null
    packingShipmentMethod.value = null
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

watch(selectedBasicId, (id) => {
  if (!profileBasics.value.length) return
  const next = profileBasics.value.find((r) => r.id === id) ?? null
  if (next?.id === basicDefault.value?.id) return
  basicDefault.value = next
  void loadSealBlobUrl(sealForCurrentLetterhead())
})

function syncPrintOrientationClass() {
  document.body.classList.toggle(PO_REPORT_PRINT_LANDSCAPE_CLASS, pageOrientation.value === 'landscape')
}

watch(pageOrientation, (v) => {
  writePackingReportOrientation(v)
  syncPrintOrientationClass()
})

watch(inspectionVariant, (v) => {
  writePackingReportInspectionVariant(v)
  load()
})

onMounted(() => {
  document.body.classList.add(PO_REPORT_PRINT_BODY_CLASS)
  syncPrintOrientationClass()
  void ensureLogisticsDict()
  load()
})
watch(packingId, () => load())

onBeforeUnmount(() => {
  document.body.classList.remove(PO_REPORT_PRINT_BODY_CLASS)
  document.body.classList.remove(PO_REPORT_PRINT_LANDSCAPE_CLASS)
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

.print-root--landscape {
  display: flex;
  justify-content: center;
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
