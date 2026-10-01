<template>
  <aside class="bbs-cats">
    <div class="bbs-cats__title-row">
      <div class="bbs-cats__title">{{ t('bbs.categories') }}</div>
      <el-tooltip
        v-if="canAssignModerator"
        :content="t('bbs.moderator.manageBoards')"
        placement="top"
        :hide-after="0"
      >
        <el-button
          link
          type="primary"
          size="small"
          class="bbs-cats__settings-btn"
          :aria-label="t('bbs.moderator.manageBoards')"
          @click="openManageDialog"
        >
          <el-icon :size="15"><Setting /></el-icon>
        </el-button>
      </el-tooltip>
    </div>
    <div class="bbs-cats__hint">{{ t('bbs.boardStatLegend') }}</div>
    <button
      type="button"
      class="bbs-cat"
      :class="{ 'is-active': activeKey === 'all' }"
      @click="emit('select', 'all')"
    >
      <span class="bbs-cat__icon" aria-hidden="true">
        <svg viewBox="0 0 24 24" width="16" height="16"><path fill="currentColor" d="M12 2a10 10 0 1 0 10 10h-2a8 8 0 1 1-8-8V2zm1 5v5.5l4 2.4-.9 1.5L11 13V7h2z"/></svg>
      </span>
      <span class="bbs-cat__label">{{ t('bbs.filters.all') }}</span>
      <span class="bbs-cat__stats" :title="statTitle(stats?.all)">
        <span>{{ formatCount(stats?.all?.subjectCount) }}</span>
        <span class="bbs-cat__stats-sep">/</span>
        <span>{{ formatCount(stats?.all?.viewCount) }}</span>
      </span>
    </button>
    <button
      type="button"
      class="bbs-cat"
      :class="{ 'is-active': activeKey === 'top' }"
      @click="emit('select', 'top')"
    >
      <span class="bbs-cat__icon" aria-hidden="true">
        <svg viewBox="0 0 24 24" width="16" height="16"><path fill="currentColor" d="M12 2l3 7h7l-5.5 4.5L18 21l-6-4-6 4 1.5-7.5L2 9h7l3-7z"/></svg>
      </span>
      <span class="bbs-cat__label">{{ t('bbs.topSection') }}</span>
      <span class="bbs-cat__stats" :title="statTitle(stats?.top)">
        <span>{{ formatCount(stats?.top?.subjectCount) }}</span>
        <span class="bbs-cat__stats-sep">/</span>
        <span>{{ formatCount(stats?.top?.viewCount) }}</span>
      </span>
    </button>

    <div
      v-for="tp in fixedBoardTypesVisible"
      :key="'fixed-' + tp"
      class="bbs-cat-block"
      :class="{ 'is-active': activeKey === tp }"
    >
      <button type="button" class="bbs-cat" @click="emit('select', tp)">
        <span class="bbs-cat__icon" aria-hidden="true">
          <!-- 系统更新 -->
          <svg
            v-if="tp === BbsSubjectType.SystemUpdate"
            viewBox="0 0 24 24"
            width="16"
            height="16"
          >
            <path
              fill="currentColor"
              d="M12 4V1L8 5l4 4V6c3.31 0 6 2.69 6 6 0 1.01-.25 1.97-.7 2.8l1.46 1.46C19.54 14.93 20 13.5 20 12c0-4.42-3.58-8-8-8zm0 14c-3.31 0-6-2.69-6-6 0-1.01.25-1.97.7-2.8L5.24 7.74C4.46 9.07 4 10.5 4 12c0 4.42 3.58 8 8 8v3l4-4-4-4v3z"
            />
          </svg>
          <!-- 操作说明 -->
          <svg
            v-else-if="tp === BbsSubjectType.OpsGuide"
            viewBox="0 0 24 24"
            width="16"
            height="16"
          >
            <path
              fill="currentColor"
              d="M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z"
            />
          </svg>
          <span v-else class="bbs-cat__icon--text">{{ typeShort(tp) }}</span>
        </span>
        <span class="bbs-cat__label">{{ boardLabel(tp) }}</span>
        <span class="bbs-cat__right">
          <span class="bbs-cat__stats" :title="statTitle(typeStat(tp))">
            <span>{{ formatCount(typeStat(tp)?.subjectCount) }}</span>
            <span class="bbs-cat__stats-sep">/</span>
            <span>{{ formatCount(typeStat(tp)?.viewCount) }}</span>
          </span>
        </span>
      </button>
    </div>

    <div
      v-if="movableBoardTypesVisible.length"
      class="bbs-cats__divider"
      role="separator"
      :aria-label="t('bbs.moderator.boardDivider')"
    />

    <div
      v-for="tp in movableBoardTypesVisible"
      :key="'move-' + tp"
      class="bbs-cat-block"
      :class="{ 'is-active': activeKey === tp }"
    >
      <button type="button" class="bbs-cat" @click="emit('select', tp)">
        <span class="bbs-cat__icon bbs-cat__icon--text" aria-hidden="true">{{ typeShort(tp) }}</span>
        <span class="bbs-cat__label">{{ boardLabel(tp) }}</span>
        <span class="bbs-cat__right">
          <span class="bbs-cat__stats" :title="statTitle(typeStat(tp))">
            <span>{{ formatCount(typeStat(tp)?.subjectCount) }}</span>
            <span class="bbs-cat__stats-sep">/</span>
            <span>{{ formatCount(typeStat(tp)?.viewCount) }}</span>
          </span>
          <el-tooltip
            v-if="canAssignModerator && bbsSupportsBoardModerator(tp)"
            :content="t('bbs.moderator.set')"
            placement="top"
            :hide-after="0"
          >
            <el-button
              link
              type="primary"
              size="small"
              class="bbs-cat__mod-btn"
              :aria-label="t('bbs.moderator.set')"
              @click.stop="openSetModerator(tp)"
            >
              <el-icon :size="14"><Setting /></el-icon>
            </el-button>
          </el-tooltip>
        </span>
      </button>
      <div v-if="bbsSupportsBoardModerator(tp)" class="bbs-cat__mod" @click.stop>
        <span class="bbs-cat__mod-label">{{ t('bbs.moderator.label') }}</span>
        <span class="bbs-cat__mod-name" :title="moderatorTitle(tp)">{{ moderatorDisplay(tp) }}</span>
      </div>
    </div>

    <!-- 单板块：名称 / 版主 / 删除 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogTitle"
      width="420px"
      destroy-on-close
      append-to-body
    >
      <div class="bbs-board-form">
        <div class="bbs-board-form__row">
          <label class="bbs-board-form__label">{{ t('bbs.moderator.boardName') }}</label>
          <el-input
            v-model="boardNameDraft"
            maxlength="50"
            show-word-limit
            clearable
            :placeholder="boardNamePlaceholder"
          />
        </div>
        <div class="bbs-board-form__row">
          <label class="bbs-board-form__label">{{ t('bbs.moderator.moderatorField') }}</label>
          <el-select
            v-model="selectedUserId"
            filterable
            clearable
            remote
            :remote-method="filterUsers"
            :loading="userLoading"
            style="width: 100%"
            :placeholder="t('bbs.moderator.pickPh')"
          >
            <el-option
              v-for="u in userOptions"
              :key="u.id"
              :label="u.label"
              :value="u.id"
            />
          </el-select>
        </div>
      </div>
      <template #footer>
        <div class="bbs-board-form__footer">
          <el-button
            v-if="canDeleteBoard"
            type="danger"
            plain
            :loading="deleting"
            :disabled="saving"
            @click="deleteBoard"
          >
            {{ t('bbs.moderator.deleteBoard') }}
          </el-button>
          <span class="bbs-board-form__footer-spacer" />
          <el-button @click="clearModerator" :disabled="saving || deleting">{{ t('bbs.moderator.clear') }}</el-button>
          <el-button type="primary" :loading="saving" :disabled="deleting" @click="saveModerator">{{ t('bbs.moderator.save') }}</el-button>
        </div>
      </template>
    </el-dialog>

    <!-- 分类管理：新建 + 拖放排序 -->
    <el-dialog
      v-model="manageVisible"
      :title="t('bbs.moderator.manageBoardsTitle')"
      width="460px"
      destroy-on-close
      append-to-body
      @closed="onManageClosed"
    >
      <div class="bbs-manage">
        <div class="bbs-manage__create">
          <el-input
            v-model="newBoardName"
            maxlength="50"
            show-word-limit
            clearable
            :placeholder="t('bbs.moderator.newBoardPh')"
            @keyup.enter="createBoard"
          />
          <el-button type="primary" :loading="creating" :disabled="!newBoardName.trim()" @click="createBoard">
            {{ t('bbs.moderator.createBoard') }}
          </el-button>
        </div>

        <p class="bbs-manage__hint">{{ t('bbs.moderator.manageBoardsHint') }}</p>

        <ul ref="sortableListEl" class="bbs-manage__list bbs-manage__list--sortable">
          <li
            v-for="tp in manageOrder"
            :key="'move-' + tp"
            class="bbs-manage__item"
            :data-type="tp"
          >
            <span class="bbs-manage__grip" :title="t('bbs.moderator.dragSort')" aria-hidden="true">⋮⋮</span>
            <span class="bbs-manage__name">{{ boardLabel(tp) }}</span>
          </li>
        </ul>
        <p v-if="!manageOrder.length" class="bbs-manage__empty">{{ t('bbs.moderator.noMovableBoards') }}</p>
      </div>
      <template #footer>
        <el-button @click="manageVisible = false">{{ t('bbs.moderator.close') }}</el-button>
      </template>
    </el-dialog>
  </aside>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import Sortable from 'sortablejs'
import type { SortableEvent } from 'sortablejs'
import { authApi } from '@/api/auth'
import {
  bbsApi,
  BbsSubjectType,
  BbsSubjectTypeI18nKey,
  BbsFixedBoardTypes,
  BbsBuiltinMovableTypes,
  bbsSupportsBoardModerator,
  bbsIsCustomBoardType,
  type BbsBoardModerator,
  type BbsBoardStatItem,
  type BbsBoardStats
} from '@/api/bbs'
import { useAuthStore } from '@/stores/auth'
import { getApiErrorMessage } from '@/utils/apiError'

export type BbsCategoryKey = 'all' | 'top' | number

const props = defineProps<{
  activeKey: BbsCategoryKey
}>()

const emit = defineEmits<{
  select: [key: BbsCategoryKey]
}>()

const { t } = useI18n()
const authStore = useAuthStore()
const stats = ref<BbsBoardStats | null>(null)
const moderators = ref<BbsBoardModerator[]>([])

const canAssignModerator = computed(
  () => authStore.user?.isSysAdmin === true || authStore.user?.isSysManager === true
)

function isBoardDeleted(type: number) {
  return !!moderatorOf(type)?.isDeleted
}

const fixedBoardTypesVisible = computed(() =>
  BbsFixedBoardTypes.filter((tp) => !isBoardDeleted(tp))
)

/** 按 sortOrder 的可拖放板块（含自定义） */
const movableBoardTypesVisible = computed(() =>
  collectMovableTypes().sort((a, b) => {
    const oa = boardSortOrder(a)
    const ob = boardSortOrder(b)
    if (oa !== ob) return oa - ob
    return a - b
  })
)

function collectMovableTypes(): number[] {
  const set = new Set<number>()
  for (const tp of BbsBuiltinMovableTypes) {
    if (!isBoardDeleted(tp)) set.add(tp)
  }
  for (const m of moderators.value) {
    const tp = Number(m.type)
    if (!Number.isFinite(tp) || tp <= 0) continue
    if (!bbsSupportsBoardModerator(tp)) continue
    if (m.isDeleted) continue
    set.add(tp)
  }
  return [...set]
}

const dialogVisible = ref(false)
const dialogType = ref<number | null>(null)
const selectedUserId = ref<string | undefined>()
const boardNameDraft = ref('')
const saving = ref(false)
const deleting = ref(false)
const userLoading = ref(false)
const allUsers = ref<Array<{ id: string; label: string; userName: string; realName?: string }>>([])
const userOptions = ref<Array<{ id: string; label: string; userName: string; realName?: string }>>([])

const manageVisible = ref(false)
const manageOrder = ref<number[]>([])
const newBoardName = ref('')
const creating = ref(false)
const reorderSaving = ref(false)
const sortableListEl = ref<HTMLElement | null>(null)
let sortableInst: Sortable | null = null

const dialogTitle = computed(() => t('bbs.moderator.setTitle'))

const boardNamePlaceholder = computed(() => {
  if (dialogType.value == null) return ''
  const m = moderatorOf(dialogType.value)
  if (m?.defaultName) return m.defaultName
  const key = BbsSubjectTypeI18nKey[dialogType.value]
  return key ? t(key) : ''
})

const canDeleteBoard = computed(() => {
  if (dialogType.value == null) return false
  if (!bbsSupportsBoardModerator(dialogType.value)) return false
  const m = moderatorOf(dialogType.value)
  const count = m?.subjectCount ?? typeStat(dialogType.value)?.subjectCount ?? 0
  return count === 0
})

const TYPE_SHORT: Record<number, string> = {
  [BbsSubjectType.CompanyNotice]: '通',
  [BbsSubjectType.IndustryNews]: '讯',
  [BbsSubjectType.Share]: '享',
  [BbsSubjectType.OpsGuide]: '说',
  [BbsSubjectType.Suggestion]: '建',
  [BbsSubjectType.SystemUpdate]: '更'
}

function typeShort(type: number) {
  if (TYPE_SHORT[type]) return TYPE_SHORT[type]
  const label = boardLabel(type)
  return (label && label[0]) || '板'
}

function boardLabel(type: number) {
  const m = moderatorOf(type)
  const custom = (m?.displayName || '').trim()
  if (custom) return custom
  if (m?.defaultName) return m.defaultName
  const key = BbsSubjectTypeI18nKey[type]
  return key ? t(key) : bbsIsCustomBoardType(type) ? t('bbs.moderator.customBoard', { type }) : String(type)
}

function boardSortOrder(type: number) {
  const m = moderatorOf(type)
  const n = Number(m?.sortOrder ?? 0)
  if (Number.isFinite(n) && n > 0) return n
  const defaults: Record<number, number> = {
    [BbsSubjectType.CompanyNotice]: 10,
    [BbsSubjectType.IndustryNews]: 20,
    [BbsSubjectType.Share]: 30,
    [BbsSubjectType.Suggestion]: 40
  }
  if (defaults[type] != null) return defaults[type]
  if (bbsIsCustomBoardType(type)) return 100 + (type - BbsSubjectType.CustomMin) * 10
  return 100
}

function typeStat(type: number): BbsBoardStatItem | undefined {
  return stats.value?.byType?.find((x) => Number(x.type) === type || x.key === String(type))
}

function formatCount(n: number | null | undefined) {
  if (n == null || !Number.isFinite(n)) return '—'
  if (n >= 10000) return `${(n / 10000).toFixed(n >= 100000 ? 0 : 1)}万`
  return String(n)
}

function statTitle(item: BbsBoardStatItem | null | undefined) {
  if (!item) return ''
  return t('bbs.boardStatTip', { posts: item.subjectCount, views: item.viewCount })
}

function moderatorOf(type: number): BbsBoardModerator | undefined {
  return moderators.value.find((x) => Number(x.type) === type)
}

function moderatorDisplay(type: number) {
  const m = moderatorOf(type)
  const name = (m?.userName || m?.realName || '').trim()
  return name || t('bbs.moderator.unset')
}

function moderatorTitle(type: number) {
  const m = moderatorOf(type)
  if (!m?.userId) return ''
  const parts = [m.userName, m.realName].filter((x) => !!x && String(x).trim())
  return parts.join(' · ')
}

async function loadStats() {
  try {
    stats.value = await bbsApi.boardStats()
  } catch {
    stats.value = null
  }
}

async function loadModerators() {
  try {
    moderators.value = await bbsApi.boardModerators()
  } catch {
    moderators.value = []
  }
}

async function ensureUsersLoaded() {
  if (allUsers.value.length) return
  userLoading.value = true
  try {
    const res = (await authApi.getUsers()) as unknown
    const list = Array.isArray(res)
      ? res
      : Array.isArray((res as { data?: unknown })?.data)
        ? ((res as { data: unknown[] }).data)
        : []
    allUsers.value = (
      list as Array<{ id: string; label: string; userName: string; realName?: string }>
    ).map((u) => ({
      id: String(u.id),
      label: u.label || u.userName || u.id,
      userName: u.userName || '',
      realName: u.realName
    }))
    userOptions.value = allUsers.value.slice(0, 80)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.moderator.loadUsersFailed')))
  } finally {
    userLoading.value = false
  }
}

function filterUsers(query: string) {
  const q = (query || '').trim().toLowerCase()
  if (!q) {
    userOptions.value = allUsers.value.slice(0, 80)
    return
  }
  userOptions.value = allUsers.value
    .filter((u) => {
      const hay = `${u.userName} ${u.realName || ''} ${u.label}`.toLowerCase()
      return hay.includes(q)
    })
    .slice(0, 80)
}

async function openSetModerator(type: number) {
  if (!bbsSupportsBoardModerator(type)) return
  dialogType.value = type
  const m = moderatorOf(type)
  selectedUserId.value = m?.userId || undefined
  boardNameDraft.value = (m?.displayName || '').trim()
  dialogVisible.value = true
  await ensureUsersLoaded()
  filterUsers('')
}

async function saveModerator() {
  if (dialogType.value == null) return
  saving.value = true
  try {
    const dto = await bbsApi.setBoardModerator(dialogType.value, {
      userId: selectedUserId.value || null,
      displayName: boardNameDraft.value.trim() || null
    })
    upsertModerator(dto)
    await loadModerators()
    ElMessage.success(t('bbs.moderator.saveOk'))
    dialogVisible.value = false
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.moderator.saveFailed')))
  } finally {
    saving.value = false
  }
}

async function clearModerator() {
  if (dialogType.value == null) return
  saving.value = true
  try {
    selectedUserId.value = undefined
    const dto = await bbsApi.setBoardModerator(dialogType.value, {
      userId: null,
      displayName: boardNameDraft.value.trim() || null
    })
    upsertModerator(dto)
    ElMessage.success(t('bbs.moderator.clearOk'))
    dialogVisible.value = false
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.moderator.saveFailed')))
  } finally {
    saving.value = false
  }
}

async function deleteBoard() {
  if (dialogType.value == null || !canDeleteBoard.value) return
  const type = dialogType.value
  const name = boardLabel(type)
  try {
    await ElMessageBox.confirm(
      t('bbs.moderator.deleteBoardConfirm', { name }),
      t('bbs.moderator.deleteBoard'),
      { type: 'warning', confirmButtonText: t('bbs.moderator.deleteBoard'), cancelButtonText: t('bbs.moderator.cancel') }
    )
  } catch {
    return
  }
  deleting.value = true
  try {
    await bbsApi.deleteBoard(type)
    upsertModerator({
      type,
      isDeleted: true,
      subjectCount: 0,
      userId: null,
      displayName: null,
      defaultName: moderatorOf(type)?.defaultName
    })
    ElMessage.success(t('bbs.moderator.deleteBoardOk'))
    dialogVisible.value = false
    manageOrder.value = manageOrder.value.filter((x) => x !== type)
    if (props.activeKey === type) emit('select', 'all')
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.moderator.deleteBoardFailed')))
  } finally {
    deleting.value = false
  }
}

function upsertModerator(dto: BbsBoardModerator) {
  const idx = moderators.value.findIndex((x) => Number(x.type) === Number(dto.type))
  if (idx >= 0) moderators.value[idx] = dto
  else moderators.value.push(dto)
}

function openManageDialog() {
  manageOrder.value = collectMovableTypes().sort((a, b) => {
    const oa = boardSortOrder(a)
    const ob = boardSortOrder(b)
    if (oa !== ob) return oa - ob
    return a - b
  })
  newBoardName.value = ''
  manageVisible.value = true
}

function destroySortable() {
  sortableInst?.destroy()
  sortableInst = null
}

function initSortable() {
  destroySortable()
  const el = sortableListEl.value
  if (!el || manageOrder.value.length < 1) return
  sortableInst = Sortable.create(el, {
    animation: 160,
    handle: '.bbs-manage__grip',
    ghostClass: 'bbs-manage__ghost',
    onEnd: (evt: SortableEvent) => {
      const { oldIndex, newIndex } = evt
      if (oldIndex == null || newIndex == null || oldIndex === newIndex) return
      const next = [...manageOrder.value]
      const [moved] = next.splice(oldIndex, 1)
      next.splice(newIndex, 0, moved)
      manageOrder.value = next
      void persistReorder()
    }
  })
}

async function persistReorder() {
  if (!manageOrder.value.length) return
  reorderSaving.value = true
  try {
    await bbsApi.reorderBoards(manageOrder.value)
    await loadModerators()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.moderator.reorderFailed')))
    await loadModerators()
    manageOrder.value = collectMovableTypes().sort((a, b) => boardSortOrder(a) - boardSortOrder(b) || a - b)
  } finally {
    reorderSaving.value = false
  }
}

async function createBoard() {
  const name = newBoardName.value.trim()
  if (!name) return
  creating.value = true
  try {
    const dto = await bbsApi.createBoard({ displayName: name })
    upsertModerator(dto)
    manageOrder.value = [...manageOrder.value, Number(dto.type)]
    newBoardName.value = ''
    await loadModerators()
    await loadStats()
    ElMessage.success(t('bbs.moderator.createBoardOk'))
    await nextTick()
    initSortable()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.moderator.createBoardFailed')))
  } finally {
    creating.value = false
  }
}

function onManageClosed() {
  destroySortable()
  newBoardName.value = ''
}

watch(manageVisible, async (v) => {
  if (!v) {
    destroySortable()
    return
  }
  await nextTick()
  initSortable()
})

async function reload() {
  await Promise.all([loadStats(), loadModerators()])
}

onMounted(() => {
  void reload()
})

onBeforeUnmount(() => {
  destroySortable()
})

defineExpose({ reload })
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.bbs-cats {
  position: sticky;
  top: 12px;
  padding: 12px 10px;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
}

.bbs-cats__title-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 4px 4px 2px 10px;
}

.bbs-cats__title {
  font-size: 12px;
  font-weight: 600;
  color: $text-muted;
  letter-spacing: 0.02em;
}

.bbs-cats__settings-btn {
  padding: 2px !important;
  height: auto !important;
  min-height: 0 !important;
}

.bbs-cats__hint {
  padding: 0 10px 10px;
  font-size: 11px;
  color: $text-muted;
  opacity: 0.8;
}

.bbs-cats__divider {
  margin: 8px 10px;
  height: 0;
  border: none;
  border-top: 1px solid $border-card;
  opacity: 0.9;
}

.bbs-cat-block {
  margin: 0 0 4px;
  border-radius: 8px;

  &.is-active {
    background: rgba(0, 212, 255, 0.14);

    .bbs-cat {
      color: $cyan-primary;
      font-weight: 600;
      background: transparent;
    }

    .bbs-cat__stats {
      color: rgba(0, 212, 255, 0.85);
    }
  }
}

.bbs-cat {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  margin: 0;
  padding: 8px 10px 4px;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: $text-secondary;
  font-size: 13px;
  text-align: left;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease;

  &:hover {
    background: rgba(0, 212, 255, 0.06);
    color: $text-primary;
  }

  &.is-active {
    background: rgba(0, 212, 255, 0.14);
    color: $cyan-primary;
    font-weight: 600;
  }
}

.bbs-cat__icon {
  width: 22px;
  height: 22px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  opacity: 0.9;

  &--text {
    font-size: 12px;
    font-weight: 700;
    border-radius: 6px;
    background: rgba(255, 255, 255, 0.06);
  }
}

.bbs-cat__label {
  flex: 1 1 auto;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.bbs-cat__right {
  flex: 0 0 auto;
  margin-left: auto;
  display: inline-flex;
  align-items: center;
  gap: 2px;
  min-width: 0;
}

.bbs-cat__stats {
  flex: 0 0 auto;
  display: inline-flex;
  align-items: baseline;
  gap: 2px;
  margin-left: auto;
  font-size: 11px;
  font-variant-numeric: tabular-nums;
  color: $text-muted;
  white-space: nowrap;
  line-height: 1.2;
}

.bbs-cat.is-active .bbs-cat__stats {
  color: rgba(0, 212, 255, 0.85);
}

.bbs-cat__stats-sep {
  opacity: 0.55;
}

.bbs-cat__mod {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 0 10px 8px 40px;
  min-width: 0;
  font-size: 11px;
  color: $text-muted;
  line-height: 1.3;
}

.bbs-cat__mod-label {
  flex: 0 0 auto;
  opacity: 0.85;
}

.bbs-cat__mod-name {
  flex: 1 1 auto;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: $text-secondary;
}

.bbs-cat__mod-btn {
  flex: 0 0 auto;
  padding: 0 2px !important;
  height: auto !important;
  min-height: 0 !important;
}

.bbs-board-form {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.bbs-board-form__row {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.bbs-board-form__label {
  font-size: 13px;
  color: $text-secondary;
}

.bbs-board-form__footer {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.bbs-board-form__footer-spacer {
  flex: 1 1 auto;
}

.bbs-manage {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.bbs-manage__hint {
  margin: 0;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.45;
}

.bbs-manage__section-title {
  margin-top: 4px;
  font-size: 12px;
  font-weight: 600;
  color: $text-secondary;
}

.bbs-manage__list {
  list-style: none;
  margin: 0;
  padding: 0;
  border: 1px solid $border-card;
  border-radius: 8px;
  overflow: hidden;
}

.bbs-manage__item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 12px;
  background: $layer-1;
  border-bottom: 1px solid $border-card;
  font-size: 13px;
  color: $text-primary;

  &:last-child {
    border-bottom: none;
  }

  &.is-fixed {
    opacity: 0.72;
    background: rgba(255, 255, 255, 0.02);
  }
}

.bbs-manage__grip {
  flex: 0 0 auto;
  width: 18px;
  text-align: center;
  letter-spacing: -2px;
  color: $text-muted;
  cursor: grab;
  user-select: none;
  font-size: 12px;
  line-height: 1;

  &.is-disabled {
    cursor: default;
    opacity: 0.35;
  }

  &:active:not(.is-disabled) {
    cursor: grabbing;
  }
}

.bbs-manage__name {
  flex: 1 1 auto;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.bbs-manage__tag {
  flex: 0 0 auto;
  font-size: 11px;
  color: $text-muted;
  padding: 1px 6px;
  border-radius: 4px;
  background: rgba(255, 255, 255, 0.06);
}

.bbs-manage__empty {
  margin: 0;
  font-size: 12px;
  color: $text-muted;
}

.bbs-manage__create {
  display: flex;
  gap: 8px;
  align-items: center;
  margin-bottom: 4px;
}

.bbs-manage__ghost {
  opacity: 0.55;
  background: rgba(0, 212, 255, 0.12) !important;
}

@media (max-width: 900px) {
  .bbs-cats {
    position: static;
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
    padding: 10px;
  }

  .bbs-cats__title-row {
    width: 100%;
    padding-bottom: 6px;
  }

  .bbs-cat-block {
    width: auto;
    margin: 0;
  }

  .bbs-cat {
    width: auto;
    margin: 0;
  }

  .bbs-cat__mod {
    display: none;
  }
}
</style>
