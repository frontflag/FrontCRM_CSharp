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
                    <span class="role-perm-picker__code">{{ p.permissionCode }}</span>
                    <span class="role-perm-picker__name">{{ p.permissionName }}</span>
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

const formData = ref({
  roleCode: '',
  roleName: '',
  description: '',
  status: 1,
  permissionIds: [] as string[]
})

function permissionGroupKey(p: RbacPermission): string {
  const resource = (p.resource ?? '').trim()
  if (resource) return resource
  const code = p.permissionCode ?? ''
  const dot = code.indexOf('.')
  return dot > 0 ? code.slice(0, dot) : code || 'other'
}

const permissionGroups = computed(() => {
  const q = permFilter.value.trim().toLowerCase()
  const filtered = permissions.value.filter((p) => {
    if (p.status !== 1) return false
    if (!q) return true
    return (
      p.permissionCode.toLowerCase().includes(q) ||
      p.permissionName.toLowerCase().includes(q) ||
      (p.resource ?? '').toLowerCase().includes(q)
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
  align-items: baseline;
  gap: 8px;
  line-height: 1.45;
  white-space: normal;
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
</style>

