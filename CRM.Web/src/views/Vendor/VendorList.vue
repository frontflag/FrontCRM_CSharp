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
        <div class="stat-value">{{ vendorStats.totalVendors }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">活跃供应商</div>
        <div class="stat-value">{{ vendorStats.activeVendors }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">本月新增</div>
        <div class="stat-value">{{ vendorStats.newThisMonth }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">涉及行业数</div>
        <div class="stat-value">{{ Object.keys(vendorStats.byIndustry).length }}</div>
      </div>
    </div>

    <div class="search-panel">
      <div class="search-panel-inner">
        <div class="search-field">
          <label class="field-label">关键字</label>
          <div class="input-wrapper">
            <span class="input-icon">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
              </svg>
            </span>
            <el-input
              v-model="searchForm.keyword"
              placeholder="供应商编号/名称"
              clearable
              @keyup.enter="handleSearch"
              class="custom-input"
            />
          </div>
        </div>
        <div class="search-field">
          <label class="field-label">状态</label>
          <el-select v-model="searchForm.status" placeholder="全部状态" clearable class="custom-select">
            <el-option label="草稿" :value="0" />
            <el-option label="待审" :value="1" />
            <el-option label="已审" :value="2" />
          </el-select>
        </div>
        <div class="search-actions">
          <button class="btn-primary btn-sm" @click="handleSearch">搜索</button>
          <button class="btn-ghost btn-sm" @click="handleReset">重置</button>
        </div>
      </div>
    </div>

    <div class="table-panel">
      <el-table
        :data="vendorList"
        v-loading="loading"
        @row-click="handleRowClick"
        class="quantum-table"
        :header-cell-style="tableHeaderStyle"
        :cell-style="tableCellStyle"
        :row-style="tableRowStyle"
      >
        <el-table-column type="index" width="50" align="center" />
        <el-table-column prop="code" label="供应商编号" width="140">
          <template #default="{ row }">
            <span class="code-link" @click.stop="handleView(row)">{{ row.code }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="officialName" label="供应商名称" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            <div class="vendor-name-cell">
              <div class="vendor-avatar">{{ (row.officialName || '?')[0] }}</div>
              <span class="name">{{ row.officialName || '--' }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="联系人" width="180">
          <template #default="{ row }">
            <div v-if="row.contacts && row.contacts.length > 0" class="contact-cell">
              <div class="contact-name">{{ row.contacts[0].cName || row.contacts[0].eName || '--' }}</div>
              <div class="contact-phone">{{ row.contacts[0].mobile || row.contacts[0].tel || '' }}</div>
            </div>
            <span v-else class="no-data">--</span>
          </template>
        </el-table-column>
        <el-table-column prop="industry" label="行业" width="140" show-overflow-tooltip>
          <template #default="{ row }">
            <span class="text-secondary">{{ row.industry || '--' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="officeAddress" label="地区/地址" min-width="180" show-overflow-tooltip>
          <template #default="{ row }">
            <span class="text-secondary">{{ row.officeAddress || '--' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="credit" label="信用评级" width="110" align="center">
          <template #default="{ row }">
            <span class="text-secondary">{{ row.credit ?? '--' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="companyInfo" label="备注" min-width="160" show-overflow-tooltip>
          <template #default="{ row }">
            <span class="text-secondary">{{ row.companyInfo || '--' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-switch
              v-model="row.status"
              :active-value="1"
              :inactive-value="0"
              :active-color="'#46BF91'"
              :inactive-color="'rgba(255,255,255,0.1)'"
              @change="(val: any) => handleStatusChange(row, val)"
              @click.stop
            />
          </template>
        </el-table-column>
        <el-table-column prop="createTime" label="创建时间" width="170">
          <template #default="{ row }">
            <span class="text-secondary">{{ formatDate(row.createTime) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="140" fixed="right">
          <template #default="{ row }">
            <button class="action-btn" @click.stop="handleEdit(row)">编辑</button>
            <el-dropdown @command="(cmd: string) => handleCommand(cmd, row)" @click.stop>
              <button class="action-btn action-btn--more">
                更多
                <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="6 9 12 15 18 9"/>
                </svg>
              </button>
              <template #dropdown>
                <el-dropdown-menu class="quantum-dropdown">
                  <el-dropdown-item command="delete" class="danger-item">删除</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="pagination.pageNumber"
          v-model:page-size="pagination.pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="totalCount"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handlePageChange"
          class="quantum-pagination"
        />
      </div>
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

const tableHeaderStyle = () => ({
  background: '#0A1628',
  color: 'rgba(200,216,232,0.55)',
  fontSize: '12px',
  fontWeight: '500',
  letterSpacing: '0.5px',
  borderBottom: '1px solid rgba(0,212,255,0.12)',
  padding: '10px 0'
});

const tableCellStyle = () => ({
  background: 'transparent',
  borderBottom: '1px solid rgba(255,255,255,0.05)',
  color: 'rgba(224,244,255,0.85)',
  fontSize: '13px'
});

const tableRowStyle = () => ({
  background: 'transparent',
  cursor: 'pointer'
});

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
const handleRowClick = (row: Vendor) => handleView(row);

const handleStatusChange = async (row: Vendor, val: number) => {
  try {
    if (val === 1) {
      await vendorApi.activateVendor(row.id);
    } else {
      await vendorApi.deactivateVendor(row.id);
    }
    ElMessage.success(`供应商已${val === 1 ? '启用' : '停用'}`);
  } catch (e) {
    row.status = val === 1 ? 0 : 1;
    ElMessage.error('更新状态失败');
  }
};

const handleCommand = (cmd: string, row: Vendor) => {
  if (cmd === 'delete') {
    ElMessageBox.prompt(
      `确定要删除供应商 "${row.officialName || row.code}" 吗？可填写删除原因，便于回溯。`,
      '确认删除',
      {
        confirmButtonText: '删除',
        cancelButtonText: '取消',
        inputPlaceholder: '如：长期停供、信息重复、业务合并等',
        inputType: 'textarea'
      }
    ).then(async ({ value }) => {
      await vendorApi.deleteVendorSoft(row.id, value || '');
      ElMessage.success('删除成功（已进入回收站）');
      fetchVendorList();
    }).catch(() => {});
  }
};

const handleSizeChange = () => { fetchVendorList(); };
const handlePageChange = () => { fetchVendorList(); };

const formatDate = (val: string | undefined) => {
  if (!val) return '--';
  try {
    const d = new Date(val);
    return isNaN(d.getTime()) ? val : d.toLocaleString('zh-CN');
  } catch {
    return val;
  }
};

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
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  .header-left { display: flex; align-items: center; gap: 12px; }
  .header-right { display: flex; align-items: center; gap: 10px; }
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

.stats-row {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
  margin-bottom: 16px;
}

.stat-card {
  background: $layer-2;
  border-radius: $border-radius-md;
  border: 1px solid rgba(0, 212, 255, 0.1);
  padding: 10px 14px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.stat-label {
  font-size: 11px;
  color: $text-muted;
}

.stat-value {
  font-size: 18px;
  font-weight: 600;
  color: $text-primary;
  font-family: 'Space Mono', monospace;
}

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
  &:hover { border-color: rgba(0, 212, 255, 0.3); color: $text-secondary; }
}

.search-panel {
  margin-bottom: 16px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  padding: 16px;
}
.search-panel-inner {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: 16px;
}
.search-field {
  .field-label { font-size: 12px; color: $text-muted; margin-bottom: 6px; display: block; }
}
.search-actions { display: flex; gap: 8px; }
.input-wrapper { display: flex; align-items: center; }
.custom-input, .custom-select { width: 200px; }
.text-secondary { color: $text-muted; }

.table-panel {
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  padding: 16px;
}
.code-link {
  color: $cyan-primary;
  cursor: pointer;
  &:hover { text-decoration: underline; }
}
.vendor-name-cell {
  display: flex;
  align-items: center;
  gap: 10px;
  .vendor-avatar {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: rgba(0, 212, 255, 0.2);
    color: $cyan-primary;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 14px;
    font-weight: 600;
    flex-shrink: 0;
  }
  .name { font-weight: 500; color: $text-primary; }
}
.contact-cell {
  display: flex;
  flex-direction: column;
  gap: 2px;
  .contact-name { font-size: 13px; color: $text-primary; }
  .contact-phone { font-size: 12px; color: $text-muted; }
}
.no-data {
  font-size: 12px;
  color: $text-muted;
}
.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-0 { background: rgba(255,255,255,0.1); color: $text-muted; }
  &.status-1 { background: rgba(255, 193, 7, 0.2); color: #ffc107; }
  &.status-2 { background: rgba(70, 191, 145, 0.2); color: #46BF91; }
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  margin-right: 4px;
  &:hover { text-decoration: underline; }
  &.action-btn--more { color: $text-muted; display: inline-flex; align-items: center; gap: 2px; }
}
.pagination-wrapper {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
:deep(.quantum-pagination) {
  --el-pagination-button-color: rgba(0, 212, 255, 0.8);
  --el-pagination-hover-color: #00D4FF;
}
:deep(.danger-item) { color: #C95745; }
</style>
