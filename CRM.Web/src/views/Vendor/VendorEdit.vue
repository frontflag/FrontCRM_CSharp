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
        <button v-if="isEdit" class="btn-ghost" @click="handleRestoreDraft" :disabled="saving">从草稿恢复</button>
        <button class="btn-ghost" @click="saveDraftOnly" :disabled="saving">保存草稿</button>
        <button class="btn-ghost" @click="goBack">取消</button>
        <button class="btn-primary" @click="handleConvertToFormal" :disabled="saving">
          <svg v-if="!saving" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/>
            <polyline points="17 21 17 13 7 13 7 21"/>
            <polyline points="7 3 7 8 15 8"/>
          </svg>
          {{ saving ? '处理中...' : (isEdit ? '保存' : '转正式') }}
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
              <el-form-item label="等级">
                <el-select v-model="formData.level" placeholder="请选择等级" clearable class="q-select">
                  <el-option
                    v-for="opt in VENDOR_LEVEL_OPTIONS"
                    :key="opt.value"
                    :label="opt.label"
                    :value="opt.value"
                  />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="身份">
                <el-select v-model="formData.credit" placeholder="请选择身份" clearable class="q-select">
                  <el-option
                    v-for="opt in VENDOR_IDENTITY_OPTIONS"
                    :key="opt.value"
                    :label="opt.label"
                    :value="opt.value"
                  />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>

          <el-row :gutter="24">
            <el-col :span="8">
              <el-form-item label="合作状态">
                <el-select v-model="formData.status" placeholder="请选择状态" class="q-select">
                  <el-option label="草稿" :value="0" />
                  <el-option label="待审核" :value="1" />
                  <el-option label="合作中" :value="2" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="采购员">
                <PurchaserCascader
                  v-model="formData.purchaseUserId"
                  placeholder="请选择负责采购员"
                  class="q-select"
                  @change="onPurchaserChange"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="办公地址">
                <el-input v-model="formData.officeAddress" placeholder="详细办公地址" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="24">
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
          <span class="section-title">财务信息</span>
        </div>
        <div class="section-body">
          <el-row :gutter="24">
            <el-col :span="8">
              <el-form-item label="结算货币">
                <el-select v-model="formData.currency" placeholder="请选择货币" class="q-select">
                  <el-option
                    v-for="opt in SETTLEMENT_CURRENCY_OPTIONS"
                    :key="opt.value"
                    :label="opt.label"
                    :value="opt.value"
                  />
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

    <!-- 联系人信息（与客户页面一致：内联添加/删除） -->
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
        <div v-if="contacts.length === 0" class="empty-contacts">
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
            <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
            <circle cx="9" cy="7" r="4"/>
            <line x1="23" y1="11" x2="17" y2="11"/>
            <line x1="20" y1="8" x2="20" y2="14"/>
          </svg>
          <p>暂无联系人，点击上方按钮添加</p>
        </div>

        <div v-for="(contact, index) in contacts" :key="contact._key || index" class="contact-item">
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
              <el-form-item label="姓名">
                <el-input v-model="contact.cName" placeholder="联系人姓名" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item label="职位">
                <el-input v-model="contact.title" placeholder="职位/角色" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item label="手机">
                <el-input v-model="contact.mobile" placeholder="手机号" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item label="邮箱">
                <el-input v-model="contact.email" placeholder="邮箱" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>

          <el-row :gutter="16" style="margin-top: 6px;">
            <el-col :span="6">
              <el-form-item label="部门">
                <el-input v-model="contact.department" placeholder="所属部门" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item label="固话">
                <el-input v-model="contact.tel" placeholder="座机电话" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item label="备注">
                <el-input v-model="contact.remark" placeholder="备注" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item label=" ">
                <el-checkbox v-model="contact.isMain" class="q-checkbox">设为主联系人</el-checkbox>
              </el-form-item>
            </el-col>
          </el-row>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { vendorApi, vendorBankApi, vendorContactApi } from '@/api/vendor';
import { draftApi } from '@/api/draft';
import type { CreateVendorRequest, UpdateVendorRequest, Vendor, VendorContactInfo } from '@/types/vendor';
import { runValidatedFormSave } from '@/composables/useFormSubmit';
import { SETTLEMENT_CURRENCY_OPTIONS } from '@/constants/currency';
import { VENDOR_LEVEL_OPTIONS, VENDOR_IDENTITY_OPTIONS } from '@/constants/vendorEnums';
import PurchaserCascader from '@/components/PurchaserCascader.vue';
import { logRecentApi } from '@/api/logRecent';
import { VENDOR_RECENT_HISTORY_CHANGED_EVENT } from '@/constants/vendorRecentHistory';

const route = useRoute();
const router = useRouter();

/** 与 CustomerEdit 一致：create 路由无 id，:id/edit 有 id */
const isEdit = computed(() => !!route.params.id);
const vendorId = computed(() => route.params.id as string);

const formRef = ref<FormInstance>();
const saving = ref(false);
const currentDraftId = ref('');

const formData = reactive({
  code: '',
  officialName: '',
  nickName: '',
  industry: '',
  level: undefined as number | undefined,
  /** 身份（vendorinfo.Credit） */
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
  purchaseUserId: '',
  purchaserName: '',
  taxNumber: '',
  bankName: '',
  bankAccount: '',
  bankAccountName: '',
  companyInfo: '',
  remark: ''
});

const formRules: FormRules = {
  officialName: [{ required: true, message: '请输入供应商全称', trigger: 'blur' }]
};

function onPurchaserChange(p: { id: string; label: string }) {
  formData.purchaserName = p.label || '';
}

type VendorContactDraft = Omit<VendorContactInfo, 'vendorId' | 'id'> & { id?: string; vendorId?: string; _key?: string };
const contacts = ref<VendorContactDraft[]>([]);

const goBack = () => router.push({ name: 'VendorList' });

const buildVendorApiPayload = (): CreateVendorRequest & UpdateVendorRequest => ({
  name: formData.officialName.trim(),
  nickName: formData.nickName?.trim(),
  industry: formData.industry || undefined,
  level: formData.level,
  credit: formData.credit,
  status: formData.status,
  officeAddress: formData.officeAddress?.trim(),
  website: formData.website?.trim(),
  purchaserName: formData.purchaserName?.trim(),
  tradeCurrency: formData.currency,
  paymentMethod: formData.paymentMethod || undefined,
  paymentDays: Number(formData.paymentDays ?? 0),
  creditCode: formData.taxNumber?.trim(),
  companyInfo: formData.companyInfo?.trim(),
  remark: formData.remark?.trim()
});

const syncVendorBankForVendor = async (targetVendorId: string) => {
  const hasBankData = !!(
    formData.bankName?.trim() ||
    formData.bankAccount?.trim() ||
    formData.bankAccountName?.trim()
  );
  const existing = await vendorBankApi.getBanksByVendorId(targetVendorId);
  if (!hasBankData) {
    for (const b of existing) {
      await vendorBankApi.deleteBank(b.id);
    }
    return;
  }
  const payload = {
    bankName: formData.bankName?.trim() || undefined,
    bankAccount: formData.bankAccount?.trim() || undefined,
    accountName: formData.bankAccountName?.trim() || undefined,
    currency: formData.currency,
    isDefault: true
  };
  const first = existing[0];
  if (first) {
    await vendorBankApi.updateBank(first.id, payload);
    for (let i = 1; i < existing.length; i++) {
      await vendorBankApi.deleteBank(existing[i].id);
    }
  } else {
    await vendorBankApi.createBank(targetVendorId, payload);
  }
};

const fetchVendorDetail = async () => {
  if (!isEdit.value) return;
  try {
    const data: Vendor = await vendorApi.getVendorById(vendorId.value);
    formData.code = data.code ?? '';
    formData.officialName = data.officialName ?? data.name ?? '';
    formData.nickName = data.nickName ?? '';
    formData.industry = data.industry ?? '';
    formData.level = data.level;
    formData.credit = data.credit;
    formData.status = data.status ?? 0;
    formData.officeAddress = data.officeAddress ?? '';
    formData.website = data.website ?? '';
    formData.currency = data.tradeCurrency ?? 1;
    formData.paymentMethod = data.paymentMethod ?? '';
    formData.paymentDays = Number(data.payment ?? 0);
    formData.purchaserName = data.purchaserName ?? '';
    formData.purchaseUserId = '';
    formData.taxNumber = data.creditCode ?? '';
    formData.companyInfo = data.companyInfo ?? '';
    formData.remark = data.remark ?? '';
    const banks = data.bankAccounts ?? [];
    const b0 = banks[0];
    formData.bankName = b0?.bankName ?? '';
    formData.bankAccount = b0?.bankAccount ?? '';
    formData.bankAccountName = b0?.accountName ?? '';
    contacts.value = (data.contacts ?? []).map((c, idx: number) => ({
      ...c,
      _key: c.id || `srv-${idx}`
    }));
    logRecentApi
      .record({
        bizType: 'Vendor',
        recordId: data.id,
        recordCode: data.code || undefined,
        openKind: 'edit'
      })
      .then(() => window.dispatchEvent(new CustomEvent(VENDOR_RECENT_HISTORY_CHANGED_EVENT)))
      .catch(() => {});
  } catch (e) {
    ElMessage.error('获取供应商详情失败');
  }
};

const buildDraftPayload = () => ({
  ...formData,
  contacts: contacts.value
});

const applyDraftPayload = (payload: any) => {
  Object.assign(formData, payload || {});
  if (Array.isArray(payload?.contacts)) {
    contacts.value = payload.contacts.map((c: any, idx: number) => ({
      ...c,
      _key: c.id || `tmp-${idx}`
    }));
  }
};

const saveDraftOnly = async () => {
  try {
    const draft = await draftApi.saveDraft({
      draftId: currentDraftId.value || undefined,
      entityType: 'VENDOR',
      draftName: formData.officialName || formData.nickName || '供应商草稿',
      payloadJson: JSON.stringify(buildDraftPayload()),
      remark: isEdit.value ? `来源供应商ID:${vendorId.value}` : undefined
    });
    currentDraftId.value = draft.draftId;
    ElMessage.success(`草稿已保存（${draft.draftId}）`);
  } catch (error: any) {
    ElMessage.error(error?.message || '草稿保存失败');
  }
};

const restoreDraftById = async (draftId: string) => {
  const draft = await draftApi.getDraftById(draftId);
  if (draft.entityType !== 'VENDOR') throw new Error('该草稿不是供应商类型');
  applyDraftPayload(JSON.parse(draft.payloadJson || '{}'));
  currentDraftId.value = draft.draftId;
};

const handleRestoreDraft = async () => {
  try {
    const { value } = await ElMessageBox.prompt('请输入草稿ID', '从草稿恢复', {
      confirmButtonText: '恢复',
      cancelButtonText: '取消',
      inputPlaceholder: 'DraftId'
    });
    if (!value) return;
    await restoreDraftById(value);
    ElMessage.success('供应商草稿已恢复');
  } catch (e: any) {
    if (e === 'cancel' || e === 'close') return;
    ElMessage.error(e?.message || '草稿恢复失败');
  }
};

const handleSave = async () => {
  const editing = isEdit.value;
  await runValidatedFormSave(formRef, {
    loading: saving,
    task: async () => {
      let targetVendorId = '';
      let mode: 'edit' | 'create' = 'create';
      const payload = buildVendorApiPayload();
      if (editing) {
        await vendorApi.updateVendor(vendorId.value, payload);
        targetVendorId = vendorId.value;
        mode = 'edit';
      } else {
        const created = await vendorApi.createVendor(payload);
        targetVendorId = (created as { id?: string })?.id || '';
        mode = 'create';
      }

      if (targetVendorId) {
        await syncVendorBankForVendor(targetVendorId);
        await syncContactsForVendor(targetVendorId);
      }

      return mode;
    },
    formatSuccess: (mode) => (mode === 'edit' ? '保存成功' : '创建成功'),
    onSuccess: (mode) => {
      if (mode === 'create') router.replace({ name: 'VendorList' });
      else void fetchVendorDetail();
    },
    errorMessage: (error: unknown) => {
      const e = error as { message?: string; data?: { message?: string } };
      return e?.message || e?.data?.message || '保存失败';
    }
  });
};

const handleConvertToFormal = async () => {
  // 需求：只有用户主动点击“保存草稿”才保存草稿；
  // “转正式”应直接保存正式数据，不再自动保存草稿。
  await handleSave();
};

const addContact = () => {
  const isFirst = contacts.value.length === 0;
  contacts.value.push({
    _key: `new-${Date.now()}-${Math.random().toString(16).slice(2)}`,
    cName: '',
    eName: '',
    title: '',
    department: '',
    mobile: '',
    tel: '',
    email: '',
    qq: '',
    weChat: '',
    isMain: isFirst,
    remark: ''
  } as VendorContactDraft);
};

const removeContact = async (index: number) => {
  try {
    await ElMessageBox.confirm('确定要删除该联系人吗？', '确认删除', { type: 'warning' });
    contacts.value.splice(index, 1);
  } catch (e) {
    if (e !== 'cancel') ElMessage.error('删除失败');
  }
};

const syncContactsForVendor = async (targetVendorId: string) => {
  const existingContacts = await vendorContactApi.getContactsByVendorId(targetVendorId);
  const existingById = new Map(existingContacts.map((c: any) => [c.id, c]));
  const keptIds = new Set<string>();

  // 只保留第一个 isMain=true，避免后端状态不一致
  let defaultAssigned = false;
  const preparedContacts: VendorContactDraft[] = contacts.value.map((c) => ({ ...c }));
  for (const c of preparedContacts) {
    if (c.isMain) {
      if (!defaultAssigned) defaultAssigned = true;
      else c.isMain = false;
    }
  }

  for (const contact of preparedContacts) {
    const cName = (contact.cName || '').trim();
    if (!cName) continue; // 跳过空行，避免插入空联系人

    const payload = {
      cName: cName || undefined,
      title: (contact.title || '').trim(),
      department: (contact.department || '').trim(),
      mobile: (contact.mobile || '').trim(),
      tel: (contact.tel || '').trim(),
      email: (contact.email || '').trim(),
      isMain: !!contact.isMain,
      remark: (contact.remark || '').trim()
    };

    if (contact.id && existingById.has(contact.id)) {
      await vendorContactApi.updateContact(contact.id, payload as any);
      keptIds.add(contact.id);
    } else {
      const created = await vendorContactApi.createContact(targetVendorId, payload as any);
      const createdId = (created as any)?.id || (created as any)?.data?.id;
      if (createdId) keptIds.add(createdId);
    }
  }

  for (const oldContact of existingContacts) {
    if (!keptIds.has(oldContact.id)) {
      await vendorContactApi.deleteContact(oldContact.id);
    }
  }

  // 同步后拉取一次，保持界面数据与数据库一致
  const latest = await vendorContactApi.getContactsByVendorId(targetVendorId);
  contacts.value = latest.map((c: any, idx: number) => ({ ...c, _key: c.id || `srv-${idx}` }));
};

onMounted(() => {
  void fetchVendorDetail();
  const draftId = route.query.draftId;
  if (!isEdit.value && typeof draftId === 'string' && draftId) {
    restoreDraftById(draftId).catch((err: any) => {
      ElMessage.error(err?.message || '草稿恢复失败');
    });
  }
});
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.vendor-edit-page {
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
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

.empty-contacts {
  text-align: center;
  padding: 32px;
  color: $text-muted;
  font-size: 13px;

  svg { margin-bottom: 10px; opacity: 0.4; }
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
    color: $cyan-primary;
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
  color: #C95745;
  font-size: 11px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.15s;

  &:hover { background: rgba(201, 87, 69, 0.15); }
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
