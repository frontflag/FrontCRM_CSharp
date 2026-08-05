import type { Component } from 'vue'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import PackingReportSkinSemicore from './skins/PackingReportSkinSemicore.vue'
import PackingReportSkinIdesemi from './skins/PackingReportSkinIdesemi.vue'
import PackingReportSkinEcoinf from './skins/PackingReportSkinEcoinf.vue'

/**
 * 租户 ↔ 组件（文件名保留原命名，映射可对调）：
 * - semicore ↔ ecoinf：半芯用 Ide 深紫琥珀，Eco 用原橙表 Semicore
 * - idesemi：工业极简 Eco 组件
 */
const SKINS: Record<string, Component> = {
  semicore: PackingReportSkinIdesemi,
  idesemi: PackingReportSkinEcoinf,
  ecoinf: PackingReportSkinSemicore
}

/** 按构建租户选择 Packing List 打印皮肤；未知租户回退 Semicore */
export function resolvePackingReportSkin(tenantId: string = LOGIN_TENANT_ID): Component {
  const key = (tenantId || 'semicore').trim().toLowerCase()
  return SKINS[key] ?? PackingReportSkinSemicore
}
