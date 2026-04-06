<template>
  <div class="vendor-edit-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
          {{ t('vendorEdit.back') }}
        </button>
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
              <polyline points="9 22 9 12 15 12 15 22"/>
            </svg>
          </div>
          <h1 class="page-title">{{ isEdit ? t('vendorEdit.titleEdit') : t('vendorEdit.titleCreate') }}</h1>
        </div>
      </div>
      <div class="header-right">
        <button v-if="isEdit" class="btn-ghost" @click="handleRestoreDraft" :disabled="saving">{{ t('vendorEdit.restoreDraft') }}</button>
        <button class="btn-ghost" @click="saveDraftOnly" :disabled="saving">{{ t('vendorEdit.saveDraft') }}</button>
        <button class="btn-ghost" @click="goBack">{{ t('vendorEdit.cancel') }}</button>
        <button class="btn-primary" @click="handleConvertToFormal" :disabled="saving">
          <svg v-if="!saving" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/>
            <polyline points="17 21 17 13 7 13 7 21"/>
            <polyline points="7 3 7 8 15 8"/>
          </svg>
          {{ saving ? t('vendorEdit.saving') : (isEdit ? t('vendorEdit.save') : t('vendorEdit.convertFormal')) }}
        </button>
      </div>
    </div>

    <el-form ref="formRef" :model="formData" :rules="formRules" label-width="110px" class="vendor-form">

      <!-- 基本信息 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--cyan"></div>
          <span class="section-title">{{ t('vendorEdit.sections.basic') }}</span>
        </div>
        <div class="section-body">
          <el-row :gutter="24">
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.vendorCode')">
                <el-input
                  :model-value="isEdit ? formData.code : ''"
                  :placeholder="isEdit ? '' : t('vendorEdit.fields.codeSystemGen')"
                  disabled
                  class="q-input readonly-code"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.officialName')" prop="officialName">
                <el-input v-model="formData.officialName" :placeholder="t('vendorEdit.fields.officialNamePh')" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.englishOfficialName')">
                <el-input
                  v-model="formData.englishOfficialName"
                  :placeholder="t('vendorEdit.fields.englishOfficialNamePh')"
                  class="q-input"
                />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="24">
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.nickName')">
                <el-input v-model="formData.nickName" :placeholder="t('vendorEdit.fields.nickNamePh')" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.industry')">
                <el-select v-model="formData.industry" :placeholder="t('vendorEdit.fields.industryPh')" clearable class="q-select">
                  <el-option
                    v-for="opt in vendorDict.industryOptions"
                    :key="opt.value"
                    :label="opt.label"
                    :value="opt.value"
                  />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.level')">
                <el-select v-model="formData.level" :placeholder="t('vendorEdit.fields.levelPh')" clearable class="q-select">
                  <el-option
                    v-for="opt in vendorDict.levelSelectOptions"
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
              <el-form-item :label="t('vendorEdit.fields.identity')">
                <el-select v-model="formData.credit" :placeholder="t('vendorEdit.fields.identityPh')" clearable class="q-select">
                  <el-option
                    v-for="opt in vendorDict.identitySelectOptions"
                    :key="opt.value"
                    :label="opt.label"
                    :value="opt.value"
                  />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.workflowStatus')">
                <el-select v-model="formData.status" :placeholder="t('vendorEdit.fields.statusPh')" class="q-select">
                  <el-option :label="t('vendorEdit.fields.statusDraft')" :value="0" />
                  <el-option :label="t('vendorEdit.fields.statusNew')" :value="1" />
                  <el-option :label="t('vendorEdit.fields.statusPending')" :value="2" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.purchaser')">
                <PurchaserCascader
                  v-model="formData.purchaseUserId"
                  :placeholder="t('vendorEdit.fields.purchaserPh')"
                  class="q-select"
                  @change="onPurchaserChange"
                />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="24">
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.officeAddress')">
                <el-input v-model="formData.officeAddress" :placeholder="t('vendorEdit.fields.officeAddressPh')" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="16">
              <el-form-item :label="t('vendorEdit.fields.website')">
                <el-input v-model="formData.website" :placeholder="t('vendorEdit.fields.websitePh')" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>
        </div>
      </div>

      <!-- 合作条款 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--amber"></div>
          <span class="section-title">{{ t('vendorEdit.sections.finance') }}</span>
        </div>
        <div class="section-body">
          <el-row :gutter="24">
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.settlementCurrency')">
                <el-select v-model="formData.currency" :placeholder="t('vendorEdit.fields.currencyPh')" class="q-select">
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
              <el-form-item :label="t('vendorEdit.fields.paymentMethod')">
                <el-select v-model="formData.paymentMethod" :placeholder="t('vendorEdit.fields.paymentPh')" class="q-select">
                  <el-option
                    v-for="opt in vendorDict.paymentMethodOptions"
                    :key="opt.value"
                    :label="opt.label"
                    :value="opt.value"
                  />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.paymentDays')">
                <el-input-number v-model="formData.paymentDays" :min="0" :max="365" placeholder="0" class="q-input" style="width:100%" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="24">
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.taxNumber')">
                <el-input v-model="formData.taxNumber" :placeholder="t('vendorEdit.fields.taxNumberPh')" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.bankName')">
                <el-input v-model="formData.bankName" :placeholder="t('vendorEdit.fields.bankNamePh')" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="24">
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.bankAccount')">
                <el-input v-model="formData.bankAccount" :placeholder="t('vendorEdit.fields.bankAccountPh')" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('vendorEdit.fields.bankAccountName')">
                <el-input v-model="formData.bankAccountName" :placeholder="t('vendorEdit.fields.bankAccountNamePh')" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>
        </div>
      </div>

      <!-- 备注 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--purple"></div>
          <span class="section-title">{{ t('vendorEdit.sections.remark') }}</span>
        </div>
        <div class="section-body">
          <el-form-item :label="t('vendorEdit.fields.companyInfo')">
            <el-input
              v-model="formData.companyInfo"
              type="textarea"
              :rows="3"
              :placeholder="t('vendorEdit.fields.companyInfoPh')"
              class="q-input"
            />
          </el-form-item>
          <el-form-item :label="t('vendorEdit.fields.remark')">
            <el-input
              v-model="formData.remark"
              type="textarea"
              :rows="2"
              :placeholder="t('vendorEdit.fields.remarkPh')"
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
        <span class="section-title">{{ t('vendorEdit.sections.contacts') }}</span>
        <button type="button" class="btn-add-contact" @click="addContact">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          {{ t('vendorEdit.contacts.add') }}
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
          <p>{{ t('vendorEdit.contacts.empty') }}</p>
        </div>

        <el-radio-group v-else v-model="mainContactKey" class="vendor-contacts-main-group">
        <div v-for="(contact, index) in contacts" :key="contact._key || index" class="contact-item">
          <div class="contact-item-header">
            <span class="contact-index">{{ t('vendorEdit.contacts.contactIndex', { n: index + 1 }) }}</span>
            <button type="button" class="btn-remove" @click="removeContact(index)">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="3 6 5 6 21 6"/>
                <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
              </svg>
              {{ t('vendorEdit.contacts.remove') }}
            </button>
          </div>

          <el-row :gutter="16">
            <el-col :span="6">
              <el-form-item :label="t('vendorEdit.contacts.name')">
                <el-input v-model="contact.cName" :placeholder="t('vendorEdit.contacts.namePh')" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item :label="t('vendorEdit.contacts.title')">
                <el-input v-model="contact.title" :placeholder="t('vendorEdit.contacts.titlePh')" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item :label="t('vendorEdit.contacts.mobile')">
                <el-input v-model="contact.mobile" :placeholder="t('vendorEdit.contacts.mobilePh')" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item :label="t('vendorEdit.contacts.email')">
                <el-input v-model="contact.email" :placeholder="t('vendorEdit.contacts.emailPh')" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>

          <el-row :gutter="16" style="margin-top: 6px;">
            <el-col :span="6">
              <el-form-item :label="t('vendorEdit.contacts.department')">
                <el-input v-model="contact.department" :placeholder="t('vendorEdit.contacts.departmentPh')" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item :label="t('vendorEdit.contacts.tel')">
                <el-input v-model="contact.tel" :placeholder="t('vendorEdit.contacts.telPh')" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item :label="t('vendorEdit.contacts.remark')">
                <el-input v-model="contact.remark" :placeholder="t('vendorEdit.contacts.remarkPh')" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item label=" ">
                <el-radio :value="contact._key" class="vendor-main-contact-radio">
                  {{ t('vendorEdit.contacts.setMain') }}
                </el-radio>
              </el-form-item>
            </el-col>
          </el-row>
        </div>
        </el-radio-group>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { vendorApi, vendorBankApi, vendorContactApi } from '@/api/vendor';
import { draftApi } from '@/api/draft';
import type { CreateVendorRequest, UpdateVendorRequest, Vendor, VendorContactInfo } from '@/types/vendor';
import { runValidatedFormSave } from '@/composables/useFormSubmit';
import { SETTLEMENT_CURRENCY_OPTIONS } from '@/constants/currency';
import PurchaserCascader from '@/components/PurchaserCascader.vue';
import { useVendorDictStore } from '@/stores/vendorDict';
import { logRecentApi } from '@/api/logRecent';
import { VENDOR_RECENT_HISTORY_CHANGED_EVENT } from '@/constants/vendorRecentHistory';

const route = useRoute();
const router = useRouter();
const { t, locale } = useI18n();

const vendorDict = useVendorDictStore();

/** 与 CustomerEdit 一致：create 路由无 id，:id/edit 有 id */
const isEdit = computed(() => !!route.params.id);
const vendorId = computed(() => route.params.id as string);

const formRef = ref<FormInstance>();
const saving = ref(false);
const currentDraftId = ref('');

const formData = reactive({
  code: '',
  officialName: '',
  englishOfficialName: '',
  nickName: '',
  industry: '',
  level: undefined as number | undefined,
  /** 身份（vendorinfo.Credit） */
  credit: undefined as number | undefined,
  /** 与后端一致：1=新建 2=待审核…；新建页默认新建，便于列表出现「提交审核」 */
  status: 1 as number,
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

const formRules = computed<FormRules>(() => {
  void locale.value;
  return {
    officialName: [{ required: true, message: t('vendorEdit.rules.officialNameRequired'), trigger: 'blur' }]
  };
});

const mobilePhonePattern = /^1[3-9]\d{9}$/;
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function onPurchaserChange(p: { id: string; label: string }) {
  formData.purchaserName = p.label || '';
}

type VendorContactDraft = Omit<VendorContactInfo, 'vendorId' | 'id'> & { id?: string; vendorId?: string; _key?: string };
const contacts = ref<VendorContactDraft[]>([]);

/** 默认联系人单选：与每行 contact.isMain 同步，保证仅一人为 true */
const mainContactKey = ref<string | undefined>(undefined);

function applyMainFlagsFromKey(k: string | undefined) {
  for (const c of contacts.value) {
    c.isMain = k != null && c._key === k;
  }
}

watch(mainContactKey, (k) => {
  applyMainFlagsFromKey(k);
});

function reconcileMainContactKey() {
  const list = contacts.value;
  if (!list.length) {
    mainContactKey.value = undefined;
    return;
  }
  for (let i = 0; i < list.length; i++) {
    if (!list[i]._key) list[i]._key = `row-${i}-${Date.now()}`;
  }
  const keySet = new Set(list.map((c) => c._key).filter(Boolean) as string[]);
  let k = mainContactKey.value;
  if (!k || !keySet.has(k)) {
    const main = list.find((c) => c.isMain);
    k = (main?._key ?? list[0]._key) as string;
    mainContactKey.value = k;
  }
  applyMainFlagsFromKey(k);
}

const goBack = () => router.push({ name: 'VendorList' });

const buildVendorApiPayload = (): CreateVendorRequest & UpdateVendorRequest => ({
  name: formData.officialName.trim(),
  englishOfficialName: formData.englishOfficialName?.trim() || undefined,
  nickName: formData.nickName?.trim(),
  industry: formData.industry || undefined,
  level: formData.level,
  credit: formData.credit,
  /** 新建点「转正式」时若仍为草稿(0)，后端会原样落库为 0，列表不会出现「提交审核」；转正至少应为新建(1) */
  status: !isEdit.value && formData.status === 0 ? 1 : formData.status,
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
    formData.englishOfficialName = data.englishOfficialName ?? '';
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
    reconcileMainContactKey();
    void vendorDict.hydrateVendorEditForm({
      industry: formData.industry,
      level: formData.level,
      credit: formData.credit,
      paymentMethod: formData.paymentMethod
    });
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
    ElMessage.error(t('vendorEdit.messages.fetchFailed'));
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
      _key: c.id || c._key || `tmp-${idx}`
    }));
    reconcileMainContactKey();
  }
  void vendorDict.hydrateVendorEditForm({
    industry: formData.industry,
    level: formData.level,
    credit: formData.credit,
    paymentMethod: formData.paymentMethod
  });
};

const saveDraftOnly = async () => {
  try {
    const draft = await draftApi.saveDraft({
      draftId: currentDraftId.value || undefined,
      entityType: 'VENDOR',
      draftName: formData.officialName || formData.nickName || t('vendorEdit.draftNameDefault'),
      payloadJson: JSON.stringify(buildDraftPayload()),
      remark: isEdit.value ? t('vendorEdit.draftRemarkFromVendor', { id: vendorId.value }) : undefined
    });
    currentDraftId.value = draft.draftId;
    ElMessage.success(t('vendorEdit.messages.draftSaved', { id: draft.draftId }));
  } catch (error: any) {
    ElMessage.error(error?.message || t('vendorEdit.messages.draftSaveFailed'));
  }
};

const restoreDraftById = async (draftId: string) => {
  const draft = await draftApi.getDraftById(draftId);
  if (draft.entityType !== 'VENDOR') throw new Error(t('vendorEdit.messages.wrongDraftType'));
  applyDraftPayload(JSON.parse(draft.payloadJson || '{}'));
  currentDraftId.value = draft.draftId;
};

const handleRestoreDraft = async () => {
  try {
    const { value } = await ElMessageBox.prompt(t('vendorEdit.messages.promptDraftId'), t('vendorEdit.messages.promptRestoreTitle'), {
      confirmButtonText: t('vendorEdit.messages.restore'),
      cancelButtonText: t('common.cancel'),
      inputPlaceholder: 'DraftId'
    });
    if (!value) return;
    await restoreDraftById(value);
    ElMessage.success(t('vendorEdit.messages.draftRestored'));
  } catch (e: any) {
    if (e === 'cancel' || e === 'close') return;
    ElMessage.error(e?.message || t('vendorEdit.messages.draftRestoreFailed'));
  }
};

const handleSave = async () => {
  const editing = isEdit.value;
  await runValidatedFormSave(formRef, {
    loading: saving,
    task: async () => {
      validateVendorContacts();
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
    formatSuccess: (mode) => (mode === 'edit' ? t('vendorEdit.messages.saveSuccess') : t('vendorEdit.messages.createSuccess')),
    onSuccess: (mode) => {
      if (mode === 'create') router.replace({ name: 'VendorList' });
      else void fetchVendorDetail();
    },
    errorMessage: (error: unknown) => {
      const e = error as { message?: string; data?: { message?: string } };
      return e?.message || e?.data?.message || t('vendorEdit.messages.saveFailed');
    }
  });
};

const validateVendorContacts = () => {
  for (let i = 0; i < contacts.value.length; i++) {
    const contact = contacts.value[i];
    const name = (contact.cName || '').trim();
    if (!name) {
      continue;
    }

    const mobile = (contact.mobile || '').trim();
    const email = (contact.email || '').trim();

    if (mobile && mobile !== '-' && !mobilePhonePattern.test(mobile)) {
      throw new Error(t('vendorEdit.messages.contactMobileInvalid', { n: i + 1 }));
    }

    if (email && email !== '-' && !emailPattern.test(email)) {
      throw new Error(t('vendorEdit.messages.contactEmailInvalid', { n: i + 1 }));
    }
  }
};

const handleConvertToFormal = async () => {
  // 需求：只有用户主动点击“保存草稿”才保存草稿；
  // “转正式”应直接保存正式数据，不再自动保存草稿。
  await handleSave();
};

const addContact = () => {
  const isFirst = contacts.value.length === 0;
  const newKey = `new-${Date.now()}-${Math.random().toString(16).slice(2)}`;
  contacts.value.push({
    _key: newKey,
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
  if (isFirst) {
    mainContactKey.value = newKey;
  }
};

const removeContact = async (index: number) => {
  try {
    await ElMessageBox.confirm(t('vendorEdit.messages.deleteContactConfirm'), t('vendorEdit.messages.deleteContactTitle'), {
      type: 'warning'
    });
    contacts.value.splice(index, 1);
    reconcileMainContactKey();
  } catch (e) {
    if (e !== 'cancel') ElMessage.error(t('vendorEdit.messages.deleteFailed'));
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
  reconcileMainContactKey();
};

onMounted(() => {
  void vendorDict.ensureLoaded();
  void fetchVendorDetail();
  const draftId = route.query.draftId;
  if (!isEdit.value && typeof draftId === 'string' && draftId) {
    restoreDraftById(draftId).catch((err: any) => {
      ElMessage.error(err?.message || t('vendorEdit.messages.draftRestoreFailed'));
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

.vendor-contacts-main-group {
  display: block;
  width: 100%;
}

.vendor-main-contact-radio {
  margin-right: 0;
  white-space: nowrap;

  :deep(.el-radio__label) {
    color: $text-secondary !important;
    font-size: 12px;
    padding-left: 8px;
  }
  :deep(.el-radio__inner) {
    border-color: $border-panel !important;
    background: $layer-3 !important;
  }
  :deep(.el-radio__input.is-checked .el-radio__inner) {
    background: $color-mint-green !important;
    border-color: $color-mint-green !important;
  }
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
