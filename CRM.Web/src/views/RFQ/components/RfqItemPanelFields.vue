<template>
  <div class="rfq-item-panel-fields">
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
            :delegate-create-dialog="delegateBrandCreate"
            placeholder="请选择品牌"
            size="default"
            @request-create="emit('request-brand-create')"
            @change="(p) => emit('brand-change', p)"
          />
          <div v-if="needsBrandAttention" class="brand-import-hint">
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
            v-model:currency="priceCurrencyModel"
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
          <MaterialProductionDateSelect v-model="productionDateModel" select-class="q-select" />
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
          <el-input v-model="row.remark" type="textarea" :rows="2" placeholder="备注" class="q-input" />
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import BizBrandSelect from '@/components/Biz/BizBrandSelect.vue'
import SettlementCurrencyAmountInput from '@/components/SettlementCurrencyAmountInput.vue'
import MaterialProductionDateSelect from '@/components/MaterialProductionDateSelect.vue'
import { DEFAULT_SETTLEMENT_CURRENCY_CODE } from '@/constants/currency'

export type RfqItemPanelRow = {
  customerMpn?: string
  customerBrand?: string
  mpn?: string
  brand?: string
  brandId?: number
  _importBrandText?: string
  targetPrice?: number
  priceCurrency?: number
  quantity?: number
  productionDate?: string
  expiryDate?: string
  minPackageQty?: number
  minOrderQty?: number
  alternativeMaterials?: string
  remark?: string
}

const props = defineProps<{
  row: RfqItemPanelRow
  delegateBrandCreate?: boolean
}>()

const emit = defineEmits<{
  'brand-change': [payload: { id: number; standardBrand: string; auditStatus?: number | null }]
  'request-brand-create': []
}>()

const needsBrandAttention = computed(() => {
  const hasId = props.row.brandId != null && props.row.brandId > 0
  const text = (props.row._importBrandText || props.row.brand || '').trim()
  return !hasId && !!text
})

const priceCurrencyModel = computed({
  get: () => props.row.priceCurrency ?? DEFAULT_SETTLEMENT_CURRENCY_CODE,
  set: (v: number) => {
    props.row.priceCurrency = v
  }
})

const productionDateModel = computed({
  get: () => props.row.productionDate ?? '',
  set: (v: string) => {
    props.row.productionDate = v
  }
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

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

.brand-import-hint {
  margin-top: 4px;
  font-size: 12px;
  line-height: 1.4;
  color: #e6a23c;
}

.q-input {
  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    color: $text-primary !important;
    transition: border-color 0.2s;
    &:hover {
      border-color: rgba(0, 212, 255, 0.25) !important;
    }
    &.is-focus {
      border-color: rgba(0, 212, 255, 0.5) !important;
      box-shadow: 0 0 0 2px rgba(0, 212, 255, 0.08) !important;
    }
  }
  :deep(.el-input__inner) {
    color: $text-primary !important;
    background: transparent !important;
    font-size: 13px;
    &::placeholder {
      color: $text-placeholder !important;
    }
  }
  :deep(.el-textarea__inner) {
    font-size: 13px;
    &::placeholder {
      color: $text-placeholder !important;
    }
  }
}

.q-select {
  :deep(.el-select__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    color: $text-primary !important;
    &.is-focused {
      border-color: rgba(0, 212, 255, 0.5) !important;
    }
  }
  :deep(.el-select__placeholder) {
    color: $text-placeholder !important;
  }
  :deep(.el-select__selected-item) {
    color: $text-primary !important;
  }
}

.q-number {
  :deep(.el-input__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    &.is-focus {
      border-color: rgba(0, 212, 255, 0.5) !important;
    }
  }
  :deep(.el-input__inner) {
    color: $text-primary !important;
    background: transparent !important;
  }
}

.q-date {
  :deep(.el-input__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    &.is-focus {
      border-color: rgba(0, 212, 255, 0.5) !important;
    }
  }
  :deep(.el-input__inner) {
    color: $text-primary !important;
  }
}
</style>
