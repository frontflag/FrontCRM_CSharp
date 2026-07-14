import * as XLSX from 'xlsx'

export type RfqExcelWorkbookCache = {
  sheetNames: string[]
  readSheetRows: (sheetIndex: number) => unknown[][]
}

function readFileAsArrayBuffer(file: File): Promise<ArrayBuffer> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = (e) => resolve(e.target!.result as ArrayBuffer)
    reader.onerror = reject
    reader.readAsArrayBuffer(file)
  })
}

/** 读取 Excel 工作簿并缓存，供多 Sheet 切换时本地解析行数据。 */
export async function loadRfqExcelWorkbook(file: File): Promise<RfqExcelWorkbookCache> {
  const data = await readFileAsArrayBuffer(file)
  const wb = XLSX.read(data, { type: 'array' })
  const sheetNames = [...wb.SheetNames]

  return {
    sheetNames,
    readSheetRows(sheetIndex: number) {
      const name = sheetNames[sheetIndex]
      if (!name) return []
      const ws = wb.Sheets[name]
      if (!ws) return []
      return XLSX.utils.sheet_to_json(ws, { header: 1, defval: '' }) as unknown[][]
    }
  }
}
