<template>
  <div class="customer-list-page">
    <!-- 页面标题和操作按钮 -->
    <div class="page-header">
      <div class="header-left">
        <h1>客户管理</h1>
        <el-tag type="info" class="customer-count">共 {{ totalCount }} 个客户</el-tag>
      </div>
      <div class="header-right">
        <el-button type="primary" @click="handleCreate">
          <el-icon><Plus /></el-icon>新增客户
        </el-button>
        <el-button @click="handleExport">
          <el-icon><Download /></el-icon>导出
        </el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="statistics-row" v-if="statistics">
      <el-card class="stat-card">
        <div class="stat-value">{{ statistics.totalCustomers }}</div>
        <div class="stat-label">客户总数</div>
      </el-card>
      <el-card class="stat-card">
        <div class="stat-value">{{ statistics.activeCustomers }}</div>
        <div class="stat-label">活跃客户</div>
      </el-card>
      <el-card class="stat-card">
        <div class="stat-value">{{ statistics.newThisMonth }}</div>
        <div class="stat-label">本月新增</div>
      </el-card>
      <el-card class="stat-card">
        <div class="stat-value">{{ statistics.totalBalance }}</div>
        <div class="stat-label">应收余额</div>
      </el-card>
    </div>

    <!-- 搜索和筛选区域 -->
    <el-card class="search-card">
      <el-form :model="searchForm" inline>
        <el-form-item label="关键字">
          <el-input
            v-model="searchForm.searchTerm"
            placeholder="客户编号/名称/联系人/电话"
            clearable
            @keyup.enter="handleSearch"
            style="width: 250px"
          />
        </el-form-item>
        <el-form-item label="客户类型">
          <el-select v-model="searchForm.customerType" placeholder="全部类型" clearable style="width: 120px">
            <el-option label="企业客户" :value="0" />
            <el-option label="个人客户" :value="1" />
            <el-option label="政府/机构" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item label="客户等级">
          <el-select v-model="searchForm.customerLevel" placeholder="全部等级" clearable style="width: 120px">
            <el-option label="VIP" value="VIP" />
            <el-option label="重要" value="Important" />
            <el-option label="普通" value="Normal" />
            <el-option label="潜在客户" value="Lead" />
          </el-select>
        </el-form-item>
        <el-form-item label="行业">
          <el-select v-model="searchForm.industry" placeholder="全部行业" clearable style="width: 150px">
            <el-option label="制造业" value="Manufacturing" />
            <el-option label="贸易/零售" value="Trading" />
            <el-option label="科技/IT" value="Technology" />
            <el-option label="建筑/工程" value="Construction" />
            <el-option label="医疗/健康" value="Healthcare" />
            <el-option label="教育" value="Education" />
            <el-option label="金融" value="Finance" />
            <el-option label="其他" value="Other" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.isActive" placeholder="全部状态" clearable style="width: 100px">
            <el-option label="启用" :value="true" />
            <el-option label="停用" :value="false" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>搜索
          </el-button>
          <el-button @click="handleReset">
            <el-icon><Refresh /></el-icon>重置
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 客户列表表格 -->
    <el-card class="table-card">
      <el-table
        :data="customerList"
        v-loading="loading"
        stripe
        highlight-current-row
        @row-click="handleRowClick"
      >
        <el-table-column type="index" width="50" align="center" />
        <el-table-column prop="customerCode" label="客户编号" width="120" sortable>
          <template #default="{ row }">
            <el-link type="primary" @click.stop="handleView(row)">{{ row.customerCode }}</el-link>
          </template>
        </el-table-column>
        <el-table-column prop="customerName" label="客户名称" min-width="180" show-overflow-tooltip>
          <template #default="{ row }">
            <div class="customer-name-cell">
              <el-avatar :size="32" :icon="UserFilled" class="customer-avatar" />
              <div class="customer-info">
                <div class="name">{{ row.customerName }}</div>
                <el-tag v-if="row.customerLevel" size="small" :type="getLevelType(row.customerLevel)">
                  {{ getLevelLabel(row.customerLevel) }}
                </el-tag>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="customerType" label="类型" width="100">
          <template #default="{ row }">
            <el-tag :type="getTypeType(row.customerType)">{{ getTypeLabel(row.customerType) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="industry" label="行业" width="120">
          <template #default="{ row }">
            {{ getIndustryLabel(row.industry) }}
          </template>
        </el-table-column>
        <el-table-column label="联系人" width="150">
          <template #default="{ row }">
            <div v-if="row.contacts && row.contacts.length > 0">
              <div>{{ row.contacts[0].contactName }}</div>
              <div class="contact-phone">{{ row.contacts[0].mobilePhone }}</div>
            </div>
            <span v-else class="no-data">--</span>
          </template>
        </el-table-column>
        <el-table-column prop="region" label="地区" width="120" show-overflow-tooltip />
        <el-table-column prop="creditLimit" label="信用额度" width="120" align="right">
          <template #default="{ row }">
            {{ formatCurrency(row.creditLimit) }}
          </template>
        </el-table-column>
        <el-table-column prop="balance" label="账户余额" width="120" align="right">
          <template #default="{ row }">
            <span :class="{ 'negative': row.balance && row.balance < 0 }">
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
            />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click.stop="handleView(row)">
              查看
            </el-button>
            <el-button link type="primary" size="small" @click.stop="handleEdit(row)">
              编辑
            </el-button>
            <el-dropdown @command="(cmd: any) => handleCommand(cmd, row)" @click.stop>
              <el-button link type="primary" size="small">
                更多<el-icon class="el-icon--right"><arrow-down /></el-icon>
              </el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="contact">添加联系人</el-dropdown-item>
                  <el-dropdown-item command="address">添加地址</el-dropdown-item>
                  <el-dropdown-item command="quote">创建报价</el-dropdown-item>
                  <el-dropdown-item command="order">创建订单</el-dropdown-item>
                  <el-dropdown-item divided command="delete" class="danger">删除</el-dropdown-item>
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
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Download, Search, Refresh, ArrowDown, UserFilled } from '@element-plus/icons-vue';
import { customerApi } from '@/api/customer';
import type { Customer, CustomerStatistics, CustomerSearchRequest } from '@/types/customer';

const router = useRouter();

// 加载状态
const loading = ref(false);

// 客户列表数据
const customerList = ref<Customer[]>([]);
const totalCount = ref(0);

// 统计数据
const statistics = ref<CustomerStatistics | null>(null);

// 搜索表单
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

// 分页
const pagination = reactive({
  pageNumber: 1,
  pageSize: 20
});

// 获取客户列表
const fetchCustomerList = async () => {
  loading.value = true;
  try {
    const params: CustomerSearchRequest = {
      ...searchForm,
      pageNumber: pagination.pageNumber,
      pageSize: pagination.pageSize
    };
    const response = await customerApi.searchCustomers(params);
    // 映射后端字段到前端字段
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
    console.error('获取客户列表失败:', error);
    ElMessage.error('获取客户列表失败');
  } finally {
    loading.value = false;
  }
};

// 获取统计数据
const fetchStatistics = async () => {
  try {
    statistics.value = await customerApi.getCustomerStatistics();
  } catch (error) {
    console.error('获取统计数据失败:', error);
  }
};

// 搜索
const handleSearch = () => {
  pagination.pageNumber = 1;
  fetchCustomerList();
};

// 重置搜索
const handleReset = () => {
  searchForm.searchTerm = '';
  searchForm.customerType = undefined;
  searchForm.customerLevel = undefined;
  searchForm.industry = undefined;
  searchForm.region = undefined;
  searchForm.isActive = undefined;
  pagination.pageNumber = 1;
  fetchCustomerList();
};

// 分页变化
const handlePageChange = (page: number) => {
  pagination.pageNumber = page;
  fetchCustomerList();
};

const handleSizeChange = (size: number) => {
  pagination.pageSize = size;
  pagination.pageNumber = 1;
  fetchCustomerList();
};

// 创建客户
const handleCreate = () => {
  router.push('/customers/create');
};

// 查看客户详情
const handleView = (row: Customer) => {
  router.push(`/customers/${row.id}`);
};

// 编辑客户
const handleEdit = (row: Customer) => {
  router.push(`/customers/${row.id}/edit`);
};

// 行点击
const handleRowClick = (row: Customer) => {
  router.push(`/customers/${row.id}`);
};

// 状态变更
const handleStatusChange = async (row: Customer, isActive: boolean) => {
  try {
    if (isActive) {
      await customerApi.activateCustomer(row.id);
      ElMessage.success('客户已启用');
    } else {
      await customerApi.deactivateCustomer(row.id);
      ElMessage.success('客户已停用');
    }
  } catch (error) {
    row.isActive = !isActive;
    ElMessage.error('状态变更失败');
  }
};

// 更多操作
const handleCommand = async (command: string, row: Customer) => {
  switch (command) {
    case 'contact':
      router.push(`/customers/${row.id}?tab=contacts&action=add`);
      break;
    case 'address':
      router.push(`/customers/${row.id}?tab=addresses&action=add`);
      break;
    case 'quote':
      router.push(`/quotes/create?customerId=${row.id}`);
      break;
    case 'order':
      router.push(`/orders/create?customerId=${row.id}`);
      break;
    case 'delete':
      handleDelete(row);
      break;
  }
};

// 删除客户
const handleDelete = async (row: Customer) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除客户 "${row.customerName}" 吗？此操作不可恢复。`,
      '确认删除',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    );
    await customerApi.deleteCustomer(row.id);
    ElMessage.success('删除成功');
    fetchCustomerList();
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败');
    }
  }
};

// 导出
const handleExport = () => {
  ElMessage.info('导出功能开发中...');
};

// 格式化货币
const formatCurrency = (value: number | undefined) => {
  if (value === undefined || value === null) return '¥0.00';
  return `¥${value.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',')}`;
};

// 获取类型标签
const getTypeLabel = (type: number) => {
  const map: Record<number, string> = {
    0: '企业',
    1: '个人',
    2: '政府'
  };
  return map[type] || '未知';
};

const getTypeType = (type: number) => {
  const map: Record<number, any> = {
    0: 'primary',
    1: 'success',
    2: 'warning'
  };
  return map[type] || '';
};

// 获取等级标签
const getLevelLabel = (level: string) => {
  const map: Record<string, string> = {
    'VIP': 'VIP',
    'Important': '重要',
    'Normal': '普通',
    'Lead': '潜在'
  };
  return map[level] || level;
};

const getLevelType = (level: string) => {
  const map: Record<string, any> = {
    'VIP': 'danger',
    'Important': 'warning',
    'Normal': 'info',
    'Lead': ''
  };
  return map[level] || '';
};

// 获取行业标签
const getIndustryLabel = (industry: string) => {
  const map: Record<string, string> = {
    'Manufacturing': '制造业',
    'Trading': '贸易/零售',
    'Technology': '科技/IT',
    'Construction': '建筑/工程',
    'Healthcare': '医疗/健康',
    'Education': '教育',
    'Finance': '金融',
    'Other': '其他'
  };
  return map[industry] || industry;
};

// 初始化
onMounted(() => {
  fetchCustomerList();
  fetchStatistics();
});
</script>

<style scoped lang="scss">
.customer-list-page {
  padding: 20px;

  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;

    .header-left {
      display: flex;
      align-items: center;
      gap: 12px;

      h1 {
        margin: 0;
        font-size: 24px;
        font-weight: 600;
      }
    }

    .header-right {
      display: flex;
      gap: 10px;
    }
  }

  .statistics-row {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 16px;
    margin-bottom: 20px;

    .stat-card {
      text-align: center;
      padding: 10px;

      .stat-value {
        font-size: 28px;
        font-weight: 600;
        color: #409eff;
        margin-bottom: 8px;
      }

      .stat-label {
        font-size: 14px;
        color: #909399;
      }
    }
  }

  .search-card {
    margin-bottom: 20px;
  }

  .table-card {
    .customer-name-cell {
      display: flex;
      align-items: center;
      gap: 10px;

      .customer-avatar {
        background-color: #409eff;
      }

      .customer-info {
        .name {
          font-weight: 500;
          margin-bottom: 4px;
        }
      }
    }

    .contact-phone {
      font-size: 12px;
      color: #909399;
    }

    .no-data {
      color: #909399;
    }

    .negative {
      color: #f56c6c;
    }

    .pagination-wrapper {
      display: flex;
      justify-content: flex-end;
      margin-top: 20px;
    }
  }
}

.danger {
  color: #f56c6c;
}
</style>
