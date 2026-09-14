const PAGE_SLICE_EPS_MM = 0.5

/**
 * 将截图贴满 A4（不再叠加 10mm 页边距）。
 * 报表 DOM 已是 210mm 版式，内边距/表头出血与打印预览一致；再缩进会变成「纸中纸」。
 */
export function layoutCanvasOnA4Pages(
  canvasWidthPx: number,
  canvasHeightPx: number,
  pdfWmm: number,
  pdfHmm: number
): { imgW: number; imgH: number; pageOffsetsMm: number[] } {
  if (canvasWidthPx <= 0) {
    return { imgW: pdfWmm, imgH: 0, pageOffsetsMm: [0] }
  }
  const imgW = pdfWmm
  const imgH = (canvasHeightPx * imgW) / canvasWidthPx
  const pageOffsetsMm: number[] = []
  let heightLeft = imgH
  let position = 0
  pageOffsetsMm.push(position)
  heightLeft -= pdfHmm
  while (heightLeft > PAGE_SLICE_EPS_MM) {
    position = -(imgH - heightLeft)
    pageOffsetsMm.push(position)
    heightLeft -= pdfHmm
  }
  return { imgW, imgH, pageOffsetsMm }
}
