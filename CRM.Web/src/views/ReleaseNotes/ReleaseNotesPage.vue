<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { marked } from 'marked'
import { helpAssetUrl } from '@/utils/helpDocPath'

marked.setOptions({ gfm: true, breaks: true })

const RELEASE_NOTES_REL = 'pages/版本更新日志_MENU_RELEASE_NOTES.md'

const loading = ref(true)
const missing = ref(false)
const html = ref('')

onMounted(async () => {
  loading.value = true
  missing.value = false
  html.value = ''
  try {
    const res = await fetch(helpAssetUrl(RELEASE_NOTES_REL), { cache: 'no-cache' })
    if (!res.ok) {
      missing.value = true
      return
    }
    const text = await res.text()
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
})
</script>

<template>
  <div class="release-notes-page">
    <header class="release-notes-header">
      <h1>版本更新日志</h1>
      <router-link class="back-link" to="/debug">Debug / 构建版本</router-link>
    </header>

    <main v-loading="loading" class="release-notes-body">
      <div v-if="missing && !loading" class="release-notes-empty">暂无版本说明</div>
      <article v-else-if="html" class="release-notes-md" v-html="html" />
    </main>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.release-notes-page {
  min-height: 100vh;
  background: $layer-1;
  color: $text-primary;
  padding: 32px 24px 48px;
}

.release-notes-header {
  max-width: 820px;
  margin: 0 auto 24px;
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  justify-content: space-between;
  gap: 12px;

  h1 {
    margin: 0;
    font-size: 24px;
    font-weight: 700;
  }
}

.back-link {
  font-size: 13px;
  color: $primary-color;
  text-decoration: none;
  font-weight: 600;

  &:hover {
    text-decoration: underline;
  }
}

.release-notes-body {
  max-width: 820px;
  margin: 0 auto;
  min-height: 200px;
}

.release-notes-empty {
  padding: 32px;
  text-align: center;
  color: $text-muted;
}

.release-notes-md :deep(h1) {
  font-size: 22px;
  font-weight: 600;
  margin: 0 0 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid $border-panel;
}

.release-notes-md :deep(h2) {
  font-size: 17px;
  font-weight: 600;
  margin: 20px 0 10px;
}

.release-notes-md :deep(h3) {
  font-size: 15px;
  font-weight: 600;
  margin: 14px 0 8px;
}

.release-notes-md :deep(p),
.release-notes-md :deep(li) {
  font-size: 14px;
  line-height: 1.65;
  color: $text-secondary;
}

.release-notes-md :deep(ul),
.release-notes-md :deep(ol) {
  padding-left: 1.35em;
  margin: 0 0 12px;
}

.release-notes-md :deep(a) {
  color: $primary-color;
}
</style>
