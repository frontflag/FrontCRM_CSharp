using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Ai;

public sealed class MockAiLlmProvider : IAiLlmProvider
{
    private readonly ILogger<MockAiLlmProvider> _logger;

    public MockAiLlmProvider(ILogger<MockAiLlmProvider> logger)
    {
        _logger = logger;
    }

    public string ProviderCode => AiProviderCodes.Mock;

    public Task<AiChatCompletionResult> ChatAsync(AiChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var systemMsg = request.Messages.FirstOrDefault(m => m.Role == "system")?.Content ?? string.Empty;
        var userMsg = request.Messages.LastOrDefault(m => m.Role == "user")?.Content ?? string.Empty;

        if (systemMsg.Contains("客户主档解析", StringComparison.Ordinal))
        {
            var json = """
                       {
                         "customer_name": "Mock 客户有限公司",
                         "customer_short_name": "Mock客户",
                         "english_official_name": null,
                         "customer_type": 2,
                         "customer_level": "B",
                         "industry": "电子制造",
                         "country": "中国",
                         "province": null,
                         "city": null,
                         "district": null,
                         "address": null,
                         "unified_social_credit_code": null,
                         "credit_limit": null,
                         "payment_terms": 30,
                         "currency": 1,
                         "tax_rate": 13,
                         "invoice_type": 2,
                         "remarks": "Mock 开发数据，请配置 moonshot 后使用真实解析。"
                       }
                       """;
            return Task.FromResult(new AiChatCompletionResult
            {
                Content = json,
                Usage = new AiTokenUsageDto { PromptTokens = 80, CompletionTokens = 160, TotalTokens = 240 }
            });
        }

        if (systemMsg.Contains("RFQ 需求解析", StringComparison.Ordinal))
        {
            var rawText = ExtractRawTextFromUserMessage(userMsg);
            var json = BuildMockRfqParseJson(rawText);
            return Task.FromResult(new AiChatCompletionResult
            {
                Content = json,
                Usage = new AiTokenUsageDto { PromptTokens = 90, CompletionTokens = 200, TotalTokens = 290 }
            });
        }

        if (systemMsg.Contains("供应商主档解析", StringComparison.Ordinal))
        {
            var json = """
                       {
                         "official_name": "Mock 供应商有限公司",
                         "english_official_name": null,
                         "nick_name": "Mock供应商",
                         "industry": "电子元器件",
                         "level": 3,
                         "credit": 2,
                         "office_address": "深圳市南山区 Mock 路 1 号",
                         "website": null,
                         "trade_currency": 1,
                         "payment_method": "电汇",
                         "payment_days": 30,
                         "credit_code": null,
                         "company_info": null,
                         "remark": "Mock 开发数据，请配置 moonshot 后使用真实解析。"
                       }
                       """;
            return Task.FromResult(new AiChatCompletionResult
            {
                Content = json,
                Usage = new AiTokenUsageDto { PromptTokens = 85, CompletionTokens = 170, TotalTokens = 255 }
            });
        }

        if (systemMsg.Contains("客户名片解析", StringComparison.Ordinal))
        {
            var json = """
                       {
                         "customer": {
                           "customer_name": "Mock 名片客户有限公司",
                           "customer_short_name": "Mock名片客户",
                           "english_official_name": "Mock Business Card Co., Ltd.",
                           "customer_type": 2,
                           "customer_level": "B",
                           "industry": "电子制造",
                           "country": "中国",
                           "province": "广东省",
                           "city": "深圳市",
                           "district": "福田区",
                           "address": "Mock 深南大道 1000 号",
                           "unified_social_credit_code": null,
                           "credit_limit": null,
                           "payment_terms": null,
                           "currency": 1,
                           "tax_rate": null,
                           "invoice_type": null,
                           "company_info": "以芯为源，赋能万物；专注半导体、新能源、智能芯片与 IoT 生态相关业务。",
                           "remarks": null
                         },
                         "contact": {
                           "c_name": "Mock 张三",
                           "e_name": "Mock Zhang San",
                           "gender": 1,
                           "department": "采购部",
                           "position": "采购经理",
                           "mobile_phone": "13800138000",
                           "phone": "0755-12345678",
                           "email": "mock.card@example.com",
                           "fax": null,
                           "social_account": null,
                           "is_default": true,
                           "is_decision_maker": false,
                           "remarks": null
                         },
                         "address": {
                           "address_type": "Office",
                           "country": "中国",
                           "province": "广东省",
                           "city": "深圳市",
                           "district": "福田区",
                           "street_address": "Mock 深南大道 1000 号",
                           "company_name": "Mock 名片客户有限公司",
                           "zip_code": "518000",
                           "contact_person": "Mock 张三",
                           "contact_phone": "13800138000",
                           "is_default": true
                         }
                       }
                       """;
            return Task.FromResult(new AiChatCompletionResult
            {
                Content = json,
                Usage = new AiTokenUsageDto { PromptTokens = 120, CompletionTokens = 280, TotalTokens = 400 }
            });
        }

        if (systemMsg.Contains("供应商名片解析", StringComparison.Ordinal))
        {
            var json = """
                       {
                         "vendor": {
                           "official_name": "Mock 名片供应商有限公司",
                           "english_official_name": "Mock Vendor Card Co., Ltd.",
                           "nick_name": "Mock名片供应商",
                           "industry": "半导体、新能源、智能芯片、IoT生态",
                           "level": 3,
                           "credit": 2,
                           "office_address": "Mock 科技园 Mock 路 88 号",
                           "website": null,
                           "trade_currency": 1,
                           "payment_method": null,
                           "payment_days": 30,
                           "credit_code": null,
                           "company_info": "以芯为源，赋能万物；主营半导体、新能源、智能芯片与 IoT 生态相关业务。",
                           "remark": null
                         },
                         "contact": {
                           "c_name": "Mock 李四",
                           "e_name": "Mock Li Si",
                           "gender": 1,
                           "title": "销售经理",
                           "department": "销售部",
                           "mobile": "13900139000",
                           "tel": "0755-87654321",
                           "email": "vendor.card@example.com",
                           "is_main": true,
                           "remark": null
                         },
                         "address": {
                           "address_type": 1,
                           "country": "中国",
                           "province": "广东省",
                           "city": "深圳市",
                           "area": "南山区",
                           "address": "Mock 科技园 Mock 路 88 号",
                           "contact_name": "Mock 李四",
                           "contact_phone": "13900139000",
                           "is_default": true,
                           "remark": null
                         }
                       }
                       """;
            return Task.FromResult(new AiChatCompletionResult
            {
                Content = json,
                Usage = new AiTokenUsageDto { PromptTokens = 115, CompletionTokens = 270, TotalTokens = 385 }
            });
        }

        if (systemMsg.Contains("客户联系人解析", StringComparison.Ordinal))
        {
            var json = """
                       {
                         "c_name": "Mock 张三",
                         "e_name": "Mock Zhang San",
                         "gender": 1,
                         "department": "采购部",
                         "position": "采购经理",
                         "mobile_phone": "13800138000",
                         "phone": "0755-12345678",
                         "email": "mock@example.com",
                         "fax": null,
                         "social_account": "mock_wechat",
                         "is_default": false,
                         "is_decision_maker": true,
                         "remarks": "Mock 开发数据，请配置 moonshot 后使用真实解析。"
                       }
                       """;
            return Task.FromResult(new AiChatCompletionResult
            {
                Content = json,
                Usage = new AiTokenUsageDto { PromptTokens = 75, CompletionTokens = 150, TotalTokens = 225 }
            });
        }

        if (systemMsg.Contains("客户地址解析", StringComparison.Ordinal))
        {
            var json = """
                       {
                         "address_type": "Shipping",
                         "country": "中国",
                         "province": "广东省",
                         "city": "深圳市",
                         "district": "福田区",
                         "street_address": "Mock 深南大道 1000 号",
                         "company_name": "Mock 收货公司",
                         "zip_code": "518000",
                         "contact_person": "Mock 王五",
                         "contact_phone": "13700137000",
                         "is_default": false
                       }
                       """;
            return Task.FromResult(new AiChatCompletionResult
            {
                Content = json,
                Usage = new AiTokenUsageDto { PromptTokens = 78, CompletionTokens = 155, TotalTokens = 233 }
            });
        }

        if (systemMsg.Contains("供应商地址解析", StringComparison.Ordinal))
        {
            var json = """
                       {
                         "address_type": 1,
                         "country": "中国",
                         "province": "广东省",
                         "city": "深圳市",
                         "area": "南山区",
                         "address": "Mock 科技园 Mock 路 88 号",
                         "contact_name": "Mock 赵六",
                         "contact_phone": "13600136000",
                         "is_default": false,
                         "remark": "Mock 开发数据，请配置 moonshot 后使用真实解析。"
                       }
                       """;
            return Task.FromResult(new AiChatCompletionResult
            {
                Content = json,
                Usage = new AiTokenUsageDto { PromptTokens = 76, CompletionTokens = 150, TotalTokens = 226 }
            });
        }

        if (systemMsg.Contains("供应商联系人解析", StringComparison.Ordinal))
        {
            var json = """
                       {
                         "c_name": "Mock 李四",
                         "e_name": "Mock Li",
                         "title": "销售经理",
                         "department": "销售部",
                         "mobile": "13900139000",
                         "tel": "0755-87654321",
                         "email": "vendor.mock@example.com",
                         "is_main": false,
                         "remark": "Mock 开发数据，请配置 moonshot 后使用真实解析。"
                       }
                       """;
            return Task.FromResult(new AiChatCompletionResult
            {
                Content = json,
                Usage = new AiTokenUsageDto { PromptTokens = 72, CompletionTokens = 140, TotalTokens = 212 }
            });
        }

        if (systemMsg.Contains("客户情报", StringComparison.Ordinal)
            || systemMsg.Contains("供应商情报", StringComparison.Ordinal))
        {
            var company = ExtractBetween(userMsg, "企业名称", "；")
                          ?? ExtractBetween(userMsg, "企业名称", ";")
                          ?? ExtractBetween(userMsg, "company_name", "；")
                          ?? (systemMsg.Contains("供应商情报", StringComparison.Ordinal)
                              ? "Mock 供应商有限公司"
                              : "Mock 客户有限公司");
            company = company.Trim().TrimStart('：', ':').Trim();
            var json = BuildMockCustomerIntelJson(company);
            return Task.FromResult(new AiChatCompletionResult
            {
                Content = json,
                Usage = new AiTokenUsageDto { PromptTokens = 200, CompletionTokens = 1200, TotalTokens = 1400 }
            });
        }

        var pn = ExtractBetween(userMsg, "PN=", "，") ?? ExtractBetween(userMsg, "PN=", ",") ?? "UNKNOWN";
        var brand = ExtractBetween(userMsg, "品牌=", "。") ?? ExtractBetween(userMsg, "品牌=", ".") ?? "UNKNOWN";

        var legacyJson = $$"""
                     {
                       "package": "Mock-SOIC-8",
                       "voltage": "2.7V-5.5V",
                       "temperature_range": "-40°C to +125°C",
                       "description": "Mock response for PN={{pn}} brand={{brand}}. Replace provider with moonshot for live lookup.",
                       "confidence": "low",
                       "disclaimer": "This is mock data for development only."
                     }
                     """;

        return Task.FromResult(new AiChatCompletionResult
        {
            Content = legacyJson,
            Usage = new AiTokenUsageDto { PromptTokens = 50, CompletionTokens = 120, TotalTokens = 170 }
        });
    }

    private static string? ExtractBetween(string text, string start, string end)
    {
        var i = text.IndexOf(start, StringComparison.OrdinalIgnoreCase);
        if (i < 0)
            return null;
        i += start.Length;
        var j = text.IndexOf(end, i, StringComparison.Ordinal);
        if (j < 0)
            return text[i..].Trim();
        return text[i..j].Trim();
    }

    private static string ExtractRawTextFromUserMessage(string userMsg)
    {
        const string marker = "原文：";
        var idx = userMsg.LastIndexOf(marker, StringComparison.Ordinal);
        if (idx >= 0)
            return userMsg[(idx + marker.Length)..].Trim();
        return userMsg.Trim();
    }

    private static string BuildMockRfqParseJson(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return BuildDefaultMockRfqJson();

        var lines = rawText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (lines.Count == 0)
            return BuildDefaultMockRfqJson();

        string? customerName = null;
        string? headerRemark = null;
        var items = new List<Dictionary<string, object?>>();

        foreach (var line in lines)
        {
            if (customerName == null && LooksLikeCompanyName(line))
            {
                customerName = line;
                continue;
            }

            if (TryParseCombinedItemLine(line, out var combinedItem))
            {
                items.Add(combinedItem);
                continue;
            }

            if (LooksLikeMpn(line))
            {
                items.Add(CreateMockItemDict(mpn: line));
                continue;
            }

            if (TryParseQuantityPriceLine(line, out var qty, out var price, out var currency))
            {
                if (items.Count > 0)
                {
                    var last = items[^1];
                    last["quantity"] = qty;
                    last["target_price"] = price;
                    last["price_currency"] = currency;
                }
                continue;
            }

            if (items.Count > 0)
                AppendItemRemark(items[^1], line);
            else
                headerRemark = AppendRemarkText(headerRemark, line);
        }

        if (items.Count == 0)
            return BuildDefaultMockRfqJson();

        var payload = new Dictionary<string, object?>
        {
            ["customer_name"] = customerName ?? "Mock 客户有限公司",
            ["contact_email"] = null,
            ["industry"] = null,
            ["product"] = null,
            ["rfq_type"] = 1,
            ["target_type"] = 1,
            ["quote_method"] = 2,
            ["assign_method"] = 2,
            ["importance"] = 2,
            ["project_background"] = null,
            ["competitor"] = null,
            ["remark"] = headerRemark,
            ["items"] = items
        };

        return JsonSerializer.Serialize(payload);
    }

    private static Dictionary<string, object?> CreateMockItemDict(
        string? mpn = null,
        int quantity = 1,
        decimal? targetPrice = null,
        int priceCurrency = 1,
        string? remark = null)
    {
        return new Dictionary<string, object?>
        {
            ["customer_mpn"] = mpn,
            ["customer_brand"] = null,
            ["mpn"] = mpn,
            ["brand"] = null,
            ["target_price"] = targetPrice,
            ["price_currency"] = priceCurrency,
            ["quantity"] = quantity,
            ["production_date"] = null,
            ["expiry_date"] = null,
            ["min_package_qty"] = null,
            ["moq"] = null,
            ["alternatives"] = null,
            ["remark"] = remark
        };
    }

    private static bool TryParseCombinedItemLine(string line, out Dictionary<string, object?> item)
    {
        item = CreateMockItemDict();
        var parts = line.Split(['，', ',', '；', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return false;

        var mpnPart = parts[0];
        if (!LooksLikeMpn(mpnPart))
            return false;

        item["customer_mpn"] = mpnPart;
        item["mpn"] = mpnPart;

        var tail = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty;
        if (!string.IsNullOrWhiteSpace(tail) &&
            TryParseQuantityPriceLine(tail, out var qty, out var price, out var currency))
        {
            item["quantity"] = qty;
            item["target_price"] = price;
            item["price_currency"] = currency;
        }

        return true;
    }

    private static void AppendItemRemark(Dictionary<string, object?> item, string line)
    {
        item["remark"] = AppendRemarkText(item["remark"] as string, line);
    }

    private static string AppendRemarkText(string? existing, string line)
    {
        if (string.IsNullOrWhiteSpace(existing))
            return line.Trim();
        return $"{existing.Trim()}；{line.Trim()}";
    }

    private static string BuildMockCustomerIntelJson(string companyName)
    {
        var safeName = string.IsNullOrWhiteSpace(companyName) ? "Mock 客户有限公司" : companyName.Trim();
        return $$"""
                   {
                     "meta": {
                       "schema_version": "1.1",
                       "company_name_primary": "{{safeName}}",
                       "company_name_aliases": [],
                       "credit_code": null,
                       "region": "广东省深圳市",
                       "generated_at": "2026-07-14T00:00:00Z",
                       "data_freshness": "mixed",
                       "overall_confidence": "low"
                     },
                     "query": {
                       "company_name": "{{safeName}}",
                       "credit_code": null,
                       "region": null,
                       "intent": "full"
                     },
                     "sections": [
                       {
                         "id": "registry",
                         "title": "基础档案",
                         "summary": "Mock 开发数据，请配置 moonshot 后使用真实调查。",
                         "confidence": "low",
                         "content": {
                           "official_name": "{{safeName}}",
                           "operating_status": "存续（Mock）"
                         },
                         "sources": []
                       },
                       {
                         "id": "ownership",
                         "title": "股权结构",
                         "summary": "Mock 股权结构",
                         "confidence": "low",
                         "content": {
                           "shareholders": [],
                           "parent_company": null,
                           "ultimate_controller": null,
                           "listed_info": { "is_listed": false, "stock_code": null, "exchange": null },
                           "ownership_notes": "Mock 数据"
                         },
                         "sources": []
                       },
                       {
                         "id": "business",
                         "title": "经营业务",
                         "summary": "Mock 主营业务示例",
                         "confidence": "low",
                         "content": {
                           "main_products": ["示例产品 A", "示例产品 B"],
                           "business_model": "ODM/OEM",
                           "industry_tags": ["电子制造"]
                         },
                         "sources": []
                       },
                       {
                         "id": "scale",
                         "title": "企业规模",
                         "summary": "Mock 规模数据",
                         "confidence": "low",
                         "content": { "employee_total": { "value": 500, "unit": "人" } },
                         "sources": []
                       },
                       {
                         "id": "certifications",
                         "title": "资质与认证",
                         "summary": "Mock 资质认证",
                         "confidence": "low",
                         "content": {
                           "is_high_tech_enterprise": false,
                           "items": [],
                           "honors": []
                         },
                         "sources": []
                       },
                       {
                         "id": "timeline",
                         "title": "发展历程",
                         "summary": "Mock 发展历程",
                         "confidence": "low",
                         "content": { "events": [] },
                         "sources": []
                       },
                       {
                         "id": "contacts",
                         "title": "联系方式",
                         "summary": "Mock 联系方式",
                         "confidence": "low",
                         "content": { "locations": [], "public_emails": [] },
                         "sources": []
                       },
                       {
                         "id": "compliance_risks",
                         "title": "合规与司法风险",
                         "summary": "Mock 无司法风险记录",
                         "confidence": "low",
                         "content": {
                           "risk_level": "low",
                           "checks": [
                             { "type": "litigation", "count": 0, "status": "clear" }
                           ],
                           "attention_items": []
                         },
                         "sources": []
                       },
                       {
                         "id": "market_risks",
                         "title": "经营与市场风险",
                         "summary": "Mock 市场风险",
                         "confidence": "low",
                         "content": {
                           "risk_level": "low",
                           "items": [],
                           "customer_concentration": null,
                           "competition_summary": null,
                           "policy_risks": []
                         },
                         "sources": []
                       },
                       {
                         "id": "procurement_signals",
                         "title": "采购与供应链信号",
                         "summary": "Mock 采购信号",
                         "confidence": "low",
                         "content": {
                           "items": [],
                           "expansion_signals": [],
                           "bom_needs": [],
                           "localization_signals": []
                         },
                         "sources": []
                       },
                       {
                         "id": "opportunities",
                         "title": "商机线索",
                         "summary": "Mock 商机",
                         "confidence": "low",
                         "content": {
                           "items": [
                             {
                               "id": "opp-mock-1",
                               "type": "other",
                               "title": "Mock 商机示例",
                               "description": "请配置 moonshot 获取真实商机。",
                               "priority": "medium",
                               "suggested_actions": ["配置 AI_MOONSHOT_API_KEY"]
                             }
                           ]
                         },
                         "sources": []
                       },
                       {
                         "id": "key_people",
                         "title": "关键人与组织",
                         "summary": "Mock 关键人",
                         "confidence": "low",
                         "content": {
                           "people": [],
                           "org_summary": null,
                           "rd_team_summary": null
                         },
                         "sources": []
                       },
                       {
                         "id": "ai_assessment",
                         "title": "AI 综合评估",
                         "summary": "Mock 评估，仅供开发联调",
                         "confidence": "low",
                         "content": {
                           "dimensions": [],
                           "overall_summary": "Mock 数据",
                           "visit_strategy": {},
                           "recommended_next_steps": []
                         },
                         "sources": []
                       }
                     ],
                     "relations": {
                       "section_order": ["registry","ownership","business","scale","certifications","timeline","contacts","compliance_risks","market_risks","procurement_signals","opportunities","key_people","ai_assessment"],
                       "for_risk_control": ["registry","ownership","compliance_risks","market_risks"],
                       "for_sales_followup": ["opportunities","procurement_signals","timeline","key_people","ai_assessment"]
                     },
                     "disclaimer": "本信息来自公开渠道及 AI 整理，仅供参考；当前为 Mock 开发数据。"
                   }
                   """;
    }

    private static string BuildDefaultMockRfqJson()
    {
        return """
               {
                 "customer_name": "Mock 客户有限公司",
                 "contact_email": null,
                 "industry": "电子制造",
                 "product": "Mock 产品",
                 "rfq_type": 1,
                 "target_type": 1,
                 "quote_method": 2,
                 "assign_method": 2,
                 "importance": 2,
                 "project_background": null,
                 "competitor": null,
                 "remark": null,
                 "items": [
                   {
                     "customer_mpn": "CUST-PN-001",
                     "customer_brand": null,
                     "mpn": "STM32F103C8T6",
                     "brand": "ST",
                     "target_price": null,
                     "price_currency": 1,
                     "quantity": 1000,
                     "production_date": null,
                     "expiry_date": null,
                     "min_package_qty": null,
                     "moq": null,
                     "alternatives": null,
                     "remark": "Mock 开发数据，请配置 moonshot 后使用真实解析。"
                   }
                 ]
               }
               """;
    }

    private static bool LooksLikeCompanyName(string line)
    {
        return line.Contains("有限公司", StringComparison.Ordinal)
               || line.Contains("股份公司", StringComparison.Ordinal)
               || line.Contains("集团", StringComparison.Ordinal)
               || line.EndsWith("公司", StringComparison.Ordinal);
    }

    private static bool LooksLikeMpn(string line)
    {
        if (line.Length < 4)
            return false;
        if (LooksLikeCompanyName(line))
            return false;
        return Regex.IsMatch(line, @"^[A-Za-z0-9][A-Za-z0-9./:_+\-]+$");
    }

    private static bool TryParseQuantityPriceLine(string line, out int quantity, out decimal? targetPrice, out int priceCurrency)
    {
        quantity = 1;
        targetPrice = null;
        priceCurrency = 1;

        var qtyMatch = Regex.Match(line, @"(?<qty>\d+)\s*(?:pc|pcs|PC|PCS|颗|个|片|k|K)?", RegexOptions.CultureInvariant);
        if (!qtyMatch.Success || !int.TryParse(qtyMatch.Groups["qty"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity))
            return false;

        var priceMatch = Regex.Match(line, @"(?<price>\d+(?:\.\d+)?)", RegexOptions.CultureInvariant);
        if (priceMatch.Success && decimal.TryParse(priceMatch.Groups["price"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
            targetPrice = price;

        if (line.Contains("美元", StringComparison.Ordinal) || line.Contains("USD", StringComparison.OrdinalIgnoreCase) || line.Contains('$'))
            priceCurrency = 2;
        else if (line.Contains("欧元", StringComparison.Ordinal) || line.Contains("EUR", StringComparison.OrdinalIgnoreCase))
            priceCurrency = 3;
        else if (line.Contains("港币", StringComparison.Ordinal) || line.Contains("港元", StringComparison.Ordinal) || line.Contains("HKD", StringComparison.OrdinalIgnoreCase))
            priceCurrency = 4;
        else if (line.Contains("人民币", StringComparison.Ordinal) || line.Contains("RMB", StringComparison.OrdinalIgnoreCase) || line.Contains("CNY", StringComparison.OrdinalIgnoreCase) || line.Contains('￥') || line.Contains('¥'))
            priceCurrency = 1;

        return qtyMatch.Success;
    }
}
