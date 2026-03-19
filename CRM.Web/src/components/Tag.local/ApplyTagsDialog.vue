<template>
  <el-dialog
    v-model="visibleInner"
    :title="title"
    width="520px"
    :close-on-click-modal="false"
  >
    <div class="dialog-body">
      <div class="chips-row">
        <span class="chips-label">已选标签</span>
        <div class="chips-list">
          <span
            v-for="tag in selectedTags"
            :key="tag.id"
            class="chip"
          >
            {{ tag.name }}
            <button class="chip-remove" @click="removeTag(tag.id)">×</button>
          </span>
          <span v-if="!selectedTags.length" class="chips-empty">暂无标签</span>
        </div>
      </div>

      <el-input
        v-model="keyword"
        placeholder="搜索标签名称"
        size="small"
        clearable
        @input="debouncedFetchTags"
      />

      <div class="tags-panel">
        <div
          v-for="tag in tagOptions"
          :key="tag.id"
          class="tag-option"
          :class="{ 'tag-option--active': isSelected(tag.id) }"
          @click="toggleTag(tag)"
        >
          <span class="color-dot" :style="{ backgroundColor: tag.color || '#4FC3F7' }" />
          <span class="name">{{ tag.name }}</span>
          <span class="meta">{{ tag.description || '通用' }}</span>
        </div>
        <div v-if="!tagOptions.length" class="empty">
          暂无可用标签
        </div>
      </div>
    </div>

    <template #footer>
      <el-button @click="visibleInner = false">取消</el-button>
      <el-button type="primary" :loading="saving" @click="handleConfirm">
        确认打标签
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { tagApi, type TagDefinitionDto } from '@/api/tag'

const props = defineProps<{
  modelValue: boolean
  entityType: string
  entityIds: string[]
  title?: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void
  (e: 'success'): void
}>()

const visibleInner = ref(props.modelValue)
watch(
  () => props.modelValue,
  v => (visibleInner.value = v)
)
watch(
  () => visibleInner.value,
  v => emit('update:modelValue', v)
)

const title = computed(() => props.title || '添加标签')

const keyword = ref('')
const tagOptions = ref<TagDefinitionDto[]>([])
const selectedTags = ref<TagDefinitionDto[]>([])
const saving = ref(false)

const isSelected = (id: string) => selectedTags.value.some(t => t.id === id)

const toggleTag = (tag: TagDefinitionDto) => {
  if (isSelected(tag.id)) {
    selectedTags.value = selectedTags.value.filter(t => t.id !== tag.id)
  } else {
    selectedTags.value.push(tag)
  }
}

const removeTag = (id: string) => {
  selectedTags.value = selectedTags.value.filter(t => t.id !== id)
}

const fetchTags = async () => {
  try {
    const all = await tagApi.getTagDefinitions(props.entityType)
    const kw = keyword.value.trim().toLowerCase()
    tagOptions.value = kw
      ? all.filter(t => t.name.toLowerCase().includes(kw))
      : all
  } catch {
    tagOptions.value = []
  }
}

let timer: number | undefined
const debouncedFetchTags = () => {
  if (timer) window.clearTimeout(timer)
  timer = window.setTimeout(fetchTags, 300)
}

watch(
  () => visibleInner.value,
  v => {
    if (v) {
      keyword.value = ''
      selectedTags.value = []
      fetchTags()
    }
  }
)

const handleConfirm = async () => {
  if (!selectedTags.value.length) {
    ElMessage.warning('请先选择标签')
    return
  }
  if (!props.entityIds.length) {
    ElMessage.warning('未找到要打标签的记录')
    return
  }

  saving.value = true
  try {
    await tagApi.applyTags({
      entityType: props.entityType,
      entityIds: props.entityIds,
      tagIds: selectedTags.value.map(t => t.id)
    })
    ElMessage.success('打标签成功')
    visibleInner.value = false
    emit('success')
  } catch (e) {
    console.error(e)
    ElMessage.error('打标签失败，请稍后重试')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped lang="scss">
.dialog-body {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.chips-row {
  display: flex;
  align-items: flex-start;
  gap: 8px;
}

.chips-label {
  font-size: 12px;
  color: rgba(200, 216, 232, 0.8);
  margin-top: 4px;
  flex-shrink: 0;
}

.chips-list {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.chip {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 6px;
  border-radius: 999px;
  background: rgba(0, 212, 255, 0.08);
  border: 1px solid rgba(0, 212, 255, 0.25);
  font-size: 11px;
  color: #e0f4ff;
}

.chip-remove {
  border: none;
  background: transparent;
  color: rgba(200, 216, 232, 0.7);
  cursor: pointer;
  font-size: 11px;
}

.chips-empty {
  font-size: 12px;
  color: rgba(200, 216, 232, 0.6);
}

.tags-panel {
  max-height: 260px;
  overflow: auto;
  margin-top: 4px;
  padding: 4px 0;
  border-radius: 6px;
  border: 1px solid rgba(255, 255, 255, 0.06);
}

.tag-option {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 10px;
  cursor: pointer;
  font-size: 12px;
  color: rgba(224, 244, 255, 0.85);

  &:hover {
    background: rgba(0, 212, 255, 0.06);
  }

  &--active {
    background: rgba(0, 212, 255, 0.12);
  }

  .color-dot {
    width: 10px;
    height: 10px;
    border-radius: 999px;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.5);
  }

  .name {
    flex: 1;
  }

  .meta {
    font-size: 11px;
    color: rgba(200, 216, 232, 0.7);
  }
}

.empty {
  text-align: center;
  padding: 16px;
  font-size: 12px;
  color: rgba(200, 216, 232, 0.7);
}
</style>

