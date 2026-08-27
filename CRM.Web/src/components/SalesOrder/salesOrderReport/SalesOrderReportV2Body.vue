<!-- V2：chrome 对齐 PO V2；明细保留 V1 十列；条款为 5 条段落 -->
<template>
  <div class="po-v2">
    <header class="po-v2__head">
      <div class="po-v2__head-left">
        <div class="po-v2__logo-stack">
          <img v-if="logoUrl" class="po-v2__logo" :src="logoUrl" alt="" />
          <div v-else class="po-v2__logo-fallback">{{ headerCompanyName }}</div>
          <div class="po-v2__tagline">YOUR RELIABLE SUPPLIER</div>
        </div>
      </div>
      <div class="po-v2__head-right">
        <div class="po-v2__title-zh">销售订单</div>
        <div class="po-v2__title-en">SALES ORDER</div>
        <div class="po-v2__po-no">订单编号 / SO NO. {{ dash(orderCode) }}</div>
      </div>
    </header>
    <div class="po-v2__fade" aria-hidden="true" />

    <div class="po-v2__meta">
      <div class="po-v2__meta-cell">
        <div class="po-v2__meta-k">订单日期 / ORDER DATE</div>
        <div class="po-v2__meta-v">{{ dash(orderDate) }}</div>
      </div>
      <div class="po-v2__meta-cell">
        <div class="po-v2__meta-k">合同编号 / CONTRACT NO.</div>
        <div class="po-v2__meta-v">{{ dash(contractNo) }}</div>
      </div>
      <div class="po-v2__meta-cell">
        <div class="po-v2__meta-k">货币 / CURRENCY</div>
        <div class="po-v2__meta-v">{{ dash(currencyLabel) }}</div>
      </div>
      <div class="po-v2__meta-cell">
        <div class="po-v2__meta-k">付款条款 / PAYMENT</div>
        <div class="po-v2__meta-v">{{ dash(paymentTerms) }}</div>
      </div>
    </div>

    <section class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        交易方信息 / PARTIES
      </div>
      <div class="po-v2__parties">
        <div class="po-v2__party">
          <div class="po-v2__party-role">卖方（供方） / SELLER</div>
          <div class="po-v2__party-body">
            <div class="po-v2__party-line">公司名称：{{ dash(partySeller.name) }}</div>
            <div class="po-v2__party-line">公司地址：{{ dash(partySeller.address) }}</div>
            <div class="po-v2__party-line">联系电话：{{ dash(partySeller.phone) }}</div>
            <div class="po-v2__party-line">业务员：{{ dash(partySeller.consignee) }}</div>
          </div>
        </div>
        <div class="po-v2__party">
          <div class="po-v2__party-role">买方（客户） / BUYER</div>
          <div class="po-v2__party-body">
            <div class="po-v2__party-line">公司名称：{{ dash(partyBuyer.name) }}</div>
            <div class="po-v2__party-line">地址：{{ dash(partyBuyer.address) }}</div>
          </div>
        </div>
      </div>
    </section>

    <section class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        订购明细 / LINE ITEMS
      </div>
      <table class="po-v2__grid po-v2__grid--so10">
        <colgroup>
          <col class="c-so-idx" />
          <col class="c-so-name" />
          <col class="c-so-spec" />
          <col class="c-so-brand" />
          <col class="c-so-unit" />
          <col class="c-so-cur" />
          <col class="c-so-qty" />
          <col class="c-so-price" />
          <col class="c-so-tax" />
          <col class="c-so-amt" />
        </colgroup>
        <thead>
          <tr>
            <th>序号</th>
            <th>产品名称</th>
            <th>规格型号</th>
            <th>品牌</th>
            <th>单位</th>
            <th>币种</th>
            <th>数量</th>
            <th>单价（含税）</th>
            <th>税率</th>
            <th>合计金额（含税）</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="line in lines" :key="'l' + line.index">
            <td class="cen">{{ line.index }}</td>
            <td>{{ dash(line.productName) }}</td>
            <td>{{ dash(line.spec) }}</td>
            <td class="po-v2__brand">{{ dash(line.brand) }}</td>
            <td class="cen">{{ dash(line.unit) }}</td>
            <td class="cen">{{ dash(line.currency) }}</td>
            <td class="num">{{ showAmounts ? dash(line.qty) : '—' }}</td>
            <td class="num">{{ showAmounts ? dash(line.unitPrice) : '—' }}</td>
            <td class="num">{{ dash(line.taxRate) }}</td>
            <td class="num">{{ showAmounts ? dash(line.lineTotal) : '—' }}</td>
          </tr>
          <tr v-if="lines.length === 0">
            <td colspan="10" class="po-v2__empty">暂无明细</td>
          </tr>
        </tbody>
      </table>
    </section>

    <div class="po-v2__lower">
      <section class="po-v2__panel">
        <div class="po-v2__panel-hd po-v2__panel-hd--plain">交付信息 / DELIVERY</div>
        <div class="po-v2__kv">
          <span class="po-v2__k">交货地址 / Ship To</span>
          <span>{{ dash(shipTo) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">最晚交期 / Delivery</span>
          <span>{{ dash(deliveryDate) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">运输方式 / Transport</span>
          <span>{{ dash(deliveryMode) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">订单备注 / Remarks</span>
          <span>{{ dash(orderRemark) }}</span>
        </div>
      </section>
      <section class="po-v2__panel">
        <div class="po-v2__panel-hd">金额汇总 / SUMMARY</div>
        <div class="po-v2__kv">
          <span class="po-v2__k">小计 / Subtotal</span>
          <span class="num">{{ money(exclTax) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">税费 / Taxes</span>
          <span class="num">{{ money(taxAmount) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">运费 / Freight</span>
          <span class="num">{{ money(freightAmount) }}</span>
        </div>
        <div class="po-v2__kv po-v2__kv--total">
          <span>订单总额 / Total</span>
          <span class="num">{{ money(grandIncl) }}</span>
        </div>
      </section>
    </div>

    <section class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        服务条款 / SERVICE TERMS
      </div>
      <div class="po-v2__terms-plain">
        <div v-for="(t, i) in terms" :key="'t' + i" class="po-v2__term-line">{{ t }}</div>
      </div>
      <p class="po-v2__confirm">请在约定时间内确认本销售订单并签字/盖章，谢谢！</p>
    </section>

    <section class="po-v2__sign">
      <div class="po-v2__sign-box">
        <div class="po-v2__sign-t">卖方（供方签章） / Seller</div>
        <div class="po-v2__sign-name">公司名称：{{ dash(partySeller.name) }}</div>
        <div class="po-v2__sign-pad">
          <img v-if="showSeal && sealUrl" class="po-v2__seal" :src="sealUrl" alt="" />
        </div>
        <div class="po-v2__sign-rule" />
        <div class="po-v2__sign-lines">
          <span>授权代表签字：________________</span>
          <span>日期：{{ dash(sellerSignDate) }}</span>
        </div>
      </div>
      <div class="po-v2__sign-box">
        <div class="po-v2__sign-t">买方（客户签章） / Buyer</div>
        <div class="po-v2__sign-name">公司名称：{{ dash(partyBuyer.name) }}</div>
        <div class="po-v2__sign-pad">
          <span class="po-v2__seal-hint">（盖章）</span>
        </div>
        <div class="po-v2__sign-rule" />
        <div class="po-v2__sign-lines">
          <span>授权代表签字：________________</span>
          <span>日期：____________</span>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import {
  salesOrderReportDocumentPropDefaults,
  type SalesOrderReportDocumentProps
} from './types'

const props = withDefaults(
  defineProps<SalesOrderReportDocumentProps>(),
  salesOrderReportDocumentPropDefaults
)

function dash(v?: string | null) {
  const s = (v ?? '').trim()
  return s || '—'
}

function money(v?: string | null) {
  if (!props.showAmounts) return '—'
  const s = dash(v)
  if (s === '—') return '—'
  const cur = (props.currencyLabel ?? '').trim()
  return cur ? `${s} ${cur}` : s
}
</script>

<style lang="scss">
.po-doc--so-v2 {
  --po-v2-navy: #090e1d;
  --po-v2-accent: #00d2ef;
  --po-v2-line: #05e5ff;
  --po-v2-head-fg: #fff;
  --po-v2-ink: #1a1d22;
  --po-v2-muted: #6b7280;
  --po-v2-border: #d8e1e6;
  --po-v2-wash: #f5f7fa;
  --po-v2-panel: #eaf6fb;
  --po-v2-row: #f4f9fc;
  --po-v2-legal: #f3f7f9;

  width: 210mm;
  min-height: 297mm;
  margin: 0 auto;
  padding: 0 10mm 8mm;
  box-sizing: border-box;
  background: #fff;
  color: var(--po-v2-ink);
  font-size: 8.5pt;
  line-height: 1.4;
  font-family: Arial, Helvetica, 'Microsoft YaHei', sans-serif;

  .po-v2__head {
    position: relative;
    display: flex;
    justify-content: space-between;
    align-items: flex-end;
    gap: 8mm;
    margin: 0 -10mm 0;
    padding: 5.2mm 10mm 4.6mm;
    overflow: hidden;
    background: var(--po-v2-navy);
    color: var(--po-v2-head-fg);
  }

  .po-v2__head-left {
    position: relative;
    z-index: 1;
    display: flex;
    flex-direction: column;
    justify-content: flex-end;
    align-items: flex-start;
    min-width: 0;
  }

  .po-v2__logo-stack {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1.4mm;
    width: max-content;
    max-width: 100%;
  }

  .po-v2__logo {
    max-height: 12mm;
    max-width: 42mm;
    object-fit: contain;
    display: block;
  }

  .po-v2__logo-fallback {
    font-size: 15pt;
    font-weight: 800;
    letter-spacing: 0.14em;
    color: var(--po-v2-accent);
  }

  .po-v2__tagline {
    font-size: 6pt;
    letter-spacing: 0.24em;
    color: rgba(255, 255, 255, 0.78);
    text-transform: uppercase;
    text-align: center;
    width: 100%;
    white-space: nowrap;
  }

  .po-v2__head-right {
    position: relative;
    z-index: 1;
    text-align: right;
    flex: 0 0 88mm;
  }

  .po-v2__head-right::before {
    content: '';
    position: absolute;
    top: -10mm;
    right: -12mm;
    bottom: -8mm;
    width: 82mm;
    pointer-events: none;
    opacity: 0.55;
    background-image:
      repeating-linear-gradient(45deg, transparent 0 8px, rgba(5, 229, 255, 0.22) 8px 8.7px),
      repeating-linear-gradient(-45deg, transparent 0 8px, rgba(5, 229, 255, 0.22) 8px 8.7px);
    mask-image: linear-gradient(90deg, transparent 0%, #000 42%);
    -webkit-mask-image: linear-gradient(90deg, transparent 0%, #000 42%);
  }

  .po-v2__title-zh {
    position: relative;
    font-size: 16pt;
    font-weight: 800;
    letter-spacing: 0.16em;
    line-height: 1.05;
    color: #fff;
    font-family: 'Microsoft YaHei', Arial, sans-serif;
  }

  .po-v2__title-en {
    position: relative;
    margin-top: 1mm;
    font-size: 6.5pt;
    font-weight: 700;
    letter-spacing: 0.46em;
    text-indent: 0.46em;
    text-transform: uppercase;
    color: var(--po-v2-accent);
    line-height: 1.15;
  }

  .po-v2__po-no {
    position: relative;
    margin-top: 2mm;
    font-size: 7pt;
    font-weight: 500;
    letter-spacing: 0.03em;
    color: #fff;
    white-space: nowrap;
  }

  .po-v2__fade {
    height: 1.6px;
    margin: 0 0 2.8mm;
    background: linear-gradient(90deg, #05e5ff 0%, #53edff 28%, #c9f8ff 62%, transparent 100%);
  }

  .po-v2__head + .po-v2__fade {
    margin-top: 10px;
    height: 3.2px;
  }

  .po-v2__meta {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    background: var(--po-v2-wash);
    border: 1px solid var(--po-v2-border);
    margin-bottom: 3.6mm;
  }

  .po-v2__meta-cell {
    padding: 2mm 2.8mm 2.2mm;
    border-right: 1px solid var(--po-v2-border);
    min-width: 0;
  }

  .po-v2__meta-cell:last-child {
    border-right: none;
  }

  .po-v2__meta-k {
    font-size: 6.5pt;
    font-weight: 700;
    color: #4b5563;
    letter-spacing: 0.04em;
    margin-bottom: 1mm;
  }

  .po-v2__meta-v {
    font-size: 9pt;
    font-weight: 400;
    color: #4b5563;
    word-break: break-all;
  }

  .po-v2__block {
    margin-bottom: 3.2mm;
  }

  .po-v2__sec-hd {
    display: flex;
    align-items: center;
    gap: 2mm;
    font-weight: 700;
    font-size: 9pt;
    color: var(--po-v2-ink);
    margin-bottom: 1.2mm;
  }

  .po-v2__guide {
    display: inline-block;
    width: 2.4mm;
    height: 3.6mm;
    background: var(--po-v2-line);
    flex-shrink: 0;
  }

  .po-v2__parties {
    display: grid;
    grid-template-columns: 1fr 1fr;
  }

  .po-v2__party {
    border: 1px solid var(--po-v2-border);
    border-top: 2.4px solid var(--po-v2-line);
  }

  .po-v2__party + .po-v2__party {
    border-left: none;
  }

  .po-v2__party-role {
    padding: 1.4mm 3mm;
    background: var(--po-v2-wash);
    font-size: 8pt;
    font-weight: 700;
    border-bottom: 1px solid var(--po-v2-border);
  }

  .po-v2__party-body {
    padding: 2mm 3mm 2.2mm;
    font-size: 7.8pt;
    line-height: 1.45;
  }

  .po-v2__party-line {
    margin-bottom: 0.8mm;
    word-break: break-word;
  }

  .po-v2__party-line:last-child {
    margin-bottom: 0;
  }

  .po-v2__grid {
    width: 100%;
    border-collapse: collapse;
    table-layout: fixed;
    font-size: 7.4pt;
    border: 1px solid var(--po-v2-border);
  }

  .po-v2__grid--so10 .c-so-idx {
    width: 5%;
  }
  .po-v2__grid--so10 .c-so-name {
    width: 14%;
  }
  .po-v2__grid--so10 .c-so-spec {
    width: 14%;
  }
  .po-v2__grid--so10 .c-so-brand {
    width: 10%;
  }
  .po-v2__grid--so10 .c-so-unit {
    width: 6%;
  }
  .po-v2__grid--so10 .c-so-cur {
    width: 7%;
  }
  .po-v2__grid--so10 .c-so-qty {
    width: 8%;
  }
  .po-v2__grid--so10 .c-so-price {
    width: 12%;
  }
  .po-v2__grid--so10 .c-so-tax {
    width: 7%;
  }
  .po-v2__grid--so10 .c-so-amt {
    width: 17%;
  }

  .po-v2__grid th,
  .po-v2__grid td {
    border: 1px solid var(--po-v2-border);
    padding: 2.6px 4px;
    vertical-align: middle;
    word-break: break-word;
  }

  .po-v2__grid thead th {
    background: var(--po-v2-navy);
    color: #fff;
    font-weight: 700;
    text-align: center;
    line-height: 2.1;
    border-left-color: var(--po-v2-navy);
    border-right-color: var(--po-v2-navy);
  }

  .po-v2__grid tbody td {
    background: var(--po-v2-row);
    line-height: 2.1;
  }

  .po-v2__brand {
    white-space: nowrap;
  }

  .po-v2__grid .cen {
    text-align: center;
  }

  .po-v2__grid .num,
  .po-v2 .num {
    text-align: right;
    font-variant-numeric: tabular-nums;
  }

  .po-v2__empty {
    text-align: center;
    color: var(--po-v2-muted);
    padding: 5mm 0 !important;
    background: #fff !important;
  }

  .po-v2__lower {
    display: grid;
    grid-template-columns: 2fr 1fr;
    gap: 2.5mm;
    margin-bottom: 3mm;
  }

  .po-v2__panel {
    border: 1px solid var(--po-v2-border);
    background: #fff;
  }

  .po-v2__panel-hd {
    padding: 1.6mm 3mm;
    background: var(--po-v2-panel);
    font-weight: 700;
    font-size: 8pt;
    color: var(--po-v2-ink);
  }

  .po-v2__panel-hd--plain {
    background: var(--po-v2-legal);
  }

  .po-v2__kv {
    display: grid;
    grid-template-columns: 34mm 1fr;
    gap: 2mm;
    padding: 1.7mm 3mm;
    font-size: 7.4pt;
    border-bottom: 1px dashed #c5ced4;
  }

  .po-v2__panel .po-v2__kv:last-child {
    border-bottom: none;
  }

  .po-v2__k {
    color: var(--po-v2-muted);
  }

  .po-v2__kv--total {
    grid-template-columns: 1fr auto;
    font-weight: 800;
    font-size: 9pt;
    color: var(--po-v2-ink);
    border-top: 1px solid var(--po-v2-border);
    border-bottom: none;
  }

  .po-v2__terms-plain {
    border: 1px solid var(--po-v2-border);
    padding: 2.4mm 3mm;
    background: #fff;
    font-size: 7.2pt;
    line-height: 1.48;
  }

  .po-v2__term-line {
    margin-bottom: 1.2mm;
  }

  .po-v2__term-line:last-child {
    margin-bottom: 0;
  }

  .po-v2__confirm {
    margin: 2.4mm 0 0;
    font-size: 8pt;
    font-weight: 700;
    color: var(--po-v2-ink);
  }

  .po-v2__sign {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 4mm;
    margin: 4mm 0 2mm;
  }

  .po-v2__sign-box {
    border: 1px dashed #b7c0c8;
    padding: 2.6mm 3.2mm 3mm;
    min-height: 32mm;
  }

  .po-v2__sign-t {
    font-weight: 800;
    font-size: 9pt;
    margin-bottom: 1.4mm;
  }

  .po-v2__sign-name {
    font-size: 7.8pt;
  }

  .po-v2__sign-pad {
    min-height: 14mm;
    position: relative;
    margin-top: 1mm;
  }

  .po-v2__seal-hint {
    font-size: 8pt;
    color: var(--po-v2-muted);
  }

  .po-v2__seal {
    max-height: 22mm;
    max-width: 22mm;
    object-fit: contain;
    position: absolute;
    left: 12mm;
    top: 0;
  }

  .po-v2__sign-rule {
    height: 1px;
    background: #c5ced4;
    margin: 2mm 0 1.6mm;
  }

  .po-v2__sign-lines {
    display: flex;
    flex-direction: column;
    gap: 1mm;
    font-size: 7.6pt;
    color: var(--po-v2-muted);
  }

  @media print {
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }
}
</style>
