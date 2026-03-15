<template>
  <div class="dashboard-view">
    <el-container style="height: 100vh">
      <el-header class="header">
        <div class="logo">FrontCRM</div>
        <div class="user-info">
          <span>{{ authStore.user?.userName }}</span>
          <el-button type="danger" size="small" @click="handleLogout">
            退出登录
          </el-button>
        </div>
      </el-header>

      <el-container>
        <el-aside width="200px">
          <el-menu
            :default-active="activeMenu"
            class="el-menu-vertical"
            router
          >
            <el-menu-item index="/dashboard">
              <el-icon><House /></el-icon>
              <span>首页</span>
            </el-menu-item>
            <el-menu-item index="/dashboard/customers">
              <el-icon><User /></el-icon>
              <span>客户管理</span>
            </el-menu-item>
            <el-menu-item index="/dashboard/settings">
              <el-icon><Setting /></el-icon>
              <span>系统设置</span>
            </el-menu-item>
          </el-menu>
        </el-aside>

        <el-main>
          <el-card>
            <template #header>
              <h3>欢迎回来，{{ authStore.user?.userName }}！</h3>
            </template>
            <p>这是 FrontCRM 系统的控制台。</p>
            <el-divider />
            <div class="stats-grid">
              <el-statistic title="总客户数" :value="0" />
              <el-statistic title="待处理事项" :value="0" />
              <el-statistic title="本月新增" :value="0" />
            </div>
          </el-card>
        </el-main>
      </el-container>
    </el-container>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessageBox } from 'element-plus'
import { useAuthStore } from '@/stores'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const activeMenu = ref(route.path)

const handleLogout = async () => {
  try {
    await ElMessageBox.confirm('确定要退出登录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    authStore.logout()
    router.push('/login')
  } catch {
    // 用户取消
  }
}
</script>

<style scoped lang="scss">
.dashboard-view {
  .header {
    background: #fff;
    border-bottom: 1px solid #e4e7ed;
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0 20px;

    .logo {
      font-size: 20px;
      font-weight: bold;
      color: #667eea;
    }

    .user-info {
      display: flex;
      align-items: center;
      gap: 15px;

      span {
        font-weight: 500;
      }
    }
  }

  .el-aside {
    background: #fff;
    border-right: 1px solid #e4e7ed;

    .el-menu-vertical {
      border-right: none;
    }
  }

  .stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 20px;
    margin-top: 20px;
  }
}
</style>
