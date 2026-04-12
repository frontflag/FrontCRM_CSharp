<template>
  <div class="vendor-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
              <polyline points="9 22 9 12 15 12 15 22"/>
            </svg>
          </div>
          <h1 class="page-title">{{ t('vendorList.title') }}</h1>
        </div>
        <div class="vendor-count-badge">{{ t('vendorList.count', { count: totalCount }) }}</div>
      </div>
      <div class="header-right">
        <template v-if="canSubmitAudit">
          <div class="btn-split-group">
            <button type="button" class="btn-success" @click="handleCreate">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="5" x2="12" y2="19"/>
                <line x1="5" y1="12" x2="19" y2="12"/>
              </svg>
              {{ t('vendorList.create') }}
            </button>
            <el-dropdown trigger="click" @command="onCreateDropdownCommand">
              <button type="button" class="btn-success btn-success--caret" :aria-label="t('vendorList.expandMenu')">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="6 9 12 15 18 9" />
                </svg>
              </button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="import">{{ t('vendorList.importExcel') }}</el-dropdown-item>
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
          {{ t('vendorList.create') }}
        </button>
      </div>
    </div>

    <!-- 搜索栏：关键词 → 状态 → 等级 → 身份 → 类型 → 行业 → 采购员 → 创建日期区间 -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">{{ t('vendorList.filters.keyword') }}</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="searchForm.searchTerm"
            class="search-input"
            :placeholder="canViewVendorInfo ? t('vendorList.filters.keywordPlaceholderFull') : t('vendorList.filters.keywordPlaceholderCode')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-select
          v-model="searchForm.status"
          :placeholder="t('vendorList.filters.allStatus')"
          clearable
          class="status-select"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option :label="t('vendorList.status.draft')" :value="0" />
          <el-option :label="t('vendorList.status.new')" :value="1" />
          <el-option :label="t('vendorList.status.pending')" :value="2" />
          <el-option :label="t('vendorList.status.approved')" :value="10" />
          <el-option :label="t('vendorList.status.pendingFinance')" :value="12" />
          <el-option :label="t('vendorList.status.financeFiled')" :value="20" />
          <el-option :label="t('vendorList.status.failed')" :value="-1" />
        </el-select>
        <el-select
          v-model="searchForm.level"
          :placeholder="t('vendorList.filters.allLevel')"
          clearable
          class="status-select"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option v-for="opt in vendorDict.levelSelectOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-select
          v-model="searchForm.credit"
          :placeholder="t('vendorList.filters.allIdentity')"
          clearable
          class="status-select status-select--identity"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option v-for="opt in vendorDict.identitySelectOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-select
          v-model="searchForm.ascriptionType"
          :placeholder="t('vendorList.filters.allType')"
          clearable
          class="status-select"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option :label="t('vendorList.type.private')" :value="1" />
          <el-option :label="t('vendorList.type.pool')" :value="2" />
        </el-select>
        <el-select
          v-model="searchForm.industry"
          :placeholder="t('vendorList.filters.allIndustry')"
          clearable
          class="status-select status-select--industry"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option
            v-for="opt in vendorDict.industryOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-select
          v-model="searchForm.purchaseUserId"
          :placeholder="t('vendorList.filters.allPurchasers')"
          clearable
          filterable
          class="status-select status-select--purchaser"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option v-for="u in purchaseUsers" :key="u.id" :label="purchaserUserLabel(u)" :value="u.id" />
        </el-select>
        <el-date-picker
          v-model="createdDateRange"
          type="daterange"
          :range-separator="t('vendorList.filters.to')"
          :start-placeholder="t('vendorList.filters.startDate')"
          :end-placeholder="t('vendorList.filters.endDate')"
          value-format="YYYY-MM-DD"
          clearable
          class="filter-date-range"
          :teleported="false"
          @change="onCreatedRangeChange"
        />
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">{{ t('vendorList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="handleReset">{{ t('vendorList.filters.reset') }}</button>
      </div>
    </div>

    <!-- 表格列表 -->
    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        ref="dataTableRef"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        column-layout-key="vendor-list-main-v2"
        :columns="vendorTableColumns"
        :show-column-settings="false"
        :data="vendorList"
        row-key="id"
        @row-dblclick="handleView"
      >
        <template #col-status="{ row }">
          <span class="status-badge" :class="getStatusClass(row.status)">
            {{ getStatusLabel(row.status) }}
          </span>
        </template>
        <template #col-officialName="{ row }">
          <div class="vendor-name-cell">
            <div class="cell-avatar">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
                <polyline points="9 22 9 12 15 12 15 22"/>
              </svg>
            </div>
            <div class="cell-name-group">
              <div class="cell-name-line">
                <span
                  class="cell-name"
                  :class="{ 'party-entity-name--muted': isPartyStatusMuted(row) }"
                >{{ row.officialName || row.code }}</span>
                <PartyStatusIcons
                  :entity-id="row.id"
                  :frozen="!!row.isDisenable"
                  :blacklist="!!row.blackList"
                  size="sm"
                />
              </div>
              <div class="cell-short" v-if="row.code">{{ row.code }}</div>
            </div>
          </div>
        </template>
        <template #col-chineseOfficialName="{ row }">
          <span class="td-muted">{{ row.officialName || row.name || '--' }}</span>
        </template>
        <template #col-englishOfficialName="{ row }">
          <span class="td-muted">{{ row.englishOfficialName || '--' }}</span>
        </template>
        <template #col-level="{ row }"><span class="td-muted">{{ vendorDict.levelLabel(row.level) }}</span></template>
        <template #col-credit="{ row }"><span class="td-muted td-identity">{{ vendorDict.identityLabel(row.credit) }}</span></template>
        <template #col-industry="{ row }"><span class="td-muted">{{ vendorDict.industryLabel(row.industry) }}</span></template>
        <template #col-contactName="{ row }"><span class="td-contact">{{ row.contacts && row.contacts.length > 0 ? (row.contacts[0].cName || row.contacts[0].eName || '--') : '--' }}</span></template>
        <template #col-phone="{ row }"><span class="td-phone">{{ row.contacts && row.contacts.length > 0 ? (row.contacts[0].mobile || row.contacts[0].tel || '--') : '--' }}</span></template>
        <template #col-officeAddress="{ row }"><span class="td-address" :title="row.officeAddress">{{ row.officeAddress || '--' }}</span></template>
        <template #col-createTime="{ row }"><span class="td-muted">{{ formatDate(row.createTime) }}</span></template>
        <template #col-createUser="{ row }"><span class="td-muted">{{ (row as any).createUserName || (row as any).createdBy || (row as any).purchaseUserName || '--' }}</span></template>
        <template #col-actions-header>
          <div class="op-col-header">
            <span class="op-col-header-text">{{ t('vendorList.actions.column') }}</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns always-visible">
              <button class="action-btn action-btn--primary" @click.stop="handleView(row)">{{ t('vendorList.actions.detail') }}</button>
              <button class="action-btn action-btn--primary" @click.stop="handleEdit(row)">{{ t('vendorList.actions.edit') }}</button>
              <button
                v-if="row.status === 1 || row.status === -1"
                class="action-btn action-btn--warning"
                @click.stop="handleSubmitAudit(row)"
              >
                {{ t('vendorList.actions.submitAudit') }}
              </button>
            </div>
            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="handleView(row)">{{ t('vendorList.actions.detail') }}</el-dropdown-item>
                  <el-dropdown-item @click.stop="handleEdit(row)">{{ t('vendorList.actions.edit') }}</el-dropdown-item>
                  <el-dropdown-item v-if="row.status === 1 || row.status === -1" @click.stop="handleSubmitAudit(row)">
                    {{ t('vendorList.actions.submitAudit') }}
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>

      <!-- 空状态 -->
      <div v-if="!loading && vendorList.length === 0" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
          <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
          <polyline points="9 22 9 12 15 12 15 22"/>
        </svg>
        <p>{{ t('vendorList.empty') }}</p>
        <template v-if="canSubmitAudit">
          <div class="btn-split-group empty-split">
            <button type="button" class="btn-success" @click="handleCreate">{{ t('vendorList.create') }}</button>
            <el-dropdown trigger="click" @command="onCreateDropdownCommand">
              <button type="button" class="btn-success btn-success--caret" :aria-label="t('vendorList.expandMenu')">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="6 9 12 15 18 9" />
                </svg>
              </button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="import">{{ t('vendorList.importExcel') }}</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
        <button v-else type="button" class="btn-success" @click="handleCreate">{{ t('vendorList.create') }}</button>
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

    <VendorImportDialog v-model="importDialogVisible" @success="fetchVendorList" />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, watch, nextTick, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus';
import { Setting } from '@element-plus/icons-vue'
import { vendorApi } from '@/api/vendor';
import { favoriteApi } from '@/api/favorite';
import { authApi, type PurchaseUserSelectOption } from '@/api/auth';
import { formatDisplayDateTime } from '@/utils/displayDateTime';
import type { Vendor, VendorSearchRequest } from '@/types/vendor';
import { useAuthStore } from '@/stores/auth';
import { useVendorDictStore } from '@/stores/vendorDict';
import PartyStatusIcons from '@/components/party/PartyStatusIcons.vue';
import VendorImportDialog from '@/components/Vendor/VendorImportDialog.vue';
import CrmDataTable from '@/components/CrmDataTable.vue'
import { parseVendorListQuery, buildVendorListQuery } from '@/utils/vendorListQuery';
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const route = useRoute();
const router = useRouter();
const { t } = useI18n()

function isPartyStatusMuted(v: Vendor) {
  return !!(v.isDisenable || v.blackList);
}

const authStore = useAuthStore();
const vendorDict = useVendorDictStore();
const canViewVendorInfo = authStore.hasPermission('vendor.info.read');
const canSubmitAudit = authStore.hasPermission('vendor.write');
const importDialogVisible = ref(false);
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 120
const OP_COL_EXPANDED_WIDTH = 220
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? 220 : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}
const loading = ref(false);
const vendorList = ref<Vendor[]>([]);
const totalCount = ref(0);
const favoriteOnly = ref(false);
const skipQueryRouteFetch = ref(false);

const purchaseUsers = ref<PurchaseUserSelectOption[]>([]);
const createdDateRange = ref<[string, string] | null>(null);

/** 与 URL query / 左侧检索面板同步（不含分页） */
const searchForm = reactive({
  searchTerm: '',
  status: undefined as number | undefined,
  level: undefined as number | undefined,
  credit: undefined as number | undefined,
  ascriptionType: undefined as number | undefined,
  industry: undefined as string | undefined,
  purchaseUserId: undefined as string | undefined,
  createdFrom: undefined as string | undefined,
  createdTo: undefined as string | undefined
});

function purchaserUserLabel(u: PurchaseUserSelectOption) {
  const name = u.realName || u.label || u.userName;
  return u.userName && name !== u.userName ? `${name}（${u.userName}）` : name;
}

function applyVendorListQueryFromRoute() {
  const p = parseVendorListQuery(route.query);
  searchForm.searchTerm = p.searchTerm;
  searchForm.status = p.status;
  searchForm.level = p.level;
  searchForm.credit = p.credit;
  searchForm.ascriptionType = p.ascriptionType;
  searchForm.industry = p.industry;
  searchForm.purchaseUserId = p.purchaseUserId;
  searchForm.createdFrom = p.createdFrom;
  searchForm.createdTo = p.createdTo;
  createdDateRange.value = p.createdFrom && p.createdTo ? [p.createdFrom, p.createdTo] : null;
  favoriteOnly.value = p.favoriteOnly;
}

function replaceRouteQueryAndFetch() {
  skipQueryRouteFetch.value = true;
  router
    .replace({
      name: 'VendorList',
      query: buildVendorListQuery({
        searchTerm: searchForm.searchTerm || '',
        status: searchForm.status,
        level: searchForm.level,
        credit: searchForm.credit,
        ascriptionType: searchForm.ascriptionType,
        industry: searchForm.industry,
        purchaseUserId: searchForm.purchaseUserId,
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
  void fetchVendorList();
}

const handleSearch = () => {
  pagination.pageNumber = 1;
  replaceRouteQueryAndFetch();
};

const handleReset = () => {
  searchForm.searchTerm = '';
  searchForm.status = undefined;
  searchForm.level = undefined;
  searchForm.credit = undefined;
  searchForm.ascriptionType = undefined;
  searchForm.industry = undefined;
  searchForm.purchaseUserId = undefined;
  searchForm.createdFrom = undefined;
  searchForm.createdTo = undefined;
  createdDateRange.value = null;
  favoriteOnly.value = false;
  pagination.pageNumber = 1;
  skipQueryRouteFetch.value = true;
  router.replace({ name: 'VendorList', query: {} }).finally(() => {
    nextTick(() => {
      skipQueryRouteFetch.value = false;
    });
  });
  void fetchVendorList();
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

const pagination = reactive({ pageNumber: 1, pageSize: 20 });
const vendorTableColumns = computed<CrmTableColumnDef[]>(() => {
  const cols: CrmTableColumnDef[] = [
    { key: 'status', label: t('vendorList.columns.status'), prop: 'status', width: 160, align: 'center' },
    { key: 'chineseOfficialName', label: t('vendorList.columns.chineseOfficialName'), minWidth: 160, showOverflowTooltip: true },
    { key: 'englishOfficialName', label: t('vendorList.columns.englishOfficialName'), minWidth: 160, showOverflowTooltip: true },
    { key: 'level', label: t('vendorList.columns.level'), prop: 'level', width: 80, align: 'center' },
    { key: 'credit', label: t('vendorList.columns.identity'), prop: 'credit', width: 100, align: 'center' },
    { key: 'industry', label: t('vendorList.columns.industry'), prop: 'industry', width: 100, showOverflowTooltip: true },
    { key: 'officeAddress', label: t('vendorList.columns.address'), prop: 'officeAddress', width: 160, showOverflowTooltip: true }
  ]
  if (canViewVendorInfo) {
    cols.splice(3, 0, {
      key: 'officialName',
      label: t('vendorList.columns.name'),
      minWidth: 200,
      showOverflowTooltip: true
    })
    cols.push(
      { key: 'contactName', label: t('vendorList.columns.contactName'), width: 130, showOverflowTooltip: true },
      { key: 'phone', label: t('vendorList.columns.phone'), width: 130, showOverflowTooltip: true }
    )
  }
  cols.push(
    { key: 'code', label: t('vendorList.columns.code'), prop: 'code', width: 160, minWidth: 160, showOverflowTooltip: true },
    { key: 'createTime', label: t('vendorList.columns.createdAt'), prop: 'createTime', width: 160 },
    { key: 'createUser', label: t('vendorList.columns.createUser'), width: 120, showOverflowTooltip: true }
  )
  cols.push({
    key: 'actions',
    label: t('vendorList.actions.column'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    hideable: false,
    pinned: 'end',
    reorderable: false
  })
  return cols
})

const getStatusLabel = (status?: number) => {
  if (status === 0) return t('vendorList.status.draft');
  if (status === 1) return t('vendorList.status.new');
  if (status === 2) return t('vendorList.status.pending');
  if (status === 10) return t('vendorList.status.approved');
  if (status === 12) return t('vendorList.status.pendingFinance');
  if (status === 20) return t('vendorList.status.financeFiled');
  if (status === -1) return t('vendorList.status.failed');
  return t('vendorList.status.unknown');
};
const getStatusClass = (status?: number) => {
  if (status === 2 || status === 12) return 'status-pending';
  if (status === 10 || status === 20) return 'status-active';
  if (status === -1) return 'status-danger';
  if (status === 1) return 'status-draft';
  return 'status-draft';
};

const parseDateMs = (v?: string) => {
  if (!v) return 0;
  const t = new Date(v).getTime();
  return Number.isFinite(t) ? t : 0;
};

const formatDate = (v?: string) => {
  return formatDisplayDateTime(v);
};

const fetchVendorList = async () => {
  loading.value = true;
  try {
    const params: VendorSearchRequest = {
      searchTerm: searchForm.searchTerm,
      status: searchForm.status,
      level: searchForm.level,
      credit: searchForm.credit,
      ascriptionType: searchForm.ascriptionType,
      industry: searchForm.industry,
      purchaseUserId: searchForm.purchaseUserId,
      createdFrom: searchForm.createdFrom,
      createdTo: searchForm.createdTo,
      pageNumber: pagination.pageNumber,
      pageSize: pagination.pageSize
    };
    const [response, favoriteIds] = await Promise.all([
      vendorApi.searchVendors(params),
      favoriteApi.getFavoriteEntityIds('VENDOR')
    ]);
    const favoriteSet = new Set(favoriteIds);
    let mapped = (response.items || []).map((item: any) => ({
      ...item,
      isFavorite: favoriteSet.has(item.id)
    }));
    if (favoriteOnly.value) {
      mapped = mapped.filter((item: any) => item.isFavorite);
    }
    // 兜底：确保按创建日期（createTime）降序展示
    mapped.sort((a: any, b: any) => (parseDateMs(b?.createTime) - parseDateMs(a?.createTime)));
    vendorList.value = mapped;
    totalCount.value = favoriteOnly.value ? mapped.length : (response.totalCount ?? 0);
  } catch (error: any) {
    // 仅在非空结果（真实网络/服务器错误）时提示，空数据不报错
    const isEmptyResult = !error?.response || error?.response?.status === 404;
    if (!isEmptyResult) {
      console.error(t('vendorList.errors.fetchFailed'), error);
      ElMessage.error(t('vendorList.errors.fetchFailed'));
    } else {
      vendorList.value = [];
      totalCount.value = 0;
    }
  } finally {
    loading.value = false;
  }
};

const handleCreate = () => router.push('/vendors/create');

function onCreateDropdownCommand(cmd: string) {
  if (cmd === 'import') importDialogVisible.value = true;
}
const handleView = (row: Vendor) => router.push(`/vendors/${row.id}`);
const handleEdit = (row: Vendor) => router.push(`/vendors/${row.id}/edit`);

const handleSubmitAudit = async (row: Vendor) => {
  if (!canSubmitAudit) {
    ElMessage.warning(t('vendorList.audit.noPermission'));
    return;
  }
  try {
    await ElMessageBox.confirm(t('vendorList.audit.confirmMessage'), t('vendorList.audit.confirmTitle'), {
      confirmButtonText: t('vendorList.audit.confirmButton'),
      cancelButtonText: t('common.cancel'),
      type: 'warning'
    });
    await vendorApi.submitAudit(row.id);
    ElMessage.success(t('vendorList.audit.success'));
    fetchVendorList();
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e?.message || t('vendorList.audit.failed'));
  }
};

const handleSizeChange = (size: number) => { pagination.pageSize = size; fetchVendorList(); };
const handlePageChange = (page: number) => { pagination.pageNumber = page; fetchVendorList(); };

watch(
  () => [route.name, route.query] as const,
  () => {
    if (route.name !== 'VendorList') return;
    if (skipQueryRouteFetch.value) return;
    applyVendorListQueryFromRoute();
    pagination.pageNumber = 1;
    void fetchVendorList();
  },
  { deep: true }
);

onMounted(async () => {
  void vendorDict.ensureLoaded();
  applyVendorListQueryFromRoute();
  try {
    purchaseUsers.value = await authApi.getPurchaseUsersForSelect();
  } catch {
    purchaseUsers.value = [];
  }
  void fetchVendorList();
});
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.vendor-list-page {
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

.vendor-count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

// ---- 按钮 ----
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

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
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

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.75), rgba(0, 212, 255, 0.65));
  border: 1px solid rgba(0, 212, 255, 0.35);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 2px 12px rgba(0, 212, 255, 0.2);
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

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}

// ---- 搜索栏（与客户列表对齐） ----
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
  width: 200px;
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

.status-select--identity {
  width: 130px;
}

.status-select--industry {
  width: 128px;
}

.status-select--purchaser {
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

.table-row {
  cursor: pointer;
  transition: background 0.15s;

  &:hover {
    background: rgba(0, 212, 255, 0.04);

    .action-btns { opacity: 1; }
  }

  &:last-child td { border-bottom: none; }
}

// 供应商名称单元格
.vendor-name-cell {
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
  font-family: 'Space Mono', monospace;
}

// 紧密：供应商「主名 + 状态图标」与「编号」同一行并排（仅本列；依赖 CrmDataTable 根类 crm-items-table--density-compact）
.vendor-list-page .table-wrapper :deep(.crm-items-table--density-compact .vendor-name-cell .cell-name-group) {
  display: flex;
  flex-direction: row;
  align-items: center;
  flex-wrap: nowrap;
  gap: 6px;
  min-width: 0;
  overflow: hidden;
}

.vendor-list-page .table-wrapper :deep(.crm-items-table--density-compact .vendor-name-cell .cell-name-line) {
  flex: 1 1 0;
  min-width: 0;
}

.vendor-list-page .table-wrapper :deep(.crm-items-table--density-compact .vendor-name-cell .cell-short) {
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

.td-address {
  color: $text-secondary;
  font-size: 12px;
  display: -webkit-box;
  -webkit-line-clamp: 1;
  -webkit-box-orient: vertical;
  overflow: hidden;
  max-width: 160px;
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

// ---- 状态徽章 ----
.status-badge {
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

  &.status-pending {
    color: $color-amber;
    background: rgba(201,154,69,0.1);
    border: 1px solid rgba(201,154,69,0.25);
    &::before { background: $color-amber; }
  }

  &.status-draft {
    color: $text-muted;
    background: rgba(107,122,141,0.1);
    border: 1px solid rgba(107,122,141,0.2);
    &::before { background: #8A9BB0; }
  }

  &.status-danger {
    color: #C95745;
    background: rgba(201, 87, 69, 0.08);
    border: 1px solid rgba(201, 87, 69, 0.2);
    &::before { background: #C95745; box-shadow: 0 0 4px rgba(201, 87, 69, 0.8); }
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

.list-footer-density-anchor {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  min-height: 0;
}

.list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
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
