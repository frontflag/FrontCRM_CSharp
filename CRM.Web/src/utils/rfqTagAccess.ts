type RfqTagUiUser = {
  id?: string
  isSysAdmin?: boolean
  saleDataScope?: number
} | null | undefined

function normalizeUserId(v: unknown): string {
  return String(v ?? '').trim().toLowerCase()
}

/** 是否展示 RFQ 标签 UI（列表列/筛选/详情）；采购等销售范围 4 不可见。 */
export function canUseRfqTagUi(user: RfqTagUiUser): boolean {
  if (!user) return false
  if (user.isSysAdmin) return true
  return (user.saleDataScope ?? 1) !== 4
}

export function resolveRfqCanViewTags(rfq: {
  canViewRfqTags?: boolean
  CanViewRfqTags?: boolean
} | null | undefined): boolean {
  if (!rfq) return false
  return rfq.canViewRfqTags === true || rfq.CanViewRfqTags === true
}

export function resolveRfqCanEditTags(rfq: {
  canEditRfqTags?: boolean
  CanEditRfqTags?: boolean
  createByUserId?: string
  CreateByUserId?: string
  salesUserId?: string
  SalesUserId?: string
} | null | undefined): boolean {
  if (!rfq) return false
  return rfq.canEditRfqTags === true || rfq.CanEditRfqTags === true
}

/** 与后端 CanEditRfqTagsAsync 一致：创建人 + 当前业务员（管理员仅可查看）。 */
export function resolveRfqCanEditTagsForUser(
  rfq: {
    canEditRfqTags?: boolean
    CanEditRfqTags?: boolean
    createByUserId?: string
    CreateByUserId?: string
    salesUserId?: string
    SalesUserId?: string
  } | null | undefined,
  user: RfqTagUiUser
): boolean {
  if (resolveRfqCanEditTags(rfq)) return true
  if (!rfq || !user?.id || user.isSysAdmin) return false
  const uid = normalizeUserId(user.id)
  if (!uid) return false
  const creator = normalizeUserId(rfq.createByUserId ?? rfq.CreateByUserId)
  const sales = normalizeUserId(rfq.salesUserId ?? rfq.SalesUserId)
  if (creator && creator === uid) return true
  if (sales && sales === uid) return true
  return false
}

export function normalizeRfqTags(
  rfq: {
    tags?: Array<{ id: string; name: string; color?: string; type?: number }>
    Tags?: Array<{ id: string; name: string; color?: string; type?: number }>
  } | null | undefined
) {
  return rfq?.tags ?? rfq?.Tags ?? []
}
