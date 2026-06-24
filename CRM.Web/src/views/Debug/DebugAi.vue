<template>
  <div class="debug-page">
    <div class="debug-header">
      <h1>AI 物料规格查询</h1>
      <div class="debug-sub muted">
        场景 <code>material.spec.lookup</code>（默认 Mock 厂商）。输入 PN + 品牌后调用 AI，结果只读展示，可复制 JSON。
        <router-link class="debug-link" to="/debug">返回 Debug</router-link>
      </div>
    </div>

    <section class="debug-panel">
      <h2 class="panel-title">查询参数</h2>
      <div class="panel-body form-grid">
        <el-form label-width="80px" @submit.prevent>
          <el-form-item label="PN">
            <el-input v-model="pn" placeholder="如 LM358N" clearable />
          </el-form-item>
          <el-form-item label="品牌">
            <el-input v-model="brand" placeholder="如 TI" clearable />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" :loading="loading" :disabled="!canSubmit" @click="onInvoke">
              查询规格
            </el-button>
          </el-form-item>
        </el-form>
      </div>
    </section>

    <section v-if="result" class="debug-panel">
      <h2 class="panel-title">
        查询结果
        <el-tag v-if="result.fromCache" size="small" type="info" class="cache-tag">缓存</el-tag>
        <el-tag v-else size="small" type="success" class="cache-tag">实时</el-tag>
      </h2>
      <div class="panel-body meta-row">
        <span>厂商：{{ result.providerCode }}</span>
        <span>模型：{{ result.model }}</span>
        <span v-if="result.usage">Token：{{ result.usage.totalTokens }}</span>
        <span>耗时日志 ID：{{ result.invocationId }}</span>
      </div>
      <div class="panel-body result-actions">
        <el-button size="small" @click="copyJson">复制 JSON</el-button>
      </div>
      <pre class="result-json">{{ formattedJson }}</pre>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { aiApi, AI_SCENARIO_MATERIAL_SPEC_LOOKUP, type AiInvokeResult } from '@/api/ai'
import { getApiErrorMessage } from '@/utils/apiError'

const pn = ref('')
const brand = ref('')
const loading = ref(false)
const result = ref<AiInvokeResult | null>(null)

const canSubmit = computed(() => pn.value.trim().length > 0 && brand.value.trim().length > 0)

const formattedJson = computed(() => {
  if (!result.value) return ''
  const data = result.value.data ?? tryParseJson(result.value.content)
  return JSON.stringify(data ?? { content: result.value.content }, null, 2)
})

function tryParseJson(text: string): unknown {
  try {
    return JSON.parse(text)
  } catch {
    return null
  }
}

async function onInvoke() {
  if (!canSubmit.value) return
  loading.value = true
  result.value = null
  try {
    result.value = await aiApi.invoke({
      scenarioCode: AI_SCENARIO_MATERIAL_SPEC_LOOKUP,
      input: { pn: pn.value.trim(), brand: brand.value.trim() }
    })
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, 'AI 调用失败'))
  } finally {
    loading.value = false
  }
}

async function copyJson() {
  if (!formattedJson.value) return
  try {
    await navigator.clipboard.writeText(formattedJson.value)
    ElMessage.success('已复制到剪贴板')
  } catch {
    ElMessage.error('复制失败')
  }
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

.cache-tag {
  margin-left: 4px;
}

.form-grid {
  max-width: 480px;
}

.meta-row {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  font-size: 13px;
  color: #606266;
  margin-bottom: 8px;
}

.result-json {
  margin: 0;
  padding: 12px;
  background: #f5f7fa;
  border-radius: 6px;
  font-size: 13px;
  line-height: 1.5;
  overflow: auto;
  max-height: 480px;
}

.result-actions {
  margin-bottom: 8px;
}
</style>
