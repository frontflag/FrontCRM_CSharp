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
      <div v-if="activePreset" class="search-preset-chip-row">
        <span class="search-preset-chip">
          {{ t(presetI18nKey(activePreset)) }}
          <button
            type="button"
            class="search-preset-chip__clear"
            :title="t('purchaseOrderItemList.searchPanel.clearPreset')"
            @click="clearPresetChip"
          >×</button>
        </span>
      </div>
      <div class="search-left">
        <el-date-picker
          v-if="!presetActive"
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
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.freightForwarderOrderNo"
            class="search-input"
            :placeholder="t('common.freightForwarderOrderNoPlaceholder')"
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
          v-if="tabModeDimension !== 'currency'"
          v-model="filters.transactionCurrency"
          clearable
          :placeholder="t('purchaseOrderItemList.filters.transactionCurrency')"
          class="filter-select"
          :teleported="false"
        >
          <el-option :label="t('purchaseOrderItemList.filters.transactionCurrencyRmb')" value="rmb" />
          <el-option :label="t('purchaseOrderItemList.filters.transactionCurrencyForeign')" value="foreign" />
        </el-select>

        <el-select
          v-if="tabModeDimension !== 'orderType'"
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

        <template v-if="!presetActive">
          <el-select
            v-if="tabModeDimension !== 'payment'"
            v-model="filters.paymentProgressStatus"
            clearable
            :placeholder="t('purchaseOrderItemList.filters.paymentProgressStatus')"
            class="filter-select filter-select--progress"
            :teleported="false"
          >
            <el-option
              v-for="opt in poProgressFilterOptions('payment')"
              :key="`payment-${opt.value}`"
              :label="opt.label"
              :value="opt.value"
            />
          </el-select>
          <el-select
            v-if="tabModeDimension !== 'purchase'"
            v-model="filters.purchaseProgressStatus"
            clearable
            :placeholder="t('purchaseOrderItemList.filters.purchaseProgressStatus')"
            class="filter-select filter-select--progress"
            :teleported="false"
          >
            <el-option
              v-for="opt in poProgressFilterOptions('purchase')"
              :key="`purchase-${opt.value}`"
              :label="opt.label"
              :value="opt.value"
            />
          </el-select>
          <el-select
            v-if="tabModeDimension !== 'stockIn'"
            v-model="filters.stockInProgressStatus"
            clearable
            :placeholder="t('purchaseOrderItemList.filters.stockInProgressStatus')"
            class="filter-select filter-select--progress"
            :teleported="false"
          >
            <el-option
              v-for="opt in poProgressFilterOptions('stockIn')"
              :key="`stockIn-${opt.value}`"
              :label="opt.label"
              :value="opt.value"
            />
          </el-select>
          <el-select
            v-if="tabModeDimension !== 'invoice'"
            v-model="filters.invoiceProgressStatus"
            clearable
            :placeholder="t('purchaseOrderItemList.filters.invoiceProgressStatus')"
            class="filter-select filter-select--progress"
            :teleported="false"
          >
            <el-option
              v-for="opt in poProgressFilterOptions('invoice')"
              :key="`invoice-${opt.value}`"
              :label="opt.label"
              :value="opt.value"
            />
          </el-select>
        </template>

        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="runSearch">
          {{ t('purchaseOrderItemList.filters.search') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="resetFilters">
          {{ t('purchaseOrderItemList.filters.reset') }}
        </button>
        <button
          class="btn-ghost btn-sm btn-board-active"
          type="button"
          @click="toggleViewMode"
        >
          {{ viewMode === 'board' ? t('purchaseOrderItemList.filters.listView') : t('purchaseOrderItemList.filters.boardView') }}
        </button>
        <el-popover
          v-model:visible="settingsMenuOpen"
          trigger="click"
          placement="bottom-end"
          :width="168"
          :show-arrow="false"
          popper-class="po-item-list-settings-popper"
        >
          <template #reference>
            <button
              type="button"
              class="btn-ghost btn-sm btn-icon-only"
              :title="t('purchaseOrderItemList.settingsMenu.aria')"
              :aria-label="t('purchaseOrderItemList.settingsMenu.aria')"
            >
              <el-icon :size="14"><Setting /></el-icon>
            </button>
          </template>
          <div class="po-item-list-settings-menu">
            <button
              type="button"
              class="po-item-list-settings-menu__item"
              :disabled="tabModeDimension === 'off'"
              @click="closeFilterTabMode"
            >
              {{ t('purchaseOrderItemList.settingsMenu.closeTabs') }}
            </button>
            <div
              class="po-item-list-settings-menu__submenu"
              @mouseenter="settingsSubmenuOpen = true"
              @mouseleave="settingsSubmenuOpen = false"
            >
              <div class="po-item-list-settings-menu__item po-item-list-settings-menu__item--parent">
                <span>{{ t('purchaseOrderItemList.settingsMenu.tabMode') }}</span>
                <el-icon class="po-item-list-settings-menu__caret"><ArrowRight /></el-icon>
              </div>
              <div v-show="settingsSubmenuOpen" class="po-item-list-settings-menu__flyout">
                <button
                  v-for="dim in visibleTabModeMenuOptions"
                  :key="dim"
                  type="button"
                  class="po-item-list-settings-menu__item"
                  :class="{ 'is-active': tabModeDimension === dim }"
                  @click="enableFilterTabMode(dim)"
                >
                  {{ tabModeDimensionLabel(dim) }}
                </button>
              </div>
            </div>
          </div>
        </el-popover>
      </div>
    </div>

    <div class="po-main-panel" :class="{ 'po-main-panel--with-filter-tabs': filterTabStripVisible }">
    <div
      v-if="filterTabStripVisible"
      class="po-filter-tabs"
      role="tablist"
      :aria-label="filterTabStripAriaLabel"
    >
      <button
        v-for="tab in filterTabOptions"
        :key="tab.id"
        type="button"
        role="tab"
        class="po-filter-tabs__item"
        :class="{ 'is-active': activeFilterTabId === tab.id }"
        :aria-selected="activeFilterTabId === tab.id"
        @click="onFilterTabClick(tab.id)"
      >
        {{ tab.label }}
      </button>
    </div>

    <PurchaseOrderItemListBoard v-if="viewMode === 'board'" :filters="boardFilters" />

    <div v-show="viewMode === 'list'" class="po-list-body">
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
      :row-class-name="opsPanelRowClassName"
      @selection-change="onSelectionChange"
      @row-click="onRowClick"
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
      <template #col-paymentRequestProgressStatus="{ row }">
        <el-tag
          effect="dark"
          size="small"
          :type="Number(row.paymentRequestProgressStatus ?? 0) >= 1 ? 'success' : 'info'"
        >
          {{ poPaymentRequestProgressText(Number(row.paymentRequestProgressStatus ?? 0)) }}
        </el-tag>
      </template>
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
            <el-form-item :label="t('purchaseOrderItemList.paymentDialog.purchaser')">
              <el-input :model-value="paymentForm.purchaseUserName || '--'" disabled />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="12">
          <el-col :span="24">
            <el-form-item :label="t('purchaseOrderItemList.paymentDialog.vendorInfo')">
              <vendor-name-readonly-field
                :name-zh="paymentForm.vendorName"
                :name-en="paymentForm.vendorEnglishName"
                :masked="maskPurchaseSensitiveFields"
                mode="compact"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <payment-request-vendor-bank-section
          v-model="paymentForm.vendorBankId"
          :vendor-id="paymentForm.vendorId"
          :banks="paymentForm.vendorBanks"
          :masked="maskPurchaseSensitiveFields"
          @maintain="paymentDialogVisible = false"
        >
          <template #trailing>
            <el-form-item :label="t('purchaseOrderItemList.paymentDialog.paymentMode')" required>
              <el-select v-model="paymentForm.paymentMode" style="width: 100%">
                <el-option :label="t('purchaseOrderItemList.paymentDialog.modeWire')" :value="1" />
                <el-option :label="t('purchaseOrderItemList.paymentDialog.modeCash')" :value="2" />
                <el-option :label="t('purchaseOrderItemList.paymentDialog.modeCheck')" :value="3" />
                <el-option :label="t('purchaseOrderItemList.paymentDialog.modeAcceptance')" :value="4" />
              </el-select>
            </el-form-item>
          </template>
        </payment-request-vendor-bank-section>
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
          <CrmCopyableTableColumn prop="pn" :label="t('purchaseOrderItemList.paymentDialog.colPn')" min-width="120" />
          <CrmCopyableTableColumn prop="brand" :label="t('purchaseOrderItemList.paymentDialog.colBrand')" width="100" />
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
        <el-button
          type="primary"
          :loading="paymentSubmitting"
          :disabled="!hasEnabledVendorBanks"
          @click="submitPayment()"
        >
          {{ t('purchaseOrderItemList.paymentDialog.submit') }}
        </el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="arrivalDialogVisible"
      :title="t('purchaseOrderItemList.arrivalDialog.title')"
      width="1240px"
      align-center
      destroy-on-close
    >
      <div class="arrival-form-layout">

        <div class="arrival-section">
          <el-form label-width="120px" class="arrival-notice-form">
            <el-row :gutter="12">
              <el-col :span="8">
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
                <el-form-item :label="t('purchaseOrderItemList.arrivalDialog.purchaseOrderCode')">
                  <el-input
                    :model-value="arrivalForm.purchaseOrderCode"
                    class="arrival-po-code-input"
                    readonly
                  />
                </el-form-item>
              </el-col>
            </el-row>
            <el-row :gutter="12">
              <ShipmentExpressFields
                v-model:shipment-method="arrivalForm.shipmentMethod"
                v-model:express-company="arrivalForm.expressCompany"
                :shipment-label="t('purchaseOrderItemList.arrivalDialog.expectedArrivalMethod')"
                :express-label="t('purchaseOrderItemList.arrivalDialog.expressCompany')"
                :placeholder="t('purchaseOrderItemList.arrivalDialog.selectPlaceholder')"
                :shipment-required="false"
                :col-span="8"
              />
              <el-col :span="8">
                <el-form-item :label="t('purchaseOrderItemList.arrivalDialog.expectedArrivalExpressNo')">
                  <el-input v-model="arrivalForm.courierTrackingNo" />
                </el-form-item>
              </el-col>
            </el-row>
            <el-row :gutter="12">
              <el-col :span="12">
                <el-form-item :label="t('purchaseOrderItemList.arrivalDialog.companyName')">
                  <el-input v-if="!maskPurchaseSensitiveFields" v-model="arrivalForm.companyName" />
                  <el-input v-else model-value="—" disabled />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('purchaseOrderItemList.arrivalDialog.address')">
                  <el-input v-model="arrivalForm.address" />
                </el-form-item>
              </el-col>
            </el-row>
            <el-row :gutter="12">
              <el-col :span="12"><el-form-item :label="t('purchaseOrderItemList.arrivalDialog.phone')"><el-input v-model="arrivalForm.phone" /></el-form-item></el-col>
              <el-col :span="12"><el-form-item :label="t('purchaseOrderItemList.arrivalDialog.contact')"><el-input v-model="arrivalForm.contact" /></el-form-item></el-col>
            </el-row>
          </el-form>
        </div>

        <div class="arrival-section">
          <div class="section-title">{{ t('purchaseOrderItemList.arrivalDialog.sectionLines') }}</div>
          <CrmDataTable :data="arrivalForm.lines" size="small">
            <el-table-column :label="t('purchaseOrderItemList.arrivalDialog.seq')" width="70">
              <template #default="{ $index }">{{ $index + 1 }}</template>
            </el-table-column>
            <el-table-column :label="t('purchaseOrderItemList.arrivalDialog.materialPn')" min-width="160">
              <template #default="{ row }">
                <CrmListCopyableTextCell :text="row.pn || ''" />
              </template>
            </el-table-column>
            <el-table-column :label="t('purchaseOrderItemList.arrivalDialog.brand')" min-width="130">
              <template #default="{ row }">
                <CrmListCopyableTextCell :text="row.brand || ''" />
              </template>
            </el-table-column>
            <el-table-column :label="t('purchaseOrderItemList.arrivalDialog.orderQty')" width="100" align="right">
              <template #default="{ row }">{{ formatArrivalNoticeQty(row.orderQty) }}</template>
            </el-table-column>
            <el-table-column :label="t('purchaseOrderItemList.arrivalDialog.alreadyNotified')" min-width="118" align="right">
              <template #default="{ row }">{{ formatArrivalNoticeQty(row.alreadyNotified) }}</template>
            </el-table-column>
            <el-table-column :label="t('purchaseOrderItemList.arrivalDialog.applicableQty')" min-width="118" align="right">
              <template #default="{ row }">
                <span :class="{ 'arrival-applicable-zero': row.applicableQty <= 0 }">
                  {{ formatArrivalNoticeQty(row.applicableQty) }}
                </span>
              </template>
            </el-table-column>
            <el-table-column :label="t('purchaseOrderItemList.arrivalDialog.thisQty')" min-width="140" align="right">
              <template #default="{ row }">
                <span v-if="row.applicableQty <= 0" class="arrival-qty-cannot-apply">
                  {{ t('purchaseOrderItemList.messages.cannotApplyArrival') }}
                </span>
                <el-input-number
                  v-else
                  v-model="row.qty"
                  :min="0"
                  :max="row.applicableQty"
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
import { computed, inject, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { ArrowRight, Setting } from '@element-plus/icons-vue'
import PurchaseOrderItemListBoard from './PurchaseOrderItemListBoard.vue'
import type { PurchaseOrderItemListAnalyticsQuery } from '@/api/purchaseOrderItemAnalytics'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import {
  buildPoItemListRouteQuery,
  isPoItemListPresetId,
  isPoItemTimePresetId,
  presetI18nKey,
  resolvePoItemTimePresetDateRange,
  type PoItemListPresetId
} from '@/utils/purchaseOrderItemListPreset'
import {
  currencyFilterToTab,
  currencyTabToFilter,
  isPoProgressTabDimension,
  orderTypeFilterToTab,
  orderTypeTabToFilter,
  progressDimensionToFilterKey,
  progressFilterToTab,
  progressTabToFilter,
  readPoItemTabMode,
  writePoItemTabMode,
  PO_ITEM_TAB_MODE_OPTIONS,
  type PoItemCurrencyTabId,
  type PoItemOrderTypeTabId,
  type PoItemProgressTabId,
  type PoItemTabModeDimension
} from '@/utils/purchaseOrderItemListTabMode'
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
import { buildPurchaseOrderItemListColumns } from '@/composables/buildPurchaseOrderItemListColumns'
import SettlementCurrencyAmountInput from '@/components/SettlementCurrencyAmountInput.vue'
import PaymentRequestVendorBankSection from '@/components/Vendor/PaymentRequestVendorBankSection.vue'
import VendorNameReadonlyField from '@/components/Vendor/VendorNameReadonlyField.vue'
import ShipmentExpressFields from '@/components/Logistics/ShipmentExpressFields.vue'
import { REGION_TYPE_DOMESTIC, REGION_TYPE_OVERSEAS, normalizeRegionType } from '@/constants/regionType'
import { CurrencyCode, DEFAULT_SETTLEMENT_CURRENCY_CODE } from '@/constants/currency'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { usePurchaseOrderWriteGate } from '@/composables/useDepartmentDataReadOnly'
import { onCrmDetailListRowDblClick } from '@/utils/crmDetailListRowDblClick'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import { resetListRightPanelOnReload } from '@/composables/useListRightPanelReset'
import { usePurchaseOrderItemOpsPanelStore } from '@/stores/purchaseOrderItemOpsPanel'
import { vendorBankApi } from '@/api/vendor'
import { filterEnabledVendorBanks, resolveVendorDefaultBankId } from '@/utils/vendorFinancePaymentBank'
import {
  buildPurchaseArrivalNoticeLineRow,
  formatArrivalNoticeQty
} from '@/utils/purchaseArrivalNoticeLine'

const router = useRouter()
const route = useRoute()
const { t, locale } = useI18n()
const authStore = useAuthStore()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { canWritePo } = usePurchaseOrderWriteGate()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const purchaseOrderItemOpsStore = usePurchaseOrderItemOpsPanelStore()
const viewMode = ref<'list' | 'board'>('list')
const tabModeDimension = ref<PoItemTabModeDimension>(readPoItemTabMode())
const settingsMenuOpen = ref(false)
const settingsSubmenuOpen = ref(false)

const TAB_MODE_FILTER_I18N: Record<Exclude<PoItemTabModeDimension, 'off'>, string> = {
  currency: 'purchaseOrderItemList.filters.transactionCurrency',
  orderType: 'purchaseOrderItemList.filters.orderType',
  payment: 'purchaseOrderItemList.filters.paymentProgressStatus',
  purchase: 'purchaseOrderItemList.filters.purchaseProgressStatus',
  stockIn: 'purchaseOrderItemList.filters.stockInProgressStatus',
  invoice: 'purchaseOrderItemList.filters.invoiceProgressStatus'
}

function tabModeDimensionLabel(dim: Exclude<PoItemTabModeDimension, 'off'>) {
  return t(TAB_MODE_FILTER_I18N[dim])
}

function closeFilterTabMode() {
  if (tabModeDimension.value === 'off') return
  tabModeDimension.value = 'off'
  writePoItemTabMode('off')
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

watch(settingsMenuOpen, (open) => {
  if (!open) settingsSubmenuOpen.value = false
})

/** 与采购订单列表/详情一致：脱敏时不得展示供应商检索与列 */
const canViewVendor = computed(
  () =>
    !maskPurchaseSensitiveFields.value &&
    (authStore.hasPermission('vendor.info.read') ||
      authStore.hasPermission('vendor.read') ||
      authStore.hasPermission('purchase-order.read') ||
      authStore.hasPermission('purchase-order.write'))
)

async function onPurchaseOrderItemRowDblClick(row: Record<string, unknown>, _column: unknown, event?: MouseEvent) {
  onCrmDetailListRowDblClick(row, _column, event, {
    canEdit: canWritePo.value && !maskPurchaseSensitiveFields.value,
    onEdit: goEdit,
    onDefault: navigatePurchaseOrderItemDetail,
  })
}

function navigatePurchaseOrderItemDetail(row: Record<string, unknown>) {
  if (maskPurchaseSensitiveFields.value) return
  const purchaseOrderId = String(row?.purchaseOrderId ?? '').trim()
  const purchaseOrderItemId = String(row?.purchaseOrderItemId ?? '').trim()
  if (!purchaseOrderId || !purchaseOrderItemId) return
  router.push({
    name: 'PurchaseOrderDetail',
    params: { id: purchaseOrderId },
    query: { purchaseOrderItemId }
  })
}

function goEdit(row: Record<string, unknown>) {
  const purchaseOrderId = String(row?.purchaseOrderId ?? '').trim()
  if (!purchaseOrderId) return
  router.push({ name: 'PurchaseOrderEdit', params: { id: purchaseOrderId } })
}

function isRightPanelVisible() {
  return workspaceLayout?.rightPanelVisible.value ?? false
}

async function onRowClick(row: Record<string, unknown>) {
  if (maskPurchaseSensitiveFields.value) return
  workspaceLayout?.setRightActiveTab('r-ops')

  if (isRightPanelVisible()) {
    await purchaseOrderItemOpsStore.selectRow(row, t('purchaseOrderItemList.messages.loadLineFailed'))
    return
  }

  purchaseOrderItemOpsStore.setRowOnly(row)
  workspaceLayout?.toggleRightPanel(true)
}

function opsPanelRowClassName({ row }: { row: Record<string, unknown> }) {
  if (!purchaseOrderItemOpsStore.row) return ''
  return purchaseOrderItemOpsStore.rowKey(row) === purchaseOrderItemOpsStore.rowKey(purchaseOrderItemOpsStore.row)
    ? 'so-item-row--active'
    : ''
}

watch(maskPurchaseSensitiveFields, (masked) => {
  if (masked) purchaseOrderItemOpsStore.clear()
})

watch(
  () => workspaceLayout?.rightPanelVisible.value,
  (visible, wasVisible) => {
    if (route.name !== 'PurchaseOrderItemList') return
    if (!visible || wasVisible || !purchaseOrderItemOpsStore.row) return
    void purchaseOrderItemOpsStore.loadAggregates(t('purchaseOrderItemList.messages.loadLineFailed'))
  }
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
  return buildPurchaseOrderItemListColumns({
    t,
    canViewVendor: canViewVendor.value,
    canViewPurchaseUser: canViewPurchaseUser.value,
    canViewAmount: canViewAmount.value,
    opColWidth: opColWidth.value,
    opColMinWidth: opColMinWidth.value,
    withSelection: true,
    withActions: true
  })
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
  shipmentMethod: '',
  expressCompany: '',
  courierTrackingNo: '',
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
  vendorEnglishName: '',
  purchaseUserName: '',
  vendorBankId: '',
  vendorBanks: [] as import('@/types/vendor').VendorBankInfo[],
  paymentMode: 1,
  currency: DEFAULT_SETTLEMENT_CURRENCY_CODE,
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

const hasEnabledVendorBanks = computed(() => filterEnabledVendorBanks(paymentForm.vendorBanks).length > 0)

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
  freightForwarderOrderNo: '',
  vendorName: '',
  purchaseUserName: '',
  pn: '',
  transactionCurrency: '' as '' | 'rmb' | 'foreign',
  orderType: undefined as number | undefined,
  paymentProgressStatus: undefined as number | undefined,
  purchaseProgressStatus: undefined as number | undefined,
  stockInProgressStatus: undefined as number | undefined,
  invoiceProgressStatus: undefined as number | undefined
})

const activePreset = computed((): PoItemListPresetId | null => {
  const p = route.query.preset
  return typeof p === 'string' && isPoItemListPresetId(p) ? p : null
})

const presetActive = computed(() => !!activePreset.value)

/** preset 打开时隐藏进度类页签模式项，保留币别与订单类型 */
const visibleTabModeMenuOptions = computed(() =>
  presetActive.value
    ? PO_ITEM_TAB_MODE_OPTIONS.filter((dim) => dim === 'currency' || dim === 'orderType')
    : PO_ITEM_TAB_MODE_OPTIONS
)

function enableFilterTabMode(dim: Exclude<PoItemTabModeDimension, 'off'>) {
  if (isPoProgressTabDimension(dim) && presetActive.value) return
  tabModeDimension.value = dim
  writePoItemTabMode(dim)
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

const boardFilters = computed((): PurchaseOrderItemListAnalyticsQuery => {
  const q: PurchaseOrderItemListAnalyticsQuery = {}
  if (dateRange.value?.[0]) q.startDate = dateRange.value[0]
  if (dateRange.value?.[1]) q.endDate = dateRange.value[1]
  const qf = route.query.quickFilter
  if (typeof qf === 'string' && qf.trim() && activePreset.value) {
    q.quickFilter = qf.trim()
  }
  const poc = String(filters.purchaseOrderCode ?? '').trim()
  if (poc) q.purchaseOrderCode = poc
  const ffo = String(filters.freightForwarderOrderNo ?? '').trim()
  if (ffo) q.freightForwarderOrderNo = ffo
  if (canViewVendor.value) {
    const vn = String(filters.vendorName ?? '').trim()
    if (vn) q.vendorName = vn
  }
  if (canViewPurchaseUser.value) {
    const pun = String(filters.purchaseUserName ?? '').trim()
    if (pun) q.purchaseUserName = pun
  }
  const pnk = String(filters.pn ?? '').trim()
  if (pnk) q.pn = pnk
  if (filters.orderType !== undefined && filters.orderType !== null) q.orderType = filters.orderType
  if (filters.transactionCurrency) q.transactionCurrency = filters.transactionCurrency
  if (!activePreset.value) {
    if (filters.paymentProgressStatus !== undefined && filters.paymentProgressStatus !== null) {
      q.paymentProgressStatus = filters.paymentProgressStatus
    }
    if (filters.purchaseProgressStatus !== undefined && filters.purchaseProgressStatus !== null) {
      q.purchaseProgressStatus = filters.purchaseProgressStatus
    }
    if (filters.stockInProgressStatus !== undefined && filters.stockInProgressStatus !== null) {
      q.stockInProgressStatus = filters.stockInProgressStatus
    }
    if (filters.invoiceProgressStatus !== undefined && filters.invoiceProgressStatus !== null) {
      q.invoiceProgressStatus = filters.invoiceProgressStatus
    }
  }
  return q
})

function toggleViewMode() {
  viewMode.value = viewMode.value === 'list' ? 'board' : 'list'
}

type PoProgressFilterKind = 'payment' | 'purchase' | 'stockIn' | 'invoice'

function poProgressFilterOptions(kind: PoProgressFilterKind) {
  const keyMap: Record<PoProgressFilterKind, Record<0 | 1 | 2, string>> = {
    payment: { 0: 'paymentPending', 1: 'paymentPartial', 2: 'paymentDone' },
    purchase: { 0: 'purchasePending', 1: 'purchasePartial', 2: 'purchaseDone' },
    stockIn: { 0: 'stockInPending', 1: 'stockInPartial', 2: 'stockInDone' },
    invoice: { 0: 'invoicePending', 1: 'invoicePartial', 2: 'invoiceDone' }
  }
  return ([0, 1, 2] as const).map((value) => ({
    value,
    label: t(`purchaseOrderItemList.extendProgress.${keyMap[kind][value]}`)
  }))
}

type FilterTabId = PoItemCurrencyTabId | PoItemOrderTypeTabId | PoItemProgressTabId

/** 进度类页签在左栏 preset 打开时隐藏；币别 / 订单类型仍显示 */
const filterTabStripVisible = computed(() => {
  const dim = tabModeDimension.value
  if (dim === 'off') return false
  if (isPoProgressTabDimension(dim) && presetActive.value) return false
  return true
})

const filterTabStripAriaLabel = computed(() => {
  const dim = tabModeDimension.value
  if (dim === 'off') return ''
  return tabModeDimensionLabel(dim)
})

const filterTabOptions = computed(() => {
  const dim = tabModeDimension.value
  if (dim === 'off') return [] as Array<{ id: FilterTabId; label: string }>
  if (dim === 'currency') {
    return [
      { id: 'all' as const, label: t('purchaseOrderItemList.filterTabs.all') },
      { id: 'rmb' as const, label: t('purchaseOrderItemList.filters.transactionCurrencyRmb') },
      { id: 'foreign' as const, label: t('purchaseOrderItemList.filters.transactionCurrencyForeign') }
    ]
  }
  if (dim === 'orderType') {
    return [
      { id: 'all' as const, label: t('purchaseOrderItemList.filterTabs.all') },
      { id: '1' as const, label: t('purchaseOrderItemList.filters.orderTypeCustomer') },
      { id: '2' as const, label: t('purchaseOrderItemList.filters.orderTypeStocking') },
      { id: '3' as const, label: t('purchaseOrderItemList.filters.orderTypeSample') }
    ]
  }
  const kind = dim as PoProgressFilterKind
  return [
    { id: 'all' as const, label: t('purchaseOrderItemList.filterTabs.all') },
    ...poProgressFilterOptions(kind).map((opt) => ({
      id: String(opt.value) as PoItemProgressTabId,
      label: opt.label
    }))
  ]
})

const activeFilterTabId = computed((): FilterTabId => {
  const dim = tabModeDimension.value
  if (dim === 'currency') return currencyFilterToTab(filters.transactionCurrency)
  if (dim === 'orderType') return orderTypeFilterToTab(filters.orderType)
  if (isPoProgressTabDimension(dim)) {
    const key = progressDimensionToFilterKey(dim)
    return progressFilterToTab(filters[key])
  }
  return 'all'
})

function onFilterTabClick(tab: FilterTabId) {
  const dim = tabModeDimension.value
  if (dim === 'currency') {
    const next = currencyTabToFilter(tab as PoItemCurrencyTabId)
    if (filters.transactionCurrency === next) return
    filters.transactionCurrency = next
    runSearch()
    return
  }
  if (dim === 'orderType') {
    const next = orderTypeTabToFilter(tab as PoItemOrderTypeTabId)
    if (filters.orderType === next) return
    filters.orderType = next
    runSearch()
    return
  }
  if (!isPoProgressTabDimension(dim)) return
  const key = progressDimensionToFilterKey(dim)
  const next = progressTabToFilter(tab as PoItemProgressTabId)
  if (filters[key] === next) return
  filters[key] = next
  runSearch()
}

function isLineStockingPurchase(row: any) {
  return Number(row?.purchaseOrderType) === 2
}

function clearTableSelection() {
  selectedRows.value = []
  ;(tableRef.value as any)?.clearSelection?.()
}

function onOrderTypeFilterChange() {
  runSearch()
}

function collectKeywordQuery(): Record<string, string> {
  const keywords: Record<string, string> = {}
  const poc = String(filters.purchaseOrderCode ?? '').trim()
  if (poc) keywords.purchaseOrderCode = poc
  const ffo = String(filters.freightForwarderOrderNo ?? '').trim()
  if (ffo) keywords.freightForwarderOrderNo = ffo
  if (canViewVendor.value) {
    const vn = String(filters.vendorName ?? '').trim()
    if (vn) keywords.vendorName = vn
  }
  if (canViewPurchaseUser.value) {
    const pun = String(filters.purchaseUserName ?? '').trim()
    if (pun) keywords.purchaseUserName = pun
  }
  const pnk = String(filters.pn ?? '').trim()
  if (pnk) keywords.pn = pnk
  if (filters.transactionCurrency) keywords.transactionCurrency = filters.transactionCurrency
  if (filters.orderType !== undefined && filters.orderType !== null) {
    keywords.orderType = String(filters.orderType)
  }
  return keywords
}

function buildListRouteQueryFromUi(): Record<string, string> {
  const keywords = collectKeywordQuery()
  if (activePreset.value) {
    return buildPoItemListRouteQuery({ preset: activePreset.value, keywords })
  }
  const advanced: Record<string, string> = {}
  if (dateRange.value?.[0]) advanced.startDate = dateRange.value[0]
  if (dateRange.value?.[1]) advanced.endDate = dateRange.value[1]
  if (filters.paymentProgressStatus !== undefined && filters.paymentProgressStatus !== null) {
    advanced.paymentProgressStatus = String(filters.paymentProgressStatus)
  }
  if (filters.purchaseProgressStatus !== undefined && filters.purchaseProgressStatus !== null) {
    advanced.purchaseProgressStatus = String(filters.purchaseProgressStatus)
  }
  if (filters.stockInProgressStatus !== undefined && filters.stockInProgressStatus !== null) {
    advanced.stockInProgressStatus = String(filters.stockInProgressStatus)
  }
  if (filters.invoiceProgressStatus !== undefined && filters.invoiceProgressStatus !== null) {
    advanced.invoiceProgressStatus = String(filters.invoiceProgressStatus)
  }
  return buildPoItemListRouteQuery({ keywords, advanced })
}

/** 筛选条件变更后查询：回到第一页（与分页切换区分）。 */
function runSearch() {
  page.value = 1
  router.replace({ name: 'PurchaseOrderItemList', query: buildListRouteQueryFromUi() })
}

function clearPresetChip() {
  router.replace({ name: 'PurchaseOrderItemList', query: {} })
}

function parseProgressQuery(v: unknown): number | undefined {
  if (typeof v !== 'string' || !v.trim()) return undefined
  const n = Number(v)
  return n === 0 || n === 1 || n === 2 ? n : undefined
}

function syncFiltersFromRoute() {
  if (route.name !== 'PurchaseOrderItemList') return
  const q = route.query
  filters.purchaseOrderCode = typeof q.purchaseOrderCode === 'string' ? q.purchaseOrderCode : ''
  filters.freightForwarderOrderNo = typeof q.freightForwarderOrderNo === 'string' ? q.freightForwarderOrderNo : ''
  filters.vendorName = typeof q.vendorName === 'string' ? q.vendorName : ''
  filters.purchaseUserName = typeof q.purchaseUserName === 'string' ? q.purchaseUserName : ''
  filters.pn = typeof q.pn === 'string' ? q.pn : ''
  filters.transactionCurrency =
    q.transactionCurrency === 'rmb' || q.transactionCurrency === 'foreign' ? q.transactionCurrency : ''
  const ot = typeof q.orderType === 'string' ? Number(q.orderType) : NaN
  filters.orderType = ot === 1 || ot === 2 || ot === 3 ? ot : undefined

  const preset = activePreset.value
  if (preset) {
    filters.paymentProgressStatus = undefined
    filters.purchaseProgressStatus = undefined
    filters.stockInProgressStatus = undefined
    filters.invoiceProgressStatus = undefined
    if (isPoItemTimePresetId(preset)) {
      dateRange.value = resolvePoItemTimePresetDateRange(preset)
    } else {
      dateRange.value = null
    }
    return
  }

  const from = typeof q.startDate === 'string' ? q.startDate : ''
  const to = typeof q.endDate === 'string' ? q.endDate : ''
  dateRange.value = from && to ? [from, to] : null
  filters.paymentProgressStatus = parseProgressQuery(q.paymentProgressStatus)
  filters.purchaseProgressStatus = parseProgressQuery(q.purchaseProgressStatus)
  filters.stockInProgressStatus = parseProgressQuery(q.stockInProgressStatus)
  filters.invoiceProgressStatus = parseProgressQuery(q.invoiceProgressStatus)
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

function poPaymentRequestProgressText(v: number) {
  if (Number(v) >= 1) return t('purchaseOrderItemList.extendProgress.paymentRequestApplied')
  return t('purchaseOrderItemList.extendProgress.paymentRequestPending')
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
  paymentForm.vendorId = row.vendorId || ''
  paymentForm.vendorName = row.vendorName || ''
  paymentForm.vendorEnglishName = row.vendorEnglishName || ''
  paymentForm.purchaseUserName = row.purchaseUserName || ''
  paymentForm.vendorBankId = ''
  paymentForm.vendorBanks = []
  if (paymentForm.vendorId) {
    try {
      const banks = await vendorBankApi.getBanksByVendorId(paymentForm.vendorId)
      paymentForm.vendorBanks = banks
      paymentForm.vendorBankId = resolveVendorDefaultBankId(banks)
    } catch {
      paymentForm.vendorBanks = []
      paymentForm.vendorBankId = ''
    }
  }
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
  arrivalForm.shipmentMethod = ''
  arrivalForm.expressCompany = ''
  arrivalForm.courierTrackingNo = ''
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
  arrivalForm.lines = [buildPurchaseArrivalNoticeLineRow(row)]
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
  const line = arrivalForm.lines?.[0]
  const expectQty = Number(line?.qty ?? 0)
  const applicableQty = Math.max(0, Math.round(Number(line?.applicableQty ?? 0)))
  if (!expectQty || expectQty <= 0) {
    ElMessage.warning(t('purchaseOrderItemList.messages.qtyMustBePositive'))
    return
  }
  if (expectQty > applicableQty) {
    ElMessage.warning(
      t('purchaseOrderItemList.messages.qtyExceedsApplicable', { max: formatArrivalNoticeQty(applicableQty) })
    )
    return
  }
  arrivalSubmitting.value = true
  try {
    await logisticsApi.createArrivalNotice({
      purchaseOrderItemId: arrivalForm.purchaseOrderItemId,
      expectQty,
      purchaseOrderId: arrivalForm.purchaseOrderId,
      expectedArrivalDate: arrivalForm.expectedArrivalDate,
      regionType: normalizeRegionType(arrivalForm.regionType),
      shipmentMethod: arrivalForm.shipmentMethod?.trim() || undefined,
      expressCompany: arrivalForm.expressCompany?.trim() || undefined,
      courierTrackingNo: arrivalForm.courierTrackingNo?.trim() || undefined
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
      vendorBankId: paymentForm.vendorBankId,
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
  loading.value = true
  try {
    const params: {
      page: number
      pageSize: number
      startDate?: string
      endDate?: string
      purchaseOrderCode?: string
      freightForwarderOrderNo?: string
      vendorName?: string
      purchaseUserName?: string
      pn?: string
      orderType?: number
      transactionCurrency?: 'rmb' | 'foreign'
      paymentProgressStatus?: number
      purchaseProgressStatus?: number
      stockInProgressStatus?: number
      invoiceProgressStatus?: number
      quickFilter?: string
    } = {
      page: page.value,
      pageSize: pageSize.value
    }
    if (dateRange.value?.[0]) params.startDate = dateRange.value[0]
    if (dateRange.value?.[1]) params.endDate = dateRange.value[1]
    if (filters.purchaseOrderCode.trim()) params.purchaseOrderCode = filters.purchaseOrderCode.trim()
    if (filters.freightForwarderOrderNo.trim()) params.freightForwarderOrderNo = filters.freightForwarderOrderNo.trim()
    if (canViewVendor.value && filters.vendorName.trim()) params.vendorName = filters.vendorName.trim()
    if (canViewPurchaseUser.value && filters.purchaseUserName.trim()) params.purchaseUserName = filters.purchaseUserName.trim()
    if (filters.pn.trim()) params.pn = filters.pn.trim()
    if (filters.orderType !== undefined && filters.orderType !== null) params.orderType = filters.orderType
    if (filters.transactionCurrency) params.transactionCurrency = filters.transactionCurrency
    const qf = route.query.quickFilter
    if (typeof qf === 'string' && qf.trim() && activePreset.value) {
      params.quickFilter = qf.trim()
    } else if (!activePreset.value) {
      if (filters.paymentProgressStatus !== undefined && filters.paymentProgressStatus !== null) {
        params.paymentProgressStatus = filters.paymentProgressStatus
      }
      if (filters.purchaseProgressStatus !== undefined && filters.purchaseProgressStatus !== null) {
        params.purchaseProgressStatus = filters.purchaseProgressStatus
      }
      if (filters.stockInProgressStatus !== undefined && filters.stockInProgressStatus !== null) {
        params.stockInProgressStatus = filters.stockInProgressStatus
      }
      if (filters.invoiceProgressStatus !== undefined && filters.invoiceProgressStatus !== null) {
        params.invoiceProgressStatus = filters.invoiceProgressStatus
      }
    }

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
    resetListRightPanelOnReload(purchaseOrderItemOpsStore)
  } catch (e: any) {
    // eslint-disable-next-line no-console
    console.error(e)
  } finally {
    loading.value = false
  }
}

function resetFilters() {
  page.value = 1
  router.replace({ name: 'PurchaseOrderItemList', query: {} })
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
  purchaseOrderItemOpsStore.registerHandlers({
    applyArrival: (row) => {
      void openArrivalDialog(row)
    },
    applyPayment: (row) => {
      void openPaymentDialog(row)
    }
  })
})

watch(
  () => [route.name, route.query] as const,
  async () => {
    syncFiltersFromRoute()
    if (route.name === 'PurchaseOrderItemList') {
      page.value = 1
      await loadList()
    }
  },
  { deep: true, immediate: true }
)

onBeforeUnmount(() => {
  purchaseOrderItemOpsStore.unregisterHandlers()
  purchaseOrderItemOpsStore.clear()
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
.btn-board-active {
  border-color: rgba(0, 212, 255, 0.45);
  color: #00d4ff;
  background: rgba(0, 212, 255, 0.08);
}
.btn-icon-only {
  width: 32px;
  padding-left: 0;
  padding-right: 0;
  justify-content: center;
}
.po-main-panel {
  width: 100%;
}
.po-main-panel--with-filter-tabs {
  .po-list-body,
  :deep(.po-item-list-board) {
    margin-top: 0;
  }

  :deep(.table-wrapper),
  :deep(.crm-data-table-root),
  .quantum-table-block {
    margin-top: 0;
    border-top-left-radius: 0;
    border-top-right-radius: 0;
  }

  :deep(.el-table),
  :deep(.el-table__inner-wrapper),
  :deep(.el-table__header-wrapper) {
    border-top-left-radius: 0;
    border-top-right-radius: 0;
  }

  :deep(.po-item-list-board > .board-toolbar.card:first-child),
  :deep(.po-item-list-board > .section:first-child) {
    border-top-left-radius: 0;
    border-top-right-radius: 0;
  }
}
.po-filter-tabs {
  display: flex;
  align-items: stretch;
  width: 100%;
  margin: 0;
  padding: 0;
  gap: 4px;
}
.po-filter-tabs__item {
  flex: 1 1 0;
  min-width: 0;
  padding: 9px 12px;
  border: 1px solid var(--crm-border-panel, #e2e8f0);
  border-bottom: none;
  border-radius: 8px 8px 0 0;
  background: #e8edf5;
  color: var(--crm-text-primary);
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  font-weight: 500;
  text-align: center;
  cursor: pointer;
  transition: background 0.12s, border-color 0.12s, color 0.12s, box-shadow 0.12s;

  &:hover {
    border-color: color-mix(in srgb, var(--crm-cyan-primary) 45%, var(--crm-border-panel));
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }

  &.is-active {
    background: color-mix(in srgb, var(--crm-cyan-primary) 16%, var(--crm-layer-2, #fff));
    border-color: color-mix(in srgb, var(--crm-cyan-primary) 55%, var(--crm-border-panel));
    box-shadow: inset 0 2px 0 0 var(--crm-cyan-primary);
    font-weight: 600;
    z-index: 1;
  }
}

html[data-theme='dark'] .po-filter-tabs__item:not(.is-active) {
  background: var(--crm-layer-1);

  &:hover {
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }
}
.po-list-body {
  width: 100%;
}
.search-bar {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 8px;
  margin-bottom: 12px;
}
.search-preset-chip-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}
.search-preset-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 8px 4px 10px;
  font-size: 12px;
  color: $text-primary;
  background: rgba(0, 212, 255, 0.1);
  border: 1px solid rgba(0, 212, 255, 0.35);
  border-radius: 20px;
}
.search-preset-chip__clear {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  padding: 0;
  border: none;
  border-radius: 50%;
  background: transparent;
  color: $text-muted;
  font-size: 14px;
  line-height: 1;
  cursor: pointer;
  &:hover {
    color: $text-primary;
    background: rgba(255, 255, 255, 0.08);
  }
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

.filter-select {
  width: 130px;
  &.filter-select--progress {
    width: 132px;
  }
  :deep(.el-select__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
  :deep(.el-select__placeholder),
  :deep(.el-select__selected-item) {
    font-size: 13px;
  }
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

.arrival-form-layout :deep(.arrival-po-code-input .el-input__inner),
.arrival-form-layout :deep(.arrival-po-code-input .el-input__wrapper) {
  color: #e6a23c;
  cursor: default;
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

.arrival-applicable-zero {
  color: $warning-color;
}

.arrival-qty-cannot-apply {
  color: $text-muted;
  font-size: 13px;
}

.arrival-line-label {
  display: inline-block;
  color: $text-primary;
  line-height: 32px;
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

<style lang="scss">
.po-item-list-settings-popper.el-popover.el-popper {
  padding: 6px;
  min-width: 160px;
  overflow: visible;
}

.po-item-list-settings-menu {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.po-item-list-settings-menu__item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 8px 10px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: var(--crm-text-secondary, rgba(224, 244, 255, 0.7));
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  text-align: left;
  cursor: pointer;

  &:hover:not(:disabled) {
    background: var(--crm-accent-008, rgba(0, 212, 255, 0.08));
    color: var(--crm-text-primary, #e8f4ff);
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }

  &.is-active {
    color: var(--crm-cyan-primary, #00d4ff);
  }

  &--parent {
    cursor: default;
  }
}

.po-item-list-settings-menu__caret {
  margin-left: 8px;
  font-size: 12px;
  color: var(--crm-text-muted, rgba(200, 216, 232, 0.55));
}

.po-item-list-settings-menu__submenu {
  position: relative;
}

.po-item-list-settings-menu__flyout {
  position: absolute;
  top: 0;
  left: calc(100% + 4px);
  min-width: 148px;
  padding: 6px;
  border-radius: 8px;
  border: 1px solid var(--crm-border-panel, rgba(0, 212, 255, 0.15));
  background: var(--crm-layer-2, #0d1e35);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.28);
  z-index: 10;
}
</style>

