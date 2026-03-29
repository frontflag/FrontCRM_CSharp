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

  /** 供应商/采购筛选下拉：仅采购部门用户；采购员账号仅自己及下属 */
  async getPurchaseUsersForSelect(): Promise<PurchaseUserSelectOption[]> {
    const tree = (await apiClient.get<OrgUserTreeNode[]>('/api/v1/auth/purchase-users-tree')) as OrgUserTreeNode[]
    return flattenOrgUserTreeLeaves(Array.isArray(tree) ? tree : [])
  },

  /** 客户筛选业务员：仅销售部门用户；销售账号仅自己及下属 */
  async getSalesUsersForSelect(): Promise<SalesUserSelectOption[]> {
    const tree = (await apiClient.get<SalesUserTreeNode[]>('/api/v1/auth/sales-users-tree')) as SalesUserTreeNode[]
    return flattenSalesUserTreeForSelect(Array.isArray(tree) ? tree : [])
  }
}
