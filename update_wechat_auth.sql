-- ============================================================
-- 微信扫码登录功能数据库迁移脚本
-- 自主绑定模式
-- ============================================================

-- 1. 更新用户表，增加微信相关字段
ALTER TABLE "user" 
ADD COLUMN IF NOT EXISTS "WechatOpenId" VARCHAR(100),
ADD COLUMN IF NOT EXISTS "WechatUnionId" VARCHAR(100),
ADD COLUMN IF NOT EXISTS "WechatNickname" VARCHAR(100),
ADD COLUMN IF NOT EXISTS "WechatAvatarUrl" VARCHAR(500),
ADD COLUMN IF NOT EXISTS "WechatBindTime" TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS "LoginType" SMALLINT DEFAULT 0;  -- 0=账号密码, 1=微信扫码

-- 2. 创建索引
CREATE INDEX IF NOT EXISTS "IX_User_WechatOpenId" ON "user" ("WechatOpenId");
CREATE INDEX IF NOT EXISTS "IX_User_WechatUnionId" ON "user" ("WechatUnionId");

-- 3. 创建微信登录票据表（用于扫码登录）
CREATE TABLE IF NOT EXISTS "wechat_login_ticket" (
    "Ticket" VARCHAR(64) PRIMARY KEY,
    "QrCodeUrl" VARCHAR(500) NOT NULL,
    "Status" SMALLINT DEFAULT 0,  -- 0=待扫码, 1=已扫码, 2=已确认, 3=已过期, 4=已取消, 5=未绑定
    "OpenId" VARCHAR(100),
    "UnionId" VARCHAR(100),
    "UserId" VARCHAR(100),
    "CreateTime" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "ExpireTime" TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_WechatLoginTicket_Status" ON "wechat_login_ticket" ("Status");
CREATE INDEX IF NOT EXISTS "IX_WechatLoginTicket_ExpireTime" ON "wechat_login_ticket" ("ExpireTime");
CREATE INDEX IF NOT EXISTS "IX_WechatLoginTicket_OpenId" ON "wechat_login_ticket" ("OpenId");

-- 4. 创建微信绑定请求表（用于用户自助绑定）
CREATE TABLE IF NOT EXISTS "wechat_bind_request" (
    "Id" VARCHAR(64) PRIMARY KEY,
    "UserId" VARCHAR(100) NOT NULL,
    "Status" VARCHAR(20) DEFAULT 'pending',  -- pending/scanned/success/expired
    "OpenId" VARCHAR(100),
    "UnionId" VARCHAR(100),
    "Nickname" VARCHAR(100),
    "AvatarUrl" VARCHAR(500),
    "CreateTime" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "ExpireTime" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CompleteTime" TIMESTAMP WITH TIME ZONE
);

CREATE INDEX IF NOT EXISTS "IX_WechatBindRequest_UserId" ON "wechat_bind_request" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_WechatBindRequest_Status" ON "wechat_bind_request" ("Status");
CREATE INDEX IF NOT EXISTS "IX_WechatBindRequest_ExpireTime" ON "wechat_bind_request" ("ExpireTime");

-- 5. 注释说明
COMMENT ON COLUMN "user"."WechatOpenId" IS '微信OpenId（同一公众号下唯一）';
COMMENT ON COLUMN "user"."WechatUnionId" IS '微信UnionId（同一主体多个应用唯一）';
COMMENT ON COLUMN "user"."WechatNickname" IS '微信昵称';
COMMENT ON COLUMN "user"."WechatAvatarUrl" IS '微信头像URL';
COMMENT ON COLUMN "user"."WechatBindTime" IS '微信绑定时间';
COMMENT ON COLUMN "user"."LoginType" IS '登录方式：0=账号密码, 1=微信扫码';

COMMENT ON TABLE "wechat_login_ticket" IS '微信扫码登录临时票据表';
COMMENT ON TABLE "wechat_bind_request" IS '微信绑定请求表（用户自助绑定）';
