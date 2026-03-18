namespace CRM.Core.Document
{
    /// <summary>
    /// 文档模块配置（appsettings DocumentModule 节点）
    /// </summary>
    public class DocumentModuleOptions
    {
        public const string SectionName = "DocumentModule";

        /// <summary>存储根目录，如 D:\FrontCRM_Uploads</summary>
        public string RootPath { get; set; } = "Uploads";

        /// <summary>单文件最大大小（MB）</summary>
        public int MaxFileSizeMb { get; set; } = 50;

        /// <summary>单次上传最多文件数</summary>
        public int MaxFilesPerUpload { get; set; } = 5;

        /// <summary>允许的扩展名白名单，如 .pdf,.jpg,.jpeg,.png,.docx,.xlsx,.zip</summary>
        public string[] AllowedExtensions { get; set; } = { ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".xlsx", ".zip" };

        /// <summary>缩略图配置</summary>
        public ThumbnailOptions Thumbnail { get; set; } = new();
    }

    public class ThumbnailOptions
    {
        public bool Enable { get; set; } = true;
        public int MaxWidth { get; set; } = 400;
        public int MaxHeight { get; set; } = 400;
        public int Quality { get; set; } = 80;
    }
}
