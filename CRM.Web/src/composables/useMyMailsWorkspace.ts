import { computed, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  deleteMyMail,
  restoreMyMail,
  fetchMyMailDetail,
  fetchMyMailMailboxOptions,
  fetchMyMails,
  fetchMyMailSummary,
  markMyMailRead,
  markAllMyMailsRead,
  setMyMailStarred,
  saveMyMailRemark,
  clearMyMailRemark,
  sendMyMail,
  saveMyMailDraft,
  syncMyMails,
  fetchMyMailAddressBook,
  type MyMailAddressBookItem,
  type MyMailDetail,
  type MyMailListItem,
  type MyMailMailboxOption,
  type MyMailSummary
} from '@/api/myMails'
import { getApiErrorMessage } from '@/utils/apiError'
import { normalizeAuthUserId } from '@/utils/authUserId'

const MAILBOX_PREF_PREFIX = 'crm-my-mails-mailbox:v1:'

function currentAuthUserId() {
  try {
    const raw = localStorage.getItem('user')
    return normalizeAuthUserId(raw ? JSON.parse(raw) : null, localStorage.getItem('token'))
  } catch {
    return ''
  }
}

function readSavedMailboxId() {
  const uid = currentAuthUserId()
  if (!uid) return ''
  try {
    return localStorage.getItem(MAILBOX_PREF_PREFIX + uid)?.trim() || ''
  } catch {
    return ''
  }
}

function persistMailboxId(id: string) {
  const uid = currentAuthUserId()
  if (!uid) return
  try {
    if (id) localStorage.setItem(MAILBOX_PREF_PREFIX + uid, id)
    else localStorage.removeItem(MAILBOX_PREF_PREFIX + uid)
  } catch {
    /* ignore quota */
  }
}

export type MyMailsViewMode = 'list' | 'body' | 'compose' | 'contacts'
export type MyMailsMainView = 'list' | 'contacts'
export type MyMailsReadFilter = 'all' | 'unread' | 'read' | 'starred' | 'remarked'
export type MyMailsFolderId = 'inbox' | 'draft' | 'sent' | 'deleted'
export type MyMailsComposeMode = 'new' | 'reply'

export interface MyMailsComposeDraft {
  mode: MyMailsComposeMode
  draftId: string
  to: string
  cc: string
  subject: string
  body: string
  inReplyToMailId: string
}

const summaryLoaded = ref(false)
const summary = reactive<MyMailSummary>({
  hasVerifiedMailbox: false,
  verifiedMailboxCount: 0,
  totalCount: 0,
  unreadCount: 0
})
const mailboxOptions = ref<MyMailMailboxOption[]>([])
const mailboxesLoaded = ref(false)
const mailboxId = ref('')
const folderId = ref<MyMailsFolderId>('inbox')
const readFilter = ref<MyMailsReadFilter>('all')
const receivedRange = ref<[string, string] | null>(null)
const keyword = ref('')
const rows = ref<MyMailListItem[]>([])
const loading = ref(false)
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const selectedId = ref('')
const viewMode = ref<MyMailsViewMode>('list')
const detail = ref<MyMailDetail | null>(null)
const detailLoading = ref(false)
const syncing = ref(false)
const sending = ref(false)
const savingDraft = ref(false)
const deleting = ref(false)
const restoring = ref(false)
const markingAllRead = ref(false)
const isDeletedFolder = computed(() => folderId.value === 'deleted')
const hasMailbox = computed(() => mailboxOptions.value.length > 0)
const remarkDraft = ref('')
const remarkSaving = ref(false)
const remarkClearing = ref(false)
const canSaveRemark = computed(() => remarkDraft.value.trim().length > 0)
const canClearRemark = computed(() => !!(detail.value?.remark?.trim()))
const sendFromDisplay = computed(() => {
  const box =
    mailboxOptions.value.find((m) => m.id === mailboxId.value) ?? mailboxOptions.value[0]
  return box?.address?.trim() || ''
})
const lastMainView = ref<MyMailsMainView>('list')
const addressKeyword = ref('')
const addressRows = ref<MyMailAddressBookItem[]>([])
const addressLoading = ref(false)
const addressTotal = ref(0)
const addressPage = ref(1)
const addressPageSize = ref(20)
const selectedAddressId = ref('')
const compose = reactive<MyMailsComposeDraft>({
  mode: 'new',
  draftId: '',
  to: '',
  cc: '',
  subject: '',
  body: '',
  inReplyToMailId: ''
})

function listFolderParam(): 'inbox' | 'deleted' | 'sent' | 'draft' {
  if (folderId.value === 'deleted') return 'deleted'
  if (folderId.value === 'sent') return 'sent'
  if (folderId.value === 'draft') return 'draft'
  return 'inbox'
}

function resetCompose() {
  compose.mode = 'new'
  compose.draftId = ''
  compose.to = ''
  compose.cc = ''
  compose.subject = ''
  compose.body = ''
  compose.inReplyToMailId = ''
}

export function formatMailAt(v?: string | null) {
  if (!v) return '—'
  const d = new Date(v)
  if (Number.isNaN(d.getTime())) return String(v)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export function formatMailFrom(row: { fromName?: string | null; fromAddress?: string | null }) {
  const name = row.fromName?.trim()
  if (name) return name
  return row.fromAddress?.trim() || '—'
}

export function useMyMailsWorkspace() {
  const { t } = useI18n()
  const selectedRow = computed(() => rows.value.find((r) => r.id === selectedId.value) ?? null)

  async function loadSummary() {
    try {
      const s = await fetchMyMailSummary()
      Object.assign(summary, s)
    } catch (e) {
      ElMessage.error(getApiErrorMessage(e, t('myMails.messages.loadFailed')))
    } finally {
      summaryLoaded.value = true
    }
  }

  function applyMailboxSelection() {
    const ids = mailboxOptions.value.map((m) => m.id)
    if (!ids.length) {
      mailboxId.value = ''
      return
    }
    const saved = readSavedMailboxId()
    mailboxId.value = saved && ids.includes(saved) ? saved : ids[0]
    persistMailboxId(mailboxId.value)
  }

  function onMailboxChange() {
    persistMailboxId(mailboxId.value)
    page.value = 1
    void loadList()
  }

  async function loadMailboxes() {
    try {
      mailboxOptions.value = await fetchMyMailMailboxOptions()
    } catch {
      mailboxOptions.value = []
    }
    applyMailboxSelection()
    mailboxesLoaded.value = true
  }

  async function loadList() {
    if (!hasMailbox.value) {
      rows.value = []
      total.value = 0
      selectedId.value = ''
      return
    }
    loading.value = true
    try {
      const isUnread =
        readFilter.value === 'unread' ? true : readFilter.value === 'read' ? false : undefined
      const data = await fetchMyMails({
        mailboxId: mailboxId.value || undefined,
        q: keyword.value.trim() || undefined,
        isUnread,
        isStarred: readFilter.value === 'starred' ? true : undefined,
        hasRemark: readFilter.value === 'remarked' ? true : undefined,
        folder: listFolderParam(),
        receivedFrom: receivedRange.value?.[0],
        receivedTo: receivedRange.value?.[1],
        page: page.value,
        pageSize: pageSize.value
      })
      rows.value = data?.items ?? []
      total.value = data?.total ?? 0
      if (selectedId.value && !rows.value.some((r) => r.id === selectedId.value)) {
        selectedId.value = ''
      }
    } catch (e) {
      rows.value = []
      total.value = 0
      ElMessage.error(getApiErrorMessage(e, t('myMails.messages.loadFailed')))
    } finally {
      loading.value = false
    }
  }

  function search() {
    if (viewMode.value === 'contacts') {
      addressPage.value = 1
      void loadAddressBook()
      return
    }
    if (!hasMailbox.value) return
    page.value = 1
    void loadList()
  }

  async function loadAddressBook() {
    addressLoading.value = true
    try {
      const data = await fetchMyMailAddressBook({
        q: addressKeyword.value.trim() || undefined,
        page: addressPage.value,
        pageSize: addressPageSize.value
      })
      addressRows.value = data?.items ?? []
      addressTotal.value = data?.total ?? 0
      if (selectedAddressId.value && !addressRows.value.some((r) => r.id === selectedAddressId.value)) {
        selectedAddressId.value = ''
      }
    } catch (e) {
      addressRows.value = []
      addressTotal.value = 0
      ElMessage.error(getApiErrorMessage(e, t('myMails.messages.loadFailed')))
    } finally {
      addressLoading.value = false
    }
  }

  function openAddressBook() {
    if (!hasMailbox.value) return
    lastMainView.value = 'contacts'
    viewMode.value = 'contacts'
    addressPage.value = 1
    void loadAddressBook()
  }

  async function receiveSelectedMailbox() {
    if (!hasMailbox.value || !mailboxId.value || syncing.value) return
    lastMainView.value = 'list'
    folderId.value = 'inbox'
    selectedId.value = ''
    detail.value = null
    viewMode.value = 'list'
    page.value = 1
    try {
      const result = await runSync(mailboxId.value)
      const err = (result.errors || []).filter(Boolean)
      if (!err.length) {
        ElMessage.success(
          t('myMails.messages.syncDone', {
            fetched: result.fetchedCount,
            upserted: result.upsertedCount
          })
        )
      }
    } catch (e) {
      ElMessage.error(getApiErrorMessage(e, t('myMails.messages.syncFailed')))
    }
  }

  function selectAddress(row: MyMailAddressBookItem) {
    selectedAddressId.value = row.id
  }

  function composeToAddress(row: MyMailAddressBookItem) {
    selectedAddressId.value = row.id
    startCompose(row.email)
  }

  function onAddressPageSizeChange() {
    addressPage.value = 1
    void loadAddressBook()
  }

  function selectListFilter(id: MyMailsReadFilter) {
    if (!hasMailbox.value) return
    lastMainView.value = 'list'
    if (readFilter.value === id && viewMode.value === 'list') return
    readFilter.value = id
    selectedId.value = ''
    viewMode.value = 'list'
    page.value = 1
    void loadList()
  }

  function applyRemarkToMail(id: string, remark: string | null) {
    const row = rows.value.find((r) => r.id === id)
    if (row) row.remark = remark
    if (detail.value?.id === id) detail.value.remark = remark
    remarkDraft.value = remark ?? ''
  }

  async function saveRemark() {
    const id = detail.value?.id || selectedId.value
    const text = remarkDraft.value.trim()
    if (!id || !text) return false
    remarkSaving.value = true
    try {
      await saveMyMailRemark(id, text)
      applyRemarkToMail(id, text)
      ElMessage.success(t('myMails.messages.remarkSaved'))
      return true
    } catch (e) {
      ElMessage.error(getApiErrorMessage(e, t('myMails.messages.remarkFailed')))
      return false
    } finally {
      remarkSaving.value = false
    }
  }

  async function clearRemark() {
    const id = detail.value?.id || selectedId.value
    if (!id || !canClearRemark.value) return false
    remarkClearing.value = true
    try {
      await clearMyMailRemark(id)
      applyRemarkToMail(id, null)
      ElMessage.success(t('myMails.messages.remarkCleared'))
      if (readFilter.value === 'remarked') {
        page.value = 1
        await loadList()
      }
      return true
    } catch (e) {
      ElMessage.error(getApiErrorMessage(e, t('myMails.messages.remarkFailed')))
      return false
    } finally {
      remarkClearing.value = false
    }
  }

  async function toggleStar(row: MyMailListItem) {
    if (!hasMailbox.value) return
    const next = !row.isStarred
    try {
      await setMyMailStarred(row.id, next)
      row.isStarred = next
      if (detail.value?.id === row.id) detail.value.isStarred = next
      if (readFilter.value === 'starred' && !next) {
        page.value = 1
        await loadList()
      }
    } catch (e) {
      ElMessage.error(getApiErrorMessage(e, t('myMails.messages.starFailed')))
    }
  }

  function selectFolder(id: MyMailsFolderId) {
    if (!hasMailbox.value) return
    lastMainView.value = 'list'
    if (folderId.value === id && viewMode.value === 'list') return
    folderId.value = id
    selectedId.value = ''
    detail.value = null
    viewMode.value = 'list'
    page.value = 1
    void loadList()
  }

  function onPageSizeChange() {
    page.value = 1
    void loadList()
  }

  function selectRow(row: MyMailListItem) {
    selectedId.value = row.id
  }

  async function openBody(row: MyMailListItem) {
    if (folderId.value === 'draft' || row.folder === 'DRAFT') {
      await openDraft(row)
      return
    }
    selectedId.value = row.id
    detailLoading.value = true
    try {
      detail.value = await fetchMyMailDetail(row.id)
      remarkDraft.value = detail.value?.remark ?? ''
      viewMode.value = 'body'
      if (row.isUnread) {
        await markMyMailRead(row.id)
        row.isUnread = false
        if (folderId.value !== 'deleted' && summary.unreadCount > 0) summary.unreadCount -= 1
        if (detail.value) detail.value.isUnread = false
      }
    } catch (e) {
      ElMessage.error(getApiErrorMessage(e, t('myMails.messages.loadFailed')))
    } finally {
      detailLoading.value = false
    }
  }

  function backToList() {
    viewMode.value = 'list'
  }

  async function markAllRead() {
    if (!hasMailbox.value || !mailboxId.value || markingAllRead.value) return
    markingAllRead.value = true
    try {
      const result = await markAllMyMailsRead({
        mailboxId: mailboxId.value,
        folder: listFolderParam()
      })
      const updated = result?.updatedCount ?? 0
      if (updated > 0) {
        ElMessage.success(t('myMails.messages.markAllReadDone'))
        if (detail.value) detail.value.isUnread = false
        await Promise.all([loadSummary(), loadList()])
      } else {
        ElMessage.info(t('myMails.messages.markAllReadEmpty'))
      }
    } catch (e) {
      ElMessage.error(getApiErrorMessage(e, t('myMails.messages.markAllReadFailed')))
    } finally {
      markingAllRead.value = false
    }
  }

  function replySubject(subject?: string | null) {
    const s = (subject || '').trim()
    if (!s) return 'Re: '
    return /^re\s*:/i.test(s) ? s : `Re: ${s}`
  }

  function startCompose(to?: string) {
    if (!hasMailbox.value) return
    resetCompose()
    compose.to = to?.trim() || ''
    viewMode.value = 'compose'
  }

  async function openDraft(row: MyMailListItem) {
    selectedId.value = row.id
    detailLoading.value = true
    try {
      const mail = await fetchMyMailDetail(row.id)
      compose.mode = 'new'
      compose.draftId = mail.id
      compose.to = mail.toAddresses?.trim() || ''
      compose.cc = mail.ccAddresses?.trim() || ''
      compose.subject = mail.subject || ''
      compose.body = mail.bodyText || ''
      compose.inReplyToMailId = mail.inReplyToMailId || ''
      detail.value = mail
      remarkDraft.value = mail.remark ?? ''
      viewMode.value = 'compose'
    } catch (e) {
      ElMessage.error(getApiErrorMessage(e, t('myMails.messages.loadFailed')))
    } finally {
      detailLoading.value = false
    }
  }

  function startReply(mail: MyMailDetail) {
    if (!hasMailbox.value) return
    compose.mode = 'reply'
    compose.draftId = ''
    compose.to = mail.fromAddress?.trim() || ''
    compose.cc = ''
    compose.subject = replySubject(mail.subject)
    compose.inReplyToMailId = mail.id
    const quoted = (mail.bodyText || '').trim()
    const header = [
      '',
      '',
      '----- Original Message -----',
      `From: ${formatMailFrom(mail)}${mail.fromAddress ? ` <${mail.fromAddress}>` : ''}`,
      `Date: ${formatMailAt(mail.receivedAt)}`,
      `Subject: ${mail.subject || ''}`,
      ''
    ].join('\n')
    compose.body = `${header}${quoted}`
    viewMode.value = 'compose'
  }

  function cancelCompose() {
    if (compose.mode === 'reply' && !compose.draftId && detail.value) {
      viewMode.value = 'body'
      return
    }
    viewMode.value = lastMainView.value
  }

  async function saveDraft() {
    if (!hasMailbox.value || !mailboxId.value || savingDraft.value) return false
    if (
      !compose.to.trim()
      && !compose.cc.trim()
      && !compose.subject.trim()
      && !compose.body.trim()
    ) {
      ElMessage.warning(t('myMails.messages.needDraftContent'))
      return false
    }
    savingDraft.value = true
    try {
      const result = await saveMyMailDraft({
        id: compose.draftId || undefined,
        mailboxId: mailboxId.value,
        to: compose.to,
        cc: compose.cc,
        subject: compose.subject,
        body: compose.body,
        inReplyToMailId: compose.inReplyToMailId || undefined
      })
      compose.draftId = result?.id || compose.draftId
      ElMessage.success(t('myMails.messages.draftSaved'))
      return true
    } catch (e) {
      ElMessage.error(getApiErrorMessage(e, t('myMails.messages.draftSaveFailed')))
      return false
    } finally {
      savingDraft.value = false
    }
  }

  async function sendCompose() {
    if (!compose.to.trim()) {
      ElMessage.warning(t('myMails.messages.needTo'))
      return false
    }
    if (!compose.subject.trim()) {
      ElMessage.warning(t('myMails.messages.needSubject'))
      return false
    }
    if (!compose.body.trim()) {
      ElMessage.warning(t('myMails.messages.needBody'))
      return false
    }
    sending.value = true
    try {
      await sendMyMail({
        to: compose.to.trim(),
        cc: compose.cc.trim() || undefined,
        subject: compose.subject.trim(),
        body: compose.body,
        inReplyToMailId: compose.inReplyToMailId || undefined,
        draftId: compose.draftId || undefined
      })
      ElMessage.success(t('myMails.messages.sent'))
      resetCompose()
      lastMainView.value = 'list'
      folderId.value = 'sent'
      selectedId.value = ''
      viewMode.value = 'list'
      page.value = 1
      await loadList()
      return true
    } catch (e) {
      ElMessage.error(getApiErrorMessage(e, t('myMails.messages.sendFailed')))
      return false
    } finally {
      sending.value = false
    }
  }

  async function deleteCurrent() {
    const id = detail.value?.id || selectedId.value || compose.draftId
    if (!id) {
      ElMessage.warning(t('myMails.messages.needMail'))
      return false
    }
    deleting.value = true
    try {
      await deleteMyMail(id)
      ElMessage.success(t('myMails.messages.deleted'))
      if (detail.value?.id === id) detail.value = null
      if (selectedId.value === id) selectedId.value = ''
      if (compose.draftId === id) resetCompose()
      viewMode.value = 'list'
      await loadSummary()
      await loadList()
      return true
    } catch (e) {
      ElMessage.error(getApiErrorMessage(e, t('myMails.messages.deleteFailed')))
      return false
    } finally {
      deleting.value = false
    }
  }

  async function restoreCurrent() {
    const id = detail.value?.id || selectedId.value
    if (!id) {
      ElMessage.warning(t('myMails.messages.needMail'))
      return false
    }
    restoring.value = true
    try {
      await restoreMyMail(id)
      ElMessage.success(t('myMails.messages.restored'))
      if (detail.value?.id === id) detail.value = null
      if (selectedId.value === id) selectedId.value = ''
      viewMode.value = 'list'
      await loadSummary()
      await loadList()
      return true
    } catch (e) {
      ElMessage.error(getApiErrorMessage(e, t('myMails.messages.restoreFailed')))
      return false
    } finally {
      restoring.value = false
    }
  }

  function comingSoon(message: string) {
    ElMessage.info(message)
  }

  async function runSync(syncMailboxId?: string | null) {
    syncing.value = true
    try {
      const result = await syncMyMails(syncMailboxId || null)
      const err = (result.errors || []).filter(Boolean)
      if (err.length) {
        ElMessage.warning(err.join('；'))
      }
      await loadSummary()
      await loadList()
      return result
    } finally {
      syncing.value = false
    }
  }

  async function ensureLoaded() {
    await Promise.all([loadSummary(), loadMailboxes()])
    await loadList()
  }

  return {
    summaryLoaded,
    summary,
    mailboxOptions,
    mailboxesLoaded,
    mailboxId,
    hasMailbox,
    sendFromDisplay,
    onMailboxChange,
    folderId,
    isDeletedFolder,
    readFilter,
    selectListFilter,
    toggleStar,
    remarkDraft,
    remarkSaving,
    remarkClearing,
    canSaveRemark,
    canClearRemark,
    saveRemark,
    clearRemark,
    receivedRange,
    keyword,
    rows,
    loading,
    total,
    page,
    pageSize,
    selectedId,
    selectedRow,
    viewMode,
    detail,
    detailLoading,
    syncing,
    sending,
    savingDraft,
    saveDraft,
    deleting,
    restoring,
    markingAllRead,
    markAllRead,
    compose,
    startCompose,
    startReply,
    cancelCompose,
    sendCompose,
    deleteCurrent,
    restoreCurrent,
    selectFolder,
    openAddressBook,
    receiveSelectedMailbox,
    addressKeyword,
    addressRows,
    addressLoading,
    addressTotal,
    addressPage,
    addressPageSize,
    selectedAddressId,
    loadAddressBook,
    selectAddress,
    composeToAddress,
    onAddressPageSizeChange,
    loadSummary,
    loadMailboxes,
    loadList,
    search,
    onPageSizeChange,
    selectRow,
    openBody,
    backToList,
    comingSoon,
    ensureLoaded
  }
}
