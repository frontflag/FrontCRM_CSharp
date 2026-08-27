<!-- V2：藏青 chrome 对齐装箱/PO V2；Bill To/Ship To 多行文本；明细 7 列与 V1 一致 -->
<template>
  <div class="po-v2">
    <header class="po-v2__head">
      <div class="po-v2__head-left">
        <div class="po-v2__logo-stack">
          <img v-if="logoUrl" class="po-v2__logo" :src="logoUrl" alt="" />
          <div v-else class="po-v2__logo-fallback">{{ headerCompanyName }}</div>
          <div class="po-v2__tagline">YOUR RELIABLE SUPPLIER</div>
        </div>
      </div>
      <div class="po-v2__head-right">
        <div class="po-v2__title-zh">发票</div>
        <div class="po-v2__title-en">INVOICE</div>
        <div class="po-v2__po-no">发票号码 / Invoice No. {{ dash(invoiceNo) }}</div>
      </div>
    </header>
    <div class="po-v2__fade" aria-hidden="true" />

    <div class="po-v2__meta po-v2__meta--inv">
      <div class="po-v2__meta-cell">
        <div class="po-v2__meta-k">单据日期 / DOCUMENT DATE</div>
        <div class="po-v2__meta-v">{{ dash(invoiceDate) }}</div>
      </div>
      <div class="po-v2__meta-cell">
        <div class="po-v2__meta-k">发票号码 / INVOICE NO.</div>
        <div class="po-v2__meta-v">{{ dash(invoiceNo) }}</div>
      </div>
    </div>

    <section class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        {{ sectionTitle.billShip }}
      </div>
      <div class="po-v2__parties">
        <div class="po-v2__party">
          <div class="po-v2__party-role">{{ labels.billTo }}</div>
          <div class="po-v2__addr-body">
            <div v-for="(t, i) in billToLines" :key="'bt' + i" class="po-v2__addr-line">{{ t }}</div>
            <div v-if="!billToLines.length" class="po-v2__addr-line">—</div>
          </div>
        </div>
        <div class="po-v2__party">
          <div class="po-v2__party-role">{{ labels.shipTo }}</div>
          <div class="po-v2__addr-body">
            <div v-for="(t, i) in shipToLines" :key="'st' + i" class="po-v2__addr-line">{{ t }}</div>
            <div v-if="!shipToLines.length" class="po-v2__addr-line">—</div>
          </div>
        </div>
      </div>
    </section>

    <section class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        {{ sectionTitle.invoiceDetails }}
      </div>
      <table class="po-v2__grid">
        <colgroup>
          <col class="c-inv-idx" />
          <col class="c-inv-pn" />
          <col class="c-inv-brand" />
          <col class="c-inv-qty" />
          <col class="c-inv-price" />
          <col class="c-inv-amt" />
          <col class="c-inv-rmk" />
        </colgroup>
        <thead>
          <tr>
            <th>{{ tableHead.no }}</th>
            <th>{{ tableHead.pn }}</th>
            <th>{{ tableHead.brand }}</th>
            <th>{{ tableHead.qty }}</th>
            <th>{{ tableHead.upUsd }}</th>
            <th>{{ tableHead.amountUsd }}</th>
            <th>{{ tableHead.remark }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="line in lines" :key="'l' + line.index">
            <td class="cen">{{ line.index }}</td>
            <td class="po-v2__mpn">
              <div>{{ dash(line.pn) }}</div>
              <div v-if="line.customerPn" class="po-v2__cell-sub">{{ line.customerPn }}</div>
            </td>
            <td class="po-v2__brand">
              <div>{{ dash(line.brand) }}</div>
              <div v-if="line.customerBrand" class="po-v2__cell-sub">{{ line.customerBrand }}</div>
            </td>
            <td class="num">{{ dash(line.qty) }}</td>
            <td class="num">{{ showAmounts ? dash(line.unitPrice) : '—' }}</td>
            <td class="num">{{ showAmounts ? dash(line.amount) : '—' }}</td>
            <td>{{ dash(line.remark) }}</td>
          </tr>
          <tr v-for="i in fillerRowCount" :key="'f' + i">
            <td v-for="c in 7" :key="`${i}-${c}`">&nbsp;</td>
          </tr>
          <tr v-if="lines.length === 0">
            <td colspan="7" class="po-v2__empty">{{ labels.noItems }}</td>
          </tr>
          <tr v-if="lines.length > 0" class="po-v2__sum-row">
            <td>{{ labels.total }}</td>
            <td colspan="2"></td>
            <td class="num">{{ totalQty }}</td>
            <td></td>
            <td class="num">{{ showAmounts ? totalAmount : '—' }}</td>
            <td></td>
          </tr>
        </tbody>
      </table>
    </section>

    <section class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        {{ sectionTitle.bankDetails }}
      </div>
      <div class="po-v2__bank">
        <div v-for="(t, i) in bankLines" :key="'b' + i" class="po-v2__bank-line">{{ t }}</div>
      </div>
    </section>

    <section class="po-v2__sign">
      <div class="po-v2__sign-box">
        <div class="po-v2__sign-t">{{ labels.exporterSign }}</div>
        <div class="po-v2__sign-pad">
          <img v-if="showSeal && sealUrl" class="po-v2__seal" :src="sealUrl" alt="" />
        </div>
        <div class="po-v2__sign-foot">{{ labels.date }}{{ signDate }}</div>
      </div>
      <div class="po-v2__sign-box">
        <div class="po-v2__sign-t">{{ labels.consigneeSign }}</div>
        <div class="po-v2__sign-pad"></div>
        <div class="po-v2__sign-foot">{{ labels.date }}</div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import {
  invoiceReportDocumentPropDefaults,
  invoiceReportFillerRowCount,
  type InvoiceReportDocumentProps
} from './types'

const props = withDefaults(defineProps<InvoiceReportDocumentProps>(), invoiceReportDocumentPropDefaults)

const fillerRowCount = computed(() => invoiceReportFillerRowCount(props.lines.length))

const TABLE_HEAD_ZH = {
  no: '序号',
  pn: '料号',
  brand: '厂牌',
  qty: '数量',
  upUsd: '单价（USD）',
  amountUsd: '金额（USD）',
  remark: '备注'
} as const

const TABLE_HEAD_EN = {
  no: 'No.',
  pn: 'PN',
  brand: 'Brand',
  qty: 'Qty',
  upUsd: 'UP（USD）',
  amountUsd: 'Amount（USD）',
  remark: 'Remark'
} as const

const tableHead = computed(() => (props.reportLang === 'zh' ? TABLE_HEAD_ZH : TABLE_HEAD_EN))

const SECTION_TITLE_ZH = {
  billShip: '账单与收货',
  invoiceDetails: '发票明细',
  bankDetails: '收款银行'
} as const

const SECTION_TITLE_EN = {
  billShip: 'BILL & SHIP',
  invoiceDetails: 'INVOICE DETAILS',
  bankDetails: 'BANK DETAILS'
} as const

const sectionTitle = computed(() => (props.reportLang === 'zh' ? SECTION_TITLE_ZH : SECTION_TITLE_EN))

function dash(v?: string | null) {
  const s = (v ?? '').trim()
  return s || '—'
}
</script>

<style lang="scss">
.po-doc--inv-v2 {
  --po-v2-navy: #090e1d;
  --po-v2-accent: #00d2ef;
  --po-v2-line: #05e5ff;
  --po-v2-head-fg: #fff;
  --po-v2-ink: #1a1d22;
  --po-v2-muted: #6b7280;
  --po-v2-border: #d8e1e6;
  --po-v2-wash: #f5f7fa;
  --po-v2-panel: #eaf6fb;
  --po-v2-row: #f4f9fc;
  --po-v2-legal: #f3f7f9;

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
      repeating-linear-gradient(
        45deg,
        transparent 0 8px,
        rgba(5, 229, 255, 0.22) 8px 8.7px
      ),
      repeating-linear-gradient(
        -45deg,
        transparent 0 8px,
        rgba(5, 229, 255, 0.22) 8px 8.7px
      );
    mask-image: linear-gradient(90deg, transparent 0%, #000 42%);
    -webkit-mask-image: linear-gradient(90deg, transparent 0%, #000 42%);
  }

  .po-v2__title-zh {
    position: relative;
    font-size: 16pt;
    font-weight: 800;
    letter-spacing: 0.16em;
    line-height: 1.05;
    color: #fff;
    font-family: 'Microsoft YaHei', Arial, sans-serif;
  }

  .po-v2__title-en {
    position: relative;
    margin-top: 1mm;
    font-size: 6.5pt;
    font-weight: 700;
    letter-spacing: 0.46em;
    text-indent: 0.46em;
    text-transform: uppercase;
    color: var(--po-v2-accent);
    line-height: 1.15;
  }

  .po-v2__po-no {
    position: relative;
    margin-top: 2mm;
    font-size: 7pt;
    font-weight: 500;
    letter-spacing: 0.03em;
    color: #fff;
    white-space: nowrap;
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
    grid-template-columns: repeat(4, 1fr);
    background: var(--po-v2-wash);
    border: 1px solid var(--po-v2-border);
    margin-bottom: 3.6mm;
  }

  .po-v2__meta--inv {
    grid-template-columns: repeat(2, 1fr);
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

  .po-v2__parties {
    display: grid;
    grid-template-columns: 1fr 1fr;
  }

  .po-v2__party {
    padding: 0 0 2.2mm;
    border: 1px solid var(--po-v2-border);
    border-top: 2.4px solid var(--po-v2-line);
    min-height: 22mm;
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

  .po-v2__addr-body {
    padding: 2mm 3mm 1.6mm;
    font-size: 8pt;
    line-height: 1.45;
  }

  .po-v2__addr-line {
    margin-bottom: 1mm;
    word-break: break-word;
  }

  .po-v2__addr-line:last-child {
    margin-bottom: 0;
  }

  .po-v2__grid {
    width: 100%;
    border-collapse: collapse;
    table-layout: fixed;
    font-size: 7.4pt;
    border: 1px solid var(--po-v2-border);
  }

  .c-inv-idx {
    width: 8%;
  }
  .c-inv-pn {
    width: 20%;
  }
  .c-inv-brand {
    width: 18%;
  }
  .c-inv-qty {
    width: 10%;
  }
  .c-inv-price {
    width: 12%;
  }
  .c-inv-amt {
    width: 14%;
  }
  .c-inv-rmk {
    width: 18%;
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

  .po-v2__mpn {
    font-weight: 700;
  }

  .po-v2__cell-sub {
    margin-top: 1px;
    font-size: 6.8pt;
    font-style: italic;
    color: var(--po-v2-muted);
    font-weight: 400;
  }

  .po-v2__grid .cen {
    text-align: center;
  }

  .po-v2__grid .num,
  .po-v2 .num {
    text-align: right;
    font-variant-numeric: tabular-nums;
  }

  .po-v2__empty {
    text-align: center;
    color: var(--po-v2-muted);
    padding: 5mm 0 !important;
    background: #fff !important;
  }

  .po-v2__sum-row td {
    font-weight: 800;
    background: var(--po-v2-panel) !important;
  }

  .po-v2__bank {
    border: 1px solid var(--po-v2-border);
    padding: 2.4mm 3mm;
    background: #fff;
  }

  .po-v2__bank-line {
    font-size: 7.8pt;
    line-height: 1.45;
    margin-bottom: 1mm;
    word-break: break-word;
  }

  .po-v2__bank-line:last-child {
    margin-bottom: 0;
  }

  .po-v2__sign {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 4mm;
    margin: 4mm 0 3mm;
  }

  .po-v2__sign-box {
    border: 1px dashed #b7c0c8;
    padding: 2.6mm 3.2mm 3mm;
    min-height: 32mm;
  }

  .po-v2__sign-t {
    font-weight: 800;
    font-size: 9.5pt;
    margin-bottom: 1.4mm;
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
    left: 12mm;
    top: 0;
  }

  .po-v2__sign-foot {
    margin-top: 2mm;
    font-size: 8pt;
    color: var(--po-v2-muted);
  }

  @media print {
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }
}
</style>
