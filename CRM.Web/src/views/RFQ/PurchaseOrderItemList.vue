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
        <span class="filter-field-label">{{ t('purchaseOrderItemList.filters.dateRangeLabel') }}</span>
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

        <span class="filter-field-label">{{ t('purchaseOrderList.filters.orderCode') }}</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.purchaseOrderCode"
            class="search-input"
            :placeholder="t('purchaseOrderItemList.filters.poCodePlaceholder')"
            @keyup.enter="loadList"
          />
        </div>
        <template v-if="canViewVendor">
          <span class="filter-field-label">{{ t('purchaseOrderList.filters.vendor') }}</span>
          <div class="search-input-wrap">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="filters.vendorName"
              class="search-input"
              :placeholder="t('purchaseOrderItemList.filters.vendorPlaceholder')"
              @keyup.enter="loadList"
            />
          </div>
        </template>
        <template v-if="canViewPurchaseUser">
          <span class="filter-field-label">{{ t('purchaseOrderList.columns.purchaser') }}</span>
          <div class="search-input-wrap">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="filters.purchaseUserName"
              class="search-input"
              :placeholder="t('purchaseOrderItemList.filters.purchaserPlaceholder')"
              @keyup.enter="loadList"
            />
          </div>
        </template>
        <span class="filter-field-label">{{ t('purchaseOrderItemList.columns.pn') }}</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.pn"
            class="search-input"
            :placeholder="t('purchaseOrderItemList.filters.pnPlaceholder')"
            @keyup.enter="loadList"
          />
        </div>

        <span class="filter-field-label">{{ t('purchaseOrderItemList.filters.orderType') }}</span>
        <el-select
          v-model="filters.orderType"
          :placeholder="t('purchaseOrderItemList.filters.allOrderTypes')"
          clearable
          class="po-order-type-select"
          :teleported="false"
          @change="applyOrderTypeFilter"
        >
          <el-option :label="t('purchaseOrderItemList.filters.orderTypeCustomer')" :value="1" />
          <el-option :label="t('purchaseOrderItemList.filters.orderTypeStocking')" :value="2" />
          <el-option :label="t('purchaseOrderItemList.filters.orderTypeSample')" :value="3" />
        </el-select>

        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="loadList">
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
      :data="pagedList"
      v-loading="loading"
      row-key="purchaseOrderItemId"
      @selection-change="onSelectionChange"
      @row-dblclick="goDetail"
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
      <template #col-cost="{ row }">{{ formatCurrencyUnitPrice(row.cost, row.currency) }}</template>
      <template #col-lineTotal="{ row }">{{ formatCurrencyTotal(row.lineTotal, row.currency) }}</template>
      <template #col-createTime="{ row }">{{ formatDt(row.createTime || row.orderCreateTime) }}</template>
      <template #col-createUser="{ row }">{{ row.createUserName || row.createdBy || row.purchaseUserName || '—' }}</template>
      <template #col-actions-header>
        <div class="op-col-header">
            <span class="op-col-header-text">{{ t('purchaseOrderItemList.columns.actions') }}</span>
          <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
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
        :page-sizes="[10, 20, 50]"
        layout="total, prev, pager, next, sizes"
        class="quantum-pagination"
        @current-change="onPageChange"
        @size-change="onPageSizeChange"
      />
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
              <el-input :model-value="paymentForm.vendorName || '--'" disabled />
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
                style="width: 100%"
              >
                <el-option :label="t('purchaseOrderItemList.paymentDialog.bankBoc')" value="bank-boc" />
                <el-option :label="t('purchaseOrderItemList.paymentDialog.bankIcbc')" value="bank-icbc" />
                <el-option :label="t('purchaseOrderItemList.paymentDialog.bankCcb')" value="bank-ccb" />
                <el-option :label="t('purchaseOrderItemList.paymentDialog.bankAbc')" value="bank-abc" />
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
                <el-form-item :label="t('purchaseOrderItemList.arrivalDialog.companyName')"><el-input v-model="arrivalForm.companyName" /></el-form-item>
              </el-col>
            </el-row>
            <el-row :gutter="12">
              <el-col :span="6">
                <el-form-item :label="t('purchaseOrderItemList.arrivalDialog.regionType')">
                  <el-select
                    :model-value="normalizeRegionType(arrivalForm.regionType)"
                    :teleported="false"
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
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { Setting } from '@element-plus/icons-vue'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { financePaymentApi } from '@/api/finance'
import { logisticsApi } from '@/api/logistics'
import { ElMessage } from 'element-plus'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatCurrencyTotal, formatCurrencyUnitPrice } from '@/utils/moneyFormat'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import SettlementCurrencyAmountInput from '@/components/SettlementCurrencyAmountInput.vue'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { REGION_TYPE_DOMESTIC, REGION_TYPE_OVERSEAS, normalizeRegionType } from '@/constants/regionType'

const router = useRouter()
const route = useRoute()
const { t, locale } = useI18n()
const authStore = useAuthStore()

const { ensureLoaded: ensureLogisticsDict, arrivalOptions: arrivalMethodDictOptions, expressOptions: expressMethodDictOptions } =
  useLogisticsFormDict()

const canViewVendor = computed(() => authStore.hasPermission('vendor.info.read'))
const canViewPurchaseUser = computed(() => authStore.hasPermission('purchase.user.read') || authStore.hasPermission('purchase-order.read'))
const canViewAmount = computed(() => authStore.hasPermission('purchase.amount.read'))
const canCreateArrivalNotice = computed(() => authStore.hasPermission('purchase-order.read'))

const loading = ref(false)
const allLines = ref<any[]>([])
/** 未按采购订单类型筛选前的明细行（便于仅改类型下拉时不重新拉接口） */
const linesBeforeOrderType = ref<any[]>([])
const pagedList = ref<any[]>([])

const total = ref(0)
const page = ref(1)
const pageSize = ref(20)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 260
const OP_COL_EXPANDED_MIN_WIDTH = 240
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
    labelClassName: 'op-col'
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

function normalizePurchaseOrderType(detail: any, orderSummary: any): number {
  const n = Number(detail?.type ?? detail?.Type ?? orderSummary?.type ?? orderSummary?.Type ?? 1)
  return n >= 1 && n <= 3 ? n : 1
}

function isLineStockingPurchase(row: any) {
  return Number(row?.purchaseOrderType) === 2
}

function applyOrderTypeFilter() {
  const ot = filters.orderType
  allLines.value = linesBeforeOrderType.value.filter((x: any) => {
    if (ot === undefined || ot === null) return true
    return Number(x.purchaseOrderType) === ot
  })
  page.value = 1
  applyPagination()
}

const applyPagination = () => {
  const start = (page.value - 1) * pageSize.value
  pagedList.value = allLines.value.slice(start, start + pageSize.value)
  total.value = allLines.value.length
  // 避免分页切换后 checkbox 状态“残留”
  selectedRows.value = []
  ;(tableRef.value as any)?.clearSelection?.()
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

function openPaymentDialog(row: any) {
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
  arrivalForm.regionType = REGION_TYPE_DOMESTIC
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

  const lineRemark = paymentForm.lines
    .filter((x: any) => x.remark)
    .map((x: any) => `${x.pn || x.purchaseOrderCode}:${x.remark}`)
    .join('; ')
  const extRemark = [
    paymentForm.remark || '',
    `供应商银行:${paymentForm.vendorBankId}`,
    `费用(中转/手续费/运费/杂费/尾差):${paymentForm.fee.intermediateBankFee}/${paymentForm.fee.bankCharge}/${paymentForm.fee.freight}/${paymentForm.fee.miscFee}/${paymentForm.fee.rounding}`,
    `中转行费用承担方:${paymentForm.fee.intermediateBankFeePayer}`,
    lineRemark ? `明细备注:${lineRemark}` : ''
  ].filter(Boolean).join(' | ')

  paymentSubmitting.value = true
  try {
    const created = await financePaymentApi.create({
      financePaymentCode: buildFinancePaymentCode(),
      vendorId: paymentForm.vendorId,
      vendorName: paymentForm.vendorName,
      paymentMode: paymentForm.paymentMode,
      paymentCurrency: paymentForm.currency,
      paymentAmountToBe: paymentTotalAmount.value,
      remark: extRemark,
      items: paymentForm.lines.map((line: any) => ({
        purchaseOrderId: line.purchaseOrderId,
        purchaseOrderItemId: line.purchaseOrderItemId,
        paymentAmountToBe: Number(line.requestAmount || 0),
        pn: line.pn,
        brand: line.brand
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
    const params: Record<string, unknown> = {
      page: 1,
      pageSize: 2000
    }
    if (dateRange.value?.[0]) params.startDate = dateRange.value[0]
    if (dateRange.value?.[1]) params.endDate = dateRange.value[1]
    if (filters.purchaseOrderCode.trim()) params.keyword = filters.purchaseOrderCode.trim()

    const res = await purchaseOrderApi.getList(params)
    const orders = (res as { items?: any[] } | undefined)?.items ?? []

    // 列表接口通常不带完整明细，逐单拉详情后再展开明细行
    const detailResults = await Promise.allSettled(
      orders
        .filter((o: any) => !!o?.id)
        .map((o: any) => purchaseOrderApi.getById(o.id))
    )
    const detailMap = new Map<string, any>()
    detailResults.forEach((result) => {
      if (result.status !== 'fulfilled') return
      const detail = result.value as any
      if (!detail?.id) return
      detailMap.set(detail.id, detail)
    })

    const pnK = filters.pn.trim().toLowerCase()
    const vendorK = filters.vendorName.trim().toLowerCase()
    const purchaseUserK = filters.purchaseUserName.trim().toLowerCase()
    const poCodeK = filters.purchaseOrderCode.trim().toLowerCase()

    const lines = orders.flatMap((o: any) => {
      const detail = detailMap.get(o.id) ?? o
      const items = detail?.items ?? []
      return items.map((it: any) => ({
        purchaseOrderItemId: it.purchaseOrderItemId ?? it.id ?? it.Id,
        purchaseOrderId: detail.id ?? o.id,
        purchaseOrderItemCode: it.purchaseOrderItemCode ?? it.PurchaseOrderItemCode ?? '',
        purchaseOrderCode: detail.purchaseOrderCode ?? o.purchaseOrderCode,
        purchaseOrderType: normalizePurchaseOrderType(detail, o),
        vendorId:
          it.vendorId ??
          it.VendorId ??
          detail.vendorId ??
          detail.VendorId ??
          o.vendorId ??
          o.VendorId,
        itemStatus: it.status,
        purchaseProgressStatus: Number(it.purchaseProgressStatus ?? 0),
        stockInProgressStatus: Number(it.stockInProgressStatus ?? 0),
        paymentProgressStatus: Number(it.paymentProgressStatus ?? 0),
        invoiceProgressStatus: Number(it.invoiceProgressStatus ?? 0),
        canApplyPayment: Boolean(it.canApplyPayment ?? it.CanApplyPayment ?? false),
        orderCreateTime: detail.createTime ?? o.createTime,
        vendorName:
          it.vendorName ??
          it.VendorName ??
          detail.vendorName ??
          detail.VendorName ??
          o.vendorName ??
          o.VendorName,
        purchaseUserName: detail.purchaseUserName ?? o.purchaseUserName,
        pn: it.pn,
        brand: it.brand,
        qty: it.qty,
        cost: it.cost,
        lineTotal: (it.qty || 0) * (it.cost || 0),
        paymentRequestedAmount: Number(it.paymentRequestedAmount ?? it.PaymentRequestedAmount ?? 0),
        currency: it.currency ?? detail.currency ?? o.currency,
        deliveryDate: it.deliveryDate ?? detail.deliveryDate
      }))
    })

    // 采购单号/供应商/采购员/物料型号等在前端做过滤（后端采购订单明细分页接口尚未补齐）
    linesBeforeOrderType.value = lines.filter((x: any) => {
      if (poCodeK && !String(x.purchaseOrderCode || '').toLowerCase().includes(poCodeK)) return false
      if (pnK && !String(x.pn || '').toLowerCase().includes(pnK)) return false
      if (canViewVendor.value && vendorK && !String(x.vendorName || '').toLowerCase().includes(vendorK)) return false
      if (canViewPurchaseUser.value && purchaseUserK && !String(x.purchaseUserName || '').toLowerCase().includes(purchaseUserK)) return false
      return true
    })

    applyOrderTypeFilter()
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
  loadList()
}

function onPageChange(nextPage: number) {
  page.value = nextPage
  applyPagination()
}

function onPageSizeChange(nextSize: number) {
  pageSize.value = nextSize
  page.value = 1
  applyPagination()
}

function goDetail(row: any) {
  router.push({ name: 'PurchaseOrderDetail', params: { id: row.purchaseOrderId } })
}

onMounted(() => {
  const qpn = route.query.pn
  if (typeof qpn === 'string' && qpn.trim()) {
    filters.pn = qpn.trim()
  }
  loadList()
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
</style>

