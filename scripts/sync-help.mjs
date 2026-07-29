/**
 * 1) 将仓库 help/ 同步到 CRM.Web/public/help
 * 2) 根据 help/menu-registry.json 生成 帮助文档目录.md
 * 3) 为每个菜单项生成缺省的 pages/{菜单名称}_{id}.md 占位（不覆盖已有文件）
 *
 * 占位正文为用户向简短文案，不含开发指引；完稿须符合《扩展面板.帮助规范》§2.6。
 */
import { cpSync, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const repoRoot = join(__dirname, '..')
const helpRoot = join(repoRoot, 'help')
const dest = join(repoRoot, 'CRM.Web', 'public', 'help')

const STUB = (label, catalogHref, catalogTitle) => `[${catalogTitle}](${catalogHref})

# ${label}

## 页面功能

本页帮助说明正在完善中。

## 操作说明

如有疑问，请联系系统管理员。
`

function main() {
  if (!existsSync(helpRoot)) {
    console.warn('[sync-help] 未找到 help 目录，跳过:', helpRoot)
    process.exit(0)
  }

  const regPath = join(helpRoot, 'menu-registry.json')
  if (!existsSync(regPath)) {
    console.warn('[sync-help] 未找到 menu-registry.json，仅复制 help')
    mkdirSync(join(repoRoot, 'CRM.Web', 'public'), { recursive: true })
    if (existsSync(dest)) {
      rmSync(dest, { recursive: true, force: true })
    }
    cpSync(helpRoot, dest, { recursive: true })
    console.log('[sync-help] 已同步', helpRoot, '->', dest)
    return
  }

  const registry = JSON.parse(readFileSync(regPath, 'utf-8'))
  const pagesDirName = registry.pagesDir || 'pages'
  const catalogFile = registry.catalogFile || '帮助文档目录.md'
  const entries = registry.entries || []
  const pagesDir = join(helpRoot, pagesDirName)

  mkdirSync(pagesDir, { recursive: true })

  const entryDocRel = (e) => {
    if (typeof e.docPath === 'string' && e.docPath.trim()) {
      return e.docPath.replace(/^\/+/, '').trim()
    }
    return `${pagesDirName}/${e.label}_${e.id}.md`
  }

  const lines = ['# 帮助文档目录', '', '点击下列页面名称查看对应说明：', '']
  for (const e of entries) {
    lines.push(`- [${e.label}](${entryDocRel(e)})`)
  }
  lines.push('')
  writeFileSync(join(helpRoot, catalogFile), lines.join('\n'), 'utf-8')

  for (const e of entries) {
    const rel = entryDocRel(e)
    const fpath = join(helpRoot, rel)
    mkdirSync(dirname(fpath), { recursive: true })
    if (!existsSync(fpath)) {
      const depth = rel.split('/').length - 1
      const catalogHref = `${'../'.repeat(Math.max(depth, 1))}${catalogFile}`
      const catalogTitle = catalogFile.replace(/\.md$/, '')
      writeFileSync(fpath, STUB(e.label, catalogHref, catalogTitle), 'utf-8')
      console.log('[sync-help] 新建占位:', fpath)
    }
  }

  mkdirSync(join(repoRoot, 'CRM.Web', 'public'), { recursive: true })
  if (existsSync(dest)) {
    rmSync(dest, { recursive: true, force: true })
  }
  cpSync(helpRoot, dest, { recursive: true })
  console.log('[sync-help] 已同步', helpRoot, '->', dest)
}

main()
