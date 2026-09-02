<template>
  <div class="refresh-customer-settings" v-loading="loading">
    <div class="section-head">
      <div class="section-head__left">
        <div class="section-title">
          <span class="title-bar"></span>{{ t('salesParams.refreshCustomerTitle') }}
        </div>
        <p class="section-hint">{{ t('salesParams.refreshCustomerHint') }}</p>
      </div>
      <div class="section-head__actions">
        <el-button type="primary" :loading="saving" @click="save">{{ t('salesParams.saveBtn') }}</el-button>
        <el-button :loading="loading" @click="load">{{ t('salesParams.refreshBtn') }}</el-button>
      </div>
    </div>

    <div class="group-card">
      <div class="field-row" v-for="row in facetRows" :key="row.key">
        <span class="field-label">{{ t(row.labelKey) }}</span>
        <div class="field-control">
          <el-switch
            v-model="facets[row.key]"
            :active-text="t('salesParams.allow')"
            :inactive-text="t('salesParams.disallow')"
            inline-prompt
          />
        </div>
      </div>
      <p class="field-note">{{ t('salesParams.allowRefreshCompletedNote') }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  salesParamsApi,
  type SalesRefreshCompletedFacets
} from '@/api/salesParams'

const { t } = useI18n()
const loading = ref(false)
const saving = ref(false)
const facets = reactive<SalesRefreshCompletedFacets>({
  customer: false,
  pn: true,
  brand: true,
  qty: true,
  price: true
})

const facetRows: { key: keyof SalesRefreshCompletedFacets; labelKey: string }[] = [
  { key: 'customer', labelKey: 'salesParams.allowRefreshCompletedCustomer' },
  { key: 'pn', labelKey: 'salesParams.allowRefreshCompletedPn' },
  { key: 'brand', labelKey: 'salesParams.allowRefreshCompletedBrand' },
  { key: 'qty', labelKey: 'salesParams.allowRefreshCompletedQty' },
  { key: 'price', labelKey: 'salesParams.allowRefreshCompletedPrice' }
]

function applyFacets(next: SalesRefreshCompletedFacets) {
  facets.customer = next.customer
  facets.pn = next.pn
  facets.brand = next.brand
  facets.qty = next.qty
  facets.price = next.price
}

async function load() {
  loading.value = true
  try {
    applyFacets(await salesParamsApi.getRefreshCompletedFacets())
  } catch {
    ElMessage.error(t('salesParams.refreshCustomerLoadFailed'))
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  try {
    applyFacets(
      await salesParamsApi.setRefreshCompletedFacets({
        customer: facets.customer,
        pn: facets.pn,
        brand: facets.brand,
        qty: facets.qty,
        price: facets.price
      })
    )
    ElMessage.success(t('salesParams.saveSuccess'))
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('salesParams.saveFailed')
    ElMessage.error(msg)
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.refresh-customer-settings {
  min-height: 200px;
}

.section-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 16px;
  &__left {
    flex: 1;
    min-width: 0;
  }
  &__actions {
    display: flex;
    gap: 8px;
    flex-shrink: 0;
  }
}

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 6px;
}

.title-bar {
  width: 3px;
  height: 16px;
  background: linear-gradient(180deg, #00c8ff, #0066cc);
  border-radius: 2px;
}

.section-hint {
  margin: 0;
  font-size: 13px;
  color: $text-muted;
  line-height: 1.5;
}

.group-card {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 20px;
}

.field-row {
  display: flex;
  align-items: center;
  gap: 16px;
  & + & {
    margin-top: 14px;
  }
}

.field-label {
  font-size: 13px;
  color: $text-secondary;
  min-width: 220px;
}

.field-control {
  display: flex;
  align-items: center;
}

.field-note {
  margin: 16px 0 0;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}
</style>
