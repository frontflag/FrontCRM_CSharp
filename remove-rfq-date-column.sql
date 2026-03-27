-- Remove RFQ demand date column from database.
-- Safe for repeated execution.

BEGIN;

ALTER TABLE IF EXISTS rfq
  DROP COLUMN IF EXISTS rfq_date;

-- Compatibility for historical mixed-case schema.
ALTER TABLE IF EXISTS "rfq"
  DROP COLUMN IF EXISTS "RfqDate";

COMMIT;
