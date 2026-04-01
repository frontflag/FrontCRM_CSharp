import { createI18n } from 'vue-i18n'
import zhCN from '@/locales/zh-CN'
import enUS from '@/locales/en-US'

export const LOCALE_STORAGE_KEY = 'crm_locale'
export type AppLocale = 'zh-CN' | 'en-US'

/** 首次访问或未选择过时使用英文；用户切换语言后由 `setAppLocale` 写入 localStorage，下次打开/登录沿用 */
const defaultLocale: AppLocale = 'en-US'

const fallbackLocale: AppLocale = 'en-US'

export function resolveInitialLocale(): AppLocale {
  try {
    const saved = localStorage.getItem(LOCALE_STORAGE_KEY)
    if (saved === 'zh-CN' || saved === 'en-US') return saved
  } catch {
    /* private mode / blocked storage */
  }
  return defaultLocale
}

const i18n = createI18n({
  legacy: false,
  locale: resolveInitialLocale(),
  fallbackLocale,
  messages: {
    'zh-CN': zhCN,
    'en-US': enUS
  }
})

export function setAppLocale(locale: AppLocale) {
  i18n.global.locale.value = locale
  try {
    localStorage.setItem(LOCALE_STORAGE_KEY, locale)
  } catch {
    /* still apply UI locale for this session */
  }
}

export default i18n
