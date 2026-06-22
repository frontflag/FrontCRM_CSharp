<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { inventoryApi } from '@/api/inventory'
import { formatDate } from '@/utils/index'

const authStore = useAuthStore()
const stats = ref({
  customerCount: 0,
  orderCount: 0,
  inventoryCount: 0,
})
const recentPickings = ref<any[]>([])

onMounted(async () => {
  try {
    const pickingRes = await inventoryApi.getPickingList({ page: 1, pageSize: 5 })
    recentPickings.value = pickingRes.items || []
  } catch {
    // 忽略加载错误
  }
})
</script>

<template>
  <view class="home-page">
    <!-- 头部欢迎 -->
    <view class="home-header">
      <view class="welcome-text">
        <text class="welcome-greeting">你好，</text>
        <text class="welcome-name">{{ authStore.displayName }}</text>
      </view>
      <text class="welcome-date">{{ formatDate(new Date().toISOString()) }}</text>
    </view>

    <!-- 快捷入口 -->
    <view class="quick-actions">
      <view class="action-item" @click="uni.switchTab({ url: '/pages/customer/list' })">
        <view class="action-icon action-icon-blue">👥</view>
        <text class="action-text">客户管理</text>
      </view>
      <view class="action-item" @click="uni.switchTab({ url: '/pages/order/list' })">
        <view class="action-icon action-icon-orange">📋</view>
        <text class="action-text">订单管理</text>
      </view>
      <view class="action-item" @click="uni.switchTab({ url: '/pages/inventory/list' })">
        <view class="action-icon action-icon-green">📦</view>
        <text class="action-text">库存查询</text>
      </view>
      <view class="action-item" @click="uni.switchTab({ url: '/pages/mine/mine' })">
        <view class="action-icon action-icon-purple">👤</view>
        <text class="action-text">个人中心</text>
      </view>
    </view>

    <!-- 最近拣货单 -->
    <view class="section">
      <view class="section-header">
        <text class="section-title">最近拣货单</text>
        <text class="section-more" @click="uni.switchTab({ url: '/pages/inventory/list' })">
          查看全部 ›
        </text>
      </view>
      <view v-if="recentPickings.length > 0" class="picking-list">
        <view
          v-for="item in recentPickings"
          :key="item.id"
          class="picking-item"
        >
          <view class="picking-info">
            <text class="picking-code">{{ item.taskCode }}</text>
            <text class="picking-customer">{{ item.customerName || '—' }}</text>
          </view>
          <text class="picking-qty">{{ item.planQtyTotal || 0 }}件</text>
        </view>
      </view>
      <view v-else class="empty-state">
        <text class="empty-text">暂无数据</text>
      </view>
    </view>
  </view>
</template>

<style lang="scss" scoped>
.home-page {
  min-height: 100vh;
  background: #f5f5f5;
}

.home-header {
  background: linear-gradient(135deg, #1677ff 0%, #4096ff 100%);
  padding: 48rpx 32rpx 64rpx;
  color: #fff;
}

.welcome-text {
  font-size: 36rpx;
  margin-bottom: 8rpx;
}

.welcome-name {
  font-weight: bold;
}

.welcome-date {
  font-size: 24rpx;
  opacity: 0.8;
}

.quick-actions {
  display: flex;
  justify-content: space-around;
  background: #fff;
  margin: -32rpx 24rpx 24rpx;
  padding: 32rpx 16rpx;
  border-radius: 16rpx;
  box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, 0.06);
}

.action-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12rpx;
}

.action-icon {
  width: 88rpx;
  height: 88rpx;
  border-radius: 24rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 40rpx;
}

.action-icon-blue { background: #e6f4ff; }
.action-icon-orange { background: #fff7e6; }
.action-icon-green { background: #f6ffed; }
.action-icon-purple { background: #f9f0ff; }

.action-text {
  font-size: 24rpx;
  color: #333;
}

.section {
  background: #fff;
  margin: 0 24rpx 24rpx;
  border-radius: 16rpx;
  padding: 24rpx;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24rpx;
}

.section-title {
  font-size: 30rpx;
  font-weight: 600;
  color: #333;
}

.section-more {
  font-size: 24rpx;
  color: #1677ff;
}

.picking-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20rpx 0;
  border-bottom: 1rpx solid #f0f0f0;

  &:last-child {
    border-bottom: none;
  }
}

.picking-info {
  display: flex;
  flex-direction: column;
  gap: 4rpx;
}

.picking-code {
  font-size: 28rpx;
  color: #333;
  font-weight: 500;
}

.picking-customer {
  font-size: 24rpx;
  color: #999;
}

.picking-qty {
  font-size: 26rpx;
  color: #1677ff;
}

.empty-state {
  text-align: center;
  padding: 48rpx 0;
}

.empty-text {
  font-size: 26rpx;
  color: #999;
}
</style>
