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
          <h1 class="page-title">客户</h1>
        </div>
        <div class="customer-count-badge">共 {{ totalCount }} 个客户</div>
      </div>
      <div class="header-right">
        <button class="btn-success" @click="handleCreate">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"/>
            <line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          新增客户
        </button>
      </div>
    </div>

    <!-- 搜索栏：关键词 → 状态 → 级别 → 类型 → 行业 → 业务员 → 创建日期区间 -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">关键词</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
          </svg>
          <input
            v-model="searchForm.searchTerm"
            class="search-input"
            :placeholder="canViewCustomerInfo ? '客户名称 / 联系人…' : '客户编号…'"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-select v-model="searchForm.status" placeholder="全部状态" clearable class="status-select" :teleported="false" @change="handleSearch">
          <el-option
            v-for="opt in workflowStatusOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-select v-model="searchForm.customerLevel" placeholder="全部级别" clearable class="status-select" :teleported="false" @change="handleSearch">
          <el-option label="D级" value="D" />
          <el-option label="C级" value="C" />
          <el-option label="B级" value="B" />
          <el-option label="BPO" value="BPO" />
          <el-option label="VIP" value="VIP" />
          <el-option label="VPO" value="VPO" />
        </el-select>
        <el-select v-model="searchForm.customerType" placeholder="全部类型" clearable class="status-select" :teleported="false" @change="handleSearch">
          <el-option label="OEM" :value="1" />
          <el-option label="ODM" :value="2" />
          <el-option label="终端用户" :value="3" />
          <el-option label="贸易商" :value="5" />
          <el-option label="代理商" :value="6" />
        </el-select>
        <el-select v-model="searchForm.industry" placeholder="全部行业" clearable class="status-select" :teleported="false" @change="handleSearch">
          <el-option label="制造业" value="Manufacturing" />
          <el-option label="科技/IT" value="Technology" />
          <el-option label="贸易/零售" value="Trading" />
          <el-option label="建筑/工程" value="Construction" />
          <el-option label="其他" value="Other" />
        </el-select>
        <el-select
          v-model="searchForm.salesPersonId"
          placeholder="全部业务员"
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
          range-separator="至"
          start-placeholder="创建起"
          end-placeholder="创建止"
          value-format="YYYY-MM-DD"
          clearable
          class="filter-date-range"
          :teleported="false"
          @change="onCreatedRangeChange"
        />
        <button class="btn-primary btn-sm" @click="handleSearch">搜索</button>
        <button class="btn-ghost btn-sm" @click="handleReset">重置</button>
      </div>
    </div>

    <!-- 表格列表 -->
    <div class="table-wrapper" v-loading="loading">
      <table class="data-table">
        <thead>
          <tr>
            <th style="width:160px;min-width:160px">客户编号</th>
            <th style="width:160px">状态</th>
            <th v-if="canViewCustomerInfo" style="min-width:200px">客户名称</th>
            <th style="width:80px">类型</th>
            <th style="width:80px">级别</th>
            <th style="width:110px">行业</th>
            <th v-if="canViewCustomerInfo" style="width:130px">联系人</th>
            <th v-if="canViewCustomerInfo" style="width:130px">联系电话</th>
            <th style="min-width:100px">地区</th>
            <th style="width:160px">创建日期</th>
            <th style="width:120px">创建人</th>
            <th style="width:200px">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="customer in customerList"
            :key="customer.id"
            class="table-row"
            @dblclick="handleView(customer)"
          >
            <td class="td-code">{{ customer.customerCode }}</td>
            <td>
              <span class="status-dot" :class="getStatusDotClass(customer.status)">
                {{ getStatusText(customer.status) }}
              </span>
            </td>
            <td v-if="canViewCustomerInfo">
              <div class="customer-name-cell">
                <div class="cell-avatar">
                  <span>{{ (customer.customerName || customer.customerShortName || '?')[0] }}</span>
                </div>
                <div class="cell-name-group">
                  <div class="cell-name-line">
                    <span
                      class="cell-name"
                      :class="{ 'party-entity-name--muted': isPartyStatusMuted(customer) }"
                    >{{ customer.customerName || customer.customerShortName }}</span>
                    <PartyStatusIcons
                      :entity-id="customer.id"
                      :frozen="!!customer.disenableStatus"
                      :blacklist="!!customer.blackList"
                      size="sm"
                    />
                  </div>
                  <div class="cell-short" v-if="customer.customerShortName && customer.customerShortName !== customer.customerName">
                    {{ customer.customerShortName }}
                  </div>
                </div>
              </div>
            </td>
            <td>
              <span class="badge badge-type" v-if="customer.customerType">{{ getTypeLabel(customer.customerType) }}</span>
            </td>
            <td>
              <span class="badge" :class="`badge-level-${(customer.customerLevel || '').toLowerCase()}`" v-if="customer.customerLevel">
                {{ getLevelLabel(customer.customerLevel) }}
              </span>
            </td>
            <td class="td-muted">{{ getIndustryLabel(customer.industry || '') }}</td>
            <td v-if="canViewCustomerInfo">
              <template v-if="customer.contacts && customer.contacts.length > 0">
                <span class="td-contact">{{ customer.contacts[0].contactName }}</span>
              </template>
              <span v-else class="td-empty">--</span>
            </td>
            <td v-if="canViewCustomerInfo">
              <template v-if="customer.contacts && customer.contacts.length > 0">
                <span class="td-phone">{{ customer.contacts[0].mobilePhone }}</span>
              </template>
              <span v-else class="td-empty">--</span>
            </td>
            <td class="td-muted">{{ customer.city || customer.region || '--' }}</td>
            <td class="td-muted">{{ formatDate(customer.createdAt ?? (customer as any).createTime) }}</td>
            <td class="td-muted">{{ (customer as any).createUserName || (customer as any).createdBy || (customer as any).salesPersonName || '--' }}</td>
            <td @click.stop @dblclick.stop>
              <div class="action-btns">
                <button class="action-btn action-btn--primary" @click.stop="handleView(customer)">详情</button>
                <button class="action-btn action-btn--primary" @click.stop="handleEdit(customer)">编辑</button>
                <button
                  v-if="customer.status === 1"
                  class="action-btn action-btn--warning"
                  @click.stop="handleSubmitAudit(customer)"
                >
                  提交审核
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- 空状态 -->
      <div v-if="!loading && customerList.length === 0" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
          <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/>
        </svg>
        <p>暂无客户数据</p>
        <button class="btn-success" @click="handleCreate">新增客户</button>
      </div>
    </div>

    <!-- 分页 -->
    <div class="pagination-wrapper" v-if="totalCount > 0">
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
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, watch, nextTick } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ElNotification, ElMessageBox } from 'element-plus';
import { customerApi } from '@/api/customer';
import { authApi } from '@/api/auth';
import { favoriteApi } from '@/api/favorite';
import { formatDisplayDateTime } from '@/utils/displayDateTime';
import type { Customer, CustomerSearchRequest } from '@/types/customer';
import { useAuthStore } from '@/stores/auth';
import PartyStatusIcons from '@/components/party/PartyStatusIcons.vue';
import { buildCustomerListQuery, parseCustomerListQuery } from '@/utils/customerListQuery';
import { CUSTOMER_WORKFLOW_STATUS_OPTIONS } from '@/constants/customerWorkflowStatus';

const router = useRouter();
const route = useRoute();

function isPartyStatusMuted(c: Customer) {
  return !!(c.disenableStatus || c.blackList);
}
const authStore = useAuthStore();
const canViewCustomerInfo = authStore.hasPermission('customer.info.read');
const canSubmitAudit = authStore.hasPermission('customer.write');

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
      customerLevel: item.customerLevel || (item.level ? ['', 'D', 'C', 'B', 'BPO', 'VIP', 'VPO'][item.level] : 'Normal'),
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
      console.error('获取客户列表失败', error);
      ElNotification.error({ title: '加载失败', message: '获取客户列表失败，请检查网络或后端服务' });
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
const handleView = (row: Customer) => router.push(`/customers/${row.id}`);
const handleEdit = (row: Customer) => router.push(`/customers/${row.id}/edit`);

const getStatusText = (status: number | undefined) => {
  const s = Number(status ?? 0);
  const map: Record<number, string> = {
    1: '新建',
    2: '待审核',
    10: '已审核',
    12: '待财务审核',
    20: '财务建档',
    [-1]: '审核失败'
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
    ElNotification.warning({ title: '无权限', message: '没有权限提交审核' });
    return;
  }
  await ElMessageBox.confirm(
    '确定提交审核？提交后将进入“待审批”列表，由上级角色审批。',
    '提交审核',
    { type: 'warning', confirmButtonText: '提交', cancelButtonText: '取消' }
  );
  try {
    loading.value = true;
    await customerApi.submitAudit(row.id);
    ElNotification.success({ title: '成功', message: '已提交审核' });
    await fetchCustomerList();
  } catch (error: any) {
    ElNotification.error({ title: '提交失败', message: error?.message || '提交审核失败，请稍后重试' });
  } finally {
    loading.value = false;
  }
};

const handleSizeChange = (size: number) => { pagination.pageSize = size; fetchCustomerList(); };
const handlePageChange = (page: number) => { pagination.pageNumber = page; fetchCustomerList(); };

const getLevelLabel = (level: string) => ({ VIP: 'VIP', VPO: 'VPO', BPO: 'BPO', B: 'B级', C: 'C级', D: 'D级', Important: '重要', Normal: '普通', Lead: '潜在' }[level] || level || '--');
const getTypeLabel = (type: number) => ({ 1: 'OEM', 2: 'ODM', 3: '终端用户', 4: 'IDH', 5: '贸易商', 6: '代理商' }[type] || '未知');
const getIndustryLabel = (industry: string) => ({
  Manufacturing: '制造业', Trading: '贸易/零售', Technology: '科技/IT',
  Construction: '建筑/工程', Healthcare: '医疗/健康', Education: '教育',
  Finance: '金融', Other: '其他'
}[industry] || industry || '--');

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
  justify-content: flex-end;
  margin-top: 16px;
}

.quantum-pagination {
  :deep(.el-pagination__total) { color: $text-muted; }
  :deep(.el-pagination__sizes .el-select__wrapper) { background: $layer-2 !important; border: 1px solid $border-panel !important; box-shadow: none !important; }
  :deep(.el-pager li) { background: $layer-2; border: 1px solid $border-panel; color: $text-secondary; border-radius: 6px; margin: 0 2px; }
  :deep(.el-pager li.is-active) { background: rgba(0,212,255,0.15); border-color: rgba(0,212,255,0.4); color: $cyan-primary; }
  :deep(.btn-prev), :deep(.btn-next) { background: $layer-2 !important; border: 1px solid $border-panel !important; color: $text-secondary !important; border-radius: 6px !important; }
}
</style>
