import { describe, expect, it } from 'vitest'
import { appBrandHeaderTitle, usesCompanyProfileWordmarkLogo } from '@/config/loginTenant'

describe('usesCompanyProfileWordmarkLogo', () => {
  it('仅 semicore 用横版字标', () => {
    expect(usesCompanyProfileWordmarkLogo('semicore')).toBe(true)
    expect(usesCompanyProfileWordmarkLogo('idesemi')).toBe(false)
    expect(usesCompanyProfileWordmarkLogo('ecoinf')).toBe(false)
  })
})

describe('appBrandHeaderTitle', () => {
  it('semicore 去掉标题前的 Semicore，其它租户不改', () => {
    expect(appBrandHeaderTitle('Semicore AI Intelligent System', 'semicore')).toBe(
      'AI Intelligent System'
    )
    expect(appBrandHeaderTitle('Semicore AI Intelligent System', 'idesemi')).toBe(
      'Semicore AI Intelligent System'
    )
    expect(appBrandHeaderTitle('IDESemi AI Intelligent System', 'idesemi')).toBe(
      'IDESemi AI Intelligent System'
    )
  })
})
