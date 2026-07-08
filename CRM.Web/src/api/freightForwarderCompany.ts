import apiClient from './client'

export interface FreightForwarderCompany {
  id: string
  companyCode: string
  cname: string
  ename?: string | null
  status: number
  remark?: string | null
  banks?: FreightForwarderCompanyBank[]
}

export interface FreightForwarderCompanyBank {
  id: string
  freightForwarderCompanyId: string
  bankName: string
  accountName?: string | null
  accountNo?: string | null
  currency: number
  isDefault: boolean
  isDisabled: boolean
}

export async function fetchFreightForwarderCompanies(activeOnly = true): Promise<FreightForwarderCompany[]> {
  const list = await apiClient.get<FreightForwarderCompany[]>('/api/v1/freight-forwarder-companies', {
    params: activeOnly ? {} : { all: true }
  })
  return list ?? []
}

export async function fetchFreightForwarderCompaniesAdmin(): Promise<FreightForwarderCompany[]> {
  return fetchFreightForwarderCompanies(false)
}

export async function createFreightForwarderCompany(body: {
  cname: string
  ename?: string
  remark?: string
}): Promise<FreightForwarderCompany> {
  return apiClient.post<FreightForwarderCompany>('/api/v1/freight-forwarder-companies', body)
}

export async function updateFreightForwarderCompany(
  id: string,
  body: { cname: string; ename?: string; remark?: string }
): Promise<FreightForwarderCompany> {
  return apiClient.put<FreightForwarderCompany>(`/api/v1/freight-forwarder-companies/${id}`, body)
}

export async function patchFreightForwarderCompanyStatus(id: string, status: number): Promise<FreightForwarderCompany> {
  return apiClient.patch<FreightForwarderCompany>(`/api/v1/freight-forwarder-companies/${id}/status`, { status })
}

export async function deleteFreightForwarderCompany(id: string): Promise<void> {
  await apiClient.delete(`/api/v1/freight-forwarder-companies/${id}`)
}

export async function fetchFfCompanyBanks(companyId: string): Promise<FreightForwarderCompanyBank[]> {
  const list = await apiClient.get<FreightForwarderCompanyBank[]>(`/api/v1/freight-forwarder-companies/${companyId}/banks`)
  return list ?? []
}

export async function createFfCompanyBank(
  companyId: string,
  body: {
    bankName: string
    accountName?: string
    accountNo?: string
    currency?: number
    isDefault?: boolean
  }
): Promise<FreightForwarderCompanyBank> {
  return apiClient.post<FreightForwarderCompanyBank>(`/api/v1/freight-forwarder-companies/${companyId}/banks`, body)
}

export async function updateFfCompanyBank(
  bankId: string,
  body: {
    bankName: string
    accountName?: string
    accountNo?: string
    currency?: number
    isDefault?: boolean
    isDisabled?: boolean
  }
): Promise<FreightForwarderCompanyBank> {
  return apiClient.put<FreightForwarderCompanyBank>(`/api/v1/freight-forwarder-companies/banks/${bankId}`, body)
}

export async function deleteFfCompanyBank(bankId: string): Promise<void> {
  await apiClient.delete(`/api/v1/freight-forwarder-companies/banks/${bankId}`)
}
