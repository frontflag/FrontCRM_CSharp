<template>
  <div class="customer-detail-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
          返回
        </button>
        <div class="customer-title-group">
          <div class="customer-avatar-lg">{{ (customer?.customerName || '?')[0] }}</div>
          <div>
            <h1 class="page-title">{{ customer?.customerName || '客户详情' }}</h1>
            <div class="title-meta">
              <span class="customer-code">{{ customer?.customerCode }}</span>
              <span class="status-badge" :class="customer?.isActive ? 'status--active' : 'status--inactive'">
                {{ customer?.isActive ? '启用' : '停用' }}
              </span>
              <span v-if="customer?.blackList" class="status-badge status--blacklist">黑名单</span>
              <span v-if="customer?.customerLevel" class="level-badge" :class="`level-${customer.customerLevel?.toLowerCase()}`">
                {{ getLevelLabel(customer.customerLevel) }}
              </span>
            </div>
            <div class="title-tags-row">
              <TagListDisplay :tags="customerTags" />
              <button class="btn-add-tag" @click="showTagDialog = true">添加标签</button>
            </div>
          </div>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="handleEdit">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
          </svg>
          编辑
        </button>
        <button v-if="!customer?.blackList" class="btn-warning" @click="showBlacklistDialog = true">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/><line x1="4.93" y1="4.93" x2="19.07" y2="19.07"/>
          </svg>
          加入黑名单
        </button>
        <button v-else class="btn-warning btn-warning--active" @click="handleRemoveBlacklist">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/><polyline points="9 12 11 14 15 10"/>
          </svg>
          解除黑名单
        </button>
        <button class="btn-danger" @click="showDeleteDialog = true">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
          </svg>
          删除客户
        </button>
        <button class="btn-primary" @click="handleCreateQuote">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
            <polyline points="14 2 14 8 20 8"/>
          </svg>
          创建报价
        </button>
        <button class="btn-primary btn-primary--green" @click="handleCreateOrder">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="9" cy="21" r="1"/><circle cx="20" cy="21" r="1"/>
            <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"/>
          </svg>
          创建订单
        </button>
      </div>
    </div>

    <div v-loading="loading" element-loading-background="rgba(10,22,40,0.8)" class="detail-content">
      <template v-if="customer">
        <!-- 基本信息卡片 -->
        <div class="info-section">
          <div class="section-header">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">基本信息</span>
          </div>
          <div class="info-grid">
            <div class="info-item">
              <span class="info-label">客户编号</span>
              <span class="info-value info-value--code">{{ customer.customerCode }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">客户名称</span>
              <span class="info-value">{{ customer.customerName }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">客户类型</span>
              <span class="type-badge" :class="`type-${customer.customerType ?? 0}`">{{ getTypeLabel(customer.customerType ?? 0) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">统一社会信用代码</span>
              <span class="info-value info-value--code">{{ customer.unifiedSocialCreditCode || '--' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">行业</span>
              <span class="info-value">{{ getIndustryLabel(customer.industry || '') }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">地区</span>
              <span class="info-value">{{ customer.region || '--' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">所属业务员</span>
              <span class="info-value">{{ customer.salesPersonName || '--' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">信用额度</span>
              <span class="info-value info-value--amount">{{ formatCurrency(customer.creditLimit) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">账期(天)</span>
              <span class="info-value">{{ customer.paymentTerms || 0 }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">账户余额</span>
              <span class="info-value info-value--amount" :class="{ 'amount-negative': customer.balance && customer.balance < 0 }">
                {{ formatCurrency(customer.balance) }}
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">创建时间</span>
              <span class="info-value info-value--time">{{ formatDateTime(customer.createdAt) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">更新时间</span>
              <span class="info-value info-value--time">{{ formatDateTime(customer.updatedAt) }}</span>
            </div>
          </div>
        </div>

        <!-- 标签页 -->
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
            <!-- 联系人 -->
            <div v-show="activeTab === 'contacts'">
              <div class="tab-toolbar">
                <button class="btn-add-item" @click="goAddContact">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
                  </svg>
                  添加联系人
                </button>
              </div>
              <CrmDataTable :data="customer.contacts" class="quantum-table"
                :header-cell-style="tableHeaderStyle" :cell-style="tableCellStyle" :row-style="tableRowStyle">
                <el-table-column prop="contactName" label="姓名" width="100">
                  <template #default="{ row }"><span class="cell-primary">{{ row.contactName }}</span></template>
                </el-table-column>
                <el-table-column prop="gender" label="性别" width="70">
                  <template #default="{ row }">
                    <span class="cell-muted">{{ row.gender === 0 ? '男' : row.gender === 1 ? '女' : '未知' }}</span>
                  </template>
                </el-table-column>
                <el-table-column prop="department" label="部门" width="120">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.department || '--' }}</span></template>
                </el-table-column>
                <el-table-column prop="position" label="职位" width="120">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.position || '--' }}</span></template>
                </el-table-column>
                <el-table-column prop="mobilePhone" label="手机" width="140">
                  <template #default="{ row }"><span class="cell-code">{{ row.mobilePhone }}</span></template>
                </el-table-column>
                <el-table-column prop="email" label="邮箱" min-width="180" show-overflow-tooltip>
                  <template #default="{ row }"><span class="cell-secondary">{{ row.email || '--' }}</span></template>
                </el-table-column>
                <el-table-column prop="isDefault" label="默认" width="80" align="center">
                  <template #default="{ row }">
                    <span v-if="row.isDefault" class="default-badge">默认</span>
                    <span v-else class="cell-muted">--</span>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="150" fixed="right">
                  <template #default="{ row }">
                    <button class="action-btn" @click="goEditContact(row)">编辑</button>
                    <button class="action-btn action-btn--danger" @click="deleteContact(row)">删除</button>
                  </template>
                </el-table-column>
              </CrmDataTable>
            </div>

            <!-- 地址信息 -->
            <div v-show="activeTab === 'addresses'">
              <div class="tab-toolbar">
                <button class="btn-add-item" @click="showAddressDialog = true">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
                  </svg>
                  添加地址
                </button>
              </div>
              <CrmDataTable :data="customer.addresses" class="quantum-table"
                :header-cell-style="tableHeaderStyle" :cell-style="tableCellStyle" :row-style="tableRowStyle">
                <el-table-column prop="addressType" label="地址类型" width="110">
                  <template #default="{ row }">
                    <span class="type-badge type-0">{{ getAddressTypeLabel(row.addressType) }}</span>
                  </template>
                </el-table-column>
                <el-table-column prop="contactPerson" label="联系人" width="100">
                  <template #default="{ row }"><span class="cell-primary">{{ row.contactPerson || '--' }}</span></template>
                </el-table-column>
                <el-table-column prop="contactPhone" label="联系电话" width="140">
                  <template #default="{ row }"><span class="cell-code">{{ row.contactPhone || '--' }}</span></template>
                </el-table-column>
                <el-table-column label="详细地址" min-width="250" show-overflow-tooltip>
                  <template #default="{ row }"><span class="cell-secondary">{{ formatFullAddress(row) }}</span></template>
                </el-table-column>
                <el-table-column prop="isDefault" label="默认" width="80" align="center">
                  <template #default="{ row }">
                    <span v-if="row.isDefault" class="default-badge">默认</span>
                    <span v-else class="cell-muted">--</span>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="150" fixed="right">
                  <template #default="{ row }">
                    <button class="action-btn" @click="editAddress(row)">编辑</button>
                    <button class="action-btn action-btn--danger" @click="deleteAddress(row)">删除</button>
                  </template>
                </el-table-column>
              </CrmDataTable>
            </div>

            <!-- 银行信息 -->
            <div v-show="activeTab === 'banks'">
              <div class="tab-toolbar">
                <button class="btn-add-item" @click="showBankDialog = true">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
                  </svg>
                  添加银行信息
                </button>
              </div>
              <CrmDataTable :data="customer.banks" class="quantum-table"
                :header-cell-style="tableHeaderStyle" :cell-style="tableCellStyle" :row-style="tableRowStyle">
                <el-table-column prop="accountName" label="账户名称" min-width="150">
                  <template #default="{ row }"><span class="cell-primary">{{ row.accountName }}</span></template>
                </el-table-column>
                <el-table-column prop="bankName" label="开户银行" min-width="180">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.bankName }}</span></template>
                </el-table-column>
                <el-table-column prop="bankBranch" label="开户支行" min-width="180" show-overflow-tooltip>
                  <template #default="{ row }"><span class="cell-secondary">{{ row.bankBranch || '--' }}</span></template>
                </el-table-column>
                <el-table-column prop="accountNumber" label="银行账号" min-width="180">
                  <template #default="{ row }"><span class="cell-code">{{ row.accountNumber }}</span></template>
                </el-table-column>
                <el-table-column prop="currency" label="币种" width="80">
                  <template #default="{ row }"><span class="cell-muted">{{ getCurrencyLabel(row.currency) }}</span></template>
                </el-table-column>
                <el-table-column prop="isDefault" label="默认" width="80" align="center">
                  <template #default="{ row }">
                    <span v-if="row.isDefault" class="default-badge">默认</span>
                    <span v-else class="cell-muted">--</span>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="150" fixed="right">
                  <template #default="{ row }">
                    <button class="action-btn" @click="editBank(row)">编辑</button>
                    <button class="action-btn action-btn--danger" @click="deleteBank(row)">删除</button>
                  </template>
                </el-table-column>
              </CrmDataTable>
            </div>

            <!-- 联系历史 -->
            <div v-show="activeTab === 'contactHistory'">
              <div class="tab-toolbar">
                <button class="btn-add-item" @click="showContactHistoryForm = true">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
                  </svg>
                  添加记录
                </button>
              </div>
              <!-- 新增表单 -->
              <div v-if="showContactHistoryForm" class="inline-form">
                <el-select v-model="newHistory.type" placeholder="类型" size="small" style="width:120px">
                  <el-option label="电话" value="电话"/>
                  <el-option label="邮件" value="邮件"/>
                  <el-option label="拜访" value="拜访"/>
                  <el-option label="微信" value="微信"/>
                  <el-option label="其他" value="其他"/>
                </el-select>
                <el-input v-model="newHistory.content" placeholder="联系内容" size="small" style="flex:1" />
                <el-input v-model="newHistory.followUpResult" placeholder="跟进结果" size="small" style="width:200px" />
                <button class="btn-add-item" @click="submitContactHistory">保存</button>
                <button class="action-btn" @click="showContactHistoryForm = false">取消</button>
              </div>
              <div v-if="contactHistories.length === 0 && !showContactHistoryForm" class="empty-state">
                <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>
                <p>暂无联系记录</p>
              </div>
              <div v-for="h in contactHistories" :key="h.id" class="timeline-item">
                <div class="timeline-dot dot--primary"></div>
                <div class="timeline-content" style="flex:1">
                  <div style="display:flex;justify-content:space-between;align-items:center">
                    <span class="timeline-text"><strong>{{ h.contactType || h.type }}</strong> · {{ h.content }}</span>
                    <div>
                      <button class="action-btn" style="margin-right:4px" @click="deleteHistory(h)">删除</button>
                    </div>
                  </div>
                  <span v-if="h.followUpResult" class="timeline-time">跟进结果：{{ h.followUpResult }}</span>
                  <span class="timeline-time">{{ formatDateTime(h.contactTime || h.createTime) }}</span>
                </div>
              </div>
            </div>

            <!-- 操作日志 -->
            <div v-show="activeTab === 'logs'">
              <div v-if="operationLogs.length === 0 && fieldChangeLogs.length === 0" class="empty-state">
                <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
                <p>暂无操作记录</p>
              </div>
              <div v-if="operationLogs.length > 0">
                <div class="section-header" style="padding:8px 0 10px;border-bottom:1px solid rgba(255,255,255,0.06);margin-bottom:12px">
                  <div class="section-dot section-dot--cyan"></div>
                  <span class="section-title">操作日志</span>
                </div>
                <div v-for="log in operationLogs" :key="log.id" class="timeline-item">
                  <div class="timeline-dot dot--primary"></div>
                  <div class="timeline-content">
                    <span class="timeline-text">{{ log.operationType }} · {{ log.description }}</span>
                    <span class="timeline-time">{{ log.operatorUserName || '系统' }} · {{ formatDateTime(log.operationTime || log.createTime) }}</span>
                  </div>
                </div>
              </div>
              <div v-if="fieldChangeLogs.length > 0" style="margin-top:20px">
                <div class="section-header" style="padding:8px 0 10px;border-bottom:1px solid rgba(255,255,255,0.06);margin-bottom:12px">
                  <div class="section-dot section-dot--cyan"></div>
                  <span class="section-title">字段变更日志</span>
                </div>
                <div v-for="log in fieldChangeLogs" :key="log.id" class="timeline-item">
                  <div class="timeline-dot dot--warning"></div>
                  <div class="timeline-content">
                    <span class="timeline-text">{{ log.fieldName }}：{{ log.oldValue || '(空)' }} → {{ log.newValue || '(空)' }}</span>
                    <span class="timeline-time">{{ log.operatorUserName || '系统' }} · {{ formatDateTime(log.operationTime || log.createTime) }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </template>

      <div v-else-if="!loading" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
          <circle cx="12" cy="12" r="10"/>
          <line x1="12" y1="8" x2="12" y2="12"/>
          <line x1="12" y1="16" x2="12.01" y2="16"/>
        </svg>
        <p>客户信息加载失败</p>
      </div>
    </div>

    <!-- 对话框 -->
    <AddressDialog v-model="showAddressDialog" :customer-id="customerId" :address="editingAddress" @success="handleAddressSuccess" />
    <BankDialog v-model="showBankDialog" :customer-id="customerId" :bank="editingBank" @success="handleBankSuccess" />

    <ApplyTagsDialog
      v-model="showTagDialog"
      entity-type="CUSTOMER"
      :entity-ids="[customerId]"
      title="为客户添加标签"
      @success="fetchCustomerTags"
    />

    <!-- 删除客户弹窗 -->
    <el-dialog v-model="showDeleteDialog" title="确认删除客户" width="440px" :close-on-click-modal="false">
      <div style="color:rgba(200,216,232,0.75);font-size:13px;margin-bottom:16px">删除后可在回收站中查看并恢复。</div>
      <el-form label-width="90px">
        <el-form-item label="删除理由">
          <el-input v-model="deleteReason" type="textarea" :rows="3" placeholder="请输入删除理由" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showDeleteDialog = false">取消</el-button>
        <el-button type="danger" :loading="actionLoading" @click="handleDelete">确认删除</el-button>
      </template>
    </el-dialog>

    <!-- 加入黑名单弹窗 -->
    <el-dialog v-model="showBlacklistDialog" title="加入黑名单" width="440px" :close-on-click-modal="false">
      <div style="color:rgba(200,216,232,0.75);font-size:13px;margin-bottom:16px">加入黑名单后可在黑名单管理中查看并移出。</div>
      <el-form label-width="90px">
        <el-form-item label="黑名单理由" required>
          <el-input v-model="blacklistReason" type="textarea" :rows="3" placeholder="请输入黑名单理由（必填）" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showBlacklistDialog = false">取消</el-button>
        <el-button type="warning" :loading="actionLoading" @click="handleAddBlacklist">确认加入黑名单</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElNotification, ElMessageBox } from 'element-plus';
import { customerApi, customerContactApi, customerAddressApi, customerBankApi } from '@/api/customer';
import { tagApi, type TagDefinitionDto } from '@/api/tag';
import TagListDisplay from '@/components/Tag/TagListDisplay.vue';
import ApplyTagsDialog from '@/components/Tag/ApplyTagsDialog.vue';
import type { Customer, CustomerContactInfo, CustomerAddress, CustomerBankInfo } from '@/types/customer';

import AddressDialog from './components/AddressDialog.vue';
import BankDialog from './components/BankDialog.vue';
import { formatDisplayDateTime } from '@/utils/displayDateTime';

const route = useRoute();
const router = useRouter();
const customerId = route.params.id as string;
const loading = ref(false);
const customer = ref<Customer | null>(null);
const customerTags = ref<TagDefinitionDto[]>([]);
const activeTab = ref('contacts');
const showAddressDialog = ref(false);
const showBankDialog = ref(false);
const editingAddress = ref<CustomerAddress | undefined>(undefined);
const editingBank = ref<CustomerBankInfo | undefined>(undefined);

const tabs = [
  { key: 'contacts', label: '联系人' },
  { key: 'addresses', label: '地址信息' },
  { key: 'banks', label: '銀行信息' },
  { key: 'contactHistory', label: '联系历史' },
  { key: 'logs', label: '操作日志' }
];

// 联系历史
const contactHistories = ref<any[]>([]);
const showContactHistoryForm = ref(false);
const newHistory = ref({ type: '', content: '', followUpResult: '' });

// 操作日志
const operationLogs = ref<any[]>([]);
const fieldChangeLogs = ref<any[]>([]);

// 删除 / 黑名单
const showDeleteDialog = ref(false);
const showBlacklistDialog = ref(false);
const deleteReason = ref('');
const blacklistReason = ref('');
const actionLoading = ref(false);
const showTagDialog = ref(false);

const tableHeaderStyle = () => ({ background: '#0A1628', color: 'rgba(200,216,232,0.55)', fontSize: '12px', fontWeight: '500', letterSpacing: '0.5px', borderBottom: '1px solid rgba(0,212,255,0.12)', padding: '10px 0' });
const tableCellStyle = () => ({ background: 'transparent', borderBottom: '1px solid rgba(255,255,255,0.05)', color: 'rgba(224,244,255,0.85)', fontSize: '13px' });
const tableRowStyle = () => ({ background: 'transparent' });

const fetchCustomerDetail = async () => {
  loading.value = true;
  try { customer.value = await customerApi.getCustomerById(customerId); }
  catch { ElNotification.error({ title: '加载失败', message: '获取客户详情失败，请刷新重试' }); }
  finally { loading.value = false; }
};

const fetchCustomerTags = async () => {
  try {
    customerTags.value = await tagApi.getEntityTags('CUSTOMER', customerId);
  } catch {
    customerTags.value = [];
  }
};

const fetchContactHistory = async () => {
  try { contactHistories.value = await customerApi.getCustomerContactHistory(customerId); }
  catch { /* 静默失败 */ }
};

const fetchLogs = async () => {
  try {
    const [ops, fields] = await Promise.all([
      customerApi.getOperationLogs(customerId),
      customerApi.getFieldChangeLogs(customerId)
    ]);
    operationLogs.value = Array.isArray(ops) ? ops : [];
    fieldChangeLogs.value = Array.isArray(fields) ? fields : [];
  } catch { /* 静默失败 */ }
};

const submitContactHistory = async () => {
  if (!newHistory.value.content.trim()) { ElNotification.warning({ title: '请填写内容', message: '请输入联系内容' }); return; }
  try {
    await customerApi.addContactHistory(customerId, {
      contactType: newHistory.value.type,
      content: newHistory.value.content,
      followUpResult: newHistory.value.followUpResult
    });
    ElNotification.success({ title: '添加成功', message: '联系记录已添加' });
    newHistory.value = { type: '', content: '', followUpResult: '' };
    showContactHistoryForm.value = false;
    fetchContactHistory();
  } catch { ElNotification.error({ title: '添加失败', message: '联系记录添加失败，请稍后重试' }); }
};

const deleteHistory = async (h: any) => {
  try {
    await ElMessageBox.confirm('确定要删除该联系记录吗？', '确认删除', { type: 'warning' });
    await customerApi.deleteContactHistory(customerId, h.id);
    ElNotification.success({ title: '删除成功', message: '联系记录已删除' }); fetchContactHistory();
  } catch (e) { if (e !== 'cancel') ElNotification.error({ title: '删除失败', message: '联系记录删除失败' }); }
};

const handleDelete = async () => {
  actionLoading.value = true;
  try {
    await customerApi.deleteCustomer(customerId, deleteReason.value);
    ElNotification.success({ title: '删除成功', message: '客户已移至回收站' });
    showDeleteDialog.value = false;
    router.push('/customers');
  } catch { ElNotification.error({ title: '删除失败', message: '客户删除失败，请稍后重试' }); }
  finally { actionLoading.value = false; }
};

const handleAddBlacklist = async () => {
  if (!blacklistReason.value.trim()) { ElNotification.warning({ title: '请填写理由', message: '请输入黑名单理由' }); return; }
  actionLoading.value = true;
  try {
    await customerApi.addToBlacklist(customerId, blacklistReason.value);
    ElNotification.success({ title: '操作成功', message: '客户已加入黑名单' });
    showBlacklistDialog.value = false;
    blacklistReason.value = '';
    fetchCustomerDetail();
  } catch { ElNotification.error({ title: '操作失败', message: '加入黑名单失败，请稍后重试' }); }
  finally { actionLoading.value = false; }
};

const handleRemoveBlacklist = async () => {
  try {
    await ElMessageBox.confirm('确定要解除该客户的黑名单状态吗？', '解除黑名单', { type: 'warning' });
    await customerApi.removeFromBlacklist(customerId);
    ElNotification.success({ title: '操作成功', message: '已解除黑名单状态' }); fetchCustomerDetail();
  } catch (e) { if (e !== 'cancel') ElNotification.error({ title: '操作失败', message: '解除黑名单失败' }); }
};

const goBack = () => router.push('/customers');
const handleEdit = () => router.push(`/customers/${customerId}/edit`);
const handleCreateQuote = () => ElNotification.info({ title: '功能开发中', message: '报价单功能正在开发中，敬请期待' });
const handleCreateOrder = () => ElNotification.info({ title: '功能开发中', message: '订单功能正在开发中，敬请期待' });

const goAddContact = () => router.push({ name: 'CustomerContactCreate', params: { id: customerId } });
const goEditContact = (contact: CustomerContactInfo) => router.push({ name: 'CustomerContactEdit', params: { id: customerId, contactId: contact.id } });
const deleteContact = async (contact: CustomerContactInfo) => {
  try {
    await ElMessageBox.confirm('确定要删除该联系人吗？', '确认删除', { type: 'warning' });
    await customerContactApi.deleteContact(contact.id);
    ElNotification.success({ title: '删除成功', message: '联系人已删除' }); fetchCustomerDetail();
  } catch (e) { if (e !== 'cancel') ElNotification.error({ title: '删除失败', message: '联系人删除失败' }); }
};


const editAddress = (address: CustomerAddress) => { editingAddress.value = address; showAddressDialog.value = true; };
const deleteAddress = async (address: CustomerAddress) => {
  try {
    await ElMessageBox.confirm('确定要删除该地址吗？', '确认删除', { type: 'warning' });
    await customerAddressApi.deleteAddress(address.id);
    ElNotification.success({ title: '删除成功', message: '地址已删除' }); fetchCustomerDetail();
  } catch (e) { if (e !== 'cancel') ElNotification.error({ title: '删除失败', message: '地址删除失败' }); }
};
const handleAddressSuccess = () => { editingAddress.value = undefined; fetchCustomerDetail(); };

const editBank = (bank: CustomerBankInfo) => { editingBank.value = bank; showBankDialog.value = true; };
const deleteBank = async (bank: CustomerBankInfo) => {
  try {
    await ElMessageBox.confirm('确定要删除该银行信息吗？', '确认删除', { type: 'warning' });
    await customerBankApi.deleteBank(bank.id);
    ElNotification.success({ title: '删除成功', message: '银行信息已删除' }); fetchCustomerDetail();
  } catch (e) { if (e !== 'cancel') ElNotification.error({ title: '删除失败', message: '银行信息删除失败' }); }
};
const handleBankSuccess = () => { editingBank.value = undefined; fetchCustomerDetail(); };

const formatCurrency = (value: number | undefined) => {
  if (value === undefined || value === null) return '¥0.00';
  return `¥${value.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',')}`;
};
const formatDateTime = (date: string | undefined) => (date ? formatDisplayDateTime(date) : '--');
const getTypeLabel = (type: number) => ({ 0: '企业', 1: '个人', 2: '政府' }[type] || '未知');
const getLevelLabel = (level: string) => ({ VIP: 'VIP客户', Important: '重要客户', Normal: '普通客户', Lead: '潜在客户' }[level] || level);
// const getLevelType = (level: string) => ({ VIP: 'danger', Important: 'warning', Normal: 'info', Lead: '' }[level] || '');
const getIndustryLabel = (industry: string) => ({ Manufacturing: '制造业', Trading: '贸易/零售', Technology: '科技/IT', Construction: '建筑/工程', Healthcare: '医疗/健康', Education: '教育', Finance: '金融', Other: '其他' }[industry] || industry);
const getAddressTypeLabel = (type: string) => ({ Office: '办公地址', Billing: '开票地址', Shipping: '收货地址', Registered: '注册地址' }[type] || type);
const formatFullAddress = (address: CustomerAddress) => [address.country, address.province, address.city, address.district, address.streetAddress].filter(Boolean).join(' ');
const getCurrencyLabel = (currency: number) => ({ 1: 'RMB', 2: 'USD', 3: 'EUR', 4: 'JPY', 5: 'GBP', 6: 'HKD' }[currency] || 'RMB');

onMounted(() => {
  fetchCustomerDetail();
  fetchCustomerTags();
  fetchContactHistory();
  fetchLogs();
});
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.customer-detail-page {
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
  margin-bottom: 24px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 16px;
  }

  .header-right {
    display: flex;
    gap: 10px;
  }
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover { background: rgba(255,255,255,0.07); color: $text-secondary; border-color: rgba(0,212,255,0.2); }
}

.customer-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
}

.customer-avatar-lg {
  width: 48px;
  height: 48px;
  background: linear-gradient(135deg, rgba(0,102,255,0.3), rgba(0,212,255,0.2));
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
  color: $cyan-primary;
  flex-shrink: 0;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0 0 6px 0;
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
}

.title-tags-row {
  margin-top: 6px;
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn-add-tag {
  padding: 3px 8px;
  border-radius: 999px;
  border: 1px dashed rgba(0, 212, 255, 0.35);
  background: transparent;
  color: rgba(200, 216, 232, 0.85);
  font-size: 11px;
  cursor: pointer;
  transition: all 0.15s;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
  }
}

.customer-code {
  font-family: 'Space Mono', monospace;
  font-size: 11px;
  color: $text-muted;
}

.status-badge {
  font-size: 10px;
  padding: 2px 7px;
  border-radius: 3px;

  &--active     { background: rgba(70,191,145,0.15); color: $color-mint-green; border: 1px solid rgba(70,191,145,0.3); }
  &--inactive   { background: rgba(107,122,141,0.15); color: #8A9BB0; border: 1px solid rgba(107,122,141,0.3); }
  &--blacklist  { background: rgba(201,87,69,0.15); color: $color-red-brown; border: 1px solid rgba(201,87,69,0.3); }
}

.level-badge {
  display: inline-block;
  font-size: 10px;
  padding: 2px 7px;
  border-radius: 3px;

  &.level-vip       { background: rgba(201,87,69,0.2);  color: #C95745; border: 1px solid rgba(201,87,69,0.3); }
  &.level-important { background: rgba(201,154,69,0.2); color: #C99A45; border: 1px solid rgba(201,154,69,0.3); }
  &.level-normal    { background: rgba(107,122,141,0.2); color: #8A9BB0; border: 1px solid rgba(107,122,141,0.3); }
  &.level-lead      { background: rgba(70,191,145,0.15); color: #46BF91; border: 1px solid rgba(70,191,145,0.3); }
}

.btn-warning {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: rgba(201,154,69,0.15);
  border: 1px solid rgba(201,154,69,0.4);
  border-radius: $border-radius-md;
  color: $color-amber;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover { background: rgba(201,154,69,0.25); }

  &--active {
    background: rgba(107,122,141,0.15);
    border-color: rgba(107,122,141,0.3);
    color: #8A9BB0;
    &:hover { background: rgba(107,122,141,0.25); }
  }
}

.btn-danger {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: rgba(201,87,69,0.15);
  border: 1px solid rgba(201,87,69,0.4);
  border-radius: $border-radius-md;
  color: $color-red-brown;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover { background: rgba(201,87,69,0.25); }
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: linear-gradient(135deg, rgba(0,102,255,0.8), rgba(0,212,255,0.7));
  border: 1px solid rgba(0,212,255,0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover { transform: translateY(-1px); box-shadow: 0 4px 16px rgba(0,212,255,0.25); }

  &--green {
    background: linear-gradient(135deg, rgba(50,149,201,0.8), rgba(70,191,145,0.7));
    border-color: rgba(70,191,145,0.4);
    &:hover { box-shadow: 0 4px 16px rgba(70,191,145,0.25); }
  }
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: rgba(255,255,255,0.05);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover { background: rgba(255,255,255,0.08); border-color: rgba(0,212,255,0.25); }
}

// ---- 基本信息 ----
.info-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255,255,255,0.05);
  background: rgba(0,0,0,0.1);
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;

  &--cyan { background: $cyan-primary; box-shadow: 0 0 6px rgba(0,212,255,0.6); }
}

.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-primary;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
  padding: 0;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 16px 20px;
  border-bottom: 1px solid rgba(255,255,255,0.04);
  border-right: 1px solid rgba(255,255,255,0.04);

  &:nth-child(3n) { border-right: none; }

  .info-label {
    font-size: 11px;
    color: $text-muted;
    letter-spacing: 0.5px;
    text-transform: uppercase;
  }

  .info-value {
    font-size: 13px;
    color: $text-secondary;

    &--code   { font-family: 'Space Mono', monospace; font-size: 12px; color: $color-ice-blue; }
    &--amount { font-family: 'Space Mono', monospace; font-size: 13px; color: $text-primary; font-weight: 500; }
    &--time   { font-size: 12px; color: $text-muted; }
  }
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

.amount-negative { color: $color-red-brown !important; }

// ---- 标签页 ----
.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.tabs-nav {
  display: flex;
  border-bottom: 1px solid rgba(255,255,255,0.06);
  padding: 0 16px;
  background: rgba(0,0,0,0.1);
}

.tab-btn {
  padding: 12px 16px;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  margin-bottom: -1px;

  &:hover { color: $text-secondary; }

  &--active {
    color: $cyan-primary;
    border-bottom-color: $cyan-primary;
  }
}

.tabs-body { padding: 20px; }

.tab-toolbar {
  margin-bottom: 14px;
}

.btn-add-item {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 5px 12px;
  background: rgba(0,212,255,0.08);
  border: 1px solid rgba(0,212,255,0.25);
  border-radius: $border-radius-sm;
  color: $cyan-primary;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover { background: rgba(0,212,255,0.14); }
}

// ---- 表格 ----
.quantum-table {
  width: 100%;
  background: transparent !important;

  :deep(.el-table__inner-wrapper) { background: transparent; }
  :deep(tr) { background: transparent !important; &:hover td { background: rgba(0,212,255,0.04) !important; } }
  :deep(.el-table__fixed-right) { background: $layer-2 !important; }
  // 操作列按钮禁止折行
  :deep(.el-table__cell) {
    .el-button { white-space: nowrap !important; }
    .cell { white-space: nowrap; }
  }
}

.cell-primary   { color: $text-primary; font-size: 13px; }
.cell-secondary { color: $text-secondary; font-size: 13px; }
.cell-muted     { color: $text-muted; font-size: 12px; }
.cell-code      { font-family: 'Space Mono', monospace; font-size: 12px; color: $color-ice-blue; }

.default-badge {
  display: inline-block;
  font-size: 10px;
  padding: 1px 6px;
  background: rgba(70,191,145,0.15);
  color: $color-mint-green;
  border: 1px solid rgba(70,191,145,0.3);
  border-radius: 3px;
}

.action-btn {
  display: inline-flex;
  align-items: center;
  padding: 3px 8px;
  background: transparent;
  border: 1px solid rgba(0,212,255,0.2);
  border-radius: 4px;
  color: $color-ice-blue;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.15s;
  margin-right: 4px;
  white-space: nowrap;
  flex-shrink: 0;

  &:hover { background: rgba(0,212,255,0.08); border-color: rgba(0,212,255,0.4); color: $cyan-primary; }

  &--danger {
    border-color: rgba(201,87,69,0.2);
    color: $color-red-brown;
    &:hover { background: rgba(201,87,69,0.08); border-color: rgba(201,87,69,0.4); }
  }
}

// ---- inline-form ----
.inline-form {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px;
  background: rgba(0,212,255,0.04);
  border: 1px solid rgba(0,212,255,0.12);
  border-radius: $border-radius-md;
  margin-bottom: 14px;
  flex-wrap: wrap;
}

// ---- 时间线 ----
.timeline-wrapper { padding: 8px 0; }

.timeline-item {
  display: flex;
  align-items: flex-start;
  gap: 14px;
  padding: 12px 0;
  border-bottom: 1px solid rgba(255,255,255,0.04);

  &:last-child { border-bottom: none; }
}

.timeline-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  margin-top: 3px;
  flex-shrink: 0;

  &.dot--primary { background: $color-steel-cyan; box-shadow: 0 0 6px rgba(50,149,201,0.6); }
  &.dot--success { background: $color-mint-green; box-shadow: 0 0 6px rgba(70,191,145,0.6); }
  &.dot--warning { background: $color-amber; box-shadow: 0 0 6px rgba(201,154,69,0.6); }
}

.timeline-content {
  display: flex;
  flex-direction: column;
  gap: 3px;

  .timeline-text { font-size: 13px; color: $text-secondary; }
  .timeline-time { font-size: 11px; color: $text-muted; font-family: 'Space Mono', monospace; }
}

// ---- 空状态 ----
.empty-state {
  text-align: center;
  padding: 60px;
  color: $text-muted;

  svg { margin-bottom: 12px; opacity: 0.3; }
  p { font-size: 14px; margin: 0; }
}
</style>
