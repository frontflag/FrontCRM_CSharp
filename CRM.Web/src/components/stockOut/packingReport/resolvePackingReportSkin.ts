import type { Component } from 'vue'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import PackingReportSkinSemicore from './skins/PackingReportSkinSemicore.vue'
import PackingReportSkinIdesemi from './skins/PackingReportSkinIdesemi.vue'
import PackingReportSkinEcoinf from './skins/PackingReportSkinEcoinf.vue'

const SKINS: Record<string, Component> = {
  semicore: PackingReportSkinSemicore,
  idesemi: PackingReportSkinIdesemi,
  ecoinf: PackingReportSkinEcoinf
}

/** 按构建租户选择 Packing List 打印皮肤；未知租户回退 Semicore */
export function resolvePackingReportSkin(tenantId: string = LOGIN_TENANT_ID): Component {
  const key = (tenantId || 'semicore').trim().toLowerCase()
  return SKINS[key] ?? PackingReportSkinSemicore
}
