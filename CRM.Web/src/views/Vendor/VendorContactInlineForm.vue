<template>
  <!-- 内嵌供应商联系人表单面板，替代弹窗 -->
  <Transition name="panel-slide">
    <div v-if="modelValue" class="contact-inline-panel">
      <!-- 面板头部 -->
      <div class="panel-header">
        <div class="panel-title">
          <span class="panel-title-icon">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
              <circle cx="12" cy="7" r="4"/>
            </svg>
          </span>
          {{ mode === 'create' ? '新增联系人' : '编辑联系人' }}
        </div>
        <button class="panel-close-btn" @click="handleClose" title="关闭">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
          </svg>
        </button>
      </div>

      <!-- 表单内容 -->
      <div class="panel-body">
        <el-form :model="form" label-width="80px" label-position="left" size="small" :disabled="loading">
          <div class="form-grid">
            <!-- 姓名 + 职位 -->
            <el-form-item label="姓名" class="form-item">
              <el-input v-model="form.cName" placeholder="联系人姓名" />
            </el-form-item>
            <el-form-item label="职位" class="form-item">
              <el-input v-model="form.title" placeholder="职位/角色" />
            </el-form-item>
            <!-- 部门 + 手机 -->
            <el-form-item label="部门" class="form-item">
              <el-input v-model="form.department" placeholder="所属部门" />
            </el-form-item>
            <el-form-item label="手机" class="form-item">
              <el-input v-model="form.mobile" placeholder="手机号" />
            </el-form-item>
            <!-- 电话 + 邮箱 -->
            <el-form-item label="电话" class="form-item">
              <el-input v-model="form.tel" placeholder="座机电话" />
            </el-form-item>
            <el-form-item label="邮箱" class="form-item">
              <el-input v-model="form.email" placeholder="邮箱" />
            </el-form-item>
            <!-- 主联系人 跨两列 -->
            <el-form-item label="主联系人" class="form-item form-item--full form-item--switch">
              <el-switch v-model="form.isMain" />
              <span class="switch-hint">{{ form.isMain ? '设为默认联系人' : '普通联系人' }}</span>
            </el-form-item>
            <!-- 备注 跨两列 -->
            <el-form-item label="备注" class="form-item form-item--full">
              <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="补充说明信息" />
            </el-form-item>
          </div>
        </el-form>
      </div>

      <!-- 面板底部操作 -->
      <div class="panel-footer">
        <button class="panel-btn panel-btn--cancel" @click="handleClose">取消</button>
        <button class="panel-btn panel-btn--submit" :disabled="loading" @click="handleConfirm">
          <span v-if="loading" class="btn-loading">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="spin">
              <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
            </svg>
          </span>
          {{ loading ? '保存中...' : (mode === 'create' ? '确认添加' : '保存修改') }}
        </button>
      </div>
    </div>
  </Transition>
</template>

<script setup lang="ts">
import { reactive, watch } from 'vue';
import type { VendorContactInfo, AddVendorContactRequest, UpdateVendorContactRequest } from '@/types/vendor';

const props = defineProps<{
  modelValue: boolean;
  mode: 'create' | 'edit';
  contact?: VendorContactInfo | null;
  loading?: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
  (e: 'confirm', payload: AddVendorContactRequest | UpdateVendorContactRequest): void;
}>();

const form = reactive<AddVendorContactRequest & { id?: string }>({
  cName: '', title: '', department: '',
  mobile: '', tel: '', email: '',
  isMain: false, remark: ''
});

watch(
  () => props.contact,
  (val) => {
    if (props.mode === 'edit' && val) {
      form.cName = val.cName || '';
      form.title = val.title || '';
      form.department = val.department || '';
      form.mobile = val.mobile || '';
      form.tel = val.tel || '';
      form.email = val.email || '';
      form.isMain = !!val.isMain;
      form.remark = val.remark || '';
      form.id = val.id;
    } else if (props.mode === 'create') {
      form.cName = ''; form.title = ''; form.department = '';
      form.mobile = ''; form.tel = ''; form.email = '';
      form.isMain = false; form.remark = '';
      form.id = undefined;
    }
  },
  { immediate: true }
);

const handleClose = () => {
  emit('update:modelValue', false);
};

const handleConfirm = () => {
  emit('confirm', { ...form });
};
</script>

<style lang="scss" scoped>
$cyan: #00D4FF;
$layer-3: #162233;
$text-primary: rgba(224, 244, 255, 0.92);
$text-muted: rgba(130, 170, 200, 0.6);

/* ── 过渡动画 ── */
.panel-slide-enter-active,
.panel-slide-leave-active {
  transition: all 0.28s cubic-bezier(0.4, 0, 0.2, 1);
  overflow: hidden;
}
.panel-slide-enter-from,
.panel-slide-leave-to {
  max-height: 0;
  opacity: 0;
  transform: translateY(-8px);
}
.panel-slide-enter-to,
.panel-slide-leave-from {
  max-height: 500px;
  opacity: 1;
  transform: translateY(0);
}

/* ── 面板容器 ── */
.contact-inline-panel {
  margin: 0 0 16px 0;
  background: $layer-3;
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3), inset 0 1px 0 rgba(0, 212, 255, 0.08);
}

/* ── 面板头部 ── */
.panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 16px;
  background: rgba(0, 212, 255, 0.06);
  border-bottom: 1px solid rgba(0, 212, 255, 0.12);
}

.panel-title {
  display: flex;
  align-items: center;
  gap: 7px;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  font-weight: 600;
  color: rgba(0, 212, 255, 0.9);
  letter-spacing: 0.3px;
}

.panel-title-icon {
  display: flex;
  align-items: center;
  color: $cyan;
}

.panel-close-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  background: transparent;
  border: 1px solid rgba(0, 212, 255, 0.15);
  border-radius: 4px;
  color: $text-muted;
  cursor: pointer;
  transition: all 0.15s;

  &:hover {
    background: rgba(201, 87, 69, 0.12);
    border-color: rgba(201, 87, 69, 0.3);
    color: #C95745;
  }
}

/* ── 表单区域 ── */
.panel-body {
  padding: 16px 20px 8px;

  :deep(.el-form-item) { margin-bottom: 12px; }

  :deep(.el-form-item__label) {
    font-family: 'Noto Sans SC', sans-serif;
    font-size: 12.5px;
    color: rgba(130, 170, 200, 0.7);
    padding-right: 8px;
  }

  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner) {
    background: rgba(0, 212, 255, 0.04);
    border-color: rgba(0, 212, 255, 0.15);
    box-shadow: none;
    color: $text-primary;
    &:hover { border-color: rgba(0, 212, 255, 0.3); }
    &.is-focus { border-color: rgba(0, 212, 255, 0.5); background: rgba(0, 212, 255, 0.06); }
  }

  :deep(.el-input__inner),
  :deep(.el-textarea__inner) {
    color: $text-primary;
    font-family: 'Noto Sans SC', sans-serif;
    font-size: 13px;
    background: transparent;
  }
}

/* ── 两列网格布局 ── */
.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  column-gap: 24px;
}

.form-item--full { grid-column: 1 / -1; }

.form-item--switch {
  :deep(.el-form-item__content) {
    display: flex;
    align-items: center;
    gap: 10px;
  }
}

.switch-hint {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 12px;
  color: $text-muted;
}

/* ── 底部操作栏 ── */
.panel-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
  padding: 10px 20px 12px;
  border-top: 1px solid rgba(0, 212, 255, 0.08);
}

.panel-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 18px;
  border-radius: 5px;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.15s;

  &--cancel {
    background: transparent;
    border: 1px solid rgba(0, 212, 255, 0.2);
    color: rgba(130, 170, 200, 0.8);
    &:hover { border-color: rgba(0, 212, 255, 0.4); color: $text-primary; }
  }

  &--submit {
    background: linear-gradient(135deg, rgba(0, 102, 255, 0.7), rgba(0, 212, 255, 0.6));
    border: 1px solid rgba(0, 212, 255, 0.4);
    color: #fff;
    font-weight: 500;
    &:hover { background: linear-gradient(135deg, rgba(0, 102, 255, 0.9), rgba(0, 212, 255, 0.8)); }
    &:disabled { opacity: 0.6; cursor: not-allowed; }
  }
}

.btn-loading { display: flex; align-items: center; }
.spin { animation: spin 0.8s linear infinite; }
@keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
</style>
