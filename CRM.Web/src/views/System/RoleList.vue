<template>
  <div class="system-page">
    <el-card>
      <div class="toolbar">
        <div class="title">角色管理</div>
        <el-button type="primary" @click="router.push({ name: 'RoleCreate' })">新增角色</el-button>
      </div>

      <el-table v-loading="loading" :data="roles" border style="width: 100%">
        <el-table-column prop="roleCode" label="角色编码" min-width="160" show-overflow-tooltip />
        <el-table-column prop="roleName" label="角色名称" min-width="180" show-overflow-tooltip />
        <el-table-column prop="description" label="描述" min-width="240" show-overflow-tooltip />
        <el-table-column prop="status" label="状态" width="90">
          <template #default="{ row }">
            <el-tag effect="dark" :type="row.status === 1 ? 'success' : 'info'" size="small">
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
import { rbacAdminApi, type RbacRole } from '@/api/rbacAdmin'

const router = useRouter()

const loading = ref(false)
const roles = ref<RbacRole[]>([])

const load = async () => {
  loading.value = true
  try {
    roles.value = await rbacAdminApi.getRoles()
  } catch (e: any) {
    ElMessage.error(e?.message || '加载角色列表失败')
  } finally {
    loading.value = false
  }
}

const goEdit = (id: string) => {
  router.push({ name: 'RoleEdit', params: { id } })
}

const handleDelete = async (id: string) => {
  try {
    await ElMessageBox.confirm('确定删除该角色吗？', '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消'
    })
    await rbacAdminApi.deleteRole(id)
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

