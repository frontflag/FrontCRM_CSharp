<template>
  <div class="blacklist-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-icon">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <circle cx="12" cy="12" r="10" /><line x1="4.93" y1="4.93" x2="19.07" y2="19.07" />
          </svg>
        </div>
        <h1 class="page-title">{{ t('vendorBlacklist.title') }}</h1>
        <div class="count-badge">{{ t('vendorBlacklist.count', { count: totalCount }) }}</div>
      </div>
      <div class="header-right">
        <el-input
          v-model="keyword"
          :placeholder="t('vendorBlacklist.searchPlaceholder')"
          clearable
          style="width:220px"
          @keyup.enter="onSearch"
          @clear="onSearch"
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
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
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
            <span class="blacklist-tag">{{ t('vendorBlacklist.tag') }}</span>
          </div>
        </template>
        <template #col-industry="{ row }">{{ row.industry || '--' }}</template>
        <template #col-actions-header>
          <div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="opColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="toggleOpCol"
            >
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <el-button size="small" @click.stop="goDetail(row)">{{ t('vendorBlacklist.viewDetail') }}</el-button>
              <el-button type="warning" size="small" :loading="removingId === row.id" @click.stop="handleRemove(row)">
                {{ t('vendorBlacklist.remove') }}
              </el-button>
            </div>
            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="goDetail(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('vendorBlacklist.viewDetail') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item divided @click.stop="handleRemove(row)">
                    <span class="op-more-item op-more-item--warning">{{ t('vendorBlacklist.remove') }}</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>
    </div>

    <div v-if="totalCount > 0" class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button class="list-settings-btn" link type="primary" :aria-label="t('systemUser.colSetting')" @click="dataTableRef?.openColumnSettings?.()">
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
      <el-pagination
        v-model:current-page="pageIndex"
        v-model:page-size="pageSize"
        :page-sizes="[20, 50, 100]"
        :total="totalCount"
        layout="total, sizes, prev, pager, next"
        background
        @size-change="onPageSizeChange"
        @current-change="fetchData"
      />
    </div>

    <el-dialog v-model="showRemoveDialog" :title="t('vendorBlacklist.removeTitle')" width="440px" :close-on-click-modal="false">
      <div style="color:rgba(200,216,232,0.75);font-size:13px;margin-bottom:16px">
        {{ t('vendorBlacklist.removeConfirm', { name: pendingRemove?.officialName || pendingRemove?.code || '' }) }}
      </div>
      <el-form label-width="90px">
        <el-form-item :label="t('vendorBlacklist.removeReason')" required>
          <el-input
            v-model="removeReason"
            type="textarea"
            :rows="3"
            :placeholder="t('vendorBlacklist.removeReasonPlaceholder')"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showRemoveDialog = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="removingId !== null" @click="confirmRemoveFromBlacklist">{{ t('common.confirm') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { ElNotification } from 'element-plus';
import { useI18n } from 'vue-i18n'
import { Setting } from '@element-plus/icons-vue'
import { vendorApi } from '@/api/vendor';
import type { Vendor } from '@/types/vendor';
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter();
const { t } = useI18n()
const loading = ref(false);
const items = ref<Vendor[]>([]);
const totalCount = ref(0);
const pageIndex = ref(1);
const pageSize = ref(20);
const keyword = ref('');
const removingId = ref<string | null>(null);
const showRemoveDialog = ref(false);
const pendingRemove = ref<Vendor | null>(null);
const removeReason = ref('');
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 173
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const blacklistColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'code', label: t('vendorBlacklist.columns.code'), prop: 'code', width: 180, showOverflowTooltip: true },
  { key: 'officialName', label: t('vendorBlacklist.columns.name'), minWidth: 260, showOverflowTooltip: true },
  { key: 'industry', label: t('vendorBlacklist.columns.industry'), width: 160, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('vendorBlacklist.columns.actions'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    className: 'op-col',
    labelClassName: 'op-col',
    hideable: false,
    pinned: 'end',
    reorderable: false
  }
])

function onPageSizeChange() {
  pageIndex.value = 1;
  fetchData();
}

function onSearch() {
  pageIndex.value = 1;
  fetchData();
}

const fetchData = async () => {
  loading.value = true;
  try {
    const res = await vendorApi.getBlacklist({ page: pageIndex.value, pageSize: pageSize.value, keyword: keyword.value });
    items.value = res?.items ?? (Array.isArray(res) ? res : []);
    totalCount.value = res?.totalCount ?? res?.total ?? 0;
  } catch {
    ElNotification.error({ title: t('vendorBlacklist.loadFailedTitle'), message: t('vendorBlacklist.loadFailedMessage') });
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
    ElNotification.warning({ title: t('vendorBlacklist.reasonRequiredTitle'), message: t('vendorBlacklist.reasonRequiredMessage') });
    return;
  }
  removingId.value = item.id;
  try {
    await vendorApi.removeFromBlacklist(item.id, removeReason.value.trim());
    ElNotification.success({ title: t('vendorBlacklist.successTitle'), message: t('vendorBlacklist.successMessage') });
    showRemoveDialog.value = false;
    pendingRemove.value = null;
    removeReason.value = '';
    fetchData();
  } catch {
    ElNotification.error({ title: t('vendorBlacklist.failedTitle'), message: t('vendorBlacklist.failedMessage') });
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
    font-family: 'Noto Sans SC', sans-serif;
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

.list-footer-density-anchor {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  min-height: 0;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}
</style>

