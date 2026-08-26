<template>
  <div class="po-doc po-doc--v2 po-doc--v2-ecoinf">
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
/* 与 V1 Ecoinf 同源；挂到 idesemi */
.po-doc--v2-ecoinf {
  --po-v2-navy: #11161f;
  --po-v2-accent: #6dc5f6;
  --po-v2-line: #6dc5f6;
  --po-v2-head-fg: #fff;
  --po-v2-ink: #1a1d22;
  --po-v2-border: #d4d4d4;
  --po-v2-muted: #6b7280;
  --po-v2-wash: #f4f6f8;
  --po-v2-panel: #eaf4fb;
  --po-v2-row: #f5f9fc;
  --po-v2-legal: #f3f6f8;
}
</style>
