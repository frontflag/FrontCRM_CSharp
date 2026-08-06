<template>
  <div class="quote-upsert-page" :class="{ 'quote-upsert-page--embedded': embedded }">
    <div class="page-header">
      <div class="header-left">
        <button v-if="!embedded" class="btn-back" type="button" @click="handleBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('quoteUpsert.back') }}
        </button>
        <div class="quote-caption-title-group">
          <div class="caption-avatar-lg">{{ captionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1
                  class="page-title"
                  :class="{ 'page-title--muted': isEditMode && quoteStatus === 2 }"
                >
                  <template v-if="isEditMode && formData.quoteCode">
                    {{ t('quoteDetail.captionPrefix') }} {{ formData.quoteCode }}
                  </template>
                  <template v-else>{{ t('quoteUpsert.createTitle') }}</template>
                </h1>
              </div>
            </div>
            <div
              v-if="isEditMode && quoteStatus != null"
              class="title-meta title-meta--caption quote-header-meta-row"
            >
              <el-tag effect="dark" :type="quoteMainStatusTagType(quoteStatus)" size="small">
                {{ t(quoteMainStatusI18nKey(quoteStatus)) }}
              </el-tag>
              <span v-if="isQuoteReadOnly(quoteStatus)" class="quote-caption-meta-text">
                {{ t('quoteUpsert.readOnlyHint') }}
              </span>
            </div>
            <div v-else-if="!isEditMode" class="title-meta title-meta--caption quote-header-meta-row">
              <el-tag effect="dark" type="primary" size="small">
                {{ t('quoteList.status.new') }}
              </el-tag>
            </div>
          </div>
        </div>
      </div>
      <div class="header-right">
        <button
          v-if="!embedded && !isEditMode"
          type="button"
          class="btn-quote-desktop"
          @click="openQuoteDesktop"
        >
          <span>{{ t('quoteDesktop.openFromCreate') }}</span>
          <el-icon class="btn-quote-desktop__arrow"><ArrowRight /></el-icon>
        </button>
        <el-button
          v-if="embedded && canMarkNoQuote"
          :loading="markNoQuoteLoading"
          @click="handleMarkNoQuote"
        >
          {{ t('quoteDesktop.actions.markNoQuote') }}
        </el-button>
        <el-button v-if="!embedded" @click="handleBack">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          <el-icon><Check /></el-icon> 保存
        </el-button>
      </div>
    </div>

    <div
      class="quote-upsert-content"
      v-loading="pageLoading"
      element-loading-background="rgba(10,22,40,0.8)"
    >
      <el-alert
        v-if="hasRfqLinkAlert"
        type="info"
        :closable="false"
        show-icon
        class="link-alert"
      >
        <template #title>
          <div class="link-alert-title-row">
            <span class="la-block-rfq">
              <span class="la-muted">报价需求</span><span class="la-pre">{{ linkAlertGap2 }}</span><span class="la-strong la-rfq-val">{{ linkAlertRfqDisplay }}</span>
              <template v-if="linkAlertBatchCount > 0">
                <span class="la-pre">{{ linkAlertSep2 }}</span>
                <span class="la-muted">明细 {{ linkAlertBatchCount }} 条（批量报价）</span>
              </template>
            </span>
            <span class="la-pre">{{ linkAlertSep8Ideo }}</span>
            <span class="la-block-detail"><span class="la-muted">物料号</span><span class="la-pre">{{ linkAlertGap2 }}</span><span class="la-value-brown">{{ formData.mpn || '—' }}</span><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">品牌</span><span class="la-pre">{{ linkAlertGap2 }}</span><span class="la-value-brown">{{ formData.brand || '—' }}</span><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">数量</span><span class="la-pre">{{ linkAlertGap2 }}</span><span class="la-value-brown">{{ formatNumber(formData.quantity) }}</span><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">目标价</span><span class="la-pre">{{ linkAlertGap2 }}</span><span class="la-value-brown">{{ targetPriceText }}</span></span>
          </div>
        </template>
      </el-alert>

      <!-- 基本信息（§4 info-section，参考销售订单详情） -->
      <div class="info-section basic-info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('quoteDetail.basicInfo') }}</span>
          </div>
          <div class="section-header__meta">
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">{{ t('quoteDetail.createDate') }}</span>
              <span class="section-header-meta-item__value">{{ quoteBasicCreateDateText }}</span>
            </span>
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">{{ t('quoteDetail.createUser') }}</span>
              <span class="section-header-meta-item__value">{{ quoteBasicCreateUserText }}</span>
            </span>
          </div>
        </div>
        <div class="basic-info-section__body">
          <el-form ref="formRef" :model="formData" :rules="formRules" label-width="128px" class="upsert-form">
        <!-- 第一行：供应商 · 联系人 · 失效日期 -->
        <el-row :gutter="12" class="quote-triple-row">
          <template v-if="!maskPurchaseSensitiveFields">
            <el-col :span="8">
              <el-form-item label="供应商" prop="vendorId">
                <el-select
                  v-model="formData.vendorId"
                  class="q-select"
                  placeholder="请选择供应商"
                  style="width: 100%"
                  filterable
                  clearable
                  :filter-method="onVendorFilterInput"
                  :loading="vendorSearchLoading"
                  loading-text="搜索中..."
                  @change="onVendorChange"
                >
                  <template #empty>
                    <div class="vendor-search-hint">
                      <span>请输入内容之后选择</span>
                    </div>
                  </template>
                  <el-option v-for="v in vendorOptions" :key="v.value" :label="v.label" :value="v.value" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="联系人" prop="vendorContactId">
                <el-select
                  v-model="formData.vendorContactId"
                  class="q-select"
                  placeholder="请先选择供应商"
                  style="width: 100%"
                  filterable
                  clearable
                  :disabled="!formData.vendorId"
                  :loading="contactLoading"
                  @change="onContactChange"
                >
                  <el-option v-for="c in contactOptions" :key="c.value" :label="c.label" :value="c.value" />
                </el-select>
              </el-form-item>
            </el-col>
          </template>
          <template v-else>
            <el-col :span="8">
              <el-form-item label="供应商">
                <el-input model-value="—" disabled placeholder="—" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="联系人">
                <el-input model-value="—" disabled placeholder="—" />
              </el-form-item>
            </el-col>
          </template>
          <el-col :span="8">
            <el-form-item label="失效日期" prop="expiryDate">
              <el-date-picker
                v-model="formData.expiryDate"
                type="date"
                placeholder="请选择失效日期"
                value-format="YYYY-MM-DD"
                style="width: 100%"
                class="q-date"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 第二行：物料型号 · 品牌 · 品牌属地 -->
        <el-row :gutter="12" class="quote-triple-row">
          <el-col :span="8">
            <el-form-item label="物料型号" prop="mpn">
              <el-input
                v-model="formData.mpn"
                :placeholder="rfqDetailLocked ? '来自需求明细' : '请输入MPN'"
                :disabled="rfqDetailLocked"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="品牌" prop="brand">
              <el-input
                v-model="formData.brand"
                :placeholder="rfqDetailLocked ? '来自需求明细' : '请输入品牌'"
                :disabled="rfqDetailLocked"
                clearable
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="品牌属地">
              <el-input v-model="formData.brandOrigin" placeholder="如：韩国" disabled />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 第三行：价格类型 · 生产日期 · 交期 -->
        <el-row :gutter="12" class="quote-triple-row">
          <el-col :span="8">
            <el-form-item label="价格类型" prop="priceType">
              <el-select v-model="formData.priceType" placeholder="请选择价格类型" class="q-select" style="width: 100%">
                <el-option label="现货价" value="现货价" />
                <el-option label="期货价" value="期货价" />
                <el-option label="样品价" value="样品价" />
                <el-option label="排单价" value="排单价" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="生产日期" prop="productionDate">
              <MaterialProductionDateSelect v-model="formData.productionDate" placeholder="请选择生产日期" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="交期">
              <el-input v-model="formData.leadTime" placeholder="请输入交期" clearable />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 第四行：最小包装 · 起订量 · 库存 -->
        <el-row :gutter="12" class="quote-triple-row">
          <el-col :span="8">
            <el-form-item label="最小包装">
              <el-input-number v-model="formData.minPackageQty" :min="0" :controls="false" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="起订量">
              <el-input-number v-model="formData.moq" :min="0" :controls="false" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="库存">
              <el-input-number v-model="formData.stockQty" :min="0" :controls="false" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 第五行：涂标 · 报价晶圆产地 · 报价封装产地 · 包邮 -->
        <el-row :gutter="12" class="quote-quad-row">
          <el-col :span="6">
            <el-form-item label="涂标">
              <el-radio-group v-model="formData.labelType" class="seg-group">
                <el-radio-button :label="0">不涂标</el-radio-button>
                <el-radio-button :label="1">涂标</el-radio-button>
                <el-radio-button :label="2">待确定</el-radio-button>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="报价晶圆产地" prop="waferOrigin">
              <el-radio-group v-model="formData.waferOrigin" class="seg-group">
                <el-radio-button :label="0">美产</el-radio-button>
                <el-radio-button :label="1">非美产</el-radio-button>
                <el-radio-button :label="2">待确定</el-radio-button>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="报价封装产地" prop="packageOrigin">
              <el-radio-group v-model="formData.packageOrigin" class="seg-group">
                <el-radio-button :label="0">美产</el-radio-button>
                <el-radio-button :label="1">非美产</el-radio-button>
                <el-radio-button :label="2">待确定</el-radio-button>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="包邮">
              <el-switch v-model="formData.freeShipping" />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 第六行：业务员 · 采购员 -->
        <el-row :gutter="12" class="quote-triple-row">
          <el-col :span="8">
            <el-form-item label="业务员" prop="salesUserId">
              <template v-if="maskSaleSensitiveFields">
                <el-input model-value="—" disabled />
              </template>
              <SalesUserCascader
                v-else
                v-model="formData.salesUserId"
                placeholder="请选择业务员"
                @change="onSalesUserChange"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="采购员" prop="purchaseUserId">
              <el-select
                v-model="formData.purchaseUserId"
                placeholder="请选择采购员"
                filterable
                clearable
                style="width: 100%"
                :loading="purchaseUserOptionsLoading"
                @change="onPurchaseUserSelectChange"
              >
                <el-option
                  v-for="u in purchaseUserSelectOptions"
                  :key="u.id"
                  :label="u.userName"
                  :value="u.id"
                />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 第七行：备注（整行） -->
        <el-form-item label="备注" class="quote-remark-item">
          <el-input v-model="formData.remark" type="textarea" :rows="2" placeholder="请输入备注" />
        </el-form-item>
          </el-form>
        </div>
      </div>

    <!-- 采购报价（独立面板，§4 info-section；编辑模式含更改日志/文档页签） -->
    <div class="info-section purchase-quote-section">
      <template v-if="!isEditMode">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('quoteUpsert.purchaseQuoteSection') }}</span>
          </div>
          <div v-if="formData.quotePriceRows.length > 0" class="section-header__meta">
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">{{ t('quoteUpsert.purchaseQuoteRowCount') }}</span>
              <span class="section-header-meta-item__value">{{ formData.quotePriceRows.length }}</span>
            </span>
          </div>
        </div>
        <div class="purchase-quote-section__body">
          <p class="purchase-quote-section__hint">
            {{ t('quoteUpsert.purchaseQuoteHint') }}
          </p>
          <div class="detail-items-table-wrap">
            <CrmDataTable
              :data="formData.quotePriceRows"
              class="items-table detail-panel-list-table quote-price-tier-table"
              size="small"
              stripe
              embedded
              :border="false"
            >
              <el-table-column :label="t('quoteUpsert.purchaseQuoteColumns.quantity')" min-width="120">
                <template #default="{ $index }">
                  <el-input-number
                    v-model="formData.quotePriceRows[$index].quantity"
                    :min="0"
                    :controls="false"
                    style="width: 100%"
                  />
                </template>
              </el-table-column>
              <el-table-column
                :label="t('quoteUpsert.purchaseQuoteColumns.priceCurrency')"
                min-width="220"
                class-name="tier-col-price-ccy"
              >
                <template #default="{ $index }">
                  <SettlementCurrencyAmountInput
                    v-model="formData.quotePriceRows[$index].unitPrice"
                    v-model:currency="formData.quotePriceRows[$index].currency"
                    :min="0"
                    :precision="6"
                    size="small"
                    class="q-select tier-price-ccy-input"
                  />
                </template>
              </el-table-column>
              <el-table-column :label="t('quoteUpsert.purchaseQuoteColumns.convertedUsd')" min-width="168">
                <template #default="{ $index }">
                  <span
                    class="tier-converted-display"
                    :title="convertedPriceTitle(formData.quotePriceRows[$index].convertedPrice)"
                  >
                    {{ formatConvertedPrice(formData.quotePriceRows[$index].convertedPrice) }}
                  </span>
                </template>
              </el-table-column>
              <el-table-column label="" width="108" align="center" fixed="right">
                <template #default="{ $index }">
                  <div class="tier-actions">
                    <el-button
                      type="danger"
                      link
                      :disabled="formData.quotePriceRows.length <= 1"
                      @click="removePriceRow($index)"
                      :title="t('quoteUpsert.purchaseQuoteActions.removeRow')"
                    >
                      <el-icon><Minus /></el-icon>
                    </el-button>
                    <el-button
                      type="primary"
                      link
                      @click="insertPriceRowAfter($index)"
                      :title="t('quoteUpsert.purchaseQuoteActions.insertRow')"
                    >
                      <el-icon><Plus /></el-icon>
                    </el-button>
                  </div>
                </template>
              </el-table-column>
            </CrmDataTable>
          </div>
        </div>
      </template>

      <template v-else>
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('quoteUpsert.supplierQuoteSection') }}</span>
          </div>
        </div>
        <div class="tabs-section">
        <div class="tabs-nav">
          <button
            class="tab-btn"
            :class="{ 'tab-btn--active': editPanelTab === 'items' }"
            type="button"
            @click="editPanelTab = 'items'"
          >
            {{ formatQuoteEditPanelTabLabel(t('quoteDetail.tabs.items'), 'items') }}
          </button>
          <button
            class="tab-btn"
            :class="{ 'tab-btn--active': editPanelTab === 'changeLogs' }"
            type="button"
            @click="editPanelTab = 'changeLogs'"
          >
            {{ formatQuoteEditPanelTabLabel(t('quoteDetail.tabs.changeLogs'), 'changeLogs') }}
          </button>
          <button
            class="tab-btn"
            :class="{ 'tab-btn--active': editPanelTab === 'documents' }"
            type="button"
            @click="editPanelTab = 'documents'"
          >
            {{ formatQuoteEditPanelTabLabel(t('quoteDetail.tabs.documents'), 'documents') }}
          </button>
        </div>
        <div class="tabs-body">
          <div v-show="editPanelTab === 'items'" class="purchase-quote-section__body purchase-quote-section__body--tab">
            <p class="purchase-quote-section__hint">
              {{ t('quoteUpsert.purchaseQuoteHint') }}
            </p>
            <div class="detail-items-table-wrap">
              <CrmDataTable
                :data="formData.quotePriceRows"
                class="items-table detail-panel-list-table quote-price-tier-table"
                size="small"
                stripe
                embedded
                :border="false"
              >
                <el-table-column :label="t('quoteUpsert.purchaseQuoteColumns.quantity')" min-width="120">
                  <template #default="{ $index }">
                    <el-input-number
                      v-model="formData.quotePriceRows[$index].quantity"
                      :min="0"
                      :controls="false"
                      style="width: 100%"
                    />
                  </template>
                </el-table-column>
                <el-table-column
                  :label="t('quoteUpsert.purchaseQuoteColumns.priceCurrency')"
                  min-width="220"
                  class-name="tier-col-price-ccy"
                >
                  <template #default="{ $index }">
                    <SettlementCurrencyAmountInput
                      v-model="formData.quotePriceRows[$index].unitPrice"
                      v-model:currency="formData.quotePriceRows[$index].currency"
                      :min="0"
                      :precision="6"
                      size="small"
                      class="q-select tier-price-ccy-input"
                    />
                  </template>
                </el-table-column>
                <el-table-column :label="t('quoteUpsert.purchaseQuoteColumns.convertedUsd')" min-width="168">
                  <template #default="{ $index }">
                    <span
                      class="tier-converted-display"
                      :title="convertedPriceTitle(formData.quotePriceRows[$index].convertedPrice)"
                    >
                      {{ formatConvertedPrice(formData.quotePriceRows[$index].convertedPrice) }}
                    </span>
                  </template>
                </el-table-column>
                <el-table-column label="" width="108" align="center" fixed="right">
                  <template #default="{ $index }">
                    <div class="tier-actions">
                      <el-button
                        type="danger"
                        link
                        :disabled="formData.quotePriceRows.length <= 1"
                        @click="removePriceRow($index)"
                        :title="t('quoteUpsert.purchaseQuoteActions.removeRow')"
                      >
                        <el-icon><Minus /></el-icon>
                      </el-button>
                      <el-button
                        type="primary"
                        link
                        @click="insertPriceRowAfter($index)"
                        :title="t('quoteUpsert.purchaseQuoteActions.insertRow')"
                      >
                        <el-icon><Plus /></el-icon>
                      </el-button>
                    </div>
                  </template>
                </el-table-column>
              </CrmDataTable>
            </div>
          </div>
          <div v-show="editPanelTab === 'changeLogs'" class="detail-items-table-wrap">
            <el-table
              v-if="fieldChangeLogs.length > 0"
              v-loading="changeLogsLoading"
              :data="fieldChangeLogs"
              class="detail-panel-list-table"
              size="small"
              stripe
            >
              <el-table-column :label="t('quoteDetail.logs.colChangeTime')" width="160">
                <template #default="{ row }">{{ formatChangeLogTime(row?.changedAt) }}</template>
              </el-table-column>
              <el-table-column :label="t('quoteDetail.logs.colOperator')" width="100" show-overflow-tooltip>
                <template #default="{ row }">{{ row.changedByUserName || t('quoteDetail.logs.system') }}</template>
              </el-table-column>
              <el-table-column :label="t('quoteDetail.logs.colObject')" width="140" show-overflow-tooltip>
                <template #default="{ row }">{{ quoteChangeLogObjectLabel(row) }}</template>
              </el-table-column>
              <el-table-column :label="t('quoteDetail.logs.colField')" min-width="120" show-overflow-tooltip>
                <template #default="{ row }">{{ row.fieldLabel || row.fieldName }}</template>
              </el-table-column>
              <el-table-column :label="t('quoteDetail.logs.colOldValue')" min-width="160" show-overflow-tooltip>
                <template #default="{ row }">{{ row.oldValue ?? t('quoteDetail.logs.emptyValue') }}</template>
              </el-table-column>
              <el-table-column :label="t('quoteDetail.logs.colNewValue')" min-width="160" show-overflow-tooltip>
                <template #default="{ row }">{{ row.newValue ?? t('quoteDetail.logs.emptyValue') }}</template>
              </el-table-column>
            </el-table>
            <DetailListPanelEmpty v-else-if="!changeLogsLoading" size="low" />
          </div>
          <div v-show="editPanelTab === 'documents'" class="doc-tab-content">
            <DocumentUploadPanel
              biz-type="QUOTE"
              :biz-id="quoteEditId"
              :max-files="20"
              :max-size-mb="100"
              @uploaded="onQuoteDocumentUploaded"
            />
            <DocumentListPanel
              ref="docListRef"
              biz-type="QUOTE"
              :biz-id="quoteEditId"
              view-mode="list"
              style="margin-top: 16px"
            />
          </div>
        </div>
        </div>
      </template>
    </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, inject } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ArrowRight, Check, Plus, Minus } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { quoteApi, type QuoteFieldChangeLogRow } from '@/api/quote'
import { vendorApi, vendorContactApi } from '@/api/vendor'
import { rfqApi } from '@/api/rfq'
import type { Vendor } from '@/types/vendor'
import { getApiErrorMessage } from '@/utils/apiError'
import { runValidatedFormSave } from '@/composables/useFormSubmit'
import {
  extractMpn,
  extractBrand,
  mapCurrencyLabelFromRaw,
  fetchLinkedRfqItemRecord
} from '@/utils/rfqLinkedItemSummary'
import { useAuthStore } from '@/stores/auth'
import { useQuoteHistoryContextStore } from '@/stores/quoteHistoryContext'
import { useMaterialIntelLookupStore } from '@/stores/materialIntelLookup'
import { AI_PERMISSION_MATERIAL_INTEL_LOOKUP } from '@/api/ai'
import { resolveRfqItemMaterialPn } from '@/utils/materialPn'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import SalesUserCascader from '@/components/SalesUserCascader.vue'
import SettlementCurrencyAmountInput from '@/components/SettlementCurrencyAmountInput.vue'
import MaterialProductionDateSelect from '@/components/MaterialProductionDateSelect.vue'
import { authApi, type PurchaseDeptStaffUserOption } from '@/api/auth'
import { useMaterialProductionDateDict } from '@/composables/useMaterialProductionDateDict'
import { financeExchangeRateApi } from '@/api/financeExchangeRate'
import { normalizeSettlementCurrencyCode, DEFAULT_SETTLEMENT_CURRENCY_CODE, DEFAULT_SETTLEMENT_CURRENCY_STRING } from '@/constants/currency'
import { unitLocalToUsd } from '@/utils/exchangeRateToUsd'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { quoteChangeLogObjectLabel } from '@/utils/businessLogLabels'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import { canQuoteRfqItem } from '@/utils/rfqItemQuoteAccessRules'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import {
  QuoteMainStatus,
  isQuoteReadOnly,
  quoteMainStatusI18nKey,
  quoteMainStatusTagType,
  normalizeQuoteMainStatus
} from '@/utils/quoteMainStatus'

const props = withDefaults(
  defineProps<{
    embedded?: boolean
    embedRfqId?: string | null
    embedRfqItemId?: string | null
    embedRfqCode?: string | null
  }>(),
  {
    embedded: false,
    embedRfqId: null,
    embedRfqItemId: null,
    embedRfqCode: null
  }
)

const emit = defineEmits<{
  success: []
  'mark-no-quote': []
}>()

const embedded = computed(() => !!props.embedded)

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const quoteHistoryContextStore = useQuoteHistoryContextStore()
const materialIntelLookupStore = useMaterialIntelLookupStore()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { ensureLoaded: ensureMaterialPdDict, coerceProductionDateToCode: coercePd } = useMaterialProductionDateDict()

/** 与采购订单编辑一致：采购部职员 + SYS_ADMIN 启用账号（purchase-dept-staff-users） */
const purchaseUserSelectOptions = ref<PurchaseDeptStaffUserOption[]>([])
const purchaseUserOptionsLoading = ref(false)

const isEditMode = computed(() => route.name === 'QuoteEdit')
const quoteEditId = computed(() => (isEditMode.value ? String(route.params.id ?? '') : ''))
const quoteStatus = ref<number | null>(null)

const editPanelTab = ref<'items' | 'changeLogs' | 'documents'>('items')
const docListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)
const documentCount = ref(0)
const fieldChangeLogs = ref<QuoteFieldChangeLogRow[]>([])
const changeLogsLoading = ref(false)
const changeLogsLoaded = ref(false)

type QuoteEditPanelTabKey = 'items' | 'changeLogs' | 'documents'

function quoteEditPanelTabCount(tab: QuoteEditPanelTabKey): number {
  switch (tab) {
    case 'items':
      return formData.value.quotePriceRows.length
    case 'documents':
      return documentCount.value
    case 'changeLogs':
      return fieldChangeLogs.value.length
    default:
      return 0
  }
}

function formatQuoteEditPanelTabLabel(label: string, tab: QuoteEditPanelTabKey): string {
  const count = quoteEditPanelTabCount(tab)
  return count > 0 ? `${label} (${count})` : label
}

function resetChangeLogs() {
  fieldChangeLogs.value = []
  changeLogsLoaded.value = false
}

async function loadChangeLogs(opts?: { silent?: boolean }) {
  const id = quoteEditId.value
  if (!id) return
  changeLogsLoading.value = true
  try {
    fieldChangeLogs.value = (await quoteApi.getChangeLogs(id)) ?? []
    changeLogsLoaded.value = true
  } catch (e: unknown) {
    if (!opts?.silent) {
      ElMessage.error(e instanceof Error ? e.message : t('quoteList.loadFailed'))
    }
  } finally {
    changeLogsLoading.value = false
  }
}

async function fetchDocumentCount() {
  const id = quoteEditId.value
  if (!id) {
    documentCount.value = 0
    return
  }
  try {
    const { documentApi } = await import('@/api/document')
    const res = await documentApi.getDocuments('QUOTE', id)
    documentCount.value = Array.isArray(res) ? res.length : 0
  } catch {
    documentCount.value = 0
  }
}

function formatChangeLogTime(v?: string) {
  if (!v) return '—'
  return formatDisplayDateTime(v) || '—'
}

function onQuoteDocumentUploaded() {
  docListRef.value?.refresh()
  void fetchDocumentCount()
}

watch(editPanelTab, (tab) => {
  if (tab === 'changeLogs' && !changeLogsLoaded.value) void loadChangeLogs()
})

const rfqLink = computed(() => {
  if (embedded.value) {
    const rfqId = (props.embedRfqId || '').trim() || undefined
    const rfqCode = (props.embedRfqCode || '').trim() || undefined
    const rfqItemId = (props.embedRfqItemId || '').trim() || undefined
    return { rfqId, rfqCode, rfqItemId, rfqItemIds: [] as string[] }
  }
  const rfqId = route.query.rfqId as string | undefined
  const rfqCode = route.query.rfqCode as string | undefined
  const rfqItemId = route.query.rfqItemId as string | undefined
  const raw = route.query.rfqItemIds as string | undefined
  const rfqItemIds = raw ? raw.split(',').map((s) => s.trim()).filter(Boolean) : []
  return { rfqId, rfqCode, rfqItemId, rfqItemIds }
})

const markNoQuoteLoading = ref(false)
const canMarkNoQuote = computed(
  () => embedded.value && !isEditMode.value && !!String(rfqLink.value.rfqItemId || '').trim()
)

function openQuoteDesktop() {
  const q: Record<string, string> = {}
  const itemId = String(rfqLink.value.rfqItemId || '').trim()
  if (itemId) q.rfqItemId = itemId
  router.push({ name: 'QuoteDesktop', query: q })
}

/** 顶部提示：不展示明细 ID，仅报价需求编号 + 物料号/品牌/数量/目标价（单行） */
const hasRfqLinkAlert = computed(() => !!rfqLink.value.rfqId)

const linkAlertRfqDisplay = computed(() => {
  const { rfqId, rfqCode } = rfqLink.value
  return (rfqCode || rfqId || '').trim() || '—'
})

const linkAlertBatchCount = computed(() => {
  const ids = rfqLink.value.rfqItemIds
  return ids.length > 1 ? ids.length : 0
})

/** 提示栏间距：报价需求↔RFQ 为 2 个半角空格；黄框↔红框 8 个汉字宽（全角空格）；红框内四段之间 4 个全角空格 */
const linkAlertGap2 = '  '
const linkAlertSep2 = '  '
const linkAlertSep8Ideo = '\u3000'.repeat(8)
const linkAlertSep4Ideo = '\u3000'.repeat(4)

const submitLoading = ref(false)
const pageLoading = ref(false)
const formRef = ref()
const rfqDetailLocked = ref(false)

const vendorOptions = ref<{ value: string; label: string }[]>([])
const vendorSearchLoading = ref(false)
let vendorSearchTimer: ReturnType<typeof setTimeout> | null = null

const contactOptions = ref<{ value: string; label: string }[]>([])
const contactLoading = ref(false)

function todayStr() {
  return new Date().toISOString().slice(0, 10)
}

/** 与 RFQ 编辑页展示一致：接口失败时的默认汇率（1 USD 兑外币数量） */
const DEFAULT_EXCHANGE_RATES = { usdToCny: 6.9228, usdToHkd: 7.8238, usdToEur: 0.8525 }

const exchangeRates = ref({ ...DEFAULT_EXCHANGE_RATES })

function emptyPriceRow() {
  return {
    id: undefined as string | undefined,
    quantity: 0,
    unitPrice: 0,
    /** 与 SETTLEMENT_CURRENCY_OPTIONS / CurrencyCode 一致 */
    currency: DEFAULT_SETTLEMENT_CURRENCY_CODE,
    /** 美元折算单价（convert_price 口径），由汇率自动计算，勿手改 */
    convertedPrice: undefined as number | undefined
  }
}

const formData = ref({
  quoteCode: '',
  quoteDate: todayStr(),
  rfqId: '',
  rfqItemId: '',
  mpn: '',
  brand: '',
  brandOrigin: '',
  /** 需求明细数量（摘要条展示），与下方报价阶梯行独立 */
  quantity: 1,
  targetPrice: undefined as number | undefined,
  currencyLabel: DEFAULT_SETTLEMENT_CURRENCY_STRING,

  vendorId: '',
  vendorName: '',
  vendorContactId: '',
  contactName: '',
  priceType: '',
  expiryDate: '',
  productionDate: '',
  leadTime: '',
  labelType: 0,
  waferOrigin: 2,
  packageOrigin: 2,
  freeShipping: false,
  minPackageQty: 0,
  stockQty: 0,
  moq: 0,
  salesUserId: '',
  purchaseUserId: '',
  salesUserName: '',
  purchaseUserName: '',
  createTime: '',
  createUserName: '',
  remark: '',
  /** 采购报价阶梯：数量 / 价格 / 币别 / 折算价，新建默认一行空白 */
  quotePriceRows: [emptyPriceRow()]
})

const captionAvatarChar = computed(() => {
  const code = String(formData.value.quoteCode ?? '').trim()
  if (code) return code.charAt(0).toUpperCase()
  return 'Q'
})

const quoteBasicCreateDateText = computed(() => {
  const raw = formData.value.createTime?.trim()
  if (raw) return formatDisplayDate(raw) || '—'
  const quoteDate = formData.value.quoteDate?.trim()
  if (quoteDate) return formatDisplayDate(quoteDate) || quoteDate
  return '—'
})

const quoteBasicCreateUserText = computed(() => {
  const name = formData.value.createUserName?.trim()
  if (name) return name
  if (isEditMode.value) {
    return (
      formData.value.purchaseUserName?.trim() ||
      formData.value.salesUserName?.trim() ||
      '—'
    )
  }
  return authStore.user?.userName?.trim() || '—'
})

/**
 * 将单行「单价 + 币别」折算为美元单价（与 ExchangeRateToUsdConverter / 订单 convert_price 一致）。
 */
function unitPriceToUsd(
  unitPrice: number,
  currency: number,
  rates: { usdToCny: number; usdToHkd: number; usdToEur: number }
): number | undefined {
  return unitLocalToUsd(unitPrice, currency, rates)
}

function recalcAllConvertedPrices() {
  const rates = exchangeRates.value
  for (const row of formData.value.quotePriceRows) {
    const usd = unitPriceToUsd(Number(row.unitPrice), Number(row.currency), rates)
    if (row.convertedPrice !== usd) row.convertedPrice = usd
  }
}

function formatConvertedPrice(v: number | undefined) {
  if (v == null || Number.isNaN(Number(v))) return '—'
  return Number(v).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

function convertedPriceTitle(v: number | undefined) {
  if (v == null || Number.isNaN(Number(v))) return ''
  return `美元折算价：${Number(v)} USD（保存时提交）`
}

async function refreshExchangeRatesFromApi() {
  try {
    const dto = await financeExchangeRateApi.getCurrent()
    exchangeRates.value = {
      usdToCny: Number(dto.usdToCny) || DEFAULT_EXCHANGE_RATES.usdToCny,
      usdToHkd: Number(dto.usdToHkd) || DEFAULT_EXCHANGE_RATES.usdToHkd,
      usdToEur: Number(dto.usdToEur) || DEFAULT_EXCHANGE_RATES.usdToEur
    }
  } catch {
    exchangeRates.value = { ...DEFAULT_EXCHANGE_RATES }
    ElMessage.warning(
      '加载系统汇率失败，已使用默认汇率计算折算价；请在「系统设置 → 财务参数 → 汇率」维护后刷新本页。'
    )
  }
}

watch(
  () => formData.value.quotePriceRows,
  () => {
    recalcAllConvertedPrices()
  },
  { deep: true }
)

watch(exchangeRates, () => recalcAllConvertedPrices(), { deep: true })

const targetPriceText = computed(() => {
  const p = formData.value.targetPrice
  if (p == null || p === ('' as any)) return '—'
  const n = Number(p)
  if (Number.isNaN(n)) return '—'
  return `${n.toLocaleString('zh-CN', { minimumFractionDigits: 4, maximumFractionDigits: 4 })} ${formData.value.currencyLabel || DEFAULT_SETTLEMENT_CURRENCY_STRING}`
})

function formatNumber(n: number) {
  if (n == null || Number.isNaN(Number(n))) return '—'
  return Number(n).toLocaleString('zh-CN')
}

function rfqItemAssigneeFromRecord(item: Record<string, unknown>) {
  return {
    assignedPurchaserUserId1: String(item.assignedPurchaserUserId1 ?? item.AssignedPurchaserUserId1 ?? ''),
    assignedPurchaserUserId2: String(item.assignedPurchaserUserId2 ?? item.AssignedPurchaserUserId2 ?? '')
  }
}

function denyQuoteAccessAndReturn() {
  ElMessage.warning('您无权为该需求明细创建报价')
  const back = parseReturnTo()
  router.push(back || { name: 'RFQItemList' })
}

async function ensureQuoteAccessForRfqItemId(rfqId: string, itemId: string): Promise<boolean> {
  const loaded = await fetchLinkedRfqItemRecord(rfqId, itemId)
  if (!loaded) return true
  if (!canQuoteRfqItem(authStore.user, rfqItemAssigneeFromRecord(loaded.item as Record<string, unknown>))) {
    denyQuoteAccessAndReturn()
    return false
  }
  return true
}

async function ensureQuoteAccessForLinkedItems(): Promise<boolean> {
  const { rfqId, rfqItemId, rfqItemIds } = rfqLink.value
  const ids = rfqItemIds.length ? rfqItemIds : rfqItemId ? [rfqItemId] : []
  if (!ids.length || !rfqId) return true
  for (const id of ids) {
    const ok = await ensureQuoteAccessForRfqItemId(rfqId, id)
    if (!ok) return false
  }
  return true
}

async function loadLinkedRfqItem() {
  await ensureMaterialPdDict()
  const { rfqId, rfqItemId, rfqItemIds } = rfqLink.value
  const itemId = rfqItemId || (rfqItemIds.length === 1 ? rfqItemIds[0] : '')
  rfqDetailLocked.value = false
  if (!itemId) {
    if (formData.value.quotePriceRows.length === 0) {
      formData.value.quotePriceRows = [emptyPriceRow()]
    }
    return
  }

  if (!(await ensureQuoteAccessForLinkedItems())) return

  pageLoading.value = true
  try {
    const loaded = await fetchLinkedRfqItemRecord(rfqId || '', itemId)
    if (!loaded) {
      ElMessage.warning('未找到需求明细，请手动填写物料型号与品牌')
      return
    }
    const { item, rfqHeader } = loaded

    const mpn = extractMpn(item)
    const brand = extractBrand(item)
    const qty = Number(item['quantity'] ?? item['Quantity'] ?? 1) || 1
    const tp = item['targetPrice'] ?? item['TargetPrice']
    const targetPrice = tp != null && tp !== '' ? Number(tp) : undefined

    formData.value.mpn = mpn
    formData.value.brand = brand
    formData.value.rfqItemId = itemId
    formData.value.rfqId = String(item['rfqId'] ?? item['RfqId'] ?? rfqId ?? '')
    formData.value.quantity = qty
    formData.value.targetPrice = targetPrice
    formData.value.currencyLabel = mapCurrencyLabelFromRaw(item)
    formData.value.productionDate = coercePd(
      String(item.productionDate ?? item.ProductionDate ?? '').trim()
    )
    const exp = item.expiryDate ?? item.ExpiryDate
    if (exp) {
      formData.value.expiryDate = String(exp).slice(0, 10)
    }
    formData.value.minPackageQty = Number(item.minPackageQty ?? item.MinPackageQty ?? 0) || 0
    formData.value.moq = Number(item.minOrderQty ?? item.MinOrderQty ?? 0) || 0
    formData.value.stockQty = qty
    formData.value.brandOrigin = ''
    rfqDetailLocked.value = true

    formData.value.quotePriceRows = [emptyPriceRow()]

    const assignedPurchaserId = String(
      item.assignedPurchaserUserId1 ??
        item.AssignedPurchaserUserId1 ??
        item.assignedPurchaserUserId2 ??
        item.AssignedPurchaserUserId2 ??
        ''
    ).trim()
    if (assignedPurchaserId) {
      formData.value.purchaseUserId = assignedPurchaserId
      const name =
        (item.assignedPurchaserName1 as string) ||
        (item.AssignedPurchaserName1 as string) ||
        (item.assignedPurchaserName2 as string) ||
        (item.AssignedPurchaserName2 as string) ||
        ''
      if (name) formData.value.purchaseUserName = name
    }

    if (rfqId) {
      try {
        const rfq = rfqHeader ?? (await rfqApi.getRFQById(rfqId))
        if (rfq.salesUserId) formData.value.salesUserId = String(rfq.salesUserId)
        formData.value.salesUserName = rfq.salesUserName || formData.value.salesUserName
      } catch {
        /* 主表失败时保留手工业务员 */
      }
    }
  } catch (e) {
    ElMessage.warning(getApiErrorMessage(e, '加载需求明细失败，请手动填写物料型号与品牌'))
    rfqDetailLocked.value = false
  } finally {
    pageLoading.value = false
  }
}

watch(
  () =>
    embedded.value
      ? `${props.embedRfqId || ''}|${props.embedRfqItemId || ''}`
      : `${route.query.rfqId || ''}|${route.query.rfqItemId || ''}|${route.query.rfqItemIds || ''}`,
  () => {
    if (!embedded.value && route.name === 'QuoteEdit') return
    void loadLinkedRfqItem()
  },
  { immediate: true }
)

const formRules = computed(() => {
  const base: Record<string, unknown[]> = {
    mpn: [{ required: true, message: '请输入物料型号', trigger: 'blur' }],
    priceType: [{ required: true, message: '请选择价格类型', trigger: 'change' }],
    brand: [{ required: true, message: '请输入品牌', trigger: 'blur' }],
    productionDate: [{ required: true, message: '请选择生产日期/DC', trigger: 'change' }],
    waferOrigin: [{ required: true, message: '请选择晶圆产地', trigger: 'change' }],
    packageOrigin: [{ required: true, message: '请选择封装产地', trigger: 'change' }],
    ...(maskSaleSensitiveFields.value
      ? {}
      : { salesUserId: [{ required: true, message: '请选择业务员', trigger: 'change' }] }),
    purchaseUserId: [{ required: true, message: '请选择采购员', trigger: 'change' }]
  }
  if (!maskPurchaseSensitiveFields.value) {
    base.vendorId = [{ required: true, message: '请选择供应商', trigger: 'change' }]
    base.vendorContactId = [{ required: true, message: '请选择联系人', trigger: 'change' }]
  }
  return base
})

function onSalesUserChange(p: { id: string; label: string }) {
  formData.value.salesUserName = p.label || ''
}

function normalizePurchaseDeptStaffUser(row: Record<string, unknown>): PurchaseDeptStaffUserOption | null {
  const id = String(row.id ?? row.Id ?? '').trim()
  if (!id) return null
  const userName = String(row.userName ?? row.UserName ?? row.label ?? row.Label ?? '').trim()
  return {
    id,
    userName: userName || id,
    realName: row.realName != null ? String(row.realName) : row.RealName != null ? String(row.RealName) : undefined,
    label: userName || id
  }
}

function findPurchaseUserOption(userId: string): PurchaseDeptStaffUserOption | undefined {
  const key = userId.trim().toLowerCase()
  return purchaseUserSelectOptions.value.find((u) => u.id.trim().toLowerCase() === key)
}

function reconcileQuotePurchaseUserWithSelectOptions(allowExistingFromQuote = false) {
  const id = formData.value.purchaseUserId?.trim()
  if (!id) return

  const hit = findPurchaseUserOption(id)
  if (hit) {
    formData.value.purchaseUserName = hit.userName
    return
  }

  const name = formData.value.purchaseUserName?.trim()
  if (allowExistingFromQuote && name) {
    purchaseUserSelectOptions.value = [
      ...purchaseUserSelectOptions.value,
      { id, userName: name, realName: undefined, label: name }
    ]
  }
}

function onPurchaseUserSelectChange(userId: string | undefined) {
  const id = userId ? String(userId) : ''
  const row = findPurchaseUserOption(id)
  formData.value.purchaseUserName = row?.userName ?? ''
}

async function loadPurchaseUserSelectOptions() {
  purchaseUserOptionsLoading.value = true
  try {
    const rows = await authApi.getPurchaseDeptStaffUsers()
    purchaseUserSelectOptions.value = rows
      .map((u) => normalizePurchaseDeptStaffUser(u as unknown as Record<string, unknown>))
      .filter((u): u is PurchaseDeptStaffUserOption => u != null)
    reconcileQuotePurchaseUserWithSelectOptions(true)
  } catch (e: unknown) {
    purchaseUserSelectOptions.value = []
    const msg = e instanceof Error ? e.message : String(e)
    ElMessage.error(msg || '加载采购员列表失败')
  } finally {
    purchaseUserOptionsLoading.value = false
  }
}

function onVendorFilterInput(query: string) {
  if (vendorSearchTimer) clearTimeout(vendorSearchTimer)
  if (!query || query.trim().length < 1) {
    if (formData.value.vendorId && formData.value.vendorName) {
      vendorOptions.value = [{ value: formData.value.vendorId, label: formData.value.vendorName }]
    } else {
      vendorOptions.value = []
    }
    return
  }
  vendorSearchTimer = setTimeout(async () => {
    vendorSearchLoading.value = true
    try {
      const res = await vendorApi.searchVendors({
        pageNumber: 1,
        pageSize: 30,
        keyword: query.trim()
      })
      vendorOptions.value = (res.items || []).map((v: Vendor) => ({
        value: v.id,
        label: v.officialName || v.nickName || v.code || '供应商'
      }))
    } catch {
      vendorOptions.value = []
    } finally {
      vendorSearchLoading.value = false
    }
  }, 300)
}

function onVendorChange(val: string | null | undefined) {
  formData.value.vendorContactId = ''
  formData.value.contactName = ''
  contactOptions.value = []
  if (!val) {
    formData.value.vendorName = ''
    return
  }
  const found = vendorOptions.value.find((x) => x.value === val)
  if (found) formData.value.vendorName = found.label
  void loadVendorContacts(val)
}

/** 编辑回填：联系人 ID 可能不在下拉列表中（已删/接口延迟），仍保留选中态 */
function reconcileQuoteVendorContact(preferredContactId?: string, preferredContactName?: string) {
  const id = (preferredContactId ?? formData.value.vendorContactId ?? '').trim()
  const name = (preferredContactName ?? formData.value.contactName ?? '').trim()
  if (!id) {
    formData.value.vendorContactId = ''
    if (!name) formData.value.contactName = ''
    return
  }
  let hit = contactOptions.value.find((c) => c.value === id)
  if (!hit) {
    hit = { value: id, label: name || id }
    contactOptions.value = [...contactOptions.value, hit]
  }
  formData.value.vendorContactId = id
  formData.value.contactName = hit.label.split(' / ')[0]?.trim() || name || id
}

async function loadVendorContacts(
  vendorId: string,
  preferredContactId?: string,
  preferredContactName?: string
) {
  if (!vendorId) {
    contactOptions.value = []
    reconcileQuoteVendorContact(preferredContactId, preferredContactName)
    return
  }
  contactLoading.value = true
  try {
    const list = await vendorContactApi.getContactsByVendorId(vendorId)
    contactOptions.value = list.map((c) => ({
      value: c.id,
      label: [c.cName, c.mobile].filter(Boolean).join(' / ') || c.id
    }))
  } catch {
    contactOptions.value = []
  } finally {
    contactLoading.value = false
  }
  reconcileQuoteVendorContact(preferredContactId, preferredContactName)
}

function onContactChange(id: string | undefined) {
  if (!id) {
    formData.value.contactName = ''
    return
  }
  const row = contactOptions.value.find((c) => c.value === id)
  formData.value.contactName = row?.label?.split(' / ')[0] || ''
}

/** 将列表/详情中的报价主表 + 明细映射为新建页表单（与保存时结构一致） */
async function applyQuoteToForm(q: Record<string, unknown>) {
  const prRows = q.quotePriceRows as unknown
  let rows: ReturnType<typeof emptyPriceRow>[] = []
  if (Array.isArray(prRows) && prRows.length > 0) {
    rows = prRows.map((r: Record<string, unknown>) => ({
      id: String(r.id ?? r.Id ?? '').trim() || undefined,
      quantity: Number(r.quantity) || 0,
      unitPrice: r.unitPrice != null && r.unitPrice !== '' ? Number(r.unitPrice) : 0,
      currency: normalizeSettlementCurrencyCode(r.currency ?? r.Currency),
      convertedPrice:
        r.convertedPrice != null && r.convertedPrice !== '' ? Number(r.convertedPrice) : undefined
    }))
  } else {
    const items = q.items as unknown
    if (Array.isArray(items) && items.length > 0) {
      rows = items.map((it: Record<string, unknown>) => ({
        id: String(it.id ?? it.Id ?? '').trim() || undefined,
        quantity: Number(it.quantity) || 0,
        unitPrice: it.unitPrice != null && it.unitPrice !== '' ? Number(it.unitPrice) : 0,
        currency: normalizeSettlementCurrencyCode(it.currency ?? it.Currency),
        convertedPrice:
          it.convertedPrice != null && it.convertedPrice !== ''
            ? Number(it.convertedPrice)
            : undefined
      }))
    }
  }
  if (rows.length === 0) rows = [emptyPriceRow()]

  formData.value.quoteCode = String(q.quoteCode ?? q.quoteNumber ?? q.QuoteCode ?? '')
  quoteStatus.value = normalizeQuoteMainStatus(q.status ?? q.Status) ?? QuoteMainStatus.New
  formData.value.createTime = String(q.createTime ?? q.CreateTime ?? '')
  formData.value.createUserName = String(q.createUserName ?? q.CreateUserName ?? '')
  formData.value.quoteDate = String(q.quoteDate ?? todayStr()).slice(0, 10)
  formData.value.rfqId = String(q.rfqId ?? q.RfqId ?? '')
  formData.value.rfqItemId = String(q.rfqItemId ?? q.RfqItemId ?? '')
  formData.value.mpn = String(q.mpn ?? q.Mpn ?? '')
  formData.value.remark = String(q.remark ?? '')
  formData.value.salesUserId = String(q.salesUserId ?? q.SalesUserId ?? '')
  formData.value.purchaseUserId = String(q.purchaseUserId ?? q.PurchaseUserId ?? '')
  formData.value.salesUserName = String(q.salesUserName ?? '')
  formData.value.purchaseUserName = String(q.purchaseUserName ?? '')
  formData.value.quotePriceRows = rows

  const items = q.items as Record<string, unknown>[] | undefined
  const first = items?.[0]
  if (first) {
    formData.value.vendorId = String(first.vendorId ?? first.VendorId ?? '')
    formData.value.vendorName = String(first.vendorName ?? first.VendorName ?? '')
    const savedContactId = String(first.contactId ?? first.ContactId ?? '').trim()
    const savedContactName = String(first.contactName ?? first.ContactName ?? '').trim()
    formData.value.contactName = savedContactName
    formData.value.vendorContactId = savedContactId
    formData.value.priceType = String(first.priceType ?? first.PriceType ?? '')
    const exp = first.expiryDate ?? first.ExpiryDate
    formData.value.expiryDate = exp ? String(exp).slice(0, 10) : ''
    formData.value.productionDate = coercePd(
      String(
        first.productionDate ?? first.ProductionDate ?? first.dateCode ?? first.DateCode ?? ''
      ).trim()
    )
    formData.value.leadTime = String(first.leadTime ?? first.LeadTime ?? '')
    formData.value.labelType = Number(first.labelType ?? first.LabelType ?? 0)
    formData.value.waferOrigin = Number(first.waferOrigin ?? first.WaferOrigin ?? 2)
    formData.value.packageOrigin = Number(first.packageOrigin ?? first.PackageOrigin ?? 2)
    formData.value.freeShipping = Boolean(first.freeShipping ?? first.FreeShipping)
    formData.value.minPackageQty = Number(first.minPackageQty ?? first.MinPackageQty ?? 0)
    formData.value.stockQty = Number(first.stockQty ?? first.StockQty ?? 0)
    formData.value.moq = Number(first.moq ?? first.Moq ?? first.minOrderQty ?? 0)
    formData.value.brand = String(first.brand ?? first.Brand ?? '')
    formData.value.brandOrigin = String(first.brandOrigin ?? first.BrandOrigin ?? '')
    formData.value.quantity = Number(q.quantity ?? first.quantity ?? formData.value.quantity) || 1
    const tp = q.targetPrice ?? first.targetPrice
    formData.value.targetPrice =
      tp != null && tp !== '' ? Number(tp as number) : formData.value.targetPrice

    if (formData.value.vendorId) {
      vendorOptions.value = [
        { value: formData.value.vendorId, label: formData.value.vendorName || formData.value.vendorId }
      ]
      await loadVendorContacts(formData.value.vendorId, savedContactId, savedContactName)
    }
  } else {
    formData.value.brand = String(q.brand ?? q.Brand ?? '')
    formData.value.brandOrigin = String(q.brandOrigin ?? q.BrandOrigin ?? '')
  }
}

async function loadQuoteForEdit() {
  await ensureMaterialPdDict()
  const id = route.params.id as string
  if (!id) return
  pageLoading.value = true
  resetChangeLogs()
  documentCount.value = 0
  editPanelTab.value = 'items'
  try {
    const res = await quoteApi.getById(id)
    const q = res?.data as Record<string, unknown> | undefined
    if (!q) {
      ElMessage.error('报价单不存在')
      router.push({ name: 'QuoteList' })
      return
    }
    await applyQuoteToForm(q)
    void loadChangeLogs({ silent: true })
    void fetchDocumentCount()
    const itemId = formData.value.rfqItemId?.trim()
    const rfqId = (formData.value.rfqId || rfqLink.value.rfqId || '').trim()
    if (itemId && rfqId) {
      await ensureQuoteAccessForRfqItemId(rfqId, itemId)
    }
  } catch {
    ElMessage.error('加载报价失败')
    router.push({ name: 'QuoteList' })
  } finally {
    pageLoading.value = false
  }
}

/** 从需求明细等入口携带 returnTo，保存/返回时回到来源页（仅允许站内 path） */
function parseReturnTo(): string | null {
  const raw = route.query.returnTo
  const s = Array.isArray(raw) ? raw[0] : raw
  if (typeof s !== 'string' || !s.trim()) return null
  let path = s.trim()
  try {
    path = decodeURIComponent(path)
  } catch {
    return null
  }
  if (!path.startsWith('/') || path.startsWith('//')) return null
  if (/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(path)) return null
  return path
}

const handleBack = () => {
  const back = parseReturnTo()
  if (back) {
    router.push(back)
    return
  }
  router.push({ name: 'QuoteList' })
}

function insertPriceRowAfter(index: number) {
  formData.value.quotePriceRows.splice(index + 1, 0, emptyPriceRow())
}

function removePriceRow(index: number) {
  if (formData.value.quotePriceRows.length <= 1) return
  formData.value.quotePriceRows.splice(index, 1)
}

function syncMaterialIntelFromForm() {
  // 嵌入报价桌面时由 QuoteDesktop 按队列行绑定，避免与表单竞态
  if (embedded.value) return
  const pn = resolveRfqItemMaterialPn({ mpn: formData.value.mpn })
  materialIntelLookupStore.bindPn(pn)
  if (pn && authStore.hasPermission(AI_PERMISSION_MATERIAL_INTEL_LOOKUP)) {
    void materialIntelLookupStore.ensureLookup(pn, { triggerType: 'auto' })
  }
}

watch(
  () => [formData.value.mpn, formData.value.brand] as const,
  ([mpn, brand]) => {
    quoteHistoryContextStore.bind({ mpn, brand })
    syncMaterialIntelFromForm()
  },
  { immediate: true }
)

onMounted(async () => {
  workspaceLayout?.toggleRightPanel(true)
  quoteHistoryContextStore.bind({ mpn: formData.value.mpn, brand: formData.value.brand })
  syncMaterialIntelFromForm()
  await refreshExchangeRatesFromApi()
  await ensureMaterialPdDict()
  await loadPurchaseUserSelectOptions()
  if (isEditMode.value) {
    await loadQuoteForEdit()
    reconcileQuotePurchaseUserWithSelectOptions(true)
    recalcAllConvertedPrices()
    quoteHistoryContextStore.bind({ mpn: formData.value.mpn, brand: formData.value.brand })
    syncMaterialIntelFromForm()
    return
  }
  const u = authStore.user
  if (u?.id && !formData.value.salesUserId) {
    formData.value.salesUserId = u.id
  }
  if (u?.userName && !formData.value.salesUserName) {
    formData.value.salesUserName = u.userName
  }
  recalcAllConvertedPrices()
})

onUnmounted(() => {
  // 嵌入报价桌面时由队列 selected 提供 MPN，避免切换明细时右栏闪空
  if (!embedded.value) {
    quoteHistoryContextStore.clear()
    materialIntelLookupStore.clearBound()
  }
})

const handleSubmit = async () => {
  const rows = formData.value.quotePriceRows
  await runValidatedFormSave(formRef, {
    loading: submitLoading,
    afterValidate: async () => {
      const hasValidTier = rows.some(
        (r) => Number(r.quantity) >= 1 && r.unitPrice != null && !Number.isNaN(Number(r.unitPrice))
      )
      if (!hasValidTier) {
        ElMessage.warning('请在「采购报价」列表中至少填写一行：数量≥1 且 价格有效')
        return false
      }
      return true
    },
    task: async () => {
      const first = rows[0]
      const ids = rfqLink.value.rfqItemIds
      const fallbackItemId =
        formData.value.rfqItemId ||
        rfqLink.value.rfqItemId ||
        (ids.length === 1 ? ids[0] : '')
      const data = {
        ...formData.value,
        quoteDate: formData.value.quoteDate || todayStr(),
        rfqId: formData.value.rfqId || rfqLink.value.rfqId,
        rfqItemId: fallbackItemId,
        quotePriceRows: rows.map((r) => ({ ...r })),
        quoteCurrency: first?.currency ?? DEFAULT_SETTLEMENT_CURRENCY_CODE,
        unitPrice: first?.unitPrice ?? 0,
        convertedPrice: first?.convertedPrice,
        quoteLineQuantity: first?.quantity,
        items: [] as any[]
      }
      if (isEditMode.value) {
        const id = route.params.id as string
        await quoteApi.update(id, data)
        return { kind: 'edit' as const, id }
      }
      const res = await quoteApi.create(data)
      const id = (res?.data as { id?: string } | undefined)?.id
      return { kind: 'create' as const, id, back: parseReturnTo() }
    },
    onSuccess: (r) => {
      if (embedded.value) {
        ElMessage.success(t('quoteDesktop.messages.saved'))
        emit('success')
        return
      }
      if (r.kind === 'edit') {
        router.push({ name: 'QuoteList' })
        return
      }
      if (r.back) {
        router.push(r.back)
        return
      }
      if (r.id) router.push({ name: 'QuoteDetail', params: { id: r.id } })
      else router.push({ name: 'QuoteList' })
    },
    errorMessage: (e) => getApiErrorMessage(e, '保存失败')
  })
}

async function handleMarkNoQuote() {
  const itemId = (rfqLink.value.rfqItemId || formData.value.rfqItemId || '').trim()
  if (!itemId) {
    ElMessage.warning(t('quoteDesktop.messages.noRfqItem'))
    return
  }
  try {
    await ElMessageBox.confirm(
      t('quoteDesktop.messages.markNoQuoteConfirm'),
      t('quoteDesktop.actions.markNoQuote'),
      { type: 'warning' }
    )
  } catch {
    return
  }
  markNoQuoteLoading.value = true
  try {
    await rfqApi.markNoQuote(itemId)
    ElMessage.success(t('quoteDesktop.messages.markNoQuoteOk'))
    emit('mark-no-quote')
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, t('quoteDesktop.messages.markNoQuoteFail')))
  } finally {
    markNoQuoteLoading.value = false
  }
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.quote-upsert-page {
  padding: 20px;

  &--embedded {
    padding: 0 4px 12px;
  }
}

.btn-quote-desktop {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 8px 16px 8px 18px;
  margin-right: 8px;
  border: none;
  border-radius: 10px;
  background: #eaf5ff;
  color: #1a2332;
  font-size: 13px;
  font-weight: 500;
  line-height: 1.2;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;

  &:hover {
    background: #ddefff;
    color: #0f172a;
  }

  &__arrow {
    font-size: 14px;
  }
}

.quote-upsert-content {
  min-height: 120px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 20px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 14px;
    min-width: 0;
    flex: 1;
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-shrink: 0;
  }
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

.quote-caption-title-group {
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
    color: rgba(150, 170, 195, 0.82);
  }
}

.title-meta--caption {
  margin-top: 4px;
}

.quote-header-meta-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  min-height: 28px;
}

.quote-caption-meta-text {
  font-size: 13px;
  color: $text-muted;
}

.basic-info-section__body {
  padding: 16px 20px 20px;
}

.link-alert {
  margin-bottom: 16px;

  /* 与 Element Plus 标题区字号一致，整行同一字号 */
  .link-alert-title-row {
    display: flex;
    flex-wrap: wrap;
    align-items: baseline;
    gap: 0;
    line-height: 1.55;
    font-size: inherit;
    font-weight: inherit;
  }

  .la-pre {
    white-space: pre;
    font-size: inherit;
  }

  .la-muted {
    color: rgba(0, 0, 0, 0.55);
    font-size: inherit;
    font-weight: 400;
  }

  .la-strong {
    color: rgba(0, 0, 0, 0.88);
    font-size: inherit;
    font-weight: 600;
    font-family: inherit;
  }

  .la-rfq-val {
    font-size: inherit;
  }

  .la-block-detail {
    font-size: inherit;
    font-family: inherit;
  }

  /* 物料号/品牌/数量/目标价 的数值；与 /rfq-items 采购报价条一致（$color-amber） */
  .la-value-brown {
    color: $color-amber;
    font-size: inherit;
    font-weight: 600;
    font-family: inherit;
  }

  :deep(.el-alert__title) {
    width: 100%;
    font-size: 14px;
    line-height: 1.55;
  }

  :deep(.el-alert__content) {
    width: 100%;
  }

  :deep(.el-alert__description) {
    display: none;
  }
}

.purchase-quote-section {
  .purchase-quote-section__body {
    padding: 20px;
  }

  .purchase-quote-section__body--tab {
    padding: 0;
  }

  .tabs-section {
    background: $layer-2;
    border: none;
    border-radius: 0;
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

  .tabs-body {
    padding: 20px;
  }

  .doc-tab-content {
    min-height: 120px;
  }

  .purchase-quote-section__hint {
    margin: 0 0 12px;
    padding: 0;
    font-size: 12px;
    line-height: 1.45;
    color: $text-muted;
  }

  .detail-items-table-wrap {
    margin-top: 0;
  }

  // §7.4 面板列表：表头/表体基线见 detail-panel-list-table.scss；此处仅页内扩展
  .detail-items-table-wrap :deep(.items-table),
  .detail-items-table-wrap :deep(.crm-items-table.detail-panel-list-table) {
    --el-table-border-color: transparent;
    --el-table-fixed-box-shadow: none;
    background: transparent !important;
    border-radius: 0;
    border: none;
    min-height: 0;
    overflow: visible;

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
      .cell {
        white-space: nowrap;
      }
    }
  }

  .tier-converted-display {
    display: inline-block;
    width: 100%;
    font-variant-numeric: tabular-nums;
    color: $text-primary;
    font-size: 13px;
    font-weight: 600;
  }

  .tier-actions {
    display: inline-flex;
    align-items: center;
    justify-content: flex-end;
    gap: 2px;
    width: 100%;
  }

  :deep(.tier-col-price-ccy .cell) {
    overflow: visible;
  }

  .tier-price-ccy-input {
    width: 100%;
  }
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
  justify-content: space-between;
  gap: 16px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: var(--crm-detail-section-header-bg);
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

.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-primary;
}

.upsert-form {
  :deep(.el-input__inner) {
    background: rgba(255, 255, 255, 0.03);
    border-color: rgba(0, 212, 255, 0.2);
    color: #e8f4ff;
  }

  :deep(.el-form-item__label) {
    color: rgba(200, 216, 232, 0.7);
  }

  .q-select {
    :deep(.el-select__wrapper) {
      background: rgba(255, 255, 255, 0.03);
      box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.2);
    }
    :deep(.el-select__placeholder) {
      color: rgba(200, 216, 232, 0.45);
    }
    :deep(.el-select__selected-item) {
      color: #e8f4ff;
    }
  }

  .q-date {
    :deep(.el-input__wrapper) {
      background: rgba(255, 255, 255, 0.03);
      box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.2);
    }
  }

  .quote-quad-row,
  .quote-triple-row {
    :deep(.el-col) {
      min-width: 0;
    }
  }

  .quote-remark-item {
    margin-bottom: 0;
  }

  .seg-group {
    flex-wrap: wrap;
    :deep(.el-radio-button__inner) {
      background: $layer-3;
      border-color: $border-panel;
      color: $text-secondary;
      font-size: 12px;
      padding: 5px 10px;
      box-shadow: none;
    }
    :deep(.el-radio-button.is-active .el-radio-button__inner),
    :deep(.el-radio-button__original-radio:checked + .el-radio-button__inner) {
      background: var(--crm-accent-012);
      border-color: var(--crm-accent-045);
      color: $cyan-primary;
      box-shadow: none;
    }
  }
}

.vendor-search-hint {
  padding: 8px 12px;
  font-size: 13px;
  color: rgba(200, 216, 232, 0.55);
  text-align: center;
}
</style>
