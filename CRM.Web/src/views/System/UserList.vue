<template>
  <div class="crm-system-list-page">
    <el-card class="crm-system-list-card" shadow="never">
      <div class="crm-system-list-toolbar">
        <h1 class="crm-system-list-title">员工管理</h1>
        <el-button type="primary" @click="router.push({ name: 'UserCreate' })">新增员工</el-button>
      </div>

      <CrmDataTable v-loading="loading" :data="users">
        <el-table-column prop="userName" label="员工账号" min-width="160" show-overflow-tooltip />
        <el-table-column prop="realName" label="真实姓名" min-width="160" show-overflow-tooltip />
        <el-table-column prop="email" label="邮箱" min-width="200" show-overflow-tooltip />
        <el-table-column prop="mobile" label="手机" width="140" />
        <el-table-column prop="status" label="状态" width="90">
          <template #default="{ row }">
            <el-tag effect="dark" :type="row.status === 1 ? 'success' : 'info'" size="small">
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
      </CrmDataTable>
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
    ElMessage.error(e?.message || '加载员工列表失败')
  } finally {
    loading.value = false
  }
}

const goEdit = (id: string) => {
  router.push({ name: 'UserEdit', params: { id } })
}

const handleDelete = async (id: string) => {
  try {
    await ElMessageBox.confirm('确定删除该员工吗？', '删除确认', {
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

<style scoped lang="scss">
@import '@/assets/styles/system-list-page.scss';
</style>

