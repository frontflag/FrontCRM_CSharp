<template>
  <span class="packing-cascade-item-summary">
    <span class="panel-hint__value">{{ pn }}</span><span class="panel-hint__sep"> / </span
    ><span class="panel-hint__value">{{ brand }}</span><span class="panel-hint__sep"> / </span
    ><span class="panel-hint__value">{{ qtyText }}</span>
  </span>
</template>

<script setup lang="ts">
defineProps<{
  pn: string
  brand: string
  qtyText: string
}>()
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.panel-hint__value {
  color: $color-amber;
}

.panel-hint__sep {
  color: $text-muted;
}
</style>
