/** 与后端 SysRelationMapTypeCode 一致：sys_relation_map.type */

/** 销售助理 → 销售员 */
export const SALES_ASSISTANT_TO_SALESPERSON = 100
/** 采购助理 → 采购员 */
export const PURCHASE_ASSISTANT_TO_PURCHASER = 101

/** 采购员 → 销售员（对哪些销售的需求报价） */
export const PURCHASER_QUOTES_SALESPERSON_RFQ = 200

export const PERSONNEL_RANGE_MIN = 100
export const PERSONNEL_RANGE_MAX = 199
export const BUSINESS_RANGE_MIN = 200
export const BUSINESS_RANGE_MAX = 299

export function isPersonnelRelationType(type: number): boolean {
  return type >= PERSONNEL_RANGE_MIN && type <= PERSONNEL_RANGE_MAX
}

export function isBusinessRelationType(type: number): boolean {
  return type >= BUSINESS_RANGE_MIN && type <= BUSINESS_RANGE_MAX
}
