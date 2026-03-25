<template>
  <div class="packing-list-page">
    <div class="page-header">
      <h1 class="page-title">装箱单</h1>
      <button class="btn-secondary" @click="refresh">刷新</button>
    </div>

    <div class="placeholder-card">
      <div class="title">装箱单页面</div>
      <div class="desc">当前已完成菜单与路由接入，可在此页继续落地装箱单业务列表与详情。</div>
    </div>
  </div>
</template>

<script setup lang="ts">
const refresh = () => {
  window.location.reload()
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.packing-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}

.placeholder-card {
  padding: 18px;
  border-radius: 10px;
  border: 1px solid $border-panel;
  background: $layer-2;
}

.title {
  font-size: 16px;
  color: $text-primary;
  margin-bottom: 8px;
}

.desc {
  color: $text-muted;
  font-size: 13px;
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid $border-panel;
  background: rgba(255, 255, 255, 0.05);
  color: $text-secondary;
}
</style>
