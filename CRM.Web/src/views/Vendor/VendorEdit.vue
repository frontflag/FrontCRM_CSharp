<template>
  <div class="vendor-edit-page">
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
          <h1 class="page-title">{{ isEdit ? '编辑供应商' : '新增供应商' }}</h1>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-primary" @click="handleSave">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/>
            <polyline points="17 21 17 13 7 13 7 21"/>
            <polyline points="7 3 7 8 15 8"/>
          </svg>
          保存
        </button>
      </div>
    </div>

    <el-form ref="formRef" :model="formData" :rules="formRules" label-width="100px" class="vendor-form">
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--cyan"></div>
          <span class="section-title">基本信息</span>
        </div>
        <div class="section-body">
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item label="供应商编号" prop="code">
                <el-input
                  v-model="formData.code"
                  :placeholder="isEdit ? '' : '请输入供应商编号'"
                  :disabled="!!isEdit"
                  class="q-input"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="供应商名称" prop="name">
                <el-input v-model="formData.name" placeholder="请输入供应商名称" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="备注">
                <el-input v-model="formData.remark" placeholder="备注" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>
        </div>
      </div>

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
            <el-table-column prop="mobile" label="手机" width="120" />
            <el-table-column prop="email" label="邮箱" min-width="160" show-overflow-tooltip />
            <el-table-column label="主联系人" width="80" align="center">
              <template #default="{ row }">
                <el-tag v-if="row.isMain" type="success" size="small">主</el-tag>
                <span v-else>--</span>
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
    </el-form>

    <VendorContactDialog
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
const formData = reactive({
  code: '',
  name: '',
  remark: ''
});

const formRules = computed<FormRules>(() => ({
  name: [{ required: true, message: '请输入供应商名称', trigger: 'blur' }],
  code: isEdit.value ? [] : [{ required: true, message: '请输入供应商编号', trigger: 'blur' }]
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
    formData.name = data.officialName ?? data.name ?? '';
    formData.remark = data.companyInfo ?? data.remark ?? '';
    contacts.value = data.contacts ?? [];
  } catch (e) {
    ElMessage.error('获取供应商详情失败');
  }
};

const handleSave = async () => {
  const valid = await formRef.value?.validate().catch(() => false);
  if (!valid) return;
  try {
    if (isEdit.value) {
      await vendorApi.updateVendor(vendorId.value, {
        name: formData.name,
        remark: formData.remark
      });
      ElMessage.success('保存成功');
    } else {
      const created = await vendorApi.createVendor({
        code: formData.code?.trim() || undefined,
        name: formData.name.trim(),
        remark: formData.remark?.trim() || undefined
      });
      ElMessage.success('创建成功');
      router.replace(`/vendors/${(created as any).id}/edit`);
      return;
    }
  } catch (error: any) {
    const msg = error?.message || error?.data?.message || '保存失败';
    ElMessage.error(msg);
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
  margin-bottom: 20px;
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
  .page-title { font-size: 20px; font-weight: 600; color: $text-primary; margin: 0; }
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
  cursor: pointer;
  transition: all 0.2s;
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
  padding: 12px 16px;
  border-bottom: 1px solid $border-panel;
  .section-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    &.section-dot--cyan { background: $cyan-primary; }
    &.section-dot--green { background: #46BF91; }
  }
  .section-title { font-size: 14px; font-weight: 600; color: $text-primary; flex: 1; }
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
  cursor: pointer;
  transition: all 0.2s;
  &:hover { background: rgba(0, 212, 255, 0.2); }
}

.section-body { padding: 16px; }

.q-input { width: 100%; }

.empty-contacts {
  text-align: center;
  padding: 24px;
  color: $text-muted;
  font-size: 13px;
}

.contact-table {
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-header-bg-color: #0A1628;
}
.action-btn {
  background: none;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 0 6px;
  margin-right: 8px;
  &:hover { text-decoration: underline; }
  &.danger { color: #C95745; }
}
</style>
