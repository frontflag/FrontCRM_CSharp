import type { CustomsDeclarationDetailDto } from '@/api/customs'

export interface CustomsArrivalDisabledHintContent {
  summary: string
  details: string[]
  nextStep: string
}

type TranslateFn = (key: string, params?: Record<string, unknown>) => string

interface PrerequisiteCheck {
  done: boolean
  detail?: string
  nextStepKey: string
}

function isVoided(detail: CustomsDeclarationDetailDto): boolean {
  return detail.internalStatus === -1
}

function itemCount(detail: CustomsDeclarationDetailDto): number {
  return detail.items?.length ?? 0
}

function arrivalCompleted(detail: CustomsDeclarationDetailDto): boolean {
  const total = itemCount(detail)
  if (total <= 0) return false
  const pending = detail.pendingArrivalNotifyCount ?? 0
  const existing = detail.existingArrivalNotifyCount ?? 0
  if (existing >= total && pending === 0) return true
  return !detail.canCreateArrivalNotifies && existing > 0 && pending === 0
}

function collectPrerequisites(detail: CustomsDeclarationDetailDto, t: TranslateFn): PrerequisiteCheck[] {
  const checks: PrerequisiteCheck[] = []

  checks.push({
    done: !isVoided(detail),
    detail: isVoided(detail) ? t('customsPages.declarations.opsPanel.prereqVoided') : undefined,
    nextStepKey: 'customsPages.declarations.opsPanel.nextVoided'
  })
  if (isVoided(detail)) return checks

  checks.push({
    done: detail.customsClearanceStatus === 100,
    detail: detail.customsClearanceStatus !== 100
      ? t('customsPages.declarations.opsPanel.prereqClearance')
      : undefined,
    nextStepKey: 'customsPages.declarations.opsPanel.nextClearance'
  })

  checks.push({
    done: Number(detail.exchangeRate) > 0,
    detail: Number(detail.exchangeRate) <= 0 ? t('customsPages.declarations.opsPanel.prereqExchangeRate') : undefined,
    nextStepKey: 'customsPages.declarations.opsPanel.nextExchangeRate'
  })

  checks.push({
    done: Boolean(detail.feesCalculatedAt),
    detail: !detail.feesCalculatedAt ? t('customsPages.declarations.opsPanel.prereqFeesCalculated') : undefined,
    nextStepKey: 'customsPages.declarations.opsPanel.nextFeesCalculated'
  })

  const toWh = String(detail.toWarehouseId ?? '').trim()
  checks.push({
    done: toWh.length > 0,
    detail: !toWh ? t('customsPages.declarations.opsPanel.prereqToWarehouse') : undefined,
    nextStepKey: 'customsPages.declarations.opsPanel.nextToWarehouse'
  })

  const total = itemCount(detail)
  checks.push({
    done: total > 0,
    detail: total <= 0 ? t('customsPages.declarations.opsPanel.prereqHasItems') : undefined,
    nextStepKey: 'customsPages.declarations.opsPanel.nextHasItems'
  })

  if (total > 0 && detail.feesCalculatedAt) {
    for (const item of detail.items ?? []) {
      if (Number(item.originalPurchasePrice) <= 0) {
        checks.push({
          done: false,
          detail: t('customsPages.declarations.opsPanel.prereqLinePurchasePrice', { line: item.lineNo }),
          nextStepKey: 'customsPages.declarations.opsPanel.nextLineFees'
        })
        break
      }
      if (!String(item.purchaseCostParamId ?? '').trim() || Number(item.purchaseRatio) <= 0) {
        checks.push({
          done: false,
          detail: t('customsPages.declarations.opsPanel.prereqLinePurchaseRatio', { line: item.lineNo }),
          nextStepKey: 'customsPages.declarations.opsPanel.nextLineFees'
        })
        break
      }
      if (Number(item.dutyRate) < 0) {
        checks.push({
          done: false,
          detail: t('customsPages.declarations.opsPanel.prereqLineDutyRate', { line: item.lineNo }),
          nextStepKey: 'customsPages.declarations.opsPanel.nextLineFees'
        })
        break
      }
      if (Number(item.dutyRate) === 0 && !String(item.hsCode ?? '').trim()) {
        checks.push({
          done: false,
          detail: t('customsPages.declarations.opsPanel.prereqLineHsCode', { line: item.lineNo }),
          nextStepKey: 'customsPages.declarations.opsPanel.nextLineFees'
        })
        break
      }
      if (Number(item.taxIncludedUnitPrice) <= 0) {
        checks.push({
          done: false,
          detail: t('customsPages.declarations.opsPanel.prereqLineTaxPrice', { line: item.lineNo }),
          nextStepKey: 'customsPages.declarations.opsPanel.nextLineFees'
        })
        break
      }
    }
  }

  const stockOutPending = (detail.items ?? []).some((item) => !String(item.arrivalNotifyCode ?? '').trim())
  const block = String(detail.arrivalNotifyBlockReason ?? '').trim()
  const stockOutBlocked =
    block.includes('报关出库') ||
    block.toLowerCase().includes('stock-out') ||
    block.toLowerCase().includes('stock out')
  if (
    stockOutPending &&
    (stockOutBlocked ||
      (detail.customsClearanceStatus === 100 &&
        Boolean(detail.feesCalculatedAt) &&
        Number(detail.exchangeRate) > 0 &&
        String(detail.toWarehouseId ?? '').trim().length > 0 &&
        !detail.canCreateArrivalNotifies))
  ) {
    const alreadyListed = checks.some(
      (c) => c.nextStepKey === 'customsPages.declarations.opsPanel.nextStockOutDone'
    )
    if (!alreadyListed) {
      checks.push({
        done: false,
        detail: t('customsPages.declarations.opsPanel.prereqStockOutDone'),
        nextStepKey: 'customsPages.declarations.opsPanel.nextStockOutDone'
      })
    }
  }

  return checks
}

export function isCustomsArrivalOpsCompleted(detail: CustomsDeclarationDetailDto | null): boolean {
  if (!detail) return false
  return arrivalCompleted(detail)
}

export function buildCustomsArrivalDisabledHintContent(
  detail: CustomsDeclarationDetailDto | null,
  t: TranslateFn
): CustomsArrivalDisabledHintContent | null {
  if (!detail || detail.canCreateArrivalNotifies) return null
  if (arrivalCompleted(detail)) return null

  const block = String(detail.arrivalNotifyBlockReason ?? '').trim()
  const checks = collectPrerequisites(detail, t)
  const pending = checks.filter((c) => !c.done && c.detail)

  if (pending.length === 0 && block) {
    return {
      summary: block,
      details: [],
      nextStep: t('customsPages.declarations.opsPanel.nextGeneric')
    }
  }

  const summary =
    block ||
    pending[0]?.detail ||
    t('customsPages.declarations.opsPanel.arrivalBlockedGeneric')

  const firstPending = checks.find((c) => !c.done)
  const nextStep = firstPending ? t(firstPending.nextStepKey) : t('customsPages.declarations.opsPanel.nextGeneric')

  return {
    summary,
    details: pending.map((c) => c.detail!).filter(Boolean),
    nextStep
  }
}
