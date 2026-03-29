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

/** 产品仅支持「采购轮询」（与 DB assign_method = 2 一致） */
export const ASSIGN_METHOD_OPTIONS: ReadonlyArray<{ label: string; value: number }> = [
  { label: '采购轮询', value: 2 }
]

/** 历史数据 1/3/4 仅用于详情只读展示 */
const ASSIGN_METHOD_LEGACY_LABELS: Readonly<Record<number, string>> = {
  1: '系统分配同一采购',
  3: '相同品牌分配同一采购',
  4: '指定采购员'
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

export function formatAssignMethodLabel(v?: number | null) {
  if (v == null) return '—'
  const hit = ASSIGN_METHOD_OPTIONS.find((o) => o.value === v)
  if (hit) return hit.label
  return ASSIGN_METHOD_LEGACY_LABELS[v] ?? '—'
}
