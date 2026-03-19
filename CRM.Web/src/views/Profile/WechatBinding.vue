<template>
  <div class="wechat-binding-page">
    <el-card class="bind-card">
      <template #header>
        <div class="card-header">
          <span>微信绑定</span>
        </div>
      </template>

      <!-- 已绑定状态 -->
      <div v-if="bindInfo.isBound" class="bound-state">
        <div class="wechat-card">
          <el-avatar :size="64" :src="bindInfo.avatarUrl" />
          <div class="info">
            <p class="nickname">{{ bindInfo.nickname }}</p>
            <p class="time">绑定于 {{ formatDate(bindInfo.bindTime) }}</p>
          </div>
        </div>
        <el-divider />
        <div class="actions">
          <el-button type="danger" @click="handleUnbind">解除绑定</el-button>
        </div>
        <p class="tip">解除绑定后将无法使用微信扫码登录</p>
      </div>

      <!-- 未绑定状态 -->
      <div v-else class="unbound-state">
        <div class="bind-intro">
          <el-icon :size="48" color="#07C160"><ChatDotRound /></el-icon>
          <h3>绑定微信</h3>
          <p>绑定后可以使用微信扫码快速登录</p>
        </div>

        <!-- 生成二维码 -->
        <div v-if="!bindQrCode" class="generate-section">
          <el-button
            type="primary"
            size="large"
            @click="generateQrCode"
            :loading="generating"
          >
            生成绑定二维码
          </el-button>
          <p class="tip">二维码5分钟内有效</p>
        </div>

        <!-- 显示二维码 -->
        <div v-else class="qrcode-section">
          <div class="qr-container">
            <img :src="bindQrCode" alt="绑定二维码" class="bind-qrcode" />
            <div v-if="bindStatus === 'pending'" class="status pending">
              <el-icon class="loading"><Loading /></el-icon>
              等待微信扫码确认...
            </div>
            <div v-else-if="bindStatus === 'success'" class="status success">
              <el-icon><SuccessFilled /></el-icon>
              绑定成功！
            </div>
            <div v-else-if="bindStatus === 'expired'" class="status expired">
              <el-icon><Warning /></el-icon>
              二维码已过期
              <el-button link type="primary" @click="generateQrCode">重新生成</el-button>
            </div>
          </div>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ChatDotRound, Loading, SuccessFilled, Warning } from '@element-plus/icons-vue'
import { getWechatBindInfo, generateBindQrCode, checkBindStatus, unbindWechat } from '@/api/wechatAuth'
import { formatDate } from '@/utils/date'

const bindInfo = ref<{
  isBound: boolean
  nickname?: string
  avatarUrl?: string
  bindTime?: string
}>({ isBound: false })

const bindQrCode = ref('')
const bindId = ref('')
const bindStatus = ref('') // pending/success/expired
const generating = ref(false)
let pollingTimer: number | null = null

// 获取绑定信息
async function fetchBindInfo() {
  try {
    // apiClient 拦截器已解包，res 直接是业务数据
    const res = await getWechatBindInfo() as any
    if (res != null) {
      bindInfo.value = res
    }
  } catch (error) {
    console.error('获取绑定信息失败', error)
  }
}

// 生成绑定二维码
async function generateQrCode() {
  generating.value = true
  bindStatus.value = 'pending'
  try {
    // apiClient 拦截器已解包，res 直接是业务数据
    const res = await generateBindQrCode() as any
    if (res?.qrCodeUrl) {
      bindQrCode.value = res.qrCodeUrl
      bindId.value = res.bindId
      startPolling()
    }
  } catch (error) {
    ElMessage.error('生成二维码失败')
  }
  generating.value = false
}

// 轮询绑定状态
function startPolling() {
  pollingTimer = window.setInterval(async () => {
    try {
      // apiClient 拦截器已解包，res 直接是业务数据
      const res = await checkBindStatus(bindId.value) as any
      if (res?.status == null) return

      bindStatus.value = res.status

      if (res.status === 'success') {
        clearInterval(pollingTimer!)
        pollingTimer = null
        ElMessage.success('微信绑定成功！')
        // 更新绑定信息
        bindInfo.value = {
          isBound: true,
          nickname: res.nickname,
          avatarUrl: undefined, // 需要重新获取
          bindTime: new Date().toISOString()
        }
        // 重新获取完整信息
        setTimeout(fetchBindInfo, 500)
      } else if (res.status === 'expired') {
        clearInterval(pollingTimer!)
        pollingTimer = null
      }
    } catch (error) {
      console.error('轮询失败', error)
    }
  }, 2000)
}

// 解除绑定
async function handleUnbind() {
  try {
    await ElMessageBox.confirm(
      '解除绑定后将无法使用微信扫码登录，确定解除吗？',
      '确认解除绑定',
      { type: 'warning' }
    )

    // apiClient 拦截器已解包，请求成功则直接返回数据
    await unbindWechat()
    ElMessage.success('已解除微信绑定')
    bindQrCode.value = ''
    bindId.value = ''
    bindStatus.value = ''
    fetchBindInfo()
  } catch {
    // 取消
  }
}

onMounted(() => {
  fetchBindInfo()
})

onUnmounted(() => {
  if (pollingTimer) {
    clearInterval(pollingTimer)
  }
})
</script>

<style scoped lang="scss">
.wechat-binding-page {
  max-width: 600px;
  margin: 0 auto;
  padding: 20px;

  .bind-card {
    .card-header {
      font-size: 16px;
      font-weight: 500;
    }
  }

  .bound-state {
    .wechat-card {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 20px;
      background: #f5f7fa;
      border-radius: 8px;

      .nickname {
        font-size: 16px;
        font-weight: 500;
        margin: 0 0 4px;
      }

      .time {
        color: #909399;
        font-size: 13px;
        margin: 0;
      }
    }

    .actions {
      text-align: center;
      margin: 20px 0;
    }

    .tip {
      text-align: center;
      color: #909399;
      font-size: 13px;
    }
  }

  .unbound-state {
    text-align: center;
    padding: 20px;

    .bind-intro {
      margin-bottom: 32px;

      h3 {
        margin: 16px 0 8px;
        font-size: 18px;
      }

      p {
        color: #606266;
      }
    }

    .generate-section {
      .tip {
        margin-top: 12px;
        color: #909399;
        font-size: 13px;
      }
    }

    .qrcode-section {
      .qr-container {
        display: inline-block;
        padding: 20px;
        background: #f5f7fa;
        border-radius: 8px;
      }

      .bind-qrcode {
        width: 200px;
        height: 200px;
        border-radius: 8px;
        background: white;
        padding: 8px;
      }

      .status {
        margin-top: 16px;
        padding: 10px 20px;
        border-radius: 4px;
        font-size: 14px;
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 8px;

        &.pending {
          color: #409EFF;
          background: #ecf5ff;

          .loading {
            animation: rotate 1s linear infinite;
          }
        }

        &.success {
          color: #67C23A;
          background: #f0f9eb;
        }

        &.expired {
          color: #F56C6C;
          background: #fef0f0;
        }
      }
    }
  }
}

@keyframes rotate {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
</style>
