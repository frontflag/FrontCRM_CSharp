<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { orderApi, type OrderDetail } from '@/api/order'
import { showToast, formatDate, formatMoney } from '@/utils/index'

const orderType = ref<'sales' | 'purchase'>('sales')
const detail = ref<OrderDetail | null>(null)
const loading = ref(true)

onLoad((options: any) => {
  orderType.value = options?.type || 'sales'
  const id = options?.id
  if (id) {
    loadDetail(id)
  } else {
    showToast('缺少订单ID', 'error')
  }
})

async function loadDetail(id: string) {
  loading.value = true
  try {
    if (orderType.value === 'sales') {
      detail.value = await orderApi.getSalesOrderDetail(id)
    } else {
      detail.value = await orderApi.getPurchaseOrderDetail(id)
    }
  } catch (err: any) {
    showToast(err.message || '加载失败', 'error')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <view class="detail-page">
    <template v-if="loading">
      <view class="loading-state"><text>加载中...</text></view>
    </template>

    <template v-else-if="detail">
      <!-- 订单概要 -->
      <view class="info-card">
        <view class="card-title">订单信息</view>
        <view class="info-row">
          <text class="info-label">订单编号</text>
          <text class="info-value code-value">{{ detail.orderCode }}</text>
        </view>
        <view class="info-row">
          <text class="info-label">状态</text>
          <text class="info-value status-value">{{ detail.statusLabel }}</text>
        </view>
        <view class="info-row">
          <text class="info-label">公司</text>
          <text class="info-value">{{ detail.customerName || detail.vendorName || '—' }}</text>
        </view>
        <view class="info-row">
          <text class="info-label">总金额</text>
          <text class="info-value amount-value">
            {{ formatMoney(detail.totalAmount, detail.currency as any) }}
          </text>
        </view>
        <view class="info-row">
          <text class="info-label">创建时间</text>
          <text class="info-value">{{ formatDate(detail.createTime, 'YYYY-MM-DD HH:mm') }}</text>
        </view>
        <view class="info-row">
          <text class="info-label">创建人</text>
          <text class="info-value">{{ detail.createUser || '—' }}</text>
        </view>
      </view>

      <!-- 明细列表 -->
      <view class="info-card">
        <view class="card-title">订单明细 ({{ detail.items?.length || 0 }}项)</view>
        <view v-if="detail.items?.length > 0" class="item-list">
          <view v-for="(item, idx) in detail.items" :key="item.id || idx" class="item-row">
            <view class="item-info">
              <text class="item-material">{{ item.materialName || item.itemCode }}</text>
              <text class="item-model">{{ item.materialModel || '' }}</text>
            </view>
            <view class="item-meta">
              <text class="item-qty">×{{ item.quantity }}</text>
              <text class="item-price">{{ formatMoney(item.totalPrice, detail!.currency as any) }}</text>
            </view>
          </view>
        </view>
        <view v-else class="empty-items">
          <text>暂无明细数据</text>
        </view>
      </view>

      <!-- 备注 -->
      <view v-if="detail.remark" class="info-card">
        <view class="card-title">备注</view>
        <text class="remark-text">{{ detail.remark }}</text>
      </view>
    </template>
  </view>
</template>

<style lang="scss" scoped>
.detail-page {
  min-height: 100vh;
  background: #f5f5f5;
  padding-bottom: 48rpx;
}

.loading-state {
  text-align: center;
  padding: 120rpx 0;
  font-size: 28rpx;
  color: #999;
}

.info-card {
  background: #fff;
  margin: 16rpx 24rpx;
  padding: 24rpx;
  border-radius: 12rpx;
}

.card-title {
  font-size: 30rpx;
  font-weight: 600;
  color: #333;
  margin-bottom: 20rpx;
  padding-bottom: 16rpx;
  border-bottom: 1rpx solid #f0f0f0;
}

.info-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: 16rpx 0;

  &:not(:last-child) {
    border-bottom: 1rpx solid #fafafa;
  }
}

.info-label {
  font-size: 26rpx;
  color: #999;
  flex-shrink: 0;
  width: 160rpx;
}

.info-value {
  font-size: 26rpx;
  color: #333;
  text-align: right;
  flex: 1;
  word-break: break-all;
}

.code-value {
  color: #1677ff;
  font-weight: 500;
}

.status-value {
  color: #1677ff;
  background: #e6f4ff;
  padding: 4rpx 12rpx;
  border-radius: 6rpx;
  display: inline-block;
}

.amount-value {
  font-weight: 600;
  color: #ff4d4f;
}

.item-list {
  margin-top: 8rpx;
}

.item-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: 16rpx 0;

  &:not(:last-child) {
    border-bottom: 1rpx solid #fafafa;
  }
}

.item-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 4rpx;
}

.item-material {
  font-size: 26rpx;
  color: #333;
}

.item-model {
  font-size: 22rpx;
  color: #999;
}

.item-meta {
  text-align: right;
  display: flex;
  flex-direction: column;
  gap: 4rpx;
  margin-left: 16rpx;
}

.item-qty {
  font-size: 24rpx;
  color: #666;
}

.item-price {
  font-size: 26rpx;
  font-weight: 500;
  color: #ff4d4f;
}

.empty-items {
  text-align: center;
  padding: 32rpx 0;
  font-size: 24rpx;
  color: #999;
}

.remark-text {
  font-size: 26rpx;
  color: #666;
  line-height: 1.6;
}
</style>
