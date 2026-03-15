<template>
  <div class="login-view">
    <el-container>
      <el-main>
        <el-card class="login-card">
          <template #header>
            <div class="card-header">
              <h2>登录</h2>
            </div>
          </template>

          <el-form
            ref="formRef"
            :model="form"
            :rules="rules"
            label-width="80px"
            @submit.prevent="handleLogin"
          >
            <el-form-item label="邮箱" prop="email">
              <el-input
                v-model="form.email"
                type="email"
                placeholder="请输入邮箱"
              />
            </el-form-item>

            <el-form-item label="密码" prop="password">
              <el-input
                v-model="form.password"
                type="password"
                placeholder="请输入密码"
                show-password
              />
            </el-form-item>

            <el-form-item>
              <el-button
                type="primary"
                :loading="loading"
                native-type="submit"
                style="width: 100%"
              >
                登录
              </el-button>
            </el-form-item>
          </el-form>

          <div class="footer-links">
            <router-link to="/register">还没有账号？立即注册</router-link>
          </div>
        </el-card>
      </el-main>
    </el-container>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { useAuthStore } from '@/stores'

const router = useRouter()
const authStore = useAuthStore()

const formRef = ref<FormInstance>()
const loading = ref(false)

const form = reactive({
  email: '',
  password: ''
})

const rules: FormRules = {
  email: [
    { required: true, message: '请输入邮箱地址', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱地址', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6个字符', trigger: 'blur' }
  ]
}

const handleLogin = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    loading.value = true
    try {
      const success = await authStore.login(form)
      if (success) {
        ElMessage.success('登录成功')
        router.push('/dashboard')
      } else {
        ElMessage.error('登录失败，请检查邮箱和密码')
      }
    } catch (error: any) {
      ElMessage.error(error.response?.data?.message || '登录失败，请稍后重试')
    } finally {
      loading.value = false
    }
  })
}
</script>

<style scoped lang="scss">
.login-view {
  width: 100%;
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;

  .el-main {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 20px;
  }

  .login-card {
    width: 100%;
    max-width: 400px;

    .card-header {
      text-align: center;

      h2 {
        margin: 0;
        font-size: 24px;
        color: #333;
      }
    }
  }

  .footer-links {
    text-align: center;
    margin-top: 20px;

    a {
      color: #667eea;
      text-decoration: none;

      &:hover {
        text-decoration: underline;
      }
    }
  }
}
</style>
