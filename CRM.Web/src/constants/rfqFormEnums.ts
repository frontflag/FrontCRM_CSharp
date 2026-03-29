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

export const ASSIGN_METHOD_OPTIONS: ReadonlyArray<{ label: string; value: number }> = [
  { label: '系统分配同一采购', value: 1 },
  { label: '系统分配多人采购', value: 2 },
  { label: '相同品牌分配同一采购', value: 3 },
  { label: '指定采购员', value: 4 }
]

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
  return labelFromOptions(ASSIGN_METHOD_OPTIONS, v)
}
