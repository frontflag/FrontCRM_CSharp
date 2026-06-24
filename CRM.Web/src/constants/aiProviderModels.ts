/** 各 AI 厂商可选模型（管理端下拉）；未知模型仍可通过当前值保留在选项中 */
export const AI_PROVIDER_MODEL_PRESETS: Record<string, string[]> = {
  mock: ['mock'],
  moonshot: [
    'kimi-k2.5',
    'kimi-k2.6',
    'kimi-k2.7-code',
    'kimi-k2.7-code-highspeed'
  ]
}

export function buildModelOptions(providerCode: string, defaultModel?: string | null, currentModel?: string | null): string[] {
  const code = (providerCode ?? '').trim()
  const set = new Set<string>()
  for (const m of AI_PROVIDER_MODEL_PRESETS[code] ?? []) {
    if (m) set.add(m)
  }
  const def = (defaultModel ?? '').trim()
  if (def) set.add(def)
  const cur = (currentModel ?? '').trim()
  if (cur) set.add(cur)
  return Array.from(set)
}
