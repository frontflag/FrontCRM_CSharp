<template>
  <div class="debug-page">
    <div class="debug-header">
      <h1>AI 物料情报（契约 v2 对照）</h1>
      <div class="debug-sub muted">
        场景 <code>material.intel.lookup</code>。左侧为 RFQ 同款结构化预览，右侧为原始 JSON；底部为契约 v2 校验报告。
        <router-link class="debug-link" to="/debug">返回 Debug</router-link>
      </div>
    </div>

    <section class="debug-panel">
      <h2 class="panel-title">查询 / 粘贴 JSON</h2>
      <div class="panel-body form-row">
        <el-input v-model="pn" placeholder="型号 PN，如 HMCG94AGBRA632N" clearable class="pn-input" />
        <el-button type="primary" :loading="loading" :disabled="!canInvoke" @click="onInvoke">
          AI 查询
        </el-button>
        <el-button :disabled="!pasteJson.trim()" @click="onParsePaste">解析粘贴 JSON</el-button>
      </div>
      <el-input
        v-model="pasteJson"
        type="textarea"
        :rows="4"
        placeholder="可选：粘贴 AI 返回的 JSON 做离线对照（不调用 API）"
        class="paste-area"
      />
    </section>

    <section v-if="parsedData" class="debug-panel">
      <div class="panel-head">
        <h2 class="panel-title">
          对照预览
          <el-tag v-if="fromCache" size="small" type="info">缓存</el-tag>
          <el-tag v-else-if="invoked" size="small" type="success">实时</el-tag>
        </h2>
        <el-button size="small" @click="copyRawJson">复制原始 JSON</el-button>
      </div>

      <div class="compare-grid">
        <div class="compare-col">
          <h3 class="compare-col__title">结构化预览</h3>
          <MaterialIntelResultPanel :data="parsedData" :from-cache="fromCache" />
        </div>
        <div class="compare-col">
          <h3 class="compare-col__title">原始 JSON</h3>
          <pre class="raw-json">{{ formattedJson }}</pre>
        </div>
      </div>

      <div class="validation-block">
        <h3 class="compare-col__title">
          契约 v2 校验
          <el-tag :type="validation.valid ? 'success' : 'warning'" size="small">
            {{ validation.valid ? '通过' : '有问题' }}
          </el-tag>
        </h3>
        <p v-if="!validation.issues.length" class="validation-ok">与契约 v2 一致，未发现异常。</p>
        <ul v-else class="validation-list">
          <li
            v-for="(issue, idx) in validation.issues"
            :key="idx"
            :class="'sev-' + issue.severity"
          >
            <code>{{ issue.path || '$' }}</code>
            <span class="issue-code">{{ issue.code }}</span>
            {{ issue.message }}
          </li>
        </ul>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import MaterialIntelResultPanel from '@/components/RFQ/MaterialIntelResultPanel.vue'
import { aiApi, AI_SCENARIO_MATERIAL_INTEL_LOOKUP, type AiInvokeResult } from '@/api/ai'
import { getApiErrorMessage } from '@/utils/apiError'
import { parseAiJsonObject } from '@/utils/aiJson'
import { copyTextToClipboard } from '@/utils/clipboard'
import { validateMaterialIntelJson } from '@/utils/materialIntelSchema'

const pn = ref('')
const pasteJson = ref('')
const loading = ref(false)
const invoked = ref(false)
const fromCache = ref(false)
const rawContent = ref('')
const parsedData = ref<Record<string, unknown> | null>(null)

const canInvoke = computed(() => pn.value.trim().length > 0)

const formattedJson = computed(() => {
  if (!parsedData.value) return ''
  return JSON.stringify(parsedData.value, null, 2)
})

const validation = computed(() => validateMaterialIntelJson(parsedData.value))

watch(parsedData, () => {
  /* validation reactive via computed */
})

function applyParsed(data: Record<string, unknown> | null, content: string, cache: boolean, didInvoke: boolean) {
  parsedData.value = data
  rawContent.value = content
  fromCache.value = cache
  invoked.value = didInvoke
}

async function onInvoke() {
  if (!canInvoke.value) return
  loading.value = true
  parsedData.value = null
  try {
    const result: AiInvokeResult = await aiApi.invoke({
      scenarioCode: AI_SCENARIO_MATERIAL_INTEL_LOOKUP,
      input: { pn: pn.value.trim() }
    })
    const data = parseAiJsonObject(result.data, result.content)
    if (!data) {
      ElMessage.warning('AI 返回内容无法解析为 JSON 对象')
      applyParsed(null, result.content, result.fromCache, true)
      return
    }
    pasteJson.value = JSON.stringify(data, null, 2)
    applyParsed(data, result.content, result.fromCache, true)
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, 'AI 调用失败'))
  } finally {
    loading.value = false
  }
}

function onParsePaste() {
  const text = pasteJson.value.trim()
  if (!text) return
  try {
    const data = parseAiJsonObject(null, text)
    if (!data) {
      ElMessage.error('无法解析为 JSON 对象')
      return
    }
    applyParsed(data, text, false, false)
    ElMessage.success('已加载粘贴 JSON')
  } catch {
    ElMessage.error('JSON 格式无效')
  }
}

async function copyRawJson() {
  if (!formattedJson.value) return
  if (copyTextToClipboard(formattedJson.value)) {
    ElMessage.success('已复制到剪贴板')
    return
  }
  if (typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(formattedJson.value)
      ElMessage.success('已复制到剪贴板')
      return
    } catch {
      /* fall through */
    }
  }
  ElMessage.error('复制失败')
}
</script>

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
  line-height: 1.6;
  color: #909399;
}

.debug-link {
  margin-left: 8px;
  color: var(--el-color-primary);
}

.debug-panel {
  border: 1px solid #ebeef5;
  border-radius: 8px;
  padding: 16px 20px;
  background: #fff;
}

.panel-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 8px;
}

.panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;

  .panel-title {
    margin: 0;
  }
}

.form-row {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
  margin-bottom: 12px;
}

.pn-input {
  max-width: 320px;
}

.paste-area {
  font-family: ui-monospace, monospace;
  font-size: 12px;
}

.compare-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
  align-items: start;
}

@media (max-width: 1100px) {
  .compare-grid {
    grid-template-columns: 1fr;
  }
}

.compare-col__title {
  margin: 0 0 10px;
  font-size: 14px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 8px;
}

.raw-json {
  margin: 0;
  padding: 12px;
  background: #f5f7fa;
  border-radius: 6px;
  font-size: 12px;
  line-height: 1.5;
  overflow: auto;
  max-height: 640px;
}

.validation-block {
  margin-top: 20px;
  padding-top: 16px;
  border-top: 1px solid #ebeef5;
}

.validation-ok {
  margin: 0;
  font-size: 13px;
  color: #67c23a;
}

.validation-list {
  margin: 0;
  padding: 0;
  list-style: none;
  font-size: 13px;
  line-height: 1.7;

  li {
    padding: 4px 0;
    border-bottom: 1px solid #f2f3f5;
  }

  code {
    margin-right: 6px;
    font-size: 12px;
  }

  .issue-code {
    margin-right: 6px;
    color: #909399;
    font-size: 11px;
  }

  .sev-error {
    color: #f56c6c;
  }

  .sev-warn {
    color: #e6a23c;
  }

  .sev-info {
    color: #909399;
  }
}

:deep(.material-intel-panel) {
  max-width: none;
  margin: 0;
}
</style>
