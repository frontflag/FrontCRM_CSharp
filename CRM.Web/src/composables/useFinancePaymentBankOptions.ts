import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { financePaymentBankApi, type FinancePaymentBankDto } from '@/api/financePaymentBank'

/** 申请付款等场景：从财务参数「付款银行」拉取下拉数据。 */
export function useFinancePaymentBankOptions() {
  const { t } = useI18n()
  const paymentBankOptions = ref<FinancePaymentBankDto[]>([])

  async function loadPaymentBankOptions(): Promise<void> {
    try {
      paymentBankOptions.value = await financePaymentBankApi.listOptions()
    } catch {
      paymentBankOptions.value = []
      ElMessage.warning(t('purchaseOrderItemList.messages.paymentBanksLoadFailed'))
    }
  }

  function selectedPaymentBankName(vendorBankId: string): string {
    if (!vendorBankId) return ''
    return paymentBankOptions.value.find((b) => b.id === vendorBankId)?.bankName || vendorBankId
  }

  return { paymentBankOptions, loadPaymentBankOptions, selectedPaymentBankName }
}
