import { ref, watch, onMounted, type MaybeRefOrGetter, toValue } from 'vue'
import { logRecentApi, type LogRecentItem } from '@/api/logRecent'

export function useRecentHistoryList(bizType: MaybeRefOrGetter<string>, take: MaybeRefOrGetter<number> = 20) {
  const loading = ref(false)
  const items = ref<LogRecentItem[]>([])

  async function reload() {
    loading.value = true
    try {
      items.value = await logRecentApi.list(toValue(bizType), toValue(take))
    } catch {
      items.value = []
    } finally {
      loading.value = false
    }
  }

  onMounted(reload)
  watch(
    () => [toValue(bizType), toValue(take)] as const,
    () => {
      void reload()
    }
  )

  return { loading, items, reload }
}
