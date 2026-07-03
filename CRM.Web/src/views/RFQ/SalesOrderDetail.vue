<template>
  <div class="sales-order-detail">
    <!-- 详情 CaptionBar（对齐《业务详情页面规范.md》） -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="router.back()">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          返回
        </button>
        <div v-if="order" class="so-caption-title-group">
          <div class="caption-avatar-lg">{{ captionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title" :class="{ 'page-title--muted': order.status === -2 }">
                  {{ t('salesOrderDetailView.captionPrefix') }} {{ order.sellOrderCode }}
                </h1>
                <button
                  type="button"
                  class="btn-favorite-star"
                  :class="{ 'is-favorite': soFavorited }"
                  :disabled="favoriteLoading"
                  :title="soFavorited ? t('salesOrderDetailView.unfavorite') : t('salesOrderDetailView.favorite')"
                  :aria-pressed="soFavorited"
                  :aria-label="soFavorited ? t('salesOrderDetailView.unfavorite') : t('salesOrderDetailView.favorite')"
                  @click="toggleFavorite"
                >
                  <svg
                    v-if="!soFavorited"
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
                <div v-if="showSoHeaderTags" class="so-header-tags-row tags-row">
                  <TagListDisplay v-if="currentTags.length" :tags="currentTags" />
                  <button
                    v-if="canWriteSo"
                    type="button"
                    class="btn-secondary so-header-add-tag-btn"
                    @click="tagDialogVisible = true"
                  >
                    <span class="so-header-add-tag-icon" aria-hidden="true">±</span>
                    {{ t('salesOrderDetailView.tags.add') }}
                  </button>
                </div>
              </div>
            </div>
            <div class="title-meta title-meta--caption so-header-meta-row">
              <el-tag :type="getStatusType(order.status)" size="small" effect="dark">
                {{ getStatusText(order.status) }}
              </el-tag>
            </div>
          </div>
        </div>
      </div>
      <div v-if="order" class="header-right">
        <button
          v-if="canCancelSalesOrderFromMenu"
          type="button"
          class="btn-close-so"
          @click="handleCancelSalesOrder"
        >
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10" /><line x1="15" y1="9" x2="9" y2="15" /><line x1="9" y1="9" x2="15" y2="15" />
          </svg>
          {{ t('salesOrderDetailView.cancelOrder') }}
        </button>
        <button class="btn-secondary" type="button" :disabled="refreshingExtends" @click="handleRefreshItemExtends">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="23 4 23 10 17 10" />
            <polyline points="1 20 1 14 7 14" />
            <path d="M3.51 9a9 9 0 0 1 14.13-3.36L23 10M1 14l5.36 4.36A9 9 0 0 0 20.49 15" />
          </svg>
          {{ refreshingExtends ? t('salesOrderDetailView.refreshing') : t('salesOrderDetailView.refresh') }}
        </button>
        <button v-if="canWriteSo" class="btn-primary" type="button" @click="handleEdit">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
          </svg>
          {{ t('salesOrderDetailView.edit') }}
        </button>
        <el-dropdown
          v-if="canWriteSo"
          trigger="click"
          placement="bottom-end"
          popper-class="so-detail-header-more-popper"
          @command="onHeaderMoreCommand"
        >
          <button type="button" class="btn-more-actions" :title="t('salesOrderDetailView.more')" :aria-label="t('salesOrderDetailView.more')">
            <span class="btn-more-actions__dots" aria-hidden="true">⋯</span>
          </button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="delete" class="detail-more-item--danger">{{ t('salesOrderDetailView.deleteOrder') }}</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </div>

    <div v-if="loading" class="loading-wrap">
      <el-skeleton :rows="8" animated />
    </div>

    <template v-else-if="order">
      <!-- 基本信息卡片 -->
      <div class="info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('salesOrderDetailView.basicInfo') }}</span>
          </div>
          <div class="section-header__meta">
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">{{ t('salesOrderDetailView.createDate') }}</span>
              <span class="section-header-meta-item__value">{{ soBasicCreateDateText }}</span>
            </span>
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">{{ t('salesOrderDetailView.createUser') }}</span>
              <span class="section-header-meta-item__value">{{ soBasicCreateUserText }}</span>
            </span>
          </div>
        </div>
        <!-- 第 1 行：客户 · 客户联系人 · 总金额 -->
        <div class="info-grid info-grid--inline-labels info-grid--basic">
          <div v-if="showCustomerIdentityFields" class="info-item">
            <span class="info-label">{{ t('salesOrderCreate.fields.customer') }}</span>
            <span class="info-value">{{ order.customerName || '—' }}</span>
          </div>
          <div v-if="showCustomerIdentityFields" class="info-item">
            <span class="info-label">{{ t('salesOrderCreate.fields.customerContact') }}</span>
            <span class="info-value">{{ order.customerContactName || order.CustomerContactName || '—' }}</span>
          </div>
          <div v-if="showSalesMoneyFields" class="info-item">
            <span class="info-label">{{ t('salesOrderDetailView.totalAmount') }}</span>
            <span class="info-value info-value--amount amount-with-code">
              <span>{{ formatTotalAmountNumber(order.total) }}</span>
              <span v-if="formatTotalAmountNumber(order.total) !== '—'" :class="['dock-tier-ccy', listAmountCurrencyDockClass(order.currency)]">
                {{ listAmountCurrencyIso(order.currency) }}
              </span>
            </span>
          </div>
        </div>
        <!-- 第 2 行：销售员 · 销售助理 · （占位） -->
        <div class="info-grid info-grid--inline-labels info-grid--basic">
          <div class="info-item">
            <span class="info-label">{{ t('salesOrderCreate.fields.salesUser') }}</span>
            <span class="info-value">{{ maskSaleSensitiveFields ? '—' : order.salesUserName || '—' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('salesOrderCreate.fields.assistor') }}</span>
            <span class="info-value">{{ showCustomerIdentityFields ? ((order as any).assistorUserName || '—') : '—' }}</span>
          </div>
          <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
        </div>
        <!-- 第 3 行：送货地址 · 交货日期 · （占位） -->
        <div class="info-grid info-grid--inline-labels info-grid--basic">
          <div class="info-item">
            <span class="info-label">{{ t('salesOrderDetailView.deliveryAddress') }}</span>
            <span class="info-value">{{ order.deliveryAddress || '—' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('salesOrderDetailView.deliveryDate') }}</span>
            <span class="info-value info-value--time">{{ formatDateTime(order.deliveryDate) }}</span>
          </div>
          <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
        </div>
        <!-- 第 4 行：备注 -->
        <div class="info-grid info-grid--inline-labels">
          <div class="info-item info-item--span-all">
            <span class="info-label">{{ t('salesOrderDetailView.remark') }}</span>
            <span class="info-value">{{ order.headerRemarkDisplay || order.HeaderRemarkDisplay || order.comment || '—' }}</span>
          </div>
          <div v-if="order.auditRemark || order.status === -1" class="info-item info-item--span-all">
            <span class="info-label">{{ t('salesOrderDetailView.auditRejectReason') }}</span>
            <span class="info-value info-value--warn">{{ order.auditRemark || '—' }}</span>
          </div>
        </div>
      </div>

      <!-- TabBar：订单明细 | 文档（采购/入库/库存等下游见底部「销售订单明细详情」） -->
      <div class="tabs-section">
        <div class="tabs-nav">
          <button class="tab-btn" :class="{ 'tab-btn--active': activeTab === 'items' }" @click="activeTab = 'items'">{{ formatOrderDetailTabLabel('订单明细', 'items') }}</button>
          <button
            v-if="!maskSaleSensitiveFields"
            class="tab-btn"
            :class="{ 'tab-btn--active': activeTab === 'documents' }"
            @click="activeTab = 'documents'"
          >
            {{ formatOrderDetailTabLabel('文档', 'documents') }}
          </button>
          <button
            v-if="!maskSaleSensitiveFields"
            class="tab-btn"
            :class="{ 'tab-btn--active': activeTab === 'changeLog' }"
            @click="activeTab = 'changeLog'"
          >
            {{ formatOrderDetailTabLabel('更改日志', 'changeLog') }}
          </button>
          <button
            v-if="!maskSaleSensitiveFields"
            class="tab-btn"
            :class="{ 'tab-btn--active': activeTab === 'deleteLog' }"
            @click="activeTab = 'deleteLog'"
          >
            {{ formatOrderDetailTabLabel('删除日志', 'deleteLog') }}
          </button>
        </div>
        <div class="tabs-body">
          <div v-show="activeTab === 'items'" class="detail-items-table-wrap">
            <CrmDataTable
              v-if="order.items?.length"
              :data="order.items"
              size="small"
              stripe
              embedded
              :border="false"
              class="items-table detail-panel-list-table so-detail-items-table"
              row-key="id"
              :row-class-name="soItemRowClassName"
              @row-click="onSalesOrderItemRowClick"
              @row-dblclick="onSalesOrderItemRowDblClick"
            >
              <el-table-column type="index" width="50" label="#" />
              <el-table-column
                prop="sellOrderItemCode"
                :label="t('salesOrderItemList.columns.sellOrderItemCode')"
                min-width="168"
                show-overflow-tooltip
              />
              <el-table-column
                v-if="showCustomerIdentityFields"
                prop="customerPn"
                label="客户物料型号"
                min-width="140"
                show-overflow-tooltip
              />
              <el-table-column
                v-if="showCustomerIdentityFields"
                prop="customerBrand"
                label="客户品牌"
                width="100"
                show-overflow-tooltip
              />
              <el-table-column
                v-if="showCustomerIdentityFields"
                prop="customerSo"
                label="客户订单号"
                min-width="120"
                show-overflow-tooltip
              />
              <el-table-column prop="pn" label="物料型号" min-width="160" />
              <el-table-column prop="brand" label="品牌" width="120" />
              <el-table-column label="" width="48" class-name="so-item-col-spacer" label-class-name="so-item-col-spacer">
                <template #default />
              </el-table-column>
              <el-table-column v-if="showSalesMoneyFields" prop="price" label="销售单价" align="right" width="120">
                <template #default="{ row }">
                  <span class="amount-with-code">
                    <span>{{ formatUnitPriceNumber(row.price) }}</span>
                    <span v-if="formatUnitPriceNumber(row.price) !== '—'" :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">
                      {{ listAmountCurrencyIso(row.currency) }}
                    </span>
                  </span>
                </template>
              </el-table-column>
              <el-table-column prop="qty" label="销售数量" align="right" width="100" />
              <el-table-column v-if="showSalesMoneyFields" label="销售总额" align="right" width="130">
                <template #default="{ row }">
                  <span class="amount-with-code">
                    <span>{{ formatTotalAmountNumber(row.qty * row.price) }}</span>
                    <span v-if="formatTotalAmountNumber(row.qty * row.price) !== '—'" :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">
                      {{ listAmountCurrencyIso(row.currency) }}
                    </span>
                  </span>
                </template>
              </el-table-column>
              <el-table-column label="生产日期" width="108" align="center">
                <template #default="{ row }">
                  {{ fmtSoItemDateCode(row) }}
                </template>
              </el-table-column>
              <el-table-column label="交期" width="112" align="center">
                <template #default="{ row }">
                  {{ fmtSoItemDeliveryDate(row) }}
                </template>
              </el-table-column>
              <el-table-column label="" width="48" class-name="so-item-col-spacer" label-class-name="so-item-col-spacer">
                <template #default />
              </el-table-column>
              <el-table-column prop="comment" label="备注" min-width="200" show-overflow-tooltip />
              <el-table-column label="采购状态" width="100" align="center">
                <template #default="{ row }">
                  <el-tag :type="getExtendTriStatusTagType(row.purchaseProgressStatus)" size="small" effect="dark">
                    {{ getPurchaseProgressText(row.purchaseProgressStatus) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="入库状态" width="100" align="center">
                <template #default="{ row }">
                  <el-tag :type="getExtendTriStatusTagType(row.stockInProgressStatus)" size="small" effect="dark">
                    {{ getStockInProgressText(row.stockInProgressStatus) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="出库通知状态" width="120" align="center">
                <template #default="{ row }">
                  <el-tag :type="getExtendTriStatusTagType(row.stockOutNotifyProgressStatus)" size="small" effect="dark">
                    {{ getStockOutNotifyProgressText(row.stockOutNotifyProgressStatus) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="出库状态" width="100" align="center">
                <template #default="{ row }">
                  <el-tag :type="getExtendTriStatusTagType(row.stockOutProgressStatus)" size="small" effect="dark">
                    {{ getStockOutProgressText(row.stockOutProgressStatus) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="收款状态" width="100" align="center">
                <template #default="{ row }">
                  <el-tag :type="getExtendTriStatusTagType(row.receiptProgressStatus)" size="small" effect="dark">
                    {{ getReceiptProgressText(row.receiptProgressStatus) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="开票状态" width="100" align="center">
                <template #default="{ row }">
                  <el-tag :type="getExtendTriStatusTagType(row.invoiceProgressStatus)" size="small" effect="dark">
                    {{ getSellInvoiceProgressText(row.invoiceProgressStatus) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column v-if="showSalesMoneyFields" label="折算美金单价" align="right" width="140">
                <template #default="{ row }">
                  <span v-if="row.usdUnitPrice != null" class="amount-with-code">
                    <span>{{ Number(row.usdUnitPrice).toFixed(6) }}</span>
                    <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
                  </span>
                  <span v-else>—</span>
                </template>
              </el-table-column>
              <el-table-column v-if="showSalesMoneyFields" label="折算美金总额" align="right" width="140">
                <template #default="{ row }">
                  <span v-if="row.usdLineTotal != null" class="amount-with-code">
                    <span>{{ Number(row.usdLineTotal).toFixed(2) }}</span>
                    <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
                  </span>
                  <span v-else>—</span>
                </template>
              </el-table-column>
              <el-table-column v-if="showSalesMoneyFields" :label="t('salesOrderItemList.columns.salesProfitExpected')" align="right" width="140">
                <template #default="{ row }">
                  <span v-if="row.salesProfitExpected != null" class="amount-with-code">
                    <span>{{ Number(row.salesProfitExpected).toFixed(2) }}</span>
                    <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
                  </span>
                  <span v-else>—</span>
                </template>
              </el-table-column>
              <el-table-column v-if="showSalesMoneyFields" :label="t('salesOrderItemList.columns.profitOutBizUsd')" align="right" width="120">
                <template #default="{ row }">
                  <span v-if="row.profitOutBizUsd != null" class="amount-with-code">
                    <span>{{ Number(row.profitOutBizUsd).toFixed(2) }}</span>
                    <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
                  </span>
                  <span v-else>—</span>
                </template>
              </el-table-column>
              <el-table-column v-if="showSalesMoneyFields" :label="t('salesOrderItemList.columns.profitOutRateBiz')" align="right" width="120">
                <template #default="{ row }">
                  {{ row.profitOutRateBiz != null ? Number(row.profitOutRateBiz).toFixed(6) : '—' }}
                </template>
              </el-table-column>
              <el-table-column
                label="操作"
                :width="soDetailItemsOpColWidth"
                :min-width="soDetailItemsOpColMinWidth"
                fixed="right"
                align="center"
                class-name="op-col"
                label-class-name="op-col"
              >
                <template #header>
                  <div class="so-detail-op-col-header--icon-only">
                    <button
                      type="button"
                      class="op-col-toggle-btn so-detail-op-col-toggle"
                      @click.stop="toggleSoDetailItemsOpCol"
                    >
                      {{ soDetailItemsOpColExpanded ? '>' : '<' }}
                    </button>
                  </div>
                </template>
                <template #default="{ row }">
                  <div @click.stop @dblclick.stop>
                    <div v-if="soDetailItemsOpColExpanded" class="action-btns">
                      <el-button link type="primary" size="small" @click.stop="goSoItemDetail(row)">
                        {{ t('salesOrderItemList.actions.detail') }}
                      </el-button>
                      <el-button v-if="canWriteSo" link type="primary" size="small" @click.stop="goSoItemEdit(row)">
                        {{ t('salesOrderItemList.actions.edit') }}
                      </el-button>
                      <el-button
                        v-if="canPurchaseReq && mainAllowsOps(row)"
                        link
                        type="warning"
                        size="small"
                        :disabled="applyPurchaseDisabled(row)"
                        @click.stop="applyPurchaseOne(row)"
                      >
                        {{ t('salesOrderItemList.actions.applyPurchase') }}
                      </el-button>
                      <span v-if="canWriteSo && mainAllowsOps(row)" class="action-with-hint">
                        <el-button
                          link
                          type="warning"
                          size="small"
                          :disabled="salesOrderLineApplyStockOutButtonDisabled(row)"
                          @click.stop="applyStockOutOne(row)"
                        >
                          {{ t('salesOrderItemList.actions.applyStockOut') }}
                        </el-button>
                        <ApplyStockOutDisabledHint
                          v-if="applyStockOutDisabledHint(row)"
                          :content="applyStockOutDisabledHint(row)!"
                        />
                      </span>
                    </div>
                    <el-dropdown v-else trigger="click" placement="bottom-end">
                      <div class="op-more-dropdown-trigger">
                        <button type="button" class="op-more-trigger">...</button>
                      </div>
                      <template #dropdown>
                        <el-dropdown-menu>
                          <el-dropdown-item @click.stop="goSoItemDetail(row)">
                            <span class="op-more-item op-more-item--primary">{{ t('salesOrderItemList.actions.detail') }}</span>
                          </el-dropdown-item>
                          <el-dropdown-item v-if="canWriteSo" @click.stop="goSoItemEdit(row)">
                            <span class="op-more-item op-more-item--primary">{{ t('salesOrderItemList.actions.edit') }}</span>
                          </el-dropdown-item>
                          <el-dropdown-item
                            v-if="canPurchaseReq && mainAllowsOps(row)"
                            :disabled="applyPurchaseDisabled(row)"
                            @click.stop="applyPurchaseOne(row)"
                          >
                            <span
                              class="op-more-item"
                              :class="applyPurchaseDisabled(row) ? 'op-more-item--disabled' : 'op-more-item--warning'"
                            >{{ t('salesOrderItemList.actions.applyPurchase') }}</span>
                          </el-dropdown-item>
                          <el-dropdown-item
                            v-if="canWriteSo && mainAllowsOps(row)"
                            @click.stop="onApplyStockOutDropdownClick(row)"
                          >
                            <span class="op-more-item-row">
                              <span
                                class="op-more-item"
                                :class="
                                  salesOrderLineApplyStockOutButtonDisabled(row)
                                    ? 'op-more-item--disabled'
                                    : 'op-more-item--warning'
                                "
                              >{{ t('salesOrderItemList.actions.applyStockOut') }}</span>
                              <ApplyStockOutDisabledHint
                                v-if="applyStockOutDisabledHint(row)"
                                :content="applyStockOutDisabledHint(row)!"
                              />
                            </span>
                          </el-dropdown-item>
                        </el-dropdown-menu>
                      </template>
                    </el-dropdown>
                  </div>
                </template>
              </el-table-column>
            </CrmDataTable>
            <DetailListPanelEmpty v-else size="low" />
          </div>
          <div v-show="activeTab === 'documents' && !maskSaleSensitiveFields" class="doc-tab-content">
            <DocumentUploadPanel
              biz-type="SALES_ORDER"
              :biz-id="String(order.id)"
              :max-files="20"
              :max-size-mb="100"
              @uploaded="onSoDocumentUploaded"
            />
            <DocumentListPanel
              ref="docListRef"
              biz-type="SALES_ORDER"
              :biz-id="String(order.id)"
              view-mode="list"
              style="margin-top: 16px;"
            />
          </div>
          <div v-show="activeTab === 'changeLog' && !maskSaleSensitiveFields" v-loading="changeLogsLoading" class="so-aggregate-table-wrap">
            <el-table v-if="changeLogs.length > 0" :data="changeLogs" size="small" stripe>
              <el-table-column label="变更时间" width="160">
                <template #default="{ row }">{{ formatDateTime(row?.changedAt) }}</template>
              </el-table-column>
              <el-table-column label="操作人" width="100" show-overflow-tooltip>
                <template #default="{ row }">{{ row.changedByUserName || '系统' }}</template>
              </el-table-column>
              <el-table-column label="对象" width="120" show-overflow-tooltip>
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
            <DetailListPanelEmpty v-else size="low" />
          </div>
          <div v-show="activeTab === 'deleteLog' && !maskSaleSensitiveFields" v-loading="deletedItemsLoading" class="so-aggregate-table-wrap">
            <el-table v-if="deletedItems.length > 0" :data="deletedItems" size="small" stripe>
              <el-table-column label="删除日期" width="160">
                <template #default="{ row }">{{ formatDateTime(row?.deletedAt || row?.createTime) }}</template>
              </el-table-column>
              <el-table-column label="操作人" width="100" show-overflow-tooltip>
                <template #default="{ row }">{{ row.deletedByUserName || '—' }}</template>
              </el-table-column>
              <el-table-column prop="sellOrderItemCode" label="销售订单明细编号" min-width="140" show-overflow-tooltip />
              <el-table-column prop="pn" label="物料型号" min-width="140" show-overflow-tooltip />
              <el-table-column prop="brand" label="品牌" width="100" show-overflow-tooltip />
              <el-table-column label="数量" width="90" align="right" prop="qty" />
              <el-table-column label="单价+币别" width="120" align="right">
                <template #default="{ row }">{{ formatDeletedItemPrice(row) }}</template>
              </el-table-column>
              <el-table-column prop="comment" label="备注" min-width="140" show-overflow-tooltip />
            </el-table>
            <DetailListPanelEmpty v-else size="low" />
          </div>
        </div>
      </div>

      <!-- 双击「订单明细」行：按该销售明细编号加载下游列表 -->
      <div v-if="soItemLinePanel.visible && !maskSaleSensitiveFields" class="so-item-line-detail-panel">
        <div class="so-item-line-detail-panel__head">
          <span class="so-item-line-detail-panel__title">销售订单明细详情</span>
          <span class="so-item-line-detail-panel__code panel-hint__value">{{ soItemLinePanel.sellOrderItemCode || '—' }}</span>
          <button type="button" class="so-item-line-detail-panel__close" @click="closeSoItemLinePanel">收起</button>
        </div>
        <el-alert
          v-if="soItemLinePanel.loadError"
          type="error"
          :closable="false"
          :title="soItemLinePanel.loadError"
          class="so-item-line-detail-panel__alert"
          show-icon
        />
        <div v-loading="soItemLinePanel.loading" class="so-item-line-detail-panel__body so-item-line-detail-panel__body--tabbed">
          <div class="tabs-section so-item-line-detail-tabs-section">
            <div class="tabs-nav">
              <button type="button" class="tab-btn" :class="{ 'tab-btn--active': soItemLinePanel.activeTab === 'rfqItems' }" @click="soItemLinePanel.activeTab = 'rfqItems'">
                {{ formatSoItemLineTabLabel(t('salesOrderDetailView.tabs.rfqItems'), 'rfqItems') }}
              </button>
              <button type="button" class="tab-btn" :class="{ 'tab-btn--active': soItemLinePanel.activeTab === 'quotes' }" @click="soItemLinePanel.activeTab = 'quotes'">
                {{ formatSoItemLineTabLabel(t('salesOrderDetailView.tabs.quotes'), 'quotes') }}
              </button>
              <button type="button" class="tab-btn" :class="{ 'tab-btn--active': soItemLinePanel.activeTab === 'pr' }" @click="soItemLinePanel.activeTab = 'pr'">
                {{ formatSoItemLineTabLabel('采购申请', 'pr') }}
              </button>
              <button type="button" class="tab-btn" :class="{ 'tab-btn--active': soItemLinePanel.activeTab === 'po' }" @click="soItemLinePanel.activeTab = 'po'">
                {{ formatSoItemLineTabLabel('采购订单明细', 'po') }}
              </button>
              <button type="button" class="tab-btn" :class="{ 'tab-btn--active': soItemLinePanel.activeTab === 'stockIn' }" @click="soItemLinePanel.activeTab = 'stockIn'">
                {{ formatSoItemLineTabLabel('入库', 'stockIn') }}
              </button>
              <button type="button" class="tab-btn" :class="{ 'tab-btn--active': soItemLinePanel.activeTab === 'stock' }" @click="soItemLinePanel.activeTab = 'stock'">
                {{ formatSoItemLineTabLabel('库存', 'stock') }}
              </button>
              <button type="button" class="tab-btn" :class="{ 'tab-btn--active': soItemLinePanel.activeTab === 'outNotify' }" @click="soItemLinePanel.activeTab = 'outNotify'">
                {{ formatSoItemLineTabLabel('出库通知', 'outNotify') }}
              </button>
              <button type="button" class="tab-btn" :class="{ 'tab-btn--active': soItemLinePanel.activeTab === 'stockOut' }" @click="soItemLinePanel.activeTab = 'stockOut'">
                {{ formatSoItemLineTabLabel('出库', 'stockOut') }}
              </button>
              <button type="button" class="tab-btn" :class="{ 'tab-btn--active': soItemLinePanel.activeTab === 'receipt' }" @click="soItemLinePanel.activeTab = 'receipt'">
                {{ formatSoItemLineTabLabel('收款', 'receipt') }}
              </button>
              <button type="button" class="tab-btn" :class="{ 'tab-btn--active': soItemLinePanel.activeTab === 'sellInvoice' }" @click="soItemLinePanel.activeTab = 'sellInvoice'">
                {{ formatSoItemLineTabLabel('销项发票', 'sellInvoice') }}
              </button>
              <button type="button" class="tab-btn" :class="{ 'tab-btn--active': soItemLinePanel.activeTab === 'qcImages' }" @click="soItemLinePanel.activeTab = 'qcImages'">
                {{ formatSoItemLineTabLabel(t('salesOrderDetailView.tabs.qcImages'), 'qcImages') }}
              </button>
            </div>
            <div class="tabs-body">
              <div v-show="soItemLinePanel.activeTab === 'rfqItems'" class="so-aggregate-table-wrap">
                <el-table
                  v-if="(lineTabAggregates?.rfqItems?.length ?? 0) > 0"
                  :data="lineTabAggregates?.rfqItems ?? []"
                  size="small"
                  stripe
                >
                  <el-table-column type="index" width="50" label="#" />
                  <el-table-column min-width="140" label="需求编号">
                    <template #default="{ row }">
                      <router-link class="so-tab-link" :to="`/rfqs/${row.rfqId}`">{{ row.rfqCode }}</router-link>
                    </template>
                  </el-table-column>
                  <el-table-column prop="lineNo" label="行号" width="72" align="center" />
                  <el-table-column v-if="showCustomerIdentityFields" prop="customerName" label="客户" min-width="120" show-overflow-tooltip />
                  <el-table-column label="销售员" width="110" show-overflow-tooltip>
                    <template #default="{ row }">{{ row.salesUserName || '—' }}</template>
                  </el-table-column>
                  <el-table-column prop="mpn" label="物料型号" min-width="140" show-overflow-tooltip />
                  <el-table-column prop="brand" label="品牌" width="120" show-overflow-tooltip />
                  <el-table-column label="数量" width="100" align="right" prop="quantity" />
                  <el-table-column label="状态" width="100">
                    <template #default="{ row }">{{ rfqItemStatusLabel(row?.status) }}</template>
                  </el-table-column>
                  <el-table-column label="询价采购员" min-width="140" show-overflow-tooltip>
                    <template #default="{ row }">{{ formatRfqItemAssignedPurchasers(row) }}</template>
                  </el-table-column>
                  <el-table-column min-width="130" label="报价单号">
                    <template #default="{ row }">
                      <span>{{ row.quoteCode || '—' }}</span>
                    </template>
                  </el-table-column>
                  <el-table-column label="生产日期" width="120" prop="productionDate" show-overflow-tooltip />
                  <el-table-column label="需求创建" width="160">
                    <template #default="{ row }">{{ formatDateTime(row?.rfqCreateTime) }}</template>
                  </el-table-column>
                </el-table>
                <DetailListPanelEmpty v-else size="low" />
              </div>
              <div v-show="soItemLinePanel.activeTab === 'quotes'" class="so-aggregate-table-wrap">
                <el-table
                  v-if="(lineTabAggregates?.quotes?.length ?? 0) > 0"
                  :data="lineTabAggregates?.quotes ?? []"
                  class="dock-quote-table"
                  size="small"
                  stripe
                >
                  <el-table-column type="index" width="50" label="#" />
                  <el-table-column min-width="140" label="报价单号">
                    <template #default="{ row }">
                      <router-link class="so-tab-link" :to="`/quotes/${row.id}`">{{ row.quoteCode }}</router-link>
                    </template>
                  </el-table-column>
                  <el-table-column prop="rfqCode" label="需求编号" min-width="130" show-overflow-tooltip />
                  <el-table-column prop="mpn" label="物料型号" min-width="140" show-overflow-tooltip />
                  <el-table-column prop="brand" label="品牌" width="120" show-overflow-tooltip />
                  <el-table-column label="供应商" min-width="140" show-overflow-tooltip>
                    <template #default="{ row }">{{ soQuoteVendorNamesDisplay(row) }}</template>
                  </el-table-column>
                  <el-table-column label="报价数量" width="100" align="right" class-name="dock-tier-col">
                    <template #default="{ row }">
                      <div class="dock-quote-tiers">
                        <template v-if="soQuoteLineItems(row).length">
                          <div
                            v-for="(it, idx) in soQuoteLineItems(row)"
                            :key="idx"
                            class="dock-quote-tier-line"
                          >
                            {{ formatSoQuoteTierQuantity(it.quantity) }}
                          </div>
                        </template>
                        <span v-else class="dock-tier-empty">—</span>
                      </div>
                    </template>
                  </el-table-column>
                  <el-table-column label="报价" min-width="128" align="right" class-name="dock-tier-col">
                    <template #default="{ row }">
                      <div class="dock-quote-tiers">
                        <template v-if="soQuoteLineItems(row).length">
                          <div
                            v-for="(it, idx) in soQuoteLineItems(row)"
                            :key="idx"
                            class="dock-quote-tier-line dock-tier-price-line"
                          >
                            <template v-if="!soQuoteTierUnitPriceHasValue(it.unitPrice)">—</template>
                            <template v-else>
                              <template v-for="amt in [splitSoQuoteTierAmountParts(it.unitPrice)]" :key="idx + '-amt'">
                                <span class="dock-tier-amt">
                                  <span class="dock-tier-amt-int">{{ amt.intPart }}</span
                                  ><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
                                </span>
                              </template>
                              <span class="dock-tier-ccy-gap">&nbsp;</span>
                              <span :class="['dock-tier-ccy', soQuoteTierCurrencyCodeClass(it.currency)]">{{
                                soQuoteTierCurrencyCode(it.currency)
                              }}</span>
                            </template>
                          </div>
                        </template>
                        <span v-else class="dock-tier-empty">—</span>
                      </div>
                    </template>
                  </el-table-column>
                  <el-table-column label="状态" width="100">
                    <template #default="{ row }">{{ quoteStatusLabel(row?.status) }}</template>
                  </el-table-column>
                  <el-table-column label="销售员" width="110" show-overflow-tooltip>
                    <template #default="{ row }">{{ row.salesUserName || '—' }}</template>
                  </el-table-column>
                  <el-table-column label="采购员" width="110" show-overflow-tooltip>
                    <template #default="{ row }">{{ row.purchaseUserName || '—' }}</template>
                  </el-table-column>
                  <el-table-column label="报价日期" width="160">
                    <template #default="{ row }">{{ formatDateTime(row?.quoteDate) }}</template>
                  </el-table-column>
                  <el-table-column label="创建时间" width="160">
                    <template #default="{ row }">{{ formatDateTime(row?.createTime) }}</template>
                  </el-table-column>
                </el-table>
                <DetailListPanelEmpty v-else size="low" />
              </div>
              <div v-show="soItemLinePanel.activeTab === 'pr'" class="so-aggregate-table-wrap">
                <el-table
                  v-if="(lineTabAggregates?.purchaseRequisitions?.length ?? 0) > 0"
                  :data="lineTabAggregates?.purchaseRequisitions ?? []"
                  size="small"
                  stripe
                >
                  <el-table-column type="index" width="50" label="#" />
                  <el-table-column min-width="200" label="申请单号">
                    <template #default="{ row }">
                      <router-link class="so-tab-link" :to="`/purchase-requisitions/${row.id}`">{{ row.billCode }}</router-link>
                    </template>
                  </el-table-column>
                  <el-table-column label="状态" width="100" prop="status">
                    <template #default="{ row }">{{ prStatusLabel(row?.status) }}</template>
                  </el-table-column>
                  <el-table-column prop="pn" label="PN" min-width="140" show-overflow-tooltip />
                  <el-table-column prop="brand" label="品牌" width="120" show-overflow-tooltip />
                  <el-table-column label="数量" width="100" align="right" prop="qty" />
                  <el-table-column label="预计采购" width="160" prop="expectedPurchaseTime">
                    <template #default="{ row }">{{ formatDateTime(row?.expectedPurchaseTime) }}</template>
                  </el-table-column>
                  <el-table-column label="创建时间" width="160" prop="createTime">
                    <template #default="{ row }">{{ formatDateTime(row?.createTime) }}</template>
                  </el-table-column>
                </el-table>
                <DetailListPanelEmpty v-else size="low" />
              </div>
              <div v-show="soItemLinePanel.activeTab === 'po'" class="so-aggregate-table-wrap">
                <el-table
                  v-if="(lineTabAggregates?.purchaseOrderItems?.length ?? 0) > 0"
                  :data="lineTabAggregates?.purchaseOrderItems ?? []"
                  size="small"
                  stripe
                >
                  <el-table-column type="index" width="50" label="#" />
                  <el-table-column min-width="130" label="采购单号">
                    <template #default="{ row }">
                      <router-link v-if="!maskPurchaseSensitiveFields" class="so-tab-link" :to="`/purchase-orders/${row.purchaseOrderId}`">
                        {{ row.purchaseOrderCode }}
                      </router-link>
                      <span v-else>{{ row.purchaseOrderCode || '—' }}</span>
                    </template>
                  </el-table-column>
                  <el-table-column min-width="130" label="采购明细号">
                    <template #default="{ row }">
                      <router-link v-if="!maskPurchaseSensitiveFields" class="so-tab-link" :to="`/purchase-orders/${row.purchaseOrderId}`">
                        {{ row.purchaseOrderItemCode }}
                      </router-link>
                      <span v-else>{{ row.purchaseOrderItemCode || '—' }}</span>
                    </template>
                  </el-table-column>
                  <el-table-column label="主单状态" width="100">
                    <template #default="{ row }">{{ poHeaderStatusLabel(row?.poStatus) }}</template>
                  </el-table-column>
                  <el-table-column label="明细状态" width="100">
                    <template #default="{ row }">{{ poItemStatusLabel(row?.itemStatus) }}</template>
                  </el-table-column>
                  <el-table-column label="供应商" min-width="140" show-overflow-tooltip>
                    <template #default="{ row }">{{ formatVendorNameReadonly(row.vendorName, row.vendorEnglishName, { masked: maskPurchaseSensitiveFields }) }}</template>
                  </el-table-column>
                  <el-table-column prop="purchaseUserName" label="采购员" width="100" show-overflow-tooltip />
                  <el-table-column prop="pn" label="PN" min-width="140" show-overflow-tooltip />
                  <el-table-column prop="brand" label="品牌" width="120" show-overflow-tooltip />
                  <el-table-column label="数量" width="100" align="right" prop="qty" />
                  <el-table-column label="单价" width="110" align="right">
                    <template #default="{ row }">{{ maskPurchaseSensitiveFields ? '—' : formatPoLineCost(row) }}</template>
                  </el-table-column>
                  <el-table-column label="创建时间" width="160" prop="createTime">
                    <template #default="{ row }">{{ formatDateTime(row?.createTime) }}</template>
                  </el-table-column>
                </el-table>
                <DetailListPanelEmpty v-else size="low" />
              </div>
              <div v-show="soItemLinePanel.activeTab === 'stockIn'" class="so-aggregate-table-wrap">
                <el-table v-if="(lineTabAggregates?.stockIns?.length ?? 0) > 0" :data="lineTabAggregates?.stockIns ?? []" size="small" stripe>
                  <el-table-column type="index" width="50" label="#" />
                  <el-table-column min-width="180" label="入库单号">
                    <template #default="{ row }">
                      <router-link class="so-tab-link" :to="`/inventory/stock-in/${row.id}`">{{ row.stockInCode }}</router-link>
                    </template>
                  </el-table-column>
                  <el-table-column label="类型" width="100" prop="stockInType">
                    <template #default="{ row }">
                      <StockBizTypeTag biz="in" :type="row?.stockInType" />
                    </template>
                  </el-table-column>
                  <el-table-column label="状态" width="100" prop="status">
                    <template #default="{ row }">{{ stockInStatusLabel(row?.status) }}</template>
                  </el-table-column>
                  <el-table-column label="入库日期" width="160" prop="stockInDate">
                    <template #default="{ row }">{{ formatDateTime(row?.stockInDate) }}</template>
                  </el-table-column>
                  <el-table-column label="创建时间" width="160" prop="createTime">
                    <template #default="{ row }">{{ formatDateTime(row?.createTime) }}</template>
                  </el-table-column>
                </el-table>
                <DetailListPanelEmpty v-else size="low" />
              </div>
              <div v-show="soItemLinePanel.activeTab === 'stock'" class="so-aggregate-table-wrap">
                <el-table v-if="(lineTabAggregates?.stockItems?.length ?? 0) > 0" :data="lineTabAggregates?.stockItems ?? []" size="small" stripe>
                  <el-table-column type="index" width="50" label="#" />
                  <el-table-column min-width="170" label="在库明细号">
                    <template #default="{ row }">
                      <router-link class="so-tab-link" :to="`/inventory/stocks/${row.stockAggregateId}`">{{
                        row.stockItemCode || row.id
                      }}</router-link>
                    </template>
                  </el-table-column>
                  <el-table-column label="库存类型" width="88" align="center">
                    <template #default="{ row }">
                      <span
                        class="stock-type-chip"
                        :class="`stock-type-chip--${stockItemTypeKind(row)}`"
                      >{{ stockItemTypeLabel(row) }}</span>
                    </template>
                  </el-table-column>
                  <el-table-column prop="stockInCode" label="入库单号" min-width="150" show-overflow-tooltip />
                  <el-table-column label="入库日期" width="160" prop="stockInDate">
                    <template #default="{ row }">{{ row?.stockInDate ? formatDateTime(row.stockInDate) : '—' }}</template>
                  </el-table-column>
                  <el-table-column prop="warehouseName" label="仓库" min-width="130" show-overflow-tooltip />
                  <el-table-column label="地域" width="90" align="center">
                    <template #default="{ row }">
                      <span class="region-type-chip" :class="`region-type-chip--${stockRegionTypeKind(row?.regionType)}`">
                        <span>{{ stockRegionTypeLabel(row?.regionType) }}</span>
                      </span>
                    </template>
                  </el-table-column>
                  <el-table-column label="出库状态" width="100" align="center">
                    <template #default="{ row }">
                      <span class="outbound-status-chip" :class="`outbound-status-chip--${stockOutboundStatusKind(row?.stockOutStatus)}`">
                        <span>{{ stockOutboundStatusLabel(row?.stockOutStatus) }}</span>
                      </span>
                    </template>
                  </el-table-column>
                  <el-table-column prop="purchasePn" label="PN" min-width="140" show-overflow-tooltip />
                  <el-table-column prop="purchaseBrand" label="品牌" width="120" show-overflow-tooltip />
                  <el-table-column label="总入库数量" width="110" align="right" prop="qtyInbound" />
                  <el-table-column label="已出库数量" width="110" align="right" prop="qtyStockOut" />
                  <el-table-column label="现存量" width="100" align="right" prop="qtyRepertory" />
                  <el-table-column prop="purchaseOrderItemCode" label="采购明细号" min-width="130" show-overflow-tooltip />
                  <el-table-column prop="sellOrderItemCode" label="销售明细号" min-width="140" show-overflow-tooltip />
                  <el-table-column prop="batchNo" label="批次号" min-width="100" show-overflow-tooltip />
                  <el-table-column prop="locationId" label="库位" min-width="110" show-overflow-tooltip />
                  <el-table-column label="可用" width="100" align="right" prop="qtyRepertoryAvailable" />
                </el-table>
                <DetailListPanelEmpty v-else size="low" />
              </div>
              <div v-show="soItemLinePanel.activeTab === 'outNotify'" class="so-aggregate-table-wrap">
                <el-table
                  v-if="(lineTabAggregates?.stockOutRequests?.length ?? 0) > 0"
                  :data="lineTabAggregates?.stockOutRequests ?? []"
                  size="small"
                  stripe
                >
                  <el-table-column type="index" width="50" label="#" />
                  <el-table-column prop="requestCode" label="通知单号" min-width="160" show-overflow-tooltip />
                  <el-table-column prop="materialCode" label="型号" min-width="140" show-overflow-tooltip />
                  <el-table-column label="数量" width="100" align="right" prop="quantity" />
                  <el-table-column label="状态" width="100" prop="status">
                    <template #default="{ row }">{{ outReqStatusLabel(row?.status) }}</template>
                  </el-table-column>
                  <el-table-column label="申请日期" width="160" prop="requestDate">
                    <template #default="{ row }">{{ formatDateTime(row?.requestDate) }}</template>
                  </el-table-column>
                  <el-table-column
                    label="操作"
                    :width="soOutNotifyOpColWidth"
                    :min-width="soOutNotifyOpColMinWidth"
                    fixed="right"
                    align="center"
                    class-name="op-col"
                    label-class-name="op-col"
                  >
                    <template #header>
                      <div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="soOutNotifyOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="toggleSoOutNotifyOpCol"
            >
              {{ soOutNotifyOpColExpanded ? '>' : '<' }}
            </button>
          </div>
                    </template>
                    <template #default="{ row }">
                      <div @click.stop @dblclick.stop>
                        <template v-if="Number(row.status) !== 1">
                          <div v-if="soOutNotifyOpColExpanded">
                            <router-link
                              class="so-tab-link so-tab-link--sm"
                              :to="`/inventory/stock-out/create?requestId=${encodeURIComponent(String(row.id))}`"
                            >
                              {{ t('salesOrderDetailView.goExecute') }}
                            </router-link>
                          </div>
                          <el-dropdown v-else trigger="click" placement="bottom-end">
                            <div class="op-more-dropdown-trigger">
                              <button type="button" class="op-more-trigger">...</button>
                            </div>
                            <template #dropdown>
                              <el-dropdown-menu>
                                <el-dropdown-item @click.stop="goStockOutCreateFromNotify(row)">
                                  <span class="op-more-item op-more-item--primary">{{ t('salesOrderDetailView.goExecute') }}</span>
                                </el-dropdown-item>
                              </el-dropdown-menu>
                            </template>
                          </el-dropdown>
                        </template>
                        <span v-else class="cell-muted">—</span>
                      </div>
                    </template>
                  </el-table-column>
                </el-table>
                <DetailListPanelEmpty v-else size="low" />
              </div>
              <div v-show="soItemLinePanel.activeTab === 'stockOut'" class="so-aggregate-table-wrap">
                <el-table v-if="(lineTabAggregates?.stockOuts?.length ?? 0) > 0" :data="lineTabAggregates?.stockOuts ?? []" size="small" stripe>
                  <el-table-column type="index" width="50" label="#" />
                  <el-table-column min-width="180" label="出库单号">
                    <template #default="{ row }">
                      <router-link class="so-tab-link" :to="`/inventory/stock-out/${row.id}`">{{ row.stockOutCode }}</router-link>
                    </template>
                  </el-table-column>
                  <el-table-column label="状态" width="100" prop="status">
                    <template #default="{ row }">{{ stockOutStatusLabel(row?.status) }}</template>
                  </el-table-column>
                  <el-table-column label="总数量" width="100" align="right" prop="totalQuantity" />
                  <el-table-column label="出库日期" width="160" prop="stockOutDate">
                    <template #default="{ row }">{{ formatDateTime(row?.stockOutDate) }}</template>
                  </el-table-column>
                  <el-table-column label="创建时间" width="160" prop="createTime">
                    <template #default="{ row }">{{ formatDateTime(row?.createTime) }}</template>
                  </el-table-column>
                </el-table>
                <DetailListPanelEmpty v-else size="low" />
              </div>
              <div v-show="soItemLinePanel.activeTab === 'receipt'" class="so-aggregate-table-wrap">
                <el-table v-if="(lineTabAggregates?.receipts?.length ?? 0) > 0" :data="lineTabAggregates?.receipts ?? []" size="small" stripe>
                  <el-table-column type="index" width="50" label="#" />
                  <el-table-column min-width="200" label="收款单号">
                    <template #default="{ row }">
                      <router-link class="so-tab-link" :to="`/finance/receipts/${row.id}`">{{ row.financeReceiptCode }}</router-link>
                    </template>
                  </el-table-column>
                  <el-table-column prop="customerName" label="客户" min-width="160" show-overflow-tooltip />
                  <el-table-column label="状态" width="100" prop="status">
                    <template #default="{ row }">{{ receiptStatusLabel(row?.status) }}</template>
                  </el-table-column>
                  <el-table-column v-if="showSalesMoneyFields" label="金额" width="120" align="right">
                    <template #default="{ row }">
                      <span v-if="row.receiptAmount != null" class="amount-with-code">
                        <span>{{ formatTotalAmountNumber(row.receiptAmount) }}</span>
                        <span
                          v-if="formatTotalAmountNumber(row.receiptAmount) !== '—'"
                          :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.receiptCurrency)]"
                          >{{ listAmountCurrencyIso(row.receiptCurrency) }}</span
                        >
                      </span>
                      <span v-else>—</span>
                    </template>
                  </el-table-column>
                  <el-table-column v-else label="金额" width="100" align="right">
                    <template #default>—</template>
                  </el-table-column>
                  <el-table-column label="收款日期" width="120" prop="receiptDate">
                    <template #default="{ row }">{{
                      row?.receiptDate ? formatDateTime(row.receiptDate) : '—'
                    }}</template>
                  </el-table-column>
                  <el-table-column label="创建时间" width="160" prop="createTime">
                    <template #default="{ row }">{{ formatDateTime(row?.createTime) }}</template>
                  </el-table-column>
                </el-table>
                <DetailListPanelEmpty v-else size="low" />
              </div>
              <div v-show="soItemLinePanel.activeTab === 'sellInvoice'" class="so-aggregate-table-wrap">
                <el-table v-if="(lineTabAggregates?.sellInvoices?.length ?? 0) > 0" :data="lineTabAggregates?.sellInvoices ?? []" size="small" stripe>
                  <el-table-column type="index" width="50" label="#" />
                  <el-table-column min-width="200" label="发票单号">
                    <template #default="{ row }">
                      <router-link class="so-tab-link" :to="`/finance/sell-invoices/${row.id}`">{{ row.invoiceCode || row.id }}</router-link>
                    </template>
                  </el-table-column>
                  <el-table-column prop="invoiceNo" label="纸票号码" min-width="120" show-overflow-tooltip />
                  <el-table-column prop="customerName" label="客户" min-width="160" show-overflow-tooltip />
                  <el-table-column label="状态" width="120" prop="invoiceStatus">
                    <template #default="{ row }">{{ sellInvoiceStatusLabel(row?.invoiceStatus) }}</template>
                  </el-table-column>
                  <el-table-column v-if="showSalesMoneyFields" label="发票总额" width="120" align="right">
                    <template #default="{ row }">
                      <span v-if="row.invoiceTotal != null" class="amount-with-code">
                        <span>{{ formatTotalAmountNumber(row.invoiceTotal) }}</span>
                        <span
                          v-if="formatTotalAmountNumber(row.invoiceTotal) !== '—'"
                          :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]"
                          >{{ listAmountCurrencyIso(row.currency) }}</span
                        >
                      </span>
                      <span v-else>—</span>
                    </template>
                  </el-table-column>
                  <el-table-column v-else label="发票总额" width="100" align="right">
                    <template #default>—</template>
                  </el-table-column>
                  <el-table-column label="开票日期" width="120" prop="makeInvoiceDate">
                    <template #default="{ row }">{{
                      row?.makeInvoiceDate ? formatDateTime(row.makeInvoiceDate) : '—'
                    }}</template>
                  </el-table-column>
                  <el-table-column label="创建时间" width="160" prop="createTime">
                    <template #default="{ row }">{{ formatDateTime(row?.createTime) }}</template>
                  </el-table-column>
                </el-table>
                <DetailListPanelEmpty v-else size="low" />
              </div>
              <div v-show="soItemLinePanel.activeTab === 'qcImages'" class="so-aggregate-table-wrap so-qc-images-wrap">
                <QcImagesReadonlyGallery
                  :images="lineTabAggregates?.qcImages ?? []"
                  :empty-text="t('salesOrderDetailView.emptyQcImages')"
                />
              </div>
            </div>
          </div>
        </div>
      </div>

      <SalesOrderStockOutBatchPanel
        v-if="!maskSaleSensitiveFields"
        :sales-order-id="String(order.id)"
        :sales-order-code="order.sellOrderCode || ''"
      />
    </template>

    <el-empty v-else :description="loadError || '订单不存在'" />

    <!-- 标签弹窗 -->
    <ApplyTagsDialog
      v-model="tagDialogVisible"
      entity-type="SALES_ORDER"
      :entity-ids="order ? [order.id] : []"
      title="为销售订单添加标签"
      @success="refreshTags"
    />

    <ApplyStockOutDialog ref="applyStockOutDialogRef" @success="onApplyStockOutSuccess" />

    <!-- 新建采购申请（与「销售订单明细」列表页逻辑一致） -->
    <el-dialog v-model="prApplyDialogVisible" title="新建采购申请" width="720px" destroy-on-close>
      <el-form ref="prApplyFormRef" :model="prApplyForm" :rules="prApplyRules" label-width="140px" v-loading="prApplyLoading">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="物料型号">
              <el-input v-model="prApplyForm.pn" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="品牌">
              <el-input v-model="prApplyForm.brand" disabled />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="订单明细数量">
              <el-input :model-value="prApplyFormSalesOrderQtyText" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="已下采购量">
              <el-input :model-value="prApplyFormPurchasedQtyText" disabled />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="进行中申请">
              <el-input :model-value="prApplyFormOpenPrQtyText" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="可申请数量">
              <el-input :model-value="prApplyFormRemainingQtyText" disabled />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="本次申请数量" prop="requestQty">
              <el-input-number
                v-model="prApplyForm.requestQty"
                :min="0"
                :precision="0"
                :step="1"
                :max="prApplyForm.remainingQty"
                controls-position="right"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="预计采购日期" prop="expectedPurchaseDate">
              <el-date-picker
                v-model="prApplyForm.expectedPurchaseDate"
                type="date"
                placeholder="请选择预计采购日期"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="备注">
          <el-input v-model="prApplyForm.remark" type="textarea" rows="3" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="prApplyDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="prApplySubmitting" :disabled="prApplyLoading" @click="submitPrApply">确认</el-button>
      </template>
    </el-dialog>

  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch, nextTick, defineAsyncComponent } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import salesOrderApi, {
  type SalesOrderItemExtendRefreshResult,
  type SalesOrderDetailTabAggregates,
  type SalesOrderFieldChangeLogRow,
  type SalesOrderDeletedItemRow
} from '@/api/salesOrder'
import { financeCustomerAdvanceApi } from '@/api/financeCustomerAdvance'
import { CURRENCY_MAP } from '@/api/finance'
import { getApiErrorMessage } from '@/utils/apiError'
import purchaseRequisitionApi from '@/api/purchaseRequisition'
import { runSaveTask, validateElFormOrWarn } from '@/composables/useFormSubmit'
import { favoriteApi } from '@/api/favorite'
import {
  translateSalesOrderStatus,
  salesOrderStatusTagType,
  salesOrderMainAllowsPurchaseAndStockOut,
  salesOrderLineApplyStockOutButtonDisabled,
  salesOrderLinePurchasedStockReliefOk,
  salesOrderLineApplyStockOutDisabled
} from '@/constants/salesOrderStatus'
import { buildApplyStockOutDisabledHintContent } from '@/utils/applyStockOutDisabledHint'
import type { ApplyStockOutDisabledHintContent } from '@/utils/applyStockOutDisabledHint'
import {
  SALES_ORDER_FAVORITE_ENTITY_TYPE,
  SALES_ORDER_FAVORITES_CHANGED_EVENT
} from '@/constants/salesOrderFavorites'
import { recordSalesOrderRecentView } from '@/utils/salesOrderRecentHistory'
import { tagApi, type TagDefinitionDto } from '@/api/tag'
import { useAuthStore } from '@/stores/auth'
import TagListDisplay from '@/components/Tag/TagListDisplay.vue'
import ApplyTagsDialog from '@/components/Tag/ApplyTagsDialog.vue'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import { documentApi } from '@/api/document'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatTotalAmountNumber, formatUnitPriceNumber, listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'
import { productionDateDisplayLabel, useMaterialProductionDateDict } from '@/composables/useMaterialProductionDateDict'
import ApplyStockOutDialog from '@/components/RFQ/ApplyStockOutDialog.vue'
import QcImagesReadonlyGallery from '@/components/Logistics/QcImagesReadonlyGallery.vue'
import { REGION_TYPE_OVERSEAS, normalizeRegionType } from '@/constants/regionType'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { quoteMainStatusI18nKey } from '@/utils/quoteMainStatus'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { formatVendorNameReadonly } from '@/utils/vendorDisplayName'
import { useSaleOrderWriteGate } from '@/composables/useDepartmentDataReadOnly'
import ApplyStockOutDisabledHint from '@/components/RFQ/ApplyStockOutDisabledHint.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'

const SalesOrderStockOutBatchPanel = defineAsyncComponent(
  () => import('@/components/Inventory/SalesOrderStockOutBatchPanel.vue')
)

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const authStore = useAuthStore()
const { options: materialPdOptions, ensureLoaded: ensureMaterialPdDict } = useMaterialProductionDateDict()
function fmtSoItemDateCode(row: { dateCode?: string; DateCode?: string } | null | undefined) {
  if (!row) return '—'
  const raw = String(row.dateCode ?? row.DateCode ?? '').trim()
  if (!raw) return '—'
  return productionDateDisplayLabel(raw, materialPdOptions.value) || raw
}

function fmtSoItemDeliveryDate(row: { deliveryDate?: unknown; DeliveryDate?: unknown } | null | undefined) {
  if (!row) return '—'
  const v = row.deliveryDate ?? row.DeliveryDate
  if (v == null || v === '') return '—'
  const s = formatDisplayDate(v as string | Date)
  return s === '--' ? '—' : s
}

const canViewCustomerInfo = computed(() => authStore.hasPermission('customer.info.read'))
const canViewSalesAmount = computed(() => authStore.hasPermission('sales.amount.read'))
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const showCustomerIdentityFields = computed(() => canViewCustomerInfo.value && !maskSaleSensitiveFields.value)
const showSalesMoneyFields = computed(() => canViewSalesAmount.value && !maskSaleSensitiveFields.value)
const { canWriteSo } = useSaleOrderWriteGate()

/** 与采购订单列表原「取消订单」一致：审核通过(10)前可取消主单为 -2；已取消不可再取消 */
const canCancelSalesOrderFromMenu = computed(() => {
  const o = order.value
  if (!o || !canWriteSo.value) return false
  const s = Number(o.status)
  if (!Number.isFinite(s) || s === -2) return false
  return s < 10
})
const canPurchaseReq = computed(
  () =>
    authStore.hasPermission('purchase-requisition.write') ||
    authStore.hasPermission('sales-order.write')
)

function mainAllowsOps(_row?: unknown) {
  return order.value != null && salesOrderMainAllowsPurchaseAndStockOut(Number(order.value.status))
}

/** 剩余可采为 0 时禁用「申请采购」（与明细列表口径一致） */
function applyPurchaseDisabled(row: Record<string, unknown>) {
  const raw = (row as { purchaseRemainingQty?: unknown }).purchaseRemainingQty
  if (raw === undefined || raw === null) return false
  const n = Number(raw)
  if (!Number.isFinite(n)) return false
  return n <= 0
}

function goSoItemDetail(row: Record<string, unknown>) {
  void onSalesOrderItemRowDblClick(row)
}

function goSoItemEdit(_row: Record<string, unknown>) {
  handleEdit()
}

async function applyPurchaseOne(row: Record<string, unknown>) {
  if (applyPurchaseDisabled(row)) {
    ElMessage.warning(t('salesOrderItemList.messages.prLineNotAvailable'))
    return
  }
  if (!mainAllowsOps(row)) {
    ElMessage.warning(t('salesOrderItemList.messages.applyPurchaseNeedAudit'))
    return
  }
  await handleOpenApplyPurchase(row)
}

function applyStockOutDisabledHint(row: Record<string, unknown>): ApplyStockOutDisabledHintContent | null {
  return buildApplyStockOutDisabledHintContent(row, t)
}

function onApplyStockOutDropdownClick(row: Record<string, unknown>) {
  if (salesOrderLineApplyStockOutButtonDisabled(row)) return
  applyStockOutOne(row)
}

function buildApplyStockOutOrderContext() {
  if (!order.value) return null
  return {
    salesOrderId: order.value.id,
    customerId: order.value.customerId || '',
    customerName: order.value.customerName || '',
    sellOrderCode: order.value.sellOrderCode || ''
  }
}

function onApplyStockOutSuccess() {
  router.push('/stock-out-notifies')
}

async function applyStockOutOne(row: Record<string, unknown>) {
  if (salesOrderLineApplyStockOutButtonDisabled(row)) return
  if (!mainAllowsOps(row)) {
    ElMessage.warning(t('salesOrderItemList.messages.applyStockOutNeedAudit'))
    return
  }
  if (!stockOutApplyPurchaseGateOk(row) && !salesOrderLinePurchasedStockReliefOk(row)) {
    ElMessage.warning(t('salesOrderItemList.messages.applyStockOutNeedPurchaseGate'))
    return
  }
  if (salesOrderLineApplyStockOutDisabled(row) && !salesOrderLinePurchasedStockReliefOk(row)) {
    ElMessage.warning(t('salesOrderItemList.messages.applyStockOutDisabledByProgress'))
    return
  }
  const ctx = buildApplyStockOutOrderContext()
  if (!ctx) return
  await applyStockOutDialogRef.value?.open(ctx, row)
}

function stockOutApplyPurchaseGateOk(row: any) {
  return row?.stockOutApplyPurchaseGateOk === true
}

// —— 明细行「申请采购」弹窗（与 SalesOrderItemList 一致）——
const prApplyDialogVisible = ref(false)
const prApplyLoading = ref(false)
const prApplySubmitting = ref(false)
const prApplyFormRef = ref<FormInstance>()
const prApplyForm = reactive({
  sellOrderItemId: '' as string,
  pn: '',
  brand: '',
  salesOrderQty: 0,
  purchasedQty: 0,
  openPurchaseRequisitionQty: 0,
  remainingQty: 0,
  requestQty: 0,
  expectedPurchaseDate: '' as string,
  remark: ''
})
const prApplyRules: FormRules = {
  requestQty: [{ required: true, message: '请输入本次申请数量', trigger: 'change' }],
  expectedPurchaseDate: [{ required: true, message: '请选择预计采购日期', trigger: 'change' }]
}
const prApplyFormSalesOrderQtyText = computed(() => String(Math.trunc(Number(prApplyForm.salesOrderQty ?? 0) || 0)))
const prApplyFormPurchasedQtyText = computed(() => String(Math.trunc(Number(prApplyForm.purchasedQty ?? 0) || 0)))
const prApplyFormOpenPrQtyText = computed(() => String(Math.trunc(Number(prApplyForm.openPurchaseRequisitionQty ?? 0) || 0)))
const prApplyFormRemainingQtyText = computed(() => String(Math.trunc(Number(prApplyForm.remainingQty ?? 0) || 0)))

function prApplyFormReset() {
  prApplyForm.sellOrderItemId = ''
  prApplyForm.pn = ''
  prApplyForm.brand = ''
  prApplyForm.salesOrderQty = 0
  prApplyForm.purchasedQty = 0
  prApplyForm.openPurchaseRequisitionQty = 0
  prApplyForm.remainingQty = 0
  prApplyForm.requestQty = 0
  prApplyForm.remark = ''
  prApplyForm.expectedPurchaseDate = new Date().toISOString().slice(0, 10)
}

async function submitPrApply() {
  if (!prApplyFormRef.value) return
  const ok = await validateElFormOrWarn(prApplyFormRef)
  if (!ok) return
  if (prApplyForm.requestQty <= 0) {
    ElMessage.warning('本次申请数量必须大于 0')
    return
  }
  if (prApplyForm.requestQty > prApplyForm.remainingQty) {
    ElMessage.warning('本次申请数量不能大于可申请数量')
    return
  }
  if (!prApplyForm.expectedPurchaseDate) {
    ElMessage.warning('请选择预计采购日期')
    return
  }
  const created = await runSaveTask({
    loading: prApplySubmitting,
    task: async () => {
      const expectedPurchaseTime = `${prApplyForm.expectedPurchaseDate}T00:00:00.000Z`
      return purchaseRequisitionApi.create({
        sellOrderItemId: prApplyForm.sellOrderItemId,
        qty: prApplyForm.requestQty,
        expectedPurchaseTime,
        type: 0,
        remark: prApplyForm.remark || undefined
      })
    },
    formatSuccess: () => '采购申请已创建',
    errorMessage: (e: unknown) => {
      const err = e as { response?: { data?: { message?: string } }; message?: string }
      return err?.response?.data?.message || err?.message || '创建失败'
    }
  })
  if (!created) return
  prApplyDialogVisible.value = false
  await fetchOrder()
}

function normId(s: unknown) {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

function stockRegionTypeLabel(regionType: unknown): string {
  const n = normalizeRegionType(regionType)
  return n === REGION_TYPE_OVERSEAS ? '海外' : '大陆'
}

function stockRegionTypeKind(regionType: unknown): 'domestic' | 'overseas' {
  const n = normalizeRegionType(regionType)
  return n === REGION_TYPE_OVERSEAS ? 'overseas' : 'domestic'
}

function stockOutboundStatusLabel(status: unknown): string {
  const n = Number(status)
  if (n === 1) return '未出库'
  if (n === 2) return '部分出库'
  if (n === 3) return '出库完成'
  return '—'
}

function stockOutboundStatusKind(status: unknown): 'none' | 'partial' | 'done' | 'unknown' {
  const n = Number(status)
  if (n === 1) return 'none'
  if (n === 2) return 'partial'
  if (n === 3) return 'done'
  return 'unknown'
}

function stockItemTypeNum(row: { stockType?: unknown; isStockingPoolMatch?: unknown }): number {
  const n = Number(row.stockType ?? 1)
  if (n >= 1 && n <= 3) return n
  return row.isStockingPoolMatch ? 2 : 1
}

function stockItemTypeLabel(row: { stockType?: unknown; isStockingPoolMatch?: unknown }): string {
  const n = stockItemTypeNum(row)
  if (n === 2) return t('inventoryList.stockTypes.stocking')
  if (n === 3) return t('inventoryList.stockTypes.sample')
  return t('inventoryList.stockTypes.customer')
}

function stockItemTypeKind(row: { stockType?: unknown; isStockingPoolMatch?: unknown }): 'customer' | 'stocking' | 'sample' {
  const n = stockItemTypeNum(row)
  if (n === 2) return 'stocking'
  if (n === 3) return 'sample'
  return 'customer'
}

async function handleOpenApplyPurchase(row: any) {
  if (!order.value) return
  if (!salesOrderMainAllowsPurchaseAndStockOut(Number(order.value.status))) {
    ElMessage.warning('销售订单主表审核通过后，方可申请采购')
    return
  }
  prApplyFormReset()
  prApplyDialogVisible.value = true
  prApplyLoading.value = true
  try {
    const sellOrderId = order.value.id as string
    const sellOrderItemId = String(row.sellOrderItemId ?? row.id ?? row.Id ?? '').trim()

    const options = (await purchaseRequisitionApi.getLineOptions(sellOrderId)) || []
    const line = options.find((x: any) => normId(x.sellOrderItemId) === normId(sellOrderItemId))
    if (!line) {
      ElMessage.warning(t('salesOrderItemList.messages.prLineNotAvailable'))
      prApplyDialogVisible.value = false
      return
    }

    prApplyForm.sellOrderItemId = sellOrderItemId
    prApplyForm.pn = line.pn ?? row.pn ?? ''
    prApplyForm.brand = line.brand ?? row.brand ?? ''
    const toInt = (v: unknown) => Math.trunc(Number(v) || 0)
    prApplyForm.salesOrderQty = toInt(line.salesOrderQty ?? row.qty ?? 0)
    prApplyForm.purchasedQty = toInt(line.purchasedQty ?? 0)
    prApplyForm.openPurchaseRequisitionQty = toInt(line.openPurchaseRequisitionQty ?? 0)
    prApplyForm.remainingQty = toInt(line.remainingQty)
    prApplyForm.requestQty = Math.max(0, prApplyForm.remainingQty)
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.message || e?.message || '加载明细失败')
    prApplyDialogVisible.value = false
  } finally {
    prApplyLoading.value = false
  }
}

const loading = ref(false)
const refreshingExtends = ref(false)
const order = ref<any>(null)
const customerAdvanceText = ref('')
/** 加载失败时展示具体原因（权限/网络/库表等），避免一律显示「订单不存在」 */
const loadError = ref('')
const soFavorited = ref(false)
const favoriteLoading = ref(false)
const activeTab = ref('items')
const docListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)
const documentCount = ref(0)
const changeLogs = ref<SalesOrderFieldChangeLogRow[]>([])
const deletedItems = ref<SalesOrderDeletedItemRow[]>([])
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

/** Tab 标题旁显示 (N)，与底部销售订单明细详情面板一致 */
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
    const res = await documentApi.getDocuments('SALES_ORDER', id)
    documentCount.value = Array.isArray(res) ? res.length : 0
  } catch {
    documentCount.value = 0
  }
}

function onSoDocumentUploaded() {
  docListRef.value?.refresh()
  void fetchDocumentCount()
}

async function loadChangeLogs(opts?: { silent?: boolean }) {
  const id = String(order.value?.id ?? '').trim()
  if (!id) return
  changeLogsLoading.value = true
  try {
    changeLogs.value = (await salesOrderApi.getChangeLogs(id)) ?? []
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
    deletedItems.value = (await salesOrderApi.getDeletedItems(id)) ?? []
    deletedItemsLoaded.value = true
  } catch (e: unknown) {
    if (!opts?.silent) ElMessage.error(getApiErrorMessage(e, '加载删除日志失败'))
  } finally {
    deletedItemsLoading.value = false
  }
}

function formatDeletedItemPrice(row: SalesOrderDeletedItemRow) {
  const cost = Number(row?.price)
  if (!Number.isFinite(cost)) return '—'
  const cur = Number(row?.currency)
  const curLabel =
    cur === 2 ? 'USD' : cur === 3 ? 'EUR' : cur === 4 ? 'HKD' : cur === 1 ? 'RMB' : cur > 0 ? String(cur) : ''
  return curLabel ? `${cost.toFixed(4)} ${curLabel}` : cost.toFixed(4)
}

watch(activeTab, (tab) => {
  if (tab === 'changeLog' && !changeLogsLoaded.value) void loadChangeLogs()
  if (tab === 'deleteLog' && !deletedItemsLoaded.value) void loadDeletedItems()
})

/** 《列表操作列规范》：销售订单明细操作列（列宽与采购订单明细表对齐） */
const soDetailItemsOpColExpanded = ref(false)
const SO_DETAIL_ITEMS_OP_COL_EXPANDED_WIDTH = 173
const SO_DETAIL_ITEMS_OP_COL_COLLAPSED_WIDTH = 43
const SO_DETAIL_ITEMS_OP_COL_EXPANDED_MIN_WIDTH = 160
const soDetailItemsOpColWidth = computed(() =>
  soDetailItemsOpColExpanded.value ? SO_DETAIL_ITEMS_OP_COL_EXPANDED_WIDTH : SO_DETAIL_ITEMS_OP_COL_COLLAPSED_WIDTH
)
const soDetailItemsOpColMinWidth = computed(() =>
  soDetailItemsOpColExpanded.value ? SO_DETAIL_ITEMS_OP_COL_EXPANDED_MIN_WIDTH : SO_DETAIL_ITEMS_OP_COL_COLLAPSED_WIDTH
)
function toggleSoDetailItemsOpCol() {
  soDetailItemsOpColExpanded.value = !soDetailItemsOpColExpanded.value
}

/** 《列表操作列规范》：明细面板「出库通知」聚合表 */
const soOutNotifyOpColExpanded = ref(false)
const SO_OUT_NOTIFY_OP_COL_COLLAPSED = 43
const SO_OUT_NOTIFY_OP_COL_EXPANDED = 173
const SO_OUT_NOTIFY_OP_COL_EXPANDED_MIN = 160
const soOutNotifyOpColWidth = computed(() =>
  soOutNotifyOpColExpanded.value ? SO_OUT_NOTIFY_OP_COL_EXPANDED : SO_OUT_NOTIFY_OP_COL_COLLAPSED
)
const soOutNotifyOpColMinWidth = computed(() =>
  soOutNotifyOpColExpanded.value ? SO_OUT_NOTIFY_OP_COL_EXPANDED_MIN : SO_OUT_NOTIFY_OP_COL_COLLAPSED
)
function toggleSoOutNotifyOpCol() {
  soOutNotifyOpColExpanded.value = !soOutNotifyOpColExpanded.value
}
function goStockOutCreateFromNotify(row: Record<string, unknown>) {
  const id = String(row?.id ?? row?.Id ?? '').trim()
  if (!id) return
  router.push(`/inventory/stock-out/create?requestId=${encodeURIComponent(id)}`)
}

/** 双击订单明细行：底部「销售订单明细详情」面板数据（按销售明细主键） */
const lineTabAggregates = ref<SalesOrderDetailTabAggregates | null>(null)

type SoItemLineTabKey =
  | 'rfqItems'
  | 'quotes'
  | 'pr'
  | 'po'
  | 'stockIn'
  | 'stock'
  | 'outNotify'
  | 'stockOut'
  | 'receipt'
  | 'sellInvoice'
  | 'qcImages'

function soItemLineTabRecordCount(tab: SoItemLineTabKey): number {
  const agg = lineTabAggregates.value
  if (!agg) return 0
  switch (tab) {
    case 'rfqItems':
      return agg.rfqItems?.length ?? 0
    case 'quotes':
      return agg.quotes?.length ?? 0
    case 'pr':
      return agg.purchaseRequisitions?.length ?? 0
    case 'po':
      return agg.purchaseOrderItems?.length ?? 0
    case 'stockIn':
      return agg.stockIns?.length ?? 0
    case 'stock':
      return agg.stockItems?.length ?? 0
    case 'outNotify':
      return agg.stockOutRequests?.length ?? 0
    case 'stockOut':
      return agg.stockOuts?.length ?? 0
    case 'receipt':
      return agg.receipts?.length ?? 0
    case 'sellInvoice':
      return agg.sellInvoices?.length ?? 0
    case 'qcImages':
      return agg.qcImages?.length ?? 0
    default:
      return 0
  }
}

/** Tab 标题旁显示 (N)，便于未点开 Tab 时感知是否有数据 */
function formatSoItemLineTabLabel(label: string, tab: SoItemLineTabKey): string {
  const count = soItemLineTabRecordCount(tab)
  return count > 0 ? `${label} (${count})` : label
}

const soItemLinePanel = reactive({
  visible: false,
  sellOrderItemId: '',
  sellOrderItemCode: '',
  activeTab: 'rfqItems',
  loading: false,
  loadError: ''
})

function closeSoItemLinePanel() {
  soItemLinePanel.visible = false
  soItemLinePanel.loadError = ''
  lineTabAggregates.value = null
}

async function reloadSoItemLinePanelAggregates() {
  const oid = String(route.params.id ?? '').trim()
  const sellOrderItemId = soItemLinePanel.sellOrderItemId
  if (!oid || !sellOrderItemId || !soItemLinePanel.visible) return
  try {
    lineTabAggregates.value = await salesOrderApi.getSellOrderItemDetailTabAggregates(oid, sellOrderItemId)
  } catch {
    /* 刷新失败时保留原列表 */
  }
}

async function selectSalesOrderItemRow(row: Record<string, unknown>) {
  if (maskSaleSensitiveFields.value) return
  const orderId = String(route.params.id ?? '').trim()
  const sellOrderItemId = soItemRowKey(row)
  const sellOrderItemCode = String(row?.sellOrderItemCode ?? '').trim()
  if (!orderId || !sellOrderItemId) return
  soItemLinePanel.sellOrderItemId = sellOrderItemId
  soItemLinePanel.sellOrderItemCode = sellOrderItemCode || sellOrderItemId
  soItemLinePanel.visible = true
  soItemLinePanel.activeTab = 'rfqItems'
  soItemLinePanel.loading = true
  soItemLinePanel.loadError = ''
  lineTabAggregates.value = null
  try {
    lineTabAggregates.value = await salesOrderApi.getSellOrderItemDetailTabAggregates(orderId, sellOrderItemId)
  } catch (e: unknown) {
    soItemLinePanel.loadError = getApiErrorMessage(e, '加载明细关联数据失败')
  } finally {
    soItemLinePanel.loading = false
  }
}

async function onSalesOrderItemRowClick(row: Record<string, unknown>) {
  await selectSalesOrderItemRow(row)
}

async function onSalesOrderItemRowDblClick(row: Record<string, unknown>) {
  await selectSalesOrderItemRow(row)
}

function soItemRowKey(row: Record<string, unknown>) {
  return String(row?.sellOrderItemId ?? row?.id ?? row?.Id ?? '').trim()
}

function soItemRowClassName({ row }: { row: Record<string, unknown> }) {
  if (!soItemLinePanel.visible) return ''
  return soItemRowKey(row) === soItemLinePanel.sellOrderItemId ? 'so-item-row--active' : ''
}

watch(maskSaleSensitiveFields, (m) => {
  if (m && (activeTab.value === 'documents' || activeTab.value === 'changeLog' || activeTab.value === 'deleteLog')) {
    activeTab.value = 'items'
  }
  if (m) closeSoItemLinePanel()
})

watch(
  () => String(route.params.id ?? ''),
  () => {
    closeSoItemLinePanel()
  }
)

// 标签
const currentTags = ref<TagDefinitionDto[]>([])
const tagDialogVisible = ref(false)

const applyStockOutDialogRef = ref<InstanceType<typeof ApplyStockOutDialog> | null>(null)

const orderId = computed(() => {
  const raw = route.params.id
  if (Array.isArray(raw)) return String(raw[0] ?? '').trim()
  return String(raw ?? '').trim()
})

/** CaptionBar 头像缩写 */
const captionAvatarChar = computed(() => {
  const code = order.value?.sellOrderCode?.trim()
  return (code && code[0]) || '销'
})

const showSoHeaderTags = computed(() => canWriteSo.value || currentTags.value.length > 0)

const soBasicCreateDateText = computed(() => {
  const o = order.value
  if (!o?.createTime) return '—'
  const s = formatDisplayDate(o.createTime)
  return s === '--' ? '—' : s
})

const soBasicCreateUserText = computed(() => {
  const o = order.value as Record<string, unknown> | null | undefined
  if (!o) return '—'
  const name = o.createUserName ?? o.CreateUserName ?? o.createdBy
  const s = name != null ? String(name).trim() : ''
  return s || '—'
})

function onHeaderMoreCommand(cmd: string) {
  if (cmd === 'delete') void handleDeleteOrder()
}

async function handleCancelSalesOrder() {
  if (!order.value?.id || !canCancelSalesOrderFromMenu.value) return
  try {
    await ElMessageBox.confirm(
      `确认将销售订单 ${order.value.sellOrderCode} 标记为「取消」吗？`,
      '取消订单',
      { type: 'warning', confirmButtonText: '确认', cancelButtonText: '关闭' }
    )
    await salesOrderApi.updateStatus(order.value.id, -2)
    ElMessage.success('订单已取消')
    await fetchOrder()
  } catch {
    /* 取消 */
  }
}

async function handleDeleteOrder() {
  if (!order.value?.id) return
  try {
    await ElMessageBox.confirm(`确定要删除销售订单 ${order.value.sellOrderCode} 吗？`, '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消'
    })
    await salesOrderApi.delete(order.value.id)
    ElMessage.success('已删除')
    router.push({ name: 'SalesOrderList' })
  } catch {
    /* 取消 */
  }
}

onMounted(() => {
  void ensureMaterialPdDict()
  fetchOrder()
})

watch(orderId, () => {
  fetchOrder()
})

async function loadFavoriteState() {
  const id = orderId.value
  if (!id) {
    soFavorited.value = false
    return
  }
  try {
    soFavorited.value = await favoriteApi.checkFavorite(SALES_ORDER_FAVORITE_ENTITY_TYPE, id)
  } catch {
    soFavorited.value = false
  }
}

async function toggleFavorite() {
  const id = orderId.value
  if (!id || favoriteLoading.value) return
  favoriteLoading.value = true
  try {
    if (soFavorited.value) {
      await favoriteApi.removeFavorite(SALES_ORDER_FAVORITE_ENTITY_TYPE, id)
      soFavorited.value = false
    } else {
      await favoriteApi.addFavorite({ entityType: SALES_ORDER_FAVORITE_ENTITY_TYPE, entityId: id })
      soFavorited.value = true
    }
    window.dispatchEvent(new Event(SALES_ORDER_FAVORITES_CHANGED_EVENT))
  } catch {
    /* 全局拦截器已提示 */
  } finally {
    favoriteLoading.value = false
  }
}

const fetchOrder = async () => {
  loading.value = true
  loadError.value = ''
  customerAdvanceText.value = ''
  resetOrderLogTabs()
  try {
    const id = orderId.value
    if (!id) {
      order.value = null
      loadError.value = '链接中缺少订单编号'
      soFavorited.value = false
      return
    }
    order.value = await salesOrderApi.getById(id)
    soDetailItemsOpColExpanded.value = false
    if (order.value) {
      loadError.value = ''
      if (order.value.customerId && !maskSaleSensitiveFields.value) {
        try {
          const adv = await financeCustomerAdvanceApi.getBalance(order.value.customerId)
          const balances = (adv.balances ?? []).filter(b => (b.balance ?? 0) > 0)
          customerAdvanceText.value = balances.length
            ? balances
                .map(b => `${CURRENCY_MAP[b.currency] ?? b.currency} ${Number(b.balance).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`)
                .join(' / ')
            : '—'
        } catch {
          customerAdvanceText.value = ''
        }
      }
      refreshTags()
      recordSalesOrderRecentView({
        id: String(order.value.id),
        sellOrderCode: order.value.sellOrderCode,
        customerName: order.value.customerName
      })
      await loadFavoriteState()
      await nextTick()
      // 底部「销售订单明细详情」：默认按订单明细第一行加载（与双击同一接口）
      if (!maskSaleSensitiveFields.value) {
        const lines = order.value.items
        if (Array.isArray(lines) && lines.length > 0) {
          await onSalesOrderItemRowDblClick(lines[0] as Record<string, unknown>)
        }
      }
    } else {
      soFavorited.value = false
      loadError.value = '未找到该销售订单'
    }
  } catch (e) {
    order.value = null
    soFavorited.value = false
    loadError.value = getApiErrorMessage(e, '加载失败，请稍后重试')
    ElMessage.error(loadError.value)
  } finally {
    loading.value = false
    if (order.value?.id && !maskSaleSensitiveFields.value) {
      void fetchDocumentCount()
      if (!changeLogsLoaded.value) void loadChangeLogs({ silent: true })
      if (!deletedItemsLoaded.value) void loadDeletedItems({ silent: true })
    }
  }
}

const refreshTags = async () => {
  if (!order.value) return
  try {
    currentTags.value = await tagApi.getEntityTags('SALES_ORDER', order.value.id) || []
  } catch {
    currentTags.value = []
  }
}

const getStatusType = (status: number) => salesOrderStatusTagType(status)
const getStatusText = (status: number) => translateSalesOrderStatus(status, t)
// ===== 销售明细扩展：执行进度（0=待 1=部分 2=完成）=====
const getExtendTriStatusTagType = (v?: number): '' | 'info' | 'success' | 'warning' | 'danger' => {
  const map: Record<number, '' | 'info' | 'success' | 'warning' | 'danger'> = {
    0: 'info',
    1: 'warning',
    2: 'success'
  }
  return v !== undefined && v !== null ? (map[v] ?? 'info') : 'info'
}
const getPurchaseProgressText = (v?: number) => {
  const map: Record<number, string> = { 0: '待采购', 1: '采购中', 2: '采购完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}
const getStockInProgressText = (v?: number) => {
  const map: Record<number, string> = { 0: '待入库', 1: '部分入库', 2: '入库完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}
const getStockOutProgressText = (v?: number) => {
  const map: Record<number, string> = { 0: '待出库', 1: '部分出库', 2: '出库完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}
const getStockOutNotifyProgressText = (v?: number) => {
  const map: Record<number, string> = { 0: '未通知', 1: '部分通知', 2: '通知完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}
const getReceiptProgressText = (v?: number) => {
  const map: Record<number, string> = { 0: '待收款', 1: '部分收款', 2: '收款完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}
const getSellInvoiceProgressText = (v?: number) => {
  const map: Record<number, string> = { 0: '待开票', 1: '部分开票', 2: '开票完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}

function salesRefreshStatusText(field: string, value: string) {
  const n = Number(value)
  if (!Number.isFinite(n)) return value
  if (field === 'purchaseProgressStatus') {
    const map: Record<number, string> = { 0: '待采购', 1: '采购中', 2: '采购完成' }
    return map[n] ?? value
  }
  if (field === 'stockInProgressStatus') {
    const map: Record<number, string> = { 0: '待入库', 1: '部分入库', 2: '入库完成' }
    return map[n] ?? value
  }
  if (field === 'stockOutProgressStatus') {
    const map: Record<number, string> = { 0: '待出库', 1: '部分出库', 2: '出库完成' }
    return map[n] ?? value
  }
  if (field === 'stockOutNotifyProgressStatus') {
    const map: Record<number, string> = { 0: '待通知', 1: '部分通知', 2: '通知完成' }
    return map[n] ?? value
  }
  if (field === 'receiptProgressStatus') {
    const map: Record<number, string> = { 0: '待收款', 1: '部分收款', 2: '收款完成' }
    return map[n] ?? value
  }
  if (field === 'invoiceProgressStatus') {
    const map: Record<number, string> = { 0: '待开票', 1: '部分开票', 2: '开票完成' }
    return map[n] ?? value
  }
  return value
}

function buildSalesRefreshResultHtml(result: SalesOrderItemExtendRefreshResult) {
  const syncedNotifyCount = Number(result.syncedStockOutNotifyStatusCount ?? 0)
  const lines: string[] = [
    `共 ${result.changedItems} 条明细发生更新，${result.changedFieldsCount} 个字段已变更。`,
    `已同步回写 ${syncedNotifyCount} 条出库通知状态。`,
    ''
  ]
  for (const change of result.changes) {
    const lineCode = change.sellOrderItemCode || change.sellOrderItemId
    lines.push(`【${lineCode}】`)
    for (const field of change.fields) {
      const beforeText = salesRefreshStatusText(field.field, field.before)
      const afterText = salesRefreshStatusText(field.field, field.after)
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
      `确认刷新销售订单 ${order.value.sellOrderCode} 的明细执行状态与扩展字段吗？`,
      '刷新确认',
      { type: 'warning', confirmButtonText: '刷新', cancelButtonText: '取消' }
    )
  } catch {
    return
  }

  refreshingExtends.value = true
  try {
    const result = await salesOrderApi.refreshItemExtends(order.value.id)
    await fetchOrder()
    await reloadSoItemLinePanelAggregates()
    if (!result || result.changedItems <= 0) {
      await ElMessageBox.alert('无更新数据', '刷新结果', { confirmButtonText: '知道了' })
      return
    }
    await ElMessageBox.alert(buildSalesRefreshResultHtml(result), '刷新结果', {
      dangerouslyUseHTMLString: true,
      confirmButtonText: '知道了'
    })
  } catch (e) {
    await ElMessageBox.alert(
      getApiErrorMessage(e, '刷新失败，请稍后重试'),
      '刷新失败',
      { confirmButtonText: '知道了' }
    )
  } finally {
    refreshingExtends.value = false
  }
}

const formatDateTime = (v?: string | null | number) =>
  v != null && String(v).length > 0 ? formatDisplayDateTime(String(v)) : '--'

function prStatusLabel(v: unknown) {
  const s = Number(v)
  if (s === 0) return t('salesOrderDetailView.prStatus0')
  if (s === 1) return t('salesOrderDetailView.prStatus1')
  if (s === 2) return t('salesOrderDetailView.prStatus2')
  if (s === 3) return t('salesOrderDetailView.prStatus3')
  return `(${String(v)})`
}

function rfqItemStatusLabel(status?: unknown) {
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
  const s = Number(status)
  return Number.isFinite(s) ? (map[s] ?? `(${String(status)})`) : '—'
}

function formatRfqItemAssignedPurchasers(row: {
  assignedPurchaserName1?: string | null
  assignedPurchaserName2?: string | null
}) {
  const names = [row.assignedPurchaserName1, row.assignedPurchaserName2]
    .map((x) => String(x ?? '').trim())
    .filter(Boolean)
  return names.length ? names.join('、') : '—'
}

function quoteStatusLabel(status?: unknown) {
  const s = Number(status)
  return Number.isFinite(s) ? t(quoteMainStatusI18nKey(s)) : '—'
}

interface SoQuoteTierLine {
  quantity: number
  unitPrice: number
  currency: number
  vendorName?: string | null
}

function soQuoteLineItems(quoteRow: {
  items?: SoQuoteTierLine[] | null
}): SoQuoteTierLine[] {
  const raw = quoteRow.items
  if (!raw?.length) return []
  return raw.map((it) => ({
    quantity: Number(it.quantity ?? 0),
    unitPrice: Number(it.unitPrice ?? 0),
    currency: Number(it.currency ?? 1) || 1,
    vendorName: it.vendorName ?? null
  }))
}

function soQuoteVendorNamesDisplay(quoteRow: { items?: SoQuoteTierLine[] | null }): string {
  if (maskPurchaseSensitiveFields.value) return '—'
  const set = new Set<string>()
  for (const it of soQuoteLineItems(quoteRow)) {
    const n = String(it.vendorName ?? '').trim()
    if (n) set.add(n)
  }
  return set.size > 0 ? [...set].join('、') : '—'
}

function formatSoQuoteTierQuantity(q: number) {
  if (!Number.isFinite(q)) return '—'
  if (Math.abs(q - Math.round(q)) < 1e-9) return String(Math.round(q))
  return q.toLocaleString('zh-CN', { maximumFractionDigits: 4 })
}

function soQuoteTierCurrencyCode(currency?: number): string {
  const n = Number(currency)
  if (n === 2) return 'USD'
  if (n === 3) return 'EUR'
  if (n === 4) return 'HKD'
  if (n === 5) return 'JPY'
  if (n === 6) return 'GBP'
  return 'RMB'
}

function soQuoteTierCurrencyCodeClass(currency?: number): string {
  const n = Number(currency)
  if (n === 1 || !Number.isFinite(n) || n === 0) return 'dock-tier-ccy--rmb'
  if (n === 2) return 'dock-tier-ccy--usd'
  if (n === 3) return 'dock-tier-ccy--eur'
  if (n === 4) return 'dock-tier-ccy--hkd'
  return 'dock-tier-ccy--purple'
}

function soQuoteTierUnitPriceHasValue(unitPrice: number): boolean {
  return Number.isFinite(unitPrice) && unitPrice !== 0
}

function splitSoQuoteTierAmountParts(unitPrice: number): { intPart: string; fracPart: string } {
  if (!soQuoteTierUnitPriceHasValue(unitPrice)) return { intPart: '—', fracPart: '' }
  const parts = new Intl.NumberFormat('zh-CN', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 6
  }).formatToParts(Number(unitPrice))
  let intPart = ''
  let fracPart = ''
  for (const p of parts) {
    if (p.type === 'integer' || p.type === 'group') intPart += p.value
    else if (p.type === 'decimal' || p.type === 'fraction') fracPart += p.value
  }
  if (!fracPart) {
    return {
      intPart:
        intPart ||
        Number(unitPrice).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 6 }),
      fracPart: ''
    }
  }
  return { intPart, fracPart }
}

const PO_HEADER_STATUS_TEXT: Record<number, string> = {
  0: '草稿',
  1: '新建',
  2: '待审核',
  10: '审核通过',
  20: '待确认',
  30: '已确认',
  50: '进行中',
  100: '采购完成',
  [-1]: '审核失败',
  [-2]: '取消'
}

const PO_ITEM_STATUS_TEXT: Record<number, string> = {
  1: '新建',
  2: '待审核',
  10: '审核通过',
  20: '待确认',
  30: '已确认',
  40: '已付款',
  50: '已发货',
  60: '已入库',
  100: '采购完成',
  [-1]: '审核失败',
  [-2]: '取消'
}

function poHeaderStatusLabel(v: unknown) {
  const s = Number(v)
  return Number.isFinite(s) ? (PO_HEADER_STATUS_TEXT[s] ?? String(v)) : '—'
}

function poItemStatusLabel(v: unknown) {
  const s = Number(v)
  return Number.isFinite(s) ? (PO_ITEM_STATUS_TEXT[s] ?? String(v)) : '—'
}

function formatPoLineCost(row: { cost?: unknown; currency?: unknown }) {
  const cost = Number(row?.cost)
  if (!Number.isFinite(cost)) return '—'
  const cur = Number(row?.currency)
  const curLabel =
    cur === 2 ? 'USD' : cur === 3 ? 'EUR' : cur === 4 ? 'HKD' : cur === 1 ? 'RMB' : cur > 0 ? String(cur) : ''
  return curLabel ? `${cost.toFixed(4)} ${curLabel}` : cost.toFixed(4)
}
function stockInStatusLabel(v: unknown) {
  const s = Number(v)
  if (s === 0) return t('salesOrderDetailView.stockInSt0')
  if (s === 1) return t('salesOrderDetailView.stockInSt1')
  if (s === 2) return t('salesOrderDetailView.stockInSt2')
  if (s === 3) return t('salesOrderDetailView.stockInSt3')
  return `(${String(v)})`
}
function outReqStatusLabel(v: unknown) {
  const s = Number(v)
  if (s === 0) return t('salesOrderDetailView.outReqSt0')
  if (s === 1) return t('salesOrderDetailView.outReqSt1')
  if (s === 2) return t('salesOrderDetailView.outReqSt2')
  return `(${String(v)})`
}
function stockOutStatusLabel(v: unknown) {
  const s = Number(v)
  if (s === 0) return t('salesOrderDetailView.soSt0')
  if (s === 1) return t('salesOrderDetailView.soSt1')
  if (s === 2) return t('salesOrderDetailView.soSt2')
  if (s === 3) return t('salesOrderDetailView.soSt3')
  return `(${String(v)})`
}
function receiptStatusLabel(v: unknown) {
  const s = Number(v)
  if (s === 0) return t('salesOrderDetailView.recSt0')
  if (s === 1) return t('salesOrderDetailView.recSt1')
  if (s === 2) return t('salesOrderDetailView.recSt2')
  if (s === 3) return t('salesOrderDetailView.recSt3')
  if (s === 4) return t('salesOrderDetailView.recSt4')
  return `(${String(v)})`
}
function sellInvoiceStatusLabel(v: unknown) {
  const s = Number(v)
  if (s === 1) return t('salesOrderDetailView.invSt1')
  if (s === 2) return t('salesOrderDetailView.invSt2')
  if (s === 100) return t('salesOrderDetailView.invSt100')
  if (s === 101) return t('salesOrderDetailView.invSt101')
  if (s === -1) return t('salesOrderDetailView.invStNeg1')
  return `(${String(v)})`
}

const handleEdit = () => {
  if (!canWriteSo.value) {
    ElMessage.warning('无编辑权限')
    return
  }
  const id = orderId.value
  if (!id) return
  router.push({ name: 'SalesOrderEdit', params: { id } })
}

</script>

<style lang="scss" scoped>
/* UI：《业务详情页面规范.md》— 区块/Tab 标题栏、Key:Value 并排、§7.4 面板列表、嵌套明细面板 */
@import '@/assets/styles/variables.scss';

.sales-order-detail {
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
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
  &:hover { background: rgba(255,255,255,0.07); color: $text-secondary; border-color: rgba(0,212,255,0.2); }
}

.so-caption-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
}

.caption-avatar-lg {
  width: 48px;
  height: 48px;
  flex-shrink: 0;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
  color: $cyan-primary;
  border: 1px solid rgba(0, 212, 255, 0.25);
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.3), rgba(0, 212, 255, 0.2));
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

.so-header-meta-row {
  min-height: 28px;
}

.so-header-tags-row {
  flex-shrink: 0;
}

.so-header-add-tag-btn {
  padding: 6px 12px;
  font-size: 12px;
}

.so-header-add-tag-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 13px;
  font-size: 15px;
  font-weight: 500;
  line-height: 1;
}

.caption-code {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 11px;
  color: $text-muted;
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

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid rgba(0, 212, 255, 0.4);
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  cursor: pointer;
  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }
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

.btn-warning {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid rgba(201,154,69,0.4);
  color: $color-amber;
  font-size: 13px;
  background: rgba(201,154,69,0.15);
  cursor: pointer;
}

.btn-warning--sm {
  padding: 4px 10px;
  font-size: 12px;
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

.btn-close-so {
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

.section-header--with-inline-code {
  .section-header-left {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    column-gap: 14px;
    row-gap: 6px;
    min-width: 0;
    .section-dot {
      flex-shrink: 0;
    }
    .section-title {
      flex-shrink: 0;
    }
  }
  .section-header-code {
    display: flex;
    align-items: baseline;
    gap: 8px;
    flex-shrink: 1;
    min-width: 0;
    max-width: min(520px, 72vw);
  }
  .section-header-code__label {
    font-size: 12px;
    font-weight: 500;
    color: $text-muted;
    white-space: nowrap;
  }
  .section-header-code__value {
    font-size: 13px;
    font-weight: 500;
    color: $warning-color;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
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

.order-code {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 11px;
  color: $text-muted;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  border-bottom: 1px solid rgba(255,255,255,0.04);
  border-right: 1px solid rgba(255,255,255,0.04);
  &:nth-child(4n) { border-right: none; }
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
  grid-template-columns: repeat(3, 1fr);
  .info-item {
    &:nth-child(4n) { border-right: 1px solid rgba(255,255,255,0.04); }
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
  color: $text-primary;
  font-weight: 400;
}

.amount-with-code {
  display: inline-flex;
  align-items: baseline;
  gap: 4px;
}

.info-value--time {
  font-size: 12px;
  color: $text-muted;
}

.info-value--warn {
  color: #f89898;
  white-space: pre-wrap;
}

.tags-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
}

.btn-add-tag {
  padding: 3px 8px;
  border-radius: 999px;
  border: 1px dashed rgba(0, 212, 255, 0.35);
  background: transparent;
  color: rgba(200, 216, 232, 0.85);
  font-size: 11px;
  cursor: pointer;
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
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  margin-bottom: -1px;
  display: flex;
  align-items: center;
  gap: 6px;
  &:hover { color: $text-secondary; }
}

.tab-btn--active {
  color: $cyan-primary;
  border-bottom-color: $cyan-primary;
}

.tabs-body {
  padding: 20px;
}

.so-aggregate-table-wrap {
  margin-top: 4px;
}
.so-tab-link {
  color: $cyan-primary;
  text-decoration: none;
  font-weight: 500;
  &:hover {
    text-decoration: underline;
  }
}
.so-tab-link--sm {
  font-size: 12px;
  font-weight: 500;
}

.outbound-status-chip {
  display: inline-flex;
  align-items: center;
  gap: 0;
  justify-content: center;
  min-width: 56px;
  padding: 3px 10px;
  border-radius: 5px;
  font-size: 12px;
  line-height: 1.1;
  font-weight: 400;
  color: #fff;
  border: none;
  white-space: nowrap;
}

.outbound-status-chip--none {
  background: #9ca3af;
}

.outbound-status-chip--partial {
  background: #e6a23c;
}

.outbound-status-chip--done {
  background: #67c23a;
}

.outbound-status-chip--unknown {
  background: #9ca3af;
}

.region-type-chip {
  display: inline-flex;
  align-items: center;
  gap: 0;
  padding: 2px 8px;
  border-radius: 999px;
  font-size: 12px;
  line-height: 1.2;
}

.region-type-chip--domestic {
  color: #e6a23c;
  background: rgba(230, 162, 60, 0.14);
}

.region-type-chip--overseas {
  color: #409eff;
  background: rgba(64, 158, 255, 0.14);
}

.stock-type-chip {
  display: inline-flex;
  align-items: center;
  padding: 2px 8px;
  border-radius: 999px;
  font-size: 12px;
  line-height: 1.2;
}

.stock-type-chip--customer {
  color: #67c23a;
  background: rgba(103, 194, 58, 0.14);
}

.stock-type-chip--stocking {
  color: #e6a23c;
  background: rgba(230, 162, 60, 0.14);
}

.stock-type-chip--sample {
  color: #909399;
  background: rgba(144, 147, 153, 0.14);
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
  :deep(th.op-col .so-detail-op-col-header--icon-only) {
    display: flex;
    justify-content: center;
    align-items: center;
    width: 100%;
  }
  :deep(th.op-col .so-detail-op-col-toggle) {
    min-width: 28px;
    min-height: 28px;
    font-size: 18px;
    font-weight: 700;
    line-height: 1;
  }
  /** 物料明细表：与编辑页一致的第三列空白占位 */
  :deep(th.so-item-col-spacer .cell),
  :deep(td.so-item-col-spacer .cell) {
    padding-left: 2px;
    padding-right: 2px;
  }

  .op-more-item {
    font-size: 13px;
    line-height: 1.4;
  }

  .op-more-item--primary {
    color: $cyan-primary;
  }

  .op-more-item--warning {
    color: $color-amber;
  }

  .op-more-item--disabled {
    color: $text-muted !important;
  }

  .op-more-item-row {
    display: inline-flex;
    align-items: center;
    gap: 2px;
    max-width: 100%;
  }

  .action-with-hint {
    display: inline-flex;
    align-items: center;
    gap: 2px;
    vertical-align: middle;
  }

  :deep(.action-btns .el-button.is-disabled.is-link.el-button--warning) {
    color: $text-muted !important;
    --el-button-hover-link-text-color: #{$text-muted};
  }
}

.so-detail-items-table {
  cursor: pointer;
}

.doc-tab-content {
  padding-top: 4px;
}

.so-item-line-detail-panel {
  margin-top: 20px;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  background: var(--crm-detail-panel-card-bg);
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

/* 与上方主卡片「订单明细 / 采购申请…」同一套 tabs-nav / tab-btn 样式，去掉嵌套双框 */
.so-item-line-detail-tabs-section.tabs-section {
  background: transparent;
  border: none;
  border-radius: 0;
  padding: 0;
  margin: 0;
}
</style>

<!-- 顶栏「更多」下拉 Teleport 到 body -->
<style lang="scss">
@import '@/assets/styles/variables.scss';

.so-detail-header-more-popper.el-dropdown__popper,
.so-detail-header-more-popper.el-popper {
  background: $layer-2 !important;
  border: 1px solid rgba(0, 212, 255, 0.15) !important;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.45) !important;
}

.so-detail-header-more-popper .el-dropdown-menu {
  background: transparent !important;
  border: none !important;
  box-shadow: none !important;
  padding: 4px 0 !important;
}

.so-detail-header-more-popper .el-dropdown-menu__item {
  color: rgba(200, 220, 240, 0.92) !important;
  font-size: 13px;

  &:hover,
  &:focus {
    background: rgba(0, 212, 255, 0.1) !important;
    color: #e8f4ff !important;
  }
}

.so-detail-header-more-popper .detail-more-item--danger {
  color: rgba(245, 108, 108, 0.95) !important;
  &:hover,
  &:focus {
    background: rgba(245, 108, 108, 0.12) !important;
    color: #ff9a9a !important;
  }
}

</style>
