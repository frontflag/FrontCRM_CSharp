<template>
  <div class="purchase-requisition-list-page">
    <div class="page-header">
      <h2>采购申请列表</h2>
      <el-button type="primary" @click="handleCreate">
        <el-icon><Plus /></el-icon>新建采购申请
      </el-button>
    </div>

    <el-card class="filter-card">
      <el-form :inline="true" :model="filterForm">
        <el-form-item label="单号">
          <el-input v-model="filterForm.billCode" placeholder="请输入采购申请号" clearable />
        </el-form-item>
        <el-form-item label="销售订单">
          <el-input v-model="filterForm.sellOrderId" placeholder="请输入销售订单ID" clearable />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="filterForm.status" placeholder="全部状态" clearable>
            <el-option :value="0" label="新建" />
            <el-option :value="1" label="部分完成" />
            <el-option :value="2" label="全部完成" />
            <el-option :value="3" label="已取消" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="loadList" :loading="loading">查询</el-button>
          <el-button @click="handleReset" :disabled="loading">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card class="table-card">
      <CrmDataTable :data="list" v-loading="loading" highlight-current-row>
        <el-table-column prop="billCode" label="采购申请号" width="160" min-width="160" show-overflow-tooltip />
        <el-table-column prop="status" label="状态" width="160" align="center">
          <template #default="{ row }">
            <el-tag effect="dark" :type="getStatusTagType(row.status)" size="small">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="sellOrderCode" label="销售订单" width="160" min-width="160" show-overflow-tooltip />
        <el-table-column prop="pn" label="物料型号" min-width="140" show-overflow-tooltip />
        <el-table-column prop="brand" label="品牌" min-width="120" show-overflow-tooltip />
        <el-table-column prop="qty" label="申请数量" width="120" align="right" />
        <el-table-column prop="expectedPurchaseTime" label="预计采购日期" width="160">
          <template #default="{ row }">
            {{ formatDisplayDateTime(row.expectedPurchaseTime) }}
          </template>
        </el-table-column>
        <el-table-column prop="type" label="类型" width="110" align="center">
          <template #default="{ row }">
            {{ getTypeText(row.type) }}
          </template>
        </el-table-column>
        <el-table-column prop="purchaseUserId" label="采购员ID" width="140" show-overflow-tooltip />
        <el-table-column prop="remark" label="备注" min-width="180" show-overflow-tooltip />
        <el-table-column label="创建时间" width="160">
          <template #default="{ row }">
            {{ row.createTime ? formatDisplayDateTime(row.createTime) : '--' }}
          </template>
        </el-table-column>
        <el-table-column label="创建人" width="120" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.createUserName || row.createdBy || row.purchaseUserName || row.purchaseUserId || '--' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleView(row)">查看</el-button>
            <el-button link type="success" size="small" @click="handleGeneratePurchaseOrder(row)">
              生成采购订单
            </el-button>
          </template>
        </el-table-column>
      </CrmDataTable>

      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          :total="total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @current-change="loadList"
          @size-change="loadList"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Plus } from '@element-plus/icons-vue'
import { purchaseRequisitionApi } from '@/api/purchaseRequisition'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()

const loading = ref(false)
const list = ref<any[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)

const filterForm = reactive({
  billCode: '',
  sellOrderId: '',
  status: undefined as number | undefined
})

function getStatusText(s: number) {
  const m: Record<number, string> = {
    0: '新建',
    1: '部分完成',
    2: '全部完成',
    3: '已取消'
  }
  return m[s] ?? String(s)
}

function getStatusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  if (s === 0) return 'info'
  if (s === 1 || s === 2) return 'success'
  if (s === 3) return 'danger'
  return ''
}

function getTypeText(t: number) {
  const m: Record<number, string> = {
    0: '专属',
    1: '公开备货'
  }
  return m[t] ?? String(t)
}

async function loadList() {
  // 用于定位菜单点击后是否真的进入列表页
  // 用 console.log 避免 console.debug 被浏览器/控制台过滤
  console.log('[PurchaseRequisitionList] loadList() called')

  loading.value = true
  try {
    const data = await purchaseRequisitionApi.getList({
      keyword: filterForm.billCode.trim() || undefined,
      sellOrderId: filterForm.sellOrderId.trim() || undefined,
      status: filterForm.status,
      page: page.value,
      pageSize: pageSize.value
    })
    list.value = data?.items ?? []
    total.value = data?.total ?? 0
  } catch (e: any) {
    // eslint-disable-next-line no-console
    console.error(e)
  } finally {
    loading.value = false
  }
}

function handleReset() {
  filterForm.billCode = ''
  filterForm.sellOrderId = ''
  filterForm.status = undefined
  page.value = 1
  loadList()
}

function handleCreate() {
  router.push('/purchase-requisitions/new')
}

function handleView(row: any) {
  router.push(`/purchase-requisitions/${row.id}`)
}

function handleGeneratePurchaseOrder(row: any) {
  router.push({ name: 'PurchaseOrderCreate', query: { requisitionId: row?.id } })
}

onMounted(loadList)
</script>

<style scoped>
.purchase-requisition-list-page {
  padding: 20px;
}
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}
.page-header h2 {
  margin: 0;
  color: #E8F4FF;
  font-size: 20px;
}
.filter-card {
  margin-bottom: 20px;
  background: #0A1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
}
.table-card {
  margin-top: 0;
  background: #0A1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
  :deep(.el-table) {
    background: transparent;
    --el-table-header-bg-color: rgba(0, 212, 255, 0.1);
    --el-table-tr-bg-color: transparent;
    --el-table-border-color: rgba(0, 212, 255, 0.1);
    color: #E8F4FF;

    .el-table__cell .el-button {
      white-space: nowrap !important;
    }
  }
}
.pagination-wrapper {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>

