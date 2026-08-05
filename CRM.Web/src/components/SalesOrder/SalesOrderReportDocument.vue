<!-- 兼容入口：默认绿表组件；页面请用 resolveSalesOrderReportSkin -->
<template>
  <SalesOrderReportSkinSemicore v-bind="props" />
</template>

<script setup lang="ts">
import SalesOrderReportSkinSemicore from './salesOrderReport/skins/SalesOrderReportSkinSemicore.vue'
import {
  salesOrderReportDocumentPropDefaults,
  type SalesOrderReportDocumentProps
} from './salesOrderReport/types'

const props = withDefaults(defineProps<SalesOrderReportDocumentProps>(), salesOrderReportDocumentPropDefaults)
</script>
