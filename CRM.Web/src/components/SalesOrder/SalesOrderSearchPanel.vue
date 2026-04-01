<script setup lang="ts">
import { reactive, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const form = reactive({
  code: '',
  customer: '',
  status: undefined as number | undefined
})

const statusOptions = [
  { label: '新建', value: 1 },
  { label: '待审核', value: 2 },
  { label: '审核通过', value: 10 },
  { label: '进行中', value: 20 },
  { label: '完成', value: 100 },
  { label: '审核失败', value: -1 },
  { label: '取消', value: -2 }
] as const

function syncFromRoute() {
  if (route.name !== 'SalesOrderList') return
  form.code = typeof route.query.code === 'string' ? route.query.code : ''
  form.customer = typeof route.query.customer === 'string' ? route.query.customer : ''
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
  router.push({ name: 'SalesOrderList', query: {} })
}

function handleSearch() {
  const query: Record<string, string> = {}
  const code = form.code.trim()
  if (code) query.code = code
  const customer = form.customer.trim()
  if (customer) query.customer = customer
  if (form.status !== undefined && form.status !== null) query.status = String(form.status)
  router.push({ name: 'SalesOrderList', query })
}
</script>

<template>
  <div class="so-search-panel">
    <div class="so-search-panel__head">销售订单检索</div>

    <div class="so-search-panel__fields">
      <div class="field-col">
        <label class="field-label">订单号</label>
        <div class="field-control">
          <input
            v-model="form.code"
            type="text"
            class="field-input"
            placeholder="订单编号"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>

      <div class="field-col">
        <label class="field-label">客户</label>
        <div class="field-control">
          <input
            v-model="form.customer"
            type="text"
            class="field-input"
            placeholder="客户名称"
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
          :teleported="false"
        >
          <el-option v-for="opt in statusOptions" :key="String(opt.value)" :label="opt.label" :value="opt.value" />
        </el-select>
      </div>
    </div>

    <div class="so-search-panel__actions">
      <button type="button" class="btn-search" @click="handleSearch">搜索</button>
      <button type="button" class="btn-reset" @click="handleReset">重置</button>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.so-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: $text-secondary;
}

.so-search-panel__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 12px;
  font-size: 13px;
}

.so-search-panel__fields {
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
  color: $text-muted;
}

.field-control {
  width: 100%;
}

.field-input {
  width: 100%;
  box-sizing: border-box;
  padding: 7px 10px;
  font-size: 12px;
  color: $text-primary;
  background: $layer-3;
  border: 1px solid $border-panel;
  border-radius: 6px;
  outline: none;

  &::placeholder {
    color: $text-placeholder;
  }

  &:focus {
    border-color: var(--crm-accent-06);
  }
}

.field-select {
  width: 100%;
}

.so-search-panel__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 16px;
  padding-top: 12px;
  border-top: 1px solid $border-panel;
}

.btn-search {
  flex: 1;
  min-width: 72px;
  padding: 8px 12px;
  font-size: 12px;
  font-weight: 500;
  color: #fff;
  background: linear-gradient(135deg, $blue-primary, $cyan-primary);
  border: 1px solid var(--crm-action-primary-border);
  border-radius: 6px;
  cursor: pointer;
  transition: box-shadow 0.15s, transform 0.12s;

  &:hover {
    box-shadow: var(--crm-shadow-glow);
    transform: translateY(-1px);
  }
}

.btn-reset {
  padding: 8px 12px;
  font-size: 12px;
  color: $text-secondary;
  background: $layer-3;
  border: 1px solid $border-panel;
  border-radius: 6px;
  cursor: pointer;

  &:hover {
    background: var(--crm-accent-008);
    border-color: var(--crm-accent-018);
  }
}
</style>
