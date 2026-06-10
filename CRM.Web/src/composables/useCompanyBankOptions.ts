import { ref } from 'vue'
import { fetchCompanyProfileForReport, type CompanyBankRow } from '@/api/companyProfile'
import { filterEnabledCompanyPaymentBanks } from '@/utils/companyBank'

const companyBankRows = ref<CompanyBankRow[]>([])
let loadPromise: Promise<CompanyBankRow[]> | null = null

export function useCompanyBankOptions() {
  async function loadCompanyBankOptions(force = false): Promise<CompanyBankRow[]> {
    if (!force && companyBankRows.value.length) return companyBankRows.value
    if (!force && loadPromise) return loadPromise

    loadPromise = fetchCompanyProfileForReport()
      .then((bundle) => {
        companyBankRows.value = filterEnabledCompanyPaymentBanks(bundle.bankInfos ?? [])
        return companyBankRows.value
      })
      .catch(() => {
        companyBankRows.value = []
        return companyBankRows.value
      })
      .finally(() => {
        loadPromise = null
      })

    return loadPromise
  }

  return {
    companyBankRows,
    loadCompanyBankOptions
  }
}
