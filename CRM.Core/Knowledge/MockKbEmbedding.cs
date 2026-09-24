using System.Security.Cryptography;
using System.Text;

namespace CRM.Core.Knowledge;

public static class MockKbEmbedding
{
    public static float[] Vector(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? ""));
        var values = new float[KbHandbookCodes.EmbeddingDimension];
        for (var i = 0; i < values.Length; i++)
            values[i] = (hash[i % hash.Length] / 255f) - 0.5f;

        double sum = 0;
        foreach (var value in values)
            sum += value * value;
        var norm = Math.Sqrt(sum);
        if (norm <= 0)
            return values;
        for (var i = 0; i < values.Length; i++)
            values[i] = (float)(values[i] / norm);
        return values;
    }
}
