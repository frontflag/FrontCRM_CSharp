import fs from 'fs'
import path from 'path'
import { fileURLToPath } from 'url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const root = path.resolve(__dirname, '../src/views')

function walk(dir, out = []) {
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name)
    if (ent.isDirectory()) walk(p, out)
    else if (ent.name.endsWith('.vue')) out.push(p)
  }
  return out
}

let n = 0
for (const fp of walk(root)) {
  let s = fs.readFileSync(fp, 'utf8')
  if (!s.includes("key: 'actions'") || !s.includes("labelClassName: 'op-col'")) continue
  if (/key: 'actions'[\s\S]*?resizable:\s*false/s.test(s)) continue

  const orig = s
  // cols.push({ ... }) or plain object — insert before closing `}` of actions column (last labelClassName before actions closing)
  s = s.replace(
    /(\{\s*\n\s*key: 'actions',[\s\S]*?labelClassName: 'op-col')\s*\n(\s*)(\}\)?)/,
    "$1,\n$2resizable: false\n$2$3"
  )
  if (s !== orig) {
    fs.writeFileSync(fp, s, 'utf8')
    n++
    console.log('resizable:', path.relative(path.resolve(__dirname, '..'), fp))
  }
}
console.log('files:', n)
