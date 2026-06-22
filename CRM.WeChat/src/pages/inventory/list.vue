<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { inventoryApi, type InventoryOverview } from '@/api/inventory'
import { showToast } from '@/utils/index'

const keyword = ref('')
const list = ref<InventoryOverview[]>([])
const loading = ref(false)

onMounted(() => {
  loadData()
})

async function loadData() {
  loading.value = true
  try {
    const res = await inventoryApi.getOverview({
      keyword: keyword.value || undefined,
      page: 1,
      pageSize: 50,
    })
    list.value = res.items || []
  } catch (err: any) {
    showToast(err.message || '加载失败', 'error')
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  loadData()
}
</script>

<template>
  <view class="inventory-page">
    <!-- 搜索栏 -->
    <view class="search-bar">
      <view class="search-input-wrap">
        <text class="search-icon">🔍</text>
        <input
          v-model="keyword"
          class="search-input"
          type="text"
          placeholder="搜索物料名称/型号"
          confirm-type="search"
          @confirm="handleSearch"
        />
      </view>
    </view>

    <!-- 库存列表 -->
    <view v-if="list.length > 0" class="inventory-list">
      <view v-for="item in list" :key="item.stockId" class="inventory-item">
        <view class="inv-main">
          <text class="inv-material">{{ item.materialName || item.materialId }}</text>
          <text class="inv-model">{{ item.materialModel || '' }}</text>
        </view>
        <view class="inv-meta">
          <view class="inv-qty-group">
            <view class="qty-item">
              <text class="qty-label">在库</text>
              <text class="qty-value">{{ item.onHandQty }}</text>
            </view>
            <view class="qty-item">
              <text class="qty-label">可用</text>
              <text class="qty-value available">{{ item.availableQty }}</text>
            </view>
            <view class="qty-item">
              <text class="qty-label">锁定</text>
              <text class="qty-value locked">{{ item.lockedQty }}</text>
            </view>
          </view>
          <view class="inv-extra">
            <text class="inv-warehouse">{{ item.warehouseCode || item.warehouseId }}</text>
            <text class="inv-code">{{ item.stockCode || '—' }}</text>
          </view>
        </view>
      </view>
    </view>

    <!-- 空状态 -->
    <view v-else-if="!loading" class="empty-state">
      <text class="empty-icon">📦</text>
      <text class="empty-text">暂无库存数据</text>
    </view>
  </view>
</template>

<style lang="scss" scoped>
.inventory-page {
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

.inventory-list {
  padding: 16rpx 24rpx;
}

.inventory-item {
  background: #fff;
  padding: 24rpx;
  border-radius: 12rpx;
  margin-bottom: 16rpx;
}

.inv-main {
  margin-bottom: 16rpx;
}

.inv-material {
  font-size: 30rpx;
  font-weight: 500;
  color: #333;
  display: block;
  margin-bottom: 4rpx;
}

.inv-model {
  font-size: 24rpx;
  color: #999;
}

.inv-qty-group {
  display: flex;
  gap: 48rpx;
  margin-bottom: 12rpx;
}

.qty-item {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.qty-label {
  font-size: 22rpx;
  color: #999;
  margin-bottom: 4rpx;
}

.qty-value {
  font-size: 32rpx;
  font-weight: 600;
  color: #333;

  &.available {
    color: #52c41a;
  }

  &.locked {
    color: #ff4d4f;
  }
}

.inv-extra {
  display: flex;
  justify-content: space-between;
}

.inv-warehouse,
.inv-code {
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
