<template>
  <div class="qc-images-readonly">
    <template v-if="grouped.length > 0">
      <section v-for="group in grouped" :key="group.qcId" class="qc-images-readonly__group">
        <div v-if="group.qcCode || group.stockInNotifyCode" class="qc-images-readonly__group-head">
          <span v-if="group.qcCode" class="qc-images-readonly__qc-code">{{ group.qcCode }}</span>
          <span v-if="group.stockInNotifyCode" class="qc-images-readonly__notify-code">{{ group.stockInNotifyCode }}</span>
        </div>
        <div class="qc-images-readonly__grid">
          <button
            v-for="img in group.images"
            :key="img.documentId"
            type="button"
            class="qc-images-readonly__thumb"
            :title="img.originalFileName || undefined"
            @click="onPreview(img.documentId)"
          >
            <img
              v-if="thumbUrlById[img.documentId]"
              :src="thumbUrlById[img.documentId]"
              :alt="img.originalFileName || 'QC image'"
              loading="lazy"
            />
            <span v-else class="qc-images-readonly__thumb-placeholder" />
          </button>
        </div>
      </section>
    </template>
    <el-empty v-else :description="emptyText" :image-size="64" />
  </div>
</template>

<script setup lang="ts">
import { computed, onUnmounted, ref, watch } from 'vue'
import apiClient from '@/api/client'
import { documentApi, type QcImageReadonlyRow } from '@/api/document'

export type { QcImageReadonlyRow }

const props = withDefaults(
  defineProps<{
    images: QcImageReadonlyRow[]
    emptyText?: string
  }>(),
  {
    images: () => [],
    emptyText: '暂无质检图片',
  }
)

type QcImageGroup = {
  qcId: string
  qcCode?: string | null
  stockInNotifyCode?: string | null
  images: QcImageReadonlyRow[]
}

const grouped = computed<QcImageGroup[]>(() => {
  const map = new Map<string, QcImageGroup>()
  for (const img of props.images) {
    const qcId = String(img.qcId || '').trim()
    if (!qcId) continue
    let g = map.get(qcId)
    if (!g) {
      g = {
        qcId,
        qcCode: img.qcCode,
        stockInNotifyCode: img.stockInNotifyCode,
        images: [],
      }
      map.set(qcId, g)
    }
    g.images.push(img)
  }
  return [...map.values()]
})

const thumbUrlById = ref<Record<string, string>>({})
const blobUrls: string[] = []

function revokeBlobUrls() {
  blobUrls.forEach((u) => URL.revokeObjectURL(u))
  blobUrls.length = 0
  thumbUrlById.value = {}
}

async function loadThumbnails(images: QcImageReadonlyRow[]) {
  revokeBlobUrls()
  const next: Record<string, string> = {}
  let seq = 0
  for (const img of images) {
    const id = String(img.documentId || '').trim()
    if (!id || next[id]) continue
    try {
      const blob = (await apiClient.get(`/api/v1/documents/${encodeURIComponent(id)}/preview?thumbnail=true`, {
        responseType: 'blob',
      })) as unknown as Blob
      if (!(blob instanceof Blob) || blob.size === 0) continue
      const url = URL.createObjectURL(blob)
      blobUrls.push(url)
      next[id] = url
      seq += 1
    } catch {
      /* skip broken thumb */
    }
  }
  thumbUrlById.value = next
}

watch(
  () => props.images,
  (imgs) => {
    void loadThumbnails(imgs ?? [])
  },
  { immediate: true, deep: true }
)

function onPreview(documentId: string) {
  void documentApi.openPreviewInNewTab(documentId)
}

onUnmounted(() => {
  revokeBlobUrls()
})
</script>

<style scoped lang="scss">
.qc-images-readonly {
  padding: 12px 4px 4px;
}

.qc-images-readonly__group + .qc-images-readonly__group {
  margin-top: 16px;
}

.qc-images-readonly__group-head {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 16px;
  margin-bottom: 10px;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.qc-images-readonly__qc-code {
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.qc-images-readonly__grid {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.qc-images-readonly__thumb {
  width: 148px;
  height: 148px;
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

.qc-images-readonly__thumb-placeholder {
  display: block;
  width: 100%;
  height: 100%;
  background: var(--el-fill-color);
}
</style>
