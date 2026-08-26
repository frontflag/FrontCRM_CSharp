<template>
  <div class="po-doc po-doc--v2 po-doc--v2-idesemi">
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
/* 与 V1 Idesemi 同源；挂到 semicore */
.po-doc--v2-idesemi {
  --po-v2-navy: #090e1d;
  --po-v2-accent: #00d2ef;
  --po-v2-line: #05e5ff;
  --po-v2-head-fg: #fff;
  --po-v2-ink: #1a1d22;
  --po-v2-border: #d8e1e6;
  --po-v2-muted: #6b7280;
  --po-v2-wash: #f5f7fa;
  --po-v2-panel: #eaf6fb;
  --po-v2-row: #f4f9fc;
  --po-v2-legal: #f3f7f9;
}
</style>
