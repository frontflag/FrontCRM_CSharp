<template>
  <div class="po-doc po-doc--idesemi">
    <header class="po-doc__masthead">
      <div class="po-doc__brand-bar">
        <div class="po-doc__brand-bar-inner">
          <img v-if="logoUrl" class="po-doc__logo" :src="logoUrl" alt="" />
          <div class="po-doc__brand-text">
            <div class="po-doc__masthead-company">{{ headerCompanyName }}</div>
          </div>
        </div>
        <div class="po-doc__amber-line" aria-hidden="true" />
      </div>
      <div class="po-doc__masthead-row">
        <div class="po-doc__title-block">
          <div class="po-doc__masthead-title">销售订单</div>
        </div>
        <aside class="po-doc__meta-card">
          <div><span class="po-doc__k">日期：</span>{{ orderDate }}</div>
          <div class="po-doc__meta-line--nowrap">
            <span class="po-doc__k">单号：</span>{{ orderCode }}
          </div>
        </aside>
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
          <div class="po-doc__addr-col">
            <div class="po-doc__addr-hd">卖方（供方）</div>
            <div class="po-doc__addr-body">
              <div><span class="po-doc__lbl">公司名称：</span>{{ partySeller.name }}</div>
              <div><span class="po-doc__lbl">公司地址：</span>{{ partySeller.address }}</div>
              <div><span class="po-doc__lbl">联系电话：</span>{{ partySeller.phone }}</div>
              <div><span class="po-doc__lbl">业务员：</span>{{ partySeller.consignee }}</div>
            </div>
          </div>
          <div class="po-doc__addr-col">
            <div class="po-doc__addr-hd">买方（客户）</div>
            <div class="po-doc__addr-body">
              <div><span class="po-doc__lbl">公司名称：</span>{{ partyBuyer.name }}</div>
              <div><span class="po-doc__lbl">地址：</span>{{ partyBuyer.address }}</div>
            </div>
          </div>
          <div class="po-doc__addr-col">
            <div class="po-doc__addr-hd">交付</div>
            <div class="po-doc__addr-body">
              <div><span class="po-doc__lbl">运输方式：</span>{{ deliveryMode }}</div>
              <div><span class="po-doc__lbl">最晚交期：</span>{{ deliveryDate }}</div>
            </div>
          </div>
        </div>
      </template>
      <template #addon-bar>
        <div class="po-doc__section-title">附加信息</div>
      </template>
      <template #sign>
        <div class="po-doc__sign-col">
          <div class="po-doc__sign-t">卖方（供方签章）</div>
          <div class="po-doc__sign-pad po-doc__sign-pad--seal">
            <img v-if="showSeal && sealUrl" class="po-doc__seal" :src="sealUrl" alt="" />
          </div>
          <div class="po-doc__sign-date">日期：{{ sellerSignDate }}</div>
        </div>
        <div class="po-doc__sign-col">
          <div class="po-doc__sign-t">买方（客户签章）</div>
          <div class="po-doc__sign-pad"></div>
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
/* 与 Packing/Invoice Idesemi 同源；挂到 semicore */
.po-doc--idesemi {
  $ide-purple: #2d1b4e;
  $ide-purple-deep: #1a0a2e;
  $ide-header-bg: #0d1f35;
  $ide-accent: #020612;
  $ide-border: #c4b5d4;
  $ide-text: #1f1235;

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
  }

  .po-doc__masthead-company {
    font-size: 14pt;
    font-weight: 700;
    letter-spacing: 0.04em;
    color: #fff;
  }

  .po-doc__amber-line {
    height: 3px;
    background: linear-gradient(90deg, $ide-accent 0%, #0a1628 45%, transparent 100%);
  }

  .po-doc__masthead-row {
    display: grid;
    grid-template-columns: 1fr 48mm;
    gap: 5mm;
    align-items: start;
    margin-bottom: 5mm;
  }

  .po-doc__masthead-title {
    font-size: 16pt;
    font-weight: 700;
    letter-spacing: 0.2em;
    color: $ide-purple-deep;
  }

  .po-doc__meta-card {
    border: 1px solid $ide-border;
    background: #faf7ff;
    padding: 3mm 3.5mm;
    font-size: 9pt;
    line-height: 1.65;
    border-left: 3px solid $ide-accent;
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
    grid-template-columns: 1.2fr 1fr 0.85fr;
    gap: 3mm;
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
    background: $ide-accent;
    border-radius: 1px;
  }

  .po-doc__addr-body {
    padding: 6px 10px;
    font-size: 9.5pt;
  }

  .po-doc__addr-body > div {
    margin-bottom: 3px;
  }

  .po-doc__lbl {
    font-weight: 600;
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
    border: 1px solid $ide-border;
    padding: 5px 10px;
  }

  .po-doc__fin-lbl {
    width: 42%;
    font-weight: 600;
    background: #f3eef9;
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
    color: #6b5a7e;
  }

  .po-doc__section-title {
    font-weight: 700;
    font-size: 10.5pt;
    color: $ide-purple-deep;
    padding: 5px 0 5px 8px;
    border-left: 3px solid $ide-accent;
  }

  .po-doc__addon {
    margin-top: 6px;
    margin-bottom: 12px;
  }

  .po-doc__addon-body {
    border: 1px solid $ide-border;
    padding: 8px 12px 10px;
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
    column-gap: 6mm;
    margin-top: 7mm;
    font-size: 9.5pt;
  }

  .po-doc__sign-col {
    border: 1px solid $ide-border;
    padding: 4mm;
    background: #fcfaff;
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

  .po-doc__sign-date {
    font-size: 9pt;
    color: #5b4a6e;
  }
}

@media print {
  .po-doc--idesemi {
    width: auto;
    min-height: auto;
    margin: 0;
    padding: 8mm 10mm;

    .po-doc__brand-bar {
      margin: -8mm -10mm 4mm;
      padding: 5mm 10mm 0;
    }
  }
}
</style>
