<template>
  <div class="po-doc po-doc--ecoinf">
    <header class="po-doc__masthead">
      <div class="po-doc__masthead-top">
        <div class="po-doc__title-stack">
          <div class="po-doc__masthead-title">{{ docTitle }}</div>
          <div class="po-doc__masthead-company">{{ headerCompanyName }}</div>
          <div v-if="headerWarehouseAddress" class="po-doc__masthead-warehouse-addr">{{ headerWarehouseAddress }}</div>
          <div v-if="docSubtitle" class="po-doc__masthead-sub">{{ docSubtitle }}</div>
        </div>
        <div class="po-doc__masthead-logo-wrap">
          <img v-if="logoUrl" class="po-doc__logo" :src="logoUrl" alt="" />
        </div>
      </div>
      <div class="po-doc__meta-row">
        <div><span class="po-doc__k">{{ labels.date }}</span>{{ docDate }}</div>
        <div class="po-doc__meta-line--nowrap">
          <span class="po-doc__k">{{ labels.packingNo }}</span>{{ docNo }}
        </div>
        <div class="po-doc__meta-line--nowrap">
          <span class="po-doc__k">{{ labels.shipMethod }}</span>{{ shipmentMethodDisplay }}
        </div>
      </div>
    </header>

    <div class="po-doc__addr">
      <section class="po-doc__addr-block">
        <div class="po-doc__section-label">{{ labels.billTo }}</div>
        <div class="po-doc__addr-body">
          <div v-for="(t, i) in billToLines" :key="'bt' + i" class="po-doc__tri-line">{{ t }}</div>
        </div>
      </section>
      <section class="po-doc__addr-block">
        <div class="po-doc__section-label">{{ labels.shipTo }}</div>
        <div class="po-doc__addr-body">
          <div v-for="(t, i) in shipToLines" :key="'st' + i" class="po-doc__tri-line">{{ t }}</div>
        </div>
      </section>
    </div>

    <div class="po-doc__body">
      <table class="po-doc__grid">
        <thead>
          <tr>
            <th class="w-pk-idx">{{ labels.no }}</th>
            <th class="w-pk-pn">{{ labels.pn }}</th>
            <th class="w-pk-brand">{{ labels.brand }}</th>
            <th class="w-pk-qty num">{{ labels.qty }}</th>
            <th class="w-pk-ctn">{{ labels.carton }}</th>
            <th class="w-pk-rmk">{{ labels.remark }}</th>
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
            <td class="cen">{{ line.carton }}</td>
            <td>{{ line.remark }}</td>
          </tr>
          <tr v-for="i in fillerRowCount" :key="'f' + i">
            <td v-for="c in 6" :key="`${i}-${c}`">&nbsp;</td>
          </tr>
          <tr v-if="lines.length === 0">
            <td colspan="6" class="po-doc__empty">{{ labels.noItems }}</td>
          </tr>
          <tr v-else class="po-doc__hint-row">
            <td colspan="6" class="po-doc__hint">{{ labels.blankBelow }}</td>
          </tr>
          <tr v-if="lines.length > 0" class="po-doc__sum-row">
            <td>{{ labels.total }}</td>
            <td colspan="2"></td>
            <td class="num">{{ totalQty }}</td>
            <td colspan="2"></td>
          </tr>
        </tbody>
      </table>

      <section v-if="withShipmentInspection" class="po-doc__qc">
        <div class="po-doc__section-label">{{ labels.outboundInspection }}</div>
        <ul class="po-doc__qc-list">
          <li v-for="(item, idx) in labels.qcItems" :key="'qc' + idx" class="po-doc__qc-li">
            <span class="po-doc__qc-box" aria-hidden="true" />
            <span class="po-doc__qc-idx">{{ idx + 1 }}.</span>
            <span class="po-doc__qc-text">{{ item }}</span>
            <span class="po-doc__qc-result">{{ labels.result }}</span>
          </li>
        </ul>
        <div class="po-doc__qc-foot">
          <span>{{ labels.qcInspector }}</span>
          <span>{{ labels.qcDate }}</span>
        </div>
      </section>

      <section class="po-doc__addon">
        <div class="po-doc__section-label">{{ labels.remarks }}</div>
        <div class="po-doc__addon-body">
          <div v-for="(t, i) in notes" :key="'n' + i" class="po-doc__term-line">{{ t }}</div>
        </div>
      </section>

      <section class="po-doc__sign">
        <div class="po-doc__sign-col">
          <div class="po-doc__sign-t">{{ labels.shipperSign }}</div>
          <div class="po-doc__sign-line po-doc__sign-pad--seal">
            <img v-if="showSeal && sealUrl" class="po-doc__seal" :src="sealUrl" alt="" />
          </div>
          <div class="po-doc__sign-foot">{{ labels.date }} {{ signDate }}</div>
        </div>
        <div class="po-doc__sign-col">
          <div class="po-doc__sign-t">{{ labels.consigneeSign }}</div>
          <div class="po-doc__sign-line"></div>
          <div class="po-doc__sign-foot">{{ labels.date }}</div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import {
  packingReportDocumentPropDefaults,
  packingReportFillerRowCount,
  type PackingReportDocumentProps
} from '../types'

const props = withDefaults(defineProps<PackingReportDocumentProps>(), packingReportDocumentPropDefaults)

const fillerRowCount = computed(() => packingReportFillerRowCount(props.lines.length))
</script>

<style scoped lang="scss">
$eco-lime: #84cc16;
$eco-ink: #111;
$eco-muted: #525252;
$eco-line: #d4d4d4;
$eco-zebra: #f5f5f5;

.po-doc {
  width: 210mm;
  min-height: 297mm;
  margin: 0 auto;
  padding: 10mm 12mm 14mm;
  box-sizing: border-box;
  background: #fff;
  color: $eco-ink;
  font-size: 10pt;
  line-height: 1.45;
  font-family: 'IBM Plex Sans', 'Segoe UI', 'Microsoft YaHei', system-ui, sans-serif;
}

.po-doc__masthead {
  margin-bottom: 5mm;
  padding-bottom: 3mm;
  border-bottom: 2px solid $eco-ink;
}

.po-doc__masthead-top {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 6mm;
}

.po-doc__title-stack {
  text-align: left;
  flex: 1;
  min-width: 0;
}

.po-doc__masthead-title {
  font-size: 20pt;
  font-weight: 700;
  letter-spacing: 0.28em;
  text-indent: 0.08em;
  text-transform: uppercase;
  line-height: 1.15;
}

.po-doc__masthead-company {
  margin-top: 3mm;
  font-size: 9.5pt;
  font-weight: 600;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: $eco-muted;
}

.po-doc__masthead-warehouse-addr {
  margin-top: 2px;
  font-size: 9pt;
  color: $eco-muted;
  line-height: 1.4;
  letter-spacing: 0;
  text-transform: none;
  font-weight: 400;
}

.po-doc__masthead-sub {
  margin-top: 2px;
  font-size: 9pt;
  color: $eco-muted;
}

.po-doc__masthead-logo-wrap {
  flex-shrink: 0;
}

.po-doc__logo {
  max-height: 14mm;
  max-width: 32mm;
  object-fit: contain;
  display: block;
}

.po-doc__meta-row {
  display: flex;
  flex-wrap: wrap;
  gap: 2mm 8mm;
  margin-top: 3.5mm;
  font-size: 9.5pt;
  font-variant-numeric: tabular-nums;
}

.po-doc__k {
  font-weight: 700;
  color: $eco-ink;
}

.po-doc__meta-line--nowrap {
  white-space: nowrap;
}

.po-doc__section-label {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 9pt;
  font-weight: 700;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: $eco-ink;
  margin: 0 0 2.5mm;
}

.po-doc__section-label::before {
  content: '';
  width: 3px;
  height: 11px;
  background: $eco-lime;
  flex-shrink: 0;
}

.po-doc__addr {
  display: flex;
  flex-direction: column;
  gap: 3.5mm;
  margin-bottom: 5mm;
}

.po-doc__addr-block {
  border: 1px solid $eco-line;
  padding: 2.5mm 3.5mm 3mm;
}

.po-doc__addr-block .po-doc__section-label {
  margin-bottom: 1.5mm;
}

.po-doc__addr-body {
  font-size: 9.5pt;
}

.po-doc__tri-line {
  margin-bottom: 2px;
}

.po-doc__tri-line:last-child {
  margin-bottom: 0;
}

.po-doc__grid {
  width: 100%;
  border-collapse: collapse;
  margin-bottom: 6mm;
  table-layout: fixed;
}

.po-doc__grid th,
.po-doc__grid td {
  border: none;
  border-bottom: 1px solid $eco-line;
  padding: 5px 4px;
  vertical-align: middle;
  word-break: break-all;
  font-size: 9pt;
}

.po-doc__grid thead th {
  background: transparent;
  color: $eco-ink;
  font-weight: 700;
  text-align: left;
  font-size: 8.2pt;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  border-top: 2px solid $eco-ink;
  border-bottom: 2px solid $eco-ink;
  padding: 6px 4px;
}

.po-doc__grid thead th.num {
  text-align: right;
}

.po-doc__grid tbody tr:nth-child(odd) td {
  background: $eco-zebra;
}

.po-doc__grid tbody tr.po-doc__hint-row td,
.po-doc__grid tbody tr.po-doc__sum-row td {
  background: #fff;
}

.w-pk-idx {
  width: 8%;
}
.w-pk-pn {
  width: 24%;
}
.w-pk-brand {
  width: 24%;
}
.w-pk-qty {
  width: 12%;
}
.w-pk-ctn {
  width: 12%;
}
.w-pk-rmk {
  width: 20%;
}

.po-doc__cell-sub {
  margin-top: 2px;
  font-size: 8.5pt;
  color: $eco-muted;
  font-family: ui-monospace, 'Cascadia Mono', monospace;
}

.cen {
  text-align: center;
}

.num {
  text-align: right;
  font-variant-numeric: tabular-nums;
}

.po-doc__empty {
  text-align: center;
  color: $eco-muted;
  padding: 14px !important;
}

.po-doc__hint-row .po-doc__hint {
  text-align: center;
  font-size: 9pt;
  color: $eco-muted;
  padding: 8px !important;
  letter-spacing: 0.08em;
}

.po-doc__sum-row td {
  font-weight: 700;
  border-top: 2px solid $eco-ink;
  border-bottom: 2px solid $eco-ink;
  padding: 7px 4px;
}

.po-doc__qc {
  margin-bottom: 5mm;
}

.po-doc__qc-list {
  list-style: none;
  margin: 0;
  padding: 0;
  border: 1px solid $eco-line;
}

.po-doc__qc-li {
  display: grid;
  grid-template-columns: 12px 18px 1fr 22mm;
  gap: 6px;
  align-items: start;
  padding: 5px 8px;
  border-bottom: 1px solid $eco-line;
  font-size: 9pt;
}

.po-doc__qc-li:last-child {
  border-bottom: none;
}

.po-doc__qc-li:nth-child(odd) {
  background: $eco-zebra;
}

.po-doc__qc-box {
  width: 10px;
  height: 10px;
  margin-top: 3px;
  border: 1.5px solid $eco-ink;
  box-shadow: inset 0 0 0 1px $eco-lime;
}

.po-doc__qc-idx {
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.po-doc__qc-text {
  line-height: 1.4;
  text-align: justify;
}

.po-doc__qc-result {
  font-size: 8pt;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: $eco-muted;
  text-align: right;
  border-bottom: 1px solid $eco-line;
  min-height: 14px;
}

.po-doc__qc-foot {
  display: flex;
  justify-content: space-between;
  margin-top: 6px;
  font-size: 9.5pt;
  padding: 0 2px;
}

.po-doc__addon {
  margin-bottom: 8mm;
}

.po-doc__addon-body {
  border: 1px solid $eco-line;
  padding: 8px 10px;
  min-height: 14mm;
  font-size: 9.5pt;
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
  column-gap: 14mm;
  margin-top: 6mm;
  font-size: 9.5pt;
}

.po-doc__sign-t {
  font-weight: 600;
  font-size: 9pt;
  letter-spacing: 0.04em;
  margin-bottom: 10mm;
}

.po-doc__sign-line {
  position: relative;
  border-bottom: 1.5px solid $eco-ink;
  min-height: 18mm;
  margin-bottom: 3mm;
}

.po-doc__sign-pad--seal {
  isolation: isolate;
}

.po-doc__seal {
  position: absolute;
  left: 0;
  bottom: 2px;
  max-height: 22mm;
  max-width: 28mm;
  object-fit: contain;
}

.po-doc__sign-foot {
  font-size: 9pt;
  color: $eco-muted;
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
