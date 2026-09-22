using CRM.Core.Constants;

namespace CRM.Core.Tests.Constants;

public class UploadDocumentCategoryTests
{
    [Theory]
    [InlineData(null, UploadDocumentCategory.Other)]
    [InlineData("", UploadDocumentCategory.Other)]
    [InlineData("  ", UploadDocumentCategory.Other)]
    [InlineData("unknown", UploadDocumentCategory.Other)]
    [InlineData("SHIP_PHOTO", UploadDocumentCategory.ShipPhoto)]
    [InlineData("ship_photo", UploadDocumentCategory.ShipPhoto)]
    [InlineData("POD", UploadDocumentCategory.Pod)]
    [InlineData("pod", UploadDocumentCategory.Pod)]
    [InlineData("CONTRACT", UploadDocumentCategory.Contract)]
    [InlineData("contract", UploadDocumentCategory.Contract)]
    [InlineData("OTHER", UploadDocumentCategory.Other)]
    public void Normalize_MapsKnownCodesAndFallsBackToOther(string? raw, string expected)
    {
        Assert.Equal(expected, UploadDocumentCategory.Normalize(raw));
    }
}
