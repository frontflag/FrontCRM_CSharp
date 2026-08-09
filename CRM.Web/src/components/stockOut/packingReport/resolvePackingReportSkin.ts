import type { Component } from 'vue'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import PackingReportSkinSemicore from './skins/PackingReportSkinSemicore.vue'
import PackingReportSkinIdesemi from './skins/PackingReportSkinIdesemi.vue'
import PackingReportSkinEcoinf from './skins/PackingReportSkinEcoinf.vue'
import PackingReportLandscapeDocument from './PackingReportLandscapeDocument.vue'
import type { PackingReportOrientation, PackingReportTheme } from './types'

/**
 * 租户 ↔ 竖版组件（文件名保留原命名，映射可对调）：
 * - semicore ↔ ecoinf：半芯用 Ide 深紫琥珀，Eco 用原橙表 Semicore
 * - idesemi：工业极简 Eco 组件
 */
const PORTRAIT_SKINS: Record<string, Component> = {
  semicore: PackingReportSkinIdesemi,
  idesemi: PackingReportSkinEcoinf,
  ecoinf: PackingReportSkinSemicore
}

/** 横版视觉主题与竖版租户映射一致 */
const LANDSCAPE_THEMES: Record<string, PackingReportTheme> = {
  semicore: 'idesemi',
  idesemi: 'ecoinf',
  ecoinf: 'semicore'
}

function normalizeTenant(tenantId: string): string {
  return (tenantId || 'semicore').trim().toLowerCase()
}

/** 按构建租户选择 Packing List 竖版皮肤；未知租户回退 Semicore */
export function resolvePackingReportSkin(tenantId: string = LOGIN_TENANT_ID): Component {
  const key = normalizeTenant(tenantId)
  return PORTRAIT_SKINS[key] ?? PackingReportSkinSemicore
}

export function resolvePackingReportLandscapeTheme(
  tenantId: string = LOGIN_TENANT_ID
): PackingReportTheme {
  const key = normalizeTenant(tenantId)
  return LANDSCAPE_THEMES[key] ?? 'semicore'
}

export function resolvePackingReportView(
  orientation: PackingReportOrientation,
  tenantId: string = LOGIN_TENANT_ID
): { component: Component; landscapeTheme?: PackingReportTheme } {
  if (orientation === 'landscape') {
    return {
      component: PackingReportLandscapeDocument,
      landscapeTheme: resolvePackingReportLandscapeTheme(tenantId)
    }
  }
  return { component: resolvePackingReportSkin(tenantId) }
}
