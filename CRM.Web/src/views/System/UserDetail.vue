<template>
  <div class="user-detail-page">
    <div class="page-header">
      <div class="header-left">
        <button type="button" class="btn-back" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          返回
        </button>
        <div v-if="user" class="customer-title-group">
          <div v-if="avatarSrc" class="customer-avatar-lg customer-avatar-lg--photo">
            <img :src="avatarSrc" alt="" />
          </div>
          <div v-else class="customer-avatar-lg">{{ fallbackLetter }}</div>
          <div>
            <h1 class="page-title">{{ displayName }}</h1>
            <div class="title-meta">
              <span class="customer-code">{{ user.userName }}</span>
              <span
                class="status-badge"
                :class="user.status === 1 ? 'status--active' : 'status--inactive'"
              >
                {{ user.status === 1 ? '启用' : '禁用' }}
              </span>
            </div>
          </div>
        </div>
      </div>
      <div class="header-right">
        <button v-if="user" type="button" class="btn-secondary" @click="goEdit">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
          </svg>
          编辑
        </button>
      </div>
    </div>

    <div
      v-loading="loading"
      element-loading-background="rgba(10,22,40,0.8)"
      class="detail-content"
    >
      <template v-if="user">
        <div class="info-section">
          <div class="section-header">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">基本信息</span>
          </div>
          <div class="info-grid">
            <div class="info-item">
              <span class="info-label">员工账号</span>
              <span class="info-value info-value--code">{{ user.userName }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">状态</span>
              <span>
                <span
                  class="status-badge"
                  :class="user.status === 1 ? 'status--active' : 'status--inactive'"
                >
                  {{ user.status === 1 ? '启用' : '禁用' }}
                </span>
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">真实姓名</span>
              <span class="info-value">{{ user.realName || '--' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">邮箱</span>
              <span class="info-value">{{ user.email || '--' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">手机</span>
              <span class="info-value info-value--code">{{ user.mobile || '--' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">出生日期</span>
              <span class="info-value info-value--time">{{ formatDate(user.birthDate) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">入职日期</span>
              <span class="info-value info-value--time">{{ formatDate(user.hireDate) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">角色</span>
              <span class="info-value">{{ user.roleCodes?.length ? user.roleCodes.join(', ') : '--' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">所属部门</span>
              <span class="info-value">{{ deptLabel }}</span>
            </div>
            <div class="info-item info-item--full">
              <span class="info-label">主部门</span>
              <span class="info-value">
                {{ user.primaryDepartmentName || '--' }}
                <span v-if="user.primaryDepartmentPath" class="path-muted">
                  （{{ user.primaryDepartmentPath }}）
                </span>
              </span>
            </div>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { rbacAdminApi, type AdminUserDto } from '@/api/rbacAdmin'

const route = useRoute()
const router = useRouter()

const loading = ref(false)
const user = ref<AdminUserDto | null>(null)
const avatarBust = ref(0)

const userId = computed(() => (route.params.id as string) || '')

const displayName = computed(() => {
  const u = user.value
  if (!u) return '员工详情'
  const name = u.realName?.trim()
  return name || u.userName || '员工详情'
})

const avatarSrc = computed(() => {
  const u = user.value?.avatarUrl?.trim()
  if (!u) return ''
  const sep = u.includes('?') ? '&' : '?'
  return `${u}${sep}t=${avatarBust.value}`
})

const fallbackLetter = computed(() => {
  const n = user.value?.realName?.trim() || user.value?.userName?.trim() || ''
  return n ? n.charAt(0).toUpperCase() : '?'
})

const deptLabel = computed(() => {
  if (!user.value?.departmentIds?.length) return '--'
  return `${user.value.departmentIds.length} 个部门`
})

const formatDate = (v: string | null | undefined) => {
  if (v == null || v === '') return '--'
  return v.length >= 10 ? v.slice(0, 10) : v
}

const load = async () => {
  const id = userId.value
  if (!id) {
    ElMessage.error('无效的员工 ID')
    router.push({ name: 'UserList' })
    return
  }
  loading.value = true
  try {
    user.value = await rbacAdminApi.getUserById(id)
    avatarBust.value = Date.now()
  } catch (e: any) {
    ElMessage.error(e?.message || '加载员工详情失败')
    router.push({ name: 'UserList' })
  } finally {
    loading.value = false
  }
}

const goBack = () => router.push({ name: 'UserList' })
const goEdit = () => router.push({ name: 'UserEdit', params: { id: userId.value } })

onMounted(load)
watch(
  () => route.params.id,
  () => load()
)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.user-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 16px;
  }

  .header-right {
    display: flex;
    gap: 10px;
  }
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
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.customer-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
}

.customer-avatar-lg {
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

  &--photo {
    padding: 0;
    overflow: hidden;

    img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
  }
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0 0 6px 0;
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
}

.customer-code {
  font-family: 'Space Mono', monospace;
  font-size: 11px;
  color: $text-muted;
}

.status-badge {
  font-size: 10px;
  padding: 2px 7px;
  border-radius: 3px;

  &--active {
    background: rgba(70, 191, 145, 0.15);
    color: $color-mint-green;
    border: 1px solid rgba(70, 191, 145, 0.3);
  }

  &--inactive {
    background: rgba(107, 122, 141, 0.15);
    color: #8a9bb0;
    border: 1px solid rgba(107, 122, 141, 0.3);
  }
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.25);
  }
}

.info-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: rgba(0, 0, 0, 0.1);
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.6);
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
  padding: 0;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 16px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
  border-right: 1px solid rgba(255, 255, 255, 0.04);

  &:nth-child(3n) {
    border-right: none;
  }

  &--full {
    grid-column: 1 / -1;
    border-right: none;
  }

  .info-label {
    font-size: 11px;
    color: $text-muted;
    letter-spacing: 0.5px;
    text-transform: uppercase;
  }

  .info-value {
    font-size: 13px;
    color: $text-secondary;

    &--code {
      font-family: 'Space Mono', monospace;
      font-size: 12px;
      color: $color-ice-blue;
    }

    &--time {
      font-size: 12px;
      color: $text-muted;
    }
  }
}

.path-muted {
  color: $text-muted;
  font-size: 12px;
  margin-left: 4px;
}
</style>
