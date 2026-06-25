-- 客户银行账户：银行地址、银行代码、SWIFT
ALTER TABLE customerbankinfo ADD COLUMN IF NOT EXISTS "BankAddress" character varying(500);
ALTER TABLE customerbankinfo ADD COLUMN IF NOT EXISTS "BankCode" character varying(32);
ALTER TABLE customerbankinfo ADD COLUMN IF NOT EXISTS "Swift" character varying(64);
