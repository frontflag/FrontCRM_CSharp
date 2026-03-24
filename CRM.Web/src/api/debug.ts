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
