<template>
  <div class="demand-edit-page" v-loading="pageLoading">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="page-header-left">
        <el-button :icon="ArrowLeft" text @click="handleBack">返回</el-button>
        <h1 class="page-title">{{ isEdit ? '编辑需求' : '新增需求' }}</h1>
        <el-tag v-if="isEdit" type="info" size="small">{{ demandCode }}</el-tag>
      </div>
      <div class="page-header-right">
        <el-button @click="handleBack">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">保存需求</el-button>
      </div>
    </div>

    <el-form ref="formRef" :model="form" :rules="rules" label-width="110px" class="demand-form">
      <!-- 基本信息 -->
      <el-card class="form-card" shadow="never">
        <template #header><span class="card-title">基本信息</span></template>
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="客户" prop="customerId" required>
              <el-select
                v-model="form.customerId"
                placeholder="请选择客户"
                filterable
                remote
                :remote-method="searchCustomers"
                :loading="customerSearching"
                style="width: 100%"
                @change="handleCustomerChange"
              >
                <el-option
                  v-for="c in customerOptions"
                  :key="c.id"
                  :label="c.customerName"
                  :value="c.id"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="需求日期" prop="demandDate" required>
              <el-date-picker
                v-model="form.demandDate"
                type="date"
                placeholder="选择日期"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="期望交期">
              <el-date-picker
                v-model="form.expectedDeliveryDate"
                type="date"
                placeholder="选择日期"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="需求来源">
              <el-select v-model="form.source" placeholder="请选择" style="width: 100%">
                <el-option label="手动录入" :value="1" />
                <el-option label="导入" :value="2" />
                <el-option label="邮件" :value="3" />
                <el-option label="在线" :value="4" />
                <el-option label="电话" :value="5" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="货币">
              <el-select v-model="form.currency" placeholder="请选择" style="width: 100%">
                <el-option label="人民币 (CNY)" value="CNY" />
                <el-option label="美元 (USD)" value="USD" />
                <el-option label="欧元 (EUR)" value="EUR" />
                <el-option label="港币 (HKD)" value="HKD" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="付款条款">
              <el-input v-model="form.paymentTerms" placeholder="如：30天账期" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="质量要求">
              <el-input v-model="form.qualityRequirements" placeholder="质量要求（可选）" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="认证要求">
              <el-input v-model="form.certificationRequirements" placeholder="认证要求（可选）" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="备注">
              <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="备注（可选）" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-card>

      <!-- 需求明细 -->
      <el-card class="form-card" shadow="never">
        <template #header>
          <div class="items-header">
            <span class="card-title">需求明细</span>
            <div class="items-header-actions">
              <el-tag type="info" size="small">{{ form.items.length }} 条</el-tag>
              <el-button size="small" type="primary" :icon="Plus" @click="addItem">添加明细</el-button>
            </div>
          </div>
        </template>

        <div v-if="form.items.length === 0" class="empty-items">
          <el-icon class="empty-icon"><Document /></el-icon>
          <p>暂无明细，请点击「添加明细」</p>
        </div>

        <div v-else>
          <div
            v-for="(item, index) in form.items"
            :key="index"
            class="item-row"
          >
            <div class="item-row-header">
              <span class="item-row-no">第 {{ index + 1 }} 行</span>
              <div class="item-row-actions">
                <el-button
                  size="small"
                  type="warning"
                  text
                  :loading="checkingDuplicate[index]"
                  @click="checkDuplicate(index)"
                >
                  检查重复
                </el-button>
                <el-button size="small" type="danger" text :icon="Delete" @click="removeItem(index)">删除</el-button>
              </div>
            </div>

            <!-- 重复物料警告 -->
            <el-alert
              v-if="duplicateWarnings[index]"
              :title="duplicateWarnings[index]"
              type="warning"
              show-icon
              :closable="true"
              class="duplicate-alert"
              @close="duplicateWarnings[index] = ''"
            />

            <el-row :gutter="16">
              <el-col :span="6">
                <el-form-item :label="'物料编码'" :prop="`items.${index}.materialCode`">
                  <el-input v-model="item.materialCode" placeholder="物料编码" @blur="() => checkDuplicateOnBlur(index)" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item :label="'物料名称'">
                  <el-input v-model="item.materialName" placeholder="物料名称" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item :label="'规格型号'">
                  <el-input v-model="item.materialModel" placeholder="规格型号" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item :label="'客户物料编码'">
                  <el-input v-model="item.customerMaterialCode" placeholder="客户物料编码" />
                </el-form-item>
              </el-col>
              <el-col :span="4">
                <el-form-item :label="'数量'" :prop="`items.${index}.quantity`" required>
                  <el-input-number
                    v-model="item.quantity"
                    :min="0.0001"
                    :precision="4"
                    style="width: 100%"
                    controls-position="right"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="3">
                <el-form-item :label="'单位'">
                  <el-select v-model="item.unit" placeholder="单位" style="width: 100%">
                    <el-option label="PCS" value="PCS" />
                    <el-option label="KG" value="KG" />
                    <el-option label="M" value="M" />
                    <el-option label="SET" value="SET" />
                    <el-option label="BOX" value="BOX" />
                    <el-option label="REEL" value="REEL" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="4">
                <el-form-item :label="'目标单价'">
                  <el-input-number
                    v-model="item.targetPrice"
                    :min="0"
                    :precision="4"
                    style="width: 100%"
                    controls-position="right"
                    placeholder="目标单价"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="4">
                <el-form-item :label="'品牌要求'">
                  <el-input v-model="item.brandRequirement" placeholder="品牌要求" />
                </el-form-item>
              </el-col>
              <el-col :span="4">
                <el-form-item :label="'产地要求'">
                  <el-input v-model="item.originRequirement" placeholder="产地要求" />
                </el-form-item>
              </el-col>
              <el-col :span="5">
                <el-form-item :label="'交货日期'">
                  <el-date-picker
                    v-model="item.deliveryDate"
                    type="date"
                    placeholder="交货日期"
                    value-format="YYYY-MM-DD"
                    style="width: 100%"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="4">
                <el-form-item :label="'最小订单量'">
                  <el-input-number
                    v-model="item.moq"
                    :min="0"
                    :precision="4"
                    style="width: 100%"
                    controls-position="right"
                    placeholder="MOQ"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="7">
                <el-form-item :label="'备注'">
                  <el-input v-model="item.remark" placeholder="明细备注" />
                </el-form-item>
              </el-col>
            </el-row>
          </div>
        </div>

        <div class="add-item-btn-wrap">
          <el-button type="dashed" :icon="Plus" @click="addItem" style="width: 100%">添加明细行</el-button>
        </div>
      </el-card>
    </el-form>

    <!-- 底部操作栏 -->
    <div class="bottom-bar">
      <el-button @click="handleBack">取消</el-button>
      <el-button type="primary" :loading="saving" @click="handleSave">保存需求</el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElNotification, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { ArrowLeft, Plus, Delete, Document } from '@element-plus/icons-vue'
import { demandApi } from '@/api/demand'
import { customerApi } from '@/api/customer'
import type { CreateDemandRequest, CreateDemandItemRequest } from '@/types/demand'

const router = useRouter()
const route = useRoute()
const demandId = route.params.id as string | undefined
const isEdit = computed(() => !!demandId && route.path.includes('/edit'))
const demandCode = ref('')

// ─── 表单 ───
const formRef = ref<FormInstance>()
const pageLoading = ref(false)
const saving = ref(false)

const form = reactive<{
  customerId: string
  demandDate: string
  expectedDeliveryDate: string
  source: number | undefined
  currency: string
  paymentTerms: string
  qualityRequirements: string
  certificationRequirements: string
  remark: string
  items: CreateDemandItemRequest[]
}>({
  customerId: '',
  demandDate: new Date().toISOString().slice(0, 10),
  expectedDeliveryDate: '',
  source: 1,
  currency: 'CNY',
  paymentTerms: '',
  qualityRequirements: '',
  certificationRequirements: '',
  remark: '',
  items: [],
})

const rules: FormRules = {
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }],
  demandDate: [{ required: true, message: '请选择需求日期', trigger: 'change' }],
}

// ─── 客户搜索 ───
const customerOptions = ref<any[]>([])
const customerSearching = ref(false)

async function searchCustomers(query: string) {
  if (!query) return
  customerSearching.value = true
  try {
    const res = await customerApi.searchCustomers({ searchTerm: query, pageSize: 20 })
    customerOptions.value = res.items ?? []
  } catch {
    customerOptions.value = []
  } finally {
    customerSearching.value = false
  }
}

function handleCustomerChange() {
  // 切换客户时清除重复警告
  duplicateWarnings.value = form.items.map(() => '')
}

// ─── 明细操作 ───
const duplicateWarnings = ref<string[]>([])
const checkingDuplicate = ref<boolean[]>([])

function addItem() {
  form.items.push({
    materialCode: '',
    materialName: '',
    materialModel: '',
    customerMaterialCode: '',
    quantity: 1,
    unit: 'PCS',
    targetPrice: undefined,
    brandRequirement: '',
    originRequirement: '',
    deliveryDate: '',
    moq: undefined,
    remark: '',
  })
  duplicateWarnings.value.push('')
  checkingDuplicate.value.push(false)
}

function removeItem(index: number) {
  form.items.splice(index, 1)
  duplicateWarnings.value.splice(index, 1)
  checkingDuplicate.value.splice(index, 1)
}

async function checkDuplicate(index: number) {
  const item = form.items[index]
  if (!item.materialCode || !form.customerId) {
    ElNotification.warning({ title: '请先填写', message: '请先选择客户并填写物料编码' })
    return
  }
  checkingDuplicate.value[index] = true
  try {
    const res = await demandApi.checkDuplicateMaterial({
      customerId: form.customerId,
      materialCode: item.materialCode,
      daysRange: 30,
    })
    if (res.isDuplicate) {
      duplicateWarnings.value[index] = `⚠️ 30天内已有重复需求：${res.existingDemandCode || ''} - ${res.message || '该物料已存在相似需求'}`
    } else {
      duplicateWarnings.value[index] = ''
      ElNotification.success({ title: '检查通过', message: '未发现重复物料需求' })
    }
  } catch {
    duplicateWarnings.value[index] = ''
  } finally {
    checkingDuplicate.value[index] = false
  }
}

async function checkDuplicateOnBlur(index: number) {
  const item = form.items[index]
  if (item.materialCode && form.customerId) {
    await checkDuplicate(index)
  }
}

// ─── 数据加载（编辑模式） ───
async function loadDemandForEdit() {
  if (!demandId) return
  pageLoading.value = true
  try {
    const d = await demandApi.getDemandDetail(demandId)
    demandCode.value = d.demandCode

    form.customerId = d.customerId
    form.demandDate = d.demandDate?.slice(0, 10) ?? ''
    form.expectedDeliveryDate = d.expectedDeliveryDate?.slice(0, 10) ?? ''
    form.source = d.source
    form.currency = d.currency ?? 'CNY'
    form.paymentTerms = d.paymentTerms ?? ''
    form.qualityRequirements = d.qualityRequirements ?? ''
    form.certificationRequirements = d.certificationRequirements ?? ''
    form.remark = d.remark ?? ''

    // 加载明细
    const items = await demandApi.getDemandItemsByDemandId(demandId)
    form.items = items.map(i => ({
      materialCode: i.materialCode ?? '',
      materialName: i.materialName ?? '',
      materialModel: i.materialModel ?? '',
      customerMaterialCode: i.customerMaterialCode ?? '',
      quantity: i.quantity,
      unit: i.unit ?? 'PCS',
      targetPrice: i.targetPrice,
      brandRequirement: i.brandRequirement ?? '',
      originRequirement: i.originRequirement ?? '',
      deliveryDate: i.deliveryDate?.slice(0, 10) ?? '',
      moq: i.moq,
      remark: i.remark ?? '',
    }))
    duplicateWarnings.value = form.items.map(() => '')
    checkingDuplicate.value = form.items.map(() => false)

    // 加载客户选项（用于显示）
    if (d.customerName) {
      customerOptions.value = [{ id: d.customerId, customerName: d.customerName }]
    }
  } catch (err: any) {
    ElNotification.error({ title: '加载失败', message: err?.message || '获取需求数据失败' })
  } finally {
    pageLoading.value = false
  }
}

// ─── 保存 ───
async function handleSave() {
  if (!formRef.value) return

  // 表单校验
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) {
    ElNotification.warning({ title: '校验失败', message: '请检查表单填写是否完整，必填项不能为空' })
    return
  }

  // 明细校验
  if (form.items.length === 0) {
    ElNotification.warning({ title: '请添加明细', message: '需求明细不能为空，请至少添加一条明细' })
    return
  }

  for (let i = 0; i < form.items.length; i++) {
    const item = form.items[i]
    if (!item.quantity || item.quantity <= 0) {
      ElNotification.warning({ title: '明细校验失败', message: `第 ${i + 1} 行数量必须大于 0` })
      return
    }
  }

  // 后端数据验证
  saving.value = true
  try {
    const payload: CreateDemandRequest = {
      customerId: form.customerId,
      demandDate: form.demandDate,
      expectedDeliveryDate: form.expectedDeliveryDate || undefined,
      source: form.source,
      currency: form.currency,
      paymentTerms: form.paymentTerms || undefined,
      qualityRequirements: form.qualityRequirements || undefined,
      certificationRequirements: form.certificationRequirements || undefined,
      remark: form.remark || undefined,
      items: form.items.map((item, idx) => ({
        ...item,
        lineNo: idx + 1,
        targetPrice: item.targetPrice ?? undefined,
        moq: item.moq ?? undefined,
        deliveryDate: item.deliveryDate || undefined,
      })),
    }

    if (isEdit.value && demandId) {
      await demandApi.updateDemand(demandId, payload)
      ElNotification.success({ title: '保存成功', message: '需求信息已成功更新' })
      setTimeout(() => router.push(`/demands/${demandId}`), 1000)
    } else {
      const created = await demandApi.createDemand(payload)
      ElNotification.success({ title: '创建成功', message: `需求 ${created.demandCode} 已成功创建` })
      setTimeout(() => router.push(`/demands/${created.id}`), 1000)
    }
  } catch (err: any) {
    ElNotification.error({ title: '保存失败', message: err?.message || '保存失败，请重试' })
  } finally {
    saving.value = false
  }
}

function handleBack() {
  if (isEdit.value && demandId) {
    router.push(`/demands/${demandId}`)
  } else {
    router.push('/demands')
  }
}

onMounted(() => {
  if (isEdit.value) {
    loadDemandForEdit()
  } else {
    // 新增模式默认添加一行
    addItem()
  }
})
</script>

<style scoped lang="scss">
.demand-edit-page {
  padding: 20px;
  min-height: 100%;
  padding-bottom: 80px;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.page-header-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.page-title {
  font-size: 18px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  margin: 0;
}

.page-header-right {
  display: flex;
  gap: 8px;
}

.demand-form { display: flex; flex-direction: column; gap: 16px; }

.form-card {
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
}

.card-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.items-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.items-header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.empty-items {
  text-align: center;
  padding: 32px 0;
  color: var(--el-text-color-secondary);
}

.empty-icon {
  font-size: 40px;
  color: var(--el-text-color-placeholder);
  margin-bottom: 8px;
}

.item-row {
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
  padding: 14px 16px 4px;
  margin-bottom: 12px;
  background: var(--el-fill-color-light);
  transition: border-color 0.2s;

  &:hover { border-color: var(--el-color-primary-light-5); }
}

.item-row-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.item-row-no {
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-secondary);
}

.item-row-actions {
  display: flex;
  gap: 4px;
}

.duplicate-alert {
  margin-bottom: 12px;
}

.add-item-btn-wrap {
  margin-top: 8px;
}

.bottom-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: var(--el-bg-color);
  border-top: 1px solid var(--el-border-color-light);
  padding: 12px 24px;
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  z-index: 100;
}
</style>
