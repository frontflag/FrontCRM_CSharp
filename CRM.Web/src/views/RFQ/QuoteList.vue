<template>
  <div class="quote-list-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <h1 class="page-title">报价管理 (Quote)</h1>
        <div class="count-badge">共 {{ totalCount }} 条报价</div>
      </div>
      <div class="header-right">
        <el-button type="primary" @click="handleCreate">
          <el-icon><Plus /></el-icon>新增报价
        </el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="20" class="stat-row">
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-value">{{ stats.total }}</div>
          <div class="stat-label">报价总数</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-warning">
          <div class="stat-value">{{ stats.pending }}</div>
          <div class="stat-label">草稿/待审</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-cyan">
          <div class="stat-value">{{ stats.sent }}</div>
          <div class="stat-label">已发送</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-success">
          <div class="stat-value">{{ stats.accepted }}</div>
          <div class="stat-label">已接受</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索面板 -->
    <el-card class="filter-card">
      <el-form :inline="true" :model="searchForm">
        <el-form-item label="搜索">
          <el-input 
            v-model="searchForm.keyword" 
            placeholder="报价编号/MPN/客户"
            clearable
            @keyup.enter="handleSearch"
            style="width: 280px"
          />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="全部状态" clearable style="width: 140px">
            <el-option label="草稿" :value="0" />
            <el-option label="待审核" :value="1" />
            <el-option label="已审核" :value="2" />
            <el-option label="已发送" :value="3" />
            <el-option label="已接受" :value="4" />
            <el-option label="已拒绝" :value="5" />
            <el-option label="已过期" :value="6" />
            <el-option label="已关闭" :value="7" />
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
        :data="quoteList" 
        v-loading="loading"
        stripe
        border
        highlight-current-row
      >
        <el-table-column prop="quoteCode" label="报价编号" width="150" sortable>
          <template #default="{ row }">
            <el-link type="primary" @click="handleView(row)">{{ row.quoteCode }}</el-link>
          </template>
        </el-table-column>
        <el-table-column prop="mpn" label="物料型号" min-width="150" show-overflow-tooltip />
        <el-table-column prop="customerName" label="客户" min-width="140" show-overflow-tooltip />
        <el-table-column prop="salesUserName" label="业务员" width="100" />
        <el-table-column prop="purchaseUserName" label="采购员" width="100" />
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag effect="dark" :type="getStatusType(row.status)" size="small">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="供应商数" width="90" align="center">
          <template #default="{ row }">
            {{ row.items?.length || 0 }}
          </template>
        </el-table-column>
        <el-table-column prop="quoteDate" label="报价日期" width="110" />
        <el-table-column prop="createTime" label="创建时间" width="150" />
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleView(row)">查看</el-button>
            <el-button link type="primary" @click="handleEdit(row)">编辑</el-button>
            <el-dropdown @command="(cmd: string) => handleMore(cmd, row)">
              <el-button link type="primary">
                更多<el-icon class="el-icon--right"><ArrowDown /></el-icon>
              </el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="status">更新状态</el-dropdown-item>
                  <el-dropdown-item command="delete" type="danger" divided>删除</el-dropdown-item>
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
      width="950px"
      destroy-on-close
    >
      <el-form ref="formRef" :model="formData" :rules="formRules" label-width="100px">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="报价编号">
              <el-input v-model="formData.quoteCode" placeholder="系统自动生成" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="物料型号" prop="mpn">
              <el-input v-model="formData.mpn" placeholder="请输入MPN" />
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
            <el-form-item label="采购员">
              <el-input v-model="formData.purchaseUserName" placeholder="请输入采购员" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="备注">
          <el-input v-model="formData.remark" type="textarea" rows="2" />
        </el-form-item>

        <!-- 供应商报价明细 -->
        <div class="items-section">
          <div class="items-header">
            <h4>供应商报价明细</h4>
            <el-button type="primary" size="small" @click="addItem">
              <el-icon><Plus /></el-icon>添加供应商报价
            </el-button>
          </div>
          <el-table :data="formData.items" border size="small">
            <el-table-column type="index" width="50" />
            <el-table-column label="供应商" min-width="140">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].vendorName" placeholder="供应商名称" />
              </template>
            </el-table-column>
            <el-table-column label="联系人" width="120">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].contactName" placeholder="联系人" />
              </template>
            </el-table-column>
            <el-table-column label="品牌" width="100">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].brand" placeholder="品牌" />
              </template>
            </el-table-column>
            <el-table-column label="数量" width="80">
              <template #default="{ $index }">
                <el-input-number v-model="formData.items[$index].quantity" :min="1" :controls="false" style="width: 100%" />
              </template>
            </el-table-column>
            <el-table-column label="单价" width="110">
              <template #default="{ $index }">
                <el-input-number v-model="formData.items[$index].unitPrice" :min="0" :precision="4" :controls="false" style="width: 100%" />
              </template>
            </el-table-column>
            <el-table-column label="币别" width="80">
              <template #default="{ $index }">
                <el-select v-model="formData.items[$index].currency" size="small" style="width: 100%">
                  <el-option label="USD" :value="1" />
                  <el-option label="CNY" :value="0" />
                </el-select>
              </template>
            </el-table-column>
            <el-table-column label="交期" width="100">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].leadTime" placeholder="交期" size="small" />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="80" align="center">
              <template #default="{ $index }">
                <el-button link type="danger" @click="removeItem($index)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
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
            <el-option label="待审核" :value="1" />
            <el-option label="已审核" :value="2" />
            <el-option label="已发送" :value="3" />
            <el-option label="已接受" :value="4" />
            <el-option label="已拒绝" :value="5" />
            <el-option label="已过期" :value="6" />
            <el-option label="已关闭" :value="7" />
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
import { mockQuoteApi as quoteApi } from '@/api/mockQuote'

const router = useRouter()

const loading = ref(false)
const quoteList = ref<any[]>([])
const stats = ref({ total: 0, pending: 0, sent: 0, accepted: 0 })

// 搜索表单
const searchForm = ref({
  keyword: '',
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
const dialogTitle = ref('新建报价')
const statusDialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref()
const currentRow = ref<any>(null)
const isEdit = ref(false)
const newStatus = ref(0)

// 表单数据
const formData = ref({
  quoteCode: '',
  mpn: '',
  salesUserName: '',
  purchaseUserName: '',
  remark: '',
  items: [] as any[]
})

const formRules = {
  mpn: [{ required: true, message: '请输入物料型号', trigger: 'blur' }],
  salesUserName: [{ required: true, message: '请输入业务员', trigger: 'blur' }]
}

const totalCount = computed(() => quoteList.value.length)

// 状态处理
const getStatusType = (status: number) => {
  const map: Record<number, string> = { 
    0: 'info', 1: 'warning', 2: 'primary', 3: 'success',
    4: 'success', 5: 'danger', 6: 'info', 7: 'info'
  }
  return map[status] || 'info'
}

const getStatusText = (status: number) => {
  const map: Record<number, string> = {
    0: '草稿', 1: '待审核', 2: '已审核', 3: '已发送',
    4: '已接受', 5: '已拒绝', 6: '已过期', 7: '已关闭'
  }
  return map[status] || '未知'
}

// 计算统计
const calculateStats = () => {
  stats.value = {
    total: quoteList.value.length,
    pending: quoteList.value.filter(q => q.status === 0 || q.status === 1).length,
    sent: quoteList.value.filter(q => q.status === 3).length,
    accepted: quoteList.value.filter(q => q.status === 4).length
  }
}

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res = await quoteApi.getList(searchForm.value)
    quoteList.value = res.data || []
    pageInfo.value.total = res.total || 0
    calculateStats()
  } catch (error) {
    ElMessage.error('加载数据失败')
  } finally {
    loading.value = false
  }
}

// 搜索和重置
const handleSearch = () => {
  pageInfo.value.page = 1
  loadData()
}

const handleReset = () => {
  searchForm.value = { keyword: '', status: undefined }
  loadData()
}

// 分页
const handleSizeChange = (val: number) => {
  pageInfo.value.pageSize = val
  loadData()
}

const handlePageChange = (val: number) => {
  pageInfo.value.page = val
  loadData()
}

// 新建
const handleCreate = () => {
  isEdit.value = false
  dialogTitle.value = '新建报价'
  formData.value = {
    quoteCode: '',
    mpn: '',
    salesUserName: '',
    purchaseUserName: '',
    remark: '',
    items: []
  }
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: any) => {
  isEdit.value = true
  dialogTitle.value = '编辑报价'
  currentRow.value = row
  formData.value = {
    quoteCode: row.quoteCode,
    mpn: row.mpn,
    salesUserName: row.salesUserName,
    purchaseUserName: row.purchaseUserName,
    remark: row.remark,
    items: row.items ? JSON.parse(JSON.stringify(row.items)) : []
  }
  dialogVisible.value = true
}

// 查看
const handleView = (row: any) => {
  router.push({ name: 'QuoteDetail', params: { id: row.id } })
}

// 更多操作
const handleMore = (cmd: string, row: any) => {
  currentRow.value = row
  switch (cmd) {
    case 'status':
      newStatus.value = row.status
      statusDialogVisible.value = true
      break
    case 'delete':
      handleDelete(row)
      break
  }
}

// 删除
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除报价单 ${row.quoteCode} 吗？`, '警告', { type: 'warning' })
    await quoteApi.delete(row.id)
    loadData()
  } catch {
    // 取消
  }
}

// 更新状态
const confirmUpdateStatus = async () => {
  if (!currentRow.value) return
  await quoteApi.updateStatus(currentRow.value.id, newStatus.value)
  statusDialogVisible.value = false
  loadData()
}

// 添加/删除明细
const addItem = () => {
  formData.value.items.push({
    vendorName: '',
    contactName: '',
    brand: '',
    quantity: 1,
    unitPrice: 0,
    currency: 1,
    leadTime: '',
    stockQty: 0
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
      quoteDate: new Date().toISOString().slice(0, 10)
    }
    if (isEdit.value && currentRow.value) {
      await quoteApi.update(currentRow.value.id, data)
    } else {
      await quoteApi.create(data)
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
.quote-list-page {
  padding: 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  .page-title {
    margin: 0;
    color: #E8F4FF;
    font-size: 20px;
  }
  .count-badge {
    margin-left: 10px;
    padding: 2px 10px;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.3);
    border-radius: 12px;
    font-size: 12px;
    color: #00D4FF;
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
  &.stat-warning .stat-value { color: #E6A23C; }
  &.stat-cyan .stat-value { color: #00D4FF; }
  &.stat-success .stat-value { color: #67C23A; }
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
}
</style>
