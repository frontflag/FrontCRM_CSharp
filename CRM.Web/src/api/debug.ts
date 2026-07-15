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

/** GET /api/v1/debug/simulation-banner — 仿真顶栏（FZFlag / FZColor / FZCaption） */
export type SimulationBanner = {
  enabled: boolean
  backgroundColor: string
  caption: string
}

const emptySimulationBanner = (): SimulationBanner => ({
  enabled: false,
  backgroundColor: '',
  caption: ''
})

function normalizeSimulationBanner(raw: unknown): SimulationBanner {
  const r = raw as Record<string, unknown> | null | undefined
  const inner = (r?.data ?? r?.Data ?? r) as Record<string, unknown> | null | undefined
  if (!inner || typeof inner !== 'object') return emptySimulationBanner()
  const enabled = inner.enabled ?? inner.Enabled
  return {
    enabled: enabled === true || enabled === 'true',
    backgroundColor: String(inner.backgroundColor ?? inner.BackgroundColor ?? '').trim(),
    caption: String(inner.caption ?? inner.Caption ?? '').trim()
  }
}

export async function getSimulationBanner(): Promise<SimulationBanner> {
  const raw = await apiClient.get<unknown>('/api/v1/debug/simulation-banner')
  return normalizeSimulationBanner(raw)
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

export type RecalculateStockAggregatesResult = {
  totalBuckets: number
  mismatchedBefore: number
  bucketsUpdated: number
  totalAvailOverstatement: number
  updatedStockCodes: string[]
}

export type RefreshSellOrderCommentSplitResult = {
  totalWithComment: number
  rowsProcessed: number
}

export type RefreshSellOrderItemCustomerPnFromCommentResult = {
  totalWithComment: number
  rowsFilled: number
}

export type RefreshFinancePaymentLegacyRemarkResult = {
  totalPaymentsRemarkNonEmpty: number
  legacyPackedCandidates: number
  parsedAndApplied: number
  skippedMalformed: number
  itemsLineRemarkUpdated: number
  bankIdsResolvedFromName: number
}

export type RefreshFinanceReceivablesFromStockOutsResult = {
  totalCompletedSalesStockOuts: number
  alreadyHasReceivableCount: number
  candidateCount: number
  createdCount: number
  skippedIneligibleCount: number
  failedCount: number
  stockOutDatesSyncedCount: number
  prematureReceivablesRemovedCount: number
  createdStockOutCodes: string[]
  skippedIneligibleStockOutCodes: string[]
  stockOutDatesSyncedStockOutCodes: string[]
  prematureReceivablesRemovedStockOutCodes: string[]
  failedStockOutCodes: string[]
  failedMessages: string[]
}

export type RefreshPurchaseOrderMainStatusResult = {
  totalOrders: number
  changedOrders: number
  changedOrderCodes: string[]
  skippedTerminalOrders: number
  totalItems: number
  changedItems: number
  failedCount: number
  failedMessages: string[]
}

export type RefreshSellOrderMainStatusResult = {
  totalOrders: number
  changedOrders: number
  changedOrderCodes: string[]
  skippedTerminalOrders: number
  failedCount: number
  failedMessages: string[]
}

/** POST /api/v1/debug/refresh-sellorderitemextend-outbound-profit — 批量重算销售明细出库利润 */
export type RefreshSellOrderItemExtendOutboundProfitResult = {
  totalLines: number
  linesWithOutboundQty: number
  profitChangedCount: number
  changedLineCodes: string[]
  failedCount: number
  failedMessages: string[]
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

/** POST /api/v1/debug/recalculate-stock-aggregates — 按 stock_item 全库重算 stock 汇总桶 */
export async function recalculateStockAggregates(): Promise<RecalculateStockAggregatesResult> {
  const raw = await apiClient.post<any>('/api/v1/debug/recalculate-stock-aggregates', {})
  const outer = (raw?.data ?? raw?.Data ?? raw) as Record<string, any>
  const inner = (outer?.data ?? outer?.Data ?? outer) as Record<string, any>
  const codesRaw = inner?.updatedStockCodes ?? inner?.UpdatedStockCodes
  return {
    totalBuckets: Number(inner?.totalBuckets ?? inner?.TotalBuckets ?? 0),
    mismatchedBefore: Number(inner?.mismatchedBefore ?? inner?.MismatchedBefore ?? 0),
    bucketsUpdated: Number(inner?.bucketsUpdated ?? inner?.BucketsUpdated ?? 0),
    totalAvailOverstatement: Number(inner?.totalAvailOverstatement ?? inner?.TotalAvailOverstatement ?? 0),
    updatedStockCodes: Array.isArray(codesRaw) ? codesRaw.map((x: unknown) => String(x)) : []
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
  const raw = await apiClient.post<any>(
    '/api/v1/debug/refresh-purchase-order-main-status',
    {},
    { timeout: 3_600_000 }
  )
  const outer = (raw?.data ?? raw?.Data ?? raw) as Record<string, any>
  const inner = (outer?.data ?? outer?.Data ?? outer) as Record<string, any>
  const codesRaw = inner?.changedOrderCodes ?? inner?.ChangedOrderCodes
  return {
    totalOrders: Number(inner?.totalOrders ?? inner?.TotalOrders ?? 0),
    changedOrders: Number(inner?.changedOrders ?? inner?.ChangedOrders ?? 0),
    changedOrderCodes: Array.isArray(codesRaw) ? codesRaw.map((x: unknown) => String(x)) : [],
    skippedTerminalOrders: Number(inner?.skippedTerminalOrders ?? inner?.SkippedTerminalOrders ?? 0),
    totalItems: Number(inner?.totalItems ?? inner?.TotalItems ?? 0),
    changedItems: Number(inner?.changedItems ?? inner?.ChangedItems ?? 0),
    failedCount: Number(inner?.failedCount ?? inner?.FailedCount ?? 0),
    failedMessages: normalizeStringList(inner?.failedMessages ?? inner?.FailedMessages)
  }
}

/** POST /api/v1/debug/refresh-sellorder-main-status — 批量刷新销售订单明细扩展并重算主状态 */
export async function refreshSellOrderMainStatus(): Promise<RefreshSellOrderMainStatusResult> {
  const raw = await apiClient.post<any>(
    '/api/v1/debug/refresh-sellorder-main-status',
    {},
    { timeout: 3_600_000 }
  )
  const outer = (raw?.data ?? raw?.Data ?? raw) as Record<string, any>
  const inner = (outer?.data ?? outer?.Data ?? outer) as Record<string, any>
  const codesRaw = inner?.changedOrderCodes ?? inner?.ChangedOrderCodes
  return {
    totalOrders: Number(inner?.totalOrders ?? inner?.TotalOrders ?? 0),
    changedOrders: Number(inner?.changedOrders ?? inner?.ChangedOrders ?? 0),
    changedOrderCodes: Array.isArray(codesRaw) ? codesRaw.map((x: unknown) => String(x)) : [],
    skippedTerminalOrders: Number(inner?.skippedTerminalOrders ?? inner?.SkippedTerminalOrders ?? 0),
    failedCount: Number(inner?.failedCount ?? inner?.FailedCount ?? 0),
    failedMessages: normalizeStringList(inner?.failedMessages ?? inner?.FailedMessages)
  }
}

/** POST /api/v1/debug/refresh-sellorderitemextend-outbound-profit — 批量重算 sellorderitemextend 出库利润 */
export async function refreshSellOrderItemExtendOutboundProfit(): Promise<RefreshSellOrderItemExtendOutboundProfitResult> {
  const raw = await apiClient.post<any>(
    '/api/v1/debug/refresh-sellorderitemextend-outbound-profit',
    {},
    { timeout: 3_600_000 }
  )
  const outer = (raw?.data ?? raw?.Data ?? raw) as Record<string, any>
  const inner = (outer?.data ?? outer?.Data ?? outer) as Record<string, any>
  const codesRaw = inner?.changedLineCodes ?? inner?.ChangedLineCodes
  return {
    totalLines: Number(inner?.totalLines ?? inner?.TotalLines ?? 0),
    linesWithOutboundQty: Number(inner?.linesWithOutboundQty ?? inner?.LinesWithOutboundQty ?? 0),
    profitChangedCount: Number(inner?.profitChangedCount ?? inner?.ProfitChangedCount ?? 0),
    changedLineCodes: Array.isArray(codesRaw) ? codesRaw.map((x: unknown) => String(x)) : [],
    failedCount: Number(inner?.failedCount ?? inner?.FailedCount ?? 0),
    failedMessages: normalizeStringList(inner?.failedMessages ?? inner?.FailedMessages)
  }
}

/** POST /api/v1/debug/refresh-financepayment-remark-from-legacy */
export async function refreshFinancePaymentRemarkFromLegacy(): Promise<RefreshFinancePaymentLegacyRemarkResult> {
  const raw = await apiClient.post<any>('/api/v1/debug/refresh-financepayment-remark-from-legacy', {})
  const outer = (raw?.data ?? raw?.Data ?? raw) as Record<string, any>
  const inner = (outer?.data ?? outer?.Data ?? outer) as Record<string, any>
  return {
    totalPaymentsRemarkNonEmpty: Number(inner?.totalPaymentsRemarkNonEmpty ?? inner?.TotalPaymentsRemarkNonEmpty ?? 0),
    legacyPackedCandidates: Number(inner?.legacyPackedCandidates ?? inner?.LegacyPackedCandidates ?? 0),
    parsedAndApplied: Number(inner?.parsedAndApplied ?? inner?.ParsedAndApplied ?? 0),
    skippedMalformed: Number(inner?.skippedMalformed ?? inner?.SkippedMalformed ?? 0),
    itemsLineRemarkUpdated: Number(inner?.itemsLineRemarkUpdated ?? inner?.ItemsLineRemarkUpdated ?? 0),
    bankIdsResolvedFromName: Number(inner?.bankIdsResolvedFromName ?? inner?.BankIdsResolvedFromName ?? 0)
  }
}

function normalizeStringList(raw: unknown): string[] {
  return Array.isArray(raw) ? raw.map((x: unknown) => String(x)) : []
}

/** POST /api/v1/debug/refresh-finance-receivables-from-stock-outs — 为已完成出库补生成应收款 */
export async function refreshFinanceReceivablesFromStockOuts(): Promise<RefreshFinanceReceivablesFromStockOutsResult> {
  const raw = await apiClient.post<any>('/api/v1/debug/refresh-finance-receivables-from-stock-outs', {})
  const outer = (raw?.data ?? raw?.Data ?? raw) as Record<string, any>
  const inner = (outer?.data ?? outer?.Data ?? outer) as Record<string, any>
  return {
    totalCompletedSalesStockOuts: Number(inner?.totalCompletedSalesStockOuts ?? inner?.TotalCompletedSalesStockOuts ?? 0),
    alreadyHasReceivableCount: Number(inner?.alreadyHasReceivableCount ?? inner?.AlreadyHasReceivableCount ?? 0),
    candidateCount: Number(inner?.candidateCount ?? inner?.CandidateCount ?? 0),
    createdCount: Number(inner?.createdCount ?? inner?.CreatedCount ?? 0),
    skippedIneligibleCount: Number(inner?.skippedIneligibleCount ?? inner?.SkippedIneligibleCount ?? 0),
    failedCount: Number(inner?.failedCount ?? inner?.FailedCount ?? 0),
    stockOutDatesSyncedCount: Number(inner?.stockOutDatesSyncedCount ?? inner?.StockOutDatesSyncedCount ?? 0),
    prematureReceivablesRemovedCount: Number(
      inner?.prematureReceivablesRemovedCount ?? inner?.PrematureReceivablesRemovedCount ?? 0
    ),
    createdStockOutCodes: normalizeStringList(inner?.createdStockOutCodes ?? inner?.CreatedStockOutCodes),
    skippedIneligibleStockOutCodes: normalizeStringList(
      inner?.skippedIneligibleStockOutCodes ?? inner?.SkippedIneligibleStockOutCodes
    ),
    stockOutDatesSyncedStockOutCodes: normalizeStringList(
      inner?.stockOutDatesSyncedStockOutCodes ?? inner?.StockOutDatesSyncedStockOutCodes
    ),
    prematureReceivablesRemovedStockOutCodes: normalizeStringList(
      inner?.prematureReceivablesRemovedStockOutCodes ?? inner?.PrematureReceivablesRemovedStockOutCodes
    ),
    failedStockOutCodes: normalizeStringList(inner?.failedStockOutCodes ?? inner?.FailedStockOutCodes),
    failedMessages: normalizeStringList(inner?.failedMessages ?? inner?.FailedMessages)
  }
}

export type RefreshRfqMaterialIntelCacheResult = {
  totalRfqItemRows: number
  distinctPnCount: number
  alreadyCachedCount: number
  invokedCount: number
  failedCount: number
  invokedPns: string[]
  failedPns: string[]
  failedMessages: string[]
}

function normalizeRefreshRfqMaterialIntelCacheResult(raw: unknown): RefreshRfqMaterialIntelCacheResult {
  const outer = (raw as Record<string, unknown> | null | undefined)
  const inner = (outer?.data ?? outer?.Data ?? outer) as Record<string, unknown> | null | undefined
  return {
    totalRfqItemRows: Number(inner?.totalRfqItemRows ?? inner?.TotalRfqItemRows ?? 0),
    distinctPnCount: Number(inner?.distinctPnCount ?? inner?.DistinctPnCount ?? 0),
    alreadyCachedCount: Number(inner?.alreadyCachedCount ?? inner?.AlreadyCachedCount ?? 0),
    invokedCount: Number(inner?.invokedCount ?? inner?.InvokedCount ?? 0),
    failedCount: Number(inner?.failedCount ?? inner?.FailedCount ?? 0),
    invokedPns: normalizeStringList(inner?.invokedPns ?? inner?.InvokedPns),
    failedPns: normalizeStringList(inner?.failedPns ?? inner?.FailedPns),
    failedMessages: normalizeStringList(inner?.failedMessages ?? inner?.FailedMessages)
  }
}

/** POST /api/v1/debug/refresh-rfq-material-intel-cache — 为无 AI 缓存的 RFQ 物料型号批量触发查询 */
export async function refreshRfqMaterialIntelCache(): Promise<RefreshRfqMaterialIntelCacheResult> {
  const raw = await apiClient.post<unknown>(
    '/api/v1/debug/refresh-rfq-material-intel-cache',
    {},
    { timeout: 3_600_000 }
  )
  return normalizeRefreshRfqMaterialIntelCacheResult(raw)
}
