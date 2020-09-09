using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace FeatureNinjas.LogPack.NotifcationServices
{
    public class EmailNotificationService : INotificationService
    {
        private readonly string _to;
        private readonly string _from;
        private readonly string _server;
        private readonly string _username;
        private readonly string _password;

        public EmailNotificationService(string to, string from, string server, string username, string password)
        {
            _from = from;
            _to = to;
            _server = server;
            _username = username;
            _password = password;
        }
        
        public Task Send(string logPackName)
        {
            var client = new SmtpClient(_server);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(_username, _password);

            var body = $"New LogPack uploaded. Check!: {logPackName}";

            var message = new MailMessage();
            message.From = new MailAddress(_from);
            message.To.Add(_to);
            message.IsBodyHtml = false;
            message.Body = body;
            message.Subject = $"New LogPack uploaded. Check!: {logPackName}";

            return client.SendMailAsync(message);
        }
    }
}