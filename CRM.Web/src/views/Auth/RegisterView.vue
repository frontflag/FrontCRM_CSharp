<template>
  <div class="register-view">
    <el-container>
      <el-main>
        <el-card class="register-card">
          <template #header>
            <div class="card-header">
              <h2>注册</h2>
            </div>
          </template>

          <el-form
            ref="formRef"
            :model="form"
            :rules="rules"
            label-width="80px"
            @submit.prevent="handleRegister"
          >
            <el-form-item label="用户名" prop="userName">
              <el-input
                v-model="form.userName"
                placeholder="请输入用户名"
              />
            </el-form-item>

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
                注册
              </el-button>
            </el-form-item>
          </el-form>

          <div class="footer-links">
            <router-link to="/login">已有账号？立即登录</router-link>
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
  userName: '',
  email: '',
  password: ''
})

const rules: FormRules = {
  userName: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 2, max: 50, message: '用户名长度在 2 到 50 个字符', trigger: 'blur' }
  ],
  email: [
    { required: true, message: '请输入邮箱地址', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱地址', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6个字符', trigger: 'blur' }
  ]
}

const handleRegister = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    loading.value = true
    try {
      const success = await authStore.register(form)
      if (success) {
        ElMessage.success('注册成功')
        router.push('/dashboard')
      } else {
        ElMessage.error('注册失败，请稍后重试')
      }
    } catch (error: any) {
      ElMessage.error(error.response?.data?.message || '注册失败，请稍后重试')
    } finally {
      loading.value = false
    }
  })
}
</script>

<style scoped lang="scss">
.register-view {
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

  .register-card {
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
