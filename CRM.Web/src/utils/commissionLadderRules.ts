import { COMMISSION_LADDER_COUNT } from '@/api/commissionRates'

export interface CommissionLadderDraftSlot {
  thresholdAmount: number | null
  ratePoints: number | null
}

export function validateCommissionLadders(slots: CommissionLadderDraftSlot[]): string | null {
  if (!slots || slots.length !== COMMISSION_LADDER_COUNT) return `阶梯须为 ${COMMISSION_LADDER_COUNT} 档`

  const filled: boolean[] = []
  for (let i = 0; i < COMMISSION_LADDER_COUNT; i++) {
    const hasT = slots[i].thresholdAmount != null
    const hasP = slots[i].ratePoints != null
    if (hasT !== hasP) return `第 ${i + 1} 档达标金额与提成点数需同时填写或同时留空`
    filled[i] = hasT
  }

  let k = 0
  while (k < COMMISSION_LADDER_COUNT && filled[k]) k++
  for (let i = k; i < COMMISSION_LADDER_COUNT; i++) {
    if (filled[i]) return `第 ${i + 1} 档有值时，第 1～${i + 1} 档必须连续填写`
  }

  let prev: number | null = null
  for (let i = 0; i < k; i++) {
    const threshold = slots[i].thresholdAmount as number
    const points = slots[i].ratePoints as number
    if (threshold < 0) return `第 ${i + 1} 档达标金额不能为负`
    if (points < 0 || points > 100) return `第 ${i + 1} 档提成点数须为 0.0～100.0`
    if (prev != null && threshold <= prev) return '达标金额必须随阶梯严格递增'
    prev = threshold
  }
  return null
}
