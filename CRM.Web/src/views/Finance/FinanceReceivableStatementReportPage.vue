<template>
  <div class="po-report-page">
    <div class="toolbar no-print">
      <el-button @click="goBack">{{ t('financeReceivableStatement.back') }}</el-button>
      <div class="toolbar__sp" />
      <div class="toolbar__opt">
        <el-radio-group v-model="reportLang" size="small" class="toolbar__lang">
          <el-radio-button label="zh">{{ t('financeReceivableStatement.langZh') }}</el-radio-button>
          <el-radio-button label="en">{{ t('financeReceivableStatement.langEn') }}</el-radio-button>
        </el-radio-group>
      </div>
      <el-button type="primary" :disabled="!ready" @click="doPrint">{{ t('financeReceivableStatement.print') }}</el-button>
      <el-button type="primary" :disabled="!ready" :loading="exporting" @click="doExportPdf">
        {{ t('financeReceivableStatement.exportPdf') }}
      </el-button>
    </div>

    <div v-loading="loading" class="preview-wrap">
      <div v-if="errorMsg" class="err">{{ errorMsg }}</div>
      <div v-else-if="ready" id="stmt-report-print-root" ref="reportRoot" class="print-root">
        <FinanceReceivableStatementReportBody v-bind="reportBind" />
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
  financeReceivableStatementApi,
  type FinanceReceivableStatementDetail,
  type FinanceReceivableStatementLine
} from '@/api/financeReceivableStatement'
import { fetchCompanyProfileForReport, type CompanyBasicRow } from '@/api/companyProfile'
import apiClient from '@/api/client'
import { getApiErrorMessage } from '@/utils/apiError'
import { listAmountCurrencyIso, formatTotalAmountNumber } from '@/utils/moneyFormat'
import { pickEnabledDefault, pickReportLogoRow } from '@/utils/reportLetterhead'
import { renderElementToPdfBlob } from '@/utils/poReportPdf'
import { lastCalendarMonthRange, todayYmd } from '@/utils/receivableStatementPeriod'
import FinanceReceivableStatementReportBody from '@/components/Finance/FinanceReceivableStatementReportBody.vue'

const PO_REPORT_PRINT_BODY_CLASS = 'po-order-report-print'
const DEFAULT_REPORT_LOGO = '/purchase-order-template/logo.svg'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const loading = ref(false)
const exporting = ref(false)
const errorMsg = ref('')
const detail = ref<FinanceReceivableStatementDetail | null>(null)
const reportRoot = ref<HTMLElement | null>(null)
const reportLang = ref<'zh' | 'en'>(route.query.lang === 'en' ? 'en' : 'zh')
const basicDefault = ref<CompanyBasicRow | null>(null)
const companyLogoObjectUrl = ref<string | null>(null)

const ready = computed(() => !!detail.value && !errorMsg.value)

const customerDisplayName = computed(() => {
  const c = detail.value?.customer
  if (!c) return '—'
  return c.customerName || c.customerEnglishName || c.customerCode || '—'
})

const paymentDaysText = computed(() => {
  const days = detail.value?.customer.paymentDays
  if (days == null || !detail.value?.customer.canViewFull) return '—'
  return t('financeReceivableStatement.paymentDaysValue', { n: days })
})

const creditLimitText = computed(() => {
  const v = detail.value?.customer.creditLimit
  if (v == null || !detail.value?.customer.canViewFull) return '—'
  return `${formatTotalAmountNumber(v)}（${t('financeReceivableStatement.creditFromMaster')}）`
})

const reportBind = computed(() => {
  const d = detail.value
  const lang = reportLang.value
  const currency = listAmountCurrencyIso(d?.statement.currency)
  const customerLines = [
    customerDisplayName.value,
    `${lang === 'en' ? 'Code' : '客户编号'} ${d?.customer.customerCode || '—'}`,
    d?.customer.canViewFull
      ? `${lang === 'en' ? 'Contact' : '联系人'} ${d.customer.contactName || '—'} · ${d.customer.contactPhone || '—'}`
      : ''
  ].filter(Boolean)
  const termLines = [
    `${lang === 'en' ? 'Period' : '对账期间'} ${d?.statement.periodFrom || '—'} — ${d?.statement.periodTo || '—'}`,
    `${lang === 'en' ? 'Payment terms' : '账期'} ${paymentDaysText.value}`,
    `${lang === 'en' ? 'Credit limit' : '信用额度'} ${creditLimitText.value}`,
    `${lang === 'en' ? 'Aging cutoff' : '账龄截止'} ${d?.statement.agingCutoff || '—'}`
  ]
  return {
    reportLang: lang,
    logoUrl: companyLogoObjectUrl.value ?? DEFAULT_REPORT_LOGO,
    headerCompanyName: basicDefault.value?.companyName || '—',
    generatedOn: d?.statement.generatedOn || '—',
    currency,
    customerLines,
    termLines,
    opening: formatPrintAmount(d?.statement.opening ?? 0),
    periodIncrease: formatPrintAmount(d?.statement.periodIncrease ?? 0),
    periodReceived: formatPrintAmount(d?.statement.periodReceived ?? 0),
    ending: formatPrintAmount(d?.statement.ending ?? 0),
    lines: (d?.lines ?? []).map(row => ({
      date: row.date,
      docNo: displayDocNo(row),
      summary: lineSummary(row, lang),
      increase: row.increaseAmount == null ? '' : formatPrintAmount(row.increaseAmount),
      received: row.receivedAmount == null ? '' : formatPrintAmount(row.receivedAmount),
      balance: formatPrintAmount(row.balance)
    }))
  }
})

function lineSummary(row: FinanceReceivableStatementLine, lang: 'zh' | 'en') {
  if (row.lineType === 'opening') return lang === 'en' ? 'Opening AR balance' : '期初应收余额'
  if (row.lineType === 'receipt') {
    if (!row.docNo) return lang === 'en' ? 'Advance write-off' : '预收核销'
    return lang === 'en' ? `Receipt write-off ${row.docNo}` : `收款核销 ${row.docNo}`
  }
  return row.summary || '—'
}

function displayDocNo(row: FinanceReceivableStatementLine) {
  if (row.lineType === 'opening') return ''
  if (row.lineType === 'receipt' && !row.docNo) return t('financeReceivableStatement.advanceDoc')
  return row.docNo || '—'
}

function formatPrintAmount(n: number) {
  return Number(n).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function currentPeriod(): [string, string] {
  const qFrom = typeof route.query.from === 'string' ? route.query.from : ''
  const qTo = typeof route.query.to === 'string' ? route.query.to : ''
  if (qFrom && qTo) return [qFrom, qTo]
  return lastCalendarMonthRange()
}

function currentAging() {
  return typeof route.query.aging === 'string' && route.query.aging ? route.query.aging : todayYmd()
}

async function loadDetail() {
  const customerId = String(route.params.customerId || '')
  const currency = Number(route.params.currency)
  if (!customerId || !Number.isFinite(currency)) {
    errorMsg.value = t('financeReceivableStatement.notFound')
    return
  }
  const [from, to] = currentPeriod()
  loading.value = true
  errorMsg.value = ''
  try {
    detail.value = await financeReceivableStatementApi.getDetail(customerId, currency, {
      from,
      to,
      aging: currentAging()
    })
  } catch (e) {
    detail.value = null
    errorMsg.value = getApiErrorMessage(e, t('financeReceivableStatement.notFound'))
  } finally {
    loading.value = false
  }
}

async function loadLetterhead() {
  if (companyLogoObjectUrl.value) {
    URL.revokeObjectURL(companyLogoObjectUrl.value)
    companyLogoObjectUrl.value = null
  }
  try {
    const profile = await fetchCompanyProfileForReport()
    basicDefault.value = pickEnabledDefault(profile.basicInfos) ?? null
    const logo = pickReportLogoRow(profile.logos)
    if (logo?.documentId) {
      const blob = await apiClient.getBlob(`/api/v1/documents/${logo.documentId}/download`)
      if (blob.size > 0) companyLogoObjectUrl.value = URL.createObjectURL(blob)
    }
  } catch {
    basicDefault.value = null
  }
}

function goBack() {
  void router.push({
    name: 'FinanceReceivableStatementDetail',
    params: route.params,
    query: {
      from: typeof route.query.from === 'string' ? route.query.from : undefined,
      to: typeof route.query.to === 'string' ? route.query.to : undefined,
      aging: typeof route.query.aging === 'string' ? route.query.aging : undefined
    }
  })
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
  if (!el) return
  exporting.value = true
  try {
    const blob = await renderElementToPdfBlob(el)
    const code = detail.value?.customer.customerCode || 'customer'
    const ccy = listAmountCurrencyIso(detail.value?.statement.currency)
    const [from, to] = currentPeriod()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `客户对账单-${code}-${ccy}-${from}-${to}.pdf`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success(t('financeReceivableStatement.exportOk'))
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('financeReceivableStatement.exportFailed')))
  } finally {
    exporting.value = false
  }
}

watch(reportLang, lang => {
  void router.replace({
    name: 'FinanceReceivableStatementPreview',
    params: route.params,
    query: { ...route.query, lang }
  })
})

onMounted(() => {
  document.body.classList.add(PO_REPORT_PRINT_BODY_CLASS)
  void loadDetail()
  void loadLetterhead()
})

onBeforeUnmount(() => {
  document.body.classList.remove(PO_REPORT_PRINT_BODY_CLASS)
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
