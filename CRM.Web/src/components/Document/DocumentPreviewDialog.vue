<template>
  <el-dialog v-model="visible" title="文档预览" width="80%" top="5vh" destroy-on-close @closed="onClosed">
    <div v-if="loading" class="preview-loading">加载中...</div>
    <div v-else-if="blobUrl" class="preview-body">
      <img v-if="isImageType" :src="blobUrl" class="preview-img" />
      <iframe v-else-if="isPdf" :src="blobUrl" class="preview-iframe" />
      <div v-else class="preview-other">
        <p>当前格式不支持在线预览，请下载后查看。</p>
        <a :href="blobUrl" target="_blank" rel="noopener" download>下载文件</a>
      </div>
    </div>
    <template #footer>
      <button type="button" class="btn-ghost" @click="visible = false">关闭</button>
      <a v-if="blobUrl && !isImageType && !isPdf" :href="blobUrl" download class="btn-primary">下载</a>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import apiClient from '@/api/client'

const props = defineProps<{
  modelValue: boolean
  documentId: string
  mimeType?: string
}>()

const emit = defineEmits<{ 'update:modelValue': [v: boolean] }>()

const visible = computed({
  get: () => props.modelValue,
  set: (v) => emit('update:modelValue', v)
})

const loading = ref(false)
const blobUrl = ref('')

const isImageType = computed(() => /^image\//i.test(props.mimeType || ''))
const isPdf = computed(() => (props.mimeType || '').toLowerCase().includes('pdf'))

function fetchBlob() {
  if (!props.documentId) return
  blobUrl.value = ''
  loading.value = true
  const url = `/api/v1/documents/${props.documentId}/preview`
  apiClient
    .get(url, { responseType: 'blob' })
    .then((blob: any) => {
      if (blob && blob instanceof Blob) {
        blobUrl.value = URL.createObjectURL(blob)
      }
    })
    .catch(() => {})
    .finally(() => (loading.value = false))
}

watch(
  () => [props.modelValue, props.documentId],
  () => {
    if (props.modelValue && props.documentId) fetchBlob()
  },
  { immediate: true }
)

function onClosed() {
  if (blobUrl.value) {
    URL.revokeObjectURL(blobUrl.value)
    blobUrl.value = ''
  }
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.preview-loading { padding: 48px; text-align: center; color: $text-muted; }
.preview-body { min-height: 60vh; display: flex; align-items: center; justify-content: center; }
.preview-img { max-width: 100%; max-height: 80vh; object-fit: contain; }
.preview-iframe { width: 100%; height: 75vh; border: none; }
.preview-other { text-align: center; padding: 24px; .btn-primary { display: inline-block; margin-top: 12px; padding: 8px 16px; background: linear-gradient(135deg, rgba(0,102,255,0.8), rgba(0,212,255,0.7)); color: #fff; border-radius: 8px; text-decoration: none; } }
.btn-ghost { padding: 6px 14px; background: transparent; border: 1px solid $border-panel; border-radius: 6px; color: $text-muted; cursor: pointer; }
</style>
