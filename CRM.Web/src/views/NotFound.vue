<script setup lang="ts">
</script>

<template>
  <div class="not-found">
    <div class="code">404</div>
    <div class="msg">页面不存在</div>
  </div>
</template>

<style lang="scss" scoped>
.not-found {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  background: #f5f7fa;
  color: #303133;
  font-family: system-ui, -apple-system, 'Segoe UI', sans-serif;
}

.code {
  font-size: 64px;
  font-weight: 700;
  letter-spacing: 2px;
  line-height: 1;
  color: #909399;
}

.msg {
  font-size: 16px;
  color: #606266;
}
</style>
