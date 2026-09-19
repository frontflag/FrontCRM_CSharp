<template>
  <div
    class="document-list-panel"
    :class="{
      'document-list-panel--compact': hideToolbar || compact,
      'document-list-panel--tagged': showCategoryTag
    }"
  >
    <div v-if="!hideToolbar" class="toolbar">
      <span class="title">关联文档</span>
      <button type="button" class="btn-ghost btn-sm" @click="fetchList" :disabled="loading">刷新</button>
    </div>
    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="!list.length" class="empty">{{ emptyText }}</div>
    <div v-else class="list" :class="viewMode">
      <div v-for="doc in list" :key="doc.id" class="doc-card" :class="{ 'doc-card--list': isListView }">
        <template v-if="isListView">
          <div class="doc-main-row">
            <el-tag
              v-if="showCategoryTag"
              class="doc-cat-tag"
              size="small"
              effect="plain"
              :type="categoryTagType(doc)"
            >
              {{ categoryLabel(doc) }}
            </el-tag>
            <span
              class="doc-name doc-name--link"
              role="link"
              tabindex="0"
              :title="doc.originalFileName"
              @click="preview(doc)"
              @keydown.enter.prevent="preview(doc)"
              @keydown.space.prevent="preview(doc)"
            >
              {{ doc.originalFileName }}
            </span>
            <div v-if="!compact" class="doc-date">{{ formatDate(doc.createTime) }}</div>
            <div v-if="!compact" class="doc-bytes">{{ formatFileBytes(doc.fileSize) }}</div>
            <div
              class="actions"
              :class="{ 'doc-row-toolbar': compact && showCategoryTag }"
            >
              <button type="button" class="link" @click="preview(doc)">预览</button>
              <button type="button" class="link" @click="download(doc)">下载</button>
              <button v-if="!readonly" type="button" class="link danger" @click="remove(doc)">删除</button>
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
            <el-tag
              v-if="showCategoryTag"
              class="doc-cat-tag"
              size="small"
              effect="plain"
              :type="categoryTagType(doc)"
            >
              {{ categoryLabel(doc) }}
            </el-tag>
            <button
              type="button"
              class="name name--link"
              :title="doc.originalFileName"
              @click="preview(doc)"
            >
              {{ doc.originalFileName }}
            </button>
            <div class="meta">{{ formatDate(doc.createTime) }} · {{ formatSize(doc.fileSize) }}</div>
            <div v-if="doc.remark" class="remark">{{ doc.remark }}</div>
          </div>
          <div class="actions">
            <button type="button" class="link" @click="preview(doc)">预览</button>
            <button type="button" class="link" @click="download(doc)">下载</button>
            <button v-if="!readonly" type="button" class="link danger" @click="remove(doc)">删除</button>
          </div>
        </template>
      </div>
    </div>
    <DocumentPreviewDialog v-model="previewVisible" :document-id="previewId" :mime-type="previewMime" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { documentApi, type UploadDocumentDto } from '@/api/document'
import DocumentPreviewDialog from './DocumentPreviewDialog.vue'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import {
  UPLOAD_DOC_CATEGORY,
  normalizeUploadDocCategory,
  uploadDocCategoryI18nKey
} from '@/constants/uploadDocumentCategory'

const props = withDefaults(
  defineProps<{
    bizType: string
    bizId: string
    viewMode?: 'grid' | 'list'
    /** 只读：仅预览/下载，不可删除 */
    readonly?: boolean
    /** 隐藏「关联文档 / 刷新」工具条 */
    hideToolbar?: boolean
    /** 无文档时文案 */
    emptyText?: string
    /** 行首显示类型标签（出货照片 / 签收单 / 其他） */
    showCategoryTag?: boolean
    compact?: boolean
  }>(),
  { readonly: false, hideToolbar: false, emptyText: '暂无文档', showCategoryTag: false, compact: false }
)

const { t } = useI18n()

const emit = defineEmits<{ updated: [count: number] }>()
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
    .then((res) => {
      list.value = res
      emit('updated', res.length)
    })
    .catch(() => {
      list.value = []
      emit('updated', 0)
    })
    .finally(() => (loading.value = false))
}

watch(() => [props.bizType, props.bizId], fetchList, { immediate: false })
onMounted(fetchList)

function categoryLabel(doc: UploadDocumentDto) {
  return t(uploadDocCategoryI18nKey(normalizeUploadDocCategory(doc.docCategory)))
}

function categoryTagType(doc: UploadDocumentDto): 'warning' | 'success' | 'info' {
  const code = normalizeUploadDocCategory(doc.docCategory)
  if (code === UPLOAD_DOC_CATEGORY.ShipPhoto) return 'warning'
  if (code === UPLOAD_DOC_CATEGORY.Pod) return 'success'
  return 'info'
}

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

  &--compact {
    .loading,
    .empty {
      margin: 0;
      padding: 8px 0 4px;
      font-size: 13px;
      text-align: center;
    }
  }
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
    .info {
      .doc-cat-tag {
        margin-bottom: 2px;
      }
      .name {
        font-size: 12px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
      .name--link {
        display: block;
        width: 100%;
        max-width: 100%;
        padding: 0;
        border: none;
        background: none;
        text-align: left;
        color: $cyan-primary;
        cursor: pointer;
        &:hover { text-decoration: underline; }
      }
      .meta { font-size: 11px; color: $text-muted; }
      .remark { font-size: 11px; color: $text-secondary; margin-top: 2px; }
    }
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

  .doc-name,
  button.doc-name {
    flex: 0 1 auto;
    max-width: 200px;
    min-width: 0;
    font-family: 'Noto Sans SC', sans-serif;
    font-size: 13px;
    font-weight: 400;
    line-height: 1.5;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .doc-name--link,
  button.doc-name--link {
    padding: 0;
    border: none;
    background: none;
    text-align: left;
    color: $cyan-primary;
    cursor: pointer;
    font-family: 'Noto Sans SC', sans-serif;
    font-size: 13px;
    font-weight: 400;
    line-height: 1.5;
    &:hover { text-decoration: underline; }
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

  .doc-cat-tag {
    flex: 0 0 auto;
  }

  &--tagged {
    .list:not(.grid) {
      flex-direction: column;
      flex-wrap: nowrap;
      width: 100%;
      gap: 0;
      border: 1px solid $border-card;
      border-radius: $border-radius-lg;
      overflow: hidden;
      background: $layer-2;
    }

    .doc-card--list {
      display: flex;
      width: 100%;
      max-width: 100%;
      min-width: 0;
      border: none;
      border-radius: 0;
      background: transparent;
    }

    .doc-card {
      border: none;
      border-radius: 0;
      background: transparent;
    }

    .doc-main-row {
      width: 100%;
    }

    .doc-name,
    button.doc-name,
    span.doc-name {
      flex: 1 1 auto;
      max-width: none;
      padding-right: 4px;
      color: var(--crm-table-text);
      font-family: 'Noto Sans SC', sans-serif;
      font-size: 13px;
      font-weight: 400;
      line-height: 1.5;
    }

    .doc-name--link,
    button.doc-name--link,
    span.doc-name--link {
      color: inherit;
      cursor: default;
      font-family: 'Noto Sans SC', sans-serif;
      font-size: 13px;
      font-weight: 400;
      line-height: 1.5;

      &:hover {
        color: var(--el-color-primary);
        text-decoration: underline;
        cursor: pointer;
      }
    }
  }

  &--tagged#{&}--compact {
    .list:not(.grid) {
      background: transparent;
      border: none;
      border-radius: 0;
      overflow: visible;
      gap: 3px;
    }

    .doc-card--list {
      padding: 4px 0;
      gap: 4px;
    }

    .doc-main-row {
      position: relative;
      gap: 8px;
    }

    .doc-row-toolbar {
      position: absolute;
      right: 0;
      top: 50%;
      transform: translateY(-50%);
      z-index: 2;
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 4px 10px;
      border-radius: 6px;
      background: #fffbeb;
      border: 1px solid rgba(217, 119, 6, 0.18);
      box-shadow: 0 2px 8px rgba(15, 23, 42, 0.08);
      white-space: nowrap;
      opacity: 0;
      pointer-events: none;
      transition: opacity 0.12s ease;
    }

    .doc-card--list:hover .doc-row-toolbar,
    .doc-card--list:focus-within .doc-row-toolbar,
    .doc-main-row:hover .doc-row-toolbar {
      opacity: 1;
      pointer-events: auto;
    }

    @media (hover: none) {
      .doc-row-toolbar {
        opacity: 1;
        pointer-events: auto;
      }
    }
  }
}
</style>
