using System.Threading.Tasks;

namespace FeatureNinjas.LogPack
{
    public interface INotificationService
    {
        Task Send(string logPackName, string meta);
    }
}