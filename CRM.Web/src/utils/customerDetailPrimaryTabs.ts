export const CUSTOMER_DETAIL_PRIMARY_TAB_KEYS = [
  'profile',
  'portrait',
  'statement',
  'opportunity',
  'followUp',
  'timeline',
  'news'
] as const

export type CustomerDetailPrimaryTab = (typeof CUSTOMER_DETAIL_PRIMARY_TAB_KEYS)[number]

export const CUSTOMER_DETAIL_PRIMARY_TAB_DEFAULT: CustomerDetailPrimaryTab = 'profile'

export const CUSTOMER_DETAIL_PRIMARY_TAB_PLACEHOLDERS: readonly CustomerDetailPrimaryTab[] =
  CUSTOMER_DETAIL_PRIMARY_TAB_KEYS.filter(
    (k) =>
      k !== CUSTOMER_DETAIL_PRIMARY_TAB_DEFAULT &&
      k !== 'statement' &&
      k !== 'portrait' &&
      k !== 'news'
  )

export const CUSTOMER_PORTRAIT_SUB_TAB_KEYS = ['rfq', 'order'] as const

export type CustomerPortraitSubTab = (typeof CUSTOMER_PORTRAIT_SUB_TAB_KEYS)[number]

export const CUSTOMER_PORTRAIT_SUB_TAB_DEFAULT: CustomerPortraitSubTab = 'rfq'

export function parseCustomerPortraitSubTab(raw: unknown): CustomerPortraitSubTab {
  const s = String(Array.isArray(raw) ? raw[0] : raw ?? '').trim()
  return (CUSTOMER_PORTRAIT_SUB_TAB_KEYS as readonly string[]).includes(s)
    ? (s as CustomerPortraitSubTab)
    : CUSTOMER_PORTRAIT_SUB_TAB_DEFAULT
}

export function parseCustomerDetailPrimaryTab(raw: unknown): CustomerDetailPrimaryTab {
  const s = String(Array.isArray(raw) ? raw[0] : raw ?? '').trim()
  return (CUSTOMER_DETAIL_PRIMARY_TAB_KEYS as readonly string[]).includes(s)
    ? (s as CustomerDetailPrimaryTab)
    : CUSTOMER_DETAIL_PRIMARY_TAB_DEFAULT
}
