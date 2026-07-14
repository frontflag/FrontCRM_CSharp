<template>
  <div class="rfq-upsert-page">
    <!-- CaptionBar（《业务详情页面规范》§3 单据类） -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="handleBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          返回列表
        </button>
        <div class="rfq-caption-title-group">
          <div class="caption-avatar-lg">{{ captionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title">
                  <template v-if="isEditMode && formData.rfqCode">需求 {{ formData.rfqCode }}</template>
                  <template v-else>新建需求</template>
                </h1>
              </div>
            </div>
            <div class="title-meta title-meta--caption rfq-header-meta-row">
              <el-tag effect="dark" :type="isEditMode ? 'warning' : 'primary'" size="small">
                {{ isEditMode ? '编辑' : '新建' }}
              </el-tag>
              <span v-if="formData.rfqCode" class="rfq-caption-meta-text">单号 {{ formData.rfqCode }}</span>
            </div>
          </div>
        </div>
      </div>
      <div class="header-right">
        <el-button v-if="!isEditMode" @click="saveDraftOnly">保存草稿</el-button>
        <el-button @click="handleBack">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          <el-icon><Check /></el-icon> {{ isEditMode ? '保存修改' : '保存' }}
        </el-button>
      </div>
    </div>

    <div
      class="rfq-upsert-content"
      v-loading="pageLoading"
      element-loading-background="rgba(10,22,40,0.8)"
    >
    <el-form ref="formRef" :model="formData" :rules="formRules" label-width="108px" class="upsert-form">

      <!-- 基础信息 -->
      <div class="info-section basic-info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">基础信息</span>
          </div>
          <div class="section-header__meta">
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">业务员</span>
              <span class="section-header-meta-item__value">{{ formData.salesUserName || authStore.user?.userName || '—' }}</span>
            </span>
          </div>
        </div>
        <div class="basic-info-section__body">
        <el-row :gutter="12" class="rfq-basic-triple-row">
          <el-col :span="8">
            <el-form-item label="客户" prop="customerId">
              <el-select
                ref="customerSelectRef"
                v-model="formData.customerId"
                placeholder="请输入客户名称搜索"
                style="width: 100%"
                filterable
                :filter-method="onCustomerFilterInput"
                :loading="customerSearchLoading"
                loading-text="搜索中..."
                class="q-select"
              >
                <template #empty>
                  <div class="customer-search-hint">
                    <span>请输入内容之后选择</span>
                  </div>
                </template>
                <el-option
                  v-for="c in customerOptions"
                  :key="c.value"
                  :label="c.label"
                  :value="c.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="客户联系人">
              <el-select
                v-model="formData.contactId"
                :placeholder="contactSelectPlaceholder"
                clearable
                filterable
                style="width: 100%"
                class="q-select"
                :disabled="!formData.customerId"
                @change="onContactChange"
              >
                <el-option
                  v-for="c in contactOptions"
                  :key="c.value"
                  :label="c.label"
                  :value="c.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="联系人邮箱">
              <el-input v-model="formData.contactEmail" placeholder="选择联系人可自动带出，也可手填" class="q-input" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="12" class="rfq-basic-triple-row">
          <el-col :span="8">
            <el-form-item label="业务员">
              <SalesUserCascader
                v-model="formData.salesUserId"
                placeholder="请选择业务员"
                class="q-input"
                @change="onRfqCreateSalesUserChange"
              />
            </el-form-item>
          </el-col>
        </el-row>
        </div>
      </div>

      <!-- 需求信息 -->
      <div class="info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">需求信息</span>
          </div>
        </div>
        <div class="info-section__body">
        <el-row :gutter="24">
          <el-col :span="8">
            <el-form-item label="需求类型" prop="rfqType">
              <el-select
                v-model="formData.rfqType"
                placeholder="请选择需求类型"
                style="width: 100%"
                class="q-select"
              >
                <el-option v-for="o in RFQ_TYPE_OPTIONS" :key="o.value" :label="o.label" :value="o.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="目标类型">
              <el-select v-model="formData.targetType" style="width: 100%" class="q-select">
                <el-option label="比价需求" :value="1" />
                <el-option label="独家需求" :value="2" />
                <el-option label="紧急需求" :value="3" />
                <el-option label="常规需求" :value="4" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item prop="quoteMethod">
              <template #label>
                <span class="rfq-field-label">
                  报价方式
                  <el-tooltip content="选择报价结果与通知的触达方式（系统推送 / 邮件 / 短信等）" placement="top">
                    <el-icon class="rfq-label-help" aria-hidden="true"><QuestionFilled /></el-icon>
                  </el-tooltip>
                </span>
              </template>
              <el-select
                v-model="formData.quoteMethod"
                placeholder="请选择报价方式"
                style="width: 100%"
                class="q-select"
              >
                <el-option v-for="o in QUOTE_METHOD_OPTIONS" :key="o.value" :label="o.label" :value="o.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="8">
            <el-form-item label="分配方式" prop="assignMethod">
              <el-select
                v-model="formData.assignMethod"
                placeholder="请选择分配方式"
                style="width: 100%"
                class="q-select"
                popper-class="rfq-assign-method-select-popper"
              >
                <el-option v-for="o in ASSIGN_METHOD_OPTIONS" :key="o.value" :label="o.label" :value="o.value">
                  <span class="assign-method-option">
                    <span class="assign-method-option-label">{{ o.label }}</span>
                    <el-tooltip :content="o.tip" placement="top" :hide-after="0">
                      <el-icon class="assign-method-option-tip" aria-label="说明" @click.stop>
                        <QuestionFilled />
                      </el-icon>
                    </el-tooltip>
                  </span>
                </el-option>
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="行业">
              <el-select
                v-model="formData.industry"
                placeholder="请选择或输入行业"
                style="width: 100%"
                class="q-select"
                filterable
                allow-create
                default-first-option
                clearable
              >
                <el-option
                  v-for="opt in customerDict.industryOptions"
                  :key="opt.value"
                  :label="opt.label"
                  :value="opt.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="产品">
              <el-input v-model="formData.product" placeholder="请输入产品" class="q-input" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24" class="rfq-row-bg-comp-importance">
          <el-col :span="8">
            <el-form-item label="背景">
              <el-input v-model="formData.projectBackground" type="textarea" :rows="2" placeholder="请输入背景" class="q-input" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="竞争对手">
              <el-input v-model="formData.competitor" placeholder="请输入竞争对手" class="q-input" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="重要程度" class="importance-inline-item">
              <!-- 重要程度仅 1–3 星（与列表、存盘一致）；勿改大 max，否则与后端 short 语义不一致 -->
              <el-rate
                v-model="formData.importance"
                :max="RFQ_IMPORTANCE_RATE_MAX"
                :colors="['#C99A45', '#C99A45', '#C99A45']"
                void-color="rgba(200,216,232,0.2)"
                class="q-rate"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="24">
            <el-form-item label="备注">
              <el-input v-model="formData.remark" type="textarea" :rows="2" placeholder="请输入备注" class="q-input" />
            </el-form-item>
          </el-col>
        </el-row>
        </div>
      </div>

      <!-- 物料明细：面板（默认） / 列表 可切换 -->
      <div class="info-section items-section">
        <div class="section-header section-header--items">
          <div class="section-header__main">
            <div class="section-dot section-dot--amber"></div>
            <span class="section-title">物料明细</span>
            <span class="section-item-count">共 {{ formData.items.length }} 条</span>
          </div>
          <div class="section-header__actions">
            <el-radio-group v-model="materialItemsViewMode" size="small" class="items-view-toggle">
              <el-radio-button label="panel">面板</el-radio-button>
              <el-radio-button label="list">列表</el-radio-button>
            </el-radio-group>
            <el-button type="primary" size="small" class="add-item-btn" @click="addItem">
              <el-icon><Plus /></el-icon> 添加明细
            </el-button>
          </div>
        </div>
        <div class="items-section__body">
        <!-- 面板：每行 4 个字段（span=6） -->
        <div v-if="materialItemsViewMode === 'panel' && formData.items.length > 0" class="items-panel-list">
          <div
            v-for="(row, idx) in formData.items"
            :key="'panel-' + idx"
            class="item-panel-card"
          >
            <div class="item-panel-card__head">
              <span class="item-panel-card__idx">明细 {{ idx + 1 }}</span>
              <el-button link type="danger" @click.stop="removeItem(idx)">删除</el-button>
            </div>
            <el-row :gutter="16" class="item-panel-row">
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">客户物料型号</div>
                  <el-input v-model="row.customerMpn" placeholder="客户物料型号" class="q-input" />
                </div>
              </el-col>
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">客户品牌</div>
                  <el-input v-model="row.customerBrand" placeholder="客户品牌" class="q-input" />
                </div>
              </el-col>
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">物料型号</div>
                  <el-input v-model="row.mpn" placeholder="物料型号(MPN)" class="q-input" />
                </div>
              </el-col>
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">品牌</div>
                  <BizBrandSelect
                    v-model="row.brandId"
                    placeholder="请选择品牌"
                    size="default"
                    @change="(p) => onItemBrandChange(row, p)"
                  />
                  <div v-if="itemNeedsBrandAttention(row)" class="brand-import-hint">
                    导入品牌「{{ row._importBrandText || row.brand }}」未能自动匹配，请手动选择
                  </div>
                </div>
              </el-col>
            </el-row>
            <el-row :gutter="16" class="item-panel-row">
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">目标价 / 币别</div>
                  <SettlementCurrencyAmountInput
                    v-model="row.targetPrice"
                    v-model:currency="row.priceCurrency"
                    :min="0"
                    :precision="6"
                    class="q-number rfq-target-price-ccy"
                  />
                </div>
              </el-col>
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">数量</div>
                  <el-input-number
                    v-model="row.quantity"
                    :min="1"
                    :controls="false"
                    style="width: 100%"
                    class="q-number"
                  />
                </div>
              </el-col>
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">生产日期</div>
                  <MaterialProductionDateSelect v-model="row.productionDate" select-class="q-select" />
                </div>
              </el-col>
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">失效日期</div>
                  <el-date-picker
                    v-model="row.expiryDate"
                    type="date"
                    placeholder="选择日期"
                    value-format="YYYY-MM-DD"
                    style="width: 100%"
                    class="q-date"
                  />
                </div>
              </el-col>
            </el-row>
            <el-row :gutter="16" class="item-panel-row">
              <el-col :span="8">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">最小包装（PCS）</div>
                  <el-input-number
                    v-model="row.minPackageQty"
                    :min="0"
                    :controls="false"
                    style="width: 100%"
                    class="q-number"
                  />
                </div>
              </el-col>
              <el-col :span="8">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">最小起订量（PCS）</div>
                  <el-input-number
                    v-model="row.minOrderQty"
                    :min="0"
                    :controls="false"
                    style="width: 100%"
                    class="q-number"
                  />
                </div>
              </el-col>
              <el-col :span="8">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">可替代料</div>
                  <el-input v-model="row.alternativeMaterials" placeholder="逗号分隔" class="q-input" />
                </div>
              </el-col>
            </el-row>
            <el-row :gutter="16" class="item-panel-row">
              <el-col :span="24">
                <div class="item-panel-field item-panel-field--remark">
                  <div class="item-panel-field__label">备注</div>
                  <el-input
                    v-model="row.remark"
                    type="textarea"
                    :rows="2"
                    placeholder="备注"
                    class="q-input"
                  />
                </div>
              </el-col>
            </el-row>
          </div>
        </div>

        <!-- 列表：横向表格 -->
        <div v-if="materialItemsViewMode === 'list' && formData.items.length > 0" class="items-table-wrap">
          <el-table :data="formData.items" size="small" class="items-table items-table--h-scroll">
            <el-table-column label="客户物料型号" min-width="130">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].customerMpn" placeholder="客户物料型号" class="q-input" />
              </template>
            </el-table-column>
            <el-table-column label="客户品牌" min-width="100">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].customerBrand" placeholder="客户品牌" class="q-input" />
              </template>
            </el-table-column>
            <el-table-column label="物料型号" min-width="140">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].mpn" placeholder="物料型号(MPN)" class="q-input" />
              </template>
            </el-table-column>
            <el-table-column label="品牌" min-width="220" class-name="rfq-table-brand-col">
              <template #default="{ $index }">
                <BizBrandSelect
                  v-model="formData.items[$index].brandId"
                  placeholder="请选择品牌"
                  size="small"
                  @change="(p) => onItemBrandChange(formData.items[$index], p)"
                />
                <div
                  v-if="itemNeedsBrandAttention(formData.items[$index])"
                  class="brand-import-hint brand-import-hint--table"
                >
                  导入「{{ formData.items[$index]._importBrandText || formData.items[$index].brand }}」待选择
                </div>
              </template>
            </el-table-column>
            <el-table-column label="目标价 / 币别" min-width="200" class-name="rfq-table-target-ccy-col">
              <template #default="{ $index }">
                <SettlementCurrencyAmountInput
                  v-model="formData.items[$index].targetPrice"
                  v-model:currency="formData.items[$index].priceCurrency"
                  :min="0"
                  :precision="6"
                  size="small"
                  class="q-number"
                />
              </template>
            </el-table-column>
            <el-table-column label="数量" width="100">
              <template #default="{ $index }">
                <el-input-number
                  v-model="formData.items[$index].quantity"
                  :min="1"
                  :controls="false"
                  style="width: 100%"
                  class="q-number"
                />
              </template>
            </el-table-column>
            <el-table-column label="生产日期" min-width="140">
              <template #default="{ $index }">
                <MaterialProductionDateSelect v-model="formData.items[$index].productionDate" select-class="q-select" />
              </template>
            </el-table-column>
            <el-table-column label="失效日期" width="138">
              <template #default="{ $index }">
                <el-date-picker
                  v-model="formData.items[$index].expiryDate"
                  type="date"
                  placeholder="选择日期"
                  value-format="YYYY-MM-DD"
                  style="width: 100%"
                  class="q-date"
                />
              </template>
            </el-table-column>
            <el-table-column label="最小包装（PCS）" width="100">
              <template #default="{ $index }">
                <el-input-number
                  v-model="formData.items[$index].minPackageQty"
                  :min="0"
                  :controls="false"
                  style="width: 100%"
                  class="q-number"
                />
              </template>
            </el-table-column>
            <el-table-column label="最小起订量（PCS）" width="100">
              <template #default="{ $index }">
                <el-input-number
                  v-model="formData.items[$index].minOrderQty"
                  :min="0"
                  :controls="false"
                  style="width: 100%"
                  class="q-number"
                />
              </template>
            </el-table-column>
            <el-table-column label="可替代料" min-width="120">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].alternativeMaterials" placeholder="逗号分隔" class="q-input" />
              </template>
            </el-table-column>
            <el-table-column label="备注" min-width="220" class-name="rfq-table-remark-col">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].remark" placeholder="备注" class="q-input" />
              </template>
            </el-table-column>
            <el-table-column
              label="操作"
              :width="rfqCreateLineOpColWidth"
              :min-width="rfqCreateLineOpColMinWidth"
              align="center"
              fixed="right"
              class-name="op-col"
              label-class-name="op-col"
            >
              <template #header>
                <div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="rfqCreateLineOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="toggleRfqCreateLineOpCol"
            >
              {{ rfqCreateLineOpColExpanded ? '>' : '<' }}
            </button>
          </div>
              </template>
              <template #default="{ $index }">
                <div @click.stop @dblclick.stop>
                  <div v-if="rfqCreateLineOpColExpanded" class="action-btns">
                    <el-button link type="danger" @click.stop="removeItem($index)">删除</el-button>
                  </div>
                  <el-dropdown v-else trigger="click" placement="bottom-end">
                    <div class="op-more-dropdown-trigger">
                      <button type="button" class="op-more-trigger">...</button>
                    </div>
                    <template #dropdown>
                      <el-dropdown-menu>
                        <el-dropdown-item @click.stop="removeItem($index)">
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
        <div v-if="formData.items.length === 0" class="empty-hint">
          暂无明细，点击「添加明细」添加
        </div>
        </div>
      </div>

    </el-form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElNotification } from 'element-plus'
import { Check, Plus, QuestionFilled } from '@element-plus/icons-vue'
import { rfqApi } from '@/api/rfq'
import { customerApi, customerContactApi } from '@/api/customer'
import { draftApi } from '@/api/draft'
import { consumeAiPrefill } from '@/utils/aiPrefill'
import { markEntityParseSaved } from '@/utils/entityParseLogTrack'
import type { CreateRFQItemRequest, CreateRFQRequest, UpdateRFQRequest } from '@/types/rfq'
import { useAuthStore } from '@/stores/auth'
import { getApiErrorMessage } from '@/utils/apiError'
import { runValidatedFormSave } from '@/composables/useFormSubmit'
import SalesUserCascader from '@/components/SalesUserCascader.vue'
import {
  RFQ_TYPE_OPTIONS,
  QUOTE_METHOD_OPTIONS,
  ASSIGN_METHOD_OPTIONS
} from '@/constants/rfqFormEnums'
import MaterialProductionDateSelect from '@/components/MaterialProductionDateSelect.vue'
import SettlementCurrencyAmountInput from '@/components/SettlementCurrencyAmountInput.vue'
import { DEFAULT_SETTLEMENT_CURRENCY_CODE, normalizeSettlementCurrencyCode } from '@/constants/currency'
import BizBrandSelect from '@/components/Biz/BizBrandSelect.vue'
import { resolveBrandIdsForItems } from '@/utils/bizBrandMatch'
import { useMaterialProductionDateDict } from '@/composables/useMaterialProductionDateDict'
import { useCustomerDictStore } from '@/stores/customerDict'
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const formRef = ref()
const submitLoading = ref(false)
const pageLoading = ref(false)
const authStore = useAuthStore()

let cachedDefaultAssignMethod: number | null = null

async function resolveDefaultAssignMethod(): Promise<number> {
  if (cachedDefaultAssignMethod != null) return cachedDefaultAssignMethod
  try {
    cachedDefaultAssignMethod = await rfqApi.getDefaultAssignMethod()
  } catch {
    cachedDefaultAssignMethod = 5
  }
  return cachedDefaultAssignMethod
}
const { ensureLoaded: ensureMaterialPdDict, defaultCode: defaultProductionDateCode, coerceProductionDateToCode: coercePd } =
  useMaterialProductionDateDict()
const customerDict = useCustomerDictStore()
const currentDraftId = ref('')
/** 已应用的 aiPrefill token，避免 StrictMode 重复 consume / 清 query 后误 reset */
const appliedAiPrefillTokens = new Set<string>()
const aiParseLogId = ref<string | null>(null)
let skipCreateResetOnce = false

function queryToken(v: unknown): string {
  const raw = Array.isArray(v) ? v[0] : v
  return typeof raw === 'string' ? raw.trim() : ''
}

/** 物料明细展示：面板（默认，每行 4 字段） / 列表（横向表格） */
const materialItemsViewMode = ref<'panel' | 'list'>('panel')

/** 《列表操作列规范》：需求明细行 */
const rfqCreateLineOpColExpanded = ref(false)
const RFQ_CREATE_OP_COL_COLLAPSED = 43
const RFQ_CREATE_OP_COL_EXPANDED = 173
const RFQ_CREATE_OP_COL_EXPANDED_MIN = 160
const rfqCreateLineOpColWidth = computed(() =>
  rfqCreateLineOpColExpanded.value ? RFQ_CREATE_OP_COL_EXPANDED : RFQ_CREATE_OP_COL_COLLAPSED
)
const rfqCreateLineOpColMinWidth = computed(() =>
  rfqCreateLineOpColExpanded.value ? RFQ_CREATE_OP_COL_EXPANDED_MIN : RFQ_CREATE_OP_COL_COLLAPSED
)
function toggleRfqCreateLineOpCol() {
  rfqCreateLineOpColExpanded.value = !rfqCreateLineOpColExpanded.value
}

const rfqId = computed(() => {
  const id = route.params.id
  if (Array.isArray(id)) return id[0] || ''
  return String(id || '')
})

const isEditMode = computed(() => route.name === 'RFQEdit' && !!rfqId.value)

const captionAvatarChar = computed(() => {
  const code = String(formData.value.rfqCode ?? '').trim()
  if (code) return code.charAt(0).toUpperCase()
  return 'R'
})

function handleBack() {
  router.push({ name: 'RFQList' })
}

// 客户下拉搜索
const customerOptions = ref<{ value: string; label: string }[]>([])
const customerSearchLoading = ref(false)
const customerSelectRef = ref<any>(null)
let customerSearchTimer: ReturnType<typeof setTimeout> | null = null

const contactOptions = ref<
  { value: string; label: string; email?: string; isDefault?: boolean }[]
>([])

const contactSelectPlaceholder = computed(() =>
  formData.value.customerId ? '请选择联系人' : '请先选择客户'
)

function contactEmailFromRaw(c: Record<string, unknown>): string {
  const v = c.email ?? c.Email
  return typeof v === 'string' ? v.trim() : ''
}

/** 从客户主档带出行业（新建客户在客户资料中维护的行业） */
function pickIndustryFromCustomerRecord(c: Record<string, unknown>): string {
  const raw = c.industry ?? c.Industry
  return typeof raw === 'string' ? raw.trim() : raw != null ? String(raw).trim() : ''
}

async function applyIndustryFromCustomer(customerId: string) {
  const id = customerId?.trim()
  if (!id) {
    formData.value.industry = ''
    return
  }
  try {
    const c = await customerApi.getCustomerById(id)
    const ext = c as unknown as Record<string, unknown>
    const fromCustomer = pickIndustryFromCustomerRecord(ext)
    formData.value.industry = await customerDict.resolveIndustryStorageLabel(fromCustomer || undefined)
  } catch {
    /* 接口失败时不覆盖用户已填行业 */
  }
}

/** 拉取联系人；切换客户时由 watch(customerId) 触发，保证与 el-select 同步 */
async function loadContactsForCustomer(customerId: string) {
  if (!customerId) {
    contactOptions.value = []
    return
  }
  try {
    const list = await customerContactApi.getContactsByCustomerId(customerId)
    const rows = Array.isArray(list) ? list : []
    contactOptions.value = rows
      .map((c: any) => {
        const raw = c as Record<string, unknown>
        const id = String(c.id ?? raw.contactId ?? '').trim()
        if (!id) return null
        const email = contactEmailFromRaw(raw)
        return {
          value: id,
          label: String(c.contactName ?? c.name ?? '联系人'),
          email: email || undefined,
          isDefault: !!(c.isDefault ?? c.isMain)
        }
      })
      .filter(Boolean) as { value: string; label: string; email?: string; isDefault?: boolean }[]
  } catch {
    contactOptions.value = []
  }
}

/** 未选手动联系人时，默认选主联系人/第一条并带出邮箱 */
function applyDefaultContactAndEmail() {
  if (formData.value.contactId) return
  const opts = contactOptions.value
  if (!opts.length) return
  const preferred = opts.find((o) => o.isDefault) || opts[0]
  if (!preferred) return
  formData.value.contactId = preferred.value
  if (preferred.email) {
    formData.value.contactEmail = preferred.email
  }
}

// 生成需求编号
const getYYMMDD = (d: Date) => {
  const yy = String(d.getFullYear()).slice(-2)
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${yy}${mm}${dd}`
}

const genRfqCode = () => {
  const date = getYYMMDD(new Date())
  const seq = String(Math.floor(Math.random() * 10000)).padStart(4, '0')
  return `RFQ${date}${seq}`
}

/** 一条空白物料明细（新建默认 1 条，与「添加明细」结构一致） */
function createEmptyRfqItem() {
  return {
    customerMpn: '',
    customerBrand: '',
    mpn: '',
    brand: '',
    brandId: undefined as number | undefined,
    quantity: 1,
    targetPrice: undefined,
    productionDate: defaultProductionDateCode(),
    expiryDate: '',
    minPackageQty: undefined,
    minOrderQty: undefined,
    alternativeMaterials: '',
    remark: '',
    priceCurrency: DEFAULT_SETTLEMENT_CURRENCY_CODE
  }
}

const emptyForm = () => ({
  rfqCode: genRfqCode(),
  customerId: '',
  customerName: '',
  contactId: '' as string,
  salesUserId: '',
  salesUserName: '',
  contactEmail: '',
  product: '',
  industry: '',
  rfqType: 1,
  targetType: 1,
  quoteMethod: 2,
  assignMethod: 5,
  importance: 1,
  projectBackground: '',
  competitor: '',
  remark: '',
  items: [] as any[]
})

const formData = ref(emptyForm())

async function resetFormForCreate() {
  const defaultAssignMethod = await resolveDefaultAssignMethod()
  formData.value = {
    ...emptyForm(),
    assignMethod: defaultAssignMethod,
    items: [createEmptyRfqItem()]
  }
  contactOptions.value = []
  const user = authStore.user
  if (user) {
    formData.value.salesUserId = user.id || ''
    formData.value.salesUserName = user.userName || ''
  }
}

/** 从路由 query（如客户详情「创建需求」）预填客户下拉与 customerId */
async function applyPrefillCustomerFromQuery() {
  const raw = route.query.customerId
  const cid = Array.isArray(raw) ? raw[0] : raw
  if (!cid || typeof cid !== 'string') return
  try {
    const c = await customerApi.getCustomerById(cid)
    const name =
      c.customerName ||
      (c as any).officialName ||
      c.customerShortName ||
      (c as any).nickName ||
      c.customerCode ||
      '客户'
    const id = String(c.id)
    customerOptions.value = [{ value: id, label: name }]
    formData.value.customerId = id
    formData.value.customerName = name
    const ext = c as unknown as Record<string, unknown>
    const fromCustomer = pickIndustryFromCustomerRecord(ext)
    formData.value.industry = await customerDict.resolveIndustryStorageLabel(fromCustomer || undefined)
  } catch {
    ElMessage.warning('无法加载预选客户，请在「客户」中搜索选择')
  }
}

function buildDraftPayload() {
  return {
    ...formData.value,
    items: formData.value.items.map((it: any) => ({ ...it }))
  }
}

async function applyDraftPayload(payload: Record<string, unknown>) {
  const p = payload || {}
  if (typeof p.rfqCode === 'string' && p.rfqCode) {
    formData.value.rfqCode = p.rfqCode
  }
  formData.value.customerId = String(p.customerId || '')
  formData.value.customerName = String(p.customerName || '')
  formData.value.contactId = String(p.contactId || '')
  formData.value.contactEmail = String(p.contactEmail || '')
  formData.value.salesUserId = String(p.salesUserId || formData.value.salesUserId || '')
  formData.value.salesUserName = String(p.salesUserName || formData.value.salesUserName || '')
  formData.value.product = String(p.product || '')
  formData.value.projectBackground = String(p.projectBackground || '')
  formData.value.competitor = String(p.competitor || '')
  formData.value.remark = String(p.remark || '')
  if (p.rfqType != null) formData.value.rfqType = Number(p.rfqType)
  if (p.targetType != null) formData.value.targetType = Number(p.targetType)
  if (p.quoteMethod != null) formData.value.quoteMethod = Number(p.quoteMethod)
  if (p.assignMethod != null) formData.value.assignMethod = Number(p.assignMethod)
  if (p.importance != null) formData.value.importance = normalizeImportance(p.importance)
  formData.value.industry = await customerDict.resolveIndustryStorageLabel(String(p.industry || ''))
  const items = Array.isArray(p.items) ? p.items : []
  formData.value.items = items.length
    ? items.map((raw: any) => ({
        ...createEmptyRfqItem(),
        ...raw,
        quantity: raw.quantity != null && Number(raw.quantity) > 0 ? Number(raw.quantity) : 1
      }))
    : [createEmptyRfqItem()]
  if (formData.value.customerId) {
    const label = formData.value.customerName || '客户'
    if (!customerOptions.value.some((o) => o.value === formData.value.customerId)) {
      customerOptions.value = [{ value: formData.value.customerId, label }]
    }
    await loadContactsForCustomer(formData.value.customerId)
    applyDefaultContactAndEmail()
  }
  // Excel 导入第二步已做品牌匹配与提示，进入新建页不再重复弹 Toast
  if (p._prefillSource !== 'excel-import') {
    await resolveBrandIdsForItems(formData.value.items, {
      onWarning: (msg) => ElMessage.warning(msg)
    })
    clearResolvedImportBrandHints(formData.value.items)
  }
}

async function restoreDraftById(draftId: string) {
  const draft = await draftApi.getDraftById(draftId)
  if (draft.entityType !== 'RFQ') throw new Error('该草稿不是 RFQ 类型')
  await applyDraftPayload(JSON.parse(draft.payloadJson || '{}'))
  currentDraftId.value = draft.draftId
}

async function saveDraftOnly() {
  try {
    const draft = await draftApi.saveDraft({
      draftId: currentDraftId.value || undefined,
      entityType: 'RFQ',
      draftName: formData.value.product || formData.value.rfqCode || 'RFQ草稿',
      payloadJson: JSON.stringify(buildDraftPayload())
    })
    currentDraftId.value = draft.draftId
    ElNotification.success({ title: '保存成功', message: `草稿已保存（${draft.draftId}）` })
  } catch (err: unknown) {
    ElNotification.error({
      title: '保存失败',
      message: getApiErrorMessage(err, '草稿保存失败')
    })
  }
}

/** 新建/编辑页「重要程度」星级上限（Element Plus el-rate 的 max） */
const RFQ_IMPORTANCE_RATE_MAX = 3

/** 重要程度：界面为 1–3 星；兼容历史 1–5 星或约 1–10 的存盘值 */
function normalizeImportance(v: unknown): number {
  const n = Number(v)
  if (!Number.isFinite(n) || n < 1) return 1
  if (n <= 3) return Math.round(n)
  if (n <= 5) return Math.min(3, Math.max(1, Math.round(n)))
  return Math.max(1, Math.min(3, Math.round((n * 3) / 10)))
}

function mapCurrencyToPriceCurrency(c?: string | number): number {
  return normalizeSettlementCurrencyCode(c)
}

function formatExpiryForPicker(v: unknown): string {
  if (v == null || v === '') return ''
  if (typeof v === 'string') return v.length >= 10 ? v.slice(0, 10) : v
  const d = v as Date
  if (d instanceof Date && !Number.isNaN(d.getTime())) {
    const y = d.getFullYear()
    const m = String(d.getMonth() + 1).padStart(2, '0')
    const day = String(d.getDate()).padStart(2, '0')
    return `${y}-${m}-${day}`
  }
  return ''
}

function mapItemsFromApi(items: any[]) {
  return items.map((raw: any) => ({
    id: raw.id || raw.Id || undefined,
    customerMpn: raw.customerMpn || raw.customerMaterialModel || '',
    customerBrand: raw.customerBrand || '',
    mpn: raw.mpn || raw.materialModel || '',
    brand: raw.brand || '',
    brandId:
      raw.brandId != null || raw.BrandId != null
        ? Number(raw.brandId ?? raw.BrandId)
        : undefined,
    quantity: raw.quantity ?? 1,
    targetPrice: raw.targetPrice,
    productionDate: coercePd(raw.productionDate || ''),
    expiryDate: formatExpiryForPicker(raw.expiryDate),
    minPackageQty: raw.minPackageQty != null ? Number(raw.minPackageQty) : undefined,
    minOrderQty: raw.moq != null ? Number(raw.moq) : raw.minOrderQty != null ? Number(raw.minOrderQty) : undefined,
    alternativeMaterials: raw.alternatives || raw.alternativeMaterials || '',
    remark: raw.remark || '',
    priceCurrency: mapCurrencyToPriceCurrency(raw.priceCurrency ?? raw.currency)
  }))
}

async function loadRfqForEdit() {
  if (!isEditMode.value || !rfqId.value) return
  pageLoading.value = true
  try {
    const data = await rfqApi.getRFQById(rfqId.value)
    const d = data as any
    if (data.customerId) {
      customerOptions.value = [
        { value: data.customerId, label: data.customerName || d.customerName || '客户' }
      ]
    } else {
      customerOptions.value = []
    }
    await loadContactsForCustomer(data.customerId || '')
    formData.value = {
      rfqCode: data.rfqCode || '',
      customerId: data.customerId || '',
      customerName: data.customerName || '',
      contactId: (d.contactId || d.contactPersonId || '') as string,
      salesUserId: data.salesUserId || '',
      salesUserName: data.salesUserName || '',
      contactEmail: d.contactEmail || d.contactPersonEmail || '',
      product: data.product || '',
      industry: '',
      rfqType: data.rfqType ?? 1,
      targetType: data.targetType ?? 1,
      quoteMethod: d.quoteMethod ?? 2,
      assignMethod: data.assignMethod ?? 5,
      importance: normalizeImportance(d.importanceLevel ?? d.importance),
      projectBackground: data.projectBackground || '',
      competitor: data.competitor || '',
      remark: data.remark || '',
      items: data.items?.length ? mapItemsFromApi(data.items) : []
    }
    await resolveBrandIdsForItems(formData.value.items, {
      onWarning: (msg) => ElMessage.warning(msg)
    })
    clearResolvedImportBrandHints(formData.value.items)
    formData.value.industry = await customerDict.resolveIndustryStorageLabel(data.industry || '')
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '加载需求失败'))
    router.push({ name: 'RFQList' })
  } finally {
    pageLoading.value = false
  }
}

watch(
  () =>
    [route.name, route.params.id, route.query.customerId, route.query.draftId, route.query.aiPrefill] as const,
  async () => {
    await ensureMaterialPdDict()
    await customerDict.ensureLoaded()
    if (route.name === 'RFQEdit' && rfqId.value) {
      currentDraftId.value = ''
      await loadRfqForEdit()
      return
    }
    if (route.name !== 'RFQCreate') return

    const draftId = queryToken(route.query.draftId)
    const aiToken = queryToken(route.query.aiPrefill)

    if (draftId) {
      await resetFormForCreate()
      currentDraftId.value = ''
      try {
        await restoreDraftById(draftId)
      } catch (err: unknown) {
        ElNotification.error({
          title: '恢复失败',
          message: getApiErrorMessage(err, '草稿恢复失败')
        })
      }
      return
    }

    if (aiToken) {
      if (!appliedAiPrefillTokens.has(aiToken)) {
        await resetFormForCreate()
        currentDraftId.value = ''
        const consumed = consumeAiPrefill('RFQ', aiToken)
        if (consumed) {
          aiParseLogId.value = consumed.parseLogId
          await applyDraftPayload(consumed.payload)
          appliedAiPrefillTokens.add(aiToken)
        } else {
          ElMessage.warning('预填数据已失效，请重新发起 AI 创建')
        }
      }
      if (route.query.aiPrefill) {
        skipCreateResetOnce = true
        const q = { ...route.query }
        delete q.aiPrefill
        await router.replace({ query: q })
      }
      return
    }

    if (skipCreateResetOnce) {
      skipCreateResetOnce = false
      return
    }

    await resetFormForCreate()
    currentDraftId.value = ''
    await applyPrefillCustomerFromQuery()
  },
  { immediate: true }
)

watch(
  () => formData.value.customerId,
  async (id, oldId) => {
    if (!id) {
      contactOptions.value = []
      formData.value.contactId = ''
      formData.value.contactEmail = ''
      formData.value.industry = ''
      return
    }
    if (oldId && oldId !== id) {
      formData.value.contactId = ''
      formData.value.contactEmail = ''
    }
    const found = customerOptions.value.find((c) => c.value === id)
    if (found) {
      formData.value.customerName = found.label
    }
    await Promise.all([loadContactsForCustomer(id), applyIndustryFromCustomer(id)])
    applyDefaultContactAndEmail()
  }
)

const formRules = {
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }],
  rfqType: [{ required: true, message: '请选择需求类型', trigger: 'change' }],
  quoteMethod: [{ required: true, message: '请选择报价方式', trigger: 'change' }],
  assignMethod: [{ required: true, message: '请选择分配方式', trigger: 'change' }]
}

function onRfqCreateSalesUserChange(p: { id: string; label: string }) {
  formData.value.salesUserName = p.label || ''
}

// 客户搜索防抖
async function onCustomerFilterInput(query: string) {
  if (customerSearchTimer) clearTimeout(customerSearchTimer)
  if (!query || query.trim().length < 1) {
    customerOptions.value = []
    return
  }
  customerSearchTimer = setTimeout(async () => {
    customerSearchLoading.value = true
    try {
      const { customerApi } = await import('@/api/customer')
      const res = await customerApi.searchCustomers({
        pageNumber: 1,
        pageSize: 30,
        searchTerm: query.trim()
      })
      customerOptions.value = (res.items || []).map((c: any) => ({
        value: c.id,
        label: c.customerName || (c as any).officialName || c.name || '未知客户'
      }))
    } catch {
      customerOptions.value = []
    } finally {
      customerSearchLoading.value = false
    }
  }, 300)
}

function onContactChange(contactId: string | null | undefined) {
  if (!contactId) {
    formData.value.contactEmail = ''
    return
  }
  const row = contactOptions.value.find((c) => c.value === contactId)
  if (row?.email) {
    formData.value.contactEmail = row.email
  }
}

// 添加/删除明细
const addItem = () => {
  formData.value.items.push(createEmptyRfqItem())
}

const removeItem = (index: number) => {
  formData.value.items.splice(index, 1)
}

function itemNeedsBrandAttention(row: {
  brand?: string
  brandId?: number
  _importBrandText?: string
}): boolean {
  const hasId = row.brandId != null && row.brandId > 0
  const text = (row._importBrandText || row.brand || '').trim()
  return !hasId && !!text
}

function clearResolvedImportBrandHints(
  items: Array<{ brandId?: number; _importBrandText?: string }>
) {
  for (const it of items) {
    if (it.brandId && it.brandId > 0) it._importBrandText = undefined
  }
}

function onItemBrandChange(
  row: { brand?: string; brandId?: number; _importBrandText?: string },
  payload: { id: number; standardBrand: string }
) {
  if (payload.id > 0) {
    row.brand = (payload.standardBrand || '').trim()
    row._importBrandText = undefined
  } else {
    row.brand = ''
    row.brandId = undefined
  }
}

function validateItemsBrand(): boolean {
  if (!formData.value.items.length) {
    ElMessage.warning('请至少添加一条物料明细')
    return false
  }
  for (let i = 0; i < formData.value.items.length; i++) {
    const it = formData.value.items[i]
    if (!it.brandId || it.brandId <= 0) {
      ElMessage.warning(`明细 ${i + 1}：请选择供应品牌`)
      return false
    }
  }
  return true
}

function buildItemPayload(): CreateRFQItemRequest[] {
  return formData.value.items.map((it: any, idx: number) => {
    const qty = Math.max(1, Number(it.quantity) || 1)
    const moq =
      it.minOrderQty != null && it.minOrderQty !== ''
        ? Number(it.minOrderQty)
        : undefined
    const minPkg =
      it.minPackageQty != null && it.minPackageQty !== ''
        ? Number(it.minPackageQty)
        : undefined
    const expiryRaw = it.expiryDate
    const expiryDate =
      expiryRaw && typeof expiryRaw === 'string'
        ? expiryRaw
        : undefined
    const lineId = (it.id || it.Id || '').trim()
    return {
      ...(lineId ? { id: lineId } : {}),
      lineNo: idx + 1,
      customerMpn: (it.customerMpn || '').trim() || undefined,
      mpn: (it.mpn || '').trim(),
      customerBrand: (it.customerBrand || '').trim(),
      brand: (it.brand || '').trim(),
      brandId: it.brandId != null && it.brandId > 0 ? Number(it.brandId) : undefined,
      targetPrice: it.targetPrice != null ? Number(it.targetPrice) : undefined,
      priceCurrency: Number(it.priceCurrency) || DEFAULT_SETTLEMENT_CURRENCY_CODE,
      quantity: qty,
      productionDate: (it.productionDate || '').trim() || undefined,
      expiryDate,
      minPackageQty: minPkg,
      moq: moq,
      alternatives: (it.alternativeMaterials || '').trim() || undefined,
      remark: (it.remark || '').trim() || undefined
    } as CreateRFQItemRequest
  })
}

// 提交
const handleSubmit = async () => {
  if (!validateItemsBrand()) return
  const editMode = isEditMode.value
  const id = rfqId.value
  await runValidatedFormSave(formRef, {
    loading: submitLoading,
    task: async () => {
      if (editMode && id) {
        const payload: UpdateRFQRequest = {
          customerId: formData.value.customerId,
          contactId: formData.value.contactId || undefined,
          contactEmail: formData.value.contactEmail,
          salesUserId: formData.value.salesUserId,
          industry: formData.value.industry,
          product: formData.value.product,
          rfqType: formData.value.rfqType,
          targetType: formData.value.targetType,
          quoteMethod: formData.value.quoteMethod,
          assignMethod: formData.value.assignMethod,
          importance: normalizeImportance(formData.value.importance),
          projectBackground: formData.value.projectBackground,
          competitor: formData.value.competitor,
          remark: formData.value.remark,
          items: buildItemPayload()
        }
        await rfqApi.updateRFQ(id, payload)
        return { mode: 'edit' as const, rfqId: id }
      }
      const createPayload: CreateRFQRequest = {
        customerId: formData.value.customerId,
        contactId: formData.value.contactId || undefined,
        contactEmail: formData.value.contactEmail,
        salesUserId: formData.value.salesUserId,
        rfqType: formData.value.rfqType,
        quoteMethod: formData.value.quoteMethod,
        assignMethod: formData.value.assignMethod,
        industry: formData.value.industry,
        product: formData.value.product,
        targetType: formData.value.targetType,
        importance: normalizeImportance(formData.value.importance),
        projectBackground: formData.value.projectBackground,
        competitor: formData.value.competitor,
        remark: formData.value.remark,
        items: buildItemPayload()
      }
      const created = await rfqApi.createRFQ(createPayload)
      return { mode: 'create' as const, rfqId: created?.id || '' }
    },
    formatSuccess: (result) => (result.mode === 'edit' ? '需求已更新' : '需求创建成功'),
    onSuccess: (result) => {
      if (result.mode === 'create' && result.rfqId) {
        markEntityParseSaved(aiParseLogId.value, result.rfqId)
        aiParseLogId.value = null
      }
      router.push({ name: 'RFQList' })
    },
    errorMessage: (e) => getApiErrorMessage(e, editMode ? '保存失败，请重试' : '创建失败，请重试')
  })
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.rfq-upsert-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.rfq-upsert-content {
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

.rfq-caption-title-group {
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
}

.title-meta--caption {
  margin-top: 4px;
}

.rfq-header-meta-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  min-height: 28px;
}

.rfq-caption-meta-text {
  font-size: 13px;
  color: $text-muted;
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

  &--items {
    flex-wrap: wrap;
  }
}

.section-header__main {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.section-header__meta,
.section-header__actions {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
  margin-left: auto;
  flex-wrap: wrap;
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

  &--amber {
    background: $color-amber;
    box-shadow: 0 0 6px rgba(201, 154, 69, 0.5);
  }
}

.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-primary;
}

.section-item-count {
  font-size: 12px;
  font-weight: 400;
  color: $text-muted;
  white-space: nowrap;
}

.basic-info-section__body,
.info-section__body,
.items-section__body {
  padding: 16px 20px 20px;
}

.upsert-form {
  .rfq-basic-triple-row {
    :deep(.el-col) {
      min-width: 0;
    }
  }

  .items-view-toggle {
    :deep(.el-radio-button__inner) {
      background: $layer-3;
      border-color: $border-panel;
      color: $text-muted;
      font-size: 12px;
      padding: 5px 12px;
    }
    :deep(.el-radio-button__original-radio:checked + .el-radio-button__inner) {
      background: rgba(0, 212, 255, 0.12);
      border-color: rgba(0, 212, 255, 0.45);
      color: $cyan-primary;
      box-shadow: none;
    }
  }

  :deep(.el-form-item__label) {
    color: $text-muted;
    font-size: 13px;
  }

  .rfq-field-label {
    display: inline-flex;
    align-items: center;
    gap: 4px;
  }
  .rfq-label-help {
    font-size: 14px;
    color: #eab308;
    cursor: help;
    vertical-align: middle;
  }

  .assign-method-option {
    display: flex;
    align-items: center;
    width: 100%;
    min-width: 0;
    gap: 8px;
  }
  .assign-method-option-label {
    flex: 1;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .assign-method-option-tip {
    margin-left: auto;
    font-size: 14px;
    color: #eab308;
    cursor: help;
    flex-shrink: 0;
  }

  .rfq-row-bg-comp-importance {
    align-items: flex-start;
    .importance-inline-item :deep(.el-form-item__content) {
      padding-top: 6px;
    }
  }
}

// 输入框统一暗色风格（参考 CustomerEdit.vue）
.q-input {
  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    color: $text-primary !important;
    transition: border-color 0.2s;
    &:hover { border-color: rgba(0, 212, 255, 0.25) !important; }
    &.is-focus { border-color: rgba(0, 212, 255, 0.5) !important; box-shadow: 0 0 0 2px rgba(0,212,255,0.08) !important; }
  }
  :deep(.el-input__inner) {
    color: $text-primary !important;
    background: transparent !important;
    font-size: 13px;
    &::placeholder { color: $text-placeholder !important; }
  }
  :deep(.el-textarea__inner) {
    font-size: 13px;
    &::placeholder { color: $text-placeholder !important; }
  }
  :deep(.el-input__wrapper.is-disabled) {
    opacity: 0.5;
  }
}

.q-select {
  :deep(.el-select__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    color: $text-primary !important;
    &.is-focused { border-color: rgba(0, 212, 255, 0.5) !important; }
  }
  :deep(.el-select__placeholder) { color: $text-placeholder !important; }
  :deep(.el-select__selected-item) { color: $text-primary !important; }
}

.q-number {
  :deep(.el-input__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    &.is-focus { border-color: rgba(0, 212, 255, 0.5) !important; }
  }
  :deep(.el-input__inner) {
    color: $text-primary !important;
    background: transparent !important;
  }
}

.q-rate {
  :deep(.el-rate__icon) {
    font-size: 20px;
  }
}

// 客户搜索提示
.customer-search-hint {
  padding: 8px 12px;
  color: $text-muted;
  font-size: 12px;
  text-align: center;
}

.items-panel-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.item-panel-card {
  background: rgba(0, 212, 255, 0.028);
  border: 1px solid rgba(0, 212, 255, 0.14);
  border-radius: $border-radius-md;
  padding: 14px 16px 16px;
}

.item-panel-card__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.item-panel-card__idx {
  font-size: 12px;
  font-weight: 600;
  color: rgba(200, 216, 232, 0.75);
  letter-spacing: 0.3px;
}

.item-panel-row {
  margin-bottom: 4px;
  &:last-child {
    margin-bottom: 0;
  }
}

.item-panel-field {
  margin-bottom: 10px;
  min-width: 0;
}

.item-panel-field__label {
  font-size: 12px;
  color: $text-muted;
  margin-bottom: 4px;
  line-height: 1.3;
}

.rfq-target-price-ccy {
  width: 100%;
}

.items-table-wrap {
  width: 100%;
  overflow-x: auto;
  overflow-y: hidden;
}

// 列表模式：列总宽超出容器时出现横向滚动条
.items-table--h-scroll {
  width: max-content;
  min-width: 1880px;
}

.q-date {
  :deep(.el-input__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    &.is-focus { border-color: rgba(0, 212, 255, 0.5) !important; }
  }
  :deep(.el-input__inner) {
    color: $text-primary !important;
  }
}

// 明细表格
.items-table {
  // 无外边框，行间细线分隔，对标客户管理列表风格
  --el-table-border-color: transparent;
  --el-table-header-bg-color: rgba(0, 212, 255, 0.04);
  --el-table-row-hover-bg-color: rgba(0, 212, 255, 0.04);
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
  :deep(.el-table__inner-wrapper) {
    background: transparent;
    &::before { display: none !important; }
    &::after  { display: none !important; }
  }
  :deep(.el-table__border-left-patch) { display: none !important; }
  :deep(.el-table__header-wrapper) {
    th.el-table__cell {
      background: rgba(0, 212, 255, 0.04) !important;
      border-bottom: 1px solid rgba(0, 212, 255, 0.1) !important;
      border-right: none !important;
      color: rgba(200, 216, 232, 0.55);
      font-size: 12px;
      font-weight: 500;
      letter-spacing: 0.3px;
    }
  }
  :deep(.el-table__row) {
    background: transparent !important;
    td.el-table__cell {
      background: transparent !important;
      border-bottom: 1px solid rgba(255, 255, 255, 0.04) !important;
      border-right: none !important;
      color: rgba(224, 244, 255, 0.85);
      font-size: 13px;
    }
    &:last-child td.el-table__cell { border-bottom: none !important; }
    &:hover td.el-table__cell { background: rgba(0, 212, 255, 0.04) !important; }
  }
  :deep(.el-table__cell) {
    .el-button { white-space: nowrap !important; }
    .cell { white-space: nowrap; }
  }
  :deep(.rfq-table-target-ccy-col .cell) {
    overflow: visible;
    white-space: normal;
  }
  :deep(.rfq-table-brand-col .cell) {
    overflow: visible;
    white-space: normal;
  }
  :deep(.rfq-table-brand-col .biz-brand-select__control) {
    min-width: 148px;
  }
  :deep(.rfq-table-brand-col .el-select__selected-item) {
    overflow: visible;
    text-overflow: clip;
  }
  :deep(.rfq-table-remark-col .cell) {
    overflow: visible;
    white-space: normal;
  }
}

.brand-import-hint {
  margin-top: 4px;
  font-size: 12px;
  line-height: 1.4;
  color: #e6a23c;
}

.brand-import-hint--table {
  margin-top: 6px;
  max-width: 220px;
}

.empty-hint {
  text-align: center;
  padding: 20px 0;
  color: $text-muted;
  font-size: 13px;
}
</style>

<style lang="scss">
/* 下拉 teleport 到 body，需全局 popper 样式使选项行撑满、提示图标靠右 */
.rfq-assign-method-select-popper {
  .el-select-dropdown__item {
    padding-right: 12px;
  }

  .assign-method-option {
    display: flex;
    align-items: center;
    width: 100%;
    min-width: 0;
    gap: 8px;
  }

  .assign-method-option-label {
    flex: 1;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .assign-method-option-tip {
    margin-left: auto;
    flex-shrink: 0;
    font-size: 14px;
    color: #eab308;
    cursor: help;
  }
}
</style>
