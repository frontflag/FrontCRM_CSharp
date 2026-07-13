-- 增量：customer.intel.lookup 契约扩展至 13 章（Phase 2）
-- 已有库执行本脚本；新库亦可先跑 ai_customer_intel_lookup_postgresql.sql 再跑本脚本

UPDATE public.ai_prompt_template
SET system_prompt = '你是面向中国销售与风控人员的客户情报调查助手。根据企业名称等信息，仅返回合法 JSON（禁止 markdown 代码块）。JSON 键名必须保持英文 snake_case。所有描述性字符串值必须使用简体中文。未知用 null，无数据用空数组；禁止编造司法风险数量、行政处罚、联系方式、股东持股比例。必须输出 meta（schema_version 填 1.1）、query、sections（固定 13 章）、relations、disclaimer。'
    || ' sections 必须包含且 id 固定为：registry、ownership、business、scale、certifications、timeline、contacts、compliance_risks、market_risks、procurement_signals、opportunities、key_people、ai_assessment。每章含 id、title、summary、confidence（high|medium|low）、content、sources。'
    || ' ownership：shareholders[{name,share_ratio,shareholder_type,note}]、parent_company、ultimate_controller、listed_info{is_listed,stock_code,exchange}、ownership_notes。'
    || ' certifications：is_high_tech_enterprise、items[{name,certification_type,issuer,valid_until,status}]、honors[]。'
    || ' market_risks：risk_level、items[{type,title,description,severity}]、customer_concentration、competition_summary、policy_risks[]。'
    || ' procurement_signals：items[{type,title,description,urgency,suggested_actions[]}]、expansion_signals[]、bom_needs[]、localization_signals[]。'
    || ' key_people：people[{name,role,department,background,public_contact}]、org_summary、rd_team_summary。'
    || ' compliance_risks.checks[].count 查不到填 null，status 可填 unknown。ai_assessment.dimensions[].basis_section_ids 必须引用事实章节 id。relations.section_order 为上述 13 个 id 顺序数组。',
    user_prompt_template = '请调查以下企业的公开信息：企业名称 '
        || CHR(123) || CHR(123) || 'company_name' || CHR(125) || CHR(125)
        || '；统一社会信用代码 '
        || CHR(123) || CHR(123) || 'credit_code' || CHR(125) || CHR(125)
        || '；地区 '
        || CHR(123) || CHR(123) || 'region' || CHR(125) || CHR(125)
        || '。返回 13 章客户情报 JSON（registry、ownership、business、scale、certifications、timeline、contacts、compliance_risks、market_risks、procurement_signals、opportunities、key_people、ai_assessment）。',
    json_schema_hint = '{"meta":{"schema_version":"1.1","company_name_primary":"string","credit_code":"string|null","overall_confidence":"string"},"query":{"company_name":"string","credit_code":"string|null","region":"string|null","intent":"full"},"sections":[{"id":"registry|ownership|business|scale|certifications|timeline|contacts|compliance_risks|market_risks|procurement_signals|opportunities|key_people|ai_assessment","title":"string","summary":"string","confidence":"high|medium|low","content":{},"sources":[]}],"relations":{"section_order":["registry","ownership","business","scale","certifications","timeline","contacts","compliance_risks","market_risks","procurement_signals","opportunities","key_people","ai_assessment"],"for_risk_control":["registry","ownership","compliance_risks","market_risks"],"for_sales_followup":["opportunities","procurement_signals","timeline","key_people","ai_assessment"]},"disclaimer":"string"}',
    modify_time = timezone('utc', now())
WHERE code = 'customer.intel.lookup' AND version = 1;

UPDATE public.ai_scenario
SET description = '按企业名称调查公开客户情报（13章结构化报告）',
    modify_time = timezone('utc', now())
WHERE code = 'customer.intel.lookup';

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'customer.intel.lookup';
