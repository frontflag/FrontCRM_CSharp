<template>
  <div class="po-item-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" />
            </svg>
          </div>
          <h1 class="page-title">采购订单明细</h1>
        </div>
        <div class="list-count-badge">共 {{ total }} 条</div>
      </div>
      <div class="header-right">
        <!-- 采购订单明细目前仅展示；如后续需要批量操作，可在此扩展 -->
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
          class="po-date-range"
          clearable
        />

        <input
          v-if="canViewVendor"
          v-model="filters.vendorName"
          class="search-input po-filter-input"
          placeholder="供应商名称"
          @keyup.enter="loadList"
        />
        <input
          v-if="canViewPurchaseUser"
          v-model="filters.purchaseUserName"
          class="search-input po-filter-input"
          placeholder="采购员名称"
          @keyup.enter="loadList"
        />
        <input v-model="filters.pn" class="search-input po-filter-input" placeholder="物料型号" @keyup.enter="loadList" />

        <button type="button" class="btn-primary btn-sm" @click="loadList">查询</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">重置</button>
      </div>
    </div>

    <CrmDataTable
      ref="tableRef"
      class="quantum-table-block el-table-host"
      :data="pagedList"
      v-loading="loading"
      row-key="purchaseOrderItemId"
      @selection-change="onSelectionChange"
    >
      <el-table-column type="selection" width="48" :reserve-selection="true" />

      <el-table-column prop="purchaseOrderCode" label="采购单号" width="160" min-width="160" show-overflow-tooltip />
      <el-table-column prop="itemStatus" label="状态" width="160" align="center">
        <template #default="{ row }">
          <el-tag effect="dark" :type="statusTagType(row.itemStatus)" size="small">{{ statusText(row.itemStatus) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="orderCreateTime" label="订单生成日期" width="160">
        <template #default="{ row }">{{ formatDt(row.orderCreateTime) }}</template>
      </el-table-column>

      <el-table-column v-if="canViewVendor" prop="vendorName" label="供应商名称" min-width="200" show-overflow-tooltip />
      <el-table-column v-if="canViewPurchaseUser" prop="purchaseUserName" label="采购员" width="100" show-overflow-tooltip />
      <el-table-column prop="pn" label="物料型号" min-width="130" show-overflow-tooltip />
      <el-table-column prop="brand" label="品牌" width="110" show-overflow-tooltip />

      <el-table-column prop="qty" label="数量" width="100" align="right" />
      <el-table-column prop="financePaymentStatus" label="付款状态" width="120" align="center">
        <template #default="{ row }">
          <el-tag effect="dark" size="small" :type="financeStatusTagType(Number(row.financePaymentStatus ?? 0))">
            {{ financeStatusText(Number(row.financePaymentStatus ?? 0)) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="stockInStatus" label="货运状态" width="120" align="center">
        <template #default="{ row }">
          <el-tag effect="dark" size="small" :type="shippingStatusTagType(Number(row.stockInStatus ?? 0))">
            {{ shippingStatusText(Number(row.stockInStatus ?? 0)) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column v-if="canViewAmount" prop="cost" label="单价" width="160" align="right">
        <template #default="{ row }">{{ formatMoney(row.cost, row.currency) }}</template>
      </el-table-column>
      <el-table-column v-if="canViewAmount" prop="lineTotal" label="明细总额" width="160" align="right">
        <template #default="{ row }">{{ formatMoney(row.lineTotal, row.currency) }}</template>
      </el-table-column>
      <el-table-column label="创建时间" width="160">
        <template #default="{ row }">{{ formatDt(row.createTime || row.orderCreateTime) }}</template>
      </el-table-column>
      <el-table-column label="创建人" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ row.createUserName || row.createdBy || row.purchaseUserName || '—' }}</template>
      </el-table-column>

      <el-table-column label="操作" width="260" fixed="right" align="center" class-name="op-col" label-class-name="op-col-head">
        <template #default="{ row }">
          <el-button link type="primary" size="small" @click="goDetail(row)">详情</el-button>
          <el-button
            v-if="row.itemStatus === 30 && canCreateArrivalNotice"
            link
            type="warning"
            size="small"
            @click="openArrivalDialog(row)"
          >
            通知到货
          </el-button>
          <el-button
            v-if="row.canApplyPayment"
            link
            type="success"
            size="small"
            @click="openPaymentDialog(row)"
          >
            申请付款
          </el-button>
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
        @current-change="onPageChange"
        @size-change="onPageSizeChange"
      />
    </div>

    <el-dialog
      v-model="paymentDialogVisible"
      title="申请付款窗口"
      width="980px"
      destroy-on-close
      class="payment-dialog"
    >
      <el-form label-width="120px">
        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="供应商信息">
              <el-input :model-value="paymentForm.vendorName || '--'" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="采购员">
              <el-input :model-value="paymentForm.purchaseUserName || '--'" disabled />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="供应商银行" required>
              <el-select v-model="paymentForm.vendorBankId" placeholder="请选择供应商银行" style="width: 100%">
                <el-option label="中国银行" value="bank-boc" />
                <el-option label="工商银行" value="bank-icbc" />
                <el-option label="建设银行" value="bank-ccb" />
                <el-option label="农业银行" value="bank-abc" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="请款方式" required>
              <el-select v-model="paymentForm.paymentMode" style="width: 100%">
                <el-option label="银行转账" :value="1" />
                <el-option label="现金" :value="2" />
                <el-option label="支票" :value="3" />
                <el-option label="承兑汇票" :value="4" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="请款备注">
          <el-input v-model="paymentForm.remark" type="textarea" :rows="2" />
        </el-form-item>

        <div class="section-title">费用明细</div>
        <el-row :gutter="12">
          <el-col :span="8">
            <el-form-item label="中转行费用">
              <el-input-number v-model="paymentForm.fee.intermediateBankFee" :min="0" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="银行手续费">
              <el-input-number v-model="paymentForm.fee.bankCharge" :min="0" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="运费">
              <el-input-number v-model="paymentForm.fee.freight" :min="0" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="杂费">
              <el-input-number v-model="paymentForm.fee.miscFee" :min="0" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="尾差">
              <el-input-number v-model="paymentForm.fee.rounding" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="费用承担方">
              <el-radio-group v-model="paymentForm.fee.intermediateBankFeePayer">
                <el-radio label="我方">我方</el-radio>
                <el-radio label="供应商">供应商</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>

        <div class="section-title">订单明细列表</div>
        <CrmDataTable :data="paymentForm.lines" size="small">
          <el-table-column prop="purchaseOrderCode" label="采购单号" width="160" min-width="160" show-overflow-tooltip />
          <el-table-column prop="pn" label="型号" min-width="120" />
          <el-table-column prop="brand" label="品牌" width="100" />
          <el-table-column prop="qty" label="数量" width="90" align="right" />
          <el-table-column prop="cost" label="单价" width="160" align="right">
            <template #default="{ row }">{{ formatMoney(row.cost, row.currency) }}</template>
          </el-table-column>
          <el-table-column prop="alreadyRequested" label="已请款" width="160" align="right">
            <template #default="{ row }">{{ formatMoney(row.alreadyRequested, row.currency) }}</template>
          </el-table-column>
          <el-table-column prop="pendingRequested" label="待请款" width="160" align="right">
            <template #default="{ row }">{{ formatMoney(row.pendingRequested, row.currency) }}</template>
          </el-table-column>
          <el-table-column label="本次请款金额*" width="150">
            <template #default="{ row }">
              <el-input-number v-model="row.requestAmount" :min="0" :max="row.pendingRequested" :precision="2" style="width: 130px" />
            </template>
          </el-table-column>
          <el-table-column label="备注" min-width="140">
            <template #default="{ row }">
              <el-input v-model="row.remark" />
            </template>
          </el-table-column>
        </CrmDataTable>

        <el-alert :closable="false" type="info" style="margin-top: 8px">
          <template #title>
            合计：请款总额 {{ formatMoney(paymentTotalAmount, paymentForm.currency) }}
          </template>
        </el-alert>
      </el-form>

      <template #footer>
        <el-button @click="paymentDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="paymentSubmitting" @click="submitPayment()">提交审批</el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="arrivalDialogVisible"
      title="新建到货通知"
      width="1180px"
      align-center
      destroy-on-close
    >
      <div class="arrival-form-layout">

        <div class="arrival-section">
          <el-form label-width="90px">
            <el-row :gutter="12">
              <el-col :span="8"><el-form-item label="单号"><el-input v-model="arrivalForm.purchaseOrderCode" /></el-form-item></el-col>
              <el-col :span="8">
                <el-form-item label="预计到货日期" required>
                  <el-date-picker
                    v-model="arrivalForm.expectedArrivalDate"
                    type="date"
                    value-format="YYYY-MM-DD"
                    placeholder="选择预计到货日期"
                    style="width: 100%"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="8"><el-form-item label="公司名称"><el-input v-model="arrivalForm.companyName" /></el-form-item></el-col>
            </el-row>
            <el-row :gutter="12">
              <el-col :span="8"><el-form-item label="地址"><el-input v-model="arrivalForm.address" /></el-form-item></el-col>
              <el-col :span="8"><el-form-item label="电话"><el-input v-model="arrivalForm.phone" /></el-form-item></el-col>
              <el-col :span="8"><el-form-item label="联系人"><el-input v-model="arrivalForm.contact" /></el-form-item></el-col>
            </el-row>
            <el-row :gutter="12">
              <el-col :span="8"><el-form-item label="来货方式"><el-input v-model="arrivalForm.arrivalMethod" /></el-form-item></el-col>
              <el-col :span="8"><el-form-item label="快递方式"><el-input v-model="arrivalForm.expressMethod" /></el-form-item></el-col>
              <el-col :span="8"><el-form-item label="快递单号"><el-input v-model="arrivalForm.expressNo" /></el-form-item></el-col>
            </el-row>
          </el-form>
        </div>

        <div class="arrival-section">
          <div class="section-title">来货明细</div>
          <CrmDataTable :data="arrivalForm.lines" size="small">
            <el-table-column label="序号" width="70">
              <template #default="{ $index }">{{ $index + 1 }}</template>
            </el-table-column>
            <el-table-column label="原厂型号" min-width="180">
              <template #default="{ row }"><el-input v-model="row.pn" /></template>
            </el-table-column>
            <el-table-column label="品牌" width="120">
              <template #default="{ row }"><el-input v-model="row.brand" /></template>
            </el-table-column>
            <el-table-column label="数量" min-width="168" align="right">
              <template #default="{ row }">
                <el-input-number
                  v-model="row.qty"
                  :min="0"
                  :precision="4"
                  class="arrival-qty-input"
                  controls-position="right"
                />
              </template>
            </el-table-column>
            <el-table-column label="规格参数" min-width="130">
              <template #default="{ row }"><el-input v-model="row.spec" /></template>
            </el-table-column>
            <el-table-column label="包装" width="120">
              <template #default="{ row }"><el-input v-model="row.packaging" /></template>
            </el-table-column>
          </CrmDataTable>
        </div>

        <div class="arrival-section">
          <el-form label-width="90px">
            <el-form-item label="验货要求"><el-input v-model="arrivalForm.inspectionRequirement" /></el-form-item>
            <el-form-item label="备注"><el-input v-model="arrivalForm.remark" type="textarea" :rows="2" /></el-form-item>
          </el-form>
        </div>

        <!-- 新建到货通知不展示签收/质检/入库；后续若支持编辑已存在通知可改为 v-if="arrivalNoticeShowProcessFields" -->
        <div v-if="arrivalNoticeShowProcessFields" class="arrival-section">
          <el-form label-width="90px">
            <el-row :gutter="12">
              <el-col :span="6"><el-form-item label="签收人"><el-input v-model="arrivalForm.signer" /></el-form-item></el-col>
              <el-col :span="6"><el-form-item label="签收日期"><el-date-picker v-model="arrivalForm.signDate" type="date" value-format="YYYY-MM-DD" style="width:100%" /></el-form-item></el-col>
              <el-col :span="6"><el-form-item label="质检员"><el-input v-model="arrivalForm.qcUser" /></el-form-item></el-col>
              <el-col :span="6"><el-form-item label="质检日期"><el-date-picker v-model="arrivalForm.qcDate" type="date" value-format="YYYY-MM-DD" style="width:100%" /></el-form-item></el-col>
            </el-row>
            <el-row :gutter="12">
              <el-col :span="6"><el-form-item label="入库人"><el-input v-model="arrivalForm.stockInUser" /></el-form-item></el-col>
              <el-col :span="6"><el-form-item label="入库日期"><el-date-picker v-model="arrivalForm.stockInDate" type="date" value-format="YYYY-MM-DD" style="width:100%" /></el-form-item></el-col>
            </el-row>
          </el-form>
        </div>
      </div>
      <template #footer>
        <el-button @click="arrivalDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="arrivalSubmitting" @click="submitArrivalNotice">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { financePaymentApi } from '@/api/finance'
import { logisticsApi } from '@/api/logistics'
import { ElMessage } from 'element-plus'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()
const authStore = useAuthStore()

const canViewVendor = computed(() => authStore.hasPermission('vendor.info.read'))
const canViewPurchaseUser = computed(() => authStore.hasPermission('purchase.user.read') || authStore.hasPermission('purchase-order.read'))
const canViewAmount = computed(() => authStore.hasPermission('purchase.amount.read'))
const canCreateArrivalNotice = computed(() => authStore.hasPermission('purchase-order.read'))

const loading = ref(false)
const allLines = ref<any[]>([])
const pagedList = ref<any[]>([])

const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const tableRef = ref<any>(null)
const selectedRows = ref<any[]>([])
const paymentDialogVisible = ref(false)
const paymentSubmitting = ref(false)
const arrivalDialogVisible = ref(false)
/** 新建为 false；若以后支持「编辑到货通知」并需填写签收/质检/入库，可置为 true */
const arrivalNoticeShowProcessFields = ref(false)
const arrivalSubmitting = ref(false)
const arrivalForm = reactive<any>({
  purchaseOrderId: '',
  purchaseOrderCode: '',
  vendorName: '',
  pn: '',
  expectedArrivalDate: '' as string,
  companyName: '',
  address: '',
  phone: '',
  contact: '',
  arrivalMethod: '',
  expressMethod: '',
  expressNo: '',
  inspectionRequirement: '',
  remark: '',
  signer: '',
  signDate: '',
  qcUser: '',
  qcDate: '',
  stockInUser: '',
  stockInDate: '',
  lines: [] as any[]
})

const paymentForm = reactive<any>({
  vendorId: '',
  vendorName: '',
  purchaseUserName: '',
  vendorBankId: '',
  paymentMode: 1,
  currency: 1,
  remark: '',
  fee: {
    intermediateBankFee: 0,
    bankCharge: 0,
    freight: 0,
    miscFee: 0,
    rounding: 0,
    intermediateBankFeePayer: '我方'
  },
  lines: [] as any[]
})

const paymentTotalAmount = computed(() => {
  const linesTotal = paymentForm.lines.reduce((sum: number, line: any) => sum + Number(line.requestAmount || 0), 0)
  const fee = paymentForm.fee
  const feeTotal = Number(fee.intermediateBankFee || 0) + Number(fee.bankCharge || 0) + Number(fee.freight || 0) + Number(fee.miscFee || 0) + Number(fee.rounding || 0)
  return Math.max(0, linesTotal + feeTotal)
})

const dateRange = ref<[string, string] | null>(null)
const filters = reactive({
  vendorName: '',
  purchaseUserName: '',
  pn: ''
})

const applyPagination = () => {
  const start = (page.value - 1) * pageSize.value
  pagedList.value = allLines.value.slice(start, start + pageSize.value)
  total.value = allLines.value.length
  // 避免分页切换后 checkbox 状态“残留”
  selectedRows.value = []
  ;(tableRef.value as any)?.clearSelection?.()
}

function statusText(s: number) {
  const m: Record<number, string> = {
    1: '新建',
    2: '待审核',
    10: '审核通过',
    20: '待确认',
    30: '已确认',
    40: '已付款',
    50: '已发货',
    60: '已入库',
    100: '采购完成',
    [-1]: '审核失败',
    [-2]: '取消'
  }
  return m[s] ?? String(s)
}

function statusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' | 'primary' {
  const map: Record<number, '' | 'success' | 'warning' | 'info' | 'danger' | 'primary'> = {
    1: 'info',
    2: 'warning',
    10: 'success',
    20: 'warning',
    30: 'primary',
    40: 'primary',
    50: 'warning',
    60: 'success',
    100: 'success',
    [-1]: 'danger',
    [-2]: 'info'
  }
  return map[s] ?? 'info'
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

function financeStatusText(s: number) {
  const map: Record<number, string> = {
    0: '未付款',
    1: '部分付款',
    2: '全部付款'
  }
  return map[s] ?? String(s)
}

function financeStatusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' | 'primary' {
  const map: Record<number, '' | 'success' | 'warning' | 'info' | 'danger' | 'primary'> = {
    0: 'info',
    1: 'warning',
    2: 'success'
  }
  return map[s] ?? 'info'
}

function shippingStatusText(s: number) {
  const map: Record<number, string> = {
    0: '未到货',
    1: '部分到货',
    2: '全部到货'
  }
  return map[s] ?? String(s)
}

function shippingStatusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' | 'primary' {
  const map: Record<number, '' | 'success' | 'warning' | 'info' | 'danger' | 'primary'> = {
    0: 'info',
    1: 'warning',
    2: 'success'
  }
  return map[s] ?? 'info'
}

function buildFinancePaymentCode() {
  const d = new Date()
  const yy = String(d.getFullYear()).slice(-2)
  const MM = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  const HH = String(d.getHours()).padStart(2, '0')
  const mm = String(d.getMinutes()).padStart(2, '0')
  const ss = String(d.getSeconds()).padStart(2, '0')
  const rand = String(Math.floor(Math.random() * 100)).padStart(2, '0')
  // FP + yymmddHHmmss + 2位随机数 = 16位
  return `FP${yy}${MM}${dd}${HH}${mm}${ss}${rand}`
}

function openPaymentDialog(row: any) {
  paymentForm.vendorId = row.vendorId || ''
  paymentForm.vendorName = row.vendorName || ''
  paymentForm.purchaseUserName = row.purchaseUserName || ''
  paymentForm.vendorBankId = ''
  paymentForm.paymentMode = 1
  paymentForm.currency = row.currency || 1
  paymentForm.remark = ''
  paymentForm.fee = { intermediateBankFee: 0, bankCharge: 0, freight: 0, miscFee: 0, rounding: 0, intermediateBankFeePayer: '我方' }
  paymentForm.lines = [{
    purchaseOrderId: row.purchaseOrderId,
    purchaseOrderItemId: row.purchaseOrderItemId,
    purchaseOrderCode: row.purchaseOrderCode,
    pn: row.pn,
    brand: row.brand,
    qty: row.qty,
    cost: row.cost,
    currency: row.currency,
    alreadyRequested: 0,
    pendingRequested: Number(row.lineTotal || 0),
    requestAmount: Number(row.lineTotal || 0),
    remark: ''
  }]
  paymentDialogVisible.value = true
}

function openArrivalDialog(row: any) {
  arrivalNoticeShowProcessFields.value = false
  arrivalForm.purchaseOrderId = row.purchaseOrderId || ''
  arrivalForm.purchaseOrderCode = row.purchaseOrderCode || ''
  arrivalForm.vendorName = row.vendorName || ''
  arrivalForm.pn = row.pn || ''
  arrivalForm.expectedArrivalDate = toDatePickerValue(row.deliveryDate)
  arrivalForm.companyName = row.vendorName || ''
  arrivalForm.address = ''
  arrivalForm.phone = ''
  arrivalForm.contact = ''
  arrivalForm.arrivalMethod = ''
  arrivalForm.expressMethod = ''
  arrivalForm.expressNo = ''
  arrivalForm.inspectionRequirement = ''
  arrivalForm.remark = ''
  arrivalForm.signer = ''
  arrivalForm.signDate = ''
  arrivalForm.qcUser = ''
  arrivalForm.qcDate = ''
  arrivalForm.stockInUser = ''
  arrivalForm.stockInDate = ''
  arrivalForm.lines = [{
    pn: row.pn || '',
    brand: row.brand || '',
    qty: row.qty || 0,
    spec: '',
    packaging: ''
  }]
  arrivalDialogVisible.value = true
}

function toDatePickerValue(v: unknown): string {
  if (v == null || v === '') return ''
  const s = String(v)
  const m = s.match(/^(\d{4}-\d{2}-\d{2})/)
  if (m) return m[1]
  const d = formatDisplayDate(s)
  return d === '--' ? '' : d
}

async function submitArrivalNotice() {
  if (arrivalSubmitting.value) return
  if (!arrivalForm.purchaseOrderId) {
    ElMessage.warning('缺少采购订单ID，无法创建到货通知')
    return
  }
  if (!arrivalForm.expectedArrivalDate) {
    ElMessage.warning('请填写预计到货日期')
    return
  }
  arrivalSubmitting.value = true
  try {
    await logisticsApi.createArrivalNotice({
      purchaseOrderId: arrivalForm.purchaseOrderId,
      expectedArrivalDate: arrivalForm.expectedArrivalDate
    })
    ElMessage.success('到货通知已创建')
    arrivalDialogVisible.value = false
  } catch (error: any) {
    ElMessage.error(error?.message || '创建到货通知失败')
  } finally {
    arrivalSubmitting.value = false
  }
}

async function submitPayment() {
  if (paymentSubmitting.value) {
    return
  }

  if (!paymentForm.vendorId) {
    ElMessage.warning('缺少供应商ID，无法创建请款单')
    return
  }
  if (!paymentForm.vendorBankId) {
    ElMessage.warning('请选择供应商银行')
    return
  }
  if (!paymentForm.lines.length || paymentForm.lines.some((x: any) => Number(x.requestAmount || 0) <= 0)) {
    ElMessage.warning('请填写本次请款金额，且必须大于0')
    return
  }

  const lineRemark = paymentForm.lines
    .filter((x: any) => x.remark)
    .map((x: any) => `${x.pn || x.purchaseOrderCode}:${x.remark}`)
    .join('; ')
  const extRemark = [
    paymentForm.remark || '',
    `供应商银行:${paymentForm.vendorBankId}`,
    `费用(中转/手续费/运费/杂费/尾差):${paymentForm.fee.intermediateBankFee}/${paymentForm.fee.bankCharge}/${paymentForm.fee.freight}/${paymentForm.fee.miscFee}/${paymentForm.fee.rounding}`,
    `中转行费用承担方:${paymentForm.fee.intermediateBankFeePayer}`,
    lineRemark ? `明细备注:${lineRemark}` : ''
  ].filter(Boolean).join(' | ')

  paymentSubmitting.value = true
  try {
    const created = await financePaymentApi.create({
      financePaymentCode: buildFinancePaymentCode(),
      vendorId: paymentForm.vendorId,
      vendorName: paymentForm.vendorName,
      paymentMode: paymentForm.paymentMode,
      paymentCurrency: paymentForm.currency,
      paymentAmountToBe: paymentTotalAmount.value,
      remark: extRemark,
      items: paymentForm.lines.map((line: any) => ({
        purchaseOrderId: line.purchaseOrderId,
        purchaseOrderItemId: line.purchaseOrderItemId,
        paymentAmountToBe: Number(line.requestAmount || 0),
        pn: line.pn,
        brand: line.brand
      }))
    })

    // 接口返回可能是 data 或直接对象，做兼容解析
    const paymentId = (created as any)?.id || (created as any)?.data?.id || (created as any)?.data?.data?.id
    if (!paymentId) {
      throw new Error('创建请款单成功，但未获取到单据ID')
    }

    await financePaymentApi.updateStatus(paymentId, 2)
    ElMessage.success('请款单已提交审批')
    paymentDialogVisible.value = false
  } catch (error: any) {
    ElMessage.error(error?.message || '提交审批失败，请稍后重试')
  } finally {
    paymentSubmitting.value = false
  }
}

function onSelectionChange(rows: any[]) {
  selectedRows.value = rows
}

async function loadList() {
  loading.value = true
  try {
    const params: Record<string, unknown> = {
      page: 1,
      pageSize: 2000
    }
    if (dateRange.value?.[0]) params.startDate = dateRange.value[0]
    if (dateRange.value?.[1]) params.endDate = dateRange.value[1]

    const res = await purchaseOrderApi.getList(params)
    const orders = (res as { items?: any[] } | undefined)?.items ?? []

    // 列表接口通常不带完整明细，逐单拉详情后再展开明细行
    const detailResults = await Promise.allSettled(
      orders
        .filter((o: any) => !!o?.id)
        .map((o: any) => purchaseOrderApi.getById(o.id))
    )
    const detailMap = new Map<string, any>()
    detailResults.forEach((result) => {
      if (result.status !== 'fulfilled') return
      const detail = result.value as any
      if (!detail?.id) return
      detailMap.set(detail.id, detail)
    })

    const pnK = filters.pn.trim().toLowerCase()
    const vendorK = filters.vendorName.trim().toLowerCase()
    const purchaseUserK = filters.purchaseUserName.trim().toLowerCase()

    const lines = orders.flatMap((o: any) => {
      const detail = detailMap.get(o.id) ?? o
      const items = detail?.items ?? []
      return items.map((it: any) => ({
        purchaseOrderItemId: it.id,
        purchaseOrderId: detail.id ?? o.id,
        purchaseOrderCode: detail.purchaseOrderCode ?? o.purchaseOrderCode,
        vendorId: detail.vendorId ?? o.vendorId,
        itemStatus: it.status,
        stockInStatus: it.stockInStatus ?? 0,
        financePaymentStatus: it.financePaymentStatus ?? 0,
        canApplyPayment: Boolean(it.canApplyPayment ?? it.CanApplyPayment ?? false),
        orderCreateTime: detail.createTime ?? o.createTime,
        vendorName: detail.vendorName ?? o.vendorName,
        purchaseUserName: detail.purchaseUserName ?? o.purchaseUserName,
        pn: it.pn,
        brand: it.brand,
        qty: it.qty,
        cost: it.cost,
        lineTotal: (it.qty || 0) * (it.cost || 0),
        currency: it.currency ?? detail.currency ?? o.currency,
        deliveryDate: it.deliveryDate ?? detail.deliveryDate
      }))
    })

    // 客户/业务员/物料型号等在前端做过滤（后端采购订单 lines 分页接口尚未补齐）
    allLines.value = lines.filter((x: any) => {
      if (pnK && !String(x.pn || '').toLowerCase().includes(pnK)) return false
      if (canViewVendor.value && vendorK && !String(x.vendorName || '').toLowerCase().includes(vendorK)) return false
      if (canViewPurchaseUser.value && purchaseUserK && !String(x.purchaseUserName || '').toLowerCase().includes(purchaseUserK)) return false
      return true
    })

    // 查询后回到第一页
    page.value = 1
    applyPagination()
  } catch (e: any) {
    // eslint-disable-next-line no-console
    console.error(e)
  } finally {
    loading.value = false
  }
}

function resetFilters() {
  dateRange.value = null
  filters.vendorName = ''
  filters.purchaseUserName = ''
  filters.pn = ''
  page.value = 1
  loadList()
}

function onPageChange(nextPage: number) {
  page.value = nextPage
  applyPagination()
}

function onPageSizeChange(nextSize: number) {
  pageSize.value = nextSize
  page.value = 1
  applyPagination()
}

function goDetail(row: any) {
  router.push({ name: 'PurchaseOrderDetail', params: { id: row.purchaseOrderId } })
}

onMounted(() => {
  loadList()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.po-item-list-page {
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
.po-date-range {
  width: 260px;
}
.po-filter-input {
  width: 160px;
}
.table-wrapper {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

/* 右侧固定操作列浮层：全层级强制不透明，覆盖全局透明表格样式 */
.po-item-list-page :deep(.el-table__fixed-right),
.po-item-list-page :deep(.el-table__fixed-right-patch),
.po-item-list-page :deep(.el-table__fixed-right .el-table__fixed-header-wrapper),
.po-item-list-page :deep(.el-table__fixed-right .el-table__fixed-body-wrapper),
.po-item-list-page :deep(.el-table__fixed-right .el-table__fixed-footer-wrapper),
.po-item-list-page :deep(.el-table__fixed-right table),
.po-item-list-page :deep(.el-table__fixed-right tr),
.po-item-list-page :deep(.el-table__fixed-right th.el-table__cell),
.po-item-list-page :deep(.el-table__fixed-right td.el-table__cell),
.po-item-list-page :deep(.el-table__fixed-right .cell),
.po-item-list-page :deep(.el-table .el-table-fixed-column--right),
.po-item-list-page :deep(.el-table .el-table-fixed-column--right.is-leaf),
.po-item-list-page :deep(.el-table .el-table-fixed-column--right .cell) {
  background-color: $layer-2 !important;
}

/* fixed 列 hover 时也保持不透明，避免出现“透视缝隙” */
.po-item-list-page :deep(.el-table__fixed-right .el-table__row:hover td.el-table__cell),
.po-item-list-page :deep(.el-table__fixed-right .el-table__row.hover-row td.el-table__cell) {
  background-color: $layer-2 !important;
}

/* 固定列与普通列边界：左侧阴影 + 细分隔线 */
.po-item-list-page :deep(.el-table__fixed-right) {
  box-shadow: -12px 0 18px -10px rgba(0, 0, 0, 0.72) !important;
}
.po-item-list-page :deep(.el-table__fixed-right::before),
.po-item-list-page :deep(.el-table__fixed-right-patch) {
  background-color: rgba(255, 255, 255, 0.08) !important;
}
.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}

.arrival-form-layout {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.arrival-section {
  border: 1px solid $border-panel;
  border-radius: 8px;
  padding: 12px;
  background: rgba(255, 255, 255, 0.02);
}
.section-title {
  font-size: 20px;
  margin-bottom: 8px;
  color: $text-primary;
}

/* 来货明细：数量步进器占满列宽，避免裁切 */
:deep(.arrival-qty-input) {
  width: 100%;
  box-sizing: border-box;
}
:deep(.arrival-qty-input .el-input__wrapper) {
  width: 100%;
}
</style>

