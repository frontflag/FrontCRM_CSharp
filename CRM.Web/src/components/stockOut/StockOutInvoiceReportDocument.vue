<template>
  <div class="po-doc">
    <header class="po-doc__masthead">
      <div class="po-doc__masthead-left">
        <img v-if="logoUrl" class="po-doc__logo" :src="logoUrl" alt="" />
      </div>
      <div class="po-doc__masthead-center">
        <div class="po-doc__masthead-company">{{ headerCompanyName }}</div>
        <div class="po-doc__masthead-title">{{ invoiceTitle }}</div>
        <div v-if="invoiceSubtitle" class="po-doc__masthead-sub">{{ invoiceSubtitle }}</div>
      </div>
      <div class="po-doc__masthead-right">
        <div><span class="po-doc__k">日期：</span>{{ invoiceDate }}</div>
        <div><span class="po-doc__k">Invoice No.：</span>{{ invoiceNo }}</div>
      </div>
    </header>

    <table class="po-doc__tri">
      <thead>
        <tr>
          <th>Exporter / 供方</th>
          <th>Consignee / 收货方</th>
          <th>Shipment / 出货信息</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td class="po-doc__tri-cell">
            <div v-for="(t, i) in exporterLines" :key="'ex' + i" class="po-doc__tri-line">{{ t }}</div>
          </td>
          <td class="po-doc__tri-cell">
            <div v-for="(t, i) in consigneeLines" :key="'cn' + i" class="po-doc__tri-line">{{ t }}</div>
          </td>
          <td class="po-doc__tri-cell">
            <div v-for="(t, i) in shipmentLines" :key="'sh' + i" class="po-doc__tri-line">{{ t }}</div>
          </td>
        </tr>
      </tbody>
    </table>

    <table class="po-doc__grid">
      <thead>
        <tr>
          <th class="w-inv-idx">序号</th>
          <th class="w-inv-ref">参考号 / Ref.</th>
          <th class="w-inv-desc">品名及规格 / Description</th>
          <th class="w-inv-qty num">数量 / Qty</th>
          <th class="w-inv-price num">单价 / U.P.</th>
          <th class="w-inv-amt num">金额 / Amount</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="line in lines" :key="'l' + line.index">
          <td class="cen">{{ line.index }}</td>
          <td>{{ line.ref }}</td>
          <td>{{ line.description }}</td>
          <td class="num">{{ showAmounts ? line.qty : '—' }}</td>
          <td class="num">{{ showAmounts ? line.unitPrice : '—' }}</td>
          <td class="num">{{ showAmounts ? line.amount : '—' }}</td>
        </tr>
        <tr v-for="i in fillerRowCount" :key="'f' + i">
          <td v-for="c in 6" :key="`${i}-${c}`">&nbsp;</td>
        </tr>
        <tr v-if="lines.length === 0">
          <td colspan="6" class="po-doc__empty">暂无明细</td>
        </tr>
        <tr v-else class="po-doc__hint-row">
          <td colspan="6" class="po-doc__hint">以下空白</td>
        </tr>
        <tr v-if="showAmounts && lines.length > 0" class="po-doc__sum-row">
          <td>总计 / Total</td>
          <td colspan="2"></td>
          <td class="num">{{ totalQty }}</td>
          <td></td>
          <td class="num">{{ totalAmount }}</td>
        </tr>
      </tbody>
    </table>

    <div v-if="showAmounts" class="po-doc__finance-wrap">
      <table class="po-doc__finance">
        <tbody>
          <tr>
            <td class="po-doc__fin-lbl">合计金额 / Grand Total（{{ currencyLabel }}）</td>
            <td class="num po-doc__fin-grand">{{ grandTotal }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <section class="po-doc__addon">
      <div class="po-doc__addon-bar">银行账户 / Bank details</div>
      <div class="po-doc__addon-body">
        <div v-for="(t, i) in bankLines" :key="'b' + i" class="po-doc__addon-line">{{ t }}</div>
        <div class="po-doc__addon-terms-hd">备注 / Remarks</div>
        <div v-for="(t, i) in remarkLines" :key="'r' + i" class="po-doc__addon-line">{{ t }}</div>
        <div class="po-doc__addon-terms-hd">条款 / Terms</div>
        <div v-for="(t, i) in terms" :key="'t' + i" class="po-doc__term-line">{{ t }}</div>
      </div>
    </section>

    <section class="po-doc__sign">
      <div class="po-doc__sign-cell">
        <div class="po-doc__sign-t">Exporter（供方签章）</div>
        <div class="po-doc__sign-pad po-doc__sign-pad--seal">
          <img v-if="showSeal && sealUrl" class="po-doc__seal" :src="sealUrl" alt="" />
        </div>
        <div>日期：{{ signDate }}</div>
      </div>
      <div class="po-doc__sign-cell po-doc__sign-cell--buyer">
        <div class="po-doc__sign-t">Consignee（收货方签章）</div>
        <div class="po-doc__sign-pad"></div>
        <div class="po-doc__sign-date">日期：</div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

export interface StockOutInvoiceLineVm {
  index: number
  ref: string
  description: string
  qty: string
  unitPrice: string
  amount: string
}

const props = withDefaults(
  defineProps<{
    headerCompanyName: string
    invoiceTitle: string
    invoiceSubtitle?: string
    invoiceNo: string
    invoiceDate: string
    exporterLines: string[]
    consigneeLines: string[]
    shipmentLines: string[]
    currencyLabel: string
    lines: StockOutInvoiceLineVm[]
    totalQty: string
    totalAmount: string
    grandTotal: string
    bankLines: string[]
    remarkLines: string[]
    terms: string[]
    sealUrl: string | null
    logoUrl: string | null
    showAmounts: boolean
    showSeal: boolean
    signDate: string
  }>(),
  {
    invoiceSubtitle: '',
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
.w-inv-ref {
  width: 18%;
}
.w-inv-desc {
  width: 34%;
}
.w-inv-qty {
  width: 12%;
}
.w-inv-price {
  width: 14%;
}
.w-inv-amt {
  width: 14%;
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
  width: 58%;
  font-weight: 600;
}

.po-doc__fin-grand {
  font-weight: 700;
  font-size: 10.5pt;
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
}

.po-doc__sign-date {
  text-align: right;
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
