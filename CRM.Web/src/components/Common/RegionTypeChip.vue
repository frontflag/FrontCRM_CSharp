<template>
  <span
    v-if="kind"
    class="region-type-chip"
    :class="`region-type-chip--${kind}`"
  >
    <span>{{ label }}</span>
  </span>
  <span v-else>—</span>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'

const props = defineProps<{
  regionType?: number | null
}>()

const { t } = useI18n()

const kind = computed<'domestic' | 'overseas' | null>(() => {
  if (props.regionType == null) return null
  const n = Number(props.regionType)
  if (n !== 10 && n !== 20) return null
  return normalizeRegionType(n) === REGION_TYPE_OVERSEAS ? 'overseas' : 'domestic'
})

const label = computed(() =>
  kind.value === 'overseas'
    ? t('inventoryList.warehouse.regionOverseas')
    : t('inventoryList.warehouse.regionDomestic')
)
</script>

<style scoped lang="scss">
.region-type-chip {
  display: inline-flex;
  align-items: center;
  gap: 0;
  padding: 2px 8px;
  border-radius: 999px;
  font-size: 12px;
  line-height: 1.2;
}

.region-type-chip--domestic {
  color: #e6a23c;
  background: rgba(230, 162, 60, 0.14);
}

.region-type-chip--overseas {
  color: #409eff;
  background: rgba(64, 158, 255, 0.14);
}
</style>
