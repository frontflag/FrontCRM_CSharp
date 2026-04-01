<template>
  <div class="blacklist-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <div class="page-icon">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <circle cx="12" cy="12" r="10"/><line x1="4.93" y1="4.93" x2="19.07" y2="19.07"/>
          </svg>
        </div>
        <h1 class="page-title">黑名单管理</h1>
        <div class="count-badge">共 {{ totalCount }} 位黑名单客户</div>
      </div>
      <div class="header-right">
        <el-input
          v-model="keyword"
          placeholder="搜索客户名称/编号"
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
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="customer-blacklist-main"
        :columns="blacklistColumns"
        :show-column-settings="false"
        :data="items"
        row-key="id"
        @row-dblclick="goDetail"
      >
        <template #col-customerName="{ row }">
          <div class="record-name-row">
            <span class="record-name record-name--muted">{{ row.customerName || row.officialName }}</span>
            <PartyStatusIcons
              :entity-id="row.id"
              :frozen="!!row.disenableStatus"
              :blacklist="true"
              size="sm"
            />
            <span class="blacklist-tag">黑名单</span>
            <span v-if="row.customerLevel" class="level-tag">{{ getLevelLabel(row.customerLevel) }}</span>
          </div>
        </template>
        <template #col-blackListTime="{ row }">{{ formatDateTime(row.blackListTime || row.updatedAt) }}</template>
        <template #col-blackListUserName="{ row }">{{ row.blackListUserName || '系统员工' }}</template>
        <template #col-blackListReason="{ row }">{{ row.blackListReason || '--' }}</template>
        <template #col-actions="{ row }">
          <div class="record-actions" @dblclick.stop>
            <el-button type="primary" size="small" style="margin-right:8px" @click="goDetail(row)">查看详情</el-button>
            <el-button type="warning" size="small" :loading="removingId === row.id" @click="handleRemove(row)">
              移出黑名单
            </el-button>
          </div>
        </template>
      </CrmDataTable>
    </div>

    <!-- 分页 -->
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
        确定要将「{{ pendingRemove?.customerName || pendingRemove?.officialName || '' }}」移出黑名单吗？原因将记入操作日志。
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
import { customerApi } from '@/api/customer';
import { formatDisplayDateTime } from '@/utils/displayDateTime';
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter();
const loading = ref(false);
const items = ref<any[]>([]);
const totalCount = ref(0);
const pageIndex = ref(1);
const pageSize = 20;
const keyword = ref('');
const removingId = ref<string | null>(null);
const showRemoveDialog = ref(false);
const pendingRemove = ref<any | null>(null);
const removeReason = ref('');
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)

const blacklistColumns: CrmTableColumnDef[] = [
  { key: 'customerCode', label: '客户编号', prop: 'customerCode', width: 180, showOverflowTooltip: true },
  { key: 'customerName', label: '客户名称', minWidth: 260, showOverflowTooltip: true },
  { key: 'customerLevel', label: '级别', width: 100, align: 'center' },
  { key: 'blackListTime', label: '加入时间', width: 180 },
  { key: 'blackListUserName', label: '操作人', width: 130, showOverflowTooltip: true },
  { key: 'blackListReason', label: '黑名单理由', minWidth: 220, showOverflowTooltip: true },
  { key: 'actions', label: '操作', width: 190, fixed: 'right', hideable: false, pinned: 'end', reorderable: false }
];

const fetchData = async () => {
  loading.value = true;
  try {
    const res = await customerApi.getBlacklist({ pageIndex: pageIndex.value, pageSize, keyword: keyword.value });
    items.value = res?.items ?? res?.data ?? (Array.isArray(res) ? res : []);
    totalCount.value = res?.totalCount ?? res?.total ?? items.value.length;
  } catch {
    ElNotification.error({ title: '加载失败', message: '获取黑名单数据失败，请刷新重试' });
  } finally {
    loading.value = false;
  }
};

const handleRemove = (item: any) => {
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
    await customerApi.removeFromBlacklist(item.id, removeReason.value.trim());
    ElNotification.success({ title: '操作成功', message: '客户已移出黑名单' });
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

const goDetail = (item: any) => router.push(`/customers/${item.id}`);

const getLevelLabel = (level: string | number) => {
  const map: Record<string, string> = { '1': 'D级', '2': 'C级', '3': 'B级', '4': 'BPO', '5': 'VIP', '6': 'VPO', D: 'D级', C: 'C级', B: 'B级', BPO: 'BPO', VIP: 'VIP', VPO: 'VPO' };
  return map[String(level)] || String(level);
};

const formatDateTime = (date: string | undefined) => (date ? formatDisplayDateTime(date) : '--');

onMounted(() => fetchData());
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

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
  background: rgba(201,87,69,0.12);
  border: 1px solid rgba(201,87,69,0.25);
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
  background: rgba(201,87,69,0.1);
  border: 1px solid rgba(201,87,69,0.2);
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

  &:hover { border-color: rgba(201,87,69,0.3); }
}

.record-avatar {
  width: 44px;
  height: 44px;
  background: linear-gradient(135deg, rgba(201,87,69,0.2), rgba(201,154,69,0.15));
  border: 1px solid rgba(201,87,69,0.2);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  font-weight: 700;
  color: $color-red-brown;
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

.blacklist-tag {
  font-size: 10px;
  padding: 1px 6px;
  background: rgba(201,87,69,0.15);
  color: $color-red-brown;
  border: 1px solid rgba(201,87,69,0.3);
  border-radius: 3px;
}

.level-tag {
  font-size: 10px;
  padding: 1px 6px;
  background: rgba(50,149,201,0.15);
  color: $color-steel-cyan;
  border: 1px solid rgba(50,149,201,0.25);
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

.meta-sep { color: $text-muted; font-size: 11px; }

.meta-text {
  font-size: 12px;
  color: $text-muted;
}

.record-reason {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  color: $color-amber;
  margin-top: 2px;
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

  svg { margin-bottom: 12px; opacity: 0.3; display: block; margin-left: auto; margin-right: auto; }
  p { font-size: 14px; margin: 0; }
}

.pagination-wrapper {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-top: 24px;
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
