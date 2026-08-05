<template>
  <div class="wty-doc wty-doc--semicore">
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
/* ecoinf 租户：绿表点缀 */
.wty-doc--semicore {
  $accent: #a8d070;
  $ink: #101010;
  $border: #222;

  width: 210mm;
  min-height: 297mm;
  margin: 0 auto;
  padding: 12mm 14mm 14mm;
  box-sizing: border-box;
  background: #fff;
  color: $ink;
  font-size: 10.5pt;
  line-height: 1.55;
  font-family: 'Microsoft YaHei', 'SimSun', system-ui, sans-serif;

  .wty-doc__top {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 6mm;
    margin-bottom: 6mm;
  }

  .wty-doc__logo {
    max-height: 14mm;
    max-width: 28mm;
    object-fit: contain;
  }

  .wty-doc__top-right {
    text-align: right;
    max-width: 110mm;
  }

  .wty-doc__company {
    font-size: 12pt;
    font-weight: 700;
    color: $ink;
  }

  .wty-doc__company-addr {
    margin-top: 2px;
    font-size: 8.5pt;
    color: #333;
    line-height: 1.4;
  }

  .wty-doc__title-block {
    text-align: center;
    margin-bottom: 6mm;
  }

  .wty-doc__title {
    font-size: 18pt;
    font-weight: 700;
    letter-spacing: 0.35em;
    text-indent: 0.35em;
  }

  .wty-doc__subtitle {
    margin-top: 4px;
    font-size: 12pt;
    font-weight: 600;
    letter-spacing: 0.06em;
  }

  .wty-doc__parties {
    margin-bottom: 4mm;
    font-size: 10.5pt;
  }

  .wty-doc__parties > div {
    margin-bottom: 3px;
  }

  .wty-doc__lbl {
    font-weight: 700;
  }

  .wty-doc__intro {
    margin: 0 0 4mm;
    text-align: justify;
    text-indent: 2em;
  }

  .wty-doc__notes-hd {
    font-weight: 700;
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
    border: 1px solid $border;
    padding: 5px 4px;
    font-size: 9.5pt;
    word-break: break-all;
  }

  .wty-doc__grid thead th {
    background: $accent;
    font-weight: 700;
    text-align: center;
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
    color: #666;
    padding: 12px !important;
  }

  .wty-doc__sign {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 10mm;
    margin-top: 4mm;
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
    min-height: 22mm;
    margin-top: 4mm;
    position: relative;
  }

  .wty-doc__sign-pad--seal {
    background: #fff;
    isolation: isolate;
  }

  .wty-doc__seal {
    position: absolute;
    left: 0;
    bottom: 0;
    max-height: 24mm;
    max-width: 30mm;
    object-fit: contain;
  }
}

@media print {
  .wty-doc--semicore {
    width: auto;
    min-height: auto;
    margin: 0;
    padding: 8mm 10mm;
  }
}
</style>
