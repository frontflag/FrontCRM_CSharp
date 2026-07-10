import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { aiApi, AI_SCENARIO_MATERIAL_INTEL_LOOKUP } from '@/api/ai'
import { getApiErrorMessage } from '@/utils/apiError'
import { parseAiJsonObject } from '@/utils/aiJson'
import { normalizeMaterialPn } from '@/utils/materialPn'

export type MaterialIntelCacheStatus = 'done' | 'error'

export interface MaterialIntelCacheEntry {
  status: MaterialIntelCacheStatus
  data: Record<string, unknown> | null
  fromCache: boolean
  errorMessage?: string
  loadedAt: number
}

/**
 * 全局 AI 物料情报缓存（/rfq 首页与需求明细列表「物料」页签共用）。
 * 按 PN 去重；进行中的请求不因切换选中行而取消。
 */
export const useMaterialIntelLookupStore = defineStore('materialIntelLookup', () => {
  const cacheByPn = ref<Record<string, MaterialIntelCacheEntry>>({})
  const inFlightByPn = new Map<string, Promise<void>>()
  const loadingPnKeys = ref<string[]>([])
  const loadingStartedAt = ref<Record<string, number>>({})

  /** 需求明细列表当前绑定 PN（右栏「物料」页签展示用） */
  const boundPn = ref<string | null>(null)

  const boundPnNormalized = computed(() => normalizeMaterialPn(boundPn.value))

  function isPnLoading(pn: string): boolean {
    const key = normalizeMaterialPn(pn)
    return key ? loadingPnKeys.value.includes(key) : false
  }

  function getCacheEntry(pn: string): MaterialIntelCacheEntry | null {
    const key = normalizeMaterialPn(pn)
    if (!key) return null
    return cacheByPn.value[key] ?? null
  }

  function getLoadingElapsedSeconds(pn: string): number {
    const key = normalizeMaterialPn(pn)
    if (!key) return 0
    const started = loadingStartedAt.value[key]
    if (!started) return 0
    return Math.max(0, Math.floor((Date.now() - started) / 1000))
  }

  function addLoadingKey(key: string) {
    if (!loadingPnKeys.value.includes(key)) {
      loadingPnKeys.value = [...loadingPnKeys.value, key]
    }
    loadingStartedAt.value = { ...loadingStartedAt.value, [key]: Date.now() }
  }

  function removeLoadingKey(key: string) {
    loadingPnKeys.value = loadingPnKeys.value.filter((k) => k !== key)
    const next = { ...loadingStartedAt.value }
    delete next[key]
    loadingStartedAt.value = next
  }

  async function ensureLookup(pn: string, options?: { force?: boolean }): Promise<void> {
    const key = normalizeMaterialPn(pn)
    if (!key) return

    const existing = inFlightByPn.get(key)
    if (existing && !options?.force) return existing

    if (!options?.force) {
      const cached = cacheByPn.value[key]
      if (cached?.status === 'done' || cached?.status === 'error') return
    } else {
      const next = { ...cacheByPn.value }
      delete next[key]
      cacheByPn.value = next
    }

    const task = (async () => {
      addLoadingKey(key)
      try {
        const result = await aiApi.invoke({
          scenarioCode: AI_SCENARIO_MATERIAL_INTEL_LOOKUP,
          input: { pn: key }
        })
        const data = parseAiJsonObject(result.data, result.content)
        cacheByPn.value = {
          ...cacheByPn.value,
          [key]: {
            status: 'done',
            data,
            fromCache: !!result.fromCache,
            loadedAt: Date.now()
          }
        }
      } catch (e: unknown) {
        cacheByPn.value = {
          ...cacheByPn.value,
          [key]: {
            status: 'error',
            data: null,
            fromCache: false,
            errorMessage: getApiErrorMessage(e, 'AI 查询失败'),
            loadedAt: Date.now()
          }
        }
      } finally {
        removeLoadingKey(key)
        inFlightByPn.delete(key)
      }
    })()

    inFlightByPn.set(key, task)
    return task
  }

  function bindPn(pn: string | null | undefined) {
    const key = normalizeMaterialPn(pn)
    boundPn.value = key || null
  }

  function clearBound() {
    boundPn.value = null
  }

  return {
    cacheByPn,
    loadingPnKeys,
    boundPn,
    boundPnNormalized,
    isPnLoading,
    getCacheEntry,
    getLoadingElapsedSeconds,
    ensureLookup,
    bindPn,
    clearBound
  }
})
