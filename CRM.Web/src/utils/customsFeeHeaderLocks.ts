export type FeeHeaderPanelMode =
  | 'readonly_void'
  | 'readonly_completed'
  | 'readonly_locked'
  | 'blocked_no_p0'
  | 'editable'

export type FeeHeaderLockInput = {
  panelMode: FeeHeaderPanelMode
  canWrite: boolean
  /** 系统管理员或平台管理员，且采购字段未脱敏。 */
  canCorrectLocked: boolean
  /** 单头已有大于 0 的代理费率。1 表示 0% 代理费，仍算已有。 */
  hasSavedAgencyRate: boolean
}

export type FeeHeaderLocks = {
  exchangeRate: boolean
  agencyRate: boolean
  purchaseRatio: boolean
}

/**
 * 锁表示这次保存不会改数据库里的这一列。
 * 列会被改写就不加锁，即使页头显示的数不是最终写入的数。
 */
export function resolveFeeHeaderLocks(input: FeeHeaderLockInput): FeeHeaderLocks {
  const canMaintain = input.panelMode === 'editable' || input.panelMode === 'blocked_no_p0'
  const fullRecalc =
    input.canWrite &&
    input.panelMode !== 'readonly_void' &&
    (canMaintain || input.canCorrectLocked)
  const preserveAgency =
    input.canCorrectLocked &&
    input.hasSavedAgencyRate &&
    (input.panelMode === 'readonly_locked' || input.panelMode === 'readonly_completed')

  return {
    exchangeRate: !(input.canWrite && canMaintain),
    agencyRate: !(fullRecalc && !preserveAgency),
    purchaseRatio: !fullRecalc
  }
}
