<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { customerApi, type CustomerDetail } from '@/api/customer'
import { showToast, formatDate, maskPhone } from '@/utils/index'

const detail = ref<CustomerDetail | null>(null)
const loading = ref(true)

onLoad((options: any) => {
  const id = options?.id
  if (id) {
    loadDetail(id)
  } else {
    showToast('缺少客户ID', 'error')
  }
})

async function loadDetail(id: string) {
  loading.value = true
  try {
    detail.value = await customerApi.getDetail(id)
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
      <view class="loading-state">
        <text>加载中...</text>
      </view>
    </template>

    <template v-else-if="detail">
      <!-- 基本信息 -->
      <view class="info-card">
        <view class="card-title">基本信息</view>
        <view class="info-row">
          <text class="info-label">客户名称</text>
          <text class="info-value">{{ detail.customerName }}</text>
        </view>
        <view class="info-row">
          <text class="info-label">简称</text>
          <text class="info-value">{{ detail.customerShortName || '—' }}</text>
        </view>
        <view class="info-row">
          <text class="info-label">客户类型</text>
          <text class="info-value">{{ detail.customerTypeLabel || '—' }}</text>
        </view>
        <view class="info-row">
          <text class="info-label">客户等级</text>
          <text class="info-value">{{ detail.customerLevelLabel || '—' }}</text>
        </view>
        <view class="info-row">
          <text class="info-label">统一社会信用代码</text>
          <text class="info-value">{{ detail.unifiedSocialCreditCode || '—' }}</text>
        </view>
      </view>

      <!-- 联系信息 -->
      <view class="info-card">
        <view class="card-title">联系信息</view>
        <view class="info-row">
          <text class="info-label">联系人</text>
          <text class="info-value">{{ detail.contactPerson || '—' }}</text>
        </view>
        <view class="info-row">
          <text class="info-label">联系电话</text>
          <text class="info-value">{{ maskPhone(detail.contactPhone) }}</text>
        </view>
        <view class="info-row">
          <text class="info-label">联系邮箱</text>
          <text class="info-value">{{ detail.contactEmail || '—' }}</text>
        </view>
        <view class="info-row">
          <text class="info-label">地址</text>
          <text class="info-value">{{ detail.address || '—' }}</text>
        </view>
      </view>

      <!-- 销售信息 -->
      <view class="info-card">
        <view class="card-title">销售信息</view>
        <view class="info-row">
          <text class="info-label">销售负责人</text>
          <text class="info-value">{{ detail.salesUserName || '—' }}</text>
        </view>
        <view class="info-row">
          <text class="info-label">销售电话</text>
          <text class="info-value">{{ maskPhone(detail.salesUserPhone) }}</text>
        </view>
        <view class="info-row">
          <text class="info-label">创建时间</text>
          <text class="info-value">{{ formatDate(detail.createTime, 'YYYY-MM-DD HH:mm') }}</text>
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
  width: 200rpx;
}

.info-value {
  font-size: 26rpx;
  color: #333;
  text-align: right;
  flex: 1;
  word-break: break-all;
}

.remark-text {
  font-size: 26rpx;
  color: #666;
  line-height: 1.6;
}
</style>
