<template>
  <div class="bbs-detail-page" v-loading="loading">
    <div class="bbs-layout">
      <BbsCategoryAside :active-key="sidebarKey" @select="goCategory" />

      <main v-if="detail" class="bbs-main">
        <div class="bbs-toolbar">
          <button type="button" class="bbs-back" @click="goList">
            <svg viewBox="0 0 24 24" width="14" height="14" aria-hidden="true">
              <path fill="currentColor" d="M15.41 7.41 14 6l-6 6 6 6 1.41-1.41L10.83 12z" />
            </svg>
            {{ t('bbs.backList') }}
          </button>
          <div class="bbs-toolbar__actions">
            <el-button v-if="detail.canEdit" type="primary" size="small" @click="goEdit">{{ t('bbs.edit') }}</el-button>
            <el-button
              v-if="detail.canModerate || detail.canEdit"
              size="small"
              @click="toggleClose"
            >{{ detail.status === BbsSubjectStatus.Close ? t('bbs.open') : t('bbs.close') }}</el-button>
            <el-button v-if="detail.canSetTop" size="small" @click="toggleTop">
              {{ detail.isTop ? t('bbs.untop') : t('bbs.setTop') }}
            </el-button>
            <el-button v-if="detail.canDelete" size="small" type="danger" plain @click="onDelete">
              {{ t('bbs.delete') }}
            </el-button>
          </div>
        </div>

        <article class="bbs-topic">
          <div class="bbs-topic__head">
            <div class="bbs-row__type" :title="detailTypeLabel">
              <span class="bbs-type-mark">{{ detailTypeShort }}</span>
            </div>
            <div class="bbs-topic__head-main">
              <div class="bbs-topic__title-row">
                <span v-if="detail.isTop" class="bbs-badge bbs-badge--top">{{ t('bbs.badgeTop') }}</span>
                <span v-if="detail.isHot" class="bbs-badge bbs-badge--hot">{{ t('bbs.badgeHot') }}</span>
                <span v-if="Number(detail.kind) === 1" class="bbs-badge bbs-badge--poll">{{ t('bbs.badgePoll') }}</span>
                <h1 class="bbs-topic__title">{{ detail.title }}</h1>
              </div>
              <div class="bbs-row__meta">
                <span>{{ detail.authorDisplay }}</span>
                <span>·</span>
                <span>{{ formatTime(detail.createTime) }}</span>
                <span>·</span>
                <span>{{ detailTypeLabel }}</span>
                <span>·</span>
                <span :class="{ 'is-closed': detail.status === BbsSubjectStatus.Close }">
                  {{ statusLabel(detail.status) }}
                </span>
                <span>·</span>
                <span>{{ t('bbs.metaViews', { n: detail.viewCount }) }}</span>
                <span>·</span>
                <span>{{ t('bbs.metaReplies', { n: detail.replyCount }) }}</span>
                <template v-if="Number(detail.kind) === 1">
                  <span>·</span>
                  <span>{{ t('bbs.metaVotes', { n: detail.voteCount || 0 }) }}</span>
                </template>
              </div>
            </div>
          </div>
          <div
            ref="topicBodyRef"
            class="bbs-topic__body markdown-body"
            v-html="bodyHtml"
          />

          <section v-if="detail.poll" class="bbs-poll">
            <div class="bbs-poll__head">
              <strong>{{ t('bbs.poll.sectionTitle') }}</strong>
              <span class="bbs-poll__meta">
                {{
                  detail.poll.voteMode === BbsVoteMode.Multi
                    ? t('bbs.poll.multi')
                    : t('bbs.poll.single')
                }}
                <template v-if="detail.poll.voteDeadline">
                  · {{ t('bbs.poll.deadlineLabel', { t: formatTime(detail.poll.voteDeadline) }) }}
                </template>
                <template v-if="detail.poll.isClosedForVote"> · {{ t('bbs.poll.closed') }}</template>
              </span>
            </div>
            <div class="bbs-poll__options">
              <label
                v-for="opt in detail.poll.options"
                :key="opt.id"
                class="bbs-poll__option"
                :class="{ 'is-selected': opt.selected, 'is-disabled': !detail.poll.canVote }"
              >
                <input
                  v-if="detail.poll.canVote"
                  :type="detail.poll.voteMode === BbsVoteMode.Multi ? 'checkbox' : 'radio'"
                  :name="'poll-' + detail.id"
                  :value="opt.id"
                  :checked="selectedPollIds.includes(opt.id)"
                  @change="onPollOptionToggle(opt.id, ($event.target as HTMLInputElement).checked)"
                />
                <span class="bbs-poll__option-text">{{ opt.text }}</span>
                <template v-if="detail.poll.canSeeStats">
                  <span class="bbs-poll__stat">{{ opt.voteCount ?? 0 }}（{{ opt.percent ?? 0 }}%）</span>
                  <div class="bbs-poll__bar">
                    <div class="bbs-poll__bar-fill" :style="{ width: `${opt.percent ?? 0}%` }" />
                  </div>
                </template>
              </label>
            </div>
            <p v-if="!detail.poll.canSeeStats" class="bbs-poll__hint">{{ t('bbs.poll.statsHidden') }}</p>
            <div v-if="detail.poll.canVote" class="bbs-poll__actions">
              <el-button type="primary" :loading="voting" :disabled="!selectedPollIds.length" @click="submitPollVote">
                {{ t('bbs.poll.submit') }}
              </el-button>
            </div>
            <p v-else-if="detail.poll.hasVoted" class="bbs-poll__hint">{{ t('bbs.poll.voted') }}</p>
          </section>

          <div class="bbs-reactions">
            <button
              type="button"
              class="bbs-react"
              :class="{ 'is-active': detail.myReaction === BbsReactionValue.Like }"
              :title="t('bbs.react.like')"
              @click="reactSubject(BbsReactionValue.Like)"
            >
              <svg viewBox="0 0 24 24" width="14" height="14" aria-hidden="true">
                <path
                  fill="currentColor"
                  d="M2 21h4V9H2v12zm20.1-10.9c-.4-.5-1-.8-1.6-.8h-5.5l.8-4c.1-.5 0-1-.3-1.4L14.9 2 8.4 8.5c-.4.4-.6.9-.6 1.5V19c0 1.1.9 2 2 2h8c.8 0 1.5-.5 1.8-1.2l3-7c.2-.5.1-1.1-.3-1.5z"
                />
              </svg>
              <span>{{ detail.likeCount || 0 }}</span>
            </button>
            <button
              type="button"
              class="bbs-react"
              :class="{ 'is-active is-down': detail.myReaction === BbsReactionValue.Dislike }"
              :title="t('bbs.react.dislike')"
              @click="reactSubject(BbsReactionValue.Dislike)"
            >
              <svg viewBox="0 0 24 24" width="14" height="14" aria-hidden="true">
                <path
                  fill="currentColor"
                  d="M22 3h-4v12h4V3zM2.9 13.9c.4.5 1 .8 1.6.8h5.5l-.8 4c-.1.5 0 1 .3 1.4l.6 1.2 6.5-6.5c.4-.4.6-.9.6-1.5V5c0-1.1-.9-2-2-2H6c-.8 0-1.5.5-1.8 1.2l-3 7c-.2.5-.1 1.1.3 1.5z"
                />
              </svg>
              <span>{{ detail.dislikeCount || 0 }}</span>
            </button>
          </div>
        </article>

        <section class="bbs-replies">
          <div class="bbs-feed__head">{{ t('bbs.repliesTitle', { n: replyTotal }) }}</div>
          <div v-if="!replies.length" class="bbs-empty">{{ t('bbs.noReplies') }}</div>
          <div v-for="r in replies" :key="r.id" class="bbs-reply">
            <div class="bbs-reply__avatar" aria-hidden="true">{{ authorInitial(r.authorDisplay) }}</div>
            <div class="bbs-reply__main">
              <div class="bbs-reply__head">
                <strong>{{ r.authorDisplay }}</strong>
                <span>{{ formatTime(r.createTime) }}</span>
                <el-button
                  v-if="r.canDelete"
                  link
                  type="danger"
                  size="small"
                  @click="onDeleteReply(r.id)"
                >{{ t('bbs.delete') }}</el-button>
              </div>
              <div class="bbs-reply__body">{{ r.content }}</div>
            </div>
          </div>

          <div class="bbs-composer" v-if="detail.status === BbsSubjectStatus.Open">
            <div class="bbs-composer__title">{{ t('bbs.replySubmit') }}</div>
            <el-input
              v-model="replyContent"
              type="textarea"
              :rows="4"
              :placeholder="t('bbs.replyPh')"
            />
            <div class="bbs-composer__actions">
              <el-checkbox v-model="replyAnonymous">{{ t('bbs.form.anonymous') }}</el-checkbox>
              <el-button type="primary" :loading="replying" @click="submitReply">
                {{ t('bbs.replySubmit') }}
              </el-button>
            </div>
          </div>
          <el-alert
            v-else
            class="bbs-closed-alert"
            type="info"
            :closable="false"
            :title="t('bbs.closedHint')"
            show-icon
          />
        </section>
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  bbsApi,
  BbsReactionValue,
  BbsSubjectStatus,
  BbsSubjectType,
  BbsSubjectTypeI18nKey,
  BbsVoteMode,
  type BbsReplyItem,
  type BbsSubjectDetail
} from '@/api/bbs'
import BbsCategoryAside, { type BbsCategoryKey } from '@/components/Bbs/BbsCategoryAside.vue'
import {
  resolveAnnouncementDocumentImages,
  revokeObjectUrls,
  renderBbsContent
} from '@/utils/sanitizeAnnouncementHtml'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { getApiErrorMessage } from '@/utils/apiError'

type CategoryKey = BbsCategoryKey

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const id = computed(() => String(route.params.id || ''))
const loading = ref(false)
const detail = ref<BbsSubjectDetail | null>(null)
const replies = ref<BbsReplyItem[]>([])
const replyTotal = ref(0)
const replyContent = ref('')
const replyAnonymous = ref(false)
const replying = ref(false)
const voting = ref(false)
const selectedPollIds = ref<string[]>([])
const topicBodyRef = ref<HTMLElement | null>(null)
const mediaObjectUrls = ref<string[]>([])

const TYPE_SHORT: Record<number, string> = {
  [BbsSubjectType.CompanyNotice]: '通',
  [BbsSubjectType.IndustryNews]: '讯',
  [BbsSubjectType.Share]: '享',
  [BbsSubjectType.OpsGuide]: '说',
  [BbsSubjectType.Suggestion]: '建',
  [BbsSubjectType.SystemUpdate]: '更'
}

const bodyHtml = computed(() => renderBbsContent(detail.value?.content || ''))

const sidebarKey = computed<CategoryKey>(() => {
  if (!detail.value) return 'all'
  if (detail.value.isTop) return 'top'
  return detail.value.type
})

async function resolveMedia() {
  revokeObjectUrls(mediaObjectUrls.value)
  mediaObjectUrls.value = []
  await nextTick()
  mediaObjectUrls.value = await resolveAnnouncementDocumentImages(topicBodyRef.value)
}

function typeLabel(type: number) {
  const key = BbsSubjectTypeI18nKey[type]
  return key ? t(key) : String(type)
}

const detailTypeLabel = computed(() => {
  if (!detail.value) return ''
  return (detail.value.typeLabel || '').trim() || typeLabel(detail.value.type)
})

const detailTypeShort = computed(() => {
  if (!detail.value) return '板'
  if (TYPE_SHORT[detail.value.type]) return TYPE_SHORT[detail.value.type]
  const label = detailTypeLabel.value
  return (label && label[0]) || '板'
})

function statusLabel(status: number) {
  return status === BbsSubjectStatus.Close ? t('bbs.status.close') : t('bbs.status.open')
}

function formatTime(v: string | null | undefined) {
  return v ? formatDisplayDateTime(v) : '—'
}

function authorInitial(name: string) {
  const s = (name || '').trim()
  return s ? s.slice(0, 1) : '?'
}

function goList() {
  router.push({ name: 'BbsList' })
}

function goCategory(key: CategoryKey) {
  if (key === 'all') {
    router.push({ name: 'BbsList' })
    return
  }
  if (key === 'top') {
    router.push({ name: 'BbsList', query: { cat: 'top' } })
    return
  }
  router.push({ name: 'BbsList', query: { type: String(key) } })
}

function goEdit() {
  router.push({ name: 'BbsEdit', params: { id: id.value } })
}

async function load() {
  if (!id.value) return
  loading.value = true
  try {
    detail.value = await bbsApi.detail(id.value)
    selectedPollIds.value = [...(detail.value?.poll?.myOptionIds || [])]
    const page = await bbsApi.replies(id.value, 1, 100)
    replies.value = page?.items || []
    replyTotal.value = page?.total || 0
    await resolveMedia()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.loadFailed')))
  } finally {
    loading.value = false
  }
}

function onPollOptionToggle(optionId: string, checked: boolean) {
  const poll = detail.value?.poll
  if (!poll?.canVote) return
  if (poll.voteMode === BbsVoteMode.Single) {
    selectedPollIds.value = checked ? [optionId] : []
    return
  }
  const set = new Set(selectedPollIds.value)
  if (checked) {
    const max = poll.voteMaxChoices && poll.voteMaxChoices > 0 ? poll.voteMaxChoices : poll.options.length
    if (set.size >= max && !set.has(optionId)) {
      ElMessage.warning(t('bbs.poll.maxChoicesInvalid'))
      return
    }
    set.add(optionId)
  } else {
    set.delete(optionId)
  }
  selectedPollIds.value = [...set]
}

async function submitPollVote() {
  if (!detail.value?.poll?.canVote || !selectedPollIds.value.length) return
  voting.value = true
  try {
    detail.value = await bbsApi.votePoll(id.value, selectedPollIds.value)
    selectedPollIds.value = [...(detail.value.poll?.myOptionIds || [])]
    ElMessage.success(t('bbs.poll.voteOk'))
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.poll.voteFailed')))
  } finally {
    voting.value = false
  }
}

async function toggleClose() {
  if (!detail.value) return
  try {
    if (detail.value.status === BbsSubjectStatus.Close) await bbsApi.open(id.value)
    else await bbsApi.close(id.value)
    await load()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.actionFailed')))
  }
}

async function toggleTop() {
  if (!detail.value) return
  try {
    if (detail.value.isTop) await bbsApi.untop(id.value)
    else await bbsApi.setTop(id.value)
    await load()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.actionFailed')))
  }
}

async function onDelete() {
  try {
    await ElMessageBox.confirm(t('bbs.deleteConfirm'), t('bbs.delete'), { type: 'warning' })
    await bbsApi.deleteSubject(id.value)
    ElMessage.success(t('bbs.deleted'))
    goList()
  } catch (e) {
    if (e === 'cancel') return
    ElMessage.error(getApiErrorMessage(e, t('bbs.actionFailed')))
  }
}

async function submitReply() {
  if (!replyContent.value.trim()) {
    ElMessage.warning(t('bbs.replyRequired'))
    return
  }
  replying.value = true
  try {
    await bbsApi.addReply(id.value, {
      content: replyContent.value.trim(),
      anonymous: replyAnonymous.value
    })
    replyContent.value = ''
    replyAnonymous.value = false
    ElMessage.success(t('bbs.replyOk'))
    await load()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.actionFailed')))
  } finally {
    replying.value = false
  }
}

async function onDeleteReply(replyId: string) {
  try {
    await ElMessageBox.confirm(t('bbs.deleteReplyConfirm'), t('bbs.delete'), { type: 'warning' })
    await bbsApi.deleteReply(replyId)
    await load()
  } catch (e) {
    if (e === 'cancel') return
    ElMessage.error(getApiErrorMessage(e, t('bbs.actionFailed')))
  }
}

async function reactSubject(value: number) {
  if (!detail.value) return
  try {
    const r = await bbsApi.reactSubject(id.value, value)
    detail.value.likeCount = r.likeCount
    detail.value.dislikeCount = r.dislikeCount
    detail.value.myReaction = r.myReaction
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.actionFailed')))
  }
}

watch(id, () => {
  void load()
})

onMounted(() => {
  void load()
})

onUnmounted(() => {
  revokeObjectUrls(mediaObjectUrls.value)
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.bbs-detail-page {
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
  display: flex;
  flex-direction: column;
  gap: 12px;
  background: transparent;
  border: none;
  border-radius: 0;
  overflow: visible;
}

.bbs-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding: 12px 14px;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
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

.bbs-toolbar__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.bbs-topic {
  padding: 18px 20px 22px;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
}

.bbs-topic__head {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  margin-bottom: 14px;
}

.bbs-row__type {
  flex: 0 0 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.06);
}

.bbs-type-mark {
  font-size: 13px;
  font-weight: 700;
  color: $cyan-primary;
}

.bbs-topic__head-main {
  flex: 1 1 auto;
  min-width: 0;
}

.bbs-topic__title-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px;
  margin-bottom: 6px;
}

.bbs-topic__title {
  margin: 0;
  font-size: 20px;
  font-weight: 700;
  line-height: 1.35;
  color: $text-primary;
  word-break: break-word;
}

.bbs-badge {
  flex-shrink: 0;
  padding: 1px 6px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 600;
  line-height: 1.4;

  &--top {
    color: #fbbf24;
    background: rgba(251, 191, 36, 0.12);
  }

  &--hot {
    color: #f87171;
    background: rgba(248, 113, 113, 0.12);
  }

  &--poll {
    color: #38bdf8;
    background: rgba(56, 189, 248, 0.14);
  }
}

.bbs-poll {
  margin: 16px 0 8px;
  padding: 14px 14px 12px;
  border: 1px solid $border-card;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.02);
}

.bbs-poll__head {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 12px;
  align-items: baseline;
  margin-bottom: 12px;
}

.bbs-poll__meta {
  font-size: 12px;
  color: $text-muted;
}

.bbs-poll__options {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.bbs-poll__option {
  display: grid;
  grid-template-columns: auto 1fr auto;
  grid-template-rows: auto auto;
  column-gap: 8px;
  row-gap: 4px;
  align-items: center;
  padding: 8px 10px;
  border-radius: 8px;
  border: 1px solid transparent;
  cursor: pointer;

  &.is-selected {
    border-color: rgba(56, 189, 248, 0.35);
    background: rgba(56, 189, 248, 0.08);
  }

  &.is-disabled {
    cursor: default;
  }
}

.bbs-poll__option-text {
  grid-column: 2;
  color: $text-primary;
  font-size: 13px;
}

.bbs-poll__stat {
  grid-column: 3;
  font-size: 12px;
  color: $text-muted;
  white-space: nowrap;
}

.bbs-poll__bar {
  grid-column: 2 / 4;
  height: 6px;
  border-radius: 999px;
  background: rgba(148, 163, 184, 0.2);
  overflow: hidden;
}

.bbs-poll__bar-fill {
  height: 100%;
  background: rgba(56, 189, 248, 0.75);
}

.bbs-poll__hint {
  margin: 10px 0 0;
  font-size: 12px;
  color: $text-muted;
}

.bbs-poll__actions {
  margin-top: 12px;
}

.bbs-row__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;

  .is-closed {
    color: $text-secondary;
  }
}

.bbs-topic__body {
  padding-left: 48px;
  font-size: 14px;
  line-height: 1.7;
  color: $text-secondary;

  :deep(img),
  :deep(video) {
    max-width: 100%;
    height: auto;
    border-radius: 8px;
    margin: 8px 0;
    display: block;
  }

  :deep(video) {
    width: min(100%, 720px);
    background: #000;
  }
}

.bbs-reactions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 16px;
  padding-left: 48px;
}

.bbs-react {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 10px;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.03);
  color: $text-secondary;
  font-size: 12px;
  cursor: pointer;

  &:hover {
    color: $text-primary;
    border-color: rgba(0, 212, 255, 0.35);
  }

  &.is-active {
    color: #fbbf24;
    border-color: rgba(251, 191, 36, 0.55);
    background: rgba(251, 191, 36, 0.14);
  }

  &.is-down.is-active {
    color: #f87171;
    border-color: rgba(248, 113, 113, 0.45);
    background: rgba(248, 113, 113, 0.1);
  }
}

.bbs-replies {
  padding: 0 0 10px;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.bbs-feed__head {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 0;
  padding: 12px 16px;
  font-size: 14px;
  font-weight: 700;
  color: $text-primary;
  background: rgba(255, 255, 255, 0.03);
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);

  &::before {
    content: '';
    width: 3px;
    height: 14px;
    border-radius: 2px;
    background: $cyan-primary;
  }
}

.bbs-empty {
  padding: 24px 16px;
  text-align: center;
  color: $text-muted;
  font-size: 13px;
}

.bbs-reply {
  display: flex;
  gap: 12px;
  padding: 14px 16px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
}

.bbs-reply__avatar {
  flex: 0 0 32px;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  background: rgba(0, 212, 255, 0.12);
  color: $cyan-primary;
  font-size: 13px;
  font-weight: 700;
}

.bbs-reply__main {
  flex: 1 1 auto;
  min-width: 0;
}

.bbs-reply__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
  margin-bottom: 6px;
  font-size: 12px;
  color: $text-muted;

  strong {
    color: $text-primary;
    font-size: 13px;
  }
}

.bbs-reply__body {
  font-size: 13px;
  line-height: 1.65;
  color: $text-secondary;
  white-space: pre-wrap;
  word-break: break-word;
}

.bbs-composer {
  margin: 12px 16px 16px;
  padding: 14px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.02);
}

.bbs-composer__title {
  margin-bottom: 8px;
  font-size: 13px;
  font-weight: 600;
  color: $text-primary;
}

.bbs-composer__actions {
  margin-top: 10px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
}

.bbs-closed-alert {
  margin: 12px 16px 16px;
}

.markdown-body :deep(p) {
  margin: 0 0 0.6em;
}

.markdown-body :deep(pre) {
  overflow: auto;
  padding: 8px;
  background: rgba(255, 255, 255, 0.04);
  border-radius: 6px;
}

.markdown-body :deep(a) {
  color: $cyan-primary;
}

@media (max-width: 900px) {
  .bbs-layout {
    grid-template-columns: 1fr;
  }

  .bbs-topic__body {
    padding-left: 0;
  }
}
</style>
