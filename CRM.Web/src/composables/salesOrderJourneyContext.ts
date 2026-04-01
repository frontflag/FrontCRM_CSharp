import { ref } from 'vue'

/** 销售订单列表当前行（供右侧「订单旅程」在无路由 id 时使用） */
export const journeyListFocusedOrderId = ref('')
export const journeyListFocusedOrderCode = ref('')

export function setJourneyListFocus(id: string, sellOrderCode?: string) {
  journeyListFocusedOrderId.value = (id || '').trim()
  journeyListFocusedOrderCode.value = (sellOrderCode || '').trim()
}
