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
          <div class="customer-avatar-lg">{{ detailAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1
                  class="page-title"
                  :class="{
                    'page-title--muted':
                      customer && (customer.disenableStatus || customer.blackList)
                  }"
                >
                  {{ detailCustomerTitle }}
                </h1>
                <PartyStatusIcons
                  v-if="customer"
                  :entity-id="customer.id || customerId"
                  :frozen="!!customer.disenableStatus"
                  :blacklist="!!customer.blackList"
                  size="md"
                />
              </div>
              <button
                v-if="customer"
                type="button"
                class="btn-favorite-star"
                :class="{ 'is-favorite': isFavorite }"
                :disabled="favoriteLoading"
                :title="isFavorite ? '取消收藏' : '收藏'"
                :aria-label="isFavorite ? '取消收藏' : '收藏客户'"
                :aria-pressed="isFavorite"
                @click="toggleFavorite"
              >
                <!-- 未收藏：空心星 -->
                <svg
                  v-if="!isFavorite"
                  class="star-icon"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="1.75"
                  stroke-linejoin="round"
                  aria-hidden="true"
                >
                  <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
                </svg>
                <!-- 已收藏：实心星 -->
                <svg v-else class="star-icon star-icon--solid" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
                </svg>
              </button>
            </div>
            <div class="title-meta">
              <span class="customer-code">{{ detailCustomerCodeDisplay }}</span>
              <span v-if="customer?.customerLevel" class="level-badge" :class="`level-${customer.customerLevel?.toLowerCase()}`">
                {{ customerDict.levelLabel(customer.customerLevel) }}
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
        <button class="btn-primary" @click="handleEdit">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
          </svg>
          编辑
        </button>
        <el-dropdown
          v-if="customer"
          trigger="click"
          placement="bottom-end"
          popper-class="customer-detail-header-more-popper"
          @command="onHeaderMoreCommand"
        >
          <button type="button" class="btn-more-actions" title="更多操作" aria-label="更多操作">
            <span class="btn-more-actions__dots" aria-hidden="true">⋯</span>
          </button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item v-if="!customer.blackList" command="blacklist">加入黑名单</el-dropdown-item>
              <el-dropdown-item v-else command="unblacklist">解除黑名单</el-dropdown-item>
              <el-dropdown-item v-if="!customer.disenableStatus" command="freeze">冻结</el-dropdown-item>
              <el-dropdown-item v-else command="unfreeze">启用</el-dropdown-item>
              <el-dropdown-item command="delete" divided class="detail-more-item--danger">删除客户</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
        <button v-if="canCreateRfqFromCustomer" class="btn-success" type="button" @click="handleCreateRfq">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
            <polyline points="14 2 14 8 20 8"/>
            <line x1="12" y1="11" x2="12" y2="17"/>
            <line x1="9" y1="14" x2="15" y2="14"/>
          </svg>
          创建需求
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
              <span
                class="info-value"
                :class="{
                  'info-value--party-muted':
                    customer.disenableStatus || customer.blackList
                }"
              >{{ detailCustomerNameDisplay }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">客户类型</span>
              <span class="info-value">{{ customerDict.typeLabel(customer.customerType ?? 0) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">统一社会信用代码</span>
              <span class="info-value info-value--code">{{ customer.unifiedSocialCreditCode || '--' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">行业</span>
              <span class="info-value">{{ customerDict.industryLabel(customer.industry) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">地区</span>
              <span class="info-value">{{ customer.region || '--' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">所属业务员</span>
              <span class="info-value">{{ maskSaleSensitiveFields ? '—' : customer.salesPersonName || '--' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">信用额度</span>
              <span class="info-value">
                <template v-if="maskSaleSensitiveFields">—</template>
                <template v-else-if="customerDisplayCreditLimit !== null">
                  <span class="amount-with-code">
                    <span>{{ formatTotalAmountNumber(customerDisplayCreditLimit) }}</span>
                    <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(customerDisplayCurrency)]">
                      {{ listAmountCurrencyIso(customerDisplayCurrency) }}
                    </span>
                  </span>
                </template>
                <template v-else>—</template>
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">账期(天)</span>
              <span class="info-value">{{ customer.paymentTerms || 0 }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">账户余额</span>
              <span
                class="info-value"
                :class="{
                  'amount-negative':
                    !maskSaleSensitiveFields && customerDisplayBalance !== null && customerDisplayBalance < 0
                }"
              >
                <template v-if="maskSaleSensitiveFields">—</template>
                <template v-else-if="customerDisplayBalance !== null">
                  <span class="amount-with-code">
                    <span>{{ formatTotalAmountNumber(customerDisplayBalance) }}</span>
                    <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(customerDisplayCurrency)]">
                      {{ listAmountCurrencyIso(customerDisplayCurrency) }}
                    </span>
                  </span>
                </template>
                <template v-else>—</template>
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
                <el-table-column prop="contactName" label="姓名" min-width="140" show-overflow-tooltip>
                  <template #default="{ row }"><span class="cell-primary">{{ maskSaleSensitiveFields ? '—' : row.contactName || '--' }}</span></template>
                </el-table-column>
                <el-table-column prop="gender" label="性别" width="72">
                  <template #default="{ row }">
                    <span class="cell-muted">{{ formatCustomerContactGenderLabel(row.gender) }}</span>
                  </template>
                </el-table-column>
                <el-table-column prop="department" label="部门" width="120">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.department || '--' }}</span></template>
                </el-table-column>
                <el-table-column prop="position" label="职位" width="120">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.position || row.title || '--' }}</span></template>
                </el-table-column>
                <el-table-column prop="mobilePhone" label="手机" min-width="130">
                  <template #default="{ row }"><span class="cell-code">{{ maskSaleSensitiveFields ? '—' : row.mobilePhone || '--' }}</span></template>
                </el-table-column>
                <el-table-column prop="email" label="邮箱" min-width="180" show-overflow-tooltip>
                  <template #default="{ row }"><span class="cell-secondary">{{ maskSaleSensitiveFields ? '—' : row.email || '--' }}</span></template>
                </el-table-column>
                <el-table-column label="默认" width="80" align="center">
                  <template #default="{ row }">
                    <span v-if="parseApiBoolean(row.isDefault)" class="default-badge">默认</span>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="200" fixed="right" class-name="op-col" label-class-name="op-col">
                  <template #default="{ row }">
                    <div @click.stop @dblclick.stop>
                      <div class="action-btns">
                        <button type="button" class="action-btn action-btn--primary" @click.stop="goEditContact(row)">编辑</button>
                        <button type="button" class="action-btn action-btn--danger" @click.stop="deleteContact(row)">删除</button>
                      </div>
                    </div>
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
                    <span class="type-badge type-0">{{ formatCustomerAddressTypeLabel(row.addressType) }}</span>
                  </template>
                </el-table-column>
                <el-table-column prop="contactPerson" label="联系人" width="100">
                  <template #default="{ row }"><span class="cell-primary">{{ maskSaleSensitiveFields ? '—' : row.contactPerson || '--' }}</span></template>
                </el-table-column>
                <el-table-column prop="contactPhone" label="联系电话" width="140">
                  <template #default="{ row }"><span class="cell-code">{{ maskSaleSensitiveFields ? '—' : row.contactPhone || '--' }}</span></template>
                </el-table-column>
                <el-table-column label="详细地址" min-width="250" show-overflow-tooltip>
                  <template #default="{ row }"><span class="cell-secondary">{{ formatFullAddress(row) }}</span></template>
                </el-table-column>
                <el-table-column label="默认" width="80" align="center">
                  <template #default="{ row }">
                    <span v-if="parseApiBoolean(row.isDefault)" class="default-badge">默认</span>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="200" fixed="right" class-name="op-col" label-class-name="op-col">
                  <template #default="{ row }">
                    <div @click.stop @dblclick.stop>
                      <div class="action-btns">
                        <button type="button" class="action-btn action-btn--primary" @click.stop="editAddress(row)">编辑</button>
                        <button type="button" class="action-btn action-btn--danger" @click.stop="deleteAddress(row)">删除</button>
                      </div>
                    </div>
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
                <el-table-column label="默认" width="80" align="center">
                  <template #default="{ row }">
                    <span v-if="parseApiBoolean(row.isDefault)" class="default-badge">默认</span>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="200" fixed="right" class-name="op-col" label-class-name="op-col">
                  <template #default="{ row }">
                    <div @click.stop @dblclick.stop>
                      <div class="action-btns">
                        <button type="button" class="action-btn action-btn--primary" @click.stop="editBank(row)">编辑</button>
                        <button type="button" class="action-btn action-btn--danger" @click.stop="deleteBank(row)">删除</button>
                      </div>
                    </div>
                  </template>
                </el-table-column>
              </CrmDataTable>
            </div>

            <!-- 文档 -->
            <div v-show="activeTab === 'documents'" class="documents-tab">
              <DocumentUploadPanel
                bizType="Customer"
                :bizId="canonicalCustomerId"
                @uploaded="documentListRef?.refresh?.()"
              />
              <DocumentListPanel
                ref="documentListRef"
                bizType="Customer"
                :bizId="canonicalCustomerId"
                view-mode="grid"
              />
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
                      <button class="action-btn action-btn--danger" style="margin-right:4px" @click="deleteHistory(h)">删除</button>
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
                    <span class="timeline-text">
                      <template v-if="log.bizType && log.bizType !== 'Customer'">
                        <span class="log-biz-pill">{{ operationBizTypeLabel(log.bizType) }}</span>
                        ·
                      </template>
                      {{ log.operationType }} · {{ log.operationDesc || log.description || '—' }}
                      <template v-if="log.remark"> · 原因：{{ log.remark }}</template>
                      <template v-if="log.recordCode"> · 单号 {{ log.recordCode }}</template>
                    </span>
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
                    <span class="timeline-text">
                      <template v-if="log.bizType && log.bizType !== 'Customer'">
                        <span class="log-biz-pill">{{ operationBizTypeLabel(log.bizType) }}</span>
                        ·
                      </template>
                      {{ log.fieldLabel || log.fieldName }}：{{ log.oldValue || '(空)' }} → {{ log.newValue || '(空)' }}
                      <template v-if="log.recordCode"> · 单号 {{ log.recordCode }}</template>
                    </span>
                    <span class="timeline-time">{{ log.changedByUserName || log.operatorUserName || '系统' }} · {{ formatDateTime(log.changedAt || log.operationTime || log.createTime) }}</span>
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
    <!-- 冻结 / 启用客户 -->
    <el-dialog
      v-model="showFreezeDialog"
      :title="freezeDialogTitle"
      width="440px"
      :close-on-click-modal="false"
      @closed="freezeReason = ''"
    >
      <div style="color:rgba(200,216,232,0.75);font-size:13px;margin-bottom:16px">
        {{ freezeDialogHint }}
      </div>
      <el-form label-width="90px">
        <el-form-item :label="freezeReasonLabel" required>
          <el-input
            v-model="freezeReason"
            type="textarea"
            :rows="3"
            :placeholder="freezeReasonPlaceholder"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showFreezeDialog = false">取消</el-button>
        <el-button
          :type="freezeMode === 'freeze' ? 'warning' : 'primary'"
          :loading="actionLoading"
          @click="handleConfirmFreezeOrUnfreeze"
        >
          确定
        </el-button>
      </template>
    </el-dialog>

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

    <el-dialog v-model="showRemoveBlacklistDialog" title="解除黑名单" width="440px" :close-on-click-modal="false">
      <div style="color:rgba(200,216,232,0.75);font-size:13px;margin-bottom:16px">
        解除后该客户将不再处于黑名单状态，原因将记入操作日志。
      </div>
      <el-form label-width="90px">
        <el-form-item label="移出原因" required>
          <el-input
            v-model="removeFromBlacklistReason"
            type="textarea"
            :rows="3"
            placeholder="请输入移出黑名单原因（必填）"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showRemoveBlacklistDialog = false">取消</el-button>
        <el-button type="primary" :loading="actionLoading" @click="handleConfirmRemoveBlacklist">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@/stores/auth';
import { ElNotification, ElMessageBox } from 'element-plus';
import {
  customerApi,
  customerContactApi,
  customerAddressApi,
  customerBankApi,
  normalizeCustomerAddressFromApi,
  formatCustomerAddressTypeLabel,
} from '@/api/customer';
import { favoriteApi } from '@/api/favorite';
import { tagApi, type TagDefinitionDto } from '@/api/tag';
import TagListDisplay from '@/components/Tag/TagListDisplay.vue';
import ApplyTagsDialog from '@/components/Tag/ApplyTagsDialog.vue';
import PartyStatusIcons from '@/components/party/PartyStatusIcons.vue';
import type { Customer, CustomerContactInfo, CustomerAddress, CustomerBankInfo } from '@/types/customer';
import { useCustomerDictStore } from '@/stores/customerDict';

import AddressDialog from './components/AddressDialog.vue';
import BankDialog from './components/BankDialog.vue';
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue';
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue';
import { formatDisplayDateTime } from '@/utils/displayDateTime';
import { parseApiBoolean } from '@/utils/parseApiBoolean';
import { operationBizTypeLabel } from '@/utils/businessLogLabels';
import { CUSTOMER_FAVORITES_CHANGED_EVENT } from '@/constants/customerFavorites';
import { logRecentApi } from '@/api/logRecent';
import { CUSTOMER_RECENT_HISTORY_CHANGED_EVENT } from '@/constants/customerRecentHistory';
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency';
import { isDistrictPlaceholder } from '@/constants/region';
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask';
import { formatTotalAmountNumber, listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat';

const route = useRoute();
const router = useRouter();
const { t } = useI18n();
const authStore = useAuthStore();
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask();
const canCreateRfqFromCustomer = computed(() => authStore.hasPermission('rfq.create'));
const customerDict = useCustomerDictStore();
const customerId = route.params.id as string;

/** 客户联系人性别：0=保密、1=男、2=女 */
function formatCustomerContactGenderLabel(v: unknown): string {
  const g = v === null || v === undefined || v === '' ? NaN : Number(v);
  if (g === 0) return '保密';
  if (g === 1) return '男';
  if (g === 2) return '女';
  return '未知';
}

/** 详情加载后主键（与路由业务编号区分，文档等用主键更稳） */
const canonicalCustomerId = computed(() => customer.value?.id ?? (route.params.id as string));
const loading = ref(false);
const customer = ref<Customer | null>(null);

const detailCustomerTitle = computed(() =>
  maskSaleSensitiveFields.value ? '客户详情' : customer.value?.customerName?.trim() || '客户详情'
);
const detailCustomerNameDisplay = computed(() =>
  maskSaleSensitiveFields.value ? '—' : customer.value?.customerName?.trim() || '—'
);
const detailCustomerCodeDisplay = computed(() =>
  maskSaleSensitiveFields.value ? '—' : customer.value?.customerCode?.trim() || '—'
);
const detailAvatarChar = computed(() => {
  if (maskSaleSensitiveFields.value) return '?';
  const n = customer.value?.customerName?.trim();
  return (n && n[0]) || '?';
});

const customerTags = ref<TagDefinitionDto[]>([]);
const activeTab = ref('contacts');
const showAddressDialog = ref(false);
const showBankDialog = ref(false);
const editingAddress = ref<CustomerAddress | undefined>(undefined);
const editingBank = ref<CustomerBankInfo | undefined>(undefined);

const tabs = [
  { key: 'contacts', label: '联系人' },
  { key: 'addresses', label: '地址信息' },
  { key: 'banks', label: '银行信息' },
  { key: 'documents', label: '文档' },
  { key: 'contactHistory', label: '联系历史' },
  { key: 'logs', label: '操作日志' }
];

const documentListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null);

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
const showRemoveBlacklistDialog = ref(false);
const showFreezeDialog = ref(false);
/** freeze = 冻结客户；unfreeze = 启用（解除冻结） */
const freezeMode = ref<'freeze' | 'unfreeze'>('freeze');
const freezeReason = ref('');
const deleteReason = ref('');
const blacklistReason = ref('');
const removeFromBlacklistReason = ref('');

const freezeDialogTitle = computed(() => (freezeMode.value === 'freeze' ? '冻结客户' : '启用客户'));
const freezeDialogHint = computed(() =>
  freezeMode.value === 'freeze'
    ? '冻结后客户将标记为冻结状态，是否确认冻结？'
    : '启用后客户将解除冻结状态，是否确认启用？'
);
const freezeReasonLabel = computed(() => (freezeMode.value === 'freeze' ? '冻结原因' : '启用原因'));
const freezeReasonPlaceholder = computed(() =>
  freezeMode.value === 'freeze' ? '请输入冻结原因（必填）' : '请输入启用原因（必填）'
);
const actionLoading = ref(false);
const showTagDialog = ref(false);
const isFavorite = ref(false);
const favoriteLoading = ref(false);

const tableHeaderStyle = () => ({ background: '#0A1628', color: 'rgba(200,216,232,0.55)', fontSize: '12px', fontWeight: '500', letterSpacing: '0.5px', borderBottom: '1px solid rgba(0,212,255,0.12)', padding: '10px 0' });
const tableCellStyle = () => ({ background: 'transparent', borderBottom: '1px solid rgba(255,255,255,0.05)', color: 'rgba(224,244,255,0.85)', fontSize: '13px' });
const tableRowStyle = () => ({ background: 'transparent' });

const refreshFavoriteStatus = async () => {
  try {
    isFavorite.value = await favoriteApi.checkFavorite('CUSTOMER', customerId);
  } catch {
    isFavorite.value = false;
  }
};

function trackRecentDetail() {
  const c = customer.value;
  if (!c?.id) return;
  logRecentApi
    .record({
      bizType: 'Customer',
      recordId: String(c.id),
      recordCode: c.customerCode || undefined,
      openKind: 'detail'
    })
    .then(() => window.dispatchEvent(new CustomEvent(CUSTOMER_RECENT_HISTORY_CHANGED_EVENT)))
    .catch(() => {});
}

const fetchCustomerDetail = async () => {
  loading.value = true;
  try {
    const c = await customerApi.getCustomerById(customerId);
    if (c.addresses?.length) {
      c.addresses = c.addresses.map((a) => normalizeCustomerAddressFromApi(a));
    }
    customer.value = c;
    await refreshFavoriteStatus();
    trackRecentDetail();
    void customerDict.hydrateCustomerEditForm({
      customerType: c.customerType,
      customerLevel: c.customerLevel,
      industry: c.industry,
      taxRate: c.taxRate,
      invoiceType: c.invoiceType
    });
  } catch {
    ElNotification.error({ title: '加载失败', message: '获取客户详情失败，请刷新重试' });
  } finally {
    loading.value = false;
  }
};

const toggleFavorite = async () => {
  if (!customer.value?.id || favoriteLoading.value) return;
  favoriteLoading.value = true;
  try {
    if (isFavorite.value) {
      await favoriteApi.removeFavorite('CUSTOMER', customerId);
      isFavorite.value = false;
      ElNotification.success({ title: '已取消收藏', message: customer.value.customerName || '' });
    } else {
      await favoriteApi.addFavorite({ entityType: 'CUSTOMER', entityId: customerId });
      isFavorite.value = true;
      ElNotification.success({ title: '收藏成功', message: customer.value.customerName || '' });
    }
    window.dispatchEvent(new CustomEvent(CUSTOMER_FAVORITES_CHANGED_EVENT));
  } catch (error: any) {
    ElNotification.error({ title: '操作失败', message: error?.message || '收藏操作失败，请稍后重试' });
  } finally {
    favoriteLoading.value = false;
  }
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
    router.push({ name: 'CustomerList' });
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
    await fetchCustomerDetail();
    await fetchLogs();
  } catch { ElNotification.error({ title: '操作失败', message: '加入黑名单失败，请稍后重试' }); }
  finally { actionLoading.value = false; }
};

const handleConfirmRemoveBlacklist = async () => {
  if (!removeFromBlacklistReason.value.trim()) {
    ElNotification.warning({ title: '请填写原因', message: '请输入移出黑名单原因' });
    return;
  }
  actionLoading.value = true;
  try {
    await customerApi.removeFromBlacklist(customerId, removeFromBlacklistReason.value.trim());
    ElNotification.success({ title: '操作成功', message: '已解除黑名单状态' });
    showRemoveBlacklistDialog.value = false;
    removeFromBlacklistReason.value = '';
    await fetchCustomerDetail();
    await fetchLogs();
  } catch {
    ElNotification.error({ title: '操作失败', message: '解除黑名单失败，请稍后重试' });
  } finally {
    actionLoading.value = false;
  }
};

const goBack = () => router.push({ name: 'CustomerList' });
const handleEdit = () => router.push(`/customers/${customerId}/edit`);

function onHeaderMoreCommand(command: string) {
  if (command === 'blacklist') showBlacklistDialog.value = true;
  else if (command === 'unblacklist') {
    removeFromBlacklistReason.value = '';
    showRemoveBlacklistDialog.value = true;
  }
  else if (command === 'freeze') {
    freezeMode.value = 'freeze';
    freezeReason.value = '';
    showFreezeDialog.value = true;
  } else if (command === 'unfreeze') {
    freezeMode.value = 'unfreeze';
    freezeReason.value = '';
    showFreezeDialog.value = true;
  } else if (command === 'delete') showDeleteDialog.value = true;
}

const handleConfirmFreezeOrUnfreeze = async () => {
  if (!freezeReason.value.trim()) {
    ElNotification.warning({
      title: '请填写原因',
      message: freezeMode.value === 'freeze' ? '请输入冻结原因' : '请输入启用原因'
    });
    return;
  }
  actionLoading.value = true;
  try {
    if (freezeMode.value === 'freeze') {
      await customerApi.freezeCustomer(customerId, freezeReason.value.trim());
      ElNotification.success({ title: '操作成功', message: '客户已冻结' });
    } else {
      await customerApi.unfreezeCustomer(customerId, freezeReason.value.trim());
      ElNotification.success({ title: '操作成功', message: '客户已启用' });
    }
    showFreezeDialog.value = false;
    freezeReason.value = '';
    await fetchCustomerDetail();
    await fetchLogs();
  } catch (e: any) {
    ElNotification.error({ title: '操作失败', message: e?.message || '请求失败，请稍后重试' });
  } finally {
    actionLoading.value = false;
  }
};

/** 跳转新建需求并带上当前客户，由 RFQCreate 根据 query 预填「客户」 */
const handleCreateRfq = () => {
  if (!authStore.hasPermission('rfq.create')) {
    ElNotification.warning({ title: '无权限', message: t('rfqHome.createNeedRfqCreate') });
    return;
  }
  if (!customer.value?.id) {
    ElNotification.warning({ title: '无法创建', message: '客户信息未加载完成' });
    return;
  }
  router.push({ name: 'RFQCreate', query: { customerId: customer.value.id } });
};

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

/** 详情接口字段与列表归一化一致：可能为 creditLine / creditLineRemain / tradeCurrency */
type CustomerMoneyApi = Customer & {
  creditLine?: unknown
  CreditLine?: unknown
  creditLineRemain?: unknown
  CreditLineRemain?: unknown
  tradeCurrency?: unknown
  TradeCurrency?: unknown
}

function resolveCustomerCreditLimit(c: Customer | null | undefined): number | null {
  if (!c) return null
  const r = c as CustomerMoneyApi
  const v = c.creditLimit ?? r.creditLine ?? r.CreditLine
  if (v === undefined || v === null || v === '') return null
  const n = Number(v)
  return Number.isFinite(n) ? n : null
}

function resolveCustomerBalance(c: Customer | null | undefined): number | null {
  if (!c) return null
  const r = c as CustomerMoneyApi
  const v = c.balance ?? r.creditLineRemain ?? r.CreditLineRemain
  if (v === undefined || v === null || v === '') return null
  const n = Number(v)
  return Number.isFinite(n) ? n : null
}

function resolveCustomerSettlementCurrency(c: Customer | null | undefined): number {
  if (!c) return 1
  const r = c as CustomerMoneyApi
  const v = c.currency ?? r.tradeCurrency ?? r.TradeCurrency
  if (v === undefined || v === null || v === '') return 1
  const n = Number(v)
  return Number.isFinite(n) ? n : 1
}

const customerDisplayCreditLimit = computed(() => resolveCustomerCreditLimit(customer.value))
const customerDisplayBalance = computed(() => resolveCustomerBalance(customer.value))
const customerDisplayCurrency = computed(() => resolveCustomerSettlementCurrency(customer.value))

const formatDateTime = (date: string | undefined) => (date ? formatDisplayDateTime(date) : '--');
const formatFullAddress = (address: CustomerAddress) =>
  [
    address.country,
    address.province,
    address.city,
    isDistrictPlaceholder(address.district) ? '' : address.district,
    address.streetAddress
  ]
    .filter(Boolean)
    .join(' ');
const getCurrencyLabel = (currency: number) => CURRENCY_CODE_TO_TEXT[currency] || 'RMB';

onMounted(() => {
  void customerDict.ensureLoaded();
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

.page-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 6px;
}

.page-title-with-icons {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  min-width: 0;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0;

  &--muted {
    color: rgba(150, 170, 195, 0.82);
  }
}

.info-value--party-muted {
  color: rgba(150, 170, 195, 0.82);
}

.btn-favorite-star {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 4px;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: rgba(200, 220, 240, 0.5);
  cursor: pointer;
  transition: color 0.15s, background 0.15s, transform 0.12s;

  .star-icon {
    width: 22px;
    height: 22px;
    display: block;
  }

  &:hover:not(:disabled) {
    color: #00d4ff;
    background: rgba(0, 212, 255, 0.1);
  }

  &:active:not(:disabled) {
    transform: scale(0.92);
  }

  &.is-favorite {
    color: #ffc94d;
  }

  &.is-favorite:hover:not(:disabled) {
    color: #ffd666;
    background: rgba(255, 201, 77, 0.12);
  }

  &:disabled {
    opacity: 0.45;
    cursor: not-allowed;
  }
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
}

// 新建/新增/创建（含「创建需求」入口）（UI 规范：success 绿）
.btn-success {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: linear-gradient(135deg, rgba(46, 160, 67, 0.88), rgba(70, 191, 145, 0.78));
  border: 1px solid rgba(70, 191, 145, 0.45);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(70, 191, 145, 0.3);
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

.btn-more-actions {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 36px;
  height: 36px;
  padding: 0 10px;
  box-sizing: border-box;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  cursor: pointer;
  transition: all 0.2s;
  font-family: 'Noto Sans SC', sans-serif;

  &:hover {
    background: rgba(255, 255, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.25);
    color: $text-primary;
  }

  &__dots {
    font-size: 18px;
    line-height: 1;
    letter-spacing: 0.5px;
    transform: translateY(-1px);
    font-weight: 700;
  }
}

// ---- 基本信息 ----
.info-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.amount-with-code {
  display: inline-flex;
  align-items: baseline;
  gap: 4px;
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
  background: rgba(70, 191, 145, 0.12);
  border: 1px solid rgba(70, 191, 145, 0.35);
  border-radius: $border-radius-sm;
  color: $color-mint-green;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(70, 191, 145, 0.2);
    border-color: rgba(70, 191, 145, 0.5);
  }
}

// ---- 表格 ----
.quantum-table {
  width: 100%;
  background: transparent !important;

  :deep(.el-table__inner-wrapper) { background: transparent; }
  :deep(tr) { background: transparent !important; &:hover td { background: rgba(0,212,255,0.04) !important; } }
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
  border: 1px solid $border-panel;
  border-radius: 4px;
  color: $text-muted;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.15s;
  margin-right: 4px;
  white-space: nowrap;
  flex-shrink: 0;

  &:hover {
    border-color: rgba(0, 212, 255, 0.25);
    color: $text-secondary;
  }

  &--primary {
    border: 1px solid rgba(0, 212, 255, 0.35);
    color: $color-ice-blue;
    &:hover {
      background: rgba(0, 102, 255, 0.12);
      border-color: rgba(0, 212, 255, 0.5);
      color: $cyan-primary;
    }
  }

  &--danger {
    border: 1px solid rgba(201, 87, 69, 0.25);
    color: $color-red-brown;
    &:hover { background: rgba(201, 87, 69, 0.1); border-color: rgba(201, 87, 69, 0.45); }
  }
}

.documents-tab {
  display: flex;
  flex-direction: column;
  gap: 20px;
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

.log-biz-pill {
  display: inline-block;
  padding: 0 6px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 600;
  color: rgba(165, 243, 252, 0.95);
  background: rgba(34, 211, 238, 0.12);
  border: 1px solid rgba(34, 211, 238, 0.25);
  vertical-align: middle;
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

<!-- 下拉 Teleport 到 body，需非 scoped -->
<style lang="scss">
@import '@/assets/styles/variables.scss';

.customer-detail-header-more-popper.el-dropdown__popper,
.customer-detail-header-more-popper.el-popper {
  background: $layer-2 !important;
  border: 1px solid rgba(0, 212, 255, 0.15) !important;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.45) !important;
}

.customer-detail-header-more-popper .el-dropdown-menu {
  background: transparent !important;
  border: none !important;
  box-shadow: none !important;
  padding: 4px 0 !important;
}

.customer-detail-header-more-popper .el-dropdown-menu__item {
  color: rgba(200, 220, 240, 0.92) !important;
  font-size: 13px;

  &:hover,
  &:focus {
    background: rgba(0, 212, 255, 0.1) !important;
    color: #e8f4ff !important;
  }
}

.customer-detail-header-more-popper .detail-more-item--danger {
  color: $color-red-brown !important;

  &:hover,
  &:focus {
    color: #e8a090 !important;
    background: rgba(201, 87, 69, 0.12) !important;
  }
}
</style>
