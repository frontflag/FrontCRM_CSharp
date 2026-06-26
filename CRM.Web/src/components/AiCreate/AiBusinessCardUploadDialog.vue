<template>
  <el-dialog
    v-model="visibleModel"
    :title="t('aiBusinessCard.uploadDialog.title')"
    width="640px"
    destroy-on-close
    :close-on-click-modal="false"
    @closed="onClosed"
  >
    <p class="bc-upload-dialog__hint">{{ t('aiBusinessCard.uploadDialog.hint') }}</p>
    <div class="bc-upload-dialog__dual">
      <div class="bc-upload-dialog__side">
        <div class="bc-upload-dialog__side-label">{{ t('aiBusinessCard.uploadDialog.frontSide') }}</div>
        <div v-if="frontPreviewUrl" class="bc-upload-dialog__preview">
          <img :src="frontPreviewUrl" alt="" class="bc-upload-dialog__img" />
          <p class="bc-upload-dialog__name">{{ frontFile?.name }}</p>
          <button type="button" class="bc-upload-dialog__clear-btn" @click="clearFront">
            {{ t('aiBusinessCard.uploadDialog.clearSide') }}
          </button>
        </div>
        <label
          v-else
          class="bc-upload-dialog__zone"
          :class="{ 'bc-upload-dialog__zone--dragover': frontDragOver }"
          @dragenter.prevent="onDragEnter('front')"
          @dragover.prevent="frontDragOver = true"
          @dragleave.prevent="onDragLeave('front')"
          @drop.prevent="onDrop('front', $event)"
        >
          <input type="file" accept=".jpg,.jpeg,.png,.webp,.heic" style="display: none" @change="onFileSelect('front', $event)" />
          <span>{{ t('aiBusinessCard.uploadDialog.pickFile') }}</span>
        </label>
      </div>

      <div class="bc-upload-dialog__side">
        <div class="bc-upload-dialog__side-label">
          {{ t('aiBusinessCard.uploadDialog.backSide') }}
          <span class="bc-upload-dialog__optional">{{ t('aiBusinessCard.uploadDialog.optional') }}</span>
        </div>
        <div v-if="backPreviewUrl" class="bc-upload-dialog__preview">
          <img :src="backPreviewUrl" alt="" class="bc-upload-dialog__img" />
          <p class="bc-upload-dialog__name">{{ backFile?.name }}</p>
          <button type="button" class="bc-upload-dialog__clear-btn" @click="clearBack">
            {{ t('aiBusinessCard.uploadDialog.clearSide') }}
          </button>
        </div>
        <label
          v-else
          class="bc-upload-dialog__zone"
          :class="{ 'bc-upload-dialog__zone--dragover': backDragOver }"
          @dragenter.prevent="onDragEnter('back')"
          @dragover.prevent="backDragOver = true"
          @dragleave.prevent="onDragLeave('back')"
          @drop.prevent="onDrop('back', $event)"
        >
          <input type="file" accept=".jpg,.jpeg,.png,.webp,.heic" style="display: none" @change="onFileSelect('back', $event)" />
          <span>{{ t('aiBusinessCard.uploadDialog.pickFile') }}</span>
        </label>
      </div>
    </div>
    <template #footer>
      <el-button @click="visibleModel = false" :disabled="loading">{{ t('common.cancel') }}</el-button>
      <el-button v-if="frontFile || backFile" @click="clearAll" :disabled="loading">
        {{ t('aiBusinessCard.uploadDialog.repick') }}
      </el-button>
      <el-button type="primary" :loading="loading" :disabled="!frontFile" @click="emitParse">
        {{ t('aiBusinessCard.uploadDialog.parse') }}
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'

export type BusinessCardUploadPayload = {
  front: File
  back?: File | null
}

type CardSide = 'front' | 'back'

const props = defineProps<{
  visible: boolean
  loading: boolean
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  parse: [payload: BusinessCardUploadPayload]
}>()

const { t } = useI18n()

const frontFile = ref<File | null>(null)
const backFile = ref<File | null>(null)
const frontPreviewUrl = ref('')
const backPreviewUrl = ref('')
const frontDragOver = ref(false)
const backDragOver = ref(false)
const frontDragDepth = ref(0)
const backDragDepth = ref(0)

const visibleModel = computed({
  get: () => props.visible,
  set: (v) => emit('update:visible', v)
})

watch(
  () => props.visible,
  (open) => {
    if (open) clearAll()
  }
)

function validateFile(file: File): boolean {
  const ext = '.' + (file.name.split('.').pop() || '').toLowerCase()
  const allowed = ['.jpg', '.jpeg', '.png', '.webp', '.heic']
  if (!allowed.includes(ext) && !file.type.startsWith('image/')) {
    ElMessage.warning(t('aiBusinessCard.uploadDialog.invalidFormat'))
    return false
  }
  if (file.size > 20 * 1024 * 1024) {
    ElMessage.warning(t('aiBusinessCard.uploadDialog.tooLarge'))
    return false
  }
  return true
}

function acceptSide(side: CardSide, file: File | null | undefined) {
  if (!file || !validateFile(file)) return
  const url = URL.createObjectURL(file)
  if (side === 'front') {
    if (frontPreviewUrl.value) URL.revokeObjectURL(frontPreviewUrl.value)
    frontFile.value = file
    frontPreviewUrl.value = url
  } else {
    if (backPreviewUrl.value) URL.revokeObjectURL(backPreviewUrl.value)
    backFile.value = file
    backPreviewUrl.value = url
  }
}

function onFileSelect(side: CardSide, e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  acceptSide(side, file)
}

function onDragEnter(side: CardSide) {
  if (side === 'front') {
    frontDragDepth.value += 1
    frontDragOver.value = true
  } else {
    backDragDepth.value += 1
    backDragOver.value = true
  }
}

function onDragLeave(side: CardSide) {
  if (side === 'front') {
    frontDragDepth.value = Math.max(0, frontDragDepth.value - 1)
    if (frontDragDepth.value === 0) frontDragOver.value = false
  } else {
    backDragDepth.value = Math.max(0, backDragDepth.value - 1)
    if (backDragDepth.value === 0) backDragOver.value = false
  }
}

function onDrop(side: CardSide, e: DragEvent) {
  if (side === 'front') {
    frontDragDepth.value = 0
    frontDragOver.value = false
  } else {
    backDragDepth.value = 0
    backDragOver.value = false
  }
  acceptSide(side, e.dataTransfer?.files?.[0])
}

function clearFront() {
  frontFile.value = null
  frontDragOver.value = false
  frontDragDepth.value = 0
  if (frontPreviewUrl.value) {
    URL.revokeObjectURL(frontPreviewUrl.value)
    frontPreviewUrl.value = ''
  }
}

function clearBack() {
  backFile.value = null
  backDragOver.value = false
  backDragDepth.value = 0
  if (backPreviewUrl.value) {
    URL.revokeObjectURL(backPreviewUrl.value)
    backPreviewUrl.value = ''
  }
}

function clearAll() {
  clearFront()
  clearBack()
}

function onClosed() {
  clearAll()
}

function emitParse() {
  if (!frontFile.value) {
    ElMessage.warning(t('aiBusinessCard.uploadDialog.noFrontFile'))
    return
  }
  emit('parse', {
    front: frontFile.value,
    back: backFile.value
  })
}
</script>

<style scoped lang="scss">
.bc-upload-dialog__hint {
  margin: 0 0 12px;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.bc-upload-dialog__dual {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

.bc-upload-dialog__side-label {
  margin-bottom: 8px;
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.bc-upload-dialog__optional {
  margin-left: 4px;
  font-weight: 400;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.bc-upload-dialog__zone {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 160px;
  padding: 12px;
  border: 1px dashed var(--el-border-color);
  border-radius: 8px;
  cursor: pointer;
  color: var(--el-text-color-secondary);
  font-size: 13px;
  text-align: center;
  transition: border-color 0.15s ease, background-color 0.15s ease;

  &--dragover {
    border-color: var(--el-color-primary);
    background: var(--el-color-primary-light-9);
    color: var(--el-color-primary);
  }
}

.bc-upload-dialog__preview {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 8px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  background: var(--el-fill-color-light);
}

.bc-upload-dialog__img {
  max-width: 100%;
  max-height: 200px;
  object-fit: contain;
}

.bc-upload-dialog__name {
  margin: 8px 0 4px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  word-break: break-all;
  text-align: center;
}

.bc-upload-dialog__clear-btn {
  border: none;
  background: none;
  color: var(--el-color-primary);
  font-size: 12px;
  cursor: pointer;
  padding: 0;

  &:hover {
    text-decoration: underline;
  }
}
</style>
