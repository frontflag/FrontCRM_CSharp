import apiClient from './client'

export interface MyMailSummary {
  hasVerifiedMailbox: boolean
  verifiedMailboxCount: number
  totalCount: number
  unreadCount: number
}

export interface MyMailMailboxOption {
  id: string
  address: string
  kind: string
  displayName?: string | null
}

export interface MyMailListItem {
  id: string
  mailboxId: string
  mailboxAddress: string
  subject?: string | null
  snippet?: string | null
  fromAddress?: string | null
  fromName?: string | null
  receivedAt?: string | null
  isUnread: boolean
  hasAttachments: boolean
}

export interface MyMailDetail extends MyMailListItem {
  toAddresses?: string | null
  bodyText?: string | null
  bodyHtml?: string | null
  messageId?: string | null
}

export interface MyMailSyncResult {
  mailboxCount: number
  fetchedCount: number
  upsertedCount: number
  errors: string[]
}

export interface MyMailListQuery {
  mailboxId?: string
  subject?: string
  from?: string
  body?: string
  receivedFrom?: string
  receivedTo?: string
  page?: number
  pageSize?: number
}

export interface MyMailPaged {
  items: MyMailListItem[]
  total: number
  page: number
  pageSize: number
}

const BASE = '/api/v1/me/mails'

export async function fetchMyMailSummary(): Promise<MyMailSummary> {
  return await apiClient.get<MyMailSummary>(`${BASE}/summary`)
}

export async function fetchMyMailMailboxOptions(): Promise<MyMailMailboxOption[]> {
  const res = await apiClient.get<MyMailMailboxOption[]>(`${BASE}/mailboxes`)
  return Array.isArray(res) ? res : []
}

export async function fetchMyMails(query: MyMailListQuery): Promise<MyMailPaged> {
  return await apiClient.get<MyMailPaged>(BASE, { params: query })
}

export async function fetchMyMailDetail(id: string): Promise<MyMailDetail> {
  return await apiClient.get<MyMailDetail>(`${BASE}/${encodeURIComponent(id)}`)
}

export async function syncMyMails(mailboxId?: string | null): Promise<MyMailSyncResult> {
  return await apiClient.post<MyMailSyncResult>(
    `${BASE}/sync`,
    { mailboxId: mailboxId || null },
    { timeout: 180_000 }
  )
}

export async function markMyMailRead(id: string): Promise<void> {
  await apiClient.post(`${BASE}/${encodeURIComponent(id)}/read`)
}
