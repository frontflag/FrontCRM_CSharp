import { ref } from 'vue'
import { defineStore } from 'pinia'
import type { AdminUserDto } from '@/api/rbacAdmin'
import { userLevelApi, type UserLevelHistoryItem } from '@/api/userLevel'
import { getApiErrorMessage } from '@/utils/apiError'

export const useUserLevelLogStore = defineStore('userLevelLog', () => {
  const row = ref<AdminUserDto | null>(null)
  const history = ref<UserLevelHistoryItem[]>([])
  const loading = ref(false)
  const loadError = ref('')
  let loadSeq = 0

  function clear() {
    row.value = null
    history.value = []
    loadError.value = ''
    loading.value = false
    loadSeq += 1
  }

  function setRowOnly(target: AdminUserDto) {
    row.value = target
  }

  async function loadHistory(loadFailedText = '加载变更记录失败') {
    const id = String(row.value?.id ?? '').trim()
    if (!id) return
    const seq = ++loadSeq
    loading.value = true
    loadError.value = ''
    try {
      const data = await userLevelApi.getHistory(id)
      if (seq !== loadSeq) return
      history.value = Array.isArray(data) ? data : []
    } catch (e: unknown) {
      if (seq !== loadSeq) return
      history.value = []
      loadError.value = getApiErrorMessage(e, loadFailedText)
    } finally {
      if (seq === loadSeq) loading.value = false
    }
  }

  async function selectRow(target: AdminUserDto, loadFailedText?: string) {
    setRowOnly(target)
    await loadHistory(loadFailedText)
  }

  return { row, history, loading, loadError, clear, setRowOnly, selectRow, loadHistory }
})
