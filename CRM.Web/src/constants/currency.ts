export enum CurrencyCode {
  RMB = 1,
  USD = 2,
  EUR = 3,
  HKD = 4,
  JPY = 5,
  GBP = 6,
}

export type CurrencyOption = { label: string; value: CurrencyCode }

// Single source of truth for all currency dropdowns (1-based codes)
export const CURRENCY_OPTIONS: CurrencyOption[] = [
  { label: '人民币(RMB)', value: CurrencyCode.RMB },
  { label: '美元(USD)', value: CurrencyCode.USD },
  { label: '欧元(EUR)', value: CurrencyCode.EUR },
  { label: '港币(HKD)', value: CurrencyCode.HKD },
  { label: '日元(JPY)', value: CurrencyCode.JPY },
  { label: '英镑(GBP)', value: CurrencyCode.GBP },
]

export const CURRENCY_ISO_OPTIONS: CurrencyOption[] = [
  { label: 'RMB', value: CurrencyCode.RMB },
  { label: 'USD', value: CurrencyCode.USD },
  { label: 'EUR', value: CurrencyCode.EUR },
  { label: 'HKD', value: CurrencyCode.HKD },
  { label: 'JPY', value: CurrencyCode.JPY },
  { label: 'GBP', value: CurrencyCode.GBP },
]

// Common CRM settlement currency order requirement (Customer/Vendor)
export const SETTLEMENT_CURRENCY_OPTIONS: CurrencyOption[] = [
  { label: '人民币(RMB)', value: CurrencyCode.RMB },
  { label: '港币(HKD)', value: CurrencyCode.HKD },
  { label: '美金(USD)', value: CurrencyCode.USD },
  { label: '欧元(EUR)', value: CurrencyCode.EUR },
]

export const CURRENCY_CODE_TO_TEXT: Record<number, string> = {
  [CurrencyCode.RMB]: 'RMB',
  [CurrencyCode.USD]: 'USD',
  [CurrencyCode.EUR]: 'EUR',
  [CurrencyCode.HKD]: 'HKD',
  [CurrencyCode.JPY]: 'JPY',
  [CurrencyCode.GBP]: 'GBP',
}

