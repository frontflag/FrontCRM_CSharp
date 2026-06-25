<template>
  <section class="json-block-panel">
    <header
      v-if="title"
      class="json-block-panel__head"
      :class="{ 'json-block-panel__head--plain': !headDivider }"
    >
      <h3 class="json-block-panel__title">{{ title }}</h3>
    </header>
    <div class="json-block-panel__body">
      <slot />
    </div>
  </section>
</template>

<script setup lang="ts">
withDefaults(
  defineProps<{
    title?: string
    /** 标题下是否显示分隔横线，列表项卡片可设为 false */
    headDivider?: boolean
  }>(),
  {
    headDivider: true
  }
)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.json-block-panel {
  padding: 14px 16px;
  background: $layer-1;
  border: 1px solid $border-panel;
  border-radius: 10px;
}

.json-block-panel__head {
  margin-bottom: 10px;
  padding-bottom: 8px;
  border-bottom: 1px solid $border-panel;

  &--plain {
    margin-bottom: 8px;
    padding-bottom: 0;
    border-bottom: none;
  }
}

.json-block-panel__title {
  margin: 0;
  font-size: 13px;
  font-weight: 600;
  color: $text-secondary;
}

.json-block-panel__body {
  min-width: 0;
}
</style>
