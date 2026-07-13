/**
 * 移除帮助页中无实际限制的「前置条件：无」行；保留有真实限制的前置条件。
 * 用法：node scripts/clean-help-prerequisites.mjs
 */
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const pagesDir = path.join(root, 'help', 'pages')

/** 整行删除：表示无限制 */
const REMOVE_LINE = [
  /^\*\*前置条件：\*\* 无\s*$/,
  /^\*\*前置条件：\*\* 无。\s*$/,
  /^\*\*前置条件：\*\* —\s*$/,
  /^\*\*前置条件：\*\* 无（删除以确认框为准）。\s*$/,
  /^\*\*前置条件：\*\* 无（删除以确认框为准）\s*$/,
  /^\*\*前置条件：\*\* 无特殊管理员权限。\s*$/,
  /^\*\*前置条件：\*\* 无特殊写权限要求。\s*$/,
  /^\*\*前置条件：\*\* 无固定状态门槛（以产品迭代为准）。\s*$/
]

/** 「无；」前缀去掉，保留分号后的真实条件 */
const TRANSFORM_LINE = [/^\*\*前置条件：\*\* 无；(.+)$/, '**前置条件：** $1']

function shouldRemoveLine(line) {
  return REMOVE_LINE.some((re) => re.test(line))
}

function transformLine(line) {
  for (const [re, repl] of [TRANSFORM_LINE]) {
    if (re.test(line)) return line.replace(re, repl)
  }
  return line
}

function cleanContent(text) {
  const lines = text.split(/\r?\n/)
  const out = []
  for (const line of lines) {
    if (shouldRemoveLine(line)) continue
    out.push(transformLine(line))
  }
  return out.join('\n')
}

const files = fs.readdirSync(pagesDir).filter((f) => f.endsWith('.md'))
let changed = 0
let removed = 0

for (const file of files) {
  const fp = path.join(pagesDir, file)
  const before = fs.readFileSync(fp, 'utf8')
  const after = cleanContent(before)
  if (after !== before) {
    const beforeCount = (before.match(/^\*\*前置条件：\*\*/gm) || []).length
    const afterCount = (after.match(/^\*\*前置条件：\*\*/gm) || []).length
    removed += beforeCount - afterCount
    fs.writeFileSync(fp, after, 'utf8')
    changed++
    console.log(`[clean-help] ${file}: ${beforeCount - afterCount} line(s) removed`)
  }
}

console.log(`[clean-help] done: ${changed} file(s), ${removed} prerequisite line(s) removed`)
