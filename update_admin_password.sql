-- 更新 Admin 密码为 Admin123（BCrypt，与 seed_initial_rbac_admin.sql 一致）

UPDATE "user"
SET "Password" = '$2a$11$7pie8fLiEFHn0zsateSai.01o9jGO/Hn2KHgEzTKwn88kPX/KeaNe',
    "PasswordPlain" = 'Admin123',
    "IsActive" = true,
    "Status" = 1
WHERE "UserName" = 'Admin';

INSERT INTO "user" ("UserId", "UserName", "Email", "Password", "Salt", "PasswordPlain", "IsActive", "Status", "CreateTime")
SELECT
  'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
  'Admin',
  'admin@frontcrm.com',
  '$2a$11$7pie8fLiEFHn0zsateSai.01o9jGO/Hn2KHgEzTKwn88kPX/KeaNe',
  'init_salt',
  'Admin123',
  true,
  1,
  CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM "user" WHERE "UserName" = 'Admin');

SELECT "UserName", "Email", "IsActive", "Status" FROM "user" WHERE "UserName" = 'Admin';
