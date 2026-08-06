<template>
  <div
    class="qc-detail-page"
    v-loading="pageLoading"
    element-loading-background="rgba(10,22,40,0.8)"
  >
    <!-- CaptionBar（《业务详情页面规范》§3 单据类） -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('qcDetail.back') }}
        </button>
        <div class="qc-caption-title-group">
          <div class="caption-avatar-lg">{{ captionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title" :class="{ 'page-title--muted': isEdit && qcRecord?.status === -1 }">
                  {{ pageTitle }}
                </h1>
              </div>
            </div>
            <div class="title-meta title-meta--caption qc-header-meta-row">
              <template v-if="isEdit && qcRecord">
                <el-tag effect="dark" :type="qcStatusTagType(qcRecord.status)" size="small">
                  {{ qcStatusText(qcRecord.status) }}
                </el-tag>
                <el-tag effect="dark" :type="stockInStatusTagType(displayStockInStatus(qcRecord))" size="small">
                  {{ stockInStatusText(displayStockInStatus(qcRecord)) }}
                </el-tag>
              </template>
              <span v-else-if="form.noticeCode" class="qc-caption-meta-text">
                {{ t('qcDetail.meta.notice') }} {{ form.noticeCode }}
              </span>
              <StockBizTypeTag
                v-if="detailStockInType != null"
                biz="in"
                :type="detailStockInType"
                :customs-declaration-id="qcRecord?.customsDeclarationId"
                :customs-declaration-code="qcRecord?.customsDeclarationCode"
              />
            </div>
          </div>
        </div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-secondary btn-close-qc" @click="goBack">
          {{ t('qcDetail.cancel') }}
        </button>
        <button type="button" class="btn-primary" :disabled="submitting" @click="submitQc">
          {{ submitting ? t('qcDetail.saving') : isEdit ? t('qcDetail.update') : t('qcDetail.save') }}
        </button>
      </div>
    </div>

    <div class="detail-content">
      <!-- 供应信息（只读） -->
      <div class="info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('qcDetail.sections.supply') }}</span>
          </div>
          <div v-if="isEdit && qcRecord" class="section-header__meta">
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">{{ t('qcDetail.meta.createDate') }}</span>
              <span class="section-header-meta-item__value">{{ qcCreateDateText }}</span>
            </span>
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">{{ t('qcDetail.meta.createUser') }}</span>
              <span class="section-header-meta-item__value">{{ qcCreateUserText }}</span>
            </span>
          </div>
        </div>
        <div class="info-grid info-grid--inline-labels info-grid--basic">
          <div class="info-item">
            <span class="info-label">{{ t('qcDetail.fields.noticeCode') }}</span>
            <span class="info-value info-value--code">{{ cellText(form.noticeCode) }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('qcDetail.fields.purchaseUser') }}</span>
            <span class="info-value">{{ cellText(form.purchaseUserName) }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('qcDetail.fields.purchaseOrderCode') }}</span>
            <span class="info-value info-value--code">{{ cellText(form.purchaseOrderCode) }}</span>
          </div>
        </div>
        <div class="info-grid info-grid--inline-labels">
          <div class="info-item info-item--span-all">
            <span class="info-label">{{ t('qcDetail.fields.vendor') }}</span>
            <span class="info-value">
              <vendor-name-readonly-text
                :name-zh="form.vendorName"
                :name-en="form.vendorEnglishName"
                :masked="maskPurchaseSensitiveFields"
              />
            </span>
          </div>
          <div v-if="form.noticeRemark?.trim()" class="info-item info-item--span-all">
            <span class="info-label">{{ t('qcDetail.fields.noticeRemark') }}</span>
            <span class="info-value">{{ form.noticeRemark.trim() }}</span>
          </div>
        </div>
      </div>

      <!-- 送货信息 -->
      <div class="info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--amber"></div>
            <span class="section-title">{{ t('qcDetail.sections.delivery') }}</span>
          </div>
        </div>
        <div class="info-section__body">
          <el-form label-width="120px" class="qc-form">
            <el-row :gutter="12">
              <el-col :md="6" :sm="12" :xs="24">
                <el-form-item :label="t('qcDetail.fields.expressNo')">
                  <el-input v-model="form.expressNo" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :md="6" :sm="12" :xs="24">
                <el-form-item :label="t('qcDetail.fields.deliveryMethod')">
                  <el-input v-model="form.deliveryMethod" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :md="6" :sm="12" :xs="24">
                <el-form-item :label="t('qcDetail.fields.expressMethod')">
                  <el-input v-model="form.expressMethod" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :md="6" :sm="12" :xs="24">
                <el-form-item :label="t('qcDetail.fields.arrivalDate')">
                  <el-date-picker
                    v-model="form.arrivalDate"
                    type="date"
                    value-format="YYYY-MM-DD"
                    style="width: 100%"
                    class="q-date"
                  />
                </el-form-item>
              </el-col>
              <el-col :md="6" :sm="12" :xs="24">
                <el-form-item :label="t('qcDetail.fields.stockInPlanDate')">
                  <el-date-picker
                    v-model="form.stockInPlanDate"
                    type="date"
                    value-format="YYYY-MM-DD"
                    :placeholder="t('qcDetail.stockInPlanDatePlaceholder')"
                    style="width: 100%"
                    class="q-date"
                  />
                </el-form-item>
              </el-col>
            </el-row>
          </el-form>
        </div>
      </div>

      <!-- 物料信息 -->
      <div class="info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--green"></div>
            <span class="section-title">{{ t('qcDetail.sections.material') }}</span>
          </div>
        </div>
        <div class="info-section__body">
          <el-form label-width="120px" class="qc-form">
            <el-row :gutter="12">
              <el-col :md="8" :sm="12" :xs="24">
                <el-form-item :label="t('qcDetail.fields.materialCode')">
                  <el-input v-model="form.materialCode" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :md="8" :sm="12" :xs="24">
                <el-form-item :label="t('qcDetail.fields.brand')">
                  <el-input v-model="form.brand" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :md="8" :sm="12" :xs="24">
                <el-form-item :label="t('qcDetail.fields.arrivedTotalQty')">
                  <el-input-number
                    v-model="form.arrivedTotalQty"
                    :min="0"
                    :precision="0"
                    :step="1"
                    step-strictly
                    style="width: 100%"
                    class="q-number"
                  />
                </el-form-item>
              </el-col>
            </el-row>
          </el-form>
        </div>
      </div>

      <!-- 质检信息 -->
      <div class="info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('qcDetail.sections.qcInfo') }}</span>
          </div>
        </div>
        <div class="info-section__body">
          <el-form label-width="120px" class="qc-form">
            <el-row :gutter="12">
              <el-col :md="6" :sm="12" :xs="24">
                <el-form-item :label="t('qcDetail.fields.sampleQty')">
                  <el-input-number
                    v-model="form.sampleQty"
                    :min="0"
                    :precision="0"
                    :step="1"
                    step-strictly
                    style="width: 100%"
                    class="q-number"
                  />
                </el-form-item>
              </el-col>
              <el-col :md="6" :sm="12" :xs="24">
                <el-form-item :label="t('qcDetail.fields.sampleDate')">
                  <el-date-picker
                    v-model="form.sampleDate"
                    type="date"
                    value-format="YYYY-MM-DD"
                    style="width: 100%"
                    class="q-date"
                  />
                </el-form-item>
              </el-col>
              <el-col :md="6" :sm="12" :xs="24">
                <el-form-item :label="t('qcDetail.fields.qcUser')">
                  <el-select
                    v-model="form.qcUserId"
                    filterable
                    clearable
                    :placeholder="t('qcDetail.qcUserPlaceholder')"
                    style="width: 100%"
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
              <el-col :md="6" :sm="12" :xs="24">
                <el-form-item :label="t('qcDetail.fields.qcResult')">
                  <el-select
                    v-model="form.qcResult"
                    style="width: 100%"
                    :class="['q-select', 'qc-result-select', `qc-result-${form.qcResult}`]"
                  >
                    <el-option :label="t('qcDetail.qcResult.pass')" value="pass" />
                    <el-option :label="t('qcDetail.qcResult.partial')" value="partial" />
                    <el-option :label="t('qcDetail.qcResult.reject')" value="reject" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :md="6" :sm="12" :xs="24">
                <el-form-item :label="t('qcDetail.fields.stockInQty')">
                  <el-input-number
                    v-model="form.stockInQty"
                    :min="0"
                    :precision="0"
                    :step="1"
                    step-strictly
                    style="width: 100%"
                    class="q-number"
                  />
                </el-form-item>
              </el-col>
            </el-row>
            <el-form-item :label="t('qcDetail.fields.remark')">
              <el-input v-model="form.remark" type="textarea" :rows="2" class="q-input" />
            </el-form-item>
          </el-form>
        </div>
      </div>

      <!-- 质检图片 -->
      <div class="info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--amber"></div>
            <span class="section-title">{{ t('qcDetail.sections.images') }}</span>
          </div>
        </div>
        <div class="info-section__body">
          <div class="qc-upload-hint-block">
            <p v-if="!currentQcId" class="qc-upload-hint">{{ t('qcDetail.uploadHintCreate') }}</p>
            <p v-else class="qc-upload-hint">{{ t('qcDetail.uploadHintEdit') }}</p>
          </div>
          <el-upload
            class="qc-upload"
            :class="{ 'qc-upload--collapsed': qcImagesCollapsed }"
            action="#"
            list-type="picture-card"
            :auto-upload="false"
            v-model:file-list="qcFileList"
            multiple
            accept="image/jpeg,image/jpg,image/png,image/webp,image/gif"
            :limit="MAX_QC_IMAGES"
            :before-upload="beforeSelectQcImage"
            :before-remove="beforeRemoveQcImage"
            :on-preview="onPreviewQcImage"
            :on-exceed="onExceedQcImages"
          >
            <el-icon><Plus /></el-icon>
          </el-upload>
          <div v-if="qcHiddenImageCount > 0 || qcImagesExpanded" class="qc-upload-more">
            <el-button
              v-if="!qcImagesExpanded && qcHiddenImageCount > 0"
              link
              type="primary"
              @click="qcImagesExpanded = true"
            >
              {{ t('qcDetail.imagesShowMore', { n: qcHiddenImageCount }) }}
            </el-button>
            <el-button
              v-else-if="qcImagesExpanded && qcFileList.length > QC_IMAGES_INITIAL_VISIBLE"
              link
              type="primary"
              @click="qcImagesExpanded = false"
            >
              {{ t('qcDetail.imagesCollapse') }}
            </el-button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { UploadFile, UploadRawFile } from 'element-plus'
import { logisticsApi, type QcInfoDto } from '@/api/logistics'
import { authApi, type SalesUserSelectOption } from '@/api/auth'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { useRoute, useRouter } from 'vue-router'
import { Plus } from '@element-plus/icons-vue'
import apiClient from '@/api/client'
import { documentApi, DOCUMENT_BIZ_TYPE_QC } from '@/api/document'
import {
  QC_IMAGE_UPLOAD_PLACEHOLDER_URL,
  fetchQcDocumentPreviewBlob,
  filterQcImageDocuments,
  resolveUploadDocumentId
} from '@/utils/qcImageDocument'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import VendorNameReadonlyText from '@/components/Vendor/VendorNameReadonlyText.vue'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { qcUploadFilesToBrowserItems } from '@/utils/imageBrowserItems'
import { useImageBrowser } from '@/composables/useImageBrowser'

type QcUploadFile = UploadFile & { documentId?: string; uploadFailReason?: string }

const { t } = useI18n()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { openImageBrowser } = useImageBrowser()
const route = useRoute()
const router = useRouter()
const pageLoading = ref(false)
const submitting = ref(false)
const isEdit = ref(false)
const currentQcId = ref('')
const qcRecord = ref<QcInfoDto | null>(null)
const detailStockInType = ref<number | undefined>(undefined)
const logisticsUserOptions = ref<SalesUserSelectOption[]>([])
const qcFileList = ref<QcUploadFile[]>([])
const qcPreviewBlobUrls: string[] = []

const form = reactive<any>({
  noticeId: '',
  noticeCode: '',
  materialCode: '',
  brand: '',
  vendorName: '',
  vendorEnglishName: '',
  purchaseOrderCode: '',
  purchaseUserId: '',
  purchaseUserName: '',
  noticeRemark: '',
  deliveryMethod: '',
  expressMethod: '',
  expressNo: '',
  arrivalDate: '',
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

const captionAvatarChar = computed(() => {
  const code = isEdit.value ? qcRecord.value?.qcCode?.trim() : form.noticeCode?.trim()
  if (code) return code.slice(-1).toUpperCase()
  return isEdit.value ? 'Q' : '检'
})

const pageTitle = computed(() => {
  if (isEdit.value && qcRecord.value?.qcCode?.trim()) {
    return `${t('qcDetail.captionPrefix')} ${qcRecord.value.qcCode.trim()}`
  }
  return t('qcDetail.createTitle')
})

const qcCreateDateText = computed(() => formatDisplayDate(qcRecord.value?.createTime) || '—')

const qcCreateUserText = computed(() => {
  const name = qcRecord.value?.createUserName ?? qcRecord.value?.CreateUserName
  return String(name ?? '').trim() || '—'
})

function cellText(v: unknown) {
  const s = String(v ?? '').trim()
  return s || '—'
}

function qcStatusText(s: number) {
  const keyMap: Record<number, 'failed' | 'partial' | 'passed'> = {
    [-1]: 'failed',
    10: 'partial',
    100: 'passed'
  }
  const k = keyMap[s]
  return k ? t(`qcList.qcStatus.${k}`) : t('qcList.qcStatus.unknown')
}

function qcStatusTagType(s: number) {
  return ({ [-1]: 'danger', 10: 'warning', 100: 'success' } as Record<number, string>)[s] || 'info'
}

function stockInStatusText(s: number | undefined) {
  const keyMap: Record<number, 'rejected' | 'notStocked' | 'partial' | 'all'> = {
    [-1]: 'rejected',
    1: 'notStocked',
    10: 'partial',
    100: 'all'
  }
  if (s === undefined || s === null) return t('qcList.stockInStatus.unknown')
  const k = keyMap[s]
  return k ? t(`qcList.stockInStatus.${k}`) : t('qcList.stockInStatus.unknown')
}

function stockInStatusTagType(s: number | undefined) {
  return s === undefined || s === null
    ? 'info'
    : ({ [-1]: 'danger', 1: 'info', 10: 'warning', 100: 'success' } as Record<number, string>)[s] || 'info'
}

function displayStockInStatus(row: QcInfoDto) {
  if (row.status === -1) return -1
  if (!row.stockInId) return 1
  return row.stockInStatus
}

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
    const imageDocs = filterQcImageDocuments(docs)
    const list: QcUploadFile[] = []
    let seq = 0
    for (const d of imageDocs) {
      const documentId = resolveUploadDocumentId(d)
      if (!documentId) continue
      const blob = await fetchQcDocumentPreviewBlob(documentId, (url) =>
        apiClient.getBlob(url)
      )
      let url = QC_IMAGE_UPLOAD_PLACEHOLDER_URL
      if (blob) {
        url = URL.createObjectURL(blob)
        qcPreviewBlobUrls.push(url)
      }
      seq += 1
      list.push({
        name: d.originalFileName || `image-${seq}`,
        url,
        uid: Date.now() + seq,
        status: 'success',
        documentId
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
        cancelButtonText: '取消'
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
  const items = qcUploadFilesToBrowserItems(qcFileList.value)
  if (items.length === 0) return
  const idx = qcFileList.value.findIndex((f) => f.uid === uploadFile.uid)
  openImageBrowser({
    items,
    initialIndex: idx >= 0 ? idx : 0,
    title: t('qcDetail.sections.images')
  })
}

const MAX_QC_IMAGES = 100
const MAX_QC_IMAGE_SIZE_MB = 8
/** 详情图片区默认展示张数，超出后「查看剩余 / 收起」 */
const QC_IMAGES_INITIAL_VISIBLE = 10
const qcImagesExpanded = ref(false)
const qcHiddenImageCount = computed(() =>
  Math.max(0, qcFileList.value.length - QC_IMAGES_INITIAL_VISIBLE)
)
const qcImagesCollapsed = computed(
  () => !qcImagesExpanded.value && qcFileList.value.length > QC_IMAGES_INITIAL_VISIBLE
)

watch(
  () => qcFileList.value.length,
  (n) => {
    if (n <= QC_IMAGES_INITIAL_VISIBLE) qcImagesExpanded.value = false
  }
)

type QcImageUploadFailure = { item: QcUploadFile; reason: string }
type QcImageUploadBatchResult = { successCount: number; failed: QcImageUploadFailure[] }

function beforeSelectQcImage(rawFile: UploadRawFile) {
  if (qcFileList.value.length >= MAX_QC_IMAGES) {
    ElMessage.warning(`最多上传 ${MAX_QC_IMAGES} 张图片，当前已达上限`)
    return false
  }
  const maxBytes = MAX_QC_IMAGE_SIZE_MB * 1024 * 1024
  if (rawFile.size > maxBytes) {
    ElMessage.warning(`单张图片不能超过 ${MAX_QC_IMAGE_SIZE_MB}MB，请压缩后再上传`)
    return false
  }
  return true
}

function onExceedQcImages() {
  ElMessage.warning(`最多 ${MAX_QC_IMAGES} 张图片，请删除部分后再添加`)
}

function normalizeQcUploadError(error: unknown): string {
  const msg = getApiErrorMessage(error, '上传失败')
  if (/timeout|超时/i.test(msg)) return '上传超时，请检查网络后重试'
  if (/network error|网络/i.test(msg)) return '网络异常，请稍后重试'
  return msg
}

function qcUploadFileLabel(item: QcUploadFile): string {
  return item.name || item.raw?.name || '未知文件'
}

async function uploadPendingQcImages(qcId: string, pending: QcUploadFile[]): Promise<QcImageUploadBatchResult> {
  const failed: QcImageUploadFailure[] = []
  let successCount = 0
  for (const item of pending) {
    const file = item.raw
    if (!file) continue
    try {
      await documentApi.uploadDocuments(DOCUMENT_BIZ_TYPE_QC, qcId, [file])
      successCount += 1
    } catch (e: unknown) {
      failed.push({ item, reason: normalizeQcUploadError(e) })
    }
  }
  return { successCount, failed }
}

function buildQcUploadResultMessage(result: QcImageUploadBatchResult): string {
  const { successCount, failed } = result
  const failCount = failed.length
  const detailLines = failed.slice(0, 5).map((f) => `「${qcUploadFileLabel(f.item)}」：${f.reason}`)
  const detail =
    failCount <= 5 ? detailLines.join('；') : `${detailLines.join('；')}…等共 ${failCount} 张`
  return [
    `质检主单已保存。图片上传：成功 ${successCount} 张，失败 ${failCount} 张。`,
    failCount ? `失败明细：${detail}。` : '',
    '未成功的图片仍保留在下方列表中，补传后再次点击「保存质检」即可，无需重新选择已成功的图片。'
  ]
    .filter(Boolean)
    .join('')
}

async function refreshQcFileListAfterUpload(qcId: string, failed: QcImageUploadFailure[]) {
  await loadQcDocuments(qcId)
  for (const { item, reason } of failed) {
    const file = item.raw
    if (!file) continue
    let url = item.url
    if (!url?.startsWith('blob:')) {
      url = URL.createObjectURL(file)
      qcPreviewBlobUrls.push(url)
    }
    qcFileList.value.push({
      name: qcUploadFileLabel(item),
      url,
      uid: item.uid,
      status: 'fail',
      raw: file,
      uploadFailReason: reason
    })
  }
}

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

const fillNotice = async (noticeId: string, opts?: { skipDefaultStockInPlanDate?: boolean }): Promise<string> => {
  form.noticeId = noticeId
  if (!noticeId) return Promise.resolve('')
  const { items: noticeRows } = await logisticsApi.getArrivalNotices({
    id: noticeId,
    page: 1,
    pageSize: 1
  })
  const row = noticeRows[0]
  if (!row) return ''
  detailStockInType.value = row.stockInType
  const firstItem = row.items?.[0]
  const sumItemArrived = Number((row.items || []).reduce((s, x) => s + Number(x.arrivedQty || 0), 0))
  const rq = Number(row.receiveQty ?? 0)
  const eq = Number(row.expectQty ?? 0)
  const arrivedTotalQty = Math.round(sumItemArrived > 0 ? sumItemArrived : rq > 0 ? rq : eq)
  form.noticeCode = row.noticeCode || ''
  form.purchaseOrderCode = row.purchaseOrderCode || ''
  form.materialCode = firstItem?.pn || row.pn || ''
  form.brand = firstItem?.brand || row.brand || ''
  form.vendorName = row.vendorName || ''
  form.vendorEnglishName = row.vendorEnglishName || ''
  form.purchaseUserName = row.purchaseUserName || ''
  form.purchaseUserId = ''
  await applyPurchaseUserFromPurchaseOrder(row.purchaseOrderId)
  form.noticeRemark = (row.remark ?? '').trim()
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
    const { items: qcRows } = await logisticsApi.getQcs({ qcId, page: 1, pageSize: 1 })
    const qc = qcRows[0]
    if (qc) {
      qcRecord.value = qc
      detailStockInType.value = qc.stockInType
      form.noticeId = qc.stockInNotifyId || ''
      form.noticeCode = qc.stockInNotifyCode || ''
      form.purchaseOrderCode = qc.purchaseOrderCode || ''
      form.qcResult = qc.status === -1 ? 'reject' : qc.status === 10 ? 'partial' : 'pass'
      const passR = Math.round(Number(qc.passQty || 0))
      const rejectR = Math.round(Number(qc.rejectQty || 0))
      form.stockInQty = passR
      form.sampleQty = passR
      form.arrivedTotalQty = passR + rejectR

      const expectedYmd = await fillNotice(qc.stockInNotifyId, { skipDefaultStockInPlanDate: true })
      form.qcResult = qc.status === -1 ? 'reject' : qc.status === 10 ? 'partial' : 'pass'
      form.stockInQty = Math.round(Number(qc.passQty || 0))
      form.sampleQty = Math.round(Number(qc.passQty || 0))
      form.remark = String(qc.remark ?? '').trim()
      const savedYmd = qcStockInPlanDateToYmd(qc.stockInPlanDate) || qcStockInPlanDateToYmd(qc.StockInPlanDate)
      form.stockInPlanDate = savedYmd || expectedYmd
    }
    await loadQcDocuments(qcId)
    return
  }

  isEdit.value = false
  qcRecord.value = null
  await fillNotice(noticeId)
}

const submitQc = async () => {
  if (submitting.value) return
  if (!form.noticeId) {
    ElMessage.warning(t('qcDetail.messages.noticeMissing'))
    return
  }
  submitting.value = true
  try {
    let qcId = currentQcId.value
    const wasEdit = isEdit.value
    if (!isEdit.value) {
      const qc = await logisticsApi.createQc(form.noticeId)
      qcId = qc.id
      currentQcId.value = qcId
      isEdit.value = true
      qcRecord.value = qc
    }
    const passQty = Math.round(Number(form.stockInQty || 0))
    const rejectQty = Math.max(0, Math.round(Number(form.arrivedTotalQty || 0)) - passQty)
    const plan = (form.stockInPlanDate || '').trim()
    const updated = await logisticsApi.updateQcResult(qcId, {
      result: form.qcResult,
      passQty,
      rejectQty,
      hasStockInPlanDate: true,
      stockInPlanDate: plan ? `${plan}T12:00:00.000Z` : null,
      hasRemark: true,
      remark: (form.remark || '').trim() || null
    })
    qcRecord.value = updated
    if (qcFileList.value.length > MAX_QC_IMAGES) {
      ElMessage.warning(`质检图片最多 ${MAX_QC_IMAGES} 张，当前已选 ${qcFileList.value.length} 张，请删除多余图片`)
      return
    }
    const pendingItems = qcFileList.value.filter((f) => f.raw != null)
    let uploadResult: QcImageUploadBatchResult | null = null
    if (pendingItems.length) {
      uploadResult = await uploadPendingQcImages(qcId, pendingItems)
    }
    if (uploadResult && uploadResult.failed.length > 0) {
      await refreshQcFileListAfterUpload(qcId, uploadResult.failed)
      ElMessage.warning({
        message: buildQcUploadResultMessage(uploadResult),
        duration: 12_000,
        showClose: true
      })
      return
    }
    if (pendingItems.length) {
      await loadQcDocuments(qcId)
    }
    ElMessage.success(wasEdit ? t('qcDetail.messages.updateSuccess') : t('qcDetail.messages.saveSuccess'))
    router.push({ name: 'QcList', query: { qcId } })
  } catch (e: unknown) {
    ElMessage.error(
      getApiErrorMessage(
        e,
        isEdit.value ? t('qcDetail.messages.updateFailed') : t('qcDetail.messages.createFailed')
      )
    )
  } finally {
    submitting.value = false
  }
}

const goBack = () => router.back()

onMounted(async () => {
  pageLoading.value = true
  try {
    await Promise.all([loadLogisticsUsers(), loadPageData()])
  } finally {
    pageLoading.value = false
  }
})

onUnmounted(() => {
  revokeQcPreviewUrls()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.qc-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 24px;
}

.header-left {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  min-width: 0;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-shrink: 0;
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
  flex-shrink: 0;

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.qc-caption-title-group {
  display: flex;
  align-items: flex-start;
  gap: 14px;
  min-width: 0;
}

.caption-avatar-lg {
  width: 48px;
  height: 48px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.3), rgba(0, 212, 255, 0.2));
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
  color: $cyan-primary;
  flex-shrink: 0;
}

.page-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 6px;
}

.page-title-with-icons {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  min-width: 0;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0;

  &--muted {
    opacity: 0.55;
  }
}

.title-meta--caption {
  margin-top: 4px;
}

.qc-header-meta-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  min-height: 28px;
}

.qc-caption-meta-text {
  font-size: 12px;
  color: $text-muted;
}

.btn-secondary,
.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-secondary {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  color: $text-secondary;

  &:hover {
    background: rgba(255, 255, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.25);
  }
}

.btn-close-qc {
  color: $color-amber;
  border: none;
  background: transparent;

  &:hover {
    background: rgba(255, 255, 255, 0.08);
    border: none;
  }
}

.btn-primary {
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  color: #fff;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.detail-content {
  min-height: 200px;
}

.info-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.info-section__body {
  padding: 16px 20px 20px;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: var(--crm-detail-section-header-bg);
}

.section-header__main {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.section-header__meta {
  display: flex;
  align-items: center;
  gap: 20px;
  flex-shrink: 0;
  margin-left: auto;
}

.section-header-meta-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  white-space: nowrap;

  &__label {
    color: $text-muted;

    &::after {
      content: '：';
    }
  }

  &__value {
    color: $text-secondary;
  }
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.6);
  }

  &--amber {
    background: $color-amber;
    box-shadow: 0 0 6px rgba(201, 154, 69, 0.6);
  }

  &--green {
    background: $color-mint-green;
    box-shadow: 0 0 6px rgba(70, 191, 145, 0.6);
  }
}

.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-primary;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
  border-right: 1px solid rgba(255, 255, 255, 0.04);

  &:nth-child(3n) {
    border-right: none;
  }
}

.info-grid:not(.info-grid--inline-labels) .info-item {
  padding: 16px 20px;
}

.info-grid--inline-labels .info-item {
  flex-direction: row;
  align-items: center;
  gap: 8px;

  .info-label {
    flex-shrink: 0;
    white-space: nowrap;
    text-transform: none;
    letter-spacing: 0;
    font-size: 12px;

    &::after {
      content: '：';
    }
  }

  .info-value {
    flex: 1;
    min-width: 0;
    word-break: break-word;
  }
}

.info-grid--basic .info-item:nth-child(3n) {
  border-right: none;
}

.info-grid--inline-labels .info-item--span-all {
  grid-column: 1 / -1;
  border-right: none;
}

.info-label {
  font-size: 11px;
  color: $text-muted;
}

.info-value {
  font-size: 13px;
  color: $text-secondary;

  &--code {
    color: $color-ice-blue;
  }
}

.qc-upload-hint-block {
  margin-bottom: 10px;
}

.qc-upload-hint {
  font-size: 12px;
  color: $text-muted;
  margin: 0;
  line-height: 1.55;
}

.qc-upload-more {
  margin-top: 10px;
}

.qc-upload {
  /* 折叠：仅隐藏第 11 张及以后的已选缩略图，保留「+」上传入口 */
  &--collapsed {
    :deep(.el-upload-list--picture-card > .el-upload-list__item:nth-child(n + 11)) {
      display: none;
    }
  }

  :deep(.el-upload-list--picture-card .el-upload-list__item-actions) {
    position: absolute;
    inset: 0;
  }

  :deep(.el-upload-list--picture-card .el-upload-list__item-preview) {
    position: absolute;
    inset: 0;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    color: #fff !important;
  }

  :deep(.el-upload-list--picture-card .el-upload-list__item-delete) {
    position: absolute;
    right: 10px !important;
    bottom: 10px !important;
    top: auto !important;
    left: auto !important;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 22px;
    height: 22px;
    border-radius: 50%;
    background: rgba(214, 48, 49, 0.92);
    color: #fff !important;
  }

  :deep(.el-upload-list--picture-card .el-upload-list__item-delete .el-icon) {
    font-size: 12px;
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

  :deep(.el-input__inner) {
    color: $text-primary !important;
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
