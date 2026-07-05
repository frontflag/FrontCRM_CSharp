<script setup lang="ts">
import { computed } from 'vue'
import { FRONTEND_DEBUG_VERSION } from './debugConstants'
import internalLogRaw from './internal-version-log.txt?raw'

const logLines = computed(() =>
  internalLogRaw
    .split('\n')
    .map((l) => l.trimEnd())
    .filter((l) => l.length > 0)
)
</script>

<template>
  <div class="debug-page">
    <div class="debug-header">
      <h1>内部版本日志</h1>
      <div class="debug-sub muted">
        每次 Git 提交自动追加一行（post-commit）；仅系统管理员可查看。
        <router-link class="debug-link" to="/debug">返回 Debug</router-link>
      </div>
    </div>

    <section class="debug-panel">
      <h2 class="panel-title">当前版本</h2>
      <div class="panel-body">
        <span class="meta-value mono version-strong">{{ FRONTEND_DEBUG_VERSION }}</span>
      </div>
    </section>

    <section class="debug-panel">
      <h2 class="panel-title">提交历史（新 → 旧）</h2>
      <pre v-if="logLines.length" class="log-pre mono">{{ logLines.join('\n') }}</pre>
      <div v-else class="debug-empty">尚无记录；下次提交后将自动写入。</div>
    </section>
  </div>
</template>

<style lang="scss" scoped>
.debug-page {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 20px;
  color: #303133;
}

.debug-header h1 {
  margin: 0;
  font-size: 20px;
  font-weight: 700;
}

.debug-sub {
  margin-top: 6px;
  font-size: 13px;
  color: #606266;
  line-height: 1.6;

  &.muted {
    color: #909399;
  }
}

.debug-link {
  color: var(--el-color-primary);
  text-decoration: none;
  font-weight: 600;
  margin-left: 8px;
}

.debug-link:hover {
  text-decoration: underline;
}

.debug-panel {
  padding: 16px 18px;
  border-radius: 10px;
  border: 1px solid var(--el-border-color-lighter);
  background: var(--el-bg-color);
  box-shadow: var(--el-box-shadow-light);
}

.panel-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
}

.panel-body {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
}

.version-strong {
  font-size: 16px;
  font-weight: 700;
  color: var(--el-color-primary);
}

.mono {
  font-family: ui-monospace, 'Cascadia Code', 'Consolas', monospace;
}

.log-pre {
  margin: 0;
  padding: 12px 14px;
  border-radius: 8px;
  border: 1px solid var(--el-border-color-lighter);
  background: var(--el-fill-color-light);
  font-size: 12px;
  line-height: 1.55;
  white-space: pre-wrap;
  word-break: break-word;
  max-height: min(70vh, 640px);
  overflow: auto;
}

.debug-empty {
  padding: 12px 14px;
  border-radius: 8px;
  border: 1px solid var(--el-border-color-lighter);
  background: var(--el-fill-color-light);
  color: #606266;
  font-size: 13px;
}
</style>
