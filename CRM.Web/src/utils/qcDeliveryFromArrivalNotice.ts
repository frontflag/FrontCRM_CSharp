/** 物流字典 ItemCode → 当前语言展示名；未知码回退原文。 */
export function logisticsDictLabel(
  options: ReadonlyArray<{ label: string; value: string }>,
  code?: string | number | null
): string {
  const c = String(code ?? '').trim()
  if (!c) return ''
  return options.find((o) => String(o.value).trim() === c)?.label ?? c
}

/** 质检「送货信息」三字段：只读带入到货通知，不落质检表。 */
export function qcDeliveryFromArrivalNotice(
  notice: {
    shipmentMethod?: string | null
    expressCompany?: string | null
    courierTrackingNo?: string | null
  },
  arrivalOptions: ReadonlyArray<{ label: string; value: string }>,
  expressOptions: ReadonlyArray<{ label: string; value: string }>
): { deliveryMethod: string; expressMethod: string; expressNo: string } {
  return {
    deliveryMethod: logisticsDictLabel(arrivalOptions, notice.shipmentMethod),
    expressMethod: logisticsDictLabel(expressOptions, notice.expressCompany),
    expressNo: String(notice.courierTrackingNo ?? '').trim()
  }
}
