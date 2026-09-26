import { describe, expect, it } from 'vitest'
import { isActivationTarget, isEditableTarget, shouldStartAiRound } from '@/utils/aiInteractionHotkey'
import {
  chooseAiSkill,
  formatAiTranscript,
  isPasteCreateSource,
  suggestPasteCreateKind
} from '@/utils/aiInteractionTranscript'

function key(code: string, init: KeyboardEventInit = {}): KeyboardEvent {
  return new KeyboardEvent('keydown', { code, bubbles: true, ...init })
}

describe('shouldStartAiRound', () => {
  it('opens on a plain space', () => {
    expect(shouldStartAiRound(key('Space'))).toBe(true)
  })

  it('ignores space inside an input, on a button, while composing, or when repeated', () => {
    const input = document.createElement('input')
    const onInput = key('Space')
    Object.defineProperty(onInput, 'target', { value: input })
    expect(shouldStartAiRound(onInput)).toBe(false)
    expect(isEditableTarget(input)).toBe(true)

    const button = document.createElement('button')
    const onButton = key('Space')
    Object.defineProperty(onButton, 'target', { value: button })
    expect(shouldStartAiRound(onButton)).toBe(false)
    expect(isActivationTarget(button)).toBe(true)

    expect(shouldStartAiRound(key('Space', { isComposing: true }))).toBe(false)
    expect(shouldStartAiRound(key('Space', { repeat: true }))).toBe(false)
    expect(shouldStartAiRound(key('Enter'))).toBe(false)
  })

  it('ignores a closed drawer overlay that is still in the page', () => {
    const hidden = document.createElement('div')
    hidden.className = 'el-overlay'
    hidden.style.display = 'none'
    document.body.appendChild(hidden)
    expect(shouldStartAiRound(key('Space'))).toBe(true)

    hidden.style.display = 'block'
    expect(shouldStartAiRound(key('Space'))).toBe(false)
    hidden.remove()
  })
})

describe('ai interaction transcript', () => {
  const label = (skill: 'feedback' | 'handbook') => (skill === 'feedback' ? '反馈与建议' : '培训问答')

  it('keeps the whole round and marks a skill switch', () => {
    const text = formatAiTranscript(
      [
        { kind: 'user', id: '1', text: '怎么报关', skill: 'handbook' },
        { kind: 'assistant', id: '2', text: '先装箱。', skill: 'handbook' },
        { kind: 'marker', id: '3', skill: 'feedback' },
        { kind: 'user', id: '4', text: '上面这条不对', skill: 'feedback' }
      ],
      label
    )
    expect(text).toContain('已切换到 反馈与建议')
    expect(text).toContain('用户：上面这条不对')
    expect(text).toContain('培训问答：先装箱。')
  })

  it('offers create for pasted source text and leaves short questions to skills', () => {
    expect(isPasteCreateSource('怎么新建客户')).toBe(false)
    expect(isPasteCreateSource('张三\n13800001111\n上海某公司')).toBe(true)
    expect(suggestPasteCreateKind('这是供应商，厂家名称上海某某')).toBe('vendor')
    expect(suggestPasteCreateKind('客户张三，也是供应商')).toBe(null)
    expect(suggestPasteCreateKind('询价型号 STM32 数量 100')).toBe('rfq')
  })

  it('routes an unselected skill from the wording', () => {
    expect(chooseAiSkill('这个页面报错了', ['feedback', 'handbook'])).toBe('feedback')
    expect(chooseAiSkill('培训教材里怎么看库存', ['feedback', 'handbook'])).toBe('handbook')
    expect(chooseAiSkill('随便说说', ['feedback'])).toBe('feedback')
  })
})
