/** 流程页签站点章。口径：document/PRD/规范/UI规范/流程页签规范.md §0.4 */

export type FlowStationBadge =
  | 'empty'
  | 'active'
  | 'done'
  | 'cancelled'
  | 'failed'
  | 'reviewFailed'
  | 'invoiceFailed'
  | 'redFlushed'

export type FlowDocOutcome = Exclude<FlowStationBadge, 'empty'>

const failureFirst: FlowDocOutcome[] = ['reviewFailed', 'invoiceFailed', 'failed', 'redFlushed']

export function foldFlowStationBadge(outcomes: FlowDocOutcome[]): FlowStationBadge {
  if (outcomes.length === 0) return 'empty'
  if (outcomes.some((o) => o === 'active')) return 'active'
  for (const kind of failureFirst) {
    if (outcomes.some((o) => o === kind)) return kind
  }
  if (outcomes.every((o) => o === 'cancelled')) return 'cancelled'
  return 'done'
}

export function foldFlowCards(
  cards: Array<{ outcome?: FlowDocOutcome; isDeleted?: boolean }>
): FlowStationBadge {
  if (cards.length === 0) return 'empty'
  const live = cards.filter((c) => !c.isDeleted)
  if (live.length === 0) return 'cancelled'
  return foldFlowStationBadge(live.map((c) => c.outcome ?? 'active'))
}

export function stockOutOutcome(status: unknown): FlowDocOutcome {
  const s = Number(status)
  if (s === 4) return 'done'
  if (s === 3) return 'cancelled'
  return 'active'
}

export function stockOutNotifyOutcome(status: unknown): FlowDocOutcome {
  const s = Number(status)
  if (s === 100) return 'done'
  if (s === -1) return 'cancelled'
  return 'active'
}

export function packingOutcome(status: unknown): FlowDocOutcome {
  const s = Number(status)
  if (s >= 100) return 'done'
  if (s === -1) return 'cancelled'
  return 'active'
}

export function stockInOutcome(status: unknown): FlowDocOutcome {
  const s = Number(status)
  if (s === 2) return 'done'
  if (s === 3) return 'cancelled'
  return 'active'
}

export function arrivalOutcome(status: unknown): FlowDocOutcome {
  return Number(status) >= 100 ? 'done' : 'active'
}

export function pickingOutcome(status: unknown): FlowDocOutcome {
  const s = Number(status)
  if (s === 100) return 'done'
  if (s === -1) return 'cancelled'
  return 'active'
}

export function prOutcome(status: unknown): FlowDocOutcome {
  const s = Number(status)
  if (s === 2) return 'done'
  if (s === 3) return 'cancelled'
  return 'active'
}

export function poItemOutcome(status: unknown): FlowDocOutcome {
  const s = Number(status)
  if (s === 100) return 'done'
  if (s === -2) return 'cancelled'
  if (s === -1) return 'reviewFailed'
  return 'active'
}

export function paymentOutcome(status: unknown): FlowDocOutcome {
  const s = Number(status)
  if (s === 100) return 'done'
  if (s === -2) return 'cancelled'
  if (s === -1) return 'reviewFailed'
  return 'active'
}

export function salesOrderOutcome(status: unknown): FlowDocOutcome {
  const s = Number(status)
  if (s === 100) return 'done'
  if (s === -2) return 'cancelled'
  if (s === -1) return 'reviewFailed'
  return 'active'
}

export function sellLineOutcome(itemStatus: unknown, receipt: unknown, invoice: unknown): FlowDocOutcome {
  if (Number(itemStatus) === 1) return 'cancelled'
  if (Number(receipt) === 2 && Number(invoice) === 2) return 'done'
  return 'active'
}

export function salesInvoiceOutcome(status: unknown): FlowDocOutcome {
  const s = Number(status)
  if (s === 100) return 'done'
  if (s === -1) return 'cancelled'
  if (s === 101) return 'invoiceFailed'
  return 'active'
}

export function purchaseInvoiceOutcome(confirmStatus: unknown, redInvoiceStatus: unknown): FlowDocOutcome {
  const red = Number(redInvoiceStatus)
  if (Number.isFinite(red) && red > 0) return 'redFlushed'
  if (Number(confirmStatus) === 1) return 'done'
  return 'active'
}

export function qcOutcome(status: unknown): FlowDocOutcome {
  const s = Number(status)
  if (s === 100) return 'done'
  if (s === -1) return 'failed'
  return 'active'
}

export function declarationOutcome(status: unknown): FlowDocOutcome {
  const s = Number(status)
  if (s === 3) return 'done'
  if (s === -1) return 'cancelled'
  return 'active'
}

export function pendlistOutcome(status: unknown): FlowDocOutcome {
  const s = Number(status)
  if (s === 10) return 'done'
  if (s === -1) return 'cancelled'
  return 'active'
}

type TFunc = (key: string, ...args: unknown[]) => string

export function flowStationBadgeLabel(status: FlowStationBadge, t: TFunc): string {
  if (status === 'active') return t('salesOrderItemList.flowPanel.stationActive')
  if (status === 'done') return t('salesOrderItemList.flowPanel.stationDone')
  if (status === 'cancelled') return t('salesOrderItemList.flowPanel.stationCancelled')
  if (status === 'failed') return t('salesOrderItemList.flowPanel.stationFailed')
  if (status === 'reviewFailed') return t('salesOrderItemList.flowPanel.stationReviewFailed')
  if (status === 'invoiceFailed') return t('salesOrderItemList.flowPanel.stationInvoiceFailed')
  if (status === 'redFlushed') return t('salesOrderItemList.flowPanel.stationRedFlushed')
  return t('salesOrderItemList.flowPanel.stationEmpty')
}
