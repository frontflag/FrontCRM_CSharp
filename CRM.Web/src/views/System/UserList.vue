<template>
  <div class="crm-system-list-page">
    <el-card class="crm-system-list-card" shadow="never">
      <div class="crm-system-list-toolbar">
        <h1 class="crm-system-list-title">员工管理</h1>
        <el-button type="primary" @click="router.push({ name: 'UserCreate' })">新增员工</el-button>
      </div>

      <CrmDataTable v-loading="loading" :data="users" @row-dblclick="onRowDblclick">
        <el-table-column prop="status" label="状态" width="90">
          <template #default="{ row }">
            <el-tag effect="dark" :type="row.status === 1 ? 'success' : 'info'" size="small">
              {{ row.status === 1 ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="userName" label="员工账号" min-width="160" show-overflow-tooltip />
        <el-table-column prop="realName" label="真实姓名" min-width="160" show-overflow-tooltip />
        <el-table-column prop="email" label="邮箱" min-width="200" show-overflow-tooltip />
        <el-table-column prop="mobile" label="手机" width="140" />
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
                <el-button
                  v-if="canImpersonate && impersonateVisibleForRow(row)"
                  link
                  type="warning"
                  :loading="impersonateUserId === row.id"
                  @click.stop="handleImpersonate(row)"
                >
                  模拟登录
                </el-button>
              </div>

              <el-dropdown v-else trigger="click" placement="bottom-end">
                <button type="button" class="op-more-trigger">...</button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item @click.stop>
                      <span class="op-more-item op-more-item--primary" @click.stop="goEdit(row.id)">编辑</span>
                    </el-dropdown-item>
                    <el-dropdown-item @click.stop>
                      <span class="op-more-item op-more-item--danger" @click.stop="handleDelete(row.id)">删除</span>
                    </el-dropdown-item>
                    <el-dropdown-item
                      v-if="canImpersonate && impersonateVisibleForRow(row)"
                      :disabled="impersonateUserId === row.id"
                      @click.stop
                    >
                      <span
                        class="op-more-item op-more-item--warning"
                        @click.stop="handleImpersonate(row)"
                      >
                        模拟登录
                      </span>
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
import { rbacAdminApi, type AdminUserDto } from '@/api/rbacAdmin'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const loading = ref(false)
const users = ref<AdminUserDto[]>([])
const impersonateUserId = ref<string | null>(null)

/** 仅系统管理员（SYS_ADMIN）可见模拟登录 */
const canImpersonate = computed(() => authStore.user?.isSysAdmin === true)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = computed(() => (canImpersonate.value ? 320 : 240))
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH.value : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH.value : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

function impersonateVisibleForRow(row: AdminUserDto) {
  if (row.status !== 1) return false
  const selfId = authStore.user?.id
  if (selfId && row.id === selfId) return false
  return true
}

const formatCreateTime = (v?: string) => formatDisplayDateTime(v)

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

const onRowDblclick = (row: AdminUserDto) => goEdit(row.id)

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

async function handleImpersonate(row: AdminUserDto) {
  const label = row.realName || row.userName
  try {
    await ElMessageBox.confirm(
      `将以「${label}」（${row.userName}）身份登录，当前管理员会话将结束。是否继续？`,
      '模拟登录',
      { type: 'warning', confirmButtonText: '模拟登录', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  impersonateUserId.value = row.id
  try {
    await authStore.impersonate(row.id)
    ElMessage.success(`已切换为 ${row.userName}`)
    await router.replace({ name: 'Dashboard' })
  } catch (e: any) {
    ElMessage.error(e?.message || '模拟登录失败')
  } finally {
    impersonateUserId.value = null
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
@import '@/assets/styles/system-list-page.scss';
@import '@/assets/styles/variables.scss';

.op-col-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0;
  width: 100%;
}

.op-col-header-text {
  font-size: 12px;
  line-height: 1;
  white-space: nowrap;
}

.op-col-toggle-btn {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  flex: 0 0 auto;
}

.op-more-trigger {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  opacity: 0;
  transition: opacity 0.15s;
}

:deep(.el-table__body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__body-wrapper .el-table__body tr.hover-row .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr.hover-row .op-more-trigger) {
  opacity: 1;
}

.op-more-item {
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
}

.op-more-item--primary {
  color: $cyan-primary;
}

.op-more-item--warning {
  color: $color-amber;
}

.op-more-item--danger {
  color: $color-red-brown;
}
</style>

