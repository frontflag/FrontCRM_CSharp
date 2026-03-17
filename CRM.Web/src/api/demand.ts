// ============================================================
// 需求管理模块 - API 调用层
// 对应后端路由: /api/v1/demands
// ============================================================
import apiClient from './client'
import type {
  Demand,
  DemandItem,
  DemandAssignment,
  DemandCloseRecord,
  PurchaserInfo,
  AssignmentStatistics,
  DemandSearchRequest,
  DemandSearchResponse,
  DemandItemSearchRequest,
  DemandItemSearchResponse,
  CreateDemandRequest,
  UpdateDemandRequest,
  UpdateDemandStatusRequest,
  AssignPurchaserRequest,
  CloseDemandRequest,
  CheckDuplicateMaterialRequest,
  CheckDuplicateMaterialResponse,
} from '@/types/demand'

const BASE = '/api/v1/demands'

// 构建查询字符串
function buildQuery(params: Record<string, any>): string {
  const q = new URLSearchParams()
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') {
      q.append(k, String(v))
    }
  })
  return q.toString()
}

export const demandApi = {
  // ─────────────────────────────────────────────────────────
  // 一、查询功能
  // ─────────────────────────────────────────────────────────

  /** 1. 需求主列表分页查询 */
  async searchDemands(params: DemandSearchRequest): Promise<DemandSearchResponse> {
    const q = buildQuery(params as Record<string, any>)
    return apiClient.get<DemandSearchResponse>(`${BASE}?${q}`)
  },

  /** 2. 需求明细列表分页查询（多条件筛选） */
  async searchDemandItems(params: DemandItemSearchRequest): Promise<DemandItemSearchResponse> {
    const q = buildQuery(params as Record<string, any>)
    return apiClient.get<DemandItemSearchResponse>(`${BASE}/items?${q}`)
  },

  /** 3. 需求明细详情查看（根据明细ID） */
  async getDemandItemById(itemId: string): Promise<DemandItem> {
    return apiClient.get<DemandItem>(`${BASE}/items/${itemId}`)
  },

  /** 4. 根据主表ID获取明细集合 */
  async getDemandItemsByDemandId(demandId: string): Promise<DemandItem[]> {
    return apiClient.get<DemandItem[]>(`${BASE}/${demandId}/items`)
  },

  /** 5. 根据ID获取需求实体 */
  async getDemandById(id: string): Promise<Demand> {
    return apiClient.get<Demand>(`${BASE}/${id}`)
  },

  /** 6. 获取需求详情信息（含扩展信息） */
  async getDemandDetail(id: string): Promise<Demand> {
    return apiClient.get<Demand>(`${BASE}/${id}/detail`)
  },

  /** 7. 根据条件获取需求明细简要数据 */
  async getDemandItemsBrief(params: DemandItemSearchRequest): Promise<DemandItem[]> {
    const q = buildQuery(params as Record<string, any>)
    return apiClient.get<DemandItem[]>(`${BASE}/items/brief?${q}`)
  },

  /** 8. 获取需求明细数据和最优报价 */
  async getDemandItemsWithBestQuote(demandId: string): Promise<DemandItem[]> {
    return apiClient.get<DemandItem[]>(`${BASE}/${demandId}/items/best-quote`)
  },

  /** 9. 获取客户订阅需求列表 */
  async getCustomerSubscribedDemands(customerId: string, params: { pageNumber?: number; pageSize?: number }): Promise<DemandSearchResponse> {
    const q = buildQuery({ customerId, ...params })
    return apiClient.get<DemandSearchResponse>(`${BASE}/subscribed?${q}`)
  },

  /** 10. 获取最近询盘（数字大屏） */
  async getRecentInquiries(count?: number): Promise<Demand[]> {
    return apiClient.get<Demand[]>(`${BASE}/recent-inquiries?count=${count ?? 10}`)
  },

  // ─────────────────────────────────────────────────────────
  // 二、创建功能
  // ─────────────────────────────────────────────────────────

  /** 11. 新增需求 */
  async createDemand(data: CreateDemandRequest): Promise<Demand> {
    return apiClient.post<Demand>(BASE, data)
  },

  /** 14. 批量自动生成需求/报价/订单 */
  async batchAutoGenerate(data: CreateDemandRequest[]): Promise<{ successCount: number; failCount: number; errors: string[] }> {
    return apiClient.post(`${BASE}/batch-auto-generate`, data)
  },

  /** 15. 数据验证 */
  async validateDemand(data: CreateDemandRequest): Promise<{ isValid: boolean; errors: string[] }> {
    return apiClient.post(`${BASE}/validate`, data)
  },

  /** 16. 检查重复物料 */
  async checkDuplicateMaterial(data: CheckDuplicateMaterialRequest): Promise<CheckDuplicateMaterialResponse> {
    return apiClient.post(`${BASE}/check-duplicate`, data)
  },

  // ─────────────────────────────────────────────────────────
  // 三、编辑功能
  // ─────────────────────────────────────────────────────────

  /** 17. 编辑需求及明细 */
  async updateDemand(id: string, data: UpdateDemandRequest): Promise<Demand> {
    return apiClient.put<Demand>(`${BASE}/${id}`, data)
  },

  /** 18. 修改需求客户 */
  async updateDemandCustomer(id: string, customerId: string): Promise<void> {
    return apiClient.put(`${BASE}/${id}/customer`, { customerId })
  },

  /** 19. 设置重要标识 */
  async setImportant(id: string, isImportant: boolean): Promise<void> {
    return apiClient.put(`${BASE}/${id}/important`, { isImportant })
  },

  /** 20. 批量新增可替代料 */
  async addAlternativeMaterials(demandId: string, items: { itemId: string; alternativeMaterialCode: string; alternativeMaterialName: string }[]): Promise<void> {
    return apiClient.post(`${BASE}/${demandId}/alternative-materials`, { items })
  },

  /** 21. 修改BOM包基本信息 */
  async updateBomInfo(id: string, data: { bomCode?: string; bomVersion?: string; remark?: string }): Promise<void> {
    return apiClient.put(`${BASE}/${id}/bom-info`, data)
  },

  // ─────────────────────────────────────────────────────────
  // 四、删除功能
  // ─────────────────────────────────────────────────────────

  /** 22. 删除需求实体（单个明细） */
  async deleteDemandItem(demandId: string, itemId: string): Promise<void> {
    return apiClient.delete(`${BASE}/${demandId}/items/${itemId}`)
  },

  /** 23. 删除主需求实体（含明细） */
  async deleteDemand(id: string): Promise<void> {
    return apiClient.delete(`${BASE}/${id}`)
  },

  // ─────────────────────────────────────────────────────────
  // 五、采购员分配功能
  // ─────────────────────────────────────────────────────────

  /** 24. 需求分配采购员（自动/手动） */
  async assignPurchaser(demandId: string, data: AssignPurchaserRequest): Promise<DemandAssignment> {
    return apiClient.post<DemandAssignment>(`${BASE}/${demandId}/assign`, data)
  },

  /** 25. 根据需求编号重新设置采购员 */
  async reassignByCode(demandCode: string, data: AssignPurchaserRequest): Promise<void> {
    return apiClient.put(`${BASE}/reassign-by-code/${demandCode}`, data)
  },

  /** 26. 根据需求ID重新分配采购员 */
  async reassignById(demandId: string, data: AssignPurchaserRequest): Promise<void> {
    return apiClient.put(`${BASE}/${demandId}/reassign`, data)
  },

  /** 28. 新增需求分配（手动指定） */
  async addAssignment(demandId: string, data: AssignPurchaserRequest): Promise<DemandAssignment> {
    return apiClient.post<DemandAssignment>(`${BASE}/${demandId}/assignments`, data)
  },

  /** 29. 获取推荐分配采购员 */
  async getRecommendedPurchasers(demandId: string): Promise<PurchaserInfo[]> {
    return apiClient.get<PurchaserInfo[]>(`${BASE}/${demandId}/recommended-purchasers`)
  },

  /** 30. 获取分配采购员简要统计 */
  async getAssignmentStatistics(): Promise<AssignmentStatistics[]> {
    return apiClient.get<AssignmentStatistics[]>(`${BASE}/assignment-statistics`)
  },

  /** 31. 获取采购员列表 */
  async getPurchasers(): Promise<PurchaserInfo[]> {
    return apiClient.get<PurchaserInfo[]>(`${BASE}/purchasers`)
  },

  // ─────────────────────────────────────────────────────────
  // 六、需求状态管理
  // ─────────────────────────────────────────────────────────

  /** 33. 变更需求流转状态 */
  async updateFlowStatus(id: string, data: UpdateDemandStatusRequest): Promise<void> {
    return apiClient.put(`${BASE}/${id}/flow-status`, data)
  },

  /** 35. 更新需求状态（批量） */
  async batchUpdateStatus(ids: string[], status: number): Promise<void> {
    return apiClient.put(`${BASE}/batch-status`, { ids, status })
  },

  /** 36. 更新需求明细状态（批量） */
  async batchUpdateItemStatus(itemIds: string[], status: number): Promise<void> {
    return apiClient.put(`${BASE}/items/batch-status`, { itemIds, status })
  },

  /** 37. 设置采购员处理状态 */
  async setPurchaserHandleStatus(demandId: string, handleStatus: number): Promise<void> {
    return apiClient.put(`${BASE}/${demandId}/purchaser-handle-status`, { handleStatus })
  },

  /** 38. 更新需求报价读取状态（标记已读） */
  async markQuoteRead(demandId: string): Promise<void> {
    return apiClient.put(`${BASE}/${demandId}/quote-read`)
  },

  /** 39. 获取需求关闭记录 */
  async getCloseRecords(demandId: string): Promise<DemandCloseRecord[]> {
    return apiClient.get<DemandCloseRecord[]>(`${BASE}/${demandId}/close-records`)
  },

  /** 40. 新增Close记录 */
  async addCloseRecord(demandId: string, data: CloseDemandRequest): Promise<DemandCloseRecord> {
    return apiClient.post<DemandCloseRecord>(`${BASE}/${demandId}/close-records`, data)
  },
}
