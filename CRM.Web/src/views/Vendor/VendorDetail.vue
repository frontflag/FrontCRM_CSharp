<template>
  <div class="vendor-detail-page" v-loading="loading">
    <!-- 头部 -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          返回
        </button>
        <div class="vendor-title-group">
          <div class="vendor-avatar-lg">{{ (vendor?.officialName || '?')[0] }}</div>
          <div>
            <h1 class="page-title">{{ vendor?.officialName || '供应商详情' }}</h1>
            <div class="title-meta">
              <span class="vendor-code">{{ vendor?.code }}</span>
              <span class="status-badge" :class="vendorStatusClass">{{ vendorStatusText }}</span>
              <span v-if="vendor?.credit" class="level-badge">信用 {{ vendor.credit }}</span>
              <span v-if="vendor?.blackList" class="blacklist-badge">黑名单</span>
            </div>
          </div>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="handleEdit">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
          </svg>
          编辑
        </button>
        <button class="btn-primary btn-primary--green" v-if="vendor?.status !== 1" @click="handleActivate">
          启用
        </button>
        <button class="btn-warning" v-else @click="handleDeactivate">
          停用
        </button>
        <button
          v-if="!vendor?.blackList"
          class="btn-danger"
          @click="handleAddToBlacklist"
        >
          加入黑名单
        </button>
        <button
          v-else
          class="btn-warning-outline"
          @click="handleRemoveFromBlacklist"
        >
          解除黑名单
        </button>
      </div>
    </div>

    <div v-if="vendor" class="detail-content">
      <!-- 基本信息卡片 -->
      <div class="info-section">
        <div class="section-header">
          <div class="section-dot section-dot--cyan"></div>
          <span class="section-title">基本信息</span>
        </div>
        <div class="info-grid">
          <div class="info-item">
            <span class="info-label">供应商编号</span>
            <span class="info-value info-value--code">{{ vendor.code }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">供应商名称</span>
            <span class="info-value">{{ vendor.officialName }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">简称</span>
            <span class="info-value">{{ vendor.nickName || '--' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">行业</span>
            <span class="info-value">{{ vendor.industry || '--' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">地区/地址</span>
            <span class="info-value">{{ vendor.officeAddress || '--' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">信用评级</span>
            <span class="info-value">{{ vendor.credit ?? '--' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">备注</span>
            <span class="info-value">{{ vendor.companyInfo || '--' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">创建时间</span>
            <span class="info-value info-value--time">{{ formatDateTime(vendor.createTime) }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">更新时间</span>
            <span class="info-value info-value--time">{{ formatDateTime(vendor.modifyTime) }}</span>
          </div>
        </div>
      </div>

      <!-- Tab 区域（预留） -->
      <div class="tabs-section">
        <div class="tabs-nav">
          <button
            v-for="tab in tabs"
            :key="tab.key"
            class="tab-btn"
            :class="{ 'tab-btn--active': activeTab === tab.key }"
            @click="activeTab = tab.key"
          >
            {{ tab.label }}
          </button>
        </div>
        <div class="tabs-body">
          <div v-show="activeTab === 'contacts'" class="contacts-tab">
            <div class="contacts-toolbar">
              <button class="btn-primary btn-sm" @click="openCreateContact">新增联系人</button>
            </div>
            <el-table :data="contacts" size="small" class="contacts-table">
              <el-table-column type="index" width="50" />
              <el-table-column prop="cName" label="姓名" min-width="120" />
              <el-table-column prop="title" label="职位" min-width="120" />
              <el-table-column prop="department" label="部门" min-width="120" />
              <el-table-column prop="mobile" label="手机" min-width="130" />
              <el-table-column prop="email" label="邮箱" min-width="180" show-overflow-tooltip />
              <el-table-column label="主联系人" width="90" align="center">
                <template #default="{ row }">
                  <el-tag v-if="row.isMain" type="success" size="small">主</el-tag>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="200" fixed="right">
                <template #default="{ row }">
                  <button class="action-btn" @click="openEditContact(row)">编辑</button>
                  <button class="action-btn action-btn--danger" @click="handleDeleteContact(row)">删除</button>
                  <button
                    v-if="!row.isMain"
                    class="action-btn action-btn--link"
                    @click="handleSetMainContact(row)"
                  >
                    设为主联系人
                  </button>
                </template>
              </el-table-column>
            </el-table>
          </div>
          <div v-show="activeTab === 'addresses'" class="addresses-tab">
            <div class="contacts-toolbar">
              <button class="btn-primary btn-sm" @click="openCreateAddress">新增地址</button>
            </div>
            <el-table :data="addresses" size="small" class="contacts-table">
              <el-table-column type="index" width="50" />
              <el-table-column prop="addressType" label="类型" width="100">
                <template #default="{ row }">
                  <span class="text-secondary">{{ getAddressTypeLabel(row.addressType) }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="contactName" label="联系人" width="120" />
              <el-table-column prop="contactPhone" label="联系电话" width="140" />
              <el-table-column label="详细地址" min-width="260" show-overflow-tooltip>
                <template #default="{ row }">
                  <span class="text-secondary">{{ formatFullAddress(row) }}</span>
                </template>
              </el-table-column>
              <el-table-column label="默认" width="80" align="center">
                <template #default="{ row }">
                  <el-tag v-if="row.isDefault" type="success" size="small">默认</el-tag>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="220" fixed="right">
                <template #default="{ row }">
                  <button class="action-btn" @click="openEditAddress(row)">编辑</button>
                  <button class="action-btn action-btn--danger" @click="handleDeleteAddress(row)">删除</button>
                  <button
                    v-if="!row.isDefault"
                    class="action-btn action-btn--link"
                    @click="handleSetDefaultAddress(row)"
                  >
                    设为默认
                  </button>
                </template>
              </el-table-column>
            </el-table>
          </div>
          <div v-show="activeTab === 'banks'" class="banks-tab">
            <div class="contacts-toolbar">
              <button class="btn-primary btn-sm" @click="openCreateBank">新增银行账户</button>
            </div>
            <el-table :data="banks" size="small" class="contacts-table">
              <el-table-column type="index" width="50" />
              <el-table-column prop="accountName" label="账户名称" min-width="160" show-overflow-tooltip />
              <el-table-column prop="bankName" label="开户银行" min-width="160" show-overflow-tooltip />
              <el-table-column prop="bankBranch" label="开户支行" min-width="160" show-overflow-tooltip />
              <el-table-column prop="bankAccount" label="银行账号" min-width="180" show-overflow-tooltip />
              <el-table-column label="默认" width="80" align="center">
                <template #default="{ row }">
                  <el-tag v-if="row.isDefault" type="success" size="small">默认</el-tag>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="220" fixed="right">
                <template #default="{ row }">
                  <button class="action-btn" @click="openEditBank(row)">编辑</button>
                  <button class="action-btn action-btn--danger" @click="handleDeleteBank(row)">删除</button>
                  <button
                    v-if="!row.isDefault"
                    class="action-btn action-btn--link"
                    @click="handleSetDefaultBank(row)"
                  >
                    设为默认
                  </button>
                </template>
              </el-table-column>
            </el-table>
          </div>
          <div v-show="activeTab === 'history'" class="history-tab">
            <div class="contacts-toolbar">
              <button class="btn-primary btn-sm" @click="showHistoryForm = true">新增联系记录</button>
            </div>
            <div v-if="showHistoryForm" class="inline-form">
              <el-select v-model="newHistory.type" placeholder="类型" size="small" style="width:120px">
                <el-option label="电话" value="call" />
                <el-option label="拜访" value="visit" />
                <el-option label="邮件" value="email" />
                <el-option label="会议" value="meeting" />
                <el-option label="其他" value="other" />
              </el-select>
              <el-input v-model="newHistory.subject" placeholder="主题" size="small" style="width:200px" />
              <el-input v-model="newHistory.content" placeholder="联系内容" size="small" style="flex:1" />
              <el-input v-model="newHistory.result" placeholder="联系结果" size="small" style="width:200px" />
              <button class="btn-primary btn-sm" @click="submitHistory">保存</button>
              <button class="action-btn" @click="cancelHistoryForm">取消</button>
            </div>
            <div v-if="histories.length === 0 && !showHistoryForm" class="empty-tab">
              <p>暂无联系记录</p>
            </div>
            <div v-for="h in histories" :key="h.id" class="timeline-item">
              <div class="timeline-dot dot--primary"></div>
              <div class="timeline-content">
                <div class="timeline-header">
                  <span class="timeline-text">
                    <strong>{{ historyTypeLabel(h.type) }}</strong>
                    <span v-if="h.subject"> · {{ h.subject }}</span>
                  </span>
                  <div class="timeline-actions">
                    <button class="action-btn action-btn--danger" @click="deleteHistory(h)">删除</button>
                  </div>
                </div>
                <div class="timeline-body">
                  <span v-if="h.content" class="timeline-line">内容：{{ h.content }}</span>
                  <span v-if="h.result" class="timeline-line">结果：{{ h.result }}</span>
                  <span class="timeline-time">
                    {{ formatDateTime(h.time || h.createTime) }}
                  </span>
                </div>
              </div>
            </div>
          </div>
          <div v-show="activeTab === 'documents'" class="documents-tab">
            <DocumentUploadPanel
              bizType="Vendor"
              :bizId="vendorId"
              @uploaded="documentListRef?.refresh?.()"
            />
            <DocumentListPanel ref="documentListRef" bizType="Vendor" :bizId="vendorId" view-mode="grid" />
          </div>
          <div v-show="activeTab === 'logs'" class="logs-tab">
            <div v-if="operationLogs.length === 0 && fieldChangeLogs.length === 0" class="empty-tab">
              <p>暂无操作记录</p>
            </div>
            <div v-if="operationLogs.length > 0" class="logs-section">
              <div class="section-header small-header">
                <div class="section-dot section-dot--cyan"></div>
                <span class="section-title">操作日志</span>
              </div>
              <div v-for="log in operationLogs" :key="log.id" class="timeline-item">
                <div class="timeline-dot dot--primary"></div>
                <div class="timeline-content">
                  <span class="timeline-text">{{ log.operationType }} · {{ log.operationDesc || log.description }}</span>
                  <span class="timeline-time">
                    {{ log.operatorUserName || '系统' }} · {{ formatDateTime(log.operationTime || log.createTime) }}
                  </span>
                </div>
              </div>
            </div>
            <div v-if="fieldChangeLogs.length > 0" class="logs-section">
              <div class="section-header small-header">
                <div class="section-dot section-dot--cyan"></div>
                <span class="section-title">字段变更日志</span>
              </div>
              <div v-for="log in fieldChangeLogs" :key="log.id" class="timeline-item">
                <div class="timeline-dot dot--warning"></div>
                <div class="timeline-content">
                  <span class="timeline-text">
                    {{ log.fieldLabel || log.fieldName }}：{{ log.oldValue || '(空)' }} → {{ log.newValue || '(空)' }}
                  </span>
                  <span class="timeline-time">
                    {{ log.changedByUserName || log.operatorUserName || '系统' }} ·
                    {{ formatDateTime(log.changedAt || log.changeTime || log.operationTime || log.createTime) }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <VendorContactDialog
      v-model="contactDialogVisible"
      :mode="contactDialogMode"
      :contact="editingContact"
      @confirm="handleContactDialogConfirm"
    />

    <VendorAddressDialog
      v-model="addressDialogVisible"
      :mode="addressDialogMode"
      :address="editingAddress"
      @confirm="handleAddressDialogConfirm"
    />

    <VendorBankDialog
      v-model="bankDialogVisible"
      :mode="bankDialogMode"
      :bank="editingBank"
      @confirm="handleBankDialogConfirm"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { vendorApi, vendorContactApi, vendorAddressApi, vendorBankApi } from '@/api/vendor';
import type { Vendor, VendorContactInfo, VendorAddress, VendorBankInfo, AddVendorContactRequest, UpdateVendorContactRequest } from '@/types/vendor';
import VendorContactDialog from './VendorContactDialog.vue';
import VendorAddressDialog from './VendorAddressDialog.vue';
import VendorBankDialog from './VendorBankDialog.vue';
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue';
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue';

const route = useRoute();
const router = useRouter();

const vendorId = route.params.id as string;
const loading = ref(false);
const vendor = ref<Vendor | null>(null);
const contacts = ref<VendorContactInfo[]>([]);
const addresses = ref<VendorAddress[]>([]);
const banks = ref<VendorBankInfo[]>([]);
const histories = ref<any[]>([]);
const operationLogs = ref<any[]>([]);
const fieldChangeLogs = ref<any[]>([]);
const documentListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null);

const tabs = [
  { key: 'contacts', label: '联系人' },
  { key: 'addresses', label: '地址信息' },
  { key: 'banks', label: '银行信息' },
  { key: 'history', label: '联系历史' },
  { key: 'documents', label: '文档' },
  { key: 'logs', label: '操作日志' }
];
const activeTab = ref('contacts');

const vendorStatusText = computed(() => {
  const s = vendor.value?.status ?? 0;
  if (s === 2) return '已审';
  if (s === 1) return '待审';
  return '草稿';
});

const vendorStatusClass = computed(() => {
  const s = vendor.value?.status ?? 0;
  if (s === 2) return 'status--approved';
  if (s === 1) return 'status--pending';
  return 'status--draft';
});

const formatDateTime = (val?: string) => {
  if (!val) return '--';
  const d = new Date(val);
  if (isNaN(d.getTime())) return val;
  return d.toLocaleString('zh-CN');
};

const fetchVendor = async () => {
  if (!vendorId) return;
  loading.value = true;
  try {
    vendor.value = await vendorApi.getVendorById(vendorId);
    contacts.value = await vendorContactApi.getContactsByVendorId(vendorId);
    addresses.value = await vendorAddressApi.getAddressesByVendorId(vendorId);
    banks.value = await vendorBankApi.getBanksByVendorId(vendorId);
    histories.value = await vendorApi.getVendorContactHistory(vendorId);
    const [ops, fields] = await Promise.all([
      vendorApi.getOperationLogs(vendorId),
      vendorApi.getFieldChangeLogs(vendorId)
    ]);
    operationLogs.value = Array.isArray(ops) ? ops : [];
    fieldChangeLogs.value = Array.isArray(fields) ? fields : [];
  } catch (e) {
    console.error(e);
    ElMessage.error('获取供应商详情失败');
  } finally {
    loading.value = false;
  }
};

const refreshContacts = async () => {
  contacts.value = await vendorContactApi.getContactsByVendorId(vendorId);
};

const refreshAddresses = async () => {
  addresses.value = await vendorAddressApi.getAddressesByVendorId(vendorId);
};

const refreshBanks = async () => {
  banks.value = await vendorBankApi.getBanksByVendorId(vendorId);
};

const showHistoryForm = ref(false);
const newHistory = ref<{ type: string; subject: string; content: string; result: string }>({
  type: 'call',
  subject: '',
  content: '',
  result: ''
});

const historyTypeLabel = (type: string | undefined) => {
  switch (type) {
    case 'call': return '电话';
    case 'visit': return '拜访';
    case 'email': return '邮件';
    case 'meeting': return '会议';
    default: return '其他';
  }
};

const submitHistory = async () => {
  if (!newHistory.value.content.trim()) {
    ElMessage.warning('请填写联系内容');
    return;
  }
  try {
    await vendorApi.addVendorContactHistory(vendorId, {
      type: newHistory.value.type,
      subject: newHistory.value.subject,
      content: newHistory.value.content,
      result: newHistory.value.result
    });
    ElMessage.success('联系记录已新增');
    showHistoryForm.value = false;
    newHistory.value = { type: 'call', subject: '', content: '', result: '' };
    histories.value = await vendorApi.getVendorContactHistory(vendorId);
  } catch {
    ElMessage.error('保存联系记录失败');
  }
};

const cancelHistoryForm = () => {
  showHistoryForm.value = false;
};

const deleteHistory = async (h: any) => {
  try {
    await ElMessageBox.confirm('确定要删除该联系记录吗？', '删除联系记录', { type: 'warning' });
    await vendorApi.deleteVendorContactHistory(vendorId, h.id);
    ElMessage.success('联系记录已删除');
    histories.value = await vendorApi.getVendorContactHistory(vendorId);
  } catch {
    // ignore
  }
};

const goBack = () => router.push('/vendors');
const handleEdit = () => router.push(`/vendors/${vendorId}/edit`);

const handleActivate = async () => {
  if (!vendor.value) return;
  try {
    await vendorApi.activateVendor(vendorId);
    vendor.value.status = 1;
    ElMessage.success('供应商已启用');
  } catch (e) {
    ElMessage.error('启用供应商失败');
  }
};

const handleDeactivate = async () => {
  if (!vendor.value) return;
  try {
    await vendorApi.deactivateVendor(vendorId);
    vendor.value.status = 0;
    ElMessage.success('供应商已停用');
  } catch (e) {
    ElMessage.error('停用供应商失败');
  }
};

const handleAddToBlacklist = async () => {
  if (!vendor.value) return;
  try {
    const { value } = await ElMessageBox.prompt('请输入加入黑名单的原因（可选）', '加入黑名单', {
      confirmButtonText: '确认',
      cancelButtonText: '取消',
      inputPlaceholder: '如：多次违约、账期严重逾期等',
      inputType: 'textarea'
    });
    await vendorApi.addToBlacklist(vendorId, value || '');
    vendor.value.blackList = true;
    ElMessage.success('已加入黑名单');
  } catch {
    // 用户取消或失败时忽略
  }
};

const handleRemoveFromBlacklist = async () => {
  if (!vendor.value) return;
  try {
    await ElMessageBox.confirm(
      `确定要将「${vendor.value.officialName || vendor.value.code}」移出黑名单吗？`,
      '解除黑名单',
      { type: 'warning' }
    );
    await vendorApi.removeFromBlacklist(vendorId);
    vendor.value.blackList = false;
    ElMessage.success('已移出黑名单');
  } catch {
    // 取消时忽略
  }
};

const contactDialogVisible = ref(false);
const contactDialogMode = ref<'create' | 'edit'>('create');
const editingContact = ref<VendorContactInfo | null>(null);

const openCreateContact = () => {
  contactDialogMode.value = 'create';
  editingContact.value = null;
  contactDialogVisible.value = true;
};

const openEditContact = (row: VendorContactInfo) => {
  contactDialogMode.value = 'edit';
  editingContact.value = row;
  contactDialogVisible.value = true;
};

const handleContactDialogConfirm = async (payload: AddVendorContactRequest | UpdateVendorContactRequest) => {
  try {
    if (contactDialogMode.value === 'create') {
      await vendorContactApi.createContact(vendorId, payload as AddVendorContactRequest);
      ElMessage.success('联系人已新增');
    } else if (editingContact.value) {
      await vendorContactApi.updateContact(editingContact.value.id, payload as UpdateVendorContactRequest);
      ElMessage.success('联系人已更新');
    }
    contactDialogVisible.value = false;
    await refreshContacts();
  } catch {
    ElMessage.error('保存联系人失败');
  }
};

const handleDeleteContact = async (row: VendorContactInfo) => {
  try {
    await ElMessageBox.confirm(`确定要删除联系人「${row.cName || row.eName || row.mobile || row.email}」吗？`, '删除联系人', {
      type: 'warning'
    });
    await vendorContactApi.deleteContact(row.id);
    ElMessage.success('联系人已删除');
    await refreshContacts();
  } catch {
    // ignore
  }
};

const handleSetMainContact = async (row: VendorContactInfo) => {
  try {
    await vendorApi.setMainContact(row.id);
    ElMessage.success('已设置为主联系人');
    await refreshContacts();
  } catch {
    ElMessage.error('设置主联系人失败');
  }
};

const addressDialogVisible = ref(false);
const addressDialogMode = ref<'create' | 'edit'>('create');
const editingAddress = ref<VendorAddress | null>(null);

const openCreateAddress = () => {
  addressDialogMode.value = 'create';
  editingAddress.value = null;
  addressDialogVisible.value = true;
};

const openEditAddress = (row: VendorAddress) => {
  addressDialogMode.value = 'edit';
  editingAddress.value = row;
  addressDialogVisible.value = true;
};

const handleAddressDialogConfirm = async (payload: any) => {
  try {
    if (addressDialogMode.value === 'create') {
      await vendorAddressApi.createAddress(vendorId, payload);
      ElMessage.success('地址已新增');
    } else if (editingAddress.value) {
      await vendorAddressApi.updateAddress(editingAddress.value.id, payload);
      ElMessage.success('地址已更新');
    }
    addressDialogVisible.value = false;
    await refreshAddresses();
  } catch {
    ElMessage.error('保存地址失败');
  }
};

const handleDeleteAddress = async (row: VendorAddress) => {
  try {
    await ElMessageBox.confirm(`确定要删除该地址「${formatFullAddress(row)}」吗？`, '删除地址', {
      type: 'warning'
    });
    await vendorAddressApi.deleteAddress(row.id);
    ElMessage.success('地址已删除');
    await refreshAddresses();
  } catch {
    // ignore
  }
};

const handleSetDefaultAddress = async (row: VendorAddress) => {
  try {
    await vendorAddressApi.setDefaultAddress(row.id);
    ElMessage.success('已设为默认地址');
    await refreshAddresses();
  } catch {
    ElMessage.error('设置默认地址失败');
  }
};

const getAddressTypeLabel = (type: number) => {
  if (type === 2) return '账单地址';
  return '收货地址';
};

const formatFullAddress = (addr: VendorAddress) => {
  const parts = [addr.province, addr.city, addr.area, addr.address].filter(Boolean);
  return parts.join(' ');
};

const bankDialogVisible = ref(false);
const bankDialogMode = ref<'create' | 'edit'>('create');
const editingBank = ref<VendorBankInfo | null>(null);

const openCreateBank = () => {
  bankDialogMode.value = 'create';
  editingBank.value = null;
  bankDialogVisible.value = true;
};

const openEditBank = (row: VendorBankInfo) => {
  bankDialogMode.value = 'edit';
  editingBank.value = row;
  bankDialogVisible.value = true;
};

const handleBankDialogConfirm = async (payload: any) => {
  try {
    if (bankDialogMode.value === 'create') {
      await vendorBankApi.createBank(vendorId, payload);
      ElMessage.success('银行账户已新增');
    } else if (editingBank.value) {
      await vendorBankApi.updateBank(editingBank.value.id, payload);
      ElMessage.success('银行账户已更新');
    }
    bankDialogVisible.value = false;
    await refreshBanks();
  } catch {
    ElMessage.error('保存银行账户失败');
  }
};

const handleDeleteBank = async (row: VendorBankInfo) => {
  try {
    await ElMessageBox.confirm(`确定要删除银行账户「${row.bankName || row.bankAccount || row.accountName}」吗？`, '删除银行账户', {
      type: 'warning'
    });
    await vendorBankApi.deleteBank(row.id);
    ElMessage.success('银行账户已删除');
    await refreshBanks();
  } catch {
    // ignore
  }
};

const handleSetDefaultBank = async (row: VendorBankInfo) => {
  try {
    await vendorBankApi.setDefaultBank(row.id);
    ElMessage.success('已设为默认银行账户');
    await refreshBanks();
  } catch {
    ElMessage.error('设置默认银行账户失败');
  }
};

onMounted(fetchVendor);
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.vendor-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

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

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $cyan-primary;
  }
}

.vendor-title-group {
  display: flex;
  align-items: center;
  gap: 12px;
}

.vendor-avatar-lg {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: linear-gradient(135deg, #0066ff, #00d4ff);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  font-weight: 700;
  color: #fff;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0 0 4px;
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
}

.vendor-code {
  font-size: 12px;
  color: $text-muted;
  padding: 2px 8px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
}

.status-badge {
  font-size: 12px;
  padding: 2px 8px;
  border-radius: 999px;
  &.status--draft {
    background: rgba(255, 255, 255, 0.06);
    color: $text-muted;
  }
  &.status--pending {
    background: rgba(255, 193, 7, 0.16);
    color: #ffc107;
  }
  &.status--approved {
    background: rgba(70, 191, 145, 0.16);
    color: #46bf91;
  }
}

.blacklist-badge {
  font-size: 12px;
  padding: 2px 8px;
  border-radius: 999px;
  background: rgba(201, 87, 69, 0.16);
  color: $color-red-brown;
}

.level-badge {
  font-size: 12px;
  padding: 2px 8px;
  border-radius: 999px;
  background: rgba(0, 212, 255, 0.12);
  color: $cyan-primary;
}

.detail-content {
  margin-top: 12px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.info-section {
  background: $layer-2;
  border-radius: $border-radius-md;
  border: 1px solid $border-panel;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  border-bottom: 1px solid $border-panel;
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  &--cyan {
    background: $cyan-primary;
  }
}

.section-title {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 12px 24px;
  padding: 16px;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.info-label {
  font-size: 12px;
  color: $text-muted;
}

.info-value {
  font-size: 13px;
  color: $text-primary;
  &--code {
    font-family: 'Space Mono', monospace;
  }
  &--time {
    font-size: 12px;
    color: $text-secondary;
  }
}

.tabs-section {
  background: $layer-2;
  border-radius: $border-radius-md;
  border: 1px solid $border-panel;
  overflow: hidden;
}

.tabs-nav {
  display: flex;
  gap: 4px;
  padding: 8px 8px 0;
}

.tab-btn {
  padding: 6px 12px;
  border-radius: 6px 6px 0 0;
  border: none;
  background: transparent;
  color: $text-muted;
  font-size: 13px;
  cursor: pointer;
  &--active {
    background: $layer-3;
    color: $text-primary;
    border-bottom: 1px solid transparent;
  }
}

.tabs-body {
  padding: 16px;
}

.empty-tab {
  text-align: center;
  color: $text-muted;
  font-size: 13px;
}

.history-tab {
  .inline-form {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px;
    padding: 12px;
    margin-bottom: 12px;
    background: rgba(0, 212, 255, 0.04);
    border-radius: $border-radius-md;
    border: 1px solid rgba(0, 212, 255, 0.15);
  }

  .timeline-item {
    display: flex;
    gap: 10px;
    padding: 10px 0;
    border-bottom: 1px solid $border-panel;
    &:last-child {
      border-bottom: none;
    }
  }

  .timeline-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    margin-top: 6px;
    &.dot--primary {
      background: $cyan-primary;
    }
  }

  .timeline-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 4px;
  }

  .timeline-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .timeline-text {
    font-size: 13px;
    color: $text-primary;
  }

  .timeline-body {
    display: flex;
    flex-direction: column;
    gap: 2px;
  }

  .timeline-line {
    font-size: 12px;
    color: $text-secondary;
  }

  .timeline-time {
    font-size: 11px;
    color: $text-muted;
  }

  .timeline-actions {
    display: flex;
    gap: 6px;
  }
}

.documents-tab {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.logs-tab {
  .logs-section {
    margin-bottom: 16px;
  }

  .small-header {
    padding: 6px 0 8px;
    margin-bottom: 8px;
    border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  }

  .timeline-item {
    display: flex;
    gap: 10px;
    padding: 8px 0;
    border-bottom: 1px solid rgba(255, 255, 255, 0.06);
    &:last-child {
      border-bottom: none;
    }
  }

  .timeline-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    margin-top: 6px;
    &.dot--primary {
      background: $cyan-primary;
    }
    &.dot--warning {
      background: $color-amber;
    }
  }

  .timeline-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 4px;
  }

  .timeline-text {
    font-size: 13px;
    color: $text-primary;
  }

  .timeline-time {
    font-size: 11px;
    color: $text-muted;
  }
}
</style>

