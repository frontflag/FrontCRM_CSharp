<template>
  <div class="debug-page">
    <div class="debug-header">
      <h1>Debug 工具</h1>
      <div class="debug-sub muted">
        订单刷新与修复工具，需登录后使用。
        <router-link class="debug-link" to="/debug">返回 Debug</router-link>
      </div>
    </div>

    <section class="debug-panel panel-refresh">
      <h2 class="panel-title">刷新订单面板</h2>
      <div class="panel-body refresh-actions">
        <el-button type="primary" :loading="refreshingSalesOrders" :disabled="refreshingAny" @click="refreshAllSalesOrders">
          刷新全部销售订单
        </el-button>
        <el-button type="warning" :loading="refreshingPurchaseOrders" :disabled="refreshingAny" @click="refreshAllPurchaseOrders">
          刷新全部采购单
        </el-button>
      </div>
      <div class="refresh-hint">逐条调用订单详情页“刷新”同源接口，自动循环执行。</div>
      <div class="refresh-result-grid">
        <div class="refresh-result-card">
          <div class="refresh-result-title">销售订单刷新结果</div>
          <div class="refresh-result-line">共刷新：{{ salesRefreshResult.total }} 条</div>
          <div class="refresh-result-line">有数据变更：{{ salesRefreshResult.changed }} 条</div>
          <div class="refresh-result-line">变更单号：{{ salesRefreshResult.codesText }}</div>
          <div class="refresh-result-line">失败条数：{{ salesRefreshResult.failed }} 条（{{ salesRefreshResult.failedCodesText }}）</div>
        </div>
        <div class="refresh-result-card">
          <div class="refresh-result-title">采购订单刷新结果</div>
          <div class="refresh-result-line">共刷新：{{ purchaseRefreshResult.total }} 条</div>
          <div class="refresh-result-line">有数据变更：{{ purchaseRefreshResult.changed }} 条</div>
          <div class="refresh-result-line">变更单号：{{ purchaseRefreshResult.codesText }}</div>
          <div class="refresh-result-line">失败条数：{{ purchaseRefreshResult.failed }} 条（{{ purchaseRefreshResult.failedCodesText }}）</div>
          <div class="refresh-result-line">主状态变更：{{ purchaseRefreshResult.mainStatusChanged }} 条</div>
          <div class="refresh-result-line">主状态跳过终态：{{ purchaseRefreshResult.mainStatusSkippedTerminal }} 条</div>
          <div class="refresh-result-line">主状态变更单号：{{ purchaseRefreshResult.mainStatusCodesText }}</div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { refreshPurchaseOrderMainStatus, type RefreshPurchaseOrderMainStatusResult } from '@/api/debug'
import { salesOrderApi } from '@/api/salesOrder'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { getApiErrorMessage } from '@/utils/apiError'

const refreshingSalesOrders = ref(false)
const refreshingPurchaseOrders = ref(false)
const salesRefreshTotal = ref(0)
const salesRefreshChanged = ref(0)
const salesRefreshCodes = ref<string[]>([])
const salesRefreshFailedCodes = ref<string[]>([])
const purchaseRefreshTotal = ref(0)
const purchaseRefreshChanged = ref(0)
const purchaseRefreshCodes = ref<string[]>([])
const purchaseRefreshFailedCodes = ref<string[]>([])
const purchaseMainStatusRefreshResult = ref<RefreshPurchaseOrderMainStatusResult | null>(null)

const refreshingAny = computed(() => refreshingSalesOrders.value || refreshingPurchaseOrders.value)
const salesRefreshResult = computed(() => ({
  total: salesRefreshTotal.value,
  changed: salesRefreshChanged.value,
  codesText: salesRefreshCodes.value.length ? salesRefreshCodes.value.join('，') : '无',
  failed: salesRefreshFailedCodes.value.length,
  failedCodesText: salesRefreshFailedCodes.value.length ? salesRefreshFailedCodes.value.join('，') : '无'
}))
const purchaseRefreshResult = computed(() => ({
  total: purchaseRefreshTotal.value,
  changed: purchaseRefreshChanged.value,
  codesText: purchaseRefreshCodes.value.length ? purchaseRefreshCodes.value.join('，') : '无',
  failed: purchaseRefreshFailedCodes.value.length,
  failedCodesText: purchaseRefreshFailedCodes.value.length ? purchaseRefreshFailedCodes.value.join('，') : '无',
  mainStatusChanged: purchaseMainStatusRefreshResult.value?.changedOrders ?? 0,
  mainStatusSkippedTerminal: purchaseMainStatusRefreshResult.value?.skippedTerminalOrders ?? 0,
  mainStatusCodesText: (purchaseMainStatusRefreshResult.value?.changedOrderCodes?.length ?? 0)
    ? purchaseMainStatusRefreshResult.value!.changedOrderCodes.join('，')
    : '无'
}))

async function loadAllSalesOrders() {
  const pageSize = 200
  let page = 1
  const rows: any[] = []
  while (true) {
    const res = await salesOrderApi.getList({ page, pageSize })
    const batch = (res as { items?: any[] }).items || []
    rows.push(...batch)
    if (batch.length < pageSize) break
    page += 1
  }
  return rows
}

async function loadAllPurchaseOrders() {
  const pageSize = 200
  let page = 1
  const rows: any[] = []
  while (true) {
    const res = await purchaseOrderApi.getList({ page, pageSize })
    const batch = (res as { items?: any[] }).items || []
    rows.push(...batch)
    if (batch.length < pageSize) break
    page += 1
  }
  return rows
}

async function refreshAllSalesOrders() {
  if (refreshingAny.value) return
  refreshingSalesOrders.value = true
  salesRefreshTotal.value = 0
  salesRefreshChanged.value = 0
  salesRefreshCodes.value = []
  salesRefreshFailedCodes.value = []
  try {
    const allRows = await loadAllSalesOrders()
    const orders = allRows
      .map((x) => ({
        id: String(x.id ?? ''),
        code: String(x.sellOrderCode ?? x.code ?? x.id ?? '')
      }))
      .filter((x) => x.id)
    salesRefreshTotal.value = orders.length

    const changedCodes: string[] = []
    const failedCodes: string[] = []
    for (const order of orders) {
      try {
        const result = await salesOrderApi.refreshItemExtends(order.id)
        if ((result?.changedItems ?? 0) > 0 || (result?.changedFieldsCount ?? 0) > 0) changedCodes.push(order.code)
      } catch {
        failedCodes.push(order.code)
      }
    }
    salesRefreshCodes.value = changedCodes
    salesRefreshFailedCodes.value = failedCodes
    salesRefreshChanged.value = changedCodes.length
    ElMessage.success(`销售订单刷新完成：共 ${orders.length} 条，变更 ${changedCodes.length} 条，失败 ${failedCodes.length} 条`)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '刷新销售订单失败'))
  } finally {
    refreshingSalesOrders.value = false
  }
}

async function refreshAllPurchaseOrders() {
  if (refreshingAny.value) return
  refreshingPurchaseOrders.value = true
  purchaseRefreshTotal.value = 0
  purchaseRefreshChanged.value = 0
  purchaseRefreshCodes.value = []
  purchaseRefreshFailedCodes.value = []
  purchaseMainStatusRefreshResult.value = null
  try {
    const allRows = await loadAllPurchaseOrders()
    const orders = allRows
      .map((x) => ({
        id: String(x.id ?? ''),
        code: String(x.purchaseOrderCode ?? x.code ?? x.id ?? '')
      }))
      .filter((x) => x.id)
    purchaseRefreshTotal.value = orders.length

    const changedCodes: string[] = []
    const failedCodes: string[] = []
    for (const order of orders) {
      try {
        const result = await purchaseOrderApi.refreshItemExtends(order.id)
        if ((result?.changedItems ?? 0) > 0 || (result?.changedFieldsCount ?? 0) > 0) changedCodes.push(order.code)
      } catch {
        failedCodes.push(order.code)
      }
    }
    purchaseRefreshCodes.value = changedCodes
    purchaseRefreshFailedCodes.value = failedCodes
    purchaseRefreshChanged.value = changedCodes.length
    const mainStatusResult = await refreshPurchaseOrderMainStatus()
    purchaseMainStatusRefreshResult.value = mainStatusResult
    ElMessage.success(
      `采购订单刷新完成：共 ${orders.length} 条，变更 ${changedCodes.length} 条，失败 ${failedCodes.length} 条；主状态变更 ${mainStatusResult.changedOrders} 条`
    )
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '刷新采购订单失败'))
  } finally {
    refreshingPurchaseOrders.value = false
  }
}
</script>

<style lang="scss" scoped>
.debug-page {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 20px;
  color: #303133;
}

.debug-header h1 {
  margin: 0;
  font-size: 20px;
  font-weight: 700;
  color: #303133;
}

.debug-sub {
  margin-top: 6px;
  font-size: 13px;
  color: #606266;
  line-height: 1.6;

  &.muted {
    margin-top: 4px;
  }
}

.debug-link {
  margin-left: 10px;
  color: var(--el-color-primary);
  text-decoration: none;
  font-weight: 600;
  white-space: nowrap;

  &:hover {
    text-decoration: underline;
    color: var(--el-color-primary-light-3);
  }
}

.debug-panel {
  padding: 16px 18px;
  border-radius: 10px;
  border: 1px solid var(--el-border-color-lighter);
  background: var(--el-bg-color);
  box-shadow: var(--el-box-shadow-light);
}

.panel-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
  color: #303133;
}

.panel-body {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
}

.refresh-actions {
  gap: 10px;
}

.refresh-hint {
  margin-top: 8px;
  font-size: 12px;
  color: #909399;
}

.simulate-result {
  margin-top: 10px;
  border: 1px solid var(--el-border-color-lighter);
  background: var(--el-fill-color-light);
  border-radius: 8px;
  padding: 10px 12px;
  color: #303133;
  font-size: 13px;
  display: grid;
  gap: 4px;
}

.refresh-result-grid {
  margin-top: 12px;
  display: grid;
  gap: 12px;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
}

.refresh-result-card {
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  background: var(--el-fill-color-blank);
  padding: 10px 12px;
}

.refresh-result-title {
  font-size: 13px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 8px;
}

.refresh-result-line {
  font-size: 13px;
  color: #606266;
  line-height: 1.6;
  word-break: break-all;
}
</style>
