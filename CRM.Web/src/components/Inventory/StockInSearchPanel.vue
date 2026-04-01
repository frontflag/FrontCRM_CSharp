<script setup lang="ts">
import { reactive, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'

const { t } = useI18n()

const route = useRoute()
const router = useRouter()

const form = reactive({
  model: '',
  vendorName: '',
  purchaseOrderCode: '',
  salesOrderCode: ''
})

function syncFromRoute() {
  if (route.name !== 'StockInList') return
  const q = route.query
  form.model = typeof q.model === 'string' ? q.model : ''
  form.vendorName = typeof q.vendorName === 'string' ? q.vendorName : ''
  form.purchaseOrderCode = typeof q.purchaseOrderCode === 'string' ? q.purchaseOrderCode : ''
  form.salesOrderCode = typeof q.salesOrderCode === 'string' ? q.salesOrderCode : ''
}

watch(
  () => [route.name, route.query] as const,
  () => syncFromRoute(),
  { deep: true, immediate: true }
)

function handleReset() {
  router.push({ name: 'StockInList', query: {} })
}

function handleSearch() {
  const query: Record<string, string> = {}
  const m = form.model.trim()
  if (m) query.model = m
  const v = form.vendorName.trim()
  if (v) query.vendorName = v
  const p = form.purchaseOrderCode.trim()
  if (p) query.purchaseOrderCode = p
  const s = form.salesOrderCode.trim()
  if (s) query.salesOrderCode = s
  router.push({ name: 'StockInList', query })
}
</script>

<template>
  <div class="si-search-panel">
    <div class="si-search-panel__head">{{ t('stockInList.leftPanel.title') }}</div>

    <div class="si-search-panel__fields">
      <div class="field-col">
        <label class="field-label">{{ t('stockInList.leftPanel.materialModel') }}</label>
        <div class="field-control">
          <input
            v-model="form.model"
            type="text"
            class="field-input"
            :placeholder="t('stockInList.leftPanel.materialModelPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('stockInList.leftPanel.vendorName') }}</label>
        <div class="field-control">
          <input
            v-model="form.vendorName"
            type="text"
            class="field-input"
            :placeholder="t('stockInList.leftPanel.vendorPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('stockInList.leftPanel.purchaseOrderCode') }}</label>
        <div class="field-control">
          <input
            v-model="form.purchaseOrderCode"
            type="text"
            class="field-input"
            :placeholder="t('stockInList.leftPanel.purchaseOrderPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('stockInList.leftPanel.salesOrderCode') }}</label>
        <div class="field-control">
          <input
            v-model="form.salesOrderCode"
            type="text"
            class="field-input"
            :placeholder="t('stockInList.leftPanel.salesOrderPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>
    </div>

    <div class="si-search-panel__actions">
      <button type="button" class="btn-search" @click="handleSearch">{{ t('stockInList.leftPanel.search') }}</button>
      <button type="button" class="btn-reset" @click="handleReset">{{ t('stockInList.leftPanel.reset') }}</button>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.si-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: $text-secondary;
}

.si-search-panel__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 12px;
  font-size: 13px;
}

.si-search-panel__fields {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.field-col {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.field-label {
  font-size: 11px;
  font-weight: 500;
  color: $text-muted;
}

.field-control {
  width: 100%;
}

.field-input {
  width: 100%;
  box-sizing: border-box;
  padding: 7px 10px;
  font-size: 12px;
  color: $text-primary;
  background: $layer-3;
  border: 1px solid $border-panel;
  border-radius: 6px;
  outline: none;

  &::placeholder {
    color: $text-placeholder;
  }

  &:focus {
    border-color: var(--crm-accent-06);
  }
}

.si-search-panel__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 16px;
  padding-top: 12px;
  border-top: 1px solid $border-panel;
}

.btn-search {
  flex: 1;
  min-width: 72px;
  padding: 8px 12px;
  font-size: 12px;
  font-weight: 500;
  color: #fff;
  background: linear-gradient(135deg, $blue-primary, $cyan-primary);
  border: 1px solid var(--crm-action-primary-border);
  border-radius: 6px;
  cursor: pointer;
  transition: box-shadow 0.15s, transform 0.12s;

  &:hover {
    box-shadow: var(--crm-shadow-glow);
    transform: translateY(-1px);
  }
}

.btn-reset {
  padding: 8px 12px;
  font-size: 12px;
  color: $text-secondary;
  background: $layer-3;
  border: 1px solid $border-panel;
  border-radius: 6px;
  cursor: pointer;

  &:hover {
    background: var(--crm-accent-008);
    border-color: var(--crm-accent-018);
  }
}
</style>
