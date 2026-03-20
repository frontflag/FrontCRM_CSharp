import apiClient from './client'

export type DebugItem = {
  name: string
  value: string
}

export async function getDebugItems(): Promise<DebugItem[]> {
  return apiClient.get<DebugItem[]>('/api/v1/debug')
}

