<template>
  <div class="stockin-edit-page" v-loading="detailLoading">
    <div class="page-header">
      <template v-if="isCreateMode">
        <div class="header-left">
          <div class="page-title-group">
            <div class="page-icon">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
                <path d="M3 9h18" />
                <path d="M9 21V9" />
              </svg>
            </div>
            <h1 class="page-title">新建入库单</h1>
          </div>
        </div>
        <div class="header-right">
          <button class="btn-secondary" type="button" @click="goBack">返回列表</button>
          <button
            v-if="canWriteLogisticsData"
            class="btn-primary"
            type="button"
            @click="handleSubmit"
            :disabled="submitting"
          >
            {{ submitting ? '保存中...' : '保存并入库' }}
          </button>
        </div>
      </template>
      <template v-else>
        <div class="header-left">
          <button class="btn-back" type="button" @click="goBack">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="15 18 9 12 15 6" />
            </svg>
            {{ t('stockInDetail.back') }}
          </button>
          <div class="stockin-caption-title-group">
            <div class="caption-avatar-lg">{{ stockInCaptionAvatarChar }}</div>
            <div>
              <div class="page-title-row">
                <div class="page-title-with-icons">
                  <h1 class="page-title" :class="{ 'page-title--muted': detailStatus === 3 }">
                    {{ t('stockInDetail.captionPrefix') }} {{ form.stockInCode || '—' }}
                  </h1>
                  <el-tooltip
                    v-if="isCustomsStockInDetail && detailArrivalNotifyTooltip"
                    :content="detailArrivalNotifyTooltip"
                    placement="top"
                    :hide-after="0"
                  >
                    <span class="customs-notify-tag">{{ t('stockInList.customsNotifyTag') }}</span>
                  </el-tooltip>
                </div>
              </div>
              <div class="title-meta title-meta--caption stockin-header-meta-row">
                <el-tag v-if="detailStatus !== null" effect="dark" :type="stockInStatusTagType" size="small">
                  {{ statusLabel(detailStatus) }}
                </el-tag>
                <StockBizTypeTag biz="in" :type="detailStockInType" />
              </div>
            </div>
          </div>
        </div>
      </template>
    </div>

    <div v-if="isCreateMode" class="form-layout">
      <div class="form-card">
        <h3 class="section-title">基础信息</h3>
        <el-form :model="form" label-width="90px" class="stockin-form">
          <el-form-item label="入库单号" required>
            <el-input v-model="form.stockInCode" placeholder="如：SIN202603180001" />
          </el-form-item>
          <el-form-item label="仓库ID" required>
            <el-input v-model="form.warehouseId" placeholder="目标仓库ID" />
          </el-form-item>
          <el-form-item v-if="maskPurchaseSensitiveFields" label="供应商">
            <span class="stockin-report-cell">—</span>
          </el-form-item>
          <el-form-item v-else label="供应商ID">
            <el-input v-model="form.vendorId" placeholder="供应商ID（可选）" />
          </el-form-item>
          <el-form-item label="到货通知号">
            <el-input v-model="form.purchaseOrderId" placeholder="到货通知/采购行号等（可选）" />
          </el-form-item>
          <el-form-item label="入库日期" required>
            <el-date-picker
              v-model="form.stockInDate"
              type="datetime"
              format="YYYY-MM-DD HH:mm"
              value-format="YYYY-MM-DDTHH:mm:ss"
              style="width: 100%"
            />
          </el-form-item>
          <el-form-item label="备注">
            <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="备注信息" />
          </el-form-item>
        </el-form>
      </div>

      <div class="form-card">
        <div class="section-header">
          <h3 class="section-title">入库明细</h3>
          <button type="button" class="btn-secondary btn-sm" @click="addRow">新增一行</button>
        </div>
        <div class="detail-items-table-wrap">
          <el-table :data="form.items" class="items-table quantum-table" style="width: 100%">
            <el-table-column type="index" width="50" align="center" />
            <el-table-column label="物料型号" min-width="168" show-overflow-tooltip>
              <template #default="{ row }">
                <el-input
                  v-model="row.materialCode"
                  placeholder="物料主数据 Id（UUID）或采购明细行 Id"
                />
              </template>
            </el-table-column>
            <el-table-column label="品牌" min-width="120" show-overflow-tooltip>
              <template #default="{ row }">
                <el-input v-model="row.materialBrand" placeholder="可选" />
              </template>
            </el-table-column>
            <el-table-column label="数量" width="110" align="right" header-align="right">
              <template #default="{ row }">
                <el-input-number v-model="row.quantity" :min="0" :step="1" />
              </template>
            </el-table-column>
            <el-table-column label="单位" width="90" align="center" header-align="center">
              <template #default="{ row }">
                <el-input v-model="row.unit" placeholder="PCS" />
              </template>
            </el-table-column>
            <el-table-column label="单价" width="120" align="right" header-align="right">
              <template #default="{ row }">
                <el-input-number
                  v-if="!maskPurchaseSensitiveFields"
                  v-model="row.unitPrice"
                  :min="0"
                  :precision="6"
                  :controls="false"
                />
                <span v-else class="stockin-report-cell stockin-report-cell--num">—</span>
              </template>
            </el-table-column>
            <el-table-column label="批次号" width="140" show-overflow-tooltip>
              <template #default="{ row }">
                <el-input v-model="row.batchNo" placeholder="批次号" />
              </template>
            </el-table-column>
            <el-table-column label="库位" width="140" show-overflow-tooltip>
              <template #default="{ row }">
                <el-input v-model="row.warehouseLocation" placeholder="库位编码" />
              </template>
            </el-table-column>
            <el-table-column
              label="操作"
              :width="stockInCreateOpColWidth"
              :min-width="stockInCreateOpColMinWidth"
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
                    :aria-label="stockInCreateOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
                    @click.stop="toggleStockInCreateOpCol"
                  >
                    {{ stockInCreateOpColExpanded ? '>' : '<' }}
                  </button>
                </div>
              </template>
              <template #default="{ $index }">
                <div @click.stop @dblclick.stop>
                  <div v-if="stockInCreateOpColExpanded" class="action-btns">
                    <button v-if="canWriteLogisticsData" type="button" class="action-btn action-btn--danger" @click.stop="removeRow($index)">删除</button>
                  </div>
                  <el-dropdown v-else trigger="click" placement="bottom-end">
                    <div class="op-more-dropdown-trigger">
                      <button type="button" class="op-more-trigger">...</button>
                    </div>
                    <template #dropdown>
                      <el-dropdown-menu>
                        <el-dropdown-item v-if="canWriteLogisticsData" @click.stop="removeRow($index)">
                          <span class="op-more-item op-more-item--danger">删除</span>
                        </el-dropdown-item>
                      </el-dropdown-menu>
                    </template>
                  </el-dropdown>
                </div>
              </template>
            </el-table-column>
          </el-table>
        </div>
        <div class="table-footer">
          <div class="total">
            合计数量：<span>{{ totalQuantityDisplay }}</span>
          </div>
        </div>
      </div>
    </div>

    <div v-else class="detail-content">
      <div class="info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('stockInDetail.basicInfo') }}</span>
          </div>
          <div class="section-header__meta">
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">{{ t('stockInDetail.createDate') }}</span>
              <span class="section-header-meta-item__value">{{ stockInBasicCreateDateText }}</span>
            </span>
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">{{ t('stockInDetail.createUser') }}</span>
              <span class="section-header-meta-item__value">{{ stockInBasicCreateUserText }}</span>
            </span>
          </div>
        </div>
        <div class="info-grid info-grid--inline-labels info-grid--basic">
          <div class="info-item">
            <span class="info-label">{{ t('stockInList.columns.stockInType') }}</span>
            <span class="info-value"><StockBizTypeTag biz="in" :type="detailStockInType" /></span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('stockInDetail.warehouseName') }}</span>
            <span class="info-value">{{ detailWarehouseNameText }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('stockInList.columns.stockInDate') }}</span>
            <span class="info-value info-value--time">{{ reportDateTimeText(form.stockInDate) }}</span>
          </div>
        </div>
        <div class="info-grid info-grid--inline-labels info-grid--basic">
          <div class="info-item">
            <span class="info-label">{{ isCustomsStockInDetail ? t('stockInDetail.fields.originalVendor') : t('stockInList.columns.vendor') }}</span>
            <span class="info-value">{{ reportCellText(formatRowVendorName({ vendorName: displayVendorName, vendorEnglishName: displayVendorEnglishName })) }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{
              isCustomsStockInDetail
                ? t('stockInDetail.fields.customsArrivalNotify')
                : t('stockInDetail.fields.purchaseArrivalNotify')
            }}</span>
            <span class="info-value">{{ reportCellText(form.purchaseOrderId) }}</span>
          </div>
          <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
        </div>
        <div class="info-grid info-grid--inline-labels">
          <div class="info-item info-item--span-all">
            <span class="info-label">{{ t('stockInList.columns.remark') }}</span>
            <span class="info-value">{{ reportCellText(form.remark) }}</span>
          </div>
        </div>
      </div>

      <div v-if="isCustomsStockInDetail" class="info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('stockInDetail.customsSection') }}</span>
          </div>
        </div>
        <div class="info-section__body">
        <div v-if="customsContext?.qcCode" class="stockin-report-row stockin-report-row--inline">
          <span class="customs-meta-label">{{ t('stockInDetail.customsQc') }}</span>
          <router-link
            v-if="customsContext.qcId"
            :to="{ name: 'QcCreate', query: { qcId: customsContext.qcId } }"
            class="cell-link"
          >
            {{ customsContext.qcCode }}
          </router-link>
          <span v-else>{{ customsContext.qcCode }}</span>
        </div>
        <div v-if="customsDeclarationSummaries.length" class="customs-summary-wrap">
          <h4 class="sub-title">{{ t('stockInDetail.declarationSummary') }}</h4>
          <el-table :data="customsDeclarationSummaries" size="small" border class="customs-summary-table">
            <el-table-column :label="t('stockInDetail.customsDeclaration')" min-width="128">
              <template #default="{ row }">
                <router-link
                  v-if="row.declarationId"
                  :to="{ name: 'CustomsDeclarationDetail', params: { id: row.declarationId } }"
                  class="cell-link"
                >
                  {{ row.declarationCode }}
                </router-link>
              </template>
            </el-table-column>
            <el-table-column :label="t('stockInDetail.customsBroker')" min-width="140">
              <template #default="{ row }">{{ row.customsBrokerName || '—' }}</template>
            </el-table-column>
            <el-table-column :label="t('customsPages.declarations.colDeclareDate')" width="110">
              <template #default="{ row }">{{ customsDateText(row.declareDate) }}</template>
            </el-table-column>
            <el-table-column :label="t('stockInDetail.exchangeRate')" width="88" align="right">
              <template #default="{ row }">{{ customsFeeMoneyText(row.exchangeRate) }}</template>
            </el-table-column>
            <el-table-column :label="t('stockInDetail.declarationTotalTax')" width="110" align="right">
              <template #default="{ row }">{{ customsFeeMoneyText(row.declarationTotalTaxAmount) }}</template>
            </el-table-column>
            <el-table-column :label="t('stockInDetail.warehouseRoute')" min-width="140">
              <template #default="{ row }">{{ customsWarehouseRoute(row) }}</template>
            </el-table-column>
          </el-table>
        </div>
        <h4 v-if="customsContextItems.length" class="sub-title">{{ t('stockInDetail.customsTraceSection') }}</h4>
        <el-table
          v-if="customsContextItems.length"
          :data="customsContextItems"
          class="customs-context-table quantum-table"
          size="small"
          border
          style="width: 100%; margin-top: 8px"
        >
          <el-table-column :label="t('stockInDetail.customsDeclaration')" min-width="128">
            <template #default="{ row }">
              <router-link
                v-if="row.declarationId"
                :to="{ name: 'CustomsDeclarationDetail', params: { id: row.declarationId } }"
                class="cell-link"
              >
                {{ row.declarationCode }}
              </router-link>
              <span v-else>{{ row.declarationCode || '—' }}</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.customsLineNo')" width="88" align="center">
            <template #default="{ row }">{{ row.lineNo ?? '—' }}</template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.fields.customsArrivalNotify')" min-width="120">
            <template #default="{ row }">{{ row.arrivalNotifyCode || '—' }}</template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.customsBroker')" min-width="140" show-overflow-tooltip>
            <template #default="{ row }">
              {{ row.customsBrokerName || row.customsBrokerCode || '—' }}
            </template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.customsPacking')" min-width="120">
            <template #default="{ row }">
              <router-link
                v-if="row.packingId"
                :to="{ name: 'PackingDetail', params: { id: row.packingId } }"
                class="cell-link"
              >
                {{ row.packingCode || row.packingId }}
              </router-link>
              <span v-else>—</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.warehouseRoute')" min-width="140">
            <template #default="{ row }">{{ customsWarehouseRoute(row) }}</template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.salesStockOutNotify')" min-width="128">
            <template #default="{ row }">
              <router-link
                v-if="row.salesStockOutNotifyId"
                :to="{ name: 'StockOutNotifyDetail', params: { id: row.salesStockOutNotifyId } }"
                class="cell-link"
              >
                {{ row.salesStockOutNotifyCode || row.salesStockOutNotifyId }}
              </router-link>
              <span v-else>—</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.customsStockOutNotify')" min-width="128">
            <template #default="{ row }">
              <router-link
                v-if="row.customsStockOutNotifyId"
                :to="{ name: 'StockOutNotifyDetail', params: { id: row.customsStockOutNotifyId } }"
                class="cell-link"
              >
                {{ row.customsStockOutNotifyCode || row.customsStockOutNotifyId }}
              </router-link>
              <span v-else>—</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.customer')" min-width="120" show-overflow-tooltip>
            <template #default="{ row }">{{
              maskSaleSensitiveFields ? '—' : row.customerName || '—'
            }}</template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.sellOrderItemCode')" min-width="120" show-overflow-tooltip>
            <template #default="{ row }">{{
              maskSaleSensitiveFields ? '—' : row.sellOrderItemCode || '—'
            }}</template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.clearanceStatus')" width="96">
            <template #default="{ row }">{{ customsClearanceLabel(row.customsClearanceStatus) }}</template>
          </el-table-column>
        </el-table>
        <h4 v-if="customsContextItems.length" class="sub-title">{{ t('stockInDetail.customsFeeSection') }}</h4>
        <el-table
          v-if="customsContextItems.length"
          :data="customsContextItems"
          class="customs-fee-table quantum-table"
          size="small"
          border
          style="width: 100%; margin-top: 8px"
        >
          <el-table-column :label="t('stockInDetail.customsDeclaration')" min-width="120">
            <template #default="{ row }">{{ row.declarationCode }}-{{ row.lineNo }}</template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.purchasePn')" min-width="120" show-overflow-tooltip>
            <template #default="{ row }">{{ row.purchasePn || '—' }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colBrand')" width="96" show-overflow-tooltip>
            <template #default="{ row }">{{ row.purchaseBrand || '—' }}</template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.hsCode')" width="100" show-overflow-tooltip>
            <template #default="{ row }">{{ row.hsCode || '—' }}</template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.declareQty')" width="88" align="right">
            <template #default="{ row }">{{ reportQtyText(row.declareQty) }}</template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.vendor')" min-width="120" show-overflow-tooltip>
            <template #default="{ row }">{{ formatRowVendorName(row) }}</template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.declareUnitPrice')" width="100" align="right">
            <template #default="{ row }">{{ customsFeeMoneyText(row.declareUnitPrice) }}</template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.originalPrice')" width="100" align="right">
            <template #default="{ row }">{{ customsPriceText(row.originalPurchasePrice) }}</template>
          </el-table-column>
          <el-table-column :label="t('stockInDetail.taxIncludedPrice')" width="100" align="right">
            <template #default="{ row }">{{ customsPriceText(row.taxIncludedUnitPrice) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colDuty')" width="88" align="right">
            <template #default="{ row }">{{ customsFeeMoneyText(row.dutyAmount) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colVat')" width="88" align="right">
            <template #default="{ row }">{{ customsFeeMoneyText(row.vatAmount) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colGoods')" width="96" align="right">
            <template #default="{ row }">{{ customsFeeMoneyText(row.customsPaymentGoods) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colAgency')" width="96" align="right">
            <template #default="{ row }">{{ customsFeeMoneyText(row.customsAgencyFee) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colOther')" width="80" align="right">
            <template #default="{ row }">{{ customsFeeMoneyText(row.otherFee) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colInspection')" width="88" align="right">
            <template #default="{ row }">{{ customsFeeMoneyText(row.inspectionFee) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colTotalTax')" width="100" align="right">
            <template #default="{ row }">{{ customsFeeMoneyText(row.totalValueTax) }}</template>
          </el-table-column>
        </el-table>
        <div v-if="customsTimelineGroups.length" class="customs-timeline-wrap">
          <h4 class="sub-title">{{ t('stockInDetail.timelineSection') }}</h4>
          <div
            v-for="group in customsTimelineGroups"
            :key="group.key"
            class="customs-timeline-group"
          >
            <div class="customs-timeline-group-title">{{ group.title }}</div>
            <el-timeline class="customs-timeline">
              <el-timeline-item
                v-for="step in group.steps"
                :key="step.stepCode"
                :type="step.state === 'done' ? 'success' : 'info'"
                :hollow="step.state !== 'done'"
                :timestamp="customsTimelineTimeText(step.occurredAt)"
                placement="top"
              >
                <div class="customs-timeline-step">
                  <span class="customs-timeline-step-label">{{ customsTimelineStepLabel(step.stepCode) }}</span>
                  <router-link
                    v-if="customsTimelineRoute(step)"
                    :to="customsTimelineRoute(step)!"
                    class="cell-link customs-timeline-doc"
                  >
                    {{ customsTimelineDocText(step) }}
                  </router-link>
                  <span v-else-if="customsTimelineDocText(step)" class="customs-timeline-doc">{{
                    customsTimelineDocText(step)
                  }}</span>
                  <span v-if="customsTimelineStatusText(step)" class="customs-timeline-status">{{
                    customsTimelineStatusText(step)
                  }}</span>
                  <span
                    v-if="step.state === 'pending'"
                    class="customs-timeline-state customs-timeline-state--pending"
                  >{{ t('stockInDetail.timelineStatePending') }}</span>
                </div>
              </el-timeline-item>
            </el-timeline>
          </div>
        </div>
        <div v-if="!customsContextItems.length" class="stockin-report-empty">{{ t('stockInDetail.noCustomsItems') }}</div>
        </div>
      </div>

      <div class="tabs-section">
        <div class="tabs-nav">
          <button
            type="button"
            class="tab-btn"
            :class="{ 'tab-btn--active': detailActiveTab === 'items' }"
            @click="detailActiveTab = 'items'"
          >
            {{ t('stockInDetail.tabs.items') }}
            <span v-if="form.items?.length" class="tab-count">{{ form.items.length }}</span>
          </button>
          <button
            type="button"
            class="tab-btn"
            :class="{ 'tab-btn--active': detailActiveTab === 'stockItems' }"
            @click="detailActiveTab = 'stockItems'"
          >
            {{ t('stockInDetail.tabs.stockItems') }}
            <span v-if="stockItemRows.length" class="tab-count">{{ stockItemRows.length }}</span>
          </button>
        </div>
        <div class="tabs-body">
          <div v-show="detailActiveTab === 'items'" class="detail-items-table-wrap">
          <CrmDataTable :data="form.items" :border="false" class="items-table detail-panel-list-table" size="small" stripe>
            <el-table-column type="index" width="50" align="center" />
            <el-table-column label="入库明细编号" min-width="148" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.stockInItemCode) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="入库日期" width="148" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportDateTimeText(row.stockInDate) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="到货通知单号" min-width="140" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.sourceCode) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="采购订单明细编号" min-width="160" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.purchaseOrderItemCode) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="供应商名称" min-width="160" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(formatRowVendorName(row)) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="物料型号" min-width="168" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.materialName) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="品牌" min-width="120" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.materialBrand) }}</span>
              </template>
            </el-table-column>
            <el-table-column
              label="采购单价"
              min-width="140"
              align="right"
              header-align="right"
              class-name="stock-item-unit-price-col"
            >
              <template #default="{ row }">
                <span v-if="maskPurchaseSensitiveFields" class="stockin-report-cell">—</span>
                <template v-else-if="unitPriceDockHasValue(row.unitPrice)">
                  <div class="dock-tier-price-line">
                    <template v-for="amt in [splitUnitPriceDockParts(row.unitPrice)]" :key="'up-' + row.itemId">
                      <span class="dock-tier-amt">
                        <span class="dock-tier-amt-int">{{ amt.intPart }}</span
                        ><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
                      </span>
                    </template>
                    <span class="dock-tier-ccy-gap">&nbsp;</span>
                    <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">{{
                      listAmountCurrencyIso(row.currency)
                    }}</span>
                  </div>
                </template>
                <span v-else class="stockin-report-cell">—</span>
              </template>
            </el-table-column>
            <el-table-column label="数量" width="110" align="right" header-align="right">
              <template #default="{ row }">
                <span class="stockin-report-cell stockin-report-cell--num">{{ reportQtyText(row.quantity) }}</span>
              </template>
            </el-table-column>
            <el-table-column
              label="采购总额"
              min-width="140"
              align="right"
              header-align="right"
              class-name="stock-item-unit-price-col"
            >
              <template #default="{ row }">
                <span v-if="maskPurchaseSensitiveFields" class="stockin-report-cell">—</span>
                <template v-else-if="listTotalAmountHasValue(row.amount)">
                  <div class="dock-tier-price-line">
                    <template v-for="amt in [splitListMoneyParts(Number(row.amount))]" :key="'amt-' + row.itemId">
                      <span class="dock-tier-amt">
                        <span class="dock-tier-amt-int">{{ amt.intPart }}</span
                        ><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
                      </span>
                    </template>
                    <span class="dock-tier-ccy-gap">&nbsp;</span>
                    <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">{{
                      listAmountCurrencyIso(row.currency)
                    }}</span>
                  </div>
                </template>
                <span v-else class="stockin-report-cell">—</span>
              </template>
            </el-table-column>
            <el-table-column label="地域类型" width="100" align="center" header-align="center">
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ regionTypeLabel(row.regionType) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="仓库" width="100" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.warehouseCode) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="入库类型" width="110" show-overflow-tooltip>
              <template #default="{ row }">
                <StockBizTypeTag biz="in" :type="row.stockInType ?? detailStockInType" />
              </template>
            </el-table-column>
            <el-table-column label="批次号" width="140" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.batchNo) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="库位" width="140" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.warehouseLocation) }}</span>
              </template>
            </el-table-column>
            <el-table-column
              v-if="detailStatus === 2"
              label="操作"
              :width="stockInReportOpColWidth"
              :min-width="stockInReportOpColMinWidth"
              fixed="right"
              align="center"
              header-align="center"
              class-name="op-col"
              label-class-name="op-col"
            >
              <template #header>
                <div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="stockInReportOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="toggleStockInReportOpCol"
            >
              {{ stockInReportOpColExpanded ? '>' : '<' }}
            </button>
          </div>
              </template>
              <template #default="{ row }">
                <div @click.stop @dblclick.stop>
                  <div v-if="stockInReportOpColExpanded" class="action-btns">
                    <button type="button" class="action-btn action-btn--primary" @click.stop="openBatchImport(row)">入库批次</button>
                  </div>
                  <el-dropdown v-else trigger="click" placement="bottom-end">
                    <div class="op-more-dropdown-trigger">
                      <button type="button" class="op-more-trigger">...</button>
                    </div>
                    <template #dropdown>
                      <el-dropdown-menu>
                        <el-dropdown-item @click.stop="openBatchImport(row)">
                          <span class="op-more-item op-more-item--primary">入库批次</span>
                        </el-dropdown-item>
                      </el-dropdown-menu>
                    </template>
                  </el-dropdown>
                </div>
              </template>
            </el-table-column>
          </CrmDataTable>
          </div>
          <div v-show="detailActiveTab === 'items'" class="table-footer">
            <div class="total">
              合计数量：<span>{{ totalQuantityDisplay }}</span>
            </div>
          </div>

          <div v-show="detailActiveTab === 'stockItems'" class="detail-items-table-wrap stockin-stock-items-table-wrap">
          <CrmDataTable :data="stockItemRows" :border="false" class="items-table detail-panel-list-table" size="small" stripe>
            <el-table-column type="index" width="50" align="center" fixed="left" />
            <el-table-column label="库存明细编号" min-width="150" show-overflow-tooltip fixed="left">
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.stockItemCode) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="仓库名称" min-width="120" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ stockItemWarehouseNameText(row) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="库存类型" min-width="88" align="center" header-align="center">
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ stockItemStockTypeLabel(row) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="入库类型" min-width="88" show-overflow-tooltip>
              <template #default="{ row }">
                <StockBizTypeTag biz="in" :type="row.stockInType ?? detailStockInType" />
              </template>
            </el-table-column>
            <el-table-column label="入库明细编号" min-width="150" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.stockInItemCode) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="入库日期" min-width="130" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportDateTimeText(row.stockInDate) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="物料型号" min-width="168" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.purchasePn) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="品牌" min-width="120" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.purchaseBrand) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="客户物料型号" min-width="140" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.customerPn) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="客户品牌" min-width="120" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.customerBrand) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="入库数量" min-width="96" align="right" header-align="right">
              <template #default="{ row }">
                <span class="stockin-report-cell stockin-report-cell--num">{{ reportQtyText(row.qtyInbound) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="已出库数量" min-width="108" align="right" header-align="right">
              <template #default="{ row }">
                <span class="stockin-report-cell stockin-report-cell--num">{{ reportQtyText(row.qtyStockOut) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="在库数量" min-width="96" align="right" header-align="right">
              <template #default="{ row }">
                <span class="stockin-report-cell stockin-report-cell--num">{{ reportQtyText(row.qtyRepertory) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="供应商ID" min-width="120" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.vendorId) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="供应商名称" min-width="140" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(formatRowVendorName(row)) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="采购员名称" min-width="100" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.purchaserName) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="采购订单明细编号" min-width="168" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.purchaseOrderItemCode) }}</span>
              </template>
            </el-table-column>
            <el-table-column
              label="采购单价+币别"
              min-width="132"
              align="right"
              header-align="right"
              class-name="stock-item-unit-price-col"
            >
              <template #default="{ row }">
                <span v-if="maskPurchaseSensitiveFields" class="stockin-report-cell">—</span>
                <template v-else-if="unitPriceDockHasValue(row.purchasePrice)">
                  <div class="dock-tier-price-line">
                    <template
                      v-for="amt in [splitUnitPriceDockParts(row.purchasePrice)]"
                      :key="'sip-' + row.stockItemId"
                    >
                      <span class="dock-tier-amt">
                        <span class="dock-tier-amt-int">{{ amt.intPart }}</span
                        ><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
                      </span>
                    </template>
                    <span class="dock-tier-ccy-gap">&nbsp;</span>
                    <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.purchaseCurrency)]">{{
                      listAmountCurrencyIso(row.purchaseCurrency)
                    }}</span>
                  </div>
                </template>
                <span v-else class="stockin-report-cell">—</span>
              </template>
            </el-table-column>
            <el-table-column label="客户ID" min-width="120" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.customerId) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="客户名称" min-width="140" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{
                  maskSaleSensitiveFields ? '—' : reportCellText(row.customerName)
                }}</span>
              </template>
            </el-table-column>
            <el-table-column label="业务员名称" min-width="108" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{
                  maskSaleSensitiveFields ? '—' : reportCellText(row.salespersonName)
                }}</span>
              </template>
            </el-table-column>
            <el-table-column label="销售订单明细编号" min-width="168" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.sellOrderItemCode) }}</span>
              </template>
            </el-table-column>
            <el-table-column
              label="销售单价+币别"
              min-width="132"
              align="right"
              header-align="right"
              class-name="stock-item-unit-price-col"
            >
              <template #default="{ row }">
                <span v-if="maskSaleSensitiveFields" class="stockin-report-cell">—</span>
                <template v-else-if="row.salesPrice != null && unitPriceDockHasValue(row.salesPrice)">
                  <div class="dock-tier-price-line">
                    <template
                      v-for="amt in [splitUnitPriceDockParts(row.salesPrice)]"
                      :key="'sis-' + row.stockItemId"
                    >
                      <span class="dock-tier-amt">
                        <span class="dock-tier-amt-int">{{ amt.intPart }}</span
                        ><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
                      </span>
                    </template>
                    <span class="dock-tier-ccy-gap">&nbsp;</span>
                    <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.salesCurrency)]">{{
                      listAmountCurrencyIso(row.salesCurrency)
                    }}</span>
                  </div>
                </template>
                <span v-else class="stockin-report-cell">—</span>
              </template>
            </el-table-column>
            <el-table-column label="批次号" min-width="88" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.batchNo) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="生产日期" min-width="96" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportProductionDateText(row.productionDate) }}</span>
              </template>
            </el-table-column>
          </CrmDataTable>
          <div v-if="!stockItemRows.length" class="stockin-report-empty">暂无对应库存明细</div>
          </div>
        </div>
      </div>

      <StockInBatchPanel
        v-if="detailBatchPanelReady && stockInRouteId"
        ref="batchPanelRef"
        :stock-in-id="stockInRouteId"
        :stock-in-code="form.stockInCode"
        :items="form.items ?? []"
        :can-write="canWriteLogisticsData"
      />
    </div>

    <StockInBatchImportDialog
      v-model="batchImportVisible"
      :stock-in-id="stockInHeaderId"
      :stock-in-item-id="batchImportItemId"
      :stock-in-item-code="batchImportItemCode"
      @success="onRowBatchImportSuccess"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, defineAsyncComponent, reactive, ref, watch } from 'vue'
import { useRoute, useRouter, type RouteLocationRaw } from 'vue-router'
import { CUSTOMS_PENDLIST_STATUS } from '@/api/customs'
import { PackingStatusCode } from '@/api/packing'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { ElMessage } from 'element-plus'
import {
  stockInApi,
  type CreateStockInRequest,
  type StockInCustomsContextDto,
  type StockInCustomsContextItemDto,
  type StockInCustomsTimelineStepDto,
  type StockInDto,
  type StockInItemDto
} from '@/api/stockIn'
import { inventoryCenterApi, type StockItemListRow } from '@/api/inventoryCenter'
import StockInBatchImportDialog from '@/components/Inventory/StockInBatchImportDialog.vue'

const StockInBatchPanel = defineAsyncComponent(
  () => import('@/components/Inventory/StockInBatchPanel.vue')
)
import CrmDataTable from '@/components/CrmDataTable.vue'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { formatVendorNameReadonly } from '@/utils/vendorDisplayName'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import { normalizeRegionType, REGION_TYPE_DOMESTIC, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { StockInTypeCode } from '@/constants/stockInType'
import {
  listAmountCurrencyDockClass,
  listAmountCurrencyIso,
  listTotalAmountHasValue,
  splitListMoneyParts,
  splitUnitPriceDockParts,
  unitPriceDockHasValue
} from '@/utils/moneyFormat'
import { useI18n } from 'vue-i18n'
import { formatDisplayDate } from '@/utils/displayDateTime'

const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { canWriteLogisticsData } = useDepartmentDataReadOnly()
const { t } = useI18n()

const router = useRouter()
const route = useRoute()
const submitting = ref(false)
const detailLoading = ref(false)
const detailStatus = ref<number | null>(null)
/** 详情页展示：仓库编号（非 UUID） */
const displayWarehouseCode = ref('')
/** 详情页展示：仓库名称 */
const displayWarehouseName = ref('')
/** 详情页展示：供应商名称 */
const displayVendorName = ref('')
const displayVendorEnglishName = ref('')
/** 详情页：单头入库类型 / 地域（库存明细行无值时回退） */
const detailStockInType = ref(0)
const detailSourceDisplayNo = ref('')
const detailRegionType = ref(REGION_TYPE_DOMESTIC)
const detailCreateTime = ref('')
const detailCreateUserName = ref('')
const detailActiveTab = ref<'items' | 'stockItems'>('items')
const stockItemRows = ref<StockItemListRow[]>([])
const customsContext = ref<StockInCustomsContextDto | null>(null)

const batchImportVisible = ref(false)
const batchImportItemId = ref('')
const batchImportItemCode = ref('')
const batchPanelRef = ref<{ refresh?: () => void } | null>(null)
/** 详情主数据加载完成后再挂载批次面板，避免与首屏加载争抢且便于隔离面板异常 */
const detailBatchPanelReady = ref(false)

const isCreateMode = computed(() => route.name === 'StockInCreate')

const stockInCaptionAvatarChar = computed(() => {
  const c = form.stockInCode?.trim()
  return c ? c[0]! : '入'
})

const stockInBasicCreateDateText = computed(() => {
  if (!detailCreateTime.value) return '—'
  const s = formatDisplayDate(detailCreateTime.value)
  return s === '--' ? '—' : s
})

const stockInBasicCreateUserText = computed(() => detailCreateUserName.value.trim() || '—')

const detailWarehouseNameText = computed(() => {
  const name = displayWarehouseName.value.trim()
  if (name) return name
  const code = displayWarehouseCode.value.trim()
  return code || '—'
})

const stockInStatusTagType = computed((): '' | 'success' | 'warning' | 'info' | 'danger' => {
  const s = detailStatus.value
  if (s === 0) return 'info'
  if (s === 1) return 'warning'
  if (s === 2) return 'success'
  if (s === 3) return 'danger'
  return 'info'
})

/** 《列表操作列规范》：新建明细行 / 详情行内操作 */
const stockInReportOpColExpanded = ref(false)
const stockInCreateOpColExpanded = ref(false)
const STOCK_IN_OP_COL_COLLAPSED = 43
const STOCK_IN_OP_COL_EXPANDED = 173
const STOCK_IN_OP_COL_EXPANDED_MIN = 160
const stockInReportOpColWidth = computed(() =>
  stockInReportOpColExpanded.value ? STOCK_IN_OP_COL_EXPANDED : STOCK_IN_OP_COL_COLLAPSED
)
const stockInReportOpColMinWidth = computed(() =>
  stockInReportOpColExpanded.value ? STOCK_IN_OP_COL_EXPANDED_MIN : STOCK_IN_OP_COL_COLLAPSED
)
const stockInCreateOpColWidth = computed(() =>
  stockInCreateOpColExpanded.value ? STOCK_IN_OP_COL_EXPANDED : STOCK_IN_OP_COL_COLLAPSED
)
const stockInCreateOpColMinWidth = computed(() =>
  stockInCreateOpColExpanded.value ? STOCK_IN_OP_COL_EXPANDED_MIN : STOCK_IN_OP_COL_COLLAPSED
)
function toggleStockInReportOpCol() {
  stockInReportOpColExpanded.value = !stockInReportOpColExpanded.value
}
function toggleStockInCreateOpCol() {
  stockInCreateOpColExpanded.value = !stockInCreateOpColExpanded.value
}

const stockInRouteId = computed(() => {
  if (route.name !== 'StockInDetail') return ''
  const p = route.params.id
  if (typeof p === 'string') return p.trim()
  if (Array.isArray(p) && p[0]) return String(p[0]).trim()
  return ''
})

const stockInHeaderId = stockInRouteId

const form = reactive<CreateStockInRequest>({
  stockInCode: '',
  purchaseOrderId: '',
  vendorId: '',
  warehouseId: '',
  operatorId: '',
  stockInDate: new Date().toISOString(),
  totalQuantity: 0,
  remark: '',
  items: []
})

function resetCreateForm() {
  detailStatus.value = null
  displayWarehouseCode.value = ''
  displayWarehouseName.value = ''
  displayVendorName.value = ''
  displayVendorEnglishName.value = ''
  detailStockInType.value = 0
  detailSourceDisplayNo.value = ''
  detailRegionType.value = REGION_TYPE_DOMESTIC
  detailCreateTime.value = ''
  detailCreateUserName.value = ''
  detailActiveTab.value = 'items'
  form.stockInCode = ''
  form.purchaseOrderId = ''
  form.vendorId = ''
  form.warehouseId = ''
  form.operatorId = ''
  form.stockInDate = new Date().toISOString()
  form.totalQuantity = 0
  form.remark = ''
  form.items = []
  stockItemRows.value = []
  customsContext.value = null
}

function normalizeDateForPicker(iso: string | undefined | null): string {
  if (!iso || typeof iso !== 'string') return new Date().toISOString().slice(0, 19)
  const t = iso.includes('T') ? iso.slice(0, 19) : iso.replace(' ', 'T').slice(0, 19)
  return t || new Date().toISOString().slice(0, 19)
}

function pickStr(obj: Record<string, unknown>, ...keys: string[]): string {
  for (const k of keys) {
    const v = obj[k]
    if (v != null && String(v).trim() !== '') return String(v).trim()
  }
  return ''
}

function extractDetailItemRows(d: StockInDto): Record<string, unknown>[] {
  const r = d as unknown as Record<string, unknown>
  const raw = r.items ?? r.Items
  return Array.isArray(raw) ? (raw as Record<string, unknown>[]) : []
}

function applyDetailToForm(d: StockInDto) {
  const r = d as unknown as Record<string, unknown>
  detailStatus.value = d.status ?? null
  detailCreateTime.value = d.createTime ?? ''
  detailCreateUserName.value = pickStr(r, 'createUserName', 'CreateUserName')
  form.stockInCode = d.stockInCode ?? ''
  form.warehouseId = d.warehouseId ?? ''
  form.vendorId = d.vendorId ?? ''
  const wh = pickStr(r, 'detailWarehouseCode', 'DetailWarehouseCode')
  displayWarehouseCode.value = wh || (form.warehouseId ? String(form.warehouseId) : '—')
  displayWarehouseName.value = pickStr(r, 'detailWarehouseName', 'DetailWarehouseName')
  const vn = pickStr(r, 'detailVendorName', 'DetailVendorName')
  displayVendorName.value = vn || (form.vendorId ? String(form.vendorId) : '—')
  displayVendorEnglishName.value = pickStr(r, 'vendorEnglishName', 'VendorEnglishName', 'detailVendorEnglishName', 'DetailVendorEnglishName')
  const headerStockInTypeEarly = Number(d.stockInType) || 0
  if (headerStockInTypeEarly === StockInTypeCode.Customs) {
    form.purchaseOrderId = (d.sourceCode ?? '').trim()
  } else {
    const parts = [d.sourceCode, d.purchaseOrderItemCode].filter(x => x != null && String(x).trim() !== '')
    form.purchaseOrderId = parts.length ? parts.map(x => String(x).trim()).join(' / ') : ''
  }
  form.stockInDate = normalizeDateForPicker(d.stockInDate)
  form.remark = d.remark ?? ''
  form.totalQuantity = d.totalQuantity ?? 0
  form.operatorId = ''

  const headerStockInDate = normalizeDateForPicker(d.stockInDate)
  const headerSourceCode = (d.sourceCode ?? '').trim()
  const headerVendorName = displayVendorName.value
  const headerVendorEnglishName = displayVendorEnglishName.value
  const headerWarehouseCode = displayWarehouseCode.value
  const headerRegionType = normalizeRegionType(d.regionType)
  const headerStockInType = Number(d.stockInType) || 0
  detailStockInType.value = headerStockInType
  detailSourceDisplayNo.value = headerSourceCode
  detailRegionType.value = headerRegionType

  const rawCustoms = (r.customsContext ?? r.CustomsContext) as StockInCustomsContextDto | undefined
  customsContext.value =
    rawCustoms && Array.isArray(rawCustoms.items) && rawCustoms.items.length > 0
      ? rawCustoms
      : rawCustoms?.qcId || rawCustoms?.qcCode
        ? rawCustoms
        : null

  const rawItems = extractDetailItemRows(d)
  form.items = rawItems.map((it, i): StockInItemDto => {
    const code =
      pickStr(it, 'detailMaterialCode', 'DetailMaterialCode') ||
      pickStr(it, 'materialId', 'MaterialId')
    const model =
      pickStr(it, 'detailMaterialModel', 'DetailMaterialModel') ||
      pickStr(it, 'purchasePn', 'PurchasePn') ||
      pickStr(it, 'detailMaterialName', 'DetailMaterialName')
    const brand =
      pickStr(it, 'detailMaterialBrand', 'DetailMaterialBrand') ||
      pickStr(it, 'purchaseBrand', 'PurchaseBrand')
    const unit = pickStr(it, 'detailUnit', 'DetailUnit') || 'PCS'
    const qty = Number(it.quantity ?? it.Quantity) || 0
    const price = Number(it.price ?? it.Price) || 0
    const amount = Number(it.amount ?? it.Amount)
    const currencyRaw = it.detailCurrency ?? it.DetailCurrency ?? it.currency ?? it.Currency
    const currency = currencyRaw != null && currencyRaw !== '' ? Number(currencyRaw) : undefined
    const lineStockInDate = pickStr(it, 'detailStockInDate', 'DetailStockInDate')
    const regionRaw = it.detailRegionType ?? it.DetailRegionType ?? d.regionType
    const stockInTypeRaw = it.detailStockInType ?? it.DetailStockInType ?? d.stockInType
    return {
      lineNo: i + 1,
      itemId: pickStr(it, 'id', 'Id', 'itemId', 'ItemId'),
      stockInItemCode: pickStr(it, 'stockInItemCode', 'StockInItemCode'),
      stockInDate: lineStockInDate ? normalizeDateForPicker(lineStockInDate) : headerStockInDate,
      sourceCode: pickStr(it, 'detailSourceCode', 'DetailSourceCode') || headerSourceCode,
      purchaseOrderItemCode: pickStr(it, 'detailPurchaseOrderItemCode', 'DetailPurchaseOrderItemCode'),
      vendorName: pickStr(it, 'detailVendorName', 'DetailVendorName') || headerVendorName,
      vendorEnglishName: pickStr(it, 'vendorEnglishName', 'VendorEnglishName', 'detailVendorEnglishName', 'DetailVendorEnglishName') || headerVendorEnglishName,
      materialCode: code,
      materialName: model,
      materialBrand: brand,
      specification: '',
      quantity: qty,
      unit,
      unitPrice: price,
      amount: Number.isFinite(amount) ? amount : undefined,
      currency: Number.isFinite(currency) ? currency : undefined,
      regionType: normalizeRegionType(regionRaw),
      warehouseCode: pickStr(it, 'detailWarehouseCode', 'DetailWarehouseCode') || headerWarehouseCode,
      stockInType: Number(stockInTypeRaw) || headerStockInType,
      batchNo: pickStr(it, 'batchNo', 'BatchNo'),
      warehouseLocation: pickStr(it, 'locationId', 'LocationId')
    }
  })
}

async function loadStockInDetail(id: string) {
  const rid = (id ?? '').trim()
  if (!rid) return

  detailLoading.value = true
  detailBatchPanelReady.value = false
  try {
    const data = await stockInApi.getById(rid)
    if (!data) {
      ElMessage.error('入库单不存在或无权查看')
      router.replace({ name: 'StockInList' })
      return
    }
    applyDetailToForm(data)
    const stockInCode = (data.stockInCode ?? '').trim()
    if (stockInCode) {
      try {
        const res = await inventoryCenterApi.searchStockItems({ stockInCode, page: 1, pageSize: 2000 })
        stockItemRows.value = res.items.filter((r) => String(r.stockInId || '').trim() === rid)
      } catch (stockErr) {
        console.error(stockErr)
        stockItemRows.value = []
        ElMessage.warning('库存明细加载失败，其余信息已正常显示')
      }
    } else {
      stockItemRows.value = []
    }
    detailBatchPanelReady.value = detailStatus.value === 2
  } catch (e) {
    console.error(e)
    ElMessage.error('加载入库单失败')
    router.replace({ name: 'StockInList' })
  } finally {
    detailLoading.value = false
  }
}

watch(
  () => ({ name: route.name, id: stockInRouteId.value }),
  async ({ name, id }) => {
    if (name === 'StockInCreate') {
      detailBatchPanelReady.value = false
      resetCreateForm()
      return
    }
    if (name === 'StockInDetail' && id) {
      await loadStockInDetail(id)
    }
  },
  { immediate: true }
)

const statusLabel = (s: number) => {
  switch (s) {
    case 0:
      return t('stockInList.status.draft')
    case 1:
      return t('stockInList.status.pending')
    case 2:
      return t('stockInList.status.done')
    case 3:
      return t('stockInList.status.cancelled')
    default:
      return String(s)
  }
}

const addRow = () => {
  const lineNo = (form.items?.length ?? 0) + 1
  const item: StockInItemDto = {
    lineNo,
    materialCode: '',
    materialName: '',
    materialBrand: '',
    specification: '',
    quantity: 0,
    unit: 'PCS',
    unitPrice: 0,
    batchNo: '',
    warehouseLocation: ''
  }
  form.items = [...(form.items || []), item]
}

const removeRow = (index: number) => {
  if (!form.items) return
  const items = [...form.items]
  items.splice(index, 1)
  form.items = items.map((x, i) => ({ ...x, lineNo: i + 1 }))
}

const totalQuantity = computed(() => (form.items || []).reduce((sum, x) => sum + (x.quantity || 0), 0))
/** 与业务列表数量展示一致（千分位） */
const totalQuantityDisplay = computed(() => totalQuantity.value.toLocaleString('zh-CN'))

function formatRowVendorName(row: { vendorName?: string | null; vendorEnglishName?: string | null }) {
  return formatVendorNameReadonly(row.vendorName, row.vendorEnglishName, { masked: maskPurchaseSensitiveFields.value })
}

/** 详情只读报表：空值统一为 — */
function reportCellText(v: unknown): string {
  if (v === null || v === undefined) return '—'
  const s = String(v).trim()
  return s ? s : '—'
}

function reportDateTimeText(iso: string | undefined | null): string {
  if (!iso || typeof iso !== 'string') return '—'
  const t = iso.includes('T') ? iso.slice(0, 16).replace('T', ' ') : iso.trim().slice(0, 16)
  return t || '—'
}

function reportProductionDateText(iso: string | undefined | null): string {
  if (!iso || typeof iso !== 'string') return '—'
  const raw = iso.trim()
  if (!raw) return '—'
  const datePart = raw.includes('T') ? raw.slice(0, 10) : raw.slice(0, 10)
  return datePart || '—'
}

function reportQtyText(n: unknown): string {
  const x = Number(n)
  if (!Number.isFinite(x)) return '—'
  return x.toLocaleString('zh-CN')
}

function regionTypeLabel(v: number | undefined): string {
  const n = normalizeRegionType(v)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

function stockItemWarehouseNameText(row: StockItemListRow): string {
  const name = (row.warehouseName ?? '').trim()
  if (name) return name
  const code = (row.warehouseCode ?? '').trim()
  return code || displayWarehouseCode.value.trim() || '—'
}

function stockItemStockTypeLabel(row: StockItemListRow): string {
  const n = Number(row.stockType)
  if (n === 2) return t('inventoryList.stockTypes.stocking')
  if (n === 3) return t('inventoryList.stockTypes.sample')
  return t('inventoryList.stockTypes.customer')
}

const isCustomsStockInDetail = computed(() => detailStockInType.value === StockInTypeCode.Customs)

const customsContextItems = computed((): StockInCustomsContextItemDto[] => customsContext.value?.items ?? [])

const customsDeclarationSummaries = computed(() => {
  const map = new Map<string, StockInCustomsContextItemDto>()
  for (const row of customsContextItems.value) {
    const id = (row.declarationId ?? '').trim()
    if (!id || map.has(id)) continue
    map.set(id, row)
  }
  return [...map.values()]
})

function customsWarehouseRoute(row: StockInCustomsContextItemDto): string {
  const from = (row.fromWarehouseCode ?? row.fromWarehouseId ?? '').trim()
  const to = (row.toWarehouseCode ?? row.toWarehouseId ?? '').trim()
  if (!from && !to) return '—'
  if (from && to) return `${from} → ${to}`
  return from || to
}

function customsPriceText(n: number | null | undefined): string {
  if (maskPurchaseSensitiveFields.value) return '—'
  const x = Number(n)
  if (!Number.isFinite(x) || x <= 0) return '—'
  return x.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

function customsFeeMoneyText(n: number | null | undefined): string {
  if (maskPurchaseSensitiveFields.value) return '—'
  const x = Number(n)
  if (!Number.isFinite(x) || x <= 0) return '—'
  return x.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function customsDateText(iso: string | null | undefined): string {
  if (!iso || typeof iso !== 'string') return '—'
  return iso.includes('T') ? iso.slice(0, 10) : iso.slice(0, 10)
}

function customsClearanceLabel(v: number | null | undefined): string {
  const n = Number(v)
  if (n === 10) return t('stockInDetail.clearanceReleased')
  if (n === 100) return t('stockInDetail.clearanceCleared')
  return t('stockInDetail.clearanceNone')
}

const customsTimelineGroups = computed(() =>
  customsContextItems.value
    .filter((row) => (row.timeline?.length ?? 0) > 0)
    .map((row) => ({
      key: `${row.declarationId}-${row.lineNo}`,
      title: `${row.declarationCode || '—'}-${row.lineNo ?? '?'}`,
      steps: [...(row.timeline ?? [])].sort((a, b) => a.sortOrder - b.sortOrder)
    }))
)

function customsTimelineStepLabel(stepCode: string): string {
  const label = t(`stockInDetail.timelineSteps.${stepCode}`)
  return label === `stockInDetail.timelineSteps.${stepCode}` ? stepCode : label
}

function customsTimelineTimeText(iso: string | null | undefined): string {
  if (!iso || typeof iso !== 'string') return ''
  return iso.includes('T') ? iso.replace('T', ' ').slice(0, 19) : iso.slice(0, 19)
}

function customsTimelineDocText(step: StockInCustomsTimelineStepDto): string {
  const code = (step.docCode ?? '').trim()
  if (code) return code
  if (step.stepCode === 'pendlist' && step.state === 'done') return t('stockInDetail.timelineSteps.pendlist')
  return ''
}

function customsTimelineStatusText(step: StockInCustomsTimelineStepDto): string {
  if (step.state !== 'done' || step.status == null) return ''
  const n = Number(step.status)
  if (step.stepCode === 'pendlist') {
    if (n === CUSTOMS_PENDLIST_STATUS.Open) return t('stockInDetail.timelinePendlistStatus.open')
    if (n === CUSTOMS_PENDLIST_STATUS.CustomsOutNotifyCreated)
      return t('stockInDetail.timelinePendlistStatus.customsOutCreated')
    if (n === CUSTOMS_PENDLIST_STATUS.InCustomsProcess) return t('stockInDetail.timelinePendlistStatus.inProcess')
    if (n === CUSTOMS_PENDLIST_STATUS.Closed) return t('stockInDetail.timelinePendlistStatus.closed')
    if (n === CUSTOMS_PENDLIST_STATUS.Cancelled) return t('stockInDetail.timelinePendlistStatus.cancelled')
  }
  if (step.stepCode === 'salesStockOutNotify' || step.stepCode === 'customsStockOutNotify') {
    if (n === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return t('stockInDetail.timelineSorStatus.pendingCustoms')
    if (n === STOCK_OUT_REQUEST_STATUS.PendingPacking) return t('stockInDetail.timelineSorStatus.pendingPacking')
    if (n === STOCK_OUT_REQUEST_STATUS.Packed) return t('stockInDetail.timelineSorStatus.packed')
    if (n === STOCK_OUT_REQUEST_STATUS.StockedOut) return t('stockInDetail.timelineSorStatus.stockedOut')
    if (n === STOCK_OUT_REQUEST_STATUS.Cancelled) return t('stockInDetail.timelineSorStatus.cancelled')
  }
  if (step.stepCode === 'packing') {
    if (n === PackingStatusCode.New) return t('stockInDetail.timelinePackingStatus.new')
    if (n === PackingStatusCode.Confirmed) return t('stockInDetail.timelinePackingStatus.confirmed')
    if (n === PackingStatusCode.Picked) return t('stockInDetail.timelinePackingStatus.picked')
    if (n === PackingStatusCode.Ready) return t('stockInDetail.timelinePackingStatus.ready')
    if (n === PackingStatusCode.PendingStockOut) return t('stockInDetail.timelinePackingStatus.pendingStockOut')
    if (n === PackingStatusCode.StockOutFinished) return t('stockInDetail.timelinePackingStatus.stockOutFinished')
  }
  if (step.stepCode === 'declaration') {
    if (n === 1) return t('stockInDetail.timelineDeclarationStatus.pending')
    if (n === 2) return t('stockInDetail.timelineDeclarationStatus.processing')
    if (n === 3) return t('stockInDetail.timelineDeclarationStatus.completed')
    if (n === -1) return t('stockInDetail.timelineDeclarationStatus.voided')
  }
  if (step.stepCode === 'arrivalNotify') {
    if (n === 10) return t('stockInDetail.timelineArrivalStatus.notArrived')
    if (n === 20) return t('stockInDetail.timelineArrivalStatus.pendingQc')
    if (n === 30) return t('stockInDetail.timelineArrivalStatus.qcDone')
    if (n === 100) return t('stockInDetail.timelineArrivalStatus.stockedIn')
  }
  if (step.stepCode === 'qc') {
    if (n === -1) return t('stockInDetail.timelineQcStatus.failed')
    if (n === 10) return t('stockInDetail.timelineQcStatus.partial')
    if (n === 100) return t('stockInDetail.timelineQcStatus.passed')
  }
  return ''
}

function customsTimelineRoute(step: StockInCustomsTimelineStepDto): RouteLocationRaw | null {
  const id = (step.docId ?? '').trim()
  if (!id || step.state !== 'done') return null
  switch (step.stepCode) {
    case 'salesStockOutNotify':
    case 'customsStockOutNotify':
      return { name: 'StockOutNotifyDetail', params: { id } }
    case 'pendlist':
      return { name: 'CustomsPendlistList' }
    case 'packing':
      return { name: 'PackingDetail', params: { id } }
    case 'declaration':
      return { name: 'CustomsDeclarationDetail', params: { id } }
    case 'stockTransfer':
      return { name: 'StockTransferList' }
    case 'arrivalNotify':
      return { name: 'ArrivalNoticeList' }
    case 'qc':
      return { name: 'QcCreate', query: { qcId: id } }
    case 'stockIn':
      return { name: 'StockInDetail', params: { id } }
    default:
      return null
  }
}

const detailArrivalNotifyTooltip = computed(() => {
  const code = detailSourceDisplayNo.value.trim()
  if (!code) return ''
  return t('stockInList.arrivalNotifyCodeTooltip', { code })
})

const handleSubmit = async () => {
  if (!form.stockInCode || !form.warehouseId) {
    ElMessage.warning('请填写入库单号和仓库ID')
    return
  }
  if (!form.items || form.items.length === 0) {
    ElMessage.warning('请至少添加一条入库明细')
    return
  }

  submitting.value = true
  try {
    form.totalQuantity = totalQuantity.value
    const payload: CreateStockInRequest = {
      ...form,
      items: (form.items || []).map(({ materialBrand: _brand, ...rest }) => ({ ...rest }))
    }
    await stockInApi.create(payload)
    ElMessage.success('入库单创建成功')
    router.push('/inventory/stock-in')
  } catch (e) {
    console.error(e)
    ElMessage.error('保存入库单失败')
  } finally {
    submitting.value = false
  }
}

const goBack = () => {
  router.push('/inventory/stock-in')
}

function openBatchImport(row: StockInItemDto) {
  if (detailStatus.value !== 2) {
    ElMessage.error('仅已过账的入库单可录入批次')
    return
  }
  const id = (row.itemId ?? '').trim()
  if (!id) {
    ElMessage.error('该明细缺少主键，无法录入批次')
    return
  }
  batchImportItemId.value = id
  batchImportItemCode.value = (row.stockInItemCode ?? '').trim()
  batchImportVisible.value = true
}

function onRowBatchImportSuccess() {
  batchPanelRef.value?.refresh?.()
}

</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockin-edit-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
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
  flex-shrink: 0;

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.stockin-caption-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
  min-width: 0;
}

.caption-avatar-lg {
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

.title-meta--caption {
  margin-top: 4px;
}

.stockin-header-meta-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  min-height: 28px;
}

.detail-content {
  min-height: 200px;
}

.info-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.info-section__body {
  padding: 16px 20px 20px;
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
  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.6);
  }
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
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
  border-right: 1px solid rgba(255, 255, 255, 0.04);
  &:nth-child(3n) {
    border-right: none;
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
}

.info-grid--basic {
  .info-item {
    &:nth-child(3n) {
      border-right: none;
    }
  }
  .info-item--basic-spacer {
    border-right: none;
  }
}

.info-grid--inline-labels .info-item--span-all {
  grid-column: 1 / -1;
  border-right: none;
}

.info-label {
  font-size: 11px;
  color: $text-muted;
}

.info-value {
  font-size: 13px;
  color: $text-secondary;
  &--time {
    font-size: 12px;
    color: $text-muted;
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
  display: inline-flex;
  align-items: center;
  gap: 6px;
  &:hover {
    color: $text-secondary;
  }
  &--active {
    color: $cyan-primary;
    border-bottom-color: $cyan-primary;
  }
}

.tab-count {
  font-size: 11px;
  padding: 1px 7px;
  border-radius: 999px;
  background: rgba(0, 212, 255, 0.1);
  color: $cyan-primary;
}

.tabs-body {
  padding: 20px;
}

.detail-items-table-wrap {
  margin-top: 4px;
}

// §7.4 表头/表体基线见 detail-panel-list-table.scss；此处仅 CrmDataTable 操作列等页内扩展
.detail-items-table-wrap :deep(.items-table) {
  --el-table-border-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
  border-radius: 0;

  :deep(.el-table) {
    color: var(--crm-table-text);
  }
  :deep(.el-table__inner-wrapper) {
    background: transparent;
    &::before {
      display: none !important;
    }
    &::after {
      display: none !important;
    }
  }
  :deep(.el-table__border-left-patch) {
    display: none !important;
  }
  :deep(.el-table__cell) {
    .el-button {
      white-space: nowrap !important;
    }
    .cell {
      white-space: nowrap;
    }
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

/* 库存明细列多：横向滚动 + 表头单行不换行 */
.stockin-stock-items-table-wrap {
  overflow-x: auto;
  :deep(.items-table .el-table__header-wrapper th.el-table__cell .cell) {
    white-space: nowrap;
    word-break: keep-all;
    line-height: 1.35;
  }
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  .header-left { display: flex; align-items: center; gap: 12px; }
  .header-right { display: flex; align-items: center; gap: 8px; }
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
  .page-icon {
    width: 36px;
    height: 36px;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.25);
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: $cyan-primary;
  }
  .page-title { font-size: 20px; font-weight: 600; color: $text-primary; margin: 0; }
}
.btn-primary,
.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid transparent;
}
.btn-primary {
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border-color: rgba(0, 212, 255, 0.4);
  color: #fff;
}
.btn-secondary {
  background: rgba(255, 255, 255, 0.05);
  border-color: $border-panel;
  color: $text-secondary;
}
.btn-sm {
  padding: 6px 10px;
  font-size: 12px;
}
.form-layout {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.form-card {
  background: $layer-2;
  border-radius: 8px;
  border: 1px solid $border-panel;
  padding: 16px 18px;
}
.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: var(--crm-detail-section-header-bg);
  margin-bottom: 0;
  .section-title {
    margin: 0;
  }
}
.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-secondary;
  margin: 0 0 8px;
}
.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-0 { background: rgba(255,255,255,0.05); color: $text-muted; }
  &.status-1 { background: rgba(255,193,7,0.15); color: #ffc107; }
  &.status-2 { background: rgba(70,191,145,0.18); color: #46BF91; }
  &.status-3 { background: rgba(201,87,69,0.18); color: #C95745; }
}
.stockin-form {
  max-width: 600px;
}

/* 详情：基础信息只读报表（非输入框外观） */
.stockin-report-dl {
  margin: 0;
  max-width: 720px;
}
.stockin-report-row {
  display: grid;
  grid-template-columns: 96px 1fr;
  gap: 10px 16px;
  align-items: start;
  padding: 8px 0;
  border-bottom: 1px solid $border-panel;
  font-size: 13px;
  &:last-child {
    border-bottom: none;
  }
  dt {
    margin: 0;
    color: $text-muted;
    font-weight: 500;
    white-space: nowrap;
  }
  dd {
    margin: 0;
    color: $text-primary;
    word-break: break-word;
  }
}
.stockin-report-row--block {
  grid-template-columns: 96px 1fr;
}
.stockin-report-multiline {
  white-space: pre-wrap;
  line-height: 1.5;
}

.stockin-report-cell {
  display: inline-block;
  font-size: 13px;
  color: $text-primary;
  line-height: 1.5;
  &--num {
    font-variant-numeric: tabular-nums;
  }
}

.stockin-report-empty {
  margin-top: 10px;
  font-size: 12px;
  color: $text-muted;
}

.stockin-report-row--inline {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 4px;
  font-size: 13px;
}
.customs-meta-label {
  color: $text-muted;
  font-weight: 500;
}
.sub-title {
  margin: 16px 0 8px;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}
.customs-summary-wrap {
  margin-top: 8px;
}
.cell-link {
  color: $cyan-primary;
  text-decoration: none;
  &:hover {
    text-decoration: underline;
  }
}

.table-footer {
  display: flex;
  justify-content: flex-end;
  margin-top: 8px;
  .total {
    font-size: 13px;
    color: $text-secondary;
    span {
      color: $cyan-primary;
      font-weight: 600;
      margin-left: 4px;
    }
  }
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  white-space: nowrap;
  flex-shrink: 0;
  &:hover { text-decoration: underline; }
}

.stockin-code-cell {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}

.customs-notify-tag {
  flex: 0 0 auto;
  padding: 1px 6px;
  border-radius: 4px;
  font-size: 11px;
  line-height: 1.4;
  color: #ffb84d;
  background: rgba(255, 184, 77, 0.14);
  border: 1px solid rgba(255, 184, 77, 0.45);
  cursor: default;
  user-select: none;
}

.customs-timeline-wrap {
  margin-top: 16px;
}

.customs-timeline-group {
  margin-bottom: 20px;

  &:last-child {
    margin-bottom: 0;
  }
}

.customs-timeline-group-title {
  margin-bottom: 8px;
  font-size: 13px;
  font-weight: 600;
  color: $text-secondary;
}

.customs-timeline {
  padding-left: 4px;
}

.customs-timeline-step {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.customs-timeline-step-label {
  font-weight: 500;
  color: $text-primary;
}

.customs-timeline-doc {
  font-size: 13px;
}

.customs-timeline-status {
  font-size: 12px;
  color: $text-muted;
}

.customs-timeline-state {
  font-size: 12px;

  &--pending {
    color: $text-muted;
  }
}
</style>

