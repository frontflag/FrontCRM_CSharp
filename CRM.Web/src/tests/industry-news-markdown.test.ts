import { describe, expect, it } from 'vitest'
import { normalizeIndustryNewsMarkdown } from '@/utils/industryNewsMarkdown'

describe('normalizeIndustryNewsMarkdown', () => {
  it('图1：行首多颗星收成一颗', () => {
    const md = [
      '## 本周要闻 Top 5',
      '',
      '★★★ 2026-09-12 英伟达洽谈以基石投资者身份参与Anthropic IPO',
      '★★ 2026-09-13 OpenAI CEO Sam Altman称当下IPO不明智',
      '★ 2026-09-09 (背景) 长鑫存储全球市场份额达10%'
    ].join('\n')
    const out = normalizeIndustryNewsMarkdown(md)
    expect(out).toContain('★ 2026-09-12 英伟达洽谈以基石投资者身份参与Anthropic IPO')
    expect(out).toContain('★ 2026-09-13 OpenAI CEO Sam Altman称当下IPO不明智')
    expect(out).toContain('★ 2026-09-09 (背景) 长鑫存储全球市场份额达10%')
    expect(out).not.toMatch(/★★/)
  })

  it('图2：句末 ★3 移到行首单星', () => {
    const md = [
      '## 本周要闻 Top 5',
      '',
      '苹果2026秋季发布会重磅登场（9月10日）：iPhone 18 Pro系列首发 ★3',
      '摩根士丹利大幅上调WFE市场预测（9月11日）：2025-2028年CAGR达28% ★3'
    ].join('\n')
    const out = normalizeIndustryNewsMarkdown(md)
    expect(out).toContain(
      '★ 苹果2026秋季发布会重磅登场（9月10日）：iPhone 18 Pro系列首发'
    )
    expect(out).not.toMatch(/★3/)
    expect(out.match(/★/g)?.length).toBe(2)
  })

  it('图3：日期后的 ★3 改为行首单星', () => {
    const md = [
      '## 本周要闻 Top 5',
      '',
      '2026-09-12 ★3 英伟达考虑投资Anthropic IPO达100亿美元',
      '2026-09-12 ★2 苹果iPhone 18 Pro系列全球数十个国家和地区同步开启预订',
      '2026-09-13 ★1 国常会部署算力网建设，企业将迎来发展机遇'
    ].join('\n')
    const out = normalizeIndustryNewsMarkdown(md)
    expect(out).toBe(
      [
        '## 本周要闻 Top 5',
        '',
        '★ 2026-09-12 英伟达考虑投资Anthropic IPO达100亿美元',
        '★ 2026-09-12 苹果iPhone 18 Pro系列全球数十个国家和地区同步开启预订',
        '★ 2026-09-13 国常会部署算力网建设，企业将迎来发展机遇'
      ].join('\n')
    )
  })

  it('列表项同样只保留行首一颗星', () => {
    const md = ['## 本周要闻 Top 5', '', '- ★★★ 2026-09-12 标题'].join('\n')
    expect(normalizeIndustryNewsMarkdown(md)).toContain('- ★ 2026-09-12 标题')
  })

  it('分类详述不改行首星，只收连续星', () => {
    const md = [
      '## 本周要闻 Top 5',
      '',
      '★★ 标题A',
      '',
      '## 分类详述',
      '',
      '2026-09-13 国务院常务会议部署算力网建设 ★★★'
    ].join('\n')
    const out = normalizeIndustryNewsMarkdown(md)
    expect(out).toContain('★ 标题A')
    expect(out).toContain('2026-09-13 国务院常务会议部署算力网建设 ★')
    expect(out).not.toContain('★★★')
  })

  it('重复规范化仍保持一行一颗星', () => {
    const md = ['## 本周要闻 Top 5', '2026-09-12 ★3 标题'].join('\n')
    const once = normalizeIndustryNewsMarkdown(md)
    expect(normalizeIndustryNewsMarkdown(once)).toBe(once)
  })

  it('内容总结按句号和分号拆成一行一句并加圆点', () => {
    const md = [
      '## 内容总结',
      '',
      '本周行业主线为"AI算力基建高景气+存储格局重构+终端创新拉动"。摩根士丹利上调WFE预测验证半导体设备长周期向上，苹果折叠屏iPhone Duo发布开辟新增长极，中国存储双雄（长江/长鑫）份额攀升加剧全球竞争。对采购团队建议：优先锁定DDR5及HBM长单，关注苹果供应链国产替代机会；对销售团队建议：加大AI服务器相关器件（高速连接器、先进封装材料、高容MLCC）推广力度，警惕存储价格波动风险。'
    ].join('\n')
    expect(normalizeIndustryNewsMarkdown(md)).toBe(
      [
        '## 内容总结',
        '- 本周行业主线为"AI算力基建高景气+存储格局重构+终端创新拉动"。',
        '- 摩根士丹利上调WFE预测验证半导体设备长周期向上，苹果折叠屏iPhone Duo发布开辟新增长极，中国存储双雄（长江/长鑫）份额攀升加剧全球竞争。',
        '- 对采购团队建议：',
        '  优先锁定DDR5及HBM长单，关注苹果供应链国产替代机会；',
        '- 对销售团队建议：',
        '  加大AI服务器相关器件（高速连接器、先进封装材料、高容MLCC）推广力度，警惕存储价格波动风险。'
      ].join('\n')
    )
  })

  it('内容总结重复规范化不叠加圆点', () => {
    const md = [
      '## 内容总结',
      '第一句。第二句；第三句。'
    ].join('\n')
    const once = normalizeIndustryNewsMarkdown(md)
    expect(normalizeIndustryNewsMarkdown(once)).toBe(once)
    expect(once).toBe(
      ['## 内容总结', '- 第一句。', '- 第二句；', '- 第三句。'].join('\n')
    )
  })

  it('内容总结冒号后折行且不加圆点', () => {
    const md = [
      '## 内容总结',
      '本周行业主线围绕AI资本化加速与分化展开：Anthropic拟创纪录IPO及英伟达潜在巨额投资显示AI产业链资本整合深化，而OpenAI推迟上市则反映头部企业对监管环境的审慎。上游设备端景气度获摩根士丹利大幅上修，WFE市场进入高增长周期。'
    ].join('\n')
    const out = normalizeIndustryNewsMarkdown(md)
    expect(out).toBe(
      [
        '## 内容总结',
        '- 本周行业主线围绕AI资本化加速与分化展开：',
        '  Anthropic拟创纪录IPO及英伟达潜在巨额投资显示AI产业链资本整合深化，而OpenAI推迟上市则反映头部企业对监管环境的审慎。',
        '- 上游设备端景气度获摩根士丹利大幅上修，WFE市场进入高增长周期。'
      ].join('\n')
    )
    expect(normalizeIndustryNewsMarkdown(out)).toBe(out)
  })
})
