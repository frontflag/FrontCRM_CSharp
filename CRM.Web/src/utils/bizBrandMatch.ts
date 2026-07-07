import { bizBrandApi, type BizBrandOption } from '@/api/bizBrand'

const ALIAS_TOKEN_SEPARATORS = /[,;\n\r，、|]/

export type BrandMatchStatus = 'matched' | 'pending' | 'empty'

export type BrandMatchResult = {
  status: BrandMatchStatus
  brandId?: number
  standardBrand?: string
  matchKeyword?: string
}

function norm(s: string | null | undefined): string {
  return (s || '').trim()
}

function equalsIgnoreCase(a: string, b: string): boolean {
  return a.toLowerCase() === b.toLowerCase()
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

export async function matchBrandKeyword(keyword: string): Promise<BrandMatchResult> {
  const text = norm(keyword)
  if (!text) return { status: 'empty' }
  try {
    const opts = await fetchBrandOptionsForKeyword(text)
    const match = pickBizBrandMatch(text, opts)
    if (match) {
      return {
        status: 'matched',
        brandId: match.id,
        standardBrand: norm(match.standardBrand) || text,
        matchKeyword: text
      }
    }
    return { status: 'pending', matchKeyword: text }
  } catch {
    return { status: 'pending', matchKeyword: text }
  }
}

export async function buildBrandMatchCache(
  keywords: string[]
): Promise<Map<string, BrandMatchResult>> {
  const cache = new Map<string, BrandMatchResult>()
  const seen = new Set<string>()
  for (const raw of keywords) {
    const text = norm(raw)
    if (!text) continue
    const key = text.toLowerCase()
    if (seen.has(key)) continue
    seen.add(key)
    cache.set(key, await matchBrandKeyword(text))
  }
  return cache
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
    const result = cache.get(kw.toLowerCase())
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
