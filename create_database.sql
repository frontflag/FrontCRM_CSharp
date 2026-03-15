-- 创建 FrontCRM 数据库
CREATE DATABASE "FrontCRM"
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'Chinese (Simplified)_China.936'
    LC_CTYPE = 'Chinese (Simplified)_China.936'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;

-- 切换到 FrontCRM 数据库
\c FrontCRM

-- 创建扩展（如果需要）
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 创建表空间（可选）
-- CREATE TABLESPACE frontcrm_tablespace
--   OWNER postgres
--   LOCATION 'C:\Program Files\PostgreSQL\16\data\tablespaces\frontcrm';

-- 设置数据库默认表空间
-- ALTER DATABASE "FrontCRM" SET TABLESPACE frontcrm_tablespace;