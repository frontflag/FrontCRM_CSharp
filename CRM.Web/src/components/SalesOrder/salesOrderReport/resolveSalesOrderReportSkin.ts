import type { Component } from 'vue'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import SalesOrderReportSkinSemicore from './skins/SalesOrderReportSkinSemicore.vue'
import SalesOrderReportSkinIdesemi from './skins/SalesOrderReportSkinIdesemi.vue'
import SalesOrderReportSkinEcoinf from './skins/SalesOrderReportSkinEcoinf.vue'

/**
 * 与 Packing / Invoice 相同租户映射（组件文件名保留原命名）：
 * - semicore → Idesemi 深色顶栏
 * - idesemi → Ecoinf 工业极简
 * - ecoinf → Semicore 绿表
 */
const SKINS: Record<string, Component> = {
  semicore: SalesOrderReportSkinIdesemi,
  idesemi: SalesOrderReportSkinEcoinf,
  ecoinf: SalesOrderReportSkinSemicore
}

export function resolveSalesOrderReportSkin(tenantId: string = LOGIN_TENANT_ID): Component {
  const key = (tenantId || 'semicore').trim().toLowerCase()
  return SKINS[key] ?? SalesOrderReportSkinSemicore
}
