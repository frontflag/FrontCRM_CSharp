// ============================================================
// RFQ（需求询价）管理模块 - API 调用层
// 对应后端路由: /api/v1/rfqs
// ============================================================
import apiClient from './client'
import type {
  RFQ,
  RFQItem,
  RFQAssignment,
  RFQCloseRecord,
  PurchaserInfo,
  AssignmentStatistics,
  RFQSearchRequest,
  RFQSearchResponse,
  RFQItemSearchRequest,
  RFQItemSearchResponse,
  CreateRFQRequest,
  UpdateRFQRequest,
  UpdateRFQStatusRequest,
  AssignPurchaserRequest,
  CloseRFQRequest,
  CheckDuplicateMaterialRequest,
  CheckDuplicateMaterialResponse,
} from '@/types/rfq'

const BASE = '/api/v1/rfqs'

export type RfqFieldChangeLogRow = {
  id?: string
  rfqId?: string
  rfqCode?: string
  fieldName?: string
  fieldLabel?: string
  oldValue?: string | null
  newValue?: string | null
  changedByUserId?: string | null
  changedByUserName?: string | null
  changedAt?: string
  objectLabel?: string
}

/** 需求明细行上已软删的报价（谁、何时、单号） */
export type RfqItemDeletedQuoteRow = {
  quoteId: string
  quoteCode: string
  rfqItemId: string
  lineNo: number
  mpn: string
  brand: string
  dateCodeText: string
  leadTimeText: string
  quantityText: string
  quoteCreatedAt: string | null
  vendorName: string
  vendorLevel: string
  unitPriceText: string
  currencyText: string
  purchaseUserName: string
  deletedAt: string | null
  deletedByUserId: string | null
  deletedByUserName: string | null
}

export function normalizeRfqItemDeletedQuoteRows(raw: unknown): RfqItemDeletedQuoteRow[] {
  if (!Array.isArray(raw)) return []
  return raw.map((item) => {
    const o = (item ?? {}) as Record<string, unknown>
    const str = (a: unknown, b: unknown) => {
      const v = a ?? b
      return v == null ? '' : String(v).trim()
    }
    const num = (a: unknown, b: unknown) => {
      const n = Number(a ?? b ?? 0)
      return Number.isFinite(n) ? n : 0
    }
    return {
      quoteId: str(o.quoteId, o.QuoteId),
      quoteCode: str(o.quoteCode, o.QuoteCode),
      rfqItemId: str(o.rfqItemId, o.RfqItemId),
      lineNo: num(o.lineNo, o.LineNo),
      mpn: str(o.mpn, o.Mpn),
      brand: str(o.brand, o.Brand),
      dateCodeText: str(o.dateCodeText, o.DateCodeText),
      leadTimeText: str(o.leadTimeText, o.LeadTimeText),
      quantityText: str(o.quantityText, o.QuantityText),
      quoteCreatedAt: (() => {
        const v = o.quoteCreatedAt ?? o.QuoteCreatedAt
        if (v == null || v === '') return null
        return String(v)
      })(),
      vendorName: str(o.vendorName, o.VendorName),
      vendorLevel: str(o.vendorLevel, o.VendorLevel),
      unitPriceText: str(o.unitPriceText, o.UnitPriceText),
      currencyText: str(o.currencyText, o.CurrencyText),
      purchaseUserName: str(o.purchaseUserName, o.PurchaseUserName),
      deletedAt: (() => {
        const v = o.deletedAt ?? o.DeletedAt
        if (v == null || v === '') return null
        return String(v)
      })(),
      deletedByUserId: str(o.deletedByUserId, o.DeletedByUserId) || null,
      deletedByUserName: str(o.deletedByUserName, o.DeletedByUserName) || null
    }
  })
}

/** 已删报价多行单价/币别（后端按换行拼接） */
export function splitRfqDeletedQuoteMultiline(s: string | null | undefined): string[] {
  const t = (s ?? '').trim()
  if (!t) return []
  return t.split(/\r?\n/).map((x) => x.trim()).filter(Boolean)
}

/** 已删报价阶梯列（生产日期/交期/数量）：保留空行以便与单价对齐 */
export function splitRfqDeletedQuoteAlignLines(s: string | null | undefined): string[] {
  const raw = s ?? ''
  if (!raw.trim()) return []
  return raw.split(/\r?\n/).map((x) => x.trim())
}

/** 已删报价单价：固定 4 位小数 */
export function formatRfqDeletedQuotePriceLine(line: string): string {
  const n = Number(String(line).replace(/,/g, ''))
  if (!Number.isFinite(n)) return line.trim() || '—'
  return n.toLocaleString('zh-CN', { minimumFractionDigits: 4, maximumFractionDigits: 4 })
}

// 构建查询字符串
function buildQuery(params: Record<string, any>): string {
  const q = new URLSearchParams()
  Object.entries(params).forEach(([k, v]) => {
    if (v === undefined || v === null || v === '') {
      return
    }
    if (Array.isArray(v)) {
      v.forEach((item) => {
        if (item !== undefined && item !== null && item !== '') {
          q.append(k, String(item))
        }
      })
      return
    }
    q.append(k, String(v))
  })
  return q.toString()
}

export const rfqApi = {
  // ─────────────────────────────────────────────────────────
  // 一、查询功能
  // ─────────────────────────────────────────────────────────
  /** 1. RFQ 主列表分页查询 */
  async searchRFQs(params: RFQSearchRequest): Promise<RFQSearchResponse> {
    const q = buildQuery(params as Record<string, any>)
    return apiClient.get<RFQSearchResponse>(`${BASE}?${q}`)
  },
  /** 2. RFQ 明细列表分页查询（多条件筛选） */
  async searchRFQItems(params: RFQItemSearchRequest): Promise<RFQItemSearchResponse> {
    const q = buildQuery(params as Record<string, any>)
    return apiClient.get<RFQItemSearchResponse>(`${BASE}/items?${q}`)
  },
  /** 3. RFQ 明细详情查看（根据明细ID） */
  async getRFQItemById(itemId: string): Promise<RFQItem> {
    return apiClient.get<RFQItem>(`${BASE}/items/${itemId}`)
  },
  /** 4. 根据主表ID获取明细集合（从详情接口中提取） */
  async getRFQItemsByRFQId(rfqId: string): Promise<RFQItem[]> {
    const rfq = await apiClient.get<any>(`${BASE}/${rfqId}`)
    return rfq?.items || []
  },
  /** 5. 根据ID获取 RFQ 实体 */
  async getRFQById(id: string): Promise<RFQ> {
    return apiClient.get<RFQ>(`${BASE}/${id}`)
  },
  /** 6. RFQ 详情（与后端 GET /rfqs/{id} 一致；历史上曾用 /detail 路由，后端未实现） */
  async getRFQDetail(id: string): Promise<RFQ> {
    return apiClient.get<RFQ>(`${BASE}/${id}`)
  },
  /** 7. 根据条件获取 RFQ 明细简要数据 */
  async getRFQItemsBrief(params: RFQItemSearchRequest): Promise<RFQItem[]> {
    const q = buildQuery(params as Record<string, any>)
    return apiClient.get<RFQItem[]>(`${BASE}/items/brief?${q}`)
  },
  /** 8. RFQ 明细（含主表已加载的明细行；最优报价字段待后端提供独立接口后再切换） */
  async getRFQItemsWithBestQuote(rfqId: string): Promise<RFQItem[]> {
    const rfq = await apiClient.get<{ items?: RFQItem[] }>(`${BASE}/${rfqId}`)
    return rfq?.items ?? []
  },
  /** 9. 获取客户订阅 RFQ 列表 */
  async getCustomerSubscribedRFQs(customerId: string, params: { pageNumber?: number; pageSize?: number }): Promise<RFQSearchResponse> {
    const q = buildQuery({ customerId, ...params })
    return apiClient.get<RFQSearchResponse>(`${BASE}/subscribed?${q}`)
  },
  /** 10. 获取最近询盘（数字大屏） */
  async getRecentInquiries(count?: number): Promise<RFQ[]> {
    return apiClient.get<RFQ[]>(`${BASE}/recent-inquiries?count=${count ?? 10}`)
  },
  // ─────────────────────────────────────────────────────────
  // 二、创建功能
  // ─────────────────────────────────────────────────────────
  /** 11. 新增 RFQ */
  async createRFQ(data: CreateRFQRequest): Promise<RFQ> {
    return apiClient.post<RFQ>(BASE, data)
  },
  /** 新建/编辑需求页：默认分配方式（2/3/5）以及是否允许指定采购 */
  async getDefaultAssignMethod(): Promise<{ assignMethod: number; allowDesignatedPurchaser: boolean }> {
    const res = await apiClient.get<{ assignMethod: number; allowDesignatedPurchaser?: boolean }>(
      `${BASE}/default-assign-method`
    )
    return {
      assignMethod: res.assignMethod,
      allowDesignatedPurchaser: !!res.allowDesignatedPurchaser
    }
  },
  /** 指定采购下拉：报价员池已勾选且在职（不要求采购参数权限） */
  async getQuoterPoolOptions(): Promise<
    Array<{ userId: string; userName: string; realName?: string; departmentName?: string }>
  > {
    const res = await apiClient.get<{
      items?: Array<{ userId?: string; userName?: string; realName?: string; departmentName?: string }>
    }>(`${BASE}/quoter-pool-options`)
    return (res.items ?? [])
      .filter((m) => !!m.userId)
      .map((m) => ({
        userId: String(m.userId),
        userName: String(m.userName ?? ''),
        realName: m.realName,
        departmentName: m.departmentName
      }))
  },
  /** 14. 批量自动生成 RFQ/报价/订单 */
  async batchAutoGenerate(data: CreateRFQRequest[]): Promise<{ successCount: number; failCount: number; errors: string[] }> {
    return apiClient.post(`${BASE}/batch-auto-generate`, data)
  },
  /** 15. 数据验证 */
  async validateRFQ(data: CreateRFQRequest): Promise<{ isValid: boolean; errors: string[] }> {
    return apiClient.post(`${BASE}/validate`, data)
  },
  /** 16. 检查重复物料 */
  async checkDuplicateMaterial(data: CheckDuplicateMaterialRequest): Promise<CheckDuplicateMaterialResponse> {
    return apiClient.post(`${BASE}/check-duplicate`, data)
  },
  // ─────────────────────────────────────────────────────────
  // 三、编辑功能
  // ─────────────────────────────────────────────────────────
  /** 17. 编辑 RFQ 及明细 */
  async updateRFQ(id: string, data: UpdateRFQRequest): Promise<RFQ> {
    return apiClient.put<RFQ>(`${BASE}/${id}`, data)
  },
  /** 18. 修改 RFQ 客户 */
  async updateRFQCustomer(id: string, customerId: string): Promise<void> {
    return apiClient.put(`${BASE}/${id}/customer`, { customerId })
  },
  /** 19. 设置重要标识 */
  async setImportant(id: string, isImportant: boolean): Promise<void> {
    return apiClient.put(`${BASE}/${id}/important`, { isImportant })
  },
  /** 20. 批量新增可替代料 */
  async addAlternativeMaterials(rfqId: string, items: { itemId: string; alternativeMaterialCode: string; alternativeMaterialName: string }[]): Promise<void> {
    return apiClient.post(`${BASE}/${rfqId}/alternative-materials`, { items })
  },
  /** 21. 修改BOM包基本信息 */
  async updateBomInfo(id: string, data: { bomCode?: string; bomVersion?: string; remark?: string }): Promise<void> {
    return apiClient.put(`${BASE}/${id}/bom-info`, data)
  },
  // ─────────────────────────────────────────────────────────
  // 四、删除功能
  // ─────────────────────────────────────────────────────────
  /** 22. 删除 RFQ 明细（单个明细） */
  async deleteRFQItem(rfqId: string, itemId: string): Promise<void> {
    return apiClient.delete(`${BASE}/${rfqId}/items/${itemId}`)
  },
  /** 23. 删除主 RFQ 实体（含明细） */
  async deleteRFQ(id: string): Promise<void> {
    return apiClient.delete(`${BASE}/${id}`)
  },
  // ─────────────────────────────────────────────────────────
  // 五、采购员分配功能
  // ─────────────────────────────────────────────────────────
  /** 24. RFQ 分配采购员（自动/手动） */
  async assignPurchaser(rfqId: string, data: AssignPurchaserRequest): Promise<RFQAssignment> {
    return apiClient.post<RFQAssignment>(`${BASE}/${rfqId}/assign`, data)
  },
  /** 25. 根据 RFQ 编号重新设置采购员 */
  async reassignByCode(rfqCode: string, data: AssignPurchaserRequest): Promise<void> {
    return apiClient.put(`${BASE}/reassign-by-code/${rfqCode}`, data)
  },
  /** 26. 根据 RFQ ID 重新分配采购员 */
  async reassignById(rfqId: string, data: AssignPurchaserRequest): Promise<void> {
    return apiClient.put(`${BASE}/${rfqId}/reassign`, data)
  },
  /** 28. 新增 RFQ 分配（手动指定） */
  async addAssignment(rfqId: string, data: AssignPurchaserRequest): Promise<RFQAssignment> {
    return apiClient.post<RFQAssignment>(`${BASE}/${rfqId}/assignments`, data)
  },
  /** 29. 获取推荐分配采购员 */
  async getRecommendedPurchasers(rfqId: string): Promise<PurchaserInfo[]> {
    return apiClient.get<PurchaserInfo[]>(`${BASE}/${rfqId}/recommended-purchasers`)
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
  // 六、RFQ 状态管理
  // ─────────────────────────────────────────────────────────
  /** 33. 变更 RFQ 流转状态 */
  async updateFlowStatus(id: string, data: UpdateRFQStatusRequest): Promise<void> {
    return apiClient.put(`${BASE}/${id}/flow-status`, data)
  },
  /** 35. 更新 RFQ 状态（批量） */
  async batchUpdateStatus(ids: string[], status: number): Promise<void> {
    return apiClient.put(`${BASE}/batch-status`, { ids, status })
  },
  /** 36. 更新 RFQ 明细状态（批量） */
  async batchUpdateItemStatus(itemIds: string[], status: number): Promise<void> {
    return apiClient.put(`${BASE}/items/batch-status`, { itemIds, status })
  },
  /** 37. 设置采购员处理状态 */
  async setPurchaserHandleStatus(rfqId: string, handleStatus: number): Promise<void> {
    return apiClient.put(`${BASE}/${rfqId}/purchaser-handle-status`, { handleStatus })
  },
  /** 38. 更新 RFQ 报价读取状态（标记已读） */
  async markQuoteRead(rfqId: string): Promise<void> {
    return apiClient.put(`${BASE}/${rfqId}/quote-read`)
  },
  /** 39. 需求主表及明细字段变更日志 */
  async getChangeLogs(rfqId: string): Promise<RfqFieldChangeLogRow[]> {
    return apiClient.get<RfqFieldChangeLogRow[]>(`${BASE}/${rfqId}/change-logs`)
  },
  /** 本需求各明细行上已删除的报价 */
  async getDeletedQuotes(rfqId: string): Promise<RfqItemDeletedQuoteRow[]> {
    const rows = await apiClient.get<unknown>(`${BASE}/${encodeURIComponent(rfqId)}/deleted-quotes`)
    return normalizeRfqItemDeletedQuoteRows(rows)
  },
  /** 指定需求明细行上已删除的报价 */
  async getDeletedQuotesForItem(itemId: string): Promise<RfqItemDeletedQuoteRow[]> {
    const rows = await apiClient.get<unknown>(
      `${BASE}/items/${encodeURIComponent(itemId)}/deleted-quotes`
    )
    return normalizeRfqItemDeletedQuoteRows(rows)
  },
  /** 40. 获取 RFQ 关闭记录 */
  async getCloseRecords(rfqId: string): Promise<RFQCloseRecord[]> {
    return apiClient.get<RFQCloseRecord[]>(`${BASE}/${rfqId}/close-records`)
  },
  /** 40. 新增关闭记录 */
  async addCloseRecord(rfqId: string, data: CloseRFQRequest): Promise<RFQCloseRecord> {
    return apiClient.post<RFQCloseRecord>(`${BASE}/${rfqId}/close-records`, data)
  },
  /** 41. 标记需求明细为查无报价（status 0→5） */
  async markNoQuote(itemId: string): Promise<{ id: string; status: number }> {
    return apiClient.post<{ id: string; status: number }>(`${BASE}/items/${itemId}/mark-no-quote`, {})
  },
  async getRecycleBin(params: RFQSearchRequest = {}): Promise<RFQSearchResponse> {
    const q = buildQuery(params as Record<string, any>)
    return apiClient.get<RFQSearchResponse>(`${BASE}/recycle-bin?${q}`)
  },
  async getRecycleRFQById(id: string): Promise<RFQ> {
    return apiClient.get<RFQ>(`${BASE}/recycle-bin/${id}`)
  },
  async restoreRFQ(id: string): Promise<void> {
    await apiClient.post(`${BASE}/${id}/restore`)
  },
}
