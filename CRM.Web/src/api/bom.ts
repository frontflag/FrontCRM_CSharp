// ============================================================
// BOM（批量快速报价）管理模块 - API 调用层
// 对应后端路由: /api/v1/boms
// ============================================================
import apiClient from './client'
import type {
  BOM,
  BOMItem,
  BOMSearchRequest,
  BOMSearchResponse,
  CreateBOMRequest,
  UpdateBOMRequest,
  AutoQuoteBOMRequest,
  AutoQuoteBOMResponse,
  ManualQuoteBOMItemRequest,
  DeleteBOMRequest,
} from '@/types/bom'

const BASE = '/api/v1/boms'

function buildQuery(params: Record<string, any>): string {
  const q = new URLSearchParams()
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') {
      q.append(k, String(v))
    }
  })
  return q.toString()
}

export const bomApi = {
  // ─────────────────────────────────────────────────────────
  // 一、查询
  // ─────────────────────────────────────────────────────────

  /** BOM 主列表分页查询 */
  async searchBOMs(params: BOMSearchRequest): Promise<BOMSearchResponse> {
    const q = buildQuery(params as Record<string, any>)
    return apiClient.get<BOMSearchResponse>(`${BASE}?${q}`)
  },

  /** 获取 BOM 详情（含明细） */
  async getBOMById(id: string): Promise<BOM> {
    return apiClient.get<BOM>(`${BASE}/${id}`)
  },

  /** 获取 BOM 明细列表 */
  async getBOMItems(bomId: string): Promise<BOMItem[]> {
    const bom = await apiClient.get<any>(`${BASE}/${bomId}`)
    return bom?.items || []
  },

  // ─────────────────────────────────────────────────────────
  // 二、新建 / 更新 / 删除
  // ─────────────────────────────────────────────────────────

  /** 新建 BOM（含明细） */
  async createBOM(data: CreateBOMRequest): Promise<BOM> {
    return apiClient.post<BOM>(BASE, data)
  },

  /** 更新 BOM 主体信息 */
  async updateBOM(id: string, data: UpdateBOMRequest): Promise<BOM> {
    return apiClient.put<BOM>(`${BASE}/${id}`, data)
  },

  /** 删除单条 BOM */
  async deleteBOM(id: string): Promise<void> {
    return apiClient.delete<void>(`${BASE}/${id}`)
  },

  /** 批量删除 BOM */
  async deleteBOMs(data: DeleteBOMRequest): Promise<void> {
    return apiClient.post<void>(`${BASE}/batch-delete`, data)
  },

  // ─────────────────────────────────────────────────────────
  // 三、报价功能
  // ─────────────────────────────────────────────────────────

  /** 一键快速报价（系统自动报价） */
  async autoQuote(data: AutoQuoteBOMRequest): Promise<AutoQuoteBOMResponse> {
    return apiClient.post<AutoQuoteBOMResponse>(`${BASE}/${data.bomId}/auto-quote`, {
      itemIds: data.itemIds,
    })
  },

  /** 人工报价某条明细 */
  async manualQuoteItem(
    bomId: string,
    itemId: string,
    data: ManualQuoteBOMItemRequest
  ): Promise<BOMItem> {
    return apiClient.post<BOMItem>(`${BASE}/${bomId}/items/${itemId}/manual-quote`, data)
  },

  /** 修改某条明细的报价 */
  async updateItemQuote(
    bomId: string,
    itemId: string,
    data: ManualQuoteBOMItemRequest
  ): Promise<BOMItem> {
    return apiClient.put<BOMItem>(`${BASE}/${bomId}/items/${itemId}/quote`, data)
  },

  // ─────────────────────────────────────────────────────────
  // 四、状态变更
  // ─────────────────────────────────────────────────────────

  /** 更新 BOM 状态 */
  async updateStatus(id: string, status: number, remark?: string): Promise<BOM> {
    return apiClient.put<BOM>(`${BASE}/${id}/status`, { status, remark })
  },
}
