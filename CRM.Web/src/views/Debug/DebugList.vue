<template>
  <div class="debug-page">
    <div class="debug-header">
      <h1>Debug</h1>
      <div class="debug-sub">
        当前前端构建版本：<span class="mono version-strong">{{ FRONTEND_DEBUG_VERSION }}</span>
        （来自 package.json，每次 <code>npm run build</code> 写入；用于核对线上是否为本次构建）
      </div>
      <div class="debug-sub muted">按 PRD：数据库连接（后端脱敏）、debug 表记录；版本号见上。</div>
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
        <CrmDataTable
          embedded
          class="debug-table"
          column-layout-key="debug-records"
          :columns="debugTableColumns"
          :data="items"
        >
          <template #col-value="{ row }">
            <span class="mono">{{ row.value }}</span>
          </template>
        </CrmDataTable>
        <div v-if="items.length === 0" class="debug-empty">没有 debug 记录</div>
      </section>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getDebugPage } from '@/api/debug'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

/** debug 页面展示版本号（按发布批次手动维护） */
const FRONTEND_DEBUG_VERSION = '1.1.0412-19:00'

/** 可配置列示例（列设置 / 顺序 / localStorage 持久化） */
const debugTableColumns: CrmTableColumnDef[] = [
  { key: 'name', label: 'Name', prop: 'name', minWidth: 200 },
  { key: 'value', label: 'Value', prop: 'value', minWidth: 320, showOverflowTooltip: true }
]

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

.debug-panel {
  padding: 16px 18px;
  border-radius: 10px;
  border: 1px solid rgba(0, 212, 255, 0.2);
  background: rgba(0, 212, 255, 0.06);
}

/* 记录面板在空数据时提升底色和边界对比 */
.panel-records {
  border-color: rgba(0, 212, 255, 0.2);
  background: rgba(0, 212, 255, 0.06);
  box-shadow: none;
}

.panel-records .panel-title {
  color: #e8f4ff;
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
  padding: 12px 14px;
  border-radius: 8px;
  border: 1px solid rgba(0, 212, 255, 0.55);
  background: rgba(0, 212, 255, 0.14);
  color: rgba(242, 250, 255, 0.98);
  font-size: 13px;
  font-weight: 500;
}

.debug-table {
  :deep(.crm-data-table-root) {
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.12);
    border-radius: 10px;
    padding: 6px 8px;
  }
  :deep(.el-table__inner-wrapper::before) {
    background-color: rgba(255, 255, 255, 0.16);
  }
  :deep(.el-table__header-wrapper th) {
    background: rgba(255, 255, 255, 0.06);
    color: rgba(225, 238, 250, 0.95);
  }
  :deep(.el-table__body-wrapper td) {
    color: #e8f4ff;
  }
  :deep(.el-table__empty-block) {
    min-height: 116px;
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(255, 255, 255, 0.16);
    border-radius: 8px;
    margin: 8px 0;
  }
  :deep(.el-table__empty-text) {
    color: rgba(220, 235, 250, 0.92);
    font-weight: 500;
  }
}
</style>
