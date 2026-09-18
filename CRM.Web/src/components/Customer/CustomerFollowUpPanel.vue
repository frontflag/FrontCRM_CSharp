<template>
  <div class="customer-follow" v-loading="loading">
    <div class="customer-follow__hint">{{ t('customerDetail.followUp.hint') }}</div>
    <div class="customer-follow__body">
      <aside class="customer-follow__left">
        <header class="customer-follow__cal-head">
          <button type="button" class="customer-follow__nav-btn" @click="shiftMonth(-1)">‹</button>
          <span class="customer-follow__ym">{{ year }}-{{ pad(month) }}</span>
          <button type="button" class="customer-follow__nav-btn" @click="shiftMonth(1)">›</button>
          <button type="button" class="customer-follow__today" @click="goToday">
            {{ t('dashboard.calendar.today') }}
          </button>
        </header>
        <div class="customer-follow__week">
          <span v-for="w in weekLabels" :key="w">{{ w }}</span>
        </div>
        <div class="customer-follow__grid">
          <button
            v-for="cell in cells"
            :key="cell.key"
            type="button"
            class="customer-follow__cell"
            :class="{
              'is-out': !cell.inMonth,
              'is-today': cell.isToday,
              'is-selected': cell.ymd === startDate
            }"
            :disabled="!cell.inMonth"
            :title="cell.inMonth ? cellTitle(cell) : undefined"
            @click="selectDay(cell.ymd)"
          >
            <span class="customer-follow__num">{{ cell.day || '' }}</span>
            <span v-if="cell.inMonth && cell.hasTask" class="customer-follow__dots">
              <i class="dot dot--task" />
            </span>
          </button>
        </div>

        <div class="customer-follow__toolbar">
          <el-button size="small" :type="startDate ? 'default' : 'primary'" @click="clearDate">
            {{ t('customerDetail.followUp.all') }}
          </el-button>
          <el-switch
            v-model="includeCancelled"
            :active-text="t('customerDetail.followUp.includeCancelled')"
          />
          <el-button v-if="canCreate" type="primary" size="small" @click="openCreate">
            {{ t('customerDetail.followUp.newFollow') }}
          </el-button>
        </div>

        <ul v-if="items.length" class="customer-follow__list">
          <li
            v-for="row in items"
            :key="row.id"
            class="customer-follow__row"
            :class="{ 'is-active': row.id === selectedId }"
            @click="selectRow(row)"
          >
            <div class="customer-follow__row-main">
              <strong>{{ row.title }}</strong>
              <el-tag size="small" :type="statusType(row.status)">{{ statusLabel(row.status) }}</el-tag>
            </div>
            <div class="customer-follow__row-meta">
              {{ row.startDate }} · {{ row.assigneeUserName || '—' }} · P{{ row.priority }}
            </div>
          </li>
        </ul>
        <p v-else class="customer-follow__empty">{{ t('customerDetail.followUp.empty') }}</p>
        <el-pagination
          v-if="totalCount > 0"
          class="customer-follow__pager"
          layout="prev, pager, next, total"
          :page-size="pageSize"
          :current-page="page"
          :total="totalCount"
          :pager-count="5"
          small
          @current-change="onPageChange"
        />
      </aside>

      <section class="customer-follow__right">
        <template v-if="selected">
          <el-form label-position="top">
            <el-form-item :label="t('customerDetail.followUp.title')">
              <el-input
                v-model="draftTitle"
                maxlength="200"
                show-word-limit
                :disabled="!canEditSelected"
              />
            </el-form-item>
            <div class="customer-follow__meta">
              <span>{{ statusLabel(selected.status) }}</span>
              <span>{{ selected.startDate }}</span>
              <span>{{ selected.assigneeUserName || '—' }}</span>
              <span>P{{ selected.priority }}</span>
            </div>
            <el-form-item :label="t('customerDetail.followUp.content')">
              <el-input
                v-model="draftContent"
                type="textarea"
                :rows="8"
                maxlength="2000"
                show-word-limit
                :disabled="!canEditSelected"
              />
            </el-form-item>
          </el-form>
          <p v-if="selected.contactHistoryId" class="customer-follow__history">
            {{ t('customerDetail.followUp.writtenToHistory') }}
          </p>
          <div class="customer-follow__ops">
            <el-button
              v-if="canEditSelected && dirty"
              type="primary"
              :loading="saving"
              @click="() => void saveDraft()"
            >
              {{ t('customerDetail.followUp.save') }}
            </el-button>
            <el-button
              v-if="selected.canWrite && selected.status === 10"
              :loading="acting"
              @click="() => void act('start')"
            >
              {{ t('dashboard.calendar.start') }}
            </el-button>
            <el-button
              v-if="selected.canWrite && (selected.status === 10 || selected.status === 20)"
              type="primary"
              :loading="acting"
              @click="() => void act('complete')"
            >
              {{ t('dashboard.calendar.complete') }}
            </el-button>
            <el-button
              v-if="selected.canWrite && (selected.status === 10 || selected.status === 20)"
              :loading="acting"
              @click="() => void act('cancel')"
            >
              {{ t('dashboard.calendar.cancel') }}
            </el-button>
          </div>
        </template>
        <p v-else class="customer-follow__empty">{{ t('customerDetail.followUp.noSelection') }}</p>
      </section>
    </div>

    <el-dialog v-model="createOpen" :title="t('customerDetail.followUp.newFollow')" width="480px" append-to-body>
      <el-form label-position="top">
        <el-form-item :label="t('dashboard.calendar.customer')">
          <el-input :model-value="customerName || t('customerDetail.followUp.customerLocked')" disabled />
        </el-form-item>
        <el-form-item :label="t('dashboard.calendar.taskTitle')" required>
          <el-input v-model="form.title" maxlength="200" show-word-limit />
        </el-form-item>
        <el-form-item :label="t('dashboard.calendar.content')">
          <el-input v-model="form.content" type="textarea" :rows="3" maxlength="2000" />
        </el-form-item>
        <el-form-item :label="t('dashboard.calendar.startDate')" required>
          <el-date-picker v-model="form.startDate" type="date" value-format="YYYY-MM-DD" />
        </el-form-item>
        <el-form-item :label="t('dashboard.calendar.priority')">
          <el-select v-model="form.priority">
            <el-option :label="t('dashboard.calendar.p0')" :value="0" />
            <el-option :label="t('dashboard.calendar.p1')" :value="1" />
            <el-option :label="t('dashboard.calendar.p2')" :value="2" />
            <el-option :label="t('dashboard.calendar.p3')" :value="3" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('dashboard.calendar.assignee')">
          <el-select v-model="form.assigneeUserId" filterable>
            <el-option
              v-for="u in assignees"
              :key="u.id"
              :label="u.realName ? `${u.userName} / ${u.realName}` : u.userName"
              :value="u.id"
            />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createOpen = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="saving" @click="() => void submitCreate()">
          {{ t('dashboard.calendar.save') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter, type LocationQuery } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores'
import { getApiErrorMessage } from '@/utils/apiError'
import {
  workCalendarApi,
  type CustomerWorkTaskItem,
  type WorkTaskAssigneeOption,
  type WorkTaskDetail
} from '@/api/workCalendar'

const props = defineProps<{
  customerId: string
  customerName?: string
  active: boolean
}>()

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const canCreate = computed(() => authStore.hasPermission('work-task.write'))

const pageSize = 50
const now = new Date()
const queryFollowDate = parseYmd(String(route.query.followDate ?? ''))
const year = ref(queryFollowDate?.getFullYear() ?? now.getFullYear())
const month = ref((queryFollowDate?.getMonth() ?? now.getMonth()) + 1)
const startDate = ref(queryFollowDate ? formatYmd(queryFollowDate) : '')
const includeCancelled = ref(String(route.query.followIncludeCancelled ?? '') === '1')
const counts = ref<{ date: string; taskCount: number }[]>([])
const items = ref<CustomerWorkTaskItem[]>([])
const extra = ref<CustomerWorkTaskItem | null>(null)
const selectedId = ref(String(route.query.taskId ?? '').trim())
const totalCount = ref(0)
const page = ref(1)
const loading = ref(false)
const saving = ref(false)
const acting = ref(false)
const createOpen = ref(false)
const assignees = ref<WorkTaskAssigneeOption[]>([])
const draftTitle = ref('')
const draftContent = ref('')
const loaded = ref(false)
const deepLinkDone = ref(false)
const skipFilterReload = ref(false)

const form = reactive({
  title: '',
  content: '',
  startDate: '',
  priority: 2,
  assigneeUserId: authStore.user?.id ?? ''
})

const weekLabels = computed(() => [
  t('dashboard.calendar.mon'),
  t('dashboard.calendar.tue'),
  t('dashboard.calendar.wed'),
  t('dashboard.calendar.thu'),
  t('dashboard.calendar.fri'),
  t('dashboard.calendar.sat'),
  t('dashboard.calendar.sun')
])

const todayYmd = computed(() => formatYmd(new Date()))

const selected = computed(() => {
  const id = selectedId.value
  if (!id) return null
  return items.value.find((x) => x.id === id) ?? (extra.value?.id === id ? extra.value : null)
})

const canEditSelected = computed(() => !!selected.value?.canWrite)

const dirty = computed(() => {
  const row = selected.value
  if (!row) return false
  return draftTitle.value !== (row.title ?? '') || draftContent.value !== (row.content ?? '')
})

const cells = computed(() => {
  const first = new Date(year.value, month.value - 1, 1)
  const startWeekday = (first.getDay() + 6) % 7
  const daysInMonth = new Date(year.value, month.value, 0).getDate()
  const map = new Map(counts.value.map((x) => [x.date, x.taskCount]))
  const list: { key: string; day: number; ymd: string; inMonth: boolean; isToday: boolean; hasTask: boolean }[] = []
  for (let i = 0; i < startWeekday; i++) {
    list.push({ key: `p${i}`, day: 0, ymd: '', inMonth: false, isToday: false, hasTask: false })
  }
  for (let d = 1; d <= daysInMonth; d++) {
    const ymd = `${year.value}-${pad(month.value)}-${pad(d)}`
    list.push({
      key: ymd,
      day: d,
      ymd,
      inMonth: true,
      isToday: ymd === todayYmd.value,
      hasTask: (map.get(ymd) ?? 0) > 0
    })
  }
  return list
})

function pad(n: number) {
  return String(n).padStart(2, '0')
}

function formatYmd(d: Date) {
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

function parseYmd(raw: string): Date | null {
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(raw.trim())
  if (!m) return null
  const d = new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]))
  return Number.isNaN(d.getTime()) ? null : d
}

function cellTitle(cell: { ymd: string; hasTask: boolean }) {
  const n = counts.value.find((x) => x.date === cell.ymd)?.taskCount ?? 0
  return `${t('dashboard.calendar.dayTask')} ${n}`
}

function statusLabel(status: number) {
  if (status === 10) return t('dashboard.calendar.statusPending')
  if (status === 20) return t('dashboard.calendar.statusDoing')
  if (status === 30) return t('dashboard.calendar.statusDone')
  if (status === 90) return t('dashboard.calendar.statusCancel')
  return String(status)
}

function statusType(status: number): 'warning' | 'primary' | 'success' | 'info' {
  if (status === 20) return 'primary'
  if (status === 30) return 'success'
  if (status === 90) return 'info'
  return 'warning'
}

function bindDraft(row: CustomerWorkTaskItem | null) {
  draftTitle.value = row?.title ?? ''
  draftContent.value = row?.content ?? ''
}

function applyRow(row: CustomerWorkTaskItem) {
  extra.value = items.value.some((x) => x.id === row.id) ? null : row
  selectedId.value = row.id
  bindDraft(row)
  syncQuery()
}

function selectRow(row: CustomerWorkTaskItem) {
  applyRow(row)
}

function syncQuery() {
  if (!props.active) return
  const next: LocationQuery = { ...route.query, tab: 'followUp' }
  if (selectedId.value) next.taskId = selectedId.value
  else delete next.taskId
  if (startDate.value) next.followDate = startDate.value
  else delete next.followDate
  if (includeCancelled.value) next.followIncludeCancelled = '1'
  else delete next.followIncludeCancelled
  const same =
    String(route.query.tab ?? '') === 'followUp' &&
    String(route.query.taskId ?? '') === String(next.taskId ?? '') &&
    String(route.query.followDate ?? '') === String(next.followDate ?? '') &&
    String(route.query.followIncludeCancelled ?? '') === String(next.followIncludeCancelled ?? '')
  if (same) return
  void router.replace({ query: next })
}

async function loadMonth() {
  try {
    const data = await workCalendarApi.customerMonth(
      props.customerId,
      year.value,
      month.value,
      includeCancelled.value
    )
    counts.value = data?.days ?? []
  } catch (e) {
    counts.value = []
    ElMessage.error(getApiErrorMessage(e, t('customerDetail.followUp.loadFailed')))
  }
}

async function loadList() {
  loading.value = true
  try {
    const data = await workCalendarApi.customerList(props.customerId, {
      page: page.value,
      pageSize,
      startDate: startDate.value || undefined,
      includeCancelled: includeCancelled.value
    })
    items.value = data?.items ?? []
    totalCount.value = data?.totalCount ?? 0
    page.value = data?.page ?? page.value
    if (!deepLinkDone.value) {
      deepLinkDone.value = true
      await resolveDeepLink()
    } else if (selectedId.value && !items.value.some((x) => x.id === selectedId.value) && extra.value?.id !== selectedId.value) {
      extra.value = null
      selectedId.value = items.value[0]?.id ?? ''
      bindDraft(selected.value)
      syncQuery()
    } else if (!selectedId.value && items.value[0]) {
      applyRow(items.value[0])
    } else {
      bindDraft(selected.value)
      syncQuery()
    }
  } catch (e) {
    items.value = []
    totalCount.value = 0
    ElMessage.error(getApiErrorMessage(e, t('customerDetail.followUp.loadFailed')))
  } finally {
    loading.value = false
  }
}

async function resolveDeepLink() {
  const id = String(route.query.taskId ?? '').trim()
  if (!id) {
    if (items.value[0]) applyRow(items.value[0])
    else {
      selectedId.value = ''
      bindDraft(null)
      syncQuery()
    }
    return
  }
  const inList = items.value.find((x) => x.id === id)
  if (inList) {
    applyRow(inList)
    return
  }
  try {
    const one = await workCalendarApi.customerGet(props.customerId, id)
    let reload = false
    if (one.status === 90 && !includeCancelled.value) {
      skipFilterReload.value = true
      includeCancelled.value = true
      reload = true
    }
    if (startDate.value && one.startDate !== startDate.value) {
      startDate.value = ''
      reload = true
    }
    if (reload) {
      await nextTick()
      await Promise.all([loadMonth(), reloadListKeepSelection(one.id)])
    }
    skipFilterReload.value = false
    const after = items.value.find((x) => x.id === one.id) ?? { ...one, canWrite: one.canWrite }
    applyRow(after)
  } catch {
    if (items.value[0]) applyRow(items.value[0])
    else {
      selectedId.value = ''
      bindDraft(null)
      syncQuery()
    }
  }
}

async function reloadListKeepSelection(preferId: string) {
  const data = await workCalendarApi.customerList(props.customerId, {
    page: 1,
    pageSize,
    startDate: startDate.value || undefined,
    includeCancelled: includeCancelled.value
  })
  items.value = data?.items ?? []
  totalCount.value = data?.totalCount ?? 0
  page.value = data?.page ?? 1
  selectedId.value = preferId
}

function selectDay(ymd: string) {
  if (!ymd) return
  startDate.value = ymd
  page.value = 1
  void loadList()
}

function clearDate() {
  startDate.value = ''
  page.value = 1
  void loadList()
}

function onPageChange(next: number) {
  page.value = next
  void loadList()
}

function shiftMonth(delta: number) {
  const d = new Date(year.value, month.value - 1 + delta, 1)
  year.value = d.getFullYear()
  month.value = d.getMonth() + 1
}

function goToday() {
  const d = new Date()
  year.value = d.getFullYear()
  month.value = d.getMonth() + 1
}

function openCreate() {
  form.title = ''
  form.content = ''
  form.startDate = startDate.value || formatYmd(new Date())
  form.priority = 2
  form.assigneeUserId = authStore.user?.id ?? ''
  createOpen.value = true
  void loadAssignees()
}

async function loadAssignees() {
  if (!canCreate.value) return
  try {
    assignees.value = (await workCalendarApi.assignees()) ?? []
  } catch {
    assignees.value = []
  }
}

async function submitCreate() {
  if (!form.title.trim() || !form.startDate) {
    ElMessage.warning(t('customerDetail.followUp.formRequired'))
    return
  }
  saving.value = true
  try {
    const created = await workCalendarApi.create({
      objectId: props.customerId,
      title: form.title.trim(),
      content: form.content.trim() || undefined,
      startDate: form.startDate,
      priority: form.priority,
      assigneeUserId: form.assigneeUserId || undefined
    })
    ElMessage.success(t('customerDetail.followUp.saved'))
    createOpen.value = false
    startDate.value = created.startDate
    const d = parseYmd(created.startDate)
    if (d) {
      year.value = d.getFullYear()
      month.value = d.getMonth() + 1
    }
    page.value = 1
    await Promise.all([loadMonth(), loadList()])
    const row = items.value.find((x) => x.id === created.id)
    if (row) applyRow(row)
    else applyRow({ ...created, canWrite: true })
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('customerDetail.followUp.saveFailed')))
  } finally {
    saving.value = false
  }
}

async function saveDraft() {
  const row = selected.value
  if (!row || !canEditSelected.value) return
  if (!draftTitle.value.trim()) {
    ElMessage.warning(t('customerDetail.followUp.formRequired'))
    return
  }
  saving.value = true
  try {
    const updated = await workCalendarApi.patch(row.id, {
      title: draftTitle.value.trim(),
      content: draftContent.value
    })
    mergeUpdated(updated)
    ElMessage.success(t('customerDetail.followUp.saved'))
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('customerDetail.followUp.saveFailed')))
  } finally {
    saving.value = false
  }
}

async function act(kind: 'start' | 'complete' | 'cancel') {
  const row = selected.value
  if (!row?.canWrite) return
  acting.value = true
  try {
    const updated =
      kind === 'start'
        ? await workCalendarApi.start(row.id)
        : kind === 'complete'
          ? await workCalendarApi.complete(row.id)
          : await workCalendarApi.cancel(row.id)
    mergeUpdated(updated)
    if (kind === 'cancel' && !includeCancelled.value) {
      extra.value = null
      await Promise.all([loadMonth(), loadList()])
    } else if (kind === 'complete' || kind === 'start') {
      await loadMonth()
    }
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('customerDetail.followUp.saveFailed')))
  } finally {
    acting.value = false
  }
}

function mergeUpdated(updated: WorkTaskDetail) {
  const next: CustomerWorkTaskItem = {
    ...(selected.value ?? ({} as CustomerWorkTaskItem)),
    ...updated,
    canWrite: selected.value?.canWrite ?? true
  }
  const i = items.value.findIndex((x) => x.id === next.id)
  if (i >= 0) items.value[i] = { ...items.value[i], ...next }
  if (extra.value?.id === next.id) extra.value = next
  selectedId.value = next.id
  bindDraft(next)
  syncQuery()
}

async function boot() {
  if (!props.customerId) return
  loaded.value = true
  await Promise.all([loadMonth(), loadList()])
}

watch(
  () => props.active,
  (on) => {
    if (!on || loaded.value) return
    void boot()
  },
  { immediate: true }
)

watch(
  () => props.customerId,
  () => {
    if (!props.active) {
      loaded.value = false
      deepLinkDone.value = false
      return
    }
    deepLinkDone.value = false
    page.value = 1
    void boot()
  }
)

watch(includeCancelled, () => {
  if (!loaded.value || skipFilterReload.value) return
  page.value = 1
  void Promise.all([loadMonth(), loadList()])
})

watch([year, month], () => {
  if (!loaded.value) return
  void loadMonth()
})
</script>

<style lang="scss" scoped>
.customer-follow {
  display: flex;
  flex-direction: column;
  min-height: 420px;
  height: calc(100vh - 220px);
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 12px;
  overflow: hidden;
}

.customer-follow__hint {
  padding: 8px 16px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.customer-follow__body {
  display: flex;
  flex: 1;
  min-height: 0;
}

.customer-follow__left {
  flex: 0 0 340px;
  display: flex;
  flex-direction: column;
  min-height: 0;
  padding: 10px 12px;
  border-right: 1px solid var(--el-border-color-lighter);
  overflow: auto;
}

.customer-follow__right {
  flex: 1;
  min-width: 0;
  overflow: auto;
  padding: 16px 20px;
}

.customer-follow__cal-head {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 8px;
}

.customer-follow__nav-btn,
.customer-follow__today {
  border: 1px solid var(--el-border-color);
  background: transparent;
  border-radius: 6px;
  padding: 2px 8px;
  cursor: pointer;
}

.customer-follow__ym {
  font-variant-numeric: tabular-nums;
  min-width: 72px;
  text-align: center;
}

.customer-follow__week,
.customer-follow__grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 2px;
}

.customer-follow__week {
  margin-bottom: 4px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  text-align: center;
}

.customer-follow__cell {
  min-height: 36px;
  border: none;
  background: transparent;
  border-radius: 8px;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 2px 0;
  &.is-out {
    visibility: hidden;
  }
  &.is-today .customer-follow__num {
    width: 20px;
    height: 20px;
    border-radius: 50%;
    background: var(--el-color-primary);
    color: #fff;
    display: inline-flex;
    align-items: center;
    justify-content: center;
  }
  &.is-selected {
    background: var(--el-color-primary-light-9);
  }
}

.customer-follow__num {
  font-size: 12px;
}

.customer-follow__dots {
  min-height: 6px;
  margin-top: 2px;
}

.dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  display: inline-block;
}

.dot--task {
  background: #f59e0b;
}

.customer-follow__toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin: 10px 0 8px;
}

.customer-follow__list {
  list-style: none;
  margin: 0;
  padding: 0;
  flex: 1;
}

.customer-follow__row {
  padding: 8px 10px;
  border-radius: 8px;
  cursor: pointer;
  & + & {
    margin-top: 2px;
  }
  &:hover:not(.is-active) {
    background: var(--el-fill-color-light);
  }
  &.is-active {
    background: var(--el-color-primary-light-9);
  }
}

.customer-follow__row-main {
  display: flex;
  align-items: center;
  gap: 6px;
  strong {
    font-size: 13px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}

.customer-follow__row-meta {
  margin-top: 2px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.customer-follow__pager {
  margin-top: 8px;
  justify-content: flex-end;
}

.customer-follow__empty {
  margin: 24px 0;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.customer-follow__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  margin: 0 0 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.customer-follow__history {
  margin: 0 0 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.customer-follow__ops {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

@media (max-width: 900px) {
  .customer-follow__body {
    flex-direction: column;
  }
  .customer-follow__left {
    flex: none;
    border-right: none;
    border-bottom: 1px solid var(--el-border-color-lighter);
  }
}
</style>
