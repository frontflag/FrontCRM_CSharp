<template>
  <div class="stockout-detail-page" v-loading="loading" element-loading-background="rgba(10,22,40,0.8)">
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('stockOutDetail.back') }}
        </button>
        <div v-if="detail" class="stockout-caption-title-group">
          <div class="caption-avatar-lg">{{ stockOutCaptionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title" :class="{ 'page-title--muted': detail.status === 3 }">
                  {{ t('stockOutDetail.captionPrefix') }} {{ detail.stockOutCode || '—' }}
                </h1>
                <el-tooltip
                  v-if="isCustomsStockOut && salesNotifyTooltip"
                  :content="salesNotifyTooltip"
                  placement="top"
                  :hide-after="0"
                >
                  <span class="customs-notify-tag">{{ t('stockOutList.customsNotifyTag') }}</span>
                </el-tooltip>
              </div>
            </div>
            <div class="title-meta title-meta--caption stockout-header-meta-row">
              <el-tag effect="dark" :type="stockOutStatusTagType" size="small">
                {{ statusLabel(detail.status) }}
              </el-tag>
              <StockBizTypeTag
                biz="out"
                :type="detail.stockOutType"
                :customs-declaration-id="detail.customsDeclarationId"
                :customs-declaration-code="detail.customsDeclarationCode"
              />
            </div>
          </div>
        </div>
      </div>
      <div v-if="detail" class="header-right">
        <button type="button" class="btn-secondary" @click="goInvoiceReport">
          {{ t('stockOutDetail.printInvoice') }}
        </button>
        <button type="button" class="btn-primary" :disabled="saving" @click="saveHeader">
          {{ saving ? t('stockOutDetail.saving') : t('stockOutDetail.save') }}
        </button>
      </div>
    </div>

    <div class="detail-content">
      <template v-if="detail">
        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('stockOutDetail.basicInfo') }}</span>
            </div>
            <div class="section-header__meta">
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('stockOutDetail.createDate') }}</span>
                <span class="section-header-meta-item__value">{{ stockOutBasicCreateDateText }}</span>
              </span>
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('stockOutDetail.createUser') }}</span>
                <span class="section-header-meta-item__value">{{ stockOutBasicCreateUserText }}</span>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('stockOutDetail.sourceCode') }}</span>
              <span class="info-value">{{ reportCellText(detail.sourceCode) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutDetail.warehouseName') }}</span>
              <span class="info-value">{{ detailWarehouseNameText }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutList.columns.totalQuantity') }}</span>
              <span class="info-value">{{ formatNum(detail.totalQuantity) }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('stockOutList.columns.customerName') }}</span>
              <span class="info-value">{{ maskSaleSensitiveFields ? '—' : reportCellText(detail.customerName) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutList.columns.salesUserName') }}</span>
              <span class="info-value">{{ maskSaleSensitiveFields ? '—' : reportCellText(detail.salesUserName) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutList.columns.expectedStockOutDate') }}</span>
              <span class="info-value info-value--time">{{ expectedStockOutDateText }}</span>
            </div>
          </div>
          <div
            v-if="detailSalesNotifyId && detailSalesNotifyCode"
            class="info-grid info-grid--inline-labels info-grid--basic"
          >
            <div class="info-item">
              <span class="info-label">{{ t('stockOutList.salesNotifyCodeLink') }}</span>
              <span class="info-value">
                <router-link :to="{ name: 'StockOutNotifyDetail', params: { id: detailSalesNotifyId } }" class="cell-link">
                  {{ detailSalesNotifyCode }}
                </router-link>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels">
            <div class="info-item info-item--span-all">
              <span class="info-label">{{ t('stockOutList.columns.remark') }}</span>
              <span class="info-value">{{ reportCellText(detail.remark) }}</span>
            </div>
          </div>
        </div>

        <StockOutCustomsSummaryPanel v-if="detail.customsSummary?.declarationId" :summary="detail.customsSummary" />

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('stockOutDetail.sectionEditable') }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic info-grid--editable">
            <div class="info-item">
              <span class="info-label">{{ t('stockOutList.columns.stockOutDate') }}</span>
              <span class="info-value info-value--control">
                <el-date-picker
                  v-model="editForm.stockOutDate"
                  type="date"
                  value-format="YYYY-MM-DD"
                  :placeholder="t('stockOutDetail.pickDate')"
                  style="width: 100%"
                />
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutDetail.shipmentMethod') }}</span>
              <span class="info-value info-value--control">
                <el-select
                  v-model="editForm.shipmentMethod"
                  clearable
                  filterable
                  :placeholder="t('stockOutDetail.shipmentPlaceholder')"
                  style="width: 100%"
                >
                  <el-option v-for="o in shipmentMethodOptions" :key="o.value" :label="o.label" :value="o.value" />
                </el-select>
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutList.columns.expressCompany') }}</span>
              <span class="info-value info-value--control">
                <el-select
                  v-model="editForm.expressCompany"
                  clearable
                  filterable
                  :disabled="!isExpressShipmentMethod(editForm.shipmentMethod)"
                  :placeholder="t('stockOutDetail.shipmentPlaceholder')"
                  style="width: 100%"
                >
                  <el-option v-for="o in expressOptions" :key="o.value" :label="o.label" :value="o.value" />
                </el-select>
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutDetail.courierTrackingNo') }}</span>
              <span class="info-value info-value--control">
                <el-input
                  v-model="editForm.courierTrackingNo"
                  clearable
                  :placeholder="t('stockOutDetail.trackingPlaceholder')"
                />
              </span>
            </div>
          </div>
        </div>

        <div class="tabs-section">
          <div class="tabs-nav">
            <button
              class="tab-btn"
              type="button"
              :class="{ 'tab-btn--active': detailActiveTab === 'items' }"
              @click="detailActiveTab = 'items'"
            >
              {{ t('stockOutDetail.tabs.items') }}
              <span v-if="stockOutItems.length" class="tab-count">{{ stockOutItems.length }}</span>
            </button>
            <button
              class="tab-btn"
              type="button"
              :class="{ 'tab-btn--active': detailActiveTab === 'documents' }"
              @click="detailActiveTab = 'documents'"
            >
              {{ t('stockOutDetail.tabs.documents') }}
            </button>
          </div>
          <div class="tabs-body">
            <div v-show="detailActiveTab === 'items'" v-loading="itemsLoading" class="detail-items-table-wrap">
              <el-empty v-if="!itemsLoading && !stockOutItems.length" :description="t('stockOutDetail.noItems')" :image-size="80" />
              <CrmDataTable
                v-else-if="stockOutItems.length"
                :data="stockOutItems"
                class="items-table detail-panel-list-table receivable-stock-out-item-embed"
                size="small"
                stripe
                row-key="stockOutItemId"
              >
                <el-table-column :label="t('stockOutItemList.columns.status')" width="100" align="center">
                  <template #default="{ row }">
                    <span :class="['status-badge', `status-${row.status}`]">{{ stockOutItemStatusLabel(row.status) }}</span>
                  </template>
                </el-table-column>
                <el-table-column
                  prop="stockOutCode"
                  :label="t('stockOutItemList.columns.stockOutCode')"
                  width="150"
                  show-overflow-tooltip
                />
                <el-table-column
                  prop="stockOutItemCode"
                  :label="t('stockOutItemList.columns.stockOutItemCode')"
                  width="160"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">{{ row.stockOutItemCode || stockOutItemNa }}</template>
                </el-table-column>
                <el-table-column
                  prop="stockInCode"
                  :label="t('stockOutItemList.columns.stockInCode')"
                  width="140"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">{{ row.stockInCode || stockOutItemNa }}</template>
                </el-table-column>
                <el-table-column
                  prop="packingCode"
                  :label="t('stockOutItemList.columns.packingCode')"
                  width="150"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">
                    <router-link
                      v-if="row.packingId?.trim() && row.packingCode?.trim()"
                      class="cell-link mono-cell"
                      :to="`/inventory/packing/${row.packingId.trim()}`"
                      @click.stop
                    >
                      {{ row.packingCode.trim() }}
                    </router-link>
                    <span v-else-if="row.packingCode?.trim()" class="mono-cell">{{ row.packingCode.trim() }}</span>
                    <span v-else>{{ stockOutItemNa }}</span>
                  </template>
                </el-table-column>
                <el-table-column
                  prop="freightForwarderOrderNo"
                  :label="t('common.freightForwarderOrderNo')"
                  width="160"
                >
                  <template #default="{ row }">
                    <CrmListCopyableTextCell :text="pickCrmCopyableRowField(row, 'freightForwarderOrderNo')" :empty-text="stockOutItemNa" />
                  </template>
                </el-table-column>
                <el-table-column :label="t('stockOutItemList.columns.stockOutDate')" width="118">
                  <template #default="{ row }">{{ formatStockOutDateOnly(row.stockOutDate) }}</template>
                </el-table-column>
                <el-table-column
                  prop="customerName"
                  :label="t('stockOutItemList.columns.customerName')"
                  min-width="120"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">{{ maskSaleSensitiveFields ? '—' : row.customerName || stockOutItemNa }}</template>
                </el-table-column>
                <el-table-column
                  prop="salesUserName"
                  :label="t('stockOutItemList.columns.salesUserName')"
                  width="100"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">{{ maskSaleSensitiveFields ? '—' : row.salesUserName || stockOutItemNa }}</template>
                </el-table-column>
                <el-table-column
                  prop="purchasePn"
                  :label="t('stockOutItemList.columns.purchasePn')"
                  min-width="130"
                >
                  <template #default="{ row }">
                    <CrmListCopyableTextCell :text="pickCrmCopyableRowField(row, 'purchasePn')" :empty-text="stockOutItemNa" />
                  </template>
                </el-table-column>
                <el-table-column
                  prop="purchaseBrand"
                  :label="t('stockOutItemList.columns.purchaseBrand')"
                  min-width="100"
                >
                  <template #default="{ row }">
                    <CrmListCopyableTextCell :text="pickCrmCopyableRowField(row, 'purchaseBrand')" :empty-text="stockOutItemNa" />
                  </template>
                </el-table-column>
                <el-table-column
                  prop="outQuantity"
                  :label="t('stockOutItemList.columns.outQuantity')"
                  min-width="120"
                  align="right"
                  show-overflow-tooltip
                />
                <el-table-column
                  :label="t('financeReceivableDetail.stockOutItemLabels.salesUnitPrice')"
                  min-width="132"
                  align="right"
                  header-align="right"
                  class-name="stock-item-unit-price-col"
                >
                  <template #default="{ row }">
                    <span v-if="!listShowAmountColumns">—</span>
                    <template v-else-if="row.salesPrice != null && unitPriceDockHasValue(row.salesPrice)">
                      <div class="dock-tier-price-line">
                        <template v-for="amt in [splitUnitPriceDockParts(row.salesPrice)]" :key="`sup-${row.stockOutItemId}`">
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
                    <span v-else>{{ stockOutItemNa }}</span>
                  </template>
                </el-table-column>
                <el-table-column
                  :label="t('financeReceivableDetail.stockOutItemLabels.outAmount')"
                  min-width="128"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">
                    <span v-if="!listShowAmountColumns">—</span>
                    <template v-else-if="stockOutLineAmount(row) != null">
                      <div class="dock-tier-price-line">
                        <span class="dock-tier-amt">{{ formatTotalAmountNumber(stockOutLineAmount(row)) }}</span>
                        <span class="dock-tier-ccy-gap">&nbsp;</span>
                        <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.salesCurrency)]">{{
                          listAmountCurrencyIso(row.salesCurrency)
                        }}</span>
                      </div>
                    </template>
                    <span v-else>{{ stockOutItemNa }}</span>
                  </template>
                </el-table-column>
                <el-table-column
                  prop="shipmentMethod"
                  :label="t('stockOutItemList.columns.shipmentMethod')"
                  width="110"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">{{ stockOutItemShipmentMethodDisplay(row.shipmentMethod) }}</template>
                </el-table-column>
                <el-table-column
                  prop="courierTrackingNo"
                  :label="t('stockOutItemList.columns.courierTrackingNo')"
                  width="130"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">{{ row.courierTrackingNo || stockOutItemNa }}</template>
                </el-table-column>
                <el-table-column
                  prop="sellOrderItemCode"
                  :label="t('stockOutItemList.columns.sellOrderItemCode')"
                  width="130"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">{{ row.sellOrderItemCode || stockOutItemNa }}</template>
                </el-table-column>
              </CrmDataTable>
            </div>
            <div v-show="detailActiveTab === 'documents'">
              <p class="doc-hint">{{ t('stockOutDetail.docHint') }}</p>
              <DocumentUploadPanel
                :biz-type="DOC_BIZ"
                :biz-id="detail.id"
                :max-files="20"
                :max-size-mb="100"
                @uploaded="docListRef?.refresh()"
              />
              <DocumentListPanel
                ref="docListRef"
                :biz-type="DOC_BIZ"
                :biz-id="detail.id"
                view-mode="list"
                style="margin-top: 16px"
              />
            </div>
          </div>
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('financeReceivableDetail.sellOrderItemSection') }}</span>
              <span v-if="sellOrderItemRows.length" class="section-count">{{ sellOrderItemRows.length }}</span>
            </div>
          </div>
          <div class="detail-panel-section-body">
            <div v-loading="sellOrderItemLoading" class="detail-items-table-wrap">
              <CrmDataTable
                v-if="sellOrderItemRows.length"
                :data="sellOrderItemRows"
                embedded
                column-layout-key="stock-out-detail-so-item"
                :columns="sellOrderItemColumns"
                :border="false"
                :show-column-settings="false"
                :show-row-density-toggle="false"
                class="items-table detail-panel-list-table receivable-so-item-embed"
                size="small"
                stripe
                row-key="sellOrderItemId"
              >
                <template #col-customerName="{ row }">
                  <span>{{ maskSaleSensitiveFields ? '—' : (row.customerName || '—') }}</span>
                </template>
                <template #col-customerSo="{ row }">
                  <span>{{ maskSaleSensitiveFields ? '—' : (row.customerSo || '—') }}</span>
                </template>
                <template #col-customerPn="{ row }">
                  <span>{{ maskSaleSensitiveFields ? '—' : (row.customerPn || '—') }}</span>
                </template>
                <template #col-salesUserName="{ row }">
                  <span>{{ maskSaleSensitiveFields ? '—' : (row.salesUserName || '—') }}</span>
                </template>
                <template #col-orderStatus="{ row }">
                  <el-tag effect="dark" :type="soItemStatusTagType(row.orderStatus)" size="small">
                    {{ soItemStatusText(row.orderStatus) }}
                  </el-tag>
                </template>
                <template #col-purchaseProgressStatus="{ row }">
                  <el-tag effect="dark" :type="soItemExtendTriTagType(row.purchaseProgressStatus)" size="small">
                    {{ soItemExtendTriLabel('purchase', row.purchaseProgressStatus) }}
                  </el-tag>
                </template>
                <template #col-stockInProgressStatus="{ row }">
                  <el-tag effect="dark" :type="soItemExtendTriTagType(row.stockInProgressStatus)" size="small">
                    {{ soItemExtendTriLabel('stockIn', row.stockInProgressStatus) }}
                  </el-tag>
                </template>
                <template #col-stockOutProgressStatus="{ row }">
                  <el-tag effect="dark" :type="soItemExtendTriTagType(row.stockOutProgressStatus)" size="small">
                    {{ soItemExtendTriLabel('stockOut', row.stockOutProgressStatus) }}
                  </el-tag>
                </template>
                <template #col-stockOutNotifyProgressStatus="{ row }">
                  <el-tag effect="dark" :type="soItemExtendTriTagType(row.stockOutNotifyProgressStatus)" size="small">
                    {{ soItemExtendTriLabel('stockOutNotify', row.stockOutNotifyProgressStatus) }}
                  </el-tag>
                </template>
                <template #col-receiptProgressStatus="{ row }">
                  <el-tag effect="dark" :type="soItemExtendTriTagType(row.receiptProgressStatus)" size="small">
                    {{ soItemExtendTriLabel('receipt', row.receiptProgressStatus) }}
                  </el-tag>
                </template>
                <template #col-invoiceProgressStatus="{ row }">
                  <el-tag effect="dark" :type="soItemExtendTriTagType(row.invoiceProgressStatus)" size="small">
                    {{ soItemExtendTriLabel('invoice', row.invoiceProgressStatus) }}
                  </el-tag>
                </template>
                <template #col-currency="{ row }">{{ soItemSettlementCurrencyLabel(row.currency) }}</template>
                <template #col-price="{ row }">
                  <span class="amount-with-code">
                    <span>{{ formatUnitPriceNumber(row.price) }}</span>
                    <span
                      v-if="formatUnitPriceNumber(row.price) !== '—'"
                      :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]"
                    >
                      {{ listAmountCurrencyIso(row.currency) }}
                    </span>
                  </span>
                </template>
                <template #col-lineTotal="{ row }">
                  <span class="amount-with-code">
                    <span>{{ formatTotalAmountNumber(row.lineTotal) }}</span>
                    <span
                      v-if="formatTotalAmountNumber(row.lineTotal) !== '—'"
                      :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]"
                    >
                      {{ listAmountCurrencyIso(row.currency) }}
                    </span>
                  </span>
                </template>
                <template #col-usdUnitPrice="{ row }">
                  <span v-if="row.usdUnitPrice != null" class="amount-with-code">
                    <span>{{ Number(row.usdUnitPrice).toFixed(6) }}</span>
                    <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
                  </span>
                  <span v-else>—</span>
                </template>
                <template #col-usdLineTotal="{ row }">
                  <span v-if="row.usdLineTotal != null" class="amount-with-code">
                    <span>{{ Number(row.usdLineTotal).toFixed(2) }}</span>
                    <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
                  </span>
                  <span v-else>—</span>
                </template>
                <template #col-salesProfitExpected="{ row }">
                  <span v-if="row.salesProfitExpected != null" class="amount-with-code">
                    <span>{{ Number(row.salesProfitExpected).toFixed(2) }}</span>
                    <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
                  </span>
                  <span v-else>—</span>
                </template>
                <template #col-profitOutBizUsd="{ row }">
                  <span v-if="row.profitOutBizUsd != null" class="amount-with-code">
                    <span>{{ Number(row.profitOutBizUsd).toFixed(2) }}</span>
                    <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
                  </span>
                  <span v-else>—</span>
                </template>
                <template #col-profitOutRateBiz="{ row }">{{
                  formatProfitOutRateBizDisplay(row.profitOutBizUsd, row.profitOutRateBiz)
                }}</template>
                <template #col-createTime="{ row }">{{ soItemFormatDt(row.createTime || row.orderCreateTime) }}</template>
                <template #col-createUser="{ row }">{{
                  row.createUserName || row.createdBy || (!maskSaleSensitiveFields ? row.salesUserName : '') || '—'
                }}</template>
              </CrmDataTable>
              <DetailListPanelEmpty
                v-else-if="!sellOrderItemLoading"
                size="low"
                :description="t('financeReceivableDetail.noSellOrderItem')"
              />
            </div>
          </div>
        </div>

        <div v-if="isSalesStockOut" class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('stockOutDetail.receivableSection') }}</span>
              <span v-if="receivableRows.length" class="section-count">{{ receivableRows.length }}</span>
            </div>
          </div>
          <div class="detail-panel-section-body">
            <div v-loading="receivableLoading" class="detail-items-table-wrap">
              <CrmDataTable
                v-if="receivableRows.length"
                :data="receivableRows"
                embedded
                column-layout-key="stock-out-detail-receivables"
                :border="false"
                :show-column-settings="false"
                :show-row-density-toggle="false"
                class="items-table detail-panel-list-table receivable-so-item-embed"
                size="small"
                stripe
                row-key="id"
              >
                <el-table-column
                  prop="receivableCode"
                  :label="t('financeReceivableList.columns.code')"
                  min-width="140"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">
                    <router-link
                      v-if="canOpenReceivableDetail && row.id && row.receivableCode"
                      class="cell-link mono-cell"
                      :to="`/finance/receivables/${row.id}`"
                    >
                      {{ row.receivableCode }}
                    </router-link>
                    <span v-else class="mono-cell">{{ row.receivableCode || '—' }}</span>
                  </template>
                </el-table-column>
                <el-table-column
                  prop="sellOrderItemCode"
                  :label="t('stockOutItemList.columns.sellOrderItemCode')"
                  min-width="150"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">{{ row.sellOrderItemCode || '—' }}</template>
                </el-table-column>
                <el-table-column
                  prop="outboundQty"
                  :label="t('financeReceivableList.columns.qty')"
                  width="110"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">{{ formatNum(row.outboundQty) }}</template>
                </el-table-column>
                <el-table-column
                  :label="t('financeReceivableList.columns.amount')"
                  min-width="140"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">
                    <span v-if="!listShowAmountColumns">—</span>
                    <span v-else class="amount-with-code">
                      <span>{{ formatTotalAmountNumber(row.amount) }}</span>
                      <span
                        v-if="formatTotalAmountNumber(row.amount) !== '—'"
                        :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]"
                      >
                        {{ listAmountCurrencyIso(row.currency) }}
                      </span>
                    </span>
                  </template>
                </el-table-column>
                <el-table-column
                  :label="t('financeReceivableList.columns.verificationStatus')"
                  width="120"
                  align="center"
                >
                  <template #default="{ row }">
                    <el-tag :type="receivableVerificationTagType(row.verificationStatus)" size="small">
                      {{ receivableVerificationLabel(row.verificationStatus) }}
                    </el-tag>
                  </template>
                </el-table-column>
                <el-table-column
                  :label="t('financeReceivableList.columns.verifiedDone')"
                  min-width="140"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">
                    <span v-if="!listShowAmountColumns">—</span>
                    <span v-else class="amount-with-code">
                      <span>{{ formatTotalAmountNumber(row.verifiedDone) }}</span>
                      <span
                        v-if="formatTotalAmountNumber(row.verifiedDone) !== '—'"
                        :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]"
                      >
                        {{ listAmountCurrencyIso(row.currency) }}
                      </span>
                    </span>
                  </template>
                </el-table-column>
                <el-table-column
                  :label="t('financeReceivableList.columns.verifiedToBe')"
                  min-width="140"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">
                    <span v-if="!listShowAmountColumns">—</span>
                    <span v-else class="amount-with-code">
                      <span>{{ formatTotalAmountNumber(row.verifiedToBe) }}</span>
                      <span
                        v-if="formatTotalAmountNumber(row.verifiedToBe) !== '—'"
                        :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]"
                      >
                        {{ listAmountCurrencyIso(row.currency) }}
                      </span>
                    </span>
                  </template>
                </el-table-column>
                <el-table-column
                  :label="t('financeReceivableList.columns.invoiceMatchStatus')"
                  width="120"
                  align="center"
                >
                  <template #default="{ row }">
                    <el-tag :type="receivableVerificationTagType(row.invoiceMatchStatus)" size="small">
                      {{ receivableVerificationLabel(row.invoiceMatchStatus) }}
                    </el-tag>
                  </template>
                </el-table-column>
              </CrmDataTable>
              <DetailListPanelEmpty
                v-else-if="!receivableLoading"
                size="low"
                :description="receivableEmptyText"
              />
            </div>
          </div>
        </div>
      </template>

      <el-empty v-else-if="!loading" :description="loadError || t('stockOutDetail.notFound')" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, inject, onMounted, onBeforeUnmount, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { stockOutApi, type StockOutDetailDto, type StockOutDetailReceivableRow, type StockOutItemListRow } from '@/api/stockOut'
import salesOrderApi from '@/api/salesOrder'
import CrmDataTable from '@/components/CrmDataTable.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import { buildSalesOrderItemListColumns } from '@/composables/buildSalesOrderItemListColumns'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { translateSalesOrderStatus, salesOrderStatusTagType } from '@/constants/salesOrderStatus'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import { isExpressShipmentMethod, useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { StockOutTypeCode } from '@/constants/stockOutType'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import StockOutCustomsSummaryPanel from '@/components/Customs/StockOutCustomsSummaryPanel.vue'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { pickCrmCopyableRowField } from '@/utils/crmListCopyableField'
import { useAuthStore } from '@/stores/auth'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import { useListRightOpsPanelInteraction } from '@/composables/useListRightOpsPanelInteraction'
import { useCustomerWorkspacePanelStore } from '@/stores/customerWorkspacePanel'
import type { SalesOrderItemLineRow } from '@/stores/salesOrderItemListBasket'
import {
  formatTotalAmountNumber,
  formatUnitPriceNumber,
  listAmountCurrencyDockClass,
  listAmountCurrencyIso,
  splitUnitPriceDockParts,
  unitPriceDockHasValue
} from '@/utils/moneyFormat'
import { formatProfitOutRateBizDisplay } from '@/utils/profitOutRateDisplay'

const authStore = useAuthStore()

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const DOC_BIZ = 'STOCK_OUT'

const route = useRoute()
const router = useRouter()
const { t, locale } = useI18n()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions: shipmentMethodOptions, arrivalOptions, expressOptions } =
  useLogisticsFormDict()

const stockOutItemNa = computed(() => t('quoteList.na'))

const arrivalLabelByCode = computed(() => {
  const m = new Map<string, string>()
  for (const o of arrivalOptions.value) {
    const k = String(o.value ?? '').trim()
    if (k) m.set(k.toLowerCase(), o.label)
  }
  return m
})

const canViewCustomer = computed(
  () => authStore.hasPermission('customer.info.read') || authStore.hasPermission('sales-order.read')
)
const canViewAmount = computed(() => authStore.hasPermission('sales.amount.read'))
const listCustomerColumnOk = computed(() => canViewCustomer.value && !maskSaleSensitiveFields.value)
const listShowAmountColumns = computed(() => canViewAmount.value && !maskSaleSensitiveFields.value)
const canOpenReceivableDetail = computed(() => authStore.hasPermission('finance-receipt.read'))
const isSalesStockOut = computed(() => {
  const type = Number(detail.value?.stockOutType)
  return type === StockOutTypeCode.Sales || type === 1
})
const receivableEmptyText = computed(() =>
  Number(detail.value?.status) === 4
    ? t('stockOutDetail.receivableEmptyNone')
    : t('stockOutDetail.receivableEmptyNotFinished')
)

const sellOrderItemColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return buildSalesOrderItemListColumns({
    t,
    listCustomerColumnOk: listCustomerColumnOk.value,
    listShowAmountColumns: listShowAmountColumns.value,
    opColWidth: 0,
    opColMinWidth: 0,
    withSelection: false,
    withActions: false
  })
})

const loading = ref(false)
const saving = ref(false)
const itemsLoading = ref(false)
const sellOrderItemLoading = ref(false)
const receivableLoading = ref(false)
const loadError = ref('')
const detail = ref<StockOutDetailDto | null>(null)
const stockOutItems = ref<StockOutItemListRow[]>([])
const sellOrderItemRows = ref<SalesOrderItemLineRow[]>([])
const receivableRows = ref<StockOutDetailReceivableRow[]>([])
const detailActiveTab = ref<'items' | 'documents'>('items')
const docListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)

const stockOutId = computed(() => {
  const raw = route.params.id
  if (Array.isArray(raw)) return String(raw[0] ?? '').trim()
  return String(raw ?? '').trim()
})
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const customerWorkspacePanelStore = useCustomerWorkspacePanelStore()
customerWorkspacePanelStore.setSource('stockOut')
useListRightOpsPanelInteraction({
  workspaceLayout,
  isActiveRoute: () => route.name === 'StockOutDetail',
  hasSelectedRow: () => !!customerWorkspacePanelStore.boundId,
  setRowOnly: () => {
    if (stockOutId.value) customerWorkspacePanelStore.bind('stockOut', stockOutId.value)
  },
  selectRow: async () => {
    if (!stockOutId.value) return
    customerWorkspacePanelStore.bind('stockOut', stockOutId.value)
    await customerWorkspacePanelStore.load(t('customerWorkspace.loadFailed'))
  },
  loadSelected: () => {
    void customerWorkspacePanelStore.load(t('customerWorkspace.loadFailed'))
  },
  dataTabIds: ['r-customer']
})

function bindCustomerWorkspace() {
  const id = stockOutId.value
  if (!id) {
    customerWorkspacePanelStore.clear()
    return
  }
  customerWorkspacePanelStore.bind('stockOut', id)
  if (
    workspaceLayout?.rightPanelVisible.value &&
    workspaceLayout.rightActiveTabId.value === 'r-customer'
  ) {
    void customerWorkspacePanelStore.load(t('customerWorkspace.loadFailed'))
  }
}

const editForm = ref({
  stockOutDate: '' as string,
  shipmentMethod: '' as string,
  expressCompany: '' as string,
  courierTrackingNo: '' as string
})

const stockOutCaptionAvatarChar = computed(() => {
  const c = detail.value?.stockOutCode?.trim()
  return c ? c[0]! : '出'
})

const stockOutBasicCreateDateText = computed(() => {
  const raw = detail.value?.createTime
  if (!raw) return '—'
  const s = formatDisplayDate(raw)
  return s === '--' ? '—' : s
})

const stockOutBasicCreateUserText = computed(() => detail.value?.createUserName?.trim() || '—')

const detailWarehouseNameText = computed(() => {
  const name = detail.value?.warehouseName?.trim()
  if (name) return name
  const code = detail.value?.warehouseCode?.trim()
  return code || '—'
})

const stockOutStatusTagType = computed((): '' | 'success' | 'warning' | 'info' | 'danger' | 'primary' => {
  const s = detail.value?.status
  if (s === 0) return 'info'
  if (s === 1) return 'warning'
  if (s === 2) return 'success'
  if (s === 3) return 'danger'
  if (s === 4) return 'primary'
  return 'info'
})

const expectedStockOutDateText = computed(() => {
  const raw = detail.value?.expectedStockOutDate
  if (!raw) return '—'
  const s = formatDisplayDate(raw)
  return s === '--' ? '—' : s
})

function toDateOnly(iso?: string) {
  if (!iso) return ''
  const s = String(iso)
  return s.length >= 10 ? s.slice(0, 10) : s
}

function syncEditFromDetail(d: StockOutDetailDto) {
  editForm.value = {
    stockOutDate: toDateOnly(d.stockOutDate),
    shipmentMethod: d.shipmentMethod ?? '',
    expressCompany: d.expressCompany ?? '',
    courierTrackingNo: d.courierTrackingNo ?? ''
  }
}

watch(detail, (d) => {
  if (d) syncEditFromDetail(d)
})

watch(
  () => editForm.value.shipmentMethod,
  (next) => {
    if (!isExpressShipmentMethod(next) && editForm.value.expressCompany) {
      editForm.value.expressCompany = ''
    }
  }
)

function reportCellText(v: unknown): string {
  if (v === null || v === undefined) return '—'
  const s = String(v).trim()
  return s ? s : '—'
}

const formatNum = (v: number) => (v == null ? '—' : Number(v).toLocaleString())

function stockOutLineAmount(row: StockOutItemListRow): number | null {
  if (!unitPriceDockHasValue(row.salesPrice)) return null
  const qty = Number(row.outQuantity)
  const price = Number(row.salesPrice)
  if (!Number.isFinite(qty) || !Number.isFinite(price)) return null
  return qty * price
}

function stockOutItemShipmentMethodDisplay(code?: string | number | null) {
  if (code === null || code === undefined || code === '') return stockOutItemNa.value
  const c = String(code).trim()
  if (!c) return stockOutItemNa.value
  return arrivalLabelByCode.value.get(c.toLowerCase()) ?? c
}

function stockOutItemStatusLabel(s: number) {
  switch (s) {
    case 0:
      return t('stockOutList.status.draft')
    case 1:
      return t('stockOutList.status.pending')
    case 2:
      return t('stockOutList.status.done')
    case 3:
      return t('stockOutList.status.cancelled')
    case 4:
      return t('stockOutList.status.finished')
    default:
      return t('rfqDetail.unknown')
  }
}

function formatStockOutDateOnly(v?: string | null) {
  if (!v) return stockOutItemNa.value
  return formatDisplayDateTime(v).split(/\s+/)[0] || stockOutItemNa.value
}

function soItemSettlementCurrencyLabel(code: unknown): string {
  const c = Number(code)
  if (!Number.isFinite(c)) return '—'
  return CURRENCY_CODE_TO_TEXT[c as keyof typeof CURRENCY_CODE_TO_TEXT] ?? '—'
}

function soItemStatusText(s: number) {
  return translateSalesOrderStatus(s, t)
}

function soItemStatusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  return salesOrderStatusTagType(s) as '' | 'success' | 'warning' | 'info' | 'danger'
}

function soItemExtendTriTagType(v?: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  const map: Record<number, '' | 'info' | 'success' | 'warning' | 'danger'> = {
    0: 'info',
    1: 'warning',
    2: 'success'
  }
  return v !== undefined && v !== null ? (map[v] ?? 'info') : 'info'
}

function soItemExtendTriLabel(
  kind: 'purchase' | 'stockIn' | 'stockOut' | 'stockOutNotify' | 'receipt' | 'invoice',
  v?: number
): string {
  const slot = v === 2 ? 'complete' : v === 1 ? 'partial' : 'pending'
  return t(`salesOrderItemList.extendProgress.${kind}.${slot}`)
}

function soItemFormatDt(v: unknown) {
  if (v == null || v === '') return '—'
  const s = formatDisplayDateTime(String(v))
  return s === '--' ? '—' : s
}

const isCustomsStockOut = computed(() => Number(detail.value?.stockOutType) === StockOutTypeCode.Customs)

const detailSalesNotifyId = computed(() => String(detail.value?.salesStockOutNotifyId ?? '').trim())
const detailSalesNotifyCode = computed(() => String(detail.value?.salesStockOutNotifyCode ?? '').trim())

const salesNotifyTooltip = computed(() => {
  const code = detailSalesNotifyCode.value
  if (!code) return ''
  return t('stockOutList.salesNotifyCodeTooltip', { code })
})

const statusLabel = (s: number) => {
  switch (s) {
    case 0:
      return t('stockOutList.status.draft')
    case 1:
      return t('stockOutList.status.pending')
    case 2:
      return t('stockOutList.status.done')
    case 3:
      return t('stockOutList.status.cancelled')
    case 4:
      return t('stockOutList.status.finished')
    default:
      return String(s)
  }
}

async function loadSellOrderItem() {
  const idSet = new Set<string>()
  const codes: string[] = []
  for (const row of stockOutItems.value) {
    const id = row.sellOrderItemId?.trim()
    if (id) idSet.add(id.toLowerCase())
    const code = row.sellOrderItemCode?.trim()
    if (code && !codes.includes(code)) codes.push(code)
  }
  if (idSet.size === 0) {
    sellOrderItemRows.value = []
    return
  }
  sellOrderItemLoading.value = true
  try {
    const seen = new Set<string>()
    const matched: SalesOrderItemLineRow[] = []
    const queries: Array<{ sellOrderItemCode?: string; sellOrderCode?: string }> =
      codes.length > 0 ? codes.map((sellOrderItemCode) => ({ sellOrderItemCode })) : []
    const headerOrderCode = detail.value?.sellOrderCode?.trim()
    if (queries.length === 0 && headerOrderCode) {
      queries.push({ sellOrderCode: headerOrderCode })
    }
    for (const q of queries) {
      const data = (await salesOrderApi.getItemLines({
        ...q,
        page: 1,
        pageSize: 100
      })) as { items?: SalesOrderItemLineRow[] }
      for (const row of data.items ?? []) {
        const rid = String(row.sellOrderItemId ?? row.id ?? '').trim()
        const ridKey = rid.toLowerCase()
        if (!rid || seen.has(ridKey) || !idSet.has(ridKey)) continue
        seen.add(ridKey)
        matched.push(row)
      }
    }
    sellOrderItemRows.value = matched
  } catch {
    sellOrderItemRows.value = []
  } finally {
    sellOrderItemLoading.value = false
  }
}

function receivableVerificationLabel(status: number) {
  if (status === 2) return t('financeReceivableList.verification.complete')
  if (status === 1) return t('financeReceivableList.verification.partial')
  return t('financeReceivableList.verification.pending')
}

function receivableVerificationTagType(status: number): 'success' | 'warning' | 'info' {
  if (status === 2) return 'success'
  if (status === 1) return 'warning'
  return 'info'
}

async function loadReceivables() {
  const id = stockOutId.value
  if (!id || !isSalesStockOut.value) {
    receivableRows.value = []
    return
  }
  receivableLoading.value = true
  try {
    receivableRows.value = await stockOutApi.getReceivables(id)
  } catch {
    receivableRows.value = []
  } finally {
    receivableLoading.value = false
  }
}

async function loadItems() {
  const code = detail.value?.stockOutCode?.trim()
  if (!code) {
    stockOutItems.value = []
    return
  }
  itemsLoading.value = true
  try {
    stockOutItems.value = await stockOutApi.searchItems({ stockOutCode: code })
  } catch {
    stockOutItems.value = []
  } finally {
    itemsLoading.value = false
  }
}

async function load() {
  const id = stockOutId.value
  if (!id) {
    loadError.value = t('stockOutDetail.notFound')
    customerWorkspacePanelStore.clear()
    return
  }
  loading.value = true
  loadError.value = ''
  try {
    await ensureLogisticsDict()
    const d = await stockOutApi.getById(id)
    if (!d) {
      detail.value = null
      loadError.value = t('stockOutDetail.notFound')
      receivableRows.value = []
      sellOrderItemRows.value = []
      stockOutItems.value = []
      customerWorkspacePanelStore.clear()
      return
    }
    detail.value = d
    bindCustomerWorkspace()
    syncEditFromDetail(d)
    await loadItems()
    await loadSellOrderItem()
    await loadReceivables()
  } catch {
    detail.value = null
    loadError.value = t('stockOutDetail.loadFailed')
    sellOrderItemRows.value = []
    receivableRows.value = []
    customerWorkspacePanelStore.clear()
  } finally {
    loading.value = false
  }
}

async function saveHeader() {
  const id = stockOutId.value
  const d = detail.value
  if (!id || !d) return
  if (!editForm.value.stockOutDate) {
    ElMessage.warning(t('stockOutDetail.needDate'))
    return
  }
  saving.value = true
  try {
    const dateIso = `${editForm.value.stockOutDate}T00:00:00.000Z`
    await stockOutApi.updateHeader(id, {
      stockOutDate: dateIso,
      shipmentMethod: editForm.value.shipmentMethod?.trim() || null,
      expressCompany: isExpressShipmentMethod(editForm.value.shipmentMethod)
        ? editForm.value.expressCompany?.trim() || null
        : null,
      courierTrackingNo: editForm.value.courierTrackingNo?.trim() || null
    })
    ElMessage.success(t('stockOutDetail.saveOk'))
    await load()
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.message || e?.message || t('stockOutDetail.saveFail'))
  } finally {
    saving.value = false
  }
}

function goBack() {
  router.push({ name: 'StockOutList' })
}

function goInvoiceReport() {
  const id = stockOutId.value
  if (!id) return
  router.push({ name: 'StockOutInvoiceReport', params: { id } })
}

onMounted(() => void load())

onBeforeUnmount(() => {
  customerWorkspacePanelStore.clear()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import '@/assets/styles/crm-quote-tier-dock.scss';

.amount-with-code {
  display: inline-flex;
  align-items: baseline;
  gap: 4px;
}

.mono-cell {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 12px;
}

.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;

  &.status-0 {
    background: rgba(255, 255, 255, 0.05);
    color: $text-muted;
  }

  &.status-1 {
    background: rgba(255, 193, 7, 0.15);
    color: #ffc107;
  }

  &.status-2 {
    background: rgba(70, 191, 145, 0.18);
    color: #46bf91;
  }

  &.status-3 {
    background: rgba(201, 87, 69, 0.18);
    color: #c95745;
  }

  &.status-4 {
    background: rgba(0, 212, 255, 0.18);
    color: $cyan-primary;
  }
}

.section-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 20px;
  height: 20px;
  padding: 0 6px;
  border-radius: 10px;
  font-size: 11px;
  font-weight: 600;
  color: $cyan-primary;
  background: rgba(0, 212, 255, 0.12);
  border: 1px solid rgba(0, 212, 255, 0.25);
}

.detail-panel-section-body {
  padding: 16px 20px 20px;
}

.stockout-detail-page {
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

.stockout-caption-title-group {
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

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;

  &--muted {
    opacity: 0.55;
  }
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

.stockout-header-meta-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  min-height: 28px;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
}

.header-left {
  min-width: 0;
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

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: var(--crm-detail-section-header-bg);

  .section-title {
    margin: 0;
    font-size: 14px;
    font-weight: 600;
    color: $text-primary;
  }
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

.info-grid--inline-labels .info-item {
  flex-direction: row;
  align-items: center;
  gap: 8px;
  padding: 12px 20px;

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

  &--control {
    :deep(.el-input),
    :deep(.el-select),
    :deep(.el-date-editor) {
      width: 100%;
    }
  }
}

.info-grid--editable .info-item {
  align-items: center;
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

.detail-items-table-wrap :deep(.items-table) {
  --el-table-border-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
}

.btn-secondary,
.btn-primary {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
}

.btn-secondary {
  border: 1px solid $border-panel;
  color: $text-secondary;
  background: rgba(255, 255, 255, 0.05);
}

.btn-primary {
  border: none;
  background: linear-gradient(135deg, #00a8cc, #0066cc);
  color: #fff;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.doc-hint {
  font-size: 12px;
  color: $text-muted;
  margin: 0 0 12px;
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

.cell-link {
  color: $cyan-primary;
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}
</style>
