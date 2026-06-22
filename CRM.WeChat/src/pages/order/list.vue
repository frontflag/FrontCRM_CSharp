<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { orderApi, type SalesOrderListItem, type PurchaseOrderListItem } from '@/api/order'
import { showToast, formatDate, formatMoney } from '@/utils/index'

const tabIndex = ref(0)
const tabs = ['销售订单', '采购订单']

const salesOrders = ref<SalesOrderListItem[]>([])
const purchaseOrders = ref<PurchaseOrderListItem[]>([])
const loading = ref(false)

onMounted(() => {
  loadSalesOrders()
})

async function loadSalesOrders() {
  loading.value = true
  try {
    const res = await orderApi.getSalesOrders({ page: 1, pageSize: 50 })
    salesOrders.value = res.items || []
  } catch (err: any) {
    showToast(err.message || '加载失败', 'error')
  } finally {
    loading.value = false
  }
}

async function loadPurchaseOrders() {
  loading.value = true
  try {
    const res = await orderApi.getPurchaseOrders({ page: 1, pageSize: 50 })
    purchaseOrders.value = res.items || []
  } catch (err: any) {
    showToast(err.message || '加载失败', 'error')
  } finally {
    loading.value = false
  }
}

function handleTabChange(index: number) {
  tabIndex.value = index
  if (index === 0 && salesOrders.value.length === 0) loadSalesOrders()
  if (index === 1 && purchaseOrders.value.length === 0) loadPurchaseOrders()
}

function goDetail(type: 'sales' | 'purchase', id: string) {
  uni.navigateTo({ url: `/pages/order/detail?type=${type}&id=${id}` })
}

const currentList = () => (tabIndex.value === 0 ? salesOrders.value : purchaseOrders.value)
</script>

<template>
  <view class="order-page">
    <!-- Tab 切换 -->
    <view class="tabs">
      <view
        v-for="(tab, index) in tabs"
        :key="tab"
        class="tab-item"
        :class="{ active: tabIndex === index }"
        @click="handleTabChange(index)"
      >
        <text>{{ tab }}</text>
      </view>
    </view>

    <!-- 订单列表 -->
    <view v-if="currentList().length > 0" class="order-list">
      <view
        v-for="item in currentList()"
        :key="item.id"
        class="order-item"
        @click="goDetail(tabIndex === 0 ? 'sales' : 'purchase', item.id)"
      >
        <view class="order-header">
          <text class="order-code">{{ item.orderCode }}</text>
          <text class="order-status">{{ (item as any).statusLabel || item.status }}</text>
        </view>
        <view class="order-body">
          <text class="order-company">
            {{ tabIndex === 0 ? (item as SalesOrderListItem).customerName : (item as PurchaseOrderListItem).vendorName }}
          </text>
          <text class="order-amount">{{ formatMoney(item.totalAmount, item.currency) }}</text>
        </view>
        <view class="order-footer">
          <text class="order-user">
            {{ tabIndex === 0 ? (item as SalesOrderListItem).salesUserName : (item as PurchaseOrderListItem).purchaseUserName || '—' }}
          </text>
          <text class="order-time">{{ formatDate(item.createTime) }}</text>
        </view>
      </view>
    </view>

    <!-- 空状态 -->
    <view v-else-if="!loading" class="empty-state">
      <text class="empty-icon">📋</text>
      <text class="empty-text">暂无订单数据</text>
    </view>
  </view>
</template>

<style lang="scss" scoped>
.order-page {
  min-height: 100vh;
  background: #f5f5f5;
}

.tabs {
  display: flex;
  background: #fff;
  padding: 0 24rpx;
  border-bottom: 1rpx solid #f0f0f0;
}

.tab-item {
  flex: 1;
  text-align: center;
  padding: 24rpx 0;
  font-size: 28rpx;
  color: #666;
  position: relative;

  &.active {
    color: #1677ff;
    font-weight: 500;

    &::after {
      content: '';
      position: absolute;
      bottom: 0;
      left: 50%;
      transform: translateX(-50%);
      width: 48rpx;
      height: 4rpx;
      background: #1677ff;
      border-radius: 2rpx;
    }
  }
}

.order-list {
  padding: 16rpx 24rpx;
}

.order-item {
  background: #fff;
  padding: 24rpx;
  border-radius: 12rpx;
  margin-bottom: 16rpx;
}

.order-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12rpx;
}

.order-code {
  font-size: 30rpx;
  font-weight: 500;
  color: #333;
}

.order-status {
  font-size: 22rpx;
  color: #1677ff;
  background: #e6f4ff;
  padding: 4rpx 16rpx;
  border-radius: 8rpx;
}

.order-body {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8rpx;
}

.order-company {
  font-size: 26rpx;
  color: #666;
}

.order-amount {
  font-size: 28rpx;
  font-weight: 600;
  color: #ff4d4f;
}

.order-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.order-user,
.order-time {
  font-size: 22rpx;
  color: #999;
}

.empty-state {
  text-align: center;
  padding: 120rpx 0;
}

.empty-icon {
  font-size: 80rpx;
  display: block;
  margin-bottom: 24rpx;
}

.empty-text {
  font-size: 28rpx;
  color: #999;
}
</style>
