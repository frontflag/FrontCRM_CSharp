<template>
  <div class="po-doc po-doc--v2 po-doc--so-v2">
    <SalesOrderReportV2Body v-bind="props" />
  </div>
</template>

<script setup lang="ts">
import SalesOrderReportV2Body from '../SalesOrderReportV2Body.vue'
import {
  salesOrderReportDocumentPropDefaults,
  type SalesOrderReportDocumentProps
} from '../types'

const props = withDefaults(defineProps<SalesOrderReportDocumentProps>(), salesOrderReportDocumentPropDefaults)
</script>
