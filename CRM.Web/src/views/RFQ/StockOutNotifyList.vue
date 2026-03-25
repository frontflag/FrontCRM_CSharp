<template>
  <div class="stockout-notify-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 5h18M3 12h18M3 19h18" />
            </svg>
          </div>
          <h1 class="page-title">出库通知</h1>
        </div>
        <div class="count-badge">共 {{ filteredList.length }} 条</div>
      </div>
      <div class="header-right">
        <el-input v-model="keyword" placeholder="通知单号/销售单号/客户" clearable style="width: 280px" @keyup.enter="fetchList" />
        <button class="btn-secondary" @click="fetchList">刷新</button>
      </div>
    </div>

    <CrmDataTable :data="filteredList" v-loading="loading">
      <el-table-column prop="requestCode" label="通知单号" width="170" />
      <el-table-column prop="salesOrderCode" label="销售单号" width="170" />
      <el-table-column prop="materialModel" label="物料型号" width="180" show-overflow-tooltip />
      <el-table-column prop="brand" label="品牌" width="140" show-overflow-tooltip />
      <el-table-column prop="outQuantity" label="出库数量" width="110" align="right" />
      <el-table-column prop="requestDate" label="预计出库日期" width="170">
        <template #default="{ row }">{{ formatRequestDateTime(row.requestDate) }}</template>
      </el-table-column>
      <el-table-column prop="salesUserName" label="业务员名称" width="130" show-overflow-tooltip />
      <el-table-column prop="customerName" label="客户" min-width="180" show-overflow-tooltip />
      <el-table-column label="申请人" width="140" show-overflow-tooltip>
        <template #default="{ row }">{{ row.requestUserName || '--' }}</template>
      </el-table-column>
      <el-table-column prop="createTime" label="申请时间" width="170">
        <template #default="{ row }">{{ formatRequestDateTime(row.createTime) }}</template>
      </el-table-column>
      <el-table-column prop="status" label="状态" width="110">
        <template #default="{ row }">
          <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="remark" label="备注" min-width="180" show-overflow-tooltip />
      <el-table-column label="操作" width="140" fixed="right">
        <template #default="{ row }">
          <button class="action-btn" @click="goExecute(row)">执行出库</button>
        </template>
      </el-table-column>
    </CrmDataTable>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { stockOutApi, type StockOutRequestDto } from '@/api/stockOut'
import { formatDate as formatDateTimeZh } from '@/utils/date'

const router = useRouter()
const loading = ref(false)
const keyword = ref('')
const list = ref<StockOutRequestDto[]>([])

const statusLabel = (s: number) => {
  if (s === 0) return '待出库'
  if (s === 1) return '已出库'
  if (s === 2) return '已取消'
  return '未知'
}

/** 按本地时区显示年月日 + 时分 */
const formatRequestDateTime = (v?: string | null) => {
  if (v == null || v === '') return '--'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
}

const filteredList = computed(() => {
  const k = keyword.value.trim().toLowerCase()
  if (!k) return list.value
  return list.value.filter(x =>
    (x.requestCode || '').toLowerCase().includes(k) ||
    (x.salesOrderCode || '').toLowerCase().includes(k) ||
    (x.customerName || '').toLowerCase().includes(k)
  )
})

const fetchList = async () => {
  loading.value = true
  try {
    list.value = await stockOutApi.getRequestList()
  } catch (e) {
    console.error(e)
    ElMessage.error('加载出库通知列表失败')
  } finally {
    loading.value = false
  }
}

const goExecute = (row: StockOutRequestDto) => {
  router.push({ path: '/inventory/stock-out/create', query: { requestId: row.id } })
}

onMounted(fetchList)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockout-notify-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}
.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}
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
.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
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
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid $border-panel;
  color: $text-secondary;
  font-size: 13px;
  background: rgba(255, 255, 255, 0.05);
  cursor: pointer;
}
.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-0 { background: rgba(255,193,7,0.15); color: #ffc107; }
  &.status-1 { background: rgba(70,191,145,0.18); color: #46BF91; }
  &.status-2 { background: rgba(201,87,69,0.18); color: #C95745; }
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  &:hover { text-decoration: underline; }
}
</style>
