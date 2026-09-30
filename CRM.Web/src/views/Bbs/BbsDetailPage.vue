<template>
  <div class="bbs-detail-page" v-loading="loading">
    <template v-if="detail">
      <div class="page-header">
        <div>
          <div class="tags">
            <el-tag v-if="detail.isTop" size="small" type="warning" effect="plain">{{ t('bbs.badgeTop') }}</el-tag>
            <el-tag v-if="detail.isHot" size="small" type="danger" effect="plain">{{ t('bbs.badgeHot') }}</el-tag>
            <el-tag size="small" effect="plain">{{ typeLabel(detail.type) }}</el-tag>
            <el-tag
              size="small"
              effect="plain"
              :type="detail.status === BbsSubjectStatus.Close ? 'info' : 'success'"
            >{{ statusLabel(detail.status) }}</el-tag>
          </div>
          <h2>{{ detail.title }}</h2>
          <div class="meta">
            <span>{{ detail.authorDisplay }}</span>
            <span>{{ formatTime(detail.createTime) }}</span>
            <span>{{ t('bbs.metaViews', { n: detail.viewCount }) }}</span>
            <span>{{ t('bbs.metaReplies', { n: detail.replyCount }) }}</span>
          </div>
        </div>
        <div class="actions">
          <el-button @click="goList">{{ t('bbs.backList') }}</el-button>
          <el-button v-if="detail.canEdit" @click="goEdit">{{ t('bbs.edit') }}</el-button>
          <el-button
            v-if="detail.canModerate || detail.canEdit"
            @click="toggleClose"
          >{{ detail.status === BbsSubjectStatus.Close ? t('bbs.open') : t('bbs.close') }}</el-button>
          <el-button v-if="detail.canSetTop" @click="toggleTop">
            {{ detail.isTop ? t('bbs.untop') : t('bbs.setTop') }}
          </el-button>
          <el-button v-if="detail.canDelete" type="danger" plain @click="onDelete">{{ t('bbs.delete') }}</el-button>
        </div>
      </div>

      <el-card shadow="never" class="body-card">
        <div class="markdown-body" v-html="bodyHtml" />
      </el-card>

      <el-card shadow="never" class="reply-card">
        <div class="section-title">{{ t('bbs.repliesTitle', { n: replyTotal }) }}</div>
        <div v-if="!replies.length" class="empty">{{ t('bbs.noReplies') }}</div>
        <div v-for="r in replies" :key="r.id" class="reply-item">
          <div class="reply-head">
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
          <div class="markdown-body reply-body" v-html="renderMd(r.content)" />
        </div>

        <div class="reply-form" v-if="detail.status === BbsSubjectStatus.Open">
          <el-input
            v-model="replyContent"
            type="textarea"
            :rows="4"
            :placeholder="t('bbs.replyPh')"
          />
          <div class="reply-form-actions">
            <el-checkbox v-model="replyAnonymous">{{ t('bbs.form.anonymous') }}</el-checkbox>
            <el-button type="primary" :loading="replying" @click="submitReply">{{ t('bbs.replySubmit') }}</el-button>
          </div>
        </div>
        <el-alert
          v-else
          type="info"
          :closable="false"
          :title="t('bbs.closedHint')"
          show-icon
        />
      </el-card>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  bbsApi,
  BbsSubjectStatus,
  BbsSubjectTypeI18nKey,
  type BbsReplyItem,
  type BbsSubjectDetail
} from '@/api/bbs'
import { renderAnnouncementMarkdown } from '@/utils/sanitizeAnnouncementHtml'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { getApiErrorMessage } from '@/utils/apiError'

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

const bodyHtml = computed(() => renderAnnouncementMarkdown(detail.value?.content || ''))

function renderMd(md: string) {
  return renderAnnouncementMarkdown(md)
}

function typeLabel(type: number) {
  const key = BbsSubjectTypeI18nKey[type]
  return key ? t(key) : String(type)
}

function statusLabel(status: number) {
  return status === BbsSubjectStatus.Close ? t('bbs.status.close') : t('bbs.status.open')
}

function formatTime(v: string | null | undefined) {
  return v ? formatDisplayDateTime(v) : '—'
}

async function load() {
  if (!id.value) return
  loading.value = true
  try {
    detail.value = await bbsApi.detail(id.value)
    const page = await bbsApi.replies(id.value, 1, 100)
    replies.value = page?.items || []
    replyTotal.value = page?.total || 0
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.loadFailed')))
  } finally {
    loading.value = false
  }
}

function goList() {
  router.push({ name: 'BbsList' })
}

function goEdit() {
  router.push({ name: 'BbsEdit', params: { id: id.value } })
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

watch(id, () => {
  void load()
})

onMounted(() => {
  void load()
})
</script>

<style scoped>
.bbs-detail-page {
  padding: 16px 20px 40px;
  max-width: 960px;
}
.page-header {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 12px;
}
.page-header h2 {
  margin: 8px 0;
  font-size: 20px;
}
.tags {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}
.meta,
.reply-head {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: flex-start;
}
.body-card,
.reply-card {
  margin-bottom: 12px;
}
.section-title {
  font-weight: 600;
  margin-bottom: 12px;
}
.reply-item {
  padding: 12px 0;
  border-bottom: 1px solid var(--el-border-color-lighter);
}
.reply-body {
  margin-top: 6px;
}
.reply-form {
  margin-top: 16px;
}
.reply-form-actions {
  margin-top: 8px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.empty {
  color: var(--el-text-color-secondary);
  padding: 12px 0;
}
.markdown-body :deep(p) {
  margin: 0 0 0.6em;
}
.markdown-body :deep(pre) {
  overflow: auto;
  padding: 8px;
  background: var(--el-fill-color-light);
  border-radius: 4px;
}
</style>
