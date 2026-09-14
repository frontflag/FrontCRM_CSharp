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

function waitForImages(root: HTMLElement): Promise<void> {
  const imgs = Array.from(root.querySelectorAll('img'))
  return Promise.all(
    imgs.map(
      (img) =>
        new Promise<void>((resolve) => {
          if (img.complete) {
            resolve()
            return
          }
          img.onload = () => resolve()
          img.onerror = () => resolve()
        })
    )
  ).then(() => undefined)
}

/**
 * 把报表节点挪到视口左上角再截，避免 html2canvas 按「整窗宽 + 居中稿」截出纸中纸。
 * 不要把 windowWidth 设成稿宽：iframe 会被裁成稿宽，元素仍在原坐标，四周全是白边。
 */
async function captureElementCanvas(el: HTMLElement): Promise<HTMLCanvasElement> {
  const host = document.createElement('div')
  host.setAttribute('data-po-pdf-capture', '1')
  host.style.cssText = [
    'position:fixed',
    'left:0',
    'top:0',
    'width:210mm',
    'margin:0',
    'padding:0',
    'z-index:2147483646',
    'background:#ffffff',
    'overflow:visible',
    'pointer-events:none'
  ].join(';')

  const clone = el.cloneNode(true) as HTMLElement
  clone.style.margin = '0'
  clone.style.marginLeft = '0'
  clone.style.marginRight = '0'
  clone.style.position = 'relative'
  clone.style.left = '0'
  clone.style.top = '0'
  clone.style.right = 'auto'
  clone.style.transform = 'none'
  clone.style.boxShadow = 'none'
  clone.style.maxWidth = 'none'
  clone.style.width = '210mm'
  host.appendChild(clone)
  document.body.appendChild(host)

  try {
    await waitForImages(clone)
    await new Promise<void>((r) => requestAnimationFrame(() => r()))
    return await html2canvas(clone, {
      scale: 2,
      useCORS: true,
      allowTaint: true,
      backgroundColor: '#ffffff',
      logging: false,
      scrollX: 0,
      scrollY: 0,
      onclone: (clonedDoc, clonedEl) => {
        flattenSealImagesInClone(clonedDoc)
        clonedEl.style.margin = '0'
        clonedEl.style.position = 'relative'
        clonedEl.style.left = '0'
        clonedEl.style.top = '0'
        clonedEl.style.boxShadow = 'none'
        clonedEl.style.width = '210mm'
      }
    })
  } finally {
    host.remove()
  }
}

export { layoutCanvasOnA4Pages } from '@/utils/poReportPdfLayout'

/** 将 DOM 区域渲染为多页 A4 PDF（与打印预览同为铺满纸面） */
export async function renderElementToPdfBlob(el: HTMLElement): Promise<Blob> {
  const canvas = await captureElementCanvas(el)
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
