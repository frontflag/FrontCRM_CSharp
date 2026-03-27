<template>
  <div class="stockin-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
              <path d="M3 9h18" />
              <path d="M9 21V9" />
            </svg>
          </div>
          <h1 class="page-title">入库单列表</h1>
        </div>
        <div class="count-badge">共 {{ filteredList.length }} 条</div>
      </div>
    </div>

    <!-- 查询栏（与客户列表一致的结构与样式） -->
    <div class="search-bar">
      <div class="search-left">
        <span class="list-title">入库单查询</span>
        <input
          v-model="filters.model"
          class="search-input search-input--filter"
          placeholder="型号"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.vendorName"
          class="search-input search-input--filter"
          placeholder="供应商名称"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.purchaseOrderCode"
          class="search-input search-input--filter"
          placeholder="采购订单号"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.salesOrderCode"
          class="search-input search-input--filter"
          placeholder="销售订单号"
          @keyup.enter="fetchList"
        />
        <button type="button" class="btn-primary btn-sm" @click="fetchList">搜索</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">重置</button>
      </div>
    </div>

    <CrmDataTable
      :data="filteredList"
      v-loading="loading"
      @row-dblclick="handleView"
    >
        <el-table-column prop="stockInCode" label="入库单号" width="160">
          <template #default="{ row }">
            <span class="code-link" @click.stop="handleView(row)">{{ row.stockInCode }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="110">
          <template #default="{ row }">
            <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="sourceDisplayNo" label="来源单号" width="160" show-overflow-tooltip />
        <el-table-column prop="warehouseId" label="仓库ID" width="140" show-overflow-tooltip />
        <el-table-column prop="vendorName" label="供应商" min-width="160" show-overflow-tooltip />
      <el-table-column prop="salesOrderCode" label="销售订单号" min-width="170" show-overflow-tooltip />
        <el-table-column prop="stockInDate" label="入库日期" width="160">
          <template #default="{ row }">
            <span class="text-secondary">{{ formatDate(row.stockInDate) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="totalQuantity" label="入库数量" width="110" align="right">
          <template #default="{ row }">{{ formatNum(row.totalQuantity) }}</template>
        </el-table-column>
        <el-table-column prop="totalAmount" label="入库金额" width="110" align="right">
          <template #default="{ row }">{{ formatMoney(row.totalAmount) }}</template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="160" show-overflow-tooltip />
        <el-table-column label="创建时间" width="160">
          <template #default="{ row }">{{ formatDate((row as any).createTime || (row as any).createdAt) }}</template>
        </el-table-column>
        <el-table-column label="创建人" width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ (row as any).createUserName || (row as any).createdBy || '--' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <button class="action-btn" @click.stop="handleEditRemark(row)">修改备注</button>
            <button
              v-if="row.status !== 2 && row.status !== 3"
              class="action-btn"
              @click.stop="handleFinish(row)"
            >
              标记已入库
            </button>
          </template>
        </el-table-column>
    </CrmDataTable>

    <el-dialog v-model="remarkDialogVisible" title="修改备注" width="420px">
      <el-input v-model="remarkForm.remark" type="textarea" :rows="4" placeholder="请输入入库单备注" />
      <template #footer>
        <button class="btn-secondary" @click="remarkDialogVisible = false">取消</button>
        <button class="btn-primary" @click="submitRemark">保存</button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { stockInApi, type StockInListItemDto } from '@/api/stockIn'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()
const loading = ref(false)
const list = ref<StockInListItemDto[]>([])
const filters = reactive({
  model: '',
  vendorName: '',
  purchaseOrderCode: '',
  salesOrderCode: ''
})

const remarkDialogVisible = ref(false)
const remarkForm = reactive<{ id: string; remark: string }>({
  id: '',
  remark: ''
})

const formatNum = (v: number) => (v == null ? '--' : Number(v).toLocaleString())
const formatMoney = (v: number) => (v == null ? '--' : Number(v).toFixed(2))
const formatDate = (v?: string) => formatDisplayDateTime(v)

const statusLabel = (s: number) => {
  switch (s) {
    case 0: return '草稿'
    case 1: return '待入库'
    case 2: return '已入库'
    case 3: return '已取消'
    default: return '未知'
  }
}

const fetchList = async () => {
  loading.value = true
  try {
    list.value = await stockInApi.getAll({
      model: filters.model || undefined,
      vendorName: filters.vendorName || undefined,
      purchaseOrderCode: filters.purchaseOrderCode || undefined,
      salesOrderCode: filters.salesOrderCode || undefined
    })
  } catch (e) {
    console.error(e)
    ElMessage.error('加载入库单失败')
  } finally {
    loading.value = false
  }
}

const keywordHit = (text: string | undefined, keyword: string): boolean => {
  if (!keyword) return true
  return (text ?? '').toLowerCase().includes(keyword.toLowerCase())
}

// 前端兜底过滤：避免后端筛选偶发不生效时页面无响应
const filteredList = computed(() => {
  const model = filters.model.trim()
  const vendorName = filters.vendorName.trim()
  const purchaseOrderCode = filters.purchaseOrderCode.trim()
  const salesOrderCode = filters.salesOrderCode.trim()

  return list.value.filter((row) => {
    const rowAny = row as any
    const modelText = `${rowAny.model ?? ''} ${rowAny.materialCode ?? ''} ${rowAny.remark ?? ''}`
    const poText = `${row.sourceDisplayNo ?? ''} ${row.stockInCode ?? ''}`
    return (
      keywordHit(modelText, model) &&
      keywordHit(row.vendorName, vendorName) &&
      keywordHit(poText, purchaseOrderCode) &&
      keywordHit(row.salesOrderCode, salesOrderCode)
    )
  })
})

const resetFilters = () => {
  filters.model = ''
  filters.vendorName = ''
  filters.purchaseOrderCode = ''
  filters.salesOrderCode = ''
  fetchList()
}

const handleView = (row: StockInListItemDto) => {
  // 暂时直接进入编辑页查看
  router.push(`/inventory/stock-in/${row.id}`)
}

const handleEditRemark = (row: StockInListItemDto) => {
  remarkForm.id = row.id
  remarkForm.remark = row.remark || ''
  remarkDialogVisible.value = true
}

const submitRemark = async () => {
  try {
    await stockInApi.update(remarkForm.id, { remark: remarkForm.remark })
    ElMessage.success('备注已更新')
    remarkDialogVisible.value = false
    fetchList()
  } catch (e) {
    console.error(e)
    ElMessage.error('更新备注失败')
  }
}

const handleFinish = async (row: StockInListItemDto) => {
  try {
    await stockInApi.updateStatus(row.id, 2)
    ElMessage.success('已标记为已入库')
    fetchList()
  } catch (e) {
    console.error(e)
    ElMessage.error('更新状态失败')
  }
}

onMounted(fetchList)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockin-list-page {
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

// ---- 查询栏（对齐客户列表 CustomerList.vue）----
.search-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.list-title {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
  white-space: nowrap;
}

.search-input {
  width: 220px;
  padding: 7px 12px 7px 12px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  outline: none;
  transition: border-color 0.2s;

  &::placeholder {
    color: $text-muted;
  }

  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }

  &--filter {
    width: 160px;
  }
}

.btn-primary,
.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid transparent;
  font-family: 'Noto Sans SC', sans-serif;
  transition: all 0.2s;
}
.btn-primary {
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border-color: rgba(0, 212, 255, 0.4);
  color: #fff;
  letter-spacing: 0.5px;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}
.btn-secondary {
  background: rgba(255, 255, 255, 0.05);
  border-color: $border-panel;
  color: $text-secondary;
}
.btn-ghost {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}
.code-link {
  color: $cyan-primary;
  cursor: pointer;
  &:hover { text-decoration: underline; }
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
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  margin-right: 4px;
  white-space: nowrap;
  flex-shrink: 0;
  &:hover { text-decoration: underline; }
}
</style>

