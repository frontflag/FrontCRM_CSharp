<!-- V2：对齐 semicore_purchase_order_one_page_refined.pdf；配色由皮肤 CSS 变量提供 -->
<template>
  <div class="po-v2">
    <header class="po-v2__head">
      <div class="po-v2__head-left">
        <img v-if="logoUrl" class="po-v2__logo" :src="logoUrl" alt="" />
        <div v-else class="po-v2__logo-fallback">{{ headerCompanyName }}</div>
        <div class="po-v2__tagline">YOUR RELIABLE SUPPLIER</div>
      </div>
      <div class="po-v2__head-right">
        <div class="po-v2__title-zh">采购订单</div>
        <div class="po-v2__title-en">PURCHASE ORDER</div>
        <div class="po-v2__po-no">订单编号 / PO NO. {{ dash(orderCode) }}</div>
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
          <div class="po-v2__party-role">需方 / BUYER</div>
          <div class="po-v2__party-name">{{ dash(partyBuyer.name) }}</div>
          <div class="po-v2__party-line">
            地址 / Address: {{ dash(partyBuyer.address) }}
            <span class="po-v2__gap" />
            联系人 / Contact: {{ dash(partyBuyer.contact) }}
            <span class="po-v2__gap" />
            电话 / Tel: {{ dash(partyBuyer.phone) }}
          </div>
          <div class="po-v2__party-line">邮箱 / Email: {{ dash(partyBuyer.email) }}</div>
        </div>
        <div class="po-v2__party">
          <div class="po-v2__party-role">供方 / SUPPLIER</div>
          <div class="po-v2__party-name">{{ dash(partySeller.name) }}</div>
          <div class="po-v2__party-line">
            地址 / Address: {{ dash(partySeller.address) }}
            <span class="po-v2__gap" />
            联系人 / Contact: {{ dash(partySeller.contact) }}
            <span class="po-v2__gap" />
            电话 / Tel: {{ dash(partySeller.contactPhone || partySeller.phone) }}
          </div>
          <div class="po-v2__party-line">邮箱 / Email: {{ dash(partySeller.email) }}</div>
        </div>
      </div>
    </section>

    <section class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        订购明细 / LINE ITEMS
      </div>
      <table class="po-v2__grid">
        <colgroup>
          <col class="c-idx" />
          <col class="c-mpn" />
          <col class="c-brand" />
          <col class="c-lot" />
          <col class="c-desc" />
          <col class="c-qty" />
          <col class="c-price" />
          <col class="c-amt" />
        </colgroup>
        <thead>
          <tr>
            <th>序号<br />No.</th>
            <th>物料型号 / MPN</th>
            <th>厂牌 / Brand</th>
            <th>批号 / Lot No.</th>
            <th>规格及描述 / Description</th>
            <th>数量<br />Qty</th>
            <th>单价</th>
            <th>金额 / Amount</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="line in lines" :key="'l' + line.index">
            <td class="cen">{{ String(line.index).padStart(2, '0') }}</td>
            <td class="po-v2__mpn">{{ dash(line.spec) }}</td>
            <td>{{ dash(line.brand) }}</td>
            <td class="cen">{{ dash(line.lotNo) }}</td>
            <td>{{ dash(line.productName) }}</td>
            <td class="num">{{ showAmounts ? line.qty : '—' }}</td>
            <td class="num">{{ showAmounts ? line.unitPrice : '—' }}</td>
            <td class="num">{{ showAmounts ? line.lineTotal : '—' }}</td>
          </tr>
          <tr v-if="lines.length === 0">
            <td colspan="8" class="po-v2__empty">暂无明细</td>
          </tr>
        </tbody>
      </table>
    </section>

    <div class="po-v2__lower">
      <section class="po-v2__panel">
        <div class="po-v2__panel-hd po-v2__panel-hd--plain">交付与质量要求 / DELIVERY &amp; QUALITY</div>
        <div class="po-v2__kv">
          <span class="po-v2__k">交货地址 / Ship To</span>
          <span>{{ dash(shipTo) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">交货日期 / Delivery</span>
          <span>{{ dash(deliveryDate) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">贸易条款 / Incoterms</span>
          <span>{{ dash(deliveryMode) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">文件要求 / Documentation</span>
          <span>{{ dash(freightNote) }}</span>
        </div>
      </section>
      <section class="po-v2__panel">
        <div class="po-v2__panel-hd">金额汇总 / SUMMARY</div>
        <div class="po-v2__kv">
          <span class="po-v2__k">小计 / Subtotal</span>
          <span class="num">{{ showAmounts ? exclTax : '—' }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">税费 / Taxes</span>
          <span class="num">{{ showAmounts ? taxAmount : '—' }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">运费 / Freight</span>
          <span class="num">{{ dash(freightAmount) }}</span>
        </div>
        <div class="po-v2__kv po-v2__kv--total">
          <span>订单总额 / Total</span>
          <span class="num">{{ showAmounts ? grandIncl : '—' }}</span>
        </div>
      </section>
    </div>

    <section class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        订单条款 / PURCHASE ORDER TERMS
      </div>
      <div class="po-v2__legal">
        <strong>文件效力：</strong>本采购订单及下列条款为不可分割的整体；供需双方盖章后生效，扫描件与合同原件具有同等法律效力。本订单壹式贰份。
      </div>
      <div class="po-v2__rule" aria-hidden="true" />
      <div class="po-v2__terms-grid">
        <div v-for="card in termCards" :key="card.title" class="po-v2__term">
          <div class="po-v2__term-t">{{ card.title }}</div>
          <div class="po-v2__term-b">{{ card.body }}</div>
        </div>
      </div>
    </section>

    <section class="po-v2__sign">
      <div class="po-v2__sign-box">
        <div class="po-v2__sign-t">需方 / Buyer</div>
        <div class="po-v2__sign-name">公司名称：{{ dash(partyBuyer.name) }}</div>
        <div class="po-v2__sign-pad">
          <span class="po-v2__seal-hint">（盖章）</span>
          <img v-if="showSeal && sealUrl" class="po-v2__seal" :src="sealUrl" alt="" />
        </div>
        <div class="po-v2__sign-rule" />
        <div class="po-v2__sign-lines">
          <span>授权代表签字：________________</span>
          <span>日期：{{ dash(buyerSignDate) }}</span>
        </div>
      </div>
      <div class="po-v2__sign-box">
        <div class="po-v2__sign-t">供方 / Supplier</div>
        <div class="po-v2__sign-name">公司名称：{{ dash(partySeller.name) }}</div>
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

    <footer class="po-v2__foot">
      <span>请在24小时内确认此合同并签字/盖章，谢谢！</span>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { PURCHASE_ORDER_SERVICE_TERM_CARDS } from '@/constants/purchaseOrderReportTerms'
import {
  purchaseOrderReportDocumentPropDefaults,
  type PurchaseOrderReportDocumentProps
} from './types'

withDefaults(defineProps<PurchaseOrderReportDocumentProps>(), purchaseOrderReportDocumentPropDefaults)

const termCards = PURCHASE_ORDER_SERVICE_TERM_CARDS

function dash(v?: string | null) {
  const s = (v ?? '').trim()
  return s || '—'
}
</script>

<style lang="scss">
.po-doc--v2 {
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
}

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
  gap: 1.4mm;
  min-width: 0;
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
    repeating-linear-gradient(
      45deg,
      transparent 0 8px,
      rgba(5, 229, 255, 0.22) 8px 8.7px
    ),
    repeating-linear-gradient(
      -45deg,
      transparent 0 8px,
      rgba(5, 229, 255, 0.22) 8px 8.7px
    );
  mask-image: linear-gradient(90deg, transparent 0%, #000 42%);
  -webkit-mask-image: linear-gradient(90deg, transparent 0%, #000 42%);
}

.po-v2__title-zh {
  position: relative;
  font-size: 20pt;
  font-weight: 800;
  letter-spacing: 0.16em;
  line-height: 1.05;
  color: #fff;
  font-family: 'Microsoft YaHei', Arial, sans-serif;
}

.po-v2__title-en {
  position: relative;
  margin-top: 1mm;
  font-size: 9.5pt;
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
  font-size: 8pt;
  font-weight: 500;
  letter-spacing: 0.03em;
  color: #fff;
  white-space: nowrap;
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

.po-v2__fade {
  height: 1.6px;
  margin: 0 0 2.8mm;
  background: linear-gradient(90deg, #05e5ff 0%, #53edff 28%, #c9f8ff 62%, transparent 100%);
}

.po-v2__head + .po-v2__fade {
  margin-top: 10px;
  height: 3.2px;
}

.po-v2__rule {
  height: 1.6px;
  margin: -8px 0 calc(2.2mm + 8px);
  background: var(--po-v2-line);
}

.po-v2__parties {
  display: grid;
  grid-template-columns: 1fr 1fr;
}

.po-v2__party {
  padding: 0 0 2.2mm;
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

.po-v2__party-name {
  padding: 2mm 3mm 1mm;
  font-size: 10pt;
  font-weight: 800;
}

.po-v2__party-line {
  padding: 0 3mm 0.8mm;
  font-size: 7.5pt;
  color: var(--po-v2-muted);
}

.po-v2__gap {
  display: inline-block;
  width: 3.5mm;
}

.po-v2__grid {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
  font-size: 7.4pt;
  border: 1px solid var(--po-v2-border);
}

.po-v2__grid .c-idx {
  width: 8%;
}

.po-v2__grid .c-mpn {
  width: 18%;
}

.po-v2__grid .c-brand {
  width: 11%;
}

.po-v2__grid .c-lot {
  width: 13%;
}

.po-v2__grid .c-desc {
  width: 22%;
}

.po-v2__grid .c-qty,
.po-v2__grid .c-price {
  width: 8%;
}

.po-v2__grid .c-amt {
  width: 12%;
}

.po-v2__grid th,
.po-v2__grid td {
  border: 1px solid var(--po-v2-border);
  padding: 2.6px 4px;
  vertical-align: middle;
  word-break: break-all;
}

.po-v2__grid thead th {
  background: var(--po-v2-navy);
  color: #fff;
  font-weight: 700;
  text-align: center;
  padding: 3.2px 3px;
  line-height: 1.25;
  border-left-color: var(--po-v2-navy);
  border-right-color: var(--po-v2-navy);
}

.po-v2__grid thead th:last-child {
  white-space: nowrap;
  word-break: keep-all;
}

.po-v2__grid tbody td {
  background: var(--po-v2-row);
  line-height: 2.1;
}

.po-v2__mpn {
  font-weight: 700;
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
  font-size: 7.8pt;
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
  font-size: 9.5pt;
  color: var(--po-v2-ink);
  border-top: 1px solid var(--po-v2-border);
  border-bottom: none;
}

.po-v2__legal {
  padding: 2.2mm 3mm;
  margin-bottom: 2mm;
  background: var(--po-v2-legal);
  border-left: 2.4px solid var(--po-v2-line);
  font-size: 7.6pt;
  line-height: 1.5;
}

.po-v2__terms-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  border: 1px solid var(--po-v2-border);
}

.po-v2__term {
  padding: 2mm 2.6mm 2.2mm;
  border-right: 1px solid var(--po-v2-border);
  border-bottom: 1px solid var(--po-v2-border);
  font-size: 6.8pt;
  line-height: 1.42;
  background: #fff;
}

.po-v2__term:nth-child(2n) {
  border-right: none;
}

.po-v2__term:nth-last-child(-n + 2) {
  border-bottom: none;
}

.po-v2__term-t {
  font-weight: 800;
  margin-bottom: 0.8mm;
  color: var(--po-v2-ink);
}

.po-v2__term-b {
  color: #333;
}

.po-v2__sign {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 4mm;
  margin: 4mm 0 3mm;
}

.po-v2__sign-box {
  border: 1px dashed #b7c0c8;
  padding: 2.6mm 3.2mm 3mm;
  min-height: 32mm;
}

.po-v2__sign-t {
  font-weight: 800;
  font-size: 9.5pt;
  margin-bottom: 1.4mm;
}

.po-v2__sign-name {
  font-size: 8pt;
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
  margin: 1mm 0 2mm;
}

.po-v2__sign-lines {
  display: flex;
  flex-wrap: wrap;
  gap: 2mm 5mm;
  font-size: 8pt;
}

.po-v2__foot {
  margin-top: 2mm;
  padding-top: 2mm;
  border-top: 1px solid #c5d0d8;
  font-size: 7pt;
  color: #9aa3ab;
}
</style>
