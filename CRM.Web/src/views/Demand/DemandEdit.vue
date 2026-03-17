<template>
  <div class="demand-edit-page">
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
              <circle cx="12" cy="12" r="10"/>
              <path d="M12 8v4l3 3"/>
            </svg>
          </div>
          <h1 class="page-title">{{ isEdit ? '编辑需求' : '新增需求' }}</h1>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="handleSave(false)" :disabled="saving">取消</button>
        <button class="btn-primary" @click="handleSave(true)" :disabled="saving">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/>
            <polyline points="17 21 17 13 7 13 7 21"/>
            <polyline points="7 3 7 8 15 8"/>
          </svg>
          {{ saving ? '保存中...' : '保存需求' }}
        </button>
      </div>
    </div>

    <el-form
      ref="formRef"
      :model="formData"
      :rules="formRules"
      label-width="110px"
      class="demand-form"
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
              <el-form-item label="需求编号">
                <el-input
                  v-model="formData.demandCode"
                  placeholder="保存后自动生成"
                  disabled
                  class="q-input"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="客户" prop="customerId">
                <el-select
                  v-model="formData.customerId"
                  placeholder="请输入客户名称搜索"
                  style="width: 100%"
                  filterable
                  remote
                  :remote-method="searchCustomers"
                  :loading="customerSearchLoading"
                  loading-text="搜索中..."
                  no-data-text="未找到匹配客户，请输入名称搜索"
                  class="q-select"
                  @change="onCustomerChange"
                >
                  <el-option
                    v-for="c in customerOptions"
                    :key="c.value"
                    :label="c.label"
                    :value="c.value"
                  />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="需求日期" prop="demandDate">
                <el-date-picker
                  v-model="formData.demandDate"
                  type="date"
                  placeholder="选择日期"
                  style="width: 100%"
                  format="YYYY-MM-DD"
                  value-format="YYYY-MM-DD"
                  class="q-input"
                />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item label="期望交货日期">
                <el-date-picker
                  v-model="formData.expectedDeliveryDate"
                  type="date"
                  placeholder="选择日期"
                  style="width: 100%"
                  format="YYYY-MM-DD"
                  value-format="YYYY-MM-DD"
                  class="q-input"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="需求来源">
                <el-select v-model="formData.source" placeholder="请选择" style="width: 100%" class="q-select">
                  <el-option label="手动录入" :value="1" />
                  <el-option label="导入" :value="2" />
                  <el-option label="邮件" :value="3" />
                  <el-option label="在线" :value="4" />
                  <el-option label="电话" :value="5" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="结算货币">
                <el-select v-model="formData.currency" placeholder="请选择" style="width: 100%" class="q-select">
                  <el-option label="人民币(CNY)" value="CNY" />
                  <el-option label="美元(USD)" value="USD" />
                  <el-option label="欧元(EUR)" value="EUR" />
                  <el-option label="港币(HKD)" value="HKD" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item label="账期(天)">
                <el-input v-model="formData.paymentTerms" placeholder="如: 30天账期" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="质量要求">
                <el-input v-model="formData.qualityRequirements" placeholder="质量要求（可选）" class="q-input" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="认证要求">
                <el-input v-model="formData.certificationRequirements" placeholder="认证要求（可选）" class="q-input" />
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
                  placeholder="备注（可选）"
                  class="q-input"
                />
              </el-form-item>
            </el-col>
          </el-row>
        </div>
      </div>

      <!-- 需求明细 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--amber"></div>
          <span class="section-title">需求明细</span>
          <div class="section-actions">
            <button type="button" class="btn-check-dup" @click="checkDuplicates" v-if="formData.items.length > 0">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
              </svg>
              检查重复
            </button>
            <button type="button" class="btn-add-item" @click="addItem">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
              </svg>
              添加明细
            </button>
          </div>
        </div>
        <div class="section-body">
          <!-- 空状态 -->
          <div class="empty-items" v-if="formData.items.length === 0">
            <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
              <rect x="3" y="3" width="18" height="18" rx="2"/>
              <path d="M3 9h18M9 21V9"/>
            </svg>
            <p>暂无明细，点击右上角"添加明细"按钮添加</p>
          </div>

          <!-- 明细列表 -->
          <div
            v-for="(item, index) in formData.items"
            :key="item._key"
            class="item-card"
            :class="{ 'item-card--dup': item._isDuplicate }"
          >
            <div class="item-card-header">
              <span class="item-index">第 {{ index + 1 }} 行</span>
              <div class="item-actions">
                <span class="dup-badge" v-if="item._isDuplicate">
                  <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/>
                    <line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>
                  </svg>
                  重复物料
                </span>
                <button type="button" class="btn-remove" @click="removeItem(index)">
                  <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polyline points="3 6 5 6 21 6"/>
                    <path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6"/>
                    <path d="M10 11v6M14 11v6"/>
                  </svg>
                  删除
                </button>
              </div>
            </div>

            <el-row :gutter="16">
              <el-col :span="6">
                <el-form-item
                  label="物料编码"
                  :prop="`items.${index}.materialCode`"
                  :rules="[{ required: true, message: '请输入物料编码', trigger: 'blur' }]"
                >
                  <el-input v-model="item.materialCode" placeholder="物料编码" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item label="物料名称">
                  <el-input v-model="item.materialName" placeholder="物料名称" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item label="规格型号">
                  <el-input v-model="item.specification" placeholder="规格型号" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item label="客户物料码">
                  <el-input v-model="item.customerMaterialCode" placeholder="客户物料码" class="q-input" />
                </el-form-item>
              </el-col>
            </el-row>

            <el-row :gutter="16">
              <el-col :span="4">
                <el-form-item
                  label="数量"
                  :prop="`items.${index}.quantity`"
                  :rules="[{ required: true, message: '请输入数量', trigger: 'blur' }]"
                >
                  <el-input-number
                    v-model="item.quantity"
                    :min="0.0001"
                    :precision="4"
                    style="width: 100%"
                    class="q-number"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="4">
                <el-form-item label="单位">
                  <el-select v-model="item.unit" placeholder="单位" style="width: 100%" class="q-select">
                    <el-option label="PCS" value="PCS" />
                    <el-option label="SET" value="SET" />
                    <el-option label="KG" value="KG" />
                    <el-option label="M" value="M" />
                    <el-option label="BOX" value="BOX" />
                    <el-option label="ROLL" value="ROLL" />
                    <el-option label="其他" value="OTHER" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="4">
                <el-form-item label="目标单价">
                  <el-input-number
                    v-model="item.targetUnitPrice"
                    :min="0"
                    :precision="4"
                    style="width: 100%"
                    class="q-number"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="4">
                <el-form-item label="品牌要求">
                  <el-input v-model="item.brandRequirement" placeholder="品牌要求" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="4">
                <el-form-item label="产地要求">
                  <el-input v-model="item.originRequirement" placeholder="产地要求" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="4">
                <el-form-item label="交货日期">
                  <el-date-picker
                    v-model="item.deliveryDate"
                    type="date"
                    placeholder="交货日期"
                    style="width: 100%"
                    format="YYYY-MM-DD"
                    value-format="YYYY-MM-DD"
                    class="q-input"
                  />
                </el-form-item>
              </el-col>
            </el-row>

            <el-row :gutter="16">
              <el-col :span="4">
                <el-form-item label="MOQ">
                  <el-input-number
                    v-model="item.moq"
                    :min="0"
                    :precision="4"
                    style="width: 100%"
                    class="q-number"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="20">
                <el-form-item label="明细备注">
                  <el-input v-model="item.itemRemarks" placeholder="明细备注" class="q-input" />
                </el-form-item>
              </el-col>
            </el-row>
          </div>

          <!-- 底部添加按钮 -->
          <div class="add-item-footer" v-if="formData.items.length > 0">
            <button type="button" class="btn-add-item-footer" @click="addItem">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
              </svg>
              添加明细行
            </button>
          </div>
        </div>
      </div>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElNotification, ElMessageBox } from 'element-plus'
import { demandApi } from '@/api/demand'
import type { CreateDemandRequest, CreateDemandItemRequest } from '@/types/demand'

const router = useRouter()
const route = useRoute()
const formRef = ref()
const saving = ref(false)

const isEdit = computed(() => !!route.params.id)

// 客户选项（远程搜索）
const customerOptions = ref<{ value: string; label: string }[]>([])
const customerSearchLoading = ref(false)
let customerSearchTimer: ReturnType<typeof setTimeout> | null = null

// 表单数据
const formData = reactive<{
  demandCode: string
  customerId: string
  demandDate: string
  expectedDeliveryDate: string
  source: number
  currency: string
  paymentTerms: string
  qualityRequirements: string
  certificationRequirements: string
  remarks: string
  items: Array<CreateDemandItemRequest & { _key: number; _isDuplicate?: boolean; specification?: string; targetUnitPrice?: number; itemRemarks?: string }>
}>({
  demandCode: '',
  customerId: '',
  demandDate: new Date().toISOString().split('T')[0],
  expectedDeliveryDate: '',
  source: 1,
  currency: 'CNY',
  paymentTerms: '',
  qualityRequirements: '',
  certificationRequirements: '',
  remarks: '',
  items: []
})

let _keyCounter = 0
function newItem(): CreateDemandItemRequest & { _key: number; _isDuplicate?: boolean; specification?: string; targetUnitPrice?: number; itemRemarks?: string } {
  return {
    _key: ++_keyCounter,
    _isDuplicate: false,
    materialCode: '',
    materialName: '',
    specification: '',
    customerMaterialCode: '',
    quantity: 1,
    unit: 'PCS',
    targetUnitPrice: undefined,
    brandRequirement: '',
    originRequirement: '',
    deliveryDate: '',
    moq: undefined,
    itemRemarks: ''
  }
}

function addItem() {
  formData.items.push(newItem())
}

function removeItem(index: number) {
  ElMessageBox.confirm(
    `确定删除第 ${index + 1} 行明细吗？`,
    '删除确认',
    { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'warning' }
  ).then(() => {
    formData.items.splice(index, 1)
  }).catch(() => {})
}

// 检查重复物料
async function checkDuplicates() {
  if (!formData.customerId) {
    ElNotification.warning({ title: '提示', message: '请先选择客户后再检查重复物料' })
    return
  }
  try {
    let dupCount = 0
    for (const item of formData.items) {
      if (!item.materialCode) continue
      try {
        const result = await demandApi.checkDuplicateMaterial({
          customerId: formData.customerId,
          materialCode: item.materialCode
        })
        item._isDuplicate = result.isDuplicate ?? false
        if (item._isDuplicate) dupCount++
      } catch { /* 单条检查失败静默处理 */ }
    }
    if (dupCount > 0) {
      ElNotification.warning({ title: '发现重复物料', message: `共 ${dupCount} 行物料在近期已有需求记录，请确认` })
    } else {
      ElNotification.success({ title: '检查完成', message: '未发现重复物料' })
    }
  } catch {
    ElNotification.error({ title: '检查失败', message: '重复物料检查失败，请稍后重试' })
  }
}

function onCustomerChange() {
  // 客户变更时清除重复标记
  formData.items.forEach(item => { item._isDuplicate = false })
}

// 表单校验规则
const formRules = {
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }],
  demandDate: [{ required: true, message: '请选择需求日期', trigger: 'change' }]
}

// 远程搜索客户
async function searchCustomers(query: string) {
  if (customerSearchTimer) clearTimeout(customerSearchTimer)
  if (!query || query.trim().length < 1) {
    customerOptions.value = []
    return
  }
  customerSearchTimer = setTimeout(async () => {
    customerSearchLoading.value = true
    try {
      const { customerApi } = await import('@/api/customer')
      const res = await customerApi.searchCustomers({
        pageNumber: 1,
        pageSize: 20,
        searchTerm: query.trim()
      })
      customerOptions.value = (res.items || []).map((c: any) => ({
        value: c.id,
        label: c.customerName || c.officialName || c.name || '未知客户'
      }))
    } catch {
      customerOptions.value = []
    } finally {
      customerSearchLoading.value = false
    }
  }, 300)
}

// 加载编辑数据
async function loadDemand() {
  if (!isEdit.value) return
  try {
    const data = await demandApi.getDemandById(route.params.id as string)
    formData.demandCode = data.demandCode || ''
    formData.customerId = data.customerId || ''
    formData.demandDate = data.demandDate?.split('T')[0] || ''
    formData.expectedDeliveryDate = data.expectedDeliveryDate?.split('T')[0] || ''
    formData.source = data.source || 1
    formData.currency = data.currency || 'CNY'
    formData.paymentTerms = data.paymentTerms || ''
    formData.qualityRequirements = data.qualityRequirements || ''
    formData.certificationRequirements = data.certificationRequirements || ''
    formData.remarks = data.remark || ''
    // 加载明细
    const items = await demandApi.getDemandItemsByDemandId(data.id)
    formData.items = (items || []).map((item: any) => ({
      ...item,
      _key: ++_keyCounter,
      _isDuplicate: false
    }))
  } catch {
    ElNotification.error({ title: '加载失败', message: '需求数据加载失败，请刷新重试' })
  }
}

async function handleSave(submit: boolean) {
  if (!submit) {
    goBack()
    return
  }
  try {
    await formRef.value?.validate()
  } catch {
    ElNotification.warning({ title: '校验失败', message: '请检查表单填写是否完整，必填项不能为空' })
    return
  }
  if (formData.items.length === 0) {
    ElNotification.warning({ title: '提示', message: '请至少添加一条需求明细' })
    return
  }
  saving.value = true
  try {
    const payload: CreateDemandRequest = {
      customerId: formData.customerId,
      demandDate: formData.demandDate,
      expectedDeliveryDate: formData.expectedDeliveryDate || undefined,
      source: formData.source,
      currency: formData.currency,
      paymentTerms: formData.paymentTerms || undefined,
      qualityRequirements: formData.qualityRequirements || undefined,
      certificationRequirements: formData.certificationRequirements || undefined,
      remark: formData.remarks || undefined,
      items: formData.items.map(item => ({
        materialCode: item.materialCode,
        materialName: item.materialName,
        specification: item.specification,
        customerMaterialCode: item.customerMaterialCode,
        quantity: item.quantity,
        unit: item.unit,
        targetUnitPrice: item.targetUnitPrice,
        brandRequirement: item.brandRequirement,
        originRequirement: item.originRequirement,
        deliveryDate: item.deliveryDate || undefined,
        moq: item.moq,
        remark: item.itemRemarks
      }))
    }
    if (isEdit.value) {
      await demandApi.updateDemand(route.params.id as string, payload)
    } else {
      await demandApi.createDemand(payload)
    }
    ElNotification.success({
      title: '保存成功',
      message: isEdit.value ? '需求信息已成功更新' : '需求已成功创建'
    })
    setTimeout(() => router.push('/demands'), 1500)
  } catch (err: any) {
    ElNotification.error({
      title: '保存失败',
      message: err?.message || '保存失败，请稍后重试'
    })
  } finally {
    saving.value = false
  }
}

function goBack() {
  router.push('/demands')
}

onMounted(async () => {
  // 编辑模式：先加载需求数据，再预填客户选项
  if (isEdit.value) {
    await loadDemand()
    // 编辑模式下预填当前客户选项，使选择框能正确显示客户名称
    if (formData.customerId) {
      try {
        const { customerApi } = await import('@/api/customer')
        const res = await customerApi.searchCustomers({ pageNumber: 1, pageSize: 1, searchTerm: formData.customerId })
        if (res.items?.length) {
          const c = res.items[0]
          customerOptions.value = [{ value: c.id, label: c.customerName || (c as any).officialName || '未知客户' }]
        }
      } catch { /* 静默失败 */ }
    }
  } else {
    // 新增时默认添加一行
    addItem()
  }
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+SC:wght@300;400;500&display=swap');

.demand-edit-page {
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

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &:disabled { opacity: 0.6; cursor: not-allowed; }
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

  &:hover:not(:disabled) {
    background: rgba(255, 255, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.25);
  }

  &:disabled { opacity: 0.6; cursor: not-allowed; }
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

.section-actions {
  display: flex;
  gap: 8px;
}

.section-body {
  padding: 20px;
}

// ---- 明细操作按钮 ----
.btn-add-item {
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

.btn-check-dup {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 10px;
  background: rgba(0, 212, 255, 0.08);
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: $border-radius-sm;
  color: $cyan-primary;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(0, 212, 255, 0.15);
  }
}

// ---- 空状态 ----
.empty-items {
  text-align: center;
  padding: 32px;
  color: $text-muted;

  svg { margin-bottom: 10px; opacity: 0.4; }
  p { font-size: 13px; margin: 0; }
}

// ---- 明细条目 ----
.item-card {
  background: $layer-3;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  padding: 16px;
  margin-bottom: 12px;
  transition: border-color 0.2s;

  &:last-child { margin-bottom: 0; }

  &--dup {
    border-color: rgba(201, 154, 69, 0.4);
    background: rgba(201, 154, 69, 0.04);
  }
}

.item-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;

  .item-index {
    font-size: 12px;
    font-weight: 500;
    color: $color-ice-blue;
    letter-spacing: 0.5px;
  }

  .item-actions {
    display: flex;
    align-items: center;
    gap: 8px;
  }
}

.dup-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 7px;
  background: rgba(201, 154, 69, 0.12);
  border: 1px solid rgba(201, 154, 69, 0.3);
  border-radius: 4px;
  color: $color-amber;
  font-size: 11px;
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

// ---- 底部添加按钮 ----
.add-item-footer {
  text-align: center;
  padding-top: 16px;
  border-top: 1px solid rgba(255, 255, 255, 0.04);
  margin-top: 4px;
}

.btn-add-item-footer {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 6px 16px;
  background: transparent;
  border: 1px dashed rgba(0, 212, 255, 0.25);
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    border-color: rgba(0, 212, 255, 0.5);
    color: $cyan-primary;
    background: rgba(0, 212, 255, 0.05);
  }
}

// ---- Element Plus 覆写 ----
.demand-form {
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

  :deep(.el-input__wrapper.is-disabled) { opacity: 0.5; }
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
</style>
