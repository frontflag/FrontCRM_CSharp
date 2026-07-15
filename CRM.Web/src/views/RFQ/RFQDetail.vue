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
                <h1 class="page-title">
                  <template v-if="rfq?.rfqCode">{{ t('rfqDetail.rfqCodePrefix') }} {{ rfq.rfqCode }}</template>
                  <template v-else>{{ t('rfqDetail.title') }}</template>
                </h1>
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
                <template v-if="showRfqTagsSection && (rfqTags.length || canEditRfqTags)">
                  <div class="rfq-header-tags-row tags-row">
                    <TagListDisplay v-if="rfqTags.length" :tags="rfqTags" />
                    <button v-if="canEditRfqTags" type="button" class="btn-secondary rfq-header-add-tag-btn" @click="tagDialogVisible = true">
                      <span class="rfq-header-add-tag-icon" aria-hidden="true">±</span>
                      {{ t('rfqDetail.tags.add') }}
                    </button>
                  </div>
                </template>
                <span v-if="hasRfqSourceLabel" class="source-tag">{{ getSourceLabel(rfq?.source) }}</span>
              </div>
            </div>
            <div class="title-meta title-meta--caption rfq-header-meta-row">
              <el-tag v-if="rfq" effect="dark" :type="getStatusType(rfq.status)" size="small">
                {{ getStatusLabel(rfq.status) }}
              </el-tag>
              <div v-if="rfqItemQuoteStatPills.length" class="item-quote-stats-bar item-quote-stats-bar--header">
                <template v-for="(pill, idx) in rfqItemQuoteStatPills" :key="pill.key">
                  <span v-if="idx > 0" class="item-quote-stats__sep" aria-hidden="true">·</span>
                  <span class="item-quote-stats__pill" :class="pill.class">{{ pill.label }}</span>
                </template>
              </div>
            </div>
          </div>
        </div>
      </div>
      <div class="header-right">
        <button
          class="btn-secondary btn-close-rfq"
          @click="showCloseDialog"
          v-if="canWriteSaleData && rfq?.status !== 7 && rfq?.status !== 8"
        >
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/>
          </svg>
          {{ t('rfqDetail.closeRfq') }}
        </button>
        <button
          class="btn-primary"
          @click="handleEdit"
          v-if="canWriteSaleData && (rfq?.status === 0 || rfq?.status === 1)"
        >
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
          </svg>
          {{ t('rfqDetail.edit') }}
        </button>
        <el-dropdown
          v-if="canWriteSaleData"
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
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('rfqDetail.sections.basic') }}</span>
            </div>
            <div class="section-header__meta">
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('rfqDetail.fields.createDate') }}</span>
                <span class="section-header-meta-item__value">{{ rfqBasicCreateDateText }}</span>
              </span>
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('rfqDetail.fields.createUser') }}</span>
                <span class="section-header-meta-item__value">{{ rfqBasicCreateUserText }}</span>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
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
            <div class="info-item info-item--basic-full-row">
              <span class="info-label">{{ t('rfqDetail.fields.salesUser') }}</span>
              <span class="info-value">{{ rfq.salesUserName || '—' }}</span>
            </div>
          </div>
        </div>

        <!-- 需求信息 -->
        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('rfqDetail.sections.rfq') }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels">
            <!-- 第 1 行：需求类型 · 目标类型 · 分配方式 -->
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.rfqType') }}</span>
              <span class="info-value">{{ getRFQTypeLabel(rfq.rfqType) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.targetType') }}</span>
              <span class="info-value">{{ getTargetTypeLabel(rfq.targetType) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.assignMethod') }}</span>
              <span class="info-value">{{ getAssignMethodLabel(rfq.assignMethod) }}</span>
            </div>
            <!-- 第 2 行：行业 · 产品 · 来源 -->
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.industry') }}</span>
              <span class="info-value">{{ rfq.industry || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.product') }}</span>
              <span class="info-value">{{ rfq.product || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.source') }}</span>
              <span class="info-value">{{ getSourceLabel(rfq.source) }}</span>
            </div>
            <!-- 第 3 行：重要程度 · 报价方式 · 最后一次询价 -->
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.importance') }}</span>
              <span class="info-value info-value--importance">
                <el-rate
                  v-if="rfqImportanceStars != null"
                  :model-value="rfqImportanceStars"
                  disabled
                  :max="3"
                  :colors="[...RFQ_IMPORTANCE_RATE_COLORS]"
                  :void-color="RFQ_IMPORTANCE_RATE_VOID_COLOR"
                />
                <template v-else>—</template>
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.quoteMethod') }}</span>
              <span class="info-value">{{ getQuoteMethodLabel(rfq.quoteMethod) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('rfqDetail.fields.lastInquiry') }}</span>
              <span class="info-value">{{ ((rfq as any).isLastInquiry ?? rfq.isLastQuote) ? t('rfqDetail.yes') : t('rfqDetail.no') }}</span>
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
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('rfqDetail.sections.purchaser') }}</span>
            </div>
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
              {{ formatRfqDetailTabLabel(tab.label, tab.key) }}
            </button>
          </div>
          <div class="tabs-body">
            <!-- 需求明细 -->
            <div v-if="activeTab === 'items'">
              <div class="tab-toolbar">
                <div class="tab-toolbar__actions">
                  <el-radio-group v-model="itemsViewMode" size="small" class="items-view-toggle">
                    <el-radio-button label="list">{{ t('rfqDetail.list') }}</el-radio-button>
                    <el-radio-button label="panel">{{ t('rfqDetail.panel') }}</el-radio-button>
                  </el-radio-group>
                  <button
                    v-if="showAssignPurchaserToolbar"
                    type="button"
                    class="btn-add-item"
                    @click="showAssignDialog()"
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
                    <div class="item-panel-card__body">
                    <el-row :gutter="16" class="item-panel-row">
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <span class="item-panel-field__label">状态</span>
                          <span class="item-panel-field__value">
                            <el-tag size="small" effect="dark" :type="itemStatusTagType(effectiveItemLineStatus(row))">
                              {{ itemStatusText(effectiveItemLineStatus(row)) }}
                            </el-tag>
                          </span>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <span class="item-panel-field__label">询价采购员</span>
                          <span class="item-panel-field__value cell-secondary">{{ formatAssignedPurchasers(row) }}</span>
                        </div>
                      </el-col>
                      <el-col :xs="0" :sm="0" :md="6" class="item-panel-field-spacer" aria-hidden="true" />
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <span class="item-panel-field__label">失效日期</span>
                          <span class="item-panel-field__value cell-muted">{{ formatDate(row.expiryDate) }}</span>
                        </div>
                      </el-col>
                    </el-row>
                    <el-row :gutter="16" class="item-panel-row">
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <span class="item-panel-field__label">物料型号</span>
                          <span class="item-panel-field__value item-panel-field__value--code">
                            {{ row.materialModel || (row as any).mpn || '—' }}
                          </span>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <span class="item-panel-field__label">品牌</span>
                          <span class="item-panel-field__value cell-primary">{{ row.brand || '—' }}</span>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <span class="item-panel-field__label">客户物料型号</span>
                          <span class="item-panel-field__value cell-secondary">
                            {{ row.customerMaterialModel || (row as any).customerMpn || '—' }}
                          </span>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <span class="item-panel-field__label">客户品牌</span>
                          <span class="item-panel-field__value cell-secondary">{{ row.customerBrand || '—' }}</span>
                        </div>
                      </el-col>
                    </el-row>
                    <el-row :gutter="16" class="item-panel-row">
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <span class="item-panel-field__label">目标价</span>
                          <span class="item-panel-field__value cell-secondary amount-with-code">
                            <span>{{ formatRfqItemTargetPriceNumber(row as Record<string, unknown>) }}</span>
                            <span
                              v-if="formatRfqItemTargetPriceNumber(row as Record<string, unknown>) !== '—'"
                              :class="['dock-tier-ccy', formatRfqItemTargetCurrencyClass(row as Record<string, unknown>)]"
                            >
                              {{ formatRfqItemTargetCurrency(row as Record<string, unknown>) }}
                            </span>
                          </span>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <span class="item-panel-field__label">数量</span>
                          <span class="item-panel-field__value cell-secondary">{{ row.quantity ?? '—' }}</span>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <span class="item-panel-field__label">最小起订量</span>
                          <span class="item-panel-field__value cell-muted">{{ row.minOrderQty ?? '—' }}</span>
                        </div>
                      </el-col>
                      <el-col :xs="24" :sm="12" :md="6">
                        <div class="item-panel-field">
                          <span class="item-panel-field__label">生产日期</span>
                          <span class="item-panel-field__value cell-muted">{{ fmtProductionDate(row.productionDate) }}</span>
                        </div>
                      </el-col>
                    </el-row>
                    </div>
                  </div>
                </div>
              </div>
              <div v-else class="detail-items-table-wrap">
              <CrmDataTable
                :data="rfqItems"
                v-loading="itemsLoading"
                size="small"
                stripe
                class="items-table detail-panel-list-table"
              >
                <el-table-column label="状态" width="112" min-width="112" align="center">
                  <template #default="{ row }">
                    <el-tag size="small" effect="dark" :type="itemStatusTagType(effectiveItemLineStatus(row))">
                      {{ itemStatusText(effectiveItemLineStatus(row)) }}
                    </el-tag>
                  </template>
                </el-table-column>
                <el-table-column type="index" label="#" width="50" align="center">
                  <template #default="{ $index }"><span class="cell-muted">{{ $index + 1 }}</span></template>
                </el-table-column>
                <el-table-column label="物料型号" min-width="160">
                  <template #default="{ row }"><CrmListCopyableTextCell :text="pickCrmCopyableRowField(row, 'materialModel')" /></template>
                </el-table-column>
                <el-table-column label="品牌" width="130">
                  <template #default="{ row }"><CrmListCopyableTextCell :text="pickCrmCopyableRowField(row, 'brand')" /></template>
                </el-table-column>
                <el-table-column label="客户物料型号" width="160">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.customerMaterialModel || (row as any).customerMpn || '—' }}</span></template>
                </el-table-column>
                <el-table-column label="客户品牌" width="110">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.customerBrand || '—' }}</span></template>
                </el-table-column>
                <el-table-column label="目标价" width="110" align="right">
                  <template #default="{ row }">
                    <span class="cell-secondary amount-with-code">
                      <span>{{ formatRfqItemTargetPriceNumber(row as Record<string, unknown>) }}</span>
                      <span
                        v-if="formatRfqItemTargetPriceNumber(row as Record<string, unknown>) !== '—'"
                        :class="['dock-tier-ccy', formatRfqItemTargetCurrencyClass(row as Record<string, unknown>)]"
                      >
                        {{ formatRfqItemTargetCurrency(row as Record<string, unknown>) }}
                      </span>
                    </span>
                  </template>
                </el-table-column>
                <el-table-column label="数量" width="90" align="right">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.quantity }}</span></template>
                </el-table-column>
                <el-table-column label="询价采购员" min-width="150" show-overflow-tooltip>
                  <template #default="{ row }"><span class="cell-secondary">{{ formatAssignedPurchasers(row) }}</span></template>
                </el-table-column>
                <el-table-column label="生产日期" width="112" min-width="112">
                  <template #default="{ row }"><span class="cell-muted">{{ fmtProductionDate(row.productionDate) }}</span></template>
                </el-table-column>
                <el-table-column label="失效日期" width="110">
                  <template #default="{ row }"><span class="cell-muted">{{ formatDate(row.expiryDate) }}</span></template>
                </el-table-column>
                <el-table-column label="最小起订量" width="128" min-width="128" align="right">
                  <template #default="{ row }"><span class="cell-muted">{{ row.minOrderQty ?? '—' }}</span></template>
                </el-table-column>
                <el-table-column
                  v-if="canAssignRfqPurchaser"
                  label="操作"
                  :width="rfqDetailAssignOpColWidth"
                  :min-width="rfqDetailAssignOpColMinWidth"
                  align="center"
                  fixed="right"
                  class-name="op-col"
                  label-class-name="op-col"
                >
                  <template #header>
                    <div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="rfqDetailAssignOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="toggleRfqDetailAssignOpCol"
            >
              {{ rfqDetailAssignOpColExpanded ? '>' : '<' }}
            </button>
          </div>
                  </template>
                  <template #default="{ row, $index }">
                    <div v-if="rfqClosedForAssign" class="cell-muted">—</div>
                    <div v-else @click.stop @dblclick.stop>
                      <div v-if="rfqDetailAssignOpColExpanded" class="action-btns">
                        <button type="button" class="action-btn action-btn--primary" @click.stop="showAssignDialog(row, $index)">
                          {{ t('rfqDetail.assignPurchaser') }}
                        </button>
                      </div>
                      <el-dropdown v-else trigger="click" placement="bottom-end">
                        <div class="op-more-dropdown-trigger">
                          <button type="button" class="op-more-trigger">...</button>
                        </div>
                        <template #dropdown>
                          <el-dropdown-menu>
                            <el-dropdown-item @click.stop="showAssignDialog(row, $index)">
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
            </div>

            <!-- 更改日志 -->
            <div v-if="activeTab === 'changeLogs'" class="detail-items-table-wrap">
              <el-table
                v-if="fieldChangeLogs.length > 0"
                v-loading="changeLogsLoading"
                :data="fieldChangeLogs"
                class="detail-panel-list-table"
                size="small"
                stripe
              >
                <el-table-column :label="t('rfqDetail.logs.colChangeTime')" width="160">
                  <template #default="{ row }">{{ formatChangeLogTime(row?.changedAt) }}</template>
                </el-table-column>
                <el-table-column :label="t('rfqDetail.logs.colOperator')" width="100" show-overflow-tooltip>
                  <template #default="{ row }">{{ row.changedByUserName || t('rfqDetail.logs.system') }}</template>
                </el-table-column>
                <el-table-column :label="t('rfqDetail.logs.colObject')" width="140" show-overflow-tooltip>
                  <template #default="{ row }">{{ rfqChangeLogObjectLabel(row) }}</template>
                </el-table-column>
                <el-table-column :label="t('rfqDetail.logs.colField')" min-width="120" show-overflow-tooltip>
                  <template #default="{ row }">{{ row.fieldLabel || row.fieldName }}</template>
                </el-table-column>
                <el-table-column :label="t('rfqDetail.logs.colOldValue')" min-width="160" show-overflow-tooltip>
                  <template #default="{ row }">{{ row.oldValue ?? t('rfqDetail.logs.emptyValue') }}</template>
                </el-table-column>
                <el-table-column :label="t('rfqDetail.logs.colNewValue')" min-width="160" show-overflow-tooltip>
                  <template #default="{ row }">{{ row.newValue ?? t('rfqDetail.logs.emptyValue') }}</template>
                </el-table-column>
              </el-table>
              <DetailListPanelEmpty v-else-if="!changeLogsLoading" size="low" />
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
    <el-dialog v-model="assignDialogVisible" :title="assignDialogTitle" width="480px" :close-on-click-modal="false">
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

    <ApplyTagsDialog
      v-if="rfq"
      v-model="tagDialogVisible"
      entity-type="RFQ"
      :entity-ids="[rfq.id]"
      :title="t('rfqDetail.tags.dialogTitle')"
      @success="refreshTags"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElNotification, ElMessageBox } from 'element-plus'
import { rfqApi, type RfqFieldChangeLogRow } from '@/api/rfq'
import { quoteApi } from '@/api/quote'
import { favoriteApi } from '@/api/favorite'
import { RFQ_FAVORITE_ENTITY_TYPE, RFQ_FAVORITES_CHANGED_EVENT } from '@/constants/rfqFavorites'
import { canManualAssignRfqPurchaser } from '@/constants/rfqPurchaserAssign'
import { useAuthStore } from '@/stores/auth'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import { recordRfqRecentView } from '@/utils/rfqRecentHistory'
import {
  effectiveRfqItemLineStatus,
  rfqItemStatusTagType,
  RFQ_ITEM_STATUS_I18N_KEYS,
} from '@/utils/rfqItemLineStatus'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { pickCrmCopyableRowField } from '@/utils/crmListCopyableField'
import {
  rfqImportanceDisplayStars,
  RFQ_IMPORTANCE_RATE_COLORS,
  RFQ_IMPORTANCE_RATE_VOID_COLOR
} from '@/utils/rfqImportance'
import PurchaserCascader from '@/components/PurchaserCascader.vue'
import {
  formatRfqTypeLabel as getRFQTypeLabel,
  formatQuoteMethodLabel as getQuoteMethodLabel,
  formatAssignMethodLabel as getAssignMethodLabel
} from '@/constants/rfqFormEnums'
import { productionDateDisplayLabel, useMaterialProductionDateDict } from '@/composables/useMaterialProductionDateDict'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import { RFQItemStatus } from '@/types/rfq'
import TagListDisplay from '@/components/Tag/TagListDisplay.vue'
import ApplyTagsDialog from '@/components/Tag/ApplyTagsDialog.vue'
import {
  canUseRfqTagUi,
  normalizeRfqTags,
  resolveRfqCanEditTagsForUser,
  resolveRfqCanViewTags,
} from '@/utils/rfqTagAccess'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import { rfqChangeLogObjectLabel } from '@/utils/businessLogLabels'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const { canWriteSaleData } = useDepartmentDataReadOnly()
const rfqId = route.params.id as string
const { options: materialPdOptions, ensureLoaded: ensureMaterialPdDict } = useMaterialProductionDateDict()
const tagDialogVisible = ref(false)

const showRfqTagsSection = computed(
  () => canUseRfqTagUi(authStore.user) && resolveRfqCanViewTags(rfq.value)
)
const canEditRfqTags = computed(() => resolveRfqCanEditTagsForUser(rfq.value, authStore.user))
const rfqTags = computed(() => normalizeRfqTags(rfq.value))

async function refreshTags() {
  await loadRFQ()
}

function fmtProductionDate(v: unknown) {
  const s = String(v ?? '').trim()
  if (!s) return '—'
  return productionDateDisplayLabel(s, materialPdOptions.value) || '—'
}

/** 明细目标价币别：接口为 priceCurrency（1–4），勿用 currency 字符串（新建保存后详情接口不带该字段） */
function formatRfqItemTargetCurrency(row: Record<string, unknown>): string {
  const raw = row.priceCurrency ?? row.PriceCurrency ?? row.currency
  const n = typeof raw === 'number' ? raw : raw != null && raw !== '' ? Number(raw) : NaN
  if (Number.isFinite(n) && n >= 1) {
    const label = CURRENCY_CODE_TO_TEXT[Math.round(n)]
    if (label) return label
  }
  if (typeof raw === 'string') {
    const u = raw.trim().toUpperCase()
    if (u === 'CNY' || u === 'RMB') return 'RMB'
    if (u === 'USD' || u === 'EUR' || u === 'HKD') return u
  }
  return 'RMB'
}

function formatRfqItemTargetPriceNumber(row: Record<string, unknown>): string {
  const tp = row.targetPrice ?? row.TargetPrice
  if (tp == null || tp === '') return '—'
  const n = Number(tp)
  if (Number.isNaN(n)) return String(tp)
  return n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

function formatRfqItemTargetCurrencyClass(row: Record<string, unknown>): string {
  const ccy = formatRfqItemTargetCurrency(row)
  if (ccy === 'RMB') return 'dock-tier-ccy--rmb'
  if (ccy === 'USD') return 'dock-tier-ccy--usd'
  return 'dock-tier-ccy--fx'
}

const canAssignRfqPurchaser = computed(() => canManualAssignRfqPurchaser(authStore.user))
const rfqClosedForAssign = computed(() => {
  const s = rfq.value?.status
  return s === 7 || s === 8
})
/** 工具栏提供「为全部明细分配采购员」入口（列表/面板均显示；单行分配见列表操作列） */
const showAssignPurchaserToolbar = computed(
  () => canAssignRfqPurchaser.value && !rfqClosedForAssign.value
)

/** 需求明细报价统计（与列表 effective 状态口径一致） */
const rfqItemQuoteStats = computed(() => {
  let pending = 0
  let quoted = 0
  let noQuote = 0
  for (const row of rfqItems.value) {
    const st = effectiveItemLineStatus(row as Record<string, unknown>)
    if (st === RFQItemStatus.Pending) pending++
    else if (st === RFQItemStatus.Quoted) quoted++
    else if (st === RFQItemStatus.NoQuoteFound) noQuote++
  }
  return { pending, quoted, noQuote }
})

/** 仅展示 count > 0 的统计标签 */
const rfqItemQuoteStatPills = computed(() => {
  const c = rfqItemQuoteStats.value
  const pills: Array<{ key: string; class: string; label: string }> = []
  if (c.pending > 0) {
    pills.push({
      key: 'pending',
      class: 'item-quote-stats__pill--pending',
      label: t('rfqDetail.itemQuoteStats.pending', { count: c.pending }),
    })
  }
  if (c.quoted > 0) {
    pills.push({
      key: 'quoted',
      class: 'item-quote-stats__pill--quoted',
      label: t('rfqDetail.itemQuoteStats.quoted', { count: c.quoted }),
    })
  }
  if (c.noQuote > 0) {
    pills.push({
      key: 'noQuote',
      class: 'item-quote-stats__pill--no-quote',
      label: t('rfqDetail.itemQuoteStats.noQuote', { count: c.noQuote }),
    })
  }
  return pills
})

const loading = ref(false)
const rfqFavorited = ref(false)
const favoriteLoading = ref(false)
const rfq = ref<any>(null)
const rfqItems = ref<any[]>([])
const quoteRecordCountByRfqItemId = ref<Record<string, number>>({})
const closeRecords = ref<any[]>([])
const itemsLoading = ref(false)
const activeTab = ref<'items' | 'changeLogs' | 'closeRecords'>('items')
const fieldChangeLogs = ref<RfqFieldChangeLogRow[]>([])
const changeLogsLoading = ref(false)
const changeLogsLoaded = ref(false)
/** 需求明细：列表（默认） / 面板 */
const itemsViewMode = ref<'list' | 'panel'>('list')

type RfqDetailTabKey = 'items' | 'changeLogs' | 'closeRecords'

function rfqDetailTabCount(tab: RfqDetailTabKey): number {
  switch (tab) {
    case 'items':
      return rfqItems.value.length
    case 'changeLogs':
      return fieldChangeLogs.value.length
    case 'closeRecords':
      return closeRecords.value.length
    default:
      return 0
  }
}

function formatRfqDetailTabLabel(label: string, tab: RfqDetailTabKey): string {
  const count = rfqDetailTabCount(tab)
  return count > 0 ? `${label} (${count})` : label
}

function resetChangeLogs() {
  fieldChangeLogs.value = []
  changeLogsLoaded.value = false
}

async function loadChangeLogs(opts?: { silent?: boolean }) {
  if (!rfqId) return
  changeLogsLoading.value = true
  try {
    fieldChangeLogs.value = (await rfqApi.getChangeLogs(rfqId)) ?? []
    changeLogsLoaded.value = true
  } catch (e: unknown) {
    if (!opts?.silent) {
      ElNotification.error({
        title: t('rfqDetail.toast.changeLogsLoadFailedTitle'),
        message:
          (e instanceof Error ? e.message : '') ||
          t('rfqDetail.toast.changeLogsLoadFailedMessage'),
      })
    }
  } finally {
    changeLogsLoading.value = false
  }
}

function formatChangeLogTime(v?: string) {
  if (!v) return '—'
  return formatDisplayDateTime(v) || '—'
}

watch(activeTab, (tab) => {
  if (tab === 'changeLogs' && !changeLogsLoaded.value) void loadChangeLogs()
})

/** 《列表操作列规范》：需求明细「分配采购员」列 */
const rfqDetailAssignOpColExpanded = ref(false)
const RFQ_DETAIL_ASSIGN_OP_COL_COLLAPSED = 43
const RFQ_DETAIL_ASSIGN_OP_COL_EXPANDED = 173
const RFQ_DETAIL_ASSIGN_OP_COL_EXPANDED_MIN = 160
const rfqDetailAssignOpColWidth = computed(() =>
  rfqDetailAssignOpColExpanded.value ? RFQ_DETAIL_ASSIGN_OP_COL_EXPANDED : RFQ_DETAIL_ASSIGN_OP_COL_COLLAPSED
)
const rfqDetailAssignOpColMinWidth = computed(() =>
  rfqDetailAssignOpColExpanded.value ? RFQ_DETAIL_ASSIGN_OP_COL_EXPANDED_MIN : RFQ_DETAIL_ASSIGN_OP_COL_COLLAPSED
)
function toggleRfqDetailAssignOpCol() {
  rfqDetailAssignOpColExpanded.value = !rfqDetailAssignOpColExpanded.value
}

const tabs = computed(() => [
  { key: 'items' as const, label: t('rfqDetail.tabs.items') },
  { key: 'changeLogs' as const, label: t('rfqDetail.tabs.changeLogs') },
  { key: 'closeRecords' as const, label: t('rfqDetail.tabs.closeRecords') },
])

const assignDialogVisible = ref(false)
const assignLoading = ref(false)
const recommendedPurchaser = ref<any>(null)
const assignTargetItemId = ref<string | null>(null)
const assignTargetLineNo = ref<number | null>(null)
const assignForm = reactive({ purchaserId: '', remark: '' })

const assignDialogTitle = computed(() => {
  if (assignTargetLineNo.value != null) {
    return t('rfqDetail.assignPurchaserForLine', { n: assignTargetLineNo.value })
  }
  return t('rfqDetail.assignPurchaserForAll')
})

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

function getStatusType(status?: number) {
  const map: Record<number, '' | 'success' | 'warning' | 'info' | 'danger' | 'primary'> = {
    0: 'info',
    1: 'warning',
    2: 'primary',
    3: 'success',
    4: 'success',
    5: 'success',
    6: 'info',
    7: 'info',
    8: 'warning'
  }
  return status !== undefined ? (map[status] ?? 'info') : 'info'
}

function getStatusLabel(status?: number) {
  const map: Record<number, string> = {
    0: t('rfqList.status.pending'),
    1: t('rfqList.status.assigned'),
    2: t('rfqList.status.processing'),
    3: t('rfqList.status.quoted'),
    4: t('rfqList.status.selected'),
    5: t('rfqList.status.converted'),
    6: t('rfqList.status.closed'),
    7: t('rfqList.status.closed'),
    8: t('rfqList.status.cancelled')
  }
  return status !== undefined ? (map[status] ?? t('rfqDetail.unknown')) : t('quoteList.na')
}

function resolveRfqItemRowId(row: Record<string, unknown>): string {
  const id = row.id ?? row.Id
  return id != null && String(id).trim() !== '' ? String(id).trim() : ''
}

function effectiveItemLineStatus(row: Record<string, unknown>): number | undefined {
  const raw = row.status ?? row.Status
  const id = resolveRfqItemRowId(row)
  const qc = id ? quoteRecordCountByRfqItemId.value[id] ?? 0 : 0
  return effectiveRfqItemLineStatus(raw as number | string | undefined, qc)
}

function itemStatusText(status?: number | string) {
  const n = status === undefined || status === null || status === '' ? NaN : Number(status)
  const key = Number.isFinite(n) ? RFQ_ITEM_STATUS_I18N_KEYS[n] : undefined
  return key ? t(key) : t('quoteList.na')
}

function itemStatusTagType(status?: number | string) {
  return rfqItemStatusTagType(status)
}

function getSourceLabel(source?: number) {
  const map: Record<number, string> = { 1: t('rfqDetail.source.offline'), 2: t('rfqDetail.source.online'), 3: t('rfqDetail.source.email'), 4: t('rfqDetail.source.phone'), 5: t('rfqDetail.source.import') }
  return source !== undefined ? (map[source] ?? t('quoteList.na')) : t('quoteList.na')
}

function isValidRfqSource(source?: number): boolean {
  return source === 1 || source === 2 || source === 3 || source === 4 || source === 5
}

const hasRfqSourceLabel = computed(() => isValidRfqSource(rfq.value?.source))

const rfqImportanceStars = computed(() => {
  const o = rfq.value
  if (!o) return null
  const raw = o.importanceLevel ?? (o as Record<string, unknown>).importance
  if (raw == null || raw === '') return null
  return rfqImportanceDisplayStars(raw)
})

const rfqBasicCreateDateText = computed(() => {
  const o = rfq.value
  if (!o) return '—'
  const raw = (o as Record<string, unknown>).createTime ?? o.createdAt
  return formatDate(typeof raw === 'string' ? raw : undefined)
})

const rfqBasicCreateUserText = computed(() => {
  const o = rfq.value
  if (!o) return '—'
  const name =
    o.createUserName ||
    (o as Record<string, unknown>).CreateUserName ||
    o.createdBy
  const s = name != null ? String(name).trim() : ''
  return s || '—'
})

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
  resetChangeLogs()
  try {
    rfq.value = await rfqApi.getRFQDetail(rfqId)
    if (rfq.value) {
      recordRfqRecentView({
        id: rfqId,
        rfqCode: rfq.value.rfqCode,
        customerName: rfq.value.customerName
      })
      void loadChangeLogs({ silent: true })
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
  try {
    const res = await rfqApi.getRFQItemsWithBestQuote(rfqId)
    rfqItems.value = res || []
    const ids = rfqItems.value
      .map((row) => resolveRfqItemRowId(row as Record<string, unknown>))
      .filter(Boolean)
    if (ids.length) {
      try {
        const { counts } = await quoteApi.getQuoteCountsByRfqItemIds(ids)
        quoteRecordCountByRfqItemId.value = counts || {}
      } catch {
        quoteRecordCountByRfqItemId.value = {}
      }
    } else {
      quoteRecordCountByRfqItemId.value = {}
    }
  } catch {
    rfqItems.value = []
    quoteRecordCountByRfqItemId.value = {}
  } finally {
    itemsLoading.value = false
  }
}

async function loadCloseRecords() {
  try { const res = await rfqApi.getCloseRecords(rfqId); closeRecords.value = res || [] }
  catch { closeRecords.value = [] }
}

async function showAssignDialog(row?: { id?: string; lineNo?: number }, rowIndex?: number) {
  assignTargetItemId.value = row?.id?.trim() || null
  const lineNo = Number(row?.lineNo)
  assignTargetLineNo.value =
    row != null
      ? lineNo > 0
        ? lineNo
        : rowIndex != null
          ? rowIndex + 1
          : null
      : null
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
    await rfqApi.assignPurchaser(rfqId, {
      purchaserId: assignForm.purchaserId,
      remark: assignForm.remark,
      ...(assignTargetItemId.value ? { rfqItemId: assignTargetItemId.value } : {})
    })
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

onMounted(() => {
  void ensureMaterialPdDict()
  loadRFQ()
  loadItems()
  loadCloseRecords()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+SC:wght@300;400;500&display=swap');

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
  font-family: 'Noto Sans SC', sans-serif;
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.title-meta--caption {
  margin-top: 4px;
}

.rfq-header-meta-row {
  min-height: 28px;
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
  color: #ffc94d;
  cursor: pointer;
  transition: color 0.15s, background 0.15s, transform 0.12s;

  .star-icon {
    width: 22px;
    height: 22px;
    display: block;
  }

  &:not(.is-favorite) .star-icon {
    stroke-dasharray: 3 2.5;
  }

  &:not(.is-favorite):hover:not(:disabled) {
    color: #ffd666;
    background: rgba(255, 201, 77, 0.12);
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

.source-tag {
  font-size: 11px;
  color: $text-muted;
  background: rgba(255,255,255,0.05);
  border: 1px solid $border-panel;
  border-radius: 4px;
  padding: 1px 6px;
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
.btn-close-rfq {
  color: $color-amber;
  border: none;
  background: transparent;
  &:hover {
    background: rgba(255, 255, 255, 0.08);
    border: none;
  }
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
  justify-content: space-between;
  gap: 16px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255,255,255,0.05);
  background: var(--crm-detail-section-header-bg);
}
.section-header__main {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}
.section-header__meta {
  display: flex;
  align-items: center;
  gap: 20px;
  flex-shrink: 0;
  margin-left: auto;
}
.section-header-meta-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  white-space: nowrap;
  &__label {
    color: $text-muted;
    &::after {
      content: '：';
    }
  }
  &__value {
    color: $text-secondary;
  }
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
  border-bottom: 1px solid rgba(255,255,255,0.04);
  border-right: 1px solid rgba(255,255,255,0.04);
  &:nth-child(3n) { border-right: none; }
  .info-label { font-size: 11px; color: $text-muted; letter-spacing: 0.5px; text-transform: uppercase; }
  .info-value {
    font-size: 13px; color: $text-secondary;
    &--code { font-family: 'Noto Sans SC', sans-serif; font-size: 12px; color: $color-ice-blue; }
    &--time { font-size: 12px; color: $text-muted; }
  }
}
.info-grid:not(.info-grid--inline-labels) .info-item {
  padding: 16px 20px;
}
.info-grid--inline-labels .info-item {
  flex-direction: row;
  align-items: center;
  gap: 8px;
  .info-label {
    flex-shrink: 0;
    white-space: nowrap;
    text-transform: none;
    letter-spacing: 0;
    font-size: 12px;
    &::after {
      content: '：';
    }
  }
  .info-value {
    flex: 1;
    min-width: 0;
    word-break: break-word;
  }
  .info-value--importance {
    flex: 0 0 auto;
    :deep(.el-rate) {
      height: auto;
      --el-rate-icon-size: 16px;
    }
  }
}
.info-grid--basic {
  .info-item--basic-full-row {
    grid-column: 1 / -1;
    border-right: none;
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
  background: var(--crm-detail-section-header-bg);
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
  font-family: 'Noto Sans SC', sans-serif;
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
.tab-toolbar__meta {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  min-width: 0;
}
.item-quote-stats-bar {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin: -6px 0 14px;
  padding: 10px 14px;
  border-radius: 8px;
  border: 1px solid $border-panel;
  background: rgba(0, 0, 0, 0.12);
}
.item-quote-stats-bar--header {
  margin: 0;
  padding: 0;
  border: none;
  background: transparent;
  gap: 6px;
}
.item-quote-stats-bar--header .item-quote-stats__pill {
  padding: 2px 8px;
  font-size: 11px;
}
.item-quote-stats__sep {
  color: $text-muted;
  font-size: 12px;
  user-select: none;
}
.item-quote-stats__pill {
  display: inline-flex;
  align-items: center;
  padding: 4px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 500;
  line-height: 1.4;
  border: 1px solid transparent;
  white-space: nowrap;
}
.item-quote-stats__pill--pending {
  color: $text-secondary;
  background: rgba(138, 155, 176, 0.16);
  border-color: rgba(138, 155, 176, 0.28);
}
.item-quote-stats__pill--quoted {
  color: $cyan-primary;
  background: rgba(0, 212, 255, 0.1);
  border-color: rgba(0, 212, 255, 0.28);
}
.item-quote-stats__pill--no-quote {
  color: $color-amber;
  background: rgba(201, 154, 69, 0.14);
  border-color: rgba(201, 154, 69, 0.32);
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
  background: var(--crm-detail-panel-card-bg);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  padding: 0;
  overflow: hidden;
}
.item-panel-card__head {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  padding: 12px 16px;
  border-bottom: 1px solid $border-panel;
  background: var(--crm-detail-panel-card-head-bg);
}
.item-panel-card__idx {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}
.item-panel-card__body {
  padding: 12px 16px 16px;
}
.item-panel-row {
  margin-bottom: 4px;
  &:last-child {
    margin-bottom: 0;
  }
}
.item-panel-field {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 10px;
  min-width: 0;
}
.item-panel-field__label {
  flex-shrink: 0;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.3;
  white-space: nowrap;
}
.item-panel-field__label::after {
  content: '：';
}
.item-panel-field__value {
  flex: 1;
  min-width: 0;
  font-size: 13px;
  line-height: 1.45;
  word-break: break-word;
}
.item-panel-field-spacer {
  @media (max-width: 991px) {
    display: none;
  }
}
.item-panel-field__value--code {
  font-family: 'Noto Sans SC', sans-serif;
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

// ---- 需求明细列表（§7.4 面板列表）----
.detail-items-table-wrap {
  margin-top: 4px;
}

.detail-items-table-wrap :deep(.items-table) {
  --el-table-border-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
  :deep(.el-table) {
    color: var(--crm-table-text);
  }
  :deep(.el-table__inner-wrapper) {
    background: transparent;
    &::before { display: none !important; }
    &::after  { display: none !important; }
  }
  :deep(.el-table__border-left-patch) { display: none !important; }
  :deep(.el-table__cell) {
    .el-button { white-space: nowrap !important; }
    .cell { white-space: nowrap; }
  }
  :deep(th.op-col.el-table__cell .cell) {
    display: flex;
    justify-content: center;
    align-items: center;
    padding-left: 2px !important;
    padding-right: 2px !important;
  }
  :deep(th.op-col .list-op-col-header--icon-only) {
    display: flex;
    justify-content: center;
    align-items: center;
    width: 100%;
  }
  :deep(th.op-col .list-op-col-toggle) {
    min-width: 28px;
    min-height: 28px;
    font-size: 18px;
    font-weight: 700;
    line-height: 1;
  }
}

// ---- 表格（关闭记录等 §7.2 Tab 内嵌表）----
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
.cell-code      { font-family: 'Noto Sans SC', sans-serif; font-size: 12px; color: $color-ice-blue; }

.amount-with-code {
  display: inline-flex;
  align-items: baseline;
  gap: 4px;
}

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

.tags-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.rfq-header-tags-row {
  flex-shrink: 0;
}

.rfq-header-add-tag-btn {
  padding: 6px 12px;
  font-size: 12px;
}

.rfq-header-add-tag-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 13px;
  font-size: 15px;
  font-weight: 500;
  line-height: 1;
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
