using CRM.Core.Document;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Infrastructure.Document
{
    public static class DocumentModuleExtensions
    {
        public static IServiceCollection AddDocumentModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DocumentModuleOptions>(configuration.GetSection(DocumentModuleOptions.SectionName));
            services.AddScoped<FileNameGenerator>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<IDocumentService, DocumentService>();
            return services;
        }
    }
}
