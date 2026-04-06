import { ref, shallowRef } from 'vue'
import { dictionaryApi, type DictionaryItemDto } from '@/api/dictionary'

/** 将历史明文（如「2年内」）或已有 ItemCode 规范为字典 ItemCode；自由文本原样返回 */
export function coerceProductionDateToCode(
  raw: string | undefined,
  opts: readonly DictionaryItemDto[]
): string {
  const s = String(raw ?? '').trim()
  if (!s) return ''
  if (opts.some((o) => o.code === s)) return s
  const byLabel = opts.find((o) => o.label === s)
  if (byLabel) return byLabel.code
  return s
}

/** 列表/详情展示：ItemCode → 文案；未知则原样 */
export function productionDateDisplayLabel(
  value: string | undefined,
  opts: readonly DictionaryItemDto[]
): string {
  const s = String(value ?? '').trim()
  if (!s) return ''
  const hit = opts.find((o) => o.code === s)
  if (hit) return hit.label
  return s
}

const sharedOptions = shallowRef<DictionaryItemDto[]>([])
const sharedLoading = ref(false)
let loadPromise: Promise<void> | null = null

export async function ensureMaterialProductionDateDictLoaded(): Promise<void> {
  if (sharedOptions.value.length > 0) return
  if (loadPromise) {
    await loadPromise
    return
  }
  sharedLoading.value = true
  loadPromise = (async () => {
    try {
      const map = await dictionaryApi.fetchMaterialForm()
      sharedOptions.value = map.MaterialProductionDate ?? []
    } catch {
      sharedOptions.value = []
    } finally {
      sharedLoading.value = false
    }
  })()
  await loadPromise
}

export function useMaterialProductionDateDict() {
  function defaultCode(): string {
    return sharedOptions.value[0]?.code ?? '1'
  }

  function coerce(raw: string | undefined): string {
    return coerceProductionDateToCode(raw, sharedOptions.value)
  }

  function labelOf(v: string | undefined): string {
    return productionDateDisplayLabel(v, sharedOptions.value)
  }

  return {
    options: sharedOptions,
    loading: sharedLoading,
    ensureLoaded: ensureMaterialProductionDateDictLoaded,
    defaultCode,
    coerceProductionDateToCode: coerce,
    productionDateDisplayLabel: labelOf
  }
}
