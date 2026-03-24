<template>
  <div class="purchase-requisition-detail-page">
    <!-- 页面头部：参考 CustomerDetail.vue 的深色详情页布局 -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="router.push('/purchase-requisitions')">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          返回
        </button>

        <div class="customer-title-group">
          <h1 class="page-title">采购申请详情</h1>
          <div class="title-meta">
            <span class="customer-code">{{ data?.billCode || '—' }}</span>
          </div>
        </div>
      </div>
    </div>

    <div v-loading="loading" element-loading-background="rgba(10,22,40,0.8)" class="detail-content">
      <template v-if="data">
        <div class="info-section">
          <div class="section-header">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">基本信息</span>
          </div>

          <div class="info-grid">
            <div class="info-item">
              <span class="info-label">采购申请号</span>
              <span class="info-value info-value--code">{{ data.billCode }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">销售订单</span>
              <span class="info-value info-value--code">{{ data.sellOrderCode || data.sellOrderId }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">创建时间</span>
              <span class="info-value info-value--time">{{ data.createTime }}</span>
            </div>

            <div class="info-item">
              <span class="info-label">物料型号</span>
              <span class="info-value">{{ data.pn || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">品牌</span>
              <span class="info-value">{{ data.brand || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">采购员ID</span>
              <span class="info-value">{{ data.purchaseUserId || '—' }}</span>
            </div>

            <div class="info-item">
              <span class="info-label">申请数量</span>
              <span class="info-value">{{ data.qty ?? '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">预计采购日期</span>
              <span class="info-value info-value--time">{{ data.expectedPurchaseTime || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">状态</span>
              <span class="info-value">{{ getStatusText(data.status) }}</span>
            </div>

            <div class="info-item">
              <span class="info-label">类型</span>
              <span class="info-value">{{ getTypeText(data.type) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">备注</span>
              <span class="info-value">{{ data.remark || '—' }}</span>
            </div>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { purchaseRequisitionApi } from '@/api/purchaseRequisition'

const route = useRoute()
const router = useRouter()

const loading = ref(false)
const data = ref<any>(null)

function getStatusText(s: number) {
  const m: Record<number, string> = {
    0: '新建',
    1: '部分完成',
    2: '全部完成',
    3: '已取消'
  }
  return m[s] ?? String(s)
}

function getTypeText(t: number) {
  const m: Record<number, string> = {
    0: '专属',
    1: '公开备货'
  }
  return m[t] ?? String(t)
}

async function load() {
  const id = route.params.id as string
  if (!id) {
    ElMessage.error('参数错误：缺少 id')
    router.push('/purchase-requisitions')
    return
  }

  loading.value = true
  try {
    data.value = await purchaseRequisitionApi.getById(id)
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.message || e?.message || '加载失败')
    router.push('/purchase-requisitions')
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.purchase-requisition-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

// ---- 页面头部 ----
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.customer-title-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0;
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
}

.customer-code {
  font-family: 'Space Mono', monospace;
  font-size: 11px;
  color: $text-muted;
}

// ---- 基本信息 ----
.detail-content {
  min-height: 200px;
}

.info-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: rgba(0, 0, 0, 0.1);
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.6);
  }
}

.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-primary;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
  padding: 0;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 16px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
  border-right: 1px solid rgba(255, 255, 255, 0.04);

  &:nth-child(3n) {
    border-right: none;
  }

  .info-label {
    font-size: 11px;
    color: $text-muted;
    letter-spacing: 0.5px;
    text-transform: uppercase;
  }

  .info-value {
    font-size: 13px;
    color: $text-secondary;

    &--code {
      font-family: 'Space Mono', monospace;
      font-size: 12px;
      color: $color-ice-blue;
    }

    &--amount {
      font-family: 'Space Mono', monospace;
      font-size: 13px;
      color: $text-primary;
      font-weight: 500;
    }

    &--time {
      font-size: 12px;
      color: $text-muted;
    }
  }
}
</style>

