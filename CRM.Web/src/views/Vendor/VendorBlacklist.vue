<template>
  <div class="blacklist-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-icon">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <circle cx="12" cy="12" r="10" /><line x1="4.93" y1="4.93" x2="19.07" y2="19.07" />
          </svg>
        </div>
        <h1 class="page-title">供应商黑名单</h1>
        <div class="count-badge">共 {{ totalCount }} 个黑名单供应商</div>
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
          <circle cx="12" cy="12" r="10" /><line x1="4.93" y1="4.93" x2="19.07" y2="19.07" />
        </svg>
        <p>当前没有黑名单供应商</p>
      </div>

      <div v-for="item in items" :key="item.id" class="record-card">
        <div class="record-avatar">{{ (item.officialName || '?')[0] }}</div>
        <div class="record-body">
          <div class="record-name-row">
            <span class="record-name">{{ item.officialName }}</span>
            <span class="blacklist-tag">黑名单</span>
          </div>
          <div class="record-meta">
            <span class="meta-code">{{ item.code }}</span>
            <span class="meta-sep">·</span>
            <span class="meta-text">行业：{{ item.industry || '--' }}</span>
          </div>
        </div>
        <div class="record-actions">
          <el-button size="small" style="margin-right:8px" @click="goDetail(item)">查看详情</el-button>
          <el-button type="warning" size="small" :loading="removingId === item.id" @click="handleRemove(item)">
            移出黑名单
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
import { useRouter } from 'vue-router';
import { ElNotification, ElMessageBox } from 'element-plus';
import { vendorApi } from '@/api/vendor';
import type { Vendor } from '@/types/vendor';

const router = useRouter();
const loading = ref(false);
const items = ref<Vendor[]>([]);
const totalCount = ref(0);
const pageIndex = ref(1);
const pageSize = 20;
const keyword = ref('');
const removingId = ref<string | null>(null);

const fetchData = async () => {
  loading.value = true;
  try {
    const res = await vendorApi.getBlacklist({ pageNumber: pageIndex.value, pageSize, keyword: keyword.value });
    items.value = res?.items ?? (Array.isArray(res) ? res : []);
    totalCount.value = res?.totalCount ?? items.value.length;
  } catch {
    ElNotification.error({ title: '加载失败', message: '获取供应商黑名单失败，请刷新重试' });
  } finally {
    loading.value = false;
  }
};

const handleRemove = async (item: Vendor) => {
  try {
    await ElMessageBox.confirm(`确定要将「${item.officialName || item.code}」移出黑名单吗？`, '移出黑名单', { type: 'warning' });
    removingId.value = item.id;
    await vendorApi.removeFromBlacklist(item.id);
    ElNotification.success({ title: '操作成功', message: '供应商已移出黑名单' });
    fetchData();
  } catch (e) {
    if (e !== 'cancel') ElNotification.error({ title: '操作失败', message: '移出黑名单失败，请稍后重试' });
  } finally {
    removingId.value = null;
  }
};

const goDetail = (item: Vendor) => router.push(`/vendors/${item.id}`);

onMounted(fetchData);
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.blacklist-page {
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
  background: rgba(201, 87, 69, 0.12);
  border: 1px solid rgba(201, 87, 69, 0.25);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: $color-red-brown;
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
  background: rgba(201, 87, 69, 0.1);
  border: 1px solid rgba(201, 87, 69, 0.2);
  border-radius: 12px;
  color: $color-red-brown;
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
    border-color: rgba(201, 87, 69, 0.3);
  }
}

.record-avatar {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  background: rgba(201, 87, 69, 0.2);
  color: $color-red-brown;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  font-weight: 600;
  flex-shrink: 0;
}

.record-body {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.record-name-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.record-name {
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
}

.blacklist-tag {
  font-size: 12px;
  padding: 2px 8px;
  border-radius: 999px;
  background: rgba(201, 87, 69, 0.16);
  color: $color-red-brown;
}

.record-meta {
  font-size: 12px;
  color: $text-muted;

  .meta-code {
    font-family: 'Space Mono', monospace;
  }
  .meta-sep {
    margin: 0 4px;
  }
}

.record-actions {
  display: flex;
  align-items: center;
}

.empty-state {
  margin-top: 40px;
  text-align: center;
  color: $text-muted;
  svg {
    margin-bottom: 8px;
  }
}

.pagination-wrapper {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>

