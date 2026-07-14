<template>
  <ImportRFQDialog v-model="importVisible" @parsed="onParsed" />
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import ImportRFQDialog from '@/views/RFQ/components/ImportRFQDialog.vue'
import { rfqPrefillToFormPayload, type ParsedRfqFields } from '@/utils/entityParseSchema'
import { setAiPrefill } from '@/utils/aiPrefill'

const router = useRouter()

const importVisible = ref(false)

function open() {
  importVisible.value = true
}

function onParsed(data: ParsedRfqFields) {
  const formPayload = rfqPrefillToFormPayload(data)
  formPayload._prefillSource = 'excel-import'
  const token = setAiPrefill('RFQ', formPayload)
  importVisible.value = false
  router.push({ name: 'RFQCreate', query: { aiPrefill: token } })
}

defineExpose({ open })
</script>
