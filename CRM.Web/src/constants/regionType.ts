/** 与后端 RegionTypeCode / 数据库一致：10=境内 20=境外（仓库、到货通知等共用） */
export const REGION_TYPE_DOMESTIC = 10
export const REGION_TYPE_OVERSEAS = 20

export function normalizeRegionType(v: unknown): number {
  const n = typeof v === 'number' && Number.isFinite(v) ? v : Number(v)
  return n === REGION_TYPE_OVERSEAS ? REGION_TYPE_OVERSEAS : REGION_TYPE_DOMESTIC
}
