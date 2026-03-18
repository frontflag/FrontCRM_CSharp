<template>
  <div class="vendor-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
              <polyline points="9 22 9 12 15 12 15 22"/>
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

    <!-- 统计卡片 -->
    <div class="stats-row">
      <div class="stat-card">
        <div class="stat-icon stat-icon--blue">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
            <polyline points="9 22 9 12 15 12 15 22"/>
          </svg>
        </div>
        <div class="stat-body">
          <div class="stat-value">{{ vendorStats.totalVendors }}</div>
          <div class="stat-label">供应商总数</div>
        </div>
        <div class="stat-glow stat-glow--blue"></div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--green">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>
          </svg>
        </div>
        <div class="stat-body">
          <div class="stat-value">{{ vendorStats.activeVendors }}</div>
          <div class="stat-label">合作中</div>
        </div>
        <div class="stat-glow stat-glow--green"></div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--cyan">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <line x1="12" y1="5" x2="12" y2="19"/>
            <line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
        </div>
        <div class="stat-body">
          <div class="stat-value">--</div>
          <div class="stat-label">采购总单数</div>
        </div>
        <div class="stat-glow stat-glow--cyan"></div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--amber">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
          </svg>
        </div>
        <div class="stat-body">
          <div class="stat-value">{{ vendorStats.newThisMonth }}</div>
          <div class="stat-label">主要供应商</div>
        </div>
        <div class="stat-glow stat-glow--amber"></div>
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

    <!-- 表格列表 -->
    <div class="table-wrapper" v-loading="loading">
      <table class="data-table">
        <thead>
          <tr>
            <th style="width:44px">序号</th>
            <th style="min-width:180px">供应商名称</th>
            <th style="width:110px">供应商编号</th>
            <th style="width:80px">评级</th>
            <th style="width:100px">行业</th>
            <th style="width:130px">联系人</th>
            <th style="width:130px">联系电话</th>
            <th style="width:160px">地址</th>
            <th style="width:80px">状态</th>
            <th style="width:150px">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="(vendor, index) in vendorList"
            :key="vendor.id"
            class="table-row"
            @click="handleView(vendor)"
          >
            <td class="td-index">{{ (pagination.pageNumber - 1) * pagination.pageSize + index + 1 }}</td>
            <td>
              <div class="vendor-name-cell">
                <div class="cell-avatar">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                    <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
                    <polyline points="9 22 9 12 15 12 15 22"/>
                  </svg>
                </div>
                <div class="cell-name-group">
                  <div class="cell-name">{{ vendor.officialName || vendor.code }}</div>
                  <div class="cell-short" v-if="vendor.code">{{ vendor.code }}</div>
                </div>
              </div>
            </td>
            <td class="td-code">{{ vendor.code }}</td>
            <td>
              <div class="star-rating">
                <span v-for="i in 5" :key="i" class="star" :class="{ 'star--active': i <= (vendor.credit || 0) }">★</span>
              </div>
            </td>
            <td class="td-muted">{{ vendor.industry || '--' }}</td>
            <td>
              <template v-if="vendor.contacts && vendor.contacts.length > 0">
                <span class="td-contact">{{ vendor.contacts[0].cName || vendor.contacts[0].eName || '--' }}</span>
              </template>
              <span v-else class="td-empty">--</span>
            </td>
            <td>
              <template v-if="vendor.contacts && vendor.contacts.length > 0">
                <span class="td-phone">{{ vendor.contacts[0].mobile || vendor.contacts[0].tel || '--' }}</span>
              </template>
              <span v-else class="td-empty">--</span>
            </td>
            <td>
              <span class="td-address" :title="vendor.officeAddress">{{ vendor.officeAddress || '--' }}</span>
            </td>
            <td>
              <span class="status-badge" :class="getStatusClass(vendor.status)">
                {{ getStatusLabel(vendor.status) }}
              </span>
            </td>
            <td @click.stop>
              <div class="action-btns">
                <button class="action-btn" @click.stop="handleView(vendor)">详情</button>
                <button class="action-btn" @click.stop="handleEdit(vendor)">编辑</button>
                <button class="action-btn action-btn--danger" @click.stop="handleDelete(vendor)">删除</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- 空状态 -->
      <div v-if="!loading && vendorList.length === 0" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
          <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
          <polyline points="9 22 9 12 15 12 15 22"/>
        </svg>
        <p>暂无供应商数据</p>
        <button class="btn-primary" @click="handleCreate">新增供应商</button>
      </div>
    </div>

    <!-- 分页 -->
    <div class="pagination-wrapper" v-if="totalCount > 0">
      <el-pagination
        v-model:current-page="pagination.pageNumber"
        v-model:page-size="pagination.pageSize"
        :page-sizes="[20, 50, 100]"
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
  pageSize: 20,
  keyword: '',
  status: undefined
});

const pagination = reactive({ pageNumber: 1, pageSize: 20 });

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
  } catch (error: any) {
    // 仅在非空结果（真实网络/服务器错误）时提示，空数据不报错
    const isEmptyResult = !error?.response || error?.response?.status === 404;
    if (!isEmptyResult) {
      console.error('获取供应商列表失败', error);
      ElMessage.error('获取供应商列表失败，请检查网络或后端服务');
    } else {
      vendorList.value = [];
      totalCount.value = 0;
    }
  } finally {
    loading.value = false;
  }
};

const fetchVendorStats = async () => {
  try {
    vendorStats.value = await vendorApi.getVendorStatistics();
  } catch {
    // 统计接口失败时静默处理，不影响列表展示
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

const handleSizeChange = (size: number) => { pagination.pageSize = size; fetchVendorList(); };
const handlePageChange = (page: number) => { pagination.pageNumber = page; fetchVendorList(); };

onMounted(() => {
  fetchVendorList();
  fetchVendorStats();
});
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.vendor-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

// ---- 页面头部 ----
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 10px;
  }
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;

  .page-icon {
    width: 36px;
    height: 36px;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.25);
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: $cyan-primary;
  }

  .page-title {
    font-size: 20px;
    font-weight: 600;
    color: $text-primary;
    margin: 0;
    letter-spacing: 0.5px;
  }
}

.vendor-count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

// ---- 按钮 ----
.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  letter-spacing: 0.5px;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &.btn-sm { padding: 6px 12px; font-size: 12px; }
}

.btn-ghost {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
}

// ---- 统计卡片 ----
.stats-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 20px;
}

.stat-card {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  position: relative;
  overflow: hidden;
  transition: transform 0.2s, box-shadow 0.2s;

  &:hover {
    transform: translateY(-2px);
    box-shadow: $shadow-md;
  }
}

.stat-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  &--blue  { background: rgba(50, 149, 201, 0.15); color: $color-steel-cyan; border: 1px solid rgba(50,149,201,0.25); }
  &--green { background: rgba(70, 191, 145, 0.15); color: $color-mint-green; border: 1px solid rgba(70,191,145,0.25); }
  &--cyan  { background: rgba(0, 212, 255, 0.12);  color: $cyan-primary;     border: 1px solid rgba(0,212,255,0.25); }
  &--amber { background: rgba(201, 154, 69, 0.15); color: $color-amber;      border: 1px solid rgba(201,154,69,0.25); }
}

.stat-body {
  .stat-value {
    font-size: 22px;
    font-weight: 700;
    color: $text-primary;
    font-family: 'Space Mono', monospace;
    line-height: 1.2;
  }
  .stat-label {
    font-size: 12px;
    color: $text-muted;
    margin-top: 3px;
  }
}

.stat-glow {
  position: absolute;
  right: -20px;
  top: -20px;
  width: 80px;
  height: 80px;
  border-radius: 50%;
  filter: blur(30px);
  opacity: 0.4;

  &--blue  { background: $color-steel-cyan; }
  &--green { background: $color-mint-green; }
  &--cyan  { background: $cyan-primary; }
  &--amber { background: $color-amber; }
}

// ---- 搜索栏 ----
.search-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.list-title {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
  white-space: nowrap;
}

.search-input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.search-icon {
  position: absolute;
  left: 10px;
  color: $text-muted;
  pointer-events: none;
}

.search-input {
  width: 220px;
  padding: 7px 12px 7px 32px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  outline: none;
  transition: border-color 0.2s;

  &::placeholder { color: $text-muted; }
  &:focus { border-color: rgba(0,212,255,0.4); }
}

.status-select {
  width: 120px;
  :deep(.el-select__wrapper) { background: $layer-2 !important; box-shadow: none !important; border: 1px solid $border-panel !important; border-radius: $border-radius-md !important; }
  :deep(.el-select__placeholder) { color: $text-muted !important; }
  :deep(.el-select__selected-item) { color: $text-primary !important; }
}

// ---- 表格 ----
.table-wrapper {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
  min-height: 200px;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;

  thead tr {
    background: rgba(0, 212, 255, 0.04);
    border-bottom: 1px solid rgba(0, 212, 255, 0.1);
  }

  th {
    padding: 12px 14px;
    text-align: left;
    font-size: 12px;
    font-weight: 500;
    color: $text-muted;
    white-space: nowrap;
    letter-spacing: 0.3px;
  }

  td {
    padding: 11px 14px;
    border-bottom: 1px solid rgba(255, 255, 255, 0.04);
    vertical-align: middle;
  }
}

.table-row {
  cursor: pointer;
  transition: background 0.15s;

  &:hover {
    background: rgba(0, 212, 255, 0.04);

    .action-btns { opacity: 1; }
  }

  &:last-child td { border-bottom: none; }
}

// 供应商名称单元格
.vendor-name-cell {
  display: flex;
  align-items: center;
  gap: 10px;
}

.cell-avatar {
  width: 32px;
  height: 32px;
  flex-shrink: 0;
  background: linear-gradient(135deg, rgba(0,102,255,0.2), rgba(0,212,255,0.15));
  border: 1px solid rgba(0,212,255,0.2);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: $cyan-primary;
}

.cell-name-group {
  min-width: 0;
}

.cell-name {
  font-size: 13px;
  font-weight: 500;
  color: $text-primary;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.cell-short {
  font-size: 11px;
  color: $text-muted;
  margin-top: 1px;
  font-family: 'Space Mono', monospace;
}

.td-code {
  font-family: 'Space Mono', monospace;
  font-size: 12px;
  color: $text-muted;
}

.td-muted {
  color: $text-secondary;
  font-size: 12.5px;
}

.td-contact {
  color: $text-secondary;
  font-size: 12.5px;
}

.td-phone {
  color: $text-secondary;
  font-size: 12px;
  font-family: 'Space Mono', monospace;
}

.td-address {
  color: $text-secondary;
  font-size: 12px;
  display: -webkit-box;
  -webkit-line-clamp: 1;
  -webkit-box-orient: vertical;
  overflow: hidden;
  max-width: 160px;
}

.td-empty {
  color: rgba(255,255,255,0.2);
  font-size: 12px;
}

.td-index {
  color: $text-muted;
  font-size: 12px;
  text-align: center;
}

// ---- 星级 ----
.star-rating {
  display: flex;
  gap: 1px;
}

.star {
  font-size: 13px;
  color: rgba(255,255,255,0.15);
  line-height: 1;

  &--active {
    color: #C99A45;
  }
}

// ---- 状态徽章 ----
.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  padding: 2px 8px;
  border-radius: 10px;

  &::before {
    content: '';
    width: 6px;
    height: 6px;
    border-radius: 50%;
    flex-shrink: 0;
  }

  &.status-active {
    color: #46BF91;
    background: rgba(70,191,145,0.1);
    border: 1px solid rgba(70,191,145,0.25);
    &::before { background: #46BF91; box-shadow: 0 0 4px #46BF91; }
  }

  &.status-pending {
    color: $color-amber;
    background: rgba(201,154,69,0.1);
    border: 1px solid rgba(201,154,69,0.25);
    &::before { background: $color-amber; }
  }

  &.status-draft {
    color: $text-muted;
    background: rgba(107,122,141,0.1);
    border: 1px solid rgba(107,122,141,0.2);
    &::before { background: #8A9BB0; }
  }
}

// ---- 操作按钮 ----
.action-btns {
  display: flex;
  align-items: center;
  gap: 6px;
  opacity: 0;
  transition: opacity 0.15s;
}

.action-btn {
  padding: 4px 10px;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  border-radius: 5px;
  cursor: pointer;
  transition: all 0.15s;
  background: rgba(0, 212, 255, 0.08);
  border: 1px solid rgba(0, 212, 255, 0.2);
  color: $cyan-primary;

  &:hover {
    background: rgba(0, 212, 255, 0.15);
    border-color: rgba(0, 212, 255, 0.4);
  }

  &--danger {
    background: rgba(201, 87, 69, 0.08);
    border-color: rgba(201, 87, 69, 0.2);
    color: #C95745;

    &:hover {
      background: rgba(201, 87, 69, 0.15);
      border-color: rgba(201, 87, 69, 0.4);
    }
  }
}

// ---- 空状态 ----
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: $text-muted;

  svg { margin-bottom: 16px; opacity: 0.4; }
  p { margin: 0 0 16px; font-size: 14px; }
}

// ---- 分页 ----
.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}

.quantum-pagination {
  :deep(.el-pagination__total) { color: $text-muted; }
  :deep(.el-pagination__sizes .el-select__wrapper) { background: $layer-2 !important; border: 1px solid $border-panel !important; box-shadow: none !important; }
  :deep(.el-pager li) { background: $layer-2; border: 1px solid $border-panel; color: $text-secondary; border-radius: 6px; margin: 0 2px; }
  :deep(.el-pager li.is-active) { background: rgba(0,212,255,0.15); border-color: rgba(0,212,255,0.4); color: $cyan-primary; }
  :deep(.btn-prev), :deep(.btn-next) { background: $layer-2 !important; border: 1px solid $border-panel !important; color: $text-secondary !important; border-radius: 6px !important; }
}
</style>
