<template>
  <div class="inventory-placeholder">
    <div class="icon-wrap">
      <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
        <path d="M9 11l3 3L22 4"/>
        <path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11"/>
      </svg>
    </div>
    <h2>库存盘点</h2>
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
