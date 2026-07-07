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
  fallback: string
): string {
  const v = import.meta.env[envKey]?.trim()
  return v || fallback
}

/** 顶栏 Logo 旁系统名称（layout.brandFull），见 VITE_APP_BRAND_TITLE */
export function appBrandTitle(fallback: string): string {
  const v = import.meta.env.VITE_APP_BRAND_TITLE?.trim()
  return v || fallback
}
