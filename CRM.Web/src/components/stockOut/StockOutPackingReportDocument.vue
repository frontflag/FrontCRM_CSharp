<!-- 兼容入口：默认 Semicore 橙表；页面请用 resolvePackingReportSkin -->
<template>
  <PackingReportSkinSemicore v-bind="props" />
</template>

<script setup lang="ts">
import PackingReportSkinSemicore from './packingReport/skins/PackingReportSkinSemicore.vue'
import {
  packingReportDocumentPropDefaults,
  type PackingReportDocumentProps
} from './packingReport/types'

const props = withDefaults(defineProps<PackingReportDocumentProps>(), packingReportDocumentPropDefaults)
</script>
