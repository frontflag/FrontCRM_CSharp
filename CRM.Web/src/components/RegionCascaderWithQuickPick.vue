<template>
  <div class="region-cascader-field">
    <el-cascader
      :model-value="modelValue"
      :options="cascaderOptions"
      :props="cascaderInnerProps"
      separator="/"
      :placeholder="placeholder"
      :clearable="clearable"
      class="region-cascader-field__cascader"
      :class="cascaderClass"
      style="width: 100%"
      popper-class="region-cascader-popper"
      @update:model-value="onCascaderUpdate"
    >
      <template #default="{ node, data }">
        <span class="region-cascader-node">
          <button
            v-if="showStarForRow(node, data)"
            type="button"
            class="region-cascader-star"
            :class="{ 'is-on': isStarOn(node, data) }"
            :title="isStarOn(node, data) ? '取消收藏' : '收藏该地区'"
            @click.stop="onStarClick(node, data)"
          >
            <el-icon>
              <StarFilled v-if="isStarOn(node, data)" />
              <Star v-else />
            </el-icon>
          </button>
          <span class="region-cascader-node__text">{{ data.label }}</span>
          <button
            v-if="showUnfavoriteForFavoritesRow(node, data)"
            type="button"
            class="region-cascader-unfav"
            title="取消收藏"
            aria-label="取消收藏"
            @click.stop="onUnfavoriteFavoritesRow(data)"
          >
            <el-icon><Close /></el-icon>
          </button>
        </span>
      </template>
    </el-cascader>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { CascaderValue } from 'element-plus';
import { Star, StarFilled, Close } from '@element-plus/icons-vue';
import { regionData, type RegionNode } from '@/data/regions';
import { useFavoriteRegions } from '@/composables/useFavoriteRegions';

/** 第一列首项：收藏（下挂已保存的省/市或省/市/区） */
const FAVORITES_ROOT = '__crm_favorites__';

/** 可选任意一级：点选省或市即可确认；与 showPrefix:false 搭配减少单选圆点占位 */
const cascaderInnerProps = {
  expandTrigger: 'click' as const,
  checkStrictly: true,
  emitPath: true,
  checkOnClickNode: true,
  showPrefix: false
};

const props = withDefaults(
  defineProps<{
    modelValue: string[];
    options?: RegionNode[];
    placeholder?: string;
    clearable?: boolean;
    cascaderClass?: string;
  }>(),
  {
    options: () => regionData,
    placeholder: '请选择地区',
    clearable: true,
    cascaderClass: ''
  }
);

const emit = defineEmits<{
  (e: 'update:modelValue', value: string[]): void;
  (e: 'change', value: string[]): void;
}>();

const { favorites, hasPath, addFromPath, removeByPath, touchPath, pathKey } = useFavoriteRegions();

const favoriteList = computed(() => [...favorites.value]);

function favLeafValue(path: readonly string[]): string {
  return `__fav::${encodeURIComponent(pathKey(path))}`;
}

function parseFavLeafValue(v: unknown): string[] | null {
  if (typeof v !== 'string' || !v.startsWith('__fav::')) return null;
  const key = decodeURIComponent(v.slice('__fav::'.length));
  const hit = favoriteList.value.find((f) => pathKey(f.path) === key);
  return hit ? [...hit.path] : null;
}

type CascaderRow = RegionNode & { leaf?: boolean; disabled?: boolean };

const cascaderOptions = computed((): CascaderRow[] => {
  const list = favoriteList.value;
  const favChildren: CascaderRow[] = list.length
    ? list.map((f) => ({
        value: favLeafValue(f.path),
        label: f.label,
        leaf: true
      }))
    : [
        {
          value: '__fav_empty__',
          label: '暂无收藏',
          disabled: true,
          leaf: true
        }
      ];

  /** 禁止将「收藏」当作省/市选中；checkStrictly 下仍可点击展开子菜单 */
  const favRoot: CascaderRow = {
    value: FAVORITES_ROOT,
    label: '收藏',
    disabled: true,
    children: favChildren
  };

  return [favRoot, ...props.options];
});

function isUnderFavoritesBranch(node: { pathValues?: CascaderValue[] }): boolean {
  const pv = node.pathValues;
  return Array.isArray(pv) && pv.length > 0 && pv[0] === FAVORITES_ROOT;
}

/** 「收藏」子菜单中每条真实收藏右侧：取消收藏 */
function showUnfavoriteForFavoritesRow(
  node: { pathValues?: CascaderValue[] },
  data: CascaderRow
): boolean {
  if (!data?.disabled && isUnderFavoritesBranch(node) && typeof data.value === 'string') {
    return data.value.startsWith('__fav::');
  }
  return false;
}

function onUnfavoriteFavoritesRow(data: CascaderRow): void {
  const path = parseFavLeafValue(data.value);
  if (path?.length) removeByPath(path);
}

/** 省=1、市=2（与 Element Plus 级联节点 level 一致） */
function showStarForRow(node: { level: number; pathValues?: CascaderValue[] }, data: CascaderRow): boolean {
  if (!data || data.disabled) return false;
  if (data.value === FAVORITES_ROOT) return false;
  if (typeof data.value === 'string' && data.value.startsWith('__fav::')) return false;
  if (data.value === '__fav_empty__') return false;
  if (isUnderFavoritesBranch(node)) return false;
  if (node.level !== 1 && node.level !== 2) return false;
  return resolveTargetPathForStar(node, data) !== null;
}

/** 省行 → [省, 第一个市]；市行 → [省, 市]（与可选二级地区一致） */
function resolveTargetPathForStar(
  node: { level: number; pathValues?: CascaderValue[] },
  data: CascaderRow
): string[] | null {
  const children = data.children;
  if (!children?.length) return null;

  if (node.level === 1) {
    const city = children[0] as RegionNode;
    return [String(data.value), String(city.value)];
  }

  if (node.level === 2) {
    const pv = node.pathValues;
    if (!pv || pv.length < 2) return null;
    return [String(pv[0]), String(pv[1])];
  }

  return null;
}

function isStarOn(node: { level: number; pathValues?: CascaderValue[] }, data: CascaderRow): boolean {
  const p = resolveTargetPathForStar(node, data);
  return p !== null && hasPath(p);
}

function onStarClick(node: { level: number; pathValues?: CascaderValue[] }, data: CascaderRow): void {
  const p = resolveTargetPathForStar(node, data);
  if (!p) return;
  if (hasPath(p)) removeByPath(p);
  else addFromPath(p);
}

function toStringPath(val: CascaderValue | null | undefined): string[] {
  if (val == null) return [];
  if (Array.isArray(val)) return val.map((x) => String(x));
  return [];
}

function onCascaderUpdate(val: CascaderValue | null | undefined): void {
  const arr = toStringPath(val);

  if (arr.length >= 1 && arr[0] === FAVORITES_ROOT) {
    if (arr.length < 2) return;
    const leaf = arr[arr.length - 1];
    if (leaf === '__fav_empty__') return;
    const resolved = parseFavLeafValue(leaf);
    if (resolved && resolved.length >= 2) {
      touchPath(resolved);
      emit('update:modelValue', resolved);
      emit('change', resolved);
      return;
    }
    return;
  }

  emit('update:modelValue', arr);
  emit('change', arr);
}
</script>

<style scoped lang="scss">
.region-cascader-field {
  width: 100%;
}

.region-cascader-field__cascader {
  width: 100%;
}

.region-cascader-node {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  width: 100%;
  min-width: 0;
  max-width: 100%;
}

.region-cascader-node__text {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.region-cascader-star {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 22px;
  height: 22px;
  padding: 0;
  margin: 0;
  border: none;
  border-radius: 4px;
  background: transparent;
  color: var(--el-text-color-secondary, #909399);
  cursor: pointer;
  transition: color 0.15s, background 0.15s;

  &:hover {
    color: var(--el-color-warning, #e6a23c);
    background: var(--el-fill-color-light, rgba(0, 0, 0, 0.06));
  }

  &.is-on {
    color: var(--el-color-warning, #e6a23c);
  }

  .el-icon {
    font-size: 15px;
  }
}

.region-cascader-unfav {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 22px;
  height: 22px;
  padding: 0;
  margin: 0 0 0 4px;
  border: none;
  border-radius: 4px;
  background: transparent;
  color: var(--el-text-color-secondary, #909399);
  cursor: pointer;
  transition: color 0.15s, background 0.15s;

  &:hover {
    color: var(--el-color-danger, #f56c6c);
    background: var(--el-fill-color-light, rgba(0, 0, 0, 0.06));
  }

  .el-icon {
    font-size: 14px;
  }
}
</style>

<style lang="scss">
/* 下拉内行高，避免星标换行 */
.region-cascader-popper {
  .el-cascader-node {
    padding: 0 4px;
  }
}
</style>
