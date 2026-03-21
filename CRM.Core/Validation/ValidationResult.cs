namespace CRM.Core.Validation
{
    /// <summary>业务保存前校验结果（可包含多条错误信息）。</summary>
    public sealed class ValidationResult
    {
        private ValidationResult(IReadOnlyList<string> errors)
        {
            Errors = errors;
        }

        public IReadOnlyList<string> Errors { get; }

        public bool Succeeded => Errors.Count == 0;

        public string CombineMessage() =>
            Errors.Count == 0 ? string.Empty : string.Join("；", Errors);

        public static ValidationResult Success() => new(Array.Empty<string>());

        public static ValidationResult Failure(params string[] errors) =>
            new(errors.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList());

        public static ValidationResult Failure(IEnumerable<string> errors) =>
            Failure(errors.ToArray());
    }
}
