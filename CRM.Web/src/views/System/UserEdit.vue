<template>
  <div class="system-page">
    <el-card>
      <div class="toolbar">
        <div class="title">{{ isEdit ? t('systemUser.edit') : t('systemUser.create') }}</div>
      </div>

      <el-form :model="formData" label-width="120px" :disabled="loading">
        <el-form-item :label="t('systemUser.colUserName')">
          <el-input v-model="formData.userName" :disabled="isEdit" />
        </el-form-item>
        <el-form-item v-if="!isEdit" :label="t('login.passwordLabel')">
          <el-input v-model="formData.password" type="password" show-password />
        </el-form-item>

        <el-form-item :label="t('systemUser.colRealName')">
          <el-input v-model="formData.realName" />
        </el-form-item>
        <el-form-item :label="t('systemUser.colEmail')">
          <el-input v-model="formData.email" />
        </el-form-item>
        <el-form-item :label="t('systemUser.colMobile')">
          <el-input v-model="formData.mobile" />
        </el-form-item>
        <el-form-item v-if="formData.status !== 2" :label="t('systemUser.colStatus')">
          <el-select v-model="formData.status" style="width: 160px">
            <el-option :value="1" :label="t('systemUser.statusEnabled')" />
            <el-option :value="0" :label="t('systemUser.statusDisabled')" />
          </el-select>
        </el-form-item>
        <el-form-item v-else :label="t('systemUser.colStatus')">
          <el-tag type="danger" effect="dark">{{ t('systemUser.statusFrozen') }}</el-tag>
          <div class="field-hint">{{ t('systemUser.editFrozenHint') }}</div>
        </el-form-item>

        <el-form-item label="部门角色">
          <el-select v-model="selectedDeptRoleId" filterable clearable placeholder="请选择总监 / 经理 / 员工" style="width: 100%">
            <el-option
              v-for="r in departmentRoles"
              :key="r.id"
              :label="`${r.roleCode} — ${r.roleName}`"
              :value="r.id"
            />
          </el-select>
          <div class="field-hint">每个账号在部门维度仅分配一种组织角色，编码固定为 DEPT_DIRECTOR / DEPT_MANAGER / DEPT_EMPLOYEE。系统最高权限由下方「系统管理员」单独授予（角色 SYS_ADMIN），不出现在本下拉里。</div>
        </el-form-item>

        <el-form-item v-if="canGrantSysAdmin" label="SuperAdmin">
          <el-checkbox v-model="grantSysAdmin" :disabled="!sysAdminRoleId">
            授予 SuperAdmin（SYS_ADMIN）
          </el-checkbox>
          <div v-if="!sysAdminRoleId" class="field-hint field-hint-warn">
            库中无 SYS_ADMIN 角色，无法勾选。
          </div>
          <div v-else class="field-hint">
            仅当前登录为 SuperAdmin 时可授予。改密请用 /debug/super 自助或数据库 SQL。
          </div>
        </el-form-item>

        <el-form-item v-if="canGrantBizManager" label="Manager">
          <el-checkbox v-model="grantBizManager" :disabled="!bizManagerRoleId">
            授予 Manager（SYS_BIZ_MANAGER）
          </el-checkbox>
          <div v-if="!bizManagerRoleId" class="field-hint field-hint-warn">
            库中无 SYS_BIZ_MANAGER 角色。请在生产库执行
            <code>seed_management_roles_system_permissions.sql</code>（或对应 EF 迁移）后刷新本页。
          </div>
          <div v-else class="field-hint">
            Admin / SuperAdmin 可新建 Manager；Manager 本人不能创建其他 Manager。
          </div>
        </el-form-item>

        <el-form-item v-if="canGrantSysManager" label="Admin">
          <el-checkbox v-model="grantSysManager" :disabled="!sysManagerRoleId">
            授予 Admin（SYS_MANAGER）
          </el-checkbox>
          <div v-if="!sysManagerRoleId" class="field-hint field-hint-warn">
            库中无 SYS_MANAGER 角色。请在生产库执行
            <code>seed_management_roles_system_permissions.sql</code>（或对应 EF 迁移）后刷新本页。
          </div>
          <div v-else class="field-hint">仅 SuperAdmin 可授予 Admin。</div>
        </el-form-item>

        <el-form-item label="业务扩展角色">
          <el-select
            v-model="selectedBusinessRoleIds"
            multiple
            filterable
            collapse-tags
            collapse-tags-tooltip
            placeholder="可选：如采购员 purchase_buyer"
            style="width: 100%"
          >
            <el-option
              v-for="r in businessExtensionRoleOptions"
              :key="r.id"
              :label="`${r.roleCode} — ${r.roleName}`"
              :value="r.id"
            />
          </el-select>
          <div class="field-hint">
            与「部门角色」可同时存在。需求询价轮询、部分采购菜单等依赖 <code>purchase_buyer</code>；仅
            DEPT_EMPLOYEE 不会自动包含该项，需在此勾选保存。
          </div>
        </el-form-item>

        <el-form-item label="部门">
          <el-select
            v-model="formData.departmentIds"
            multiple
            filterable
            collapse-tags
            style="width: 100%"
          >
            <el-option v-for="d in departments" :key="d.id" :label="d.departmentName" :value="d.id" />
          </el-select>
        </el-form-item>

        <el-form-item label="主部门">
          <el-select v-model="formData.primaryDepartmentId" filterable style="width: 100%">
            <el-option
              v-for="d in availablePrimaryDepartments"
              :key="d.id"
              :label="d.departmentName"
              :value="d.id"
            />
          </el-select>
        </el-form-item>

        <div class="footer-bar">
          <el-button @click="router.push({ name: 'UserList' })">{{ t('rfqDetail.back') }}</el-button>
          <el-button type="primary" :loading="saving" @click="handleSubmit">
            {{ isEdit ? t('common.save') : t('systemUser.create') }}
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
import { rbacAdminApi, type AdminUserDto, type RbacDepartment, type RbacRole } from '@/api/rbacAdmin'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const { t } = useI18n()

const canGrantSysAdmin = computed(() => authStore.user?.isSysAdmin === true)
const canGrantSysManager = computed(() => authStore.user?.isSysAdmin === true)
const canGrantBizManager = computed(
  () => authStore.user?.isSysAdmin === true || authStore.user?.isSysManager === true
)

const userId = route.params.id as string | undefined
const isEdit = !!userId

const loading = ref(false)
const saving = ref(false)

/** 与后端数据权限、seed_dept_org_roles.sql 一致；下拉里展示顺序：员工 → 经理 → 总监 */
const ORG_ROLE_CODES = ['DEPT_EMPLOYEE', 'DEPT_MANAGER', 'DEPT_DIRECTOR'] as const

/** 与 RbacController.AssignableUserRoleCodes 一致：可与部门角色并存，打开员工编辑时不得被误删 */
const PRESERVABLE_BUSINESS_ROLE_CODES = new Set<string>([
  'purchase_buyer',
  'biz_all',
  'sales_operator',
  'purchase_operator',
  'commerce_operator',
  'purchase_ops_operator',
  'logistics_operator',
  'finance_operator'
])

const roles = ref<RbacRole[]>([])
const departments = ref<RbacDepartment[]>([])
/** 非部门标准角色的 roleId（如业务扩展角色），保存时原样带回，避免误删；SYS_ADMIN 改由 grantSysAdmin 控制 */
const preservedNonOrgRoleIds = ref<string[]>([])
/** 是否授予 SYS_ADMIN（需当前登录用户也是系统管理员） */
const grantSysAdmin = ref(false)
const grantSysManager = ref(false)
const grantBizManager = ref(false)

const formData = ref({
  userName: '',
  password: '',
  realName: '',
  email: '',
  mobile: '',
  status: 1,
  roleIds: [] as string[],
  departmentIds: [] as string[],
  primaryDepartmentId: '' as string
})

const departmentRoles = computed(() => {
  const list = roles.value.filter(r => ORG_ROLE_CODES.includes(r.roleCode as (typeof ORG_ROLE_CODES)[number]))
  const rank = (code: string) => ORG_ROLE_CODES.indexOf(code as (typeof ORG_ROLE_CODES)[number])
  return [...list].sort((a, b) => rank(a.roleCode) - rank(b.roleCode))
})

/** 可与部门角色并存的业务角色（与 RbacController.AssignableUserRoleCodes 一致，不含 SYS_ADMIN） */
const businessExtensionRoleOptions = computed(() =>
  roles.value.filter(r => PRESERVABLE_BUSINESS_ROLE_CODES.has(r.roleCode))
)

const businessAssignableRoleIdSet = computed(
  () => new Set(businessExtensionRoleOptions.value.map(r => r.id))
)

/** 多选绑定到 preservedNonOrgRoleIds 中的业务角色段 */
const selectedBusinessRoleIds = computed({
  get() {
    return preservedNonOrgRoleIds.value.filter(id => businessAssignableRoleIdSet.value.has(id))
  },
  set(ids: string[]) {
    const rest = preservedNonOrgRoleIds.value.filter(id => !businessAssignableRoleIdSet.value.has(id))
    preservedNonOrgRoleIds.value = [...rest, ...ids]
  }
})

const sysAdminRoleId = computed(() => roles.value.find(r => r.roleCode === 'SYS_ADMIN')?.id ?? '')
const sysManagerRoleId = computed(() => roles.value.find(r => r.roleCode === 'SYS_MANAGER')?.id ?? '')
const bizManagerRoleId = computed(() => roles.value.find(r => r.roleCode === 'SYS_BIZ_MANAGER')?.id ?? '')

const MANAGEMENT_ROLE_CODES = new Set(['SYS_ADMIN', 'SYS_MANAGER', 'SYS_BIZ_MANAGER'])

const orgRoleIdSet = computed(() => new Set(departmentRoles.value.map(r => r.id)))

const selectedDeptRoleId = computed({
  get() {
    const hit = formData.value.roleIds.find(id => orgRoleIdSet.value.has(id))
    return hit ?? ''
  },
  set(v: string) {
    const rest = formData.value.roleIds.filter(id => !orgRoleIdSet.value.has(id))
    formData.value.roleIds = v ? [...rest, v] : rest
  }
})

function stripManagedRoleIds(ids: string[]): string[] {
  const strip = new Set<string>()
  if (canGrantSysAdmin.value && sysAdminRoleId.value) strip.add(sysAdminRoleId.value)
  if (canGrantSysManager.value && sysManagerRoleId.value) strip.add(sysManagerRoleId.value)
  if (canGrantBizManager.value && bizManagerRoleId.value) strip.add(bizManagerRoleId.value)
  return ids.filter(id => !strip.has(id))
}

function buildRoleIdsForSubmit(): string[] {
  const deptId = selectedDeptRoleId.value
  let merged = stripManagedRoleIds([...preservedNonOrgRoleIds.value])
  if (canGrantSysAdmin.value && grantSysAdmin.value && sysAdminRoleId.value) merged.push(sysAdminRoleId.value)
  if (canGrantSysManager.value && grantSysManager.value && sysManagerRoleId.value) merged.push(sysManagerRoleId.value)
  if (canGrantBizManager.value && grantBizManager.value && bizManagerRoleId.value) merged.push(bizManagerRoleId.value)
  if (!canGrantSysAdmin.value && !canGrantSysManager.value && !canGrantBizManager.value) {
    // Manager 等：仅保留非管理角色
    merged = preservedNonOrgRoleIds.value.filter(id => {
      const code = roles.value.find(r => r.id === id)?.roleCode
      return code != null && !MANAGEMENT_ROLE_CODES.has(code)
    })
  }
  if (deptId) merged.push(deptId)
  return [...new Set(merged)]
}

const availablePrimaryDepartments = computed(() => {
  if (!formData.value.departmentIds.length) return []
  return departments.value.filter(d => formData.value.departmentIds.includes(d.id))
})

const normalizePrimaryDepartment = () => {
  if (!formData.value.departmentIds.length) {
    formData.value.primaryDepartmentId = ''
    return
  }
  if (!formData.value.primaryDepartmentId || !formData.value.departmentIds.includes(formData.value.primaryDepartmentId)) {
    formData.value.primaryDepartmentId = formData.value.departmentIds[0]
  }
}

const load = async () => {
  loading.value = true
  try {
    roles.value = await rbacAdminApi.getRoles()
    departments.value = await rbacAdminApi.getDepartments()

    if (
      (canGrantSysManager.value && !sysManagerRoleId.value) ||
      (canGrantBizManager.value && !bizManagerRoleId.value)
    ) {
      ElMessage.warning(
        '库中缺少 SYS_MANAGER / SYS_BIZ_MANAGER 管理角色，请执行 seed_management_roles_system_permissions.sql 后刷新'
      )
    }

    if (isEdit && userId) {
      const dto: AdminUserDto = await rbacAdminApi.getUserById(userId)
      formData.value.userName = dto.userName
      formData.value.realName = dto.realName || ''
      formData.value.email = dto.email || ''
      formData.value.mobile = dto.mobile || ''
      formData.value.status = dto.status ?? 1
      const allIds = dto.roleIds || []
      const orgSet = orgRoleIdSet.value
      preservedNonOrgRoleIds.value = allIds.filter(id => !orgSet.has(id))
      formData.value.roleIds = allIds.filter(id => orgSet.has(id) || preservedNonOrgRoleIds.value.includes(id))

      // 管理角色由下方勾选框单独控制，不算「未识别扩展角色」
      const unknown = preservedNonOrgRoleIds.value.filter(id => {
        const r = roles.value.find(x => x.id === id)
        if (!r) return true
        const code = r.roleCode
        if (MANAGEMENT_ROLE_CODES.has(code)) return false
        return !PRESERVABLE_BUSINESS_ROLE_CODES.has(code)
      })
      if (unknown.length) {
        ElMessage.warning(
          '该用户含有未识别的扩展角色，保存时将被移除；如需采购员/财务职员等权限，请使用系统定义的业务角色（如 purchase_buyer）。'
        )
        preservedNonOrgRoleIds.value = preservedNonOrgRoleIds.value.filter(id => {
          const r = roles.value.find(x => x.id === id)
          const code = r?.roleCode
          return (
            code != null &&
            (MANAGEMENT_ROLE_CODES.has(code) || PRESERVABLE_BUSINESS_ROLE_CODES.has(code))
          )
        })
        formData.value.roleIds = [...preservedNonOrgRoleIds.value, ...allIds.filter(id => orgSet.has(id))]
      }

      const sId = sysAdminRoleId.value
      grantSysAdmin.value = !!(sId && (dto.roleIds || []).includes(sId))
      const mId = sysManagerRoleId.value
      grantSysManager.value = !!(mId && (dto.roleIds || []).includes(mId))
      const bId = bizManagerRoleId.value
      grantBizManager.value = !!(bId && (dto.roleIds || []).includes(bId))

      formData.value.departmentIds = dto.departmentIds || []
      formData.value.primaryDepartmentId = dto.primaryDepartmentId || ''
      normalizePrimaryDepartment()
    } else {
      preservedNonOrgRoleIds.value = []
      grantSysAdmin.value = false
      formData.value.roleIds = []
    }
  } catch (e: any) {
    ElMessage.error(e?.message || '加载数据失败')
  } finally {
    loading.value = false
  }
}

const handleSubmit = async () => {
  if (saving.value) return
  normalizePrimaryDepartment()

  if (!formData.value.userName.trim()) {
    ElMessage.warning('请填写员工账号')
    return
  }

  if (!isEdit && !formData.value.password.trim()) {
    ElMessage.warning('请填写密码')
    return
  }

  const roleIdsForApi = buildRoleIdsForSubmit()
  if (!selectedDeptRoleId.value) {
    ElMessage.warning('请选择部门角色：部门总监 / 部门经理 / 部门员工')
    return
  }

  saving.value = true
  try {
    if (isEdit && userId) {
      await rbacAdminApi.updateUser(userId, {
        realName: formData.value.realName || undefined,
        email: formData.value.email || undefined,
        mobile: formData.value.mobile || undefined,
        status: formData.value.status,
        roleIds: roleIdsForApi,
        departmentIds: formData.value.departmentIds,
        primaryDepartmentId: formData.value.primaryDepartmentId || undefined
      })
      ElMessage.success('保存成功')
    } else {
      await rbacAdminApi.createUser({
        userName: formData.value.userName,
        password: formData.value.password,
        realName: formData.value.realName || undefined,
        email: formData.value.email || undefined,
        mobile: formData.value.mobile || undefined,
        status: formData.value.status,
        roleIds: roleIdsForApi,
        departmentIds: formData.value.departmentIds,
        primaryDepartmentId: formData.value.primaryDepartmentId || undefined
      })
      ElMessage.success('创建成功')
    }

    router.push({ name: 'UserList' })
  } catch (e: any) {
    ElMessage.error(e?.message || '保存失败')
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

.field-hint {
  margin-top: 6px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  line-height: 1.45;
}
.field-hint-warn {
  color: var(--el-color-warning-dark-2, #b88230);
}
.field-hint code {
  font-size: 12px;
}
</style>

