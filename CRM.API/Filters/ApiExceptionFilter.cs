using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CRM.API.Filters
{
    public class ApiExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<ApiExceptionFilter> _logger;

        public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "API exception occurred");

            var response = new
            {
                success = false,
                message = context.Exception.Message,
                errorCode = 500,
                data = (object?)null
            };

            context.Result = new ObjectResult(response)
            {
                StatusCode = 500
            };
        }
    }
}
