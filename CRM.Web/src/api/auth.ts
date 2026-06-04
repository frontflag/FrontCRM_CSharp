/**
 * 业务员 / 采购员选人数据范围见：
 * document/PRD/规范/业务规范/业务员与采购员下拉规范.md
 */
import apiClient from './client'

export interface LoginRequest {
  userName: string
  password: string
}

export interface RegisterRequest {
  userName: string
  email: string
  password: string
}

export interface AuthResponse {
  token: string
  userName: string
  email: string
  userId: string
  isSysAdmin: boolean
  roleCodes: string[]
  permissionCodes: string[]
  departmentIds: string[]
}

export interface ApiResponse<T> {
  success: boolean
  message: string
  data: T
  errorCode: number
}

export interface SalesUserTreeNode {
  value: string
  label: string
  isUser: boolean
  /** 人员节点：登录账号（与 cascader Label 通常一致） */
  userName?: string
  /** 人员节点：真实姓名 */
  realName?: string
  children?: SalesUserTreeNode[]
}

/** 销售/采购组织树节点（结构相同） */
export type OrgUserTreeNode = SalesUserTreeNode

/** 采购员树展平后的下拉项（与 /purchase-users-tree 一致：仅采购部门；采购员仅自己及下属） */
export type PurchaseUserSelectOption = { id: string; userName: string; realName?: string; label?: string }

/** 客户筛选「业务员」等：仅销售部门；销售账号仅自己及下属（与 /sales-users-tree 一致） */
export type SalesUserSelectOption = { id: string; userName: string; realName?: string; label?: string }

/** 采购订单「采购助理」：采购运营部门职员（/purchase-ops-staff-users） */
export type PurchaseOpsStaffUserOption = { id: string; userName: string; realName?: string; label: string }

/** 采购订单「采购员」全量下拉：采购相关部门职员 + 全部 SYS_ADMIN（/purchase-dept-staff-users） */
export type PurchaseDeptStaffUserOption = { id: string; userName: string; realName?: string; label: string }

/** 销售订单「销售助理」：商务相关部门职员（/business-ops-staff-users） */
export type BusinessOpsStaffUserOption = { id: string; userName: string; realName?: string; label: string }

/** 销售订单「销售员」全量下拉：销售相关部门职员 + 全部 SYS_ADMIN（/sales-dept-staff-users） */
export type SalesDeptStaffUserOption = { id: string; userName: string; realName?: string; label: string }

export function flattenSalesUserTreeForSelect(nodes: SalesUserTreeNode[]): SalesUserSelectOption[] {
  const out: SalesUserSelectOption[] = []
  const walk = (ns: SalesUserTreeNode[]) => {
    for (const n of ns) {
      if (n.isUser) {
        const userName = n.userName ?? n.label
        out.push({
          id: n.value,
          userName,
          realName: n.realName,
          label: n.realName || n.label
        })
      }
      if (n.children?.length) walk(n.children)
    }
  }
  if (nodes?.length) walk(nodes)
  return out
}

export function flattenOrgUserTreeLeaves(nodes: OrgUserTreeNode[]): PurchaseUserSelectOption[] {
  const out: PurchaseUserSelectOption[] = []
  const walk = (ns: OrgUserTreeNode[]) => {
    for (const n of ns) {
      if (n.isUser) out.push({ id: n.value, userName: n.label, label: n.label })
      if (n.children?.length) walk(n.children)
    }
  }
  if (nodes?.length) walk(nodes)
  return out
}

export const authApi = {
  login(data: LoginRequest): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post('/api/v1/auth/login', data)
  },

  register(data: RegisterRequest): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post('/api/v1/auth/register', data)
  },

  /** 系统管理员模拟登录为员工（需当前 Bearer 为 SYS_ADMIN）；userId 放 Body，避免路径在代理下 404 */
  impersonate(userId: string): Promise<AuthResponse> {
    return apiClient.post('/api/v1/auth/impersonate', { userId })
  },

  getCurrentUser(): Promise<ApiResponse<any>> {
    return apiClient.get('/api/v1/auth/me')
  },

  getPermissionSummary(): Promise<ApiResponse<any>> {
    return apiClient.get('/api/v1/auth/permission-summary')
  },

  /** 全站启用用户；勿用于客户业务员/供应商采购员等选人（见业务规范文档） */
  getUsers(): Promise<ApiResponse<Array<{ id: string; label: string; userName: string; realName?: string }>>> {
    return apiClient.get('/api/v1/auth/users')
  },

  getSalesUsersTree(): Promise<ApiResponse<SalesUserTreeNode[]>> {
    return apiClient.get('/api/v1/auth/sales-users-tree')
  },

  getPurchaseUsersTree(): Promise<ApiResponse<OrgUserTreeNode[]>> {
    return apiClient.get('/api/v1/auth/purchase-users-tree')
  },

  /** 采购订单采购助理下拉：采购运营部门全部启用职员 */
  async getPurchaseOpsStaffUsers(): Promise<PurchaseOpsStaffUserOption[]> {
    const rows = (await apiClient.get('/api/v1/auth/purchase-ops-staff-users')) as PurchaseOpsStaffUserOption[]
    return Array.isArray(rows) ? rows : []
  },

  /** 采购订单采购员全量下拉：采购相关部门全部启用职员（不含采购运营部） */
  async getPurchaseDeptStaffUsers(): Promise<PurchaseDeptStaffUserOption[]> {
    const rows = (await apiClient.get('/api/v1/auth/purchase-dept-staff-users')) as PurchaseDeptStaffUserOption[]
    return Array.isArray(rows) ? rows : []
  },

  /** 采购助理新建订单：sys_relation_map type=101 已配置的采购员 */
  async getPurchaseOrderMappedPurchasers(assistantUserId?: string): Promise<PurchaseDeptStaffUserOption[]> {
    const params = assistantUserId?.trim() ? { assistantUserId: assistantUserId.trim() } : undefined
    const rows = (await apiClient.get('/api/v1/auth/purchase-order-mapped-purchasers', {
      params
    })) as PurchaseDeptStaffUserOption[]
    return Array.isArray(rows) ? rows : []
  },

  /** 销售订单销售助理下拉：商务相关部门全部启用职员 */
  async getBusinessOpsStaffUsers(): Promise<BusinessOpsStaffUserOption[]> {
    const rows = (await apiClient.get('/api/v1/auth/business-ops-staff-users')) as BusinessOpsStaffUserOption[]
    return Array.isArray(rows) ? rows : []
  },

  /** 销售订单销售员全量下拉：销售相关部门全部启用职员 */
  async getSalesDeptStaffUsers(): Promise<SalesDeptStaffUserOption[]> {
    const rows = (await apiClient.get('/api/v1/auth/sales-dept-staff-users')) as SalesDeptStaffUserOption[]
    return Array.isArray(rows) ? rows : []
  },

  /** 销售助理新建订单：sys_relation_map type=100 已配置的销售员 */
  async getSalesOrderMappedSalespersons(assistantUserId?: string): Promise<SalesDeptStaffUserOption[]> {
    const params = assistantUserId?.trim() ? { assistantUserId: assistantUserId.trim() } : undefined
    const rows = (await apiClient.get('/api/v1/auth/sales-order-mapped-salespersons', {
      params
    })) as SalesDeptStaffUserOption[]
    return Array.isArray(rows) ? rows : []
  },

  getLogisticsUsersTree(): Promise<ApiResponse<SalesUserTreeNode[]>> {
    return apiClient.get('/api/v1/auth/logistics-users-tree')
  },

  /** 供应商/采购筛选下拉：仅采购部门用户；采购员账号仅自己及下属 */
  async getPurchaseUsersForSelect(): Promise<PurchaseUserSelectOption[]> {
    const tree = (await apiClient.get<OrgUserTreeNode[]>('/api/v1/auth/purchase-users-tree')) as OrgUserTreeNode[]
    return flattenOrgUserTreeLeaves(Array.isArray(tree) ? tree : [])
  },

  /** 客户筛选业务员：仅销售部门用户；销售账号仅自己及下属 */
  async getSalesUsersForSelect(): Promise<SalesUserSelectOption[]> {
    const tree = (await apiClient.get<SalesUserTreeNode[]>('/api/v1/auth/sales-users-tree')) as SalesUserTreeNode[]
    return flattenSalesUserTreeForSelect(Array.isArray(tree) ? tree : [])
  },

  /** 质检「质检人」：仅物流相关部门用户（身份含物流或部门名匹配）；权限范围与业务员树类似 */
  async getLogisticsUsersForSelect(): Promise<SalesUserSelectOption[]> {
    const tree = (await apiClient.get<SalesUserTreeNode[]>('/api/v1/auth/logistics-users-tree')) as SalesUserTreeNode[]
    return flattenSalesUserTreeForSelect(Array.isArray(tree) ? tree : [])
  }
}
