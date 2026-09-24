using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CRM.Core.Interfaces;
using CRM.Core.Knowledge;

namespace CRM.Infrastructure.Knowledge;

public interface IKbEmbeddingClient
{
    Task<IReadOnlyList<float[]>> EmbedAsync(
        string baseUrl,
        string apiKey,
        string model,
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default);
}

public sealed class OpenAiCompatibleEmbeddingClient : IKbEmbeddingClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenAiCompatibleEmbeddingClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        string baseUrl,
        string apiKey,
        string model,
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        if (inputs.Count == 0)
            return Array.Empty<float[]>();

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(120);
        var url = baseUrl.TrimEnd('/') + "/embeddings";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { model, input = inputs }),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"向量接口失败：{(int)response.StatusCode} {Trim(body)}");

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("data", out var data))
            throw new InvalidOperationException("向量接口没有返回 data。");

        var vectors = new float[inputs.Count][];
        foreach (var item in data.EnumerateArray())
        {
            var index = item.GetProperty("index").GetInt32();
            var embedding = item.GetProperty("embedding");
            var values = new float[embedding.GetArrayLength()];
            var i = 0;
            foreach (var number in embedding.EnumerateArray())
                values[i++] = number.GetSingle();
            if (values.Length != KbHandbookCodes.EmbeddingDimension)
                throw new InvalidOperationException($"向量维度是 {values.Length}，需要 {KbHandbookCodes.EmbeddingDimension}。");
            if (index < 0 || index >= vectors.Length)
                throw new InvalidOperationException("向量接口返回的 index 超出批次范围。");
            vectors[index] = values;
        }

        if (vectors.Any(v => v == null))
            throw new InvalidOperationException("向量接口返回的条数与输入不一致。");
        return vectors;
    }

    private static string Trim(string body) =>
        body.Length <= 300 ? body : body[..300];
}
