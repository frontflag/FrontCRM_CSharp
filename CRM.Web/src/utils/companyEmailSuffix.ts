/** 客户/供应商企业邮箱：只存后缀（如 @huawei.com）。与后端 CompanyEmailSuffix 对齐。 */

export const COMPANY_EMAIL_SUFFIX_MAX_LEN = 128

export const COMPANY_EMAIL_SUFFIX_INVALID = '企业邮箱须为邮箱后缀，如 @xxx.com'

const PUBLIC_SUFFIXES = new Set([
  '@qq.com',
  '@gmail.com',
  '@163.com',
  '@126.com',
  '@sina.com',
  '@sina.cn',
  '@hotmail.com',
  '@outlook.com',
  '@live.com',
  '@msn.com',
  '@yahoo.com',
  '@yahoo.com.cn',
  '@icloud.com',
  '@me.com',
  '@foxmail.com',
  '@yeah.net',
  '@139.com',
  '@189.cn',
  '@sohu.com',
  '@aliyun.com',
  '@tom.com',
  '@aol.com',
  '@proton.me',
  '@protonmail.com',
  '@mail.com',
  '@gmx.com',
  '@yandex.com'
])

const DOMAIN_RE = /^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z]{2,63}$/

export function normalizeCompanyEmailSuffix(input?: string | null): string | null {
  if (input == null) return null
  const raw = String(input).trim().toLowerCase().replace(/[\s\u3000]/g, '')
  if (!raw) return null
  const at = raw.lastIndexOf('@')
  const domain = (at >= 0 ? raw.slice(at + 1) : raw.replace(/^@+/, '')).replace(/^\.+|\.+$/g, '')
  if (!domain) return null
  return `@${domain}`
}

export function isPublicCompanyEmailSuffix(input?: string | null): boolean {
  const n = normalizeCompanyEmailSuffix(input)
  return n != null && PUBLIC_SUFFIXES.has(n)
}

export function tryNormalizeCompanyEmailSuffix(input?: string | null): {
  suffix: string | null
  error: string | null
} {
  if (input == null || String(input).trim() === '') return { suffix: null, error: null }
  const n = normalizeCompanyEmailSuffix(input)
  if (!n || n.length > COMPANY_EMAIL_SUFFIX_MAX_LEN || !DOMAIN_RE.test(n.slice(1))) {
    return { suffix: null, error: COMPANY_EMAIL_SUFFIX_INVALID }
  }
  return { suffix: n, error: null }
}

/** 联系人完整邮箱 → 去重后缀；公共域不进入下拉。 */
export function contactEmailSuffixOptions(emails: Array<string | null | undefined>): string[] {
  const seen = new Set<string>()
  const out: string[] = []
  for (const e of emails) {
    const { suffix, error } = tryNormalizeCompanyEmailSuffix(e)
    if (!suffix || error || isPublicCompanyEmailSuffix(suffix) || seen.has(suffix)) continue
    seen.add(suffix)
    out.push(suffix)
  }
  return out
}
