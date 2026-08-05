<template>
  <div class="po-doc po-doc--ecoinf">
    <header class="po-doc__masthead">
      <div class="po-doc__masthead-top">
        <div class="po-doc__title-stack">
          <div class="po-doc__masthead-title">销售订单</div>
          <div class="po-doc__masthead-company">{{ headerCompanyName }}</div>
        </div>
        <div class="po-doc__masthead-logo-wrap">
          <img v-if="logoUrl" class="po-doc__logo" :src="logoUrl" alt="" />
        </div>
      </div>
      <div class="po-doc__meta-row">
        <div><span class="po-doc__k">日期：</span>{{ orderDate }}</div>
        <div class="po-doc__meta-line--nowrap">
          <span class="po-doc__k">单号：</span>{{ orderCode }}
        </div>
      </div>
    </header>

    <SalesOrderReportBody
      :lines="lines"
      :show-amounts="showAmounts"
      :total-qty="totalQty"
      :total-incl="totalIncl"
      :excl-tax="exclTax"
      :tax-amount="taxAmount"
      :grand-incl="grandIncl"
      :tax-rate-label="taxRateLabel"
      :currency-label="currencyLabel"
      :extra-lines="extraLines"
      :terms="terms"
    >
      <template #parties>
        <div class="po-doc__addr">
          <section class="po-doc__addr-block">
            <div class="po-doc__section-label">卖方（供方）</div>
            <div class="po-doc__addr-body">
              <div><span class="po-doc__lbl">公司名称：</span>{{ partySeller.name }}</div>
              <div><span class="po-doc__lbl">公司地址：</span>{{ partySeller.address }}</div>
              <div><span class="po-doc__lbl">联系电话：</span>{{ partySeller.phone }}</div>
              <div><span class="po-doc__lbl">业务员：</span>{{ partySeller.consignee }}</div>
            </div>
          </section>
          <section class="po-doc__addr-block">
            <div class="po-doc__section-label">买方（客户）</div>
            <div class="po-doc__addr-body">
              <div><span class="po-doc__lbl">公司名称：</span>{{ partyBuyer.name }}</div>
              <div><span class="po-doc__lbl">地址：</span>{{ partyBuyer.address }}</div>
            </div>
          </section>
          <section class="po-doc__addr-block">
            <div class="po-doc__section-label">交付</div>
            <div class="po-doc__addr-body">
              <div><span class="po-doc__lbl">运输方式：</span>{{ deliveryMode }}</div>
              <div><span class="po-doc__lbl">最晚交期：</span>{{ deliveryDate }}</div>
            </div>
          </section>
        </div>
      </template>
      <template #addon-bar>
        <div class="po-doc__section-label">附加信息</div>
      </template>
      <template #sign>
        <div class="po-doc__sign-col">
          <div class="po-doc__sign-t">卖方（供方签章）</div>
          <div class="po-doc__sign-line po-doc__sign-pad--seal">
            <img v-if="showSeal && sealUrl" class="po-doc__seal" :src="sealUrl" alt="" />
          </div>
          <div class="po-doc__sign-date">日期：{{ sellerSignDate }}</div>
        </div>
        <div class="po-doc__sign-col">
          <div class="po-doc__sign-t">买方（客户签章）</div>
          <div class="po-doc__sign-line"></div>
          <div class="po-doc__sign-date">日期：</div>
        </div>
      </template>
    </SalesOrderReportBody>
  </div>
</template>

<script setup lang="ts">
import SalesOrderReportBody from '../SalesOrderReportBody.vue'
import {
  salesOrderReportDocumentPropDefaults,
  type SalesOrderReportDocumentProps
} from '../types'

withDefaults(defineProps<SalesOrderReportDocumentProps>(), salesOrderReportDocumentPropDefaults)
</script>

<style lang="scss">
/* 与 Packing/Invoice Ecoinf 工业极简同源；挂到 idesemi */
.po-doc--ecoinf {
  $eco-accent: #6dc5f6;
  $eco-title: #11161f;
  $eco-ink: #111;
  $eco-muted: #525252;
  $eco-line: #d4d4d4;
  $eco-zebra: #f5f5f5;

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
    flex: 1;
    min-width: 0;
  }

  .po-doc__masthead-title {
    font-size: 20pt;
    font-weight: 700;
    letter-spacing: 0.28em;
    text-indent: 0.08em;
    line-height: 1.15;
    color: $eco-title;
  }

  .po-doc__masthead-company {
    margin-top: 3mm;
    font-size: 9.5pt;
    font-weight: 600;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: $eco-muted;
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
  }

  .po-doc__k {
    font-weight: 700;
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
    letter-spacing: 0.12em;
    color: $eco-ink;
    margin: 0 0 2.5mm;
  }

  .po-doc__section-label::before {
    content: '';
    width: 3px;
    height: 11px;
    background: $eco-accent;
    flex-shrink: 0;
  }

  .po-doc__addr {
    display: grid;
    grid-template-columns: 1.2fr 1fr 0.85fr;
    gap: 3mm;
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

  .po-doc__addr-body > div {
    margin-bottom: 2px;
  }

  .po-doc__lbl {
    font-weight: 600;
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
    font-size: 8pt;
    letter-spacing: 0.04em;
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

  .w-idx {
    width: 7%;
  }
  .w-name {
    width: 14%;
  }
  .w-spec {
    width: 18%;
  }
  .w-brand {
    width: 9%;
  }
  .w-unit {
    width: 6%;
  }
  .w-cur {
    width: 7%;
  }
  .w-qty {
    width: 9%;
  }
  .w-price {
    width: 11%;
  }
  .w-tax {
    width: 7%;
  }
  .w-amt {
    width: 12%;
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
  }

  .po-doc__sum-row td {
    font-weight: 700;
    border-top: 2px solid $eco-ink;
    border-bottom: 2px solid $eco-ink;
    padding: 7px 4px;
  }

  .po-doc__finance-wrap {
    display: flex;
    justify-content: flex-end;
    margin-bottom: 12px;
  }

  .po-doc__finance {
    border-collapse: collapse;
    min-width: 52%;
    font-size: 9.5pt;
  }

  .po-doc__finance td {
    border: 1px solid $eco-line;
    padding: 5px 10px;
  }

  .po-doc__fin-lbl {
    width: 42%;
    font-weight: 600;
  }

  .po-doc__fin-grand {
    font-weight: 700;
    font-size: 10.5pt;
  }

  .po-doc__fin-cur {
    text-align: right;
  }

  .po-doc__fin-sep {
    margin: 0 6px;
    color: $eco-muted;
  }

  .po-doc__addon {
    margin-bottom: 8mm;
  }

  .po-doc__addon-body {
    border: 1px solid $eco-line;
    padding: 8px 10px;
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

  .po-doc__confirm {
    margin: 12px 0 0;
    font-weight: 700;
    font-size: 10pt;
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

  .po-doc__sign-date {
    font-size: 9pt;
    color: $eco-muted;
  }
}

@media print {
  .po-doc--ecoinf {
    width: auto;
    min-height: auto;
    margin: 0;
    padding: 8mm 10mm;
  }
}
</style>
