<!-- V2：chrome 对齐 PO V2；业务字段与 V1 一致；Meta 两格；分区标题随路由语言 -->
<template>
  <div class="po-v2">
    <header class="po-v2__head">
      <div class="po-v2__head-left">
        <div class="po-v2__logo-stack">
          <img v-if="logoUrl" class="po-v2__logo" :src="logoUrl" alt="" />
          <div v-else class="po-v2__logo-fallback">{{ partyAName }}</div>
          <div class="po-v2__tagline">YOUR RELIABLE SUPPLIER</div>
        </div>
      </div>
      <div class="po-v2__head-right">
        <div class="po-v2__wty-title-main">{{ docTitle }}</div>
        <div class="po-v2__wty-title-sub">{{ docSubtitle }}</div>
      </div>
    </header>
    <div class="po-v2__fade" aria-hidden="true" />

    <div class="po-v2__meta po-v2__meta--wty">
      <div class="po-v2__meta-cell">
        <div class="po-v2__meta-k">{{ metaLabel.documentDate }}</div>
        <div class="po-v2__meta-v">{{ dash(orderDate) }}</div>
      </div>
      <div class="po-v2__meta-cell">
        <div class="po-v2__meta-k">{{ metaLabel.orderNo }}</div>
        <div class="po-v2__meta-v">{{ dash(orderCode) }}</div>
      </div>
    </div>

    <section class="po-v2__block po-v2__wty-parties">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        {{ sectionTitle.parties }}
      </div>
      <div class="po-v2__parties">
        <div class="po-v2__party">
          <div class="po-v2__party-role">{{ partyRole(partyALabel) }}</div>
          <div class="po-v2__party-body">
            <div class="po-v2__party-line">{{ partyAName }}</div>
          </div>
        </div>
        <div class="po-v2__party">
          <div class="po-v2__party-role">{{ partyRole(partyBLabel) }}</div>
          <div class="po-v2__party-body">
            <div class="po-v2__party-line">{{ partyBName }}</div>
          </div>
        </div>
      </div>
    </section>

    <section class="po-v2__wty-prose">
      <p class="po-v2__wty-intro" :class="{ 'po-v2__wty-intro--zh': lang === 'zh' }">{{ introText }}</p>
      <div v-if="notesHeading" class="po-v2__wty-notes-hd">{{ notesHeading }}</div>
      <ol v-if="notes.length" class="po-v2__wty-notes-list">
        <li v-for="(n, i) in notes" :key="i">{{ n }}</li>
      </ol>
      <p v-if="notesAfter" class="po-v2__wty-notes-after">{{ notesAfter }}</p>
      <div v-if="goodsLead" class="po-v2__wty-notes-hd">{{ goodsLead }}</div>
    </section>

    <section class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        {{ sectionTitle.goods }}
      </div>
      <table class="po-v2__grid po-v2__grid--wty6">
        <colgroup>
          <col class="c-wty-pn" />
          <col class="c-wty-brand" />
          <col class="c-wty-qty" />
          <col class="c-wty-dc" />
          <col class="c-wty-cpn" />
          <col class="c-wty-cso" />
        </colgroup>
        <thead>
          <tr>
            <th>{{ colPn }}</th>
            <th>{{ colBrand }}</th>
            <th>{{ colQty }}</th>
            <th>{{ colDc }}</th>
            <th>{{ colCustomerPn }}</th>
            <th>{{ colCustomerSo }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(line, i) in lines" :key="'l' + i">
            <td>{{ line.pn }}</td>
            <td class="po-v2__brand">{{ line.brand }}</td>
            <td class="cen">{{ line.qty }}</td>
            <td class="cen">{{ line.dateCode }}</td>
            <td>{{ line.customerPn }}</td>
            <td>{{ line.customerSo }}</td>
          </tr>
          <tr v-if="lines.length === 0">
            <td colspan="6" class="po-v2__empty">{{ emptyLinesHint }}</td>
          </tr>
        </tbody>
      </table>
    </section>

    <section class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        {{ sectionTitle.sign }}
      </div>
      <div class="po-v2__sign">
        <div class="po-v2__sign-box">
          <div class="po-v2__sign-t">{{ partyALabel }}{{ partyAName }}</div>
          <div class="po-v2__sign-field">
            <span class="po-v2__sign-lbl">{{ signRepLabel }}</span>{{ partyARep }}
          </div>
          <div class="po-v2__sign-field">
            <span class="po-v2__sign-lbl">{{ signPhoneLabel }}</span>{{ partyAPhone }}
          </div>
          <div class="po-v2__sign-field">
            <span class="po-v2__sign-lbl">{{ signAddrLabel }}</span>{{ partyAAddress }}
          </div>
          <div class="po-v2__sign-pad">
            <img v-if="showSeal && sealUrl" class="po-v2__seal" :src="sealUrl" alt="" />
          </div>
        </div>
        <div class="po-v2__sign-box">
          <div class="po-v2__sign-t">{{ partyBLabel }}{{ partyBName }}</div>
          <div class="po-v2__sign-field">
            <span class="po-v2__sign-lbl">{{ signRepLabel }}</span>{{ partyBRep }}
          </div>
          <div class="po-v2__sign-field">
            <span class="po-v2__sign-lbl">{{ signPhoneLabel }}</span>{{ partyBPhone }}
          </div>
          <div class="po-v2__sign-field">
            <span class="po-v2__sign-lbl">{{ signAddrLabel }}</span>{{ partyBAddress }}
          </div>
          <div class="po-v2__sign-pad"></div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import {
  salesOrderWarrantyReportDocumentPropDefaults,
  type SalesOrderWarrantyReportDocumentProps
} from './types'

const props = withDefaults(
  defineProps<SalesOrderWarrantyReportDocumentProps>(),
  salesOrderWarrantyReportDocumentPropDefaults
)

const SECTION_TITLE_ZH = {
  parties: '交易方',
  goods: '货物明细',
  sign: '签章确认'
} as const

const SECTION_TITLE_EN = {
  parties: 'Parties',
  goods: 'Goods Information',
  sign: 'Signatures'
} as const

const META_LABEL_ZH = {
  documentDate: '单据日期',
  orderNo: '销售订单号'
} as const

const META_LABEL_EN = {
  documentDate: 'Document Date',
  orderNo: 'Sales Order No.'
} as const

const sectionTitle = computed(() => (props.lang === 'zh' ? SECTION_TITLE_ZH : SECTION_TITLE_EN))
const metaLabel = computed(() => (props.lang === 'zh' ? META_LABEL_ZH : META_LABEL_EN))

function dash(v?: string | null) {
  const s = (v ?? '').trim()
  return s || '—'
}

function partyRole(label: string) {
  return label.replace(/[：:]\s*$/, '').trim()
}
</script>

<style lang="scss">
.po-doc--so-wty-v2 {
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

  .po-v2__wty-parties {
    margin-top: 0;
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

  .po-v2__parties {
    display: grid;
    grid-template-columns: 1fr 1fr;
  }

  .po-v2__party {
    border: 1px solid var(--po-v2-border);
    border-top: 2.4px solid var(--po-v2-line);
  }

  .po-v2__party + .po-v2__party {
    border-left: none;
  }

  .po-v2__party-role {
    padding: 1.4mm 3mm;
    background: var(--po-v2-wash);
    font-size: 8pt;
    font-weight: 700;
    border-bottom: 1px solid var(--po-v2-border);
  }

  .po-v2__party-body {
    padding: 2mm 3mm 2.2mm;
    font-size: 7.8pt;
    line-height: 1.45;
  }

  .po-v2__party-line {
    word-break: break-word;
  }

  .po-v2__wty-prose {
    margin-bottom: 3.2mm;
  }

  .po-v2__wty-intro {
    margin: 0 0 3mm;
    text-align: justify;
  }

  .po-v2__wty-intro--zh {
    text-indent: 2em;
  }

  .po-v2__wty-notes-hd {
    font-weight: 700;
    font-size: 8pt;
    margin-bottom: 1.4mm;
  }

  .po-v2__wty-notes-list {
    margin: 0 0 3mm;
    padding-left: 1.6em;
    font-size: 7.8pt;
  }

  .po-v2__wty-notes-list li {
    margin-bottom: 1mm;
    text-align: justify;
  }

  .po-v2__wty-notes-after {
    margin: 0 0 2.4mm;
    text-align: justify;
    font-size: 7.8pt;
  }

  .po-v2__grid {
    width: 100%;
    border-collapse: collapse;
    table-layout: fixed;
    font-size: 7.4pt;
    border: 1px solid var(--po-v2-border);
  }

  .po-v2__grid--wty6 .c-wty-pn {
    width: 18%;
  }
  .po-v2__grid--wty6 .c-wty-brand {
    width: 14%;
  }
  .po-v2__grid--wty6 .c-wty-qty {
    width: 12%;
  }
  .po-v2__grid--wty6 .c-wty-dc {
    width: 14%;
  }
  .po-v2__grid--wty6 .c-wty-cpn {
    width: 20%;
  }
  .po-v2__grid--wty6 .c-wty-cso {
    width: 22%;
  }

  .po-v2__grid th,
  .po-v2__grid td {
    border: 1px solid var(--po-v2-border);
    padding: 2.6px 4px;
    vertical-align: middle;
    word-break: break-word;
  }

  .po-v2__grid thead th {
    background: var(--po-v2-navy);
    color: #fff;
    font-weight: 700;
    text-align: center;
    line-height: 2.1;
    border-left-color: var(--po-v2-navy);
    border-right-color: var(--po-v2-navy);
  }

  .po-v2__grid tbody td {
    background: var(--po-v2-row);
    line-height: 2.1;
  }

  .po-v2__brand {
    white-space: nowrap;
  }

  .po-v2__grid .cen {
    text-align: center;
  }

  .po-v2__empty {
    text-align: center;
    color: var(--po-v2-muted);
    padding: 5mm 0 !important;
    background: #fff !important;
  }

  .po-v2__sign {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 4mm;
    margin-top: 1mm;
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

  .po-v2__sign-field {
    font-size: 7.8pt;
    margin-bottom: 0.8mm;
    word-break: break-word;
  }

  .po-v2__sign-lbl {
    font-weight: 700;
  }

  .po-v2__sign-pad {
    min-height: 14mm;
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

  @media print {
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }
}

@media print {
  .po-doc--so-wty-v2 {
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
