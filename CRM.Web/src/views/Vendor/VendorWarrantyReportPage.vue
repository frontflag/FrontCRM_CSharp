<template>
  <div class="po-report-page">
    <div class="toolbar no-print">
      <el-button @click="router.back()">{{ t('vendorWarrantyReport.back') }}</el-button>
      <div class="toolbar__sp" />
      <div class="toolbar__opt" :title="t('vendorWarrantyReport.sealHint')">
        <span class="toolbar__opt-lbl">{{ t('vendorWarrantyReport.sealOnReport') }}</span>
        <el-switch v-model="showSealOnReport" />
      </div>
      <el-button type="primary" :disabled="!ready" @click="doPrint">{{ t('vendorWarrantyReport.print') }}</el-button>
      <el-button type="primary" :disabled="!ready" :loading="exporting" @click="doExportPdf">
        {{ t('vendorWarrantyReport.exportPdf') }}
      </el-button>
    </div>

    <div v-loading="loading" class="preview-wrap">
      <div v-if="errorMsg" class="err">{{ errorMsg }}</div>
      <div v-else-if="ready" ref="reportRoot" class="print-root">
        <VendorWarrantyReportDocument v-bind="docBind" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { vendorApi } from '@/api/vendor'
import { fetchCompanyProfileForReport } from '@/api/companyProfile'
import type { CompanyBasicRow, CompanyLogoRow, CompanySealRow } from '@/api/companyProfile'
import type { Vendor } from '@/types/vendor'
import apiClient from '@/api/client'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { VENDOR_WARRANTY_PARAGRAPHS_ZH } from '@/constants/vendorWarrantyReportZh'
import { VENDOR_WARRANTY_PARAGRAPHS_EN } from '@/constants/vendorWarrantyReportEn'
import VendorWarrantyReportDocument from '@/components/Vendor/VendorWarrantyReportDocument.vue'
import { renderElementToPdfBlob } from '@/utils/poReportPdf'
import { renderPdfBlobFirstPageToPngDataUrl } from '@/utils/pdfSealToPng'
import { getApiErrorMessage } from '@/utils/apiError'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'

const PO_REPORT_PRINT_BODY_CLASS = 'po-order-report-print'
const DEFAULT_LOGO = '/purchase-order-template/logo.svg'

const route = useRoute()
const router = useRouter()
const { t, locale } = useI18n()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()

const loading = ref(true)
const errorMsg = ref('')
const vendor = ref<Vendor | null>(null)
const basicDefault = ref<CompanyBasicRow | null>(null)
const sealUrl = ref<string | null>(null)
const companyLogoObjectUrl = ref<string | null>(null)
const reportRoot = ref<HTMLElement | null>(null)
const exporting = ref(false)
const showSealOnReport = ref(true)

const vendorId = computed(() => String(route.params.id || ''))
const lang = computed(() => {
  const l = String(route.params.lang || '').toLowerCase()
  return l === 'en' || l === 'zh' ? l : ''
})

const ready = computed(() => !!vendor.value && !!lang.value && !errorMsg.value && !loading.value)

function pickDefault<T extends { isDefault?: boolean; enabled?: boolean }>(rows: T[] | undefined | null): T | undefined {
  if (!rows?.length) return undefined
  const d = rows.find((r) => r.isDefault && r.enabled !== false)
  return d ?? rows[0]
}

function pickReportLogoRow(rows: CompanyLogoRow[] | undefined | null): CompanyLogoRow | undefined {
  if (!rows?.length) return undefined
  const hasDoc = (r: CompanyLogoRow) => typeof r.documentId === 'string' && r.documentId.trim().length > 0
  const defWithDoc = rows.find((r) => r.isDefault && hasDoc(r))
  if (defWithDoc) return defWithDoc
  return rows.find((r) => hasDoc(r))
}

function pickReportSealRow(rows: CompanySealRow[] | undefined | null) {
  if (!rows?.length) return undefined
  const hasDoc = (r: CompanySealRow) => typeof r.documentId === 'string' && r.documentId.trim().length > 0
  const defWithDoc = rows.find((r) => r.isDefault && r.enabled !== false && hasDoc(r))
  if (defWithDoc) return defWithDoc
  const anyWithDoc = rows.find((r) => hasDoc(r))
  if (anyWithDoc) return anyWithDoc
  return rows.find((r) => r.isDefault) ?? rows[0]
}

const docBind = computed(() => {
  void locale.value
  const v = vendor.value
  const isEn = lang.value === 'en'
  const issuer = basicDefault.value?.companyName || '—'
  const redact = maskPurchaseSensitiveFields.value
  const zhName = redact ? '—' : (v?.officialName || v?.name || '').trim() || '—'
  const enName = redact ? '—' : (v?.englishOfficialName || v?.officialName || v?.name || '').trim() || '—'
  const vendorName = isEn ? enName : zhName
  const addr = redact ? '—' : (v?.officeAddress && String(v.officeAddress).trim()) || '—'
  const code = redact ? '' : (v?.code && String(v.code).trim()) || ''
  const today = formatDisplayDate(new Date()) || '—'
  const docNo = redact ? '—' : v?.code ? `QW-${v.code}` : `QW-${vendorId.value.slice(0, 8)}`

  return {
    issuerName: issuer,
    docTitle: isEn ? t('vendorWarrantyReport.titleEn') : t('vendorWarrantyReport.titleZh'),
    docNo,
    issueDate: today,
    noLabel: isEn ? t('vendorWarrantyReport.labelNoEn') : t('vendorWarrantyReport.labelNoZh'),
    dateLabel: isEn ? t('vendorWarrantyReport.labelDateEn') : t('vendorWarrantyReport.labelDateZh'),
    toNameLabel: isEn ? t('vendorWarrantyReport.labelToNameEn') : t('vendorWarrantyReport.labelToNameZh'),
    codeLabel: isEn ? t('vendorWarrantyReport.labelCodeEn') : t('vendorWarrantyReport.labelCodeZh'),
    addrLabel: isEn ? t('vendorWarrantyReport.labelAddrEn') : t('vendorWarrantyReport.labelAddrZh'),
    vendorName,
    vendorCode: code,
    vendorAddress: addr,
    paragraphs: isEn ? VENDOR_WARRANTY_PARAGRAPHS_EN : VENDOR_WARRANTY_PARAGRAPHS_ZH,
    issuerSignLabel: isEn ? t('vendorWarrantyReport.signIssuerEn') : t('vendorWarrantyReport.signIssuerZh'),
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
  const id = vendorId.value
  const lg = lang.value
  if (!id) {
    errorMsg.value = t('vendorWarrantyReport.missingId')
    loading.value = false
    return
  }
  if (!lg) {
    errorMsg.value = t('vendorWarrantyReport.badLang')
    loading.value = false
    return
  }
  try {
    const v = await vendorApi.getVendorById(id)
    vendor.value = v
    const profile = await fetchCompanyProfileForReport()
    const logos = profile?.logos ?? []
    const seals = profile?.seals ?? []
    basicDefault.value = pickDefault(profile.basicInfos) ?? null
    await loadSealBlobUrl(pickReportSealRow(seals))
    await loadCompanyLogoBlobUrl(pickReportLogoRow(logos))
  } catch (e) {
    errorMsg.value = getApiErrorMessage(e, t('vendorWarrantyReport.loadFailed'))
    vendor.value = null
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
  return wrap.querySelector('.wty-doc') as HTMLElement | null
}

async function doExportPdf() {
  const el = getPdfDocumentElement()
  if (!el) {
    ElMessage.error(t('vendorWarrantyReport.pdfNoDom'))
    return
  }
  exporting.value = true
  try {
    const blob = await renderElementToPdfBlob(el)
    const v = vendor.value
    const suffix = lang.value === 'en' ? 'EN' : 'ZH'
    const name = (maskPurchaseSensitiveFields.value ? 'vendor' : v?.code || 'vendor') + `-warranty-${suffix}`
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${name}.pdf`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success(t('vendorWarrantyReport.exportOk'))
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('vendorWarrantyReport.exportFailed')))
  } finally {
    exporting.value = false
  }
}

onMounted(() => {
  document.body.classList.add(PO_REPORT_PRINT_BODY_CLASS)
  load()
})
watch(() => [vendorId.value, lang.value] as const, () => load())

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
