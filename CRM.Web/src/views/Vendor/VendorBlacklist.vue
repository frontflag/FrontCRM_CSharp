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
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="vendor-blacklist-main"
        :columns="blacklistColumns"
        :show-column-settings="false"
        :data="items"
        row-key="id"
        @row-dblclick="goDetail"
      >
        <template #col-officialName="{ row }">
          <div class="record-name-row">
            <span class="record-name record-name--muted">{{ row.officialName }}</span>
            <PartyStatusIcons
              :entity-id="row.id"
              :frozen="!!row.isDisenable"
              :blacklist="true"
              size="sm"
            />
            <span class="blacklist-tag">黑名单</span>
          </div>
        </template>
        <template #col-industry="{ row }">{{ row.industry || '--' }}</template>
        <template #col-actions="{ row }">
          <div class="record-actions" @dblclick.stop>
            <el-button size="small" style="margin-right:8px" @click="goDetail(row)">查看详情</el-button>
            <el-button type="warning" size="small" :loading="removingId === row.id" @click="handleRemove(row)">
              移出黑名单
            </el-button>
          </div>
        </template>
      </CrmDataTable>
    </div>

    <div v-if="totalCount > pageSize" class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip content="列设置" placement="top" :hide-after="0">
          <el-button class="list-settings-btn" link type="primary" aria-label="列设置" @click="dataTableRef?.openColumnSettings?.()">
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
      <el-pagination
        v-model:current-page="pageIndex"
        :page-size="pageSize"
        :total="totalCount"
        layout="prev, pager, next"
        background
        @current-change="fetchData"
      />
    </div>

    <el-dialog v-model="showRemoveDialog" title="移出黑名单" width="440px" :close-on-click-modal="false">
      <div style="color:rgba(200,216,232,0.75);font-size:13px;margin-bottom:16px">
        确定要将「{{ pendingRemove?.officialName || pendingRemove?.code || '' }}」移出黑名单吗？原因将记入操作日志。
      </div>
      <el-form label-width="90px">
        <el-form-item label="移出原因" required>
          <el-input
            v-model="removeReason"
            type="textarea"
            :rows="3"
            placeholder="请输入移出黑名单原因（必填）"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showRemoveDialog = false">取消</el-button>
        <el-button type="primary" :loading="removingId !== null" @click="confirmRemoveFromBlacklist">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElNotification } from 'element-plus';
import { Setting } from '@element-plus/icons-vue'
import { vendorApi } from '@/api/vendor';
import type { Vendor } from '@/types/vendor';
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter();
const loading = ref(false);
const items = ref<Vendor[]>([]);
const totalCount = ref(0);
const pageIndex = ref(1);
const pageSize = 20;
const keyword = ref('');
const removingId = ref<string | null>(null);
const showRemoveDialog = ref(false);
const pendingRemove = ref<Vendor | null>(null);
const removeReason = ref('');
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)

const blacklistColumns: CrmTableColumnDef[] = [
  { key: 'code', label: '供应商编号', prop: 'code', width: 180, showOverflowTooltip: true },
  { key: 'officialName', label: '供应商名称', minWidth: 260, showOverflowTooltip: true },
  { key: 'industry', label: '行业', width: 160, showOverflowTooltip: true },
  { key: 'actions', label: '操作', width: 190, fixed: 'right', hideable: false, pinned: 'end', reorderable: false }
]

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

const handleRemove = (item: Vendor) => {
  pendingRemove.value = item;
  removeReason.value = '';
  showRemoveDialog.value = true;
};

const confirmRemoveFromBlacklist = async () => {
  const item = pendingRemove.value;
  if (!item?.id) return;
  if (!removeReason.value.trim()) {
    ElNotification.warning({ title: '请填写原因', message: '请输入移出黑名单原因' });
    return;
  }
  removingId.value = item.id;
  try {
    await vendorApi.removeFromBlacklist(item.id, removeReason.value.trim());
    ElNotification.success({ title: '操作成功', message: '供应商已移出黑名单' });
    showRemoveDialog.value = false;
    pendingRemove.value = null;
    removeReason.value = '';
    fetchData();
  } catch {
    ElNotification.error({ title: '操作失败', message: '移出黑名单失败，请稍后重试' });
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

  &--muted {
    color: rgba(150, 170, 195, 0.82);
  }
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
  justify-content: space-between;
  align-items: flex-start;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
}

.list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}
</style>

