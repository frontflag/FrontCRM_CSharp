<!-- chrome 对齐装箱 Invoice V2：`po-doc--inv-v2` 样式来自 InvoiceReportV2Body -->
<template>
  <div class="po-doc po-doc--v2 po-doc--inv-v2">
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
          <div class="po-v2__title-zh">报价单</div>
          <div class="po-v2__title-en">QUOTATION</div>
          <div class="po-v2__po-no">报价单号 / Quotation No. {{ dash(quotationNo) }}</div>
        </div>
      </header>
      <div class="po-v2__fade" aria-hidden="true" />

      <div class="po-v2__meta po-v2__meta--inv">
        <div class="po-v2__meta-cell">
          <div class="po-v2__meta-k">单据日期 / DOCUMENT DATE</div>
          <div class="po-v2__meta-v">{{ dash(quoteDate) }}</div>
        </div>
        <div class="po-v2__meta-cell">
          <div class="po-v2__meta-k">报价单号 / QUOTATION NO.</div>
          <div class="po-v2__meta-v">{{ dash(quotationNo) }}</div>
        </div>
      </div>

      <section class="po-v2__block">
        <div class="po-v2__sec-hd">
          <i class="po-v2__guide" aria-hidden="true" />
          {{ sectionTitle.parties }}
        </div>
        <div class="po-v2__parties">
          <div class="po-v2__party">
            <div class="po-v2__party-role">{{ partyRole.billTo }}</div>
            <div class="po-v2__addr-body">
              <div v-for="(t, i) in billToLines" :key="'bt' + i" class="po-v2__addr-line">{{ t }}</div>
              <div v-if="!billToLines.length" class="po-v2__addr-line">—</div>
            </div>
          </div>
          <div class="po-v2__party">
            <div class="po-v2__party-role">{{ partyRole.quoter }}</div>
            <div class="po-v2__addr-body">
              <div v-for="(t, i) in quoterLines" :key="'qt' + i" class="po-v2__addr-line">{{ t }}</div>
              <div v-if="!quoterLines.length" class="po-v2__addr-line">—</div>
            </div>
          </div>
        </div>
      </section>

      <section class="po-v2__block">
        <div class="po-v2__sec-hd">
          <i class="po-v2__guide" aria-hidden="true" />
          {{ sectionTitle.details }}
        </div>
        <table
          class="po-v2__grid po-v2__grid--cq"
          :class="{
            'po-v2__grid--cq-cust': showCustomerPnBrand,
            'po-v2__grid--cq-rmk-empty': remarkColumnEmpty
          }"
        >
          <colgroup>
            <col class="c-cq-idx" />
            <col class="c-cq-pn-brand" />
            <col v-if="showCustomerPnBrand" class="c-cq-cust-pn-brand" />
            <col class="c-cq-qty" />
            <col class="c-cq-price" />
            <col class="c-cq-amt" />
            <col class="c-cq-lt" />
            <col class="c-cq-dc" />
            <col class="c-cq-rmk" />
          </colgroup>
          <thead>
            <tr>
              <th>{{ tableHead.no }}</th>
              <th>{{ tableHead.pnBrand }}</th>
              <th v-if="showCustomerPnBrand">{{ tableHead.customerPnBrand }}</th>
              <th>{{ tableHead.qty }}</th>
              <th>{{ tableHead.up }}</th>
              <th>{{ tableHead.amount }}</th>
              <th>{{ tableHead.lt }}</th>
              <th>{{ tableHead.dc }}</th>
              <th class="c-cq-rmk-h">{{ tableHead.remark }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="line in lines" :key="'l' + line.lineNo">
              <td class="cen">{{ line.lineNo }}</td>
              <td class="po-v2__mpn">
                <div>{{ dash(line.mpn) }}</div>
                <div class="po-v2__cq-brand">{{ dash(line.brand) }}</div>
              </td>
              <td v-if="showCustomerPnBrand" class="po-v2__mpn">
                <div>{{ dash(line.customerMpn) }}</div>
                <div class="po-v2__cq-brand">{{ dash(line.customerBrand) }}</div>
              </td>
              <td class="num">{{ dash(line.qty) }}</td>
              <td class="num">{{ dash(line.unitPrice) }} {{ line.currency }}</td>
              <td class="num">{{ dash(line.amount) }} {{ line.currency }}</td>
              <td>{{ dash(line.leadTime) }}</td>
              <td>{{ dash(line.dateCode) }}</td>
              <td>{{ dash(line.remark) }}</td>
            </tr>
            <tr v-if="lines.length === 0">
              <td :colspan="colCount" class="po-v2__empty">{{ emptyText }}</td>
            </tr>
            <template v-if="lines.length > 0">
              <tr
                v-for="(row, idx) in totals"
                :key="'t' + row.currency"
                class="po-v2__sum-row"
              >
                <td>{{ totalLabel }}{{ totals.length > 1 ? ' ' + row.currency : '' }}</td>
                <td></td>
                <td v-if="showCustomerPnBrand"></td>
                <td class="num">{{ idx === 0 ? totalQty : '' }}</td>
                <td></td>
                <td class="num">{{ row.amount }} {{ row.currency }}</td>
                <td colspan="3"></td>
              </tr>
            </template>
          </tbody>
        </table>
        <div v-if="headerRemarkText" class="po-v2__cq-header-remark">{{ headerRemarkText }}</div>
      </section>

      <section class="po-v2__block">
        <div class="po-v2__sec-hd">
          <i class="po-v2__guide" aria-hidden="true" />
          交易条款 / Terms &amp; Conditions
        </div>
        <div class="po-v2__cq-terms">
          <div class="po-v2__cq-terms-grid">
            <div class="po-v2__cq-term">
              <div class="po-v2__cq-term-k">{{ termLabel.payment }}</div>
              <div class="po-v2__cq-term-v">{{ terms.payment }}</div>
            </div>
            <div class="po-v2__cq-term">
              <div class="po-v2__cq-term-k">{{ termLabel.delivery }}</div>
              <div class="po-v2__cq-term-v">{{ terms.delivery }}</div>
            </div>
            <div class="po-v2__cq-term">
              <div class="po-v2__cq-term-k">{{ termLabel.warranty }}</div>
              <div class="po-v2__cq-term-v">{{ terms.warranty }}</div>
            </div>
            <div class="po-v2__cq-term">
              <div class="po-v2__cq-term-k">{{ termLabel.validity }}</div>
              <div class="po-v2__cq-term-v">{{ terms.validity }}</div>
            </div>
          </div>
          <div class="po-v2__cq-term po-v2__cq-term--remark">
            <div class="po-v2__cq-term-k">{{ termLabel.remarks }}</div>
            <div class="po-v2__cq-term-v">{{ terms.remarks }}</div>
          </div>
        </div>
      </section>

      <section class="po-v2__sign">
        <div class="po-v2__sign-box">
          <div class="po-v2__sign-t">{{ signLabel.quoter }}</div>
          <div class="po-v2__sign-pad"></div>
          <div class="po-v2__sign-foot">{{ dateLabel }}{{ dash(signDate) }}</div>
        </div>
        <div class="po-v2__sign-box">
          <div class="po-v2__sign-t">{{ signLabel.customer }}</div>
          <div class="po-v2__sign-pad"></div>
          <div class="po-v2__sign-foot">{{ dateLabel }}</div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { CustomerQuoteReportDocProps } from './types'
/** 注入装箱 Invoice V2 的 `.po-doc--inv-v2` 样式 */
import '@/components/stockOut/invoiceReport/InvoiceReportV2Body.vue'

const props = defineProps<CustomerQuoteReportDocProps>()

function hasText(v?: string | null) {
  const s = (v ?? '').trim()
  return Boolean(s) && s !== '—'
}

function dash(v?: string | null) {
  const s = (v ?? '').trim()
  return s || '—'
}

const headerRemarkText = computed(() => (props.headerRemark ?? '').trim())

const showCustomerPnBrand = computed(() =>
  props.lines.some((line) => hasText(line.customerMpn) || hasText(line.customerBrand))
)

const colCount = computed(() => (showCustomerPnBrand.value ? 9 : 8))

const remarkColumnEmpty = computed(() => props.lines.every((line) => !hasText(line.remark)))

const TABLE_HEAD_ZH = {
  no: '序号',
  pnBrand: '型号/品牌',
  customerPnBrand: '客户型号/品牌',
  qty: '数量',
  up: '单价',
  amount: '金额',
  lt: '交期',
  dc: '生产日期',
  remark: '备注'
} as const

const TABLE_HEAD_EN = {
  no: 'No.',
  pnBrand: 'PN/Brand',
  customerPnBrand: 'Customer PN/Brand',
  qty: 'Qty',
  up: 'UP',
  amount: 'Amount',
  lt: 'LT',
  dc: 'DC',
  remark: 'Remark'
} as const

const tableHead = computed(() => (props.reportLang === 'zh' ? TABLE_HEAD_ZH : TABLE_HEAD_EN))

const sectionTitle = computed(() =>
  props.reportLang === 'zh'
    ? { parties: '客户与报价方', details: '报价明细' }
    : { parties: 'CUSTOMER & QUOTER', details: 'QUOTATION DETAILS' }
)

const partyRole = computed(() =>
  props.reportLang === 'zh'
    ? { billTo: '客户信息', quoter: '报价方' }
    : { billTo: 'Customer Information', quoter: 'Quoter' }
)

const signLabel = computed(() =>
  props.reportLang === 'zh'
    ? { quoter: '报价方（签章）', customer: '客户（签章）' }
    : { quoter: 'Quoter (Signature/Stamp)', customer: 'Customer (Signature/Stamp)' }
)

const emptyText = computed(() => (props.reportLang === 'zh' ? '无明细' : 'No items'))
const totalLabel = computed(() => (props.reportLang === 'zh' ? '合计' : 'Total'))
const dateLabel = computed(() => (props.reportLang === 'zh' ? '日期：' : 'Date: '))

const TERMS_ZH = {
  payment: '月结 30 天，对公转账',
  warranty: '自收货之日起 12 个月',
  remarks: '以上价格含 13% 增值税，不含运费；最终价格以双方签订的采购合同为准。',
  delivery: '确认订单后 15 个工作日内',
  validity: '30 天，逾期请重新询价'
} as const

const TERMS_EN = {
  payment: 'Net 30 days, bank transfer',
  warranty: '12 months from the date of receipt',
  remarks:
    'Prices include 13% VAT, exclude freight; final price is subject to the signed purchase contract.',
  delivery: 'Within 15 working days after order confirmation',
  validity: '30 days; please re-inquire after expiration'
} as const

const terms = computed(() => (props.reportLang === 'zh' ? TERMS_ZH : TERMS_EN))

const termLabel = computed(() =>
  props.reportLang === 'zh'
    ? {
        payment: '付款方式',
        warranty: '质保期',
        remarks: '备注',
        delivery: '交货期',
        validity: '报价有效期'
      }
    : {
        payment: 'Payment',
        warranty: 'Warranty',
        remarks: 'Remarks',
        delivery: 'Delivery',
        validity: 'Validity'
      }
)
</script>

<style lang="scss">
.po-doc--inv-v2 .po-v2__cq-header-remark {
  margin-top: 2.2mm;
  padding: 0 0.4mm;
  font-size: 8pt;
  line-height: 1.45;
  color: #333;
  white-space: pre-wrap;
  word-break: break-word;
}

.po-doc--inv-v2 .po-v2__cq-terms {
  padding: 2.4mm 3.2mm 2.2mm;
  background: var(--po-v2-panel);
  border: 1px solid var(--po-v2-border);
}

.po-doc--inv-v2 .po-v2__cq-terms-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 2.2mm 8mm;
  margin-bottom: 2.2mm;
}

.po-doc--inv-v2 .po-v2__cq-term-k {
  font-size: 8pt;
  font-weight: 700;
  color: var(--po-v2-ink);
  margin-bottom: 0.6mm;
}

.po-doc--inv-v2 .po-v2__cq-term-v {
  font-size: 8pt;
  line-height: 1.45;
  color: #333;
}

.po-doc--inv-v2 .po-v2__cq-term--remark .po-v2__cq-term-k,
.po-doc--inv-v2 .po-v2__cq-term--remark .po-v2__cq-term-v {
  font-weight: 700;
}

.po-doc--inv-v2 .po-v2__grid--cq .po-v2__cq-brand {
  font-weight: 400;
}

.po-doc--inv-v2 .po-v2__grid--cq {
  thead .c-cq-rmk-h {
    white-space: nowrap;
  }

  .c-cq-idx {
    width: 6%;
  }
  .c-cq-pn-brand {
    width: 28%;
  }
  .c-cq-qty {
    width: 8%;
  }
  .c-cq-price {
    width: 12%;
  }
  .c-cq-amt {
    width: 13%;
  }
  .c-cq-lt {
    width: 10%;
  }
  .c-cq-dc {
    width: 11%;
  }
  .c-cq-rmk {
    width: 12%;
    min-width: 6.4em;
  }

  &.po-v2__grid--cq-rmk-empty {
    .c-cq-pn-brand {
      width: 34%;
    }
    .c-cq-rmk {
      width: 6.4em;
      min-width: 6.4em;
    }
  }

  &.po-v2__grid--cq-cust {
    .c-cq-idx {
      width: 5%;
    }
    .c-cq-pn-brand,
    .c-cq-cust-pn-brand {
      width: 16%;
    }
    .c-cq-qty {
      width: 7%;
    }
    .c-cq-price {
      width: 11%;
    }
    .c-cq-amt {
      width: 12%;
    }
    .c-cq-lt {
      width: 9%;
    }
    .c-cq-dc {
      width: 10%;
    }
    .c-cq-rmk {
      width: 14%;
      min-width: 6.4em;
    }

    &.po-v2__grid--cq-rmk-empty {
      .c-cq-pn-brand,
      .c-cq-cust-pn-brand {
        width: 19%;
      }
      .c-cq-rmk {
        width: 6.4em;
        min-width: 6.4em;
      }
    }
  }
}
</style>
