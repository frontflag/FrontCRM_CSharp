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
        <el-table-column :label="t('sysAnnouncement.colDelivery')" width="120">
          <template #default="{ row }">{{ deliveryLabel(row.delivery) }}</template>
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
      width="1080px"
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
        <el-form-item :label="t('sysAnnouncement.colDelivery')" required>
          <el-radio-group v-model="form.delivery">
            <el-radio value="popup">{{ t('sysAnnouncement.deliveryPopup') }}</el-radio>
            <el-radio value="desktop">{{ t('sysAnnouncement.deliveryDesktop') }}</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item :label="t('sysAnnouncement.body')" required class="body-item">
          <div class="ann-editor-wrap">
            <div class="ann-editor-head">
              <el-upload
                :show-file-list="false"
                :http-request="onUploadImage"
                accept="image/*"
              >
                <el-button size="small">{{ t('sysAnnouncement.insertImage') }}</el-button>
              </el-upload>
              <div class="ann-editor-head__right">
                <el-radio-group v-model="contentMode" size="small" @change="onContentModeChange">
                  <el-radio-button label="rich">{{ t('bbs.form.modeRich') }}</el-radio-button>
                  <el-radio-button label="markdown">{{ t('bbs.form.modeMarkdown') }}</el-radio-button>
                </el-radio-group>
                <el-radio-group
                  v-if="contentMode === 'markdown'"
                  v-model="editorLayout"
                  size="small"
                >
                  <el-radio-button label="split">{{ t('bbs.form.layoutSplit') }}</el-radio-button>
                  <el-radio-button label="tab">{{ t('bbs.form.layoutTab') }}</el-radio-button>
                </el-radio-group>
              </div>
            </div>

            <BbsRichEditor
              v-if="contentMode === 'rich'"
              ref="richEditorRef"
              v-model="form.bodyMd"
              :placeholder="t('bbs.form.richPh')"
            />

            <div
              v-else
              class="ann-editor"
              :class="editorLayout === 'tab' ? 'ann-editor--tab' : 'ann-editor--split'"
            >
              <div v-if="editorLayout === 'tab'" class="ann-editor__tabs" role="tablist">
                <button
                  type="button"
                  role="tab"
                  class="ann-editor__tab"
                  :class="{ 'is-active': activeTab === 'edit' }"
                  :aria-selected="activeTab === 'edit'"
                  @click="activeTab = 'edit'"
                >{{ t('bbs.form.editPane') }}</button>
                <button
                  type="button"
                  role="tab"
                  class="ann-editor__tab"
                  :class="{ 'is-active': activeTab === 'preview' }"
                  :aria-selected="activeTab === 'preview'"
                  @click="activeTab = 'preview'"
                >{{ t('bbs.form.previewPane') }}</button>
              </div>
              <div class="ann-editor__body">
                <div
                  v-show="editorLayout === 'split' || activeTab === 'edit'"
                  class="ann-editor__pane ann-editor__pane--edit"
                >
                  <div v-if="editorLayout === 'split'" class="ann-editor__pane-head">
                    {{ t('bbs.form.editPane') }}
                  </div>
                  <el-input
                    ref="contentInputRef"
                    v-model="form.bodyMd"
                    type="textarea"
                    :rows="14"
                    :placeholder="t('bbs.form.contentPh')"
                    class="ann-editor__input"
                    @blur="rememberCursor"
                    @click="rememberCursor"
                    @keyup="rememberCursor"
                  />
                </div>
                <div
                  v-show="editorLayout === 'split' || activeTab === 'preview'"
                  class="ann-editor__pane ann-editor__pane--preview"
                >
                  <div v-if="editorLayout === 'split'" class="ann-editor__pane-head">
                    {{ t('bbs.form.previewPane') }}
                  </div>
                  <div v-if="!form.bodyMd.trim()" class="ann-preview ann-preview--empty">
                    {{ t('bbs.form.previewEmpty') }}
                  </div>
                  <div v-else class="ann-preview markdown-body" v-html="previewHtml" />
                </div>
              </div>
            </div>
            <div class="ann-editor-hint">{{ contentModeHint }}</div>
          </div>
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
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox, type InputInstance } from 'element-plus'
import {
  sysAnnouncementsApi,
  type AnnouncementAdminListItem,
  type AnnouncementDetail
} from '@/api/sysAnnouncements'
import { documentApi } from '@/api/document'
import SystemAnnouncementModal from '@/components/SystemAnnouncement/SystemAnnouncementModal.vue'
import BbsRichEditor from '@/components/Bbs/BbsRichEditor.vue'
import { isLikelyHtmlContent, renderBbsContent } from '@/utils/sanitizeAnnouncementHtml'
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
  delivery: 'popup',
  bodyMd: ''
})

const previewOpen = ref(false)
const previewItems = ref<AnnouncementDetail[]>([])

const MODE_KEY = 'sysAnnouncement.editor.contentMode'
const LAYOUT_KEY = 'sysAnnouncement.editor.layout'
const contentMode = ref<'rich' | 'markdown'>(
  typeof localStorage !== 'undefined' && localStorage.getItem(MODE_KEY) === 'markdown' ? 'markdown' : 'rich'
)
const editorLayout = ref<'split' | 'tab'>(
  typeof localStorage !== 'undefined' && localStorage.getItem(LAYOUT_KEY) === 'tab' ? 'tab' : 'split'
)
const activeTab = ref<'edit' | 'preview'>('edit')
const richEditorRef = ref<InstanceType<typeof BbsRichEditor> | null>(null)
const contentInputRef = ref<InputInstance | null>(null)
const cursorPos = ref(0)
let modeSwitchGuard = false

watch(editorLayout, (v) => {
  try {
    localStorage.setItem(LAYOUT_KEY, v)
  } catch {
    /* ignore */
  }
  if (v === 'tab') activeTab.value = 'edit'
})

watch(contentMode, (v) => {
  try {
    localStorage.setItem(MODE_KEY, v)
  } catch {
    /* ignore */
  }
})

const contentModeHint = computed(() =>
  contentMode.value === 'rich' ? t('bbs.form.richHint') : t('bbs.form.contentHint')
)

const previewHtml = computed(() => renderBbsContent(form.bodyMd))

function applyContentModeForBody(body: string) {
  contentMode.value = isLikelyHtmlContent(body)
    ? 'rich'
    : body.trim()
      ? 'markdown'
      : contentMode.value
  activeTab.value = 'edit'
}

function typeLabel(type: string) {
  return type === 'version_update'
    ? t('sysAnnouncement.typeVersionUpdate')
    : t('sysAnnouncement.typePlatformNotice')
}

function deliveryLabel(delivery?: string) {
  return delivery === 'desktop'
    ? t('sysAnnouncement.deliveryDesktop')
    : t('sysAnnouncement.deliveryPopup')
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
  form.delivery = 'popup'
  form.bodyMd = ''
  applyContentModeForBody('')
  editorOpen.value = true
}

async function openEdit(row: AnnouncementAdminListItem) {
  try {
    const d = await sysAnnouncementsApi.adminGet(row.id)
    editingId.value = d.id
    form.title = d.title
    form.type = d.type || 'platform_notice'
    form.delivery = d.delivery === 'desktop' ? 'desktop' : 'popup'
    form.bodyMd = d.bodyMd || ''
    applyContentModeForBody(form.bodyMd)
    editorOpen.value = true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysAnnouncement.loadFailed'))
  }
}

function resetEditor() {
  editingId.value = null
}

async function onContentModeChange(next: string | number | boolean | undefined) {
  const mode = String(next) as 'rich' | 'markdown'
  if (modeSwitchGuard) return
  const prev = mode === 'rich' ? 'markdown' : 'rich'
  if (!form.bodyMd.trim()) {
    contentMode.value = mode
    return
  }
  try {
    await ElMessageBox.confirm(t('bbs.form.modeSwitchConfirm'), t('bbs.form.modeSwitchTitle'), {
      type: 'warning',
      confirmButtonText: t('bbs.form.modeSwitchOk'),
      cancelButtonText: t('bbs.form.cancel')
    })
    if (mode === 'rich' && prev === 'markdown') {
      form.bodyMd = renderBbsContent(form.bodyMd)
    } else if (mode === 'markdown' && prev === 'rich') {
      const tmp = document.createElement('div')
      tmp.innerHTML = form.bodyMd
      form.bodyMd = (tmp.innerText || tmp.textContent || '').trim()
    }
    contentMode.value = mode
  } catch {
    modeSwitchGuard = true
    contentMode.value = prev
    await nextTick()
    modeSwitchGuard = false
  }
}

function getTextarea(): HTMLTextAreaElement | null {
  const root = contentInputRef.value?.$el as HTMLElement | undefined
  return root?.querySelector?.('textarea') ?? null
}

function rememberCursor() {
  const ta = getTextarea()
  if (ta) cursorPos.value = ta.selectionStart ?? form.bodyMd.length
}

function insertAtCursor(snippet: string) {
  const ta = getTextarea()
  const useLive = !!(ta && document.activeElement === ta)
  const start = useLive ? (ta!.selectionStart ?? cursorPos.value) : cursorPos.value
  const end = useLive ? (ta!.selectionEnd ?? start) : start
  form.bodyMd = `${form.bodyMd.slice(0, start)}${snippet}${form.bodyMd.slice(end)}`
  cursorPos.value = start + snippet.length
}

async function save() {
  saving.value = true
  try {
    const payload = {
      title: form.title.trim(),
      type: form.type,
      delivery: form.delivery,
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
    const src = `/api/v1/documents/${id}/preview`
    if (contentMode.value === 'rich') {
      richEditorRef.value?.insertHtml(`<p><img src="${src}" alt=""></p>`)
    } else {
      insertAtCursor(`\n![](${src})\n`)
    }
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

.body-item :deep(.el-form-item__content) {
  display: block;
}

.ann-editor-wrap {
  width: 100%;
}

.ann-editor-head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 8px;
}

.ann-editor-head__right {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.ann-editor-hint {
  margin-top: 8px;
  font-size: 12px;
  line-height: 1.5;
  color: var(--el-text-color-secondary);
}

.ann-editor {
  border: 1px solid var(--el-border-color);
  border-radius: 8px;
  overflow: hidden;
  background: var(--el-fill-color-blank);
}

.ann-editor__tabs {
  display: flex;
  border-bottom: 1px solid var(--el-border-color-lighter);
  background: var(--el-fill-color-light);
}

.ann-editor__tab {
  padding: 9px 16px;
  border: none;
  border-bottom: 2px solid transparent;
  background: transparent;
  color: var(--el-text-color-secondary);
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;

  &.is-active {
    color: var(--el-color-primary);
    border-bottom-color: var(--el-color-primary);
  }
}

.ann-editor__body {
  display: grid;
  align-items: stretch;
}

.ann-editor--split .ann-editor__body {
  grid-template-columns: 1fr 1fr;
}

.ann-editor--tab .ann-editor__body {
  grid-template-columns: 1fr;
}

.ann-editor__pane {
  min-width: 0;
  display: flex;
  flex-direction: column;
  min-height: 320px;
}

.ann-editor--split .ann-editor__pane--edit {
  border-right: 1px solid var(--el-border-color-lighter);
}

.ann-editor__pane-head {
  padding: 8px 12px;
  font-size: 12px;
  font-weight: 600;
  color: var(--el-text-color-secondary);
  border-bottom: 1px solid var(--el-border-color-lighter);
  background: var(--el-fill-color-light);
}

.ann-editor__input {
  flex: 1;

  :deep(.el-textarea__inner) {
    min-height: 320px !important;
    height: 100%;
    border: none;
    border-radius: 0;
    box-shadow: none !important;
    background: transparent;
    font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
    font-size: 13px;
    line-height: 1.65;
    resize: vertical;
  }
}

.ann-preview {
  flex: 1;
  min-height: 320px;
  max-height: 480px;
  overflow: auto;
  padding: 14px 16px 20px;
  font-size: 14px;
  line-height: 1.7;
  word-break: break-word;

  &--empty {
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--el-text-color-secondary);
    font-size: 13px;
  }

  :deep(img) {
    max-width: 100%;
  }

  :deep(p) {
    margin: 0 0 0.75em;
  }
}
</style>
