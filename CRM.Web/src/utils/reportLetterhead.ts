import type { CompanyBasicRow, CompanyLogoRow, CompanySealRow } from '@/api/companyProfile'
import { isRmbCurrency } from '@/api/companyProfile'
import { CurrencyCode } from '@/constants/currency'

/** 交易币别偏好：人民币 / 外币 / 未知（走全局默认） */
export type LetterheadPrefer = 'rmb' | 'foreign' | null

export function pickEnabledDefault<T extends { isDefault?: boolean; enabled?: boolean }>(
  rows: T[] | undefined | null
): T | undefined {
  if (!rows?.length) return undefined
  return rows.find((r) => r.isDefault && r.enabled !== false) ?? rows[0]
}

/** 行/头交易币别 → 抬头偏好；无法识别时返回 null（用全局默认） */
export function tradeCurrencyToLetterheadPrefer(
  code: number | string | null | undefined
): LetterheadPrefer {
  if (code == null || code === '') return null
  if (typeof code === 'number' || (typeof code === 'string' && /^-?\d+(\.\d+)?$/.test(code.trim()))) {
    const n = Number(code)
    if (!Number.isFinite(n)) return null
    return n === CurrencyCode.RMB ? 'rmb' : 'foreign'
  }
  return isRmbCurrency(String(code)) ? 'rmb' : 'foreign'
}

/** 取明细第一行币别；无行币别时回退抬头币别 */
export function firstLineTradeCurrency(
  lines: Array<{ currency?: number | string | null }> | undefined | null,
  headerCurrency?: number | string | null
): number | string | null | undefined {
  if (lines?.length) {
    for (const row of lines) {
      const c = row.currency
      if (c != null && c !== '') return c
    }
  }
  return headerCurrency
}

export function pickBasicLetterhead(
  rows: CompanyBasicRow[] | undefined | null,
  prefer: LetterheadPrefer
): CompanyBasicRow | undefined {
  if (!rows?.length) return undefined
  const enabled = (r: CompanyBasicRow) => r.enabled !== false
  if (prefer === 'rmb') {
    const hit = rows.find((r) => r.isDefaultRmb && enabled(r))
    if (hit) return hit
  } else if (prefer === 'foreign') {
    const hit = rows.find((r) => r.isDefaultForeign && enabled(r))
    if (hit) return hit
  }
  return pickEnabledDefault(rows)
}

/** 由已选抬头推导印章币别偏好（换抬头时印章跟抬头走） */
export function letterheadKindOf(row: CompanyBasicRow | null | undefined): LetterheadPrefer {
  if (!row) return null
  if (row.isDefaultRmb) return 'rmb'
  if (row.isDefaultForeign) return 'foreign'
  return null
}

export interface LetterheadOptionLabels {
  defaultSuffix: string
  fallbackRmb: string
  fallbackForeign: string
  fallbackDefault: string
}

/**
 * 下拉选项：已配置的人民币抬头 + 外币抬头；
 * 二者皆未配置时仅全局「默认」一项。
 * 「（默认）」仅标在按币别自动选中的那一项。
 */
export function buildLetterheadSelectOptions(
  rows: CompanyBasicRow[] | undefined | null,
  autoSelectedId: string | undefined,
  labels: LetterheadOptionLabels
): { value: string; label: string }[] {
  if (!rows?.length) return []
  const enabled = (r: CompanyBasicRow) => r.enabled !== false
  const rmb = rows.find((r) => r.isDefaultRmb && enabled(r))
  const fx = rows.find((r) => r.isDefaultForeign && enabled(r))

  const mk = (r: CompanyBasicRow, fallback: string) => {
    const name = (r.companyName || '').trim() || fallback
    const suffix = autoSelectedId && r.id === autoSelectedId ? labels.defaultSuffix : ''
    return { value: r.id, label: `${name}${suffix}` }
  }

  if (!rmb && !fx) {
    const d = pickEnabledDefault(rows)
    return d ? [mk(d, labels.fallbackDefault)] : []
  }
  const opts: { value: string; label: string }[] = []
  if (rmb) opts.push(mk(rmb, labels.fallbackRmb))
  if (fx) opts.push(mk(fx, labels.fallbackForeign))
  return opts
}

/** 自动选中 id 若不在下拉选项中，回落到选项首项 */
export function resolveLetterheadSelection(
  rows: CompanyBasicRow[] | undefined | null,
  prefer: LetterheadPrefer,
  labels: LetterheadOptionLabels
): {
  auto: CompanyBasicRow | undefined
  options: { value: string; label: string }[]
  selectedId: string
} {
  const auto = pickBasicLetterhead(rows, prefer)
  const options = buildLetterheadSelectOptions(rows, auto?.id, labels)
  const selectedId =
    auto && options.some((o) => o.value === auto.id)
      ? auto.id
      : options[0]?.value ?? auto?.id ?? ''
  return { auto, options, selectedId }
}

function hasDoc(r: { documentId?: string | null }): boolean {
  const id = r.documentId
  return typeof id === 'string' && id.trim().length > 0
}

/** Logo：始终全局默认（有文件），换抬头不变 */
export function pickReportLogoRow(rows: CompanyLogoRow[] | undefined | null): CompanyLogoRow | undefined {
  if (!rows?.length) return undefined
  const defWithDoc = rows.find((r) => r.isDefault && hasDoc(r))
  if (defWithDoc) return defWithDoc
  return rows.find((r) => hasDoc(r))
}

/**
 * 印章：跟已选抬头币别走（人民币/外币印章标记）；
 * 无对应标记时回退全局默认有文件印章。
 */
export function pickReportSealRow(
  rows: CompanySealRow[] | undefined | null,
  prefer: LetterheadPrefer = null
): CompanySealRow | undefined {
  if (!rows?.length) return undefined
  const withDoc = rows.filter((r) => r.enabled !== false && hasDoc(r))
  const pool = withDoc.length ? withDoc : rows.filter((r) => r.enabled !== false)
  const source = pool.length ? pool : rows

  if (prefer === 'rmb') {
    const hit = source.find((r) => r.isDefaultRmb && hasDoc(r)) ?? source.find((r) => r.isDefaultRmb)
    if (hit) return hit
  } else if (prefer === 'foreign') {
    const hit =
      source.find((r) => r.isDefaultForeign && hasDoc(r)) ?? source.find((r) => r.isDefaultForeign)
    if (hit) return hit
  }

  const defWithDoc = source.find((r) => r.isDefault && hasDoc(r))
  if (defWithDoc) return defWithDoc
  const anyDoc = source.find((r) => hasDoc(r))
  if (anyDoc) return anyDoc
  return source.find((r) => r.isDefault) ?? source[0]
}
