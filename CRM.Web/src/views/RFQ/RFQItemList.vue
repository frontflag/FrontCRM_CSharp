<template>
  <div class="rfq-item-list-page customer-list-theme">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">明</div>
          <h1 class="page-title">{{ t('rfqItemList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('rfqItemList.count', { count: totalCount }) }}</div>
      </div>
      <button
        v-if="canOpenQuoteDesktop"
        type="button"
        class="btn-quote-desktop"
        @click="openQuoteDesktop"
      >
        <span>{{ t('quoteDesktop.openFromList') }}</span>
        <el-icon class="btn-quote-desktop__arrow"><ArrowRight /></el-icon>
      </button>
    </div>

    <!-- 搜索栏：与客户列表 CustomerList 同款布局与控件皮肤 -->
    <div class="search-bar">
      <div v-if="activePreset" class="search-preset-chip-row">
        <span class="search-preset-chip">
          {{ t(presetI18nKey(activePreset)) }}
          <button
            type="button"
            class="search-preset-chip__clear"
            :title="t('rfqItemList.searchPanel.clearPreset')"
            @click="clearPresetChip"
          >
            ×
          </button>
        </span>
      </div>
      <div class="search-left">
        <el-date-picker
          v-if="!presetActive"
          v-model="dateRange"
          type="daterange"
          :range-separator="t('rfqItemList.filters.to')"
          :start-placeholder="t('rfqItemList.filters.startDate')"
          :end-placeholder="t('rfqItemList.filters.endDate')"
          value-format="YYYY-MM-DD"
          clearable
          class="filter-date-range"
          :teleported="false"
        />
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="searchForm.rfqCode"
            class="search-input search-input--w140"
            :placeholder="t('rfqItemList.filters.rfqCodePlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-select
          v-if="!presetActive && tabModeDimension !== 'itemStatus'"
          v-model="searchForm.itemStatus"
          :placeholder="t('rfqItemList.filters.allItemStatuses')"
          clearable
          class="status-select status-select--item-status"
          :teleported="false"
        >
          <el-option :label="t('rfqItemList.status.pending')" :value="0" />
          <el-option :label="t('rfqItemList.status.quoted')" :value="1" />
          <el-option :label="t('rfqItemList.status.noQuote')" :value="5" />
          <el-option :label="t('rfqItemList.status.accepted')" :value="2" />
          <el-option :label="t('rfqItemList.status.rejected')" :value="3" />
          <el-option :label="t('rfqItemList.status.closed')" :value="4" />
        </el-select>
        <template v-if="canViewCustomerInRfq">
          <div class="search-input-wrap">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="searchForm.customerKeyword"
              class="search-input search-input--w180"
              :placeholder="t('rfqItemList.filters.customerPlaceholder')"
              @keyup.enter="handleSearch"
            />
          </div>
        </template>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="searchForm.materialModel"
            class="search-input search-input--w160"
            :placeholder="t('rfqItemList.filters.materialPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <BizBrandSelect
          v-model="searchForm.brandId"
          class="search-brand-select"
          :placeholder="t('rfqItemList.filters.brandPlaceholder')"
          :show-create-button="false"
        />
        <template v-if="showRfqSalesUserColumn">
          <el-select
            v-model="searchForm.salesUserId"
            :placeholder="t('rfqItemList.filters.allSalesUsers')"
            clearable
            filterable
            class="status-select status-select--sales"
            :teleported="false"
          >
            <el-option v-for="u in salesUsers" :key="u.id" :label="salesUserLabel(u)" :value="u.id" />
          </el-select>
        </template>
        <el-select
          v-model="searchForm.purchaserUserId"
          :placeholder="t('rfqItemList.filters.allPurchasers')"
          clearable
          filterable
          class="status-select status-select--purchase"
          :teleported="false"
        >
          <el-option v-for="u in purchaseUsers" :key="u.id" :label="purchaseUserLabel(u)" :value="u.id" />
        </el-select>
        <el-checkbox
          v-if="!presetActive"
          v-model="searchForm.hasQuotesOnly"
          class="filter-checkbox-has-quotes"
          border
          @change="handleSearch"
        >
          {{ t('rfqItemList.filters.hasQuotes') }}
        </el-checkbox>
        <button class="btn-primary btn-sm" type="button" @click="handleSearch">{{ t('rfqItemList.filters.query') }}</button>
        <button class="btn-ghost btn-sm" type="button" @click="handleReset">{{ t('rfqItemList.filters.reset') }}</button>
        <button
          class="btn-ghost btn-sm btn-board-active"
          type="button"
          @click="toggleViewMode"
        >
          {{ viewMode === 'board' ? t('rfqItemList.filters.listView') : t('rfqItemList.filters.boardView') }}
        </button>
        <el-popover
          v-model:visible="settingsMenuOpen"
          trigger="click"
          placement="bottom-end"
          :width="168"
          :show-arrow="false"
          popper-class="rfq-item-list-settings-popper"
        >
          <template #reference>
            <button
              type="button"
              class="btn-ghost btn-sm btn-icon-only"
              :title="t('rfqItemList.settingsMenu.aria')"
              :aria-label="t('rfqItemList.settingsMenu.aria')"
            >
              <el-icon :size="14"><Setting /></el-icon>
            </button>
          </template>
          <div class="rfq-item-list-settings-menu">
            <button
              type="button"
              class="rfq-item-list-settings-menu__item"
              :disabled="tabModeDimension === 'off'"
              @click="closeFilterTabMode"
            >
              {{ t('rfqItemList.settingsMenu.closeTabs') }}
            </button>
            <div
              v-if="visibleTabModeMenuOptions.length"
              class="rfq-item-list-settings-menu__submenu"
              @mouseenter="settingsSubmenuOpen = true"
              @mouseleave="settingsSubmenuOpen = false"
            >
              <div class="rfq-item-list-settings-menu__item rfq-item-list-settings-menu__item--parent">
                <span>{{ t('rfqItemList.settingsMenu.tabMode') }}</span>
                <el-icon class="rfq-item-list-settings-menu__caret"><ArrowRight /></el-icon>
              </div>
              <div v-show="settingsSubmenuOpen" class="rfq-item-list-settings-menu__flyout">
                <button
                  v-for="dim in visibleTabModeMenuOptions"
                  :key="dim"
                  type="button"
                  class="rfq-item-list-settings-menu__item"
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

    <div class="rfq-main-panel" :class="{ 'rfq-main-panel--with-filter-tabs': filterTabStripVisible }">
    <div
      v-if="filterTabStripVisible"
      class="rfq-filter-tabs"
      role="tablist"
      :aria-label="filterTabStripAriaLabel"
    >
      <button
        v-for="tab in filterTabOptions"
        :key="tab.id"
        type="button"
        role="tab"
        class="rfq-filter-tabs__item"
        :class="{ 'is-active': activeFilterTabId === tab.id }"
        :aria-selected="activeFilterTabId === tab.id"
        @click="onFilterTabClick(tab.id)"
      >
        {{ tab.label }}
      </button>
    </div>

    <div v-if="viewMode === 'board'" class="rfq-item-board-scroll">
      <RfqItemListBoard :filters="boardFilters" />
    </div>

    <div
      v-show="viewMode === 'list'"
      ref="rfqItemsSplitRootRef"
      class="rfq-items-split-root"
      :class="dockSplitRootClass"
      :style="dockSplitRootStyle"
    >
    <div class="rfq-item-main">
    <div class="rfq-item-table-panel">
      <div class="table-card-scroll rfq-items-main-table" v-loading="loading">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="rfq-item-list-main"
        :columns="rfqItemMainTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="tableData"
        row-key="id"
        highlight-current-row
        @row-click="onItemRowClick"
        @row-dblclick="onRfqItemRowDblClick"
        @selection-change="onSelectionChange"
      >
        <template #col-itemStatus="{ row }">
          <el-tag size="small" effect="dark" :type="itemStatusTagType(effectiveItemLineStatus(row))">
            {{ itemStatusText(effectiveItemLineStatus(row)) }}
          </el-tag>
        </template>
        <template #col-quoteCount="{ row }">
          <span
            class="rfq-item-quote-count"
            :class="{ 'rfq-item-quote-count--positive': (quoteRecordCountByRfqItemId[row.id] ?? 0) > 0 }"
          >
            {{ quoteRecordCountByRfqItemId[row.id] ?? 0 }}
          </span>
        </template>
        <template #col-materialModel="{ row }">
          <CrmListCopyableTextCell :text="row.materialModel || row.mpn || ''" />
        </template>
        <template #col-brand="{ row }">
          <CrmListCopyableTextCell :text="row.brand || ''" />
        </template>
        <template #col-customerPart="{ row }">
          {{ row.customerMaterialModel || row.customerMpn || '—' }}
        </template>
        <template #col-customerBrand="{ row }">
          {{ row.customerBrand || '—' }}
        </template>
        <template #col-quantity="{ row }">
          {{ row.quantity ?? '—' }}
        </template>
        <template #col-priceCurrency="{ row }">
          <span
            :class="['dock-tier-ccy', dockTierCurrencyCodeClass(resolveRfqItemPriceCurrency(row))]"
          >
            {{ dockTierCurrencyCode(resolveRfqItemPriceCurrency(row)) }}
          </span>
        </template>
        <template #col-purchasers="{ row }">
          {{ formatAssignedPurchasers(row) }}
        </template>
        <template #col-remark="{ row }">
          {{ row.remark?.trim() || '—' }}
        </template>
        <template #col-createTime="{ row }">
          <template
            v-for="p in [formatDisplayDateTime2DigitYearParts(row.createTime || row.rfqCreateTime)]"
            :key="`ct-main-${row.id}`"
          >
            <span v-if="p" class="crm-quote-create-time">
              <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
              <span class="crm-quote-create-time__hm">{{ p.time }}</span>
            </span>
            <span v-else>—</span>
          </template>
        </template>
        <template #col-createUser="{ row }">
          {{
            row.createUserName ||
              row.CreateUserName ||
              row.createdBy ||
              '—'
          }}
        </template>
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
          <div v-if="opColExpanded" @click.stop @dblclick.stop>
            <div class="action-btns">
              <button type="button" class="action-btn action-btn--primary" @click.stop="goDetail(row)">{{ t('rfqItemList.actions.detail') }}</button>
              <button type="button" class="action-btn" @click.stop="handleCopyRfqItemRow(row)">{{ t('rfqItemList.actions.copy') }}</button>
              <button
                v-if="canQuoteRfqItemRow(row)"
                type="button"
                class="action-btn action-btn--warning"
                @click.stop="goQuote(row)"
              >{{ t('rfqItemList.actions.quote') }}</button>
              <button
                v-if="canMarkNoQuoteRow(row)"
                type="button"
                class="action-btn action-btn--warning"
                @click.stop="handleMarkNoQuote(row)"
              >{{ t('rfqItemList.actions.markNoQuote') }}</button>
            </div>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger" @click.stop @dblclick.stop>
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="goDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('rfqItemList.actions.detail') }}</span>
                </el-dropdown-item>
                <el-dropdown-item @click.stop="handleCopyRfqItemRow(row)">
                  <span class="op-more-item">{{ t('rfqItemList.actions.copy') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canQuoteRfqItemRow(row)" @click.stop="goQuote(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('rfqItemList.actions.quote') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canMarkNoQuoteRow(row)" @click.stop="handleMarkNoQuote(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('rfqItemList.actions.markNoQuote') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </template>
      </CrmDataTable>
      </div>

      <div class="pagination-wrapper">
        <div class="list-footer-left">
          <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
            <el-button
              class="list-settings-btn"
              link
              type="primary"
              :aria-label="t('systemUser.colSetting')"
              @click="dataTableRef?.openColumnSettings?.()"
            >
              <el-icon><Setting /></el-icon>
            </el-button>
          </el-tooltip>
          <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />

          <div class="list-footer-spacer" aria-hidden="true"></div>

          <div class="basket-footer-left">
            <el-button class="basket-open-btn" link type="primary" @click="basketDrawerVisible = true">
              复选篮子<span v-if="basketCount" class="basket-count-label">（{{ basketCount }}）</span>
            </el-button>
            <el-button
              v-if="basketCount"
              class="basket-clear-btn"
              link
              type="warning"
              @click="handleClearBasket"
            >
              清空篮子
            </el-button>
          </div>
        </div>
        <el-pagination
          class="quantum-pagination"
          v-model:current-page="pageInfo.page"
          v-model:page-size="pageInfo.pageSize"
          :total="pageInfo.total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </div>
    </div>

    <div
      v-show="isDockSplitbarVisible"
      class="rfq-dock-splitbar"
      :class="{ 'is-dragging': isDockSplitDragging }"
      role="separator"
      :aria-label="t('rfqItemList.dockQuotes.dragSplit')"
      :aria-orientation="'horizontal'"
      @mousedown.prevent="startDockSplitDrag"
    >
      <span class="rfq-dock-splitbar__grip" aria-hidden="true" />
    </div>

    <!-- 底部：采购报价（当前选中需求明细对应的报价列表） -->
    <div class="supplier-quote-dock" :class="{ collapsed: isPurchaseQuoteDockCollapsed }">
      <div class="dock-header">
        <div class="dock-header-top">
          <div class="dock-header-main">
            <span class="dock-title">{{ t('rfqItemList.dockQuotes.title') }}</span>
            <!-- 与新建报价页提示栏同一套字段与拉数逻辑；与标题同一行 -->
            <div
              v-show="isPurchaseQuoteDockBodyVisible && selectedRfqItem"
              v-loading="dockSummaryLoading"
              class="dock-link-alert-wrap dock-link-alert-wrap--inline"
            >
              <div v-if="dockLinkAlert" class="dock-link-alert-title-row">
                <span class="la-block-rfq">
                  <span class="la-muted">报价需求</span><span class="la-pre">{{ linkAlertGap2 }}</span
                  ><span class="la-strong la-rfq-val">{{ dockLinkAlert.linkAlertRfqDisplay }}</span>
                </span>
                <span class="la-pre">{{ linkAlertSep8Ideo }}</span>
                <span class="la-block-detail">
                  <span class="la-muted">物料号</span><span class="la-pre">{{ linkAlertGap2 }}</span
                  ><span class="la-value-brown">{{ dockLinkAlert.mpn || '—' }}</span
                  ><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">品牌</span
                  ><span class="la-pre">{{ linkAlertGap2 }}</span
                  ><span class="la-value-brown">{{ dockLinkAlert.brand || '—' }}</span
                  ><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">数量</span
                  ><span class="la-pre">{{ linkAlertGap2 }}</span
                  ><span class="la-value-brown">{{ dockLinkAlert.quantityDisplay }}</span
                  ><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">目标价</span
                  ><span class="la-pre">{{ linkAlertGap2 }}</span
                  ><span class="la-value-brown">{{ dockLinkAlert.targetPriceText }}</span>
                </span>
              </div>
            </div>
          </div>
          <div class="dock-header-actions dock-layout-actions">
            <div
              v-if="selectedRfqItem && (canQuoteRfqItemRow(selectedRfqItem) || canMarkNoQuoteRow(selectedRfqItem))"
              class="dock-selected-row-actions"
            >
              <button
                v-if="canMarkNoQuoteRow(selectedRfqItem)"
                type="button"
                class="dock-row-action-btn dock-row-action-btn--no-quote"
                @click="handleMarkNoQuote(selectedRfqItem)"
              >
                {{ t('rfqItemList.actions.markNoQuote') }}
              </button>
              <button
                v-if="canQuoteRfqItemRow(selectedRfqItem)"
                type="button"
                class="dock-row-action-btn dock-row-action-btn--quote"
                @click="goQuote(selectedRfqItem)"
              >
                {{ t('rfqItemList.actions.quote') }}
              </button>
            </div>
            <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
              <el-button
                class="list-settings-btn dock-quote-col-settings-btn"
                link
                type="primary"
                :aria-label="t('systemUser.colSetting')"
                :disabled="!selectedRfqItem"
                @click="dockQuoteTableRef?.openColumnSettings?.()"
              >
                <el-icon><Setting /></el-icon>
              </el-button>
            </el-tooltip>
            <el-tooltip :content="t('rfqItemList.dockQuotes.layoutSideBySide')" placement="top" :hide-after="0">
              <el-button
                class="dock-layout-btn"
                :class="{ 'is-active': purchaseQuoteDockLayout === 'sideBySide' }"
                text
                circle
                :aria-pressed="purchaseQuoteDockLayout === 'sideBySide'"
                @click="setPurchaseQuoteDockLayout('sideBySide')"
              >
                <svg class="rfq-dock-layout-icon" viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
                  <rect x="4" y="3" width="6" height="18" rx="1.2" fill="currentColor" />
                  <rect x="14" y="3" width="6" height="18" rx="1.2" fill="currentColor" />
                </svg>
              </el-button>
            </el-tooltip>
            <el-tooltip :content="t('rfqItemList.dockQuotes.layoutStackHalf')" placement="top" :hide-after="0">
              <el-button
                class="dock-layout-btn"
                :class="{ 'is-active': purchaseQuoteDockLayout === 'stackHalf' }"
                text
                circle
                :aria-pressed="purchaseQuoteDockLayout === 'stackHalf'"
                @click="setPurchaseQuoteDockLayout('stackHalf')"
              >
                <svg class="rfq-dock-layout-icon" viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
                  <path fill="currentColor" d="M12 3.5 16 8H8l4-4.5zm0 7L16 15H8l4-4.5z" />
                </svg>
              </el-button>
            </el-tooltip>
            <el-tooltip :content="t('rfqItemList.dockQuotes.layoutStackCompact')" placement="top" :hide-after="0">
              <el-button
                class="dock-layout-btn"
                :class="{ 'is-active': purchaseQuoteDockLayout === 'stackCompact' }"
                text
                circle
                :aria-pressed="purchaseQuoteDockLayout === 'stackCompact'"
                @click="setPurchaseQuoteDockLayout('stackCompact')"
              >
                <svg class="rfq-dock-layout-icon" viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
                  <path fill="currentColor" d="M12 6.5 17.5 12H6.5L12 6.5z" />
                </svg>
              </el-button>
            </el-tooltip>
            <el-tooltip :content="t('rfqItemList.dockQuotes.layoutHeaderOnly')" placement="top" :hide-after="0">
              <el-button
                class="dock-layout-btn"
                :class="{ 'is-active': purchaseQuoteDockLayout === 'headerOnly' }"
                text
                circle
                :aria-pressed="purchaseQuoteDockLayout === 'headerOnly'"
                @click="setPurchaseQuoteDockLayout('headerOnly')"
              >
                <svg class="rfq-dock-layout-icon" viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
                  <rect x="5" y="11" width="14" height="2.5" rx="1" fill="currentColor" />
                </svg>
              </el-button>
            </el-tooltip>
          </div>
        </div>
      </div>
      <div v-show="isPurchaseQuoteDockBodyVisible" class="dock-body">
        <div v-if="!selectedRfqItem" class="dock-placeholder">{{ t('rfqItemList.dockQuotes.pickRowHint') }}</div>
        <template v-else>
          <div
            v-loading="quotesLoading"
            class="dock-table-wrap"
            :class="{ 'dock-table-wrap--quotes-empty': !quotesLoading && !quotesForItem.length }"
          >
            <CrmDataTable
              v-if="selectedRfqItem"
              ref="dockQuoteTableRef"
              embedded
              class="dock-quote-table"
              column-layout-key="rfq-item-list-dock-quotes"
              :columns="dockQuoteTableColumns"
              :show-column-settings="false"
              :show-row-density-toggle="false"
              :data="quotesForItem"
              size="small"
              stripe
              v-bind="dockQuoteTableExtraAttrs"
              :row-key="dockQuoteRowKey"
              @header-dragend="onDockQuoteTableHeaderDragEnd"
              @row-dblclick="onDockQuoteRowDblClick"
            >
              <template #empty>
                <div class="dock-quote-empty-row">{{ t('rfqItemList.dockQuotes.empty') }}</div>
              </template>
              <template #col-quoteCode="{ row }">
                {{ displayQuoteCode(row as Record<string, unknown>) }}
              </template>
              <template #col-brand="{ row }">
                {{ dockQuoteBrandDisplay(row as Record<string, unknown>) }}
              </template>
              <template #col-productionDateDc="{ row }">
                <div class="dock-quote-tiers dock-quote-tiers--left">
                  <template v-if="dockQuoteLineItems(row as Record<string, unknown>).length">
                    <div
                      v-for="(it, idx) in dockQuoteLineItems(row as Record<string, unknown>)"
                      :key="idx"
                      class="dock-quote-tier-line"
                    >
                      {{ formatDockTierDateCode(it.dateCode) }}
                    </div>
                  </template>
                  <span v-else class="dock-tier-empty">—</span>
                </div>
              </template>
              <template #col-leadTime="{ row }">
                <div class="dock-quote-tiers dock-quote-tiers--left">
                  <template v-if="dockQuoteLineItems(row as Record<string, unknown>).length">
                    <div
                      v-for="(it, idx) in dockQuoteLineItems(row as Record<string, unknown>)"
                      :key="idx"
                      class="dock-quote-tier-line"
                    >
                      {{ formatDockTierLeadTime(it.leadTime) }}
                    </div>
                  </template>
                  <span v-else class="dock-tier-empty">—</span>
                </div>
              </template>
              <template #col-vendorName="{ row }">
                {{ dockQuoteVendorNamesDisplay(row as Record<string, unknown>) }}
              </template>
              <template #col-vendorLevel="{ row }">
                {{ dockQuoteVendorLevelsDisplay(row as Record<string, unknown>) }}
              </template>
              <template #col-vendorTradeCount="{ row }">
                {{ dockQuoteVendorTradeCountsDisplay(row as Record<string, unknown>) }}
              </template>
              <template #col-quoteQty="{ row }">
                <div class="dock-quote-tiers">
                  <template v-if="dockQuoteLineItems(row as Record<string, unknown>).length">
                    <div
                      v-for="(it, idx) in dockQuoteLineItems(row as Record<string, unknown>)"
                      :key="idx"
                      class="dock-quote-tier-line"
                    >
                      {{ formatDockTierQuantity(it.quantity) }}
                    </div>
                  </template>
                  <span v-else class="dock-tier-empty">—</span>
                </div>
              </template>
              <template #col-unitPriceTiers="{ row }">
                <div class="dock-quote-tiers">
                  <template v-if="dockQuoteLineItems(row as Record<string, unknown>).length">
                    <div
                      v-for="(it, idx) in dockQuoteLineItems(row as Record<string, unknown>)"
                      :key="idx"
                      class="dock-quote-tier-line dock-tier-price-line"
                    >
                      <template v-if="!dockTierUnitPriceHasValue(it.unitPrice)">
                        —
                      </template>
                      <template v-else>
                        <template v-for="amt in [splitDockTierAmountParts(it.unitPrice)]" :key="idx + '-amt'">
                          <span class="dock-tier-amt">
                            <span class="dock-tier-amt-int">{{ amt.intPart }}</span
                            ><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
                          </span>
                        </template>
                        <span class="dock-tier-ccy-gap">&nbsp;</span>
                        <span :class="['dock-tier-ccy', dockTierCurrencyCodeClass(it.currency)]">{{
                          dockTierCurrencyCode(it.currency)
                        }}</span>
                      </template>
                    </div>
                  </template>
                  <span v-else class="dock-tier-empty">—</span>
                </div>
              </template>
              <template #col-dockQuoteExtend-header>
                <DockQuoteExtendColumnHeader
                  :active-field="dockQuoteExtendActiveField"
                  @set-active-field="setDockQuoteExtendActiveField"
                />
              </template>
              <template #col-dockQuoteExtend="{ row }">
                <DockQuoteExtendCell
                  :row="row as Record<string, unknown>"
                  :active-field="dockQuoteExtendActiveField"
                  :empty-text="t('quoteList.na')"
                />
              </template>
              <template #col-status="{ row }">
                <el-tag effect="dark" :type="quoteStatusType(row.status)" size="small">
                  {{ quoteStatusText(row.status) }}
                </el-tag>
              </template>
              <template #col-purchaser="{ row }">
                {{ dockQuotePurchaseUserDisplay(row as Record<string, unknown>) }}
              </template>
              <template #col-createTime="{ row }">
                <template
                  v-for="p in [formatDisplayDateTime2DigitYearParts((row as Record<string, unknown>).createTime as string)]"
                  :key="String(dockQuoteRowKey(row)) + '-ct'"
                >
                  <span v-if="p" class="crm-quote-create-time">
                    <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
                    <span class="crm-quote-create-time__hm">{{ p.time }}</span>
                  </span>
                  <span v-else>—</span>
                </template>
              </template>
              <template #col-remark="{ row }">
                {{ String((row as Record<string, unknown>).remark ?? '').trim() || '—' }}
              </template>
              <template #col-actions-header>
                <div class="list-op-col-header--icon-only">
                  <button
                    type="button"
                    class="op-col-toggle-btn list-op-col-toggle"
                    :aria-label="opDockColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
                    @click.stop="toggleOpDockCol"
                  >
                    {{ opDockColExpanded ? '>' : '<' }}
                  </button>
                </div>
              </template>
              <template #col-actions="{ row }">
                <div v-if="opDockColExpanded" @click.stop @dblclick.stop>
                  <div class="action-btns">
                    <el-button
                      class="action-btn"
                      link
                      size="small"
                      @click.stop="handleCopyDockQuote(row)"
                    >
                      {{ t('quoteList.actions.copy') }}
                    </el-button>
                    <el-button
                      v-if="canEditDockQuoteRow(row as Record<string, unknown>)"
                      class="action-btn action-btn--primary"
                      link
                      type="primary"
                      size="small"
                      @click.stop="goEditDockQuote(row)"
                    >
                      {{ t('rfqItemList.dockQuotes.edit') }}
                    </el-button>
                    <el-button
                      v-if="canDeleteDockQuoteRow(row as Record<string, unknown>)"
                      class="action-btn action-btn--danger"
                      link
                      type="danger"
                      size="small"
                      @click.stop="handleDeleteDockQuote(row)"
                    >
                      {{ t('quoteList.actions.delete') }}
                    </el-button>
                    <el-button
                      class="action-btn action-btn--warning"
                      link
                      type="warning"
                      size="small"
                      :loading="dockRowSalesOrderQuoteId === resolveQuoteRowId(row)"
                      @click.stop="handleDockRowGenerateSalesOrder(row)"
                    >
                      {{ t('rfqItemList.dockQuotes.genSalesOrder') }}
                    </el-button>
                  </div>
                </div>
                <el-dropdown v-else trigger="click" placement="bottom-end">
                  <div class="op-more-dropdown-trigger" @click.stop @dblclick.stop>
                    <button type="button" class="op-more-trigger">...</button>
                  </div>
                  <template #dropdown>
                    <el-dropdown-menu>
                      <el-dropdown-item @click.stop="handleCopyDockQuote(row)">
                        <span class="op-more-item">{{ t('quoteList.actions.copy') }}</span>
                      </el-dropdown-item>
                      <el-dropdown-item
                        v-if="canEditDockQuoteRow(row as Record<string, unknown>)"
                        @click.stop="goEditDockQuote(row)"
                      >
                        <span class="op-more-item op-more-item--primary">{{ t('rfqItemList.dockQuotes.edit') }}</span>
                      </el-dropdown-item>
                      <el-dropdown-item
                        v-if="canDeleteDockQuoteRow(row as Record<string, unknown>)"
                        @click.stop="handleDeleteDockQuote(row)"
                      >
                        <span class="op-more-item op-more-item--danger">{{ t('quoteList.actions.delete') }}</span>
                      </el-dropdown-item>
                      <el-dropdown-item @click.stop="handleDockRowGenerateSalesOrder(row)">
                        <span class="op-more-item op-more-item--warning">{{ t('rfqItemList.dockQuotes.genSalesOrder') }}</span>
                      </el-dropdown-item>
                    </el-dropdown-menu>
                  </template>
                </el-dropdown>
              </template>
            </CrmDataTable>
          <div v-if="deletedQuotesForItem.length" class="dock-deleted-quotes">
            <button
              type="button"
              class="dock-deleted-quotes__toggle"
              :aria-expanded="deletedQuotesExpanded"
              @click="deletedQuotesExpanded = !deletedQuotesExpanded"
            >
              <el-icon
                class="dock-deleted-quotes__chevron"
                :class="{ 'is-expanded': deletedQuotesExpanded }"
              >
                <ArrowRight />
              </el-icon>
              <span class="dock-deleted-quotes__title">{{
                t('rfqItemList.dockQuotes.deletedTitleWithCount', { count: deletedQuotesForItem.length })
              }}</span>
            </button>
            <el-table
              v-show="deletedQuotesExpanded"
              :data="deletedQuotesForItem"
              size="small"
              stripe
              class="dock-deleted-quotes__table"
            >
              <el-table-column :label="t('rfqItemList.dockQuotes.quoteCode')" min-width="120" show-overflow-tooltip>
                <template #default="{ row }">{{ row.quoteCode || '—' }}</template>
              </el-table-column>
              <el-table-column :label="t('rfqItemList.dockQuotes.mpn')" min-width="120" show-overflow-tooltip>
                <template #default="{ row }">{{ row.mpn || '—' }}</template>
              </el-table-column>
              <el-table-column :label="t('rfqItemList.dockQuotes.brand')" min-width="100" show-overflow-tooltip>
                <template #default="{ row }">{{ row.brand || '—' }}</template>
              </el-table-column>
              <el-table-column :label="t('rfqItemList.dockQuotes.productionDateDc')" width="120">
                <template #default="{ row }">
                  <div v-if="splitRfqDeletedQuoteAlignLines(row.dateCodeText).length" class="deleted-quote-stack">
                    <div v-for="(line, idx) in splitRfqDeletedQuoteAlignLines(row.dateCodeText)" :key="idx">{{
                      formatDockTierDateCode(line)
                    }}</div>
                  </div>
                  <span v-else>—</span>
                </template>
              </el-table-column>
              <el-table-column :label="t('rfqItemList.dockQuotes.leadTime')" min-width="100" show-overflow-tooltip>
                <template #default="{ row }">
                  <div v-if="splitRfqDeletedQuoteAlignLines(row.leadTimeText).length" class="deleted-quote-stack">
                    <div v-for="(line, idx) in splitRfqDeletedQuoteAlignLines(row.leadTimeText)" :key="idx">{{
                      formatDockTierLeadTime(line)
                    }}</div>
                  </div>
                  <span v-else>—</span>
                </template>
              </el-table-column>
              <el-table-column :label="t('rfqItemList.dockQuotes.deletedVendor')" min-width="120" show-overflow-tooltip>
                <template #default="{ row }">{{
                  maskPurchaseSensitiveFields ? '—' : row.vendorName || '—'
                }}</template>
              </el-table-column>
              <el-table-column :label="t('rfqItemList.dockQuotes.vendorLevel')" width="110" show-overflow-tooltip>
                <template #default="{ row }">{{ row.vendorLevel || '—' }}</template>
              </el-table-column>
              <el-table-column :label="t('rfqItemList.dockQuotes.quoteQty')" width="100" align="right">
                <template #default="{ row }">
                  <div
                    v-if="splitRfqDeletedQuoteAlignLines(row.quantityText).length"
                    class="deleted-quote-stack deleted-quote-stack--right"
                  >
                    <div v-for="(line, idx) in splitRfqDeletedQuoteAlignLines(row.quantityText)" :key="idx">{{
                      formatDeletedQuoteQuantityLine(line)
                    }}</div>
                  </div>
                  <span v-else>—</span>
                </template>
              </el-table-column>
              <el-table-column :label="t('rfqItemList.dockQuotes.quotePrice')" min-width="100" align="right">
                <template #default="{ row }">
                  <div
                    v-if="splitRfqDeletedQuoteMultiline(row.unitPriceText).length"
                    class="deleted-quote-stack deleted-quote-stack--right"
                  >
                    <div v-for="(line, idx) in splitRfqDeletedQuoteMultiline(row.unitPriceText)" :key="idx">{{
                      formatRfqDeletedQuotePriceLine(line)
                    }}</div>
                  </div>
                  <span v-else>—</span>
                </template>
              </el-table-column>
              <el-table-column :label="t('rfqItemList.dockQuotes.quoteCurrency')" width="72">
                <template #default="{ row }">
                  <div v-if="splitRfqDeletedQuoteMultiline(row.currencyText).length" class="deleted-quote-stack">
                    <div v-for="(line, idx) in splitRfqDeletedQuoteMultiline(row.currencyText)" :key="idx">{{ line }}</div>
                  </div>
                  <span v-else>—</span>
                </template>
              </el-table-column>
              <el-table-column :label="t('rfqItemList.dockQuotes.quoter')" width="110" show-overflow-tooltip>
                <template #default="{ row }">{{ row.purchaseUserName || '—' }}</template>
              </el-table-column>
              <el-table-column :label="t('rfqItemList.dockQuotes.createDate')" width="168">
                <template #default="{ row }">
                  <template
                    v-for="p in [formatDisplayDateTime2DigitYearParts(row.quoteCreatedAt)]"
                    :key="row.quoteId + '-cd'"
                  >
                    <span v-if="p" class="crm-quote-create-time">
                      <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
                      <span class="crm-quote-create-time__hm">{{ p.time }}</span>
                    </span>
                    <span v-else>—</span>
                  </template>
                </template>
              </el-table-column>
              <el-table-column :label="t('rfqItemList.dockQuotes.deletedBy')" width="110" show-overflow-tooltip>
                <template #default="{ row }">{{ row.deletedByUserName || '—' }}</template>
              </el-table-column>
              <el-table-column :label="t('rfqItemList.dockQuotes.deletedAt')" width="168">
                <template #default="{ row }">
                  <template
                    v-for="p in [formatDisplayDateTime2DigitYearParts(row.deletedAt)]"
                    :key="row.quoteId + '-del'"
                  >
                    <span v-if="p" class="crm-quote-create-time">
                      <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
                      <span class="crm-quote-create-time__hm">{{ p.time }}</span>
                    </span>
                    <span v-else>—</span>
                  </template>
                </template>
              </el-table-column>
            </el-table>
          </div>
          </div>
        </template>
      </div>
    </div>
    </div>
    </div>

    <el-drawer
      v-model="basketDrawerVisible"
      title="复选篮子"
      direction="rtl"
      size="min(560px, 94vw)"
      class="rfq-basket-drawer"
    >
      <p v-if="!basketCount" class="basket-drawer-hint">篮子里暂无记录。在列表中勾选行即可加入篮子，翻页后已选记录会保留。</p>
      <template v-else>
        <p class="basket-drawer-summary">
          共 <strong>{{ basketCount }}</strong> 条，可在此移除单条或点击
          <el-button
            class="basket-clear-btn basket-clear-btn--drawer-inline"
            link
            type="warning"
            @click="handleClearBasket"
          >
            清空篮子
          </el-button>
          全部清除。
        </p>
        <div class="crm-items-table crm-data-table">
          <el-table :data="basketItems" max-height="70vh" size="small" border stripe>
            <el-table-column prop="rfqCode" label="需求编号" min-width="140" show-overflow-tooltip />
            <el-table-column v-if="canViewCustomerInRfq" prop="customerName" label="客户" min-width="120" show-overflow-tooltip />
            <el-table-column label="物料型号" min-width="130">
              <template #default="{ row }">
                <CrmListCopyableTextCell :text="row.materialModel || row.mpn || ''" />
              </template>
            </el-table-column>
            <el-table-column prop="quantity" label="数量" width="72" align="right" />
            <el-table-column
              :label="t('rfqItemList.actions.column')"
              :width="rfqBasketOpColWidth"
              :min-width="rfqBasketOpColMinWidth"
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
                    :aria-label="rfqBasketOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
                    @click.stop="toggleRfqBasketOpCol"
                  >
                    {{ rfqBasketOpColExpanded ? '>' : '<' }}
                  </button>
                </div>
              </template>
              <template #default="{ row }">
                <div @click.stop @dblclick.stop>
                  <div v-if="rfqBasketOpColExpanded" class="action-btns">
                    <button type="button" class="action-btn action-btn--danger" @click.stop="removeOneFromBasket(row.id)">移除</button>
                  </div>
                  <el-dropdown v-else trigger="click" placement="bottom-end">
                    <div class="op-more-dropdown-trigger">
                      <button type="button" class="op-more-trigger">...</button>
                    </div>
                    <template #dropdown>
                      <el-dropdown-menu>
                        <el-dropdown-item @click.stop="removeOneFromBasket(row.id)">
                          <span class="op-more-item op-more-item--danger">移除</span>
                        </el-dropdown-item>
                      </el-dropdown-menu>
                    </template>
                  </el-dropdown>
                </div>
              </template>
            </el-table-column>
          </el-table>
        </div>
        <div class="basket-drawer-actions">
          <el-button type="primary" @click="handleBatchCopyBasket">
            {{ t('rfqItemList.basket.batchCopy') }}
          </el-button>
        </div>
      </template>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, reactive, onMounted, onUnmounted, onBeforeUnmount, nextTick, watch, inject } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { storeToRefs } from 'pinia'
import { rfqApi, type RfqItemDeletedQuoteRow, splitRfqDeletedQuoteMultiline, splitRfqDeletedQuoteAlignLines, formatRfqDeletedQuotePriceLine } from '@/api/rfq'
import { quoteApi } from '@/api/quote'
import { buildLinkAlertFieldsFromItem, fetchLinkedRfqItemRecord } from '@/utils/rfqLinkedItemSummary'
import { assertQuotesSameCustomer } from '@/utils/quoteSalesOrderPrefill'
import { RFQItemStatus, type RFQItem } from '@/types/rfq'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { authApi, type PurchaseUserSelectOption, type SalesUserSelectOption } from '@/api/auth'
import { useAuthStore } from '@/stores/auth'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { productionDateDisplayLabel, useMaterialProductionDateDict } from '@/composables/useMaterialProductionDateDict'
import { useRfqItemListBasketStore } from '@/stores/rfqItemListBasket'
import { canAccessQuoteDesktop, canQuoteRfqItem } from '@/utils/rfqItemQuoteAccessRules'
import { copyQuoteSummaryToClipboard } from '@/utils/quoteSummaryCopy'
import { quoteVendorNamesDisplay, quoteVendorLevelsDisplay, quoteVendorTradeCountsDisplay } from '@/utils/quoteVendorDisplay'
import { copyTextToClipboard } from '@/utils/clipboard'
import { useVendorDictStore } from '@/stores/vendorDict'
import {
  consumeRfqItemListRestoreState,
  saveRfqItemListRestoreState
} from '@/utils/rfqItemListRestore'
import { resolveRfqItemMaterialPn } from '@/utils/materialPn'
import { useMaterialIntelLookupStore } from '@/stores/materialIntelLookup'
import { useCustomerWorkspacePanelStore } from '@/stores/customerWorkspacePanel'
import { resetListRightPanelOnReload } from '@/composables/useListRightPanelReset'
import { useListRightOpsPanelInteraction } from '@/composables/useListRightOpsPanelInteraction'
import { useListBoardHelpOverride } from '@/composables/useHelpDocOverride'
import { AI_PERMISSION_MATERIAL_INTEL_LOOKUP } from '@/api/ai'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import {
  effectiveRfqItemLineStatus,
  rfqItemStatusTagType,
  RFQ_ITEM_STATUS_I18N_KEYS,
} from '@/utils/rfqItemLineStatus'
import CrmDataTable from '@/components/CrmDataTable.vue'
import BizBrandSelect from '@/components/Biz/BizBrandSelect.vue'
import RfqItemListBoard from './RfqItemListBoard.vue'
import type { RfqItemListAnalyticsQuery } from '@/api/rfqItemAnalytics'
import DockQuoteExtendColumnHeader from '@/components/list/DockQuoteExtendColumnHeader.vue'
import DockQuoteExtendCell from '@/components/list/DockQuoteExtendCell.vue'
import {
  useDockQuoteExtendColumn,
  isDockQuoteExtendTableColumn
} from '@/composables/useDockQuoteExtendColumn'
import {
  quoteMainStatusI18nKey,
  quoteMainStatusTagType,
  isQuoteReadOnly,
  isQuoteDeleteForbidden
} from '@/utils/quoteMainStatus'
import { useQuoteListBasketStore } from '@/stores/quoteListBasket'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { onCrmDetailListRowDblClick } from '@/utils/crmDetailListRowDblClick'
import { ArrowRight, Setting } from '@element-plus/icons-vue'
import {
  RFQ_ITEM_TAB_MODE_OPTIONS,
  RFQ_ITEM_STATUS_TAB_VALUES,
  readRfqItemTabMode,
  writeRfqItemTabMode,
  itemStatusFilterToTab,
  itemStatusTabToFilter,
  type RfqItemTabModeDimension,
  type RfqItemStatusTabId
} from '@/utils/rfqItemListTabMode'
import {
  buildRfqItemListRouteQuery,
  isRfqItemListPresetId,
  pickRfqItemKeywordQuery,
  presetI18nKey,
  resolveRfqItemPresetApiParams,
  type RfqItemListPresetId
} from '@/utils/rfqItemListPreset'

const router = useRouter()
const route = useRoute()
const { t, locale } = useI18n()

function openQuoteDesktop() {
  router.push({ name: 'QuoteDesktop' })
}
const authStore = useAuthStore()
/** 无权报价账号不展示入口（与行内「报价」/后端 CanQuote 账号侧能力一致） */
const canOpenQuoteDesktop = computed(() => canAccessQuoteDesktop(authStore.user))
const canEditRfq = computed(() => authStore.hasPermission('rfq.write'))
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const materialIntelLookupStore = useMaterialIntelLookupStore()
const customerWorkspacePanelStore = useCustomerWorkspacePanelStore()
customerWorkspacePanelStore.setSource('rfqItem')
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const vendorDict = useVendorDictStore()
const { onOpsPanelRowClick } = useListRightOpsPanelInteraction({
  workspaceLayout,
  isActiveRoute: () => route.name === 'RFQItemList',
  hasSelectedRow: () => !!customerWorkspacePanelStore.boundId,
  setRowOnly: row => customerWorkspacePanelStore.setRowOnly({ id: String(row.id ?? '') }),
  selectRow: row =>
    customerWorkspacePanelStore.selectRow({ id: String(row.id ?? '') }, t('customerWorkspace.loadFailed')),
  loadSelected: () => {
    void customerWorkspacePanelStore.load(t('customerWorkspace.loadFailed'))
  },
  dataTabIds: ['r-customer']
})
const {
  activeField: dockQuoteExtendActiveField,
  colWidth: dockQuoteExtendColWidth,
  colMinWidth: dockQuoteExtendColMinWidth,
  setActiveField: setDockQuoteExtendActiveField,
  applyOuterWidthFromTable: applyDockQuoteExtendOuterWidth
} = useDockQuoteExtendColumn()

function onDockQuoteTableHeaderDragEnd(
  newWidth: number,
  _oldWidth: number,
  column: { property?: string; label?: string }
) {
  if (!isDockQuoteExtendTableColumn(column)) return
  applyDockQuoteExtendOuterWidth(newWidth)
}

const { options: materialPdOptions, ensureLoaded: ensureMaterialPdDict } = useMaterialProductionDateDict()
/** 与后端 RFQ 脱敏一致：采购等角色可有 customer.read 但不应见需求侧客户名/客户料号筛选（需 customer.info.read）；§5.2.1 时强制不可见 */
const canViewCustomerInRfq = computed(
  () => authStore.hasPermission('customer.info.read') && !maskSaleSensitiveFields.value
)
/** 需求明细列表：采购部门需见主表业务员以便协同询价（§5.2.1 仍脱敏客户身份等） */
const showRfqSalesUserColumn = computed(() => true)

function canQuoteRfqItemRow(row: RFQItem): boolean {
  return canQuoteRfqItem(authStore.user, row)
}

function canMarkNoQuoteRow(row: RFQItem): boolean {
  if (!canQuoteRfqItemRow(row)) return false
  const qc = quoteRecordCountByRfqItemId.value[row.id] ?? 0
  // 与「明细状态」列同一口径：仅待报价且报价条数为 0
  return effectiveRfqItemLineStatus(row.status, qc) === RFQItemStatus.Pending
}

/** 需求明细列表：按当前筛选与分页自动刷新间隔 */
const RFQ_ITEM_LIST_AUTO_REFRESH_MS = 5 * 60 * 1000
/** 浏览器 setInterval 句柄；显式用 number 避免与 Node Timeout 类型冲突 */
let rfqItemListAutoRefreshTimer: number | null = null

const basketStore = useRfqItemListBasketStore()
const quoteListBasketStore = useQuoteListBasketStore()
const { count: basketCount, items: basketItems } = storeToRefs(basketStore)

const loading = ref(false)
const viewMode = ref<'list' | 'board'>('list')
useListBoardHelpOverride('pages/需求明细看板_MENU_RFQ_ITEMS_BOARD.md', viewMode)
const tabModeDimension = ref<RfqItemTabModeDimension>(readRfqItemTabMode())
const settingsMenuOpen = ref(false)
const settingsSubmenuOpen = ref(false)

const activePreset = computed((): RfqItemListPresetId | null => {
  const p = route.query.preset
  return typeof p === 'string' && isRfqItemListPresetId(p) ? p : null
})
const presetActive = computed(() => !!activePreset.value)

/** preset 打开时隐藏明细状态页签模式入口（与销售进度类互斥同理） */
const visibleTabModeMenuOptions = computed(() =>
  presetActive.value ? [] : RFQ_ITEM_TAB_MODE_OPTIONS
)

function clearPresetChip() {
  router.replace({ name: 'RFQItemList', query: {} })
}

function tabModeDimensionLabel(_dim: Exclude<RfqItemTabModeDimension, 'off'>) {
  return t('rfqItemList.filters.itemStatus')
}

function closeFilterTabMode() {
  if (tabModeDimension.value === 'off') return
  tabModeDimension.value = 'off'
  writeRfqItemTabMode('off')
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

function enableFilterTabMode(dim: Exclude<RfqItemTabModeDimension, 'off'>) {
  if (presetActive.value) return
  tabModeDimension.value = dim
  writeRfqItemTabMode(dim)
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

watch(settingsMenuOpen, (open) => {
  if (!open) settingsSubmenuOpen.value = false
})

const filterTabStripVisible = computed(
  () => tabModeDimension.value !== 'off' && !presetActive.value
)

const filterTabStripAriaLabel = computed(() => {
  if (tabModeDimension.value === 'off') return ''
  return tabModeDimensionLabel(tabModeDimension.value)
})

const filterTabOptions = computed(() => {
  if (tabModeDimension.value !== 'itemStatus') return [] as Array<{ id: RfqItemStatusTabId; label: string }>
  return [
    { id: 'all' as const, label: t('rfqItemList.filterTabs.all') },
    ...RFQ_ITEM_STATUS_TAB_VALUES.map((value) => ({
      id: String(value) as RfqItemStatusTabId,
      label: itemStatusText(value)
    }))
  ]
})

const activeFilterTabId = computed((): RfqItemStatusTabId => {
  if (tabModeDimension.value !== 'itemStatus') return 'all'
  return itemStatusFilterToTab(searchForm.itemStatus)
})

function onFilterTabClick(tab: RfqItemStatusTabId) {
  if (tabModeDimension.value !== 'itemStatus') return
  const next = itemStatusTabToFilter(tab)
  if (searchForm.itemStatus === next) return
  searchForm.itemStatus = next
  handleSearch()
}
const tableData = ref<RFQItem[]>([])
const totalCount = ref(0)

/** CrmDataTable 暴露的 el-table 方法 */
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const dockQuoteTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const suppressBasketMerge = ref(false)
const basketDrawerVisible = ref(false)

/** 全页列表操作列宽度（《列表操作列规范》高密度） */
const LIST_OP_COL_COLLAPSED_WIDTH = 43
/** 主表：详情 + 报价 + 查无报价 + 复制 四钮 */
const RFQ_ITEM_MAIN_OP_COL_EXPANDED_WIDTH = 320
const RFQ_ITEM_MAIN_OP_COL_EXPANDED_MIN_WIDTH = 300
/** 底部报价表 / 复选篮子：双钮或单钮 */
const LIST_OP_COL_EXPANDED_WIDTH = 173
const LIST_OP_COL_EXPANDED_MIN_WIDTH = 160

/** 《列表操作列规范》：复选篮子抽屉内表（列宽与主表一致） */
const rfqBasketOpColExpanded = ref(false)
const rfqBasketOpColWidth = computed(() =>
  rfqBasketOpColExpanded.value ? LIST_OP_COL_EXPANDED_WIDTH : LIST_OP_COL_COLLAPSED_WIDTH
)
const rfqBasketOpColMinWidth = computed(() =>
  rfqBasketOpColExpanded.value ? LIST_OP_COL_EXPANDED_MIN_WIDTH : LIST_OP_COL_COLLAPSED_WIDTH
)
function toggleRfqBasketOpCol() {
  rfqBasketOpColExpanded.value = !rfqBasketOpColExpanded.value
}
const dateRange = ref<[string, string] | null>(null)

const opColExpanded = ref(false)
const opColWidth = computed(() =>
  opColExpanded.value ? RFQ_ITEM_MAIN_OP_COL_EXPANDED_WIDTH : LIST_OP_COL_COLLAPSED_WIDTH
)
const opColMinWidth = computed(() =>
  opColExpanded.value ? RFQ_ITEM_MAIN_OP_COL_EXPANDED_MIN_WIDTH : LIST_OP_COL_COLLAPSED_WIDTH
)
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

/** 主表可配置列（列设置 / 顺序 / localStorage：crm-table-columns:v1:rfq-item-list-main） */
const rfqItemMainTableColumns = computed<CrmTableColumnDef[]>(() => {
  const cols: CrmTableColumnDef[] = [
  {
    key: 'sel',
    type: 'selection',
    width: 48,
    hideable: false,
    pinned: 'start',
    resizable: false,
    reserveSelection: true
  },
  {
    key: 'itemStatus',
    label: t('rfqItemList.columns.itemStatus'),
    width: 160,
    minWidth: 160,
    align: 'center',
    resizable: true
  },
  {
    key: 'quoteCount',
    label: t('rfqItemList.columns.quoteCount'),
    width: 120,
    minWidth: 112,
    align: 'center',
    resizable: true
  },
  ]
  if (canViewCustomerInRfq.value) {
    cols.push({
      key: 'customerName',
      label: t('rfqItemList.columns.customer'),
      prop: 'customerName',
      minWidth: 200,
      showOverflowTooltip: true,
      resizable: true
    })
  }
  cols.push(
  {
    key: 'customerPart',
    label: t('rfqItemList.columns.customerPart'),
    width: 120,
    minWidth: 118,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'customerBrand',
    label: t('rfqItemList.columns.customerBrand'),
    minWidth: 100,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'materialModel',
    label: t('rfqItemList.columns.materialModel'),
    minWidth: 120,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'brand',
    label: t('rfqItemList.columns.brand'),
    minWidth: 100,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'quantity',
    label: t('rfqItemList.columns.quantity'),
    prop: 'quantity',
    width: 108,
    minWidth: 96,
    align: 'right',
    resizable: true
  },
  {
    key: 'priceCurrency',
    label: t('rfqItemList.columns.priceCurrency'),
    width: 88,
    minWidth: 80,
    align: 'center',
    resizable: true
  }
  )
  if (showRfqSalesUserColumn.value) {
    cols.push({
      key: 'salesUserName',
      label: t('rfqItemList.columns.salesUser'),
      prop: 'salesUserName',
      width: 112,
      minWidth: 104,
      showOverflowTooltip: true,
      resizable: true
    })
  }
  cols.push(
  {
    key: 'purchasers',
    label: t('rfqItemList.columns.purchaser'),
    minWidth: 160,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'remark',
    label: t('rfqItemList.columns.remark'),
    prop: 'remark',
    minWidth: 160,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'rfqCode',
    label: t('rfqItemList.columns.rfqCode'),
    prop: 'rfqCode',
    width: 160,
    minWidth: 160,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'createTime',
    label: t('rfqItemList.columns.createTime'),
    width: 160,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'createUser',
    label: t('rfqItemList.columns.createUser'),
    width: 120,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'actions',
    label: t('rfqItemList.actions.column'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    hideable: false,
    pinned: 'end',
    reorderable: false,
    className: 'op-col',
    labelClassName: 'op-col',
    resizable: false
  }
  )
  if (!canViewCustomerInRfq.value) {
    return cols.filter(c => c.key !== 'customerPart' && c.key !== 'customerBrand')
  }
  return cols
})

// 底部采购报价表操作列：与主表同宽、同列头
const opDockColExpanded = ref(false)
const opDockColWidth = computed(() =>
  opDockColExpanded.value ? LIST_OP_COL_EXPANDED_WIDTH : LIST_OP_COL_COLLAPSED_WIDTH
)
const opDockColMinWidth = computed(() =>
  opDockColExpanded.value ? LIST_OP_COL_EXPANDED_MIN_WIDTH : LIST_OP_COL_COLLAPSED_WIDTH
)
function toggleOpDockCol() {
  opDockColExpanded.value = !opDockColExpanded.value
}

/** 采购报价面板可配置列（localStorage：crm-table-columns:v1:rfq-item-list-dock-quotes） */
const dockQuoteTableColumns = computed((): CrmTableColumnDef[] => {
  void locale.value
  void dockQuoteExtendColWidth.value
  void opDockColWidth.value
  return [
    {
      key: 'quoteCode',
      label: t('rfqItemList.dockQuotes.quoteCode'),
      width: 160,
      minWidth: 160,
      showOverflowTooltip: true,
      resizable: true
    },
    {
      key: 'mpn',
      label: t('rfqItemList.dockQuotes.mpn'),
      prop: 'mpn',
      minWidth: 120,
      showOverflowTooltip: true,
      resizable: true
    },
    {
      key: 'brand',
      label: t('rfqItemList.dockQuotes.brand'),
      minWidth: 100,
      width: 110,
      showOverflowTooltip: true,
      resizable: true
    },
    {
      key: 'productionDateDc',
      label: t('rfqItemList.dockQuotes.productionDateDc'),
      width: 120,
      minWidth: 104,
      showOverflowTooltip: true,
      className: 'dock-tier-col dock-tier-col--left',
      resizable: true
    },
    {
      key: 'leadTime',
      label: t('rfqItemList.dockQuotes.leadTime'),
      width: 120,
      minWidth: 100,
      showOverflowTooltip: true,
      className: 'dock-tier-col dock-tier-col--left',
      resizable: true
    },
    {
      key: 'vendorName',
      label: t('rfqItemList.dockQuotes.vendorName'),
      minWidth: 140,
      showOverflowTooltip: true,
      resizable: true
    },
    {
      key: 'vendorLevel',
      label: t('rfqItemList.dockQuotes.vendorLevel'),
      width: 110,
      minWidth: 96,
      showOverflowTooltip: true,
      resizable: true
    },
    {
      key: 'vendorTradeCount',
      label: t('rfqItemList.dockQuotes.vendorTradeCount'),
      width: 100,
      minWidth: 88,
      align: 'right' as const,
      showOverflowTooltip: true,
      resizable: true
    },
    {
      key: 'quoteQty',
      label: t('rfqItemList.dockQuotes.quoteQty'),
      width: 100,
      minWidth: 88,
      align: 'right',
      className: 'dock-tier-col',
      resizable: true
    },
    {
      key: 'unitPriceTiers',
      label: t('rfqItemList.dockQuotes.unitPriceTiers'),
      minWidth: 128,
      align: 'right',
      className: 'dock-tier-col',
      resizable: true
    },
    {
      key: 'dockQuoteExtend',
      label: t('common.dockQuoteExtendCol.columnTitle'),
      prop: 'dockQuoteExtend',
      width: dockQuoteExtendColWidth.value,
      minWidth: dockQuoteExtendColMinWidth.value,
      align: 'center',
      className: 'customer-extend-col dock-quote-extend-col',
      labelClassName: 'customer-extend-col dock-quote-extend-col',
      resizable: true
    },
    {
      key: 'status',
      label: t('rfqItemList.dockQuotes.status'),
      width: 96,
      align: 'center',
      resizable: true
    },
    {
      key: 'purchaser',
      label: t('rfqItemList.dockQuotes.purchaser'),
      width: 100,
      showOverflowTooltip: true,
      resizable: true
    },
    {
      key: 'createTime',
      label: t('rfqItemList.dockQuotes.createTime'),
      width: 160,
      showOverflowTooltip: true,
      resizable: true
    },
    {
      key: 'remark',
      label: t('rfqItemList.dockQuotes.remark'),
      prop: 'remark',
      minWidth: 160,
      showOverflowTooltip: true,
      resizable: true
    },
    {
      key: 'actions',
      label: t('rfqItemList.actions.column'),
      width: opDockColWidth.value,
      minWidth: opDockColMinWidth.value,
      align: 'center',
      fixed: 'right',
      hideable: false,
      pinned: 'end',
      reorderable: false,
      className: 'op-col',
      labelClassName: 'op-col',
      resizable: false
    }
  ]
})

const searchForm = reactive({
  customerKeyword: '',
  materialModel: '',
  brandId: null as number | null,
  rfqCode: '',
  itemStatus: undefined as number | undefined,
  salesUserId: undefined as string | undefined,
  purchaserUserId: undefined as string | undefined,
  hasQuotesOnly: false
})

function appendRfqItemListFilterParams(
  q: RfqItemListAnalyticsQuery | Record<string, unknown>
): void {
  const preset = activePreset.value
  if (preset) {
    const api = resolveRfqItemPresetApiParams(preset)
    if (api.itemCreateStart) q.itemCreateStart = api.itemCreateStart
    if (api.itemCreateEndExclusive) q.itemCreateEndExclusive = api.itemCreateEndExclusive
    if (api.quoteCreateStart) q.quoteCreateStart = api.quoteCreateStart
    if (api.quoteCreateEndExclusive) q.quoteCreateEndExclusive = api.quoteCreateEndExclusive
    if (api.quickFilter) q.quickFilter = api.quickFilter
  } else {
    if (dateRange.value?.[0]) q.startDate = dateRange.value[0]
    if (dateRange.value?.[1]) q.endDate = dateRange.value[1]
    if (searchForm.itemStatus !== undefined && searchForm.itemStatus !== null) {
      q.status = searchForm.itemStatus
    }
    if (searchForm.hasQuotesOnly) q.hasQuotesOnly = true
  }
  const ck = searchForm.customerKeyword.trim()
  if (ck) q.customerKeyword = ck
  const mm = searchForm.materialModel.trim()
  if (mm) q.materialModel = mm
  if (searchForm.brandId != null && searchForm.brandId > 0) q.brandId = searchForm.brandId
  const rc = searchForm.rfqCode.trim()
  if (rc) q.rfqCode = rc
  if (searchForm.salesUserId) q.salesUserId = searchForm.salesUserId
  if (searchForm.purchaserUserId) q.purchaserUserId = searchForm.purchaserUserId
}

const boardFilters = computed((): RfqItemListAnalyticsQuery => {
  const q: RfqItemListAnalyticsQuery = {}
  appendRfqItemListFilterParams(q)
  return q
})

function toggleViewMode() {
  viewMode.value = viewMode.value === 'list' ? 'board' : 'list'
}

const salesUsers = ref<SalesUserSelectOption[]>([])
const purchaseUsers = ref<PurchaseUserSelectOption[]>([])

function salesUserLabel(u: SalesUserSelectOption) {
  const name = u.realName || u.label || u.userName
  return u.userName && name !== u.userName ? `${name}（${u.userName}）` : name
}

function purchaseUserLabel(u: PurchaseUserSelectOption) {
  const name = u.realName || u.label || u.userName
  return u.userName && name !== u.userName ? `${name}（${u.userName}）` : name
}

const pageInfo = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

/** 当前点击选中的需求明细（用于底部采购报价面板） */
const selectedRfqItem = ref<RFQItem | null>(null)
/** 从报价页返回时恢复选中行（loadData 消费后清空） */
const pendingRestoreSelectedItemId = ref<string | undefined>()

/** 需求明细主表与采购报价面板的相对布局（持久化至 localStorage） */
type PurchaseQuoteDockLayout = 'sideBySide' | 'stackHalf' | 'stackCompact' | 'headerOnly'
const PURCHASE_QUOTE_DOCK_LAYOUT_STORAGE_KEY = 'crm:rfq-item-list:purchase-quote-dock-layout'
const PURCHASE_QUOTE_DOCK_SPLIT_RATIO_STORAGE_KEY = 'crm:rfq-item-list:purchase-quote-dock-split-ratio'
const DOCK_SPLIT_RATIO_MIN = 0.2
const DOCK_SPLIT_RATIO_MAX = 0.8
const DOCK_SPLIT_RATIO_DEFAULT = 0.5
const DOCK_SPLITBAR_HEIGHT_PX = 6
const PURCHASE_QUOTE_DOCK_LAYOUTS: PurchaseQuoteDockLayout[] = [
  'sideBySide',
  'stackHalf',
  'stackCompact',
  'headerOnly'
]
/** 紧凑模式下报价表 max-height（约 2 行数据 + 表头，small 表格） */
const DOCK_QUOTE_TABLE_MAX_HEIGHT_COMPACT = 128

function readPersistedDockSplitRatio(): number {
  try {
    const raw = localStorage.getItem(PURCHASE_QUOTE_DOCK_SPLIT_RATIO_STORAGE_KEY)
    const n = raw != null ? Number(raw) : NaN
    if (Number.isFinite(n) && n >= DOCK_SPLIT_RATIO_MIN && n <= DOCK_SPLIT_RATIO_MAX) return n
  } catch {
    /* ignore */
  }
  return DOCK_SPLIT_RATIO_DEFAULT
}

function readPersistedPurchaseQuoteDockLayout(): PurchaseQuoteDockLayout {
  try {
    const raw = localStorage.getItem(PURCHASE_QUOTE_DOCK_LAYOUT_STORAGE_KEY)
    if (raw && (PURCHASE_QUOTE_DOCK_LAYOUTS as string[]).includes(raw)) {
      return raw as PurchaseQuoteDockLayout
    }
  } catch {
    /* ignore */
  }
  return 'stackCompact'
}

const purchaseQuoteDockLayout = ref<PurchaseQuoteDockLayout>(readPersistedPurchaseQuoteDockLayout())
const dockSplitRatio = ref(readPersistedDockSplitRatio())
const rfqItemsSplitRootRef = ref<HTMLElement | null>(null)
const isDockSplitDragging = ref(false)

watch(purchaseQuoteDockLayout, (v) => {
  try {
    localStorage.setItem(PURCHASE_QUOTE_DOCK_LAYOUT_STORAGE_KEY, v)
  } catch {
    /* ignore */
  }
})

watch(dockSplitRatio, (v) => {
  try {
    localStorage.setItem(PURCHASE_QUOTE_DOCK_SPLIT_RATIO_STORAGE_KEY, String(v))
  } catch {
    /* ignore */
  }
})

function clampDockSplitRatio(raw: number): number {
  return Math.min(DOCK_SPLIT_RATIO_MAX, Math.max(DOCK_SPLIT_RATIO_MIN, raw))
}

function setPurchaseQuoteDockLayout(mode: PurchaseQuoteDockLayout) {
  purchaseQuoteDockLayout.value = mode
}

const isVerticalDockLayout = computed(
  () => purchaseQuoteDockLayout.value === 'stackHalf' || purchaseQuoteDockLayout.value === 'stackCompact'
)

const isDockSplitbarVisible = computed(() => isVerticalDockLayout.value)

const isDockSplitRatioLayout = computed(() => purchaseQuoteDockLayout.value === 'stackHalf')

const dockSplitRootStyle = computed(() => {
  if (!isDockSplitRatioLayout.value) return undefined
  return {
    '--dock-split-top': `${dockSplitRatio.value * 100}%`,
    '--dock-splitbar-size': `${DOCK_SPLITBAR_HEIGHT_PX}px`
  } as Record<string, string>
})

function startDockSplitDrag(ev: MouseEvent) {
  const root = rfqItemsSplitRootRef.value
  if (!root) return

  isDockSplitDragging.value = true
  purchaseQuoteDockLayout.value = 'stackHalf'

  const startY = ev.clientY
  const startRatio = dockSplitRatio.value
  const rect = root.getBoundingClientRect()
  const totalH = Math.max(1, rect.height - DOCK_SPLITBAR_HEIGHT_PX)

  const onMove = (e: MouseEvent) => {
    const dy = e.clientY - startY
    dockSplitRatio.value = clampDockSplitRatio(startRatio + dy / totalH)
  }

  const onUp = () => {
    isDockSplitDragging.value = false
    document.removeEventListener('mousemove', onMove)
    document.removeEventListener('mouseup', onUp)
    document.body.style.cursor = ''
    document.body.style.userSelect = ''
  }

  document.body.style.cursor = 'row-resize'
  document.body.style.userSelect = 'none'
  document.addEventListener('mousemove', onMove)
  document.addEventListener('mouseup', onUp)
}

const isPurchaseQuoteDockCollapsed = computed(() => purchaseQuoteDockLayout.value === 'headerOnly')
const isPurchaseQuoteDockBodyVisible = computed(() => purchaseQuoteDockLayout.value !== 'headerOnly')

const dockSplitRootClass = computed(() => ({
  'rfq-items-split-root--side': purchaseQuoteDockLayout.value === 'sideBySide',
  'rfq-items-split-root--stack-half': purchaseQuoteDockLayout.value === 'stackHalf',
  'rfq-items-split-root--stack-compact': purchaseQuoteDockLayout.value === 'stackCompact',
  'rfq-items-split-root--stack-resizable': isDockSplitRatioLayout.value,
  'rfq-items-split-root--dock-body-fill':
    purchaseQuoteDockLayout.value === 'sideBySide' || purchaseQuoteDockLayout.value === 'stackHalf'
}))

const dockQuoteTableExtraAttrs = computed(() =>
  purchaseQuoteDockLayout.value === 'stackCompact' ? { maxHeight: DOCK_QUOTE_TABLE_MAX_HEIGHT_COMPACT } : {}
)
const quotesForItem = ref<Record<string, unknown>[]>([])
const quotesLoading = ref(false)
const deletedQuotesForItem = ref<RfqItemDeletedQuoteRow[]>([])
const deletedQuotesExpanded = ref(false)
const expandDeletedQuotesAfterDelete = ref(false)
let deletedQuotesBoundItemId = ''
/** 正在预检并跳转生成销售订单的报价行 id（行内按钮 loading） */
const dockRowSalesOrderQuoteId = ref<string | null>(null)

/** 每条需求明细对应的报价单数量（报价头 rfqItemId 与明细 id 一致） */
const quoteRecordCountByRfqItemId = ref<Record<string, number>>({})

/** 底部面板提示条（与 QuoteCreate 提示栏字段一致） */
const dockSummaryLoading = ref(false)
const dockLinkAlert = ref<{
  linkAlertRfqDisplay: string
  mpn: string
  brand: string
  quantityDisplay: string
  targetPriceText: string
} | null>(null)

/** 与 QuoteCreate 提示栏排版一致 */
const linkAlertGap2 = '  '
const linkAlertSep8Ideo = '\u3000'.repeat(8)
const linkAlertSep4Ideo = '\u3000'.repeat(4)

/** 轮询分配的两名询价采购员展示 */
function formatAssignedPurchasers(row: RFQItem) {
  const n1 = row.assignedPurchaserName1?.trim()
  const n2 = row.assignedPurchaserName2?.trim()
  const parts = [n1, n2].filter((x): x is string => !!x)
  return parts.length ? parts.join('、') : '—'
}

function itemStatusText(s?: number | string) {
  const n = s === undefined || s === null || s === '' ? NaN : Number(s)
  const key = Number.isFinite(n) ? RFQ_ITEM_STATUS_I18N_KEYS[n] : undefined
  return key ? t(key) : t('quoteList.na')
}

/** 待报价单独灰色标签；查无报价黄色；其余沿用主题 primary（蓝色） */
function itemStatusTagType(s?: number | string) {
  return rfqItemStatusTagType(s)
}

/** 库内 status 未回写或接口未部署旧版时，与「报价条目」列一致：有条数则不应仍显示待报价 */
function effectiveItemLineStatus(row: RFQItem): number | undefined {
  const qc = quoteRecordCountByRfqItemId.value[row.id] ?? 0
  return effectiveRfqItemLineStatus(row.status, qc)
}

function quoteStatusText(status: number) {
  return t(quoteMainStatusI18nKey(status))
}

function quoteStatusType(status: number) {
  return quoteMainStatusTagType(status)
}

function canEditDockQuoteRow(row: Record<string, unknown>) {
  return !isQuoteReadOnly(row.status)
}

function canDeleteDockQuoteRow(row: Record<string, unknown>) {
  return !isQuoteDeleteForbidden(row.status)
}

function displayQuoteCode(row: Record<string, unknown>) {
  const v = row.quoteCode ?? row.quoteNumber ?? row.QuoteCode
  if (v != null && String(v).trim() !== '') return String(v)
  return '—'
}

function dockQuoteItemsRaw(quoteRow: Record<string, unknown>): Record<string, unknown>[] {
  const rawItems = (quoteRow.items ?? quoteRow.Items) as unknown[] | undefined
  if (!rawItems?.length) return []
  return rawItems.map((it) => it as Record<string, unknown>)
}

/** 采购报价表：品牌（多行明细去重后用顿号拼接；无则看报价头 brand） */
function dockQuoteBrandDisplay(quoteRow: Record<string, unknown>): string {
  const items = dockQuoteItemsRaw(quoteRow)
  const set = new Set<string>()
  for (const o of items) {
    const b = o.brand ?? o.Brand
    if (b != null && String(b).trim() !== '') set.add(String(b).trim())
  }
  if (set.size > 0) return [...set].join('、')
  const hb = quoteRow.brand ?? quoteRow.Brand
  if (hb != null && String(hb).trim() !== '') return String(hb).trim()
  return '—'
}

/** 采购报价表：供应商名称（多供应商去重后顿号拼接） */
function dockQuoteVendorNamesDisplay(quoteRow: Record<string, unknown>): string {
  return quoteVendorNamesDisplay(quoteRow, maskPurchaseSensitiveFields.value)
}

/** 采购报价表：供应商等级（现读 S/A/B/C，多供应商去重后顿号拼接） */
function dockQuoteVendorLevelsDisplay(quoteRow: Record<string, unknown>): string {
  return quoteVendorLevelsDisplay(quoteRow, (level) => vendorDict.levelLabel(level))
}

/** 采购报价表：供应商交易次数（全公司现读，多供应商去重后顿号拼接） */
function dockQuoteVendorTradeCountsDisplay(quoteRow: Record<string, unknown>): string {
  return quoteVendorTradeCountsDisplay(quoteRow)
}

/** 报价单行：与后端 quoteitem / 前端阶梯行一致 */
interface DockQuoteTierLine {
  quantity: number
  unitPrice: number
  currency: number
  /** 生产日期 / DC（quoteitem.date_code） */
  dateCode?: string | null
  /** 交期（quoteitem.lead_time） */
  leadTime?: string | null
}

function dockQuoteLineItems(quoteRow: Record<string, unknown>): DockQuoteTierLine[] {
  const rawItems = (quoteRow.items ?? quoteRow.Items) as unknown[] | undefined
  if (rawItems && rawItems.length > 0) {
    const lines: DockQuoteTierLine[] = []
    const headerCurrency = Number(quoteRow.currency ?? quoteRow.Currency ?? 1)
    for (const it of rawItems) {
      const o = it as Record<string, unknown>
      const quantity = Number(o.quantity ?? o.Quantity ?? 0)
      const unitPrice = Number(o.unitPrice ?? o.UnitPrice ?? 0)
      const currency = Number(o.currency ?? o.Currency ?? headerCurrency) || 1
      const dcRaw = o.dateCode ?? o.DateCode
      const dateCode =
        dcRaw != null && String(dcRaw).trim() !== '' ? String(dcRaw).trim() : null
      const ltRaw = o.leadTime ?? o.LeadTime
      const leadTime =
        ltRaw != null && String(ltRaw).trim() !== '' ? String(ltRaw).trim() : null
      lines.push({ quantity, unitPrice, currency, dateCode, leadTime })
    }
    return lines
  }
  const q = Number(quoteRow.quantity ?? quoteRow.quoteLineQuantity ?? 0)
  const p = Number(quoteRow.unitPrice ?? quoteRow.UnitPrice ?? 0)
  const c = Number(quoteRow.currency ?? quoteRow.Currency ?? 1) || 1
  const hdrDc = quoteRow.dateCode ?? quoteRow.DateCode
  const dateCode =
    hdrDc != null && String(hdrDc).trim() !== '' ? String(hdrDc).trim() : null
  const hdrLt = quoteRow.leadTime ?? quoteRow.LeadTime
  const leadTime =
    hdrLt != null && String(hdrLt).trim() !== '' ? String(hdrLt).trim() : null
  if ((Number.isFinite(q) && q !== 0) || (Number.isFinite(p) && p !== 0)) {
    return [{ quantity: q, unitPrice: p, currency: c, dateCode, leadTime }]
  }
  return []
}

/** 采购报价「生产日期/DC」：库内多为字典 ItemCode，展示为字典文案（与需求详情 / 销售订单明细一致） */
function formatDockTierDateCode(dc: string | null | undefined) {
  const s = String(dc ?? '').trim()
  if (!s) return '—'
  return productionDateDisplayLabel(s, materialPdOptions.value) || '—'
}

function formatDockTierLeadTime(lt: string | null | undefined) {
  const s = String(lt ?? '').trim()
  return s || '—'
}

function formatDockTierQuantity(q: number) {
  if (!Number.isFinite(q)) return '—'
  if (Math.abs(q - Math.round(q)) < 1e-9) return String(Math.round(q))
  return q.toLocaleString('zh-CN', { maximumFractionDigits: 4 })
}

function formatDeletedQuoteQuantityLine(line: string) {
  const n = Number(String(line).replace(/,/g, ''))
  return Number.isFinite(n) ? formatDockTierQuantity(n) : line.trim() || '—'
}

/** 明细目标价币别（与新建需求物料明细 priceCurrency 一致） */
function resolveRfqItemPriceCurrency(row: RFQItem): number {
  const raw = row.priceCurrency ?? row.currency
  const n = typeof raw === 'number' ? raw : raw != null && raw !== '' ? Number(raw) : NaN
  if (Number.isFinite(n) && n >= 1) return Math.round(n)
  if (typeof raw === 'string') {
    const u = raw.trim().toUpperCase()
    if (u === 'USD') return 2
    if (u === 'EUR') return 3
    if (u === 'HKD') return 4
    if (u === 'JPY') return 5
    if (u === 'GBP') return 6
  }
  return 1
}

/** 与报价阶梯币别枚举一致：1=RMB，2=USD，3=EUR，4=HKD，5=JPY，6=GBP */
function dockTierCurrencyCode(currency?: number): string {
  const n = Number(currency)
  if (n === 2) return 'USD'
  if (n === 3) return 'EUR'
  if (n === 4) return 'HKD'
  if (n === 5) return 'JPY'
  if (n === 6) return 'GBP'
  return 'RMB'
}

function dockTierCurrencyCodeClass(currency?: number): string {
  const n = Number(currency)
  if (n === 1 || !Number.isFinite(n) || n === 0) return 'dock-tier-ccy--rmb'
  if (n === 2) return 'dock-tier-ccy--usd'
  if (n === 3) return 'dock-tier-ccy--eur'
  if (n === 4) return 'dock-tier-ccy--hkd'
  return 'dock-tier-ccy--purple'
}

function dockTierUnitPriceHasValue(unitPrice: number): boolean {
  return Number.isFinite(unitPrice) && unitPrice !== 0
}

function formatDockTierAmountNum(unitPrice: number): string {
  if (!dockTierUnitPriceHasValue(unitPrice)) return '—'
  return unitPrice.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

/** 与 `formatDockTierAmountNum` 同规则；用 `formatToParts` 避免环境小数点字符非 `.` 时拆分失败 */
function splitDockTierAmountParts(unitPrice: number): { intPart: string; fracPart: string } {
  if (!dockTierUnitPriceHasValue(unitPrice)) return { intPart: '—', fracPart: '' }
  const n = Number(unitPrice)
  const parts = new Intl.NumberFormat('zh-CN', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 6
  }).formatToParts(n)
  let intPart = ''
  let fracPart = ''
  for (const p of parts) {
    if (p.type === 'integer' || p.type === 'group') intPart += p.value
    else if (p.type === 'decimal' || p.type === 'fraction') fracPart += p.value
  }
  if (!fracPart) return { intPart: intPart || formatDockTierAmountNum(n), fracPart: '' }
  return { intPart, fracPart }
}

function mapRow(row: any): RFQItem {
  return {
    ...row,
    status: row.status ?? row.Status,
    assignedPurchaserUserId1: row.assignedPurchaserUserId1 ?? row.AssignedPurchaserUserId1,
    assignedPurchaserUserId2: row.assignedPurchaserUserId2 ?? row.AssignedPurchaserUserId2,
    rfqCreateTime: row.rfqCreateTime,
    materialModel: row.mpn ?? row.materialModel,
    customerMaterialModel: row.customerMpn ?? row.customerMaterialModel,
    customerBrand: row.customerBrand ?? row.CustomerBrand
  }
}

function applyRouteQueryToFilters() {
  const q = route.query
  const s = q.startDate
  const e = q.endDate
  if (typeof s === 'string' && typeof e === 'string' && s && e) {
    dateRange.value = [s, e]
  } else {
    dateRange.value = null
  }
  searchForm.customerKeyword = typeof q.customerKeyword === 'string' ? q.customerKeyword : ''
  searchForm.materialModel = typeof q.materialModel === 'string' ? q.materialModel : ''
  const bidRaw = Array.isArray(q.brandId) ? q.brandId[0] : q.brandId
  const bidNum = typeof bidRaw === 'string' || typeof bidRaw === 'number' ? Number(bidRaw) : NaN
  searchForm.brandId = Number.isFinite(bidNum) && bidNum > 0 ? bidNum : null
  searchForm.rfqCode = typeof q.rfqCode === 'string' ? q.rfqCode : ''
  const stRaw = q.itemStatus ?? q.status
  const stStr = Array.isArray(stRaw) ? stRaw[0] : stRaw
  if (stStr != null && String(stStr).trim() !== '') {
    const n = Number(stStr)
    searchForm.itemStatus = Number.isFinite(n) ? n : undefined
  } else {
    searchForm.itemStatus = undefined
  }
  const sid = q.salesUserId
  const sidRaw = Array.isArray(sid) ? sid[0] : sid
  searchForm.salesUserId =
    typeof sidRaw === 'string' && sidRaw !== '' ? sidRaw : undefined
  const pid = q.purchaserUserId
  const pidRaw = Array.isArray(pid) ? pid[0] : pid
  searchForm.purchaserUserId =
    typeof pidRaw === 'string' && pidRaw !== '' ? pidRaw : undefined
  const hq = q.hasQuotesOnly
  const hqRaw = Array.isArray(hq) ? hq[0] : hq
  const hqStr = hqRaw != null && typeof hqRaw !== 'object' ? String(hqRaw).trim().toLowerCase() : ''
  searchForm.hasQuotesOnly = hqStr === '1' || hqStr === 'true' || hqStr === 'yes'
}

async function loadData() {
  loading.value = true
  try {
    const filterBag: RfqItemListAnalyticsQuery = {}
    appendRfqItemListFilterParams(filterBag)
    const res = await rfqApi.searchRFQItems({
      pageNumber: pageInfo.page,
      pageSize: pageInfo.pageSize,
      startDate: filterBag.startDate,
      endDate: filterBag.endDate,
      itemCreateStart: filterBag.itemCreateStart,
      itemCreateEndExclusive: filterBag.itemCreateEndExclusive,
      quoteCreateStart: filterBag.quoteCreateStart,
      quoteCreateEndExclusive: filterBag.quoteCreateEndExclusive,
      quickFilter: filterBag.quickFilter,
      customerKeyword: filterBag.customerKeyword,
      materialModel: filterBag.materialModel,
      brandId: filterBag.brandId,
      rfqCode: filterBag.rfqCode,
      ...(filterBag.status !== undefined && filterBag.status !== null
        ? { status: filterBag.status as RFQItemStatus }
        : {}),
      salesUserId: filterBag.salesUserId,
      purchaserUserId: filterBag.purchaserUserId,
      ...(filterBag.hasQuotesOnly ? { hasQuotesOnly: true } : {})
    })
    const rawItems = (res.items || []) as { id?: string }[]
    const idList = rawItems.map((r) => String(r.id ?? '').trim()).filter(Boolean)
    let countMap: Record<string, number> = {}
    if (idList.length) {
      try {
        const { counts } = await quoteApi.getQuoteCountsByRfqItemIds(idList)
        countMap = counts || {}
      } catch {
        countMap = {}
      }
    }
    quoteRecordCountByRfqItemId.value = countMap
    tableData.value = rawItems.map(mapRow)
    totalCount.value = res.totalCount ?? 0
    pageInfo.total = res.totalCount ?? 0

    const selId = pendingRestoreSelectedItemId.value || selectedRfqItem.value?.id
    pendingRestoreSelectedItemId.value = undefined
    if (selId) {
      const found = tableData.value.find((r) => r.id === selId)
      if (found) {
        selectedRfqItem.value = found
        await loadQuotesForRfqItem(found)
        await refreshDockLinkAlert(found)
        await nextTick()
        dataTableRef.value?.setCurrentRow(found)
      } else {
        selectedRfqItem.value = null
        quotesForItem.value = []
        deletedQuotesForItem.value = []
        deletedQuotesExpanded.value = false
        deletedQuotesBoundItemId = ''
        dockLinkAlert.value = null
      }
    }
  } catch (e: unknown) {
    tableData.value = []
    totalCount.value = 0
    pageInfo.total = 0
    quoteRecordCountByRfqItemId.value = {}
    selectedRfqItem.value = null
    quotesForItem.value = []
    deletedQuotesForItem.value = []
    deletedQuotesExpanded.value = false
    deletedQuotesBoundItemId = ''
    dockLinkAlert.value = null
    const msg = e instanceof Error ? e.message : t('rfqItemList.loadFailed')
    ElMessage.error(msg)
  } finally {
    loading.value = false
  }
  await nextTick()
  await restoreTableSelectionFromBasket()
  resetListRightPanelOnReload(materialIntelLookupStore)
  resetListRightPanelOnReload(customerWorkspacePanelStore)
}

function handleSearch() {
  pageInfo.page = 1
  const keywords = pickRfqItemKeywordQuery({
    rfqCode: searchForm.rfqCode.trim(),
    customerKeyword: searchForm.customerKeyword.trim(),
    materialModel: searchForm.materialModel.trim(),
    brandId: searchForm.brandId != null && searchForm.brandId > 0 ? String(searchForm.brandId) : '',
    salesUserId: searchForm.salesUserId ?? '',
    purchaserUserId: searchForm.purchaserUserId ?? ''
  })
  if (activePreset.value) {
    router.replace({
      name: 'RFQItemList',
      query: buildRfqItemListRouteQuery({ preset: activePreset.value, keywords })
    })
    return
  }
  router.replace({
    name: 'RFQItemList',
    query: buildRfqItemListRouteQuery({
      keywords,
      advanced: {
        startDate: dateRange.value?.[0],
        endDate: dateRange.value?.[1],
        itemStatus:
          searchForm.itemStatus !== undefined && searchForm.itemStatus !== null
            ? String(searchForm.itemStatus)
            : undefined,
        hasQuotesOnly: searchForm.hasQuotesOnly
      }
    })
  })
}

function handleReset() {
  router.replace({ name: 'RFQItemList', query: {} })
}

watch(
  () => [route.name, route.query] as const,
  (curr, prev) => {
    if (route.name !== 'RFQItemList') return
    applyRouteQueryToFilters()

    const restored = consumeRfqItemListRestoreState()
    if (restored) {
      pageInfo.page = restored.page
      pageInfo.pageSize = restored.pageSize
      pendingRestoreSelectedItemId.value = restored.selectedItemId
    } else {
      const queryChanged =
        prev != null &&
        prev[0] === 'RFQItemList' &&
        JSON.stringify(prev[1]) !== JSON.stringify(curr[1])
      const enteredFromOtherRoute = prev == null || prev[0] !== 'RFQItemList'
      if (queryChanged || enteredFromOtherRoute) {
        pageInfo.page = 1
      }
    }
    if (viewMode.value === 'list') void loadData()
  },
  { deep: true, immediate: true }
)

function onSelectionChange(rows: RFQItem[]) {
  if (suppressBasketMerge.value) return
  basketStore.mergePageSelection(tableData.value, rows)
}

async function restoreTableSelectionFromBasket() {
  const table = dataTableRef.value
  if (!table) return
  suppressBasketMerge.value = true
  await nextTick()
  table.clearSelection()
  await nextTick()
  for (const row of tableData.value) {
    if (basketStore.has(row.id)) {
      table.toggleRowSelection(row, true)
    }
  }
  await nextTick()
  suppressBasketMerge.value = false
}

function removeOneFromBasket(id: string) {
  if (!id) return
  basketStore.remove(id)
  suppressBasketMerge.value = true
  const row = tableData.value.find((r) => r.id === id)
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
    await ElMessageBox.confirm('确定清空复选篮子中的全部记录？', '清空确认', {
      type: 'warning',
      confirmButtonText: '清空',
      cancelButtonText: '取消'
    })
  } catch {
    return
  }
  basketStore.clear()
  suppressBasketMerge.value = true
  dataTableRef.value?.clearSelection()
  await nextTick()
  suppressBasketMerge.value = false
  ElMessage.success('已清空复选篮子')
}

function formatRfqItemCopyLine(row: RFQItem): string {
  const rowAny = row as RFQItem & { mpn?: string }
  const mpn = String(rowAny.materialModel || rowAny.mpn || '').trim() || '—'
  const brand = String(row.brand || '').trim() || '—'
  const qty = row.quantity != null && Number.isFinite(row.quantity) ? String(row.quantity) : '—'
  const currency = dockTierCurrencyCode(resolveRfqItemPriceCurrency(row))
  return [mpn, brand, qty, currency].join('    ')
}

async function copyRfqItemTextToClipboard(text: string): Promise<boolean> {
  if (copyTextToClipboard(text)) return true
  if (typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(text)
      return true
    } catch {
      return false
    }
  }
  return false
}

async function handleBatchCopyBasket() {
  const lines = basketItems.value.map(formatRfqItemCopyLine)
  if (!lines.length) return
  const text = lines.join('\n')
  const ok = await copyRfqItemTextToClipboard(text)
  if (ok) {
    ElMessage.success(t('rfqItemList.basket.batchCopySuccess', { count: lines.length }))
    return
  }
  ElMessage.error(t('rfqItemList.actions.copyFailed'))
}

function dockQuoteRowKey(row: Record<string, unknown>) {
  return resolveQuoteRowId(row) ?? ''
}

/** 与报价列表「采购员」语义一致：优先采购员姓名（接口填充 purchaseUserName），缺省再回退创建人 / 业务员 */
function dockQuotePurchaseUserDisplay(row: Record<string, unknown>) {
  const v =
    row.purchaseUserName ??
    row.PurchaseUserName ??
    row.createUserName ??
    row.CreateUserName ??
    row.createdBy ??
    row.salesUserName ??
    row.SalesUserName
  const s = v != null && v !== '' ? String(v).trim() : ''
  return s || '—'
}

async function loadQuotesForRfqItem(item: RFQItem | null) {
  const itemId = item?.id ?? ''
  const itemChanged = itemId !== deletedQuotesBoundItemId
  deletedQuotesBoundItemId = itemId
  if (!item?.id) {
    quotesForItem.value = []
    deletedQuotesForItem.value = []
    deletedQuotesExpanded.value = false
    expandDeletedQuotesAfterDelete.value = false
    return
  }
  quotesLoading.value = true
  try {
    const [res, deleted] = await Promise.all([
      quoteApi.getList({ rfqItemId: item.id, page: 1, pageSize: 2000 }),
      rfqApi.getDeletedQuotesForItem(item.id).catch(() => [] as RfqItemDeletedQuoteRow[])
    ])
    quotesForItem.value = (res.data || []) as Record<string, unknown>[]
    deletedQuotesForItem.value = deleted
    if (expandDeletedQuotesAfterDelete.value) {
      deletedQuotesExpanded.value = deleted.length > 0
      expandDeletedQuotesAfterDelete.value = false
    } else if (itemChanged) {
      deletedQuotesExpanded.value = false
    }
  } catch {
    quotesForItem.value = []
    deletedQuotesForItem.value = []
    deletedQuotesExpanded.value = false
    expandDeletedQuotesAfterDelete.value = false
  } finally {
    quotesLoading.value = false
  }
}

async function refreshDockLinkAlert(row: RFQItem) {
  dockSummaryLoading.value = true
  dockLinkAlert.value = null
  try {
    const loaded = await fetchLinkedRfqItemRecord(row.rfqId || '', row.id)
    const raw = loaded?.item ?? (row as unknown as Record<string, unknown>)
    dockLinkAlert.value = buildLinkAlertFieldsFromItem(raw, {
      rfqCode: row.rfqCode,
      rfqId: row.rfqId,
      rfqHeader: loaded?.rfqHeader ?? undefined
    })
  } finally {
    dockSummaryLoading.value = false
  }
}

function onItemRowClick(row: RFQItem) {
  if (purchaseQuoteDockLayout.value === 'headerOnly') {
    purchaseQuoteDockLayout.value = 'stackCompact'
  }
  selectedRfqItem.value = row
  void loadQuotesForRfqItem(row)
  void refreshDockLinkAlert(row)

  const pn = resolveRfqItemMaterialPn(row)
  materialIntelLookupStore.bindPn(pn)
  if (pn && authStore.hasPermission(AI_PERMISSION_MATERIAL_INTEL_LOOKUP)) {
    void materialIntelLookupStore.ensureLookup(pn, { triggerType: 'auto' })
  }

  void onOpsPanelRowClick(row as unknown as Record<string, unknown>)
}

function goDetail(row: RFQItem) {
  if (!row.rfqId) return
  router.push({ name: 'RFQDetail', params: { id: row.rfqId } })
}

function goEditRfqFromItem(row: RFQItem) {
  if (!canEditRfq.value) {
    ElMessage.warning(t('rfqList.editNeedRfqWrite'))
    return
  }
  if (!row.rfqId) return
  router.push({ name: 'RFQEdit', params: { id: row.rfqId } })
}

/** 双击：需求详情；按住 Ctrl 双击：编辑所属需求（与需求列表「编辑」同入口） */
function onRfqItemRowDblClick(row: RFQItem, _column: unknown, event?: MouseEvent) {
  onCrmDetailListRowDblClick(row, _column, event, {
    canEdit: canEditRfq.value,
    onEdit: goEditRfqFromItem,
    onDefault: goDetail,
  })
}

function onDockQuoteRowDblClick(row: Record<string, unknown>, _column: unknown, event?: MouseEvent) {
  onCrmDetailListRowDblClick(row, _column, event, {
    canEdit: (r) => canEditDockQuoteRow(r),
    onEdit: goEditDockQuote,
  })
}

function persistRfqItemListStateForReturn(targetItemId?: string) {
  saveRfqItemListRestoreState({
    page: pageInfo.page,
    pageSize: pageInfo.pageSize,
    selectedItemId: targetItemId || selectedRfqItem.value?.id
  })
}

function goQuote(row: RFQItem) {
  if (!canQuoteRfqItemRow(row)) {
    ElMessage.warning(t('rfqItemList.warnings.quoteNotAllowed'))
    return
  }
  if (!row.rfqId || !row.id) {
    ElMessage.warning(t('rfqItemList.warnings.missingIds'))
    return
  }
  persistRfqItemListStateForReturn(row.id)
  router.push({
    name: 'QuoteCreate',
    query: {
      rfqId: row.rfqId,
      rfqItemId: row.id,
      ...(row.rfqCode ? { rfqCode: row.rfqCode } : {}),
      returnTo: route.fullPath
    }
  })
}

async function handleMarkNoQuote(row: RFQItem) {
  if (!canMarkNoQuoteRow(row)) return
  try {
    await ElMessageBox.confirm(
      t('rfqItemList.confirmMarkNoQuote.message'),
      t('rfqItemList.confirmMarkNoQuote.title'),
      {
        type: 'warning',
        confirmButtonText: t('common.confirm'),
        cancelButtonText: t('common.cancel')
      }
    )
  } catch {
    return
  }
  try {
    await rfqApi.markNoQuote(row.id)
    ElMessage.success(t('rfqItemList.markNoQuoteSuccess'))
    await loadData()
  } catch (e) {
    const msg = e instanceof Error ? e.message : t('rfqItemList.markNoQuoteFailed')
    ElMessage.error(msg)
  }
}

function resolveQuoteRowId(row: Record<string, unknown>): string | undefined {
  const id = row.id ?? row.Id
  if (id != null && String(id).trim() !== '') return String(id)
  return undefined
}

function goEditDockQuote(row: Record<string, unknown>) {
  if (!canEditDockQuoteRow(row)) {
    ElMessage.warning(t('quoteList.warnings.readOnly'))
    return
  }
  const id = resolveQuoteRowId(row)
  if (!id) {
    ElMessage.warning(t('rfqItemList.warnings.missingQuoteId'))
    return
  }
  router.push({
    name: 'QuoteEdit',
    params: { id },
    query: { returnTo: route.fullPath }
  })
}

async function handleDeleteDockQuote(row: Record<string, unknown>) {
  if (!canDeleteDockQuoteRow(row)) {
    ElMessage.warning(t('quoteList.warnings.cannotDeleteWon'))
    return
  }
  try {
    await ElMessageBox.confirm(
      t('quoteList.deleteConfirm', { code: displayQuoteCode(row) }),
      t('quoteList.deleteTitle'),
      { type: 'warning' }
    )
  } catch {
    return
  }
  const rid = resolveQuoteRowId(row)
  if (!rid) {
    ElMessage.warning(t('rfqItemList.warnings.missingQuoteId'))
    return
  }
  try {
    await quoteApi.delete(rid)
    quoteListBasketStore.remove(rid)
    ElMessage.success(t('quoteDetail.deleteSuccess'))
    expandDeletedQuotesAfterDelete.value = true
    await loadData()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('quoteList.loadFailed'))
  }
}

async function handleCopyRfqItemRow(row: RFQItem) {
  const text = formatRfqItemCopyLine(row)
  const ok = await copyRfqItemTextToClipboard(text)
  if (ok) {
    ElMessage.success(t('rfqItemList.actions.copySuccess'))
    return
  }
  ElMessage.error(t('rfqItemList.actions.copyFailed'))
}

async function handleCopyDockQuote(row: Record<string, unknown>) {
  const ok = await copyQuoteSummaryToClipboard(row, {
    naLabel: t('quoteList.na'),
    materialPdOptions: materialPdOptions.value
  })
  if (ok) {
    ElMessage.success(t('quoteList.actions.copySuccess'))
    return
  }
  ElMessage.error(t('quoteList.actions.copyFailed'))
}

async function handleDockRowGenerateSalesOrder(row: Record<string, unknown>) {
  const id = resolveQuoteRowId(row)
  if (!id) {
    ElMessage.warning('无法识别报价主键')
    return
  }
  dockRowSalesOrderQuoteId.value = id
  try {
    const check = await assertQuotesSameCustomer([id])
    if (!check.ok) {
      ElMessage.error(check.message)
      return
    }
    router.push({
      name: 'SalesOrderCreate',
      query: { quoteIds: id, returnTo: route.fullPath }
    })
  } finally {
    dockRowSalesOrderQuoteId.value = null
  }
}

onMounted(async () => {
  void ensureMaterialPdDict()
  void vendorDict.ensureLoaded()
  try {
    salesUsers.value = await authApi.getSalesUsersForSelect()
  } catch {
    salesUsers.value = []
  }
  try {
    purchaseUsers.value = await authApi.getPurchaseUsersForSelect()
  } catch {
    purchaseUsers.value = []
  }
  rfqItemListAutoRefreshTimer = window.setInterval(() => {
    if (route.name !== 'RFQItemList' || viewMode.value !== 'list' || loading.value) return
    void loadData()
  }, RFQ_ITEM_LIST_AUTO_REFRESH_MS)
})

onBeforeUnmount(() => {
  materialIntelLookupStore.clearBound()
  customerWorkspacePanelStore.clear()
})

onUnmounted(() => {
  if (rfqItemListAutoRefreshTimer != null) {
    clearInterval(rfqItemListAutoRefreshTimer)
    rfqItemListAutoRefreshTimer = null
  }
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+SC:wght@300;400;500&display=swap');

.rfq-item-list-page {
  display: flex;
  flex-direction: column;
  /* 填满 AppLayout.content-wrapper 可视高度，避免整页被父级滚动条拉长导致底部面板不可见 */
  height: 100%;
  min-height: 0;
  max-height: 100%;
  padding: 24px;
  padding-bottom: 12px;
  box-sizing: border-box;
  overflow: hidden;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.btn-icon-only {
  width: 32px;
  padding-left: 0;
  padding-right: 0;
  justify-content: center;
}

.rfq-main-panel {
  flex: 1 1 0;
  min-height: 0;
  width: 100%;
  display: flex;
  flex-direction: column;
}

.rfq-main-panel--with-filter-tabs {
  .rfq-items-split-root,
  .rfq-item-board-scroll {
    margin-top: 0;
  }

  :deep(.crm-data-table-root),
  :deep(.table-card-scroll) {
    border-top-left-radius: 0;
    border-top-right-radius: 0;
  }

  :deep(.el-table),
  :deep(.el-table__inner-wrapper),
  :deep(.el-table__header-wrapper) {
    border-top-left-radius: 0;
    border-top-right-radius: 0;
  }

  :deep(.rfq-item-list-board > .board-toolbar.card:first-child),
  :deep(.rfq-item-list-board > .section:first-child) {
    border-top-left-radius: 0;
    border-top-right-radius: 0;
  }
}

.rfq-filter-tabs {
  display: flex;
  align-items: stretch;
  width: 100%;
  margin: 0;
  padding: 0;
  gap: 4px;
  flex-shrink: 0;
}

.rfq-filter-tabs__item {
  flex: 1 1 0;
  min-width: 0;
  padding: 9px 8px;
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

html[data-theme='dark'] .rfq-filter-tabs__item:not(.is-active) {
  background: var(--crm-layer-1);

  &:hover {
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }
}

.rfq-items-split-root {
  flex: 1 1 0;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.rfq-item-board-scroll {
  flex: 1 1 0;
  min-height: 0;
  overflow-y: auto;
  overflow-x: hidden;
}

.rfq-items-split-root--side {
  flex-direction: row;
  align-items: stretch;
  gap: 12px;

  .rfq-item-main {
    flex: 1 1 0;
    min-width: 0;
    padding-bottom: 0;
  }

  .supplier-quote-dock {
    flex: 1 1 0;
    min-width: 0;
  }
}

.rfq-items-split-root--stack-half {
  .rfq-item-main {
    min-height: 0;
  }

  .supplier-quote-dock {
    min-height: 0;
  }
}

.rfq-items-split-root--stack-resizable {
  display: grid;
  grid-template-rows: minmax(160px, var(--dock-split-top, 50%)) var(--dock-splitbar-size, 6px) minmax(120px, 1fr);
  grid-template-columns: 1fr;

  .rfq-item-main {
    grid-row: 1;
    flex: unset;
    min-height: 0;
    overflow: hidden;
    padding-bottom: 0;
  }

  .rfq-dock-splitbar {
    grid-row: 2;
  }

  .supplier-quote-dock {
    grid-row: 3;
    flex: unset;
    min-height: 0;
  }
}

.rfq-items-split-root--stack-compact {
  .rfq-item-main {
    flex: 1 1 auto;
    min-height: 0;
  }

  .supplier-quote-dock {
    flex: 0 0 auto;
  }
}

.rfq-items-split-root--dock-body-fill .supplier-quote-dock:not(.collapsed) .dock-body {
  flex: 1 1 auto;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.rfq-items-split-root--dock-body-fill .dock-table-wrap {
  flex: 1 1 auto;
  min-height: 0;
  overflow: auto;
}

.rfq-item-main {
  flex: 1 1 0;
  min-height: 0;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  padding-bottom: 8px;
}

.rfq-dock-splitbar {
  flex-shrink: 0;
  height: 6px;
  margin: 2px 0;
  cursor: row-resize;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  touch-action: none;
  user-select: none;

  &:hover,
  &.is-dragging {
    background: var(--crm-accent-008);
  }

  &__grip {
    width: 48px;
    height: 3px;
    border-radius: 2px;
    background: $border-panel;
    pointer-events: none;
  }
}

.rfq-items-split-root--stack-resizable .rfq-dock-splitbar {
  margin: 0;
  height: var(--dock-splitbar-size, 6px);
}

.supplier-quote-dock {
  flex: 0 0 auto;
  flex-shrink: 0;
  margin-top: 0;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  overflow: hidden;

  &:not(.collapsed) {
    display: flex;
    flex-direction: column;
    min-height: 0;
  }
}

.dock-header {
  padding: 10px 14px;
  background: var(--crm-table-header-bg);
  border-bottom: 1px solid $border-panel;
}

.dock-header-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  min-height: 32px;
}

.dock-header-main {
  display: flex;
  align-items: center;
  gap: 14px;
  flex: 1 1 auto;
  min-width: 0;
}

.dock-header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.dock-selected-row-actions {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  margin-right: 4px;
}

.dock-row-action-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 92px;
  min-width: 92px;
  padding: 6px 10px;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  font-weight: 500;
  border-radius: 5px;
  border: 1px solid transparent;
  cursor: pointer;
  transition: all 0.15s;
  white-space: nowrap;
  line-height: 1.2;

  &--quote {
    background: linear-gradient(135deg, rgba(0, 102, 255, 0.92), rgba(0, 168, 232, 0.88));
    border-color: rgba(0, 102, 255, 0.55);
    color: #fff;

    &:hover {
      background: linear-gradient(135deg, rgba(0, 118, 255, 1), rgba(0, 188, 245, 0.95));
      box-shadow: 0 2px 10px rgba(0, 102, 255, 0.28);
    }
  }

  &--no-quote {
    background: #fef9e7;
    border-color: rgba(230, 162, 60, 0.42);
    color: #b88230;

    &:hover {
      background: #fdf3d7;
      border-color: rgba(230, 162, 60, 0.58);
      box-shadow: 0 2px 8px rgba(230, 162, 60, 0.16);
    }
  }
}

.dock-title {
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
  flex-shrink: 0;
  line-height: 1.4;
}

.dock-layout-actions {
  gap: 2px;
}

.dock-layout-btn {
  color: $cyan-primary !important;

  &.is-active {
    background: rgba(0, 212, 255, 0.12) !important;
    outline: 1px solid rgba(0, 212, 255, 0.35);
  }
}

.rfq-dock-layout-icon {
  display: block;
}

.dock-link-alert-wrap {
  margin-top: 10px;
  min-height: 28px;
}

.dock-link-alert-wrap--inline {
  margin-top: 0;
  min-height: 0;
  flex: 1 1 auto;
  min-width: 0;
  align-self: center;
}

.dock-link-alert-title-row {
  display: flex;
  flex-wrap: nowrap;
  align-items: baseline;
  gap: 0;
  line-height: 1.55;
  font-size: 14px;
  font-weight: 400;
  color: $text-primary;
  white-space: nowrap;
  overflow-x: auto;
  overflow-y: hidden;
  min-width: 0;
  scrollbar-width: thin;
}

.dock-link-alert-title-row .la-pre {
  white-space: pre;
  font-size: inherit;
}

.dock-link-alert-title-row .la-muted {
  color: $text-muted;
  font-weight: 400;
}

.dock-link-alert-title-row .la-strong {
  color: $text-primary;
  font-weight: 600;
}

.dock-link-alert-title-row .la-value-brown {
  color: $color-amber;
  font-weight: 600;
}

.supplier-quote-dock.collapsed .dock-header {
  border-bottom: none;
}

.supplier-quote-dock.collapsed .dock-link-alert-wrap--inline {
  display: none;
}

.dock-body {
  padding: 12px 14px 14px;
  min-height: 0;
}

.dock-placeholder {
  padding: 24px 12px;
  text-align: center;
  font-size: 13px;
  color: $text-muted;
}

.dock-table-wrap {
  min-height: 120px;
}

.dock-table-wrap--quotes-empty {
  min-height: 0;
}

.dock-deleted-quotes {
  margin-top: 10px;
  padding-top: 10px;
  border-top: 1px solid var(--crm-border-panel, #e2e8f0);
}

.dock-deleted-quotes__toggle {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  margin: 0 0 6px;
  padding: 0;
  border: 0;
  background: transparent;
  cursor: pointer;
  color: var(--crm-text-primary, #1a2332);
}

.dock-deleted-quotes__chevron {
  font-size: 12px;
  color: var(--crm-text-muted, #64748b);
  transition: transform 0.15s ease;
}

.dock-deleted-quotes__chevron.is-expanded {
  transform: rotate(90deg);
}

.dock-deleted-quotes__title {
  font-size: 12px;
  font-weight: 600;
}

.dock-deleted-quotes__table {
  width: 100%;
}

.deleted-quote-stack {
  display: flex;
  flex-direction: column;
  gap: 2px;
  line-height: 1.35;
}

.deleted-quote-stack--right {
  align-items: flex-end;
}

/* 与 /pending-approvals「进入审批桌面」同款 */
.btn-quote-desktop {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 8px 16px 8px 18px;
  border: none;
  border-radius: 10px;
  background: #eaf5ff;
  color: #1a2332;
  font-size: 13px;
  font-weight: 500;
  font-family: 'Noto Sans SC', sans-serif;
  line-height: 1.2;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;

  &:hover {
    background: #ddefff;
    color: #0f172a;
  }

  &:active {
    background: #d0e8ff;
  }

  &__arrow {
    font-size: 14px;
  }
}

/* 采购报价阶梯表（数量 / 金额 / 币别）样式见 assets/styles/crm-quote-tier-dock.scss */

.page-header {
  flex-shrink: 0;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  margin-bottom: 20px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .basket-count-label {
    color: $cyan-primary;
    font-weight: 600;
    margin-left: 2px;
  }

  .page-title {
    margin: 0;
    color: $text-primary;
    font-size: 20px;
    font-weight: 600;
    letter-spacing: 0.5px;
  }

  .count-badge {
    font-size: 12px;
    color: $text-muted;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid $border-panel;
    border-radius: 20px;
    padding: 3px 10px;
  }
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;

  .page-icon {
    width: 36px;
    height: 36px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.25);
    color: $cyan-primary;
    font-weight: 700;
  }
}

// ---- 搜索栏（与客户列表 CustomerList 对齐）----
.search-bar {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 8px;
  margin-bottom: 12px;
  flex-shrink: 0;
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
  font-weight: 500;
  color: var(--crm-text-primary);
  background: color-mix(in srgb, var(--crm-cyan-primary) 14%, var(--crm-layer-2, #fff));
  border: 1px solid color-mix(in srgb, var(--crm-cyan-primary) 40%, var(--crm-border-panel));
  border-radius: 999px;
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
  color: var(--crm-text-muted);
  cursor: pointer;
  font-size: 14px;
  line-height: 1;

  &:hover {
    color: var(--crm-text-primary);
    background: var(--crm-accent-008, rgba(0, 212, 255, 0.08));
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
  padding: 7px 12px 7px 32px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  outline: none;
  transition: border-color 0.2s;

  &::placeholder {
    color: $text-muted;
  }

  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }

  &.search-input--w180 {
    width: 180px;
  }

  &.search-input--w160 {
    width: 160px;
  }

  &.search-input--w140 {
    width: 140px;
  }
}

.status-select {
  width: 120px;

  :deep(.el-select__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }

  :deep(.el-select__placeholder) {
    color: $text-muted !important;
  }

  :deep(.el-select__selected-item) {
    color: $text-primary !important;
  }
}

.status-select--sales,
.status-select--purchase,
.status-select--item-status {
  width: 180px;
}

.search-brand-select {
  width: 180px;
  flex-shrink: 0;

  :deep(.el-select__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }

  :deep(.el-select__placeholder) {
    color: $text-muted !important;
  }

  :deep(.el-select__selected-item) {
    color: $text-primary !important;
  }
}

.filter-checkbox-has-quotes {
  flex-shrink: 0;

  :deep(.el-checkbox__label) {
    color: $text-primary;
    font-size: 12px;
  }
}

.rfq-item-quote-count {
  font-variant-numeric: tabular-nums;
}

.rfq-item-quote-count--positive {
  color: $warning-color;
  font-weight: 600;
}

.filter-date-range {
  width: 218px;
  flex-shrink: 0;

  :deep(.el-range-editor.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    padding-inline: 6px;
  }

  :deep(.el-range-input) {
    color: $text-primary !important;
    font-size: 12px !important;
    width: 82px !important;
    min-width: 82px !important;
    max-width: 82px !important;
    flex: 0 0 82px !important;
  }

  :deep(.el-range-separator) {
    color: $text-muted !important;
    flex-shrink: 0;
    padding: 0 2px;
    font-size: 12px;
  }
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
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  letter-spacing: 0.5px;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
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
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
}

.btn-ghost.btn-board-active {
  border-color: rgba(0, 212, 255, 0.45);
  color: #00d4ff;
  background: rgba(0, 212, 255, 0.08);

  &:hover {
    border-color: rgba(0, 212, 255, 0.55);
    color: #00d4ff;
    background: rgba(0, 212, 255, 0.12);
  }
}

// 表格区：无 el-card，由 CrmDataTable 根节点 .table-wrapper 承接 crm-unified-list 皮肤
.rfq-item-table-panel {
  flex: 1 1 0;
  min-height: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.table-card-scroll {
  flex: 1 1 0;
  min-height: 0;
  overflow: hidden;
  display: flex;
  flex-direction: column;

  :deep(.table-wrapper) {
    flex: 1 1 0;
    min-height: 0;
    overflow: hidden;
    display: flex;
    flex-direction: column;
  }

  :deep(.el-table) {
    flex: 1 1 0;
    height: 100% !important;
  }

  :deep(.el-table__inner-wrapper) {
    height: 100%;
  }

  /*
   * 勿在 .el-table__body-wrapper 上设 padding-bottom：会在横向滚动条下方留出一条「缝」，
   * 仍能扫到表格底色/内容。改为给内部 table 加 margin-bottom，滚动条贴在列表区域最底，
   * 同时竖向滚到底时最后一行仍能在横条之上完整露出。
   */
  :deep(.el-table__body-wrapper .el-table__body) {
    margin-bottom: 12px;
  }

  :deep(.el-table__fixed-body-wrapper .el-table__body) {
    margin-bottom: 12px;
  }

  /* 竖向条与菜单同宽 $scrollbar-vertical-width；横向条高度 $scrollbar-table-horizontal-height */
  :deep(.el-table__body-wrapper::-webkit-scrollbar),
  :deep(.el-table__header-wrapper::-webkit-scrollbar),
  :deep(.el-table__fixed-body-wrapper::-webkit-scrollbar) {
    width: $scrollbar-vertical-width;
    height: $scrollbar-table-horizontal-height;
  }

  :deep(.el-table__body-wrapper::-webkit-scrollbar-thumb),
  :deep(.el-table__header-wrapper::-webkit-scrollbar-thumb),
  :deep(.el-table__fixed-body-wrapper::-webkit-scrollbar-thumb) {
    border-radius: $scrollbar-table-horizontal-height * 0.5;
  }

  /* 与 RFQList 列表区一致的表头变量（操作列表头单独 #0A1D30 见下） */
  :deep(.el-table) {
    --el-table-header-bg-color: rgba(255, 255, 255, 0.03);
    --el-table-tr-bg-color: transparent;
    --el-table-border-color: #{$border-panel};
  }

  /* 选中行左侧橙色箭头（与业务详情 §7.4.6 主从联动选中行一致） */
  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row.current-row > td.el-table__cell:first-child),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row.current-row > td.el-table__cell:first-child),
  :deep(.el-table__fixed .el-table__body tr.el-table__row.current-row > td.el-table__cell:first-child) {
    position: relative;

    &::before {
      content: '';
      position: absolute;
      left: 4px;
      top: 50%;
      transform: translateY(-50%);
      width: 0;
      height: 0;
      border-top: 5px solid transparent;
      border-bottom: 5px solid transparent;
      border-left: 7px solid var(--crm-list-row-indicator-color);
      pointer-events: none;
      z-index: 1;
    }
  }

}

// 操作列表头 / 列宽：crm-unified-list.scss 全局 .el-table th.op-col + .list-op-col-*

/* 底栏：与《业务列表规范》及 CustomerList 一致（列设置齿轮 → 行高密度锚点 → Spacer → 复选篮子） */
.pagination-wrapper {
  flex-shrink: 0;
  margin-top: 6px;
  padding-top: 0;
  display: flex;
  align-items: flex-start;
  justify-content: flex-start;
  gap: 12px 16px;
  flex-wrap: wrap;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
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
  width: 26px;
  flex: 0 0 26px;
}

.basket-footer-left {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  flex-wrap: nowrap;
  flex-shrink: 0;
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

.pagination-wrapper .quantum-pagination {
  margin-left: auto;
  align-self: flex-start;
}

// 主列表操作列 .action-btns / .action-btn / op-col：crm-unified-list.scss 中 .crm-data-table 统一提供

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
</style>

<!-- 抽屉 / 齿轮菜单挂载在 body，需单独样式块 -->
<style lang="scss">
@import '@/assets/styles/variables.scss';

.rfq-item-list-settings-popper.el-popover.el-popper {
  padding: 6px;
  min-width: 160px;
  overflow: visible;
}

.rfq-item-list-settings-menu {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.rfq-item-list-settings-menu__item {
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

.rfq-item-list-settings-menu__caret {
  margin-left: 8px;
  font-size: 12px;
  color: var(--crm-text-muted, rgba(200, 216, 232, 0.55));
}

.rfq-item-list-settings-menu__submenu {
  position: relative;
}

.rfq-item-list-settings-menu__flyout {
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

.rfq-basket-drawer {
  .basket-drawer-hint {
    font-size: 13px;
    color: $text-secondary;
    line-height: 1.6;
    margin: 0 0 12px;
  }

  .basket-drawer-summary {
    font-size: 13px;
    color: $text-secondary;
    margin: 0 0 12px;
    line-height: 1.6;

    strong {
      color: $text-primary;
      font-weight: 600;
    }
  }

  /* 与列表页底部「清空篮子」同款 label 链式按钮，嵌入说明句内 */
  .basket-clear-btn--drawer-inline {
    vertical-align: baseline;
    height: auto !important;
    min-height: 0 !important;
    padding: 0 2px !important;
    margin: 0 1px;
    font-size: 13px !important;
    font-weight: 500;
  }

  .basket-drawer-actions {
    margin-top: 16px;
    display: flex;
    justify-content: flex-end;
  }

}
</style>
