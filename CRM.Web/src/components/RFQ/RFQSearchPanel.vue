<script setup lang="ts">
import { reactive, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const form = reactive({
  keyword: '',
  status: undefined as number | undefined
})

function syncFromRoute() {
  if (route.name !== 'RFQList') return
  form.keyword = typeof route.query.keyword === 'string' ? route.query.keyword : ''
  const s = route.query.status
  if (s === undefined || s === null || s === '') {
    form.status = undefined
    return
  }
  const n = Number(s)
  form.status = Number.isNaN(n) ? undefined : n
}

watch(
  () => [route.name, route.query] as const,
  () => syncFromRoute(),
  { deep: true, immediate: true }
)

function handleReset() {
  router.push({ name: 'RFQList', query: {} })
}

function handleSearch() {
  const query: Record<string, string> = {}
  const kw = form.keyword.trim()
  if (kw) query.keyword = kw
  if (form.status !== undefined && form.status !== null) query.status = String(form.status)
  router.push({ name: 'RFQList', query })
}
</script>

<template>
  <div class="rfq-search-panel">
    <div class="rfq-search-panel__head">需求检索</div>

    <div class="rfq-search-panel__fields">
      <div class="field-col">
        <label class="field-label">关键词</label>
        <div class="field-control">
          <input
            v-model="form.keyword"
            type="text"
            class="field-input"
            placeholder="需求编号 / 客户 / 产品"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>

      <div class="field-col">
        <label class="field-label">状态</label>
        <el-select
          v-model="form.status"
          placeholder="全部状态"
          clearable
          class="field-select"
          filterable
          :teleported="false"
        >
          <el-option label="待分配" :value="0" />
          <el-option label="已分配" :value="1" />
          <el-option label="报价中" :value="2" />
          <el-option label="已报价" :value="3" />
          <el-option label="已选价" :value="4" />
          <el-option label="已转订单" :value="5" />
          <el-option label="已关闭" :value="6" />
        </el-select>
      </div>
    </div>

    <div class="rfq-search-panel__actions">
      <button type="button" class="btn-search" @click="handleSearch">搜索</button>
      <button type="button" class="btn-reset" @click="handleReset">重置</button>
    </div>
  </div>
</template>

<style scoped lang="scss">
.rfq-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: rgba(200, 220, 240, 0.9);
}

.rfq-search-panel__head {
  font-weight: 600;
  color: #e8f4ff;
  margin-bottom: 12px;
  font-size: 13px;
}

.rfq-search-panel__fields {
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

.field-select {
  width: 100%;
}

.rfq-search-panel__actions {
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
