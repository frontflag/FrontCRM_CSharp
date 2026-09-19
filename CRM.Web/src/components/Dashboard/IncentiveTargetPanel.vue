<template>
  <section v-if="canRead" class="incentive" aria-labelledby="dashboard-incentive-title">
    <header class="incentive__head">
      <div class="incentive__title-row">
        <h3 id="dashboard-incentive-title" class="incentive__title">
          {{ titleText }}
        </h3>
        <span class="incentive__meta">{{ roleText }}</span>
      </div>
      <div class="incentive__actions">
        <router-link v-if="detailTo" class="incentive__link" :to="detailTo">
          {{ t('dashboard.incentive.detail') }}
        </router-link>
        <button v-if="canWrite" type="button" class="incentive__link" @click="openDialog">
          {{ t('dashboard.incentive.settings') }}
        </button>
      </div>
    </header>

    <div v-loading="loading" class="incentive__body">
      <div class="incentive__hero">
        <div class="incentive__hero-left">
          <p class="incentive__amount">
            <strong>{{ formatUsd(mine?.term.actualUsd) }}</strong>
            <span class="incentive__label">{{ t('dashboard.incentive.estimated') }}</span>
          </p>
        </div>
        <div class="incentive__hero-right">
          <div class="incentive__progress-head">
            <span class="incentive__plan">
              {{ t('dashboard.incentive.termPlan') }}
              {{ mine?.term.hasTarget ? formatUsd(mine.term.targetUsd) : t('dashboard.incentive.unset') }}
            </span>
            <span class="incentive__rate" :class="{ 'is-over': overTerm }">{{ rateText }}</span>
          </div>
          <div class="incentive__track" :class="{ 'is-empty': !mine?.term.hasTarget }">
            <div class="incentive__fill" :style="{ width: barWidth }" />
          </div>
        </div>
      </div>

      <p class="incentive__foot">
        <span>
          {{ t('dashboard.incentive.yearPlan') }}
          {{ mine?.year.hasTarget ? formatUsd(mine.year.targetUsd) : t('dashboard.incentive.unset') }}
        </span>
        <span class="incentive__dot">·</span>
        <span>
          {{ t('dashboard.incentive.yearActual') }}
          {{ formatUsd(mine?.year.actualUsd) }}
        </span>
        <template v-if="yearRateText">
          <span class="incentive__dot">·</span>
          <span>{{ yearRateText }}</span>
        </template>
      </p>
    </div>

    <el-dialog
      v-model="dialogVisible"
      :title="t('dashboard.incentive.dialogTitle')"
      width="420px"
      destroy-on-close
      @closed="resetForm"
    >
      <p class="incentive__hint">{{ t('dashboard.incentive.hint') }}</p>
      <el-form label-position="top">
        <el-form-item>
          <template #label>{{ termFieldLabel }}</template>
          <el-input-number
            v-model="form.term"
            :min="0"
            :max="99999999.99"
            :precision="2"
            :step="100"
            :controls="false"
            class="incentive__input"
          />
        </el-form-item>
        <el-form-item>
          <template #label>{{ yearFieldLabel }}</template>
          <el-input-number
            v-model="form.year"
            :min="0"
            :max="99999999.99"
            :precision="2"
            :step="100"
            :controls="false"
            class="incentive__input"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="saving" @click="save">{{ t('dashboard.incentive.save') }}</el-button>
      </template>
    </el-dialog>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores'
import { incentiveTargetsApi, type IncentiveTargetMine } from '@/api/incentive/targets'
import { getApiErrorMessage } from '@/utils/apiError'
import { resolveIncentiveTermPeriod, resolveIncentiveYearPeriod } from '@/utils/incentiveTargetPeriod'

const { t } = useI18n()
const authStore = useAuthStore()

const canRead = computed(() => {
  if (!authStore.hasPermission('incentive-target.read')) return false
  if (authStore.hasSysAdminRole()) return true
  const identity = authStore.user?.identityType ?? 0
  return identity === 1 || identity === 2 || identity === 3 || authStore.user?.belongsToPurchaseDept === true
})
const canWrite = computed(() => authStore.hasPermission('incentive-target.write'))

const loading = ref(false)
const saving = ref(false)
const dialogVisible = ref(false)
const mine = ref<IncentiveTargetMine | null>(null)

const form = reactive<{ term: number | undefined; year: number | undefined }>({
  term: undefined,
  year: undefined
})

const isPurchase = computed(() => mine.value?.roleType === 2)
const roleText = computed(() =>
  isPurchase.value ? t('dashboard.incentive.purchaseRole') : t('dashboard.incentive.salesRole')
)
const titleText = computed(() => {
  const span = resolveIncentiveTermPeriod(mine.value)
  return span ? t('dashboard.incentive.titleWithSpan', { span }) : t('dashboard.incentive.title')
})

const detailTo = computed(() => {
  if (isPurchase.value) {
    return authStore.hasPermission('commission-estimated-purchase.read')
      ? { name: 'CommissionEstimatedPurchase' }
      : null
  }
  return authStore.hasPermission('commission-estimated-sales.read')
    ? { name: 'CommissionEstimatedSales' }
    : null
})

const termFieldLabel = computed(() =>
  t('dashboard.incentive.termField', { period: resolveIncentiveTermPeriod(mine.value) })
)
const yearFieldLabel = computed(() =>
  t('dashboard.incentive.yearField', { period: resolveIncentiveYearPeriod(mine.value) })
)

const dash = '—'
const overTerm = computed(() => (mine.value?.term.completionPct ?? 0) > 100)

function formatUsd(n: number | null | undefined) {
  if (n == null || !Number.isFinite(n)) return dash
  return `$\u00a0${n.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function formatRate(pct: number | null | undefined) {
  if (pct == null || !Number.isFinite(pct)) return ''
  if (pct > 999.9) return t('dashboard.incentive.rateOver')
  return t('dashboard.incentive.rate', { pct: pct.toFixed(1) })
}

const rateText = computed(() => {
  if (!mine.value?.term.hasTarget) return t('dashboard.incentive.unset')
  return formatRate(mine.value.term.completionPct) || dash
})

const yearRateText = computed(() => {
  if (!mine.value?.year.hasTarget) return ''
  return formatRate(mine.value.year.completionPct)
})

const barWidth = computed(() => {
  const pct = mine.value?.term.hasTarget ? mine.value.term.completionPct : null
  if (pct == null || !Number.isFinite(pct) || pct <= 0) return '0%'
  return `${Math.min(100, pct)}%`
})

function openDialog() {
  form.term = mine.value?.term.hasTarget ? mine.value.term.targetUsd ?? undefined : undefined
  form.year = mine.value?.year.hasTarget ? mine.value.year.targetUsd ?? undefined : undefined
  dialogVisible.value = true
}

function resetForm() {
  form.term = undefined
  form.year = undefined
}

async function load() {
  if (!canRead.value) return
  loading.value = true
  try {
    mine.value = await incentiveTargetsApi.getMine()
  } catch {
    mine.value = null
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  try {
    mine.value = await incentiveTargetsApi.putMine({
      termTargetUsd: form.term && form.term > 0 ? form.term : null,
      yearTargetUsd: form.year && form.year > 0 ? form.year : null
    })
    dialogVisible.value = false
    ElMessage.success(t('dashboard.incentive.saved'))
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('dashboard.incentive.saveFailed')))
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  void load()
})
</script>

<style lang="scss" scoped>
.incentive {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 12px;
  padding: 16px 20px 14px;
}

.incentive__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 14px;
}

.incentive__title-row {
  display: flex;
  align-items: baseline;
  gap: 10px;
  min-width: 0;
}

.incentive__title {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
}

.incentive__meta {
  flex-shrink: 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.incentive__actions {
  display: flex;
  align-items: center;
  gap: 14px;
  flex-shrink: 0;
}

.incentive__link {
  padding: 0;
  border: 0;
  background: none;
  font-size: 12px;
  color: var(--el-color-primary);
  text-decoration: none;
  cursor: pointer;
  &:hover {
    text-decoration: underline;
  }
}

.incentive__hero {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(180px, 1.2fr);
  gap: 20px 28px;
  align-items: end;
  @media (max-width: 720px) {
    grid-template-columns: 1fr;
  }
}

.incentive__label {
  display: block;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.incentive__amount {
  display: flex;
  align-items: baseline;
  flex-wrap: wrap;
  gap: 8px;
  margin: 0;
  line-height: 1.25;
}

.incentive__amount .incentive__label {
  display: inline;
}

.incentive__amount strong {
  font-size: 22px;
  font-weight: 700;
  letter-spacing: -0.02em;
}

.incentive__progress-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 8px;
}

.incentive__plan {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.incentive__rate {
  font-size: 12px;
  color: var(--el-color-success);
  &.is-over {
    color: var(--el-color-primary);
  }
}

.incentive__track {
  height: 8px;
  border-radius: 999px;
  background: var(--el-fill-color);
  overflow: hidden;
  &.is-empty {
    opacity: 0.45;
  }
}

.incentive__fill {
  height: 100%;
  border-radius: inherit;
  background: var(--el-color-success);
  transition: width 0.2s ease;
}

.incentive__foot {
  margin: 14px 0 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  line-height: 1.5;
}

.incentive__dot {
  margin: 0 6px;
}

.incentive__hint {
  margin: 0 0 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  line-height: 1.5;
}

.incentive__input {
  width: 100%;
}
</style>
