<template>
  <div class="wty-doc wty-doc--v2 po-doc po-doc--v2 po-doc--so-wty-v2">
    <SalesOrderWarrantyReportV2Body v-bind="props" />
  </div>
</template>

<script setup lang="ts">
import SalesOrderWarrantyReportV2Body from '../SalesOrderWarrantyReportV2Body.vue'
import {
  salesOrderWarrantyReportDocumentPropDefaults,
  type SalesOrderWarrantyReportDocumentProps
} from '../types'

const props = withDefaults(
  defineProps<SalesOrderWarrantyReportDocumentProps>(),
  salesOrderWarrantyReportDocumentPropDefaults
)
</script>
