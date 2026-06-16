using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;
using NSubstitute;

namespace CRM.TestCommon.Biz;

/// <summary>为单元/集成测试提供 <see cref="IBizBrandService"/> 替身。</summary>
public static class BizBrandTestSubstitute
{
    /// <param name="standardBrandById">按品牌 ID 返回标准品牌名；未配置时使用 <c>Brand-{id}</c>。</param>
    public static IBizBrandService Create(Dictionary<long, string>? standardBrandById = null)
    {
        var map = standardBrandById ?? new Dictionary<long, string> { [1] = "Brand-A" };
        var svc = Substitute.For<IBizBrandService>();
        svc.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var id = ci.Arg<long>();
                var name = map.TryGetValue(id, out var n) ? n : $"Brand-{id}";
                return new BizBrandRowDto
                {
                    Id = id,
                    StandardBrand = name,
                    AuditStatus = BizBrandAuditStatus.Approved
                };
            });
        return svc;
    }
}
