<template>
  <div class="so-item-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('salesOrderItemList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('salesOrderItemList.count', { count: total }) }}</div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="loadList">{{ t('salesOrderItemList.filters.refresh') }}</button>
      </div>
    </div>

    <div class="search-bar">
      <div v-if="activePreset" class="search-preset-chip-row">
        <span class="search-preset-chip">
          {{ t(presetI18nKey(activePreset)) }}
          <button type="button" class="search-preset-chip__clear" :title="t('salesOrderItemList.searchPanel.clearPreset')" @click="clearPresetChip">×</button>
        </span>
      </div>
      <div class="search-bar__row">
      <div class="search-left">
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.sellOrderCode"
            class="search-input"
            :placeholder="t('salesOrderItemList.filters.sellOrderCode')"
            @keyup.enter="runSearch"
          />
        </div>
        <template v-if="listCustomerColumnOk">
          <div class="search-input-wrap">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="filters.customerName"
              class="search-input"
              :placeholder="t('salesOrderItemList.filters.customerName')"
              @keyup.enter="runSearch"
            />
          </div>
        </template>
        <template v-if="listSalesUserFilterOk">
          <div class="search-input-wrap">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="filters.salesUserName"
              class="search-input"
              :placeholder="t('salesOrderItemList.filters.salesUserName')"
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
            v-model="filters.purchaseUserAccount"
            class="search-input"
            :placeholder="t('salesOrderItemList.filters.purchaseUserAccount')"
            @keyup.enter="runSearch"
          />
        </div>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.pn"
            class="search-input"
            :placeholder="t('salesOrderItemList.filters.pn')"
            @keyup.enter="runSearch"
          />
        </div>
        <template v-if="listCustomerColumnOk">
          <div class="search-input-wrap">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="filters.customerSo"
              class="search-input"
              :placeholder="t('salesOrderItemList.filters.customerSo')"
              @keyup.enter="runSearch"
            />
          </div>
          <div class="search-input-wrap">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="filters.customerPn"
              class="search-input"
              :placeholder="t('salesOrderItemList.filters.customerPn')"
              @keyup.enter="runSearch"
            />
          </div>
        </template>
        <el-select
          v-if="tabModeDimension !== 'currency'"
          v-model="filters.transactionCurrency"
          clearable
          :placeholder="t('salesOrderItemList.filters.transactionCurrency')"
          class="filter-select"
          :teleported="false"
        >
          <el-option :label="t('salesOrderItemList.filters.transactionCurrencyRmb')" value="rmb" />
          <el-option :label="t('salesOrderItemList.filters.transactionCurrencyForeign')" value="foreign" />
        </el-select>
        <template v-if="!presetActive">
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            :range-separator="t('salesOrderItemList.filters.rangeTo')"
            :start-placeholder="t('salesOrderItemList.filters.dateStart')"
            :end-placeholder="t('salesOrderItemList.filters.dateEnd')"
            value-format="YYYY-MM-DD"
            class="filter-date-range so-date-range"
            clearable
            :teleported="false"
          />
          <el-select
            v-if="tabModeDimension !== 'purchase'"
            v-model="filters.purchaseProgressStatus"
            clearable
            :placeholder="t('salesOrderItemList.filters.purchaseProgressStatus')"
            class="filter-select filter-select--progress"
            :teleported="false"
          >
            <el-option
              v-for="opt in progressFilterOptions('purchase')"
              :key="`purchase-${opt.value}`"
              :label="opt.label"
              :value="opt.value"
            />
          </el-select>
          <el-select
            v-if="tabModeDimension !== 'stockIn'"
            v-model="filters.stockInProgressStatus"
            clearable
            :placeholder="t('salesOrderItemList.filters.stockInProgressStatus')"
            class="filter-select filter-select--progress"
            :teleported="false"
          >
            <el-option
              v-for="opt in progressFilterOptions('stockIn')"
              :key="`stockIn-${opt.value}`"
              :label="opt.label"
              :value="opt.value"
            />
          </el-select>
          <el-select
            v-if="tabModeDimension !== 'stockOutNotify'"
            v-model="filters.stockOutNotifyProgressStatus"
            clearable
            :placeholder="t('salesOrderItemList.filters.stockOutNotifyProgressStatus')"
            class="filter-select filter-select--progress"
            :teleported="false"
          >
            <el-option
              v-for="opt in progressFilterOptions('stockOutNotify')"
              :key="`stockOutNotify-${opt.value}`"
              :label="opt.label"
              :value="opt.value"
            />
          </el-select>
          <el-select
            v-if="tabModeDimension !== 'stockOut'"
            v-model="filters.stockOutProgressStatus"
            clearable
            :placeholder="t('salesOrderItemList.filters.stockOutProgressStatus')"
            class="filter-select filter-select--progress"
            :teleported="false"
          >
            <el-option
              v-for="opt in progressFilterOptions('stockOut')"
              :key="`stockOut-${opt.value}`"
              :label="opt.label"
              :value="opt.value"
            />
          </el-select>
          <el-select
            v-if="tabModeDimension !== 'receipt'"
            v-model="filters.receiptProgressStatus"
            clearable
            :placeholder="t('salesOrderItemList.filters.receiptProgressStatus')"
            class="filter-select filter-select--progress"
            :teleported="false"
          >
            <el-option
              v-for="opt in progressFilterOptions('receipt')"
              :key="`receipt-${opt.value}`"
              :label="opt.label"
              :value="opt.value"
            />
          </el-select>
          <el-select
            v-if="tabModeDimension !== 'invoice'"
            v-model="filters.invoiceProgressStatus"
            clearable
            :placeholder="t('salesOrderItemList.filters.invoiceProgressStatus')"
            class="filter-select filter-select--progress"
            :teleported="false"
          >
            <el-option
              v-for="opt in progressFilterOptions('invoice')"
              :key="`invoice-${opt.value}`"
              :label="opt.label"
              :value="opt.value"
            />
          </el-select>
        </template>
        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="runSearch">{{ t('salesOrderItemList.filters.query') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">{{ t('salesOrderItemList.filters.reset') }}</button>
        <button
          class="btn-ghost btn-sm btn-board-active"
          type="button"
          @click="toggleViewMode"
        >
          {{ viewMode === 'board' ? t('salesOrderItemList.filters.listView') : t('salesOrderItemList.filters.boardView') }}
        </button>
        <el-popover
          v-model:visible="settingsMenuOpen"
          trigger="click"
          placement="bottom-end"
          :width="168"
          :show-arrow="false"
          popper-class="so-item-list-settings-popper"
        >
          <template #reference>
            <button
              type="button"
              class="btn-ghost btn-sm btn-icon-only"
              :title="t('salesOrderItemList.settingsMenu.aria')"
              :aria-label="t('salesOrderItemList.settingsMenu.aria')"
            >
              <el-icon :size="14"><Setting /></el-icon>
            </button>
          </template>
          <div class="so-item-list-settings-menu">
            <button
              type="button"
              class="so-item-list-settings-menu__item"
              :disabled="tabModeDimension === 'off'"
              @click="closeFilterTabMode"
            >
              {{ t('salesOrderItemList.settingsMenu.closeTabs') }}
            </button>
            <div
              class="so-item-list-settings-menu__submenu"
              @mouseenter="settingsSubmenuOpen = true"
              @mouseleave="settingsSubmenuOpen = false"
            >
              <div class="so-item-list-settings-menu__item so-item-list-settings-menu__item--parent">
                <span>{{ t('salesOrderItemList.settingsMenu.tabMode') }}</span>
                <el-icon class="so-item-list-settings-menu__caret"><ArrowRight /></el-icon>
              </div>
              <div v-show="settingsSubmenuOpen" class="so-item-list-settings-menu__flyout">
                <button
                  v-for="dim in visibleTabModeMenuOptions"
                  :key="dim"
                  type="button"
                  class="so-item-list-settings-menu__item"
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
    </div>

    <div class="so-main-panel" :class="{ 'so-main-panel--with-filter-tabs': filterTabStripVisible }">
    <div
      v-if="filterTabStripVisible"
      class="so-filter-tabs"
      role="tablist"
      :aria-label="filterTabStripAriaLabel"
    >
      <button
        v-for="tab in filterTabOptions"
        :key="tab.id"
        type="button"
        role="tab"
        class="so-filter-tabs__item"
        :class="{ 'is-active': activeFilterTabId === tab.id }"
        :aria-selected="activeFilterTabId === tab.id"
        @click="onFilterTabClick(tab.id)"
      >
        {{ tab.label }}
      </button>
    </div>

    <SalesOrderItemListBoard v-if="viewMode === 'board'" :filters="boardFilters" />

    <div v-show="viewMode === 'list'" class="so-list-body">
    <CrmDataTable
      ref="dataTableRef"
      class="quantum-table-block el-table-host"
      column-layout-key="sales-order-item-list-v5"
      :columns="salesOrderItemColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="list"
      v-loading="loading"
      row-key="sellOrderItemId"
      :row-class-name="opsPanelRowClassName"
      @selection-change="onSelectionChange"
      @row-click="onRowClick"
      @row-dblclick="onSalesOrderItemListRowDblClick"
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
      <template #col-purchaseUserAccountDisplay="{ row }">
        <span>{{ row.purchaseUserAccountDisplay || '—' }}</span>
      </template>
      <template #col-orderStatus="{ row }">
        <el-tag effect="dark" :type="statusTagType(row.orderStatus)" size="small">{{ statusText(row.orderStatus) }}</el-tag>
      </template>
      <template #col-purchaseProgressStatus="{ row }">
        <el-tag effect="dark" :type="extendTriTagType(row.purchaseProgressStatus)" size="small">
          {{ extendTriLabel('purchase', row.purchaseProgressStatus) }}
        </el-tag>
      </template>
      <template #col-stockInProgressStatus="{ row }">
        <el-tag effect="dark" :type="extendTriTagType(row.stockInProgressStatus)" size="small">
          {{ extendTriLabel('stockIn', row.stockInProgressStatus) }}
        </el-tag>
      </template>
      <template #col-stockOutProgressStatus="{ row }">
        <el-tag effect="dark" :type="extendTriTagType(row.stockOutProgressStatus)" size="small">
          {{ extendTriLabel('stockOut', row.stockOutProgressStatus) }}
        </el-tag>
      </template>
      <template #col-stockOutNotifyProgressStatus="{ row }">
        <el-tag effect="dark" :type="extendTriTagType(row.stockOutNotifyProgressStatus)" size="small">
          {{ extendTriLabel('stockOutNotify', row.stockOutNotifyProgressStatus) }}
        </el-tag>
      </template>
      <template #col-receiptProgressStatus="{ row }">
        <el-tag effect="dark" :type="extendTriTagType(row.receiptProgressStatus)" size="small">
          {{ extendTriLabel('receipt', row.receiptProgressStatus) }}
        </el-tag>
      </template>
      <template #col-invoiceProgressStatus="{ row }">
        <el-tag effect="dark" :type="extendTriTagType(row.invoiceProgressStatus)" size="small">
          {{ extendTriLabel('invoice', row.invoiceProgressStatus) }}
        </el-tag>
      </template>
      <template #col-currency="{ row }">{{ settlementCurrencyLabel(row.currency) }}</template>
      <template #col-price="{ row }">
        <span class="amount-with-code">
          <span>{{ formatUnitPriceNumber(row.price) }}</span>
          <span v-if="formatUnitPriceNumber(row.price) !== '—'" :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">
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
      <template #col-createTime="{ row }">{{ formatDt(row.createTime || row.orderCreateTime) }}</template>
      <template #col-createUser="{ row }">{{
        row.createUserName || row.createdBy || (!maskSaleSensitiveFields ? row.salesUserName : '') || '—'
      }}</template>
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
            <el-button link type="primary" size="small" @click.stop="goDetail(row)">{{ t('salesOrderItemList.actions.detail') }}</el-button>
            <el-button v-if="canWriteSo" link type="primary" size="small" @click.stop="goEdit(row)">{{ t('salesOrderItemList.actions.edit') }}</el-button>
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
                <el-dropdown-item @click.stop="goDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('salesOrderItemList.actions.detail') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteSo" @click.stop="goEdit(row)">
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
    </CrmDataTable>

    <div v-if="total > 0" class="table-footer-bar">
      <div class="basket-footer-left">
        <el-tooltip :content="t('salesOrderItemList.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('salesOrderItemList.columnSettings')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
        <el-button class="basket-open-btn" link type="primary" @click="basketDrawerVisible = true">
          {{ t('salesOrderItemList.basket.open') }}<span v-if="basketCount" class="basket-count-label">（{{ basketCount }}）</span>
        </el-button>
        <el-button
          v-if="basketCount"
          class="basket-clear-btn"
          link
          type="warning"
          @click="handleClearBasket"
        >
          {{ t('salesOrderItemList.basket.clear') }}
        </el-button>
        <button
          v-if="canPurchaseReq"
          type="button"
          class="btn-primary btn-sm basket-batch-purchase-btn"
          :disabled="!basketCount || !basketItems.every((r) => mainAllowsOps(r))"
          @click="batchApplyPurchase"
        >
          {{ t('salesOrderItemList.basket.batchPurchase') }}
        </button>
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

    <el-drawer
      v-model="basketDrawerVisible"
      :title="t('salesOrderItemList.basket.drawerTitle')"
      direction="rtl"
      size="min(560px, 94vw)"
      class="so-item-basket-drawer"
    >
      <p v-if="!basketCount" class="basket-drawer-hint">{{ t('salesOrderItemList.basket.emptyHint') }}</p>
      <template v-else>
        <p class="basket-drawer-summary">
          {{ t('salesOrderItemList.basket.summaryBeforeBtn', { count: basketCount }) }}
          <el-button
            class="basket-clear-btn basket-clear-btn--drawer-inline"
            link
            type="warning"
            @click="handleClearBasket"
          >
            {{ t('salesOrderItemList.basket.clear') }}
          </el-button>
          {{ t('salesOrderItemList.basket.summaryAfterBtn') }}
        </p>
        <div class="crm-items-table crm-data-table">
          <el-table :data="basketItems" max-height="70vh" size="small" border stripe>
            <el-table-column
              prop="sellOrderCode"
              :label="t('salesOrderItemList.columns.sellOrderCode')"
              min-width="140"
              show-overflow-tooltip
            />
            <el-table-column :label="t('salesOrderItemList.columns.status')" width="100" align="center">
              <template #default="{ row }">
                <el-tag effect="dark" :type="statusTagType(Number(row.orderStatus))" size="small">
                  {{ statusText(Number(row.orderStatus)) }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column
              v-if="listCustomerColumnOk"
              prop="customerName"
              :label="t('salesOrderItemList.columns.customerName')"
              min-width="120"
              show-overflow-tooltip
            />
            <CrmCopyableTableColumn prop="pn" :label="t('salesOrderItemList.columns.pn')" min-width="130" />
            <el-table-column prop="qty" :label="t('salesOrderItemList.columns.qty')" width="72" align="right" />
            <el-table-column
              :label="t('salesOrderItemList.columns.actions')"
              :width="soItemBasketOpColWidth"
              :min-width="soItemBasketOpColMinWidth"
              fixed="right"
              align="center"
              class-name="op-col"
              label-class-name="op-col"
              :resizable="false"
            >
              <template #header>
                <div class="list-op-col-header--icon-only">
                  <button
                    type="button"
                    class="op-col-toggle-btn list-op-col-toggle"
                    :aria-label="soItemBasketOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
                    @click.stop="toggleSoItemBasketOpCol"
                  >
                    {{ soItemBasketOpColExpanded ? '>' : '<' }}
                  </button>
                </div>
              </template>
              <template #default="{ row }">
                <div @click.stop @dblclick.stop>
                  <div v-if="soItemBasketOpColExpanded" class="action-btns">
                    <el-button
                      link
                      type="danger"
                      size="small"
                      @click.stop="removeOneFromBasket(String(row.sellOrderItemId ?? ''))"
                    >
                      {{ t('salesOrderItemList.actions.remove') }}
                    </el-button>
                  </div>
                  <el-dropdown v-else trigger="click" placement="bottom-end">
                    <div class="op-more-dropdown-trigger">
                      <button type="button" class="op-more-trigger">...</button>
                    </div>
                    <template #dropdown>
                      <el-dropdown-menu>
                        <el-dropdown-item @click.stop="removeOneFromBasket(String(row.sellOrderItemId ?? ''))">
                          <span class="op-more-item op-more-item--danger">{{ t('salesOrderItemList.actions.remove') }}</span>
                        </el-dropdown-item>
                      </el-dropdown-menu>
                    </template>
                  </el-dropdown>
                </div>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </template>
    </el-drawer>

    <!-- 新建采购申请弹窗 -->
    <el-dialog v-model="applyDialogVisible" :title="t('salesOrderItemList.dialog.createPrTitle')" width="720px" destroy-on-close>
      <el-form ref="applyFormRef" :model="applyForm" :rules="applyRules" label-width="140px" v-loading="applyLoading">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.pn')">
              <el-input v-model="applyForm.pn" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.brand')">
              <el-input v-model="applyForm.brand" disabled />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.orderLineQty')">
              <el-input :model-value="applyFormSalesOrderQtyText" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.purchasedQty')">
              <el-input :model-value="applyFormPurchasedQtyText" disabled />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.openPrQty')">
              <el-input :model-value="applyFormOpenPrQtyText" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.availableQty')">
              <el-input :model-value="applyFormRemainingQtyText" disabled />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.requestQty')" prop="requestQty">
              <el-input-number
                v-model="applyForm.requestQty"
                :min="0"
                :precision="0"
                :step="1"
                :max="applyForm.remainingQty"
                controls-position="right"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.expectedPurchaseDate')" prop="expectedPurchaseDate">
              <el-date-picker
                v-model="applyForm.expectedPurchaseDate"
                type="date"
                :placeholder="t('salesOrderItemList.dialog.expectedDatePlaceholder')"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item :label="t('salesOrderItemList.dialog.remark')">
          <el-input v-model="applyForm.remark" type="textarea" rows="3" :placeholder="t('salesOrderItemList.dialog.remarkPlaceholder')" />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="applyDialogVisible = false">{{ t('salesOrderItemList.dialog.cancel') }}</el-button>
          <el-button type="primary" :loading="applySubmitting" @click="submitApply" :disabled="applyLoading">
            {{ t('salesOrderItemList.dialog.confirm') }}
          </el-button>
        </span>
      </template>
    </el-dialog>

    <ApplyStockOutDialog ref="applyStockOutDialogRef" @success="onApplyStockOutSuccess" />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, nextTick, watch, inject, onMounted, onBeforeUnmount } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { ArrowRight, Setting } from '@element-plus/icons-vue'
import {
  currencyFilterToTab,
  currencyTabToFilter,
  isProgressTabDimension,
  progressDimensionToFilterKey,
  progressFilterToTab,
  progressTabToFilter,
  readSoItemTabMode,
  writeSoItemTabMode,
  SO_ITEM_TAB_MODE_OPTIONS,
  type SoItemCurrencyTabId,
  type SoItemProgressTabId,
  type SoItemTabModeDimension
} from '@/utils/salesOrderItemListTabMode'
import ApplyStockOutDisabledHint from '@/components/RFQ/ApplyStockOutDisabledHint.vue'
import ApplyStockOutDialog from '@/components/RFQ/ApplyStockOutDialog.vue'
import SalesOrderItemListBoard from './SalesOrderItemListBoard.vue'
import type { SalesOrderItemListAnalyticsQuery } from '@/api/salesOrderItemAnalytics'
import { storeToRefs } from 'pinia'
import { useAuthStore } from '@/stores/auth'
import { useSalesOrderItemListBasketStore } from '@/stores/salesOrderItemListBasket'
import { useSalesOrderItemOpsPanelStore } from '@/stores/salesOrderItemOpsPanel'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import { useListRightOpsPanelInteraction } from '@/composables/useListRightOpsPanelInteraction'
import CrmDataTable from '@/components/CrmDataTable.vue'
import salesOrderApi from '@/api/salesOrder'
import purchaseRequisitionApi from '@/api/purchaseRequisition'
import { runSaveTask, validateElFormOrWarn } from '@/composables/useFormSubmit'
import {
  translateSalesOrderStatus,
  salesOrderStatusTagType,
  salesOrderMainAllowsPurchaseAndStockOut,
  salesOrderLineApplyStockOutButtonDisabled,
  salesOrderLineApplyStockOutDisabled,
  salesOrderLinePurchasedStockReliefOk
} from '@/constants/salesOrderStatus'
import { buildApplyStockOutDisabledHintContent } from '@/utils/applyStockOutDisabledHint'
import type { ApplyStockOutDisabledHintContent } from '@/utils/applyStockOutDisabledHint'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { onCrmDetailListRowDblClick } from '@/utils/crmDetailListRowDblClick'
import { formatTotalAmountNumber, formatUnitPriceNumber, listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'
import { formatProfitOutRateBizDisplay } from '@/utils/profitOutRateDisplay'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import type { SalesOrderItemLineRow } from '@/stores/salesOrderItemListBasket'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { buildSalesOrderItemListColumns } from '@/composables/buildSalesOrderItemListColumns'
import { useSaleOrderWriteGate } from '@/composables/useDepartmentDataReadOnly'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { resetListRightPanelOnReload } from '@/composables/useListRightPanelReset'
import {
  buildSoItemListRouteQuery,
  isSoItemListPresetId,
  isSoItemTimePresetId,
  presetI18nKey,
  resolveSoItemTimePresetDateRange,
  type SoItemListPresetId
} from '@/utils/salesOrderItemListPreset'

const router = useRouter()
const route = useRoute()
const { t, locale } = useI18n()
const authStore = useAuthStore()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const salesOrderItemOpsStore = useSalesOrderItemOpsPanelStore()

const basketStore = useSalesOrderItemListBasketStore()
const { count: basketCount, items: basketItems } = storeToRefs(basketStore)
const suppressBasketMerge = ref(false)
const basketDrawerVisible = ref(false)
/** 《列表操作列规范》：复选篮子抽屉内表 */
const soItemBasketOpColExpanded = ref(false)
const SO_ITEM_BASKET_OP_COL_COLLAPSED = 43
const SO_ITEM_BASKET_OP_COL_EXPANDED = 173
const SO_ITEM_BASKET_OP_COL_EXPANDED_MIN = 160
const soItemBasketOpColWidth = computed(() =>
  soItemBasketOpColExpanded.value ? SO_ITEM_BASKET_OP_COL_EXPANDED : SO_ITEM_BASKET_OP_COL_COLLAPSED
)
const soItemBasketOpColMinWidth = computed(() =>
  soItemBasketOpColExpanded.value ? SO_ITEM_BASKET_OP_COL_EXPANDED_MIN : SO_ITEM_BASKET_OP_COL_COLLAPSED
)
function toggleSoItemBasketOpCol() {
  soItemBasketOpColExpanded.value = !soItemBasketOpColExpanded.value
}
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const canViewCustomer = computed(
  () => authStore.hasPermission('customer.info.read') || authStore.hasPermission('sales-order.read')
)
const canViewAmount = computed(() => authStore.hasPermission('sales.amount.read'))
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const listCustomerColumnOk = computed(() => canViewCustomer.value && !maskSaleSensitiveFields.value)
const listSalesUserFilterOk = computed(() => !maskSaleSensitiveFields.value)
const listShowAmountColumns = computed(() => canViewAmount.value && !maskSaleSensitiveFields.value)
const { canWriteSo } = useSaleOrderWriteGate()
const viewMode = ref<'list' | 'board'>('list')

const tabModeDimension = ref<SoItemTabModeDimension>(readSoItemTabMode())
const settingsMenuOpen = ref(false)
const settingsSubmenuOpen = ref(false)
const TAB_MODE_FILTER_I18N: Record<Exclude<SoItemTabModeDimension, 'off'>, string> = {
  currency: 'salesOrderItemList.filters.transactionCurrency',
  purchase: 'salesOrderItemList.filters.purchaseProgressStatus',
  stockIn: 'salesOrderItemList.filters.stockInProgressStatus',
  stockOutNotify: 'salesOrderItemList.filters.stockOutNotifyProgressStatus',
  stockOut: 'salesOrderItemList.filters.stockOutProgressStatus',
  receipt: 'salesOrderItemList.filters.receiptProgressStatus',
  invoice: 'salesOrderItemList.filters.invoiceProgressStatus'
}

function tabModeDimensionLabel(dim: Exclude<SoItemTabModeDimension, 'off'>) {
  return t(TAB_MODE_FILTER_I18N[dim])
}

function closeFilterTabMode() {
  if (tabModeDimension.value === 'off') return
  tabModeDimension.value = 'off'
  writeSoItemTabMode('off')
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

watch(settingsMenuOpen, (open) => {
  if (!open) settingsSubmenuOpen.value = false
})
/** 业务员可从销售明细发起采购申请，不必单独持有 purchase-requisition.write */
const canPurchaseReq = computed(
  () =>
    authStore.hasPermission('purchase-requisition.write') ||
    authStore.hasPermission('sales-order.write')
)

function stockOutApplyPurchaseGateOk(row: Record<string, unknown>) {
  return row.stockOutApplyPurchaseGateOk === true
}

/** 剩余可采为 0 时禁用「申请采购」（与行选项 / 服务端创建校验口径一致） */
function applyPurchaseDisabled(row: Record<string, unknown>) {
  const raw = (row as { purchaseRemainingQty?: unknown }).purchaseRemainingQty
  if (raw === undefined || raw === null) return false
  const n = Number(raw)
  if (!Number.isFinite(n)) return false
  return n <= 0
}

function mainAllowsOps(row: SalesOrderItemLineRow) {
  const os = row['orderStatus']
  return salesOrderMainAllowsPurchaseAndStockOut(Number(os))
}

/** 结算币别编码 → ISO 文案（与 SETTLEMENT_CURRENCY_OPTIONS 一致） */
function settlementCurrencyLabel(code: unknown): string {
  const c = Number(code)
  if (!Number.isFinite(c)) return '—'
  return CURRENCY_CODE_TO_TEXT[c as keyof typeof CURRENCY_CODE_TO_TEXT] ?? '—'
}

const loading = ref(false)
const list = ref<any[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)

// 规范：列表进入页面时“操作”列默认处于收起态（Collapsed）
const opColExpanded = ref(false)
const OP_COL_EXPANDED_WIDTH = 173
// 收起态需要同时显示列头「操作」与「<」按钮；
// 由于 el-table header/cell 默认 padding 较大，这里给一个偏保守的最小宽度，避免被裁剪。
const OP_COL_COLLAPSED_WIDTH = 43
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? 260 : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const salesOrderItemColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return buildSalesOrderItemListColumns({
    t,
    listCustomerColumnOk: listCustomerColumnOk.value,
    listShowAmountColumns: listShowAmountColumns.value,
    opColWidth: opColWidth.value,
    opColMinWidth: opColMinWidth.value,
    withSelection: true,
    withActions: true
  })
})

const dateRange = ref<[string, string] | null>(null)
const stockOutPending = ref(false)
const invoicePending = ref(false)
const salesUserIdFilter = ref('')
const customerIdFilter = ref('')
const filters = reactive({
  sellOrderCode: '',
  customerName: '',
  salesUserName: '',
  purchaseUserAccount: '',
  pn: '',
  customerSo: '',
  customerPn: '',
  transactionCurrency: '' as '' | 'rmb' | 'foreign',
  purchaseProgressStatus: undefined as number | undefined,
  stockInProgressStatus: undefined as number | undefined,
  stockOutNotifyProgressStatus: undefined as number | undefined,
  stockOutProgressStatus: undefined as number | undefined,
  receiptProgressStatus: undefined as number | undefined,
  invoiceProgressStatus: undefined as number | undefined
})

const activePreset = computed((): SoItemListPresetId | null => {
  const p = route.query.preset
  return typeof p === 'string' && isSoItemListPresetId(p) ? p : null
})

const presetActive = computed(() => !!activePreset.value)

/** preset 打开时隐藏进度类页签模式项，仅保留币别 */
const visibleTabModeMenuOptions = computed(() =>
  presetActive.value
    ? SO_ITEM_TAB_MODE_OPTIONS.filter((dim) => dim === 'currency')
    : SO_ITEM_TAB_MODE_OPTIONS
)

function enableFilterTabMode(dim: Exclude<SoItemTabModeDimension, 'off'>) {
  if (isProgressTabDimension(dim) && presetActive.value) return
  tabModeDimension.value = dim
  writeSoItemTabMode(dim)
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

const boardFilters = computed((): SalesOrderItemListAnalyticsQuery => {
  const q: SalesOrderItemListAnalyticsQuery = {}
  if (dateRange.value?.[0]) q.orderCreateStart = dateRange.value[0]
  if (dateRange.value?.[1]) q.orderCreateEnd = dateRange.value[1]
  const qf = route.query.quickFilter
  if (typeof qf === 'string' && qf.trim() && activePreset.value) {
    q.quickFilter = qf.trim()
  }
  const soc = String(filters.sellOrderCode ?? '').trim()
  if (soc) q.sellOrderCode = soc
  if (listCustomerColumnOk.value) {
    const cn = String(filters.customerName ?? '').trim()
    if (cn) q.customerName = cn
    const cso = String(filters.customerSo ?? '').trim()
    if (cso) q.customerSo = cso
    const cpn = String(filters.customerPn ?? '').trim()
    if (cpn) q.customerPn = cpn
    const cid = customerIdFilter.value.trim()
    if (cid) q.customerId = cid
  }
  if (listSalesUserFilterOk.value) {
    const sun = String(filters.salesUserName ?? '').trim()
    if (sun) q.salesUserName = sun
    const suid = salesUserIdFilter.value.trim()
    if (suid) q.salesUserId = suid
  }
  const pnk = String(filters.pn ?? '').trim()
  if (pnk) q.pn = pnk
  const pua = String(filters.purchaseUserAccount ?? '').trim()
  if (pua) q.purchaseUserAccount = pua
  if (filters.transactionCurrency) q.transactionCurrency = filters.transactionCurrency
  if (!activePreset.value) {
    if (filters.purchaseProgressStatus !== undefined && filters.purchaseProgressStatus !== null) {
      q.purchaseProgressStatus = filters.purchaseProgressStatus
    }
    if (filters.stockInProgressStatus !== undefined && filters.stockInProgressStatus !== null) {
      q.stockInProgressStatus = filters.stockInProgressStatus
    }
    if (filters.stockOutNotifyProgressStatus !== undefined && filters.stockOutNotifyProgressStatus !== null) {
      q.stockOutNotifyProgressStatus = filters.stockOutNotifyProgressStatus
    }
    if (filters.stockOutProgressStatus !== undefined && filters.stockOutProgressStatus !== null) {
      q.stockOutProgressStatus = filters.stockOutProgressStatus
    }
    if (filters.receiptProgressStatus !== undefined && filters.receiptProgressStatus !== null) {
      q.receiptProgressStatus = filters.receiptProgressStatus
    }
    if (filters.invoiceProgressStatus !== undefined && filters.invoiceProgressStatus !== null) {
      q.invoiceProgressStatus = filters.invoiceProgressStatus
    }
    if (stockOutPending.value) q.stockOutPending = true
    if (invoicePending.value) q.invoicePending = true
  }
  return q
})

function toggleViewMode() {
  viewMode.value = viewMode.value === 'list' ? 'board' : 'list'
}

type ExtendProgressKind = 'purchase' | 'stockIn' | 'stockOut' | 'stockOutNotify' | 'receipt' | 'invoice'

function progressFilterOptions(kind: ExtendProgressKind) {
  const slots: Array<{ value: 0 | 1 | 2; slot: 'pending' | 'partial' | 'complete' }> = [
    { value: 0, slot: 'pending' },
    { value: 1, slot: 'partial' },
    { value: 2, slot: 'complete' }
  ]
  return slots.map(({ value, slot }) => ({
    value,
    label: t(`salesOrderItemList.extendProgress.${kind}.${slot}`)
  }))
}

type FilterTabId = SoItemCurrencyTabId | SoItemProgressTabId

/** 进度类页签在左栏 preset 打开时隐藏；币别页签仍显示 */
const filterTabStripVisible = computed(() => {
  const dim = tabModeDimension.value
  if (dim === 'off') return false
  if (isProgressTabDimension(dim) && presetActive.value) return false
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
      { id: 'all' as const, label: t('salesOrderItemList.currencyTabs.all') },
      { id: 'rmb' as const, label: t('salesOrderItemList.currencyTabs.rmb') },
      { id: 'foreign' as const, label: t('salesOrderItemList.currencyTabs.foreign') }
    ]
  }
  const kind = dim as ExtendProgressKind
  return [
    { id: 'all' as const, label: t('salesOrderItemList.currencyTabs.all') },
    ...progressFilterOptions(kind).map((opt) => ({
      id: String(opt.value) as SoItemProgressTabId,
      label: opt.label
    }))
  ]
})

const activeFilterTabId = computed((): FilterTabId => {
  const dim = tabModeDimension.value
  if (dim === 'currency') return currencyFilterToTab(filters.transactionCurrency)
  if (isProgressTabDimension(dim)) {
    const key = progressDimensionToFilterKey(dim)
    return progressFilterToTab(filters[key])
  }
  return 'all'
})

function onFilterTabClick(tab: FilterTabId) {
  const dim = tabModeDimension.value
  if (dim === 'currency') {
    const next = currencyTabToFilter(tab as SoItemCurrencyTabId)
    if (filters.transactionCurrency === next) return
    filters.transactionCurrency = next
    runSearch()
    return
  }
  if (!isProgressTabDimension(dim)) return
  const key = progressDimensionToFilterKey(dim)
  const next = progressTabToFilter(tab as SoItemProgressTabId)
  if (filters[key] === next) return
  filters[key] = next
  runSearch()
}

// ==============================
// 新建采购申请弹窗
// ==============================
const applyDialogVisible = ref(false)
const applyLoading = ref(false)
const applySubmitting = ref(false)
const applyFormRef = ref<FormInstance>()
const applyForm = reactive({
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
const applyRules = computed<FormRules>(() => ({
  requestQty: [{ required: true, message: t('salesOrderItemList.validation.requestQtyRequired'), trigger: 'change' }],
  expectedPurchaseDate: [
    { required: true, message: t('salesOrderItemList.validation.expectedDateRequired'), trigger: 'change' }
  ]
}))

const applyFormReset = () => {
  applyForm.sellOrderItemId = ''
  applyForm.pn = ''
  applyForm.brand = ''
  applyForm.salesOrderQty = 0
  applyForm.purchasedQty = 0
  applyForm.openPurchaseRequisitionQty = 0
  applyForm.remainingQty = 0
  applyForm.requestQty = 0
  applyForm.remark = ''
  applyForm.expectedPurchaseDate = new Date().toISOString().slice(0, 10)
}

const submitApply = async () => {
  if (!applyFormRef.value) return
  const ok = await validateElFormOrWarn(applyFormRef)
  if (!ok) return

  // 附加校验：不能超过可申请数量
  if (applyForm.requestQty <= 0) {
    ElMessage.warning(t('salesOrderItemList.validation.requestQtyPositive'))
    return
  }
  if (applyForm.requestQty > applyForm.remainingQty) {
    ElMessage.warning(t('salesOrderItemList.validation.requestQtyMax'))
    return
  }
  if (!applyForm.expectedPurchaseDate) {
    ElMessage.warning(t('salesOrderItemList.validation.expectedDatePick'))
    return
  }

  const created = await runSaveTask({
    loading: applySubmitting,
    task: async () => {
      const expectedPurchaseTime = `${applyForm.expectedPurchaseDate}T00:00:00.000Z`
      return purchaseRequisitionApi.create({
        sellOrderItemId: applyForm.sellOrderItemId,
        qty: applyForm.requestQty,
        expectedPurchaseTime,
        type: 0, // 0=专属；该弹窗不做类型选择
        remark: applyForm.remark || undefined
      })
    },
    formatSuccess: () => t('salesOrderItemList.messages.prCreated'),
    errorMessage: (e: unknown) => {
      const err = e as { response?: { data?: { message?: string } }; message?: string }
      return err?.response?.data?.message || err?.message || t('salesOrderItemList.messages.createFailed')
    }
  })
  if (!created) return
  applyDialogVisible.value = false
  await loadList()
}

function normSellOrderItemId(s: unknown) {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

async function applyPurchaseOne(row: any) {
  if (applyPurchaseDisabled(row)) {
    ElMessage.warning(t('salesOrderItemList.messages.prLineNotAvailable'))
    return
  }
  if (!mainAllowsOps(row)) {
    ElMessage.warning(t('salesOrderItemList.messages.applyPurchaseNeedAudit'))
    return
  }
  applyFormReset()
  applyDialogVisible.value = true
  applyLoading.value = true
  try {
    const sellOrderId = row.sellOrderId as string
    const sellOrderItemId = String(row.sellOrderItemId ?? row.id ?? row.Id ?? '').trim()

    const options = (await purchaseRequisitionApi.getLineOptions(sellOrderId)) || []
    const line = options.find((x: any) => normSellOrderItemId(x.sellOrderItemId) === normSellOrderItemId(sellOrderItemId))
    if (!line) {
      ElMessage.warning(t('salesOrderItemList.messages.prLineNotAvailable'))
      applyDialogVisible.value = false
      return
    }

    applyForm.sellOrderItemId = sellOrderItemId
    applyForm.pn = line.pn ?? row.pn ?? ''
    applyForm.brand = line.brand ?? row.brand ?? ''
    const toInt = (v: unknown) => Math.trunc(Number(v) || 0)
    applyForm.salesOrderQty = toInt(line.salesOrderQty ?? row.qty ?? 0)
    applyForm.purchasedQty = toInt(line.purchasedQty ?? 0)
    applyForm.openPurchaseRequisitionQty = toInt(line.openPurchaseRequisitionQty ?? 0)
    applyForm.remainingQty = toInt(line.remainingQty)
    applyForm.requestQty = Math.max(0, applyForm.remainingQty)
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.message || e?.message || t('salesOrderItemList.messages.loadLineFailed'))
    applyDialogVisible.value = false
  } finally {
    applyLoading.value = false
  }
}

// 将数字转为截图那种“输入框字符串效果”
const applyFormSalesOrderQtyText = computed(() => String(Math.trunc(Number(applyForm.salesOrderQty ?? 0) || 0)))
const applyFormPurchasedQtyText = computed(() => String(Math.trunc(Number(applyForm.purchasedQty ?? 0) || 0)))
const applyFormOpenPrQtyText = computed(() => String(Math.trunc(Number(applyForm.openPurchaseRequisitionQty ?? 0) || 0)))
const applyFormRemainingQtyText = computed(() => String(Math.trunc(Number(applyForm.remainingQty ?? 0) || 0)))

function statusText(s: number) {
  return translateSalesOrderStatus(s, t)
}

function statusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  return salesOrderStatusTagType(s) as '' | 'success' | 'warning' | 'info' | 'danger'
}

/** 明细扩展进度 0=待 1=部分 2=完成 */
function extendTriTagType(v?: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  const map: Record<number, '' | 'info' | 'success' | 'warning' | 'danger'> = {
    0: 'info',
    1: 'warning',
    2: 'success'
  }
  return v !== undefined && v !== null ? (map[v] ?? 'info') : 'info'
}

function extendTriLabel(
  kind: 'purchase' | 'stockIn' | 'stockOut' | 'stockOutNotify' | 'receipt' | 'invoice',
  v?: number
): string {
  const slot = v === 2 ? 'complete' : v === 1 ? 'partial' : 'pending'
  return t(`salesOrderItemList.extendProgress.${kind}.${slot}`)
}

function formatDt(v: string) {
  if (!v) return '—'
  const s = formatDisplayDateTime(v)
  return s === '--' ? '—' : s
}

function onSelectionChange(rows: any[]) {
  if (suppressBasketMerge.value) return
  basketStore.mergePageSelection(list.value as any[], rows as any[])
}

async function restoreTableSelectionFromBasket() {
  const table = dataTableRef.value
  if (!table) return
  suppressBasketMerge.value = true
  await nextTick()
  table.clearSelection()
  await nextTick()
  for (const row of list.value) {
    const id = String((row as any).sellOrderItemId ?? '').trim()
    if (id && basketStore.has(id)) {
      table.toggleRowSelection(row, true)
    }
  }
  await nextTick()
  suppressBasketMerge.value = false
}

function removeOneFromBasket(sellOrderItemId: string) {
  if (!sellOrderItemId) return
  basketStore.remove(sellOrderItemId)
  suppressBasketMerge.value = true
  const row = list.value.find((r) => String((r as any).sellOrderItemId ?? '').trim() === sellOrderItemId)
  if (row) {
    dataTableRef.value?.toggleRowSelection(row, false)
  }
  void nextTick(() => {
    suppressBasketMerge.value = false
  })
}

async function handleClearBasket() {
  if (!basketStore.count) return
  try {
    await ElMessageBox.confirm(
      t('salesOrderItemList.messages.clearBasketConfirm'),
      t('salesOrderItemList.messages.clearBasketTitle'),
      {
        type: 'warning',
        confirmButtonText: t('salesOrderItemList.messages.clearButton'),
        cancelButtonText: t('common.cancel')
      }
    )
  } catch {
    return
  }
  basketStore.clear()
  suppressBasketMerge.value = true
  dataTableRef.value?.clearSelection()
  await nextTick()
  suppressBasketMerge.value = false
  ElMessage.success(t('salesOrderItemList.messages.basketCleared'))
}

function runSearch() {
  page.value = 1
  router.replace({ name: 'SalesOrderItemList', query: buildRouteQueryFromFilters() })
}

function buildRouteQueryFromFilters(): Record<string, string> {
  const keywords: Record<string, string> = {}
  const soc = String(filters.sellOrderCode ?? '').trim()
  if (soc) keywords.sellOrderCode = soc
  if (listCustomerColumnOk.value) {
    const cn = String(filters.customerName ?? '').trim()
    if (cn) keywords.customerName = cn
    const cso = String(filters.customerSo ?? '').trim()
    if (cso) keywords.customerSo = cso
    const cpn = String(filters.customerPn ?? '').trim()
    if (cpn) keywords.customerPn = cpn
  }
  if (listSalesUserFilterOk.value) {
    const sun = String(filters.salesUserName ?? '').trim()
    if (sun) keywords.salesUserName = sun
  }
  const pnk = String(filters.pn ?? '').trim()
  if (pnk) keywords.pn = pnk
  const pua = String(filters.purchaseUserAccount ?? '').trim()
  if (pua) keywords.purchaseUserAccount = pua
  if (filters.transactionCurrency) keywords.transactionCurrency = filters.transactionCurrency

  if (activePreset.value) {
    return buildSoItemListRouteQuery({ preset: activePreset.value, keywords })
  }

  const advanced: Record<string, string> = {}
  if (dateRange.value?.[0]) advanced.orderCreateStart = dateRange.value[0]
  if (dateRange.value?.[1]) advanced.orderCreateEnd = dateRange.value[1]
  if (filters.purchaseProgressStatus !== undefined && filters.purchaseProgressStatus !== null) {
    advanced.purchaseProgressStatus = String(filters.purchaseProgressStatus)
  }
  if (filters.stockInProgressStatus !== undefined && filters.stockInProgressStatus !== null) {
    advanced.stockInProgressStatus = String(filters.stockInProgressStatus)
  }
  if (filters.stockOutNotifyProgressStatus !== undefined && filters.stockOutNotifyProgressStatus !== null) {
    advanced.stockOutNotifyProgressStatus = String(filters.stockOutNotifyProgressStatus)
  }
  if (filters.stockOutProgressStatus !== undefined && filters.stockOutProgressStatus !== null) {
    advanced.stockOutProgressStatus = String(filters.stockOutProgressStatus)
  }
  if (filters.receiptProgressStatus !== undefined && filters.receiptProgressStatus !== null) {
    advanced.receiptProgressStatus = String(filters.receiptProgressStatus)
  }
  if (filters.invoiceProgressStatus !== undefined && filters.invoiceProgressStatus !== null) {
    advanced.invoiceProgressStatus = String(filters.invoiceProgressStatus)
  }

  return buildSoItemListRouteQuery({ keywords, advanced })
}

function clearPresetChip() {
  router.replace({ name: 'SalesOrderItemList', query: {} })
}

function onPageChange() {
  void loadList()
}

function onPageSizeChange(next: number) {
  pageSize.value = Math.min(100, next)
  page.value = 1
  void loadList()
}

async function loadList() {
  loading.value = true
  try {
    const params: Record<string, unknown> = {
      page: page.value,
      pageSize: Math.min(100, pageSize.value)
    }
    if (dateRange.value?.[0]) params.orderCreateStart = dateRange.value[0]
    if (dateRange.value?.[1]) params.orderCreateEnd = dateRange.value[1]
    const soc = String(filters.sellOrderCode ?? '').trim()
    if (soc) params.sellOrderCode = soc
    if (!maskSaleSensitiveFields.value) {
      const cn = String(filters.customerName ?? '').trim()
      if (cn) params.customerName = cn
      const sun = String(filters.salesUserName ?? '').trim()
      if (sun) params.salesUserName = sun
    }
    const pnk = String(filters.pn ?? '').trim()
    if (pnk) params.pn = pnk
    const pua = String(filters.purchaseUserAccount ?? '').trim()
    if (pua) params.purchaseUserAccount = pua
    if (listCustomerColumnOk.value) {
      const cso = String(filters.customerSo ?? '').trim()
      if (cso) params.customerSo = cso
      const cpn = String(filters.customerPn ?? '').trim()
      if (cpn) params.customerPn = cpn
    }
    if (filters.transactionCurrency) params.transactionCurrency = filters.transactionCurrency
    const qf = route.query.quickFilter
    if (typeof qf === 'string' && qf.trim() && activePreset.value) {
      params.quickFilter = qf.trim()
    } else if (!activePreset.value) {
      if (filters.purchaseProgressStatus !== undefined && filters.purchaseProgressStatus !== null) {
        params.purchaseProgressStatus = filters.purchaseProgressStatus
      }
      if (filters.stockInProgressStatus !== undefined && filters.stockInProgressStatus !== null) {
        params.stockInProgressStatus = filters.stockInProgressStatus
      }
      if (filters.stockOutNotifyProgressStatus !== undefined && filters.stockOutNotifyProgressStatus !== null) {
        params.stockOutNotifyProgressStatus = filters.stockOutNotifyProgressStatus
      }
      if (filters.stockOutProgressStatus !== undefined && filters.stockOutProgressStatus !== null) {
        params.stockOutProgressStatus = filters.stockOutProgressStatus
      }
      if (filters.receiptProgressStatus !== undefined && filters.receiptProgressStatus !== null) {
        params.receiptProgressStatus = filters.receiptProgressStatus
      }
      if (filters.invoiceProgressStatus !== undefined && filters.invoiceProgressStatus !== null) {
        params.invoiceProgressStatus = filters.invoiceProgressStatus
      }
      if (stockOutPending.value) params.stockOutPending = true
      if (invoicePending.value) params.invoicePending = true
    }
    const suid = salesUserIdFilter.value.trim()
    if (suid) params.salesUserId = suid
    const cid = customerIdFilter.value.trim()
    if (cid) params.customerId = cid

    const data = (await salesOrderApi.getItemLines(params)) as {
      items?: any[]
      total?: number
      page?: number
      pageSize?: number
    }
    const items = data.items ?? []
    const nTotal = data.total ?? 0
    if (page.value > 1 && items.length === 0 && nTotal > 0) {
      page.value = 1
      await loadList()
      return
    }
    list.value = items
    total.value = nTotal
    if (typeof data.page === 'number' && data.page >= 1) page.value = data.page
    if (typeof data.pageSize === 'number' && data.pageSize >= 1) pageSize.value = Math.min(100, data.pageSize)
  } catch (e: any) {
    ElMessage.error(e?.message || t('salesOrderItemList.messages.loadListFailed'))
  } finally {
    loading.value = false
  }
  await nextTick()
  await restoreTableSelectionFromBasket()
  resetListRightPanelOnReload(salesOrderItemOpsStore)
}

function resetFilters() {
  page.value = 1
  basketStore.clear()
  suppressBasketMerge.value = true
  dataTableRef.value?.clearSelection()
  void nextTick(() => {
    suppressBasketMerge.value = false
  })
  router.replace({ name: 'SalesOrderItemList', query: {} })
}

function goDetail(row: any) {
  router.push({ name: 'SalesOrderDetail', params: { id: row.sellOrderId } })
}

function onSalesOrderItemListRowDblClick(row: any, _column: unknown, event?: MouseEvent) {
  onCrmDetailListRowDblClick(row, _column, event, {
    canEdit: canWriteSo.value,
    onEdit: goEdit,
    onDefault: goDetail,
  })
}

const { onOpsPanelRowClick } = useListRightOpsPanelInteraction({
  workspaceLayout,
  isActiveRoute: () => route.name === 'SalesOrderItemList',
  hasSelectedRow: () => !!salesOrderItemOpsStore.row,
  setRowOnly: (row) => salesOrderItemOpsStore.setRowOnly(row),
  selectRow: (row) =>
    salesOrderItemOpsStore.selectRow(row, t('salesOrderItemList.messages.loadLineFailed')),
  loadSelected: () => {
    void salesOrderItemOpsStore.loadAggregates(t('salesOrderItemList.messages.loadLineFailed'))
  },
  shouldBlockRowClick: () => maskSaleSensitiveFields.value
})

async function onRowClick(row: Record<string, unknown>) {
  await onOpsPanelRowClick(row)
}

function opsPanelRowClassName({ row }: { row: Record<string, unknown> }) {
  if (!salesOrderItemOpsStore.row) return ''
  return salesOrderItemOpsStore.rowKey(row) === salesOrderItemOpsStore.rowKey(salesOrderItemOpsStore.row)
    ? 'so-item-row--active'
    : ''
}

watch(maskSaleSensitiveFields, (masked) => {
  if (masked) salesOrderItemOpsStore.clear()
})

onMounted(() => {
  salesOrderItemOpsStore.registerHandlers({
    applyPurchase: (row) => {
      void applyPurchaseOne(row)
    },
    applyStockOut: (row) => {
      applyStockOutOne(row)
    }
  })
})

onBeforeUnmount(() => {
  salesOrderItemOpsStore.unregisterHandlers()
  salesOrderItemOpsStore.clear()
})

function goEdit(row: any) {
  router.push({ name: 'SalesOrderEdit', params: { id: String(row.sellOrderId) } })
}

function navigateNewPr(sellOrderId: string, itemIds: string[]) {
  const q: Record<string, string> = { sellOrderId }
  if (itemIds.length) q.itemIds = itemIds.join(',')
  router.push({ path: '/purchase-requisitions/new', query: q })
}

function batchApplyPurchase() {
  const rows = basketStore.items as any[]
  if (!rows.length) {
    ElMessage.warning(t('salesOrderItemList.messages.basketNeedRows'))
    return
  }
  if (!rows.every((r) => mainAllowsOps(r))) {
    ElMessage.warning(t('salesOrderItemList.messages.applyPurchaseNeedAudit'))
    return
  }
  if (rows.length === 1) {
    // 1条时走弹窗，避免跳到可能未完善的路由页面
    applyPurchaseOne(rows[0])
    return
  }

  ElMessage.warning(t('salesOrderItemList.messages.batchNotImplemented'))
  return

  const orderIds = new Set(rows.map((r) => r.sellOrderId))
  if (orderIds.size !== 1) {
    ElMessage.warning(t('salesOrderItemList.messages.batchSameOrderOnly'))
    return
  }
  if (canViewCustomer.value) {
    const cids = rows.map((r) => r.customerId).filter(Boolean)
    const names = rows.map((r) => r.customerName).filter(Boolean)
    if (cids.length === rows.length) {
      if (!cids.every((id) => id === cids[0])) {
        ElMessage.warning(t('salesOrderItemList.messages.sameCustomer'))
        return
      }
    } else if (names.length === rows.length) {
      if (!names.every((n) => n === names[0])) {
        ElMessage.warning(t('salesOrderItemList.messages.sameCustomer'))
        return
      }
    }
  }
  navigateNewPr(rows[0].sellOrderId, rows.map((r) => r.sellOrderItemId))
}

function applyStockOutDisabledHint(row: Record<string, unknown>): ApplyStockOutDisabledHintContent | null {
  return buildApplyStockOutDisabledHintContent(row, t)
}

function onApplyStockOutDropdownClick(row: Record<string, unknown>) {
  if (salesOrderLineApplyStockOutButtonDisabled(row)) return
  applyStockOutOne(row)
}

const applyStockOutDialogRef = ref<InstanceType<typeof ApplyStockOutDialog> | null>(null)

function onApplyStockOutSuccess() {
  void loadList()
}

function applyStockOutOne(row: Record<string, unknown>) {
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
  void applyStockOutDialogRef.value?.open(
    {
      salesOrderId: String(row.sellOrderId ?? ''),
      customerId: String(row.customerId ?? ''),
      customerName: String(row.customerName ?? ''),
      sellOrderCode: String(row.sellOrderCode ?? '')
    },
    row
  )
}

function parseProgressQuery(v: unknown): number | undefined {
  if (v === undefined || v === null || v === '') return undefined
  const n = Number(v)
  return Number.isNaN(n) ? undefined : n
}

function syncFiltersFromRoute() {
  if (route.name !== 'SalesOrderItemList') return
  const q = route.query
  filters.sellOrderCode = typeof q.sellOrderCode === 'string' ? q.sellOrderCode : ''
  filters.customerName = typeof q.customerName === 'string' ? q.customerName : ''
  filters.salesUserName = typeof q.salesUserName === 'string' ? q.salesUserName : ''
  filters.purchaseUserAccount = typeof q.purchaseUserAccount === 'string' ? q.purchaseUserAccount : ''
  filters.pn = typeof q.pn === 'string' ? q.pn : ''
  filters.customerSo = typeof q.customerSo === 'string' ? q.customerSo : ''
  filters.customerPn = typeof q.customerPn === 'string' ? q.customerPn : ''
  filters.transactionCurrency =
    q.transactionCurrency === 'rmb' || q.transactionCurrency === 'foreign' ? q.transactionCurrency : ''
  salesUserIdFilter.value = typeof q.salesUserId === 'string' ? q.salesUserId : ''
  customerIdFilter.value = typeof q.customerId === 'string' ? q.customerId : ''
  stockOutPending.value = q.stockOutPending === '1' || q.stockOutPending === 'true'
  invoicePending.value = q.invoicePending === '1' || q.invoicePending === 'true'

  const preset = activePreset.value
  if (preset) {
    filters.purchaseProgressStatus = undefined
    filters.stockInProgressStatus = undefined
    filters.stockOutNotifyProgressStatus = undefined
    filters.stockOutProgressStatus = undefined
    filters.receiptProgressStatus = undefined
    filters.invoiceProgressStatus = undefined
    stockOutPending.value = false
    invoicePending.value = false
    if (isSoItemTimePresetId(preset)) {
      dateRange.value = resolveSoItemTimePresetDateRange(preset)
    } else {
      dateRange.value = null
    }
    return
  }

  const from =
    typeof q.orderCreateStart === 'string'
      ? q.orderCreateStart
      : typeof q.startDate === 'string'
        ? q.startDate
        : ''
  const to =
    typeof q.orderCreateEnd === 'string' ? q.orderCreateEnd : typeof q.endDate === 'string' ? q.endDate : ''
  dateRange.value = from && to ? [from, to] : null
  filters.purchaseProgressStatus = parseProgressQuery(q.purchaseProgressStatus)
  filters.stockInProgressStatus = parseProgressQuery(q.stockInProgressStatus)
  filters.stockOutNotifyProgressStatus = parseProgressQuery(q.stockOutNotifyProgressStatus)
  filters.stockOutProgressStatus = parseProgressQuery(q.stockOutProgressStatus)
  filters.receiptProgressStatus = parseProgressQuery(q.receiptProgressStatus)
  filters.invoiceProgressStatus = parseProgressQuery(q.invoiceProgressStatus)
}

watch(
  () => [route.name, route.query] as const,
  async () => {
    syncFiltersFromRoute()
    if (route.name === 'SalesOrderItemList') await loadList()
  },
  { deep: true, immediate: true }
)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.so-item-list-page {
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
  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
  }
  .header-right {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-shrink: 0;
  }
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
.so-main-panel {
  width: 100%;
}
.so-main-panel--with-filter-tabs {
  .so-list-body,
  :deep(.so-item-list-board) {
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

  :deep(.so-item-list-board > .board-toolbar.card:first-child),
  :deep(.so-item-list-board > .section:first-child) {
    border-top-left-radius: 0;
    border-top-right-radius: 0;
  }
}
.so-filter-tabs {
  display: flex;
  align-items: stretch;
  width: 100%;
  margin: 0;
  padding: 0;
  gap: 4px;
}
.so-filter-tabs__item {
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

html[data-theme='dark'] .so-filter-tabs__item:not(.is-active) {
  background: var(--crm-layer-1);

  &:hover {
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }
}
.so-list-body {
  width: 100%;
}
.search-bar {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 12px;
}
.search-bar__row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
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
.filter-select {
  width: 130px;
  &.filter-select--progress {
    width: 148px;
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
.filter-date-range.so-date-range {
  width: 260px;
  :deep(.el-range-editor.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
}
.so-date-range {
  width: 260px;
}
.table-wrapper {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}
.table-footer-bar {
  flex-shrink: 0;
  margin-top: 12px;
  padding-top: 4px;
  display: flex;
  align-items: center;
  justify-content: flex-start;
  gap: 12px 16px;
  flex-wrap: wrap;
}

.basket-footer-left {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  flex-wrap: nowrap;
  flex-shrink: 0;
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
  width: 18px;
  flex: 0 0 18px;
}

.basket-open-btn {
  padding: 4px 6px 4px 8px !important;
  font-size: 13px;
  font-weight: 500;
}

.basket-clear-btn {
  padding: 4px 8px 4px 2px !important;
  font-size: 13px;
  font-weight: 500;
}

.basket-batch-purchase-btn {
  margin-left: 10px;
}

.basket-count-label {
  color: $cyan-primary;
  font-weight: 600;
  margin-left: 2px;
}

.table-footer-bar .quantum-pagination {
  margin-left: auto;
}

.quantum-pagination {
  :deep(.el-pagination__total) {
    color: $text-muted;
  }

  :deep(.el-pagination__sizes .el-select__wrapper) {
    background: $layer-2 !important;
    border: 1px solid $border-panel !important;
    box-shadow: none !important;
  }

  :deep(.el-pager li) {
    background: $layer-2;
    border: 1px solid $border-panel;
    color: $text-secondary;
    border-radius: 6px;
    margin: 0 2px;
  }

  :deep(.el-pager li.is-active) {
    background: rgba(0, 212, 255, 0.15);
    border-color: rgba(0, 212, 255, 0.4);
    color: $cyan-primary;
  }

  :deep(.btn-prev),
  :deep(.btn-next) {
    background: $layer-2 !important;
    border: 1px solid $border-panel !important;
    color: $text-secondary !important;
    border-radius: 6px !important;
  }
}

.op-col-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0;
  width: 100%;
}

.op-col-header-text {
  font-size: 12px;
  line-height: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.op-col-toggle-btn {
  padding: 0;
  border: none;
  background: transparent;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 16px;
  line-height: 1;
  flex: 0 0 auto;
}

.op-more-trigger {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  opacity: 0;
  transition: opacity 0.15s;
}

.op-more-item {
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
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

/* 展开操作列：禁用仍用 type=warning 时文字改为灰色 */
.action-btns :deep(.el-button.is-disabled.is-link.el-button--warning) {
  color: $text-muted !important;
  --el-button-hover-link-text-color: #{$text-muted};
}

:deep(.el-table__body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__body-wrapper .el-table__body tr.hover-row .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr.hover-row .op-more-trigger) {
  opacity: 1;
}
</style>

<!-- 抽屉挂载在 body，需单独样式块 -->
<style lang="scss">
@import '@/assets/styles/variables.scss';

.so-item-basket-drawer {
  .basket-drawer-hint {
    font-size: 13px;
    color: rgba(255, 255, 255, 0.55);
    line-height: 1.6;
    margin: 0 0 12px;
  }

  .basket-drawer-summary {
    font-size: 13px;
    color: rgba(232, 244, 255, 0.75);
    margin: 0 0 12px;
    line-height: 1.6;
  }

  .basket-clear-btn--drawer-inline {
    vertical-align: baseline;
    height: auto !important;
    min-height: 0 !important;
    padding: 0 2px !important;
    margin: 0 1px;
    font-size: 13px !important;
    font-weight: 500;
  }
}

.so-item-list-settings-popper.el-popover.el-popper {
  padding: 6px;
  min-width: 160px;
  overflow: visible;
}

.so-item-list-settings-menu {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.so-item-list-settings-menu__item {
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

.so-item-list-settings-menu__caret {
  margin-left: 8px;
  font-size: 12px;
  color: var(--crm-text-muted, rgba(200, 216, 232, 0.55));
}

.so-item-list-settings-menu__submenu {
  position: relative;
}

.so-item-list-settings-menu__flyout {
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
