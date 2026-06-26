/** 名片原图暂存（与 aiPrefill token 同键，一次性消费） */

const fileMap = new Map<string, File[]>()

export function storeBusinessCardFiles(token: string, files: File[]): void {
  if (!token?.trim() || !files.length) return
  fileMap.set(token.trim(), [...files])
}

/** @deprecated 使用 storeBusinessCardFiles */
export function storeBusinessCardFile(token: string, file: File): void {
  storeBusinessCardFiles(token, [file])
}

export function consumeBusinessCardFiles(token: string): File[] {
  if (!token?.trim()) return []
  const key = token.trim()
  const files = fileMap.get(key) ?? []
  fileMap.delete(key)
  return files
}

/** @deprecated 使用 consumeBusinessCardFiles */
export function consumeBusinessCardFile(token: string): File | null {
  const files = consumeBusinessCardFiles(token)
  return files[0] ?? null
}

export function clearBusinessCardFiles(token: string): void {
  if (!token?.trim()) return
  fileMap.delete(token.trim())
}

/** @deprecated 使用 clearBusinessCardFiles */
export function clearBusinessCardFile(token: string): void {
  clearBusinessCardFiles(token)
}
