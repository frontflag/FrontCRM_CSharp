<template>
  <div class="freeze-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-icon">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"/>
          </svg>
        </div>
        <h1 class="page-title">{{ t('customerFreeze.title') }}</h1>
        <div class="count-badge">{{ t('customerFreeze.count', { count: totalCount }) }}</div>
      </div>
      <div class="header-right">
        <el-input
          v-model="keyword"
          :placeholder="t('customerFreeze.searchPlaceholder')"
          clearable
          size="default"
          style="width:220px"
          @keyup.enter="fetchData"
          @clear="fetchData"
        >
          <template #prefix>
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
            </svg>
          </template>
        </el-input>
      </div>
    </div>

    <div v-loading="loading" element-loading-background="rgba(10,22,40,0.8)" class="list-container">
      <div v-if="!loading && items.length === 0" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
          <path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4"/>
        </svg>
        <p>{{ t('customerFreeze.empty') }}</p>
      </div>

      <div v-for="item in items" :key="item.id" class="record-card" @dblclick="goDetail(item)">
        <div class="record-avatar">{{ (item.customerName || item.officialName || '?')[0] }}</div>
        <div class="record-body">
          <div class="record-name-row">
            <span class="record-name record-name--muted">{{ item.customerName || item.officialName }}</span>
            <PartyStatusIcons
              :entity-id="item.id"
              :frozen="true"
              :blacklist="!!item.blackList"
              size="sm"
            />
            <span class="freeze-tag">{{ t('customerFreeze.tag') }}</span>
            <span v-if="item.customerLevel" class="level-tag">{{ getLevelLabel(item.customerLevel) }}</span>
          </div>
          <div class="record-meta">
            <span class="meta-code">{{ item.customerCode }}</span>
            <span class="meta-sep">·</span>
            <span class="meta-text">{{ t('customerFreeze.updatedAt') }}{{ formatDateTime(item.modifyTime || item.updatedAt) }}</span>
          </div>
          <div class="record-hint">{{ t('customerFreeze.hint') }}</div>
        </div>
        <div class="record-actions" @dblclick.stop>
          <el-button type="primary" size="small" style="margin-right:8px" @click="goDetail(item)">{{ t('customerFreeze.viewDetail') }}</el-button>
          <el-button type="warning" size="small" :loading="removingId === item.id" @click="handleUnfreeze(item)">
            {{ t('customerFreeze.unfreeze') }}
          </el-button>
        </div>
      </div>
    </div>

    <div v-if="totalCount > pageSize" class="pagination-wrapper">
      <el-pagination
        v-model:current-page="pageIndex"
        :page-size="pageSize"
        :total="totalCount"
        layout="prev, pager, next"
        background
        @current-change="fetchData"
      />
    </div>

    <el-dialog v-model="showUnfreezeDialog" :title="t('customerFreeze.unfreezeTitle')" width="440px" :close-on-click-modal="false">
      <div style="color:rgba(200,216,232,0.75);font-size:13px;margin-bottom:16px">
        {{ t('customerFreeze.unfreezeConfirm', { name: pendingUnfreeze?.customerName || pendingUnfreeze?.officialName || '' }) }}
      </div>
      <el-form label-width="90px">
        <el-form-item :label="t('customerFreeze.reason')" required>
          <el-input
            v-model="unfreezeReason"
            type="textarea"
            :rows="3"
            :placeholder="t('customerFreeze.reasonPlaceholder')"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showUnfreezeDialog = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="removingId !== null" @click="confirmUnfreeze">{{ t('common.confirm') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElNotification } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { customerApi } from '@/api/customer'
import PartyStatusIcons from '@/components/party/PartyStatusIcons.vue'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()
const { t } = useI18n()
const loading = ref(false)
const items = ref<any[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const pageSize = 20
const keyword = ref('')
const removingId = ref<string | null>(null)
const showUnfreezeDialog = ref(false)
const pendingUnfreeze = ref<any | null>(null)
const unfreezeReason = ref('')

const fetchData = async () => {
  loading.value = true
  try {
    const res = await customerApi.getFrozen({ pageIndex: pageIndex.value, pageSize, keyword: keyword.value })
    items.value = res?.items ?? res?.data ?? (Array.isArray(res) ? res : [])
    totalCount.value = res?.totalCount ?? res?.total ?? items.value.length
  } catch {
    ElNotification.error({ title: t('customerFreeze.loadFailedTitle'), message: t('customerFreeze.loadFailedMessage') })
  } finally {
    loading.value = false
  }
}

const handleUnfreeze = (item: any) => {
  pendingUnfreeze.value = item
  unfreezeReason.value = ''
  showUnfreezeDialog.value = true
}

const confirmUnfreeze = async () => {
  const item = pendingUnfreeze.value
  if (!item?.id) return
  if (!unfreezeReason.value.trim()) {
    ElNotification.warning({ title: t('customerFreeze.reasonRequiredTitle'), message: t('customerFreeze.reasonRequiredMessage') })
    return
  }
  removingId.value = item.id
  try {
    await customerApi.unfreezeCustomer(item.id, unfreezeReason.value.trim())
    ElNotification.success({ title: t('customerFreeze.successTitle'), message: t('customerFreeze.successMessage') })
    showUnfreezeDialog.value = false
    pendingUnfreeze.value = null
    unfreezeReason.value = ''
    fetchData()
  } catch {
    ElNotification.error({ title: t('customerFreeze.failedTitle'), message: t('customerFreeze.failedMessage') })
  } finally {
    removingId.value = null
  }
}

const goDetail = (item: any) => router.push(`/customers/${item.id}`)

const getLevelLabel = (level: string | number) => {
  const map: Record<string, string> = { '1': 'D级', '2': 'C级', '3': 'B级', '4': 'BPO', '5': 'VIP', '6': 'VPO', D: 'D级', C: 'C级', B: 'B级', BPO: 'BPO', VIP: 'VIP', VPO: 'VPO' }
  return map[String(level)] || String(level)
}

const formatDateTime = (date: string | undefined) => (date ? formatDisplayDateTime(date) : '--')

onMounted(() => fetchData())
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.freeze-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  .header-right {
    display: flex;
    gap: 10px;
  }
}

.page-icon {
  width: 36px;
  height: 36px;
  background: rgba(201, 154, 69, 0.12);
  border: 1px solid rgba(201, 154, 69, 0.28);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: $color-amber;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0;
}

.count-badge {
  font-size: 12px;
  padding: 3px 10px;
  background: rgba(201, 154, 69, 0.1);
  border: 1px solid rgba(201, 154, 69, 0.22);
  border-radius: 12px;
  color: $color-amber;
}

.list-container {
  display: flex;
  flex-direction: column;
  gap: 10px;
  min-height: 200px;
}

.record-card {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px 20px;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  transition: border-color 0.2s;

  &:hover {
    border-color: rgba(201, 154, 69, 0.35);
  }
}

.record-avatar {
  width: 44px;
  height: 44px;
  background: linear-gradient(135deg, rgba(201, 154, 69, 0.2), rgba(50, 149, 201, 0.12));
  border: 1px solid rgba(201, 154, 69, 0.25);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  font-weight: 700;
  color: $color-amber;
  flex-shrink: 0;
}

.record-body {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.record-name-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.record-name {
  font-size: 15px;
  font-weight: 500;
  color: $text-primary;

  &--muted {
    color: rgba(150, 170, 195, 0.82);
  }
}

.freeze-tag {
  font-size: 10px;
  padding: 1px 6px;
  background: rgba(201, 154, 69, 0.15);
  color: $color-amber;
  border: 1px solid rgba(201, 154, 69, 0.35);
  border-radius: 3px;
}

.level-tag {
  font-size: 10px;
  padding: 1px 6px;
  background: rgba(50, 149, 201, 0.15);
  color: $color-steel-cyan;
  border: 1px solid rgba(50, 149, 201, 0.25);
  border-radius: 3px;
}

.record-meta {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}

.meta-code {
  font-family: 'Space Mono', monospace;
  font-size: 11px;
  color: $color-ice-blue;
}

.meta-sep {
  color: $text-muted;
  font-size: 11px;
}

.meta-text {
  font-size: 12px;
  color: $text-muted;
}

.record-hint {
  font-size: 11px;
  color: $text-muted;
  opacity: 0.85;
}

.record-actions {
  flex-shrink: 0;
  display: flex;
  align-items: center;
}

.empty-state {
  text-align: center;
  padding: 80px;
  color: $text-muted;

  svg {
    margin-bottom: 12px;
    opacity: 0.3;
    display: block;
    margin-left: auto;
    margin-right: auto;
  }
  p {
    font-size: 14px;
    margin: 0;
  }
}

.pagination-wrapper {
  display: flex;
  justify-content: center;
  margin-top: 24px;
}
</style>
