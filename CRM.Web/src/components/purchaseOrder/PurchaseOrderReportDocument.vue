<template>
  <div class="po-doc">
    <!-- 图2：顶栏 Logo 左、我方公司名+标题中、日期单号右 -->
    <header class="po-doc__masthead">
      <div class="po-doc__masthead-left">
        <img v-if="logoUrl" class="po-doc__logo" :src="logoUrl" alt="" />
      </div>
      <div class="po-doc__masthead-center">
        <div class="po-doc__masthead-company">{{ headerCompanyName }}</div>
        <div class="po-doc__masthead-title">采购订单</div>
      </div>
      <div class="po-doc__masthead-right">
        <div><span class="po-doc__k">日期：</span>{{ orderDate }}</div>
        <div><span class="po-doc__k">单号：</span>{{ orderCode }}</div>
      </div>
    </header>

    <!-- 图2：三栏 卖方(供应商) | 买方(我方) | 交付 — 橙色表头 -->
    <table class="po-doc__tri">
      <thead>
        <tr>
          <th>卖方</th>
          <th>买方</th>
          <th>交付</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td class="po-doc__tri-cell">
            <div><span class="po-doc__lbl">公司名称：</span>{{ partySeller.name }}</div>
            <div><span class="po-doc__lbl">公司地址：</span>{{ partySeller.address }}</div>
            <div><span class="po-doc__lbl">联系电话：</span>{{ partySeller.phone }}</div>
            <div><span class="po-doc__lbl">收货人：</span>{{ partySeller.consignee }}</div>
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

    <!-- 图2：明细表 10 列 + 橙表头；空行 +「以下空白」+ 总计行 -->
    <table class="po-doc__grid">
      <thead>
        <tr>
          <th class="w-idx">序号</th>
          <th class="w-name">产品名称</th>
          <th class="w-spec">规格型号</th>
          <th class="w-brand">品牌</th>
          <th class="w-unit">单位</th>
          <th class="w-cur">币种</th>
          <th class="w-qty num">数量</th>
          <th class="w-price num">单价（含税）</th>
          <th class="w-tax num">税率</th>
          <th class="w-amt num">合计金额（含税）</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="line in lines" :key="'l' + line.index">
          <td class="cen">{{ line.index }}</td>
          <td>{{ line.productName }}</td>
          <td>{{ line.spec }}</td>
          <td>{{ line.brand }}</td>
          <td class="cen">{{ line.unit }}</td>
          <td class="cen">{{ line.currency }}</td>
          <td class="num">{{ showAmounts ? line.qty : '—' }}</td>
          <td class="num">{{ showAmounts ? line.unitPrice : '—' }}</td>
          <td class="num">{{ line.taxRate }}</td>
          <td class="num">{{ showAmounts ? line.lineTotal : '—' }}</td>
        </tr>
        <tr v-for="i in fillerRowCount" :key="'f' + i">
          <td v-for="c in 10" :key="`${i}-${c}`">&nbsp;</td>
        </tr>
        <tr v-if="lines.length === 0">
          <td colspan="10" class="po-doc__empty">暂无明细</td>
        </tr>
        <tr v-else-if="lines.length > 0" class="po-doc__hint-row">
          <td colspan="10" class="po-doc__hint">以下空白</td>
        </tr>
        <tr v-if="showAmounts && lines.length > 0" class="po-doc__sum-row">
          <td>总计</td>
          <td colspan="5"></td>
          <td class="num">{{ totalQty }}</td>
          <td colspan="2"></td>
          <td class="num">{{ totalIncl }}</td>
        </tr>
      </tbody>
    </table>

    <!-- 图2：金额汇总在表右下 -->
    <div v-if="showAmounts" class="po-doc__finance-wrap">
      <table class="po-doc__finance">
        <tbody>
          <tr>
            <td class="po-doc__fin-lbl">不含税金额</td>
            <td class="num">{{ exclTax }}</td>
          </tr>
          <tr>
            <td class="po-doc__fin-lbl">增值税</td>
            <td class="num">{{ taxAmount }}</td>
          </tr>
          <tr>
            <td class="po-doc__fin-lbl">价税合计金额</td>
            <td class="num po-doc__fin-grand">{{ grandIncl }}</td>
          </tr>
          <tr>
            <td class="po-doc__fin-lbl">货币</td>
            <td class="po-doc__fin-cur">
              <span class="po-doc__fin-rate">{{ taxRateLabel }}</span>
              <span class="po-doc__fin-sep">|</span>
              <span>{{ currencyLabel }}</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- 图2：附加信息橙条 + 运费/地址/付款 + 服务条款 + 确认语 -->
    <section class="po-doc__addon">
      <div class="po-doc__addon-bar">附加信息</div>
      <div class="po-doc__addon-body">
        <div v-for="(t, i) in extraLines" :key="i" class="po-doc__addon-line">{{ t }}</div>
        <div class="po-doc__addon-terms-hd">服务条款</div>
        <div v-for="(t, i) in terms" :key="'t' + i" class="po-doc__term-line">{{ t }}</div>
        <p class="po-doc__confirm">请在24小时内确认此合同并签字/盖章，谢谢！</p>
      </div>
    </section>

    <!-- 签章：印章放买方侧（图2） -->
    <section class="po-doc__sign">
      <div class="po-doc__sign-cell">
        <div class="po-doc__sign-t">Vendor（卖方签章）</div>
        <div class="po-doc__sign-pad"></div>
        <div>日期：</div>
      </div>
      <div class="po-doc__sign-cell po-doc__sign-cell--buyer">
        <div class="po-doc__sign-t">Buyer（买方签章）</div>
        <div class="po-doc__sign-pad po-doc__sign-pad--seal">
          <img v-if="showSeal && sealUrl" class="po-doc__seal" :src="sealUrl" alt="" />
        </div>
        <div class="po-doc__sign-date">日期：{{ buyerSignDate }}</div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

export interface PoReportLineVm {
  index: number
  brand: string
  unit: string
  currency: string
  qty: string
  unitPrice: string
  taxRate: string
  lineTotal: string
  productName: string
  spec: string
}

const props = withDefaults(
  defineProps<{
    /** 顶栏居中大字：我方（采购方）公司名 */
    headerCompanyName: string
    orderCode: string
    orderDate: string
    deliveryDate: string
    deliveryMode: string
    /** 模版「卖方」= 供应商 */
    partySeller: { name: string; address: string; phone: string; consignee: string }
    /** 模版「买方」= 我方 */
    partyBuyer: { name: string; address: string }
    currencyLabel: string
    lines: PoReportLineVm[]
    totalQty: string
    totalIncl: string
    exclTax: string
    taxAmount: string
    grandIncl: string
    taxRateLabel: string
    extraLines: string[]
    terms: string[]
    sealUrl: string | null
    logoUrl: string | null
    showAmounts: boolean
    /** 是否在买方签章区显示印章图 */
    showSeal: boolean
    /** 买方签章日期，与图2一致可填订单日期 */
    buyerSignDate?: string
  }>(),
  {
    buyerSignDate: '',
    showSeal: true
  }
)

/** 明细行上方保留若干空行，接近图2版式 */
const fillerRowCount = computed(() => {
  const target = 6
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
  /* 明确白纸底，避免透明 PNG 与祖先/半透明层合成时出现网格或灰底 */
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

.po-doc__sign-cell--buyer .po-doc__sign-pad--seal {
  margin-left: auto;
  max-width: 38mm;
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
