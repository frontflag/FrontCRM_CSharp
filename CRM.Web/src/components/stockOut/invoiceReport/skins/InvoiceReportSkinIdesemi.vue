<template>
  <div class="po-doc po-doc--idesemi">
    <header class="po-doc__masthead">
      <div class="po-doc__brand-bar">
        <div class="po-doc__brand-bar-inner">
          <img v-if="logoUrl" class="po-doc__logo" :src="logoUrl" alt="" />
          <div class="po-doc__brand-text">
            <div class="po-doc__masthead-company">{{ headerCompanyName }}</div>
            <div v-if="headerWarehouseAddress" class="po-doc__masthead-warehouse-addr">{{ headerWarehouseAddress }}</div>
          </div>
        </div>
        <div class="po-doc__amber-line" aria-hidden="true" />
      </div>

      <div class="po-doc__masthead-row">
        <div class="po-doc__title-block">
          <div class="po-doc__masthead-title">{{ invoiceTitle }}</div>
          <div v-if="invoiceSubtitle" class="po-doc__masthead-sub">{{ invoiceSubtitle }}</div>
        </div>
        <aside class="po-doc__meta-card">
          <div><span class="po-doc__k">{{ labels.date }}</span>{{ invoiceDate }}</div>
          <div class="po-doc__meta-line--nowrap">
            <span class="po-doc__k">{{ labels.invoiceNo }}</span>{{ invoiceNo }}
          </div>
        </aside>
      </div>
    </header>

    <div class="po-doc__addr">
      <div class="po-doc__addr-col">
        <div class="po-doc__addr-hd">{{ labels.billTo }}</div>
        <div class="po-doc__addr-body">
          <div v-for="(t, i) in billToLines" :key="'bt' + i" class="po-doc__tri-line">{{ t }}</div>
        </div>
      </div>
      <div class="po-doc__addr-col">
        <div class="po-doc__addr-hd">{{ labels.shipTo }}</div>
        <div class="po-doc__addr-body">
          <div v-for="(t, i) in shipToLines" :key="'st' + i" class="po-doc__tri-line">{{ t }}</div>
        </div>
      </div>
    </div>

    <div class="po-doc__body">
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
        <div class="po-doc__section-title">{{ labels.bankDetails }}</div>
        <div class="po-doc__addon-body">
          <div v-for="(t, i) in bankLines" :key="'b' + i" class="po-doc__term-line">{{ t }}</div>
        </div>
      </section>

      <section class="po-doc__sign">
        <div class="po-doc__sign-col">
          <div class="po-doc__sign-t">{{ labels.exporterSign }}</div>
          <div class="po-doc__sign-pad po-doc__sign-pad--seal">
            <img v-if="showSeal && sealUrl" class="po-doc__seal" :src="sealUrl" alt="" />
          </div>
          <div class="po-doc__sign-foot">{{ labels.date }}{{ signDate }}</div>
        </div>
        <div class="po-doc__sign-col">
          <div class="po-doc__sign-t">{{ labels.consigneeSign }}</div>
          <div class="po-doc__sign-pad"></div>
          <div class="po-doc__sign-foot">{{ labels.date }}</div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import {
  invoiceReportDocumentPropDefaults,
  invoiceReportFillerRowCount,
  type InvoiceReportDocumentProps
} from '../types'

const props = withDefaults(defineProps<InvoiceReportDocumentProps>(), invoiceReportDocumentPropDefaults)

const fillerRowCount = computed(() => invoiceReportFillerRowCount(props.lines.length))
</script>

<style scoped lang="scss">
/* 与 Packing Idesemi 同源；挂到 semicore 租户 */
$ide-purple: #2d1b4e;
$ide-purple-deep: #1a0a2e;
$ide-header-bg: #0d1f35;
$ide-accent: #020612;
$ide-amber: $ide-accent;
$ide-amber-soft: #0a1628;
$ide-border: #c4b5d4;
$ide-text: #1f1235;

.po-doc {
  width: 210mm;
  min-height: 297mm;
  margin: 0 auto;
  padding: 8mm 11mm 12mm;
  box-sizing: border-box;
  background: #fff;
  color: $ide-text;
  font-size: 10pt;
  line-height: 1.5;
  font-family: 'Segoe UI', 'Microsoft YaHei', system-ui, sans-serif;
}

.po-doc__brand-bar {
  margin: -8mm -11mm 4mm;
  padding: 5mm 11mm 0;
  background: $ide-header-bg;
  color: #fff;
}

.po-doc__brand-bar-inner {
  display: flex;
  align-items: center;
  gap: 4mm;
  padding-bottom: 3.5mm;
}

.po-doc__logo {
  max-height: 12mm;
  max-width: 26mm;
  object-fit: contain;
  display: block;
  background: rgba(255, 255, 255, 0.92);
  padding: 2px 4px;
  border-radius: 2px;
}

.po-doc__brand-text {
  flex: 1;
  min-width: 0;
  text-align: left;
}

.po-doc__masthead-company {
  font-size: 14pt;
  font-weight: 700;
  letter-spacing: 0.04em;
  color: #fff;
}

.po-doc__masthead-warehouse-addr {
  margin-top: 2px;
  font-size: 9pt;
  color: rgba(255, 255, 255, 0.82);
  line-height: 1.4;
}

.po-doc__amber-line {
  height: 3px;
  background: linear-gradient(90deg, $ide-amber 0%, $ide-amber-soft 45%, transparent 100%);
}

.po-doc__masthead-row {
  display: grid;
  grid-template-columns: 1fr 52mm;
  gap: 5mm;
  align-items: start;
  margin-bottom: 5mm;
}

.po-doc__title-block {
  text-align: left;
  padding-top: 1mm;
}

.po-doc__masthead-title {
  font-size: 16pt;
  font-weight: 700;
  letter-spacing: 0.12em;
  color: $ide-purple-deep;
}

.po-doc__masthead-sub {
  margin-top: 3px;
  font-size: 9.5pt;
  color: #5b4a6e;
}

.po-doc__meta-card {
  border: 1px solid $ide-border;
  background: #faf7ff;
  padding: 3mm 3.5mm;
  font-size: 9pt;
  line-height: 1.65;
  border-left: 3px solid $ide-amber;
}

.po-doc__k {
  font-weight: 700;
  color: $ide-purple;
}

.po-doc__meta-line--nowrap {
  white-space: nowrap;
}

.po-doc__addr {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 4mm;
  margin-bottom: 5mm;
}

.po-doc__addr-col {
  border: 1px solid $ide-border;
  min-height: 22mm;
}

.po-doc__addr-hd {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 4px 8px;
  font-weight: 700;
  font-size: 10pt;
  color: $ide-purple-deep;
  background: #f3eef9;
  border-bottom: 1px solid $ide-border;
}

.po-doc__addr-hd::before {
  content: '';
  width: 3px;
  height: 12px;
  background: $ide-amber;
  border-radius: 1px;
}

.po-doc__addr-body {
  padding: 6px 10px;
  font-size: 9.5pt;
}

.po-doc__tri-line {
  margin-bottom: 3px;
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
  border: 1px solid $ide-border;
  padding: 4px 5px;
  vertical-align: middle;
  word-break: break-all;
}

.po-doc__grid thead th {
  background: $ide-header-bg;
  color: #fff;
  font-weight: 700;
  text-align: center;
  font-size: 8.6pt;
  padding: 7px 3px;
  border-bottom: 2.5px solid $ide-accent;
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
  color: #6b5a7e;
}

.cen {
  text-align: center;
}

.num {
  text-align: right;
}

.po-doc__empty {
  text-align: center;
  color: #6b5a7e;
  padding: 14px !important;
}

.po-doc__hint-row .po-doc__hint {
  text-align: center;
  font-size: 9.5pt;
  color: #5b4a6e;
  padding: 8px !important;
}

.po-doc__sum-row td {
  font-weight: 700;
  padding: 6px 5px;
  background: #faf7ff;
}

.po-doc__section-title {
  font-weight: 700;
  font-size: 10.5pt;
  color: $ide-purple-deep;
  padding: 5px 0 5px 8px;
  margin-bottom: 0;
  border-left: 3px solid $ide-amber;
  background: transparent;
}

.po-doc__addon {
  margin-top: 6px;
  margin-bottom: 12px;
}

.po-doc__addon-body {
  border: 1px solid $ide-border;
  padding: 8px 12px 10px;
  font-size: 9.5pt;
  margin-top: 0;
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
  column-gap: 6mm;
  margin-top: 7mm;
  font-size: 9.5pt;
}

.po-doc__sign-col {
  border: 1px solid $ide-border;
  padding: 4mm 4mm 3mm;
  background: #fcfaff;
}

.po-doc__sign-col + .po-doc__sign-col {
  border-left-color: rgba(2, 6, 18, 0.35);
}

.po-doc__sign-t {
  font-weight: 700;
  color: $ide-purple;
  margin-bottom: 4px;
}

.po-doc__sign-pad {
  min-height: 24mm;
  margin: 4px 0 6px;
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
  max-height: 24mm;
  max-width: 30mm;
  object-fit: contain;
}

.po-doc__sign-foot {
  font-size: 9pt;
  color: #5b4a6e;
}

@media print {
  .po-doc {
    width: auto;
    min-height: auto;
    margin: 0;
    padding: 8mm 10mm;
  }

  .po-doc__brand-bar {
    margin: -8mm -10mm 4mm;
    padding: 5mm 10mm 0;
  }
}
</style>
