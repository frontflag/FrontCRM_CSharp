<template>
  <div class="debug-page">
    <div class="debug-header">
      <h1>Debug</h1>
      <div class="debug-sub">
        当前前端构建版本：<span class="mono version-strong">{{ FRONTEND_DEBUG_VERSION }}</span>
        （来自 package.json，每次 <code>npm run build</code> 写入；用于核对线上是否为本次构建）
      </div>
      <div class="debug-sub muted">按 PRD：数据库连接（后端脱敏）、debug 表记录；版本号见上。</div>
      <div class="debug-sub muted debug-nav">
        <router-link class="debug-link" to="/debug/data">业务链路模拟数据</router-link>
        <span class="debug-nav-hint">（需登录）</span>
      </div>
    </div>

    <section class="debug-panel panel-version">
      <h2 class="panel-title">版本面板</h2>
      <div class="panel-body">
        <span class="meta-label">版本号</span>
        <span class="meta-value mono version-strong">{{ FRONTEND_DEBUG_VERSION }}</span>
      </div>
    </section>

    <div v-if="loading" class="debug-loading">Loading...</div>
    <div v-else-if="error" class="debug-error">{{ error }}</div>

    <template v-else>
      <!-- 数据库面板 -->
      <section class="debug-panel panel-db">
        <h2 class="panel-title">数据库面板</h2>
        <div class="panel-body connection-block">
          <span class="meta-value mono break-all">{{ connectionDisplay }}</span>
        </div>
      </section>

      <!-- 记录面板 -->
      <section class="debug-panel panel-records">
        <h2 class="panel-title">记录面板</h2>
        <CrmDataTable embedded :data="items" class="debug-table">
          <el-table-column prop="name" label="Name" min-width="200" />
          <el-table-column prop="value" label="Value" min-width="320" show-overflow-tooltip />
        </CrmDataTable>
        <div v-if="items.length === 0" class="debug-empty">没有 debug 记录</div>
      </section>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getDebugPage } from '@/api/debug'

/** debug 页面展示版本号（按发布批次手动维护） */
const FRONTEND_DEBUG_VERSION = '1.1.0327'

const items = ref<{ name: string; value: string }[]>([])
const databaseConnectionDisplay = ref('')
const loading = ref(false)
const error = ref<string | null>(null)

const connectionDisplay = computed(() => databaseConnectionDisplay.value || '—')

onMounted(async () => {
  loading.value = true
  error.value = null
  try {
    const page = await getDebugPage()
    databaseConnectionDisplay.value = page.databaseConnectionDisplay ?? ''
    items.value = page.items ?? []
  } catch (e: any) {
    error.value =
      e?.response?.data?.message ||
      e?.message ||
      '获取 Debug 数据失败'
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
  gap: 20px;
}

.debug-header h1 {
  margin: 0;
  font-size: 20px;
  font-weight: 700;
  color: #e8f4ff;
}

.debug-sub {
  margin-top: 6px;
  font-size: 12px;
  color: rgba(200, 220, 240, 0.7);
  line-height: 1.5;

  &.muted {
    margin-top: 4px;
    opacity: 0.85;
  }

  code {
    font-size: 11px;
    padding: 0 4px;
    border-radius: 4px;
    background: rgba(0, 0, 0, 0.25);
  }
}

.debug-nav {
  margin-top: 8px;
}

.debug-link {
  color: #00d4ff;
  text-decoration: none;
  font-weight: 600;

  &:hover {
    text-decoration: underline;
  }
}

.debug-nav-hint {
  margin-left: 4px;
  opacity: 0.85;
}

.debug-panel {
  padding: 16px 18px;
  border-radius: 10px;
  border: 1px solid rgba(0, 212, 255, 0.2);
  background: rgba(0, 212, 255, 0.06);
}

.panel-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
  color: #e8f4ff;
}

.panel-body {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
}

.connection-block {
  display: block;
}

.meta-label {
  font-size: 13px;
  font-weight: 600;
  color: rgba(200, 216, 232, 0.85);
}

.meta-value {
  font-size: 13px;
  color: #e8f4ff;
}

.version-strong {
  font-size: 18px;
  font-weight: 700;
  color: #00d4ff;
}

.mono {
  font-family: ui-monospace, 'Cascadia Code', 'Consolas', monospace;
}

.break-all {
  word-break: break-all;
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
  margin-top: 10px;
  color: rgba(200, 220, 240, 0.7);
  font-size: 13px;
}

.debug-table {
  :deep(.el-table__header-wrapper th) {
    background: rgba(0, 212, 255, 0.08);
    color: rgba(200, 216, 232, 0.9);
  }
  :deep(.el-table__body-wrapper td) {
    color: #e8f4ff;
  }
}
</style>
