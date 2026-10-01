<template>
  <div class="bbs-create-page" v-loading="loading">
    <div class="bbs-layout">
      <BbsCategoryAside :active-key="asideKey" @select="onAsideSelect" />

      <main class="bbs-main">
        <div class="bbs-toolbar">
          <button type="button" class="bbs-back" @click="goBack">
            <svg viewBox="0 0 24 24" width="14" height="14" aria-hidden="true">
              <path fill="currentColor" d="M15.41 7.41 14 6l-6 6 6 6 1.41-1.41L10.83 12z" />
            </svg>
            {{ isEdit ? t('bbs.backToPost') : t('bbs.backList') }}
          </button>
          <h1 class="bbs-toolbar__title">
            {{
              isEdit
                ? t('bbs.editTitle')
                : isPollMode
                  ? t('bbs.createPollTitle')
                  : t('bbs.createTitle')
            }}
          </h1>
        </div>

        <div class="bbs-form">
          <div class="bbs-field">
            <label class="bbs-field__label">
              <span class="req">*</span>{{ t('bbs.form.title') }}
            </label>
            <el-input
              v-model="form.title"
              maxlength="200"
              show-word-limit
              :placeholder="t('bbs.form.titlePh')"
              class="bbs-field__control"
            />
          </div>

          <div class="bbs-field">
            <label class="bbs-field__label">
              <span class="req">*</span>{{ t('bbs.form.type') }}
            </label>
            <div class="bbs-type-grid" role="radiogroup">
              <button
                v-for="tp in creatableTypes"
                :key="tp"
                type="button"
                class="bbs-type-chip"
                :class="{ 'is-active': form.type === tp }"
                role="radio"
                :aria-checked="form.type === tp"
                @click="form.type = tp"
              >
                <span class="bbs-type-chip__mark">{{ typeShort(tp) }}</span>
                <span>{{ boardTypeLabel(tp) }}</span>
              </button>
            </div>
          </div>

          <div class="bbs-field bbs-field--row">
            <label class="bbs-field__label bbs-field__label--inline">{{ t('bbs.form.anonymous') }}</label>
            <el-switch v-model="form.anonymous" />
            <span class="bbs-field__hint">{{ t('bbs.form.anonymousHint') }}</span>
          </div>

          <template v-if="isPollMode">
            <div class="bbs-field">
              <label class="bbs-field__label">
                <span class="req">*</span>{{ t('bbs.poll.voteMode') }}
              </label>
              <el-radio-group v-model="pollForm.voteMode" :disabled="pollOptionsLocked">
                <el-radio :value="BbsVoteMode.Single">{{ t('bbs.poll.single') }}</el-radio>
                <el-radio :value="BbsVoteMode.Multi">{{ t('bbs.poll.multi') }}</el-radio>
              </el-radio-group>
            </div>
            <div v-if="pollForm.voteMode === BbsVoteMode.Multi" class="bbs-field bbs-field--row">
              <label class="bbs-field__label bbs-field__label--inline">{{ t('bbs.poll.maxChoices') }}</label>
              <el-input-number
                v-model="pollForm.maxChoices"
                :min="2"
                :max="Math.max(2, pollForm.options.filter((x) => x.trim()).length || 2)"
                :disabled="pollOptionsLocked"
                controls-position="right"
              />
              <span class="bbs-field__hint">{{ t('bbs.poll.maxChoicesHint') }}</span>
            </div>
            <div class="bbs-field">
              <label class="bbs-field__label">{{ t('bbs.poll.deadline') }}</label>
              <el-date-picker
                v-model="pollForm.deadline"
                type="datetime"
                value-format="YYYY-MM-DDTHH:mm:ss"
                :placeholder="t('bbs.poll.deadlinePh')"
                :disabled="pollOptionsLocked"
                :disabled-date="pollDeadlineDisabledDate"
                style="width: 100%"
              />
              <span class="bbs-field__hint">{{ t('bbs.poll.deadlineHint') }}</span>
            </div>
            <div class="bbs-field">
              <label class="bbs-field__label">
                <span class="req">*</span>{{ t('bbs.poll.options') }}
              </label>
              <p v-if="pollOptionsLocked" class="bbs-field__hint">{{ t('bbs.poll.optionsLocked') }}</p>
              <div
                v-for="(_opt, idx) in pollForm.options"
                :key="idx"
                class="bbs-poll-option-row"
              >
                <el-input
                  v-model="pollForm.options[idx]"
                  maxlength="100"
                  show-word-limit
                  :disabled="pollOptionsLocked"
                  :placeholder="t('bbs.poll.optionPh', { n: idx + 1 })"
                />
                <el-button
                  v-if="!pollOptionsLocked && pollForm.options.length > 2"
                  link
                  type="danger"
                  @click="removePollOption(idx)"
                >{{ t('bbs.poll.removeOption') }}</el-button>
              </div>
              <el-button
                v-if="!pollOptionsLocked && pollForm.options.length < 20"
                size="small"
                @click="addPollOption"
              >{{ t('bbs.poll.addOption') }}</el-button>
            </div>
          </template>

          <div class="bbs-field">
            <label class="bbs-field__label">{{ t('bbs.media.title') }}</label>
            <div class="bbs-media-actions">
              <el-button size="small" @click="pickImages">{{ t('bbs.media.addImages') }}</el-button>
              <el-button size="small" :disabled="!canAddVideo" @click="pickVideo">
                {{ t('bbs.media.addVideo') }}
              </el-button>
              <span class="bbs-field__hint bbs-media-limit">{{ t('bbs.media.limitsHint') }}</span>
            </div>
            <input
              ref="imageInputRef"
              type="file"
              accept=".jpg,.jpeg,.png,.webp,.gif,image/jpeg,image/png,image/webp,image/gif"
              multiple
              class="bbs-file-input"
              @change="onImagesPicked"
            />
            <input
              ref="videoInputRef"
              type="file"
              accept=".mp4,.webm,video/mp4,video/webm"
              class="bbs-file-input"
              @change="onVideoPicked"
            />
            <div v-if="savedMedia.length || pendingMedia.length" class="bbs-media-grid">
              <div v-for="m in savedMedia" :key="m.id" class="bbs-media-card">
                <img v-if="m.kind === 'image' && m.blobUrl" :src="m.blobUrl" alt="" class="bbs-media-thumb" />
                <div v-else class="bbs-media-thumb bbs-media-thumb--video">{{ t('bbs.media.videoBadge') }}</div>
                <div class="bbs-media-name" :title="m.originalFileName">{{ m.originalFileName }}</div>
                <div class="bbs-media-card__actions">
                  <el-button link type="primary" size="small" @click="insertSaved(m)">
                    {{ t('bbs.media.insert') }}
                  </el-button>
                  <el-button
                    v-if="canManageMedia"
                    link
                    type="danger"
                    size="small"
                    @click="removeSaved(m.id)"
                  >{{ t('bbs.delete') }}</el-button>
                </div>
              </div>
              <div v-for="p in pendingMedia" :key="p.localId" class="bbs-media-card is-pending">
                <img v-if="p.kind === 'image'" :src="p.previewUrl" alt="" class="bbs-media-thumb" />
                <div v-else class="bbs-media-thumb bbs-media-thumb--video">{{ t('bbs.media.videoBadge') }}</div>
                <div class="bbs-media-name" :title="p.file.name">{{ p.file.name }}</div>
                <div class="bbs-media-card__actions">
                  <el-button link type="primary" size="small" @click="insertPending(p)">
                    {{ t('bbs.media.insert') }}
                  </el-button>
                  <el-button link type="danger" size="small" @click="removePending(p.localId)">
                    {{ t('bbs.delete') }}
                  </el-button>
                </div>
              </div>
            </div>
          </div>

          <div class="bbs-field">
            <div class="bbs-content-head">
              <label class="bbs-field__label bbs-field__label--inline">
                <span class="req">*</span>{{ t('bbs.form.content') }}
              </label>
              <div class="bbs-content-head__right">
                <el-radio-group v-model="contentMode" size="small" @change="onContentModeChange">
                  <el-radio-button label="rich">{{ t('bbs.form.modeRich') }}</el-radio-button>
                  <el-radio-button label="markdown">{{ t('bbs.form.modeMarkdown') }}</el-radio-button>
                </el-radio-group>
                <el-radio-group
                  v-if="contentMode === 'markdown'"
                  v-model="editorLayout"
                  size="small"
                  class="bbs-layout-switch"
                >
                  <el-radio-button label="split">{{ t('bbs.form.layoutSplit') }}</el-radio-button>
                  <el-radio-button label="tab">{{ t('bbs.form.layoutTab') }}</el-radio-button>
                </el-radio-group>
              </div>
            </div>

            <BbsRichEditor
              v-if="contentMode === 'rich'"
              ref="richEditorRef"
              v-model="form.content"
              :placeholder="t('bbs.form.richPh')"
            />

            <div
              v-else
              class="bbs-editor"
              :class="editorLayout === 'tab' ? 'bbs-editor--tab' : 'bbs-editor--split'"
            >
              <div v-if="editorLayout === 'tab'" class="bbs-editor__tabs" role="tablist">
                <button
                  type="button"
                  role="tab"
                  class="bbs-editor__tab"
                  :class="{ 'is-active': activeTab === 'edit' }"
                  :aria-selected="activeTab === 'edit'"
                  @click="activeTab = 'edit'"
                >{{ t('bbs.form.editPane') }}</button>
                <button
                  type="button"
                  role="tab"
                  class="bbs-editor__tab"
                  :class="{ 'is-active': activeTab === 'preview' }"
                  :aria-selected="activeTab === 'preview'"
                  @click="activeTab = 'preview'"
                >{{ t('bbs.form.previewPane') }}</button>
              </div>

              <div class="bbs-editor__body">
                <div
                  v-show="editorLayout === 'split' || activeTab === 'edit'"
                  class="bbs-editor__pane bbs-editor__pane--edit"
                >
                  <div v-if="editorLayout === 'split'" class="bbs-editor__pane-head">
                    {{ t('bbs.form.editPane') }}
                  </div>
                  <el-input
                    ref="contentInputRef"
                    v-model="form.content"
                    type="textarea"
                    :rows="16"
                    :placeholder="t('bbs.form.contentPh')"
                    class="bbs-field__control bbs-field__control--body"
                    @blur="rememberCursor"
                    @click="rememberCursor"
                    @keyup="rememberCursor"
                  />
                </div>
                <div
                  v-show="editorLayout === 'split' || activeTab === 'preview'"
                  class="bbs-editor__pane bbs-editor__pane--preview"
                >
                  <div v-if="editorLayout === 'split'" class="bbs-editor__pane-head">
                    {{ t('bbs.form.previewPane') }}
                  </div>
                  <div
                    v-if="!form.content.trim()"
                    class="bbs-preview bbs-preview--empty"
                  >{{ t('bbs.form.previewEmpty') }}</div>
                  <div
                    v-else
                    class="bbs-preview markdown-body"
                    v-html="previewHtml"
                  />
                </div>
              </div>
            </div>
            <div class="bbs-field__hint">{{ contentModeHint }}</div>
          </div>

          <div class="bbs-form__actions">
            <el-button type="primary" :loading="saving" @click="submit">{{ t('bbs.form.submit') }}</el-button>
            <el-button @click="goBack">{{ t('bbs.form.cancel') }}</el-button>
          </div>
        </div>
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { InputInstance } from 'element-plus'
import {
  bbsApi,
  BbsSubjectType,
  BbsSubjectTypeI18nKey,
  BbsFixedBoardTypes,
  BbsBuiltinMovableTypes,
  BbsSubjectKind,
  BbsVoteMode,
  bbsIsAdminOnlyPostType,
  bbsIsCustomBoardType,
  bbsSupportsBoardModerator,
  type BbsBoardModerator,
  type BbsMediaItem
} from '@/api/bbs'
import BbsCategoryAside, { type BbsCategoryKey } from '@/components/Bbs/BbsCategoryAside.vue'
import BbsRichEditor from '@/components/Bbs/BbsRichEditor.vue'
import apiClient from '@/api/client'
import {
  isLikelyHtmlContent,
  renderBbsContent
} from '@/utils/sanitizeAnnouncementHtml'
import { getApiErrorMessage } from '@/utils/apiError'
import { useAuthStore } from '@/stores/auth'

const MAX_IMAGES = 50
const MAX_VIDEOS = 1
const MAX_IMAGE_BYTES = 5 * 1024 * 1024
const MAX_VIDEO_BYTES = 50 * 1024 * 1024
const IMAGE_EXT = new Set(['.jpg', '.jpeg', '.png', '.webp', '.gif'])
const VIDEO_EXT = new Set(['.mp4', '.webm'])
const PENDING_PREFIX = 'bbs-pending:'

type PendingMedia = {
  localId: string
  file: File
  kind: 'image' | 'video'
  previewUrl: string
}

type SavedMediaView = BbsMediaItem & { blobUrl?: string }

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const isSysAdmin = computed(() => authStore.user?.isSysAdmin === true)
const creatableTypes = computed(() => {
  const notDeleted = (tp: number) => !boardSettings.value.find((x) => Number(x.type) === tp)?.isDeleted
  const orderOf = (tp: number) => {
    const n = Number(boardSettings.value.find((x) => Number(x.type) === tp)?.sortOrder ?? 0)
    if (n > 0) return n
    const defaults: Record<number, number> = {
      [BbsSubjectType.CompanyNotice]: 10,
      [BbsSubjectType.IndustryNews]: 20,
      [BbsSubjectType.Share]: 30,
      [BbsSubjectType.Suggestion]: 40
    }
    if (defaults[tp] != null) return defaults[tp]
    if (bbsIsCustomBoardType(tp)) return 100 + (tp - BbsSubjectType.CustomMin) * 10
    return 100
  }
  const fixed = isSysAdmin.value
    ? [...BbsFixedBoardTypes].filter(notDeleted)
    : []
  const movableSet = new Set<number>()
  for (const tp of BbsBuiltinMovableTypes) {
    if (notDeleted(tp)) movableSet.add(tp)
  }
  for (const m of boardSettings.value) {
    const tp = Number(m.type)
    if (!bbsSupportsBoardModerator(tp) || m.isDeleted) continue
    movableSet.add(tp)
  }
  const movable = [...movableSet].sort((a, b) => {
    const oa = orderOf(a)
    const ob = orderOf(b)
    if (oa !== ob) return oa - ob
    return a - b
  })
  return [...fixed, ...movable]
})
const boardSettings = ref<BbsBoardModerator[]>([])

function boardTypeLabel(type: number) {
  const m = boardSettings.value.find((x) => Number(x.type) === type)
  const custom = (m?.displayName || '').trim()
  if (custom) return custom
  if (m?.defaultName) return m.defaultName
  const key = BbsSubjectTypeI18nKey[type]
  return key ? t(key) : bbsIsCustomBoardType(type) ? t('bbs.moderator.customBoard', { type }) : String(type)
}

const editId = computed(() => (typeof route.params.id === 'string' ? route.params.id : ''))
const isEdit = computed(() => !!editId.value && route.name === 'BbsEdit')
const loading = ref(false)
const saving = ref(false)
const canManageMedia = ref(true)
const form = reactive({
  title: '',
  content: '',
  type: BbsSubjectType.Share as number,
  anonymous: false
})
const isPollMode = ref(false)
const pollOptionsLocked = ref(false)
const pollForm = reactive({
  voteMode: BbsVoteMode.Single as number,
  maxChoices: null as number | null,
  deadline: null as string | null,
  options: ['', ''] as string[]
})

function addPollOption() {
  if (pollForm.options.length >= 20) return
  pollForm.options.push('')
}

function removePollOption(idx: number) {
  if (pollForm.options.length <= 2) return
  pollForm.options.splice(idx, 1)
}

/** 截止日不可早于当天（按本地日期）。 */
function pollDeadlineDisabledDate(date: Date) {
  const start = new Date()
  start.setHours(0, 0, 0, 0)
  return date.getTime() < start.getTime()
}
const pendingMedia = ref<PendingMedia[]>([])
const savedMedia = ref<SavedMediaView[]>([])
const imageInputRef = ref<HTMLInputElement | null>(null)
const videoInputRef = ref<HTMLInputElement | null>(null)
const contentInputRef = ref<InputInstance | null>(null)
const richEditorRef = ref<InstanceType<typeof BbsRichEditor> | null>(null)
const blobUrls = ref<string[]>([])
const cursorPos = ref(0)
const CONTENT_MODE_KEY = 'bbs.create.contentMode'
const LAYOUT_KEY = 'bbs.create.editorLayout'
const contentMode = ref<'rich' | 'markdown'>(
  typeof localStorage !== 'undefined' && localStorage.getItem(CONTENT_MODE_KEY) === 'markdown'
    ? 'markdown'
    : 'rich'
)
const editorLayout = ref<'split' | 'tab'>(
  typeof localStorage !== 'undefined' && localStorage.getItem(LAYOUT_KEY) === 'tab'
    ? 'tab'
    : 'split'
)
const activeTab = ref<'edit' | 'preview'>('edit')
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
    localStorage.setItem(CONTENT_MODE_KEY, v)
  } catch {
    /* ignore */
  }
})

const contentModeHint = computed(() =>
  contentMode.value === 'rich' ? t('bbs.form.richHint') : t('bbs.form.contentHint')
)

const TYPE_SHORT: Record<number, string> = {
  [BbsSubjectType.CompanyNotice]: '通',
  [BbsSubjectType.IndustryNews]: '讯',
  [BbsSubjectType.Share]: '享',
  [BbsSubjectType.OpsGuide]: '说',
  [BbsSubjectType.Suggestion]: '建',
  [BbsSubjectType.SystemUpdate]: '更'
}

const asideKey = computed<BbsCategoryKey>(() => form.type)

const pendingImageCount = computed(() => pendingMedia.value.filter((x) => x.kind === 'image').length)
const pendingVideoCount = computed(() => pendingMedia.value.filter((x) => x.kind === 'video').length)
const savedImageCount = computed(() => savedMedia.value.filter((x) => x.kind === 'image').length)
const savedVideoCount = computed(() => savedMedia.value.filter((x) => x.kind === 'video').length)
const canAddVideo = computed(
  () => savedVideoCount.value + pendingVideoCount.value < MAX_VIDEOS
)

const previewHtml = computed(() => {
  let md = form.content || ''
  for (const p of pendingMedia.value) {
    md = md.split(`${PENDING_PREFIX}${p.localId}`).join(p.previewUrl)
    md = md.split(p.previewUrl).join(p.previewUrl)
  }
  for (const m of savedMedia.value) {
    if (!m.blobUrl) continue
    const path = m.previewPath || `/api/v1/documents/${m.id}/preview`
    md = md.split(path).join(m.blobUrl)
  }
  return renderBbsContent(md)
})

async function onContentModeChange(next: string | number | boolean | undefined) {
  const mode = String(next) as 'rich' | 'markdown'
  if (modeSwitchGuard) return
  const prev = mode === 'rich' ? 'markdown' : 'rich'
  if (!form.content.trim()) {
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
      // MD → HTML
      form.content = renderBbsContent(form.content)
    } else if (mode === 'markdown' && prev === 'rich') {
      // HTML → 纯文本（保底）；复杂 HTML 用户可改回富文本
      const tmp = document.createElement('div')
      tmp.innerHTML = form.content
      form.content = (tmp.innerText || tmp.textContent || '').trim()
    }
    contentMode.value = mode
  } catch {
    modeSwitchGuard = true
    contentMode.value = prev
    await nextTick()
    modeSwitchGuard = false
  }
}

function typeShort(type: number) {
  return TYPE_SHORT[type] ?? '帖'
}

function extOf(name: string) {
  const i = name.lastIndexOf('.')
  return i >= 0 ? name.slice(i).toLowerCase() : ''
}

function pendingToken(localId: string) {
  return `${PENDING_PREFIX}${localId}`
}

function pendingMarkdown(p: PendingMedia) {
  if (p.kind === 'video') {
    return `\n\n<video controls src="${pendingToken(p.localId)}"></video>\n`
  }
  const alt = (p.file.name || 'image').replace(/[\[\]]/g, '')
  return `\n\n![${alt}](${pendingToken(p.localId)})\n`
}

function savedMarkdown(item: BbsMediaItem) {
  const path = item.previewPath || `/api/v1/documents/${item.id}/preview`
  if (item.kind === 'video') {
    return `\n\n<video controls src="${path}"></video>\n`
  }
  const alt = (item.originalFileName || 'image').replace(/[\[\]]/g, '')
  return `\n\n![${alt}](${path})\n`
}

function stripPendingFromContent(content: string, localId: string) {
  const token = localId.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
  const imgRe = new RegExp(`!\\[[^\\]]*\\]\\(${PENDING_PREFIX}${token}\\)`, 'gi')
  const videoRe = new RegExp(
    `<video[^>]*src=["']${PENDING_PREFIX}${token}["'][^>]*>\\s*</video>`,
    'gi'
  )
  return content.replace(imgRe, '').replace(videoRe, '')
}

function stripPendingBlobFromContent(content: string, previewUrl: string) {
  const esc = previewUrl.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
  const imgRe = new RegExp(`<img[^>]*src=["']${esc}["'][^>]*/?>`, 'gi')
  const videoRe = new RegExp(`<video[^>]*src=["']${esc}["'][^>]*>\\s*</video>`, 'gi')
  return content.replace(imgRe, '').replace(videoRe, '')
}

function stripMediaFromContent(content: string, documentId: string) {
  const id = documentId.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
  const imgMd = new RegExp(`!\\[[^\\]]*\\]\\(/api/v1/documents/${id}/preview\\)`, 'gi')
  const videoMd = new RegExp(
    `<video[^>]*src=["']/api/v1/documents/${id}/preview["'][^>]*>\\s*</video>`,
    'gi'
  )
  const imgHtml = new RegExp(`<img[^>]*src=["']/api/v1/documents/${id}/preview["'][^>]*/?>`, 'gi')
  return content.replace(imgMd, '').replace(videoMd, '').replace(imgHtml, '')
}

function contentWithPendingTokens(content: string): string {
  let next = content
  for (const p of pendingMedia.value) {
    if (p.previewUrl && next.includes(p.previewUrl)) {
      next = next.split(p.previewUrl).join(pendingToken(p.localId))
    }
  }
  for (const m of savedMedia.value) {
    if (m.blobUrl && next.includes(m.blobUrl)) {
      const path = m.previewPath || `/api/v1/documents/${m.id}/preview`
      next = next.split(m.blobUrl).join(path)
    }
  }
  return next
}

function getTextarea(): HTMLTextAreaElement | null {
  const root = contentInputRef.value?.$el as HTMLElement | undefined
  return root?.querySelector?.('textarea') ?? null
}

function rememberCursor() {
  const ta = getTextarea()
  if (ta) cursorPos.value = ta.selectionStart ?? form.content.length
}

function insertAtCursor(snippet: string) {
  const ta = getTextarea()
  const useLive = !!(ta && document.activeElement === ta)
  const start = useLive ? (ta!.selectionStart ?? cursorPos.value) : cursorPos.value
  const end = useLive ? (ta!.selectionEnd ?? start) : start
  const before = form.content.slice(0, start)
  const after = form.content.slice(end)
  form.content = `${before}${snippet}${after}`
  const nextPos = before.length + snippet.length
  cursorPos.value = nextPos
  void nextTick(() => {
    const el = getTextarea()
    if (!el) return
    el.focus()
    el.setSelectionRange(nextPos, nextPos)
  })
}

function insertPending(p: PendingMedia) {
  if (contentMode.value === 'rich') {
    const alt = (p.file.name || 'image').replace(/"/g, '')
    const html =
      p.kind === 'video'
        ? `<p><video controls src="${p.previewUrl}"></video></p>`
        : `<p><img src="${p.previewUrl}" alt="${alt}"></p>`
    richEditorRef.value?.insertHtml(html)
    return
  }
  insertAtCursor(pendingMarkdown(p))
}

function insertSaved(m: SavedMediaView) {
  if (contentMode.value === 'rich') {
    const src = m.blobUrl || m.previewPath || `/api/v1/documents/${m.id}/preview`
    const alt = (m.originalFileName || 'image').replace(/"/g, '')
    const html =
      m.kind === 'video'
        ? `<p><video controls src="${src}"></video></p>`
        : `<p><img src="${src}" alt="${alt}"></p>`
    richEditorRef.value?.insertHtml(html)
    return
  }
  insertAtCursor(savedMarkdown(m))
}

function onAsideSelect(key: BbsCategoryKey) {
  if (key === 'all') {
    router.push({ name: 'BbsList' })
    return
  }
  if (key === 'top') {
    router.push({ name: 'BbsList', query: { cat: 'top' } })
    return
  }
  if (bbsIsAdminOnlyPostType(key) && !isSysAdmin.value) {
    router.push({ name: 'BbsList', query: { type: String(key) } })
    return
  }
  form.type = key
}

function pickImages() {
  rememberCursor()
  imageInputRef.value?.click()
}

function pickVideo() {
  if (!canAddVideo.value) {
    ElMessage.warning(t('bbs.media.videoLimit'))
    return
  }
  rememberCursor()
  videoInputRef.value?.click()
}

function onImagesPicked(ev: Event) {
  const input = ev.target as HTMLInputElement
  const files = Array.from(input.files || [])
  input.value = ''
  const added: PendingMedia[] = []
  for (const file of files) {
    const ext = extOf(file.name)
    if (!IMAGE_EXT.has(ext)) {
      ElMessage.warning(t('bbs.media.badImageType', { name: file.name }))
      continue
    }
    if (file.size > MAX_IMAGE_BYTES) {
      ElMessage.warning(t('bbs.media.imageTooLarge', { name: file.name }))
      continue
    }
    if (savedImageCount.value + pendingImageCount.value + added.length >= MAX_IMAGES) {
      ElMessage.warning(t('bbs.media.imageLimit'))
      break
    }
    const previewUrl = URL.createObjectURL(file)
    blobUrls.value.push(previewUrl)
    added.push({
      localId: crypto.randomUUID(),
      file,
      kind: 'image',
      previewUrl
    })
  }
  if (!added.length) return
  pendingMedia.value.push(...added)
  for (const p of added) insertAtCursor(pendingMarkdown(p))
}

function onVideoPicked(ev: Event) {
  const input = ev.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return
  const ext = extOf(file.name)
  if (!VIDEO_EXT.has(ext)) {
    ElMessage.warning(t('bbs.media.badVideoType', { name: file.name }))
    return
  }
  if (file.size > MAX_VIDEO_BYTES) {
    ElMessage.warning(t('bbs.media.videoTooLarge', { name: file.name }))
    return
  }
  if (!canAddVideo.value) {
    ElMessage.warning(t('bbs.media.videoLimit'))
    return
  }
  const previewUrl = URL.createObjectURL(file)
  blobUrls.value.push(previewUrl)
  const p: PendingMedia = {
    localId: crypto.randomUUID(),
    file,
    kind: 'video',
    previewUrl
  }
  pendingMedia.value.push(p)
  insertAtCursor(pendingMarkdown(p))
}

function removePending(localId: string) {
  const idx = pendingMedia.value.findIndex((x) => x.localId === localId)
  if (idx < 0) return
  const [removed] = pendingMedia.value.splice(idx, 1)
  form.content = stripPendingFromContent(form.content, localId)
  form.content = stripPendingBlobFromContent(form.content, removed.previewUrl)
  try {
    URL.revokeObjectURL(removed.previewUrl)
  } catch {
    /* ignore */
  }
}

async function removeSaved(id: string) {
  if (!canManageMedia.value) return
  try {
    await bbsApi.deleteMedia(id)
    savedMedia.value = savedMedia.value.filter((x) => x.id !== id)
    form.content = stripMediaFromContent(form.content, id)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.actionFailed')))
  }
}

async function loadSavedBlobs(list: BbsMediaItem[]) {
  const views: SavedMediaView[] = []
  for (const m of list) {
    const row: SavedMediaView = { ...m }
    if (m.id) {
      try {
        const blob = await apiClient.getBlob(`/api/v1/documents/${encodeURIComponent(m.id)}/preview`)
        if (blob?.size) {
          row.blobUrl = URL.createObjectURL(blob)
          blobUrls.value.push(row.blobUrl)
        }
      } catch {
        /* ignore */
      }
    }
    views.push(row)
  }
  savedMedia.value = views
}

async function uploadAndReplacePending(subjectId: string, content: string): Promise<string> {
  let next = content
  const used = pendingMedia.value.filter((p) => next.includes(pendingToken(p.localId)))
  for (const p of used) {
    const part = await bbsApi.uploadMedia(subjectId, [p.file])
    const item = part[0]
    if (!item?.id) continue
    const path = item.previewPath || `/api/v1/documents/${item.id}/preview`
    next = next.split(pendingToken(p.localId)).join(path)
  }
  return next
}

function clearPending() {
  for (const p of pendingMedia.value) {
    try {
      URL.revokeObjectURL(p.previewUrl)
    } catch {
      /* ignore */
    }
  }
  pendingMedia.value = []
}

async function load() {
  if (!isEdit.value) {
    isPollMode.value = String(route.query.kind || '') === 'poll'
    pollOptionsLocked.value = false
    pollForm.voteMode = BbsVoteMode.Single
    pollForm.maxChoices = null
    pollForm.deadline = null
    pollForm.options = ['', '']
    const typeQ = Number(route.query.type)
    if (
      Number.isFinite(typeQ) &&
      (BbsBuiltinMovableTypes.includes(typeQ as (typeof BbsBuiltinMovableTypes)[number]) ||
        BbsFixedBoardTypes.includes(typeQ as (typeof BbsFixedBoardTypes)[number]) ||
        bbsIsCustomBoardType(typeQ)) &&
      (isSysAdmin.value || !bbsIsAdminOnlyPostType(typeQ))
    ) {
      form.type = typeQ
    } else if (bbsIsAdminOnlyPostType(form.type) && !isSysAdmin.value) {
      form.type = BbsSubjectType.Share
    }
    canManageMedia.value = true
    return
  }
  loading.value = true
  try {
    const d = await bbsApi.detail(editId.value)
    form.title = d.title
    form.type = d.type
    form.anonymous = d.anonymous
    isPollMode.value = Number(d.kind) === BbsSubjectKind.Poll
    pollOptionsLocked.value = isPollMode.value && Number(d.voteCount || 0) > 0
    if (isPollMode.value && d.poll) {
      pollForm.voteMode = Number(d.poll.voteMode) || BbsVoteMode.Single
      pollForm.maxChoices = d.poll.voteMaxChoices ?? null
      pollForm.deadline = d.poll.voteDeadline
        ? String(d.poll.voteDeadline).slice(0, 19)
        : null
      const texts = (d.poll.options || []).map((o) => o.text)
      pollForm.options = texts.length >= 2 ? texts : ['', '']
    }
    canManageMedia.value = !!(d.canEdit || d.canModerate)
    contentMode.value = isLikelyHtmlContent(d.content) ? 'rich' : 'markdown'
    const media = await bbsApi.listMedia(editId.value)
    await loadSavedBlobs(media)
    let body = d.content || ''
    if (contentMode.value === 'rich') {
      for (const m of savedMedia.value) {
        if (!m.blobUrl) continue
        const path = m.previewPath || `/api/v1/documents/${m.id}/preview`
        body = body.split(path).join(m.blobUrl)
      }
    }
    form.content = body
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.loadFailed')))
  } finally {
    loading.value = false
  }
}

async function submit() {
  if (!form.title.trim() || !form.content.trim()) {
    ElMessage.warning(t('bbs.form.required'))
    return
  }
  if (bbsIsAdminOnlyPostType(form.type) && !isSysAdmin.value) {
    ElMessage.error(t('bbs.form.adminOnlyType'))
    return
  }

  let pollPayload: {
    kind?: number
    voteMode?: number
    voteMaxChoices?: number | null
    voteDeadline?: string | null
    pollOptions?: string[]
  } = {}
  if (isPollMode.value) {
    const pollOptions = pollForm.options.map((x) => x.trim()).filter(Boolean)
    if (!pollOptionsLocked.value && pollOptions.length < 2) {
      ElMessage.warning(t('bbs.poll.optionsMin'))
      return
    }
    if (
      !pollOptionsLocked.value &&
      pollForm.voteMode === BbsVoteMode.Multi &&
      pollForm.maxChoices != null &&
      (pollForm.maxChoices < 2 || pollForm.maxChoices > pollOptions.length)
    ) {
      ElMessage.warning(t('bbs.poll.maxChoicesInvalid'))
      return
    }
    if (pollForm.deadline) {
      const d = new Date(pollForm.deadline)
      const start = new Date()
      start.setHours(0, 0, 0, 0)
      if (Number.isNaN(d.getTime()) || d.getTime() < start.getTime()) {
        ElMessage.warning(t('bbs.poll.deadlineTooEarly'))
        return
      }
    }
    pollPayload = {
      kind: BbsSubjectKind.Poll,
      voteMode: pollForm.voteMode,
      voteMaxChoices:
        pollForm.voteMode === BbsVoteMode.Multi ? pollForm.maxChoices : null,
      voteDeadline: pollForm.deadline || null,
      pollOptions: pollOptionsLocked.value ? undefined : pollOptions
    }
  }

  let working = contentWithPendingTokens(form.content.trim())
  const unused = pendingMedia.value.filter(
    (p) =>
      !working.includes(pendingToken(p.localId)) && !form.content.includes(p.previewUrl)
  )
  if (unused.length) {
    ElMessage.warning(t('bbs.media.unusedPending'))
  }

  saving.value = true
  try {
    const body = {
      title: form.title.trim(),
      content: working,
      type: form.type,
      anonymous: form.anonymous,
      ...pollPayload
    }
    let d = isEdit.value
      ? await bbsApi.update(editId.value, body)
      : await bbsApi.create(body)

    const hasPendingTokens = pendingMedia.value.some((p) =>
      working.includes(pendingToken(p.localId))
    )
    if (hasPendingTokens) {
      const nextContent = await uploadAndReplacePending(d.id, working)
      d = await bbsApi.update(d.id, {
        title: form.title.trim(),
        content: nextContent,
        type: form.type,
        anonymous: form.anonymous,
        ...pollPayload
      })
    }
    clearPending()

    ElMessage.success(t('bbs.form.saved'))
    router.replace({ name: 'BbsDetail', params: { id: d.id } })
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.saveFailed')))
  } finally {
    saving.value = false
  }
}

function goBack() {
  if (isEdit.value) router.push({ name: 'BbsDetail', params: { id: editId.value } })
  else router.push({ name: 'BbsList' })
}

onMounted(() => {
  void load()
  void bbsApi.boardModerators().then((list) => {
    boardSettings.value = list || []
  }).catch(() => {
    boardSettings.value = []
  })
})

onUnmounted(() => {
  for (const u of blobUrls.value) {
    try {
      URL.revokeObjectURL(u)
    } catch {
      /* ignore */
    }
  }
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.bbs-create-page {
  padding: 16px 20px 40px;
  min-height: 100%;
}

.bbs-layout {
  display: grid;
  grid-template-columns: 268px minmax(0, 1fr);
  gap: 16px;
  align-items: start;
}

.bbs-main {
  min-width: 0;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.bbs-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
  padding: 12px 14px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.bbs-back {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 8px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: $text-secondary;
  font-size: 13px;
  cursor: pointer;

  &:hover {
    color: $cyan-primary;
    background: rgba(0, 212, 255, 0.06);
  }
}

.bbs-toolbar__title {
  margin: 0;
  font-size: 16px;
  font-weight: 700;
  color: $text-primary;
}

.bbs-form {
  padding: 18px 20px 24px;
}

.bbs-field {
  margin-bottom: 18px;

  &--row {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 12px;
  }
}

.bbs-field__label {
  display: block;
  margin-bottom: 8px;
  font-size: 13px;
  font-weight: 600;
  color: $text-secondary;

  &--inline {
    margin-bottom: 0;
  }
}

.req {
  margin-right: 4px;
  color: #f87171;
}

.bbs-field__hint {
  margin-top: 6px;
  font-size: 12px;
  color: $text-muted;
}

.bbs-field--row .bbs-field__hint {
  margin-top: 0;
}

.bbs-poll-option-row {
  display: flex;
  gap: 8px;
  align-items: center;
  margin-bottom: 8px;
}

.bbs-type-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  gap: 8px;
}

.bbs-type-chip {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 12px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.02);
  color: $text-secondary;
  font-size: 13px;
  text-align: left;
  cursor: pointer;
  transition: background 0.12s ease, border-color 0.12s ease, color 0.12s ease;

  &:hover {
    border-color: rgba(0, 212, 255, 0.35);
    color: $text-primary;
  }

  &.is-active {
    border-color: rgba(0, 212, 255, 0.55);
    background: rgba(0, 212, 255, 0.12);
    color: $cyan-primary;
    font-weight: 600;
  }
}

.bbs-type-chip__mark {
  flex: 0 0 22px;
  height: 22px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  background: rgba(255, 255, 255, 0.06);
  font-size: 12px;
  font-weight: 700;
}

.bbs-media-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-bottom: 10px;
}

.bbs-media-limit {
  margin-top: 0;
}

.bbs-file-input {
  display: none;
}

.bbs-media-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
  gap: 10px;
}

.bbs-media-card {
  padding: 8px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.02);

  &.is-pending {
    border-style: dashed;
    border-color: rgba(0, 212, 255, 0.35);
  }
}

.bbs-media-thumb {
  width: 100%;
  height: 72px;
  object-fit: cover;
  border-radius: 6px;
  background: rgba(0, 0, 0, 0.2);

  &--video {
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    font-weight: 600;
    color: $cyan-primary;
  }
}

.bbs-media-name {
  margin: 6px 0 4px;
  font-size: 11px;
  color: $text-muted;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.bbs-media-card__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 2px;
}

.bbs-content-head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 8px;
}

.bbs-content-head__right {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.bbs-layout-switch {
  flex-shrink: 0;
}

.bbs-editor {
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 8px;
  overflow: hidden;
  background: rgba(255, 255, 255, 0.02);
}

.bbs-editor__tabs {
  display: flex;
  gap: 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  background: rgba(0, 0, 0, 0.15);
}

.bbs-editor__tab {
  padding: 9px 16px;
  border: none;
  border-bottom: 2px solid transparent;
  background: transparent;
  color: $text-muted;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;

  &:hover {
    color: $text-primary;
  }

  &.is-active {
    color: $cyan-primary;
    border-bottom-color: $cyan-primary;
  }
}

.bbs-editor__body {
  display: grid;
  align-items: stretch;
}

.bbs-editor--split .bbs-editor__body {
  grid-template-columns: 1fr 1fr;
}

.bbs-editor--tab .bbs-editor__body {
  grid-template-columns: 1fr;
}

.bbs-editor__pane {
  min-width: 0;
  display: flex;
  flex-direction: column;
  min-height: 360px;
}

.bbs-editor--split .bbs-editor__pane--edit {
  border-right: 1px solid rgba(255, 255, 255, 0.06);
}

.bbs-editor__pane-head {
  padding: 8px 12px;
  font-size: 12px;
  font-weight: 600;
  color: $text-muted;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  background: rgba(0, 0, 0, 0.12);
}

.bbs-field__control {
  width: 100%;
}

.bbs-field__control--body {
  flex: 1;

  :deep(.el-textarea__inner) {
    min-height: 360px !important;
    height: 100%;
    border: none;
    border-radius: 0;
    box-shadow: none !important;
    background: transparent;
    color: $text-primary;
    font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
    font-size: 13px;
    line-height: 1.65;
    resize: vertical;
  }
}

.bbs-preview {
  flex: 1;
  min-height: 360px;
  max-height: 640px;
  overflow: auto;
  padding: 14px 16px 20px;
  font-size: 14px;
  line-height: 1.7;
  color: $text-primary;
  word-break: break-word;

  &--empty {
    display: flex;
    align-items: center;
    justify-content: center;
    color: $text-muted;
    font-size: 13px;
  }

  :deep(h1),
  :deep(h2),
  :deep(h3),
  :deep(h4),
  :deep(h5),
  :deep(h6) {
    margin: 1em 0 0.45em;
    font-weight: 700;
    line-height: 1.3;
    color: $text-primary;
  }

  :deep(h1) {
    font-size: 1.55em;
    border-bottom: 1px solid rgba(255, 255, 255, 0.08);
    padding-bottom: 0.25em;
  }

  :deep(h2) {
    font-size: 1.3em;
  }

  :deep(h3) {
    font-size: 1.12em;
  }

  :deep(p) {
    margin: 0 0 0.75em;
    color: $text-secondary;
  }

  :deep(ul),
  :deep(ol) {
    margin: 0 0 0.75em;
    padding-left: 1.4em;
    color: $text-secondary;
  }

  :deep(li) {
    margin: 0.2em 0;
  }

  :deep(blockquote) {
    margin: 0 0 0.75em;
    padding: 6px 12px;
    border-left: 3px solid rgba(0, 212, 255, 0.45);
    color: $text-muted;
    background: rgba(255, 255, 255, 0.03);
  }

  :deep(code) {
    padding: 1px 5px;
    border-radius: 4px;
    background: rgba(255, 255, 255, 0.08);
    font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
    font-size: 0.9em;
    color: #fbbf24;
  }

  :deep(pre) {
    margin: 0 0 0.75em;
    padding: 10px 12px;
    overflow: auto;
    border-radius: 8px;
    background: rgba(0, 0, 0, 0.35);
    border: 1px solid rgba(255, 255, 255, 0.06);

    code {
      padding: 0;
      background: transparent;
      color: $text-secondary;
    }
  }

  :deep(a) {
    color: $cyan-primary;
    text-decoration: underline;
    text-underline-offset: 2px;
  }

  :deep(hr) {
    margin: 1em 0;
    border: none;
    border-top: 1px solid rgba(255, 255, 255, 0.1);
  }

  :deep(table) {
    width: 100%;
    margin: 0 0 0.75em;
    border-collapse: collapse;
    font-size: 13px;
  }

  :deep(th),
  :deep(td) {
    border: 1px solid rgba(255, 255, 255, 0.1);
    padding: 6px 8px;
    text-align: left;
  }

  :deep(th) {
    background: rgba(255, 255, 255, 0.04);
    color: $text-primary;
  }

  :deep(strong) {
    font-weight: 700;
    color: $text-primary;
  }

  :deep(em) {
    font-style: italic;
  }

  :deep(img),
  :deep(video) {
    max-width: 100%;
    height: auto;
    border-radius: 8px;
    margin: 8px 0;
    display: block;
  }

  :deep(video) {
    width: min(100%, 480px);
    background: #000;
  }
}

.bbs-form__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  padding-top: 4px;
}

@media (max-width: 1100px) {
  .bbs-editor--split .bbs-editor__body {
    grid-template-columns: 1fr;
  }

  .bbs-editor--split .bbs-editor__pane--edit {
    border-right: none;
    border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  }
}

@media (max-width: 900px) {
  .bbs-layout {
    grid-template-columns: 1fr;
  }
}
</style>
