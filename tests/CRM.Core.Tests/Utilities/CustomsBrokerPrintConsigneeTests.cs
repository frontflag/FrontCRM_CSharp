using CRM.Core.Models.Customs;
using CRM.Core.Utilities;
using FluentAssertions;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class CustomsBrokerPrintConsigneeTests
{
    private static CustomsBroker ReadyBroker() => new()
    {
        Id = "b1",
        BrokerCode = "CBR0001",
        Cname = "深圳报关行",
        Ename = "SZ Broker Ltd",
        ContactName = "张三",
        Tel = "0755-12345678",
        Email = "a@broker.com",
        Address = "深圳市福田区1号"
    };

    [Fact]
    public void ResolvePrintName_prefers_ename()
    {
        CustomsBrokerPrintConsignee.ResolvePrintName(ReadyBroker()).Should().Be("SZ Broker Ltd");
    }

    [Fact]
    public void ResolvePrintName_falls_back_to_cname()
    {
        var broker = ReadyBroker();
        broker.Ename = "  ";
        CustomsBrokerPrintConsignee.ResolvePrintName(broker).Should().Be("深圳报关行");
    }

    [Fact]
    public void BuildAddressLines_maps_consignee_block()
    {
        var lines = CustomsBrokerPrintConsignee.BuildAddressLines(ReadyBroker());
        lines.Should().Equal("SZ Broker Ltd", "深圳市福田区1号", "张三", "0755-12345678");
        CustomsBrokerPrintConsignee.PrintEmail(ReadyBroker()).Should().Be("a@broker.com");
    }

    [Fact]
    public void EnsurePrintReady_throws_when_contact_missing()
    {
        var broker = ReadyBroker();
        broker.ContactName = null;
        var act = () => CustomsBrokerPrintConsignee.EnsurePrintReady(broker);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage(CustomsBrokerPrintConsignee.IncompleteForPrintMessage);
    }

    [Fact]
    public void EnsurePrintReady_throws_when_broker_null()
    {
        var act = () => CustomsBrokerPrintConsignee.EnsurePrintReady(null);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage(CustomsBrokerPrintConsignee.MissingBrokerForPrintMessage);
    }
}
