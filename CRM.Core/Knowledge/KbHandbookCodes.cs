namespace CRM.Core.Knowledge;

public static class KbHandbookCodes
{
    public const string DocumentCode = "handbook.distributor.newcomer";
    public const string Scenario = "knowledge.handbook.qa";
    public const string AskPermission = "biz.ai.kb.qa";
    public const string AdminPermission = "biz.ai.kb.admin";
    public const string EmbeddingEnv = "AI_EMBEDDING_API_KEY";
    public const string ProviderCode = "siliconflow";
    public const string EmbeddingModel = "BAAI/bge-m3";
    public const int EmbeddingDimension = 1024;
    public const int TopK = 5;
    public const double DefaultMaxTopDistance = 0.45;
    public const double DefaultMaxChunkDistance = 0.55;

    public const short StatusPending = 0;
    public const short StatusEmbedding = 1;
    public const short StatusReady = 2;
    public const short StatusFailed = 3;
    public const short StatusRetired = 4;
}
