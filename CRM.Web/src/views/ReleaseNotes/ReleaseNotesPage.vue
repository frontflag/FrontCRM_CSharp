<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { marked } from 'marked'
import { helpAssetUrl } from '@/utils/helpDocPath'

marked.setOptions({ gfm: true, breaks: true })

const RELEASE_NOTES_REL = 'pages/版本更新日志_MENU_RELEASE_NOTES.md'

type ReleaseNotePanel = {
  title: string
  html: string
}

const loading = ref(true)
const missing = ref(false)
const introHtml = ref('')
const panels = ref<ReleaseNotePanel[]>([])

/** 按 Markdown `##` 拆成面板；`##` 行作标题，至下一 `##` 之前为面板正文 */
function parseReleaseNotePanels(markdown: string): { intro: string; sections: ReleaseNotePanel[] } {
  const sections: ReleaseNotePanel[] = []
  const introLines: string[] = []
  let currentTitle = ''
  let currentBody: string[] = []
  let inSection = false

  for (const line of markdown.split('\n')) {
    const trimmed = line.trim()
    const h2 = /^##\s+(.+)$/.exec(trimmed)
    if (h2) {
      if (inSection && currentTitle) {
        sections.push({
          title: currentTitle,
          html: marked.parse(currentBody.join('\n').trim()) as string
        })
      }
      inSection = true
      currentTitle = h2[1].trim()
      currentBody = []
      continue
    }
    if (inSection) currentBody.push(line)
    else introLines.push(line)
  }

  if (inSection && currentTitle) {
    sections.push({
      title: currentTitle,
      html: marked.parse(currentBody.join('\n').trim()) as string
    })
  }

  const introRaw = introLines.join('\n').trim()
  return {
    intro: introRaw ? (marked.parse(introRaw) as string) : '',
    sections
  }
}

onMounted(async () => {
  loading.value = true
  missing.value = false
  introHtml.value = ''
  panels.value = []
  try {
    const res = await fetch(helpAssetUrl(RELEASE_NOTES_REL), { cache: 'no-cache' })
    if (!res.ok) {
      missing.value = true
      return
    }
    const text = await res.text()
    const { intro, sections } = parseReleaseNotePanels(text)
    const hasIntroText = intro.replace(/<[^>]+>/g, '').replace(/&nbsp;/g, ' ').trim().length > 0
    const hasPanels = sections.some((p) =>
      p.html.replace(/<[^>]+>/g, '').replace(/&nbsp;/g, ' ').trim().length > 0
    )
    if (!hasIntroText && !hasPanels) {
      missing.value = true
      return
    }
    introHtml.value = intro
    panels.value = sections.filter((p) =>
      p.html.replace(/<[^>]+>/g, '').replace(/&nbsp;/g, ' ').trim().length > 0
    )
    if (!hasIntroText && panels.value.length === 0) missing.value = true
  } catch {
    missing.value = true
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <!-- L1 页面 -->
  <div class="release-notes-page">
    <!-- L2 内容面板 -->
    <div class="release-notes-content">
      <header class="release-notes-header">
        <h1>版本更新日志</h1>
      </header>

      <main v-loading="loading" class="release-notes-body">
        <div v-if="missing && !loading" class="release-notes-empty">暂无版本说明</div>
        <template v-else-if="!loading">
          <div v-if="introHtml" class="release-notes-intro release-notes-md" v-html="introHtml" />
          <!-- L3 单条日志面板列表 -->
          <div v-if="panels.length" class="release-notes-log-list">
            <section
              v-for="(panel, index) in panels"
              :key="`${panel.title}-${index}`"
              class="item-panel-card"
            >
              <div class="item-panel-card__head">
                <span class="item-panel-card__idx">{{ panel.title }}</span>
              </div>
              <div class="item-panel-card__body release-notes-md" v-html="panel.html" />
            </section>
          </div>
        </template>
      </main>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

/** L1 页面 */
.release-notes-page {
  width: 100%;
  min-height: 100vh;
  background: #f2f2f7;
  color: $text-primary;
  padding: 32px 16px 48px;
}

/** L2 内容面板 */
.release-notes-content {
  width: 1000px;
  max-width: 100%;
  margin: 0 auto;
  background: #ffffff;
  padding: 24px 24px 32px;
}

.release-notes-header {
  margin-bottom: 20px;

  h1 {
    margin: 0;
    font-size: 22px;
    font-weight: 700;
    color: $text-primary;
  }
}

.release-notes-body {
  min-height: 120px;
}

.release-notes-empty {
  padding: 32px;
  text-align: center;
  color: $text-muted;
  background: var(--crm-detail-panel-card-bg);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
}

.release-notes-intro {
  margin-bottom: 12px;
}

/** L3 单条日志 — 对齐业务详情页 §7.3 item-panel-card */
.release-notes-log-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.item-panel-card {
  background: var(--crm-detail-panel-card-bg);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  padding: 0;
  overflow: hidden;
}

.item-panel-card__head {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  padding: 12px 16px;
  border-bottom: 1px solid $border-panel;
  background: var(--crm-detail-panel-card-head-bg);
}

.item-panel-card__idx {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}

.item-panel-card__body {
  padding: 12px 16px 16px;

  :deep(> :first-child) {
    margin-top: 0;
  }
}

.release-notes-md :deep(h1) {
  font-size: 18px;
  font-weight: 600;
  margin: 0 0 10px;
}

.release-notes-md :deep(h2) {
  font-size: 16px;
  font-weight: 600;
  margin: 14px 0 8px;
}

.release-notes-md :deep(h3) {
  font-size: 15px;
  font-weight: 600;
  margin: 12px 0 6px;
  color: $text-primary;
}

.release-notes-md :deep(p),
.release-notes-md :deep(li) {
  font-size: 14px;
  line-height: 1.65;
  color: $text-secondary;
}

.release-notes-md :deep(ul),
.release-notes-md :deep(ol) {
  margin: 0 0 8px;
  padding-left: 1.35em;
}

.release-notes-md :deep(a) {
  color: $primary-color;
}
</style>
