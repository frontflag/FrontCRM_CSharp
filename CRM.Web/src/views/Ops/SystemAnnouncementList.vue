<template>
  <div class="sys-ann-admin-page">
    <div class="page-header">
      <div class="header-left">
        <h1 class="page-title">{{ t('sysAnnouncement.adminTitle') }}</h1>
        <div class="count-badge">{{ t('sysAnnouncement.count', { count: rows.length }) }}</div>
      </div>
      <el-button type="primary" @click="openCreate">{{ t('sysAnnouncement.create') }}</el-button>
    </div>

    <div class="search-bar">
      <el-select
        v-model="statusFilter"
        clearable
        :placeholder="t('sysAnnouncement.filterStatus')"
        style="width: 160px"
        @change="load"
      >
        <el-option :label="t('sysAnnouncement.statusDraft')" value="draft" />
        <el-option :label="t('sysAnnouncement.statusPublished')" value="published" />
      </el-select>
      <el-select
        v-model="typeFilter"
        clearable
        :placeholder="t('sysAnnouncement.filterType')"
        style="width: 160px"
        @change="load"
      >
        <el-option :label="t('sysAnnouncement.typePlatformNotice')" value="platform_notice" />
        <el-option :label="t('sysAnnouncement.typeVersionUpdate')" value="version_update" />
      </el-select>
      <el-button @click="load">{{ t('sysAnnouncement.query') }}</el-button>
    </div>

    <div v-loading="loading" class="table-wrap">
      <el-table :data="rows" row-key="id" stripe>
        <el-table-column prop="title" :label="t('sysAnnouncement.colTitle')" min-width="200" />
        <el-table-column :label="t('sysAnnouncement.colType')" width="120">
          <template #default="{ row }">{{ typeLabel(row.type) }}</template>
        </el-table-column>
        <el-table-column :label="t('sysAnnouncement.colStatus')" width="110">
          <template #default="{ row }">
            <el-tag :type="row.status === 'published' ? 'success' : 'info'" size="small" effect="plain">
              {{ statusLabel(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('sysAnnouncement.colPublishedAt')" width="170">
          <template #default="{ row }">{{ formatDate(row.publishedAt) }}</template>
        </el-table-column>
        <el-table-column :label="t('sysAnnouncement.colActions')" width="280" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openPreview(row)">{{ t('sysAnnouncement.preview') }}</el-button>
            <el-button
              v-if="row.status === 'draft'"
              link
              type="primary"
              @click="openEdit(row)"
            >{{ t('sysAnnouncement.edit') }}</el-button>
            <el-button
              v-if="row.status === 'draft'"
              link
              type="success"
              @click="publish(row)"
            >{{ t('sysAnnouncement.publish') }}</el-button>
            <el-button
              v-if="row.status === 'draft'"
              link
              type="danger"
              @click="remove(row)"
            >{{ t('sysAnnouncement.delete') }}</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div v-if="!loading && rows.length === 0" class="empty">{{ t('sysAnnouncement.adminEmpty') }}</div>
    </div>

    <el-dialog
      v-model="editorOpen"
      :title="editingId ? t('sysAnnouncement.editTitle') : t('sysAnnouncement.createTitle')"
      width="820px"
      destroy-on-close
      @closed="resetEditor"
    >
      <el-form label-width="90px">
        <el-form-item :label="t('sysAnnouncement.colTitle')" required>
          <el-input v-model="form.title" maxlength="100" show-word-limit />
        </el-form-item>
        <el-form-item :label="t('sysAnnouncement.colType')" required>
          <el-select v-model="form.type" style="width: 220px">
            <el-option :label="t('sysAnnouncement.typePlatformNotice')" value="platform_notice" />
            <el-option :label="t('sysAnnouncement.typeVersionUpdate')" value="version_update" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('sysAnnouncement.body')" required>
          <div class="editor-toolbar">
            <el-upload
              :show-file-list="false"
              :http-request="onUploadImage"
              accept="image/*"
            >
              <el-button size="small">{{ t('sysAnnouncement.insertImage') }}</el-button>
            </el-upload>
          </div>
          <el-input
            v-model="form.bodyMd"
            type="textarea"
            :rows="12"
            :placeholder="t('sysAnnouncement.bodyPh')"
          />
          <div class="md-preview-label">{{ t('sysAnnouncement.mdPreview') }}</div>
          <div class="md-preview markdown-body" v-html="previewHtml" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editorOpen = false">{{ t('sysAnnouncement.cancel') }}</el-button>
        <el-button type="primary" :loading="saving" @click="save">{{ t('sysAnnouncement.save') }}</el-button>
      </template>
    </el-dialog>

    <SystemAnnouncementModal
      v-model="previewOpen"
      mode="preview"
      :items="previewItems"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  sysAnnouncementsApi,
  type AnnouncementAdminListItem,
  type AnnouncementDetail
} from '@/api/sysAnnouncements'
import { documentApi } from '@/api/document'
import SystemAnnouncementModal from '@/components/SystemAnnouncement/SystemAnnouncementModal.vue'
import { renderAnnouncementMarkdown } from '@/utils/sanitizeAnnouncementHtml'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()
const loading = ref(false)
const saving = ref(false)
const rows = ref<AnnouncementAdminListItem[]>([])
const statusFilter = ref<string | undefined>(undefined)
const typeFilter = ref<string | undefined>(undefined)

const editorOpen = ref(false)
const editingId = ref<string | null>(null)
const form = reactive({
  title: '',
  type: 'platform_notice',
  bodyMd: ''
})

const previewOpen = ref(false)
const previewItems = ref<AnnouncementDetail[]>([])

const previewHtml = computed(() => renderAnnouncementMarkdown(form.bodyMd))

function typeLabel(type: string) {
  return type === 'version_update'
    ? t('sysAnnouncement.typeVersionUpdate')
    : t('sysAnnouncement.typePlatformNotice')
}

function statusLabel(status: string) {
  return status === 'published'
    ? t('sysAnnouncement.statusPublished')
    : t('sysAnnouncement.statusDraft')
}

function formatDate(v?: string | null) {
  if (!v) return '—'
  return formatDisplayDate(v)
}

async function load() {
  loading.value = true
  try {
    rows.value = await sysAnnouncementsApi.adminList({
      status: statusFilter.value,
      type: typeFilter.value
    })
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysAnnouncement.loadFailed'))
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  form.title = ''
  form.type = 'platform_notice'
  form.bodyMd = ''
  editorOpen.value = true
}

async function openEdit(row: AnnouncementAdminListItem) {
  try {
    const d = await sysAnnouncementsApi.adminGet(row.id)
    editingId.value = d.id
    form.title = d.title
    form.type = d.type || 'platform_notice'
    form.bodyMd = d.bodyMd || ''
    editorOpen.value = true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysAnnouncement.loadFailed'))
  }
}

function resetEditor() {
  editingId.value = null
}

async function save() {
  saving.value = true
  try {
    const payload = {
      title: form.title.trim(),
      type: form.type,
      bodyMd: form.bodyMd
    }
    if (editingId.value) {
      await sysAnnouncementsApi.adminUpdate(editingId.value, payload)
    } else {
      await sysAnnouncementsApi.adminCreate(payload)
    }
    ElMessage.success(t('sysAnnouncement.saved'))
    editorOpen.value = false
    await load()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysAnnouncement.saveFailed'))
  } finally {
    saving.value = false
  }
}

async function publish(row: AnnouncementAdminListItem) {
  try {
    await ElMessageBox.confirm(
      t('sysAnnouncement.publishConfirm'),
      t('sysAnnouncement.publish'),
      { type: 'warning' }
    )
    await sysAnnouncementsApi.adminPublish(row.id)
    ElMessage.success(t('sysAnnouncement.published'))
    await load()
  } catch (e: any) {
    if (e === 'cancel' || e === 'close') return
    ElMessage.error(getApiErrorMessage(e) || t('sysAnnouncement.publishFailed'))
  }
}

async function remove(row: AnnouncementAdminListItem) {
  try {
    await ElMessageBox.confirm(
      t('sysAnnouncement.deleteConfirm', { title: row.title }),
      t('sysAnnouncement.delete'),
      { type: 'warning' }
    )
    await sysAnnouncementsApi.adminDelete(row.id)
    ElMessage.success(t('sysAnnouncement.deleted'))
    await load()
  } catch (e: any) {
    if (e === 'cancel' || e === 'close') return
    ElMessage.error(getApiErrorMessage(e) || t('sysAnnouncement.deleteFailed'))
  }
}

async function openPreview(row: AnnouncementAdminListItem) {
  try {
    const d = await sysAnnouncementsApi.adminGet(row.id)
    previewItems.value = [d]
    previewOpen.value = true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysAnnouncement.loadFailed'))
  }
}

async function onUploadImage(opt: any) {
  const file = opt?.file as File | undefined
  if (!file) return
  const bizId = editingId.value || 'draft-temp'
  try {
    const docs = await documentApi.uploadDocuments('SYS_ANNOUNCEMENT', bizId, [file])
    const id = docs?.[0]?.id
    if (!id) throw new Error('upload empty')
    const md = `\n![](/api/v1/documents/${id}/preview)\n`
    form.bodyMd = (form.bodyMd || '') + md
    ElMessage.success(t('sysAnnouncement.imageInserted'))
    opt?.onSuccess?.(docs[0])
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysAnnouncement.imageFailed'))
    opt?.onError?.(e)
  }
}

onMounted(() => void load())
</script>

<style lang="scss" scoped>
.sys-ann-admin-page {
  padding: 16px 20px 32px;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}

.count-badge {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.search-bar {
  display: flex;
  gap: 10px;
  margin-bottom: 14px;
}

.table-wrap {
  min-height: 200px;
}

.empty {
  padding: 40px;
  text-align: center;
  color: var(--el-text-color-secondary);
}

.editor-toolbar {
  margin-bottom: 8px;
}

.md-preview-label {
  margin-top: 12px;
  margin-bottom: 6px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.md-preview {
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
  padding: 12px;
  min-height: 80px;
  max-height: 240px;
  overflow: auto;
  background: var(--el-fill-color-blank);

  :deep(img) {
    max-width: 100%;
  }
}
</style>
