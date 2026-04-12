<template>
  <div class="customer-list-page">
    <!-- 页面标题和操作按钮 -->
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
              <circle cx="9" cy="7" r="4"/>
              <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
              <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
            </svg>
          </div>
          <h1 class="page-title">{{ t('customerList.title') }}</h1>
        </div>
        <div class="customer-count-badge">{{ t('customerList.count', { count: totalCount }) }}</div>
      </div>
      <div class="header-right">
        <template v-if="canSubmitAudit">
          <div class="btn-split-group">
            <button type="button" class="btn-success" @click="handleCreate">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="5" x2="12" y2="19"/>
                <line x1="5" y1="12" x2="19" y2="12"/>
              </svg>
              {{ t('customerList.create') }}
            </button>
            <el-dropdown trigger="click" @command="onCreateDropdownCommand">
              <button type="button" class="btn-success btn-success--caret" :aria-label="t('customerList.expandMenu')">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="6 9 12 15 18 9" />
                </svg>
              </button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="import">{{ t('customerList.importExcel') }}</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
        <button v-else type="button" class="btn-success" @click="handleCreate">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"/>
            <line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          {{ t('customerList.create') }}
        </button>
      </div>
    </div>

    <!-- 搜索栏：关键词 → 状态 → 级别 → 类型 → 行业 → 业务员 → 创建日期区间 -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">{{ t('customerList.filters.keyword') }}</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
          </svg>
          <input
            v-model="searchForm.searchTerm"
            class="search-input"
            :placeholder="canViewCustomerInfo ? t('customerList.filters.keywordPlaceholderFull') : t('customerList.filters.keywordPlaceholderCode')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-select v-model="searchForm.status" :placeholder="t('customerList.filters.allStatus')" clearable class="status-select" :teleported="false" @change="handleSearch">
          <el-option
            v-for="opt in workflowStatusOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-select v-model="searchForm.customerLevel" :placeholder="t('customerList.filters.allLevel')" clearable class="status-select" :teleported="false" @change="handleSearch">
          <el-option
            v-for="opt in customerDict.levelStringOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-select v-model="searchForm.customerType" :placeholder="t('customerList.filters.allType')" clearable class="status-select" :teleported="false" @change="handleSearch">
          <el-option
            v-for="opt in customerDict.typeSelectOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-select v-model="searchForm.industry" :placeholder="t('customerList.filters.allIndustry')" clearable class="status-select" :teleported="false" @change="handleSearch">
          <el-option
            v-for="opt in customerDict.industryOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.label"
          />
        </el-select>
        <el-select
          v-model="searchForm.salesPersonId"
          :placeholder="t('customerList.filters.allSalesUsers')"
          clearable
          filterable
          class="status-select status-select--sales"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option
            v-for="u in salesUsers"
            :key="u.id"
            :label="salesUserLabel(u)"
            :value="u.id"
          />
        </el-select>
        <el-date-picker
          v-model="createdDateRange"
          type="daterange"
          :range-separator="t('customerList.filters.to')"
          :start-placeholder="t('customerList.filters.startDate')"
          :end-placeholder="t('customerList.filters.endDate')"
          value-format="YYYY-MM-DD"
          clearable
          class="filter-date-range"
          :teleported="false"
          @change="onCreatedRangeChange"
        />
        <button class="btn-primary btn-sm" @click="handleSearch">{{ t('customerList.filters.search') }}</button>
        <button class="btn-ghost btn-sm" @click="handleReset">{{ t('customerList.filters.reset') }}</button>
      </div>
    </div>

    <!-- 表格列表 -->
    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="customer-list-main"
        :columns="customerTableColumns"
        :show-column-settings="false"
        :data="customerList"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        row-key="id"
        @row-dblclick="handleView"
      >
        <template #col-status="{ row }">
          <span class="status-dot" :class="getStatusDotClass(row.status)">
            {{ getStatusText(row.status) }}
          </span>
        </template>
        <template #col-customerName="{ row }">
          <div class="customer-name-cell">
            <div class="cell-avatar">
              <span>{{ (row.customerName || row.customerShortName || '?')[0] }}</span>
            </div>
            <div class="cell-name-group">
              <div class="cell-name-line">
                <span
                  class="cell-name"
                  :class="{ 'party-entity-name--muted': isPartyStatusMuted(row) }"
                >{{ row.customerName || row.customerShortName }}</span>
                <PartyStatusIcons
                  :entity-id="row.id"
                  :frozen="!!row.disenableStatus"
                  :blacklist="!!row.blackList"
                  size="sm"
                />
              </div>
              <div class="cell-short" v-if="row.customerShortName && row.customerShortName !== row.customerName">
                {{ row.customerShortName }}
              </div>
            </div>
          </div>
        </template>
        <template #col-customerType="{ row }">
          <span class="badge badge-type" v-if="row.customerType">{{ customerDict.typeLabel(row.customerType) }}</span>
        </template>
        <template #col-customerLevel="{ row }">
          <span class="badge" :class="`badge-level-${(row.customerLevel || '').toLowerCase()}`" v-if="row.customerLevel">
            {{ customerDict.levelLabel(row.customerLevel) }}
          </span>
        </template>
        <template #col-industry="{ row }"><span class="td-muted">{{ customerDict.industryLabel(row.industry) }}</span></template>
        <template #col-contactName="{ row }">
          <span class="td-contact">{{ row.contacts && row.contacts.length > 0 ? row.contacts[0].contactName : '--' }}</span>
        </template>
        <template #col-mobilePhone="{ row }">
          <span class="td-phone">{{ row.contacts && row.contacts.length > 0 ? row.contacts[0].mobilePhone : '--' }}</span>
        </template>
        <template #col-region="{ row }"><span class="td-muted">{{ row.city || row.region || '--' }}</span></template>
        <template #col-createdAt="{ row }"><span class="td-muted">{{ formatDate(row.createdAt ?? (row as any).createTime) }}</span></template>
        <template #col-createUser="{ row }">
          <span class="td-muted">{{ (row as any).createUserName || (row as any).createdBy || (row as any).salesPersonName || '--' }}</span>
        </template>
        <template #col-actions-header>
          <div class="op-col-header">
            <span class="op-col-header-text">{{ t('customerList.actions.column') }}</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns always-visible">
              <button class="action-btn action-btn--primary" @click.stop="handleView(row)">{{ t('customerList.actions.detail') }}</button>
              <button class="action-btn action-btn--primary" @click.stop="handleEdit(row)">{{ t('customerList.actions.edit') }}</button>
              <button
                v-if="row.status === 1 || row.status === -1"
                class="action-btn action-btn--warning"
                @click.stop="handleSubmitAudit(row)"
              >
                {{ t('customerList.actions.submitAudit') }}
              </button>
            </div>
            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="handleView(row)">{{ t('customerList.actions.detail') }}</el-dropdown-item>
                  <el-dropdown-item @click.stop="handleEdit(row)">{{ t('customerList.actions.edit') }}</el-dropdown-item>
                  <el-dropdown-item v-if="row.status === 1 || row.status === -1" @click.stop="handleSubmitAudit(row)">
                    {{ t('customerList.actions.submitAudit') }}
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>

      <div v-if="!loading && customerList.length === 0" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
          <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/>
        </svg>
        <p>{{ t('customerList.empty') }}</p>
        <template v-if="canSubmitAudit">
          <div class="btn-split-group empty-split">
            <button type="button" class="btn-success" @click="handleCreate">{{ t('customerList.create') }}</button>
            <el-dropdown trigger="click" @command="onCreateDropdownCommand">
              <button type="button" class="btn-success btn-success--caret" :aria-label="t('customerList.expandMenu')">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="6 9 12 15 18 9" />
                </svg>
              </button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="import">{{ t('customerList.importExcel') }}</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
        <button v-else type="button" class="btn-success" @click="handleCreate">{{ t('customerList.create') }}</button>
      </div>
    </div>

    <!-- 分页 -->
    <div class="pagination-wrapper" v-if="totalCount > 0">
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
        v-model:current-page="pagination.pageNumber"
        v-model:page-size="pagination.pageSize"
        :page-sizes="[20, 50, 100]"
        :total="totalCount"
        layout="total, sizes, prev, pager, next"
        @size-change="handleSizeChange"
        @current-change="handlePageChange"
        class="quantum-pagination"
      />
    </div>

    <CustomerImportDialog v-model="importDialogVisible" @success="fetchCustomerList" />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, watch, nextTick, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n'
import { ElNotification, ElMessageBox } from 'element-plus';
import { Setting } from '@element-plus/icons-vue'
import { customerApi } from '@/api/customer';
import { authApi } from '@/api/auth';
import { favoriteApi } from '@/api/favorite';
import { formatDisplayDateTime } from '@/utils/displayDateTime';
import { type Customer, type CustomerSearchRequest } from '@/types/customer';
import { useAuthStore } from '@/stores/auth';
import { useCustomerDictStore } from '@/stores/customerDict';
import PartyStatusIcons from '@/components/party/PartyStatusIcons.vue';
import CustomerImportDialog from '@/components/Customer/CustomerImportDialog.vue';
import CrmDataTable from '@/components/CrmDataTable.vue'
import { buildCustomerListQuery, parseCustomerListQuery } from '@/utils/customerListQuery';
import { CUSTOMER_WORKFLOW_STATUS_OPTIONS } from '@/constants/customerWorkflowStatus';
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter();
const route = useRoute();
const { t } = useI18n()

function isPartyStatusMuted(c: Customer) {
  return !!(c.disenableStatus || c.blackList);
}
const authStore = useAuthStore();
const customerDict = useCustomerDictStore();
/** 联系人、电话等敏感列；与 RBAC 节点 customer.info.read 一致 */
const canViewCustomerInfo = authStore.hasPermission('customer.info.read')
/** 列表页已要求 customer.read；客户名称与编号同属基础识别信息，不应依赖 info.read（生产常见仅绑 read 导致无名称列） */
const canViewCustomerNameColumn = authStore.hasPermission('customer.read')
const canSubmitAudit = authStore.hasPermission('customer.write');

const importDialogVisible = ref(false);
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
/** 行高切换 Teleport 锚点（与列设置齿轮同排） */
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 220
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? 220 : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const loading = ref(false);
const customerList = ref<Customer[]>([]);
const totalCount = ref(0);
const favoriteOnly = ref(false);
/** 列表内 replace 查询串时避免 watch 与手动 fetch 重复请求 */
const skipQueryRouteFetch = ref(false);

const workflowStatusOptions = CUSTOMER_WORKFLOW_STATUS_OPTIONS;

type SalesUserOption = { id: string; userName: string; realName?: string; label?: string };

const salesUsers = ref<SalesUserOption[]>([]);
const createdDateRange = ref<[string, string] | null>(null);

const searchForm = reactive<CustomerSearchRequest>({
  pageNumber: 1,
  pageSize: 20,
  searchTerm: '',
  customerType: undefined,
  customerLevel: undefined,
  industry: undefined,
  region: undefined,
  salesPersonId: undefined,
  createdFrom: undefined,
  createdTo: undefined,
  status: undefined,
  isActive: undefined,
  sortBy: 'CreatedAt',
  sortDescending: true
});

const pagination = reactive({ pageNumber: 1, pageSize: 20 });

const customerTableColumns = computed<CrmTableColumnDef[]>(() => {
  const cols: CrmTableColumnDef[] = [
    { key: 'status', label: t('customerList.columns.status'), prop: 'status', width: 160, align: 'center' },
    ...(canViewCustomerNameColumn
      ? [{ key: 'customerName', label: t('customerList.columns.customerName'), minWidth: 200, showOverflowTooltip: true } as CrmTableColumnDef]
      : []),
    { key: 'customerType', label: t('customerList.columns.type'), width: 80, align: 'center' },
    { key: 'customerLevel', label: t('customerList.columns.level'), width: 80, align: 'center' },
    { key: 'industry', label: t('customerList.columns.industry'), width: 110, showOverflowTooltip: true },
  ]
  if (canViewCustomerInfo) {
    cols.push(
      { key: 'contactName', label: t('customerList.columns.contactName'), width: 130, showOverflowTooltip: true },
      { key: 'mobilePhone', label: t('customerList.columns.mobilePhone'), width: 130, showOverflowTooltip: true }
    )
  }
  cols.push(
    { key: 'region', label: t('customerList.columns.region'), minWidth: 100, showOverflowTooltip: true },
    { key: 'customerCode', label: t('customerList.columns.customerCode'), prop: 'customerCode', width: 160, minWidth: 160, showOverflowTooltip: true },
    { key: 'createdAt', label: t('customerList.columns.createdAt'), width: 160 },
    { key: 'createUser', label: t('customerList.columns.createUser'), width: 120, showOverflowTooltip: true }
  )
  cols.push({
    key: 'actions',
    label: t('customerList.actions.column'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    hideable: false,
    pinned: 'end',
    reorderable: false
  })
  return cols
})

function salesUserLabel(u: SalesUserOption) {
  const name = u.realName || u.label || u.userName;
  return u.userName && name !== u.userName ? `${name}（${u.userName}）` : name;
}

const fetchCustomerList = async () => {
  loading.value = true;
  try {
    const params: CustomerSearchRequest = {
      ...searchForm,
      pageNumber: pagination.pageNumber,
      pageSize: pagination.pageSize
    };
    const [response, favoriteIds] = await Promise.all([
      customerApi.searchCustomers(params),
      favoriteApi.getFavoriteEntityIds('CUSTOMER')
    ]);
    const favoriteSet = new Set(favoriteIds);
    let mapped = response.items.map((item: any) => ({
      ...item,
      customerName: item.customerName || item.officialName,
      customerShortName: item.customerShortName || item.nickName,
      customerLevel: item.customerLevel || (item.level ? ['', 'D', 'C', 'B', 'BPO', 'VIP', 'VPO'][item.level] : 'B'),
      customerType: item.customerType ?? item.type ?? 0,
      salesPersonName: item.salesPersonName,
      creditLimit: item.creditLimit ?? item.creditLine ?? 0,
      balance: item.balance ?? item.creditLineRemain ?? 0,
      // 新状态体系：已审核及以后视为“有效/正常”
      isActive: item.isActive ?? (Number(item.status ?? 0) >= 10),
      isFavorite: favoriteSet.has(item.id),
      contacts: item.contacts || [],
      // 后端 CustomerInfo 序列化字段为 createTime，与前端 Customer.createdAt 对齐
      createdAt: item.createdAt ?? item.createTime
    }));
    if (favoriteOnly.value) {
      mapped = mapped.filter((item: any) => item.isFavorite);
    }
    // 兜底：确保按创建日期（createdAt）降序展示
    mapped.sort((a: any, b: any) => (parseDateMs(b?.createdAt) - parseDateMs(a?.createdAt)));
    customerList.value = mapped;
    totalCount.value = favoriteOnly.value ? mapped.length : response.totalCount;
  } catch (error: any) {
    // 仅在真实网络/服务器错误时提示，空数据不报错
    const isEmptyResult = !error?.response || error?.response?.status === 404;
    if (!isEmptyResult) {
      console.error(t('customerList.errors.fetchFailed'), error);
      ElNotification.error({ title: t('customerList.errors.loadTitle'), message: t('customerList.errors.fetchFailed') });
    } else {
      customerList.value = [];
      totalCount.value = 0;
    }
  } finally {
    loading.value = false;
  }
};

function syncSearchFromRouteQuery() {
  const p = parseCustomerListQuery(route.query);
  searchForm.searchTerm = p.searchTerm;
  searchForm.customerType = p.customerType;
  searchForm.customerLevel = p.customerLevel;
  searchForm.industry = p.industry;
  searchForm.status = p.status;
  searchForm.salesPersonId = p.salesUserId;
  searchForm.createdFrom = p.createdFrom;
  searchForm.createdTo = p.createdTo;
  createdDateRange.value =
    p.createdFrom && p.createdTo ? [p.createdFrom, p.createdTo] : null;
  favoriteOnly.value = p.favoriteOnly;
}

function replaceRouteQueryAndFetch() {
  skipQueryRouteFetch.value = true;
  router
    .replace({
      name: 'CustomerList',
      query: buildCustomerListQuery({
        searchTerm: searchForm.searchTerm || '',
        customerType: searchForm.customerType,
        customerLevel: searchForm.customerLevel,
        industry: searchForm.industry,
        status: searchForm.status,
        salesUserId: searchForm.salesPersonId,
        createdFrom: searchForm.createdFrom,
        createdTo: searchForm.createdTo,
        favoriteOnly: favoriteOnly.value
      })
    })
    .finally(() => {
      nextTick(() => {
        skipQueryRouteFetch.value = false;
      });
    });
  fetchCustomerList();
}

const handleSearch = () => {
  pagination.pageNumber = 1;
  replaceRouteQueryAndFetch();
};

const handleReset = () => {
  Object.assign(searchForm, {
    searchTerm: '',
    customerType: undefined,
    customerLevel: undefined,
    industry: undefined,
    region: undefined,
    salesPersonId: undefined,
    createdFrom: undefined,
    createdTo: undefined,
    status: undefined,
    isActive: undefined
  });
  createdDateRange.value = null;
  favoriteOnly.value = false;
  pagination.pageNumber = 1;
  skipQueryRouteFetch.value = true;
  router.replace({ name: 'CustomerList', query: {} }).finally(() => {
    nextTick(() => {
      skipQueryRouteFetch.value = false;
    });
  });
  fetchCustomerList();
};

function onCreatedRangeChange(val: [string, string] | null | undefined) {
  if (val && val.length === 2) {
    searchForm.createdFrom = val[0];
    searchForm.createdTo = val[1];
  } else {
    searchForm.createdFrom = undefined;
    searchForm.createdTo = undefined;
  }
  handleSearch();
}

const handleCreate = () => router.push('/customers/create');

function onCreateDropdownCommand(cmd: string) {
  if (cmd === 'import') importDialogVisible.value = true;
}
const handleView = (row: Customer) => router.push(`/customers/${row.id}`);
const handleEdit = (row: Customer) => router.push(`/customers/${row.id}/edit`);

const getStatusText = (status: number | undefined) => {
  const s = Number(status ?? 0);
  const map: Record<number, string> = {
    1: t('customerList.status.new'),
    2: t('customerList.status.pending'),
    10: t('customerList.status.approved'),
    12: t('customerList.status.pendingFinance'),
    20: t('customerList.status.financeFiled'),
    [-1]: t('customerList.status.failed')
  };
  return map[s] ?? String(status ?? '');
};

const getStatusDotClass = (status: number | undefined) => {
  const s = Number(status ?? 0);
  if (s === 10 || s === 20) return 'status-active';
  if (s === 2 || s === 12) return 'status-warning';
  if (s === -1) return 'status-danger';
  return 'status-inactive';
};

const handleSubmitAudit = async (row: Customer) => {
  if (!canSubmitAudit) {
    ElNotification.warning({ title: t('customerList.audit.noPermissionTitle'), message: t('customerList.audit.noPermissionMessage') });
    return;
  }
  await ElMessageBox.confirm(
    t('customerList.audit.confirmMessage'),
    t('customerList.audit.confirmTitle'),
    { type: 'warning', confirmButtonText: t('customerList.audit.confirmButton'), cancelButtonText: t('common.cancel') }
  );
  try {
    loading.value = true;
    await customerApi.submitAudit(row.id);
    ElNotification.success({ title: t('customerList.audit.successTitle'), message: t('customerList.audit.successMessage') });
    await fetchCustomerList();
  } catch (error: any) {
    ElNotification.error({ title: t('customerList.audit.failedTitle'), message: error?.message || t('customerList.audit.failedMessage') });
  } finally {
    loading.value = false;
  }
};

const handleSizeChange = (size: number) => { pagination.pageSize = size; fetchCustomerList(); };
const handlePageChange = (page: number) => { pagination.pageNumber = page; fetchCustomerList(); };

const parseDateMs = (v?: string) => {
  if (!v) return 0;
  const t = new Date(v).getTime();
  return Number.isFinite(t) ? t : 0;
};

const formatDate = (v?: string) => {
  return formatDisplayDateTime(v);
};

watch(
  () => route.query,
  () => {
    if (route.name !== 'CustomerList') return;
    if (skipQueryRouteFetch.value) return;
    syncSearchFromRouteQuery();
    pagination.pageNumber = 1;
    fetchCustomerList();
  },
  { deep: true }
);

onMounted(async () => {
  void customerDict.ensureLoaded();
  syncSearchFromRouteQuery();
  try {
    salesUsers.value = await authApi.getSalesUsersForSelect();
  } catch {
    salesUsers.value = [];
  }
  fetchCustomerList();
});
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.customer-list-page {
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
  margin-bottom: 20px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 10px;
  }
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;

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
    letter-spacing: 0.5px;
  }
}

.customer-count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

// ---- 按钮 ----
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

  &.btn-sm { padding: 6px 12px; font-size: 12px; }
}

// 新建/新增/创建（UI 规范：success 绿）
.btn-success {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(46, 160, 67, 0.85), rgba(70, 191, 145, 0.75));
  border: 1px solid rgba(70, 191, 145, 0.45);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  letter-spacing: 0.5px;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(70, 191, 145, 0.3);
  }

  &.btn-success--caret {
    border-top-left-radius: 0;
    border-bottom-left-radius: 0;
    min-width: 38px;
    padding-left: 10px;
    padding-right: 10px;
  }
}

.btn-split-group {
  display: inline-flex;
  align-items: stretch;

  .btn-success:first-child {
    border-top-right-radius: 0;
    border-bottom-right-radius: 0;
  }

  &.empty-split {
    margin-top: 12px;
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

// ---- 搜索栏 ----
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

.filter-field-label {
  font-size: 12px;
  font-weight: 500;
  color: $text-muted;
  white-space: nowrap;
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

  &::placeholder { color: $text-muted; }
  &:focus { border-color: rgba(0,212,255,0.4); }
}

.status-select {
  width: 120px;
  :deep(.el-select__wrapper) { background: $layer-2 !important; box-shadow: none !important; border: 1px solid $border-panel !important; border-radius: $border-radius-md !important; }
  :deep(.el-select__placeholder) { color: $text-muted !important; }
  :deep(.el-select__selected-item) { color: $text-primary !important; }
}

.status-select--sales {
  width: 150px;
}

/* 仅保留容纳 YYYY-MM-DD ×2 +「至」的宽度 */
.filter-date-range {
  width: 218px;
  flex-shrink: 0;
  :deep(.el-range-editor.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    padding-inline: 6px;
  }
  :deep(.el-range-input) {
    color: $text-primary !important;
    font-size: 12px !important;
    width: 82px !important;
    min-width: 82px !important;
    max-width: 82px !important;
    flex: 0 0 82px !important;
  }
  :deep(.el-range-separator) {
    color: $text-muted !important;
    flex-shrink: 0;
    padding: 0 2px;
    font-size: 12px;
  }
}

// ---- 表格：.table-wrapper / .data-table 全局样式见 assets/styles/crm-unified-list.scss ----
// 数据行行高（td 垂直 padding 之和）由 CrmDataTable row-density + crm-list-table-row-density.scss 控制；此处仅保留本页单元格内容紧凑样式
.customer-list-page .table-wrapper {
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

  .customer-name-cell {
    gap: 6px;
  }

  .cell-avatar {
    width: 22px;
    height: 22px;
    border-radius: 6px;
    font-size: 11px;
  }

  .cell-name {
    font-size: 12px;
  }

  .cell-short {
    font-size: 10px;
    margin-top: 0;
    line-height: 1.15;
  }

  .badge {
    font-size: 10px;
    padding: 0 5px;
    line-height: 1.35;
  }

  .status-dot {
    font-size: 11px;
    padding: 1px 6px;
    gap: 4px;

    &::before {
      width: 5px;
      height: 5px;
    }
  }

  .td-muted,
  .td-contact {
    font-size: 11px;
  }

  .td-phone {
    font-size: 11px;
  }

  .td-code {
    font-size: 11px;
  }

  .action-btn {
    padding: 2px 8px;
    font-size: 11px;
  }

  .op-col-header-text {
    font-size: 11px;
  }
}

.table-row {
  cursor: pointer;
  transition: background 0.15s;

  &:hover {
    background: rgba(0, 212, 255, 0.04);

    .action-btns { opacity: 1; }
  }

  &:last-child td { border-bottom: none; }
}

// 客户名称单元格
.customer-name-cell {
  display: flex;
  align-items: center;
  gap: 10px;
}

.cell-avatar {
  width: 32px;
  height: 32px;
  flex-shrink: 0;
  background: linear-gradient(135deg, rgba(0,102,255,0.2), rgba(0,212,255,0.15));
  border: 1px solid rgba(0,212,255,0.2);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  font-weight: 700;
  color: $cyan-primary;
}

.cell-name-group {
  min-width: 0;
}

.cell-name-line {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
}

.cell-name-line .cell-name {
  flex: 1;
  min-width: 0;
}

.party-entity-name--muted {
  color: rgba(150, 170, 195, 0.82) !important;
}

.cell-name {
  font-size: 13px;
  font-weight: 500;
  color: $text-primary;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.cell-short {
  font-size: 11px;
  color: $text-muted;
  margin-top: 1px;
}

// 紧密：客户「主名 + 状态图标」与「简称」同一行并排（仅本列；依赖 CrmDataTable 根类 crm-items-table--density-compact）
.customer-list-page .table-wrapper :deep(.crm-items-table--density-compact .customer-name-cell .cell-name-group) {
  display: flex;
  flex-direction: row;
  align-items: center;
  flex-wrap: nowrap;
  gap: 6px;
  min-width: 0;
  overflow: hidden;
}

.customer-list-page .table-wrapper :deep(.crm-items-table--density-compact .customer-name-cell .cell-name-line) {
  flex: 1 1 0;
  min-width: 0;
}

.customer-list-page .table-wrapper :deep(.crm-items-table--density-compact .customer-name-cell .cell-short) {
  margin-top: 0 !important;
  flex: 0 1 auto;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.td-code {
  font-family: 'Space Mono', monospace;
  font-size: 12px;
  color: $text-muted;
}

.td-muted {
  color: $text-secondary;
  font-size: 12.5px;
}

.td-contact {
  color: $text-secondary;
  font-size: 12.5px;
}

.td-phone {
  color: $text-secondary;
  font-size: 12px;
  font-family: 'Space Mono', monospace;
}

.td-empty {
  color: rgba(255,255,255,0.2);
  font-size: 12px;
}

.td-index {
  color: $text-muted;
  font-size: 12px;
  text-align: center;
}

// ---- 徽章 ----
.badge {
  display: inline-block;
  font-size: 11px;
  padding: 2px 8px;
  border-radius: 4px;
  font-weight: 500;
  white-space: nowrap;
}

.badge-type {
  background: rgba(50,149,201,0.12);
  color: $color-steel-cyan;
  border: 1px solid rgba(50,149,201,0.25);
}

.badge-level-vip  { background: rgba(201,87,69,0.15);   color: #C95745; border: 1px solid rgba(201,87,69,0.3); }
.badge-level-vpo  { background: rgba(0,212,255,0.12);   color: $cyan-primary; border: 1px solid rgba(0,212,255,0.3); }
.badge-level-bpo  { background: rgba(0,212,255,0.12);   color: $cyan-primary; border: 1px solid rgba(0,212,255,0.3); }
.badge-level-b    { background: rgba(70,191,145,0.12);  color: #46BF91; border: 1px solid rgba(70,191,145,0.3); }
.badge-level-c    { background: rgba(201,154,69,0.12);  color: $color-amber; border: 1px solid rgba(201,154,69,0.3); }
.badge-level-d    { background: rgba(107,122,141,0.12); color: #8A9BB0; border: 1px solid rgba(107,122,141,0.3); }
.badge-level-important { background: rgba(201,154,69,0.15); color: $color-amber; border: 1px solid rgba(201,154,69,0.3); }
.badge-level-normal    { background: rgba(107,122,141,0.12); color: #8A9BB0; border: 1px solid rgba(107,122,141,0.3); }
.badge-level-lead      { background: rgba(70,191,145,0.12);  color: #46BF91; border: 1px solid rgba(70,191,145,0.3); }

// ---- 状态 ----
.status-dot {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  padding: 2px 8px;
  border-radius: 10px;

  &::before {
    content: '';
    width: 6px;
    height: 6px;
    border-radius: 50%;
    flex-shrink: 0;
  }

  &.status-active {
    color: #46BF91;
    background: rgba(70,191,145,0.1);
    border: 1px solid rgba(70,191,145,0.25);
    &::before { background: #46BF91; box-shadow: 0 0 4px #46BF91; }
  }

  &.status-warning {
    color: $color-amber;
    background: rgba(201,154,69,0.10);
    border: 1px solid rgba(201,154,69,0.25);
    &::before { background: $color-amber; box-shadow: 0 0 4px rgba(201,154,69,0.8); }
  }

  &.status-danger {
    color: #C95745;
    background: rgba(201, 87, 69, 0.08);
    border: 1px solid rgba(201, 87, 69, 0.20);
    &::before { background: #C95745; box-shadow: 0 0 4px rgba(201, 87, 69, 0.8); }
  }

  &.status-inactive {
    color: $text-muted;
    background: rgba(107,122,141,0.1);
    border: 1px solid rgba(107,122,141,0.2);
    &::before { background: #8A9BB0; }
  }
}

// ---- 操作按钮 ----
.action-btns {
  display: flex;
  align-items: center;
  gap: 6px;
  opacity: 0;
  transition: opacity 0.15s;
  white-space: nowrap;
  flex-wrap: nowrap;
}
.action-btns.always-visible {
  opacity: 1;
}

.op-col-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
}

.op-col-header-text {
  font-size: 12px;
  white-space: nowrap;
}

.op-col-toggle-btn {
  padding: 0;
  border: none;
  background: transparent;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 16px;
  line-height: 1;
}

.op-more-trigger {
  padding: 0;
  border: none;
  background: transparent;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 16px;
  line-height: 1;
}

.action-btn {
  padding: 4px 10px;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  border-radius: 5px;
  cursor: pointer;
  transition: all 0.15s;
  white-space: nowrap;
  flex-shrink: 0;

  // 查看/详情/编辑：primary 蓝（UI 规范）
  &--primary {
    background: rgba(0, 102, 255, 0.12);
    border: 1px solid rgba(0, 212, 255, 0.35);
    color: $cyan-primary;

    &:hover {
      background: rgba(0, 102, 255, 0.2);
      border-color: rgba(0, 212, 255, 0.5);
    }
  }

  &--danger {
    background: rgba(201, 87, 69, 0.08);
    border: 1px solid rgba(201, 87, 69, 0.2);
    color: #C95745;

    &:hover {
      background: rgba(201, 87, 69, 0.15);
      border-color: rgba(201, 87, 69, 0.4);
    }
  }

  // 业务流转：warning 黄（禁止用蓝/红/绿）
  &--warning {
    background: rgba(201, 154, 69, 0.12);
    border: 1px solid rgba(201, 154, 69, 0.35);
    color: $color-amber;

    &:hover {
      background: rgba(201, 154, 69, 0.2);
      border-color: rgba(201, 154, 69, 0.5);
    }
  }
}

// ---- 空状态 ----
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: $text-muted;

  svg { margin-bottom: 16px; opacity: 0.4; }
  p { margin: 0 0 16px; font-size: 14px; }
}

// ---- 分页 ----
.pagination-wrapper {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-top: 16px;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
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

.quantum-pagination {
  :deep(.el-pagination__total) { color: $text-muted; }
  :deep(.el-pagination__sizes .el-select__wrapper) { background: $layer-2 !important; border: 1px solid $border-panel !important; box-shadow: none !important; }
  :deep(.el-pager li) { background: $layer-2; border: 1px solid $border-panel; color: $text-secondary; border-radius: 6px; margin: 0 2px; }
  :deep(.el-pager li.is-active) { background: rgba(0,212,255,0.15); border-color: rgba(0,212,255,0.4); color: $cyan-primary; }
  :deep(.btn-prev), :deep(.btn-next) { background: $layer-2 !important; border: 1px solid $border-panel !important; color: $text-secondary !important; border-radius: 6px !important; }
}
</style>
