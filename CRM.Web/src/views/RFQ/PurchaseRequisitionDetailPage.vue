<template>
  <div class="purchase-requisition-detail-page">
    <!-- 页面头部：参考 CustomerDetail.vue 的深色详情页布局 -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="router.push('/purchase-requisitions')">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          返回
        </button>

        <div class="customer-title-group">
          <h1 class="page-title">采购申请详情</h1>
          <div class="title-meta">
            <span class="customer-code">{{ data?.billCode || '—' }}</span>
          </div>
        </div>
      </div>

      <div v-if="data" class="header-actions">
        <el-tooltip
          v-if="canPrSoftDelete && data.status !== 0"
          :content="t('purchaseRequisitionList.actions.deleteDeniedStatus')"
          placement="bottom"
        >
          <span class="header-actions__wrap">
            <el-button size="small" type="danger" plain disabled>
              {{ t('purchaseRequisitionList.actions.delete') }}
            </el-button>
          </span>
        </el-tooltip>
        <el-button
          v-else-if="canPrSoftDelete"
          size="small"
          type="danger"
          plain
          :loading="deleting"
          @click="handleSoftDelete"
        >
          {{ t('purchaseRequisitionList.actions.delete') }}
        </el-button>
        <el-button
          v-if="canPrForceDelete"
          size="small"
          type="danger"
          :loading="deleting"
          @click="handleForceDelete"
        >
          {{ t('purchaseRequisitionList.actions.forceDelete') }}
        </el-button>
      </div>
    </div>

    <div v-loading="loading" element-loading-background="rgba(10,22,40,0.8)" class="detail-content">
      <template v-if="data">
        <div class="info-section">
          <div class="section-header">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">基本信息</span>
          </div>

          <div class="info-grid">
            <div class="info-item">
              <span class="info-label">采购申请号</span>
              <span class="info-value info-value--code">{{ data.billCode }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">销售订单</span>
              <span class="info-value info-value--code">{{ data.sellOrderCode || data.sellOrderId }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">创建时间</span>
              <span class="info-value info-value--time">{{ data.createTime }}</span>
            </div>

            <div class="info-item">
              <span class="info-label">物料型号</span>
              <span class="info-value">{{ data.pn || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">品牌</span>
              <span class="info-value">{{ data.brand || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">采购员ID</span>
              <span class="info-value">{{ data.purchaseUserId || '—' }}</span>
            </div>

            <div class="info-item">
              <span class="info-label">申请数量</span>
              <span class="info-value">{{ data.qty ?? '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">预计采购日期</span>
              <span class="info-value info-value--time">{{ data.expectedPurchaseTime || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">状态</span>
              <span class="info-value">{{ getStatusText(data.status) }}</span>
            </div>

            <div class="info-item">
              <span class="info-label">类型</span>
              <span class="info-value">{{ getTypeText(data.type) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">备注</span>
              <span class="info-value">{{ data.remark || '—' }}</span>
            </div>
          </div>
        </div>

        <div class="tabs-section">
          <el-tabs v-model="detailActiveTab" class="pr-detail-tabs" type="border-card">
            <el-tab-pane :label="t('purchaseRequisitionDetail.tabs.poItems')" name="poItems">
              <div v-loading="poItemsLoading" class="po-items-panel">
                <el-table
                  v-if="poLineItems.length > 0"
                  :data="poLineItems"
                  size="small"
                  stripe
                  class="po-items-table"
                >
                  <el-table-column
                    :label="t('purchaseRequisitionDetail.poItemTable.purchaseOrderCode')"
                    min-width="140"
                    show-overflow-tooltip
                  >
                    <template #default="{ row }">
                      <el-button link type="primary" @click="goPurchaseOrder(row.purchaseOrderId)">
                        {{ row.purchaseOrderCode || '—' }}
                      </el-button>
                    </template>
                  </el-table-column>
                  <el-table-column
                    :label="t('purchaseRequisitionDetail.poItemTable.lineCode')"
                    prop="purchaseOrderItemCode"
                    min-width="150"
                    show-overflow-tooltip
                  />
                  <el-table-column
                    :label="t('purchaseRequisitionDetail.poItemTable.pn')"
                    prop="pn"
                    min-width="120"
                    show-overflow-tooltip
                  />
                  <el-table-column
                    :label="t('purchaseRequisitionDetail.poItemTable.brand')"
                    prop="brand"
                    width="100"
                    show-overflow-tooltip
                  />
                  <el-table-column
                    :label="t('purchaseRequisitionDetail.poItemTable.qty')"
                    prop="qty"
                    width="100"
                    align="right"
                  />
                  <el-table-column
                    :label="t('purchaseRequisitionDetail.poItemTable.cost')"
                    width="110"
                    align="right"
                  >
                    <template #default="{ row }">
                      {{ row.cost != null ? Number(row.cost).toFixed(4) : '—' }}
                    </template>
                  </el-table-column>
                  <el-table-column :label="t('purchaseRequisitionDetail.poItemTable.currency')" width="80" align="center">
                    <template #default="{ row }">{{ formatPoCurrency(row.currency) }}</template>
                  </el-table-column>
                  <el-table-column :label="t('purchaseRequisitionDetail.poItemTable.poStatus')" width="120">
                    <template #default="{ row }">{{ formatPoStatus(row.poStatus) }}</template>
                  </el-table-column>
                  <el-table-column
                    :label="t('purchaseRequisitionList.columns.actions')"
                    width="110"
                    align="center"
                    fixed="right"
                  >
                    <template #default="{ row }">
                      <el-button type="primary" link size="small" @click="goPurchaseOrder(row.purchaseOrderId)">
                        {{ t('purchaseRequisitionDetail.poItemTable.actionOpenPo') }}
                      </el-button>
                    </template>
                  </el-table-column>
                </el-table>
                <p v-else class="po-items-empty">{{ t('purchaseRequisitionDetail.poItemTable.empty') }}</p>
              </div>
            </el-tab-pane>
          </el-tabs>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { purchaseRequisitionApi } from '@/api/purchaseRequisition'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()

const loading = ref(false)
const deleting = ref(false)
const data = ref<any>(null)

const detailActiveTab = ref('poItems')
const poLineItems = ref<
  {
    id: string
    purchaseOrderId: string
    purchaseOrderCode?: string
    purchaseOrderItemCode: string
    sellOrderItemId?: string
    vendorId?: string
    poStatus?: number
    pn?: string
    brand?: string
    qty: number
    cost: number
    currency: number
  }[]
>([])
const poItemsLoading = ref(false)

const canPrSoftDelete = computed(
  () =>
    authStore.hasPermission('purchase-requisition.write') || authStore.hasPermission('sales-order.write')
)
const canPrForceDelete = computed(() => authStore.user?.isSysAdmin === true)

function getStatusText(s: number) {
  const m: Record<number, string> = {
    0: '新建',
    1: '部分完成',
    2: '全部完成',
    3: '已取消'
  }
  return m[s] ?? String(s)
}

function getTypeText(t: number) {
  const m: Record<number, string> = {
    0: '专属',
    1: '公开备货'
  }
  return m[t] ?? String(t)
}

function formatPoCurrency(c: number | undefined) {
  if (c == null) return '—'
  const m: Record<number, string> = {
    1: t('purchaseRequisitionDetail.poItemTable.rmb'),
    2: t('purchaseRequisitionDetail.poItemTable.usd'),
    3: t('purchaseRequisitionDetail.poItemTable.eur'),
    4: t('purchaseRequisitionDetail.poItemTable.hkd')
  }
  return m[c] ?? String(c)
}

function formatPoStatus(s: number | undefined) {
  if (s == null) return '—'
  if (s === 1) return t('purchaseRequisitionDetail.poItemTable.poSt1')
  if (s === 2) return t('purchaseRequisitionDetail.poItemTable.poSt2')
  if (s === 10) return t('purchaseRequisitionDetail.poItemTable.poSt10')
  if (s === 20) return t('purchaseRequisitionDetail.poItemTable.poSt20')
  if (s === 30) return t('purchaseRequisitionDetail.poItemTable.poSt30')
  if (s === 50) return t('purchaseRequisitionDetail.poItemTable.poSt50')
  if (s === 100) return t('purchaseRequisitionDetail.poItemTable.poSt100')
  if (s === -1) return t('purchaseRequisitionDetail.poItemTable.poStNeg1')
  if (s === -2) return t('purchaseRequisitionDetail.poItemTable.poStNeg2')
  return String(s)
}

function goPurchaseOrder(purchaseOrderId: string) {
  if (!purchaseOrderId) return
  router.push({ name: 'PurchaseOrderDetail', params: { id: purchaseOrderId } })
}

async function loadPoLineItems(requisitionId: string) {
  poItemsLoading.value = true
  try {
    const raw = await purchaseRequisitionApi.getPurchaseOrderItemsByRequisitionId(requisitionId)
    poLineItems.value = Array.isArray(raw) ? (raw as typeof poLineItems.value) : []
  } catch (e: unknown) {
    poLineItems.value = []
    const msg = e instanceof Error ? e.message : String(e)
    ElMessage.error(t('purchaseRequisitionDetail.poItemTable.loadError') + (msg ? `: ${msg}` : ''))
  } finally {
    poItemsLoading.value = false
  }
}

async function load() {
  const id = route.params.id as string
  if (!id) {
    ElMessage.error('参数错误：缺少 id')
    router.push('/purchase-requisitions')
    return
  }

  loading.value = true
  try {
    data.value = await purchaseRequisitionApi.getById(id)
    await loadPoLineItems(id)
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.message || e?.message || '加载失败')
    router.push('/purchase-requisitions')
  } finally {
    loading.value = false
  }
}

async function handleSoftDelete() {
  if (!data.value || !canPrSoftDelete.value) return
  if (data.value.status !== 0) {
    ElMessage.warning(t('purchaseRequisitionList.actions.deleteDeniedStatus'))
    return
  }
  try {
    await ElMessageBox.confirm(
      t('purchaseRequisitionList.actions.deleteConfirm'),
      t('purchaseRequisitionList.actions.delete'),
      { type: 'warning', confirmButtonText: t('common.confirm'), cancelButtonText: t('common.cancel') }
    )
  } catch {
    return
  }
  deleting.value = true
  try {
    await purchaseRequisitionApi.softDelete(data.value.id)
    ElMessage.success(t('purchaseRequisitionList.actions.deleteSuccess'))
    router.push('/purchase-requisitions')
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    ElMessage.error(msg || '删除失败')
  } finally {
    deleting.value = false
  }
}

async function handleForceDelete() {
  if (!data.value || !canPrForceDelete.value) return
  let value: string | undefined
  try {
    const res = await ElMessageBox.prompt(
      t('purchaseRequisitionList.actions.forceDeletePrompt'),
      t('purchaseRequisitionList.actions.forceDeleteTitle'),
      {
        confirmButtonText: t('common.confirm'),
        cancelButtonText: t('common.cancel'),
        inputPlaceholder: t('purchaseRequisitionList.actions.forceDeleteBillPlaceholder'),
        inputValidator: (v) =>
          !!(v && String(v).trim()) || t('purchaseRequisitionList.actions.forceDeleteBillPlaceholder')
      }
    )
    value = res.value
  } catch {
    return
  }
  const entered = String(value ?? '').trim()
  const code = String(data.value.billCode ?? '').trim()
  if (!entered || entered !== code) {
    ElMessage.error(t('purchaseRequisitionList.actions.forceDeleteBillMismatch'))
    return
  }
  deleting.value = true
  try {
    await purchaseRequisitionApi.forceDelete(data.value.id, entered)
    ElMessage.success(t('purchaseRequisitionList.actions.forceDeleteSuccess'))
    router.push('/purchase-requisitions')
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    ElMessage.error(msg || '强制删除失败')
  } finally {
    deleting.value = false
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.purchase-requisition-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

// ---- 页面头部 ----
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
  flex-wrap: wrap;
  gap: 12px;
}

.header-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.header-actions__wrap {
  display: inline-flex;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.customer-title-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0;
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
}

.customer-code {
  font-family: 'Space Mono', monospace;
  font-size: 11px;
  color: $text-muted;
}

// ---- 基本信息 ----
.detail-content {
  min-height: 200px;
}

.info-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: rgba(0, 0, 0, 0.1);
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.6);
  }
}

.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-primary;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
  padding: 0;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 16px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
  border-right: 1px solid rgba(255, 255, 255, 0.04);

  &:nth-child(3n) {
    border-right: none;
  }

  .info-label {
    font-size: 11px;
    color: $text-muted;
    letter-spacing: 0.5px;
    text-transform: uppercase;
  }

  .info-value {
    font-size: 13px;
    color: $text-secondary;

    &--code {
      font-family: 'Space Mono', monospace;
      font-size: 12px;
      color: $color-ice-blue;
    }

    &--amount {
      font-family: 'Space Mono', monospace;
      font-size: 13px;
      color: $text-primary;
      font-weight: 500;
    }

    &--time {
      font-size: 12px;
      color: $text-muted;
    }
  }
}

.tabs-section {
  margin-top: 20px;
}

.pr-detail-tabs {
  :deep(.el-tabs__header) {
    margin: 0 0 12px 0;
  }
  :deep(.el-tabs__item) {
    color: $text-muted;
  }
  :deep(.el-tabs__item.is-active) {
    color: $color-ice-blue;
  }
  :deep(.el-tabs--border-card) {
    background: $layer-2;
    border-color: rgba(0, 212, 255, 0.12);
  }
  :deep(.el-tabs--border-card > .el-tabs__content) {
    background: $layer-2;
    padding: 12px 16px 20px;
  }
}

.po-items-panel {
  min-height: 120px;
}

.po-items-table {
  :deep(.el-table__header th) {
    background: rgba(0, 212, 255, 0.08);
  }
}

.po-items-empty {
  margin: 0;
  padding: 24px 12px;
  text-align: center;
  color: $text-muted;
  font-size: 13px;
}
</style>

