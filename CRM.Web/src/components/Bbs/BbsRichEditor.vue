<template>
  <div class="bbs-rich">
    <div ref="toolbarRef" class="bbs-rich__toolbar">
      <select class="ql-font" :title="t('bbs.richToolbar.font')">
        <option selected value="">默认字体</option>
        <option value="microsoft-yahei">微软雅黑</option>
        <option value="simsun">宋体</option>
        <option value="simhei">黑体</option>
        <option value="kaiti">楷体</option>
        <option value="fangsong">仿宋</option>
        <option value="serif">Serif</option>
        <option value="monospace">等宽</option>
      </select>
      <select class="ql-size" :title="t('bbs.richToolbar.size')">
        <option value="12px">12</option>
        <option selected value="14px">14</option>
        <option value="16px">16</option>
        <option value="18px">18</option>
        <option value="20px">20</option>
        <option value="24px">24</option>
        <option value="28px">28</option>
        <option value="32px">32</option>
      </select>
      <button type="button" class="ql-bold" :title="t('bbs.richToolbar.bold')" />
      <button type="button" class="ql-italic" :title="t('bbs.richToolbar.italic')" />
      <button type="button" class="ql-underline" :title="t('bbs.richToolbar.underline')" />
      <button type="button" class="ql-strike" :title="t('bbs.richToolbar.strike')" />
      <select class="ql-color" :title="t('bbs.richToolbar.color')" />
      <select class="ql-background" :title="t('bbs.richToolbar.background')">
        <option selected value="">去除底色</option>
        <option value="#000000" />
        <option value="#e60000" />
        <option value="#ff9900" />
        <option value="#ffff00" />
        <option value="#008a00" />
        <option value="#0066cc" />
        <option value="#9933ff" />
        <option value="#ffffff" />
        <option value="#facccc" />
        <option value="#ffebcc" />
        <option value="#ffffcc" />
        <option value="#cce8cc" />
        <option value="#cce0f5" />
        <option value="#ebd6ff" />
        <option value="#bbbbbb" />
        <option value="#f06666" />
        <option value="#ffc266" />
        <option value="#ffff66" />
        <option value="#66b966" />
        <option value="#66a3e0" />
        <option value="#c285ff" />
        <option value="#888888" />
        <option value="#a10000" />
        <option value="#b26b00" />
        <option value="#b2b200" />
        <option value="#006100" />
        <option value="#0047b2" />
        <option value="#6b24b2" />
        <option value="#444444" />
        <option value="#5c0000" />
        <option value="#663d00" />
        <option value="#666600" />
        <option value="#003700" />
        <option value="#002966" />
        <option value="#3d1466" />
      </select>
      <button type="button" class="ql-list" value="ordered" :title="t('bbs.richToolbar.listOrdered')" />
      <button type="button" class="ql-list" value="bullet" :title="t('bbs.richToolbar.listBullet')" />
      <select class="ql-align" :title="t('bbs.richToolbar.align')">
        <option selected />
        <option value="center" />
        <option value="right" />
        <option value="justify" />
      </select>
      <button type="button" class="ql-link" :title="t('bbs.richToolbar.link')" />
      <button type="button" class="ql-clean" :title="t('bbs.richToolbar.clean')" />
    </div>
    <div ref="editorRef" class="bbs-rich__editor" />
  </div>
</template>

<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import Quill from 'quill'
import { StyleAttributor, Scope } from 'parchment'
import 'quill/dist/quill.snow.css'

const props = defineProps<{
  modelValue: string
  placeholder?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const { t } = useI18n()
const toolbarRef = ref<HTMLElement | null>(null)
const editorRef = ref<HTMLElement | null>(null)
let quill: Quill | null = null
let syncing = false

const FontStyle = new StyleAttributor('font', 'font-family', {
  scope: Scope.INLINE,
  whitelist: ['microsoft-yahei', 'simsun', 'simhei', 'kaiti', 'fangsong', 'serif', 'monospace']
})

const SizeStyle = new StyleAttributor('size', 'font-size', {
  scope: Scope.INLINE,
  whitelist: ['12px', '14px', '16px', '18px', '20px', '24px', '28px', '32px']
})

/** Map toolbar values to CSS font-family */
const FONT_CSS: Record<string, string> = {
  'microsoft-yahei': 'Microsoft YaHei, 微软雅黑, sans-serif',
  simsun: 'SimSun, 宋体, serif',
  simhei: 'SimHei, 黑体, sans-serif',
  kaiti: 'KaiTi, 楷体, serif',
  fangsong: 'FangSong, 仿宋, serif',
  serif: 'serif',
  monospace: 'ui-monospace, monospace'
}

let formatsRegistered = false
function ensureFormats() {
  if (formatsRegistered) return
  Quill.register(FontStyle, true)
  Quill.register(SizeStyle, true)
  formatsRegistered = true
}

function normalizeFontHtml(html: string): string {
  let out = html
  for (const [key, css] of Object.entries(FONT_CSS)) {
    const re = new RegExp(`font-family:\\s*${key}\\b`, 'gi')
    out = out.replace(re, `font-family: ${css}`)
  }
  return out
}

function getHtml(): string {
  if (!quill) return ''
  const html = quill.root.innerHTML || ''
  if (html === '<p><br></p>' || html === '<p></p>') return ''
  return normalizeFontHtml(html)
}

function setHtml(html: string) {
  if (!quill) return
  syncing = true
  const next = html?.trim() ? html : ''
  quill.setText('')
  if (next) {
    quill.clipboard.dangerouslyPasteHTML(0, next)
  }
  syncing = false
}

function insertHtml(html: string) {
  if (!quill) return
  const range = quill.getSelection(true)
  const index = range ? range.index : quill.getLength()
  quill.clipboard.dangerouslyPasteHTML(index, html)
  quill.setSelection(index + 1, 0)
}

function focus() {
  quill?.focus()
}

/** Quill 会把 select 包成 picker；把 tip 同步到可见控件上 */
function applyToolbarTips() {
  const root = toolbarRef.value
  if (!root) return
  const map: Array<[string, string]> = [
    ['.ql-font', 'bbs.richToolbar.font'],
    ['.ql-size', 'bbs.richToolbar.size'],
    ['.ql-bold', 'bbs.richToolbar.bold'],
    ['.ql-italic', 'bbs.richToolbar.italic'],
    ['.ql-underline', 'bbs.richToolbar.underline'],
    ['.ql-strike', 'bbs.richToolbar.strike'],
    ['.ql-color', 'bbs.richToolbar.color'],
    ['.ql-background', 'bbs.richToolbar.background'],
    ['.ql-list[value="ordered"]', 'bbs.richToolbar.listOrdered'],
    ['.ql-list[value="bullet"]', 'bbs.richToolbar.listBullet'],
    ['.ql-align', 'bbs.richToolbar.align'],
    ['.ql-link', 'bbs.richToolbar.link'],
    ['.ql-clean', 'bbs.richToolbar.clean']
  ]
  for (const [sel, key] of map) {
    const tip = t(key)
    root.querySelectorAll(sel).forEach((el) => {
      el.setAttribute('title', tip)
      const label = el.querySelector?.('.ql-picker-label')
      if (label) label.setAttribute('title', tip)
    })
  }
}

onMounted(() => {
  if (!editorRef.value || !toolbarRef.value) return
  ensureFormats()
  quill = new Quill(editorRef.value, {
    theme: 'snow',
    placeholder: props.placeholder || '',
    modules: {
      toolbar: toolbarRef.value
    }
  })

  // Apply font-family CSS when selecting Chinese fonts from toolbar
  const toolbar = quill.getModule('toolbar') as { addHandler?: (name: string, fn: (...a: unknown[]) => void) => void }
  toolbar?.addHandler?.('font', (value: unknown) => {
    if (!quill) return
    if (!value) {
      quill.format('font', false)
      return
    }
    quill.format('font', String(value))
  })

  if (props.modelValue) setHtml(props.modelValue)

  quill.on('text-change', () => {
    if (syncing) return
    emit('update:modelValue', getHtml())
  })

  void nextTick(() => applyToolbarTips())
})

watch(
  () => props.modelValue,
  (v) => {
    if (!quill || syncing) return
    const cur = getHtml()
    if ((v || '') === cur) return
    setHtml(v || '')
  }
)

onBeforeUnmount(() => {
  quill = null
})

defineExpose({ insertHtml, focus, getHtml })
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.bbs-rich {
  display: flex;
  flex-direction: column;
  min-height: 360px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 8px;
  overflow: hidden;
  background: rgba(255, 255, 255, 0.02);
}

.bbs-rich__toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px;
  padding: 8px 10px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(0, 0, 0, 0.2);

  :deep(button),
  :deep(.ql-picker) {
    color: $text-secondary;
  }

  :deep(.ql-stroke) {
    stroke: $text-secondary;
  }

  :deep(.ql-fill) {
    fill: $text-secondary;
  }

  :deep(.ql-picker-label) {
    color: $text-secondary;
  }

  :deep(.ql-active .ql-stroke),
  :deep(button:hover .ql-stroke) {
    stroke: $cyan-primary;
  }

  :deep(.ql-active .ql-fill),
  :deep(button:hover .ql-fill) {
    fill: $cyan-primary;
  }
}

.bbs-rich__editor {
  flex: 1;
  min-height: 320px;

  :deep(.ql-editor) {
    min-height: 320px;
    color: $text-primary;
    font-size: 14px;
    line-height: 1.7;
  }

  :deep(.ql-editor.ql-blank::before) {
    color: $text-muted;
    font-style: normal;
  }

  :deep(img),
  :deep(video) {
    max-width: 100%;
  }
}

/* Quill mounts toolbar outside scoped root sometimes — keep snow toolbar usable */
:deep(.ql-toolbar.ql-snow) {
  border: none;
}

:deep(.ql-container.ql-snow) {
  border: none;
  font-size: 14px;
}
</style>

<style lang="scss">
/* Quill font picker labels (global — Quill injects picker into body-ish toolbar) */
.bbs-rich .ql-snow .ql-picker.ql-font .ql-picker-label[data-value='microsoft-yahei']::before,
.bbs-rich .ql-snow .ql-picker.ql-font .ql-picker-item[data-value='microsoft-yahei']::before {
  content: '微软雅黑';
}
.bbs-rich .ql-snow .ql-picker.ql-font .ql-picker-label[data-value='simsun']::before,
.bbs-rich .ql-snow .ql-picker.ql-font .ql-picker-item[data-value='simsun']::before {
  content: '宋体';
}
.bbs-rich .ql-snow .ql-picker.ql-font .ql-picker-label[data-value='simhei']::before,
.bbs-rich .ql-snow .ql-picker.ql-font .ql-picker-item[data-value='simhei']::before {
  content: '黑体';
}
.bbs-rich .ql-snow .ql-picker.ql-font .ql-picker-label[data-value='kaiti']::before,
.bbs-rich .ql-snow .ql-picker.ql-font .ql-picker-item[data-value='kaiti']::before {
  content: '楷体';
}
.bbs-rich .ql-snow .ql-picker.ql-font .ql-picker-label[data-value='fangsong']::before,
.bbs-rich .ql-snow .ql-picker.ql-font .ql-picker-item[data-value='fangsong']::before {
  content: '仿宋';
}
.bbs-rich .ql-snow .ql-picker.ql-size .ql-picker-label::before,
.bbs-rich .ql-snow .ql-picker.ql-size .ql-picker-item::before {
  content: attr(data-value);
}
.bbs-rich .ql-snow .ql-picker.ql-size .ql-picker-label[data-value='14px']::before,
.bbs-rich .ql-snow .ql-picker.ql-size .ql-picker-item[data-value='14px']::before {
  content: '14';
}

/* 底色：去除底色（空 value）显示为文字条 */
.bbs-rich .ql-snow .ql-color-picker.ql-background .ql-picker-item:not([data-value]) {
  width: auto;
  min-width: calc(100% - 8px);
  height: 24px;
  margin: 2px 4px 6px;
  border: 1px dashed rgba(255, 255, 255, 0.35);
  background: transparent !important;
  background-image: none !important;
  float: none;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  color: #c9d1d9;
}

.bbs-rich .ql-snow .ql-color-picker.ql-background .ql-picker-item:not([data-value])::before {
  content: attr(data-label);
}

.bbs-rich .ql-snow .ql-color-picker.ql-background .ql-picker-options {
  padding-top: 4px;
  width: 152px;
}
</style>
