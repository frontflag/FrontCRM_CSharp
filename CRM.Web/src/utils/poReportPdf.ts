import html2canvas from 'html2canvas'
import { jsPDF } from 'jspdf'
import { layoutCanvasOnA4Pages } from '@/utils/poReportPdfLayout'

/** 将印章透明区域压到白底上，避免 html2canvas / PDF 中出现透明棋盘格伪影 */
function flattenSealImagesInClone(clonedDoc: Document) {
  const imgs = clonedDoc.querySelectorAll<HTMLImageElement>(
    'img.po-doc__seal, img.po-v2__seal'
  )
  imgs.forEach((img) => {
    try {
      const w = img.naturalWidth
      const h = img.naturalHeight
      if (!w || !h) return
      const c = clonedDoc.createElement('canvas')
      c.width = w
      c.height = h
      const ctx = c.getContext('2d')
      if (!ctx) return
      ctx.fillStyle = '#ffffff'
      ctx.fillRect(0, 0, w, h)
      ctx.drawImage(img, 0, 0)
      img.src = c.toDataURL('image/png')
    } catch {
      /* 不可读画布时保留原图 */
    }
  })
}

export { layoutCanvasOnA4Pages } from '@/utils/poReportPdfLayout'

/** 将 DOM 区域渲染为多页 A4 PDF（与打印预览同为铺满纸面） */
export async function renderElementToPdfBlob(el: HTMLElement): Promise<Blob> {
  const w = el.scrollWidth
  const h = el.scrollHeight
  const canvas = await html2canvas(el, {
    scale: 2,
    useCORS: true,
    allowTaint: true,
    backgroundColor: '#ffffff',
    logging: false,
    width: w,
    height: h,
    windowWidth: w,
    windowHeight: h,
    x: 0,
    y: 0,
    onclone: (clonedDoc) => {
      flattenSealImagesInClone(clonedDoc)
    }
  })

  const imgData = canvas.toDataURL('image/png', 1.0)
  const pdf = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' })
  const pdfW = pdf.internal.pageSize.getWidth()
  const pdfH = pdf.internal.pageSize.getHeight()
  const { imgW, imgH, pageOffsetsMm } = layoutCanvasOnA4Pages(
    canvas.width,
    canvas.height,
    pdfW,
    pdfH
  )

  pageOffsetsMm.forEach((y, i) => {
    if (i > 0) pdf.addPage()
    pdf.addImage(imgData, 'PNG', 0, y, imgW, imgH)
  })

  return pdf.output('blob')
}

export function blobToDataUrl(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const r = new FileReader()
    r.onload = () => resolve(String(r.result))
    r.onerror = () => reject(new Error('读取文件失败'))
    r.readAsDataURL(blob)
  })
}
