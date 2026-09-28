import { describe, expect, it } from 'vitest'
import { resolveFeeHeaderLocks, type FeeHeaderLockInput } from '@/utils/customsFeeHeaderLocks'

function locks(partial: Partial<FeeHeaderLockInput> & Pick<FeeHeaderLockInput, 'panelMode'>): ReturnType<typeof resolveFeeHeaderLocks> {
  return resolveFeeHeaderLocks({
    canWrite: true,
    canCorrectLocked: false,
    hasSavedAgencyRate: true,
    ...partial
  })
}

describe('resolveFeeHeaderLocks', () => {
  it('leaves all three unlocked while a processing declaration can be fully recalculated', () => {
    expect(locks({ panelMode: 'editable' })).toEqual({
      exchangeRate: false,
      agencyRate: false,
      purchaseRatio: false
    })
  })

  it('locks every header field when the declaration is void', () => {
    expect(locks({ panelMode: 'readonly_void', canCorrectLocked: true })).toEqual({
      exchangeRate: true,
      agencyRate: true,
      purchaseRatio: true
    })
  })

  it('locks exchange rate and agency rate, and leaves purchase ratio unlocked, for a completed admin save', () => {
    expect(
      locks({ panelMode: 'readonly_completed', canCorrectLocked: true, hasSavedAgencyRate: true })
    ).toEqual({
      exchangeRate: true,
      agencyRate: true,
      purchaseRatio: false
    })
  })

  it('locks all three when clearance is done and the user cannot recalculate', () => {
    expect(locks({ panelMode: 'readonly_locked', canCorrectLocked: false })).toEqual({
      exchangeRate: true,
      agencyRate: true,
      purchaseRatio: true
    })
  })

  it('locks agency rate when a locked declaration is recalculated by an admin', () => {
    expect(locks({ panelMode: 'readonly_locked', canCorrectLocked: true })).toEqual({
      exchangeRate: true,
      agencyRate: true,
      purchaseRatio: false
    })
  })

  it('unlocks agency rate when an admin recalculates but no saved rate exists', () => {
    expect(
      locks({ panelMode: 'readonly_completed', canCorrectLocked: true, hasSavedAgencyRate: false }).agencyRate
    ).toBe(false)
  })

  it('leaves all three unlocked when P0 is missing but a full recalculation is still the save path', () => {
    expect(locks({ panelMode: 'blocked_no_p0' })).toEqual({
      exchangeRate: false,
      agencyRate: false,
      purchaseRatio: false
    })
  })

  it('locks all three on a completed declaration when the user cannot recalculate', () => {
    expect(locks({ panelMode: 'readonly_completed', canCorrectLocked: false })).toEqual({
      exchangeRate: true,
      agencyRate: true,
      purchaseRatio: true
    })
  })

  it('locks all three when the user cannot write', () => {
    expect(locks({ panelMode: 'editable', canWrite: false })).toEqual({
      exchangeRate: true,
      agencyRate: true,
      purchaseRatio: true
    })
  })
})
