<template>
  <div class="sales-order-list-page">
    <div class="page-header">
      <h2>销售订单管理</h2>
      <el-button type="primary" @click="handleCreate">
        <el-icon><Plus /></el-icon>新建销售订单
      </el-button>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="20" class="stat-row">
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-value">{{ statTotal }}</div>
          <div class="stat-label">订单总数</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-warning">
          <div class="stat-value">{{ statPending }}</div>
          <div class="stat-label">待处理</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-success">
          <div class="stat-value">{{ statApproved }}</div>
          <div class="stat-label">已审批</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-info">
          <div class="stat-value">{{ canViewSalesAmount ? `¥${statAmount.toLocaleString()}` : '--' }}</div>
          <div class="stat-label">总金额</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索筛选 -->
    <el-card class="filter-card">
      <el-form :inline="true" :model="filterForm">
        <el-form-item label="订单号">
          <el-input v-model="filterForm.code" placeholder="请输入订单号" clearable />
        </el-form-item>
        <el-form-item v-if="canViewCustomerInfo" label="客户">
          <el-input v-model="filterForm.customer" placeholder="请输入客户名称" clearable />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="filterForm.status" placeholder="全部状态" clearable>
            <el-option label="草稿" :value="0" />
            <el-option label="审批中" :value="1" />
            <el-option label="已审批" :value="2" />
            <el-option label="已确认" :value="3" />
            <el-option label="已完成" :value="6" />
            <el-option label="已取消" :value="-1" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>查询
          </el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 数据表格 -->
    <el-card class="table-card">
      <el-table 
        :data="filteredList" 
        v-loading="loading"
        stripe
        border
        highlight-current-row
      >
        <el-table-column prop="sellOrderCode" label="订单号" width="160" sortable>
          <template #default="{ row }">
            <el-link type="primary" @click="handleView(row)">{{ row.sellOrderCode }}</el-link>
          </template>
        </el-table-column>
        <el-table-column v-if="canViewCustomerInfo" prop="customerName" label="客户" min-width="180" show-overflow-tooltip />
        <el-table-column prop="salesUserName" label="业务员" width="100" />
        <el-table-column v-if="canViewSalesAmount" prop="total" label="总金额" width="130" align="right">
          <template #default="{ row }">
            <span class="amount">{{ formatCurrency(row.total, row.currency) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="itemRows" label="行项目" width="80" align="center" />
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag effect="dark" :type="getStatusType(row.status)" size="small">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="purchaseOrderStatus" label="采购状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag effect="dark" :type="getPurchaseStatusType(row.purchaseOrderStatus)" size="small">
              {{ getPurchaseStatusText(row.purchaseOrderStatus) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="deliveryDate" label="交货日期" width="110" />
        <el-table-column prop="createTime" label="创建时间" width="150" />
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleView(row)">查看</el-button>
            <el-button link type="primary" @click="handleEdit(row)">编辑</el-button>
            <el-dropdown @command="(cmd: string) => handleMore(cmd, row)">
              <el-button link type="primary">
                更多<el-icon class="el-icon--right"><arrow-down /></el-icon>
              </el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="status">更新状态</el-dropdown-item>
                  <el-dropdown-item command="generate" divided>生成采购单</el-dropdown-item>
                  <el-dropdown-item command="delete" type="danger">删除</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="pageInfo.page"
          v-model:page-size="pageInfo.pageSize"
          :total="pageInfo.total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handlePageChange"
        />
      </div>
    </el-card>

    <!-- 新建/编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogTitle"
      width="900px"
      destroy-on-close
    >
      <el-form ref="formRef" :model="formData" :rules="formRules" label-width="100px">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="订单号" prop="sellOrderCode">
              <el-input v-model="formData.sellOrderCode" placeholder="系统自动生成" disabled />
            </el-form-item>
          </el-col>
          <el-col v-if="canViewCustomerInfo" :span="12">
            <el-form-item label="客户" prop="customerName">
              <el-input v-model="formData.customerName" placeholder="请输入客户名称" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="业务员" prop="salesUserName">
              <el-input v-model="formData.salesUserName" placeholder="请输入业务员" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="订单类型">
              <el-select v-model="formData.type" style="width: 100%">
                <el-option label="普通订单" :value="1" />
                <el-option label="紧急订单" :value="2" />
                <el-option label="样品订单" :value="3" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="币别">
              <el-select v-model="formData.currency" style="width: 100%">
                <el-option label="CNY 人民币" :value="1" />
                <el-option label="USD 美元" :value="2" />
                <el-option label="EUR 欧元" :value="3" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="交货日期">
              <el-date-picker
                v-model="formData.deliveryDate"
                type="date"
                placeholder="选择交货日期"
                style="width: 100%"
                value-format="YYYY-MM-DD"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="送货地址">
          <el-input v-model="formData.deliveryAddress" type="textarea" rows="2" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="formData.comment" type="textarea" rows="2" />
        </el-form-item>

        <!-- 明细行 -->
        <div class="items-section">
          <div class="items-header">
            <h4>订单明细</h4>
            <el-button type="primary" size="small" @click="addItem">
              <el-icon><Plus /></el-icon>添加明细
            </el-button>
          </div>
          <el-table :data="formData.items" border size="small">
            <el-table-column type="index" width="50" />
            <el-table-column label="物料型号" min-width="150">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].pn" placeholder="PN" />
              </template>
            </el-table-column>
            <el-table-column label="品牌" width="120">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].brand" placeholder="品牌" />
              </template>
            </el-table-column>
            <el-table-column label="数量" width="100">
              <template #default="{ $index }">
                <el-input-number v-model="formData.items[$index].qty" :min="1" :controls="false" style="width: 100%" />
              </template>
            </el-table-column>
            <el-table-column v-if="canViewSalesAmount" label="单价" width="120">
              <template #default="{ $index }">
                <el-input-number v-model="formData.items[$index].price" :min="0" :precision="2" :controls="false" style="width: 100%" />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="80" align="center">
              <template #default="{ $index }">
                <el-button link type="danger" @click="removeItem($index)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
          <div v-if="canViewSalesAmount" class="total-amount">
            合计: <span class="amount">{{ formatCurrency(calculateTotal, formData.currency) }}</span>
          </div>
        </div>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitLoading">确定</el-button>
      </template>
    </el-dialog>



    <!-- 状态更新对话框 -->
    <el-dialog v-model="statusDialogVisible" title="更新状态" width="400px">
      <el-form label-width="100px">
        <el-form-item label="新状态">
          <el-select v-model="newStatus" style="width: 100%">
            <el-option label="草稿" :value="0" />
            <el-option label="审批中" :value="1" />
            <el-option label="已审批" :value="2" />
            <el-option label="已确认" :value="3" />
            <el-option label="已发货" :value="4" />
            <el-option label="已完成" :value="6" />
            <el-option label="已取消" :value="-1" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="statusDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmUpdateStatus">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Plus, Search, ArrowDown } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
// 使用模拟数据API
import { mockSalesOrderApi as salesOrderApi } from '@/api/mockSalesOrder'
import { mockPurchaseOrderApi as purchaseOrderApi } from '@/api/mockPurchaseOrder'
import { useAuthStore } from '@/stores/auth'

const loading = ref(false)
const orderList = ref<any[]>([])
const authStore = useAuthStore()
const canViewCustomerInfo = computed(() => authStore.hasPermission('customer.info.read'))
const canViewSalesAmount = computed(() => authStore.hasPermission('sales.amount.read'))

// 筛选表单
const filterForm = ref({
  code: '',
  customer: '',
  status: undefined as number | undefined
})

// 分页信息
const pageInfo = ref({
  page: 1,
  pageSize: 10,
  total: 0
})

// 对话框控制
const dialogVisible = ref(false)
const dialogTitle = ref('新建销售订单')
const statusDialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref()
const currentRow = ref<any>(null)
const isEdit = ref(false)
const newStatus = ref(0)

// 表单数据
const formData = ref({
  sellOrderCode: '',
  customerName: '',
  salesUserName: '',
  type: 1,
  currency: 1,
  deliveryDate: '',
  deliveryAddress: '',
  comment: '',
  items: [] as any[]
})

const formRules = {
  customerName: [{ required: true, message: '请输入客户名称', trigger: 'blur' }],
  salesUserName: [{ required: true, message: '请输入业务员', trigger: 'blur' }]
}

// 计算属性：筛选后的列表
const filteredList = computed(() => {
  let result = orderList.value
  if (filterForm.value.code) {
    result = result.filter(o => o.sellOrderCode.toLowerCase().includes(filterForm.value.code.toLowerCase()))
  }
  if (filterForm.value.customer) {
    result = result.filter(o => o.customerName?.toLowerCase().includes(filterForm.value.customer.toLowerCase()))
  }
  if (filterForm.value.status !== undefined) {
    result = result.filter(o => o.status === filterForm.value.status)
  }
  pageInfo.value.total = result.length
  const start = (pageInfo.value.page - 1) * pageInfo.value.pageSize
  return result.slice(start, start + pageInfo.value.pageSize)
})

// 统计
const statTotal = computed(() => orderList.value.length)
const statPending = computed(() => orderList.value.filter(o => o.status === 0 || o.status === 1).length)
const statApproved = computed(() => orderList.value.filter(o => o.status >= 2).length)
const statAmount = computed(() => orderList.value.reduce((sum, o) => sum + (o.total || 0), 0))

// 计算总金额
const calculateTotal = computed(() => {
  return formData.value.items.reduce((sum, item) => sum + (item.qty || 0) * (item.price || 0), 0)
})

// 格式化货币
const formatCurrency = (value: number, currency?: number) => {
  const symbol = currency === 2 ? '$' : currency === 3 ? '€' : '¥'
  return symbol + (value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

// 状态处理
const getStatusType = (status: number) => {
  const map: Record<number, string> = { 
    0: 'info', 1: 'warning', 2: 'success', 3: 'primary',
    4: 'primary', 5: 'success', 6: 'success', '-1': 'danger', '-2': 'danger'
  }
  return map[status] || 'info'
}

const getStatusText = (status: number) => {
  const map: Record<number, string> = {
    0: '草稿', 1: '审批中', 2: '已审批', 3: '已确认',
    4: '已发货', 5: '已收货', 6: '已完成', '-1': '已取消', '-2': '已驳回'
  }
  return map[status] || '未知'
}

const getPurchaseStatusType = (status: number) => {
  const map: Record<number, string> = { 0: 'info', 1: 'warning', 2: 'success' }
  return map[status] || 'info'
}

const getPurchaseStatusText = (status: number) => {
  const map: Record<number, string> = { 0: '未采购', 1: '部分采购', 2: '全部采购' }
  return map[status] || '未知'
}

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res = await salesOrderApi.getList()
    orderList.value = res.data || []
    pageInfo.value.total = orderList.value.length
  } catch (error) {
    ElMessage.error('加载数据失败')
  } finally {
    loading.value = false
  }
}

// 搜索和重置
const handleSearch = () => {
  pageInfo.value.page = 1
}

const handleReset = () => {
  filterForm.value = { code: '', customer: '', status: undefined }
  pageInfo.value.page = 1
}

// 分页
const handleSizeChange = (val: number) => {
  pageInfo.value.pageSize = val
}

const handlePageChange = (val: number) => {
  pageInfo.value.page = val
}

// 新建
const handleCreate = () => {
  isEdit.value = false
  dialogTitle.value = '新建销售订单'
  formData.value = {
    sellOrderCode: 'SO' + new Date().toISOString().slice(0, 10).replace(/-/g, '') + String(Math.random()).slice(2, 5),
    customerName: '',
    salesUserName: '',
    type: 1,
    currency: 1,
    deliveryDate: '',
    deliveryAddress: '',
    comment: '',
    items: []
  }
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: any) => {
  isEdit.value = true
  dialogTitle.value = '编辑销售订单'
  currentRow.value = row
  formData.value = {
    sellOrderCode: row.sellOrderCode,
    customerName: row.customerName,
    salesUserName: row.salesUserName,
    type: row.type,
    currency: row.currency,
    deliveryDate: row.deliveryDate,
    deliveryAddress: row.deliveryAddress,
    comment: row.comment,
    items: row.items ? JSON.parse(JSON.stringify(row.items)) : []
  }
  dialogVisible.value = true
}

const router = useRouter()
// 查看
const handleView = (row: any) => {
  router.push({ name: 'SalesOrderDetail', params: { id: row.id } })
}

// 更多操作
const handleMore = (cmd: string, row: any) => {
  currentRow.value = row
  switch (cmd) {
    case 'status':
      newStatus.value = row.status
      statusDialogVisible.value = true
      break
    case 'generate':
      handleGeneratePO(row)
      break
    case 'delete':
      handleDelete(row)
      break
  }
}

// 生成采购单
const handleGeneratePO = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要根据销售订单 ${row.sellOrderCode} 生成采购订单吗？`, '提示', { type: 'info' })
    await purchaseOrderApi.autoGenerate(row.id)
  } catch {
    // 取消
  }
}

// 删除
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除销售订单 ${row.sellOrderCode} 吗？`, '警告', { type: 'warning' })
    await salesOrderApi.delete(row.id)
    loadData()
  } catch {
    // 取消
  }
}

// 更新状态
const confirmUpdateStatus = async () => {
  if (!currentRow.value) return
  await salesOrderApi.updateStatus(currentRow.value.id, newStatus.value)
  statusDialogVisible.value = false
  loadData()
}

// 添加/删除明细
const addItem = () => {
  formData.value.items.push({
    pn: '',
    brand: '',
    qty: 1,
    price: 0,
    currency: formData.value.currency
  })
}

const removeItem = (index: number) => {
  formData.value.items.splice(index, 1)
}

// 提交
const handleSubmit = async () => {
  await formRef.value.validate()
  submitLoading.value = true
  try {
    const data = {
      ...formData.value,
      total: calculateTotal.value,
      itemRows: formData.value.items.length
    }
    if (isEdit.value && currentRow.value) {
      await salesOrderApi.update(currentRow.value.id, data)
    } else {
      await salesOrderApi.create(data)
    }
    dialogVisible.value = false
    loadData()
  } finally {
    submitLoading.value = false
  }
}

onMounted(loadData)
</script>

<style scoped lang="scss">
.sales-order-list-page {
  padding: 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  h2 {
    margin: 0;
    color: #E8F4FF;
    font-size: 20px;
  }
}

.stat-row {
  margin-bottom: 20px;
}

.stat-card {
  text-align: center;
  background: #0A1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
  :deep(.el-card__body) {
    padding: 15px;
  }
  .stat-value {
    font-size: 24px;
    font-weight: bold;
    color: #00D4FF;
    margin-bottom: 5px;
  }
  .stat-label {
    font-size: 14px;
    color: rgba(200, 216, 232, 0.6);
  }
}

.filter-card {
  margin-bottom: 20px;
  background: #0A1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
}

.table-card {
  background: #0A1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
  :deep(.el-table) {
    background: transparent;
    --el-table-header-bg-color: rgba(0, 212, 255, 0.1);
    --el-table-tr-bg-color: transparent;
    --el-table-border-color: rgba(0, 212, 255, 0.1);
    color: #E8F4FF;

    // 操作列按钮禁止折行
    .el-table__cell .el-button {
      white-space: nowrap !important;
    }
    .el-table__cell .cell {
      white-space: nowrap;
    }
  }
}

.amount {
  color: #00D4FF;
  font-weight: 500;
}

.pagination-wrapper {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

.items-section {
  margin-top: 20px;
  padding-top: 20px;
  border-top: 1px solid rgba(0, 212, 255, 0.1);
  .items-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 10px;
    h4 {
      margin: 0;
      color: #E8F4FF;
    }
  }
  .total-amount {
    margin-top: 10px;
    text-align: right;
    font-size: 16px;
    color: #E8F4FF;
    .amount {
      font-size: 20px;
      font-weight: bold;
      color: #00D4FF;
    }
  }
}

.tags-row {
  display: flex;
  align-items: center;
  gap: 8px;
}
.doc-tab-content {
  padding: 8px 0;
}
.detail-tabs {
  :deep(.el-tabs__header) {
    margin-bottom: 16px;
  }
  :deep(.el-tabs__content) {
    min-height: 200px;
  }
}
</style>
