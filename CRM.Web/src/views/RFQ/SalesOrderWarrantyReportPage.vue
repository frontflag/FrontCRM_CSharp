<template>
  <div class="po-report-page">
    <div class="toolbar no-print">
      <el-button @click="router.back()">{{ t('salesOrderWarrantyReport.back') }}</el-button>
      <ReportLetterheadSelect
        v-model="selectedBasicId"
        :options="letterheadOptions"
        :disabled="!ready"
      />
      <div class="toolbar__sp" />
      <div class="toolbar__opt" :title="t('salesOrderWarrantyReport.sealHint')">
        <span class="toolbar__opt-lbl">{{ t('salesOrderWarrantyReport.sealOnReport') }}</span>
        <el-switch v-model="showSealOnReport" />
      </div>
      <el-button type="primary" :disabled="!ready" @click="doPrint">{{ t('salesOrderWarrantyReport.print') }}</el-button>
      <el-button type="primary" :disabled="!ready" :loading="exporting" @click="doExportPdf">
        {{ t('salesOrderWarrantyReport.exportPdf') }}
      </el-button>
    </div>

    <div v-loading="loading" class="preview-wrap">
      <div v-if="errorMsg" class="err">{{ errorMsg }}</div>
      <div v-else-if="ready" id="so-warranty-print-root" ref="reportRoot" class="print-root">
        <component :is="warrantySkin" v-bind="docBind" />
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
import { customerApi } from '@/api/customer'
import {
  type CompanyBasicRow,
  type CompanyLogoRow,
  type CompanySealRow,
  fetchCompanyProfileForReport
} from '@/api/companyProfile'
import ReportLetterheadSelect from '@/components/Common/ReportLetterheadSelect.vue'
import {
  firstLineTradeCurrency,
  letterheadKindOf,
  pickReportLogoRow,
  pickReportSealRow,
  resolveLetterheadSelection,
  tradeCurrencyToLetterheadPrefer
} from '@/utils/reportLetterhead'
import apiClient from '@/api/client'
import {
  resolveSalesOrderWarrantyReportSkin
} from '@/components/SalesOrder/salesOrderWarrantyReport/resolveSalesOrderWarrantyReportSkin'
import { reportParamsApi, type ReportStyleVersion } from '@/api/reportParams'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import type { SalesOrderWarrantyLang, SoWarrantyLineVm } from '@/components/SalesOrder/salesOrderWarrantyReport/types'
import {
  SALES_ORDER_WARRANTY_INTRO_ZH,
  SALES_ORDER_WARRANTY_NOTES_ZH,
  SALES_ORDER_WARRANTY_TITLE_ZH,
  SALES_ORDER_WARRANTY_SUBTITLE_ZH
} from '@/constants/salesOrderWarrantyReportZh'
import {
  SALES_ORDER_WARRANTY_INTRO_EN,
  SALES_ORDER_WARRANTY_NOTES_EN,
  SALES_ORDER_WARRANTY_NOTES_HEADING_EN,
  SALES_ORDER_WARRANTY_NOTES_AFTER_EN,
  SALES_ORDER_WARRANTY_GOODS_LEAD_EN,
  SALES_ORDER_WARRANTY_TITLE_EN,
  SALES_ORDER_WARRANTY_SUBTITLE_EN
} from '@/constants/salesOrderWarrantyReportEn'
import { salesOrderReportAllowed } from '@/constants/salesOrderStatus'
import { renderElementToPdfBlob } from '@/utils/poReportPdf'
import { renderPdfBlobFirstPageToPngDataUrl } from '@/utils/pdfSealToPng'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const PO_REPORT_PRINT_BODY_CLASS = 'po-order-report-print'
const DEFAULT_LOGO = '/purchase-order-template/logo.svg'

const styleVersion = ref<ReportStyleVersion>('V1')
const warrantySkin = computed(() =>
  resolveSalesOrderWarrantyReportSkin(LOGIN_TENANT_ID, styleVersion.value)
)

const loading = ref(true)
const errorMsg = ref('')
const order = ref<Record<string, any> | null>(null)
const customer = ref<Record<string, any> | null>(null)
const basicDefault = ref<CompanyBasicRow | null>(null)
const profileBasics = ref<CompanyBasicRow[]>([])
const profileSeals = ref<CompanySealRow[]>([])
const selectedBasicId = ref('')
const letterheadOptions = ref<{ value: string; label: string }[]>([])
const sealUrl = ref<string | null>(null)
const companyLogoObjectUrl = ref<string | null>(null)
const reportRoot = ref<HTMLElement | null>(null)
const exporting = ref(false)
const showSealOnReport = ref(true)

const soId = computed(() => String(route.params.id || ''))
const lang = computed<SalesOrderWarrantyLang | null>(() => {
  const l = String(route.params.lang || '').toLowerCase()
  if (l === 'zh' || l === 'en') return l
  return null
})
const ready = computed(() => !!order.value && !!lang.value && !errorMsg.value && !loading.value)

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

function dash(v: unknown): string {
  const s = String(v ?? '').trim()
  return s || '—'
}

function formatQty(n: number): string {
  return (n ?? 0).toLocaleString('zh-CN', { maximumFractionDigits: 4 })
}

function pickCustomerContact(c: Record<string, any> | null): { name: string; phone: string; address: string } {
  if (!c) return { name: '—', phone: '—', address: '—' }
  const contacts = Array.isArray(c.contacts) ? c.contacts : []
  const contact =
    contacts.find((x: any) => x.isDefault || x.isPrimary || x.IsDefault || x.IsPrimary) ?? contacts[0]
  const name = dash(contact?.contactName ?? contact?.name ?? c.contactName)
  const phone = dash(contact?.phone ?? contact?.tel ?? c.phone ?? c.tel)
  const addresses = Array.isArray(c.addresses) ? c.addresses : []
  const addrRow =
    addresses.find((x: any) => x.isDefault || x.IsDefault) ?? addresses[0]
  const address = dash(
    addrRow?.streetAddress ?? addrRow?.address ?? c.address ?? c.companyAddress
  )
  return { name, phone, address }
}

const docBind = computed(() => {
  const isEn = lang.value === 'en'
  const o = order.value
  const seller = basicDefault.value
  const cust = customer.value
  const partyB = pickCustomerContact(cust)

  const items = (o?.items as any[]) || []
  const lines: SoWarrantyLineVm[] = items.map((row) => {
    const qty = Number(row.qty) || 0
    const dc = String(row.dateCode ?? row.DateCode ?? '').trim()
    return {
      pn: dash(row.pn),
      brand: dash(row.brand),
      qty: formatQty(qty),
      dateCode: dc || '—',
      customerPn: dash(row.customerPn ?? row.CustomerPn),
      customerSo: dash(row.customerSo ?? row.CustomerSo)
    }
  })

  const partyARep = maskSaleSensitiveFields.value
    ? '—'
    : dash(o?.salesUserRealName ?? o?.SalesUserRealName ?? o?.salesUserName)

  const partyBName = maskSaleSensitiveFields.value ? '—' : dash(o?.customerName ?? cust?.customerName ?? cust?.name)
  const partyBRep = maskSaleSensitiveFields.value ? '—' : partyB.name
  const partyBPhone = maskSaleSensitiveFields.value ? '—' : partyB.phone
  const partyBAddress = maskSaleSensitiveFields.value ? '—' : partyB.address

  return {
    lang: (lang.value || 'zh') as SalesOrderWarrantyLang,
    orderCode: maskSaleSensitiveFields.value
      ? '—'
      : dash(o?.sellOrderCode ?? o?.SellOrderCode),
    orderDate: formatDisplayDate(o?.createTime ?? o?.CreateTime) || '—',
    companyName: dash(seller?.companyName),
    companyAddress: dash(seller?.address),
    docTitle: isEn ? SALES_ORDER_WARRANTY_TITLE_EN : SALES_ORDER_WARRANTY_TITLE_ZH,
    docSubtitle: isEn ? SALES_ORDER_WARRANTY_SUBTITLE_EN : SALES_ORDER_WARRANTY_SUBTITLE_ZH,
    partyALabel: isEn ? 'Party A: ' : '甲方：',
    partyBLabel: isEn ? 'Party B: ' : '乙方：',
    partyAName: dash(seller?.companyName),
    partyBName,
    introText: isEn ? SALES_ORDER_WARRANTY_INTRO_EN : SALES_ORDER_WARRANTY_INTRO_ZH,
    notesHeading: isEn ? SALES_ORDER_WARRANTY_NOTES_HEADING_EN : '说明：',
    notes: isEn ? SALES_ORDER_WARRANTY_NOTES_EN : SALES_ORDER_WARRANTY_NOTES_ZH,
    notesAfter: isEn ? SALES_ORDER_WARRANTY_NOTES_AFTER_EN : '',
    goodsLead: isEn ? SALES_ORDER_WARRANTY_GOODS_LEAD_EN : '',
    colPn: isEn ? 'P/N' : '型号',
    colBrand: isEn ? 'Brand' : '品牌',
    colQty: isEn ? 'Qty' : '数量',
    colDc: 'DC',
    colCustomerPn: isEn ? 'Customer P/N' : '客户型号',
    colCustomerSo: isEn ? 'Customer PO' : '客户订单号',
    lines,
    emptyLinesHint: isEn ? 'No line items' : '暂无明细',
    signRepLabel: isEn ? 'Representative: ' : '代表人：',
    signPhoneLabel: isEn ? 'Tel: ' : '电话：',
    signAddrLabel: isEn ? 'Address: ' : '地址：',
    partyARep,
    partyAPhone: dash(seller?.phone),
    partyAAddress: dash(seller?.address),
    partyBRep,
    partyBPhone,
    partyBAddress,
    logoUrl: companyLogoObjectUrl.value ?? DEFAULT_LOGO,
    sealUrl: sealUrl.value,
    showSeal: showSealOnReport.value
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
    if (mime === 'application/pdf' || mime === 'application/x-pdf' || /\.pdf$/i.test(String(seal.fileName || ''))) {
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
      /* ignore */
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
  order.value = null
  customer.value = null

  const id = soId.value
  const lg = lang.value
  if (!id) {
    errorMsg.value = t('salesOrderWarrantyReport.missingId')
    loading.value = false
    return
  }
  if (!lg) {
    errorMsg.value = t('salesOrderWarrantyReport.badLang')
    loading.value = false
    return
  }

  try {
    const o = (await salesOrderApi.getById(id)) as Record<string, any>
    const st = Number(o?.status ?? o?.Status)
    if (!salesOrderReportAllowed(st)) {
      errorMsg.value = t('salesOrderWarrantyReport.reportNotAllowed')
      loading.value = false
      return
    }
    order.value = o

    const cid = String(o?.customerId ?? o?.CustomerId ?? '').trim()
    if (cid) {
      try {
        customer.value = (await customerApi.getCustomerById(cid)) as Record<string, any>
      } catch {
        customer.value = null
      }
    }

    styleVersion.value = await reportParamsApi.getEffectiveStyleVersion()

    const profile = await fetchCompanyProfileForReport()
    profileBasics.value = profile.basicInfos ?? []
    profileSeals.value = profile.seals ?? []
    const items = (o?.items ?? o?.Items ?? []) as Array<{ currency?: number | string | null }>
    applyLetterheadSelection(firstLineTradeCurrency(items, o?.currency ?? o?.Currency))
    await loadSealBlobUrl(sealForCurrentLetterhead())
    await loadCompanyLogoBlobUrl(pickReportLogoRow(profile.logos))
  } catch (e) {
    errorMsg.value = getApiErrorMessage(e, t('salesOrderWarrantyReport.loadFailed'))
    order.value = null
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

function getPdfDocumentElement(): HTMLElement | null {
  const wrap = reportRoot.value
  if (!wrap) return null
  return wrap.querySelector('.wty-doc') as HTMLElement | null
}

async function doExportPdf() {
  const el = getPdfDocumentElement()
  if (!el) {
    ElMessage.error(t('salesOrderWarrantyReport.pdfNoDom'))
    return
  }
  exporting.value = true
  try {
    const blob = await renderElementToPdfBlob(el)
    const code = maskSaleSensitiveFields.value ? 'SO' : String(order.value?.sellOrderCode || 'SO')
    const suffix = lang.value === 'en' ? 'EN' : 'ZH'
    const name = `${code}-warranty-${suffix}`
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${name}.pdf`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success(t('salesOrderWarrantyReport.exportOk'))
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('salesOrderWarrantyReport.exportFailed')))
  } finally {
    exporting.value = false
  }
}

function onBeforePrint() {
  document.body.classList.add(PO_REPORT_PRINT_BODY_CLASS)
}
function onAfterPrint() {
  document.body.classList.remove(PO_REPORT_PRINT_BODY_CLASS)
}

onMounted(() => {
  window.addEventListener('beforeprint', onBeforePrint)
  window.addEventListener('afterprint', onAfterPrint)
  void load()
})

onBeforeUnmount(() => {
  window.removeEventListener('beforeprint', onBeforePrint)
  window.removeEventListener('afterprint', onAfterPrint)
  onAfterPrint()
})

onUnmounted(() => {
  revokeSealUrlIfBlob()
  if (companyLogoObjectUrl.value) URL.revokeObjectURL(companyLogoObjectUrl.value)
})

watch([soId, lang], () => void load())
</script>

<style scoped lang="scss">
.po-report-page {
  min-height: 100%;
  background: #e8e8e8;
}

.toolbar {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 16px;
  background: #fff;
  border-bottom: 1px solid #e5e5e5;
  position: sticky;
  top: 0;
  z-index: 5;
}

.toolbar__sp {
  flex: 1;
}

.toolbar__opt {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-right: 8px;
}

.toolbar__opt-lbl {
  font-size: 13px;
  color: #606266;
}

.preview-wrap {
  padding: 16px;
  min-height: 60vh;
}

.print-root {
  display: flex;
  justify-content: center;
}

.err {
  color: #c45656;
  padding: 24px;
  text-align: center;
}

@media print {
  .no-print {
    display: none !important;
  }
  .preview-wrap {
    padding: 0;
    background: #fff;
  }
  .print-root {
    display: block;
  }
}
</style>
