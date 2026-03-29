<template>
  <div class="so-item-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" />
            </svg>
          </div>
          <h1 class="page-title">销售订单明细</h1>
        </div>
        <div class="list-count-badge">共 {{ total }} 条</div>
      </div>
      <div class="header-right">
        <button
          v-if="canPurchaseReq"
          type="button"
          class="btn-primary btn-sm"
          :disabled="!selectedRows.length"
          @click="batchApplyPurchase"
        >
          批量申请采购
        </button>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <span class="list-title">筛选</span>
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="订单生成起"
          end-placeholder="订单生成止"
          value-format="YYYY-MM-DD"
          class="so-date-range"
          clearable
        />
        <input
          v-if="canViewCustomer"
          v-model="filters.customerName"
          class="search-input so-filter-input"
          placeholder="客户名称"
          @keyup.enter="loadList"
        />
        <input
          v-model="filters.salesUserName"
          class="search-input so-filter-input"
          placeholder="业务员名称"
          @keyup.enter="loadList"
        />
        <input v-model="filters.pn" class="search-input so-filter-input" placeholder="物料型号" @keyup.enter="loadList" />
        <button type="button" class="btn-primary btn-sm" @click="loadList">查询</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">重置</button>
      </div>
    </div>

    <CrmDataTable
      ref="tableRef"
      class="quantum-table-block el-table-host"
      :data="list"
      v-loading="loading"
      row-key="sellOrderItemId"
      @selection-change="onSelectionChange"
      @row-dblclick="goDetail"
    >
        <el-table-column type="selection" width="48" :reserve-selection="true" />
        <el-table-column prop="sellOrderCode" label="销售单号" width="160" min-width="160" show-overflow-tooltip />
        <el-table-column prop="orderStatus" label="状态" width="160" align="center">
          <template #default="{ row }">
            <el-tag effect="dark" :type="statusTagType(row.orderStatus)" size="small">{{ statusText(row.orderStatus) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="orderCreateTime" label="订单生成日期" width="160">
          <template #default="{ row }">{{ formatDt(row.orderCreateTime) }}</template>
        </el-table-column>
        <el-table-column v-if="canViewCustomer" prop="customerName" label="客户名称" min-width="200" show-overflow-tooltip />
        <el-table-column prop="salesUserName" label="业务员" width="100" show-overflow-tooltip />
        <el-table-column prop="pn" label="物料型号" min-width="130" show-overflow-tooltip />
        <el-table-column prop="brand" label="品牌" width="110" show-overflow-tooltip />
        <el-table-column prop="qty" label="数量" width="100" align="right" />
        <el-table-column v-if="canViewAmount" prop="price" label="单价" width="160" align="right">
          <template #default="{ row }">{{ formatMoney(row.price, row.currency) }}</template>
        </el-table-column>
        <el-table-column v-if="canViewAmount" prop="lineTotal" label="明细总额" width="160" align="right">
          <template #default="{ row }">{{ formatMoney(row.lineTotal, row.currency) }}</template>
        </el-table-column>
        <el-table-column v-if="canViewAmount" label="折算美金单价" width="160" align="right">
          <template #default="{ row }">{{ row.usdUnitPrice != null ? `$${Number(row.usdUnitPrice).toFixed(6)}` : '—' }}</template>
        </el-table-column>
        <el-table-column v-if="canViewAmount" label="折算美金总额" width="160" align="right">
          <template #default="{ row }">{{ row.usdLineTotal != null ? `$${Number(row.usdLineTotal).toFixed(2)}` : '—' }}</template>
        </el-table-column>
        <el-table-column label="创建时间" width="160">
          <template #default="{ row }">{{ formatDt(row.createTime || row.orderCreateTime) }}</template>
        </el-table-column>
        <el-table-column label="创建人" width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ row.createUserName || row.createdBy || row.salesUserName || '—' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="280" fixed="right" align="center" class-name="op-col" label-class-name="op-col">
          <template #default="{ row }">
            <div @click.stop @dblclick.stop>
              <div class="action-btns">
                <el-button link type="primary" size="small" @click.stop="goDetail(row)">详情</el-button>
                <el-button v-if="canWriteSo" link type="primary" size="small" @click.stop="goEdit(row)">编辑</el-button>
                <el-button v-if="canPurchaseReq" link type="warning" size="small" @click.stop="applyPurchaseOne(row)">申请采购</el-button>
                <el-button v-if="canWriteSo" link type="warning" size="small" @click.stop="applyStockOutOne(row)">申请出库</el-button>
              </div>
            </div>
          </template>
        </el-table-column>
    </CrmDataTable>

    <div v-if="total > 0" class="pagination-wrapper">
      <el-pagination
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[10, 20, 50]"
        layout="total, prev, pager, next, sizes"
        class="quantum-pagination"
        @current-change="loadList"
        @size-change="loadList"
      />
    </div>

    <!-- 新建采购申请弹窗 -->
    <el-dialog v-model="applyDialogVisible" title="新建采购申请" width="720px" destroy-on-close>
      <el-form ref="applyFormRef" :model="applyForm" :rules="applyRules" label-width="140px" v-loading="applyLoading">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="物料型号">
              <el-input v-model="applyForm.pn" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="品牌">
              <el-input v-model="applyForm.brand" disabled />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="订单明细数量">
              <el-input :model-value="applyFormSalesOrderQtyText" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="可申请数量">
              <el-input :model-value="applyFormRemainingQtyText" disabled />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="本次申请数量" prop="requestQty">
              <el-input-number
                v-model="applyForm.requestQty"
                :min="0"
                :precision="0"
                :step="1"
                :max="applyForm.remainingQty"
                controls-position="right"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="预计采购日期" prop="expectedPurchaseDate">
              <el-date-picker
                v-model="applyForm.expectedPurchaseDate"
                type="date"
                placeholder="请选择预计采购日期"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="备注">
          <el-input v-model="applyForm.remark" type="textarea" rows="3" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="applyDialogVisible = false">取消</el-button>
          <el-button type="primary" :loading="applySubmitting" @click="submitApply" :disabled="applyLoading">
            确认
          </el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import salesOrderApi from '@/api/salesOrder'
import purchaseRequisitionApi from '@/api/purchaseRequisition'
import { runSaveTask, validateElFormOrWarn } from '@/composables/useFormSubmit'
import { salesOrderStatusText, salesOrderStatusTagType } from '@/constants/salesOrderStatus'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()
const authStore = useAuthStore()
const canViewCustomer = computed(() => authStore.hasPermission('customer.info.read'))
const canViewAmount = computed(() => authStore.hasPermission('sales.amount.read'))
const canWriteSo = computed(() => authStore.hasPermission('sales-order.write'))
const canPurchaseReq = computed(() => authStore.hasPermission('purchase-requisition.write'))

const loading = ref(false)
const list = ref<any[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const tableRef = ref()
const selectedRows = ref<any[]>([])

const dateRange = ref<[string, string] | null>(null)
const filters = reactive({
  customerName: '',
  salesUserName: '',
  pn: ''
})

// ==============================
// 新建采购申请弹窗
// ==============================
const applyDialogVisible = ref(false)
const applyLoading = ref(false)
const applySubmitting = ref(false)
const applyFormRef = ref<FormInstance>()
const applyForm = reactive({
  sellOrderItemId: '' as string,
  pn: '',
  brand: '',
  salesOrderQty: 0,
  remainingQty: 0,
  requestQty: 0,
  expectedPurchaseDate: '' as string,
  remark: ''
})
const applyRules: FormRules = {
  requestQty: [
    { required: true, message: '请输入本次申请数量', trigger: 'change' }
  ],
  expectedPurchaseDate: [{ required: true, message: '请选择预计采购日期', trigger: 'change' }]
}

const applyFormReset = () => {
  applyForm.sellOrderItemId = ''
  applyForm.pn = ''
  applyForm.brand = ''
  applyForm.salesOrderQty = 0
  applyForm.remainingQty = 0
  applyForm.requestQty = 0
  applyForm.remark = ''
  applyForm.expectedPurchaseDate = new Date().toISOString().slice(0, 10)
}

const submitApply = async () => {
  if (!applyFormRef.value) return
  const ok = await validateElFormOrWarn(applyFormRef)
  if (!ok) return

  // 附加校验：不能超过可申请数量
  if (applyForm.requestQty <= 0) {
    ElMessage.warning('本次申请数量必须大于 0')
    return
  }
  if (applyForm.requestQty > applyForm.remainingQty) {
    ElMessage.warning('本次申请数量不能大于可申请数量')
    return
  }
  if (!applyForm.expectedPurchaseDate) {
    ElMessage.warning('请选择预计采购日期')
    return
  }

  const created = await runSaveTask({
    loading: applySubmitting,
    task: async () => {
      const expectedPurchaseTime = `${applyForm.expectedPurchaseDate}T00:00:00.000Z`
      return purchaseRequisitionApi.create({
        sellOrderItemId: applyForm.sellOrderItemId,
        qty: applyForm.requestQty,
        expectedPurchaseTime,
        type: 0, // 0=专属；该弹窗不做类型选择
        remark: applyForm.remark || undefined
      })
    },
    formatSuccess: () => '采购申请已创建',
    errorMessage: (e: unknown) => {
      const err = e as { response?: { data?: { message?: string } }; message?: string }
      return err?.response?.data?.message || err?.message || '创建失败'
    }
  })
  if (!created) return
  applyDialogVisible.value = false
  await loadList()
}

async function applyPurchaseOne(row: any) {
  applyFormReset()
  applyDialogVisible.value = true
  try {
    const sellOrderId = row.sellOrderId as string
    const sellOrderItemId = row.sellOrderItemId as string

    const options = await purchaseRequisitionApi.getLineOptions(sellOrderId)
    const line = (options || []).find((x: any) => x.sellOrderItemId === sellOrderItemId)

    applyForm.sellOrderItemId = sellOrderItemId
    applyForm.pn = line?.pn ?? row.pn ?? ''
    applyForm.brand = line?.brand ?? row.brand ?? ''
    const toInt = (v: unknown) => Math.trunc(Number(v) || 0)
    applyForm.salesOrderQty = toInt(line?.salesOrderQty ?? row.qty ?? 0)
    applyForm.remainingQty = toInt(line?.remainingQty ?? row.qty ?? 0)
    applyForm.requestQty = applyForm.remainingQty
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.message || e?.message || '加载明细失败')
    applyDialogVisible.value = false
  }
}

// 将数字转为截图那种“输入框字符串效果”
const applyFormSalesOrderQtyText = computed(() => String(Math.trunc(Number(applyForm.salesOrderQty ?? 0) || 0)))
const applyFormRemainingQtyText = computed(() => String(Math.trunc(Number(applyForm.remainingQty ?? 0) || 0)))

function statusText(s: number) {
  return salesOrderStatusText(s)
}

function statusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  return salesOrderStatusTagType(s) as '' | 'success' | 'warning' | 'info' | 'danger'
}

function formatDt(v: string) {
  if (!v) return '—'
  const s = formatDisplayDateTime(v)
  return s === '--' ? '—' : s
}

function formatMoney(n: number, currency?: number) {
  const sym = currency === 2 ? '$' : currency === 3 ? '€' : '¥'
  return `${sym}${Number(n || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 6 })}`
}

function onSelectionChange(rows: any[]) {
  selectedRows.value = rows
}

async function loadList() {
  loading.value = true
  try {
    const params: Record<string, unknown> = {
      page: page.value,
      pageSize: pageSize.value
    }
    if (dateRange.value?.[0]) params.orderCreateStart = dateRange.value[0]
    if (dateRange.value?.[1]) params.orderCreateEnd = dateRange.value[1]
    if (filters.customerName.trim()) params.customerName = filters.customerName.trim()
    if (filters.salesUserName.trim()) params.salesUserName = filters.salesUserName.trim()
    if (filters.pn.trim()) params.pn = filters.pn.trim()

    const data = await salesOrderApi.getItemLines(params)
    list.value = data?.items ?? []
    total.value = data?.total ?? 0
  } catch (e: any) {
    ElMessage.error(e?.message || '加载失败')
  } finally {
    loading.value = false
  }
}

function resetFilters() {
  dateRange.value = null
  filters.customerName = ''
  filters.salesUserName = ''
  filters.pn = ''
  page.value = 1
  tableRef.value?.clearSelection()
  loadList()
}

function goDetail(row: any) {
  router.push({ name: 'SalesOrderDetail', params: { id: row.sellOrderId } })
}

function goEdit(row: any) {
  router.push({ path: `/sales-orders/${row.sellOrderId}`, query: { edit: '1' } })
}

function navigateNewPr(sellOrderId: string, itemIds: string[]) {
  const q: Record<string, string> = { sellOrderId }
  if (itemIds.length) q.itemIds = itemIds.join(',')
  router.push({ path: '/purchase-requisitions/new', query: q })
}

function batchApplyPurchase() {
  const rows = selectedRows.value
  if (!rows.length) {
    ElMessage.warning('请先勾选销售订单明细')
    return
  }
  if (rows.length === 1) {
    // 1条时走弹窗，避免跳到可能未完善的路由页面
    applyPurchaseOne(rows[0])
    return
  }

  ElMessage.warning('批量申请采购暂未改造成弹窗，请逐条使用“申请采购”')
  return

  const orderIds = new Set(rows.map((r) => r.sellOrderId))
  if (orderIds.size !== 1) {
    ElMessage.warning('批量申请采购仅支持同一销售订单下的明细，请分次操作')
    return
  }
  if (canViewCustomer.value) {
    const cids = rows.map((r) => r.customerId).filter(Boolean)
    const names = rows.map((r) => r.customerName).filter(Boolean)
    if (cids.length === rows.length) {
      if (!cids.every((id) => id === cids[0])) {
        ElMessage.warning('所选明细必须属于同一客户')
        return
      }
    } else if (names.length === rows.length) {
      if (!names.every((n) => n === names[0])) {
        ElMessage.warning('所选明细必须属于同一客户')
        return
      }
    }
  }
  navigateNewPr(rows[0].sellOrderId, rows.map((r) => r.sellOrderItemId))
}

function applyStockOutOne(row: any) {
  router.push({
    path: `/sales-orders/${row.sellOrderId}`,
    query: { applyStockOut: '1' }
  })
}

onMounted(() => loadList())
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.so-item-list-page {
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
  gap: 12px;
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
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0;
}
.list-count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}
.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  cursor: pointer;
  &:disabled {
    opacity: 0.45;
    cursor: not-allowed;
  }
  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}
.btn-ghost {
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  cursor: pointer;
}
.search-bar {
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
}
.search-input {
  padding: 7px 12px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  outline: none;
}
.so-date-range {
  width: 260px;
}
.so-filter-input {
  width: 160px;
}
.table-wrapper {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}
.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}
</style>
