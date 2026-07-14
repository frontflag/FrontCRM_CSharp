using System.Text.Json;
using System.Text.Json.Nodes;
using CRM.Core.Constants;

namespace CRM.Infrastructure.Ai.EntityParse;

/// <summary>与 CRM.Web/src/utils/entityParseSchema.ts 对齐的后端 normalize。</summary>
public static class EntityParseNormalizer
{
    private const string CountryChina = "中国";
    private const int CountryDomesticCode = 1;
    private const int CountryOverseasCode = 2;

    private static readonly HashSet<string> ValidCustomerLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "D", "C", "B", "BPO", "VIP", "VPO"
    };

    private static readonly Dictionary<string, string> ChinaCascaderCountryAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["香港"] = "香港",
        ["Hong Kong"] = "香港",
        ["HK"] = "香港",
        ["台湾"] = "台湾",
        ["Taiwan"] = "台湾",
        ["TW"] = "台湾",
        ["澳门"] = "澳门",
        ["Macau"] = "澳门",
        ["MO"] = "澳门"
    };

    public static bool IsEntityParseScenario(string scenarioCode)
    {
        if (!scenarioCode.StartsWith(AiEntityParseScenarioCodes.Prefix, StringComparison.OrdinalIgnoreCase))
            return false;
        return !string.Equals(scenarioCode, AiEntityParseScenarioCodes.RfqExcelColumnMap, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(scenarioCode, AiEntityParseScenarioCodes.RfqExcelBrandMap, StringComparison.OrdinalIgnoreCase);
    }

    public static string? EntityTypeFromScenario(string scenarioCode) => scenarioCode.ToLowerInvariant() switch
    {
        AiEntityParseScenarioCodes.Customer => "CUSTOMER",
        AiEntityParseScenarioCodes.Rfq => "RFQ",
        AiEntityParseScenarioCodes.Vendor => "VENDOR",
        AiEntityParseScenarioCodes.CustomerContact => "CUSTOMER_CONTACT",
        AiEntityParseScenarioCodes.VendorContact => "VENDOR_CONTACT",
        AiEntityParseScenarioCodes.CustomerAddress => "CUSTOMER_ADDRESS",
        AiEntityParseScenarioCodes.VendorAddress => "VENDOR_ADDRESS",
        AiEntityParseScenarioCodes.CustomerBusinessCard => "CUSTOMER_BUSINESS_CARD",
        AiEntityParseScenarioCodes.VendorBusinessCard => "VENDOR_BUSINESS_CARD",
        _ => null
    };

    public static string? ParentBizTypeFromEntityType(string entityType) => entityType switch
    {
        "CUSTOMER_CONTACT" or "CUSTOMER_ADDRESS" => "CUSTOMER",
        "VENDOR_CONTACT" or "VENDOR_ADDRESS" => "VENDOR",
        _ => null
    };

    public static JsonObject? Normalize(string scenarioCode, JsonElement raw)
    {
        if (raw.ValueKind != JsonValueKind.Object)
            return null;

        return scenarioCode.ToLowerInvariant() switch
        {
            AiEntityParseScenarioCodes.Customer => NormalizeCustomer(raw),
            AiEntityParseScenarioCodes.Rfq => NormalizeRfq(raw),
            AiEntityParseScenarioCodes.Vendor => NormalizeVendor(raw),
            AiEntityParseScenarioCodes.CustomerContact => NormalizeCustomerContact(raw),
            AiEntityParseScenarioCodes.VendorContact => NormalizeVendorContact(raw),
            AiEntityParseScenarioCodes.CustomerAddress => NormalizeCustomerAddress(raw),
            AiEntityParseScenarioCodes.VendorAddress => NormalizeVendorAddress(raw),
            AiEntityParseScenarioCodes.CustomerBusinessCard => NormalizeCustomerBusinessCard(raw),
            AiEntityParseScenarioCodes.VendorBusinessCard => NormalizeVendorBusinessCard(raw),
            _ => null
        };
    }

    private static JsonObject NormalizeCustomer(JsonElement raw)
    {
        var level = NormalizeCustomerLevel(GetStr(raw, "customer_level"));
        var enriched = EntityParseRegionHelper.EnrichCustomerRegionFields(new EntityParseRegionHelper.RegionFields(
            GetStr(raw, "province"),
            GetStr(raw, "city"),
            GetStr(raw, "district"),
            GetStr(raw, "country"),
            GetStr(raw, "address")));

        return new JsonObject
        {
            ["customerName"] = GetStr(raw, "customer_name"),
            ["customerShortName"] = InferShortName(GetStr(raw, "customer_name"), GetStr(raw, "customer_short_name")),
            ["englishOfficialName"] = GetStr(raw, "english_official_name"),
            ["customerType"] = NumOrNull(raw, "customer_type"),
            ["customerLevel"] = ValidCustomerLevels.Contains(level) ? level : string.Empty,
            ["industry"] = GetStr(raw, "industry"),
            ["country"] = enriched.Country,
            ["province"] = enriched.Province,
            ["city"] = enriched.City,
            ["district"] = enriched.District,
            ["address"] = enriched.Address.Length > 0 ? enriched.Address : GetStr(raw, "address"),
            ["unifiedSocialCreditCode"] = GetStr(raw, "unified_social_credit_code"),
            ["creditLimit"] = NumOrNull(raw, "credit_limit"),
            ["paymentTerms"] = NumOrNull(raw, "payment_terms"),
            ["currency"] = NumOrNull(raw, "currency"),
            ["taxRate"] = NumOrNull(raw, "tax_rate"),
            ["invoiceType"] = NumOrNull(raw, "invoice_type"),
            ["companyInfo"] = GetStr(raw, "company_info"),
            ["remarks"] = GetStr(raw, "remarks")
        };
    }

    private static JsonObject NormalizeRfq(JsonElement raw)
    {
        var topRemark = GetStr(raw, "remark", "remarks", "notes");
        var items = new JsonArray();

        if (raw.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var itemEl in itemsEl.EnumerateArray())
            {
                if (itemEl.ValueKind != JsonValueKind.Object) continue;
                var item = NormalizeRfqItem(itemEl);
                if (HasItemContent(item)) items.Add(item);
            }
        }
        else if (raw.TryGetProperty("item", out var legacyItem) && legacyItem.ValueKind == JsonValueKind.Object)
        {
            var item = NormalizeRfqItem(legacyItem);
            if (string.IsNullOrEmpty(GetStr(item, "remark")) && topRemark.Length > 0)
                item["remark"] = topRemark;
            if (HasItemContent(item)) items.Add(item);
        }

        if (items.Count == 0)
        {
            var fallback = EmptyRfqItem();
            if (topRemark.Length > 0) fallback["remark"] = topRemark;
            items.Add(fallback);
        }
        else if (items.Count == 1)
        {
            var first = items[0]!.AsObject();
            if (string.IsNullOrEmpty(GetStr(first, "remark")) && topRemark.Length > 0)
                first["remark"] = topRemark;
        }

        return new JsonObject
        {
            ["customerName"] = GetStr(raw, "customer_name"),
            ["customerId"] = string.Empty,
            ["contactEmail"] = GetStr(raw, "contact_email"),
            ["industry"] = GetStr(raw, "industry"),
            ["product"] = GetStr(raw, "product"),
            ["rfqType"] = NumOrNull(raw, "rfq_type"),
            ["targetType"] = NumOrNull(raw, "target_type"),
            ["quoteMethod"] = NumOrNull(raw, "quote_method"),
            ["assignMethod"] = NumOrNull(raw, "assign_method"),
            ["importance"] = NumOrNull(raw, "importance"),
            ["projectBackground"] = GetStr(raw, "project_background"),
            ["competitor"] = GetStr(raw, "competitor"),
            ["remark"] = topRemark,
            ["items"] = items
        };
    }

    private static JsonObject NormalizeRfqItem(JsonElement itemRaw)
    {
        return new JsonObject
        {
            ["customerMpn"] = GetStr(itemRaw, "customer_mpn"),
            ["customerBrand"] = GetStr(itemRaw, "customer_brand"),
            ["mpn"] = GetStr(itemRaw, "mpn"),
            ["brand"] = GetStr(itemRaw, "brand"),
            ["targetPrice"] = NumOrNull(itemRaw, "target_price"),
            ["priceCurrency"] = MapPriceCurrency(GetAny(itemRaw, "price_currency", "target_price_currency", "currency")) ?? 1,
            ["quantity"] = NumOrNull(itemRaw, "quantity"),
            ["productionDate"] = GetStr(itemRaw, "production_date"),
            ["expiryDate"] = GetStr(itemRaw, "expiry_date"),
            ["minPackageQty"] = NumOrNull(itemRaw, "min_package_qty"),
            ["minOrderQty"] = NumOrNull(itemRaw, "moq", "min_order_qty"),
            ["alternativeMaterials"] = GetStr(itemRaw, "alternatives"),
            ["remark"] = GetStr(itemRaw, "remark", "remarks", "notes")
        };
    }

    private static JsonObject EmptyRfqItem() => new()
    {
        ["customerMpn"] = string.Empty,
        ["customerBrand"] = string.Empty,
        ["mpn"] = string.Empty,
        ["brand"] = string.Empty,
        ["targetPrice"] = null,
        ["priceCurrency"] = 1,
        ["quantity"] = 1,
        ["productionDate"] = string.Empty,
        ["expiryDate"] = string.Empty,
        ["minPackageQty"] = null,
        ["minOrderQty"] = null,
        ["alternativeMaterials"] = string.Empty,
        ["remark"] = string.Empty
    };

    private static bool HasItemContent(JsonObject item)
    {
        var hasQty = false;
        if (item["quantity"] is JsonValue qtyVal && qtyVal.TryGetValue(out int q) && q > 0)
            hasQty = true;

        return !string.IsNullOrEmpty(GetStr(item, "customerMpn"))
               || !string.IsNullOrEmpty(GetStr(item, "mpn"))
               || !string.IsNullOrEmpty(GetStr(item, "customerBrand"))
               || !string.IsNullOrEmpty(GetStr(item, "brand"))
               || !string.IsNullOrEmpty(GetStr(item, "remark"))
               || (item["targetPrice"] != null && item["targetPrice"]!.GetValueKind() != JsonValueKind.Null)
               || hasQty;
    }

    private static JsonObject NormalizeCustomerBusinessCard(JsonElement raw)
    {
        var result = new JsonObject();
        if (raw.TryGetProperty("customer", out var customerEl) && customerEl.ValueKind == JsonValueKind.Object)
        {
            result["customer"] = NormalizeCustomer(customerEl);
            PromoteCustomerCardCompanyInfo(result["customer"] as JsonObject);
        }
        else
            result["customer"] = new JsonObject();

        if (raw.TryGetProperty("contact", out var contactEl) && contactEl.ValueKind == JsonValueKind.Object)
        {
            result["contact"] = NormalizeCustomerContact(contactEl);
            ApplyDefaultBusinessCardContactGender(result["contact"] as JsonObject);
        }
        else
            result["contact"] = new JsonObject();

        if (raw.TryGetProperty("address", out var addressEl) && addressEl.ValueKind == JsonValueKind.Object)
        {
            var address = NormalizeCustomerAddress(addressEl);
            var street = address["streetAddress"] is JsonValue sv ? sv.GetValue<string>()?.Trim() ?? string.Empty : string.Empty;
            if (!string.IsNullOrWhiteSpace(street))
                result["address"] = address;
        }

        return result;
    }

    /// <summary>名片场景：AI 常将公司简介写入 remarks，统一提升到 companyInfo。</summary>
    private static void PromoteCustomerCardCompanyInfo(JsonObject? customer)
    {
        if (customer == null) return;
        var companyInfo = GetStr(customer, "companyInfo");
        var remarks = GetStr(customer, "remarks");
        if (string.IsNullOrWhiteSpace(companyInfo) && remarks.Length > 0)
        {
            customer["companyInfo"] = remarks;
            customer["remarks"] = string.Empty;
        }
    }

    private static JsonObject NormalizeVendorBusinessCard(JsonElement raw)
    {
        var result = new JsonObject();
        if (raw.TryGetProperty("vendor", out var vendorEl) && vendorEl.ValueKind == JsonValueKind.Object)
            result["vendor"] = NormalizeVendor(vendorEl);
        else
            result["vendor"] = new JsonObject();

        if (raw.TryGetProperty("contact", out var contactEl) && contactEl.ValueKind == JsonValueKind.Object)
        {
            result["contact"] = NormalizeVendorContact(contactEl);
            ApplyDefaultBusinessCardContactGender(result["contact"] as JsonObject);
        }
        else
            result["contact"] = new JsonObject();

        if (raw.TryGetProperty("address", out var addressEl) && addressEl.ValueKind == JsonValueKind.Object)
        {
            var address = NormalizeVendorAddress(addressEl);
            var street = address["address"] is JsonValue sv ? sv.GetValue<string>()?.Trim() ?? string.Empty : string.Empty;
            if (!string.IsNullOrWhiteSpace(street))
                result["address"] = address;
        }

        return result;
    }

    private static JsonObject NormalizeVendor(JsonElement raw) => new()
    {
        ["officialName"] = GetStr(raw, "official_name", "vendor_name", "name"),
        ["englishOfficialName"] = GetStr(raw, "english_official_name"),
        ["nickName"] = InferShortName(GetStr(raw, "official_name", "vendor_name", "name"), GetStr(raw, "nick_name", "short_name", "vendor_short_name")),
        ["industry"] = GetStr(raw, "industry"),
        ["level"] = NormalizeVendorLevel(GetAny(raw, "level", "vendor_level")),
        ["credit"] = NormalizeVendorCredit(GetAny(raw, "credit", "identity", "vendor_credit")),
        ["officeAddress"] = GetStr(raw, "office_address", "address"),
        ["website"] = GetStr(raw, "website"),
        ["currency"] = MapPriceCurrency(GetAny(raw, "trade_currency", "currency")) ?? 1,
        ["paymentMethod"] = GetStr(raw, "payment_method"),
        ["paymentDays"] = NumOrNull(raw, "payment_days", "payment_terms"),
        ["taxNumber"] = GetStr(raw, "credit_code", "tax_number", "unified_social_credit_code"),
        ["companyInfo"] = GetStr(raw, "company_info", "remarks"),
        ["remark"] = GetStr(raw, "remark")
    };

    private static JsonObject NormalizeCustomerContact(JsonElement raw)
    {
        var cName = GetStr(raw, "c_name", "contact_name", "name", "cName", "contactName");
        var eName = GetStr(raw, "e_name", "english_name", "eName");
        return new JsonObject
        {
            ["cName"] = cName,
            ["eName"] = eName,
            ["gender"] = NormalizeContactGender(GetAny(raw, "gender", "sex")),
            ["department"] = GetStr(raw, "department"),
            ["position"] = GetStr(raw, "position", "title", "job_title"),
            ["mobilePhone"] = GetStr(raw, "mobile_phone", "mobile", "cellphone"),
            ["phone"] = GetStr(raw, "phone", "landline", "tel"),
            ["email"] = GetStr(raw, "email", "mail"),
            ["fax"] = GetStr(raw, "fax"),
            ["socialAccount"] = GetStr(raw, "social_account", "qq", "wechat", "weixin"),
            ["isDefault"] = BoolOrFalse(GetAny(raw, "is_default", "default")),
            ["isDecisionMaker"] = BoolOrFalse(GetAny(raw, "is_decision_maker", "decision_maker")),
            ["remarks"] = GetStr(raw, "remark", "remarks", "notes")
        };
    }

    private static JsonObject NormalizeVendorContact(JsonElement raw)
    {
        var cName = GetStr(raw, "c_name", "contact_name", "name", "cName");
        var eName = GetStr(raw, "e_name", "english_name", "eName");
        return new JsonObject
        {
            ["cName"] = cName,
            ["eName"] = eName,
            ["gender"] = NormalizeContactGender(GetAny(raw, "gender", "sex")),
            ["title"] = GetStr(raw, "title", "position", "job_title"),
            ["department"] = GetStr(raw, "department"),
            ["mobile"] = GetStr(raw, "mobile", "mobile_phone", "cellphone"),
            ["tel"] = GetStr(raw, "tel", "phone", "landline"),
            ["email"] = GetStr(raw, "email", "mail"),
            ["isMain"] = BoolOrFalse(GetAny(raw, "is_main", "is_default", "default")),
            ["remark"] = GetStr(raw, "remark", "remarks", "notes")
        };
    }

    private static JsonObject NormalizeCustomerAddress(JsonElement raw)
    {
        var country = GetStr(raw, "country", "country_name");
        var province = GetStr(raw, "province", "state");
        var city = GetStr(raw, "city");
        var district = GetStr(raw, "district", "area");
        var streetAddress = GetStr(raw, "street_address", "address", "detail_address");

        if (UsesChinaRegionCascader(country, province))
        {
            var normalized = NormalizeAddressChinaCascaderCountry(country, province);
            country = normalized.Country;
            province = normalized.Province;
            var enriched = EntityParseRegionHelper.EnrichCustomerRegionFields(new EntityParseRegionHelper.RegionFields(
                province, city, district, country, streetAddress));

            return new JsonObject
            {
                ["addressType"] = NormalizeAddressType(GetAny(raw, "address_type", "type")),
                ["country"] = enriched.Country.Length > 0 ? enriched.Country : CountryChina,
                ["countryCode"] = CountryDomesticCode,
                ["isDomestic"] = true,
                ["province"] = enriched.Province,
                ["city"] = enriched.City,
                ["district"] = enriched.District,
                ["streetAddress"] = streetAddress.Length > 0 ? streetAddress : enriched.Address,
                ["companyName"] = GetStr(raw, "company_name"),
                ["zipCode"] = GetStr(raw, "zip_code", "postal_code"),
                ["contactPerson"] = GetStr(raw, "contact_person", "contact_name"),
                ["contactPhone"] = GetStr(raw, "contact_phone", "phone"),
                ["isDefault"] = BoolOrFalse(GetAny(raw, "is_default", "default"))
            };
        }

        return new JsonObject
        {
            ["addressType"] = NormalizeAddressType(GetAny(raw, "address_type", "type")),
            ["country"] = country,
            ["countryCode"] = CountryOverseasCode,
            ["isDomestic"] = false,
            ["province"] = province,
            ["city"] = city,
            ["district"] = string.Empty,
            ["streetAddress"] = streetAddress,
            ["companyName"] = GetStr(raw, "company_name"),
            ["zipCode"] = GetStr(raw, "zip_code", "postal_code"),
            ["contactPerson"] = GetStr(raw, "contact_person", "contact_name"),
            ["contactPhone"] = GetStr(raw, "contact_phone", "phone"),
            ["isDefault"] = BoolOrFalse(GetAny(raw, "is_default", "default"))
        };
    }

    private static JsonObject NormalizeVendorAddress(JsonElement raw)
    {
        var countryName = GetStr(raw, "country", "country_name");
        var province = GetStr(raw, "province", "state");
        var city = GetStr(raw, "city");
        var area = GetStr(raw, "area", "district");
        var address = GetStr(raw, "address", "street_address", "detail_address");

        if (UsesChinaRegionCascader(countryName, province))
        {
            var normalized = NormalizeAddressChinaCascaderCountry(countryName, province);
            countryName = normalized.Country;
            province = normalized.Province;
            var enriched = EntityParseRegionHelper.EnrichCustomerRegionFields(new EntityParseRegionHelper.RegionFields(
                province, city, area, countryName, address));

            return new JsonObject
            {
                ["addressType"] = NormalizeVendorAddressType(GetAny(raw, "address_type", "type")),
                ["countryName"] = enriched.Country.Length > 0 ? enriched.Country : CountryChina,
                ["country"] = CountryDomesticCode,
                ["province"] = enriched.Province,
                ["city"] = enriched.City,
                ["area"] = enriched.District,
                ["address"] = address.Length > 0 ? address : enriched.Address,
                ["contactName"] = GetStr(raw, "contact_name", "contact_person"),
                ["contactPhone"] = GetStr(raw, "contact_phone", "phone"),
                ["isDefault"] = BoolOrFalse(GetAny(raw, "is_default", "default")),
                ["remark"] = GetStr(raw, "remark", "remarks")
            };
        }

        return new JsonObject
        {
            ["addressType"] = NormalizeVendorAddressType(GetAny(raw, "address_type", "type")),
            ["countryName"] = countryName,
            ["country"] = CountryOverseasCode,
            ["province"] = province.Length > 0 ? province : countryName,
            ["city"] = city,
            ["area"] = string.Empty,
            ["address"] = address,
            ["contactName"] = GetStr(raw, "contact_name", "contact_person"),
            ["contactPhone"] = GetStr(raw, "contact_phone", "phone"),
            ["isDefault"] = BoolOrFalse(GetAny(raw, "is_default", "default")),
            ["remark"] = GetStr(raw, "remark", "remarks")
        };
    }

    private static bool UsesChinaRegionCascader(string countryName, string province)
    {
        var c = countryName.Trim();
        if (string.IsNullOrEmpty(c) || c == CountryChina) return true;
        if (ChinaCascaderCountryAliases.ContainsKey(c) || c is "香港" or "澳门" or "台湾") return true;
        var p = province.Trim();
        return p is "香港" or "台湾" or "澳门";
    }

    private static (string Country, string Province) NormalizeAddressChinaCascaderCountry(string country, string province)
    {
        var c = country.Trim();
        var p = province.Trim();
        if (string.IsNullOrEmpty(c) || c == CountryChina)
            return (CountryChina, p);
        if (ChinaCascaderCountryAliases.TryGetValue(c, out var mapped))
            return (CountryChina, p.Length > 0 ? p : mapped);
        if (c is "香港" or "澳门" or "台湾")
            return (CountryChina, p.Length > 0 ? p : c);
        return (c, p);
    }

    private static string NormalizeAddressType(JsonElement? v)
    {
        var raw = StrFromElement(v);
        if (string.IsNullOrEmpty(raw)) return "Office";
        var lower = raw.ToLowerInvariant();
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["office"] = "Office",
            ["billing"] = "Billing",
            ["shipping"] = "Shipping",
            ["registered"] = "Registered"
        };
        if (map.TryGetValue(lower, out var mapped)) return mapped;
        if (raw is "Office" or "Billing" or "Shipping" or "Registered") return raw;
        if (raw.Contains("办公", StringComparison.Ordinal)) return "Office";
        if (raw.Contains("账单", StringComparison.Ordinal)) return "Billing";
        if (raw.Contains("收货", StringComparison.Ordinal) || raw.Contains("送货", StringComparison.Ordinal)) return "Shipping";
        if (raw.Contains("注册", StringComparison.Ordinal)) return "Registered";
        return "Office";
    }

    private static JsonNode NormalizeVendorAddressType(JsonElement? v)
    {
        var raw = StrFromElement(v).Trim();
        if (string.IsNullOrEmpty(raw)) return 1;
        if (int.TryParse(raw, out var n) && (n == 1 || n == 2)) return n;
        var lower = raw.ToLowerInvariant();
        if (lower == "billing" || raw.Contains("账单", StringComparison.Ordinal)) return 2;
        if (lower == "shipping" || raw.Contains("收货", StringComparison.Ordinal) || raw.Contains("送货", StringComparison.Ordinal)) return 1;
        return 1;
    }

    private static string NormalizeCustomerLevel(string v)
    {
        var s = v.Trim().ToUpperInvariant();
        return ValidCustomerLevels.Contains(s) ? s : string.Empty;
    }

    private static JsonNode? NormalizeVendorLevel(JsonElement? v)
    {
        var n = NumFromElement(v);
        if (n == null) return null;
        var r = (int)Math.Round(n.Value);
        return r is >= 1 and <= 13 ? r : null;
    }

    private static JsonNode? NormalizeVendorCredit(JsonElement? v)
    {
        var n = NumFromElement(v);
        if (n == null) return null;
        var r = (int)Math.Round(n.Value);
        return r is >= 1 and <= 10 ? r : null;
    }

    private static int NormalizeContactGender(JsonElement? v)
    {
        var n = NumFromElement(v);
        if (n == null) return 0;
        var r = (int)Math.Round(n.Value);
        return r is 1 or 2 ? r : 0;
    }

    private static int? MapPriceCurrency(JsonElement? v)
    {
        if (v == null || v.Value.ValueKind == JsonValueKind.Null) return null;
        if (v.Value.ValueKind == JsonValueKind.Number && v.Value.TryGetDouble(out var d) && double.IsFinite(d))
        {
            var n = (int)Math.Round(d);
            if (n is >= 1 and <= 4) return n;
            return null;
        }

        var s = StrFromElement(v).ToUpperInvariant();
        if (string.IsNullOrEmpty(s)) return null;
        if (s == "1" || s.Contains("RMB", StringComparison.Ordinal) || s.Contains("CNY", StringComparison.Ordinal)
            || s.Contains("人民币", StringComparison.Ordinal) || s is "￥" or "¥") return 1;
        if (s == "2" || s.Contains("USD", StringComparison.Ordinal) || s.Contains("美元", StringComparison.Ordinal) || s == "$") return 2;
        if (s == "3" || s.Contains("EUR", StringComparison.Ordinal) || s.Contains("欧元", StringComparison.Ordinal)) return 3;
        if (s == "4" || s.Contains("HKD", StringComparison.Ordinal) || s.Contains("港币", StringComparison.Ordinal)
            || s.Contains("港元", StringComparison.Ordinal)) return 4;
        if (double.TryParse(s, out var parsed) && double.IsFinite(parsed))
        {
            var r = (int)Math.Round(parsed);
            if (r is >= 1 and <= 4) return r;
        }

        return null;
    }

    private static bool BoolOrFalse(JsonElement? v)
    {
        if (v == null) return false;
        var el = v.Value;
        if (el.ValueKind == JsonValueKind.True) return true;
        if (el.ValueKind == JsonValueKind.False) return false;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n) && n == 1) return true;
        if (el.ValueKind == JsonValueKind.String)
        {
            var u = el.GetString()?.Trim().ToLowerInvariant() ?? string.Empty;
            return u is "true" or "yes" or "1" or "是";
        }

        return false;
    }

    private static JsonElement? GetAny(JsonElement raw, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (raw.TryGetProperty(key, out var val)) return val;
            foreach (var prop in raw.EnumerateObject())
            {
                if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase))
                    return prop.Value;
            }
        }

        return null;
    }

    private static string GetStr(JsonElement raw, params string[] keys)
    {
        var el = GetAny(raw, keys);
        return StrFromElement(el);
    }

    private static string GetStr(JsonObject obj, string key)
    {
        if (!obj.TryGetPropertyValue(key, out var node) || node == null) return string.Empty;
        return node.GetValueKind() switch
        {
            JsonValueKind.String => node.GetValue<string>()?.Trim() ?? string.Empty,
            JsonValueKind.Number => node.ToJsonString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => node.ToJsonString().Trim('"')
        };
    }

    private static string StrFromElement(JsonElement? el)
    {
        if (el == null || el.Value.ValueKind == JsonValueKind.Null) return string.Empty;
        return el.Value.ValueKind switch
        {
            JsonValueKind.String => el.Value.GetString()?.Trim() ?? string.Empty,
            JsonValueKind.Number => el.Value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => el.Value.GetRawText()
        };
    }

    private static JsonNode? NumOrNull(JsonElement raw, params string[] keys)
    {
        var n = NumFromElement(GetAny(raw, keys));
        return n.HasValue ? n.Value : null;
    }

    private static double? NumFromElement(JsonElement? el)
    {
        if (el == null || el.Value.ValueKind == JsonValueKind.Null) return null;
        if (el.Value.ValueKind == JsonValueKind.Number && el.Value.TryGetDouble(out var d) && double.IsFinite(d))
            return d;
        var s = StrFromElement(el);
        if (string.IsNullOrEmpty(s)) return null;
        return double.TryParse(s, out var parsed) && double.IsFinite(parsed) ? parsed : null;
    }

    private static string InferShortName(string fullName, string existingShort)
    {
        if (!string.IsNullOrWhiteSpace(existingShort)) return existingShort.Trim();
        var name = (fullName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name)) return string.Empty;

        ReadOnlySpan<string> suffixes =
        [
            "股份有限公司", "有限责任公司", "有限公司", "集团公司", "集团", "公司"
        ];
        foreach (var suffix in suffixes)
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal) && name.Length > suffix.Length)
            {
                name = name[..^suffix.Length].Trim();
                break;
            }
        }

        name = System.Text.RegularExpressions.Regex.Replace(name, @",?\s*Inc\.?$", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+Ltd\.?$", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+LLC\.?$", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+Co\.?,?\s*Ltd\.?$", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        return name.Trim();
    }

    private static void ApplyDefaultBusinessCardContactGender(JsonObject? contact)
    {
        if (contact == null) return;
        var gender = contact["gender"] switch
        {
            JsonValue gv when gv.TryGetValue(out int g) => g,
            _ => 0
        };
        if (gender is not (1 or 2)) contact["gender"] = 1;
    }
}
