<script setup lang="ts">
defineProps<{
  collapsed: boolean
  tooltip: string
}>()
</script>

<template>
  <template v-if="collapsed">
    <el-tooltip :content="tooltip" placement="right" :show-after="180" effect="dark">
      <span class="sidebar-menu-tooltip-collapsed">
        <slot />
      </span>
    </el-tooltip>
  </template>
  <template v-else>
    <slot />
  </template>
</template>

<style scoped lang="scss">
.sidebar-menu-tooltip-collapsed {
  display: flex;
  justify-content: center;
  width: 100%;
}

:deep(.el-tooltip__trigger) {
  display: block;
  width: 100%;
  border: none;
  background: transparent;
  padding: 0;
  font: inherit;
  color: inherit;
}
</style>
