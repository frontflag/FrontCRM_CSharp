<template>
  <div class="mailbox-panel" v-loading="loading">
    <div class="section-head">
      <div class="section-head__left">
        <h3 class="section-title">{{ t('profilePage.mailbox.sectionTitle') }}</h3>
        <p class="section-hint">{{ t('profilePage.mailbox.sectionHint') }}</p>
      </div>
      <div class="section-head__actions">
        <el-button type="primary" plain @click="openCreate('platform')">
          {{ t('profilePage.mailbox.addPlatform') }}
        </el-button>
        <el-button type="primary" @click="openCreate('personal')">
          {{ t('profilePage.mailbox.addPersonal') }}
        </el-button>
      </div>
    </div>

    <el-alert v-if="suffixMissingHint" type="info" :closable="false" show-icon class="mb-12">
      {{ t('profilePage.mailbox.suffixMissing') }}
    </el-alert>

    <div v-if="!loading && rows.length === 0" class="empty-hint">{{ t('profilePage.mailbox.noRows') }}</div>

    <div v-for="row in rows" :key="row.id" class="mailbox-card">
      <div class="mailbox-card__head">
        <!-- 邮箱地址 + 类型/状态标签紧跟其后；操作按钮靠右 -->
        <div class="mailbox-address-block">
          <span class="mailbox-address-label">{{ t('profilePage.mailbox.address') }}</span>
          <div v-if="row.kind === 'platform'" class="mailbox-address-edit mailbox-address-edit--head">
            <el-input
              v-model="draft[row.id].localPart"
              class="mailbox-local-input"
              size="small"
              clearable
              :placeholder="t('profilePage.mailbox.localPart')"
            />
            <span class="mailbox-suffix">{{ platformSuffixOf(row) }}</span>
          </div>
          <el-input
            v-else
            v-model="draft[row.id].address"
            class="mailbox-address-input--head"
            size="small"
            clearable
          />
          <div class="mailbox-card__meta">
            <el-tag size="small" :type="row.kind === 'platform' ? 'primary' : 'info'">
              {{
                row.kind === 'platform'
                  ? t('profilePage.mailbox.kindPlatform')
                  : t('profilePage.mailbox.kindPersonal')
              }}
            </el-tag>
            <el-tag size="small" :type="statusTagType(row.verifyStatus)" effect="plain">
              {{ statusLabel(row.verifyStatus) }}
            </el-tag>
            <el-radio
              v-if="row.kind === 'platform' && row.verifyStatus === 'ok'"
              :model-value="defaultSendId"
              :value="row.id"
              size="small"
              :disabled="defaultingId === row.id"
              @change="onSetDefault(row)"
            >
              {{ t('profilePage.mailbox.defaultSend') }}
            </el-radio>
          </div>
        </div>
        <div class="mailbox-card__actions">
          <el-button
            type="primary"
            size="small"
            :loading="verifyingId === row.id"
            :disabled="!row.passwordSet && !draftPassword[row.id]"
            @click="onVerify(row)"
          >
            {{ t('profilePage.mailbox.verify') }}
          </el-button>
        </div>
      </div>

      <!-- 仅验证失败时展示，位于地址行与密码行之间 -->
      <p
        v-if="row.verifyStatus === 'fail' && row.verifyMessage"
        class="verify-msg verify-msg--fail-banner"
      >
        {{ row.verifyMessage }}
      </p>

      <el-form label-width="110px" class="mailbox-form" size="default">
        <el-form-item :label="t('profilePage.mailbox.password')">
          <div class="pwd-row">
            <el-input
              v-model="draftPassword[row.id]"
              class="mailbox-control"
              :type="revealed[row.id] ? 'text' : 'password'"
              :placeholder="
                row.passwordSet
                  ? t('profilePage.mailbox.phPasswordKeep')
                  : t('profilePage.mailbox.phPassword')
              "
              clearable
              autocomplete="new-password"
            />
            <el-button
              v-if="row.passwordSet"
              link
              type="primary"
              :loading="revealingId === row.id"
              @click="toggleReveal(row)"
            >
              {{ revealed[row.id] ? '🙈' : '👁' }}
            </el-button>
          </div>
        </el-form-item>
        <el-form-item :label="t('profilePage.mailbox.displayName')">
          <el-input
            v-model="draft[row.id].displayName"
            class="mailbox-control"
            clearable
            :placeholder="t('profilePage.mailbox.phDisplayName')"
          />
          <p class="field-hint">{{ t('profilePage.mailbox.displayNameHint') }}</p>
        </el-form-item>
        <el-form-item v-if="row.kind === 'personal'" :label="t('profilePage.mailbox.imapHost')">
          <div class="pop-row">
            <el-input v-model="draft[row.id].imapHost" class="mailbox-control" clearable />
            <span class="pop-inline-label">{{ t('profilePage.mailbox.imapPort') }}</span>
            <el-input-number
              v-model="draft[row.id].imapPort"
              class="pop-port"
              :min="1"
              :max="65535"
              controls-position="right"
            />
            <span class="pop-inline-label">{{ t('profilePage.mailbox.imapSsl') }}</span>
            <el-switch v-model="draft[row.id].imapUseSsl" />
          </div>
        </el-form-item>
      </el-form>

      <div class="mailbox-card__foot">
        <div class="mailbox-card__foot-actions">
          <el-button size="small" :loading="savingId === row.id" @click="onSave(row)">
            {{ t('profilePage.mailbox.save') }}
          </el-button>
          <el-button size="small" type="danger" plain :loading="deletingId === row.id" @click="onDelete(row)">
            {{ t('profilePage.mailbox.delete') }}
          </el-button>
        </div>
      </div>
    </div>

    <el-dialog
      v-model="createVisible"
      :title="
        createKind === 'platform'
          ? t('profilePage.mailbox.addPlatform')
          : t('profilePage.mailbox.addPersonal')
      "
      width="520px"
      destroy-on-close
      @closed="resetCreate"
    >
      <el-form label-width="110px">
        <el-form-item v-if="createKind === 'platform'" :label="t('profilePage.mailbox.address')">
          <div class="mailbox-address-edit mailbox-address-edit--dialog">
            <el-input
              v-model="createForm.localPart"
              class="mailbox-local-input"
              clearable
              :placeholder="t('profilePage.mailbox.localPart')"
            />
            <span class="mailbox-suffix">{{ createPlatformSuffix || '—' }}</span>
          </div>
        </el-form-item>
        <el-form-item v-else :label="t('profilePage.mailbox.address')">
          <el-input v-model="createForm.address" clearable />
        </el-form-item>
        <el-form-item :label="t('profilePage.mailbox.password')">
          <el-input v-model="createForm.password" type="password" show-password autocomplete="new-password" />
        </el-form-item>
        <el-form-item :label="t('profilePage.mailbox.displayName')">
          <el-input
            v-model="createForm.displayName"
            clearable
            :placeholder="t('profilePage.mailbox.phDisplayName')"
          />
          <p class="field-hint">{{ t('profilePage.mailbox.displayNameHint') }}</p>
        </el-form-item>
        <template v-if="createKind === 'personal'">
          <el-form-item :label="t('profilePage.mailbox.imapHost')">
            <el-input v-model="createForm.imapHost" clearable />
          </el-form-item>
          <el-form-item :label="t('profilePage.mailbox.imapPort')">
            <el-input-number v-model="createForm.imapPort" :min="1" :max="65535" controls-position="right" />
          </el-form-item>
          <el-form-item :label="t('profilePage.mailbox.imapSsl')">
            <el-switch v-model="createForm.imapUseSsl" />
          </el-form-item>
        </template>
      </el-form>
      <template #footer>
        <el-button @click="createVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="creating" @click="submitCreate">{{ t('profilePage.mailbox.save') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  createMyMailbox,
  deleteMyMailbox,
  fetchMyMailboxes,
  revealMyMailboxPassword,
  setMyMailboxDefaultSend,
  updateMyMailbox,
  verifyMyMailbox,
  type MailboxKind,
  type MailboxVerifyStatus,
  type UserMailbox,
  type UserMailboxWrite
} from '@/api/userMailboxes'
import { getApiErrorMessage } from '@/utils/apiError'

type DraftRow = {
  displayName: string
  localPart: string
  address: string
  imapHost: string
  imapPort: number
  imapUseSsl: boolean
}

const { t } = useI18n()

const loading = ref(false)
const rows = ref<UserMailbox[]>([])
const draft = reactive<Record<string, DraftRow>>({})
const draftPassword = reactive<Record<string, string>>({})
const revealed = reactive<Record<string, string>>({})
const savingId = ref<string | null>(null)
const verifyingId = ref<string | null>(null)
const deletingId = ref<string | null>(null)
const revealingId = ref<string | null>(null)
const defaultingId = ref<string | null>(null)

const defaultSendId = computed(() => rows.value.find((r) => r.isDefaultSend)?.id ?? '')

const createVisible = ref(false)
const createKind = ref<MailboxKind>('personal')
const creating = ref(false)
const createForm = reactive({
  displayName: '',
  localPart: '',
  address: '',
  password: '',
  imapHost: '',
  imapPort: 993,
  imapUseSsl: true
})

/** 创建平台邮箱因未配后缀失败后提示；列表含平台行时清除 */
const suffixMissingHint = computed(
  () => suffixMissingFlag.value && !rows.value.some((r) => r.kind === 'platform')
)
const suffixMissingFlag = ref(false)

function statusLabel(s: MailboxVerifyStatus) {
  if (s === 'ok') return t('profilePage.mailbox.statusOk')
  if (s === 'fail') return t('profilePage.mailbox.statusFail')
  return t('profilePage.mailbox.statusNone')
}

function statusTagType(s: MailboxVerifyStatus): 'success' | 'danger' | 'info' {
  if (s === 'ok') return 'success'
  if (s === 'fail') return 'danger'
  return 'info'
}

/** 从完整地址取出 @ 及之后的后缀（只读展示） */
function suffixFromAddress(address?: string | null): string {
  const a = (address || '').trim()
  const i = a.indexOf('@')
  return i >= 0 ? a.slice(i) : ''
}

function localFromAddress(address?: string | null): string {
  const a = (address || '').trim()
  const i = a.indexOf('@')
  return i >= 0 ? a.slice(0, i) : a
}

function platformSuffixOf(row: UserMailbox): string {
  return suffixFromAddress(row.address) || createPlatformSuffix.value || '—'
}

/** 新建平台邮箱时展示的只读后缀（取自已有平台行） */
const createPlatformSuffix = computed(() => {
  const p = rows.value.find((r) => r.kind === 'platform' && suffixFromAddress(r.address))
  return p ? suffixFromAddress(p.address) : ''
})

function syncDraftFromRows(list: UserMailbox[]) {
  for (const key of Object.keys(draft)) delete draft[key]
  for (const key of Object.keys(draftPassword)) delete draftPassword[key]
  for (const key of Object.keys(revealed)) delete revealed[key]
  for (const row of list) {
    draft[row.id] = {
      displayName: row.displayName || '',
      localPart: (row.localPart || localFromAddress(row.address) || '').trim(),
      address: row.address || '',
      imapHost: row.imapHost || '',
      imapPort: row.imapPort && row.imapPort >= 1 ? row.imapPort : 993,
      imapUseSsl: row.imapUseSsl !== false
    }
    draftPassword[row.id] = ''
  }
}

async function load() {
  loading.value = true
  try {
    const list = await fetchMyMailboxes()
    rows.value = list
    syncDraftFromRows(list)
    if (list.some((r) => r.kind === 'platform')) suffixMissingFlag.value = false
    else if (list.length === 0) suffixMissingFlag.value = true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('profilePage.mailbox.loadFailed')))
  } finally {
    loading.value = false
  }
}

function openCreate(kind: MailboxKind) {
  createKind.value = kind
  resetCreate()
  createVisible.value = true
}

function resetCreate() {
  createForm.displayName = ''
  createForm.localPart = ''
  createForm.address = ''
  createForm.password = ''
  createForm.imapHost = ''
  createForm.imapPort = 993
  createForm.imapUseSsl = true
}

async function submitCreate() {
  creating.value = true
  try {
    const body: UserMailboxWrite = {
      kind: createKind.value,
      displayName: createForm.displayName,
      password: createForm.password
    }
    if (createKind.value === 'platform') {
      body.localPart = createForm.localPart.trim().replace(/^@+/, '').split('@')[0]
    } else {
      body.address = createForm.address
      body.imapHost = createForm.imapHost
      body.imapPort = createForm.imapPort
      body.imapUseSsl = createForm.imapUseSsl
    }
    await createMyMailbox(body)
    ElMessage.success(t('profilePage.mailbox.saved'))
    createVisible.value = false
    suffixMissingFlag.value = false
    await load()
  } catch (e) {
    const msg = getApiErrorMessage(e, t('profilePage.mailbox.saveFailed'))
    if (msg.includes('后缀') || msg.toLowerCase().includes('suffix')) {
      suffixMissingFlag.value = true
    }
    ElMessage.error(msg)
  } finally {
    creating.value = false
  }
}

function buildWrite(row: UserMailbox): UserMailboxWrite {
  const d = draft[row.id]
  const body: UserMailboxWrite = {
    kind: row.kind,
    displayName: d.displayName
  }
  const pwd = (draftPassword[row.id] || '').trim()
  if (pwd) body.password = pwd
  if (row.kind === 'platform') {
    // 仅提交本地部分，去掉误输入的 @ 及后缀
    body.localPart = (d.localPart || '').trim().replace(/^@+/, '').split('@')[0]
  } else {
    body.address = d.address
    body.imapHost = d.imapHost
    body.imapPort = d.imapPort
    body.imapUseSsl = d.imapUseSsl
  }
  return body
}

async function onSave(row: UserMailbox) {
  savingId.value = row.id
  try {
    await updateMyMailbox(row.id, buildWrite(row))
    ElMessage.success(t('profilePage.mailbox.saved'))
    await load()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('profilePage.mailbox.saveFailed')))
  } finally {
    savingId.value = null
  }
}

async function onVerify(row: UserMailbox) {
  const d = draft[row.id]
  const localChanged =
    row.kind === 'platform' &&
    (d.localPart || '').trim().replace(/^@+/, '').split('@')[0] !==
      (row.localPart || localFromAddress(row.address) || '').trim()
  // 本地部分或密码有未保存改动时先保存再验
  if ((draftPassword[row.id] || '').trim() || localChanged) {
    await onSave(row)
  }
  verifyingId.value = row.id
  try {
    const result = await verifyMyMailbox(row.id)
    // 分步弹消息：先 POP，再 SMTP（平台）
    if (result.imapOk ?? result.popOk) {
      ElMessage.success((result.imapMessage ?? result.popMessage) || 'IMAP 收信验证成功')
    } else {
      ElMessage.error((result.imapMessage ?? result.popMessage) || 'IMAP 收信验证失败')
    }
    if (result.smtpOk != null) {
      if (result.smtpOk) {
        ElMessage.success(result.smtpMessage || 'SMTP 发信验证成功')
      } else {
        ElMessage.error(result.smtpMessage || 'SMTP 发信验证失败')
      }
    }
    await load()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('profilePage.mailbox.verifyFailed')))
  } finally {
    verifyingId.value = null
  }
}

async function onSetDefault(row: UserMailbox) {
  if (row.id === defaultSendId.value) return
  defaultingId.value = row.id
  try {
    await setMyMailboxDefaultSend(row.id)
    ElMessage.success(t('profilePage.mailbox.setDefaultOk'))
    await load()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('profilePage.mailbox.setDefaultFailed')))
  } finally {
    defaultingId.value = null
  }
}

async function onDelete(row: UserMailbox) {
  try {
    await ElMessageBox.confirm(
      t('profilePage.mailbox.deleteConfirm', { address: row.address }),
      t('profilePage.mailbox.delete'),
      { type: 'warning' }
    )
  } catch {
    return
  }
  deletingId.value = row.id
  try {
    await deleteMyMailbox(row.id)
    ElMessage.success(t('profilePage.mailbox.deleted'))
    await load()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('profilePage.mailbox.deleteFailed')))
  } finally {
    deletingId.value = null
  }
}

async function toggleReveal(row: UserMailbox) {
  if (revealed[row.id] != null) {
    delete revealed[row.id]
    return
  }
  revealingId.value = row.id
  try {
    const pwd = await revealMyMailboxPassword(row.id)
    revealed[row.id] = pwd
    draftPassword[row.id] = pwd
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('profilePage.mailbox.revealFailed')))
  } finally {
    revealingId.value = null
  }
}

onMounted(() => {
  void load()
})
</script>

<style scoped lang="scss">
@use '@/assets/styles/variables' as vars;

// 与头部「邮箱地址」控件同宽，保证三行输入左对齐
$mailbox-control-width: 280px;

.section-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 16px;
  flex-wrap: wrap;
}

.section-title {
  margin: 0 0 4px;
  font-size: 16px;
  font-weight: 600;
  color: vars.$text-primary;
}

.section-hint {
  margin: 0;
  font-size: 12px;
  color: vars.$text-muted;
  max-width: 520px;
}

.section-head__actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.mb-12 {
  margin-bottom: 12px;
}

.empty-hint {
  color: vars.$text-muted;
  font-size: 13px;
  padding: 24px 0;
}

.mailbox-card {
  background: vars.$layer-2;
  border: 1px solid vars.$border-card;
  border-radius: 8px;
  padding: 14px 16px 6px;
  margin-bottom: 12px;
}

.mailbox-card__head {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 10px;
  flex-wrap: wrap;
}

.mailbox-card__meta {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  flex-shrink: 0;
  margin-left: 10px;
}

.mailbox-address-block {
  display: flex;
  align-items: center;
  gap: 0;
  min-width: 0;
  flex-shrink: 0;
}

.mailbox-address-label {
  width: 110px;
  flex-shrink: 0;
  padding-right: 12px;
  box-sizing: border-box;
  text-align: right;
  font-size: 14px;
  color: vars.$text-primary;
  line-height: 24px;
}

.mailbox-control {
  width: $mailbox-control-width;
  max-width: 100%;
}

.pop-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px 10px;
  min-width: 0;
  width: 100%;
}

.pop-inline-label {
  flex-shrink: 0;
  font-size: 13px;
  color: vars.$text-primary;
  white-space: nowrap;
}

.pop-port {
  width: 120px;
  flex-shrink: 0;
}

.mailbox-address-edit {
  display: flex;
  align-items: stretch;
  min-width: 0;

  &--head {
    width: $mailbox-control-width;
    max-width: 100%;
  }

  &--dialog {
    width: 100%;
    max-width: none;
  }
}

.mailbox-address-input--head {
  width: $mailbox-control-width;
  max-width: 100%;
}

.mailbox-local-input {
  flex: 1;
  min-width: 72px;
  width: auto;

  :deep(.el-input__wrapper) {
    border-top-right-radius: 0;
    border-bottom-right-radius: 0;
  }
}

.mailbox-address-edit--dialog .mailbox-local-input {
  flex: 1;
  width: auto;
  min-width: 120px;
}

.mailbox-suffix {
  display: inline-flex;
  align-items: center;
  flex-shrink: 0;
  height: 24px;
  padding: 0 8px;
  font-size: 12px;
  color: vars.$text-muted;
  background: rgba(0, 0, 0, 0.12);
  border: 1px solid vars.$border-panel;
  border-left: none;
  border-radius: 0 4px 4px 0;
  white-space: nowrap;
  user-select: none;
}

.mailbox-address-edit--dialog .mailbox-suffix {
  height: 32px;
  padding: 0 12px;
  font-size: 13px;
}

.field-hint {
  margin: 6px 0 0;
  max-width: 520px;
  font-size: 12px;
  line-height: 1.5;
  color: vars.$text-muted;
}

.mailbox-card__actions {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
  flex-shrink: 0;
  margin-left: auto;
}

.pwd-row {
  display: flex;
  align-items: center;
  gap: 6px;
}

.mailbox-card__foot {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  margin: 0 0 8px;
  min-height: 32px;
}

.mailbox-card__foot-actions {
  display: flex;
  gap: 6px;
  flex-shrink: 0;
}

.verify-msg--fail-banner {
  margin: 0 0 10px 110px;
  font-size: 12px;
  line-height: 1.5;
  color: #f56c6c;
}
</style>
