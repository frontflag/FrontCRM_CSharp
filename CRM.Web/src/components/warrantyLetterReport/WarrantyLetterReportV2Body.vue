<!-- V2：chrome 对齐 PO V2 / SO 质保书 V2；信笺式无货表；Meta=文件编号+单据日期；单栏签章 -->
<template>
  <div class="po-v2">
    <header class="po-v2__head">
      <div class="po-v2__head-left">
        <div class="po-v2__logo-stack">
          <img v-if="logoUrl" class="po-v2__logo" :src="logoUrl" alt="" />
          <div v-else class="po-v2__logo-fallback">{{ issuerName }}</div>
          <div class="po-v2__tagline">YOUR RELIABLE SUPPLIER</div>
        </div>
      </div>
      <div class="po-v2__head-right">
        <div class="po-v2__wty-title-main">{{ docTitle }}</div>
        <div class="po-v2__wty-title-sub">{{ docSubtitle }}</div>
      </div>
    </header>
    <div class="po-v2__fade" aria-hidden="true" />

    <div class="po-v2__meta">
      <div class="po-v2__meta-cell">
        <div class="po-v2__meta-k">{{ metaLabel.refNo }}</div>
        <div class="po-v2__meta-v">{{ dash(docNo) }}</div>
      </div>
      <div class="po-v2__meta-cell">
        <div class="po-v2__meta-k">{{ metaLabel.documentDate }}</div>
        <div class="po-v2__meta-v">{{ dash(issueDate) }}</div>
      </div>
    </div>

    <section class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        {{ sectionTitle.recipient }}
      </div>
      <div class="po-v2__letter-recipient">
        <div class="po-v2__letter-line">
          <span class="po-v2__letter-lbl">{{ toNameLabel }}</span>{{ vendorName }}
        </div>
        <div v-if="vendorCode" class="po-v2__letter-line">
          <span class="po-v2__letter-lbl">{{ codeLabel }}</span>{{ vendorCode }}
        </div>
        <div class="po-v2__letter-line">
          <span class="po-v2__letter-lbl">{{ addrLabel }}</span>{{ vendorAddress }}
        </div>
      </div>
    </section>

    <section class="po-v2__wty-prose">
      <p
        v-for="(p, i) in paragraphs"
        :key="i"
        class="po-v2__wty-intro"
        :class="{ 'po-v2__wty-intro--zh': lang === 'zh' }"
      >
        {{ p }}
      </p>
    </section>

    <section class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        {{ sectionTitle.sign }}
      </div>
      <div class="po-v2__sign po-v2__sign--single">
        <div class="po-v2__sign-box">
          <div class="po-v2__sign-t">{{ issuerSignLabel }}</div>
          <div class="po-v2__sign-pad">
            <img v-if="showSeal && sealUrl" class="po-v2__seal" :src="sealUrl" alt="" />
          </div>
          <div class="po-v2__sign-date">{{ dateLabel }} {{ issueDate }}</div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import {
  warrantyLetterReportDocumentPropDefaults,
  type WarrantyLetterReportDocumentProps
} from './types'

const props = withDefaults(
  defineProps<WarrantyLetterReportDocumentProps>(),
  warrantyLetterReportDocumentPropDefaults
)

const SECTION_TITLE_ZH = {
  recipient: '收件方',
  sign: '签章确认'
} as const

const SECTION_TITLE_EN = {
  recipient: 'Recipient',
  sign: 'Signatures'
} as const

const META_LABEL_ZH = {
  refNo: '文件编号',
  documentDate: '单据日期'
} as const

const META_LABEL_EN = {
  refNo: 'Ref. No.',
  documentDate: 'Document Date'
} as const

const lang = computed(() => props.lang ?? 'zh')
const sectionTitle = computed(() => (lang.value === 'zh' ? SECTION_TITLE_ZH : SECTION_TITLE_EN))
const metaLabel = computed(() => (lang.value === 'zh' ? META_LABEL_ZH : META_LABEL_EN))

function dash(v?: string | null) {
  const s = (v ?? '').trim()
  return s || '—'
}
</script>

<style lang="scss">
.po-doc--letter-wty-v2 {
  --po-v2-navy: #090e1d;
  --po-v2-accent: #00d2ef;
  --po-v2-line: #05e5ff;
  --po-v2-head-fg: #fff;
  --po-v2-ink: #1a1d22;
  --po-v2-muted: #6b7280;
  --po-v2-border: #d8e1e6;
  --po-v2-wash: #f5f7fa;
  --po-v2-row: #f4f9fc;

  width: 210mm;
  min-height: 297mm;
  margin: 0 auto;
  padding: 0 10mm 8mm;
  box-sizing: border-box;
  background: #fff;
  color: var(--po-v2-ink);
  font-size: 8.5pt;
  line-height: 1.4;
  font-family: Arial, Helvetica, 'Microsoft YaHei', sans-serif;

  .po-v2__head {
    position: relative;
    display: flex;
    justify-content: space-between;
    align-items: flex-end;
    gap: 8mm;
    margin: 0 -10mm 0;
    padding: 5.2mm 10mm 4.6mm;
    overflow: hidden;
    background: var(--po-v2-navy);
    color: var(--po-v2-head-fg);
  }

  .po-v2__head-left {
    position: relative;
    z-index: 1;
    display: flex;
    flex-direction: column;
    justify-content: flex-end;
    align-items: flex-start;
    min-width: 0;
  }

  .po-v2__logo-stack {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1.4mm;
    width: max-content;
    max-width: 100%;
  }

  .po-v2__logo {
    max-height: 12mm;
    max-width: 42mm;
    object-fit: contain;
    display: block;
  }

  .po-v2__logo-fallback {
    font-size: 15pt;
    font-weight: 800;
    letter-spacing: 0.14em;
    color: var(--po-v2-accent);
  }

  .po-v2__tagline {
    font-size: 6pt;
    letter-spacing: 0.24em;
    color: rgba(255, 255, 255, 0.78);
    text-transform: uppercase;
    text-align: center;
    width: 100%;
    white-space: nowrap;
  }

  .po-v2__head-right {
    position: relative;
    z-index: 1;
    text-align: right;
    flex: 0 0 88mm;
  }

  .po-v2__head-right::before {
    content: '';
    position: absolute;
    top: -10mm;
    right: -12mm;
    bottom: -8mm;
    width: 82mm;
    pointer-events: none;
    opacity: 0.55;
    background-image:
      repeating-linear-gradient(45deg, transparent 0 8px, rgba(5, 229, 255, 0.22) 8px 8.7px),
      repeating-linear-gradient(-45deg, transparent 0 8px, rgba(5, 229, 255, 0.22) 8px 8.7px);
    mask-image: linear-gradient(90deg, transparent 0%, #000 42%);
    -webkit-mask-image: linear-gradient(90deg, transparent 0%, #000 42%);
  }

  .po-v2__wty-title-main {
    position: relative;
    font-size: 16pt;
    font-weight: 800;
    letter-spacing: 0.12em;
    line-height: 1.05;
    color: #fff;
    font-family: 'Microsoft YaHei', Arial, sans-serif;
  }

  .po-v2__wty-title-sub {
    position: relative;
    margin-top: 1mm;
    font-size: 6.5pt;
    font-weight: 700;
    letter-spacing: 0.2em;
    text-transform: uppercase;
    color: var(--po-v2-accent);
    line-height: 1.15;
  }

  .po-v2__fade {
    height: 1.6px;
    margin: 0 0 2.8mm;
    background: linear-gradient(90deg, #05e5ff 0%, #53edff 28%, #c9f8ff 62%, transparent 100%);
  }

  .po-v2__head + .po-v2__fade {
    margin-top: 10px;
    height: 3.2px;
  }

  .po-v2__meta {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    background: var(--po-v2-wash);
    border: 1px solid var(--po-v2-border);
    margin-bottom: 3.6mm;
  }

  .po-v2__meta-cell {
    padding: 2mm 2.8mm 2.2mm;
    border-right: 1px solid var(--po-v2-border);
    min-width: 0;
  }

  .po-v2__meta-cell:last-child {
    border-right: none;
  }

  .po-v2__meta-k {
    font-size: 6.5pt;
    font-weight: 700;
    color: #4b5563;
    letter-spacing: 0.04em;
    margin-bottom: 1mm;
  }

  .po-v2__meta-v {
    font-size: 9pt;
    font-weight: 400;
    color: #4b5563;
    word-break: break-all;
  }

  .po-v2__block {
    margin-bottom: 3.2mm;
  }

  .po-v2__sec-hd {
    display: flex;
    align-items: center;
    gap: 2mm;
    font-weight: 700;
    font-size: 9pt;
    color: var(--po-v2-ink);
    margin-bottom: 1.2mm;
  }

  .po-v2__guide {
    display: inline-block;
    width: 2.4mm;
    height: 3.6mm;
    background: var(--po-v2-line);
    flex-shrink: 0;
  }

  .po-v2__letter-recipient {
    border: 1px solid var(--po-v2-border);
    border-top: 2.4px solid var(--po-v2-line);
    padding: 2.4mm 3mm 2.6mm;
    background: var(--po-v2-row);
    font-size: 7.8pt;
    line-height: 1.5;
  }

  .po-v2__letter-line {
    margin-bottom: 1.2mm;
    word-break: break-word;
  }

  .po-v2__letter-line:last-child {
    margin-bottom: 0;
  }

  .po-v2__letter-lbl {
    font-weight: 700;
    color: var(--po-v2-ink);
  }

  .po-v2__wty-prose {
    margin-bottom: 3.2mm;
  }

  .po-v2__wty-intro {
    margin: 0 0 3mm;
    text-align: justify;
    font-size: 7.8pt;
  }

  .po-v2__wty-intro--zh {
    text-indent: 2em;
  }

  .po-v2__wty-intro:last-child {
    margin-bottom: 0;
  }

  .po-v2__sign--single {
    display: block;
    max-width: 80mm;
  }

  .po-v2__sign-box {
    border: 1px dashed #b7c0c8;
    padding: 2.6mm 3.2mm 3mm;
    min-height: 36mm;
  }

  .po-v2__sign-t {
    font-weight: 800;
    font-size: 9pt;
    margin-bottom: 1.4mm;
  }

  .po-v2__sign-pad {
    min-height: 22mm;
    position: relative;
    margin-top: 1mm;
  }

  .po-v2__seal {
    max-height: 22mm;
    max-width: 22mm;
    object-fit: contain;
    position: absolute;
    left: 0;
    bottom: 0;
  }

  .po-v2__sign-date {
    font-size: 7.8pt;
    margin-top: 1mm;
  }

  @media print {
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }
}

@media print {
  .po-doc--letter-wty-v2 {
    width: auto;
    min-height: auto;
    margin: 0;
    padding: 0 10mm 8mm;

    .po-v2__head {
      margin: 0 -10mm 0;
    }
  }
}
</style>
