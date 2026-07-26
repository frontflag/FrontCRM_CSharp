<template>
  <div class="system-page">
    <el-card>
      <div class="toolbar">
        <div class="title">{{ isEdit ? t('systemRole.editTitle') : t('systemRole.createTitle') }}</div>
      </div>

      <el-form :model="formData" label-width="120px" :disabled="loading">
        <el-form-item :label="t('systemRole.columns.roleCode')">
          <el-input v-model="formData.roleCode" :disabled="isEdit" />
        </el-form-item>
        <el-form-item :label="t('systemRole.columns.roleName')">
          <el-input v-model="formData.roleName" />
        </el-form-item>
        <el-form-item :label="t('systemRole.columns.description')">
          <el-input v-model="formData.description" />
        </el-form-item>
        <el-form-item :label="t('systemUser.colStatus')">
          <el-select v-model="formData.status" style="width: 160px">
            <el-option :value="1" :label="t('systemUser.statusEnabled')" />
            <el-option :value="0" :label="t('systemUser.statusDisabled')" />
          </el-select>
        </el-form-item>

        <el-form-item :label="t('layout.menu.permissionManagement')">
          <div class="role-perm-picker">
            <div class="role-perm-picker__tabs">
              <el-radio-group v-model="permKindFilter" size="small">
                <el-radio-button value="all">{{ t('systemRole.permTabAll') }}</el-radio-button>
                <el-radio-button value="menu">{{ t('systemRole.permTabMenu') }}</el-radio-button>
                <el-radio-button value="sub">{{ t('systemRole.permTabSub') }}</el-radio-button>
              </el-radio-group>
            </div>
            <div class="role-perm-picker__toolbar">
              <el-input
                v-model="permFilter"
                clearable
                :placeholder="t('systemRole.permissionFilterPlaceholder')"
                class="role-perm-picker__filter"
              />
              <span class="role-perm-picker__count">
                {{ t('systemRole.permissionSelectedCount', { count: formData.permissionIds.length }) }}
              </span>
            </div>
            <div class="role-perm-picker__legend" :title="legendTitle">
              <span class="role-perm-picker__legend-label">{{ t('systemRole.permKindLegend') }}</span>
              <el-tag size="small" type="primary" effect="plain">{{ t('systemRole.permKindMenu') }}</el-tag>
              <span class="role-perm-picker__legend-hint">{{ t('systemRole.permKindMenuHint') }}</span>
              <el-tag size="small" type="warning" effect="plain">{{ t('systemRole.permKindSub') }}</el-tag>
              <span class="role-perm-picker__legend-hint">{{ t('systemRole.permKindSubHint') }}</span>
              <el-tag size="small" type="info" effect="plain">{{ t('systemRole.permKindFeature') }}</el-tag>
              <span class="role-perm-picker__legend-hint">{{ t('systemRole.permKindFeatureHint') }}</span>
            </div>
            <div v-if="permissionGroups.length === 0" class="role-perm-picker__empty">
              {{ t('systemRole.permissionFilterEmpty') }}
            </div>
            <div v-else class="role-perm-picker__list">
              <section v-for="group in permissionGroups" :key="group.key" class="role-perm-picker__section">
                <div class="role-perm-picker__group-title">{{ group.label }}</div>
                <el-checkbox-group v-model="formData.permissionIds" class="role-perm-picker__group">
                  <el-checkbox
                    v-for="p in group.items"
                    :key="p.id"
                    :value="p.id"
                    class="role-perm-picker__item"
                  >
                    <el-tag
                      size="small"
                      :type="permKindTagType(p.permissionCode)"
                      effect="plain"
                      class="role-perm-picker__kind"
                    >
                      {{ permKindLabel(p.permissionCode) }}
                    </el-tag>
                    <span class="role-perm-picker__code">{{ p.permissionCode }}</span>
                    <span class="role-perm-picker__name">{{ p.permissionName }}</span>
                    <span v-if="permMenuLabel(p.permissionCode)" class="role-perm-picker__menu-hint">
                      {{ t('systemRole.permMenuHintPrefix') }}{{ permMenuLabel(p.permissionCode) }}
                    </span>
                  </el-checkbox>
                </el-checkbox-group>
              </section>
            </div>
          </div>
        </el-form-item>

        <div class="footer-bar">
          <el-button @click="router.push({ name: 'RoleList' })">{{ t('rfqDetail.back') }}</el-button>
          <el-button type="primary" :loading="saving" @click="handleSubmit">
            {{ isEdit ? t('common.save') : t('systemRole.create') }}
          </el-button>
        </div>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { rbacAdminApi, type RbacPermission, type RbacRole } from '@/api/rbacAdmin'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const roleId = route.params.id as string | undefined
const isEdit = !!roleId

const loading = ref(false)
const saving = ref(false)

const permissions = ref<RbacPermission[]>([])
const permFilter = ref('')
const permKindFilter = ref<'all' | 'menu' | 'sub'>('all')

const formData = ref({
  roleCode: '',
  roleName: '',
  description: '',
  status: 1,
  permissionIds: [] as string[]
})

/** 侧栏菜单入口（read 码；对应 AppLayout / routes） */
const MENU_ENTRY_LABELS: Record<string, string> = {
  'system.org.users.read': '组织管理 / 员工管理',
  'system.org.departments.read': '组织管理 / 部门管理',
  'system.rbac.roles.read': '组织管理 / 角色管理',
  'system.rbac.permissions.read': '组织管理 / 权限管理',
  'system.org.user-config.read': '组织管理 / 用户配置',
  'system.params.company.read': '参数管理 / 公司信息',
  'system.params.dict.read': '参数管理 / 数据字典',
  'system.params.sales.read': '参数管理 / 销售参数',
  'system.params.purchase.read': '参数管理 / 采购参数',
  'system.params.finance.read': '参数管理 / 财务参数',
  'system.logs.login.read': '系统日志 / 登录日志',
  'system.logs.operation.read': '系统日志 / 操作日志',
  'biz.ai.admin': '参数管理 / AI 配置'
}

/** 参数页内部左侧子导航（非侧栏一级）；未来新增按 system.params.{area}.{feature}.read|write 命名即可自动识别 */
const PAGE_SUB_LABELS: Record<string, string> = {
  'system.params.sales.refresh-customer.read': '销售参数 → 刷新客户',
  'system.params.sales.refresh-customer.write': '销售参数 → 刷新客户（写）',
  'system.params.purchase.assignee-count.read': '采购参数 → 报价人数',
  'system.params.purchase.assignee-count.write': '采购参数 → 报价人数（写）',
  'system.params.purchase.quoter-pool.read': '采购参数 → 报价员池',
  'system.params.purchase.quoter-pool.write': '采购参数 → 报价员池（写）',
  'system.params.purchase.default-assign-method.read': '采购参数 → 默认分配方式',
  'system.params.purchase.default-assign-method.write': '采购参数 → 默认分配方式（写）',
  'system.params.purchase.demand-protection.read': '采购参数 → 需求保护',
  'system.params.purchase.demand-protection.write': '采购参数 → 需求保护（写）',
  'system.params.purchase.refresh-vendor.read': '采购参数 → 刷新供应商',
  'system.params.purchase.refresh-vendor.write': '采购参数 → 刷新供应商（写）',
  'system.params.finance.exchange-rates.read': '财务参数 → 汇率',
  'system.params.finance.exchange-rates.write': '财务参数 → 汇率（写）',
  'system.params.finance.purchase-cost-params.read': '财务参数 → 采购系数',
  'system.params.finance.purchase-cost-params.write': '财务参数 → 采购系数（写）',
  'system.params.finance.payment-banks.read': '财务参数 → 付款银行',
  'system.params.finance.payment-banks.write': '财务参数 → 付款银行（写）'
}

type PermKind = 'menu' | 'sub' | 'feature'

function isParamsModuleMenu(code: string): boolean {
  return /^system\.params\.(sales|purchase|finance)\.(read|write)$/i.test(code)
}

function isParamsPageSub(code: string): boolean {
  if (PAGE_SUB_LABELS[code]) return true
  // 约定：system.params.{area}.{feature…}.(read|write)，段数 ≥ 5
  const parts = code.split('.')
  if (parts.length < 5) return false
  if (parts[0] !== 'system' || parts[1] !== 'params') return false
  if (!['sales', 'purchase', 'finance'].includes(parts[2])) return false
  const action = parts[parts.length - 1]
  return action === 'read' || action === 'write'
}

function resolvePermKind(code: string): PermKind {
  if (MENU_ENTRY_LABELS[code] || isParamsModuleMenu(code)) return 'menu'
  if (isParamsPageSub(code)) return 'sub'
  return 'feature'
}

function permKindLabel(code: string): string {
  const kind = resolvePermKind(code)
  if (kind === 'menu') return t('systemRole.permKindMenu')
  if (kind === 'sub') return t('systemRole.permKindSub')
  return t('systemRole.permKindFeature')
}

function permKindTagType(code: string): 'primary' | 'warning' | 'info' {
  const kind = resolvePermKind(code)
  if (kind === 'menu') return 'primary'
  if (kind === 'sub') return 'warning'
  return 'info'
}

function permMenuLabel(code: string): string {
  if (MENU_ENTRY_LABELS[code] || PAGE_SUB_LABELS[code]) {
    return MENU_ENTRY_LABELS[code] || PAGE_SUB_LABELS[code]
  }
  if (isParamsPageSub(code)) {
    const parts = code.split('.')
    const area = parts[2]
    const feature = parts.slice(3, -1).join('.')
    const areaLabel =
      area === 'sales' ? '销售参数' : area === 'purchase' ? '采购参数' : area === 'finance' ? '财务参数' : area
    return `${areaLabel} → ${feature}`
  }
  return ''
}

const legendTitle = computed(
  () =>
    `${t('systemRole.permKindMenuHint')}；${t('systemRole.permKindSubHint')}；${t('systemRole.permKindFeatureHint')}`
)

function permissionGroupKey(p: RbacPermission): string {
  const code = p.permissionCode ?? ''
  if (code.startsWith('system.org.')) return 'system / 组织（侧栏菜单）'
  if (code.startsWith('system.rbac.')) return 'system / 角色权限（侧栏菜单）'
  if (isParamsPageSub(code)) {
    const area = code.split('.')[2]
    if (area === 'sales') return 'system / 销售参数 · 页内子项'
    if (area === 'purchase') return 'system / 采购参数 · 页内子项'
    if (area === 'finance') return 'system / 财务参数 · 页内子项'
    return 'system / 参数 · 页内子项'
  }
  if (code.startsWith('system.params.purchase.')) return 'system / 采购参数（侧栏菜单）'
  if (code.startsWith('system.params.sales.')) return 'system / 销售参数（侧栏菜单）'
  if (code.startsWith('system.params.finance.')) return 'system / 财务参数（侧栏菜单）'
  if (code.startsWith('system.params.company.')) return 'system / 公司信息（侧栏菜单）'
  if (code.startsWith('system.params.dict.')) return 'system / 数据字典（侧栏菜单）'
  if (code.startsWith('system.params.')) return 'system / 参数'
  if (code.startsWith('system.logs.')) return 'system / 日志（侧栏菜单）'
  if (code.startsWith('system.')) return 'system'
  if (code === 'biz.ai.admin') return 'biz / AI 配置（侧栏菜单）'
  if (code.startsWith('biz.ai.')) return 'biz / AI 功能（非侧栏菜单）'
  const resource = (p.resource ?? '').trim()
  if (resource) return resource
  const dot = code.indexOf('.')
  return dot > 0 ? code.slice(0, dot) : code || 'other'
}

const permissionGroups = computed(() => {
  const q = permFilter.value.trim().toLowerCase()
  const kindFilter = permKindFilter.value
  const filtered = permissions.value.filter((p) => {
    if (p.status !== 1) return false
    const kind = resolvePermKind(p.permissionCode)
    if (kindFilter !== 'all' && kind !== kindFilter) return false
    if (!q) return true
    const menuHint = permMenuLabel(p.permissionCode).toLowerCase()
    return (
      p.permissionCode.toLowerCase().includes(q) ||
      p.permissionName.toLowerCase().includes(q) ||
      (p.resource ?? '').toLowerCase().includes(q) ||
      menuHint.includes(q)
    )
  })
  const map = new Map<string, RbacPermission[]>()
  for (const p of filtered) {
    const key = permissionGroupKey(p)
    const bucket = map.get(key)
    if (bucket) bucket.push(p)
    else map.set(key, [p])
  }
  return [...map.entries()]
    .sort((a, b) => a[0].localeCompare(b[0], 'zh-CN'))
    .map(([key, items]) => ({
      key,
      label: key,
      items: [...items].sort((a, b) => a.permissionCode.localeCompare(b.permissionCode, 'zh-CN'))
    }))
})

const load = async () => {
  loading.value = true
  try {
    permissions.value = await rbacAdminApi.getPermissions()

    if (isEdit && roleId) {
      const roles: RbacRole[] = await rbacAdminApi.getRoles()
      const role = roles.find(r => r.id === roleId)
      if (!role) throw new Error(t('systemRole.notFound'))

      formData.value.roleCode = role.roleCode
      formData.value.roleName = role.roleName
      formData.value.description = role.description || ''
      formData.value.status = role.status ?? 1

      formData.value.permissionIds = await rbacAdminApi.getRolePermissionIds(roleId)
    }
  } catch (e: any) {
    ElMessage.error(e?.message || t('systemRole.loadDetailFailed'))
  } finally {
    loading.value = false
  }
}

const handleSubmit = async () => {
  if (saving.value) return

  if (!formData.value.roleCode.trim() && !isEdit) {
    ElMessage.warning(t('systemRole.fillRoleCode'))
    return
  }
  if (!formData.value.roleName.trim()) {
    ElMessage.warning(t('systemRole.fillRoleName'))
    return
  }

  saving.value = true
  try {
    if (isEdit && roleId) {
      await rbacAdminApi.updateRole(roleId, {
        roleName: formData.value.roleName,
        description: formData.value.description || undefined,
        status: formData.value.status
      })

      await rbacAdminApi.assignRolePermissions(roleId, formData.value.permissionIds)
      ElMessage.success(t('common.saveSuccess'))
    } else {
      const created = await rbacAdminApi.createRole({
        roleCode: formData.value.roleCode,
        roleName: formData.value.roleName,
        description: formData.value.description || undefined,
        status: formData.value.status
      })
      await rbacAdminApi.assignRolePermissions(created.id, formData.value.permissionIds)
      ElMessage.success(t('common.createSuccess'))
    }

    router.push({ name: 'RoleList' })
  } catch (e: any) {
    ElMessage.error(e?.message || t('common.saveFailed'))
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.system-page {
  padding: 20px;
}

.toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;
}

.title {
  font-size: 18px;
  font-weight: 600;
}

.footer-bar {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
  margin-top: 18px;
}

.role-perm-picker {
  width: 100%;
  border: 1px solid var(--el-border-color, #dcdfe6);
  border-radius: 8px;
  background: var(--el-fill-color-blank, #fff);
}

.role-perm-picker__tabs {
  display: flex;
  align-items: center;
  padding: 10px 12px 0;
  border-bottom: 1px solid var(--el-border-color-lighter, #ebeef5);
}

.role-perm-picker__toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 12px;
  border-bottom: 1px solid var(--el-border-color-lighter, #ebeef5);
}

.role-perm-picker__filter {
  flex: 1 1 auto;
  min-width: 0;
}

.role-perm-picker__legend {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px 10px;
  padding: 8px 12px;
  border-bottom: 1px solid var(--el-border-color-lighter, #ebeef5);
  background: var(--el-fill-color-lighter, #f5f7fa);
  font-size: 12px;
  color: var(--el-text-color-secondary, #909399);
}

.role-perm-picker__legend-label {
  font-weight: 600;
  color: var(--el-text-color-regular, #606266);
}

.role-perm-picker__legend-hint {
  margin-right: 8px;
}

.role-perm-picker__count {
  flex: 0 0 auto;
  font-size: 13px;
  color: var(--el-text-color-secondary, #909399);
  white-space: nowrap;
}

.role-perm-picker__list {
  max-height: 420px;
  overflow: auto;
  padding: 8px 12px 12px;
}

.role-perm-picker__empty {
  padding: 24px 12px;
  text-align: center;
  font-size: 13px;
  color: var(--el-text-color-secondary, #909399);
}

.role-perm-picker__section + .role-perm-picker__section {
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px dashed var(--el-border-color-lighter, #ebeef5);
}

.role-perm-picker__group-title {
  margin-bottom: 6px;
  font-size: 12px;
  font-weight: 600;
  color: var(--el-text-color-secondary, #909399);
  letter-spacing: 0.02em;
}

.role-perm-picker__group {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.role-perm-picker__item {
  display: flex;
  align-items: flex-start;
  margin-right: 0;
  height: auto;
  padding: 4px 0;
}

.role-perm-picker__item :deep(.el-checkbox__label) {
  display: inline-flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  line-height: 1.45;
  white-space: normal;
}

.role-perm-picker__kind {
  flex: 0 0 auto;
}

.role-perm-picker__code {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 12px;
  color: var(--el-text-color-primary, #303133);
}

.role-perm-picker__name {
  font-size: 13px;
  color: var(--el-text-color-regular, #606266);
}

.role-perm-picker__menu-hint {
  font-size: 12px;
  color: var(--el-color-primary);
}
</style>

