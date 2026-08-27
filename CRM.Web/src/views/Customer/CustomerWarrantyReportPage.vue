<template>
  <div class="po-report-page">
    <div class="toolbar no-print">
      <el-button @click="router.back()">{{ t('customerWarrantyReport.back') }}</el-button>
      <ReportLetterheadSelect
        v-model="selectedBasicId"
        :options="letterheadOptions"
        :disabled="!ready"
      />
      <div class="toolbar__sp" />
      <div class="toolbar__opt">
        <el-radio-group v-model="reportLang" size="small" class="toolbar__lang">
          <el-radio-button label="zh">{{ t('customerWarrantyReport.langZh') }}</el-radio-button>
          <el-radio-button label="en">{{ t('customerWarrantyReport.langEn') }}</el-radio-button>
        </el-radio-group>
      </div>
      <div class="toolbar__opt" :title="t('customerWarrantyReport.sealHint')">
        <span class="toolbar__opt-lbl">{{ t('customerWarrantyReport.sealOnReport') }}</span>
        <el-switch v-model="showSealOnReport" />
      </div>
      <el-button type="primary" :disabled="!ready" @click="doPrint">{{ t('customerWarrantyReport.print') }}</el-button>
      <el-button type="primary" :disabled="!ready" :loading="exporting" @click="doExportPdf">
        {{ t('customerWarrantyReport.exportPdf') }}
      </el-button>
    </div>

    <div v-loading="loading" class="preview-wrap">
      <div v-if="errorMsg" class="err">{{ errorMsg }}</div>
      <div v-else-if="ready" ref="reportRoot" class="print-root">
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
import { customerApi } from '@/api/customer'
import { fetchCompanyProfileForReport } from '@/api/companyProfile'
import type { CompanyBasicRow, CompanyLogoRow, CompanySealRow } from '@/api/companyProfile'
import type { Customer } from '@/types/customer'
import ReportLetterheadSelect from '@/components/Common/ReportLetterheadSelect.vue'
import {
  letterheadKindOf,
  pickReportLogoRow,
  pickReportSealRow,
  resolveLetterheadSelection,
  tradeCurrencyToLetterheadPrefer
} from '@/utils/reportLetterhead'
import apiClient from '@/api/client'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { VENDOR_WARRANTY_PARAGRAPHS_ZH } from '@/constants/vendorWarrantyReportZh'
import { VENDOR_WARRANTY_PARAGRAPHS_EN } from '@/constants/vendorWarrantyReportEn'
import { resolveWarrantyLetterReportSkin } from '@/components/warrantyLetterReport/resolveWarrantyLetterReportSkin'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import { renderElementToPdfBlob } from '@/utils/poReportPdf'
import { renderPdfBlobFirstPageToPngDataUrl } from '@/utils/pdfSealToPng'
import { getApiErrorMessage } from '@/utils/apiError'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

const PO_REPORT_PRINT_BODY_CLASS = 'po-order-report-print'
const DEFAULT_LOGO = '/purchase-order-template/logo.svg'
const warrantySkin = resolveWarrantyLetterReportSkin(LOGIN_TENANT_ID)

const route = useRoute()
const router = useRouter()
const { t, locale } = useI18n()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const loading = ref(true)
const errorMsg = ref('')
const customer = ref<Customer | null>(null)
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
const reportLang = ref<'zh' | 'en'>('zh')

const customerId = computed(() => String(route.params.id || ''))

const ready = computed(() => !!customer.value && !errorMsg.value && !loading.value)

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

function formatCustomerAddress(c: Customer): string {
  const parts = [c.country, c.province, c.city, c.district, c.address, c.region]
    .map((x) => (x != null ? String(x).trim() : ''))
    .filter(Boolean)
  const joined = parts.join(' ').trim()
  return joined || '—'
}

const docBind = computed(() => {
  void locale.value
  const c = customer.value
  const isEn = reportLang.value === 'en'
  const issuer = basicDefault.value?.companyName || '—'
  const redact = maskSaleSensitiveFields.value
  const zhName = redact ? '—' : (c?.customerName || c?.customerShortName || '').trim() || '—'
  const enName = redact ? '—' : (c?.englishOfficialName || c?.customerName || c?.customerShortName || '').trim() || '—'
  const partyName = isEn ? enName : zhName
  const addr = redact || !c ? '—' : formatCustomerAddress(c)
  const code = redact ? '' : (c?.customerCode && String(c.customerCode).trim()) || ''
  const today = formatDisplayDate(new Date()) || '—'
  const docNo = redact ? '—' : c?.customerCode ? `QW-${c.customerCode}` : `QW-${customerId.value.slice(0, 8)}`

  return {
    issuerName: issuer,
    docTitle: isEn ? t('customerWarrantyReport.titleEn') : t('customerWarrantyReport.titleZh'),
    docNo,
    issueDate: today,
    noLabel: isEn ? t('customerWarrantyReport.labelNoEn') : t('customerWarrantyReport.labelNoZh'),
    dateLabel: isEn ? t('customerWarrantyReport.labelDateEn') : t('customerWarrantyReport.labelDateZh'),
    toNameLabel: isEn ? t('customerWarrantyReport.labelToNameEn') : t('customerWarrantyReport.labelToNameZh'),
    codeLabel: isEn ? t('customerWarrantyReport.labelCodeEn') : t('customerWarrantyReport.labelCodeZh'),
    addrLabel: isEn ? t('customerWarrantyReport.labelAddrEn') : t('customerWarrantyReport.labelAddrZh'),
    vendorName: partyName,
    vendorCode: code,
    vendorAddress: addr,
    paragraphs: isEn ? VENDOR_WARRANTY_PARAGRAPHS_EN : VENDOR_WARRANTY_PARAGRAPHS_ZH,
    issuerSignLabel: isEn ? t('customerWarrantyReport.signIssuerEn') : t('customerWarrantyReport.signIssuerZh'),
    sealUrl: sealUrl.value,
    logoUrl: companyLogoObjectUrl.value ?? DEFAULT_LOGO,
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
  void locale.value
  loading.value = true
  errorMsg.value = ''
  revokeSealUrlIfBlob()
  sealUrl.value = null
  if (companyLogoObjectUrl.value) {
    URL.revokeObjectURL(companyLogoObjectUrl.value)
    companyLogoObjectUrl.value = null
  }
  const id = customerId.value
  if (!id) {
    errorMsg.value = t('customerWarrantyReport.missingId')
    loading.value = false
    return
  }
  try {
    const c = await customerApi.getCustomerById(id)
    customer.value = c
    const profile = await fetchCompanyProfileForReport()
    const logos = profile?.logos ?? []
    profileBasics.value = profile?.basicInfos ?? []
    profileSeals.value = profile?.seals ?? []
    const ccy = (c as Customer & { currency?: string; tradeCurrency?: string }).currency
      ?? (c as Customer & { tradeCurrency?: string }).tradeCurrency
    applyLetterheadSelection(ccy ?? null)
    await loadSealBlobUrl(sealForCurrentLetterhead())
    await loadCompanyLogoBlobUrl(pickReportLogoRow(logos))
  } catch (e) {
    errorMsg.value = getApiErrorMessage(e, t('customerWarrantyReport.loadFailed'))
    customer.value = null
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
    ElMessage.error(t('customerWarrantyReport.pdfNoDom'))
    return
  }
  exporting.value = true
  try {
    const blob = await renderElementToPdfBlob(el)
    const c = customer.value
    const suffix = reportLang.value === 'en' ? 'EN' : 'ZH'
    const name = (maskSaleSensitiveFields.value ? 'customer' : c?.customerCode || 'customer') + `-warranty-${suffix}`
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${name}.pdf`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success(t('customerWarrantyReport.exportOk'))
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('customerWarrantyReport.exportFailed')))
  } finally {
    exporting.value = false
  }
}

onMounted(() => {
  document.body.classList.add(PO_REPORT_PRINT_BODY_CLASS)
  load()
})
watch(customerId, () => load())

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

  .toolbar,
  .no-print {
    display: none !important;
  }

  .print-root {
    background: #fff !important;
    padding: 0 !important;
    border-radius: 0 !important;
    overflow: visible !important;
  }

  .preview-wrap {
    min-height: 0 !important;
  }
}
</style>
