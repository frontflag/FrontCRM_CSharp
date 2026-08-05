import type { Component } from 'vue'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import PurchaseOrderReportSkinSemicore from './skins/PurchaseOrderReportSkinSemicore.vue'
import PurchaseOrderReportSkinIdesemi from './skins/PurchaseOrderReportSkinIdesemi.vue'
import PurchaseOrderReportSkinEcoinf from './skins/PurchaseOrderReportSkinEcoinf.vue'

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

export function resolvePurchaseOrderReportSkin(tenantId: string = LOGIN_TENANT_ID): Component {
  const key = (tenantId || 'semicore').trim().toLowerCase()
  return SKINS[key] ?? PurchaseOrderReportSkinSemicore
}
