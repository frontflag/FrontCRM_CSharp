<template>
  <div class="recycle-bin-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-icon">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <polyline points="3 6 5 6 21 6" />
            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
          </svg>
        </div>
        <h1 class="page-title">供应商回收站</h1>
        <div class="count-badge">共 {{ totalCount }} 条已删除记录</div>
      </div>
      <div class="header-right">
        <el-input
          v-model="keyword"
          placeholder="搜索供应商名称/编号"
          clearable
          style="width:220px"
          @keyup.enter="fetchData"
          @clear="fetchData"
        >
          <template #prefix>
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8" /><line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
          </template>
        </el-input>
      </div>
    </div>

    <div v-loading="loading" element-loading-background="rgba(10,22,40,0.8)" class="list-container">
      <div v-if="!loading && items.length === 0" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
          <polyline points="3 6 5 6 21 6" />
          <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
        </svg>
        <p>回收站为空</p>
      </div>

      <div v-for="item in items" :key="item.id" class="record-card">
        <div class="record-avatar">{{ (item.officialName || '?')[0] }}</div>
        <div class="record-body">
          <div class="record-name">{{ item.officialName }}</div>
          <div class="record-meta">
            <span class="meta-code">{{ item.code }}</span>
            <span class="meta-sep">·</span>
            <span class="meta-text">删除时间：{{ formatDateTime(item.deleteTime || item.modifyTime) }}</span>
          </div>
          <div v-if="item.deleteReason || item.companyInfo" class="record-reason">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10" /><line x1="12" y1="8" x2="12" y2="12" /><line x1="12" y1="16" x2="12.01" y2="16" />
            </svg>
            删除理由：{{ item.deleteReason || item.companyInfo }}
          </div>
        </div>
        <div class="record-actions">
          <el-button type="primary" size="small" :loading="restoringId === item.id" @click="handleRestore(item)">
            恢复供应商
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
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { ElNotification, ElMessageBox } from 'element-plus';
import { vendorApi } from '@/api/vendor';
import type { Vendor } from '@/types/vendor';

const loading = ref(false);
const items = ref<Vendor[]>([]);
const totalCount = ref(0);
const pageIndex = ref(1);
const pageSize = 20;
const keyword = ref('');
const restoringId = ref<string | null>(null);

const fetchData = async () => {
  loading.value = true;
  try {
    const res = await vendorApi.getRecycleBin({ pageIndex: pageIndex.value, pageSize, keyword: keyword.value });
    items.value = res?.items ?? (Array.isArray(res) ? res : []);
    totalCount.value = res?.totalCount ?? items.value.length;
  } catch {
    ElNotification.error({ title: '加载失败', message: '获取供应商回收站数据失败，请刷新重试' });
  } finally {
    loading.value = false;
  }
};

const handleRestore = async (item: Vendor) => {
  try {
    await ElMessageBox.confirm(`确定要恢复供应商「${item.officialName || item.code}」吗？`, '确认恢复', { type: 'info' });
    restoringId.value = item.id;
    await vendorApi.restoreVendor(item.id);
    ElNotification.success({ title: '恢复成功', message: '供应商已恢复到供应商列表' });
    fetchData();
  } catch (e) {
    if (e !== 'cancel') ElNotification.error({ title: '恢复失败', message: '供应商恢复失败，请稍后重试' });
  } finally {
    restoringId.value = null;
  }
};

const formatDateTime = (date?: string) => (date ? new Date(date).toLocaleString('zh-CN') : '--');

onMounted(fetchData);
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.recycle-bin-page {
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
  background: rgba(201, 135, 69, 0.12);
  border: 1px solid rgba(201, 135, 69, 0.25);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #f0a84b;
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
  background: rgba(107, 122, 141, 0.15);
  border: 1px solid rgba(107, 122, 141, 0.25);
  border-radius: 12px;
  color: $text-muted;
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
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.record-avatar {
  width: 44px;
  height: 44px;
  background: linear-gradient(135deg, rgba(201, 135, 69, 0.2), rgba(201, 154, 69, 0.15));
  border: 1px solid rgba(201, 135, 69, 0.2);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  font-weight: 700;
  color: #f0a84b;
  flex-shrink: 0;
}

.record-body {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.record-name {
  font-size: 15px;
  font-weight: 500;
  color: $text-primary;
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

.record-reason {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: $text-secondary;

  svg {
    flex-shrink: 0;
  }
}

.empty-state {
  margin-top: 40px;
  text-align: center;
  color: $text-muted;

  svg {
    margin-bottom: 8px;
  }
}

.record-actions {
  display: flex;
  align-items: center;
}

.pagination-wrapper {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>

