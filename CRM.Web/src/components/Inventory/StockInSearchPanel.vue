<script setup lang="ts">
import { reactive, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const form = reactive({
  model: '',
  vendorName: '',
  purchaseOrderCode: '',
  salesOrderCode: ''
})

function syncFromRoute() {
  if (route.name !== 'StockInList') return
  const q = route.query
  form.model = typeof q.model === 'string' ? q.model : ''
  form.vendorName = typeof q.vendorName === 'string' ? q.vendorName : ''
  form.purchaseOrderCode = typeof q.purchaseOrderCode === 'string' ? q.purchaseOrderCode : ''
  form.salesOrderCode = typeof q.salesOrderCode === 'string' ? q.salesOrderCode : ''
}

watch(
  () => [route.name, route.query] as const,
  () => syncFromRoute(),
  { deep: true, immediate: true }
)

function handleReset() {
  router.push({ name: 'StockInList', query: {} })
}

function handleSearch() {
  const query: Record<string, string> = {}
  const m = form.model.trim()
  if (m) query.model = m
  const v = form.vendorName.trim()
  if (v) query.vendorName = v
  const p = form.purchaseOrderCode.trim()
  if (p) query.purchaseOrderCode = p
  const s = form.salesOrderCode.trim()
  if (s) query.salesOrderCode = s
  router.push({ name: 'StockInList', query })
}
</script>

<template>
  <div class="si-search-panel">
    <div class="si-search-panel__head">入库单检索</div>

    <div class="si-search-panel__fields">
      <div class="field-col">
        <label class="field-label">物料型号</label>
        <div class="field-control">
          <input
            v-model="form.model"
            type="text"
            class="field-input"
            placeholder="物料型号 / 物料ID"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>

      <div class="field-col">
        <label class="field-label">供应商名称</label>
        <div class="field-control">
          <input
            v-model="form.vendorName"
            type="text"
            class="field-input"
            placeholder="供应商名称"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>

      <div class="field-col">
        <label class="field-label">采购订单号</label>
        <div class="field-control">
          <input
            v-model="form.purchaseOrderCode"
            type="text"
            class="field-input"
            placeholder="采购订单号"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>

      <div class="field-col">
        <label class="field-label">销售订单号</label>
        <div class="field-control">
          <input
            v-model="form.salesOrderCode"
            type="text"
            class="field-input"
            placeholder="销售订单号"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>
    </div>

    <div class="si-search-panel__actions">
      <button type="button" class="btn-search" @click="handleSearch">搜索</button>
      <button type="button" class="btn-reset" @click="handleReset">重置</button>
    </div>
  </div>
</template>

<style scoped lang="scss">
.si-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: rgba(200, 220, 240, 0.9);
}

.si-search-panel__head {
  font-weight: 600;
  color: #e8f4ff;
  margin-bottom: 12px;
  font-size: 13px;
}

.si-search-panel__fields {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.field-col {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.field-label {
  font-size: 11px;
  font-weight: 500;
  color: rgba(120, 190, 232, 0.7);
}

.field-control {
  width: 100%;
}

.field-input {
  width: 100%;
  box-sizing: border-box;
  padding: 7px 10px;
  font-size: 12px;
  color: rgba(224, 244, 255, 0.92);
  background: rgba(10, 22, 40, 0.85);
  border: 1px solid rgba(0, 212, 255, 0.18);
  border-radius: 6px;
  outline: none;

  &::placeholder {
    color: rgba(140, 170, 200, 0.45);
  }

  &:focus {
    border-color: rgba(0, 212, 255, 0.45);
  }
}

.si-search-panel__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 16px;
  padding-top: 12px;
  border-top: 1px solid rgba(0, 212, 255, 0.1);
}

.btn-search {
  flex: 1;
  min-width: 72px;
  padding: 8px 12px;
  font-size: 12px;
  font-weight: 500;
  color: #fff;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.75), rgba(0, 212, 255, 0.65));
  border: 1px solid rgba(0, 212, 255, 0.35);
  border-radius: 6px;
  cursor: pointer;
  transition: box-shadow 0.15s, transform 0.12s;

  &:hover {
    box-shadow: 0 2px 12px rgba(0, 212, 255, 0.2);
    transform: translateY(-1px);
  }
}

.btn-reset {
  padding: 8px 12px;
  font-size: 12px;
  color: rgba(180, 210, 230, 0.85);
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 6px;
  cursor: pointer;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.22);
  }
}
</style>
