import type { Component } from 'vue'
import type { ReportStyleVersion } from '@/api/reportParams'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import PurchaseOrderReportSkinSemicore from './skins/PurchaseOrderReportSkinSemicore.vue'
import PurchaseOrderReportSkinIdesemi from './skins/PurchaseOrderReportSkinIdesemi.vue'
import PurchaseOrderReportSkinEcoinf from './skins/PurchaseOrderReportSkinEcoinf.vue'
import PurchaseOrderReportV2SkinIdesemi from './skins/PurchaseOrderReportV2SkinIdesemi.vue'

/**
 * 与 Packing / Invoice / SO 相同租户映射（组件文件名保留原命名）：
 * - semicore → Idesemi 深色顶栏
 * - idesemi → Ecoinf 工业极简
 * - ecoinf → Semicore 绿表
 *
 * V2 仅 semicore 换版式；idesemi / ecoinf 即使全局参数为 V2 仍用上表 V1 皮肤。
 */
const SKINS: Record<string, Component> = {
  semicore: PurchaseOrderReportSkinIdesemi,
  idesemi: PurchaseOrderReportSkinEcoinf,
  ecoinf: PurchaseOrderReportSkinSemicore
}

/** 采购订单 V2 仅对这些租户生效 */
const V2_SKINS: Record<string, Component> = {
  semicore: PurchaseOrderReportV2SkinIdesemi
}

export const PURCHASE_ORDER_V2_TENANT_ID = 'semicore'

export function usesPurchaseOrderReportV2(
  tenantId: string = LOGIN_TENANT_ID,
  styleVersion: ReportStyleVersion = 'V1'
): boolean {
  const key = (tenantId || 'semicore').trim().toLowerCase()
  return styleVersion === 'V2' && key in V2_SKINS
}

export function resolvePurchaseOrderReportSkin(
  tenantId: string = LOGIN_TENANT_ID,
  styleVersion: ReportStyleVersion = 'V1'
): Component {
  const key = (tenantId || 'semicore').trim().toLowerCase()
  if (styleVersion === 'V2') {
    const v2 = V2_SKINS[key]
    if (v2) return v2
  }
  return SKINS[key] ?? PurchaseOrderReportSkinSemicore
}
