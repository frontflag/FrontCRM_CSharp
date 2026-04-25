<template>
  <div
    class="document-upload-panel"
    :class="{ 'document-upload-panel--dragging': dragging }"
    @drop.prevent.stop="onDrop"
    @dragover.prevent.stop="onDragOver"
    @dragleave.stop="onDragLeave"
  >
    <div class="upload-toolbar">
      <div class="upload-area">
        <input ref="fileInput" type="file" multiple accept=".pdf,.jpg,.jpeg,.png,.docx,.xlsx,.zip" @change="onSelect" />
        <div class="upload-placeholder">
          <p class="upload-line">
            <span>拖拽文件到此处，或</span>
            <button type="button" class="link-btn" @click="fileInput?.click()">点击选择</button>
            <span class="hint">单次最多 {{ maxFiles }} 个，单文件不超过 {{ maxSizeMb }}MB</span>
          </p>
        </div>
      </div>
      <div class="actions">
        <button type="button" class="btn-primary" :disabled="uploading || !selectedFiles.length" @click="submit">
          {{ uploading ? '上传中...' : '上传' }}
        </button>
        <button v-if="selectedFiles.length" type="button" class="btn-ghost" @click="clear">清空</button>
      </div>
    </div>
    <div v-if="selectedFiles.length" class="file-list">
      <div v-for="(f, i) in selectedFiles" :key="i" class="file-item">
        <span class="name">{{ f.name }}</span>
        <span class="size">{{ formatSize(f.size) }}</span>
        <button type="button" class="remove" @click="removeFile(i)">×</button>
      </div>
    </div>
    <div v-if="remarkAllowed && selectedFiles.length" class="remark-below">
      <el-input
        v-model="remark"
        type="textarea"
        :rows="2"
        placeholder="请输入备注，选填，最多256字"
        maxlength="256"
        show-word-limit
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { ElMessage } from 'element-plus'
import { documentApi, type UploadDocumentDto } from '@/api/document'

const props = withDefaults(
  defineProps<{
    bizType: string
    bizId: string
    maxFiles?: number
    maxSizeMb?: number
    remarkAllowed?: boolean
  }>(),
  { maxFiles: 5, maxSizeMb: 50, remarkAllowed: true }
)

const emit = defineEmits<{ uploaded: [documents: UploadDocumentDto[]] }>()

const fileInput = ref<HTMLInputElement | null>(null)
const dragging = ref(false)
const dragDepth = ref(0)
const remark = ref('')
const selectedFiles = ref<File[]>([])
const uploading = ref(false)

const maxBytes = computed(() => props.maxSizeMb * 1024 * 1024)

function formatSize(n: number) {
  if (n < 1024) return n + ' B'
  if (n < 1024 * 1024) return (n / 1024).toFixed(1) + ' KB'
  return (n / (1024 * 1024)).toFixed(1) + ' MB'
}

function onDrop(e: DragEvent) {
  dragDepth.value = 0
  dragging.value = false
  const list = e.dataTransfer?.files
  if (!list?.length) return
  addFiles(Array.from(list))
}

function onDragOver() {
  dragDepth.value = Math.max(1, dragDepth.value)
  dragging.value = true
}

function onDragLeave() {
  dragDepth.value = Math.max(0, dragDepth.value - 1)
  if (dragDepth.value === 0) {
    dragging.value = false
  }
}

function onSelect(e: Event) {
  const input = e.target as HTMLInputElement
  if (input.files?.length) addFiles(Array.from(input.files))
  input.value = ''
}

function addDroppedFiles(files: File[]) {
  if (!files.length) return
  addFiles(files)
}

function addFiles(files: File[]) {
  const allowed = ['.pdf', '.jpg', '.jpeg', '.png', '.docx', '.xlsx', '.zip']
  const next = [...selectedFiles.value]
  for (const f of files) {
    const ext = '.' + (f.name.split('.').pop() || '').toLowerCase()
    if (!allowed.includes(ext)) {
      ElMessage.warning(`不支持格式: ${f.name}`)
      continue
    }
    if (f.size > maxBytes.value) {
      ElMessage.warning(`${f.name} 超过 ${props.maxSizeMb}MB`)
      continue
    }
    if (next.length >= props.maxFiles) break
    next.push(f)
  }
  selectedFiles.value = next
}

function removeFile(i: number) {
  selectedFiles.value = selectedFiles.value.filter((_, idx) => idx !== i)
}

function clear() {
  selectedFiles.value = []
  remark.value = ''
}

async function submit() {
  if (!selectedFiles.value.length || !props.bizType || !props.bizId) return
  uploading.value = true
  try {
    const list = await documentApi.uploadDocuments(
      props.bizType,
      props.bizId,
      selectedFiles.value,
      remark.value || undefined
    )
    ElMessage.success('上传成功')
    emit('uploaded', list)
    clear()
  } catch (e: any) {
    ElMessage.error(e?.message || '上传失败')
  } finally {
    uploading.value = false
  }
}

defineExpose({
  addDroppedFiles
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.document-upload-panel {
  border: 1px solid #dcdfe6;
  border-radius: 8px;
  padding: 8px;
  transition: border-color 0.2s, background-color 0.2s;

  &.document-upload-panel--dragging {
    border-color: $cyan-primary;
    background: rgba(0, 212, 255, 0.04);
  }

  .upload-toolbar {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: nowrap;
    min-height: 52px;

    > * {
      align-self: center;
    }
  }

  .upload-area {
    flex: 0 0 auto;
    min-height: 52px;
    display: flex;
    align-items: center;
    border-radius: 8px;
    padding: 6px 12px;
    text-align: center;
    position: relative;
    cursor: pointer;
    transition: border-color 0.2s;
    input[type='file'] { position: absolute; left: 0; top: 0; width: 100%; height: 100%; opacity: 0; cursor: pointer; }
    .upload-placeholder {
      .upload-line {
        margin: 0;
        display: inline-flex;
        align-items: center;
        gap: 4px;
        white-space: nowrap;
        font-size: 13px;
      }
      .link-btn { background: none; border: none; color: $cyan-primary; cursor: pointer; text-decoration: underline; font-size: 13px; }
      .hint { font-size: 13px; color: $text-muted; }
    }
  }
  .file-list {
    margin-top: 12px;
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 10px 14px;

    .file-item {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      font-size: 13px;
      padding: 4px 8px;
      border-radius: 6px;
      background: rgba(148, 163, 184, 0.08);

      .name {
        max-width: 180px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .size {
        color: $text-muted;
        white-space: nowrap;
      }

      .remove {
        background: none;
        border: none;
        color: #C95745;
        cursor: pointer;
        padding: 0 4px;
        line-height: 1;
      }
    }
  }

  .remark-below {
    margin-top: 12px;
  }
  .actions {
    display: flex;
    align-items: center;
    gap: 8px;
    flex: 0 0 auto;
  }
  .btn-primary, .btn-ghost { padding: 6px 14px; border-radius: 6px; font-size: 13px; cursor: pointer; }
  .btn-primary { background: linear-gradient(135deg, rgba(0,102,255,0.8), rgba(0,212,255,0.7)); border: 1px solid rgba(0,212,255,0.4); color: #fff; &:disabled { opacity: 0.5; cursor: not-allowed; } }
  .btn-ghost { background: transparent; border: 1px solid $border-panel; color: $text-muted; }
}
</style>
