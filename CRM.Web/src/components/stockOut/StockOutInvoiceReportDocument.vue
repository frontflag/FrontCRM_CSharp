<!-- 兼容入口：默认绿表组件；页面请用 resolveInvoiceReportSkin -->
<template>
  <InvoiceReportSkinSemicore v-bind="props" />
</template>

<script setup lang="ts">
import InvoiceReportSkinSemicore from './invoiceReport/skins/InvoiceReportSkinSemicore.vue'
import {
  invoiceReportDocumentPropDefaults,
  type InvoiceReportDocumentProps
} from './invoiceReport/types'

const props = withDefaults(defineProps<InvoiceReportDocumentProps>(), invoiceReportDocumentPropDefaults)
</script>
