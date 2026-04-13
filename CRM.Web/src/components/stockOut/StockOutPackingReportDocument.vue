<template>
  <div class="po-doc">
    <header class="po-doc__masthead">
      <div class="po-doc__masthead-left">
        <img v-if="logoUrl" class="po-doc__logo" :src="logoUrl" alt="" />
      </div>
      <div class="po-doc__masthead-center">
        <div class="po-doc__masthead-company">{{ headerCompanyName }}</div>
        <div class="po-doc__masthead-title">{{ docTitle }}</div>
        <div v-if="docSubtitle" class="po-doc__masthead-sub">{{ docSubtitle }}</div>
      </div>
      <div class="po-doc__masthead-right">
        <div><span class="po-doc__k">日期：</span>{{ docDate }}</div>
        <div><span class="po-doc__k">Packing No.：</span>{{ docNo }}</div>
      </div>
    </header>

    <table class="po-doc__tri">
      <thead>
        <tr>
          <th>Shipper / 发货方</th>
          <th>Consignee / 收货方</th>
          <th>Shipment / 出货信息</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td class="po-doc__tri-cell">
            <div v-for="(t, i) in shipperLines" :key="'sh' + i" class="po-doc__tri-line">{{ t }}</div>
          </td>
          <td class="po-doc__tri-cell">
            <div v-for="(t, i) in consigneeLines" :key="'cn' + i" class="po-doc__tri-line">{{ t }}</div>
          </td>
          <td class="po-doc__tri-cell">
            <div v-for="(t, i) in shipmentLines" :key="'sp' + i" class="po-doc__tri-line">{{ t }}</div>
          </td>
        </tr>
      </tbody>
    </table>

    <table class="po-doc__grid">
      <thead>
        <tr>
          <th class="w-pk-idx">序号</th>
          <th class="w-pk-desc">品名及规格 / Description</th>
          <th class="w-pk-ref">参考号 / Ref.</th>
          <th class="w-pk-qty num">数量 / Qty</th>
          <th class="w-pk-ctn">箱号 / Carton</th>
          <th class="w-pk-rmk">备注 / Remark</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="line in lines" :key="'l' + line.index">
          <td class="cen">{{ line.index }}</td>
          <td>{{ line.description }}</td>
          <td>{{ line.ref }}</td>
          <td class="num">{{ line.qty }}</td>
          <td class="cen">{{ line.carton }}</td>
          <td>{{ line.remark }}</td>
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
        <tr v-if="lines.length > 0" class="po-doc__sum-row">
          <td>合计 / Total</td>
          <td colspan="2"></td>
          <td class="num">{{ totalQty }}</td>
          <td colspan="2"></td>
        </tr>
      </tbody>
    </table>

    <section v-if="withShipmentInspection" class="po-doc__qc">
      <div class="po-doc__addon-bar">出货检验 / Outbound inspection</div>
      <table class="po-doc__qc-grid">
        <thead>
          <tr>
            <th class="w-qc-i">序号</th>
            <th class="w-qc-item">检验项目 / Item</th>
            <th class="w-qc-j">判定 / Result</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(item, idx) in qcInspectionItems" :key="'qc' + idx">
            <td class="cen">{{ idx + 1 }}</td>
            <td class="qc-item-cell">{{ item }}</td>
            <td>&nbsp;</td>
          </tr>
        </tbody>
      </table>
      <div class="po-doc__qc-foot">
        <span>{{ qcInspectorLabel }}</span>
        <span>{{ qcDateLabel }}</span>
      </div>
    </section>

    <section class="po-doc__addon">
      <div class="po-doc__addon-bar">备注 / Remarks</div>
      <div class="po-doc__addon-body">
        <div v-for="(t, i) in remarkLines" :key="'r' + i" class="po-doc__addon-line">{{ t }}</div>
        <div class="po-doc__addon-terms-hd">说明 / Notes</div>
        <div v-for="(t, i) in notes" :key="'n' + i" class="po-doc__term-line">{{ t }}</div>
      </div>
    </section>

    <section class="po-doc__sign">
      <div class="po-doc__sign-cell">
        <div class="po-doc__sign-t">Shipper（发货方签章）</div>
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

export interface StockOutPackingLineVm {
  index: number
  description: string
  ref: string
  qty: string
  carton: string
  remark: string
}

const props = withDefaults(
  defineProps<{
    headerCompanyName: string
    docTitle: string
    docSubtitle?: string
    docNo: string
    docDate: string
    shipperLines: string[]
    consigneeLines: string[]
    shipmentLines: string[]
    lines: StockOutPackingLineVm[]
    totalQty: string
    remarkLines: string[]
    notes: string[]
    withShipmentInspection: boolean
    /** 含出货检时五项检验项目全文（由页面 i18n 注入） */
    qcInspectionItems: string[]
    qcInspectorLabel: string
    qcDateLabel: string
    sealUrl: string | null
    logoUrl: string | null
    showSeal: boolean
    signDate: string
  }>(),
  {
    docSubtitle: '',
    showSeal: true,
    signDate: '',
    qcInspectionItems: () => []
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

.w-pk-idx {
  width: 8%;
}
.w-pk-desc {
  width: 34%;
}
.w-pk-ref {
  width: 18%;
}
.w-pk-qty {
  width: 12%;
}
.w-pk-ctn {
  width: 14%;
}
.w-pk-rmk {
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

.po-doc__qc {
  margin-bottom: 12px;
}

.po-doc__qc-grid {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
  margin-top: 0;
}

.po-doc__qc-grid th,
.po-doc__qc-grid td {
  border: 1px solid $po-border;
  padding: 5px 4px;
  font-size: 9pt;
  vertical-align: middle;
}

.po-doc__qc-grid thead th {
  background: rgba(229, 145, 62, 0.35);
  font-weight: 700;
  text-align: center;
}

.w-qc-i {
  width: 8%;
}
.w-qc-item {
  width: 72%;
}
.w-qc-j {
  width: 20%;
}

.qc-item-cell {
  font-size: 9pt;
  line-height: 1.45;
  text-align: justify;
}

.po-doc__qc-foot {
  display: flex;
  justify-content: space-between;
  margin-top: 8px;
  font-size: 9.5pt;
  padding: 0 4px;
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
