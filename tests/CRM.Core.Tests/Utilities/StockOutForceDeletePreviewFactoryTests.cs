using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class StockOutForceDeletePreviewFactoryTests
{
    [Fact]
    public void Status4_WillRollbackInventory()
    {
        Assert.True(StockOutForceDeletePreviewFactory.WillRollbackInventory(4));
        Assert.True(StockOutForceDeletePreviewFactory.WillRollbackInventory(2));
        Assert.False(StockOutForceDeletePreviewFactory.WillRollbackInventory(0));
        Assert.False(StockOutForceDeletePreviewFactory.WillRollbackInventory(1));
    }

    [Fact]
    public void NoReceivable_CanDelete_DoesNotVoid()
    {
        var dto = StockOutForceDeletePreviewFactory.Create(
            new StockOut { Id = "so1", StockOutCode = "STO1", Status = 4 },
            ForceDeleteGuardResult.Allow(),
            Array.Empty<StockOutForceDeleteReceivableRow>());

        Assert.True(dto.CanForceDelete);
        Assert.Null(dto.BlockReason);
        Assert.False(dto.WillVoidReceivables);
        Assert.True(dto.WillRollbackInventory);
        Assert.Empty(dto.Receivables);
    }

    [Fact]
    public void UnverifiedReceivable_CanDelete_WillVoid()
    {
        var recs = new[]
        {
            new StockOutForceDeleteReceivableRow
            {
                Id = "ar1",
                ReceivableCode = "ARV1",
                Amount = 100m,
                VerifiedDone = 0m,
                VerificationStatus = 0
            }
        };
        var dto = StockOutForceDeletePreviewFactory.Create(
            new StockOut { Id = "so1", StockOutCode = "STO1", Status = 4 },
            ForceDeleteGuardResult.Allow(),
            recs);

        Assert.True(dto.CanForceDelete);
        Assert.True(dto.WillVoidReceivables);
        Assert.Equal("ARV1", dto.Receivables[0].ReceivableCode);
    }

    [Fact]
    public void Verified_Blocked_DoesNotVoid()
    {
        var recs = new[]
        {
            new StockOutForceDeleteReceivableRow
            {
                Id = "ar1",
                ReceivableCode = "ARV1",
                Amount = 100m,
                VerifiedDone = 40m,
                VerificationStatus = 1,
                ReceiptCodes = new[] { "FRC1" }
            }
        };
        var dto = StockOutForceDeletePreviewFactory.Create(
            new StockOut { Id = "so1", StockOutCode = "STO1", Status = 4 },
            ForceDeleteGuardResult.Deny("该出库单已有收款核销（已核销 40），不可删除"),
            recs);

        Assert.False(dto.CanForceDelete);
        Assert.False(dto.WillVoidReceivables);
        Assert.Contains("已有收款核销", dto.BlockReason);
        Assert.Equal("FRC1", dto.Receivables[0].ReceiptCodes[0]);
    }
}
