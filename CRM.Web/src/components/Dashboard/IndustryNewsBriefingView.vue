<template>
  <div class="news-briefing" :class="`is-${variant}`">
    <div ref="scrollRef" class="news-briefing__scroll" @scroll.passive="syncActiveFloor">
      <p v-if="staleHint" class="news-briefing__stale">{{ staleHint }}</p>
      <div class="news-briefing__md" v-html="decoratedHtml" />
    </div>
    <nav
      v-if="floors.length"
      class="news-briefing__floors"
      :aria-label="t('dashboard.industryNews.floorNav')"
    >
      <p class="news-briefing__floors-title">{{ t('dashboard.industryNews.floorNav') }}</p>
      <button
        v-for="floor in floors"
        :key="floor.id"
        type="button"
        class="news-briefing__floor"
        :class="{ 'is-active': floor.id === activeFloorId, 'is-sub': floor.level === 3 }"
        @click="scrollToFloor(floor.id)"
      >
        {{ floor.title }}
      </button>
    </nav>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  decorateIndustryNewsHtml,
  normalizeIndustryNewsMarkdown
} from '@/utils/industryNewsMarkdown'
import { renderAnnouncementMarkdown } from '@/utils/sanitizeAnnouncementHtml'

const props = withDefaults(
  defineProps<{
    markdown: string
    staleHint?: string
    variant?: 'dialog' | 'page'
  }>(),
  { staleHint: '', variant: 'dialog' }
)

const { t } = useI18n()
const scrollRef = ref<HTMLElement | null>(null)
const activeFloorId = ref('')

const renderedMarkdown = computed(() =>
  renderAnnouncementMarkdown(normalizeIndustryNewsMarkdown(props.markdown || ''))
)
const decorated = computed(() => decorateIndustryNewsHtml(renderedMarkdown.value))
const decoratedHtml = computed(() => decorated.value.html)
const floors = computed(() => decorated.value.floors)

function offsetInScroll(root: HTMLElement, el: HTMLElement) {
  return el.getBoundingClientRect().top - root.getBoundingClientRect().top + root.scrollTop
}

function syncActiveFloor() {
  const root = scrollRef.value
  if (!root || floors.value.length === 0) return
  const y = root.scrollTop + 24
  let current = floors.value[0].id
  for (const floor of floors.value) {
    const el = root.querySelector(`#${floor.id}`) as HTMLElement | null
    if (el && offsetInScroll(root, el) <= y) current = floor.id
  }
  activeFloorId.value = current
}

function scrollToFloor(id: string) {
  const root = scrollRef.value
  const el = root?.querySelector(`#${id}`) as HTMLElement | null
  if (!root || !el) return
  activeFloorId.value = id
  root.scrollTo({ top: Math.max(0, offsetInScroll(root, el) - 8), behavior: 'smooth' })
}

async function resetScroll() {
  await nextTick()
  scrollRef.value?.scrollTo({ top: 0 })
  activeFloorId.value = floors.value[0]?.id ?? ''
}

watch(() => props.markdown, resetScroll)

defineExpose({ resetScroll })
</script>

<style lang="scss" scoped>
.news-briefing {
  display: flex;
  align-items: stretch;
  gap: 8px;
  min-height: 0;
  &.is-dialog {
    min-height: 280px;
    max-height: min(70vh, 640px);
  }
  &.is-page {
    flex: 1;
    height: 100%;
  }
}

.news-briefing__scroll {
  flex: 1;
  min-width: 0;
  overflow: auto;
  padding-right: 8px;
}

.news-briefing__floors {
  flex: 0 0 148px;
  display: flex;
  flex-direction: column;
  gap: 2px;
  overflow: auto;
  padding: 0 0 0 14px;
  border-left: 1px solid var(--el-border-color-lighter);
}

.news-briefing__floors-title {
  margin: 0 0 6px;
  font-size: 11px;
  font-weight: 600;
  color: var(--el-text-color-secondary);
}

.news-briefing__floor {
  display: block;
  width: 100%;
  padding: 5px 0;
  border: 0;
  background: none;
  text-align: left;
  font-size: 12px;
  line-height: 1.35;
  color: var(--el-text-color-regular);
  cursor: pointer;
  &.is-sub {
    padding-left: 10px;
    color: var(--el-text-color-secondary);
  }
  &.is-active {
    color: var(--el-color-primary);
    font-weight: 600;
  }
  &:hover:not(.is-active) {
    color: var(--el-color-primary);
  }
}

.news-briefing__stale {
  margin: 0 0 12px;
  font-size: 12px;
  color: var(--el-color-warning);
}

.news-briefing__md {
  font-size: 13px;
  line-height: 1.7;
  color: var(--el-text-color-primary);
  :deep(h2),
  :deep(h3) {
    scroll-margin-top: 8px;
  }
  :deep(h2) {
    margin: 18px 0 8px;
    font-size: 16px;
    color: #8d4e16;
  }
  :deep(h3) {
    margin: 14px 0 6px;
    font-size: 14px;
    font-weight: 600;
  }
  :deep(h2:first-child) {
    margin-top: 0;
  }
  :deep(p) {
    margin: 0 0 8px;
  }
  :deep(ul) {
    margin: 0 0 8px;
    padding-left: 1.15em;
    list-style: disc;
  }
  :deep(li) {
    margin: 0 0 6px;
  }
  :deep(table) {
    width: 100%;
    border-collapse: collapse;
    margin: 8px 0 12px;
    font-size: 12px;
  }
  :deep(th),
  :deep(td) {
    border: 1px solid var(--el-border-color-lighter);
    padding: 6px 8px;
    text-align: left;
  }
}

@media (max-width: 640px) {
  .news-briefing__floors {
    display: none;
  }
}
</style>
