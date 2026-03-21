using CRM.Core.Interfaces;

namespace CRM.Core.Validation.Rfq
{
    /// <summary>新建 RFQ 前的业务规则校验。</summary>
    public sealed class CreateRfqValidator : IBusinessCreateValidator<CreateRFQRequest>
    {
        public Task<ValidationResult> ValidateForCreateAsync(CreateRFQRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Items == null || request.Items.Count == 0)
                return Task.FromResult(ValidationResult.Failure("请添加需求明细"));

            return Task.FromResult(ValidationResult.Success());
        }
    }
}
