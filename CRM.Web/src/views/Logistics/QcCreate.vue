<template>
  <div class="qc-create-page">
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="goBack">返回</button>
        <div class="page-title-group">
          <div class="page-icon">QC</div>
          <h1 class="page-title">{{ isEdit ? '编辑质检' : '新建质检' }}</h1>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="goBack">取消</button>
        <button class="btn-primary" :disabled="submitting" @click="submitQc">
          {{ submitting ? '保存中...' : (isEdit ? '更新质检' : '保存质检') }}
        </button>
      </div>
    </div>

    <div class="qc-layout">
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--cyan"></div>
          <span class="section-title">供应信息</span>
        </div>
        <div class="section-body">
          <el-form label-width="88px" class="qc-form">
          <el-row :gutter="12">
            <el-col :span="6"><el-form-item label="到货编码"><el-input v-model="form.noticeCode" class="q-input" /></el-form-item></el-col>
            <el-col :span="6"><el-form-item label="供应商"><el-input v-model="form.vendorName" class="q-input" /></el-form-item></el-col>
            <el-col :span="6">
              <el-form-item label="采购员">
                <PurchaserCascader
                  v-model="form.purchaseUserId"
                  placeholder="请选择采购员"
                  class="q-input"
                  @change="onPurchaseUserChange"
                />
              </el-form-item>
            </el-col>
            <el-col :span="6"><el-form-item label="采购日期"><el-date-picker v-model="form.purchaseDate" type="date" value-format="YYYY-MM-DD" style="width:100%" class="q-date" /></el-form-item></el-col>
          </el-row>
          <el-form-item label="到货通知备注"><el-input v-model="form.noticeRemark" type="textarea" :rows="2" class="q-input" /></el-form-item>
          </el-form>
        </div>
      </div>

      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--amber"></div>
          <span class="section-title">送货信息</span>
        </div>
        <div class="section-body">
          <el-form label-width="88px" class="qc-form">
          <el-row :gutter="12">
            <el-col :span="6"><el-form-item label="快递单号"><el-input v-model="form.expressNo" class="q-input" /></el-form-item></el-col>
            <el-col :span="6"><el-form-item label="送货方式"><el-input v-model="form.deliveryMethod" class="q-input" /></el-form-item></el-col>
            <el-col :span="6"><el-form-item label="快递方式"><el-input v-model="form.expressMethod" class="q-input" /></el-form-item></el-col>
            <el-col :span="6"><el-form-item label="到货日期"><el-date-picker v-model="form.arrivalDate" type="date" value-format="YYYY-MM-DD" style="width:100%" class="q-date" /></el-form-item></el-col>
          </el-row>
          </el-form>
        </div>
      </div>

      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--green"></div>
          <span class="section-title">物料信息</span>
        </div>
        <div class="section-body">
          <el-form label-width="88px" class="qc-form">
          <el-row :gutter="12">
            <el-col :span="8"><el-form-item label="物料型号"><el-input v-model="form.materialCode" class="q-input" /></el-form-item></el-col>
            <el-col :span="8"><el-form-item label="品牌"><el-input v-model="form.brand" class="q-input" /></el-form-item></el-col>
            <el-col :span="8"><el-form-item label="数量"><el-input-number v-model="form.arrivedTotalQty" :min="0" :precision="4" style="width:100%" class="q-number" /></el-form-item></el-col>
          </el-row>
          </el-form>
        </div>
      </div>

      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--cyan"></div>
          <span class="section-title">质检信息</span>
        </div>
        <div class="section-body">
          <el-form label-width="88px" class="qc-form">
          <el-row :gutter="12">
            <el-col :span="6"><el-form-item label="抽检数量"><el-input-number v-model="form.sampleQty" :min="0" :precision="4" style="width:100%" class="q-number" /></el-form-item></el-col>
            <el-col :span="6"><el-form-item label="抽检日期"><el-date-picker v-model="form.sampleDate" type="date" value-format="YYYY-MM-DD" style="width:100%" class="q-date" /></el-form-item></el-col>
            <el-col :span="6"><el-form-item label="质检人"><el-input v-model="form.qcUser" class="q-input" /></el-form-item></el-col>
            <el-col :span="6">
              <el-form-item label="质检结果">
                <el-select
                  v-model="form.qcResult"
                  style="width:100%"
                  :class="['q-select', 'qc-result-select', `qc-result-${form.qcResult}`]"
                >
                  <el-option label="通过" value="pass" />
                  <el-option label="部分通过" value="partial" />
                  <el-option label="拒收" value="reject" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="6"><el-form-item label="可入库数量"><el-input-number v-model="form.stockInQty" :min="0" :precision="4" style="width:100%" class="q-number" /></el-form-item></el-col>
          </el-row>
          <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" :rows="2" class="q-input" /></el-form-item>
          </el-form>
        </div>
      </div>

      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--amber"></div>
          <span class="section-title">质检图片</span>
        </div>
        <div class="section-body">
          <el-upload
            class="qc-upload"
            action="#"
            list-type="picture-card"
            :auto-upload="false"
            :file-list="form.fileList"
            multiple
          >
            <el-icon><Plus /></el-icon>
          </el-upload>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { logisticsApi } from '@/api/logistics'
import { useRoute, useRouter } from 'vue-router'
import { Plus } from '@element-plus/icons-vue'
import PurchaserCascader from '@/components/PurchaserCascader.vue'

const route = useRoute()
const router = useRouter()
const submitting = ref(false)
const isEdit = ref(false)
const currentQcId = ref('')

const form = reactive<any>({
  noticeId: '',
  noticeCode: '',
  materialCode: '',
  brand: '',
  vendorName: '',
  purchaseDate: '',
  purchaseUserId: '',
  purchaseUserName: '',
  noticeRemark: '',
  deliveryMethod: '',
  expressMethod: '',
  expressNo: '',
  arrivalDate: '',
  sampleQty: 0,
  sampleDate: '',
  qcUser: '',
  qcResult: 'pass',
  stockInQty: 0,
  remark: '',
  fileList: [],
  arrivedTotalQty: 0
})

function onPurchaseUserChange(p: { id: string; label: string }) {
  form.purchaseUserName = p.label || ''
}

const fillNotice = async (noticeId: string) => {
  form.noticeId = noticeId
  if (!noticeId) return
  const notices = await logisticsApi.getArrivalNotices()
  const row = notices.find(x => x.id === noticeId)
  if (!row) return
  const firstItem = row.items?.[0]
  const arrivedTotalQty = Number((row.items || []).reduce((s, x) => s + Number(x.arrivedQty || 0), 0))
  form.noticeCode = row.noticeCode || ''
  form.materialCode = firstItem?.pn || ''
  form.brand = firstItem?.brand || ''
  form.vendorName = row.vendorName || ''
  form.purchaseUserName = row.purchaseUserName || ''
  form.purchaseUserId = ''
  form.stockInQty = arrivedTotalQty
  form.sampleQty = arrivedTotalQty
  form.arrivedTotalQty = arrivedTotalQty
}

const loadPageData = async () => {
  const qcId = String(route.query.qcId || '')
  const noticeId = String(route.query.noticeId || '')
  if (qcId) {
    isEdit.value = true
    currentQcId.value = qcId
    const qcs = await logisticsApi.getQcs()
    const qc = qcs.find(x => x.id === qcId)
    if (qc) {
      // 先回填质检记录本身，保证“查看”打开时一定带出已有判定数据
      form.noticeId = qc.stockInNotifyId || ''
      form.noticeCode = qc.stockInNotifyCode || ''
      form.qcResult = qc.status === -1 ? 'reject' : qc.status === 10 ? 'partial' : 'pass'
      form.stockInQty = Number(qc.passQty || 0)
      form.sampleQty = Number(qc.passQty || 0)
      form.arrivedTotalQty = Number((qc.passQty || 0) + (qc.rejectQty || 0))

      // 再补齐到货通知维度的数据（供应商/物料等）
      await fillNotice(qc.stockInNotifyId)
      form.qcResult = qc.status === -1 ? 'reject' : qc.status === 10 ? 'partial' : 'pass'
      form.stockInQty = Number(qc.passQty || 0)
      form.sampleQty = Number(qc.passQty || 0)
    }
    return
  }

  isEdit.value = false
  await fillNotice(noticeId)
}

const submitQc = async () => {
  if (submitting.value) return
  if (!form.noticeId) {
    ElMessage.warning('缺少到货通知ID')
    return
  }
  submitting.value = true
  try {
    let qcId = currentQcId.value
    if (!isEdit.value) {
      const qc = await logisticsApi.createQc(form.noticeId)
      qcId = qc.id
    }
    const passQty = Number(form.stockInQty || 0)
    const rejectQty = Math.max(0, Number(form.arrivedTotalQty || 0) - passQty)
    await logisticsApi.updateQcResult(qcId, { result: form.qcResult, passQty, rejectQty })
    ElMessage.success(isEdit.value ? '质检已更新' : '质检已保存')
    router.push({ name: 'QcList', query: { qcId } })
  } catch (e: any) {
    ElMessage.error(e?.message || (isEdit.value ? '更新质检失败' : '创建质检失败'))
  } finally {
    submitting.value = false
  }
}

const goBack = () => router.back()

onMounted(loadPageData)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.qc-create-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
  .header-left { display: flex; align-items: center; gap: 14px; }
  .header-right { display: flex; gap: 10px; }
}

.btn-back,
.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 8px 14px;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
  &:hover { background: rgba(255, 255, 255, 0.08); border-color: rgba(0, 212, 255, 0.25); }
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
  .page-icon {
    width: 32px;
    height: 32px;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.25);
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: $cyan-primary;
    font-size: 12px;
    font-weight: 700;
  }
  .page-title {
    font-size: 18px;
    font-weight: 600;
    color: $text-primary;
    margin: 0;
  }
}

.qc-layout { display: flex; flex-direction: column; gap: 12px; }

.form-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}
.section-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: rgba(0, 0, 0, 0.1);
}
.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
  &--cyan  { background: $cyan-primary; box-shadow: 0 0 6px rgba(0,212,255,0.6); }
  &--amber { background: $color-amber;  box-shadow: 0 0 6px rgba(201,154,69,0.6); }
  &--green { background: $color-mint-green; box-shadow: 0 0 6px rgba(70,191,145,0.6); }
}
.section-title { font-size: 14px; font-weight: 500; color: $text-primary; }
.section-body { padding: 16px 20px; }
.qc-upload-title { font-size: 14px; font-weight: 600; margin-bottom: 8px; color: $text-secondary; }

.qc-form {
  :deep(.el-form-item__label) { color: $text-muted !important; font-size: 13px; }
}

.q-input {
  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    box-shadow: none !important;
    color: $text-primary !important;
  }
  :deep(.el-input__inner) { color: $text-primary !important; }
}

.q-select {
  :deep(.el-select__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    box-shadow: none !important;
  }
}

.qc-result-select.qc-result-pass {
  :deep(.el-select__wrapper) {
    background-color: rgba(70, 191, 145, 0.25) !important;
    border-color: rgba(70, 191, 145, 0.65) !important;
  }
}

.qc-result-select.qc-result-partial {
  :deep(.el-select__wrapper) {
    background-color: rgba(201, 154, 69, 0.25) !important;
    border-color: rgba(201, 154, 69, 0.65) !important;
  }
}

.qc-result-select.qc-result-reject {
  :deep(.el-select__wrapper) {
    background-color: rgba(201, 87, 69, 0.25) !important;
    border-color: rgba(201, 87, 69, 0.65) !important;
  }
}

.q-number {
  :deep(.el-input__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    box-shadow: none !important;
  }
}

.q-date {
  :deep(.el-input__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    box-shadow: none !important;
  }
}
</style>
