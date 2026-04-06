<template>
  <div class="customer-edit-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
          {{ t('customerEdit.page.back') }}
        </button>
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
            </svg>
          </div>
          <h1 class="page-title">{{ isEdit ? t('customerEdit.page.editTitle') : t('customerEdit.page.createTitle') }}</h1>
        </div>
      </div>
      <div class="header-right">
        <template v-if="isEdit">
          <button class="btn-primary" @click="handleSave">{{ t('customerEdit.page.save') }}</button>
        </template>
        <template v-else>
          <button class="btn-secondary" @click="saveDraftOnly">{{ t('customerEdit.page.saveDraft') }}</button>
          <button class="btn-warning" @click="handleConvertToFormal">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/>
              <polyline points="17 21 17 13 7 13 7 21"/>
              <polyline points="7 3 7 8 15 8"/>
            </svg>
            {{ t('customerEdit.page.convertToFormal') }}
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
          <span class="section-title">{{ t('customerEdit.sections.basicInfo') }}</span>
        </div>
        <div class="section-body">
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item :label="t('customerEdit.fields.customerCode')" prop="customerCode">
                <el-input
                  v-model="formData.customerCode"
                  :placeholder="isEdit ? '' : t('customerEdit.placeholders.customerCodeAuto')"
                  :disabled="true"
                  class="q-input"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('customerEdit.fields.chineseName')" prop="customerName">
                <el-input
                  v-model="formData.customerName"
                  :placeholder="t('customerEdit.placeholders.chineseName')"
                  class="q-input"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('customerEdit.fields.englishName')">
                <el-input
                  v-model="formData.englishOfficialName"
                  :placeholder="t('customerEdit.placeholders.englishName')"
                  class="q-input"
                />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item :label="t('customerEdit.fields.shortName')">
                <el-input
                  v-model="formData.customerShortName"
                  :placeholder="t('customerEdit.placeholders.shortName')"
                  class="q-input"
                />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item :label="t('customerEdit.fields.customerType')" prop="customerType">
                <el-select
                  v-model="formData.customerType"
                  :placeholder="t('customerEdit.placeholders.select')"
                  style="width: 100%"
                  class="q-select"
                >
                  <el-option
                    v-for="opt in customerDict.typeSelectOptions"
                    :key="opt.value"
                    :label="opt.label"
                    :value="opt.value"
                  />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('customerEdit.fields.customerLevel')" prop="customerLevel">
                <el-select
                  v-model="formData.customerLevel"
                  :placeholder="t('customerEdit.placeholders.select')"
                  style="width: 100%"
                  class="q-select"
                >
                  <el-option
                    v-for="opt in customerDict.levelStringOptions"
                    :key="opt.value"
                    :label="opt.label"
                    :value="opt.value"
                  />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('customerEdit.fields.industry')">
                <el-select
                  v-model="formData.industry"
                  :placeholder="t('customerEdit.placeholders.industrySelectOrInput')"
                  style="width: 100%"
                  class="q-select"
                  filterable
                  allow-create
                  default-first-option
                  clearable
                >
                  <el-option
                    v-for="opt in customerDict.industryOptions"
                    :key="opt.value"
                    :label="opt.label"
                    :value="opt.label"
                  />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item :label="t('customerEdit.fields.creditCode')">
                <el-input
                  v-model="formData.unifiedSocialCreditCode"
                  :placeholder="t('customerEdit.placeholders.creditCode')"
                  class="q-input"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('customerEdit.fields.salesPerson')">
                <SalesUserCascader
                  v-model="formData.salesPersonId"
                  :placeholder="t('customerEdit.placeholders.salesPerson')"
                  class="q-select"
                  @change="onSalesPersonChange"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('customerEdit.fields.region')">
                <RegionCascaderWithQuickPick
                  v-model="regionValue"
                  :options="regionOptions"
                  :placeholder="t('customerEdit.placeholders.region')"
                  cascader-class="q-cascader"
                  @change="handleRegionChange"
                />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="24">
              <el-form-item :label="t('customerEdit.fields.remarks')">
                <el-input
                  v-model="formData.remarks"
                  type="textarea"
                  :rows="3"
                  :placeholder="t('customerEdit.placeholders.remarks')"
                  class="q-input"
                />
              </el-form-item>
            </el-col>
          </el-row>
        </div>
      </div>

      <!-- 财务信息 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--amber"></div>
          <span class="section-title">{{ t('customerEdit.sections.financialInfo') }}</span>
        </div>
        <div class="section-body">
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item :label="t('customerEdit.fields.creditLimit')">
                <el-input-number v-model="formData.creditLimit" :min="0" :precision="2" style="width: 100%" class="q-number" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('customerEdit.fields.paymentTerms')">
                <el-input-number v-model="formData.paymentTerms" :min="0" :max="365" style="width: 100%" class="q-number" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('customerEdit.fields.settlementCurrency')">
                <el-select
                  v-model="formData.currency"
                  :placeholder="t('customerEdit.placeholders.select')"
                  style="width: 100%"
                  class="q-select"
                >
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
              <el-form-item :label="t('customerEdit.fields.taxRate')">
                <el-select
                  v-model="formData.taxRate"
                  :placeholder="t('customerEdit.placeholders.select')"
                  style="width: 100%"
                  class="q-select"
                >
                  <el-option
                    v-for="opt in customerDict.taxRateOptions"
                    :key="opt.value"
                    :label="opt.label"
                    :value="opt.value"
                  />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('customerEdit.fields.invoiceType')">
                <el-select
                  v-model="formData.invoiceType"
                  :placeholder="t('customerEdit.placeholders.select')"
                  style="width: 100%"
                  class="q-select"
                >
                  <el-option
                    v-for="opt in customerDict.invoiceTypeOptions"
                    :key="opt.value"
                    :label="opt.label"
                    :value="opt.value"
                  />
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
          <span class="section-title">{{ t('customerEdit.contacts.sectionTitle') }}</span>
          <button type="button" class="btn-add-contact" @click="addContact">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
            {{ t('customerEdit.contacts.addContact') }}
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
            <p>{{ t('customerEdit.contacts.emptyHint') }}</p>
          </div>
          <el-radio-group v-else v-model="mainContactKey" class="customer-contacts-main-group">
          <div
            v-for="(contact, index) in formData.contacts"
            :key="contact._key || index"
            class="contact-item"
          >
            <div class="contact-item-header">
              <span class="contact-index">{{ t('customerEdit.contacts.indexLabel', { n: index + 1 }) }}</span>
              <button type="button" class="btn-remove" @click="removeContact(index)">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="3 6 5 6 21 6"/>
                  <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
                </svg>
                {{ t('customerEdit.contacts.delete') }}
              </button>
            </div>
            <el-row :gutter="16">
              <el-col :span="6">
                <el-form-item
                  :label="t('customerEdit.contacts.name')"
                  :prop="`contacts.${index}.contactName`"
                  :rules="{ required: true, message: t('customerEdit.contacts.nameRequired'), trigger: 'blur' }"
                >
                  <el-input v-model="contact.contactName" :placeholder="t('customerEdit.contacts.namePlaceholder')" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="4">
                <el-form-item :label="t('customerEdit.contacts.gender')">
                  <el-select v-model="contact.gender" :placeholder="t('customerEdit.contacts.genderPlaceholder')" style="width: 100%" class="q-select">
                    <!-- 与后端一致：0=保密、1=男、2=女 -->
                    <el-option :label="t('customerEdit.contacts.genderUndisclosed')" :value="0" />
                    <el-option :label="t('customerEdit.contacts.genderMale')" :value="1" />
                    <el-option :label="t('customerEdit.contacts.genderFemale')" :value="2" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item
                  :label="t('customerEdit.contacts.mobile')"
                  :prop="`contacts.${index}.mobilePhone`"
                  :rules="[{ validator: validateContactMobilePhone, trigger: ['blur', 'change'] }]"
                >
                  <el-input v-model="contact.mobilePhone" :placeholder="t('customerEdit.contacts.mobile')" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item
                  :label="t('customerEdit.contacts.email')"
                  :prop="`contacts.${index}.email`"
                  :rules="[{ validator: validateContactEmail, trigger: ['blur', 'change'] }]"
                >
                  <el-input v-model="contact.email" :placeholder="t('customerEdit.contacts.emailPlaceholder')" class="q-input" />
                </el-form-item>
              </el-col>
            </el-row>
            <el-row :gutter="16">
              <el-col :span="6">
                <el-form-item :label="t('customerEdit.contacts.department')">
                  <el-input v-model="contact.department" :placeholder="t('customerEdit.contacts.departmentPlaceholder')" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item :label="t('customerEdit.contacts.position')">
                  <el-input v-model="contact.position" :placeholder="t('customerEdit.contacts.positionPlaceholder')" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item :label="t('customerEdit.contacts.landline')">
                  <el-input v-model="contact.phone" :placeholder="t('customerEdit.contacts.landlinePlaceholder')" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item label=" ">
                  <el-radio :value="contact._key" class="customer-main-contact-radio">
                    {{ t('customerEdit.contacts.setAsDefault') }}
                  </el-radio>
                </el-form-item>
              </el-col>
            </el-row>
          </div>
          </el-radio-group>
        </div>
      </div>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElNotification, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { customerApi, customerContactApi } from '@/api/customer';
import { draftApi } from '@/api/draft';
import SalesUserCascader from '@/components/SalesUserCascader.vue';
import RegionCascaderWithQuickPick from '@/components/RegionCascaderWithQuickPick.vue';
import { regionData } from '@/data/regions';
import { type CreateCustomerRequest } from '@/types/customer';
import { useCustomerDictStore } from '@/stores/customerDict';
import { runValidatedFormSave } from '@/composables/useFormSubmit';
import { SETTLEMENT_CURRENCY_OPTIONS, CurrencyCode } from '@/constants/currency';
import { logRecentApi } from '@/api/logRecent';
import { CUSTOMER_RECENT_HISTORY_CHANGED_EVENT } from '@/constants/customerRecentHistory';
import {
  REGION_DISTRICT_PLACEHOLDER,
  regionCascaderValueFromFields
} from '@/constants/region';

/** 发票类型：0 = 无需开票（el-select 不用空串，避免显示成「请选择」） */
const INVOICE_TYPE_NONE = 0 as const;

const route = useRoute();
const router = useRouter();
const { t, locale } = useI18n();
const customerDict = useCustomerDictStore();

/** 草稿/接口可能把 isDefault 落成字符串或数字，必须收成布尔 */
function coerceContactIsDefault(v: unknown): boolean {
  if (v === true || v === 1 || v === '1') return true;
  if (v === false || v === 0 || v === '0' || v === '' || v == null) return false;
  if (typeof v === 'string') {
    const u = v.trim().toLowerCase();
    if (u === 'true' || u === 'yes') return true;
    if (u === 'false' || u === 'no') return false;
  }
  return Boolean(v);
}

function newCustomerContactRowKey() {
  return `new-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

/** 默认联系人单选（与新建供应商联系人一致）：稳定 _key 绑定 el-radio-group */
const mainContactKey = ref<string | undefined>(undefined);

function applyDefaultFlagsFromKey(k: string | undefined) {
  for (const c of formData.contacts) {
    c.isDefault = k != null && c._key === k;
  }
}

watch(mainContactKey, (k) => {
  applyDefaultFlagsFromKey(k);
});

function reconcileMainContactKey() {
  const list = formData.contacts;
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
    const def = list.find((c) => coerceContactIsDefault(c.isDefault));
    k = (def?._key ?? list[0]._key) as string;
    mainContactKey.value = k;
  }
  applyDefaultFlagsFromKey(k);
}

function normalizeContactRow(c: any, idx?: number) {
  const hasDef = c && Object.prototype.hasOwnProperty.call(c, 'isDefault');
  const isDefault = hasDef
    ? coerceContactIsDefault(c.isDefault)
    : coerceContactIsDefault(c.isMain ?? false);
  const g = c.gender != null && c.gender !== '' ? Number(c.gender) : NaN;
  const genderUi = g === 1 || g === 2 || g === 0 ? g : 0;
  const _key =
    (c._key as string | undefined) ||
    (c.id as string | undefined) ||
    (idx !== undefined ? `tmp-${idx}` : newCustomerContactRowKey());
  return {
    ...c,
    _key,
    contactName: c.contactName || c.name,
    mobilePhone: c.mobilePhone || c.mobile,
    position: c.position ?? c.title ?? '',
    gender: genderUi,
    isDefault
  };
}

const isEdit = computed(() => !!route.params.id);
const customerId = computed(() => route.params.id as string);
const formRef = ref<FormInstance>();
const currentDraftId = ref('');

const formData = reactive<CreateCustomerRequest & { contacts: any[] }>({
  customerCode: '', customerName: '', englishOfficialName: '', customerShortName: '',
  customerType: 2, customerLevel: 'B', industry: '',
  unifiedSocialCreditCode: '', salesPersonId: '', salesPersonName: '',
  country: '', province: '', city: '', district: '', address: '',
  creditLimit: 0, paymentTerms: 30, currency: 1, taxRate: 13,
  invoiceType: 2, isActive: true, remarks: '', contacts: []
});

const regionValue = ref<string[]>([]);

function normalizeInvoiceTypeModel() {
  if (formData.invoiceType === ('' as any)) {
    formData.invoiceType = INVOICE_TYPE_NONE;
  }
}

/** 结算币别变更时联动税率、发票类型（非 RMB：0 + 无需开票；RMB：13 + 增值税普通发票） */
function applyTaxInvoiceByCurrency(currency: number | undefined) {
  const c = currency ?? CurrencyCode.RMB;
  if (c === CurrencyCode.RMB) {
    formData.taxRate = 13;
    formData.invoiceType = 2;
  } else {
    formData.taxRate = 0;
    formData.invoiceType = INVOICE_TYPE_NONE;
  }
}

watch(
  () => formData.currency,
  (cur) => {
    applyTaxInvoiceByCurrency(cur);
  }
);

const formRules = computed<FormRules>(() => {
  void locale.value;
  return {
    customerName: [
      { required: true, message: t('customerEdit.validation.chineseNameRequired'), trigger: 'blur' },
      { min: 2, max: 100, message: t('customerEdit.validation.chineseNameLength'), trigger: 'blur' }
    ],
    customerType: [{ required: true, message: t('customerEdit.validation.typeRequired'), trigger: 'change' }],
    customerLevel: [{ required: true, message: t('customerEdit.validation.levelRequired'), trigger: 'change' }]
  };
});

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
      customerLevel: customer.customerLevel || 'B',
      customerType: customer.customerType ?? 2,
      salesPersonId: customer.salesPersonId || customerAny.salesUserId,
      unifiedSocialCreditCode: customer.unifiedSocialCreditCode || customerAny.creditCode,
      creditLimit: customer.creditLimit ?? 0,
      paymentTerms: customer.paymentTerms ?? 30,
      currency: customer.currency ?? 1,
      taxRate: customer.taxRate ?? 13,
      invoiceType: customer.invoiceType ?? 2,
      isActive: customer.isActive ?? true,
      remarks: customer.remarks || customerAny.remark,
      englishOfficialName: customerAny.englishOfficialName ?? '',
      contacts: customer.contacts || []
    };
    Object.assign(formData, mappedData);
    normalizeInvoiceTypeModel();
    await customerDict.ensureLoaded();
    formData.industry = await customerDict.resolveIndustryStorageLabel(formData.industry || undefined);
    if (mappedData.province && mappedData.city) {
      regionValue.value = regionCascaderValueFromFields(
        mappedData.province,
        mappedData.city,
        mappedData.district
      );
    }
    if (mappedData.contacts) {
      formData.contacts = mappedData.contacts.map((c: any, idx: number) =>
        normalizeContactRow(c, idx)
      );
      reconcileMainContactKey();
    }
    void customerDict.hydrateCustomerEditForm({
      customerType: formData.customerType,
      customerLevel: formData.customerLevel,
      industry: formData.industry,
      taxRate: formData.taxRate,
      invoiceType: formData.invoiceType
    });
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
    ElNotification.error({
      title: t('customerEdit.messages.loadFailedTitle'),
      message: t('customerEdit.messages.loadFailedMsg')
    });
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
  const isFirst = formData.contacts.length === 0;
  const newKey = newCustomerContactRowKey();
  formData.contacts.push(
    normalizeContactRow({
      _key: newKey,
      contactName: '',
      gender: 0,
      department: '',
      position: '',
      mobilePhone: '',
      phone: '',
      email: '',
      isDefault: isFirst
    })
  );
  if (isFirst) {
    mainContactKey.value = newKey;
  }
};

const validateContactMobilePhone = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
  const v = (value || '').trim();
  if (v === '-') {
    callback();
    return;
  }
  if (!v) {
    callback(new Error(t('customerEdit.contacts.mobileRequired')));
    return;
  }
  if (!mobilePhonePattern.test(v)) {
    callback(new Error(t('customerEdit.contacts.mobileInvalid')));
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
    callback(new Error(t('customerEdit.contacts.emailInvalid')));
    return;
  }
  callback();
};

const removeContact = async (index: number) => {
  try {
    const name =
      formData.contacts[index]?.contactName ||
      t('customerEdit.contacts.indexLabel', { n: index + 1 });
    await ElMessageBox.confirm(
      t('customerEdit.contacts.removeContactConfirm', { name }),
      t('customerEdit.contacts.removeContactTitle'),
      {
        type: 'warning',
        confirmButtonText: t('customerEdit.contacts.removeContactButton'),
        cancelButtonText: t('common.cancel')
      }
    );
    formData.contacts.splice(index, 1);
    reconcileMainContactKey();
  } catch {
    // 用户取消，不做任何操作
  }
};

const buildDraftPayload = () => ({
  ...formData,
  contacts: formData.contacts.map((c: any) => ({ ...c }))
});

const applyDraftPayload = async (payload: any) => {
  Object.assign(formData, payload || {});
  normalizeInvoiceTypeModel();
  formData.contacts = Array.isArray(payload?.contacts)
    ? payload.contacts.map((c: any, idx: number) => normalizeContactRow(c, idx))
    : [];
  reconcileMainContactKey();
  if (formData.province && formData.city) {
    regionValue.value = regionCascaderValueFromFields(
      formData.province,
      formData.city,
      formData.district
    );
  }
  await customerDict.ensureLoaded();
  formData.industry = await customerDict.resolveIndustryStorageLabel(formData.industry || undefined);
  void customerDict.hydrateCustomerEditForm({
    customerType: formData.customerType,
    customerLevel: formData.customerLevel,
    industry: formData.industry,
    taxRate: formData.taxRate,
    invoiceType: formData.invoiceType
  });
};

const saveDraftOnly = async () => {
  try {
    const draft = await draftApi.saveDraft({
      draftId: currentDraftId.value || undefined,
      entityType: 'CUSTOMER',
      draftName: formData.customerName || formData.customerShortName || t('customerEdit.draftDefaultName'),
      payloadJson: JSON.stringify(buildDraftPayload()),
      remark: isEdit.value ? t('customerEdit.draftRemarkFromEdit', { id: customerId.value }) : undefined
    });
    currentDraftId.value = draft.draftId;
    ElNotification.success({
      title: t('customerEdit.messages.draftSavedTitle'),
      message: t('customerEdit.messages.draftSavedMsg', { id: draft.draftId })
    });
  } catch (err: any) {
    ElNotification.error({
      title: t('customerEdit.messages.saveDraftFailedTitle'),
      message: err?.message || t('customerEdit.messages.saveDraftFailed')
    });
  }
};

const restoreDraftById = async (draftId: string) => {
  const draft = await draftApi.getDraftById(draftId);
  if (draft.entityType !== 'CUSTOMER') {
    throw new Error(t('customerEdit.messages.wrongDraftType'));
  }
  await applyDraftPayload(JSON.parse(draft.payloadJson || '{}'));
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
    formatSuccess: () =>
      editing ? t('customerEdit.messages.saveSuccessUpdate') : t('customerEdit.messages.saveSuccessCreate'),
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
      return serverMsg
        ? t('customerEdit.messages.saveErrorPrefix', { msg: serverMsg })
        : t('customerEdit.messages.saveErrorGeneric');
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
  void customerDict.ensureLoaded();
  fetchCustomerDetail();
  const draftId = route.query.draftId;
  if (!isEdit.value && typeof draftId === 'string' && draftId) {
    restoreDraftById(draftId).catch((err: any) => {
      ElNotification.error({
        title: t('customerEdit.messages.restoreFailedTitle'),
        message: err?.message || t('customerEdit.messages.restoreFailed')
      });
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

.customer-contacts-main-group {
  display: block;
  width: 100%;
}

.customer-main-contact-radio {
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
</style>
