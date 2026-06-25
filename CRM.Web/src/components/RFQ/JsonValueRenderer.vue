<template>
  <!-- 顶层：每个一级字段一组独立面板（固定顺序：价格在最后） -->
  <div v-if="root && isPlainObject(value)" class="json-panel-stack">
    <JsonValueRenderer
      v-for="entry in rootEntries"
      :key="entry.key"
      :value="entry.value"
      :field-key="entry.key"
      :path="entry.key"
      :depth="1"
      section-root
    />
  </div>

  <!-- 一级字段入口：按类型拆成独立面板 -->
  <template v-else-if="sectionRoot && mode !== 'skip'">
    <template v-if="mode === 'object' && isPlainObject(value)">
      <JsonBlockPanel v-if="scalarBlock.length" :title="sectionLabel(fieldKey)">
        <dl class="kv-grid">
          <div v-for="row in scalarBlock" :key="row.key" class="kv-row">
            <dt>{{ row.label }}</dt>
            <dd>
              <a
                v-if="row.isUrl"
                :href="row.value"
                target="_blank"
                rel="noopener noreferrer"
                class="ext-link"
              >{{ row.value }}</a>
              <span v-else>{{ row.value }}</span>
            </dd>
          </div>
        </dl>
      </JsonBlockPanel>
      <JsonBlockPanel
        v-for="entry in complexBlock"
        :key="entry.key"
        :title="fieldLabel(entry.key)"
      >
        <JsonValueRenderer
          :value="entry.value"
          :field-key="entry.key"
          :path="joinPath(path, entry.key)"
          :depth="depth + 1"
          inline
        />
      </JsonBlockPanel>
    </template>

    <JsonBlockPanel v-else-if="mode === 'industry-news-list'" :title="sectionLabel(fieldKey)">
      <div class="json-panel-stack json-panel-stack--nested">
        <JsonBlockPanel v-for="(item, idx) in industryNewsRows" :key="idx">
          <IndustryNewsItemView :item="item" />
        </JsonBlockPanel>
      </div>
    </JsonBlockPanel>

    <div v-else-if="mode === 'object-list'" class="json-section-group">
      <h3 class="json-section-group__title">{{ sectionLabel(fieldKey) }}</h3>
      <div class="json-panel-stack json-panel-stack--nested">
        <JsonBlockPanel
          v-for="(item, idx) in value as Record<string, unknown>[]"
          :key="idx"
          :title="objectListTitle(item, idx)"
          :head-divider="false"
        >
          <JsonValueRenderer
            :value="item"
            :depth="depth + 1"
            inline
            :omit-field-keys="objectListOmitKeys()"
          />
        </JsonBlockPanel>
      </div>
    </div>

    <JsonBlockPanel v-else :title="sectionLabel(fieldKey)">
      <JsonValueRenderer
        :value="value"
        :field-key="fieldKey"
        :path="path"
        :depth="depth"
        inline
      />
    </JsonBlockPanel>
  </template>

  <!-- 嵌套对象：标量 KV 面板 + 复杂字段各自面板 -->
  <template v-else-if="mode === 'object' && isPlainObject(value)">
    <div class="json-panel-stack json-panel-stack--nested">
      <template v-if="scalarBlock.length">
        <dl v-if="inline" class="kv-grid">
          <div v-for="row in scalarBlock" :key="row.key" class="kv-row">
            <dt>{{ row.label }}</dt>
            <dd>
              <a
                v-if="row.isUrl"
                :href="row.value"
                target="_blank"
                rel="noopener noreferrer"
                class="ext-link"
              >{{ row.value }}</a>
              <span v-else>{{ row.value }}</span>
            </dd>
          </div>
        </dl>
        <JsonBlockPanel v-else :title="objectScalarTitle">
          <dl class="kv-grid">
            <div v-for="row in scalarBlock" :key="row.key" class="kv-row">
              <dt>{{ row.label }}</dt>
              <dd>
                <a
                  v-if="row.isUrl"
                  :href="row.value"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="ext-link"
                >{{ row.value }}</a>
                <span v-else>{{ row.value }}</span>
              </dd>
            </div>
          </dl>
        </JsonBlockPanel>
      </template>
      <JsonBlockPanel
        v-for="entry in complexBlock"
        :key="entry.key"
        :title="fieldLabel(entry.key)"
      >
        <JsonValueRenderer
          :value="entry.value"
          :field-key="entry.key"
          :path="joinPath(path, entry.key)"
          :depth="depth + 1"
          inline
        />
      </JsonBlockPanel>
    </div>
  </template>

  <!-- 独立字段（inline） -->
  <template v-else-if="mode !== 'skip'">
    <JsonBlockPanel v-if="!inline && showFieldTitle(fieldKey, value)" :title="fieldLabel(fieldKey)">
      <JsonValueRenderer
        :value="value"
        :field-key="fieldKey"
        :path="path"
        :depth="depth"
        inline
      />
    </JsonBlockPanel>

    <template v-else>
      <dl v-if="mode === 'scalar'" class="kv-grid kv-grid--inline">
        <div class="kv-row">
          <dt v-if="fieldKey">{{ fieldLabel(fieldKey) }}</dt>
          <dd>{{ formatScalar(value) }}</dd>
        </div>
      </dl>

      <div v-else-if="mode === 'url'" class="link-row">
        <a :href="String(value)" target="_blank" rel="noopener noreferrer" class="ext-link">
          {{ urlLinkText(fieldKey) }}
        </a>
      </div>

      <ul v-else-if="mode === 'string-list'" class="bullet-list">
        <li v-for="(item, idx) in value as string[]" :key="idx">{{ item }}</li>
      </ul>

      <LabeledRowsView v-else-if="mode === 'labeled-rows'" :rows="labeledRows" />

      <JsonEnhancerTable
        v-else-if="isEnhancerTable"
        :rows="enhancerTableRows"
        :columns="enhancer!.columns!"
        :table-class="mode === 'price-tiers-table' ? 'pricing-tier-table' : mode === 'alternatives-table' ? 'alternatives-table' : undefined"
      />

      <el-table
        v-else-if="mode === 'object-table'"
        :data="value as Record<string, unknown>[]"
        size="small"
        stripe
        border
      >
        <el-table-column
          v-for="col in objectTableColumns"
          :key="col"
          :prop="col"
          :label="fieldLabel(col)"
          min-width="100"
        >
          <template #default="{ row }">{{ formatCellValue(row[col]) }}</template>
        </el-table-column>
      </el-table>

      <div v-else-if="mode === 'industry-news-list'" class="json-panel-stack json-panel-stack--nested">
        <JsonBlockPanel v-for="(item, idx) in industryNewsRows" :key="idx">
          <IndustryNewsItemView :item="item" />
        </JsonBlockPanel>
      </div>

      <div v-else-if="mode === 'object-list'" class="json-panel-stack json-panel-stack--nested">
        <JsonBlockPanel
          v-for="(item, idx) in value as Record<string, unknown>[]"
          :key="idx"
          :title="objectListTitle(item, idx)"
          :head-divider="false"
        >
          <JsonValueRenderer
            :value="item"
            :depth="depth + 1"
            inline
            :omit-field-keys="objectListOmitKeys()"
          />
        </JsonBlockPanel>
      </div>

      <ul v-else-if="mode === 'mixed-list'" class="bullet-list">
        <li v-for="(item, idx) in mixedListItems" :key="idx">{{ item }}</li>
      </ul>

      <pre v-else class="json-fallback">{{ JSON.stringify(value, null, 2) }}</pre>
    </template>
  </template>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import JsonBlockPanel from '@/components/RFQ/enhancers/JsonBlockPanel.vue'
import IndustryNewsItemView from '@/components/RFQ/enhancers/IndustryNewsItemView.vue'
import LabeledRowsView from '@/components/RFQ/enhancers/LabeledRowsView.vue'
import JsonEnhancerTable from '@/components/RFQ/enhancers/JsonEnhancerTable.vue'
import {
  formatCellValue,
  formatMixedListItem,
  formatScalar,
  industryNewsItems,
  isPlainObject,
  isScalar,
  isUrlField,
  joinPath,
  objectArrayColumns,
  visibleEntries
} from '@/utils/jsonDisplay'
import {
  resolveFieldLabel,
  resolveSectionLabel,
  resolveUrlLinkText,
  sortRootSectionEntries
} from '@/utils/jsonLabels'
import {
  objectListItemTitle,
  resolveJsonRender,
  type JsonEnhancerDef
} from '@/utils/materialIntelJsonEnhancers'

defineOptions({ name: 'JsonValueRenderer' })

const props = withDefaults(
  defineProps<{
    value: unknown
    fieldKey?: string
    path?: string
    depth?: number
    root?: boolean
    inline?: boolean
    sectionRoot?: boolean
    omitFieldKeys?: string[]
  }>(),
  {
    fieldKey: '',
    path: '',
    depth: 0,
    root: false,
    inline: false,
    sectionRoot: false,
    omitFieldKeys: () => []
  }
)

const { t, te, locale } = useI18n()

const rootEntries = computed(() => {
  if (!isPlainObject(props.value)) return []
  return sortRootSectionEntries(visibleEntries(props.value))
})

const renderCtx = computed(() => ({
  fieldKey: props.fieldKey,
  path: props.path || props.fieldKey,
  value: props.value
}))

const resolved = computed(() => resolveJsonRender(renderCtx.value))

const mode = computed(() => resolved.value.mode)

const enhancer = computed((): JsonEnhancerDef | null => resolved.value.enhancer)

const hiddenFieldKeys = computed(() => new Set(props.omitFieldKeys ?? []))

function isHiddenField(key: string): boolean {
  return hiddenFieldKeys.value.has(key)
}

function objectListOmitKeys(): string[] {
  return enhancer.value?.omitItemFieldKeys ?? enhancer.value?.itemTitleKeys ?? []
}

const isEnhancerTable = computed(
  () =>
    (mode.value === 'breakdown-table' ||
      mode.value === 'price-tiers-table' ||
      mode.value === 'alternatives-table') &&
    Boolean(enhancer.value?.columns?.length)
)

const enhancerTableRows = computed(() => {
  if (!enhancer.value?.tableData) return [] as Record<string, unknown>[]
  return enhancer.value.tableData(props.value)
})

const objectTableColumns = computed(() => {
  if (!Array.isArray(props.value)) return [] as string[]
  const rows = props.value.filter((x) => isPlainObject(x)) as Record<string, unknown>[]
  return objectArrayColumns(rows)
})

const mixedListItems = computed(() => {
  if (!Array.isArray(props.value)) return [] as string[]
  return props.value
    .map((item) => formatMixedListItem(item))
    .filter(Boolean) as string[]
})

const industryNewsRows = computed(() => industryNewsItems(props.value))

const labeledRows = computed(() => {
  if (!isPlainObject(props.value)) return []
  const entries = visibleEntries(props.value).filter(({ key }) => !isHiddenField(key))
  const order = enhancer.value?.rowOrder
  const sorted =
    order && order.length
      ? [
          ...order
            .filter((key) => entries.some((e) => e.key === key))
            .map((key) => ({ key, value: entries.find((e) => e.key === key)!.value })),
          ...entries.filter((e) => !order.includes(e.key))
        ]
      : entries
  return sorted
    .map(({ key, value }) => ({
      key,
      label: fieldLabel(key),
      value: formatScalar(value),
      isUrl: isUrlField(key, value)
    }))
    .filter((row) => row.value)
})

const scalarBlock = computed(() => {
  if (!isPlainObject(props.value)) return []
  return visibleEntries(props.value)
    .filter(({ key, value }) => !isHiddenField(key) && (isScalar(value) || isUrlField(key, value)))
    .map(({ key, value }) => ({
      key,
      label: fieldLabel(key),
      value: formatScalar(value),
      isUrl: isUrlField(key, value)
    }))
})

const complexBlock = computed(() => {
  if (!isPlainObject(props.value)) return []
  return visibleEntries(props.value).filter(
    ({ key, value }) => !isHiddenField(key) && !isScalar(value) && !isUrlField(key, value)
  )
})

const objectScalarTitle = computed(() => {
  // 父级 JsonBlockPanel 已展示标题时，内层 KV 网格不再重复标题行
  if (props.inline) return undefined
  if (!props.fieldKey) return undefined
  return fieldLabel(props.fieldKey)
})

function fieldLabel(key: string): string {
  return resolveFieldLabel(key, t, te, locale.value)
}

function sectionLabel(key: string): string {
  return resolveSectionLabel(key, t, te, locale.value)
}

function urlLinkText(key: string): string {
  return resolveUrlLinkText(key, props.value, t, te, locale.value)
}

function objectListTitle(item: Record<string, unknown>, idx: number): string {
  return objectListItemTitle(item, idx, enhancer.value, fieldLabel(props.fieldKey || 'item'))
}

function showFieldTitle(key: string, value: unknown): boolean {
  if (!key) return false
  if (props.root || props.sectionRoot) return false
  const child = resolveJsonRender({ fieldKey: key, path: joinPath(props.path, key), value })
  return child.mode !== 'scalar' && child.mode !== 'url' && child.mode !== 'skip'
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.json-panel-stack {
  display: flex;
  flex-direction: column;
  gap: 12px;

  &--nested {
    gap: 10px;
  }
}

.json-section-group {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.json-section-group__title {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}

.kv-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 10px 20px;
  margin: 0;

  &--inline {
    display: block;
  }
}

.kv-row {
  dt {
    margin: 0 0 4px;
    font-size: 11px;
    color: $text-muted;
  }

  dd {
    margin: 0;
    font-size: 13px;
    color: $text-primary;
    word-break: break-word;
  }
}

.bullet-list {
  margin: 0;
  padding-left: 1.2rem;
  font-size: 13px;
  color: $text-primary;
  line-height: 1.55;

  li + li {
    margin-top: 4px;
  }
}

.link-row {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.ext-link {
  font-size: 13px;
  color: $cyan-primary;
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}

:deep(.pricing-tier-table),
:deep(.alternatives-table) {
  margin-top: 0;
  width: 100%;

  .el-table__cell .cell {
    white-space: normal;
    word-break: break-word;
    line-height: 1.5;
  }
}

.json-fallback {
  margin: 0;
  padding: 10px 12px;
  font-size: 12px;
  line-height: 1.45;
  color: $text-secondary;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: 8px;
  overflow-x: auto;
  white-space: pre-wrap;
  word-break: break-word;
}
</style>
