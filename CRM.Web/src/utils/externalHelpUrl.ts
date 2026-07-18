import mapping from '@/assets/help-mapping.json'

export type AppLocale = 'zh-CN' | 'en-US'

export interface HelpMappingEntry {
  routeName: string
  pageId: string
  context?: string
}

export interface HelpMapping {
  siteUrl: string
  defaultLocale: string
  entries: HelpMappingEntry[]
}

const localeMap: Record<AppLocale, string> = {
  'zh-CN': 'zh',
  'en-US': 'en'
}

export function getExternalHelpLocale(appLocale: string): string {
  return localeMap[appLocale as AppLocale] ?? mapping.defaultLocale
}

export function getExternalHelpUrl(
  routeName: string | null | undefined,
  context?: string,
  appLocale: string = 'zh-CN'
): string {
  const name = routeName ?? ''
  const entry = (mapping as HelpMapping).entries.find(
    e => e.routeName === name && (!context || e.context === context)
  )
  const pageId = entry?.pageId ?? 'index'
  const locale = getExternalHelpLocale(appLocale)
  const url = new URL(`${mapping.siteUrl}/${locale}/${pageId}`)
  if (name) {
    url.searchParams.set('source', 'frontcrm')
    url.searchParams.set('route', name)
  }
  return url.toString()
}

export function getExternalHelpUrlById(
  pageId: string,
  appLocale: string = 'zh-CN'
): string {
  const locale = getExternalHelpLocale(appLocale)
  return `${mapping.siteUrl}/${locale}/${pageId}`
}
