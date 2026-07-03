<template>
  <div class="purchase-requisition-detail-page">
    <!-- 详情 CaptionBar（《业务详情页面规范》§3 单据类） -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="router.push('/purchase-requisitions')">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('purchaseRequisitionDetail.back') }}
        </button>
        <div v-if="data" class="pr-caption-title-group">
          <div class="caption-avatar-lg">{{ prCaptionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1
                  class="page-title"
                  :class="{ 'page-title--muted': data.status === 3 }"
                >
                  {{ t('purchaseRequisitionDetail.captionPrefix') }} {{ data.billCode }}
                </h1>
              </div>
            </div>
            <div class="title-meta title-meta--caption pr-header-meta-row">
              <el-tag effect="dark" :type="getStatusTagType(data.status)" size="small">
                {{ getStatusText(data.status) }}
              </el-tag>
              <span class="pr-caption-type-text">{{ getPrTypeLabel(data.type) }}</span>
            </div>
          </div>
        </div>
      </div>
      <div v-if="data && (canGeneratePurchaseOrder || showHeaderMoreMenu)" class="header-right">
        <template v-if="canGeneratePurchaseOrder">
          <button type="button" class="btn-warning" @click="handleGeneratePurchaseOrder">
            {{ t('purchaseRequisitionDetail.generatePo') }}
          </button>
          <button
            v-if="inPoBasket"
            type="button"
            class="btn-secondary"
            disabled
          >
            {{ t('purchaseRequisitionDetail.addedToBatch') }}
          </button>
          <el-tooltip
            v-else-if="!isPrBasketEligible"
            :content="t('purchaseRequisitionList.basket.statusDenied')"
            placement="bottom"
          >
            <span class="inline-flex">
              <button type="button" class="btn-secondary" disabled>
                {{ t('purchaseRequisitionDetail.addToBatch') }}
              </button>
            </span>
          </el-tooltip>
          <button
            v-else
            type="button"
            class="btn-secondary"
            @click="handleAddToBatch"
          >
            {{ t('purchaseRequisitionDetail.addToBatch') }}
          </button>
        </template>
        <el-dropdown
          v-if="showHeaderMoreMenu"
          trigger="click"
          placement="bottom-end"
          popper-class="pr-detail-header-more-popper"
          @command="onHeaderMoreCommand"
        >
          <button type="button" class="btn-more-actions" :title="t('purchaseRequisitionDetail.moreActions')" :aria-label="t('purchaseRequisitionDetail.moreActions')">
            <span class="btn-more-actions__dots" aria-hidden="true">⋯</span>
          </button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item
                v-if="canPrSoftDelete && data.status === 0"
                command="softDelete"
                class="detail-more-item--danger"
              >
                {{ t('purchaseRequisitionList.actions.delete') }}
              </el-dropdown-item>
              <el-dropdown-item
                v-if="canPrSoftDelete && data.status !== 0"
                disabled
              >
                {{ t('purchaseRequisitionList.actions.deleteDeniedStatus') }}
              </el-dropdown-item>
              <el-dropdown-item
                v-if="canPrForceDelete"
                command="forceDelete"
                divided
                class="detail-more-item--danger"
              >
                {{ t('purchaseRequisitionList.actions.forceDelete') }}
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </div>

    <div v-loading="loading" element-loading-background="rgba(10,22,40,0.8)" class="detail-content">
      <template v-if="data">
        <!-- 基本信息（§4–§5） -->
        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('purchaseRequisitionDetail.basicInfo') }}</span>
            </div>
            <div class="section-header__meta">
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('purchaseRequisitionDetail.createDate') }}</span>
                <span class="section-header-meta-item__value">{{ prBasicCreateDateText }}</span>
              </span>
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('purchaseRequisitionDetail.createUser') }}</span>
                <span class="section-header-meta-item__value">{{ prBasicCreateUserText }}</span>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('purchaseRequisitionList.columns.sellOrder') }}</span>
              <span class="info-value info-value--code">
                <button
                  v-if="data.sellOrderId && !maskPurchaseSensitiveFields"
                  type="button"
                  class="info-link-btn"
                  @click="goSellOrder(data.sellOrderId)"
                >
                  {{ data.sellOrderCode || data.sellOrderId }}
                </button>
                <template v-else>{{ maskPurchaseSensitiveFields ? '—' : (data.sellOrderCode || data.sellOrderId || '—') }}</template>
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('purchaseRequisitionList.columns.pn') }}</span>
              <span class="info-value">{{ data.pn || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('purchaseRequisitionList.columns.brand') }}</span>
              <span class="info-value">{{ data.brand || '—' }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('purchaseRequisitionList.columns.qty') }}</span>
              <span class="info-value">{{ data.qty ?? '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('purchaseRequisitionList.columns.expectedPurchaseTime') }}</span>
              <span class="info-value info-value--time">{{ formatExpectedPurchaseTime(data.expectedPurchaseTime) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('purchaseRequisitionList.columns.type') }}</span>
              <span class="info-value">{{ getPrTypeLabel(data.type) }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('purchaseRequisitionList.columns.salesUserAccount') }}</span>
              <span class="info-value">{{ prSalesUserDisplay }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('purchaseRequisitionList.columns.purchaseUserAccount') }}</span>
              <span class="info-value">{{ prPurchaseUserDisplay }}</span>
            </div>
            <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
          </div>
          <div class="info-grid info-grid--inline-labels">
            <div class="info-item info-item--span-all">
              <span class="info-label">{{ t('purchaseRequisitionList.columns.remark') }}</span>
              <span class="info-value">{{ data.remark?.trim() || '—' }}</span>
            </div>
          </div>
        </div>

        <!-- TabBar（§6） -->
        <div class="tabs-section">
          <div class="tabs-nav">
            <button
              class="tab-btn"
              :class="{ 'tab-btn--active': detailActiveTab === 'poItems' }"
              type="button"
              @click="detailActiveTab = 'poItems'"
            >
              {{ t('purchaseRequisitionDetail.tabs.poItems') }}
              <span v-if="poLineItems.length" class="tab-count">{{ poLineItems.length }}</span>
            </button>
          </div>
          <div class="tabs-body">
            <div v-show="detailActiveTab === 'poItems'" v-loading="poItemsLoading" class="detail-items-table-wrap">
              <CrmDataTable
                v-if="poLineItems.length > 0"
                :data="poLineItems"
                class="items-table detail-panel-list-table"
                size="small"
                stripe
              >
                <el-table-column
                  :label="t('purchaseRequisitionDetail.poItemTable.purchaseOrderCode')"
                  min-width="140"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">
                    <button
                      v-if="!maskPurchaseSensitiveFields && row.purchaseOrderId"
                      type="button"
                      class="info-link-btn"
                      @click="goPurchaseOrder(row.purchaseOrderId)"
                    >
                      {{ row.purchaseOrderCode || '—' }}
                    </button>
                    <span v-else>{{ maskPurchaseSensitiveFields ? '—' : (row.purchaseOrderCode || '—') }}</span>
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
                    {{ maskPurchaseSensitiveFields ? '—' : row.cost != null ? Number(row.cost).toFixed(4) : '—' }}
                  </template>
                </el-table-column>
                <el-table-column :label="t('purchaseRequisitionDetail.poItemTable.currency')" width="80" align="center">
                  <template #default="{ row }">{{
                    maskPurchaseSensitiveFields ? '—' : formatPoCurrency(row.currency)
                  }}</template>
                </el-table-column>
                <el-table-column :label="t('purchaseRequisitionDetail.poItemTable.poStatus')" width="120">
                  <template #default="{ row }">{{ formatPoStatus(row.poStatus) }}</template>
                </el-table-column>
                <el-table-column
                  :label="t('purchaseRequisitionList.columns.actions')"
                  :width="prPoItemsOpColWidth"
                  :min-width="prPoItemsOpColMinWidth"
                  align="center"
                  fixed="right"
                  class-name="op-col"
                  label-class-name="op-col"
                >
                  <template #header>
                    <div class="list-op-col-header--icon-only">
                      <button
                        type="button"
                        class="op-col-toggle-btn list-op-col-toggle"
                        :aria-label="prPoItemsOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
                        @click.stop="togglePrPoItemsOpCol"
                      >
                        {{ prPoItemsOpColExpanded ? '>' : '<' }}
                      </button>
                    </div>
                  </template>
                  <template #default="{ row }">
                    <div @click.stop @dblclick.stop>
                      <div v-if="prPoItemsOpColExpanded" class="action-btns">
                        <button type="button" class="action-btn action-btn--primary" @click.stop="goPurchaseOrder(row.purchaseOrderId)">
                          {{ t('purchaseRequisitionDetail.poItemTable.actionOpenPo') }}
                        </button>
                      </div>
                      <el-dropdown v-else trigger="click" placement="bottom-end">
                        <div class="op-more-dropdown-trigger">
                          <button type="button" class="op-more-trigger">...</button>
                        </div>
                        <template #dropdown>
                          <el-dropdown-menu>
                            <el-dropdown-item @click.stop="goPurchaseOrder(row.purchaseOrderId)">
                              <span class="op-more-item op-more-item--primary">{{
                                t('purchaseRequisitionDetail.poItemTable.actionOpenPo')
                              }}</span>
                            </el-dropdown-item>
                          </el-dropdown-menu>
                        </template>
                      </el-dropdown>
                    </div>
                  </template>
                </el-table-column>
              </CrmDataTable>
              <p v-else class="po-items-empty">{{ t('purchaseRequisitionDetail.poItemTable.empty') }}</p>
            </div>
          </div>
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
import { usePurchaseRequisitionPoBasketStore } from '@/stores/purchaseRequisitionPoBasket'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { canGeneratePurchaseOrderFromRequisition } from '@/utils/purchaseOrderCreateGate'
import {
  isPrBasketEligibleStatus,
  normalizePrDetailToBasketItem
} from '@/utils/purchaseRequisitionBatchPo'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'

interface PurchaseRequisitionDetail {
  id: string
  billCode?: string
  sellOrderId?: string
  sellOrderCode?: string
  pn?: string
  brand?: string
  qty?: number
  expectedPurchaseTime?: string
  status?: number
  type?: number
  remark?: string
  createTime?: string
  createUserAccount?: string
  createUserName?: string
  createdBy?: string
  purchaseUserAccount?: string
  purchaseUserName?: string
  purchaseUserId?: string
  salesUserAccount?: string
  salesUserName?: string
  salesUserId?: string
}

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const basketStore = usePurchaseRequisitionPoBasketStore()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()

const canGeneratePurchaseOrder = computed(() =>
  canGeneratePurchaseOrderFromRequisition({
    isSysAdmin: authStore.user?.isSysAdmin,
    identityType: authStore.user?.identityType,
    roleCodes: authStore.user?.roleCodes,
    hasPermission: (code) => authStore.hasPermission(code)
  })
)

const inPoBasket = computed(() => {
  const id = String(data.value?.id ?? '').trim()
  return id ? basketStore.has(id) : false
})

const isPrBasketEligible = computed(() => isPrBasketEligibleStatus(Number(data.value?.status ?? -1)))

const loading = ref(false)
const deleting = ref(false)
const data = ref<PurchaseRequisitionDetail | null>(null)

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

const prPoItemsOpColExpanded = ref(false)
const PR_PO_ITEMS_OP_COL_COLLAPSED = 43
const PR_PO_ITEMS_OP_COL_EXPANDED = 173
const PR_PO_ITEMS_OP_COL_EXPANDED_MIN = 160
const prPoItemsOpColWidth = computed(() =>
  prPoItemsOpColExpanded.value ? PR_PO_ITEMS_OP_COL_EXPANDED : PR_PO_ITEMS_OP_COL_COLLAPSED
)
const prPoItemsOpColMinWidth = computed(() =>
  prPoItemsOpColExpanded.value ? PR_PO_ITEMS_OP_COL_EXPANDED_MIN : PR_PO_ITEMS_OP_COL_COLLAPSED
)
function togglePrPoItemsOpCol() {
  prPoItemsOpColExpanded.value = !prPoItemsOpColExpanded.value
}

const canPrSoftDelete = computed(
  () =>
    authStore.hasPermission('purchase-requisition.write') || authStore.hasPermission('sales-order.write')
)
const canPrForceDelete = computed(() => authStore.user?.isSysAdmin === true)
const showHeaderMoreMenu = computed(() => canPrSoftDelete.value || canPrForceDelete.value)

const prCaptionAvatarChar = computed(() => {
  const code = String(data.value?.billCode ?? '').trim()
  return code ? code[0]! : '采'
})

const prBasicCreateDateText = computed(() => {
  const raw = data.value?.createTime
  if (!raw) return '—'
  const s = formatDisplayDate(String(raw))
  return s === '--' ? '—' : s
})

const prBasicCreateUserText = computed(() => {
  const d = data.value
  if (!d) return '—'
  const name = d.createUserAccount ?? d.createUserName ?? d.createdBy
  const s = name != null ? String(name).trim() : ''
  return s || '—'
})

const prPurchaseUserDisplay = computed(() => {
  if (maskPurchaseSensitiveFields.value) return '—'
  const d = data.value
  if (!d) return '—'
  const name = d.purchaseUserAccount ?? d.purchaseUserName ?? d.purchaseUserId
  const s = name != null ? String(name).trim() : ''
  return s || '—'
})

const prSalesUserDisplay = computed(() => {
  if (maskPurchaseSensitiveFields.value) return '—'
  const d = data.value
  if (!d) return '—'
  const name = d.salesUserAccount ?? d.salesUserName ?? d.salesUserId
  const s = name != null ? String(name).trim() : ''
  return s || '—'
})

function getStatusText(s: number | undefined) {
  const m: Record<number, string> = {
    0: t('purchaseRequisitionList.status.new'),
    1: t('purchaseRequisitionList.status.partialDone'),
    2: t('purchaseRequisitionList.status.allDone'),
    3: t('purchaseRequisitionList.status.cancelled')
  }
  if (s == null) return '—'
  return m[s] ?? String(s)
}

function getStatusTagType(s: number | undefined): '' | 'success' | 'warning' | 'info' | 'danger' {
  if (s === 0) return 'info'
  if (s === 1 || s === 2) return 'success'
  if (s === 3) return 'danger'
  return ''
}

function getPrTypeLabel(typeVal: number | undefined) {
  if (typeVal == null) return '—'
  const m: Record<number, string> = {
    0: t('purchaseRequisitionList.type.exclusive'),
    1: t('purchaseRequisitionList.type.publicStock')
  }
  return m[typeVal] ?? String(typeVal)
}

function formatExpectedPurchaseTime(v: unknown) {
  if (v == null || v === '') return '—'
  const s = formatDisplayDateTime(String(v))
  return s === '--' ? '—' : s
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

function goSellOrder(sellOrderId: string) {
  if (!sellOrderId) return
  router.push({ name: 'SalesOrderDetail', params: { id: sellOrderId } })
}

function handleGeneratePurchaseOrder() {
  if (!canGeneratePurchaseOrder.value || !data.value?.id) return
  router.push({ name: 'PurchaseOrderCreate', query: { requisitionId: data.value.id } })
}

function handleAddToBatch() {
  if (!canGeneratePurchaseOrder.value || !data.value) return
  if (!isPrBasketEligible.value) {
    ElMessage.warning(t('purchaseRequisitionList.basket.statusDenied'))
    return
  }
  const item = normalizePrDetailToBasketItem(data.value as unknown as Record<string, unknown>)
  if (!item) return
  if (!basketStore.upsert(item)) {
    ElMessage.warning(t('purchaseRequisitionList.basket.statusDenied'))
    return
  }
  ElMessage.success(t('purchaseRequisitionList.basket.addSuccess', { count: basketStore.count }))
}

function onHeaderMoreCommand(command: string) {
  if (command === 'softDelete') void handleSoftDelete()
  else if (command === 'forceDelete') void handleForceDelete()
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
    data.value = (await purchaseRequisitionApi.getById(id)) as PurchaseRequisitionDetail
    await loadPoLineItems(id)
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    ElMessage.error(msg || '加载失败')
    router.push('/purchase-requisitions')
  } finally {
    loading.value = false
  }
}

async function handleSoftDelete() {
  if (!data.value || !canPrSoftDelete.value) return
  if (Number(data.value.status) !== 0) {
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
    await purchaseRequisitionApi.softDelete(String(data.value.id))
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
    await purchaseRequisitionApi.forceDelete(String(data.value.id), entered)
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
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+SC:wght@300;400;500&display=swap');

.purchase-requisition-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 24px;
  flex-wrap: wrap;
  gap: 12px;
}

.header-left {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  min-width: 0;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.btn-warning {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid rgba(201, 154, 69, 0.4);
  color: $color-amber;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  background: rgba(201, 154, 69, 0.15);
  cursor: pointer;

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid $border-panel;
  color: $text-secondary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  background: rgba(255, 255, 255, 0.04);
  cursor: pointer;

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
}

.inline-flex {
  display: inline-flex;
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
  flex-shrink: 0;

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.pr-caption-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
  min-width: 0;
}

.caption-avatar-lg {
  width: 48px;
  height: 48px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.3), rgba(0, 212, 255, 0.2));
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
  color: $cyan-primary;
  flex-shrink: 0;
}

.page-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 6px;
}

.page-title-with-icons {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  min-width: 0;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;

  &--muted {
    color: rgba(150, 170, 195, 0.82);
  }
}

.title-meta--caption {
  margin-top: 4px;
}

.pr-header-meta-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  min-height: 28px;
}

.pr-caption-type-text {
  font-size: 13px;
  color: $text-muted;
}

.btn-more-actions {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 36px;
  height: 36px;
  padding: 0 10px;
  box-sizing: border-box;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  cursor: pointer;
  transition: all 0.2s;
  font-family: 'Noto Sans SC', sans-serif;

  &:hover {
    background: rgba(255, 255, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.25);
    color: $text-primary;
  }

  &__dots {
    font-size: 18px;
    line-height: 1;
    letter-spacing: 0.5px;
    transform: translateY(-1px);
    font-weight: 700;
  }
}

.detail-content {
  min-height: 200px;
}

.info-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: var(--crm-detail-section-header-bg);
}

.section-header__main {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.section-header__meta {
  display: flex;
  align-items: center;
  gap: 20px;
  flex-shrink: 0;
  margin-left: auto;
}

.section-header-meta-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  white-space: nowrap;
  &__label {
    color: $text-muted;
    &::after {
      content: '：';
    }
  }
  &__value {
    color: $text-secondary;
  }
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
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
  border-right: 1px solid rgba(255, 255, 255, 0.04);

  &:nth-child(3n) {
    border-right: none;
  }
}

.info-grid:not(.info-grid--inline-labels) .info-item {
  padding: 16px 20px;
}

.info-grid--inline-labels .info-item {
  flex-direction: row;
  align-items: center;
  gap: 8px;
  .info-label {
    flex-shrink: 0;
    white-space: nowrap;
    text-transform: none;
    letter-spacing: 0;
    font-size: 12px;
    &::after {
      content: '：';
    }
  }
  .info-value {
    flex: 1;
    min-width: 0;
    word-break: break-word;
  }
}

.info-grid--basic {
  .info-item {
    &:nth-child(3n) {
      border-right: none;
    }
  }
  .info-item--basic-spacer {
    border-right: none;
  }
}

.info-grid--inline-labels .info-item--span-all {
  grid-column: 1 / -1;
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
    font-family: 'Noto Sans SC', sans-serif;
    font-size: 12px;
    color: $color-ice-blue;
  }

  &--time {
    font-size: 12px;
    color: $text-muted;
  }
}

.info-link-btn {
  padding: 0;
  border: none;
  background: transparent;
  color: $color-ice-blue;
  font: inherit;
  font-size: 12px;
  cursor: pointer;
  text-align: left;

  &:hover {
    color: $cyan-primary;
    text-decoration: underline;
  }
}

.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.tabs-nav {
  display: flex;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  padding: 0 16px;
  background: var(--crm-detail-section-header-bg);
}

.tab-btn {
  padding: 12px 16px;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  margin-bottom: -1px;
  display: inline-flex;
  align-items: center;
  gap: 6px;

  &:hover {
    color: $text-secondary;
  }

  &--active {
    color: $cyan-primary;
    border-bottom-color: $cyan-primary;
  }
}

.tab-count {
  font-size: 11px;
  padding: 1px 7px;
  border-radius: 999px;
  background: rgba(0, 212, 255, 0.1);
  color: $cyan-primary;
}

.tabs-body {
  padding: 20px;
}

.detail-items-table-wrap {
  margin-top: 4px;
}

.detail-items-table-wrap :deep(.items-table) {
  --el-table-border-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;

  .el-table {
    color: var(--crm-table-text);
  }

  .el-table__inner-wrapper {
    background: transparent;
    &::before {
      display: none !important;
    }
    &::after {
      display: none !important;
    }
  }

  .el-table__border-left-patch {
    display: none !important;
  }

  .el-table__cell {
    .el-button {
      white-space: nowrap !important;
    }
    .cell {
      white-space: nowrap;
    }
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
