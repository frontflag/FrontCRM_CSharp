import type { Component } from 'vue'
import type { ReportStyleVersion } from '@/api/reportParams'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import PackingReportSkinSemicore from './skins/PackingReportSkinSemicore.vue'
import PackingReportSkinIdesemi from './skins/PackingReportSkinIdesemi.vue'
import PackingReportSkinEcoinf from './skins/PackingReportSkinEcoinf.vue'
import PackingReportV2SkinIdesemi from './skins/PackingReportV2SkinIdesemi.vue'
import PackingReportV2LandscapeSkinIdesemi from './skins/PackingReportV2LandscapeSkinIdesemi.vue'
import PackingReportLandscapeDocument from './PackingReportLandscapeDocument.vue'
import type { PackingReportOrientation, PackingReportTheme } from './types'

/**
 * 租户 ↔ 竖版组件（文件名保留原命名，映射可对调）：
 * - semicore ↔ ecoinf：半芯用 Ide 深紫琥珀，Eco 用原橙表 Semicore
 * - idesemi：工业极简 Eco 组件
 *
 * V2 仅 semicore 换版式（竖版 + 横版）；idesemi / ecoinf 即使参数为 V2 仍用 V1。
 */
const PORTRAIT_SKINS: Record<string, Component> = {
  semicore: PackingReportSkinIdesemi,
  idesemi: PackingReportSkinEcoinf,
  ecoinf: PackingReportSkinSemicore
}

const V2_PORTRAIT_SKINS: Record<string, Component> = {
  semicore: PackingReportV2SkinIdesemi
}

const V2_LANDSCAPE_SKINS: Record<string, Component> = {
  semicore: PackingReportV2LandscapeSkinIdesemi
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

export const PACKING_REPORT_V2_TENANT_ID = 'semicore'

/** V2 仅 semicore（竖版与横版） */
export function usesPackingReportV2(
  tenantId: string = LOGIN_TENANT_ID,
  styleVersion: ReportStyleVersion = 'V1',
  _orientation?: PackingReportOrientation
): boolean {
  const key = normalizeTenant(tenantId)
  return styleVersion === 'V2' && key in V2_PORTRAIT_SKINS
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
  tenantId: string = LOGIN_TENANT_ID,
  styleVersion: ReportStyleVersion = 'V1'
): { component: Component; landscapeTheme?: PackingReportTheme } {
  if (styleVersion === 'V2') {
    const key = normalizeTenant(tenantId)
    if (orientation === 'landscape') {
      const v2ls = V2_LANDSCAPE_SKINS[key]
      if (v2ls) return { component: v2ls }
    } else {
      const v2 = V2_PORTRAIT_SKINS[key]
      if (v2) return { component: v2 }
    }
  }
  if (orientation === 'landscape') {
    return {
      component: PackingReportLandscapeDocument,
      landscapeTheme: resolvePackingReportLandscapeTheme(tenantId)
    }
  }
  return { component: resolvePackingReportSkin(tenantId) }
}
