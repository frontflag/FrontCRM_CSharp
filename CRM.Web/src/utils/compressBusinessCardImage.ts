const MAX_BYTES = 4 * 1024 * 1024
const MAX_EDGE = 2048

/** 压缩名片图片以便 vision API 传输 */
export async function compressBusinessCardImage(file: File): Promise<File> {
  if (!file.type.startsWith('image/') || file.type.includes('heic')) {
    return file.size <= MAX_BYTES ? file : file
  }

  const bitmap = await createImageBitmap(file)
  const scale = Math.min(1, MAX_EDGE / Math.max(bitmap.width, bitmap.height))
  const width = Math.max(1, Math.round(bitmap.width * scale))
  const height = Math.max(1, Math.round(bitmap.height * scale))

  const canvas = document.createElement('canvas')
  canvas.width = width
  canvas.height = height
  const ctx = canvas.getContext('2d')
  if (!ctx) {
    bitmap.close()
    return file
  }
  ctx.drawImage(bitmap, 0, 0, width, height)
  bitmap.close()

  let quality = 0.88
  let blob = await canvasToBlob(canvas, 'image/jpeg', quality)
  while (blob.size > MAX_BYTES && quality > 0.45) {
    quality -= 0.08
    blob = await canvasToBlob(canvas, 'image/jpeg', quality)
  }

  const baseName = file.name.replace(/\.[^.]+$/, '') || 'business-card'
  return new File([blob], `${baseName}.jpg`, { type: 'image/jpeg', lastModified: Date.now() })
}

function canvasToBlob(canvas: HTMLCanvasElement, type: string, quality: number): Promise<Blob> {
  return new Promise((resolve, reject) => {
    canvas.toBlob(
      (b) => (b ? resolve(b) : reject(new Error('图片压缩失败'))),
      type,
      quality
    )
  })
}
