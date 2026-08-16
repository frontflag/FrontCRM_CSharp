<template>
  <div class="purchase-order-detail">
    <!-- 详情 CaptionBar（对齐《业务详情页面规范》§3） -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="router.back()">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          返回
        </button>
        <div v-if="order" class="po-caption-title-group">
          <div class="caption-avatar-lg">{{ captionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1
                  class="page-title"
                  :class="{ 'page-title--muted': normalizePurchaseOrderMainStatus(order) === -2 }"
                >
                  采购订单 {{ order.purchaseOrderCode }}
                </h1>
                <button
                  type="button"
                  class="btn-favorite-star"
                  :class="{ 'is-favorite': poFavorited }"
                  :disabled="favoriteLoading"
                  :title="poFavorited ? '取消收藏' : '收藏订单'"
                  :aria-label="poFavorited ? '取消收藏' : '收藏采购订单'"
                  :aria-pressed="poFavorited"
                  @click="toggleFavorite"
                >
                  <svg
                    v-if="!poFavorited"
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
                <div v-if="showPoHeaderTags" class="po-header-tags-row tags-row">
                  <TagListDisplay v-if="currentTags.length" :tags="currentTags" />
                  <button v-if="canWritePo" type="button" class="btn-secondary po-header-add-tag-btn" @click="tagDialogVisible = true">
                    <span class="po-header-add-tag-icon" aria-hidden="true">±</span>
                    标签
                  </button>
                </div>
              </div>
            </div>
            <div class="title-meta title-meta--caption po-header-meta-row">
              <el-tag effect="dark" :type="getStatusType(normalizePurchaseOrderMainStatus(order))" size="small">
                {{ getStatusText(normalizePurchaseOrderMainStatus(order)) }}
              </el-tag>
              <el-tooltip
                v-if="isStockingPurchaseOrder"
                content="备货采购"
                placement="top"
              >
                <el-tag type="warning" effect="plain" size="small" class="po-stocking-tag" round>
                  备货
                </el-tag>
              </el-tooltip>
              <el-tooltip
                v-if="isPayLaterPurchaseOrder"
                content="客户付款后再给供应商付款"
                placement="top"
              >
                <el-tag effect="dark" size="small" class="po-pay-later-tag" round>
                  后付款
                </el-tag>
              </el-tooltip>
            </div>
          </div>
        </div>
      </div>
      <div v-if="order" class="header-right">
        <button
          v-if="canCancelPurchaseOrderFromMenu"
          type="button"
          class="btn-close-po"
          @click="handleCancelPurchaseOrder"
        >
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10" /><line x1="15" y1="9" x2="9" y2="15" /><line x1="9" y1="9" x2="15" y2="15" />
          </svg>
          取消订单
        </button>
        <div class="po-header-refresh-group">
          <button
            class="btn-secondary"
            type="button"
            :disabled="refreshingExtends || syncingVendor"
            @click="handleRefreshItemExtends"
          >
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="23 4 23 10 17 10" />
              <polyline points="1 20 1 14 7 14" />
              <path d="M3.51 9a9 9 0 0 1 14.13-3.36L23 10M1 14l5.36 4.36A9 9 0 0 0 20.49 15" />
            </svg>
            {{ refreshingExtends ? t('purchaseOrderDetail.refreshing') : t('purchaseOrderDetail.refresh') }}
          </button>
          <el-dropdown
            v-if="canRefreshPoVendor"
            trigger="click"
            placement="bottom-end"
            :disabled="refreshingExtends || syncingVendor"
            popper-class="po-detail-header-more-popper"
            @command="handleRefreshMenuCommand"
          >
            <button
              type="button"
              class="btn-secondary po-header-refresh-caret"
              :disabled="refreshingExtends || syncingVendor"
              aria-label="刷新菜单"
            >
              ▾
            </button>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="vendor">刷新供应商</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
        <button
          v-if="order && purchaseOrderReportAllowed(normalizePurchaseOrderMainStatus(order))"
          class="btn-secondary"
          type="button"
          @click="handleGoReport"
        >
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
            <polyline points="14 2 14 8 20 8" />
            <line x1="12" y1="18" x2="12" y2="12" />
            <line x1="9" y1="15" x2="15" y2="15" />
          </svg>
          打印订单
        </button>
        <button
          v-if="canEditFreightForwarderOrderNo"
          class="btn-secondary"
          type="button"
          @click="openFreightForwarderOrderNoDialog"
        >
          录入货代单号
        </button>
        <button v-if="canWritePo" class="btn-primary" type="button" @click="handleEdit">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
          </svg>
          编辑
        </button>
        <el-dropdown
          v-if="showHeaderMoreMenu"
          trigger="click"
          placement="bottom-end"
          popper-class="po-detail-header-more-popper"
          @command="onHeaderMoreCommand"
        >
          <button type="button" class="btn-more-actions" title="更多操作" aria-label="更多操作">
            <span class="btn-more-actions__dots" aria-hidden="true">⋯</span>
          </button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="delete" class="detail-more-item--danger">删除订单</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </div>

    <div v-if="loading" class="loading-wrap">
      <el-skeleton :rows="8" animated />
    </div>

    <template v-else-if="order">
      <!-- 基本信息（《业务详情页面规范》§4–§5） -->
      <div class="info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">基本信息</span>
          </div>
          <div class="section-header__meta">
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">创建日期</span>
              <span class="section-header-meta-item__value">{{ poBasicCreateDateText }}</span>
            </span>
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">创建人</span>
              <span class="section-header-meta-item__value">{{ poBasicCreateUserText }}</span>
            </span>
          </div>
        </div>
        <div class="info-grid info-grid--inline-labels info-grid--basic">
          <div v-if="canViewVendorInfo" class="info-item">
            <span class="info-label">供应商</span>
            <span class="info-value">
              <vendor-name-readonly-text
                :name-zh="order.vendorName"
                :name-en="order.vendorEnglishName"
              />
            </span>
          </div>
          <div class="info-item">
            <span class="info-label">采购员</span>
            <span class="info-value">{{ order.purchaseUserName || '—' }}</span>
          </div>
          <div v-if="canViewPurchaseAmount" class="info-item">
            <span class="info-label">总金额</span>
            <span class="info-value info-value--amount amount-with-code">
              <span>{{ formatTotalAmountNumber(order.total) }}</span>
              <span v-if="formatTotalAmountNumber(order.total) !== '—'" class="amount-ccy" :class="currencyCodeClass(order.currency)">
                {{ currencyCodeText(order.currency) }}
              </span>
            </span>
          </div>
        </div>
        <div class="info-grid info-grid--inline-labels info-grid--basic">
          <div class="info-item">
            <span class="info-label">采购助理</span>
            <span class="info-value">{{ order.assistorUserName || '—' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">交货日期</span>
            <span class="info-value info-value--time">{{ formatDateTime(order.deliveryDate) }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">货代单号</span>
            <span class="info-value">{{ order.freightForwarderOrderNo?.trim() || '—' }}</span>
          </div>
        </div>
        <div class="info-grid info-grid--inline-labels info-grid--basic">
          <div class="info-item">
            <span class="info-label">送货地址</span>
            <span class="info-value">{{ order.deliveryAddress || '—' }}</span>
          </div>
          <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
          <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
        </div>
        <div class="info-grid info-grid--inline-labels">
          <div class="info-item info-item--span-all">
            <span class="info-label">备注</span>
            <span class="info-value">{{ order.comment || '—' }}</span>
          </div>
          <div class="info-item info-item--span-all">
            <span class="info-label">内部备注</span>
            <span class="info-value">{{ order.innerComment || '—' }}</span>
          </div>
        </div>
      </div>

      <!-- TabBar：订单明细 | 文档（采购申请/付款/等到货等下游见底部「采购订单明细详情」） -->
      <div class="tabs-section">
        <div class="tabs-nav">
          <button class="tab-btn" :class="{ 'tab-btn--active': activeTab === 'items' }" @click="activeTab = 'items'">{{ formatOrderDetailTabLabel('订单明细', 'items') }}</button>
          <button
            v-if="!maskPurchaseSensitiveFields"
            class="tab-btn"
            :class="{ 'tab-btn--active': activeTab === 'documents' }"
            @click="activeTab = 'documents'"
          >
            {{ formatOrderDetailTabLabel('文档', 'documents') }}
          </button>
          <button
            v-if="!maskPurchaseSensitiveFields"
            class="tab-btn"
            :class="{ 'tab-btn--active': activeTab === 'changeLog' }"
            @click="activeTab = 'changeLog'"
          >
            {{ formatOrderDetailTabLabel('更改日志', 'changeLog') }}
          </button>
          <button
            v-if="!maskPurchaseSensitiveFields"
            class="tab-btn"
            :class="{ 'tab-btn--active': activeTab === 'deleteLog' }"
            @click="activeTab = 'deleteLog'"
          >
            {{ formatOrderDetailTabLabel('删除日志', 'deleteLog') }}
          </button>
        </div>
        <div class="tabs-body">
          <div v-show="activeTab === 'items'" class="detail-items-table-wrap">
            <template v-if="order.items?.length">
              <!-- 底栏在 DOM 中先于表格，保证 Teleport 锚点已挂载；column-reverse 保持视觉顺序为表在上、底栏在下 -->
              <div class="po-detail-items-table-stack">
              <div class="pagination-wrapper po-detail-items-list-footer">
                <div class="list-footer-left">
                  <el-tooltip content="列设置" placement="top" :hide-after="0">
                    <el-button
                      class="list-settings-btn"
                      link
                      type="primary"
                      aria-label="列设置"
                      @click="poDetailItemsTableRef?.openColumnSettings?.()"
                    >
                      <el-icon><Setting /></el-icon>
                    </el-button>
                  </el-tooltip>
                  <span ref="poDetailItemsDensityAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
                  <div class="list-footer-spacer" aria-hidden="true"></div>
                </div>
              </div>
              <CrmDataTable
                ref="poDetailItemsTableRef"
                class="items-table detail-panel-list-table po-detail-items-table"
                column-layout-key="purchase-order-detail-items"
                :columns="poDetailItemsColumns"
                :show-column-settings="false"
                :density-toggle-anchor-el="poDetailItemsDensityAnchorEl"
                :border="false"
                embedded
                :data="order.items"
                :row-key="poItemRowKey"
                :row-class-name="poItemRowClassName"
                size="small"
                stripe
                @row-click="onPurchaseOrderItemRowClick"
                @row-dblclick="onPurchaseOrderItemRowDblClick"
              >
                <template #col-qty="{ row }">
                  <span class="po-detail-biz-qty">{{ formatPoProgressQty(row.qty) }}</span>
                </template>
                <template #col-cost="{ row }">
                  <span class="amount-with-code">
                    <span>{{ formatUnitPriceNumber(row.cost) }}</span>
                    <span v-if="formatUnitPriceNumber(row.cost) !== '—'" class="amount-ccy" :class="currencyCodeClass(row.currency)">
                      {{ currencyCodeText(row.currency) }}
                    </span>
                  </span>
                </template>
                <template #col-lineAmount="{ row }">
                  <span class="amount-with-code">
                    <span>{{ formatTotalAmountNumber(row.qty * row.cost) }}</span>
                    <span
                      v-if="formatTotalAmountNumber(row.qty * row.cost) !== '—'"
                      class="amount-ccy"
                      :class="currencyCodeClass(row.currency)"
                    >
                      {{ currencyCodeText(row.currency) }}
                    </span>
                  </span>
                </template>
                <template #col-purchaseProgressStatus="{ row }">
                  <el-tag :type="poExtendTriTagType(row.purchaseProgressStatus)" size="small" effect="dark">
                    {{ poPurchaseProgressText(row.purchaseProgressStatus) }}
                  </el-tag>
                </template>
                <template #col-stockInProgressStatus="{ row }">
                  <el-tag :type="poExtendTriTagType(row.stockInProgressStatus)" size="small" effect="dark">
                    {{ poStockInProgressText(row.stockInProgressStatus) }}
                  </el-tag>
                </template>
                <template #col-paymentProgressStatus="{ row }">
                  <el-tag :type="poExtendTriTagType(row.paymentProgressStatus)" size="small" effect="dark">
                    {{ poPaymentProgressText(row.paymentProgressStatus) }}
                  </el-tag>
                </template>
                <template #col-invoiceProgressStatus="{ row }">
                  <el-tag :type="poExtendTriTagType(row.invoiceProgressStatus)" size="small" effect="dark">
                    {{ poInvoiceProgressText(row.invoiceProgressStatus) }}
                  </el-tag>
                </template>
                <template #col-purchaseProgressQty="{ row }">
                  <div><span class="po-detail-biz-qty">{{ formatPoProgressQty(row.purchaseProgressQty) }}</span></div>
                  <div v-if="poShowSellLinePurchaseSum(row)" class="po-item-progress-sub">
                    同销单行 {{ formatPoProgressQty(row.sellLinePurchaseQtySum) }}
                  </div>
                </template>
                <template #col-stockInProgressQty="{ row }">
                  {{ formatPoProgressQty(row.stockInProgressQty) }}
                </template>
                <template #col-paymentProgressAmount="{ row }">
                  <span class="amount-with-code">
                    <span>{{ formatTotalAmountNumber(row.paymentProgressAmount) }}</span>
                    <span
                      v-if="formatTotalAmountNumber(row.paymentProgressAmount) !== '—'"
                      class="amount-ccy"
                      :class="currencyCodeClass(row.currency)"
                    >
                      {{ currencyCodeText(row.currency) }}
                    </span>
                  </span>
                </template>
                <template #col-invoiceProgressAmount="{ row }">
                  <span class="amount-with-code">
                    <span>{{ formatTotalAmountNumber(row.invoiceProgressAmount) }}</span>
                    <span
                      v-if="formatTotalAmountNumber(row.invoiceProgressAmount) !== '—'"
                      class="amount-ccy"
                      :class="currencyCodeClass(row.currency)"
                    >
                      {{ currencyCodeText(row.currency) }}
                    </span>
                  </span>
                </template>
                <template #col-actions-header>
                  <div class="po-detail-op-col-header--icon-only">
                    <button
                      type="button"
                      class="op-col-toggle-btn po-detail-op-col-toggle"
                      :aria-label="poDetailItemsOpColExpanded ? '收起操作列' : '展开操作列'"
                      :title="poDetailItemsOpColExpanded ? '收起' : '展开'"
                      @click.stop="togglePoDetailItemsOpCol"
                    >
                      {{ poDetailItemsOpColExpanded ? '>' : '<' }}
                    </button>
                  </div>
                </template>
                <template #col-actions="{ row }">
                  <div @click.stop @dblclick.stop>
                    <div v-if="poDetailItemsOpColExpanded" class="action-btns action-btns--po-detail-items">
                      <el-button link type="primary" size="small" @click.stop="goPoItemLines(row)">明细列表</el-button>
                      <el-button
                        v-if="poLineShowArrival(row)"
                        link
                        type="warning"
                        size="small"
                        @click.stop="openPoLineArrival(row)"
                      >
                        通知到货
                      </el-button>
                      <el-button
                        v-if="poLineShowPayment(row)"
                        link
                        type="warning"
                        size="small"
                        @click.stop="openPoLinePayment(row)"
                      >
                        申请付款
                      </el-button>
                    </div>
                    <el-dropdown v-else trigger="click" placement="bottom-end">
                      <div class="op-more-dropdown-trigger">
                        <button type="button" class="op-more-trigger">...</button>
                      </div>
                      <template #dropdown>
                        <el-dropdown-menu>
                          <el-dropdown-item @click.stop="goPoItemLines(row)">
                            <span class="op-more-item op-more-item--primary">明细列表</span>
                          </el-dropdown-item>
                          <el-dropdown-item v-if="poLineShowArrival(row)" @click.stop="openPoLineArrival(row)">
                            <span class="op-more-item op-more-item--warning">通知到货</span>
                          </el-dropdown-item>
                          <el-dropdown-item v-if="poLineShowPayment(row)" @click.stop="openPoLinePayment(row)">
                            <span class="op-more-item op-more-item--warning">申请付款</span>
                          </el-dropdown-item>
                        </el-dropdown-menu>
                      </template>
                    </el-dropdown>
                  </div>
                </template>
              </CrmDataTable>
              </div>
            </template>
            <el-empty v-else description="暂无明细" :image-size="80" />
          </div>
          <div
            v-show="activeTab === 'documents' && !maskPurchaseSensitiveFields"
            class="doc-tab-content"
            :class="{ 'doc-tab-content--dragging': docTabDragging }"
            @drop.prevent="onDocTabDrop"
            @dragover.prevent="onDocTabDragOver"
            @dragleave="onDocTabDragLeave"
          >
            <DocumentUploadPanel
              ref="docUploadRef"
              biz-type="PURCHASE_ORDER"
              :biz-id="String(order.id)"
              :max-files="20"
              :max-size-mb="100"
              @uploaded="onPoDocumentUploaded"
            />
            <DocumentListPanel
              ref="docListRef"
              biz-type="PURCHASE_ORDER"
              :biz-id="String(order.id)"
              view-mode="list"
              style="margin-top: 16px;"
            />
          </div>
          <div v-show="activeTab === 'changeLog' && !maskPurchaseSensitiveFields" class="detail-items-table-wrap">
            <el-table
              v-if="changeLogs.length > 0"
              v-loading="changeLogsLoading"
              :data="changeLogs"
              class="detail-panel-list-table"
              size="small"
              stripe
            >
              <el-table-column label="变更时间" width="160">
                <template #default="{ row }">{{ formatDateTime(row?.changedAt) }}</template>
              </el-table-column>
              <el-table-column label="操作人" width="100" show-overflow-tooltip>
                <template #default="{ row }">{{ row.changedByUserName || '系统' }}</template>
              </el-table-column>
              <el-table-column label="对象" width="140" show-overflow-tooltip>
                <template #default="{ row }">{{ row.objectLabel || '主表' }}</template>
              </el-table-column>
              <el-table-column prop="fieldLabel" label="字段" min-width="120" show-overflow-tooltip>
                <template #default="{ row }">{{ row.fieldLabel || row.fieldName }}</template>
              </el-table-column>
              <el-table-column prop="oldValue" label="原值" min-width="160" show-overflow-tooltip>
                <template #default="{ row }">{{ row.oldValue ?? '(空)' }}</template>
              </el-table-column>
              <el-table-column prop="newValue" label="新值" min-width="160" show-overflow-tooltip>
                <template #default="{ row }">{{ row.newValue ?? '(空)' }}</template>
              </el-table-column>
            </el-table>
            <DetailListPanelEmpty v-else-if="!changeLogsLoading" size="low" />
          </div>
          <div v-show="activeTab === 'deleteLog' && !maskPurchaseSensitiveFields" v-loading="deletedItemsLoading" class="po-aggregate-table-wrap">
            <el-table v-if="deletedItems.length > 0" :data="deletedItems" size="small" stripe>
              <el-table-column label="删除日期" width="160">
                <template #default="{ row }">{{ formatDateTime(row?.deletedAt || row?.createTime) }}</template>
              </el-table-column>
              <el-table-column label="操作人" width="100" show-overflow-tooltip>
                <template #default="{ row }">{{ row.deletedByUserName || '—' }}</template>
              </el-table-column>
              <el-table-column prop="purchaseOrderItemCode" label="采购订单明细编号" min-width="140" show-overflow-tooltip />
              <CrmCopyableTableColumn prop="pn" label="物料型号" min-width="140" />
              <CrmCopyableTableColumn prop="brand" label="品牌" width="100" />
              <el-table-column label="数量" width="90" align="right" prop="qty" />
              <el-table-column label="单价+币别" width="120" align="right">
                <template #default="{ row }">{{ formatDeletedPoItemCost(row) }}</template>
              </el-table-column>
              <el-table-column prop="comment" label="备注" min-width="140" show-overflow-tooltip />
            </el-table>
            <el-empty v-else description="暂无记录" :image-size="64" />
          </div>
        </div>
      </div>

      <!-- 双击「订单明细」行：按该采购明细主键加载下游列表 -->
      <div v-if="poItemLinePanel.visible && !maskPurchaseSensitiveFields" class="so-item-line-detail-panel">
        <div class="so-item-line-detail-panel__head">
          <span class="so-item-line-detail-panel__title">采购订单明细详情</span>
          <span class="so-item-line-detail-panel__code panel-hint__value">{{ poItemLinePanel.purchaseOrderItemCode || '—' }}</span>
          <button type="button" class="so-item-line-detail-panel__close" @click="closePoItemLinePanel">收起</button>
        </div>
        <el-alert
          v-if="poItemLinePanel.loadError"
          type="error"
          :closable="false"
          :title="poItemLinePanel.loadError"
          class="so-item-line-detail-panel__alert"
          show-icon
        />
        <div v-loading="poItemLinePanel.loading" class="so-item-line-detail-panel__body so-item-line-detail-panel__body--tabbed">
          <div class="tabs-section so-item-line-detail-tabs-section">
            <div class="tabs-nav">
              <button
                type="button"
                class="tab-btn"
                :class="{ 'tab-btn--active': poItemLinePanel.activeTab === 'overview' }"
                @click="poItemLinePanel.activeTab = 'overview'"
              >
                概况
              </button>
              <button
                type="button"
                class="tab-btn"
                :class="{ 'tab-btn--active': poItemLinePanel.activeTab === 'requisitions' }"
                @click="poItemLinePanel.activeTab = 'requisitions'"
              >
                {{ formatPoItemLineTabLabel('采购申请', 'requisitions') }}
              </button>
              <button
                type="button"
                class="tab-btn"
                :class="{ 'tab-btn--active': poItemLinePanel.activeTab === 'payments' }"
                @click="poItemLinePanel.activeTab = 'payments'"
              >
                {{ formatPoItemLineTabLabel('付款', 'payments') }}
              </button>
              <button
                type="button"
                class="tab-btn"
                :class="{ 'tab-btn--active': poItemLinePanel.activeTab === 'arrivals' }"
                @click="poItemLinePanel.activeTab = 'arrivals'"
              >
                {{ formatPoItemLineTabLabel('到货通知', 'arrivals') }}
              </button>
              <button
                type="button"
                class="tab-btn"
                :class="{ 'tab-btn--active': poItemLinePanel.activeTab === 'stockIns' }"
                @click="poItemLinePanel.activeTab = 'stockIns'"
              >
                {{ formatPoItemLineTabLabel('入库', 'stockIns') }}
              </button>
              <button
                type="button"
                class="tab-btn"
                :class="{ 'tab-btn--active': poItemLinePanel.activeTab === 'stocks' }"
                @click="poItemLinePanel.activeTab = 'stocks'"
              >
                {{ formatPoItemLineTabLabel('库存', 'stocks') }}
              </button>
              <button
                type="button"
                class="tab-btn"
                :class="{ 'tab-btn--active': poItemLinePanel.activeTab === 'purchaseInvoices' }"
                @click="poItemLinePanel.activeTab = 'purchaseInvoices'"
              >
                {{ formatPoItemLineTabLabel('进项发票', 'purchaseInvoices') }}
              </button>
              <button
                type="button"
                class="tab-btn"
                :class="{ 'tab-btn--active': poItemLinePanel.activeTab === 'qcImages' }"
                @click="poItemLinePanel.activeTab = 'qcImages'"
              >
                {{ formatPoItemLineTabLabel('质检图片', 'qcImages') }}
              </button>
            </div>
            <div class="tabs-body">
              <div v-show="poItemLinePanel.activeTab === 'overview'" class="so-line-overview-wrap">
                <table v-if="lineTabAggregates?.lineOverview" class="so-line-overview">
                  <colgroup>
                    <col class="so-line-overview__col-first" />
                    <col
                      v-for="col in poLineOverviewColumns"
                      :key="`cg-${col.key}`"
                      class="so-line-overview__col-data"
                    />
                  </colgroup>
                  <thead>
                    <tr>
                      <th class="so-line-overview__corner" />
                      <th
                        v-for="col in poLineOverviewColumns"
                        :key="col.key"
                        :class="['so-line-overview__col-head', poOverviewColHeadClass(col)]"
                      >
                        {{ col.label }}
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="row in poLineOverviewRows" :key="row.key">
                      <th class="so-line-overview__row-head">{{ row.label }}</th>
                      <td
                        v-for="col in poLineOverviewColumns"
                        :key="`${row.key}-${col.key}`"
                        class="so-line-overview__cell"
                        :class="{ 'so-line-overview__cell--right': col.isAmount }"
                      >
                        <span v-if="formatPoOverviewCell(col, row.key).type === 'dash'">—</span>
                        <span
                          v-else-if="formatPoOverviewCell(col, row.key).type === 'qty'"
                          class="so-line-overview__qty"
                        >{{ formatPoOverviewCell(col, row.key).text }}</span>
                        <span v-else class="amount-with-code">
                          <span>{{ formatPoOverviewCell(col, row.key).text }}</span>
                          <span
                            v-if="formatPoOverviewCell(col, row.key).currency != null"
                            :class="['amount-ccy', currencyCodeClass(formatPoOverviewCell(col, row.key).currency)]"
                          >{{ currencyCodeText(formatPoOverviewCell(col, row.key).currency) }}</span>
                        </span>
                      </td>
                    </tr>
                  </tbody>
                </table>
                <DetailListPanelEmpty v-else-if="!poItemLinePanel.loading" size="low" description="暂无概况数据" />
              </div>
              <div v-show="poItemLinePanel.activeTab === 'requisitions'" class="po-aggregate-table-wrap">
                <el-table
                  v-if="(lineTabAggregates?.purchaseRequisitions?.length ?? 0) > 0"
                  :data="lineTabAggregates?.purchaseRequisitions ?? []"
                  size="small"
                  stripe
                >
                  <el-table-column type="index" width="50" label="#" />
                  <el-table-column label="申请单号" min-width="180">
                    <template #default="{ row }">
                      <router-link class="po-tab-link" :to="`/purchase-requisitions/${row.id}`">{{ row.billCode }}</router-link>
                    </template>
                  </el-table-column>
                  <el-table-column label="状态" width="100">
                    <template #default="{ row }">{{ prStatusText(row?.status) }}</template>
                  </el-table-column>
                  <el-table-column label="销售订单号" min-width="140" show-overflow-tooltip>
                    <template #default="{ row }">
                      <router-link
                        v-if="row.sellOrderId && row.sellOrderCode"
                        class="po-tab-link"
                        :to="`/sales-orders/${row.sellOrderId}`"
                      >
                        {{ row.sellOrderCode }}
                      </router-link>
                      <span v-else-if="row.sellOrderCode">{{ row.sellOrderCode }}</span>
                      <span v-else>—</span>
                    </template>
                  </el-table-column>
                  <el-table-column prop="salesUserName" label="业务员" width="120" show-overflow-tooltip>
                    <template #default="{ row }">{{ row.salesUserName?.trim() || '—' }}</template>
                  </el-table-column>
                  <CrmCopyableTableColumn prop="pn" label="PN" min-width="140" />
                  <CrmCopyableTableColumn prop="brand" label="品牌" width="120" />
                  <el-table-column prop="qty" label="数量" width="100" align="right" />
                  <el-table-column label="预计采购" width="160">
                    <template #default="{ row }">{{ formatDateTime(row?.expectedPurchaseTime) }}</template>
                  </el-table-column>
                </el-table>
                <DetailListPanelEmpty v-else size="low" />
              </div>
              <div v-show="poItemLinePanel.activeTab === 'payments'" class="po-aggregate-table-wrap">
                <el-table v-if="(lineTabAggregates?.payments?.length ?? 0) > 0" :data="lineTabAggregates?.payments ?? []" size="small" stripe @row-dblclick="onPoPaymentRowDblClick">
                  <el-table-column type="index" width="50" label="#" />
                  <el-table-column label="付款单号" min-width="180">
                    <template #default="{ row }">
                      <router-link class="po-tab-link" :to="`/finance/payments/${row.id}`">{{ row.financePaymentCode }}</router-link>
                    </template>
                  </el-table-column>
                  <el-table-column prop="vendorName" label="供应商" min-width="160" show-overflow-tooltip />
                  <el-table-column label="状态" width="110">
                    <template #default="{ row }">{{ paymentStatusText(row?.status) }}</template>
                  </el-table-column>
                  <el-table-column v-if="canViewPurchaseAmount" label="待付金额" width="130" align="right">
                    <template #default="{ row }">{{ formatTotalAmountNumber(row?.paymentAmountToBe) }}</template>
                  </el-table-column>
                  <el-table-column v-if="canViewPurchaseAmount" label="已付金额" width="130" align="right">
                    <template #default="{ row }">{{ formatTotalAmountNumber(row?.paymentAmount) }}</template>
                  </el-table-column>
                <el-table-column label="付款日期" width="160">
                  <template #default="{ row }">{{ formatDateTime(row?.paymentDate) }}</template>
                </el-table-column>
                <el-table-column label="操作" width="240" fixed="right">
                  <template #default="{ row }">
                    <el-button
                      v-if="canEditPoPaymentRequest(row)"
                      link
                      type="primary"
                      size="small"
                      @click.stop="openPoPaymentEdit(row)"
                    >
                      编辑请款
                    </el-button>
                    <el-button
                      v-if="canWithdrawPoPayment(row)"
                      link
                      type="info"
                      size="small"
                      @click.stop="withdrawPoPayment(row)"
                    >
                      撤回
                    </el-button>
                    <el-button
                      v-if="canSubmitPoPaymentAudit(row)"
                      link
                      type="warning"
                      size="small"
                      @click.stop="submitPoPaymentAudit(row)"
                    >
                      提交审核
                    </el-button>
                    <el-button
                      v-if="canPayPoPayment(row)"
                      link
                      type="warning"
                      size="small"
                      @click.stop="openPoPaymentPay(row)"
                    >
                      付款
                    </el-button>
                  </template>
                </el-table-column>
              </el-table>
                <DetailListPanelEmpty v-else size="low" />
              </div>
              <div v-show="poItemLinePanel.activeTab === 'arrivals'" class="po-aggregate-table-wrap">
                <PurchaseOrderItemArrivalNoticeTabTable :items="lineTabAggregates?.arrivalNotices ?? []" />
              </div>
              <div v-show="poItemLinePanel.activeTab === 'stockIns'" class="po-aggregate-table-wrap">
                <SellOrderItemStockInTabTable :items="lineTabAggregates?.stockIns ?? []" />
              </div>
              <div v-show="poItemLinePanel.activeTab === 'stocks'" class="po-aggregate-table-wrap">
                <PurchaseOrderItemStockTabTable :items="lineTabAggregates?.stockItems ?? []" />
              </div>
              <div v-show="poItemLinePanel.activeTab === 'purchaseInvoices'" class="po-aggregate-table-wrap">
                <el-table v-if="(lineTabAggregates?.purchaseInvoices?.length ?? 0) > 0" :data="lineTabAggregates?.purchaseInvoices ?? []" size="small" stripe>
                  <el-table-column type="index" width="50" label="#" />
                  <el-table-column label="进项发票" min-width="180">
                    <template #default="{ row }">
                      <router-link class="po-tab-link" :to="`/finance/purchase-invoices/${row.id}`">{{ row.invoiceNo || row.id }}</router-link>
                    </template>
                  </el-table-column>
                  <el-table-column prop="vendorName" label="供应商" min-width="160" show-overflow-tooltip />
                  <el-table-column v-if="canViewPurchaseAmount" label="发票金额" width="120" align="right">
                    <template #default="{ row }">{{ formatTotalAmountNumber(row?.invoiceAmount) }}</template>
                  </el-table-column>
                  <el-table-column label="认证状态" width="100">
                    <template #default="{ row }">{{ Number(row?.confirmStatus) === 1 ? '已认证' : '未认证' }}</template>
                  </el-table-column>
                  <el-table-column label="开票日期" width="160">
                    <template #default="{ row }">{{ formatDateTime(row?.invoiceDate) }}</template>
                  </el-table-column>
                </el-table>
                <DetailListPanelEmpty v-else size="low" />
              </div>
              <div v-show="poItemLinePanel.activeTab === 'qcImages'" class="po-aggregate-table-wrap po-qc-images-wrap">
                <QcImagesReadonlyGallery :images="lineTabAggregates?.qcImages ?? []" empty-text="暂无质检图片" />
              </div>
            </div>
          </div>
        </div>
      </div>

      <PurchaseOrderStockInBatchPanel
        v-if="!maskPurchaseSensitiveFields"
        :purchase-order-id="String(order.id)"
        :purchase-order-code="order.purchaseOrderCode || ''"
      />
    </template>

    <el-empty v-else description="订单不存在" />

    <!-- 标签弹窗 -->
    <ApplyTagsDialog
      v-model="tagDialogVisible"
      entity-type="PURCHASE_ORDER"
      :entity-ids="order ? [order.id] : []"
      title="为采购订单添加标签"
      @success="refreshTags"
    />

    <PurchaseOrderItemLineDialogs ref="poItemLineDialogsRef" @success="onPoLineDialogSuccess" />

    <el-dialog v-model="ffDialogVisible" title="货代单号" width="480px" destroy-on-close>
      <p class="ff-dialog-hint">与外部货代系统一一对应，用于全链路追溯；留空并保存可清除。</p>
      <el-input
        v-model="ffDraft"
        maxlength="64"
        show-word-limit
        clearable
        placeholder="请输入货代单号"
        @keyup.enter="saveFreightForwarderOrderNo"
      />
      <template #footer>
        <button type="button" class="btn-ghost" @click="ffDialogVisible = false">取消</button>
        <button type="button" class="btn-primary" :disabled="ffSaving" @click="saveFreightForwarderOrderNo">
          {{ ffSaving ? '保存中…' : '保存' }}
        </button>
      </template>
    </el-dialog>

    <FinancePaymentRequestEditDialog
      v-model="poPaymentEditVisible"
      :payment-id="poPaymentEditId"
      @success="reloadPoItemLinePanelAggregates"
    />
    <FinancePaymentPayDialog
      v-model="poPaymentPayVisible"
      :payment="poPaymentPayRow"
      @success="reloadPoItemLinePanelAggregates"
    />

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, reactive, watch, nextTick, inject, defineAsyncComponent } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import {
  purchaseOrderApi,
  type PurchaseOrderDetailTabAggregates,
  type PurchaseOrderItemExtendRefreshResult,
  type PurchaseOrderFieldChangeLogRow,
  type PurchaseOrderDeletedItemRow,
  type PurchaseOrderLineOverviewAmountMetric,
  type PurchaseOrderLineOverviewQtyMetric,
  type PurchaseOrderVendorChangePreviewResult
} from '@/api/purchaseOrder'
import { favoriteApi } from '@/api/favorite'
import {
  PURCHASE_ORDER_FAVORITE_ENTITY_TYPE,
  PURCHASE_ORDER_FAVORITES_CHANGED_EVENT
} from '@/constants/purchaseOrderFavorites'
import {
  purchaseOrderReportAllowed,
  purchaseOrderAllowsArrivalNotice,
  normalizePurchaseOrderMainStatus
} from '@/constants/purchaseOrderStatus'
import { tagApi, type TagDefinitionDto } from '@/api/tag'
import { useAuthStore } from '@/stores/auth'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { usePurchaseOrderWriteGate, useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import { canChangePurchaseOrderVendor } from '@/utils/purchaseOrderStaffPickRules'
import TagListDisplay from '@/components/Tag/TagListDisplay.vue'
import ApplyTagsDialog from '@/components/Tag/ApplyTagsDialog.vue'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import { documentApi } from '@/api/document'
import VendorNameReadonlyText from '@/components/Vendor/VendorNameReadonlyText.vue'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatTotalAmountNumber, formatUnitPriceNumber } from '@/utils/moneyFormat'
import { getApiErrorMessage } from '@/utils/apiError'
import { onCrmDetailListRowDblClick } from '@/utils/crmDetailListRowDblClick'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import PurchaseOrderItemArrivalNoticeTabTable from '@/components/RFQ/PurchaseOrderItemArrivalNoticeTabTable.vue'
import SellOrderItemStockInTabTable from '@/components/RFQ/SellOrderItemStockInTabTable.vue'
import PurchaseOrderItemStockTabTable from '@/components/RFQ/PurchaseOrderItemStockTabTable.vue'
import { recordPurchaseOrderRecentView } from '@/utils/purchaseOrderRecentHistory'
import PurchaseOrderItemLineDialogs from '@/components/purchaseOrder/PurchaseOrderItemLineDialogs.vue'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import { usePurchaseOrderItemOpsPanelStore } from '@/stores/purchaseOrderItemOpsPanel'
import { buildPurchaseOrderDetailItemsColumns } from '@/composables/buildPurchaseOrderDetailItemsColumns'
import CrmDataTable from '@/components/CrmDataTable.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import QcImagesReadonlyGallery from '@/components/Logistics/QcImagesReadonlyGallery.vue'
import { financePaymentApi, type FinancePayment } from '@/api/finance'
import FinancePaymentRequestEditDialog from '@/components/Finance/FinancePaymentRequestEditDialog.vue'
import FinancePaymentPayDialog from '@/components/Finance/FinancePaymentPayDialog.vue'
import { useFinanceWriteGate } from '@/composables/useDepartmentDataReadOnly'

const PurchaseOrderStockInBatchPanel = defineAsyncComponent(
  () => import('@/components/Inventory/PurchaseOrderStockInBatchPanel.vue')
)

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const authStore = useAuthStore()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const purchaseOrderItemOpsStore = usePurchaseOrderItemOpsPanelStore()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { canWritePo } = usePurchaseOrderWriteGate()
const { canWriteFinancePayment: canFinancePaymentWrite } = useFinanceWriteGate()
const { canWriteLogisticsData } = useDepartmentDataReadOnly()

/** 与销售详情「刷新客户」对称：有换供应商权限且未脱敏时可刷新供应商 */
const canRefreshPoVendor = computed(
  () =>
    !maskPurchaseSensitiveFields.value &&
    canChangePurchaseOrderVendor({
      isSysAdmin: authStore.user?.isSysAdmin,
      identityType: authStore.user?.identityType,
      roleCodes: authStore.user?.roleCodes,
      hasPermission: (c) => authStore.hasPermission(c)
    })
)

const FF_EDITABLE_PO_STATUSES = new Set([10, 20, 30, 50, 100])
const showHeaderMoreMenu = computed(() => canWritePo.value)
const canEditFreightForwarderOrderNo = computed(() => {
  const o = order.value
  if (!o || !canWriteLogisticsData.value) return false
  const s = normalizePurchaseOrderMainStatus(o)
  return FF_EDITABLE_PO_STATUSES.has(s)
})

const ffDialogVisible = ref(false)
const ffDraft = ref('')
const ffSaving = ref(false)

const canViewVendorInfo = computed(
  () => !maskPurchaseSensitiveFields.value && authStore.hasPermission('vendor.info.read')
)
const canViewPurchaseAmount = computed(
  () => !maskPurchaseSensitiveFields.value && authStore.hasPermission('purchase.amount.read')
)

const poDetailItemsDensityAnchorEl = ref<HTMLElement | null>(null)
const poDetailItemsTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)

/** 《列表操作列规范》：默认收起；列头 `<` / `>` 切换列宽 */
const poDetailItemsOpColExpanded = ref(false)
/** 展开态操作列宽（在既有宽度上再 ×2/3） */
const PO_DETAIL_ITEMS_OP_COL_EXPANDED_WIDTH = 173
/** 收起态操作列宽（在既有宽度上再 ×2/3）；列头仅 < > 图标 */
const PO_DETAIL_ITEMS_OP_COL_COLLAPSED_WIDTH = 43
/** 展开态操作列 min-width（×2/3） */
const PO_DETAIL_ITEMS_OP_COL_EXPANDED_MIN_WIDTH = 160
const poDetailItemsOpColWidth = computed(() =>
  poDetailItemsOpColExpanded.value ? PO_DETAIL_ITEMS_OP_COL_EXPANDED_WIDTH : PO_DETAIL_ITEMS_OP_COL_COLLAPSED_WIDTH
)
const poDetailItemsOpColMinWidth = computed(() =>
  poDetailItemsOpColExpanded.value ? PO_DETAIL_ITEMS_OP_COL_EXPANDED_MIN_WIDTH : PO_DETAIL_ITEMS_OP_COL_COLLAPSED_WIDTH
)
function togglePoDetailItemsOpCol() {
  poDetailItemsOpColExpanded.value = !poDetailItemsOpColExpanded.value
}

const poDetailItemsColumns = computed(() =>
  buildPurchaseOrderDetailItemsColumns({
    canViewPurchaseAmount: canViewPurchaseAmount.value,
    opColWidth: poDetailItemsOpColWidth.value,
    opColMinWidth: poDetailItemsOpColMinWidth.value
  })
)
/** 与采购订单明细列表「通知到货」一致 */
const canCreateArrivalNotice = computed(() => authStore.hasPermission('purchase-order.read'))

const loading = ref(false)
const refreshingExtends = ref(false)
const syncingVendor = ref(false)
const order = ref<any>(null)

/** 与原列表「取消订单」一致：审核通过(10)前可标记取消(-2)；已为取消不可再点 */
const canCancelPurchaseOrderFromMenu = computed(() => {
  const o = order.value
  if (!o || !canWritePo.value) return false
  const s = normalizePurchaseOrderMainStatus(o)
  if (!Number.isFinite(s) || s === -2) return false
  return s < 10
})

const showPoHeaderTags = computed(() => canWritePo.value || currentTags.value.length > 0)

const poBasicCreateDateText = computed(() => {
  const o = order.value
  if (!o?.createTime) return '—'
  const s = formatDisplayDate(o.createTime)
  return s === '--' ? '—' : s
})

const poBasicCreateUserText = computed(() => {
  const o = order.value as Record<string, unknown> | null | undefined
  if (!o) return '—'
  const name = o.createUserName ?? o.CreateUserName ?? o.createdBy
  const s = name != null ? String(name).trim() : ''
  return s || '—'
})

const poFavorited = ref(false)
const favoriteLoading = ref(false)
const activeTab = ref('items')

const PO_DETAIL_TAB_KEYS = ['items', 'documents', 'changeLog', 'deleteLog'] as const

function applyPoDetailTabFromRoute() {
  const tab = route.query.tab
  if (typeof tab === 'string' && (PO_DETAIL_TAB_KEYS as readonly string[]).includes(tab)) {
    activeTab.value = tab
  }
}

watch(() => route.query.tab, applyPoDetailTabFromRoute, { immediate: true })

const documentCount = ref(0)
const changeLogs = ref<PurchaseOrderFieldChangeLogRow[]>([])
const deletedItems = ref<PurchaseOrderDeletedItemRow[]>([])
const changeLogsLoading = ref(false)
const deletedItemsLoading = ref(false)
const changeLogsLoaded = ref(false)
const deletedItemsLoaded = ref(false)

function resetOrderLogTabs() {
  changeLogs.value = []
  deletedItems.value = []
  changeLogsLoaded.value = false
  deletedItemsLoaded.value = false
  documentCount.value = 0
}

type OrderDetailTabKey = 'items' | 'documents' | 'changeLog' | 'deleteLog'

function orderDetailTabCount(tab: OrderDetailTabKey): number {
  switch (tab) {
    case 'items':
      return order.value?.items?.length ?? 0
    case 'documents':
      return documentCount.value
    case 'changeLog':
      return changeLogs.value.length
    case 'deleteLog':
      return deletedItems.value.length
    default:
      return 0
  }
}

/** Tab 标题旁显示 (N)，与底部采购订单明细详情面板一致 */
function formatOrderDetailTabLabel(label: string, tab: OrderDetailTabKey): string {
  const count = orderDetailTabCount(tab)
  return count > 0 ? `${label} (${count})` : label
}

async function fetchDocumentCount() {
  const id = String(order.value?.id ?? '').trim()
  if (!id) {
    documentCount.value = 0
    return
  }
  try {
    const res = await documentApi.getDocuments('PURCHASE_ORDER', id)
    documentCount.value = Array.isArray(res) ? res.length : 0
  } catch {
    documentCount.value = 0
  }
}

function onPoDocumentUploaded() {
  docListRef.value?.refresh()
  void fetchDocumentCount()
}

async function loadChangeLogs(opts?: { silent?: boolean }) {
  const id = String(order.value?.id ?? '').trim()
  if (!id) return
  changeLogsLoading.value = true
  try {
    changeLogs.value = (await purchaseOrderApi.getChangeLogs(id)) ?? []
    changeLogsLoaded.value = true
  } catch (e: unknown) {
    if (!opts?.silent) ElMessage.error(getApiErrorMessage(e, '加载更改日志失败'))
  } finally {
    changeLogsLoading.value = false
  }
}

async function loadDeletedItems(opts?: { silent?: boolean }) {
  const id = String(order.value?.id ?? '').trim()
  if (!id) return
  deletedItemsLoading.value = true
  try {
    deletedItems.value = (await purchaseOrderApi.getDeletedItems(id)) ?? []
    deletedItemsLoaded.value = true
  } catch (e: unknown) {
    if (!opts?.silent) ElMessage.error(getApiErrorMessage(e, '加载删除日志失败'))
  } finally {
    deletedItemsLoading.value = false
  }
}

function formatDeletedPoItemCost(row: PurchaseOrderDeletedItemRow) {
  const cost = Number(row?.cost)
  if (!Number.isFinite(cost)) return '—'
  const cur = Number(row?.currency)
  const curLabel = CURRENCY_CODE_TO_TEXT[cur] ?? (cur > 0 ? String(cur) : '')
  return curLabel ? `${cost.toFixed(4)} ${curLabel}` : cost.toFixed(4)
}

watch(activeTab, (tab) => {
  if (tab === 'changeLog' && !changeLogsLoaded.value) void loadChangeLogs()
  if (tab === 'deleteLog' && !deletedItemsLoaded.value) void loadDeletedItems()
})

/** 双击订单明细行：底部面板（按采购明细主键） */
const lineTabAggregates = ref<PurchaseOrderDetailTabAggregates | null>(null)

type PoItemLineTabKey =
  | 'overview'
  | 'requisitions'
  | 'payments'
  | 'arrivals'
  | 'stockIns'
  | 'stocks'
  | 'purchaseInvoices'
  | 'qcImages'

function poItemLineTabRecordCount(tab: PoItemLineTabKey): number {
  const agg = lineTabAggregates.value
  if (!agg) return 0
  switch (tab) {
    case 'overview':
      return 0
    case 'requisitions':
      return agg.purchaseRequisitions?.length ?? 0
    case 'payments':
      return agg.payments?.length ?? 0
    case 'arrivals':
      return agg.arrivalNotices?.length ?? 0
    case 'stockIns':
      return agg.stockIns?.length ?? 0
    case 'stocks':
      return agg.stockItems?.length ?? 0
    case 'purchaseInvoices':
      return agg.purchaseInvoices?.length ?? 0
    case 'qcImages':
      return agg.qcImages?.length ?? 0
    default:
      return 0
  }
}

/** Tab 标题旁显示 (N)，便于未点开 Tab 时感知是否有数据 */
function formatPoItemLineTabLabel(label: string, tab: PoItemLineTabKey): string {
  const count = poItemLineTabRecordCount(tab)
  return count > 0 ? `${label} (${count})` : label
}

type PoLineOverviewColumnKey =
  | 'lineAmount'
  | 'lineQty'
  | 'payment'
  | 'arrivalNotice'
  | 'stockIn'
  | 'purchaseInvoice'

type PoOverviewHeaderTone = 'none' | 'gray' | 'yellow' | 'green' | 'red'

type PoLineOverviewColumnDef = {
  key: PoLineOverviewColumnKey
  label: string
  isAmount: boolean
  colorize: boolean
  metric: PurchaseOrderLineOverviewQtyMetric | PurchaseOrderLineOverviewAmountMetric | { total: number; currency?: number }
}

const poLineOverviewRows = [
  { key: 'total' as const, label: '总数' },
  { key: 'done' as const, label: '已执行' },
  { key: 'pending' as const, label: '待处理' }
]

const poLineOverviewColumns = computed<PoLineOverviewColumnDef[]>(() => {
  const o = lineTabAggregates.value?.lineOverview
  if (!o) return []
  return [
    {
      key: 'lineAmount',
      label: '采购订单明细总额',
      isAmount: true,
      colorize: false,
      metric: o.lineAmount
    },
    {
      key: 'lineQty',
      label: '采购订单明细数量',
      isAmount: false,
      colorize: false,
      metric: o.lineQty
    },
    {
      key: 'payment',
      label: '付款',
      isAmount: true,
      colorize: true,
      metric: o.payment
    },
    {
      key: 'arrivalNotice',
      label: '到货通知',
      isAmount: false,
      colorize: true,
      metric: o.arrivalNotice
    },
    {
      key: 'stockIn',
      label: '入库',
      isAmount: false,
      colorize: true,
      metric: o.stockIn
    },
    {
      key: 'purchaseInvoice',
      label: '进项发票',
      isAmount: true,
      colorize: true,
      metric: o.purchaseInvoice
    }
  ]
})

function poOverviewMetricDone(metric: PoLineOverviewColumnDef['metric']): number {
  if (!('done' in metric) || metric.done == null) return 0
  return Number(metric.done) || 0
}

function poOverviewMetricTotal(metric: PoLineOverviewColumnDef['metric']): number {
  return Number(metric.total) || 0
}

function poOverviewHeaderTone(col: PoLineOverviewColumnDef): PoOverviewHeaderTone {
  if (!col.colorize) return 'none'
  const total = poOverviewMetricTotal(col.metric)
  const done = poOverviewMetricDone(col.metric)
  if (done > total + 1e-9) return 'red'
  if (done <= 1e-9) return 'gray'
  if (done + 1e-9 >= total) return 'green'
  return 'yellow'
}

function poOverviewColHeadClass(col: PoLineOverviewColumnDef): string {
  const tone = poOverviewHeaderTone(col)
  if (tone === 'none') return ''
  return `so-line-overview__col-head--${tone}`
}

function formatPoOverviewQty(v: number) {
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  if (Math.abs(n - Math.round(n)) < 1e-9) return String(Math.round(n))
  return n.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 4 })
}

function formatPoOverviewCell(
  col: PoLineOverviewColumnDef,
  rowKey: 'total' | 'done' | 'pending'
): { type: 'dash' | 'qty' | 'amount'; text: string; currency?: number } {
  const metric = col.metric
  if (!col.colorize && rowKey !== 'total') {
    return { type: 'dash', text: '—' }
  }

  let raw: number | undefined
  if (rowKey === 'total') raw = metric.total
  else if ('done' in metric && rowKey === 'done') raw = metric.done
  else if ('pending' in metric && rowKey === 'pending') raw = metric.pending

  if (raw == null) return { type: 'dash', text: '—' }

  if (col.isAmount) {
    if (!canViewPurchaseAmount.value) return { type: 'dash', text: '—' }
    const currency = 'currency' in metric ? metric.currency : undefined
    return { type: 'amount', text: formatTotalAmountNumber(raw), currency }
  }

  return { type: 'qty', text: formatPoOverviewQty(raw) }
}

const poItemLinePanel = reactive({
  visible: false,
  purchaseOrderItemId: '',
  purchaseOrderItemCode: '',
  activeTab: 'overview' as
    | 'overview'
    | 'requisitions'
    | 'payments'
    | 'arrivals'
    | 'stockIns'
    | 'stocks'
    | 'purchaseInvoices'
    | 'qcImages',
  loading: false,
  loadError: ''
})

const poPaymentEditVisible = ref(false)
const poPaymentEditId = ref<string | null>(null)
const poPaymentPayVisible = ref(false)
const poPaymentPayRow = ref<FinancePayment | null>(null)

function poPaymentRowId(row: { id?: string }) {
  return String(row?.id ?? '').trim()
}

function poPaymentRowStatus(row: { status?: number }) {
  return Number(row?.status ?? 0)
}

function poPaymentRowCreatorId(row: Record<string, unknown>) {
  return String(row?.createByUserId ?? row?.CreateByUserId ?? '').trim()
}

function canEditPoPaymentRequest(row: { status?: number }) {
  const s = poPaymentRowStatus(row)
  if (s !== 1 && s !== -1) return false
  return canWritePo.value || canFinancePaymentWrite.value
}

function canSubmitPoPaymentAudit(row: { status?: number }) {
  return poPaymentRowStatus(row) === 1 && (canWritePo.value || canFinancePaymentWrite.value)
}

function canPayPoPayment(row: { status?: number }) {
  return canFinancePaymentWrite.value && poPaymentRowStatus(row) === 10
}

function canWithdrawPoPayment(row: Record<string, unknown>) {
  if (poPaymentRowStatus(row) !== 10) return false
  if (canFinancePaymentWrite.value) return true
  const uid = String(authStore.user?.id ?? '').trim()
  const creator = poPaymentRowCreatorId(row)
  return !!uid && !!creator && uid === creator
}

function openPoPaymentEdit(row: { id?: string }) {
  poPaymentEditId.value = poPaymentRowId(row) || null
  poPaymentEditVisible.value = true
}

function onPoPaymentRowDblClick(row: { id?: string; status?: number }, _column: unknown, event?: MouseEvent) {
  onCrmDetailListRowDblClick(row, _column, event, {
    canEdit: canEditPoPaymentRequest(row),
    onEdit: openPoPaymentEdit,
  })
}

async function openPoPaymentPay(row: { id?: string }) {
  const id = poPaymentRowId(row)
  if (!id) return
  try {
    poPaymentPayRow.value = await financePaymentApi.getById(id)
    poPaymentPayVisible.value = true
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, '加载付款单失败'))
  }
}

async function reloadPoItemLinePanelAggregates() {
  const oid = String(route.params.id ?? '').trim()
  const purchaseOrderItemId = poItemLinePanel.purchaseOrderItemId
  if (!oid || !purchaseOrderItemId || !poItemLinePanel.visible) return
  try {
    lineTabAggregates.value = await purchaseOrderApi.getPurchaseOrderItemDetailTabAggregates(oid, purchaseOrderItemId)
    const row = findPurchaseOrderItemRow(purchaseOrderItemId)
    if (row) syncOpsPanelFromLinePanel(row)
  } catch {
    /* 刷新失败时保留原列表 */
  }
}

async function withdrawPoPayment(row: Record<string, unknown>) {
  const id = poPaymentRowId(row)
  const code = String(row?.financePaymentCode ?? id)
  if (!id) return
  try {
    await ElMessageBox.confirm(
      `撤回后付款执行信息与水单附件将被清除，需修改后重新提交审批。确认撤回付款单 ${code}？`,
      '撤回请款',
      { type: 'warning' }
    )
  } catch {
    return
  }
  try {
    await financePaymentApi.withdraw(id)
    ElMessage.success('已撤回，请修改后重新提交审批')
    await reloadPoItemLinePanelAggregates()
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, '撤回失败'))
  }
}

async function submitPoPaymentAudit(row: Record<string, unknown>) {
  const id = poPaymentRowId(row)
  const code = String(row?.financePaymentCode ?? id)
  if (!id) return
  try {
    await ElMessageBox.confirm(`确认提交付款单 ${code} 审核？`, '提交审核', { type: 'info' })
  } catch {
    return
  }
  try {
    await financePaymentApi.submit(id)
    ElMessage.success('已提交审核')
    await reloadPoItemLinePanelAggregates()
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, '操作失败'))
  }
}

function closePoItemLinePanel() {
  poItemLinePanel.visible = false
  poItemLinePanel.loadError = ''
  lineTabAggregates.value = null
}

async function selectPurchaseOrderItemRow(row: Record<string, unknown>) {
  if (maskPurchaseSensitiveFields.value) return
  const oid = String(route.params.id ?? '').trim()
  const purchaseOrderItemId = String(
    row?.id ?? row?.Id ?? row?.purchaseOrderItemId ?? row?.PurchaseOrderItemId ?? ''
  ).trim()
  const purchaseOrderItemCode = String(row?.purchaseOrderItemCode ?? '').trim()
  if (!oid || !purchaseOrderItemId) return
  // 不强制切换右侧「操作」页签；底部明细面板照常打开。右栏仅在已展开且为「操作」时展示同步数据
  poItemLinePanel.purchaseOrderItemId = purchaseOrderItemId
  poItemLinePanel.purchaseOrderItemCode = purchaseOrderItemCode || purchaseOrderItemId
  poItemLinePanel.visible = true
  poItemLinePanel.activeTab = 'overview'
  poItemLinePanel.loading = true
  poItemLinePanel.loadError = ''
  lineTabAggregates.value = null
  const opsRow = toOpsPanelRow(row)
  if (opsRow) purchaseOrderItemOpsStore.syncRowAndAggregates(opsRow, null)
  try {
    lineTabAggregates.value = await purchaseOrderApi.getPurchaseOrderItemDetailTabAggregates(oid, purchaseOrderItemId)
    syncOpsPanelFromLinePanel(row)
  } catch (e: unknown) {
    poItemLinePanel.loadError = getApiErrorMessage(e, '加载明细关联数据失败')
    syncOpsPanelFromLinePanel(row, poItemLinePanel.loadError)
  } finally {
    poItemLinePanel.loading = false
  }
}

async function onPurchaseOrderItemRowClick(row: Record<string, unknown>) {
  await selectPurchaseOrderItemRow(row)
}

async function onPurchaseOrderItemRowDblClick(row: Record<string, unknown>) {
  await selectPurchaseOrderItemRow(row)
}

function poItemRowClassName({ row }: { row: Record<string, unknown> }) {
  const key = poItemRowKey(row)
  const bottomActive = poItemLinePanel.visible && key === poItemLinePanel.purchaseOrderItemId
  const opsActive =
    !!purchaseOrderItemOpsStore.row &&
    purchaseOrderItemOpsStore.rowKey(row) === purchaseOrderItemOpsStore.rowKey(purchaseOrderItemOpsStore.row)
  return bottomActive || opsActive ? 'po-item-row--active' : ''
}

const docListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)
const docUploadRef = ref<{ addDroppedFiles: (files: File[]) => void } | null>(null)
const docTabDragging = ref(false)
const docTabDragDepth = ref(0)

watch(maskPurchaseSensitiveFields, (m) => {
  if (m && (activeTab.value === 'documents' || activeTab.value === 'changeLog' || activeTab.value === 'deleteLog')) {
    activeTab.value = 'items'
  }
  if (m) {
    closePoItemLinePanel()
    purchaseOrderItemOpsStore.clear()
  }
})

function onDocTabDragOver() {
  docTabDragDepth.value = Math.max(1, docTabDragDepth.value)
  docTabDragging.value = true
}

function onDocTabDragLeave() {
  docTabDragDepth.value = Math.max(0, docTabDragDepth.value - 1)
  if (docTabDragDepth.value === 0) docTabDragging.value = false
}

function onDocTabDrop(e: DragEvent) {
  docTabDragDepth.value = 0
  docTabDragging.value = false
  const files = e.dataTransfer?.files ? Array.from(e.dataTransfer.files) : []
  if (!files.length) return
  docUploadRef.value?.addDroppedFiles(files)
}

// 标签
const currentTags = ref<TagDefinitionDto[]>([])
const tagDialogVisible = ref(false)

const poItemLineDialogsRef = ref<InstanceType<typeof PurchaseOrderItemLineDialogs> | null>(null)

const orderId = computed(() => route.params.id as string)

function poItemRowKey(row: any) {
  return String(row?.id ?? row?.Id ?? row?.purchaseOrderItemId ?? row?.PurchaseOrderItemId ?? '')
}

function findPurchaseOrderItemRow(itemId: string) {
  const lines = order.value?.items
  if (!Array.isArray(lines) || !itemId) return undefined
  return lines.find((it) => poItemRowKey(it) === itemId) as Record<string, unknown> | undefined
}

async function focusPurchaseOrderItemRow(row: Record<string, unknown>) {
  activeTab.value = 'items'
  await selectPurchaseOrderItemRow(row)
}

async function applyInitialPurchaseOrderItemSelection() {
  if (maskPurchaseSensitiveFields.value) return
  const lines = order.value?.items
  if (!Array.isArray(lines) || lines.length === 0) return
  const fromQuery = String(route.query.purchaseOrderItemId ?? '').trim()
  const hit = fromQuery ? findPurchaseOrderItemRow(fromQuery) : undefined
  const row = (hit ?? lines[0]) as Record<string, unknown>
  await focusPurchaseOrderItemRow(row)
}

/** 扩展表进度：0=待 1=部分 2=完成 */
function poExtendTriTagType(v?: number): '' | 'info' | 'success' | 'warning' | 'danger' {
  const map: Record<number, '' | 'info' | 'success' | 'warning' | 'danger'> = {
    0: 'info',
    1: 'warning',
    2: 'success'
  }
  return v !== undefined && v !== null ? (map[v] ?? 'info') : 'info'
}
function poPurchaseProgressText(v?: number) {
  const map: Record<number, string> = { 0: '待采购', 1: '采购中', 2: '采购完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}
function poStockInProgressText(v?: number) {
  const map: Record<number, string> = { 0: '待入库', 1: '部分入库', 2: '入库完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}
function poPaymentProgressText(v?: number) {
  const map: Record<number, string> = { 0: '待付款', 1: '部分付款', 2: '付款完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}
function poInvoiceProgressText(v?: number) {
  const map: Record<number, string> = { 0: '待开票', 1: '部分开票', 2: '开票完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}

function formatPoProgressQty(q: unknown): string {
  const n = Number(q)
  if (!Number.isFinite(n)) return '--'
  return n.toLocaleString(undefined, { maximumFractionDigits: 4 })
}

/** 同一销售明细拆成多行采购时，展示「同销单行」累计有效采购数量 */
function poShowSellLinePurchaseSum(row: any): boolean {
  const a = Number(row?.purchaseProgressQty)
  const b = Number(row?.sellLinePurchaseQtySum)
  if (!Number.isFinite(b) || b <= 0) return false
  if (!Number.isFinite(a)) return true
  return Math.abs(b - a) > 1e-6
}

/** 将详情接口返回的明细行转为与「采购订单明细」列表行一致的结构，供通知到货 / 申请付款弹窗使用 */
function poDetailLineToListShape(it: any) {
  const o = order.value
  if (!o) return null
  const qty = Number(it.qty ?? it.Qty ?? 0)
  const cost = Number(it.cost ?? it.Cost ?? 0)
  return {
    purchaseOrderItemId: String(
      it.purchaseOrderItemId ?? it.PurchaseOrderItemId ?? it.id ?? it.Id ?? ''
    ),
    purchaseOrderItemCode: String(
      it.purchaseOrderItemCode ??
        it.PurchaseOrderItemCode ??
        it.purchaseOrderItemId ??
        it.PurchaseOrderItemId ??
        it.id ??
        it.Id ??
        ''
    ).trim(),
    purchaseOrderId: String(o.id),
    purchaseOrderCode: o.purchaseOrderCode,
    vendorId: o.vendorId,
    vendorName: o.vendorName,
    vendorEnglishName: o.vendorEnglishName,
    purchaseUserName: o.purchaseUserName,
    itemStatus: Number(it.status ?? it.Status ?? 0),
    orderStatus: Number(o.status ?? o.Status ?? 0),
    pn: it.pn ?? it.PN,
    brand: it.brand ?? it.Brand,
    qty,
    cost,
    lineTotal: qty * cost,
    paymentRequestedAmount: Number(it.paymentRequestedAmount ?? it.PaymentRequestedAmount ?? 0),
    qtyStockInNotifyExpectSum: Number(
      it.qtyStockInNotifyExpectSum ?? it.QtyStockInNotifyExpectSum ?? 0
    ),
    qtyStockInNotifyNot: Number(it.qtyStockInNotifyNot ?? it.QtyStockInNotifyNot ?? qty),
    currency: it.currency ?? it.Currency ?? o.currency,
    deliveryDate: it.deliveryDate ?? it.DeliveryDate ?? o.deliveryDate,
    canApplyPayment: Boolean(it.canApplyPayment ?? it.CanApplyPayment)
  }
}

function toOpsPanelRow(row: Record<string, unknown>): Record<string, unknown> | null {
  const shaped = poDetailLineToListShape(row)
  if (shaped) return shaped
  const oid = String(route.params.id ?? '').trim()
  const purchaseOrderItemId = poItemRowKey(row)
  if (!oid || !purchaseOrderItemId) return null
  return {
    ...row,
    purchaseOrderId: String(row.purchaseOrderId ?? oid).trim() || oid,
    purchaseOrderItemId
  }
}

function syncOpsPanelFromLinePanel(row: Record<string, unknown>, error = '') {
  const opsRow = toOpsPanelRow(row)
  if (!opsRow) return
  purchaseOrderItemOpsStore.syncRowAndAggregates(opsRow, lineTabAggregates.value, { error })
}

function poLineShowArrival(row: any) {
  const line = poDetailLineToListShape(row)
  return !!(line && canWritePo.value && canCreateArrivalNotice.value && purchaseOrderAllowsArrivalNotice(line))
}

function poLineShowPayment(row: any) {
  const line = poDetailLineToListShape(row)
  return !!(line && canWritePo.value && line.canApplyPayment)
}

function goPoItemLines(row: any) {
  const pn = String(row?.pn ?? row?.PN ?? '').trim()
  router.push({
    name: 'PurchaseOrderItemList',
    query: pn ? { pn } : {}
  })
}

function openPoLineArrival(row: any) {
  const line = poDetailLineToListShape(row)
  if (!line) return
  poItemLineDialogsRef.value?.openArrival(line)
}

function openPoLinePayment(row: any) {
  const line = poDetailLineToListShape(row)
  if (!line) return
  poItemLineDialogsRef.value?.openPayment(line)
}

const isStockingPurchaseOrder = computed(() => Number(order.value?.type) === 2)
const isPayLaterPurchaseOrder = computed(() => Boolean(order.value?.isPayLater))

const captionAvatarChar = computed(() => {
  const o = order.value
  if (!o) return '采'
  if (canViewVendorInfo.value && o.vendorName?.trim()) {
    const v = String(o.vendorName).trim()
    return (v && v[0]) || '采'
  }
  const code = String(o.purchaseOrderCode ?? '').trim()
  return (code && code[0]) || '采'
})

function onHeaderMoreCommand(cmd: string) {
  if (cmd === 'delete') void handleDeleteOrder()
}

function openFreightForwarderOrderNoDialog() {
  if (!canEditFreightForwarderOrderNo.value) return
  ffDraft.value = String(order.value?.freightForwarderOrderNo ?? '').trim()
  ffDialogVisible.value = true
}

async function saveFreightForwarderOrderNo() {
  if (!order.value?.id || ffSaving.value) return
  ffSaving.value = true
  try {
    const trimmed = ffDraft.value.trim()
    await purchaseOrderApi.updateFreightForwarderOrderNo(order.value.id, trimmed || null)
    ElMessage.success('货代单号已保存')
    ffDialogVisible.value = false
    await fetchOrder()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '保存失败'))
  } finally {
    ffSaving.value = false
  }
}

async function handleCancelPurchaseOrder() {
  if (!order.value?.id || !canCancelPurchaseOrderFromMenu.value) return
  try {
    await ElMessageBox.confirm(
      `确认将采购订单 ${order.value.purchaseOrderCode} 标记为「取消」吗？`,
      '取消订单',
      { type: 'warning', confirmButtonText: '确认', cancelButtonText: '关闭' }
    )
    await purchaseOrderApi.updateStatus(order.value.id, -2)
    ElMessage.success('订单已取消')
    await fetchOrder()
  } catch {
    /* 取消 */
  }
}

async function handleDeleteOrder() {
  if (!order.value?.id) return
  try {
    await ElMessageBox.confirm(
      `确定要删除采购订单 ${order.value.purchaseOrderCode} 吗？`,
      '删除确认',
      { type: 'warning', confirmButtonText: '删除', cancelButtonText: '取消' }
    )
    await purchaseOrderApi.delete(order.value.id)
    ElMessage.success('已删除')
    router.push({ name: 'PurchaseOrderList' })
  } catch {
    /* 取消 */
  }
}

function handleGoReport() {
  if (!order.value?.id) return
  if (!purchaseOrderReportAllowed(normalizePurchaseOrderMainStatus(order.value))) {
    ElMessage.warning('仅供应商已确认后的采购订单可生成采购单报表')
    return
  }
  router.push({ name: 'PurchaseOrderReport', params: { id: String(order.value.id) } })
}

function poRefreshStatusText(v: string) {
  const n = Number(v)
  const map: Record<number, string> = { 0: '待', 1: '部分', 2: '完成' }
  return Number.isFinite(n) ? (map[n] ?? v) : v
}

function poRefreshFieldValueText(field: string, value: string) {
  if (
    field === 'purchaseProgressStatus' ||
    field === 'stockInProgressStatus' ||
    field === 'paymentProgressStatus' ||
    field === 'invoiceProgressStatus'
  ) {
    return poRefreshStatusText(value)
  }
  return value
}

function hasPurchaseRefreshUpdates(result: PurchaseOrderItemExtendRefreshResult | null | undefined) {
  if (!result) return false
  const downstream =
    Number(result.arrivalNoticesUpdated ?? 0)
    + Number(result.stockInItemsUpdated ?? 0)
    + Number(result.stockInHeadersUpdated ?? 0)
    + Number(result.stockInItemExtendsUpdated ?? 0)
    + Number(result.stockItemsUpdated ?? 0)
    + Number(result.stockOutItemExtendsUpdated ?? 0)
    + (result.purchasePriceLineChanges?.length ?? 0)
    + (result.invoiceMatchWarnings?.length ?? 0)
    + (result.paymentOverWarnings?.length ?? 0)
  return result.changedItems > 0 || result.changedFieldsCount > 0 || downstream > 0
}

function buildRefreshResultHtml(result: PurchaseOrderItemExtendRefreshResult) {
  const syncedPrCount = Number(result.syncedPurchaseRequisitionStatusCount ?? 0)
  const syncedArrivalCount = Number(result.syncedArrivalNoticeStatusCount ?? 0)
  const lines: string[] = [
    `共 ${result.changedItems} 条明细发生更新，${result.changedFieldsCount} 个字段已变更。`,
    `已同步回写 ${syncedPrCount} 条采购申请状态。`,
    `已同步回写 ${syncedArrivalCount} 条到货通知状态。`,
    t('purchaseOrderDetail.refreshDownstreamSummary', {
      notices: Number(result.arrivalNoticesUpdated ?? 0),
      stockIn: Number(result.stockInItemsUpdated ?? 0),
      stockInHead: Number(result.stockInHeadersUpdated ?? 0),
      stock: Number(result.stockItemsUpdated ?? 0),
      outItem: Number(result.stockOutItemExtendsUpdated ?? 0)
    }),
    ''
  ]
  for (const price of result.purchasePriceLineChanges ?? []) {
    lines.push(
      t('purchaseOrderDetail.refreshPriceLine', {
        code: price.purchaseOrderItemCode || price.purchaseOrderItemId,
        before: String(price.oldCost),
        after: String(price.newCost)
      })
    )
  }
  for (const warning of result.invoiceMatchWarnings ?? []) {
    lines.push(
      t('purchaseOrderDetail.refreshOverInvoice', {
        code: warning.stockInItemCode || warning.stockInItemId,
        done: String(warning.invoiceMatchDone),
        amount: String(warning.amount),
        toBe: String(warning.invoiceMatchToBe)
      })
    )
  }
  for (const warning of result.paymentOverWarnings ?? []) {
    lines.push(
      t('purchaseOrderDetail.refreshOverPayment', {
        code: warning.purchaseOrderItemCode || warning.purchaseOrderItemId,
        done: String(warning.paymentDone),
        amount: String(warning.lineAmount)
      })
    )
  }
  if (
    (result.purchasePriceLineChanges?.length ?? 0) > 0
    || (result.invoiceMatchWarnings?.length ?? 0) > 0
    || (result.paymentOverWarnings?.length ?? 0) > 0
  ) {
    lines.push('')
  }
  for (const change of result.changes) {
    const lineCode = change.purchaseOrderItemCode || change.purchaseOrderItemId
    lines.push(`【${lineCode}】`)
    for (const field of change.fields) {
      const beforeText = poRefreshFieldValueText(field.field, field.before)
      const afterText = poRefreshFieldValueText(field.field, field.after)
      lines.push(`- ${field.label}: ${beforeText} -> ${afterText}`)
    }
    lines.push('')
  }
  const escaped = lines
    .join('\n')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/\n/g, '<br/>')
  return `<div style="max-height:420px;overflow:auto;line-height:1.7;">${escaped}</div>`
}

async function handleRefreshItemExtends() {
  if (!order.value?.id || refreshingExtends.value) return
  try {
    await ElMessageBox.confirm(
      t('purchaseOrderDetail.refreshConfirm'),
      t('purchaseOrderDetail.refreshConfirmTitle'),
      {
        type: 'warning',
        confirmButtonText: t('purchaseOrderDetail.refresh'),
        cancelButtonText: t('common.cancel')
      }
    )
  } catch {
    return
  }

  refreshingExtends.value = true
  try {
    const result = await purchaseOrderApi.refreshItemExtends(order.value.id)
    await fetchOrder()
    await reloadPoItemLinePanelAggregates()
    if (!hasPurchaseRefreshUpdates(result)) {
      await ElMessageBox.alert(
        t('purchaseOrderDetail.refreshResultEmpty'),
        t('purchaseOrderDetail.refreshResultTitle'),
        { confirmButtonText: t('common.confirm') }
      )
      return
    }
    await ElMessageBox.alert(buildRefreshResultHtml(result), t('purchaseOrderDetail.refreshResultTitle'), {
      dangerouslyUseHTMLString: true,
      confirmButtonText: t('common.confirm')
    })
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, '刷新失败，请稍后重试'))
  } finally {
    refreshingExtends.value = false
  }
}

function escapeVendorSyncHtml(value: string) {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
}

function buildVendorSyncPreviewHtml(preview: PurchaseOrderVendorChangePreviewResult) {
  const lines: string[] = [
    `<div>目标供应商：${escapeVendorSyncHtml(preview.newVendorName?.trim() || preview.newVendorId || '—')}</div>`
  ]
  if ((preview.poVendorNameToSync ?? 0) > 0) {
    lines.push(
      `<div>采购订单名称快照：${escapeVendorSyncHtml(preview.oldVendorName || '—')} → ${escapeVendorSyncHtml(preview.newVendorName || '—')}</div>`
    )
  }
  if (preview.poItemsToSync > 0) lines.push(`<div>采购明细 ${preview.poItemsToSync} 条</div>`)
  if (preview.arrivalNoticesToSync > 0) lines.push(`<div>到货通知 ${preview.arrivalNoticesToSync} 条</div>`)
  if (preview.stockInsToSync > 0) lines.push(`<div>未过账入库单 ${preview.stockInsToSync} 张</div>`)
  if (preview.paymentsToSync > 0) lines.push(`<div>未完成付款单 ${preview.paymentsToSync} 张</div>`)
  if (preview.purchaseInvoicesToSync > 0) {
    lines.push(`<div>未完成进项发票 ${preview.purchaseInvoicesToSync} 张</div>`)
  }
  if (preview.blockingDocuments?.length) {
    lines.push('<div style="margin-top:8px;">阻断原因：</div>')
    for (const d of preview.blockingDocuments) {
      lines.push(`<div>· ${escapeVendorSyncHtml(d)}</div>`)
    }
  } else if (preview.blockReason) {
    lines.push(`<div style="margin-top:8px;">${escapeVendorSyncHtml(preview.blockReason)}</div>`)
  }
  return `<div style="line-height:1.7;">${lines.join('')}</div>`
}

const vendorSyncMessageBoxOptions = {
  dangerouslyUseHTMLString: true
} as const

function handleRefreshMenuCommand(command: string | number | object) {
  if (command === 'vendor') {
    void handleRefreshVendor()
  }
}

/** 对齐销售详情「刷新客户」：按已落库 VendorId 刷头名称 + 未完结下游 */
async function handleRefreshVendor() {
  if (!order.value?.id || syncingVendor.value) return
  const vendorId = String(order.value.vendorId ?? '').trim()
  if (!vendorId) {
    await ElMessageBox.alert('采购订单未设置有效供应商，无法刷新。', '刷新供应商', {
      confirmButtonText: '知道了'
    })
    return
  }

  syncingVendor.value = true
  let preview: PurchaseOrderVendorChangePreviewResult | null = null
  try {
    preview = await purchaseOrderApi.previewVendorChange(order.value.id, vendorId)
  } catch (e) {
    await ElMessageBox.alert(getApiErrorMessage(e, '预检失败，请稍后重试'), '刷新供应商', {
      confirmButtonText: '知道了'
    })
    return
  } finally {
    syncingVendor.value = false
  }

  if (!preview) return

  if (!preview.canChange) {
    await ElMessageBox.alert(buildVendorSyncPreviewHtml(preview), '无法同步供应商', {
      ...vendorSyncMessageBoxOptions,
      confirmButtonText: '知道了'
    })
    return
  }

  if (preview.noOp) {
    await ElMessageBox.alert(
      '采购订单供应商名称快照与未完结下游供应商信息已与当前 VendorId 主数据一致，无需同步。',
      '刷新供应商',
      { confirmButtonText: '知道了' }
    )
    return
  }

  try {
    await ElMessageBox.confirm(
      buildVendorSyncPreviewHtml(preview),
      `确认按采购订单 ${order.value.purchaseOrderCode} 的 VendorId 刷新名称快照，并同步未完结下游供应商吗？`,
      {
        ...vendorSyncMessageBoxOptions,
        type: 'warning',
        confirmButtonText: '同步',
        cancelButtonText: '取消'
      }
    )
  } catch {
    return
  }

  syncingVendor.value = true
  try {
    const result = await purchaseOrderApi.refreshVendorName(order.value.id)
    await fetchOrder()
    await reloadPoItemLinePanelAggregates()
    const p = preview
    const headerPart = (p.poVendorNameToSync ?? 0) > 0 ? '采购订单名称快照 1 张，' : ''
    await ElMessageBox.alert(
      result.changed
        ? `已同步：${headerPart}采购明细 ${p.poItemsToSync} 条，到货通知 ${p.arrivalNoticesToSync} 条，入库单 ${p.stockInsToSync} 张，付款单 ${p.paymentsToSync} 张，进项发票 ${p.purchaseInvoicesToSync} 张。`
        : '供应商信息与主数据一致，无需更新。',
      '刷新供应商完成',
      { confirmButtonText: '知道了' }
    )
  } catch (e) {
    await ElMessageBox.alert(getApiErrorMessage(e, '同步失败，请稍后重试'), '刷新供应商失败', {
      confirmButtonText: '知道了'
    })
  } finally {
    syncingVendor.value = false
  }
}

onMounted(() => {
  fetchOrder()
  purchaseOrderItemOpsStore.registerHandlers({
    applyArrival: (row) => {
      poItemLineDialogsRef.value?.openArrival(row)
    },
    applyPayment: (row) => {
      poItemLineDialogsRef.value?.openPayment(row)
    }
  })
})

onBeforeUnmount(() => {
  purchaseOrderItemOpsStore.unregisterHandlers()
})

watch(orderId, () => {
  closePoItemLinePanel()
  purchaseOrderItemOpsStore.clear()
  fetchOrder()
})

watch(
  () => workspaceLayout?.rightPanelVisible.value,
  (visible, wasVisible) => {
    if (route.name !== 'PurchaseOrderDetail') return
    if (!visible || wasVisible || !purchaseOrderItemOpsStore.row) return
    const opsKey = purchaseOrderItemOpsStore.rowKey(purchaseOrderItemOpsStore.row)
    if (
      lineTabAggregates.value &&
      poItemLinePanel.visible &&
      poItemLinePanel.purchaseOrderItemId === opsKey
    ) {
      purchaseOrderItemOpsStore.syncRowAndAggregates(
        purchaseOrderItemOpsStore.row,
        lineTabAggregates.value
      )
      return
    }
    void purchaseOrderItemOpsStore.loadAggregates('加载明细失败')
  }
)

async function loadFavoriteState() {
  const id = orderId.value
  if (!id) {
    poFavorited.value = false
    return
  }
  try {
    poFavorited.value = await favoriteApi.checkFavorite(PURCHASE_ORDER_FAVORITE_ENTITY_TYPE, id)
  } catch {
    poFavorited.value = false
  }
}

async function toggleFavorite() {
  const id = orderId.value
  if (!id || favoriteLoading.value) return
  favoriteLoading.value = true
  try {
    if (poFavorited.value) {
      await favoriteApi.removeFavorite(PURCHASE_ORDER_FAVORITE_ENTITY_TYPE, id)
      poFavorited.value = false
    } else {
      await favoriteApi.addFavorite({ entityType: PURCHASE_ORDER_FAVORITE_ENTITY_TYPE, entityId: id })
      poFavorited.value = true
    }
    window.dispatchEvent(new Event(PURCHASE_ORDER_FAVORITES_CHANGED_EVENT))
  } catch {
    /* 全局拦截器已提示 */
  } finally {
    favoriteLoading.value = false
  }
}

async function onPoLineDialogSuccess() {
  await fetchOrder()
  await reloadPoItemLinePanelAggregates()
}

const fetchOrder = async () => {
  loading.value = true
  resetOrderLogTabs()
  try {
    const data = await purchaseOrderApi.getById(orderId.value)
    order.value = data ?? null
    poDetailItemsOpColExpanded.value = false
    if (order.value) {
      refreshTags()
      recordPurchaseOrderRecentView({
        id: String(order.value.id),
        purchaseOrderCode: order.value.purchaseOrderCode,
        vendorName: order.value.vendorName,
        vendorEnglishName: order.value.vendorEnglishName
      })
      await loadFavoriteState()
      await nextTick()
      // 底部「采购订单明细详情」：URL 带 purchaseOrderItemId 时选中对应行，否则默认第一行
      if (!maskPurchaseSensitiveFields.value) {
        await applyInitialPurchaseOrderItemSelection()
      }
    } else {
      closePoItemLinePanel()
      poFavorited.value = false
    }
  } catch {
    order.value = null
    poFavorited.value = false
    closePoItemLinePanel()
  } finally {
    loading.value = false
    if (order.value?.id && !maskPurchaseSensitiveFields.value) {
      void fetchDocumentCount()
      if (!changeLogsLoaded.value) void loadChangeLogs({ silent: true })
      if (!deletedItemsLoaded.value) void loadDeletedItems({ silent: true })
    }
  }
}

const refreshTags = async () => {
  if (!order.value) return
  try {
    currentTags.value = await tagApi.getEntityTags('PURCHASE_ORDER', order.value.id) || []
  } catch {
    currentTags.value = []
  }
}

const getStatusType = (status: number) => {
  const map: Record<number, string> = { 1: 'info', 2: 'warning', 10: 'success', 20: 'warning', 30: 'primary', 50: 'primary', 100: 'success', [-1]: 'danger', [-2]: 'info' }
  return map[status] ?? 'info'
}
const getStatusText = (status: number) => {
  const map: Record<number, string> = { 1: '新建', 2: '待审核', 10: '审核通过', 20: '待确认', 30: '已确认', 50: '进行中', 100: '采购完成', [-1]: '审核失败', [-2]: '取消' }
  return map[status] ?? '未知'
}
const currencyCodeText = (currency?: number) => {
  const c = Number(currency)
  return CURRENCY_CODE_TO_TEXT[c as keyof typeof CURRENCY_CODE_TO_TEXT] ?? 'RMB'
}
const currencyCodeClass = (currency?: number) => {
  const c = Number(currency)
  if (c === 1 || !Number.isFinite(c)) return 'amount-ccy--rmb'
  return 'amount-ccy--fx'
}
const formatDateTime = (v?: string) => (v ? formatDisplayDateTime(v) : '--')

const prStatusText = (v?: number) => ({ 0: '新建', 1: '部分完成', 2: '全部完成', 3: '已取消' } as Record<number, string>)[Number(v)] ?? '--'
const paymentStatusText = (v?: number) => ({ 1: '新建', 2: '待审核', 10: '审核通过', 100: '付款完成', [-1]: '审核失败', [-2]: '已取消' } as Record<number, string>)[Number(v)] ?? '--'

const handleEdit = () => {
  if (!order.value?.id) return
  router.push({ name: 'PurchaseOrderEdit', params: { id: order.value.id } })
}

</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+SC:wght@300;400;500&display=swap');

.purchase-order-detail {
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
  gap: 16px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
  min-width: 0;
  flex: 1;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-shrink: 0;
  flex-wrap: wrap;
  justify-content: flex-end;
}

.po-header-refresh-group {
  display: inline-flex;
  align-items: stretch;
}

.po-header-refresh-group > .btn-secondary:first-child {
  border-top-right-radius: 0;
  border-bottom-right-radius: 0;
}

.po-header-refresh-caret {
  min-width: 28px;
  padding: 0 8px;
  border-left: none;
  border-top-left-radius: 0;
  border-bottom-left-radius: 0;
  font-size: 12px;
  line-height: 1;
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

.po-caption-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
}

.caption-avatar-lg {
  width: 48px;
  height: 48px;
  flex-shrink: 0;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.3), rgba(0, 212, 255, 0.2));
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
  color: $cyan-primary;
}

.page-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
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
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;

  &--muted {
    color: rgba(150, 170, 195, 0.82);
  }
}

.title-meta--caption {
  margin-top: 4px;
}

.po-header-meta-row {
  min-height: 28px;
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.po-header-tags-row {
  flex-shrink: 0;
}

.po-header-add-tag-btn {
  padding: 6px 12px;
  font-size: 12px;
}

.po-header-add-tag-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 13px;
  font-size: 15px;
  font-weight: 500;
  line-height: 1;
}

.tags-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
}

.po-stocking-tag {
  flex-shrink: 0;
  cursor: default;
}

.po-pay-later-tag {
  flex-shrink: 0;
  cursor: default;
  --el-tag-bg-color: rgba(230, 126, 34, 0.92);
  --el-tag-border-color: rgba(230, 126, 34, 0.95);
  --el-tag-text-color: #fff;
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid $border-panel;
  color: $text-secondary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  background: rgba(255,255,255,0.05);
  cursor: pointer;
  transition: all 0.2s;
  &:hover {
    background: rgba(255,255,255,0.08);
    border-color: rgba(0,212,255,0.25);
  }
}

.btn-close-po {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  color: $color-amber;
  border: none;
  background: transparent;
  &:hover {
    background: rgba(255, 255, 255, 0.08);
    border: none;
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
  background: linear-gradient(135deg, rgba(201, 154, 69, 0.92), rgba(255, 180, 60, 0.82));
  border: 1px solid rgba(255, 180, 60, 0.45);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(255, 180, 60, 0.35);
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

.loading-wrap {
  padding: 20px;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
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
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  border-bottom: 1px solid rgba(255,255,255,0.04);
  border-right: 1px solid rgba(255,255,255,0.04);
  &:nth-child(3n) { border-right: none; }
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
}

.info-item--span-all {
  grid-column: 1 / -1;
  border-right: none;
}

.info-grid--inline-labels .info-item--span-all {
  align-items: flex-start;
}

.info-grid--basic {
  .info-item {
    &:nth-child(3n) { border-right: none; }
  }
  .info-item--basic-full-row {
    grid-column: 1 / -1;
    border-right: none;
  }
  .info-item--basic-spacer {
    border-right: none;
  }
}

.info-label {
  font-size: 11px;
  color: $text-muted;
  letter-spacing: 0.5px;
  text-transform: uppercase;
  white-space: nowrap;
}

.info-value {
  font-size: 13px;
  color: $text-secondary;
  min-width: 0;
  word-break: break-word;
}

.info-value--code {
  font-family: 'Noto Sans SC', sans-serif;
  color: $color-ice-blue;
}

.info-value--amount {
  font-family: 'Noto Sans SC', sans-serif;
  color: $text-primary;
  font-weight: 400;
}

.amount-with-code {
  display: inline-flex;
  align-items: baseline;
  gap: 6px;
}

.amount-ccy {
  font-size: 0.92em;
  font-weight: 500;
}

.amount-ccy--rmb {
  color: #ff4f96;
}

.amount-ccy--fx {
  color: #19c37d;
}

.info-value--time {
  font-size: 12px;
  color: $text-muted;
}

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
  cursor: pointer;
  margin-bottom: -1px;
}

.tab-btn--active {
  color: $cyan-primary;
  border-bottom-color: $cyan-primary;
}

.tabs-body {
  padding: 20px;
}

.po-aggregate-table-wrap {
  margin-top: 4px;
}

.po-tab-link {
  color: $cyan-primary;
  text-decoration: none;
  font-weight: 500;
  &:hover {
    text-decoration: underline;
  }
}

.detail-items-table-wrap {
  margin-top: 4px;
}

// §7.4 表头/表体基线见 detail-panel-list-table.scss；此处仅 CrmDataTable 操作列等页内扩展
.detail-items-table-wrap :deep(.items-table),
.detail-items-table-wrap :deep(.crm-items-table.detail-panel-list-table) {
  --el-table-border-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
  border-radius: 0;
  border: none;
  min-height: 0;
  overflow: visible;
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
  :deep(th.op-col .po-detail-op-col-header--icon-only) {
    display: flex;
    justify-content: center;
    align-items: center;
    width: 100%;
  }
  :deep(th.op-col .po-detail-op-col-toggle) {
    min-width: 28px;
    min-height: 28px;
    font-size: 18px;
    font-weight: 700;
    line-height: 1;
  }
  :deep(.action-btns--po-detail-items) {
    opacity: 1;
    flex-wrap: wrap;
    justify-content: center;
    gap: 4px;
  }
  .po-item-progress-sub {
    margin-top: 2px;
    font-size: 11px;
    color: $text-muted;
    line-height: 1.25;
    white-space: normal;
  }
  :deep(.po-item-progress-qty-col .cell) {
    white-space: normal;
  }
}

.po-detail-items-table-stack {
  display: flex;
  flex-direction: column-reverse;
  gap: 12px;
}

.po-detail-items-table {
  cursor: pointer;
}

/* 与 CustomerList / RFQItemList 底栏一致：《业务列表规范》列设置 + 行高密度 + Spacer */
.po-detail-items-list-footer.pagination-wrapper {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  flex-wrap: wrap;
}

.po-detail-items-list-footer .list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
}

.po-detail-items-list-footer .list-footer-density-anchor {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  min-height: 0;
}

.po-detail-items-list-footer .list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}

.po-detail-items-list-footer .list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

/** 《业务列表规范》§3.2：数量字重与字色 */
.po-detail-biz-qty {
  font-weight: 700;
  color: #27292c;
  font-variant-numeric: tabular-nums;
}

html[data-theme='dark'] .purchase-order-detail .po-detail-biz-qty {
  color: $text-primary;
}

.doc-tab-content {
  padding-top: 4px;

  &.doc-tab-content--dragging {
    border: 1px dashed rgba(0, 212, 255, 0.5);
    border-radius: 8px;
    background: rgba(0, 212, 255, 0.03);
  }
}

.po-aggregate-table-wrap {
  margin-top: 4px;
}

.so-line-overview-wrap {
  margin-top: 4px;
  overflow-x: auto;
}

.so-line-overview {
  width: 100%;
  table-layout: fixed;
  border-collapse: collapse;
  font-size: 14px;
  line-height: 1.45;
  color: var(--crm-table-text);

  th,
  td {
    border: 1px solid var(--crm-table-cell-line);
    padding: 10px 12px;
    vertical-align: middle;
  }

  &__col-first {
    width: 88px;
  }

  &__corner {
    width: 88px;
    min-width: 88px;
    max-width: 88px;
    background: var(--crm-detail-section-header-bg);
  }

  &__row-head {
    width: 88px;
    min-width: 88px;
    max-width: 88px;
    text-align: left;
    font-weight: 400;
    white-space: nowrap;
    background: var(--crm-detail-section-header-bg);
    color: var(--crm-table-header-text);
  }

  &__col-head {
    text-align: center;
    font-weight: 400;
    white-space: normal;
    word-break: break-word;
    background: var(--crm-detail-section-header-bg);
    color: var(--crm-table-header-text);

    &--gray {
      color: $info-color;
    }

    &--yellow {
      color: $warning-color;
    }

    &--green {
      color: $success-color;
    }

    &--red {
      color: $danger-color;
    }
  }

  &__cell {
    text-align: center;
    color: var(--crm-table-text);
    font-weight: 500;
    background: var(--crm-card-bg);

    &--right {
      text-align: right;
    }
  }

  &__qty {
    font-variant-numeric: tabular-nums;
    color: var(--crm-table-text);
  }
}

.so-item-line-detail-panel {
  margin-top: 20px;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  background: $layer-2;
  overflow: hidden;
}

.so-item-line-detail-panel__head {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  padding: 12px 16px;
  border-bottom: 1px solid $border-panel;
  background: var(--crm-detail-panel-card-head-bg);
}

.so-item-line-detail-panel__title {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}

.so-item-line-detail-panel__code {
  font-size: 14px;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  /* 级联从属面板单号 — 《业务详情页面规范》§7.4.6 .panel-hint__value */
  color: $color-amber;
}

.so-item-line-detail-panel__close {
  margin-left: auto;
  padding: 4px 12px;
  font-size: 13px;
  color: rgba(200, 220, 240, 0.9);
  background: transparent;
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: $border-radius-sm;
  cursor: pointer;
  &:hover {
    border-color: rgba(0, 212, 255, 0.45);
    color: #e8f4ff;
  }
}

.so-item-line-detail-panel__alert {
  margin: 12px 16px 0;
}

.so-item-line-detail-panel__body {
  padding: 12px 16px 16px;
}

.so-item-line-detail-panel__body--tabbed {
  padding: 0;
}

.so-item-line-detail-tabs-section.tabs-section {
  background: transparent;
  border: none;
  border-radius: 0;
  padding: 0;
  margin: 0;
}
</style>

<!-- 顶栏「更多」下拉 Teleport 到 body，需非 scoped -->
<style lang="scss">
@import '@/assets/styles/variables.scss';

.po-detail-header-more-popper.el-dropdown__popper,
.po-detail-header-more-popper.el-popper {
  background: $layer-2 !important;
  border: 1px solid rgba(0, 212, 255, 0.15) !important;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.45) !important;
}

.po-detail-header-more-popper .el-dropdown-menu {
  background: transparent !important;
  border: none !important;
  box-shadow: none !important;
  padding: 4px 0 !important;
}

.po-detail-header-more-popper .el-dropdown-menu__item {
  color: $text-primary !important;
  font-size: 13px;

  &:hover,
  &:focus {
    background: rgba(0, 212, 255, 0.1) !important;
    color: $text-primary !important;
  }
}

.po-detail-header-more-popper .detail-more-item--danger {
  color: rgba(245, 108, 108, 0.95) !important;
  &:hover,
  &:focus {
    background: rgba(245, 108, 108, 0.12) !important;
    color: #ff9a9a !important;
  }
}
</style>
