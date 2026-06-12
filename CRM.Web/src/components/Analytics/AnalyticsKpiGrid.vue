<script setup lang="ts">
defineProps<{
  items: { key: string; label: string; value: string; tone?: 'todo' | 'snapshot' }[]
}>()
</script>

<template>
  <div class="kpi-grid">
    <div
      v-for="item in items"
      :key="item.key"
      class="kpi-card"
      :class="item.tone === 'todo' ? 'kpi-card--todo' : ''"
    >
      <span class="kpi-label">{{ item.label }}</span>
      <span class="kpi-value">{{ item.value }}</span>
    </div>
  </div>
</template>

<style scoped lang="scss">
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 12px;
}

.kpi-card {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 14px 16px;
}

.kpi-card--todo {
  border-color: var(--el-color-warning-light-5);
  background: var(--el-color-warning-light-9);
}

.kpi-label {
  display: block;
  font-size: 13px;
  color: var(--el-text-color-secondary);
  margin-bottom: 6px;
}

.kpi-value {
  font-size: 22px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}
</style>
