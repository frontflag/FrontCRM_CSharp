<template>
  <div class="document-list-panel">
    <div class="toolbar">
      <span class="title">关联文档</span>
      <button type="button" class="btn-ghost btn-sm" @click="fetchList" :disabled="loading">刷新</button>
    </div>
    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="!list.length" class="empty">暂无文档</div>
    <div v-else class="list" :class="viewMode">
      <div v-for="doc in list" :key="doc.id" class="doc-card" :class="{ 'doc-card--list': isListView }">
        <template v-if="isListView">
          <div class="doc-main-row">
            <div class="doc-name" :title="doc.originalFileName">{{ doc.originalFileName }}</div>
            <div class="doc-date">{{ formatDate(doc.createTime) }}</div>
            <div class="doc-bytes">{{ formatFileBytes(doc.fileSize) }}</div>
            <div class="actions">
              <button type="button" class="link" @click="preview(doc)">预览</button>
              <button type="button" class="link" @click="download(doc)">下载</button>
              <button type="button" class="link danger" @click="remove(doc)">删除</button>
            </div>
          </div>
          <div v-if="doc.remark" class="doc-remark-line">{{ doc.remark }}</div>
        </template>
        <template v-else>
          <div class="thumb" @click="preview(doc)">
            <img v-if="isImage(doc)" :src="thumbSrc(doc)" alt="" @error="onThumbError" />
            <span v-else class="file-icon">{{ fileIcon(doc) }}</span>
          </div>
          <div class="info">
            <div class="name" :title="doc.originalFileName">{{ doc.originalFileName }}</div>
            <div class="meta">{{ formatDate(doc.createTime) }} · {{ formatSize(doc.fileSize) }}</div>
            <div v-if="doc.remark" class="remark">{{ doc.remark }}</div>
          </div>
          <div class="actions">
            <button type="button" class="link" @click="preview(doc)">预览</button>
            <button type="button" class="link" @click="download(doc)">下载</button>
            <button type="button" class="link danger" @click="remove(doc)">删除</button>
          </div>
        </template>
      </div>
    </div>
    <DocumentPreviewDialog v-model="previewVisible" :document-id="previewId" :mime-type="previewMime" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { documentApi, type UploadDocumentDto } from '@/api/document'
import DocumentPreviewDialog from './DocumentPreviewDialog.vue'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const props = defineProps<{
  bizType: string
  bizId: string
  viewMode?: 'grid' | 'list'
}>()

/** 非 grid 时按列表行展示（横向：文件名、日期、字节、操作） */
const isListView = computed(() => props.viewMode !== 'grid')

const loading = ref(false)
const list = ref<UploadDocumentDto[]>([])
const previewVisible = ref(false)
const previewId = ref('')
const previewMime = ref('')

function fetchList() {
  if (!props.bizType || !props.bizId) return
  loading.value = true
  documentApi
    .getDocuments(props.bizType, props.bizId)
    .then((res) => (list.value = res))
    .catch(() => (list.value = []))
    .finally(() => (loading.value = false))
}

watch(() => [props.bizType, props.bizId], fetchList, { immediate: false })
onMounted(fetchList)

function isImage(doc: UploadDocumentDto) {
  const t = (doc.mimeType || '').toLowerCase()
  const e = (doc.fileExtension || '').toLowerCase()
  return /^image\//.test(t) || ['.jpg', '.jpeg', '.png'].includes(e)
}

function thumbSrc(doc: UploadDocumentDto) {
  if (doc.thumbnailRelativePath) return documentApi.getPreviewPath(doc.id) + '?t=' + doc.id
  if (isImage(doc)) return documentApi.getPreviewPath(doc.id)
  return ''
}

function onThumbError(ev: Event) {
  (ev.target as HTMLImageElement).style.display = 'none'
}

function fileIcon(doc: UploadDocumentDto) {
  const e = (doc.fileExtension || '').toLowerCase()
  if (e === '.pdf') return '📄'
  if (['.docx', '.doc'].includes(e)) return '📝'
  if (['.xlsx', '.xls'].includes(e)) return '📊'
  if (e === '.zip') return '📦'
  return '📎'
}

function formatDate(s?: string) {
  return formatDisplayDateTime(s)
}

function formatSize(n?: number) {
  if (n == null) return '--'
  if (n < 1024) return n + ' B'
  if (n < 1024 * 1024) return (n / 1024).toFixed(1) + ' KB'
  return (n / (1024 * 1024)).toFixed(1) + ' MB'
}

/** 列表行「字节」列：原始字节数（千分位） */
function formatFileBytes(n?: number) {
  if (n == null || !Number.isFinite(n)) return '--'
  return `${Math.round(n).toLocaleString()} 字节`
}

function preview(doc: UploadDocumentDto) {
  previewId.value = doc.id
  previewMime.value = doc.mimeType || ''
  previewVisible.value = true
}

function download(doc: UploadDocumentDto) {
  documentApi.downloadDocument(doc.id, doc.originalFileName)
}

function remove(doc: UploadDocumentDto) {
  ElMessageBox.confirm(`确定删除「${doc.originalFileName}」？`, '删除确认', {
    confirmButtonText: '删除',
    cancelButtonText: '取消',
    type: 'warning'
  })
    .then(() => documentApi.deleteDocument(doc.id))
    .then(() => {
      ElMessage.success('已删除')
      fetchList()
    })
    .catch(() => {})
}

defineExpose({ refresh: fetchList })
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.document-list-panel {
  .toolbar {
    display: flex;
    align-items: center;
    justify-content: flex-start;
    gap: 10px;
    margin-bottom: 12px;

    .title {
      font-size: 14px;
      font-weight: 500;
    }
  }
  .btn-ghost { padding: 4px 10px; font-size: 12px; background: transparent; border: 1px solid $border-panel; border-radius: 6px; color: $text-muted; cursor: pointer; &:disabled { opacity: 0.5; } }
  .loading, .empty { padding: 24px; text-align: center; color: $text-muted; font-size: 13px; }
  .list {
    display: flex;
    gap: 12px;
  }
  .list.grid {
    flex-direction: row;
    flex-wrap: wrap;
  }
  .list.list {
    flex-direction: row;
    flex-wrap: wrap;
    align-items: flex-start;
    gap: 10px 12px;
  }
  .list.grid .doc-card { width: calc(25% - 10px); min-width: 140px; }
  .doc-card {
    background: $layer-2;
    border: 1px solid $border-panel;
    border-radius: 8px;
    padding: 10px;
    display: flex;
    flex-direction: column;
    gap: 6px;
    .thumb {
      width: 100%; aspect-ratio: 1; background: rgba(0,0,0,0.2); border-radius: 6px; display: flex; align-items: center; justify-content: center; cursor: pointer; overflow: hidden;
      img { max-width: 100%; max-height: 100%; object-fit: contain; }
      .file-icon { font-size: 32px; }
    }
    .info { .name { font-size: 12px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; } .meta { font-size: 11px; color: $text-muted; } .remark { font-size: 11px; color: $text-secondary; margin-top: 2px; } }
    .actions { display: flex; gap: 8px; flex-shrink: 0; .link { background: none; border: none; padding: 0; font-size: 12px; color: $cyan-primary; cursor: pointer; text-decoration: none; &.danger { color: #C95745; } } }
  }

  .doc-card--list {
    display: inline-flex;
    flex-direction: column;
    flex: 0 1 auto;
    width: max-content;
    max-width: 100%;
    gap: 8px;
    padding: 8px 12px;
    box-sizing: border-box;
  }

  .doc-main-row {
    display: flex;
    flex-direction: row;
    align-items: center;
    flex-wrap: nowrap;
    gap: 10px 14px;
    width: auto;
    max-width: 100%;
    min-width: 0;
  }

  .doc-name {
    flex: 0 1 auto;
    max-width: 200px;
    min-width: 0;
    font-size: 13px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .doc-date,
  .doc-bytes {
    flex: 0 0 auto;
    font-size: 12px;
    color: $text-muted;
    white-space: nowrap;
  }

  .doc-remark-line {
    font-size: 11px;
    color: $text-secondary;
    padding-left: 2px;
    max-width: 360px;
    line-height: 1.35;
    word-break: break-word;
  }
}
</style>
