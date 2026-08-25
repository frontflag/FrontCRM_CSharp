import { ref } from 'vue'
import { defineStore } from 'pinia'
import {
  customerWorkspaceApi,
  type CustomerWorkspace,
  type CustomerWorkspaceSource
} from '@/api/customerWorkspace'
import { getApiErrorMessage } from '@/utils/apiError'

export const useCustomerWorkspacePanelStore = defineStore('customerWorkspacePanel', () => {
  const source = ref<CustomerWorkspaceSource | ''>('')
  const boundId = ref('')
  const summary = ref<CustomerWorkspace | null>(null)
  const loading = ref(false)
  const loadError = ref('')
  let loadSeq = 0

  function clearBound() {
    boundId.value = ''
    summary.value = null
    loadError.value = ''
    loading.value = false
    loadSeq += 1
  }

  function clear() {
    clearBound()
  }

  function setSource(next: CustomerWorkspaceSource) {
    if (source.value === next) return
    source.value = next
    boundId.value = ''
    summary.value = null
    loadError.value = ''
    loading.value = false
    loadSeq += 1
  }

  function bind(nextSource: CustomerWorkspaceSource, id: string) {
    const key = id.trim()
    if (!key) return
    if (source.value !== nextSource || boundId.value !== key) {
      summary.value = null
      loadError.value = ''
    }
    source.value = nextSource
    boundId.value = key
  }

  function setRowOnly(target: Record<string, unknown>) {
    const src = source.value
    if (!src) return
    const id = String(target.id ?? target.Id ?? '').trim()
    if (!id) return
    bind(src, id)
  }

  async function load(loadFailedText = '加载客户信息失败') {
    const src = source.value
    const id = boundId.value.trim()
    if (!src || !id) return
    const seq = ++loadSeq
    loading.value = true
    loadError.value = ''
    try {
      const data = await customerWorkspaceApi.get(src, id)
      if (seq !== loadSeq) return
      summary.value = data
    } catch (e) {
      if (seq !== loadSeq) return
      summary.value = null
      loadError.value = getApiErrorMessage(e, loadFailedText)
    } finally {
      if (seq === loadSeq) loading.value = false
    }
  }

  async function selectRow(target: Record<string, unknown>, loadFailedText?: string) {
    setRowOnly(target)
    await load(loadFailedText)
  }

  return {
    source,
    boundId,
    summary,
    loading,
    loadError,
    clear,
    clearBound,
    setSource,
    bind,
    setRowOnly,
    load,
    selectRow
  }
})
