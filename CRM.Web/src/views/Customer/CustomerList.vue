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
            placeholder="搜索客户名称/联系人..."
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
        <button class="btn-ghost btn-sm" @click="handleReset">重置</button>
      </div>
    </div>

    <!-- 卡片网格 -->
    <div v-loading="loading" class="card-grid">
      <div
        v-for="customer in customerList"
        :key="customer.id"
        class="customer-card"
        @click="handleView(customer)"
      >
        <!-- 卡片头部 -->
        <div class="card-header">
          <div class="card-avatar">
            <span class="avatar-letter">{{ (customer.customerName || customer.customerShortName || '?')[0] }}</span>
          </div>
          <div class="card-title-area">
            <div class="card-name">{{ customer.customerName || customer.customerShortName }}</div>
            <div class="card-code">{{ customer.customerCode }}</div>
          </div>
          <div v-if="customer.customerLevel" class="card-level-badge" :class="`level-${customer.customerLevel?.toLowerCase()}`">
            {{ getLevelLabel(customer.customerLevel) }}
          </div>
        </div>

        <!-- 类型和行业 -->
        <div class="card-meta">
          <span class="card-type-badge" v-if="customer.customerType">{{ getTypeLabel(customer.customerType) }}</span>
          <span class="card-industry" v-if="customer.industry">{{ getIndustryLabel(customer.industry) }}</span>
        </div>

        <!-- 联系人信息 -->
        <div class="card-contact">
          <template v-if="customer.contacts && customer.contacts.length > 0">
            <div class="contact-row">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>
              </svg>
              <span>{{ customer.contacts[0].contactName }}</span>
              <span class="contact-phone">{{ customer.contacts[0].mobilePhone }}</span>
            </div>
            <div class="contact-row" v-if="customer.city || customer.region">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/>
              </svg>
              <span>{{ customer.city || customer.region }}</span>
            </div>
          </template>
          <div v-else class="contact-row no-contact">暂无联系人</div>
        </div>

        <!-- 底部统计 -->
        <div class="card-footer">
          <div class="card-stat">
            <div class="card-stat-label">销售单数</div>
            <div class="card-stat-value">--</div>
          </div>
          <div class="card-stat">
            <div class="card-stat-label">累计销售额</div>
            <div class="card-stat-value card-stat-value--highlight">--</div>
          </div>
        </div>

        <!-- 操作按钮（hover 显示） -->
        <div class="card-actions" @click.stop>
          <button class="card-action-btn" @click.stop="handleEdit(customer)">编辑</button>
          <button class="card-action-btn card-action-btn--danger" @click.stop="handleDeleteCustomer(customer)">删除</button>
        </div>
      </div>

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
    <div class="pagination-wrapper" v-if="totalCount > pagination.pageSize">
      <el-pagination
        v-model:current-page="pagination.pageNumber"
        v-model:page-size="pagination.pageSize"
        :page-sizes="[12, 24, 48]"
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
import type { Customer, CustomerStatistics, CustomerSearchRequest } from '@/types/customer';

const router = useRouter();

const loading = ref(false);
const customerList = ref<Customer[]>([]);
const totalCount = ref(0);
const statistics = ref<CustomerStatistics | null>(null);

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

const pagination = reactive({ pageNumber: 1, pageSize: 12 });

const fetchCustomerList = async () => {
  loading.value = true;
  try {
    const params: CustomerSearchRequest = {
      ...searchForm,
      pageNumber: pagination.pageNumber,
      pageSize: pagination.pageSize
    };
    const response = await customerApi.searchCustomers(params);
    customerList.value = response.items.map((item: any) => ({
      ...item,
      customerName: item.customerName || item.officialName,
      customerShortName: item.customerShortName || item.nickName,
      customerLevel: item.customerLevel || (item.level ? ['', 'D', 'C', 'B', 'BPO', 'VIP', 'VPO'][item.level] : 'Normal'),
      customerType: item.customerType ?? item.type ?? 0,
      salesPersonName: item.salesPersonName,
      creditLimit: item.creditLimit ?? item.creditLine ?? 0,
      balance: item.balance ?? item.creditLineRemain ?? 0,
      isActive: item.isActive ?? (item.status === 1),
      contacts: item.contacts || []
    }));
    totalCount.value = response.totalCount;
  } catch (error) {
    console.error('获取客户列表失败', error);
    ElNotification.error({ title: '加载失败', message: '获取客户列表失败，请刷新重试' });
  } finally {
    loading.value = false;
  }
};

const fetchStatistics = async () => {
  try {
    const data = await customerApi.getCustomerStatistics();
    statistics.value = data;
  } catch (error) {
    console.error('获取统计数据失败', error);
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

const handleSizeChange = (size: number) => { pagination.pageSize = size; fetchCustomerList(); };
const handlePageChange = (page: number) => { pagination.pageNumber = page; fetchCustomerList(); };

const getLevelLabel = (level: string) => ({ VIP: 'VIP', VPO: 'VPO', BPO: 'BPO', B: 'B级', C: 'C级', D: 'D级', Important: '重要', Normal: '普通', Lead: '潜在' }[level] || level || '--');
// const getLevelType = (level: string) => ({ VIP: 'danger', Important: 'warning', Normal: 'info', Lead: '' }[level] || 'info');
const getTypeLabel = (type: number) => ({ 1: 'OEM', 2: 'ODM', 3: '终端用户', 4: 'IDH', 5: '贸易商', 6: '代理商' }[type] || '未知');
// const getTypeType = (type: number) => ({ 0: 'primary', 1: 'success', 2: 'warning' }[type] || 'info');
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

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.25);
    color: $text-primary;
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

/* 搜索栏 */
.search-bar {
  display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px;
}
.search-left { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.list-title { font-size: 14px; font-weight: 600; color: $text-primary; white-space: nowrap; }
.search-input-wrap { position: relative; display: flex; align-items: center; }
.search-icon { position: absolute; left: 10px; color: $text-muted; pointer-events: none; }
.search-input {
  width: 220px; padding: 7px 12px 7px 32px;
  background: $layer-2; border: 1px solid $border-panel; border-radius: $border-radius-md;
  color: $text-primary; font-size: 13px; font-family: 'Noto Sans SC', sans-serif; outline: none; transition: border-color 0.2s;
  &::placeholder { color: $text-muted; }
  &:focus { border-color: rgba(0,212,255,0.4); }
}
.status-select {
  width: 120px;
  :deep(.el-select__wrapper) { background: $layer-2 !important; box-shadow: none !important; border: 1px solid $border-panel !important; border-radius: $border-radius-md !important; }
  :deep(.el-select__placeholder) { color: $text-muted !important; }
  :deep(.el-select__selected-item) { color: $text-primary !important; }
}

/* 卡片网格 */
.card-grid {
  display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 16px; min-height: 200px;
}
.customer-card {
  background: $layer-2; border: 1px solid rgba(0,212,255,0.1);
  border-radius: 12px; padding: 16px; cursor: pointer; transition: all 0.2s;
  position: relative; overflow: hidden;
  &:hover {
    border-color: rgba(0,212,255,0.35);
    box-shadow: 0 4px 20px rgba(0,212,255,0.08);
    transform: translateY(-1px);
    .card-actions { opacity: 1; }
  }
}
.card-header { display: flex; align-items: flex-start; gap: 10px; margin-bottom: 10px; }
.card-avatar {
  width: 38px; height: 38px; flex-shrink: 0;
  background: linear-gradient(135deg, rgba(0,102,255,0.2), rgba(0,212,255,0.15));
  border: 1px solid rgba(0,212,255,0.2); border-radius: 10px;
  display: flex; align-items: center; justify-content: center;
  .avatar-letter { font-size: 16px; font-weight: 700; color: $cyan-primary; }
}
.card-title-area { flex: 1; min-width: 0; }
.card-name {
  font-size: 14px; font-weight: 600; color: $text-primary;
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-bottom: 2px;
}
.card-code { font-size: 11px; color: $text-muted; font-family: 'Space Mono', monospace; }
.card-level-badge {
  flex-shrink: 0; font-size: 11px; padding: 2px 8px; border-radius: 4px; font-weight: 500;
  &.level-vip { background: rgba(201,87,69,0.15); color: #C95745; border: 1px solid rgba(201,87,69,0.3); }
  &.level-important { background: rgba(201,154,69,0.15); color: #C99A45; border: 1px solid rgba(201,154,69,0.3); }
  &.level-normal { background: rgba(107,122,141,0.15); color: #8A9BB0; border: 1px solid rgba(107,122,141,0.3); }
  &.level-lead { background: rgba(70,191,145,0.12); color: #46BF91; border: 1px solid rgba(70,191,145,0.3); }
  &.level-bpo { background: rgba(0,212,255,0.12); color: $cyan-primary; border: 1px solid rgba(0,212,255,0.3); }
  &.level-vpo { background: rgba(0,212,255,0.12); color: $cyan-primary; border: 1px solid rgba(0,212,255,0.3); }
}
.card-meta { display: flex; align-items: center; gap: 6px; margin-bottom: 10px; }
.card-type-badge {
  font-size: 11px; padding: 1px 7px;
  background: rgba(50,149,201,0.12); border: 1px solid rgba(50,149,201,0.25);
  border-radius: 3px; color: $color-steel-cyan;
}
.card-industry {
  font-size: 11px; padding: 1px 7px;
  background: rgba(0,212,255,0.08); border: 1px solid rgba(0,212,255,0.18);
  border-radius: 3px; color: $cyan-primary;
}
.card-contact {
  margin-bottom: 12px; border-top: 1px solid rgba(255,255,255,0.05); padding-top: 10px;
}
.contact-row {
  display: flex; align-items: center; gap: 6px;
  font-size: 12px; color: $text-secondary; margin-bottom: 5px;
  svg { flex-shrink: 0; color: $text-muted; }
  &.no-contact { color: $text-muted; font-style: italic; }
}
.contact-phone { color: $text-muted; margin-left: 4px; font-family: 'Space Mono', monospace; font-size: 11px; }
.card-footer { display: flex; border-top: 1px solid rgba(255,255,255,0.05); padding-top: 10px; }
.card-stat {
  flex: 1;
  &:not(:last-child) { border-right: 1px solid rgba(255,255,255,0.05); padding-right: 12px; margin-right: 12px; }
}
.card-stat-label { font-size: 11px; color: $text-muted; margin-bottom: 3px; }
.card-stat-value {
  font-size: 15px; font-weight: 700; color: $text-primary; font-family: 'Space Mono', monospace;
  &--highlight { color: $cyan-primary; }
}
.card-actions {
  position: absolute; bottom: 0; left: 0; right: 0;
  background: linear-gradient(to top, rgba(10,22,40,0.95) 0%, transparent 100%);
  padding: 20px 16px 12px;
  display: flex; gap: 8px; justify-content: flex-end;
  opacity: 0; transition: opacity 0.2s;
}
.card-action-btn {
  padding: 5px 12px; font-size: 12px; border-radius: 5px;
  border: 1px solid rgba(0,212,255,0.3); background: rgba(0,212,255,0.1);
  color: $cyan-primary; cursor: pointer; transition: all 0.15s; font-family: 'Noto Sans SC', sans-serif;
  &:hover { background: rgba(0,212,255,0.2); }
  &--danger { border-color: rgba(201,87,69,0.3); background: rgba(201,87,69,0.1); color: #C95745; }
  &--danger:hover { background: rgba(201,87,69,0.2); }
}
.empty-state {
  grid-column: 1 / -1; display: flex; flex-direction: column; align-items: center; justify-content: center;
  padding: 60px 0; color: $text-muted; gap: 12px;
  svg { opacity: 0.3; } p { font-size: 14px; margin: 0; }
}

// 分页
.pagination-wrapper {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

.quantum-pagination {
  :deep(.el-pagination__total),
  :deep(.el-pagination__sizes),
  :deep(.el-pagination__jump) {
    color: $text-muted !important;
    font-size: 12px;
  }

  :deep(.el-pager li) {
    background: transparent !important;
    color: $text-muted !important;
    border: 1px solid transparent;
    border-radius: 6px;
    min-width: 28px;
    height: 28px;
    line-height: 28px;

    &.is-active {
      background: rgba(0, 212, 255, 0.15) !important;
      color: $cyan-primary !important;
      border-color: rgba(0, 212, 255, 0.3);
    }

    &:hover:not(.is-active) {
      background: rgba(255, 255, 255, 0.05) !important;
      color: $text-secondary !important;
    }
  }

  :deep(.btn-prev), :deep(.btn-next) {
    background: transparent !important;
    color: $text-muted !important;
    border: 1px solid $border-panel;
    border-radius: 6px;

    &:hover { border-color: rgba(0, 212, 255, 0.3); color: $text-secondary !important; }
  }
}

// Dropdown
:deep(.quantum-dropdown) {
  background: $layer-2 !important;
  border: 1px solid $border-card !important;
  border-radius: $border-radius-md !important;

  .el-dropdown-menu__item {
    color: $text-secondary !important;
    font-size: 13px;

    &:hover { background: rgba(0, 212, 255, 0.06) !important; color: $text-primary !important; }
  }

  .danger-item { color: $color-red-brown !important; }
}
</style>
