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
          <el-form label-width="120px" class="qc-form">
          <el-row :gutter="12">
            <el-col :span="6">
              <el-form-item label="到货通知编码">
                <el-input v-model="form.noticeCode" disabled class="q-input q-input--readonly" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item label="供应商">
                <el-input v-model="form.vendorName" disabled class="q-input q-input--readonly" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item label="采购员">
                <el-input :model-value="form.purchaseUserName?.trim() || '—'" disabled class="q-input q-input--readonly" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item label="采购单号">
                <el-input v-model="form.purchaseOrderCode" disabled class="q-input q-input--readonly" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-form-item label="到货通知备注">
            <el-input v-model="form.noticeRemark" type="textarea" :rows="2" disabled class="q-input q-input--readonly" />
          </el-form-item>
          </el-form>
        </div>
      </div>

      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--amber"></div>
          <span class="section-title">送货信息</span>
        </div>
        <div class="section-body">
          <el-form label-width="120px" class="qc-form">
          <el-row :gutter="12">
            <el-col :span="6"><el-form-item label="快递单号"><el-input v-model="form.expressNo" class="q-input" /></el-form-item></el-col>
            <el-col :span="6"><el-form-item label="送货方式"><el-input v-model="form.deliveryMethod" class="q-input" /></el-form-item></el-col>
            <el-col :span="6"><el-form-item label="快递方式"><el-input v-model="form.expressMethod" class="q-input" /></el-form-item></el-col>
            <el-col :span="6"><el-form-item label="到货日期"><el-date-picker v-model="form.arrivalDate" type="date" value-format="YYYY-MM-DD" style="width:100%" class="q-date" /></el-form-item></el-col>
            <el-col :span="6"><el-form-item label="入库日期"><el-date-picker v-model="form.stockInPlanDate" type="date" value-format="YYYY-MM-DD" placeholder="生成入库单用" style="width:100%" class="q-date" /></el-form-item></el-col>
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
          <el-form label-width="120px" class="qc-form">
          <el-row :gutter="12">
            <el-col :span="8"><el-form-item label="物料型号"><el-input v-model="form.materialCode" class="q-input" /></el-form-item></el-col>
            <el-col :span="8"><el-form-item label="品牌"><el-input v-model="form.brand" class="q-input" /></el-form-item></el-col>
            <el-col :span="8">
              <el-form-item label="到货数量">
                <el-input-number
                  v-model="form.arrivedTotalQty"
                  :min="0"
                  :precision="0"
                  :step="1"
                  step-strictly
                  style="width:100%"
                  class="q-number"
                />
              </el-form-item>
            </el-col>
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
          <el-form label-width="120px" class="qc-form">
          <el-row :gutter="12">
            <el-col :span="6">
              <el-form-item label="抽检数量">
                <el-input-number
                  v-model="form.sampleQty"
                  :min="0"
                  :precision="0"
                  :step="1"
                  step-strictly
                  style="width:100%"
                  class="q-number"
                />
              </el-form-item>
            </el-col>
            <el-col :span="6"><el-form-item label="抽检日期"><el-date-picker v-model="form.sampleDate" type="date" value-format="YYYY-MM-DD" style="width:100%" class="q-date" /></el-form-item></el-col>
            <el-col :span="6">
              <el-form-item label="质检人">
                <el-select
                  v-model="form.qcUserId"
                  filterable
                  clearable
                  placeholder="请选择物流部员工"
                  style="width:100%"
                  :class="['q-select', 'qc-inspector-select']"
                  @change="onQcInspectorChange"
                >
                  <el-option
                    v-for="u in logisticsUserOptions"
                    :key="u.id"
                    :label="inspectorOptionLabel(u)"
                    :value="u.id"
                  />
                </el-select>
              </el-form-item>
            </el-col>
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
            <el-col :span="6">
              <el-form-item label="可入库数量">
                <el-input-number
                  v-model="form.stockInQty"
                  :min="0"
                  :precision="0"
                  :step="1"
                  step-strictly
                  style="width:100%"
                  class="q-number"
                />
              </el-form-item>
            </el-col>
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
          <div class="qc-upload-hint-block">
            <p v-if="!currentQcId" class="qc-upload-hint">
              <strong>新建：</strong>当前尚无质检单号，首次点击「保存质检」会创建单据；保存前已选好的图片会随本次保存一并上传。
            </p>
            <p v-else class="qc-upload-hint">
              <strong>编辑：</strong>可随时添加图片，点击「保存质检」后新选择的图片会上传并关联本单；删除已保存缩略图会同步删除服务端文档。通过「质检列表」再次进入本页可查看历史图片。
            </p>
          </div>
          <el-upload
            class="qc-upload"
            action="#"
            list-type="picture-card"
            :auto-upload="false"
            v-model:file-list="qcFileList"
            multiple
            accept="image/jpeg,image/jpg,image/png,image/webp,image/gif"
            :limit="24"
            :before-remove="beforeRemoveQcImage"
            :on-preview="onPreviewQcImage"
          >
            <el-icon><Plus /></el-icon>
          </el-upload>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { UploadFile } from 'element-plus'
import { logisticsApi } from '@/api/logistics'
import { authApi, type SalesUserSelectOption } from '@/api/auth'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { useRoute, useRouter } from 'vue-router'
import { Plus } from '@element-plus/icons-vue'
import apiClient from '@/api/client'
import { documentApi, DOCUMENT_BIZ_TYPE_QC, type UploadDocumentDto } from '@/api/document'

type QcUploadFile = UploadFile & { documentId?: string }

const route = useRoute()
const router = useRouter()
const submitting = ref(false)
const isEdit = ref(false)
const currentQcId = ref('')
/** 仅物流部门（及仓储等）职员，供质检人下拉 */
const logisticsUserOptions = ref<SalesUserSelectOption[]>([])

/** 质检图片（含已保存文档的预览与待上传的本地文件） */
const qcFileList = ref<QcUploadFile[]>([])
const qcPreviewBlobUrls: string[] = []

const form = reactive<any>({
  noticeId: '',
  noticeCode: '',
  materialCode: '',
  brand: '',
  vendorName: '',
  purchaseOrderCode: '',
  purchaseUserId: '',
  purchaseUserName: '',
  noticeRemark: '',
  deliveryMethod: '',
  expressMethod: '',
  expressNo: '',
  arrivalDate: '',
  /** 与后端 qcinfo.StockInPlanDate 对应；默认来自到货通知预计到货日 */
  stockInPlanDate: '',
  sampleQty: 0,
  sampleDate: '',
  qcUserId: '',
  qcUser: '',
  qcResult: 'pass',
  stockInQty: 0,
  remark: '',
  arrivedTotalQty: 0
})

function inspectorOptionLabel(u: SalesUserSelectOption) {
  return (u.realName || u.label || u.userName || '').trim() || u.id
}

function onQcInspectorChange(id: string | undefined | null) {
  const sid = id != null && id !== '' ? String(id) : ''
  if (!sid) {
    form.qcUser = ''
    return
  }
  const u = logisticsUserOptions.value.find((x) => x.id === sid)
  form.qcUser = u ? inspectorOptionLabel(u) : ''
}

async function loadLogisticsUsers() {
  try {
    logisticsUserOptions.value = await authApi.getLogisticsUsersForSelect()
  } catch {
    logisticsUserOptions.value = []
  }
}

/** 按采购订单主键拉取采购员（与采购单号唯一对应），名称以订单数据为准 */
function isImageDocumentDto(d: UploadDocumentDto): boolean {
  const t = (d.mimeType || '').toLowerCase()
  const e = (d.fileExtension || '').toLowerCase()
  return /^image\//.test(t) || ['.jpg', '.jpeg', '.png', '.gif', '.webp'].includes(e)
}

function revokeQcPreviewUrls() {
  qcPreviewBlobUrls.forEach((u) => URL.revokeObjectURL(u))
  qcPreviewBlobUrls.length = 0
}

async function loadQcDocuments(qcId: string) {
  revokeQcPreviewUrls()
  qcFileList.value = []
  if (!qcId) return
  try {
    const docs = await documentApi.getDocuments(DOCUMENT_BIZ_TYPE_QC, qcId)
    const imageDocs = docs.filter(isImageDocumentDto)
    const list: QcUploadFile[] = []
    let seq = 0
    for (const d of imageDocs) {
      const blob = (await apiClient.get(`/api/v1/documents/${encodeURIComponent(d.id)}/preview`, {
        responseType: 'blob',
      })) as unknown as Blob
      if (!(blob instanceof Blob) || blob.size === 0) continue
      const url = URL.createObjectURL(blob)
      qcPreviewBlobUrls.push(url)
      seq += 1
      list.push({
        name: d.originalFileName || `image-${seq}`,
        url,
        uid: Date.now() + seq,
        status: 'success',
        documentId: d.id,
      })
    }
    qcFileList.value = list
  } catch {
    qcFileList.value = []
  }
}

async function beforeRemoveQcImage(uploadFile: UploadFile) {
  const qf = uploadFile as QcUploadFile
  if (qf.documentId) {
    try {
      await ElMessageBox.confirm(`确定删除图片「${uploadFile.name}」？`, '删除确认', {
        type: 'warning',
        confirmButtonText: '删除',
        cancelButtonText: '取消',
      })
      await documentApi.deleteDocument(qf.documentId)
      if (uploadFile.url?.startsWith('blob:')) {
        URL.revokeObjectURL(uploadFile.url)
        const i = qcPreviewBlobUrls.indexOf(uploadFile.url)
        if (i >= 0) qcPreviewBlobUrls.splice(i, 1)
      }
      return true
    } catch {
      return false
    }
  }
  if (uploadFile.url?.startsWith('blob:')) {
    URL.revokeObjectURL(uploadFile.url)
    const i = qcPreviewBlobUrls.indexOf(uploadFile.url)
    if (i >= 0) qcPreviewBlobUrls.splice(i, 1)
  }
  return true
}

function onPreviewQcImage(uploadFile: UploadFile) {
  const qf = uploadFile as QcUploadFile
  if (qf.documentId) {
    void documentApi.openPreviewInNewTab(qf.documentId)
    return
  }
  if (uploadFile.url) window.open(uploadFile.url, '_blank', 'noopener,noreferrer')
}

const MAX_FILES_PER_UPLOAD = 5

async function uploadPendingQcImages(qcId: string, files: File[]) {
  if (!files.length) return
  for (let i = 0; i < files.length; i += MAX_FILES_PER_UPLOAD) {
    const chunk = files.slice(i, i + MAX_FILES_PER_UPLOAD)
    await documentApi.uploadDocuments(DOCUMENT_BIZ_TYPE_QC, qcId, chunk)
  }
}

/** 解析质检计划入库日（兼容 camelCase / PascalCase、ISO 带时区） */
function qcStockInPlanDateToYmd(v: unknown): string {
  if (v == null || v === '') return ''
  const s = String(v).trim()
  if (s.length >= 10 && /^\d{4}-\d{2}-\d{2}/.test(s)) return s.slice(0, 10)
  return ''
}

async function applyPurchaseUserFromPurchaseOrder(purchaseOrderId: string | undefined | null) {
  const id = String(purchaseOrderId || '').trim()
  if (!id) return
  try {
    const po = (await purchaseOrderApi.getById(id)) as Record<string, unknown> | null | undefined
    if (!po) return
    const uid = po.purchaseUserId ?? po.PurchaseUserId
    const uname = po.purchaseUserName ?? po.PurchaseUserName
    if (uid != null && uid !== '') form.purchaseUserId = String(uid)
    const name = String(uname ?? '').trim()
    if (name) form.purchaseUserName = name
  } catch {
    /* 无权限或网络失败时保留到货通知冗余的 purchaseUserName */
  }
}

/**
 * 根据到货通知补齐表单；返回预计到货日 YYYY-MM-DD（无则空串）。
 * 编辑质检时勿用预计到货日覆盖已保存的「入库日期」，传 skipDefaultStockInPlanDate: true。
 */
const fillNotice = async (noticeId: string, opts?: { skipDefaultStockInPlanDate?: boolean }): Promise<string> => {
  form.noticeId = noticeId
  if (!noticeId) return Promise.resolve('')
  const notices = await logisticsApi.getArrivalNotices()
  const row = notices.find(x => x.id === noticeId)
  if (!row) return ''
  const firstItem = row.items?.[0]
  const sumItemArrived = Number((row.items || []).reduce((s, x) => s + Number(x.arrivedQty || 0), 0))
  const rq = Number(row.receiveQty ?? 0)
  const eq = Number(row.expectQty ?? 0)
  /** 与到货单表一致：优先明细汇总；否则用行级实收；仍为 0 则用本批通知数量作送检基准 */
  const arrivedTotalQty = Math.round(
    sumItemArrived > 0 ? sumItemArrived : rq > 0 ? rq : eq
  )
  form.noticeCode = row.noticeCode || ''
  form.purchaseOrderCode = row.purchaseOrderCode || ''
  form.materialCode = firstItem?.pn || row.pn || ''
  form.brand = firstItem?.brand || row.brand || ''
  form.vendorName = row.vendorName || ''
  form.purchaseUserName = row.purchaseUserName || ''
  form.purchaseUserId = ''
  await applyPurchaseUserFromPurchaseOrder(row.purchaseOrderId)
  form.stockInQty = arrivedTotalQty
  form.sampleQty = arrivedTotalQty
  form.arrivedTotalQty = arrivedTotalQty
  const exp = (row.expectedArrivalDate || '').trim()
  const expectedYmd = exp.length >= 10 ? exp.slice(0, 10) : ''
  if (!opts?.skipDefaultStockInPlanDate) {
    form.stockInPlanDate = expectedYmd
  }
  return expectedYmd
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
      form.purchaseOrderCode = qc.purchaseOrderCode || ''
      form.qcResult = qc.status === -1 ? 'reject' : qc.status === 10 ? 'partial' : 'pass'
      const passR = Math.round(Number(qc.passQty || 0))
      const rejectR = Math.round(Number(qc.rejectQty || 0))
      form.stockInQty = passR
      form.sampleQty = passR
      form.arrivedTotalQty = passR + rejectR

      // 再补齐到货通知维度的数据（供应商/物料等）；勿用预计到货日覆盖 qcinfo 已保存的入库计划日
      const expectedYmd = await fillNotice(qc.stockInNotifyId, { skipDefaultStockInPlanDate: true })
      form.qcResult = qc.status === -1 ? 'reject' : qc.status === 10 ? 'partial' : 'pass'
      form.stockInQty = Math.round(Number(qc.passQty || 0))
      form.sampleQty = Math.round(Number(qc.passQty || 0))
      const savedYmd =
        qcStockInPlanDateToYmd(qc.stockInPlanDate) || qcStockInPlanDateToYmd(qc.StockInPlanDate)
      form.stockInPlanDate = savedYmd || expectedYmd
    }
    await loadQcDocuments(qcId)
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
    const passQty = Math.round(Number(form.stockInQty || 0))
    const rejectQty = Math.max(0, Math.round(Number(form.arrivedTotalQty || 0)) - passQty)
    const plan = (form.stockInPlanDate || '').trim()
    await logisticsApi.updateQcResult(qcId, {
      result: form.qcResult,
      passQty,
      rejectQty,
      hasStockInPlanDate: true,
      stockInPlanDate: plan ? `${plan}T12:00:00.000Z` : null,
    })
    const pendingFiles: File[] = qcFileList.value
      .filter((f) => f.raw != null)
      .map((f) => f.raw as File)
    if (pendingFiles.length) {
      await uploadPendingQcImages(qcId, pendingFiles)
    }
    await loadQcDocuments(qcId)
    ElMessage.success(isEdit.value ? '质检已更新' : '质检已保存')
    router.push({ name: 'QcList', query: { qcId } })
  } catch (e: any) {
    ElMessage.error(e?.message || (isEdit.value ? '更新质检失败' : '创建质检失败'))
  } finally {
    submitting.value = false
  }
}

const goBack = () => router.back()

onMounted(async () => {
  await Promise.all([loadLogisticsUsers(), loadPageData()])
})

onUnmounted(() => {
  revokeQcPreviewUrls()
})
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
.qc-upload-hint-block {
  margin-bottom: 10px;
}
.qc-upload-hint {
  font-size: 12px;
  color: $text-muted;
  margin: 0 0 6px;
  line-height: 1.55;
  &:last-child { margin-bottom: 0; }
  strong {
    color: $text-secondary;
    font-weight: 600;
  }
}

.qc-form {
  :deep(.el-form-item__label) {
    color: $text-muted !important;
    font-size: 13px;
    white-space: nowrap;
    line-height: 1.4;
    padding-right: 8px;
  }
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

.q-input--readonly {
  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner) {
    cursor: default;
    opacity: 0.92;
    background-color: rgba(255, 255, 255, 0.04) !important;
  }
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
