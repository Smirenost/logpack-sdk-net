using System;
using Microsoft.AspNetCore.Http;

namespace FeatureNinjas.LogPack.Filters
{
    public class PathExcludeFilter : IExcludeFilter
    {
        private string _path;
        private bool _isCaseSensitive;
        
        public PathExcludeFilter(string path, bool isCaseSensitive)
        {
            _path = path;
        }

        public bool Exclude(HttpContext context)
        {
            var stringComparison = _isCaseSensitive
                ? StringComparison.InvariantCulture
                : StringComparison.InvariantCultureIgnoreCase;
            if (_path.Equals(context.Request.Path, stringComparison))
                return true;

            return false;
        }
    }
}