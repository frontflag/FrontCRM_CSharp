<template>
  <div class="payment-vendor-bank-section">
    <el-row :gutter="12">
      <el-col :span="12">
        <el-form-item :label="t('purchaseOrderItemList.paymentDialog.vendorBank')" required>
          <vendor-bank-select
            :model-value="modelValue"
            :options="enabledBanks"
            :placeholder="t('purchaseOrderItemList.paymentDialog.vendorBankPlaceholder')"
            :masked="masked"
            :disabled="!vendorId"
            @update:model-value="emit('update:modelValue', $event)"
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <slot name="trailing" />
      </el-col>
    </el-row>

    <el-alert
      v-if="vendorId && !enabledBanks.length"
      type="warning"
      :closable="false"
      show-icon
      class="payment-vendor-bank-section__alert"
    >
      <template #title>
        <span>{{ t('purchaseOrderItemList.paymentDialog.noVendorBank') }}</span>
        <el-link type="primary" :underline="false" class="payment-vendor-bank-section__link" @click="goMaintainVendorBank">
          {{ t('purchaseOrderItemList.paymentDialog.maintainVendorBank') }}
        </el-link>
      </template>
    </el-alert>

    <vendor-bank-info-panel v-if="selectedBank" :bank="selectedBank" :masked="masked" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import type { VendorBankInfo } from '@/types/vendor'
import VendorBankSelect from '@/components/Vendor/VendorBankSelect.vue'
import VendorBankInfoPanel from '@/components/Vendor/VendorBankInfoPanel.vue'
import { filterEnabledVendorBanks } from '@/utils/vendorFinancePaymentBank'

const props = withDefaults(
  defineProps<{
    vendorId?: string
    modelValue?: string
    banks?: VendorBankInfo[]
    masked?: boolean
  }>(),
  {
    vendorId: '',
    modelValue: '',
    banks: () => [],
    masked: false
  }
)

const emit = defineEmits<{
  'update:modelValue': [value: string]
  maintain: []
}>()

const { t } = useI18n()
const router = useRouter()

const enabledBanks = computed(() => filterEnabledVendorBanks(props.banks))

const selectedBank = computed(() => {
  const id = props.modelValue?.trim()
  if (!id) return null
  return enabledBanks.value.find((b) => b.id === id) ?? null
})

function goMaintainVendorBank() {
  if (!props.vendorId) return
  emit('maintain')
  void router.push({
    name: 'VendorDetail',
    params: { id: props.vendorId },
    query: { tab: 'banks' }
  })
}
</script>

<style scoped>
.payment-vendor-bank-section__alert {
  margin-bottom: 12px;
}

.payment-vendor-bank-section__link {
  margin-left: 8px;
}
</style>
