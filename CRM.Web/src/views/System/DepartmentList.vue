<template>
  <div class="crm-system-list-page">
    <el-card class="crm-system-list-card" shadow="never">
      <div class="crm-system-list-toolbar">
        <h1 class="crm-system-list-title">部门管理</h1>
        <el-button type="primary" @click="router.push({ name: 'DepartmentCreate' })">新增部门</el-button>
      </div>

      <el-table v-loading="loading" :data="departments" style="width: 100%" row-key="id">
        <el-table-column prop="departmentName" label="部门名称" min-width="160" show-overflow-tooltip />
        <el-table-column label="上级部门" min-width="140" show-overflow-tooltip>
          <template #default="{ row }">
            {{ parentLabel(row.parentId) }}
          </template>
        </el-table-column>
        <el-table-column prop="level" label="层级" width="72" />
        <el-table-column label="销售数据范围" min-width="120">
          <template #default="{ row }">{{ scopeLabel(row.saleDataScope) }}</template>
        </el-table-column>
        <el-table-column label="采购数据范围" min-width="120">
          <template #default="{ row }">{{ scopeLabel(row.purchaseDataScope) }}</template>
        </el-table-column>
        <el-table-column label="业务身份" min-width="100">
          <template #default="{ row }">{{ identityLabel(row.identityType) }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="90">
          <template #default="{ row }">
            <el-tag effect="dark" :type="row.status === 1 ? 'success' : 'info'" size="small">
              {{ row.status === 1 ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="260" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="goDetail(row)">详情</el-button>
            <el-button link type="primary" @click="goEdit(row.id)">编辑</el-button>
            <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { rbacAdminApi, type RbacDepartment } from '@/api/rbacAdmin'

const router = useRouter()
const loading = ref(false)
const departments = ref<RbacDepartment[]>([])

const nameById = computed(() => {
  const m: Record<string, string> = {}
  for (const d of departments.value) {
    m[d.id] = d.departmentName
  }
  return m
})

const parentLabel = (parentId?: string | null) => {
  if (!parentId) return '—'
  return nameById.value[parentId] || parentId
}

const scopeLabel = (v: number) => {
  const map: Record<number, string> = {
    0: '全部',
    1: '自己',
    2: '本部门',
    3: '本部门及下级',
    4: '禁止'
  }
  return map[v] ?? String(v)
}

const identityLabel = (v: number) => {
  const map: Record<number, string> = {
    0: '无',
    1: '销售',
    2: '采购',
    3: '采购运营',
    4: '商务',
    5: '财务',
    6: '物流'
  }
  return map[v] ?? String(v)
}

const load = async () => {
  loading.value = true
  try {
    departments.value = await rbacAdminApi.getDepartments()
  } catch (e: any) {
    ElMessage.error(e?.message || '加载部门列表失败')
  } finally {
    loading.value = false
  }
}

const goEdit = (id: string) => {
  router.push({ name: 'DepartmentEdit', params: { id } })
}

const goDetail = (row: RbacDepartment) => {
  router.push({ name: 'DepartmentDetail', params: { id: row.id } })
}

const handleDelete = async (row: RbacDepartment) => {
  try {
    await ElMessageBox.confirm(`确定删除部门「${row.departmentName}」吗？`, '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消'
    })
    await rbacAdminApi.deleteDepartment(row.id)
    ElMessage.success('删除成功')
    await load()
  } catch {
    /* cancel or error */
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
@import '@/assets/styles/system-list-page.scss';
</style>
