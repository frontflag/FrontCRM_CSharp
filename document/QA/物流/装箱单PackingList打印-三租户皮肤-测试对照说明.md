# 装箱单 Packing List 打印 — 三租户皮肤 — 测试对照说明

> **设计文档：** [装箱单PackingList打印-三租户皮肤-设计与实现](../../System/物流/装箱单PackingList打印-三租户皮肤-设计与实现.md)  
> **PRD：** [报表规范-PackingList](../../PRD/规范/业务规范/报表规范-PackingList.md)

---

## 一、前置

- 准备同一装箱单（建议含 ≥2 行明细、有 Bill/Ship 地址）。
- 分别在 **semicore / idesemi / ecoinf** 前端构建包（或对应 Vite mode）中打开：

```
/inventory/packing/{id}/packing-report/with-inspection
/inventory/packing/{id}/packing-report/without-inspection
```

---

## 二、三租户观感（必过）

| 核对项 | Semicore | IdeSemi | EcoInf |
|--------|----------|---------|--------|
| 一眼区分 | 橙色表头/分区条 | 深紫顶栏 + 琥珀强调 | 大标题字距 + 绿竖条、无橙块 |
| Logo/公司名 | 来自公司档案 | 同左 | 同左 |
| 明细列 | No/PN/Brand/Qty/Carton/Remark | 同左 | 同左 |
| 含检 | 有 Outbound Inspection 橙条表 | 有 QC，序号圆点、无橙条 | 有 QC 清单 + checkbox |
| 不含检 | 无 QC 区 | 同左 | 同左 |

任一套皮肤若与另两套「只差 Logo」→ 不通过。

---

## 三、功能回归（每租户各测一次即可抽样）

| 步骤 | 预期 |
|------|------|
| 中文 / 英文切换 | 标签语言切换，明细数据不变 |
| 印章开/关 | 发货方区印章显示/隐藏 |
| 打印预览 | 无顶栏/侧栏；表头或品牌色保留 |
| 明细空 | 显示「无明细 / No items」类提示 |

---

## 四、Semicore 回归

相对改版前橙表：布局、橙色、Bill/Ship 双列表头、填充空行、合计行应一致，避免现网客户观感回退。
