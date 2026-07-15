# RFQ Excel 导入 — 设计需求与实现说明（FrontCRM）

| 项 | 内容 |
|----|------|
| 状态 | 已上线（2026-07） |
| 关联文档 | [AI实体解析建单-设计与实现](./AI实体解析建单-设计与实现.md) · [智能学习品牌匹配-设计与实现](./智能学习品牌匹配-设计与实现.md) |
| 测试对照 | [RFQ Excel导入-测试对照说明](../QA/需求报价/RFQ Excel导入-测试对照说明.md) · [智能学习品牌匹配-测试对照说明](../QA/需求报价/智能学习品牌匹配-测试对照说明.md) |
| 权限脚本 | `scripts/ai_entity_parse_postgresql.sql`（`biz.ai.entity.parse.rfq`） |
| AI 场景脚本 | `scripts/ai_entity_parse_rfq_excel_column_map_postgresql.sql`、`scripts/ai_entity_parse_rfq_excel_brand_map_postgresql.sql` |
| 学习表脚本 | `scripts/ensure_biz_brand_learned_mapping_postgresql.sql` |

---

## 一、功能定位

**RFQ Excel 导入**允许业务员将客户询价 Excel **在浏览器本地解析**为需求明细行，经表头映射与品牌匹配后，**预填到新建 RFQ 页面**供核对与保存。

与「粘贴文本 AI 建 RFQ」（`entity.parse.rfq`）的区别：

| 维度 | 文本 AI 建单 | Excel 导入 |
|------|-------------|------------|
| 输入 | 非结构化文本 | `.xlsx` / `.xls` 工作簿 |
| 行数据解析 | LLM 抽取 | **前端 SheetJS 本地读表** |
| LLM 用途 | 整单字段抽取 | **仅表头列映射 + 品牌名映射** |
| 解析日志 | 写 `ai_entity_parse_log` | **不写**（列映射/品牌映射场景） |
| 落库时机 | 用户确认后 `RFQCreate` 保存 | 同左 |

**设计原则：**

1. **Excel 文件不上传服务器** — 降低传输与隐私风险；行内容仅在客户端处理。
2. **导入 ≠ 直接建单** — 必须进入 `RFQCreate` 补全单头（客户等）并确认明细后再 `POST /api/v1/rfqs`。
3. **品牌必选** — 保存时每行 `brandId > 0`；导入可留「品牌待选择」，由用户或学习映射补齐。
4. **AI 补漏** — 表头/品牌优先规则与学习表，AI 仅在规则不足时介入。

---

## 二、用户流程

```
需求列表 / 需求首页
  → 「导入 Excel 创建」或「新建需求」→「Excel 导入」
  → ImportRFQDialog（两步向导）
       Step 1：上传文件、选工作表、选表头行
       Step 2：列映射（规则 / AI / 手动）+ 品牌匹配（学习 / 规则 / AI / 手动）
  → 解析成功 → sessionStorage 预填（aiPrefill token）
  → 跳转 RFQCreate（?aiPrefill=token）
  → 用户补全单头、处理待选品牌 → 保存 RFQ
```

### 2.1 入口与权限

| 入口 | 位置 | 权限 |
|------|------|------|
| 工具栏按钮 | `RFQList.vue` —「导入 Excel 创建」 | `rfq.create` **且** `biz.ai.entity.parse.rfq` |
| 下拉菜单 | `RFQList.vue` / `RFQHome.vue` —「新建需求」→「Excel 导入」 | 同上 |

无 `biz.ai.entity.parse.rfq` 时入口不可见（列映射/品牌 AI 与文本 AI 建单共用该权限）。

### 2.2 对话框步骤

**Step 1 — 文件与工作表**

- 支持 `.xlsx`、`.xls`；可下载内置模板 `RFQ导入模板.xlsx`。
- 多工作表时下拉选择；切换工作表时表头行重置为第 1 行。
- 表头行：1-based 展示，可选前 10 行（`RFQ_EXCEL_MAX_HEADER_ROW_OPTIONS`）。
- 预览表头与样例行（最多 10 行预览）。

**Step 2 — 映射与匹配**

- 列映射表：Excel 列号、表头文案、映射字段、来源标签（`rule` / `ai` / `manual`）。
- 工具栏：
  - **通用规则匹配表头** — 同义词表 `HEADER_SYNONYMS` 重算；
  - **AI 智能匹配表头** — 场景 `entity.parse.rfq_excel_column_map`；
  - **AI 智能匹配品牌** — 场景 `entity.parse.rfq_excel_brand_map`（仅 `pending` 品牌）。
- 进入 Step 2 时：若规则映射后仍缺必填列（`mpn`、`quantity`），**自动静默调用一次** AI 列映射。
- 品牌统计标签：**规则/学习已匹配**、**AI 已匹配**、**品牌待选择**。
- 确认导入：校验通过 → `emit('parsed')` → 关闭对话框。

### 2.3 限制与校验

| 项 | 值 |
|----|-----|
| 最大明细行数 | 500（`RFQ_EXCEL_MAX_DATA_ROWS`） |
| 必填列 | `mpn`（物料型号）、`quantity`（数量） |
| 行级校验 | MPN 非空；数量 > 0 |
| 空行 | 跳过 |

---

## 三、列映射设计

### 3.1 可映射字段（13 项）

定义于 `CRM.Web/src/utils/rfqExcelColumnMap.ts` — `RFQ_EXCEL_FIELD_METAS`：

| 字段键 | 中文标签 | 必填 |
|--------|----------|------|
| `customer_mpn` | 客户物料型号 | 否 |
| `mpn` | 物料型号(MPN) | **是** |
| `customer_brand` | 客户品牌 | 否 |
| `brand` | 供应品牌 | 否 |
| `quantity` | 数量 | **是** |
| `target_price` | 目标价 | 否 |
| `price_currency` | 货币 | 否（默认结算币别） |
| `min_package_qty` | 最小包装量 | 否 |
| `min_order_qty` | 最小起订量 | 否 |
| `alternative_materials` | 可替代料 | 否 |
| `production_date` | 生产日期 | 否 |
| `expiry_date` | 有效期 | 否 |
| `remark` | 备注 | 否 |

### 3.2 规则映射（同义词）

`HEADER_SYNONYMS` 将归一化表头文本映射到字段键，覆盖中英文常见写法（如「料号」「Part No」「MPN」→ `mpn`）。

映射来源优先级展示：`manual` 覆盖 `ai` 覆盖 `rule`；用户改下拉后标记为 `manual`，并清除其他列对同一字段的重复映射。

### 3.3 AI 列映射

| 项 | 内容 |
|----|------|
| 场景码 | `entity.parse.rfq_excel_column_map` |
| API | `POST /api/v1/ai/invoke` |
| 权限 | `biz.ai.entity.parse.rfq` |
| 输入 | `headers`（JSON 表头数组）、`target_fields`（允许字段键列表） |
| 输出 JSON | `{ header_row_index, columns: [{ col_index, field, confidence }] }` |
| 缓存键 | `headers`（`cache_ttl_seconds` 默认 3600） |

**约束（Prompt）：** 只映射列，不解析行数据；每个 `field` 最多一列；`field` 必须为 `target_fields` 内值或 `null`。

脚本：`scripts/ai_entity_parse_rfq_excel_column_map_postgresql.sql`（Provider/Model 与 `entity.parse.rfq` 对齐）。

---

## 四、品牌匹配设计

品牌自动匹配与 **智能学习** 的完整设计见：[智能学习品牌匹配-设计与实现](./智能学习品牌匹配-设计与实现.md)。

Excel 导入场景摘要：

### 4.1 匹配关键词

每行取 **供应品牌 `brand`**；为空则用 **客户品牌 `customer_brand`**（`resolveBrandMatchKeyword`）。

### 4.2 匹配链（学习 → 规则 → AI）

```
导入原文 → 学习表 → 规则拆词匹配 →（可选）AI 标准名映射 → pending 手选 → 学习写入
```

- **学习 / 规则** 统计为「规则/学习已匹配」；**AI** 单独统计；**pending** 为「品牌待选择」。
- AI 场景：`entity.parse.rfq_excel_brand_map`（脚本 `scripts/ai_entity_parse_rfq_excel_brand_map_postgresql.sql`），**仅**处理 pending，不覆盖已学习/规则命中。
- 用户在 `RFQCreate` 手选品牌且存在 `_importBrandText` 时写入学习表（见智能学习品牌匹配文档 §4）。

---

## 五、预填与 RFQCreate 集成

### 5.1 预填桥接

`RfqExcelImportHost.vue`：

1. `rfqPrefillToFormPayload(parsed)` 转为表单结构；
2. 设置 `_prefillSource: 'excel-import'`；
3. `setAiPrefill('RFQ', formPayload)` → `sessionStorage`（30 分钟 TTL）；
4. `router.push({ name: 'RFQCreate', query: { aiPrefill: token } })`。

### 5.2 RFQCreate 行为差异

| 行为 | Excel 导入预填 | 其他预填 |
|------|----------------|----------|
| 品牌 ID 批量解析 | **跳过**（对话框已匹配） | 可能 toast 提示 |
| 待选品牌提示 | 行内「导入品牌「xxx」未能自动匹配，请手动选择」 | — |
| 选手品牌 | 若存在 `_importBrandText` → `rememberLearnedBrandMapping` | 不学习 |
| 保存校验 | 每行 `brandId > 0` | 同左 |

明细行扩展字段（导入上下文）：

- `_importBrandText` — 原始品牌文本（学习用）
- `_clientRowKey` — 列表/面板行稳定 key

---

## 六、后端 API 一览

**无 Excel 上传接口。** 相关 API：

| 方法 | 路径 | 用途 |
|------|------|------|
| POST | `/api/v1/ai/invoke` | 列映射 / 品牌映射（见 §3.3、§4.2） |
| GET | `/api/v1/biz-brands/options` | 品牌搜索（规则匹配） |
| POST | `/api/v1/biz-brands/learned-mappings/resolve` | 批量解析学习映射 |
| POST | `/api/v1/biz-brands/learned-mappings/remember` | 写入学习映射 |
| POST | `/api/v1/rfqs` | 最终创建 RFQ（`RFQService`，行级 `brandId` 校验） |

AI 常量：`CRM.Core/Constants/AiCodes.cs` — `RfqExcelColumnMap`、`RfqExcelBrandMap`。

`EntityParseNormalizer` **不**将上述两场景视为实体解析日志场景（不写 `ai_entity_parse_log`）。

开发环境 Mock：`CRM.Infrastructure/Ai/MockAiLlmProvider.cs` 提供启发式 JSON。

---

## 七、前端模块结构

```
CRM.Web/src/
├── views/RFQ/
│   ├── components/ImportRFQDialog.vue    # 两步导入向导（主 UI）
│   ├── RFQList.vue / RFQHome.vue         # 入口
│   └── RFQCreate.vue                     # 预填消费、品牌学习、保存
├── components/AiCreate/
│   └── RfqExcelImportHost.vue            # 对话框 + 路由桥接
└── utils/
    ├── rfqExcelWorkbook.ts               # SheetJS 读簿、多 sheet
    ├── rfqExcelColumnMap.ts              # 同义词、规则映射、行解析
    ├── rfqExcelBrandMap.ts               # AI 品牌响应解析
    ├── bizBrandMatch.ts                  # 学习/规则匹配链
    ├── entityParseSchema.ts              # ParsedRfqFields、预填转换
    └── aiPrefill.ts                      # sessionStorage token
```

**依赖：** `xlsx`（SheetJS）、`marked`（帮助无关）。

**国际化：** `locales/zh-CN.ts` / `en-US.ts` — 键前缀 `rfqExcelImport`。

---

## 八、运维与部署注意

| 项 | 说明 |
|----|------|
| AI 超时 | 前端 invoke 超时 180s；生产 Nginx 需配置 `/api/v1/ai/invoke` 读超时 ≥ 300s（见 `scripts/nginx-ai-invoke-timeout.snippet.conf`） |
| 帮助文档 | 构建时 `sync-help` 将 `help/` 拷入 `dist/help/`；部署 **不可** 排除 `*.md`（见 `deploy_full_to_server.ps1`） |
| 数据库 | 新环境依次执行：`ensure_biz_brand_learned_mapping_postgresql.sql`、两个 Excel AI 场景脚本 |

---

## 九、与 EBS / 原系统

EBS 无对等的「Excel 导入 RFQ」能力；本功能为 FrontCRM 新增。RFQ 主数据与明细字段口径可参考 `document/System/RFQ主状态枚举.md`、`document/System/RFQ明细状态枚举.md`。

---

## 十、源码索引

| 类别 | 路径 |
|------|------|
| 导入对话框 | `CRM.Web/src/views/RFQ/components/ImportRFQDialog.vue` |
| 预填宿主 | `CRM.Web/src/components/AiCreate/RfqExcelImportHost.vue` |
| 列映射 | `CRM.Web/src/utils/rfqExcelColumnMap.ts` |
| 品牌匹配 | `CRM.Web/src/utils/bizBrandMatch.ts` |
| 品牌服务 | `CRM.Infrastructure/Biz/BizBrandService.cs` |
| 品牌 API | `CRM.API/Controllers/BizBrandsController.cs` |
| AI 控制器 | `CRM.API/Controllers/AiController.cs` |
| 列映射 SQL | `scripts/ai_entity_parse_rfq_excel_column_map_postgresql.sql` |
| 品牌映射 SQL | `scripts/ai_entity_parse_rfq_excel_brand_map_postgresql.sql` |
| 学习表 SQL | `scripts/ensure_biz_brand_learned_mapping_postgresql.sql` |

---

*文档维护：功能变更时请同步更新本文与 [测试对照说明](../QA/需求报价/RFQ Excel导入-测试对照说明.md)。*
