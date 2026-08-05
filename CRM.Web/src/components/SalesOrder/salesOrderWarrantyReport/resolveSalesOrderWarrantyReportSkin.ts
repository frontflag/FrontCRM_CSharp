import type { Component } from 'vue'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import SalesOrderWarrantyReportSkinSemicore from './skins/SalesOrderWarrantyReportSkinSemicore.vue'
import SalesOrderWarrantyReportSkinIdesemi from './skins/SalesOrderWarrantyReportSkinIdesemi.vue'
import SalesOrderWarrantyReportSkinEcoinf from './skins/SalesOrderWarrantyReportSkinEcoinf.vue'

/** 与 Packing 相同租户映射 */
const SKINS: Record<string, Component> = {
  semicore: SalesOrderWarrantyReportSkinIdesemi,
  idesemi: SalesOrderWarrantyReportSkinEcoinf,
  ecoinf: SalesOrderWarrantyReportSkinSemicore
}

export function resolveSalesOrderWarrantyReportSkin(tenantId: string = LOGIN_TENANT_ID): Component {
  const key = (tenantId || 'semicore').trim().toLowerCase()
  return SKINS[key] ?? SalesOrderWarrantyReportSkinSemicore
}
