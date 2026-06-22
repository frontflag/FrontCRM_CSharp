<script setup lang="ts">
import { onLaunch, onShow, onHide } from '@dcloudio/uni-app'

onLaunch(() => {
  console.log('FrontCRM WeChat App Launch')
})

onShow(() => {
  console.log('App Show')
})

onHide(() => {
  console.log('App Hide')
})
</script>

<style lang="scss">
@import '@/uni.scss';

/* 全局样式 */
page {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
  font-size: 28rpx;
  color: #333333;
  background-color: #f5f5f5;
}

/* 通用工具类 */
.flex-center {
  display: flex;
  align-items: center;
  justify-content: center;
}

.flex-between {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.text-primary {
  color: #1677ff;
}

.text-danger {
  color: #ff4d4f;
}

.text-success {
  color: #52c41a;
}

.text-muted {
  color: #999999;
}

.bg-white {
  background-color: #ffffff;
}

.rounded-8 {
  border-radius: 16rpx;
}

.p-24 {
  padding: 24rpx;
}

.mt-16 {
  margin-top: 16rpx;
}

.mt-24 {
  margin-top: 24rpx;
}

.mb-16 {
  margin-bottom: 16rpx;
}
</style>
