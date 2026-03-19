<template>
  <div class="settings-view">
    <el-card>

            <el-tabs v-model="activeTab">
              <el-tab-pane label="基本信息" name="basic">
                <el-form :model="basicSettings" label-width="120px">
                  <el-form-item label="系统名称">
                    <el-input v-model="basicSettings.systemName" placeholder="请输入系统名称" />
                  </el-form-item>
                  <el-form-item label="系统版本">
                    <el-input v-model="basicSettings.systemVersion" placeholder="请输入系统版本" />
                  </el-form-item>
                  <el-form-item label="联系邮箱">
                    <el-input v-model="basicSettings.contactEmail" type="email" placeholder="请输入联系邮箱" />
                  </el-form-item>
                  <el-form-item label="联系电话">
                    <el-input v-model="basicSettings.contactPhone" placeholder="请输入联系电话" />
                  </el-form-item>
                  <el-form-item>
                    <el-button type="primary" @click="handleSaveBasic">保存设置</el-button>
                  </el-form-item>
                </el-form>
              </el-tab-pane>

              <el-tab-pane label="安全设置" name="security">
                <el-form :model="securitySettings" label-width="120px">
                  <el-form-item label="密码复杂度">
                    <el-switch v-model="securitySettings.requireComplexPassword" />
                    <span style="margin-left: 10px; color: #909399;">
                      启用后密码需包含大小写字母、数字和特殊字符
                    </span>
                  </el-form-item>
                  <el-form-item label="最小密码长度">
                    <el-input-number v-model="securitySettings.minPasswordLength" :min="6" :max="20" />
                  </el-form-item>
                  <el-form-item label="登录失败锁定">
                    <el-switch v-model="securitySettings.enableLockout" />
                  </el-form-item>
                  <el-form-item label="最大失败次数">
                    <el-input-number v-model="securitySettings.maxFailedAttempts" :min="3" :max="10" :disabled="!securitySettings.enableLockout" />
                  </el-form-item>
                  <el-form-item label="锁定时间（分钟）">
                    <el-input-number v-model="securitySettings.lockoutDuration" :min="5" :max="60" :disabled="!securitySettings.enableLockout" />
                  </el-form-item>
                  <el-form-item>
                    <el-button type="primary" @click="handleSaveSecurity">保存设置</el-button>
                  </el-form-item>
                </el-form>
              </el-tab-pane>

              <el-tab-pane label="通知设置" name="notification">
                <el-form :model="notificationSettings" label-width="120px">
                  <el-form-item label="邮件通知">
                    <el-switch v-model="notificationSettings.emailEnabled" />
                  </el-form-item>
                  <el-form-item label="新客户注册">
                    <el-switch v-model="notificationSettings.newCustomerNotification" />
                  </el-form-item>
                  <el-form-item label="系统更新">
                    <el-switch v-model="notificationSettings.systemUpdateNotification" />
                  </el-form-item>
                  <el-form-item label="安全警报">
                    <el-switch v-model="notificationSettings.securityAlertNotification" />
                  </el-form-item>
                  <el-form-item>
                    <el-button type="primary" @click="handleSaveNotification">保存设置</el-button>
                  </el-form-item>
                </el-form>
              </el-tab-pane>

              <el-tab-pane label="关于" name="about">
                <div class="about-section">
                  <h4>FrontCRM 系统</h4>
                  <p>一个基于 Vue 3 和 .NET 的现代化客户关系管理系统</p>

                  <el-divider />

                  <div class="info-list">
                    <div class="info-item">
                      <span class="label">版本：</span>
                      <span class="value">1.0.0</span>
                    </div>
                    <div class="info-item">
                      <span class="label">前端框架：</span>
                      <span class="value">Vue 3 + TypeScript + Vite</span>
                    </div>
                    <div class="info-item">
                      <span class="label">后端框架：</span>
                      <span class="value">ASP.NET Core 9.0</span>
                    </div>
                    <div class="info-item">
                      <span class="label">UI 框架：</span>
                      <span class="value">Element Plus</span>
                    </div>
                  </div>

                  <el-divider />

                  <p style="color: #909399; text-align: center;">
                    © 2024 FrontCRM. All rights reserved.
                  </p>
                </div>
              </el-tab-pane>
            </el-tabs>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { ElNotification } from 'element-plus'

const activeTab = ref('basic')

const basicSettings = reactive({
  systemName: 'FrontCRM',
  systemVersion: '1.0.0',
  contactEmail: 'admin@frontcrm.com',
  contactPhone: '400-123-4567'
})

const securitySettings = reactive({
  requireComplexPassword: true,
  minPasswordLength: 8,
  enableLockout: true,
  maxFailedAttempts: 5,
  lockoutDuration: 30
})

const notificationSettings = reactive({
  emailEnabled: true,
  newCustomerNotification: true,
  systemUpdateNotification: true,
  securityAlertNotification: true
})

const handleSaveBasic = () => {
  ElNotification.success({ title: '保存成功', message: '基本信息已保存' })
}

const handleSaveSecurity = () => {
  ElNotification.success({ title: '保存成功', message: '安全设置已保存' })
}

const handleSaveNotification = () => {
  ElNotification.success({ title: '保存成功', message: '通知设置已保存' })
}


</script>

<style scoped lang="scss">
.settings-view {
  .about-section {
    padding: 20px 0;

    h4 {
      font-size: 24px;
      margin-bottom: 10px;
      color: #333;
    }

    p {
      color: #666;
      line-height: 1.8;
    }

    .info-list {
      margin: 20px 0;

      .info-item {
        display: flex;
        margin-bottom: 15px;

        .label {
          font-weight: 500;
          color: #606266;
          min-width: 120px;
        }

        .value {
          color: #303133;
        }
      }
    }
  }
}
</style>
