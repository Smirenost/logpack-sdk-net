using Microsoft.AspNetCore.Builder;

namespace FeatureNinjas.LogPack
{
    public static class LogPackMiddlewareExtensions
    {
        public static IApplicationBuilder UseLogPack(this IApplicationBuilder builder, LogPackOptions options = null) 
        {
            return builder.UseMiddleware<LogPackMiddleware>(options);
        }
    }
}