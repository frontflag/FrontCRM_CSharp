/// <reference types="vite/client" />

/** Injected in vite.config.ts from package.json at build time */
declare const __APP_VERSION__: string

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string
  readonly VITE_TENANT_ID?: string
  readonly VITE_APP_BRAND_TITLE?: string
  readonly VITE_LOGIN_LAYOUT?: string
  readonly VITE_LOGIN_SLOGAN_LINE1?: string
  readonly VITE_LOGIN_SLOGAN_LINE2?: string
  readonly VITE_LOGIN_FEATURE_1?: string
  readonly VITE_LOGIN_FEATURE_2?: string
  readonly VITE_LOGIN_FEATURE_3?: string
  readonly VITE_LOGIN_WELCOME_TITLE?: string
  readonly VITE_LOGIN_WELCOME_SUB?: string
  readonly VITE_LOGIN_COPYRIGHT?: string
  readonly VITE_LOGIN_DESCRIPTION?: string
  readonly VITE_LOGIN_ACCOUNT_PLACEHOLDER?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
