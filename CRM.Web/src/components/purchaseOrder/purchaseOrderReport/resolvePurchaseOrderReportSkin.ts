import type { Component } from 'vue'
import type { ReportStyleVersion } from '@/api/reportParams'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import PurchaseOrderReportSkinSemicore from './skins/PurchaseOrderReportSkinSemicore.vue'
import PurchaseOrderReportSkinIdesemi from './skins/PurchaseOrderReportSkinIdesemi.vue'
import PurchaseOrderReportSkinEcoinf from './skins/PurchaseOrderReportSkinEcoinf.vue'
import PurchaseOrderReportV2SkinSemicore from './skins/PurchaseOrderReportV2SkinSemicore.vue'
import PurchaseOrderReportV2SkinIdesemi from './skins/PurchaseOrderReportV2SkinIdesemi.vue'
import PurchaseOrderReportV2SkinEcoinf from './skins/PurchaseOrderReportV2SkinEcoinf.vue'

/**
 * 与 Packing / Invoice / SO 相同租户映射（组件文件名保留原命名）：
 * - semicore → Idesemi 深色顶栏
 * - idesemi → Ecoinf 工业极简
 * - ecoinf → Semicore 绿表
 */
const SKINS: Record<string, Component> = {
  semicore: PurchaseOrderReportSkinIdesemi,
  idesemi: PurchaseOrderReportSkinEcoinf,
  ecoinf: PurchaseOrderReportSkinSemicore
}

const V2_SKINS: Record<string, Component> = {
  semicore: PurchaseOrderReportV2SkinIdesemi,
  idesemi: PurchaseOrderReportV2SkinEcoinf,
  ecoinf: PurchaseOrderReportV2SkinSemicore
}

export function resolvePurchaseOrderReportSkin(
  tenantId: string = LOGIN_TENANT_ID,
  styleVersion: ReportStyleVersion = 'V1'
): Component {
  const key = (tenantId || 'semicore').trim().toLowerCase()
  if (styleVersion === 'V2') {
    return V2_SKINS[key] ?? PurchaseOrderReportV2SkinSemicore
  }
  return SKINS[key] ?? PurchaseOrderReportSkinSemicore
}
