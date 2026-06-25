-- 报价明细 converted_price：由历史「折合人民币」口径改为与订单 convert_price 一致的美元单价。
-- 汇率含义：UsdToCny 等为 1 USD 可兑外币数量；currency 1=RMB 2=USD 3=EUR 4=HKD。
-- 执行前请备份；建议在业务低峰执行。

UPDATE public.quoteitem AS i
SET converted_price = CASE i.currency
  WHEN 2 THEN round(i.unit_price, 6)
  WHEN 1 THEN CASE WHEN f."UsdToCny" > 0 THEN round(i.unit_price / f."UsdToCny", 6) ELSE 0 END
  WHEN 3 THEN CASE WHEN f."UsdToEur" > 0 THEN round(i.unit_price / f."UsdToEur", 6) ELSE 0 END
  WHEN 4 THEN CASE WHEN f."UsdToHkd" > 0 THEN round(i.unit_price / f."UsdToHkd", 6) ELSE 0 END
  ELSE COALESCE(i.converted_price, 0)
END
FROM public.financeexchangeratesetting AS f
WHERE f."FinanceExchangeRateSettingId" = '00000000-0000-4000-8000-0000000000E1'
  AND i.is_deleted = false
  AND i.unit_price IS NOT NULL;
