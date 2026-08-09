<template>
  <div class="po-doc po-doc--semicore">
    <header class="po-doc__masthead">
      <div class="po-doc__masthead-top">
        <div class="po-doc__masthead-logo-wrap">
          <img v-if="logoUrl" class="po-doc__logo" :src="logoUrl" alt="" />
        </div>
        <div class="po-doc__masthead-center">
          <div class="po-doc__masthead-company">{{ headerCompanyName }}</div>
          <div v-if="headerWarehouseAddress" class="po-doc__masthead-warehouse-addr">{{ headerWarehouseAddress }}</div>
          <div class="po-doc__masthead-title-gap" aria-hidden="true"></div>
          <div class="po-doc__masthead-title">{{ docTitle }}</div>
          <div v-if="docSubtitle" class="po-doc__masthead-sub">{{ docSubtitle }}</div>
        </div>
      </div>
      <div class="po-doc__masthead-meta">
        <div><span class="po-doc__k">{{ labels.date }}</span>{{ docDate }}</div>
        <div class="po-doc__masthead-meta-line po-doc__masthead-meta-line--nowrap">
          <span class="po-doc__k">{{ labels.packingNo }}</span>{{ docNo }}
        </div>
        <div class="po-doc__masthead-meta-line po-doc__masthead-meta-line--nowrap">
          <span class="po-doc__k">{{ labels.shipMethod }}</span>{{ shipmentMethodDisplay }}
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
          <tr v-if="lines.length > 0" class="po-doc__sum-row">
            <td>{{ labels.total }}</td>
            <td colspan="2"></td>
            <td class="num">{{ totalQty }}</td>
            <td colspan="2"></td>
          </tr>
        </tbody>
      </table>

    <section v-if="withShipmentInspection" class="po-doc__qc">
      <div class="po-doc__panel po-doc__panel--qc">
        <div class="po-doc__addon-bar">{{ labels.outboundInspection }}</div>
        <table class="po-doc__qc-grid">
          <thead>
            <tr>
              <th class="w-qc-i">{{ labels.no }}</th>
              <th class="w-qc-item">{{ labels.item }}</th>
              <th class="w-qc-j">{{ labels.result }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(item, idx) in labels.qcItems" :key="'qc' + idx">
              <td class="cen">{{ idx + 1 }}</td>
              <td class="qc-item-cell">{{ item }}</td>
              <td>&nbsp;</td>
            </tr>
          </tbody>
        </table>
      </div>
      <div class="po-doc__qc-foot">
        <span>{{ labels.qcInspector }}</span>
        <span>{{ labels.qcDate }}</span>
      </div>
    </section>

    <section class="po-doc__addon po-doc__panel">
      <div class="po-doc__addon-bar">{{ labels.remarks }}</div>
      <div class="po-doc__addon-body">
        <div v-for="(t, i) in notes" :key="'n' + i" class="po-doc__term-line">{{ t }}</div>
      </div>
    </section>

    <section class="po-doc__sign">
      <div class="po-doc__sign-t po-doc__sign-t--left">{{ labels.shipperSign }}</div>
      <div class="po-doc__sign-t po-doc__sign-t--right">{{ labels.consigneeSign }}</div>
      <div class="po-doc__sign-pad po-doc__sign-pad--left po-doc__sign-pad--seal">
        <img v-if="showSeal && sealUrl" class="po-doc__seal" :src="sealUrl" alt="" />
      </div>
      <div class="po-doc__sign-pad po-doc__sign-pad--right"></div>
      <div class="po-doc__sign-foot po-doc__sign-foot--left">{{ labels.date }} {{ signDate }}</div>
      <div class="po-doc__sign-foot po-doc__sign-foot--right">{{ labels.date }}</div>
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
/* 分区/表头底色 + 黑框；公司名深色 */
$po-orange: #a8d070; /* Bill/Ship、明细表头、QC/Remarks 条 */
$po-border: #222;
$po-head-fg: #111;
$po-company: #101010;
$po-radius: 6px;

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

.po-doc__body {
  margin-top: 0;
}

.po-doc__masthead-company {
  font-size: 16pt;
  font-weight: 700;
  letter-spacing: 0.02em;
  color: $po-company;
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

.po-doc__masthead-title {
  font-size: 17pt;
  font-weight: 400;
  margin-top: 0;
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
  border-collapse: separate;
  border-spacing: 0;
  margin-bottom: 10px;
  table-layout: fixed;
  border-radius: $po-radius;
  overflow: hidden;
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
  border-top: none;
  padding: 8px 10px;
  vertical-align: top;
  font-size: 9.5pt;
}

/* Bill To / Ship To：表头与内容区均去掉中间竖线 */
.po-doc__tri--addr th:first-child,
.po-doc__tri--addr tbody td:first-child {
  border-right: none;
}

.po-doc__tri--addr th:last-child,
.po-doc__tri--addr tbody td:last-child {
  border-left: none;
}

.po-doc__tri--addr thead th:first-child {
  border-top-left-radius: $po-radius;
}

.po-doc__tri--addr thead th:last-child {
  border-top-right-radius: $po-radius;
}

.po-doc__tri--addr tbody td:first-child {
  border-bottom-left-radius: $po-radius;
}

.po-doc__tri--addr tbody td:last-child {
  border-bottom-right-radius: $po-radius;
}

.po-doc__tri-line {
  margin-bottom: 4px;
}

.po-doc__tri-line:last-child {
  margin-bottom: 0;
}

.po-doc__grid {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
  /* 明细表与下方区块间距 */
  margin-bottom: 15px;
  table-layout: fixed;
  border-radius: $po-radius;
  overflow: hidden;
}

.po-doc__grid th,
.po-doc__grid td {
  border: 1px solid $po-border;
  border-top: none;
  border-left: none;
  padding: 4px 5px;
  vertical-align: middle;
  word-break: break-all;
}

.po-doc__grid th:first-child,
.po-doc__grid td:first-child {
  border-left: 1px solid $po-border;
}

.po-doc__grid thead th {
  background: $po-orange;
  color: $po-head-fg;
  font-weight: 700;
  text-align: center;
  font-size: 8.6pt;
  padding: 6px 3px;
  border-top: 1px solid $po-border;
}

.po-doc__grid thead th:first-child {
  border-top-left-radius: $po-radius;
}

.po-doc__grid thead th:last-child {
  border-top-right-radius: $po-radius;
}

.po-doc__grid tbody tr:last-child td:first-child {
  border-bottom-left-radius: $po-radius;
}

.po-doc__grid tbody tr:last-child td:last-child {
  border-bottom-right-radius: $po-radius;
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

.po-doc__panel {
  border: 1px solid $po-border;
  border-radius: $po-radius;
  overflow: hidden;
}

.po-doc__qc {
  margin-bottom: 12px;
}

/* Outbound Inspection 标题条：上圆角（绿）、下直角（红）贴合下方表格 */
.po-doc__panel--qc .po-doc__addon-bar {
  border: none;
  border-bottom: 1px solid $po-border;
  border-radius: $po-radius $po-radius 0 0;
}

.po-doc__qc-grid {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
  table-layout: fixed;
  margin-top: 0;
  border-radius: 0;
}

.po-doc__qc-grid th,
.po-doc__qc-grid td {
  border: none;
  border-bottom: 1px solid $po-border;
  border-right: 1px solid $po-border;
  padding: 5px 4px;
  font-size: 9pt;
  vertical-align: middle;
}

.po-doc__qc-grid th:last-child,
.po-doc__qc-grid td:last-child {
  border-right: none;
}

.po-doc__qc-grid tbody tr:last-child td {
  border-bottom: none;
}

.po-doc__qc-grid thead th {
  background: #e5f5d3;
  font-weight: 700;
  text-align: center;
  border-top: none;
  border-radius: 0;
}

.w-qc-i {
  width: 8%;
}
.w-qc-item {
  width: 72%;
}
.w-qc-j {
  width: 20%;
}

.qc-item-cell {
  font-size: 9pt;
  line-height: 1.45;
  text-align: justify;
}

.po-doc__qc-foot {
  display: flex;
  justify-content: space-between;
  margin-top: 8px;
  font-size: 9.5pt;
  padding: 0 4px;
}

.po-doc__addon {
  margin-top: 6px;
  margin-bottom: 14px;
}

.po-doc__panel .po-doc__addon-bar {
  border: none;
  border-bottom: 1px solid $po-border;
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

.po-doc__panel .po-doc__addon-body {
  border: none;
}

.po-doc__addon-body {
  border: 1px solid $po-border;
  padding: 10px 12px 12px;
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
