import { aiApi } from '@/api/ai'

/** 建单保存成功后回写 entity parse log（失败不阻断业务） */
export function markEntityParseSaved(
  parseLogId: string | null | undefined,
  savedBizId: string | null | undefined
): void {
  const logId = parseLogId?.trim()
  const bizId = savedBizId?.trim()
  if (!logId || !bizId) return
  void aiApi.markEntityParseSaved(logId, bizId).catch(() => {})
}
