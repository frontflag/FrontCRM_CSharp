<script setup lang="ts">
defineProps<{
  label: string
  checked: boolean
}>()
</script>

<template>
  <span class="progress-multi-option">
    <el-checkbox :model-value="checked" tabindex="-1" />
    <span class="progress-multi-option__text">{{ label }}</span>
  </span>
</template>

<style scoped lang="scss">
.progress-multi-option {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  min-width: 0;

  :deep(.el-checkbox) {
    pointer-events: none;
    height: auto;
    margin-right: 0;

    .el-checkbox__label {
      display: none;
    }
  }
}

.progress-multi-option__text {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
