<template>
  <div class="department-detail-page">
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          返回
        </button>
        <div class="dept-title-group">
          <div class="dept-avatar-lg">{{ (department?.departmentName || '?')[0] }}</div>
          <div>
            <h1 class="page-title">{{ department?.departmentName || '部门详情' }}</h1>
            <div class="title-meta">
              <span class="dept-code">{{ department?.path || '—' }}</span>
              <span class="status-badge" :class="department?.status === 1 ? 'status--active' : 'status--inactive'">
                {{ department?.status === 1 ? '启用' : '禁用' }}
              </span>
              <span class="level-pill">层级 {{ department?.level ?? '—' }}</span>
            </div>
          </div>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" type="button" @click="handleEdit">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
          </svg>
          编辑
        </button>
        <button class="btn-danger" type="button" @click="handleDeleteClick">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="3 6 5 6 21 6" />
            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
          </svg>
          删除部门
        </button>
      </div>
    </div>

    <div v-loading="loading" element-loading-background="rgba(10,22,40,0.8)" class="detail-content">
      <template v-if="department">
        <div class="info-section">
          <div class="section-header">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">部门信息</span>
          </div>
          <div class="info-grid">
            <div class="info-item">
              <span class="info-label">部门名称</span>
              <span class="info-value">{{ department.departmentName }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">上级部门</span>
              <span class="info-value">{{ parentLabel }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">层级</span>
              <span class="info-value">{{ department.level }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">路径</span>
              <span class="info-value info-value--code">{{ department.path || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">销售数据范围</span>
              <span class="info-value">{{ scopeLabel(department.saleDataScope) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">采购数据范围</span>
              <span class="info-value">{{ scopeLabel(department.purchaseDataScope) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">业务身份</span>
              <span class="info-value">{{ identityLabel(department.identityType) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">状态</span>
              <span class="info-value">{{ department.status === 1 ? '启用' : '禁用' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">创建时间</span>
              <span class="info-value info-value--time">{{ formatDateTime(department.createTime) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">更新时间</span>
              <span class="info-value info-value--time">{{ formatDateTime(department.modifyTime ?? undefined) }}</span>
            </div>
          </div>
        </div>

        <div class="tabs-section">
          <div class="tabs-nav">
            <button type="button" class="tab-btn tab-btn--active">部门员工</button>
          </div>
          <div class="tabs-body">
            <CrmDataTable
              v-loading="usersLoading"
              :data="users"
              class="quantum-table"
              :header-cell-style="tableHeaderStyle"
              :cell-style="tableCellStyle"
              :row-style="tableRowStyle"
            >
              <el-table-column prop="userName" label="员工账号" min-width="120">
                <template #default="{ row }"><span class="cell-primary">{{ row.userName }}</span></template>
              </el-table-column>
              <el-table-column prop="realName" label="姓名" width="100">
                <template #default="{ row }"><span class="cell-secondary">{{ row.realName || '—' }}</span></template>
              </el-table-column>
              <el-table-column prop="email" label="邮箱" min-width="180" show-overflow-tooltip>
                <template #default="{ row }"><span class="cell-secondary">{{ row.email || '—' }}</span></template>
              </el-table-column>
              <el-table-column prop="mobile" label="手机" width="130">
                <template #default="{ row }"><span class="cell-code">{{ row.mobile || '—' }}</span></template>
              </el-table-column>
              <el-table-column label="状态" width="88">
                <template #default="{ row }">
                  <span class="cell-muted">{{ row.status === 1 ? '正常' : '停用' }}</span>
                </template>
              </el-table-column>
              <el-table-column label="角色" min-width="140" show-overflow-tooltip>
                <template #default="{ row }">
                  <span class="cell-secondary">{{ (row.roleCodes || []).join('、') || '—' }}</span>
                </template>
              </el-table-column>
              <el-table-column label="主部门" width="88" align="center">
                <template #default="{ row }">
                  <span v-if="row.primaryDepartmentId === departmentId" class="default-badge">主部门</span>
                  <span v-else class="cell-muted">—</span>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="100" fixed="right">
                <template #default="{ row }">
                  <button type="button" class="action-btn" @click="goEditUser(row.id)">编辑</button>
                </template>
              </el-table-column>
            </CrmDataTable>
            <div v-if="!usersLoading && users.length === 0" class="empty-hint">暂无关联员工</div>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { rbacAdminApi, type AdminUserDto, type RbacDepartment } from '@/api/rbacAdmin'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const route = useRoute()
const router = useRouter()
const departmentId = computed(() => route.params.id as string)

const loading = ref(false)
const usersLoading = ref(false)
const department = ref<RbacDepartment | null>(null)
const users = ref<AdminUserDto[]>([])
const allDepartments = ref<RbacDepartment[]>([])

const parentLabel = computed(() => {
  const d = department.value
  if (!d?.parentId) return '—'
  const p = allDepartments.value.find((x) => x.id === d.parentId)
  return p?.departmentName || d.parentId
})

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

const formatDateTime = (date: string | undefined) => (date ? formatDisplayDateTime(date) : '—')

const tableHeaderStyle = () => ({
  background: '#0A1628',
  color: 'rgba(200,216,232,0.55)',
  fontSize: '12px',
  fontWeight: '500',
  letterSpacing: '0.5px',
  borderBottom: '1px solid rgba(0,212,255,0.12)',
  padding: '10px 0'
})
const tableCellStyle = () => ({
  background: 'transparent',
  borderBottom: '1px solid rgba(255,255,255,0.05)',
  color: 'rgba(224,244,255,0.85)',
  fontSize: '13px'
})
const tableRowStyle = () => ({ background: 'transparent' })

const load = async () => {
  const id = departmentId.value
  if (!id) return
  loading.value = true
  usersLoading.value = true
  department.value = null
  users.value = []
  try {
    const [dept, deptList, memberList] = await Promise.all([
      rbacAdminApi.getDepartmentById(id),
      rbacAdminApi.getDepartments(),
      rbacAdminApi.getDepartmentUsers(id)
    ])
    department.value = dept
    allDepartments.value = deptList
    users.value = memberList
  } catch (e: any) {
    ElMessage.error(e?.message || '加载部门详情失败')
  } finally {
    loading.value = false
    usersLoading.value = false
  }
}

watch(departmentId, load, { immediate: true })

const goBack = () => router.push({ name: 'DepartmentList' })
const handleEdit = () => router.push({ name: 'DepartmentEdit', params: { id: departmentId.value } })
const goEditUser = (userId: string) => router.push({ name: 'UserEdit', params: { id: userId } })

const handleDeleteClick = async () => {
  const d = department.value
  if (!d) return
  try {
    await ElMessageBox.confirm(`确定删除部门「${d.departmentName}」吗？`, '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消'
    })
    await rbacAdminApi.deleteDepartment(d.id)
    ElMessage.success('删除成功')
    router.push({ name: 'DepartmentList' })
  } catch {
    /* cancel or error */
  }
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.department-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 16px;
  }

  .header-right {
    display: flex;
    gap: 10px;
  }
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.dept-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
}

.dept-avatar-lg {
  width: 48px;
  height: 48px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.3), rgba(0, 212, 255, 0.2));
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
  color: $cyan-primary;
  flex-shrink: 0;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0 0 6px 0;
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.dept-code {
  font-family: 'Space Mono', monospace;
  font-size: 11px;
  color: $text-muted;
  max-width: 360px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.status-badge {
  font-size: 10px;
  padding: 2px 7px;
  border-radius: 3px;

  &--active {
    background: rgba(70, 191, 145, 0.15);
    color: $color-mint-green;
    border: 1px solid rgba(70, 191, 145, 0.3);
  }
  &--inactive {
    background: rgba(107, 122, 141, 0.15);
    color: #8a9bb0;
    border: 1px solid rgba(107, 122, 141, 0.3);
  }
}

.level-pill {
  font-size: 10px;
  padding: 2px 7px;
  border-radius: 3px;
  background: rgba(50, 149, 201, 0.15);
  color: $color-steel-cyan;
  border: 1px solid rgba(50, 149, 201, 0.25);
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.25);
  }
}

.btn-danger {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: rgba(201, 87, 69, 0.15);
  border: 1px solid rgba(201, 87, 69, 0.4);
  border-radius: $border-radius-md;
  color: $color-red-brown;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(201, 87, 69, 0.25);
  }
}

.info-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: rgba(0, 0, 0, 0.1);
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.6);
  }
}

.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-primary;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
  padding: 0;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 16px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
  border-right: 1px solid rgba(255, 255, 255, 0.04);

  &:nth-child(3n) {
    border-right: none;
  }

  .info-label {
    font-size: 11px;
    color: $text-muted;
    letter-spacing: 0.5px;
    text-transform: uppercase;
  }

  .info-value {
    font-size: 13px;
    color: $text-secondary;

    &--code {
      font-family: 'Space Mono', monospace;
      font-size: 12px;
      color: $color-ice-blue;
    }
    &--time {
      font-size: 12px;
      color: $text-muted;
    }
  }
}

.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.tabs-nav {
  display: flex;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  padding: 0 16px;
  background: rgba(0, 0, 0, 0.1);
}

.tab-btn {
  padding: 12px 16px;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: default;
  margin-bottom: -1px;

  &--active {
    color: $cyan-primary;
    border-bottom-color: $cyan-primary;
  }
}

.tabs-body {
  padding: 20px;
}

.empty-hint {
  text-align: center;
  padding: 28px;
  color: $text-muted;
  font-size: 13px;
}

.quantum-table {
  width: 100%;
  background: transparent !important;

  :deep(.el-table__inner-wrapper) {
    background: transparent;
  }
  :deep(tr) {
    background: transparent !important;
    &:hover td {
      background: rgba(0, 212, 255, 0.04) !important;
    }
  }
  :deep(.el-table__fixed-right) {
    background: $layer-2 !important;
  }
}

.cell-primary {
  color: $text-primary;
  font-size: 13px;
}
.cell-secondary {
  color: $text-secondary;
  font-size: 13px;
}
.cell-muted {
  color: $text-muted;
  font-size: 12px;
}
.cell-code {
  font-family: 'Space Mono', monospace;
  font-size: 12px;
  color: $color-ice-blue;
}

.default-badge {
  display: inline-block;
  font-size: 10px;
  padding: 1px 6px;
  background: rgba(70, 191, 145, 0.15);
  color: $color-mint-green;
  border: 1px solid rgba(70, 191, 145, 0.3);
  border-radius: 3px;
}

.action-btn {
  display: inline-flex;
  align-items: center;
  padding: 3px 8px;
  background: transparent;
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 4px;
  color: $color-ice-blue;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.15s;
  white-space: nowrap;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.4);
    color: $cyan-primary;
  }
}
</style>
