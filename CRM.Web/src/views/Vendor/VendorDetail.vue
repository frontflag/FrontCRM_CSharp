<template>
  <div class="customer-detail-page">
    <!-- 页面头部（布局与样式与客户详情一致） -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('vendorDetail.back') }}
        </button>
        <div class="customer-title-group">
          <div class="customer-avatar-lg">{{ vendorAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1
                  class="page-title"
                  :class="{ 'page-title--muted': vendorPartyMuted }"
                >
                  {{ vendorHeaderTitle }}
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
                :title="isFavorite ? t('vendorDetail.favoriteRemove') : t('vendorDetail.favoriteAdd')"
                :aria-label="isFavorite ? t('vendorDetail.favoriteRemove') : t('vendorDetail.favoriteAriaVendor')"
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
              <span class="customer-code">{{ maskPurchaseSensitiveFields ? '—' : vendor?.code }}</span>
              <span class="status-badge" :class="vendorStatusClass">{{ vendorStatusText }}</span>
              <span v-if="vendor?.isDisenable" class="status-badge status--frozen">{{ t('vendorDetail.frozen') }}</span>
              <span v-if="vendorLevelDisplay !== '--'" class="level-badge">{{ vendorLevelDisplay }}</span>
              <span v-if="vendorIdentityDisplay !== '--'" class="level-badge level-badge--credit">{{ vendorIdentityDisplay }}</span>
              <span v-if="vendor?.blackList" class="status-badge status--blacklist">{{ t('vendorDetail.blacklistBadge') }}</span>
            </div>
            <div class="title-tags-row">
              <TagListDisplay :tags="vendorTags" />
              <button class="btn-add-tag" @click="showTagDialog = true">{{ t('vendorDetail.addTag') }}</button>
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
          {{ t('vendorDetail.edit') }}
        </button>
        <el-dropdown
          v-if="vendor"
          trigger="click"
          placement="bottom-end"
          popper-class="vendor-detail-header-more-popper"
          @command="onHeaderMoreCommand"
        >
          <button type="button" class="btn-more-actions" :title="t('vendorDetail.moreActions')" :aria-label="t('vendorDetail.moreActions')">
            <span class="btn-more-actions__dots" aria-hidden="true">⋯</span>
          </button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item v-if="!vendor.blackList" command="blacklist">{{ t('vendorDetail.moreBlacklist') }}</el-dropdown-item>
              <el-dropdown-item v-else command="unblacklist">{{ t('vendorDetail.moreUnblacklist') }}</el-dropdown-item>
              <el-dropdown-item v-if="!vendor.isDisenable" command="freeze">{{ t('vendorDetail.moreFreeze') }}</el-dropdown-item>
              <el-dropdown-item v-else command="unfreeze">{{ t('vendorDetail.moreUnfreeze') }}</el-dropdown-item>
              <el-dropdown-item command="delete" divided class="detail-more-item--danger">{{ t('vendorDetail.moreDeleteVendor') }}</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
        <button class="btn-success" v-if="vendor?.status !== 1" type="button" @click="handleActivate">
          {{ t('vendorDetail.activate') }}
        </button>
      </div>
    </div>

    <div v-loading="loading" element-loading-background="rgba(10,22,40,0.8)" class="detail-content">
      <template v-if="vendor">
      <!-- 基本信息卡片 -->
      <div class="info-section">
        <div class="section-header">
          <div class="section-dot section-dot--cyan"></div>
          <span class="section-title">{{ t('vendorDetail.basicInfo') }}</span>
        </div>
        <div class="info-grid">
          <div class="info-item">
            <span class="info-label">{{ t('vendorDetail.fields.vendorCode') }}</span>
            <span class="info-value info-value--code">{{ maskPurchaseSensitiveFields ? '—' : vendor.code }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('vendorDetail.fields.vendorName') }}</span>
            <span
              class="info-value"
              :class="{
                'info-value--party-muted': vendor.isDisenable || vendor.blackList
              }"
            >{{ maskPurchaseSensitiveFields ? '—' : (vendor.officialName || '--') }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('vendorDetail.fields.shortName') }}</span>
            <span class="info-value">{{ maskPurchaseSensitiveFields ? '—' : (vendor.nickName || '--') }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('vendorDetail.fields.industry') }}</span>
            <span class="info-value">{{ industryDisplay }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('vendorDetail.fields.regionAddress') }}</span>
            <span class="info-value">{{ maskPurchaseSensitiveFields ? '—' : (vendor.officeAddress || '--') }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('vendorDetail.fields.level') }}</span>
            <span class="info-value">{{ vendorLevelDisplay }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('vendorDetail.fields.identity') }}</span>
            <span class="info-value">{{ vendorIdentityDisplay }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('vendorDetail.fields.remarks') }}</span>
            <span class="info-value">{{ vendor.companyInfo || '--' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('vendorDetail.fields.createdAt') }}</span>
            <span class="info-value info-value--time">{{ formatDateTime(vendor.createTime) }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('vendorDetail.fields.updatedAt') }}</span>
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
                {{ t('vendorDetail.contacts.add') }}
              </button>
            </div>
            <CrmDataTable
              :data="contacts"
              class="quantum-table"
              :header-cell-style="tableHeaderStyle"
              :cell-style="tableCellStyle"
              :row-style="tableRowStyle"
            >
              <el-table-column prop="cName" :label="t('vendorDetail.contacts.name')" min-width="140" show-overflow-tooltip>
                <template #default="{ row }">
                  <span class="cell-primary">{{ maskPurchaseSensitiveFields ? '—' : (row.cName || row.eName || '--') }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="title" :label="t('vendorDetail.contacts.title')" min-width="120">
                <template #default="{ row }"><span class="cell-secondary">{{ maskPurchaseSensitiveFields ? '—' : (row.title || '--') }}</span></template>
              </el-table-column>
              <el-table-column prop="department" :label="t('vendorDetail.contacts.department')" min-width="120">
                <template #default="{ row }"><span class="cell-secondary">{{ maskPurchaseSensitiveFields ? '—' : (row.department || '--') }}</span></template>
              </el-table-column>
              <el-table-column prop="mobile" :label="t('vendorDetail.contacts.mobile')" min-width="130">
                <template #default="{ row }"><span class="cell-code">{{ maskPurchaseSensitiveFields ? '—' : (row.mobile || '--') }}</span></template>
              </el-table-column>
              <el-table-column prop="email" :label="t('vendorDetail.contacts.email')" min-width="180" show-overflow-tooltip>
                <template #default="{ row }"><span class="cell-secondary">{{ maskPurchaseSensitiveFields ? '—' : (row.email || '--') }}</span></template>
              </el-table-column>
              <el-table-column :label="t('vendorDetail.contacts.mainContact')" width="90" align="center">
                <template #default="{ row }">
                  <span v-if="row.isMain" class="default-badge">{{ t('vendorDetail.contacts.mainBadge') }}</span>
                  <span v-else class="cell-muted">--</span>
                </template>
              </el-table-column>
              <el-table-column :label="t('vendorDetail.contacts.actions')" width="200" fixed="right" class-name="op-col" label-class-name="op-col">
                <template #default="{ row }">
                  <div @click.stop @dblclick.stop>
                    <div class="action-btns">
                      <button type="button" class="action-btn action-btn--primary" @click.stop="goEditContact(row)">{{ t('vendorDetail.contacts.edit') }}</button>
                      <button type="button" class="action-btn action-btn--danger" @click.stop="handleDeleteContact(row)">{{ t('vendorDetail.contacts.delete') }}</button>
                      <button
                        v-if="!row.isMain"
                        type="button"
                        class="action-btn action-btn--info"
                        @click.stop="handleSetMainContact(row)"
                      >
                        {{ t('vendorDetail.contacts.setMain') }}
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
                {{ t('vendorDetail.addresses.add') }}
              </button>
            </div>
            <CrmDataTable
              :data="addresses"
              class="quantum-table"
              :header-cell-style="tableHeaderStyle"
              :cell-style="tableCellStyle"
              :row-style="tableRowStyle"
            >
              <el-table-column prop="addressType" :label="t('vendorDetail.addresses.type')" width="100">
                <template #default="{ row }">
                  <span class="cell-secondary">{{ getAddressTypeLabel(row.addressType) }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="contactName" :label="t('vendorDetail.addresses.contactName')" width="120">
                <template #default="{ row }"><span class="cell-primary">{{ maskPurchaseSensitiveFields ? '—' : (row.contactName || '--') }}</span></template>
              </el-table-column>
              <el-table-column prop="contactPhone" :label="t('vendorDetail.addresses.phone')" width="140">
                <template #default="{ row }"><span class="cell-code">{{ maskPurchaseSensitiveFields ? '—' : (row.contactPhone || '--') }}</span></template>
              </el-table-column>
              <el-table-column :label="t('vendorDetail.addresses.fullAddress')" min-width="260" show-overflow-tooltip>
                <template #default="{ row }">
                  <span class="cell-secondary">{{ maskPurchaseSensitiveFields ? '—' : formatFullAddress(row) }}</span>
                </template>
              </el-table-column>
              <el-table-column :label="t('vendorDetail.addresses.defaultCol')" width="80" align="center">
                <template #default="{ row }">
                  <span v-if="row.isDefault" class="default-badge">{{ t('vendorDetail.addresses.defaultBadge') }}</span>
                  <span v-else class="cell-muted">--</span>
                </template>
              </el-table-column>
              <el-table-column :label="t('vendorDetail.contacts.actions')" width="220" fixed="right" class-name="op-col" label-class-name="op-col">
                <template #default="{ row }">
                  <div @click.stop @dblclick.stop>
                    <div class="action-btns">
                      <button type="button" class="action-btn action-btn--primary" @click.stop="openEditAddress(row)">{{ t('vendorDetail.addresses.edit') }}</button>
                      <button type="button" class="action-btn action-btn--danger" @click.stop="handleDeleteAddress(row)">{{ t('vendorDetail.addresses.delete') }}</button>
                      <button
                        v-if="!row.isDefault"
                        type="button"
                        class="action-btn action-btn--info"
                        @click.stop="handleSetDefaultAddress(row)"
                      >
                        {{ t('vendorDetail.addresses.setDefault') }}
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
                {{ t('vendorDetail.banks.add') }}
              </button>
            </div>
            <CrmDataTable
              :data="banks"
              class="quantum-table"
              :header-cell-style="tableHeaderStyle"
              :cell-style="tableCellStyle"
              :row-style="tableRowStyle"
            >
              <el-table-column prop="accountName" :label="t('vendorDetail.banks.accountName')" min-width="160" show-overflow-tooltip>
                <template #default="{ row }"><span class="cell-primary">{{ maskPurchaseSensitiveFields ? '—' : (row.accountName || '--') }}</span></template>
              </el-table-column>
              <el-table-column prop="bankName" :label="t('vendorDetail.banks.bankName')" min-width="160" show-overflow-tooltip>
                <template #default="{ row }"><span class="cell-secondary">{{ maskPurchaseSensitiveFields ? '—' : (row.bankName || '--') }}</span></template>
              </el-table-column>
              <el-table-column prop="bankBranch" :label="t('vendorDetail.banks.branch')" min-width="160" show-overflow-tooltip>
                <template #default="{ row }"><span class="cell-secondary">{{ maskPurchaseSensitiveFields ? '—' : (row.bankBranch || '--') }}</span></template>
              </el-table-column>
              <el-table-column prop="bankAccount" :label="t('vendorDetail.banks.accountNo')" min-width="180" show-overflow-tooltip>
                <template #default="{ row }"><span class="cell-code">{{ maskPurchaseSensitiveFields ? '—' : (row.bankAccount || '--') }}</span></template>
              </el-table-column>
              <el-table-column :label="t('vendorDetail.banks.defaultCol')" width="80" align="center">
                <template #default="{ row }">
                  <span v-if="row.isDefault" class="default-badge">{{ t('vendorDetail.banks.defaultBadge') }}</span>
                  <span v-else class="cell-muted">--</span>
                </template>
              </el-table-column>
              <el-table-column :label="t('vendorDetail.contacts.actions')" width="220" fixed="right" class-name="op-col" label-class-name="op-col">
                <template #default="{ row }">
                  <div @click.stop @dblclick.stop>
                    <div class="action-btns">
                      <button type="button" class="action-btn action-btn--primary" @click.stop="openEditBank(row)">{{ t('vendorDetail.banks.edit') }}</button>
                      <button type="button" class="action-btn action-btn--danger" @click.stop="handleDeleteBank(row)">{{ t('vendorDetail.banks.delete') }}</button>
                      <button
                        v-if="!row.isDefault"
                        type="button"
                        class="action-btn action-btn--info"
                        @click.stop="handleSetDefaultBank(row)"
                      >
                        {{ t('vendorDetail.banks.setDefault') }}
                      </button>
                    </div>
                  </div>
                </template>
              </el-table-column>
            </CrmDataTable>
          </div>
          <div v-show="activeTab === 'documents'" class="documents-tab">
            <DocumentUploadPanel
              bizType="Vendor"
              :bizId="canonicalVendorId"
              @uploaded="documentListRef?.refresh?.()"
            />
            <DocumentListPanel ref="documentListRef" bizType="Vendor" :bizId="canonicalVendorId" view-mode="grid" />
          </div>
          <div v-show="activeTab === 'history'" class="history-tab">
            <div class="tab-toolbar">
              <button class="btn-add-item" @click="showHistoryForm = true">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <line x1="12" y1="5" x2="12" y2="19" />
                  <line x1="5" y1="12" x2="19" y2="12" />
                </svg>
                {{ t('vendorDetail.history.addRecord') }}
              </button>
            </div>
            <div v-if="showHistoryForm" class="inline-form">
              <el-select v-model="newHistory.type" :placeholder="t('vendorDetail.history.typePlaceholder')" size="small" style="width:120px">
                <el-option :label="t('vendorDetail.history.types.call')" value="call" />
                <el-option :label="t('vendorDetail.history.types.visit')" value="visit" />
                <el-option :label="t('vendorDetail.history.types.email')" value="email" />
                <el-option :label="t('vendorDetail.history.types.meeting')" value="meeting" />
                <el-option :label="t('vendorDetail.history.types.other')" value="other" />
              </el-select>
              <el-input v-model="newHistory.subject" :placeholder="t('vendorDetail.history.subject')" size="small" style="width:200px" />
              <el-input v-model="newHistory.content" :placeholder="t('vendorDetail.history.content')" size="small" style="flex:1" />
              <el-input v-model="newHistory.result" :placeholder="t('vendorDetail.history.result')" size="small" style="width:200px" />
              <button class="btn-add-item" @click="submitHistory">{{ t('vendorDetail.history.save') }}</button>
              <button class="action-btn" @click="cancelHistoryForm">{{ t('vendorDetail.history.cancel') }}</button>
            </div>
            <div v-if="histories.length === 0 && !showHistoryForm" class="empty-state">
              <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
                <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" />
              </svg>
              <p>{{ t('vendorDetail.history.empty') }}</p>
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
                    <button class="action-btn action-btn--danger" @click="deleteHistory(h)">{{ t('vendorDetail.history.delete') }}</button>
                  </div>
                </div>
                <div class="timeline-body">
                  <span v-if="h.content" class="timeline-line">{{ t('vendorDetail.history.contentLine', { text: h.content }) }}</span>
                  <span v-if="h.result" class="timeline-line">{{ t('vendorDetail.history.resultLine', { text: h.result }) }}</span>
                  <span class="timeline-time">
                    {{ formatDateTime(h.time || h.createTime) }}
                  </span>
                </div>
              </div>
            </div>
          </div>
          <div v-show="activeTab === 'logs'" class="logs-tab">
            <div v-if="operationLogs.length === 0 && fieldChangeLogs.length === 0" class="empty-state">
              <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                <polyline points="14 2 14 8 20 8" />
              </svg>
              <p>{{ t('vendorDetail.logs.empty') }}</p>
            </div>
            <div v-if="operationLogs.length > 0" class="logs-section">
              <div class="section-header" style="padding: 8px 0 10px; border-bottom: 1px solid rgba(255, 255, 255, 0.06); margin-bottom: 12px">
                <div class="section-dot section-dot--cyan"></div>
                <span class="section-title">{{ t('vendorDetail.logs.operationLog') }}</span>
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
                    <template v-if="log.remark">{{ t('vendorDetail.logs.reasonSuffix', { text: log.remark }) }}</template>
                    <template v-if="log.recordCode">{{ t('vendorDetail.logs.docNoSuffix', { code: log.recordCode }) }}</template>
                  </span>
                  <span class="timeline-time">
                    {{ log.operatorUserName || t('vendorDetail.logs.system') }} · {{ formatDateTime(log.operationTime || log.createTime) }}
                  </span>
                </div>
              </div>
            </div>
            <div v-if="fieldChangeLogs.length > 0" class="logs-section" style="margin-top: 20px">
              <div class="section-header" style="padding: 8px 0 10px; border-bottom: 1px solid rgba(255, 255, 255, 0.06); margin-bottom: 12px">
                <div class="section-dot section-dot--cyan"></div>
                <span class="section-title">{{ t('vendorDetail.logs.fieldChangeLog') }}</span>
              </div>
              <div v-for="log in fieldChangeLogs" :key="log.id" class="timeline-item">
                <div class="timeline-dot dot--warning"></div>
                <div class="timeline-content">
                  <span class="timeline-text">
                    <template v-if="log.bizType && log.bizType !== 'Vendor'">
                      <span class="log-biz-pill">{{ operationBizTypeLabel(log.bizType) }}</span>
                      ·
                    </template>
                    {{ log.fieldLabel || log.fieldName }}：{{ log.oldValue || t('vendorDetail.logs.emptyValue') }}{{ t('vendorDetail.logs.arrow') }}{{ log.newValue || t('vendorDetail.logs.emptyValue') }}
                    <template v-if="log.recordCode">{{ t('vendorDetail.logs.docNoSuffix', { code: log.recordCode }) }}</template>
                  </span>
                  <span class="timeline-time">
                    {{ log.changedByUserName || log.operatorUserName || t('vendorDetail.logs.system') }} ·
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
        <p>{{ t('vendorDetail.loadFailed') }}</p>
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

    <el-dialog v-model="showDeleteDialog" :title="t('vendorDetail.deleteVendor.title')" width="440px" :close-on-click-modal="false">
      <div style="color:rgba(200,216,232,0.75);font-size:13px;margin-bottom:16px">{{ t('vendorDetail.deleteVendor.hint') }}</div>
      <el-form label-width="90px">
        <el-form-item :label="t('vendorDetail.deleteVendor.reasonLabel')">
          <el-input v-model="deleteReason" type="textarea" :rows="3" :placeholder="t('vendorDetail.deleteVendor.reasonPh')" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showDeleteDialog = false">{{ t('vendorDetail.deleteVendor.cancel') }}</el-button>
        <el-button type="danger" :loading="actionLoading" @click="handleDeleteVendor">{{ t('vendorDetail.deleteVendor.confirm') }}</el-button>
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
        <el-button @click="showFreezeDialog = false">{{ t('vendorDetail.freeze.cancel') }}</el-button>
        <el-button
          :type="freezeMode === 'freeze' ? 'warning' : 'primary'"
          :loading="actionLoading"
          @click="handleConfirmFreezeOrUnfreeze"
        >
          {{ t('vendorDetail.freeze.confirm') }}
        </el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="showBlacklistDialog" :title="t('vendorDetail.blacklist.title')" width="440px" :close-on-click-modal="false">
      <div style="color:rgba(200,216,232,0.75);font-size:13px;margin-bottom:16px">
        {{ t('vendorDetail.blacklist.hint') }}
      </div>
      <el-form label-width="90px">
        <el-form-item :label="t('vendorDetail.blacklist.reasonLabel')" required>
          <el-input v-model="blacklistReason" type="textarea" :rows="3" :placeholder="t('vendorDetail.blacklist.reasonPh')" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showBlacklistDialog = false">{{ t('vendorDetail.blacklist.cancel') }}</el-button>
        <el-button type="warning" :loading="actionLoading" @click="handleAddBlacklistConfirm">{{ t('vendorDetail.blacklist.confirm') }}</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="showRemoveBlacklistDialog" :title="t('vendorDetail.removeBlacklist.title')" width="440px" :close-on-click-modal="false">
      <div style="color:rgba(200,216,232,0.75);font-size:13px;margin-bottom:16px">
        {{ t('vendorDetail.removeBlacklist.hint') }}
      </div>
      <el-form label-width="90px">
        <el-form-item :label="t('vendorDetail.removeBlacklist.reasonLabel')" required>
          <el-input
            v-model="removeFromBlacklistReason"
            type="textarea"
            :rows="3"
            :placeholder="t('vendorDetail.removeBlacklist.reasonPh')"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showRemoveBlacklistDialog = false">{{ t('vendorDetail.removeBlacklist.cancel') }}</el-button>
        <el-button type="primary" :loading="actionLoading" @click="handleConfirmRemoveBlacklist">{{ t('vendorDetail.removeBlacklist.confirm') }}</el-button>
      </template>
    </el-dialog>

    <ApplyTagsDialog
      v-model="showTagDialog"
      entity-type="VENDOR"
      :entity-ids="[canonicalVendorId]"
      :title="t('vendorDetail.tagDialogTitle')"
      @success="fetchVendorTags"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useVendorDictStore } from '@/stores/vendorDict';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
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
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask';

const route = useRoute();
const router = useRouter();
const { t, locale } = useI18n();
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask();
const vendorDict = useVendorDictStore();

const vendorId = route.params.id as string;
/** 详情加载后主键（与路由中业务编号区分，文档/标签等用主键更稳） */
const canonicalVendorId = computed(() => vendor.value?.id ?? (route.params.id as string));
const loading = ref(false);
const vendor = ref<Vendor | null>(null);

const vendorHeaderTitle = computed(() => {
  if (!vendor.value) return t('vendorDetail.titleFallback');
  if (maskPurchaseSensitiveFields.value) return '—';
  return vendor.value.officialName || t('vendorDetail.titleFallback');
});
const vendorAvatarChar = computed(() => {
  if (maskPurchaseSensitiveFields.value) return '?';
  const s = (vendor.value?.officialName || '?').trim();
  return s ? s[0]! : '?';
});

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

const freezeDialogTitle = computed(() =>
  freezeMode.value === 'freeze' ? t('vendorDetail.freeze.titleFreeze') : t('vendorDetail.freeze.titleUnfreeze')
);
const freezeDialogHint = computed(() =>
  freezeMode.value === 'freeze' ? t('vendorDetail.freeze.hintFreeze') : t('vendorDetail.freeze.hintUnfreeze')
);
const freezeReasonLabel = computed(() =>
  freezeMode.value === 'freeze' ? t('vendorDetail.freeze.labelFreeze') : t('vendorDetail.freeze.labelUnfreeze')
);
const freezeReasonPlaceholder = computed(() =>
  freezeMode.value === 'freeze' ? t('vendorDetail.freeze.phFreeze') : t('vendorDetail.freeze.phUnfreeze')
);

const tabs = computed(() => {
  void locale.value;
  return [
    { key: 'contacts', label: t('vendorDetail.tabs.contacts') },
    { key: 'addresses', label: t('vendorDetail.tabs.addresses') },
    { key: 'banks', label: t('vendorDetail.tabs.banks') },
    { key: 'documents', label: t('vendorDetail.tabs.documents') },
    { key: 'history', label: t('vendorDetail.tabs.history') },
    { key: 'logs', label: t('vendorDetail.tabs.logs') }
  ];
});
const activeTab = ref('contacts');

/** 与列表 VendorList / types 一致：1=新建 2=待审核 10=已审核… */
const vendorStatusText = computed(() => {
  const s = vendor.value?.status;
  if (s === 1) return t('vendorList.status.new');
  if (s === 2) return t('vendorList.status.pending');
  if (s === 10) return t('vendorList.status.approved');
  if (s === 12) return t('vendorList.status.pendingFinance');
  if (s === 20) return t('vendorList.status.financeFiled');
  if (s === -1) return t('vendorList.status.failed');
  if (s === 0) return t('vendorList.status.draft');
  return t('vendorList.status.unknown');
});

const vendorStatusClass = computed(() => {
  const s = vendor.value?.status ?? 0;
  if (s === 2 || s === 12) return 'status--pending';
  if (s === 10 || s === 20) return 'status--approved';
  if (s === -1) return 'status--danger';
  if (s === 1) return 'status--draft';
  return 'status--draft';
});

const vendorPartyMuted = computed(
  () => !!(vendor.value?.isDisenable || vendor.value?.blackList)
);

const vendorLevelDisplay = computed(() => vendorDict.levelLabel(vendor.value?.level));
const vendorIdentityDisplay = computed(() => vendorDict.identityLabel(vendor.value?.credit));
const industryDisplay = computed(() => vendorDict.industryLabel(vendor.value?.industry));

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
      ElNotification.success({
        title: t('vendorDetail.notify.unfavorited'),
        message: maskPurchaseSensitiveFields.value ? '—' : vendor.value?.officialName || vendor.value?.code || ''
      });
    } else {
      await favoriteApi.addFavorite({ entityType: 'VENDOR', entityId: id });
      isFavorite.value = true;
      ElNotification.success({
        title: t('vendorDetail.notify.favorited'),
        message: maskPurchaseSensitiveFields.value ? '—' : vendor.value?.officialName || vendor.value?.code || ''
      });
    }
    window.dispatchEvent(new CustomEvent(VENDOR_FAVORITES_CHANGED_EVENT));
  } catch (error: any) {
    ElNotification.error({
      title: t('vendorDetail.notify.favoriteFailedTitle'),
      message: error?.message || t('vendorDetail.notify.favoriteFailedMsg')
    });
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
    if (vendor.value) {
      void vendorDict.hydrateVendorEditForm({
        industry: vendor.value.industry,
        level: vendor.value.level,
        credit: vendor.value.credit,
        paymentMethod: vendor.value.paymentMethod ?? ''
      });
    }
  } catch (e) {
    console.error(e);
    ElMessage.error(t('vendorDetail.messages.fetchFailed'));
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
    case 'call':
      return t('vendorDetail.history.types.call');
    case 'visit':
      return t('vendorDetail.history.types.visit');
    case 'email':
      return t('vendorDetail.history.types.email');
    case 'meeting':
      return t('vendorDetail.history.types.meeting');
    default:
      return t('vendorDetail.history.types.other');
  }
};

const submitHistory = async () => {
  if (!newHistory.value.content.trim()) {
    ElMessage.warning(t('vendorDetail.messages.fillHistoryContent'));
    return;
  }
  try {
    await vendorApi.addVendorContactHistory(vendorId, {
      type: newHistory.value.type,
      subject: newHistory.value.subject,
      content: newHistory.value.content,
      result: newHistory.value.result
    });
    ElMessage.success(t('vendorDetail.messages.historySaved'));
    showHistoryForm.value = false;
    newHistory.value = { type: 'call', subject: '', content: '', result: '' };
    histories.value = await vendorApi.getVendorContactHistory(vendorId);
  } catch {
    ElMessage.error(t('vendorDetail.messages.historySaveFailed'));
  }
};

const cancelHistoryForm = () => {
  showHistoryForm.value = false;
};

const deleteHistory = async (h: any) => {
  try {
    await ElMessageBox.confirm(t('vendorDetail.messages.deleteHistoryConfirm'), t('vendorDetail.messages.deleteHistoryTitle'), {
      type: 'warning'
    });
    await vendorApi.deleteVendorContactHistory(vendorId, h.id);
    ElMessage.success(t('vendorDetail.messages.historyDeleted'));
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
    ElMessage.success(t('vendorDetail.messages.activated'));
    await fetchVendor();
  } catch (e) {
    ElMessage.error(t('vendorDetail.messages.activateFailed'));
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
    ElMessage.warning(t('vendorDetail.messages.blacklistReasonRequired'));
    return;
  }
  const effectiveId = vendor.value?.id ?? vendorId;
  actionLoading.value = true;
  try {
    await vendorApi.addToBlacklist(effectiveId, blacklistReason.value.trim());
    ElMessage.success(t('vendorDetail.messages.blacklisted'));
    showBlacklistDialog.value = false;
    blacklistReason.value = '';
    await fetchVendor();
    await fetchLogsOnly(effectiveId);
  } catch {
    ElMessage.error(t('vendorDetail.messages.blacklistFailed'));
  } finally {
    actionLoading.value = false;
  }
};

const handleConfirmRemoveBlacklist = async () => {
  if (!vendor.value) return;
  if (!removeFromBlacklistReason.value.trim()) {
    ElMessage.warning(t('vendorDetail.messages.removeReasonRequired'));
    return;
  }
  const effectiveId = vendor.value.id;
  actionLoading.value = true;
  try {
    await vendorApi.removeFromBlacklist(effectiveId, removeFromBlacklistReason.value.trim());
    ElMessage.success(t('vendorDetail.messages.removedBlacklist'));
    showRemoveBlacklistDialog.value = false;
    removeFromBlacklistReason.value = '';
    await fetchVendor();
    await fetchLogsOnly(effectiveId);
  } catch {
    ElMessage.error(t('vendorDetail.messages.removeBlacklistFailed'));
  } finally {
    actionLoading.value = false;
  }
};

const handleDeleteVendor = async () => {
  const effectiveId = vendor.value?.id ?? vendorId;
  actionLoading.value = true;
  try {
    await vendorApi.deleteVendorSoft(effectiveId, deleteReason.value || undefined);
    ElMessage.success(t('vendorDetail.messages.movedToRecycle'));
    showDeleteDialog.value = false;
    router.push({ name: 'VendorList' });
  } catch {
    ElMessage.error(t('vendorDetail.messages.deleteFailed'));
  } finally {
    actionLoading.value = false;
  }
};

const handleConfirmFreezeOrUnfreeze = async () => {
  if (!freezeReason.value.trim()) {
    ElMessage.warning(
      freezeMode.value === 'freeze'
        ? t('vendorDetail.messages.freezeReasonRequired')
        : t('vendorDetail.messages.unfreezeReasonRequired')
    );
    return;
  }
  const effectiveId = vendor.value?.id ?? vendorId;
  actionLoading.value = true;
  try {
    if (freezeMode.value === 'freeze') {
      await vendorApi.freezeVendor(effectiveId, freezeReason.value.trim());
      ElMessage.success(t('vendorDetail.messages.frozenOk'));
    } else {
      await vendorApi.unfreezeVendor(effectiveId, freezeReason.value.trim());
      ElMessage.success(t('vendorDetail.messages.unfrozenOk'));
    }
    showFreezeDialog.value = false;
    freezeReason.value = '';
    await fetchVendor();
    await fetchLogsOnly(effectiveId);
  } catch (e: any) {
    ElMessage.error(e?.message || t('vendorDetail.messages.opFailed'));
  } finally {
    actionLoading.value = false;
  }
};

const goCreateContact = () => router.push({ name: 'VendorContactCreate', params: { id: vendorId } });
const goEditContact = (row: VendorContactInfo) => router.push({ name: 'VendorContactEdit', params: { id: vendorId, contactId: row.id } });

const handleDeleteContact = async (row: VendorContactInfo) => {
  try {
    await ElMessageBox.confirm(
      t('vendorDetail.confirm.deleteContact', {
        name: row.cName || row.eName || row.mobile || row.email || '—'
      }),
      t('vendorDetail.confirm.deleteContactTitle'),
      { type: 'warning' }
    );
    await vendorContactApi.deleteContact(row.id);
    ElMessage.success(t('vendorDetail.messages.contactDeleted'));
    await refreshContacts();
  } catch {
    // ignore
  }
};

const handleSetMainContact = async (row: VendorContactInfo) => {
  try {
    await vendorApi.setMainContact(row.id);
    ElMessage.success(t('vendorDetail.messages.mainContactSet'));
    await refreshContacts();
  } catch {
    ElMessage.error(t('vendorDetail.messages.mainContactFailed'));
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
      ElMessage.success(t('vendorDetail.messages.addressCreated'));
    } else if (editingAddress.value) {
      await vendorAddressApi.updateAddress(editingAddress.value.id, payload);
      ElMessage.success(t('vendorDetail.messages.addressUpdated'));
    }
    addressDialogVisible.value = false;
    await refreshAddresses();
  } catch {
    ElMessage.error(t('vendorDetail.messages.addressSaveFailed'));
  }
};

const handleDeleteAddress = async (row: VendorAddress) => {
  try {
    await ElMessageBox.confirm(
      t('vendorDetail.messages.deleteAddressConfirm', { detail: formatFullAddress(row) }),
      t('vendorDetail.messages.deleteAddressTitle'),
      { type: 'warning' }
    );
    await vendorAddressApi.deleteAddress(row.id);
    ElMessage.success(t('vendorDetail.messages.addressDeleted'));
    await refreshAddresses();
  } catch {
    // ignore
  }
};

const handleSetDefaultAddress = async (row: VendorAddress) => {
  try {
    await vendorAddressApi.setDefaultAddress(row.id);
    ElMessage.success(t('vendorDetail.messages.defaultAddressSet'));
    await refreshAddresses();
  } catch {
    ElMessage.error(t('vendorDetail.messages.defaultAddressFailed'));
  }
};

const getAddressTypeLabel = (type: number) =>
  type === 2 ? t('vendorDetail.addresses.typeBilling') : t('vendorDetail.addresses.typeShipping');

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
      ElMessage.success(t('vendorDetail.messages.bankCreated'));
    } else if (editingBank.value) {
      await vendorBankApi.updateBank(editingBank.value.id, payload);
      ElMessage.success(t('vendorDetail.messages.bankUpdated'));
    }
    bankDialogVisible.value = false;
    await refreshBanks();
  } catch {
    ElMessage.error(t('vendorDetail.messages.bankSaveFailed'));
  }
};

const handleDeleteBank = async (row: VendorBankInfo) => {
  try {
    await ElMessageBox.confirm(
      t('vendorDetail.messages.deleteBankConfirm', {
        name: row.bankName || row.bankAccount || row.accountName || '—'
      }),
      t('vendorDetail.messages.deleteBankTitle'),
      { type: 'warning' }
    );
    await vendorBankApi.deleteBank(row.id);
    ElMessage.success(t('vendorDetail.messages.bankDeleted'));
    await refreshBanks();
  } catch {
    // ignore
  }
};

const handleSetDefaultBank = async (row: VendorBankInfo) => {
  try {
    await vendorBankApi.setDefaultBank(row.id);
    ElMessage.success(t('vendorDetail.messages.defaultBankSet'));
    await refreshBanks();
  } catch {
    ElMessage.error(t('vendorDetail.messages.defaultBankFailed'));
  }
};

onMounted(() => {
  void vendorDict.ensureLoaded();
  void fetchVendor();
  void fetchVendorTags();
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
  &.status--danger {
    background: rgba(201, 87, 69, 0.15);
    color: $color-red-brown;
    border: 1px solid rgba(201, 87, 69, 0.35);
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

  // 辅助动作（设为默认联系人 / 设为默认地址等）：中性样式，非 primary/danger
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
