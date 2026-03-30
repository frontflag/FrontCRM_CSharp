<template>
  <div class="system-page">
    <el-card>
      <div class="toolbar">
        <div class="title">{{ isEdit ? '编辑员工' : '新增员工' }}</div>
      </div>

      <el-form :model="formData" label-width="120px" :disabled="loading">
        <el-form-item label="员工账号">
          <el-input v-model="formData.userName" :disabled="isEdit" />
        </el-form-item>
        <el-form-item v-if="!isEdit" label="密码">
          <el-input v-model="formData.password" type="password" show-password />
        </el-form-item>

        <el-form-item label="真实姓名">
          <el-input v-model="formData.realName" />
        </el-form-item>
        <el-form-item label="邮箱">
          <el-input v-model="formData.email" />
        </el-form-item>
        <el-form-item label="手机">
          <el-input v-model="formData.mobile" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="formData.status" style="width: 160px">
            <el-option :value="1" label="启用" />
            <el-option :value="0" label="禁用" />
          </el-select>
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

        <el-form-item v-if="canGrantSysAdmin && sysAdminRoleId" label="系统管理员">
          <el-checkbox v-model="grantSysAdmin">授予系统管理员权限（SYS_ADMIN）</el-checkbox>
          <div class="field-hint">与部门角色可同时存在：仍须选择一种部门组织角色。仅当前登录账号为系统管理员时显示此项。</div>
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
          <el-button @click="router.push({ name: 'UserList' })">返回</el-button>
          <el-button type="primary" :loading="saving" @click="handleSubmit">
            {{ isEdit ? '保存修改' : '创建员工' }}
          </el-button>
        </div>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { rbacAdminApi, type AdminUserDto, type RbacDepartment, type RbacRole } from '@/api/rbacAdmin'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const canGrantSysAdmin = computed(() => authStore.user?.isSysAdmin === true)

const userId = route.params.id as string | undefined
const isEdit = !!userId

const loading = ref(false)
const saving = ref(false)

/** 与后端数据权限、seed_dept_org_roles.sql 一致 */
const ORG_ROLE_CODES = ['DEPT_DIRECTOR', 'DEPT_MANAGER', 'DEPT_EMPLOYEE'] as const

const roles = ref<RbacRole[]>([])
const departments = ref<RbacDepartment[]>([])
/** 非部门标准角色的 roleId（如业务扩展角色），保存时原样带回，避免误删；SYS_ADMIN 改由 grantSysAdmin 控制 */
const preservedNonOrgRoleIds = ref<string[]>([])
/** 是否授予 SYS_ADMIN（需当前登录用户也是系统管理员） */
const grantSysAdmin = ref(false)

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

const departmentRoles = computed(() =>
  roles.value.filter(r => ORG_ROLE_CODES.includes(r.roleCode as (typeof ORG_ROLE_CODES)[number]))
)

const sysAdminRoleId = computed(() => roles.value.find(r => r.roleCode === 'SYS_ADMIN')?.id ?? '')

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

function buildRoleIdsForSubmit(): string[] {
  const deptId = selectedDeptRoleId.value
  const sid = sysAdminRoleId.value
  let merged: string[]
  if (canGrantSysAdmin.value) {
    merged = [...preservedNonOrgRoleIds.value.filter(id => !sid || id !== sid)]
    if (grantSysAdmin.value && sid) merged.push(sid)
  } else {
    // 无「授予系统管理员」能力时保留库里已有的非部门角色（含 SYS_ADMIN），避免误删
    merged = [...preservedNonOrgRoleIds.value]
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

      const unknown = preservedNonOrgRoleIds.value.filter(id => {
        const r = roles.value.find(x => x.id === id)
        if (!r) return true
        return r.roleCode !== 'SYS_ADMIN'
      })
      if (unknown.length) {
        ElMessage.warning(
          '该用户含有非标准业务角色，保存时将被移除；请改用部门三种角色或联系管理员迁移数据。'
        )
        preservedNonOrgRoleIds.value = preservedNonOrgRoleIds.value.filter(id => {
          const r = roles.value.find(x => x.id === id)
          return r?.roleCode === 'SYS_ADMIN'
        })
        formData.value.roleIds = [...preservedNonOrgRoleIds.value, ...allIds.filter(id => orgSet.has(id))]
      }

      const sId = sysAdminRoleId.value
      grantSysAdmin.value = !!(sId && (dto.roleIds || []).includes(sId))

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
</style>

