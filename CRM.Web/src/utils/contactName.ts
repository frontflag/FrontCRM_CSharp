/** 客户/供应商联系人姓名：中文名 + 英文名，至少填一项 */

export type ContactNameFields = {
  cName?: string
  eName?: string
}

export function contactDisplayName(contact: ContactNameFields & { contactName?: string }): string {
  const c = (contact.cName ?? '').trim()
  const e = (contact.eName ?? '').trim()
  if (c) return c
  if (e) return e
  return (contact.contactName ?? '').trim()
}

export function hasContactName(contact: ContactNameFields): boolean {
  return !!(contact.cName ?? '').trim() || !!(contact.eName ?? '').trim()
}

export const CONTACT_NAME_AT_LEAST_ONE_MESSAGE = '中文名与英文名至少填写一项'

/** Element Plus：中文名、英文名至少填一项（绑定到 cName 表单项） */
export function validateContactNameAtLeastOne(
  _rule: unknown,
  _value: unknown,
  callback: (error?: Error) => void,
  form: ContactNameFields
) {
  if (hasContactName(form)) {
    callback()
    return
  }
  callback(new Error(CONTACT_NAME_AT_LEAST_ONE_MESSAGE))
}

/** 内嵌联系人表格行校验 */
export function contactRowNameValidator(contact: ContactNameFields) {
  return (_rule: unknown, _value: unknown, callback: (error?: Error) => void) => {
    validateContactNameAtLeastOne(_rule, _value, callback, contact)
  }
}

/** 从 API 响应 / 旧 contactName 字段填充表单 */
export function splitContactNamesFromApi(contact: {
  cName?: string
  eName?: string
  contactName?: string
}): { cName: string; eName: string } {
  const cName = (contact.cName ?? '').trim()
  const eName = (contact.eName ?? '').trim()
  if (cName || eName) {
    return { cName, eName }
  }
  const legacy = (contact.contactName ?? '').trim()
  return { cName: legacy, eName: '' }
}
