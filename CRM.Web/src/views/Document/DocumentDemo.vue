<template>
  <div class="document-demo-page">
    <div class="page-header">
      <h1 class="page-title">文档模块演示</h1>
      <p class="page-desc">选择业务类型与业务ID，上传文档并预览/下载。</p>
    </div>
    <div class="form-row">
      <div class="field">
        <label>业务类型</label>
        <el-select v-model="bizType" placeholder="选择" style="width: 160px">
          <el-option label="Vendor（供应商）" value="Vendor" />
          <el-option label="Customer（客户）" value="Customer" />
          <el-option label="StockIn（入库单）" value="StockIn" />
          <el-option label="SalesOrder（销售订单）" value="SalesOrder" />
        </el-select>
      </div>
      <div class="field">
        <label>业务ID</label>
        <el-input v-model="bizId" placeholder="如供应商/客户ID" style="width: 240px" />
      </div>
    </div>
    <div class="panels">
      <div class="card">
        <h3>上传</h3>
        <DocumentUploadPanel
          :biz-type="bizType"
          :biz-id="bizId"
          @uploaded="listRef?.refresh?.()"
        />
      </div>
      <div class="card">
        <h3>列表与预览</h3>
        <DocumentListPanel ref="listRef" :biz-type="bizType" :biz-id="bizId" view-mode="grid" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'

const bizType = ref('Vendor')
const bizId = ref('demo-id')
const listRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.document-demo-page {
  padding: 24px;
  max-width: 900px;
  margin: 0 auto;
}
.page-header {
  margin-bottom: 20px;
  .page-title { font-size: 20px; font-weight: 600; color: $text-primary; margin: 0 0 8px; }
  .page-desc { font-size: 13px; color: $text-muted; margin: 0; }
}
.form-row {
  display: flex;
  gap: 16px;
  margin-bottom: 20px;
  .field label { display: block; margin-bottom: 4px; font-size: 12px; color: $text-muted; }
}
.panels {
  display: flex;
  flex-direction: column;
  gap: 20px;
}
.card {
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: 8px;
  padding: 16px;
  h3 { font-size: 14px; margin: 0 0 12px; color: $text-secondary; }
}
</style>
