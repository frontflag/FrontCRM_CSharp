-- 更新 admin 密码为 Admin123
-- 使用正确的 BCrypt 哈希

UPDATE "user" 
SET "Password" = '$2a$11$vqvt8BRISDc6itm/ANyMGOo39xI8vkqEI.8IRvfcEW2mV9IA77to.',
    "PasswordPlain" = 'Admin123',
    "IsActive" = true,
    "Status" = 1
WHERE "UserName" = 'admin';

-- 如果没有找到用户，插入新用户
INSERT INTO "user" ("UserId", "UserName", "Email", "Password", "Salt", "PasswordPlain", "IsActive", "Status", "CreateTime")
SELECT
  'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
  'admin',
  'admin@frontcrm.com',
  '$2a$11$NG0gQf4DfJDQRP47SJ3OXueDdnuKS3gNjU4lLFCZ35Q9ypDPRFJfu$',
  'init_salt',
  'Admin123',
  true,
  1,
  CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM "user" WHERE "UserName" = 'admin');

-- 验证
SELECT "UserName", "Email", "IsActive", "Status" FROM "user" WHERE "UserName" = 'admin';
