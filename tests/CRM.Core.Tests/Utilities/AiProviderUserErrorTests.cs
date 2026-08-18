using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class AiProviderUserErrorTests
{
    [Fact]
    public void Http429_QuotaJson_DoesNotLeakOrgOrKey()
    {
        const string body =
            """{"error":{"message":"Your account org-aa617028b1674ccba4ba5a34907a2514 <ak-f4jxscgbkbb111cjqsa1> is suspended due to insufficient balance, please recharge your account","type":"exceeded_current_quota_error"}}""";

        var msg = AiProviderUserError.FromHttp(429, body);

        Assert.Equal(AiProviderUserError.Quota, msg);
        Assert.DoesNotContain("org-", msg);
        Assert.DoesNotContain("ak-", msg);
        Assert.DoesNotContain("insufficient balance", msg);
    }

    [Fact]
    public void Http401_MapsToAuth()
    {
        Assert.Equal(AiProviderUserError.Auth, AiProviderUserError.FromHttp(401, "Invalid Authentication"));
    }

    [Fact]
    public void Http500_MapsToUnavailable()
    {
        Assert.Equal(AiProviderUserError.Unavailable, AiProviderUserError.FromHttp(500, "upstream"));
    }

    [Fact]
    public void Other4xx_IsGeneric_WithoutBody()
    {
        var msg = AiProviderUserError.FromHttp(400, """{"error":{"message":"bad request"}}""");
        Assert.Equal(AiProviderUserError.Generic, msg);
        Assert.DoesNotContain("bad request", msg);
    }
}
