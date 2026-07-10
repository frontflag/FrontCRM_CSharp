/**
 * 币别枚举与下拉选项（与后端 short 编码一致）
 *
 * 业务规范：表单中「币别 / 结算币别 / 币种」类下拉仅使用 SETTLEMENT_CURRENCY_OPTIONS，
 * 顺序与文案与客户编辑页「结算货币」一致，勿在页面内手写 el-option。
 * 规范文档：document/PRD/规范/业务规范/结算币别下拉规范.md
 */
export enum CurrencyCode {
  RMB = 1,
  USD = 2,
  EUR = 3,
  HKD = 4,
  JPY = 5,
  GBP = 6,
}

export type CurrencyOption = { label: string; value: CurrencyCode };

/** 新建表单 / 空值时的默认结算币别（与下拉首项 USD 一致） */
export const DEFAULT_SETTLEMENT_CURRENCY_CODE = CurrencyCode.USD;
export const DEFAULT_SETTLEMENT_CURRENCY_STRING = 'USD';

/** 客户/供应商「结算货币」及全系统币种下拉（仅四项，顺序固定；展示为 ISO 代码） */
export const SETTLEMENT_CURRENCY_OPTIONS: CurrencyOption[] = [
  { label: 'USD', value: CurrencyCode.USD },
  { label: 'RMB', value: CurrencyCode.RMB },
  { label: 'HKD', value: CurrencyCode.HKD },
  { label: 'EUR', value: CurrencyCode.EUR },
];

/**
 * 使用字符串代码的表单（如 BOM、RFQ 行上的 RMB/HKD/USD/EUR），
 * 标签与顺序同 SETTLEMENT_CURRENCY_OPTIONS。
 */
export const SETTLEMENT_CURRENCY_STRING_OPTIONS: { label: string; value: string }[] = [
  { label: 'USD', value: 'USD' },
  { label: 'RMB', value: 'RMB' },
  { label: 'HKD', value: 'HKD' },
  { label: 'EUR', value: 'EUR' },
];

/**
 * 完整枚举（结算四项 + 日元 + 英镑）。表单下拉请优先用 SETTLEMENT_CURRENCY_OPTIONS；
 * 此列表可用于需展示历史 JPY/GBP 等场景的扩展下拉。
 */
export const CURRENCY_OPTIONS: CurrencyOption[] = [
  ...SETTLEMENT_CURRENCY_OPTIONS,
  { label: '日元(JPY)', value: CurrencyCode.JPY },
  { label: '英镑(GBP)', value: CurrencyCode.GBP },
];

/** 与 SETTLEMENT_CURRENCY_OPTIONS 一致（报价阶梯等历史 import 名） */
export const CURRENCY_ISO_OPTIONS: CurrencyOption[] = SETTLEMENT_CURRENCY_OPTIONS;

export const CURRENCY_CODE_TO_TEXT: Record<number, string> = {
  [CurrencyCode.RMB]: 'RMB',
  [CurrencyCode.USD]: 'USD',
  [CurrencyCode.EUR]: 'EUR',
  [CurrencyCode.HKD]: 'HKD',
  [CurrencyCode.JPY]: 'JPY',
  [CurrencyCode.GBP]: 'GBP',
};

/** 人民币明细在报表/价税拆分中使用的增值税率（小数）；外币无增值税。规范见 document/PRD/规范/业务规范/销售与采购订单增值税与币别规范.md */
export const SETTLEMENT_RMB_VAT_RATE_DECIMAL = 0.13

/**
 * 按明细结算币别返回增值税率（小数）。仅 `CurrencyCode.RMB`(1) 为 13%，其余为 0。
 * 用于销售单明细、采购单明细及采购订单打印报表等与币别一致的价税推算。
 */
export function settlementVatRateDecimal(currencyCode: number | undefined | null): number {
  const c = Number(currencyCode)
  return c === CurrencyCode.RMB ? SETTLEMENT_RMB_VAT_RATE_DECIMAL : 0
}

const SETTLEMENT_CURRENCY_LABEL_TO_CODE: Record<string, CurrencyCode> = {
  RMB: CurrencyCode.RMB,
  USD: CurrencyCode.USD,
  EUR: CurrencyCode.EUR,
  HKD: CurrencyCode.HKD,
}

/**
 * 将接口/列表中的币别原始值规范为与 SETTLEMENT_CURRENCY_OPTIONS 一致的 short 编码。
 * 支持：统一 1-based 编码、历史 0-based quoteitem 编码、ISO 字符串（RMB/USD/…）。
 */
export function normalizeSettlementCurrencyCode(raw: unknown): CurrencyCode {
  if (raw == null || raw === '') return DEFAULT_SETTLEMENT_CURRENCY_CODE

  if (typeof raw === 'string') {
    const s = raw.trim().toUpperCase()
    const byLabel = SETTLEMENT_CURRENCY_LABEL_TO_CODE[s]
    if (byLabel != null) return byLabel
    const n = Number(s)
    if (Number.isFinite(n)) return normalizeSettlementCurrencyCode(n)
    return DEFAULT_SETTLEMENT_CURRENCY_CODE
  }

  const n = Number(raw)
  if (!Number.isFinite(n)) return DEFAULT_SETTLEMENT_CURRENCY_CODE

  if (n >= CurrencyCode.RMB && n <= CurrencyCode.GBP) return n as CurrencyCode

  // 历史 quoteitem.currency：0=RMB 1=USD 2=EUR 3=HKD（迁移前）
  if (n >= 0 && n <= 3) return (n + 1) as CurrencyCode

  return DEFAULT_SETTLEMENT_CURRENCY_CODE
}
