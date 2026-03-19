<template>
  <div class="bc-uploader">
    <!-- 标题栏 -->
    <div class="bc-header">
      <div class="bc-title">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <rect x="2" y="5" width="20" height="14" rx="2"/>
          <line x1="2" y1="10" x2="22" y2="10"/>
        </svg>
        名片
        <span v-if="cards.length" class="bc-count">{{ cards.length }}</span>
      </div>
      <!-- 上传触发按钮 -->
      <label class="bc-upload-btn" :class="{ disabled: uploading }">
        <input
          ref="fileInput"
          type="file"
          multiple
          accept=".jpg,.jpeg,.png,.webp,.heic"
          style="display:none"
          @change="onFileSelect"
        />
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
          <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
        </svg>
        {{ uploading ? '上传中...' : '上传名片' }}
      </label>
    </div>

    <!-- 加载中 -->
    <div v-if="loading" class="bc-loading">
      <span class="bc-loading-dot"></span>
      <span class="bc-loading-dot"></span>
      <span class="bc-loading-dot"></span>
    </div>

    <!-- 名片网格 -->
    <div v-else-if="cards.length" class="bc-grid">
      <div
        v-for="card in cards"
        :key="card.id"
        class="bc-card"
        @click="openPreview(card)"
      >
        <!-- 图片缩略图 -->
        <div class="bc-thumb">
          <img
            :src="getThumbUrl(card)"
            :alt="card.originalFileName"
            @error="onImgError($event)"
          />
          <div class="bc-thumb-overlay">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
              <line x1="11" y1="8" x2="11" y2="14"/><line x1="8" y1="11" x2="14" y2="11"/>
            </svg>
            <span>放大查看</span>
          </div>
        </div>
        <!-- 文件名 + 删除 -->
        <div class="bc-card-footer">
          <span class="bc-filename" :title="card.originalFileName">{{ card.originalFileName }}</span>
          <button
            type="button"
            class="bc-delete-btn"
            title="删除名片"
            @click.stop="deleteCard(card)"
          >
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="3 6 5 6 21 6"/>
              <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
              <path d="M10 11v6M14 11v6"/>
              <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
            </svg>
          </button>
        </div>
      </div>

      <!-- 额外上传格子（最多 10 张） -->
      <label v-if="cards.length < maxCards" class="bc-add-tile" :class="{ disabled: uploading }">
        <input
          type="file"
          multiple
          accept=".jpg,.jpeg,.png,.webp,.heic"
          style="display:none"
          @change="onFileSelect"
        />
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
        </svg>
        <span>添加</span>
      </label>
    </div>

    <!-- 空状态 -->
    <div v-else class="bc-empty">
      <label class="bc-empty-zone" :class="{ disabled: uploading }">
        <input
          type="file"
          multiple
          accept=".jpg,.jpeg,.png,.webp,.heic"
          style="display:none"
          @change="onFileSelect"
        />
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2">
          <rect x="2" y="5" width="20" height="14" rx="2"/>
          <line x1="2" y1="10" x2="22" y2="10"/>
        </svg>
        <p class="bc-empty-text">点击或拖拽上传名片图片</p>
        <p class="bc-empty-hint">支持 JPG / PNG / WebP，最多 {{ maxCards }} 张</p>
      </label>
    </div>

    <!-- 大图预览弹窗 -->
    <el-dialog
      v-model="previewVisible"
      :title="previewCard?.originalFileName || '名片预览'"
      width="760px"
      top="5vh"
      destroy-on-close
      class="bc-preview-dialog"
      @closed="onPreviewClosed"
    >
      <div class="bc-preview-body">
        <div v-if="previewLoading" class="bc-preview-loading">加载中...</div>
        <img
          v-else-if="previewBlobUrl"
          :src="previewBlobUrl"
          class="bc-preview-img"
          :alt="previewCard?.originalFileName"
        />
      </div>
      <template #footer>
        <div class="bc-preview-footer">
          <!-- 翻页 -->
          <div class="bc-preview-nav">
            <button
              type="button"
              class="nav-btn"
              :disabled="previewIndex <= 0"
              @click="navigatePreview(-1)"
            >
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="15 18 9 12 15 6"/>
              </svg>
              上一张
            </button>
            <span class="nav-indicator">{{ previewIndex + 1 }} / {{ cards.length }}</span>
            <button
              type="button"
              class="nav-btn"
              :disabled="previewIndex >= cards.length - 1"
              @click="navigatePreview(1)"
            >
              下一张
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="9 18 15 12 9 6"/>
              </svg>
            </button>
          </div>
          <div class="bc-preview-actions">
            <button type="button" class="btn-ghost" @click="downloadCard(previewCard!)">下载</button>
            <button type="button" class="btn-danger" @click="deleteCardFromPreview">删除</button>
            <button type="button" class="btn-close" @click="previewVisible = false">关闭</button>
          </div>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { documentApi, type UploadDocumentDto } from '@/api/document'
import apiClient from '@/api/client'

const props = withDefaults(
  defineProps<{
    /** 业务类型，如 'contact' */
    bizType: string
    /** 联系人 ID（新建时为空，编辑时传入） */
    bizId?: string
    maxCards?: number
  }>(),
  { maxCards: 10 }
)

const emit = defineEmits<{
  /** 上传成功后通知父组件，返回最新文档列表 */
  uploaded: [docs: UploadDocumentDto[]]
}>()

const fileInput = ref<HTMLInputElement | null>(null)
const loading = ref(false)
const uploading = ref(false)
const cards = ref<UploadDocumentDto[]>([])

// ── 预览状态 ──
const previewVisible = ref(false)
const previewLoading = ref(false)
const previewBlobUrl = ref('')
const previewCard = ref<UploadDocumentDto | null>(null)
const previewIndex = ref(0)

// ── 加载已有名片 ──
async function fetchCards() {
  if (!props.bizId) return
  loading.value = true
  try {
    const all = await documentApi.getDocuments(props.bizType, props.bizId)
    // 只取图片类型
    cards.value = all.filter((d) => {
      const ext = (d.fileExtension || '').toLowerCase()
      const mime = (d.mimeType || '').toLowerCase()
      return /^image\//.test(mime) || ['.jpg', '.jpeg', '.png', '.webp', '.heic'].includes(ext)
    })
  } catch {
    cards.value = []
  } finally {
    loading.value = false
  }
}

onMounted(fetchCards)
watch(() => props.bizId, fetchCards)

// ── 获取缩略图 URL ──
function getThumbUrl(doc: UploadDocumentDto) {
  return documentApi.getPreviewPath(doc.id)
}

function onImgError(e: Event) {
  const img = e.target as HTMLImageElement
  img.src = 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="80" height="60"><rect width="80" height="60" fill="%230d1e35"/><text x="40" y="35" text-anchor="middle" fill="%2300D4FF" font-size="10">图片加载失败</text></svg>'
}

// ── 文件选择 & 上传 ──
async function onFileSelect(e: Event) {
  const input = e.target as HTMLInputElement
  const files = Array.from(input.files || [])
  input.value = ''
  if (!files.length) return

  // 新建模式（无 bizId）：提示需先保存联系人
  if (!props.bizId) {
    ElMessage.warning('请先保存联系人后再上传名片')
    return
  }

  // 图片格式校验
  const allowed = ['.jpg', '.jpeg', '.png', '.webp', '.heic']
  const valid = files.filter((f) => {
    const ext = '.' + (f.name.split('.').pop() || '').toLowerCase()
    if (!allowed.includes(ext)) {
      ElMessage.warning(`不支持格式：${f.name}，请上传图片文件`)
      return false
    }
    if (f.size > 20 * 1024 * 1024) {
      ElMessage.warning(`${f.name} 超过 20MB`)
      return false
    }
    return true
  })

  if (!valid.length) return

  // 数量限制
  const remaining = props.maxCards - cards.value.length
  const toUpload = valid.slice(0, remaining)
  if (toUpload.length < valid.length) {
    ElMessage.warning(`最多上传 ${props.maxCards} 张名片，本次上传 ${toUpload.length} 张`)
  }

  uploading.value = true
  try {
    const uploaded = await documentApi.uploadDocuments(props.bizType, props.bizId, toUpload)
    ElMessage.success(`成功上传 ${uploaded.length} 张名片`)
    await fetchCards()
    emit('uploaded', cards.value)
  } catch {
    ElMessage.error('名片上传失败，请稍后重试')
  } finally {
    uploading.value = false
  }
}

// ── 删除名片 ──
async function deleteCard(card: UploadDocumentDto) {
  try {
    await ElMessageBox.confirm(
      `确定删除名片「${card.originalFileName}」？`,
      '删除名片',
      { confirmButtonText: '删除', cancelButtonText: '取消', type: 'warning' }
    )
    await documentApi.deleteDocument(card.id)
    ElMessage.success('名片已删除')
    await fetchCards()
    emit('uploaded', cards.value)
  } catch {
    // 取消忽略
  }
}

// ── 从预览弹窗删除 ──
async function deleteCardFromPreview() {
  if (!previewCard.value) return
  const card = previewCard.value
  previewVisible.value = false
  await deleteCard(card)
}

// ── 打开预览 ──
async function openPreview(card: UploadDocumentDto) {
  previewCard.value = card
  previewIndex.value = cards.value.findIndex((c) => c.id === card.id)
  previewVisible.value = true
  await loadPreviewBlob(card)
}

async function loadPreviewBlob(card: UploadDocumentDto) {
  previewBlobUrl.value = ''
  previewLoading.value = true
  try {
    const blob = await apiClient.get<Blob>(`/api/v1/documents/${card.id}/preview`, { responseType: 'blob' } as any)
    const b = blob instanceof Blob ? blob : (blob as any)?.data
    if (b instanceof Blob) previewBlobUrl.value = URL.createObjectURL(b)
  } catch {
    ElMessage.error('图片加载失败')
  } finally {
    previewLoading.value = false
  }
}

// ── 翻页 ──
async function navigatePreview(delta: number) {
  const next = previewIndex.value + delta
  if (next < 0 || next >= cards.value.length) return
  previewIndex.value = next
  previewCard.value = cards.value[next]
  await loadPreviewBlob(cards.value[next])
}

// ── 关闭预览清理 ──
function onPreviewClosed() {
  if (previewBlobUrl.value) {
    URL.revokeObjectURL(previewBlobUrl.value)
    previewBlobUrl.value = ''
  }
  previewCard.value = null
}

// ── 下载 ──
function downloadCard(card: UploadDocumentDto) {
  documentApi.downloadDocument(card.id, card.originalFileName)
}

// 暴露刷新方法供父组件调用
defineExpose({ refresh: fetchCards })
</script>

<style lang="scss" scoped>
$cyan: #00D4FF;
$bg: #0d1e35;
$border: rgba(0, 212, 255, 0.15);
$text-muted: rgba(130, 170, 200, 0.6);
$text-primary: rgba(224, 244, 255, 0.92);

.bc-uploader {
  background: rgba(0, 212, 255, 0.03);
  border: 1px solid $border;
  border-radius: 8px;
  overflow: hidden;
}

/* ── 标题栏 ── */
.bc-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 14px;
  background: rgba(0, 212, 255, 0.04);
  border-bottom: 1px solid rgba(0, 212, 255, 0.08);
}

.bc-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 600;
  color: rgba(0, 212, 255, 0.85);
}

.bc-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: rgba(0, 212, 255, 0.15);
  font-size: 11px;
  color: $cyan;
  font-weight: 700;
}

.bc-upload-btn {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 12px;
  border-radius: 5px;
  background: transparent;
  border: 1px solid rgba(0, 212, 255, 0.25);
  color: rgba(0, 212, 255, 0.75);
  font-size: 12px;
  cursor: pointer;
  transition: all 0.15s;

  &:hover:not(.disabled) {
    background: rgba(0, 212, 255, 0.07);
    border-color: rgba(0, 212, 255, 0.45);
  }

  &.disabled { opacity: 0.5; cursor: not-allowed; pointer-events: none; }
}

/* ── 加载动画 ── */
.bc-loading {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 24px;
}

.bc-loading-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: $cyan;
  animation: bc-bounce 1.2s ease-in-out infinite;

  &:nth-child(2) { animation-delay: 0.2s; }
  &:nth-child(3) { animation-delay: 0.4s; }
}

@keyframes bc-bounce {
  0%, 80%, 100% { transform: scale(0.6); opacity: 0.4; }
  40% { transform: scale(1); opacity: 1; }
}

/* ── 名片网格 ── */
.bc-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(130px, 1fr));
  gap: 10px;
  padding: 12px;
}

.bc-card {
  background: rgba(0, 0, 0, 0.2);
  border: 1px solid rgba(0, 212, 255, 0.1);
  border-radius: 6px;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.15s;

  &:hover {
    border-color: rgba(0, 212, 255, 0.35);
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);

    .bc-thumb-overlay { opacity: 1; }
  }
}

.bc-thumb {
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 10;
  background: rgba(0, 0, 0, 0.3);
  overflow: hidden;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    display: block;
  }
}

.bc-thumb-overlay {
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.55);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  opacity: 0;
  transition: opacity 0.15s;
  color: #fff;

  span { font-size: 11px; }
}

.bc-card-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 5px 8px;
  gap: 4px;
}

.bc-filename {
  flex: 1;
  font-size: 11px;
  color: $text-muted;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.bc-delete-btn {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  background: transparent;
  border: 1px solid rgba(201, 87, 69, 0.2);
  border-radius: 3px;
  color: rgba(201, 87, 69, 0.6);
  cursor: pointer;
  transition: all 0.15s;

  &:hover {
    background: rgba(201, 87, 69, 0.1);
    border-color: rgba(201, 87, 69, 0.45);
    color: #C95745;
  }
}

/* ── 添加格子 ── */
.bc-add-tile {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 5px;
  aspect-ratio: 16 / 10;
  border: 1px dashed rgba(0, 212, 255, 0.2);
  border-radius: 6px;
  color: rgba(0, 212, 255, 0.4);
  cursor: pointer;
  transition: all 0.15s;
  font-size: 11px;

  &:hover:not(.disabled) {
    border-color: rgba(0, 212, 255, 0.45);
    color: rgba(0, 212, 255, 0.7);
    background: rgba(0, 212, 255, 0.04);
  }

  &.disabled { opacity: 0.4; cursor: not-allowed; pointer-events: none; }
}

/* ── 空状态 ── */
.bc-empty { padding: 12px; }

.bc-empty-zone {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 28px 16px;
  border: 1px dashed rgba(0, 212, 255, 0.2);
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s;
  color: rgba(0, 212, 255, 0.35);

  &:hover:not(.disabled) {
    border-color: rgba(0, 212, 255, 0.4);
    background: rgba(0, 212, 255, 0.03);
    color: rgba(0, 212, 255, 0.6);
  }

  &.disabled { opacity: 0.4; cursor: not-allowed; pointer-events: none; }
}

.bc-empty-text { font-size: 13px; color: $text-muted; margin: 0; }
.bc-empty-hint { font-size: 11.5px; color: rgba(130, 170, 200, 0.4); margin: 0; }

/* ── 预览弹窗 ── */
:deep(.bc-preview-dialog) {
  .el-dialog__header { padding: 14px 20px; border-bottom: 1px solid rgba(0, 212, 255, 0.1); }
  .el-dialog__title { font-size: 14px; color: $text-primary; }
  .el-dialog__body { padding: 0; }
  .el-dialog__footer { padding: 12px 20px; border-top: 1px solid rgba(0, 212, 255, 0.08); }
}

.bc-preview-body {
  min-height: 300px;
  max-height: 65vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #040e1a;
  overflow: hidden;
}

.bc-preview-loading {
  color: $text-muted;
  font-size: 13px;
}

.bc-preview-img {
  max-width: 100%;
  max-height: 65vh;
  object-fit: contain;
  display: block;
}

.bc-preview-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.bc-preview-nav {
  display: flex;
  align-items: center;
  gap: 10px;
}

.nav-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 5px 12px;
  background: transparent;
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 5px;
  color: rgba(0, 212, 255, 0.7);
  font-size: 12.5px;
  cursor: pointer;
  transition: all 0.15s;

  &:hover:not(:disabled) { background: rgba(0, 212, 255, 0.07); border-color: rgba(0, 212, 255, 0.4); }
  &:disabled { opacity: 0.35; cursor: not-allowed; }
}

.nav-indicator {
  font-size: 12px;
  color: $text-muted;
  min-width: 40px;
  text-align: center;
}

.bc-preview-actions {
  display: flex;
  gap: 8px;
}

.btn-ghost {
  padding: 5px 14px;
  background: transparent;
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 5px;
  color: $text-muted;
  font-size: 12.5px;
  cursor: pointer;
  transition: all 0.15s;
  &:hover { border-color: rgba(0, 212, 255, 0.4); color: $text-primary; }
}

.btn-danger {
  padding: 5px 14px;
  background: transparent;
  border: 1px solid rgba(201, 87, 69, 0.3);
  border-radius: 5px;
  color: rgba(201, 87, 69, 0.7);
  font-size: 12.5px;
  cursor: pointer;
  transition: all 0.15s;
  &:hover { background: rgba(201, 87, 69, 0.1); border-color: rgba(201, 87, 69, 0.5); color: #C95745; }
}

.btn-close {
  padding: 5px 14px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.65), rgba(0, 212, 255, 0.55));
  border: 1px solid rgba(0, 212, 255, 0.35);
  border-radius: 5px;
  color: #fff;
  font-size: 12.5px;
  cursor: pointer;
  transition: all 0.15s;
  &:hover { background: linear-gradient(135deg, rgba(0, 102, 255, 0.85), rgba(0, 212, 255, 0.75)); }
}
</style>
