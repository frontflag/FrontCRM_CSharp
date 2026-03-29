<template>
  <div class="po-report-page">
    <div class="toolbar no-print">
      <el-button @click="router.back()">返回</el-button>
      <div class="toolbar__sp" />
      <div class="toolbar__opt" title="关闭后预览、打印、导出 PDF、发邮件均不包含买方电子印章图">
        <span class="toolbar__opt-lbl">报表含印章</span>
        <el-switch v-model="showSealOnReport" />
      </div>
      <el-button type="primary" :disabled="!ready" @click="doPrint">打印</el-button>
      <el-button type="primary" :disabled="!ready" :loading="exporting" @click="doExportPdf">导出 PDF</el-button>
      <el-button type="primary" :disabled="!ready" @click="openEmailDialog">发送邮件</el-button>
    </div>

    <div v-loading="loading" class="preview-wrap">
      <div v-if="errorMsg" class="err">{{ errorMsg }}</div>
      <div v-else-if="ready" id="po-report-print-root" ref="reportRoot" class="print-root">
        <PurchaseOrderReportDocument v-bind="docBind" />
      </div>
    </div>

    <el-dialog v-model="emailVisible" title="发送采购订单报表" width="480px" class="no-print" @closed="onEmailClosed">
      <p class="email-tip">将当前预览的采购订单 PDF 作为附件发送。</p>
      <el-form label-width="100px" @submit.prevent>
        <el-form-item label="收件人邮箱" required>
          <el-input v-model="emailTo" type="email" placeholder="supplier@example.com" clearable />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="emailVisible = false">取消</el-button>
        <el-button type="primary" :loading="emailSending" :disabled="!emailTo.trim()" @click="confirmSendEmail">
          发送
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import type {
  CompanyBasicRow,
  CompanyLogoRow,
  CompanySealRow,
  CompanyWarehouseRow
} from '@/api/companyProfile'
import apiClient from '@/api/client'
import { sendPurchaseOrderReportEmail } from '@/api/purchaseOrderReport'
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDate } from '@/utils/displayDateTime'
import {
  PURCHASE_ORDER_SERVICE_TERMS,
  PURCHASE_ORDER_REPORT_TAX_RATE
} from '@/constants/purchaseOrderReportTerms'
import {
  purchaseOrderReportAllowed,
  normalizePurchaseOrderMainStatus
} from '@/constants/purchaseOrderStatus'
import PurchaseOrderReportDocument from '@/components/purchaseOrder/PurchaseOrderReportDocument.vue'
import { renderElementToPdfBlob, blobToDataUrl } from '@/utils/poReportPdf'
import { getApiErrorMessage } from '@/utils/apiError'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

/** 与 print-purchase-order.scss 配合；勿改名，须与样式中 body 类名一致 */
const PO_REPORT_PRINT_BODY_CLASS = 'po-order-report-print'

/** 公司信息未配置或未上传 Logo 时的占位（与 public 下 SVG 一致） */
const DEFAULT_PO_REPORT_LOGO = '/purchase-order-template/logo.svg'

const loading = ref(true)
const errorMsg = ref('')
const order = ref<Record<string, any> | null>(null)
const basicDefault = ref<CompanyBasicRow | null>(null)
const warehouseDefault = ref<CompanyWarehouseRow | null>(null)
const sealUrl = ref<string | null>(null)
const companyLogoObjectUrl = ref<string | null>(null)
const reportRoot = ref<HTMLElement | null>(null)

const exporting = ref(false)
const emailVisible = ref(false)
const emailTo = ref('')
const emailSending = ref(false)
const defaultVendorEmail = ref('')

/** 买方签章区是否叠加印章图（打印/导出/邮件与预览一致） */
const showSealOnReport = ref(true)

const canViewAmount = computed(() => authStore.hasPermission('purchase.amount.read'))

const poId = computed(() => String(route.params.id || ''))

const ready = computed(() => !!order.value && !errorMsg.value && !loading.value)

function pickDefault<T extends { isDefault?: boolean; enabled?: boolean }>(rows: T[] | undefined | null): T | undefined {
  if (!rows?.length) return undefined
  const d = rows.find((r) => r.isDefault && r.enabled !== false)
  return d ?? rows[0]
}

/** 报表 Logo：与公司信息页「默认组可用于报表」一致，只要默认行已保存 documentId 即用，不因「启用」关闭而退回到无文件的第一组。 */
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

function currencyCode(v: number | undefined): string {
  if (v === 2) return 'USD'
  if (v === 3) return 'EUR'
  return 'RMB'
}

function formatMoney(n: number): string {
  return (n ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

/** 签章区日期：2026年3月27日 */
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
      taxRateLabel: String(PURCHASE_ORDER_REPORT_TAX_RATE),
      extraLines: [] as string[],
      terms: PURCHASE_ORDER_SERVICE_TERMS,
      sealUrl: null as string | null,
      logoUrl: companyLogoObjectUrl.value ?? DEFAULT_PO_REPORT_LOGO,
      showAmounts: canViewAmount.value,
      showSeal: showSealOnReport.value,
      buyerSignDate: ''
    }
  }

  const seller = basicDefault.value
  const wh = warehouseDefault.value
  const items = (o.items as any[]) || []
  const cur = currencyCode(o.currency)

  let qtySum = 0
  let inclSum = 0
  const lines = items.map((row, i) => {
    const qty = Number(row.qty) || 0
    const cost = Number(row.cost) || 0
    const lineTotal = qty * cost
    qtySum += qty
    inclSum += lineTotal
    return {
      index: i + 1,
      brand: row.brand || '—',
      unit: '个',
      currency: cur,
      qty: formatMoney(qty),
      unitPrice: formatMoney(cost),
      taxRate: String(PURCHASE_ORDER_REPORT_TAX_RATE),
      lineTotal: formatMoney(lineTotal),
      productName: (row.comment && String(row.comment).trim()) || '—',
      spec: row.pn || '—'
    }
  })

  const rate = PURCHASE_ORDER_REPORT_TAX_RATE
  const excl = inclSum / (1 + rate)
  const tax = inclSum - excl

  const shipTo = [
    seller?.companyName,
    wh?.warehouseName,
    wh?.address,
    [wh?.contactName, wh?.contactPhone].filter(Boolean).join(' ')
  ]
    .filter((x) => x && String(x).trim())
    .join(' ')

  const consignee = [wh?.contactName, wh?.contactPhone].filter(Boolean).join(' ') || '—'

  const vendorName = o.vendorName || '—'
  const vendorAddr = o.vendorOfficeAddress || '—'
  const vendorPhoneLine = [o.vendorContactName, o.vendorContactPhone].filter(Boolean).join(' ') || '—'

  return {
    headerCompanyName: seller?.companyName || '—',
    orderCode: o.purchaseOrderCode || '—',
    orderDate: formatDisplayDate(o.createTime) || '—',
    deliveryDate: formatDisplayDate(o.deliveryDate) || '—',
    deliveryMode: '送货/物流/快递',
    partySeller: {
      name: vendorName,
      address: vendorAddr,
      phone: vendorPhoneLine,
      consignee
    },
    partyBuyer: {
      name: seller?.companyName || '—',
      address: seller?.address || '—'
    },
    currencyLabel: cur,
    lines,
    totalQty: formatMoney(qtySum),
    totalIncl: formatMoney(inclSum),
    exclTax: formatMoney(excl),
    taxAmount: formatMoney(tax),
    grandIncl: formatMoney(inclSum),
    taxRateLabel: String(PURCHASE_ORDER_REPORT_TAX_RATE),
    extraLines: [
      '运费承担：供方承担',
      `收货地址：${shipTo || '—'}`,
      `付款方式：${(o.comment && String(o.comment).trim()) || '—'}`
    ],
    terms: PURCHASE_ORDER_SERVICE_TERMS,
    sealUrl: sealUrl.value,
    logoUrl: companyLogoObjectUrl.value ?? DEFAULT_PO_REPORT_LOGO,
    showAmounts: canViewAmount.value,
    showSeal: showSealOnReport.value,
    buyerSignDate: formatChineseDate(o.createTime)
  }
})

async function loadSealBlobUrl(seal: CompanySealRow | undefined) {
  if (seal?.documentId) {
    try {
      const blob = await apiClient.getBlob(`/api/v1/documents/${seal.documentId}/download`)
      if (blob.size > 0) {
        sealUrl.value = URL.createObjectURL(blob)
        return
      }
    } catch {
      // ignore
    }
  }
  sealUrl.value = null
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
  if (sealUrl.value) {
    URL.revokeObjectURL(sealUrl.value)
    sealUrl.value = null
  }
  if (companyLogoObjectUrl.value) {
    URL.revokeObjectURL(companyLogoObjectUrl.value)
    companyLogoObjectUrl.value = null
  }
  try {
    const id = poId.value
    if (!id) {
      errorMsg.value = '缺少订单 ID'
      return
    }
    const data = (await purchaseOrderApi.getReportData(id)) as {
      order: Record<string, unknown>
      companyProfile: {
        basicInfos?: CompanyBasicRow[]
        warehouses?: CompanyWarehouseRow[]
        seals?: CompanySealRow[]
        logos?: CompanyLogoRow[]
      }
    }
    order.value = data.order as any
    if (!purchaseOrderReportAllowed(normalizePurchaseOrderMainStatus(order.value))) {
      errorMsg.value = '仅供应商已确认后的采购订单可生成采购单报表'
      order.value = null
      return
    }
    const profile = data.companyProfile
    const logos =
      profile?.logos ??
      (profile as { Logos?: CompanyLogoRow[] } | undefined)?.Logos ??
      []
    basicDefault.value = pickDefault(profile.basicInfos) ?? null
    warehouseDefault.value = pickDefault(profile.warehouses) ?? null
    const seal = pickDefault(profile.seals)
    await loadSealBlobUrl(seal)
    const logo = pickReportLogoRow(logos)
    await loadCompanyLogoBlobUrl(logo)

    defaultVendorEmail.value = String((data.order as any)?.vendorContactEmail || '').trim()
    emailTo.value = defaultVendorEmail.value
  } catch (e) {
    errorMsg.value = getApiErrorMessage(e, '加载失败')
    order.value = null
  } finally {
    loading.value = false
  }
}

function doPrint() {
  window.print()
}

/** 仅截取白色报表 DOM（.print-root 含灰底与 padding，不能作为 PDF 源） */
function getPdfDocumentElement(): HTMLElement | null {
  const wrap = reportRoot.value
  if (!wrap) return null
  return wrap.querySelector('.po-doc') as HTMLElement | null
}

async function doExportPdf() {
  const el = getPdfDocumentElement()
  if (!el) {
    ElMessage.error('未找到报表内容，请稍后重试')
    return
  }
  exporting.value = true
  try {
    const blob = await renderElementToPdfBlob(el)
    const code = order.value?.purchaseOrderCode || '采购订单'
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${code}.pdf`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success('已导出 PDF')
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '导出失败'))
  } finally {
    exporting.value = false
  }
}

function openEmailDialog() {
  emailTo.value = defaultVendorEmail.value || emailTo.value
  emailVisible.value = true
}

function onEmailClosed() {
  emailSending.value = false
}

async function confirmSendEmail() {
  const el = getPdfDocumentElement()
  const id = poId.value
  if (!id) return
  if (!el) {
    ElMessage.error('未找到报表内容，请稍后重试')
    return
  }
  const to = emailTo.value.trim()
  if (!to) {
    ElMessage.warning('请填写收件人邮箱')
    return
  }
  emailSending.value = true
  try {
    const blob = await renderElementToPdfBlob(el)
    const dataUrl = await blobToDataUrl(blob)
    const code = order.value?.purchaseOrderCode || '采购订单'
    await sendPurchaseOrderReportEmail(id, {
      to,
      pdfBase64: dataUrl,
      fileName: `${code}.pdf`,
      subject: `采购订单 ${code}`,
      body: `请查收附件：采购订单 ${code}。`
    })
    ElMessage.success('邮件已发送')
    emailVisible.value = false
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '发送失败'))
  } finally {
    emailSending.value = false
  }
}

onMounted(() => {
  document.body.classList.add(PO_REPORT_PRINT_BODY_CLASS)
  load()
})
watch(poId, () => load())

onBeforeUnmount(() => {
  document.body.classList.remove(PO_REPORT_PRINT_BODY_CLASS)
})

onUnmounted(() => {
  if (sealUrl.value) URL.revokeObjectURL(sealUrl.value)
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

.email-tip {
  margin: 0 0 12px;
  font-size: 13px;
  color: #606266;
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
