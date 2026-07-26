<script setup lang="ts">
import { reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { debugSuperApi } from '@/api/debugSuper'
import type { OperationLogRow } from '@/api/operationLogs'
import { getApiErrorMessage } from '@/utils/apiError'

type LeftMenu = 'account'
type AccountTab = 'password' | 'create' | 'logs'

const activeMenu = ref<LeftMenu>('account')
const activeTab = ref<AccountTab>('password')

const pwdForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})
const pwdSaving = ref(false)

const createForm = reactive({
  userName: '',
  password: '',
  confirmPassword: '',
  realName: '',
  email: ''
})
const createSaving = ref(false)

const logLoading = ref(false)
const logRows = ref<OperationLogRow[]>([])
const logTotal = ref(0)
const logPage = ref(1)
const logPageSize = ref(20)
const logsLoaded = ref(false)

async function loadLogs() {
  logLoading.value = true
  try {
    const data = await debugSuperApi.listOperationLogs(logPage.value, logPageSize.value)
    logRows.value = data.items
    logTotal.value = data.total
    logPage.value = data.page
    logPageSize.value = data.pageSize
    logsLoaded.value = true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || '加载操作记录失败')
  } finally {
    logLoading.value = false
  }
}

async function submitChangePassword() {
  if (!pwdForm.currentPassword) {
    ElMessage.warning('请输入当前密码')
    return
  }
  if (!pwdForm.newPassword || pwdForm.newPassword.length < 6) {
    ElMessage.warning('新密码长度至少 6 位')
    return
  }
  if (pwdForm.newPassword !== pwdForm.confirmPassword) {
    ElMessage.warning('两次输入的新密码不一致')
    return
  }
  pwdSaving.value = true
  try {
    await debugSuperApi.changePassword(pwdForm.currentPassword, pwdForm.newPassword)
    ElMessage.success('密码已更新')
    pwdForm.currentPassword = ''
    pwdForm.newPassword = ''
    pwdForm.confirmPassword = ''
    if (logsLoaded.value) await loadLogs()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || '改密失败')
  } finally {
    pwdSaving.value = false
  }
}

async function submitCreate() {
  if (!createForm.userName.trim()) {
    ElMessage.warning('请输入账号')
    return
  }
  if (!createForm.password || createForm.password.length < 6) {
    ElMessage.warning('密码长度至少 6 位')
    return
  }
  if (createForm.password !== createForm.confirmPassword) {
    ElMessage.warning('两次输入的密码不一致')
    return
  }
  createSaving.value = true
  try {
    const created = await debugSuperApi.createSuperAdmin({
      userName: createForm.userName.trim(),
      password: createForm.password,
      realName: createForm.realName.trim() || undefined,
      email: createForm.email.trim() || undefined
    })
    ElMessage.success(`已创建 SuperAdmin：${created.userName}`)
    createForm.userName = ''
    createForm.password = ''
    createForm.confirmPassword = ''
    createForm.realName = ''
    createForm.email = ''
    if (logsLoaded.value) await loadLogs()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || '创建失败')
  } finally {
    createSaving.value = false
  }
}

function formatTime(v?: string | null) {
  if (!v) return '—'
  const d = new Date(v)
  if (Number.isNaN(d.getTime())) return v
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}

watch(activeTab, (tab) => {
  if (tab === 'logs' && !logsLoaded.value) void loadLogs()
})
</script>

<template>
  <div class="debug-super">
    <aside class="super-menu">
      <div class="menu-title">运维菜单</div>
      <el-menu :default-active="activeMenu" class="menu-list">
        <el-menu-item index="account" @click="activeMenu = 'account'">
          SuperAdmin账号设置
        </el-menu-item>
      </el-menu>
    </aside>

    <main class="super-main">
      <div v-if="activeMenu === 'account'" class="main-panel">
        <div class="main-header">
          <h1>SuperAdmin账号设置</h1>
          <div class="main-sub">
            仅 SuperAdmin 可访问；不出现在 Debug 列表。改密仅限本人且须验证旧密码。
          </div>
        </div>

        <el-tabs v-model="activeTab" class="account-tabs">
          <el-tab-pane label="修改自己的密码" name="password">
            <el-form label-width="110px" class="form-block" @submit.prevent>
              <el-form-item label="当前密码" required>
                <el-input
                  v-model="pwdForm.currentPassword"
                  type="password"
                  show-password
                  autocomplete="current-password"
                />
              </el-form-item>
              <el-form-item label="新密码" required>
                <el-input
                  v-model="pwdForm.newPassword"
                  type="password"
                  show-password
                  autocomplete="new-password"
                />
              </el-form-item>
              <el-form-item label="确认新密码" required>
                <el-input
                  v-model="pwdForm.confirmPassword"
                  type="password"
                  show-password
                  autocomplete="new-password"
                />
              </el-form-item>
              <el-form-item>
                <el-button type="primary" :loading="pwdSaving" @click="submitChangePassword">
                  更新密码
                </el-button>
              </el-form-item>
            </el-form>
          </el-tab-pane>

          <el-tab-pane label="创建SuperAdmin账号" name="create">
            <div class="panel-hint">仅分配 SYS_ADMIN；部门可空。初始密码由创建者设定。</div>
            <el-form label-width="110px" class="form-block" @submit.prevent>
              <el-form-item label="账号" required>
                <el-input v-model="createForm.userName" autocomplete="off" />
              </el-form-item>
              <el-form-item label="初始密码" required>
                <el-input
                  v-model="createForm.password"
                  type="password"
                  show-password
                  autocomplete="new-password"
                />
              </el-form-item>
              <el-form-item label="确认密码" required>
                <el-input
                  v-model="createForm.confirmPassword"
                  type="password"
                  show-password
                  autocomplete="new-password"
                />
              </el-form-item>
              <el-form-item label="姓名">
                <el-input v-model="createForm.realName" autocomplete="off" />
              </el-form-item>
              <el-form-item label="邮箱">
                <el-input v-model="createForm.email" autocomplete="off" />
              </el-form-item>
              <el-form-item>
                <el-button type="primary" :loading="createSaving" @click="submitCreate">创建</el-button>
              </el-form-item>
            </el-form>
          </el-tab-pane>

          <el-tab-pane label="敏感操作记录" name="logs">
            <div class="logs-toolbar">
              <div class="panel-hint">BizType = super_admin；系统「操作日志」页不展示此类记录。</div>
              <el-button size="small" :loading="logLoading" @click="loadLogs">刷新</el-button>
            </div>
            <el-table
              v-loading="logLoading"
              :data="logRows"
              stripe
              empty-text="暂无记录"
              style="width: 100%"
            >
              <el-table-column prop="operationTime" label="时间" min-width="160">
                <template #default="{ row }">{{ formatTime(row.operationTime) }}</template>
              </el-table-column>
              <el-table-column prop="actionType" label="动作" min-width="120" />
              <el-table-column prop="operatorUserName" label="操作者" min-width="120" />
              <el-table-column prop="recordCode" label="对象账号" min-width="120" />
              <el-table-column prop="operationDesc" label="说明" min-width="280" show-overflow-tooltip />
            </el-table>
            <div class="pager">
              <el-pagination
                v-model:current-page="logPage"
                v-model:page-size="logPageSize"
                :total="logTotal"
                :page-sizes="[10, 20, 50]"
                layout="total, sizes, prev, pager, next"
                background
                @current-change="loadLogs"
                @size-change="() => { logPage = 1; loadLogs() }"
              />
            </div>
          </el-tab-pane>
        </el-tabs>
      </div>
    </main>
  </div>
</template>

<style lang="scss" scoped>
.debug-super {
  display: flex;
  min-height: calc(100vh - 120px);
  color: #303133;
  background: var(--el-bg-color-page, #f5f7fa);
}

.super-menu {
  width: 220px;
  flex-shrink: 0;
  border-right: 1px solid var(--el-border-color-lighter);
  background: var(--el-bg-color);
}

.menu-title {
  padding: 16px 18px 10px;
  font-size: 13px;
  font-weight: 600;
  color: #909399;
}

.menu-list {
  border-right: none;

  :deep(.el-menu-item) {
    height: 44px;
    line-height: 44px;
    font-size: 14px;
  }
}

.super-main {
  flex: 1;
  min-width: 0;
  padding: 20px 24px 28px;
}

.main-panel {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 10px;
  padding: 18px 20px 22px;
  box-shadow: var(--el-box-shadow-light);
}

.main-header h1 {
  margin: 0;
  font-size: 18px;
  font-weight: 700;
}

.main-sub {
  margin-top: 6px;
  margin-bottom: 14px;
  font-size: 13px;
  color: #909399;
  line-height: 1.6;
}

.account-tabs {
  :deep(.el-tabs__header) {
    margin-bottom: 18px;
  }
}

.panel-hint {
  margin: 0 0 14px;
  font-size: 12px;
  color: #909399;
  line-height: 1.5;
}

.form-block {
  max-width: 480px;
  padding-top: 4px;
}

.logs-toolbar {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 4px;

  .panel-hint {
    margin: 0;
    flex: 1;
  }
}

.pager {
  margin-top: 14px;
  display: flex;
  justify-content: flex-end;
}
</style>
