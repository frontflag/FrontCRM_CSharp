<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { marked } from 'marked'
import {
  helpAssetUrl,
  helpCatalogRelativePath,
  helpDocRelativePathForRoute,
  resolveHelpLinkHref
} from '@/utils/helpDocPath'

marked.setOptions({ gfm: true, breaks: true })

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
/** 相对 /help/ 根的当前文档路径（如 pages/xxx.md 或 帮助文档目录.md） */
const activeRel = ref(helpCatalogRelativePath())

async function loadDoc() {
  const url = helpAssetUrl(activeRel.value)
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

function onPanelClick(ev: MouseEvent) {
  const a = (ev.target as HTMLElement | null)?.closest?.('a') as HTMLAnchorElement | null
  if (!a) return
  const href = a.getAttribute('href')
  if (!href) return
  const rel = resolveHelpLinkHref(href, activeRel.value)
  if (!rel) return
  ev.preventDefault()
  ev.stopPropagation()
  activeRel.value = rel
  void loadDoc()
}

watch(
  () => [route.path, route.name, route.fullPath] as const,
  () => {
    activeRel.value = helpDocRelativePathForRoute(route) ?? helpCatalogRelativePath()
    void loadDoc()
  },
  { immediate: true }
)
</script>

<template>
  <div class="help-manual-panel" v-loading="loading" @click.capture="onPanelClick">
    <div v-if="missing && !loading" class="help-manual-panel__empty">暂无帮助</div>
    <div v-else-if="!loading && html" class="help-manual-panel__body help-md" v-html="html" />
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.help-manual-panel {
  min-height: 120px;
  font-size: 12px;
  color: $text-secondary;
}

.help-manual-panel__empty {
  padding: 24px 8px;
  text-align: center;
  color: $text-muted;
  font-size: 13px;
}

.help-manual-panel__body {
  padding: 4px 2px 16px;
  line-height: 1.55;
  word-break: break-word;
}

.help-md :deep(h1) {
  font-size: 19px;
  font-weight: 600;
  color: $text-primary;
  margin: 0 0 10px;
  padding-bottom: 6px;
  border-bottom: 1px solid $border-panel;
}

.help-md :deep(h1.help-h1--offset-down) {
  margin-top: 8px;
}

.help-md :deep(h2) {
  font-size: 16px;
  font-weight: 400;
  color: $text-primary;
  margin: 14px 0 8px;
}

.help-md :deep(h3) {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
  margin: 10px 0 6px;
}

.help-md :deep(p) {
  margin: 0 0 8px;
  color: $text-secondary;
}

.help-md :deep(ul),
.help-md :deep(ol) {
  margin: 0 0 8px;
  padding-left: 1.25em;
  color: $text-secondary;
}

.help-md :deep(li) {
  margin-bottom: 4px;
}

.help-md :deep(code) {
  font-family: ui-monospace, monospace;
  font-size: 11px;
  padding: 1px 4px;
  border-radius: 4px;
  background: var(--crm-accent-008);
  color: $text-primary;
  border: 1px solid $border-panel;
}

.help-md :deep(pre) {
  margin: 8px 0;
  padding: 8px 10px;
  overflow-x: auto;
  border-radius: 6px;
  background: $layer-3;
  border: 1px solid $border-panel;
  color: $text-primary;
}

.help-md :deep(pre code) {
  padding: 0;
  background: none;
  border: none;
  color: inherit;
}

.help-md :deep(a) {
  color: $cyan-primary;
  text-decoration: underline;
  text-underline-offset: 2px;
  cursor: pointer;
}

.help-md :deep(blockquote) {
  margin: 8px 0;
  padding: 6px 10px;
  border-left: 3px solid var(--crm-accent-018);
  background: var(--crm-accent-005);
  color: $text-secondary;
}

.help-md :deep(table) {
  width: 100%;
  border-collapse: collapse;
  font-size: 11px;
  margin: 8px 0;
  color: $text-secondary;
}

.help-md :deep(th),
.help-md :deep(td) {
  border: 1px solid $border-panel;
  padding: 4px 6px;
  text-align: left;
}

.help-md :deep(th) {
  background: var(--crm-table-header-bg);
  color: $text-primary;
}

/* 操作说明：单列多块（标题 + 说明 + 前置条件） */
.help-md :deep(.help-op-block) {
  margin: 12px 0;
  padding: 10px 12px;
  border-radius: 8px;
  border: 1px solid $border-panel;
  background: $layer-3;
}

.help-md :deep(.help-op-block > p) {
  margin: 0 0 6px;
  font-size: 12px;
  color: $text-secondary;
}

.help-md :deep(.help-op-block > p:last-child) {
  margin-bottom: 0;
}

.help-md :deep(.help-op-block > p:first-of-type) {
  margin-bottom: 8px;
}

.help-md :deep(.help-op-block > p:first-of-type strong) {
  font-size: 13px;
  font-weight: 700;
  color: $text-primary;
}
</style>
