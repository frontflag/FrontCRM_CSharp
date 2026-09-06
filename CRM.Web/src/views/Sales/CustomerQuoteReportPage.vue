<template>
  <div class="po-report-page">
    <div class="toolbar no-print">
      <el-button @click="goBack">{{ t('customerQuoteReport.back') }}</el-button>
      <div class="toolbar__sp" />
      <div class="toolbar__opt">
        <el-radio-group v-model="reportLang" size="small" class="toolbar__lang">
          <el-radio-button label="zh">{{ t('customerQuoteReport.langZh') }}</el-radio-button>
          <el-radio-button label="en">{{ t('customerQuoteReport.langEn') }}</el-radio-button>
        </el-radio-group>
      </div>
      <el-button type="primary" :disabled="!ready" @click="doPrint">{{ t('customerQuoteReport.print') }}</el-button>
      <el-button type="primary" :disabled="!ready" :loading="exporting" @click="doExportPdf">
        {{ t('customerQuoteReport.exportPdf') }}
      </el-button>
    </div>

    <div v-if="emailVisible" id="cq-email-layer-debug" class="cq-email-layer no-print">
      <div class="cq-email-layer__panel" role="dialog" aria-modal="true">
        <div class="cq-email-layer__hd">{{ t('customerQuoteReport.emailTitle') }}</div>
        <div class="cq-email-layer__bd">
          <p class="email-tip">{{ t('customerQuoteReport.emailTip') }}</p>
          <p v-if="!mailSendReady" class="email-warn">
            {{ mailSendBlockTip }}
            <button type="button" class="email-warn__link" @click="goConfigureMailbox">
              {{ t('profilePage.mailboxSend.goConfigure') }}
            </button>
          </p>
          <div class="cq-mail-row">
            <label class="cq-mail-row__label">{{ t('customerQuoteReport.emailFrom') }}</label>
            <el-input
              :model-value="emailFrom"
              disabled
              :placeholder="t('customerQuoteReport.emailFromPlaceholder')"
            />
          </div>
          <div class="cq-mail-row">
            <label class="cq-mail-row__label">{{ t('customerQuoteReport.contact') }}</label>
            <el-select
              v-model="emailContactId"
              filterable
              clearable
              :teleported="false"
              :placeholder="t('customerQuoteReport.contactPlaceholder')"
              style="width: 100%"
              @change="onEmailContactChange"
            >
              <el-option
                v-for="c in contactOptions"
                :key="c.value"
                :label="c.label"
                :value="c.value"
              />
            </el-select>
          </div>
          <div class="cq-mail-row">
            <label class="cq-mail-row__label">
              <span class="cq-mail-row__req">*</span>{{ t('customerQuoteReport.emailTo') }}
            </label>
            <el-input
              v-model="emailTo"
              :placeholder="t('customerQuoteReport.emailToPlaceholder')"
              clearable
            />
          </div>
          <div class="cq-mail-row">
            <label class="cq-mail-row__label">
              <span class="cq-mail-row__req">*</span>{{ t('customerQuoteReport.emailSubject') }}
            </label>
            <el-input v-model="emailSubject" clearable />
          </div>
          <div class="cq-mail-row cq-mail-row--top">
            <label class="cq-mail-row__label">{{ t('customerQuoteReport.emailBody') }}</label>
            <el-input v-model="emailBody" type="textarea" :rows="5" />
          </div>
        </div>
        <div class="cq-email-layer__ft">
          <el-button @click="closeEmailDialog">{{ t('common.cancel') }}</el-button>
          <el-button
            type="primary"
            :loading="emailSending"
            :disabled="!mailSendReady || !emailTo.trim() || !emailSubject.trim()"
            @click="confirmSendEmail"
          >
            {{ t('customerQuoteReport.sendEmailAction') }}
          </el-button>
        </div>
      </div>
    </div>

    <div v-loading="loading" class="preview-wrap">
      <div v-if="errorMsg" class="err">{{ errorMsg }}</div>
      <div v-else-if="!ready" class="preview-loading">加载中…</div>
      <div v-else id="cq-report-print-root" ref="reportRoot" class="print-root">
        <CustomerQuoteReportBody v-bind="docBind" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, onUnmounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  customerQuoteApi,
  type CustomerQuoteBillTo,
  type CustomerQuoteRow
} from '@/api/customerQuote'
import { customerContactApi } from '@/api/customer'
import { type CompanyBasicRow, type CompanyLogoRow } from '@/api/companyProfile'
import { getInvoiceReportLabels, type InvoiceReportLang } from '@/components/stockOut/packingReportLabels'
import apiClient from '@/api/client'
import { fetchMailboxSendReady, fetchMyMailboxes, type MailboxSendBlockReason } from '@/api/userMailboxes'
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import { pickEnabledDefault, pickReportLogoRow } from '@/utils/reportLetterhead'
import { renderElementToPdfBlob, blobToDataUrl } from '@/utils/poReportPdf'
import { profileMailboxLocation } from '@/utils/profileMailboxLink'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatCustomerQuoteDisplayCode } from '@/utils/customerQuoteDisplay'
import CustomerQuoteReportBody from '@/components/Sales/customerQuoteReport/CustomerQuoteReportBody.vue'
import type { CustomerQuoteReportDocProps } from '@/components/Sales/customerQuoteReport/types'

const PO_REPORT_PRINT_BODY_CLASS = 'po-order-report-print'
const DEFAULT_CQ_REPORT_LOGO = '/purchase-order-template/logo.svg'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()

const loading = ref(true)
const errorMsg = ref('')
const quote = ref<CustomerQuoteRow | null>(null)
const billTo = ref<CustomerQuoteBillTo>({})
const basicDefault = ref<CompanyBasicRow | null>(null)
const companyLogoObjectUrl = ref<string | null>(null)
const reportRoot = ref<HTMLElement | null>(null)
const reportLang = ref<InvoiceReportLang>('en')

const exporting = ref(false)
const emailVisible = ref(false)
const emailFrom = ref('')
const emailTo = ref('')
const emailSubject = ref('')
const emailBody = ref('')
const emailContactId = ref('')
const emailSending = ref(false)
const mailSendReady = ref(false)
const mailSendBlockReason = ref<MailboxSendBlockReason | null>(null)

type ContactOption = { value: string; label: string; email: string }
const contactOptions = ref<ContactOption[]>([])

const quoteId = computed(() => String(route.params.id || ''))
const ready = computed(() => !!quote.value && !errorMsg.value && !loading.value)
const canSendEmail = computed(
  () => authStore.hasPermission('customer-quote.send') && quote.value?.status !== 2
)

const displayCode = computed(() => {
  const q = quote.value
  if (!q) return ''
  return formatCustomerQuoteDisplayCode(q.customerQuoteCode, q.versionNo)
})

const mailSendBlockTip = computed(() => {
  const code = mailSendBlockReason.value
  if (!code) return t('profilePage.mailboxSend.NoDefaultMailbox')
  const key = `profilePage.mailboxSend.${code}`
  const msg = t(key)
  return msg === key ? t('profilePage.mailboxSend.NoDefaultMailbox') : msg
})

function currencyCode(v: number | undefined | null): string {
  const n = Number(v)
  if (!Number.isFinite(n)) return CURRENCY_CODE_TO_TEXT[1]
  return CURRENCY_CODE_TO_TEXT[n] ?? CURRENCY_CODE_TO_TEXT[1]
}

function formatQty(n: number): string {
  return (n ?? 0).toLocaleString('zh-CN', { maximumFractionDigits: 4 })
}

function formatAmount(n: number): string {
  return (n ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatUnitPrice(n: number): string {
  return (n ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

function blank(v: string | null | undefined): string {
  return (v || '').trim()
}

function dash(v?: string | null) {
  return blank(v) || '—'
}

const docBind = computed<CustomerQuoteReportDocProps>(() => {
  const q = quote.value
  const seller = basicDefault.value
  const L = getInvoiceReportLabels(reportLang.value)
  const items = q?.items || []
  const lines = items.map((row) => {
    const qty = Number(row.quantity) || 0
    const price = Number(row.sendPrice) || 0
    const cur = currencyCode(row.sendCurrency)
    return {
      lineNo: row.lineNo,
      mpn: dash(row.mpn),
      customerMpn: blank(row.customerMpn),
      brand: dash(row.brand),
      customerBrand: blank(row.customerBrand),
      qty: formatQty(qty),
      unitPrice: formatUnitPrice(price),
      currency: cur,
      amount: formatAmount(qty * price),
      leadTime: dash(row.leadTime),
      dateCode: dash(row.dateCode),
      remark: dash(row.remark)
    }
  })

  const totalsMap = new Map<string, { amount: number; qty: number }>()
  for (const row of items) {
    const cur = currencyCode(row.sendCurrency)
    const amt = (Number(row.quantity) || 0) * (Number(row.sendPrice) || 0)
    const prev = totalsMap.get(cur) || { amount: 0, qty: 0 }
    totalsMap.set(cur, { amount: prev.amount + amt, qty: prev.qty + (Number(row.quantity) || 0) })
  }
  const totals = [...totalsMap.entries()].map(([currency, v]) => ({
    currency,
    amount: formatAmount(v.amount),
    qty: formatQty(v.qty)
  }))
  const totalQty = formatQty(items.reduce((acc, row) => acc + (Number(row.quantity) || 0), 0))

  const bt = billTo.value
  const billToLines = [
    dash(bt.company || q?.customerName),
    ...(blank(bt.companyEn) ? [bt.companyEn!.trim()] : []),
    `${L.attn}${dash(bt.attn || q?.contactName)}`,
    `${L.tel}${dash(bt.tel)}`,
    `${L.email}${dash(bt.email || q?.contactEmail)}`,
    dash(bt.address)
  ]

  const quoterLines = [
    dash(seller?.companyName),
    `${reportLang.value === 'zh' ? '销售员：' : 'Sales: '}${dash(q?.salesUserName)}`,
    `${L.tel}${dash(seller?.phone)}`,
    ...(blank(seller?.email) ? [`${L.email}${seller!.email.trim()}`] : []),
    dash(seller?.address)
  ]

  return {
    logoUrl: companyLogoObjectUrl.value ?? DEFAULT_CQ_REPORT_LOGO,
    headerCompanyName: seller?.companyName || '—',
    quotationNo: displayCode.value || '—',
    quoteDate: formatDisplayDate(q?.createTime) || '—',
    billToLines,
    quoterLines,
    lines,
    totals,
    totalQty,
    headerRemark: (q?.remark ?? '').trim(),
    signDate: formatDisplayDate(q?.createTime) || '',
    reportLang: reportLang.value
  }
})

function goBack() {
  const id = quoteId.value
  if (id) {
    void router.push({ name: 'CustomerQuoteEdit', params: { id } })
    return
  }
  router.back()
}

function goConfigureMailbox() {
  router.push(profileMailboxLocation(route.fullPath))
}

function contactLabelFromRaw(c: Record<string, unknown>): string {
  return (
    String(c.contactName ?? c.cName ?? c.eName ?? c.name ?? '').trim() ||
    t('customerQuoteReport.contact')
  )
}

function contactEmailFromRaw(c: Record<string, unknown>): string {
  const v = c.email ?? c.Email
  return typeof v === 'string' ? v.trim() : ''
}

async function loadContacts(customerId?: string | null) {
  const cid = (customerId || '').trim()
  if (!cid) {
    contactOptions.value = []
    return
  }
  try {
    const list = await customerContactApi.getContactsByCustomerId(cid)
    contactOptions.value = (Array.isArray(list) ? list : [])
      .map((c) => {
        const raw = c as unknown as Record<string, unknown>
        const id = String(c.id ?? raw.contactId ?? '').trim()
        if (!id) return null
        return { value: id, label: contactLabelFromRaw(raw), email: contactEmailFromRaw(raw) }
      })
      .filter((x): x is ContactOption => x != null)
  } catch {
    contactOptions.value = []
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

async function refreshMailSendReady() {
  try {
    const r = await fetchMailboxSendReady()
    mailSendReady.value = !!r.ready
    mailSendBlockReason.value = r.blockReason ?? null
  } catch {
    mailSendReady.value = false
    mailSendBlockReason.value = 'NoDefaultMailbox'
  }
  try {
    const boxes = await fetchMyMailboxes()
    const def =
      boxes.find((b) => b.isDefaultSend) ??
      boxes.find((b) => b.kind === 'platform' && b.verifyStatus === 'ok')
    emailFrom.value = (def?.address || '').trim()
  } catch {
    emailFrom.value = ''
  }
}

async function load() {
  loading.value = true
  errorMsg.value = ''
  if (companyLogoObjectUrl.value) {
    URL.revokeObjectURL(companyLogoObjectUrl.value)
    companyLogoObjectUrl.value = null
  }
  try {
    const id = quoteId.value
    if (!id) {
      errorMsg.value = t('customerQuoteReport.missingId')
      return
    }
    const data = await customerQuoteApi.getReportData(id)
    quote.value = data.quote
    billTo.value = data.billTo || {}
    const profile = data.companyProfile || {}
    const basics = (profile.basicInfos || []) as CompanyBasicRow[]
    const logos = (profile.logos || []) as CompanyLogoRow[]
    basicDefault.value = pickEnabledDefault(basics) ?? null
    loading.value = false
    await Promise.all([
      loadCompanyLogoBlobUrl(pickReportLogoRow(logos)),
      loadContacts(quote.value.customerId),
      refreshMailSendReady()
    ])
  } catch (e) {
    errorMsg.value = getApiErrorMessage(e, t('customerQuoteReport.loadFailed'))
    quote.value = null
  } finally {
    loading.value = false
  }
}

function logPreviewAction(action: 'print' | 'export') {
  const id = quoteId.value
  if (!id) return
  void customerQuoteApi.appendActionLog(id, { action }).catch(() => undefined)
}

function doPrint() {
  window.print()
  void logPreviewAction('print')
}

function getPdfDocumentElement(): HTMLElement | null {
  const wrap = reportRoot.value
  if (!wrap) return null
  return wrap.querySelector('.po-doc') as HTMLElement | null
}

async function doExportPdf() {
  const el = getPdfDocumentElement()
  if (!el) {
    ElMessage.error(t('customerQuoteReport.pdfNoDom'))
    return
  }
  exporting.value = true
  try {
    const blob = await renderElementToPdfBlob(el)
    const code = displayCode.value || 'quotation'
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${code}.pdf`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success(t('customerQuoteReport.exportOk'))
    void logPreviewAction('export')
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('customerQuoteReport.exportFailed')))
  } finally {
    exporting.value = false
  }
}

function onEmailLayerKeydown(ev: KeyboardEvent) {
  if (ev.key !== 'Escape') return
  if (emailSending.value) return
  closeEmailDialog()
}

function openEmailDialog() {
  if (!canSendEmail.value || emailVisible.value) return
  const code = displayCode.value || ''
  emailContactId.value = quote.value?.customerContactId || ''
  emailTo.value = (billTo.value.email || quote.value?.contactEmail || '').trim()
  if (!emailTo.value && emailContactId.value) {
    const row = contactOptions.value.find((c) => c.value === emailContactId.value)
    if (row?.email) emailTo.value = row.email.trim()
  }
  try {
    emailSubject.value = t('customerQuoteReport.defaultSubject', { code })
    emailBody.value = t('customerQuoteReport.defaultBody', { code })
  } catch {
    emailSubject.value = `客户报价单 ${code}`
    emailBody.value = `请查收附件：客户报价单 ${code}。`
  }
  emailVisible.value = true
  window.addEventListener('keydown', onEmailLayerKeydown)
}

defineExpose({ openEmailDialog })

function closeEmailDialog() {
  emailVisible.value = false
  emailSending.value = false
  window.removeEventListener('keydown', onEmailLayerKeydown)
}

function onEmailContactChange(contactId: string | null | undefined) {
  const id = (contactId || '').trim()
  if (!id) return
  const row = contactOptions.value.find((c) => c.value === id)
  if (row?.email) emailTo.value = row.email
}

async function confirmSendEmail() {
  const el = getPdfDocumentElement()
  const id = quoteId.value
  if (!id) return
  if (!mailSendReady.value) {
    ElMessage.warning(mailSendBlockTip.value)
    return
  }
  if (!el) {
    ElMessage.error(t('customerQuoteReport.pdfNoDom'))
    return
  }
  const to = emailTo.value.trim()
  if (!to) {
    ElMessage.warning(t('customerQuoteReport.emailToRequired'))
    return
  }
  emailSending.value = true
  try {
    const blob = await renderElementToPdfBlob(el)
    const dataUrl = await blobToDataUrl(blob)
    const code = displayCode.value || 'quotation'
    const updated = await customerQuoteApi.sendEmail(id, {
      to,
      pdfBase64: dataUrl,
      fileName: `${code}.pdf`,
      subject: emailSubject.value.trim(),
      body: emailBody.value
    })
    if (updated?.id) quote.value = { ...quote.value, ...updated }
    ElMessage.success(t('customerQuoteReport.sendOk'))
    closeEmailDialog()
  } catch (e) {
    const ax = e as { response?: { data?: { code?: string; message?: string } } }
    const code = ax.response?.data?.code
    if (code) {
      mailSendReady.value = code !== 'SmtpRejected' ? false : mailSendReady.value
      mailSendBlockReason.value = code === 'SmtpRejected' ? mailSendBlockReason.value : code
      const apiMsg = ax.response?.data?.message
      if (code === 'SmtpRejected' && apiMsg) {
        ElMessage.error(apiMsg)
      } else {
        const key = `profilePage.mailboxSend.${code}`
        const mapped = t(key)
        ElMessage.error(mapped !== key ? mapped : apiMsg || getApiErrorMessage(e, t('customerQuoteReport.sendFailed')))
      }
      if (code !== 'SmtpRejected') await refreshMailSendReady()
    } else {
      ElMessage.error(getApiErrorMessage(e, t('customerQuoteReport.sendFailed')))
    }
  } finally {
    emailSending.value = false
  }
}

onMounted(() => {
  document.body.classList.add(PO_REPORT_PRINT_BODY_CLASS)
  load()
})
watch(quoteId, () => load())

onBeforeUnmount(() => {
  document.body.classList.remove(PO_REPORT_PRINT_BODY_CLASS)
  window.removeEventListener('keydown', onEmailLayerKeydown)
})

onUnmounted(() => {
  if (companyLogoObjectUrl.value) URL.revokeObjectURL(companyLogoObjectUrl.value)
})
</script>

<style scoped lang="scss">
.po-report-page {
  min-height: 60vh;
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

.preview-wrap {
  min-height: 400px;
}

.preview-loading {
  color: #94a3b8;
  padding: 24px;
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

.cq-email-layer {
  display: flex;
  justify-content: center;
  margin: 0 0 16px;
  position: sticky;
  top: 8px;
  z-index: 40;
}

.cq-email-layer__panel {
  width: 560px;
  max-width: 100%;
  background: #fff;
  color: #303133;
  border: 1px solid #e5e7eb;
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.18);
  border-radius: 10px;
}

.cq-email-layer__hd {
  padding: 16px 20px 14px;
  font-size: 15px;
  font-weight: 600;
  color: #303133;
  border-bottom: 1px solid #ebeef5;
}

.cq-email-layer__bd {
  padding: 18px 20px;

  :deep(.el-input__wrapper),
  :deep(.el-select__wrapper),
  :deep(.el-textarea__inner) {
    background: #fff !important;
    box-shadow: 0 0 0 1px #dcdfe6 inset;
  }

  :deep(.el-input__inner),
  :deep(.el-textarea__inner),
  :deep(.el-select__placeholder),
  :deep(.el-select__selected-item) {
    color: #303133 !important;
  }
}

.cq-mail-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}

.cq-mail-row--top {
  align-items: flex-start;
}

.cq-mail-row__label {
  width: 130px;
  flex-shrink: 0;
  text-align: right;
  font-size: 14px;
  line-height: 32px;
  color: #606266;
}

.cq-mail-row__req {
  color: #f56c6c;
  margin-right: 4px;
}

.cq-mail-row :deep(.el-input),
.cq-mail-row :deep(.el-select),
.cq-mail-row :deep(.el-textarea) {
  flex: 1;
  min-width: 0;
}

.cq-email-layer__ft {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding: 12px 20px 16px;
  border-top: 1px solid #ebeef5;
}

.email-tip {
  margin: 0 0 12px;
  font-size: 13px;
  color: #606266;
}

.email-warn {
  margin: 0 0 12px;
  font-size: 13px;
  color: #b45309;
  background: #fff7ed;
  border: 1px solid #fed7aa;
  border-radius: 4px;
  padding: 8px 10px;
}

.email-warn__link {
  margin-left: 8px;
  border: none;
  background: none;
  color: #2563eb;
  cursor: pointer;
  padding: 0;
  font-size: 13px;
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

  .cq-email-layer {
    display: none !important;
  }
}
</style>
