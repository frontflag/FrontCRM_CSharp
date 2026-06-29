<template>
  <el-tooltip :content="displayText" placement="top" :disabled="tooltipDisabled">
    <el-input
      v-if="mode === 'compact'"
      type="textarea"
      :autosize="{ minRows: 1, maxRows: 2 }"
      :model-value="displayText"
      :disabled="masked"
      readonly
      class="vendor-name-readonly-field vendor-name-readonly-field--compact"
    />
    <el-input
      v-else
      :model-value="displayText"
      :disabled="masked"
      readonly
      class="vendor-name-readonly-field vendor-name-readonly-field--inline"
    />
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
    /** compact：弹窗/窄列，textarea 1~2 行；inline：详情表单单行 */
    mode?: 'inline' | 'compact'
  }>(),
  {
    nameZh: '',
    nameEn: '',
    masked: false,
    mode: 'compact',
  }
)

const displayText = computed(() =>
  formatVendorNameReadonly(props.nameZh, props.nameEn, { masked: props.masked })
)

const tooltipDisabled = computed(() => props.masked || !displayText.value || displayText.value === '—')
</script>

<style lang="scss" scoped>
.vendor-name-readonly-field {
  width: 100%;

  :deep(.el-textarea__inner),
  :deep(.el-input__inner) {
    cursor: default;
  }
}
</style>
