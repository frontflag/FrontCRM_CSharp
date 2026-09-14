const HEADING = /^(#{1,6})\s+(.*)$/
const LIST_PREFIX = /^(\s*(?:[-*+]|\d+[.)])\s+)(.*)$/

function collapseOtherSectionStars(line: string) {
  return line.replace(/[★☆⭐✦✪🌟]{2,}/g, '★')
}

function stripStarNoise(text: string) {
  return text
    .replace(/[（(]\s*[★☆⭐✦✪🌟]+\s*[1-3]?\s*[)）]/g, ' ')
    .replace(/[★☆⭐✦✪🌟]+\s*[1-3](?!\d)/g, ' ')
    .replace(/[★☆⭐✦✪🌟]+/g, ' ')
    .replace(/[ \t]{2,}/g, ' ')
    .replace(/^[ \t]+|[ \t]+$/g, '')
}

function prefixSingleStar(body: string) {
  const cleaned = stripStarNoise(body)
  if (!cleaned) return body
  return `★ ${cleaned}`
}

function formatTop5ItemLine(line: string) {
  if (!line.trim()) return line
  if (/^\s*\|/.test(line)) return line
  const heading = HEADING.exec(line)
  if (heading) return line
  const list = LIST_PREFIX.exec(line)
  if (list) return `${list[1]}${prefixSingleStar(list[2])}`
  return prefixSingleStar(line)
}

function isSummaryHeading(title: string) {
  return /内容总结|內容總結/.test(title)
}

function splitSummarySentences(text: string): string[] {
  const parts: string[] = []
  let buf = ''
  for (let i = 0; i < text.length; i++) {
    const ch = text[i]
    buf += ch
    if (ch === '。' || ch === '；' || ch === ';') {
      const next = text[i + 1]
      if (next === '"' || next === '”' || next === '’' || next === "'") {
        buf += next
        i += 1
      }
      const sentence = buf.replace(/^\s*[-*+•●]\s+/, '').trim()
      if (sentence) parts.push(sentence)
      buf = ''
      continue
    }
    if (ch === '.') {
      const prev = text[i - 1]
      const next = text[i + 1]
      if (prev && /\d/.test(prev) && next && /\d/.test(next)) continue
      if (!next || /\s/.test(next) || /[\u4e00-\u9fff]/.test(next)) {
        const sentence = buf.replace(/^\s*[-*+•●]\s+/, '').trim()
        if (sentence) parts.push(sentence)
        buf = ''
      }
    }
  }
  const tail = buf.replace(/^\s*[-*+•●]\s+/, '').trim()
  if (tail) parts.push(tail)
  return parts
}

function breakAfterColon(sentence: string) {
  let out = ''
  for (let i = 0; i < sentence.length; i++) {
    const ch = sentence[i]
    out += ch
    if (ch !== '：' && ch !== ':') continue
    if (ch === ':' && sentence[i + 1] === '/') continue
    const prev = sentence[i - 1]
    const next = sentence[i + 1]
    if (ch === ':' && prev && /\d/.test(prev) && next && /\d/.test(next)) continue
    let j = i + 1
    while (sentence[j] === ' ' || sentence[j] === '\t') j++
    if (j >= sentence.length || sentence[j] === '\n') continue
    out += '\n  '
    while (sentence[i + 1] === ' ' || sentence[i + 1] === '\t') i++
  }
  return out
}

function formatSummaryBlock(lines: string[]): string[] {
  const text = lines
    .map((line) => line.replace(/^\s*[-*+•●]\s+/, '').trim())
    .filter(Boolean)
    .join('')
  if (!text) return lines.length ? [''] : []
  return splitSummarySentences(text).flatMap((s) => {
    const [first, ...rest] = breakAfterColon(s).split('\n')
    return [`- ${first.trim()}`, ...rest.map((line) => `  ${line.trim()}`)]
  })
}

/** 本周要闻每条只保留行首一颗 ★；内容总结按句号/分号一行一句并加圆点。 */
export function normalizeIndustryNewsMarkdown(md: string) {
  if (!md) return md
  const lines = md.split('\n')
  const out: string[] = []
  let section: 'top5' | 'summary' | 'other' = 'other'
  let summaryBuf: string[] = []

  const flushSummary = () => {
    if (section !== 'summary') return
    out.push(...formatSummaryBlock(summaryBuf))
    summaryBuf = []
  }

  for (const line of lines) {
    const heading = HEADING.exec(line)
    if (heading) {
      if (heading[1].length === 2) {
        flushSummary()
        if (/本周要闻/.test(heading[2])) section = 'top5'
        else if (isSummaryHeading(heading[2])) section = 'summary'
        else section = 'other'
      }
      out.push(line)
      continue
    }
    if (section === 'summary') {
      summaryBuf.push(line)
      continue
    }
    out.push(section === 'top5' ? formatTop5ItemLine(line) : collapseOtherSectionStars(line))
  }
  flushSummary()
  return out.join('\n')
}

export type IndustryNewsFloor = { id: string; title: string; level: 2 | 3 }

const categoryFloorTitles = new Set([
  '行业动态',
  '关键厂商',
  '行情价格',
  '重组并购',
  '公司治理',
  '展会信息',
  '行業動態',
  '關鍵廠商',
  '行情價格',
  '重組併購',
  '展會信息',
  '展會資訊'
])

function headingTitle(el: Element) {
  return (el.textContent || '').replace(/\s+/g, ' ').trim()
}

export function decorateIndustryNewsHtml(html: string): { html: string; floors: IndustryNewsFloor[] } {
  if (!html.trim() || typeof DOMParser === 'undefined') return { html, floors: [] }
  const doc = new DOMParser().parseFromString(`<div class="in-root">${html}</div>`, 'text/html')
  const root = doc.body.querySelector('.in-root')
  if (!root) return { html, floors: [] }
  const floors: IndustryNewsFloor[] = []
  const nodes = Array.from(root.querySelectorAll('h2, h3, p'))
  nodes.forEach((el) => {
    const title = headingTitle(el)
    if (!title) return
    const isHeading = el.tagName === 'H2' || el.tagName === 'H3'
    const isCategoryPara = el.tagName === 'P' && categoryFloorTitles.has(title)
    if (!isHeading && !isCategoryPara) return
    const id = `industry-news-floor-${floors.length}`
    el.setAttribute('id', id)
    floors.push({
      id,
      title,
      level: el.tagName === 'H2' ? 2 : 3
    })
  })
  return { html: root.innerHTML, floors }
}
