/**
 * 构建后校验：dist/help 须与 prebuild（sync-help）同步后的 help/ 源完全一致。
 * 防止发布包携带陈旧帮助文档（如未含最新 pages/*.md 或目录页）。
 *
 * 用法：
 *   node scripts/verify-help-dist.mjs
 *   node scripts/verify-help-dist.mjs --source path/to/help --dist path/to/dist/help
 */
import { createHash } from 'node:crypto'
import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs'
import { dirname, join, relative } from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const repoRoot = join(__dirname, '..')

function parseArgs() {
  const args = process.argv.slice(2)
  let source = join(repoRoot, 'help')
  let distHelp = join(repoRoot, 'CRM.Web', 'dist', 'help')
  for (let i = 0; i < args.length; i++) {
    if (args[i] === '--source' && args[i + 1]) source = args[++i]
    else if (args[i] === '--dist' && args[i + 1]) distHelp = args[++i]
  }
  return { source, distHelp }
}

function normalizeContent(path) {
  return readFileSync(path, 'utf8').replace(/\r\n/g, '\n')
}

function hashFile(path) {
  return createHash('sha256').update(normalizeContent(path), 'utf8').digest('hex')
}

function walkFiles(dir, base = dir) {
  const result = []
  if (!existsSync(dir)) return result
  for (const name of readdirSync(dir)) {
    const full = join(dir, name)
    if (statSync(full).isDirectory()) {
      result.push(...walkFiles(full, base))
    } else {
      result.push(relative(base, full).replace(/\\/g, '/'))
    }
  }
  return result.sort()
}

function main() {
  const { source, distHelp } = parseArgs()

  if (!existsSync(source)) {
    console.error('[verify-help-dist] FAIL: 源 help 目录不存在:', source)
    process.exit(1)
  }
  if (!existsSync(distHelp)) {
    console.error('[verify-help-dist] FAIL: dist/help 不存在，构建产物未包含帮助文档。')
    console.error('  期望路径:', distHelp)
    console.error('  请确认 prebuild 已执行 sync-help，且 Vite 已将 public/help 复制到 dist。')
    process.exit(1)
  }

  const sourceFiles = walkFiles(source)
  const distFileSet = new Set(walkFiles(distHelp))

  const missing = []
  const mismatched = []

  for (const rel of sourceFiles) {
    const dstPath = join(distHelp, rel)
    if (!existsSync(dstPath)) {
      missing.push(rel)
      continue
    }
    const srcHash = hashFile(join(source, rel))
    const dstHash = hashFile(dstPath)
    if (srcHash !== dstHash) {
      mismatched.push(rel)
    }
  }

  const registryIssues = []
  const regPath = join(source, 'menu-registry.json')
  if (existsSync(regPath)) {
    const registry = JSON.parse(readFileSync(regPath, 'utf8'))
    const pagesDir = registry.pagesDir || 'pages'
    for (const e of registry.entries || []) {
      const rel = `${pagesDir}/${e.label}_${e.id}.md`
      if (!existsSync(join(distHelp, rel))) {
        registryIssues.push(rel)
      }
    }
  }

  const extraInDist = [...distFileSet].filter((rel) => !sourceFiles.includes(rel))

  if (missing.length || mismatched.length || registryIssues.length) {
    console.error('[verify-help-dist] FAIL: dist/help 与 help/ 源不一致，发布包中的帮助文档可能不是最新。')
    if (missing.length) {
      console.error(`\n缺失文件 (${missing.length}):`)
      for (const f of missing) console.error(`  - ${f}`)
    }
    if (mismatched.length) {
      console.error(`\n内容不一致 (${mismatched.length}):`)
      for (const f of mismatched) console.error(`  - ${f}`)
    }
    if (registryIssues.length) {
      console.error(`\n注册表页面在 dist 中缺失 (${registryIssues.length}):`)
      for (const f of registryIssues) console.error(`  - ${f}`)
    }
    console.error('\n修复：在仓库根执行 cd CRM.Web && npm run build（确保 prebuild/sync-help 已跑），再重新部署。')
    process.exit(1)
  }

  if (extraInDist.length) {
    console.warn(`[verify-help-dist] WARN: dist/help 含 ${extraInDist.length} 个源目录不存在的文件（可能为历史残留）`)
    for (const f of extraInDist.slice(0, 5)) console.warn(`  - ${f}`)
    if (extraInDist.length > 5) console.warn(`  ... 共 ${extraInDist.length} 个`)
  }

  console.log(`[verify-help-dist] OK: ${sourceFiles.length} 个帮助文件已与 dist/help 对齐`)
}

main()
