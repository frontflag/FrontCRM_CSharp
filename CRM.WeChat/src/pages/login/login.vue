<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { showToast } from '@/utils/index'

const authStore = useAuthStore()

const username = ref('')
const password = ref('')
const loading = ref(false)

async function handleLogin() {
  if (!username.value.trim()) {
    showToast('请输入用户名', 'none')
    return
  }
  if (!password.value.trim()) {
    showToast('请输入密码', 'none')
    return
  }

  loading.value = true
  try {
    await authStore.login(username.value.trim(), password.value)
    await authStore.fetchUserInfo()
    showToast('登录成功', 'success')
    uni.switchTab({ url: '/pages/index/index' })
  } catch (err: any) {
    showToast(err.message || '登录失败', 'error')
  } finally {
    loading.value = false
  }
}

/** 微信一键登录（小程序端） */
function handleWechatLogin() {
  // #ifdef MP-WEIXIN
  uni.login({
    provider: 'weixin',
    success: async (loginRes: any) => {
      try {
        loading.value = true
        await authStore.wechatLogin(loginRes.code)
        await authStore.fetchUserInfo()
        showToast('登录成功', 'success')
        uni.switchTab({ url: '/pages/index/index' })
      } catch (err: any) {
        showToast(err.message || '微信登录失败', 'error')
      } finally {
        loading.value = false
      }
    },
    fail: () => {
      showToast('微信授权失败', 'error')
    },
  })
  // #endif
}
</script>

<template>
  <view class="login-page">
    <view class="login-header">
      <view class="login-logo">
        <text class="logo-text">FrontCRM</text>
      </view>
      <text class="login-subtitle">客户关系管理系统</text>
    </view>

    <view class="login-form">
      <view class="form-item">
        <input
          v-model="username"
          class="form-input"
          type="text"
          placeholder="请输入用户名"
          :disabled="loading"
        />
      </view>
      <view class="form-item">
        <input
          v-model="password"
          class="form-input"
          type="password"
          placeholder="请输入密码"
          :disabled="loading"
        />
      </view>
      <button
        class="login-btn"
        :class="{ loading }"
        :disabled="loading"
        @click="handleLogin"
      >
        {{ loading ? '登录中...' : '登录' }}
      </button>

      <!-- 微信一键登录（仅小程序） -->
      <!-- #ifdef MP-WEIXIN -->
      <button class="wechat-login-btn" :disabled="loading" @click="handleWechatLogin">
        微信一键登录
      </button>
      <!-- #endif -->
    </view>
  </view>
</template>

<style lang="scss" scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60rpx 48rpx;
  background: linear-gradient(135deg, #1677ff 0%, #4096ff 100%);
}

.login-header {
  text-align: center;
  margin-bottom: 80rpx;
}

.login-logo {
  width: 160rpx;
  height: 160rpx;
  background: rgba(255, 255, 255, 0.2);
  border-radius: 40rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 32rpx;
}

.logo-text {
  font-size: 48rpx;
  font-weight: bold;
  color: #fff;
}

.login-subtitle {
  font-size: 28rpx;
  color: rgba(255, 255, 255, 0.8);
}

.login-form {
  width: 100%;
  max-width: 600rpx;
}

.form-item {
  margin-bottom: 32rpx;
}

.form-input {
  width: 100%;
  height: 96rpx;
  background: rgba(255, 255, 255, 0.15);
  border: 2rpx solid rgba(255, 255, 255, 0.3);
  border-radius: 16rpx;
  padding: 0 32rpx;
  font-size: 30rpx;
  color: #fff;
  box-sizing: border-box;

  &::placeholder {
    color: rgba(255, 255, 255, 0.6);
  }
}

.login-btn {
  width: 100%;
  height: 96rpx;
  background: #fff;
  color: #1677ff;
  font-size: 32rpx;
  font-weight: 500;
  border-radius: 16rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  margin-top: 16rpx;

  &.loading {
    opacity: 0.7;
  }
}

.wechat-login-btn {
  width: 100%;
  height: 96rpx;
  background: #07c160;
  color: #fff;
  font-size: 32rpx;
  font-weight: 500;
  border-radius: 16rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  margin-top: 24rpx;
}
</style>
