<template>
  <div class="document-upload-panel">
    <div class="upload-area" @drop.prevent="onDrop" @dragover.prevent="dragging = true" @dragleave="dragging = false"
      :class="{ dragging }">
      <input ref="fileInput" type="file" multiple accept=".pdf,.jpg,.jpeg,.png,.docx,.xlsx,.zip" @change="onSelect" />
      <div class="upload-placeholder">
        <span class="icon">📁</span>
        <p>拖拽文件到此处，或<button type="button" class="link-btn" @click="fileInput?.click()">点击选择</button></p>
        <p class="hint">单次最多 {{ maxFiles }} 个，单文件不超过 {{ maxSizeMb }}MB</p>
      </div>
    </div>
    <div v-if="remarkAllowed" class="remark-row">
      <label>备注</label>
      <el-input v-model="remark" type="textarea" :rows="2" placeholder="选填，最多256字" maxlength="256" show-word-limit />
    </div>
    <div v-if="selectedFiles.length" class="file-list">
      <div v-for="(f, i) in selectedFiles" :key="i" class="file-item">
        <span class="name">{{ f.name }}</span>
        <span class="size">{{ formatSize(f.size) }}</span>
        <button type="button" class="remove" @click="removeFile(i)">×</button>
      </div>
    </div>
    <div class="actions">
      <button type="button" class="btn-primary" :disabled="uploading || !selectedFiles.length" @click="submit">
        {{ uploading ? '上传中...' : '上传' }}
      </button>
      <button v-if="selectedFiles.length" type="button" class="btn-ghost" @click="clear">清空</button>
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
  dragging.value = false
  const list = e.dataTransfer?.files
  if (!list?.length) return
  addFiles(Array.from(list))
}

function onSelect(e: Event) {
  const input = e.target as HTMLInputElement
  if (input.files?.length) addFiles(Array.from(input.files))
  input.value = ''
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
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.document-upload-panel {
  .upload-area {
    border: 1px dashed $border-panel;
    border-radius: 8px;
    padding: 24px;
    text-align: center;
    position: relative;
    cursor: pointer;
    transition: border-color 0.2s;
    &.dragging { border-color: $cyan-primary; background: rgba(0,212,255,0.05); }
    input[type='file'] { position: absolute; left: 0; top: 0; width: 100%; height: 100%; opacity: 0; cursor: pointer; }
    .upload-placeholder {
      .icon { font-size: 32px; display: block; margin-bottom: 8px; }
      .link-btn { background: none; border: none; color: $cyan-primary; cursor: pointer; text-decoration: underline; }
      .hint { font-size: 12px; color: $text-muted; margin-top: 4px; }
    }
  }
  .remark-row { margin-top: 12px; label { display: block; margin-bottom: 4px; font-size: 12px; color: $text-muted; } }
  .file-list { margin-top: 12px; .file-item { display: flex; align-items: center; gap: 8px; font-size: 13px; margin-bottom: 4px; .name { flex: 1; overflow: hidden; text-overflow: ellipsis; } .size { color: $text-muted; } .remove { background: none; border: none; color: #C95745; cursor: pointer; padding: 0 4px; } } }
  .actions { margin-top: 12px; display: flex; gap: 8px; }
  .btn-primary, .btn-ghost { padding: 6px 14px; border-radius: 6px; font-size: 13px; cursor: pointer; }
  .btn-primary { background: linear-gradient(135deg, rgba(0,102,255,0.8), rgba(0,212,255,0.7)); border: 1px solid rgba(0,212,255,0.4); color: #fff; &:disabled { opacity: 0.5; cursor: not-allowed; } }
  .btn-ghost { background: transparent; border: 1px solid $border-panel; color: $text-muted; }
}
</style>
