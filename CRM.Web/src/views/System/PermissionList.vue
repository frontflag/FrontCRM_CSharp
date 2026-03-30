<template>
  <div class="crm-system-list-page">
    <el-card class="crm-system-list-card" shadow="never">
      <div class="crm-system-list-toolbar">
        <h1 class="crm-system-list-title">权限管理</h1>
        <el-button type="primary" @click="router.push({ name: 'PermissionCreate' })">新增权限</el-button>
      </div>

      <CrmDataTable v-loading="loading" :data="permissions" @row-dblclick="onRowDblclick">
        <el-table-column prop="status" label="状态" width="90">
          <template #default="{ row }">
            <el-tag effect="dark" :type="row.status === 1 ? 'success' : 'info'" size="small">
              {{ row.status === 1 ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="permissionCode" label="权限编码" min-width="180" show-overflow-tooltip />
        <el-table-column prop="permissionName" label="权限名称" min-width="200" show-overflow-tooltip />
        <el-table-column prop="permissionType" label="类型" width="110" />
        <el-table-column prop="resource" label="资源" min-width="200" show-overflow-tooltip />
        <el-table-column prop="action" label="动作" min-width="140" show-overflow-tooltip />
        <el-table-column label="创建时间" width="160">
          <template #default="{ row }">{{ formatCreateTime(row.createTime || row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="创建人" width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ row.createUserName || row.createdBy || '-' }}</template>
        </el-table-column>
        <el-table-column
          label="操作"
          :width="opColWidth"
          :min-width="opColMinWidth"
          fixed="right"
          class-name="op-col"
          label-class-name="op-col"
        >
          <template #header>
            <div class="op-col-header">
              <span class="op-col-header-text">操作</span>
              <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
                {{ opColExpanded ? '>' : '<' }}
              </button>
            </div>
          </template>
          <template #default="{ row }">
            <div @click.stop @dblclick.stop>
              <div v-if="opColExpanded" class="action-btns">
                <el-button link type="primary" @click.stop="goEdit(row.id)">编辑</el-button>
                <el-button link type="danger" @click.stop="handleDelete(row.id)">删除</el-button>
              </div>

              <el-dropdown v-else trigger="click" placement="bottom-end">
                <button type="button" class="op-more-trigger">...</button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item @click.stop="goEdit(row.id)">
                      <span class="op-more-item op-more-item--primary">编辑</span>
                    </el-dropdown-item>
                    <el-dropdown-item @click.stop="handleDelete(row.id)">
                      <span class="op-more-item op-more-item--danger">删除</span>
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
          </template>
        </el-table-column>
      </CrmDataTable>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { rbacAdminApi, type RbacPermission } from '@/api/rbacAdmin'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()

const loading = ref(false)
const permissions = ref<RbacPermission[]>([])
const formatCreateTime = (v?: string) => formatDisplayDateTime(v)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 240
const OP_COL_EXPANDED_MIN_WIDTH = 240
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

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

const onRowDblclick = (row: RbacPermission) => goEdit(row.id)

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

<style scoped lang="scss">
@import '@/assets/styles/system-list-page.scss';
</style>

