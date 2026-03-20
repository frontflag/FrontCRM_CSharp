<template>
  <div class="debug-page">
    <div class="debug-header">
      <h1>Debug</h1>
      <div class="debug-sub">读取数据库 `debug` 表的全部记录</div>
    </div>

    <div v-if="loading" class="debug-loading">Loading...</div>
    <div v-else-if="error" class="debug-error">{{ error }}</div>

    <el-table v-else :data="items" border style="width: 100%">
      <el-table-column prop="name" label="Name" min-width="200" />
      <el-table-column prop="value" label="Value" min-width="320" show-overflow-tooltip />
    </el-table>

    <div v-if="!loading && !error && items.length === 0" class="debug-empty">
      没有 debug 记录
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getDebugItems, type DebugItem } from '@/api/debug'

const items = ref<DebugItem[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

onMounted(async () => {
  loading.value = true
  error.value = null
  try {
    items.value = await getDebugItems()
  } catch (e: any) {
    error.value =
      e?.response?.data?.message ||
      e?.message ||
      '获取 Debug 列表失败'
  } finally {
    loading.value = false
  }
})
</script>

<style lang="scss" scoped>
.debug-page {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.debug-header h1 {
  margin: 0;
  font-size: 20px;
  font-weight: 700;
}

.debug-sub {
  margin-top: 6px;
  font-size: 12px;
  color: rgba(200, 220, 240, 0.7);
}

.debug-loading {
  color: rgba(200, 220, 240, 0.8);
}

.debug-error {
  color: #ff6b6b;
  background: rgba(255, 107, 107, 0.08);
  border: 1px solid rgba(255, 107, 107, 0.25);
  padding: 12px 14px;
  border-radius: 10px;
}

.debug-empty {
  color: rgba(200, 220, 240, 0.7);
  font-size: 13px;
}
</style>

