<template>
  <div class="rfq-item-list-page">
    <div class="page-header">
      <div class="header-left">
        <h1 class="page-title">需求明细</h1>
        <div class="count-badge">共 {{ totalCount }} 条</div>
      </div>
      <div class="header-right">
        <el-button type="primary" :disabled="!selectedRows.length" @click="handleBatchQuote">
          批量报价
        </el-button>
      </div>
    </div>

    <el-card class="filter-card">
      <el-form :inline="true" :model="searchForm">
        <el-form-item label="需求创建日期">
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始"
            end-placeholder="结束"
            value-format="YYYY-MM-DD"
            clearable
          />
        </el-form-item>
        <el-form-item label="客户">
          <el-input v-model="searchForm.customerKeyword" placeholder="客户名称模糊" clearable style="width: 180px" />
        </el-form-item>
        <el-form-item label="物料型号">
          <el-input v-model="searchForm.materialModel" placeholder="MPN / 客户料号" clearable style="width: 160px" />
        </el-form-item>
        <el-form-item label="业务员">
          <el-input v-model="searchForm.salesUserKeyword" placeholder="姓名/账号模糊" clearable style="width: 160px" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card class="table-card">
      <el-table
        :data="tableData"
        v-loading="loading"
        row-key="id"
        @selection-change="onSelectionChange"
      >
        <el-table-column type="selection" width="48" :reserve-selection="true" />
        <el-table-column label="需求创建" width="110">
          <template #default="{ row }">{{ formatDate(row.rfqCreateTime) }}</template>
        </el-table-column>
        <el-table-column prop="rfqCode" label="需求编号" width="140" show-overflow-tooltip />
        <el-table-column prop="customerName" label="客户" min-width="140" show-overflow-tooltip />
        <el-table-column label="物料型号" min-width="140" show-overflow-tooltip>
          <template #default="{ row }">{{ row.materialModel || row.mpn || '—' }}</template>
        </el-table-column>
        <el-table-column label="客户料号" width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ row.customerMaterialModel || row.customerMpn || '—' }}</template>
        </el-table-column>
        <el-table-column prop="quantity" label="数量" width="90" align="right" />
        <el-table-column prop="salesUserName" label="业务员" width="100" show-overflow-tooltip />
        <el-table-column label="明细状态" width="96" align="center">
          <template #default="{ row }">
            <el-tag size="small" effect="dark">{{ itemStatusText(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="goDetail(row)">详情</el-button>
            <el-button link type="primary" @click="goQuote(row)">报价</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="pageInfo.page"
          v-model:page-size="pageInfo.pageSize"
          :total="pageInfo.total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { rfqApi } from '@/api/rfq'
import type { RFQItem } from '@/types/rfq'

const router = useRouter()

const loading = ref(false)
const tableData = ref<RFQItem[]>([])
const totalCount = ref(0)
const selectedRows = ref<RFQItem[]>([])
const dateRange = ref<[string, string] | null>(null)

const searchForm = reactive({
  customerKeyword: '',
  materialModel: '',
  salesUserKeyword: ''
})

const pageInfo = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

function formatDate(val?: string) {
  if (!val) return '—'
  return String(val).split('T')[0]
}

function itemStatusText(s?: number) {
  const map: Record<number, string> = {
    0: '待报价',
    1: '已报价',
    2: '已接受',
    3: '已拒绝',
    4: '已关闭'
  }
  return s !== undefined ? map[s] ?? '—' : '—'
}

function mapRow(row: any): RFQItem {
  return {
    ...row,
    rfqCreateTime: row.rfqCreateTime,
    materialModel: row.mpn ?? row.materialModel,
    customerMaterialModel: row.customerMpn ?? row.customerMaterialModel
  }
}

async function loadData() {
  loading.value = true
  try {
    const res = await rfqApi.searchRFQItems({
      pageNumber: pageInfo.page,
      pageSize: pageInfo.pageSize,
      startDate: dateRange.value?.[0],
      endDate: dateRange.value?.[1],
      customerKeyword: searchForm.customerKeyword || undefined,
      materialModel: searchForm.materialModel || undefined,
      salesUserKeyword: searchForm.salesUserKeyword || undefined
    })
    tableData.value = (res.items || []).map(mapRow)
    totalCount.value = res.totalCount ?? 0
    pageInfo.total = res.totalCount ?? 0
  } catch {
    tableData.value = []
    totalCount.value = 0
    pageInfo.total = 0
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  pageInfo.page = 1
  loadData()
}

function handleReset() {
  dateRange.value = null
  searchForm.customerKeyword = ''
  searchForm.materialModel = ''
  searchForm.salesUserKeyword = ''
  pageInfo.page = 1
  loadData()
}

function onSelectionChange(rows: RFQItem[]) {
  selectedRows.value = rows
}

function goDetail(row: RFQItem) {
  if (!row.rfqId) return
  router.push({ name: 'RFQDetail', params: { id: row.rfqId } })
}

function goQuote(row: RFQItem) {
  if (!row.rfqId || !row.id) return
  router.push({
    name: 'QuoteCreate',
    query: {
      rfqId: row.rfqId,
      rfqItemId: row.id,
      ...(row.rfqCode ? { rfqCode: row.rfqCode } : {})
    }
  })
}

function handleBatchQuote() {
  const rows = selectedRows.value
  if (!rows.length) {
    ElMessage.warning('请先勾选需求明细')
    return
  }
  const rfqIds = new Set(rows.map((r) => r.rfqId).filter(Boolean))
  if (rfqIds.size !== 1) {
    ElMessage.warning('批量报价仅支持同一需求下的多条明细，请重新选择')
    return
  }
  const rfqId = [...rfqIds][0]!
  const ids = rows.map((r) => r.id).filter(Boolean)
  const code = rows[0]?.rfqCode
  router.push({
    name: 'QuoteCreate',
    query: {
      rfqId,
      rfqItemIds: ids.join(','),
      ...(code ? { rfqCode: code } : {})
    }
  })
}

onMounted(() => {
  loadData()
})
</script>

<style scoped lang="scss">
.rfq-item-list-page {
  padding: 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;

  .header-left {
    display: flex;
    align-items: center;
  }

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
</style>
