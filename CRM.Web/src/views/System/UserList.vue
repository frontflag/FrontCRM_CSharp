<template>
  <div class="system-page">
    <el-card>
      <div class="toolbar">
        <div class="title">用户管理</div>
        <el-button type="primary" @click="router.push({ name: 'UserCreate' })">新增用户</el-button>
      </div>

      <el-table v-loading="loading" :data="users" border style="width: 100%">
        <el-table-column prop="userName" label="用户名" min-width="160" show-overflow-tooltip />
        <el-table-column prop="realName" label="真实姓名" min-width="160" show-overflow-tooltip />
        <el-table-column prop="email" label="邮箱" min-width="200" show-overflow-tooltip />
        <el-table-column prop="mobile" label="手机" width="140" />
        <el-table-column prop="status" label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'" size="small">
              {{ row.status === 1 ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="角色" min-width="220" show-overflow-tooltip>
          <template #default="{ row }">
            <span>{{ row.roleCodes?.join(', ') || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="主部门" min-width="220" show-overflow-tooltip>
          <template #default="{ row }">
            <span>{{ row.primaryDepartmentName || '-' }}</span>
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
import { rbacAdminApi, type AdminUserDto } from '@/api/rbacAdmin'

const router = useRouter()

const loading = ref(false)
const users = ref<AdminUserDto[]>([])

const load = async () => {
  loading.value = true
  try {
    users.value = await rbacAdminApi.getUsers()
  } catch (e: any) {
    ElMessage.error(e?.message || '加载用户列表失败')
  } finally {
    loading.value = false
  }
}

const goEdit = (id: string) => {
  router.push({ name: 'UserEdit', params: { id } })
}

const handleDelete = async (id: string) => {
  try {
    await ElMessageBox.confirm('确定删除该用户吗？', '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消'
    })
    await rbacAdminApi.deleteUser(id)
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

