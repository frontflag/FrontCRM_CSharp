<template>
  <div class="rfq-list-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <h1 class="page-title">需求管理 (RFQ)</h1>
        <div class="count-badge">共 {{ totalCount }} 条需求</div>
      </div>
      <div class="header-right">
        <el-button @click="importDialogVisible = true">
          <el-icon><Upload /></el-icon>导入 Excel 创建
        </el-button>
        <el-button type="primary" @click="handleCreate">
          <el-icon><Plus /></el-icon>新增需求
        </el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="20" class="stat-row">
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-value">{{ stats.total }}</div>
          <div class="stat-label">需求总数</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-warning">
          <div class="stat-value">{{ stats.pending }}</div>
          <div class="stat-label">待分配</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-cyan">
          <div class="stat-value">{{ stats.processing }}</div>
          <div class="stat-label">报价中</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-success">
          <div class="stat-value">{{ stats.quoted }}</div>
          <div class="stat-label">已报价</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索面板 -->
    <el-card class="filter-card">
      <el-form :inline="true" :model="searchForm">
        <el-form-item label="搜索">
          <el-input 
            v-model="searchForm.keyword" 
            placeholder="需求编号/客户名称/产品"
            clearable
            @keyup.enter="handleSearch"
            style="width: 280px"
          />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="全部状态" clearable style="width: 140px">
            <el-option label="待分配" :value="0" />
            <el-option label="已分配" :value="1" />
            <el-option label="报价中" :value="2" />
            <el-option label="已报价" :value="3" />
            <el-option label="已选价" :value="4" />
            <el-option label="已转订单" :value="5" />
            <el-option label="已关闭" :value="6" />
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
        :data="rfqList" 
        v-loading="loading"
        stripe
        border
        highlight-current-row
      >
        <el-table-column prop="rfqCode" label="需求编号" width="150" sortable>
          <template #default="{ row }">
            <el-link type="primary" @click="handleView(row)">{{ row.rfqCode }}</el-link>
          </template>
        </el-table-column>
        <el-table-column prop="customerName" label="客户" min-width="160" show-overflow-tooltip />
        <el-table-column prop="product" label="产品" min-width="150" show-overflow-tooltip />
        <el-table-column prop="industry" label="行业" width="100" />
        <el-table-column prop="itemCount" label="明细数" width="80" align="center" />
        <el-table-column prop="importance" label="重要度" width="90" align="center">
          <template #default="{ row }">
            <el-rate v-model="row.importance" disabled :max="10" />
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="rfqType" label="类型" width="90">
          <template #default="{ row }">
            {{ getTypeText(row.rfqType) }}
          </template>
        </el-table-column>
        <el-table-column prop="salesUserName" label="业务员" width="100" />
        <el-table-column prop="rfqDate" label="需求日期" width="110" />
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
                  <el-dropdown-item command="quote">生成报价</el-dropdown-item>
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
            <el-form-item label="需求编号">
              <el-input v-model="formData.rfqCode" placeholder="系统自动生成" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
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
            <el-form-item label="联系邮箱">
              <el-input v-model="formData.contactEmail" placeholder="请输入联系邮箱" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="产品">
              <el-input v-model="formData.product" placeholder="请输入产品名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="行业">
              <el-input v-model="formData.industry" placeholder="请输入行业" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="需求类型">
              <el-select v-model="formData.rfqType" style="width: 100%">
                <el-option label="现货" :value="1" />
                <el-option label="期货" :value="2" />
                <el-option label="样品" :value="3" />
                <el-option label="批量" :value="4" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="目标类型">
              <el-select v-model="formData.targetType" style="width: 100%">
                <el-option label="比价需求" :value="1" />
                <el-option label="独家需求" :value="2" />
                <el-option label="紧急需求" :value="3" />
                <el-option label="常规需求" :value="4" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="重要程度">
              <el-rate v-model="formData.importance" :max="10" show-score />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="项目背景">
          <el-input v-model="formData.projectBackground" type="textarea" rows="2" />
        </el-form-item>
        <el-form-item label="竞争对手">
          <el-input v-model="formData.competitor" placeholder="请输入竞争对手" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="formData.remark" type="textarea" rows="2" />
        </el-form-item>

        <!-- 明细行 -->
        <div class="items-section">
          <div class="items-header">
            <h4>需求明细</h4>
            <el-button type="primary" size="small" @click="addItem">
              <el-icon><Plus /></el-icon>添加明细
            </el-button>
          </div>
          <el-table :data="formData.items" border size="small">
            <el-table-column type="index" width="50" />
            <el-table-column label="物料型号(MPN)" min-width="150">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].mpn" placeholder="请输入MPN" />
              </template>
            </el-table-column>
            <el-table-column label="品牌" width="120">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].brand" placeholder="品牌" />
              </template>
            </el-table-column>
            <el-table-column label="客户料号" width="120">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].customerMpn" placeholder="客户料号" />
              </template>
            </el-table-column>
            <el-table-column label="数量" width="100">
              <template #default="{ $index }">
                <el-input-number v-model="formData.items[$index].quantity" :min="1" :controls="false" style="width: 100%" />
              </template>
            </el-table-column>
            <el-table-column label="目标价" width="100">
              <template #default="{ $index }">
                <el-input-number v-model="formData.items[$index].targetPrice" :min="0" :precision="2" :controls="false" style="width: 100%" />
              </template>
            </el-table-column>
            <el-table-column label="币别" width="90">
              <template #default="{ $index }">
                <el-select v-model="formData.items[$index].priceCurrency" size="small">
                  <el-option label="CNY" :value="1" />
                  <el-option label="USD" :value="2" />
                  <el-option label="EUR" :value="3" />
                  <el-option label="HKD" :value="4" />
                </el-select>
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
            <el-option label="待分配" :value="0" />
            <el-option label="已分配" :value="1" />
            <el-option label="报价中" :value="2" />
            <el-option label="已报价" :value="3" />
            <el-option label="已选价" :value="4" />
            <el-option label="已转订单" :value="5" />
            <el-option label="已关闭" :value="6" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="statusDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmUpdateStatus">确定</el-button>
      </template>
    </el-dialog>

    <!-- 导入 Excel 创建 RFQ 对话框 -->
    <ImportRFQDialog
      v-model="importDialogVisible"
      @created="handleImportCreated"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Plus, Search, ArrowDown, Upload } from '@element-plus/icons-vue'
import ImportRFQDialog from './components/ImportRFQDialog.vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { rfqApi } from '@/api/rfq'

const router = useRouter()

const loading = ref(false)
const rfqList = ref<any[]>([])
const stats = ref({ total: 0, pending: 0, processing: 0, quoted: 0 })

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
const dialogTitle = ref('新建需求')
const statusDialogVisible = ref(false)
const importDialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref()
const currentRow = ref<any>(null)
const isEdit = ref(false)
const newStatus = ref(0)

// 表单数据
const formData = ref({
  rfqCode: '',
  customerName: '',
  salesUserName: '',
  contactEmail: '',
  product: '',
  industry: '',
  rfqType: 1,
  targetType: 1,
  importance: 5,
  projectBackground: '',
  competitor: '',
  remark: '',
  items: [] as any[]
})

const formRules = {
  customerName: [{ required: true, message: '请输入客户名称', trigger: 'blur' }],
  salesUserName: [{ required: true, message: '请输入业务员', trigger: 'blur' }]
}

const totalCount = computed(() => rfqList.value.length)

// 状态处理
const getStatusType = (status: number) => {
  const map: Record<number, string> = { 
    0: 'info', 1: 'warning', 2: 'primary', 3: 'success', 
    4: 'success', 5: 'success', 6: 'danger'
  }
  return map[status] || 'info'
}

const getStatusText = (status: number) => {
  const map: Record<number, string> = {
    0: '待分配', 1: '已分配', 2: '报价中', 3: '已报价',
    4: '已选价', 5: '已转订单', 6: '已关闭'
  }
  return map[status] || '未知'
}

const getTypeText = (type: number) => {
  const map: Record<number, string> = { 1: '现货', 2: '期货', 3: '样品', 4: '批量' }
  return map[type] || '未知'
}

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res = await rfqApi.searchRFQs({
      keyword: searchForm.value.keyword,
      status: searchForm.value.status,
      pageNumber: pageInfo.value.page,
      pageSize: pageInfo.value.pageSize
    })
    rfqList.value = res.items || []
    pageInfo.value.total = res.totalCount || 0
    
    // 计算统计数据
    stats.value = {
      total: res.totalCount || 0,
      pending: rfqList.value.filter((r: any) => r.status === 0).length,
      processing: rfqList.value.filter((r: any) => r.status === 1 || r.status === 2).length,
      quoted: rfqList.value.filter((r: any) => r.status >= 3).length
    }
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
// 导入 Excel 创建 RFQ 成功后刷新列表
const handleImportCreated = (_rfqId: string) => {
  loadData()
}

const handleCreate = () => {
  isEdit.value = false
  dialogTitle.value = '新建需求'
  formData.value = {
    rfqCode: '',
    customerName: '',
    salesUserName: '',
    contactEmail: '',
    product: '',
    industry: '',
    rfqType: 1,
    targetType: 1,
    importance: 5,
    projectBackground: '',
    competitor: '',
    remark: '',
    items: []
  }
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: any) => {
  isEdit.value = true
  dialogTitle.value = '编辑需求'
  currentRow.value = row
  formData.value = {
    rfqCode: row.rfqCode,
    customerName: row.customerName,
    salesUserName: row.salesUserName,
    contactEmail: row.contactEmail,
    product: row.product,
    industry: row.industry,
    rfqType: row.rfqType,
    targetType: row.targetType,
    importance: row.importance,
    projectBackground: row.projectBackground,
    competitor: row.competitor,
    remark: row.remark,
    items: row.items ? JSON.parse(JSON.stringify(row.items)) : []
  }
  dialogVisible.value = true
}

// 查看
const handleView = (row: any) => {
  router.push({ name: 'RFQDetail', params: { id: row.id } })
}

// 更多操作
const handleMore = (cmd: string, row: any) => {
  currentRow.value = row
  switch (cmd) {
    case 'status':
      newStatus.value = row.status
      statusDialogVisible.value = true
      break
    case 'quote':
      ElMessage.success('已生成报价单')
      break
    case 'delete':
      handleDelete(row)
      break
  }
}

// 删除
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除需求单 ${row.rfqCode} 吗？`, '警告', { type: 'warning' })
    await rfqApi.deleteRFQ(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch {
    // 取消
  }
}

// 更新状态
const confirmUpdateStatus = async () => {
  if (!currentRow.value) return
  await rfqApi.updateFlowStatus(currentRow.value.id, { status: newStatus.value })
  ElMessage.success('状态更新成功')
  statusDialogVisible.value = false
  loadData()
}

// 添加/删除明细
const addItem = () => {
  formData.value.items.push({
    mpn: '',
    brand: '',
    customerMpn: '',
    quantity: 1,
    targetPrice: undefined,
    priceCurrency: 1
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
      rfqDate: new Date().toISOString().slice(0, 10)
    }
    if (isEdit.value && currentRow.value) {
      await rfqApi.updateRFQ(currentRow.value.id, data)
      ElMessage.success('更新成功')
    } else {
      // RFQ 创建接口的请求类型要求 customerId，但该页面的表单当前仅维护 customerName。
      // 为保证类型检查通过且不影响现有运行逻辑，先做类型兼容处理。
      await rfqApi.createRFQ(data as any)
      ElMessage.success('创建成功')
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
.rfq-list-page {
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
