import en from 'element-plus/es/locale/lang/en'
import zhCn from 'element-plus/es/locale/lang/zh-cn'
import type { Language } from 'element-plus/es/locale'

/** Element Plus 2.9 内置 zh-cn 缺少 pagination，补全中文翻页文案 */
const zhCnWithPagination: Language = {
  ...zhCn,
  el: {
    ...zhCn.el,
    pagination: {
      goto: '至',
      pagesize: '/页',
      total: '共{total}',
      pageClassifier: '页',
      page: '页',
      prev: '上一页',
      next: '下一页',
      currentPage: '第 {pager} 页',
      prevPages: '向前 {pager} 页',
      nextPages: '向后 {pager} 页',
      deprecationWarning:
        '你使用了一些已被废弃的用法，请参考 el-pagination 的官方文档'
    }
  }
}

export function getElementPlusLocale(appLocale: string): Language {
  return appLocale === 'zh-CN' ? zhCnWithPagination : en
}
