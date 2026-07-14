import { bizBrandApi, type BizBrandOption } from '@/api/bizBrand'

const ALIAS_TOKEN_SEPARATORS = /[,;\n\r，、|/]/

export type BrandMatchStatus = 'matched' | 'pending' | 'empty'
export type BrandMappingSource = 'learned' | 'rule' | 'ai'

export type BrandMatchResult = {
  status: BrandMatchStatus
  brandId?: number
  standardBrand?: string
  matchKeyword?: string
  mappingSource?: BrandMappingSource
}

function norm(s: string | null | undefined): string {
  return (s || '').trim()
}

function equalsIgnoreCase(a: string, b: string): boolean {
  return a.toLowerCase() === b.toLowerCase()
}

function toHalfWidth(input: string): string {
  let out = ''
  for (const ch of input) {
    const code = ch.charCodeAt(0)
    if (code >= 0xff01 && code <= 0xff5e) out += String.fromCharCode(code - 0xfee0)
    else if (code === 0x3000) out += ' '
    else out += ch
  }
  return out
}

function collapseWhitespace(s: string): string {
  return s.replace(/\s+/g, ' ').trim()
}

/** 与后端 BizBrandSourceKeyHelper 一致：全公司学习映射查重键 */
export function normalizeBrandSourceKey(raw: string | null | undefined): string {
  const text = norm(raw)
  if (!text) return ''
  let s = text.replace(/（/g, '(').replace(/）/g, ')')
  s = toHalfWidth(s)
  s = collapseWhitespace(s)
  return s.toLowerCase()
}

/** 从导入原文拆出多个候选关键词（规则层，非品牌清单） */
export function expandBrandMatchCandidates(raw: string): string[] {
  const text = norm(raw)
  if (!text) return []

  const normalized = text.replace(/（/g, '(').replace(/）/g, ')')
  const candidates: string[] = []
  const push = (v: string) => {
    const t = norm(v)
    if (!t) return
    if (!candidates.some((c) => equalsIgnoreCase(c, t))) candidates.push(t)
  }

  push(text)
  push(normalized)

  const parenIdx = normalized.indexOf('(')
  if (parenIdx > 0) {
    push(normalized.slice(0, parenIdx))
    const inside = normalized.slice(parenIdx + 1).replace(/\).*$/, '')
    push(inside)
  }

  for (const part of normalized.split('/')) push(part)
  for (const part of normalized.split(ALIAS_TOKEN_SEPARATORS)) push(part)

  return candidates
}

/** 与后端 BizBrandAliasHelper.ContainsExactToken 一致 */
export function aliasContainsExactToken(alias: string | null | undefined, keyword: string): boolean {
  if (!alias || !keyword) return false
  const kw = keyword.trim()
  for (const token of alias.split(ALIAS_TOKEN_SEPARATORS)) {
    if (equalsIgnoreCase(token.trim(), kw)) return true
  }
  return false
}

/** 从候选列表中选取唯一匹配；多条或零条歧义时返回 null */
export function pickBizBrandMatch(keyword: string, options: BizBrandOption[]): BizBrandOption | null {
  const text = norm(keyword)
  if (!text || !options.length) return null

  const uniqueById = (list: BizBrandOption[]) => {
    const seen = new Set<number>()
    return list.filter((o) => {
      if (!o.id || o.id <= 0 || seen.has(o.id)) return false
      seen.add(o.id)
      return true
    })
  }

  const exactStandard = uniqueById(options.filter((o) => equalsIgnoreCase(norm(o.standardBrand), text)))
  if (exactStandard.length === 1) return exactStandard[0]
  if (exactStandard.length > 1) return null

  const exactEN = uniqueById(options.filter((o) => equalsIgnoreCase(norm(o.brandEName), text)))
  if (exactEN.length === 1) return exactEN[0]
  if (exactEN.length > 1) return null

  const exactCN = uniqueById(options.filter((o) => equalsIgnoreCase(norm(o.brandCName), text)))
  if (exactCN.length === 1) return exactCN[0]
  if (exactCN.length > 1) return null

  const aliasExact = uniqueById(options.filter((o) => aliasContainsExactToken(o.alias, text)))
  if (aliasExact.length === 1) return aliasExact[0]
  if (aliasExact.length > 1) return null

  if (options.length === 1) return options[0]

  return null
}

/** D 列为空时用 C 列（客户品牌）兜底 */
export function resolveBrandMatchKeyword(supplyBrand?: string, customerBrand?: string): string {
  const d = norm(supplyBrand)
  if (d) return d
  return norm(customerBrand)
}

export async function fetchBrandOptionsForKeyword(keyword: string): Promise<BizBrandOption[]> {
  const text = norm(keyword)
  if (!text) return []
  return bizBrandApi.fetchOptions({ keyword: text, pageSize: 50 })
}

function cacheLookupKey(keyword: string): string {
  return normalizeBrandSourceKey(keyword)
}

async function matchBrandKeywordWithRules(
  keyword: string,
  learnedByKey: Map<string, BrandMatchResult>
): Promise<BrandMatchResult> {
  const text = norm(keyword)
  if (!text) return { status: 'empty' }

  const sourceKey = normalizeBrandSourceKey(text)
  const learned = learnedByKey.get(sourceKey)
  if (learned?.status === 'matched' && learned.brandId) {
    return { ...learned, matchKeyword: text }
  }

  for (const candidate of expandBrandMatchCandidates(text)) {
    try {
      const opts = await fetchBrandOptionsForKeyword(candidate)
      const match = pickBizBrandMatch(candidate, opts)
      if (match) {
        return {
          status: 'matched',
          brandId: match.id,
          standardBrand: norm(match.standardBrand) || candidate,
          matchKeyword: text,
          mappingSource: 'rule'
        }
      }
    } catch {
      // try next candidate
    }
  }

  return { status: 'pending', matchKeyword: text }
}

export async function matchBrandKeyword(keyword: string): Promise<BrandMatchResult> {
  const cache = await buildBrandMatchCache([keyword])
  return cache.get(cacheLookupKey(keyword)) ?? { status: 'pending', matchKeyword: norm(keyword) }
}

export async function buildBrandMatchCache(
  keywords: string[]
): Promise<Map<string, BrandMatchResult>> {
  const uniqueTexts: string[] = []
  const seenKeys = new Set<string>()
  for (const raw of keywords) {
    const text = norm(raw)
    if (!text) continue
    const sk = normalizeBrandSourceKey(text)
    if (seenKeys.has(sk)) continue
    seenKeys.add(sk)
    uniqueTexts.push(text)
  }

  const learnedByKey = new Map<string, BrandMatchResult>()
  if (uniqueTexts.length > 0) {
    try {
      const learnedRows = await bizBrandApi.resolveLearnedMappings({ sourceTexts: uniqueTexts })
      for (const row of learnedRows) {
        const sk = normalizeBrandSourceKey(row.sourceText)
        if (!sk || !row.brandId) continue
        learnedByKey.set(sk, {
          status: 'matched',
          brandId: row.brandId,
          standardBrand: norm(row.standardBrand) || row.sourceText,
          matchKeyword: row.sourceText,
          mappingSource: 'learned'
        })
      }
    } catch {
      // learned lookup optional
    }
  }

  const cache = new Map<string, BrandMatchResult>()
  for (const text of uniqueTexts) {
    const result = await matchBrandKeywordWithRules(text, learnedByKey)
    cache.set(cacheLookupKey(text), result)
  }
  return cache
}

export async function rememberLearnedBrandMapping(sourceText: string, brandId: number): Promise<void> {
  const text = norm(sourceText)
  if (!text || brandId <= 0) return
  await bizBrandApi.rememberLearnedMapping({ sourceText: text, brandId })
}

export async function resolveBrandIdsForItems(
  items: Array<{ brand?: string; brandId?: number; customerBrand?: string }>,
  options?: { silent?: boolean; onWarning?: (msg: string) => void }
): Promise<void> {
  const warn = options?.onWarning ?? (() => {})
  const silent = options?.silent ?? false

  const keywords: string[] = []
  for (const it of items) {
    if (it.brandId && it.brandId > 0) continue
    const kw = resolveBrandMatchKeyword(it.brand, it.customerBrand)
    if (kw) keywords.push(kw)
  }
  const cache = await buildBrandMatchCache(keywords)

  for (let i = 0; i < items.length; i++) {
    const it = items[i]
    if (it.brandId && it.brandId > 0) continue
    const kw = resolveBrandMatchKeyword(it.brand, it.customerBrand)
    if (!kw) {
      if (!silent) warn(`明细 ${i + 1}：请选择供应品牌`)
      continue
    }
    const result = cache.get(cacheLookupKey(kw))
    if (result?.status === 'matched' && result.brandId) {
      it.brandId = result.brandId
      it.brand = result.standardBrand
    } else {
      it.brand = kw
      if (!silent) warn(`明细 ${i + 1}：品牌「${kw}」未能自动匹配，请手动选择`)
    }
  }
}

export function brandMatchStatusLabel(result: BrandMatchResult): string {
  if (result.status === 'matched') return result.standardBrand || '已匹配'
  if (result.status === 'pending') return result.matchKeyword ? `待选择（${result.matchKeyword}）` : '待选择'
  return '缺少品牌'
}
