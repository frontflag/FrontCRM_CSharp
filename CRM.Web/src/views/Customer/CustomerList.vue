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
        <button class="btn-secondary" @click="handleExport">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
            <polyline points="7 10 12 15 17 10"/>
            <line x1="12" y1="15" x2="12" y2="3"/>
          </svg>
          导出
        </button>
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

    <!-- 搜索和筛选区域 -->
    <div class="search-panel">
      <div class="search-panel-inner">
        <div class="search-field">
          <label class="field-label">关键字</label>
          <div class="input-wrapper">
            <span class="input-icon">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
              </svg>
            </span>
            <el-input
              v-model="searchForm.searchTerm"
              placeholder="客户编号/名称/联系人/电话"
              clearable
              @keyup.enter="handleSearch"
              class="custom-input"
            />
          </div>
        </div>
        <div class="search-field">
          <label class="field-label">客户类型</label>
          <el-select v-model="searchForm.customerType" placeholder="全部类型" clearable class="custom-select">
            <el-option label="企业客户" :value="0" />
            <el-option label="个人客户" :value="1" />
            <el-option label="政府/机构" :value="2" />
          </el-select>
        </div>
        <div class="search-field">
          <label class="field-label">客户等级</label>
          <el-select v-model="searchForm.customerLevel" placeholder="全部等级" clearable class="custom-select">
            <el-option label="VIP" value="VIP" />
            <el-option label="重要" value="Important" />
            <el-option label="普通" value="Normal" />
            <el-option label="潜在客户" value="Lead" />
          </el-select>
        </div>
        <div class="search-field">
          <label class="field-label">行业</label>
          <el-select v-model="searchForm.industry" placeholder="全部行业" clearable class="custom-select">
            <el-option label="制造业" value="Manufacturing" />
            <el-option label="贸易/零售" value="Trading" />
            <el-option label="科技/IT" value="Technology" />
            <el-option label="建筑/工程" value="Construction" />
            <el-option label="医疗/健康" value="Healthcare" />
            <el-option label="教育" value="Education" />
            <el-option label="金融" value="Finance" />
            <el-option label="其他" value="Other" />
          </el-select>
        </div>
        <div class="search-field">
          <label class="field-label">状态</label>
          <el-select v-model="searchForm.isActive" placeholder="全部状态" clearable class="custom-select">
            <el-option label="启用" :value="true" />
            <el-option label="停用" :value="false" />
          </el-select>
        </div>
        <div class="search-actions">
          <button class="btn-primary btn-sm" @click="handleSearch">
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
            </svg>
            搜索
          </button>
          <button class="btn-ghost btn-sm" @click="handleReset">
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="1 4 1 10 7 10"/>
              <path d="M3.51 15a9 9 0 1 0 .49-3.51"/>
            </svg>
            重置
          </button>
        </div>
      </div>
    </div>

    <!-- 客户列表表格 -->
    <div class="table-panel">
      <el-table
        :data="customerList"
        v-loading="loading"
        @row-click="handleRowClick"
        class="quantum-table"
        :header-cell-style="tableHeaderStyle"
        :cell-style="tableCellStyle"
        :row-style="tableRowStyle"
      >
        <el-table-column type="index" width="50" align="center" />
        <el-table-column prop="customerCode" label="客户编号" width="130" sortable>
          <template #default="{ row }">
            <span class="code-link" @click.stop="handleView(row)">{{ row.customerCode }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="customerName" label="客户名称" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            <div class="customer-name-cell">
              <div class="customer-avatar">{{ (row.customerName || '?')[0] }}</div>
              <div class="customer-info">
                <div class="name">{{ row.customerName }}</div>
                <span v-if="row.customerLevel" class="level-badge" :class="`level-${row.customerLevel?.toLowerCase()}`">
                  {{ getLevelLabel(row.customerLevel) }}
                </span>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="customerType" label="类型" width="100">
          <template #default="{ row }">
            <span class="type-badge" :class="`type-${row.customerType}`">{{ getTypeLabel(row.customerType) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="industry" label="行业" width="120">
          <template #default="{ row }">
            <span class="text-secondary">{{ getIndustryLabel(row.industry) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="联系人" width="160">
          <template #default="{ row }">
            <div v-if="row.contacts && row.contacts.length > 0" class="contact-cell">
              <div class="contact-name">{{ row.contacts[0].contactName }}</div>
              <div class="contact-phone">{{ row.contacts[0].mobilePhone }}</div>
            </div>
            <span v-else class="no-data">--</span>
          </template>
        </el-table-column>
        <el-table-column prop="region" label="地区" width="120" show-overflow-tooltip>
          <template #default="{ row }">
            <span class="text-secondary">{{ row.region || '--' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="creditLimit" label="信用额度" width="120" align="right">
          <template #default="{ row }">
            <span class="amount-value">{{ formatCurrency(row.creditLimit) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="balance" label="账户余额" width="120" align="right">
          <template #default="{ row }">
            <span class="amount-value" :class="{ 'amount-negative': row.balance && row.balance < 0 }">
              {{ formatCurrency(row.balance) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="isActive" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-switch
              v-model="row.isActive"
              @change="(val: any) => handleStatusChange(row, val)"
              @click.stop
              :active-color="'#46BF91'"
              :inactive-color="'rgba(255,255,255,0.1)'"
            />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <button class="action-btn" @click.stop="handleView(row)">查看</button>
            <button class="action-btn" @click.stop="handleEdit(row)">编辑</button>
            <el-dropdown @command="(cmd: any) => handleCommand(cmd, row)" @click.stop>
              <button class="action-btn action-btn--more">
                更多
                <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="6 9 12 15 18 9"/>
                </svg>
              </button>
              <template #dropdown>
                <el-dropdown-menu class="quantum-dropdown">
                  <el-dropdown-item command="contact">添加联系人</el-dropdown-item>
                  <el-dropdown-item command="address">添加地址</el-dropdown-item>
                  <el-dropdown-item command="quote">创建报价</el-dropdown-item>
                  <el-dropdown-item command="order">创建订单</el-dropdown-item>
                  <el-dropdown-item divided command="delete" class="danger-item">删除</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="pagination.pageNumber"
          v-model:page-size="pagination.pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="totalCount"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handlePageChange"
          class="quantum-pagination"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
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

const pagination = reactive({ pageNumber: 1, pageSize: 20 });

const tableHeaderStyle = () => ({
  background: '#0A1628',
  color: 'rgba(200,216,232,0.55)',
  fontSize: '12px',
  fontWeight: '500',
  letterSpacing: '0.5px',
  borderBottom: '1px solid rgba(0,212,255,0.12)',
  padding: '10px 0'
});

const tableCellStyle = () => ({
  background: 'transparent',
  borderBottom: '1px solid rgba(255,255,255,0.05)',
  color: 'rgba(224,244,255,0.85)',
  fontSize: '13px'
});

const tableRowStyle = () => ({
  background: 'transparent',
  cursor: 'pointer'
});

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
    ElMessage.error('获取客户列表失败');
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
const handleRowClick = (row: Customer) => handleView(row);

const handleStatusChange = async (row: Customer, val: boolean) => {
  try {
    if (val) {
      await customerApi.activateCustomer(row.id!);
    } else {
      await customerApi.deactivateCustomer(row.id!);
    }
    ElMessage.success(`客户已${val ? '启用' : '停用'}`);
  } catch {
    row.isActive = !val;
    ElMessage.error('操作失败');
  }
};

const handleCommand = (cmd: string, row: Customer) => {
  if (cmd === 'delete') {
    ElMessageBox.confirm(`确定要删除客户 "${row.customerName}" 吗？`, '确认删除', {
      confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning'
    }).then(async () => {
      await customerApi.deleteCustomer(row.id!);
      ElMessage.success('删除成功');
      fetchCustomerList();
    }).catch(() => {});
  } else {
    ElMessage.info('功能开发中');
  }
};

const handleExport = () => ElMessage.info('导出功能开发中');
const handleSizeChange = (size: number) => { pagination.pageSize = size; fetchCustomerList(); };
const handlePageChange = (page: number) => { pagination.pageNumber = page; fetchCustomerList(); };

const getLevelLabel = (level: string) => ({ VIP: 'VIP', Important: '重要', Normal: '普通', Lead: '潜在' }[level] || level);
// const getLevelType = (level: string) => ({ VIP: 'danger', Important: 'warning', Normal: 'info', Lead: '' }[level] || 'info');
const getTypeLabel = (type: number) => ({ 0: '企业', 1: '个人', 2: '机构' }[type] || '未知');
// const getTypeType = (type: number) => ({ 0: 'primary', 1: 'success', 2: 'warning' }[type] || 'info');
const getIndustryLabel = (industry: string) => ({
  Manufacturing: '制造业', Trading: '贸易/零售', Technology: '科技/IT',
  Construction: '建筑/工程', Healthcare: '医疗/健康', Education: '教育',
  Finance: '金融', Other: '其他'
}[industry] || industry || '--');

const formatCurrency = (val: number | undefined) => {
  if (val === undefined || val === null) return '--';
  return new Intl.NumberFormat('zh-CN', { style: 'currency', currency: 'CNY', minimumFractionDigits: 2 }).format(val);
};

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

// ---- 搜索面板 ----
.search-panel {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  padding: 18px 20px;
  margin-bottom: 16px;
}

.search-panel-inner {
  display: flex;
  align-items: flex-end;
  gap: 16px;
  flex-wrap: wrap;
}

.search-field {
  display: flex;
  flex-direction: column;
  gap: 6px;

  .field-label {
    font-size: 11px;
    font-weight: 500;
    color: $text-muted;
    letter-spacing: 0.5px;
    text-transform: uppercase;
  }
}

.search-actions {
  display: flex;
  gap: 8px;
  align-items: center;
  padding-bottom: 1px;
}

.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;

  .input-icon {
    position: absolute;
    left: 10px;
    z-index: 2;
    color: rgba(0, 212, 255, 0.45);
    display: flex;
    align-items: center;
    pointer-events: none;
  }

  :deep(.el-input__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    padding-left: 32px !important;
    height: 34px;

    &.is-focus {
      border-color: rgba(0, 212, 255, 0.5) !important;
      box-shadow: 0 0 0 2px rgba(0, 212, 255, 0.08) !important;
    }
  }

  :deep(.el-input__inner) {
    color: $text-primary !important;
    background: transparent !important;
    font-size: 13px;
    &::placeholder { color: $text-placeholder !important; }
  }
}

.custom-select {
  :deep(.el-select__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    height: 34px;
    color: $text-primary !important;

    &.is-focused {
      border-color: rgba(0, 212, 255, 0.5) !important;
    }
  }

  :deep(.el-select__placeholder) {
    color: $text-placeholder !important;
  }
}

// ---- 表格面板 ----
.table-panel {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.quantum-table {
  width: 100%;
  background: transparent !important;

  :deep(.el-table__inner-wrapper) { background: transparent; }
  :deep(.el-table__header-wrapper) { background: $layer-2; }
  :deep(.el-table__body-wrapper) { background: transparent; }

  :deep(tr) {
    background: transparent !important;
    transition: background 0.15s;

    &:hover td { background: rgba(0, 212, 255, 0.04) !important; }
  }

  :deep(.el-table__fixed-right) {
    background: $layer-2 !important;
    .el-table__fixed-right-patch { background: $layer-2; }
  }

  :deep(.el-loading-mask) { background: rgba(10, 22, 40, 0.7); }
}

// 表格内容样式
.code-link {
  color: $color-ice-blue;
  cursor: pointer;
  font-family: 'Space Mono', monospace;
  font-size: 12px;
  transition: color 0.2s;

  &:hover { color: $cyan-primary; text-decoration: underline; }
}

.customer-name-cell {
  display: flex;
  align-items: center;
  gap: 10px;

  .customer-avatar {
    width: 32px;
    height: 32px;
    background: linear-gradient(135deg, rgba(0,102,255,0.3), rgba(0,212,255,0.2));
    border: 1px solid rgba(0, 212, 255, 0.2);
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 13px;
    font-weight: 600;
    color: $cyan-primary;
    flex-shrink: 0;
  }

  .customer-info {
    display: flex;
    flex-direction: column;
    gap: 3px;

    .name {
      font-size: 13px;
      color: $text-primary;
      font-weight: 500;
    }
  }
}

.level-badge {
  display: inline-block;
  font-size: 10px;
  padding: 1px 6px;
  border-radius: 3px;
  font-weight: 500;

  &.level-vip     { background: rgba(201, 87, 69, 0.2);  color: #C95745; border: 1px solid rgba(201,87,69,0.3); }
  &.level-important { background: rgba(201,154,69,0.2); color: #C99A45; border: 1px solid rgba(201,154,69,0.3); }
  &.level-normal  { background: rgba(107,122,141,0.2);  color: #8A9BB0; border: 1px solid rgba(107,122,141,0.3); }
  &.level-lead    { background: rgba(70,191,145,0.15);  color: #46BF91; border: 1px solid rgba(70,191,145,0.3); }
}

.type-badge {
  display: inline-block;
  font-size: 11px;
  padding: 2px 8px;
  border-radius: 4px;

  &.type-0 { background: rgba(50,149,201,0.15); color: $color-steel-cyan; border: 1px solid rgba(50,149,201,0.25); }
  &.type-1 { background: rgba(70,191,145,0.15); color: $color-mint-green; border: 1px solid rgba(70,191,145,0.25); }
  &.type-2 { background: rgba(201,154,69,0.15); color: $color-amber;      border: 1px solid rgba(201,154,69,0.25); }
}

.contact-cell {
  .contact-name { font-size: 13px; color: $text-secondary; }
  .contact-phone { font-size: 11px; color: $text-muted; font-family: 'Space Mono', monospace; margin-top: 2px; }
}

.text-secondary { color: $text-secondary; font-size: 13px; }
.no-data { color: $text-muted; font-size: 12px; }

.amount-value {
  font-family: 'Space Mono', monospace;
  font-size: 12px;
  color: $text-secondary;
}

.amount-negative { color: $color-red-brown !important; }

// 操作按钮
.action-btn {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  padding: 3px 8px;
  background: transparent;
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 4px;
  color: $color-ice-blue;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.15s;
  margin-right: 4px;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.4);
    color: $cyan-primary;
  }

  &--more {
    color: $text-muted;
    border-color: $border-panel;

    &:hover {
      color: $text-secondary;
      border-color: rgba(0, 212, 255, 0.2);
    }
  }
}

// 分页
.pagination-wrapper {
  padding: 16px 20px;
  border-top: 1px solid rgba(255, 255, 255, 0.05);
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
