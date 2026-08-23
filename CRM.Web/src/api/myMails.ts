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
  isDefaultSend?: boolean
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
  isStarred?: boolean
  remark?: string | null
  hasAttachments: boolean
  isDeleted?: boolean
  folder?: string | null
}

export interface MyMailDetail extends MyMailListItem {
  toAddresses?: string | null
  bodyText?: string | null
  bodyHtml?: string | null
  messageId?: string | null
  ccAddresses?: string | null
  inReplyToMailId?: string | null
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
  q?: string
  isUnread?: boolean
  isStarred?: boolean
  hasRemark?: boolean
  folder?: 'inbox' | 'deleted' | 'sent' | 'draft'
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

export async function markAllMyMailsRead(body: {
  mailboxId: string
  folder?: 'inbox' | 'deleted' | 'sent' | 'draft'
}): Promise<{ updatedCount: number }> {
  return await apiClient.post<{ updatedCount: number }>(`${BASE}/read-all`, body)
}

export async function setMyMailStarred(id: string, starred: boolean): Promise<void> {
  await apiClient.post(`${BASE}/${encodeURIComponent(id)}/star`, { starred })
}

export async function saveMyMailRemark(id: string, remark: string): Promise<void> {
  await apiClient.post(`${BASE}/${encodeURIComponent(id)}/remark`, { remark })
}

export async function clearMyMailRemark(id: string): Promise<void> {
  await apiClient.post(`${BASE}/${encodeURIComponent(id)}/remark/clear`)
}

export async function deleteMyMail(id: string): Promise<void> {
  await apiClient.delete(`${BASE}/${encodeURIComponent(id)}`)
}

export async function restoreMyMail(id: string): Promise<void> {
  await apiClient.post(`${BASE}/${encodeURIComponent(id)}/restore`)
}

export interface MyMailSendRequest {
  to: string
  cc?: string
  subject: string
  body: string
  inReplyToMailId?: string
  draftId?: string
}

export async function sendMyMail(body: MyMailSendRequest): Promise<void> {
  await apiClient.post(`${BASE}/send`, body, { timeout: 60_000 })
}

export async function saveMyMailDraft(body: {
  id?: string
  mailboxId: string
  to?: string
  cc?: string
  subject?: string
  body?: string
  inReplyToMailId?: string
}): Promise<{ id: string }> {
  return await apiClient.post<{ id: string }>(`${BASE}/drafts`, body)
}

export interface MyMailAddressBookItem {
  id: string
  partyKind: 'customer' | 'vendor' | string
  partyId: string
  partyName?: string | null
  contactName?: string | null
  email: string
}

export interface MyMailAddressBookQuery {
  q?: string
  page?: number
  pageSize?: number
}

export interface MyMailAddressBookPaged {
  items: MyMailAddressBookItem[]
  total: number
  page: number
  pageSize: number
}

export async function fetchMyMailAddressBook(
  query: MyMailAddressBookQuery
): Promise<MyMailAddressBookPaged> {
  return await apiClient.get<MyMailAddressBookPaged>(`${BASE}/address-book`, { params: query })
}
