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
          <h1 class="page-title">客户管理</h1>
        </div>
        <div class="customer-count-badge">共 {{ totalCount }} 个客户</div>
      </div>
      <div class="header-right">
        <button class="btn-primary" @click="handleCreate">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"/>
            <line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          新增客户
        </button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="statistics-row" v-if="statistics">
      <div class="stat-card">
        <div class="stat-icon stat-icon--blue">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
            <circle cx="9" cy="7" r="4"/>
          </svg>
        </div>
        <div class="stat-body">
          <div class="stat-value">{{ statistics.totalCustomers }}</div>
          <div class="stat-label">客户总数</div>
        </div>
        <div class="stat-glow stat-glow--blue"></div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--green">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>
          </svg>
        </div>
        <div class="stat-body">
          <div class="stat-value">{{ statistics.activeCustomers }}</div>
          <div class="stat-label">活跃客户</div>
        </div>
        <div class="stat-glow stat-glow--green"></div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--cyan">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <line x1="12" y1="5" x2="12" y2="19"/>
            <line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
        </div>
        <div class="stat-body">
          <div class="stat-value">{{ statistics.newThisMonth }}</div>
          <div class="stat-label">本月新增</div>
        </div>
        <div class="stat-glow stat-glow--cyan"></div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--amber">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <line x1="12" y1="1" x2="12" y2="23"/>
            <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>
          </svg>
        </div>
        <div class="stat-body">
          <div class="stat-value">{{ statistics.totalBalance }}</div>
          <div class="stat-label">应收余额</div>
        </div>
        <div class="stat-glow stat-glow--amber"></div>
      </div>
    </div>

    <!-- 搜索栏 -->
    <div class="search-bar">
      <div class="search-left">
        <span class="list-title">客户列表</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
          </svg>
          <input
            v-model="searchForm.searchTerm"
            class="search-input"
            :placeholder="canViewCustomerInfo ? '搜索客户名称/联系人...' : '搜索客户编号...'"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-select v-model="searchForm.customerType" placeholder="全部类型" clearable class="status-select" @change="handleSearch">
          <el-option label="OEM" :value="1" />
          <el-option label="ODM" :value="2" />
          <el-option label="终端用户" :value="3" />
          <el-option label="贸易商" :value="5" />
          <el-option label="代理商" :value="6" />
        </el-select>
        <el-select v-model="searchForm.industry" placeholder="全部行业" clearable class="status-select" @change="handleSearch">
          <el-option label="制造业" value="Manufacturing" />
          <el-option label="科技/IT" value="Technology" />
          <el-option label="贸易/零售" value="Trading" />
          <el-option label="建筑/工程" value="Construction" />
          <el-option label="其他" value="Other" />
        </el-select>
        <button class="btn-primary btn-sm" @click="handleSearch">搜索</button>
        <button class="btn-ghost btn-sm" :class="{ 'btn-favorite-active': favoriteOnly }" @click="toggleFavoriteOnly">
          {{ favoriteOnly ? '仅收藏中' : '仅收藏' }}
        </button>
        <button class="btn-ghost btn-sm" @click="handleReset">重置</button>
      </div>
    </div>

    <!-- 表格列表 -->
    <div class="table-wrapper" v-loading="loading">
      <table class="data-table">
        <thead>
          <tr>
            <th style="width:44px">序号</th>
            <th v-if="canViewCustomerInfo" style="min-width:180px">客户名称</th>
            <th style="width:110px">客户编号</th>
            <th style="width:80px">类型</th>
            <th style="width:80px">级别</th>
            <th style="width:110px">行业</th>
            <th v-if="canViewCustomerInfo" style="width:130px">联系人</th>
            <th v-if="canViewCustomerInfo" style="width:130px">联系电话</th>
            <th style="width:90px">地区</th>
            <th style="width:80px">状态</th>
            <th style="width:150px">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="(customer, index) in customerList"
            :key="customer.id"
            class="table-row"
            @click="handleView(customer)"
          >
            <td class="td-index">{{ (pagination.pageNumber - 1) * pagination.pageSize + index + 1 }}</td>
            <td v-if="canViewCustomerInfo">
              <div class="customer-name-cell">
                <div class="cell-avatar">
                  <span>{{ (customer.customerName || customer.customerShortName || '?')[0] }}</span>
                </div>
                <div class="cell-name-group">
                  <div class="cell-name">{{ customer.customerName || customer.customerShortName }}</div>
                  <div class="cell-short" v-if="customer.customerShortName && customer.customerShortName !== customer.customerName">
                    {{ customer.customerShortName }}
                  </div>
                </div>
              </div>
            </td>
            <td class="td-code">{{ customer.customerCode }}</td>
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
            <td>
              <span class="status-dot" :class="customer.isActive ? 'status-active' : 'status-inactive'">
                {{ customer.isActive ? '正常' : '停用' }}
              </span>
            </td>
            <td @click.stop>
              <div class="action-btns">
                <button class="action-btn" @click.stop="handleView(customer)">详情</button>
                <button class="action-btn" @click.stop="handleEdit(customer)">编辑</button>
                <button
                  class="action-btn"
                  :class="{ 'action-btn--favorite': customer.isFavorite }"
                  @click.stop="toggleFavorite(customer)"
                >
                  {{ customer.isFavorite ? '取消收藏' : '收藏' }}
                </button>
                <button class="action-btn action-btn--danger" @click.stop="handleDeleteCustomer(customer)">删除</button>
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
        <button class="btn-primary" @click="handleCreate">新增客户</button>
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
import { ref, reactive, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElNotification, ElMessageBox } from 'element-plus';
import { customerApi } from '@/api/customer';
import { favoriteApi } from '@/api/favorite';
import type { Customer, CustomerStatistics, CustomerSearchRequest } from '@/types/customer';
import { useAuthStore } from '@/stores/auth';

const router = useRouter();
const authStore = useAuthStore();
const canViewCustomerInfo = authStore.hasPermission('customer.info.read');

const loading = ref(false);
const customerList = ref<Customer[]>([]);
const totalCount = ref(0);
const statistics = ref<CustomerStatistics | null>(null);
const favoriteOnly = ref(false);

const searchForm = reactive<CustomerSearchRequest>({
  pageNumber: 1,
  pageSize: 20,
  searchTerm: '',
  customerType: undefined,
  customerLevel: undefined,
  industry: undefined,
  region: undefined,
  isActive: undefined,
  sortBy: 'CreatedAt',
  sortDescending: true
});

const pagination = reactive({ pageNumber: 1, pageSize: 20 });

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
      isActive: item.isActive ?? (item.status === 1),
      isFavorite: favoriteSet.has(item.id),
      contacts: item.contacts || []
    }));
    if (favoriteOnly.value) {
      mapped = mapped.filter((item: any) => item.isFavorite);
    }
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

const fetchStatistics = async () => {
  try {
    const data = await customerApi.getCustomerStatistics();
    statistics.value = data;
  } catch {
    // 统计接口失败时静默处理，不影响列表展示
  }
};

const handleSearch = () => {
  pagination.pageNumber = 1;
  fetchCustomerList();
};

const handleReset = () => {
  Object.assign(searchForm, {
    searchTerm: '', customerType: undefined, customerLevel: undefined,
    industry: undefined, region: undefined, isActive: undefined
  });
  favoriteOnly.value = false;
  handleSearch();
};

const handleCreate = () => router.push('/customers/create');
const handleView = (row: Customer) => router.push(`/customers/${row.id}`);
const handleEdit = (row: Customer) => router.push(`/customers/${row.id}/edit`);

const handleDeleteCustomer = (row: Customer) => {
  ElMessageBox.confirm(`确定要删除客户 "${row.customerName}" 吗？`, '确认删除', {
    confirmButtonText: '删除', cancelButtonText: '取消', type: 'warning'
  }).then(async () => {
    await customerApi.deleteCustomer(row.id!);
    ElNotification.success({ title: '删除成功', message: '客户已删除' });
    fetchCustomerList();
  }).catch(() => {});
};

const toggleFavoriteOnly = () => {
  favoriteOnly.value = !favoriteOnly.value;
  handleSearch();
};

const toggleFavorite = async (row: any) => {
  try {
    if (row.isFavorite) {
      await favoriteApi.removeFavorite('CUSTOMER', row.id);
      row.isFavorite = false;
      ElNotification.success({ title: '已取消收藏', message: row.customerName || '客户记录已取消收藏' });
    } else {
      await favoriteApi.addFavorite({ entityType: 'CUSTOMER', entityId: row.id });
      row.isFavorite = true;
      ElNotification.success({ title: '收藏成功', message: row.customerName || '客户记录已收藏' });
    }
    if (favoriteOnly.value) {
      customerList.value = customerList.value.filter((item: any) => item.isFavorite);
      totalCount.value = customerList.value.length;
    }
  } catch (error: any) {
    ElNotification.error({ title: '操作失败', message: error?.message || '收藏操作失败，请稍后重试' });
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

onMounted(() => {
  fetchCustomerList();
  fetchStatistics();
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

.btn-favorite-active {
  border-color: rgba(201, 154, 69, 0.45) !important;
  color: $color-amber !important;
  background: rgba(201, 154, 69, 0.1) !important;
}

// ---- 统计卡片 ----
.statistics-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 20px;
}

.stat-card {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  position: relative;
  overflow: hidden;
  transition: transform 0.2s, box-shadow 0.2s;

  &:hover {
    transform: translateY(-2px);
    box-shadow: $shadow-md;
  }
}

.stat-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  &--blue  { background: rgba(50, 149, 201, 0.15); color: $color-steel-cyan; border: 1px solid rgba(50,149,201,0.25); }
  &--green { background: rgba(70, 191, 145, 0.15); color: $color-mint-green; border: 1px solid rgba(70,191,145,0.25); }
  &--cyan  { background: rgba(0, 212, 255, 0.12);  color: $cyan-primary;     border: 1px solid rgba(0,212,255,0.25); }
  &--amber { background: rgba(201, 154, 69, 0.15); color: $color-amber;      border: 1px solid rgba(201,154,69,0.25); }
}

.stat-body {
  .stat-value {
    font-size: 22px;
    font-weight: 700;
    color: $text-primary;
    font-family: 'Space Mono', monospace;
    line-height: 1.2;
  }
  .stat-label {
    font-size: 12px;
    color: $text-muted;
    margin-top: 3px;
  }
}

.stat-glow {
  position: absolute;
  right: -20px;
  top: -20px;
  width: 80px;
  height: 80px;
  border-radius: 50%;
  filter: blur(30px);
  opacity: 0.4;

  &--blue  { background: $color-steel-cyan; }
  &--green { background: $color-mint-green; }
  &--cyan  { background: $cyan-primary; }
  &--amber { background: $color-amber; }
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

.list-title {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
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

// ---- 表格 ----
.table-wrapper {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
  min-height: 200px;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;

  thead tr {
    background: rgba(0, 212, 255, 0.04);
    border-bottom: 1px solid rgba(0, 212, 255, 0.1);
  }

  th {
    padding: 12px 14px;
    text-align: left;
    font-size: 12px;
    font-weight: 500;
    color: $text-muted;
    white-space: nowrap;
    letter-spacing: 0.3px;
  }

  td {
    padding: 11px 14px;
    border-bottom: 1px solid rgba(255, 255, 255, 0.04);
    vertical-align: middle;
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
  background: rgba(0, 212, 255, 0.08);
  border: 1px solid rgba(0, 212, 255, 0.2);
  color: $cyan-primary;

  &:hover {
    background: rgba(0, 212, 255, 0.15);
    border-color: rgba(0, 212, 255, 0.4);
  }

  &--danger {
    background: rgba(201, 87, 69, 0.08);
    border-color: rgba(201, 87, 69, 0.2);
    color: #C95745;

    &:hover {
      background: rgba(201, 87, 69, 0.15);
      border-color: rgba(201, 87, 69, 0.4);
    }
  }

  &--favorite {
    background: rgba(201, 154, 69, 0.1);
    border-color: rgba(201, 154, 69, 0.3);
    color: $color-amber;
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
