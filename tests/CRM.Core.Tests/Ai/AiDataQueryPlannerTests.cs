using CRM.Core.Ai;
using CRM.Core.Models.Ai;

namespace CRM.Core.Tests.Ai;

public class AiDataQueryPlannerTests
{
    [Fact]
    public void Performance_asks_metric_before_query()
    {
        var plan = AiDataQueryPlanner.Plan("本月业绩", null);

        Assert.Equal(AiDataQueryKinds.Clarify, plan.Kind);
        Assert.Equal(AiDataQueryIntents.Performance, plan.State.Intent);
        Assert.Equal("thisMonth", plan.State.Period);
        Assert.Equal(AiDataQuerySlotNames.Metric, plan.State.PendingSlot);
        Assert.Contains(plan.Options, o => o.Id == "stockOutAmount");
    }

    [Fact]
    public void Answer_fills_metric_and_keeps_period()
    {
        var first = AiDataQueryPlanner.Plan("本月业绩", null);
        var plan = AiDataQueryPlanner.Plan("出库金额", first.State);

        Assert.Equal(AiDataQueryKinds.Result, plan.Kind);
        Assert.Equal("stockOutAmount", plan.State.Metric);
        Assert.Equal("thisMonth", plan.State.Period);
        Assert.Null(plan.State.PendingSlot);
    }

    [Fact]
    public void Period_change_keeps_metric()
    {
        var ready = AiDataQueryPlanner.Plan("出库金额", AiDataQueryPlanner.Plan("本月业绩", null).State);
        var plan = AiDataQueryPlanner.Plan("换成近三个月", ready.State);

        Assert.Equal(AiDataQueryKinds.Result, plan.Kind);
        Assert.Equal("stockOutAmount", plan.State.Metric);
        Assert.Equal("last3Months", plan.State.Period);
    }

    [Fact]
    public void New_intent_drops_previous_slots()
    {
        var ready = AiDataQueryPlanner.Plan("出库金额", AiDataQueryPlanner.Plan("本月业绩", null).State);
        var plan = AiDataQueryPlanner.Plan("当前库存金额", ready.State);

        Assert.Equal(AiDataQueryKinds.Result, plan.Kind);
        Assert.Equal(AiDataQueryIntents.InventoryAmount, plan.State.Intent);
        Assert.Null(plan.State.Metric);
        Assert.Null(plan.State.Period);
    }

    [Fact]
    public void Receivable_and_inventory_do_not_ask()
    {
        Assert.Equal(AiDataQueryKinds.Result, AiDataQueryPlanner.Plan("当前待收款", null).Kind);
        Assert.Equal(AiDataQueryKinds.Result, AiDataQueryPlanner.Plan("哪些客户有应收", null).Kind);
        Assert.Equal(AiDataQueryKinds.Result, AiDataQueryPlanner.Plan("当前库存金额", null).Kind);
    }

    [Fact]
    public void Top_brands_asks_measure_then_basis()
    {
        var first = AiDataQueryPlanner.Plan("今天卖得最好的 Top10 品牌", null);
        Assert.Equal(AiDataQuerySlotNames.Measure, first.State.PendingSlot);
        Assert.Equal(10, first.State.TopN);
        Assert.Equal("today", first.State.Period);

        var second = AiDataQueryPlanner.Plan("按金额", first.State);
        Assert.Equal(AiDataQuerySlotNames.Basis, second.State.PendingSlot);

        var third = AiDataQueryPlanner.Plan("按出库", second.State);
        Assert.Equal(AiDataQueryKinds.Result, third.Kind);
        Assert.Equal("amount", third.State.Measure);
        Assert.Equal("stockOut", third.State.Basis);
    }

    [Fact]
    public void Backlog_asks_days_and_accepts_typed_number()
    {
        var first = AiDataQueryPlanner.Plan("仓库积压最多的品牌", null);
        Assert.Equal(AiDataQuerySlotNames.AgeDays, first.State.PendingSlot);
        Assert.Equal("brand", first.State.Dimension);

        var second = AiDataQueryPlanner.Plan("75", first.State);
        Assert.Equal(75, second.State.AgeDays);
        Assert.Equal(AiDataQuerySlotNames.Measure, second.State.PendingSlot);
    }

    [Fact]
    public void Order_trend_asks_amount_or_count()
    {
        var plan = AiDataQueryPlanner.Plan("本月订单趋势", null);
        Assert.Equal(AiDataQueryIntents.OrderTrend, plan.State.Intent);
        Assert.Equal(AiDataQuerySlotNames.Metric, plan.State.PendingSlot);
        Assert.Equal("thisMonth", plan.State.Period);
        Assert.Contains(plan.Options, o => o.Id == "orderCount");
        Assert.DoesNotContain(plan.Options, o => o.Id == "stockOutAmount");
    }

    [Fact]
    public void Profit_is_not_queried()
    {
        var first = AiDataQueryPlanner.Plan("本月业绩", null);
        var plan = AiDataQueryPlanner.Plan("毛利", first.State);

        Assert.Equal(AiDataQueryKinds.Clarify, plan.Kind);
        Assert.Null(plan.State.Metric);
        Assert.Contains("还不能查", plan.Summary);
    }

    [Fact]
    public void Two_misses_still_do_not_query()
    {
        var first = AiDataQueryPlanner.Plan("本月业绩", null);
        var second = AiDataQueryPlanner.Plan("随便说说", first.State);
        var third = AiDataQueryPlanner.Plan("还是随便", second.State);

        Assert.Equal(AiDataQueryKinds.Clarify, third.Kind);
        Assert.True(third.State.MissCount >= 2);
        Assert.Contains(third.Options, o => o.Id == "stockOutAmount");
    }

    [Fact]
    public void New_orders_ask_count_and_amount_together()
    {
        var plan = AiDataQueryPlanner.Plan("本月新建订单是多少条？金额是？", null);

        Assert.Equal(AiDataQueryKinds.Result, plan.Kind);
        Assert.Equal(AiDataQueryIntents.Performance, plan.State.Intent);
        Assert.Equal("orderCountAndAmount", plan.State.Metric);
        Assert.Equal("thisMonth", plan.State.Period);
    }

    [Fact]
    public void Unknown_question_is_unsupported()
    {
        var plan = AiDataQueryPlanner.Plan("今天天气怎么样", null);
        Assert.Equal(AiDataQueryKinds.Unsupported, plan.Kind);
    }
}
