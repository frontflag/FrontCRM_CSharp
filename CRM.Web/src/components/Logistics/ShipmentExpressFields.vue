<template>
  <el-col :span="colSpan">
    <el-form-item :label="shipmentLabelText" :required="shipmentRequired">
      <el-select
        :model-value="shipmentMethod"
        filterable
        :clearable="shipmentClearable"
        :placeholder="placeholderText"
        style="width: 100%"
        @update:model-value="onShipmentChange"
      >
        <el-option
          v-for="o in shipmentArrivalOptions"
          :key="o.value"
          :label="o.label"
          :value="o.value"
        />
      </el-select>
    </el-form-item>
  </el-col>
  <el-col :span="colSpan">
    <el-form-item :label="expressLabelText">
      <el-select
        :model-value="expressCompany"
        clearable
        filterable
        :disabled="!expressEnabled"
        :placeholder="placeholderText"
        style="width: 100%"
        @update:model-value="emit('update:expressCompany', String($event ?? ''))"
      >
        <el-option
          v-for="o in expressOptions"
          :key="o.value"
          :label="o.label"
          :value="o.value"
        />
      </el-select>
    </el-form-item>
  </el-col>
</template>

<script setup lang="ts">
import { computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { isExpressShipmentMethod, useLogisticsFormDict } from '@/composables/useLogisticsFormDict'

const props = withDefaults(
  defineProps<{
    shipmentMethod: string
    expressCompany: string
    shipmentLabel?: string
    expressLabel?: string
    placeholder?: string
    shipmentRequired?: boolean
    shipmentClearable?: boolean
    colSpan?: number
  }>(),
  {
    shipmentRequired: true,
    shipmentClearable: false,
    colSpan: 12
  }
)

const emit = defineEmits<{
  'update:shipmentMethod': [v: string]
  'update:expressCompany': [v: string]
}>()

const { t } = useI18n()
const { ensureLoaded, shipmentArrivalOptions, expressOptions } = useLogisticsFormDict()

const shipmentLabelText = computed(
  () => props.shipmentLabel ?? t('packingList.columns.shipmentMethod')
)
const expressLabelText = computed(
  () => props.expressLabel ?? t('pickingSlip.detail.expressCompany')
)
const placeholderText = computed(
  () => props.placeholder ?? t('packingCreate.deliveryMethodPlaceholder')
)
const expressEnabled = computed(() => isExpressShipmentMethod(props.shipmentMethod))

function onShipmentChange(v: string) {
  emit('update:shipmentMethod', String(v ?? ''))
}

watch(
  () => props.shipmentMethod,
  (next) => {
    if (!isExpressShipmentMethod(next) && props.expressCompany) {
      emit('update:expressCompany', '')
    }
  }
)

onMounted(() => {
  void ensureLoaded()
})
</script>
