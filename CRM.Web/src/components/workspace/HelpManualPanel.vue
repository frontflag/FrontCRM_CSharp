<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { marked } from 'marked'
import { helpMarkdownFetchUrl } from '@/utils/helpDocPath'

marked.setOptions({ gfm: true, breaks: true })

/**
 * 开发/部署环境下请求不存在的 .md 时，常 200 返回 SPA 的 index.html，不能当帮助正文。
 */
function isInvalidHelpFetchBody(raw: string, contentType: string | null): boolean {
  const trimmed = raw.trim()
  if (!trimmed) return true

  const ct = (contentType || '').toLowerCase()
  if (ct.includes('text/html')) return true

  const probe = trimmed.slice(0, 4000)
  if (!/^[\s]*</.test(probe)) return false

  const low = probe.toLowerCase()
  if (low.includes('<!doctype html') || /\b<html[\s>]/.test(low)) return true
  if (low.includes('<meta charset')) return true
  if (/<title>\s*vite/i.test(probe) || /<title>\s*[^<]*vite\s*\+\s*vue/i.test(probe)) return true
  if (/\/@vite\/client|@vite\/client/.test(probe)) return true
  return false
}

const route = useRoute()
const loading = ref(false)
const html = ref('')
const missing = ref(false)

async function loadDoc() {
  const url = helpMarkdownFetchUrl(route)
  if (!url) {
    missing.value = true
    html.value = ''
    return
  }
  loading.value = true
  missing.value = false
  html.value = ''
  try {
    const res = await fetch(url, { cache: 'no-cache' })
    if (!res.ok) {
      missing.value = true
      return
    }
    const text = await res.text()
    if (isInvalidHelpFetchBody(text, res.headers.get('content-type'))) {
      missing.value = true
      return
    }
    const parsed = marked.parse(text) as string
    const textOnly = parsed.replace(/<[^>]+>/g, '').replace(/&nbsp;/g, ' ').trim()
    if (!textOnly) {
      missing.value = true
      return
    }
    html.value = parsed
  } catch {
    missing.value = true
  } finally {
    loading.value = false
  }
}

watch(
  () => [route.path, route.name, route.fullPath] as const,
  () => {
    void loadDoc()
  },
  { immediate: true }
)
</script>

<template>
  <div class="help-manual-panel" v-loading="loading">
    <div v-if="missing && !loading" class="help-manual-panel__empty">暂无帮助</div>
    <div v-else-if="!loading && html" class="help-manual-panel__body help-md" v-html="html" />
  </div>
</template>

<style scoped lang="scss">
.help-manual-panel {
  min-height: 120px;
  font-size: 12px;
  color: rgba(210, 225, 240, 0.92);
}

.help-manual-panel__empty {
  padding: 24px 8px;
  text-align: center;
  color: rgba(140, 180, 210, 0.75);
  font-size: 13px;
}

.help-manual-panel__body {
  padding: 4px 2px 16px;
  line-height: 1.55;
  word-break: break-word;
}

.help-md :deep(h1) {
  font-size: 15px;
  font-weight: 600;
  color: #e8f4ff;
  margin: 0 0 10px;
  padding-bottom: 6px;
  border-bottom: 1px solid rgba(0, 212, 255, 0.15);
}

.help-md :deep(h2) {
  font-size: 13px;
  font-weight: 600;
  color: rgba(180, 220, 255, 0.95);
  margin: 14px 0 8px;
}

.help-md :deep(h3) {
  font-size: 12px;
  font-weight: 600;
  color: rgba(160, 200, 230, 0.9);
  margin: 10px 0 6px;
}

.help-md :deep(p) {
  margin: 0 0 8px;
  color: rgba(200, 220, 240, 0.88);
}

.help-md :deep(ul),
.help-md :deep(ol) {
  margin: 0 0 8px;
  padding-left: 1.25em;
}

.help-md :deep(li) {
  margin-bottom: 4px;
}

.help-md :deep(code) {
  font-family: ui-monospace, monospace;
  font-size: 11px;
  padding: 1px 4px;
  border-radius: 4px;
  background: rgba(0, 212, 255, 0.08);
  color: rgba(190, 230, 255, 0.95);
}

.help-md :deep(pre) {
  margin: 8px 0;
  padding: 8px 10px;
  overflow-x: auto;
  border-radius: 6px;
  background: rgba(0, 0, 0, 0.35);
  border: 1px solid rgba(0, 212, 255, 0.12);
}

.help-md :deep(pre code) {
  padding: 0;
  background: none;
}

.help-md :deep(a) {
  color: #5ec8ff;
  text-decoration: underline;
  text-underline-offset: 2px;
}

.help-md :deep(blockquote) {
  margin: 8px 0;
  padding: 6px 10px;
  border-left: 3px solid rgba(0, 212, 255, 0.35);
  background: rgba(0, 212, 255, 0.05);
  color: rgba(190, 210, 230, 0.85);
}

.help-md :deep(table) {
  width: 100%;
  border-collapse: collapse;
  font-size: 11px;
  margin: 8px 0;
}

.help-md :deep(th),
.help-md :deep(td) {
  border: 1px solid rgba(0, 212, 255, 0.12);
  padding: 4px 6px;
  text-align: left;
}

.help-md :deep(th) {
  background: rgba(0, 212, 255, 0.06);
}
</style>
