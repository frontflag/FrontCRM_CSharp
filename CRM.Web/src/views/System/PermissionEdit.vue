<template>
  <div class="system-page">
    <el-card>
      <div class="toolbar">
        <div class="title">{{ isEdit ? t('systemPermission.editTitle') : t('systemPermission.createTitle') }}</div>
      </div>

      <el-form :model="formData" label-width="120px" :disabled="loading">
        <el-form-item :label="t('systemPermission.columns.permissionCode')">
          <el-input v-model="formData.permissionCode" :disabled="isEdit" />
        </el-form-item>
        <el-form-item :label="t('systemPermission.columns.permissionName')">
          <el-input v-model="formData.permissionName" />
        </el-form-item>
        <el-form-item :label="t('systemPermission.columns.permissionType')">
          <el-input v-model="formData.permissionType" :placeholder="t('systemPermission.typePlaceholder')" />
        </el-form-item>
        <el-form-item :label="t('systemPermission.columns.resource')">
          <el-input v-model="formData.resource" />
        </el-form-item>
        <el-form-item :label="t('systemPermission.columns.action')">
          <el-input v-model="formData.action" />
        </el-form-item>
        <el-form-item :label="t('systemUser.colStatus')">
          <el-select v-model="formData.status" style="width: 160px">
            <el-option :value="1" :label="t('systemUser.statusEnabled')" />
            <el-option :value="0" :label="t('systemUser.statusDisabled')" />
          </el-select>
        </el-form-item>

        <div class="footer-bar">
          <el-button @click="router.push({ name: 'PermissionList' })">{{ t('rfqDetail.back') }}</el-button>
          <el-button type="primary" :loading="saving" @click="handleSubmit">
            {{ isEdit ? t('common.save') : t('systemPermission.create') }}
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
import { rbacAdminApi } from '@/api/rbacAdmin'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const permissionId = route.params.id as string | undefined
const isEdit = !!permissionId

const loading = ref(false)
const saving = ref(false)

const formData = ref({
  permissionCode: '',
  permissionName: '',
  permissionType: 'api',
  resource: '',
  action: '',
  status: 1
})

const load = async () => {
  loading.value = true
  try {
    if (isEdit && permissionId) {
      const list = await rbacAdminApi.getPermissions()
      const p = list.find(x => x.id === permissionId)
      if (!p) throw new Error(t('systemPermission.notFound'))

      formData.value.permissionCode = p.permissionCode
      formData.value.permissionName = p.permissionName
      formData.value.permissionType = p.permissionType
      formData.value.resource = p.resource || ''
      formData.value.action = p.action || ''
      formData.value.status = p.status ?? 1
    }
  } catch (e: any) {
    ElMessage.error(e?.message || t('systemPermission.loadDetailFailed'))
  } finally {
    loading.value = false
  }
}

const handleSubmit = async () => {
  if (saving.value) return

  if (!formData.value.permissionCode.trim() && !isEdit) {
    ElMessage.warning(t('systemPermission.fillCode'))
    return
  }
  if (!formData.value.permissionName.trim()) {
    ElMessage.warning(t('systemPermission.fillName'))
    return
  }

  saving.value = true
  try {
    if (isEdit && permissionId) {
      await rbacAdminApi.updatePermission(permissionId, {
        permissionName: formData.value.permissionName,
        permissionType: formData.value.permissionType,
        resource: formData.value.resource || undefined,
        action: formData.value.action || undefined,
        status: formData.value.status
      })
      ElMessage.success(t('common.saveSuccess'))
    } else {
      await rbacAdminApi.createPermission({
        permissionCode: formData.value.permissionCode,
        permissionName: formData.value.permissionName,
        permissionType: formData.value.permissionType,
        resource: formData.value.resource || undefined,
        action: formData.value.action || undefined,
        status: formData.value.status
      })
      ElMessage.success(t('common.createSuccess'))
    }

    router.push({ name: 'PermissionList' })
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

