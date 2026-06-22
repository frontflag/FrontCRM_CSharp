<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { customerApi, type CustomerListItem } from '@/api/customer'
import { showToast } from '@/utils/index'

const keyword = ref('')
const list = ref<CustomerListItem[]>([])
const loading = ref(false)
const page = ref(1)
const total = ref(0)
const hasMore = ref(true)

const pageSize = 20

onMounted(() => {
  loadData()
})

async function loadData(reset = false) {
  if (loading.value) return
  if (!hasMore.value && !reset) return

  if (reset) {
    page.value = 1
    hasMore.value = true
  }

  loading.value = true
  try {
    const res = await customerApi.getList({
      keyword: keyword.value || undefined,
      page: page.value,
      pageSize,
    })
    if (reset) {
      list.value = res.items
    } else {
      list.value = [...list.value, ...res.items]
    }
    total.value = res.total
    hasMore.value = list.value.length < total.value
  } catch (err: any) {
    showToast(err.message || '加载失败', 'error')
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  loadData(true)
}

function handleLoadMore() {
  page.value++
  loadData()
}

function goDetail(id: string) {
  uni.navigateTo({ url: `/pages/customer/detail?id=${id}` })
}
</script>

<template>
  <view class="customer-page">
    <!-- 搜索栏 -->
    <view class="search-bar">
      <view class="search-input-wrap">
        <text class="search-icon">🔍</text>
        <input
          v-model="keyword"
          class="search-input"
          type="text"
          placeholder="搜索客户名称"
          confirm-type="search"
          @confirm="handleSearch"
        />
      </view>
    </view>

    <!-- 列表 -->
    <view v-if="list.length > 0" class="customer-list">
      <view
        v-for="item in list"
        :key="item.id"
        class="customer-item"
        @click="goDetail(item.id)"
      >
        <view class="item-main">
          <text class="item-name">{{ item.customerName }}</text>
          <text class="item-contact">{{ item.contactPerson || '无联系人' }}</text>
        </view>
        <view class="item-sub">
          <text class="item-sales">销售: {{ item.salesUserName || '—' }}</text>
          <text class="item-arrow">›</text>
        </view>
      </view>
    </view>

    <!-- 空状态 -->
    <view v-else-if="!loading" class="empty-state">
      <text class="empty-icon">📭</text>
      <text class="empty-text">暂无客户数据</text>
    </view>

    <!-- 加载更多 -->
    <view v-if="hasMore && list.length > 0" class="load-more" @click="handleLoadMore">
      <text>{{ loading ? '加载中...' : '加载更多' }}</text>
    </view>

    <!-- 没有更多 -->
    <view v-if="!hasMore && list.length > 0" class="no-more">
      <text>— 已加载全部 {{ total }} 条 —</text>
    </view>
  </view>
</template>

<style lang="scss" scoped>
.customer-page {
  min-height: 100vh;
  background: #f5f5f5;
}

.search-bar {
  background: #fff;
  padding: 16rpx 24rpx;
}

.search-input-wrap {
  display: flex;
  align-items: center;
  background: #f5f5f5;
  border-radius: 12rpx;
  padding: 12rpx 20rpx;
}

.search-icon {
  font-size: 28rpx;
  margin-right: 12rpx;
}

.search-input {
  flex: 1;
  font-size: 28rpx;
  height: 56rpx;
}

.customer-list {
  padding: 16rpx 24rpx;
}

.customer-item {
  background: #fff;
  padding: 24rpx;
  border-radius: 12rpx;
  margin-bottom: 16rpx;
}

.item-main {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8rpx;
}

.item-name {
  font-size: 30rpx;
  font-weight: 500;
  color: #333;
}

.item-contact {
  font-size: 24rpx;
  color: #999;
}

.item-sub {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.item-sales {
  font-size: 24rpx;
  color: #666;
}

.item-arrow {
  font-size: 32rpx;
  color: #ccc;
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

.load-more,
.no-more {
  text-align: center;
  padding: 24rpx;
  font-size: 24rpx;
  color: #999;
}

.load-more {
  color: #1677ff;
}
</style>
