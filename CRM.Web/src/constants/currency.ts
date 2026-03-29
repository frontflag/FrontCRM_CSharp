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

/** 客户/供应商「结算货币」及全系统币种下拉（仅四项，顺序固定） */
export const SETTLEMENT_CURRENCY_OPTIONS: CurrencyOption[] = [
  { label: '人民币(RMB)', value: CurrencyCode.RMB },
  { label: '港币(HKD)', value: CurrencyCode.HKD },
  { label: '美金(USD)', value: CurrencyCode.USD },
  { label: '欧元(EUR)', value: CurrencyCode.EUR },
];

/**
 * 使用字符串代码的表单（如 BOM、RFQ 行上的 RMB/HKD/USD/EUR），
 * 标签与顺序同 SETTLEMENT_CURRENCY_OPTIONS。
 */
export const SETTLEMENT_CURRENCY_STRING_OPTIONS: { label: string; value: string }[] = [
  { label: '人民币(RMB)', value: 'RMB' },
  { label: '港币(HKD)', value: 'HKD' },
  { label: '美金(USD)', value: 'USD' },
  { label: '欧元(EUR)', value: 'EUR' },
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
