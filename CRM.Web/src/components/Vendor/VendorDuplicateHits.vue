<template>
  <div class="vendor-dup-hits">
    <p v-if="truncated" class="vendor-dup-hits__truncated">{{ t('vendorEdit.duplicate.truncated') }}</p>
    <div
      v-for="row in matches"
      :key="row.id"
      class="vendor-dup-card"
    >
      <div class="vendor-dup-card__meta">
        {{ t('vendorEdit.duplicate.meta', {
          purchaser: row.purchaserName?.trim() || t('vendorEdit.duplicate.purchaserUnknown'),
          date: formatDate(row.createTime, 'YYYY-MM-DD')
        }) }}
        <span v-if="row.isDeleted" class="vendor-dup-tag vendor-dup-tag--deleted">{{ t('vendorEdit.duplicate.deleted') }}</span>
        <span v-if="row.blackList" class="vendor-dup-tag vendor-dup-tag--black">{{ t('vendorEdit.duplicate.blacklist') }}</span>
      </div>
      <div class="vendor-dup-card__name">
        <span class="vendor-dup-label">{{ t('vendorEdit.fields.officialName') }}：</span>
        <button
          v-if="row.canViewDetail"
          type="button"
          class="vendor-dup-link"
          @click="openVendor(row.id)"
        >{{ dash(row.officialName) }}</button>
        <span v-else class="vendor-dup-muted">{{ dash(row.officialName) }}</span>
      </div>
      <div>
        <span class="vendor-dup-label">{{ t('vendorEdit.fields.englishOfficialName') }}：</span>{{ dash(row.englishOfficialName) }}
      </div>
      <div>
        <span class="vendor-dup-label">{{ t('vendorEdit.fields.taxNumber') }}：</span>{{ dash(row.creditCode) }}
      </div>
      <div>
        <span class="vendor-dup-label">{{ t('vendorEdit.fields.duns') }}：</span>{{ dash(row.duns) }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import type { VendorDuplicateMatch } from '@/types/vendor'
import { formatDate } from '@/utils/date'

defineProps<{
  matches: VendorDuplicateMatch[]
  truncated?: boolean
}>()

const { t } = useI18n()
const router = useRouter()

function dash(v?: string | null) {
  const s = (v ?? '').trim()
  return s || '—'
}

function openVendor(id: string) {
  const href = router.resolve({ name: 'VendorDetail', params: { id } }).href
  window.open(href, '_blank')
}
</script>

<style scoped lang="scss">
.vendor-dup-hits__truncated {
  margin: 0 0 10px;
  font-size: 13px;
  line-height: 1.5;
  color: var(--el-color-warning);
}

.vendor-dup-card {
  margin-bottom: 12px;
  padding: 10px 12px;
  border: 1px solid var(--el-border-color);
  border-radius: 6px;
  font-size: 13px;
  line-height: 1.6;
  background: #fff;
  color: var(--el-text-color-primary);
  white-space: normal;

  &:last-child {
    margin-bottom: 0;
  }
}

.vendor-dup-card__meta {
  margin-bottom: 4px;
  color: var(--el-text-color-regular);
}

.vendor-dup-label {
  color: var(--el-text-color-regular);
}

.vendor-dup-link {
  padding: 0;
  border: 0;
  background: none;
  color: var(--el-color-primary);
  cursor: pointer;
  font: inherit;
  font-weight: 600;
  text-decoration: underline;
}

.vendor-dup-muted {
  color: var(--el-text-color-secondary);
}

.vendor-dup-tag {
  display: inline-block;
  margin-left: 6px;
  padding: 0 6px;
  border-radius: 4px;
  font-size: 11px;
  line-height: 18px;
}

.vendor-dup-tag--deleted {
  background: var(--el-fill-color);
  color: var(--el-text-color-secondary);
}

.vendor-dup-tag--black {
  background: rgba(201, 87, 69, 0.12);
  color: #c95745;
}
</style>
