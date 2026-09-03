[帮助文档目录](../帮助文档目录.md)

# 库存明细 · 看板

<nav class="help-toc-nav" aria-label="区块速览">
<a href="#help-isi-board-glossary">名词解释</a><span class="help-toc-sep">·</span><a href="#help-isi-board-kpi">在库概览</a><span class="help-toc-sep">·</span><a href="#help-isi-board-trend">趋势</a><span class="help-toc-sep">·</span><a href="#help-isi-board-breakdown">分布</a><span class="help-toc-sep">·</span><a href="#help-isi-board-rankings">排行</a>
</nav>

---

<h2 id="help-isi-board-glossary">名词解释</h2>

| 名词 | 说明 |
| --- | --- |
| 未分配业务员 | 通常是采购备货库存 |
| 无客户 / 备货 | 没有挂客户的在库 |

---

<h2 id="help-isi-board-kpi">在库概览</h2>

统计范围与当前搜索条件和页签一致（全部结果，不是当前页）。

| 指标 | 说明 |
| --- | --- |
| 在库数量 | 当前筛选下在库件数合计，与列表上方「在库数量」卡片相同 |
| 库存金额 | 各原币分别合计，不混加 |
| 周转天数 | 按在库数量和近 30 天出库估算还能周转多少天；没有出库或没有在库时显示「—」 |
| 呆滞料数量（>90天） | 入库超过 90 天（或没有入库日）的在库件数；**点击后回到列表**并只看这些呆滞行 |

---

<h2 id="help-isi-board-trend">趋势</h2>

可按 **天 / 周 / 月** 看各期末还剩多少在库（不是这段时间入库了多少）。默认大约最近 30 天、12 周或 12 个月。若搜索栏填了入库日期，趋势只落在该日期范围内。

| 图 | 说明 |
| --- | --- |
| 在库数量 | 各期末仍在库的件数 |
| 库存金额 · 各原币 | 各期末该币别在库金额 |

某原币没有在库时，对应金额趋势为 0。没有金额权限时不显示金额图。

---

<h2 id="help-isi-board-breakdown">分布</h2>

饼图可按 **数量** 或 **各原币金额** 查看：库存类型、仓库、业务员、库龄（与物流分析相同分桶：0–30 / 31–90 / 91–180 / 181–365 / 365 天以上）。没有入库日的行不进库龄图。

---

<h2 id="help-isi-board-rankings">排行</h2>

Top10：客户、业务员、物料（型号+品牌）、品牌。可在数量与各原币金额间切换。

**点击任一行** 会回到本页列表，只看该排行对应的库存明细，并带上当前搜索条件。
