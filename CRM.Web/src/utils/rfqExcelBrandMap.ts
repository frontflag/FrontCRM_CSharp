import { parseAiJsonObject } from '@/utils/aiJson'

export type RfqExcelAiBrandMapping = {
  sourceText: string
  standardBrand: string | null
  confidence?: number | null
}

export type RfqExcelAiBrandMapResult = {
  mappings: RfqExcelAiBrandMapping[]
}

function norm(s: unknown): string {
  return String(s ?? '').trim()
}

function normalizeAiBrandMapResult(raw: Record<string, unknown>): RfqExcelAiBrandMapResult | null {
  const mappingsRaw = raw.mappings ?? raw.Mappings
  if (!Array.isArray(mappingsRaw)) return null

  const mappings: RfqExcelAiBrandMapping[] = []
  for (const item of mappingsRaw) {
    if (!item || typeof item !== 'object' || Array.isArray(item)) continue
    const row = item as Record<string, unknown>
    const sourceText = norm(row.source_text ?? row.sourceText ?? row.SourceText)
    const standardBrand = norm(row.standard_brand ?? row.standardBrand ?? row.StandardBrand) || null
    if (!sourceText || !standardBrand) continue
    mappings.push({
      sourceText,
      standardBrand,
      confidence: row.confidence != null ? Number(row.confidence) : null
    })
  }

  if (!mappings.length) return null
  return { mappings }
}

export function parseAiBrandMapResponse(data: unknown, content: string): RfqExcelAiBrandMapResult | null {
  const tryNormalize = (raw: unknown) => {
    if (!raw || typeof raw !== 'object' || Array.isArray(raw)) return null
    return normalizeAiBrandMapResult(raw as Record<string, unknown>)
  }

  const fromData = tryNormalize(data)
  if (fromData) return fromData

  return tryNormalize(parseAiJsonObject(null, content))
}

export function buildAiBrandMapInput(sourceTexts: string[]): { sourceTextsJson: string } {
  const unique = [...new Set(sourceTexts.map((s) => norm(s)).filter(Boolean))]
  return { sourceTextsJson: JSON.stringify(unique) }
}
