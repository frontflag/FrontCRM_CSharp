<template>
  <div class="customer-edit-page">
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
              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
            </svg>
          </div>
          <h1 class="page-title">{{ isEdit ? '编辑客户' : '新增客户' }}</h1>
        </div>
      </div>
      <div class="header-right">
        <template v-if="isEdit">
          <button class="btn-primary" @click="handleSave">保存</button>
        </template>
        <template v-else>
          <button class="btn-secondary" @click="saveDraftOnly">保存草稿</button>
          <button class="btn-warning" @click="handleConvertToFormal">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/>
              <polyline points="17 21 17 13 7 13 7 21"/>
              <polyline points="7 3 7 8 15 8"/>
            </svg>
            转正式
          </button>
        </template>
      </div>
    </div>

    <el-form
      ref="formRef"
      :model="formData"
      :rules="formRules"
      label-width="110px"
      class="customer-form"
    >
      <!-- 基本信息 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--cyan"></div>
          <span class="section-title">基本信息</span>
        </div>
        <div class="section-body">
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item label="客户编号" prop="customerCode">
                <el-input
                  v-model="formData.customerCode"
                  :placeholder="isEdit ? '' : '保存后自动生成'"
                  :disabled="true"
                  class="q-input"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="客户名称" prop="customerName">
                <el-input v-model="formData.customerName" placeholder="请输入客户名称" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="客户简称">
                <el-input v-model="formData.customerShortName" placeholder="请输入客户简称" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item label="客户类型" prop="customerType">
                <el-select v-model="formData.customerType" placeholder="请选择" style="width: 100%" class="q-select">
                  <el-option label="OEM" :value="1" />
                  <el-option label="ODM" :value="2" />
                  <el-option label="终端用户" :value="3" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="客户等级" prop="customerLevel">
                <el-select v-model="formData.customerLevel" placeholder="请选择" style="width: 100%" class="q-select">
                  <el-option label="VIP" value="VIP" />
                  <el-option label="重要" value="Important" />
                  <el-option label="普通" value="Normal" />
                  <el-option label="潜在客户" value="Lead" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="行业">
                <el-select v-model="formData.industry" placeholder="请选择" style="width: 100%" class="q-select">
                  <el-option label="制造业" value="Manufacturing" />
                  <el-option label="贸易/零售" value="Trading" />
                  <el-option label="科技/IT" value="Technology" />
                  <el-option label="建筑/工程" value="Construction" />
                  <el-option label="医疗/健康" value="Healthcare" />
                  <el-option label="教育" value="Education" />
                  <el-option label="金融" value="Finance" />
                  <el-option label="其他" value="Other" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item label="统一社会信用代码">
                <el-input v-model="formData.unifiedSocialCreditCode" placeholder="请输入统一社会信用代码" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="所属业务员">
                <SalesUserCascader
                  v-model="formData.salesPersonId"
                  placeholder="请选择业务员"
                  class="q-select"
                  @change="onSalesPersonChange"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="地区">
                <RegionCascaderWithQuickPick
                  v-model="regionValue"
                  :options="regionOptions"
                  placeholder="请选择地区"
                  cascader-class="q-cascader"
                  @change="handleRegionChange"
                />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="24">
              <el-form-item label="备注">
                <el-input v-model="formData.remarks" type="textarea" :rows="3" placeholder="请输入备注信息" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>
        </div>
      </div>

      <!-- 财务信息 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--amber"></div>
          <span class="section-title">财务信息</span>
        </div>
        <div class="section-body">
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item label="信用额度">
                <el-input-number v-model="formData.creditLimit" :min="0" :precision="2" style="width: 100%" class="q-number" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="账期(天)">
                <el-input-number v-model="formData.paymentTerms" :min="0" :max="365" style="width: 100%" class="q-number" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="结算货币">
                <el-select v-model="formData.currency" placeholder="请选择" style="width: 100%" class="q-select">
                  <el-option
                    v-for="opt in SETTLEMENT_CURRENCY_OPTIONS"
                    :key="opt.value"
                    :label="opt.label"
                    :value="opt.value"
                  />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item label="税率(%)">
                <el-input-number v-model="formData.taxRate" :min="0" :max="100" :precision="2" style="width: 100%" class="q-number" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="发票类型">
                <el-select v-model="formData.invoiceType" placeholder="请选择" style="width: 100%" class="q-select">
                  <el-option label="增值税专用发票" :value="1" />
                  <el-option label="增值税普通发票" :value="2" />
                  <el-option label="电子发票" :value="3" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
        </div>
      </div>

      <!-- 联系人信息 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--green"></div>
          <span class="section-title">联系人信息</span>
          <button type="button" class="btn-add-contact" @click="addContact">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
            添加联系人
          </button>
        </div>
        <div class="section-body">
          <div v-if="formData.contacts.length === 0" class="empty-contacts">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
              <circle cx="9" cy="7" r="4"/>
              <line x1="23" y1="11" x2="17" y2="11"/>
              <line x1="20" y1="8" x2="20" y2="14"/>
            </svg>
            <p>暂无联系人，点击上方按钮添加</p>
          </div>
          <div
            v-for="(contact, index) in formData.contacts"
            :key="index"
            class="contact-item"
          >
            <div class="contact-item-header">
              <span class="contact-index">联系人 {{ index + 1 }}</span>
              <button type="button" class="btn-remove" @click="removeContact(index)">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="3 6 5 6 21 6"/>
                  <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
                </svg>
                删除
              </button>
            </div>
            <el-row :gutter="16">
              <el-col :span="6">
                <el-form-item label="姓名" :prop="`contacts.${index}.contactName`" :rules="{ required: true, message: '请输入姓名', trigger: 'blur' }">
                  <el-input v-model="contact.contactName" placeholder="姓名" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="4">
                <el-form-item label="性别">
                  <el-select v-model="contact.gender" placeholder="性别" style="width: 100%" class="q-select">
                    <el-option label="男" :value="0" />
                    <el-option label="女" :value="1" />
                    <el-option label="保密" :value="2" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item
                  label="手机"
                  :prop="`contacts.${index}.mobilePhone`"
                  :rules="[{ validator: validateContactMobilePhone, trigger: ['blur', 'change'] }]"
                >
                  <el-input v-model="contact.mobilePhone" placeholder="手机" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item
                  label="邮箱"
                  :prop="`contacts.${index}.email`"
                  :rules="[{ validator: validateContactEmail, trigger: ['blur', 'change'] }]"
                >
                  <el-input v-model="contact.email" placeholder="邮箱" class="q-input" />
                </el-form-item>
              </el-col>
            </el-row>
            <el-row :gutter="16">
              <el-col :span="6">
                <el-form-item label="部门">
                  <el-input v-model="contact.department" placeholder="部门" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item label="职位">
                  <el-input v-model="contact.position" placeholder="职位" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item label="固话">
                  <el-input v-model="contact.phone" placeholder="固话" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item label=" ">
                  <el-checkbox v-model="contact.isDefault" class="q-checkbox">设为默认联系人</el-checkbox>
                </el-form-item>
              </el-col>
            </el-row>
          </div>
        </div>
      </div>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElNotification, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { customerApi, customerContactApi } from '@/api/customer';
import { draftApi } from '@/api/draft';
import SalesUserCascader from '@/components/SalesUserCascader.vue';
import RegionCascaderWithQuickPick from '@/components/RegionCascaderWithQuickPick.vue';
import { regionData } from '@/data/regions';
import type { CreateCustomerRequest } from '@/types/customer';
import { runValidatedFormSave } from '@/composables/useFormSubmit';
import { SETTLEMENT_CURRENCY_OPTIONS } from '@/constants/currency';
import { logRecentApi } from '@/api/logRecent';
import { CUSTOMER_RECENT_HISTORY_CHANGED_EVENT } from '@/constants/customerRecentHistory';
import {
  REGION_DISTRICT_PLACEHOLDER,
  regionCascaderValueFromFields
} from '@/constants/region';

const route = useRoute();
const router = useRouter();

const isEdit = computed(() => !!route.params.id);
const customerId = computed(() => route.params.id as string);
const formRef = ref<FormInstance>();
const currentDraftId = ref('');

const formData = reactive<CreateCustomerRequest & { contacts: any[] }>({
  customerCode: '', customerName: '', customerShortName: '',
  customerType: 1, customerLevel: 'Normal', industry: '',
  unifiedSocialCreditCode: '', salesPersonId: '', salesPersonName: '',
  country: '', province: '', city: '', district: '', address: '',
  creditLimit: 0, paymentTerms: 30, currency: 1, taxRate: 13,
  invoiceType: 2, isActive: true, remarks: '', contacts: []
});

const regionValue = ref<string[]>([]);

const formRules: FormRules = {
  customerName: [
    { required: true, message: '请输入客户名称', trigger: 'blur' },
    { min: 2, max: 100, message: '长度在 2 到 100 个字符', trigger: 'blur' }
  ],
  customerType: [{ required: true, message: '请选择客户类型', trigger: 'change' }],
  customerLevel: [{ required: true, message: '请选择客户等级', trigger: 'change' }]
};

const mobilePhonePattern = /^1[3-9]\d{9}$/;
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

const regionOptions = regionData;

function onSalesPersonChange(p: { id: string; label: string }) {
  formData.salesPersonName = p.label || '';
}

const fetchCustomerDetail = async () => {
  if (!isEdit.value) return;
  try {
    const customer = await customerApi.getCustomerById(customerId.value);
    const customerAny = customer as any;
    const mappedData: any = {
      ...customer,
      customerName: customer.customerName || customerAny.officialName,
      customerShortName: customer.customerShortName || customerAny.nickName,
      customerLevel: customer.customerLevel || 'Normal',
      customerType: customer.customerType ?? 1,
      salesPersonId: customer.salesPersonId || customerAny.salesUserId,
      unifiedSocialCreditCode: customer.unifiedSocialCreditCode || customerAny.creditCode,
      creditLimit: customer.creditLimit ?? 0,
      paymentTerms: customer.paymentTerms ?? 30,
      currency: customer.currency ?? 1,
      taxRate: customer.taxRate ?? 13,
      invoiceType: customer.invoiceType ?? 2,
      isActive: customer.isActive ?? true,
      remarks: customer.remarks || customerAny.remark,
      contacts: customer.contacts || []
    };
    Object.assign(formData, mappedData);
    if (mappedData.province && mappedData.city) {
      regionValue.value = regionCascaderValueFromFields(
        mappedData.province,
        mappedData.city,
        mappedData.district
      );
    }
    if (mappedData.contacts) {
      formData.contacts = mappedData.contacts.map((c: any) => ({
        ...c, contactName: c.contactName || c.name, mobilePhone: c.mobilePhone || c.mobile
      }));
    }
    logRecentApi
      .record({
        bizType: 'Customer',
        recordId: customerId.value,
        recordCode: formData.customerCode || undefined,
        openKind: 'edit'
      })
      .then(() => window.dispatchEvent(new CustomEvent(CUSTOMER_RECENT_HISTORY_CHANGED_EVENT)))
      .catch(() => {});
  } catch (error) {
    ElNotification.error({ title: '加载失败', message: '获取客户详情失败，请刷新重试' });
  }
};

const handleRegionChange = (value: string[]) => {
  if (value && value.length >= 2) {
    formData.province = value[0];
    formData.city = value[1];
    formData.district = value.length >= 3 ? value[2] : REGION_DISTRICT_PLACEHOLDER;
    formData.country = '中国';
  } else if (value?.length === 1) {
    formData.province = value[0];
    formData.city = '';
    formData.district = '';
    formData.country = '中国';
  } else if (!value?.length) {
    formData.province = '';
    formData.city = '';
    formData.district = '';
    formData.country = '';
  }
};

const addContact = () => {
  formData.contacts.push({
    contactName: '', gender: 0, department: '', position: '',
    mobilePhone: '', phone: '', email: '', isDefault: formData.contacts.length === 0
  });
};

const validateContactMobilePhone = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
  const v = (value || '').trim();
  if (v === '-') {
    callback();
    return;
  }
  if (!v) {
    callback(new Error('请输入手机'));
    return;
  }
  if (!mobilePhonePattern.test(v)) {
    callback(new Error('请输入正确的手机号码'));
    return;
  }
  callback();
};

const validateContactEmail = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
  const v = (value || '').trim();
  if (v === '-') {
    callback();
    return;
  }
  if (!v) {
    callback();
    return;
  }
  if (!emailPattern.test(v)) {
    callback(new Error('请输入正确的邮箱地址'));
    return;
  }
  callback();
};

const removeContact = async (index: number) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除联系人「${formData.contacts[index]?.contactName || '联系人 ' + (index + 1)}」吗？`,
      '确认删除',
      { type: 'warning', confirmButtonText: '确定删除', cancelButtonText: '取消' }
    );
    formData.contacts.splice(index, 1);
  } catch {
    // 用户取消，不做任何操作
  }
};

const buildDraftPayload = () => ({
  ...formData,
  contacts: formData.contacts.map((c: any) => ({ ...c }))
});

const applyDraftPayload = (payload: any) => {
  Object.assign(formData, payload || {});
  formData.contacts = Array.isArray(payload?.contacts)
    ? payload.contacts.map((c: any) => ({ ...c }))
    : [];
  if (formData.province && formData.city) {
    regionValue.value = regionCascaderValueFromFields(
      formData.province,
      formData.city,
      formData.district
    );
  }
};

const saveDraftOnly = async () => {
  try {
    const draft = await draftApi.saveDraft({
      draftId: currentDraftId.value || undefined,
      entityType: 'CUSTOMER',
      draftName: formData.customerName || formData.customerShortName || '客户草稿',
      payloadJson: JSON.stringify(buildDraftPayload()),
      remark: isEdit.value ? `来源客户ID:${customerId.value}` : undefined
    });
    currentDraftId.value = draft.draftId;
    ElNotification.success({ title: '保存成功', message: `草稿已保存（${draft.draftId}）` });
  } catch (err: any) {
    ElNotification.error({ title: '保存失败', message: err?.message || '草稿保存失败' });
  }
};

const restoreDraftById = async (draftId: string) => {
  const draft = await draftApi.getDraftById(draftId);
  if (draft.entityType !== 'CUSTOMER') {
    throw new Error('该草稿不是客户类型');
  }
  applyDraftPayload(JSON.parse(draft.payloadJson || '{}'));
  currentDraftId.value = draft.draftId;
};

const syncContactsForCustomer = async (targetCustomerId: string) => {
  const existingContacts = await customerContactApi.getContactsByCustomerId(targetCustomerId);
  const existingById = new Map(existingContacts.map((c: any) => [c.id, c]));
  const keptIds = new Set<string>();

  // 仅保留首个默认联系人，避免多默认导致后端状态不一致
  let defaultAssigned = false;
  const preparedContacts = formData.contacts.map((c: any) => {
    const next = { ...c };
    if (next.isDefault && !defaultAssigned) {
      defaultAssigned = true;
    } else {
      next.isDefault = false;
    }
    return next;
  });

  for (const contact of preparedContacts) {
    const payload = {
      contactName: contact.contactName || contact.name || '',
      gender: contact.gender,
      department: contact.department || '',
      position: contact.position || '',
      mobilePhone: contact.mobilePhone || contact.mobile || '',
      phone: contact.phone || contact.tel || '',
      email: contact.email || '',
      fax: contact.fax || '',
      isDefault: !!contact.isDefault,
      isDecisionMaker: !!contact.isDecisionMaker,
      remarks: contact.remarks || contact.remark || ''
    };

    if (contact.id && existingById.has(contact.id)) {
      await customerContactApi.updateContact(contact.id, payload);
      keptIds.add(contact.id);
    } else {
      const created = await customerContactApi.createContact(targetCustomerId, payload as any);
      const createdId = (created as any)?.id || (created as any)?.data?.id;
      if (createdId) keptIds.add(createdId);
    }
  }

  for (const oldContact of existingContacts) {
    if (!keptIds.has(oldContact.id)) {
      await customerContactApi.deleteContact(oldContact.id);
    }
  }
};

const handleSave = async () => {
  const editing = isEdit.value;
  await runValidatedFormSave(formRef, {
    task: async () => {
      let targetCustomerId = '';
      if (editing) {
        await customerApi.updateCustomer(customerId.value, formData);
        targetCustomerId = customerId.value;
      } else {
        const created = await customerApi.createCustomer(formData);
        targetCustomerId = (created as any)?.id || (created as any)?.data?.id || '';
      }

      if (targetCustomerId) {
        await syncContactsForCustomer(targetCustomerId);
      }
    },
    formatSuccess: () => (editing ? '客户信息已成功更新' : '客户已成功创建'),
    onSuccess: () => router.push({ name: 'CustomerList' }),
    errorMessage: (err: unknown) => {
      const e = err as {
        response?: { data?: { message?: string; errors?: string[] } };
        message?: string;
      };
      const serverMsg =
        e?.response?.data?.message ||
        (Array.isArray(e?.response?.data?.errors) ? e.response!.data!.errors!.join('；') : null) ||
        e?.message;
      return serverMsg ? `保存错误：${serverMsg}` : '保存错误，请稍后重试';
    }
  });
};

const handleConvertToFormal = async () => {
  // 需求：只有用户主动点击“保存草稿”才保存草稿；
  // “转正式”应直接保存正式数据，不再自动保存草稿。
  await handleSave();
};

const goBack = () => router.back();
onMounted(() => {
  fetchCustomerDetail();
  const draftId = route.query.draftId;
  if (!isEdit.value && typeof draftId === 'string' && draftId) {
    restoreDraftById(draftId).catch((err: any) => {
      ElNotification.error({ title: '恢复失败', message: err?.message || '草稿恢复失败' });
    });
  }
});
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+SC:wght@300;400;500&display=swap');

.customer-edit-page {
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
  margin-bottom: 24px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 14px;
  }

  .header-right {
    display: flex;
    gap: 10px;
  }
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;

  .page-icon {
    width: 32px;
    height: 32px;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.25);
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: $cyan-primary;
  }

  .page-title {
    font-size: 18px;
    font-weight: 600;
    color: $text-primary;
    margin: 0;
  }
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

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.25);
  }
}

// 业务流转：草稿转正式（禁止用蓝/红/绿）
.btn-warning {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: rgba(201, 154, 69, 0.18);
  border: 1px solid rgba(201, 154, 69, 0.45);
  border-radius: $border-radius-md;
  color: $color-amber;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(201, 154, 69, 0.28);
    box-shadow: 0 2px 12px rgba(201, 154, 69, 0.15);
  }
}

// ---- 表单区块 ----
.form-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: rgba(0, 0, 0, 0.1);
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;

  &--cyan  { background: $cyan-primary; box-shadow: 0 0 6px rgba(0,212,255,0.6); }
  &--amber { background: $color-amber;  box-shadow: 0 0 6px rgba(201,154,69,0.6); }
  &--green { background: $color-mint-green; box-shadow: 0 0 6px rgba(70,191,145,0.6); }
}

.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-primary;
  flex: 1;
}

.section-body {
  padding: 20px;
}

// ---- 添加联系人按钮 ----
.btn-add-contact {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 10px;
  background: rgba(70, 191, 145, 0.1);
  border: 1px solid rgba(70, 191, 145, 0.3);
  border-radius: $border-radius-sm;
  color: $color-mint-green;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(70, 191, 145, 0.18);
    box-shadow: 0 0 8px rgba(70, 191, 145, 0.2);
  }
}

// ---- 联系人条目 ----
.empty-contacts {
  text-align: center;
  padding: 32px;
  color: $text-muted;

  svg { margin-bottom: 10px; opacity: 0.4; }
  p { font-size: 13px; margin: 0; }
}

.contact-item {
  background: $layer-3;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  padding: 16px;
  margin-bottom: 12px;

  &:last-child { margin-bottom: 0; }
}

.contact-item-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;

  .contact-index {
    font-size: 12px;
    font-weight: 500;
    color: $color-ice-blue;
    letter-spacing: 0.5px;
  }
}

.btn-remove {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 3px 8px;
  background: rgba(201, 87, 69, 0.08);
  border: 1px solid rgba(201, 87, 69, 0.2);
  border-radius: 4px;
  color: $color-red-brown;
  font-size: 11px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.15s;

  &:hover { background: rgba(201, 87, 69, 0.15); }
}

// ---- Element Plus 覆写 ----
.customer-form {
  :deep(.el-form-item__label) {
    color: $text-muted !important;
    font-size: 13px;
  }

  :deep(.el-form-item__error) {
    color: $color-red-brown !important;
    font-size: 11px;
  }
}

.q-input {
  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    color: $text-primary !important;
    transition: border-color 0.2s;

    &:hover { border-color: rgba(0, 212, 255, 0.25) !important; }
    &.is-focus { border-color: rgba(0, 212, 255, 0.5) !important; box-shadow: 0 0 0 2px rgba(0,212,255,0.08) !important; }
  }

  :deep(.el-input__inner) {
    color: $text-primary !important;
    background: transparent !important;
    font-size: 13px;
    &::placeholder { color: $text-placeholder !important; }
  }

  :deep(.el-textarea__inner) {
    font-size: 13px;
    &::placeholder { color: $text-placeholder !important; }
  }

  :deep(.el-input__wrapper.is-disabled) {
    opacity: 0.5;
  }
}

.q-select {
  :deep(.el-select__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    color: $text-primary !important;

    &.is-focused { border-color: rgba(0, 212, 255, 0.5) !important; }
  }

  :deep(.el-select__placeholder) { color: $text-placeholder !important; }
  :deep(.el-select__selected-item) { color: $text-primary !important; }
}

.q-number {
  :deep(.el-input__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;

    &.is-focus { border-color: rgba(0, 212, 255, 0.5) !important; }
  }

  :deep(.el-input__inner) {
    color: $text-primary !important;
    background: transparent !important;
  }

  :deep(.el-input-number__decrease),
  :deep(.el-input-number__increase) {
    background: rgba(255,255,255,0.05) !important;
    border-color: $border-panel !important;
    color: $text-muted !important;

    &:hover { color: $cyan-primary !important; background: rgba(0,212,255,0.08) !important; }
  }
}

.q-cascader {
  :deep(.el-input__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;

    &.is-focus { border-color: rgba(0, 212, 255, 0.5) !important; }
  }

  :deep(.el-input__inner) {
    color: $text-primary !important;
    &::placeholder { color: $text-placeholder !important; }
  }
}

.q-checkbox {
  :deep(.el-checkbox__label) { color: $text-secondary !important; font-size: 12px; }
  :deep(.el-checkbox__inner) {
    background: $layer-3 !important;
    border-color: $border-panel !important;
  }
  :deep(.el-checkbox.is-checked .el-checkbox__inner) {
    background: $color-mint-green !important;
    border-color: $color-mint-green !important;
  }
}
</style>
