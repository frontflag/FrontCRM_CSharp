export type AiPrefillEntityType = 'CUSTOMER' | 'RFQ' | 'VENDOR' | 'CUSTOMER_CONTACT' | 'VENDOR_CONTACT' | 'CUSTOMER_ADDRESS' | 'VENDOR_ADDRESS'

const STORAGE_KEY = 'crm.aiPrefill.v1'
const TTL_MS = 30 * 60 * 1000

type StoredPrefill = {
  token: string
  entityType: AiPrefillEntityType
  payload: Record<string, unknown>
  parseLogId?: string | null
  at: number
}

export type AiPrefillConsumed = {
  payload: Record<string, unknown>
  parseLogId: string | null
}

export function setAiPrefill(
  entityType: AiPrefillEntityType,
  payload: Record<string, unknown>,
  parseLogId?: string | null
): string {
  const token =
    typeof crypto !== 'undefined' && crypto.randomUUID
      ? crypto.randomUUID()
      : `pf-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`
  const record: StoredPrefill = {
    token,
    entityType,
    payload,
    parseLogId: parseLogId?.trim() || null,
    at: Date.now()
  }
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(record))
  return token
}

export function consumeAiPrefill(entityType: AiPrefillEntityType, token: string): AiPrefillConsumed | null {
  if (!token?.trim()) return null
  const raw = sessionStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  try {
    const record = JSON.parse(raw) as StoredPrefill
    if (record.entityType !== entityType || record.token !== token) return null
    if (Date.now() - (record.at || 0) > TTL_MS) {
      sessionStorage.removeItem(STORAGE_KEY)
      return null
    }
    sessionStorage.removeItem(STORAGE_KEY)
    const payload = record.payload && typeof record.payload === 'object' ? record.payload : null
    if (!payload) return null
    return {
      payload,
      parseLogId: record.parseLogId?.trim() || null
    }
  } catch {
    sessionStorage.removeItem(STORAGE_KEY)
    return null
  }
}
