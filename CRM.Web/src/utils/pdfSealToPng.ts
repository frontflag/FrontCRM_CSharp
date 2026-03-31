import { getDocument, GlobalWorkerOptions } from 'pdfjs-dist'
// Vite：将 worker 打包为独立 URL，供 pdf.js 在浏览器中加载
import pdfWorkerUrl from 'pdfjs-dist/build/pdf.worker.min.mjs?url'

let workerReady = false

function ensurePdfWorker() {
  if (workerReady) return
  GlobalWorkerOptions.workerSrc = pdfWorkerUrl
  workerReady = true
}

/** 将 PDF 印章第一页渲染为 PNG data URL，供 <img> / 打印 / html2canvas 使用（img 不能直接显示 PDF blob） */
export async function renderPdfBlobFirstPageToPngDataUrl(blob: Blob): Promise<string> {
  ensurePdfWorker()
  const data = await blob.arrayBuffer()
  const pdf = await getDocument({ data }).promise
  const page = await pdf.getPage(1)
  const viewport = page.getViewport({ scale: 2 })
  const canvas = document.createElement('canvas')
  const ctx = canvas.getContext('2d')
  if (!ctx) throw new Error('Canvas 不可用')
  canvas.width = viewport.width
  canvas.height = viewport.height
  const task = page.render({ canvasContext: ctx, viewport })
  await task.promise
  return canvas.toDataURL('image/png')
}
