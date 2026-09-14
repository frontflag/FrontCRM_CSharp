import { describe, expect, it } from 'vitest'
import { layoutCanvasOnA4Pages } from '@/utils/poReportPdfLayout'

const A4_W = 210
const A4_H = 297

describe('layoutCanvasOnA4Pages', () => {
  it('单页 A4 截图贴满整页、无额外边距', () => {
    const { imgW, imgH, pageOffsetsMm } = layoutCanvasOnA4Pages(794, 1123, A4_W, A4_H)
    expect(pageOffsetsMm).toEqual([0])
    expect(imgW).toBe(A4_W)
    expect(imgH).toBeCloseTo((1123 * A4_W) / 794, 2)
    expect(imgH).toBeLessThanOrEqual(A4_H + 0.5)
  })

  it('旧逻辑 10mm 边距会把 210mm 稿缩到 190mm；现逻辑保持 210mm', () => {
    const { imgW } = layoutCanvasOnA4Pages(2100, 2970, A4_W, A4_H)
    expect(imgW).toBe(210)
    expect(imgW).not.toBe(190)
  })

  it('超高内容按整页宽度切片翻页', () => {
    const { imgW, imgH, pageOffsetsMm } = layoutCanvasOnA4Pages(210, 600, A4_W, A4_H)
    expect(imgW).toBe(A4_W)
    expect(imgH).toBeCloseTo((600 * A4_W) / 210, 5)
    expect(pageOffsetsMm.length).toBe(3)
    expect(pageOffsetsMm[0]).toBe(0)
    expect(pageOffsetsMm[1]).toBeCloseTo(-A4_H, 5)
    expect(pageOffsetsMm[2]).toBeCloseTo(-A4_H * 2, 5)
  })
})
