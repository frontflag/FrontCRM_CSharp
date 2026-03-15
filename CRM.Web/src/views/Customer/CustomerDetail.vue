<template>
  <div class="customer-detail-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <el-button @click="goBack">
          <el-icon><ArrowLeft /></el-icon>返回
        </el-button>
        <h1>{{ customer?.customerName || '客户详情' }}</h1>
        <el-tag v-if="customer" :type="customer.isActive ? 'success' : 'info'">
          {{ customer.isActive ? '启用' : '停用' }}
        </el-tag>
      </div>
      <div class="header-right">
        <el-button @click="handleEdit">
          <el-icon><Edit /></el-icon>编辑
        </el-button>
        <el-button type="primary" @click="handleCreateQuote">
          <el-icon><Document /></el-icon>创建报价
        </el-button>
        <el-button type="primary" @click="handleCreateOrder">
          <el-icon><ShoppingCart /></el-icon>创建订单
        </el-button>
      </div>
    </div>

    <div v-loading="loading" class="detail-content">
      <template v-if="customer">
        <!-- 基本信息卡片 -->
        <el-card class="info-card">
          <template #header>
            <div class="card-header">
              <span>基本信息</span>
              <el-tag :type="getLevelType(customer.customerLevel)">
                {{ getLevelLabel(customer.customerLevel) }}
              </el-tag>
            </div>
          </template>
          <el-descriptions :column="3" border>
            <el-descriptions-item label="客户编号">{{ customer.customerCode }}</el-descriptions-item>
            <el-descriptions-item label="客户名称">{{ customer.customerName }}</el-descriptions-item>
            <el-descriptions-item label="客户类型">
              <el-tag :type="getTypeType(customer.customerType)">
                {{ getTypeLabel(customer.customerType) }}
              </el-tag>
            </el-descriptions-item>
            <el-descriptions-item label="统一社会信用代码">{{ customer.unifiedSocialCreditCode || '--' }}</el-descriptions-item>
            <el-descriptions-item label="行业">{{ getIndustryLabel(customer.industry || '') }}</el-descriptions-item>
            <el-descriptions-item label="地区">{{ customer.region || '--' }}</el-descriptions-item>
            <el-descriptions-item label="所属业务员">{{ customer.salesPersonName || '--' }}</el-descriptions-item>
            <el-descriptions-item label="信用额度">{{ formatCurrency(customer.creditLimit) }}</el-descriptions-item>
            <el-descriptions-item label="账期(天)">{{ customer.paymentTerms || 0 }}</el-descriptions-item>
            <el-descriptions-item label="账户余额" :span="1">
              <span :class="{ 'negative': customer.balance && customer.balance < 0 }">
                {{ formatCurrency(customer.balance) }}
              </span>
            </el-descriptions-item>
            <el-descriptions-item label="创建时间">{{ formatDateTime(customer.createdAt) }}</el-descriptions-item>
            <el-descriptions-item label="更新时间">{{ formatDateTime(customer.updatedAt) }}</el-descriptions-item>
          </el-descriptions>
        </el-card>

        <!-- 标签页 -->
        <el-card class="tabs-card">
          <el-tabs v-model="activeTab">
            <!-- 联系人标签 -->
            <el-tab-pane label="联系人" name="contacts">
              <div class="tab-toolbar">
                <el-button type="primary" size="small" @click="showContactDialog = true">
                  <el-icon><Plus /></el-icon>添加联系人
                </el-button>
              </div>
              <el-table :data="customer.contacts" stripe>
                <el-table-column prop="contactName" label="姓名" width="100" />
                <el-table-column prop="gender" label="性别" width="70">
                  <template #default="{ row }">
                    {{ row.gender === 0 ? '男' : row.gender === 1 ? '女' : '未知' }}
                  </template>
                </el-table-column>
                <el-table-column prop="department" label="部门" width="120" />
                <el-table-column prop="position" label="职位" width="120" />
                <el-table-column prop="mobilePhone" label="手机" width="130" />
                <el-table-column prop="email" label="邮箱" min-width="180" show-overflow-tooltip />
                <el-table-column prop="isDefault" label="默认" width="80" align="center">
                  <template #default="{ row }">
                    <el-tag v-if="row.isDefault" type="success" size="small">是</el-tag>
                    <span v-else>--</span>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="150" fixed="right">
                  <template #default="{ row }">
                    <el-button link type="primary" size="small" @click="editContact(row)">编辑</el-button>
                    <el-button link type="danger" size="small" @click="deleteContact(row)">删除</el-button>
                  </template>
                </el-table-column>
              </el-table>
            </el-tab-pane>

            <!-- 地址标签 -->
            <el-tab-pane label="地址信息" name="addresses">
              <div class="tab-toolbar">
                <el-button type="primary" size="small" @click="showAddressDialog = true">
                  <el-icon><Plus /></el-icon>添加地址
                </el-button>
              </div>
              <el-table :data="customer.addresses" stripe>
                <el-table-column prop="addressType" label="地址类型" width="100">
                  <template #default="{ row }">
                    {{ getAddressTypeLabel(row.addressType) }}
                  </template>
                </el-table-column>
                <el-table-column prop="contactPerson" label="联系人" width="100" />
                <el-table-column prop="contactPhone" label="联系电话" width="130" />
                <el-table-column label="详细地址" min-width="250" show-overflow-tooltip>
                  <template #default="{ row }">
                    {{ formatFullAddress(row) }}
                  </template>
                </el-table-column>
                <el-table-column prop="isDefault" label="默认" width="80" align="center">
                  <template #default="{ row }">
                    <el-tag v-if="row.isDefault" type="success" size="small">是</el-tag>
                    <span v-else>--</span>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="150" fixed="right">
                  <template #default="{ row }">
                    <el-button link type="primary" size="small" @click="editAddress(row)">编辑</el-button>
                    <el-button link type="danger" size="small" @click="deleteAddress(row)">删除</el-button>
                  </template>
                </el-table-column>
              </el-table>
            </el-tab-pane>

            <!-- 银行信息标签 -->
            <el-tab-pane label="银行信息" name="banks">
              <div class="tab-toolbar">
                <el-button type="primary" size="small" @click="showBankDialog = true">
                  <el-icon><Plus /></el-icon>添加银行信息
                </el-button>
              </div>
              <el-table :data="customer.banks" stripe>
                <el-table-column prop="accountName" label="账户名称" min-width="150" />
                <el-table-column prop="bankName" label="开户银行" min-width="180" />
                <el-table-column prop="bankBranch" label="开户支行" min-width="180" show-overflow-tooltip />
                <el-table-column prop="accountNumber" label="银行账号" min-width="180" />
                <el-table-column prop="currency" label="币种" width="80">
                  <template #default="{ row }">
                    {{ getCurrencyLabel(row.currency) }}
                  </template>
                </el-table-column>
                <el-table-column prop="isDefault" label="默认" width="80" align="center">
                  <template #default="{ row }">
                    <el-tag v-if="row.isDefault" type="success" size="small">是</el-tag>
                    <span v-else>--</span>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="150" fixed="right">
                  <template #default="{ row }">
                    <el-button link type="primary" size="small" @click="editBank(row)">编辑</el-button>
                    <el-button link type="danger" size="small" @click="deleteBank(row)">删除</el-button>
                  </template>
                </el-table-column>
              </el-table>
            </el-tab-pane>

            <!-- 业务记录标签 -->
            <el-tab-pane label="业务记录" name="business">
              <el-timeline>
                <el-timeline-item
                  v-for="(record, index) in businessRecords"
                  :key="index"
                  :type="record.type"
                  :timestamp="record.time"
                >
                  {{ record.content }}
                </el-timeline-item>
              </el-timeline>
            </el-tab-pane>
          </el-tabs>
        </el-card>
      </template>

      <el-empty v-else description="客户信息加载失败" />
    </div>

    <!-- 添加联系人对话框 -->
    <ContactDialog
      v-model="showContactDialog"
      :customer-id="customerId"
      :contact="editingContact"
      @success="handleContactSuccess"
    />

    <!-- 添加地址对话框 -->
    <AddressDialog
      v-model="showAddressDialog"
      :customer-id="customerId"
      :address="editingAddress"
      @success="handleAddressSuccess"
    />

    <!-- 添加银行信息对话框 -->
    <BankDialog
      v-model="showBankDialog"
      :customer-id="customerId"
      :bank="editingBank"
      @success="handleBankSuccess"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
  ArrowLeft, Edit, Plus, Document, ShoppingCart
} from '@element-plus/icons-vue';
import { customerApi, customerContactApi, customerAddressApi, customerBankApi } from '@/api/customer';
import type { Customer, CustomerContactInfo, CustomerAddress, CustomerBankInfo } from '@/types/customer';
import ContactDialog from './components/ContactDialog.vue';
import AddressDialog from './components/AddressDialog.vue';
import BankDialog from './components/BankDialog.vue';

const route = useRoute();
const router = useRouter();

// 客户ID
const customerId = route.params.id as string;

// 加载状态
const loading = ref(false);

// 客户数据
const customer = ref<Customer | null>(null);

// 当前标签页
const activeTab = ref('contacts');

// 对话框显示状态
const showContactDialog = ref(false);
const showAddressDialog = ref(false);
const showBankDialog = ref(false);

// 编辑数据
const editingContact = ref<CustomerContactInfo | undefined>(undefined);
const editingAddress = ref<CustomerAddress | undefined>(undefined);
const editingBank = ref<CustomerBankInfo | undefined>(undefined);

// 业务记录（模拟数据）
const businessRecords = ref([
  { type: 'primary', time: '2024-03-15 10:30', content: '创建报价单 QT-2024-001' },
  { type: 'success', time: '2024-03-10 14:20', content: '销售订单 SO-2024-003 已发货' },
  { type: 'warning', time: '2024-03-05 09:15', content: '收款 ¥50,000.00' },
  { type: 'primary', time: '2024-02-28 16:45', content: '创建询价单 RFQ-2024-002' }
]);

// 获取客户详情
const fetchCustomerDetail = async () => {
  loading.value = true;
  try {
    customer.value = await customerApi.getCustomerById(customerId);
  } catch (error) {
    console.error('获取客户详情失败:', error);
    ElMessage.error('获取客户详情失败');
  } finally {
    loading.value = false;
  }
};

// 返回
const goBack = () => {
  router.push('/customers');
};

// 编辑客户
const handleEdit = () => {
  router.push(`/customers/${customerId}/edit`);
};

// 创建报价
const handleCreateQuote = () => {
  router.push(`/quotes/create?customerId=${customerId}`);
};

// 创建订单
const handleCreateOrder = () => {
  router.push(`/orders/create?customerId=${customerId}`);
};

// 编辑联系人
const editContact = (contact: CustomerContactInfo) => {
  editingContact.value = contact;
  showContactDialog.value = true;
};

// 删除联系人
const deleteContact = async (contact: CustomerContactInfo) => {
  try {
    await ElMessageBox.confirm('确定要删除该联系人吗？', '确认删除', {
      type: 'warning'
    });
    await customerContactApi.deleteContact(contact.id);
    ElMessage.success('删除成功');
    fetchCustomerDetail();
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败');
    }
  }
};

// 联系人操作成功
const handleContactSuccess = () => {
  editingContact.value = undefined;
  fetchCustomerDetail();
};

// 编辑地址
const editAddress = (address: CustomerAddress) => {
  editingAddress.value = address;
  showAddressDialog.value = true;
};

// 删除地址
const deleteAddress = async (address: CustomerAddress) => {
  try {
    await ElMessageBox.confirm('确定要删除该地址吗？', '确认删除', {
      type: 'warning'
    });
    await customerAddressApi.deleteAddress(address.id);
    ElMessage.success('删除成功');
    fetchCustomerDetail();
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败');
    }
  }
};

// 地址操作成功
const handleAddressSuccess = () => {
  editingAddress.value = undefined;
  fetchCustomerDetail();
};

// 编辑银行信息
const editBank = (bank: CustomerBankInfo) => {
  editingBank.value = bank;
  showBankDialog.value = true;
};

// 删除银行信息
const deleteBank = async (bank: CustomerBankInfo) => {
  try {
    await ElMessageBox.confirm('确定要删除该银行信息吗？', '确认删除', {
      type: 'warning'
    });
    await customerBankApi.deleteBank(bank.id);
    ElMessage.success('删除成功');
    fetchCustomerDetail();
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败');
    }
  }
};

// 银行操作成功
const handleBankSuccess = () => {
  editingBank.value = undefined;
  fetchCustomerDetail();
};

// 格式化货币
const formatCurrency = (value: number | undefined) => {
  if (value === undefined || value === null) return '¥0.00';
  return `¥${value.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',')}`;
};

// 格式化日期时间
const formatDateTime = (date: string | undefined) => {
  if (!date) return '--';
  return new Date(date).toLocaleString('zh-CN');
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
    'VIP': 'VIP客户',
    'Important': '重要客户',
    'Normal': '普通客户',
    'Lead': '潜在客户'
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

// 获取地址类型标签
const getAddressTypeLabel = (type: string) => {
  const map: Record<string, string> = {
    'Office': '办公地址',
    'Billing': '开票地址',
    'Shipping': '收货地址',
    'Registered': '注册地址'
  };
  return map[type] || type;
};

// 格式化完整地址
const formatFullAddress = (address: CustomerAddress) => {
  const parts = [
    address.country,
    address.province,
    address.city,
    address.district,
    address.streetAddress
  ].filter(Boolean);
  return parts.join(' ');
};

// 获取币种标签
const getCurrencyLabel = (currency: number) => {
  const map: Record<number, string> = {
    1: 'CNY',
    2: 'USD',
    3: 'EUR',
    4: 'JPY',
    5: 'GBP',
    6: 'HKD'
  };
  return map[currency] || 'CNY';
};

// 初始化
onMounted(() => {
  fetchCustomerDetail();
});
</script>

<style scoped lang="scss">
.customer-detail-page {
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

  .detail-content {
    .info-card {
      margin-bottom: 20px;

      .card-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
      }
    }

    .tabs-card {
      .tab-toolbar {
        margin-bottom: 16px;
      }
    }
  }

  .negative {
    color: #f56c6c;
  }
}
</style>
