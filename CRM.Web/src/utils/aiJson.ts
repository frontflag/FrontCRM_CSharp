/** 从 AI  invoke 响应中解析 JSON 对象（兼容 markdown 代码块与前后说明文字）。 */
export function parseAiJsonObject(
  data: unknown,
  content: string
): Record<string, unknown> | null {
  const fromData = coerceRecord(data)
  if (fromData) return fromData
  return tryParseJsonObject(content)
}

function coerceRecord(data: unknown): Record<string, unknown> | null {
  if (data && typeof data === 'object' && !Array.isArray(data)) {
    return data as Record<string, unknown>
  }
  if (typeof data === 'string' && data.trim()) {
    return tryParseJsonObject(data)
  }
  return null
}

function tryParseJsonObject(text: string): Record<string, unknown> | null {
  for (const candidate of enumerateJsonCandidates(text)) {
    try {
      const parsed: unknown = JSON.parse(candidate)
      if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
        return parsed as Record<string, unknown>
      }
    } catch {
      // try next candidate
    }
  }
  return null
}

function* enumerateJsonCandidates(text: string): Generator<string> {
  const trimmed = (text ?? '').trim()
  if (!trimmed) return

  yield trimmed

  const fenced = trimmed.match(/^```(?:json|JSON)?\s*\r?\n([\s\S]*?)\r?\n?```\s*$/i)
  if (fenced?.[1]?.trim()) {
    yield fenced[1].trim()
  }

  if (trimmed.startsWith('```')) {
    const firstLineEnd = trimmed.indexOf('\n')
    if (firstLineEnd >= 0) {
      let body = trimmed.slice(firstLineEnd + 1).trimEnd()
      while (body.endsWith('`')) body = body.slice(0, -1).trimEnd()
      if (body) yield body
    }
  }

  const start = trimmed.indexOf('{')
  const end = trimmed.lastIndexOf('}')
  if (start >= 0 && end > start) {
    const slice = trimmed.slice(start, end + 1)
    if (slice !== trimmed) yield slice
  }
}
