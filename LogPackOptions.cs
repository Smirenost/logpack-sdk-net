using System;

namespace FeatureNinjas.LogPack
{
    public class LogPackOptions
    {
        public string[] IncludeFiles { get; set; } = new string[0];

        public LogPackSink[] Sinks { get; set; } = new LogPackSink[0];
        
        public IExcludeFilter[] Exclude { get; set; } = new IExcludeFilter[0]; 
        
        public IIncludeFilter[] Include { get; set; } = new IIncludeFilter[0];

        public INotificationService[] NotificationServices { get; set; } = new INotificationService[0];

        public Type ProgramType { get; set; } = null;

        public bool IncludeRequestPayload { get; set; } = false;

        public bool IncludeResponse { get; set; } = false;

        public bool IncludeResponsePayload { get; set; } = false;

        public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;
    }
}