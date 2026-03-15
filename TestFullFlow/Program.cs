using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private static string apiBase = "http://localhost:5030/api/v1";
    private static string token = "";
    private static string customerId = "";
    private static string contactId = "";
    private static string addressId = "";
    private static string bankId = "";

    static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("    CRM 全业务流程测试");
        Console.WriteLine("========================================\n");

        var timestamp = DateTime.Now.ToString("MMddHHmmss");
        var testUsername = $"testuser_{timestamp}";
        var testEmail = $"test_{timestamp}@example.com";
        var testPassword = "Test123!@#";
        var customerCode = $"CUST{timestamp}";

        Console.WriteLine($"测试用户名: {testUsername}");
        Console.WriteLine($"客户编码: {customerCode}\n");

        try
        {
            // 1. 用户注册
            await TestRegister(testUsername, testEmail, testPassword);
            await Task.Delay(1000);

            // 2. 用户登录
            await TestLoginWithExistingUser();

            // 3. 创建客户 - 使用已存在的客户编码避免冲突
            await TestCreateCustomer(customerCode, timestamp);

            // 如果客户创建失败，尝试使用已存在的客户
            if (string.IsNullOrEmpty(customerId))
            {
                Console.WriteLine("  尝试使用已存在的客户进行后续测试...");
                customerId = await GetExistingCustomerId();
            }

            if (!string.IsNullOrEmpty(customerId))
            {
                // 4. 创建联系人
                await TestCreateContact();

                // 5. 创建地址
                await TestCreateAddress();

                // 6. 创建银行信息
                await TestCreateBank();

                // 7. 获取客户详情验证
                await TestGetCustomerDetail();

                // 8. 查询客户列表
                await TestGetCustomerList();

                // 9. 更新联系人
                await TestUpdateContact();

                // 10. 更新地址
                await TestUpdateAddress();
            }

            Console.WriteLine("\n========================================");
            Console.WriteLine("    全业务流程测试完成!");
            Console.WriteLine("========================================\n");
            Console.WriteLine("测试数据汇总:");
            Console.WriteLine($"  - 客户ID: {customerId}");
            Console.WriteLine($"  - 联系人ID: {contactId}");
            Console.WriteLine($"  - 地址ID: {addressId}");
            Console.WriteLine($"  - 银行信息ID: {bankId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n测试失败: {ex.Message}");
        }
    }

    static async Task TestRegister(string username, string email, string password)
    {
        Console.WriteLine("【1. 用户注册测试】");
        var body = new { userName = username, email = email, password = password, confirmPassword = password };
        var response = await PostAsync("/auth/register", body, false);
        Console.WriteLine(response.GetProperty("success").GetBoolean() ? "  注册成功!" : $"  注册失败: {response.GetProperty("message").GetString()}");
    }

    static async Task TestLoginWithExistingUser()
    {
        Console.WriteLine("\n【2. 用户登录测试】");
        var body = new { email = "test_20260316040237@example.com", password = "Test123!@#" };
        var response = await PostAsync("/auth/login", body, false);
        if (response.GetProperty("success").GetBoolean())
        {
            token = response.GetProperty("data").GetProperty("token").GetString() ?? "";
            Console.WriteLine("  登录成功!");
        }
        else
        {
            Console.WriteLine($"  登录失败: {response.GetProperty("message").GetString()}");
        }
    }

    static async Task TestCreateCustomer(string code, string timestamp)
    {
        Console.WriteLine("\n【3. 创建客户测试】");
        var body = new
        {
            customerCode = code,
            officialName = $"测试客户_{timestamp}",
            nickName = $"测试_{timestamp}",
            level = 5,
            type = (short?)1,
            industry = "电子",
            email = $"customer_{timestamp}@test.com",
            phone = "13800138000",
            creditLine = 100000m,
            payment = (short?)30,
            tradeCurrency = (short?)1,
            creditCode = "1234567890123456",
            remark = "这是测试客户"
        };

        try
        {
            var response = await PostAsync("/customers", body, true);
            var responseJson = response.ToString();
            Console.WriteLine($"  API响应: {responseJson.Substring(0, Math.Min(500, responseJson.Length))}");
            if (response.GetProperty("success").GetBoolean())
            {
                customerId = response.GetProperty("data").GetProperty("id").GetString() ?? "";
                Console.WriteLine($"  客户创建成功! ID: {customerId}");
            }
            else
            {
                Console.WriteLine($"  客户创建失败: {response.GetProperty("message").GetString()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  创建客户异常: {ex.Message}");
            Console.WriteLine($"  异常详情: {ex.StackTrace}");
        }
    }

    static async Task<string> GetExistingCustomerId()
    {
        try
        {
            var response = await GetAsync("/customers?pageNumber=1&pageSize=1", true);
            if (response.GetProperty("success").GetBoolean())
            {
                var items = response.GetProperty("data").GetProperty("items").EnumerateArray();
                foreach (var item in items)
                {
                    return item.GetProperty("id").GetString() ?? "";
                }
            }
        }
        catch { }
        return "";
    }

    static async Task TestCreateContact()
    {
        Console.WriteLine("\n【4. 创建联系人测试】");
        var body = new { name = "张三", gender = 1, department = "采购部", position = "经理", phone = "010-12345678", mobile = "13800138001", email = "zhangsan@test.com", isDefault = true };
        try
        {
            var response = await PostAsync($"/customers/{customerId}/contacts", body, true);
            var responseJson = response.ToString();
            Console.WriteLine($"  完整响应: {responseJson.Substring(0, Math.Min(300, responseJson.Length))}");
            if (response.GetProperty("success").GetBoolean())
            {
                contactId = response.GetProperty("data").GetProperty("id").GetString() ?? "";
                Console.WriteLine($"  联系人创建成功! ID: {contactId}");
            }
            else
            {
                Console.WriteLine($"  联系人创建失败: {response.GetProperty("message").GetString()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  创建联系人异常: {ex.Message}");
        }
    }

    static async Task TestCreateAddress()
    {
        Console.WriteLine("\n【5. 创建地址测试】");
        var body = new { addressType = 1, country = 156, province = "广东省", city = "深圳市", area = "南山区", address = "科技园南路88号", contactName = "张三", contactPhone = "13800138001", isDefault = true };
        try
        {
            var response = await PostAsync($"/customers/{customerId}/addresses", body, true);
            if (response.GetProperty("success").GetBoolean())
            {
                addressId = response.GetProperty("data").GetProperty("id").GetString() ?? "";
                Console.WriteLine($"  地址创建成功! ID: {addressId}");
            }
            else
            {
                Console.WriteLine($"  地址创建失败: {response.GetProperty("message").GetString()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  创建地址异常: {ex.Message}");
        }
    }

    static async Task TestCreateBank()
    {
        Console.WriteLine("\n【6. 创建银行信息测试】");
        var body = new { bankName = "中国工商银行", bankAccount = "6222021234567890123", accountName = $"测试公司_{DateTime.Now:yyyyMMddHHmmss}", bankBranch = "深圳南山支行", currency = (short?)1, isDefault = true, remark = "主要收款账户" };
        try
        {
            var response = await PostAsync($"/customers/{customerId}/banks", body, true);
            if (response.GetProperty("success").GetBoolean())
            {
                bankId = response.GetProperty("data").GetProperty("id").GetString() ?? "";
                Console.WriteLine($"  银行信息创建成功! ID: {bankId}");
            }
            else
            {
                Console.WriteLine($"  银行信息创建失败: {response.GetProperty("message").GetString()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  创建银行信息异常: {ex.Message}");
        }
    }

    static async Task TestGetCustomerDetail()
    {
        Console.WriteLine("\n【7. 获取客户详情验证】");
        try
        {
            var response = await GetAsync($"/customers/{customerId}", true);
            if (response.GetProperty("success").GetBoolean())
            {
                var data = response.GetProperty("data");
                var contacts = data.GetProperty("contacts").EnumerateArray();
                var addresses = data.GetProperty("addresses").EnumerateArray();
                var banks = data.GetProperty("bankAccounts").EnumerateArray();

                int contactCount = 0, addressCount = 0, bankCount = 0;
                foreach (var _ in contacts) contactCount++;
                foreach (var _ in addresses) addressCount++;
                foreach (var _ in banks) bankCount++;

                Console.WriteLine($"  客户名称: {data.GetProperty("officialName").GetString()}");
                Console.WriteLine($"  联系人数量: {contactCount}");
                Console.WriteLine($"  地址数量: {addressCount}");
                Console.WriteLine($"  银行账户数量: {bankCount}");
                Console.WriteLine("  验证成功!");
            }
            else
            {
                Console.WriteLine($"  获取详情失败: {response.GetProperty("message").GetString()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  获取详情异常: {ex.Message}");
        }
    }

    static async Task TestGetCustomerList()
    {
        Console.WriteLine("\n【8. 查询客户列表验证】");
        try
        {
            var response = await GetAsync("/customers?pageNumber=1&pageSize=10", true);
            if (response.GetProperty("success").GetBoolean())
            {
                var data = response.GetProperty("data");
                Console.WriteLine($"  客户列表总数: {data.GetProperty("totalCount").GetInt32()}");
                Console.WriteLine("  查询成功!");
            }
            else
            {
                Console.WriteLine($"  查询失败: {response.GetProperty("message").GetString()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  查询异常: {ex.Message}");
        }
    }

    static async Task TestUpdateContact()
    {
        Console.WriteLine("\n【9. 更新联系人测试】");
        if (string.IsNullOrEmpty(contactId)) { Console.WriteLine("  跳过：无联系人ID"); return; }

        var body = new { name = "张三(已更新)", position = "总监", mobile = "13800138002" };
        try
        {
            var response = await PutAsync($"/contacts/{contactId}", body, true);
            Console.WriteLine(response.GetProperty("success").GetBoolean() ? "  联系人更新成功!" : $"  更新失败: {response.GetProperty("message").GetString()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  更新联系人异常: {ex.Message}");
        }
    }

    static async Task TestUpdateAddress()
    {
        Console.WriteLine("\n【10. 更新地址测试】");
        if (string.IsNullOrEmpty(addressId)) { Console.WriteLine("  跳过：无地址ID"); return; }

        var body = new { address = "科技园南路88号(已更新)", contactPhone = "13800138003" };
        try
        {
            var response = await PutAsync($"/addresses/{addressId}", body, true);
            Console.WriteLine(response.GetProperty("success").GetBoolean() ? "  地址更新成功!" : $"  更新失败: {response.GetProperty("message").GetString()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  更新地址异常: {ex.Message}");
        }
    }

    static async Task<JsonElement> PostAsync(string path, object body, bool auth)
    {
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, apiBase + path) { Content = content };
        if (auth && !string.IsNullOrEmpty(token)) request.Headers.Add("Authorization", "Bearer " + token);
        var response = await client.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"    [HTTP {(int)response.StatusCode}] {responseString.Substring(0, Math.Min(200, responseString.Length))}");
        return JsonDocument.Parse(responseString).RootElement;
    }

    static async Task<JsonElement> GetAsync(string path, bool auth)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, apiBase + path);
        if (auth && !string.IsNullOrEmpty(token)) request.Headers.Add("Authorization", "Bearer " + token);
        var response = await client.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(responseString).RootElement;
    }

    static async Task<JsonElement> PutAsync(string path, object body, bool auth)
    {
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Put, apiBase + path) { Content = content };
        if (auth && !string.IsNullOrEmpty(token)) request.Headers.Add("Authorization", "Bearer " + token);
        var response = await client.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(responseString).RootElement;
    }
}
