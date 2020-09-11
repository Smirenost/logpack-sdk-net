using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FeatureNinjas.LogPack.NotifcationServices
{
    public class TelegramNotificationService : INotificationService
    {
        public string ApiKey { get; set; } = null;
        public string ChatId { get; set; } = null;

        public TelegramNotificationService()
        {
            
        }
        
        public TelegramNotificationService(string apiKey, string chatId)
        {
            ApiKey = apiKey;
            ChatId = chatId;
        }
        
        public async Task Send(string logPackName, string meta)
        {
            var client = new HttpClient();
            
            var message = new StringBuilder();
            message.AppendLine($"New LogPack uploaded: {logPackName}");
            message.AppendLine();
            message.AppendLine(meta);
            
            var uri =
                $"https://api.telegram.org/bot{ApiKey}/sendMessage?chat_id={ChatId}&text={message.ToString()}";
            var response = await client.GetAsync(uri);
            
            if (response.StatusCode != HttpStatusCode.OK)
                Console.WriteLine("Could not send telegram message");
        }
    }
}