namespace CRM.Core.Validation
{
    /// <summary>
    /// 业务规则校验未通过时抛出，由 API 层映射为 400 及统一响应体。
    /// </summary>
    public sealed class BusinessValidationException : Exception
    {
        public BusinessValidationException(ValidationResult result)
            : base(result.CombineMessage())
        {
            Result = result;
        }

        public ValidationResult Result { get; }
    }
}
