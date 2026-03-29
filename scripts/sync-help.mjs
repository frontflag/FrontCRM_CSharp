/**
 * 1) 将仓库 help/ 同步到 CRM.Web/public/help
 * 2) 根据 help/menu-registry.json 生成 帮助文档目录.md
 * 3) 为每个菜单项生成缺省的 pages/{菜单名称}_{id}.md 占位（不覆盖已有文件）
 */
import { cpSync, existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const repoRoot = join(__dirname, '..')
const helpRoot = join(repoRoot, 'help')
const dest = join(repoRoot, 'CRM.Web', 'public', 'help')

const STUB = (label, catalogName) => `[${catalogName.replace(/\.md$/, '')}](../${catalogName})

# ${label}

## 页面功能

（请补充：本页面支持的业务目标、可查/可改的数据范围。）

## 操作说明

（请按《扩展面板.帮助规范》使用 \`help-op-block\` 卡片：**操作名** / **说明：** / **前置条件：**。通用操作「详情」「编辑」「删除」可省略整卡。）

<div class="help-op-block">

**示例操作**

**说明：** 占位描述。

**前置条件：** 无

</div>
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

  const lines = ['# 帮助文档目录', '', '点击下列页面名称查看对应说明：', '']
  for (const e of entries) {
    const fname = `${e.label}_${e.id}.md`
    const relLink = `${pagesDirName}/${fname}`
    lines.push(`- [${e.label}](${relLink})`)
  }
  lines.push('')
  writeFileSync(join(helpRoot, catalogFile), lines.join('\n'), 'utf-8')

  for (const e of entries) {
    const fname = `${e.label}_${e.id}.md`
    const fpath = join(pagesDir, fname)
    if (!existsSync(fpath)) {
      writeFileSync(fpath, STUB(e.label, catalogFile), 'utf-8')
      console.log('[sync-help] 新建占位:', fpath)
    }
  }

  mkdirSync(join(repoRoot, 'CRM.Web', 'public'), { recursive: true })
  cpSync(helpRoot, dest, { recursive: true })
  console.log('[sync-help] 已同步', helpRoot, '->', dest)
}

main()
