<template>
  <div class="rfq-list-page">
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
          {{ t('rfqRecycle.back') }}
        </button>
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <polyline points="3 6 5 6 21 6"/>
              <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
            </svg>
          </div>
          <h1 class="page-title">{{ t('rfqRecycle.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('rfqRecycle.count', { count: totalCount }) }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="searchForm.keyword"
            type="search"
            class="search-input search-input--w280"
            :placeholder="t('rfqList.filters.searchPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-select
          v-model="searchForm.status"
          :placeholder="t('rfqList.filters.allStatus')"
          clearable
          class="status-select status-select--rfq-status"
          :teleported="false"
        >
          <el-option :label="t('rfqList.status.pending')" :value="0" />
          <el-option :label="t('rfqList.status.assigned')" :value="1" />
          <el-option :label="t('rfqList.status.processing')" :value="2" />
          <el-option :label="t('rfqList.status.quoted')" :value="3" />
          <el-option :label="t('rfqList.status.selected')" :value="4" />
          <el-option :label="t('rfqList.status.converted')" :value="5" />
          <el-option :label="t('rfqList.status.closed')" :value="7" />
          <el-option :label="t('rfqList.status.cancelled')" :value="8" />
        </el-select>
        <template v-if="showRfqSalesUserColumn">
          <div class="search-input-wrap">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="searchForm.salesUserName"
              type="search"
              class="search-input search-input--w160"
              :placeholder="t('rfqList.filters.salesUserPlaceholder')"
              @keyup.enter="handleSearch"
            />
          </div>
        </template>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="searchForm.createUserName"
            type="search"
            class="search-input search-input--w160"
            :placeholder="t('rfqList.filters.createUserPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-select
          v-if="showRfqTagFilter"
          v-model="searchForm.tagIds"
          multiple
          collapse-tags
          collapse-tags-tooltip
          clearable
          filterable
          :placeholder="t('rfqList.filters.tags')"
          class="status-select rfq-tag-filter"
          :teleported="false"
        >
          <el-option v-for="tag in rfqTagFilterOptions" :key="tag.id" :label="tag.name" :value="tag.id" />
        </el-select>
        <el-date-picker
          v-model="searchForm.dateRange"
          type="daterange"
          :range-separator="t('rfqList.filters.createDateSep')"
          :start-placeholder="t('rfqList.filters.createDateFrom')"
          :end-placeholder="t('rfqList.filters.createDateTo')"
          value-format="YYYY-MM-DD"
          class="filter-date-range rfq-list-date-range"
          clearable
          :teleported="false"
        />
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">
          <el-icon><Search /></el-icon>{{ t('rfqList.filters.query') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" @click="handleReset">{{ t('rfqList.filters.reset') }}</button>
      </div>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="rfq-recycle-bin-main"
        :columns="rfqTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="rfqList"
        highlight-current-row
        @row-dblclick="onRfqRowDblClick"
      >
        <template #col-rfqCode="{ row }">
          <el-link type="primary" @click="handleView(row)">{{ row.rfqCode }}</el-link>
        </template>
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="getStatusType(row.status)" size="small">
            {{ getStatusText(row.status) }}
          </el-tag>
        </template>
        <template #col-importance="{ row }">
          <el-rate
            :model-value="rfqImportanceDisplayStars(row.importance)"
            disabled
            :max="3"
            :colors="[...RFQ_IMPORTANCE_RATE_COLORS]"
            :void-color="RFQ_IMPORTANCE_RATE_VOID_COLOR"
          />
        </template>
        <template #col-rfqType="{ row }">
          {{ getTypeText(row.rfqType) }}
        </template>
        <template #col-targetType="{ row }">
          {{ getTargetTypeLabel(row.targetType ?? row.TargetType) }}
        </template>
        <template #col-tags="{ row }">
          <TagListDisplay v-if="resolveRowTags(row).length > 0" :tags="resolveRowTags(row)" />
          <span v-else>—</span>
        </template>
        <template #col-product="{ row }">
          {{ row.product || '—' }}
        </template>
        <template #col-remark="{ row }">
          {{ row.remark || '—' }}
        </template>
        <template #col-itemCount="{ row }">
          <span class="rfq-list-qty">{{ formatItemCountCell(row.itemCount) }}</span>
        </template>
        <template #col-createTime="{ row }">
          <template
            v-for="p in [formatDisplayDateTime2DigitYearParts(row.createTime)]"
            :key="`ct-rfq-${row.id}`"
          >
            <span v-if="p" class="crm-quote-create-time">
              <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
              <span class="crm-quote-create-time__hm">{{ p.time }}</span>
            </span>
            <span v-else>—</span>
          </template>
        </template>
        <template #col-createUser="{ row }">
          {{ row.createUserName || row.CreateUserName || row.createdBy || '—' }}
        </template>
        <template #col-actions-header>
          <div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="opColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="toggleOpCol"
            >
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <button type="button" class="action-btn action-btn--primary" @click.stop="handleView(row)">{{ t('rfqList.actions.view') }}</button>
            </div>
            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="handleView(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('rfqList.actions.view') }}</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>
    </div>

    <div v-if="pageInfo.total > 0" class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button class="list-settings-btn" link type="primary" :aria-label="t('systemUser.colSetting')" @click="dataTableRef?.openColumnSettings?.()">
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
      <el-pagination
        class="quantum-pagination"
        v-model:current-page="pageInfo.page"
        v-model:page-size="pageInfo.pageSize"
        :total="pageInfo.total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="handleSizeChange"
        @current-change="handlePageChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { Search, Setting } from '@element-plus/icons-vue'
import { ElNotification } from 'element-plus'
import { rfqApi } from '@/api/rfq'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import {
  rfqImportanceDisplayStars,
  RFQ_IMPORTANCE_RATE_COLORS,
  RFQ_IMPORTANCE_RATE_VOID_COLOR
} from '@/utils/rfqImportance'
import { formatRfqTypeLabel } from '@/constants/rfqFormEnums'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import TagListDisplay from '@/components/Tag/TagListDisplay.vue'
import { tagApi, type TagDefinitionDto } from '@/api/tag'
import { canUseRfqTagUi } from '@/utils/rfqTagAccess'
import { onCrmDetailListRowDblClick } from '@/utils/crmDetailListRowDblClick'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const authStore = useAuthStore()

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const canViewCustomerInRfq = computed(
  () => authStore.hasPermission('customer.info.read') && !maskSaleSensitiveFields.value
)
const showRfqSalesUserColumn = computed(() => !maskSaleSensitiveFields.value)
const showRfqTagColumn = computed(() => canUseRfqTagUi(authStore.user))
const showRfqTagFilter = showRfqTagColumn
const rfqTagFilterOptions = ref<TagDefinitionDto[]>([])

const loading = ref(false)
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const rfqList = ref<any[]>([])

const searchForm = ref({
  keyword: '',
  status: undefined as number | undefined,
  salesUserName: '',
  createUserName: '',
  tagIds: [] as string[],
  dateRange: null as [string, string] | null
})

const pageInfo = ref({
  page: 1,
  pageSize: 20,
  total: 0
})

const opColExpanded = ref(false)
const LIST_OP_COL_COLLAPSED_WIDTH = 43
const LIST_OP_COL_EXPANDED_WIDTH = 90
const LIST_OP_COL_EXPANDED_MIN_WIDTH = 88
const opColWidth = computed(() =>
  opColExpanded.value ? LIST_OP_COL_EXPANDED_WIDTH : LIST_OP_COL_COLLAPSED_WIDTH
)
const opColMinWidth = computed(() =>
  opColExpanded.value ? LIST_OP_COL_EXPANDED_MIN_WIDTH : LIST_OP_COL_COLLAPSED_WIDTH
)
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const rfqTableColumns = computed((): CrmTableColumnDef[] => {
  const cols: CrmTableColumnDef[] = [
    { key: 'status', label: t('rfqList.columns.status'), prop: 'status', width: 160, align: 'center' as const },
  ]
  if (canViewCustomerInRfq.value) {
    cols.push({ key: 'customerName', label: t('rfqList.columns.customer'), prop: 'customerName', minWidth: 200, showOverflowTooltip: true })
  }
  if (showRfqSalesUserColumn.value) {
    cols.push({
      key: 'salesUserName',
      label: t('rfqList.columns.salesUser'),
      prop: 'salesUserName',
      minWidth: 100,
      width: 108,
      showOverflowTooltip: true
    })
  }
  cols.push(
    { key: 'itemCount', label: t('rfqList.columns.itemCount'), prop: 'itemCount', minWidth: 112, width: 112, align: 'center' as const },
    { key: 'targetType', label: t('rfqList.columns.targetType'), minWidth: 112, width: 112, align: 'center' as const },
    { key: 'rfqType', label: t('rfqList.columns.rfqType'), prop: 'rfqType', minWidth: 112, width: 112 },
    { key: 'industry', label: t('rfqList.columns.industry'), prop: 'industry', minWidth: 100, width: 104, showOverflowTooltip: true },
    { key: 'product', label: t('rfqList.columns.product'), prop: 'product', minWidth: 140, showOverflowTooltip: true },
    { key: 'importance', label: t('rfqList.columns.importance'), prop: 'importance', minWidth: 120, width: 120, align: 'center' as const },
    { key: 'remark', label: t('rfqList.columns.remark'), prop: 'remark', minWidth: 160, showOverflowTooltip: true },
  )
  if (showRfqTagColumn.value) {
    cols.push({
      key: 'tags',
      label: t('rfqList.columns.tags'),
      minWidth: 160,
      width: 180,
      showOverflowTooltip: true,
    })
  }
  cols.push(
    {
      key: 'rfqCode',
      label: t('rfqList.columns.rfqCode'),
      prop: 'rfqCode',
      width: 160,
      minWidth: 160,
      showOverflowTooltip: true,
      sortable: true
    },
    { key: 'createTime', label: t('rfqList.columns.createTime'), width: 160 },
    { key: 'createUser', label: t('rfqList.columns.createUser'), width: 120, showOverflowTooltip: true },
    {
      key: 'actions',
      label: t('rfqList.actions.column'),
      width: opColWidth.value,
      minWidth: opColMinWidth.value,
      fixed: 'right',
      hideable: false,
      pinned: 'end',
      reorderable: false,
      className: 'op-col',
      labelClassName: 'op-col',
      resizable: false
    }
  )
  return cols
})

const totalCount = computed(() => pageInfo.value.total)

const formatItemCountCell = (v: unknown) => {
  if (v == null || v === '') return '—'
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString('zh-CN')
}

const getStatusType = (status: number) => {
  const map: Record<number, string> = {
    0: 'info',
    1: 'warning',
    2: 'primary',
    3: 'success',
    4: 'success',
    5: 'success',
    6: 'info',
    7: 'info',
    8: 'warning'
  }
  return map[status] || 'info'
}

const getStatusText = (status: number) => {
  const map: Record<number, string> = {
    0: t('rfqList.status.pending'),
    1: t('rfqList.status.assigned'),
    2: t('rfqList.status.processing'),
    3: t('rfqList.status.quoted'),
    4: t('rfqList.status.selected'),
    5: t('rfqList.status.converted'),
    6: t('rfqList.status.closed'),
    7: t('rfqList.status.closed'),
    8: t('rfqList.status.cancelled')
  }
  return map[status] || t('rfqList.status.unknown')
}

const getTypeText = (type: number) => {
  const s = formatRfqTypeLabel(type)
  return s === '—' ? t('rfqList.status.unknown') : s
}

const getTargetTypeLabel = (type: unknown) => {
  const n = type == null || type === '' ? NaN : Number(type)
  if (!Number.isFinite(n)) return '—'
  const map: Record<number, string> = {
    1: t('rfqDetail.targetType.priceCompare'),
    2: t('rfqDetail.targetType.exclusive'),
    3: t('rfqDetail.targetType.urgent'),
    4: t('rfqDetail.targetType.normal')
  }
  return map[n] ?? t('rfqList.status.unknown')
}

function resolveRowTags(row: Record<string, unknown>): TagDefinitionDto[] {
  const raw = row.tags ?? row.Tags
  return Array.isArray(raw) ? (raw as TagDefinitionDto[]) : []
}

function emptySearchForm() {
  return {
    keyword: '',
    status: undefined as number | undefined,
    salesUserName: '',
    createUserName: '',
    tagIds: [] as string[],
    dateRange: null as [string, string] | null
  }
}

function parseQueryStringList(raw: unknown): string[] {
  const parts = Array.isArray(raw) ? raw : raw == null || raw === '' ? [] : [raw]
  return parts
    .flatMap((v) => String(v).split(','))
    .map((s) => s.trim())
    .filter(Boolean)
}

function applyRouteQueryToSearchForm() {
  const kw = typeof route.query.keyword === 'string' ? route.query.keyword : ''
  let st: number | undefined = undefined
  const qs = route.query.status
  if (qs !== undefined && qs !== null && qs !== '') {
    const raw = Array.isArray(qs) ? qs[0] : qs
    const n = Number(raw)
    if (!Number.isNaN(n)) st = n === 6 ? 7 : n
  }
  const sd = typeof route.query.startDate === 'string' ? route.query.startDate : ''
  const ed = typeof route.query.endDate === 'string' ? route.query.endDate : ''
  const dateRange: [string, string] | null = sd && ed ? [sd, ed] : null
  const salesUserName = typeof route.query.salesUserName === 'string' ? route.query.salesUserName : ''
  const createUserName = typeof route.query.createUserName === 'string' ? route.query.createUserName : ''
  searchForm.value = {
    keyword: kw,
    status: st,
    salesUserName,
    createUserName,
    tagIds: parseQueryStringList(route.query.tagIds),
    dateRange
  }
}

function searchFormToRouteQuery(): Record<string, string | string[]> {
  const q: Record<string, string | string[]> = {}
  const kw = searchForm.value.keyword.trim()
  if (kw) q.keyword = kw
  if (searchForm.value.status !== undefined && searchForm.value.status !== null) {
    q.status = String(searchForm.value.status)
  }
  const sales = searchForm.value.salesUserName.trim()
  if (sales && showRfqSalesUserColumn.value) q.salesUserName = sales
  const creator = searchForm.value.createUserName.trim()
  if (creator) q.createUserName = creator
  if (searchForm.value.dateRange?.[0]) q.startDate = searchForm.value.dateRange[0]
  if (searchForm.value.dateRange?.[1]) q.endDate = searchForm.value.dateRange[1]
  if (searchForm.value.tagIds?.length) q.tagIds = [...searchForm.value.tagIds]
  return q
}

let syncingOwnQuery = false

function syncRouteQuery() {
  syncingOwnQuery = true
  void router.replace({ name: 'RFQRecycleBin', query: searchFormToRouteQuery() }).finally(() => {
    syncingOwnQuery = false
  })
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await rfqApi.getRecycleBin({
      keyword: searchForm.value.keyword.trim() || undefined,
      status: searchForm.value.status,
      salesUserName: showRfqSalesUserColumn.value
        ? (searchForm.value.salesUserName.trim() || undefined)
        : undefined,
      createUserName: searchForm.value.createUserName.trim() || undefined,
      tagIds: searchForm.value.tagIds?.length ? searchForm.value.tagIds : undefined,
      startDate: searchForm.value.dateRange?.[0],
      endDate: searchForm.value.dateRange?.[1],
      pageNumber: pageInfo.value.page,
      pageSize: pageInfo.value.pageSize
    })
    rfqList.value = res.items || []
    pageInfo.value.total = res.totalCount ?? res.total ?? 0
  } catch (error) {
    ElNotification.error({
      title: t('rfqRecycle.loadFailedTitle'),
      message: getApiErrorMessage(error, t('rfqRecycle.loadFailedMessage'))
    })
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  pageInfo.value.page = 1
  loadData()
  syncRouteQuery()
}

const handleReset = () => {
  searchForm.value = emptySearchForm()
  pageInfo.value.page = 1
  loadData()
  syncRouteQuery()
}

watch(
  showRfqTagFilter,
  () => {
    if (!showRfqTagFilter.value) return
    void tagApi.getTagDefinitions('RFQ').then((items) => {
      rfqTagFilterOptions.value = items
    })
  },
  { immediate: true }
)

watch(
  () => [route.name, route.query] as const,
  () => {
    if (route.name !== 'RFQRecycleBin') return
    if (syncingOwnQuery) return
    applyRouteQueryToSearchForm()
    pageInfo.value.page = 1
    loadData()
  },
  { deep: true, immediate: true }
)

const handleSizeChange = (val: number) => {
  pageInfo.value.pageSize = val
  loadData()
}

const handlePageChange = (val: number) => {
  pageInfo.value.page = val
  loadData()
}

function goBack() {
  router.push({ name: 'RFQList' })
}

const handleView = (row: any) => {
  if (!row?.id) return
  router.push({ name: 'RFQDetail', params: { id: row.id }, query: { from: 'recycle' } })
}

function onRfqRowDblClick(row: any, _column: unknown, event?: MouseEvent) {
  onCrmDetailListRowDblClick(row, _column, event, {
    canEdit: false,
    onEdit: () => undefined,
    onDefault: handleView,
  })
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+SC:wght@300;400;500&display=swap');

.rfq-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  .header-left { display: flex; align-items: center; gap: 12px; }
  .page-title { margin: 0; color: $text-primary; font-size: 20px; }
  .count-badge {
    padding: 3px 10px;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid $border-panel;
    border-radius: 20px;
    font-size: 12px;
    color: $text-muted;
  }
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  background: rgba(255,255,255,0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  &:hover { background: rgba(255,255,255,0.07); color: $text-secondary; border-color: rgba(0,212,255,0.2); }
}

.page-title-group {
  display: flex; align-items: center; gap: 10px;
  .page-icon {
    width: 36px; height: 36px; border-radius: 10px; display: flex; align-items: center; justify-content: center;
    background: rgba(201, 87, 69, 0.12); border: 1px solid rgba(201, 87, 69, 0.25); color: $color-red-brown;
  }
}

.search-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.search-input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.search-icon {
  position: absolute;
  left: 10px;
  color: $text-muted;
  pointer-events: none;
}

.search-input {
  width: 220px;
  padding: 7px 12px 7px 32px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  outline: none;
  transition: border-color 0.2s;

  &::placeholder {
    color: $text-muted;
  }
  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }
}

.search-input--w280 {
  width: 280px;
}

.search-input--w160 {
  width: 160px;
}

.status-select {
  width: 120px;
  :deep(.el-select__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
  :deep(.el-select__placeholder) {
    color: $text-muted !important;
  }
  :deep(.el-select__selected-item) {
    color: $text-primary !important;
  }
}

.status-select--rfq-status {
  width: 140px;
}

.filter-date-range.rfq-list-date-range {
  width: 260px;
  :deep(.el-range-editor.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
}

.rfq-list-page .table-wrapper {
  :deep(.el-table .cell) {
    line-height: 1.2;
  }

  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row:hover),
  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row.hover-row),
  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row.current-row),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row:hover),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row.hover-row),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row.current-row) {
    transform: translateY(-1px);
  }
}

.rfq-list-qty {
  font-weight: 700;
  color: #27292c;
  font-variant-numeric: tabular-nums;
}

html[data-theme='dark'] .rfq-list-qty {
  color: $text-primary;
}

.pagination-wrapper {
  margin-top: 16px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px 16px;
  flex-wrap: wrap;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
  flex-shrink: 0;
}

.list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.list-footer-density-anchor {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  min-height: 0;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
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
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  letter-spacing: 0.5px;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}

.btn-ghost {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
}

.quantum-pagination {
  :deep(.el-pagination__total) { color: $text-muted; }
}

.op-col-toggle-btn {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  flex: 0 0 auto;
}

.op-more-trigger {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  opacity: 0;
  transition: opacity 0.15s;
}

:deep(.el-table__body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__body-wrapper .el-table__body tr.hover-row .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr.hover-row .op-more-trigger) {
  opacity: 1;
}

.op-more-item {
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
}

.op-more-item--primary {
  color: $cyan-primary;
}
</style>
