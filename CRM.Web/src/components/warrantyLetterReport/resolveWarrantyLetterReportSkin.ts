import type { Component } from 'vue'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import WarrantyLetterReportSkinSemicore from './skins/WarrantyLetterReportSkinSemicore.vue'
import WarrantyLetterReportSkinIdesemi from './skins/WarrantyLetterReportSkinIdesemi.vue'
import WarrantyLetterReportSkinEcoinf from './skins/WarrantyLetterReportSkinEcoinf.vue'

/** 与 Packing / 销售订单质保书相同：semicore→Idesemi，idesemi→Ecoinf，ecoinf→Semicore */
const SKINS: Record<string, Component> = {
  semicore: WarrantyLetterReportSkinIdesemi,
  idesemi: WarrantyLetterReportSkinEcoinf,
  ecoinf: WarrantyLetterReportSkinSemicore
}

export function resolveWarrantyLetterReportSkin(tenantId: string = LOGIN_TENANT_ID): Component {
  const key = (tenantId || 'semicore').trim().toLowerCase()
  return SKINS[key] ?? WarrantyLetterReportSkinSemicore
}
