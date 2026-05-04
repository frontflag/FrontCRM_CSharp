import apiClient from './client'

export type DebugItem = {
  name: string
  value: string
}

/** 与后端 DebugPageDto 一致（版本号由 vite 注入 package.json，不在此接口） */
export type DebugPage = {
  databaseConnectionDisplay: string
  items: DebugItem[]
}

/** 与后端 DataOrigin 一致：ignore | customer | vendor | salesorder | purchaseorder */
export type SimulateDataOrigin = 'ignore' | 'customer' | 'vendor' | 'salesorder' | 'purchaseorder'

export type SimulateBusinessChainRequest = {
  businessNode: string
  status: number
  dataOrigin?: SimulateDataOrigin
  /** 客户编号 / 供应商编码 / 销售单号 / 采购单号 */
  originReferenceCode?: string
}

export type SimulateBusinessChainResponse = {
  chainNo: string
  businessNode: string
  targetStatus: number
  createdNodes: string[]
}

function normalizeDebugPage(raw: unknown): DebugPage {
  const r = raw as Record<string, unknown> | null | undefined
  const inner = (r?.data ?? r?.Data ?? r) as Record<string, unknown> | null | undefined
  if (!inner || typeof inner !== 'object') {
    return { databaseConnectionDisplay: '', items: [] }
  }

  const itemsRaw = inner.items ?? inner.Items
  const items: DebugItem[] = Array.isArray(itemsRaw)
    ? itemsRaw.map((row: Record<string, unknown>) => ({
        name: String(row.name ?? row.Name ?? ''),
        value: String(row.value ?? row.Value ?? '')
      }))
    : []

  const databaseConnectionDisplay = String(
    inner.databaseConnectionDisplay ?? inner.DatabaseConnectionDisplay ?? ''
  )

  return { databaseConnectionDisplay, items }
}

export async function getDebugPage(): Promise<DebugPage> {
  const raw = await apiClient.get<unknown>('/api/v1/debug')
  return normalizeDebugPage(raw)
}

export async function simulateBusinessChain(payload: SimulateBusinessChainRequest): Promise<SimulateBusinessChainResponse> {
  const body = JSON.parse(JSON.stringify(payload)) as SimulateBusinessChainRequest
  const raw = await apiClient.post<any>('/api/v1/debug/simulate-business-chain', body)
  const outer = (raw?.data ?? raw?.Data ?? raw) as Record<string, any>
  const inner = (outer?.data ?? outer?.Data ?? outer) as Record<string, any>
  return {
    chainNo: String(inner?.chainNo ?? inner?.ChainNo ?? ''),
    businessNode: String(inner?.businessNode ?? inner?.BusinessNode ?? ''),
    targetStatus: Number(inner?.targetStatus ?? inner?.TargetStatus ?? 0),
    createdNodes: Array.isArray(inner?.createdNodes ?? inner?.CreatedNodes)
      ? (inner.createdNodes ?? inner.CreatedNodes).map((x: unknown) => String(x))
      : []
  }
}

export type RfqChainNode = {
  node: string
  code: string
  id: string
}

export type RfqChainPreview = {
  rfqCode: string | null
  nodes: RfqChainNode[]
}

export type RefreshStockLedgerResult = {
  stockOutUpdated: number
  stockOutReverseUpdated: number
  currencyDefaulted: number
}

export type RefreshSellOrderCommentSplitResult = {
  totalWithComment: number
  rowsProcessed: number
}

export type RefreshSellOrderItemCustomerPnFromCommentResult = {
  totalWithComment: number
  rowsFilled: number
}

export type RefreshPurchaseOrderMainStatusResult = {
  totalOrders: number
  changedOrders: number
  changedOrderCodes: string[]
  skippedTerminalOrders: number
}

function normalizeRfqChainPreview(raw: unknown): RfqChainPreview {
  const r = raw as Record<string, unknown> | null | undefined
  const inner = (r?.data ?? r?.Data ?? r) as Record<string, unknown> | null | undefined
  if (!inner || typeof inner !== 'object') {
    return { rfqCode: null, nodes: [] }
  }
  const nodesRaw = inner.nodes ?? inner.Nodes
  const nodes: RfqChainNode[] = Array.isArray(nodesRaw)
    ? nodesRaw.map((row: Record<string, unknown>) => ({
        node: String(row.node ?? row.Node ?? ''),
        code: String(row.code ?? row.Code ?? ''),
        id: String(row.id ?? row.Id ?? '')
      }))
    : []
  const rfqCode = inner.rfqCode ?? inner.RfqCode
  return { rfqCode: rfqCode != null ? String(rfqCode) : null, nodes }
}

/** GET /api/v1/debug/rfq-chain?rfqCode= */
export async function getRfqChainPreview(rfqCode: string): Promise<RfqChainPreview> {
  const enc = encodeURIComponent(rfqCode.trim())
  const raw = await apiClient.get<unknown>(`/api/v1/debug/rfq-chain?rfqCode=${enc}`)
  return normalizeRfqChainPreview(raw)
}

/** DELETE /api/v1/debug/rfq-chain?rfqCode= */
export async function deleteRfqChain(rfqCode: string): Promise<void> {
  const enc = encodeURIComponent(rfqCode.trim())
  await apiClient.delete(`/api/v1/debug/rfq-chain?rfqCode=${enc}`)
}

export async function refreshStockLedger(): Promise<RefreshStockLedgerResult> {
  const raw = await apiClient.post<any>('/api/v1/debug/refresh-stockledger', {})
  const outer = (raw?.data ?? raw?.Data ?? raw) as Record<string, any>
  const inner = (outer?.data ?? outer?.Data ?? outer) as Record<string, any>
  return {
    stockOutUpdated: Number(inner?.stockOutUpdated ?? inner?.StockOutUpdated ?? 0),
    stockOutReverseUpdated: Number(inner?.stockOutReverseUpdated ?? inner?.StockOutReverseUpdated ?? 0),
    currencyDefaulted: Number(inner?.currencyDefaulted ?? inner?.CurrencyDefaulted ?? 0)
  }
}

/** POST /api/v1/debug/refresh-sellorder-comment-split — 将 sellorder.comment 拆入结构化列并清空 comment */
export async function refreshSellOrderCommentSplit(): Promise<RefreshSellOrderCommentSplitResult> {
  const raw = await apiClient.post<any>('/api/v1/debug/refresh-sellorder-comment-split', {})
  const outer = (raw?.data ?? raw?.Data ?? raw) as Record<string, any>
  const inner = (outer?.data ?? outer?.Data ?? outer) as Record<string, any>
  return {
    totalWithComment: Number(inner?.totalWithComment ?? inner?.TotalWithComment ?? 0),
    rowsProcessed: Number(inner?.rowsProcessed ?? inner?.RowsProcessed ?? 0)
  }
}

/** POST /api/v1/debug/refresh-sellorderitem-customer-pn-from-comment — 从行 comment 解析客户物料型号写入 customer_pn（仅填空） */
export async function refreshSellOrderItemCustomerPnFromComment(): Promise<RefreshSellOrderItemCustomerPnFromCommentResult> {
  const raw = await apiClient.post<any>('/api/v1/debug/refresh-sellorderitem-customer-pn-from-comment', {})
  const outer = (raw?.data ?? raw?.Data ?? raw) as Record<string, any>
  const inner = (outer?.data ?? outer?.Data ?? outer) as Record<string, any>
  return {
    totalWithComment: Number(inner?.totalWithComment ?? inner?.TotalWithComment ?? 0),
    rowsFilled: Number(inner?.rowsFilled ?? inner?.RowsFilled ?? 0)
  }
}

export async function refreshPurchaseOrderMainStatus(): Promise<RefreshPurchaseOrderMainStatusResult> {
  const raw = await apiClient.post<any>('/api/v1/debug/refresh-purchase-order-main-status', {})
  const outer = (raw?.data ?? raw?.Data ?? raw) as Record<string, any>
  const inner = (outer?.data ?? outer?.Data ?? outer) as Record<string, any>
  const codesRaw = inner?.changedOrderCodes ?? inner?.ChangedOrderCodes
  return {
    totalOrders: Number(inner?.totalOrders ?? inner?.TotalOrders ?? 0),
    changedOrders: Number(inner?.changedOrders ?? inner?.ChangedOrders ?? 0),
    changedOrderCodes: Array.isArray(codesRaw) ? codesRaw.map((x: unknown) => String(x)) : [],
    skippedTerminalOrders: Number(inner?.skippedTerminalOrders ?? inner?.SkippedTerminalOrders ?? 0)
  }
}
