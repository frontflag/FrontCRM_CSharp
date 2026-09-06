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
          <div class="po-v2__title-zh">客户对账单</div>
          <div class="po-v2__title-en">CUSTOMER STATEMENT</div>
          <div class="po-v2__po-no">{{ currencyLabel }} {{ currency }}</div>
        </div>
      </header>
      <div class="po-v2__fade" aria-hidden="true" />

      <div class="po-v2__meta po-v2__meta--inv">
        <div class="po-v2__meta-cell">
          <div class="po-v2__meta-k">{{ meta.generatedOn }}</div>
          <div class="po-v2__meta-v">{{ dash(generatedOn) }}</div>
        </div>
        <div class="po-v2__meta-cell">
          <div class="po-v2__meta-k">{{ meta.currency }}</div>
          <div class="po-v2__meta-v">{{ dash(currency) }}</div>
        </div>
      </div>

      <section class="po-v2__block">
        <div class="po-v2__sec-hd">
          <i class="po-v2__guide" aria-hidden="true" />
          {{ section.parties }}
        </div>
        <div class="po-v2__parties">
          <div class="po-v2__party">
            <div class="po-v2__party-role">{{ party.customer }}</div>
            <div class="po-v2__addr-body">
              <div v-for="(line, i) in customerLines" :key="'c' + i" class="po-v2__addr-line">{{ line }}</div>
            </div>
          </div>
          <div class="po-v2__party">
            <div class="po-v2__party-role">{{ party.terms }}</div>
            <div class="po-v2__addr-body">
              <div v-for="(line, i) in termLines" :key="'t' + i" class="po-v2__addr-line">{{ line }}</div>
            </div>
          </div>
        </div>
      </section>

      <section class="po-v2__block">
        <div class="po-v2__sec-hd">
          <i class="po-v2__guide" aria-hidden="true" />
          {{ section.summary }}
        </div>
        <div class="po-v2__stmt-kpis">
          <div class="po-v2__stmt-kpi">
            <div class="po-v2__stmt-kpi-k">{{ kpi.opening }}</div>
            <div class="po-v2__stmt-kpi-v">{{ opening }}</div>
          </div>
          <div class="po-v2__stmt-kpi">
            <div class="po-v2__stmt-kpi-k">{{ kpi.increase }}</div>
            <div class="po-v2__stmt-kpi-v">{{ periodIncrease }}</div>
          </div>
          <div class="po-v2__stmt-kpi">
            <div class="po-v2__stmt-kpi-k">{{ kpi.ending }}</div>
            <div class="po-v2__stmt-kpi-v po-v2__stmt-kpi-v--end">{{ ending }}</div>
          </div>
        </div>
      </section>

      <section class="po-v2__block">
        <div class="po-v2__sec-hd">
          <i class="po-v2__guide" aria-hidden="true" />
          {{ section.ledger }}
        </div>
        <table class="po-v2__grid po-v2__grid--stmt">
          <colgroup>
            <col class="c-stmt-date" />
            <col class="c-stmt-doc" />
            <col class="c-stmt-sum" />
            <col class="c-stmt-inc" />
            <col class="c-stmt-recv" />
            <col class="c-stmt-bal" />
          </colgroup>
          <thead>
            <tr>
              <th>{{ table.date }}</th>
              <th>{{ table.docNo }}</th>
              <th>{{ table.summary }}</th>
              <th>{{ table.increase }}</th>
              <th>{{ table.received }}</th>
              <th>{{ table.balance }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(row, idx) in lines" :key="idx">
              <td class="cen">{{ row.date }}</td>
              <td>{{ row.docNo }}</td>
              <td>{{ row.summary }}</td>
              <td class="num">{{ row.increase }}</td>
              <td class="num po-v2__stmt-recv">{{ row.received }}</td>
              <td class="num" :class="{ 'po-v2__stmt-end': idx === lines.length - 1 }">{{ row.balance }}</td>
            </tr>
            <tr class="po-v2__sum-row">
              <td colspan="3">{{ table.total }}</td>
              <td class="num">{{ periodIncrease }}</td>
              <td class="num po-v2__stmt-recv">{{ periodReceived }}</td>
              <td class="num po-v2__stmt-end">{{ ending }}</td>
            </tr>
          </tbody>
        </table>
      </section>

      <section class="po-v2__block">
        <div class="po-v2__sec-hd">
          <i class="po-v2__guide" aria-hidden="true" />
          {{ section.notes }}
        </div>
        <div class="po-v2__cq-terms">
          <ol class="po-v2__stmt-notes">
            <li>{{ notes.n1 }}</li>
            <li>{{ notes.n2 }}</li>
          </ol>
        </div>
      </section>

      <section class="po-v2__sign">
        <div class="po-v2__sign-box">
          <div class="po-v2__sign-t">{{ sign.supplier }}</div>
          <div class="po-v2__sign-pad"></div>
          <div class="po-v2__sign-foot">{{ dateLabel }}{{ dash(generatedOn) }}</div>
        </div>
        <div class="po-v2__sign-box">
          <div class="po-v2__sign-t">{{ sign.customer }}</div>
          <div class="po-v2__sign-pad"></div>
          <div class="po-v2__sign-foot">{{ dateLabel }}</div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
/** 注入装箱 Invoice V2 / 客户报价单同一套 `.po-doc--inv-v2` 样式 */
import '@/components/stockOut/invoiceReport/InvoiceReportV2Body.vue'

export interface StatementReportLine {
  date: string
  docNo: string
  summary: string
  increase: string
  received: string
  balance: string
}

const props = defineProps<{
  reportLang: 'zh' | 'en'
  logoUrl?: string | null
  headerCompanyName: string
  generatedOn: string
  currency: string
  customerLines: string[]
  termLines: string[]
  opening: string
  periodIncrease: string
  periodReceived: string
  ending: string
  lines: StatementReportLine[]
}>()

function dash(v?: string | null) {
  const s = (v ?? '').trim()
  return s || '—'
}

const zh = computed(() => props.reportLang !== 'en')

const currencyLabel = computed(() => (zh.value ? '币种 / Currency' : 'Currency'))

const meta = computed(() =>
  zh.value
    ? { generatedOn: '生成日期 / GENERATED ON', currency: '币种 / CURRENCY' }
    : { generatedOn: 'GENERATED ON', currency: 'CURRENCY' }
)

const section = computed(() =>
  zh.value
    ? { parties: '客户与对账条件', summary: '对账摘要', ledger: '对账流水', notes: '备注' }
    : { parties: 'CUSTOMER & TERMS', summary: 'SUMMARY', ledger: 'LEDGER', notes: 'NOTES' }
)

const party = computed(() =>
  zh.value
    ? { customer: '客户信息', terms: '对账条件' }
    : { customer: 'Customer Information', terms: 'Statement Terms' }
)

const kpi = computed(() =>
  zh.value
    ? { opening: '期初应收余额', increase: '本期应收增加', ending: '期末应收余额' }
    : { opening: 'Opening AR', increase: 'Period Increase', ending: 'Ending AR' }
)

const table = computed(() =>
  zh.value
    ? {
        date: '日期',
        docNo: '单据号',
        summary: '摘要',
        increase: '应收增加',
        received: '已收/扣减',
        balance: '余额',
        total: '合计'
      }
    : {
        date: 'Date',
        docNo: 'Document No.',
        summary: 'Description',
        increase: 'AR Increase',
        received: 'Received',
        balance: 'Balance',
        total: 'Total'
      }
)

const notes = computed(() =>
  zh.value
    ? {
        n1: '本单仅供对账，不是收款凭证。',
        n2: '如有异议，请在五个工作日内提出。'
      }
    : {
        n1: 'This statement is for reconciliation only and is not a payment voucher.',
        n2: 'Please raise disputes within five business days.'
      }
)

const sign = computed(() =>
  zh.value
    ? { supplier: '供方确认（签章）', customer: '客户确认（签章）' }
    : { supplier: 'Supplier (Signature/Stamp)', customer: 'Customer (Signature/Stamp)' }
)

const dateLabel = computed(() => (zh.value ? '日期：' : 'Date: '))
</script>

<style lang="scss">
.po-doc--inv-v2 .po-v2__stmt-kpis {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  border: 1px solid var(--po-v2-border);
  background: var(--po-v2-wash);
}

.po-doc--inv-v2 .po-v2__stmt-kpi {
  padding: 2.4mm 3mm 2.6mm;
  border-right: 1px solid var(--po-v2-border);
}

.po-doc--inv-v2 .po-v2__stmt-kpi:last-child {
  border-right: none;
}

.po-doc--inv-v2 .po-v2__stmt-kpi-k {
  font-size: 7pt;
  font-weight: 700;
  color: #4b5563;
  margin-bottom: 1.2mm;
}

.po-doc--inv-v2 .po-v2__stmt-kpi-v {
  font-size: 13pt;
  font-weight: 800;
  font-variant-numeric: tabular-nums;
  color: var(--po-v2-ink);
}

.po-doc--inv-v2 .po-v2__stmt-kpi-v--end,
.po-doc--inv-v2 .po-v2__stmt-end {
  color: #c0392b;
}

.po-doc--inv-v2 .po-v2__stmt-recv {
  color: #2f9e44;
  font-weight: 700;
}

.po-doc--inv-v2 .po-v2__grid--stmt {
  .c-stmt-date {
    width: 14%;
  }
  .c-stmt-doc {
    width: 16%;
  }
  .c-stmt-sum {
    width: 28%;
  }
  .c-stmt-inc,
  .c-stmt-recv,
  .c-stmt-bal {
    width: 14%;
  }
}

.po-doc--inv-v2 .po-v2__stmt-notes {
  margin: 0;
  padding-left: 5mm;
  font-size: 8pt;
  line-height: 1.55;
  color: #333;
}
</style>
