<template>
  <div class="po-doc">
    <header class="po-doc__masthead">
      <div class="po-doc__masthead-top">
        <div class="po-doc__masthead-logo-wrap">
          <img v-if="logoUrl" class="po-doc__logo" :src="logoUrl" alt="" />
        </div>
        <div class="po-doc__masthead-center">
          <div class="po-doc__masthead-company">{{ headerCompanyName }}</div>
          <div v-if="headerWarehouseAddress" class="po-doc__masthead-warehouse-addr">{{ headerWarehouseAddress }}</div>
          <div class="po-doc__masthead-title-gap" aria-hidden="true"></div>
          <div class="po-doc__masthead-title">{{ invoiceTitle }}</div>
          <div v-if="invoiceSubtitle" class="po-doc__masthead-sub">{{ invoiceSubtitle }}</div>
        </div>
      </div>
      <div class="po-doc__masthead-meta">
        <div><span class="po-doc__k">{{ labels.date }}</span>{{ invoiceDate }}</div>
        <div class="po-doc__masthead-meta-line po-doc__masthead-meta-line--nowrap">
          <span class="po-doc__k">{{ labels.invoiceNo }}</span>{{ invoiceNo }}
        </div>
      </div>
      <div class="po-doc__masthead-meta-gap" aria-hidden="true"></div>
    </header>

    <table class="po-doc__tri po-doc__tri--addr">
      <thead>
        <tr>
          <th>{{ labels.billTo }}</th>
          <th>{{ labels.shipTo }}</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td class="po-doc__tri-cell">
            <div v-for="(t, i) in billToLines" :key="'bt' + i" class="po-doc__tri-line">{{ t }}</div>
          </td>
          <td class="po-doc__tri-cell">
            <div v-for="(t, i) in shipToLines" :key="'st' + i" class="po-doc__tri-line">{{ t }}</div>
          </td>
        </tr>
      </tbody>
    </table>

    <table class="po-doc__grid">
      <thead>
        <tr>
          <th class="w-inv-idx">{{ labels.no }}</th>
          <th class="w-inv-pn">{{ labels.pn }}</th>
          <th class="w-inv-brand">{{ labels.brand }}</th>
          <th class="w-inv-qty num">{{ labels.qty }}</th>
          <th class="w-inv-price num">{{ labels.upUsd }}</th>
          <th class="w-inv-amt num">{{ labels.amountUsd }}</th>
          <th class="w-inv-rmk">{{ labels.remark }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="line in lines" :key="'l' + line.index">
          <td class="cen">{{ line.index }}</td>
          <td>
            <div>{{ line.pn }}</div>
            <div v-if="line.customerPn" class="po-doc__cell-sub">{{ line.customerPn }}</div>
          </td>
          <td>
            <div>{{ line.brand }}</div>
            <div v-if="line.customerBrand" class="po-doc__cell-sub">{{ line.customerBrand }}</div>
          </td>
          <td class="num">{{ line.qty }}</td>
          <td class="num">{{ showAmounts ? line.unitPrice : '—' }}</td>
          <td class="num">{{ showAmounts ? line.amount : '—' }}</td>
          <td>{{ line.remark }}</td>
        </tr>
        <tr v-for="i in fillerRowCount" :key="'f' + i">
          <td v-for="c in 7" :key="`${i}-${c}`">&nbsp;</td>
        </tr>
        <tr v-if="lines.length === 0">
          <td colspan="7" class="po-doc__empty">{{ labels.noItems }}</td>
        </tr>
        <tr v-else class="po-doc__hint-row">
          <td colspan="7" class="po-doc__hint">{{ labels.blankBelow }}</td>
        </tr>
        <tr v-if="lines.length > 0" class="po-doc__sum-row">
          <td>{{ labels.total }}</td>
          <td colspan="2"></td>
          <td class="num">{{ totalQty }}</td>
          <td></td>
          <td class="num">{{ showAmounts ? totalAmount : '—' }}</td>
          <td></td>
        </tr>
      </tbody>
    </table>


    <section class="po-doc__addon">
      <div class="po-doc__addon-bar">{{ labels.bankDetails }}</div>
      <div class="po-doc__addon-body">
        <div v-for="(t, i) in bankLines" :key="'b' + i" class="po-doc__addon-line">{{ t }}</div>

      </div>
    </section>

    <section class="po-doc__sign">
      <div class="po-doc__sign-t po-doc__sign-t--left">{{ labels.exporterSign }}</div>
      <div class="po-doc__sign-t po-doc__sign-t--right">{{ labels.consigneeSign }}</div>
      <div class="po-doc__sign-pad po-doc__sign-pad--left po-doc__sign-pad--seal">
        <img v-if="showSeal && sealUrl" class="po-doc__seal" :src="sealUrl" alt="" />
      </div>
      <div class="po-doc__sign-pad po-doc__sign-pad--right"></div>
      <div class="po-doc__sign-foot po-doc__sign-foot--left">{{ labels.date }}{{ signDate }}</div>
      <div class="po-doc__sign-foot po-doc__sign-foot--right">{{ labels.date }}</div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { InvoiceReportLabels } from './packingReportLabels'

export interface StockOutInvoiceLineVm {
  index: number
  pn: string
  customerPn: string
  brand: string
  customerBrand: string
  qty: string
  unitPrice: string
  amount: string
  remark: string
}

const props = withDefaults(
  defineProps<{
    labels: InvoiceReportLabels
    headerCompanyName: string
    invoiceTitle: string
    invoiceSubtitle?: string
    invoiceNo: string
    invoiceDate: string
    headerWarehouseAddress?: string
    billToLines?: string[]
    shipToLines?: string[]
    lines: StockOutInvoiceLineVm[]
    totalQty: string
    totalAmount: string
    bankLines: string[]
    sealUrl: string | null
    logoUrl: string | null
    showAmounts: boolean
    showSeal: boolean
    signDate: string
  }>(),
  {
    invoiceSubtitle: '',
    headerWarehouseAddress: '',
    showSeal: true,
    signDate: ''
  }
)

const fillerRowCount = computed(() => {
  const target = 5
  const n = props.lines.length
  if (n === 0) return 0
  return Math.max(0, target - n)
})
</script>

<style scoped lang="scss">
$po-orange: #e5913e;
$po-border: #222;
$po-head-fg: #111;

.po-doc {
  width: 210mm;
  min-height: 297mm;
  margin: 0 auto;
  padding: 10mm 12mm 14mm;
  box-sizing: border-box;
  background: #fff;
  color: $po-head-fg;
  font-size: 10pt;
  line-height: 1.5;
  font-family: 'Microsoft YaHei', 'SimHei', 'SimSun', system-ui, sans-serif;
}

.po-doc__masthead {
  margin-bottom: 6px;
}

.po-doc__tri--addr {
  margin-bottom: 10px;
}

.po-doc__masthead-top {
  display: grid;
  grid-template-columns: 36mm 1fr;
  align-items: start;
  gap: 4mm;
}

.po-doc__masthead-logo-wrap {
  padding-top: 2px;
}

.po-doc__logo {
  max-height: 14mm;
  max-width: 28mm;
  object-fit: contain;
  display: block;
}

.po-doc__masthead-center {
  text-align: center;
  padding-top: 2px;
}

.po-doc__masthead-meta {
  margin-top: 2mm;
  font-size: 10pt;
  line-height: 1.65;
  text-align: left;
}

.po-doc__masthead-meta-gap {
  height: 1.5em;
}

.po-doc__masthead-meta-line--nowrap {
  white-space: nowrap;
}

.po-doc__masthead-warehouse-addr {
  margin-top: 4px;
  font-size: 10pt;
  font-weight: 400;
  line-height: 1.45;
  color: #333;
}

.po-doc__masthead-title-gap {
  height: 1.5em;
}

.po-doc__masthead-company {
  font-size: 16pt;
  font-weight: 700;
  letter-spacing: 0.02em;
}

.po-doc__masthead-title {
  font-size: 15pt;
  font-weight: 700;
  margin-top: 4px;
  letter-spacing: 0.2em;
  text-indent: 0.2em;
}

.po-doc__masthead-sub {
  margin-top: 4px;
  font-size: 10pt;
  color: #333;
  letter-spacing: 0.08em;
}

.po-doc__k {
  font-weight: 600;
}

.po-doc__tri {
  width: 100%;
  border-collapse: collapse;
  margin-bottom: 10px;
  table-layout: fixed;
}

.po-doc__tri th {
  background: $po-orange;
  color: $po-head-fg;
  font-weight: 700;
  border: 1px solid $po-border;
  padding: 6px 8px;
  text-align: center;
  font-size: 10.5pt;
}

.po-doc__tri td {
  border: 1px solid $po-border;
  padding: 8px 10px;
  vertical-align: top;
  font-size: 9.5pt;
}

.po-doc__tri-line {
  margin-bottom: 4px;
}

.po-doc__tri-line:last-child {
  margin-bottom: 0;
}

.po-doc__grid {
  width: 100%;
  border-collapse: collapse;
  margin-bottom: 6px;
  table-layout: fixed;
}

.po-doc__grid th,
.po-doc__grid td {
  border: 1px solid $po-border;
  padding: 4px 5px;
  vertical-align: middle;
  word-break: break-all;
}

.po-doc__grid thead th {
  background: $po-orange;
  color: $po-head-fg;
  font-weight: 700;
  text-align: center;
  font-size: 8.6pt;
  padding: 6px 3px;
}

.w-inv-idx {
  width: 8%;
}
.w-inv-pn {
  width: 20%;
}
.w-inv-brand {
  width: 18%;
}
.w-inv-qty {
  width: 10%;
}
.w-inv-price {
  width: 12%;
}
.w-inv-amt {
  width: 14%;
}
.w-inv-rmk {
  width: 18%;
}

.po-doc__cell-sub {
  margin-top: 2px;
  font-size: 9pt;
  font-style: italic;
  color: #666;
}

.cen {
  text-align: center;
}

.num {
  text-align: right;
}

.po-doc__empty {
  text-align: center;
  color: #666;
  padding: 14px !important;
}

.po-doc__hint-row .po-doc__hint {
  text-align: center;
  font-size: 9.5pt;
  color: #333;
  padding: 8px !important;
}

.po-doc__sum-row td {
  font-weight: 700;
  padding: 6px 5px;
}
.po-doc__addon {
  margin-top: 6px;
  margin-bottom: 14px;
}

.po-doc__addon-bar {
  background: $po-orange;
  color: $po-head-fg;
  font-weight: 700;
  padding: 6px 10px;
  border: 1px solid $po-border;
  border-bottom: none;
  font-size: 10.5pt;
}

.po-doc__addon-body {
  border: 1px solid $po-border;
  border-top: 1px solid $po-border;
  padding: 10px 12px 12px;
  font-size: 9.5pt;
}

.po-doc__addon-line {
  margin-bottom: 6px;
}

.po-doc__addon-terms-hd {
  font-weight: 700;
  margin: 10px 0 6px;
  font-size: 10pt;
}

.po-doc__term-line {
  font-size: 8.8pt;
  line-height: 1.45;
  margin-bottom: 3px;
  text-align: justify;
}

.po-doc__sign {
  display: grid;
  grid-template-columns: 1fr 1fr;
  grid-template-rows: auto auto auto;
  column-gap: 12mm;
  row-gap: 0;
  margin-top: 8mm;
  font-size: 9.5pt;
}

.po-doc__sign-t--left {
  grid-column: 1;
  grid-row: 1;
}

.po-doc__sign-t--right {
  grid-column: 2;
  grid-row: 1;
}

.po-doc__sign-pad--left {
  grid-column: 1;
  grid-row: 2;
}

.po-doc__sign-pad--right {
  grid-column: 2;
  grid-row: 2;
}

.po-doc__sign-foot--left {
  grid-column: 1;
  grid-row: 3;
}

.po-doc__sign-foot--right {
  grid-column: 2;
  grid-row: 3;
}

.po-doc__sign-t {
  font-weight: 600;
  margin-bottom: 4px;
}

.po-doc__sign-pad {
  min-height: 26mm;
  margin: 6px 0 8px;
  position: relative;
}

.po-doc__sign-pad--seal {
  background-color: #fff;
  isolation: isolate;
}

.po-doc__seal {
  position: absolute;
  left: 0;
  bottom: 0;
  max-height: 26mm;
  max-width: 32mm;
  object-fit: contain;
}

@media print {
  .po-doc {
    width: auto;
    min-height: auto;
    margin: 0;
    padding: 8mm 10mm;
  }
}
</style>
