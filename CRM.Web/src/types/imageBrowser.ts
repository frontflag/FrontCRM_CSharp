/** 通用图片浏览器单条记录 */
export interface ImageBrowserItem {
  /** 列表唯一键：documentId 或本地 uid */
  id: string
  /** 左侧文件名列表展示 */
  name: string
  /** 已持久化文档主键 */
  documentId?: string
  /** 未上传本地预览（blob:） */
  previewUrl?: string
}

export interface ImageBrowserOpenOptions {
  items: ImageBrowserItem[]
  initialIndex?: number
  title?: string
}

export type ImageBrowserViewMode = 'fit-window' | 'fit-width' | 'scale'
