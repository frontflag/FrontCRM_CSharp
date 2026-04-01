<template>
  <div class="contact-edit-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <button class="back-btn" @click="handleBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
          返回
        </button>
        <div class="header-breadcrumb">
          <span class="breadcrumb-item" @click="handleBack">{{ customerName }}</span>
          <span class="breadcrumb-sep">›</span>
          <span class="breadcrumb-item breadcrumb-item--active">{{ isEdit ? '编辑联系人' : '新增联系人' }}</span>
        </div>
      </div>
    </div>

    <!-- 表单卡片 -->
    <div class="form-card">
      <div class="form-card-header">
        <div class="form-card-title">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
            <circle cx="12" cy="7" r="4"/>
          </svg>
          {{ isEdit ? '编辑联系人' : '新增联系人' }}
        </div>
        <div v-if="isEdit && contactId" class="form-card-subtitle">联系人 ID：{{ contactId }}</div>
      </div>

      <div class="form-card-body" v-loading="pageLoading">
        <el-form
          ref="formRef"
          :model="formData"
          :rules="formRules"
          label-width="100px"
          label-position="left"
        >
          <!-- 基本信息 -->
          <div class="section-title">基本信息</div>
          <el-row :gutter="32">
            <el-col :span="12">
              <el-form-item label="姓名" prop="contactName">
                <el-input v-model="formData.contactName" placeholder="请输入姓名" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="性别">
                <el-radio-group v-model="formData.gender">
                  <el-radio :label="0">男</el-radio>
                  <el-radio :label="1">女</el-radio>
                  <el-radio :label="2">保密</el-radio>
                </el-radio-group>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="32">
            <el-col :span="12">
              <el-form-item label="部门">
                <el-input v-model="formData.department" placeholder="请输入部门" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="职位">
                <el-input v-model="formData.position" placeholder="请输入职位" />
              </el-form-item>
            </el-col>
          </el-row>

          <!-- 联系方式 -->
          <div class="section-title">联系方式</div>
          <el-row :gutter="32">
            <el-col :span="12">
              <el-form-item label="手机" prop="mobilePhone">
                <el-input v-model="formData.mobilePhone" placeholder="请输入手机号码" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="固话">
                <el-input v-model="formData.phone" placeholder="请输入固话号码" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="32">
            <el-col :span="12">
              <el-form-item label="邮箱" prop="email">
                <el-input v-model="formData.email" placeholder="请输入邮箱地址" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="传真">
                <el-input v-model="formData.fax" placeholder="请输入传真号码" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="32">
            <el-col :span="12">
              <el-form-item label="QQ/微信">
                <el-input v-model="formData.socialAccount" placeholder="请输入QQ或微信号" />
              </el-form-item>
            </el-col>
          </el-row>

          <!-- 其他设置 -->
          <div class="section-title">其他设置</div>
          <el-row :gutter="32">
            <el-col :span="24">
              <el-form-item label="备注">
                <el-input
                  v-model="formData.remarks"
                  type="textarea"
                  :rows="3"
                  placeholder="请输入备注信息"
                />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="32">
            <el-col :span="24">
              <el-form-item label=" ">
                <el-checkbox v-model="formData.isDefault">设为默认联系人</el-checkbox>
                <el-checkbox v-model="formData.isDecisionMaker" style="margin-left: 24px;">是决策人</el-checkbox>
              </el-form-item>
            </el-col>
          </el-row>
        </el-form>

        <!-- 名片上传区域 -->
        <div class="section-title" style="margin-top: 8px;">名片</div>
        <BusinessCardUploader
          biz-type="contact"
          :biz-id="savedContactId || undefined"
          :max-cards="10"
        />
        <p v-if="!savedContactId && !isEdit" class="bc-hint">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
          </svg>
          保存联系人后可上传名片
        </p>
      </div>

      <!-- 底部操作 -->
      <div class="form-card-footer">
        <button class="footer-btn footer-btn--cancel" @click="handleBack">取消</button>
        <button
          class="footer-btn"
          :class="isEdit ? 'footer-btn--submit' : 'footer-btn--create'"
          :disabled="submitting"
          @click="handleSubmit"
        >
          <svg v-if="submitting" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="spin">
            <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
          </svg>
          {{ submitting ? '保存中...' : (isEdit ? '保存修改' : '确认添加') }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import BusinessCardUploader from '@/components/Contact/BusinessCardUploader.vue';
import { useRoute, useRouter } from 'vue-router';
import { ElNotification, type FormInstance, type FormRules } from 'element-plus';
import { customerContactApi, customerApi } from '@/api/customer';
import type { CreateContactRequest } from '@/types/customer';

const route = useRoute();
const router = useRouter();

const customerId = route.params.id as string;
const contactId = route.params.contactId as string | undefined;
const isEdit = computed(() => !!contactId);

const customerName = ref('客户详情');
const pageLoading = ref(false);
const submitting = ref(false);
const formRef = ref<FormInstance>();
// 新建成功后保存联系人 ID，用于名片上传
const savedContactId = ref<string | null>(contactId || null);

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

onMounted(async () => {
  pageLoading.value = true;
  try {
    // 加载客户名称用于面包屑
    const customer = await customerApi.getCustomerById(customerId);
    customerName.value = customer.customerName || '客户详情';

    // 编辑模式：加载联系人数据
    if (isEdit.value && contactId) {
      const detail = await customerApi.getCustomerById(customerId);
      const contact = detail.contacts?.find((c: any) => c.id === contactId);
      if (contact) {
        formData.value = {
          contactName: contact.contactName,
          gender: contact.gender,
          department: contact.department || '',
          position: contact.position || '',
          mobilePhone: contact.mobilePhone,
          phone: contact.phone || '',
          email: contact.email || '',
          fax: contact.fax || '',
          socialAccount: contact.socialAccount || '',
          isDefault: contact.isDefault,
          isDecisionMaker: contact.isDecisionMaker,
          remarks: contact.remarks || ''
        };
      }
    }
  } catch (e) {
    console.error('加载数据失败', e);
  } finally {
    pageLoading.value = false;
  }
});

const handleBack = () => {
  router.push({ name: 'CustomerDetail', params: { id: customerId }, query: { tab: 'contacts' } });
};

const handleSubmit = async () => {
  const valid = await formRef.value?.validate().catch(() => false);
  if (!valid) return;
  submitting.value = true;
  try {
    if (isEdit.value && contactId) {
      await customerContactApi.updateContact(contactId, formData.value);
      ElNotification.success({ title: '保存成功', message: '联系人信息已更新' });
      handleBack();
    } else {
      const created = await customerContactApi.createContact(customerId, formData.value);
      // 保存联系人 ID 供名片上传使用
      const newId = (created as any)?.id || (created as any)?.data?.id;
      if (newId) savedContactId.value = newId;
      ElNotification.success({ title: '添加成功', message: '联系人已添加，可继续上传名片' });
      // 新建成功后不立即跳转，等待用户上传名片（可选）
      // 如果没有 newId 则直接返回
      if (!newId) handleBack();
    }
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
$bg-page: $layer-1;
$bg-card: $layer-2;
$bg-card-header: $layer-2;
$border: $border-card;

.contact-edit-page {
  min-height: 100vh;
  background: $bg-page;
  padding: 0 0 40px;
}

/* ── 页面头部 ── */
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 28px;
  background: $bg-card-header;
  border-bottom: 1px solid $border-panel;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.back-btn {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 5px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: 5px;
  color: $text-secondary;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.15s;

  &:hover {
    border-color: rgba(0, 212, 255, 0.35);
    color: $text-primary;
    background: rgba(0, 212, 255, 0.08);
  }
}

.header-breadcrumb {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
}

.breadcrumb-item {
  color: $text-secondary;
  cursor: pointer;
  transition: color 0.15s;

  &:hover { color: $cyan; }
  &--active { color: $text-primary; cursor: default; font-weight: 500; }
}

.breadcrumb-sep {
  color: rgba(130, 170, 200, 0.35);
  font-size: 12px;
}

/* ── 表单卡片 ── */
.form-card {
  margin: 24px 28px 0;
  background: $bg-card;
  border: 1px solid $border;
  border-radius: 10px;
  overflow: hidden;
  box-shadow: $shadow-sm;
}

.form-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 24px;
  background: rgba(0, 212, 255, 0.06);
  border-bottom: 1px solid $border-panel;
}

.form-card-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 600;
  color: rgba(0, 212, 255, 0.9);
}

.form-card-subtitle {
  font-size: 12px;
  color: $text-secondary;
  font-family: monospace;
}

.form-card-body {
  padding: 24px 28px 8px;

  :deep(.el-form-item) { margin-bottom: 18px; }

  :deep(.el-form-item__label) {
    font-size: 13px;
    color: $text-secondary;
  }

  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner) {
    background: $layer-3;
    border-color: $border-panel;
    box-shadow: none;
    color: $text-primary;

    &:hover { border-color: rgba(0, 212, 255, 0.3); }
    &.is-focus { border-color: rgba(0, 212, 255, 0.5); background: rgba(0, 212, 255, 0.06); }
  }

  :deep(.el-input__inner),
  :deep(.el-textarea__inner) {
    color: $text-primary;
    font-size: 13.5px;
    background: transparent;
  }

  :deep(.el-radio__label) {
    font-size: 13px;
    color: rgba(180, 210, 230, 0.8);
  }

  :deep(.el-radio__input.is-checked .el-radio__inner) {
    background: $cyan;
    border-color: $cyan;
  }

  :deep(.el-checkbox__label) {
    font-size: 13px;
    color: rgba(180, 210, 230, 0.8);
  }

  :deep(.el-checkbox__input.is-checked .el-checkbox__inner) {
    background: $cyan;
    border-color: $cyan;
  }
}

/* ── 分组标题 ── */
.section-title {
  font-size: 12px;
  font-weight: 600;
  color: rgba(0, 212, 255, 0.6);
  text-transform: uppercase;
  letter-spacing: 1px;
  padding: 0 0 10px;
  margin: 8px 0 4px;
  border-bottom: 1px solid rgba(0, 212, 255, 0.08);
}

/* ── 底部操作 ── */
.form-card-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 28px;
  border-top: 1px solid $border-panel;
  background: rgba(0, 212, 255, 0.03);
}

.footer-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 24px;
  border-radius: 6px;
  font-size: 13.5px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s;

  &--cancel {
    background: transparent;
    border: 1px solid rgba(0, 212, 255, 0.2);
    color: $text-secondary;
    &:hover { border-color: rgba(0, 212, 255, 0.4); color: $text-primary; }
  }

  &--submit {
    background: linear-gradient(135deg, rgba(0, 102, 255, 0.75), rgba(0, 212, 255, 0.65));
    border: 1px solid rgba(0, 212, 255, 0.4);
    color: #fff;
    min-width: 110px;
    justify-content: center;
    &:hover { background: linear-gradient(135deg, rgba(0, 102, 255, 0.95), rgba(0, 212, 255, 0.85)); }
    &:disabled { opacity: 0.6; cursor: not-allowed; }
  }

  // 新增联系人（UI 规范：success 绿）
  &--create {
    background: linear-gradient(135deg, rgba(46, 160, 67, 0.88), rgba(70, 191, 145, 0.78));
    border: 1px solid rgba(70, 191, 145, 0.45);
    color: #fff;
    min-width: 110px;
    justify-content: center;
    &:hover {
      background: linear-gradient(135deg, rgba(46, 160, 67, 0.98), rgba(70, 191, 145, 0.9));
    }
    &:disabled { opacity: 0.6; cursor: not-allowed; }
  }
}

.spin { animation: spin 0.8s linear infinite; }
@keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }

.bc-hint {
  display: flex;
  align-items: center;
  gap: 5px;
  margin-top: 6px;
  font-size: 12px;
  color: rgba(130, 170, 200, 0.5);
  padding: 0 2px;
}
</style>
