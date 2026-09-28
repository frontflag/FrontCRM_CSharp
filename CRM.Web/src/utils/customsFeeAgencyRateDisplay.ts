export type BrokerAgencyRateSource = 'current' | 'history' | 'none'

export type BrokerAgencyRateDisplayInput = {
  feesCalculatedAt?: string | null
  brokerAgencyRate?: number | null
  brokerMasterAgencyRate?: number | null
}

export type BrokerAgencyRateDisplay = {
  rate: number
  source: BrokerAgencyRateSource
}

function positiveRate(value: number | null | undefined): number | null {
  const n = Number(value)
  if (!Number.isFinite(n) || n <= 0) return null
  return n
}

function hasCalculatedFees(feesCalculatedAt: string | null | undefined): boolean {
  return typeof feesCalculatedAt === 'string' && feesCalculatedAt.trim().length > 0
}

/** 未试算用报关公司当前费率；已试算用单头快照。1 是合法的 0% 费率。source 标明实际读到的来源。 */
export function resolveDisplayedBrokerAgencyRateDetail(
  input: BrokerAgencyRateDisplayInput
): BrokerAgencyRateDisplay {
  const snapshot = positiveRate(input.brokerAgencyRate)
  const master = positiveRate(input.brokerMasterAgencyRate)
  if (hasCalculatedFees(input.feesCalculatedAt)) {
    if (snapshot != null) return { rate: snapshot, source: 'history' }
    if (master != null) return { rate: master, source: 'current' }
    return { rate: 1, source: 'none' }
  }
  if (master != null) return { rate: master, source: 'current' }
  if (snapshot != null) return { rate: snapshot, source: 'history' }
  return { rate: 1, source: 'none' }
}

export function resolveDisplayedBrokerAgencyRate(input: BrokerAgencyRateDisplayInput): number {
  return resolveDisplayedBrokerAgencyRateDetail(input).rate
}
