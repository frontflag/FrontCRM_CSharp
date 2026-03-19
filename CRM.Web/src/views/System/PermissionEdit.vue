<template>
  <div class="system-page">
    <el-card>
      <div class="toolbar">
        <div class="title">{{ isEdit ? '编辑权限' : '新增权限' }}</div>
      </div>

      <el-form :model="formData" label-width="120px" :disabled="loading">
        <el-form-item label="权限编码">
          <el-input v-model="formData.permissionCode" :disabled="isEdit" />
        </el-form-item>
        <el-form-item label="权限名称">
          <el-input v-model="formData.permissionName" />
        </el-form-item>
        <el-form-item label="权限类型">
          <el-input v-model="formData.permissionType" placeholder="api / menu / button" />
        </el-form-item>
        <el-form-item label="资源">
          <el-input v-model="formData.resource" />
        </el-form-item>
        <el-form-item label="动作">
          <el-input v-model="formData.action" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="formData.status" style="width: 160px">
            <el-option :value="1" label="启用" />
            <el-option :value="0" label="禁用" />
          </el-select>
        </el-form-item>

        <div class="footer-bar">
          <el-button @click="router.push({ name: 'PermissionList' })">返回</el-button>
          <el-button type="primary" :loading="saving" @click="handleSubmit">
            {{ isEdit ? '保存修改' : '创建权限' }}
          </el-button>
        </div>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { rbacAdminApi } from '@/api/rbacAdmin'

const route = useRoute()
const router = useRouter()

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
      if (!p) throw new Error('权限不存在')

      formData.value.permissionCode = p.permissionCode
      formData.value.permissionName = p.permissionName
      formData.value.permissionType = p.permissionType
      formData.value.resource = p.resource || ''
      formData.value.action = p.action || ''
      formData.value.status = p.status ?? 1
    }
  } catch (e: any) {
    ElMessage.error(e?.message || '加载权限失败')
  } finally {
    loading.value = false
  }
}

const handleSubmit = async () => {
  if (saving.value) return

  if (!formData.value.permissionCode.trim() && !isEdit) {
    ElMessage.warning('请填写权限编码')
    return
  }
  if (!formData.value.permissionName.trim()) {
    ElMessage.warning('请填写权限名称')
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
      ElMessage.success('保存成功')
    } else {
      await rbacAdminApi.createPermission({
        permissionCode: formData.value.permissionCode,
        permissionName: formData.value.permissionName,
        permissionType: formData.value.permissionType,
        resource: formData.value.resource || undefined,
        action: formData.value.action || undefined,
        status: formData.value.status
      })
      ElMessage.success('创建成功')
    }

    router.push({ name: 'PermissionList' })
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

