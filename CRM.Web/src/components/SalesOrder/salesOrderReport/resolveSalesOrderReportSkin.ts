import type { Component } from 'vue'
import type { ReportStyleVersion } from '@/api/reportParams'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import SalesOrderReportSkinSemicore from './skins/SalesOrderReportSkinSemicore.vue'
import SalesOrderReportSkinIdesemi from './skins/SalesOrderReportSkinIdesemi.vue'
import SalesOrderReportSkinEcoinf from './skins/SalesOrderReportSkinEcoinf.vue'
import SalesOrderReportV2SkinIdesemi from './skins/SalesOrderReportV2SkinIdesemi.vue'

/**
 * 与 Packing / Invoice 相同租户映射（组件文件名保留原命名）：
 * - semicore → Idesemi 深色顶栏
 * - idesemi → Ecoinf 工业极简
 * - ecoinf → Semicore 绿表
 *
 * V2 仅 semicore 换版式；idesemi / ecoinf 即使参数为 V2 仍用 V1。
 */
const SKINS: Record<string, Component> = {
  semicore: SalesOrderReportSkinIdesemi,
  idesemi: SalesOrderReportSkinEcoinf,
  ecoinf: SalesOrderReportSkinSemicore
}

const V2_SKINS: Record<string, Component> = {
  semicore: SalesOrderReportV2SkinIdesemi
}

export const SALES_ORDER_REPORT_V2_TENANT_ID = 'semicore'

function normalizeTenant(tenantId: string): string {
  return (tenantId || 'semicore').trim().toLowerCase()
}

export function usesSalesOrderReportV2(
  tenantId: string = LOGIN_TENANT_ID,
  styleVersion: ReportStyleVersion = 'V1'
): boolean {
  const key = normalizeTenant(tenantId)
  return styleVersion === 'V2' && key in V2_SKINS
}

export function resolveSalesOrderReportSkin(
  tenantId: string = LOGIN_TENANT_ID,
  styleVersion: ReportStyleVersion = 'V1'
): Component {
  const key = normalizeTenant(tenantId)
  if (styleVersion === 'V2') {
    const v2 = V2_SKINS[key]
    if (v2) return v2
  }
  return SKINS[key] ?? SalesOrderReportSkinSemicore
}
