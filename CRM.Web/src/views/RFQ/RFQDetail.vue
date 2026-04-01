<template>
  <div class="rfq-detail-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
          {{ t('rfqDetail.back') }}
        </button>
        <div class="rfq-title-group">
          <div class="rfq-avatar-lg">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <circle cx="12" cy="12" r="10"/><path d="M12 8v4l3 3"/>
            </svg>
          </div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title">{{ rfq?.rfqCode || t('rfqDetail.title') }}</h1>
                <button
                  v-if="rfq"
                  type="button"
                  class="btn-favorite-star"
                  :class="{ 'is-favorite': rfqFavorited }"
                  :disabled="favoriteLoading"
                  :title="rfqFavorited ? t('rfqDetail.unfavorite') : t('rfqDetail.favorite')"
                  :aria-label="rfqFavorited ? t('rfqDetail.unfavorite') : t('rfqDetail.favorite')"
                  :aria-pressed="rfqFavorited"
                  @click="toggleFavorite"
                >
                  <svg
                    v-if="!rfqFavorited"
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
            </div>
            <div class="title-meta">
              <span class="rfq-code">{{ rfq?.customerName }}</span>
              <span class="status-badge" :class="`status-${rfq?.status}`">{{ getStatusLabel(rfq?.status) }}</span>
              <span class="source-tag">{{ getSourceLabel(rfq?.source) }}</span>
            </div>
          </div>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="handleEdit" v-if="rfq?.status === 0 || rfq?.status === 1">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
          </svg>
          {{ t('rfqDetail.edit') }}
        </button>
        <button class="btn-warning" @click="showCloseDialog" v-if="rfq?.status !== 7 && rfq?.status !== 8">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/>
          </svg>
          {{ t('rfqDetail.closeRfq') }}
        </button>
        <el-dropdown
          trigger="click"
          placement="bottom-end"
          popper-class="rfq-detail-header-more-popper"
          @command="onHeaderMoreCommand"
        >
          <button type="button" class="btn-more-actions" :title="t('rfqDetail.more')" :aria-label="t('rfqDetail.more')">
            <span class="btn-more-actions__dots" aria-hidden="true">⋯</span>
          </button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="delete" class="detail-more-item--danger">{{ t('rfqDetail.deleteRfq') }}</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </div>

    <div v-loading="loading" element-loading-background="rgba(10,22,40,0.8)" class="detail-content">
      <template v-if="rfq">
        <!-- 基础信息 -->
        <div class="info-section">
          <div class="section-header">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('rfqDetail.sections.basic') }}</span>
          </div>
          <div class="info-grid">
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.rfqCode') }}</span>
              <span class="info-value info-value--code">{{ rfq.rfqCode || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.customer') }}</span>
              <span class="info-value">{{ rfq.customerName || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.contact') }}</span>
              <span class="info-value">{{ rfq.contactPersonName || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.contactEmail') }}</span>
              <span class="info-value">{{ rfq.contactPersonEmail || (rfq as any).contactEmail || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.salesUser') }}</span>
              <span class="info-value">{{ rfq.salesUserName || '—' }}</span>
            </div>
          </div>
        </div>

        <!-- 需求信息 -->
        <div class="info-section">
          <div class="section-header">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('rfqDetail.sections.rfq') }}</span>
          </div>
          <div class="info-grid">
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.rfqType') }}</span>
              <span class="info-value">{{ getRFQTypeLabel(rfq.rfqType) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.quoteMethod') }}</span>
              <span class="info-value">{{ getQuoteMethodLabel(rfq.quoteMethod) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.assignMethod') }}</span>
              <span class="info-value">{{ getAssignMethodLabel(rfq.assignMethod) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.industry') }}</span>
              <span class="info-value">{{ rfq.industry || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.product') }}</span>
              <span class="info-value">{{ rfq.product || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.targetType') }}</span>
              <span class="info-value">{{ getTargetTypeLabel(rfq.targetType) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.importance') }}</span>
              <span class="info-value">{{ rfq.importanceLevel ?? (rfq as any).importance ?? '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.lastInquiry') }}</span>
              <span class="info-value">{{ ((rfq as any).isLastInquiry ?? rfq.isLastQuote) ? t('rfqDetail.yes') : t('rfqDetail.no') }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.source') }}</span>
              <span class="info-value">{{ getSourceLabel(rfq.source) }}</span>
            </div>
            <div class="info-item" v-if="rfq.projectBackground">
              <span class="info-label">{{ t('rfqDetail.fields.projectBackground') }}</span>
              <span class="info-value">{{ rfq.projectBackground }}</span>
            </div>
            <div class="info-item" v-if="rfq.competitor">
              <span class="info-label">{{ t('rfqDetail.fields.competitor') }}</span>
              <span class="info-value">{{ rfq.competitor }}</span>
            </div>
            <div class="info-item" v-if="rfq.remark" style="grid-column: span 3">
              <span class="info-label">{{ t('rfqDetail.fields.remark') }}</span>
              <span class="info-value">{{ rfq.remark }}</span>
            </div>
          </div>
        </div>

        <!-- 采购员分配信息 -->
        <div class="info-section" v-if="rfq.purchaserName">
          <div class="section-header">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('rfqDetail.sections.purchaser') }}</span>
          </div>
          <div class="info-grid">
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.currentPurchaser') }}</span>
              <span class="info-value">{{ rfq.purchaserName }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.assignedAt') }}</span>
              <span class="info-value info-value--time">{{ formatDate(rfq.assignedAt) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.purchaserStatus') }}</span>
              <span class="info-value">{{ getPurchaserStatusLabel(rfq.purchaserStatus) }}</span>
            </div>
          </div>
        </div>

        <!-- 标签页 -->
        <div class="tabs-section">
          <div class="tabs-nav">
            <button
              v-for="tab in tabs"
              :key="tab.key"
              :class="['tab-btn', { 'tab-btn--active': activeTab === tab.key }]"
              @click="activeTab = tab.key"
            >
              {{ tab.label }}
              <span v-if="tab.count !== undefined" class="tab-count">{{ tab.count }}</span>
            </button>
          </div>
          <div class="tabs-body">
            <!-- 需求明细 -->
            <div v-if="activeTab === 'items'">
              <div class="tab-toolbar">
                <span class="cell-muted">{{ t('rfqDetail.itemsCount', { count: rfqItems.length }) }}</span>
                <div class="tab-toolbar__actions">
                  <el-radio-group v-model="itemsViewMode" size="small" class="items-view-toggle">
                    <el-radio-button label="list">{{ t('rfqDetail.list') }}</el-radio-button>
                    <el-radio-button label="panel">{{ t('rfqDetail.panel') }}</el-radio-button>
                  </el-radio-group>
                  <button
                    v-if="showAssignPurchaserToolbar"
                    type="button"
                    class="btn-add-item"
                    @click="showAssignDialog"
                  >
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>
                    </svg>
                    {{ t('rfqDetail.assignPurchaser') }}
                  </button>
                  <button type="button" class="btn-add-item" @click="loadItems">
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-3.51"/>
                    </svg>
                    {{ t('rfqDetail.refreshBestQuote') }}
                  </button>
                </div>
              </div>
              <div
                v-if="itemsViewMode === 'panel'"
                v-loading="itemsLoading"
                element-loading-background="rgba(10,22,40,0.8)"
                class="items-panel-wrap"
              >
                <div v-if="rfqItems.length === 0" class="empty-state empty-state--inline">
                  <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" opacity="0.3">
                    <rect x="3" y="4" width="18" height="16" rx="2"/>
                    <path d="M7 8h10M7 12h6"/>
                  </svg>
                  <p>{{ t('rfqDetail.noItems') }}</p>
                </div>
                <div v-else class="items-panel-list">
                  <div
                    v-for="(row, idx) in rfqItems"
                    :key="itemRowKey(row, idx)"
                    class="item-panel-card"
                  >
                    <div class="item-panel-card__head">
                      <span class="item-panel-card__idx">{{ t('rfqDetail.itemN', { n: idx + 1 }) }}</span>
                    </div>
                    <el-row :gutter="16" class="item-panel-row">
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <div class="item-panel-field__label">客户物料型号</div>
                          <div class="item-panel-field__value cell-secondary">
                            {{ row.customerMaterialModel || (row as any).customerMpn || '—' }}
                          </div>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <div class="item-panel-field__label">物料型号</div>
                          <div class="item-panel-field__value item-panel-field__value--code">
                            {{ row.materialModel || (row as any).mpn || '—' }}
                          </div>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <div class="item-panel-field__label">客户品牌</div>
                          <div class="item-panel-field__value cell-secondary">{{ row.customerBrand || '—' }}</div>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <div class="item-panel-field__label">品牌</div>
                          <div class="item-panel-field__value cell-primary">{{ row.brand || '—' }}</div>
                        </div>
                      </el-col>
                    </el-row>
                    <el-row :gutter="16" class="item-panel-row">
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <div class="item-panel-field__label">目标价</div>
                          <div class="item-panel-field__value cell-secondary">
                            {{ row.targetPrice ? `${row.currency || 'RMB'} ${row.targetPrice}` : '—' }}
                          </div>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <div class="item-panel-field__label">数量</div>
                          <div class="item-panel-field__value cell-secondary">{{ row.quantity ?? '—' }}</div>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <div class="item-panel-field__label">生产日期</div>
                          <div class="item-panel-field__value cell-muted">{{ row.productionDate || '—' }}</div>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <div class="item-panel-field__label">失效日期</div>
                          <div class="item-panel-field__value cell-muted">{{ formatDate(row.expiryDate) }}</div>
                        </div>
                      </el-col>
                    </el-row>
                    <el-row :gutter="16" class="item-panel-row">
                      <el-col :xs="24" :sm="12" :md="12">
                        <div class="item-panel-field">
                          <div class="item-panel-field__label">询价采购员</div>
                          <div class="item-panel-field__value cell-secondary">{{ formatAssignedPurchasers(row) }}</div>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <div class="item-panel-field__label">最小起订量</div>
                          <div class="item-panel-field__value cell-muted">{{ row.minOrderQty ?? '—' }}</div>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <div class="item-panel-field__label">状态</div>
                          <div class="item-panel-field__value">
                            <span :class="['status-badge', `status-${row.status}`]">{{ getStatusLabel(row.status) }}</span>
                          </div>
                        </div>
                      </el-col>
                    </el-row>
                  </div>
                </div>
              </div>
              <CrmDataTable
                v-else
                :data="rfqItems"
                v-loading="itemsLoading"
                class="quantum-table crm-data-table"
                :header-cell-style="headerCellStyle"
                :cell-style="cellStyle"
              >
                <el-table-column type="index" label="#" width="50" align="center">
                  <template #default="{ $index }"><span class="cell-muted">{{ $index + 1 }}</span></template>
                </el-table-column>
                <el-table-column label="客户物料型号" width="160">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.customerMaterialModel || (row as any).customerMpn || '—' }}</span></template>
                </el-table-column>
                <el-table-column label="物料型号" min-width="160">
                  <template #default="{ row }"><span class="cell-code">{{ row.materialModel || (row as any).mpn || '—' }}</span></template>
                </el-table-column>
                <el-table-column label="客户品牌" width="110">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.customerBrand || '—' }}</span></template>
                </el-table-column>
                <el-table-column label="品牌" width="130">
                  <template #default="{ row }"><span class="cell-primary">{{ row.brand || '—' }}</span></template>
                </el-table-column>
                <el-table-column label="目标价" width="110" align="right">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.targetPrice ? `${row.currency || 'RMB'} ${row.targetPrice}` : '—' }}</span></template>
                </el-table-column>
                <el-table-column label="数量" width="90" align="right">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.quantity }}</span></template>
                </el-table-column>
                <el-table-column label="询价采购员" min-width="150" show-overflow-tooltip>
                  <template #default="{ row }"><span class="cell-secondary">{{ formatAssignedPurchasers(row) }}</span></template>
                </el-table-column>
                <el-table-column label="生产日期" width="100">
                  <template #default="{ row }"><span class="cell-muted">{{ row.productionDate || '—' }}</span></template>
                </el-table-column>
                <el-table-column label="失效日期" width="110">
                  <template #default="{ row }"><span class="cell-muted">{{ formatDate(row.expiryDate) }}</span></template>
                </el-table-column>
                <el-table-column label="最小起订量" width="100" align="right">
                  <template #default="{ row }"><span class="cell-muted">{{ row.minOrderQty ?? '—' }}</span></template>
                </el-table-column>
                <el-table-column label="状态" width="90" align="center">
                  <template #default="{ row }">
                    <span :class="['status-badge', `status-${row.status}`]">{{ getStatusLabel(row.status) }}</span>
                  </template>
                </el-table-column>
                <el-table-column
                  v-if="canAssignRfqPurchaser"
                  label="操作"
                  width="100"
                  min-width="88"
                  align="center"
                  fixed="right"
                  class-name="op-col"
                  label-class-name="op-col"
                >
                  <template #default>
                    <div v-if="rfqClosedForAssign" class="cell-muted">—</div>
                    <div v-else @click.stop @dblclick.stop>
                      <el-dropdown trigger="click" placement="bottom-end">
                        <div class="op-more-dropdown-trigger">
                          <button type="button" class="op-more-trigger">...</button>
                        </div>
                        <template #dropdown>
                          <el-dropdown-menu>
                            <el-dropdown-item @click.stop="showAssignDialog">
                            <span class="op-more-item op-more-item--primary">{{ t('rfqDetail.assignPurchaser') }}</span>
                            </el-dropdown-item>
                          </el-dropdown-menu>
                        </template>
                      </el-dropdown>
                    </div>
                  </template>
                </el-table-column>
              </CrmDataTable>
            </div>

            <!-- 关闭记录 -->
            <div v-if="activeTab === 'closeRecords'">
              <div v-if="closeRecords.length === 0" class="empty-state">
                <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" opacity="0.3">
                  <path d="M9 11l3 3L22 4"/><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/>
                </svg>
                <p>{{ t('rfqDetail.noCloseRecords') }}</p>
              </div>
              <CrmDataTable v-else :data="closeRecords" class="quantum-table" :header-cell-style="headerCellStyle" :cell-style="cellStyle">
                <el-table-column label="关闭类型" width="120">
                  <template #default="{ row }"><span class="cell-secondary">{{ getCloseTypeLabel(row.closeType) }}</span></template>
                </el-table-column>
                <el-table-column label="关闭原因" min-width="200">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.reason || '—' }}</span></template>
                </el-table-column>
                <el-table-column label="操作人" width="120">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.operatorName || '—' }}</span></template>
                </el-table-column>
                <el-table-column label="关闭时间" width="160">
                  <template #default="{ row }"><span class="cell-muted">{{ formatCloseAt(row.createdAt) }}</span></template>
                </el-table-column>
              </CrmDataTable>
            </div>
          </div>
        </div>
      </template>
    </div>

    <!-- 分配采购员弹窗 -->
    <el-dialog v-model="assignDialogVisible" :title="t('rfqDetail.assignPurchaser')" width="480px" :close-on-click-modal="false">
      <div v-if="recommendedPurchaser" class="recommend-card">
        <div class="recommend-avatar">{{ recommendedPurchaser.name?.charAt(0) }}</div>
        <div>
          <div class="recommend-name">{{ recommendedPurchaser.name }}</div>
          <div class="recommend-meta">{{ t('rfqDetail.recommendMeta', { count: recommendedPurchaser.handlingCount ?? 0 }) }}</div>
        </div>
        <button class="btn-use-recommend" @click="assignForm.purchaserId = recommendedPurchaser.id">{{ t('rfqDetail.useRecommend') }}</button>
      </div>
      <el-form :model="assignForm" label-width="90px" style="margin-top: 16px;">
        <el-form-item :label="t('rfqDetail.fields.purchaser')" required>
          <PurchaserCascader
            v-model="assignForm.purchaserId"
            :placeholder="t('rfqDetail.selectPurchaser')"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item :label="t('rfqDetail.fields.remark')">
          <el-input v-model="assignForm.remark" type="textarea" :rows="2" :placeholder="t('rfqDetail.assignRemark')" />
        </el-form-item>
      </el-form>
      <template #footer>
        <button class="btn-secondary" @click="assignDialogVisible = false">{{ t('common.cancel') }}</button>
        <button class="btn-primary" :disabled="assignLoading" @click="handleAssignConfirm" style="margin-left: 8px;">
          {{ assignLoading ? t('rfqDetail.assigning') : t('rfqDetail.confirmAssign') }}
        </button>
      </template>
    </el-dialog>

    <!-- 关闭需求弹窗 -->
    <el-dialog v-model="closeDialogVisible" :title="t('rfqDetail.closeRfq')" width="420px" :close-on-click-modal="false">
      <el-form :model="closeForm" label-width="90px">
        <el-form-item :label="t('rfqDetail.fields.closeType')" required>
          <el-select v-model="closeForm.closeType" :placeholder="t('rfqDetail.select')" style="width: 100%">
            <el-option :label="t('rfqDetail.closeType.normal')" :value="1" />
            <el-option :label="t('rfqDetail.closeType.customerCancel')" :value="2" />
            <el-option :label="t('rfqDetail.closeType.priceMismatch')" :value="3" />
            <el-option :label="t('rfqDetail.closeType.other')" :value="9" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('rfqDetail.fields.closeReason')" required>
          <el-input v-model="closeForm.reason" type="textarea" :rows="3" :placeholder="t('rfqDetail.closeReasonPlaceholder')" />
        </el-form-item>
      </el-form>
      <template #footer>
        <button class="btn-secondary" @click="closeDialogVisible = false">{{ t('common.cancel') }}</button>
        <button class="btn-primary" :disabled="closeLoading" @click="handleCloseConfirm" style="margin-left: 8px;">
          {{ closeLoading ? t('rfqDetail.closing') : t('rfqDetail.confirmClose') }}
        </button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElNotification, ElMessageBox } from 'element-plus'
import { rfqApi } from '@/api/rfq'
import { favoriteApi } from '@/api/favorite'
import { RFQ_FAVORITE_ENTITY_TYPE, RFQ_FAVORITES_CHANGED_EVENT } from '@/constants/rfqFavorites'
import { canManualAssignRfqPurchaser } from '@/constants/rfqPurchaserAssign'
import { useAuthStore } from '@/stores/auth'
import { recordRfqRecentView } from '@/utils/rfqRecentHistory'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import PurchaserCascader from '@/components/PurchaserCascader.vue'
import {
  formatRfqTypeLabel as getRFQTypeLabel,
  formatQuoteMethodLabel as getQuoteMethodLabel,
  formatAssignMethodLabel as getAssignMethodLabel
} from '@/constants/rfqFormEnums'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const rfqId = route.params.id as string

const canAssignRfqPurchaser = computed(() => canManualAssignRfqPurchaser(authStore.user))
const rfqClosedForAssign = computed(() => {
  const s = rfq.value?.status
  return s === 7 || s === 8
})
/** 面板视图下无「操作」列，用工具栏提供同一入口 */
const showAssignPurchaserToolbar = computed(
  () =>
    canAssignRfqPurchaser.value &&
    itemsViewMode.value === 'panel' &&
    !rfqClosedForAssign.value
)

const loading = ref(false)
const rfqFavorited = ref(false)
const favoriteLoading = ref(false)
const rfq = ref<any>(null)
const rfqItems = ref<any[]>([])
const closeRecords = ref<any[]>([])
const itemsLoading = ref(false)
const activeTab = ref('items')
/** 需求明细：列表（默认） / 面板 */
const itemsViewMode = ref<'list' | 'panel'>('list')

const tabs = computed(() => [
  { key: 'items', label: t('rfqDetail.tabs.items'), count: rfqItems.value.length },
  { key: 'closeRecords', label: t('rfqDetail.tabs.closeRecords'), count: closeRecords.value.length }
])

const assignDialogVisible = ref(false)
const assignLoading = ref(false)
const recommendedPurchaser = ref<any>(null)
const assignForm = reactive({ purchaserId: '', remark: '' })

const closeDialogVisible = ref(false)
const closeLoading = ref(false)
const closeForm = reactive({ closeType: 1, reason: '' })

const headerCellStyle = {
  background: 'rgba(0, 212, 255, 0.04)',
  color: 'rgba(200, 216, 232, 0.55)',
  fontSize: '12px',
  fontWeight: '500',
  borderBottom: '1px solid rgba(0, 212, 255, 0.1)',
  borderRight: 'none',
  padding: '10px 14px',
  letterSpacing: '0.3px'
}
const cellStyle = {
  background: 'transparent',
  borderBottom: '1px solid rgba(255, 255, 255, 0.04)',
  borderRight: 'none',
  padding: '10px 14px',
  color: 'rgba(224, 244, 255, 0.85)',
  fontSize: '13px'
}

function getStatusLabel(status?: number) {
  const map: Record<number, string> = {
    0: t('rfqDetail.status.draft'), 1: t('rfqDetail.status.pending'), 2: t('rfqDetail.status.assigned'), 3: t('rfqDetail.status.processing'),
    4: t('rfqDetail.status.quoted'), 5: t('rfqDetail.status.accepted'), 6: t('rfqDetail.status.rejected'), 7: t('rfqDetail.status.closed'), 8: t('rfqDetail.status.cancelled')
  }
  return status !== undefined ? (map[status] ?? t('rfqDetail.unknown')) : t('quoteList.na')
}
function getSourceLabel(source?: number) {
  const map: Record<number, string> = { 1: t('rfqDetail.source.offline'), 2: t('rfqDetail.source.online'), 3: t('rfqDetail.source.email'), 4: t('rfqDetail.source.phone'), 5: t('rfqDetail.source.import') }
  return source !== undefined ? (map[source] ?? t('quoteList.na')) : t('quoteList.na')
}
function getTargetTypeLabel(type?: number) {
  const map: Record<number, string> = { 1: t('rfqDetail.targetType.priceCompare'), 2: t('rfqDetail.targetType.exclusive'), 3: t('rfqDetail.targetType.urgent'), 4: t('rfqDetail.targetType.normal') }
  return type !== undefined ? (map[type] ?? t('quoteList.na')) : t('quoteList.na')
}
function getPurchaserStatusLabel(status?: number) {
  const map: Record<number, string> = { 0: t('rfqDetail.purchaserStatus.pending'), 1: t('rfqDetail.purchaserStatus.processing'), 2: t('rfqDetail.purchaserStatus.done'), 3: t('rfqDetail.purchaserStatus.rejected') }
  return status !== undefined ? (map[status] ?? t('quoteList.na')) : t('quoteList.na')
}
function getCloseTypeLabel(type?: number) {
  const map: Record<number, string> = { 1: t('rfqDetail.closeType.normal'), 2: t('rfqDetail.closeType.customerCancel'), 3: t('rfqDetail.closeType.priceMismatch'), 9: t('rfqDetail.closeType.other') }
  return type !== undefined ? (map[type] ?? t('quoteList.na')) : t('quoteList.na')
}
function formatDate(val?: string) {
  if (!val) return '—'
  const s = formatDisplayDate(val)
  return s === '--' ? '—' : s
}
function formatAssignedPurchasers(row: any) {
  const n1 = String(row.assignedPurchaserName1 ?? '').trim()
  const n2 = String(row.assignedPurchaserName2 ?? '').trim()
  const parts = [n1, n2].filter(Boolean)
  return parts.length ? parts.join('、') : '—'
}
function itemRowKey(row: any, idx: number) {
  const id = row?.id
  return id != null && id !== '' ? String(id) : `rfq-item-${idx}`
}
function formatCloseAt(val?: string) {
  if (!val) return '—'
  const s = formatDisplayDateTime(val)
  return s === '--' ? '—' : s
}
function goBack() { router.push('/rfqlist') }
function handleEdit() { router.push(`/rfqs/${rfqId}/edit`) }

function onHeaderMoreCommand(cmd: string) {
  if (cmd === 'delete') void handleDelete()
}

async function loadFavoriteState() {
  if (!rfqId) return
  try {
    rfqFavorited.value = await favoriteApi.checkFavorite(RFQ_FAVORITE_ENTITY_TYPE, rfqId)
  } catch {
    rfqFavorited.value = false
  }
}

async function toggleFavorite() {
  if (!rfqId || favoriteLoading.value) return
  favoriteLoading.value = true
  try {
    if (rfqFavorited.value) {
      await favoriteApi.removeFavorite(RFQ_FAVORITE_ENTITY_TYPE, rfqId)
      rfqFavorited.value = false
      ElNotification.success({ title: t('rfqDetail.toast.unfavoritedTitle'), message: t('rfqDetail.toast.unfavoritedMessage') })
    } else {
      await favoriteApi.addFavorite({ entityType: RFQ_FAVORITE_ENTITY_TYPE, entityId: rfqId })
      rfqFavorited.value = true
      ElNotification.success({ title: t('rfqDetail.toast.favoritedTitle'), message: t('rfqDetail.toast.favoritedMessage') })
    }
    window.dispatchEvent(new Event(RFQ_FAVORITES_CHANGED_EVENT))
  } catch {
    ElNotification.error({ title: t('rfqDetail.toast.actionFailedTitle'), message: t('rfqDetail.toast.favoriteFailedMessage') })
  } finally {
    favoriteLoading.value = false
  }
}

async function loadRFQ() {
  loading.value = true
  try {
    rfq.value = await rfqApi.getRFQDetail(rfqId)
    if (rfq.value) {
      recordRfqRecentView({
        id: rfqId,
        rfqCode: rfq.value.rfqCode,
        customerName: rfq.value.customerName
      })
    }
    await loadFavoriteState()
  } catch {
    ElNotification.error({ title: t('rfqDetail.toast.loadFailedTitle'), message: t('rfqDetail.toast.loadFailedMessage') })
  } finally {
    loading.value = false
  }
}

async function loadItems() {
  itemsLoading.value = true
  try { const res = await rfqApi.getRFQItemsWithBestQuote(rfqId); rfqItems.value = res || [] }
  catch { rfqItems.value = [] }
  finally { itemsLoading.value = false }
}

async function loadCloseRecords() {
  try { const res = await rfqApi.getCloseRecords(rfqId); closeRecords.value = res || [] }
  catch { closeRecords.value = [] }
}

async function showAssignDialog() {
  assignForm.purchaserId = ''; assignForm.remark = ''; recommendedPurchaser.value = null
  try {
    const recommended = await rfqApi.getRecommendedPurchasers(rfqId)
    const list = Array.isArray(recommended) ? recommended : recommended ? [recommended] : []
    recommendedPurchaser.value = list[0] ?? null
  } catch {
    recommendedPurchaser.value = null
  }
  assignDialogVisible.value = true
}

function showCloseDialog() {
  closeForm.closeType = 1; closeForm.reason = ''
  closeDialogVisible.value = true
}

async function handleAssignConfirm() {
  if (!assignForm.purchaserId) {
    ElNotification.warning({ title: t('rfqDetail.toast.selectPurchaserTitle'), message: t('rfqDetail.toast.selectPurchaserMessage') }); return
  }
  assignLoading.value = true
  try {
    await rfqApi.assignPurchaser(rfqId, { purchaserId: assignForm.purchaserId, remark: assignForm.remark })
    ElNotification.success({ title: t('rfqDetail.toast.assignSuccessTitle'), message: t('rfqDetail.toast.assignSuccessMessage') })
    assignDialogVisible.value = false
    await loadRFQ()
    await loadItems()
  } catch { ElNotification.error({ title: t('rfqDetail.toast.assignFailedTitle'), message: t('rfqDetail.toast.assignFailedMessage') }) }
  finally { assignLoading.value = false }
}

async function handleCloseConfirm() {
  if (!closeForm.reason) {
    ElNotification.warning({ title: t('rfqDetail.toast.closeReasonRequiredTitle'), message: t('rfqDetail.toast.closeReasonRequiredMessage') }); return
  }
  closeLoading.value = true
  try {
    await rfqApi.addCloseRecord(rfqId, { closeType: closeForm.closeType, closeReason: closeForm.reason })
    ElNotification.success({ title: t('rfqDetail.toast.actionSuccessTitle'), message: t('rfqDetail.toast.closedMessage') })
    closeDialogVisible.value = false; loadRFQ(); loadCloseRecords()
  } catch { ElNotification.error({ title: t('rfqDetail.toast.actionFailedTitle'), message: t('rfqDetail.toast.closeFailedMessage') }) }
  finally { closeLoading.value = false }
}

async function handleDelete() {
  try {
    await ElMessageBox.confirm(
      t('rfqDetail.deleteConfirm', { code: rfq.value?.rfqCode }),
      t('rfqDetail.deleteTitle'),
      { confirmButtonText: t('rfqDetail.confirmDelete'), cancelButtonText: t('common.cancel'), type: 'error' }
    )
    await rfqApi.deleteRFQ(rfqId)
    ElNotification.success({ title: t('rfqDetail.toast.deleteSuccessTitle'), message: t('rfqDetail.toast.deleteSuccessMessage') })
    router.push('/rfqlist')
  } catch { /* 取消 */ }
}

onMounted(() => { loadRFQ(); loadItems(); loadCloseRecords() })
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.rfq-detail-page {
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
  .header-left { display: flex; align-items: center; gap: 16px; }
  .header-right { display: flex; align-items: center; gap: 10px; }
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  background: rgba(255,255,255,0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  &:hover { background: rgba(255,255,255,0.07); color: $text-secondary; border-color: rgba(0,212,255,0.2); }
}

.rfq-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
}

.rfq-avatar-lg {
  width: 48px;
  height: 48px;
  background: linear-gradient(135deg, rgba(0,102,255,0.3), rgba(0,212,255,0.2));
  border: 1px solid rgba(0,212,255,0.25);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
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
  font-family: 'Space Mono', monospace;
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
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

.rfq-code {
  font-size: 13px;
  color: $text-secondary;
}

.source-tag {
  font-size: 11px;
  color: $text-muted;
  background: rgba(255,255,255,0.05);
  border: 1px solid $border-panel;
  border-radius: 4px;
  padding: 1px 6px;
}

.status-badge {
  font-size: 10px;
  padding: 2px 7px;
  border-radius: 3px;
  &.status-0 { background: rgba(107,122,141,0.2); color: #8A9BB0; border: 1px solid rgba(107,122,141,0.3); }
  &.status-1 { background: rgba(201,154,69,0.2); color: $color-amber; border: 1px solid rgba(201,154,69,0.3); }
  &.status-2 { background: rgba(50,149,201,0.2); color: $color-steel-cyan; border: 1px solid rgba(50,149,201,0.3); }
  &.status-3 { background: rgba(0,212,255,0.12); color: $cyan-primary; border: 1px solid rgba(0,212,255,0.25); }
  &.status-4 { background: rgba(70,191,145,0.15); color: $color-mint-green; border: 1px solid rgba(70,191,145,0.3); }
  &.status-5 { background: rgba(70,191,145,0.2); color: $color-mint-green; border: 1px solid rgba(70,191,145,0.4); }
  &.status-6 { background: rgba(201,87,69,0.15); color: $color-red-brown; border: 1px solid rgba(201,87,69,0.3); }
  &.status-7 { background: rgba(107,122,141,0.15); color: #6B7A8D; border: 1px solid rgba(107,122,141,0.25); }
  &.status-8 { background: rgba(107,122,141,0.1); color: #6B7A8D; border: 1px solid rgba(107,122,141,0.2); }
}

.btn-primary {
  display: inline-flex; align-items: center; gap: 6px; padding: 8px 14px;
  background: linear-gradient(135deg, rgba(0,102,255,0.8), rgba(0,212,255,0.7));
  border: 1px solid rgba(0,212,255,0.4); border-radius: $border-radius-md;
  color: #fff; font-size: 13px; font-family: 'Noto Sans SC', sans-serif; cursor: pointer; transition: all 0.2s;
  &:hover { transform: translateY(-1px); box-shadow: 0 4px 16px rgba(0,212,255,0.25); }
  &:disabled { opacity: 0.6; cursor: not-allowed; transform: none; }
}
.btn-secondary {
  display: inline-flex; align-items: center; gap: 6px; padding: 8px 14px;
  background: rgba(255,255,255,0.05); border: 1px solid $border-panel; border-radius: $border-radius-md;
  color: $text-secondary; font-size: 13px; font-family: 'Noto Sans SC', sans-serif; cursor: pointer; transition: all 0.2s;
  &:hover { background: rgba(255,255,255,0.08); border-color: rgba(0,212,255,0.25); }
}
.btn-warning {
  display: inline-flex; align-items: center; gap: 6px; padding: 8px 14px;
  background: rgba(201,154,69,0.15); border: 1px solid rgba(201,154,69,0.4); border-radius: $border-radius-md;
  color: $color-amber; font-size: 13px; font-family: 'Noto Sans SC', sans-serif; cursor: pointer; transition: all 0.2s;
  &:hover { background: rgba(201,154,69,0.25); }
}
.btn-more-actions {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  padding: 0;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  background: rgba(255, 255, 255, 0.04);
  color: $text-muted;
  cursor: pointer;
  transition: all 0.2s;
  font-family: 'Noto Sans SC', sans-serif;
  &:hover {
    background: rgba(255, 255, 255, 0.08);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
  .btn-more-actions__dots {
    font-size: 18px;
    line-height: 1;
    letter-spacing: 1px;
  }
}

// ---- 信息区块 ----
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
  width: 8px; height: 8px; border-radius: 50%;
  &--cyan { background: $cyan-primary; box-shadow: 0 0 6px rgba(0,212,255,0.6); }
}
.section-title { font-size: 14px; font-weight: 500; color: $text-primary; }

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
}
.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 16px 20px;
  border-bottom: 1px solid rgba(255,255,255,0.04);
  border-right: 1px solid rgba(255,255,255,0.04);
  &:nth-child(3n) { border-right: none; }
  .info-label { font-size: 11px; color: $text-muted; letter-spacing: 0.5px; text-transform: uppercase; }
  .info-value {
    font-size: 13px; color: $text-secondary;
    &--code { font-family: 'Space Mono', monospace; font-size: 12px; color: $color-ice-blue; }
    &--time { font-size: 12px; color: $text-muted; }
  }
}

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
  display: flex;
  align-items: center;
  gap: 6px;
  &:hover { color: $text-secondary; }
  &--active { color: $cyan-primary; border-bottom-color: $cyan-primary; }
}
.tab-count {
  display: inline-block;
  padding: 0 6px;
  background: rgba(0,212,255,0.1);
  border: 1px solid rgba(0,212,255,0.2);
  border-radius: 10px;
  font-size: 11px;
  color: $cyan-primary;
  font-family: 'Space Mono', monospace;
}
.tabs-body { padding: 20px; }
.tab-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
  margin-bottom: 14px;
}
.tab-toolbar__actions {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  margin-left: auto;
}
.items-view-toggle {
  :deep(.el-radio-button__inner) {
    background: rgba(0, 0, 0, 0.2);
    border-color: $border-panel;
    color: $text-muted;
    font-size: 12px;
    padding: 5px 12px;
  }
  :deep(.el-radio-button__original-radio:checked + .el-radio-button__inner) {
    background: rgba(0, 212, 255, 0.12);
    border-color: rgba(0, 212, 255, 0.45);
    color: $cyan-primary;
    box-shadow: none;
  }
}
.items-panel-wrap {
  min-height: 80px;
}
.items-panel-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.item-panel-card {
  background: rgba(0, 0, 0, 0.15);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  padding: 14px 16px 16px;
}
.item-panel-card__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}
.item-panel-card__idx {
  font-size: 12px;
  font-weight: 600;
  color: rgba(200, 216, 232, 0.75);
  letter-spacing: 0.3px;
}
.item-panel-row {
  margin-bottom: 4px;
  &:last-child {
    margin-bottom: 0;
  }
}
.item-panel-field {
  margin-bottom: 10px;
  min-width: 0;
}
.item-panel-field__label {
  font-size: 12px;
  color: $text-muted;
  margin-bottom: 4px;
  line-height: 1.3;
}
.item-panel-field__value {
  font-size: 13px;
  line-height: 1.45;
  word-break: break-word;
}
.item-panel-field__value--code {
  font-family: 'Space Mono', monospace;
  font-size: 12px;
  color: $color-ice-blue;
}
.empty-state--inline {
  padding: 32px 16px;
  margin: 0;
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
  // 无外边框，行间细线分隔，对标客户管理列表风格
  --el-table-border-color: transparent;
  --el-table-header-bg-color: rgba(0, 212, 255, 0.04);
  --el-table-row-hover-bg-color: rgba(0, 212, 255, 0.04);
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-fixed-box-shadow: none;
  width: 100%;
  background: transparent !important;
  :deep(.el-table__inner-wrapper) {
    background: transparent;
    &::before { display: none !important; }
    &::after  { display: none !important; }
  }
  :deep(.el-table__border-left-patch) { display: none !important; }
  :deep(.el-table__header-wrapper) {
    th.el-table__cell {
      background: rgba(0, 212, 255, 0.04) !important;
      border-bottom: 1px solid rgba(0, 212, 255, 0.1) !important;
      border-right: none !important;
      color: rgba(200, 216, 232, 0.55);
      font-size: 12px;
      font-weight: 500;
      letter-spacing: 0.3px;
    }
  }
  :deep(.el-table__row) {
    background: transparent !important;
    td.el-table__cell {
      background: transparent !important;
      border-bottom: 1px solid rgba(255, 255, 255, 0.04) !important;
      border-right: none !important;
      color: rgba(224, 244, 255, 0.85);
      font-size: 13px;
    }
    &:last-child td.el-table__cell { border-bottom: none !important; }
    &:hover td.el-table__cell { background: rgba(0, 212, 255, 0.04) !important; }
  }
  :deep(.el-table__cell) {
    .el-button { white-space: nowrap !important; }
    .cell { white-space: nowrap; }
  }
}
.cell-primary   { color: $text-primary; font-size: 13px; }
.cell-secondary { color: $text-secondary; font-size: 13px; }
.cell-muted     { color: $text-muted; font-size: 12px; }
.cell-code      { font-family: 'Space Mono', monospace; font-size: 12px; color: $color-ice-blue; }

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  padding: 48px 0;
  color: $text-muted;
  font-size: 13px;
}

// ---- 推荐采购员卡片 ----
.recommend-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  background: rgba(0,212,255,0.05);
  border: 1px solid rgba(0,212,255,0.15);
  border-radius: $border-radius-md;
  margin-bottom: 8px;
}
.recommend-avatar {
  width: 36px; height: 36px;
  background: linear-gradient(135deg, rgba(0,102,255,0.3), rgba(0,212,255,0.2));
  border: 1px solid rgba(0,212,255,0.2);
  border-radius: 8px;
  display: flex; align-items: center; justify-content: center;
  font-size: 14px; font-weight: 600; color: $cyan-primary; flex-shrink: 0;
}
.recommend-name { font-size: 13px; color: $text-primary; font-weight: 500; }
.recommend-meta { font-size: 11px; color: $text-muted; margin-top: 2px; }
.btn-use-recommend {
  margin-left: auto;
  padding: 5px 10px;
  background: rgba(0,212,255,0.1);
  border: 1px solid rgba(0,212,255,0.25);
  border-radius: $border-radius-md;
  color: $cyan-primary;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.15s;
  &:hover { background: rgba(0,212,255,0.18); }
}
</style>

<style lang="scss">
@import '@/assets/styles/variables.scss';

.rfq-detail-header-more-popper.el-dropdown__popper,
.rfq-detail-header-more-popper.el-popper {
  background: $layer-2 !important;
  border: 1px solid rgba(0, 212, 255, 0.15) !important;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.45) !important;
}
.rfq-detail-header-more-popper .el-dropdown-menu {
  background: transparent !important;
  border: none !important;
  box-shadow: none !important;
  padding: 4px 0 !important;
}
.rfq-detail-header-more-popper .el-dropdown-menu__item {
  color: rgba(200, 220, 240, 0.92) !important;
  font-size: 13px;
  &:hover,
  &:focus {
    background: rgba(0, 212, 255, 0.1) !important;
    color: #e8f4ff !important;
  }
}
.rfq-detail-header-more-popper .detail-more-item--danger {
  color: rgba(245, 108, 108, 0.95) !important;
  &:hover,
  &:focus {
    background: rgba(245, 108, 108, 0.12) !important;
    color: #ff9a9a !important;
  }
}
</style>
