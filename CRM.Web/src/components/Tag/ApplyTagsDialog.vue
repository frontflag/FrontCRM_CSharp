<template>
  <el-dialog
    :model-value="modelValue"
    :title="title"
    width="480px"
    :close-on-click-modal="false"
    class="dark-dialog tag-dialog"
    @update:model-value="$emit('update:modelValue', $event)"
    @open="onOpen"
  >
    <!-- 已选标签 -->
    <div class="section-label">已选标签</div>
    <div class="selected-tags">
      <span
        v-for="tag in selectedTags"
        :key="tag.id"
        class="tag-chip tag-chip--selected"
        :style="tag.color ? { borderColor: tag.color + '66', color: tag.color, background: tag.color + '22' } : {}"
        @click="toggleTag(tag)"
      >
        {{ tag.name }}
        <span class="tag-remove">×</span>
      </span>
      <span v-if="selectedTags.length === 0" class="tag-placeholder">暂无标签</span>
    </div>

    <!-- 搜索框 -->
    <div class="search-row">
      <input
        v-model="searchText"
        class="tag-search-input"
        placeholder="搜索或新建标签..."
        @keydown.enter="handleEnterCreate"
      />
    </div>

    <!-- 可用标签列表 -->
    <div class="available-tags">
      <template v-if="filteredAvailable.length > 0">
        <span
          v-for="tag in filteredAvailable"
          :key="tag.id"
          class="tag-chip tag-chip--available"
          :style="tag.color ? { borderColor: tag.color + '55', color: tag.color, background: tag.color + '18' } : {}"
          @click="toggleTag(tag)"
        >
          {{ tag.name }}
        </span>
      </template>
      <div v-else-if="searchText.trim()" class="tag-hint">
        按 Enter 创建标签「{{ searchText.trim() }}」
      </div>
      <div v-else class="tag-hint">暂无可用标签</div>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <button class="btn-cancel" @click="$emit('update:modelValue', false)">取消</button>
        <button class="btn-confirm" :disabled="loading" @click="handleConfirm">
          <span v-if="loading">保存中...</span>
          <span v-else>确认打标签</span>
        </button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { tagApi, type TagDefinitionDto } from '@/api/tag';

const props = defineProps<{
  modelValue: boolean;
  title?: string;
  entityType: string;
  entityIds: string[];
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', val: boolean): void;
  (e: 'success'): void;
}>();

const loading = ref(false);
const searchText = ref('');
const allTags = ref<TagDefinitionDto[]>([]);
const selectedTags = ref<TagDefinitionDto[]>([]);

const filteredAvailable = computed(() => {
  const keyword = searchText.value.trim().toLowerCase();
  const selectedIds = new Set(selectedTags.value.map(t => t.id));
  return allTags.value.filter(t =>
    !selectedIds.has(t.id) &&
    (!keyword || t.name.toLowerCase().includes(keyword))
  );
});

const onOpen = async () => {
  searchText.value = '';
  selectedTags.value = [];
  allTags.value = await tagApi.getTagDefinitions(props.entityType);
  // 如果只有单个实体，加载其已有标签
  if (props.entityIds.length === 1) {
    const existing = await tagApi.getEntityTags(props.entityType, props.entityIds[0]);
    selectedTags.value = existing;
  }
};

const toggleTag = (tag: TagDefinitionDto) => {
  const idx = selectedTags.value.findIndex(t => t.id === tag.id);
  if (idx >= 0) {
    selectedTags.value.splice(idx, 1);
  } else {
    selectedTags.value.push(tag);
  }
};

const handleEnterCreate = async () => {
  const name = searchText.value.trim();
  if (!name) return;
  // 检查是否已存在同名标签
  const existing = allTags.value.find(t => t.name === name);
  if (existing) {
    toggleTag(existing);
    searchText.value = '';
    return;
  }
  // 创建新标签（后端未实现时本地添加）
  const newTag = await tagApi.createTagDefinition({ name, entityType: props.entityType });
  const tag: TagDefinitionDto = newTag || { id: `local-${Date.now()}`, name };
  allTags.value.push(tag);
  selectedTags.value.push(tag);
  searchText.value = '';
};

const handleConfirm = async () => {
  loading.value = true;
  try {
    await tagApi.applyTags({
      entityType: props.entityType,
      entityIds: props.entityIds,
      tagIds: selectedTags.value.map(t => t.id),
    });
    ElMessage.success('标签已更新');
    emit('update:modelValue', false);
    emit('success');
  } catch {
    ElMessage.warning('标签保存失败，后端接口暂未开放');
    emit('update:modelValue', false);
  } finally {
    loading.value = false;
  }
};
</script>

<style scoped lang="scss">
.section-label {
  font-size: 11px;
  color: rgba(200, 216, 232, 0.45);
  letter-spacing: 0.5px;
  margin-bottom: 8px;
}

.selected-tags {
  min-height: 32px;
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  align-items: center;
  padding: 8px 10px;
  background: rgba(0, 212, 255, 0.04);
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 6px;
  margin-bottom: 14px;
}

.tag-placeholder {
  font-size: 12px;
  color: rgba(200, 216, 232, 0.3);
  font-style: italic;
}

.search-row {
  margin-bottom: 12px;
}

.tag-search-input {
  width: 100%;
  box-sizing: border-box;
  padding: 8px 12px;
  background: rgba(10, 22, 40, 0.8);
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 6px;
  color: rgba(224, 244, 255, 0.9);
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  outline: none;
  transition: border-color 0.2s;

  &::placeholder {
    color: rgba(200, 216, 232, 0.3);
  }

  &:focus {
    border-color: rgba(0, 212, 255, 0.45);
    box-shadow: 0 0 0 2px rgba(0, 212, 255, 0.08);
  }
}

.available-tags {
  min-height: 60px;
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  align-items: flex-start;
  padding: 10px;
  background: rgba(255, 255, 255, 0.02);
  border: 1px solid rgba(255, 255, 255, 0.06);
  border-radius: 6px;
  margin-bottom: 4px;
}

.tag-hint {
  font-size: 12px;
  color: rgba(200, 216, 232, 0.35);
  font-style: italic;
  width: 100%;
  text-align: center;
  padding: 8px 0;
}

.tag-chip {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 3px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 500;
  border: 1px solid rgba(0, 212, 255, 0.3);
  color: rgba(0, 212, 255, 0.85);
  background: rgba(0, 212, 255, 0.08);
  cursor: pointer;
  transition: all 0.15s;
  user-select: none;

  &--selected {
    border-color: rgba(0, 212, 255, 0.5);
    background: rgba(0, 212, 255, 0.15);

    &:hover {
      background: rgba(201, 87, 69, 0.15);
      border-color: rgba(201, 87, 69, 0.4);
      color: #e07060;
    }
  }

  &--available {
    &:hover {
      background: rgba(0, 212, 255, 0.15);
      border-color: rgba(0, 212, 255, 0.5);
    }
  }
}

.tag-remove {
  font-size: 14px;
  line-height: 1;
  opacity: 0.6;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}

.btn-cancel {
  padding: 8px 18px;
  background: transparent;
  border: 1px solid rgba(200, 216, 232, 0.2);
  border-radius: 6px;
  color: rgba(200, 216, 232, 0.65);
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.15s;

  &:hover {
    border-color: rgba(200, 216, 232, 0.4);
    color: rgba(200, 216, 232, 0.9);
  }
}

.btn-confirm {
  padding: 8px 20px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: 6px;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}
</style>

<style lang="scss">
/* 全局覆盖 el-dialog 深色主题 */
.dark-dialog.el-dialog {
  --el-dialog-bg-color: #0d1e35;
  --el-dialog-border-radius: 10px;
  background: #0d1e35 !important;
  border: 1px solid rgba(0, 212, 255, 0.15);
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.6), 0 0 0 1px rgba(0, 212, 255, 0.08);

  .el-dialog__header {
    background: transparent;
    border-bottom: 1px solid rgba(0, 212, 255, 0.1);
    padding: 16px 20px 14px;

    .el-dialog__title {
      color: rgba(224, 244, 255, 0.92);
      font-size: 15px;
      font-weight: 600;
      letter-spacing: 0.3px;
    }

    .el-dialog__headerbtn .el-dialog__close {
      color: rgba(200, 216, 232, 0.5);
      &:hover { color: rgba(224, 244, 255, 0.9); }
    }
  }

  .el-dialog__body {
    background: transparent;
    color: rgba(200, 216, 232, 0.85);
    padding: 18px 20px;
  }

  .el-dialog__footer {
    background: transparent;
    border-top: 1px solid rgba(255, 255, 255, 0.06);
    padding: 12px 20px 16px;
  }
}
</style>
