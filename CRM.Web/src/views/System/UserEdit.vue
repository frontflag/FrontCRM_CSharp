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

        <el-form-item label="角色">
          <el-select
            v-model="formData.roleIds"
            multiple
            filterable
            collapse-tags
            style="width: 100%"
          >
            <el-option
              v-for="r in roles"
              :key="r.id"
              :label="`${r.roleCode} - ${r.roleName}`"
              :value="r.id"
            />
          </el-select>
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

const route = useRoute()
const router = useRouter()

const userId = route.params.id as string | undefined
const isEdit = !!userId

const loading = ref(false)
const saving = ref(false)

const roles = ref<RbacRole[]>([])
const departments = ref<RbacDepartment[]>([])

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
      formData.value.roleIds = dto.roleIds || []
      formData.value.departmentIds = dto.departmentIds || []
      formData.value.primaryDepartmentId = dto.primaryDepartmentId || ''
      normalizePrimaryDepartment()
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

  saving.value = true
  try {
    if (isEdit && userId) {
      await rbacAdminApi.updateUser(userId, {
        realName: formData.value.realName || undefined,
        email: formData.value.email || undefined,
        mobile: formData.value.mobile || undefined,
        status: formData.value.status,
        roleIds: formData.value.roleIds,
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
        roleIds: formData.value.roleIds,
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
</style>

