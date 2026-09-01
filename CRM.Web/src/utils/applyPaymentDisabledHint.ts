import {
  purchaseOrderAllowsApplyPayment,
  purchaseOrderApplyPaymentIsCancelled,
  purchaseOrderFinancePaymentIsComplete
} from '@/constants/purchaseOrderStatus'
import { getPaymentMetrics } from '@/utils/purchaseOrderItemOpsPanel'

export interface ApplyPaymentDisabledHintDoc {
  id: string
  code: string
}

export interface ApplyPaymentDisabledHintContent {
  summary: string
  details: string[]
  nextStep: string
  /** 货款已付清（付款完成）时列出关联付款单；undefined 表示聚合尚未返回 */
  paymentDocs?: ApplyPaymentDisabledHintDoc[]
}

export type ApplyPaymentDisabledHintOptions = {
  /** 操作面板申请付款按钮权限；未传时仅用行上 canApplyPayment 反推 */
  canInitiatePayment?: boolean
  /** 明细 aggregates；货款已付清时用来列出付款单号 */
  aggregates?: {
    payments?: Array<{ id?: string; financePaymentCode?: string; status?: number; isDeleted?: boolean }>
  } | null
}

type TranslateFn = (key: string, params?: Record<string, unknown>) => string

const FINANCE_PAYMENT_STATUS_CANCELLED = -2

function canApplyPayment(row: Record<string, unknown>): boolean {
  return row.canApplyPayment === true || row.CanApplyPayment === true
}

function hasPaymentPermission(
  row: Record<string, unknown>,
  options?: ApplyPaymentDisabledHintOptions
): boolean | undefined {
  if (typeof options?.canInitiatePayment === 'boolean') return options.canInitiatePayment
  if (canApplyPayment(row)) return true
  return undefined
}

/** 未取消、未删除的关联付款单。aggregates 未到时返回 undefined。 */
export function listLinkedFinancePaymentDocs(
  aggregates?: ApplyPaymentDisabledHintOptions['aggregates']
): ApplyPaymentDisabledHintDoc[] | undefined {
  if (aggregates == null) return undefined
  const seen = new Set<string>()
  const docs: ApplyPaymentDisabledHintDoc[] = []
  for (const row of aggregates.payments ?? []) {
    if (row.isDeleted === true) continue
    if (Number(row.status) === FINANCE_PAYMENT_STATUS_CANCELLED) continue
    const id = String(row.id ?? '').trim()
    const code = String(row.financePaymentCode ?? '').trim()
    if (!id || !code || seen.has(id)) continue
    seen.add(id)
    docs.push({ id, code })
  }
  docs.sort((a, b) => a.code.localeCompare(b.code, 'en'))
  return docs
}

function financeFullyPaidBlocksApply(
  row: Record<string, unknown>,
  aggregates?: ApplyPaymentDisabledHintOptions['aggregates']
): boolean {
  if (!purchaseOrderFinancePaymentIsComplete(row)) return false
  const docs = listLinkedFinancePaymentDocs(aggregates)
  if (docs === undefined) return true
  return docs.length > 0
}

export function applyPaymentButtonDisabled(
  row: Record<string, unknown>,
  options?: ApplyPaymentDisabledHintOptions
): boolean {
  if (purchaseOrderApplyPaymentIsCancelled(row)) return true
  if (!purchaseOrderAllowsApplyPayment(row)) return true
  if (financeFullyPaidBlocksApply(row, options?.aggregates)) return true
  if (!canApplyPayment(row)) return true
  return getPaymentMetrics(row).availableAmount <= 0
}

/** 构建申请付款禁用提示：只写本行当前成立的一条直接原因，禁止「可能…」兜底。 */
export function buildApplyPaymentDisabledHintContent(
  row: Record<string, unknown>,
  t: TranslateFn,
  options?: ApplyPaymentDisabledHintOptions
): ApplyPaymentDisabledHintContent | null {
  if (!applyPaymentButtonDisabled(row, options)) return null

  const permitted = hasPaymentPermission(row, options)
  if (permitted === false) {
    return {
      summary: t('purchaseOrderItemList.opsPanel.paymentNoPermission'),
      details: [],
      nextStep: t('purchaseOrderItemList.opsPanel.paymentNextNoPermission')
    }
  }

  if (purchaseOrderApplyPaymentIsCancelled(row)) {
    return {
      summary: t('purchaseOrderItemList.opsPanel.paymentCancelled'),
      details: [],
      nextStep: t('purchaseOrderItemList.opsPanel.paymentNextCancelled')
    }
  }

  if (!purchaseOrderAllowsApplyPayment(row)) {
    return {
      summary: t('purchaseOrderItemList.opsPanel.paymentNeedConfirmed'),
      details: [],
      nextStep: t('purchaseOrderItemList.opsPanel.paymentNextConfirmed')
    }
  }

  if (financeFullyPaidBlocksApply(row, options?.aggregates)) {
    const paymentDocs = listLinkedFinancePaymentDocs(options?.aggregates)
    if (paymentDocs === undefined) {
      return {
        summary: t('purchaseOrderItemList.opsPanel.paymentFinanceDone'),
        details: [],
        nextStep: t('purchaseOrderItemList.opsPanel.paymentNextFinanceDone')
      }
    }
    if (paymentDocs.length === 0) {
      return {
        summary: t('purchaseOrderItemList.opsPanel.paymentFinanceDone'),
        details: [t('purchaseOrderItemList.opsPanel.paymentNoLinkedDoc')],
        paymentDocs: [],
        nextStep: t('purchaseOrderItemList.opsPanel.paymentNextFinanceDoneNoDoc')
      }
    }
    return {
      summary: t('purchaseOrderItemList.opsPanel.paymentFinanceDone'),
      details: [],
      paymentDocs,
      nextStep: t('purchaseOrderItemList.opsPanel.paymentNextFinanceDoneWithDocs')
    }
  }

  if (getPaymentMetrics(row).availableAmount <= 0) {
    return {
      summary: t('purchaseOrderItemList.opsPanel.paymentNoRemaining'),
      details: [],
      nextStep: t('purchaseOrderItemList.opsPanel.paymentNextNoRemaining')
    }
  }

  if (!canApplyPayment(row)) {
    return {
      summary: t('purchaseOrderItemList.opsPanel.paymentNoPermission'),
      details: [],
      nextStep: t('purchaseOrderItemList.opsPanel.paymentNextNoPermission')
    }
  }

  return null
}
