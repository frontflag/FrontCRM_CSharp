<template>
  <div class="stockout-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM17 14l4 4-4 4M10 17h11" />
            </svg>
          </div>
          <h1 class="page-title">出库单列表</h1>
        </div>
        <div class="count-badge">共 {{ filteredList.length }} 条</div>
      </div>
      <div class="header-right">
        <el-input
          v-model="keyword"
          placeholder="出库单号/来源单号"
          clearable
          style="width: 220px; margin-right: 8px;"
          @keyup.enter="handleSearch"
        />
        <button class="btn-secondary" type="button" @click="handleSearch">搜索</button>
        <button class="btn-secondary" @click="fetchList">刷新</button>
      </div>
    </div>

    <CrmDataTable
      :data="filteredList"
      v-loading="loading"
    >
        <el-table-column prop="stockOutCode" label="出库单号" width="160">
          <template #default="{ row }">
            <span class="code-link">{{ row.stockOutCode }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="110">
          <template #default="{ row }">
            <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="sourceCode" label="来源申请" width="160" show-overflow-tooltip />
        <el-table-column prop="warehouseId" label="仓库ID" width="140" show-overflow-tooltip />
        <el-table-column prop="stockOutDate" label="出库日期" width="160">
          <template #default="{ row }">
            <span class="text-secondary">{{ formatDate(row.stockOutDate) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="totalQuantity" label="出库数量" width="110" align="right">
          <template #default="{ row }">{{ formatNum(row.totalQuantity) }}</template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="160" show-overflow-tooltip />
        <el-table-column label="创建时间" width="160">
          <template #default="{ row }">{{ formatDate((row as any).createTime || (row as any).createdAt) }}</template>
        </el-table-column>
        <el-table-column label="创建人" width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ (row as any).createUserName || (row as any).createdBy || '--' }}</template>
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
                <button
                  v-if="row.status !== 4"
                  type="button"
                  class="action-btn action-btn--warning"
                  @click.stop="handleMarkFinish(row)"
                >
                  标记完成
                </button>
              </div>

              <el-dropdown v-else trigger="click" placement="bottom-end">
                <button type="button" class="op-more-trigger">...</button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item v-if="row.status !== 4" @click.stop="handleMarkFinish(row)">
                      <span class="op-more-item op-more-item--warning">标记完成</span>
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
          </template>
        </el-table-column>
    </CrmDataTable>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { stockOutApi, type StockOutDto } from '@/api/stockOut'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const route = useRoute()
const router = useRouter()
const loading = ref(false)
const list = ref<StockOutDto[]>([])
const keyword = ref('')

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 140
const OP_COL_EXPANDED_MIN_WIDTH = 140
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() =>
  opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH
)
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

function syncKeywordFromRoute() {
  if (route.name !== 'StockOutList') return
  const q = route.query
  keyword.value = typeof q.keyword === 'string' ? q.keyword : ''
}

watch(
  () => [route.name, route.query] as const,
  () => {
    syncKeywordFromRoute()
    if (route.name === 'StockOutList') fetchList()
  },
  { deep: true, immediate: true }
)

/** 与左侧检索面板共用 URL query（keyword 仅前端过滤，仍刷新列表数据） */
const handleSearch = () => {
  const k = keyword.value.trim()
  router.replace({ name: 'StockOutList', query: k ? { keyword: k } : {} })
}

const formatNum = (v: number) => (v == null ? '--' : Number(v).toLocaleString())
const formatDate = (v?: string) => formatDisplayDateTime(v)

const statusLabel = (s: number) => {
  switch (s) {
    case 0: return '草稿'
    case 1: return '待出库'
    case 2: return '已出库'
    case 3: return '已取消'
    case 4: return '已完成'
    default: return '未知'
  }
}

const filteredList = computed(() => {
  if (!keyword.value) return list.value
  const k = keyword.value.toLowerCase()
  return list.value.filter(x =>
    x.stockOutCode.toLowerCase().includes(k) ||
    (x.sourceCode && x.sourceCode.toLowerCase().includes(k))
  )
})

const fetchList = async () => {
  loading.value = true
  try {
    list.value = await stockOutApi.getAll()
  } catch (e) {
    console.error(e)
    ElMessage.error('加载出库单失败')
  } finally {
    loading.value = false
  }
}

const handleMarkFinish = async (row: StockOutDto) => {
  try {
    await stockOutApi.updateStatus(row.id, 4)
    ElMessage.success('已标记为完成')
    fetchList()
  } catch (e) {
    console.error(e)
    ElMessage.error('更新状态失败')
  }
}

</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockout-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  .header-left { display: flex; align-items: center; gap: 12px; }
  .header-right { display: flex; align-items: center; gap: 8px; }
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
  .page-icon {
    width: 36px;
    height: 36px;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.25);
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: $cyan-primary;
  }
  .page-title { font-size: 20px; font-weight: 600; color: $text-primary; margin: 0; }
}
.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}
.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid transparent;
  background: rgba(255, 255, 255, 0.05);
  border-color: $border-panel;
  color: $text-secondary;
}
.code-link {
  color: $cyan-primary;
  cursor: default;
}
.text-secondary { color: $text-muted; }
.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-0 { background: rgba(255,255,255,0.05); color: $text-muted; }
  &.status-1 { background: rgba(255,193,7,0.15); color: #ffc107; }
  &.status-2 { background: rgba(70,191,145,0.18); color: #46BF91; }
  &.status-3 { background: rgba(201,87,69,0.18); color: #C95745; }
  &.status-4 { background: rgba(0,212,255,0.18); color: $cyan-primary; }
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  white-space: nowrap;
  flex-shrink: 0;
  &:hover { text-decoration: underline; }
}
</style>

