<template>
  <div class="po-doc po-doc--v2 po-doc--v2-semicore">
    <PurchaseOrderReportV2Body v-bind="props" />
  </div>
</template>

<script setup lang="ts">
import PurchaseOrderReportV2Body from '../PurchaseOrderReportV2Body.vue'
import {
  purchaseOrderReportDocumentPropDefaults,
  type PurchaseOrderReportDocumentProps
} from '../types'

const props = withDefaults(
  defineProps<PurchaseOrderReportDocumentProps>(),
  purchaseOrderReportDocumentPropDefaults
)
</script>

<style lang="scss">
/* 与 V1 Semicore 绿表同源；挂到 ecoinf */
.po-doc--v2-semicore {
  --po-v2-navy: #101010;
  --po-v2-accent: #a8d070;
  --po-v2-line: #a8d070;
  --po-v2-head-fg: #fff;
  --po-v2-ink: #1a1d22;
  --po-v2-border: #c8c8c8;
  --po-v2-muted: #6b7280;
  --po-v2-wash: #f4f6f2;
  --po-v2-panel: #eef6e4;
  --po-v2-row: #f6faf0;
  --po-v2-legal: #f4f7f0;
}
</style>
