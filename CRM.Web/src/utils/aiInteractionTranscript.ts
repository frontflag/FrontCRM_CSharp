export type AiInteractionSkill = 'feedback' | 'handbook' | 'ops'

export type PasteCreateKind = 'customer' | 'vendor' | 'rfq'

export type AiInteractionTurn =
  | { kind: 'user'; id: string; text: string; skill: AiInteractionSkill }
  | { kind: 'assistant'; id: string; text: string; skill: AiInteractionSkill }
  | { kind: 'marker'; id: string; skill: AiInteractionSkill }
  | { kind: 'create'; id: string; text: string }

const PASTE_QUESTION = /怎么|如何|什么是|为什么|吗[？?]?$/

const FEEDBACK_PATTERN = /反馈|建议|报错|故障|bug|不好用|无法|失败|改进|缺陷|截图/i
const FEEDBACK_STRONG_PATTERN = /报错|故障|bug|无法|缺陷|截图/i
const HANDBOOK_PATTERN = /培训|教材|新人|怎么|如何|什么是|业务|知识点|学习/
const OPS_PATTERN = /如何操作|步骤|公式|核销|提成|报关|装箱|拣货|审核|出库申请|新建|备货|收款|开票|报价|销售订单|出库/

function fallbackSkill(text: string, feedbackHit: boolean, handbookHit: boolean): AiInteractionSkill {
  if (feedbackHit && handbookHit) return FEEDBACK_STRONG_PATTERN.test(text) ? 'feedback' : 'handbook'
  if (feedbackHit) return 'feedback'
  if (handbookHit) return 'handbook'
  return text.includes('？') || text.includes('?') ? 'handbook' : 'feedback'
}

export function chooseAiSkill(text: string, allowed: AiInteractionSkill[]): AiInteractionSkill {
  if (allowed.length === 0) throw new Error('没有可用技能')
  if (allowed.length === 1) return allowed[0]
  const feedbackHit = FEEDBACK_PATTERN.test(text)
  const handbookHit = HANDBOOK_PATTERN.test(text)
  const opsHit = OPS_PATTERN.test(text)
  const pick: AiInteractionSkill =
    FEEDBACK_STRONG_PATTERN.test(text) && feedbackHit ? 'feedback' : opsHit ? 'ops' : fallbackSkill(text, feedbackHit, handbookHit)
  if (allowed.includes(pick)) return pick
  const next = pick === 'ops' ? fallbackSkill(text, feedbackHit, handbookHit) : pick
  return allowed.includes(next) ? next : allowed[0]
}

/** 多行或较长原文才建档。短问句仍走技能路由。 */
export function isPasteCreateSource(text: string): boolean {
  const raw = text.trim()
  if (raw.length < 8) return false
  const multi = /[\r\n]/.test(raw)
  const long = raw.length >= 40
  if (!multi && !long) return false
  if (!multi && raw.length < 80 && (PASTE_QUESTION.test(raw) || /[？?]$/.test(raw))) return false
  return true
}

/** 只有一种信号时标出更像哪一种。多种或没有信号时不预选。 */
export function suggestPasteCreateKind(text: string): PasteCreateKind | null {
  const hits: PasteCreateKind[] = []
  if (/需求|询价|型号|品牌|数量/i.test(text)) hits.push('rfq')
  if (/供应商|厂家|工厂/.test(text)) hits.push('vendor')
  if (/客户/.test(text)) hits.push('customer')
  return hits.length === 1 ? hits[0] : null
}

export function formatAiTranscript(
  turns: AiInteractionTurn[],
  skillLabel: (skill: AiInteractionSkill) => string
): string {
  return turns
    .map((turn) => {
      if (turn.kind === 'marker') return `已切换到 ${skillLabel(turn.skill)}`
      if (turn.kind === 'create' || turn.kind === 'user') return `用户：${turn.text}`
      return `${skillLabel(turn.skill)}：${turn.text}`
    })
    .join('\n')
}
