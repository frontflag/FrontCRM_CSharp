<template>
  <div class="po-doc po-doc--v2 po-doc--pl-v2 po-doc--pl-v2-ls po-doc--landscape">
    <PackingReportV2Body v-bind="props" />
  </div>
</template>

<script setup lang="ts">
import PackingReportV2Body from '../PackingReportV2Body.vue'
import {
  packingReportV2DocumentPropDefaults,
  type PackingReportV2DocumentProps
} from '../types'

const props = withDefaults(
  defineProps<PackingReportV2DocumentProps>(),
  packingReportV2DocumentPropDefaults
)
</script>

<style lang="scss">
.po-doc--pl-v2-ls {
  --po-v2-navy: #090e1d;
  --po-v2-accent: #00d2ef;
  --po-v2-line: #05e5ff;
  --po-v2-head-fg: #fff;
  --po-v2-ink: #1a1d22;
  --po-v2-muted: #6b7280;
  --po-v2-border: #d8e1e6;
  --po-v2-wash: #f5f7fa;
  --po-v2-panel: #eaf6fb;
  --po-v2-row: #f4f9fc;
  --po-v2-legal: #f3f7f9;
}
</style>
