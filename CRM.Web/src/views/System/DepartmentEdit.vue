<template>
  <div class="system-page">
    <el-card>
      <div class="toolbar">
        <div class="title">{{ isEdit ? '编辑部门' : '新增部门' }}</div>
      </div>

      <el-form :model="formData" label-width="140px" :disabled="loading">
        <el-form-item label="部门名称" required>
          <el-input v-model="formData.departmentName" maxlength="100" show-word-limit placeholder="请输入部门名称" />
        </el-form-item>
        <el-form-item label="上级部门">
          <el-tree-select
            v-model="formData.parentId"
            :data="parentTreeData"
            check-strictly
            clearable
            filterable
            placeholder="不选则为顶级部门"
            style="width: 100%"
            :props="{ label: 'label', value: 'value', children: 'children' }"
            :render-after-expand="false"
          />
        </el-form-item>
        <el-form-item label="销售数据范围">
          <div class="scope-access-row">
            <el-select v-model="formData.saleDataScope" class="scope-select">
              <el-option v-for="o in scopeOptions" :key="'s' + o.value" :label="o.label" :value="o.value" />
            </el-select>
            <el-select
              v-model="formData.saleDataAccess"
              class="access-select"
              :disabled="formData.saleDataScope === 4"
            >
              <el-option v-for="o in accessOptions" :key="'sa' + o.value" :label="o.label" :value="o.value" />
            </el-select>
            <el-checkbox v-model="formData.hideCustomerManagement" class="scope-hide-checkbox">
              隐藏客户管理
            </el-checkbox>
          </div>
          <div v-if="formData.saleDataScope === 4" class="field-hint">范围为「禁止」时无需设置访问方式</div>
        </el-form-item>
        <el-form-item label="采购数据范围">
          <div class="scope-access-row">
            <el-select v-model="formData.purchaseDataScope" class="scope-select">
              <el-option v-for="o in scopeOptions" :key="'p' + o.value" :label="o.label" :value="o.value" />
            </el-select>
            <el-select
              v-model="formData.purchaseDataAccess"
              class="access-select"
              :disabled="formData.purchaseDataScope === 4"
            >
              <el-option v-for="o in accessOptions" :key="'pa' + o.value" :label="o.label" :value="o.value" />
            </el-select>
            <el-checkbox v-model="formData.hideVendorManagement" class="scope-hide-checkbox">
              隐藏供应商管理
            </el-checkbox>
          </div>
          <div v-if="formData.purchaseDataScope === 4" class="field-hint">范围为「禁止」时无需设置访问方式</div>
        </el-form-item>
        <el-form-item label="物流数据范围">
          <div class="scope-access-row">
            <el-select v-model="formData.logisticsDataScope" class="scope-select">
              <el-option v-for="o in scopeOptions" :key="'l' + o.value" :label="o.label" :value="o.value" />
            </el-select>
            <el-select
              v-model="formData.logisticsDataAccess"
              class="access-select"
              :disabled="formData.logisticsDataScope === 4"
            >
              <el-option v-for="o in accessOptions" :key="'la' + o.value" :label="o.label" :value="o.value" />
            </el-select>
          </div>
          <div class="field-hint">控制入库管理、出库管理、库存管理、报关菜单及对应数据范围</div>
          <div v-if="formData.logisticsDataScope === 4" class="field-hint">范围为「禁止」时无需设置访问方式</div>
        </el-form-item>
        <el-form-item label="财务数据范围">
          <div class="scope-access-row">
            <el-select v-model="formData.financeDataScope" class="scope-select">
              <el-option v-for="o in scopeOptions" :key="'f' + o.value" :label="o.label" :value="o.value" />
            </el-select>
            <el-select
              v-model="formData.financeDataAccess"
              class="access-select"
              :disabled="formData.financeDataScope === 4"
            >
              <el-option v-for="o in accessOptions" :key="'fa' + o.value" :label="o.label" :value="o.value" />
            </el-select>
          </div>
          <div class="field-hint">控制付款管理、收款管理菜单及对应数据范围</div>
          <div v-if="formData.financeDataScope === 4" class="field-hint">范围为「禁止」时无需设置访问方式</div>
        </el-form-item>
        <el-form-item label="业务身份">
          <el-select v-model="formData.identityType" style="width: 280px">
            <el-option v-for="o in identityOptions" :key="o.value" :label="o.label" :value="o.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="formData.status" style="width: 160px">
            <el-option :value="1" label="启用" />
            <el-option :value="0" label="禁用" />
          </el-select>
        </el-form-item>

        <div class="footer-bar">
          <el-button @click="router.push({ name: 'DepartmentList' })">返回</el-button>
          <el-button type="primary" :loading="saving" @click="handleSubmit">
            {{ isEdit ? '保存修改' : '创建部门' }}
          </el-button>
        </div>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { rbacAdminApi, type RbacDepartment } from '@/api/rbacAdmin'

const route = useRoute()
const router = useRouter()
const departmentId = route.params.id as string | undefined
const isEdit = !!departmentId

const loading = ref(false)
const saving = ref(false)
const allDepartments = ref<RbacDepartment[]>([])

const scopeOptions = [
  { value: 0, label: '0 全部' },
  { value: 1, label: '1 自己' },
  { value: 2, label: '2 本部门' },
  { value: 3, label: '3 本部门及下级' },
  { value: 4, label: '4 禁止' }
]

const accessOptions = [
  { value: 0, label: '读写' },
  { value: 1, label: '只读' }
]

// 展示顺序与文案按产品约定；value 仍为库中 IdentityType，未改历史数据含义（3=原采购助理 4=原客服）
const identityOptions = [
  { value: 0, label: '0 无' },
  { value: 1, label: '1 销售' },
  { value: 2, label: '2 采购' },
  { value: 4, label: '3 商务' },
  { value: 3, label: '4 采购运营' },
  { value: 6, label: '5 物流' },
  { value: 5, label: '6 财务' }
]

const formData = ref({
  departmentName: '',
  parentId: undefined as string | undefined,
  saleDataScope: 2,
  saleDataAccess: 0,
  hideCustomerManagement: false,
  purchaseDataScope: 2,
  purchaseDataAccess: 0,
  hideVendorManagement: false,
  logisticsDataScope: 0,
  logisticsDataAccess: 0,
  financeDataScope: 0,
  financeDataAccess: 0,
  identityType: 0,
  status: 1
})

watch(
  () => formData.value.saleDataScope,
  (v) => {
    if (v === 4) formData.value.saleDataAccess = 0
  }
)
watch(
  () => formData.value.purchaseDataScope,
  (v) => {
    if (v === 4) formData.value.purchaseDataAccess = 0
  }
)
watch(
  () => formData.value.logisticsDataScope,
  (v) => {
    if (v === 4) formData.value.logisticsDataAccess = 0
  }
)
watch(
  () => formData.value.financeDataScope,
  (v) => {
    if (v === 4) formData.value.financeDataAccess = 0
  }
)

function collectExcludedIds(rootId: string, all: RbacDepartment[]): Set<string> {
  const ex = new Set<string>()
  const walk = (id: string) => {
    ex.add(id)
    all.filter(d => d.parentId === id).forEach(c => walk(c.id))
  }
  walk(rootId)
  return ex
}

function buildParentTree(all: RbacDepartment[], editingId?: string) {
  const list = editingId
    ? (() => {
        const ex = collectExcludedIds(editingId, all)
        return all.filter(d => !ex.has(d.id))
      })()
    : [...all]

  const byParent = new Map<string | null, RbacDepartment[]>()
  for (const d of list) {
    const p = d.parentId ?? null
    if (!byParent.has(p)) byParent.set(p, [])
    byParent.get(p)!.push(d)
  }

  const toNodes = (parentKey: string | null): { value: string; label: string; children?: { value: string; label: string; children?: unknown[] }[] }[] => {
    const rows = (byParent.get(parentKey) || []).slice().sort((a, b) => a.departmentName.localeCompare(b.departmentName))
    return rows.map(d => {
      const children = toNodes(d.id)
      return {
        value: d.id,
        label: d.departmentName,
        ...(children.length ? { children } : {})
      }
    })
  }

  return toNodes(null)
}

const parentTreeData = computed(() => buildParentTree(allDepartments.value, isEdit ? departmentId : undefined))

const load = async () => {
  loading.value = true
  try {
    allDepartments.value = await rbacAdminApi.getDepartments()
    if (isEdit && departmentId) {
      const d = await rbacAdminApi.getDepartmentById(departmentId)
      formData.value = {
        departmentName: d.departmentName,
        parentId: d.parentId || undefined,
        saleDataScope: d.saleDataScope,
        saleDataAccess: d.saleDataAccess ?? 0,
        hideCustomerManagement: d.hideCustomerManagement ?? false,
        purchaseDataScope: d.purchaseDataScope,
        purchaseDataAccess: d.purchaseDataAccess ?? 0,
        hideVendorManagement: d.hideVendorManagement ?? false,
        logisticsDataScope: d.logisticsDataScope ?? 0,
        logisticsDataAccess: d.logisticsDataAccess ?? 0,
        financeDataScope: d.financeDataScope ?? 0,
        financeDataAccess: d.financeDataAccess ?? 0,
        identityType: d.identityType,
        status: d.status ?? 1
      }
    }
  } catch (e: any) {
    ElMessage.error(e?.message || '加载失败')
  } finally {
    loading.value = false
  }
}

const handleSubmit = async () => {
  if (!formData.value.departmentName.trim()) {
    ElMessage.warning('请填写部门名称')
    return
  }
  saving.value = true
  try {
    const payload = {
      departmentName: formData.value.departmentName.trim(),
      parentId: formData.value.parentId || null,
      saleDataScope: formData.value.saleDataScope,
      saleDataAccess: formData.value.saleDataScope === 4 ? 0 : formData.value.saleDataAccess,
      hideCustomerManagement: formData.value.hideCustomerManagement,
      purchaseDataScope: formData.value.purchaseDataScope,
      purchaseDataAccess: formData.value.purchaseDataScope === 4 ? 0 : formData.value.purchaseDataAccess,
      hideVendorManagement: formData.value.hideVendorManagement,
      logisticsDataScope: formData.value.logisticsDataScope,
      logisticsDataAccess: formData.value.logisticsDataScope === 4 ? 0 : formData.value.logisticsDataAccess,
      financeDataScope: formData.value.financeDataScope,
      financeDataAccess: formData.value.financeDataScope === 4 ? 0 : formData.value.financeDataAccess,
      identityType: formData.value.identityType,
      status: formData.value.status
    }
    if (isEdit && departmentId) {
      await rbacAdminApi.updateDepartment(departmentId, payload)
      ElMessage.success('保存成功')
    } else {
      await rbacAdminApi.createDepartment(payload)
      ElMessage.success('创建成功')
    }
    router.push({ name: 'DepartmentList' })
  } catch (e: any) {
    ElMessage.error(e?.message || '保存失败')
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.system-page {
  padding: 20px;
}

.toolbar {
  margin-bottom: 14px;
}

.title {
  font-size: 18px;
  font-weight: 600;
}

.scope-access-row {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
  max-width: 720px;
}

.scope-hide-checkbox {
  margin-left: 4px;
  white-space: nowrap;
}

.scope-select {
  width: 280px;
}

.access-select {
  width: 120px;
}

.field-hint {
  margin-top: 6px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.footer-bar {
  margin-top: 24px;
  display: flex;
  gap: 12px;
}
</style>
