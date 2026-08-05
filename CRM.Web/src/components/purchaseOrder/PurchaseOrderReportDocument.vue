<!-- 兼容入口：默认绿表组件；页面请用 resolvePurchaseOrderReportSkin -->
<template>
  <PurchaseOrderReportSkinSemicore v-bind="props" />
</template>

<script setup lang="ts">
import PurchaseOrderReportSkinSemicore from './purchaseOrderReport/skins/PurchaseOrderReportSkinSemicore.vue'
import {
  purchaseOrderReportDocumentPropDefaults,
  type PurchaseOrderReportDocumentProps
} from './purchaseOrderReport/types'

const props = withDefaults(defineProps<PurchaseOrderReportDocumentProps>(), purchaseOrderReportDocumentPropDefaults)
</script>
