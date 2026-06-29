/** 与 RFQCreate.vue `normalizeImportance`、RFQList 重要程度列一致：界面三星，兼容历史 1–10 存盘 */
export function rfqImportanceDisplayStars(v: unknown): number {
  const n = Number(v)
  if (!Number.isFinite(n) || n < 1) return 1
  if (n <= 3) return Math.round(n)
  if (n <= 5) return Math.min(3, Math.max(1, Math.round(n)))
  return Math.max(1, Math.min(3, Math.round((n * 3) / 10)))
}

/** 列表 / 详情 el-rate 统一配色 */
export const RFQ_IMPORTANCE_RATE_COLORS = ['#C99A45', '#C99A45', '#C99A45'] as const
export const RFQ_IMPORTANCE_RATE_VOID_COLOR = 'rgba(200,216,232,0.2)'
