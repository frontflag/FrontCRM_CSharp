-- 登录日志表 log_login（与 CRM.Core.Models.System.LoginLog、EF 映射一致；列名 PascalCase 与 log_operation 一致）
CREATE TABLE IF NOT EXISTS public.log_login (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "UserId" VARCHAR(36) NOT NULL,
    "UserName" VARCHAR(100) NOT NULL,
    "LoginAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "ClientIp" VARCHAR(45) NOT NULL,
    "Country" VARCHAR(100),
    "Province" VARCHAR(100),
    "City" VARCHAR(100),
    "District" VARCHAR(100),
    "Street" VARCHAR(200),
    "AddressLine" VARCHAR(500),
    "RegionRaw" VARCHAR(500),
    "LoginMethod" SMALLINT NOT NULL CHECK ("LoginMethod" BETWEEN 1 AND 3),
    "ActorUserId" VARCHAR(36),
    "GeoSource" VARCHAR(32) NOT NULL DEFAULT 'none'
);

CREATE INDEX IF NOT EXISTS idx_log_login_login_at ON public.log_login ("LoginAt" DESC);
CREATE INDEX IF NOT EXISTS idx_log_login_user_id ON public.log_login ("UserId");
