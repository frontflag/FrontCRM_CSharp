<template>
  <div v-loading="loading" class="customs-business-records-panel">
    <div class="tabs-nav">
      <button
        v-for="tab in tabs"
        :key="tab.key"
        type="button"
        class="tab-btn"
        :class="{ 'tab-btn--active': activeTab === tab.key }"
        @click="activeTab = tab.key"
      >
        {{ tab.label }}
        <span v-if="tab.count" class="tab-count">{{ tab.count }}</span>
      </button>
    </div>
    <div class="tabs-body">
      <div v-for="tab in tabs" :key="tab.key" v-show="activeTab === tab.key">
        <template v-if="tab.key === 'salesOrder'">
          <div v-if="salesOrderItemRows.length" class="detail-items-table-wrap">
            <CrmDataTable
              class="quantum-table-block el-table-host picking-so-item-embed"
              embedded
              column-layout-key="customs-declaration-business-records-so-item"
              :columns="soItemColumns"
              :show-column-settings="false"
              :show-row-density-toggle="false"
              :data="salesOrderItemRows"
              row-key="sellOrderItemId"
              size="small"
              :empty-text="t('customsPages.declarations.businessRecords.empty')"
              @row-dblclick="onSoItemDblclick"
            >
              <template #col-sellOrderItemCode="{ row }">
                <router-link
                  v-if="row.sellOrderId && row.sellOrderItemId"
                  class="link-text"
                  :to="{
                    name: 'SalesOrderDetail',
                    params: { id: String(row.sellOrderId) },
                    query: { sellOrderItemId: String(row.sellOrderItemId) }
                  }"
                >
                  {{ row.sellOrderItemCode || '—' }}
                </router-link>
                <span v-else>{{ row.sellOrderItemCode || '—' }}</span>
              </template>
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
                row.profitOutRateBiz != null ? Number(row.profitOutRateBiz).toFixed(6) : '—'
              }}</template>
              <template #col-createTime="{ row }">{{ soItemFormatDt(row.createTime || row.orderCreateTime) }}</template>
              <template #col-createUser="{ row }">{{
                row.createUserName || row.createdBy || (!maskSaleSensitiveFields ? row.salesUserName : '') || '—'
              }}</template>
            </CrmDataTable>
          </div>
          <DetailListPanelEmpty v-else size="low" :description="t('customsPages.declarations.businessRecords.empty')" />
        </template>
        <template v-else-if="tab.key === 'purchaseOrder'">
          <div v-if="purchaseOrderItemRows.length" class="detail-items-table-wrap">
            <CrmDataTable
              class="quantum-table-block el-table-host purchase-order-item-embed"
              embedded
              column-layout-key="customs-declaration-business-records-po-item"
              :columns="purchaseOrderItemColumns"
              :show-column-settings="false"
              :show-row-density-toggle="false"
              :data="purchaseOrderItemRows"
              row-key="purchaseOrderItemId"
              size="small"
              :empty-text="t('customsPages.declarations.businessRecords.empty')"
              @row-dblclick="onPurchaseOrderItemDblclick"
            >
              <template #col-purchaseOrderItemCode="{ row }">
                <span class="po-line-code-with-badge">
                  <router-link
                    v-if="row.purchaseOrderId && row.purchaseOrderItemId"
                    class="link-text"
                    :to="{
                      name: 'PurchaseOrderDetail',
                      params: { id: String(row.purchaseOrderId) },
                      query: { purchaseOrderItemId: String(row.purchaseOrderItemId) }
                    }"
                  >
                    {{ row.purchaseOrderItemCode || '—' }}
                  </router-link>
                  <span v-else>{{ row.purchaseOrderItemCode || '—' }}</span>
                  <el-tooltip
                    v-if="isPoItemStockingPurchase(row)"
                    :content="t('purchaseOrderItemList.filters.orderTypeStocking')"
                    placement="top"
                  >
                    <el-tag type="warning" effect="plain" size="small" class="po-stocking-tag" round>
                      {{ t('purchaseOrderItemList.filters.stockingTag') }}
                    </el-tag>
                  </el-tooltip>
                </span>
              </template>
              <template #col-vendorName="{ row }">
                <vendor-name-readonly-text
                  :name-zh="row.vendorName"
                  :name-en="row.vendorEnglishName"
                  :masked="maskPurchaseSensitiveFields"
                />
              </template>
              <template #col-itemStatus="{ row }">
                <el-tag effect="dark" :type="poItemStatusTagType(row.itemStatus)" size="small">
                  {{ poItemStatusText(row.itemStatus) }}
                </el-tag>
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
                  <span
                    v-if="formatUnitPriceNumber(row.cost) !== '—'"
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
              <template #col-createTime="{ row }">{{ poItemFormatDt(row.createTime || row.orderCreateTime) }}</template>
              <template #col-createUser="{ row }">{{
                row.createUserName || row.createdBy || row.purchaseUserName || '—'
              }}</template>
            </CrmDataTable>
          </div>
          <DetailListPanelEmpty v-else size="low" :description="t('customsPages.declarations.businessRecords.empty')" />
        </template>
        <template v-else-if="isStockOutNotifyTab(tab.key)">
          <div v-if="stockOutNotifyRows(tab.key).length" class="detail-items-table-wrap">
            <CrmDataTable
              class="quantum-table-block el-table-host stock-out-notify-embed"
              embedded
              :column-layout-key="
                tab.key === 'stockOutNotify'
                  ? 'customs-declaration-business-records-stock-out-notify'
                  : 'customs-declaration-business-records-customs-stock-out-notify'
              "
              :columns="stockOutNotifyColumns"
              :show-column-settings="false"
              :show-row-density-toggle="false"
              :data="stockOutNotifyRows(tab.key)"
              row-key="id"
              size="small"
              :empty-text="t('customsPages.declarations.businessRecords.empty')"
              @row-dblclick="onStockOutNotifyDblclick"
            >
              <template #col-status="{ row }">
                <span :class="['status-badge', `status-${row.status}`]">{{ notifyStatusLabel(row.status) }}</span>
              </template>
              <template #col-customsStatus="{ row }">{{ notifyCustomsStatusLabel(row.customsStatus) }}</template>
              <template #col-stockOutType="{ row }">
                <StockBizTypeTag biz="out" :type="row.stockOutType" />
              </template>
              <template #col-requestCode="{ row }">
                <span class="notify-code-cell">
                  <router-link
                    v-if="row.id"
                    class="link-text notify-code-text"
                    :to="{ name: 'StockOutNotifyDetail', params: { id: String(row.id) } }"
                  >
                    {{ row.requestCode || '—' }}
                  </router-link>
                  <span v-else class="notify-code-text">{{ row.requestCode || '—' }}</span>
                  <el-tooltip
                    v-if="isCustomsNotifyRow(row) && notifySalesNotifyTooltip(row)"
                    :content="notifySalesNotifyTooltip(row)"
                    placement="top"
                    :hide-after="0"
                  >
                    <span class="customs-notify-tag">{{ t('stockOutNotifyList.customsNotifyTag') }}</span>
                  </el-tooltip>
                </span>
              </template>
              <template #col-outQuantity="{ row }">{{ row.outQuantity }}</template>
              <template #col-regionType="{ row }">{{ notifyRegionTypeLabel(row) }}</template>
              <template #col-shipmentMethod="{ row }">{{ shipmentMethodDisplay(row.shipmentMethod) }}</template>
              <template #col-expressCompany="{ row }">{{ expressCompanyDisplay(row.expressCompany) }}</template>
              <template #col-packingCode="{ row }">
                <router-link
                  v-if="row.packingId?.trim() && row.packingCode?.trim()"
                  :to="{ name: 'PackingDetail', params: { id: row.packingId.trim() } }"
                  class="link-text"
                  @click.stop
                >
                  {{ row.packingCode.trim() }}
                </router-link>
                <span v-else-if="row.packingCode?.trim()">{{ row.packingCode.trim() }}</span>
                <span v-else>—</span>
              </template>
              <template #col-salesOrderCode="{ row }">
                <router-link
                  v-if="row.salesOrderId?.trim() && row.salesOrderCode?.trim()"
                  class="link-text"
                  :to="{ name: 'SalesOrderDetail', params: { id: row.salesOrderId.trim() } }"
                  @click.stop
                >
                  {{ row.salesOrderCode.trim() }}
                </router-link>
                <span v-else>{{ row.salesOrderCode?.trim() || '—' }}</span>
              </template>
              <template #col-salesUserName="{ row }">
                <span>{{ maskSaleSensitiveFields ? '—' : (row.salesUserName || '—') }}</span>
              </template>
              <template #col-customerName="{ row }">
                <span>{{ maskSaleSensitiveFields ? '—' : (row.customerName || '—') }}</span>
              </template>
              <template #col-requestDate="{ row }">{{ formatNotifyDateTime(row.requestDate) }}</template>
              <template #col-createTime="{ row }">{{ formatNotifyDateTime(row.createTime) }}</template>
              <template #col-createUser="{ row }">{{ row.createUserName || row.requestUserName || '—' }}</template>
            </CrmDataTable>
          </div>
          <DetailListPanelEmpty v-else size="low" :description="t('customsPages.declarations.businessRecords.empty')" />
        </template>
        <template v-else-if="isPackingTab(tab.key)">
          <div v-if="packingRows(tab.key).length" class="detail-items-table-wrap">
            <CrmDataTable
              class="quantum-table-block el-table-host packing-list-embed"
              embedded
              :column-layout-key="
                tab.key === 'packing'
                  ? 'customs-declaration-business-records-packing'
                  : 'customs-declaration-business-records-customs-packing'
              "
              :columns="packingColumns"
              :show-column-settings="false"
              :show-row-density-toggle="false"
              :data="packingRows(tab.key)"
              row-key="id"
              size="small"
              :empty-text="t('customsPages.declarations.businessRecords.empty')"
              @row-dblclick="onPackingDblclick"
            >
              <template #col-packingCode="{ row }">
                <router-link
                  v-if="row.id"
                  class="link-text"
                  :to="{ name: 'PackingDetail', params: { id: String(row.id) } }"
                >
                  {{ row.code?.trim() || '—' }}
                </router-link>
                <span v-else>{{ row.code?.trim() || '—' }}</span>
              </template>
              <template #col-status="{ row }">
                <span :class="['status-badge', `packing-status-${row.status}`]">{{ packingStatusLabel(row.status) }}</span>
              </template>
              <template #col-stockOutType="{ row }">
                <StockBizTypeTag biz="out" :type="row.stockOutType" />
              </template>
              <template #col-materialType="{ row }">{{ packingMaterialTypeLabel(row.materialType) }}</template>
              <template #col-customerName="{ row }">
                <span>{{ maskSaleSensitiveFields ? '—' : (row.customerName?.trim() || '—') }}</span>
              </template>
              <template #col-salesUserName="{ row }">
                <span>{{ maskSaleSensitiveFields ? '—' : (row.salesUserName?.trim() || '—') }}</span>
              </template>
              <template #col-warehouseName="{ row }">{{ row.warehouseName?.trim() || '—' }}</template>
              <template #col-requestDate="{ row }">
                <template v-for="p in [formatPackingDateTimeParts(row.requestDate)]" :key="`rd-${row.id}`">
                  <span v-if="p" class="crm-quote-create-time">
                    <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
                    <span v-if="p.time" class="crm-quote-create-time__hm">{{ p.time }}</span>
                  </span>
                  <span v-else>—</span>
                </template>
              </template>
              <template #col-shipmentMethod="{ row }">{{ shipmentMethodDisplay(row.shipmentMethod) }}</template>
              <template #col-expressCompany="{ row }">{{ expressCompanyDisplay(row.expressCompany) }}</template>
              <template #col-itemRows="{ row }">
                <span class="qty-cell">{{ row.itemRows ?? 0 }}</span>
              </template>
              <template #col-remark="{ row }">
                <span>{{ row.comment?.trim() || '—' }}</span>
              </template>
              <template #col-createTime="{ row }">
                <template v-for="p in [formatPackingDateTimeParts(row.createTime)]" :key="`ct-${row.id}`">
                  <span v-if="p" class="crm-quote-create-time">
                    <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
                    <span v-if="p.time" class="crm-quote-create-time__hm">{{ p.time }}</span>
                  </span>
                  <span v-else>—</span>
                </template>
              </template>
              <template #col-createUserName="{ row }">{{ row.createUserName?.trim() || '—' }}</template>
            </CrmDataTable>
          </div>
          <DetailListPanelEmpty v-else size="low" :description="t('customsPages.declarations.businessRecords.empty')" />
        </template>
        <template v-else-if="isStockOutTab(tab.key)">
          <div v-if="stockOutRows(tab.key).length" class="detail-items-table-wrap">
            <CrmDataTable
              class="quantum-table-block el-table-host stock-out-list-embed"
              embedded
              :column-layout-key="
                tab.key === 'stockOut'
                  ? 'customs-declaration-business-records-stock-out'
                  : 'customs-declaration-business-records-customs-stock-out'
              "
              :columns="stockOutColumns"
              :show-column-settings="false"
              :show-row-density-toggle="false"
              :data="stockOutRows(tab.key)"
              row-key="id"
              size="small"
              :empty-text="t('customsPages.declarations.businessRecords.empty')"
              @row-dblclick="onStockOutDblclick"
            >
              <template #col-status="{ row }">
                <span :class="['status-badge', `status-${row.status}`]">{{ stockOutListStatusLabel(row.status) }}</span>
              </template>
              <template #col-stockOutType="{ row }">
                <StockBizTypeTag biz="out" :type="row.stockOutType" />
              </template>
              <template #col-stockOutCode="{ row }">
                <span class="stock-out-code-cell">
                  <router-link
                    v-if="row.id"
                    class="link-text mono-cell"
                    :to="{ name: 'StockOutDetail', params: { id: String(row.id) } }"
                  >
                    {{ row.stockOutCode || '—' }}
                  </router-link>
                  <span v-else class="mono-cell">{{ row.stockOutCode || '—' }}</span>
                  <el-tooltip
                    v-if="isCustomsStockOutRow(row) && stockOutSalesNotifyTooltip(row)"
                    :content="stockOutSalesNotifyTooltip(row)"
                    placement="top"
                    :hide-after="0"
                  >
                    <span class="customs-notify-tag">{{ t('stockOutList.customsNotifyTag') }}</span>
                  </el-tooltip>
                </span>
              </template>
              <template #col-stockOutDate="{ row }">
                <span class="text-secondary">{{ formatStockOutDateTime(row.stockOutDate) }}</span>
              </template>
              <template #col-expectedStockOutDate="{ row }">
                <span class="text-secondary">{{ formatStockOutDateTime(row.expectedStockOutDate) }}</span>
              </template>
              <template #col-packingCount="{ row }">{{ formatStockOutPackingCount(row.packingCount) }}</template>
              <template #col-packingCodes="{ row }">
                <span class="mono-cell">{{ row.packingCodes?.trim() || '—' }}</span>
              </template>
              <template #col-createTime="{ row }">{{ formatStockOutDateTime(row.createTime) }}</template>
              <template #col-createUser="{ row }">{{ row.createUserName || '—' }}</template>
              <template #col-customer-header>
                <CustomerExtendColumnHeader
                  :active-field="customerExtendActiveField"
                  @set-active-field="setCustomerExtendActiveField"
                />
              </template>
              <template #col-customer="{ row }">
                <CustomerExtendCell
                  :row="row"
                  :active-field="customerExtendActiveField"
                  :masked="maskSaleSensitiveFields"
                  empty-text="—"
                />
              </template>
              <template #col-salesUserName="{ row }">
                <span>{{ maskSaleSensitiveFields ? '—' : (row.salesUserName || '—') }}</span>
              </template>
              <template #col-shipmentMethod="{ row }">{{ shipmentMethodDisplay(row.shipmentMethod) }}</template>
              <template #col-expressCompany="{ row }">{{ expressCompanyDisplay(row.expressCompany) }}</template>
              <template #col-courierTrackingNo="{ row }">{{ row.courierTrackingNo || '—' }}</template>
              <template #col-freightForwarderOrderNo="{ row }">
                <CrmListCopyableTextCell :text="row.freightForwarderOrderNo || ''" />
              </template>
              <template #col-remark="{ row }">{{ row.remark?.trim() || '—' }}</template>
            </CrmDataTable>
          </div>
          <DetailListPanelEmpty v-else size="low" :description="t('customsPages.declarations.businessRecords.empty')" />
        </template>
        <template v-else-if="tab.key === 'customsArrivalNotify'">
          <div v-if="customsArrivalNotifyItemRows.length" class="detail-items-table-wrap">
            <CrmDataTable
              class="quantum-table-block el-table-host arrival-notice-embed"
              embedded
              column-layout-key="customs-declaration-business-records-customs-arrival-notify"
              :columns="arrivalNoticeColumns"
              :show-column-settings="false"
              :show-row-density-toggle="false"
              :data="customsArrivalNotifyItemRows"
              row-key="id"
              size="small"
              :empty-text="t('customsPages.declarations.businessRecords.empty')"
              @row-dblclick="onArrivalNotifyDblclick"
            >
              <template #col-status="{ row }">
                <el-tag effect="dark" :type="arrivalNotifyStatusTagType(row.status)">
                  {{ arrivalNotifyStatusLabel(row.status) }}
                </el-tag>
              </template>
              <template #col-stockInType="{ row }">
                <StockBizTypeTag
                  biz="in"
                  :type="row.stockInType"
                  :customs-declaration-id="row.customsDeclarationId ?? declarationId"
                  :customs-declaration-code="row.customsDeclarationCode"
                />
              </template>
              <template #col-pn="{ row }">{{ arrivalDisplayPn(row) }}</template>
              <template #col-brand="{ row }">{{ arrivalDisplayBrand(row) }}</template>
              <template #col-expectedArrivalDate="{ row }">{{ formatArrivalExpectedDate(row.expectedArrivalDate) }}</template>
              <template #col-shipmentMethod="{ row }">{{ shipmentMethodDisplay(arrivalPickShipmentMethod(row)) }}</template>
              <template #col-courierTrackingNo="{ row }">{{ arrivalDisplayCourierTrackingNo(row) }}</template>
              <template #col-regionType="{ row }">{{ arrivalRegionTypeLabel(row) }}</template>
              <template #col-vendorName="{ row }">
                <vendor-name-readonly-text
                  :name-zh="row.vendorName"
                  :name-en="row.vendorEnglishName"
                  :masked="maskPurchaseSensitiveFields"
                />
              </template>
              <template #col-expectQty="{ row }">
                <span class="inv-list-qty">{{ formatArrivalQtyCell(arrivalExpectQty(row)) }}</span>
              </template>
              <template #col-receiveQty="{ row }">
                <span class="inv-list-qty">{{ formatArrivalQtyCell(arrivalReceiveQty(row)) }}</span>
              </template>
              <template #col-passedQty="{ row }">
                <span class="inv-list-qty">{{ formatArrivalQtyCell(arrivalPassedQty(row)) }}</span>
              </template>
              <template #col-noticeCode="{ row }">
                <router-link
                  v-if="row.id"
                  class="link-text"
                  :to="{ name: 'ArrivalNoticeList', query: { noticeId: String(row.id) } }"
                >
                  {{ row.noticeCode || '—' }}
                </router-link>
                <span v-else>{{ row.noticeCode || '—' }}</span>
              </template>
              <template #col-purchaseOrderCode="{ row }">{{ row.purchaseOrderCode?.trim() || '—' }}</template>
              <template #col-freightForwarderOrderNo="{ row }">
                <CrmListCopyableTextCell :text="row.freightForwarderOrderNo?.trim() || ''" />
              </template>
              <template #col-createTime="{ row }">
                <template v-for="p in [formatArrivalCreateTimeParts(row.createTime)]" :key="`ct-${row.id}`">
                  <span v-if="p" class="crm-quote-create-time">
                    <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
                    <span v-if="p.time" class="crm-quote-create-time__hm">{{ p.time }}</span>
                  </span>
                  <span v-else>—</span>
                </template>
              </template>
              <template #col-createUser="{ row }">{{
                row.createUserName || row.createdBy || row.purchaseUserName || '—'
              }}</template>
            </CrmDataTable>
          </div>
          <DetailListPanelEmpty v-else size="low" :description="t('customsPages.declarations.businessRecords.empty')" />
        </template>
        <template v-else>
          <div v-if="tab.rows.length" class="detail-items-table-wrap">
            <el-table :data="tab.rows" class="detail-panel-list-table" size="small" stripe>
              <el-table-column
                :label="t('customsPages.declarations.businessRecords.colCode')"
                min-width="160"
                show-overflow-tooltip
              >
                <template #default="{ row }">
                  <router-link v-if="tab.canLink(row)" class="link-text" :to="tab.route(row)">
                    {{ rowCode(row) }}
                  </router-link>
                  <span v-else>{{ rowCode(row) }}</span>
                </template>
              </el-table-column>
              <el-table-column
                :label="t('customsPages.declarations.businessRecords.colStatus')"
                width="120"
                align="center"
              >
                <template #default="{ row }">
                  <el-tag effect="dark" :type="tab.statusTagType(row.status)" size="small">
                    {{ tab.statusLabel(row.status) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column
                :label="t('customsPages.declarations.businessRecords.colOccurredAt')"
                min-width="160"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ formatOccurredAt(row.occurredAt) }}</template>
              </el-table-column>
            </el-table>
          </div>
          <DetailListPanelEmpty v-else size="low" :description="t('customsPages.declarations.businessRecords.empty')" />
        </template>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import CrmDataTable from '@/components/CrmDataTable.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import VendorNameReadonlyText from '@/components/Vendor/VendorNameReadonlyText.vue'
import CustomerExtendColumnHeader from '@/components/list/CustomerExtendColumnHeader.vue'
import CustomerExtendCell from '@/components/list/CustomerExtendCell.vue'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { buildSalesOrderItemListColumns } from '@/composables/buildSalesOrderItemListColumns'
import { buildStockOutNotifyListColumns } from '@/composables/buildStockOutNotifyListColumns'
import { buildPackingListColumns } from '@/composables/buildPackingListColumns'
import { buildStockOutListColumns } from '@/composables/buildStockOutListColumns'
import { buildArrivalNoticeListColumns } from '@/composables/buildArrivalNoticeListColumns'
import { buildPurchaseOrderItemListColumns } from '@/composables/buildPurchaseOrderItemListColumns'
import { useCustomerExtendColumn } from '@/composables/useCustomerExtendColumn'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { translateSalesOrderStatus, salesOrderStatusTagType } from '@/constants/salesOrderStatus'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatDisplayDate, formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { formatTotalAmountNumber, formatUnitPriceNumber, listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import type { SalesOrderItemLineRow } from '@/stores/salesOrderItemListBasket'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useAuthStore } from '@/stores/auth'
import {
  fetchCustomsDeclarationBusinessRecords,
  type CustomsDeclarationBusinessRecordRowDto,
  type CustomsDeclarationBusinessRecordsDto
} from '@/api/customs'
import { stockOutApi, type StockOutDto, type StockOutRequestDto } from '@/api/stockOut'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { STOCK_OUT_NOTIFY_CUSTOMS_STATUS } from '@/constants/stockOutNotifyCustomsStatus'
import { StockOutTypeCode } from '@/constants/stockOutType'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { logisticsApi, type StockInNotifyDto, type StockInNotifyItemDto } from '@/api/logistics'
import { packingApi, packingMaterialTypeLabel, packingStatusLabel, type PackingListItem } from '@/api/packing'
import { purchaseOrderApi, type PurchaseOrderItemListLineRow } from '@/api/purchaseOrder'
import { formatDate as formatDateTimeZh } from '@/utils/date'

const props = defineProps<{
  declarationId: string
}>()

const router = useRouter()
const { t, locale } = useI18n()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const {
  activeField: customerExtendActiveField,
  colWidth: customerExtendColWidth,
  colMinWidth: customerExtendColMinWidth,
  setActiveField: setCustomerExtendActiveField
} = useCustomerExtendColumn()
const authStore = useAuthStore()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions, expressOptions } = useLogisticsFormDict()
const loading = ref(false)
const records = ref<CustomsDeclarationBusinessRecordsDto | null>(null)
const activeTab = ref('salesOrder')

const canViewCustomer = computed(
  () => authStore.hasPermission('customer.info.read') || authStore.hasPermission('sales-order.read')
)
const canViewAmount = computed(() => authStore.hasPermission('sales.amount.read'))
const listCustomerColumnOk = computed(() => canViewCustomer.value && !maskSaleSensitiveFields.value)
const listShowAmountColumns = computed(() => canViewAmount.value && !maskSaleSensitiveFields.value)
const canViewPoVendor = computed(
  () =>
    !maskPurchaseSensitiveFields.value &&
    (authStore.hasPermission('vendor.info.read') ||
      authStore.hasPermission('vendor.read') ||
      authStore.hasPermission('purchase-order.read') ||
      authStore.hasPermission('purchase-order.write'))
)
const canViewPoPurchaseUser = computed(
  () => authStore.hasPermission('purchase.user.read') || authStore.hasPermission('purchase-order.read')
)
const canViewPoAmount = computed(
  () => !maskPurchaseSensitiveFields.value && authStore.hasPermission('purchase.amount.read')
)

const salesOrderItemRows = computed(() => records.value?.salesOrderItems ?? [])
const purchaseOrderItemRows = computed(() => records.value?.purchaseOrderItems ?? [])
const stockOutNotifyItemRows = computed(() => records.value?.stockOutNotifyItems ?? [])
const customsStockOutNotifyItemRows = computed(() => records.value?.customsStockOutNotifyItems ?? [])
const packingItemRows = computed(() => records.value?.packingItems ?? [])
const customsPackingItemRows = computed(() => records.value?.customsPackingItems ?? [])
const stockOutItemRows = computed(() => records.value?.stockOutItems ?? [])
const customsStockOutItemRows = computed(() => records.value?.customsStockOutItems ?? [])
const customsArrivalNotifyItemRows = computed(() => records.value?.customsArrivalNotifyItems ?? [])

const soItemColumns = computed<CrmTableColumnDef[]>(() => {
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

const purchaseOrderItemColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return buildPurchaseOrderItemListColumns({
    t,
    canViewVendor: canViewPoVendor.value,
    canViewPurchaseUser: canViewPoPurchaseUser.value,
    canViewAmount: canViewPoAmount.value,
    opColWidth: 0,
    opColMinWidth: 0,
    withSelection: false,
    withActions: false
  })
})

const stockOutNotifyColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return buildStockOutNotifyListColumns({
    t,
    opColWidth: 0,
    opColMinWidth: 0,
    withSelection: false,
    withActions: false
  })
})

const packingColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return buildPackingListColumns({
    t,
    opColWidth: 0,
    opColMinWidth: 0,
    withSelection: false,
    withActions: false
  })
})

const stockOutColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  void customerExtendColWidth.value
  return buildStockOutListColumns({
    t,
    opColWidth: 0,
    opColMinWidth: 0,
    withSelection: false,
    withActions: false,
    withCustomerExtend: true,
    customerExtendColWidth: customerExtendColWidth.value,
    customerExtendColMinWidth: customerExtendColMinWidth.value
  })
})

const arrivalNoticeColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return buildArrivalNoticeListColumns({
    t,
    opColWidth: 0,
    opColMinWidth: 0,
    withActions: false
  })
})

function isStockOutNotifyTab(key: BusinessTabKey) {
  return key === 'stockOutNotify' || key === 'customsStockOutNotify'
}

function isPackingTab(key: BusinessTabKey) {
  return key === 'packing' || key === 'customsPacking'
}

function isStockOutTab(key: BusinessTabKey) {
  return key === 'stockOut' || key === 'customsStockOut'
}

function stockOutNotifyRows(key: BusinessTabKey): StockOutRequestDto[] {
  if (key === 'stockOutNotify') return stockOutNotifyItemRows.value
  if (key === 'customsStockOutNotify') return customsStockOutNotifyItemRows.value
  return []
}

function stockOutRows(key: BusinessTabKey): StockOutDto[] {
  if (key === 'stockOut') return stockOutItemRows.value
  if (key === 'customsStockOut') return customsStockOutItemRows.value
  return []
}

function stockOutListStatusLabel(s: unknown) {
  const n = Number(s)
  switch (n) {
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

function isCustomsStockOutRow(row: StockOutDto): boolean {
  return Number(row.stockOutType) === StockOutTypeCode.Customs
}

function stockOutSalesNotifyTooltip(row: StockOutDto): string {
  const code = String(row.salesStockOutNotifyCode ?? '').trim()
  if (!code) return ''
  return t('stockOutList.salesNotifyCodeTooltip', { code })
}

function formatStockOutDateTime(v?: string | null) {
  if (v == null || v === '') return '—'
  const s = formatDisplayDateTime(v)
  return s === '--' ? '—' : s
}

function formatStockOutPackingCount(v?: number | null) {
  if (v == null || Number.isNaN(Number(v))) return '—'
  return String(v)
}

function onStockOutDblclick(row: StockOutDto) {
  const id = String(row.id ?? '').trim()
  if (id) router.push({ name: 'StockOutDetail', params: { id } })
}

function onArrivalNotifyDblclick(row: StockInNotifyDto) {
  const id = String(row.id ?? '').trim()
  if (id) router.push({ name: 'ArrivalNoticeList', query: { noticeId: id } })
}

const arrivalNum = (v: unknown) => Number(v ?? 0)

function arrivalQtyFromItems(
  items: StockInNotifyItemDto[] | undefined,
  key: 'arrivedQty' | 'qty' | 'passedQty'
) {
  return Number((items || []).reduce((s, x) => s + arrivalNum(x?.[key]), 0).toFixed(4))
}

function arrivalPickQty(
  rowVal: number | undefined | null,
  items: StockInNotifyItemDto[] | undefined,
  itemKey: 'qty' | 'arrivedQty' | 'passedQty'
) {
  return rowVal != null && !Number.isNaN(Number(rowVal)) ? Number(rowVal) : arrivalQtyFromItems(items, itemKey)
}

function arrivalExpectQty(row: StockInNotifyDto) {
  return arrivalPickQty(row.expectQty, row.items, 'qty')
}

function arrivalReceiveQty(row: StockInNotifyDto) {
  return arrivalPickQty(row.receiveQty, row.items, 'arrivedQty')
}

function arrivalPassedQty(row: StockInNotifyDto) {
  return arrivalPickQty(row.passedQty, row.items, 'passedQty')
}

function arrivalRawPn(row: StockInNotifyDto) {
  return (row.pn != null && row.pn !== '' ? row.pn : row.items?.[0]?.pn) || ''
}

function arrivalRawBrand(row: StockInNotifyDto) {
  return (row.brand != null && row.brand !== '' ? row.brand : row.items?.[0]?.brand) || ''
}

function arrivalDisplayPn(row: StockInNotifyDto) {
  return arrivalRawPn(row) || '—'
}

function arrivalDisplayBrand(row: StockInNotifyDto) {
  return arrivalRawBrand(row) || '—'
}

function formatArrivalQtyCell(v: unknown) {
  if (v == null || v === '') return '—'
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString('zh-CN')
}

function formatArrivalExpectedDate(v?: string | null) {
  return v ? formatDisplayDate(v) : '—'
}

function formatArrivalCreateTimeParts(v?: string | null) {
  if (!v) return null
  return formatDisplayDateTime2DigitYearParts(v)
}

function arrivalPickShipmentMethod(row: StockInNotifyDto): string | null | undefined {
  const r = row as unknown as Record<string, unknown>
  return (r.shipmentMethod ?? r.ShipmentMethod) as string | null | undefined
}

function arrivalPickCourierTrackingNo(row: StockInNotifyDto): string | null | undefined {
  const r = row as unknown as Record<string, unknown>
  return (r.courierTrackingNo ?? r.CourierTrackingNo) as string | null | undefined
}

function arrivalDisplayCourierTrackingNo(row: StockInNotifyDto): string {
  const s = String(arrivalPickCourierTrackingNo(row) ?? '').trim()
  return s || '—'
}

function arrivalRegionTypeLabel(row: StockInNotifyDto) {
  const r = row as unknown as Record<string, unknown>
  const n = normalizeRegionType(r.regionType ?? r.RegionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

function packingRows(key: BusinessTabKey): PackingListItem[] {
  if (key === 'packing') return packingItemRows.value
  if (key === 'customsPacking') return customsPackingItemRows.value
  return []
}

function formatPackingDateTimeParts(v?: string | null) {
  if (!v) return null
  return formatDisplayDateTime2DigitYearParts(v)
}

function onPackingDblclick(row: PackingListItem) {
  const id = String(row.id ?? '').trim()
  if (id) router.push({ name: 'PackingDetail', params: { id } })
}

function resolveNotifyStockOutType(v: unknown): number {
  const n = Number(v)
  return Number.isFinite(n) ? n : StockOutTypeCode.Sales
}

function isCustomsNotifyRow(row: StockOutRequestDto): boolean {
  return resolveNotifyStockOutType(row.stockOutType) === StockOutTypeCode.Customs
}

function notifySalesNotifyTooltip(row: StockOutRequestDto): string {
  const code = String(row.salesStockOutNotifyCode ?? '').trim()
  if (!code) return ''
  return t('stockOutNotifyList.salesNotifyCodeTooltip', { code })
}

function notifyStatusLabel(s: unknown) {
  const n = Number(s)
  if (n === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return t('stockOutNotifyList.status.pendingCustoms')
  if (n === STOCK_OUT_REQUEST_STATUS.PendingPacking) return t('stockOutNotifyList.status.pendingPacking')
  if (n === STOCK_OUT_REQUEST_STATUS.Packed) return t('stockOutNotifyList.status.packed')
  if (n === STOCK_OUT_REQUEST_STATUS.StockedOut) return t('stockOutNotifyList.status.stockedOut')
  if (n === STOCK_OUT_REQUEST_STATUS.Cancelled) return t('stockOutNotifyList.status.cancelled')
  return t('stockOutNotifyList.status.unknown')
}

function notifyCustomsStatusLabel(code?: number | null): string {
  const n = Number(code ?? 0)
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.NotRequired) return t('stockOutNotifyList.customsStatus.notRequired')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.PendingCustoms) return t('stockOutNotifyList.customsStatus.pendingCustoms')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.InCustoms) return t('stockOutNotifyList.customsStatus.inCustoms')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.Completed) return t('stockOutNotifyList.customsStatus.completed')
  return '—'
}

function notifyRegionTypeLabel(row: StockOutRequestDto) {
  const r = row as unknown as Record<string, unknown>
  const n = normalizeRegionType(r.regionType ?? r.RegionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

function shipmentMethodDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  const hit = shipmentArrivalOptions.value.find((o) => String(o.value) === c)
  return hit?.label ?? c
}

function expressCompanyDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  const hit = expressOptions.value.find((o) => String(o.value) === c)
  return hit?.label ?? c
}

function formatNotifyDateTime(v?: string | null) {
  if (v == null || v === '') return '—'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
}

function onStockOutNotifyDblclick(row: StockOutRequestDto) {
  const id = String(row.id ?? '').trim()
  if (id) router.push({ name: 'StockOutNotifyDetail', params: { id } })
}

/** 后端未部署 items 字段时，按摘要行 Id 从出库通知列表 API 补齐完整列。 */
async function hydrateStockOutNotifyItemsIfNeeded(
  dto: CustomsDeclarationBusinessRecordsDto
): Promise<CustomsDeclarationBusinessRecordsDto> {
  const needStock = dto.stockOutNotifyItems.length === 0 && dto.stockOutNotifies.length > 0
  const needCustoms = dto.customsStockOutNotifyItems.length === 0 && dto.customsStockOutNotifies.length > 0
  if (!needStock && !needCustoms) return dto

  const page = await stockOutApi.getRequestListPaged({ page: 1, pageSize: 2000 })
  const byId = new Map(page.items.map((i) => [i.id.trim().toLowerCase(), i]))

  const pickRows = (summary: CustomsDeclarationBusinessRecordRowDto[]) =>
    summary
      .map((r) => byId.get(String(r.id ?? '').trim().toLowerCase()))
      .filter((x): x is StockOutRequestDto => Boolean(x))

  return {
    ...dto,
    stockOutNotifyItems: needStock ? pickRows(dto.stockOutNotifies) : dto.stockOutNotifyItems,
    customsStockOutNotifyItems: needCustoms ? pickRows(dto.customsStockOutNotifies) : dto.customsStockOutNotifyItems
  }
}

/** 后端未部署 items 字段时，按摘要行 Id 从装箱单列表 API 补齐完整列。 */
async function hydratePackingItemsIfNeeded(
  dto: CustomsDeclarationBusinessRecordsDto
): Promise<CustomsDeclarationBusinessRecordsDto> {
  const needPacking = dto.packingItems.length === 0 && dto.packings.length > 0
  const needCustoms = dto.customsPackingItems.length === 0 && dto.customsPackings.length > 0
  if (!needPacking && !needCustoms) return dto

  const page = await packingApi.getListPaged({ page: 1, pageSize: 2000 })
  const byId = new Map(page.items.map((i) => [i.id.trim().toLowerCase(), i]))

  const pickRows = (summary: CustomsDeclarationBusinessRecordRowDto[]) =>
    summary
      .map((r) => byId.get(String(r.id ?? '').trim().toLowerCase()))
      .filter((x): x is PackingListItem => Boolean(x))

  return {
    ...dto,
    packingItems: needPacking ? pickRows(dto.packings) : dto.packingItems,
    customsPackingItems: needCustoms ? pickRows(dto.customsPackings) : dto.customsPackingItems
  }
}

/** 后端未部署 items 字段时，按摘要行 Id 从出库单列表 API 补齐完整列。 */
async function hydrateStockOutItemsIfNeeded(
  dto: CustomsDeclarationBusinessRecordsDto
): Promise<CustomsDeclarationBusinessRecordsDto> {
  const needStock = dto.stockOutItems.length === 0 && dto.stockOuts.length > 0
  const needCustoms = dto.customsStockOutItems.length === 0 && dto.customsStockOuts.length > 0
  if (!needStock && !needCustoms) return dto

  const page = await stockOutApi.getListPaged({ page: 1, pageSize: 2000 })
  const byId = new Map(page.items.map((i) => [i.id.trim().toLowerCase(), i]))

  const pickRows = (summary: CustomsDeclarationBusinessRecordRowDto[]) =>
    summary
      .map((r) => byId.get(String(r.id ?? '').trim().toLowerCase()))
      .filter((x): x is StockOutDto => Boolean(x))

  return {
    ...dto,
    stockOutItems: needStock ? pickRows(dto.stockOuts) : dto.stockOutItems,
    customsStockOutItems: needCustoms ? pickRows(dto.customsStockOuts) : dto.customsStockOutItems
  }
}

/** 后端未部署 items 字段时，按摘要行 Id 从到货通知列表 API 补齐完整列。 */
async function hydrateCustomsArrivalNotifyItemsIfNeeded(
  dto: CustomsDeclarationBusinessRecordsDto
): Promise<CustomsDeclarationBusinessRecordsDto> {
  const need = dto.customsArrivalNotifyItems.length === 0 && dto.customsArrivalNotifies.length > 0
  if (!need) return dto

  const page = await logisticsApi.getArrivalNotices({ page: 1, pageSize: 2000 })
  const byId = new Map(page.items.map((i) => [i.id.trim().toLowerCase(), i]))

  const pickRows = (summary: CustomsDeclarationBusinessRecordRowDto[]) =>
    summary
      .map((r) => byId.get(String(r.id ?? '').trim().toLowerCase()))
      .filter((x): x is StockInNotifyDto => Boolean(x))

  return {
    ...dto,
    customsArrivalNotifyItems: pickRows(dto.customsArrivalNotifies)
  }
}

type BusinessTabKey =
  | 'salesOrder'
  | 'purchaseOrder'
  | 'stockOutNotify'
  | 'customsStockOutNotify'
  | 'customsPacking'
  | 'customsStockOut'
  | 'customsArrivalNotify'
  | 'customsStockIn'
  | 'packing'
  | 'stockOut'

interface BusinessTabDef {
  key: BusinessTabKey
  label: string
  rows: CustomsDeclarationBusinessRecordRowDto[]
  count: number
  routeName: string | null
  route: (row: CustomsDeclarationBusinessRecordRowDto) => Record<string, unknown>
  canLink: (row: CustomsDeclarationBusinessRecordRowDto) => boolean
  statusLabel: (status: unknown) => string
  statusTagType: (status: unknown) => '' | 'success' | 'warning' | 'info' | 'danger' | 'primary'
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

function sellOrderItemStatusLabel(s: unknown) {
  const n = Number(s)
  if (n === 1) return t('customsPages.declarations.businessRecords.sellLineCancelled')
  return t('customsPages.declarations.businessRecords.sellLineNormal')
}

function soItemSettlementCurrencyLabel(code: unknown): string {
  const c = Number(code)
  if (!Number.isFinite(c)) return '—'
  return CURRENCY_CODE_TO_TEXT[c as keyof typeof CURRENCY_CODE_TO_TEXT] ?? '—'
}

function soItemStatusText(s: unknown) {
  return translateSalesOrderStatus(Number(s), t)
}

function soItemStatusTagType(s: unknown): '' | 'success' | 'warning' | 'info' | 'danger' {
  return salesOrderStatusTagType(Number(s)) as '' | 'success' | 'warning' | 'info' | 'danger'
}

function soItemExtendTriTagType(v?: unknown): '' | 'success' | 'warning' | 'info' | 'danger' {
  const n = Number(v)
  const map: Record<number, '' | 'info' | 'success' | 'warning' | 'danger'> = {
    0: 'info',
    1: 'warning',
    2: 'success'
  }
  return Number.isFinite(n) ? (map[n] ?? 'info') : 'info'
}

function soItemExtendTriLabel(
  kind: 'purchase' | 'stockIn' | 'stockOut' | 'stockOutNotify' | 'receipt' | 'invoice',
  v?: unknown
): string {
  const n = Number(v)
  const slot = n === 2 ? 'complete' : n === 1 ? 'partial' : 'pending'
  return t(`salesOrderItemList.extendProgress.${kind}.${slot}`)
}

function soItemFormatDt(v: unknown) {
  if (v == null || v === '') return '—'
  const s = formatDisplayDateTime(String(v))
  return s === '--' ? '—' : s
}

function onSoItemDblclick(row: SalesOrderItemLineRow) {
  const orderId = String(row.sellOrderId ?? '').trim()
  const itemId = String(row.sellOrderItemId ?? '').trim()
  if (!orderId) return
  router.push({
    name: 'SalesOrderDetail',
    params: { id: orderId },
    query: itemId ? { sellOrderItemId: itemId } : undefined
  })
}

function isPoItemStockingPurchase(row: PurchaseOrderItemListLineRow) {
  return Number(row.purchaseOrderType) === 2
}

function poItemStatusText(s: unknown) {
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
  const k = keyMap[Number(s)]
  return k ? t(`purchaseOrderItemList.itemStatus.${k}`) : String(s ?? '—')
}

function poItemStatusTagType(s: unknown): '' | 'success' | 'warning' | 'info' | 'danger' | 'primary' {
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
  return map[Number(s)] ?? 'info'
}

function poItemFormatDt(v: unknown) {
  if (v == null || v === '') return '—'
  const s = formatDisplayDateTime(String(v))
  return s === '--' ? '—' : s
}

function poExtendTriTagType(v: number): '' | 'info' | 'success' | 'warning' | 'danger' {
  const map: Record<number, '' | 'info' | 'success' | 'warning' | 'danger'> = {
    0: 'info',
    1: 'warning',
    2: 'success'
  }
  return map[v] ?? 'info'
}

function poPaymentRequestProgressText(v: number) {
  if (Number(v) >= 1) return t('purchaseOrderItemList.extendProgress.paymentRequestApplied')
  return t('purchaseOrderItemList.extendProgress.paymentRequestPending')
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

function onPurchaseOrderItemDblclick(row: PurchaseOrderItemListLineRow) {
  if (maskPurchaseSensitiveFields.value) return
  const purchaseOrderId = String(row.purchaseOrderId ?? '').trim()
  const purchaseOrderItemId = String(row.purchaseOrderItemId ?? '').trim()
  if (!purchaseOrderId || !purchaseOrderItemId) return
  router.push({
    name: 'PurchaseOrderDetail',
    params: { id: purchaseOrderId },
    query: { purchaseOrderItemId }
  })
}

function sellOrderItemStatusTagType(s: unknown): '' | 'success' | 'warning' | 'info' | 'danger' {
  return Number(s) === 1 ? 'info' : 'success'
}

function purchaseOrderItemStatusLabel(s: unknown) {
  const n = Number(s)
  return Number.isFinite(n) ? (PO_ITEM_STATUS_TEXT[n] ?? String(s)) : '—'
}

function purchaseOrderItemStatusTagType(s: unknown): '' | 'success' | 'warning' | 'info' | 'danger' {
  const n = Number(s)
  if (n === 100 || n === 60) return 'success'
  if (n === 2 || n === 20) return 'warning'
  if (n === -1 || n === -2) return 'info'
  return 'info'
}

function stockOutRequestStatusLabel(s: unknown) {
  const n = Number(s)
  if (n === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return t('stockOutNotifyList.status.pendingCustoms')
  if (n === STOCK_OUT_REQUEST_STATUS.PendingPacking) return t('stockOutNotifyList.status.pendingPacking')
  if (n === STOCK_OUT_REQUEST_STATUS.Packed) return t('stockOutNotifyList.status.packed')
  if (n === STOCK_OUT_REQUEST_STATUS.StockedOut) return t('stockOutNotifyList.status.stockedOut')
  if (n === STOCK_OUT_REQUEST_STATUS.Cancelled) return t('stockOutNotifyList.status.cancelled')
  return t('stockOutNotifyList.status.unknown')
}

function stockOutRequestStatusTagType(s: unknown): '' | 'success' | 'warning' | 'info' | 'danger' {
  const n = Number(s)
  if (n === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return 'warning'
  if (n === STOCK_OUT_REQUEST_STATUS.PendingPacking) return 'info'
  if (n === STOCK_OUT_REQUEST_STATUS.Packed) return 'warning'
  if (n === STOCK_OUT_REQUEST_STATUS.StockedOut) return 'success'
  if (n === STOCK_OUT_REQUEST_STATUS.Cancelled) return 'info'
  return 'info'
}

function stockOutStatusLabel(s: unknown) {
  const n = Number(s)
  switch (n) {
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

function stockOutStatusTagType(s: unknown): '' | 'success' | 'warning' | 'info' | 'danger' {
  const n = Number(s)
  if (n === 2 || n === 4) return 'success'
  if (n === 1) return 'warning'
  if (n === 3) return 'info'
  return 'info'
}

function stockInStatusLabel(s: unknown) {
  const n = Number(s)
  switch (n) {
    case 0:
      return t('stockInList.status.draft')
    case 1:
      return t('stockInList.status.pending')
    case 2:
      return t('stockInList.status.done')
    case 3:
      return t('stockInList.status.cancelled')
    default:
      return String(s ?? '—')
  }
}

function stockInStatusTagType(s: unknown): '' | 'success' | 'warning' | 'info' | 'danger' {
  const n = Number(s)
  if (n === 2) return 'success'
  if (n === 1) return 'warning'
  if (n === 3) return 'info'
  return 'info'
}

function arrivalNotifyStatusLabel(s: unknown) {
  const n = Number(s)
  const keyMap: Record<number, 'new' | 'notArrived' | 'pendingQc' | 'qcDone' | 'stocked'> = {
    1: 'new',
    10: 'notArrived',
    20: 'pendingQc',
    30: 'qcDone',
    100: 'stocked'
  }
  const k = keyMap[n]
  return k ? t(`arrivalNoticeList.status.${k}`) : t('arrivalNoticeList.statusUnknown')
}

function arrivalNotifyStatusTagType(s: unknown): '' | 'success' | 'warning' | 'info' | 'danger' | 'primary' {
  const n = Number(s)
  const map: Record<number, '' | 'success' | 'warning' | 'info' | 'danger' | 'primary'> = {
    1: 'info',
    10: 'warning',
    20: 'primary',
    30: 'success',
    100: 'success'
  }
  return map[n] ?? 'info'
}

function packingStatusTagType(s: unknown): '' | 'success' | 'warning' | 'info' | 'danger' {
  const n = Number(s)
  if (n === 100) return 'success'
  if (n === 20 || n === 30) return 'warning'
  if (n === -1) return 'info'
  return 'info'
}

function formatOccurredAt(v?: string | null) {
  if (!v) return '—'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
}

function rowCode(row: CustomsDeclarationBusinessRecordRowDto) {
  const r = row as unknown as Record<string, unknown>
  const code = row.code ?? r.Code
  return typeof code === 'string' && code.trim() ? code.trim() : '—'
}

function rowId(row: CustomsDeclarationBusinessRecordRowDto) {
  const r = row as unknown as Record<string, unknown>
  return String(row.id ?? r.Id ?? '').trim()
}

function rowParentId(row: CustomsDeclarationBusinessRecordRowDto) {
  const r = row as unknown as Record<string, unknown>
  return String(row.parentId ?? r.ParentId ?? '').trim()
}

const tabs = computed<BusinessTabDef[]>(() => {
  const r = records.value
  const empty: CustomsDeclarationBusinessRecordRowDto[] = []
  const salesOrderItems = r?.salesOrderItems ?? []
  const salesOrders = r?.salesOrders ?? empty
  const purchaseOrders = r?.purchaseOrders ?? empty
  const purchaseOrderItems = r?.purchaseOrderItems ?? []
  const stockOutNotifies = r?.stockOutNotifies ?? empty
  const stockOutNotifyItems = r?.stockOutNotifyItems ?? []
  const customsStockOutNotifies = r?.customsStockOutNotifies ?? empty
  const customsStockOutNotifyItems = r?.customsStockOutNotifyItems ?? []
  const customsPackings = r?.customsPackings ?? empty
  const customsPackingItems = r?.customsPackingItems ?? []
  const customsStockOuts = r?.customsStockOuts ?? empty
  const customsStockOutItems = r?.customsStockOutItems ?? []
  const customsArrivalNotifies = r?.customsArrivalNotifies ?? empty
  const customsArrivalNotifyItems = r?.customsArrivalNotifyItems ?? []
  const customsStockIns = r?.customsStockIns ?? empty
  const packings = r?.packings ?? empty
  const packingItems = r?.packingItems ?? []
  const stockOuts = r?.stockOuts ?? empty
  const stockOutItems = r?.stockOutItems ?? []

  return [
    {
      key: 'salesOrder',
      label: t('customsPages.declarations.businessRecords.salesOrder'),
      rows: salesOrders,
      count: salesOrderItems.length || salesOrders.length,
      routeName: 'SalesOrderDetail',
      route: (row) => ({
        name: 'SalesOrderDetail',
        params: { id: rowParentId(row) },
        query: { sellOrderItemId: rowId(row) }
      }),
      canLink: (row) => Boolean(rowId(row) && rowParentId(row)),
      statusLabel: sellOrderItemStatusLabel,
      statusTagType: sellOrderItemStatusTagType
    },
    {
      key: 'purchaseOrder',
      label: t('customsPages.declarations.businessRecords.purchaseOrder'),
      rows: purchaseOrders,
      count: purchaseOrderItems.length || purchaseOrders.length,
      routeName: 'PurchaseOrderDetail',
      route: (row) => ({
        name: 'PurchaseOrderDetail',
        params: { id: rowParentId(row) },
        query: { purchaseOrderItemId: rowId(row) }
      }),
      canLink: (row) => Boolean(rowId(row) && rowParentId(row)),
      statusLabel: purchaseOrderItemStatusLabel,
      statusTagType: purchaseOrderItemStatusTagType
    },
    {
      key: 'stockOutNotify',
      label: t('customsPages.declarations.businessRecords.stockOutNotify'),
      rows: stockOutNotifies,
      count: stockOutNotifyItems.length || stockOutNotifies.length,
      routeName: 'StockOutNotifyDetail',
      route: (row) => ({ name: 'StockOutNotifyDetail', params: { id: rowId(row) } }),
      canLink: (row) => Boolean(rowId(row)),
      statusLabel: stockOutRequestStatusLabel,
      statusTagType: stockOutRequestStatusTagType
    },
    {
      key: 'customsStockOutNotify',
      label: t('customsPages.declarations.businessRecords.customsStockOutNotify'),
      rows: customsStockOutNotifies,
      count: customsStockOutNotifyItems.length || customsStockOutNotifies.length,
      routeName: 'StockOutNotifyDetail',
      route: (row) => ({ name: 'StockOutNotifyDetail', params: { id: rowId(row) } }),
      canLink: (row) => Boolean(rowId(row)),
      statusLabel: stockOutRequestStatusLabel,
      statusTagType: stockOutRequestStatusTagType
    },
    {
      key: 'customsPacking',
      label: t('customsPages.declarations.businessRecords.customsPacking'),
      rows: customsPackings,
      count: customsPackingItems.length || customsPackings.length,
      routeName: 'PackingDetail',
      route: (row) => ({ name: 'PackingDetail', params: { id: rowId(row) } }),
      canLink: (row) => Boolean(rowId(row)),
      statusLabel: (s) => packingStatusLabel(Number(s)),
      statusTagType: packingStatusTagType
    },
    {
      key: 'customsStockOut',
      label: t('customsPages.declarations.businessRecords.customsStockOut'),
      rows: customsStockOuts,
      count: customsStockOutItems.length || customsStockOuts.length,
      routeName: 'StockOutDetail',
      route: (row) => ({ name: 'StockOutDetail', params: { id: rowId(row) } }),
      canLink: (row) => Boolean(rowId(row)),
      statusLabel: stockOutStatusLabel,
      statusTagType: stockOutStatusTagType
    },
    {
      key: 'customsArrivalNotify',
      label: t('customsPages.declarations.businessRecords.customsArrivalNotify'),
      rows: customsArrivalNotifies,
      count: customsArrivalNotifyItems.length || customsArrivalNotifies.length,
      routeName: 'ArrivalNoticeList',
      route: (row) => ({ name: 'ArrivalNoticeList', query: { noticeId: rowId(row) } }),
      canLink: (row) => Boolean(rowId(row)),
      statusLabel: arrivalNotifyStatusLabel,
      statusTagType: arrivalNotifyStatusTagType
    },
    {
      key: 'customsStockIn',
      label: t('customsPages.declarations.businessRecords.customsStockIn'),
      rows: customsStockIns,
      count: customsStockIns.length,
      routeName: 'StockInDetail',
      route: (row) => ({ name: 'StockInDetail', params: { id: rowId(row) } }),
      canLink: (row) => Boolean(rowId(row)),
      statusLabel: stockInStatusLabel,
      statusTagType: stockInStatusTagType
    },
    {
      key: 'packing',
      label: t('customsPages.declarations.businessRecords.packing'),
      rows: packings,
      count: packingItems.length || packings.length,
      routeName: 'PackingDetail',
      route: (row) => ({ name: 'PackingDetail', params: { id: rowId(row) } }),
      canLink: (row) => Boolean(rowId(row)),
      statusLabel: (s) => packingStatusLabel(Number(s)),
      statusTagType: packingStatusTagType
    },
    {
      key: 'stockOut',
      label: t('customsPages.declarations.businessRecords.stockOut'),
      rows: stockOuts,
      count: stockOutItems.length || stockOuts.length,
      routeName: 'StockOutDetail',
      route: (row) => ({ name: 'StockOutDetail', params: { id: rowId(row) } }),
      canLink: (row) => Boolean(rowId(row)),
      statusLabel: stockOutStatusLabel,
      statusTagType: stockOutStatusTagType
    }
  ]
})

/** 后端未部署 purchaseOrderItems 字段时，按摘要行 Id 从采购明细列表 API 补齐完整列。 */
async function hydratePurchaseOrderItemsIfNeeded(
  dto: CustomsDeclarationBusinessRecordsDto
): Promise<CustomsDeclarationBusinessRecordsDto> {
  if (dto.purchaseOrderItems.length > 0 || dto.purchaseOrders.length === 0) return dto

  const data = (await purchaseOrderApi.getItemLinesPage({ page: 1, pageSize: 2000 })) as {
    items?: PurchaseOrderItemListLineRow[]
  }
  const items = data.items ?? []
  const byId = new Map(items.map((i) => [String(i.purchaseOrderItemId ?? '').trim().toLowerCase(), i]))

  const rows = dto.purchaseOrders
    .map((r) => byId.get(String(r.id ?? '').trim().toLowerCase()))
    .filter((x): x is PurchaseOrderItemListLineRow => Boolean(x))

  return { ...dto, purchaseOrderItems: rows }
}

async function load() {
  const id = props.declarationId?.trim()
  if (!id) {
    records.value = null
    return
  }
  loading.value = true
  try {
    const dto = await fetchCustomsDeclarationBusinessRecords(id)
    const withStockOutNotify = await hydrateStockOutNotifyItemsIfNeeded(dto)
    const withPacking = await hydratePackingItemsIfNeeded(withStockOutNotify)
    const withStockOut = await hydrateStockOutItemsIfNeeded(withPacking)
    const withArrival = await hydrateCustomsArrivalNotifyItemsIfNeeded(withStockOut)
    records.value = await hydratePurchaseOrderItemsIfNeeded(withArrival)
  } catch (e: unknown) {
    records.value = null
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  void ensureLogisticsDict()
  void load()
})

watch(
  () => props.declarationId,
  () => {
    void load()
  }
)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.tabs-nav {
  display: flex;
  flex-wrap: wrap;
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

  &:hover {
    color: $text-secondary;
  }

  &--active {
    color: $cyan-primary;
    border-bottom-color: $cyan-primary;
  }
}

.tab-count {
  margin-left: 6px;
  font-size: 11px;
  padding: 1px 6px;
  border-radius: 999px;
  background: rgba(0, 212, 255, 0.1);
  color: $cyan-primary;
}

.tabs-body {
  padding: 20px;
}

.detail-items-table-wrap {
  margin-top: 0;
}

.link-text {
  color: $cyan-primary;
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}

.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;

  &.status-5 {
    background: rgba(156, 89, 182, 0.18);
    color: #9c59b6;
  }

  &.status-10 {
    background: rgba(255, 193, 7, 0.15);
    color: #ffc107;
  }

  &.status-20 {
    background: rgba(0, 212, 255, 0.15);
    color: $cyan-primary;
  }

  &.status-100 {
    background: rgba(70, 191, 145, 0.18);
    color: #46bf91;
  }

  &.status--1 {
    background: rgba(201, 87, 69, 0.18);
    color: #c95745;
  }

  &.status-0 {
    background: rgba(148, 163, 184, 0.18);
    color: #94a3b8;
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
    background: rgba(148, 163, 184, 0.18);
    color: #94a3b8;
  }
  &.status-4 {
    background: rgba(70, 191, 145, 0.22);
    color: #46bf91;
  }

  &.packing-status-10 {
    background: rgba(255, 193, 7, 0.15);
    color: #ffc107;
  }
  &.packing-status-20 {
    background: rgba(0, 212, 255, 0.15);
    color: $cyan-primary;
  }
  &.packing-status-30 {
    background: rgba(100, 149, 237, 0.18);
    color: #8eb4ff;
  }
  &.packing-status-40 {
    background: rgba(255, 193, 7, 0.2);
    color: #ffc107;
  }
  &.packing-status-50 {
    background: rgba(255, 152, 0, 0.15);
    color: #ff9800;
  }
  &.packing-status-100 {
    background: rgba(70, 191, 145, 0.22);
    color: #46bf91;
  }
}

.qty-cell {
  font-weight: 700;
  color: $text-primary;
}

.notify-code-cell {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  max-width: 100%;
}

.notify-code-text {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}

.customs-notify-tag {
  display: inline-flex;
  align-items: center;
  padding: 1px 6px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 600;
  color: #ffc107;
  background: rgba(255, 193, 7, 0.12);
  border: 1px solid rgba(255, 193, 7, 0.35);
  flex-shrink: 0;
}

.stock-out-code-cell {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  max-width: 100%;
}

.mono-cell {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}

.text-secondary {
  color: $text-muted;
}
</style>
