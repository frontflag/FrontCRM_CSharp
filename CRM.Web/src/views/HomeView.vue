<template>
  <div class="home-view">
    <el-container>
      <el-header>
        <h1>FrontCRM</h1>
      </el-header>
      <el-main>
        <div class="welcome-section">
          <h2>欢迎使用 FrontCRM 系统</h2>
          <p>一个现代化的客户关系管理系统</p>
          <div class="action-buttons">
            <el-button type="primary" size="large" @click="$router.push('/login')">
              登录
            </el-button>
            <el-button type="success" size="large" @click="$router.push('/register')">
              注册
            </el-button>
          </div>
        </div>
      </el-main>
    </el-container>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped lang="scss">
.home-view {
  width: 100%;
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);

  .el-header {
    background: rgba(255, 255, 255, 0.1);
    backdrop-filter: blur(10px);
    display: flex;
    align-items: center;
    padding: 0 50px;

    h1 {
      color: white;
      font-size: 28px;
      font-weight: bold;
      margin: 0;
    }
  }

  .el-main {
    display: flex;
    justify-content: center;
    align-items: center;
  }

  .welcome-section {
    text-align: center;
    color: white;

    h2 {
      font-size: 48px;
      margin-bottom: 20px;
      font-weight: bold;
    }

    p {
      font-size: 20px;
      margin-bottom: 40px;
      opacity: 0.9;
    }

    .action-buttons {
      display: flex;
      gap: 20px;
      justify-content: center;

      .el-button {
        min-width: 120px;
        font-size: 16px;
      }
    }
  }
}
</style>
