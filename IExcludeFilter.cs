using Microsoft.AspNetCore.Http;

namespace FeatureNinjas.LogPack
{
    public interface IExcludeFilter
    {
        bool Exclude(HttpContext context);
    }
}