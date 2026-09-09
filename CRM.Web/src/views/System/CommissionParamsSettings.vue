<template>
  <div class="commission-settings">
    <div class="section-head">
      <div class="section-head__left">
        <div class="section-title"><span class="title-bar"></span>{{ t('commissionParams.settingsNav') }}</div>
        <p class="section-hint">{{ t('commissionParams.settingsHint') }}</p>
      </div>
      <div class="section-head__actions">
        <el-button type="primary" :loading="saving" @click="saveActive">{{ t('commissionParams.save') }}</el-button>
      </div>
    </div>

    <div v-loading="loading" class="settings-card">
      <el-form label-width="200px">
        <el-form-item :label="t('commissionParams.salesVersion')">
          <div class="version-row">
            <el-select v-model="salesVersionId" class="version-select" :filterable="false">
              <el-option
                v-for="v in salesVersions"
                :key="v.id"
                :label="formatCommissionVersionLabel(v, t('commissionParams.activeTag'))"
                :value="v.id"
              />
            </el-select>
            <el-button @click="openCreate(COMMISSION_ROLE_SALES)">{{ t('commissionParams.createVersion') }}</el-button>
          </div>
        </el-form-item>
        <el-form-item :label="t('commissionParams.purchaseVersion')">
          <div class="version-row">
            <el-select v-model="purchaseVersionId" class="version-select" :filterable="false">
              <el-option
                v-for="v in purchaseVersions"
                :key="v.id"
                :label="formatCommissionVersionLabel(v, t('commissionParams.activeTag'))"
                :value="v.id"
              />
            </el-select>
            <el-button @click="openCreate(COMMISSION_ROLE_PURCHASE)">{{ t('commissionParams.createVersion') }}</el-button>
          </div>
        </el-form-item>
      </el-form>
    </div>

    <el-dialog
      v-model="createVisible"
      :title="createTitle"
      width="480px"
      destroy-on-close
    >
      <el-form label-width="96px">
        <el-form-item :label="t('commissionParams.copyFrom')">
          <el-select v-model="createForm.copyFromVersionId" class="version-select">
            <el-option
              v-for="v in createSourceVersions"
              :key="v.id"
              :label="formatCommissionVersionLabel(v, t('commissionParams.activeTag'))"
              :value="v.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('commissionParams.versionRemark')">
          <el-input
            v-model="createForm.remark"
            type="textarea"
            :rows="2"
            maxlength="200"
            show-word-limit
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="creating" @click="submitCreate">{{ t('common.confirm') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import {
  COMMISSION_ROLE_PURCHASE,
  COMMISSION_ROLE_SALES,
  commissionRatesApi,
  formatCommissionVersionLabel,
  type CommissionRateVersion
} from '@/api/commissionRates'

const { t } = useI18n()
const loading = ref(false)
const saving = ref(false)
const creating = ref(false)
const salesVersions = ref<CommissionRateVersion[]>([])
const purchaseVersions = ref<CommissionRateVersion[]>([])
const salesVersionId = ref('')
const purchaseVersionId = ref('')
const delayDays = ref<number | undefined>(0)
const createVisible = ref(false)
const createRoleType = ref(COMMISSION_ROLE_SALES)
const createForm = reactive({
  remark: '',
  copyFromVersionId: ''
})

const createTitle = computed(() =>
  createRoleType.value === COMMISSION_ROLE_PURCHASE
    ? t('commissionParams.createPurchaseVersion')
    : t('commissionParams.createSalesVersion')
)

const createSourceVersions = computed(() =>
  createRoleType.value === COMMISSION_ROLE_PURCHASE ? purchaseVersions.value : salesVersions.value
)

function applySettings(data: Awaited<ReturnType<typeof commissionRatesApi.settings>>) {
  salesVersions.value = data.salesVersions || []
  purchaseVersions.value = data.purchaseVersions || []
  salesVersionId.value = data.salesActiveVersionId || salesVersions.value.find((v) => v.isActive)?.id || ''
  purchaseVersionId.value =
    data.purchaseActiveVersionId || purchaseVersions.value.find((v) => v.isActive)?.id || ''
  const days = Number(data.receiptWriteoffDelayDays)
  delayDays.value = Number.isInteger(days) ? days : 0
}

async function load() {
  loading.value = true
  try {
    applySettings(await commissionRatesApi.settings())
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('commissionParams.loadFailed'))
  } finally {
    loading.value = false
  }
}

async function saveActive() {
  if (!salesVersionId.value || !purchaseVersionId.value) {
    ElMessage.error(t('commissionParams.selectBothVersions'))
    return
  }
  saving.value = true
  try {
    applySettings(
      await commissionRatesApi.setActive({
        salesVersionId: salesVersionId.value,
        purchaseVersionId: purchaseVersionId.value,
        receiptWriteoffDelayDays: delayDays.value ?? 0
      })
    )
    ElMessage.success(t('commissionParams.saveSuccess'))
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('commissionParams.saveFailed'))
  } finally {
    saving.value = false
  }
}

function openCreate(roleType: number) {
  createRoleType.value = roleType
  const list = roleType === COMMISSION_ROLE_PURCHASE ? purchaseVersions.value : salesVersions.value
  createForm.remark = ''
  createForm.copyFromVersionId = list.find((v) => v.isActive)?.id || list[0]?.id || ''
  createVisible.value = true
}

async function submitCreate() {
  if (!createForm.remark.trim()) {
    ElMessage.error(t('commissionParams.versionRemarkRequired'))
    return
  }
  creating.value = true
  try {
    await commissionRatesApi.createVersion({
      roleType: createRoleType.value,
      remark: createForm.remark.trim(),
      copyFromVersionId: createForm.copyFromVersionId || undefined
    })
    ElMessage.success(t('commissionParams.createVersionSuccess'))
    createVisible.value = false
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('commissionParams.saveFailed'))
  } finally {
    creating.value = false
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.section-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 14px;
}

.section-title {
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
  display: flex;
  align-items: center;
  gap: 8px;
}

.title-bar {
  display: inline-block;
  width: 3px;
  height: 14px;
  border-radius: 2px;
  background: $cyan-primary;
}

.section-hint {
  margin: 6px 0 0;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}

.settings-card {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 20px 16px 8px;
}

.version-row {
  display: flex;
  gap: 10px;
  width: 100%;
}

.version-select {
  flex: 1;
  max-width: 420px;
}

.delay-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.delay-input {
  width: 160px;
}

.delay-unit {
  font-size: 13px;
  color: $text-muted;
}

.delay-hint {
  margin: 6px 0 0;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}
</style>
