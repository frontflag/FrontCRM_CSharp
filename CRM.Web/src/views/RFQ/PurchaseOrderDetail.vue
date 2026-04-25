<template>
  <div class="purchase-order-detail">
    <!-- 详情 CaptionBar（对齐 document/PRD/规范/UI规范/详情CaptionBar规范.md） -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="router.back()">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          返回
        </button>
        <div v-if="order" class="po-caption-title-group">
          <div class="caption-avatar-lg">{{ captionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1
                  class="page-title"
                  :class="{ 'page-title--muted': normalizePurchaseOrderMainStatus(order) === -2 }"
                >
                  {{ captionTitle }}
                </h1>
                <el-tag effect="dark" :type="getStatusType(normalizePurchaseOrderMainStatus(order))" size="small">
                  {{ getStatusText(normalizePurchaseOrderMainStatus(order)) }}
                </el-tag>
              </div>
              <button
                type="button"
                class="btn-favorite-star"
                :class="{ 'is-favorite': poFavorited }"
                :disabled="favoriteLoading"
                :title="poFavorited ? '取消收藏' : '收藏订单'"
                :aria-label="poFavorited ? '取消收藏' : '收藏采购订单'"
                :aria-pressed="poFavorited"
                @click="toggleFavorite"
              >
                <svg
                  v-if="!poFavorited"
                  class="star-icon"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="1.75"
                  stroke-linejoin="round"
                  aria-hidden="true"
                >
                  <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
                </svg>
                <svg v-else class="star-icon star-icon--solid" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
                </svg>
              </button>
            </div>
            <div v-if="captionMetaVisible" class="title-meta">
              <span class="caption-code">采购订单：{{ order.purchaseOrderCode }}</span>
              <span v-if="isStockingPurchaseOrder" class="order-type-badge order-type-badge--stocking">备货</span>
            </div>
            <div class="title-tags-row">
              <TagListDisplay :tags="currentTags" />
              <button type="button" class="btn-add-tag" @click="tagDialogVisible = true">添加标签</button>
            </div>
          </div>
        </div>
      </div>
      <div v-if="order" class="header-right">
        <button class="btn-primary" type="button" :disabled="refreshingExtends" @click="handleRefreshItemExtends">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="23 4 23 10 17 10" />
            <polyline points="1 20 1 14 7 14" />
            <path d="M3.51 9a9 9 0 0 1 14.13-3.36L23 10M1 14l5.36 4.36A9 9 0 0 0 20.49 15" />
          </svg>
          {{ refreshingExtends ? '刷新中...' : '刷新' }}
        </button>
        <button class="btn-primary" type="button" @click="handleEdit">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
          </svg>
          编辑
        </button>
        <el-dropdown
          trigger="click"
          placement="bottom-end"
          popper-class="po-detail-header-more-popper"
          @command="onHeaderMoreCommand"
        >
          <button type="button" class="btn-more-actions" title="更多操作" aria-label="更多操作">
            <span class="btn-more-actions__dots" aria-hidden="true">⋯</span>
          </button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item
                v-if="canCancelPurchaseOrderFromMenu"
                command="cancel_order"
                class="detail-more-item--danger"
              >
                取消订单
              </el-dropdown-item>
              <el-dropdown-item command="delete" class="detail-more-item--danger">删除订单</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
        <button
          v-if="order && purchaseOrderReportAllowed(normalizePurchaseOrderMainStatus(order))"
          class="btn-success"
          type="button"
          @click="handleGoReport"
        >
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
            <polyline points="14 2 14 8 20 8" />
            <line x1="12" y1="18" x2="12" y2="12" />
            <line x1="9" y1="15" x2="15" y2="15" />
          </svg>
          采购单报表
        </button>
      </div>
    </div>

    <div v-if="loading" class="loading-wrap">
      <el-skeleton :rows="8" animated />
    </div>

    <template v-else-if="order">
      <!-- 基本信息卡片 -->
      <div class="info-section">
        <div class="section-header">
          <div class="section-dot section-dot--cyan"></div>
          <span class="section-title">基本信息</span>
        </div>
        <div class="info-grid">
          <div class="info-item">
            <span class="info-label">采购订单号</span>
            <span class="info-value info-value--code">
              {{ order.purchaseOrderCode }}
              <span v-if="isStockingPurchaseOrder" class="order-type-badge order-type-badge--stocking">备货</span>
            </span>
          </div>
          <div class="info-item">
            <span class="info-label">状态</span>
            <span class="info-value">{{ getStatusText(normalizePurchaseOrderMainStatus(order)) }}</span>
          </div>
          <div class="info-item" v-if="canViewVendorInfo">
            <span class="info-label">供应商</span>
            <span class="info-value">{{ order.vendorName || '--' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">采购员</span>
            <span class="info-value">{{ order.purchaseUserName || '--' }}</span>
          </div>
          <div class="info-item" v-if="canViewPurchaseAmount">
            <span class="info-label">总金额</span>
            <span class="info-value info-value--amount amount-with-code">
              <span>{{ formatTotalAmountNumber(order.total) }}</span>
              <span v-if="formatTotalAmountNumber(order.total) !== '—'" class="amount-ccy" :class="currencyCodeClass(order.currency)">
                {{ currencyCodeText(order.currency) }}
              </span>
            </span>
          </div>
          <div class="info-item">
            <span class="info-label">行项目数</span>
            <span class="info-value">{{ order.itemRows ?? 0 }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">交货日期</span>
            <span class="info-value info-value--time">{{ formatDateTime(order.deliveryDate) }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">创建时间</span>
            <span class="info-value info-value--time">{{ formatDateTime(order.createTime) }}</span>
          </div>
          <div class="info-item info-item--span-3">
            <span class="info-label">送货地址</span>
            <span class="info-value">{{ order.deliveryAddress || '--' }}</span>
          </div>
          <div class="info-item info-item--span-3">
            <span class="info-label">备注</span>
            <span class="info-value">{{ order.comment || '--' }}</span>
          </div>
          <div class="info-item info-item--span-3">
            <span class="info-label">内部备注</span>
            <span class="info-value">{{ order.innerComment || '--' }}</span>
          </div>
        </div>
      </div>

      <!-- TabBar：订单明细 | 文档 -->
      <div class="tabs-section">
        <div class="tabs-nav">
          <button class="tab-btn" :class="{ 'tab-btn--active': activeTab === 'items' }" @click="activeTab = 'items'">订单明细</button>
          <button
            v-if="!maskPurchaseSensitiveFields"
            class="tab-btn"
            :class="{ 'tab-btn--active': activeTab === 'documents' }"
            @click="activeTab = 'documents'"
          >
            文档
          </button>
        </div>
        <div class="tabs-body">
          <div v-show="activeTab === 'items'" class="detail-items-table-wrap">
            <CrmDataTable
              :data="order.items"
              :row-key="poItemRowKey"
              size="small"
              v-if="order.items?.length"
              class="items-table"
            >
              <el-table-column type="index" width="50" label="#" />
              <el-table-column prop="purchaseOrderItemCode" label="明细编号" min-width="168" show-overflow-tooltip />
              <el-table-column prop="pn" label="物料型号" min-width="160" />
              <el-table-column prop="brand" label="品牌" width="120" />
              <el-table-column prop="qty" label="数量" align="right" width="100" />
              <el-table-column v-if="canViewPurchaseAmount" prop="cost" label="单价" align="right" width="120">
                <template #default="{ row }">
                  <span class="amount-with-code">
                    <span>{{ formatUnitPriceNumber(row.cost) }}</span>
                    <span v-if="formatUnitPriceNumber(row.cost) !== '—'" class="amount-ccy" :class="currencyCodeClass(row.currency)">
                      {{ currencyCodeText(row.currency) }}
                    </span>
                  </span>
                </template>
              </el-table-column>
              <el-table-column v-if="canViewPurchaseAmount" label="金额" align="right" width="130">
                <template #default="{ row }">
                  <span class="amount-with-code">
                    <span>{{ formatTotalAmountNumber(row.qty * row.cost) }}</span>
                    <span
                      v-if="formatTotalAmountNumber(row.qty * row.cost) !== '—'"
                      class="amount-ccy"
                      :class="currencyCodeClass(row.currency)"
                    >
                      {{ currencyCodeText(row.currency) }}
                    </span>
                  </span>
                </template>
              </el-table-column>
              <el-table-column label="采购状态" width="100" align="center">
                <template #default="{ row }">
                  <el-tag :type="poExtendTriTagType(row.purchaseProgressStatus)" size="small" effect="dark">
                    {{ poPurchaseProgressText(row.purchaseProgressStatus) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="入库状态" width="100" align="center">
                <template #default="{ row }">
                  <el-tag :type="poExtendTriTagType(row.stockInProgressStatus)" size="small" effect="dark">
                    {{ poStockInProgressText(row.stockInProgressStatus) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="付款状态" width="100" align="center">
                <template #default="{ row }">
                  <el-tag :type="poExtendTriTagType(row.paymentProgressStatus)" size="small" effect="dark">
                    {{ poPaymentProgressText(row.paymentProgressStatus) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="开票状态" width="100" align="center">
                <template #default="{ row }">
                  <el-tag :type="poExtendTriTagType(row.invoiceProgressStatus)" size="small" effect="dark">
                    {{ poInvoiceProgressText(row.invoiceProgressStatus) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="采购数量" width="108" align="right" class-name="po-item-progress-qty-col">
                <template #default="{ row }">
                  <div>{{ formatPoProgressQty(row.purchaseProgressQty) }}</div>
                  <div
                    v-if="poShowSellLinePurchaseSum(row)"
                    class="po-item-progress-sub"
                  >
                    同销单行 {{ formatPoProgressQty(row.sellLinePurchaseQtySum) }}
                  </div>
                </template>
              </el-table-column>
              <el-table-column label="入库数量" width="96" align="right">
                <template #default="{ row }">{{ formatPoProgressQty(row.stockInProgressQty) }}</template>
              </el-table-column>
              <el-table-column v-if="canViewPurchaseAmount" label="已付款" width="100" align="right">
                <template #default="{ row }">
                  <span class="amount-with-code">
                    <span>{{ formatTotalAmountNumber(row.paymentProgressAmount) }}</span>
                    <span
                      v-if="formatTotalAmountNumber(row.paymentProgressAmount) !== '—'"
                      class="amount-ccy"
                      :class="currencyCodeClass(row.currency)"
                    >
                      {{ currencyCodeText(row.currency) }}
                    </span>
                  </span>
                </template>
              </el-table-column>
              <el-table-column v-if="canViewPurchaseAmount" label="已开票额" width="100" align="right">
                <template #default="{ row }">
                  <span class="amount-with-code">
                    <span>{{ formatTotalAmountNumber(row.invoiceProgressAmount) }}</span>
                    <span
                      v-if="formatTotalAmountNumber(row.invoiceProgressAmount) !== '—'"
                      class="amount-ccy"
                      :class="currencyCodeClass(row.currency)"
                    >
                      {{ currencyCodeText(row.currency) }}
                    </span>
                  </span>
                </template>
              </el-table-column>
              <el-table-column prop="comment" label="备注" min-width="120" />
              <el-table-column prop="innerComment" label="内部备注" min-width="160" />
              <el-table-column prop="sellOrderItemCode" label="销售订单明细编号" min-width="168" show-overflow-tooltip />
              <el-table-column label="操作" width="240" fixed="right" align="center" class-name="op-col" label-class-name="op-col">
                <template #default="{ row }">
                  <div @click.stop @dblclick.stop>
                    <div class="action-btns action-btns--po-detail-items">
                      <el-button link type="primary" size="small" @click.stop="goPoItemLines(row)">明细列表</el-button>
                      <el-button
                        v-if="poLineShowArrival(row)"
                        link
                        type="warning"
                        size="small"
                        @click.stop="openPoLineArrival(row)"
                      >
                        通知到货
                      </el-button>
                      <el-button
                        v-if="poLineShowPayment(row)"
                        link
                        type="warning"
                        size="small"
                        @click.stop="openPoLinePayment(row)"
                      >
                        申请付款
                      </el-button>
                    </div>
                  </div>
                </template>
              </el-table-column>
            </CrmDataTable>
            <el-empty v-else description="暂无明细" :image-size="80" />
          </div>
          <div
            v-show="activeTab === 'documents' && !maskPurchaseSensitiveFields"
            class="doc-tab-content"
            :class="{ 'doc-tab-content--dragging': docTabDragging }"
            @drop.prevent="onDocTabDrop"
            @dragover.prevent="onDocTabDragOver"
            @dragleave="onDocTabDragLeave"
          >
            <DocumentUploadPanel
              ref="docUploadRef"
              biz-type="PURCHASE_ORDER"
              :biz-id="String(order.id)"
              :max-files="20"
              :max-size-mb="100"
              @uploaded="docListRef?.refresh()"
            />
            <DocumentListPanel
              ref="docListRef"
              biz-type="PURCHASE_ORDER"
              :biz-id="String(order.id)"
              view-mode="list"
              style="margin-top: 16px;"
            />
          </div>
        </div>
      </div>
    </template>

    <el-empty v-else description="订单不存在" />

    <!-- 标签弹窗 -->
    <ApplyTagsDialog
      v-model="tagDialogVisible"
      entity-type="PURCHASE_ORDER"
      :entity-ids="order ? [order.id] : []"
      title="为采购订单添加标签"
      @success="refreshTags"
    />

    <PurchaseOrderItemLineDialogs ref="poItemLineDialogsRef" @success="fetchOrder" />

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { purchaseOrderApi, type PurchaseOrderItemExtendRefreshResult } from '@/api/purchaseOrder'
import { favoriteApi } from '@/api/favorite'
import {
  PURCHASE_ORDER_FAVORITE_ENTITY_TYPE,
  PURCHASE_ORDER_FAVORITES_CHANGED_EVENT
} from '@/constants/purchaseOrderFavorites'
import {
  purchaseOrderReportAllowed,
  normalizePurchaseOrderMainStatus
} from '@/constants/purchaseOrderStatus'
import { tagApi, type TagDefinitionDto } from '@/api/tag'
import { useAuthStore } from '@/stores/auth'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import TagListDisplay from '@/components/Tag/TagListDisplay.vue'
import ApplyTagsDialog from '@/components/Tag/ApplyTagsDialog.vue'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatTotalAmountNumber, formatUnitPriceNumber } from '@/utils/moneyFormat'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import { recordPurchaseOrderRecentView } from '@/utils/purchaseOrderRecentHistory'
import PurchaseOrderItemLineDialogs from '@/components/purchaseOrder/PurchaseOrderItemLineDialogs.vue'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()

const canViewVendorInfo = computed(
  () => !maskPurchaseSensitiveFields.value && authStore.hasPermission('vendor.info.read')
)
const canViewPurchaseAmount = computed(
  () => !maskPurchaseSensitiveFields.value && authStore.hasPermission('purchase.amount.read')
)
/** 与采购订单明细列表「通知到货」一致 */
const canCreateArrivalNotice = computed(() => authStore.hasPermission('purchase-order.read'))

const loading = ref(false)
const refreshingExtends = ref(false)
const order = ref<any>(null)

/** 与原列表「取消订单」一致：审核通过(10)前可标记取消(-2)；已为取消不可再点 */
const canCancelPurchaseOrderFromMenu = computed(() => {
  const o = order.value
  if (!o) return false
  const s = normalizePurchaseOrderMainStatus(o)
  if (!Number.isFinite(s) || s === -2) return false
  return s < 10
})

const poFavorited = ref(false)
const favoriteLoading = ref(false)
const activeTab = ref('items')
const docListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)
const docUploadRef = ref<{ addDroppedFiles: (files: File[]) => void } | null>(null)
const docTabDragging = ref(false)
const docTabDragDepth = ref(0)

watch(maskPurchaseSensitiveFields, (m) => {
  if (m && activeTab.value === 'documents') activeTab.value = 'items'
})

function onDocTabDragOver() {
  docTabDragDepth.value = Math.max(1, docTabDragDepth.value)
  docTabDragging.value = true
}

function onDocTabDragLeave() {
  docTabDragDepth.value = Math.max(0, docTabDragDepth.value - 1)
  if (docTabDragDepth.value === 0) docTabDragging.value = false
}

function onDocTabDrop(e: DragEvent) {
  docTabDragDepth.value = 0
  docTabDragging.value = false
  const files = e.dataTransfer?.files ? Array.from(e.dataTransfer.files) : []
  if (!files.length) return
  docUploadRef.value?.addDroppedFiles(files)
}

// 标签
const currentTags = ref<TagDefinitionDto[]>([])
const tagDialogVisible = ref(false)

const poItemLineDialogsRef = ref<InstanceType<typeof PurchaseOrderItemLineDialogs> | null>(null)

const orderId = computed(() => route.params.id as string)

function poItemRowKey(row: any) {
  return String(row?.id ?? row?.Id ?? '')
}

/** 扩展表进度：0=待 1=部分 2=完成 */
function poExtendTriTagType(v?: number): '' | 'info' | 'success' | 'warning' | 'danger' {
  const map: Record<number, '' | 'info' | 'success' | 'warning' | 'danger'> = {
    0: 'info',
    1: 'warning',
    2: 'success'
  }
  return v !== undefined && v !== null ? (map[v] ?? 'info') : 'info'
}
function poPurchaseProgressText(v?: number) {
  const map: Record<number, string> = { 0: '待采购', 1: '部分采购', 2: '采购完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}
function poStockInProgressText(v?: number) {
  const map: Record<number, string> = { 0: '待入库', 1: '部分入库', 2: '入库完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}
function poPaymentProgressText(v?: number) {
  const map: Record<number, string> = { 0: '待付款', 1: '部分付款', 2: '付款完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}
function poInvoiceProgressText(v?: number) {
  const map: Record<number, string> = { 0: '待开票', 1: '部分开票', 2: '开票完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}

function formatPoProgressQty(q: unknown): string {
  const n = Number(q)
  if (!Number.isFinite(n)) return '--'
  return n.toLocaleString(undefined, { maximumFractionDigits: 4 })
}

/** 同一销售明细拆成多行采购时，展示「同销单行」累计有效采购数量 */
function poShowSellLinePurchaseSum(row: any): boolean {
  const a = Number(row?.purchaseProgressQty)
  const b = Number(row?.sellLinePurchaseQtySum)
  if (!Number.isFinite(b) || b <= 0) return false
  if (!Number.isFinite(a)) return true
  return Math.abs(b - a) > 1e-6
}

/** 将详情接口返回的明细行转为与「采购订单明细」列表行一致的结构，供通知到货 / 申请付款弹窗使用 */
function poDetailLineToListShape(it: any) {
  const o = order.value
  if (!o) return null
  const qty = Number(it.qty ?? it.Qty ?? 0)
  const cost = Number(it.cost ?? it.Cost ?? 0)
  return {
    purchaseOrderItemId: String(
      it.purchaseOrderItemId ?? it.PurchaseOrderItemId ?? it.id ?? it.Id ?? ''
    ),
    purchaseOrderId: String(o.id),
    purchaseOrderCode: o.purchaseOrderCode,
    vendorId: o.vendorId,
    vendorName: o.vendorName,
    purchaseUserName: o.purchaseUserName,
    itemStatus: Number(it.status ?? it.Status ?? 0),
    pn: it.pn ?? it.PN,
    brand: it.brand ?? it.Brand,
    qty,
    cost,
    lineTotal: qty * cost,
    paymentRequestedAmount: Number(it.paymentRequestedAmount ?? it.PaymentRequestedAmount ?? 0),
    currency: it.currency ?? it.Currency ?? o.currency,
    deliveryDate: it.deliveryDate ?? it.DeliveryDate ?? o.deliveryDate,
    canApplyPayment: Boolean(it.canApplyPayment ?? it.CanApplyPayment)
  }
}

function poLineShowArrival(row: any) {
  const line = poDetailLineToListShape(row)
  return !!(line && canCreateArrivalNotice.value && line.itemStatus === 30)
}

function poLineShowPayment(row: any) {
  const line = poDetailLineToListShape(row)
  return !!(line && line.canApplyPayment)
}

function goPoItemLines(row: any) {
  const pn = String(row?.pn ?? row?.PN ?? '').trim()
  router.push({
    name: 'PurchaseOrderItemList',
    query: pn ? { pn } : {}
  })
}

function openPoLineArrival(row: any) {
  const line = poDetailLineToListShape(row)
  if (!line) return
  poItemLineDialogsRef.value?.openArrival(line)
}

function openPoLinePayment(row: any) {
  const line = poDetailLineToListShape(row)
  if (!line) return
  poItemLineDialogsRef.value?.openPayment(line)
}

/** CaptionBar：主标题优先供应商名称（有权限且有时），否则单号 */
const captionTitle = computed(() => {
  const o = order.value
  if (!o) return '采购订单'
  if (canViewVendorInfo.value && o.vendorName?.trim()) return String(o.vendorName).trim()
  return o.purchaseOrderCode || '采购订单'
})

const captionMetaVisible = computed(() => {
  const o = order.value
  if (!o?.purchaseOrderCode) return false
  if (canViewVendorInfo.value && o.vendorName?.trim()) return true
  return false
})

const isStockingPurchaseOrder = computed(() => Number(order.value?.type) === 2)

const captionAvatarChar = computed(() => {
  const t = captionTitle.value
  return (t && t[0]) || '采'
})

function onHeaderMoreCommand(cmd: string) {
  if (cmd === 'cancel_order') void handleCancelPurchaseOrder()
  else if (cmd === 'delete') void handleDeleteOrder()
}

async function handleCancelPurchaseOrder() {
  if (!order.value?.id || !canCancelPurchaseOrderFromMenu.value) return
  try {
    await ElMessageBox.confirm(
      `确认将采购订单 ${order.value.purchaseOrderCode} 标记为「取消」吗？`,
      '取消订单',
      { type: 'warning', confirmButtonText: '确认', cancelButtonText: '关闭' }
    )
    await purchaseOrderApi.updateStatus(order.value.id, -2)
    ElMessage.success('订单已取消')
    await fetchOrder()
  } catch {
    /* 取消 */
  }
}

async function handleDeleteOrder() {
  if (!order.value?.id) return
  try {
    await ElMessageBox.confirm(
      `确定要删除采购订单 ${order.value.purchaseOrderCode} 吗？`,
      '删除确认',
      { type: 'warning', confirmButtonText: '删除', cancelButtonText: '取消' }
    )
    await purchaseOrderApi.delete(order.value.id)
    ElMessage.success('已删除')
    router.push({ name: 'PurchaseOrderList' })
  } catch {
    /* 取消 */
  }
}

function handleGoReport() {
  if (!order.value?.id) return
  if (!purchaseOrderReportAllowed(normalizePurchaseOrderMainStatus(order.value))) {
    ElMessage.warning('仅供应商已确认后的采购订单可生成采购单报表')
    return
  }
  router.push({ name: 'PurchaseOrderReport', params: { id: String(order.value.id) } })
}

function poRefreshStatusText(v: string) {
  const n = Number(v)
  const map: Record<number, string> = { 0: '待', 1: '部分', 2: '完成' }
  return Number.isFinite(n) ? (map[n] ?? v) : v
}

function poRefreshFieldValueText(field: string, value: string) {
  if (
    field === 'purchaseProgressStatus' ||
    field === 'stockInProgressStatus' ||
    field === 'paymentProgressStatus' ||
    field === 'invoiceProgressStatus'
  ) {
    return poRefreshStatusText(value)
  }
  return value
}

function buildRefreshResultHtml(result: PurchaseOrderItemExtendRefreshResult) {
  const lines: string[] = [
    `共 ${result.changedItems} 条明细发生更新，${result.changedFieldsCount} 个字段已变更。`,
    ''
  ]
  for (const change of result.changes) {
    const lineCode = change.purchaseOrderItemCode || change.purchaseOrderItemId
    lines.push(`【${lineCode}】`)
    for (const field of change.fields) {
      const beforeText = poRefreshFieldValueText(field.field, field.before)
      const afterText = poRefreshFieldValueText(field.field, field.after)
      lines.push(`- ${field.label}: ${beforeText} -> ${afterText}`)
    }
    lines.push('')
  }
  const escaped = lines
    .join('\n')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/\n/g, '<br/>')
  return `<div style="max-height:420px;overflow:auto;line-height:1.7;">${escaped}</div>`
}

async function handleRefreshItemExtends() {
  if (!order.value?.id || refreshingExtends.value) return
  try {
    await ElMessageBox.confirm(
      `确认刷新采购订单 ${order.value.purchaseOrderCode} 的明细执行状态与扩展字段吗？`,
      '刷新确认',
      { type: 'warning', confirmButtonText: '刷新', cancelButtonText: '取消' }
    )
  } catch {
    return
  }

  refreshingExtends.value = true
  try {
    const result = await purchaseOrderApi.refreshItemExtends(order.value.id)
    await fetchOrder()
    if (!result || result.changedItems <= 0) {
      await ElMessageBox.alert('无更新数据', '刷新结果', { confirmButtonText: '知道了' })
      return
    }
    await ElMessageBox.alert(buildRefreshResultHtml(result), '刷新结果', {
      dangerouslyUseHTMLString: true,
      confirmButtonText: '知道了'
    })
  } catch {
    // 全局拦截器统一提示
  } finally {
    refreshingExtends.value = false
  }
}

onMounted(() => {
  fetchOrder()
})

watch(orderId, () => {
  fetchOrder()
})

async function loadFavoriteState() {
  const id = orderId.value
  if (!id) {
    poFavorited.value = false
    return
  }
  try {
    poFavorited.value = await favoriteApi.checkFavorite(PURCHASE_ORDER_FAVORITE_ENTITY_TYPE, id)
  } catch {
    poFavorited.value = false
  }
}

async function toggleFavorite() {
  const id = orderId.value
  if (!id || favoriteLoading.value) return
  favoriteLoading.value = true
  try {
    if (poFavorited.value) {
      await favoriteApi.removeFavorite(PURCHASE_ORDER_FAVORITE_ENTITY_TYPE, id)
      poFavorited.value = false
    } else {
      await favoriteApi.addFavorite({ entityType: PURCHASE_ORDER_FAVORITE_ENTITY_TYPE, entityId: id })
      poFavorited.value = true
    }
    window.dispatchEvent(new Event(PURCHASE_ORDER_FAVORITES_CHANGED_EVENT))
  } catch {
    /* 全局拦截器已提示 */
  } finally {
    favoriteLoading.value = false
  }
}

const fetchOrder = async () => {
  loading.value = true
  try {
    const data = await purchaseOrderApi.getById(orderId.value)
    order.value = data ?? null
    if (order.value) {
      refreshTags()
      recordPurchaseOrderRecentView({
        id: String(order.value.id),
        purchaseOrderCode: order.value.purchaseOrderCode,
        vendorName: order.value.vendorName
      })
      await loadFavoriteState()
    } else {
      poFavorited.value = false
    }
  } catch {
    order.value = null
    poFavorited.value = false
  } finally {
    loading.value = false
  }
}

const refreshTags = async () => {
  if (!order.value) return
  try {
    currentTags.value = await tagApi.getEntityTags('PURCHASE_ORDER', order.value.id) || []
  } catch {
    currentTags.value = []
  }
}

const getStatusType = (status: number) => {
  const map: Record<number, string> = { 1: 'info', 2: 'warning', 10: 'success', 20: 'warning', 30: 'primary', 50: 'primary', 100: 'success', [-1]: 'danger', [-2]: 'info' }
  return map[status] ?? 'info'
}
const getStatusText = (status: number) => {
  const map: Record<number, string> = { 1: '新建', 2: '待审核', 10: '审核通过', 20: '待确认', 30: '已确认', 50: '进行中', 100: '采购完成', [-1]: '审核失败', [-2]: '取消' }
  return map[status] ?? '未知'
}
const currencyCodeText = (currency?: number) => {
  const c = Number(currency)
  return CURRENCY_CODE_TO_TEXT[c as keyof typeof CURRENCY_CODE_TO_TEXT] ?? 'RMB'
}
const currencyCodeClass = (currency?: number) => {
  const c = Number(currency)
  if (c === 1 || !Number.isFinite(c)) return 'amount-ccy--rmb'
  return 'amount-ccy--fx'
}
const formatDateTime = (v?: string) => (v ? formatDisplayDateTime(v) : '--')

const handleEdit = () => {
  if (!order.value?.id) return
  router.push({ name: 'PurchaseOrderEdit', params: { id: order.value.id } })
}

</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.purchase-order-detail {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
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

.po-caption-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
}

.caption-avatar-lg {
  width: 48px;
  height: 48px;
  flex-shrink: 0;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.3), rgba(0, 212, 255, 0.2));
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
  color: $cyan-primary;
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
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0;

  &--muted {
    color: rgba(150, 170, 195, 0.82);
  }
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
}

.caption-code {
  font-family: 'Space Mono', monospace;
  font-size: 11px;
  color: $text-muted;
}

.order-type-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 1px 10px;
  height: 22px;
  border-radius: 999px;
  font-size: 12px;
  line-height: 1;
  font-weight: 500;
  white-space: nowrap;
  box-sizing: border-box;
}

.order-type-badge--stocking {
  color: #d48316;
  border: 1px solid rgba(212, 131, 22, 0.55);
  background: rgba(255, 191, 105, 0.14);
}

.title-tags-row {
  margin-top: 6px;
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.btn-add-tag {
  padding: 3px 8px;
  border-radius: 999px;
  border: 1px dashed rgba(0, 212, 255, 0.35);
  background: transparent;
  color: rgba(200, 216, 232, 0.85);
  font-size: 11px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.15s;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
  }
}

.btn-favorite-star {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 4px;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: rgba(200, 220, 240, 0.5);
  cursor: pointer;
  transition: color 0.15s, background 0.15s, transform 0.12s;

  .star-icon {
    width: 22px;
    height: 22px;
    display: block;
  }

  &:hover:not(:disabled) {
    color: #00d4ff;
    background: rgba(0, 212, 255, 0.1);
  }

  &:active:not(:disabled) {
    transform: scale(0.92);
  }

  &.is-favorite {
    color: #ffc94d;
  }

  &.is-favorite:hover:not(:disabled) {
    color: #ffd666;
    background: rgba(255, 201, 77, 0.12);
  }

  &:disabled {
    opacity: 0.45;
    cursor: not-allowed;
  }
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }
}

.btn-success {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: linear-gradient(135deg, rgba(46, 160, 67, 0.88), rgba(70, 191, 145, 0.78));
  border: 1px solid rgba(70, 191, 145, 0.45);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(70, 191, 145, 0.3);
  }
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

.loading-wrap {
  padding: 20px;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
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
  gap: 10px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255,255,255,0.05);
  background: rgba(0,0,0,0.1);
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  &--cyan { background: $cyan-primary; box-shadow: 0 0 6px rgba(0,212,255,0.6); }
}

.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-primary;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 16px 20px;
  border-bottom: 1px solid rgba(255,255,255,0.04);
  border-right: 1px solid rgba(255,255,255,0.04);
  &:nth-child(3n) { border-right: none; }
}

.info-item--span-3 {
  grid-column: 1 / span 3;
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
}

.info-value--code {
  font-family: 'Space Mono', monospace;
  color: $color-ice-blue;
}

.info-value--amount {
  font-family: 'Space Mono', monospace;
  color: $text-primary;
  font-weight: 500;
}

.amount-with-code {
  display: inline-flex;
  align-items: baseline;
  gap: 6px;
}

.amount-ccy {
  font-size: 0.92em;
  font-weight: 500;
}

.amount-ccy--rmb {
  color: #ff4f96;
}

.amount-ccy--fx {
  color: #19c37d;
}

.info-value--time {
  font-size: 12px;
  color: $text-muted;
}

.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  padding: 0 20px 20px;
}

.tabs-nav {
  display: flex;
  border-bottom: 1px solid rgba(255,255,255,0.06);
  padding: 0 16px;
  background: rgba(0,0,0,0.1);
}

.tab-btn {
  padding: 12px 16px;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: $text-muted;
  font-size: 13px;
  cursor: pointer;
  margin-bottom: -1px;
}

.tab-btn--active {
  color: $cyan-primary;
  border-bottom-color: $cyan-primary;
}

.tabs-body {
  padding: 20px;
}

.detail-items-table-wrap {
  margin-top: 4px;
}

.items-table {
  // 与全站 .crm-items-table、销售订单详情一致：勿写死浅色字，浅色主题下行字会发虚难辨
  --el-table-border-color: transparent;
  --el-table-header-bg-color: var(--crm-table-header-bg);
  --el-table-row-hover-bg-color: var(--crm-table-row-hover);
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-text-color: #{$text-primary};
  --el-table-header-text-color: #{$text-muted};
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
  :deep(.el-table) {
    --el-table-text-color: #{$text-primary};
    color: $text-primary;
  }
  :deep(.el-table__inner-wrapper) {
    background: transparent;
    &::before { display: none !important; }
    &::after  { display: none !important; }
  }
  :deep(.el-table__border-left-patch) { display: none !important; }
  :deep(.el-table__header-wrapper) {
    th.el-table__cell {
      background: var(--crm-table-header-bg) !important;
      border-bottom: 1px solid var(--crm-table-header-line) !important;
      border-right: none !important;
      color: $text-muted !important;
      font-size: 12px;
      font-weight: 500;
      letter-spacing: 0.3px;
    }
    th.el-table__cell .cell {
      color: inherit !important;
    }
  }
  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row td.el-table__cell),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row td.el-table__cell) {
    background: transparent !important;
    border-bottom: 1px solid var(--crm-table-header-line);
    border-right: none !important;
    color: $text-primary !important;
    font-size: 13px;
  }
  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row:last-child td.el-table__cell),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row:last-child td.el-table__cell) {
    border-bottom: none !important;
  }
  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row td.el-table__cell .cell),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row td.el-table__cell .cell) {
    color: $text-primary !important;
  }
  :deep(.el-table__cell) {
    .el-button { white-space: nowrap !important; }
    .cell { white-space: nowrap; }
  }
  :deep(.action-btns--po-detail-items) {
    opacity: 1;
    flex-wrap: wrap;
    justify-content: center;
    gap: 4px;
  }
  .po-item-progress-sub {
    margin-top: 2px;
    font-size: 11px;
    color: $text-muted;
    line-height: 1.25;
    white-space: normal;
  }
  :deep(.po-item-progress-qty-col .cell) {
    white-space: normal;
  }
}

.doc-tab-content {
  padding-top: 4px;

  &.doc-tab-content--dragging {
    border: 1px dashed rgba(0, 212, 255, 0.5);
    border-radius: 8px;
    background: rgba(0, 212, 255, 0.03);
  }
}
</style>

<!-- 顶栏「更多」下拉 Teleport 到 body，需非 scoped -->
<style lang="scss">
@import '@/assets/styles/variables.scss';

.po-detail-header-more-popper.el-dropdown__popper,
.po-detail-header-more-popper.el-popper {
  background: $layer-2 !important;
  border: 1px solid rgba(0, 212, 255, 0.15) !important;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.45) !important;
}

.po-detail-header-more-popper .el-dropdown-menu {
  background: transparent !important;
  border: none !important;
  box-shadow: none !important;
  padding: 4px 0 !important;
}

.po-detail-header-more-popper .el-dropdown-menu__item {
  color: rgba(200, 220, 240, 0.92) !important;
  font-size: 13px;

  &:hover,
  &:focus {
    background: rgba(0, 212, 255, 0.1) !important;
    color: #e8f4ff !important;
  }
}

.po-detail-header-more-popper .detail-more-item--danger {
  color: rgba(245, 108, 108, 0.95) !important;
  &:hover,
  &:focus {
    background: rgba(245, 108, 108, 0.12) !important;
    color: #ff9a9a !important;
  }
}
</style>
