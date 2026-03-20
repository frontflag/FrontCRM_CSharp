<template>
  <div class="draft-list-page">
    <div class="page-header">
      <div>
        <h1 class="title">草稿箱</h1>
        <p class="sub-title">Customer / Vendor / RFQ 草稿统一管理</p>
      </div>
    </div>

    <el-card class="filter-card">
      <el-form :inline="true">
        <el-form-item label="业务类型">
          <el-select v-model="filters.entityType" clearable placeholder="全部类型" style="width: 170px">
            <el-option label="客户" value="CUSTOMER" />
            <el-option label="供应商" value="VENDOR" />
            <el-option label="RFQ需求" value="RFQ" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="filters.status" clearable placeholder="全部状态" style="width: 140px">
            <el-option label="草稿" :value="0" />
            <el-option label="已转正式" :value="1" />
            <el-option label="已废弃" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item label="关键词">
          <el-input
            v-model="filters.keyword"
            placeholder="草稿名/备注"
            clearable
            style="width: 260px"
            @keyup.enter="fetchDrafts"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="fetchDrafts">查询</el-button>
          <el-button @click="resetFilters">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card>
      <el-table :data="drafts" v-loading="loading" border stripe>
        <el-table-column prop="draftId" label="草稿ID" min-width="260" show-overflow-tooltip />
        <el-table-column prop="draftName" label="草稿名称" min-width="160" show-overflow-tooltip />
        <el-table-column label="业务类型" width="120">
          <template #default="{ row }">{{ entityTypeText(row.entityType) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="110">
          <template #default="{ row }">
            <el-tag effect="dark" :type="statusTagType(row.status)">{{ statusText(row.status) }}</el-tag effect="dark">
          </template>
        </el-table-column>
        <el-table-column prop="convertedEntityId" label="正式ID" min-width="220" show-overflow-tooltip />
        <el-table-column label="更新时间" width="180">
          <template #default="{ row }">{{ formatTime(row.modifyTime || row.createTime) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="300" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="restoreDraft(row)">恢复到编辑页</el-button>
            <el-button link type="success" @click="convertDraft(row)" :disabled="row.status !== 0">转正式</el-button>
            <el-button link type="danger" @click="deleteDraft(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { draftApi, type DraftDto } from '@/api/draft'

const router = useRouter()
const loading = ref(false)
const drafts = ref<DraftDto[]>([])

const filters = reactive<{
  entityType?: string
  status?: number
  keyword?: string
}>({
  entityType: undefined,
  status: undefined,
  keyword: ''
})

const fetchDrafts = async () => {
  loading.value = true
  try {
    drafts.value = await draftApi.getDrafts({
      entityType: filters.entityType,
      status: filters.status,
      keyword: filters.keyword?.trim() || undefined
    })
  } catch (err: any) {
    ElMessage.error(err?.message || '获取草稿列表失败')
  } finally {
    loading.value = false
  }
}

const resetFilters = async () => {
  filters.entityType = undefined
  filters.status = undefined
  filters.keyword = ''
  await fetchDrafts()
}

const entityTypeText = (type: string) => {
  if (type === 'CUSTOMER') return '客户'
  if (type === 'VENDOR') return '供应商'
  if (type === 'RFQ') return 'RFQ需求'
  return type
}

const statusText = (status: number) => {
  if (status === 0) return '草稿'
  if (status === 1) return '已转正式'
  if (status === 2) return '已废弃'
  return String(status)
}

const statusTagType = (status: number) => {
  if (status === 0) return 'info'
  if (status === 1) return 'success'
  if (status === 2) return 'warning'
  return 'info'
}

const formatTime = (value?: string) => (value ? new Date(value).toLocaleString() : '--')

const getCreatePathByEntityType = (entityType: string) => {
  if (entityType === 'CUSTOMER') return '/customers/create'
  if (entityType === 'VENDOR') return '/vendors/create'
  if (entityType === 'RFQ') return '/rfqs/create'
  return ''
}

const restoreDraft = (row: DraftDto) => {
  const path = getCreatePathByEntityType(row.entityType)
  if (!path) {
    ElMessage.error(`不支持的实体类型：${row.entityType}`)
    return
  }
  router.push({ path, query: { draftId: row.draftId } })
}

const convertDraft = async (row: DraftDto) => {
  try {
    const result = await draftApi.convertDraft(row.draftId)
    ElMessage.success(`转正式成功，实体ID：${result.entityId}`)
    await fetchDrafts()
  } catch (err: any) {
    ElMessage.error(err?.message || '草稿转正式失败')
  }
}

const deleteDraft = async (row: DraftDto) => {
  try {
    await ElMessageBox.confirm('确定删除该草稿吗？删除后不可恢复。', '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消'
    })
    await draftApi.deleteDraft(row.draftId)
    ElMessage.success('草稿已删除')
    await fetchDrafts()
  } catch (err: any) {
    if (err === 'cancel' || err === 'close') return
    ElMessage.error(err?.message || '删除草稿失败')
  }
}

onMounted(fetchDrafts)
</script>

<style scoped>
.draft-list-page {
  padding: 20px;
}
.page-header {
  margin-bottom: 16px;
}
.title {
  margin: 0;
  font-size: 20px;
}
.sub-title {
  margin: 6px 0 0;
  color: #8a8f98;
}
.filter-card {
  margin-bottom: 12px;
}
</style>
