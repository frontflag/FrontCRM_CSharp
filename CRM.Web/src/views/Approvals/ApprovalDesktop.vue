<template>
  <div class="approval-desktop-page">
    <div class="ad-nav">
      <el-button size="small" :disabled="!canPrev" @click="goPrev">
        {{ t('approvalDesktop.nav.prev') }}
      </el-button>
      <div class="ad-nav__stats" :title="statsTitle">
        <span>{{ t('approvalDesktop.stats.totalPrefix') }}</span>
        <span class="ad-nav__stats-total">{{ stats.total }}</span>
        <span>{{ t('approvalDesktop.stats.totalSuffix') }}</span>
        <span v-if="statsPartsText">{{ t('approvalDesktop.stats.partsWrap', { parts: statsPartsText }) }}</span>
      </div>
      <el-button size="small" :disabled="!canNext" @click="goNext">
        {{ t('approvalDesktop.nav.next') }}
      </el-button>
    </div>
    <div v-if="selected" class="ad-current-item">
      <el-tag effect="dark" :type="getBizTypeTagType(selected.bizType)" size="small">
        {{ selected.bizTypeName || getBizTypeText(selected.bizType) }}
      </el-tag>
      <span class="ad-current-item__code" :title="selected.documentCode">
        {{ selected.documentCode || '—' }}
      </span>
      <span class="ad-current-item__party" :title="displayCounterpartyName(selected)">
        {{ displayCounterpartyName(selected) }}
      </span>
      <span class="ad-current-item__time">{{ formatDate(selected.createdAt) }}</span>
    </div>
    <div class="ad-workspace" v-loading="loading && !selected">
      <ApprovalAuditWorkspace
        v-if="selected"
        :key="approvalItemKey(selected)"
        :row="selected"
        :read-only="!rowCanDecide(selected)"
        :embedded="true"
        @context="onPartyContext"
        @decided="onDecided"
      />
      <div v-else class="ad-empty">
        {{ t('approvalDesktop.empty.workspace') }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, inject, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import type { PendingApprovalItem } from '@/api/approvals'
import ApprovalAuditWorkspace, {
  type ApprovalAuditPartyContext
} from '@/components/Approvals/ApprovalAuditWorkspace.vue'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import {
  approvalItemKey,
  useApprovalDesktopQueueStore
} from '@/stores/approvalDesktopQueue'
import { useCustomerIntelLookupStore } from '@/stores/customerIntelLookup'
import { useVendorIntelLookupStore } from '@/stores/vendorIntelLookup'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const { t, te } = useI18n()
const route = useRoute()
const router = useRouter()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const queueStore = useApprovalDesktopQueueStore()
const customerIntelLookupStore = useCustomerIntelLookupStore()
const vendorIntelLookupStore = useVendorIntelLookupStore()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const { loading, selected, stats, canPrev, canNext } = storeToRefs(queueStore)

function displayCounterpartyName(row: PendingApprovalItem): string {
  const bt = String(row.bizType || '')
  if (maskPurchaseSensitiveFields.value && (bt === 'VENDOR' || bt === 'PURCHASE_ORDER' || bt === 'FINANCE_PAYMENT')) {
    return '—'
  }
  if (maskSaleSensitiveFields.value && (bt === 'CUSTOMER' || bt === 'SALES_ORDER' || bt === 'FINANCE_RECEIPT')) {
    return '—'
  }
  return row.counterpartyName || '—'
}

const getBizTypeText = (type: string) => {
  const key = `pendingApprovals.bizType.${type}` as const
  return te(key) ? t(key) : type
}

const getBizTypeTagType = (type: string) => {
  const map: Record<string, string> = {
    CUSTOMER: 'success',
    VENDOR: 'warning',
    SALES_ORDER: 'primary',
    PURCHASE_ORDER: 'warning',
    FINANCE_RECEIPT: 'success',
    FINANCE_PAYMENT: 'danger'
  }
  return map[type] || 'info'
}

const formatDate = (dateStr: string) => formatDisplayDateTime(dateStr)

const statsPartsText = computed(() => {
  const s = stats.value
  const parts: string[] = []
  if (s.CUSTOMER > 0) parts.push(t('approvalDesktop.stats.part.customer', { n: s.CUSTOMER }))
  if (s.VENDOR > 0) parts.push(t('approvalDesktop.stats.part.vendor', { n: s.VENDOR }))
  if (s.SALES_ORDER > 0) parts.push(t('approvalDesktop.stats.part.salesOrder', { n: s.SALES_ORDER }))
  if (s.PURCHASE_ORDER > 0) parts.push(t('approvalDesktop.stats.part.purchaseOrder', { n: s.PURCHASE_ORDER }))
  if (s.FINANCE_PAYMENT > 0) parts.push(t('approvalDesktop.stats.part.payment', { n: s.FINANCE_PAYMENT }))
  if (s.FINANCE_RECEIPT > 0) parts.push(t('approvalDesktop.stats.part.receipt', { n: s.FINANCE_RECEIPT }))
  return parts.length ? parts.join(t('approvalDesktop.stats.partSep')) : ''
})

const statsTitle = computed(() => {
  const head =
    t('approvalDesktop.stats.totalPrefix') + String(stats.value.total) + t('approvalDesktop.stats.totalSuffix')
  if (!statsPartsText.value) return head
  return head + t('approvalDesktop.stats.partsWrap', { parts: statsPartsText.value })
})

const rowCanDecide = (row: PendingApprovalItem) => row.canDecide !== false

function clearIntelBindings() {
  customerIntelLookupStore.clearBound()
  vendorIntelLookupStore.clearBound()
}

function applyPartyContext(ctx: ApprovalAuditPartyContext | null) {
  queueStore.setPartyContext(ctx)
  if (!ctx) {
    clearIntelBindings()
    return
  }

  const needsCustomer =
    ctx.bizType === 'CUSTOMER' ||
    ctx.bizType === 'SALES_ORDER' ||
    ctx.bizType === 'FINANCE_RECEIPT'
  const needsVendor =
    ctx.bizType === 'VENDOR' ||
    ctx.bizType === 'PURCHASE_ORDER' ||
    ctx.bizType === 'FINANCE_PAYMENT'

  if (needsCustomer && ctx.customerId) {
    customerIntelLookupStore.bindContext({
      customerId: ctx.customerId,
      companyName: (ctx.customerName || '').trim(),
      creditCode: null,
      region: null,
      salesPersonName: null
    })
    // 审批桌面含「客户调查」且无记录时自动发起调查
    void customerIntelLookupStore.ensureLookup()
  } else {
    customerIntelLookupStore.clearBound()
  }

  if (needsVendor && ctx.vendorId) {
    vendorIntelLookupStore.bindContext({
      vendorId: ctx.vendorId,
      companyName: (ctx.vendorName || '').trim(),
      creditCode: null,
      region: null,
      purchaserName: null
    })
    // 审批桌面含「供应商调查」且无记录时自动发起调查
    void vendorIntelLookupStore.ensureLookup()
  } else {
    vendorIntelLookupStore.clearBound()
  }
}

function onPartyContext(ctx: ApprovalAuditPartyContext | null) {
  applyPartyContext(ctx)
}

function goPrev() {
  clearIntelBindings()
  queueStore.goPrev()
}

function goNext() {
  clearIntelBindings()
  queueStore.goNext()
}

async function onDecided() {
  await queueStore.refreshAfterDecide()
}

watch(
  () => (selected.value ? approvalItemKey(selected.value) : ''),
  () => {
    queueStore.setPartyContext(null)
    clearIntelBindings()
  }
)

async function applyRouteFocusOrDefaultSelection() {
  const focusBiz = typeof route.query.bizType === 'string' ? route.query.bizType.trim() : ''
  const focusId = typeof route.query.businessId === 'string' ? route.query.businessId.trim() : ''
  if (focusBiz && focusId) {
    const ok = queueStore.focusItem(focusBiz, focusId)
    if (!ok) queueStore.pickSelectionAfterFilter()
    if (route.query.bizType != null || route.query.businessId != null) {
      await router.replace({ name: 'ApprovalDesktop', query: {} })
    }
    return
  }

  const hadSelection = !!queueStore.selected
  if (!hadSelection || !queueStore.selected) {
    queueStore.pickSelectionAfterFilter()
    queueStore.requestScrollToSelected()
    return
  }
  const key = approvalItemKey(queueStore.selected)
  const still = queueStore.filteredList.find((x) => approvalItemKey(x) === key)
  queueStore.selectItem(still ?? queueStore.filteredList[0] ?? null)
  queueStore.requestScrollToSelected()
}

onMounted(async () => {
  workspaceLayout?.toggleLeftPanel(true)
  workspaceLayout?.toggleRightPanel(true)
  try {
    await queueStore.refreshAll()
    await applyRouteFocusOrDefaultSelection()
  } catch {
    ElMessage.error(t('pendingApprovals.messages.loadFailed'))
  }
})

watch(
  () => `${String(route.query.bizType ?? '')}:${String(route.query.businessId ?? '')}`,
  async (next, prev) => {
    if (!next || next === ':' || next === prev) return
    const focusBiz = typeof route.query.bizType === 'string' ? route.query.bizType.trim() : ''
    const focusId = typeof route.query.businessId === 'string' ? route.query.businessId.trim() : ''
    if (!focusBiz || !focusId) return
    if (!queueStore.pendingList.length) {
      try {
        await queueStore.refreshAll()
      } catch {
        return
      }
    }
    queueStore.focusItem(focusBiz, focusId)
    await router.replace({ name: 'ApprovalDesktop', query: {} })
  }
)

onUnmounted(() => {
  clearIntelBindings()
  queueStore.setPartyContext(null)
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.approval-desktop-page {
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
  height: calc(100vh - 100px);
  max-height: calc(100vh - 100px);
  overflow: hidden;
  background: $layer-1;
}

.ad-nav {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  gap: 12px;
  margin: 5px 0;
  padding: 10px 14px;
  border-bottom: 1px solid $border-card;
  background: #f5fdff;

  &__stats {
    flex: 1;
    min-width: 0;
    text-align: center;
    font-size: 13px;
    font-weight: 400;
    color: $text-secondary;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__stats-total {
    font-weight: 400;
    color: #d97706;
  }
}

/* 与左栏「待审核」选中卡片同字段：类型标签、单号、对方、提交时间 */
.ad-current-item {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
  padding: 10px 14px;
  border-bottom: 1px solid $border-card;
  background: $layer-2;

  &__code {
    font-size: 14px;
    font-weight: 600;
    color: $text-primary;
    flex-shrink: 0;
  }

  &__party {
    flex: 1;
    min-width: 0;
    font-size: 13px;
    font-weight: 400;
    color: $text-secondary;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__time {
    flex-shrink: 0;
    font-size: 12px;
    font-weight: 400;
    color: $text-muted;
  }
}

.ad-workspace {
  flex: 1;
  min-height: 0;
  overflow: auto;
  padding: 12px 14px 16px;
}

.ad-empty {
  color: $text-muted;
  font-size: 13px;
  line-height: 1.5;
  padding: 48px 16px;
  text-align: center;
}
</style>
