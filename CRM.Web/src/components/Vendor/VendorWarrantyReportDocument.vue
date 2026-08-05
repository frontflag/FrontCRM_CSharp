<template>
  <!-- 兼容旧引用：转发到与 Packing 同映射的三租户质保书皮肤 -->
  <component :is="skin" v-bind="props" />
</template>

<script setup lang="ts">
import { resolveWarrantyLetterReportSkin } from '@/components/warrantyLetterReport/resolveWarrantyLetterReportSkin'
import { LOGIN_TENANT_ID } from '@/config/loginTenant'
import {
  warrantyLetterReportDocumentPropDefaults,
  type WarrantyLetterReportDocumentProps
} from '@/components/warrantyLetterReport/types'

const props = withDefaults(
  defineProps<WarrantyLetterReportDocumentProps>(),
  warrantyLetterReportDocumentPropDefaults
)

const skin = resolveWarrantyLetterReportSkin(LOGIN_TENANT_ID)
</script>
