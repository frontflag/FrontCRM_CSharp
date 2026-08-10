import apiClient from './client'

export type MailboxKind = 'platform' | 'personal'
export type MailboxVerifyStatus = 'none' | 'ok' | 'fail'

export type MailboxSendBlockReason =
  | 'SmtpDisabled'
  | 'SmtpHostMissing'
  | 'NoDefaultMailbox'
  | 'DefaultNotVerified'
  | 'SmtpRejected'
  | string

export interface UserMailbox {
  id: string
  kind: MailboxKind
  address: string
  localPart?: string | null
  displayName?: string | null
  passwordSet: boolean
  isDefaultSend?: boolean
  popHost?: string | null
  popPort?: number | null
  popUseSsl: boolean
  verifyStatus: MailboxVerifyStatus
  verifyMessage?: string | null
  verifiedAt?: string | null
}

export interface UserMailboxWrite {
  kind: MailboxKind
  localPart?: string
  address?: string
  displayName?: string
  password?: string
  popHost?: string
  popPort?: number
  popUseSsl?: boolean
}

export interface MailboxSendReady {
  ready: boolean
  blockReason?: MailboxSendBlockReason | null
}

export interface VerifiedUserMailboxRow {
  id: string
  userId: string
  userName: string
  realName?: string | null
  kind: MailboxKind | string
  address: string
  displayName?: string | null
  passwordSet: boolean
  verifiedAt?: string | null
}

const ME = '/api/v1/me/mailboxes'
const COMPANY = '/api/v1/company-profile'

export async function fetchMyMailboxes(): Promise<UserMailbox[]> {
  const res = await apiClient.get<UserMailbox[]>(ME)
  return Array.isArray(res) ? res : []
}

export async function fetchMailboxSendReady(): Promise<MailboxSendReady> {
  const res = await apiClient.get<MailboxSendReady>(`${ME}/send-ready`)
  return {
    ready: !!res?.ready,
    blockReason: res?.blockReason ?? null
  }
}

export async function createMyMailbox(body: UserMailboxWrite): Promise<UserMailbox> {
  return (await apiClient.post<UserMailbox>(ME, body)) as UserMailbox
}

export async function updateMyMailbox(id: string, body: UserMailboxWrite): Promise<UserMailbox> {
  return (await apiClient.put<UserMailbox>(`${ME}/${encodeURIComponent(id)}`, body)) as UserMailbox
}

export async function setMyMailboxDefaultSend(id: string): Promise<UserMailbox> {
  return (await apiClient.put<UserMailbox>(
    `${ME}/${encodeURIComponent(id)}/default-send`
  )) as UserMailbox
}

export async function deleteMyMailbox(id: string): Promise<void> {
  await apiClient.delete(`${ME}/${encodeURIComponent(id)}`)
}

export interface MailboxVerifyResult {
  mailbox: UserMailbox
  success: boolean
  popOk: boolean
  popMessage: string
  smtpOk?: boolean | null
  smtpMessage?: string | null
}

export async function verifyMyMailbox(id: string): Promise<MailboxVerifyResult> {
  // POP + SMTP 真发测试信常超过默认 10s
  const res = (await apiClient.post<MailboxVerifyResult>(
    `${ME}/${encodeURIComponent(id)}/verify`,
    undefined,
    { timeout: 120_000 }
  )) as MailboxVerifyResult
  return {
    mailbox: res.mailbox,
    success: !!res.success,
    popOk: !!res.popOk,
    popMessage: res.popMessage || '',
    smtpOk: res.smtpOk,
    smtpMessage: res.smtpMessage
  }
}

export async function revealMyMailboxPassword(id: string): Promise<string> {
  const res = await apiClient.get<{ password?: string }>(`${ME}/${encodeURIComponent(id)}/password`)
  return String(res?.password ?? '')
}

export async function fetchVerifiedUserMailboxes(): Promise<VerifiedUserMailboxRow[]> {
  const res = await apiClient.get<VerifiedUserMailboxRow[]>(`${COMPANY}/verified-user-mailboxes`)
  return Array.isArray(res) ? res : []
}

export async function revealVerifiedMailboxPassword(id: string): Promise<string> {
  const res = await apiClient.get<{ password?: string }>(
    `${COMPANY}/verified-user-mailboxes/${encodeURIComponent(id)}/password`
  )
  return String(res?.password ?? '')
}
