<template>
  <div class="wty-doc wty-doc--v2 po-doc po-doc--v2 po-doc--letter-wty-v2">
    <WarrantyLetterReportV2Body v-bind="props" />
  </div>
</template>

<script setup lang="ts">
import WarrantyLetterReportV2Body from '../WarrantyLetterReportV2Body.vue'
import {
  warrantyLetterReportDocumentPropDefaults,
  type WarrantyLetterReportDocumentProps
} from '../types'

const props = withDefaults(
  defineProps<WarrantyLetterReportDocumentProps>(),
  warrantyLetterReportDocumentPropDefaults
)
</script>
