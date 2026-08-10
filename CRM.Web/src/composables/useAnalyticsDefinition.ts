import { useI18n } from 'vue-i18n'

export type AnalyticsDefinitionFields = {
  showDefinition: true
  definitionLabel: string
  definitionChart: string
  definitionDataSource: string
  definitionText: string
}

/**
 * 从 `{ns}.defs.{path}.chart|dataSource|text` 组装口径 Tip 字段。
 * path 例：`snapshot.rfqItems`、`trend.amount`、`breakdown.orderStatus`
 */
export function useAnalyticsDefinition(ns: string) {
  const { t } = useI18n()

  function def(path: string): AnalyticsDefinitionFields {
    const base = `${ns}.defs.${path}`
    return {
      showDefinition: true,
      definitionLabel: t('salesAnalytics.definitionTip.button'),
      definitionChart: t(`${base}.chart`),
      definitionDataSource: t(`${base}.dataSource`),
      definitionText: t(`${base}.text`)
    }
  }

  /** 仅取三项文案（用于已有 showDefinition 结构） */
  function defParts(path: string): Pick<
    AnalyticsDefinitionFields,
    'definitionChart' | 'definitionDataSource' | 'definitionText'
  > {
    const d = def(path)
    return {
      definitionChart: d.definitionChart,
      definitionDataSource: d.definitionDataSource,
      definitionText: d.definitionText
    }
  }

  return { def, defParts, defLabel: () => t('salesAnalytics.definitionTip.button') }
}
