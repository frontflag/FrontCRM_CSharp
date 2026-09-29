import { ref } from 'vue'
import { defineStore } from 'pinia'
import type { VendorContactInfo } from '@/types/vendor'

/**
 * 新建/编辑报价右侧「操作」页签：供应商上下文 + 新建联系人回填通知。
 */
export const useQuoteOpsPanelStore = defineStore('quoteOpsPanel', () => {
  const vendorId = ref('')
  const vendorName = ref('')
  /** 最近一次在操作页签新建成功的联系人；报价页消费后清空 */
  const createdContact = ref<VendorContactInfo | null>(null)

  function bind(ctx: { vendorId?: string | null; vendorName?: string | null }) {
    vendorId.value = String(ctx.vendorId ?? '').trim()
    vendorName.value = String(ctx.vendorName ?? '').trim()
  }

  function notifyContactCreated(contact: VendorContactInfo) {
    createdContact.value = contact
  }

  function consumeCreatedContact(): VendorContactInfo | null {
    const c = createdContact.value
    createdContact.value = null
    return c
  }

  function clear() {
    vendorId.value = ''
    vendorName.value = ''
    createdContact.value = null
  }

  return {
    vendorId,
    vendorName,
    createdContact,
    bind,
    notifyContactCreated,
    consumeCreatedContact,
    clear
  }
})
