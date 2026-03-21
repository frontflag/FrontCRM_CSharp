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
          <el-select v-model="formData.saleDataScope" style="width: 280px">
            <el-option v-for="o in scopeOptions" :key="'s' + o.value" :label="o.label" :value="o.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="采购数据范围">
          <el-select v-model="formData.purchaseDataScope" style="width: 280px">
            <el-option v-for="o in scopeOptions" :key="'p' + o.value" :label="o.label" :value="o.value" />
          </el-select>
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
import { computed, onMounted, ref } from 'vue'
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
  purchaseDataScope: 2,
  identityType: 0,
  status: 1
})

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
        purchaseDataScope: d.purchaseDataScope,
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
      purchaseDataScope: formData.value.purchaseDataScope,
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

.footer-bar {
  margin-top: 24px;
  display: flex;
  gap: 12px;
}
</style>
