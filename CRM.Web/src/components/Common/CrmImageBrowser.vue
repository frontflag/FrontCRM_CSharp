<template>
  <Teleport to="body">
    <div
      v-if="visible"
      class="crm-image-browser"
      role="dialog"
      aria-modal="true"
      :aria-label="title || t('imageBrowser.defaultTitle')"
    >
      <header class="crm-image-browser__top">
        <h2 class="crm-image-browser__title">{{ title || t('imageBrowser.defaultTitle') }}</h2>
        <div class="crm-image-browser__top-tools">
          <button type="button" class="crm-image-browser__btn" :disabled="!canZoomOut" @click="zoomOut">
            {{ t('imageBrowser.zoomOut') }}
          </button>
          <button type="button" class="crm-image-browser__btn" :disabled="!canZoomIn" @click="zoomIn">
            {{ t('imageBrowser.zoomIn') }}
          </button>
          <button
            type="button"
            class="crm-image-browser__btn"
            :class="{ 'crm-image-browser__btn--active': viewMode === 'scale' && Math.abs(scale - 1) < 1e-6 }"
            @click="setViewPercent100"
          >
            {{ t('imageBrowser.view100') }}
          </button>
          <button
            type="button"
            class="crm-image-browser__btn"
            :class="{ 'crm-image-browser__btn--active': viewMode === 'fit-window' }"
            @click="setViewFitWindow"
          >
            {{ t('imageBrowser.viewFitWindow') }}
          </button>
          <button
            type="button"
            class="crm-image-browser__btn"
            :class="{ 'crm-image-browser__btn--active': viewMode === 'fit-width' }"
            @click="setViewFitWidth"
          >
            {{ t('imageBrowser.viewFitWidth') }}
          </button>
        </div>
        <button type="button" class="crm-image-browser__btn crm-image-browser__btn--close" @click="onClose">
          {{ t('imageBrowser.close') }}
        </button>
      </header>

      <div class="crm-image-browser__body">
        <aside v-if="showSidebar" class="crm-image-browser__sidebar">
          <el-scrollbar class="crm-image-browser__sidebar-scroll">
            <button
              v-for="(item, idx) in items"
              :key="item.id"
              type="button"
              class="crm-image-browser__file-item"
              :class="{ 'crm-image-browser__file-item--active': idx === activeIndex }"
              :title="item.name"
              @click="selectIndex(idx)"
            >
              <span class="crm-image-browser__file-name">{{ item.name }}</span>
            </button>
          </el-scrollbar>
        </aside>

        <div
          ref="canvasRef"
          class="crm-image-browser__canvas"
          :class="`crm-image-browser__canvas--${viewMode}`"
          @mousedown="onPanStart"
        >
          <div v-if="loading" class="crm-image-browser__state">
            <span class="crm-image-browser__spinner" aria-hidden="true" />
            <span>{{ t('imageBrowser.loading') }}</span>
          </div>
          <div v-else-if="loadError" class="crm-image-browser__state">
            <p>{{ loadError }}</p>
            <button type="button" class="crm-image-browser__btn" @click="reloadCurrent">{{ t('imageBrowser.retry') }}</button>
          </div>
          <template v-else-if="displayUrl">
            <img
              ref="imgRef"
              :src="displayUrl"
              :alt="activeItem?.name || ''"
              class="crm-image-browser__img"
              :class="imgClass"
              :style="imgStyle"
              draggable="false"
              @load="onImgLoad"
            />
          </template>
        </div>
      </div>

      <footer class="crm-image-browser__bottom">
        <button type="button" class="crm-image-browser__btn" :disabled="!canGoPrev" @click="goPrev">
          {{ t('imageBrowser.prev') }}
        </button>
        <button type="button" class="crm-image-browser__btn" :disabled="!canGoNext" @click="goNext">
          {{ t('imageBrowser.next') }}
        </button>
        <span class="crm-image-browser__counter">{{ counterText }}</span>
      </footer>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import apiClient from '@/api/client'
import { useImageBrowserStore } from '@/stores/imageBrowser'
import type { ImageBrowserItem, ImageBrowserViewMode } from '@/types/imageBrowser'

const ZOOM_MIN = 0.25
const ZOOM_MAX = 4
const ZOOM_STEP = 0.25

const { t } = useI18n()
const store = useImageBrowserStore()
const { visible, title, items, activeIndex } = storeToRefs(store)

const viewMode = ref<ImageBrowserViewMode>('fit-window')
const scale = ref(1)
const panX = ref(0)
const panY = ref(0)
const loading = ref(false)
const loadError = ref('')
const displayUrl = ref('')
const naturalWidth = ref(0)
const naturalHeight = ref(0)

const canvasRef = ref<HTMLElement | null>(null)
const imgRef = ref<HTMLImageElement | null>(null)

const blobUrlByItemId = new Map<string, string>()
const ownedBlobUrls: string[] = []

const activeItem = computed(() => items.value[activeIndex.value] ?? null)
const showSidebar = computed(() => items.value.length > 1)
const canGoPrev = computed(() => activeIndex.value > 0)
const canGoNext = computed(() => activeIndex.value < items.value.length - 1)
const counterText = computed(() => {
  if (items.value.length === 0) return '0 / 0'
  return `${activeIndex.value + 1} / ${items.value.length}`
})
const canZoomIn = computed(() => viewMode.value !== 'scale' || scale.value + 1e-9 < ZOOM_MAX)
const canZoomOut = computed(() => viewMode.value !== 'scale' || scale.value - 1e-9 > ZOOM_MIN)

const imgClass = computed(() => {
  if (viewMode.value === 'fit-window') return 'crm-image-browser__img--fit-window'
  if (viewMode.value === 'fit-width') return 'crm-image-browser__img--fit-width'
  return 'crm-image-browser__img--scale'
})

const imgStyle = computed(() => {
  if (viewMode.value !== 'scale' || !naturalWidth.value) {
    return {
      transform: panX.value || panY.value ? `translate(${panX.value}px, ${panY.value}px)` : undefined
    }
  }
  const w = Math.round(naturalWidth.value * scale.value)
  const h = naturalHeight.value ? Math.round(naturalHeight.value * scale.value) : undefined
  return {
    width: `${w}px`,
    height: h ? `${h}px` : 'auto',
    transform: `translate(${panX.value}px, ${panY.value}px)`
  }
})

let loadSeq = 0
let panDragging = false
let panStartX = 0
let panStartY = 0
let panOriginX = 0
let panOriginY = 0

function revokeAllBlobUrls() {
  for (const u of ownedBlobUrls) URL.revokeObjectURL(u)
  ownedBlobUrls.length = 0
  blobUrlByItemId.clear()
}

function rememberBlobUrl(itemId: string, url: string) {
  const prev = blobUrlByItemId.get(itemId)
  if (prev && prev !== url) {
    URL.revokeObjectURL(prev)
    const i = ownedBlobUrls.indexOf(prev)
    if (i >= 0) ownedBlobUrls.splice(i, 1)
  }
  blobUrlByItemId.set(itemId, url)
  if (!ownedBlobUrls.includes(url)) ownedBlobUrls.push(url)
}

function resetViewForNewImage() {
  viewMode.value = 'fit-window'
  scale.value = 1
  panX.value = 0
  panY.value = 0
  naturalWidth.value = 0
  naturalHeight.value = 0
}

function lockBodyScroll(lock: boolean) {
  if (typeof document === 'undefined') return
  document.body.style.overflow = lock ? 'hidden' : ''
}

async function resolveItemUrl(item: ImageBrowserItem): Promise<string> {
  const cached = blobUrlByItemId.get(item.id)
  if (cached) return cached
  const preview = String(item.previewUrl || '').trim()
  if (preview) return preview
  const docId = String(item.documentId || '').trim()
  if (!docId) throw new Error(t('imageBrowser.missingSource'))
  const blob = (await apiClient.get(`/api/v1/documents/${encodeURIComponent(docId)}/preview`, {
    responseType: 'blob'
  })) as unknown as Blob
  if (!(blob instanceof Blob) || blob.size === 0) throw new Error(t('imageBrowser.emptyFile'))
  const url = URL.createObjectURL(blob)
  rememberBlobUrl(item.id, url)
  return url
}

async function loadCurrentImage() {
  const item = activeItem.value
  if (!item) {
    displayUrl.value = ''
    return
  }
  const seq = ++loadSeq
  loading.value = true
  loadError.value = ''
  try {
    const url = await resolveItemUrl(item)
    if (seq !== loadSeq) return
    displayUrl.value = url
  } catch (e: unknown) {
    if (seq !== loadSeq) return
    displayUrl.value = ''
    loadError.value = e instanceof Error ? e.message : t('imageBrowser.loadFailed')
  } finally {
    if (seq === loadSeq) loading.value = false
  }
}

function reloadCurrent() {
  const item = activeItem.value
  if (!item) return
  const cached = blobUrlByItemId.get(item.id)
  if (cached && item.documentId) {
    URL.revokeObjectURL(cached)
    const i = ownedBlobUrls.indexOf(cached)
    if (i >= 0) ownedBlobUrls.splice(i, 1)
    blobUrlByItemId.delete(item.id)
  }
  void loadCurrentImage()
}

function onImgLoad() {
  const img = imgRef.value
  if (!img) return
  naturalWidth.value = img.naturalWidth || 0
  naturalHeight.value = img.naturalHeight || 0
}

function selectIndex(idx: number) {
  if (idx < 0 || idx >= items.value.length) return
  activeIndex.value = idx
}

function goPrev() {
  if (!canGoPrev.value) return
  activeIndex.value -= 1
}

function goNext() {
  if (!canGoNext.value) return
  activeIndex.value += 1
}

function setViewFitWindow() {
  viewMode.value = 'fit-window'
  panX.value = 0
  panY.value = 0
}

function setViewFitWidth() {
  viewMode.value = 'fit-width'
  panX.value = 0
  panY.value = 0
}

function setViewPercent100() {
  viewMode.value = 'scale'
  scale.value = 1
  panX.value = 0
  panY.value = 0
}

function zoomIn() {
  if (viewMode.value !== 'scale') {
    viewMode.value = 'scale'
    scale.value = 1
  }
  scale.value = Math.min(ZOOM_MAX, Math.round((scale.value + ZOOM_STEP) * 100) / 100)
}

function zoomOut() {
  if (viewMode.value !== 'scale') {
    viewMode.value = 'scale'
    scale.value = 1
  }
  scale.value = Math.max(ZOOM_MIN, Math.round((scale.value - ZOOM_STEP) * 100) / 100)
}

function onClose() {
  store.close()
}

function onPanStart(ev: MouseEvent) {
  if (loading.value || loadError.value || !displayUrl.value) return
  if (viewMode.value !== 'scale') return
  panDragging = true
  panStartX = ev.clientX
  panStartY = ev.clientY
  panOriginX = panX.value
  panOriginY = panY.value
  window.addEventListener('mousemove', onPanMove)
  window.addEventListener('mouseup', onPanEnd)
}

function onPanMove(ev: MouseEvent) {
  if (!panDragging) return
  panX.value = panOriginX + (ev.clientX - panStartX)
  panY.value = panOriginY + (ev.clientY - panStartY)
}

function onPanEnd() {
  panDragging = false
  window.removeEventListener('mousemove', onPanMove)
  window.removeEventListener('mouseup', onPanEnd)
}

function onKeyDown(ev: KeyboardEvent) {
  if (!visible.value) return
  if (ev.key === 'Escape') {
    ev.preventDefault()
    onClose()
    return
  }
  if (ev.key === 'ArrowLeft') {
    ev.preventDefault()
    goPrev()
    return
  }
  if (ev.key === 'ArrowRight') {
    ev.preventDefault()
    goNext()
    return
  }
  if (ev.key === '+' || ev.key === '=') {
    ev.preventDefault()
    zoomIn()
    return
  }
  if (ev.key === '-' || ev.key === '_') {
    ev.preventDefault()
    zoomOut()
    return
  }
  if (ev.key === '0') {
    ev.preventDefault()
    setViewPercent100()
  }
}

watch(visible, (v) => {
  lockBodyScroll(v)
  if (v) {
    resetViewForNewImage()
    void loadCurrentImage()
    window.addEventListener('keydown', onKeyDown)
  } else {
    loadSeq += 1
    displayUrl.value = ''
    loadError.value = ''
    loading.value = false
    revokeAllBlobUrls()
    window.removeEventListener('keydown', onKeyDown)
    onPanEnd()
  }
})

watch(activeIndex, () => {
  if (!visible.value) return
  resetViewForNewImage()
  void loadCurrentImage()
})

onUnmounted(() => {
  lockBodyScroll(false)
  revokeAllBlobUrls()
  window.removeEventListener('keydown', onKeyDown)
  onPanEnd()
})
</script>

<style scoped lang="scss">
.crm-image-browser {
  position: fixed;
  inset: 0;
  z-index: 10050;
  display: flex;
  flex-direction: column;
  background: rgba(10, 12, 18, 0.96);
  color: #e8eaed;
  user-select: none;
}

.crm-image-browser__top {
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  align-items: center;
  gap: 16px;
  min-height: 52px;
  padding: 8px 16px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  flex-shrink: 0;
}

.crm-image-browser__title {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  min-width: 0;
  justify-self: start;
}

.crm-image-browser__top-tools {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: center;
  gap: 8px;
  justify-self: center;
}

.crm-image-browser__btn--close {
  justify-self: end;
}

.crm-image-browser__btn {
  border: 1px solid rgba(255, 255, 255, 0.18);
  background: rgba(255, 255, 255, 0.06);
  color: inherit;
  border-radius: 6px;
  padding: 6px 12px;
  font-size: 13px;
  line-height: 1.2;
  cursor: pointer;

  &:hover:not(:disabled) {
    background: rgba(255, 255, 255, 0.12);
    border-color: rgba(255, 255, 255, 0.28);
  }

  &:disabled {
    opacity: 0.38;
    cursor: not-allowed;
  }

  &--active {
    border-color: var(--el-color-primary, #409eff);
    background: rgba(64, 158, 255, 0.2);
  }

  &--close {
    flex-shrink: 0;
  }
}

.crm-image-browser__body {
  flex: 1;
  min-height: 0;
  display: flex;
}

.crm-image-browser__sidebar {
  width: 220px;
  flex-shrink: 0;
  border-right: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(0, 0, 0, 0.22);
}

.crm-image-browser__sidebar-scroll {
  height: 100%;
}

.crm-image-browser__file-item {
  display: block;
  width: 100%;
  border: none;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: transparent;
  color: inherit;
  text-align: left;
  padding: 10px 12px;
  cursor: pointer;

  &:hover {
    background: rgba(255, 255, 255, 0.06);
  }

  &--active {
    background: rgba(64, 158, 255, 0.18);
  }
}

.crm-image-browser__file-name {
  display: block;
  font-size: 13px;
  line-height: 1.35;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.crm-image-browser__canvas {
  flex: 1;
  min-width: 0;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;

  &--fit-width {
    align-items: flex-start;
    overflow: auto;
  }

  &--scale {
    overflow: auto;
    cursor: grab;

    &:active {
      cursor: grabbing;
    }
  }
}

.crm-image-browser__img {
  display: block;
  max-width: none;

  &--fit-window {
    max-width: 100%;
    max-height: 100%;
    object-fit: contain;
  }

  &--fit-width {
    width: 100%;
    height: auto;
  }

  &--scale {
    max-width: none;
    max-height: none;
  }
}

.crm-image-browser__state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  color: rgba(255, 255, 255, 0.75);
  font-size: 14px;
}

.crm-image-browser__spinner {
  width: 28px;
  height: 28px;
  border: 2px solid rgba(255, 255, 255, 0.2);
  border-top-color: #fff;
  border-radius: 50%;
  animation: crm-image-browser-spin 0.8s linear infinite;
}

@keyframes crm-image-browser-spin {
  to {
    transform: rotate(360deg);
  }
}

.crm-image-browser__bottom {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  min-height: 52px;
  padding: 8px 16px;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
}

.crm-image-browser__counter {
  font-size: 13px;
  color: rgba(255, 255, 255, 0.75);
  min-width: 56px;
  text-align: center;
}
</style>
