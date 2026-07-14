import type { Ref } from 'vue'

export function formatFinanceAccumulatedMonth(date = new Date()): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  return `${y}-${m}`
}

export function shiftFinanceAccumulatedMonth(current: string, delta: number): string | null {
  const [yRaw, mRaw] = current.split('-')
  let y = Number(yRaw)
  let m = Number(mRaw)
  if (!Number.isFinite(y) || !Number.isFinite(m)) return null
  m += delta
  while (m < 1) {
    m += 12
    y -= 1
  }
  while (m > 12) {
    m -= 12
    y += 1
  }
  return `${y}-${String(m).padStart(2, '0')}`
}

export function useFinanceAccumulatedMonthNav(selectedMonth: Ref<string>, onChange: () => void) {
  function shiftMonth(delta: number) {
    const next = shiftFinanceAccumulatedMonth(selectedMonth.value, delta)
    if (!next) return
    selectedMonth.value = next
    onChange()
  }

  return { shiftMonth, formatCurrentMonth: formatFinanceAccumulatedMonth }
}
