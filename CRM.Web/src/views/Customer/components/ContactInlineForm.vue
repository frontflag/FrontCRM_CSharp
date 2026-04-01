<template>
  <!-- 内嵌联系人表单面板，替代弹窗 -->
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
          {{ isEdit ? '编辑联系人' : '新增联系人' }}
        </div>
        <button class="panel-close-btn" @click="handleCancel" title="关闭">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
          </svg>
        </button>
      </div>

      <!-- 表单内容 -->
      <div class="panel-body">
        <el-form
          ref="formRef"
          :model="formData"
          :rules="formRules"
          label-width="80px"
          label-position="left"
          size="small"
        >
          <div class="form-grid">
            <!-- 姓名 + 性别 -->
            <el-form-item label="姓名" prop="contactName" class="form-item">
              <el-input v-model="formData.contactName" placeholder="请输入姓名" />
            </el-form-item>
            <el-form-item label="性别" class="form-item">
              <el-radio-group v-model="formData.gender">
                <el-radio :label="0">男</el-radio>
                <el-radio :label="1">女</el-radio>
                <el-radio :label="2">保密</el-radio>
              </el-radio-group>
            </el-form-item>
            <!-- 部门 + 职位 -->
            <el-form-item label="部门" class="form-item">
              <el-input v-model="formData.department" placeholder="请输入部门" />
            </el-form-item>
            <el-form-item label="职位" class="form-item">
              <el-input v-model="formData.position" placeholder="请输入职位" />
            </el-form-item>
            <!-- 手机 + 固话 -->
            <el-form-item label="手机" prop="mobilePhone" class="form-item">
              <el-input v-model="formData.mobilePhone" placeholder="请输入手机号码" />
            </el-form-item>
            <el-form-item label="固话" class="form-item">
              <el-input v-model="formData.phone" placeholder="请输入固话号码" />
            </el-form-item>
            <!-- 邮箱 + 传真 -->
            <el-form-item label="邮箱" prop="email" class="form-item">
              <el-input v-model="formData.email" placeholder="请输入邮箱地址" />
            </el-form-item>
            <el-form-item label="传真" class="form-item">
              <el-input v-model="formData.fax" placeholder="请输入传真号码" />
            </el-form-item>
            <!-- QQ/微信 跨两列 -->
            <el-form-item label="QQ/微信" class="form-item form-item--full">
              <el-input v-model="formData.socialAccount" placeholder="请输入QQ或微信号" />
            </el-form-item>
            <!-- 备注 跨两列 -->
            <el-form-item label="备注" class="form-item form-item--full">
              <el-input
                v-model="formData.remarks"
                type="textarea"
                :rows="2"
                placeholder="请输入备注信息"
              />
            </el-form-item>
            <!-- 复选框 跨两列 -->
            <el-form-item class="form-item form-item--full form-item--checks">
              <el-checkbox v-model="formData.isDefault">设为默认联系人</el-checkbox>
              <el-checkbox v-model="formData.isDecisionMaker">是决策人</el-checkbox>
            </el-form-item>
          </div>
        </el-form>
      </div>

      <!-- 面板底部操作 -->
      <div class="panel-footer">
        <button class="panel-btn panel-btn--cancel" @click="handleCancel">取消</button>
        <button class="panel-btn panel-btn--submit" :disabled="submitting" @click="handleSubmit">
          <span v-if="submitting" class="btn-loading">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="spin">
              <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
            </svg>
          </span>
          {{ submitting ? '保存中...' : (isEdit ? '保存修改' : '确认添加') }}
        </button>
      </div>
    </div>
  </Transition>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { ElNotification, type FormInstance, type FormRules } from 'element-plus';
import { customerContactApi } from '@/api/customer';
import type { CustomerContactInfo, CreateContactRequest } from '@/types/customer';

const props = defineProps<{
  modelValue: boolean;
  customerId: string;
  contact?: CustomerContactInfo;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
  (e: 'success'): void;
}>();

const formRef = ref<FormInstance>();
const submitting = ref(false);
const isEdit = computed(() => !!props.contact);

const formData = ref<CreateContactRequest>({
  contactName: '',
  gender: 0,
  department: '',
  position: '',
  mobilePhone: '',
  phone: '',
  email: '',
  fax: '',
  socialAccount: '',
  isDefault: false,
  isDecisionMaker: false,
  remarks: ''
});

const mobilePhonePattern = /^1[3-9]\d{9}$/;
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

const formRules: FormRules = {
  contactName: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  mobilePhone: [
    { required: true, message: '请输入手机号码', trigger: 'blur' },
    {
      validator: (_rule: unknown, value: string, callback: (error?: Error) => void) => {
        const v = (value || '').trim();
        if (v === '-') {
          callback();
          return;
        }
        if (!mobilePhonePattern.test(v)) {
          callback(new Error('请输入正确的手机号码'));
          return;
        }
        callback();
      },
      trigger: 'blur'
    }
  ],
  email: [
    {
      validator: (_rule: unknown, value: string, callback: (error?: Error) => void) => {
        const v = (value || '').trim();
        if (!v || v === '-') {
          callback();
          return;
        }
        if (!emailPattern.test(v)) {
          callback(new Error('请输入正确的邮箱地址'));
          return;
        }
        callback();
      },
      trigger: 'blur'
    }
  ]
};

const resetForm = () => {
  formData.value = {
    contactName: '', gender: 0, department: '', position: '',
    mobilePhone: '', phone: '', email: '', fax: '',
    socialAccount: '', isDefault: false, isDecisionMaker: false, remarks: ''
  };
  formRef.value?.resetFields();
};

watch(() => props.contact, (newVal) => {
  if (newVal) {
    formData.value = {
      contactName: newVal.contactName,
      gender: newVal.gender,
      department: newVal.department || '',
      position: newVal.position || '',
      mobilePhone: newVal.mobilePhone,
      phone: newVal.phone || '',
      email: newVal.email || '',
      fax: newVal.fax || '',
      socialAccount: newVal.socialAccount || '',
      isDefault: newVal.isDefault,
      isDecisionMaker: newVal.isDecisionMaker,
      remarks: newVal.remarks || ''
    };
  } else {
    resetForm();
  }
}, { immediate: true });

const handleCancel = () => {
  resetForm();
  emit('update:modelValue', false);
};

const handleSubmit = async () => {
  const valid = await formRef.value?.validate().catch(() => false);
  if (!valid) return;
  submitting.value = true;
  try {
    if (isEdit.value && props.contact) {
      await customerContactApi.updateContact(props.contact.id, formData.value);
      ElNotification.success({ title: '保存成功', message: '联系人信息已更新' });
    } else {
      await customerContactApi.createContact(props.customerId, formData.value);
      ElNotification.success({ title: '添加成功', message: '联系人已添加' });
    }
    emit('success');
    emit('update:modelValue', false);
  } catch (error) {
    console.error('保存失败:', error);
    ElNotification.error({ title: '保存失败', message: '联系人保存失败，请稍后重试' });
  } finally {
    submitting.value = false;
  }
};
</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';

$cyan: $cyan-primary;

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
  max-height: 600px;
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

  :deep(.el-form-item) {
    margin-bottom: 12px;
  }

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
    font-family: 'Noto Sans SC', sans-serif;
    font-size: 13px;
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

  :deep(.el-radio__label) {
    font-family: 'Noto Sans SC', sans-serif;
    font-size: 12.5px;
    color: rgba(180, 210, 230, 0.8);
  }

  :deep(.el-radio__input.is-checked .el-radio__inner) {
    background: $cyan;
    border-color: $cyan;
  }

  :deep(.el-checkbox__label) {
    font-family: 'Noto Sans SC', sans-serif;
    font-size: 12.5px;
    color: rgba(180, 210, 230, 0.8);
  }

  :deep(.el-checkbox__input.is-checked .el-checkbox__inner) {
    background: $cyan;
    border-color: $cyan;
  }
}

/* ── 两列网格布局 ── */
.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  column-gap: 24px;
  row-gap: 0;
}

.form-item {
  /* 默认单列 */
}

.form-item--full {
  grid-column: 1 / -1;
}

.form-item--checks {
  :deep(.el-form-item__content) {
    display: flex;
    gap: 20px;
  }
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

/* ── 加载旋转 ── */
.btn-loading { display: flex; align-items: center; }
.spin { animation: spin 0.8s linear infinite; }
@keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
</style>
