<template>
  <div v-if="images.length > 0" class="notice-image-gallery">
    <button
      v-for="img in images"
      :key="img.documentId"
      type="button"
      class="notice-image-gallery__thumb"
      :title="img.originalFileName || undefined"
      @click="onPreview(img.documentId)"
    >
      <img
        v-if="thumbUrlById[img.documentId]"
        :src="thumbUrlById[img.documentId]"
        :alt="img.originalFileName || t('sysUserNotice.images')"
        loading="lazy"
      />
      <span v-else class="notice-image-gallery__placeholder" />
    </button>
  </div>
</template>

<script setup lang="ts">
import { computed, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import apiClient from '@/api/client'
import { useImageBrowser } from '@/composables/useImageBrowser'
import type { UserNoticeImage } from '@/api/sysUserNotices'
import type { ImageBrowserItem } from '@/types/imageBrowser'

const props = withDefaults(
  defineProps<{
    images: UserNoticeImage[]
    thumbSize?: number
  }>(),
  {
    images: () => [],
    thumbSize: 88
  }
)

const { t } = useI18n()
const { openImageBrowser } = useImageBrowser()
const thumbSizePx = computed(() => `${Math.max(48, Number(props.thumbSize) || 88)}px`)

const thumbUrlById = ref<Record<string, string>>({})
const blobUrls: string[] = []
const loadedThumbIds = new Set<string>()

function revokeBlobUrls() {
  blobUrls.forEach((u) => URL.revokeObjectURL(u))
  blobUrls.length = 0
  loadedThumbIds.clear()
  thumbUrlById.value = {}
}

async function loadThumbnails(list: UserNoticeImage[]) {
  const next = { ...thumbUrlById.value }
  for (const img of list) {
    const id = String(img.documentId || '').trim()
    if (!id || loadedThumbIds.has(id) || next[id]) continue
    try {
      const blob = (await apiClient.get(`/api/v1/documents/${encodeURIComponent(id)}/preview?thumbnail=true`, {
        responseType: 'blob'
      })) as unknown as Blob
      if (!(blob instanceof Blob) || blob.size === 0) continue
      const url = URL.createObjectURL(blob)
      blobUrls.push(url)
      next[id] = url
      loadedThumbIds.add(id)
    } catch {
      /* skip broken thumb */
    }
  }
  thumbUrlById.value = next
}

watch(
  () => props.images,
  (list) => {
    revokeBlobUrls()
    void loadThumbnails(list ?? [])
  },
  { immediate: true, deep: true }
)

function onPreview(documentId: string) {
  const items: ImageBrowserItem[] = []
  for (const img of props.images ?? []) {
    const id = String(img.documentId || '').trim()
    if (!id) continue
    items.push({
      id,
      name: String(img.originalFileName || '').trim() || id,
      documentId: id
    })
  }
  if (items.length === 0) return
  const idx = items.findIndex((x) => x.documentId === documentId)
  openImageBrowser({
    items,
    initialIndex: idx >= 0 ? idx : 0,
    title: t('imageBrowser.defaultTitleNotice')
  })
}

onUnmounted(() => {
  revokeBlobUrls()
})
</script>

<style scoped lang="scss">
.notice-image-gallery {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 12px;
}

.notice-image-gallery__thumb {
  width: v-bind(thumbSizePx);
  height: v-bind(thumbSizePx);
  padding: 0;
  border: 1px solid var(--el-border-color);
  border-radius: 6px;
  overflow: hidden;
  background: var(--el-fill-color-light);
  cursor: zoom-in;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    display: block;
  }

  &:hover {
    border-color: var(--el-color-primary);
  }
}

.notice-image-gallery__placeholder {
  display: block;
  width: 100%;
  height: 100%;
  background: var(--el-fill-color);
}
</style>
