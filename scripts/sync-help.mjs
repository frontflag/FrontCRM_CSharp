/**
 * 将仓库根目录 help/ 同步到 CRM.Web/public/help，便于 Vite 开发与打包时随静态资源发布。
 */
import { cpSync, existsSync, mkdirSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const repoRoot = join(__dirname, '..')
const src = join(repoRoot, 'help')
const dest = join(repoRoot, 'CRM.Web', 'public', 'help')

if (!existsSync(src)) {
  console.warn('[sync-help] 未找到 help 目录，跳过:', src)
  process.exit(0)
}

mkdirSync(join(repoRoot, 'CRM.Web', 'public'), { recursive: true })
cpSync(src, dest, { recursive: true })
console.log('[sync-help] 已同步', src, '->', dest)
