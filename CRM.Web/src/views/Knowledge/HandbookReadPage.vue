<template>
  <div class="reader" v-loading="loading">
    <aside class="toc" :class="{ collapsed: !tocOpen }">
      <div class="toc-head">
        <div class="toc-tools">
          <button type="button" class="toc-toggle" :aria-label="tocOpen ? '收起目录' : '展开目录'" @click="tocOpen = !tocOpen">
            <el-icon><Fold v-if="tocOpen" /><Expand v-else /></el-icon>
          </button>
          <button
            v-show="tocOpen"
            type="button"
            class="toc-icon"
            :aria-label="allSectionsOpen ? '只显示一级目录' : '展开全部目录'"
            @click="toggleAllSections"
          >
            <el-icon><ArrowUp v-if="allSectionsOpen" /><ArrowDown v-else /></el-icon>
          </button>
        </div>
        <h2 v-show="tocOpen">{{ book?.title || '培训教材' }}</h2>
      </div>
      <template v-if="tocOpen">
        <p v-if="book" class="ver">版本 {{ book.versionNo }}</p>
        <el-alert v-if="error" :title="error" type="error" show-icon />
        <div v-for="chapter in book?.chapters ?? []" :key="chapter.anchor" class="chapter">
          <div class="chapter-row">
            <button type="button" class="chapter-btn" :class="{ current: current === chapter.anchor }" @click="go(chapter.anchor)">{{ chapter.title }}</button>
            <button
              v-if="chapter.sections.length"
              type="button"
              class="toc-icon chapter-fold"
              :aria-label="isChapterOpen(chapter.anchor) ? '收起本章' : '展开本章'"
              @click="toggleChapter(chapter.anchor)"
            >
              <el-icon><ArrowDown v-if="isChapterOpen(chapter.anchor)" /><ArrowRight v-else /></el-icon>
            </button>
          </div>
          <template v-if="isChapterOpen(chapter.anchor)">
            <button
              v-for="section in chapter.sections"
              :key="section.anchor"
              type="button"
              class="section-btn"
              :class="{ current: current === section.anchor }"
              @click="go(section.anchor)"
            >{{ section.title }}</button>
          </template>
        </div>
      </template>
    </aside>
    <article class="article">
      <section
        v-for="chapter in book?.chapters ?? []"
        :key="chapter.anchor"
        :id="chapter.anchor"
        class="block"
      >
        <h3>{{ chapter.title }}</h3>
        <div
          v-for="section in chapter.sections"
          :key="section.anchor"
          :id="section.anchor"
          class="section"
          :class="{ hit: current === section.anchor }"
        >
          <h4>{{ section.title }}</h4>
          <div class="prose">
            <p
              v-for="(block, index) in blocks(section.content, section.title)"
              :key="index"
              :class="block.kind"
            >
              <span v-if="block.tag" class="tag">{{ block.tag }}</span>{{ block.text }}
            </p>
          </div>
        </div>
      </section>
    </article>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { ArrowDown, ArrowRight, ArrowUp, Expand, Fold } from '@element-plus/icons-vue'
import { useRoute } from 'vue-router'
import { knowledgeBaseApi, type KbHandbookReader } from '@/api/knowledgeBase'

const route = useRoute()
const book = ref<KbHandbookReader | null>(null)
const loading = ref(false)
const error = ref('')
const current = ref('')
const tocOpen = ref(true)
const closedChapters = ref<string[]>([])

const allSectionsOpen = computed(() => {
  const anchors = book.value?.chapters.map(chapter => chapter.anchor) ?? []
  return anchors.length > 0 && anchors.every(anchor => !closedChapters.value.includes(anchor))
})

function isChapterOpen(anchor: string) {
  return !closedChapters.value.includes(anchor)
}

function toggleChapter(anchor: string) {
  closedChapters.value = isChapterOpen(anchor)
    ? [...closedChapters.value, anchor]
    : closedChapters.value.filter(item => item !== anchor)
}

function toggleAllSections() {
  const anchors = book.value?.chapters.map(chapter => chapter.anchor) ?? []
  closedChapters.value = allSectionsOpen.value ? anchors : []
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const version = typeof route.query.version === 'string' ? route.query.version : undefined
    book.value = await knowledgeBaseApi.reader(version)
    await nextTick()
    locate()
  } catch (e) {
    error.value = e instanceof Error ? e.message : '加载教材失败'
  } finally {
    loading.value = false
  }
}

const marked = new Set(['标准', '案例', '话术', '注意', '示例'])

function blocks(content: string, title: string) {
  const lines = content.replace(/\r\n/g, '\n').split('\n').map(line => line.trim()).filter(Boolean)
  const body = lines.length > 0 && isRepeatedHeading(lines[0], title) ? lines.slice(1) : lines
  return body.map(line => {
    const match = line.match(/^【([^】]{1,12})】(.*)$/)
    if (match && marked.has(match[1])) {
      return { kind: `mark mark-${match[1]}`, tag: match[1], text: match[2].trim() }
    }
    return { kind: 'text', tag: '', text: line }
  })
}

function isRepeatedHeading(line: string, title: string) {
  if (line === title.trim()) return true
  return /^第\d+章/.test(line) || line.startsWith('附录')
}

function go(anchor: string) {
  current.value = anchor
  const el = document.getElementById(anchor)
  el?.scrollIntoView({ block: 'start' })
  history.replaceState(null, '', `#${anchor}`)
}

function locate() {
  const anchor = route.hash.replace(/^#/, '')
  if (!anchor) return
  current.value = anchor
  document.getElementById(anchor)?.scrollIntoView({ block: 'start' })
}

onMounted(load)
watch(() => route.query.version, load)
watch(() => route.hash, () => nextTick(locate))
</script>

<style scoped>
.reader { display: flex; height: calc(100vh - 108px); }
.toc {
  width: 420px; flex: 0 0 420px; overflow: auto;
  border-right: 1px solid #e5e7eb; padding: 14px 16px 24px;
  background: #fff; transition: width 0.2s ease, flex-basis 0.2s ease;
}
.toc.collapsed { width: 56px; flex-basis: 56px; padding: 14px 8px; overflow: hidden; }
.toc-head { display: flex; flex-direction: column; align-items: stretch; gap: 8px; margin-bottom: 8px; }
.toc-tools { display: flex; align-items: center; justify-content: space-between; }
.toc h2 { margin: 0; font-size: 16px; line-height: 1.45; font-weight: 650; }
.toc-toggle {
  display: inline-flex; align-items: center; justify-content: center;
  width: 32px; height: 32px; border: 1px solid #d7dbe3; background: #f8fafc; color: #374151;
  border-radius: 6px; padding: 0; cursor: pointer; font-size: 18px;
}
.toc.collapsed .toc-head { align-items: center; }
.ver { color: #6b7280; margin: 0 0 12px; font-size: 13px; }
.chapter { margin-bottom: 10px; }
.chapter-row { display: flex; align-items: flex-start; gap: 4px; }
.chapter-btn, .section-btn {
  display: block; width: 100%; text-align: left; border: 0; background: transparent;
  cursor: pointer; border-radius: 6px; line-height: 1.45;
}
.chapter-btn { flex: 1; font-weight: 650; padding: 6px 8px; color: #111827; }
.toc-icon {
  display: inline-flex; align-items: center; justify-content: center;
  width: 28px; height: 28px; border: 0; background: transparent; color: #4b5563;
  padding: 0; cursor: pointer; font-size: 16px;
}
.toc-icon:hover { color: #111827; }
.chapter-fold,
.chapter-fold:hover,
.chapter-fold .el-icon { color: #a5aab1; }
.chapter-fold { flex: 0 0 auto; margin-top: 2px; }
.section-btn { padding: 5px 8px 5px 22px; color: #4b5563; font-size: 13.5px; }
.chapter-btn.current, .section-btn.current { background: #eef4ff; color: #1d4ed8; }
.article {
  flex: 1; overflow: auto; padding: 28px 40px 72px;
  background: #f4f6f8;
}
.block { width: 100%; margin: 0 0 8px; }
.block h3 {
  margin: 28px 0 14px; font-size: 22px; font-weight: 650; color: #111827;
  letter-spacing: 0.01em;
}
.section {
  background: #fff; border: 1px solid #e6e8ec; border-radius: 10px;
  padding: 18px 28px 8px; margin-bottom: 16px;
}
.section h4 { margin: 0 0 12px; font-size: 16px; font-weight: 650; color: #1f2937; }
.section.hit { border-color: #f5c16c; box-shadow: 0 0 0 3px #fff4df; }
.prose p { margin: 0 0 14px; font-size: 15.5px; line-height: 1.85; color: #1f2937; }
.mark {
  padding: 10px 14px 10px 16px; border-radius: 8px;
  background: #f8fafc; border-left: 3px solid #94a3b8;
}
.mark-标准 { background: #f3f7f4; border-left-color: #3f8f6b; }
.mark-案例 { background: #fbf6ee; border-left-color: #c4842a; }
.mark-话术 { background: #f3f6fb; border-left-color: #3b6fb6; }
.mark-注意 { background: #fdf4f4; border-left-color: #c45c5c; }
.mark-示例 { background: #f4f7fb; border-left-color: #5b7c99; }
.tag {
  display: inline-block; margin-right: 8px; padding: 0 6px;
  border-radius: 4px; font-size: 12px; line-height: 20px; vertical-align: 1px;
  color: #fff; background: #64748b;
}
.mark-标准 .tag { background: #3f8f6b; }
.mark-案例 .tag { background: #c4842a; }
.mark-话术 .tag { background: #3b6fb6; }
.mark-注意 .tag { background: #c45c5c; }
.mark-示例 .tag { background: #5b7c99; }
</style>
