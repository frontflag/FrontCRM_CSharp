<template>
  <div class="so-journey-panel">
    <div class="panel-header">
      <div class="title">订单旅程</div>
      <div class="sub">
        <template v-if="salesOrderId">
          <span class="sub-main">{{ journeyRootLabel || `销售订单 · ${salesOrderSummary}` }}</span>
          <span class="sub-zoom-hint" title="按住 Ctrl 并滚动鼠标滚轮">Ctrl + 滚轮 缩放</span>
        </template>
        <span v-else>{{ emptyHint }}</span>
      </div>
    </div>

    <el-empty v-if="!salesOrderId" description="暂无数据" :image-size="80" />

    <div v-else class="panel-main">
      <div v-if="loading" class="loading-mask">
        <el-skeleton :rows="6" animated />
      </div>
      <div ref="containerEl" class="graph-container">
        <!-- 节点操作菜单（点击节点弹出） -->
        <div
          v-if="menuVisible"
          ref="menuEl"
          class="journey-node-menu"
          :style="{ left: menuPos.x + 'px', top: menuPos.y + 'px' }"
          @click.stop
        >
          <div class="journey-node-menu__title" :title="menuNode?.title || ''">
            {{ menuNode?.title || '节点' }}
          </div>
          <div class="journey-node-menu__divider" />
          <button type="button" class="journey-node-menu__item" @click="handleFocusNode">
            聚焦到该节点
          </button>
          <button type="button" class="journey-node-menu__item" @click="handleCopyNodeId">
            复制节点ID
          </button>
          <button
            type="button"
            class="journey-node-menu__item journey-node-menu__item--primary"
            :disabled="!menuGoToDetail"
            @click="handleGoToDetail"
          >
            跳转到详情
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Graph } from '@antv/g6'
import salesOrderApi from '@/api/salesOrder'
import { getApiErrorMessage } from '@/utils/apiError'
import { journeyListFocusedOrderCode, journeyListFocusedOrderId } from '@/composables/salesOrderJourneyContext'

type JourneyNode = {
  id: string
  type: string
  title: string
  statusText?: string
  createDate?: string
  creatorName?: string
  amount?: number | null
  quantity?: number | null
  isCurrent?: boolean
}

type JourneyEdge = {
  id: string
  source: string
  target: string
}

type JourneyResponse = {
  nodes: JourneyNode[]
  edges: JourneyEdge[]
}

function normalizeJourneyResponse(raw: unknown): JourneyResponse {
  const r = raw as Record<string, unknown> | null | undefined
  const base = (r?.data ?? r) as Record<string, unknown> | null | undefined
  const nodes = (base?.nodes ?? base?.Nodes) as JourneyNode[] | undefined
  const edges = (base?.edges ?? base?.Edges) as JourneyEdge[] | undefined
  return { nodes: nodes ?? [], edges: edges ?? [] }
}

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}

/** 财务链路节点：暖色底（参照截图「付款」节点） */
function isFinanceLane(type: string): boolean {
  const t = (type || '').toUpperCase()
  return (
    t === 'FINANCE_PAYMENT' ||
    t === 'FINANCE_RECEIPT' ||
    t === 'PURCHASE_INVOICE' ||
    t === 'SELL_INVOICE'
  )
}

const PIN_SVG = `<svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" viewBox="0 0 24 24" aria-hidden="true" style="flex-shrink:0;margin-top:1px;color:#38bdf8"><path fill="currentColor" d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5A2.5 2.5 0 1 1 12 6a2.5 2.5 0 0 1 0 5.5z"/></svg>`

function buildJourneyNodeInnerHTML(n: JourneyNode): string {
  const title = escapeHtml(String(n.title || '—').trim())
  const date = escapeHtml(String(n.createDate || '—').trim())
  const current = Boolean(n.isCurrent)
  const finance = isFinanceLane(n.type)

  const bg = finance
    ? 'linear-gradient(180deg, rgba(254, 215, 170, 0.22) 0%, rgba(251, 146, 60, 0.12) 100%)'
    : 'linear-gradient(180deg, rgba(55, 65, 81, 0.92) 0%, rgba(41, 49, 63, 0.88) 100%)'
  const border = finance ? '1px solid rgba(251, 146, 60, 0.45)' : '1px solid rgba(148, 163, 184, 0.35)'
  const shadow = current ? '0 0 0 1px rgba(56, 189, 248, 0.55), 0 4px 14px rgba(0,0,0,0.25)' : '0 2px 8px rgba(0,0,0,0.2)'

  const pinHtml = current
    ? `<span style="display:flex;align-items:flex-start;" title="您在此">${PIN_SVG}</span>`
    : `<span style="width:15px;flex-shrink:0"></span>`

  return `
<div style="box-sizing:border-box;width:100%;height:100%;padding:10px 12px;border-radius:10px;border:${border};background:${bg};box-shadow:${shadow};font-family:system-ui,-apple-system,'Segoe UI',Roboto,'Noto Sans SC',sans-serif;">
  <div style="display:flex;align-items:flex-start;gap:8px;">
    ${pinHtml}
    <div style="flex:1;min-width:0;">
      <div style="color:#7dd3fc;font-size:13px;font-weight:600;line-height:1.35;letter-spacing:0.02em;">${title}</div>
      <div style="color:rgba(186, 200, 216, 0.88);font-size:12px;margin-top:5px;font-variant-numeric:tabular-nums;">${date}</div>
      ${
        current
          ? `<div style="margin-top:6px;font-size:10px;font-weight:600;color:rgba(56,189,248,0.95);letter-spacing:0.06em;">您在此</div>`
          : ''
      }
    </div>
  </div>
</div>`.trim()
}

function buildGraphData(j: JourneyResponse) {
  const NODE_W = 220
  const NODE_H = 88

  const nodes = (j.nodes || []).map((n) => ({
    id: n.id,
    type: 'html' as const,
    data: n as unknown as Record<string, unknown>,
    style: {
      size: [NODE_W, NODE_H],
      innerHTML: buildJourneyNodeInnerHTML(n)
    }
  }))

  const edges = (j.edges || []).map((e) => ({
    id: e.id,
    source: e.source,
    target: e.target,
    style: {
      stroke: 'rgba(148, 163, 184, 0.55)',
      lineWidth: 1.25,
      radius: 6,
      endArrow: true
    }
  }))

  return { nodes, edges }
}

function layoutOpts(rankdir: 'LR' | 'TB') {
  return {
    type: 'dagre',
    rankdir,
    nodesep: rankdir === 'LR' ? 32 : 28,
    ranksep: rankdir === 'LR' ? 44 : 40
  }
}

const route = useRoute()
const router = useRouter()
const containerEl = ref<HTMLElement | null>(null)
const graph = ref<Graph | null>(null)
const ro = ref<ResizeObserver | null>(null)
const loading = ref(false)
const journeyRootLabel = ref('')

function parseBizId(compositeNodeId: string): string {
  // 节点 ID 格式：TYPE:bizId（见后端 SalesOrderJourneyService）
  const s = String(compositeNodeId || '')
  const idx = s.indexOf(':')
  if (idx < 0) return s
  return s.slice(idx + 1)
}

// ========== 节点菜单状态 ==========
const menuVisible = ref(false)
const menuPos = ref({ x: 0, y: 0 })
const menuNode = ref<JourneyNode | null>(null)
const menuEl = ref<HTMLElement | null>(null)
let docClickHandler: ((e: MouseEvent) => void) | null = null

const menuGoToDetail = computed(() => {
  if (!menuNode.value) return null
  const t = (menuNode.value.type || '').toUpperCase()
  switch (t) {
    case 'SALES_ORDER':
    case 'QUOTE':
    case 'RFQ':
    case 'PURCHASE_REQUISITION':
    case 'PURCHASE_ORDER':
    case 'FINANCE_PAYMENT':
    case 'FINANCE_RECEIPT':
    case 'PURCHASE_INVOICE':
    case 'SELL_INVOICE':
      return true
    default:
      return null
  }
})

function closeMenu() {
  menuVisible.value = false
  menuNode.value = null
}

function showMenuAtNode(evt: any, model: any) {
  const host = containerEl.value
  if (!host) return
  const domEvent = evt?.domEvent as MouseEvent | undefined
  const rect = host.getBoundingClientRect()
  const clientX = domEvent?.clientX ?? rect.left
  const clientY = domEvent?.clientY ?? rect.top

  // 节点菜单尺寸大致估计，用于边界裁剪
  const MENU_W = 220
  const MENU_H = 168
  const left = Math.max(6, Math.min(rect.width - MENU_W - 6, clientX - rect.left))
  const top = Math.max(6, Math.min(rect.height - MENU_H - 6, clientY - rect.top))

  menuPos.value = { x: left, y: top }
  menuNode.value = (model?.data ?? null) as JourneyNode | null
  menuVisible.value = true
}

function getGoToDetailRoute(node: JourneyNode | null): { name: string } | null {
  if (!node) return null
  const t = (node.type || '').toUpperCase()
  switch (t) {
    case 'SALES_ORDER':
      return { name: 'SalesOrderDetail' }
    case 'QUOTE':
      return { name: 'QuoteDetail' }
    case 'RFQ':
      return { name: 'RFQDetail' }
    case 'PURCHASE_REQUISITION':
      return { name: 'PurchaseRequisitionDetail' }
    case 'PURCHASE_ORDER':
      return { name: 'PurchaseOrderDetail' }
    case 'FINANCE_PAYMENT':
      return { name: 'FinancePaymentDetail' }
    case 'FINANCE_RECEIPT':
      return { name: 'FinanceReceiptDetail' }
    case 'PURCHASE_INVOICE':
      return { name: 'FinancePurchaseInvoiceDetail' }
    case 'SELL_INVOICE':
      return { name: 'FinanceSellInvoiceDetail' }
    default:
      return null
  }
}

async function handleCopyNodeId() {
  if (!menuNode.value?.id) return
  const text = String(menuNode.value.id)
  try {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(text)
      ElMessage.success('已复制节点ID')
    } else {
      throw new Error('no clipboard')
    }
  } catch {
    ElMessage.warning('复制失败：浏览器可能限制剪贴板权限')
  } finally {
    closeMenu()
  }
}

function handleFocusNode() {
  const g = graph.value
  if (!g || !menuNode.value?.id) return
  closeMenu()
  void g.focusElement(menuNode.value.id, { duration: 180 })
}

function handleGoToDetail() {
  if (!menuNode.value) return
  const routeDef = getGoToDetailRoute(menuNode.value)
  if (!routeDef) return
  const bizId = parseBizId(menuNode.value.id)
  closeMenu()
  router.push({ name: routeDef.name as any, params: { id: bizId } })
}

const routeOrderId = computed(() => {
  const p = route.params as Record<string, unknown>
  const q = route.query as Record<string, unknown>
  const id =
    (p?.id as string | undefined) ||
    (q?.sellOrderId as string | undefined) ||
    (q?.salesOrderId as string | undefined) ||
    (q?.id as string | undefined)
  return String(id || '').trim()
})

/** 详情/带 query 用路由 id；销售订单列表页用当前列表高亮行 */
const salesOrderId = computed(() => {
  const rid = routeOrderId.value
  if (rid) return rid
  return journeyListFocusedOrderId.value.trim()
})

const salesOrderSummary = computed(() => {
  if (routeOrderId.value) return routeOrderId.value
  const code = journeyListFocusedOrderCode.value.trim()
  if (code) return code
  return salesOrderId.value
})

const emptyHint = computed(() => {
  if (route.name === 'SalesOrderList') {
    return '当前页暂无选中订单：加载列表后将默认使用当前页第一行；也可单击表格一行查看该单旅程'
  }
  return '未检测到销售订单（请打开销售订单详情，或先在订单列表中选择一行）'
})

function pickDirection(w: number, h: number): 'LR' | 'TB' {
  return w >= h ? 'LR' : 'TB'
}

function destroyGraph() {
  ro.value?.disconnect()
  ro.value = null
  graph.value?.destroy()
  graph.value = null
}

function ensureGraph() {
  const el = containerEl.value
  if (!el || graph.value) return

  const rankdir = pickDirection(el.clientWidth || 800, el.clientHeight || 400)

  graph.value = new Graph({
    container: el,
    autoFit: 'view',
    padding: 20,
    background: 'transparent',
    node: {
      type: 'html',
      style: {
        pointerEvents: 'auto'
      }
    },
    edge: {
      type: 'polyline',
      style: {
        stroke: 'rgba(148, 163, 184, 0.55)',
        lineWidth: 1.25,
        radius: 6,
        endArrow: true
      }
    },
    layout: layoutOpts(rankdir),
    behaviors: [
      'drag-canvas',
      {
        type: 'zoom-canvas',
        /** 仅按住 Ctrl 时滚轮缩放（与常见设计软件一致，避免误触） */
        trigger: ['Control'],
        sensitivity: 1.15,
        animation: { duration: 160 }
      }
    ],
    plugins: []
  })

  ro.value = new ResizeObserver(() => {
    const g = graph.value
    const host = containerEl.value
    if (!g || !host) return
    const rd = pickDirection(host.clientWidth, host.clientHeight)
    // 容器尺寸变化时，显式刷新画布尺寸，避免出现“重新布局但视口裁切不随容器变化”的情况
    const w = host.clientWidth
    const h = host.clientHeight
    if (w > 0 && h > 0) g.resize(w, h)
    g.setLayout(layoutOpts(rd))
    g.render()
    // always：容器变化时也要保持“全部可见/尽量铺满”
    void g.fitView({ when: 'always', direction: 'both' })
  })
  ro.value.observe(el)

  // 点击节点弹出菜单
  graph.value.on('node:click', (evt: any) => {
    const item = evt?.item
    if (!item) return
    const model = item.getModel?.()
    if (!model) return
    showMenuAtNode(evt, model)
  })

  // 点击画布其它区域关闭菜单
  graph.value.on('canvas:click', () => {
    closeMenu()
  })

  // 点击菜单外关闭（防止画布事件吞掉）
  if (docClickHandler) document.removeEventListener('click', docClickHandler, true)
  docClickHandler = (e: MouseEvent) => {
    if (!menuVisible.value) return
    const target = e.target as Node | null
    if (!target) return
    if (menuEl.value?.contains(target)) return
    // 点击发生在图表画布区域时，不关闭菜单（避免同一次 node:click 后立刻被关闭）
    if (containerEl.value?.contains(target)) return
    closeMenu()
  }
  document.addEventListener('click', docClickHandler, true)
}

async function loadJourney() {
  if (!salesOrderId.value) {
    journeyRootLabel.value = ''
    destroyGraph()
    loading.value = false
    return
  }

  loading.value = true
  try {
    await nextTick()
    ensureGraph()
    const g = graph.value
    if (!g) return

    const raw = await (salesOrderApi as { getJourney: (id: string) => Promise<unknown> }).getJourney(
      salesOrderId.value
    )
    const j = normalizeJourneyResponse(raw)
    const so = j.nodes.find((x) => (x.type || '').toUpperCase() === 'SALES_ORDER')
    journeyRootLabel.value = so?.title?.trim() || ''

    const data = buildGraphData(j)
    g.setData(data as any)
    await g.render()
    g.fitView()
  } catch (e) {
    console.error(e)
    journeyRootLabel.value = ''
    ElMessage.error(getApiErrorMessage(e, '加载订单旅程失败'))
  } finally {
    loading.value = false
  }
}

watch(
  () => [route.name, salesOrderId.value, journeyListFocusedOrderId.value] as const,
  () => {
    void loadJourney()
  },
  { immediate: true }
)

onBeforeUnmount(() => {
  if (docClickHandler) document.removeEventListener('click', docClickHandler, true)
  docClickHandler = null
  destroyGraph()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.so-journey-panel {
  height: 100%;
  min-height: 260px;
  display: flex;
  flex-direction: column;
  background: $layer-1;
}

.panel-header {
  padding: 12px 12px 8px;
  border-bottom: 1px solid rgba(0, 212, 255, 0.12);
}

.title {
  font-size: 14px;
  font-weight: 700;
  color: $text-primary;
}

.sub {
  margin-top: 4px;
  font-size: 12px;
  color: $text-muted;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 12px;
}

.sub-main {
  color: rgba(200, 220, 240, 0.88);
}

.sub-zoom-hint {
  font-size: 11px;
  color: rgba(148, 170, 195, 0.75);
  white-space: nowrap;
}

.panel-main {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  position: relative;
}

.loading-mask {
  position: absolute;
  inset: 0;
  z-index: 2;
  padding: 12px;
  background: rgba(10, 18, 28, 0.72);
  backdrop-filter: blur(2px);
  pointer-events: none;
}

.graph-container {
  flex: 1;
  min-height: 220px;
  border-radius: 8px;
  overflow: hidden;
  position: relative;
  width: 100%;
  height: 100%;
}

.journey-node-menu {
  position: absolute;
  z-index: 30;
  width: 220px;
  background: rgba(12, 20, 32, 0.92);
  border: 1px solid rgba(0, 212, 255, 0.22);
  border-radius: 12px;
  box-shadow: 0 18px 50px rgba(0, 0, 0, 0.42);
  padding: 10px 10px 8px;
  backdrop-filter: blur(6px);
}

.journey-node-menu__title {
  padding: 6px 8px 8px;
  font-size: 12px;
  font-weight: 700;
  color: rgba(200, 220, 240, 0.95);
  max-width: 200px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.journey-node-menu__divider {
  height: 1px;
  background: rgba(255, 255, 255, 0.08);
  margin: 6px 0 8px;
}

.journey-node-menu__item {
  width: 100%;
  text-align: left;
  padding: 8px 10px;
  border: none;
  border-radius: 8px;
  background: transparent;
  cursor: pointer;
  color: rgba(200, 220, 240, 0.9);
  font-size: 13px;
}

.journey-node-menu__item:hover {
  background: rgba(0, 212, 255, 0.12);
  color: #e8f4ff;
}

.journey-node-menu__item--primary {
  color: rgba(125, 211, 252, 0.95);
}

.journey-node-menu__item[disabled] {
  opacity: 0.45;
  cursor: not-allowed;
}
</style>
