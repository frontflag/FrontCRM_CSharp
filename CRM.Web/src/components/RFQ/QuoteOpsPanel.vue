<template>
  <div class="quote-ops-root" aria-label="quote-ops-panel">
    <div class="quote-ops-root__content">
      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('quoteOpsPanel.title') }}</h3>
        </header>
        <div class="ops-card__body">
          <p v-if="!hasVendor" class="ops-hint">{{ t('quoteOpsPanel.needVendor') }}</p>
          <p v-else-if="vendorName" class="ops-vendor-name">{{ vendorName }}</p>
          <button
            type="button"
            class="ops-action-btn"
            :class="hasVendor ? 'ops-action-btn--primary' : 'ops-action-btn--disabled'"
            :disabled="!hasVendor"
            @click="openCreateContact"
          >
            {{ t('quoteOpsPanel.createVendorContact') }}
          </button>
        </div>
      </section>
    </div>

    <VendorContactDialog
      v-if="contactDialogVisible && hasVendor"
      v-model="contactDialogVisible"
      :vendor-id="vendorId"
      @success="onContactCreated"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import VendorContactDialog from '@/views/Vendor/components/VendorContactDialog.vue'
import { useQuoteOpsPanelStore } from '@/stores/quoteOpsPanel'
import type { VendorContactInfo } from '@/types/vendor'

const { t } = useI18n()
const quoteOpsStore = useQuoteOpsPanelStore()
const { vendorId, vendorName } = storeToRefs(quoteOpsStore)

const contactDialogVisible = ref(false)

const hasVendor = computed(() => !!vendorId.value)

function openCreateContact() {
  if (!hasVendor.value) return
  contactDialogVisible.value = true
}

function onContactCreated(contact: VendorContactInfo) {
  quoteOpsStore.notifyContactCreated(contact)
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.quote-ops-root {
  height: 100%;
  min-height: 0;
  overflow: auto;
  box-sizing: border-box;
  padding: 12px;
}

.quote-ops-root__content {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.ops-card {
  border: 1px solid $border-card;
  border-radius: 10px;
  background: #fff;
  overflow: hidden;
}

.ops-card__head {
  padding: 10px 12px 0;
}

.ops-card__title {
  margin: 0;
  font-size: 13px;
  font-weight: 600;
  color: $text-primary;
}

.ops-card__body {
  padding: 10px 12px 12px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.ops-hint {
  margin: 0;
  font-size: 12px;
  line-height: 1.5;
  color: $text-secondary;
}

.ops-vendor-name {
  margin: 0;
  font-size: 12px;
  line-height: 1.5;
  color: $text-primary;
  word-break: break-all;
}

.ops-action-btn {
  width: 100%;
  border: none;
  border-radius: 10px;
  padding: 11px 14px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
}

.ops-action-btn--primary {
  background: #0f4c81;
  color: #fff;
}

.ops-action-btn--disabled,
.ops-action-btn:disabled {
  cursor: not-allowed;
  background: #e5e7eb;
  color: #9ca3af;
  opacity: 1;
}
</style>
