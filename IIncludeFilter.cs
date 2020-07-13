using Microsoft.AspNetCore.Http;

namespace FeatureNinjas.LogPack
{
    public interface IIncludeFilter
    {
        bool Include(HttpContext context);
    }
}