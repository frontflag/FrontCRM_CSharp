<template>
  <div class="customer-detail-page">
    <!-- 页面头部（布局与样式与客户详情一致） -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          返回
        </button>
        <div class="customer-title-group">
          <div class="customer-avatar-lg">{{ (vendor?.officialName || '?')[0] }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1
                  class="page-title"
                  :class="{ 'page-title--muted': vendorPartyMuted }"
                >
                  {{ vendor?.officialName || '供应商详情' }}
                </h1>
                <PartyStatusIcons
                  v-if="vendor"
                  :entity-id="vendor.id"
                  :frozen="!!vendor.isDisenable"
                  :blacklist="!!vendor.blackList"
                  size="md"
                />
              </div>
              <button
                v-if="vendor"
                type="button"
                class="btn-favorite-star"
                :class="{ 'is-favorite': isFavorite }"
                :disabled="favoriteLoading"
                :title="isFavorite ? '取消收藏' : '收藏'"
                :aria-label="isFavorite ? '取消收藏' : '收藏供应商'"
                :aria-pressed="isFavorite"
                @click="toggleFavorite"
              >
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
                <svg v-else class="star-icon star-icon--solid" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
                </svg>
              </button>
            </div>
            <div class="title-meta">
              <span class="customer-code">{{ vendor?.code }}</span>
              <span class="status-badge" :class="vendorStatusClass">{{ vendorStatusText }}</span>
              <span v-if="vendor?.isDisenable" class="status-badge status--frozen">冻结</span>
              <span v-if="vendorLevelDisplay !== '--'" class="level-badge">{{ vendorLevelDisplay }}</span>
              <span v-if="vendorIdentityDisplay !== '--'" class="level-badge level-badge--credit">{{ vendorIdentityDisplay }}</span>
              <span v-if="vendor?.blackList" class="status-badge status--blacklist">黑名单</span>
            </div>
            <div class="title-tags-row">
              <TagListDisplay :tags="vendorTags" />
              <button class="btn-add-tag" @click="showTagDialog = true">添加标签</button>
            </div>
          </div>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-primary" @click="handleEdit">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
          </svg>
          编辑
        </button>
        <el-dropdown
          v-if="vendor"
          trigger="click"
          placement="bottom-end"
          popper-class="vendor-detail-header-more-popper"
          @command="onHeaderMoreCommand"
        >
          <button type="button" class="btn-more-actions" title="更多操作" aria-label="更多操作">
            <span class="btn-more-actions__dots" aria-hidden="true">⋯</span>
          </button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item v-if="!vendor.blackList" command="blacklist">加入黑名单</el-dropdown-item>
              <el-dropdown-item v-else command="unblacklist">解除黑名单</el-dropdown-item>
              <el-dropdown-item v-if="!vendor.isDisenable" command="freeze">冻结</el-dropdown-item>
              <el-dropdown-item v-else command="unfreeze">启用</el-dropdown-item>
              <el-dropdown-item command="delete" divided class="detail-more-item--danger">删除供应商</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
        <button class="btn-success" v-if="vendor?.status !== 1" type="button" @click="handleActivate">
          启用
        </button>
      </div>
    </div>

    <div v-loading="loading" element-loading-background="rgba(10,22,40,0.8)" class="detail-content">
      <template v-if="vendor">
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
            <span
              class="info-value"
              :class="{
                'info-value--party-muted': vendor.isDisenable || vendor.blackList
              }"
            >{{ vendor.officialName }}</span>
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
            <span class="info-label">等级</span>
            <span class="info-value">{{ vendorLevelDisplay }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">身份</span>
            <span class="info-value">{{ vendorIdentityDisplay }}</span>
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
          <div v-show="activeTab === 'contacts'">
            <div class="tab-toolbar">
              <button class="btn-add-item" @click="goCreateContact">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <line x1="12" y1="5" x2="12" y2="19" />
                  <line x1="5" y1="12" x2="19" y2="12" />
                </svg>
                添加联系人
              </button>
            </div>
            <CrmDataTable
              :data="contacts"
              class="quantum-table"
              :header-cell-style="tableHeaderStyle"
              :cell-style="tableCellStyle"
              :row-style="tableRowStyle"
            >
              <el-table-column prop="cName" label="姓名" min-width="120">
                <template #default="{ row }"><span class="cell-primary">{{ row.cName || '--' }}</span></template>
              </el-table-column>
              <el-table-column prop="title" label="职位" min-width="120">
                <template #default="{ row }"><span class="cell-secondary">{{ row.title || '--' }}</span></template>
              </el-table-column>
              <el-table-column prop="department" label="部门" min-width="120">
                <template #default="{ row }"><span class="cell-secondary">{{ row.department || '--' }}</span></template>
              </el-table-column>
              <el-table-column prop="mobile" label="手机" min-width="130">
                <template #default="{ row }"><span class="cell-code">{{ row.mobile || '--' }}</span></template>
              </el-table-column>
              <el-table-column prop="email" label="邮箱" min-width="180" show-overflow-tooltip>
                <template #default="{ row }"><span class="cell-secondary">{{ row.email || '--' }}</span></template>
              </el-table-column>
              <el-table-column label="主联系人" width="90" align="center">
                <template #default="{ row }">
                  <span v-if="row.isMain" class="default-badge">主</span>
                  <span v-else class="cell-muted">--</span>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="200" fixed="right" class-name="op-col" label-class-name="op-col">
                <template #default="{ row }">
                  <div @click.stop @dblclick.stop>
                    <div class="action-btns">
                      <button type="button" class="action-btn action-btn--primary" @click.stop="goEditContact(row)">编辑</button>
                      <button type="button" class="action-btn action-btn--danger" @click.stop="handleDeleteContact(row)">删除</button>
                      <button
                        v-if="!row.isMain"
                        type="button"
                        class="action-btn action-btn--info"
                        @click.stop="handleSetMainContact(row)"
                      >
                        设为主联系人
                      </button>
                    </div>
                  </div>
                </template>
              </el-table-column>
            </CrmDataTable>
          </div>
          <div v-show="activeTab === 'addresses'">
            <div class="tab-toolbar">
              <button class="btn-add-item" @click="openCreateAddress">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <line x1="12" y1="5" x2="12" y2="19" />
                  <line x1="5" y1="12" x2="19" y2="12" />
                </svg>
                添加地址
              </button>
            </div>
            <CrmDataTable
              :data="addresses"
              class="quantum-table"
              :header-cell-style="tableHeaderStyle"
              :cell-style="tableCellStyle"
              :row-style="tableRowStyle"
            >
              <el-table-column prop="addressType" label="类型" width="100">
                <template #default="{ row }">
                  <span class="cell-secondary">{{ getAddressTypeLabel(row.addressType) }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="contactName" label="联系人" width="120">
                <template #default="{ row }"><span class="cell-primary">{{ row.contactName || '--' }}</span></template>
              </el-table-column>
              <el-table-column prop="contactPhone" label="联系电话" width="140">
                <template #default="{ row }"><span class="cell-code">{{ row.contactPhone || '--' }}</span></template>
              </el-table-column>
              <el-table-column label="详细地址" min-width="260" show-overflow-tooltip>
                <template #default="{ row }">
                  <span class="cell-secondary">{{ formatFullAddress(row) }}</span>
                </template>
              </el-table-column>
              <el-table-column label="默认" width="80" align="center">
                <template #default="{ row }">
                  <span v-if="row.isDefault" class="default-badge">默认</span>
                  <span v-else class="cell-muted">--</span>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="220" fixed="right" class-name="op-col" label-class-name="op-col">
                <template #default="{ row }">
                  <div @click.stop @dblclick.stop>
                    <div class="action-btns">
                      <button type="button" class="action-btn action-btn--primary" @click.stop="openEditAddress(row)">编辑</button>
                      <button type="button" class="action-btn action-btn--danger" @click.stop="handleDeleteAddress(row)">删除</button>
                      <button
                        v-if="!row.isDefault"
                        type="button"
                        class="action-btn action-btn--info"
                        @click.stop="handleSetDefaultAddress(row)"
                      >
                        设为默认
                      </button>
                    </div>
                  </div>
                </template>
              </el-table-column>
            </CrmDataTable>
          </div>
          <div v-show="activeTab === 'banks'">
            <div class="tab-toolbar">
              <button class="btn-add-item" @click="openCreateBank">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <line x1="12" y1="5" x2="12" y2="19" />
                  <line x1="5" y1="12" x2="19" y2="12" />
                </svg>
                添加银行信息
              </button>
            </div>
            <CrmDataTable
              :data="banks"
              class="quantum-table"
              :header-cell-style="tableHeaderStyle"
              :cell-style="tableCellStyle"
              :row-style="tableRowStyle"
            >
              <el-table-column prop="accountName" label="账户名称" min-width="160" show-overflow-tooltip>
                <template #default="{ row }"><span class="cell-primary">{{ row.accountName || '--' }}</span></template>
              </el-table-column>
              <el-table-column prop="bankName" label="开户银行" min-width="160" show-overflow-tooltip>
                <template #default="{ row }"><span class="cell-secondary">{{ row.bankName || '--' }}</span></template>
              </el-table-column>
              <el-table-column prop="bankBranch" label="开户支行" min-width="160" show-overflow-tooltip>
                <template #default="{ row }"><span class="cell-secondary">{{ row.bankBranch || '--' }}</span></template>
              </el-table-column>
              <el-table-column prop="bankAccount" label="银行账号" min-width="180" show-overflow-tooltip>
                <template #default="{ row }"><span class="cell-code">{{ row.bankAccount || '--' }}</span></template>
              </el-table-column>
              <el-table-column label="默认" width="80" align="center">
                <template #default="{ row }">
                  <span v-if="row.isDefault" class="default-badge">默认</span>
                  <span v-else class="cell-muted">--</span>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="220" fixed="right" class-name="op-col" label-class-name="op-col">
                <template #default="{ row }">
                  <div @click.stop @dblclick.stop>
                    <div class="action-btns">
                      <button type="button" class="action-btn action-btn--primary" @click.stop="openEditBank(row)">编辑</button>
                      <button type="button" class="action-btn action-btn--danger" @click.stop="handleDeleteBank(row)">删除</button>
                      <button
                        v-if="!row.isDefault"
                        type="button"
                        class="action-btn action-btn--info"
                        @click.stop="handleSetDefaultBank(row)"
                      >
                        设为默认
                      </button>
                    </div>
                  </div>
                </template>
              </el-table-column>
            </CrmDataTable>
          </div>
          <div v-show="activeTab === 'history'" class="history-tab">
            <div class="tab-toolbar">
              <button class="btn-add-item" @click="showHistoryForm = true">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <line x1="12" y1="5" x2="12" y2="19" />
                  <line x1="5" y1="12" x2="19" y2="12" />
                </svg>
                添加记录
              </button>
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
              <button class="btn-add-item" @click="submitHistory">保存</button>
              <button class="action-btn" @click="cancelHistoryForm">取消</button>
            </div>
            <div v-if="histories.length === 0 && !showHistoryForm" class="empty-state">
              <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
                <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" />
              </svg>
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
              :bizId="canonicalVendorId"
              @uploaded="documentListRef?.refresh?.()"
            />
            <DocumentListPanel ref="documentListRef" bizType="Vendor" :bizId="canonicalVendorId" view-mode="grid" />
          </div>
          <div v-show="activeTab === 'logs'" class="logs-tab">
            <div v-if="operationLogs.length === 0 && fieldChangeLogs.length === 0" class="empty-state">
              <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                <polyline points="14 2 14 8 20 8" />
              </svg>
              <p>暂无操作记录</p>
            </div>
            <div v-if="operationLogs.length > 0" class="logs-section">
              <div class="section-header" style="padding: 8px 0 10px; border-bottom: 1px solid rgba(255, 255, 255, 0.06); margin-bottom: 12px">
                <div class="section-dot section-dot--cyan"></div>
                <span class="section-title">操作日志</span>
              </div>
              <div v-for="log in operationLogs" :key="log.id" class="timeline-item">
                <div class="timeline-dot dot--primary"></div>
                <div class="timeline-content">
                  <span class="timeline-text">
                    <template v-if="log.bizType && log.bizType !== 'Vendor'">
                      <span class="log-biz-pill">{{ operationBizTypeLabel(log.bizType) }}</span>
                      ·
                    </template>
                    {{ log.operationType }} · {{ log.operationDesc || log.description || '—' }}
                    <template v-if="log.remark"> · 原因：{{ log.remark }}</template>
                    <template v-if="log.recordCode"> · 单号 {{ log.recordCode }}</template>
                  </span>
                  <span class="timeline-time">
                    {{ log.operatorUserName || '系统' }} · {{ formatDateTime(log.operationTime || log.createTime) }}
                  </span>
                </div>
              </div>
            </div>
            <div v-if="fieldChangeLogs.length > 0" class="logs-section" style="margin-top: 20px">
              <div class="section-header" style="padding: 8px 0 10px; border-bottom: 1px solid rgba(255, 255, 255, 0.06); margin-bottom: 12px">
                <div class="section-dot section-dot--cyan"></div>
                <span class="section-title">字段变更日志</span>
              </div>
              <div v-for="log in fieldChangeLogs" :key="log.id" class="timeline-item">
                <div class="timeline-dot dot--warning"></div>
                <div class="timeline-content">
                  <span class="timeline-text">
                    <template v-if="log.bizType && log.bizType !== 'Vendor'">
                      <span class="log-biz-pill">{{ operationBizTypeLabel(log.bizType) }}</span>
                      ·
                    </template>
                    {{ log.fieldLabel || log.fieldName }}：{{ log.oldValue || '(空)' }} → {{ log.newValue || '(空)' }}
                    <template v-if="log.recordCode"> · 单号 {{ log.recordCode }}</template>
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
      </template>
      <div v-else-if="!loading" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
          <circle cx="12" cy="12" r="10" />
          <line x1="12" y1="8" x2="12" y2="12" />
          <line x1="12" y1="16" x2="12.01" y2="16" />
        </svg>
        <p>供应商信息加载失败</p>
      </div>
    </div>

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

    <el-dialog v-model="showDeleteDialog" title="确认删除供应商" width="440px" :close-on-click-modal="false">
      <div style="color:rgba(200,216,232,0.75);font-size:13px;margin-bottom:16px">删除后可在回收站中查看并恢复。</div>
      <el-form label-width="90px">
        <el-form-item label="删除理由">
          <el-input v-model="deleteReason" type="textarea" :rows="3" placeholder="请输入删除理由" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showDeleteDialog = false">取消</el-button>
        <el-button type="danger" :loading="actionLoading" @click="handleDeleteVendor">确认删除</el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="showFreezeDialog"
      :title="freezeDialogTitle"
      width="440px"
      :close-on-click-modal="false"
      @closed="freezeReason = ''"
    >
      <div style="color:rgba(200,216,232,0.75);font-size:13px;margin-bottom:16px">{{ freezeDialogHint }}</div>
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
      <div style="color:rgba(200,216,232,0.75);font-size:13px;margin-bottom:16px">
        加入黑名单后可在黑名单管理中查看并移出。
      </div>
      <el-form label-width="90px">
        <el-form-item label="黑名单理由" required>
          <el-input v-model="blacklistReason" type="textarea" :rows="3" placeholder="请输入黑名单理由（必填）" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showBlacklistDialog = false">取消</el-button>
        <el-button type="warning" :loading="actionLoading" @click="handleAddBlacklistConfirm">确认加入黑名单</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="showRemoveBlacklistDialog" title="解除黑名单" width="440px" :close-on-click-modal="false">
      <div style="color:rgba(200,216,232,0.75);font-size:13px;margin-bottom:16px">
        解除后该供应商将不再处于黑名单状态，原因将记入操作日志。
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

    <ApplyTagsDialog
      v-model="showTagDialog"
      entity-type="VENDOR"
      :entity-ids="[canonicalVendorId]"
      title="为供应商添加标签"
      @success="fetchVendorTags"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { getVendorLevelLabel, getVendorIdentityLabel } from '@/constants/vendorEnums';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox, ElNotification } from 'element-plus';
import { vendorApi, vendorContactApi, vendorAddressApi, vendorBankApi } from '@/api/vendor';
import { tagApi, type TagDefinitionDto } from '@/api/tag';
import TagListDisplay from '@/components/Tag/TagListDisplay.vue';
import ApplyTagsDialog from '@/components/Tag/ApplyTagsDialog.vue';
import PartyStatusIcons from '@/components/party/PartyStatusIcons.vue';
import type { Vendor, VendorContactInfo, VendorAddress, VendorBankInfo } from '@/types/vendor';

import VendorAddressDialog from './VendorAddressDialog.vue';
import VendorBankDialog from './VendorBankDialog.vue';
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue';
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue';
import { formatDisplayDateTime } from '@/utils/displayDateTime';
import { operationBizTypeLabel } from '@/utils/businessLogLabels';
import { logRecentApi } from '@/api/logRecent';
import { VENDOR_RECENT_HISTORY_CHANGED_EVENT } from '@/constants/vendorRecentHistory';
import { favoriteApi } from '@/api/favorite';
import { VENDOR_FAVORITES_CHANGED_EVENT } from '@/constants/vendorFavorites';

const route = useRoute();
const router = useRouter();

const vendorId = route.params.id as string;
/** 详情加载后主键（与路由中业务编号区分，文档/标签等用主键更稳） */
const canonicalVendorId = computed(() => vendor.value?.id ?? (route.params.id as string));
const loading = ref(false);
const vendor = ref<Vendor | null>(null);
const vendorTags = ref<TagDefinitionDto[]>([]);
const contacts = ref<VendorContactInfo[]>([]);
const addresses = ref<VendorAddress[]>([]);
const banks = ref<VendorBankInfo[]>([]);
const histories = ref<any[]>([]);
const operationLogs = ref<any[]>([]);
const fieldChangeLogs = ref<any[]>([]);
const documentListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null);
const showTagDialog = ref(false);
const isFavorite = ref(false);
const favoriteLoading = ref(false);

const showDeleteDialog = ref(false);
const showBlacklistDialog = ref(false);
const showRemoveBlacklistDialog = ref(false);
const showFreezeDialog = ref(false);
const freezeMode = ref<'freeze' | 'unfreeze'>('freeze');
const freezeReason = ref('');
const deleteReason = ref('');
const blacklistReason = ref('');
const removeFromBlacklistReason = ref('');
const actionLoading = ref(false);

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
const tableRowStyle = () => ({ background: 'transparent' });

const freezeDialogTitle = computed(() => (freezeMode.value === 'freeze' ? '冻结供应商' : '启用供应商'));
const freezeDialogHint = computed(() =>
  freezeMode.value === 'freeze'
    ? '冻结后供应商将标记为冻结状态，是否确认冻结？'
    : '启用后将解除冻结状态，是否确认启用？'
);
const freezeReasonLabel = computed(() => (freezeMode.value === 'freeze' ? '冻结原因' : '启用原因'));
const freezeReasonPlaceholder = computed(() =>
  freezeMode.value === 'freeze' ? '请输入冻结原因（必填）' : '请输入启用原因（必填）'
);

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

const vendorPartyMuted = computed(
  () => !!(vendor.value?.isDisenable || vendor.value?.blackList)
);

const vendorLevelDisplay = computed(() => getVendorLevelLabel(vendor.value?.level));
const vendorIdentityDisplay = computed(() => getVendorIdentityLabel(vendor.value?.credit));

const refreshFavoriteStatus = async () => {
  const id = vendor.value?.id ?? vendorId;
  if (!id) {
    isFavorite.value = false;
    return;
  }
  try {
    isFavorite.value = await favoriteApi.checkFavorite('VENDOR', id);
  } catch {
    isFavorite.value = false;
  }
};

const toggleFavorite = async () => {
  const id = vendor.value?.id ?? vendorId;
  if (!id || favoriteLoading.value) return;
  favoriteLoading.value = true;
  try {
    if (isFavorite.value) {
      await favoriteApi.removeFavorite('VENDOR', id);
      isFavorite.value = false;
      ElNotification.success({ title: '已取消收藏', message: vendor.value?.officialName || vendor.value?.code || '' });
    } else {
      await favoriteApi.addFavorite({ entityType: 'VENDOR', entityId: id });
      isFavorite.value = true;
      ElNotification.success({ title: '收藏成功', message: vendor.value?.officialName || vendor.value?.code || '' });
    }
    window.dispatchEvent(new CustomEvent(VENDOR_FAVORITES_CHANGED_EVENT));
  } catch (error: any) {
    ElNotification.error({ title: '操作失败', message: error?.message || '收藏操作失败，请稍后重试' });
  } finally {
    favoriteLoading.value = false;
  }
};

const formatDateTime = (val?: string) => {
  if (!val) return '--';
  const s = formatDisplayDateTime(val);
  return s === '--' ? val : s;
};

function trackRecentDetail() {
  const v = vendor.value;
  if (!v?.id) return;
  logRecentApi
    .record({
      bizType: 'Vendor',
      recordId: String(v.id),
      recordCode: v.code || undefined,
      openKind: 'detail'
    })
    .then(() => window.dispatchEvent(new CustomEvent(VENDOR_RECENT_HISTORY_CHANGED_EVENT)))
    .catch(() => {});
}

const fetchLogsOnly = async (effectiveId: string) => {
  try {
    const [ops, fields] = await Promise.all([
      vendorApi.getOperationLogs(effectiveId).catch(() => []),
      vendorApi.getFieldChangeLogs(effectiveId).catch(() => [])
    ]);
    operationLogs.value = Array.isArray(ops) ? ops : [];
    fieldChangeLogs.value = Array.isArray(fields) ? fields : [];
  } catch {
    /* 静默 */
  }
};

const fetchVendor = async () => {
  if (!vendorId) return;
  loading.value = true;
  try {
    vendor.value = await vendorApi.getVendorById(vendorId);
    await refreshFavoriteStatus();
    trackRecentDetail();
  } catch (e) {
    console.error(e);
    ElMessage.error('获取供应商详情失败');
    vendor.value = null;
    loading.value = false;
    return;
  }

  const effectiveId = vendor.value?.id ?? vendorId;

  try {
    const [c, a, b, h, ops, fields] = await Promise.all([
      vendorContactApi.getContactsByVendorId(effectiveId).catch((err) => {
        console.warn('加载联系人失败', err);
        return [] as VendorContactInfo[];
      }),
      vendorAddressApi.getAddressesByVendorId(effectiveId).catch((err) => {
        console.warn('加载地址失败', err);
        return [] as VendorAddress[];
      }),
      vendorBankApi.getBanksByVendorId(effectiveId).catch((err) => {
        console.warn('加载银行信息失败', err);
        return [] as VendorBankInfo[];
      }),
      vendorApi.getVendorContactHistory(effectiveId).catch((err) => {
        console.warn('加载联系历史失败', err);
        return [] as any[];
      }),
      vendorApi.getOperationLogs(effectiveId).catch((err) => {
        console.warn('加载操作日志失败', err);
        return [] as any[];
      }),
      vendorApi.getFieldChangeLogs(effectiveId).catch((err) => {
        console.warn('加载字段变更日志失败', err);
        return [] as any[];
      })
    ]);
    contacts.value = c;
    addresses.value = a;
    banks.value = b;
    histories.value = h;
    operationLogs.value = Array.isArray(ops) ? ops : [];
    fieldChangeLogs.value = Array.isArray(fields) ? fields : [];
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
};

const fetchVendorTags = async () => {
  try {
    vendorTags.value = await tagApi.getEntityTags('VENDOR', canonicalVendorId.value);
  } catch {
    vendorTags.value = [];
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

const goBack = () => router.push({ name: 'VendorList' });
const handleEdit = () => router.push(`/vendors/${vendorId}/edit`);

const handleActivate = async () => {
  if (!vendor.value) return;
  const id = vendor.value.id;
  try {
    await vendorApi.activateVendor(id);
    ElMessage.success('供应商已激活（已审核）');
    await fetchVendor();
  } catch (e) {
    ElMessage.error('启用供应商失败');
  }
};

function onHeaderMoreCommand(command: string) {
  if (command === 'blacklist') {
    blacklistReason.value = '';
    showBlacklistDialog.value = true;
  } else if (command === 'unblacklist') {
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
  } else if (command === 'delete') {
    deleteReason.value = '';
    showDeleteDialog.value = true;
  }
}

const handleAddBlacklistConfirm = async () => {
  if (!blacklistReason.value.trim()) {
    ElMessage.warning('请输入黑名单理由');
    return;
  }
  const effectiveId = vendor.value?.id ?? vendorId;
  actionLoading.value = true;
  try {
    await vendorApi.addToBlacklist(effectiveId, blacklistReason.value.trim());
    ElMessage.success('已加入黑名单');
    showBlacklistDialog.value = false;
    blacklistReason.value = '';
    await fetchVendor();
    await fetchLogsOnly(effectiveId);
  } catch {
    ElMessage.error('加入黑名单失败');
  } finally {
    actionLoading.value = false;
  }
};

const handleConfirmRemoveBlacklist = async () => {
  if (!vendor.value) return;
  if (!removeFromBlacklistReason.value.trim()) {
    ElMessage.warning('请输入移出黑名单原因');
    return;
  }
  const effectiveId = vendor.value.id;
  actionLoading.value = true;
  try {
    await vendorApi.removeFromBlacklist(effectiveId, removeFromBlacklistReason.value.trim());
    ElMessage.success('已移出黑名单');
    showRemoveBlacklistDialog.value = false;
    removeFromBlacklistReason.value = '';
    await fetchVendor();
    await fetchLogsOnly(effectiveId);
  } catch {
    ElMessage.error('解除黑名单失败');
  } finally {
    actionLoading.value = false;
  }
};

const handleDeleteVendor = async () => {
  const effectiveId = vendor.value?.id ?? vendorId;
  actionLoading.value = true;
  try {
    await vendorApi.deleteVendorSoft(effectiveId, deleteReason.value || undefined);
    ElMessage.success('供应商已移至回收站');
    showDeleteDialog.value = false;
    router.push({ name: 'VendorList' });
  } catch {
    ElMessage.error('删除失败');
  } finally {
    actionLoading.value = false;
  }
};

const handleConfirmFreezeOrUnfreeze = async () => {
  if (!freezeReason.value.trim()) {
    ElMessage.warning(freezeMode.value === 'freeze' ? '请输入冻结原因' : '请输入启用原因');
    return;
  }
  const effectiveId = vendor.value?.id ?? vendorId;
  actionLoading.value = true;
  try {
    if (freezeMode.value === 'freeze') {
      await vendorApi.freezeVendor(effectiveId, freezeReason.value.trim());
      ElMessage.success('供应商已冻结');
    } else {
      await vendorApi.unfreezeVendor(effectiveId, freezeReason.value.trim());
      ElMessage.success('供应商已启用');
    }
    showFreezeDialog.value = false;
    freezeReason.value = '';
    await fetchVendor();
    await fetchLogsOnly(effectiveId);
  } catch (e: any) {
    ElMessage.error(e?.message || '操作失败');
  } finally {
    actionLoading.value = false;
  }
};

const goCreateContact = () => router.push({ name: 'VendorContactCreate', params: { id: vendorId } });
const goEditContact = (row: VendorContactInfo) => router.push({ name: 'VendorContactEdit', params: { id: vendorId, contactId: row.id } });

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
onMounted(fetchVendorTags);
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

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
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

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }
}

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
}

.btn-warning-outline {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: transparent;
  border: 1px solid rgba(201,154,69,0.4);
  border-radius: $border-radius-md;
  color: $color-amber;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  &:hover { background: rgba(201,154,69,0.12); }
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

.customer-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
}

.customer-avatar-lg {
  width: 48px;
  height: 48px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.3), rgba(0, 212, 255, 0.2));
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

.info-value--party-muted {
  color: rgba(150, 170, 195, 0.82);
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

  &--active {
    background: rgba(70, 191, 145, 0.15);
    color: $color-mint-green;
    border: 1px solid rgba(70, 191, 145, 0.3);
  }
  &--inactive {
    background: rgba(107, 122, 141, 0.15);
    color: #8a9bb0;
    border: 1px solid rgba(107, 122, 141, 0.3);
  }
  &--blacklist {
    background: rgba(201, 87, 69, 0.15);
    color: $color-red-brown;
    border: 1px solid rgba(201, 87, 69, 0.3);
  }
  &--frozen {
    background: rgba(201, 154, 69, 0.15);
    color: $color-amber;
    border: 1px solid rgba(201, 154, 69, 0.35);
  }

  &.status--draft {
    background: rgba(107, 122, 141, 0.15);
    color: #8a9bb0;
    border: 1px solid rgba(107, 122, 141, 0.3);
  }
  &.status--pending {
    background: rgba(201, 154, 69, 0.15);
    color: $color-amber;
    border: 1px solid rgba(201, 154, 69, 0.35);
  }
  &.status--approved {
    background: rgba(70, 191, 145, 0.15);
    color: $color-mint-green;
    border: 1px solid rgba(70, 191, 145, 0.3);
  }
}

.level-badge {
  display: inline-block;
  font-size: 10px;
  padding: 2px 7px;
  border-radius: 3px;

  &--credit {
    background: rgba(50, 149, 201, 0.15);
    color: $color-steel-cyan;
    border: 1px solid rgba(50, 149, 201, 0.25);
  }
}

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
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: rgba(0, 0, 0, 0.1);
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.6);
  }
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
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
  border-right: 1px solid rgba(255, 255, 255, 0.04);

  &:nth-child(3n) {
    border-right: none;
  }

  .info-label {
    font-size: 11px;
    color: $text-muted;
    letter-spacing: 0.5px;
    text-transform: uppercase;
  }

  .info-value {
    font-size: 13px;
    color: $text-secondary;

    &--code {
      font-family: 'Space Mono', monospace;
      font-size: 12px;
      color: $color-ice-blue;
    }
    &--time {
      font-size: 12px;
      color: $text-muted;
    }
  }
}

.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.tabs-nav {
  display: flex;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  padding: 0 16px;
  background: rgba(0, 0, 0, 0.1);
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

  &:hover {
    color: $text-secondary;
  }

  &--active {
    color: $cyan-primary;
    border-bottom-color: $cyan-primary;
  }
}

.tabs-body {
  padding: 20px;
}

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

.quantum-table {
  width: 100%;
  background: transparent !important;

  :deep(.el-table__inner-wrapper) {
    background: transparent;
  }
  :deep(tr) {
    background: transparent !important;
    &:hover td {
      background: rgba(0, 212, 255, 0.04) !important;
    }
  }
  :deep(.el-table__cell) {
    .el-button {
      white-space: nowrap !important;
    }
    .cell {
      white-space: nowrap;
    }
  }
}

.cell-primary {
  color: $text-primary;
  font-size: 13px;
}
.cell-secondary {
  color: $text-secondary;
  font-size: 13px;
}
.cell-muted {
  color: $text-muted;
  font-size: 12px;
}
.cell-code {
  font-family: 'Space Mono', monospace;
  font-size: 12px;
  color: $color-ice-blue;
}

.default-badge {
  display: inline-block;
  font-size: 10px;
  padding: 1px 6px;
  background: rgba(70, 191, 145, 0.15);
  color: $color-mint-green;
  border: 1px solid rgba(70, 191, 145, 0.3);
  border-radius: 3px;
}

.empty-state {
  text-align: center;
  padding: 60px;
  color: $text-muted;

  svg {
    margin-bottom: 12px;
    opacity: 0.3;
  }
  p {
    font-size: 14px;
    margin: 0;
  }
}

.inline-form {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px;
  background: rgba(0, 212, 255, 0.04);
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: $border-radius-md;
  margin-bottom: 14px;
  flex-wrap: wrap;
}

.history-tab {

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
}

// 与客户详情页一致：列表操作列按钮（见 列表操作按钮颜色规范 PRD）
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
    &:hover {
      background: rgba(201, 87, 69, 0.1);
      border-color: rgba(201, 87, 69, 0.45);
    }
  }

  // 辅助动作（设为主联系人 / 设为默认）：中性样式，非 primary/danger
  &--link {
    border: 1px solid rgba(148, 163, 184, 0.2);
    color: $text-secondary;
    &:hover {
      background: rgba(148, 163, 184, 0.08);
      border-color: rgba(148, 163, 184, 0.35);
      color: $text-primary;
    }
  }
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
</style>

<!-- 下拉 Teleport 到 body，需非 scoped（与客户详情一致） -->
<style lang="scss">
@import '@/assets/styles/variables.scss';

.vendor-detail-header-more-popper.el-dropdown__popper,
.vendor-detail-header-more-popper.el-popper {
  background: $layer-2 !important;
  border: 1px solid rgba(0, 212, 255, 0.15) !important;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.45) !important;
}

.vendor-detail-header-more-popper .el-dropdown-menu {
  background: transparent !important;
  border: none !important;
  box-shadow: none !important;
  padding: 4px 0 !important;
}

.vendor-detail-header-more-popper .el-dropdown-menu__item {
  color: rgba(200, 220, 240, 0.92) !important;
  font-size: 13px;

  &:hover,
  &:focus {
    background: rgba(0, 212, 255, 0.1) !important;
    color: #e8f4ff !important;
  }
}

.vendor-detail-header-more-popper .detail-more-item--danger {
  color: $color-red-brown !important;

  &:hover,
  &:focus {
    color: #e8a090 !important;
    background: rgba(201, 87, 69, 0.12) !important;
  }
}
</style>
