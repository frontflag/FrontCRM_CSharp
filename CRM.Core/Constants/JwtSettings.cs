namespace CRM.Core.Constants
{
    public static class JwtSettings
    {
        public const string SecretKey = "FrontCRM_Secret_Key_2024_Very_Long_Secret_Key_For_JWT_Token";
        public const string Issuer = "FrontCRM";
        public const string Audience = "FrontCRMUsers";
        public const int ExpirationMinutes = 480;
    }
}
