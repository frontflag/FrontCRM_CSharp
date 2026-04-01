<template>
  <div class="company-info-page">
    <div class="page-header">
      <div class="header-left">
        <h2 class="page-title">公司信息</h2>
        <p class="page-sub">维护公司主体、银行、Logo、印章、仓库及系统发信（SMTP）参数；多组资料可设默认与启用状态。</p>
      </div>
    </div>

    <div class="settings-body">
      <div class="settings-nav">
        <div
          v-for="item in navItems"
          :key="item.key"
          class="nav-item"
          :class="{ active: activeNav === item.key }"
          @click="activeNav = item.key"
        >
          <el-icon class="nav-icon"><component :is="item.icon" /></el-icon>
          <span>{{ item.label }}</span>
        </div>
      </div>

      <div class="settings-content" v-loading="loading">
        <!-- 公司基础信息 -->
        <div v-show="activeNav === 'basic'" class="form-section">
          <div class="section-head">
            <div class="section-head__left">
              <div class="section-title"><span class="title-bar"></span>公司基础信息</div>
              <p class="section-hint">可维护多组；「默认」整站仅一组；禁用的组不参与默认业务引用。</p>
            </div>
            <el-button type="primary" class="save-all-btn" :loading="saving" @click="saveAll">全部保存</el-button>
          </div>

          <div v-for="(row, idx) in basicInfos" :key="row.id" class="group-card">
            <div class="group-card__head">
              <span class="group-card__title">公司资料 #{{ idx + 1 }}</span>
              <div class="group-card__actions">
                <el-checkbox
                  :model-value="row.isDefault"
                  @update:model-value="(on: boolean) => toggleDefault(basicInfos, row, on)"
                >
                  默认
                </el-checkbox>
                <span class="switch-label">启用</span>
                <el-switch v-model="row.enabled" />
              </div>
            </div>
            <el-form label-width="120px" class="settings-form" :model="row">
              <el-row :gutter="16">
                <el-col :span="12">
                  <el-form-item label="公司名称"><el-input v-model="row.companyName" placeholder="公司名称" /></el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="纳税人识别号"><el-input v-model="row.taxId" placeholder="税号" /></el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="法人"><el-input v-model="row.legalPerson" placeholder="法人" /></el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="邮政编码"><el-input v-model="row.postalCode" placeholder="邮编" /></el-form-item>
                </el-col>
                <el-col :span="24">
                  <el-form-item label="公司地址"><el-input v-model="row.address" placeholder="地址" /></el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="公司电话"><el-input v-model="row.phone" placeholder="电话" /></el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="公司传真"><el-input v-model="row.fax" placeholder="传真" /></el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="公司邮箱"><el-input v-model="row.email" placeholder="邮箱" /></el-form-item>
                </el-col>
              </el-row>
            </el-form>
            <div class="group-card__footer">
              <el-button
                class="group-mini-btn"
                circle
                type="primary"
                plain
                title="在本组下方新增一组"
                @click="insertBasicAfter(idx)"
              >
                <el-icon><Plus /></el-icon>
              </el-button>
              <el-button
                class="group-mini-btn group-mini-btn--minus"
                circle
                plain
                title="删除本组"
                @click="removeBasicAt(idx)"
              >
                <el-icon><Minus /></el-icon>
              </el-button>
            </div>
          </div>
        </div>

        <!-- 公司银行信息 -->
        <div v-show="activeNav === 'bank'" class="form-section">
          <div class="section-head">
            <div class="section-head__left">
              <div class="section-title"><span class="title-bar"></span>公司银行信息</div>
              <p class="section-hint">币别：RMB / USD / EUR；银行类型：人民币账户 / 外币账户；用途：付款 / 收款。</p>
            </div>
            <el-button type="primary" class="save-all-btn" :loading="saving" @click="saveAll">全部保存</el-button>
          </div>

          <div v-for="(row, idx) in bankInfos" :key="row.id" class="group-card">
            <div class="group-card__head">
              <span class="group-card__title">银行账户 #{{ idx + 1 }}</span>
              <div class="group-card__actions">
                <el-checkbox
                  :model-value="row.isDefault"
                  @update:model-value="(on: boolean) => toggleDefault(bankInfos, row, on)"
                >
                  默认
                </el-checkbox>
                <span class="switch-label">启用</span>
                <el-switch v-model="row.enabled" />
              </div>
            </div>
            <el-form label-width="120px" class="settings-form" :model="row">
              <el-row :gutter="16">
                <el-col :span="12">
                  <el-form-item label="银行名称"><el-input v-model="row.bankName" /></el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="账户名"><el-input v-model="row.accountName" /></el-form-item>
                </el-col>
                <el-col :span="24">
                  <el-form-item label="银行地址"><el-input v-model="row.bankAddress" /></el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="SWIFT">
                    <template #label><span title="国际银行代码">SWIFT</span></template>
                    <el-input v-model="row.swift" placeholder="国际银行代码" />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="IBAN">
                    <template #label><span title="国际银行账号">IBAN</span></template>
                    <el-input v-model="row.iban" placeholder="国际银行账号" />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="联行号"><el-input v-model="row.bankCode" /></el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="币别">
                    <el-select v-model="row.currency" style="width: 100%">
                      <el-option label="RMB" value="RMB" />
                      <el-option label="USD" value="USD" />
                      <el-option label="EUR" value="EUR" />
                    </el-select>
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="银行类型">
                    <el-select v-model="row.bankType" style="width: 100%">
                      <el-option label="人民币账户" value="rmb" />
                      <el-option label="外币账户" value="foreign" />
                    </el-select>
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="用途类型">
                    <el-select v-model="row.purposeType" style="width: 100%">
                      <el-option label="付款" value="payment" />
                      <el-option label="收款" value="receipt" />
                    </el-select>
                  </el-form-item>
                </el-col>
                <el-col :span="24">
                  <el-form-item label="备注"><el-input v-model="row.remark" type="textarea" :rows="2" /></el-form-item>
                </el-col>
              </el-row>
            </el-form>
            <div class="group-card__footer">
              <el-button
                class="group-mini-btn"
                circle
                type="primary"
                plain
                title="在本组下方新增一组"
                @click="insertBankAfter(idx)"
              >
                <el-icon><Plus /></el-icon>
              </el-button>
              <el-button
                class="group-mini-btn group-mini-btn--minus"
                circle
                plain
                title="删除本组"
                @click="removeBankAt(idx)"
              >
                <el-icon><Minus /></el-icon>
              </el-button>
            </div>
          </div>
        </div>

        <!-- 公司 Logo -->
        <div v-show="activeNav === 'logo'" class="form-section">
          <div class="section-head">
            <div class="section-head__left">
              <div class="section-title"><span class="title-bar"></span>公司 Logo</div>
              <p class="section-hint">
                可维护多组 Logo；标记为「默认」且已保存文件的组会用于采购订单报表顶栏。「启用」不影响报表使用该默认 Logo；上传后务必点击「全部保存」，否则报表中仍无文件。
              </p>
            </div>
            <el-button type="primary" class="save-all-btn" :loading="saving" @click="saveAll">全部保存</el-button>
          </div>

          <div v-for="(row, idx) in logos" :key="row.id" class="group-card">
            <div class="group-card__head">
              <span class="group-card__title">Logo #{{ idx + 1 }}</span>
              <div class="group-card__actions">
                <el-checkbox
                  :model-value="row.isDefault"
                  @update:model-value="(on: boolean) => toggleDefault(logos, row, on)"
                >
                  默认
                </el-checkbox>
                <span class="switch-label">启用</span>
                <el-switch v-model="row.enabled" />
              </div>
            </div>
            <el-form label-width="120px" class="settings-form" :model="row">
              <el-row :gutter="16">
                <el-col :span="24">
                  <el-form-item label="Logo 名称"><el-input v-model="row.logoName" placeholder="如 主站横版 Logo" /></el-form-item>
                </el-col>
                <el-col :span="24">
                  <el-form-item label="Logo 文件">
                    <div class="upload-row">
                      <el-upload
                        :show-file-list="false"
                        accept="image/*,.svg"
                        :http-request="(opt: UploadRequestOptions) => onLogoUpload(row, opt)"
                      >
                        <el-button size="small" type="primary" plain>上传 Logo 文件</el-button>
                      </el-upload>
                      <span v-if="row.fileName" class="file-hint">{{ row.fileName }}</span>
                      <a
                        v-if="row.documentId"
                        href="#"
                        class="preview-link"
                        @click.prevent="openPreviewNewTab(row.documentId)"
                      >新窗口预览</a>
                    </div>
                    <div
                      v-if="row.documentId && previewByDocId[row.documentId]"
                      class="asset-preview"
                    >
                      <img
                        v-if="previewByDocId[row.documentId].kind === 'image'"
                        :src="previewByDocId[row.documentId].url"
                        alt=""
                        class="asset-preview__img"
                      />
                      <div v-else-if="previewByDocId[row.documentId].kind === 'pdf'" class="asset-preview__pdf">
                        <span class="asset-preview__pdf-label">PDF</span>
                        <a
                          href="#"
                          class="preview-link"
                          @click.prevent="openPreviewNewTab(row.documentId)"
                        >新窗口打开</a>
                      </div>
                      <div v-else class="asset-preview__other">已上传（非图片）</div>
                    </div>
                  </el-form-item>
                </el-col>
              </el-row>
            </el-form>
            <div class="group-card__footer">
              <el-button
                class="group-mini-btn"
                circle
                type="primary"
                plain
                title="在本组下方新增一组"
                @click="insertLogoAfter(idx)"
              >
                <el-icon><Plus /></el-icon>
              </el-button>
              <el-button
                class="group-mini-btn group-mini-btn--minus"
                circle
                plain
                title="删除本组"
                @click="removeLogoAt(idx)"
              >
                <el-icon><Minus /></el-icon>
              </el-button>
            </div>
          </div>
        </div>

        <!-- 公司印章 -->
        <div v-show="activeNav === 'seal'" class="form-section">
          <div class="section-head">
            <div class="section-head__left">
              <div class="section-title"><span class="title-bar"></span>公司印章</div>
              <p class="section-hint">可维护多组印章；上传文件后请先保存。</p>
            </div>
            <el-button type="primary" class="save-all-btn" :loading="saving" @click="saveAll">全部保存</el-button>
          </div>

          <div v-for="(row, idx) in seals" :key="row.id" class="group-card">
            <div class="group-card__head">
              <span class="group-card__title">印章 #{{ idx + 1 }}</span>
              <div class="group-card__actions">
                <el-checkbox
                  :model-value="row.isDefault"
                  @update:model-value="(on: boolean) => toggleDefault(seals, row, on)"
                >
                  默认
                </el-checkbox>
                <span class="switch-label">启用</span>
                <el-switch v-model="row.enabled" />
              </div>
            </div>
            <el-form label-width="120px" class="settings-form" :model="row">
              <el-row :gutter="16">
                <el-col :span="12">
                  <el-form-item label="印章名称"><el-input v-model="row.sealName" /></el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="使用场景"><el-input v-model="row.useScene" /></el-form-item>
                </el-col>
                <el-col :span="24">
                  <el-form-item label="印章文件">
                    <div class="upload-row">
                      <el-upload
                        :show-file-list="false"
                        accept="image/*,.pdf"
                        :http-request="(opt: UploadRequestOptions) => onSealUpload(row, opt)"
                      >
                        <el-button size="small" type="primary" plain>上传印章文件</el-button>
                      </el-upload>
                      <span v-if="row.fileName" class="file-hint">{{ row.fileName }}</span>
                      <a
                        v-if="row.documentId"
                        href="#"
                        class="preview-link"
                        @click.prevent="openPreviewNewTab(row.documentId)"
                      >新窗口预览</a>
                    </div>
                    <div
                      v-if="row.documentId && previewByDocId[row.documentId]"
                      class="asset-preview"
                    >
                      <img
                        v-if="previewByDocId[row.documentId].kind === 'image'"
                        :src="previewByDocId[row.documentId].url"
                        alt=""
                        class="asset-preview__img"
                      />
                      <div v-else-if="previewByDocId[row.documentId].kind === 'pdf'" class="asset-preview__pdf">
                        <span class="asset-preview__pdf-label">PDF</span>
                        <a
                          href="#"
                          class="preview-link"
                          @click.prevent="openPreviewNewTab(row.documentId)"
                        >新窗口打开</a>
                      </div>
                      <div v-else class="asset-preview__other">已上传（非图片）</div>
                    </div>
                  </el-form-item>
                </el-col>
              </el-row>
            </el-form>
            <div class="group-card__footer">
              <el-button
                class="group-mini-btn"
                circle
                type="primary"
                plain
                title="在本组下方新增一组"
                @click="insertSealAfter(idx)"
              >
                <el-icon><Plus /></el-icon>
              </el-button>
              <el-button
                class="group-mini-btn group-mini-btn--minus"
                circle
                plain
                title="删除本组"
                @click="removeSealAt(idx)"
              >
                <el-icon><Minus /></el-icon>
              </el-button>
            </div>
          </div>
        </div>

        <!-- 公司仓库信息 -->
        <div v-show="activeNav === 'warehouse'" class="form-section">
          <div class="section-head">
            <div class="section-head__left">
              <div class="section-title"><span class="title-bar"></span>公司仓库信息</div>
              <p class="section-hint">可维护多组仓库联系信息。</p>
            </div>
            <el-button type="primary" class="save-all-btn" :loading="saving" @click="saveAll">全部保存</el-button>
          </div>

          <div v-for="(row, idx) in warehouses" :key="row.id" class="group-card">
            <div class="group-card__head">
              <span class="group-card__title">仓库 #{{ idx + 1 }}</span>
              <div class="group-card__actions">
                <el-checkbox
                  :model-value="row.isDefault"
                  @update:model-value="(on: boolean) => toggleDefault(warehouses, row, on)"
                >
                  默认
                </el-checkbox>
                <span class="switch-label">启用</span>
                <el-switch v-model="row.enabled" />
              </div>
            </div>
            <el-form label-width="120px" class="settings-form" :model="row">
              <el-row :gutter="16">
                <el-col :span="12">
                  <el-form-item label="仓库名称"><el-input v-model="row.warehouseName" /></el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="联系人"><el-input v-model="row.contactName" /></el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="联系人电话"><el-input v-model="row.contactPhone" /></el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="工作时间"><el-input v-model="row.workHours" placeholder="如 9:00-18:00" /></el-form-item>
                </el-col>
                <el-col :span="24">
                  <el-form-item label="仓库地址"><el-input v-model="row.address" /></el-form-item>
                </el-col>
              </el-row>
            </el-form>
            <div class="group-card__footer">
              <el-button
                class="group-mini-btn"
                circle
                type="primary"
                plain
                title="在本组下方新增一组"
                @click="insertWarehouseAfter(idx)"
              >
                <el-icon><Plus /></el-icon>
              </el-button>
              <el-button
                class="group-mini-btn group-mini-btn--minus"
                circle
                plain
                title="删除本组"
                @click="removeWarehouseAt(idx)"
              >
                <el-icon><Minus /></el-icon>
              </el-button>
            </div>
          </div>
        </div>

        <!-- 公司邮箱（SMTP 系统发信） -->
        <div v-show="activeNav === 'email'" class="form-section">
          <div class="section-head">
            <div class="section-head__left">
              <div class="section-title"><span class="title-bar"></span>公司邮箱</div>
              <p class="section-hint">
                用于采购订单报表「发送邮件」等系统发信。发信仅读取此处已保存的配置，不再使用 appsettings。
              </p>
            </div>
            <el-button type="primary" class="save-all-btn" :loading="saving" @click="saveAll">全部保存</el-button>
          </div>

          <div class="group-card group-card--single">
            <el-form label-width="140px" class="settings-form" :model="smtpEmail">
              <el-row :gutter="16">
                <el-col :span="24">
                  <el-form-item label="启用系统发信">
                    <el-switch v-model="smtpEmail.enabled" />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="SMTP 服务器">
                    <el-input v-model="smtpEmail.smtpHost" placeholder="如 smtp.example.com" clearable />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="SMTP 端口">
                    <el-input-number
                      v-model="smtpEmail.smtpPort"
                      :min="1"
                      :max="65535"
                      controls-position="right"
                      style="width: 100%"
                    />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="SMTP 账号">
                    <el-input v-model="smtpEmail.user" placeholder="可选" clearable />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="SMTP 密码/授权码">
                    <el-input
                      :model-value="smtpPasswordUi"
                      :type="smtpPasswordMaskActive ? 'text' : 'password'"
                      :show-password="!smtpPasswordMaskActive"
                      placeholder="留空表示保留已保存的密码"
                      :clearable="!smtpPasswordMaskActive"
                      autocomplete="new-password"
                      @update:model-value="onSmtpPasswordInput"
                      @focus="onSmtpPasswordFocus"
                    />
                  </el-form-item>
                </el-col>
                <el-col v-if="smtpEmail.passwordSet" :span="24">
                  <el-alert type="info" :closable="false" show-icon>
                    已保存的授权码以 ****** 显示；修改时请聚焦输入框后输入新授权码；不修改则直接保存即可。
                  </el-alert>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="发件人邮箱">
                    <el-input v-model="smtpEmail.fromAddress" placeholder="发件人地址" clearable />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="发件人显示名">
                    <el-input v-model="smtpEmail.fromName" placeholder="如 FrontCRM" clearable />
                  </el-form-item>
                </el-col>
                <el-col :span="24">
                  <el-form-item label="使用 TLS (STARTTLS)">
                    <el-switch v-model="smtpEmail.useSsl" />
                    <span class="form-item-hint">常见 587 端口开启；若使用 465 SSL 需按邮箱服务商说明调整服务器与端口。</span>
                  </el-form-item>
                </el-col>
              </el-row>
            </el-form>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, type Ref } from 'vue'
import { ElMessage } from 'element-plus'
import type { UploadRequestOptions } from 'element-plus'
import { OfficeBuilding, Wallet, Picture, PictureFilled, Location, Promotion, Plus, Minus } from '@element-plus/icons-vue'
import {
  fetchCompanyProfile,
  saveCompanyProfile,
  type CompanyBasicRow,
  type CompanyBankRow,
  type CompanyLogoRow,
  type CompanySealRow,
  type CompanyWarehouseRow,
  type CompanySmtpEmailSettings
} from '@/api/companyProfile'
import { documentApi } from '@/api/document'
import apiClient from '@/api/client'
import { getApiErrorMessage } from '@/utils/apiError'

type AssetPreview = { url: string; kind: 'image' | 'pdf' | 'other' }

const activeNav = ref<'basic' | 'bank' | 'logo' | 'seal' | 'warehouse' | 'email'>('basic')
const loading = ref(false)
const saving = ref(false)

const basicInfos = ref<CompanyBasicRow[]>([])
const bankInfos = ref<CompanyBankRow[]>([])
const logos = ref<CompanyLogoRow[]>([])
const seals = ref<CompanySealRow[]>([])
const warehouses = ref<CompanyWarehouseRow[]>([])

const previewByDocId = ref<Record<string, AssetPreview>>({})

function revokeAllAssetPreviews() {
  for (const e of Object.values(previewByDocId.value)) {
    if (e?.url) URL.revokeObjectURL(e.url)
  }
  previewByDocId.value = {}
}

async function loadPreviewForDocument(docId: string) {
  const prev = previewByDocId.value[docId]
  if (prev?.url) URL.revokeObjectURL(prev.url)
  const cleared = { ...previewByDocId.value }
  delete cleared[docId]
  previewByDocId.value = cleared
  try {
    const blob = await apiClient.getBlob(`/api/v1/documents/${docId}/download`)
    if (!blob.size) return
    const url = URL.createObjectURL(blob)
    const kind: AssetPreview['kind'] = blob.type.startsWith('image/')
      ? 'image'
      : blob.type === 'application/pdf'
        ? 'pdf'
        : 'other'
    previewByDocId.value = { ...previewByDocId.value, [docId]: { url, kind } }
  } catch {
    /* 无权限或文件缺失 */
  }
}

async function refreshAssetPreviews() {
  revokeAllAssetPreviews()
  const ids = new Set<string>()
  logos.value.forEach((r) => {
    if (r.documentId) ids.add(r.documentId)
  })
  seals.value.forEach((r) => {
    if (r.documentId) ids.add(r.documentId)
  })
  await Promise.all([...ids].map((id) => loadPreviewForDocument(id)))
}

async function openPreviewNewTab(documentId: string) {
  try {
    await documentApi.openPreviewInNewTab(documentId)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '预览失败'))
  }
}

function emptySmtp(): CompanySmtpEmailSettings {
  return {
    enabled: false,
    smtpHost: '',
    smtpPort: 587,
    user: '',
    password: '',
    fromAddress: '',
    fromName: 'FrontCRM',
    useSsl: true,
    passwordSet: false
  }
}

const smtpEmail = ref<CompanySmtpEmailSettings>(emptySmtp())
/** 已保存过密码时占位显示 ******，聚焦后进入真实编辑 */
const smtpPasswordMaskActive = ref(false)

const smtpPasswordUi = computed(() => {
  if (smtpPasswordMaskActive.value) return '******'
  return smtpEmail.value.password
})

function onSmtpPasswordFocus() {
  if (smtpPasswordMaskActive.value) {
    smtpPasswordMaskActive.value = false
    smtpEmail.value.password = ''
  }
}

function onSmtpPasswordInput(v: string) {
  if (smtpPasswordMaskActive.value) return
  smtpEmail.value.password = v
}

const navItems = [
  { key: 'basic' as const, label: '公司基础信息', icon: OfficeBuilding },
  { key: 'bank' as const, label: '公司银行信息', icon: Wallet },
  { key: 'logo' as const, label: '公司Logo', icon: Picture },
  { key: 'seal' as const, label: '公司印章', icon: PictureFilled },
  { key: 'warehouse' as const, label: '公司仓库信息', icon: Location },
  { key: 'email' as const, label: '公司邮箱', icon: Promotion }
]

/** HTTP 非安全上下文中无 crypto.randomUUID（仅 HTTPS/localhost 可用），需降级。 */
function newId(): string {
  const c = globalThis.crypto
  if (c && typeof c.randomUUID === 'function') {
    try {
      return c.randomUUID()
    } catch {
      /* 部分环境会抛错 */
    }
  }
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (ch) => {
    const r = (Math.random() * 16) | 0
    const v = ch === 'x' ? r : (r & 0x3) | 0x8
    return v.toString(16)
  })
}

function emptyBasic(): CompanyBasicRow {
  return {
    id: newId(),
    isDefault: false,
    enabled: true,
    companyName: '',
    taxId: '',
    legalPerson: '',
    address: '',
    postalCode: '',
    phone: '',
    fax: '',
    email: ''
  }
}

function emptyBank(): CompanyBankRow {
  return {
    id: newId(),
    isDefault: false,
    enabled: true,
    bankName: '',
    accountName: '',
    bankAddress: '',
    swift: '',
    iban: '',
    bankCode: '',
    currency: 'RMB',
    bankType: 'rmb',
    purposeType: 'payment',
    remark: ''
  }
}

function emptyLogo(): CompanyLogoRow {
  return {
    id: newId(),
    isDefault: false,
    enabled: true,
    logoName: '',
    documentId: undefined,
    fileName: undefined
  }
}

function emptySeal(): CompanySealRow {
  return {
    id: newId(),
    isDefault: false,
    enabled: true,
    sealName: '',
    useScene: '',
    documentId: undefined,
    fileName: undefined
  }
}

function emptyWarehouse(): CompanyWarehouseRow {
  return {
    id: newId(),
    isDefault: false,
    enabled: true,
    warehouseName: '',
    address: '',
    contactName: '',
    contactPhone: '',
    workHours: ''
  }
}

function setDefaultById<T extends { id: string; isDefault: boolean }>(list: T[], id: string) {
  list.forEach((r) => {
    r.isDefault = r.id === id
  })
}

function toggleDefault<T extends { id: string; isDefault: boolean }>(list: T[], row: T, on: boolean) {
  if (on) setDefaultById(list, row.id)
  else row.isDefault = false
}

/** 无数据时返回一组空记录（并设为默认）。 */
function normalizeGroups<T extends { id: string; isDefault: boolean }>(
  rows: T[] | undefined | null,
  factory: () => T
): T[] {
  if (rows && rows.length > 0) return rows
  const r = factory()
  r.isDefault = true
  return [r]
}

/** 删除指定索引的一组；至少保留一组。 */
function removeGroupAt<T extends { id: string; isDefault: boolean }>(list: Ref<T[]>, index: number) {
  const arr = list.value
  if (index < 0 || index >= arr.length) return
  if (arr.length <= 1) {
    ElMessage.warning('至少保留一组')
    return
  }
  const wasDefault = arr[index].isDefault
  arr.splice(index, 1)
  if (wasDefault && arr.length) {
    arr[0].isDefault = true
  }
}

function insertBasicAfter(index: number) {
  basicInfos.value.splice(index + 1, 0, emptyBasic())
}
function removeBasicAt(index: number) {
  removeGroupAt(basicInfos, index)
}

function insertBankAfter(index: number) {
  bankInfos.value.splice(index + 1, 0, emptyBank())
}
function removeBankAt(index: number) {
  removeGroupAt(bankInfos, index)
}

function insertLogoAfter(index: number) {
  logos.value.splice(index + 1, 0, emptyLogo())
}
function removeLogoAt(index: number) {
  removeGroupAt(logos, index)
}

function insertSealAfter(index: number) {
  seals.value.splice(index + 1, 0, emptySeal())
}
function removeSealAt(index: number) {
  removeGroupAt(seals, index)
}

function insertWarehouseAfter(index: number) {
  warehouses.value.splice(index + 1, 0, emptyWarehouse())
}
function removeWarehouseAt(index: number) {
  removeGroupAt(warehouses, index)
}

async function onLogoUpload(row: CompanyLogoRow, opt: UploadRequestOptions) {
  const file = opt.file as File
  try {
    const docs = await documentApi.uploadDocuments('COMPANY_LOGO', row.id, [file])
    const d = docs[0]
    if (d) {
      row.documentId = d.id
      row.fileName = d.originalFileName
      ElMessage.success('上传成功')
      await refreshAssetPreviews()
    }
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '上传失败'))
  }
}

async function onSealUpload(row: CompanySealRow, opt: UploadRequestOptions) {
  const file = opt.file as File
  try {
    const docs = await documentApi.uploadDocuments('COMPANY_SEAL', row.id, [file])
    const d = docs[0]
    if (d) {
      row.documentId = d.id
      row.fileName = d.originalFileName
      ElMessage.success('上传成功')
      await refreshAssetPreviews()
    }
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '上传失败'))
  }
}

function bundle() {
  return {
    basicInfos: basicInfos.value,
    bankInfos: bankInfos.value,
    logos: logos.value,
    seals: seals.value,
    warehouses: warehouses.value,
    smtpEmail: {
      enabled: smtpEmail.value.enabled,
      smtpHost: smtpEmail.value.smtpHost,
      smtpPort: smtpEmail.value.smtpPort,
      user: smtpEmail.value.user,
      password: smtpEmail.value.password,
      fromAddress: smtpEmail.value.fromAddress,
      fromName: smtpEmail.value.fromName,
      useSsl: smtpEmail.value.useSsl
    }
  }
}

async function load() {
  loading.value = true
  try {
    const data = await fetchCompanyProfile()
    basicInfos.value = normalizeGroups(data.basicInfos, emptyBasic)
    bankInfos.value = normalizeGroups(data.bankInfos, emptyBank)
    logos.value = normalizeGroups(data.logos, emptyLogo)
    seals.value = normalizeGroups(data.seals, emptySeal)
    warehouses.value = normalizeGroups(data.warehouses, emptyWarehouse)
    const se = data.smtpEmail
    const pwdSet = !!(se && se.passwordSet)
    smtpEmail.value = {
      ...emptySmtp(),
      ...(se || {}),
      password: '',
      smtpPort:
        se && typeof se.smtpPort === 'number' && se.smtpPort >= 1 && se.smtpPort <= 65535
          ? se.smtpPort
          : 587,
      fromName: se?.fromName?.trim() ? se.fromName : 'FrontCRM',
      passwordSet: pwdSet
    }
    smtpPasswordMaskActive.value = pwdSet
    await refreshAssetPreviews()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '加载失败'))
    basicInfos.value = normalizeGroups([], emptyBasic)
    bankInfos.value = normalizeGroups([], emptyBank)
    logos.value = normalizeGroups([], emptyLogo)
    seals.value = normalizeGroups([], emptySeal)
    warehouses.value = normalizeGroups([], emptyWarehouse)
    smtpEmail.value = emptySmtp()
    smtpPasswordMaskActive.value = false
    revokeAllAssetPreviews()
  } finally {
    loading.value = false
  }
}

async function saveAll() {
  saving.value = true
  try {
    await saveCompanyProfile(bundle())
    ElMessage.success('已保存')
    await load()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '保存失败'))
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  load()
})

onUnmounted(() => {
  revokeAllAssetPreviews()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.company-info-page {
  padding: 20px;
  min-height: 100%;
}

.page-header {
  margin-bottom: 20px;
  .page-title {
    font-size: 18px;
    font-weight: 600;
    color: $text-primary;
    margin: 0 0 6px;
  }
  .page-sub {
    margin: 0;
    font-size: 13px;
    color: $text-muted;
    line-height: 1.5;
  }
}

.settings-body {
  display: flex;
  gap: 16px;
  align-items: flex-start;
}

.settings-nav {
  width: 200px;
  flex-shrink: 0;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 8px;

  .nav-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 14px;
    border-radius: 6px;
    cursor: pointer;
    color: $text-muted;
    font-size: 13px;
    transition: all 0.2s;

    .nav-icon {
      font-size: 16px;
    }

    &:hover {
      background: rgba(0, 212, 255, 0.06);
      color: $text-secondary;
    }

    &.active {
      background: rgba(0, 212, 255, 0.18);
      color: $cyan-primary;
      font-weight: 500;
    }
  }
}

.settings-content {
  flex: 1;
  min-width: 0;
}

.form-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 20px 24px;
}

.section-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.section-head__left {
  flex: 1;
  min-width: 0;
}

.save-all-btn {
  flex-shrink: 0;
  margin-top: 2px;
}

.form-item-hint {
  margin-left: 12px;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.4;
}

.group-card--single {
  max-width: 920px;
}

.group-card__footer {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 6px;
  margin-top: 10px;
  padding-top: 8px;
  border-top: 1px solid rgba(255, 255, 255, 0.06);
}

.group-mini-btn {
  width: 24px !important;
  min-width: 24px !important;
  height: 24px !important;
  padding: 0 !important;

  :deep(.el-icon) {
    font-size: 12px;
  }
}

.group-mini-btn--minus {
  border-color: rgba(201, 87, 69, 0.45) !important;
  color: #c95745 !important;
  background: rgba(201, 87, 69, 0.08) !important;

  &:hover {
    border-color: rgba(201, 87, 69, 0.65) !important;
    background: rgba(201, 87, 69, 0.14) !important;
  }
}

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
  margin: 0 0 6px;

  .title-bar {
    width: 3px;
    height: 16px;
    background: linear-gradient(180deg, #00c8ff, #0066cc);
    border-radius: 2px;
    flex-shrink: 0;
  }
}

.section-hint {
  font-size: 12px;
  color: $text-muted;
  margin: 0;
  line-height: 1.5;
}

.group-card {
  background: rgba(0, 212, 255, 0.03);
  border: 1px solid $border-panel;
  border-radius: 8px;
  padding: 14px 16px 8px;
  margin-bottom: 14px;
}

.group-card__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 8px;
  padding-bottom: 10px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.group-card__title {
  font-size: 13px;
  font-weight: 600;
  color: $text-secondary;
}

.group-card__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
}

.switch-label {
  font-size: 12px;
  color: $text-muted;
}

.upload-row {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.file-hint {
  font-size: 12px;
  color: $text-secondary;
}

.preview-link {
  font-size: 12px;
  color: $cyan-primary;
  text-decoration: none;
  &:hover {
    text-decoration: underline;
  }
}

.asset-preview {
  margin-top: 10px;
  padding: 10px;
  background: rgba(0, 212, 255, 0.04);
  border: 1px dashed $border-panel;
  border-radius: 6px;
  max-width: 280px;
}

.asset-preview__img {
  display: block;
  max-width: 100%;
  max-height: 120px;
  width: auto;
  height: auto;
  object-fit: contain;
}

.asset-preview__pdf {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 13px;
  color: $text-secondary;
}

.asset-preview__pdf-label {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 40px;
  padding: 4px 8px;
  background: $layer-3;
  border-radius: 4px;
  font-weight: 600;
  color: $text-primary;
}

.asset-preview__other {
  font-size: 12px;
  color: $text-muted;
}

.settings-form {
  :deep(.el-form-item__label) {
    color: $text-muted;
    font-size: 13px;
  }

  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner) {
    background: $layer-3;
    border-color: $border-panel;
    box-shadow: none;
    &:hover {
      border-color: rgba(0, 212, 255, 0.35);
    }
    &.is-focus {
      border-color: $cyan-primary;
    }
  }

  :deep(.el-input__inner),
  :deep(.el-textarea__inner) {
    color: $text-primary;
    background: transparent;
    &::placeholder {
      color: $text-placeholder;
    }
  }

  :deep(.el-select .el-select__wrapper) {
    background: $layer-3 !important;
    border: 1px solid $border-panel !important;
    box-shadow: none !important;
  }
}
</style>
