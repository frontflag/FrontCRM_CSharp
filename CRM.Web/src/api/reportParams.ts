import apiClient from './client'

export type ReportStyleVersion = 'V1' | 'V2'

export const reportParamsApi = {
  /** 打印页用：任意登录可读，不要求参数管理权限。 */
  async getEffectiveStyleVersion(): Promise<ReportStyleVersion> {
    try {
      const res = await apiClient.get<{ styleVersion?: string }>(
        '/api/v1/report-params/effective-style-version'
      )
      return res.styleVersion === 'V2' ? 'V2' : 'V1'
    } catch {
      return 'V1'
    }
  },

  async getStyleVersion(): Promise<ReportStyleVersion> {
    const res = await apiClient.get<{ styleVersion?: string }>(
      '/api/v1/report-params/style-version'
    )
    return res.styleVersion === 'V2' ? 'V2' : 'V1'
  },

  async setStyleVersion(styleVersion: ReportStyleVersion): Promise<ReportStyleVersion> {
    const res = await apiClient.put<{ styleVersion?: string }>(
      '/api/v1/report-params/style-version',
      { styleVersion }
    )
    return res.styleVersion === 'V2' ? 'V2' : 'V1'
  }
}
