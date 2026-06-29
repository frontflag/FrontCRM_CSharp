<template>
  <el-tooltip :content="displayText" placement="top" :disabled="tooltipDisabled">
    <span class="vendor-name-readonly-text">{{ displayText }}</span>
  </el-tooltip>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { formatVendorNameReadonly } from '@/utils/vendorDisplayName'

const props = withDefaults(
  defineProps<{
    nameZh?: string | null
    nameEn?: string | null
    masked?: boolean
  }>(),
  {
    nameZh: '',
    nameEn: '',
    masked: false,
  }
)

const displayText = computed(() =>
  formatVendorNameReadonly(props.nameZh, props.nameEn, { masked: props.masked })
)

const tooltipDisabled = computed(() => props.masked || !displayText.value || displayText.value === '—')
</script>

<style lang="scss" scoped>
.vendor-name-readonly-text {
  word-break: break-word;
}
</style>
