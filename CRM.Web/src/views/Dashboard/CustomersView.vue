<template>
  <div class="customers-view">
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
              <div class="card-header">
                <h3>客户管理</h3>
                <el-button type="primary" @click="handleAdd">
                  添加客户
                </el-button>
              </div>
            </template>

            <el-table :data="customers" stripe style="width: 100%">
              <el-table-column prop="id" label="ID" width="80" />
              <el-table-column prop="name" label="客户名称" />
              <el-table-column prop="phone" label="电话" />
              <el-table-column prop="email" label="邮箱" />
              <el-table-column prop="createdAt" label="创建时间" width="180">
                <template #default="{ row }">
                  {{ formatDate(row.createdAt) }}
                </template>
              </el-table-column>
              <el-table-column label="操作" width="200" fixed="right">
                <template #default="{ row }">
                  <el-button size="small" @click="handleEdit(row)">
                    编辑
                  </el-button>
                  <el-button size="small" type="danger" @click="handleDelete(row)">
                    删除
                  </el-button>
                </template>
              </el-table-column>
            </el-table>

            <el-empty v-if="customers.length === 0" description="暂无客户数据" />
          </el-card>

          <!-- 添加/编辑客户对话框 -->
          <el-dialog
            v-model="dialogVisible"
            :title="dialogTitle"
            width="500px"
          >
            <el-form :model="form" :rules="rules" ref="formRef" label-width="80px">
              <el-form-item label="客户名称" prop="name">
                <el-input v-model="form.name" placeholder="请输入客户名称" />
              </el-form-item>
              <el-form-item label="电话" prop="phone">
                <el-input v-model="form.phone" placeholder="请输入电话号码" />
              </el-form-item>
              <el-form-item label="邮箱" prop="email">
                <el-input v-model="form.email" type="email" placeholder="请输入邮箱地址" />
              </el-form-item>
              <el-form-item label="地址" prop="address">
                <el-input
                  v-model="form.address"
                  type="textarea"
                  placeholder="请输入地址"
                  :rows="3"
                />
              </el-form-item>
              <el-form-item label="备注" prop="notes">
                <el-input
                  v-model="form.notes"
                  type="textarea"
                  placeholder="请输入备注信息"
                  :rows="3"
                />
              </el-form-item>
            </el-form>
            <template #footer>
              <el-button @click="dialogVisible = false">取消</el-button>
              <el-button type="primary" @click="handleSubmit">确定</el-button>
            </template>
          </el-dialog>
        </el-main>
      </el-container>
    </el-container>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElNotification, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { useAuthStore } from '@/stores'

const router = useRouter()
const authStore = useAuthStore()

const activeMenu = ref('/dashboard/customers')
const dialogVisible = ref(false)
const dialogTitle = ref('添加客户')
const formRef = ref<FormInstance>()

interface Customer {
  id: number
  name: string
  phone?: string
  email?: string
  address?: string
  notes?: string
  createdAt: string
}

const customers = ref<Customer[]>([
  { id: 1, name: '示例客户A', phone: '13800138000', email: 'customer@example.com', address: '北京市朝阳区', notes: '重点客户', createdAt: new Date().toISOString() },
  { id: 2, name: '示例客户B', phone: '13900139000', email: 'client@example.com', address: '上海市浦东新区', notes: '', createdAt: new Date().toISOString() }
])

const form = reactive({
  id: 0,
  name: '',
  phone: '',
  email: '',
  address: '',
  notes: ''
})

const rules: FormRules = {
  name: [
    { required: true, message: '请输入客户名称', trigger: 'blur' },
    { min: 2, max: 50, message: '客户名称长度在 2 到 50 个字符', trigger: 'blur' }
  ],
  email: [
    { type: 'email', message: '请输入正确的邮箱地址', trigger: 'blur' }
  ]
}

const formatDate = (dateString: string) => {
  const date = new Date(dateString)
  return date.toLocaleString('zh-CN')
}

const handleAdd = () => {
  dialogTitle.value = '添加客户'
  Object.assign(form, {
    id: 0,
    name: '',
    phone: '',
    email: '',
    address: '',
    notes: ''
  })
  dialogVisible.value = true
}

const handleEdit = (row: Customer) => {
  dialogTitle.value = '编辑客户'
  Object.assign(form, row)
  dialogVisible.value = true
}

const handleDelete = async (row: Customer) => {
  try {
    await ElMessageBox.confirm('确定要删除该客户吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    const index = customers.value.findIndex(c => c.id === row.id)
    if (index > -1) {
      customers.value.splice(index, 1)
      ElNotification.success({ title: '删除成功', message: '客户已删除' })
    }
  } catch {
    // 用户取消
  }
}

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate((valid) => {
    if (!valid) return

    if (form.id === 0) {
      // 添加新客户
      const newCustomer: Customer = {
        ...form,
        id: customers.value.length > 0 ? Math.max(...customers.value.map(c => c.id)) + 1 : 1,
        createdAt: new Date().toISOString()
      }
      customers.value.push(newCustomer)
      ElNotification.success({ title: '添加成功', message: '客户已添加' })
    } else {
      // 编辑客户
      const index = customers.value.findIndex(c => c.id === form.id)
      if (index > -1) {
        customers.value[index] = { ...customers.value[index], ...form }
        ElNotification.success({ title: '更新成功', message: '客户信息已更新' })
      }
    }

    dialogVisible.value = false
  })
}

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
.customers-view {
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

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;

    h3 {
      margin: 0;
    }
  }
}
</style>
