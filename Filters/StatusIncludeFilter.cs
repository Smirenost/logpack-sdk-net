using System.Linq;
using Microsoft.AspNetCore.Http;

namespace FeatureNinjas.LogPack.Filters
{
    public class StatusIncludeFilter : IIncludeFilter
    {
        private bool _isRange = false;
        private int _minStatusCode;
        private int _maxStatusCode;
        private int[] _rangeStatusCodes;
        
        public StatusIncludeFilter(int min, int max)
        {
            _isRange = true;
            _minStatusCode = min;
            _maxStatusCode = max;
        }

        public StatusIncludeFilter(int[] codes)
        {
            _isRange = false;
            _rangeStatusCodes = codes;
        }
        
        public bool Include(HttpContext context)
        {
            var sc = context.Response.StatusCode;
            if (_isRange)
            {
                if (sc >= _minStatusCode && sc < _maxStatusCode)
                {
                    return true;
                }
            }
            else
            {
                if (_rangeStatusCodes.Any(s => s == sc))
                {
                    return true;
                }
            }

            return false;
        }
    }
}