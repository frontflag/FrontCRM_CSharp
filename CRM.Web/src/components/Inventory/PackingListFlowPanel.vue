<template>
  <div class="packing-list-flow" aria-label="packing-list-flow-panel">
    <div v-if="!packing && !bindingLoading" class="so-item-flow-root__empty">
      {{ bindError || t('packingList.flowPanel.pickPacking') }}
    </div>

    <div v-else v-loading="bindingLoading" class="packing-list-flow__body">
      <template v-if="packing && !bindingLoading">
        <div v-if="itemChips.length === 0" class="so-item-flow-root__empty">
          {{ t('packingList.flowPanel.noItems') }}
        </div>
        <template v-else>
          <div class="packing-list-flow__chips" role="tablist" :aria-label="t('packingList.flowPanel.itemSwitcher')">
            <button
              v-for="chip in itemChips"
              :key="chip.id"
              type="button"
              role="tab"
              class="packing-list-flow__chip"
              :class="{ 'is-active': chip.id === selectedPackingItemId }"
              :aria-selected="chip.id === selectedPackingItemId"
              @click="onSelectItem(chip.id)"
            >
              {{ chip.itemCode }}
            </button>
          </div>
          <PackingItemFlowPanel
            :row="flowRow"
            :aggregates="aggregates"
            :extras="flowExtras"
            :loading="loading"
            :load-error="loadError"
            :missing-sell-link="missingSellLink"
            :mask-sensitive="maskSensitive"
          />
        </template>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import PackingItemFlowPanel from '@/components/Inventory/PackingItemFlowPanel.vue'
import { usePackingDetailFlowPanelStore } from '@/stores/packingDetailFlowPanel'

defineProps<{
  maskSensitive?: boolean
}>()

const { t } = useI18n()
const store = usePackingDetailFlowPanelStore()
const {
  packing,
  itemChips,
  selectedPackingItemId,
  flowRow,
  flowExtras,
  aggregates,
  loading,
  bindingLoading,
  bindError,
  loadError,
  missingSellLink
} = storeToRefs(store)

function onSelectItem(itemId: string) {
  void store.selectItemById(itemId)
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import '@/assets/styles/so-item-flow-panel.scss';

.packing-list-flow {
  min-width: 0;
  width: 100%;
  height: 100%;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.packing-list-flow__body {
  min-height: 0;
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.packing-list-flow__chips {
  flex: 0 0 auto;
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  padding: 10px 10px 8px;
  border-bottom: 1px solid $border-panel;
}

.packing-list-flow__chip {
  appearance: none;
  border: 1px solid $border-panel;
  background: transparent;
  color: $text-primary;
  border-radius: 4px;
  padding: 3px 8px;
  font-size: 12px;
  line-height: 1.4;
  cursor: pointer;
  font-family: inherit;

  &:hover {
    border-color: rgba(0, 160, 220, 0.55);
  }

  &.is-active {
    border-color: #00a0dc;
    color: #00a0dc;
    background: rgba(0, 160, 220, 0.08);
    font-weight: 600;
  }
}

.packing-list-flow :deep(.so-item-flow-root) {
  flex: 1;
  min-height: 0;
  overflow: auto;
}
</style>
