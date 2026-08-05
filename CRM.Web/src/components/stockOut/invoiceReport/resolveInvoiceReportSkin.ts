import type { Component } from 'vue'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import InvoiceReportSkinSemicore from './skins/InvoiceReportSkinSemicore.vue'
import InvoiceReportSkinIdesemi from './skins/InvoiceReportSkinIdesemi.vue'
import InvoiceReportSkinEcoinf from './skins/InvoiceReportSkinEcoinf.vue'

/**
 * 与 Packing List 相同租户映射（组件文件名保留原命名）：
 * - semicore → Idesemi 深色顶栏
 * - idesemi → Ecoinf 工业极简
 * - ecoinf → Semicore 绿表
 */
const SKINS: Record<string, Component> = {
  semicore: InvoiceReportSkinIdesemi,
  idesemi: InvoiceReportSkinEcoinf,
  ecoinf: InvoiceReportSkinSemicore
}

/** 按构建租户选择 Invoice 打印皮肤；未知租户回退橙/绿表组件 */
export function resolveInvoiceReportSkin(tenantId: string = LOGIN_TENANT_ID): Component {
  const key = (tenantId || 'semicore').trim().toLowerCase()
  return SKINS[key] ?? InvoiceReportSkinSemicore
}
