<template>
  <div class="wty-doc wty-doc--idesemi">
    <div class="wty-doc__brand-bar">
      <div class="wty-doc__brand-bar-inner">
        <img v-if="logoUrl" class="wty-doc__logo" :src="logoUrl" alt="" />
        <div class="wty-doc__brand-text">
          <div class="wty-doc__company">{{ companyName }}</div>
          <div v-if="companyAddress" class="wty-doc__company-addr">{{ companyAddress }}</div>
        </div>
      </div>
      <div class="wty-doc__accent-line" aria-hidden="true" />
    </div>
    <SalesOrderWarrantyReportBody v-bind="bodyBind" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import SalesOrderWarrantyReportBody from '../SalesOrderWarrantyReportBody.vue'
import {
  salesOrderWarrantyReportDocumentPropDefaults,
  type SalesOrderWarrantyReportDocumentProps
} from '../types'

const props = withDefaults(
  defineProps<SalesOrderWarrantyReportDocumentProps>(),
  salesOrderWarrantyReportDocumentPropDefaults
)

/** 顶栏已展示公司信息，正文顶栏隐藏 logo/公司以免重复 */
const bodyBind = computed(() => ({
  ...props,
  logoUrl: null as string | null,
  companyName: '',
  companyAddress: ''
}))
</script>

<style lang="scss">
/* semicore 租户：深色顶栏 */
.wty-doc--idesemi {
  $header: #0d1f35;
  $accent: #020612;
  $border: #c4b5d4;
  $ink: #1f1235;

  width: 210mm;
  min-height: 297mm;
  margin: 0 auto;
  padding: 0 11mm 12mm;
  box-sizing: border-box;
  background: #fff;
  color: $ink;
  font-size: 10.5pt;
  line-height: 1.55;
  font-family: 'Segoe UI', 'Microsoft YaHei', system-ui, sans-serif;

  .wty-doc__brand-bar {
    margin: 0 -11mm 5mm;
    padding: 5mm 11mm 0;
    background: $header;
    color: #fff;
  }

  .wty-doc__brand-bar-inner {
    display: flex;
    align-items: center;
    gap: 4mm;
    padding-bottom: 3.5mm;
  }

  .wty-doc__logo {
    max-height: 12mm;
    max-width: 26mm;
    object-fit: contain;
    background: rgba(255, 255, 255, 0.92);
    padding: 2px 4px;
    border-radius: 2px;
  }

  .wty-doc__company {
    font-size: 13pt;
    font-weight: 700;
    color: #fff;
  }

  .wty-doc__company-addr {
    margin-top: 2px;
    font-size: 8.5pt;
    color: rgba(255, 255, 255, 0.82);
    line-height: 1.35;
  }

  .wty-doc__accent-line {
    height: 3px;
    background: linear-gradient(90deg, $accent 0%, #0a1628 45%, transparent 100%);
  }

  .wty-doc__top {
    display: none;
  }

  .wty-doc__title-block {
    text-align: left;
    margin-bottom: 5mm;
  }

  .wty-doc__title {
    font-size: 16pt;
    font-weight: 700;
    letter-spacing: 0.2em;
    color: $header;
  }

  .wty-doc__subtitle {
    margin-top: 3px;
    font-size: 11pt;
    font-weight: 600;
    color: #5b4a6e;
  }

  .wty-doc__parties {
    margin-bottom: 4mm;
  }

  .wty-doc__parties > div {
    margin-bottom: 3px;
  }

  .wty-doc__lbl {
    font-weight: 700;
    color: $header;
  }

  .wty-doc__intro {
    margin: 0 0 4mm;
    text-align: justify;
    text-indent: 2em;
  }

  .wty-doc__notes-hd {
    font-weight: 700;
    border-left: 3px solid $accent;
    padding-left: 8px;
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
    background: $header;
    color: #fff;
    font-weight: 700;
    text-align: center;
    border-bottom: 2.5px solid $accent;
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
    color: #6b5a7e;
    padding: 12px !important;
  }

  .wty-doc__sign {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 6mm;
  }

  .wty-doc__sign-col {
    border: 1px solid $border;
    padding: 4mm;
    background: #fcfaff;
  }

  .wty-doc__sign-party {
    font-weight: 700;
    margin-bottom: 3mm;
    color: $header;
  }

  .wty-doc__sign-line {
    margin-bottom: 2px;
    font-size: 9.5pt;
  }

  .wty-doc__sign-pad {
    min-height: 20mm;
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
    max-height: 22mm;
    max-width: 28mm;
    object-fit: contain;
  }
}

@media print {
  .wty-doc--idesemi {
    width: auto;
    min-height: auto;
    margin: 0;
    padding: 0 10mm 8mm;

    .wty-doc__brand-bar {
      margin: 0 -10mm 5mm;
      padding: 5mm 10mm 0;
    }
  }
}
</style>
