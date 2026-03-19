<template>
  <div class="system-page">
    <el-card>
      <div class="toolbar">
        <div class="title">权限管理</div>
        <el-button type="primary" @click="router.push({ name: 'PermissionCreate' })">新增权限</el-button>
      </div>

      <el-table v-loading="loading" :data="permissions" border style="width: 100%">
        <el-table-column prop="permissionCode" label="权限编码" min-width="180" show-overflow-tooltip />
        <el-table-column prop="permissionName" label="权限名称" min-width="200" show-overflow-tooltip />
        <el-table-column prop="permissionType" label="类型" width="110" />
        <el-table-column prop="resource" label="资源" min-width="200" show-overflow-tooltip />
        <el-table-column prop="action" label="动作" min-width="140" show-overflow-tooltip />
        <el-table-column prop="status" label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'" size="small">
              {{ row.status === 1 ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="240" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="goEdit(row.id)">编辑</el-button>
            <el-button link type="danger" @click="handleDelete(row.id)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { rbacAdminApi, type RbacPermission } from '@/api/rbacAdmin'

const router = useRouter()

const loading = ref(false)
const permissions = ref<RbacPermission[]>([])

const load = async () => {
  loading.value = true
  try {
    permissions.value = await rbacAdminApi.getPermissions()
  } catch (e: any) {
    ElMessage.error(e?.message || '加载权限列表失败')
  } finally {
    loading.value = false
  }
}

const goEdit = (id: string) => {
  router.push({ name: 'PermissionEdit', params: { id } })
}

const handleDelete = async (id: string) => {
  try {
    await ElMessageBox.confirm('确定删除该权限吗？', '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消'
    })
    await rbacAdminApi.deletePermission(id)
    ElMessage.success('删除成功')
    await load()
  } catch {
    // cancel
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
</style>

