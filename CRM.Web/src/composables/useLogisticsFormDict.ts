import { ref, computed } from 'vue'
import { dictionaryApi, type DictionaryItemDto } from '@/api/dictionary'

const loaded = ref(false)
const arrivalItems = ref<DictionaryItemDto[]>([])
const expressItems = ref<DictionaryItemDto[]>([])

export async function ensureLogisticsFormDictLoaded(): Promise<void> {
  if (loaded.value) return
  const map = await dictionaryApi.fetchLogisticsForm()
  arrivalItems.value = map.LogisticsArrivalMethod ?? []
  expressItems.value = map.LogisticsExpressMethod ?? []
  loaded.value = true
}

/** 到货通知等：来货方式、快递方式（与 sys_dict_item 一致，v-model 存 ItemCode） */
export function useLogisticsFormDict() {
  const arrivalOptions = computed(() =>
    arrivalItems.value.map((o) => ({ label: o.label, value: o.code }))
  )
  const expressOptions = computed(() =>
    expressItems.value.map((o) => ({ label: o.label, value: o.code }))
  )

  return {
    ensureLoaded: ensureLogisticsFormDictLoaded,
    arrivalOptions,
    expressOptions
  }
}
