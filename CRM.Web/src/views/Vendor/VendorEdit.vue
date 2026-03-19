<template>
  <div class="vendor-edit-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
          返回
        </button>
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
              <polyline points="9 22 9 12 15 12 15 22"/>
            </svg>
          </div>
          <h1 class="page-title">{{ isEdit ? '编辑供应商' : '新增供应商' }}</h1>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-ghost" @click="goBack">取消</button>
        <button class="btn-primary" @click="handleSave" :disabled="saving">
          <svg v-if="!saving" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/>
            <polyline points="17 21 17 13 7 13 7 21"/>
            <polyline points="7 3 7 8 15 8"/>
          </svg>
          {{ saving ? '保存中...' : '保存' }}
        </button>
      </div>
    </div>

    <el-form ref="formRef" :model="formData" :rules="formRules" label-width="110px" class="vendor-form">

      <!-- 基本信息 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--cyan"></div>
          <span class="section-title">基本信息</span>
        </div>
        <div class="section-body">
          <el-row :gutter="24">
            <el-col :span="8">
              <el-form-item label="供应商编号">
                <el-input
                  :model-value="isEdit ? formData.code : ''"
                  :placeholder="isEdit ? '' : '系统生成'"
                  disabled
                  class="q-input readonly-code"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="供应商全称" prop="officialName">
                <el-input v-model="formData.officialName" placeholder="请输入供应商全称" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="简称/别名">
                <el-input v-model="formData.nickName" placeholder="常用简称" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="24">
            <el-col :span="8">
              <el-form-item label="所属行业">
                <el-select v-model="formData.industry" placeholder="请选择行业" clearable class="q-select">
                  <el-option label="电子/半导体" value="Electronics" />
                  <el-option label="机械/设备" value="Machinery" />
                  <el-option label="化工/材料" value="Chemical" />
                  <el-option label="纺织/服装" value="Textile" />
                  <el-option label="食品/农业" value="Food" />
                  <el-option label="建筑/工程" value="Construction" />
                  <el-option label="贸易/零售" value="Trading" />
                  <el-option label="科技/IT" value="Technology" />
                  <el-option label="医疗/健康" value="Healthcare" />
                  <el-option label="其他" value="Other" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="信用评级">
                <el-select v-model="formData.credit" placeholder="请选择评级" clearable class="q-select">
                  <el-option label="★☆☆☆☆ 1星" :value="1" />
                  <el-option label="★★☆☆☆ 2星" :value="2" />
                  <el-option label="★★★☆☆ 3星" :value="3" />
                  <el-option label="★★★★☆ 4星" :value="4" />
                  <el-option label="★★★★★ 5星" :value="5" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="合作状态">
                <el-select v-model="formData.status" placeholder="请选择状态" class="q-select">
                  <el-option label="草稿" :value="0" />
                  <el-option label="待审核" :value="1" />
                  <el-option label="合作中" :value="2" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
        </div>
      </div>

      <!-- 联系信息 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--green"></div>
          <span class="section-title">联系信息</span>
        </div>
        <div class="section-body">
          <el-row :gutter="24">
            <el-col :span="8">
              <el-form-item label="联系人姓名">
                <el-input v-model="formData.contactName" placeholder="主要联系人" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="联系电话">
                <el-input v-model="formData.contactPhone" placeholder="手机号码" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="联系邮箱">
                <el-input v-model="formData.contactEmail" placeholder="电子邮箱" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="24">
            <el-col :span="16">
              <el-form-item label="办公地址">
                <el-input v-model="formData.officeAddress" placeholder="详细办公地址" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="官方网站">
                <el-input v-model="formData.website" placeholder="https://..." class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>
        </div>
      </div>

      <!-- 合作条款 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--amber"></div>
          <span class="section-title">合作条款</span>
        </div>
        <div class="section-body">
          <el-row :gutter="24">
            <el-col :span="8">
              <el-form-item label="结算货币">
                <el-select v-model="formData.currency" placeholder="请选择货币" class="q-select">
                  <el-option label="人民币 CNY" :value="1" />
                  <el-option label="美元 USD" :value="2" />
                  <el-option label="欧元 EUR" :value="3" />
                  <el-option label="港币 HKD" :value="4" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="付款方式">
                <el-select v-model="formData.paymentMethod" placeholder="请选择付款方式" class="q-select">
                  <el-option label="预付款" value="Prepaid" />
                  <el-option label="货到付款" value="COD" />
                  <el-option label="月结" value="Monthly" />
                  <el-option label="账期" value="Credit" />
                  <el-option label="电汇 T/T" value="TT" />
                  <el-option label="信用证 L/C" value="LC" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="账期（天）">
                <el-input-number v-model="formData.paymentDays" :min="0" :max="365" placeholder="0" class="q-input" style="width:100%" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="24">
            <el-col :span="8">
              <el-form-item label="采购员">
                <el-input v-model="formData.purchaserName" placeholder="负责采购员" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="税号">
                <el-input v-model="formData.taxNumber" placeholder="统一社会信用代码" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="开户银行">
                <el-input v-model="formData.bankName" placeholder="开户银行名称" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="24">
            <el-col :span="8">
              <el-form-item label="银行账号">
                <el-input v-model="formData.bankAccount" placeholder="银行账号" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="账户名称">
                <el-input v-model="formData.bankAccountName" placeholder="开户名称" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>
        </div>
      </div>

      <!-- 备注 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--purple"></div>
          <span class="section-title">备注信息</span>
        </div>
        <div class="section-body">
          <el-form-item label="公司简介">
            <el-input
              v-model="formData.companyInfo"
              type="textarea"
              :rows="3"
              placeholder="供应商公司简介、主营业务等"
              class="q-input"
            />
          </el-form-item>
          <el-form-item label="备注">
            <el-input
              v-model="formData.remark"
              type="textarea"
              :rows="2"
              placeholder="其他备注信息"
              class="q-input"
            />
          </el-form-item>
        </div>
      </div>

    </el-form>

    <!-- 联系人（仅编辑时显示） -->
    <div v-if="isEdit" class="form-section">
      <div class="section-header">
        <div class="section-dot section-dot--green"></div>
        <span class="section-title">联系人</span>
        <button type="button" class="btn-add-contact" @click="openContactDialog()">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          添加联系人
        </button>
      </div>
      <div class="section-body">
        <div v-if="contacts.length === 0" class="empty-contacts">
          <p>暂无联系人，点击上方按钮添加</p>
        </div>
        <el-table v-else :data="contacts" class="contact-table" size="small">
          <el-table-column prop="cName" label="姓名" width="100" />
          <el-table-column prop="title" label="职位" width="100" />
          <el-table-column prop="department" label="部门" width="100" />
          <el-table-column prop="mobile" label="手机" width="130" />
          <el-table-column prop="email" label="邮箱" min-width="160" show-overflow-tooltip />
          <el-table-column label="主联系人" width="80" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.isMain" type="success" size="small">主</el-tag>
              <span v-else class="td-empty">--</span>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="120" fixed="right">
            <template #default="{ row }">
              <button type="button" class="action-btn" @click="openContactDialog(row)">编辑</button>
              <button type="button" class="action-btn danger" @click="handleDeleteContact(row)">删除</button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>

    <VendorContactDialog
      v-if="isEdit"
      v-model="showContactDialog"
      :vendor-id="vendorId"
      :contact="editingContact"
      @success="onContactSuccess"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { vendorApi, vendorContactApi } from '@/api/vendor';
import type { VendorContactInfo } from '@/types/vendor';
import VendorContactDialog from './components/VendorContactDialog.vue';

const route = useRoute();
const router = useRouter();

const isEdit = computed(() => !!route.params.id);
const vendorId = computed(() => route.params.id as string);

const formRef = ref<FormInstance>();
const saving = ref(false);

const formData = reactive({
  code: '',
  officialName: '',
  nickName: '',
  industry: '',
  credit: undefined as number | undefined,
  status: 0 as number,
  contactName: '',
  contactPhone: '',
  contactEmail: '',
  officeAddress: '',
  website: '',
  currency: 1 as number,
  paymentMethod: '',
  paymentDays: 0,
  purchaserName: '',
  taxNumber: '',
  bankName: '',
  bankAccount: '',
  bankAccountName: '',
  companyInfo: '',
  remark: ''
});

const formRules = computed<FormRules>(() => ({
  officialName: [{ required: true, message: '请输入供应商全称', trigger: 'blur' }],
  // code 由后端自动生成，无需前端必填验证
}));

const contacts = ref<VendorContactInfo[]>([]);
const showContactDialog = ref(false);
const editingContact = ref<VendorContactInfo | null>(null);

const goBack = () => router.push('/vendors');

const loadVendor = async () => {
  if (!vendorId.value) return;
  try {
    const v = await vendorApi.getVendorById(vendorId.value);
    const data = v as any;
    formData.code = data.code ?? '';
    formData.officialName = data.officialName ?? data.name ?? '';
    formData.nickName = data.nickName ?? '';
    formData.industry = data.industry ?? '';
    formData.credit = data.credit;
    formData.status = data.status ?? 0;
    formData.officeAddress = data.officeAddress ?? '';
    formData.companyInfo = data.companyInfo ?? '';
    formData.remark = data.remark ?? '';
    contacts.value = data.contacts ?? [];
  } catch (e) {
    ElMessage.error('获取供应商详情失败');
  }
};

const handleSave = async () => {
  const valid = await formRef.value?.validate().catch(() => false);
  if (!valid) return;
  saving.value = true;
  try {
    if (isEdit.value) {
      await vendorApi.updateVendor(vendorId.value, {
        name: formData.officialName,
        remark: formData.remark
      });
      ElMessage.success('保存成功');
    } else {
      await vendorApi.createVendor({
        name: formData.officialName.trim(),
        officialName: formData.officialName.trim(),
        nickName: formData.nickName?.trim() || undefined,
        industry: formData.industry || undefined,
        credit: formData.credit || undefined,
        status: formData.status ?? 0,
        officeAddress: formData.officeAddress?.trim() || undefined,
        creditCode: formData.taxNumber?.trim() || undefined,
        companyInfo: formData.companyInfo?.trim() || undefined,
        remark: formData.remark?.trim() || undefined
      });
      ElMessage.success('创建成功');
      router.replace('/vendors');
      return;
    }
  } catch (error: any) {
    const msg = error?.message || error?.data?.message || '保存失败';
    ElMessage.error(msg);
  } finally {
    saving.value = false;
  }
};

const openContactDialog = (contact?: VendorContactInfo) => {
  editingContact.value = contact ?? null;
  showContactDialog.value = true;
};

const onContactSuccess = () => {
  loadVendor();
};

const handleDeleteContact = async (row: VendorContactInfo) => {
  try {
    await ElMessageBox.confirm('确定要删除该联系人吗？', '确认删除', { type: 'warning' });
    await vendorContactApi.deleteContact(row.id);
    ElMessage.success('删除成功');
    loadVendor();
  } catch (e) {
    if (e !== 'cancel') ElMessage.error('删除失败');
  }
};

onMounted(() => {
  if (isEdit.value) loadVendor();
});
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.vendor-edit-page {
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

  .header-left { display: flex; align-items: center; gap: 12px; }
  .header-right { display: flex; align-items: center; gap: 10px; }
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover { border-color: rgba(0, 212, 255, 0.3); color: $cyan-primary; }
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
  }
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 18px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  letter-spacing: 0.5px;

  &:hover:not(:disabled) { transform: translateY(-1px); box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25); }
  &:disabled { opacity: 0.6; cursor: not-allowed; }
}

.btn-ghost {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 7px 14px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover { border-color: rgba(0, 212, 255, 0.3); color: $text-secondary; }
}

.vendor-form { max-width: 100%; }

.form-section {
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  margin-bottom: 16px;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: rgba(0, 212, 255, 0.02);

  .section-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    flex-shrink: 0;

    &.section-dot--cyan   { background: $cyan-primary; box-shadow: 0 0 6px $cyan-primary; }
    &.section-dot--green  { background: #46BF91; box-shadow: 0 0 6px #46BF91; }
    &.section-dot--amber  { background: $color-amber; box-shadow: 0 0 6px $color-amber; }
    &.section-dot--purple { background: #9B7FD4; box-shadow: 0 0 6px #9B7FD4; }
  }

  .section-title {
    font-size: 14px;
    font-weight: 600;
    color: $text-primary;
    flex: 1;
    letter-spacing: 0.3px;
  }
}

.btn-add-contact {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  background: rgba(0, 212, 255, 0.1);
  border: 1px solid rgba(0, 212, 255, 0.3);
  border-radius: 6px;
  color: $cyan-primary;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover { background: rgba(0, 212, 255, 0.2); }
}

.section-body {
  padding: 20px 20px 8px;
}

// Element Plus 输入框深色主题适配
.q-input {
  width: 100%;

  :deep(.el-input__wrapper) {
    background: rgba(255, 255, 255, 0.04) !important;
    box-shadow: 0 0 0 1px $border-panel !important;
    border-radius: $border-radius-md !important;

    &:hover { box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.3) !important; }
    &.is-focus { box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.5) !important; }
  }

  :deep(.el-input__inner) {
    color: $text-primary !important;
    font-family: 'Noto Sans SC', sans-serif;

    &::placeholder { color: $text-muted !important; }
  }

  :deep(.el-textarea__inner) {
    background: rgba(255, 255, 255, 0.04) !important;
    box-shadow: 0 0 0 1px $border-panel !important;
    border-radius: $border-radius-md !important;
    color: $text-primary !important;
    font-family: 'Noto Sans SC', sans-serif;
    resize: vertical;

    &::placeholder { color: $text-muted !important; }
    &:focus { box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.5) !important; }
  }
}

.q-select {
  width: 100%;

  :deep(.el-select__wrapper) {
    background: rgba(255, 255, 255, 0.04) !important;
    box-shadow: 0 0 0 1px $border-panel !important;
    border-radius: $border-radius-md !important;

    &:hover { box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.3) !important; }
    &.is-focused { box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.5) !important; }
  }

  :deep(.el-select__placeholder) { color: $text-muted !important; }
  :deep(.el-select__selected-item) { color: $text-primary !important; }
}

:deep(.el-form-item__label) {
  color: $text-secondary !important;
  font-size: 13px;
}

:deep(.el-input-number) {
  .el-input__wrapper {
    background: rgba(255, 255, 255, 0.04) !important;
    box-shadow: 0 0 0 1px $border-panel !important;
    border-radius: $border-radius-md !important;

    &:hover { box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.3) !important; }
  }

  .el-input__inner { color: $text-primary !important; }
}

// 联系人表格
.contact-table {
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-header-bg-color: rgba(0, 212, 255, 0.04);
  --el-table-border-color: rgba(255, 255, 255, 0.06);
  --el-table-text-color: #{$text-secondary};
  --el-table-header-text-color: #{$text-muted};
  --el-table-row-hover-bg-color: rgba(0, 212, 255, 0.04);
}

.action-btn {
  background: none;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 0 6px;
  margin-right: 8px;
  font-family: 'Noto Sans SC', sans-serif;
  white-space: nowrap;
  flex-shrink: 0;

  &:hover { text-decoration: underline; }
  &.danger { color: #C95745; }
}

.empty-contacts {
  text-align: center;
  padding: 24px;
  color: $text-muted;
  font-size: 13px;
}

.td-empty {
  color: rgba(255,255,255,0.2);
  font-size: 12px;
}
// 只读编号字段样式
.readonly-code {
  :deep(.el-input__wrapper) {
    background: rgba(255,255,255,0.02) !important;
    border-color: rgba(255,255,255,0.06) !important;
    cursor: default !important;
  }
  :deep(.el-input__inner) {
    color: rgba(255,255,255,0.4) !important;
    font-family: 'Space Mono', monospace;
    font-size: 12px;
    letter-spacing: 0.5px;
    cursor: default !important;
  }
  :deep(.el-input__inner::placeholder) {
    color: rgba(0, 212, 255, 0.5) !important;
    font-family: 'Noto Sans SC', sans-serif;
    font-size: 12px;
    font-style: italic;
  }
}
</style>
