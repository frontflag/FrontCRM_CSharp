<template>
  <div class="stockin-edit-page" v-loading="detailLoading">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
              <path d="M3 9h18" />
              <path d="M9 21V9" />
            </svg>
          </div>
          <h1 class="page-title">{{ isCreateMode ? '新建入库单' : '入库单详情' }}</h1>
          <span v-if="!isCreateMode && detailStatus !== null" :class="['status-badge', `status-${detailStatus}`]">{{ statusLabel(detailStatus) }}</span>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="goBack">返回列表</button>
        <button
          v-if="isCreateMode && canWriteLogisticsData"
          class="btn-primary"
          style="margin-left: 8px"
          @click="handleSubmit"
          :disabled="submitting"
        >
          {{ submitting ? '保存中...' : '保存并入库' }}
        </button>
      </div>
    </div>

    <div class="form-layout">
      <div class="form-card">
        <h3 class="section-title">基础信息</h3>
        <el-form v-if="isCreateMode" :model="form" label-width="90px" class="stockin-form">
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
        <dl v-else class="stockin-report-dl" aria-label="基础信息">
          <div class="stockin-report-row">
            <dt>入库单号</dt>
            <dd class="stockin-code-cell">
              <span>{{ reportCellText(form.stockInCode) }}</span>
              <el-tooltip
                v-if="isCustomsStockInDetail && detailArrivalNotifyTooltip"
                :content="detailArrivalNotifyTooltip"
                placement="top"
                :hide-after="0"
              >
                <span class="customs-notify-tag">{{ t('stockInList.customsNotifyTag') }}</span>
              </el-tooltip>
            </dd>
          </div>
          <div class="stockin-report-row">
            <dt>入库类型</dt>
            <dd>
              <StockBizTypeTag biz="in" :type="detailStockInType" />
            </dd>
          </div>
          <div class="stockin-report-row">
            <dt>仓库编号</dt>
            <dd>{{ reportCellText(displayWarehouseCode) }}</dd>
          </div>
          <div class="stockin-report-row">
            <dt>供应商名称</dt>
            <dd>{{ reportCellText(maskPurchaseSensitiveFields ? '—' : displayVendorName) }}</dd>
          </div>
          <div class="stockin-report-row">
            <dt>到货通知号</dt>
            <dd>{{ reportCellText(form.purchaseOrderId) }}</dd>
          </div>
          <div class="stockin-report-row">
            <dt>入库日期</dt>
            <dd>{{ reportDateTimeText(form.stockInDate) }}</dd>
          </div>
          <div class="stockin-report-row stockin-report-row--block">
            <dt>备注</dt>
            <dd class="stockin-report-multiline">{{ reportCellText(form.remark) }}</dd>
          </div>
        </dl>
      </div>

      <div class="form-card">
        <div class="section-header">
          <h3 class="section-title">入库明细</h3>
          <button v-if="isCreateMode" type="button" class="btn-secondary btn-sm" @click="addRow">新增一行</button>
        </div>
        <div class="detail-items-table-wrap">
          <el-table :data="form.items" class="items-table quantum-table" style="width: 100%">
            <el-table-column type="index" width="50" align="center" />
            <el-table-column v-if="!isCreateMode" label="入库明细编号" min-width="148" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.stockInItemCode) }}</span>
              </template>
            </el-table-column>
            <el-table-column v-if="!isCreateMode" label="入库日期" width="148" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportDateTimeText(row.stockInDate) }}</span>
              </template>
            </el-table-column>
            <el-table-column v-if="!isCreateMode" label="到货通知单号" min-width="140" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.sourceCode) }}</span>
              </template>
            </el-table-column>
            <el-table-column v-if="!isCreateMode" label="采购订单明细编号" min-width="160" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.purchaseOrderItemCode) }}</span>
              </template>
            </el-table-column>
            <el-table-column v-if="!isCreateMode" label="供应商名称" min-width="160" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(maskPurchaseSensitiveFields ? '—' : row.vendorName) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="物料型号" min-width="168" show-overflow-tooltip>
              <template #default="{ row }">
                <el-input
                  v-if="isCreateMode"
                  v-model="row.materialCode"
                  placeholder="物料主数据 Id（UUID）或采购明细行 Id"
                />
                <span v-else class="stockin-report-cell">{{ reportCellText(row.materialName) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="品牌" min-width="120" show-overflow-tooltip>
              <template #default="{ row }">
                <el-input v-if="isCreateMode" v-model="row.materialBrand" placeholder="可选" />
                <span v-else class="stockin-report-cell">{{ reportCellText(row.materialBrand) }}</span>
              </template>
            </el-table-column>
            <el-table-column
              v-if="!isCreateMode"
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
                <el-input-number v-if="isCreateMode" v-model="row.quantity" :min="0" :step="1" />
                <span v-else class="stockin-report-cell stockin-report-cell--num">{{ reportQtyText(row.quantity) }}</span>
              </template>
            </el-table-column>
            <el-table-column
              v-if="!isCreateMode"
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
            <el-table-column v-if="!isCreateMode" label="地域类型" width="100" align="center" header-align="center">
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ regionTypeLabel(row.regionType) }}</span>
              </template>
            </el-table-column>
            <el-table-column v-if="!isCreateMode" label="仓库" width="100" show-overflow-tooltip>
              <template #default="{ row }">
                <span class="stockin-report-cell">{{ reportCellText(row.warehouseCode) }}</span>
              </template>
            </el-table-column>
            <el-table-column v-if="!isCreateMode" label="入库类型" width="110" show-overflow-tooltip>
              <template #default="{ row }">
                <StockBizTypeTag biz="in" :type="row.stockInType ?? detailStockInType" />
              </template>
            </el-table-column>
            <el-table-column v-if="isCreateMode" label="单位" width="90" align="center" header-align="center">
              <template #default="{ row }">
                <el-input v-model="row.unit" placeholder="PCS" />
              </template>
            </el-table-column>
            <el-table-column v-if="isCreateMode" label="单价" width="120" align="right" header-align="right">
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
                <el-input v-if="isCreateMode" v-model="row.batchNo" placeholder="批次号" />
                <span v-else class="stockin-report-cell">{{ reportCellText(row.batchNo) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="库位" width="140" show-overflow-tooltip>
              <template #default="{ row }">
                <el-input v-if="isCreateMode" v-model="row.warehouseLocation" placeholder="库位编码" />
                <span v-else class="stockin-report-cell">{{ reportCellText(row.warehouseLocation) }}</span>
              </template>
            </el-table-column>
            <el-table-column
              v-if="!isCreateMode"
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
                    <button type="button" class="action-btn action-btn--primary" @click.stop="openBatchImport(row)">录入批次</button>
                  </div>
                  <el-dropdown v-else trigger="click" placement="bottom-end">
                    <div class="op-more-dropdown-trigger">
                      <button type="button" class="op-more-trigger">...</button>
                    </div>
                    <template #dropdown>
                      <el-dropdown-menu>
                        <el-dropdown-item @click.stop="openBatchImport(row)">
                          <span class="op-more-item op-more-item--primary">录入批次</span>
                        </el-dropdown-item>
                      </el-dropdown-menu>
                    </template>
                  </el-dropdown>
                </div>
              </template>
            </el-table-column>
            <el-table-column
              v-if="isCreateMode"
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

      <div v-if="!isCreateMode" class="form-card">
        <div class="section-header">
          <h3 class="section-title">库存明细</h3>
        </div>
        <div class="detail-items-table-wrap stockin-stock-items-table-wrap">
          <el-table :data="stockItemRows" class="items-table quantum-table" style="width: 100%">
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
                <span class="stockin-report-cell">{{
                  maskPurchaseSensitiveFields ? '—' : reportCellText(row.vendorName)
                }}</span>
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
          </el-table>
          <div v-if="!stockItemRows.length" class="stockin-report-empty">暂无对应库存明细</div>
        </div>
      </div>
    </div>

    <StockInBatchImportDialog
      v-model="batchImportVisible"
      :stock-in-id="stockInHeaderId"
      :stock-in-item-id="batchImportItemId"
      :stock-in-item-code="batchImportItemCode"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { stockInApi, type CreateStockInRequest, type StockInDto, type StockInItemDto } from '@/api/stockIn'
import { inventoryCenterApi, type StockItemListRow } from '@/api/inventoryCenter'
import StockInBatchImportDialog from '@/components/Inventory/StockInBatchImportDialog.vue'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
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
/** 详情页展示：供应商名称 */
const displayVendorName = ref('')
/** 详情页：单头入库类型 / 地域（库存明细行无值时回退） */
const detailStockInType = ref(0)
const detailSourceDisplayNo = ref('')
const detailRegionType = ref(REGION_TYPE_DOMESTIC)
const stockItemRows = ref<StockItemListRow[]>([])

const batchImportVisible = ref(false)
const batchImportItemId = ref('')
const batchImportItemCode = ref('')

const isCreateMode = computed(() => route.name === 'StockInCreate')

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

const stockInHeaderId = computed(() =>
  route.name === 'StockInDetail' && typeof route.params.id === 'string' ? route.params.id : ''
)

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
  displayVendorName.value = ''
  detailStockInType.value = 0
  detailSourceDisplayNo.value = ''
  detailRegionType.value = REGION_TYPE_DOMESTIC
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
  form.stockInCode = d.stockInCode ?? ''
  form.warehouseId = d.warehouseId ?? ''
  form.vendorId = d.vendorId ?? ''
  const wh = pickStr(r, 'detailWarehouseCode', 'DetailWarehouseCode')
  displayWarehouseCode.value = wh || (form.warehouseId ? String(form.warehouseId) : '—')
  const vn = pickStr(r, 'detailVendorName', 'DetailVendorName')
  displayVendorName.value = vn || (form.vendorId ? String(form.vendorId) : '—')
  const parts = [d.sourceCode, d.purchaseOrderItemCode].filter(x => x != null && String(x).trim() !== '')
  form.purchaseOrderId = parts.length ? parts.map(x => String(x).trim()).join(' / ') : ''
  form.stockInDate = normalizeDateForPicker(d.stockInDate)
  form.remark = d.remark ?? ''
  form.totalQuantity = d.totalQuantity ?? 0
  form.operatorId = ''

  const headerStockInDate = normalizeDateForPicker(d.stockInDate)
  const headerSourceCode = (d.sourceCode ?? '').trim()
  const headerVendorName = displayVendorName.value
  const headerWarehouseCode = displayWarehouseCode.value
  const headerRegionType = normalizeRegionType(d.regionType)
  const headerStockInType = Number(d.stockInType) || 0
  detailStockInType.value = headerStockInType
  detailSourceDisplayNo.value = headerSourceCode
  detailRegionType.value = headerRegionType

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
  detailLoading.value = true
  try {
    const data = await stockInApi.getById(id)
    if (!data) {
      ElMessage.error('入库单不存在或无权查看')
      router.replace('/inventory/stock-in')
      return
    }
    applyDetailToForm(data)
    const stockInCode = (data.stockInCode ?? '').trim()
    if (stockInCode) {
      const res = await inventoryCenterApi.searchStockItems({ stockInCode, page: 1, pageSize: 2000 })
      stockItemRows.value = res.items.filter((r) => String(r.stockInId || '').trim() === id)
    } else {
      stockItemRows.value = []
    }
  } catch (e) {
    console.error(e)
    ElMessage.error('加载入库单失败')
    router.replace('/inventory/stock-in')
  } finally {
    detailLoading.value = false
  }
}

watch(
  () => ({ name: route.name, id: route.params.id }),
  async ({ name, id }) => {
    if (name === 'StockInCreate') {
      resetCreateForm()
      return
    }
    if (name === 'StockInDetail' && typeof id === 'string' && id) {
      await loadStockInDetail(id)
    }
  },
  { immediate: true }
)

const statusLabel = (s: number) => {
  switch (s) {
    case 0:
      return '草稿'
    case 1:
      return '待入库'
    case 2:
      return '已入库'
    case 3:
      return '已取消'
    default:
      return '未知'
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
  const id = (row.itemId ?? '').trim()
  if (!id) {
    ElMessage.error('该明细缺少主键，无法录入批次')
    return
  }
  batchImportItemId.value = id
  batchImportItemCode.value = (row.stockInItemCode ?? '').trim()
  batchImportVisible.value = true
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
  margin-bottom: 8px;
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

/* 与订单详情「订单明细」表头/行样式一致（业务列表范式） */
.detail-items-table-wrap {
  margin-top: 4px;
}

/* 库存明细列多：横向滚动 + 表头单行不换行 */
.stockin-stock-items-table-wrap {
  overflow-x: auto;
  .items-table {
    :deep(.el-table__header-wrapper th.el-table__cell .cell) {
      white-space: nowrap;
      word-break: keep-all;
      line-height: 1.35;
    }
  }
}

.items-table {
  --el-table-border-color: transparent;
  --el-table-header-bg-color: var(--crm-table-header-bg);
  --el-table-row-hover-bg-color: var(--crm-table-row-hover);
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-text-color: #{$text-primary};
  --el-table-header-text-color: #{$text-muted};
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
  :deep(.el-table) {
    --el-table-text-color: #{$text-primary};
    color: $text-primary;
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
  :deep(.el-table__header-wrapper) {
    th.el-table__cell {
      background: var(--crm-table-header-bg) !important;
      border-bottom: 1px solid var(--crm-table-header-line) !important;
      border-right: none !important;
      color: $text-muted !important;
      font-size: 12px;
      font-weight: 500;
      letter-spacing: 0.3px;
    }
    th.el-table__cell .cell {
      color: inherit !important;
    }
  }
  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row td.el-table__cell),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row td.el-table__cell) {
    color: $text-primary !important;
    font-size: 13px;
  }
  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row td.el-table__cell .cell),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row td.el-table__cell .cell) {
    color: $text-primary !important;
  }
  :deep(.el-table__cell) {
    .el-button {
      white-space: nowrap !important;
    }
    .cell {
      white-space: nowrap;
    }
  }
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
</style>

