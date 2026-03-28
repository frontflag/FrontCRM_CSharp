<script setup lang="ts">
defineProps<{
  collapsed: boolean
  expanded: boolean
}>()

const emit = defineEmits<{ (e: 'toggle'): void }>()

function onToggle() {
  emit('toggle')
}
</script>

<template>
  <div class="menu-group">
    <template v-if="!collapsed">
      <button type="button" class="menu-item has-children" :class="{ 'group-open': expanded }" @click="onToggle">
        <slot name="icon" />
        <slot name="label" />
        <slot name="chevron" />
      </button>
      <div v-if="expanded" class="submenu">
        <slot name="submenu" />
      </div>
    </template>
    <el-popover
      v-else
      placement="right-start"
      trigger="hover"
      :width="232"
      :show-arrow="false"
      :offset="6"
      :teleported="true"
      popper-class="sidebar-menu-flyout-popper"
      :popper-options="{ strategy: 'fixed' }"
    >
      <template #reference>
        <button type="button" class="menu-item has-children" @click.prevent>
          <slot name="icon" />
        </button>
      </template>
      <div class="sidebar-menu-flyout-body" @click.stop>
        <slot name="submenu" />
      </div>
    </el-popover>
  </div>
</template>
