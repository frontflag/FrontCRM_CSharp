<template>
  <div class="po-doc po-doc--v2 po-doc--inv-v2">
    <InvoiceReportV2Body v-bind="props" />
  </div>
</template>

<script setup lang="ts">
import InvoiceReportV2Body from '../InvoiceReportV2Body.vue'
import {
  invoiceReportDocumentPropDefaults,
  type InvoiceReportDocumentProps
} from '../types'

const props = withDefaults(defineProps<InvoiceReportDocumentProps>(), invoiceReportDocumentPropDefaults)
</script>
