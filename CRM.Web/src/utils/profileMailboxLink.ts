/** 个人设置 · 我的邮箱独立路由 */
export const PROFILE_MAILBOX_PATH = '/profile/mailbox'

/**
 * 仅允许站内相对路径，防止 open redirect。
 * 接受 `/path` 或 `/path?x=1`，拒绝 `//evil`、协议相对与外链。
 */
export function resolveSafeReturnPath(from: unknown): string | null {
  if (typeof from !== 'string') return null
  const path = from.trim()
  if (!path.startsWith('/') || path.startsWith('//')) return null
  if (path.includes('://')) return null
  return path
}

/** 业务页跳转到「我的邮箱」，并带上来源以便返回 */
export function profileMailboxLocation(fromFullPath?: string) {
  const from = resolveSafeReturnPath(fromFullPath)
  return {
    path: PROFILE_MAILBOX_PATH,
    query: from ? { from } : {}
  }
}
