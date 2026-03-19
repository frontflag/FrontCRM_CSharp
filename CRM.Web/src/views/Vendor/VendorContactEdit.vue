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
          <span class="breadcrumb-item" @click="handleBack">{{ vendorName }}</span>
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
          label-width="100px"
          label-position="left"
        >
          <!-- 基本信息 -->
          <div class="section-title">基本信息</div>
          <el-row :gutter="32">
            <el-col :span="12">
              <el-form-item label="姓名">
                <el-input v-model="formData.cName" placeholder="联系人姓名" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="职位">
                <el-input v-model="formData.title" placeholder="职位/角色" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="32">
            <el-col :span="12">
              <el-form-item label="部门">
                <el-input v-model="formData.department" placeholder="所属部门" />
              </el-form-item>
            </el-col>
          </el-row>

          <!-- 联系方式 -->
          <div class="section-title">联系方式</div>
          <el-row :gutter="32">
            <el-col :span="12">
              <el-form-item label="手机">
                <el-input v-model="formData.mobile" placeholder="手机号" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="电话">
                <el-input v-model="formData.tel" placeholder="座机电话" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="32">
            <el-col :span="12">
              <el-form-item label="邮箱">
                <el-input v-model="formData.email" placeholder="邮箱" />
              </el-form-item>
            </el-col>
          </el-row>

          <!-- 其他设置 -->
          <div class="section-title">其他设置</div>
          <el-row :gutter="32">
            <el-col :span="12">
              <el-form-item label="主联系人">
                <el-switch v-model="formData.isMain" />
                <span class="switch-hint">{{ formData.isMain ? '设为主联系人' : '普通联系人' }}</span>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="32">
            <el-col :span="24">
              <el-form-item label="备注">
                <el-input
                  v-model="formData.remark"
                  type="textarea"
                  :rows="3"
                  placeholder="补充说明信息"
                />
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
        <button class="footer-btn footer-btn--submit" :disabled="submitting" @click="handleSubmit">
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
import { ref, computed, onMounted, reactive } from 'vue';
import BusinessCardUploader from '@/components/Contact/BusinessCardUploader.vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { vendorApi, vendorContactApi } from '@/api/vendor';
import type { AddVendorContactRequest, UpdateVendorContactRequest } from '@/types/vendor';

const route = useRoute();
const router = useRouter();

const vendorId = route.params.id as string;
const contactId = route.params.contactId as string | undefined;
const isEdit = computed(() => !!contactId);

const vendorName = ref('供应商详情');
const pageLoading = ref(false);
const submitting = ref(false);
// 新建成功后保存联系人 ID，用于名片上传
const savedContactId = ref<string | null>(contactId || null);

const formData = reactive<AddVendorContactRequest & { id?: string }>({
  cName: '', title: '', department: '',
  mobile: '', tel: '', email: '',
  isMain: false, remark: ''
});

onMounted(async () => {
  pageLoading.value = true;
  try {
    const vendor = await vendorApi.getVendorById(vendorId);
    vendorName.value = vendor.officialName || vendor.nickName || vendor.code || '供应商详情';

    if (isEdit.value && contactId) {
      const contacts = vendor.contacts || [];
      const contact = contacts.find((c: any) => c.id === contactId);
      if (contact) {
        formData.cName = contact.cName || '';
        formData.title = contact.title || '';
        formData.department = contact.department || '';
        formData.mobile = contact.mobile || '';
        formData.tel = contact.tel || '';
        formData.email = contact.email || '';
        formData.isMain = !!contact.isMain;
        formData.remark = contact.remark || '';
        formData.id = contact.id;
      }
    }
  } catch (e) {
    console.error('加载数据失败', e);
  } finally {
    pageLoading.value = false;
  }
});

const handleBack = () => {
  router.push({ name: 'VendorDetail', params: { id: vendorId }, query: { tab: 'contacts' } });
};

const handleSubmit = async () => {
  submitting.value = true;
  try {
    if (isEdit.value && contactId) {
      await vendorContactApi.updateContact(contactId, formData as UpdateVendorContactRequest);
      ElMessage.success('联系人已更新');
      handleBack();
    } else {
      const created = await vendorContactApi.createContact(vendorId, formData as AddVendorContactRequest);
      const newId = (created as any)?.id || (created as any)?.data?.id;
      if (newId) savedContactId.value = newId;
      ElMessage.success('联系人已新增，可继续上传名片');
      if (!newId) handleBack();
    }
  } catch (error) {
    console.error('保存失败:', error);
    ElMessage.error('保存联系人失败');
  } finally {
    submitting.value = false;
  }
};
</script>

<style lang="scss" scoped>
$cyan: #00D4FF;
$bg-page: #071525;
$bg-card: #0d1e35;
$bg-card-header: #0a1929;
$border: rgba(0, 212, 255, 0.15);
$text-primary: rgba(224, 244, 255, 0.92);
$text-secondary: rgba(130, 170, 200, 0.7);

.contact-edit-page {
  min-height: 100vh;
  background: $bg-page;
  padding: 0 0 40px;
}

.page-header {
  display: flex;
  align-items: center;
  padding: 16px 28px;
  background: $bg-card-header;
  border-bottom: 1px solid rgba(0, 212, 255, 0.08);
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
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 5px;
  color: $text-secondary;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.15s;
  &:hover { border-color: rgba(0, 212, 255, 0.4); color: $text-primary; background: rgba(0, 212, 255, 0.05); }
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

.breadcrumb-sep { color: rgba(130, 170, 200, 0.35); font-size: 12px; }

.form-card {
  margin: 24px 28px 0;
  background: $bg-card;
  border: 1px solid $border;
  border-radius: 10px;
  overflow: hidden;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.25);
}

.form-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 24px;
  background: rgba(0, 212, 255, 0.05);
  border-bottom: 1px solid rgba(0, 212, 255, 0.1);
}

.form-card-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 600;
  color: rgba(0, 212, 255, 0.9);
}

.form-card-subtitle { font-size: 12px; color: $text-secondary; font-family: monospace; }

.form-card-body {
  padding: 24px 28px 8px;

  :deep(.el-form-item) { margin-bottom: 18px; }
  :deep(.el-form-item__label) { font-size: 13px; color: $text-secondary; }

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
  :deep(.el-textarea__inner) { color: $text-primary; font-size: 13.5px; background: transparent; }
}

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

.switch-hint {
  margin-left: 10px;
  font-size: 12px;
  color: $text-secondary;
}

.form-card-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 28px;
  border-top: 1px solid rgba(0, 212, 255, 0.08);
  background: rgba(0, 0, 0, 0.1);
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
