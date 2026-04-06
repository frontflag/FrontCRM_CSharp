import apiClient from './client'

const getYYMMDD = (d: Date) => {
  const yy = String(d.getFullYear()).slice(-2)
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${yy}${mm}${dd}`
}

const random4 = () => String(Math.floor(Math.random() * 10000)).padStart(4, '0')

/** 与后端 CreateQuoteItemRequest / 前端表单字段对齐 */
function buildItemsFromQuoteCreateForm(form: Record<string, unknown>): Record<string, unknown>[] {
  const rows = (form.quotePriceRows as Record<string, unknown>[] | undefined) || []
  const items: Record<string, unknown>[] = []
  for (const r of rows) {
    const qty = Number(r.quantity)
    const up = r.unitPrice != null && r.unitPrice !== '' ? Number(r.unitPrice) : NaN
    if (qty < 1 || Number.isNaN(up)) continue
    items.push({
      vendorId: form.vendorId || undefined,
      vendorName: (form.vendorName as string) || undefined,
      vendorCode: undefined,
      contactId: form.vendorContactId || undefined,
      contactName: (form.contactName as string) || undefined,
      priceType: (form.priceType as string) || undefined,
      expiryDate: form.expiryDate ? new Date(String(form.expiryDate)).toISOString() : undefined,
      mpn: (form.mpn as string) || undefined,
      brand: (form.brand as string) || undefined,
      brandOrigin: (form.brandOrigin as string) || undefined,
      dateCode: (form.productionDate as string) || undefined,
      leadTime: (form.leadTime as string) || undefined,
      labelType: Number(form.labelType ?? 0),
      waferOrigin: Number(form.waferOrigin ?? 2),
      packageOrigin: Number(form.packageOrigin ?? 2),
      freeShipping: Boolean(form.freeShipping),
      currency: Number(r.currency ?? 1),
      quantity: qty,
      unitPrice: up,
      convertedPrice:
        r.convertedPrice != null && r.convertedPrice !== '' ? Number(r.convertedPrice) : undefined,
      minPackageQty: Number(form.minPackageQty ?? 0),
      minPackageUnit: undefined,
      stockQty: Number(form.stockQty ?? 0),
      moq: Number(form.moq ?? 0),
      remark: (form.remark as string) || undefined,
      status: 0
    })
  }
  return items
}

function mapToCreateQuoteRequest(form: Record<string, unknown>): Record<string, unknown> {
  const items = buildItemsFromQuoteCreateForm(form)
  if (items.length === 0) {
    throw new Error('至少需要一条有效的供应商报价明细（数量≥1 且单价有效）')
  }

  const quoteCode =
    (form.quoteCode && String(form.quoteCode).trim()) ||
    `QT${getYYMMDD(new Date())}${random4()}`

  return {
    quoteCode,
    rfqId: form.rfqId || null,
    rfqItemId: form.rfqItemId || null,
    mpn: (form.mpn as string) || null,
    customerId: form.customerId || null,
    salesUserId: form.salesUserId || null,
    purchaseUserId: form.purchaseUserId || null,
    quoteDate: (form.quoteDate as string) || new Date().toISOString().slice(0, 10),
    status: 0,
    remark: (form.remark as string) || null,
    items
  }
}

function mapQuoteEditSimpleToUpdate(body: Record<string, unknown>): Record<string, unknown> {
  const rawItems = (body.items as Record<string, unknown>[]) || []
  const items = rawItems.map((it) => ({
    vendorId: it.vendorId,
    vendorName: it.vendorName,
    vendorCode: it.vendorCode,
    contactId: it.contactId,
    contactName: it.contactName,
    priceType: it.priceType,
    mpn: it.mpn ?? body.mpn,
    brand: it.brand,
    brandOrigin: it.brandOrigin,
    dateCode: it.dateCode,
    leadTime: it.leadTime,
    labelType: Number(it.labelType ?? 2),
    waferOrigin: Number(it.waferOrigin ?? 2),
    packageOrigin: Number(it.packageOrigin ?? 2),
    freeShipping: Boolean(it.freeShipping ?? false),
    currency: Number(it.currency ?? 1),
    quantity: Number(it.quantity),
    unitPrice: Number(it.unitPrice),
    convertedPrice: it.convertedPrice != null ? Number(it.convertedPrice) : undefined,
    minPackageQty: Number(it.minPackageQty ?? 0),
    minPackageUnit: it.minPackageUnit,
    stockQty: Number(it.stockQty ?? 0),
    moq: Number(it.moq ?? 0),
    remark: it.remark,
    status: Number(it.status ?? 0)
  }))
  return {
    mpn: body.mpn,
    remark: body.remark ?? null,
    salesUserId: body.salesUserId ?? null,
    purchaseUserId: body.purchaseUserId ?? null,
    items
  }
}

function mapQuoteCreateFormToUpdate(form: Record<string, unknown>): Record<string, unknown> {
  const items = buildItemsFromQuoteCreateForm(form)
  return {
    mpn: form.mpn,
    remark: form.remark ?? null,
    salesUserId: form.salesUserId ?? null,
    purchaseUserId: form.purchaseUserId ?? null,
    items
  }
}

export const quoteApi = {
  /**
   * GET /api/v1/quotes（后端暂无筛选参数，在此做前端过滤）
   * 返回形状与旧 mock 一致：{ data, total }
   */
  async getList(params?: {
    keyword?: string
    status?: number
    rfqItemId?: string | null
  }) {
    const rows = await apiClient.get<unknown[]>('/api/v1/quotes')
    const list = Array.isArray(rows) ? rows : []
    let result = [...list] as Record<string, unknown>[]
    if (params?.keyword) {
      const kw = params.keyword.toLowerCase()
      result = result.filter((q) => {
        const items = (q.items ?? q.Items) as Record<string, unknown>[] | undefined
        const it0 = Array.isArray(items) && items.length > 0 ? items[0] : null
        const parts = [
          q.quoteCode,
          q.quoteNumber,
          q.rfqCode,
          q.mpn,
          q.customerName,
          q.salesUserName,
          it0?.brand,
          it0?.Brand,
          it0?.unitPrice,
          it0?.UnitPrice
        ]
          .filter((x) => x != null)
          .map((x) => String(x).toLowerCase())
        return parts.some((s) => s.includes(kw))
      })
    }
    if (params?.status !== undefined && params?.status !== null) {
      result = result.filter((q) => Number(q.status) === Number(params.status))
    }
    if (params?.rfqItemId != null && String(params.rfqItemId).trim() !== '') {
      const rid = String(params.rfqItemId).trim()
      result = result.filter((q) => String(q.rfqItemId ?? q.RfqItemId ?? '') === rid)
    }
    return { data: result, total: result.length }
  },

  async getById(id: string) {
    const data = await apiClient.get<unknown>(`/api/v1/quotes/${encodeURIComponent(id)}`)
    return { data }
  },

  async create(body: Record<string, unknown>) {
    const payload = mapToCreateQuoteRequest(body)
    const created = await apiClient.post<unknown>('/api/v1/quotes', payload)
    return { data: created }
  },

  async update(id: string, body: Record<string, unknown>) {
    const payload =
      body.quotePriceRows != null && Array.isArray(body.quotePriceRows)
        ? mapQuoteCreateFormToUpdate(body)
        : mapQuoteEditSimpleToUpdate(body)
    const updated = await apiClient.put<unknown>(`/api/v1/quotes/${encodeURIComponent(id)}`, payload)
    return { data: updated }
  },

  async delete(id: string) {
    await apiClient.delete(`/api/v1/quotes/${encodeURIComponent(id)}`)
  },

  async updateStatus(id: string, status: number) {
    await apiClient.patch(`/api/v1/quotes/${encodeURIComponent(id)}/status`, { status })
  }
}

export default quoteApi
