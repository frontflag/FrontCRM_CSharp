<template>
  <el-cascader
    :model-value="innerValue"
    :options="options"
    :props="cascaderProps"
    :placeholder="placeholder"
    :clearable="clearable"
    :filterable="filterable"
    :show-all-levels="false"
    style="width: 100%"
    @change="handleChange"
  />
</template>

<script setup lang="ts">
/** 数据范围：document/PRD/规范/业务规范/业务员与采购员下拉规范.md §4 */
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { authApi } from '@/api/auth'
import type { OrgUserTreeNode } from '@/api/auth'

type CascaderNode = {
  value: string
  label: string
  isUser: boolean
  children?: CascaderNode[]
}

const props = withDefaults(
  defineProps<{
    modelValue?: string
    placeholder?: string
    clearable?: boolean
    filterable?: boolean
  }>(),
  {
    modelValue: '',
    placeholder: '请选择采购员',
    clearable: true,
    filterable: true
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'change', payload: { id: string; label: string }): void
}>()

const options = ref<CascaderNode[]>([])

const innerValue = computed<string[]>({
  get() {
    if (!props.modelValue) return []
    const path = findUserPath(options.value, props.modelValue)
    return path ?? []
  },
  set(v) {
    const next = Array.isArray(v) && v.length > 0 ? String(v[v.length - 1]) : ''
    emit('update:modelValue', next)
    const node = findNodeByPath(options.value, Array.isArray(v) ? v.map((x) => String(x)) : [])
    emit('change', { id: next, label: node?.isUser ? node.label : '' })
  }
})

const cascaderProps = {
  value: 'value',
  label: 'label',
  children: 'children',
  emitPath: true,
  checkStrictly: true
}

function mapTree(nodes: OrgUserTreeNode[] | undefined): CascaderNode[] {
  if (!nodes || !Array.isArray(nodes)) return []
  return nodes.map((n) => ({
    value: n.value,
    label: n.label,
    isUser: !!n.isUser,
    children: mapTree(n.children)
  }))
}

function findUserPath(nodes: CascaderNode[], userId: string, stack: string[] = []): string[] | null {
  for (const node of nodes) {
    const next = [...stack, node.value]
    if (node.isUser && node.value === userId) {
      return next
    }
    if (node.children?.length) {
      const found = findUserPath(node.children, userId, next)
      if (found) return found
    }
  }
  return null
}

function handleChange(v: unknown) {
  const arr = Array.isArray(v) ? v.map((x) => String(x)) : []
  const leaf = arr.length ? arr[arr.length - 1] : ''
  const node = findNodeByPath(options.value, arr)
  const userId = node?.isUser ? leaf : ''
  innerValue.value = userId ? arr : []
  if (!userId) {
    emit('update:modelValue', '')
    emit('change', { id: '', label: '' })
  }
}

function findNodeByPath(nodes: CascaderNode[], path: string[]): CascaderNode | null {
  if (!path.length) return null
  let currentNodes = nodes
  let current: CascaderNode | null = null
  for (const key of path) {
    current = currentNodes.find((x) => x.value === key) ?? null
    if (!current) return null
    currentNodes = current.children ?? []
  }
  return current
}

function syncLabelToParent() {
  if (!props.modelValue || !options.value.length) return
  const path = findUserPath(options.value, props.modelValue)
  if (!path?.length) return
  const node = findNodeByPath(options.value, path)
  if (node?.isUser && node.label) {
    emit('change', { id: props.modelValue, label: node.label })
  }
}

onMounted(async () => {
  try {
    const res = await authApi.getPurchaseUsersTree()
    const raw = res as unknown
    const data = Array.isArray(raw)
      ? (raw as OrgUserTreeNode[])
      : raw && typeof raw === 'object' && 'data' in raw && Array.isArray((raw as { data: unknown }).data)
        ? (raw as { data: OrgUserTreeNode[] }).data
        : []
    options.value = mapTree(data)
    if (options.value.length === 0) {
      console.warn('[PurchaserCascader] empty options from /api/v1/auth/purchase-users-tree', res)
    }
    await nextTick()
    syncLabelToParent()
  } catch {
    options.value = []
    console.warn('[PurchaserCascader] failed to load /api/v1/auth/purchase-users-tree')
  }
})

watch(
  () => props.modelValue,
  () => {
    void nextTick(() => syncLabelToParent())
  }
)
</script>
