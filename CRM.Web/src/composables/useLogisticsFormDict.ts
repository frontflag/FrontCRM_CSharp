import { ref, computed } from 'vue'
import { dictionaryApi, type DictionaryItemDto } from '@/api/dictionary'

const loaded = ref(false)
const arrivalItems = ref<DictionaryItemDto[]>([])
const expressItems = ref<DictionaryItemDto[]>([])

/** 出货场景不含「物流(4)」 */
export const SHIPMENT_ARRIVAL_EXCLUDED_CODES = ['4'] as const

/** 出货方式为「快递」时的字典 ItemCode */
export const EXPRESS_SHIPMENT_CODE = '3'

export function isExpressShipmentMethod(code?: string | null): boolean {
  return String(code ?? '').trim() === EXPRESS_SHIPMENT_CODE
}

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
  /** 出库/装箱等出货场景：排除「物流(4)」 */
  const shipmentArrivalOptions = computed(() =>
    arrivalItems.value
      .filter((o) => !SHIPMENT_ARRIVAL_EXCLUDED_CODES.includes(String(o.code) as '4'))
      .map((o) => ({ label: o.label, value: o.code }))
  )
  const expressOptions = computed(() =>
    expressItems.value.map((o) => ({ label: o.label, value: o.code }))
  )

  return {
    ensureLoaded: ensureLogisticsFormDictLoaded,
    arrivalOptions,
    shipmentArrivalOptions,
    expressOptions
  }
}
