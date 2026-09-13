<template>
  <section class="work-cal" aria-labelledby="work-cal-title">
    <header class="work-cal__head">
      <h3 id="work-cal-title" class="work-cal__title">{{ t('dashboard.calendar.title') }}</h3>
      <div class="work-cal__nav">
        <button type="button" class="work-cal__nav-btn" @click="shiftMonth(-1)">‹</button>
        <span class="work-cal__ym">{{ year }}-{{ String(month).padStart(2, '0') }}</span>
        <button type="button" class="work-cal__nav-btn" @click="shiftMonth(1)">›</button>
        <button type="button" class="work-cal__today" @click="goToday">{{ t('dashboard.calendar.today') }}</button>
      </div>
    </header>

    <div class="work-cal__week">
      <span v-for="w in weekLabels" :key="w">{{ w }}</span>
    </div>
    <div class="work-cal__grid">
      <button
        v-for="cell in cells"
        :key="cell.key"
        type="button"
        class="work-cal__cell"
        :class="{
          'is-out': !cell.inMonth,
          'is-today': cell.isToday,
          'is-selected': cell.ymd === selectedDate
        }"
        :disabled="!cell.inMonth"
        :title="cell.inMonth ? cellTitle(cell) : undefined"
        @click="openDay(cell.ymd)"
      >
        <span class="work-cal__num">{{ cell.day }}</span>
        <span v-if="cell.inMonth && (cell.rfq || cell.so || cell.task)" class="work-cal__dots">
          <i v-if="cell.rfq" class="dot dot--rfq" />
          <i v-if="cell.so" class="dot dot--so" />
          <i v-if="cell.task" class="dot dot--task" />
        </span>
      </button>
    </div>

    <p class="work-cal__legend">
      <span><i class="dot dot--rfq" />{{ t('dashboard.calendar.legendRfq') }}</span>
      <span><i class="dot dot--so" />{{ t('dashboard.calendar.legendSo') }}</span>
      <span><i class="dot dot--task" />{{ t('dashboard.calendar.legendTask') }}</span>
    </p>
  </section>

  <el-drawer v-model="dayOpen" :title="selectedDate" size="400px" append-to-body>
    <div v-loading="dayLoading" class="day-panel">
      <section>
        <h4>{{ t('dashboard.calendar.dayRfq') }} ({{ dayDto?.rfqs.length ?? 0 }})</h4>
        <ul v-if="dayDto?.rfqs.length">
          <li v-for="row in dayDto.rfqs" :key="row.id">
            <router-link :to="{ name: 'RFQDetail', params: { id: row.id } }">{{ row.code }}</router-link>
            <span v-if="row.customerName" class="muted">{{ row.customerName }}</span>
          </li>
        </ul>
        <p v-else class="muted">{{ t('dashboard.calendar.empty') }}</p>
      </section>
      <section>
        <h4>{{ t('dashboard.calendar.daySo') }} ({{ dayDto?.salesOrders.length ?? 0 }})</h4>
        <ul v-if="dayDto?.salesOrders.length">
          <li v-for="row in dayDto.salesOrders" :key="row.id">
            <router-link :to="{ name: 'SalesOrderDetail', params: { id: row.id } }">{{ row.code }}</router-link>
            <span v-if="row.customerName" class="muted">{{ row.customerName }}</span>
          </li>
        </ul>
        <p v-else class="muted">{{ t('dashboard.calendar.empty') }}</p>
      </section>
      <section>
        <h4>{{ t('dashboard.calendar.dayTask') }} ({{ dayDto?.tasks.length ?? 0 }})</h4>
        <ul v-if="dayDto?.tasks.length">
          <li v-for="row in dayDto.tasks" :key="row.id" class="task-row">
            <div>
              <strong>{{ row.title }}</strong>
              <span class="muted"> · {{ statusLabel(row.status) }} · P{{ row.priority }}</span>
              <div v-if="row.customerName" class="muted">{{ row.customerName }}</div>
            </div>
            <div class="task-row__ops">
              <el-button
                v-if="canWrite && row.status === 10"
                size="small"
                @click="() => void act(row.id, 'start')"
              >
                {{ t('dashboard.calendar.start') }}
              </el-button>
              <el-button
                v-if="canWrite && (row.status === 10 || row.status === 20)"
                size="small"
                type="primary"
                @click="() => void act(row.id, 'complete')"
              >
                {{ t('dashboard.calendar.complete') }}
              </el-button>
              <el-button
                v-if="canWrite && (row.status === 10 || row.status === 20)"
                size="small"
                @click="() => void act(row.id, 'cancel')"
              >
                {{ t('dashboard.calendar.cancel') }}
              </el-button>
            </div>
          </li>
        </ul>
        <p v-else class="muted">{{ t('dashboard.calendar.empty') }}</p>
      </section>
      <el-button v-if="canWrite" type="primary" @click="openCreate">
        {{ t('dashboard.calendar.newFollow') }}
      </el-button>
    </div>
  </el-drawer>

  <el-dialog v-model="createOpen" :title="t('dashboard.calendar.newFollow')" width="480px" append-to-body>
    <el-form label-position="top">
      <el-form-item :label="t('dashboard.calendar.customer')" required>
        <el-select
          v-model="form.objectId"
          filterable
          remote
          :remote-method="searchCustomers"
          :loading="customerLoading"
          :placeholder="t('dashboard.calendar.customerPlaceholder')"
        >
          <el-option
            v-for="c in customerOpts"
            :key="c.id"
            :label="c.label"
            :value="c.id"
          />
        </el-select>
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
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores'
import { customerApi } from '@/api/customer'
import { getApiErrorMessage } from '@/utils/apiError'
import {
  workCalendarApi,
  type WorkCalendarDay,
  type WorkCalendarDayCount,
  type WorkTaskAssigneeOption
} from '@/api/workCalendar'

const { t } = useI18n()
const authStore = useAuthStore()
const canWrite = computed(() => authStore.hasPermission('work-task.write'))

const now = new Date()
const year = ref(now.getFullYear())
const month = ref(now.getMonth() + 1)
const counts = ref<WorkCalendarDayCount[]>([])
const selectedDate = ref('')
const dayOpen = ref(false)
const dayLoading = ref(false)
const dayDto = ref<WorkCalendarDay | null>(null)
const createOpen = ref(false)
const saving = ref(false)
const customerLoading = ref(false)
const customerOpts = ref<{ id: string; label: string }[]>([])
const assignees = ref<WorkTaskAssigneeOption[]>([])

const form = reactive({
  objectId: '',
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

function formatYmd(d: Date) {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function pad(n: number) {
  return String(n).padStart(2, '0')
}

function cellTitle(cell: { ymd: string; rfq: boolean; so: boolean; task: boolean }) {
  const c = counts.value.find((x) => x.date === cell.ymd)
  const rfq = c?.rfqCount ?? 0
  const so = c?.soCount ?? 0
  const task = c?.taskCount ?? 0
  return `${t('dashboard.calendar.dayRfq')} ${rfq} / ${t('dashboard.calendar.daySo')} ${so} / ${t('dashboard.calendar.dayTask')} ${task}`
}

const cells = computed(() => {
  const first = new Date(year.value, month.value - 1, 1)
  const startWeekday = (first.getDay() + 6) % 7
  const daysInMonth = new Date(year.value, month.value, 0).getDate()
  const map = new Map(counts.value.map((x) => [x.date, x]))
  const list: {
    key: string
    day: number
    ymd: string
    inMonth: boolean
    isToday: boolean
    rfq: boolean
    so: boolean
    task: boolean
  }[] = []
  for (let i = 0; i < startWeekday; i++) {
    list.push({
      key: `p${i}`,
      day: 0,
      ymd: '',
      inMonth: false,
      isToday: false,
      rfq: false,
      so: false,
      task: false
    })
  }
  for (let d = 1; d <= daysInMonth; d++) {
    const ymd = `${year.value}-${pad(month.value)}-${pad(d)}`
    const c = map.get(ymd)
    list.push({
      key: ymd,
      day: d,
      ymd,
      inMonth: true,
      isToday: ymd === todayYmd.value,
      rfq: (c?.rfqCount ?? 0) > 0,
      so: (c?.soCount ?? 0) > 0,
      task: (c?.taskCount ?? 0) > 0
    })
  }
  return list
})

async function loadMonth() {
  try {
    const data = await workCalendarApi.month(year.value, month.value)
    counts.value = data?.days ?? []
  } catch (e) {
    counts.value = []
    ElMessage.error(getApiErrorMessage(e, t('dashboard.calendar.loadFailed')))
  }
}

async function openDay(ymd: string) {
  if (!ymd) return
  selectedDate.value = ymd
  dayOpen.value = true
  dayLoading.value = true
  try {
    dayDto.value = await workCalendarApi.day(ymd)
  } catch (e) {
    dayDto.value = { date: ymd, rfqs: [], salesOrders: [], tasks: [] }
    ElMessage.error(getApiErrorMessage(e, t('dashboard.calendar.loadFailed')))
  } finally {
    dayLoading.value = false
  }
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
  void openDay(formatYmd(d))
}

function statusLabel(status: number) {
  if (status === 10) return t('dashboard.calendar.statusPending')
  if (status === 20) return t('dashboard.calendar.statusDoing')
  if (status === 30) return t('dashboard.calendar.statusDone')
  if (status === 90) return t('dashboard.calendar.statusCancel')
  return String(status)
}

async function act(id: string, kind: 'start' | 'complete' | 'cancel') {
  try {
    if (kind === 'start') await workCalendarApi.start(id)
    if (kind === 'complete') await workCalendarApi.complete(id)
    if (kind === 'cancel') await workCalendarApi.cancel(id)
    ElMessage.success(t('dashboard.calendar.saved'))
    if (selectedDate.value) await openDay(selectedDate.value)
    await loadMonth()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('dashboard.calendar.saveFailed')))
  }
}

function openCreate() {
  form.objectId = ''
  form.title = ''
  form.content = ''
  form.startDate = selectedDate.value || todayYmd.value
  form.priority = 2
  form.assigneeUserId = authStore.user?.id ?? ''
  createOpen.value = true
  void loadAssignees()
  void searchCustomers('')
}

async function loadAssignees() {
  if (!canWrite.value) return
  try {
    assignees.value = (await workCalendarApi.assignees()) ?? []
  } catch {
    assignees.value = []
  }
}

async function searchCustomers(q: string) {
  customerLoading.value = true
  try {
    const res = await customerApi.searchCustomers({ searchTerm: q, page: 1, pageSize: 20 })
    customerOpts.value = (res.items ?? []).map((c) => ({
      id: c.id,
      label: c.customerName || c.customerCode || c.id
    }))
  } catch {
    customerOpts.value = []
  } finally {
    customerLoading.value = false
  }
}

async function submitCreate() {
  if (!form.objectId || !form.title.trim() || !form.startDate) {
    ElMessage.warning(t('dashboard.calendar.formRequired'))
    return
  }
  saving.value = true
  try {
    await workCalendarApi.create({
      objectId: form.objectId,
      title: form.title.trim(),
      content: form.content.trim() || undefined,
      startDate: form.startDate,
      priority: form.priority,
      assigneeUserId: form.assigneeUserId || undefined
    })
    ElMessage.success(t('dashboard.calendar.saved'))
    createOpen.value = false
    if (selectedDate.value) await openDay(selectedDate.value)
    await loadMonth()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('dashboard.calendar.saveFailed')))
  } finally {
    saving.value = false
  }
}

watch([year, month], () => {
  void loadMonth()
})

onMounted(() => {
  void loadMonth()
})
</script>

<style lang="scss" scoped>
.work-cal {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 12px;
  padding: 16px;
}
.work-cal__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 12px;
}
.work-cal__title {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
}
.work-cal__nav {
  display: flex;
  align-items: center;
  gap: 6px;
}
.work-cal__nav-btn,
.work-cal__today {
  border: 1px solid var(--el-border-color);
  background: transparent;
  border-radius: 6px;
  padding: 2px 8px;
  cursor: pointer;
}
.work-cal__ym {
  font-variant-numeric: tabular-nums;
  min-width: 72px;
  text-align: center;
}
.work-cal__week,
.work-cal__grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 4px;
}
.work-cal__week {
  margin-bottom: 4px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  text-align: center;
}
.work-cal__cell {
  min-height: 48px;
  border: none;
  background: transparent;
  border-radius: 8px;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 4px 0;
  &.is-out {
    visibility: hidden;
  }
  &.is-today .work-cal__num {
    width: 22px;
    height: 22px;
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
.work-cal__num {
  font-size: 13px;
}
.work-cal__dots {
  display: flex;
  gap: 3px;
  margin-top: 4px;
  min-height: 6px;
}
.dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  display: inline-block;
}
.dot--rfq {
  background: #3b82f6;
}
.dot--so {
  background: #22c55e;
}
.dot--task {
  background: #f59e0b;
}
.work-cal__legend {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  margin: 12px 0 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  span {
    display: inline-flex;
    align-items: center;
    gap: 4px;
  }
}
.day-panel {
  display: flex;
  flex-direction: column;
  gap: 16px;
  h4 {
    margin: 0 0 8px;
    font-size: 13px;
  }
  ul {
    margin: 0;
    padding-left: 18px;
  }
}
.muted {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}
.task-row {
  list-style: none;
  margin-left: -18px;
  padding: 8px 0;
  border-bottom: 1px solid var(--el-border-color-lighter);
}
.task-row__ops {
  margin-top: 6px;
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}
</style>
