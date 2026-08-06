import { ref } from 'vue'
import { defineStore } from 'pinia'

/**
 * 右扩展「历史报价」页签上下文（报价桌面 / 新建报价页共享）。
 */
export const useQuoteHistoryContextStore = defineStore('quoteHistoryContext', () => {
  const mpn = ref('')
  const brand = ref('')

  function bind(ctx: { mpn?: string | null; brand?: string | null }) {
    mpn.value = String(ctx.mpn ?? '').trim()
    brand.value = String(ctx.brand ?? '').trim()
  }

  function clear() {
    mpn.value = ''
    brand.value = ''
  }

  return { mpn, brand, bind, clear }
})
