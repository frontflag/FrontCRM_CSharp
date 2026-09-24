<template>
  <div class="page">
    <h2>培训问答</h2>
    <p class="hint">以下内容来自新人培训教材，不是公司制度。</p>
    <el-input v-model="question" type="textarea" :rows="3" maxlength="500" placeholder="输入问题" />
    <div class="actions">
      <el-button type="primary" :loading="loading" @click="ask">提问</el-button>
    </div>
    <el-alert v-if="error" :title="error" type="error" show-icon />
    <div v-if="result" class="answer">
      <pre>{{ result.answer }}</pre>
      <p v-if="result.versionNo">教材版本 {{ result.versionNo }}<span v-if="result.fromCache"> · 缓存</span></p>
      <el-collapse v-if="result.citations?.length">
        <el-collapse-item v-for="(item, index) in result.citations" :key="index" :title="item.heading || '摘录'">
          <pre>{{ item.excerpt }}</pre>
        </el-collapse-item>
      </el-collapse>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { knowledgeBaseApi, type KbAskResult } from '@/api/knowledgeBase'

const question = ref('')
const loading = ref(false)
const error = ref('')
const result = ref<KbAskResult | null>(null)

async function ask() {
  error.value = ''
  result.value = null
  loading.value = true
  try {
    result.value = await knowledgeBaseApi.ask(question.value.trim())
  } catch (e) {
    error.value = e instanceof Error ? e.message : '提问失败'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.page { padding: 16px; max-width: 880px; }
.hint { color: #666; }
.actions { margin: 12px 0; }
.answer pre { white-space: pre-wrap; }
</style>
