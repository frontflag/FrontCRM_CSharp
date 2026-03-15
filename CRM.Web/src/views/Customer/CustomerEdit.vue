<template>
  <div class="customer-edit-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <el-button @click="goBack">
          <el-icon><ArrowLeft /></el-icon>返回
        </el-button>
        <h1>{{ isEdit ? '编辑客户' : '新增客户' }}</h1>
      </div>
      <div class="header-right">
        <el-button @click="handleSave(false)">保存草稿</el-button>
        <el-button type="primary" @click="handleSave(true)">保存并提交</el-button>
      </div>
    </div>

    <el-form
      ref="formRef"
      :model="formData"
      :rules="formRules"
      label-width="120px"
      class="customer-form"
    >
      <!-- 基本信息 -->
      <el-card class="form-card">
        <template #header>
          <span>基本信息</span>
        </template>
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="客户编号" prop="customerCode">
              <el-input
                v-model="formData.customerCode"
                placeholder="系统自动生成"
                :disabled="isEdit"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="客户名称" prop="customerName">
              <el-input v-model="formData.customerName" placeholder="请输入客户名称" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="客户简称">
              <el-input v-model="formData.customerShortName" placeholder="请输入客户简称" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="客户类型" prop="customerType">
              <el-select v-model="formData.customerType" placeholder="请选择" style="width: 100%">
                <el-option label="企业客户" :value="0" />
                <el-option label="个人客户" :value="1" />
                <el-option label="政府/机构" :value="2" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="客户等级" prop="customerLevel">
              <el-select v-model="formData.customerLevel" placeholder="请选择" style="width: 100%">
                <el-option label="VIP" value="VIP" />
                <el-option label="重要" value="Important" />
                <el-option label="普通" value="Normal" />
                <el-option label="潜在客户" value="Lead" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="行业">
              <el-select v-model="formData.industry" placeholder="请选择" style="width: 100%">
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
              <el-input v-model="formData.unifiedSocialCreditCode" placeholder="请输入统一社会信用代码" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="所属业务员">
              <el-select-v2
                v-model="formData.salesPersonId"
                :options="salesPersonOptions"
                placeholder="请选择业务员"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="地区">
              <el-cascader
                v-model="regionValue"
                :options="regionOptions"
                placeholder="请选择地区"
                style="width: 100%"
                @change="handleRegionChange"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
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
      </el-card>

      <!-- 财务信息 -->
      <el-card class="form-card">
        <template #header>
          <span>财务信息</span>
        </template>
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="信用额度">
              <el-input-number
                v-model="formData.creditLimit"
                :min="0"
                :precision="2"
                style="width: 100%"
                placeholder="请输入信用额度"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="账期(天)">
              <el-input-number
                v-model="formData.paymentTerms"
                :min="0"
                :max="365"
                style="width: 100%"
                placeholder="请输入账期天数"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="结算货币">
              <el-select v-model="formData.currency" placeholder="请选择" style="width: 100%">
                <el-option label="人民币(CNY)" :value="1" />
                <el-option label="美元(USD)" :value="2" />
                <el-option label="欧元(EUR)" :value="3" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="税率(%)">
              <el-input-number
                v-model="formData.taxRate"
                :min="0"
                :max="100"
                :precision="2"
                style="width: 100%"
                placeholder="请输入税率"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="发票类型">
              <el-select v-model="formData.invoiceType" placeholder="请选择" style="width: 100%">
                <el-option label="增值税专用发票" :value="1" />
                <el-option label="增值税普通发票" :value="2" />
                <el-option label="电子发票" :value="3" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
      </el-card>

      <!-- 联系人信息 -->
      <el-card class="form-card">
        <template #header>
          <div class="card-header">
            <span>联系人信息</span>
            <el-button type="primary" size="small" @click="addContact">
              <el-icon><Plus /></el-icon>添加联系人
            </el-button>
          </div>
        </template>
        <div
          v-for="(contact, index) in formData.contacts"
          :key="index"
          class="contact-item"
        >
          <el-row :gutter="20">
            <el-col :span="6">
              <el-form-item :label="index === 0 ? '姓名' : ''" :prop="`contacts.${index}.contactName`" :rules="{ required: true, message: '请输入姓名', trigger: 'blur' }">
                <el-input v-model="contact.contactName" placeholder="姓名" />
              </el-form-item>
            </el-col>
            <el-col :span="4">
              <el-form-item :label="index === 0 ? '性别' : ''">
                <el-select v-model="contact.gender" placeholder="性别" style="width: 100%">
                  <el-option label="男" :value="0" />
                  <el-option label="女" :value="1" />
                  <el-option label="保密" :value="2" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item :label="index === 0 ? '手机' : ''" :prop="`contacts.${index}.mobilePhone`" :rules="{ required: true, message: '请输入手机', trigger: 'blur' }">
                <el-input v-model="contact.mobilePhone" placeholder="手机" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item :label="index === 0 ? '邮箱' : ''">
                <el-input v-model="contact.email" placeholder="邮箱" />
              </el-form-item>
            </el-col>
            <el-col :span="2">
              <el-form-item :label="index === 0 ? '操作' : ''">
                <el-button type="danger" link @click="removeContact(index)">
                  <el-icon><Delete /></el-icon>
                </el-button>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="6">
              <el-form-item>
                <el-input v-model="contact.department" placeholder="部门" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item>
                <el-input v-model="contact.position" placeholder="职位" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item>
                <el-input v-model="contact.phone" placeholder="固话" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item>
                <el-checkbox v-model="contact.isDefault">设为默认联系人</el-checkbox>
              </el-form-item>
            </el-col>
          </el-row>
        </div>
        <el-empty v-if="formData.contacts.length === 0" description="暂无联系人，请点击上方按钮添加" />
      </el-card>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { ArrowLeft, Plus, Delete } from '@element-plus/icons-vue';
import { customerApi } from '@/api/customer';
import type { CreateCustomerRequest } from '@/types/customer';

const route = useRoute();
const router = useRouter();

// 判断是编辑还是新增
const isEdit = computed(() => !!route.params.id);
const customerId = computed(() => route.params.id as string);

// 表单引用
const formRef = ref<FormInstance>();

// 表单数据
const formData = reactive<CreateCustomerRequest & { contacts: any[] }>({
  customerCode: '',
  customerName: '',
  customerShortName: '',
  customerType: 0,
  customerLevel: 'Normal',
  industry: '',
  unifiedSocialCreditCode: '',
  salesPersonId: '',
  salesPersonName: '',
  country: '',
  province: '',
  city: '',
  district: '',
  address: '',
  creditLimit: 0,
  paymentTerms: 30,
  currency: 1,
  taxRate: 13,
  invoiceType: 2,
  isActive: true,
  remarks: '',
  contacts: []
});

// 地区选择值
const regionValue = ref<string[]>([]);

// 表单校验规则
const formRules: FormRules = {
  customerName: [
    { required: true, message: '请输入客户名称', trigger: 'blur' },
    { min: 2, max: 100, message: '长度在 2 到 100 个字符', trigger: 'blur' }
  ],
  customerType: [
    { required: true, message: '请选择客户类型', trigger: 'change' }
  ],
  customerLevel: [
    { required: true, message: '请选择客户等级', trigger: 'change' }
  ]
};

// 业务员选项（模拟数据）
const salesPersonOptions = ref([
  { value: 'user-001', label: '张三' },
  { value: 'user-002', label: '李四' },
  { value: 'user-003', label: '王五' }
]);

// 地区选项（简化版）
const regionOptions = ref([
  {
    value: '北京市',
    label: '北京市',
    children: [
      { value: '北京市', label: '北京市', children: [
        { value: '朝阳区', label: '朝阳区' },
        { value: '海淀区', label: '海淀区' },
        { value: '东城区', label: '东城区' },
        { value: '西城区', label: '西城区' }
      ]}
    ]
  },
  {
    value: '上海市',
    label: '上海市',
    children: [
      { value: '上海市', label: '上海市', children: [
        { value: '浦东新区', label: '浦东新区' },
        { value: '黄浦区', label: '黄浦区' },
        { value: '徐汇区', label: '徐汇区' }
      ]}
    ]
  },
  {
    value: '广东省',
    label: '广东省',
    children: [
      { value: '广州市', label: '广州市', children: [
        { value: '天河区', label: '天河区' },
        { value: '越秀区', label: '越秀区' }
      ]},
      { value: '深圳市', label: '深圳市', children: [
        { value: '南山区', label: '南山区' },
        { value: '福田区', label: '福田区' }
      ]}
    ]
  }
]);

// 获取客户详情
const fetchCustomerDetail = async () => {
  if (!isEdit.value) return;
  try {
    const customer = await customerApi.getCustomerById(customerId.value);
    
    // 映射后端字段到前端表单字段
    const mappedData: any = {
      ...customer,
      customerName: customer.customerName || customer.officialName,
      customerShortName: customer.customerShortName || customer.nickName,
      customerLevel: customer.customerLevel || (customer.level ? ['', 'D', 'C', 'B', 'BPO', 'VIP', 'VPO'][customer.level] : 'Normal'),
      customerType: customer.customerType ?? customer.type ?? 0,
      salesPersonId: customer.salesPersonId || customer.salesUserId,
      salesPersonName: customer.salesPersonName,
      unifiedSocialCreditCode: customer.unifiedSocialCreditCode || customer.creditCode,
      creditLimit: customer.creditLimit ?? customer.creditLine ?? 0,
      paymentTerms: customer.paymentTerms ?? customer.payment ?? 30,
      currency: customer.currency ?? customer.tradeCurrency ?? 1,
      taxRate: customer.taxRate ?? 13,
      invoiceType: customer.invoiceType ?? 2,
      isActive: customer.isActive ?? (customer.status === 1),
      remarks: customer.remarks || customer.remark,
      country: customer.countryName || customer.country,
      contacts: customer.contacts || []
    };
    
    Object.assign(formData, mappedData);
    
    if (mappedData.province && mappedData.city && mappedData.district) {
      regionValue.value = [mappedData.province, mappedData.city, mappedData.district];
    }
    if (mappedData.contacts) {
      formData.contacts = mappedData.contacts.map((c: any) => ({ 
        ...c,
        contactName: c.contactName || c.name,
        mobilePhone: c.mobilePhone || c.mobile
      }));
    }
  } catch (error) {
    console.error('获取客户详情失败:', error);
    ElMessage.error('获取客户详情失败');
  }
};

// 地区变更
const handleRegionChange = (value: string[]) => {
  if (value && value.length >= 3) {
    formData.province = value[0];
    formData.city = value[1];
    formData.district = value[2];
    formData.country = '中国';
  }
};

// 添加联系人
const addContact = () => {
  formData.contacts.push({
    contactName: '',
    gender: 0,
    department: '',
    position: '',
    mobilePhone: '',
    phone: '',
    email: '',
    isDefault: formData.contacts.length === 0
  });
};

// 删除联系人
const removeContact = (index: number) => {
  formData.contacts.splice(index, 1);
};

// 保存
const handleSave = async (_submit: boolean) => {
  const valid = await formRef.value?.validate();
  if (!valid) return;

  try {
    if (isEdit.value) {
      await customerApi.updateCustomer(customerId.value, formData);
      ElMessage.success('更新成功');
    } else {
      await customerApi.createCustomer(formData);
      ElMessage.success('创建成功');
    }
    router.push('/customers');
  } catch (error) {
    console.error('保存失败:', error);
    ElMessage.error('保存失败');
  }
};

// 返回
const goBack = () => {
  router.back();
};

// 初始化
onMounted(() => {
  fetchCustomerDetail();
});
</script>

<style scoped lang="scss">
.customer-edit-page {
  padding: 20px;

  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;

    .header-left {
      display: flex;
      align-items: center;
      gap: 12px;

      h1 {
        margin: 0;
        font-size: 24px;
        font-weight: 600;
      }
    }

    .header-right {
      display: flex;
      gap: 10px;
    }
  }

  .customer-form {
    .form-card {
      margin-bottom: 20px;

      .card-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
      }
    }

    .contact-item {
      padding: 16px;
      margin-bottom: 16px;
      background-color: #f5f7fa;
      border-radius: 4px;

      &:last-child {
        margin-bottom: 0;
      }
    }
  }
}
</style>
