<template>
  <div class="sales-order-detail">
    <!-- 详情 CaptionBar（对齐 detailsCaptionBar规范.md：删除 / 取消订单在「更多」） -->
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
                  {{ captionTitle }}
                </h1>
                <button
                  type="button"
                  class="btn-favorite-star"
                  :class="{ 'is-favorite': soFavorited }"
                  :disabled="favoriteLoading"
                  :title="soFavorited ? '取消收藏' : '收藏订单'"
                  :aria-pressed="soFavorited"
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
                <el-tag :type="getStatusType(order.status)" size="small" effect="dark">
                  {{ getStatusText(order.status) }}
                </el-tag>
              </div>
            </div>
            <div v-if="captionMetaVisible" class="title-meta title-meta--caption">
              <span class="caption-code">{{ order.sellOrderCode }}</span>
            </div>
          </div>
        </div>
      </div>
      <div v-if="order" class="header-right">
        <button v-if="canWriteSo" class="btn-primary" type="button" @click="handleEdit">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
          </svg>
          编辑
        </button>
        <el-dropdown
          trigger="click"
          placement="bottom-end"
          popper-class="so-detail-header-more-popper"
          @command="onHeaderMoreCommand"
        >
          <button type="button" class="btn-more-actions" title="更多操作" aria-label="更多操作">
            <span class="btn-more-actions__dots" aria-hidden="true">⋯</span>
          </button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item
                v-if="canCancelSalesOrderFromMenu"
                command="cancel_order"
                class="detail-more-item--danger"
              >
                取消订单
              </el-dropdown-item>
              <el-dropdown-item command="delete" class="detail-more-item--danger">删除订单</el-dropdown-item>
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
          <div class="section-dot section-dot--cyan"></div>
          <span class="section-title">基本信息</span>
        </div>
        <div class="info-grid">
          <div class="info-item">
            <span class="info-label">订单号</span>
            <span class="info-value info-value--code">{{ order.sellOrderCode }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">状态</span>
            <span class="info-value">{{ getStatusText(order.status) }}</span>
          </div>
          <div class="info-item" v-if="showCustomerIdentityFields">
            <span class="info-label">客户</span>
            <span class="info-value">{{ order.customerName || '--' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">业务员</span>
            <span class="info-value">{{ maskSaleSensitiveFields ? '—' : order.salesUserName || '--' }}</span>
          </div>
          <div class="info-item" v-if="showSalesMoneyFields">
            <span class="info-label">总金额</span>
            <span class="info-value info-value--amount">{{ formatCurrencyTotal(order.total, order.currency) }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">行项目数</span>
            <span class="info-value">{{ order.itemRows ?? 0 }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">交货日期</span>
            <span class="info-value info-value--time">{{ formatDateTime(order.deliveryDate) }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">创建时间</span>
            <span class="info-value info-value--time">{{ formatDateTime(order.createTime) }}</span>
          </div>
          <div class="info-item info-item--span-3">
            <span class="info-label">标签</span>
            <div class="tags-row">
              <TagListDisplay :tags="currentTags" />
              <button class="btn-add-tag" @click="tagDialogVisible = true">添加标签</button>
            </div>
          </div>
          <div class="info-item info-item--span-3">
            <span class="info-label">送货地址</span>
            <span class="info-value">{{ order.deliveryAddress || '--' }}</span>
          </div>
          <div class="info-item info-item--span-3">
            <span class="info-label">备注</span>
            <span class="info-value">{{ order.comment || '--' }}</span>
          </div>
          <div v-if="order.auditRemark || order.status === -1" class="info-item info-item--span-3">
            <span class="info-label">审核拒绝原因</span>
            <span class="info-value info-value--warn">{{ order.auditRemark || '--' }}</span>
          </div>
        </div>
      </div>

      <!-- TabBar：订单明细 | 文档 -->
      <div class="tabs-section">
        <div class="tabs-nav">
          <button class="tab-btn" :class="{ 'tab-btn--active': activeTab === 'items' }" @click="activeTab = 'items'">订单明细</button>
          <button
            v-if="!maskSaleSensitiveFields"
            class="tab-btn"
            :class="{ 'tab-btn--active': activeTab === 'documents' }"
            @click="activeTab = 'documents'"
          >
            文档
          </button>
        </div>
        <div class="tabs-body">
          <div v-show="activeTab === 'items'" class="detail-items-table-wrap">
            <CrmDataTable :data="order.items" size="small" v-if="order.items?.length" class="items-table">
              <el-table-column type="index" width="50" label="#" />
              <el-table-column
                prop="sellOrderItemCode"
                :label="t('salesOrderItemList.columns.sellOrderItemCode')"
                min-width="168"
                show-overflow-tooltip
              />
              <el-table-column prop="pn" label="物料型号" min-width="160" />
              <el-table-column prop="brand" label="品牌" width="120" />
              <el-table-column label="生产日期" width="108" align="center">
                <template #default="{ row }">
                  {{ fmtSoItemDateCode(row) }}
                </template>
              </el-table-column>
              <el-table-column prop="qty" label="数量" align="right" width="100" />
              <el-table-column v-if="showSalesMoneyFields" prop="price" label="单价" align="right" width="120">
                <template #default="{ row }">
                  {{ formatCurrencyUnitPrice(row.price, row.currency) }}
                </template>
              </el-table-column>
              <el-table-column v-if="showSalesMoneyFields" label="金额" align="right" width="130">
                <template #default="{ row }">
                  {{ formatCurrencyTotal(row.qty * row.price, row.currency) }}
                </template>
              </el-table-column>
              <el-table-column v-if="showSalesMoneyFields" label="折算美金单价" align="right" width="140">
                <template #default="{ row }">
                  {{ row.usdUnitPrice != null ? `$${Number(row.usdUnitPrice).toFixed(6)}` : '—' }}
                </template>
              </el-table-column>
              <el-table-column v-if="showSalesMoneyFields" label="折算美金总额" align="right" width="140">
                <template #default="{ row }">
                  {{ row.usdLineTotal != null ? `$${Number(row.usdLineTotal).toFixed(2)}` : '—' }}
                </template>
              </el-table-column>
              <el-table-column v-if="showSalesMoneyFields" :label="t('salesOrderItemList.columns.salesProfitExpected')" align="right" width="140">
                <template #default="{ row }">
                  {{ row.salesProfitExpected != null ? `$${Number(row.salesProfitExpected).toFixed(2)}` : '—' }}
                </template>
              </el-table-column>
              <el-table-column v-if="showSalesMoneyFields" :label="t('salesOrderItemList.columns.profitOutBizUsd')" align="right" width="120">
                <template #default="{ row }">
                  {{ row.profitOutBizUsd != null ? `$${Number(row.profitOutBizUsd).toFixed(2)}` : '—' }}
                </template>
              </el-table-column>
              <el-table-column v-if="showSalesMoneyFields" :label="t('salesOrderItemList.columns.profitOutRateBiz')" align="right" width="120">
                <template #default="{ row }">
                  {{ row.profitOutRateBiz != null ? Number(row.profitOutRateBiz).toFixed(6) : '—' }}
                </template>
              </el-table-column>
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
              <el-table-column prop="comment" label="备注" min-width="120" />
              <el-table-column label="操作" width="200" fixed="right" class-name="op-col" label-class-name="op-col">
                <template #default="{ row }">
                  <div @click.stop @dblclick.stop>
                    <div
                      v-if="canApplyStockOutForItems && (canPurchaseReq || canWriteSo)"
                      class="action-btns action-btns--detail-items"
                    >
                      <button
                        v-if="canPurchaseReq"
                        type="button"
                        class="action-btn action-btn--warning"
                        @click.stop="handleOpenApplyPurchase(row)"
                      >
                        申请采购
                      </button>
                      <button
                        v-if="canWriteSo && (stockOutApplyPurchaseGateOk(row) || salesOrderLinePurchasedStockReliefOk(row))"
                        type="button"
                        class="action-btn action-btn--warning"
                        :disabled="salesOrderLineApplyStockOutButtonDisabled(row)"
                        @click.stop="handleOpenApplyStockOut(row)"
                      >
                        申请出库
                      </button>
                    </div>
                  </div>
                </template>
              </el-table-column>
            </CrmDataTable>
            <el-empty v-else description="暂无明细" :image-size="80" />
          </div>
          <div v-show="activeTab === 'documents' && !maskSaleSensitiveFields" class="doc-tab-content">
            <DocumentUploadPanel
              biz-type="SALES_ORDER"
              :biz-id="String(order.id)"
              :max-files="20"
              :max-size-mb="100"
              @uploaded="docListRef?.refresh()"
            />
            <DocumentListPanel
              ref="docListRef"
              biz-type="SALES_ORDER"
              :biz-id="String(order.id)"
              view-mode="list"
              style="margin-top: 16px;"
            />
          </div>
        </div>
      </div>
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

    <el-dialog v-model="applyDialogVisible" title="新建出货通知" width="960px" destroy-on-close>
      <div v-loading="applyStockOutLoading">
        <el-alert
          v-if="applyForm.sellOrderItemId && applyStockOutZeroQtyBannerVisible"
          type="warning"
          :closable="false"
          show-icon
          class="apply-so-stock-alert"
          :title="t('salesOrderItemList.applyStockOutDialog.zeroApplyBanner')"
        />
        <el-alert
          v-if="applyForm.sellOrderItemId && !applyStockOutLoading"
          type="info"
          :closable="false"
          show-icon
          class="apply-so-stock-purchasing-info"
          :class="{ 'apply-so-stock-purchasing-info--has-stock': applyPurchasedStockingPurchasingHasQty }"
          :title="applyPurchasedStockingPurchasingBarTitle"
        />
      <el-form :model="applyForm" label-width="140px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="通知单号">
              <el-input :model-value="applyForm.requestCode || '（提交时由系统生成）'" readonly />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="预计出货日期" required>
              <el-date-picker
                v-model="applyForm.requestDate"
                type="datetime"
                placeholder="选择日期与时间"
                format="YYYY-MM-DD HH:mm"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="客户">
              <el-input :model-value="maskSaleSensitiveFields ? '—' : order?.customerName || '--'" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="销售单号">
              <el-input :model-value="order?.sellOrderCode || '--'" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="出货方式">
              <el-select
                v-model="applyForm.shipmentMethod"
                clearable
                filterable
                placeholder="请选择"
                style="width: 100%"
              >
                <el-option
                  v-for="o in shipmentMethodOptions"
                  :key="o.value"
                  :label="o.label"
                  :value="o.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('stockOutNotifyList.applyDialog.regionType')">
              <el-select
                :model-value="normalizeRegionType(applyForm.regionType)"
                :teleported="false"
                style="width: 100%"
                @update:model-value="(v: string | number) => { applyForm.regionType = normalizeRegionType(v) }"
              >
                <el-option :value="REGION_TYPE_DOMESTIC" :label="t('inventoryList.warehouse.regionDomestic')" />
                <el-option :value="REGION_TYPE_OVERSEAS" :label="t('inventoryList.warehouse.regionOverseas')" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="备注">
              <el-input v-model="applyForm.remark" type="textarea" :rows="2" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <!-- 单条销售订单明细 → 一条出库通知（单表） -->
      <div v-if="applyForm.sellOrderItemId" class="apply-stock-lines items-table">
        <div class="apply-stock-lines__head">
          <span class="cell cell--idx">#</span>
          <span class="cell cell--pn">物料型号</span>
          <span class="cell cell--brand">品牌</span>
          <span class="cell cell--num">订单数量</span>
          <span class="cell cell--num">已占用通知</span>
          <span class="cell cell--num">尚可申请</span>
          <span class="cell cell--num">在库可用</span>
          <span class="cell cell--num">{{ t('salesOrderItemList.applyStockOutDialog.stockingOnHand') }}</span>
          <span class="cell cell--qty">本次数量</span>
        </div>
        <div class="apply-stock-lines__row">
          <span class="cell cell--idx">1</span>
          <span class="cell cell--pn">{{ applyForm.materialCode }}</span>
          <span class="cell cell--brand">{{ applyForm.materialName }}</span>
          <span class="cell cell--num">{{ applyFormSalesOrderQtyText }}</span>
          <span class="cell cell--num">{{ applyFormAlreadyNotifiedText }}</span>
          <span class="cell cell--num">{{ applyFormRemainingNotifyText }}</span>
          <span class="cell cell--num">{{ applyStockQtyText }}</span>
          <span class="cell cell--num">{{ applyPurchasedStockQtyText }}</span>
          <span class="cell cell--qty">
            <el-input-number
              v-model="applyForm.notifyQty"
              :min="0"
              :max="applyForm.maxQty"
              :precision="0"
              controls-position="right"
              style="width: 140px"
            />
          </span>
        </div>
      </div>
      <el-empty v-else description="请从上方明细行点击「申请出库」" :image-size="64" />
      </div>
      <template #footer>
        <el-button @click="applyDialogVisible = false">取消</el-button>
        <el-button
          type="primary"
          :loading="applySubmitting"
          :disabled="applyStockOutConfirmDisabled"
          @click="submitApplyStockOut"
        >
          确定
        </el-button>
      </template>
    </el-dialog>

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

    <el-dialog v-model="editDialogVisible" title="编辑销售订单" width="560px" destroy-on-close @closed="onEditDialogClosed">
      <el-form v-if="order" label-width="100px">
        <el-form-item v-if="showCustomerIdentityFields" label="客户名称">
          <el-input v-model="editForm.customerName" />
        </el-form-item>
        <el-form-item label="业务员">
          <el-input v-if="maskSaleSensitiveFields" model-value="—" disabled />
          <SalesUserCascader
            v-else
            v-model="editForm.salesUserId"
            placeholder="请选择业务员"
            @change="onEditSalesUserChange"
          />
        </el-form-item>
        <el-form-item label="订单类型">
          <el-select v-model="editForm.type" style="width: 100%">
            <el-option :label="t('salesOrderCreate.orderTypes.normal')" :value="1" />
            <el-option :label="t('salesOrderCreate.orderTypes.urgent')" :value="2" />
            <el-option :label="t('salesOrderCreate.orderTypes.sample')" :value="3" />
          </el-select>
        </el-form-item>
        <el-form-item label="币别">
          <el-select v-model="editForm.currency" style="width: 100%">
            <el-option
              v-for="opt in SETTLEMENT_CURRENCY_OPTIONS"
              :key="opt.value"
              :label="opt.label"
              :value="opt.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="交货日期">
          <el-date-picker v-model="editForm.deliveryDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
        </el-form-item>
        <el-form-item label="送货地址">
          <el-input v-model="editForm.deliveryAddress" type="textarea" :rows="2" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="editForm.comment" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="editSaving" @click="saveEdit">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import salesOrderApi from '@/api/salesOrder'
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
import {
  SALES_ORDER_FAVORITE_ENTITY_TYPE,
  SALES_ORDER_FAVORITES_CHANGED_EVENT
} from '@/constants/salesOrderFavorites'
import { recordSalesOrderRecentView } from '@/utils/salesOrderRecentHistory'
import { stockOutApi } from '@/api/stockOut'
import { tagApi, type TagDefinitionDto } from '@/api/tag'
import { useAuthStore } from '@/stores/auth'
import TagListDisplay from '@/components/Tag/TagListDisplay.vue'
import ApplyTagsDialog from '@/components/Tag/ApplyTagsDialog.vue'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatCurrencyTotal, formatCurrencyUnitPrice } from '@/utils/moneyFormat'
import SalesUserCascader from '@/components/SalesUserCascader.vue'
import { SETTLEMENT_CURRENCY_OPTIONS } from '@/constants/currency'
import { productionDateDisplayLabel, useMaterialProductionDateDict } from '@/composables/useMaterialProductionDateDict'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { REGION_TYPE_DOMESTIC, REGION_TYPE_OVERSEAS, normalizeRegionType } from '@/constants/regionType'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const authStore = useAuthStore()
const { options: materialPdOptions, ensureLoaded: ensureMaterialPdDict } = useMaterialProductionDateDict()
const { ensureLoaded: ensureLogisticsDict, arrivalOptions: shipmentMethodOptions } = useLogisticsFormDict()

function fmtSoItemDateCode(row: { dateCode?: string; DateCode?: string } | null | undefined) {
  if (!row) return '—'
  const raw = String(row.dateCode ?? row.DateCode ?? '').trim()
  if (!raw) return '—'
  return productionDateDisplayLabel(raw, materialPdOptions.value) || raw
}

const canViewCustomerInfo = computed(() => authStore.hasPermission('customer.info.read'))
const canViewSalesAmount = computed(() => authStore.hasPermission('sales.amount.read'))
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const showCustomerIdentityFields = computed(() => canViewCustomerInfo.value && !maskSaleSensitiveFields.value)
const showSalesMoneyFields = computed(() => canViewSalesAmount.value && !maskSaleSensitiveFields.value)
const canWriteSo = computed(() => authStore.hasPermission('sales-order.write'))

/** 与采购订单列表原「取消订单」一致：审核通过(10)前可取消主单为 -2；已取消不可再取消 */
const canCancelSalesOrderFromMenu = computed(() => {
  const o = order.value
  if (!o || !canWriteSo.value) return false
  const s = Number(o.status)
  if (!Number.isFinite(s) || s === -2) return false
  return s < 10
})
const canPurchaseReq = computed(() => authStore.hasPermission('purchase-requisition.write'))

/** 主表审核通过后，明细才允许申请出库 */
const canApplyStockOutForItems = computed(() =>
  order.value != null && salesOrderMainAllowsPurchaseAndStockOut(Number(order.value.status))
)

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
const order = ref<any>(null)
/** 加载失败时展示具体原因（权限/网络/库表等），避免一律显示「订单不存在」 */
const loadError = ref('')
const soFavorited = ref(false)
const favoriteLoading = ref(false)
const activeTab = ref('items')
const docListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)

watch(maskSaleSensitiveFields, (m) => {
  if (m && activeTab.value === 'documents') activeTab.value = 'items'
})

// 标签
const currentTags = ref<TagDefinitionDto[]>([])
const tagDialogVisible = ref(false)

const editDialogVisible = ref(false)
const editSaving = ref(false)
const applyDialogVisible = ref(false)
const applySubmitting = ref(false)
const applyStockOutLoading = ref(false)
const applyForm = ref({
  requestCode: '',
  requestDate: null as Date | null,
  /** 数据字典 LogisticsArrivalMethod（与物流「来货方式」同源） */
  shipmentMethod: '' as string,
  /** RegionType：10=境内 20=境外 */
  regionType: REGION_TYPE_DOMESTIC as number,
  remark: '',
  sellOrderItemId: '',
  materialCode: '',
  materialName: '',
  /** 销售明细数量（服务端 apply-context） */
  salesOrderQty: 0,
  alreadyNotifiedQty: 0,
  remainingNotifyQty: 0,
  /** 本次输入上限 = min(尚可申请, 客单在库+备货在库)，由服务端计算 */
  maxQty: 0,
  stockAvailableQty: 0,
  /** 同 PN+品牌备货在库可用（服务端 apply-context） */
  purchasedStockAvailableQty: 0,
  notifyQty: 0
})

const intQtyText = (v: unknown) => {
  const n = Math.trunc(Number(v) || 0)
  return String(Number.isFinite(n) ? n : 0)
}

/** 在库可用：来自出库申请上下文接口 */
const applyStockQtyText = computed(() => intQtyText(applyForm.value.stockAvailableQty ?? 0))
/** 备货在库：与销售扩展 PurchasedStock_AvailableQty 一致 */
const applyPurchasedStockQtyText = computed(() => intQtyText(applyForm.value.purchasedStockAvailableQty ?? 0))
/** 无可申请数量且备货也为 0 时在弹窗内展示横幅（不再使用页面级 Message 重复提示） */
const applyStockOutZeroQtyBannerVisible = computed(() => {
  const maxQ = Math.max(0, Math.trunc(Number(applyForm.value.maxQty) || 0))
  const stocking = Math.max(0, Math.trunc(Number(applyForm.value.purchasedStockAvailableQty) || 0))
  return maxQ <= 0 && stocking <= 0
})
/** 与零可申请横幅一致：该提示出现时不可提交（加载中亦禁用，避免初始 maxQty=0 误判） */
const applyStockOutConfirmDisabled = computed(
  () =>
    applySubmitting.value ||
    applyStockOutLoading.value ||
    (!!applyForm.value.sellOrderItemId && applyStockOutZeroQtyBannerVisible.value)
)
/** 表单上方：同物料型号的采购备货在库数量说明 */
const applyPurchasedStockingPurchasingBarTitle = computed(() => {
  const qty = Math.max(0, Math.trunc(Number(applyForm.value.purchasedStockAvailableQty) || 0))
  if (qty > 0)
    return t('salesOrderItemList.applyStockOutDialog.stockingPurchasingSummary', { qty })
  return t('salesOrderItemList.applyStockOutDialog.stockingPurchasingNone')
})
/** 有备货在库数量时，备货说明栏标题用成功色 */
const applyPurchasedStockingPurchasingHasQty = computed(
  () => Math.max(0, Math.trunc(Number(applyForm.value.purchasedStockAvailableQty) || 0)) > 0
)
const applyFormSalesOrderQtyText = computed(() => intQtyText(applyForm.value.salesOrderQty))
const applyFormAlreadyNotifiedText = computed(() => intQtyText(applyForm.value.alreadyNotifiedQty))
const applyFormRemainingNotifyText = computed(() => intQtyText(applyForm.value.remainingNotifyQty))
const editForm = ref({
  customerName: '',
  salesUserId: '',
  salesUserName: '',
  type: 1,
  currency: 1,
  deliveryDate: '' as string | undefined,
  deliveryAddress: '',
  comment: ''
})

function onEditSalesUserChange(p: { id: string; label: string }) {
  editForm.value.salesUserName = p.label || ''
}

const orderId = computed(() => {
  const raw = route.params.id
  if (Array.isArray(raw)) return String(raw[0] ?? '').trim()
  return String(raw ?? '').trim()
})

/** CaptionBar 主标题：有客户读权限且有名称为客户名，否则为销售单号 */
const captionTitle = computed(() => {
  const o = order.value
  if (!o) return '销售订单'
  if (showCustomerIdentityFields.value && o.customerName?.trim()) return String(o.customerName).trim()
  return o.sellOrderCode || '销售订单'
})

const captionMetaVisible = computed(() => {
  const o = order.value
  if (!o?.sellOrderCode) return false
  if (showCustomerIdentityFields.value && o.customerName?.trim()) return true
  return false
})

const captionAvatarChar = computed(() => {
  const t = captionTitle.value
  return (t && t[0]) || '销'
})

function onHeaderMoreCommand(cmd: string) {
  if (cmd === 'cancel_order') void handleCancelSalesOrder()
  else if (cmd === 'delete') void handleDeleteOrder()
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

watch(
  () => [route.query.edit, order.value?.id] as const,
  () => {
    if (route.query.edit === '1' && order.value && canWriteSo.value) {
      openEditDialog()
    }
  }
)

watch(
  () => [route.query.applyStockOut, order.value?.id] as const,
  () => {
    if (route.query.applyStockOut === '1' && order.value && canWriteSo.value) {
      if (!salesOrderMainAllowsPurchaseAndStockOut(Number(order.value.status))) {
        ElMessage.warning('销售订单主表审核通过后，方可申请出库')
        router.replace({ path: route.path, query: { ...route.query, applyStockOut: undefined } })
        return
      }
      const lines = order.value.items || []
      if (
        lines.length === 1 &&
        !stockOutApplyPurchaseGateOk(lines[0]) &&
        !salesOrderLinePurchasedStockReliefOk(lines[0])
      ) {
        ElMessage.warning(t('salesOrderItemList.messages.applyStockOutNeedPurchaseGate'))
        router.replace({ path: route.path, query: { ...route.query, applyStockOut: undefined } })
        return
      }
      if (
        lines.length === 1 &&
        salesOrderLineApplyStockOutDisabled(lines[0]) &&
        !salesOrderLinePurchasedStockReliefOk(lines[0])
      ) {
        ElMessage.warning(t('salesOrderItemList.messages.applyStockOutDisabledByProgress'))
        router.replace({ path: route.path, query: { ...route.query, applyStockOut: undefined } })
        return
      }
      handleOpenApplyStockOut()
      router.replace({ path: route.path, query: { ...route.query, applyStockOut: undefined } })
    }
  }
)

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
  try {
    const id = orderId.value
    if (!id) {
      order.value = null
      loadError.value = '链接中缺少订单编号'
      soFavorited.value = false
      return
    }
    order.value = await salesOrderApi.getById(id)
    if (order.value) {
      loadError.value = ''
      refreshTags()
      recordSalesOrderRecentView({
        id: String(order.value.id),
        sellOrderCode: order.value.sellOrderCode,
        customerName: order.value.customerName
      })
      await loadFavoriteState()
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
  }
}

function openEditDialog() {
  if (!order.value) return
  const o = order.value
  editForm.value = {
    customerName: o.customerName || '',
    salesUserId: o.salesUserId || '',
    salesUserName: o.salesUserName || '',
    type: o.type ?? 1,
    currency: o.currency ?? 1,
    deliveryDate: o.deliveryDate ? String(o.deliveryDate).slice(0, 10) : undefined,
    deliveryAddress: o.deliveryAddress || '',
    comment: o.comment || ''
  }
  editDialogVisible.value = true
}

function onEditDialogClosed() {
  if (route.query.edit === '1') {
    router.replace({ path: route.path, query: {} })
  }
}

const saveEdit = async () => {
  if (!order.value) return
  editSaving.value = true
  try {
    await salesOrderApi.update(order.value.id, {
      customerName: editForm.value.customerName || undefined,
      salesUserId: editForm.value.salesUserId || undefined,
      salesUserName: editForm.value.salesUserName || undefined,
      type: editForm.value.type,
      currency: editForm.value.currency,
      deliveryDate: editForm.value.deliveryDate || undefined,
      deliveryAddress: editForm.value.deliveryAddress || undefined,
      comment: editForm.value.comment || undefined
    })
    ElMessage.success('已保存')
    editDialogVisible.value = false
    await fetchOrder()
  } catch (e: any) {
    ElMessage.error(e?.message || '保存失败')
  } finally {
    editSaving.value = false
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
  const map: Record<number, string> = { 0: '待采购', 1: '部分采购', 2: '采购完成' }
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
const getReceiptProgressText = (v?: number) => {
  const map: Record<number, string> = { 0: '待收款', 1: '部分收款', 2: '收款完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}
const getSellInvoiceProgressText = (v?: number) => {
  const map: Record<number, string> = { 0: '待开票', 1: '部分开票', 2: '开票完成' }
  return v !== undefined && v !== null ? (map[v] ?? '--') : '--'
}

const formatDateTime = (v?: string) => (v ? formatDisplayDateTime(v) : '--')

const handleEdit = () => {
  if (!canWriteSo.value) {
    ElMessage.warning('无编辑权限')
    return
  }
  openEditDialog()
}

const handleOpenApplyStockOut = async (item?: any) => {
  if (!order.value) return
  if (!salesOrderMainAllowsPurchaseAndStockOut(Number(order.value.status))) {
    ElMessage.warning('销售订单主表审核通过后，方可申请出库')
    return
  }
  const list = order.value.items || []
  let line: any
  if (item) {
    line = item
  } else {
    if (list.length !== 1) {
      ElMessage.warning('请从订单明细行点击「申请出库」，每次仅针对一条明细生成出库通知')
      return
    }
    line = list[0]
  }
  if (!stockOutApplyPurchaseGateOk(line) && !salesOrderLinePurchasedStockReliefOk(line)) {
    ElMessage.warning(t('salesOrderItemList.messages.applyStockOutNeedPurchaseGate'))
    return
  }
  if (salesOrderLineApplyStockOutDisabled(line) && !salesOrderLinePurchasedStockReliefOk(line)) {
    ElMessage.warning(t('salesOrderItemList.messages.applyStockOutDisabledByProgress'))
    return
  }
  const sellOrderItemId = String(line.id ?? line.Id ?? '').trim()
  if (!sellOrderItemId) {
    ElMessage.error('销售订单明细缺少主键，无法申请出库')
    return
  }
  applyDialogVisible.value = true
  applyStockOutLoading.value = true
  try {
    await ensureLogisticsDict()
    const ctx = await stockOutApi.getApplyContext(order.value.id, sellOrderItemId)
    const maxQ = Math.max(0, Math.trunc(Number(ctx.suggestedMaxQty) || 0))
    const stocking = Math.max(0, Math.trunc(Number(ctx.purchasedStockAvailableQty ?? 0) || 0))
    applyForm.value = {
      requestCode: '',
      requestDate: new Date(),
      shipmentMethod: '',
      regionType: REGION_TYPE_DOMESTIC,
      remark: '',
      sellOrderItemId,
      materialCode: String(line.pn ?? line.PN ?? '').trim(),
      materialName: String(line.brand ?? line.Brand ?? '').trim(),
      salesOrderQty: Number(ctx.salesOrderQty ?? 0),
      alreadyNotifiedQty: Number(ctx.alreadyNotifiedQty ?? 0),
      remainingNotifyQty: Number(ctx.remainingNotifyQty ?? 0),
      maxQty: maxQ,
      stockAvailableQty: Number(ctx.availableStockQty ?? 0),
      purchasedStockAvailableQty: stocking,
      notifyQty: maxQ
    }
  } catch (e: any) {
    ElMessage.error(e?.message || '加载出库申请数据失败')
    applyDialogVisible.value = false
  } finally {
    applyStockOutLoading.value = false
  }
}

const submitApplyStockOut = async () => {
  if (!order.value) return
  const rd = applyForm.value.requestDate
  if (!rd || !(rd instanceof Date) || Number.isNaN(rd.getTime())) {
    ElMessage.warning('请选择预计出货日期与时间')
    return
  }
  if (!applyForm.value.sellOrderItemId) {
    ElMessage.warning('请选择一条销售订单明细后再申请出库')
    return
  }
  const qty = Number(applyForm.value.notifyQty)
  if (!(qty > 0)) {
    ElMessage.warning('出库通知数量必须大于 0')
    return
  }
  /** 与后端 GetApplyContext：suggestedMaxQty = min(尚可申请, 客单在库+备货在库) 及 el-input-number :max 一致 */
  const maxAllowed = Math.max(0, Math.trunc(Number(applyForm.value.maxQty) || 0))
  if (qty > maxAllowed) {
    ElMessage.warning(t('salesOrderItemList.messages.applyStockOutExceedsSuggestedMax', { max: maxAllowed }))
    return
  }
  applySubmitting.value = true
  try {
    await stockOutApi.createRequest({
      requestCode: applyForm.value.requestCode?.trim() || undefined,
      salesOrderId: order.value.id,
      salesOrderItemId: applyForm.value.sellOrderItemId,
      materialCode: applyForm.value.materialCode,
      materialName: applyForm.value.materialName,
      quantity: qty,
      customerId: order.value.customerId || '',
      requestUserId: (authStore.user as any)?.id || '',
      requestDate: rd.toISOString(),
      remark: applyForm.value.remark || undefined,
      shipmentMethod: applyForm.value.shipmentMethod?.trim() || undefined,
      regionType: normalizeRegionType(applyForm.value.regionType)
    })
    applyDialogVisible.value = false
    ElMessage.success('申请出库成功')
    router.push('/stock-out-notifies')
  } catch (e: any) {
    ElMessage.error(e?.message || '申请出库失败')
  } finally {
    applySubmitting.value = false
  }
}

</script>

<style lang="scss" scoped>
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
  margin-top: 0;
}

.caption-code {
  font-family: 'Space Mono', monospace;
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
  color: rgba(200, 220, 240, 0.5);
  cursor: pointer;
  transition: color 0.15s, background 0.15s, transform 0.12s;

  .star-icon {
    width: 20px;
    height: 20px;
    display: block;
  }

  &:hover:not(:disabled) {
    color: #00d4ff;
    background: rgba(0, 212, 255, 0.1);
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
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid $border-panel;
  color: $text-secondary;
  font-size: 13px;
  background: rgba(255,255,255,0.05);
  cursor: pointer;
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
  gap: 10px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255,255,255,0.05);
  background: rgba(0,0,0,0.1);
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
  font-family: 'Space Mono', monospace;
  font-size: 11px;
  color: $text-muted;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 16px 20px;
  border-bottom: 1px solid rgba(255,255,255,0.04);
  border-right: 1px solid rgba(255,255,255,0.04);
  &:nth-child(3n) { border-right: none; }
}

.info-item--span-3 {
  grid-column: 1 / span 3;
  border-right: none;
}

.info-label {
  font-size: 11px;
  color: $text-muted;
  letter-spacing: 0.5px;
  text-transform: uppercase;
}

.info-value {
  font-size: 13px;
  color: $text-secondary;
}

.info-value--code {
  font-family: 'Space Mono', monospace;
  color: $color-ice-blue;
}

.info-value--amount {
  font-family: 'Space Mono', monospace;
  color: $text-primary;
  font-weight: 500;
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
  padding: 0 20px 20px;
}

.tabs-nav {
  display: flex;
  border-bottom: 1px solid rgba(255,255,255,0.06);
  padding: 0 16px;
  background: rgba(0,0,0,0.1);
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
}

.tab-btn--active {
  color: $cyan-primary;
  border-bottom-color: $cyan-primary;
}

.tabs-body {
  padding: 20px;
}

.detail-items-table-wrap {
  margin-top: 4px;
}

.items-table {
  // 与全站 .crm-items-table 一致：文字色跟随 html[data-theme]（勿写死浅色字，否则浅色主题下看不清）
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
    // 与 td/.cell 双保险，避免 EP 默认色在浅色卡片上偏灰、难辨认
    --el-table-text-color: #{$text-primary};
    color: $text-primary;
  }
  :deep(.el-table__inner-wrapper) {
    background: transparent;
    &::before { display: none !important; }
    &::after  { display: none !important; }
  }
  :deep(.el-table__border-left-patch) { display: none !important; }
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
  // 正文实际文字多在 .cell 内，须单独拉高对比度（否则仅 td 上色仍可能被 EP 覆盖）
  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row td.el-table__cell .cell),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row td.el-table__cell .cell) {
    color: $text-primary !important;
  }
  :deep(.el-table__cell) {
    .el-button { white-space: nowrap !important; }
    .cell { white-space: nowrap; }
  }
}

.apply-so-stock-purchasing-info {
  margin-bottom: 10px;
}

.apply-so-stock-purchasing-info--has-stock {
  /* 覆盖 el-alert--info 默认色（含 Teleport 出 scoped 根节点时） */
  :deep(.el-alert__content),
  :deep(.el-alert__title),
  :deep(.el-alert__icon) {
    color: $success-color !important;
  }
}

.apply-so-stock-alert {
  margin-bottom: 12px;
}

.apply-stock-lines {
  margin-top: 8px;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  /* 窄屏时横向滚动，避免「备货在库」等列被 overflow:hidden 裁掉 */
  overflow-x: auto;
  overflow-y: visible;
  background: $layer-2;
}
.apply-stock-lines__head,
.apply-stock-lines__row {
  display: grid;
  grid-template-columns: 44px minmax(96px, 1fr) 84px 66px 66px 66px 66px 66px 132px;
  gap: 8px;
  align-items: center;
  padding: 10px 12px;
  min-width: 760px;
}
.apply-stock-lines__head {
  background: var(--crm-table-header-bg);
  font-size: 12px;
  color: var(--crm-table-header-text);
  font-weight: 600;
  border-bottom: 1px solid var(--crm-table-header-line);
  .cell {
    color: inherit;
  }
}
.apply-stock-lines__row {
  border-bottom: 1px solid $border-panel;
  font-size: 13px;
  color: $text-primary;
  .cell {
    color: inherit;
  }
  &:last-child {
    border-bottom: none;
  }
}
.apply-stock-lines .cell--num {
  text-align: right;
  font-variant-numeric: tabular-nums;
}

.doc-tab-content {
  padding-top: 4px;
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

/* 申请出库弹窗 Teleport 到 body 时，scoped 可能无法作用到 el-alert 内层，此处用全局唯一类兜底 */
.apply-so-stock-purchasing-info--has-stock.el-alert .el-alert__content,
.apply-so-stock-purchasing-info--has-stock.el-alert .el-alert__title,
.apply-so-stock-purchasing-info--has-stock.el-alert .el-alert__icon {
  color: $success-color !important;
}
</style>
