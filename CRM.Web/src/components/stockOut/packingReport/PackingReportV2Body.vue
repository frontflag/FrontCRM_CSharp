<!-- V2：竖版对齐 bilingual PDF；横版包装明细 14 列英文表头。配色由皮肤 CSS 变量提供 -->
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
        <div class="po-v2__title-zh">装箱单</div>
        <div class="po-v2__title-en">PACKING LIST</div>
        <div class="po-v2__po-no">装箱单号码 / Packing List No. {{ dash(packingNo) }}</div>
      </div>
    </header>
    <div class="po-v2__fade" aria-hidden="true" />

    <div class="po-v2__meta">
      <div class="po-v2__meta-cell">
        <div class="po-v2__meta-k">单据日期 / DOCUMENT DATE</div>
        <div class="po-v2__meta-v">{{ dash(docDate) }}</div>
      </div>
      <div class="po-v2__meta-cell">
        <div class="po-v2__meta-k">发票/订单号 / INVOICE / PO NO.</div>
        <div class="po-v2__meta-v">{{ dash(invoicePoNo) }}</div>
      </div>
      <div class="po-v2__meta-cell">
        <div class="po-v2__meta-k">贸易术语 / INCOTERMS</div>
        <div class="po-v2__meta-v">{{ dash(incoterms) }}</div>
      </div>
      <div class="po-v2__meta-cell">
        <div class="po-v2__meta-k">运输方式 / TRANSPORT MODE</div>
        <div class="po-v2__meta-v">{{ dash(transportMode) }}</div>
      </div>
    </div>

    <section class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        发货与收货信息 / SHIPPING PARTIES
      </div>
      <div class="po-v2__parties">
        <div class="po-v2__party">
          <div class="po-v2__party-role">发货人 / SHIPPER</div>
          <div class="po-v2__party-name">{{ dash(shipper.name) }}</div>
          <div class="po-v2__party-line">
            地址 / Address: {{ dash(shipper.address) }}
            <span class="po-v2__gap" />
            联系人 / Contact: {{ dash(shipper.contact) }}
            <span class="po-v2__gap" />
            电话 / Tel: {{ dash(shipper.phone) }}
          </div>
          <div class="po-v2__party-line">邮箱 / Email: {{ dash(shipper.email) }}</div>
        </div>
        <div class="po-v2__party">
          <div class="po-v2__party-role">收货人 / CONSIGNEE</div>
          <div class="po-v2__party-name">{{ dash(consignee.name) }}</div>
          <div class="po-v2__party-line">
            地址 / Address: {{ dash(consignee.address) }}
            <span class="po-v2__gap" />
            联系人 / Contact: {{ dash(consignee.contact) }}
            <span class="po-v2__gap" />
            电话 / Tel: {{ dash(consignee.phone) }}
          </div>
          <div class="po-v2__party-line">邮箱 / Email: {{ dash(consignee.email) }}</div>
        </div>
      </div>
    </section>

    <section class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        包装明细 / PACKING DETAILS
      </div>
      <table v-if="!isLandscape" class="po-v2__grid">
        <colgroup>
          <col class="c-ctn" />
          <col class="c-mpn" />
          <col class="c-brand" />
          <col class="c-lot" />
          <col class="c-desc" />
          <col class="c-qty" />
          <col class="c-nw" />
          <col class="c-gw" />
          <col class="c-dim" />
        </colgroup>
        <thead>
          <tr>
            <th>箱号<br />Carton</th>
            <th>物料型号<br />MPN</th>
            <th>厂牌<br />Brand</th>
            <th>批号<br />Lot No.</th>
            <th>描述<br />Description</th>
            <th>数量<br />Qty</th>
            <th>净重<br />N.W.</th>
            <th>毛重<br />G.W.</th>
            <th>尺寸<br />Dimensions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="line in lines" :key="'l' + line.index">
            <td class="cen">{{ line.carton }}</td>
            <td class="po-v2__mpn">{{ dash(line.mpn) }}</td>
            <td class="po-v2__brand">{{ dash(line.brand) }}</td>
            <td class="cen">{{ dash(line.lotNo) }}</td>
            <td class="po-v2__desc">{{ dash(line.description) }}</td>
            <td class="num">{{ dash(line.qty) }}</td>
            <td class="num">{{ dash(line.nw) }}</td>
            <td class="num">{{ dash(line.gw) }}</td>
            <td class="cen">{{ dash(line.dimensions) }}</td>
          </tr>
          <tr v-if="lines.length === 0">
            <td colspan="9" class="po-v2__empty">暂无明细 / No items</td>
          </tr>
        </tbody>
      </table>
      <table v-else class="po-v2__grid po-v2__grid--ls">
        <colgroup>
          <col class="c-ls-idx" />
          <col class="c-ls-po" />
          <col class="c-ls-pn" />
          <col class="c-ls-cpn" />
          <col class="c-ls-brand" />
          <col class="c-ls-qty" />
          <col class="c-ls-dc" />
          <col class="c-ls-co" />
          <col class="c-ls-cod" />
          <col class="c-ls-size" />
          <col class="c-ls-nw" />
          <col class="c-ls-gw" />
          <col class="c-ls-ctn" />
          <col class="c-ls-remark" />
        </colgroup>
        <thead>
          <tr>
            <th>S/No.</th>
            <th>CUSTOMER PO</th>
            <th>PART NUMBER</th>
            <th>CUSTOMER PN</th>
            <th>Brand</th>
            <th>QTY (PCS)</th>
            <th>DC</th>
            <th>CO</th>
            <th>COD</th>
            <th>SIZE</th>
            <th>NW (KG)</th>
            <th>GW (KG)</th>
            <th>CARTON</th>
            <th>Remark</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="line in landscapeLines" :key="'ls' + line.index">
            <td class="cen">{{ line.index }}</td>
            <td>{{ dash(line.customerPo) }}</td>
            <td class="po-v2__mpn">{{ dash(line.partNumber) }}</td>
            <td>{{ dash(line.customerPn) }}</td>
            <td class="po-v2__brand">{{ dash(line.brand) }}</td>
            <td class="num">{{ dash(line.qty) }}</td>
            <td class="cen">{{ dash(line.dc) }}</td>
            <td class="cen">{{ dash(line.co) }}</td>
            <td class="cen">{{ dash(line.cod) }}</td>
            <td class="cen">{{ dash(line.size) }}</td>
            <td class="num">{{ dash(line.nw) }}</td>
            <td class="num">{{ dash(line.gw) }}</td>
            <td class="cen">{{ dash(line.carton) }}</td>
            <td class="po-v2__desc">{{ dash(line.remark) }}</td>
          </tr>
          <tr v-if="landscapeLines.length === 0">
            <td colspan="14" class="po-v2__empty">暂无明细 / No items</td>
          </tr>
        </tbody>
      </table>
    </section>

    <section v-if="withShipmentInspection" class="po-v2__block">
      <div class="po-v2__sec-hd">
        <i class="po-v2__guide" aria-hidden="true" />
        出库检验 / OUTBOUND INSPECTION
      </div>
      <table class="po-v2__grid po-v2__qc">
        <colgroup>
          <col class="c-qc-i" />
          <col class="c-qc-item" />
          <col class="c-qc-r" />
        </colgroup>
        <thead>
          <tr>
            <th>序号 / No.</th>
            <th>检验项目 / Item</th>
            <th>结果 / Result</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(item, idx) in qcItems" :key="'qc' + idx">
            <td class="cen">{{ String(idx + 1).padStart(2, '0') }}</td>
            <td class="po-v2__desc">{{ item }}</td>
            <td class="cen">☐</td>
          </tr>
        </tbody>
      </table>
      <div class="po-v2__qc-foot">
        <span>检验员 / Inspector: ____________</span>
        <span>检验日期 / Inspection Date: ____________</span>
      </div>
    </section>

    <div class="po-v2__lower">
      <section class="po-v2__panel">
        <div class="po-v2__panel-hd po-v2__panel-hd--plain">运输资料 / SHIPMENT INFORMATION</div>
        <div class="po-v2__kv">
          <span class="po-v2__k">运输标记 / Shipping Marks</span>
          <span>{{ dash(shipMarks) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">起运地 / Place of Departure</span>
          <span>{{ dash(departure) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">目的地 / Destination</span>
          <span>{{ dash(destination) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">承运人/快递单号 / Carrier / AWB</span>
          <span>{{ dash(carrierAwb) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">备注 / Remarks</span>
          <span>
            <template v-if="remarks.length">
              <div v-for="(n, i) in remarks" :key="'r' + i">{{ n }}</div>
            </template>
            <template v-else>
              所有货物应按合同与包装要求完成标识、包装和保护。 / All goods shall be marked, packed and
              protected in accordance with the contract and packing requirements.
            </template>
          </span>
        </div>
      </section>
      <section class="po-v2__panel">
        <div class="po-v2__panel-hd">包装汇总 / PACKING SUMMARY</div>
        <div class="po-v2__kv">
          <span class="po-v2__k">总箱数 / Total Cartons</span>
          <span class="num">{{ dash(totalCartons) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">总数量 / Total Quantity</span>
          <span class="num">{{ dash(totalQty) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">总净重 / Total N.W.</span>
          <span class="num">{{ dash(totalNw) }}</span>
        </div>
        <div class="po-v2__kv">
          <span class="po-v2__k">总毛重 / Total G.W.</span>
          <span class="num">{{ dash(totalGw) }}</span>
        </div>
        <div class="po-v2__kv po-v2__kv--total">
          <span>总体积 / Total Volume</span>
          <span class="num">{{ dash(totalVolume) }}</span>
        </div>
      </section>
    </div>

    <div class="po-v2__legal">
      <strong>包装声明 / Packing Declaration：</strong>
      本装箱单所列货物、包装数量、重量与尺寸应与实际出运货物一致。 / The goods, package count, weights and
      dimensions listed in this packing list shall correspond to the actual shipment.
    </div>

    <section class="po-v2__sign">
      <div class="po-v2__sign-box">
        <div class="po-v2__sign-t">发货人 / Shipper</div>
        <div class="po-v2__sign-name">公司名称 / Company: {{ dash(shipper.name) }}</div>
        <div class="po-v2__sign-pad">
          <span class="po-v2__seal-hint">（盖章 / Seal）</span>
          <img v-if="showSeal && sealUrl" class="po-v2__seal" :src="sealUrl" alt="" />
        </div>
        <div class="po-v2__sign-rule" />
        <div class="po-v2__sign-lines">
          <span>授权代表签字 / Authorized Signature: ________________</span>
          <span>日期 / Date: ____________</span>
        </div>
      </div>
      <div class="po-v2__sign-box">
        <div class="po-v2__sign-t">收货人确认 / Consignee Acknowledgement</div>
        <div class="po-v2__sign-name">公司名称 / Company: {{ dash(consignee.name) }}</div>
        <div class="po-v2__sign-pad">
          <span class="po-v2__seal-hint">（盖章 / Seal，如需）</span>
        </div>
        <div class="po-v2__sign-rule" />
        <div class="po-v2__sign-lines">
          <span>授权代表签字 / Authorized Signature: ________________</span>
          <span>日期 / Date: ____________</span>
        </div>
      </div>
    </section>

    <footer class="po-v2__foot">
      <span>装箱单（草案）/ Packing List Draft — 出运前请核对件数、重量、尺寸与运输资料。</span>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import {
  packingReportV2DocumentPropDefaults,
  type PackingReportV2DocumentProps
} from './types'

const props = withDefaults(
  defineProps<PackingReportV2DocumentProps>(),
  packingReportV2DocumentPropDefaults
)

const isLandscape = computed(() => props.orientation === 'landscape')

function dash(v?: string | null) {
  const s = (v ?? '').trim()
  return s || '—'
}
</script>

<style lang="scss">
.po-doc--pl-v2 {
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
  table-layout: auto;
  font-size: 7.4pt;
  border: 1px solid var(--po-v2-border);
}

.po-v2__grid .c-ctn,
.po-v2__grid .c-mpn,
.po-v2__grid .c-brand,
.po-v2__grid .c-lot,
.po-v2__grid .c-qty,
.po-v2__grid .c-nw,
.po-v2__grid .c-gw,
.po-v2__grid .c-dim {
  width: 1%;
}

.po-v2__grid .c-desc {
  width: auto;
}

.po-v2__qc {
  table-layout: fixed;
}

.po-v2__qc .c-qc-i {
  width: 50px;
}

.po-v2__qc .c-qc-r {
  width: 70px;
}

.po-v2__qc .c-qc-item {
  width: auto;
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
  padding: 3.2px 3px;
  line-height: 1.25;
  border-left-color: var(--po-v2-navy);
  border-right-color: var(--po-v2-navy);
  /* 竖版：中英各一行（模板 <br />），禁止再按字折成竖条 */
  white-space: nowrap;
  word-break: keep-all;
  overflow-wrap: normal;
}

.po-v2__grid tbody td {
  background: var(--po-v2-row);
  line-height: 2.1;
}

.po-v2__grid.po-v2__qc thead th {
  padding: 2.6px 4px;
  line-height: 2.1;
  white-space: nowrap;
  word-break: keep-all;
  overflow-wrap: normal;
}

.po-v2__mpn {
  font-weight: 700;
}

.po-v2__mpn,
.po-v2__brand {
  white-space: nowrap;
  word-break: keep-all;
}

.po-v2__desc {
  white-space: normal;
  word-break: break-word;
  overflow-wrap: anywhere;
}

.po-v2__grid .cen {
  text-align: center;
  white-space: nowrap;
  word-break: keep-all;
}

.po-v2__grid .num,
.po-v2 .num {
  text-align: right;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
  word-break: keep-all;
}

.po-v2__empty {
  text-align: center;
  color: var(--po-v2-muted);
  padding: 5mm 0 !important;
  background: #fff !important;
}

.po-v2__qc-foot {
  display: flex;
  justify-content: space-between;
  margin-top: 1.6mm;
  font-size: 7.8pt;
  color: var(--po-v2-muted);
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
  grid-template-columns: 42mm 1fr;
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
}

.po-doc--pl-v2-ls {
  width: 297mm;
  min-height: 210mm;

  .po-v2__head {
    padding: 4mm 10mm 3.6mm;
  }

  .po-v2__grid--ls {
    --ls-base: calc(88% / 13);
    --ls-remark-cur: calc(3.6% + 88% / 13 * 0.2 * 8);
    --ls-remark-share: calc(var(--ls-remark-cur) * 0.2 / 4);
    --ls-remark-prev: calc(var(--ls-remark-cur) * 0.8);
    --ls-remark-to-brand: calc(var(--ls-remark-prev) * 0.2);
    font-size: 6.8pt;
    table-layout: fixed;
  }

  .po-v2__grid--ls .c-ls-qty {
    width: var(--ls-base);
  }

  .po-v2__grid--ls .c-ls-idx,
  .po-v2__grid--ls .c-ls-dc,
  .po-v2__grid--ls .c-ls-co,
  .po-v2__grid--ls .c-ls-cod,
  .po-v2__grid--ls .c-ls-size,
  .po-v2__grid--ls .c-ls-nw,
  .po-v2__grid--ls .c-ls-gw,
  .po-v2__grid--ls .c-ls-ctn {
    width: calc(var(--ls-base) * 0.8);
  }

  .po-v2__grid--ls .c-ls-po,
  .po-v2__grid--ls .c-ls-pn,
  .po-v2__grid--ls .c-ls-cpn {
    width: calc(var(--ls-base) + 8.4% / 3 + var(--ls-remark-share));
  }

  .po-v2__grid--ls .c-ls-brand {
    width: calc(var(--ls-base) + var(--ls-remark-share) + var(--ls-remark-to-brand));
  }

  .po-v2__grid--ls .c-ls-remark {
    width: calc(var(--ls-remark-prev) * 0.8);
  }

  .po-v2__grid--ls thead th {
    font-size: 6.8pt;
    padding: 2.6px 4px;
    line-height: 2.1;
    white-space: nowrap;
    word-break: keep-all;
    overflow-wrap: normal;
    border-left-color: rgba(255, 255, 255, 0.45);
    border-right-color: rgba(255, 255, 255, 0.45);
  }

  .po-v2__grid--ls thead th:first-child {
    border-left-color: var(--po-v2-navy);
  }

  .po-v2__grid--ls thead th:last-child {
    border-right-color: var(--po-v2-navy);
  }
}
</style>
