<template>
  <div class="crm-biz-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M12 3v18" />
              <path d="M5 10h14" />
              <path d="M7 21h10a2 2 0 0 0 2-2V8L16 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2z" />
            </svg>
          </div>
          <h1 class="page-title">{{ pageTitle }}</h1>
        </div>
        <div class="count-badge">{{ t('commissionResult.count', { count: total }) }}</div>
      </div>
      <div v-if="canAdmin" class="header-right">
        <button
          v-if="!official"
          type="button"
          class="btn-primary btn-sm"
          :disabled="running"
          @click="onRecalc"
        >
          {{ t('commissionResult.recalc') }}
        </button>
        <button
          v-else
          type="button"
          class="btn-primary btn-sm"
          :disabled="running"
          @click="onLock"
        >
          {{ t('commissionResult.lock') }}
        </button>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="keyword"
            class="search-input"
            :placeholder="t('commissionResult.keywordPlaceholder')"
            @keyup.enter="reload"
          />
        </div>
        <el-select
          v-if="official"
          v-model="term"
          class="status-select status-select--wide"
          :placeholder="t('commissionResult.term')"
          :teleported="false"
          @change="reload"
        >
          <el-option v-for="item in terms" :key="item.term" :label="item.label" :value="item.term" />
        </el-select>
        <button type="button" class="btn-primary btn-sm" @click="reload">{{ t('commissionResult.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetSearch">{{ t('commissionResult.reset') }}</button>
      </div>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        v-show="loading || rows.length > 0"
        ref="dataTableRef"
        :column-layout-key="columnKey"
        :columns="tableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="rows"
        row-key="rowKey"
        highlight-current-row
        @row-dblclick="onRowDblclick"
      >
        <template #col-userName="{ row }">
          <span class="dock-quote-tier-line">{{ row.userName || '—' }}</span>
        </template>
        <template #col-monthCount="{ row }">
          <span class="dock-quote-tier-line">{{ row.monthCount }}</span>
        </template>
        <template #col-periodGpUsd="{ row }">
          <ReceivableStatementMoneyCell :amount="row.periodGpUsd" :currency="usdCurrency" />
        </template>
        <template #col-commissionUsd="{ row }">
          <ReceivableStatementMoneyCell :amount="row.commissionUsd" :currency="usdCurrency" />
        </template>
      </CrmDataTable>
      <div v-show="!loading && rows.length === 0" class="empty-state">
        <p>{{ t('commissionResult.empty') }}</p>
      </div>
    </div>

    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('systemUser.colSetting')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true" />
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="load"
        @size-change="onPageSizeChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import CrmDataTable from '@/components/CrmDataTable.vue'
import ReceivableStatementMoneyCell from '@/views/Finance/ReceivableStatementMoneyCell.vue'
import { CurrencyCode } from '@/constants/currency'
import { useAuthStore } from '@/stores'
import {
  COMMISSION_ROLE_PURCHASE,
  commissionApi,
  type CommissionSummaryRow,
  type CommissionTermOption
} from '@/api/commission'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

type SummaryTableRow = CommissionSummaryRow & { rowKey: string }

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const official = computed(() => route.meta.commissionMode === 'official')
const roleType = computed(() => (Number(route.meta.commissionRoleType) === 2 ? 2 : 1))
const isPurchase = computed(() => roleType.value === COMMISSION_ROLE_PURCHASE)
const canAdmin = computed(() => authStore.canForceDelete())
const usdCurrency = CurrencyCode.USD

const pageTitle = computed(() => {
  if (official.value) {
    return isPurchase.value
      ? t('layout.menu.commissionOfficialPurchase')
      : t('layout.menu.commissionOfficialSales')
  }
  return isPurchase.value
    ? t('layout.menu.commissionEstimatedPurchase')
    : t('layout.menu.commissionEstimatedSales')
})

const personLabel = computed(() =>
  isPurchase.value ? t('commissionResult.purchaser') : t('commissionResult.salesperson')
)

const amountLabel = computed(() =>
  official.value ? t('commissionResult.officialAmount') : t('commissionResult.estimatedAmount')
)

const columnKey = computed(
  () => `commission-result-${official.value ? 'official' : 'estimated'}-${roleType.value}-v4`
)

const loading = ref(false)
const running = ref(false)
const keyword = ref('')
const term = ref('')
const terms = ref<CommissionTermOption[]>([])
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const rows = ref<SummaryTableRow[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

function headerMin(label: string) {
  return estimateListColumnHeaderMinWidth(label)
}

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  const person = personLabel.value
  const level = t('commissionResult.userLevel')
  const months = t('commissionResult.monthCount')
  const gp = t('commissionResult.periodGp')
  const amt = amountLabel.value
  const personW = Math.max(160, headerMin(person))
  const gpW = Math.max(160, headerMin(gp))
  const amtW = Math.max(160, headerMin(amt))
  return [
    {
      key: 'userName',
      label: person,
      prop: 'userName',
      width: personW,
      minWidth: personW,
      showOverflowTooltip: true
    },
    { key: 'userLevel', label: level, width: Math.max(88, headerMin(level)), align: 'center', prop: 'userLevel' },
    { key: 'monthCount', label: months, width: Math.max(96, headerMin(months)), align: 'right' },
    { key: 'periodGpUsd', label: gp, width: gpW, minWidth: gpW, align: 'right' },
    { key: 'commissionUsd', label: amt, width: amtW, minWidth: amtW, align: 'right' },
    {
      key: 'flexGutter',
      label: '',
      minWidth: 48,
      hideable: false,
      reorderable: false,
      pinned: 'end',
      resizable: false,
      className: 'commission-result-flex-col',
      labelClassName: 'commission-result-flex-col'
    }
  ]
})

function queryParams() {
  return {
    roleType: roleType.value,
    keyword: keyword.value.trim() || undefined,
    term: official.value ? term.value || undefined : undefined,
    page: page.value,
    pageSize: pageSize.value
  }
}

async function load() {
  loading.value = true
  try {
    const data = official.value
      ? await commissionApi.officialSummary(queryParams())
      : await commissionApi.estimatedSummary(queryParams())
    rows.value = (data.items || []).map((r) => ({ ...r, rowKey: r.userId }))
    total.value = data.totalCount
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('commissionResult.loadFailed'))
  } finally {
    loading.value = false
  }
}

function reload() {
  page.value = 1
  void load()
}

function resetSearch() {
  keyword.value = ''
  if (official.value) term.value = terms.value[0]?.term || ''
  reload()
}

function onPageSizeChange() {
  page.value = 1
  void load()
}

function onRowDblclick(row: SummaryTableRow) {
  const query: Record<string, string> = {}
  if (row.userName) query.userName = row.userName
  if (official.value && term.value) query.term = term.value
  router.push({
    name: official.value
      ? isPurchase.value
        ? 'CommissionOfficialPurchasePerson'
        : 'CommissionOfficialSalesPerson'
      : isPurchase.value
        ? 'CommissionEstimatedPurchasePerson'
        : 'CommissionEstimatedSalesPerson',
    params: { userId: row.userId },
    query
  })
}

async function onRecalc() {
  try {
    await ElMessageBox.confirm(t('commissionResult.recalcConfirm'), t('commissionResult.recalc'), {
      type: 'warning'
    })
  } catch {
    return
  }
  running.value = true
  try {
    const r = await commissionApi.recalcEstimated()
    ElMessage.success(
      t('commissionResult.recalcDone', { sales: r.salesCount, purchase: r.purchaseCount })
    )
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('commissionResult.runFailed'))
  } finally {
    running.value = false
  }
}

async function onLock() {
  try {
    await ElMessageBox.confirm(t('commissionResult.lockConfirm'), t('commissionResult.lock'), {
      type: 'warning'
    })
  } catch {
    return
  }
  running.value = true
  try {
    const r = await commissionApi.lockOfficial()
    ElMessage.success(t('commissionResult.lockDone', { term: r.term, n: r.inserted }))
    await bootstrapOfficial()
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('commissionResult.runFailed'))
  } finally {
    running.value = false
  }
}

async function bootstrapOfficial() {
  terms.value = await commissionApi.officialTerms(roleType.value)
  if (!term.value && terms.value.length > 0) term.value = terms.value[0].term
}

onMounted(async () => {
  try {
    if (official.value) await bootstrapOfficial()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('commissionResult.loadFailed'))
  }
  await load()
})
</script>

<style lang="scss">
@import '@/assets/styles/crm-biz-list-page.scss';

.crm-biz-list-page .commission-result-flex-col .cell {
  padding: 0;
}
</style>
