<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { showConfirm } from '@/utils/index'

const authStore = useAuthStore()

async function handleLogout() {
  const confirmed = await showConfirm('确定要退出登录吗？')
  if (confirmed) {
    await authStore.logout()
  }
}
</script>

<template>
  <view class="mine-page">
    <!-- 用户信息 -->
    <view class="user-header">
      <view class="user-avatar">
        <text class="avatar-text">{{ authStore.displayName?.charAt(0) || 'U' }}</text>
      </view>
      <text class="user-name">{{ authStore.displayName }}</text>
      <text class="user-id">ID: {{ authStore.userInfo?.userId || '—' }}</text>
    </view>

    <!-- 菜单列表 -->
    <view class="menu-group">
      <view class="menu-item">
        <text class="menu-icon">📊</text>
        <text class="menu-text">我的客户</text>
        <text class="menu-arrow">›</text>
      </view>
      <view class="menu-item">
        <text class="menu-icon">📋</text>
        <text class="menu-text">我的订单</text>
        <text class="menu-arrow">›</text>
      </view>
    </view>

    <view class="menu-group">
      <view class="menu-item">
        <text class="menu-icon">⚙️</text>
        <text class="menu-text">设置</text>
        <text class="menu-arrow">›</text>
      </view>
      <view class="menu-item">
        <text class="menu-icon">ℹ️</text>
        <text class="menu-text">关于</text>
        <text class="menu-arrow">›</text>
      </view>
    </view>

    <!-- 退出登录 -->
    <view class="logout-section">
      <button class="logout-btn" @click="handleLogout">退出登录</button>
    </view>
  </view>
</template>

<style lang="scss" scoped>
.mine-page {
  min-height: 100vh;
  background: #f5f5f5;
}

.user-header {
  background: linear-gradient(135deg, #1677ff 0%, #4096ff 100%);
  padding: 64rpx 32rpx 48rpx;
  display: flex;
  flex-direction: column;
  align-items: center;
  color: #fff;
}

.user-avatar {
  width: 120rpx;
  height: 120rpx;
  background: rgba(255, 255, 255, 0.2);
  border-radius: 60rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 16rpx;
}

.avatar-text {
  font-size: 48rpx;
  font-weight: bold;
  color: #fff;
}

.user-name {
  font-size: 34rpx;
  font-weight: 600;
  margin-bottom: 4rpx;
}

.user-id {
  font-size: 24rpx;
  opacity: 0.7;
}

.menu-group {
  background: #fff;
  margin: 16rpx 24rpx;
  border-radius: 12rpx;
  overflow: hidden;
}

.menu-item {
  display: flex;
  align-items: center;
  padding: 28rpx 24rpx;

  &:not(:last-child) {
    border-bottom: 1rpx solid #f5f5f5;
  }
}

.menu-icon {
  font-size: 36rpx;
  margin-right: 16rpx;
}

.menu-text {
  flex: 1;
  font-size: 28rpx;
  color: #333;
}

.menu-arrow {
  font-size: 32rpx;
  color: #ccc;
}

.logout-section {
  padding: 48rpx 24rpx;
}

.logout-btn {
  width: 100%;
  height: 88rpx;
  background: #fff;
  color: #ff4d4f;
  font-size: 30rpx;
  border-radius: 12rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1rpx solid #ff4d4f;
}
</style>
