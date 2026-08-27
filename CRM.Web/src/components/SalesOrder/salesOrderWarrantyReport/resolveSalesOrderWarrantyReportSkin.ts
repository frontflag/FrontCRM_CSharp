import type { Component } from 'vue'
import type { ReportStyleVersion } from '@/api/reportParams'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import SalesOrderWarrantyReportSkinSemicore from './skins/SalesOrderWarrantyReportSkinSemicore.vue'
import SalesOrderWarrantyReportSkinIdesemi from './skins/SalesOrderWarrantyReportSkinIdesemi.vue'
import SalesOrderWarrantyReportSkinEcoinf from './skins/SalesOrderWarrantyReportSkinEcoinf.vue'
import SalesOrderWarrantyReportV2SkinIdesemi from './skins/SalesOrderWarrantyReportV2SkinIdesemi.vue'

/** 与 Packing 相同租户映射 */
const SKINS: Record<string, Component> = {
  semicore: SalesOrderWarrantyReportSkinIdesemi,
  idesemi: SalesOrderWarrantyReportSkinEcoinf,
  ecoinf: SalesOrderWarrantyReportSkinSemicore
}

const V2_SKINS: Record<string, Component> = {
  semicore: SalesOrderWarrantyReportV2SkinIdesemi
}

export const SALES_ORDER_WARRANTY_REPORT_V2_TENANT_ID = 'semicore'

function normalizeTenant(tenantId: string): string {
  return (tenantId || 'semicore').trim().toLowerCase()
}

export function usesSalesOrderWarrantyReportV2(
  tenantId: string = LOGIN_TENANT_ID,
  styleVersion: ReportStyleVersion = 'V1'
): boolean {
  const key = normalizeTenant(tenantId)
  return styleVersion === 'V2' && key in V2_SKINS
}

export function resolveSalesOrderWarrantyReportSkin(
  tenantId: string = LOGIN_TENANT_ID,
  styleVersion: ReportStyleVersion = 'V1'
): Component {
  const key = normalizeTenant(tenantId)
  if (styleVersion === 'V2') {
    const v2 = V2_SKINS[key]
    if (v2) return v2
  }
  return SKINS[key] ?? SalesOrderWarrantyReportSkinSemicore
}
