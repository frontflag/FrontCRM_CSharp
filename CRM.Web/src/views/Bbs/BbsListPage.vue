<template>
  <div class="bbs-list-page">
    <div class="page-header">
      <h2>{{ t('bbs.title') }}</h2>
      <el-button type="primary" @click="goCreate">{{ t('bbs.create') }}</el-button>
    </div>

    <el-card v-if="topItems.length" shadow="never" class="top-card">
      <div class="section-title">{{ t('bbs.topSection') }}</div>
      <div
        v-for="row in topItems"
        :key="row.id"
        class="subject-row top"
        @click="goDetail(row.id)"
      >
        <div class="subject-title">
          <el-tag size="small" type="warning" effect="plain">{{ t('bbs.badgeTop') }}</el-tag>
          <el-tag v-if="row.isHot" size="small" type="danger" effect="plain">{{ t('bbs.badgeHot') }}</el-tag>
          <el-tag size="small" effect="plain">{{ typeLabel(row.type) }}</el-tag>
          <span class="title-text">{{ row.title }}</span>
        </div>
        <div class="subject-meta">
          <span>{{ row.authorDisplay }}</span>
          <span>{{ t('bbs.metaViews', { n: row.viewCount }) }}</span>
          <span>{{ t('bbs.metaReplies', { n: row.replyCount }) }}</span>
          <span>{{ formatTime(row.createTime) }}</span>
        </div>
      </div>
    </el-card>

    <el-card shadow="never" class="filter-card">
      <el-form :inline="true" class="filter-form" @submit.prevent="search">
        <el-form-item :label="t('bbs.filters.keyword')">
          <el-input
            v-model="keyword"
            clearable
            style="width: 220px"
            :placeholder="t('bbs.filters.keywordPh')"
            @keyup.enter="search"
          />
        </el-form-item>
        <el-form-item :label="t('bbs.filters.type')">
          <el-select v-model="typeFilter" clearable style="width: 140px" :placeholder="t('bbs.filters.all')">
            <el-option
              v-for="tp in BbsSubjectTypeOptions"
              :key="tp"
              :label="t(BbsSubjectTypeI18nKey[tp])"
              :value="tp"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('bbs.filters.status')">
          <el-select v-model="statusFilter" clearable style="width: 120px" :placeholder="t('bbs.filters.all')">
            <el-option :label="t('bbs.status.open')" :value="BbsSubjectStatus.Open" />
            <el-option :label="t('bbs.status.close')" :value="BbsSubjectStatus.Close" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="loading" @click="search">{{ t('bbs.filters.search') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card" v-loading="loading">
      <div v-if="!items.length && !loading" class="empty">{{ t('bbs.empty') }}</div>
      <div
        v-for="row in items"
        :key="row.id"
        class="subject-row"
        @click="goDetail(row.id)"
      >
        <div class="subject-title">
          <el-tag v-if="row.isHot" size="small" type="danger" effect="plain">{{ t('bbs.badgeHot') }}</el-tag>
          <el-tag
            size="small"
            effect="plain"
            :type="row.status === BbsSubjectStatus.Close ? 'info' : 'success'"
          >{{ statusLabel(row.status) }}</el-tag>
          <el-tag size="small" effect="plain">{{ typeLabel(row.type) }}</el-tag>
          <span class="title-text">{{ row.title }}</span>
        </div>
        <div class="subject-meta">
          <span>{{ row.authorDisplay }}</span>
          <span>{{ t('bbs.metaViews', { n: row.viewCount }) }}</span>
          <span>{{ t('bbs.metaReplies', { n: row.replyCount }) }}</span>
          <span>{{ formatTime(row.lastReplyTime || row.createTime) }}</span>
        </div>
      </div>

      <div class="pager" v-if="total > pageSize">
        <el-pagination
          background
          layout="prev, pager, next, total"
          :total="total"
          :page-size="pageSize"
          :current-page="page"
          @current-change="onPage"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import {
  bbsApi,
  BbsSubjectStatus,
  BbsSubjectTypeI18nKey,
  BbsSubjectTypeOptions,
  type BbsSubjectListItem
} from '@/api/bbs'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()
const router = useRouter()

const topItems = ref<BbsSubjectListItem[]>([])
const items = ref<BbsSubjectListItem[]>([])
const loading = ref(false)
const keyword = ref('')
const typeFilter = ref<number | undefined>()
const statusFilter = ref<number | undefined>()
const page = ref(1)
const pageSize = 20
const total = ref(0)

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

function goCreate() {
  router.push({ name: 'BbsCreate' })
}

function goDetail(id: string) {
  router.push({ name: 'BbsDetail', params: { id } })
}

async function loadTop() {
  try {
    topItems.value = (await bbsApi.getTop()) || []
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.loadFailed')))
  }
}

async function loadList() {
  loading.value = true
  try {
    const data = await bbsApi.list({
      keyword: keyword.value.trim() || undefined,
      type: typeFilter.value ?? null,
      status: statusFilter.value ?? null,
      page: page.value,
      pageSize
    })
    items.value = data?.items || []
    total.value = data?.total || 0
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.loadFailed')))
  } finally {
    loading.value = false
  }
}

function search() {
  page.value = 1
  void loadList()
}

function onPage(p: number) {
  page.value = p
  void loadList()
}

onMounted(() => {
  void loadTop()
  void loadList()
})
</script>

<style scoped>
.bbs-list-page {
  padding: 16px 20px 32px;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}
.page-header h2 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
}
.top-card,
.filter-card,
.table-card {
  margin-bottom: 12px;
}
.section-title {
  font-weight: 600;
  margin-bottom: 8px;
}
.subject-row {
  padding: 10px 4px;
  border-bottom: 1px solid var(--el-border-color-lighter);
  cursor: pointer;
}
.subject-row:hover {
  background: var(--el-fill-color-lighter);
}
.subject-row.top {
  background: var(--el-color-warning-light-9);
}
.subject-title {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  align-items: center;
  margin-bottom: 4px;
}
.title-text {
  font-weight: 500;
}
.subject-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
.empty {
  padding: 32px;
  text-align: center;
  color: var(--el-text-color-secondary);
}
.pager {
  display: flex;
  justify-content: flex-end;
  margin-top: 12px;
}
</style>
