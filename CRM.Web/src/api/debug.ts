import apiClient from './client'

export type DebugItem = {
  name: string
  value: string
}

/** 与后端 DebugPageDto 一致（版本号由前端 PRD 硬编码，不在此接口） */
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
