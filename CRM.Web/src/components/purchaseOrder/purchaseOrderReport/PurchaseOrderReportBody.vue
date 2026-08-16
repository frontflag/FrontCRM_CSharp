<!-- 三皮肤共用正文；样式由父皮肤根 class 提供 -->
<template>
  <div class="po-doc__body">
    <slot name="parties" />

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
        <tr v-if="lines.length === 0">
          <td colspan="10" class="po-doc__empty">暂无明细</td>
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

    <section class="po-doc__addon">
      <slot name="addon-bar">
        <div class="po-doc__addon-bar">附加信息</div>
      </slot>
      <div class="po-doc__addon-body">
        <div v-for="(t, i) in extraLines" :key="i" class="po-doc__addon-line">{{ t }}</div>
        <div class="po-doc__addon-terms-hd">服务条款</div>
        <div v-for="(t, i) in terms" :key="'t' + i" class="po-doc__term-line">{{ t }}</div>
        <p class="po-doc__confirm">请在24小时内确认此合同并签字/盖章，谢谢！</p>
      </div>
    </section>

    <section class="po-doc__sign">
      <slot name="sign" />
    </section>
  </div>
</template>

<script setup lang="ts">
import { type PurchaseOrderReportDocumentProps } from './types'

defineProps<
  Pick<
    PurchaseOrderReportDocumentProps,
    | 'lines'
    | 'showAmounts'
    | 'totalQty'
    | 'totalIncl'
    | 'exclTax'
    | 'taxAmount'
    | 'grandIncl'
    | 'taxRateLabel'
    | 'currencyLabel'
    | 'extraLines'
    | 'terms'
  >
>()
</script>
