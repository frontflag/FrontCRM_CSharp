<template>
  <div class="dict-admin-page">
    <div class="page-header">
      <div class="header-text">
        <h2 class="page-title">{{ t('dictAdmin.title') }}</h2>
        <p class="page-sub">{{ t('dictAdmin.subtitle') }}</p>
      </div>
    </div>

    <el-card class="main-card" shadow="never">
      <el-form :inline="true" class="toolbar" @submit.prevent="loadList">
        <el-form-item :label="t('dictAdmin.filterCategory')">
          <el-select v-model="filters.bizKind" style="width: 140px" @change="onBizKindFilterChange">
            <el-option :label="t('dictAdmin.bizKindCustomer')" value="customer" />
            <el-option :label="t('dictAdmin.bizKindVendor')" value="vendor" />
            <el-option :label="t('dictAdmin.bizKindMaterial')" value="material" />
            <el-option :label="t('dictAdmin.bizKindLogistics')" value="logistics" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('dictAdmin.filterControlName')">
          <el-select
            v-model="filters.category"
            style="width: 240px"
            @change="onCategoryOrStatusFilterChange"
          >
            <el-option
              v-for="c in controlCategoryCodes"
              :key="c"
              :label="categoryLabel(c)"
              :value="c"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('dictAdmin.filterStatus')">
          <el-select v-model="filters.status" style="width: 140px" @change="onCategoryOrStatusFilterChange">
            <el-option :label="t('dictAdmin.statusAll')" value="all" />
            <el-option :label="t('dictAdmin.statusActive')" value="active" />
            <el-option :label="t('dictAdmin.statusInactive')" value="inactive" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="loading" @click="loadList">{{ t('dictAdmin.query') }}</el-button>
          <el-button @click="resetFilters">{{ t('dictAdmin.reset') }}</el-button>
          <el-button type="success" plain @click="openAdd">{{ t('dictAdmin.add') }}</el-button>
        </el-form-item>
      </el-form>

      <p class="hint-muted">{{ t('dictAdmin.hintNoDelete') }}</p>
      <p v-if="dragColumnVisible" class="hint-muted hint-drag">{{ t('dictAdmin.hintDragActive') }}</p>

      <el-table
        ref="tableRef"
        v-loading="loading || reorderSaving"
        row-key="id"
        :data="rows"
        stripe
        class="data-table"
        empty-text="—"
      >
        <el-table-column
          v-if="dragColumnVisible"
          :label="t('dictAdmin.colDrag')"
          width="52"
          align="center"
          class-name="dict-drag-col"
        >
          <template #default>
            <span class="dict-drag-handle dict-drag-handle--active" :title="t('dictAdmin.colDrag')">
              <span class="dict-drag-bars" aria-hidden="true">
                <span class="dict-drag-bar"></span>
                <span class="dict-drag-bar"></span>
                <span class="dict-drag-bar"></span>
              </span>
            </span>
          </template>
        </el-table-column>
        <el-table-column :label="t('dictAdmin.colCategory')" min-width="160">
          <template #default="{ row }">
            <span :title="row.category">{{ categoryLabel(row.category) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="nameZh" :label="t('dictAdmin.colNameZh')" min-width="160" />
        <el-table-column prop="nameEn" :label="t('dictAdmin.colNameEn')" min-width="140">
          <template #default="{ row }">{{ row.nameEn || '—' }}</template>
        </el-table-column>
        <el-table-column prop="itemCode" :label="t('dictAdmin.colCode')" min-width="120" />
        <el-table-column prop="sortOrder" :label="t('dictAdmin.colSort')" width="100" />
        <el-table-column :label="t('dictAdmin.colActive')" width="100">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'" size="small">
              {{ row.isActive ? t('dictAdmin.statusActive') : t('dictAdmin.statusInactive') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('dictAdmin.colCreateTime')" min-width="170">
          <template #default="{ row }">{{ formatCreateTime(row.createTime) }}</template>
        </el-table-column>
        <el-table-column :label="t('dictAdmin.colActions')" width="100" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openEdit(row)">{{ t('dictAdmin.edit') }}</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div v-if="total > 0" class="pagination-wrap pagination-wrap--inline">
        <span class="total-only">{{ t('dictAdmin.totalRows', { n: total }) }}</span>
      </div>
    </el-card>

    <el-dialog
      v-model="dialogVisible"
      :title="dialogMode === 'add' ? t('dictAdmin.dialogAddTitle') : t('dictAdmin.dialogEditTitle')"
      width="520px"
      destroy-on-close
      @closed="onDialogClosed"
    >
      <el-form :model="dialogForm" label-width="120px">
        <template v-if="dialogMode === 'add'">
          <el-form-item :label="t('dictAdmin.filterCategory')">
            <el-input :model-value="bizKindLabel(dialogBizKind)" disabled />
          </el-form-item>
          <el-form-item :label="t('dictAdmin.filterControlName')">
            <el-input :model-value="categoryLabel(dialogForm.category)" :title="dialogForm.category" disabled />
          </el-form-item>
          <el-form-item :label="t('dictAdmin.fieldCode')">
            <el-input
              :model-value="dialogForm.itemCode"
              :placeholder="nextCodeLoading ? t('dictAdmin.nextCodeLoading') : ''"
              disabled
            />
            <div class="field-hint">{{ t('dictAdmin.fieldCodeAutoHint') }}</div>
          </el-form-item>
        </template>
        <template v-else>
          <el-form-item :label="t('dictAdmin.fieldCategory')">
            <el-input :model-value="categoryLabel(dialogForm.category)" :title="dialogForm.category" disabled />
          </el-form-item>
          <el-form-item :label="t('dictAdmin.fieldCode')">
            <el-input :model-value="dialogForm.itemCode" disabled />
            <div class="field-hint">{{ t('dictAdmin.fieldCodeReadonly') }}</div>
          </el-form-item>
        </template>
        <el-form-item :label="t('dictAdmin.fieldNameZh')" required>
          <el-input v-model="dialogForm.nameZh" />
        </el-form-item>
        <el-form-item :label="t('dictAdmin.fieldNameEn')">
          <el-input v-model="dialogForm.nameEn" />
        </el-form-item>
        <el-form-item :label="t('dictAdmin.fieldSort')">
          <el-input-number v-model="dialogForm.sortOrder" :step="1" controls-position="right" style="width: 100%" />
        </el-form-item>
        <el-form-item :label="t('dictAdmin.fieldActive')">
          <el-switch v-model="dialogForm.isActive" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('dictAdmin.cancel') }}</el-button>
        <el-button
          type="primary"
          :loading="saving"
          :disabled="dialogMode === 'add' && (nextCodeLoading || !dialogForm.itemCode.trim())"
          @click="submitDialog"
        >
          {{ t('dictAdmin.save') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted, onBeforeUnmount, nextTick } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import Sortable from 'sortablejs';
import type { SortableEvent } from 'sortablejs';
import { dictAdminApi, type SysDictItemAdminRow } from '@/api/dictAdmin';
import { formatDisplayDateTime } from '@/utils/displayDateTime';
import {
  DICT_CUSTOMER_CATEGORIES,
  DICT_VENDOR_CATEGORIES,
  DICT_MATERIAL_CATEGORIES,
  DICT_LOGISTICS_CATEGORIES,
  type DictBizKind
} from '@/constants/dictCategories';

const { t, te } = useI18n();

function categoryLabel(code: string | null | undefined) {
  const c = (code ?? '').trim();
  if (!c) return '—';
  const key = `dictAdmin.categories.${c}`;
  return te(key) ? String(t(key)) : c;
}

function bizKindLabel(bk: DictBizKind) {
  if (bk === 'customer') return String(t('dictAdmin.bizKindCustomer'));
  if (bk === 'vendor') return String(t('dictAdmin.bizKindVendor'));
  if (bk === 'material') return String(t('dictAdmin.bizKindMaterial'));
  return String(t('dictAdmin.bizKindLogistics'));
}

function firstCategoryForBiz(biz: DictBizKind): string {
  const list =
    biz === 'customer'
      ? DICT_CUSTOMER_CATEGORIES
      : biz === 'vendor'
        ? DICT_VENDOR_CATEGORIES
        : biz === 'material'
          ? DICT_MATERIAL_CATEGORIES
          : DICT_LOGISTICS_CATEGORIES;
  return list[0] ?? '';
}

const loading = ref(false);
const saving = ref(false);
const rows = ref<SysDictItemAdminRow[]>([]);
const total = ref(0);

const filters = reactive<{
  bizKind: DictBizKind;
  category: string;
  status: 'all' | 'active' | 'inactive';
}>({
  bizKind: 'customer',
  category: DICT_CUSTOMER_CATEGORIES[0] ?? '',
  status: 'all'
});

const controlCategoryCodes = computed(() => {
  if (filters.bizKind === 'customer') return [...DICT_CUSTOMER_CATEGORIES];
  if (filters.bizKind === 'vendor') return [...DICT_VENDOR_CATEGORIES];
  if (filters.bizKind === 'material') return [...DICT_MATERIAL_CATEGORIES];
  return [...DICT_LOGISTICS_CATEGORIES];
});

/** 控件名称必选：始终落在当前业务类型下列表中的某一项（默认第一项） */
watch(
  [() => filters.bizKind, () => controlCategoryCodes.value, () => filters.category],
  () => {
    const list = controlCategoryCodes.value as readonly string[];
    if (!list.length) {
      filters.category = '';
      return;
    }
    const v = (filters.category ?? '').trim();
    if (!v || !list.includes(v)) {
      filters.category = list[0]!;
    }
  },
  { immediate: true }
);

const dragColumnVisible = computed(() => filters.status === 'all');

const tableRef = ref<{ $el?: HTMLElement } | null>(null);
let sortableInst: Sortable | null = null;

function destroySortable() {
  sortableInst?.destroy();
  sortableInst = null;
}

function getTableTbody(): HTMLElement | null {
  const inst = tableRef.value as unknown as { $el?: HTMLElement } | null;
  const root = inst?.$el;
  return root?.querySelector?.('.el-table__body-wrapper tbody') ?? null;
}

function initSortable() {
  destroySortable();
  if (!dragColumnVisible.value || loading.value) return;
  const tbody = getTableTbody();
  if (!tbody || rows.value.length < 2) return;

  sortableInst = Sortable.create(tbody, {
    handle: '.dict-drag-handle--active',
    animation: 160,
    ghostClass: 'dict-sortable-ghost',
    onEnd: (evt: SortableEvent) => {
      const { oldIndex, newIndex } = evt;
      if (oldIndex == null || newIndex == null || oldIndex === newIndex) return;
      const next = [...rows.value];
      const [moved] = next.splice(oldIndex, 1);
      next.splice(newIndex, 0, moved);
      rows.value = next;
      next.forEach((r, i) => {
        r.sortOrder = i + 1;
      });
      void persistReorder();
    }
  });
}

const reorderSaving = ref(false);

async function persistReorder() {
  const cat = filters.category?.trim();
  if (!cat || !dragColumnVisible.value) return;
  if (reorderSaving.value) return;
  reorderSaving.value = true;
  try {
    await dictAdminApi.reorderItems({
      category: cat,
      orderedIds: rows.value.map((r) => r.id)
    });
    ElMessage.success(t('dictAdmin.reorderSuccess'));
    await loadList();
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : '';
    ElMessage.error(msg ? `${t('dictAdmin.reorderFailed')}: ${msg}` : t('dictAdmin.reorderFailed'));
    await loadList();
  } finally {
    reorderSaving.value = false;
  }
}

function onBizKindFilterChange() {
  filters.category = firstCategoryForBiz(filters.bizKind);
  void loadList();
}

function onCategoryOrStatusFilterChange() {
  void loadList();
}

const nextCodeLoading = ref(false);
const dialogVisible = ref(false);
const dialogMode = ref<'add' | 'edit'>('add');
const editingId = ref('');
const dialogBizKind = ref<DictBizKind>('customer');

const dialogForm = reactive({
  category: '',
  itemCode: '',
  nameZh: '',
  nameEn: '',
  sortOrder: 0,
  isActive: true
});

const formatCreateTime = (v?: string) => formatDisplayDateTime(v);

function activeFilterParam(): boolean | null | undefined {
  if (filters.status === 'active') return true;
  if (filters.status === 'inactive') return false;
  return undefined;
}

async function loadList() {
  destroySortable();
  loading.value = true;
  try {
    const cat = filters.category.trim();
    const data = await dictAdminApi.fetchItems({
      bizSegment: filters.bizKind,
      category: cat,
      isActive: activeFilterParam() ?? null,
      page: 1,
      pageSize: 500
    });
    rows.value = data.items ?? [];
    total.value = data.total ?? 0;
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : '';
    ElMessage.error(msg ? `${t('dictAdmin.loadListFailed')}: ${msg}` : t('dictAdmin.loadListFailed'));
    rows.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
    await nextTick();
    initSortable();
  }
}

function resetFilters() {
  filters.bizKind = 'customer';
  filters.category = firstCategoryForBiz('customer');
  filters.status = 'all';
  void loadList();
}

async function openAdd() {
  dialogMode.value = 'add';
  editingId.value = '';
  dialogBizKind.value = filters.bizKind;
  dialogForm.category = filters.category;
  dialogForm.itemCode = '';
  dialogForm.nameZh = '';
  dialogForm.nameEn = '';
  dialogForm.sortOrder = 0;
  dialogForm.isActive = true;
  dialogVisible.value = true;
  nextCodeLoading.value = true;
  try {
    dialogForm.itemCode = await dictAdminApi.getNextItemCode(filters.category);
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : '';
    ElMessage.error(msg ? `${t('dictAdmin.nextCodeFailed')}: ${msg}` : t('dictAdmin.nextCodeFailed'));
    dialogForm.itemCode = '';
  } finally {
    nextCodeLoading.value = false;
  }
}

function openEdit(row: SysDictItemAdminRow) {
  dialogMode.value = 'edit';
  editingId.value = row.id;
  dialogForm.category = row.category;
  dialogForm.itemCode = row.itemCode;
  dialogForm.nameZh = row.nameZh;
  dialogForm.nameEn = row.nameEn ?? '';
  dialogForm.sortOrder = row.sortOrder;
  dialogForm.isActive = row.isActive;
  dialogVisible.value = true;
}

function onDialogClosed() {
  editingId.value = '';
}

async function submitDialog() {
  const nameZh = dialogForm.nameZh?.trim();
  if (!nameZh) {
    ElMessage.warning(t('dictAdmin.validationNameZh'));
    return;
  }
  if (dialogMode.value === 'add') {
    const category = dialogForm.category?.trim();
    if (!category) {
      ElMessage.warning(t('dictAdmin.validationAddRequired'));
      return;
    }
    saving.value = true;
    try {
      await dictAdminApi.createItem({
        category,
        nameZh,
        nameEn: dialogForm.nameEn?.trim() || undefined,
        sortOrder: dialogForm.sortOrder,
        isActive: dialogForm.isActive
      });
      ElMessage.success(t('dictAdmin.saveSuccess'));
      dialogVisible.value = false;
      await loadList();
    } catch (e: any) {
      ElMessage.error(e?.message || t('dictAdmin.saveFailed'));
    } finally {
      saving.value = false;
    }
    return;
  }

  saving.value = true;
  try {
    await dictAdminApi.updateItem(editingId.value, {
      nameZh,
      nameEn: dialogForm.nameEn?.trim() || undefined,
      sortOrder: dialogForm.sortOrder,
      isActive: dialogForm.isActive
    });
    ElMessage.success(t('dictAdmin.saveSuccess'));
    dialogVisible.value = false;
    await loadList();
  } catch (e: any) {
    ElMessage.error(e?.message || t('dictAdmin.saveFailed'));
  } finally {
    saving.value = false;
  }
}

onMounted(() => {
  void loadList();
});

onBeforeUnmount(() => {
  destroySortable();
});
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.dict-admin-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
}

.page-header {
  margin-bottom: 20px;
  .page-title {
    margin: 0 0 6px;
    font-size: 20px;
    font-weight: 600;
    color: $text-primary;
  }
  .page-sub {
    margin: 0;
    font-size: 13px;
    color: $text-muted;
    max-width: 720px;
    line-height: 1.5;
  }
}

.main-card {
  background: rgba(255, 255, 255, 0.02);
  border: 1px solid $border-panel;
}

.toolbar {
  margin-bottom: 8px;
}

.hint-muted {
  font-size: 12px;
  color: $text-muted;
  margin: 0 0 12px;
}

.data-table {
  width: 100%;
}

.pagination-wrap {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}

.field-hint {
  font-size: 12px;
  color: $text-muted;
  margin-top: 4px;
}

.hint-drag {
  margin-top: -4px;
}

.dict-drag-handle {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: $text-muted;
  padding: 4px;
  user-select: none;
}

.dict-drag-handle--active {
  cursor: grab;

  &:active {
    cursor: grabbing;
  }

  &:hover {
    color: $cyan-primary;
  }
}

.dict-drag-bars {
  display: inline-flex;
  flex-direction: column;
  gap: 3px;
  width: 14px;
}

.dict-drag-bar {
  display: block;
  height: 2px;
  border-radius: 1px;
  background-color: currentColor;
}

.pagination-wrap--inline {
  justify-content: flex-start;
}

.total-only {
  font-size: 13px;
  color: $text-muted;
}

:deep(.dict-sortable-ghost) {
  opacity: 0.45;
  background: rgba(0, 212, 255, 0.08);
}

:deep(tr.dict-sortable-ghost td) {
  border-color: rgba(0, 212, 255, 0.25);
}
</style>
