<template>
  <div class="so-journey-panel">
    <div class="panel-header">
      <div class="title">订单旅程</div>
      <div class="sub">
        <span v-if="salesOrderId">销售订单ID：{{ salesOrderId }}</span>
        <span v-else>未检测到销售订单ID（详情页路由或查询参数）</span>
        <span class="sub-debug">（route: {{ String(route.name || '-') }}）</span>
      </div>
    </div>

    <el-empty
      v-if="!salesOrderId"
      description="暂无数据"
      :image-size="80"
    />

    <!-- 加载时不能用 v-if 换走图表容器，否则 G6 绑定的 DOM 被销毁且实例未重建，会一直空白 -->
    <div v-else class="panel-main">
      <div v-if="loading" class="loading-mask">
        <el-skeleton :rows="6" animated />
      </div>
      <div ref="containerEl" class="graph-container" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Graph } from '@antv/g6'
import salesOrderApi from '@/api/salesOrder'
import { getApiErrorMessage } from '@/utils/apiError'

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

/** 兼容解包后仍带一层 data、或 PascalCase 字段 */
function normalizeJourneyResponse(raw: unknown): JourneyResponse {
  const r = raw as Record<string, unknown> | null | undefined
  const base = (r?.data ?? r) as Record<string, unknown> | null | undefined
  const nodes = (base?.nodes ?? base?.Nodes) as JourneyNode[] | undefined
  const edges = (base?.edges ?? base?.Edges) as JourneyEdge[] | undefined
  return { nodes: nodes ?? [], edges: edges ?? [] }
}

const route = useRoute()
const containerEl = ref<HTMLElement | null>(null)
const graph = ref<Graph | null>(null)
const ro = ref<ResizeObserver | null>(null)
const loading = ref(false)

const salesOrderId = computed(() => {
  const p = route.params as any
  const q = route.query as any
  const id =
    (p?.id as string | undefined) ||
    (q?.sellOrderId as string | undefined) ||
    (q?.salesOrderId as string | undefined) ||
    (q?.id as string | undefined)
  return String(id || '').trim()
})

function pickDirection(w: number, h: number) {
  return w >= h ? 'LR' : 'TB'
}

function buildGraphData(j: JourneyResponse) {
  const nodes = (j.nodes || []).map((n) => {
    const isCurrent = Boolean(n.isCurrent)
    const labelParts = [n.title, n.statusText, n.createDate, n.creatorName].filter((x) => x != null && String(x).trim() !== '')
    return {
      id: n.id,
      data: n,
      style: {
        labelText: labelParts.join('\n'),
        radius: 10,
        lineWidth: isCurrent ? 2 : 1,
        fill: isCurrent ? 'rgba(0, 212, 255, 0.16)' : 'rgba(255, 255, 255, 0.04)',
        stroke: isCurrent ? '#00d4ff' : 'rgba(0, 212, 255, 0.25)'
      }
    }
  })

  const edges = (j.edges || []).map((e) => ({
    id: e.id,
    source: e.source,
    target: e.target,
    style: {
      stroke: 'rgba(0, 212, 255, 0.35)',
      lineWidth: 1.2,
      endArrow: true
    }
  }))

  return { nodes, edges }
}

function destroyGraph() {
  ro.value?.disconnect()
  ro.value = null
  graph.value?.destroy()
  graph.value = null
}

function ensureGraph() {
  const el = containerEl.value
  if (!el) return

  if (graph.value) return

  graph.value = new Graph({
    container: el,
    autoFit: 'view',
    node: {
      type: 'rect',
      style: {
        size: [220, 86],
        radius: 10,
        labelFill: 'rgba(255, 255, 255, 0.92)',
        labelFontSize: 12,
        labelLineHeight: 16,
        labelTextAlign: 'left',
        labelTextBaseline: 'top',
        labelPadding: [10, 10, 10, 10]
      }
    },
    edge: {
      type: 'polyline'
    },
    layout: {
      type: 'dagre',
      rankdir: pickDirection(el.clientWidth, el.clientHeight),
      nodesep: 24,
      ranksep: 34
    },
    behaviors: ['drag-canvas', 'zoom-canvas']
  })

  ro.value = new ResizeObserver(() => {
    const g = graph.value
    const host = containerEl.value
    if (!g || !host) return
    g.setLayout({
      type: 'dagre',
      rankdir: pickDirection(host.clientWidth, host.clientHeight),
      nodesep: 24,
      ranksep: 34
    })
    // 重新布局 + 适配视图
    g.render()
    g.fitView()
  })
  ro.value.observe(el)
}

async function loadJourney() {
  if (!salesOrderId.value) {
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

    const raw = await (salesOrderApi as any).getJourney(salesOrderId.value)
    const j = normalizeJourneyResponse(raw)
    const data = buildGraphData(j)
    g.setData(data as any)
    await g.render()
    g.fitView()
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, '加载订单旅程失败'))
  } finally {
    loading.value = false
  }
}

watch(
  () => [route.name, salesOrderId.value] as const,
  () => {
    void loadJourney()
  },
  { immediate: true }
)

onBeforeUnmount(() => {
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
}
</style>

