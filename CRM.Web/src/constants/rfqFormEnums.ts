/**
 * 需求单表单：需求类型 / 报价方式 / 分配方式
 * 与 DB smallint 取值一致；展示与下拉请统一引用本文件，避免与 RFQDetail、列表列不一致。
 */

export const RFQ_TYPE_OPTIONS: ReadonlyArray<{ label: string; value: number }> = [
  { label: '现货', value: 1 },
  { label: '排单', value: 2 },
  { label: '代理', value: 3 },
  { label: '自营', value: 4 },
  { label: '信息服务', value: 5 }
]

export const QUOTE_METHOD_OPTIONS: ReadonlyArray<{ label: string; value: number }> = [
  { label: '不接受任何消息', value: 1 },
  { label: '系统推送', value: 2 },
  { label: '邮件', value: 3 },
  { label: '短信', value: 4 }
]

/** 分配方式下拉（与 DB assign_method 一致）；指定采购固定在最后 */
export const ASSIGN_METHOD_OPTIONS: ReadonlyArray<{ label: string; value: number; tip: string }> = [
  { label: '采报优先', value: 5, tip: '按物料型号优先分配给曾采购或报价的采购员；无记录时按条目轮询' },
  { label: '条目轮询', value: 2, tip: '按需求条目轮询分配报价员' },
  { label: '品牌轮询', value: 3, tip: '按品牌轮询分配报价员' },
  { label: '指定采购', value: 4, tip: '本单全部明细指定同一位报价员，不受报价人数影响' }
]

/** 采购参数「默认分配方式」不含指定采购；新建/编辑页仅在勾选「允许指定采购」后才追加该项 */
export const DEFAULT_ASSIGN_METHOD_OPTIONS = ASSIGN_METHOD_OPTIONS.filter((o) => o.value !== 4)

/** @deprecated 请使用 ASSIGN_METHOD_OPTIONS[].tip */
export const ASSIGN_METHOD_ITEM_ROUND_ROBIN_TIP =
  ASSIGN_METHOD_OPTIONS.find((o) => o.value === 2)?.tip ?? '按需求条目轮询分配报价员'

/** 历史数据 1 仅用于详情只读展示 */
const ASSIGN_METHOD_LEGACY_LABELS: Readonly<Record<number, string>> = {
  1: '系统分配同一采购'
}

function labelFromOptions(options: ReadonlyArray<{ label: string; value: number }>, v?: number | null) {
  if (v == null) return '—'
  const hit = options.find((o) => o.value === v)
  return hit?.label ?? '—'
}

export function formatRfqTypeLabel(v?: number | null) {
  return labelFromOptions(RFQ_TYPE_OPTIONS, v)
}

export function formatQuoteMethodLabel(v?: number | null) {
  return labelFromOptions(QUOTE_METHOD_OPTIONS, v)
}

export function getAssignMethodTip(v?: number | null) {
  if (v == null) return ''
  const hit = ASSIGN_METHOD_OPTIONS.find((o) => o.value === v)
  return hit?.tip ?? ''
}

export function formatAssignMethodLabel(v?: number | null) {
  if (v == null) return '—'
  const hit = ASSIGN_METHOD_OPTIONS.find((o) => o.value === v)
  if (hit) return hit.label
  return ASSIGN_METHOD_LEGACY_LABELS[v] ?? '—'
}
