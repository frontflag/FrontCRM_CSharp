import {
  salesOrderLineApplyStockOutButtonDisabled,
  salesOrderLineApplyStockOutDisabledReasonKey,
  salesOrderLinePurchasedStockReliefOk
} from '@/constants/salesOrderStatus'
import { purchaseOrderMainStatusLabel } from '@/constants/purchaseOrderStatus'

export interface StockOutApplyPurchaseGateBlockingPo {
  purchaseOrderId?: string
  orderCode?: string | null
  status?: number
  missing?: boolean
}

export interface StockOutApplyPurchaseGateDetail {
  ok?: boolean
  hasPoItems?: boolean
  blockingPurchaseOrders?: StockOutApplyPurchaseGateBlockingPo[]
}

export interface ApplyStockOutDisabledHintContent {
  summary: string
  details: string[]
  nextStep: string
}

type TranslateFn = (key: string, params?: Record<string, unknown>) => string

type ExtendProgressKind = 'purchase' | 'stockIn' | 'stockOut' | 'stockOutNotify' | 'receipt' | 'invoice'

function extendTriLabel(t: TranslateFn, kind: ExtendProgressKind, v?: unknown): string {
  const n = Number(v)
  const slot = n === 2 ? 'complete' : n === 1 ? 'partial' : 'pending'
  return t(`salesOrderItemList.extendProgress.${kind}.${slot}`)
}

function normalizeGateDetail(row: Record<string, unknown>): StockOutApplyPurchaseGateDetail | null {
  const raw = row.stockOutApplyPurchaseGateDetail ?? row.StockOutApplyPurchaseGateDetail
  if (!raw || typeof raw !== 'object') return null
  return raw as StockOutApplyPurchaseGateDetail
}

function gateOk(row: Record<string, unknown>): boolean {
  return row.stockOutApplyPurchaseGateOk === true || row.StockOutApplyPurchaseGateOk === true
}

function stockingQty(row: Record<string, unknown>): number {
  const n = Number(row.purchasedStockAvailableQty ?? row.PurchasedStockAvailableQty)
  return Number.isFinite(n) ? Math.max(0, Math.trunc(n)) : 0
}

function collectGateBlockerDetails(row: Record<string, unknown>, t: TranslateFn): string[] {
  const lines: string[] = []
  const detail = normalizeGateDetail(row)
  const requiredStatus = t('purchaseOrderList.status.confirmed')

  if (detail && detail.hasPoItems === false) {
    lines.push(t('salesOrderItemList.messages.applyStockOutHintDetailNoPoItems'))
    return lines
  }

  const blockers = detail?.blockingPurchaseOrders ?? []
  if (blockers.length > 0) {
    for (const po of blockers) {
      const code = String(po.orderCode ?? '').trim() || String(po.purchaseOrderId ?? '').trim() || '—'
      if (po.missing) {
        lines.push(t('salesOrderItemList.messages.applyStockOutHintDetailPoMissing', { code }))
        continue
      }
      const statusLabel = purchaseOrderMainStatusLabel(t, po.status)
      lines.push(
        t('salesOrderItemList.messages.applyStockOutHintDetailPoNotConfirmed', {
          code,
          status: statusLabel,
          requiredStatus
        })
      )
    }
    return lines
  }

  if (!gateOk(row)) {
    lines.push(t('salesOrderItemList.messages.applyStockOutHintDetailPurchaseGateUnknown'))
  }
  return lines
}

/** 构建申请出库禁用提示（规则摘要 + 当前不满足的具体条件）。 */
export function buildApplyStockOutDisabledHintContent(
  row: Record<string, unknown>,
  t: TranslateFn
): ApplyStockOutDisabledHintContent | null {
  if (!salesOrderLineApplyStockOutButtonDisabled(row)) return null

  const key = salesOrderLineApplyStockOutDisabledReasonKey(row)
  if (!key) return null

  const summaryMap = {
    stockOutDone: 'salesOrderItemList.messages.applyStockOutDisabledStockOutDone',
    notifyDone: 'salesOrderItemList.messages.applyStockOutDisabledNotifyDone',
    needPurchaseGate: 'salesOrderItemList.messages.applyStockOutNeedPurchaseGate',
    pendingPurchase: 'salesOrderItemList.messages.applyStockOutDisabledPendingPurchase'
  } as const

  const summary = t(summaryMap[key])
  const details: string[] = []
  const reliefOk = salesOrderLinePurchasedStockReliefOk(row)
  const purchaseProgress = Number(row.purchaseProgressStatus ?? row.PurchaseProgressStatus)
  const stockOutProgress = Number(row.stockOutProgressStatus ?? row.StockOutProgressStatus)

  const nextStepMap = {
    stockOutDone: 'salesOrderItemList.opsPanel.stockOutNextDone',
    notifyDone: 'salesOrderItemList.opsPanel.stockOutNextNotifyDone',
    needPurchaseGate: 'salesOrderItemList.opsPanel.stockOutNextPurchaseGate',
    pendingPurchase: 'salesOrderItemList.opsPanel.stockOutNextPendingPurchase'
  } as const

  const notifyProgress = Number(row.stockOutNotifyProgressStatus ?? row.StockOutNotifyProgressStatus)
  if (notifyProgress === 2) {
    details.push(
      t('salesOrderItemList.messages.applyStockOutHintDetailNotifyDone', {
        status: extendTriLabel(t, 'stockOutNotify', notifyProgress)
      })
    )
  }

  if (stockOutProgress === 2) {
    details.push(
      t('salesOrderItemList.messages.applyStockOutHintDetailStockOutDone', {
        status: extendTriLabel(t, 'stockOut', stockOutProgress)
      })
    )
  }

  if (!reliefOk) {
    if (!gateOk(row)) {
      details.push(...collectGateBlockerDetails(row, t))
    }

    if (purchaseProgress === 0) {
      details.push(
        t('salesOrderItemList.messages.applyStockOutHintDetailPendingPurchase', {
          status: extendTriLabel(t, 'purchase', purchaseProgress)
        })
      )
    }

    const qty = stockingQty(row)
    if (qty <= 0 && (!gateOk(row) || purchaseProgress === 0)) {
      details.push(
        t('salesOrderItemList.messages.applyStockOutHintDetailNoStockingRelief', { qty })
      )
    }
  }

  return { summary, details: [...new Set(details)], nextStep: t(nextStepMap[key]) }
}

/** 供旧调用方兼容：拼接为单段文本。 */
export function buildApplyStockOutDisabledHintText(row: Record<string, unknown>, t: TranslateFn): string {
  const content = buildApplyStockOutDisabledHintContent(row, t)
  if (!content) return ''
  if (content.details.length === 0) return content.summary
  return [
    content.summary,
    t('salesOrderItemList.messages.applyStockOutHintDetailTitle'),
    ...content.details.map((line) => `• ${line}`)
  ].join('\n')
}
