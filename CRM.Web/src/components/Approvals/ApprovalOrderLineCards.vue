<template>
  <div class="approval-order-ref" :class="{ 'approval-order-ref--cols-2': columns === 2 }" v-loading="loading">
    <p v-if="!orderId" class="approval-order-ref__hint">
      {{ t('approvalDesktop.orderRef.emptyOrder') }}
    </p>
    <template v-else>
      <section class="approval-order-ref__panel">
        <div class="approval-order-ref__panel-title">
          {{ t('approvalDesktop.orderRef.overviewTitle') }}
        </div>
        <div class="approval-order-ref__kv">
          <div class="approval-order-ref__row">
            <span class="k">{{ overviewCodeLabel }}</span>
            <span class="v" :title="overview.code">{{ overview.code || '—' }}</span>
          </div>
          <div v-if="mode === 'sales'" class="approval-order-ref__row">
            <span class="k">{{ t('approvalDesktop.orderRef.orderType') }}</span>
            <span class="v">{{ overview.orderType || '—' }}</span>
          </div>
          <div v-if="mode === 'purchase'" class="approval-order-ref__row">
            <span class="k">{{ t('approvalDesktop.orderRef.createTime') }}</span>
            <span class="v">{{ overview.createTime || '—' }}</span>
          </div>
          <div class="approval-order-ref__row">
            <span class="k">{{ overviewPartyLabel }}</span>
            <span class="v" :title="overview.party">{{ overview.party || '—' }}</span>
          </div>
          <div v-if="mode === 'purchase'" class="approval-order-ref__row">
            <span class="k">{{ t('approvalDesktop.orderRef.orderType') }}</span>
            <span class="v">{{ overview.orderType || '—' }}</span>
          </div>
          <div class="approval-order-ref__row">
            <span class="k">{{ t('approvalDesktop.orderRef.orderAmount') }}</span>
            <span class="v">
              <template v-if="overview.amountText === '—'">—</template>
              <span v-else class="amount-with-code">
                <span class="approval-order-ref__amount-num">{{ overview.amountText }}</span>
                <span
                  v-if="overview.currencyIso"
                  :class="['dock-tier-ccy', listAmountCurrencyDockClass(overview.currency)]"
                >{{ overview.currencyIso }}</span>
              </span>
            </span>
          </div>
          <div v-if="mode === 'sales'" class="approval-order-ref__row">
            <span class="k">{{ t('approvalDesktop.orderRef.createTime') }}</span>
            <span class="v">{{ overview.createTime || '—' }}</span>
          </div>
          <div class="approval-order-ref__row">
            <span class="k">{{ overviewUserLabel }}</span>
            <span class="v" :title="overview.salesUserAccount">{{ overview.salesUserAccount || '—' }}</span>
          </div>
        </div>
      </section>

      <section
        class="approval-order-ref__panel"
        :class="{ 'approval-order-ref__panel--lines-collapsed': linesCollapsed }"
      >
        <div class="approval-order-ref__panel-head">
          <div class="approval-order-ref__panel-title">
            {{ t('approvalDesktop.orderRef.linesTitle') }}
            <span class="approval-order-ref__panel-count">{{ t('approvalDesktop.orderRef.linesCount', { n: lines.length }) }}</span>
          </div>
          <el-tooltip
            :content="linesCollapsed ? t('approvalDesktop.orderRef.expandLines') : t('approvalDesktop.orderRef.collapseLines')"
            placement="top"
            :show-after="200"
          >
            <el-button
              size="small"
              text
              type="primary"
              class="approval-order-ref__fold-btn"
              :aria-expanded="!linesCollapsed"
              :aria-label="linesCollapsed ? t('approvalDesktop.orderRef.expandLines') : t('approvalDesktop.orderRef.collapseLines')"
              @click="linesCollapsed = !linesCollapsed"
            >
              <el-icon>
                <ArrowDown v-if="linesCollapsed" />
                <ArrowUp v-else />
              </el-icon>
            </el-button>
          </el-tooltip>
        </div>
        <div v-show="!linesCollapsed">
          <p v-if="!loading && lines.length === 0" class="approval-order-ref__hint">
            {{ t('approvalDesktop.orderRef.emptyItems') }}
          </p>
          <div v-else class="approval-order-ref__list">
          <div v-for="line in lines" :key="line.key" class="line-card">
            <div
              class="line-card__row"
              :class="{ 'line-card__row--span': mode === 'purchase' }"
            >
              <span class="k">{{ t('approvalDesktop.orderRef.itemCode') }}</span>
              <span class="v" :title="line.itemCode">{{ line.itemCode || '—' }}</span>
            </div>
            <div v-if="mode === 'sales'" class="line-card__row">
              <span class="k">{{ t('approvalDesktop.orderRef.customerSo') }}</span>
              <span class="v" :title="line.customerSo">{{ line.customerSo || '—' }}</span>
            </div>
            <div class="line-card__row">
              <span class="k">{{ t('approvalDesktop.orderRef.pn') }}</span>
              <span class="v" :title="line.pn">{{ line.pn || '—' }}</span>
            </div>
            <div class="line-card__row">
              <span class="k">{{ t('approvalDesktop.orderRef.brand') }}</span>
              <span class="v">{{ line.brand || '—' }}</span>
            </div>
            <div class="line-card__row">
              <span class="k">{{ t('approvalDesktop.orderRef.dateCode') }}</span>
              <span class="v" :title="line.dateCode">{{ line.dateCode || '—' }}</span>
            </div>
            <div class="line-card__row">
              <span class="k">{{ t('approvalDesktop.orderRef.deliveryDate') }}</span>
              <span class="v">{{ line.deliveryDate || '—' }}</span>
            </div>
            <div class="line-card__row">
              <span class="k">{{ unitPriceLabel }}</span>
              <span class="v">
                <template v-if="line.unitPriceText === '—'">—</template>
                <span v-else class="amount-with-code">
                  <span>{{ line.unitPriceText }}</span>
                  <span
                    v-if="line.currencyIso"
                    :class="['dock-tier-ccy', listAmountCurrencyDockClass(line.currency)]"
                  >{{ line.currencyIso }}</span>
                </span>
              </span>
            </div>
            <div class="line-card__row">
              <span class="k">{{ t('approvalDesktop.orderRef.qty') }}</span>
              <span class="v">{{ formatQty(line.qty) }}</span>
            </div>
            <div class="line-card__row">
              <span class="k">{{ profitLabel }}</span>
              <span class="v line-card__profit-v">
                <template v-if="line.profitText === '—'">—</template>
                <span v-else class="amount-with-code">
                  <span>{{ line.profitText }}</span>
                  <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
                </span>
                <el-popover
                  v-if="line.profitFormulaLines.length"
                  placement="left"
                  :width="720"
                  trigger="click"
                  :teleported="true"
                  popper-class="approval-order-ref-profit-tip"
                >
                  <template #reference>
                    <button
                      type="button"
                      class="line-card__tip-btn"
                      :aria-label="profitTipAria"
                      @click.stop
                    >
                      <el-icon><QuestionFilled /></el-icon>
                    </button>
                  </template>
                  <div class="line-card__tip-body">
                    <p class="line-card__tip-title">{{ profitTipTitle }}</p>
                    <ul class="line-card__tip-list">
                      <li v-for="(text, idx) in line.profitFormulaLines" :key="idx">{{ text }}</li>
                    </ul>
                  </div>
                </el-popover>
              </span>
            </div>
            <div class="line-card__row">
              <span class="k">{{ profitRateLabel }}</span>
              <span class="v line-card__profit-v">
                <template v-if="line.profitRateText === '—'">—</template>
                <span v-else>{{ line.profitRateText }}</span>
                <el-popover
                  v-if="line.profitRateFormulaLines.length"
                  placement="left"
                  :width="720"
                  trigger="click"
                  :teleported="true"
                  popper-class="approval-order-ref-profit-tip"
                >
                  <template #reference>
                    <button
                      type="button"
                      class="line-card__tip-btn"
                      :aria-label="profitRateTipAria"
                      @click.stop
                    >
                      <el-icon><QuestionFilled /></el-icon>
                    </button>
                  </template>
                  <div class="line-card__tip-body">
                    <p class="line-card__tip-title">{{ profitRateTipTitle }}</p>
                    <ul class="line-card__tip-list">
                      <li v-for="(text, idx) in line.profitRateFormulaLines" :key="idx">{{ text }}</li>
                    </ul>
                  </div>
                </el-popover>
              </span>
            </div>
          </div>
        </div>
        </div>
      </section>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ArrowDown, ArrowUp, QuestionFilled } from '@element-plus/icons-vue'
import { salesOrderApi, type SellOrderLineProfit } from '@/api/salesOrder'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { formatTotalAmountNumber, formatUnitPriceNumber, listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'
import { formatUsdProfitAmount, formatProfitRateMultiplierDisplay } from '@/utils/sellOrderLineProfitDisplay'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatVendorNameReadonly } from '@/utils/vendorDisplayName'
import { formatCustomerNameReadonly } from '@/utils/customerDisplayName'
import {
  productionDateDisplayLabel,
  useMaterialProductionDateDict
} from '@/composables/useMaterialProductionDateDict'

const props = withDefaults(
  defineProps<{
    mode: 'sales' | 'purchase'
    orderId: string
    /** 来自审批工作区已加载的明细；缺省时组件内自行拉取 */
    items?: any[] | null
    /** 中栏宽面板用 2 列；右栏窄页签保持 1 列 */
    columns?: 1 | 2
  }>(),
  { columns: 1 }
)

const { t } = useI18n()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { options: materialPdOptions, ensureLoaded: ensureMaterialPdDict } = useMaterialProductionDateDict()

const loading = ref(false)
const linesCollapsed = ref(false)
const overview = ref({
  code: '',
  party: '',
  createTime: '',
  salesUserAccount: '',
  orderType: '',
  amountText: '—',
  currency: undefined as number | undefined,
  currencyIso: ''
})
const lines = ref<
  Array<{
    key: string
    itemCode: string
    pn: string
    brand: string
    customerSo: string
    dateCode: string
    deliveryDate: string
    qty: number
    unitPriceText: string
    currency?: number
    currencyIso: string
    profitText: string
    profitFormulaLines: string[]
    profitRateText: string
    profitRateFormulaLines: string[]
  }>
>([])

function fmtTipQty(value: number): string {
  if (!Number.isFinite(value)) return '—'
  if (Math.abs(value - Math.round(value)) < 1e-9) return String(Math.round(value))
  return value.toFixed(4).replace(/\.?0+$/, '')
}

function fmtTipUnitPrice(value: number): string {
  if (!Number.isFinite(value)) return '—'
  return value.toFixed(2)
}

/** 审批右栏 Tip：报价利润 = (销售单价 − 报价) × 销售数量 */
function buildQuoteProfitTip(lp: SellOrderLineProfit | null | undefined): string[] {
  if (!lp) return []
  const qty = Number(lp.qty)
  const sellUnit = Number(lp.convertPrice)
  const quoteUnit = Number(lp.quoteConvertCost)
  if (!Number.isFinite(qty) || !Number.isFinite(sellUnit)) return []
  if (!(quoteUnit > 0)) {
    return [t('approvalDesktop.orderRef.quoteProfitTipNoCost')]
  }
  return [
    t('approvalDesktop.orderRef.quoteProfitTipFormula', {
      sellUnitPrice: fmtTipUnitPrice(sellUnit),
      quotePrice: fmtTipUnitPrice(quoteUnit),
      qty: fmtTipQty(qty),
      result: formatUsdProfitAmount(lp.quote?.profitUsd)
    })
  ]
}

/** 审批桌面 Tip：报价利润率 = 销售单价 ÷ 报价 */
function buildQuoteProfitRateTip(lp: SellOrderLineProfit | null | undefined): string[] {
  if (!lp) return []
  const sellUnit = Number(lp.convertPrice)
  const quoteUnit = Number(lp.quoteConvertCost)
  if (!Number.isFinite(sellUnit)) return []
  if (!(quoteUnit > 0)) {
    return [t('approvalDesktop.orderRef.quoteProfitRateTipNoCost')]
  }
  return [
    t('approvalDesktop.orderRef.quoteProfitRateTipFormula', {
      sellUnitPrice: fmtTipUnitPrice(sellUnit),
      quotePrice: fmtTipUnitPrice(quoteUnit),
      result: formatProfitRateMultiplierDisplay(lp.quote?.profitUsd, lp.quote?.profitRate, 2)
    })
  ]
}

type PurchaseProfitOpts = {
  purchaseOrderItemId: string
  purchaseQty: number
  purchaseConvertPrice?: number
}

function resolvePurchaseProfitInputs(
  lp: SellOrderLineProfit | null | undefined,
  opts: PurchaseProfitOpts
): { sellUnit: number; purchaseUnit: number; qty: number } | null {
  if (!lp) return null
  const sellUnit = Number(lp.convertPrice)
  if (!Number.isFinite(sellUnit)) return null

  const poLine = (lp.poCostLines ?? []).find(
    (x) => String(x.purchaseOrderItemId ?? '').trim() === opts.purchaseOrderItemId
  )
  const purchaseUnit = Number(
    opts.purchaseConvertPrice ?? poLine?.convertPriceUsd ?? lp.avgPoCostUsd ?? 0
  )
  const qty = Number(
    Number.isFinite(opts.purchaseQty) && opts.purchaseQty > 0
      ? opts.purchaseQty
      : poLine?.qty ?? lp.poQtyTotal ?? 0
  )
  return { sellUnit, purchaseUnit, qty }
}

/** 审批右栏 Tip：采购利润 = (销售单价 − 采购单价) × 采购数量 */
function buildPurchaseProfitTip(
  lp: SellOrderLineProfit | null | undefined,
  opts: PurchaseProfitOpts
): string[] {
  const inputs = resolvePurchaseProfitInputs(lp, opts)
  if (!inputs) return []
  const { sellUnit, purchaseUnit, qty } = inputs
  if (!(purchaseUnit > 0) || !(qty > 0)) {
    return [t('approvalDesktop.orderRef.purchaseProfitTipNoCost')]
  }
  return [
    t('approvalDesktop.orderRef.purchaseProfitTipFormula', {
      sellUnitPrice: fmtTipUnitPrice(sellUnit),
      purchaseUnitPrice: fmtTipUnitPrice(purchaseUnit),
      qty: fmtTipQty(qty),
      result: formatUsdProfitAmount(lp?.purchaseProfitExpected)
    })
  ]
}

/** 审批桌面 Tip：采购利润率 = 销售单价 ÷ 采购单价 */
function buildPurchaseProfitRateTip(
  lp: SellOrderLineProfit | null | undefined,
  opts: PurchaseProfitOpts
): string[] {
  const inputs = resolvePurchaseProfitInputs(lp, opts)
  if (!inputs) return []
  const { sellUnit, purchaseUnit } = inputs
  if (!(purchaseUnit > 0)) {
    return [t('approvalDesktop.orderRef.purchaseProfitRateTipNoCost')]
  }
  const rate = sellUnit / purchaseUnit
  return [
    t('approvalDesktop.orderRef.purchaseProfitRateTipFormula', {
      sellUnitPrice: fmtTipUnitPrice(sellUnit),
      purchaseUnitPrice: fmtTipUnitPrice(purchaseUnit),
      result: formatProfitRateMultiplierDisplay(lp?.purchaseProfitExpected, rate, 2)
    })
  ]
}

const overviewCodeLabel = computed(() =>
  props.mode === 'sales'
    ? t('approvalDesktop.orderRef.salesOrderCode')
    : t('approvalDesktop.orderRef.purchaseOrderCode')
)
const overviewPartyLabel = computed(() =>
  props.mode === 'sales'
    ? t('approvalDesktop.orderRef.customerName')
    : t('approvalDesktop.orderRef.vendorName')
)
const overviewUserLabel = computed(() =>
  props.mode === 'sales'
    ? t('approvalDesktop.orderRef.salesUserAccount')
    : t('approvalDesktop.orderRef.purchaseUserAccount')
)
const unitPriceLabel = computed(() =>
  props.mode === 'sales'
    ? t('approvalDesktop.orderRef.sellUnitPrice')
    : t('approvalDesktop.orderRef.purchaseUnitPrice')
)
const profitLabel = computed(() =>
  props.mode === 'sales'
    ? t('approvalDesktop.orderRef.quoteProfit')
    : t('approvalDesktop.orderRef.purchaseProfit')
)
const profitTipTitle = computed(() =>
  props.mode === 'sales'
    ? t('approvalDesktop.orderRef.quoteProfitTipTitle')
    : t('approvalDesktop.orderRef.purchaseProfitTipTitle')
)
const profitTipAria = computed(() =>
  props.mode === 'sales'
    ? t('approvalDesktop.orderRef.quoteProfitTipAria')
    : t('approvalDesktop.orderRef.purchaseProfitTipAria')
)
const profitRateLabel = computed(() =>
  props.mode === 'sales'
    ? t('approvalDesktop.orderRef.quoteProfitRate')
    : t('approvalDesktop.orderRef.purchaseProfitRate')
)
const profitRateTipTitle = computed(() =>
  props.mode === 'sales'
    ? t('approvalDesktop.orderRef.quoteProfitRateTipTitle')
    : t('approvalDesktop.orderRef.purchaseProfitRateTipTitle')
)
const profitRateTipAria = computed(() =>
  props.mode === 'sales'
    ? t('approvalDesktop.orderRef.quoteProfitRateTipAria')
    : t('approvalDesktop.orderRef.purchaseProfitRateTipAria')
)

function formatQty(q: number) {
  if (!Number.isFinite(q)) return '—'
  if (Math.abs(q - Math.round(q)) < 1e-9) return String(Math.round(q))
  return q.toLocaleString('zh-CN', { maximumFractionDigits: 4 })
}

function pickCustomerSo(it: Record<string, unknown>): string {
  const raw =
    it.customerSo ??
    it.CustomerSo ??
    it.customerPO ??
    it.CustomerPO ??
    it.customerPo ??
    it.CustomerPo ??
    ''
  return String(raw ?? '').trim()
}

function formatLineDateCode(it: Record<string, unknown>): string {
  const raw = String(it.dateCode ?? it.DateCode ?? '').trim()
  if (!raw) return ''
  return productionDateDisplayLabel(raw, materialPdOptions.value) || raw
}

function formatLineDeliveryDate(it: Record<string, unknown>): string {
  const v = it.deliveryDate ?? it.DeliveryDate
  if (v == null || v === '') return ''
  const s = formatDisplayDate(v as string | Date)
  return s === '--' ? '' : s
}

function normalizeItems(raw: any[] | null | undefined): any[] {
  return Array.isArray(raw) ? raw : []
}

function formatCreateTime(raw: unknown): string {
  const s = String(raw ?? '').trim()
  if (!s) return ''
  return formatDisplayDateTime(s)
}

function emptyOverview() {
  return {
    code: '',
    party: '',
    createTime: '',
    salesUserAccount: '',
    orderType: '',
    amountText: '—',
    currency: undefined as number | undefined,
    currencyIso: ''
  }
}

function formatSalesOrderType(raw: unknown): string {
  const n = Number(raw)
  if (n === 1) return t('salesOrderCreate.orderTypes.normal')
  if (n === 2) return t('salesOrderCreate.orderTypes.urgent')
  if (n === 3) return t('salesOrderCreate.orderTypes.sample')
  return ''
}

async function resolveSellOrderIdMap(poId: string): Promise<Map<string, string>> {
  const map = new Map<string, string>()
  if (!poId) return map
  try {
    const agg = await purchaseOrderApi.getDetailTabAggregates(poId)
    const prs = Array.isArray(agg?.purchaseRequisitions) ? agg.purchaseRequisitions : []
    for (const pr of prs) {
      const soi = String(pr.sellOrderItemId || '').trim()
      const so = String(pr.sellOrderId || '').trim()
      if (soi && so) map.set(soi, so)
    }
  } catch {
    /* ignore */
  }
  return map
}

async function loadPanel() {
  const orderId = String(props.orderId || '').trim()
  overview.value = emptyOverview()
  lines.value = []
  if (!orderId) return

  loading.value = true
  try {
    if (props.mode === 'sales') {
      const detail = await salesOrderApi.getById(orderId)
      const d = (detail as any)?.data ?? detail
      const mask = maskSaleSensitiveFields.value
      const currency = Number(d?.currency ?? d?.Currency ?? 0) || undefined
      overview.value = {
        code: String(d?.sellOrderCode ?? d?.SellOrderCode ?? '').trim(),
        party: mask
          ? '—'
          : formatCustomerNameReadonly(
              d?.customerName || d?.CustomerName,
              d?.customerEnglishName || d?.CustomerEnglishName,
              { masked: false }
            ),
        createTime: formatCreateTime(d?.createTime ?? d?.CreateTime),
        salesUserAccount: mask
          ? '—'
          : String(d?.salesUserName ?? d?.SalesUserName ?? d?.salesUserId ?? d?.SalesUserId ?? '').trim(),
        orderType: formatSalesOrderType(d?.type ?? d?.Type),
        amountText: mask ? '—' : formatTotalAmountNumber(d?.total ?? d?.Total),
        currency: mask ? undefined : currency,
        currencyIso: mask || currency == null ? '' : listAmountCurrencyIso(currency)
      }

      const items = normalizeItems(props.items?.length ? props.items : d?.items ?? d?.Items)
      await ensureMaterialPdDict()
      lines.value = await Promise.all(
        items.slice(0, 40).map(async (it, idx) => {
          const itemId = String(it.id ?? it.Id ?? '').trim()
          const itemCode = String(
            it.sellOrderItemCode ?? it.SellOrderItemCode ?? it.itemCode ?? itemId
          ).trim()
          const pn = String(it.pn ?? it.PN ?? '').trim()
          const brand = String(it.brand ?? it.Brand ?? '').trim()
          const qty = Number(it.qty ?? it.Qty ?? 0)
          const price = Number(it.price ?? it.Price ?? 0)
          const currency = Number(it.currency ?? it.Currency ?? 0) || undefined
          let profitText = '—'
          let profitFormulaLines: string[] = []
          let profitRateText = '—'
          let profitRateFormulaLines: string[] = []
          if (!mask && itemId) {
            try {
              const lp = await salesOrderApi.getSellOrderItemLineProfit(orderId, itemId)
              profitText = formatUsdProfitAmount(lp?.quote?.profitUsd)
              profitFormulaLines = buildQuoteProfitTip(lp)
              profitRateText = formatProfitRateMultiplierDisplay(lp?.quote?.profitUsd, lp?.quote?.profitRate, 2)
              profitRateFormulaLines = buildQuoteProfitRateTip(lp)
            } catch {
              profitText = '—'
              profitRateText = '—'
            }
          }
          return {
            key: itemId || `so-${idx}`,
            itemCode,
            pn,
            brand,
            customerSo: mask ? '—' : pickCustomerSo(it),
            dateCode: formatLineDateCode(it),
            deliveryDate: formatLineDeliveryDate(it),
            qty,
            unitPriceText: mask ? '—' : formatUnitPriceNumber(price),
            currency: mask ? undefined : currency,
            currencyIso: mask || currency == null ? '' : listAmountCurrencyIso(currency),
            profitText: mask ? '—' : profitText,
            profitFormulaLines,
            profitRateText: mask ? '—' : profitRateText,
            profitRateFormulaLines
          }
        })
      )
      return
    }

    // purchase
    const detail = await purchaseOrderApi.getById(orderId)
    const d = (detail as any)?.data ?? detail
    const mask = maskPurchaseSensitiveFields.value
    const currency = Number(d?.currency ?? d?.Currency ?? 0) || undefined
    overview.value = {
      ...emptyOverview(),
      code: String(d?.purchaseOrderCode ?? d?.PurchaseOrderCode ?? '').trim(),
      party: mask
        ? '—'
        : formatVendorNameReadonly(
            d?.vendorName || d?.VendorName || d?.officialName,
            d?.vendorEnglishName || d?.VendorEnglishName,
            { masked: false }
          ),
      createTime: formatCreateTime(d?.createTime ?? d?.CreateTime),
      orderType: formatSalesOrderType(d?.type ?? d?.Type),
      salesUserAccount: mask
        ? '—'
        : String(
            d?.purchaseUserName ??
              d?.PurchaseUserName ??
              d?.purchaseUserId ??
              d?.PurchaseUserId ??
              d?.salesUserName ??
              d?.SalesUserName ??
              ''
          ).trim(),
      amountText: mask ? '—' : formatTotalAmountNumber(d?.total ?? d?.Total),
      currency: mask ? undefined : currency,
      currencyIso: mask || currency == null ? '' : listAmountCurrencyIso(currency)
    }

    const items = normalizeItems(props.items?.length ? props.items : d?.items ?? d?.Items)
    const soIdMap = await resolveSellOrderIdMap(orderId)
    await ensureMaterialPdDict()

    lines.value = await Promise.all(
      items.slice(0, 40).map(async (it, idx) => {
        const itemId = String(it.id ?? it.Id ?? '').trim()
        const itemCode = String(
          it.purchaseOrderItemCode ?? it.PurchaseOrderItemCode ?? it.itemCode ?? itemId
        ).trim()
        const pn = String(it.pn ?? it.PN ?? '').trim()
        const brand = String(it.brand ?? it.Brand ?? '').trim()
        const qty = Number(it.qty ?? it.Qty ?? 0)
        const cost = Number(it.cost ?? it.Cost ?? 0)
        const lineCurrency = Number(it.currency ?? it.Currency ?? 0) || undefined
        const sellOrderItemId = String(it.sellOrderItemId ?? it.SellOrderItemId ?? '').trim()
        const sellOrderId = String(
          it.sellOrderId ?? it.SellOrderId ?? soIdMap.get(sellOrderItemId) ?? ''
        ).trim()
        let profitText = '—'
        let profitFormulaLines: string[] = []
        let profitRateText = '—'
        let profitRateFormulaLines: string[] = []
        if (!mask && sellOrderId && sellOrderItemId) {
          try {
            const lp = await salesOrderApi.getSellOrderItemLineProfit(sellOrderId, sellOrderItemId)
            const purchaseConvertPrice = Number(
              it.convertPrice ?? it.ConvertPrice ?? it.convert_price ?? NaN
            )
            const opts: PurchaseProfitOpts = {
              purchaseOrderItemId: itemId,
              purchaseQty: qty,
              purchaseConvertPrice: Number.isFinite(purchaseConvertPrice)
                ? purchaseConvertPrice
                : undefined
            }
            profitText = formatUsdProfitAmount(lp?.purchaseProfitExpected)
            profitFormulaLines = buildPurchaseProfitTip(lp, opts)
            const inputs = resolvePurchaseProfitInputs(lp, opts)
            const rate =
              inputs && inputs.purchaseUnit > 0 ? inputs.sellUnit / inputs.purchaseUnit : null
            profitRateText = formatProfitRateMultiplierDisplay(
              lp?.purchaseProfitExpected,
              rate,
              2
            )
            profitRateFormulaLines = buildPurchaseProfitRateTip(lp, opts)
          } catch {
            profitText = '—'
            profitRateText = '—'
          }
        }
        return {
            key: itemId || `po-${idx}`,
            itemCode,
            pn,
            brand,
            customerSo: '',
            dateCode: formatLineDateCode(it),
            deliveryDate: formatLineDeliveryDate(it),
            qty,
            unitPriceText: mask ? '—' : formatUnitPriceNumber(cost),
            currency: mask ? undefined : lineCurrency,
            currencyIso: mask || lineCurrency == null ? '' : listAmountCurrencyIso(lineCurrency),
            profitText: mask ? '—' : profitText,
            profitFormulaLines,
            profitRateText: mask ? '—' : profitRateText,
            profitRateFormulaLines
          }
      })
    )
  } catch {
    overview.value = emptyOverview()
    lines.value = []
  } finally {
    loading.value = false
  }
}

watch(
  () =>
    `${props.mode}:${props.orderId}:${Array.isArray(props.items) ? props.items.length : 0}:${maskSaleSensitiveFields.value}:${maskPurchaseSensitiveFields.value}`,
  () => {
    void loadPanel()
  },
  { immediate: true }
)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.approval-order-ref {
  min-height: 120px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-bottom: 12px;

  &__hint {
    margin: 8px 4px;
    color: $text-secondary;
    font-size: 13px;
    line-height: 1.5;
  }

  &__panel {
    border: 1px solid rgba(0, 212, 255, 0.16);
    border-radius: 10px;
    padding: 10px 12px;
    background: rgba(0, 212, 255, 0.03);
  }

  &__amount-num {
    font-weight: 700;
  }

  &__panel-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    margin-bottom: 8px;
  }

  &__panel--lines-collapsed &__panel-head {
    margin-bottom: 0;
  }

  &__panel-title {
    font-size: 13px;
    font-weight: 600;
    color: $text-primary;
    margin-bottom: 8px;

    .approval-order-ref__panel-head & {
      margin-bottom: 0;
    }
  }

  &__panel-count {
    margin-left: 0.4em;
    font-weight: 400;
  }

  &__fold-btn {
    flex-shrink: 0;
    padding: 0 4px;
    margin-right: -4px;

    .el-icon {
      font-size: 14px;
    }
  }

  &__kv {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }

  &__row {
    display: flex;
    gap: 10px;
    font-size: 12px;
    line-height: 1.5;

    .k {
      color: $text-muted;
      flex-shrink: 0;
      width: 6.5em;
    }

    .v {
      text-align: left;
      color: $text-primary;
      min-width: 0;
      flex: 1;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
  }

  &--cols-2 {
    .approval-order-ref__kv,
    .line-card {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 6px 24px;
    }

    .line-card__row--span {
      grid-column: 1 / -1;
    }
  }

  &__list {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }
}

.line-card {
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 8px;
  padding: 8px 10px;
  background: rgba(255, 255, 255, 0.35);

  &__row {
    display: flex;
    gap: 8px;
    font-size: 12px;
    line-height: 1.55;

    .k {
      color: $text-muted;
      flex-shrink: 0;
      width: 6.5em;
    }

    .v {
      text-align: left;
      color: $text-primary;
      min-width: 0;
      flex: 1;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
  }

  &__profit-v {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    overflow: visible;
    white-space: normal;
  }

  &__tip-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 0;
    width: 16px;
    height: 16px;
    border: none;
    background: transparent;
    color: $text-muted;
    cursor: pointer;
    flex-shrink: 0;

    &:hover {
      color: $cyan-primary;
    }

    .el-icon {
      font-size: 13px;
    }
  }

  &__tip-body {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  &__tip-title {
    margin: 0;
    font-size: 12px;
    font-weight: 600;
    color: $text-primary;
  }

  &__tip-list {
    margin: 0;
    padding-left: 16px;
    font-size: 12px;
    line-height: 1.55;
    color: $text-secondary;
    white-space: nowrap;

    li + li {
      margin-top: 6px;
    }
  }
}

.amount-with-code {
  display: inline-flex;
  align-items: baseline;
  gap: 4px;
}
</style>

<!-- teleported popover：保证公式单行完整显示 -->
<style lang="scss">
.approval-order-ref-profit-tip.el-popper {
  width: max-content !important;
  max-width: calc(100vw - 24px);
  box-sizing: border-box;

  .line-card__tip-list {
    margin: 0;
    padding-left: 16px;
    font-size: 12px;
    line-height: 1.55;
    white-space: nowrap;
  }

  .line-card__tip-title {
    margin: 0 0 8px;
    font-size: 12px;
    font-weight: 600;
  }
}
</style>
