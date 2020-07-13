using System;
using System.Collections.Generic;

namespace FeatureNinjas.LogPack
{
    public class LogPackTracer
    {
        public static LogPackTracer Tracer = new LogPackTracer();

        private Dictionary<string, List<string>> _traces = new Dictionary<string, List<string>>();

        private readonly int objectId = new Random().Next();

        private LogPackTracer()
        {
        }

        public void Trace(string context, string message)
        {
            if (!_traces.ContainsKey(context))
                _traces.Add(context, new List<string>());
            _traces[context].Add(message);
        }

        public List<string> Get(string context)
        {
            if (_traces.ContainsKey(context))
                return _traces[context];
            return new List<string>();
        }

        public void Remove(string context)
        {
            _traces.Remove(context);
        }
    }
}