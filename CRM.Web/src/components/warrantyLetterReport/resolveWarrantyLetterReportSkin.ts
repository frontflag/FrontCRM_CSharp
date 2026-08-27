import type { Component } from 'vue'
import type { ReportStyleVersion } from '@/api/reportParams'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import WarrantyLetterReportSkinSemicore from './skins/WarrantyLetterReportSkinSemicore.vue'
import WarrantyLetterReportSkinIdesemi from './skins/WarrantyLetterReportSkinIdesemi.vue'
import WarrantyLetterReportSkinEcoinf from './skins/WarrantyLetterReportSkinEcoinf.vue'
import WarrantyLetterReportV2SkinIdesemi from './skins/WarrantyLetterReportV2SkinIdesemi.vue'

/** 与 Packing / 销售订单质保书相同：semicore→Idesemi，idesemi→Ecoinf，ecoinf→Semicore */
const SKINS: Record<string, Component> = {
  semicore: WarrantyLetterReportSkinIdesemi,
  idesemi: WarrantyLetterReportSkinEcoinf,
  ecoinf: WarrantyLetterReportSkinSemicore
}

const V2_SKINS: Record<string, Component> = {
  semicore: WarrantyLetterReportV2SkinIdesemi
}

export const WARRANTY_LETTER_REPORT_V2_TENANT_ID = 'semicore'

function normalizeTenant(tenantId: string): string {
  return (tenantId || 'semicore').trim().toLowerCase()
}

export function usesWarrantyLetterReportV2(
  tenantId: string = LOGIN_TENANT_ID,
  styleVersion: ReportStyleVersion = 'V1'
): boolean {
  const key = normalizeTenant(tenantId)
  return styleVersion === 'V2' && key in V2_SKINS
}

export function resolveWarrantyLetterReportSkin(
  tenantId: string = LOGIN_TENANT_ID,
  styleVersion: ReportStyleVersion = 'V1'
): Component {
  const key = normalizeTenant(tenantId)
  if (styleVersion === 'V2') {
    const v2 = V2_SKINS[key]
    if (v2) return v2
  }
  return SKINS[key] ?? WarrantyLetterReportSkinSemicore
}
