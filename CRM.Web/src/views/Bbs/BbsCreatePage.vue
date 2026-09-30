<template>
  <div class="bbs-create-page">
    <div class="page-header">
      <h2>{{ isEdit ? t('bbs.editTitle') : t('bbs.createTitle') }}</h2>
    </div>

    <el-card shadow="never" v-loading="loading">
      <el-form label-width="88px" @submit.prevent>
        <el-form-item :label="t('bbs.form.title')" required>
          <el-input v-model="form.title" maxlength="200" show-word-limit :placeholder="t('bbs.form.titlePh')" />
        </el-form-item>
        <el-form-item :label="t('bbs.form.type')" required>
          <el-radio-group v-model="form.type">
            <el-radio
              v-for="tp in BbsSubjectTypeOptions"
              :key="tp"
              :value="tp"
            >{{ t(BbsSubjectTypeI18nKey[tp]) }}</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item :label="t('bbs.form.anonymous')">
          <el-switch v-model="form.anonymous" />
        </el-form-item>
        <el-form-item :label="t('bbs.form.content')" required>
          <el-input
            v-model="form.content"
            type="textarea"
            :rows="14"
            :placeholder="t('bbs.form.contentPh')"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="saving" @click="submit">{{ t('bbs.form.submit') }}</el-button>
          <el-button @click="goBack">{{ t('bbs.form.cancel') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { bbsApi, BbsSubjectType, BbsSubjectTypeI18nKey, BbsSubjectTypeOptions } from '@/api/bbs'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const editId = computed(() => (typeof route.params.id === 'string' ? route.params.id : ''))
const isEdit = computed(() => !!editId.value && route.name === 'BbsEdit')
const loading = ref(false)
const saving = ref(false)
const form = reactive({
  title: '',
  content: '',
  type: BbsSubjectType.Share as number,
  anonymous: false
})

async function load() {
  if (!isEdit.value) return
  loading.value = true
  try {
    const d = await bbsApi.detail(editId.value)
    form.title = d.title
    form.content = d.content
    form.type = d.type
    form.anonymous = d.anonymous
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.loadFailed')))
  } finally {
    loading.value = false
  }
}

async function submit() {
  if (!form.title.trim() || !form.content.trim()) {
    ElMessage.warning(t('bbs.form.required'))
    return
  }
  saving.value = true
  try {
    const body = {
      title: form.title.trim(),
      content: form.content.trim(),
      type: form.type,
      anonymous: form.anonymous
    }
    const d = isEdit.value
      ? await bbsApi.update(editId.value, body)
      : await bbsApi.create(body)
    ElMessage.success(t('bbs.form.saved'))
    router.replace({ name: 'BbsDetail', params: { id: d.id } })
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.saveFailed')))
  } finally {
    saving.value = false
  }
}

function goBack() {
  if (isEdit.value) router.push({ name: 'BbsDetail', params: { id: editId.value } })
  else router.push({ name: 'BbsList' })
}

onMounted(() => {
  void load()
})
</script>

<style scoped>
.bbs-create-page {
  padding: 16px 20px 32px;
  max-width: 880px;
}
.page-header h2 {
  margin: 0 0 12px;
  font-size: 18px;
  font-weight: 600;
}
</style>
