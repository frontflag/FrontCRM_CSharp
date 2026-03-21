namespace CRM.Core.Validation
{
    /// <summary>
    /// 新建实体写入数据库前的业务校验（可注入仓储做存在性检查等）。
    /// </summary>
    public interface IBusinessCreateValidator<in TRequest>
    {
        Task<ValidationResult> ValidateForCreateAsync(TRequest request, CancellationToken cancellationToken = default);
    }
}
