<template>
  <div class="po-item-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('purchaseOrderItemList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('purchaseOrderItemList.totalCount', { total }) }}</div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="loadList">{{ t('purchaseOrderItemList.filters.refresh') }}</button>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          :range-separator="t('purchaseOrderItemList.filters.rangeSeparator')"
          :start-placeholder="t('purchaseOrderItemList.filters.orderCreatedFrom')"
          :end-placeholder="t('purchaseOrderItemList.filters.orderCreatedTo')"
          value-format="YYYY-MM-DD"
          class="filter-date-range po-date-range"
          clearable
          :teleported="false"
        />

        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.purchaseOrderCode"
            class="search-input"
            :placeholder="t('purchaseOrderItemList.filters.poCodePlaceholder')"
            @keyup.enter="runSearch"
          />
        </div>
        <template v-if="canViewVendor">
          <div class="search-input-wrap">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="filters.vendorName"
              class="search-input"
              :placeholder="t('purchaseOrderItemList.filters.vendorPlaceholder')"
              @keyup.enter="runSearch"
            />
          </div>
        </template>
        <template v-if="canViewPurchaseUser">
          <div class="search-input-wrap">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="filters.purchaseUserName"
              class="search-input"
              :placeholder="t('purchaseOrderItemList.filters.purchaserPlaceholder')"
              @keyup.enter="runSearch"
            />
          </div>
        </template>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.pn"
            class="search-input"
            :placeholder="t('purchaseOrderItemList.filters.pnPlaceholder')"
            @keyup.enter="runSearch"
          />
        </div>

        <el-select
          v-model="filters.orderType"
          :placeholder="t('purchaseOrderItemList.filters.allOrderTypes')"
          clearable
          class="po-order-type-select"
          :teleported="false"
          @change="onOrderTypeFilterChange"
        >
          <el-option :label="t('purchaseOrderItemList.filters.orderTypeCustomer')" :value="1" />
          <el-option :label="t('purchaseOrderItemList.filters.orderTypeStocking')" :value="2" />
          <el-option :label="t('purchaseOrderItemList.filters.orderTypeSample')" :value="3" />
        </el-select>

        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="runSearch">
          {{ t('purchaseOrderItemList.filters.search') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="resetFilters">
          {{ t('purchaseOrderItemList.filters.reset') }}
        </button>
      </div>
    </div>

    <CrmDataTable
      ref="tableRef"
      class="quantum-table-block el-table-host"
      column-layout-key="purchase-order-item-list-main"
      :columns="purchaseOrderItemColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="tableRows"
      v-loading="loading"
      row-key="purchaseOrderItemId"
      @selection-change="onSelectionChange"
      @row-dblclick="onPurchaseOrderItemRowDblClick"
    >
      <template #col-purchaseOrderItemCode="{ row }">
        <span class="po-line-code-with-badge">
          <span>{{ row.purchaseOrderItemCode }}</span>
          <el-tooltip
            v-if="isLineStockingPurchase(row)"
            :content="t('purchaseOrderItemList.filters.orderTypeStocking')"
            placement="top"
          >
            <el-tag type="warning" effect="plain" size="small" class="po-stocking-tag" round>
              {{ t('purchaseOrderItemList.filters.stockingTag') }}
            </el-tag>
          </el-tooltip>
        </span>
      </template>
      <template #col-itemStatus="{ row }">
        <el-tag effect="dark" :type="statusTagType(row.itemStatus)" size="small">{{ statusText(row.itemStatus) }}</el-tag>
      </template>
      <template #col-orderCreateTime="{ row }">{{ formatDt(row.orderCreateTime) }}</template>
      <template #col-paymentProgressStatus="{ row }">
        <el-tag effect="dark" size="small" :type="poExtendTriTagType(Number(row.paymentProgressStatus ?? 0))">
          {{ poPaymentProgressText(Number(row.paymentProgressStatus ?? 0)) }}
        </el-tag>
      </template>
      <template #col-purchaseProgressStatus="{ row }">
        <el-tag effect="dark" size="small" :type="poExtendTriTagType(Number(row.purchaseProgressStatus ?? 0))">
          {{ poPurchaseProgressText(Number(row.purchaseProgressStatus ?? 0)) }}
        </el-tag>
      </template>
      <template #col-stockInProgressStatus="{ row }">
        <el-tag effect="dark" size="small" :type="poExtendTriTagType(Number(row.stockInProgressStatus ?? 0))">
          {{ poStockInProgressText(Number(row.stockInProgressStatus ?? 0)) }}
        </el-tag>
      </template>
      <template #col-invoiceProgressStatus="{ row }">
        <el-tag effect="dark" size="small" :type="poExtendTriTagType(Number(row.invoiceProgressStatus ?? 0))">
          {{ poInvoiceProgressText(Number(row.invoiceProgressStatus ?? 0)) }}
        </el-tag>
      </template>
      <template #col-cost="{ row }">
        <span class="amount-with-code">
          <span>{{ formatUnitPriceNumber(row.cost) }}</span>
          <span v-if="formatUnitPriceNumber(row.cost) !== '—'" :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">
            {{ listAmountCurrencyIso(row.currency) }}
          </span>
        </span>
      </template>
      <template #col-lineTotal="{ row }">
        <span class="amount-with-code">
          <span>{{ formatTotalAmountNumber(row.lineTotal) }}</span>
          <span v-if="formatTotalAmountNumber(row.lineTotal) !== '—'" :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">
            {{ listAmountCurrencyIso(row.currency) }}
          </span>
        </span>
      </template>
      <template #col-createTime="{ row }">{{ formatDt(row.createTime || row.orderCreateTime) }}</template>
      <template #col-createUser="{ row }">{{ row.createUserName || row.createdBy || row.purchaseUserName || '—' }}</template>
      <template #col-actions-header>
          <div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="opColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="toggleOpCol"
            >
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns">
            <el-button link type="primary" size="small" @click.stop="goDetail(row)">
              {{ t('purchaseOrderItemList.actions.detail') }}
            </el-button>
            <el-button
              v-if="row.itemStatus === 30 && canCreateArrivalNotice"
              link
              type="warning"
              size="small"
              @click.stop="openArrivalDialog(row)"
            >
              {{ t('purchaseOrderItemList.actions.notifyArrival') }}
            </el-button>
            <el-button
              v-if="row.canApplyPayment"
              link
              type="warning"
              size="small"
              @click.stop="openPaymentDialog(row)"
            >
              {{ t('purchaseOrderItemList.actions.applyPayment') }}
            </el-button>
          </div>

          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="goDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('purchaseOrderItemList.actions.detail') }}</span>
                </el-dropdown-item>
                <el-dropdown-item
                  v-if="row.itemStatus === 30 && canCreateArrivalNotice"
                  @click.stop="openArrivalDialog(row)"
                >
                  <span class="op-more-item op-more-item--warning">{{ t('purchaseOrderItemList.actions.notifyArrival') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="row.canApplyPayment" @click.stop="openPaymentDialog(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('purchaseOrderItemList.actions.applyPayment') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>

    <div v-if="total > 0" class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('purchaseOrderItemList.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('purchaseOrderItemList.columnSettings')"
            @click="tableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
      <el-pagination
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, prev, pager, next, sizes"
        class="quantum-pagination"
        @current-change="onPageChange"
        @size-change="onPageSizeChange"
      />
    </div>

    <div v-if="poItemLinePanel.visible && !maskPurchaseSensitiveFields" class="so-item-line-detail-panel">
      <div class="so-item-line-detail-panel__head">
        <span class="so-item-line-detail-panel__title">{{ t('purchaseOrderItemList.lineDetailPanel.title') }}</span>
        <span class="so-item-line-detail-panel__code">{{ poItemLinePanel.purchaseOrderItemCode || '—' }}</span>
        <button type="button" class="so-item-line-detail-panel__close" @click="closePoItemLinePanel">
          {{ t('purchaseOrderItemList.lineDetailPanel.close') }}
        </button>
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
              :class="{ 'tab-btn--active': poItemLinePanel.activeTab === 'requisitions' }"
              @click="poItemLinePanel.activeTab = 'requisitions'"
            >
              采购申请
            </button>
            <button
              type="button"
              class="tab-btn"
              :class="{ 'tab-btn--active': poItemLinePanel.activeTab === 'payments' }"
              @click="poItemLinePanel.activeTab = 'payments'"
            >
              付款
            </button>
            <button
              type="button"
              class="tab-btn"
              :class="{ 'tab-btn--active': poItemLinePanel.activeTab === 'arrivals' }"
              @click="poItemLinePanel.activeTab = 'arrivals'"
            >
              到货通知
            </button>
            <button
              type="button"
              class="tab-btn"
              :class="{ 'tab-btn--active': poItemLinePanel.activeTab === 'stockIns' }"
              @click="poItemLinePanel.activeTab = 'stockIns'"
            >
              入库
            </button>
            <button
              type="button"
              class="tab-btn"
              :class="{ 'tab-btn--active': poItemLinePanel.activeTab === 'stocks' }"
              @click="poItemLinePanel.activeTab = 'stocks'"
            >
              库存
            </button>
            <button
              type="button"
              class="tab-btn"
              :class="{ 'tab-btn--active': poItemLinePanel.activeTab === 'purchaseInvoices' }"
              @click="poItemLinePanel.activeTab = 'purchaseInvoices'"
            >
              进项发票
            </button>
          </div>
          <div class="tabs-body">
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
                  <template #default="{ row }">{{ poDetailPrStatusText(row?.status) }}</template>
                </el-table-column>
                <el-table-column prop="pn" label="PN" min-width="140" show-overflow-tooltip />
                <el-table-column prop="brand" label="品牌" width="120" show-overflow-tooltip />
                <el-table-column prop="qty" label="数量" width="100" align="right" />
                <el-table-column label="预计采购" width="160">
                  <template #default="{ row }">{{ poAggFormatDt(row?.expectedPurchaseTime) }}</template>
                </el-table-column>
              </el-table>
              <el-empty v-else :description="t('purchaseOrderItemList.lineDetailPanel.empty')" :image-size="64" />
            </div>
            <div v-show="poItemLinePanel.activeTab === 'payments'" class="po-aggregate-table-wrap">
              <el-table v-if="(lineTabAggregates?.payments?.length ?? 0) > 0" :data="lineTabAggregates?.payments ?? []" size="small" stripe>
                <el-table-column type="index" width="50" label="#" />
                <el-table-column label="付款单号" min-width="180">
                  <template #default="{ row }">
                    <router-link class="po-tab-link" :to="`/finance/payments/${row.id}`">{{ row.financePaymentCode }}</router-link>
                  </template>
                </el-table-column>
                <el-table-column prop="vendorName" label="供应商" min-width="160" show-overflow-tooltip />
                <el-table-column label="状态" width="110">
                  <template #default="{ row }">{{ poDetailPaymentStatusText(row?.status) }}</template>
                </el-table-column>
                <el-table-column v-if="canViewAmount" label="待付金额" width="130" align="right">
                  <template #default="{ row }">{{ formatTotalAmountNumber(row?.paymentAmountToBe) }}</template>
                </el-table-column>
                <el-table-column v-if="canViewAmount" label="已付金额" width="130" align="right">
                  <template #default="{ row }">{{ formatTotalAmountNumber(row?.paymentAmount) }}</template>
                </el-table-column>
                <el-table-column label="付款日期" width="160">
                  <template #default="{ row }">{{ poAggFormatDt(row?.paymentDate) }}</template>
                </el-table-column>
              </el-table>
              <el-empty v-else :description="t('purchaseOrderItemList.lineDetailPanel.empty')" :image-size="64" />
            </div>
            <div v-show="poItemLinePanel.activeTab === 'arrivals'" class="po-aggregate-table-wrap">
              <el-table v-if="(lineTabAggregates?.arrivalNotices?.length ?? 0) > 0" :data="lineTabAggregates?.arrivalNotices ?? []" size="small" stripe>
                <el-table-column type="index" width="50" label="#" />
                <el-table-column prop="noticeCode" label="通知单号" min-width="180" show-overflow-tooltip />
                <el-table-column prop="pn" label="PN" min-width="140" show-overflow-tooltip />
                <el-table-column prop="brand" label="品牌" width="120" show-overflow-tooltip />
                <el-table-column prop="expectQty" label="预计数量" width="100" align="right" />
                <el-table-column prop="receiveQty" label="已收数量" width="100" align="right" />
                <el-table-column label="状态" width="120">
                  <template #default="{ row }">{{ poDetailArrivalStatusText(row?.status) }}</template>
                </el-table-column>
              </el-table>
              <el-empty v-else :description="t('purchaseOrderItemList.lineDetailPanel.empty')" :image-size="64" />
            </div>
            <div v-show="poItemLinePanel.activeTab === 'stockIns'" class="po-aggregate-table-wrap">
              <el-table v-if="(lineTabAggregates?.stockIns?.length ?? 0) > 0" :data="lineTabAggregates?.stockIns ?? []" size="small" stripe>
                <el-table-column type="index" width="50" label="#" />
                <el-table-column label="入库单号" min-width="180">
                  <template #default="{ row }">
                    <router-link class="po-tab-link" :to="`/inventory/stock-in/${row.id}`">{{ row.stockInCode }}</router-link>
                  </template>
                </el-table-column>
                <el-table-column label="类型" width="100">
                  <template #default="{ row }">{{ poDetailStockInTypeText(row?.stockInType) }}</template>
                </el-table-column>
                <el-table-column label="状态" width="100">
                  <template #default="{ row }">{{ poDetailStockInStatusText(row?.status) }}</template>
                </el-table-column>
                <el-table-column label="入库日期" width="160">
                  <template #default="{ row }">{{ poAggFormatDt(row?.stockInDate) }}</template>
                </el-table-column>
              </el-table>
              <el-empty v-else :description="t('purchaseOrderItemList.lineDetailPanel.empty')" :image-size="64" />
            </div>
            <div v-show="poItemLinePanel.activeTab === 'stocks'" class="po-aggregate-table-wrap">
              <el-table v-if="(lineTabAggregates?.stockItems?.length ?? 0) > 0" :data="lineTabAggregates?.stockItems ?? []" size="small" stripe>
                <el-table-column type="index" width="50" label="#" />
                <el-table-column label="在库明细" min-width="200">
                  <template #default="{ row }">
                    <router-link class="po-tab-link" :to="`/inventory/stocks/${row.stockAggregateId}`">{{ row.stockItemCode || row.id }}</router-link>
                  </template>
                </el-table-column>
                <el-table-column prop="purchaseOrderItemCode" label="采购明细号" min-width="140" show-overflow-tooltip />
                <el-table-column prop="purchasePn" label="PN" min-width="140" show-overflow-tooltip />
                <el-table-column prop="purchaseBrand" label="品牌" width="120" show-overflow-tooltip />
                <el-table-column prop="qtyRepertory" label="现存量" width="100" align="right" />
                <el-table-column prop="qtyRepertoryAvailable" label="可用量" width="100" align="right" />
              </el-table>
              <el-empty v-else :description="t('purchaseOrderItemList.lineDetailPanel.empty')" :image-size="64" />
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
                <el-table-column v-if="canViewAmount" label="发票金额" width="120" align="right">
                  <template #default="{ row }">{{ formatTotalAmountNumber(row?.invoiceAmount) }}</template>
                </el-table-column>
                <el-table-column label="认证状态" width="100">
                  <template #default="{ row }">{{ Number(row?.confirmStatus) === 1 ? '已认证' : '未认证' }}</template>
                </el-table-column>
                <el-table-column label="开票日期" width="160">
                  <template #default="{ row }">{{ poAggFormatDt(row?.invoiceDate) }}</template>
                </el-table-column>
              </el-table>
              <el-empty v-else :description="t('purchaseOrderItemList.lineDetailPanel.empty')" :image-size="64" />
            </div>
          </div>
        </div>
      </div>
    </div>

    <el-dialog
      v-model="paymentDialogVisible"
      :title="t('purchaseOrderItemList.paymentDialog.title')"
      width="min(96vw, 1440px)"
      destroy-on-close
      class="payment-dialog"
    >
      <el-form label-width="120px">
        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item :label="t('purchaseOrderItemList.paymentDialog.vendorInfo')">
              <el-input :model-value="maskPurchaseSensitiveFields ? '—' : (paymentForm.vendorName || '--')" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('purchaseOrderItemList.paymentDialog.purchaser')">
              <el-input :model-value="paymentForm.purchaseUserName || '--'" disabled />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item :label="t('purchaseOrderItemList.paymentDialog.vendorBank')" required>
              <el-select
                v-model="paymentForm.vendorBankId"
                :placeholder="t('purchaseOrderItemList.paymentDialog.vendorBankPlaceholder')"
                filterable
                style="width: 100%"
              >
                <el-option
                  v-for="b in paymentBankOptions"
                  :key="b.id"
                  :label="b.bankName"
                  :value="b.id"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('purchaseOrderItemList.paymentDialog.paymentMode')" required>
              <el-select v-model="paymentForm.paymentMode" style="width: 100%">
                <el-option :label="t('purchaseOrderItemList.paymentDialog.modeWire')" :value="1" />
                <el-option :label="t('purchaseOrderItemList.paymentDialog.modeCash')" :value="2" />
                <el-option :label="t('purchaseOrderItemList.paymentDialog.modeCheck')" :value="3" />
                <el-option :label="t('purchaseOrderItemList.paymentDialog.modeAcceptance')" :value="4" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item :label="t('purchaseOrderItemList.paymentDialog.remark')">
          <el-input v-model="paymentForm.remark" type="textarea" :rows="2" />
        </el-form-item>

        <div class="section-title">{{ t('purchaseOrderItemList.paymentDialog.feeSection') }}</div>
        <el-row :gutter="12">
          <el-col :span="8">
            <el-form-item :label="t('purchaseOrderItemList.paymentDialog.intermediateBankFee')">
              <SettlementCurrencyAmountInput
                v-model="paymentForm.fee.intermediateBankFee"
                v-model:currency="paymentForm.currency"
                :min="0"
                :precision="2"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('purchaseOrderItemList.paymentDialog.bankCharge')">
              <SettlementCurrencyAmountInput
                v-model="paymentForm.fee.bankCharge"
                v-model:currency="paymentForm.currency"
                :min="0"
                :precision="2"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('purchaseOrderItemList.paymentDialog.freight')">
              <SettlementCurrencyAmountInput
                v-model="paymentForm.fee.freight"
                v-model:currency="paymentForm.currency"
                :min="0"
                :precision="2"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('purchaseOrderItemList.paymentDialog.miscFee')">
              <SettlementCurrencyAmountInput
                v-model="paymentForm.fee.miscFee"
                v-model:currency="paymentForm.currency"
                :min="0"
                :precision="2"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('purchaseOrderItemList.paymentDialog.rounding')">
              <SettlementCurrencyAmountInput
                v-model="paymentForm.fee.rounding"
                v-model:currency="paymentForm.currency"
                :precision="2"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('purchaseOrderItemList.paymentDialog.feePayer')">
              <el-radio-group v-model="paymentForm.fee.intermediateBankFeePayer">
                <el-radio label="我方">{{ t('purchaseOrderItemList.paymentDialog.payerUs') }}</el-radio>
                <el-radio label="供应商">{{ t('purchaseOrderItemList.paymentDialog.payerVendor') }}</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>

        <div class="section-title">{{ t('purchaseOrderItemList.paymentDialog.sectionLines') }}</div>
        <CrmDataTable :data="paymentForm.lines" size="small">
          <el-table-column
            prop="purchaseOrderCode"
            :label="t('purchaseOrderItemList.paymentDialog.colPoCode')"
            width="160"
            min-width="160"
            show-overflow-tooltip
          />
          <el-table-column prop="pn" :label="t('purchaseOrderItemList.paymentDialog.colPn')" min-width="120" />
          <el-table-column prop="brand" :label="t('purchaseOrderItemList.paymentDialog.colBrand')" width="100" />
          <el-table-column prop="qty" :label="t('purchaseOrderItemList.paymentDialog.colQty')" width="90" align="right" />
          <el-table-column prop="cost" :label="t('purchaseOrderItemList.paymentDialog.colUnitPrice')" width="160" align="right">
            <template #default="{ row }">{{ formatCurrencyUnitPrice(row.cost, row.currency) }}</template>
          </el-table-column>
          <el-table-column prop="alreadyRequested" :label="t('purchaseOrderItemList.paymentDialog.colAlreadyRequested')" width="160" align="right">
            <template #default="{ row }">{{ formatCurrencyTotal(row.alreadyRequested, row.currency) }}</template>
          </el-table-column>
          <el-table-column prop="pendingRequested" :label="t('purchaseOrderItemList.paymentDialog.colPending')" width="160" align="right">
            <template #default="{ row }">{{ formatCurrencyTotal(row.pendingRequested, row.currency) }}</template>
          </el-table-column>
          <el-table-column :label="t('purchaseOrderItemList.paymentDialog.colThisRequest')" min-width="220" width="220">
            <template #default="{ row }">
              <SettlementCurrencyAmountInput
                v-model="row.requestAmount"
                v-model:currency="paymentForm.currency"
                :min="0"
                :max="paymentRequestAmountMax(row)"
                :precision="2"
              />
            </template>
          </el-table-column>
          <el-table-column :label="t('purchaseOrderItemList.paymentDialog.colLineRemark')" min-width="140">
            <template #default="{ row }">
              <el-input v-model="row.remark" />
            </template>
          </el-table-column>
        </CrmDataTable>

        <el-alert :closable="false" type="info" style="margin-top: 8px">
          <template #title>
            {{
              t('purchaseOrderItemList.paymentDialog.totalAlert', {
                amount: formatCurrencyTotal(paymentTotalAmount, paymentForm.currency)
              })
            }}
          </template>
        </el-alert>
      </el-form>

      <template #footer>
        <el-button @click="paymentDialogVisible = false">{{ t('purchaseOrderItemList.paymentDialog.cancel') }}</el-button>
        <el-button type="primary" :loading="paymentSubmitting" @click="submitPayment()">
          {{ t('purchaseOrderItemList.paymentDialog.submit') }}
        </el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="arrivalDialogVisible"
      :title="t('purchaseOrderItemList.arrivalDialog.title')"
      width="1180px"
      align-center
      destroy-on-close
    >
      <div class="arrival-form-layout">

        <div class="arrival-section">
          <el-form label-width="120px" class="arrival-notice-form">
            <el-row :gutter="12">
              <el-col :span="8">
                <el-form-item :label="t('purchaseOrderItemList.arrivalDialog.code')"><el-input v-model="arrivalForm.purchaseOrderCode" /></el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item :label="t('purchaseOrderItemList.arrivalDialog.expectedArrival')" required>
                  <el-date-picker
                    v-model="arrivalForm.expectedArrivalDate"
                    type="date"
                    value-format="YYYY-MM-DD"
                    :placeholder="t('purchaseOrderItemList.arrivalDialog.expectedArrivalPlaceholder')"
                    style="width: 100%"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item :label="t('purchaseOrderItemList.arrivalDialog.companyName')">
                  <el-input v-if="!maskPurchaseSensitiveFields" v-model="arrivalForm.companyName" />
                  <el-input v-else model-value="—" disabled />
                </el-form-item>
              </el-col>
            </el-row>
            <el-row :gutter="12">
              <el-col :span="6">
                <el-form-item :label="t('purchaseOrderItemList.arrivalDialog.regionType')">
                  <el-select
                    :model-value="normalizeRegionType(arrivalForm.regionType)"
                    :teleported="false"
                    disabled
                    style="width: 100%"
                    @update:model-value="(v: string | number) => { arrivalForm.regionType = normalizeRegionType(v) }"
                  >
                    <el-option :value="REGION_TYPE_DOMESTIC" :label="t('inventoryList.warehouse.regionDomestic')" />
                    <el-option :value="REGION_TYPE_OVERSEAS" :label="t('inventoryList.warehouse.regionOverseas')" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="18"><el-form-item :label="t('purchaseOrderItemList.arrivalDialog.address')"><el-input v-model="arrivalForm.address" /></el-form-item></el-col>
            </el-row>
            <el-row :gutter="12">
              <el-col :span="12"><el-form-item :label="t('purchaseOrderItemList.arrivalDialog.phone')"><el-input v-model="arrivalForm.phone" /></el-form-item></el-col>
              <el-col :span="12"><el-form-item :label="t('purchaseOrderItemList.arrivalDialog.contact')"><el-input v-model="arrivalForm.contact" /></el-form-item></el-col>
            </el-row>
            <el-row :gutter="12">
              <el-col :span="8">
                <el-form-item :label="t('purchaseOrderItemList.arrivalDialog.arrivalMethod')">
                  <el-select
                    v-model="arrivalForm.arrivalMethod"
                    clearable
                    filterable
                    :placeholder="t('purchaseOrderItemList.arrivalDialog.selectPlaceholder')"
                    style="width: 100%"
                  >
                    <el-option
                      v-for="o in arrivalMethodDictOptions"
                      :key="o.value"
                      :label="o.label"
                      :value="o.value"
                    />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item :label="t('purchaseOrderItemList.arrivalDialog.expressMethod')">
                  <el-select
                    v-model="arrivalForm.expressMethod"
                    clearable
                    filterable
                    :placeholder="t('purchaseOrderItemList.arrivalDialog.selectPlaceholder')"
                    style="width: 100%"
                  >
                    <el-option
                      v-for="o in expressMethodDictOptions"
                      :key="o.value"
                      :label="o.label"
                      :value="o.value"
                    />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="8"><el-form-item :label="t('purchaseOrderItemList.arrivalDialog.expressNo')"><el-input v-model="arrivalForm.expressNo" /></el-form-item></el-col>
            </el-row>
          </el-form>
        </div>

        <div class="arrival-section">
          <div class="section-title">{{ t('purchaseOrderItemList.arrivalDialog.sectionLines') }}</div>
          <CrmDataTable :data="arrivalForm.lines" size="small">
            <el-table-column :label="t('purchaseOrderItemList.arrivalDialog.seq')" width="70">
              <template #default="{ $index }">{{ $index + 1 }}</template>
            </el-table-column>
            <el-table-column :label="t('purchaseOrderItemList.arrivalDialog.factoryPn')" min-width="180">
              <template #default="{ row }"><el-input v-model="row.pn" /></template>
            </el-table-column>
            <el-table-column :label="t('purchaseOrderItemList.arrivalDialog.brand')" width="120">
              <template #default="{ row }"><el-input v-model="row.brand" /></template>
            </el-table-column>
            <el-table-column :label="t('purchaseOrderItemList.arrivalDialog.qty')" min-width="168" align="right">
              <template #default="{ row }">
                <el-input-number
                  v-model="row.qty"
                  :min="0"
                  :precision="0"
                  :step="1"
                  class="arrival-qty-input"
                  controls-position="right"
                />
              </template>
            </el-table-column>
            <el-table-column :label="t('purchaseOrderItemList.arrivalDialog.spec')" min-width="130">
              <template #default="{ row }"><el-input v-model="row.spec" /></template>
            </el-table-column>
            <el-table-column :label="t('purchaseOrderItemList.arrivalDialog.packaging')" width="120">
              <template #default="{ row }"><el-input v-model="row.packaging" /></template>
            </el-table-column>
          </CrmDataTable>
        </div>

        <div class="arrival-section">
          <el-form label-width="90px" class="arrival-notice-form">
            <el-form-item :label="t('purchaseOrderItemList.arrivalDialog.inspection')"><el-input v-model="arrivalForm.inspectionRequirement" /></el-form-item>
            <el-form-item :label="t('purchaseOrderItemList.arrivalDialog.remark')"><el-input v-model="arrivalForm.remark" type="textarea" :rows="2" /></el-form-item>
          </el-form>
        </div>

        <!-- 新建到货通知不展示签收/质检/入库；后续若支持编辑已存在通知可改为 v-if="arrivalNoticeShowProcessFields" -->
        <div v-if="arrivalNoticeShowProcessFields" class="arrival-section">
          <el-form label-width="120px" class="arrival-notice-form">
            <el-row :gutter="12">
              <el-col :span="6"><el-form-item :label="t('purchaseOrderItemList.arrivalDialog.signer')"><el-input v-model="arrivalForm.signer" /></el-form-item></el-col>
              <el-col :span="6"><el-form-item :label="t('purchaseOrderItemList.arrivalDialog.signDate')"><el-date-picker v-model="arrivalForm.signDate" type="date" value-format="YYYY-MM-DD" style="width:100%" /></el-form-item></el-col>
              <el-col :span="6"><el-form-item :label="t('purchaseOrderItemList.arrivalDialog.qcUser')"><el-input v-model="arrivalForm.qcUser" /></el-form-item></el-col>
              <el-col :span="6"><el-form-item :label="t('purchaseOrderItemList.arrivalDialog.qcDate')"><el-date-picker v-model="arrivalForm.qcDate" type="date" value-format="YYYY-MM-DD" style="width:100%" /></el-form-item></el-col>
            </el-row>
            <el-row :gutter="12">
              <el-col :span="6"><el-form-item :label="t('purchaseOrderItemList.arrivalDialog.stockInUser')"><el-input v-model="arrivalForm.stockInUser" /></el-form-item></el-col>
              <el-col :span="6"><el-form-item :label="t('purchaseOrderItemList.arrivalDialog.stockInDate')"><el-date-picker v-model="arrivalForm.stockInDate" type="date" value-format="YYYY-MM-DD" style="width:100%" /></el-form-item></el-col>
            </el-row>
          </el-form>
        </div>
      </div>
      <template #footer>
        <el-button @click="arrivalDialogVisible = false">{{ t('purchaseOrderItemList.arrivalDialog.cancel') }}</el-button>
        <el-button type="primary" :loading="arrivalSubmitting" @click="submitArrivalNotice">{{ t('purchaseOrderItemList.arrivalDialog.confirm') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { Setting } from '@element-plus/icons-vue'
import { purchaseOrderApi, type PurchaseOrderDetailTabAggregates } from '@/api/purchaseOrder'
import { financePaymentApi } from '@/api/finance'
import { logisticsApi } from '@/api/logistics'
import { ElMessage } from 'element-plus'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import {
  formatCurrencyTotal,
  formatCurrencyUnitPrice,
  formatTotalAmountNumber,
  formatUnitPriceNumber,
  listAmountCurrencyDockClass,
  listAmountCurrencyIso
} from '@/utils/moneyFormat'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import SettlementCurrencyAmountInput from '@/components/SettlementCurrencyAmountInput.vue'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { REGION_TYPE_DOMESTIC, REGION_TYPE_OVERSEAS, normalizeRegionType } from '@/constants/regionType'
import { CurrencyCode } from '@/constants/currency'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useFinancePaymentBankOptions } from '@/composables/useFinancePaymentBankOptions'
import { getApiErrorMessage } from '@/utils/apiError'

const router = useRouter()
const route = useRoute()
const { t, locale } = useI18n()
const authStore = useAuthStore()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { paymentBankOptions, loadPaymentBankOptions } = useFinancePaymentBankOptions()

const lineTabAggregates = ref<PurchaseOrderDetailTabAggregates | null>(null)
const poItemLinePanel = reactive({
  visible: false,
  purchaseOrderItemId: '',
  purchaseOrderItemCode: '',
  activeTab: 'requisitions' as
    | 'requisitions'
    | 'payments'
    | 'arrivals'
    | 'stockIns'
    | 'stocks'
    | 'purchaseInvoices',
  loading: false,
  loadError: ''
})

function closePoItemLinePanel() {
  poItemLinePanel.visible = false
  poItemLinePanel.loadError = ''
  lineTabAggregates.value = null
}

watch(maskPurchaseSensitiveFields, (m) => {
  if (m) closePoItemLinePanel()
})

async function onPurchaseOrderItemRowDblClick(row: Record<string, unknown>) {
  if (maskPurchaseSensitiveFields.value) return
  const purchaseOrderId = String(row?.purchaseOrderId ?? '').trim()
  const purchaseOrderItemId = String(row?.purchaseOrderItemId ?? '').trim()
  const purchaseOrderItemCode = String(row?.purchaseOrderItemCode ?? '').trim()
  if (!purchaseOrderId || !purchaseOrderItemId) return
  poItemLinePanel.purchaseOrderItemId = purchaseOrderItemId
  poItemLinePanel.purchaseOrderItemCode = purchaseOrderItemCode || purchaseOrderItemId
  poItemLinePanel.visible = true
  poItemLinePanel.activeTab = 'requisitions'
  poItemLinePanel.loading = true
  poItemLinePanel.loadError = ''
  lineTabAggregates.value = null
  try {
    lineTabAggregates.value = await purchaseOrderApi.getPurchaseOrderItemDetailTabAggregates(
      purchaseOrderId,
      purchaseOrderItemId
    )
  } catch (e: unknown) {
    poItemLinePanel.loadError = getApiErrorMessage(e, '加载明细关联数据失败')
  } finally {
    poItemLinePanel.loading = false
  }
}

function poAggFormatDt(v?: string | null) {
  if (!v) return '—'
  const s = formatDisplayDateTime(v)
  return s === '--' ? '—' : s
}

function poDetailPrStatusText(v?: number) {
  const map: Record<number, string> = { 0: '新建', 1: '部分完成', 2: '全部完成', 3: '已取消' }
  return map[Number(v)] ?? '—'
}

function poDetailPaymentStatusText(v?: number) {
  const map: Record<number, string> = {
    1: '新建',
    2: '待审核',
    10: '审核通过',
    100: '付款完成',
    [-1]: '审核失败',
    [-2]: '已取消'
  }
  return map[Number(v)] ?? '—'
}

function poDetailArrivalStatusText(v?: number) {
  const map: Record<number, string> = { 10: '未到货', 20: '到货待检', 30: '已质检', 100: '已入库', 1: '新建' }
  return map[Number(v)] ?? '—'
}

function poDetailStockInTypeText(v?: number) {
  const map: Record<number, string> = { 1: '采购入库', 2: '退货入库', 3: '调拨入库', 4: '其他入库' }
  return map[Number(v)] ?? '—'
}

function poDetailStockInStatusText(v?: number) {
  const map: Record<number, string> = { 0: '草稿', 1: '待入库', 2: '已入库', 3: '已取消' }
  return map[Number(v)] ?? '—'
}

const { ensureLoaded: ensureLogisticsDict, arrivalOptions: arrivalMethodDictOptions, expressOptions: expressMethodDictOptions } =
  useLogisticsFormDict()

/** 与采购订单列表/详情一致：脱敏时不得展示供应商检索与列 */
const canViewVendor = computed(
  () =>
    !maskPurchaseSensitiveFields.value &&
    (authStore.hasPermission('vendor.info.read') ||
      authStore.hasPermission('vendor.read') ||
      authStore.hasPermission('purchase-order.read') ||
      authStore.hasPermission('purchase-order.write'))
)
const canViewPurchaseUser = computed(() => authStore.hasPermission('purchase.user.read') || authStore.hasPermission('purchase-order.read'))
const canViewAmount = computed(
  () => !maskPurchaseSensitiveFields.value && authStore.hasPermission('purchase.amount.read')
)
const canCreateArrivalNotice = computed(() => authStore.hasPermission('purchase-order.read'))

const loading = ref(false)
/** 当前页明细行（服务端分页） */
const tableRows = ref<any[]>([])

const total = ref(0)
const page = ref(1)
const pageSize = ref(20)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 173
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() =>
  opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH
)
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const purchaseOrderItemColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  const cols: CrmTableColumnDef[] = [
    { key: 'selection', type: 'selection', width: 48, reserveSelection: true, fixed: 'left', hideable: false, reorderable: false },
    {
      key: 'purchaseOrderItemCode',
      label: t('purchaseOrderItemList.columns.purchaseOrderItemCode'),
      prop: 'purchaseOrderItemCode',
      width: 180,
      minWidth: 168,
      fixed: 'left',
      showOverflowTooltip: true
    },
    {
      key: 'purchaseOrderCode',
      label: t('purchaseOrderItemList.columns.purchaseOrderCode'),
      prop: 'purchaseOrderCode',
      width: 160,
      minWidth: 160,
      showOverflowTooltip: true
    },
    { key: 'itemStatus', label: t('purchaseOrderItemList.columns.itemStatus'), prop: 'itemStatus', width: 160, align: 'center' },
    { key: 'orderCreateTime', label: t('purchaseOrderItemList.columns.orderCreateTime'), prop: 'orderCreateTime', width: 160 }
  ]
  if (canViewVendor.value) {
    cols.push({
      key: 'vendorName',
      label: t('purchaseOrderItemList.columns.vendorName'),
      prop: 'vendorName',
      minWidth: 200,
      showOverflowTooltip: true
    })
  }
  if (canViewPurchaseUser.value) {
    cols.push({
      key: 'purchaseUserName',
      label: t('purchaseOrderItemList.columns.purchaseUserName'),
      prop: 'purchaseUserName',
      width: 100,
      showOverflowTooltip: true
    })
  }
  cols.push(
    { key: 'pn', label: t('purchaseOrderItemList.columns.pn'), prop: 'pn', minWidth: 130, showOverflowTooltip: true },
    { key: 'brand', label: t('purchaseOrderItemList.columns.brand'), prop: 'brand', width: 110, showOverflowTooltip: true },
    { key: 'qty', label: t('purchaseOrderItemList.columns.qty'), prop: 'qty', width: 100, align: 'right' }
  )
  if (canViewAmount.value) {
    cols.push(
      { key: 'cost', label: t('purchaseOrderItemList.columns.cost'), prop: 'cost', width: 160, align: 'right' },
      { key: 'lineTotal', label: t('purchaseOrderItemList.columns.lineTotal'), prop: 'lineTotal', width: 160, align: 'right' }
    )
  }
  cols.push(
    { key: 'createTime', label: t('purchaseOrderItemList.columns.createTime'), width: 160 },
    { key: 'createUser', label: t('purchaseOrderItemList.columns.createUser'), width: 120, showOverflowTooltip: true },
    {
      key: 'paymentProgressStatus',
      label: t('purchaseOrderItemList.columns.paymentProgressStatus'),
      prop: 'paymentProgressStatus',
      width: 120,
      align: 'center'
    },
    {
      key: 'purchaseProgressStatus',
      label: t('purchaseOrderItemList.columns.purchaseProgressStatus'),
      prop: 'purchaseProgressStatus',
      width: 120,
      align: 'center'
    },
    {
      key: 'stockInProgressStatus',
      label: t('purchaseOrderItemList.columns.stockInProgressStatus'),
      prop: 'stockInProgressStatus',
      width: 120,
      align: 'center'
    },
    {
      key: 'invoiceProgressStatus',
      label: t('purchaseOrderItemList.columns.invoiceProgressStatus'),
      prop: 'invoiceProgressStatus',
      width: 120,
      align: 'center'
    }
  )
  cols.push({
    key: 'actions',
    label: t('purchaseOrderItemList.columns.actions'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    align: 'center',
    hideable: false,
    pinned: 'end',
    reorderable: false,
    className: 'op-col',
    labelClassName: 'op-col',
    resizable: false
  })
  return cols
})

const tableRef = ref<any>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const selectedRows = ref<any[]>([])
const paymentDialogVisible = ref(false)
const paymentSubmitting = ref(false)
const arrivalDialogVisible = ref(false)
/** 新建为 false；若以后支持「编辑到货通知」并需填写签收/质检/入库，可置为 true */
const arrivalNoticeShowProcessFields = ref(false)
const arrivalSubmitting = ref(false)
const arrivalForm = reactive<any>({
  purchaseOrderItemId: '',
  purchaseOrderId: '',
  purchaseOrderCode: '',
  vendorName: '',
  pn: '',
  expectedArrivalDate: '' as string,
  companyName: '',
  address: '',
  phone: '',
  contact: '',
  arrivalMethod: '',
  expressMethod: '',
  expressNo: '',
  regionType: REGION_TYPE_DOMESTIC as number,
  inspectionRequirement: '',
  remark: '',
  signer: '',
  signDate: '',
  qcUser: '',
  qcDate: '',
  stockInUser: '',
  stockInDate: '',
  lines: [] as any[]
})

const paymentForm = reactive<any>({
  vendorId: '',
  vendorName: '',
  purchaseUserName: '',
  vendorBankId: '',
  paymentMode: 1,
  currency: 1,
  remark: '',
  fee: {
    intermediateBankFee: 0,
    bankCharge: 0,
    freight: 0,
    miscFee: 0,
    rounding: 0,
    intermediateBankFeePayer: '我方'
  },
  lines: [] as any[]
})

const paymentTotalAmount = computed(() => {
  const linesTotal = paymentForm.lines.reduce((sum: number, line: any) => sum + Number(line.requestAmount || 0), 0)
  const fee = paymentForm.fee
  const feeTotal = Number(fee.intermediateBankFee || 0) + Number(fee.bankCharge || 0) + Number(fee.freight || 0) + Number(fee.miscFee || 0) + Number(fee.rounding || 0)
  return Math.max(0, linesTotal + feeTotal)
})

/** 待请款为 0 时（常见于无 purchase.amount.read 导致单价被掩码）：勿设 max=0，否则 el-input-number 会把本次请款钳成 0 */
function paymentRequestAmountMax(row: { pendingRequested?: number }) {
  const p = Number(row?.pendingRequested ?? 0)
  return p > 0 ? p : undefined
}

const dateRange = ref<[string, string] | null>(null)
const filters = reactive({
  purchaseOrderCode: '',
  vendorName: '',
  purchaseUserName: '',
  pn: '',
  orderType: undefined as number | undefined
})

function isLineStockingPurchase(row: any) {
  return Number(row?.purchaseOrderType) === 2
}

function clearTableSelection() {
  selectedRows.value = []
  ;(tableRef.value as any)?.clearSelection?.()
}

function onOrderTypeFilterChange() {
  page.value = 1
  void loadList()
}

/** 筛选条件变更后查询：回到第一页（与分页切换区分）。 */
function runSearch() {
  page.value = 1
  void loadList()
}

function statusText(s: number) {
  const keyMap: Record<number, string> = {
    1: 'new',
    2: 'pendingReview',
    10: 'approved',
    20: 'pendingConfirm',
    30: 'confirmed',
    40: 'paid',
    50: 'shipped',
    60: 'stockedIn',
    100: 'completed',
    [-1]: 'reviewFailed',
    [-2]: 'cancelled'
  }
  const k = keyMap[s]
  return k ? t(`purchaseOrderItemList.itemStatus.${k}`) : String(s)
}

function statusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' | 'primary' {
  const map: Record<number, '' | 'success' | 'warning' | 'info' | 'danger' | 'primary'> = {
    1: 'info',
    2: 'warning',
    10: 'success',
    20: 'warning',
    30: 'primary',
    40: 'primary',
    50: 'warning',
    60: 'success',
    100: 'success',
    [-1]: 'danger',
    [-2]: 'info'
  }
  return map[s] ?? 'info'
}

function formatDt(v: string) {
  if (!v) return '—'
  const s = formatDisplayDateTime(v)
  return s === '--' ? '—' : s
}

/** 扩展表三态进度：0=待 1=部分 2=完成（与采购订单详情一致） */
function poExtendTriTagType(v: number): '' | 'info' | 'success' | 'warning' | 'danger' {
  const map: Record<number, '' | 'info' | 'success' | 'warning' | 'danger'> = {
    0: 'info',
    1: 'warning',
    2: 'success'
  }
  return map[v] ?? 'info'
}

function poPurchaseProgressText(v: number) {
  const map: Record<number, string> = {
    0: 'purchasePending',
    1: 'purchasePartial',
    2: 'purchaseDone'
  }
  const k = map[v]
  return k ? t(`purchaseOrderItemList.extendProgress.${k}`) : String(v)
}

function poStockInProgressText(v: number) {
  const map: Record<number, string> = {
    0: 'stockInPending',
    1: 'stockInPartial',
    2: 'stockInDone'
  }
  const k = map[v]
  return k ? t(`purchaseOrderItemList.extendProgress.${k}`) : String(v)
}

function poPaymentProgressText(v: number) {
  const map: Record<number, string> = {
    0: 'paymentPending',
    1: 'paymentPartial',
    2: 'paymentDone'
  }
  const k = map[v]
  return k ? t(`purchaseOrderItemList.extendProgress.${k}`) : String(v)
}

function poInvoiceProgressText(v: number) {
  const map: Record<number, string> = {
    0: 'invoicePending',
    1: 'invoicePartial',
    2: 'invoiceDone'
  }
  const k = map[v]
  return k ? t(`purchaseOrderItemList.extendProgress.${k}`) : String(v)
}

function buildFinancePaymentCode() {
  const d = new Date()
  const yy = String(d.getFullYear()).slice(-2)
  const MM = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  const HH = String(d.getHours()).padStart(2, '0')
  const mm = String(d.getMinutes()).padStart(2, '0')
  const ss = String(d.getSeconds()).padStart(2, '0')
  const rand = String(Math.floor(Math.random() * 100)).padStart(2, '0')
  // FP + yymmddHHmmss + 2位随机数 = 16位
  return `FP${yy}${MM}${dd}${HH}${mm}${ss}${rand}`
}

async function openPaymentDialog(row: any) {
  await loadPaymentBankOptions()
  paymentForm.vendorId = row.vendorId || ''
  paymentForm.vendorName = row.vendorName || ''
  paymentForm.purchaseUserName = row.purchaseUserName || ''
  paymentForm.vendorBankId = ''
  paymentForm.paymentMode = 1
  paymentForm.currency = row.currency || 1
  paymentForm.remark = ''
  paymentForm.fee = { intermediateBankFee: 0, bankCharge: 0, freight: 0, miscFee: 0, rounding: 0, intermediateBankFeePayer: '我方' }
  const lineTotal = Math.round((Number(row.lineTotal || 0) + Number.EPSILON) * 100) / 100
  const alreadyRequested = Math.max(0, Number(row.paymentRequestedAmount ?? 0))
  const pendingRequested = Math.max(0, Math.round((lineTotal - alreadyRequested + Number.EPSILON) * 100) / 100)
  paymentForm.lines = [{
    purchaseOrderId: row.purchaseOrderId,
    purchaseOrderItemId: row.purchaseOrderItemId,
    purchaseOrderCode: row.purchaseOrderCode,
    pn: row.pn,
    brand: row.brand,
    qty: row.qty,
    cost: row.cost,
    currency: row.currency,
    alreadyRequested,
    pendingRequested,
    requestAmount: pendingRequested,
    remark: ''
  }]
  paymentDialogVisible.value = true
}

async function openArrivalDialog(row: any) {
  try {
    await ensureLogisticsDict()
  } catch {
    /* 字典加载失败仍允许打开 */
  }
  arrivalNoticeShowProcessFields.value = false
  arrivalForm.purchaseOrderItemId = row.purchaseOrderItemId || row.id || ''
  arrivalForm.purchaseOrderId = row.purchaseOrderId || ''
  arrivalForm.purchaseOrderCode = row.purchaseOrderCode || ''
  arrivalForm.vendorName = row.vendorName || ''
  arrivalForm.pn = row.pn || ''
  arrivalForm.expectedArrivalDate = toDatePickerValue(row.deliveryDate)
  arrivalForm.companyName = row.vendorName || ''
  arrivalForm.address = ''
  arrivalForm.phone = ''
  arrivalForm.contact = ''
  arrivalForm.arrivalMethod = ''
  arrivalForm.expressMethod = ''
  arrivalForm.expressNo = ''
  arrivalForm.regionType = inferArrivalRegionTypeByCurrency(row.currency)
  arrivalForm.inspectionRequirement = ''
  arrivalForm.remark = ''
  arrivalForm.signer = ''
  arrivalForm.signDate = ''
  arrivalForm.qcUser = ''
  arrivalForm.qcDate = ''
  arrivalForm.stockInUser = ''
  arrivalForm.stockInDate = ''
  arrivalForm.lines = [{
    pn: row.pn || '',
    brand: row.brand || '',
    qty: Math.max(0, Math.round(Number(row.qty ?? 0))),
    spec: '',
    packaging: ''
  }]
  arrivalDialogVisible.value = true
}

function toDatePickerValue(v: unknown): string {
  if (v == null || v === '') return ''
  const s = String(v)
  const m = s.match(/^(\d{4}-\d{2}-\d{2})/)
  if (m) return m[1]
  const d = formatDisplayDate(s)
  return d === '--' ? '' : d
}

function inferArrivalRegionTypeByCurrency(currency: unknown): number {
  return Number(currency) === CurrencyCode.RMB
    ? REGION_TYPE_DOMESTIC
    : REGION_TYPE_OVERSEAS
}

async function submitArrivalNotice() {
  if (arrivalSubmitting.value) return
  if (!arrivalForm.purchaseOrderItemId) {
    ElMessage.warning(t('purchaseOrderItemList.messages.missingItemId'))
    return
  }
  if (!arrivalForm.purchaseOrderId) {
    ElMessage.warning(t('purchaseOrderItemList.messages.missingPoId'))
    return
  }
  if (!arrivalForm.expectedArrivalDate) {
    ElMessage.warning(t('purchaseOrderItemList.messages.fillExpectedDate'))
    return
  }
  const expectQty = Number(arrivalForm.lines?.[0]?.qty ?? 0)
  if (!expectQty || expectQty <= 0) {
    ElMessage.warning(t('purchaseOrderItemList.messages.qtyMustBePositive'))
    return
  }
  arrivalSubmitting.value = true
  try {
    await logisticsApi.createArrivalNotice({
      purchaseOrderItemId: arrivalForm.purchaseOrderItemId,
      expectQty,
      purchaseOrderId: arrivalForm.purchaseOrderId,
      expectedArrivalDate: arrivalForm.expectedArrivalDate,
      regionType: normalizeRegionType(arrivalForm.regionType)
    })
    ElMessage.success(t('purchaseOrderItemList.messages.arrivalCreated'))
    arrivalDialogVisible.value = false
  } catch (error: any) {
    ElMessage.error(error?.message || t('purchaseOrderItemList.messages.arrivalFailed'))
  } finally {
    arrivalSubmitting.value = false
  }
}

async function submitPayment() {
  if (paymentSubmitting.value) {
    return
  }

  if (!paymentForm.vendorId) {
    ElMessage.warning(t('purchaseOrderItemList.messages.missingVendorId'))
    return
  }
  if (!paymentForm.vendorBankId) {
    ElMessage.warning(t('purchaseOrderItemList.messages.selectVendorBank'))
    return
  }
  if (!paymentForm.lines.length || paymentForm.lines.some((x: any) => Number(x.requestAmount || 0) <= 0)) {
    ElMessage.warning(t('purchaseOrderItemList.messages.fillRequestAmount'))
    return
  }

  const payer = paymentForm.fee.intermediateBankFeePayer === '供应商' ? '供应商' : '我方'

  paymentSubmitting.value = true
  try {
    const created = await financePaymentApi.create({
      financePaymentCode: buildFinancePaymentCode(),
      vendorId: paymentForm.vendorId,
      vendorName: paymentForm.vendorName,
      paymentMode: paymentForm.paymentMode,
      paymentCurrency: paymentForm.currency,
      paymentAmountToBe: paymentTotalAmount.value,
      financePaymentBankId: paymentForm.vendorBankId,
      requestRemark: paymentForm.remark?.trim() || undefined,
      feeIntermediateBank: Number(paymentForm.fee.intermediateBankFee || 0),
      feeBankCharge: Number(paymentForm.fee.bankCharge || 0),
      feeFreight: Number(paymentForm.fee.freight || 0),
      feeMisc: Number(paymentForm.fee.miscFee || 0),
      feeRounding: Number(paymentForm.fee.rounding || 0),
      feeIntermediateBankPayer: payer,
      items: paymentForm.lines.map((line: any) => ({
        purchaseOrderId: line.purchaseOrderId,
        purchaseOrderItemId: line.purchaseOrderItemId,
        paymentAmountToBe: Number(line.requestAmount || 0),
        pn: line.pn,
        brand: line.brand,
        lineRemark: line.remark?.trim() || undefined
      }))
    })

    // 接口返回可能是 data 或直接对象，做兼容解析
    const paymentId = (created as any)?.id || (created as any)?.data?.id || (created as any)?.data?.data?.id
    if (!paymentId) {
      throw new Error(t('purchaseOrderItemList.messages.paymentNoId'))
    }

    await financePaymentApi.updateStatus(paymentId, 2)
    ElMessage.success(t('purchaseOrderItemList.messages.paymentSubmitted'))
    paymentDialogVisible.value = false
  } catch (error: any) {
    ElMessage.error(error?.message || t('purchaseOrderItemList.messages.paymentSubmitFailed'))
  } finally {
    paymentSubmitting.value = false
  }
}

function onSelectionChange(rows: any[]) {
  selectedRows.value = rows
}

async function loadList() {
  closePoItemLinePanel()
  loading.value = true
  try {
    const params: {
      page: number
      pageSize: number
      startDate?: string
      endDate?: string
      purchaseOrderCode?: string
      vendorName?: string
      purchaseUserName?: string
      pn?: string
      orderType?: number
    } = {
      page: page.value,
      pageSize: pageSize.value
    }
    if (dateRange.value?.[0]) params.startDate = dateRange.value[0]
    if (dateRange.value?.[1]) params.endDate = dateRange.value[1]
    if (filters.purchaseOrderCode.trim()) params.purchaseOrderCode = filters.purchaseOrderCode.trim()
    if (canViewVendor.value && filters.vendorName.trim()) params.vendorName = filters.vendorName.trim()
    if (canViewPurchaseUser.value && filters.purchaseUserName.trim()) params.purchaseUserName = filters.purchaseUserName.trim()
    if (filters.pn.trim()) params.pn = filters.pn.trim()
    if (filters.orderType !== undefined && filters.orderType !== null) params.orderType = filters.orderType

    const data = (await purchaseOrderApi.getItemLinesPage(params)) as {
      items?: any[]
      total?: number
      page?: number
    }
    const items = data.items ?? []
    const nTotal = data.total ?? 0
    if (page.value > 1 && items.length === 0 && nTotal > 0) {
      page.value = 1
      await loadList()
      return
    }
    tableRows.value = items
    total.value = nTotal
    if (typeof data.page === 'number' && data.page >= 1) page.value = data.page
    clearTableSelection()
  } catch (e: any) {
    // eslint-disable-next-line no-console
    console.error(e)
  } finally {
    loading.value = false
  }
}

function resetFilters() {
  dateRange.value = null
  filters.purchaseOrderCode = ''
  filters.vendorName = ''
  filters.purchaseUserName = ''
  filters.pn = ''
  filters.orderType = undefined
  page.value = 1
  void loadList()
}

function onPageChange(nextPage: number) {
  page.value = nextPage
  void loadList()
}

function onPageSizeChange(nextSize: number) {
  pageSize.value = nextSize
  page.value = 1
  void loadList()
}

function goDetail(row: any) {
  router.push({ name: 'PurchaseOrderDetail', params: { id: row.purchaseOrderId } })
}

onMounted(() => {
  const qpn = route.query.pn
  if (typeof qpn === 'string' && qpn.trim()) {
    filters.pn = qpn.trim()
  }
  void loadList()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.po-item-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  gap: 12px;
  flex-wrap: wrap;
}
.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}
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
.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0;
}
.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

.amount-with-code {
  display: inline-flex;
  align-items: baseline;
  gap: 4px;
}
.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  cursor: pointer;
  &:disabled {
    opacity: 0.45;
    cursor: not-allowed;
  }
  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}
.btn-ghost {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  cursor: pointer;
  font-family: 'Noto Sans SC', sans-serif;
  transition: border-color 0.2s, color 0.2s;
  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
  &:disabled {
    opacity: 0.45;
    cursor: not-allowed;
  }
}
.search-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}
.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}
.filter-field-label {
  font-size: 12px;
  font-weight: 500;
  color: $text-muted;
  white-space: nowrap;
}
.search-input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}
.search-icon {
  position: absolute;
  left: 10px;
  color: $text-muted;
  pointer-events: none;
}
.search-input {
  width: 200px;
  padding: 7px 12px 7px 32px;
  box-sizing: border-box;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  outline: none;
  transition: border-color 0.2s;
  font-family: 'Noto Sans SC', sans-serif;
  &::placeholder {
    color: $text-muted;
  }
  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }
}
.filter-date-range.po-date-range {
  width: 260px;
  :deep(.el-range-editor.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
}
.po-date-range {
  width: 260px;
}

.po-order-type-select {
  width: 140px;
  :deep(.el-select__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
}

.po-line-code-with-badge {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.po-stocking-tag {
  flex-shrink: 0;
  cursor: default;
}
.table-wrapper {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.pagination-wrapper {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-top: 16px;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
}

.list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.list-footer-density-anchor {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  min-height: 0;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}

.arrival-form-layout {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

/* 到货通知：标签与输入/下拉/日期控件垂直居中对齐（与控件中线一致） */
.arrival-form-layout :deep(.arrival-notice-form .el-form-item) {
  align-items: center;
}

.arrival-form-layout :deep(.arrival-notice-form .el-form-item__label) {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  white-space: nowrap;
  padding-right: 10px;
  padding-top: 0;
  padding-bottom: 0;
  line-height: 1.4;
  height: auto !important;
}

.arrival-form-layout :deep(.arrival-notice-form .el-form-item__content) {
  display: flex;
  align-items: center;
}
.arrival-section {
  border: 1px solid $border-panel;
  border-radius: 8px;
  padding: 12px;
  background: rgba(255, 255, 255, 0.02);
}
.section-title {
  font-size: 20px;
  margin-bottom: 8px;
  color: $text-primary;
}

/* 来货明细：数量步进器占满列宽，避免裁切 */
:deep(.arrival-qty-input) {
  width: 100%;
  box-sizing: border-box;
}
:deep(.arrival-qty-input .el-input__wrapper) {
  width: 100%;
}

.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  padding: 0 20px 20px;
}

.tabs-nav {
  display: flex;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  padding: 0 16px;
  background: rgba(0, 0, 0, 0.1);
  flex-wrap: wrap;
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
  font-family: 'Noto Sans SC', sans-serif;
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
  background: rgba(0, 212, 255, 0.04);
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
  color: rgba(0, 212, 255, 0.95);
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
  font-family: 'Noto Sans SC', sans-serif;
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

