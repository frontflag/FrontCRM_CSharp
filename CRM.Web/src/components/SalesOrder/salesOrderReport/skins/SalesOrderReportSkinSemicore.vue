<template>
  <div class="po-doc po-doc--semicore">
    <header class="po-doc__masthead">
      <div class="po-doc__masthead-left">
        <img v-if="logoUrl" class="po-doc__logo" :src="logoUrl" alt="" />
      </div>
      <div class="po-doc__masthead-center">
        <div class="po-doc__masthead-company">{{ headerCompanyName }}</div>
        <div class="po-doc__masthead-title">销售订单</div>
      </div>
      <div class="po-doc__masthead-right">
        <div><span class="po-doc__k">日期：</span>{{ orderDate }}</div>
        <div><span class="po-doc__k">单号：</span>{{ orderCode }}</div>
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
        <table class="po-doc__tri">
          <thead>
            <tr>
              <th>卖方（供方）</th>
              <th>买方（客户）</th>
              <th>交付</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td class="po-doc__tri-cell">
                <div><span class="po-doc__lbl">公司名称：</span>{{ partySeller.name }}</div>
                <div><span class="po-doc__lbl">公司地址：</span>{{ partySeller.address }}</div>
                <div><span class="po-doc__lbl">联系电话：</span>{{ partySeller.phone }}</div>
                <div><span class="po-doc__lbl">业务员：</span>{{ partySeller.consignee }}</div>
              </td>
              <td class="po-doc__tri-cell">
                <div><span class="po-doc__lbl">公司名称：</span>{{ partyBuyer.name }}</div>
                <div><span class="po-doc__lbl">地址：</span>{{ partyBuyer.address }}</div>
              </td>
              <td class="po-doc__tri-cell">
                <div><span class="po-doc__lbl">运输方式：</span>{{ deliveryMode }}</div>
                <div><span class="po-doc__lbl">最晚交期：</span>{{ deliveryDate }}</div>
              </td>
            </tr>
          </tbody>
        </table>
      </template>
      <template #sign>
        <div class="po-doc__sign-cell po-doc__sign-cell--seller">
          <div class="po-doc__sign-t">卖方（供方签章）</div>
          <div class="po-doc__sign-pad po-doc__sign-pad--seal">
            <img v-if="showSeal && sealUrl" class="po-doc__seal" :src="sealUrl" alt="" />
          </div>
          <div class="po-doc__sign-date">日期：{{ sellerSignDate }}</div>
        </div>
        <div class="po-doc__sign-cell po-doc__sign-cell--buyer">
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
/* 与 Packing/Invoice Semicore 绿表同源；挂到 ecoinf */
.po-doc--semicore {
  $po-orange: #a8d070;
  $po-border: #222;
  $po-head-fg: #111;
  $po-company: #101010;

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

  .po-doc__masthead {
    display: grid;
    grid-template-columns: 22mm 1fr 42mm;
    align-items: start;
    gap: 4mm;
    margin-bottom: 8px;
    min-height: 28mm;
  }

  .po-doc__masthead-left {
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

  .po-doc__masthead-company {
    font-size: 16pt;
    font-weight: 700;
    letter-spacing: 0.02em;
    color: $po-company;
  }

  .po-doc__masthead-title {
    font-size: 15pt;
    font-weight: 700;
    margin-top: 4px;
    letter-spacing: 0.35em;
    text-indent: 0.35em;
  }

  .po-doc__masthead-right {
    font-size: 10pt;
    text-align: right;
    line-height: 1.65;
    padding-top: 4px;
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

  .po-doc__tri-cell > div {
    margin-bottom: 4px;
  }

  .po-doc__tri-cell > div:last-child {
    margin-bottom: 0;
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
    font-size: 9pt;
    padding: 6px 3px;
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
    border: 1px solid $po-border;
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

  .po-doc__fin-rate {
    margin-right: 4px;
  }

  .po-doc__fin-sep {
    margin: 0 6px;
    color: #666;
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

  .po-doc__confirm {
    margin: 12px 0 0;
    font-weight: 700;
    font-size: 10pt;
  }

  .po-doc__sign {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12mm;
    margin-top: 8mm;
    font-size: 9.5pt;
  }

  .po-doc__sign-t {
    font-weight: 600;
    margin-bottom: 4px;
  }

  .po-doc__sign-pad {
    min-height: 22mm;
    margin: 6px 0 8px;
    position: relative;
  }

  .po-doc__sign-pad--seal {
    min-height: 26mm;
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

  .po-doc__sign-cell--buyer {
    text-align: right;
  }

  .po-doc__sign-cell--buyer .po-doc__sign-t,
  .po-doc__sign-cell--buyer .po-doc__sign-pad {
    text-align: left;
  }

  .po-doc__sign-cell--buyer .po-doc__sign-pad {
    margin-left: auto;
    max-width: 38mm;
  }

  .po-doc__sign-date {
    text-align: right;
  }

  .po-doc__sign-cell--seller .po-doc__sign-date {
    text-align: left;
  }
}

@media print {
  .po-doc--semicore {
    width: auto;
    min-height: auto;
    margin: 0;
    padding: 8mm 10mm;
  }
}
</style>
