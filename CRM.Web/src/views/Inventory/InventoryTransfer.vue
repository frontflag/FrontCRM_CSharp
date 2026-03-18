<template>
  <div class="inventory-placeholder">
    <div class="icon-wrap">
      <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
        <path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/>
        <path d="M7.5 4.21l4.5 2.6 4.5-2.6M12 21.08V12"/>
      </svg>
    </div>
    <h2>库存调拨</h2>
    <p>功能开发中，敬请期待。</p>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
.inventory-placeholder {
  padding: 48px 24px;
  text-align: center;
  min-height: 400px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: $text-muted;
  .icon-wrap { color: rgba(0, 212, 255, 0.4); margin-bottom: 16px; }
  h2 { font-size: 18px; color: $text-secondary; margin: 0 0 8px; }
  p { font-size: 14px; margin: 0; }
}
</style>
