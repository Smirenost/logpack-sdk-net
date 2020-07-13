namespace FeatureNinjas.LogPack
{
    public class LogPackOptions
    {
        public string[] IncludeFiles { get; set; } = new string[0];

        public LogPackSink[] Sinks { get; set; } = new LogPackSink[0];
        
        public IIncludeFilter[] Include { get; set; } = new IIncludeFilter[0];
    }
}