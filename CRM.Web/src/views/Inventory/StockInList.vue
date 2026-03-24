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
        <div class="count-badge">共 {{ list.length }} 条</div>
      </div>
      <div class="header-right">
        <el-input
          v-model="keyword"
          placeholder="入库单号/来源单号"
          clearable
          style="width: 220px; margin-right: 8px;"
          @keyup.enter="fetchList"
        />
        <button class="btn-secondary" @click="fetchList">刷新</button>
        <button class="btn-primary" style="margin-left: 8px" @click="handleCreate">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          新建入库单
        </button>
      </div>
    </div>

    <CrmDataTable
      :data="filteredList"
      v-loading="loading"
      @row-dblclick="handleView"
    >
        <el-table-column type="index" width="50" align="center" />
        <el-table-column prop="stockInCode" label="入库单号" width="160">
          <template #default="{ row }">
            <span class="code-link" @click.stop="handleView(row)">{{ row.stockInCode }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="sourceCode" label="来源单号" width="160" show-overflow-tooltip />
        <el-table-column prop="warehouseId" label="仓库ID" width="140" show-overflow-tooltip />
        <el-table-column prop="vendorId" label="供应商ID" width="140" show-overflow-tooltip />
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
        <el-table-column prop="status" label="状态" width="110">
          <template #default="{ row }">
            <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="160" show-overflow-tooltip />
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <button class="action-btn" @click.stop="handleEditRemark(row)">修改备注</button>
            <button
              v-if="row.status === 0 || row.status === 1"
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
import { stockInApi, type StockInDto } from '@/api/stockIn'

const router = useRouter()
const loading = ref(false)
const list = ref<StockInDto[]>([])
const keyword = ref('')

const remarkDialogVisible = ref(false)
const remarkForm = reactive<{ id: string; remark: string }>({
  id: '',
  remark: ''
})

const formatNum = (v: number) => (v == null ? '--' : Number(v).toLocaleString())
const formatMoney = (v: number) => (v == null ? '--' : Number(v).toFixed(2))
const formatDate = (v?: string) => (v ? v.replace('T', ' ').slice(0, 16) : '--')

const statusLabel = (s: number) => {
  switch (s) {
    case 0: return '草稿'
    case 1: return '待入库'
    case 2: return '已入库'
    case 3: return '已取消'
    default: return '未知'
  }
}

const filteredList = computed(() => {
  if (!keyword.value) return list.value
  const k = keyword.value.toLowerCase()
  return list.value.filter(x =>
    x.stockInCode.toLowerCase().includes(k) ||
    (x.sourceCode && x.sourceCode.toLowerCase().includes(k))
  )
})

const fetchList = async () => {
  loading.value = true
  try {
    list.value = await stockInApi.getAll()
  } catch (e) {
    console.error(e)
    ElMessage.error('加载入库单失败')
  } finally {
    loading.value = false
  }
}

const handleCreate = () => {
  router.push('/inventory/stock-in/create')
}

const handleView = (row: StockInDto) => {
  // 暂时直接进入编辑页查看
  router.push(`/inventory/stock-in/${row.id}`)
}

const handleEditRemark = (row: StockInDto) => {
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

const handleFinish = async (row: StockInDto) => {
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
}
.btn-primary {
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border-color: rgba(0, 212, 255, 0.4);
  color: #fff;
}
.btn-secondary {
  background: rgba(255, 255, 255, 0.05);
  border-color: $border-panel;
  color: $text-secondary;
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

