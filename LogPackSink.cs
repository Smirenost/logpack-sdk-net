using System.Threading.Tasks;

namespace FeatureNinjas.LogPack
{
    public abstract class LogPackSink
    {
        public abstract Task Send(string file);
    }
}