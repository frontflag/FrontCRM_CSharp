<template>
  <div class="wty-doc wty-doc--ecoinf">
    <SalesOrderWarrantyReportBody v-bind="props" />
  </div>
</template>

<script setup lang="ts">
import SalesOrderWarrantyReportBody from '../SalesOrderWarrantyReportBody.vue'
import {
  salesOrderWarrantyReportDocumentPropDefaults,
  type SalesOrderWarrantyReportDocumentProps
} from '../types'

const props = withDefaults(
  defineProps<SalesOrderWarrantyReportDocumentProps>(),
  salesOrderWarrantyReportDocumentPropDefaults
)
</script>

<style lang="scss">
/* idesemi 租户：工业极简 + 青点缀 */
.wty-doc--ecoinf {
  $accent: #6dc5f6;
  $title: #11161f;
  $ink: #111;
  $muted: #525252;
  $line: #d4d4d4;
  $zebra: #f5f5f5;

  width: 210mm;
  min-height: 297mm;
  margin: 0 auto;
  padding: 12mm 14mm 14mm;
  box-sizing: border-box;
  background: #fff;
  color: $ink;
  font-size: 10.5pt;
  line-height: 1.5;
  font-family: 'IBM Plex Sans', 'Segoe UI', 'Microsoft YaHei', system-ui, sans-serif;

  .wty-doc__top {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 6mm;
    margin-bottom: 4mm;
    padding-bottom: 3mm;
    border-bottom: 2px solid $ink;
  }

  .wty-doc__logo {
    max-height: 14mm;
    max-width: 32mm;
    object-fit: contain;
  }

  .wty-doc__top-right {
    text-align: right;
    order: -1;
    flex: 1;
  }

  .wty-doc__top-left {
    order: 1;
  }

  .wty-doc__company {
    font-size: 10pt;
    font-weight: 600;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: $muted;
    text-align: left;
  }

  .wty-doc__company-addr {
    margin-top: 2px;
    font-size: 8.5pt;
    color: $muted;
    text-align: left;
    line-height: 1.35;
  }

  .wty-doc__title-block {
    text-align: left;
    margin-bottom: 5mm;
  }

  .wty-doc__title {
    font-size: 18pt;
    font-weight: 700;
    letter-spacing: 0.2em;
    color: $title;
  }

  .wty-doc__subtitle {
    margin-top: 3px;
    font-size: 11pt;
    color: $muted;
  }

  .wty-doc__parties {
    margin-bottom: 4mm;
  }

  .wty-doc__parties > div {
    margin-bottom: 3px;
  }

  .wty-doc__lbl {
    font-weight: 700;
  }

  .wty-doc__lbl::before {
    content: '';
    display: inline-block;
    width: 3px;
    height: 0.9em;
    background: $accent;
    margin-right: 6px;
    vertical-align: -0.1em;
  }

  .wty-doc__intro {
    margin: 0 0 4mm;
    text-align: justify;
    text-indent: 2em;
  }

  .wty-doc__notes-hd {
    font-weight: 700;
    letter-spacing: 0.08em;
    margin-bottom: 2mm;
  }

  .wty-doc__notes-list {
    margin: 0 0 4mm;
    padding-left: 1.6em;
  }

  .wty-doc__notes-list li {
    margin-bottom: 2px;
    text-align: justify;
  }

  .wty-doc__notes-after {
    margin: 0 0 3mm;
    text-align: justify;
  }

  .wty-doc__grid {
    width: 100%;
    border-collapse: collapse;
    margin-bottom: 8mm;
    table-layout: fixed;
  }

  .wty-doc__grid th,
  .wty-doc__grid td {
    border: none;
    border-bottom: 1px solid $line;
    padding: 5px 4px;
    font-size: 9.5pt;
    word-break: break-all;
  }

  .wty-doc__grid thead th {
    background: transparent;
    font-weight: 700;
    text-align: left;
    font-size: 8.5pt;
    letter-spacing: 0.04em;
    text-transform: uppercase;
    border-top: 2px solid $ink;
    border-bottom: 2px solid $ink;
  }

  .wty-doc__grid tbody tr:nth-child(odd) td {
    background: $zebra;
  }

  .w-pn {
    width: 18%;
  }
  .w-brand {
    width: 14%;
  }
  .w-qty {
    width: 12%;
  }
  .w-dc {
    width: 14%;
  }
  .w-cpn {
    width: 20%;
  }
  .w-cso {
    width: 22%;
  }

  .cen {
    text-align: center;
  }

  .wty-doc__empty {
    text-align: center;
    color: $muted;
    padding: 12px !important;
  }

  .wty-doc__sign {
    display: grid;
    grid-template-columns: 1fr 1fr;
    column-gap: 14mm;
  }

  .wty-doc__sign-party {
    font-weight: 700;
    margin-bottom: 3mm;
  }

  .wty-doc__sign-line {
    margin-bottom: 2px;
    font-size: 9.5pt;
  }

  .wty-doc__sign-pad {
    min-height: 18mm;
    margin-top: 6mm;
    border-bottom: 1.5px solid $ink;
    position: relative;
  }

  .wty-doc__sign-pad--seal {
    isolation: isolate;
  }

  .wty-doc__seal {
    position: absolute;
    left: 0;
    bottom: 2px;
    max-height: 20mm;
    max-width: 26mm;
    object-fit: contain;
  }
}

@media print {
  .wty-doc--ecoinf {
    width: auto;
    min-height: auto;
    margin: 0;
    padding: 8mm 10mm;
  }
}
</style>
