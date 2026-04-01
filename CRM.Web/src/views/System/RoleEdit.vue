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
          <el-select
            v-model="formData.permissionIds"
            multiple
            filterable
            collapse-tags
            style="width: 100%"
          >
            <el-option
              v-for="p in permissions"
              :key="p.id"
              :label="`${p.permissionCode} - ${p.permissionName}`"
              :value="p.id"
            />
          </el-select>
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
import { onMounted, ref } from 'vue'
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

const formData = ref({
  roleCode: '',
  roleName: '',
  description: '',
  status: 1,
  permissionIds: [] as string[]
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
</style>

