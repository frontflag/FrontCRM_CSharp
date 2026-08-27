import type { Component } from 'vue'
import type { ReportStyleVersion } from '@/api/reportParams'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import InvoiceReportSkinSemicore from './skins/InvoiceReportSkinSemicore.vue'
import InvoiceReportSkinIdesemi from './skins/InvoiceReportSkinIdesemi.vue'
import InvoiceReportSkinEcoinf from './skins/InvoiceReportSkinEcoinf.vue'
import InvoiceReportV2SkinIdesemi from './skins/InvoiceReportV2SkinIdesemi.vue'

/**
 * 与 Packing List 相同租户映射（组件文件名保留原命名）：
 * - semicore → Idesemi 深色顶栏
 * - idesemi → Ecoinf 工业极简
 * - ecoinf → Semicore 绿表
 *
 * V2 仅 semicore 换版式；idesemi / ecoinf 即使参数为 V2 仍用 V1。
 */
const SKINS: Record<string, Component> = {
  semicore: InvoiceReportSkinIdesemi,
  idesemi: InvoiceReportSkinEcoinf,
  ecoinf: InvoiceReportSkinSemicore
}

const V2_SKINS: Record<string, Component> = {
  semicore: InvoiceReportV2SkinIdesemi
}

export const INVOICE_REPORT_V2_TENANT_ID = 'semicore'

function normalizeTenant(tenantId: string): string {
  return (tenantId || 'semicore').trim().toLowerCase()
}

export function usesInvoiceReportV2(
  tenantId: string = LOGIN_TENANT_ID,
  styleVersion: ReportStyleVersion = 'V1'
): boolean {
  const key = normalizeTenant(tenantId)
  return styleVersion === 'V2' && key in V2_SKINS
}

/** 按构建租户与样式版本选择 Invoice 打印皮肤；未知租户回退 Semicore V1 */
export function resolveInvoiceReportSkin(
  tenantId: string = LOGIN_TENANT_ID,
  styleVersion: ReportStyleVersion = 'V1'
): Component {
  const key = normalizeTenant(tenantId)
  if (styleVersion === 'V2') {
    const v2 = V2_SKINS[key]
    if (v2) return v2
  }
  return SKINS[key] ?? InvoiceReportSkinSemicore
}
