<template>
  <div class="page">
    <h2>培训教材</h2>
    <div class="actions">
      <input type="file" accept=".docx" @change="onFile" />
      <el-button type="primary" :disabled="!file" :loading="uploading" @click="upload">上传并导入</el-button>
      <el-button @click="load">刷新</el-button>
    </div>
    <el-alert v-if="error" :title="error" type="error" show-icon />
    <el-table :data="versions" size="small">
      <el-table-column prop="versionNo" label="版本" width="80" />
      <el-table-column prop="sourceFileName" label="文件" />
      <el-table-column label="状态" width="100">
        <template #default="{ row }">{{ statusText(row.status) }}</template>
      </el-table-column>
      <el-table-column prop="chunkCount" label="块数" width="80" />
      <el-table-column prop="errorMessage" label="说明" />
      <el-table-column label="启用" width="80">
        <template #default="{ row }">{{ row.isActive ? '是' : '' }}</template>
      </el-table-column>
      <el-table-column label="操作" width="210">
        <template #default="{ row }">
          <el-button link type="primary" :disabled="row.isActive" @click="activate(row)">启用</el-button>
          <el-button link @click="openChunks(row.versionId)">切块</el-button>
          <el-button v-if="row.status === 2" link type="primary" @click="openReader(row.versionId)">浏览</el-button>
        </template>
      </el-table-column>
    </el-table>
    <p v-if="chunkNote" class="chunk-note">{{ chunkNote }}</p>
    <el-table v-if="chunks.length" :data="chunks" size="small" class="chunks">
      <el-table-column prop="chunkIndex" label="#" width="70" />
      <el-table-column prop="heading" label="章节" />
      <el-table-column prop="excerpt" label="摘录" />
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { knowledgeBaseApi, type KbChunkListItem, type KbDocumentVersion } from '@/api/knowledgeBase'

const versions = ref<KbDocumentVersion[]>([])
const chunks = ref<KbChunkListItem[]>([])
const chunkNote = ref('')
const file = ref<File | null>(null)
const uploading = ref(false)
const error = ref('')

function statusText(status: number) {
  return ['排队', '嵌入中', '就绪', '失败', '停用'][status] ?? String(status)
}

function onFile(event: Event) {
  const input = event.target as HTMLInputElement
  file.value = input.files?.[0] ?? null
}

async function load() {
  error.value = ''
  try {
    versions.value = await knowledgeBaseApi.listVersions()
  } catch (e) {
    error.value = e instanceof Error ? e.message : '加载失败'
  }
}

async function upload() {
  if (!file.value) return
  uploading.value = true
  error.value = ''
  try {
    await knowledgeBaseApi.upload(file.value)
    file.value = null
    await load()
  } catch (e) {
    error.value = e instanceof Error ? e.message : '上传失败'
  } finally {
    uploading.value = false
  }
}

async function activate(row: KbDocumentVersion) {
  error.value = ''
  if (row.status !== 2) {
    error.value = row.errorMessage
      ? `还不能启用。${row.errorMessage}`
      : '向量还没写完，状态变为「就绪」后才能启用。'
    return
  }
  try {
    await knowledgeBaseApi.activate(row.versionId)
    await load()
  } catch (e) {
    error.value = e instanceof Error ? e.message : '启用失败'
  }
}

async function openReader(versionId: string) {
  window.open(`/knowledge/handbook/read?version=${encodeURIComponent(versionId)}`, '_blank', 'noopener')
}

async function openChunks(versionId: string) {
  error.value = ''
  chunkNote.value = ''
  const row = versions.value.find(item => item.versionId === versionId)
  try {
    const pageSize = 100
    const all: KbChunkListItem[] = []
    for (let page = 1; page <= 20; page++) {
      const batch = await knowledgeBaseApi.listChunks(versionId, page, pageSize)
      all.push(...batch)
      if (batch.length < pageSize) break
    }
    chunks.value = all
    if (all.length === 0) {
      chunkNote.value = row?.errorMessage
        ? `还没有切块。${row.errorMessage}`
        : '还没有切块。'
    } else {
      chunkNote.value = `共 ${all.length} 块`
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : '读取切块失败'
  }
}

onMounted(load)
</script>

<style scoped>
.page { padding: 16px; }
.actions { display: flex; gap: 8px; align-items: center; margin-bottom: 12px; }
.chunks { margin-top: 16px; }
.chunk-note { margin-top: 12px; color: #666; }
</style>
