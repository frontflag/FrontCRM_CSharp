/** 构建时注入：semicore | idesemi | ecoinf（见 .env.production.*） */
export const LOGIN_TENANT_ID = (import.meta.env.VITE_TENANT_ID?.trim() || 'semicore').toLowerCase()

/** left-right：口号左 / 登录右；right-left：登录左 / 口号右 */
export type LoginPageLayout = 'left-right' | 'right-left'

export function loginPageLayout(): LoginPageLayout {
  const raw = import.meta.env.VITE_LOGIN_LAYOUT?.trim().toLowerCase()
  if (raw === 'right-left' || raw === 'rl') return 'right-left'
  return 'left-right'
}

/** 登录页静态主题 CSS（public/tenant/{id}/theme.css → dist/tenant/...） */
export function loginThemeCssHref(tenantId = LOGIN_TENANT_ID): string {
  return `/tenant/${tenantId}/theme.css`
}

function isChineseLocale(locale?: string): boolean {
  if (!locale) return true
  return locale.toLowerCase().startsWith('zh')
}

/**
 * 登录页租户文案：中文可用 .env 覆盖；非中文语言一律走 i18n fallback，
 * 避免 VITE_LOGIN_* 中文硬编码在英文模式下仍显示。
 */
export function loginTenantText(
  envKey:
    | 'VITE_LOGIN_SLOGAN_LINE1'
    | 'VITE_LOGIN_SLOGAN_LINE2'
    | 'VITE_LOGIN_COPYRIGHT'
    | 'VITE_LOGIN_WELCOME_TITLE'
    | 'VITE_LOGIN_WELCOME_SUB'
    | 'VITE_LOGIN_FEATURE_1'
    | 'VITE_LOGIN_FEATURE_2'
    | 'VITE_LOGIN_FEATURE_3',
  fallback: string,
  locale?: string
): string {
  if (!isChineseLocale(locale)) return fallback
  const v = import.meta.env[envKey]?.trim()
  return v || fallback
}

/** 顶栏 Logo 旁系统名称（layout.brandFull），见 VITE_APP_BRAND_TITLE */
export function appBrandTitle(fallback: string): string {
  const v = import.meta.env.VITE_APP_BRAND_TITLE?.trim()
  return v || fallback
}

/**
 * 顶栏可见标题：semicore 字标已含品牌名，去掉重复前缀（如 Semicore）。
 * 浏览器标签、设置页仍用完整 `appBrandTitle`。idesemi / ecoinf 不裁。
 */
export function appBrandHeaderTitle(fullTitle: string, tenantId = LOGIN_TENANT_ID): string {
  const title = fullTitle.trim()
  if (tenantId !== 'semicore' || !title) return title
  const stripped = title.replace(/^Semicore\s+/i, '').trim()
  return stripped || title
}

/**
 * semicore 顶栏/登录页用公司信息横版字标（保横纵比，不裁成 36×36）。
 * idesemi / ecoinf 仍用原小标布局。
 */
export function usesCompanyProfileWordmarkLogo(tenantId = LOGIN_TENANT_ID): boolean {
  return tenantId === 'semicore'
}
