<template>
  <div class="vendor-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
              <circle cx="9" cy="7" r="4"/>
              <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
              <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
            </svg>
          </div>
          <h1 class="page-title">供应商管理</h1>
        </div>
        <div class="vendor-count-badge">共 {{ totalCount }} 个供应商</div>
      </div>
      <div class="header-right">
        <button class="btn-ghost btn-sm" @click="router.push('/vendors/recycle')">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14H6L5 6"/>
            <path d="M10 11v6"/><path d="M14 11v6"/>
          </svg>
          回收站
        </button>
        <button class="btn-ghost btn-sm" @click="router.push('/vendors/blacklist')">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/><line x1="4.93" y1="4.93" x2="19.07" y2="19.07"/>
          </svg>
          黑名单
        </button>
        <button class="btn-primary" @click="handleCreate">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"/>
            <line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          新增供应商
        </button>
      </div>
    </div>

    <div class="stats-row">
      <div class="stat-card">
        <div class="stat-label">供应商总数</div>
        <div class="stat-value">{{ vendorStats.totalVendors }} <span class="stat-unit">家</span></div>
      </div>
      <div class="stat-card">
        <div class="stat-label">合作中</div>
        <div class="stat-value stat-value--cyan">{{ vendorStats.activeVendors }} <span class="stat-unit">家</span></div>
      </div>
      <div class="stat-card">
        <div class="stat-label">采购总数</div>
        <div class="stat-value">-- <span class="stat-unit">单</span></div>
      </div>
      <div class="stat-card">
        <div class="stat-label">主要供应商</div>
        <div class="stat-value">{{ vendorStats.newThisMonth }} <span class="stat-unit">家</span></div>
      </div>
    </div>

    <!-- 搜索栏 -->
    <div class="search-bar">
      <div class="search-left">
        <span class="list-title">供应商列表</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
          </svg>
          <input
            v-model="searchForm.keyword"
            class="search-input"
            placeholder="搜索供应商名称/联系人..."
            @keyup.enter="handleSearch"
          />
        </div>
        <el-select v-model="searchForm.status" placeholder="全部状态" clearable class="status-select" @change="handleSearch">
          <el-option label="草稿" :value="0" />
          <el-option label="待审" :value="1" />
          <el-option label="合作中" :value="2" />
        </el-select>
        <button class="btn-primary btn-sm" @click="handleSearch">搜索</button>
        <button class="btn-ghost btn-sm" @click="handleReset">重置</button>
      </div>
    </div>

    <!-- 卡片网格 -->
    <div v-loading="loading" class="card-grid">
      <div
        v-for="vendor in vendorList"
        :key="vendor.id"
        class="vendor-card"
        @click="handleView(vendor)"
      >
        <!-- 卡片头部 -->
        <div class="card-header">
          <div class="card-avatar">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
              <polyline points="9 22 9 12 15 12 15 22"/>
            </svg>
          </div>
          <div class="card-title-area">
            <div class="card-name">{{ vendor.officialName || vendor.code }}</div>
            <div class="card-code">{{ vendor.code }}</div>
          </div>
          <div class="card-status-badge" :class="getStatusClass(vendor.status)">
            {{ getStatusLabel(vendor.status) }}
          </div>
        </div>

        <!-- 星级评分 -->
        <div class="card-rating">
          <span v-for="i in 5" :key="i" class="star" :class="{ 'star--active': i <= (vendor.credit || 0) }">★</span>
          <span class="card-category" v-if="vendor.industry">{{ vendor.industry }}</span>
        </div>

        <!-- 联系人信息 -->
        <div class="card-contact" v-if="vendor.contacts && vendor.contacts.length > 0">
          <div class="contact-row">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>
            </svg>
            <span>{{ vendor.contacts[0].cName || vendor.contacts[0].eName || '--' }}</span>
            <span class="contact-phone">{{ vendor.contacts[0].mobile || vendor.contacts[0].tel || '' }}</span>
          </div>
          <div class="contact-row" v-if="vendor.officeAddress">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/>
            </svg>
            <span class="address-text">{{ vendor.officeAddress }}</span>
          </div>
        </div>
        <div class="card-contact" v-else>
          <div class="contact-row no-contact">暂无联系人</div>
        </div>

        <!-- 底部统计 -->
        <div class="card-footer">
          <div class="card-stat">
            <div class="card-stat-label">采购单数</div>
            <div class="card-stat-value">--</div>
          </div>
          <div class="card-stat">
            <div class="card-stat-label">累计采购额</div>
            <div class="card-stat-value card-stat-value--highlight">--</div>
          </div>
        </div>

        <!-- 操作按钮（hover 显示） -->
        <div class="card-actions" @click.stop>
          <button class="card-action-btn" @click.stop="handleEdit(vendor)">编辑</button>
          <button class="card-action-btn card-action-btn--danger" @click.stop="handleDelete(vendor)">删除</button>
        </div>
      </div>

      <!-- 空状态 -->
      <div v-if="!loading && vendorList.length === 0" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
          <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/>
          <path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>
        </svg>
        <p>暂无供应商数据</p>
        <button class="btn-primary" @click="handleCreate">新增供应商</button>
      </div>
    </div>

    <!-- 分页 -->
    <div class="pagination-wrapper" v-if="totalCount > pagination.pageSize">
      <el-pagination
        v-model:current-page="pagination.pageNumber"
        v-model:page-size="pagination.pageSize"
        :page-sizes="[12, 24, 48]"
        :total="totalCount"
        layout="total, sizes, prev, pager, next"
        @size-change="handleSizeChange"
        @current-change="handlePageChange"
        class="quantum-pagination"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { vendorApi } from '@/api/vendor';
import type { Vendor, VendorSearchRequest, VendorStatistics } from '@/types/vendor';

const router = useRouter();
const loading = ref(false);
const vendorList = ref<Vendor[]>([]);
const totalCount = ref(0);
const vendorStats = ref<VendorStatistics>({
  totalVendors: 0,
  activeVendors: 0,
  newThisMonth: 0,
  byLevel: {},
  byIndustry: {}
});

const searchForm = reactive<VendorSearchRequest>({
  pageNumber: 1,
  pageSize: 12,
  keyword: '',
  status: undefined
});

const pagination = reactive({ pageNumber: 1, pageSize: 12 });

const getStatusLabel = (status?: number) => {
  if (status === 0) return '草稿';
  if (status === 1) return '待审';
  if (status === 2) return '合作中';
  return '未知';
};
const getStatusClass = (status?: number) => {
  if (status === 0) return 'status-draft';
  if (status === 1) return 'status-pending';
  if (status === 2) return 'status-active';
  return 'status-draft';
};

const fetchVendorList = async () => {
  loading.value = true;
  try {
    const params: VendorSearchRequest = {
      ...searchForm,
      pageNumber: pagination.pageNumber,
      pageSize: pagination.pageSize
    };
    const response = await vendorApi.searchVendors(params);
    vendorList.value = response.items || [];
    totalCount.value = response.totalCount ?? 0;
    vendorStats.value = await vendorApi.getVendorStatistics();
  } catch (error) {
    console.error('获取供应商列表失败', error);
    ElMessage.error('获取供应商列表失败');
  } finally {
    loading.value = false;
  }
};

const handleSearch = () => {
  pagination.pageNumber = 1;
  fetchVendorList();
};

const handleReset = () => {
  searchForm.keyword = '';
  searchForm.status = undefined;
  handleSearch();
};

const handleCreate = () => router.push('/vendors/create');
const handleView = (row: Vendor) => router.push(`/vendors/${row.id}`);
const handleEdit = (row: Vendor) => router.push(`/vendors/${row.id}/edit`);

const handleDelete = (row: Vendor) => {
  ElMessageBox.prompt(
    `确定要删除供应商 "${row.officialName || row.code}" 吗？可填写删除原因。`,
    '确认删除',
    {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      inputPlaceholder: '如：长期停供、信息重复等',
      inputType: 'textarea'
    }
  ).then(async ({ value }) => {
    await vendorApi.deleteVendorSoft(row.id, value || '');
    ElMessage.success('删除成功（已进入回收站）');
    fetchVendorList();
  }).catch(() => {});
};

const handleSizeChange = () => { fetchVendorList(); };
const handlePageChange = () => { fetchVendorList(); };



onMounted(() => {
  fetchVendorList();
});
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.vendor-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px;
  .header-left { display: flex; align-items: center; gap: 12px; }
  .header-right { display: flex; align-items: center; gap: 8px; }
}
.page-title-group {
  display: flex; align-items: center; gap: 10px;
  .page-icon {
    width: 36px; height: 36px;
    background: rgba(0,212,255,0.1); border: 1px solid rgba(0,212,255,0.25);
    border-radius: 10px; display: flex; align-items: center; justify-content: center; color: $cyan-primary;
  }
  .page-title { font-size: 20px; font-weight: 600; color: $text-primary; margin: 0; }
}
.vendor-count-badge {
  font-size: 12px; color: $text-muted;
  background: rgba(255,255,255,0.05); border: 1px solid $border-panel;
  border-radius: 20px; padding: 3px 10px;
}
.btn-primary {
  display: inline-flex; align-items: center; gap: 6px; padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0,102,255,0.8), rgba(0,212,255,0.7));
  border: 1px solid rgba(0,212,255,0.4); border-radius: $border-radius-md;
  color: #fff; font-size: 13px; font-family: 'Noto Sans SC', sans-serif; cursor: pointer; transition: all 0.2s;
  &.btn-sm { padding: 6px 12px; font-size: 12px; }
  &:hover { opacity: 0.9; }
}
.btn-ghost {
  display: inline-flex; align-items: center; gap: 6px; padding: 6px 12px;
  background: transparent; border: 1px solid $border-panel; border-radius: $border-radius-md;
  color: $text-muted; font-size: 12px; font-family: 'Noto Sans SC', sans-serif; cursor: pointer; transition: all 0.2s;
  &.btn-sm { padding: 5px 10px; }
  &:hover { border-color: rgba(0,212,255,0.3); color: $text-secondary; }
}

/* 统计卡片 */
.stats-row {
  display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; margin-bottom: 16px;
}
.stat-card {
  background: $layer-2; border-radius: $border-radius-md;
  border: 1px solid rgba(0,212,255,0.1); padding: 14px 18px;
}
.stat-label { font-size: 12px; color: $text-muted; margin-bottom: 6px; }
.stat-value {
  font-size: 22px; font-weight: 700; color: $text-primary; font-family: 'Space Mono', monospace;
  &--cyan { color: $cyan-primary; }
  .stat-unit { font-size: 13px; font-weight: 400; color: $text-muted; margin-left: 2px; }
}

/* 搜索栏 */
.search-bar {
  display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px;
}
.search-left { display: flex; align-items: center; gap: 10px; }
.list-title { font-size: 14px; font-weight: 600; color: $text-primary; white-space: nowrap; }
.search-input-wrap { position: relative; display: flex; align-items: center; }
.search-icon { position: absolute; left: 10px; color: $text-muted; pointer-events: none; }
.search-input {
  width: 220px; padding: 7px 12px 7px 32px;
  background: $layer-2; border: 1px solid $border-panel; border-radius: $border-radius-md;
  color: $text-primary; font-size: 13px; font-family: 'Noto Sans SC', sans-serif; outline: none; transition: border-color 0.2s;
  &::placeholder { color: $text-muted; }
  &:focus { border-color: rgba(0,212,255,0.4); }
}
.status-select {
  width: 120px;
  :deep(.el-input__wrapper) { background: $layer-2; box-shadow: none; border: 1px solid $border-panel; border-radius: $border-radius-md; }
  :deep(.el-input__inner) { color: $text-primary; font-size: 13px; }
}

/* 卡片网格 */
.card-grid {
  display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 16px; min-height: 200px;
}
.vendor-card {
  background: $layer-2; border: 1px solid rgba(0,212,255,0.1);
  border-radius: 12px; padding: 16px; cursor: pointer; transition: all 0.2s;
  position: relative; overflow: hidden;
  &:hover {
    border-color: rgba(0,212,255,0.35);
    box-shadow: 0 4px 20px rgba(0,212,255,0.08);
    transform: translateY(-1px);
    .card-actions { opacity: 1; }
  }
}
.card-header { display: flex; align-items: flex-start; gap: 10px; margin-bottom: 10px; }
.card-avatar {
  width: 38px; height: 38px; flex-shrink: 0;
  background: rgba(0,212,255,0.1); border: 1px solid rgba(0,212,255,0.2);
  border-radius: 10px; display: flex; align-items: center; justify-content: center; color: $cyan-primary;
}
.card-title-area { flex: 1; min-width: 0; }
.card-name {
  font-size: 14px; font-weight: 600; color: $text-primary;
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-bottom: 2px;
}
.card-code { font-size: 11px; color: $text-muted; }
.card-status-badge {
  flex-shrink: 0; font-size: 11px; padding: 2px 8px; border-radius: 4px; font-weight: 500;
  &.status-active { background: rgba(70,191,145,0.15); color: #46BF91; }
  &.status-pending { background: rgba(255,193,7,0.15); color: #ffc107; }
  &.status-draft { background: rgba(255,255,255,0.08); color: $text-muted; }
}
.card-rating { display: flex; align-items: center; gap: 2px; margin-bottom: 10px; }
.star { font-size: 14px; color: rgba(255,255,255,0.15); &--active { color: #ffc107; } }
.card-category {
  margin-left: 8px; font-size: 11px; padding: 1px 7px;
  background: rgba(0,212,255,0.1); border: 1px solid rgba(0,212,255,0.2);
  border-radius: 3px; color: $cyan-primary;
}
.card-contact {
  margin-bottom: 12px; border-top: 1px solid rgba(255,255,255,0.05); padding-top: 10px;
}
.contact-row {
  display: flex; align-items: center; gap: 6px;
  font-size: 12px; color: $text-secondary; margin-bottom: 5px;
  svg { flex-shrink: 0; color: $text-muted; }
  &.no-contact { color: $text-muted; font-style: italic; }
}
.contact-phone { color: $text-muted; margin-left: 4px; }
.address-text { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 200px; }
.card-footer { display: flex; border-top: 1px solid rgba(255,255,255,0.05); padding-top: 10px; }
.card-stat {
  flex: 1;
  &:not(:last-child) { border-right: 1px solid rgba(255,255,255,0.05); padding-right: 12px; margin-right: 12px; }
}
.card-stat-label { font-size: 11px; color: $text-muted; margin-bottom: 3px; }
.card-stat-value {
  font-size: 15px; font-weight: 700; color: $text-primary; font-family: 'Space Mono', monospace;
  &--highlight { color: $cyan-primary; }
}
.card-actions {
  position: absolute; bottom: 0; left: 0; right: 0;
  background: linear-gradient(to top, rgba(10,22,40,0.95) 0%, transparent 100%);
  padding: 20px 16px 12px;
  display: flex; gap: 8px; justify-content: flex-end;
  opacity: 0; transition: opacity 0.2s;
}
.card-action-btn {
  padding: 5px 12px; font-size: 12px; border-radius: 5px;
  border: 1px solid rgba(0,212,255,0.3); background: rgba(0,212,255,0.1);
  color: $cyan-primary; cursor: pointer; transition: all 0.15s; font-family: 'Noto Sans SC', sans-serif;
  &:hover { background: rgba(0,212,255,0.2); }
  &--danger { border-color: rgba(201,87,69,0.3); background: rgba(201,87,69,0.1); color: #C95745; }
  &--danger:hover { background: rgba(201,87,69,0.2); }
}
.empty-state {
  grid-column: 1 / -1; display: flex; flex-direction: column; align-items: center; justify-content: center;
  padding: 60px 0; color: $text-muted; gap: 12px;
  svg { opacity: 0.3; } p { font-size: 14px; margin: 0; }
}
.pagination-wrapper { margin-top: 20px; display: flex; justify-content: flex-end; }
:deep(.quantum-pagination) {
  --el-pagination-button-color: rgba(0,212,255,0.8);
  --el-pagination-hover-color: #00D4FF;
}
</style>
