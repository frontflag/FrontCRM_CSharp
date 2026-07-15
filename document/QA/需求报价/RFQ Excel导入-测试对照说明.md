# RFQ Excel 导入 — 测试对照说明

> **设计需求与实现**见：[RFQ Excel导入-设计与实现.md](../../System/RFQ Excel导入-设计与实现.md)  
> **关联**：[AI实体解析建单-设计与实现.md](../../System/AI实体解析建单-设计与实现.md)（文本 AI 建 RFQ）

| 项 | 内容 |
|----|------|
| 菜单路径 | 需求 → 需求列表 / 需求首页 |
| 入口 | 工具栏「导入 Excel 创建」；或「新建需求」→「Excel 导入」 |
| 目标页 | `/rfqs/create?aiPrefill=<token>` |
| 权限 | `rfq.create` + `biz.ai.entity.parse.rfq`（入口可见） |
| AI 场景 | `entity.parse.rfq_excel_column_map`、`entity.parse.rfq_excel_brand_map` |

---

## 一、前置条件

| # | 检查项 | 预期 |
|---|--------|------|
| 1 | 账号具备 `rfq.create` | 可进入新建需求 |
| 2 | 账号具备 `biz.ai.entity.parse.rfq` | 可见 Excel 导入入口；AI 按钮可用 |
| 3 | 已执行 AI 场景脚本 | `ai_scenario` 存在上述两场景且 `is_enabled = true` |
| 4 | 已执行 `ensure_biz_brand_learned_mapping_postgresql.sql` | 表 `biz_brand_learned_mapping` 存在 |
| 5 | 标准品牌主数据 | `biz_brand` 有测试用品牌（如 TI、ST） |

---

## 二、Step 1 — 文件与工作表

| # | 操作 | 预期 |
|---|------|------|
| 1 | 打开导入对话框 | 显示 Step 1 上传区 |
| 2 | 下载模板 | 得到 `RFQ导入模板.xlsx`，含示例表头 |
| 3 | 上传含 2 个 sheet 的 xlsx | 出现「工作表」下拉，默认第一个 sheet |
| 4 | 切换工作表 | 表头行重置为 1；预览更新 |
| 5 | 调整表头行为第 2 行 | 预览以第 2 行作为列名 |
| 6 | 上传超过 500 行数据 | 提示超限或仅导入前 500 行（与实现一致） |
| 7 | 上传非 xlsx/xls | 拒绝或提示格式错误 |

---

## 三、Step 2 — 列映射

### 3.1 规则映射

| # | 操作 | 预期 |
|---|------|------|
| 1 | 表头含「物料型号」「数量」 | 自动映射 `mpn`、`quantity`，来源 `rule` |
| 2 | 点击「通用规则匹配表头」 | 同义词重新匹配，必填列就绪 |
| 3 | 手动改某列映射字段 | 来源变为 `manual`；同字段其他列被清空 |

### 3.2 AI 列映射

| # | 操作 | 预期 |
|---|------|------|
| 1 | 表头为非常规文案（无同义词） | 进入 Step 2 后自动静默 AI 一次（若缺必填列） |
| 2 | 点击「AI 智能匹配表头」 | 调用 invoke；映射表更新，来源含 `ai` |
| 3 | 无 AI 权限账号 | AI 按钮不可用或 invoke 403 |

**核对 AI 请求体：**

```json
{
  "scenarioCode": "entity.parse.rfq_excel_column_map",
  "input": {
    "headers": "[...]",
    "target_fields": "[\"customer_mpn\",\"mpn\",...]"
  },
  "bizType": "RFQ"
}
```

### 3.3 行解析

| # | 数据 | 预期 |
|---|------|------|
| 1 | MPN 为空 | 该行跳过或计入无效（不进入明细） |
| 2 | 数量 ≤ 0 或非数字 | 该行无效 |
| 3 | 目标价 + 币别列 | `targetPrice`、`priceCurrency` 正确；缺币别用默认结算币别 |
| 4 | 空行 | 跳过 |

---

## 四、品牌匹配

> 学习表与匹配链细则见：[智能学习品牌匹配-测试对照说明.md](./智能学习品牌匹配-测试对照说明.md)

### 4.1 学习表（优先）

| # | 操作 | 预期 |
|---|------|------|
| 1 | 先在 RFQCreate 对「TI(德州仪器)」选手品牌 TI 并保存过一次学习 | `biz_brand_learned_mapping` 有对应 `source_key` |
| 2 | 再次导入含「TI(德州仪器)」的 Excel | 统计「规则/学习已匹配」+1；行 `brandId` 已填 |

### 4.2 规则匹配

| # | 导入品牌列 | 预期 |
|---|------------|------|
| 1 | 与 `biz_brand.standard_brand` 完全一致 | `matched`，`mappingSource=rule` |
| 2 | 与英文名/中文名/别名唯一匹配 | `matched` |
| 3 | 多品牌同名歧义 | `pending` |

### 4.3 AI 品牌映射

| # | 操作 | 预期 |
|---|------|------|
| 1 | 存在 `pending` 品牌 | 「AI 智能匹配品牌」可点 |
| 2 | 点击后 | 仅 pending 文本送入 `source_texts`；命中后 `mappingSource=ai` |
| 3 | 已全部 matched | 按钮禁用或提示无需 AI |

### 4.4 统计标签

| 标签 | 含义 |
|------|------|
| 规则/学习已匹配 | `mappingSource` 为 `learned` 或 `rule` |
| AI 已匹配 | `mappingSource` 为 `ai` |
| 品牌待选择 | `status=pending` 且有关键词 |

---

## 五、预填与 RFQCreate

| # | 操作 | 预期 |
|---|------|------|
| 1 | Step 2 确认导入 | 跳转 `/rfqs/create?aiPrefill=...` |
| 2 | 明细行数 | 与 Excel 有效行一致（≤500） |
| 3 | 单头字段 | 客户等需用户补全（Excel 不解析单头） |
| 4 | 待选品牌行 | 显示橙色提示「导入品牌「xxx」未能自动匹配…」 |
| 5 | 手动选品牌（有 `_importBrandText`） | Toast「已记住该品牌映射…」；DB `hit_count` 增加 |
| 6 | 刷新页面 | 预填 token 消费一次后不再自动填充 |
| 7 | 未选齐品牌点保存 | 校验失败，提示品牌必选 |
| 8 | 选齐品牌保存 | `POST /api/v1/rfqs` 成功；`rfqitem.brand_id` 有值 |

---

## 六、API 核对

| 接口 | 方法 | 关键校验 |
|------|------|----------|
| `/api/v1/ai/invoke` | POST | 场景码、权限、180s 内响应 |
| `/api/v1/biz-brands/learned-mappings/resolve` | POST | 返回 `sourceText`、`brandId`、`standardBrand` |
| `/api/v1/biz-brands/learned-mappings/remember` | POST | 同 `source_key` 重复写入覆盖 `brand_id` |
| `/api/v1/biz-brands/options?keyword=` | GET | 规则匹配候选来源 |
| `/api/v1/rfqs` | POST | 每行 `brandId` 有效 |

**负向：**

- 无 `biz.ai.entity.parse.rfq`：工具栏无「导入 Excel 创建」。
- invoke 超时：前端提示失败，映射不变。

---

## 七、权限矩阵

| 权限 | 无 | 有 |
|------|----|----|
| `rfq.create` | 不可新建 / 不可导入 | 可进入 RFQCreate |
| `biz.ai.entity.parse.rfq` | 无 Excel 导入入口 | 入口 + AI 按钮可用 |

---

## 八、回归清单（发版前）

- [ ] 单 sheet / 多 sheet Excel 均可导入
- [ ] 规则映射 + AI 列映射 + 手动改映射
- [ ] 学习映射 → 二次导入命中
- [ ] AI 品牌映射仅处理 pending
- [ ] RFQCreate 保存成功且 `brand_id` 正确
- [ ] 生产部署包 `dist/help` 含需求列表帮助（若已更新 help 文案）

---

*测试问题请对照设计文档 § 三～§ 五 与源码 `ImportRFQDialog.vue`、`bizBrandMatch.ts`。*
