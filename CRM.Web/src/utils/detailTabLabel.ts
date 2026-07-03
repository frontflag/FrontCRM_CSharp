/** 详情页 Tab 标题计数：与销售/采购订单详情一致，仅 count > 0 时追加 ` (N)` */
export function formatDetailTabLabel(label: string, count: number): string {
  return count > 0 ? `${label} (${count})` : label
}
